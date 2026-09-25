using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// TOUS LES ECRANS HORS DU JEU (La Couronne, 26/09) : le titre, le SALON (combien
    /// de joueurs, combien de manches, quelle duree), l'ecran EN LIGNE (prepare pour
    /// la Phase 3), l'intro de chaque manche, la pause, la fin de manche, le CHOIX DES
    /// POUVOIRS, et le podium de fin de match.
    ///
    ///   Titre -> Salon -> [rechargement] -> Manche -> Fin de manche -> Choix
    ///                                          ^                          |
    ///                                          +---- [rechargement] ------+
    ///                                                      ... -> Podium
    ///
    /// CHAQUE MANCHE RECHARGE LA SCENE : le monde est rebati a neuf (le Monument a
    /// change de place, les coffres aussi, les gardes sont a leur poste). Ce qui
    /// traverse les manches -- les victoires, les pouvoirs -- vit dans Match, une
    /// classe statique que le rechargement ne touche pas.
    ///
    /// L'ECRAN-TITRE, C'EST LE PERSONNAGE : la camera tourne lentement autour de lui
    /// dans la brume. Le monde vit derriere le menu ; la pause, elle, gele le temps.
    ///
    /// Concept Unity : Time.timeScale = 0 met le temps du jeu en pause ; les Update
    /// tournent toujours mais Time.deltaTime vaut 0. Un menu doit donc s'animer
    /// avec Time.unscaledDeltaTime, qui avance toujours.
    /// </summary>
    public class Menus : MonoBehaviour
    {
        public enum State { Title, Lobby, Online, Briefing, Playing, Paused, RoundOver, Draft, Ended }

        public State Current { get; private set; }

        public bool Blocking { get { return Current != State.Playing; } }

        /// <summary>Apres "Nouveau match" : on rouvre directement le salon au rechargement.</summary>
        static bool openLobby;

        bool showControls;
        float appear;           // 0 -> 1 : apparition du titre
        float veil;             // 0 -> 1 : voile
        float curtain;          // 0 -> 1 : noir complet
        bool leaving;           // le rideau descend ; on recharge (ou on entre) quand il est noir
        System.Action afterCurtain;
        float stateTime;        // depuis quand on est dans cet ecran (temps reel)
        int roundWinner = -1;
        float slowMotion;
        int bellWarnings;
        float botPickTimer;

        // --- le salon
        int lobbyBots = 3;
        int lobbyRounds = 5;
        int lobbyMinutes = 6;

        static Texture2D sideShade;
        static Texture2D vignette;

        void Start()
        {
            Time.timeScale = 1f;
            appear = 0f;
            curtain = 1f;       // on ouvre sur du noir, la foret apparait en fondu
            if (Match.Launched) Go(State.Briefing);
            else Go(openLobby ? State.Lobby : State.Title);
            openLobby = false;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Go(State s)
        {
            Current = s;
            stateTime = 0f;
            showControls = false;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            appear = Mathf.Min(1f, appear + dt * 0.55f);
            stateTime += dt;
            if (slowMotion > 0f)
            {
                slowMotion -= dt;
                Time.timeScale = slowMotion > 0f ? Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(slowMotion / 0.8f)) : 1f;
            }

            Season season = Game.Season;
            if (Current == State.Playing && season != null)
            {
                WarnOfTime(season);
                // Le temps est ecoule : celui qui tient la Couronne gagne ; sinon personne.
                if (season.Over) EndRound(Crown.Holder != null ? Crown.Holder.Index : -1);
            }
            if (Current == State.Briefing && stateTime > BriefingLength && !leaving) Enter();
            if (Current == State.Draft) TickDraft(dt);

            if (FiefInput.CancelPressed && !leaving)
            {
                if (showControls) showControls = false;
                else if (Current == State.Lobby || Current == State.Online) Go(State.Title);
                else if (Current == State.Briefing) Enter();
                else if (Current == State.Playing) Pause();
                else if (Current == State.Paused) Resume();
            }

            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null)
            {
                bool outside = Current == State.Title || Current == State.Lobby || Current == State.Online;
                if (outside)
                {
                    // Six metres, a peine au-dessus de la tete : assez pres pour que
                    // la brume ne l'efface pas, assez loin pour voir la silhouette.
                    cam.autoOrbitSpeed = 3.2f;
                    cam.SetCinematic(6.2f, 6f);
                }
                else if ((Current == State.RoundOver || Current == State.Draft || Current == State.Ended) && Monument.Instance != null)
                {
                    // La manche est finie : la camera quitte tes yeux et tourne lentement
                    // autour du Monument -- on voit la Couronne posee sur l'autel.
                    if (cam.target != Monument.Instance.transform) cam.target = Monument.Instance.transform;
                    cam.autoOrbitSpeed = 9f;
                    cam.SetCinematic(10f, 16f);
                }
                else cam.autoOrbitSpeed = 0f;
            }

            // --- le rideau
            if (leaving)
            {
                curtain = Mathf.MoveTowards(curtain, 1f, dt * 2.6f);
                if (curtain >= 1f)
                {
                    leaving = false;
                    System.Action next = afterCurtain;
                    afterCurtain = null;
                    if (next != null) next.Invoke();
                }
            }
            else curtain = Mathf.MoveTowards(curtain, 0f, dt * (Current == State.Title ? 0.7f : 1.1f));

            // Un seul endroit decide qui a la main : souris libre et joueur fige des
            // qu'un menu est ouvert (ou qu'on est tombe).
            bool dead = Game.Hud != null && Game.Hud.Dead;
            bool blocked = Blocking || leaving || dead;
            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked && !leaving && !dead && Current != State.Briefing;

            if (Game.Player != null) Game.Player.InputLocked = blocked;
            if (cam != null) cam.InputLocked = blocked;
            if (Game.Hud != null && Game.Hud.interactor != null) Game.Hud.interactor.InputLocked = blocked;

            float wantVeil = Current == State.Paused || showControls || Current == State.Lobby || Current == State.Online ? 1f
                           : Current == State.RoundOver && stateTime < 1.3f ? 0.15f     // on regarde d'abord le ralenti
                           : Current == State.RoundOver || Current == State.Draft || Current == State.Ended ? 0.88f : 0f;
            veil = Mathf.MoveTowards(veil, wantVeil, dt * 3f);
        }

        /// <summary>Faire tomber le rideau, puis faire "then" dans le noir.</summary>
        void Curtain(System.Action then)
        {
            if (leaving) return;
            leaving = true;
            afterCurtain = then;
        }

        static void Reload()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        // ================================================================== les manches

        void Enter()
        {
            Curtain(BeginPlaying);
        }

        void BeginPlaying()
        {
            Go(State.Playing);
            Time.timeScale = 1f;
            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null)
            {
                cam.ReleaseCinematic();
                if (cam.target != null) cam.yaw = cam.target.eulerAngles.y;
                cam.pitch = 3f;
            }
            // L'horloge ne part qu'ici : l'intro ne mange pas le temps de la manche.
            if (Game.Season != null) Game.Season.Begin();
            Toasts.Clear();
            Sfx.Bell();
        }

        /// <summary>
        /// FIN DE MANCHE : "winner" a pose la Couronne au Monument (ou tenait la
        /// Couronne quand le temps s'est ecoule ; -1 : personne). C'est ici, et nulle
        /// part ailleurs, qu'une manche se termine -- en Phase 3, sur l'hote seulement.
        /// </summary>
        public void EndRound(int winner)
        {
            if (Current != State.Playing && Current != State.Paused) return;
            if (Game.Season != null) Game.Season.Stop();
            // LE RALENTI : une seconde et demie ou le monde retient son souffle.
            Time.timeScale = winner >= 0 ? 0.25f : 1f;
            slowMotion = winner >= 0 ? 1.6f : 0f;
            if (Match.IsTieBreak && winner >= 0 && !Match.TieBreakers.Contains(winner)) winner = -1;
            roundWinner = winner;
            if (winner >= 0 && Match.Local != null && winner == Match.Local.Index) Stats.Delivered++;
            Match.EndRound(winner);
            Go(State.RoundOver);
            Sfx.Bell();
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.4f);
        }

        /// <summary>Apres la fin de manche : le podium, ou le choix des pouvoirs, ou la manche suivante.</summary>
        void AfterRound()
        {
            if (Match.Over) { Go(State.Ended); return; }
            Match.Draft.Prepare();
            if (Match.Draft.Done) { NextRound(); return; }
            botPickTimer = 1.2f;
            Go(State.Draft);
        }

        void NextRound()
        {
            Curtain(Reload);
        }

        void TickDraft(float dt)
        {
            if (Match.Draft.Done) return;
            int slot = Match.Draft.Current;
            if (slot < 0 || slot >= Match.Slots.Count || !Match.Slots[slot].IsBot) return;
            botPickTimer -= dt;
            if (botPickTimer > 0f) return;
            botPickTimer = 1.1f;
            int card = Match.Draft.BotChoice(slot);
            if (card < 0) card = 0;
            if (Match.Draft.TryPick(slot, card)) Sfx.Pop();
        }

        /// <summary>Trois rappels : a une minute, trente secondes, dix secondes.</summary>
        void WarnOfTime(Season season)
        {
            float left = season.Remaining;
            if (bellWarnings == 0 && left <= 60f) { bellWarnings = 1; Sfx.Bell(); }
            else if (bellWarnings == 1 && left <= 30f) { bellWarnings = 2; Sfx.Bell(); }
            else if (bellWarnings == 2 && left <= 10f) { bellWarnings = 3; Sfx.Bell(); }
        }

        public void Pause()
        {
            Go(State.Paused);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            Go(State.Playing);
            Time.timeScale = 1f;
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ================================================================== textures

        static void EnsureTextures()
        {
            if (sideShade == null)
            {
                sideShade = new Texture2D(256, 1, TextureFormat.RGBA32, false);
                sideShade.wrapMode = TextureWrapMode.Clamp;
                for (int i = 0; i < 256; i++)
                {
                    float u = i / 255f;
                    float a = 0.80f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.62f)));
                    sideShade.SetPixel(i, 0, new Color(0.01f, 0.015f, 0.015f, a));
                }
                sideShade.Apply();
            }
            if (vignette == null)
            {
                const int Size = 64;
                vignette = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                vignette.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x + 0.5f) / Size * 2f - 1f;
                        float dy = (y + 0.5f) / Size * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41421f;
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.32f) / 0.68f)) * 0.78f;
                        vignette.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                    }
                }
                vignette.Apply();
            }
        }

        // ================================================================== dessin

        void OnGUI()
        {
            UiStyle.Ensure();
            EnsureTextures();
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);

            if (Current == State.Title)
            {
                GUI.DrawTexture(screen, vignette, ScaleMode.StretchToFill);
                GUI.DrawTexture(screen, sideShade, ScaleMode.StretchToFill);
            }
            if (veil > 0.001f) UiStyle.Fill(screen, new Color(UiStyle.Scrim.r, UiStyle.Scrim.g, UiStyle.Scrim.b, UiStyle.Scrim.a * veil));

            if (showControls) DrawControls();
            else
            {
                switch (Current)
                {
                    case State.Title: DrawTitle(); break;
                    case State.Lobby: DrawLobby(); break;
                    case State.Online: DrawOnline(); break;
                    case State.Briefing: DrawBriefing(); break;
                    case State.Paused: DrawPause(); break;
                    case State.RoundOver: DrawRoundOver(); break;
                    case State.Draft: DrawDraft(); break;
                    case State.Ended: DrawEnd(); break;
                }
            }

            // Le rideau passe par-dessus tout, y compris le texte.
            if (curtain > 0.001f) UiStyle.Fill(screen, new Color(0f, 0f, 0f, curtain));
        }

        // ------------------------------------------------------------------ outils de dessin

        /// <summary>
        /// Des braises qui montent lentement devant les menus, comme d'un feu hors
        /// champ. Dessinees par l'interface : elles vivent meme quand le temps est fige.
        /// </summary>
        static readonly Vector3[] Embers = new Vector3[70];
        static bool embersReady;

        static void DrawEmbers(float strength)
        {
            if (strength <= 0.01f) return;
            if (!embersReady)
            {
                System.Random r = new System.Random(3);
                for (int i = 0; i < Embers.Length; i++)
                    Embers[i] = new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble() * 100f);
                embersReady = true;
            }
            float t = Time.unscaledTime;
            for (int i = 0; i < Embers.Length; i++)
            {
                Vector3 e = Embers[i];
                float speed = 0.018f + (e.z % 7f) * 0.004f;
                float y = Mathf.Repeat(e.y - t * speed, 1f);
                float x = e.x + Mathf.Sin(t * 0.6f + e.z) * 0.012f;
                float life = Mathf.Sin(y * Mathf.PI);
                float flicker = 0.6f + 0.4f * Mathf.Sin(t * (3f + e.z % 5f) + e.z);
                float size = UiStyle.S(2f + (e.z % 3f));
                Color c = Color.Lerp(new Color(1f, 0.45f, 0.15f), new Color(1f, 0.8f, 0.45f), (e.z % 10f) / 10f);
                c.a = life * flicker * 0.75f * strength;
                UiStyle.Icon(new Rect(x * Screen.width, y * Screen.height, size, size), UiStyle.Shape.Dot, c);
            }
        }

        /// <summary>
        /// Une entree de menu : du texte, pas une boite. Au survol, elle s'eclaire en
        /// or, un losange apparait a gauche et un filet se dessine dessous.
        /// </summary>
        static bool Entry(Rect r, string text, bool primary, float alpha)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            GUIStyle style = primary ? UiStyle.Title : UiStyle.Head;
            int previous = style.fontSize;
            if (!primary) style.fontSize = UiStyle.S(21);
            Color c = hover ? new Color(1f, 0.86f, 0.52f, alpha) : primary ? new Color(0.94f, 0.88f, 0.74f, alpha) : new Color(0.74f, 0.69f, 0.6f, alpha);
            float indent = hover ? UiStyle.S(26) : UiStyle.S(18);
            UiStyle.Tinted(new Rect(r.x + indent, r.y, r.width - indent, r.height), text, style, c);
            style.fontSize = previous;
            if (hover)
            {
                float d = UiStyle.S(9);
                UiStyle.Icon(new Rect(r.x + UiStyle.S(4), r.center.y - d * 0.5f, d, d), UiStyle.Shape.Diamond, new Color(0.92f, 0.36f, 0.26f, alpha));
                UiStyle.FadeBand(new Rect(r.x, r.yMax - UiStyle.S(4), r.width * 0.8f, 1f), new Color(0.86f, 0.7f, 0.36f, alpha * 0.8f));
            }
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Une pastille a choisir (3, 5, 7, 10...). Doree si choisie.</summary>
        static bool Chip(Rect r, string text, bool on)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            UiStyle.Fill(r, on ? new Color(0.86f, 0.7f, 0.36f, 0.22f) : new Color(1f, 1f, 1f, hover ? 0.08f : 0.035f));
            Color edge = on ? Palette.Gold : new Color(0.55f, 0.44f, 0.26f, hover ? 0.8f : 0.4f);
            Border(r, edge);
            UiStyle.Tinted(r, text, UiStyle.Centered, on ? Palette.Gold : hover ? UiStyle.Ink : UiStyle.InkDim);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        static void Border(Rect r, Color c)
        {
            UiStyle.Fill(new Rect(r.x, r.y, r.width, 1f), c);
            UiStyle.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
            UiStyle.Fill(new Rect(r.x, r.y, 1f, r.height), c);
            UiStyle.Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
        }

        static void Centered(float y, float h, string text, GUIStyle style, Color c)
        {
            TextAnchor previous = style.alignment;
            style.alignment = TextAnchor.MiddleCenter;
            UiStyle.Tinted(new Rect(2f, y + 2f, Screen.width, h), text, style, new Color(0f, 0f, 0f, c.a * 0.8f));
            UiStyle.Tinted(new Rect(0f, y, Screen.width, h), text, style, c);
            style.alignment = previous;
        }

        static GUIStyle bigCentre;
        /// <summary>Un grand titre, centre, de la taille voulue.</summary>
        static void Headline(float y, float size, string text, Color c)
        {
            if (bigCentre == null || bigCentre.font != UiStyle.Big.font) bigCentre = new GUIStyle(UiStyle.Big);
            bigCentre.fontSize = UiStyle.S(size);
            bigCentre.alignment = TextAnchor.MiddleCenter;
            bigCentre.wordWrap = false;
            UiStyle.Tinted(new Rect(3f, y + 4f, Screen.width, UiStyle.S(size * 1.3f)), text, bigCentre, new Color(0f, 0f, 0f, c.a * 0.8f));
            UiStyle.Tinted(new Rect(0f, y, Screen.width, UiStyle.S(size * 1.3f)), text, bigCentre, c);
        }

        // ------------------------------------------------------------------ le titre

        void DrawTitle()
        {
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.15f) / 0.85f));
            float late = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.45f) / 0.55f));
            DrawEmbers(ease);

            float x = UiStyle.S(110);
            float w = Mathf.Min(UiStyle.S(640), Screen.width - x * 2f);
            float y = Screen.height - UiStyle.S(540) + (1f - ease) * UiStyle.S(18);

            GUIStyle big = UiStyle.Big;
            int previous = big.fontSize;
            big.fontSize = UiStyle.S(112);
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            UiStyle.Tinted(new Rect(x + 4f, y + 5f, w, UiStyle.S(130)), UiStyle.Spaced("FIEF"), big, new Color(0f, 0f, 0f, 0.8f));
            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(130)), UiStyle.Spaced("FIEF"), big, new Color(0.93f, 0.78f, 0.45f));
            big.fontSize = previous;
            y += UiStyle.S(128);
            // La Couronne, sous le titre.
            Pictos.Crown(new Rect(x + UiStyle.S(6), y, UiStyle.S(42), UiStyle.S(34)), 1f);
            UiStyle.Tinted(new Rect(x + UiStyle.S(60), y, w, UiStyle.S(34)), UiStyle.Spaced("LA COURONNE"), UiStyle.Head, new Color(0.95f, 0.85f, 0.6f));
            GUI.color = was;
            y += UiStyle.S(50);
            GUI.color = new Color(1f, 1f, 1f, ease);
            UiStyle.Rule(new Rect(x, y, UiStyle.S(420) * ease, UiStyle.S(8)));
            GUI.color = was;
            y += UiStyle.S(40);

            float bw = UiStyle.S(420);
            bool ready = late > 0.9f && !leaving;
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(48)), "Jouer", true, late) && ready) Go(State.Lobby);
            y += UiStyle.S(54);
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(36)), "En ligne", false, late) && ready) Go(State.Online);
            y += UiStyle.S(40);
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(36)), "Commandes", false, late) && ready) showControls = true;
            y += UiStyle.S(40);
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(36)), "Quitter", false, late) && ready) Quit();

            // La version, en bas a droite : c'est elle qui dit quel code tourne.
            UiStyle.Tinted(new Rect(0f, Screen.height - UiStyle.S(34), Screen.width - UiStyle.S(24), UiStyle.S(20)), Game.Version, RightTiny(),
                           new Color(UiStyle.InkFaint.r, UiStyle.InkFaint.g, UiStyle.InkFaint.b, late));
        }

        // ------------------------------------------------------------------ le salon

        /// <summary>
        /// LE SALON : quatre places (toi, et jusqu'a trois autres -- des bots en
        /// Phase 1, des joueurs en ligne en Phase 3), le nombre de manches, la duree
        /// maximale d'une manche. Clic sur une place vide : un bot s'y assoit ; clic
        /// sur un bot : il s'en va.
        /// </summary>
        void DrawLobby()
        {
            DrawEmbers(0.5f);
            float w = Mathf.Min(UiStyle.S(820), Screen.width - UiStyle.S(40));
            float h = UiStyle.S(560);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);
            float x = box.x + UiStyle.S(36), inner = w - UiStyle.S(72);
            float y = box.y + UiStyle.S(26);

            GUIStyle title = UiStyle.Title;
            TextAnchor previous = title.alignment;
            title.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(box.x, y, w, UiStyle.S(40)), UiStyle.Spaced("LE SALON"), title);
            title.alignment = previous;
            y += UiStyle.S(50);
            UiStyle.Rule(new Rect(x, y, inner, UiStyle.S(8)));
            y += UiStyle.S(24);

            // --- les quatre places
            float gap = UiStyle.S(14);
            float cw = (inner - gap * 3f) / 4f, chh = UiStyle.S(170);
            string[] names = { "Toi", "Mahaut", "Oswin", "Guerin" };
            for (int i = 0; i < 4; i++)
            {
                Rect card = new Rect(x + i * (cw + gap), y, cw, chh);
                bool taken = i <= lobbyBots;
                bool hover = card.Contains(Event.current.mousePosition);
                Color colour = Match.ColourOf(i);
                UiStyle.Fill(card, taken ? new Color(0.08f, 0.07f, 0.06f, 0.95f) : new Color(1f, 1f, 1f, hover ? 0.05f : 0.02f));
                Border(card, taken ? new Color(colour.r, colour.g, colour.b, 0.8f) : new Color(0.55f, 0.44f, 0.26f, 0.35f));
                if (taken)
                {
                    // Une banniere a sa couleur, un halo, le nom.
                    Rect banner = new Rect(card.center.x - UiStyle.S(22), card.y, UiStyle.S(44), UiStyle.S(78));
                    UiStyle.Fill(banner, colour);
                    UiStyle.Icon(new Rect(banner.x, banner.yMax - UiStyle.S(14), banner.width, UiStyle.S(28)), UiStyle.Shape.Triangle, colour);
                    UiStyle.Icon(new Rect(banner.center.x - UiStyle.S(9), banner.y + UiStyle.S(26), UiStyle.S(18), UiStyle.S(18)), UiStyle.Shape.Dot, new Color(1f, 1f, 1f, 0.8f));
                    UiStyle.Tinted(new Rect(card.x, card.y + UiStyle.S(104), card.width, UiStyle.S(28)), names[i], UiStyle.Centered, i == 0 ? Palette.Gold : UiStyle.Ink);
                    UiStyle.Tinted(new Rect(card.x, card.y + UiStyle.S(132), card.width, UiStyle.S(20)), i == 0 ? "toi" : "bot", UiStyle.CenteredSmall, UiStyle.InkFaint);
                }
                else
                {
                    UiStyle.Tinted(new Rect(card.x, card.y, card.width, card.height), "+", UiStyle.Title, hover ? Palette.Gold : UiStyle.InkFaint);
                }
                if (i > 0 && GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    // Une place vide : on remplit jusqu'a elle. Un bot : il s'en va (au moins un adversaire).
                    lobbyBots = taken ? Mathf.Max(1, i - 1) : i;
                    Sfx.Pop();
                }
            }
            y += chh + UiStyle.S(30);

            // --- les manches, la duree
            float labelW = UiStyle.S(200);
            float chipW = UiStyle.S(78), chipH = UiStyle.S(40);
            UiStyle.Tinted(new Rect(x, y, labelW, chipH), "Manches", UiStyle.Head, UiStyle.Ink);
            for (int i = 0; i < Match.RoundChoices.Length; i++)
            {
                int v = Match.RoundChoices[i];
                if (Chip(new Rect(x + labelW + i * (chipW + UiStyle.S(10)), y, chipW, chipH), v.ToString(), lobbyRounds == v)) { lobbyRounds = v; Sfx.Pop(); }
            }
            y += chipH + UiStyle.S(14);
            UiStyle.Tinted(new Rect(x, y, labelW, chipH), "Durée max", UiStyle.Head, UiStyle.Ink);
            for (int i = 0; i < Match.MinuteChoices.Length; i++)
            {
                int v = Match.MinuteChoices[i];
                if (Chip(new Rect(x + labelW + i * (chipW + UiStyle.S(10)), y, chipW, chipH), v + " min", lobbyMinutes == v)) { lobbyMinutes = v; Sfx.Pop(); }
            }
            y += chipH + UiStyle.S(10);
            // Une estimation, en un mot : une manche dure rarement tout son temps.
            int estimate = Mathf.RoundToInt(lobbyRounds * lobbyMinutes * 0.85f);
            UiStyle.Tinted(new Rect(x + labelW, y, inner - labelW, UiStyle.S(20)), "≈ " + estimate + " min", UiStyle.Small, UiStyle.InkFaint);

            // --- les boutons
            float bh = UiStyle.S(46);
            float by = box.yMax - bh - UiStyle.S(24);
            if (GUI.Button(new Rect(x, by, inner * 0.62f, bh), "COMMENCER", UiStyle.ButtonPrimary) && !leaving)
            {
                Match.Begin(lobbyBots, lobbyRounds, lobbyMinutes);
                Match.Launch();
                Stats.Reset();
                Curtain(Reload);
            }
            if (GUI.Button(new Rect(x + inner * 0.66f, by, inner * 0.34f, bh), "Retour", UiStyle.Button) && !leaving) Go(State.Title);
        }

        // ------------------------------------------------------------------ en ligne

        /// <summary>
        /// EN LIGNE : prepare, pas encore branche. Le jeu est deja coupe en places
        /// (Match.Slots) et tout passe par des methodes que l'hote appellera -- mais
        /// le transport (Steam, Netcode) vient en Phase 3. On le dit franchement.
        /// </summary>
        void DrawOnline()
        {
            DrawEmbers(0.5f);
            float w = Mathf.Min(UiStyle.S(620), Screen.width - UiStyle.S(40));
            float h = UiStyle.S(400);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);
            float x = box.x + UiStyle.S(40), inner = w - UiStyle.S(80);
            float y = box.y + UiStyle.S(26);
            GUIStyle title = UiStyle.Title;
            TextAnchor previous = title.alignment;
            title.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(box.x, y, w, UiStyle.S(40)), UiStyle.Spaced("EN LIGNE"), title);
            title.alignment = previous;
            y += UiStyle.S(50);
            UiStyle.Rule(new Rect(x, y, inner, UiStyle.S(8)));
            y += UiStyle.S(30);

            // Deux entrees grisees : on voit ce qui vient, on ne peut pas encore cliquer.
            for (int i = 0; i < 2; i++)
            {
                Rect r = new Rect(x, y, inner, UiStyle.S(56));
                UiStyle.Fill(r, new Color(1f, 1f, 1f, 0.03f));
                Border(r, new Color(0.55f, 0.44f, 0.26f, 0.3f));
                UiStyle.Tinted(new Rect(r.x + UiStyle.S(20), r.y, r.width, r.height), i == 0 ? "Héberger une partie" : "Rejoindre un ami", UiStyle.Head, UiStyle.InkFaint);
                UiStyle.Tinted(new Rect(r.x, r.y, r.width - UiStyle.S(20), r.height), "bientôt", RightTiny(), new Color(0.86f, 0.7f, 0.36f, 0.7f));
                y += UiStyle.S(66);
            }
            y += UiStyle.S(6);
            UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(22)), "Quatre joueurs, par Steam.", UiStyle.Centered, UiStyle.InkDim);

            float bh = UiStyle.S(44);
            if (GUI.Button(new Rect(x, box.yMax - bh - UiStyle.S(24), inner, bh), "Retour", UiStyle.Button)) Go(State.Title);
        }

        static GUIStyle rightTiny;
        static GUIStyle RightTiny()
        {
            if (rightTiny == null || rightTiny.fontSize != UiStyle.Tiny.fontSize)
            {
                rightTiny = new GUIStyle(UiStyle.Tiny);
                rightTiny.alignment = TextAnchor.MiddleRight;
            }
            return rightTiny;
        }

        // ------------------------------------------------------------------ l'intro de manche

        float BriefingLength { get { return Match.Played == 0 && !Match.IsTieBreak ? 6.5f : 3.2f; } }

        /// <summary>
        /// L'INTRO DE MANCHE, toute seule, quelques secondes : "MANCHE 2", et la
        /// premiere fois, la regle en deux lignes. Echap passe.
        /// </summary>
        void DrawBriefing()
        {
            UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.01f, 0.02f, 0.7f));
            DrawEmbers(0.35f);
            float t = stateTime;
            float a = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((BriefingLength - t) / 0.6f);
            float y = Screen.height * 0.36f;

            string head = Match.IsTieBreak ? UiStyle.Spaced("DÉPARTAGE") : UiStyle.Spaced("MANCHE " + Match.RoundNumber);
            Headline(y, 64, head, new Color(0.93f, 0.78f, 0.45f, a));
            y += UiStyle.S(96);

            if (Match.IsTieBreak)
            {
                string who = "";
                for (int i = 0; i < Match.TieBreakers.Count; i++) who += (i > 0 ? "  ·  " : "") + Match.Slots[Match.TieBreakers[i]].Name;
                Centered(y, UiStyle.S(30), who, UiStyle.Head, new Color(0.95f, 0.9f, 0.8f, a));
            }
            else if (Match.Played == 0)
            {
                float b = Mathf.Clamp01((t - 1.2f) / 0.6f) * a;
                Centered(y, UiStyle.S(30), "La Couronne dort en haut du château.", UiStyle.Head, new Color(0.95f, 0.9f, 0.8f, b));
                float c = Mathf.Clamp01((t - 2.6f) / 0.6f) * a;
                Centered(y + UiStyle.S(40), UiStyle.S(30), "Porte-la au Monument : la colonne bleue.", UiStyle.Head, new Color(0.6f, 0.8f, 1f, c));
            }
            else ScoreRow(y, a);

            UiStyle.Tinted(new Rect(0f, Screen.height - UiStyle.S(48), Screen.width - UiStyle.S(30), UiStyle.S(20)), "Échap", RightTiny(), UiStyle.InkFaint);
        }

        /// <summary>Une rangee : chaque joueur, sa couleur, ses manches gagnees (des couronnes).</summary>
        static void ScoreRow(float y, float a)
        {
            float cell = UiStyle.S(170);
            float total = cell * Match.Slots.Count;
            float x = (Screen.width - total) * 0.5f;
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                Rect r = new Rect(x + i * cell, y, cell - UiStyle.S(10), UiStyle.S(70));
                UiStyle.Fill(new Rect(r.x, r.y, r.width, UiStyle.S(3)), s.Colour);
                UiStyle.Tinted(new Rect(r.x, r.y + UiStyle.S(6), r.width, UiStyle.S(26)), s.Name, UiStyle.Centered, s.IsLocal ? Palette.Gold : UiStyle.Ink);
                float cw = UiStyle.S(24);
                float cx = r.center.x - Mathf.Max(1, s.Wins) * cw * 0.5f;
                if (s.Wins == 0) UiStyle.Tinted(new Rect(r.x, r.y + UiStyle.S(36), r.width, UiStyle.S(24)), "–", UiStyle.Centered, UiStyle.InkFaint);
                for (int k = 0; k < s.Wins; k++) Pictos.Crown(new Rect(cx + k * cw, r.y + UiStyle.S(38), cw - UiStyle.S(2), UiStyle.S(19)), 1f);
            }
            GUI.color = was;
        }

        // ------------------------------------------------------------------ la pause

        void DrawPause()
        {
            DrawEmbers(0.6f);
            float w = UiStyle.S(620);
            float h = UiStyle.S(400);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(40);
            float y = box.y + UiStyle.S(30);
            float bw = w - UiStyle.S(80);

            GUIStyle title = UiStyle.Title;
            TextAnchor previous = title.alignment;
            title.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(box.x, y, w, UiStyle.S(40)), UiStyle.Spaced("EN PAUSE"), title);
            title.alignment = previous;
            y += UiStyle.S(48);
            UiStyle.Rule(new Rect(x, y, bw, UiStyle.S(8)));
            y += UiStyle.S(18);
            Season s = Game.Season;
            if (s != null)
            {
                string round = Match.IsTieBreak ? "Départage" : "Manche " + Match.RoundNumber + " / " + Match.Rounds;
                UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(20)), round + "   ·   " + Hud.Clock(s.Remaining), UiStyle.CenteredSmall, UiStyle.InkDim);
            }
            y += UiStyle.S(30);

            if (Entry(new Rect(x, y, bw, UiStyle.S(44)), "Reprendre", true, 1f)) Resume();
            y += UiStyle.S(52);
            if (Entry(new Rect(x, y, bw, UiStyle.S(36)), "Commandes", false, 1f)) showControls = true;
            y += UiStyle.S(40);
            if (Entry(new Rect(x, y, bw, UiStyle.S(36)), "Abandonner le match", false, 1f) && !leaving)
            {
                Match.Abandon();
                Curtain(Reload);
            }
            y += UiStyle.S(40);
            if (Entry(new Rect(x, y, bw, UiStyle.S(36)), "Quitter le jeu", false, 1f)) Quit();
        }

        // ------------------------------------------------------------------ fin de manche

        /// <summary>
        /// FIN DE MANCHE : qui l'a gagnee (sa couleur, en grand), et le score. Puis on
        /// passe au choix des pouvoirs -- tout seul, ou d'un clic.
        /// </summary>
        void DrawRoundOver()
        {
            // Le ralenti d'abord (le monde, la Couronne sur l'autel), puis le verdict.
            float a = Mathf.Clamp01((stateTime - 1.2f) / 0.6f);
            if (a <= 0f) return;
            DrawEmbers(0.4f * a);
            float y = Screen.height * 0.24f;
            PlayerSlot w = roundWinner >= 0 && roundWinner < Match.Slots.Count ? Match.Slots[roundWinner] : null;
            float cs = UiStyle.S(90);
            Pictos.Crown(new Rect((Screen.width - cs) * 0.5f, y, cs, cs * 0.8f), w != null ? a : 0.35f * a);
            y += cs;
            if (w == null) Headline(y, 54, UiStyle.Spaced("PERSONNE"), new Color(0.8f, 0.75f, 0.7f, a));
            else if (w.IsLocal) Headline(y, 58, UiStyle.Spaced("MANCHE GAGNÉE"), new Color(0.95f, 0.8f, 0.4f, a));
            else Headline(y, 58, UiStyle.Spaced(w.Name.ToUpperInvariant()), new Color(w.Colour.r, w.Colour.g, w.Colour.b, a));
            y += UiStyle.S(86);
            Centered(y, UiStyle.S(26), w == null ? "Le temps s'est écoulé." : w.IsLocal ? "Tu as posé la Couronne." : "a posé la Couronne.", UiStyle.Head,
                     new Color(0.9f, 0.85f, 0.75f, a * 0.8f));
            y += UiStyle.S(60);
            ScoreRow(y, a);

            float bw = UiStyle.S(300), bh = UiStyle.S(46);
            Rect next = new Rect((Screen.width - bw) * 0.5f, Screen.height - UiStyle.S(120), bw, bh);
            string label = Match.Over ? "LE PODIUM" : "LA SUITE";
            if ((GUI.Button(next, label, UiStyle.ButtonPrimary) || stateTime > 10f) && stateTime > 2.2f) AfterRound();
        }

        // ------------------------------------------------------------------ le choix des pouvoirs

        /// <summary>
        /// LE CHOIX DES POUVOIRS : les cartes etalees au milieu, l'ordre en haut (le
        /// moins de manches d'abord, le vainqueur en dernier). Les bots choisissent
        /// tout seuls ; a ton tour, clique une carte.
        /// </summary>
        void DrawDraft()
        {
            DrawEmbers(0.4f);
            float y = UiStyle.S(60);
            Headline(y, 44, UiStyle.Spaced("LES POUVOIRS"), new Color(0.93f, 0.78f, 0.45f, 1f));
            y += UiStyle.S(70);

            // --- l'ordre
            List<int> order = Match.Draft.Order;
            float chip = UiStyle.S(150);
            float ox = (Screen.width - chip * order.Count) * 0.5f;
            for (int i = 0; i < order.Count; i++)
            {
                PlayerSlot s = Match.Slots[order[i]];
                Rect r = new Rect(ox + i * chip, y, chip - UiStyle.S(10), UiStyle.S(34));
                bool now = !Match.Draft.Done && Match.Draft.Current == order[i];
                bool done = i < Match.Draft.Turn;
                UiStyle.Fill(r, now ? new Color(s.Colour.r, s.Colour.g, s.Colour.b, 0.25f) : new Color(0f, 0f, 0f, 0.4f));
                UiStyle.Fill(new Rect(r.x, r.yMax - UiStyle.S(3), r.width, UiStyle.S(3)), new Color(s.Colour.r, s.Colour.g, s.Colour.b, done ? 0.4f : 1f));
                UiStyle.Tinted(r, (i + 1) + ".  " + s.Name, UiStyle.Centered, now ? Palette.Gold : done ? UiStyle.InkFaint : UiStyle.Ink);
            }
            y += UiStyle.S(62);

            // --- les cartes
            List<Power> offer = Match.Draft.Offer;
            int me = Match.Local != null ? Match.Local.Index : 0;
            bool myTurn = !Match.Draft.Done && Match.Draft.Current == me;
            float cw = UiStyle.S(190), chh = UiStyle.S(250), gap = UiStyle.S(18);
            float total = offer.Count * cw + Mathf.Max(0, offer.Count - 1) * gap;
            float cx = (Screen.width - total) * 0.5f;
            for (int i = 0; i < offer.Count; i++)
            {
                Power p = offer[i];
                Rect card = new Rect(cx + i * (cw + gap), y, cw, chh);
                bool owned = Match.Local != null && Match.Local.Has(p);
                bool hover = myTurn && !owned && card.Contains(Event.current.mousePosition);
                if (hover) card.y -= UiStyle.S(8);
                Color tint = PowerInfo.Tint(p);
                UiStyle.DropShadow(card, UiStyle.S(14));
                UiStyle.Fill(card, new Color(0.07f, 0.06f, 0.05f, 0.97f));
                Border(card, hover ? Palette.Gold : new Color(tint.r, tint.g, tint.b, 0.7f));
                UiStyle.Fill(new Rect(card.x, card.y, card.width, UiStyle.S(4)), tint);
                float md = UiStyle.S(96);
                Rect medal = new Rect(card.center.x - md * 0.5f, card.y + UiStyle.S(26), md, md);
                UiStyle.Icon(new Rect(medal.x - 3f, medal.y - 3f, medal.width + 6f, medal.height + 6f), UiStyle.Shape.Dot, new Color(tint.r, tint.g, tint.b, 0.6f));
                UiStyle.Icon(medal, UiStyle.Shape.Dot, new Color(0.04f, 0.035f, 0.03f, 1f));
                Pictos.Draw(new Rect(medal.x + md * 0.18f, medal.y + md * 0.18f, md * 0.64f, md * 0.64f), p, owned);
                UiStyle.Tinted(new Rect(card.x, card.y + UiStyle.S(140), card.width, UiStyle.S(30)), PowerInfo.Name(p), UiStyle.Centered, owned ? UiStyle.InkFaint : UiStyle.Ink);
                UiStyle.Tinted(new Rect(card.x, card.y + UiStyle.S(172), card.width, UiStyle.S(24)), PowerInfo.Effect(p), UiStyle.CenteredSmall, new Color(tint.r, tint.g, tint.b, owned ? 0.4f : 0.95f));
                if (owned) UiStyle.Tinted(new Rect(card.x, card.y + UiStyle.S(206), card.width, UiStyle.S(20)), "déjà à toi", UiStyle.CenteredSmall, UiStyle.InkFaint);
                if (myTurn && !owned && GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    if (Match.Draft.TryPick(me, i)) { Sfx.Discovery(); botPickTimer = 1f; }
                }
            }
            y += chh + UiStyle.S(30);

            // --- ce que chacun a deja
            if (myTurn) Centered(y, UiStyle.S(26), "À toi de choisir.", UiStyle.Head, Palette.Gold);
            else if (!Match.Draft.Done)
            {
                PlayerSlot who = Match.Slots[Match.Draft.Current];
                Centered(y, UiStyle.S(26), who.Name + "…", UiStyle.Head, who.Colour);
            }

            if (Match.Draft.Done)
            {
                float bw = UiStyle.S(320), bh = UiStyle.S(46);
                Rect next = new Rect((Screen.width - bw) * 0.5f, Screen.height - UiStyle.S(110), bw, bh);
                string label = Match.IsTieBreak ? "LE DÉPARTAGE" : "MANCHE " + Match.RoundNumber;
                if (GUI.Button(next, label, UiStyle.ButtonPrimary) && !leaving) NextRound();
            }
        }

        // ------------------------------------------------------------------ le podium

        void DrawEnd()
        {
            float a = Mathf.Clamp01(stateTime / 0.8f);
            DrawEmbers(0.6f * a);
            PlayerSlot champion = Match.Champion;
            float y = Screen.height * 0.12f;
            float cs = UiStyle.S(110);
            Pictos.Crown(new Rect((Screen.width - cs) * 0.5f, y, cs, cs * 0.8f), a);
            y += cs;
            if (champion == null) Headline(y, 56, UiStyle.Spaced("MATCH NUL"), new Color(0.85f, 0.8f, 0.72f, a));
            else if (champion.IsLocal) Headline(y, 64, UiStyle.Spaced("VICTOIRE"), new Color(0.95f, 0.8f, 0.4f, a));
            else Headline(y, 60, UiStyle.Spaced(champion.Name.ToUpperInvariant()), new Color(champion.Colour.r, champion.Colour.g, champion.Colour.b, a));
            y += UiStyle.S(92);

            // Le podium : du plus de manches au moins.
            List<PlayerSlot> order = new List<PlayerSlot>(Match.Slots);
            order.Sort((first, second) => second.Wins.CompareTo(first.Wins));
            float w = Mathf.Min(UiStyle.S(560), Screen.width - UiStyle.S(40));
            float x = (Screen.width - w) * 0.5f;
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            for (int i = 0; i < order.Count; i++)
            {
                PlayerSlot s = order[i];
                float rh = UiStyle.S(i == 0 ? 52 : 42);
                Rect row = new Rect(x, y, w, rh - UiStyle.S(6));
                UiStyle.CardFrame(row);
                UiStyle.Fill(new Rect(row.x, row.y + UiStyle.S(6), UiStyle.S(4), row.height - UiStyle.S(12)), s.Colour);
                GUIStyle nameStyle = i == 0 ? UiStyle.Head : UiStyle.Label;
                UiStyle.Tinted(new Rect(row.x + UiStyle.S(16), row.y, UiStyle.S(40), row.height), (i + 1) + ".", nameStyle, UiStyle.InkDim);
                UiStyle.Tinted(new Rect(row.x + UiStyle.S(48), row.y, w * 0.4f, row.height), s.Name, nameStyle, s.IsLocal ? Palette.Gold : UiStyle.Ink);
                for (int k = 0; k < s.Wins; k++)
                    Pictos.Crown(new Rect(row.xMax - UiStyle.S(30) - k * UiStyle.S(26), row.center.y - UiStyle.S(9), UiStyle.S(24), UiStyle.S(19)), 1f);
                y += rh;
            }
            y += UiStyle.S(10);

            // Ton match en six cases : un chiffre, un mot.
            string[] numbers =
            {
                Stats.Delivered.ToString(), Stats.CrownsTaken.ToString(), Stats.PlayersDowned.ToString(),
                Stats.GuardsDowned.ToString(), Stats.Chests.ToString(), Stats.Deaths.ToString()
            };
            string[] words = { "couronnes posées", "couronnes prises", "joueurs abattus", "gardes abattus", "coffres", "chutes" };
            float cellW = w / 3f, cellH = UiStyle.S(50);
            for (int i = 0; i < numbers.Length; i++)
            {
                Rect cell = new Rect(x + cellW * (i % 3), y + cellH * (i / 3), cellW - UiStyle.S(6), cellH - UiStyle.S(6));
                UiStyle.Fill(cell, new Color(1f, 1f, 1f, 0.03f));
                GUIStyle big = UiStyle.Value;
                TextAnchor wasA = big.alignment;
                big.alignment = TextAnchor.MiddleCenter;
                UiStyle.Tinted(new Rect(cell.x, cell.y, cell.width, cell.height * 0.62f), numbers[i], big, UiStyle.Ink);
                big.alignment = wasA;
                UiStyle.Tinted(new Rect(cell.x, cell.y + cell.height * 0.58f, cell.width, cell.height * 0.4f), words[i], UiStyle.CenteredSmall, UiStyle.InkFaint);
            }
            if (Stats.KingDowned > 0)
                UiStyle.Tinted(new Rect(x, y + cellH * 2f + UiStyle.S(4), w, UiStyle.S(22)), "Tu as abattu le Roi Creux.", UiStyle.Centered, new Color(0.7f, 0.82f, 1f));
            GUI.color = was;

            float bh = UiStyle.S(46);
            float by = Screen.height - UiStyle.S(100);
            if (GUI.Button(new Rect(x, by, w * 0.55f, bh), "NOUVEAU MATCH", UiStyle.ButtonPrimary) && stateTime > 1.5f && !leaving)
            {
                Match.Abandon();
                openLobby = true;
                Curtain(Reload);
            }
            if (GUI.Button(new Rect(x + w * 0.6f, by, w * 0.4f, bh), "Quitter", UiStyle.Button) && stateTime > 1.5f) Quit();
        }

        // ------------------------------------------------------------------ les commandes

        /// <summary>La liste des commandes. A tenir a jour a chaque nouvelle action.</summary>
        public static readonly string[,] Controls =
        {
            { "ZQSD / WASD", "Se déplacer" },
            { "Souris", "Regarder" },
            { "Maj", "Courir" },
            { "Espace", "Sauter (deux fois : Double saut)" },
            { "Clic gauche", "Épée, ou l'objet en main" },
            { "Clic droit", "Pousser" },
            { "1 / 2 / 3, molette", "Les objets" },
            { "E", "Prendre, ouvrir, poser la Couronne" },
            { "F", "Grimper" },
            { "R", "Ruée (le pouvoir)" },
            { "Tab", "Le score du match" },
            { "Échap", "Pause" }
        };

        void DrawControls()
        {
            int count = Controls.GetLength(0);
            float w = UiStyle.S(560);
            float h = UiStyle.S(170) + count * UiStyle.S(26);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(32);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(64);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), UiStyle.Spaced("COMMANDES"), UiStyle.Title);
            y += UiStyle.S(42);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(16);

            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0) UiStyle.Fill(new Rect(x - UiStyle.S(8), y - UiStyle.S(2), bw + UiStyle.S(16), UiStyle.S(26)), new Color(1f, 1f, 1f, 0.03f));
                Rect key = new Rect(x, y + UiStyle.S(1), UiStyle.S(170), UiStyle.S(22));
                UiStyle.Pill(key);
                UiStyle.Tinted(key, Controls[i, 0], UiStyle.CenteredSmall, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(190), y, bw - UiStyle.S(190), UiStyle.S(24)), Controls[i, 1], UiStyle.Small);
                y += UiStyle.S(26);
            }

            y = box.yMax - UiStyle.S(58);
            if (GUI.Button(new Rect(x, y, bw, UiStyle.S(40)), "Retour", UiStyle.ButtonPrimary)) showControls = false;
        }
    }
}
