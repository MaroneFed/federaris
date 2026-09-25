using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE STYLE DE FIEF. Toute l'interface du jeu sort d'ici.
    ///
    /// L'idee : un manuscrit relie de fer. Des panneaux de cuir presque noir, un
    /// double filet de bronze, un petit losange a chaque coin ; des titres en
    /// capitales a empattements (Palatino, Book Antiqua...), du texte couleur
    /// parchemin ; des boutons comme des plaques gravees, et un seul rouge -- le
    /// cramoisi du chateau -- pour ce qui compte.
    ///
    /// Tout est DESSINE par le code, pixel par pixel : pas une image importee.
    /// Changer une couleur ici change tout le jeu.
    ///
    /// Concept Unity : une Texture2D est une grille de pixels qu'on peut ecrire a la
    /// main. Un GUIStyle etire cette texture en "9 tranches" (les 4 coins restent
    /// intacts, les bords et le centre s'etirent) : un seul petit carre orne sert
    /// donc de cadre a un panneau de n'importe quelle taille, sans deformer les
    /// losanges des coins.
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

        // --- couleurs : parchemin, bronze, cuir, cramoisi
        public static readonly Color Ink = new Color(0.94f, 0.90f, 0.81f);
        public static readonly Color InkDim = new Color(0.70f, 0.65f, 0.56f);
        public static readonly Color InkFaint = new Color(0.48f, 0.45f, 0.40f);
        public static readonly Color Panel = new Color(0.075f, 0.066f, 0.058f, 0.96f);
        public static readonly Color Card = new Color(0.12f, 0.105f, 0.09f, 0.97f);
        public static readonly Color Edge = new Color(0.55f, 0.44f, 0.26f, 1f);
        public static readonly Color EdgeGold = new Color(0.86f, 0.70f, 0.36f, 1f);
        public static readonly Color Crimson = new Color(0.52f, 0.12f, 0.09f, 1f);
        public static readonly Color BarBg = new Color(0.02f, 0.018f, 0.015f, 0.8f);
        public static readonly Color Scrim = new Color(0.02f, 0.018f, 0.015f, 0.84f);

        // --- ancien nom conserve pour compatibilite
        public static readonly Color PanelBg = Panel;
        public static readonly Color PanelEdge = EdgeGold;

        public enum Shape { Dot, Diamond, Triangle, Square }

        static readonly Dictionary<Color, Texture2D> Solids = new Dictionary<Color, Texture2D>();
        static Texture2D panelTex, cardTex, shadowTex, btnTex, btnHoverTex, btnActiveTex;
        static Texture2D primaryTex, primaryHoverTex, ghostHoverTex, pillTex;
        static Texture2D dotTex, diamondTex, triangleTex, fadeTex;
        static Font titleFont, bodyFont;
        static float builtScale = -1f;

        public static int S(float v) { return Mathf.RoundToInt(v * Scale); }

        // ==================================================================
        //  construction
        // ==================================================================

        public static void Ensure()
        {
            Settings.Load();
            Scale = Mathf.Clamp(Screen.height / 900f, 0.8f, 2.4f) * Settings.TextSize;
            if (Mathf.Abs(Scale - builtScale) < 0.001f && Title != null) return;
            builtScale = Scale;

            BuildTextures();
            BuildFonts();
            BuildStyles();
        }

        /// <summary>
        /// Deux polices. D'abord celles du jeu, rangees dans Resources/Fonts :
        /// Titre.ttf (Grenze, demi-gras : a mi-chemin entre la lettre gothique et
        /// le romain, du caractere sans cesser d'etre lisible) et Texte.ttf
        /// (Alegreya Sans, medium : une linéale douce, nette sur fond sombre).
        /// Changees le 26/09 (Martin : "c'est la police d'ecriture... ca donne pas
        /// envie") -- Cinzel et Garamond faisaient livre de bibliotheque. Toutes deux
        /// sous licence OFL : libres, meme pour un jeu vendu, a condition de garder
        /// le fichier de licence a cote.
        ///
        /// Pour en changer : remplace le .ttf par un autre du meme nom (Google Fonts
        /// en a des centaines). Sans fichier, on retombe sur une police systeme a
        /// empattements ; et sans rien, sur celle d'Unity.
        /// </summary>
        static void BuildFonts()
        {
            if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/Titre");
            if (bodyFont == null)
            {
                bodyFont = Resources.Load<Font>("Fonts/Texte");
                // L'Alegreya a de petites minuscules : on la grossit un peu pour
                // qu'elle se lise aussi bien qu'un Arial de meme taille.
                if (bodyFont != null) bodyBoost = 1.08f;
            }
            if (titleFont == null)
                titleFont = TryFont(new[] { "Palatino Linotype", "Book Antiqua", "Palatino", "Constantia", "Georgia", "Times New Roman" });
            if (bodyFont == null)
                bodyFont = TryFont(new[] { "Constantia", "Georgia", "Palatino Linotype", "Cambria", "Segoe UI", "Arial" });
        }

        static float bodyBoost = 1f;

        static Font TryFont(string[] names)
        {
            try
            {
                return Font.CreateDynamicFontFromOSFont(names, 16);
            }
            catch (System.Exception)
            {
                return null;   // aucune de ces polices : on garde celle d'Unity
            }
        }

        static void BuildTextures()
        {
            if (panelTex != null) return;

            panelTex = Ornate(new Color(0.085f, 0.075f, 0.066f, 0.97f), new Color(0.06f, 0.052f, 0.045f, 0.97f), Edge, true);
            cardTex = Ornate(new Color(0.14f, 0.12f, 0.1f, 0.97f), new Color(0.11f, 0.095f, 0.08f, 0.97f),
                             new Color(0.38f, 0.31f, 0.2f, 1f), false);
            btnTex = Plate(new Color(0.15f, 0.13f, 0.11f, 1f), new Color(0.40f, 0.33f, 0.21f, 1f));
            btnHoverTex = Plate(new Color(0.22f, 0.19f, 0.15f, 1f), EdgeGold);
            btnActiveTex = Plate(new Color(0.10f, 0.09f, 0.075f, 1f), EdgeGold);
            primaryTex = Plate(Crimson, EdgeGold);
            primaryHoverTex = Plate(new Color(0.66f, 0.17f, 0.12f, 1f), new Color(1f, 0.86f, 0.5f, 1f));
            ghostHoverTex = Plate(new Color(1f, 0.9f, 0.7f, 0.06f), new Color(0.7f, 0.58f, 0.36f, 0.8f));
            pillTex = Plate(new Color(1f, 0.95f, 0.85f, 0.06f), new Color(1f, 0.95f, 0.85f, 0.18f));
            shadowTex = Shadow(26);

            dotTex = ShapeTex(32, Shape.Dot);
            diamondTex = ShapeTex(32, Shape.Diamond);
            triangleTex = ShapeTex(32, Shape.Triangle);
            fadeTex = HorizontalFade(64);
        }

        /// <summary>
        /// Le cadre des panneaux : cuir presque noir (un peu plus clair en haut), un
        /// filet de bronze a l'exterieur, un second plus fin a quatre pixels dedans,
        /// et un losange de bronze dans chaque coin. 48 x 48, coins de 16.
        /// </summary>
        static Texture2D Ornate(Color top, Color bottom, Color edge, bool diamonds)
        {
            const int Size = 48;
            Texture2D tex = NewTex(Size);
            Color[] px = new Color[Size * Size];
            Color innerLine = new Color(edge.r, edge.g, edge.b, 0.45f);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = Mathf.Min(Mathf.Min(fx, Size - fx), Mathf.Min(fy, Size - fy));    // distance au bord
                    Color c = Color.Lerp(bottom, top, (float)y / (Size - 1));
                    // coins legerement arrondis
                    float cx = Mathf.Max(0f, 3f - Mathf.Min(fx, Size - fx)), cy = Mathf.Max(0f, 3f - Mathf.Min(fy, Size - fy));
                    float corner = Mathf.Sqrt(cx * cx + cy * cy);
                    if (corner > 3f) { px[y * Size + x] = Color.clear; continue; }
                    if (d < 1.6f) c = Color.Lerp(c, edge, 1f);
                    else if (d > 4f && d < 5f) c = Color.Lerp(c, innerLine, innerLine.a);
                    if (diamonds)
                    {
                        // un losange a (9, 9) de chaque coin
                        float qx = Mathf.Min(fx, Size - fx) - 9f, qy = Mathf.Min(fy, Size - fy) - 9f;
                        float m = Mathf.Abs(qx) + Mathf.Abs(qy);
                        if (m < 2.6f) c = Color.Lerp(c, edge, Mathf.Clamp01(2.6f - m));
                    }
                    px[y * Size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Une plaque de bouton : fond uni, un filet, les coins coupes en biseau.</summary>
        static Texture2D Plate(Color fill, Color edge)
        {
            const int Size = 24;
            Texture2D tex = NewTex(Size);
            Color[] px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float fx = Mathf.Min(x + 0.5f, Size - x - 0.5f);
                    float fy = Mathf.Min(y + 0.5f, Size - y - 0.5f);
                    if (fx + fy < 3f) { px[y * Size + x] = Color.clear; continue; }      // biseau
                    bool border = fx < 1.5f || fy < 1.5f || fx + fy < 4.5f;
                    Color c = border ? edge : fill;
                    if (!border && y > Size * 0.55f) c = Color.Lerp(c, Color.white, 0.03f);
                    px[y * Size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Un halo sombre et flou : pose sous un panneau, il le decolle du jeu.</summary>
        static Texture2D Shadow(int size)
        {
            Texture2D tex = NewTex(size);
            float half = size * 0.5f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(0f, 0f, 0f, Mathf.Pow(1f - d, 2.4f) * 0.6f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Les petites formes de la boussole et des icones, anticrenelees.</summary>
        static Texture2D ShapeTex(int size, Shape shape)
        {
            Texture2D tex = NewTex(size);
            Color[] px = new Color[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f - h) / h, v = (y + 0.5f - h) / h;
                    float d;
                    if (shape == Shape.Dot) d = Mathf.Sqrt(u * u + v * v) - 0.85f;
                    else if (shape == Shape.Diamond) d = (Mathf.Abs(u) + Mathf.Abs(v)) - 0.92f;
                    else
                    {
                        // triangle pointe en bas (comme une aiguille) : v de -1 (bas) a +1 (haut)
                        float edge = Mathf.Abs(u) - (v + 0.9f) * 0.5f;
                        d = Mathf.Max(edge, Mathf.Max(v - 0.85f, -0.9f - v));
                    }
                    float a = Mathf.Clamp01(0.5f - d * h);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Un degrade qui s'efface aux deux bouts : le fond de la boussole.</summary>
        static Texture2D HorizontalFade(int width)
        {
            Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(u, 1f - u) * 4f));
                tex.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        static Texture2D NewTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        static void BuildStyles()
        {
            Title = Text(30, FontStyle.Normal, Palette.Gold, titleFont);
            Big = Text(46, FontStyle.Normal, Palette.Gold, titleFont);
            Head = Text(18, FontStyle.Normal, Ink, titleFont);
            Label = Text(15, FontStyle.Normal, Ink, bodyFont);
            Small = Text(13, FontStyle.Normal, InkDim, bodyFont);
            Tiny = Text(11, FontStyle.Normal, InkFaint, bodyFont);
            Value = Text(22, FontStyle.Normal, Ink, titleFont);

            Centered = Text(15, FontStyle.Normal, Ink, bodyFont);
            Centered.alignment = TextAnchor.MiddleCenter;
            CenteredSmall = Text(12, FontStyle.Normal, InkDim, bodyFont);
            CenteredSmall.alignment = TextAnchor.MiddleCenter;

            Button = Widget(btnTex, btnHoverTex, btnActiveTex, 14, Ink);
            ButtonPrimary = Widget(primaryTex, primaryHoverTex, primaryTex, 15, new Color(1f, 0.93f, 0.8f));
            ButtonGhost = Widget(null, ghostHoverTex, ghostHoverTex, 14, InkDim);
            ButtonGhost.alignment = TextAnchor.MiddleLeft;
            ButtonGhost.padding = new RectOffset(S(14), S(10), S(6), S(6));

            PanelBox = new GUIStyle();
            PanelBox.normal.background = panelTex;
            PanelBox.border = new RectOffset(16, 16, 16, 16);

            CardBox = new GUIStyle();
            CardBox.normal.background = cardTex;
            CardBox.border = new RectOffset(16, 16, 16, 16);
        }

        static GUIStyle Text(int size, FontStyle style, Color color, Font font)
        {
            GUIStyle s = new GUIStyle();
            if (font != null) s.font = font;
            s.fontSize = S(font != null && font == bodyFont ? size * bodyBoost : size);
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
            if (titleFont != null) s.font = titleFont;
            s.fontSize = S(size);
            s.alignment = TextAnchor.MiddleCenter;
            s.border = new RectOffset(7, 7, 7, 7);
            s.padding = new RectOffset(S(10), S(10), S(7), S(7));
            s.margin = new RectOffset(S(3), S(3), S(3), S(3));
            s.normal.background = normal;
            s.hover.background = hover;
            s.active.background = active;
            s.focused.background = normal;
            s.normal.textColor = color;
            s.hover.textColor = Color.Lerp(color, new Color(1f, 0.92f, 0.7f), 0.6f);
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

        /// <summary>Panneau principal : ombre portee + cuir + double filet de bronze.</summary>
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

        /// <summary>Une jauge : fond creuse, remplissage, fin reflet, et deux butees de bronze.</summary>
        public static void Bar(Rect rect, float fill01, Color fill, Color background)
        {
            Fill(rect, background);
            float w = Mathf.Clamp01(fill01) * rect.width;
            if (w > 1f)
            {
                Fill(new Rect(rect.x, rect.y, w, rect.height), fill);
                Fill(new Rect(rect.x, rect.y, w, Mathf.Max(1f, rect.height * 0.32f)), new Color(1f, 1f, 1f, 0.16f));
            }
            Fill(new Rect(rect.x - 1f, rect.y - 1f, 1f, rect.height + 2f), Edge);
            Fill(new Rect(rect.xMax, rect.y - 1f, 1f, rect.height + 2f), Edge);
        }

        /// <summary>Pastille arrondie discrete (fond d'etiquette, touche de clavier).</summary>
        public static void Pill(Rect rect)
        {
            if (pillTex == null) { Fill(rect, new Color(1f, 1f, 1f, 0.06f)); return; }
            GUI.Box(rect, GUIContent.none, PillStyle());
        }

        static GUIStyle pillStyle;
        static GUIStyle PillStyle()
        {
            if (pillStyle == null)
            {
                pillStyle = new GUIStyle();
                pillStyle.normal.background = pillTex;
                pillStyle.border = new RectOffset(7, 7, 7, 7);
            }
            return pillStyle;
        }

        /// <summary>L'icone d'une ressource : un petit losange taille, colore, avec un reflet.</summary>
        public static void Chip(Rect rect, Color color)
        {
            Icon(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Shape.Diamond, new Color(0f, 0f, 0f, 0.6f));
            Icon(rect, Shape.Diamond, color);
            Icon(new Rect(rect.x + rect.width * 0.25f, rect.y + rect.height * 0.12f, rect.width * 0.5f, rect.height * 0.4f),
                 Shape.Diamond, new Color(1f, 1f, 1f, 0.28f));
        }

        /// <summary>Dessine une forme (point, losange, triangle-aiguille, carre) teintee.</summary>
        public static void Icon(Rect rect, Shape shape, Color color)
        {
            Texture2D tex = shape == Shape.Dot ? dotTex : shape == Shape.Diamond ? diamondTex
                          : shape == Shape.Triangle ? triangleTex : Solid(Color.white);
            if (tex == null) return;
            Color was = GUI.color;
            GUI.color = new Color(was.r * color.r, was.g * color.g, was.b * color.b, was.a * color.a);
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        /// <summary>Le fond de la boussole : une bande qui s'efface aux deux bouts.</summary>
        public static void FadeBand(Rect rect, Color color)
        {
            if (fadeTex == null) { Fill(rect, color); return; }
            Color was = GUI.color;
            GUI.color = new Color(was.r * color.r, was.g * color.g, was.b * color.b, was.a * color.a);
            GUI.DrawTexture(rect, fadeTex, ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        /// <summary>
        /// Un filet de separation orne : deux traits de bronze qui s'effacent vers
        /// les bords, et un losange au milieu.
        /// </summary>
        public static void Rule(Rect rect)
        {
            float cy = rect.y + rect.height * 0.5f;
            FadeBand(new Rect(rect.x, cy, rect.width, 1f), new Color(Edge.r, Edge.g, Edge.b, 0.8f));
            float d = S(7);
            Icon(new Rect(rect.center.x - d * 0.5f, cy - d * 0.5f, d, d), Shape.Diamond, Edge);
        }

        public static void Shadowed(Rect rect, string text, GUIStyle style)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
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

        /// <summary>
        /// Un titre ESPACE ("F I E F", "L A   C L O C H E") : c'est ce qui fait
        /// "grave dans la pierre" plutot que "tape a la machine".
        /// </summary>
        public static string Spaced(string text)
        {
            System.Text.StringBuilder b = new System.Text.StringBuilder(text.Length * 2);
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0) b.Append(text[i] == ' ' ? "  " : " ");
                if (text[i] != ' ') b.Append(text[i]);
            }
            return b.ToString();
        }
    }
}
