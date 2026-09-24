using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE TAPIS DE LA FORET, autour de toi seulement.
    ///
    /// En premiere personne avec 14 m de brume, ce qu'on regarde le plus, c'est le
    /// SOL. Un sol uni dit "decor" ; un sol jonche de feuilles mortes, de brindilles
    /// et de champignons dit "foret". Mais en mettre partout sur 700 x 700 m, ce
    /// serait des millions d'objets.
    ///
    /// L'astuce : le sol est decoupe en cases de 8 m. Seules les cases autour de toi
    /// (7 x 7) ont leur tapis ; quand tu avances, les cases qui sortent sont
    /// recyclees pour celles qui entrent. Chaque case tire son contenu de ses
    /// coordonnees : repasse au meme endroit, tu retrouves les memes feuilles.
    ///
    /// Une case = UN seul objet et un seul maillage (feuilles, brindilles,
    /// champignons ensemble), avec un sous-maillage par couleur. Et, rarement, des
    /// champignons qui LUISENT faiblement dans le noir.
    /// </summary>
    public class GroundCover : MonoBehaviour
    {
        const float Cell = 8f;
        const int Reach = 3;                    // 3 cases de chaque cote : 7 x 7

        // Sous-maillages, dans l'ordre des materiaux.
        const int LeafBrown = 0, LeafOchre = 1, LeafRed = 2, Wood = 3, CapPale = 4, CapRed = 5, Glow = 6, Parts = 7;

        class Patch
        {
            public GameObject go;
            public Mesh mesh;
            public Vector2Int cell;
        }

        readonly Dictionary<Vector2Int, Patch> live = new Dictionary<Vector2Int, Patch>();
        readonly Stack<Patch> spare = new Stack<Patch>();
        Material[] materials;
        Transform player;
        Vector2Int centre = new Vector2Int(int.MinValue, int.MinValue);
        int seed;
        float half;

        // Tampons reutilises : pas d'allocation a chaque case.
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<int>[] tris = new List<int>[Parts];

        public static GroundCover Build(Transform parent, Transform player, GameConfig cfg)
        {
            GameObject go = new GameObject("TAPIS DE FORÊT");
            go.transform.SetParent(parent, false);
            GroundCover g = go.AddComponent<GroundCover>();
            g.player = player;
            g.seed = cfg != null ? cfg.worldSeed : 1;
            g.half = (cfg != null ? cfg.mapSize : 700f) * 0.5f;
            for (int i = 0; i < Parts; i++) g.tris[i] = new List<int>();
            g.materials = new Material[]
            {
                MaterialFactory.Get(new Color(0.30f, 0.21f, 0.13f)),
                MaterialFactory.Get(new Color(0.42f, 0.31f, 0.14f)),
                MaterialFactory.Get(new Color(0.36f, 0.15f, 0.10f)),
                MaterialFactory.Get(new Color(0.24f, 0.19f, 0.14f)),
                MaterialFactory.Get(new Color(0.66f, 0.60f, 0.50f)),
                MaterialFactory.Get(new Color(0.55f, 0.16f, 0.12f)),
                MaterialFactory.GetGlow(new Color(0.45f, 0.95f, 0.8f), 1.6f)
            };
            return g;
        }

        void Update()
        {
            if (player == null) return;
            Vector3 p = player.position;
            Vector2Int here = new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
            if (here == centre) return;
            centre = here;

            // Ce qui est trop loin retourne a la reserve.
            List<Vector2Int> gone = null;
            foreach (KeyValuePair<Vector2Int, Patch> kv in live)
            {
                Vector2Int c = kv.Key;
                if (Mathf.Abs(c.x - here.x) > Reach || Mathf.Abs(c.y - here.y) > Reach)
                {
                    if (gone == null) gone = new List<Vector2Int>();
                    gone.Add(c);
                }
            }
            if (gone != null)
                for (int i = 0; i < gone.Count; i++)
                {
                    Patch old = live[gone[i]];
                    old.go.SetActive(false);
                    spare.Push(old);
                    live.Remove(gone[i]);
                }

            // Ce qui manque, on le tisse.
            for (int dx = -Reach; dx <= Reach; dx++)
                for (int dz = -Reach; dz <= Reach; dz++)
                {
                    Vector2Int c = new Vector2Int(here.x + dx, here.y + dz);
                    if (live.ContainsKey(c)) continue;
                    Patch patch = spare.Count > 0 ? spare.Pop() : NewPatch();
                    patch.cell = c;
                    Weave(patch);
                    patch.go.SetActive(true);
                    live[c] = patch;
                }
        }

        Patch NewPatch()
        {
            Patch patch = new Patch();
            patch.go = new GameObject("Tapis");
            patch.go.transform.SetParent(transform, false);
            patch.mesh = new Mesh();
            patch.mesh.name = "Tapis";
            patch.go.AddComponent<MeshFilter>().sharedMesh = patch.mesh;
            MeshRenderer r = patch.go.AddComponent<MeshRenderer>();
            r.sharedMaterials = materials;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return patch;
        }

        // ================================================================== une case

        void Weave(Patch patch)
        {
            verts.Clear();
            for (int i = 0; i < Parts; i++) tris[i].Clear();

            float x0 = patch.cell.x * Cell, z0 = patch.cell.y * Cell;
            System.Random rng = new System.Random(seed * 73856093 ^ patch.cell.x * 19349663 ^ patch.cell.y * 83492791);
            bool inside = Mathf.Abs(x0) < half && Mathf.Abs(z0) < half;
            bool paved = Castle.Covers(x0 + Cell * 0.5f, z0 + Cell * 0.5f, 2f);

            if (inside && !paved)
            {
                // Les feuilles mortes : par plaques, pas en pluie reguliere.
                int drifts = 2 + rng.Next(3);
                for (int d = 0; d < drifts; d++)
                {
                    float cx = x0 + R(rng, 0f, Cell), cz = z0 + R(rng, 0f, Cell);
                    int leaves = 10 + rng.Next(14);
                    for (int i = 0; i < leaves; i++)
                    {
                        float a = R(rng, 0f, Mathf.PI * 2f), r = Mathf.Sqrt((float)rng.NextDouble()) * 1.6f;
                        Leaf(cx + Mathf.Cos(a) * r, cz + Mathf.Sin(a) * r, rng);
                    }
                }
                // Des brindilles.
                int twigs = 3 + rng.Next(5);
                for (int i = 0; i < twigs; i++) Twig(x0 + R(rng, 0f, Cell), z0 + R(rng, 0f, Cell), rng);
                // Des champignons, en ronds de sorciere ou en touffes.
                if (rng.NextDouble() < 0.55)
                {
                    bool glow = rng.NextDouble() < 0.22;
                    int part = glow ? Glow : rng.NextDouble() < 0.3 ? CapRed : CapPale;
                    float cx = x0 + R(rng, 0.5f, Cell - 0.5f), cz = z0 + R(rng, 0.5f, Cell - 0.5f);
                    int n = 3 + rng.Next(5);
                    for (int i = 0; i < n; i++)
                    {
                        float a = R(rng, 0f, Mathf.PI * 2f), r = R(rng, 0.05f, 0.45f);
                        Mushroom(cx + Mathf.Cos(a) * r, cz + Mathf.Sin(a) * r, R(rng, 0.6f, 1.3f), part, rng);
                    }
                }
            }

            Mesh mesh = patch.mesh;
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.subMeshCount = Parts;
            for (int i = 0; i < Parts; i++) mesh.SetTriangles(tris[i], i, false);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        static float R(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        /// <summary>Une feuille : un losange a peine bombe, pose a plat, un bord souleve.</summary>
        void Leaf(float x, float z, System.Random rng)
        {
            float y = Ground.Sample(x, z) + 0.015f + (float)rng.NextDouble() * 0.01f;
            float len = R(rng, 0.09f, 0.17f), wide = len * R(rng, 0.45f, 0.65f);
            float a = R(rng, 0f, Mathf.PI * 2f);
            Vector3 f = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 s = new Vector3(-f.z, 0f, f.x);
            Vector3 c = new Vector3(x, y, z);
            Vector3 tip = c + f * len + Vector3.up * R(rng, 0f, 0.04f);
            Vector3 stem = c - f * len;
            Vector3 l = c + s * wide + Vector3.up * 0.012f;
            Vector3 r = c - s * wide + Vector3.up * 0.012f;
            double pick = rng.NextDouble();
            int part = pick < 0.55 ? LeafBrown : pick < 0.85 ? LeafOchre : LeafRed;
            Tri(stem, l, tip, part);
            Tri(stem, tip, r, part);
        }

        /// <summary>Une brindille : un baton fin couche, deux faces visibles.</summary>
        void Twig(float x, float z, System.Random rng)
        {
            float y = Ground.Sample(x, z) + 0.02f;
            float len = R(rng, 0.25f, 0.8f), w = R(rng, 0.012f, 0.025f);
            float a = R(rng, 0f, Mathf.PI * 2f);
            Vector3 f = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * len * 0.5f;
            Vector3 s = new Vector3(-f.z, 0f, f.x).normalized * w;
            Vector3 c = new Vector3(x, y, z);
            Vector3 up = Vector3.up * w * 1.6f;
            Vector3 a0 = c - f, a1 = c + f + Vector3.up * R(rng, 0f, 0.05f);
            Quad(a0 - s, a0 + up, a1 + up, a1 - s, Wood);
            Quad(a0 + up, a0 + s, a1 + s, a1 + up, Wood);
            // Un rameau lateral, une fois sur deux.
            if (rng.NextDouble() < 0.5)
            {
                Vector3 b0 = Vector3.Lerp(a0, a1, R(rng, 0.3f, 0.7f));
                Vector3 b1 = b0 + (s.normalized * (rng.NextDouble() < 0.5 ? 1f : -1f) + f.normalized) * len * 0.25f;
                Quad(b0 - s * 0.7f, b0 + up, b1 + up, b1, Wood);
            }
        }

        /// <summary>Un champignon : un pied a quatre faces et un chapeau en cone ecrase.</summary>
        void Mushroom(float x, float z, float size, int capPart, System.Random rng)
        {
            float y = Ground.Sample(x, z);
            float h = 0.07f * size, stem = 0.012f * size, cap = R(rng, 0.035f, 0.055f) * size;
            Vector3 b = new Vector3(x, y, z);
            Vector3 lean = new Vector3(R(rng, -0.015f, 0.015f), 0f, R(rng, -0.015f, 0.015f)) * size;
            Vector3 top = b + Vector3.up * h + lean;
            int stemPart = capPart == Glow ? Glow : CapPale;
            const int sides = 5;
            Vector3[] foot = new Vector3[sides], neck = new Vector3[sides], rim = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                Vector3 d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                foot[i] = b + d * stem * 1.2f;
                neck[i] = top + d * stem - Vector3.up * 0.005f;
                rim[i] = top + d * cap - Vector3.up * cap * 0.35f;
            }
            Vector3 crown = top + Vector3.up * cap * 0.55f;
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Quad(foot[i], foot[j], neck[j], neck[i], stemPart);        // pied
                Tri(rim[i], rim[j], crown, capPart);                        // chapeau, dessus
                Tri(top, rim[j], rim[i], capPart == Glow ? Glow : CapPale); // lamelles, dessous
            }
        }

        // Les faces regardent du cote ou (b - a) x (c - a) pointe ; pour les feuilles
        // et le dessus des choses, c'est vers le haut. Le sens est verifie par la
        // normale : si elle pointe vers le bas alors qu'on la voulait en haut, on inverse.
        void Tri(Vector3 a, Vector3 b, Vector3 c, int part)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris[part].Add(i); tris[part].Add(i + 1); tris[part].Add(i + 2);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int part)
        {
            Tri(a, b, c, part);
            Tri(a, c, d, part);
        }
    }
}
