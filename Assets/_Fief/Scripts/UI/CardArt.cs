using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CARTES DE CAPACITE, facon jeu de cartes moderne (refaites le 01/10 -- Martin :
    /// "c'est moche, il y a un effet de flou bizarre, et le texte est coupe").
    ///
    ///   - elles arrivent FACE CACHEE et se RETOURNENT une a une (un dos bleu nuit, la
    ///     Couronne d'or) ;
    ///   - la face : un corps arrondi a la couleur de la capacite, une FENETRE ou la grosse
    ///     icone brille sur des rayons, le NOM sur un bandeau sombre, et la PHRASE en encre
    ///     sombre sur un cartouche clair -- elle rapetisse s'il le faut, elle n'est JAMAIS
    ///     coupee ; en haut, la touche et la recharge en pastilles ;
    ///   - celle qu'on vise grandit (on redessine plus grand, on n'etire pas : c'etait ce
    ///     zoom qui floutait tout), s'entoure d'un halo et de rayons ;
    ///   - la prendre : un eclair, une gerbe d'etincelles.
    ///
    /// Plus de grain de pierre : une petite image de bruit etiree a la taille de la carte,
    /// c'etait l'"effet de flou bizarre".
    ///
    /// Concept Unity : un GUIStyle "9 tranches" (border) garde ses quatre coins a leur
    /// taille exacte et n'etire que les bords et le milieu : un seul petit rectangle
    /// arrondi dessine n'importe quelle carte, coins nets.
    /// </summary>
    public static class CardArt
    {
        static Texture2D rays, glow, grad;

        // --- les etincelles (des petits points de lumiere qui montent)
        static readonly List<Vector2> sparkPos = new List<Vector2>();
        static readonly List<Vector2> sparkVel = new List<Vector2>();
        static readonly List<float> sparkLife = new List<float>();
        static readonly List<Color> sparkColour = new List<Color>();
        static Rect flashRect;
        static float flash;
        static Color flashColour;
        static float lastTick;

        public static readonly Color Gold = new Color(1f, 0.8f, 0.42f);
        static readonly Color Cream = new Color(0.98f, 0.96f, 0.91f);
        static readonly Color DarkInk = new Color(0.13f, 0.1f, 0.24f);

        public static void Ensure()
        {
            if (rays != null) return;
            // Les rayons : douze pinceaux de lumiere qui s'effacent vers l'exterieur.
            const int R = 256;
            rays = New(R, R);
            Color32[] px = new Color32[R * R];
            for (int y = 0; y < R; y++)
                for (int x = 0; x < R; x++)
                {
                    float dx = (x + 0.5f) / R * 2f - 1f, dy = (y + 0.5f) / R * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float beam = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang * 6f)), 6f);
                    float a = beam * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
                    px[y * R + x] = new Color32(255, 255, 255, (byte)(255f * a));
                }
            rays.SetPixels32(px);
            rays.Apply(false);

            const int G = 64;
            glow = New(G, G);
            px = new Color32[G * G];
            for (int y = 0; y < G; y++)
                for (int x = 0; x < G; x++)
                {
                    float dx = (x + 0.5f) / G * 2f - 1f, dy = (y + 0.5f) / G * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * G + x] = new Color32(255, 255, 255, (byte)(255f * (1f - d) * (1f - d)));
                }
            glow.SetPixels32(px);
            glow.Apply(false);
            grad = New(1, 64);
            for (int y = 0; y < 64; y++) grad.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Pow(y / 63f, 1.6f)));
            grad.Apply(false);
        }

        static Texture2D New(int w, int h)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        static void Tex(Rect r, Texture2D t, Color c)
        {
            Color was = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        // ================================================================== le rectangle arrondi

        static readonly Dictionary<int, GUIStyle> rounds = new Dictionary<int, GUIStyle>();

        /// <summary>Un rectangle aux coins arrondis de "radius" pixels (9 tranches). "shade" : plus clair en haut.</summary>
        static GUIStyle RoundStyle(int radius, bool shade)
        {
            radius = Mathf.Clamp(radius, 2, 64);
            int key = radius * 2 + (shade ? 1 : 0);
            GUIStyle s;
            if (rounds.TryGetValue(key, out s) && s != null && s.normal.background != null) return s;
            int w = radius * 2 + 4, h = shade ? radius * 2 + 96 : w;
            Texture2D t = New(w, h);
            Color32[] px = new Color32[w * h];
            float hx = w * 0.5f - radius, hy = h * 0.5f - radius;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float qx = Mathf.Abs(i + 0.5f - w * 0.5f) - hx, qy = Mathf.Abs(j + 0.5f - h * 0.5f) - hy;
                    float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    // (ligne 0 = le BAS de la texture) : le haut plus clair que le bas.
                    byte k = (byte)(255f * (shade ? Mathf.Lerp(0.72f, 1f, j / (float)(h - 1)) : 1f));
                    px[j * w + i] = new Color32(k, k, k, (byte)(255f * Mathf.Clamp01(0.5f - d)));
                }
            t.SetPixels32(px);
            t.Apply(false);
            s = new GUIStyle();
            s.normal.background = t;
            s.border = new RectOffset(radius + 1, radius + 1, radius + 1, radius + 1);
            rounds[key] = s;
            return s;
        }

        static void Round(Rect r, Color c, int radius, bool shade)
        {
            if (Event.current.type != EventType.Repaint || c.a <= 0.003f) return;
            r = Icons.Snap(r);
            if (r.width < radius * 2 + 2 || r.height < radius * 2 + 2) radius = Mathf.Max(2, Mathf.FloorToInt(Mathf.Min(r.width, r.height) * 0.5f) - 1);
            Color was = GUI.color;
            GUI.color = c;
            RoundStyle(radius, shade).Draw(r, false, false, false, false);
            GUI.color = was;
        }

        /// <summary>(v37.3) Un panneau aux coins arrondis (la bulle des capacites d'un joueur, au choix).</summary>
        public static void Panel(Rect r, Color c, int radius) { Ensure(); Round(r, c, radius, false); }

        static Rect Grow(Rect r, float by)
        {
            return new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
        }

        // ================================================================== le fond

        /// <summary>Le fond de l'ecran de choix : deux gerbes de rayons qui tournent lentement, un voile, des braises.</summary>
        public static void Background(Color tint) { Background(tint, 0f); }

        /// <summary>
        /// "scrim" (0-1) : un voile sombre sur le monde, plus epais en haut et en bas
        /// (29/09 -- le monde en 3D derriere les cartes, c'etait le bazar).
        /// </summary>
        public static void Background(Color tint, float scrim)
        {
            Ensure();
            float time = Time.unscaledTime;
            if (scrim > 0f)
            {
                Rect all = new Rect(0f, 0f, Screen.width, Screen.height);
                UiStyle.Fill(all, new Color(0.05f, 0.05f, 0.14f, scrim));
                Tex(new Rect(0f, 0f, Screen.width, Screen.height * 0.35f), grad, new Color(0f, 0f, 0.02f, scrim * 0.5f));
                Matrix4x4 up = GUI.matrix;
                Rect low = new Rect(0f, Screen.height * 0.65f, Screen.width, Screen.height * 0.35f);
                GUIUtility.ScaleAroundPivot(new Vector2(1f, -1f), low.center);
                Tex(low, grad, new Color(0f, 0f, 0.02f, scrim * 0.5f));
                GUI.matrix = up;
            }
            Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.55f);
            float size = Mathf.Max(Screen.width, Screen.height) * 1.5f;
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(time * 4f, c);
            Tex(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), rays, new Color(tint.r, tint.g, tint.b, 0.09f));
            GUI.matrix = m;
            GUIUtility.RotateAroundPivot(-time * 2.5f + 15f, c);
            Tex(new Rect(c.x - size * 0.4f, c.y - size * 0.4f, size * 0.8f, size * 0.8f), rays, new Color(1f, 0.95f, 0.85f, 0.05f));
            GUI.matrix = m;
            // Des braises qui montent du bas de l'ecran.
            if (Random.value < 0.35f) Emit(new Vector2(Random.value * Screen.width, Screen.height + 10f), new Vector2(Random.Range(-10f, 10f), Random.Range(-60f, -25f)), 4f, new Color(1f, 0.8f, 0.45f, 0.7f));
        }

        // ================================================================== une carte

        /// <summary>
        /// UNE CARTE. "enter" : 0 (face cachee, pas encore arrivee) a 1 (retournee) ;
        /// "lift" : 0 a 1 (visee). "cooldown" : 0 pour une passive ; "key" : la touche de
        /// l'active (null pour une passive).
        /// </summary>
        public static void Draw(Rect card, Color tint, string name, string line, float cooldown, string key, string replaces,
                                bool owned, bool on, float lift, float enter, string icon)
        {
            Draw(card, tint, name, line, cooldown, key, replaces, owned, on, lift, enter, icon, -1);
        }

        /// <summary>(v41) La carte avec son RANG (-1 : sans) : cadre, joyau et ruban a sa couleur.</summary>
        public static void Draw(Rect card, Color tint, string name, string line, float cooldown, string key, string replaces,
                                bool owned, bool on, float lift, float enter, string icon, int tier)
        {
            Ensure();
            if (Event.current.type != EventType.Repaint) return;
            float time = Time.unscaledTime;
            float turn = Mathf.Clamp01(enter);
            float sx = Mathf.Abs(Mathf.Cos(turn * Mathf.PI));
            bool showFace = turn >= 0.5f;
            float fade = owned ? 0.55f : 1f;
            // (10/10 -- Martin : "quand on clique sur la carte, le texte bouge, nos yeux bougent")
            // La carte visee ne grandit plus et ne monte plus : rien de ce qu'on lit ne bouge.
            // Ce qui dit "c'est celle-la" : le halo, les rayons derriere, le cadre d'or epais.
            card = Icons.Snap(card);
            int radius = Mathf.Clamp(Mathf.RoundToInt(card.width * 0.08f), 6, 26);
            int line4 = Mathf.Max(3, UiStyle.S(4));

            // Derriere la carte visee : des rayons qui tournent et un halo.
            if (lift > 0.01f && showFace)
            {
                Vector2 c = card.center;
                float size = card.height * 2.2f;
                Matrix4x4 was = GUI.matrix;
                GUIUtility.RotateAroundPivot(time * 25f, c);
                Tex(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), rays, new Color(tint.r, tint.g, tint.b, 0.5f * lift));
                GUI.matrix = was;
                float g = card.width * 0.35f;
                Tex(new Rect(card.x - g, card.y - g, card.width + g * 2f, card.height + g * 2f), glow, new Color(tint.r, tint.g, tint.b, 0.55f * lift));
                if (Random.value < 0.5f * lift)
                    Emit(new Vector2(card.x + Random.value * card.width, card.yMax - Random.value * 20f), new Vector2(Random.Range(-15f, 15f), Random.Range(-90f, -40f)), 1.2f, Color.Lerp(tint, Color.white, 0.4f));
            }

            // Le retournement : la carte s'amincit jusqu'a la tranche, puis revient de face.
            // (Seulement pendant ce demi-temps : une carte posee n'est jamais etiree.)
            Matrix4x4 keep = GUI.matrix;
            bool flipping = sx < 0.995f;
            if (flipping) GUIUtility.ScaleAroundPivot(new Vector2(Mathf.Max(0.02f, sx), 1f), card.center);

            // L'ombre franche, le liseré sombre.
            Round(new Rect(card.x, card.y + UiStyle.S(9), card.width, card.height), new Color(0f, 0f, 0.05f, 0.4f), radius, false);
            if (on) Round(Grow(card, line4 * 2f), new Color(1f, 0.9f, 0.45f, 0.5f + 0.5f * lift), radius + line4 * 2, false);
            Color rank = tier >= 0 ? AbilityInfo.TierColour(tier) : Icons.Ink;
            if (owned && tier >= 0) rank = Color.Lerp(rank, new Color(0.35f, 0.35f, 0.42f), 0.6f);   // (v43) deja a toi : le rang s'eteint
            // (v41) Le cadre a la couleur du rang ; une legendaire et une divine rayonnent toujours un peu.
            if (tier >= 3 && turn >= 0.5f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3f);
                float gg = card.width * 0.22f;
                Tex(new Rect(card.x - gg, card.y - gg, card.width + gg * 2f, card.height + gg * 2f), glow, new Color(rank.r, rank.g, rank.b, (tier == 4 ? 0.45f : 0.3f) + 0.15f * pulse));
            }
            if (tier >= 0 && !on) Round(Grow(card, line4 * 1.6f), new Color(rank.r * 0.55f, rank.g * 0.55f, rank.b * 0.55f), radius + Mathf.RoundToInt(line4 * 1.6f), false);
            Round(Grow(card, line4), on ? new Color(1f, 0.9f, 0.45f) : tier >= 0 ? rank : Icons.Ink, radius + line4, false);

            if (!showFace)
            {
                // LE DOS : bleu nuit, un rond d'or, la Couronne.
                Round(card, new Color(0.2f, 0.24f, 0.55f), radius, true);
                Round(Grow(card, -line4 * 2f), new Color(0.13f, 0.15f, 0.38f), Mathf.Max(4, radius - line4), true);
                float d = card.width * 0.56f;
                Rect disc = new Rect(card.center.x - d * 0.5f, card.center.y - d * 0.5f, d, d);
                Icons.Pill(disc, new Color(0.3f, 0.35f, 0.8f));
                Icons.Draw(Grow(disc, -d * 0.18f), "couronne", new Color(1f, 0.84f, 0.36f));
                GUI.matrix = keep;
                return;
            }

            // LE CORPS a sa couleur (plus clair en haut).
            Color body = Color.Lerp(tint, new Color(0.1f, 0.08f, 0.2f), 0.25f);
            Round(card, new Color(body.r, body.g, body.b, 1f), radius, true);

            float pad = Mathf.Round(card.width * 0.07f);
            float w = card.width - pad * 2f;

            // LA FENETRE : un creux sombre ou l'icone brille sur ses rayons.
            Rect window = new Rect(card.x + pad, card.y + pad, w, Mathf.Round(card.height * 0.44f));
            Round(window, new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.45f, 1f), Mathf.Max(4, radius - 3), false);
            GUI.BeginGroup(window);
            {
                Vector2 wc = new Vector2(window.width * 0.5f, window.height * 0.55f);
                float rs = window.width * 1.6f;
                Matrix4x4 rm = GUI.matrix;
                GUIUtility.RotateAroundPivot(time * (on ? 30f : 8f), wc);
                Tex(new Rect(wc.x - rs * 0.5f, wc.y - rs * 0.5f, rs, rs), rays, new Color(1f, 1f, 1f, on ? 0.3f : 0.16f));
                GUI.matrix = rm;
                float gs = window.height * 1.1f;
                Tex(new Rect(wc.x - gs * 0.5f, wc.y - gs * 0.5f, gs, gs), glow, new Color(tint.r, tint.g, tint.b, 0.9f));
            }
            GUI.EndGroup();
            if (icon != null)
            {
                float s = Mathf.Min(window.width * 0.62f, window.height * 0.78f);
                float bob = 0f;     // (10/10) l'icone ne danse plus non plus
                Icons.Draw(new Rect(window.center.x - s * 0.5f, window.y + window.height * 0.55f - s * 0.5f + bob, s, s), icon, Color.white);
            }

            // En haut de la fenetre : la touche (pour une active) et la recharge.
            float chip = Mathf.Round(Mathf.Clamp(card.width * 0.17f, UiStyle.S(26), UiStyle.S(40)));
            if (key != null) Icons.Key(new Rect(window.x + UiStyle.S(4), window.y + UiStyle.S(4), chip, chip), key, 1f);
            if (cooldown > 0f)
            {
                string cd = cooldown.ToString("0") + "s";
                int cs = Mathf.RoundToInt(chip * 0.5f);
                float cw = Mathf.Max(chip, Icons.Width(cd, cs) + chip * 0.5f);
                Rect cr = new Rect(window.xMax - cw - UiStyle.S(6), window.y + UiStyle.S(6), cw, chip * 0.8f);
                Icons.Pill(cr, new Color(0.14f, 0.13f, 0.3f));
                Icons.Number(cr, cd, cs, Color.white, TextAnchor.MiddleCenter);
            }

            // LE NOM, sur un bandeau sombre a cheval sur le bas de la fenetre.
            float bh = Mathf.Round(Mathf.Clamp(card.height * 0.1f, UiStyle.S(30), UiStyle.S(46)));
            Rect band = new Rect(card.x + pad * 0.5f, window.yMax - bh * 0.45f, card.width - pad, bh);
            Icons.Pill(band, new Color(0.12f, 0.1f, 0.26f));
            int ns = Mathf.RoundToInt(bh * 0.62f);
            while (ns > 10 && Icons.Width(name, ns) > band.width - bh * 0.6f) ns--;
            Icons.Number(band, name, ns, on ? new Color(1f, 0.9f, 0.45f) : Color.white, TextAnchor.MiddleCenter);

            // LA PHRASE, en encre sombre sur un cartouche clair : elle rapetisse, elle n'est
            // jamais coupee.
            float ty = band.yMax + UiStyle.S(8);
            Rect plate = new Rect(card.x + pad, ty, w, card.yMax - pad - ty - (replaces != null ? UiStyle.S(30) : 0f));
            if (plate.height > 8f)
            {
                Round(plate, Cream, Mathf.Max(4, radius - 4), false);
                Rect inside = new Rect(plate.x + UiStyle.S(8), plate.y + UiStyle.S(6), plate.width - UiStyle.S(16), plate.height - UiStyle.S(12));
                GUIStyle ds = FitStyle(line, inside.width, inside.height);
                GUIContent content = new GUIContent(line);
                float th = ds.CalcHeight(content, inside.width);
                Rect tr = Icons.Snap(new Rect(inside.x, inside.y + Mathf.Max(0f, (inside.height - th) * 0.5f), inside.width, Mathf.Max(th, inside.height)));
                Color was = GUI.color;
                GUI.color = Color.white;
                ds.normal.textColor = DarkInk;
                GUI.Label(tr, content, ds);
                GUI.color = was;
            }

            // Ce qu'elle remplace : une pastille rouge, une croix, le nom.
            if (replaces != null)
            {
                Rect rb = new Rect(card.x + pad, card.yMax - pad - UiStyle.S(24), w, UiStyle.S(24));
                Icons.Pill(rb, new Color(0.8f, 0.2f, 0.18f));
                Icons.Draw(new Rect(rb.x + UiStyle.S(4), rb.y + UiStyle.S(2), rb.height - UiStyle.S(4), rb.height - UiStyle.S(4)), "croix", Color.white);
                Icons.Number(new Rect(rb.x + rb.height, rb.y, rb.width - rb.height * 1.3f, rb.height), replaces, Mathf.RoundToInt(rb.height * 0.56f), Color.white, TextAnchor.MiddleCenter);
            }

            // (v41) LE JOYAU DU RANG, a cheval sur le haut de la carte, et son nom sur un ruban.
            if (tier >= 0)
            {
                int rs = Mathf.RoundToInt(Mathf.Clamp(card.width * 0.075f, UiStyle.S(11), UiStyle.S(15)));
                string rn = AbilityInfo.TierName(tier);
                float rw = Icons.Width(rn, rs) + rs * 2.2f;
                float rh = Mathf.Round(rs * 1.7f);
                Rect ribbon = Icons.Snap(new Rect(card.center.x - rw * 0.5f, card.y - rh * 0.55f, rw, rh));
                Icons.Pill(Grow(ribbon, 2f), new Color(0.08f, 0.06f, 0.16f));
                Icons.Pill(ribbon, new Color(rank.r * 0.85f, rank.g * 0.85f, rank.b * 0.85f));
                Icons.Number(ribbon, rn, rs, Color.white, TextAnchor.MiddleCenter);
                // Les gemmes : une par rang au-dessus de "commune", sous le ruban.
                float gem = Mathf.Round(rs * 0.8f);
                float gx = card.center.x - (tier * gem + (tier - 1) * gem * 0.4f) * 0.5f;
                for (int k = 0; k < tier; k++)
                {
                    Rect gr = Icons.Snap(new Rect(gx + k * gem * 1.4f, ribbon.yMax + UiStyle.S(3), gem, gem));
                    Icons.Pill(Grow(gr, 1f), new Color(0.08f, 0.06f, 0.16f));
                    Icons.Pill(gr, Color.Lerp(rank, Color.white, 0.25f));
                }
            }

            // Le reflet qui traverse la carte visee (et, v41, toujours celui des legendaires et divines).
            if (on || tier >= 3)
            {
                float sweep = Mathf.Repeat(time * 0.7f, 1.8f) - 0.4f;
                Matrix4x4 rm = GUI.matrix;
                GUI.BeginGroup(card);
                GUIUtility.RotateAroundPivot(18f, new Vector2(card.width * 0.5f, card.height * 0.5f));
                Tex(new Rect(card.width * sweep - card.width * 0.15f, -card.height * 0.2f, card.width * 0.3f, card.height * 1.4f), glow, new Color(1f, 1f, 1f, 0.18f));
                GUI.EndGroup();
                GUI.matrix = rm;
            }

            // "Deja a toi" : grisee, une grosse coche.
            if (owned)
            {
                Round(card, new Color(0.05f, 0.05f, 0.1f, 1f - fade), radius, false);
                float s = card.width * 0.4f;
                Icons.Pill(new Rect(card.center.x - s * 0.5f, card.center.y - s * 0.5f, s, s), new Color(0.3f, 0.75f, 0.35f));
                Icons.Draw(new Rect(card.center.x - s * 0.33f, card.center.y - s * 0.33f, s * 0.66f, s * 0.66f), "coche", Color.white);
            }
            GUI.matrix = keep;
        }

        static readonly Dictionary<int, GUIStyle> fitStyles = new Dictionary<int, GUIStyle>();

        /// <summary>La plus grande taille de la phrase qui tient dans le cartouche (de 19 a 10 points).</summary>
        static GUIStyle FitStyle(string line, float w, float h)
        {
            GUIContent c = new GUIContent(line);
            GUIStyle s = null;
            for (int size = UiStyle.S(19); size >= Mathf.Max(9, UiStyle.S(10)); size--)
            {
                s = DescStyle(size);
                if (s.CalcHeight(c, w) <= h) return s;
            }
            return s ?? DescStyle(UiStyle.S(12));
        }

        static GUIStyle DescStyle(int size)
        {
            GUIStyle s;
            if (fitStyles.TryGetValue(size, out s) && s != null) return s;
            UiStyle.Ensure();
            s = new GUIStyle(UiStyle.Label);
            s.fontSize = size;
            s.wordWrap = true;
            s.alignment = TextAnchor.UpperCenter;
            s.clipping = TextClipping.Overflow;
            s.padding = new RectOffset(0, 0, 0, 0);
            s.normal.textColor = DarkInk;
            fitStyles[size] = s;
            return s;
        }

        /// <summary>LA CARTE PRISE : un eclair, une gerbe d'etincelles.</summary>
        public static void Taken(Rect card, Color tint, bool mine)
        {
            flashRect = card;
            flash = mine ? 0.9f : 0.5f;
            flashColour = Color.Lerp(tint, Color.white, 0.6f);
            int n = mine ? 90 : 35;
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                float sp = Random.Range(80f, mine ? 520f : 300f);
                Emit(card.center + new Vector2(Random.Range(-card.width, card.width), Random.Range(-card.height, card.height)) * 0.4f,
                     new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, Random.Range(0.5f, 1.2f), Color.Lerp(tint, Color.white, Random.value * 0.6f));
            }
            if (mine) Sfx.Pop();
        }

        public static void Emit(Vector2 at, Vector2 velocity, float life, Color c)
        {
            if (sparkPos.Count > 400) return;
            sparkPos.Add(at);
            sparkVel.Add(velocity);
            sparkLife.Add(life);
            sparkColour.Add(c);
        }

        /// <summary>Les etincelles : a appeler une fois par image, par-dessus tout.</summary>
        public static void Sparks()
        {
            Ensure();
            if (Event.current.type != EventType.Repaint) return;
            float now = Time.unscaledTime;
            float dt = Mathf.Clamp(now - lastTick, 0f, 0.1f);
            lastTick = now;
            if (flash > 0f)
            {
                // L'eclair de la carte prise : sa place s'illumine et s'envole.
                flash = Mathf.Max(0f, flash - dt * 2.2f);
                float rise = (1f - flash) * UiStyle.S(60);
                Rect r = new Rect(flashRect.x, flashRect.y - rise, flashRect.width, flashRect.height);
                float g = r.width * 0.5f;
                Tex(new Rect(r.x - g, r.y - g, r.width + g * 2f, r.height + g * 2f), glow, new Color(flashColour.r, flashColour.g, flashColour.b, flash * 0.8f));
                Round(r, new Color(flashColour.r, flashColour.g, flashColour.b, flash * 0.7f), Mathf.Clamp(Mathf.RoundToInt(r.width * 0.08f), 6, 26), false);
            }
            for (int i = sparkPos.Count - 1; i >= 0; i--)
            {
                sparkLife[i] -= dt;
                if (sparkLife[i] <= 0f)
                {
                    sparkPos.RemoveAt(i);
                    sparkVel.RemoveAt(i);
                    sparkLife.RemoveAt(i);
                    sparkColour.RemoveAt(i);
                    continue;
                }
                Vector2 v = sparkVel[i] * (1f - dt * 1.5f) + new Vector2(0f, -20f) * dt;
                sparkVel[i] = v;
                sparkPos[i] = sparkPos[i] + v * dt;
                float s = UiStyle.S(3) + sparkLife[i] * UiStyle.S(5);
                Color c = sparkColour[i];
                Tex(new Rect(sparkPos[i].x - s, sparkPos[i].y - s, s * 2f, s * 2f), glow, new Color(c.r, c.g, c.b, Mathf.Clamp01(sparkLife[i]) * c.a));
            }
        }
    }
}
