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
        static float rimHeight = 40f;
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

            // --- Les collines. Six d'entre elles sont des BARRIERES posees entre deux
            //     fiefs voisins : c'est elles qui empechent de voir un chateau depuis
            //     un autre. Dix massifs ferment l'horizon, vingt collines meublent le reste.
            AddHill(175f, 649f, 116f, 74f);
            AddHill(474f, 476f, 116f, 74f);
            AddHill(650f, 173f, 127f, 74f);
            AddHill(650f, -172f, 127f, 74f);
            AddHill(474f, -476f, 135f, 74f);
            AddHill(175f, -649f, 135f, 74f);
            AddHill(-175f, -649f, 116f, 74f);
            AddHill(-474f, -476f, 116f, 74f);
            AddHill(-650f, -173f, 127f, 74f);
            AddHill(-650f, 172f, 127f, 74f);
            AddHill(-474f, 476f, 135f, 74f);
            AddHill(-175f, 649f, 135f, 74f);
            AddHill(309f, 951f, 230f, 105f);
            AddHill(809f, 588f, 230f, 105f);
            AddHill(1000f, 0f, 230f, 105f);
            AddHill(809f, -588f, 230f, 105f);
            AddHill(309f, -951f, 230f, 105f);
            AddHill(-309f, -951f, 230f, 105f);
            AddHill(-809f, -588f, 230f, 105f);
            AddHill(-1000f, 0f, 230f, 105f);
            AddHill(-809f, 588f, 230f, 105f);
            AddHill(-309f, 951f, 230f, 105f);
            AddHill(-140f, 300f, 120f, 48f);
            AddHill(-140f, -300f, 120f, 48f);
            AddHill(340f, 40f, 120f, 48f);
            AddHill(400f, -120f, 120f, 45f);
            AddHill(-320f, 280f, 120f, 45f);
            AddHill(-320f, -280f, 120f, 45f);
            AddHill(-80f, 460f, 120f, 43f);
            AddHill(-80f, -460f, 120f, 42f);
            AddHill(600f, -660f, 120f, 41f);
            AddHill(460f, 180f, 120f, 39f);
            AddHill(-860f, 200f, 120f, 38f);
            AddHill(-860f, -200f, 120f, 38f);
            AddHill(620f, 640f, 120f, 38f);
            AddHill(860f, 240f, 120f, 36f);

            // --- Zones aplanies : on ne construit pas sur une pente.
            AddFlat(Vector2.zero, cfg.marketRadius + 46f, 1f);
            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 p = cfg.FiefPosition(i);
                AddFlat(new Vector2(p.x, p.z), 96f, 1f);
            }

            // --- Zones de ressources : relief attenue, pas supprime.
            //     On veut des arbres a flanc de colline, pas un billard.
            if (cfg.zones != null)
            {
                for (int i = 0; i < cfg.zones.Count; i++)
                {
                    ResourceZone z = cfg.zones[i];
                    if (z == null) continue;
                    AddFlat(z.center, z.radius + 12f, 0.45f);
                }
            }

            // --- Couloirs aplanis le long des chemins.
            //     Une vraie route contourne ou entaille la colline, elle ne la gravit
            //     pas tout droit. Sans ca, le trajet marche-fief passait a 43 degres.
            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 p = cfg.FiefPosition(i);
                AddCorridor(Vector2.zero, new Vector2(p.x, p.z), 26f, 0.95f);
            }

            // --- Les lacs. Un bassin force le terrain a descendre a plat sous le niveau
            //     de l'eau : on obtient une cuvette propre et donc une rive nette.
            AddBasin(-250f, -25f, 48f, 16f);
            AddBasin(155f, 200f, 41f, 16f);
            AddBasin(365f, -325f, 44f, 16f);
            AddBasin(-250f, 455f, 58f, 16f);
            AddBasin(-250f, -445f, 61f, 16f);

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
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject("Terrain");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new Material[]
            {
                MaterialFactory.Get(Palette.Sand),
                MaterialFactory.Get(Palette.Grass1),
                MaterialFactory.Get(Palette.Grass2),
                MaterialFactory.Get(Palette.Grass3),
                MaterialFactory.Get(Palette.Grass4),
                MaterialFactory.Get(Palette.Scree),
                MaterialFactory.Get(Palette.Rock1),
                MaterialFactory.Get(Palette.Rock2),
                MaterialFactory.Get(Palette.Snow)
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

            int band;
            if (flatness < 0.62f)
            {
                band = n < 0.5f ? 6 : 7;                      // falaise
            }
            else if (height > 88f + n * 16f)
            {
                band = 8;                                      // neige des sommets
            }
            else if (height > 54f + n * 20f)
            {
                band = n < 0.45f ? 6 : 5;                      // roche et eboulis
            }
            else if (height < -2.2f + n * 1.4f)
            {
                band = 0;                                      // rivage des lacs
            }
            else
            {
                float g = Mathf.Clamp01(height / 54f) + n * 0.34f;
                if (g < 0.22f) band = 1;
                else if (g < 0.48f) band = 2;
                else if (g < 0.76f) band = 3;
                else band = 4;
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
