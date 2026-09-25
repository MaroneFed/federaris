using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA MACONNERIE. Un mur de chateau n'est pas un bloc gris uni : ce sont des
    /// pierres taillees, posees en assises, jointoyees au mortier, chacune d'un ton
    /// un peu different. C'est CA qui manquait (Martin : "le chateau est horrible").
    ///
    /// Apres la construction du chateau, on repasse sur chaque cube de pierre et on
    /// lui donne :
    ///   - une TEXTURE d'appareil (pierres, joints, nuances, quelques pierres
    ///     moussues), fabriquee ici au lancement -- aucun fichier ;
    ///   - une CARTE DE RELIEF (normal map) : la lumiere rasante accroche les
    ///     joints, et le mur prend du relief sans un triangle de plus ;
    ///   - des coordonnees de texture a L'ECHELLE du mur : une pierre fait toujours
    ///     ~1,3 m, que le mur mesure 2 m ou 80 m.
    ///
    /// Concept Unity : les UV. Chaque sommet d'un maillage porte une coordonnee
    /// (u, v) qui dit quel point de la texture y est colle. Le cube d'Unity colle
    /// la texture une fois par face, quelle que soit sa taille : un mur de 80 m
    /// aurait eu des pierres de 20 m. On fabrique donc un cube dont les UV suivent
    /// les metres.
    /// </summary>
    public static class Masonry
    {
        /// <summary>Metres couverts par une repetition de la texture (4 assises de 80 cm).</summary>
        const float Tile = 3.2f;
        const int Size = 512;
        const int Courses = 4;

        static Texture2D albedo;
        static Texture2D relief;
        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        static readonly Dictionary<Vector3Int, Mesh> Meshes = new Dictionary<Vector3Int, Mesh>();

        /// <summary>Ce qui garde sa couleur unie : statues, bois, fer, objets.</summary>
        static readonly string[] Spared =
        {
            "Roi de pierre", "Sainte", "Mannequin", "Caisse de fer", "Charrette", "Étendard",
            "TRÔNE", "POTERNE", "LE REGISTRE", "Vantail", "Cage", "Arbre mort"
        };

        public static int Apply(Transform root)
        {
            if (root == null) return 0;
            Mesh cube = Proto.SharedMesh(PrimitiveType.Cube);
            if (cube == null) return 0;
            if (albedo == null) BuildTextures();

            int count = 0;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer r = renderers[i];
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh != cube) continue;
                Material m = r.sharedMaterial;
                if (m == null || m.IsKeywordEnabled("_EMISSION")) continue;
                if (!Stony(m.color)) continue;

                Vector3 s = r.transform.lossyScale;
                s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                if (Mathf.Min(s.x, Mathf.Min(s.y, s.z)) < 0.3f) continue;     // petits objets : unis
                if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < 1.2f) continue;
                if (Excluded(r.transform, root)) continue;

                mf.sharedMesh = MeshFor(s);
                r.sharedMaterial = MaterialFor(m.color);

                // Chaque mur commence a un endroit different de la texture : sans ca,
                // tous les pans voisins auraient exactement les memes pierres.
                Vector3 p = r.transform.position;
                block.Clear();
                block.SetVector("_MainTex_ST", new Vector4(1f, 1f, Frac(p.x * 0.137f + p.z * 0.291f), Frac(p.y * 0.173f + p.z * 0.117f + p.x * 0.07f)));
                r.SetPropertyBlock(block);
                count++;
            }
            return count;
        }

        static float Frac(float v) { return v - Mathf.Floor(v); }

        /// <summary>De la pierre : un gris (peu de couleur), ni trop sombre ni trop clair.</summary>
        static bool Stony(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max - min < 0.075f && max > 0.15f && max < 0.55f;
        }

        static bool Excluded(Transform t, Transform root)
        {
            while (t != null && t != root)
            {
                string n = t.name;
                for (int k = 0; k < Spared.Length; k++) if (n.StartsWith(Spared[k])) return true;
                t = t.parent;
            }
            return false;
        }

        static Material MaterialFor(Color c)
        {
            Material mat;
            if (Materials.TryGetValue(c, out mat) && mat != null) return mat;
            mat = new Material(MaterialFactory.Get(c));
            mat.name = "Fief_Pierre_" + ColorUtility.ToHtmlStringRGB(c);
            // La texture est centree sur 1 (un peu moins a cause des joints) : on
            // eclaircit d'un rien pour garder la teinte d'origine.
            Color tint = c * 1.08f;
            tint.a = 1f;
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", relief);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1.1f);
                mat.EnableKeyword("_NORMALMAP");
            }
            Materials[c] = mat;
            return mat;
        }

        /// <summary>Un cube de 1 x 1 x 1 dont les UV suivent les metres de l'objet qui le porte.</summary>
        static Mesh MeshFor(Vector3 scale)
        {
            Vector3Int key = new Vector3Int(Mathf.RoundToInt(scale.x * 4f), Mathf.RoundToInt(scale.y * 4f), Mathf.RoundToInt(scale.z * 4f));
            Mesh mesh;
            if (Meshes.TryGetValue(key, out mesh) && mesh != null) return mesh;

            Vector3 s = new Vector3(key.x / 4f, key.y / 4f, key.z / 4f);
            Mesh source = Proto.SharedMesh(PrimitiveType.Cube);
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            Vector2[] uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                Vector2 w;
                if (a.x > 0.5f) w = new Vector2(v[i].z * s.z * Mathf.Sign(n[i].x), v[i].y * s.y);
                else if (a.z > 0.5f) w = new Vector2(-v[i].x * s.x * Mathf.Sign(n[i].z), v[i].y * s.y);
                else w = new Vector2(v[i].x * s.x, v[i].z * s.z);
                uv[i] = w / Tile;
            }
            mesh = new Mesh();
            mesh.name = "Pierre_" + key.x + "x" + key.y + "x" + key.z;
            mesh.vertices = v;
            mesh.normals = n;
            mesh.uv = uv;
            mesh.triangles = source.triangles;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Meshes[key] = mesh;
            return mesh;
        }

        // ================================================================== les textures

        /// <summary>
        /// L'appareil : quatre assises, trois pierres chacune (longueurs tirees au
        /// hasard, decalees d'une assise a l'autre). Pour chaque pixel on sait a quelle
        /// distance il est du joint le plus proche : c'est ce qui donne a la fois le
        /// mortier (distance 0), le chanfrein des aretes, et le relief.
        /// </summary>
        static void BuildTextures()
        {
            System.Random rng = new System.Random(1187);
            int courseHeight = Size / Courses;

            // Les joints verticaux de chaque assise, sur un cercle (la texture se
            // repete : la derniere pierre rejoint la premiere).
            int[][] joints = new int[Courses][];
            float[][] tone = new float[Courses][];
            bool[][] moss = new bool[Courses][];
            for (int c = 0; c < Courses; c++)
            {
                int stones = 3;
                float[] lengths = new float[stones];
                float total = 0f;
                for (int k = 0; k < stones; k++) { lengths[k] = 0.75f + (float)rng.NextDouble() * 0.6f; total += lengths[k]; }
                int start = rng.Next(Size);
                joints[c] = new int[stones];
                float at = start;
                for (int k = 0; k < stones; k++)
                {
                    joints[c][k] = Mathf.RoundToInt(at) % Size;
                    at += lengths[k] / total * Size;
                }
                tone[c] = new float[stones];
                moss[c] = new bool[stones];
                for (int k = 0; k < stones; k++)
                {
                    tone[c][k] = 0.84f + (float)rng.NextDouble() * 0.3f;
                    moss[c][k] = rng.NextDouble() < 0.14;
                }
            }

            float[] height = new float[Size * Size];
            Color[] colours = new Color[Size * Size];
            const float Mortar = 5f;        // demi-largeur du joint, en pixels
            const float Bevel = 9f;         // largeur du chanfrein

            for (int y = 0; y < Size; y++)
            {
                int c = y / courseHeight;
                int yin = y - c * courseHeight;
                float dy = Mathf.Min(yin, courseHeight - yin);
                for (int x = 0; x < Size; x++)
                {
                    // Distance au joint vertical le plus proche (en tournant), et quelle pierre.
                    float dx = Size;
                    int stone = 0;
                    int best = int.MaxValue;
                    for (int k = 0; k < joints[c].Length; k++)
                    {
                        int d = x - joints[c][k];
                        if (d < 0) d += Size;
                        if (d < best) { best = d; stone = k; }
                        int e = Mathf.Abs(x - joints[c][k]);
                        e = Mathf.Min(e, Size - e);
                        if (e < dx) dx = e;
                    }
                    float edge = Mathf.Min(dx, dy);

                    // Du grain : deux octaves de bruit, et un peu d'usure sur les aretes.
                    float grain = Mathf.PerlinNoise(x * 0.045f, y * 0.045f) * 0.6f + Mathf.PerlinNoise(x * 0.19f + 31f, y * 0.19f + 7f) * 0.4f;
                    float chip = Mathf.PerlinNoise(x * 0.08f + 90f, y * 0.08f + 50f);
                    float h;
                    Color col;
                    if (edge < Mortar + (chip - 0.5f) * 3f)
                    {
                        h = 0f;
                        float mv = 0.5f + grain * 0.12f;
                        col = new Color(mv, mv, mv * 0.97f);
                    }
                    else
                    {
                        float bevel = Mathf.Clamp01((edge - Mortar) / Bevel);
                        h = 0.55f + 0.45f * Mathf.SmoothStep(0f, 1f, bevel) + (grain - 0.5f) * 0.18f;
                        float v = tone[c][stone] * (0.9f + grain * 0.2f) * Mathf.Lerp(0.86f, 1f, bevel);
                        col = new Color(v, v, v * 0.98f);
                        if (moss[c][stone])
                        {
                            // De la mousse qui gagne par le bas de la pierre.
                            float lower = 1f - Mathf.Clamp01(yin / (float)courseHeight);
                            float m = Mathf.Clamp01((grain - 0.35f) * 2.2f) * Mathf.Clamp01(lower * 1.4f);
                            col = Color.Lerp(col, new Color(v * 0.62f, v * 0.82f, v * 0.5f), m * 0.8f);
                        }
                    }
                    height[y * Size + x] = h;
                    colours[y * Size + x] = col;
                }
            }

            albedo = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false);
            albedo.name = "Fief_Appareil";
            albedo.wrapMode = TextureWrapMode.Repeat;
            albedo.filterMode = FilterMode.Trilinear;
            albedo.anisoLevel = 4;
            albedo.SetPixels(colours);
            albedo.Apply(true, true);

            // Le relief : la pente de la "hauteur" dans les deux sens. Encode a la
            // facon d'Unity pour une normal map (x et y ramenes de [-1,1] a [0,1]).
            Color[] normals = new Color[Size * Size];
            const float Strength = 5f;
            for (int y = 0; y < Size; y++)
            {
                int yu = (y + 1) % Size, yd = (y + Size - 1) % Size;
                for (int x = 0; x < Size; x++)
                {
                    int xr = (x + 1) % Size, xl = (x + Size - 1) % Size;
                    float gx = (height[y * Size + xr] - height[y * Size + xl]) * Strength;
                    float gy = (height[yu * Size + x] - height[yd * Size + x]) * Strength;
                    Vector3 nrm = new Vector3(-gx, -gy, 1f).normalized;
                    normals[y * Size + x] = new Color(nrm.x * 0.5f + 0.5f, nrm.y * 0.5f + 0.5f, nrm.z * 0.5f + 0.5f, 1f);
                }
            }
            relief = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true);
            relief.name = "Fief_Appareil_Relief";
            relief.wrapMode = TextureWrapMode.Repeat;
            relief.filterMode = FilterMode.Trilinear;
            relief.anisoLevel = 4;
            relief.SetPixels(normals);
            relief.Apply(true, true);
        }
    }
}
