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
        static float rimHeight = 48f;
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

            // --- Les grandes chaines de bordure. Elles ferment l'horizon et donnent
            //     son echelle au monde : on voit des montagnes loin devant en marchant.
            AddHill(-116f, 336f, 124f, 46f);
            AddHill(96f, 356f, 108f, 40f);
            AddHill(316f, 184f, 116f, 44f);
            AddHill(344f, -84f, 100f, 38f);
            AddHill(192f, -336f, 120f, 46f);
            AddHill(-68f, -356f, 104f, 36f);
            AddHill(-324f, -208f, 112f, 42f);
            AddHill(-352f, 88f, 108f, 48f);

            // --- Collines interieures : c'est ELLES qu'on voit en jouant. Leurs positions
            //     ont ete choisies pour ne jamais mordre sur une zone de jeu aplanie.
            AddHill(-64f, 104f, 72f, 34.0f);
            AddHill(122f, 14f, 72f, 34.0f);
            AddHill(-64f, -100f, 72f, 34.0f);
            AddHill(146f, -46f, 72f, 34.0f);
            AddHill(104f, -322f, 72f, 34.0f);
            AddHill(-28f, 158f, 72f, 34.0f);
            AddHill(230f, -250f, 72f, 34.0f);
            AddHill(-28f, -154f, 72f, 34.0f);
            AddHill(-130f, 98f, 72f, 33.5f);
            AddHill(-130f, -100f, 72f, 33.2f);
            AddHill(-328f, -76f, 72f, 33.1f);
            AddHill(-328f, 80f, 72f, 33.0f);
            AddHill(92f, 326f, 72f, 31.0f);
            AddHill(236f, 242f, 72f, 30.8f);
            AddHill(152f, 74f, 72f, 30.0f);
            AddHill(326f, -94f, 72f, 29.2f);
            AddHill(80f, -34f, 72f, 28.7f);
            AddHill(326f, 92f, 72f, 28.4f);
            AddHill(170f, -292f, 72f, 28.3f);
            AddHill(-82f, -328f, 72f, 28.3f);
            AddHill(-244f, -232f, 72f, 28.1f);
            AddHill(-4f, 86f, 71f, 28.0f);

            // --- Zones aplanies : on ne construit pas sur une pente.
            AddFlat(Vector2.zero, cfg.marketRadius + 22f, 1f);
            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 p = cfg.FiefPosition(i);
                AddFlat(new Vector2(p.x, p.z), 52f, 1f);
            }

            // --- Zones de ressources : relief attenue, pas supprime.
            //     On veut des arbres a flanc de colline, pas un billard.
            if (cfg.zones != null)
            {
                for (int i = 0; i < cfg.zones.Count; i++)
                {
                    ResourceZone z = cfg.zones[i];
                    if (z == null) continue;
                    AddFlat(z.center, z.radius + 6f, 0.45f);
                }
            }

            // --- Couloirs aplanis le long des chemins.
            //     Une vraie route contourne ou entaille la colline, elle ne la gravit
            //     pas tout droit. Sans ca, le trajet marche-fief passait a 43 degres.
            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 p = cfg.FiefPosition(i);
                AddCorridor(Vector2.zero, new Vector2(p.x, p.z), 14f, 0.85f);
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
            h += Mathf.Sin(x * 0.013f) * Mathf.Cos(z * 0.011f) * 2.6f;

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

                // 56 m de transition : c'est large, mais c'est ce qui evite la marche
                // d'escalier a la sortie d'un fief. Reduis-le et les bords deviennent raides.
                float local = Mathf.Clamp01((d - f.radius) / 56f);
                local = Mathf.Lerp(1f - f.strength, 1f, local);
                if (local < factor) factor = local;
            }

            for (int i = 0; i < corridors.Count; i++)
            {
                FlatCorridor c = corridors[i];
                float d = DistanceToSegment(new Vector2(x, z), c.a, c.b);

                float local = Mathf.Clamp01((d - c.radius) / 30f);
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
            const float cell = 5f;
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
            if (flatness < 0.80f || height > 30f) target = rock;
            else if (height > 9f) target = dark;
            else target = grass;

            target.Add(i0);
            target.Add(i1);
            target.Add(i2);
        }
    }
}
