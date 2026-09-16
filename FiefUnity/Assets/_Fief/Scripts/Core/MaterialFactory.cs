using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Fabrique (et met en cache) un materiau mat par couleur.
    ///
    /// Pourquoi ce detour : le shader n'a pas le meme nom selon le pipeline de rendu.
    /// On essaie URP d'abord, puis le Built-in. Consequence : passer le projet
    /// en URP plus tard ne casse RIEN, aucun materiau n'est stocke en asset.
    /// </summary>
    public static class MaterialFactory
    {
        static readonly Dictionary<Color, Material> Cache = new Dictionary<Color, Material>();
        static Shader cachedShader;

        static Shader LitShader
        {
            get
            {
                if (cachedShader == null) cachedShader = Shader.Find("Universal Render Pipeline/Lit");
                if (cachedShader == null) cachedShader = Shader.Find("Standard");
                if (cachedShader == null) cachedShader = Shader.Find("Legacy Shaders/Diffuse");
                if (cachedShader == null) cachedShader = Shader.Find("Sprites/Default");
                return cachedShader;
            }
        }

        public static Material Get(Color color)
        {
            Material mat;
            if (Cache.TryGetValue(color, out mat) && mat != null) return mat;

            mat = new Material(LitShader);
            mat.name = "Fief_" + ColorUtility.ToHtmlStringRGB(color);
            mat.color = color;

            // Look mat, sans reflets : on veut du low-poly lisible, pas du plastique.
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            Cache[color] = mat;
            return mat;
        }

        public static void Clear()
        {
            Cache.Clear();
            cachedShader = null;
        }
    }
}
