using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES MATIERES : sol, ecorce, roche. Comme la maconnerie du chateau, elles sont
    /// fabriquees au lancement (aucun fichier) : une texture de grain, centree sur
    /// le blanc pour garder la teinte d'origine, et une carte de relief tiree de la
    /// meme "hauteur". Sous la lanterne, c'est ce relief qui fait qu'un sol est un
    /// sol et pas une moquette.
    ///
    ///   Sol      terre, petites pierres, aiguilles et brindilles, plaques de mousse
    ///   Ecorce   sillons verticaux, plaques, noeuds -- on la lit a hauteur d'yeux
    ///   Roche    grain fin, fissures, taches de lichen clair
    ///
    /// Une matiere se demande par couleur (Surfaces.Ground(c)...) ; les materiaux sont
    /// mis en cache : autant de materiaux que de teintes, jamais plus.
    /// </summary>
    public static class Surfaces
    {
        const int Size = 256;

        static Texture2D groundTex, groundBump, barkTex, barkBump, rockTex, rockBump;
        static readonly Dictionary<string, Material> Made = new Dictionary<string, Material>();

        public static Material Ground(Color c) { Ensure(); return Make("sol", c, groundTex, groundBump, 0.8f); }
        public static Material Bark(Color c) { Ensure(); return Make("écorce", c, barkTex, barkBump, 1.2f); }
        public static Material Rock(Color c) { Ensure(); return Make("roche", c, rockTex, rockBump, 1.0f); }

        static Material Make(string kind, Color c, Texture2D albedo, Texture2D bump, float bumpScale)
        {
            string key = kind + ColorUtility.ToHtmlStringRGB(c);
            Material mat;
            if (Made.TryGetValue(key, out mat) && mat != null) return mat;
            mat = new Material(MaterialFactory.Get(c));
            mat.name = "Fief_" + kind + "_" + ColorUtility.ToHtmlStringRGB(c);
            Color tint = c * 1.1f;
            tint.a = 1f;
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", bump);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            Made[key] = mat;
            return mat;
        }

        static void Ensure()
        {
            if (groundTex != null) return;
            float[] h = new float[Size * Size];

            // --- le sol
            System.Random rng = new System.Random(71);
            Color[] c = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float big = Tile(x, y, 0.035f, 3f);
                    float fine = Tile(x, y, 0.2f, 11f);
                    float v = 0.88f + big * 0.18f + (fine - 0.5f) * 0.16f;
                    h[y * Size + x] = big * 0.5f + fine * 0.35f;
                    c[y * Size + x] = new Color(v, v * 0.99f, v * 0.96f);
                }
            // Des petits cailloux clairs, des aiguilles sombres.
            for (int i = 0; i < 90; i++) Pebble(c, h, rng.Next(Size), rng.Next(Size), 2 + rng.Next(3), 1.12f);
            for (int i = 0; i < 140; i++) Needle(c, h, rng.Next(Size), rng.Next(Size), (float)rng.NextDouble() * Mathf.PI, 6 + rng.Next(10), 0.7f);
            groundTex = Albedo("sol", c);
            groundBump = Bump("sol_relief", h, 3.5f);

            // --- l'ecorce : des sillons verticaux qui ondulent, des plaques
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float wave = Mathf.Sin((x + Tile(x, y, 0.03f, 5f) * 40f) * (2f * Mathf.PI * 8f / Size));
                    float ridge = Mathf.Pow(Mathf.Abs(wave), 0.6f);
                    float plates = Tile(x, y * 0.25f, 0.08f, 17f);
                    float v = 0.72f + ridge * 0.34f + (plates - 0.5f) * 0.2f;
                    h[y * Size + x] = ridge * 0.8f + plates * 0.2f;
                    c[y * Size + x] = new Color(v, v * 0.98f, v * 0.95f);
                }
            // Quelques noeuds.
            for (int i = 0; i < 5; i++) Pebble(c, h, rng.Next(Size), rng.Next(Size), 5 + rng.Next(4), 0.62f);
            barkTex = Albedo("écorce", c);
            barkBump = Bump("écorce_relief", h, 4.5f);

            // --- la roche : grain, fissures, lichen
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float grain = Tile(x, y, 0.06f, 23f) * 0.6f + Tile(x, y, 0.25f, 29f) * 0.4f;
                    float v = 0.84f + (grain - 0.5f) * 0.3f;
                    h[y * Size + x] = grain;
                    c[y * Size + x] = new Color(v, v, v * 1.01f);
                }
            for (int i = 0; i < 12; i++) Needle(c, h, rng.Next(Size), rng.Next(Size), (float)rng.NextDouble() * Mathf.PI, 20 + rng.Next(30), 0.55f);
            for (int i = 0; i < 25; i++) Lichen(c, rng.Next(Size), rng.Next(Size), 4 + rng.Next(8));
            rockTex = Albedo("roche", c);
            rockBump = Bump("roche_relief", h, 3f);
        }

        /// <summary>Un bruit de Perlin qui se repete sans couture sur la texture.</summary>
        static float Tile(float x, float y, float freq, float seed)
        {
            float u = x / Size, v = y / Size;
            float a = Mathf.PerlinNoise(x * freq + seed, y * freq + seed);
            float b = Mathf.PerlinNoise((x - Size) * freq + seed, y * freq + seed);
            float cc = Mathf.PerlinNoise(x * freq + seed, (y - Size) * freq + seed);
            float d = Mathf.PerlinNoise((x - Size) * freq + seed, (y - Size) * freq + seed);
            float top = Mathf.Lerp(a, b, u), bottom = Mathf.Lerp(cc, d, u);
            return Mathf.Lerp(top, bottom, v);
        }

        static int Wrap(int i) { return ((i % Size) + Size) % Size; }

        static void Pebble(Color[] c, float[] h, int cx, int cy, int r, float shade)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y) / r;
                    if (d > 1f) continue;
                    int i = Wrap(cy + y) * Size + Wrap(cx + x);
                    c[i] = c[i] * Mathf.Lerp(shade, 1f, d * d);
                    h[i] += (1f - d * d) * 0.6f;
                }
        }

        static void Needle(Color[] c, float[] h, int cx, int cy, float angle, int length, float shade)
        {
            float dx = Mathf.Cos(angle), dy = Mathf.Sin(angle);
            for (int k = 0; k < length; k++)
            {
                int i = Wrap(Mathf.RoundToInt(cy + dy * k)) * Size + Wrap(Mathf.RoundToInt(cx + dx * k));
                c[i] = c[i] * shade;
                h[i] -= 0.3f;
            }
        }

        static void Lichen(Color[] c, int cx, int cy, int r)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y > r * r) continue;
                    if (Mathf.PerlinNoise((cx + x) * 0.3f, (cy + y) * 0.3f) < 0.45f) continue;
                    int i = Wrap(cy + y) * Size + Wrap(cx + x);
                    c[i] = Color.Lerp(c[i], new Color(1.15f, 1.18f, 0.9f), 0.35f);
                }
        }

        static Texture2D Albedo(string name, Color[] c)
        {
            Texture2D t = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false);
            t.name = "Fief_" + name;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels(c);
            t.Apply(true, true);
            return t;
        }

        /// <summary>La carte de relief, tiree de la "hauteur" (meme recette que la maconnerie).</summary>
        static Texture2D Bump(string name, float[] h, float strength)
        {
            Color[] n = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float gx = (h[y * Size + Wrap(x + 1)] - h[y * Size + Wrap(x - 1)]) * strength;
                    float gy = (h[Wrap(y + 1) * Size + x] - h[Wrap(y - 1) * Size + x]) * strength;
                    Vector3 v = new Vector3(-gx, -gy, 1f).normalized;
                    n[y * Size + x] = new Color(v.x * 0.5f + 0.5f, v.y * 0.5f + 0.5f, v.z * 0.5f + 0.5f, 1f);
                }
            Texture2D t = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true);
            t.name = "Fief_" + name;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels(n);
            t.Apply(true, true);
            return t;
        }
    }
}
