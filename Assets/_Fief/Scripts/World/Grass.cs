using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'HERBE ET LES FLEURS (v49). L'ile etait un tapis plat. Elle est maintenant couverte de
    /// dizaines de milliers de touffes d'herbe qui ondulent au vent et s'ecartent sous tes pieds,
    /// et de fleurs (blanches, jaunes, coquelicots) par taches.
    ///
    /// Comment en dessiner autant sans ralentir : L'INSTANCIATION. Il n'y a qu'UN maillage de
    /// touffe (six brins) ; on donne a la carte graphique une liste de positions, et elle le
    /// dessine a toutes ces places d'un seul coup (Graphics.DrawMeshInstanced, 1023 a la fois).
    /// L'ile est decoupee en cases de 14 m : on ne dessine que celles qui sont pres de la camera.
    /// Les touffes ne sont pas des objets Unity (pas de GameObject) : elles ne coutent rien a
    /// la physique, on marche a travers.
    ///
    /// Ou il en pousse : sur le dessus de l'ile et des ilots, jamais dans l'enceinte, ni sous un
    /// mur, un piege, un sanctuaire (on demande a la physique s'il y a quelque chose : CheckSphere),
    /// ni dans les rivieres.
    /// </summary>
    public class Grass : MonoBehaviour
    {
        const float Cell = 14f;
        const int Batch = 1023;

        sealed class Patch
        {
            public Vector3 Centre;
            public readonly List<Matrix4x4[]> Blades = new List<Matrix4x4[]>();
            public readonly List<Matrix4x4[]>[] Flowers = { new List<Matrix4x4[]>(), new List<Matrix4x4[]>(), new List<Matrix4x4[]>() };
        }

        readonly List<Patch> patches = new List<Patch>();
        Mesh tuft;
        Mesh bloom;
        Material grassMat;
        readonly Material[] flowerMats = new Material[3];
        float range;

        static readonly float[] Rivers = { 52f, 142f, 232f, 322f };

        public static void Build(Transform root)
        {
            Shader shader = Gfx.Find("Fief/Grass");
            if (shader == null || !SystemInfo.supportsInstancing) return;
            GameObject go = new GameObject("HERBE");
            go.transform.SetParent(root, false);
            Grass g = go.AddComponent<Grass>();
            g.Plant(shader);
        }

        void Plant(Shader shader)
        {
            range = Gfx.Ultra ? 95f : 70f;
            float spacing = Gfx.Ultra ? 0.75f : 1.05f;
            tuft = TuftMesh(5, 0.36f, 0.5f, 0.06f, 0f, 4401);
            bloom = TuftMesh(3, 0.2f, 0.4f, 0.022f, 0.07f, 977);
            grassMat = new Material(shader);
            grassMat.name = "Fief_Herbe";
            grassMat.enableInstancing = true;
            Color[] petals = { new Color(1f, 0.98f, 0.92f), new Color(1f, 0.82f, 0.22f), new Color(0.95f, 0.26f, 0.22f) };
            for (int i = 0; i < 3; i++)
            {
                flowerMats[i] = new Material(shader);
                flowerMats[i].name = "Fief_Fleurs_" + i;
                flowerMats[i].enableInstancing = true;
                flowerMats[i].SetColor("_TipColor", petals[i]);
                flowerMats[i].SetFloat("_TipSharp", 1f);
            }

            Physics.SyncTransforms();
            Dictionary<long, List<Matrix4x4>> blades = new Dictionary<long, List<Matrix4x4>>();
            Dictionary<long, List<Matrix4x4>>[] flowers = { new Dictionary<long, List<Matrix4x4>>(), new Dictionary<long, List<Matrix4x4>>(), new Dictionary<long, List<Matrix4x4>>() };
            System.Random rng = new System.Random(90210);

            // --- la grande ile
            float reach = Ground.IslandRadius + 9f;
            for (float x = -reach; x <= reach; x += spacing)
                for (float z = -reach; z <= reach; z += spacing)
                {
                    float px = x + ((float)rng.NextDouble() - 0.5f) * spacing;
                    float pz = z + ((float)rng.NextDouble() - 0.5f) * spacing;
                    float pick = (float)rng.NextDouble();
                    float turn = (float)rng.NextDouble() * 360f;
                    float size = 0.75f + (float)rng.NextDouble() * 0.7f;
                    if (!IslandSpot(px, pz)) continue;
                    Add(px, 0f, pz, pick, turn, size, blades, flowers);
                }
            // --- les ilots (pas au centre : le Monument, le sanctuaire ou l'arbaleste y sont)
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Ground.Islet it = Ground.GetIslet(i);
                for (float x = -it.Radius; x <= it.Radius; x += spacing)
                    for (float z = -it.Radius; z <= it.Radius; z += spacing)
                    {
                        float px = it.Top.x + x + ((float)rng.NextDouble() - 0.5f) * spacing;
                        float pz = it.Top.z + z + ((float)rng.NextDouble() - 0.5f) * spacing;
                        float pick = (float)rng.NextDouble();
                        float turn = (float)rng.NextDouble() * 360f;
                        float size = 0.75f + (float)rng.NextDouble() * 0.7f;
                        float d = new Vector2(px - it.Top.x, pz - it.Top.z).magnitude;
                        if (d > it.Radius - 1.2f || d < 6.5f) continue;
                        if (Blocked(px, it.Top.y, pz)) continue;
                        Add(px, it.Top.y, pz, pick, turn, size, blades, flowers);
                    }
            }

            int total = 0;
            foreach (KeyValuePair<long, List<Matrix4x4>> kv in blades)
            {
                Patch p = new Patch();
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < kv.Value.Count; i++) sum += (Vector3)kv.Value[i].GetColumn(3);
                p.Centre = sum / Mathf.Max(1, kv.Value.Count);
                Split(kv.Value, p.Blades);
                for (int f = 0; f < 3; f++)
                {
                    List<Matrix4x4> list;
                    if (flowers[f].TryGetValue(kv.Key, out list)) Split(list, p.Flowers[f]);
                }
                patches.Add(p);
                total += kv.Value.Count;
            }
            Debug.Log("[FIEF] Herbe : " + total + " touffes en " + patches.Count + " cases.");
        }

        static bool IslandSpot(float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z);
            float a = Mathf.Atan2(z, x);
            float edge = Ground.EdgeAt(a);
            if (d > edge - 1.3f) return false;
            if (Castle.Covers(x, z, 1.5f)) return false;
            // Les quatre rivieres et leur source.
            for (int i = 0; i < Rivers.Length; i++)
            {
                float ra = Rivers[i] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ra), Mathf.Sin(ra));
                float redge = Ground.EdgeAt(ra);
                float along = x * dir.x + z * dir.y;
                float side = Mathf.Abs(x * dir.y - z * dir.x);
                if (along > redge - 17f && side < 2.5f) return false;
                Vector2 pool = dir * (redge - 16f);
                if (new Vector2(x - pool.x, z - pool.y).magnitude < 4.1f) return false;
            }
            return !Blocked(x, 0f, z);
        }

        /// <summary>Quelque chose de solide est pose ici (mur, piege, sanctuaire, arbaleste...) : pas d'herbe dessous.</summary>
        static bool Blocked(float x, float y, float z)
        {
            return Physics.CheckSphere(new Vector3(x, y + 0.75f, z), 0.4f, ~0, QueryTriggerInteraction.Ignore);
        }

        void Add(float x, float y, float z, float pick, float turn, float size, Dictionary<long, List<Matrix4x4>> blades, Dictionary<long, List<Matrix4x4>>[] flowers)
        {
            long key = ((long)Mathf.FloorToInt(x / Cell) << 20) ^ (long)(Mathf.FloorToInt(z / Cell) & 0xFFFFF) ^ ((long)Mathf.RoundToInt(y) << 44);
            // Les fleurs poussent par taches (un bruit lent decide ou), une touffe sur dix dans la tache.
            float meadow = Mathf.PerlinNoise(x * 0.045f + 31.7f, z * 0.045f + 5.3f);
            int kind = -1;
            if (meadow > 0.58f && pick < 0.16f) kind = Mathf.PerlinNoise(x * 0.02f + 3.1f, z * 0.02f + 77.7f) < 0.42f ? 0 : Mathf.PerlinNoise(x * 0.027f + 60f, z * 0.027f + 9f) < 0.5f ? 1 : 2;
            Matrix4x4 m = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0f, turn, 0f), new Vector3(size, size * (0.8f + pick * 0.6f), size));
            List<Matrix4x4> list;
            if (!blades.TryGetValue(key, out list)) { list = new List<Matrix4x4>(); blades[key] = list; }
            list.Add(m);
            if (kind < 0) return;
            if (!flowers[kind].TryGetValue(key, out list)) { list = new List<Matrix4x4>(); flowers[kind][key] = list; }
            list.Add(Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0f, turn, 0f), Vector3.one * (0.9f + pick * 2f)));
        }

        static void Split(List<Matrix4x4> all, List<Matrix4x4[]> into)
        {
            for (int i = 0; i < all.Count; i += Batch)
            {
                int n = Mathf.Min(Batch, all.Count - i);
                Matrix4x4[] part = new Matrix4x4[n];
                all.CopyTo(i, part, 0, n);
                into.Add(part);
            }
        }

        /// <summary>
        /// Une touffe : "count" brins plantes en rond, chacun un ruban qui s'affine vers la pointe
        /// et se courbe. La coordonnee V de la texture dit la hauteur dans le brin (0 pied, 1 pointe) :
        /// le shader s'en sert pour le vent et pour la couleur.
        /// </summary>
        static Mesh TuftMesh(int count, float spread, float height, float width, float head, int seed)
        {
            System.Random rng = new System.Random(seed);
            List<Vector3> v = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            for (int b = 0; b < count; b++)
            {
                float a = (b + (float)rng.NextDouble() * 0.7f) / count * Mathf.PI * 2f;
                float r = spread * (0.25f + (float)rng.NextDouble() * 0.75f);
                Vector3 foot = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                float face = (float)rng.NextDouble() * Mathf.PI;
                Vector3 side = new Vector3(Mathf.Cos(face), 0f, Mathf.Sin(face));
                Vector3 lean = new Vector3(-side.z, 0f, side.x) * (0.1f + (float)rng.NextDouble() * 0.22f);
                float h = height * (0.65f + (float)rng.NextDouble() * 0.6f);
                int start = v.Count;
                for (int k = 0; k < 3; k++)
                {
                    float f = k / 3f;
                    Vector3 mid = foot + Vector3.up * (h * f) + lean * (f * f);
                    float half = width * (1f - f * 0.75f);
                    v.Add(mid - side * half); uv.Add(new Vector2(0f, f));
                    v.Add(mid + side * half); uv.Add(new Vector2(1f, f));
                }
                v.Add(foot + Vector3.up * h + lean); uv.Add(new Vector2(0.5f, 1f));
                for (int k = 0; k < 2; k++)
                {
                    int i = start + k * 2;
                    t.Add(i); t.Add(i + 2); t.Add(i + 1);
                    t.Add(i + 1); t.Add(i + 2); t.Add(i + 3);
                }
                t.Add(start + 4); t.Add(start + 6); t.Add(start + 5);
                // Une fleur : une corolle (deux losanges croises) au bout de la tige.
                if (head > 0f)
                {
                    Vector3 top = foot + Vector3.up * h + lean;
                    Vector3 other = new Vector3(-side.z, 0f, side.x);
                    for (int q = 0; q < 2; q++)
                    {
                        Vector3 across = q == 0 ? side : other;
                        int c = v.Count;
                        v.Add(top - across * head); uv.Add(new Vector2(0f, 1f));
                        v.Add(top + Vector3.up * head * 0.9f); uv.Add(new Vector2(0.5f, 1f));
                        v.Add(top + across * head); uv.Add(new Vector2(1f, 1f));
                        v.Add(top - Vector3.up * head * 0.5f); uv.Add(new Vector2(0.5f, 1f));
                        t.Add(c); t.Add(c + 1); t.Add(c + 2);
                        t.Add(c); t.Add(c + 2); t.Add(c + 3);
                    }
                }
            }
            Mesh m = new Mesh();
            m.name = "Touffe";
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            // Les sommets bougent avec le vent : une boite large, pour qu'Unity ne cache pas la touffe trop tot.
            m.bounds = new Bounds(new Vector3(0f, height * 0.6f, 0f), new Vector3(2.5f, height * 2.5f, 2.5f));
            return m;
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || grassMat == null) return;
            Transform player = Game.PlayerTransform;
            Shader.SetGlobalVector("_FiefPlayerPos", player != null ? (Vector4)player.position : new Vector4(0f, -9999f, 0f, 0f));
            Vector3 eye = cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            float far = range * range;
            for (int i = 0; i < patches.Count; i++)
            {
                Patch p = patches[i];
                Vector3 d = p.Centre - eye;
                float dist = d.sqrMagnitude;
                if (dist > far) continue;
                // Derriere la camera (sauf la case ou l'on se tient) : inutile.
                if (dist > Cell * Cell * 2.3f && Vector3.Dot(d, fwd) < -Cell) continue;
                for (int k = 0; k < p.Blades.Count; k++)
                    Graphics.DrawMeshInstanced(tuft, 0, grassMat, p.Blades[k], p.Blades[k].Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, 0, null, UnityEngine.Rendering.LightProbeUsage.Off);
                // Les fleurs, de plus pres seulement.
                if (dist > far * 0.45f) continue;
                for (int f = 0; f < 3; f++)
                    for (int k = 0; k < p.Flowers[f].Count; k++)
                        Graphics.DrawMeshInstanced(bloom, 0, flowerMats[f], p.Flowers[f][k], p.Flowers[f][k].Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, 0, null, UnityEngine.Rendering.LightProbeUsage.Off);
            }
        }
    }
}
