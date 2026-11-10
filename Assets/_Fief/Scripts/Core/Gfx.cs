using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES GRAPHISMES (v49 -- Martin : "je veux que tout soit magnifique, ameliore tout, de A a Z").
    ///
    /// Trois niveaux, dans Reglages ▸ Graphismes :
    ///   ULTRA   tout : les matieres a grain, le sol peint, l'herbe dense, le ciel a nuages, la mer
    ///           de nuages en relief, le halo des lumieres, les rayons du soleil, l'etalonnage,
    ///           l'ombre de contact (les coins et les pieds des murs s'assombrissent) ;
    ///   BEAU    pareil, sans l'ombre de contact, avec moins d'herbe et des ombres moins loin ;
    ///   RAPIDE  le jeu d'avant (le shader Standard d'Unity, pas de post-traitement).
    ///
    /// Concept Unity : un SHADER est le petit programme que la carte graphique execute pour
    /// chaque pixel d'un objet. Les notres sont dans Resources/Shaders (des fichiers texte) ;
    /// Unity les compile tout seul. Si l'un d'eux ne marche pas sur une machine, Find renvoie
    /// null et le jeu retombe sur le shader Standard : rien ne casse, c'est juste moins beau.
    /// </summary>
    public static class Gfx
    {
        public static int Quality { get { Settings.Load(); return Settings.Quality; } }
        public static bool Fancy { get { return Quality >= 1; } }
        public static bool Ultra { get { return Quality >= 2; } }
        public static readonly string[] QualityNames = { "Rapide", "Beau", "Ultra" };

        static readonly Dictionary<string, Shader> found = new Dictionary<string, Shader>();
        static Texture2D noise;

        /// <summary>Un de nos shaders, s'il existe, s'il marche ici, et si les graphismes ne sont pas sur "Rapide".</summary>
        public static Shader Find(string name)
        {
            if (!Fancy) return null;
            Shader s;
            if (found.TryGetValue(name, out s)) return s;
            s = Shader.Find(name);
            if (s != null && !s.isSupported)
            {
                Debug.LogWarning("[FIEF] Le shader " + name + " ne marche pas sur cette machine : on s'en passe.");
                s = null;
            }
            if (s == null) Debug.LogWarning("[FIEF] Shader introuvable : " + name + " (le jeu utilise l'ancien rendu pour cette matiere).");
            found[name] = s;
            return s;
        }

        /// <summary>
        /// A l'ouverture de chaque manche, avant de batir le monde : le BRUIT que tous nos shaders
        /// lisent (les plaques de l'herbe, le grain de la pierre, les nuages, le vent).
        /// </summary>
        public static void Prepare()
        {
            found.Clear();
            if (noise == null) noise = BuildNoise();
            Shader.SetGlobalTexture("_FiefNoise", noise);
            Shader.SetGlobalColor("_FiefCloudTint", Color.white);
            Shader.SetGlobalVector("_FiefPlayerPos", new Vector4(0f, -9999f, 0f, 0f));
        }

        // ------------------------------------------------------------------ le bruit

        const int Size = 256;

        /// <summary>
        /// Une texture de bruit qui se repete sans couture. Quatre couches, une par canal :
        /// R de grandes formes douces, V des formes moyennes, B un grain fin, A d'autres grandes formes.
        /// </summary>
        static Texture2D BuildNoise()
        {
            float[] r = Fbm(4, 5, 11), g = Fbm(7, 4, 23), b = Fbm(32, 2, 37), a = Fbm(5, 5, 59);
            Color[] px = new Color[Size * Size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(r[i], g[i], b[i], a[i]);
            Texture2D t = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true);
            t.name = "Fief_Bruit";
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.hideFlags = HideFlags.HideAndDontSave;
            t.SetPixels(px);
            t.Apply(true, true);
            return t;
        }

        /// <summary>Un bruit a plusieurs octaves, de periode "cells" (il boucle), etire entre 0 et 1.</summary>
        static float[] Fbm(int cells, int octaves, int seed)
        {
            float[] v = new float[Size * Size];
            float min = float.MaxValue, max = float.MinValue;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float sum = 0f, amp = 1f, total = 0f;
                    int period = cells;
                    for (int o = 0; o < octaves; o++)
                    {
                        sum += Value(x * period / (float)Size, y * period / (float)Size, period, seed + o * 131) * amp;
                        total += amp;
                        amp *= 0.5f;
                        period *= 2;
                    }
                    float n = sum / total;
                    v[y * Size + x] = n;
                    if (n < min) min = n;
                    if (n > max) max = n;
                }
            float span = Mathf.Max(0.0001f, max - min);
            for (int i = 0; i < v.Length; i++) v[i] = (v[i] - min) / span;
            return v;
        }

        static float Value(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Lattice(x0, y0, period, seed), b = Lattice(x0 + 1, y0, period, seed);
            float c = Lattice(x0, y0 + 1, period, seed), d = Lattice(x0 + 1, y0 + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Lattice(int x, int y, int period, int seed)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
