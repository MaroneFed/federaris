using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Styles et helpers de dessin pour l'interface.
    ///
    /// CHOIX ASSUME : l'interface de la Phase 1 est en IMGUI (OnGUI). C'est le systeme
    /// d'UI historique d'Unity : moche par defaut, mais il ne demande AUCUN asset,
    /// aucune police a importer, aucun Canvas a configurer. Objectif Phase 1 = valider
    /// la boucle de jeu, pas faire du beau. On refera le HUD en UI Toolkit quand la
    /// Porte 1 sera franchie (c'est note dans docs/v2-ideas.md).
    /// </summary>
    public static class UiStyle
    {
        public static float Scale = 1f;

        public static GUIStyle Title;
        public static GUIStyle Head;
        public static GUIStyle Label;
        public static GUIStyle Small;
        public static GUIStyle Value;
        public static GUIStyle Button;
        public static GUIStyle Centered;

        public static readonly Color PanelBg = new Color(0.09f, 0.09f, 0.11f, 0.94f);
        public static readonly Color PanelEdge = new Color(0.85f, 0.72f, 0.38f, 0.85f);
        public static readonly Color BarBg = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color Ink = new Color(0.94f, 0.93f, 0.89f);
        public static readonly Color InkDim = new Color(0.68f, 0.67f, 0.63f);

        static readonly Dictionary<Color, Texture2D> Textures = new Dictionary<Color, Texture2D>();
        static float builtScale = -1f;

        public static int S(float v) { return Mathf.RoundToInt(v * Scale); }

        /// <summary>A appeler au debut de chaque OnGUI : construit les styles une seule fois.</summary>
        public static void Ensure()
        {
            Scale = Mathf.Clamp(Screen.height / 900f, 0.85f, 2.2f);
            if (Mathf.Abs(Scale - builtScale) < 0.001f && Title != null) return;
            builtScale = Scale;

            Title = Make(GUI.skin.label, 24, FontStyle.Bold, Palette.Gold);
            Head = Make(GUI.skin.label, 17, FontStyle.Bold, Ink);
            Label = Make(GUI.skin.label, 15, FontStyle.Normal, Ink);
            Small = Make(GUI.skin.label, 12, FontStyle.Normal, InkDim);
            Value = Make(GUI.skin.label, 20, FontStyle.Bold, Ink);

            Centered = Make(GUI.skin.label, 15, FontStyle.Normal, Ink);
            Centered.alignment = TextAnchor.MiddleCenter;

            Button = new GUIStyle(GUI.skin.button);
            Button.fontSize = S(14);
            Button.fontStyle = FontStyle.Bold;
            Button.padding = new RectOffset(S(8), S(8), S(5), S(5));
        }

        static GUIStyle Make(GUIStyle from, int size, FontStyle style, Color color)
        {
            GUIStyle s = new GUIStyle(from);
            s.fontSize = S(size);
            s.fontStyle = style;
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.wordWrap = false;
            return s;
        }

        public static Texture2D Solid(Color color)
        {
            Texture2D tex;
            if (Textures.TryGetValue(color, out tex) && tex != null) return tex;

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            Textures[color] = tex;
            return tex;
        }

        public static void Fill(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Solid(color));
        }

        /// <summary>Fond + liseré doré : le "cadre" commun a tous les panneaux.</summary>
        public static void Frame(Rect rect)
        {
            float e = Mathf.Max(1f, 2f * Scale);
            Fill(new Rect(rect.x - e, rect.y - e, rect.width + e * 2f, rect.height + e * 2f), PanelEdge);
            Fill(rect, PanelBg);
        }

        public static void Bar(Rect rect, float fill01, Color fill, Color background)
        {
            Fill(rect, background);
            float w = Mathf.Clamp01(fill01) * rect.width;
            if (w > 0.5f) Fill(new Rect(rect.x, rect.y, w, rect.height), fill);
        }

        /// <summary>Texte avec ombre portee : reste lisible sur n'importe quel fond.</summary>
        public static void Shadowed(Rect rect, string text, GUIStyle style)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = original;
            GUI.Label(rect, text, style);
        }
    }
}
