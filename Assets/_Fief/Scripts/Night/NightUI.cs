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
        /// <summary>L'heure (Time.unscaledTime) ou l'objectif a change : NightUI l'affiche quelques secondes.</summary>
        public static float ChangedAt = -99f;

        public static void Reset()
        {
            quests.Clear();
            Fire = Key = Lantern = false;
            Relics = 0;
            Add("feu", "Trouver du feu", "La sacristie, à gauche du vestibule. Sa porte est fermée.", false);
            Add("cle", "La clé de la chapelle", "Fouille les coffres, les tombeaux, les livres du château.", false);
            Add("cierges", "Rallumer les quatre cierges", "Dans la chapelle, aux quatre coins de la nef.", false);
            Add("crypte", "Descendre derrière l'autel", "Le mur s'est ouvert. Quelque chose attend en bas.", false);
            Add("abbe", "Allumer les quatre brasiers de la crypte", "La lumière le brûle. Il ne s'approche pas d'un feu.", false);
            Add("reliques", "Trois reliques", "Chacune réchauffe la foi. Trois : une bénédiction.", true);
            Add("secret", "Le rayon qui sonne creux", "Un livre, dans la bibliothèque, n'est pas comme les autres.", true);
            Show("feu");
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
        static readonly string[] MenuItems = { "Entrer dans la nuit", "Commandes", "Quitter" };
        static readonly string[] PauseItems = { "Reprendre", "Le carnet", "Commandes", "Quitter le jeu" };

        public static void Reset()
        {
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
            bool pause = PauseOpen || BookOpen;
            Time.timeScale = pause ? 0f : 1f;
            if (Blocking) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (playing && !Application.isBatchMode) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            return false;
        }

        static bool BookKey() { return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.I); }

        static void Close() { PauseOpen = false; BookOpen = false; HelpOpen = false; Time.timeScale = 1f; }

        static bool Choose(string what)
        {
            try { Sfx.Pop(); } catch (System.Exception) { }
            switch (what)
            {
                case "Entrer dans la nuit": MenuOpen = false; StartRequested = true; return false;
                case "Commandes": HelpOpen = true; return false;
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
            DrawObjective(w, h);
        }

        /// <summary>L'objectif, en haut a gauche, a la plume : il apparait quand il change, puis s'efface.</summary>
        static void DrawObjective(float w, float h)
        {
            Journal.Quest q = Journal.Current;
            float age = Time.unscaledTime - Journal.ChangedAt;
            if (q == null || age > 9f) return;
            float a = Mathf.Clamp01(age / 0.6f) * Mathf.Clamp01((9f - age) / 1.5f);
            GUIStyle s = new GUIStyle(body);
            s.alignment = TextAnchor.UpperLeft;
            s.fontStyle = FontStyle.Italic;
            s.normal.textColor = new Color(0.9f, 0.82f, 0.62f, a);
            NightFont.Label(new Rect(w * 0.035f, h * 0.04f, w * 0.6f, h * 0.05f), q.title, s);
            s.fontSize = Mathf.RoundToInt(h * 0.022f);
            s.normal.textColor = new Color(0.75f, 0.7f, 0.6f, a * 0.85f);
            NightFont.Label(new Rect(w * 0.035f, h * 0.088f, w * 0.6f, h * 0.04f), q.detail, s);
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
                NightFont.Label(r, on ? "—   " + items[i] + "   —" : items[i], s);
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
                "Z Q S D  —  marcher", "Maj  —  courir (ça s'entend)", "Souris  —  regarder", "E  —  agir   ·   E tenu  —  fouiller, prier",
                "Espace  —  les gestes (serrure, feu)", "Tab  ou  M  —  le carnet : carte, quêtes, inventaire", "Échap  —  pause"
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
                    GUI.Label(new Rect(x + ww * 0.05f, y, ww * 0.95f, right.height * 0.06f), q.detail, small);
                    y += right.height * 0.06f;
                }
                y += right.height * 0.01f;
            }

            // --- l'inventaire : chaque objet a sa lettrine, comme dans un manuscrit
            y = Mathf.Max(y + right.height * 0.02f, right.y + right.height * 0.56f);
            GUI.Label(new Rect(right.x, y, right.width, right.height * 0.08f), "Ce que tu portes", head);
            y += right.height * 0.09f;
            int n = 0;
            if (Journal.Fire) Item(ref y, x, ww, right.height, "Pot à braises", "De quoi rallumer un feu : un cierge, un brasier.", 'B', new Color(0.75f, 0.2f, 0.05f), ref n);
            if (Journal.Key) Item(ref y, x, ww, right.height, "Clé de la chapelle", "Fer froid, dents usées.", 'C', new Color(0.45f, 0.45f, 0.5f), ref n);
            if (Journal.Relics > 0) Item(ref y, x, ww, right.height, "Reliques  ×" + Journal.Relics, Journal.Relics >= 3 ? "Bénie : ta foi s'éteint deux fois moins vite." : "Chacune a réchauffé ta foi.", 'R', new Color(0.85f, 0.65f, 0.15f), ref n);
            if (Journal.Lantern) Item(ref y, x, ww, right.height, "Lanterne des Veilleurs", "Ta lumière porte plus loin.", 'L', new Color(0.2f, 0.45f, 0.35f), ref n);
            if (n == 0) GUI.Label(new Rect(x, y, ww, right.height * 0.05f), "Rien. Tes mains tremblent.", small);
            GUI.Label(new Rect(book.x, book.yMax - book.height * 0.05f, book.width, book.height * 0.05f), "Tab  ·  Échap  —  fermer le carnet", label);
        }

        static void Item(ref float y, float x, float ww, float ph, string name, string what, char letter, Color ink, ref int n)
        {
            float s = Mathf.Round(ph * 0.075f);
            Initial(new Rect(x, y, s, s), letter, ink);
            GUI.Label(new Rect(x + s * 1.25f, y, ww - s * 1.3f, s * 0.5f), name, item);
            GUI.Label(new Rect(x + s * 1.25f, y + s * 0.48f, ww - s * 1.3f, s * 0.6f), what, small);
            y += s * 1.2f;
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
