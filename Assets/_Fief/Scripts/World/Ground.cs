using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ILE (27/09 -- Martin : "il faut supprimer la foret"). Plus de sylve : une ILE
    /// FLOTTANTE au-dessus d'une mer de nuages. Dessus, la citadelle et sa tour ;
    /// dessous, le vide. Autour, six ILOTS qui flottent plus loin -- le Monument se
    /// pose sur l'un d'eux a chaque manche : on y va en PLANANT depuis la tour, ou
    /// tire par une arbaleste geante.
    ///
    /// Le dessus est plat (la citadelle en a besoin) ; le dessous est un gros rocher
    /// qui s'effile vers le bas, a facettes, comme tout le reste. Tomber de l'ile,
    /// c'est tomber dans les nuages : on reapparait a sa place de depart (Respawn).
    ///
    /// Concept Unity : un Mesh, c'est une liste de sommets + une liste de triangles.
    /// L'ORDRE des trois sommets d'un triangle decide de quel cote il se voit (l'autre
    /// cote est invisible). La fonction Tri() range les sommets dans le bon ordre
    /// pour que la face regarde vers l'exterieur -- sans elle, la moitie de l'ile
    /// serait transparente.
    /// </summary>
    public static class Ground
    {
        /// <summary>L'altitude du "sol" hors de l'ile : le vide.</summary>
        public const float Void = -500f;
        /// <summary>En dessous, on est tombe dans les nuages : on reapparait (et la Couronne rentre).</summary>
        public const float FallLine = -40f;
        /// <summary>Le rayon moyen de l'ile.</summary>
        public const float IslandRadius = 98f;

        static int seed = 1;
        static bool ready;
        public static bool Ready { get { return ready; } }

        // ================================================================== les ilots

        public struct Islet
        {
            public Vector3 Top;       // le centre du dessus
            public float Radius;
        }

        static readonly List<Islet> islets = new List<Islet>();
        public static int IsletCount { get { return islets.Count; } }
        public static Islet GetIslet(int i) { return islets[i]; }

        // ================================================================== la forme

        /// <summary>A appeler avant tout le reste : la forme de l'ile et la place des ilots.</summary>
        public static void Prepare(GameConfig cfg)
        {
            seed = cfg != null ? cfg.worldSeed : 1;
            islets.Clear();
            System.Random rng = new System.Random(seed * 17 + 3);
            float turn = (float)rng.NextDouble() * 360f;
            for (int i = 0; i < 6; i++)
            {
                float a = (turn + i * 60f + (float)(rng.NextDouble() - 0.5) * 22f) * Mathf.Deg2Rad;
                float d = 150f + (float)rng.NextDouble() * 26f;
                Islet it = new Islet();
                it.Top = new Vector3(Mathf.Cos(a) * d, 6f + (float)rng.NextDouble() * 24f, Mathf.Sin(a) * d);
                it.Radius = 11f + (float)rng.NextDouble() * 4f;
                islets.Add(it);
            }
            ready = true;
        }

        /// <summary>Le bord de l'ile dans la direction "angle" (radians) : jamais un cercle parfait.</summary>
        public static float EdgeAt(float angle)
        {
            float s = seed * 0.37f;
            return IslandRadius + Mathf.Sin(angle * 3f + s) * 5f + Mathf.Sin(angle * 7f + s * 2.1f) * 2.5f + Mathf.Sin(angle * 13f + s * 0.7f) * 1.2f;
        }

        public static bool OnIsland(float x, float z)
        {
            return new Vector2(x, z).magnitude <= EdgeAt(Mathf.Atan2(z, x));
        }

        /// <summary>Altitude du sol en (x, z) : 0 sur l'ile ou le dessus d'un ilot, le vide ailleurs.</summary>
        public static float Height(float x, float z)
        {
            if (OnIsland(x, z)) return 0f;
            for (int i = 0; i < islets.Count; i++)
            {
                Vector3 t = islets[i].Top;
                if (new Vector2(x - t.x, z - t.z).magnitude <= islets[i].Radius) return t.y;
            }
            return Void;
        }

        public static float Sample(float x, float z) { return Height(x, z); }

        /// <summary>Pose un point sur le sol. yOffset pour surelever legerement.</summary>
        public static Vector3 Place(float x, float z, float yOffset) { return new Vector3(x, Height(x, z) + yOffset, z); }
        public static Vector3 Place(Vector3 position, float yOffset) { return Place(position.x, position.z, yOffset); }

        /// <summary>Le dessus de l'ile est plat ; hors de l'ile, c'est "raide" (rien ne s'y pose).</summary>
        public static float Slope(float x, float z) { return Height(x, z) <= Void + 1f ? 1f : 0f; }

        // ================================================================== construction

        static readonly Color[] Grass =
        {
            new Color(0.34f, 0.46f, 0.21f), new Color(0.39f, 0.51f, 0.23f), new Color(0.30f, 0.42f, 0.19f),
            new Color(0.44f, 0.49f, 0.25f), new Color(0.47f, 0.41f, 0.27f)
        };
        static readonly Color[] Rock =
        {
            new Color(0.40f, 0.35f, 0.32f), new Color(0.33f, 0.29f, 0.27f), new Color(0.46f, 0.40f, 0.35f), new Color(0.27f, 0.24f, 0.24f)
        };

        public static GameObject Build(Transform parent, GameConfig cfg)
        {
            if (!ready) Prepare(cfg);
            GameObject root = new GameObject("ÎLE");
            root.transform.SetParent(parent, false);
            BuildRock(root.transform, "Île", Vector3.zero, IslandRadius, 90f, 96, 14, seed, true);
            for (int i = 0; i < islets.Count; i++)
                BuildRock(root.transform, "Îlot", islets[i].Top, islets[i].Radius, 26f + islets[i].Radius, 28, 5, seed * 31 + i, false);
            return root;
        }

        /// <summary>
        /// Un rocher volant : un dessus plat (herbe, avec collider) et un dessous qui
        /// s'effile en pointe (roche). "island" : la grande ile (son bord suit EdgeAt).
        /// </summary>
        static void BuildRock(Transform parent, string name, Vector3 centre, float radius, float depth, int segments, int rings, int noiseSeed, bool island)
        {
            List<Vector3> verts = new List<Vector3>();
            List<int>[] bands = new List<int>[Grass.Length + Rock.Length];
            for (int i = 0; i < bands.Length; i++) bands[i] = new List<int>();
            List<Vector3> colVerts = new List<Vector3>();
            List<int> colTris = new List<int>();

            // --- le bord, angle par angle
            float[] edge = new float[segments];
            for (int s = 0; s < segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                edge[s] = island ? EdgeAt(a) : radius * (1f + 0.08f * Mathf.Sin(a * 3f + noiseSeed));
            }

            // --- le dessus : des anneaux (le premier est le centre)
            Vector3[,] top = new Vector3[rings + 1, segments];
            for (int r = 0; r <= rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float f = r / (float)rings;
                    top[r, s] = centre + new Vector3(Mathf.Cos(a) * edge[s] * f, 0f, Mathf.Sin(a) * edge[s] * f);
                }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int n = (s + 1) % segments;
                    Vector3 a = top[r, s], b = top[r, n], c = top[r + 1, s], d = top[r + 1, n];
                    int band = GrassBand(a, noiseSeed);
                    if (r > 0) Tri(verts, bands[band], a, b, c, Vector3.up);
                    Tri(verts, bands[band], b, d, c, Vector3.up);
                    int k = colVerts.Count;
                    colVerts.Add(a); colVerts.Add(b); colVerts.Add(c); colVerts.Add(d);
                    if (r > 0) AddColliderTri(colVerts, colTris, k, k + 1, k + 2);
                    AddColliderTri(colVerts, colTris, k + 1, k + 3, k + 2);
                }

            // --- le dessous : le bord, une levre, puis la roche qui s'effile en pointe
            float[] levels = { 0f, -2.5f, -0.14f, -0.34f, -0.58f, -0.8f };      // (negatifs < -1 : fractions de la profondeur)
            float[] shrink = { 1f, 0.985f, 0.82f, 0.6f, 0.34f, 0.12f };
            int L = levels.Length;
            Vector3[,] under = new Vector3[L, segments];
            System.Random rng = new System.Random(noiseSeed * 7 + 1);
            for (int l = 0; l < L; l++)
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float y = levels[l] > -1f && levels[l] < 0f ? levels[l] * depth : levels[l];
                    float jitter = l < 2 ? 0f : ((float)rng.NextDouble() - 0.5f) * 0.12f;
                    float rr = edge[s] * (shrink[l] + jitter);
                    float jy = l < 2 ? 0f : ((float)rng.NextDouble() - 0.5f) * depth * 0.05f;
                    under[l, s] = centre + new Vector3(Mathf.Cos(a) * rr, y + jy, Mathf.Sin(a) * rr);
                }
            Vector3 tip = centre + Vector3.down * depth;
            for (int l = 0; l < L - 1; l++)
                for (int s = 0; s < segments; s++)
                {
                    int n = (s + 1) % segments;
                    Vector3 a = under[l, s], b = under[l, n], c = under[l + 1, s], d = under[l + 1, n];
                    Vector3 outward = ((a + b + c + d) * 0.25f - centre);
                    outward.y = -Mathf.Abs(outward.y) * 0.2f;
                    int band = Grass.Length + (l == 0 ? 0 : (s * 7 + l * 3 + noiseSeed) % Rock.Length);
                    Tri(verts, bands[band], a, b, c, outward);
                    Tri(verts, bands[band], b, d, c, outward);
                }
            for (int s = 0; s < segments; s++)
            {
                int n = (s + 1) % segments;
                Vector3 a = under[L - 1, s], b = under[L - 1, n];
                Tri(verts, bands[Grass.Length + (s % Rock.Length)], a, b, tip, ((a + b) * 0.5f - centre) + Vector3.down * 5f);
            }

            // --- l'objet
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            int used = 0;
            for (int i = 0; i < bands.Length; i++) if (bands[i].Count > 0) used++;
            mesh.subMeshCount = used;
            List<Material> mats = new List<Material>();
            int sub = 0;
            for (int i = 0; i < bands.Length; i++)
            {
                if (bands[i].Count == 0) continue;
                mesh.SetTriangles(bands[i], sub++);
                mats.Add(i < Grass.Length ? Surfaces.Ground(Grass[i]) : Surfaces.Rock(Rock[i - Grass.Length]));
            }
            Vector2[] uv = new Vector2[verts.Count];
            for (int k = 0; k < verts.Count; k++) uv[k] = new Vector2(verts[k].x + verts[k].y, verts[k].z - verts[k].y) / 3.5f;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();

            Mesh col = new Mesh();
            col.name = name + " (sol)";
            col.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            col.SetVertices(colVerts);
            col.SetTriangles(colTris, 0);
            col.RecalculateBounds();
            go.AddComponent<MeshCollider>().sharedMesh = col;
        }

        static int GrassBand(Vector3 p, int noiseSeed)
        {
            float n = Mathf.PerlinNoise(p.x * 0.035f + noiseSeed * 0.13f, p.z * 0.035f) * 0.85f + Noise(p.x, p.z) * 0.15f;
            return Mathf.Clamp(Mathf.FloorToInt(n * Grass.Length), 0, Grass.Length - 1);
        }

        /// <summary>Un triangle a facettes, range pour que sa face regarde vers "outward".</summary>
        static void Tri(List<Vector3> verts, List<int> band, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { Vector3 t = b; b = c; c = t; }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            band.Add(i); band.Add(i + 1); band.Add(i + 2);
        }

        /// <summary>Un triangle du collider, tourne vers le haut (on marche dessus).</summary>
        static void AddColliderTri(List<Vector3> v, List<int> tris, int a, int b, int c)
        {
            if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).y < 0f) { int t = b; b = c; c = t; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        /// <summary>Bruit bon marche et reproductible, entre 0 et 1.</summary>
        static float Noise(float x, float z)
        {
            float v = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }
    }
}
