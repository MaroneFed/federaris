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

        static float mapSize = 400f;
        static float rimHeight = 26f;
        static bool ready;

        public static bool Ready { get { return ready; } }

        // ------------------------------------------------------------------ relief

        /// <summary>
        /// Definit les collines et les zones a aplanir. A appeler AVANT de placer
        /// quoi que ce soit dans le monde.
        /// </summary>
        public static void Prepare(GameConfig cfg)
        {
            hills.Clear();
            flats.Clear();
            corridors.Clear();
            mapSize = cfg.mapSize;

            // --- Les collines, placees a la main. Deplace-les, change leur taille :
            //     la carte entiere suit au prochain lancement.
            AddHill(-58f, 168f, 62f, 21f);
            AddHill(48f, 178f, 54f, 17f);
            AddHill(158f, 92f, 58f, 19f);
            AddHill(172f, -42f, 50f, 16f);
            AddHill(96f, -168f, 60f, 20f);
            AddHill(-34f, -178f, 52f, 15f);
            AddHill(-162f, -104f, 56f, 18f);
            AddHill(-176f, 44f, 54f, 22f);

            // Collines interieures : c'est ELLES qu'on voit en jouant. Leurs positions
            // ont ete choisies pour ne jamais mordre sur une zone de jeu aplanie.
            AddHill(-23f, 58f, 40f, 17.0f);
            AddHill(61f, -8f, 40f, 17.0f);
            AddHill(-23f, -56f, 40f, 17.0f);
            AddHill(-56f, 49f, 40f, 16.3f);
            AddHill(70f, 25f, 40f, 15.8f);
            AddHill(-56f, -47f, 40f, 15.6f);
            AddHill(-2f, -83f, 33f, 12.5f);
            AddHill(-104f, 118f, 32f, 12.3f);
            AddHill(73f, -41f, 32f, 12.2f);
            AddHill(52f, -149f, 32f, 12.2f);
            AddHill(103f, -119f, 32f, 12.0f);
            AddHill(-50f, 148f, 32f, 12.0f);
            AddHill(4f, 85f, 31f, 11.8f);
            AddHill(-38f, 19f, 31f, 11.7f);

            // --- Zones aplanies : on ne construit pas sur une pente.
            AddFlat(Vector2.zero, cfg.marketRadius + 12f, 1f);
            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 p = cfg.FiefPosition(i);
                AddFlat(new Vector2(p.x, p.z), 34f, 1f);
            }

            // --- Zones de ressources : relief attenue, pas supprime.
            //     On veut des arbres a flanc de colline, pas un billard.
            if (cfg.zones != null)
            {
                for (int i = 0; i < cfg.zones.Count; i++)
                {
                    ResourceZone z = cfg.zones[i];
                    if (z == null) continue;
                    AddFlat(z.center, z.radius + 4f, 0.45f);
                }
            }

            // --- Couloirs aplanis le long des chemins.
            //     Une vraie route contourne ou entaille la colline, elle ne la gravit
            //     pas tout droit. Sans ca, le trajet marche-fief passait a 43 degres.
            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 p = cfg.FiefPosition(i);
                AddCorridor(Vector2.zero, new Vector2(p.x, p.z), 11f, 0.85f);
            }

            ready = true;
        }

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
            if (edge > 0.70f)
            {
                float t = Mathf.Clamp01((edge - 0.70f) / 0.30f);
                h += rimHeight * t * t;
            }

            // Ondulation de fond : evite l'effet "collines posees sur une table".
            h += Mathf.Sin(x * 0.021f) * Mathf.Cos(z * 0.019f) * 1.6f;

            return h * FlattenFactor(x, z);
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

                // 42 m de transition : c'est large, mais c'est ce qui evite la marche
                // d'escalier a la sortie d'un fief. Reduis-le et les bords deviennent raides.
                float local = Mathf.Clamp01((d - f.radius) / 42f);
                local = Mathf.Lerp(1f - f.strength, 1f, local);
                if (local < factor) factor = local;
            }

            for (int i = 0; i < corridors.Count; i++)
            {
                FlatCorridor c = corridors[i];
                float d = DistanceToSegment(new Vector2(x, z), c.a, c.b);

                float local = Mathf.Clamp01((d - c.radius) / 22f);
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

        /// <summary>Pose un point sur le sol. yOffset pour surelever legerement.</summary>
        public static Vector3 Place(float x, float z, float yOffset)
        {
            return new Vector3(x, Height(x, z) + yOffset, z);
        }

        public static Vector3 Place(Vector3 position, float yOffset)
        {
            return new Vector3(position.x, Height(position.x, position.z) + yOffset, position.z);
        }

        /// <summary>Inclinaison du sol, de 0 (plat) a 1 (falaise). Sert a semer le decor.</summary>
        public static float Slope(float x, float z)
        {
            const float step = 2f;
            float hx = Height(x + step, z) - Height(x - step, z);
            float hz = Height(x, z + step) - Height(x, z - step);
            return Mathf.Clamp01(Mathf.Sqrt(hx * hx + hz * hz) / (2f * step));
        }

        // ------------------------------------------------------------------ maillage

        /// <summary>Construit le maillage du terrain et son collider.</summary>
        public static GameObject Build(Transform parent, GameConfig cfg)
        {
            const float cell = 4f;
            int steps = Mathf.RoundToInt(cfg.mapSize / cell);
            int side = steps + 1;
            float half = cfg.mapSize * 0.5f;

            Vector3[] vertices = new Vector3[side * side];
            Vector2[] uv = new Vector2[side * side];

            for (int iz = 0; iz < side; iz++)
            {
                for (int ix = 0; ix < side; ix++)
                {
                    float x = -half + ix * cell;
                    float z = -half + iz * cell;
                    int index = iz * side + ix;
                    vertices[index] = new Vector3(x, Height(x, z), z);
                    uv[index] = new Vector2((float)ix / steps, (float)iz / steps);
                }
            }

            // 3 sous-maillages = 3 materiaux : herbe, herbe sombre, roche.
            List<int> grass = new List<int>();
            List<int> dark = new List<int>();
            List<int> rock = new List<int>();

            for (int iz = 0; iz < steps; iz++)
            {
                for (int ix = 0; ix < steps; ix++)
                {
                    int a = iz * side + ix;
                    int b = a + 1;
                    int c = a + side;
                    int d = c + 1;
                    AddTriangle(vertices, grass, dark, rock, a, c, b);
                    AddTriangle(vertices, grass, dark, rock, b, c, d);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "TerrainFief";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(grass, 0);
            mesh.SetTriangles(dark, 1);
            mesh.SetTriangles(rock, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject("Terrain");
            go.transform.SetParent(parent, false);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[]
            {
                MaterialFactory.Get(Palette.Grass),
                MaterialFactory.Get(Palette.GrassDark),
                MaterialFactory.Get(Palette.Cliff)
            };

            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            return go;
        }

        /// <summary>Range un triangle dans le bon materiau selon sa hauteur et sa pente.</summary>
        static void AddTriangle(Vector3[] vertices, List<int> grass, List<int> dark, List<int> rock,
                                int i0, int i1, int i2)
        {
            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            float flatness = Mathf.Abs(normal.y);
            float height = (v0.y + v1.y + v2.y) / 3f;

            List<int> target;
            if (flatness < 0.80f || height > 17f) target = rock;
            else if (height > 4.5f) target = dark;
            else target = grass;

            target.Add(i0);
            target.Add(i1);
            target.Add(i2);
        }
    }
}
