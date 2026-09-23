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
        static readonly Dictionary<Color, Material> Solids = new Dictionary<Color, Material>();
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
            // Filet de securite : les teintes proches partagent un materiau.
            // Sans ca, du code qui tire des couleurs au hasard cree des milliers
            // de materiaux et le lancement s'effondre. Arrondi perceptuel : voir
            // Palette.QuantizeFine, l'arrondi lineaire ecrasait les sombres.
            color = Palette.QuantizeFine(color, 40);

            Material mat;
            if (Solids.TryGetValue(color, out mat) && mat != null) return mat;

            mat = new Material(LitShader);
            mat.name = "Fief_" + ColorUtility.ToHtmlStringRGB(color);
            mat.color = color;

            // Look mat, sans reflets : on veut du low-poly lisible, pas du plastique.
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            // Le decor represente des centaines d'objets : l'instanciation GPU permet
            // de les dessiner en un seul appel par couleur au lieu d'un par objet.
            mat.enableInstancing = true;

            Solids[color] = mat;
            return mat;
        }

        /// <summary>
        /// Un materiau TRANSPARENT (pour l'eau). Le shader Standard doit etre bascule
        /// en mode transparent a la main : c'est la recette officielle d'Unity.
        /// </summary>
        public static Material GetTransparent(Color color)
        {
            Material mat = new Material(LitShader);
            mat.name = "FiefTransparent_" + ColorUtility.ToHtmlStringRGB(color);
            mat.color = color;

            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);   // URP
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.72f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.72f);
            return mat;
        }

        static readonly Dictionary<string, Material> Glows = new Dictionary<string, Material>();

        /// <summary>
        /// Un materiau qui LUIT : flammes, runes, pierres-lune, le mage. Il ne projette
        /// pas de lumiere sur les voisins (c'est le role d'une Light), il brille pour
        /// lui-meme, et il reste visible dans la penombre ou tout le reste s'eteint.
        ///
        /// NOTE POUR PLUS TARD : dans l'editeur, tout marche. Dans un jeu exporte, Unity
        /// retire les variantes de shader qu'aucun materiau du projet n'utilise -- et
        /// comme nos materiaux sont crees par code, la variante "_EMISSION" pourrait
        /// manquer. A regler au moment de faire un build (voir docs/v2-ideas.md).
        /// </summary>
        public static Material GetGlow(Color color, float intensity)
        {
            color = Palette.QuantizeFine(color, 40);
            string key = ColorUtility.ToHtmlStringRGB(color) + "_" + intensity.ToString("0.0");

            Material mat;
            if (Glows.TryGetValue(key, out mat) && mat != null) return mat;

            mat = new Material(LitShader);
            mat.name = "FiefGlow_" + key;
            Color body = new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 1f);
            mat.color = body;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", body);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);

            Color emission = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.enableInstancing = true;

            Glows[key] = mat;
            return mat;
        }

        public static void Clear()
        {
            Solids.Clear();
            Glows.Clear();
            cachedShader = null;
        }
    }
}
