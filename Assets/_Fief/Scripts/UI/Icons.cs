using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES ICONES A L'ECRAN (30/09 -- "aucun texte, des icones, plus pro, comme Fall
    /// Guys"). IconArt dessine les formes ; ici on en fait des textures (une fois, a la
    /// demande) et on les pose : une icone blanche a peine degradee, cernee d'un epais
    /// liseré sombre ; des pastilles arrondies facon boutons de jeu, avec leur reflet.
    ///
    /// Concept Unity : une Texture2D creee par le code (new Texture2D, SetPixels, Apply)
    /// vit en memoire graphique comme une image importee ; GUI.DrawTexture la pose dans
    /// un rectangle, teintee par GUI.color.
    /// </summary>
    public static class Icons
    {
        const int Size = 128;
        const float EdgeWidth = 0.11f;

        static readonly Dictionary<string, Texture2D> glyphs = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> edges = new Dictionary<string, Texture2D>();
        static Texture2D disc, discEdge, white, shine;

        /// <summary>Le liseré des icones et des pastilles : un bleu nuit presque noir.</summary>
        public static readonly Color Ink = new Color(0.07f, 0.06f, 0.14f, 0.92f);

        // ================================================================== les icones

        /// <summary>Poser l'icone "id" dans "r", de la couleur "c", cernee de sombre.</summary>
        public static void Draw(Rect r, string id, Color c)
        {
            Draw(r, id, c, true);
        }

        public static void Draw(Rect r, string id, Color c, bool edge)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            Color was = GUI.color;
            if (edge)
            {
                GUI.color = new Color(Ink.r, Ink.g, Ink.b, Ink.a * c.a);
                GUI.DrawTexture(new Rect(r.x + r.width * 0.02f, r.y + r.height * 0.05f, r.width, r.height), Edge(id), ScaleMode.StretchToFill, true);
                GUI.DrawTexture(r, Edge(id), ScaleMode.StretchToFill, true);
            }
            GUI.color = c;
            GUI.DrawTexture(r, Glyph(id), ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        static Texture2D Glyph(string id)
        {
            Texture2D t;
            if (glyphs.TryGetValue(id, out t) && t != null) return t;
            float[] a = IconArt.Render(id, Size, 0f);
            t = Make(a, true);
            glyphs[id] = t;
            return t;
        }

        static Texture2D Edge(string id)
        {
            Texture2D t;
            if (edges.TryGetValue(id, out t) && t != null) return t;
            float[] a = IconArt.Render(id, Size, EdgeWidth);
            t = Make(a, false);
            edges[id] = t;
            return t;
        }

        /// <summary>Une texture blanche (un leger degrade du haut vers le bas si "shade") dont l'alpha est la couverture.</summary>
        static Texture2D Make(float[] a, bool shade)
        {
            Texture2D t = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[Size * Size];
            for (int j = 0; j < Size; j++)
            {
                float k = shade ? Mathf.Lerp(0.84f, 1f, j / (float)(Size - 1)) : 1f;
                for (int i = 0; i < Size; i++) px[j * Size + i] = new Color(k, k, k, a[j * Size + i]);
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        // ================================================================== les pastilles

        static void Ensure()
        {
            if (disc != null) return;
            disc = Circle(0.96f);
            discEdge = Circle(1f);
            white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            white.hideFlags = HideFlags.HideAndDontSave;
            // Le reflet : un degrade blanc, opaque en haut, nul au milieu.
            shine = new Texture2D(1, 64, TextureFormat.RGBA32, false);
            shine.wrapMode = TextureWrapMode.Clamp;
            shine.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < 64; y++) shine.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Clamp01((y - 30f) / 33f)));
            shine.Apply();
        }

        static Texture2D Circle(float radius)
        {
            const int n = 128;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(x * x + y * y) - radius;
                    px[j * n + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d * n * 0.5f));
                }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        /// <summary>
        /// UNE PASTILLE facon bouton de jeu : une gelule (ou un rond si carre) de la couleur
        /// "fill", un liseré sombre, un reflet en haut. C'est la brique de tout le HUD.
        /// </summary>
        public static void Pill(Rect r, Color fill)
        {
            Pill(r, fill, 1f);
        }

        public static void Pill(Rect r, Color fill, float alpha)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            Ensure();
            float e = Mathf.Max(2f, r.height * 0.07f);
            Color was = GUI.color;
            // L'ombre portee, le liseré, le corps, le reflet.
            Shape(new Rect(r.x - e + 1f, r.y - e + r.height * 0.08f, r.width + e * 2f, r.height + e * 2f), new Color(0f, 0f, 0f, 0.3f * alpha));
            Shape(new Rect(r.x - e, r.y - e, r.width + e * 2f, r.height + e * 2f), new Color(Ink.r, Ink.g, Ink.b, alpha));
            Shape(r, new Color(fill.r, fill.g, fill.b, fill.a * alpha));
            Rect top = new Rect(r.x + r.height * 0.18f, r.y + r.height * 0.08f, r.width - r.height * 0.36f, r.height * 0.42f);
            if (top.width > 1f)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.28f * alpha);
                GUI.DrawTexture(top, shine, ScaleMode.StretchToFill, true);
            }
            GUI.color = was;
        }

        /// <summary>Une gelule pleine (deux demi-disques et un rectangle), sans rien d'autre.</summary>
        static void Shape(Rect r, Color c)
        {
            Color was = GUI.color;
            GUI.color = c;
            float h = r.height;
            if (r.width <= h + 0.5f) GUI.DrawTexture(r, disc, ScaleMode.StretchToFill, true);
            else
            {
                GUI.BeginGroup(new Rect(r.x, r.y, h * 0.5f, h));
                GUI.DrawTexture(new Rect(0f, 0f, h, h), disc, ScaleMode.StretchToFill, true);
                GUI.EndGroup();
                GUI.BeginGroup(new Rect(r.xMax - h * 0.5f, r.y, h * 0.5f, h));
                GUI.DrawTexture(new Rect(-h * 0.5f, 0f, h, h), disc, ScaleMode.StretchToFill, true);
                GUI.EndGroup();
                GUI.DrawTexture(new Rect(r.x + h * 0.5f - 0.5f, r.y + h * 0.02f, r.width - h + 1f, h * 0.96f), white, ScaleMode.StretchToFill, true);
            }
            GUI.color = was;
        }

        /// <summary>Un disque plein (pour une recharge, un point de couleur).</summary>
        public static void Dot(Rect r, Color c)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            Ensure();
            Color was = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, disc, ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        /// <summary>
        /// La recharge d'un rond : la part "left" (0-1) encore a attendre, assombrie par le
        /// haut. (Un masque rond, coupe par un rectangle : BeginGroup ne dessine que dedans.)
        /// </summary>
        public static void Cooldown(Rect r, float left)
        {
            if (left <= 0f || Event.current == null || Event.current.type != EventType.Repaint) return;
            Ensure();
            float h = r.height * Mathf.Clamp01(left);
            Color was = GUI.color;
            GUI.color = new Color(0.04f, 0.03f, 0.1f, 0.7f);
            GUI.BeginGroup(new Rect(r.x, r.y, r.width, h));
            GUI.DrawTexture(new Rect(0f, 0f, r.width, r.height), disc, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
            GUI.color = was;
        }

        /// <summary>
        /// UNE TOUCHE : une icone de souris (pour les clics) ou une touche de clavier avec sa
        /// lettre. "bind" : un nom de FiefInput.BindNames, ou une lettre.
        /// </summary>
        public static void Key(Rect r, string bind, float alpha)
        {
            Color white2 = new Color(1f, 1f, 1f, alpha);
            if (bind == "Clic gauche") { Draw(r, "souris-g", white2); return; }
            if (bind == "Clic droit") { Draw(r, "souris-d", white2); return; }
            if (bind == "Clic molette") { Draw(r, "souris-m", white2); return; }
            Pill(new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.08f, r.width * 0.84f, r.height * 0.84f), new Color(0.93f, 0.93f, 0.97f), alpha);
            GUIStyle s = KeyStyle(Mathf.RoundToInt(r.height * (bind.Length > 2 ? 0.3f : 0.52f)));
            Color was = GUI.color;
            GUI.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            GUI.Label(new Rect(r.x, r.y - r.height * 0.02f, r.width, r.height), bind, s);
            GUI.color = was;
        }

        static GUIStyle keyStyle;
        static GUIStyle KeyStyle(int size)
        {
            if (keyStyle == null)
            {
                UiStyle.Ensure();
                keyStyle = new GUIStyle(UiStyle.Title);
                keyStyle.alignment = TextAnchor.MiddleCenter;
                keyStyle.normal.textColor = Color.white;
                keyStyle.wordWrap = false;
                keyStyle.clipping = TextClipping.Overflow;
            }
            keyStyle.fontSize = Mathf.Max(8, size);
            return keyStyle;
        }

        /// <summary>
        /// DES CHIFFRES facon Fall Guys : gros, blancs, cernes d'un liseré epais (huit
        /// ombres autour, puis le chiffre). Pour le chrono, les scores.
        /// </summary>
        public static void Number(Rect r, string digits, int size, Color c, TextAnchor align)
        {
            GUIStyle s = NumberStyle(size, align);
            Color was = GUI.color;
            float o = Mathf.Max(1.5f, size * 0.07f);
            GUI.color = new Color(Ink.r, Ink.g, Ink.b, c.a);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f;
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * o, r.y + Mathf.Sin(a) * o, r.width, r.height), digits, s);
            }
            GUI.Label(new Rect(r.x + o * 0.3f, r.y + o * 1.4f, r.width, r.height), digits, s);
            GUI.color = c;
            GUI.Label(r, digits, s);
            GUI.color = was;
        }

        static readonly Dictionary<int, GUIStyle> numberStyles = new Dictionary<int, GUIStyle>();
        static GUIStyle NumberStyle(int size, TextAnchor align)
        {
            int key = size * 16 + (int)align;
            GUIStyle s;
            if (numberStyles.TryGetValue(key, out s)) return s;
            UiStyle.Ensure();
            s = new GUIStyle(UiStyle.Title);
            s.fontSize = size;
            s.alignment = align;
            s.normal.textColor = Color.white;
            s.wordWrap = false;
            s.clipping = TextClipping.Overflow;
            numberStyles[key] = s;
            return s;
        }

        // ================================================================== les capacites

        /// <summary>L'icone de chaque capacite.</summary>
        public static string Of(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: return "ruee";
                case Ability.Grappin: return "grappin";
                case Ability.Crochet: return "crochet";
                case Ability.Onde: return "onde";
                case Ability.Clignement: return "clignement";
                case Ability.Bond: return "bond";
                case Ability.Mur: return "mur";
                case Ability.Nuee: return "nuee";
                case Ability.Mine: return "mine";
                case Ability.Gel: return "gel";
                case Ability.Voile: return "voile";
                case Ability.Echange: return "echange";
                case Ability.Rappel: return "rappel";
                case Ability.Souffle: return "souffle";
                case Ability.DoubleSaut: return "double-saut";
                case Ability.Planeur: return "planeur";
                case Ability.Coureur: return "coureur";
                case Ability.Porteur: return "porteur";
                case Ability.Poigne: return "poigne";
                case Ability.Ancrage: return "ancrage";
                case Ability.Flair: return "flair";
                case Ability.Ombre: return "ombre";
                case Ability.PriseFerme: return "prise-ferme";
                case Ability.Recharge: return "recharge";
                case Ability.Rebond: return "rebond";
                default: return "aimant";
            }
        }
    }
}
