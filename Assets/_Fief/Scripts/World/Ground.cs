using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le terrain : un vrai relief, genere en maillage (mesh) au lancement.
    ///
    /// Avant, le sol etait un plan plat : d'ou l'impression de vide. Maintenant
    /// c'est un paysage de collines, avec des zones de jeu APLANIES (marche, fiefs)
    /// pour que rien ne soit pose de travers, et du relief entre les deux pour qu'on
    /// ait quelque chose a regarder en marchant.
    ///
    /// Concept Unity : un Mesh, c'est une liste de sommets (vertices) + une liste de
    /// triangles qui reliet ces sommets. Le MeshCollider reprend le meme maillage pour
    /// que le joueur marche dessus.
    /// </summary>
    public static class Ground
    {
        struct Hill
        {
            public Vector2 center;
            public float radius;
            public float height;
        }

        struct FlatArea
        {
            public Vector2 center;
            public float radius;
            public float strength;   // 1 = aplani a fond, 0.35 = relief attenue
        }

        struct Basin
        {
            public Vector2 center;
            public float radius;
            public float depth;
        }

        struct FlatCorridor
        {
            public Vector2 a;
            public Vector2 b;
            public float radius;
            public float strength;
        }

        static readonly List<Hill> hills = new List<Hill>();
        static readonly List<FlatArea> flats = new List<FlatArea>();
        static readonly List<FlatCorridor> corridors = new List<FlatCorridor>();
        static readonly List<Basin> basins = new List<Basin>();

        // Grille des hauteurs, remplie pendant Build. Deux raisons de s'en servir :
        //  - c'est ~60x plus rapide que de recalculer 36 collines par appel, et on
        //    place des milliers d'arbres ;
        //  - c'est EXACTEMENT la surface affichee. Le maillage interpole lineairement
        //    entre les coins de chaque cellule de 9 m : un objet pose avec la formule
        //    exacte flotterait ou s'enfoncerait jusqu'a un metre sur une pente.
        static float[] grid;
        static int gridSide;
        static float gridCell;
        static float gridHalf;

        static float mapSize = 400f;
        static float rimHeight = 14f;   // un talus, pas une muraille : la brume suffit a fermer
        static bool ready;

        public static bool Ready { get { return ready; } }

        // ------------------------------------------------------------------ relief

        /// <summary>
        /// Definit les collines et les zones a aplanir. A appeler AVANT de placer
        /// quoi que ce soit dans le monde.
        /// </summary>
        public static void Prepare(GameConfig cfg)
        {
            grid = null;
            hills.Clear();
            flats.Clear();
            corridors.Clear();
            basins.Clear();
            mapSize = cfg.mapSize;

            // LE RELIEF D'UNE SYLVE.
            //
            // Il etait taille pour l'ancien monde : douze collines-barrieres posees
            // sur les lignes de vue entre chateaux, des couloirs aplanis le long des
            // routes, cinq lacs. Rien de tout ca n'a de sens ici.
            //
            // Sous un couvert ou l'on ne voit pas a quarante metres, le relief ne
            // sert plus a composer un panorama -- on ne verra jamais de panorama. Il
            // sert a ce qu'on ne marche jamais droit : une montee douce, un creux
            // humide, un replat. C'est ce qui fait qu'on perd le nord, et se perdre
            // est la moitie du sujet.
            //
            // Il est donc tire au sort a partir de la graine du monde, et non plus
            // ecrit a la main : la carte suit desormais sa taille et sa graine sans
            // qu'on ait a replacer trente collines une par une.
            System.Random rng = new System.Random(cfg.worldSeed ^ 0x51F5E);
            float half = mapSize * 0.5f;

            // Une ondulation tous les ~9000 m2 : assez pour qu'il y ait toujours du
            // relief dans les quarante metres qu'on voit.
            int count = Mathf.Clamp(Mathf.RoundToInt(mapSize * mapSize / 9000f), 12, 400);
            for (int i = 0; i < count; i++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                float radius = 34f + (float)rng.NextDouble() * 96f;

                // Autant de creux que de bosses : un terrain qui ne fait que monter
                // se lit comme une serie de taupinieres.
                float height = ((float)rng.NextDouble() * 2f - 1f) * 7.5f;
                AddHill(x, z, radius, height);
            }

            // Quelques vallons francs, plus larges et plus creux : ce sont eux qu'on
            // reconnait, et donc les seuls reperes possibles quand on n'a pas d'horizon.
            for (int i = 0; i < 5; i++)
            {
                float a = (i / 5f) * Mathf.PI * 2f + (float)rng.NextDouble();
                float d = half * (0.25f + (float)rng.NextDouble() * 0.5f);
                AddHill(Mathf.Cos(a) * d, Mathf.Sin(a) * d,
                        150f + (float)rng.NextDouble() * 90f, -11f);
            }

            // Le chateau pose sur un replat parfait. Dans son emprise la hauteur vaut
            // exactement zero, et elle rejoint le relief sur 90 m tout autour : on
            // monte ou descend en douceur vers ses murs, jamais de marche.
            AddFlat(Vector2.zero, Castle.FlatRadius, 1f);

            ready = true;
        }

        static void AddBasin(float x, float z, float radius, float depth)
        {
            Basin b = new Basin();
            b.center = new Vector2(x, z);
            b.radius = radius;
            b.depth = depth;
            basins.Add(b);
        }

        /// <summary>Niveau de la surface de l'eau d'un lac (les 3 lacs sont a la meme profondeur).</summary>
        public static float WaterLevel(int index)
        {
            if (index < 0 || index >= basins.Count) return 0f;
            return -basins[index].depth * 0.80f;
        }

        public static int LakeCount { get { return basins.Count; } }
        public static Vector2 LakeCenter(int index) { return basins[index].center; }
        public static float LakeRadius(int index) { return basins[index].radius; }

        static void AddCorridor(Vector2 a, Vector2 b, float radius, float strength)
        {
            FlatCorridor c = new FlatCorridor();
            c.a = a;
            c.b = b;
            c.radius = radius;
            c.strength = strength;
            corridors.Add(c);
        }

        static void AddHill(float x, float z, float radius, float height)
        {
            Hill h = new Hill();
            h.center = new Vector2(x, z);
            h.radius = radius;
            h.height = height;
            hills.Add(h);
        }

        static void AddFlat(Vector2 center, float radius, float strength)
        {
            FlatArea f = new FlatArea();
            f.center = center;
            f.radius = radius;
            f.strength = strength;
            flats.Add(f);
        }

        /// <summary>Altitude du sol en (x, z). C'est LA fonction qui place tout le reste.</summary>
        public static float Height(float x, float z)
        {
            if (!ready) return 0f;

            float h = 0f;

            for (int i = 0; i < hills.Count; i++)
            {
                Hill hill = hills[i];
                float dx = x - hill.center.x;
                float dz = z - hill.center.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d >= hill.radius) continue;
                // Cloche douce : 1 au centre, 0 au bord, sans cassure.
                h += hill.height * 0.5f * (1f + Mathf.Cos(Mathf.PI * d / hill.radius));
            }

            // Bord de carte : le terrain remonte en cuvette. Plus joli qu'un mur gris,
            // et ca dit au joueur ou s'arrete le monde.
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) / (mapSize * 0.5f);
            if (edge > 0.80f)
            {
                float t = Mathf.Clamp01((edge - 0.80f) / 0.20f);
                h += rimHeight * t * t;
            }

            // Ondulation de fond : evite l'effet "collines posees sur une table".
            h += Mathf.Sin(x * 0.0065f) * Mathf.Cos(z * 0.0055f) * 4.5f;

            h *= FlattenFactor(x, z);

            // Les cuvettes des lacs, creusees en dernier.
            for (int i = 0; i < basins.Count; i++)
            {
                Basin b = basins[i];
                float dx = x - b.center.x;
                float dz = z - b.center.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > b.radius + 30f) continue;

                float k = 1f - Mathf.Clamp01((d - b.radius) / 30f);
                k = k * k * (3f - 2f * k);          // adoucit la rive
                h = Mathf.Lerp(h, -b.depth, k);
            }

            return h;
        }

        /// <summary>0 = totalement aplani, 1 = relief complet. Transitions larges et douces.</summary>
        static float FlattenFactor(float x, float z)
        {
            float factor = 1f;
            for (int i = 0; i < flats.Count; i++)
            {
                FlatArea f = flats[i];
                float dx = x - f.center.x;
                float dz = z - f.center.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);

                // 56 m de transition : c'est large, mais c'est ce qui evite la marche
                // d'escalier a la sortie d'un fief. Reduis-le et les bords deviennent raides.
                float local = Mathf.Clamp01((d - f.radius) / 90f);
                local = Mathf.Lerp(1f - f.strength, 1f, local);
                if (local < factor) factor = local;
            }

            for (int i = 0; i < corridors.Count; i++)
            {
                FlatCorridor c = corridors[i];
                float d = DistanceToSegment(new Vector2(x, z), c.a, c.b);

                float local = Mathf.Clamp01((d - c.radius) / 58f);
                local = Mathf.Lerp(1f - c.strength, 1f, local);
                if (local < factor) factor = local;
            }

            return factor;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Hauteur lue dans la grille, interpolee : rapide et calee sur le maillage.</summary>
        public static float Sample(float x, float z)
        {
            if (grid == null) return Height(x, z);

            float fx = (x + gridHalf) / gridCell;
            float fz = (z + gridHalf) / gridCell;

            int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, gridSide - 2);
            int iz = Mathf.Clamp(Mathf.FloorToInt(fz), 0, gridSide - 2);
            float tx = Mathf.Clamp01(fx - ix);
            float tz = Mathf.Clamp01(fz - iz);

            float h00 = grid[iz * gridSide + ix];
            float h10 = grid[iz * gridSide + ix + 1];
            float h01 = grid[(iz + 1) * gridSide + ix];
            float h11 = grid[(iz + 1) * gridSide + ix + 1];

            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
        }

        /// <summary>Pose un point sur le sol. yOffset pour surelever legerement.</summary>
        public static Vector3 Place(float x, float z, float yOffset)
        {
            return new Vector3(x, Sample(x, z) + yOffset, z);
        }

        public static Vector3 Place(Vector3 position, float yOffset)
        {
            return new Vector3(position.x, Height(position.x, position.z) + yOffset, position.z);
        }

        /// <summary>Inclinaison du sol, de 0 (plat) a 1 (falaise). Sert a semer le decor.</summary>
        public static float Slope(float x, float z)
        {
            float step = grid != null ? gridCell : 2f;
            float hx = Sample(x + step, z) - Sample(x - step, z);
            float hz = Sample(x, z + step) - Sample(x, z - step);
            return Mathf.Clamp01(Mathf.Sqrt(hx * hx + hz * hz) / (2f * step));
        }

        // ------------------------------------------------------------------ maillage

        /// <summary>
        /// Construit le terrain.
        ///
        /// Deux maillages sont produits :
        ///  - un maillage LISSE pour le collider (sommets partages, leger) ;
        ///  - un maillage A FACETTES pour l'affichage, ou chaque triangle possede ses
        ///    propres sommets. C'est ce qui donne le look low-poly : chaque face capte
        ///    la lumiere a plat, sans degrade, comme du papier plie.
        ///
        /// Chaque triangle est range dans l'une des 9 teintes selon son altitude, sa
        /// pente et un bruit : rivage, quatre verts, eboulis, deux roches, neige.
        /// </summary>
        public static GameObject Build(Transform parent, GameConfig cfg)
        {
            const float cell = 9f;
            int steps = Mathf.RoundToInt(cfg.mapSize / cell);
            int side = steps + 1;
            float half = cfg.mapSize * 0.5f;

            // --- grille de hauteurs (et on la garde pour Sample)
            Vector3[] points = new Vector3[side * side];
            grid = new float[side * side];
            gridSide = side;
            gridCell = cell;
            gridHalf = half;

            for (int iz = 0; iz < side; iz++)
            {
                for (int ix = 0; ix < side; ix++)
                {
                    float x = -half + ix * cell;
                    float z = -half + iz * cell;
                    float y = Height(x, z);
                    points[iz * side + ix] = new Vector3(x, y, z);
                    grid[iz * side + ix] = y;
                }
            }

            // --- maillage du collider (sommets partages)
            int[] smoothTris = new int[steps * steps * 6];
            int t = 0;
            for (int iz = 0; iz < steps; iz++)
            {
                for (int ix = 0; ix < steps; ix++)
                {
                    int a = iz * side + ix;
                    int b = a + 1;
                    int c = a + side;
                    int d = c + 1;
                    smoothTris[t++] = a; smoothTris[t++] = c; smoothTris[t++] = b;
                    smoothTris[t++] = b; smoothTris[t++] = c; smoothTris[t++] = d;
                }
            }

            Mesh collisionMesh = new Mesh();
            collisionMesh.name = "TerrainCollision";
            collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            collisionMesh.vertices = points;
            collisionMesh.triangles = smoothTris;
            collisionMesh.RecalculateBounds();

            // --- maillage affiche, a facettes
            const int Bands = 9;
            List<Vector3> vertices = new List<Vector3>(steps * steps * 6);
            List<int>[] bands = new List<int>[Bands];
            for (int i = 0; i < Bands; i++) bands[i] = new List<int>();

            for (int iz = 0; iz < steps; iz++)
            {
                for (int ix = 0; ix < steps; ix++)
                {
                    int a = iz * side + ix;
                    int b = a + 1;
                    int c = a + side;
                    int d = c + 1;
                    EmitFacet(vertices, bands, points[a], points[c], points[b]);
                    EmitFacet(vertices, bands, points[b], points[c], points[d]);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Terrain";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.subMeshCount = Bands;
            for (int i = 0; i < Bands; i++) mesh.SetTriangles(bands[i], i);
            // Le grain du sol : des coordonnees de texture en metres (une repetition
            // tous les 3,5 m), prises a la verticale. Un triangle de 9 m n'est plus un
            // aplat : il a des cailloux, des aiguilles, des plaques de mousse.
            Vector2[] groundUv = new Vector2[vertices.Count];
            for (int k = 0; k < vertices.Count; k++) groundUv[k] = new Vector2(vertices[k].x, vertices[k].z) / 3.5f;
            mesh.uv = groundUv;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            GameObject go = new GameObject("Terrain");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            // LE SOL DE LA SYLVE. Neuf teintes, toutes entre 10 % et 27 % de clarte :
            // mousses dans les creux humides, litiere de feuilles sur les replats,
            // terre nue là où la pente lessive, roche mouillee sur les devers.
            // L'ancien nuancier allait du sable a la neige -- sous cette brume il
            // aurait fait une moquette vert vif.
            go.AddComponent<MeshRenderer>().sharedMaterials = new Material[]
            {
                Surfaces.Ground(new Color(0.11f, 0.13f, 0.11f)),   // 0 fond de vallon, detrempe
                Surfaces.Ground(Palette.Moss[0]),
                Surfaces.Ground(Palette.Moss[1]),
                Surfaces.Ground(Palette.Moss[2]),
                Surfaces.Ground(Palette.Moss[3]),
                Surfaces.Ground(Palette.Litter[0]),
                Surfaces.Ground(Palette.Litter[1]),
                Surfaces.Ground(Palette.Litter[2]),
                Surfaces.Rock(Palette.WetRocks[2])               // 8 devers, roche a nu
            };

            go.AddComponent<MeshCollider>().sharedMesh = collisionMesh;
            return go;
        }

        /// <summary>Ajoute un triangle avec ses propres sommets, dans la teinte qui lui convient.</summary>
        static void EmitFacet(List<Vector3> vertices, List<int>[] bands, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
            float length = normal.magnitude;
            float flatness = length > 0.0001f ? Mathf.Abs(normal.y / length) : 1f;

            float height = (v0.y + v1.y + v2.y) / 3f;
            float cx = (v0.x + v1.x + v2.x) / 3f;
            float cz = (v0.z + v1.z + v2.z) / 3f;
            float n = Noise(cx, cz);

            // Le sol d'une foret ne se lit pas a l'altitude -- il n'y a plus de
            // montagne ici, tout tient dans une quinzaine de metres. Il se lit a
            // l'HUMIDITE : ce qui est bas et plat retient l'eau et se couvre de
            // mousse, ce qui est haut et expose seche et se couvre de feuilles
            // mortes, ce qui est raide se lessive et montre la roche.
            int band;
            if (flatness < 0.58f)
            {
                band = 8;                                      // devers : roche a nu
            }
            else if (height < -6f + n * 3f)
            {
                band = 0;                                      // fond de vallon detrempe
            }
            else
            {
                // 0 = creux humide, 1 = croupe seche. Le bruit brouille la frontiere
                // pour qu'on ne lise pas les courbes de niveau.
                float dryness = Mathf.Clamp01((height + 8f) / 20f) * 0.72f + n * 0.44f;
                if (dryness < 0.20f) band = 1;
                else if (dryness < 0.36f) band = 2;
                else if (dryness < 0.52f) band = 3;
                else if (dryness < 0.66f) band = 4;
                else if (dryness < 0.80f) band = 5;
                else if (dryness < 0.92f) band = 6;
                else band = 7;
            }

            int index = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            bands[band].Add(index);
            bands[band].Add(index + 1);
            bands[band].Add(index + 2);
        }

        /// <summary>Bruit bon marche et reproductible, entre 0 et 1.</summary>
        static float Noise(float x, float z)
        {
            float v = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }
    }
}
