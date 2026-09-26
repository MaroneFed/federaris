using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CARTES DE CAPACITE, dessinees comme de vraies cartes (28/09 -- Martin :
    /// "les cartes sont mal faites, c'est pas beau ; qu'il y ait plus d'effets").
    ///
    ///   - elles arrivent FACE CACHEE et se RETOURNENT une a une (un dos de velours
    ///     sombre, un losange d'or) ;
    ///   - la face : un fond de pierre granuleux, un lavis a la couleur de la
    ///     capacite, un CADRE D'OR a double filet avec des coins ouvrages, un ruban
    ///     en haut (ACTIVE · clic gauche / PASSIVE), le nom en grand, un fleuron, la
    ///     phrase, la recharge ;
    ///   - celle qu'on vise se souleve, grossit, s'entoure d'un halo et de RAYONS de
    ///     lumiere qui tournent, et des etincelles montent autour ;
    ///   - la prendre : un eclair blanc, une gerbe d'etincelles, un coup de phonk.
    ///
    /// Aucune icone : que de la matiere, de la lumiere et des mots.
    ///
    /// Concept Unity : ces textures sont FABRIQUEES pixel par pixel au lancement
    /// (Texture2D.SetPixel). Le cadre est dessine en "9 tranches" : un GUIStyle avec
    /// une bordure (border) etire le milieu sans deformer les coins.
    /// </summary>
    public static class CardArt
    {
        static Texture2D face, back, frame, rays, glow, grad;
        static GUIStyle frameStyle;

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

        public static void Ensure()
        {
            if (face != null) return;
            System.Random rng = new System.Random(77);
            // La face : une pierre claire granuleuse, plus sombre sur les bords.
            face = new Texture2D(128, 192, TextureFormat.RGBA32, false);
            back = new Texture2D(128, 192, TextureFormat.RGBA32, false);
            for (int y = 0; y < 192; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = x / 127f, v = y / 191f;
                    float vig = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Max(Mathf.Abs(u - 0.5f) * 2f, Mathf.Abs(v - 0.5f) * 2f), 4f) * 0.6f);
                    float n = 0.82f + (float)rng.NextDouble() * 0.18f;
                    float g = n * vig;
                    face.SetPixel(x, y, new Color(g, g, g, 1f));
                    // Le dos : un velours sombre et un treillis de losanges.
                    float lattice = Mathf.Abs(Mathf.Repeat((u * 128f + v * 192f) / 16f, 1f) - 0.5f) < 0.04f
                                 || Mathf.Abs(Mathf.Repeat((u * 128f - v * 192f) / 16f, 1f) - 0.5f) < 0.04f ? 1f : 0f;
                    float b = (0.09f + 0.03f * n) * vig;
                    back.SetPixel(x, y, new Color(b + lattice * 0.18f, b * 0.8f + lattice * 0.13f, b * 1.1f + lattice * 0.05f, 1f));
                }
            face.Apply();
            back.Apply();

            // Le cadre (9 tranches) : un filet epais, un filet fin a l'interieur, des coins ouvrages.
            const int F = 96;
            frame = new Texture2D(F, F, TextureFormat.RGBA32, false);
            for (int y = 0; y < F; y++)
                for (int x = 0; x < F; x++)
                {
                    int dx = Mathf.Min(x, F - 1 - x), dy = Mathf.Min(y, F - 1 - y);
                    int d = Mathf.Min(dx, dy);
                    float a = 0f;
                    if (d <= 2) a = 1f;                                   // filet exterieur
                    else if (d == 6) a = 0.75f;                           // filet interieur
                    // Les coins : un quart de cercle et un losange.
                    if (dx < 26 && dy < 26)
                    {
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (Mathf.Abs(r - 18f) < 1.2f && dx > 6 && dy > 6) a = 1f;
                        if (Mathf.Abs(dx - 11) + Mathf.Abs(dy - 11) < 4) a = 1f;
                        if ((dx == 6 || dy == 6) && dx < 22 && dy < 22) a = 1f;
                    }
                    frame.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            frame.Apply();
            frameStyle = new GUIStyle();
            frameStyle.normal.background = frame;
            frameStyle.border = new RectOffset(30, 30, 30, 30);

            // Les rayons : douze pinceaux de lumiere qui s'effacent vers l'exterieur.
            const int R = 256;
            rays = new Texture2D(R, R, TextureFormat.RGBA32, false);
            for (int y = 0; y < R; y++)
                for (int x = 0; x < R; x++)
                {
                    float dx = (x + 0.5f) / R * 2f - 1f, dy = (y + 0.5f) / R * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float beam = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang * 6f)), 6f);
                    float a = beam * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
                    rays.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            rays.Apply();

            const int G = 64;
            glow = new Texture2D(G, G, TextureFormat.RGBA32, false);
            for (int y = 0; y < G; y++)
                for (int x = 0; x < G; x++)
                {
                    float dx = (x + 0.5f) / G * 2f - 1f, dy = (y + 0.5f) / G * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    glow.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(1f - d, 2f)));
                }
            glow.Apply();
            grad = new Texture2D(1, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) grad.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Pow(y / 63f, 1.6f)));
            grad.Apply();
            foreach (Texture2D t in new[] { face, back, frame, rays, glow, grad }) { t.wrapMode = TextureWrapMode.Clamp; t.hideFlags = HideFlags.HideAndDontSave; }
        }

        static void Tex(Rect r, Texture2D t, Color c)
        {
            Color was = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true);
            GUI.color = was;
        }

        /// <summary>Le fond de l'ecran de choix : deux gerbes de rayons qui tournent lentement, un voile, des braises.</summary>
        public static void Background(Color tint)
        {
            Ensure();
            float time = Time.unscaledTime;
            Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.55f);
            float size = Mathf.Max(Screen.width, Screen.height) * 1.5f;
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(time * 4f, c);
            Tex(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), rays, new Color(tint.r, tint.g, tint.b, 0.07f));
            GUI.matrix = m;
            GUIUtility.RotateAroundPivot(-time * 2.5f + 15f, c);
            Tex(new Rect(c.x - size * 0.4f, c.y - size * 0.4f, size * 0.8f, size * 0.8f), rays, new Color(1f, 0.9f, 0.7f, 0.05f));
            GUI.matrix = m;
            // Des braises qui montent du bas de l'ecran.
            if (Random.value < 0.35f) Emit(new Vector2(Random.value * Screen.width, Screen.height + 10f), new Vector2(Random.Range(-10f, 10f), Random.Range(-60f, -25f)), 4f, new Color(1f, 0.7f, 0.35f, 0.7f));
        }

        /// <summary>
        /// UNE CARTE. "enter" : 0 (face cachee, pas encore arrivee) a 1 (retournee) ;
        /// "lift" : 0 a 1 (visee). Le reste : les mots.
        /// </summary>
        public static void Draw(Rect card, Color tint, string name, string ribbon, string line, string foot, string replaces, bool owned, bool on, float lift, float enter)
        {
            Ensure();
            float time = Time.unscaledTime;
            // Le retournement : la carte s'amincit jusqu'a la tranche, puis revient de face.
            float turn = Mathf.Clamp01(enter);
            float sx = Mathf.Abs(Mathf.Cos(turn * Mathf.PI));
            bool showFace = turn >= 0.5f;
            float zoom = 1f + 0.06f * lift;
            float fade = owned ? 0.45f : 1f;

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
                Tex(new Rect(card.x - g, card.y - g, card.width + g * 2f, card.height + g * 2f), glow, new Color(tint.r, tint.g, tint.b, 0.6f * lift));
                if (Random.value < 0.5f * lift)
                    Emit(new Vector2(card.x + Random.value * card.width, card.yMax - Random.value * 20f), new Vector2(Random.Range(-15f, 15f), Random.Range(-90f, -40f)), 1.2f, Color.Lerp(tint, Color.white, 0.4f));
            }

            Matrix4x4 keep = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(Mathf.Max(0.02f, sx) * zoom, zoom), card.center);
            // L'ombre portee.
            Tex(new Rect(card.x + 6f, card.y + 10f, card.width, card.height), glow, new Color(0f, 0f, 0f, 0.55f));
            if (!showFace)
            {
                Tex(card, back, Color.white);
                Tex(card, grad, new Color(tint.r * 0.4f, tint.g * 0.4f, tint.b * 0.4f, 0.5f));
                FrameAt(card, Gold, 0.9f);
                // Le losange d'or au centre du dos.
                Vector2 c = card.center;
                Matrix4x4 backMatrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(45f, c);
                float d = card.width * 0.22f;
                UiStyle.Fill(new Rect(c.x - d, c.y - d, d * 2f, d * 2f), new Color(Gold.r, Gold.g, Gold.b, 0.85f));
                UiStyle.Fill(new Rect(c.x - d * 0.8f, c.y - d * 0.8f, d * 1.6f, d * 1.6f), new Color(0.08f, 0.06f, 0.09f, 1f));
                UiStyle.Fill(new Rect(c.x - d * 0.35f, c.y - d * 0.35f, d * 0.7f, d * 0.7f), new Color(tint.r, tint.g, tint.b, 0.9f));
                GUI.matrix = backMatrix;
                GUI.matrix = keep;
                return;
            }

            // LA FACE.
            Color stone = Color.Lerp(new Color(0.16f, 0.14f, 0.13f), tint * 0.35f, 0.35f);
            Tex(card, face, new Color(stone.r, stone.g, stone.b, fade));
            // Le lavis de la couleur, du haut vers le bas, et un second plus vif en haut.
            Rect top = new Rect(card.x, card.y, card.width, card.height * 0.62f);
            Matrix4x4 flip = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(1f, -1f), top.center);
            Tex(top, grad, new Color(tint.r, tint.g, tint.b, (on ? 0.75f : 0.5f) * fade));
            GUI.matrix = flip;
            Tex(new Rect(card.x, card.y, card.width, card.height * 0.25f), glow, new Color(1f, 1f, 1f, 0.12f * fade));

            float pad = card.width * 0.09f;
            float x = card.x + pad;
            float w = card.width - pad * 2f;
            float y = card.y + card.height * 0.07f;

            // Le ruban.
            GUIStyle rs = new GUIStyle(UiStyle.Tiny);
            rs.alignment = TextAnchor.MiddleCenter;
            rs.fontStyle = FontStyle.Bold;
            float rw = Mathf.Min(w, rs.CalcSize(new GUIContent(ribbon)).x + UiStyle.S(26));
            Rect rr = new Rect(card.center.x - rw * 0.5f, y, rw, UiStyle.S(22));
            UiStyle.Fill(rr, new Color(0.05f, 0.04f, 0.05f, 0.85f * fade));
            UiStyle.Fill(new Rect(rr.x, rr.y, rr.width, 1f), new Color(Gold.r, Gold.g, Gold.b, 0.9f * fade));
            UiStyle.Fill(new Rect(rr.x, rr.yMax - 1f, rr.width, 1f), new Color(Gold.r, Gold.g, Gold.b, 0.9f * fade));
            UiStyle.Tinted(rr, ribbon, rs, new Color(Gold.r, Gold.g, Gold.b, fade));
            y += UiStyle.S(40);

            // Le nom, en grand, avec une lueur de sa couleur.
            GUIStyle ns = new GUIStyle(UiStyle.Title);
            ns.alignment = TextAnchor.MiddleCenter;
            ns.wordWrap = false;
            ns.fontSize = Mathf.RoundToInt(Mathf.Min(UiStyle.S(34), card.width / Mathf.Max(5f, name.Length) * 1.7f));
            Rect nr = new Rect(card.x, y, card.width, UiStyle.S(44));
            UiStyle.Tinted(new Rect(nr.x + 3f, nr.y + 3f, nr.width, nr.height), name, ns, new Color(0f, 0f, 0f, 0.7f * fade));
            UiStyle.Tinted(new Rect(nr.x - 1f, nr.y, nr.width, nr.height), name, ns, new Color(tint.r, tint.g, tint.b, 0.7f * fade));
            UiStyle.Tinted(nr, name, ns, on ? new Color(1f, 0.95f, 0.8f, 1f) : new Color(0.97f, 0.93f, 0.86f, fade));
            y += UiStyle.S(50);

            // Le fleuron : un trait, un losange, un trait.
            float cx = card.center.x;
            UiStyle.Fill(new Rect(cx - w * 0.36f, y, w * 0.3f, 1f), new Color(Gold.r, Gold.g, Gold.b, 0.8f * fade));
            UiStyle.Fill(new Rect(cx + w * 0.06f, y, w * 0.3f, 1f), new Color(Gold.r, Gold.g, Gold.b, 0.8f * fade));
            Matrix4x4 dm = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, new Vector2(cx, y));
            UiStyle.Fill(new Rect(cx - 5f, y - 5f, 10f, 10f), new Color(tint.r, tint.g, tint.b, fade));
            GUI.matrix = dm;
            y += UiStyle.S(18);

            // Ce qu'elle fait.
            GUIStyle ls = new GUIStyle(UiStyle.Label);
            ls.wordWrap = true;
            ls.alignment = TextAnchor.UpperCenter;
            UiStyle.Tinted(new Rect(x + 1f, y + 1f, w, card.yMax - y - UiStyle.S(70)), line, ls, new Color(0f, 0f, 0f, 0.6f * fade));
            UiStyle.Tinted(new Rect(x, y, w, card.yMax - y - UiStyle.S(70)), line, ls, new Color(0.95f, 0.92f, 0.86f, fade));

            // En bas : la recharge dans une pastille ; ce qu'elle remplace, en rouge.
            GUIStyle fs = new GUIStyle(UiStyle.Small);
            fs.alignment = TextAnchor.MiddleCenter;
            float fw = fs.CalcSize(new GUIContent(foot)).x + UiStyle.S(24);
            Rect fr = new Rect(cx - fw * 0.5f, card.yMax - UiStyle.S(replaces != null ? 70 : 44), fw, UiStyle.S(24));
            UiStyle.Fill(fr, new Color(tint.r * 0.25f, tint.g * 0.25f, tint.b * 0.25f, 0.9f * fade));
            UiStyle.Fill(new Rect(fr.x, fr.yMax - 2f, fr.width, 2f), new Color(tint.r, tint.g, tint.b, 0.9f * fade));
            UiStyle.Tinted(fr, foot, fs, new Color(1f, 0.95f, 0.85f, fade));
            if (replaces != null)
            {
                Rect rb = new Rect(card.x + 8f, card.yMax - UiStyle.S(38), card.width - 16f, UiStyle.S(28));
                UiStyle.Fill(rb, new Color(0.55f, 0.1f, 0.08f, 0.9f));
                UiStyle.Tinted(rb, replaces, fs, new Color(1f, 0.9f, 0.85f, 1f));
            }

            // Le reflet qui traverse la carte visee.
            if (on)
            {
                float sweep = Mathf.Repeat(time * 0.7f, 1.8f) - 0.4f;
                Matrix4x4 rm = GUI.matrix;
                GUI.BeginGroup(card);
                GUIUtility.RotateAroundPivot(18f, new Vector2(card.width * 0.5f, card.height * 0.5f));
                Tex(new Rect(card.width * sweep - card.width * 0.15f, -card.height * 0.2f, card.width * 0.3f, card.height * 1.4f), glow, new Color(1f, 1f, 1f, 0.22f));
                GUI.EndGroup();
                GUI.matrix = rm;
            }

            // Le cadre d'or (plus vif quand on la vise) ; "deja a toi" : grise.
            FrameAt(card, on ? Color.Lerp(Gold, Color.white, 0.3f) : Color.Lerp(Gold, tint, 0.35f), (on ? 1f : 0.8f) * fade);
            if (owned)
            {
                UiStyle.Fill(card, new Color(0f, 0f, 0f, 0.35f));
                GUIStyle os = new GUIStyle(UiStyle.Head);
                os.alignment = TextAnchor.MiddleCenter;
                UiStyle.Tinted(new Rect(card.x, card.center.y - UiStyle.S(20), card.width, UiStyle.S(40)), "DÉJÀ À TOI", os, new Color(1f, 1f, 1f, 0.8f));
            }
            GUI.matrix = keep;

        }

        static void FrameAt(Rect r, Color c, float alpha)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color was = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, alpha);
            frameStyle.Draw(r, false, false, false, false);
            GUI.color = was;
        }

        /// <summary>LA CARTE PRISE : un eclair, une gerbe d'etincelles, un coup de phonk.</summary>
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
            if (mine) Sfx.PlayClip(Phonk.Sting(), 0.7f);
        }

        static void Emit(Vector2 at, Vector2 velocity, float life, Color c)
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
                UiStyle.Fill(r, new Color(flashColour.r, flashColour.g, flashColour.b, flash * 0.7f));
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
