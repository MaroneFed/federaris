using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE JOURNAL (21/11) : les quetes et ce que tu portes. Martin : "faut un menu, une carte, un inventaire,
    /// beaucoup plus de contenu jouable". Tout s'ecrit ici ; NightUI le dessine dans le carnet (Tab).
    /// </summary>
    public static class Journal
    {
        public class Quest
        {
            public string id;
            public string title;
            public string detail;
            public bool optional;
            public bool shown;
            public bool done;
        }

        static readonly List<Quest> quests = new List<Quest>();
        public static IList<Quest> Quests { get { return quests; } }

        // Ce que tu portes.
        public static bool Fire;
        public static bool Key;
        public static bool Lantern;
        public static int Relics;
        /// <summary>(21/11) Les objets qu'on utilise (touches 1 a 4) et la viande pour le chien.</summary>
        public static int Oil;
        public static int HolyWater;
        public static int Candles;
        public static bool Eternal;
        public static bool Meat;
        public static int Rescued;
        /// <summary>L'heure (Time.unscaledTime) ou l'objectif a change : NightUI le fait briller un moment.</summary>
        public static float ChangedAt = -99f;

        public static void Reset()
        {
            quests.Clear();
            Fire = Key = Lantern = Eternal = Meat = false;
            Relics = 0; Oil = 1; HolyWater = 0; Candles = 2; Rescued = 0;
            // (21/11) L'histoire : le Veilleur mourant t'a parle de trois survivants. Le novice a la cle de la chapelle.
            // (22/11) Martin : "pas toutes les quetes en meme temps". Les vivants, un par un : la suivante n'apparait
            // que quand la precedente est faite (le Veilleur n'a parle que de la cuisiniere ; elle parle du palefrenier...).
            Add("cuisiniere", "Retrouver Mathilde, la cuisinière", "Elle se cache près du feu des cuisines, à l'est du grand couloir.", false);
            Add("palefrenier", "Retrouver Aubin, le palefrenier", "Aux écuries, au nord de la grande cour.", false);
            Add("novice", "Retrouver le novice", "Barricadé en haut du donjon, à l'est de la grande cour. Prends l'escalier.", false);
            Add("cle", "Ouvrir la chapelle", "Le novice t'a donné la clé. La chapelle est au nord-ouest du château.", false);
            Add("cierges", "Rallumer les quatre cierges", "Dans la chapelle : approche ta torche de chaque cierge (E).", false);
            Add("crypte", "Descendre derrière l'autel", "Le mur s'est ouvert. Quelque chose attend en bas.", false);
            Add("abbe", "Allumer les quatre brasiers de la crypte", "La lumière le brûle. Il ne s'approche pas d'un feu.", false);
            Add("chien", "Apprivoiser le chien des écuries", "Il a faim. La cuisinière aura de la viande.", true);
            Add("bete", "La Bête de l'ossuaire", "Sous les caves. Elle charge tout droit : esquive, qu'elle se brise sur un mur, puis frappe.", true);
            Add("reliques", "Trois reliques", "Chacune fait durer ta torche.", true);
            Add("sacristie", "La sacristie verrouillée", "À gauche du vestibule. Crochète-la.", true);
            Add("secrets", "Les passages secrets", "Le château en cache. À toi de les trouver.", true);
            Show("cuisiniere");
        }

        static void Add(string id, string title, string detail, bool optional)
        {
            Quest q = new Quest();
            q.id = id; q.title = title; q.detail = detail; q.optional = optional;
            quests.Add(q);
        }

        static Quest Find(string id)
        {
            for (int i = 0; i < quests.Count; i++) if (quests[i].id == id) return quests[i];
            return null;
        }

        public static void Show(string id)
        {
            Quest q = Find(id);
            if (q == null || q.shown) return;
            q.shown = true;
            if (!q.optional) ChangedAt = Time.unscaledTime;
        }

        public static void Done(string id)
        {
            Quest q = Find(id);
            if (q == null || q.done) return;
            q.shown = true;
            q.done = true;
            ChangedAt = Time.unscaledTime;
        }

        public static bool IsDone(string id) { Quest q = Find(id); return q != null && q.done; }

        public static void SetDetail(string id, string detail) { Quest q = Find(id); if (q != null) q.detail = detail; }

        /// <summary>La quete principale en cours (la premiere montree et pas finie).</summary>
        public static Quest Current
        {
            get
            {
                for (int i = 0; i < quests.Count; i++) if (!quests[i].optional && quests[i].shown && !quests[i].done) return quests[i];
                return null;
            }
        }
    }

    /// <summary>
    /// L'INTERFACE (21/11) : le menu du debut, la pause (Echap), le carnet (Tab ou M : la carte a gauche, les quetes et
    /// l'inventaire a droite, sur du parchemin), et l'objectif qui s'ecrit en haut a gauche quand il change.
    /// Martin : "je n'aime pas la police ni les cubes en haut de l'ecran" : plus de cubes, et deux polices de livre ancien.
    ///
    /// Concept Unity : Time.timeScale = 0 met le jeu en pause (les monstres, la foi, tout s'arrete) ; l'interface,
    /// elle, compte avec Time.unscaledDeltaTime, qui avance toujours.
    /// </summary>
    public static class NightUI
    {
        /// <summary>(22/11) La luminosite (0 a 1), reglee dans le menu et la pause ; gardee d'une partie a l'autre.</summary>
        public static float Brightness = 0.35f;
        public static bool MenuOpen;
        public static bool PauseOpen;
        public static bool BookOpen;
        public static bool HelpOpen;
        /// <summary>Le joueur ne joue pas : il lit, il choisit dans un menu.</summary>
        public static bool Blocking { get { return MenuOpen || PauseOpen || BookOpen; } }
        /// <summary>Le menu du debut a choisi "Entrer dans la nuit".</summary>
        public static bool StartRequested;

        static Texture2D parchment;
        static GUIStyle title, body, small, item, label;
        static int pick;
        static readonly string[] MenuItems = { "Entrer dans la nuit", "Luminosité", "Commandes", "Quitter" };
        static readonly string[] PauseItems = { "Reprendre", "Le carnet", "Luminosité", "Commandes", "Quitter le jeu" };

        public static void Reset()
        {
            Brightness = PlayerPrefs.GetFloat("fief.nuit.luminosite", 0.35f);
            MenuOpen = PauseOpen = BookOpen = HelpOpen = false;
            StartRequested = false;
            pick = 0;
            Time.timeScale = 1f;
        }

        public static void OpenMenu() { MenuOpen = true; pick = 0; }

        static void Styles(float h)
        {
            if (title != null && title.fontSize == Mathf.RoundToInt(h * 0.11f)) return;
            title = new GUIStyle(GUI.skin.label);
            title.font = NightFont.Title();
            title.fontSize = Mathf.RoundToInt(h * 0.11f);
            title.alignment = TextAnchor.MiddleCenter;
            title.normal.textColor = new Color(0.86f, 0.80f, 0.68f);
            body = new GUIStyle(GUI.skin.label);
            body.font = NightFont.Get();
            body.fontSize = Mathf.RoundToInt(h * 0.032f);
            body.alignment = TextAnchor.MiddleCenter;
            body.normal.textColor = new Color(0.84f, 0.78f, 0.66f);
            small = new GUIStyle(body);
            small.fontSize = Mathf.RoundToInt(h * 0.022f);
            small.alignment = TextAnchor.UpperLeft;
            small.wordWrap = true;
            small.normal.textColor = new Color(0.25f, 0.17f, 0.10f);
            item = new GUIStyle(small);
            item.fontSize = Mathf.RoundToInt(h * 0.026f);
            item.fontStyle = FontStyle.Bold;
            label = new GUIStyle(small);
            label.fontStyle = FontStyle.Italic;
            label.alignment = TextAnchor.MiddleCenter;
            label.wordWrap = false;
        }

        // ------------------------------------------------------------------ chaque image

        /// <summary>Les touches de l'interface. Renvoie true quand le joueur doit quitter le jeu.</summary>
        public static bool Tick(bool playing)
        {
            if (MenuOpen)
            {
                if (HelpOpen) { if (FiefInput.CancelPressed || FiefInput.ConfirmPressed || FiefInput.ShootPressed) HelpOpen = false; }
                else
                {
                    if (FiefInput.ArrowUpPressed || FiefInput.UpPressed) pick = (pick + MenuItems.Length - 1) % MenuItems.Length;
                    if (FiefInput.ArrowDownPressed || FiefInput.DownPressed) pick = (pick + 1) % MenuItems.Length;
                    if (FiefInput.ConfirmPressed || FiefInput.JumpPressed) return Choose(MenuItems[pick]);
                    if (MenuItems[pick] == "Luminosité") Bright();
                }
            }
            else if (PauseOpen)
            {
                if (HelpOpen) { if (FiefInput.CancelPressed || FiefInput.ConfirmPressed) HelpOpen = false; }
                else if (FiefInput.CancelPressed) Close();
                else
                {
                    if (FiefInput.ArrowUpPressed || FiefInput.UpPressed) pick = (pick + PauseItems.Length - 1) % PauseItems.Length;
                    if (FiefInput.ArrowDownPressed || FiefInput.DownPressed) pick = (pick + 1) % PauseItems.Length;
                    if (FiefInput.ConfirmPressed || FiefInput.JumpPressed) return Choose(PauseItems[pick]);
                    if (PauseItems[pick] == "Luminosité") Bright();
                }
            }
            else if (BookOpen)
            {
                if (FiefInput.CancelPressed || BookKey()) Close();
            }
            else if (playing)
            {
                if (FiefInput.CancelPressed) { PauseOpen = true; pick = 0; }
                else if (BookKey()) BookOpen = true;
            }
            bool pause = PauseOpen || BookOpen || NightTalk.Open;
            Time.timeScale = pause ? 0f : 1f;
            if (Blocking) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (playing && !Application.isBatchMode) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            return false;
        }

        /// <summary>Gauche / droite : moins ou plus de lumiere (un clic sur la ligne : un cran de plus, en boucle).</summary>
        static void Bright()
        {
            float step = 0f;
            if (FiefInput.LeftPressed) step = -0.1f;
            if (FiefInput.RightPressed) step = 0.1f;
            if (step == 0f) return;
            Brightness = Mathf.Clamp01(Brightness + step);
            PlayerPrefs.SetFloat("fief.nuit.luminosite", Brightness);
        }

        static bool BookKey() { return FiefInput.Pressed(KeyCode.Tab) || FiefInput.Pressed(KeyCode.M) || FiefInput.Pressed(KeyCode.I); }

        static void Close() { PauseOpen = false; BookOpen = false; HelpOpen = false; Time.timeScale = 1f; }

        static bool Choose(string what)
        {
            try { Sfx.Pop(); } catch (System.Exception) { }
            switch (what)
            {
                case "Entrer dans la nuit": MenuOpen = false; StartRequested = true; return false;
                case "Commandes": HelpOpen = true; return false;
                case "Luminosité":
                    Brightness = Brightness >= 0.95f ? 0f : Mathf.Clamp01(Brightness + 0.1f);
                    PlayerPrefs.SetFloat("fief.nuit.luminosite", Brightness);
                    return false;
                case "Reprendre": Close(); return false;
                case "Le carnet": PauseOpen = false; BookOpen = true; return false;
                default: return true;      // quitter
            }
        }

        // ------------------------------------------------------------------ le dessin

        public static void Draw(float w, float h, GUIStyle hud, Vector3 playerPos, float yaw)
        {
            Styles(h);
            if (MenuOpen) { DrawMenu(w, h); return; }
            if (PauseOpen) { DrawPause(w, h); return; }
            if (BookOpen) { DrawBook(w, h, playerPos, yaw); return; }
            if (NightIntro.Playing || NightCinema.Playing) return;
            DrawObjective(w, h);
            DrawMiniMap(w, h, playerPos, yaw);
            DrawItems(w, h);
        }

        // ------------------------------------------------------------------ (21/11) l'ecran de jeu
        // Martin : "on voit pas ce qu'on doit faire, on doit voir tout le temps c'est quoi notre quete du moment" ;
        // "une carte qui est toujours la, en bas". En haut a gauche, la quete en cours (et deux quetes de cote) ; en bas a
        // droite, la petite carte sur son parchemin ; en bas au milieu, les objets (touches 1 a 4).

        static Texture2D fade;
        static Texture2D[] icons;

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>Un voile sombre qui s'efface vers la droite et vers le bas : le texte se lit sur n'importe quel fond.</summary>
        static Texture2D Fade()
        {
            if (fade != null) return fade;
            fade = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            fade.hideFlags = HideFlags.HideAndDontSave;
            fade.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[128 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = x / 127f, v = 1f - y / 63f;
                    px[y * 128 + x] = new Color(0f, 0f, 0f, 0.62f * (1f - Smooth(0.25f, 1f, u)) * (1f - Smooth(0.45f, 1f, v)));
                }
            fade.SetPixels(px);
            fade.Apply();
            return fade;
        }

        /// <summary>Les icones des objets, dessinees par le calcul : la fiole d'huile, l'eau benite, les bougies, le cierge eternel.</summary>
        static Texture2D Icon(int which)
        {
            if (icons == null) icons = new Texture2D[4];
            if (icons[which] != null) return icons[which];
            const int N = 96;
            Texture2D t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.HideAndDontSave;
            Color[] px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float d;
                    Color fill, rim = new Color(0.08f, 0.05f, 0.03f);
                    float glow = 0f;
                    if (which == 0)
                    {
                        // La fiole : une panse ronde, un col, un bouchon.
                        float body = Mathf.Sqrt(u * u + (v + 0.25f) * (v + 0.25f)) - 0.5f;
                        float neck = Mathf.Max(Mathf.Abs(u) - 0.15f, Mathf.Abs(v - 0.38f) - 0.22f);
                        float cork = Mathf.Max(Mathf.Abs(u) - 0.2f, Mathf.Abs(v - 0.66f) - 0.09f);
                        d = Mathf.Min(Mathf.Min(body, neck), cork);
                        fill = cork < 0f ? new Color(0.45f, 0.28f, 0.14f) : v < -0.05f ? Color.Lerp(new Color(0.75f, 0.42f, 0.08f), new Color(1f, 0.78f, 0.3f), Smooth(0.3f, -0.4f, u)) : new Color(0.55f, 0.6f, 0.58f, 1f);
                    }
                    else if (which == 1)
                    {
                        // L'eau benite : une goutte, et une petite croix d'or dedans.
                        float r = Mathf.Sqrt(u * u + (v + 0.25f) * (v + 0.25f));
                        float tip = Mathf.Abs(u) - Mathf.Max(0f, (0.72f - v) * 0.52f);
                        d = v > -0.25f ? Mathf.Max(tip, -(v + 0.25f) - 10f) : r - 0.47f;
                        if (v > -0.25f) d = Mathf.Min(Mathf.Max(tip, v - 0.72f), r - 0.47f);
                        float cross = Mathf.Min(Mathf.Max(Mathf.Abs(u) - 0.05f, Mathf.Abs(v + 0.2f) - 0.24f), Mathf.Max(Mathf.Abs(u) - 0.17f, Mathf.Abs(v + 0.12f) - 0.05f));
                        fill = cross < 0f ? new Color(1f, 0.85f, 0.4f) : Color.Lerp(new Color(0.3f, 0.5f, 0.85f), new Color(0.75f, 0.9f, 1f), Smooth(0.2f, -0.5f, u + v * 0.3f));
                    }
                    else
                    {
                        // Les bougies (trois) ou le cierge eternel (un seul, haut, et son aureole).
                        bool tall = which == 3;
                        float c1 = Mathf.Max(Mathf.Abs(u + (tall ? 0f : 0.32f)) - (tall ? 0.17f : 0.13f), Mathf.Abs(v + (tall ? 0.2f : 0.35f)) - (tall ? 0.6f : 0.4f));
                        float c2 = tall ? 9f : Mathf.Max(Mathf.Abs(u - 0.02f) - 0.13f, Mathf.Abs(v + 0.25f) - 0.5f);
                        float c3 = tall ? 9f : Mathf.Max(Mathf.Abs(u - 0.36f) - 0.13f, Mathf.Abs(v + 0.45f) - 0.3f);
                        float wax = Mathf.Min(c1, Mathf.Min(c2, c3));
                        float fl = Mathf.Sqrt(u * u * 3.5f + (v - (tall ? 0.6f : 0.4f)) * (v - (tall ? 0.6f : 0.4f))) - (tall ? 0.2f : 0.13f);
                        d = Mathf.Min(wax, fl);
                        fill = fl < 0f ? Color.Lerp(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.97f, 0.8f), Smooth(0f, -0.1f, fl)) : new Color(0.95f, 0.92f, 0.83f);
                        if (tall) glow = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + (v - 0.55f) * (v - 0.55f)) / 0.75f) * 0.6f;
                    }
                    float inside = 1f - Smooth(-0.015f, 0.015f, d);
                    float edge = (1f - Smooth(0.02f, 0.06f, d)) * (1f - inside);
                    Color c = Color.Lerp(new Color(rim.r, rim.g, rim.b, edge), fill, inside);
                    if (glow > 0f && c.a < glow) c = new Color(1f, 0.8f, 0.4f, Mathf.Max(c.a, glow * glow));
                    px[y * N + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            icons[which] = t;
            return t;
        }

        /// <summary>La quete en cours, toujours la, en haut a gauche. Elle brille quand elle vient de changer.</summary>
        static void DrawObjective(float w, float h)
        {
            Journal.Quest q = Journal.Current;
            float age = Time.unscaledTime - Journal.ChangedAt;
            float x = Mathf.Round(h * 0.045f), y = Mathf.Round(h * 0.04f);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, w * 0.5f, h * 0.26f), Fade());
            float pulse = age < 5f ? Mathf.Clamp01(1f - age / 5f) : 0f;
            // Le filet d'or, a gauche du texte.
            GUI.color = new Color(0.85f, 0.62f, 0.25f, 0.75f + 0.25f * pulse);
            GUI.DrawTexture(new Rect(x - Mathf.Round(h * 0.014f), y + h * 0.006f, Mathf.Max(2f, Mathf.Round(h * 0.003f)), h * 0.085f), Texture2D.whiteTexture);
            GUIStyle head = new GUIStyle(small);
            head.font = NightFont.Get();
            head.fontSize = Mathf.RoundToInt(h * 0.017f);
            head.fontStyle = FontStyle.Normal;
            head.wordWrap = false;
            head.normal.textColor = Color.Lerp(new Color(0.7f, 0.55f, 0.35f), new Color(1f, 0.82f, 0.4f), pulse);
            NightFont.Label(new Rect(x, y, w * 0.45f, h * 0.025f), pulse > 0f ? "NOUVELLE QUÊTE" : "QUÊTE", head);
            if (q == null) { GUI.color = Color.white; return; }
            string name = q.title;

            GUIStyle t = new GUIStyle(body);
            t.alignment = TextAnchor.UpperLeft;
            t.fontSize = Mathf.RoundToInt(h * 0.031f);
            t.fontStyle = FontStyle.Bold;
            t.normal.textColor = Color.Lerp(new Color(0.95f, 0.88f, 0.72f), new Color(1f, 0.9f, 0.55f), pulse);
            NightFont.Label(new Rect(x, y + h * 0.024f, w * 0.6f, h * 0.045f), name, t);
            GUIStyle d = new GUIStyle(t);
            d.fontSize = Mathf.RoundToInt(h * 0.021f);
            d.fontStyle = FontStyle.Italic;
            d.wordWrap = true;
            d.normal.textColor = new Color(0.78f, 0.72f, 0.62f);
            NightFont.Label(new Rect(x, y + h * 0.064f, w * 0.38f, h * 0.06f), q.detail, d);
            // Deux quetes de cote, plus discretes.
            float yy = y + h * 0.13f;
            int shown = 0;
            GUIStyle side = new GUIStyle(d);
            side.fontSize = Mathf.RoundToInt(h * 0.018f);
            side.fontStyle = FontStyle.Normal;
            side.wordWrap = false;
            side.normal.textColor = new Color(0.62f, 0.6f, 0.68f);
            foreach (Journal.Quest o in Journal.Quests)
            {
                if (!o.optional || !o.shown || o.done || shown >= 1) continue;
                string n = "·  " + o.title + (o.id == "reliques" ? "  " + Mathf.Min(Journal.Relics, 3) + "/3" : "");
                NightFont.Label(new Rect(x, yy, w * 0.45f, h * 0.026f), n, side);
                yy += h * 0.026f;
                shown++;
            }
            GUI.color = Color.white;
        }

        /// <summary>La petite carte, en bas a droite : un parchemin cercle de fer, toi au milieu, le nord en haut.</summary>
        static void DrawMiniMap(float w, float h, Vector3 me, float yaw)
        {
            float size = Mathf.Round(h * 0.25f), m = Mathf.Round(h * 0.035f);
            Rect r = new Rect(w - size - m, h - size - m, size, size);
            float b = Mathf.Max(3f, Mathf.Round(h * 0.006f));
            // Le cadre : fer sombre, puis un filet d'or.
            GUI.color = new Color(0.07f, 0.065f, 0.06f, 0.95f);
            GUI.DrawTexture(new Rect(r.x - b * 2f, r.y - b * 2f, r.width + b * 4f, r.height + b * 4f), Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.45f, 0.2f, 0.95f);
            GUI.DrawTexture(new Rect(r.x - b, r.y - b, r.width + b * 2f, r.height + b * 2f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUI.DrawTexture(r, Parchment());
            GUI.color = Color.white;
            NightMap.DrawMini(r, me, yaw, label, 7f);
            // Le nord, et le nom de la salle ou tu es.
            GUIStyle n = new GUIStyle(label);
            n.fontSize = Mathf.RoundToInt(h * 0.022f);
            n.font = NightFont.Title();
            n.normal.textColor = new Color(0.5f, 0.08f, 0.04f);
            GUI.Label(new Rect(r.x, r.y + b, r.width, h * 0.03f), "N", n);
            string room = NightMap.NameOf(NightMap.RoomAt(me));
            if (room.Length > 0)
            {
                GUIStyle rn = new GUIStyle(body);
                rn.fontSize = Mathf.RoundToInt(h * 0.02f);
                rn.fontStyle = FontStyle.Italic;
                rn.normal.textColor = new Color(0.9f, 0.82f, 0.65f);
                NightFont.Label(new Rect(r.x - size * 0.3f, r.y - h * 0.04f, r.width + size * 0.3f, h * 0.03f), room, rn);
            }
        }

        /// <summary>Les objets, en bas au milieu : quatre cases, la touche en haut a gauche, le nombre en bas a droite.</summary>
        static void DrawItems(float w, float h)
        {
            int[] count = { Journal.Oil, Journal.HolyWater, Journal.Candles, Journal.Eternal ? 1 : 0 };
            bool any = false;
            for (int i = 0; i < 4; i++) if (count[i] > 0) any = true;
            if (!any) return;
            float s = Mathf.Round(h * 0.066f), gap = Mathf.Round(h * 0.012f);
            float total = s * 4f + gap * 3f;
            float x0 = Mathf.Round((w - total) * 0.5f), y0 = Mathf.Round(h - s - h * 0.03f);
            GUIStyle k = new GUIStyle(small);
            k.fontSize = Mathf.RoundToInt(h * 0.016f);
            k.wordWrap = false;
            k.normal.textColor = new Color(0.85f, 0.75f, 0.55f);
            GUIStyle c = new GUIStyle(k);
            c.fontSize = Mathf.RoundToInt(h * 0.022f);
            c.fontStyle = FontStyle.Bold;
            c.alignment = TextAnchor.LowerRight;
            c.normal.textColor = new Color(1f, 0.95f, 0.85f);
            for (int i = 0; i < 4; i++)
            {
                Rect r = new Rect(x0 + i * (s + gap), y0, s, s);
                bool have = count[i] > 0;
                GUI.color = new Color(0.05f, 0.045f, 0.04f, have ? 0.8f : 0.4f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = new Color(0.62f, 0.45f, 0.2f, have ? 0.9f : 0.25f);
                float b = Mathf.Max(1f, Mathf.Round(h * 0.002f));
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, b), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.yMax - b, r.width, b), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.y, b, r.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.xMax - b, r.y, b, r.height), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, have ? 1f : 0.18f);
                float p = s * 0.14f;
                GUI.DrawTexture(new Rect(r.x + p, r.y + p, s - p * 2f, s - p * 2f), Icon(i));
                GUI.color = Color.white;
                NightFont.Label(new Rect(r.x + b * 3f, r.y + b, s, s * 0.35f), (i + 1).ToString(), k);
                if (have && i < 3) NightFont.Label(new Rect(r.x, r.y, s - b * 4f, s - b * 2f), "×" + count[i], c);
            }
            GUI.color = Color.white;
        }

        static void Shade(float w, float h, float alpha)
        {
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>Une liste de choix, au centre. La souris les survole et les clique ; les fleches et Entree aussi.</summary>
        static bool List(string[] items, float w, float top, float h)
        {
            float line = Mathf.Round(h * 0.065f);
            for (int i = 0; i < items.Length; i++)
            {
                Rect r = new Rect(w * 0.3f, top + i * line, w * 0.4f, line);
                Event e = Event.current;
                if (r.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseMove || e.type == EventType.Repaint) pick = i;
                    if (e.type == EventType.MouseDown) { pick = i; e.Use(); return true; }
                }
                GUIStyle s = new GUIStyle(body);
                bool on = i == pick;
                s.normal.textColor = on ? new Color(1f, 0.82f, 0.45f) : new Color(0.7f, 0.65f, 0.56f);
                string word = items[i];
                if (word == "Luminosité")
                {
                    int lit = Mathf.RoundToInt(Brightness * 10f);
                    word = "Luminosité   ‹ " + new string('|', lit) + new string('.', 10 - lit) + " ›";
                }
                NightFont.Label(r, on ? "—   " + word + "   —" : word, s);
            }
            return false;
        }

        static void DrawMenu(float w, float h)
        {
            Shade(w, h, 0.35f);
            if (HelpOpen) { DrawHelp(w, h); return; }
            float breathe = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 0.8f);
            GUIStyle t = new GUIStyle(title);
            t.normal.textColor = new Color(0.88f * breathe, 0.78f * breathe, 0.6f * breathe);
            NightFont.Label(new Rect(0f, h * 0.16f, w, h * 0.16f), "Fief", t);
            GUIStyle sub = new GUIStyle(body);
            sub.fontStyle = FontStyle.Italic;
            NightFont.Label(new Rect(0f, h * 0.31f, w, h * 0.06f), "La Nuit des Veilleurs", sub);
            if (List(MenuItems, w, h * 0.52f, h) && Choose(MenuItems[pick])) Application.Quit();
        }

        static void DrawPause(float w, float h)
        {
            Shade(w, h, 0.65f);
            if (HelpOpen) { DrawHelp(w, h); return; }
            GUIStyle t = new GUIStyle(title);
            t.fontSize = Mathf.RoundToInt(h * 0.07f);
            NightFont.Label(new Rect(0f, h * 0.2f, w, h * 0.1f), "Pause", t);
            if (List(PauseItems, w, h * 0.42f, h) && Choose(PauseItems[pick])) Application.Quit();
        }

        static void DrawHelp(float w, float h)
        {
            Rect page = Page(w, h, 0.5f);
            GUI.DrawTexture(page, Parchment());
            GUIStyle t = new GUIStyle(title);
            t.fontSize = Mathf.RoundToInt(h * 0.06f);
            t.normal.textColor = new Color(0.35f, 0.08f, 0.05f);
            GUI.Label(new Rect(page.x, page.y + page.height * 0.05f, page.width, page.height * 0.12f), "Commandes", t);
            string[] lines =
            {
                "Z Q S D  —  marcher   ·   Maj  —  courir (ça s'entend)", "Souris  —  regarder   ·   Clic gauche  —  coup de torche",
                "E  —  agir, parler, allumer   ·   E tenu  —  fouiller, raviver ta torche", "1  huile   ·   2  eau bénite   ·   3  bougie   ·   4  cierge éternel",
                "Espace  —  forcer la serrure", "Tab  ou  M  —  le carnet : carte, quêtes, inventaire", "Échap  —  pause"
            };
            for (int i = 0; i < lines.Length; i++)
                GUI.Label(new Rect(page.x + page.width * 0.12f, page.y + page.height * (0.24f + i * 0.09f), page.width * 0.8f, page.height * 0.08f), lines[i], item);
            GUIStyle foot = new GUIStyle(label);
            GUI.Label(new Rect(page.x, page.y + page.height * 0.88f, page.width, page.height * 0.06f), "Entrée ou Échap pour revenir", foot);
        }

        static Rect Page(float w, float h, float wide)
        {
            float pw = Mathf.Round(w * wide), ph = Mathf.Round(h * 0.84f);
            return new Rect(Mathf.Round((w - pw) * 0.5f), Mathf.Round((h - ph) * 0.5f), pw, ph);
        }

        /// <summary>Le carnet ouvert : la carte sur la page de gauche, les quetes et l'inventaire sur celle de droite.</summary>
        static void DrawBook(float w, float h, Vector3 playerPos, float yaw)
        {
            Shade(w, h, 0.7f);
            Rect book = Page(w, h, 0.88f);
            float half = Mathf.Round(book.width * 0.5f);
            Rect left = new Rect(book.x, book.y, half - 4f, book.height);
            Rect right = new Rect(book.x + half + 4f, book.y, half - 4f, book.height);
            GUI.DrawTexture(left, Parchment());
            GUI.DrawTexture(right, Parchment());
            GUIStyle head = new GUIStyle(title);
            head.fontSize = Mathf.RoundToInt(h * 0.05f);
            head.normal.textColor = new Color(0.35f, 0.08f, 0.05f);

            // --- la carte
            GUI.Label(new Rect(left.x, left.y + left.height * 0.02f, left.width, left.height * 0.09f), "Le Château", head);
            Rect map = new Rect(left.x + left.width * 0.07f, left.y + left.height * 0.12f, left.width * 0.86f, left.height * 0.82f);
            NightMap.DrawMap(map, playerPos, yaw, label);

            // --- les quetes
            float x = right.x + right.width * 0.08f, ww = right.width * 0.84f;
            float y = right.y + right.height * 0.02f;
            GUI.Label(new Rect(right.x, y, right.width, right.height * 0.09f), "Les Quêtes", head);
            y += right.height * 0.1f;
            foreach (Journal.Quest q in Journal.Quests)
            {
                if (!q.shown) continue;
                string name = (q.done ? "✓ " : q.optional ? "· " : "† ") + q.title;
                if (q.id == "reliques") name += "  (" + Mathf.Min(Journal.Relics, 3) + "/3)";

                GUIStyle s = new GUIStyle(item);
                s.normal.textColor = q.done ? new Color(0.35f, 0.3f, 0.25f, 0.7f) : q.optional ? new Color(0.25f, 0.22f, 0.4f) : new Color(0.3f, 0.12f, 0.06f);
                GUI.Label(new Rect(x, y, ww, right.height * 0.045f), name, s);
                y += right.height * 0.045f;
                if (!q.done)
                {
                    // (22/11) La hauteur du detail se calcule : avant, une deuxieme ligne etait coupee.
                    float dh = small.CalcHeight(new GUIContent(q.detail), ww * 0.95f) + 2f;
                    GUI.Label(new Rect(x + ww * 0.05f, y, ww * 0.95f, dh), q.detail, small);
                    y += dh;
                }
                y += right.height * 0.01f;
            }

            // --- l'inventaire : chaque objet a sa lettrine, comme dans un manuscrit
            y = Mathf.Max(y + right.height * 0.01f, right.y + right.height * 0.5f);
            GUI.Label(new Rect(right.x, y, right.width, right.height * 0.08f), "Ce que tu portes", head);
            y += right.height * 0.09f;
            int n = 0;
            if (Journal.Fire) Item(ref y, x, ww, right.height, "La torche du Veilleur", "Ravive-la aux feux (E tenu). Clic gauche : frapper.", 'T', new Color(0.75f, 0.2f, 0.05f), ref n);
            if (Journal.Key) Item(ref y, x, ww, right.height, "Clé de la chapelle", "Fer froid, dents usées.", 'C', new Color(0.45f, 0.45f, 0.5f), ref n);
            if (Journal.Oil > 0) Item(ref y, x, ww, right.height, "Huile  ×" + Journal.Oil, "Touche 1 : ta torche reprend vie.", 'H', new Color(0.6f, 0.35f, 0.05f), ref n);
            if (Journal.HolyWater > 0) Item(ref y, x, ww, right.height, "Eau bénite  ×" + Journal.HolyWater, "Touche 2 : tout ce qui rôde autour de toi s'enfuit.", 'E', new Color(0.2f, 0.35f, 0.65f), ref n);
            if (Journal.Candles > 0) Item(ref y, x, ww, right.height, "Bougies  ×" + Journal.Candles, "Touche 3 : poser une bougie. Rien ne passe sa lumière.", 'B', new Color(0.55f, 0.5f, 0.4f), ref n);
            if (Journal.Eternal) Item(ref y, x, ww, right.height, "Le cierge éternel", "Touche 4 : il ne s'éteint jamais.", 'Æ', new Color(0.8f, 0.6f, 0.15f), ref n);
            if (Journal.Meat) Item(ref y, x, ww, right.height, "Un quartier de viande", "Pour le chien des écuries.", 'V', new Color(0.5f, 0.12f, 0.08f), ref n);
            if (Journal.Relics > 0) Item(ref y, x, ww, right.height, "Reliques  ×" + Journal.Relics, "Ta torche s'use moins vite.", 'R', new Color(0.85f, 0.65f, 0.15f), ref n);
            if (Journal.Lantern) Item(ref y, x, ww, right.height, "Lanterne des Veilleurs", "Ta lumière porte plus loin.", 'L', new Color(0.2f, 0.45f, 0.35f), ref n);
            if (n == 0) GUI.Label(new Rect(x, y, ww, right.height * 0.05f), "Rien. Tes mains tremblent.", small);
            GUI.Label(new Rect(book.x, book.yMax - book.height * 0.05f, book.width, book.height * 0.05f), "Tab  ·  Échap  —  fermer le carnet", label);
        }

        static void Item(ref float y, float x, float ww, float ph, string name, string what, char letter, Color ink, ref int n)
        {
            // (22/11) Le nom et sa phrase ont chacun la hauteur de leur police (avant, le nom etait rogne par le haut).
            float s = Mathf.Round(ph * 0.062f);
            float nameH = item.CalcHeight(new GUIContent(name), ww) + 2f, whatH = small.CalcHeight(new GUIContent(what), ww - s * 1.3f) + 2f;
            Initial(new Rect(x, y + 2f, s, s), letter, ink);
            GUI.Label(new Rect(x + s * 1.25f, y, ww - s * 1.3f, nameH), name, item);
            GUI.Label(new Rect(x + s * 1.25f, y + nameH - 4f, ww - s * 1.3f, whatH), what, small);
            y += Mathf.Max(s, nameH + whatH - 4f) + 6f;
            n++;
        }

        /// <summary>Une lettrine : un carre d'encre de couleur, un liseré d'or, et la lettre en gothique.</summary>
        static void Initial(Rect r, char letter, Color ink)
        {
            GUI.color = new Color(0.75f, 0.55f, 0.18f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = ink;
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUIStyle s = new GUIStyle(title);
            s.fontSize = Mathf.RoundToInt(r.height * 0.8f);
            s.normal.textColor = new Color(1f, 0.92f, 0.7f);
            GUI.Label(r, letter.ToString(), s);
        }

        /// <summary>
        /// Le parchemin, fabrique par le calcul : un fond creme, des taches, des bords brunis et rongés.
        /// </summary>
        public static Texture2D Parchment()
        {
            if (parchment != null) return parchment;
            const int W = 512, H = 640;
            parchment = new Texture2D(W, H, TextureFormat.RGBA32, false);
            parchment.hideFlags = HideFlags.HideAndDontSave;
            parchment.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H;
                    float n = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.6f + Mathf.PerlinNoise(u * 25f + 7f, v * 25f) * 0.25f + Mathf.PerlinNoise(u * 90f, v * 90f + 3f) * 0.15f;
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float burn = Mathf.Clamp01(1f - (edge - 0.01f - 0.03f * Mathf.PerlinNoise(u * 11f, v * 13f)) / 0.07f);
                    Color c = Color.Lerp(new Color(0.86f, 0.78f, 0.6f), new Color(0.7f, 0.58f, 0.4f), n);
                    c = Color.Lerp(c, new Color(0.3f, 0.18f, 0.08f), burn * burn);
                    float alpha = edge < 0.006f + 0.012f * Mathf.PerlinNoise(u * 31f, v * 29f) ? 0f : 1f;
                    c.a = alpha;
                    px[y * W + x] = c;
                }
            parchment.SetPixels(px);
            parchment.Apply();
            return parchment;
        }
    }
}
