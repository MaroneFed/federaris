using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// La bibliotheque graphique de l'interface.
    ///
    /// Tout est fabrique par le code : les panneaux arrondis, leurs ombres, les boutons
    /// et leurs etats (normal / survol / appuye), les jauges, les pastilles. Aucune image
    /// importee, aucun Canvas a configurer.
    ///
    /// Concept Unity : une Texture2D est une grille de pixels qu'on peut ecrire a la main.
    /// Un GUIStyle etire cette texture en "9 tranches" (les 4 coins restent intacts,
    /// les bords et le centre s'etirent) : un seul petit carre arrondi sert donc de fond
    /// a un bouton de n'importe quelle taille.
    /// </summary>
    public static class UiStyle
    {
        public static float Scale = 1f;

        // --- styles de texte
        public static GUIStyle Title;
        public static GUIStyle Head;
        public static GUIStyle Label;
        public static GUIStyle Small;
        public static GUIStyle Tiny;
        public static GUIStyle Value;
        public static GUIStyle Big;
        public static GUIStyle Centered;
        public static GUIStyle CenteredSmall;

        // --- styles de widgets
        public static GUIStyle Button;
        public static GUIStyle ButtonPrimary;
        public static GUIStyle ButtonGhost;
        public static GUIStyle PanelBox;
        public static GUIStyle CardBox;

        // --- couleurs
        public static readonly Color Ink = new Color(0.95f, 0.94f, 0.90f);
        public static readonly Color InkDim = new Color(0.69f, 0.68f, 0.64f);
        public static readonly Color InkFaint = new Color(0.47f, 0.47f, 0.44f);
        public static readonly Color Panel = new Color(0.075f, 0.077f, 0.092f, 0.97f);
        public static readonly Color Card = new Color(0.115f, 0.118f, 0.138f, 0.98f);
        public static readonly Color Edge = new Color(0.30f, 0.29f, 0.33f, 1f);
        public static readonly Color EdgeGold = new Color(0.74f, 0.61f, 0.30f, 1f);
        public static readonly Color BarBg = new Color(0.02f, 0.02f, 0.03f, 0.75f);
        public static readonly Color Scrim = new Color(0.03f, 0.03f, 0.045f, 0.82f);

        // --- ancien nom conserve pour compatibilite
        public static readonly Color PanelBg = new Color(0.075f, 0.077f, 0.092f, 0.97f);
        public static readonly Color PanelEdge = new Color(0.74f, 0.61f, 0.30f, 1f);

        static readonly Dictionary<Color, Texture2D> Solids = new Dictionary<Color, Texture2D>();
        static Texture2D panelTex, cardTex, shadowTex, btnTex, btnHoverTex, btnActiveTex;
        static Texture2D primaryTex, primaryHoverTex, ghostHoverTex, pillTex;
        static Font uiFont;
        static float builtScale = -1f;

        public static int S(float v) { return Mathf.RoundToInt(v * Scale); }

        // ==================================================================
        //  construction
        // ==================================================================

        public static void Ensure()
        {
            Scale = Mathf.Clamp(Screen.height / 900f, 0.8f, 2.4f);
            if (Mathf.Abs(Scale - builtScale) < 0.001f && Title != null) return;
            builtScale = Scale;

            BuildTextures();
            BuildFont();
            BuildStyles();
        }

        static void BuildFont()
        {
            if (uiFont != null) return;
            // Une police systeme plus soignee que celle par defaut. Si aucune n'existe,
            // Unity retombe sur sa police interne : on ne risque rien.
            string[] wanted = { "Trebuchet MS", "Optima", "Georgia", "Segoe UI", "Helvetica Neue", "Arial" };
            for (int i = 0; i < wanted.Length; i++)
            {
                try
                {
                    Font f = Font.CreateDynamicFontFromOSFont(wanted[i], 16);
                    if (f != null) { uiFont = f; return; }
                }
                catch (System.Exception)
                {
                    // Police absente de ce systeme : on essaie la suivante.
                }
            }
        }

        static void BuildTextures()
        {
            if (panelTex != null) return;

            int r = 10;
            panelTex = Rounded(r, Panel, Edge, 1.4f);
            cardTex = Rounded(r, Card, new Color(0.26f, 0.26f, 0.30f, 1f), 1.2f);
            btnTex = Rounded(7, new Color(0.17f, 0.175f, 0.205f, 1f), new Color(0.33f, 0.33f, 0.38f, 1f), 1.2f);
            btnHoverTex = Rounded(7, new Color(0.24f, 0.245f, 0.285f, 1f), new Color(0.55f, 0.50f, 0.36f, 1f), 1.4f);
            btnActiveTex = Rounded(7, new Color(0.13f, 0.13f, 0.155f, 1f), new Color(0.74f, 0.61f, 0.30f, 1f), 1.4f);
            primaryTex = Rounded(7, new Color(0.62f, 0.49f, 0.17f, 1f), new Color(0.88f, 0.74f, 0.36f, 1f), 1.4f);
            primaryHoverTex = Rounded(7, new Color(0.78f, 0.63f, 0.24f, 1f), new Color(1f, 0.88f, 0.48f, 1f), 1.6f);
            ghostHoverTex = Rounded(7, new Color(1f, 1f, 1f, 0.07f), new Color(0.6f, 0.55f, 0.4f, 0.8f), 1.2f);
            pillTex = Rounded(9, new Color(1f, 1f, 1f, 0.06f), new Color(1f, 1f, 1f, 0.14f), 1f);
            shadowTex = Shadow(26);
        }

        /// <summary>
        /// Un carre arrondi dessine pixel par pixel, avec bord et anticrenelage.
        /// On mesure la distance de chaque pixel au rectangle arrondi : negative dedans,
        /// positive dehors. Le bord est la fine bande autour de zero.
        /// </summary>
        static Texture2D Rounded(int radius, Color fill, Color border, float borderWidth)
        {
            int size = radius * 2 + 6;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float half = size * 0.5f;
            float inner = half - radius;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - half;
                    float py = y + 0.5f - half;
                    float dx = Mathf.Max(Mathf.Abs(px) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(py) - inner, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                    float shape = Mathf.Clamp01(0.5f - d);
                    float edge = Mathf.Clamp01(0.5f - Mathf.Abs(d + borderWidth * 0.5f) + borderWidth * 0.5f);

                    Color c = Color.Lerp(fill, border, Mathf.Clamp01(edge));
                    c.a *= shape;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Un halo sombre et flou : pose sous un panneau, il le decolle du jeu.</summary>
        static Texture2D Shadow(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float half = size * 0.5f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = Mathf.Pow(1f - d, 2.4f) * 0.55f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static void BuildStyles()
        {
            Title = Text(30, FontStyle.Bold, Palette.Gold);
            Big = Text(46, FontStyle.Bold, Palette.Gold);
            Head = Text(18, FontStyle.Bold, Ink);
            Label = Text(15, FontStyle.Normal, Ink);
            Small = Text(13, FontStyle.Normal, InkDim);
            Tiny = Text(11, FontStyle.Normal, InkFaint);
            Value = Text(22, FontStyle.Bold, Ink);

            Centered = Text(15, FontStyle.Normal, Ink);
            Centered.alignment = TextAnchor.MiddleCenter;
            CenteredSmall = Text(12, FontStyle.Normal, InkDim);
            CenteredSmall.alignment = TextAnchor.MiddleCenter;

            Button = Widget(btnTex, btnHoverTex, btnActiveTex, 14, Ink);
            ButtonPrimary = Widget(primaryTex, primaryHoverTex, primaryTex, 15, new Color(0.11f, 0.09f, 0.04f));
            ButtonPrimary.fontStyle = FontStyle.Bold;
            ButtonGhost = Widget(null, ghostHoverTex, ghostHoverTex, 14, InkDim);

            PanelBox = new GUIStyle();
            PanelBox.normal.background = panelTex;
            PanelBox.border = new RectOffset(11, 11, 11, 11);

            CardBox = new GUIStyle();
            CardBox.normal.background = cardTex;
            CardBox.border = new RectOffset(11, 11, 11, 11);
        }

        static GUIStyle Text(int size, FontStyle style, Color color)
        {
            GUIStyle s = new GUIStyle();
            if (uiFont != null) s.font = uiFont;
            s.fontSize = S(size);
            s.fontStyle = style;
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.alignment = TextAnchor.MiddleLeft;
            s.wordWrap = false;
            s.richText = false;
            s.clipping = TextClipping.Clip;
            return s;
        }

        static GUIStyle Widget(Texture2D normal, Texture2D hover, Texture2D active, int size, Color color)
        {
            GUIStyle s = new GUIStyle();
            if (uiFont != null) s.font = uiFont;
            s.fontSize = S(size);
            s.alignment = TextAnchor.MiddleCenter;
            s.border = new RectOffset(9, 9, 9, 9);
            s.padding = new RectOffset(S(10), S(10), S(7), S(7));
            s.margin = new RectOffset(S(3), S(3), S(3), S(3));
            s.normal.background = normal;
            s.hover.background = hover;
            s.active.background = active;
            s.focused.background = normal;
            s.normal.textColor = color;
            s.hover.textColor = Color.Lerp(color, Color.white, 0.4f);
            s.active.textColor = Palette.Gold;
            s.focused.textColor = color;
            s.clipping = TextClipping.Clip;
            return s;
        }

        // ==================================================================
        //  dessin
        // ==================================================================

        public static Texture2D Solid(Color color)
        {
            Texture2D tex;
            if (Solids.TryGetValue(color, out tex) && tex != null) return tex;

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            Solids[color] = tex;
            return tex;
        }

        public static void Fill(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Solid(color));
        }

        /// <summary>Panneau principal : ombre portee + fond arrondi + liseré.</summary>
        public static void Frame(Rect rect)
        {
            DropShadow(rect, S(22));
            GUI.Box(rect, GUIContent.none, PanelBox);
        }

        /// <summary>Bloc secondaire, pose a l'interieur d'un panneau.</summary>
        public static void CardFrame(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, CardBox);
        }

        public static void DropShadow(Rect rect, int spread)
        {
            if (shadowTex == null) return;
            GUI.DrawTexture(new Rect(rect.x - spread, rect.y - spread + spread * 0.25f,
                                     rect.width + spread * 2f, rect.height + spread * 2f), shadowTex);
        }

        /// <summary>Une jauge arrondie : fond creuse, remplissage, fin reflet au-dessus.</summary>
        public static void Bar(Rect rect, float fill01, Color fill, Color background)
        {
            Fill(rect, background);
            float w = Mathf.Clamp01(fill01) * rect.width;
            if (w > 1f)
            {
                Fill(new Rect(rect.x, rect.y, w, rect.height), fill);
                Fill(new Rect(rect.x, rect.y, w, Mathf.Max(1f, rect.height * 0.32f)),
                     new Color(1f, 1f, 1f, 0.16f));
            }
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(0f, 0f, 0f, 0.35f));
        }

        /// <summary>Pastille arrondie discrete (fond d'etiquette).</summary>
        public static void Pill(Rect rect)
        {
            if (pillTex == null) { Fill(rect, new Color(1f, 1f, 1f, 0.06f)); return; }
            GUI.DrawTexture(rect, pillTex, ScaleMode.StretchToFill, true);
        }

        /// <summary>Petit carre de couleur, utilise comme icone de ressource.</summary>
        public static void Chip(Rect rect, Color color)
        {
            Fill(rect, new Color(0f, 0f, 0f, 0.45f));
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), color);
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, Mathf.Max(1f, rect.height * 0.3f)),
                 new Color(1f, 1f, 1f, 0.22f));
        }

        public static void Rule(Rect rect)
        {
            Fill(rect, new Color(1f, 1f, 1f, 0.07f));
        }

        public static void Shadowed(Rect rect, string text, GUIStyle style)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), text, style);
            style.normal.textColor = original;
            GUI.Label(rect, text, style);
        }

        /// <summary>Texte affiche avec une couleur donnee, sans casser le style partage.</summary>
        public static void Tinted(Rect rect, string text, GUIStyle style, Color color)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = original;
        }
    }
}
