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
    /// NET AU PIXEL PRES (01/10 -- Martin : "un effet de flou constant partout, ca fait
    /// jeu ancien"). Avant, chaque icone etait UNE image de 128 pixels, reduite par la
    /// carte graphique a 30 ou 40 pixels en melangeant deux niveaux de "mipmaps" : c'est
    /// ce melange qui faisait le flou. Maintenant, une icone de 37 pixels est DESSINEE a
    /// 37 pixels (la forme mathematique le permet), posee sur des pixels entiers : un
    /// pixel de l'image = un pixel de l'ecran, rien n'est etire. Idem pour les pastilles
    /// et les chiffres.
    ///
    /// Concept Unity : une Texture2D creee par le code (new Texture2D, SetPixels, Apply)
    /// vit en memoire graphique comme une image importee ; GUI.DrawTexture la pose dans
    /// un rectangle, teintee par GUI.color. Si le rectangle n'a pas la taille de l'image,
    /// la carte graphique l'etire -- et l'image devient floue.
    /// </summary>
    public static class Icons
    {
        const int MaxSize = 256;
        const float EdgeWidth = 0.11f;
        /// <summary>Combien d'images neuves au plus par image du jeu (une icone qui grossit en s'animant ne doit pas tout ralentir).</summary>
        const int BudgetPerFrame = 4;

        static readonly Dictionary<string, Dictionary<int, Texture2D>> glyphs = new Dictionary<string, Dictionary<int, Texture2D>>();
        static readonly Dictionary<string, Dictionary<int, Texture2D>> edges = new Dictionary<string, Dictionary<int, Texture2D>>();
        static readonly Dictionary<int, Texture2D> discs = new Dictionary<int, Texture2D>();
        static readonly Dictionary<int, GUIStyle> capsules = new Dictionary<int, GUIStyle>();
        static int budgetFrame = -1, budgetUsed;
        static int made;

        /// <summary>Le liseré des icones et des pastilles : un bleu nuit presque noir.</summary>
        public static readonly Color Ink = new Color(0.07f, 0.06f, 0.14f, 0.92f);

        static bool Painting { get { return Event.current != null && Event.current.type == EventType.Repaint; } }

        /// <summary>Un rectangle pose sur des pixels entiers (sinon la carte graphique melange deux pixels : flou).</summary>
        public static Rect Snap(Rect r)
        {
            float x0 = Mathf.Round(r.x), y0 = Mathf.Round(r.y);
            return new Rect(x0, y0, Mathf.Round(r.xMax) - x0, Mathf.Round(r.yMax) - y0);
        }

        /// <summary>Un carre de "px" pixels entiers, centre sur "r".</summary>
        static Rect Square(Rect r, out int px)
        {
            px = Mathf.Clamp(Mathf.RoundToInt((r.width + r.height) * 0.5f), 4, MaxSize);
            return new Rect(Mathf.Round(r.center.x - px * 0.5f), Mathf.Round(r.center.y - px * 0.5f), px, px);
        }

        /// <summary>Peut-on fabriquer une image de plus a cette image-ci ?</summary>
        static bool Afford()
        {
            if (budgetFrame != Time.frameCount) { budgetFrame = Time.frameCount; budgetUsed = 0; }
            if (budgetUsed >= BudgetPerFrame) return false;
            budgetUsed++;
            return true;
        }

        /// <summary>Trop d'images gardees (des animations de taille) : on repart de zero.</summary>
        static void Trim()
        {
            if (made < 900) return;
            made = 0;
            foreach (Dictionary<int, Texture2D> d in glyphs.Values) foreach (Texture2D t in d.Values) Object.Destroy(t);
            foreach (Dictionary<int, Texture2D> d in edges.Values) foreach (Texture2D t in d.Values) Object.Destroy(t);
            glyphs.Clear();
            edges.Clear();
        }

        // ================================================================== les icones

        /// <summary>Poser l'icone "id" dans "r", de la couleur "c", cernee de sombre.</summary>
        public static void Draw(Rect r, string id, Color c)
        {
            Draw(r, id, c, true);
        }

        public static void Draw(Rect r, string id, Color c, bool edge)
        {
            if (!Painting || c.a <= 0.003f) return;
            int px;
            Rect q = Square(r, out px);
            Color was = GUI.color;
            if (edge)
            {
                Texture2D e = Get(edges, id, px, EdgeWidth, false);
                int drop = Mathf.Max(1, Mathf.RoundToInt(px * 0.05f));
                GUI.color = new Color(Ink.r, Ink.g, Ink.b, Ink.a * c.a);
                GUI.DrawTexture(new Rect(q.x, q.y + drop, q.width, q.height), e, ScaleMode.StretchToFill, true);
                GUI.DrawTexture(q, e, ScaleMode.StretchToFill, true);
            }
            GUI.color = c;
            GUI.DrawTexture(q, Get(glyphs, id, px, 0f, true), ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        /// <summary>
        /// L'image de l'icone a "px" pixels. Deja faite : on la rend. Sinon on la fabrique,
        /// sauf si le budget de cette image est epuise : on prete alors la plus proche.
        /// </summary>
        static Texture2D Get(Dictionary<string, Dictionary<int, Texture2D>> cache, string id, int px, float grow, bool shade)
        {
            Dictionary<int, Texture2D> sizes;
            if (!cache.TryGetValue(id, out sizes)) { sizes = new Dictionary<int, Texture2D>(); cache[id] = sizes; }
            Texture2D t;
            if (sizes.TryGetValue(px, out t) && t != null) return t;
            if (sizes.Count > 0 && !Afford())
            {
                Texture2D best = null;
                int gap = int.MaxValue;
                foreach (KeyValuePair<int, Texture2D> kv in sizes)
                    if (kv.Value != null && Mathf.Abs(kv.Key - px) < gap) { gap = Mathf.Abs(kv.Key - px); best = kv.Value; }
                if (best != null) return best;
            }
            Trim();
            if (!cache.TryGetValue(id, out sizes)) { sizes = new Dictionary<int, Texture2D>(); cache[id] = sizes; }
            // Le liseré se mesure en pixels : au moins 2, pour qu'il se voie sur les petites icones.
            float g = grow > 0f ? Mathf.Max(grow, 4.2f / px) : 0f;
            t = Make(IconArt.Render(id, px, g), px, shade);
            sizes[px] = t;
            made++;
            return t;
        }

        /// <summary>Une texture blanche (un leger degrade du haut vers le bas si "shade") dont l'alpha est la couverture.</summary>
        static Texture2D Make(float[] a, int n, bool shade)
        {
            Texture2D t = NewTexture(n, n);
            Color32[] px = new Color32[n * n];
            for (int j = 0; j < n; j++)
            {
                byte k = (byte)(255f * (shade ? Mathf.Lerp(0.84f, 1f, j / (float)Mathf.Max(1, n - 1)) : 1f));
                for (int i = 0; i < n; i++) px[j * n + i] = new Color32(k, k, k, (byte)(255f * a[j * n + i] + 0.5f));
            }
            t.SetPixels32(px);
            t.Apply(false);
            return t;
        }

        /// <summary>Une texture SANS mipmaps, filtrage simple : posee a sa taille, elle est nette.</summary>
        static Texture2D NewTexture(int w, int h)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        static int warmed;

        /// <summary>
        /// Preparer une icone de plus (a appeler a chaque image dans les menus) : quand la
        /// partie commence, les tailles du HUD sont pretes, pas d'a-coup a la premiere Couronne.
        /// </summary>
        public static void WarmNext()
        {
            if (warmed >= IconArt.Names.Length * 2) return;
            string id = IconArt.Names[warmed % IconArt.Names.Length];
            // Les deux tailles les plus courantes du HUD : les ronds de capacite et le fil.
            int px = Mathf.RoundToInt((warmed < IconArt.Names.Length ? 34f : 60f) * Mathf.Clamp(Screen.height / 900f, 0.8f, 2.4f) * Settings.TextSize);
            warmed++;
            Get(glyphs, id, Mathf.Clamp(px, 4, MaxSize), 0f, true);
            Get(edges, id, Mathf.Clamp(px, 4, MaxSize), EdgeWidth, false);
        }

        // ================================================================== les pastilles

        /// <summary>Un disque plein de "n" pixels, bord lisse d'un pixel.</summary>
        static Texture2D Disc(int n)
        {
            n = Mathf.Clamp(n, 2, 1024);
            Texture2D t;
            if (discs.TryGetValue(n, out t) && t != null) return t;
            t = NewTexture(n, n);
            Color32[] px = new Color32[n * n];
            float r = n * 0.5f;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = i + 0.5f - r, y = j + 0.5f - r;
                    float d = Mathf.Sqrt(x * x + y * y) - r;
                    px[j * n + i] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(0.5f - d)));
                }
            t.SetPixels32(px);
            t.Apply(false);
            if (discs.Count > 400)
            {
                foreach (Texture2D old in discs.Values) if (old != null) Object.Destroy(old);
                discs.Clear();
            }
            discs[n] = t;
            return t;
        }

        /// <summary>
        /// Une gelule de "h" pixels de haut, en "9 tranches" : deux bouts ronds qui ne
        /// bougent pas, une bande du milieu qui s'etire en largeur (sans flou : elle est
        /// uniforme dans ce sens-la).
        /// </summary>
        static GUIStyle Capsule(int h)
        {
            h = Mathf.Clamp(h, 2, 1024);
            GUIStyle s;
            if (capsules.TryGetValue(h, out s) && s != null && s.normal.background != null) return s;
            int w = h + 2;
            Texture2D t = NewTexture(w, h);
            Color32[] px = new Color32[w * h];
            float r = h * 0.5f, a = (w - h) * 0.5f;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float x = Mathf.Max(0f, Mathf.Abs(i + 0.5f - w * 0.5f) - a), y = j + 0.5f - r;
                    float d = Mathf.Sqrt(x * x + y * y) - r;
                    px[j * w + i] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(0.5f - d)));
                }
            t.SetPixels32(px);
            t.Apply(false);
            int uniform = h % 2 == 0 ? 2 : 3;
            s = new GUIStyle();
            s.normal.background = t;
            s.border = new RectOffset((w - uniform) / 2, (w - uniform) / 2, 0, 0);
            if (capsules.Count > 400)
            {
                foreach (GUIStyle old in capsules.Values) if (old != null && old.normal.background != null) Object.Destroy(old.normal.background);
                capsules.Clear();
            }
            capsules[h] = s;
            return s;
        }

        /// <summary>
        /// UNE PASTILLE facon bouton de jeu : une gelule (ou un rond si carre) de la couleur
        /// "fill", un liseré sombre, une ombre franche dessous, le haut plus clair. C'est la
        /// brique de tout le HUD.
        /// </summary>
        public static void Pill(Rect r, Color fill)
        {
            Pill(r, fill, 1f);
        }

        public static void Pill(Rect r, Color fill, float alpha)
        {
            if (!Painting || alpha <= 0.003f) return;
            r = Snap(r);
            if (r.height < 2f || r.width < 2f) return;
            // (04/10) Le liseré et l'ombre suivent le PETIT cote : une pastille haute et fine
            // (une barre de defilement) avait un liseré de 50 pixels.
            float side = Mathf.Min(r.width, r.height);
            int e = Mathf.Max(2, Mathf.RoundToInt(side * 0.07f));
            int drop = Mathf.Max(2, Mathf.RoundToInt(side * 0.09f));
            // L'ombre franche (pas floue : decalee vers le bas), le liseré, le corps.
            Shape(new Rect(r.x - e, r.y - e + drop, r.width + e * 2f, r.height + e * 2f), new Color(Ink.r, Ink.g, Ink.b, 0.45f * alpha));
            Shape(new Rect(r.x - e, r.y - e, r.width + e * 2f, r.height + e * 2f), new Color(Ink.r, Ink.g, Ink.b, alpha));
            Shape(r, new Color(fill.r, fill.g, fill.b, fill.a * alpha));
            // Le bas un peu plus sombre (le volume), le haut plus clair (le reflet).
            int band = Mathf.RoundToInt(r.height * 0.5f);
            if (r.height >= 14f)
            {
                Rect low = new Rect(r.x, r.yMax - band, r.width, band);
                GUI.BeginGroup(low);
                Shape(new Rect(0f, band - r.height, r.width, r.height), new Color(0f, 0f, 0f, 0.14f * alpha));
                GUI.EndGroup();
                int inset = Mathf.Max(2, Mathf.RoundToInt(r.height * 0.1f));
                Rect top = new Rect(r.x + inset * 2f, r.y + inset, r.width - inset * 4f, Mathf.Round(r.height * 0.34f));
                if (top.width > top.height * 0.5f) Shape(top, new Color(1f, 1f, 1f, 0.2f * alpha));
            }
        }

        /// <summary>Une gelule pleine, a la taille exacte, sans rien d'autre.</summary>
        static void Shape(Rect r, Color c)
        {
            r = Snap(r);
            int h = Mathf.RoundToInt(r.height);
            if (h < 2) return;
            Color was = GUI.color;
            GUI.color = c;
            if (r.width <= h + 2.5f) GUI.DrawTexture(r, Disc(h), ScaleMode.StretchToFill, true);
            else Capsule(h).Draw(r, false, false, false, false);
            GUI.color = was;
        }

        /// <summary>Un disque plein (pour une recharge, un point de couleur).</summary>
        public static void Dot(Rect r, Color c)
        {
            if (!Painting) return;
            int px;
            Rect q = Square(r, out px);
            Color was = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(q, Disc(px), ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        /// <summary>
        /// La recharge d'un rond : la part "left" (0-1) encore a attendre, assombrie par le
        /// haut. (Un masque rond, coupe par un rectangle : BeginGroup ne dessine que dedans.)
        /// </summary>
        public static void Cooldown(Rect r, float left)
        {
            if (left <= 0f || !Painting) return;
            int px;
            Rect q = Square(r, out px);
            float h = Mathf.Round(q.height * Mathf.Clamp01(left));
            if (h < 1f) return;
            Color was = GUI.color;
            GUI.color = new Color(0.04f, 0.03f, 0.1f, 0.7f);
            GUI.BeginGroup(new Rect(q.x, q.y, q.width, h));
            GUI.DrawTexture(new Rect(0f, 0f, q.width, q.height), Disc(px), ScaleMode.StretchToFill, true);
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
            Rect cap = new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.08f, r.width * 0.84f, r.height * 0.84f);
            string label = bind;
            float wide = label.Length > 2 ? Mathf.Min(2.2f, 0.6f + label.Length * 0.32f) : 1f;
            if (wide > 1f) cap = new Rect(cap.center.x - cap.width * wide * 0.5f, cap.y, cap.width * wide, cap.height);
            Pill(cap, new Color(0.95f, 0.95f, 0.99f), alpha);
            Text(cap, label, Mathf.RoundToInt(cap.height * (label.Length > 2 ? 0.46f : 0.6f)), new Color(Ink.r, Ink.g, Ink.b, alpha), TextAnchor.MiddleCenter, false);
        }

        /// <summary>
        /// DES CHIFFRES ET DES MOTS facon Fall Guys : gros, blancs, cernes d'un liseré epais
        /// et d'une ombre franche. Pour le chrono, les scores, les titres, les boutons.
        /// </summary>
        public static void Number(Rect r, string digits, int size, Color c, TextAnchor align)
        {
            Text(r, digits, size, c, align, true);
        }

        static readonly GUIContent tmp = new GUIContent();
        static readonly Dictionary<int, Vector2[]> rings = new Dictionary<int, Vector2[]>();

        /// <summary>
        /// Un texte NET : on mesure sa taille, on le place nous-memes sur des pixels
        /// entiers (un texte centre dans un rectangle de hauteur impaire tombait entre
        /// deux pixels : flou), et son liseré est fait de copies decalees d'un nombre
        /// ENTIER de pixels -- chacune nette, leur reunion aussi.
        /// </summary>
        public static void Text(Rect r, string text, int size, Color c, TextAnchor align, bool outline)
        {
            if (!Painting || string.IsNullOrEmpty(text) || c.a <= 0.003f) return;
            GUIStyle s = TextStyle(size);
            tmp.text = text;
            Vector2 sz = s.CalcSize(tmp);
            int col = (int)align % 3, row = (int)align / 3;
            float x = col == 0 ? r.x : col == 1 ? r.x + (r.width - sz.x) * 0.5f : r.xMax - sz.x;
            float y = row == 0 ? r.y : row == 1 ? r.y + (r.height - sz.y) * 0.5f : r.yMax - sz.y;
            Rect t = new Rect(Mathf.Round(x), Mathf.Round(y), Mathf.Ceil(sz.x) + 2f, Mathf.Ceil(sz.y) + 2f);
            Color was = GUI.color;
            if (outline)
            {
                int o = Mathf.Max(2, Mathf.RoundToInt(size * 0.08f));
                int drop = Mathf.Max(2, Mathf.RoundToInt(size * 0.07f));
                GUI.color = new Color(Ink.r, Ink.g, Ink.b, Ink.a * c.a);
                Vector2[] ring = Ring(o);
                // L'ombre franche dessous : la moitie basse de l'anneau suffit (le haut est cache).
                for (int k = 0; k < ring.Length; k++)
                    if (ring[k].y >= 0f) GUI.Label(new Rect(t.x + ring[k].x, t.y + ring[k].y + drop, t.width, t.height), tmp, s);
                for (int k = 0; k < ring.Length; k++)
                    GUI.Label(new Rect(t.x + ring[k].x, t.y + ring[k].y, t.width, t.height), tmp, s);
            }
            GUI.color = c;
            GUI.Label(t, tmp, s);
            GUI.color = was;
        }

        /// <summary>Les decalages ENTIERS d'un cercle de rayon "o" (sans doublons), pour le liseré.</summary>
        static Vector2[] Ring(int o)
        {
            Vector2[] ring;
            if (rings.TryGetValue(o, out ring)) return ring;
            List<Vector2> list = new List<Vector2>();
            int steps = o <= 2 ? 12 : 16;
            for (int k = 0; k < steps; k++)
            {
                float a = k * Mathf.PI * 2f / steps;
                Vector2 v = new Vector2(Mathf.Round(Mathf.Cos(a) * o), Mathf.Round(Mathf.Sin(a) * o));
                if (!list.Contains(v)) list.Add(v);
            }
            // Un anneau interieur pour les gros liserés (sinon des trous entre les copies).
            if (o >= 4)
                for (int k = 0; k < 8; k++)
                {
                    float a = (k + 0.5f) * Mathf.PI * 0.25f;
                    Vector2 v = new Vector2(Mathf.Round(Mathf.Cos(a) * o * 0.55f), Mathf.Round(Mathf.Sin(a) * o * 0.55f));
                    if (!list.Contains(v)) list.Add(v);
                }
            ring = list.ToArray();
            rings[o] = ring;
            return ring;
        }

        static readonly Dictionary<int, GUIStyle> textStyles = new Dictionary<int, GUIStyle>();
        static GUIStyle TextStyle(int size)
        {
            size = Mathf.Max(6, size);
            GUIStyle s;
            if (textStyles.TryGetValue(size, out s) && s != null) return s;
            UiStyle.Ensure();
            s = new GUIStyle(UiStyle.Title);
            s.fontSize = size;
            s.alignment = TextAnchor.UpperLeft;
            s.normal.textColor = Color.white;
            s.wordWrap = false;
            s.clipping = TextClipping.Overflow;
            s.padding = new RectOffset(0, 0, 0, 0);
            s.margin = new RectOffset(0, 0, 0, 0);
            textStyles[size] = s;
            return s;
        }

        /// <summary>La largeur d'un texte a cette taille (pour ajuster une pastille).</summary>
        public static float Width(string text, int size)
        {
            tmp.text = text;
            return TextStyle(size).CalcSize(tmp).x;
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
                case Ability.Meteore: return "meteore";
                case Ability.Tornade: return "tornade";
                case Ability.TrouNoir: return "trou-noir";
                case Ability.Boulet: return "boulet";
                case Ability.Geant: return "geant";
                case Ability.Fusee: return "fusee";
                case Ability.Ressort: return "ressort";
                case Ability.Foudre: return "foudre";
                case Ability.Riposte: return "riposte";
                case Ability.Vampire: return "vampire";
                case Ability.TeteDure: return "tete-dure";
                case Ability.SecondSouffle: return "second-souffle";
                case Ability.Plume: return "plume";
                case Ability.Prison: return "prison";
                case Ability.Bombe: return "bombe";
                case Ability.Inversion: return "inversion";
                case Ability.Mini: return "mini";
                case Ability.Glu: return "glu";
                case Ability.Banane: return "banane";
                case Ability.Ballon: return "ballon";
                case Ability.Seisme: return "seisme";
                case Ability.Gant: return "gant";
                case Ability.Fantome: return "fantome";
                case Ability.Taupe: return "taupe";
                case Ability.Deluge: return "deluge";
                case Ability.Toupie: return "toupie";
                case Ability.Encre: return "encre";
                case Ability.BrasLongs: return "bras-longs";
                case Ability.Kangourou: return "kangourou";
                case Ability.Kamikaze: return "kamikaze";
                case Ability.AngeGardien: return "ange";
                case Ability.Rage: return "rage";
                case Ability.Ninja: return "ninja";
                case Ability.Lasso: return "lasso";
                case Ability.Missile: return "missile";
                case Ability.Apesanteur: return "apesanteur";
                case Ability.Flammes: return "flammes";
                case Ability.Pogo: return "pogo";
                case Ability.Geyser: return "geyser";
                case Ability.Boomerang: return "boomerang";
                case Ability.PiegeLoup: return "piege";
                case Ability.Catapulte: return "catapulte";
                case Ability.Oreillers: return "oreiller";
                case Ability.Hypnose: return "hypnose";
                case Ability.Raz: return "raz";
                case Ability.CoupDePied: return "coup-de-pied";
                case Ability.Cri: return "cri";
                case Ability.Miroir: return "miroir";
                case Ability.Increvable: return "increvable";
                case Ability.Pickpocket: return "pickpocket";
                case Ability.Chanceux: return "chanceux";
                case Ability.Armure: return "armure";
                case Ability.Sprinter: return "sprinter";
                case Ability.Apocalypse: return "apocalypse";
                case Ability.ArretTemps: return "arret-temps";
                case Ability.Rayon: return "rayon";
                case Ability.Teleport: return "teleport";
                case Ability.Tempete: return "tempete";
                case Ability.Nuke: return "nuke";
                case Ability.MainDeDieu: return "main-dieu";
                case Ability.Essaim: return "essaim";
                case Ability.GraviteZero: return "gravite";
                case Ability.Invincible: return "invincible";
                case Ability.Colosse: return "colosse";
                case Ability.Eclair: return "eclair";
                case Ability.MainLourde: return "main-lourde";
                case Ability.Phenix: return "phenix";
                default: return "sablier";
            }
        }
    }
}
