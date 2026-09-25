using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// TOUS LES ECRANS HORS DU JEU (refaits le 27/09 -- Martin : "les trucs en dehors
    /// du jeu, style menu, c'est tout moche et bugge").
    ///
    /// Le parti pris : DU TEXTE, RIEN QUE DU TEXTE. Pas de cadre dore, pas de pastille,
    /// pas de pictogramme. Une colonne de mots sur la foret qui bouge derriere, comme
    /// les menus de Journey ou d'Inside. Ce qui est choisi est en or, avec un trait.
    /// Tout se fait AU CLAVIER (fleches + Entree + Echap) comme a la souris.
    ///
    ///   Titre -> Salon -> CHOIX (1re capacite) -> [rechargement] -> Manche 1
    ///                                                                 |
    ///   ... <- [rechargement] <- CHOIX <- Fin de manche <-------------+
    ///                                          ... -> Podium
    ///
    /// CHAQUE MANCHE RECHARGE LA SCENE : le monde est rebati a neuf. Ce qui traverse
    /// les manches -- les victoires, les capacites -- vit dans Match (statique).
    ///
    /// Concept Unity : Time.timeScale = 0 met le temps du jeu en pause ; les Update
    /// tournent toujours mais Time.deltaTime vaut 0. Un menu doit donc s'animer
    /// avec Time.unscaledDeltaTime, qui avance toujours.
    ///
    /// Concept Unity (IMGUI) : OnGUI est appele PLUSIEURS fois par image (une fois
    /// pour mesurer, une fois pour dessiner, une fois par clic...). On n'y fait donc
    /// jamais d'action au clavier : le clavier est lu dans Update, une fois par image.
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

        /// <summary>L'entree choisie dans l'ecran courant (fleches ou survol de la souris).</summary>
        int selected;
        Vector2 lastMouse;

        // --- le salon
        int lobbyBots = 3;
        int lobbyRounds = 5;
        int lobbyMinutes = 6;

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
            showSettings = false;
            confirmAbandon = false;
            selected = 0;
        }

        // ================================================================== boucle

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
                if (season.Over) { endedByTime = true; EndRound(Crown.Holder != null ? Crown.Holder.Index : -1); }
            }
            if (Current == State.Briefing && stateTime > BriefingLength && !leaving) Enter();
            if (Current == State.Draft) TickDraft(dt);
            if (Current == State.Playing) TickCountdown(dt);
            if (goFlash > 0f) goFlash = Mathf.Max(0f, goFlash - dt * 1.2f);
            if (Current == State.RoundOver && stateTime > 9f && !leaving) AfterRound();

            if (!leaving) Keyboard();
            DriveCamera();

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
            // qu'un menu est ouvert.
            bool blocked = Blocking || leaving || countdown > 0f;
            Cursor.lockState = blocked && countdown <= 0f ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked && !leaving && Current != State.Briefing && countdown <= 0f;

            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (Game.Player != null) Game.Player.InputLocked = blocked;
            if (cam != null) cam.InputLocked = blocked && countdown <= 0f;     // pendant le compte, on regarde autour de soi
            if (Game.Hud != null && Game.Hud.interactor != null) Game.Hud.interactor.InputLocked = blocked;

            float wantVeil = Current == State.Paused || showControls || showSettings || Current == State.Lobby || Current == State.Online ? 0.82f
                           : Current == State.RoundOver && stateTime < 1.3f ? 0.1f     // on regarde d'abord le ralenti
                           : Current == State.RoundOver || Current == State.Draft || Current == State.Ended ? 0.86f : 0f;
            veil = Mathf.MoveTowards(veil, wantVeil, dt * 3f);
        }

        void DriveCamera()
        {
            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam == null) return;
            bool outside = !Match.Launched && (Current == State.Title || Current == State.Lobby || Current == State.Online || Current == State.Draft);
            if (outside)
            {
                // Six metres, a peine au-dessus de la tete : assez pres pour que la
                // brume ne l'efface pas, assez loin pour voir la silhouette.
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

        /// <summary>
        /// LE CLAVIER, une fois par image : haut/bas choisit, gauche/droite regle,
        /// Entree valide, Echap revient. Chaque ecran dit combien il a d'entrees.
        /// </summary>
        void Keyboard()
        {
            if (FiefInput.CancelPressed)
            {
                confirmAbandon = false;
                if (showSettings) { showSettings = false; selected = 1; }
                else if (showControls) { showControls = false; selected = 0; }
                else if (Current == State.Lobby || Current == State.Online) Go(State.Title);
                else if (Current == State.Briefing) Enter();
                else if (Current == State.Playing && countdown <= 0f) Pause();
                else if (Current == State.Paused) Resume();
                return;
            }
            if (Current == State.Playing || Current == State.Briefing) return;
            if (Current == State.Title && appear < 0.8f) return;

            if (showControls)
            {
                if (FiefInput.ConfirmPressed) { showControls = false; selected = 0; Sfx.Pop(); }
                return;
            }
            if (showSettings)
            {
                int rows = Settings.Labels.Length + 1;
                if (FiefInput.UpPressed) { selected = (selected + rows - 1) % rows; Sfx.Pop(); }
                if (FiefInput.DownPressed) { selected = (selected + 1) % rows; Sfx.Pop(); }
                if (selected < Settings.Labels.Length)
                {
                    if (FiefInput.LeftPressed) { Settings.Step(selected, -1); Sfx.Pop(); }
                    if (FiefInput.RightPressed || FiefInput.ConfirmPressed) { Settings.Step(selected, 1); Sfx.Pop(); }
                }
                else if (FiefInput.ConfirmPressed) { showSettings = false; selected = 0; Sfx.Pop(); }
                return;
            }

            if (Current == State.Draft)
            {
                int cards = Match.Draft.Offer.Count;
                if (!Match.Draft.Done && cards > 0)
                {
                    if (FiefInput.LeftPressed) { selected = (selected + cards - 1) % cards; Sfx.Pop(); }
                    if (FiefInput.RightPressed) { selected = (selected + 1) % cards; Sfx.Pop(); }
                    if (FiefInput.ConfirmPressed) PickCard(selected);
                }
                else if (Match.Draft.Done && FiefInput.ConfirmPressed) FinishDraft();
                return;
            }

            int count = Entries();
            if (count <= 0) return;
            if (FiefInput.UpPressed) { selected = (selected + count - 1) % count; confirmAbandon = false; Sfx.Pop(); }
            if (FiefInput.DownPressed) { selected = (selected + 1) % count; confirmAbandon = false; Sfx.Pop(); }
            if (Current == State.Lobby)
            {
                if (FiefInput.LeftPressed) Adjust(selected, -1);
                if (FiefInput.RightPressed) Adjust(selected, 1);
            }
            if (FiefInput.ConfirmPressed) Activate(selected);
        }

        /// <summary>Combien d'entrees dans l'ecran courant.</summary>
        int Entries()
        {
            switch (Current)
            {
                case State.Title: return TitleItems.Length;
                case State.Lobby: return LobbyRows + 2;
                case State.Online: return 1;
                case State.Paused: return PauseItems.Length;
                case State.RoundOver: return stateTime > 1.8f ? 1 : 0;
                case State.Ended: return stateTime > 1.5f ? EndItems.Length : 0;
            }
            return 0;
        }

        static readonly string[] TitleItems = { "Jouer", "En ligne", "Réglages", "Commandes", "Quitter" };
        static readonly string[] PauseItems = { "Reprendre", "Réglages", "Commandes", "Abandonner le match", "Quitter le jeu" };
        static readonly string[] EndItems = { "Nouveau match", "Quitter" };

        /// <summary>Le salon : les lignes a regler (joueurs, bots, manches, duree), puis Commencer et Retour.</summary>
        const int LobbyRows = 4;

        bool showSettings;
        /// <summary>"Abandonner le match" demande une seconde pression (on ne perd pas un match d'un clic egare).</summary>
        bool confirmAbandon;

        /// <summary>Valider l'entree "i" de l'ecran courant (Entree, ou clic).</summary>
        void Activate(int i)
        {
            if (leaving) return;
            Sfx.Pop();
            switch (Current)
            {
                case State.Title:
                    if (i == 0) Go(State.Lobby);
                    else if (i == 1) Go(State.Online);
                    else if (i == 2) { showSettings = true; selected = 0; }
                    else if (i == 3) { showControls = true; selected = 0; }
                    else Quit();
                    break;
                case State.Lobby:
                    if (i < LobbyRows) Adjust(i, 1);
                    else if (i == LobbyRows) StartMatch();
                    else Go(State.Title);
                    break;
                case State.Online:
                    Go(State.Title);
                    break;
                case State.Paused:
                    if (i == 0) Resume();
                    else if (i == 1) { showSettings = true; selected = 0; }
                    else if (i == 2) { showControls = true; selected = 0; }
                    else if (i == 3)
                    {
                        if (!confirmAbandon) { confirmAbandon = true; break; }
                        confirmAbandon = false;
                        Match.Abandon();
                        Curtain(Reload);
                    }
                    else Quit();
                    break;
                case State.RoundOver:
                    AfterRound();
                    break;
                case State.Ended:
                    if (i == 0) { Match.Abandon(); openLobby = true; Curtain(Reload); }
                    else Quit();
                    break;
            }
        }

        /// <summary>Le salon : regler une ligne (joueurs, bots, manches, duree) d'un cran.</summary>
        void Adjust(int row, int step)
        {
            if (row == 0) lobbyBots = Mathf.Clamp(lobbyBots + step, 1, 3);
            else if (row == 1) Match.BotLevel = Mathf.Clamp(Match.BotLevel + step, 0, Match.BotLevels.Length - 1);
            else if (row == 2) lobbyRounds = Cycle(Match.RoundChoices, lobbyRounds, step);
            else if (row == 3) lobbyMinutes = Cycle(Match.MinuteChoices, lobbyMinutes, step);
            else return;
            Sfx.Pop();
        }

        static int Cycle(int[] choices, int current, int step)
        {
            int at = System.Array.IndexOf(choices, current);
            if (at < 0) at = 0;
            return choices[Mathf.Clamp(at + step, 0, choices.Length - 1)];
        }

        /// <summary>
        /// COMMENCER : le match est cree, et avant la premiere manche, chacun CHOISIT
        /// SA PREMIERE CAPACITE (que des actives sur la table). On ne part jamais les
        /// mains vides : c'etait la raison n°1 de s'ennuyer.
        /// </summary>
        void StartMatch()
        {
            Match.Begin(lobbyBots, lobbyRounds, lobbyMinutes);
            Stats.Reset();
            Match.Draft.Prepare();
            botPickTimer = 0.8f;
            Go(State.Draft);
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
            // Le compte a rebours : trois secondes ou tout le monde est fige sur sa
            // ligne de depart. L'horloge (et les bots, les Yeux) ne partent qu'au "PARTEZ".
            countdown = 3f;
            lastCount = 4;
            Toasts.Clear();
        }

        float countdown;
        int lastCount;
        float goFlash;

        void TickCountdown(float dt)
        {
            if (countdown <= 0f) return;
            countdown -= dt;
            int n = Mathf.CeilToInt(countdown);
            if (n != lastCount && n > 0) { lastCount = n; Sfx.Beep(1f); }
            if (countdown > 0f) return;
            countdown = 0f;
            goFlash = 1f;
            if (Game.Season != null) Game.Season.Begin();
            Sfx.Bell();
        }

        void DrawCountdown()
        {
            if (countdown > 0f)
            {
                float frac = countdown - Mathf.Floor(countdown);
                Headline(Screen.height * 0.34f, Mathf.Lerp(90f, 130f, frac), Mathf.CeilToInt(countdown).ToString(), new Color(1f, 0.86f, 0.55f, 0.5f + 0.5f * frac));
            }
            else if (goFlash > 0f) Headline(Screen.height * 0.34f, 96f, "PARTEZ !", new Color(1f, 0.86f, 0.55f, goFlash));
        }

        /// <summary>
        /// FIN DE MANCHE : "winner" a pose la Couronne au Monument (ou tenait la
        /// Couronne quand le temps s'est ecoule ; -1 : personne). C'est ici, et nulle
        /// part ailleurs, qu'une manche se termine -- en Phase 3, sur l'hote seulement.
        /// </summary>
        public void EndRound(int winner)
        {
            if (Current != State.Playing && Current != State.Paused) return;
            if (Game.Season != null) { roundTime = Game.Season.Elapsed; Game.Season.Stop(); }
            byTime = endedByTime;
            endedByTime = false;
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

        /// <summary>Apres la fin de manche : le podium, ou le choix des capacites, ou la manche suivante.</summary>
        void AfterRound()
        {
            if (Current != State.RoundOver || stateTime < 1.8f) return;
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

        /// <summary>Le choix est fini : la manche suivante (ou la premiere : le match part).</summary>
        void FinishDraft()
        {
            if (leaving) return;
            Sfx.Pop();
            Match.Launch();
            NextRound();
        }

        void PickCard(int card)
        {
            int me = Match.Local != null ? Match.Local.Index : 0;
            if (Match.Draft.Done || Match.Draft.Current != me) return;
            if (card < 0 || card >= Match.Draft.Offer.Count || Match.Local.Has(Match.Draft.Offer[card])) { Sfx.Deny(); return; }
            if (Match.Draft.TryPick(me, card))
            {
                Sfx.Discovery();
                botPickTimer = 0.9f;
                selected = 0;
            }
        }

        void TickDraft(float dt)
        {
            if (Match.Draft.Done)
            {
                // Tout le monde a choisi : on part tout seul apres un temps de lecture.
                botPickTimer -= dt;
                if (botPickTimer < -4f) FinishDraft();
                return;
            }
            int slot = Match.Draft.Current;
            if (slot < 0 || slot >= Match.Slots.Count || !Match.Slots[slot].IsBot) return;
            botPickTimer -= dt;
            if (botPickTimer > 0f) return;
            botPickTimer = 0.9f;
            int card = Match.Draft.BotChoice(slot);
            if (card < 0) card = 0;
            if (Match.Draft.TryPick(slot, card))
            {
                Sfx.Pop();
                if (Match.Draft.Done) botPickTimer = 0f;
            }
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, Match.Draft.Offer.Count - 1));
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

        // ================================================================== dessin

        static void EnsureTextures()
        {
            if (vignette != null) return;
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
                    // Plus sombre a gauche : c'est la que le texte se pose.
                    float left = Mathf.Clamp01(1f - (x + 0.5f) / Size * 1.6f) * 0.55f;
                    float a = Mathf.Max(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.3f) / 0.7f)) * 0.8f, left);
                    vignette.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            vignette.Apply();
        }

        void OnGUI()
        {
            UiStyle.Ensure();
            EnsureTextures();
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);

            // La souris qui bouge prend la main sur le clavier (et inversement).
            Vector2 mouse = Event.current.mousePosition;
            bool mouseMoved = (mouse - lastMouse).sqrMagnitude > 1f;
            if (Event.current.type == EventType.Repaint) lastMouse = mouse;
            hoverFollows = mouseMoved;

            if (Current == State.Title || Current == State.Lobby || Current == State.Online) GUI.DrawTexture(screen, vignette, ScaleMode.StretchToFill);
            if (veil > 0.001f) UiStyle.Fill(screen, new Color(0.015f, 0.014f, 0.012f, 0.84f * veil));

            if (showSettings) DrawSettings();
            else if (showControls) DrawControls();
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
                    case State.Playing: DrawCountdown(); break;
                }
            }

            // Le rideau passe par-dessus tout, y compris le texte.
            if (curtain > 0.001f) UiStyle.Fill(screen, new Color(0f, 0f, 0f, curtain));
        }

        bool hoverFollows;

        // ------------------------------------------------------------------ outils de dessin

        /// <summary>
        /// UNE ENTREE : un mot. Choisie, elle passe en or, se decale un peu et un trait
        /// fin se dessine dessous. Rien d'autre. Vrai si on a clique dessus.
        /// </summary>
        bool Entry(Rect r, string text, int index, bool primary, float alpha)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover && hoverFollows) selected = index;
            bool on = selected == index;
            GUIStyle style = primary ? Style(UiStyle.Title, 34, TextAnchor.MiddleLeft) : Style(UiStyle.Head, 22, TextAnchor.MiddleLeft);
            float indent = on ? UiStyle.S(14) : 0f;
            Color c = on ? new Color(1f, 0.84f, 0.5f, alpha) : new Color(0.8f, 0.76f, 0.68f, alpha * 0.8f);
            Shadow(new Rect(r.x + indent, r.y, r.width - indent, r.height), text, style, c);
            if (on)
            {
                float w = style.CalcSize(new GUIContent(text)).x;
                UiStyle.Fill(new Rect(r.x + indent, r.yMax - UiStyle.S(6), w, 1f), new Color(1f, 0.84f, 0.5f, alpha * 0.7f));
            }
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Un texte avec son ombre, pour qu'il se lise sur la foret.</summary>
        static void Shadow(Rect r, string text, GUIStyle style, Color c)
        {
            UiStyle.Tinted(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style, new Color(0f, 0f, 0f, c.a * 0.7f));
            UiStyle.Tinted(r, text, style, c);
        }

        static void Centered(float y, float h, string text, GUIStyle baseStyle, Color c)
        {
            Shadow(new Rect(0f, y, Screen.width, h), text, Style(baseStyle, 0, TextAnchor.MiddleCenter), c);
        }

        /// <summary>Un grand titre, centre, de la taille voulue.</summary>
        static void Headline(float y, float size, string text, Color c)
        {
            GUIStyle s = Style(UiStyle.Big, size, TextAnchor.MiddleCenter);
            Shadow(new Rect(0f, y, Screen.width, UiStyle.S(size * 1.3f)), text, s, c);
        }

        /// <summary>
        /// Une copie d'un style, a la taille et l'alignement voulus. On ne modifie
        /// JAMAIS le style partage (UiStyle.Title...) : c'est ce qui faisait "sauter"
        /// la taille des textes d'un ecran a l'autre.
        /// </summary>
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        static GUIStyle Style(GUIStyle from, float size, TextAnchor align)
        {
            int px = size > 0f ? UiStyle.S(size) : from.fontSize;
            string key = from.name + "|" + px + "|" + (int)align + "|" + (from.font != null ? from.font.name : "");
            GUIStyle s;
            if (!styles.TryGetValue(key, out s))
            {
                s = new GUIStyle(from);
                s.fontSize = px;
                s.alignment = align;
                s.wordWrap = false;
                styles[key] = s;
            }
            return s;
        }

        static GUIStyle wrapped;
        static GUIStyle Wrapped()
        {
            if (wrapped == null || wrapped.fontSize != UiStyle.Label.fontSize)
            {
                wrapped = new GUIStyle(UiStyle.Label);
                wrapped.wordWrap = true;
                wrapped.alignment = TextAnchor.UpperLeft;
            }
            return wrapped;
        }

        /// <summary>En bas de l'ecran, ce que font les touches, en petit.</summary>
        static void Footer(string text)
        {
            Shadow(new Rect(0f, Screen.height - UiStyle.S(44), Screen.width, UiStyle.S(20)), text, Style(UiStyle.Small, 0, TextAnchor.MiddleCenter), UiStyle.InkFaint);
        }

        float Left { get { return Mathf.Max(UiStyle.S(40), Screen.width * 0.09f); } }

        // ------------------------------------------------------------------ le titre

        void DrawTitle()
        {
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.15f) / 0.85f));
            float late = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.45f) / 0.55f));
            float x = Left;
            float w = Mathf.Min(UiStyle.S(640), Screen.width - x * 2f);
            float y = Screen.height * 0.5f - UiStyle.S(200) + (1f - ease) * UiStyle.S(18);

            Shadow(new Rect(x, y, w, UiStyle.S(130)), UiStyle.Spaced("FIEF"), Style(UiStyle.Big, 112, TextAnchor.MiddleLeft), new Color(0.93f, 0.78f, 0.45f, ease));
            y += UiStyle.S(122);
            Shadow(new Rect(x + UiStyle.S(4), y, w, UiStyle.S(30)), UiStyle.Spaced("LA COURONNE"), Style(UiStyle.Head, 20, TextAnchor.MiddleLeft), new Color(0.95f, 0.85f, 0.6f, ease * 0.9f));
            y += UiStyle.S(70);

            for (int i = 0; i < TitleItems.Length; i++)
            {
                bool primary = i == 0;
                float h = UiStyle.S(primary ? 50 : 38);
                if (Entry(new Rect(x, y, UiStyle.S(420), h), TitleItems[i], i, primary, late) && late > 0.9f) Activate(i);
                y += h + UiStyle.S(4);
            }

            Footer("↑ ↓  choisir     Entrée  valider");
            // La version, en bas a droite : c'est elle qui dit quel code tourne.
            UiStyle.Tinted(new Rect(0f, Screen.height - UiStyle.S(30), Screen.width - UiStyle.S(24), UiStyle.S(20)), Game.Version,
                           Style(UiStyle.Tiny, 0, TextAnchor.MiddleRight), new Color(UiStyle.InkFaint.r, UiStyle.InkFaint.g, UiStyle.InkFaint.b, late));
        }

        // ------------------------------------------------------------------ le salon

        /// <summary>
        /// LE SALON, en trois lignes a regler et deux entrees. Pas de cartes, pas de
        /// pastilles : "Joueurs  ‹ 4 ›", les fleches gauche/droite changent la valeur.
        /// </summary>
        void DrawLobby()
        {
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(230);
            Shadow(new Rect(x, y, UiStyle.S(600), UiStyle.S(50)), UiStyle.Spaced("NOUVEAU MATCH"), Style(UiStyle.Title, 34, TextAnchor.MiddleLeft), Palette.Gold);
            y += UiStyle.S(80);

            string[] labels = { "Joueurs", "Bots", "Manches", "Durée max" };
            string[] values = { (lobbyBots + 1).ToString(), Match.BotLevels[Match.BotLevel], lobbyRounds.ToString(), lobbyMinutes + " min" };
            for (int i = 0; i < LobbyRows; i++)
            {
                int row = i;
                ValueRow(x, y, labels[i], values[i], i, step => Adjust(row, step));
                y += UiStyle.S(48);

                // Sous "Joueurs" : qui joue, a sa couleur.
                if (i == 0)
                {
                    float wx = x + UiStyle.S(16);
                    for (int k = 0; k <= lobbyBots; k++)
                    {
                        string name = k == 0 ? "Toi" : k == 1 ? "Mahaut" : k == 2 ? "Oswin" : "Guerin";
                        GUIStyle ns = Style(UiStyle.Small, 0, TextAnchor.MiddleLeft);
                        float nw = ns.CalcSize(new GUIContent(name)).x;
                        Shadow(new Rect(wx, y - UiStyle.S(8), nw + 4f, UiStyle.S(20)), name, ns, Match.ColourOf(k));
                        wx += nw + UiStyle.S(18);
                    }
                    y += UiStyle.S(20);
                }
                // Sous "Bots" : ce que ca change, en une ligne.
                if (i == 1)
                {
                    string[] what = { "plus lents, sans courants ni embuscade", "aussi rapides que toi", "rapides, vifs, sans pitié" };
                    Shadow(new Rect(x + UiStyle.S(16), y - UiStyle.S(8), UiStyle.S(500), UiStyle.S(20)), what[Match.BotLevel], Style(UiStyle.Small, 0, TextAnchor.MiddleLeft), UiStyle.InkFaint);
                    y += UiStyle.S(20);
                }
            }

            // Une estimation, en un mot : une manche dure rarement tout son temps.
            int estimate = Mathf.RoundToInt(lobbyRounds * lobbyMinutes * 0.7f);
            Shadow(new Rect(x + UiStyle.S(16), y, UiStyle.S(500), UiStyle.S(20)), "un match d'environ " + estimate + " min", Style(UiStyle.Small, 0, TextAnchor.MiddleLeft), UiStyle.InkFaint);
            y += UiStyle.S(50);

            if (Entry(new Rect(x, y, UiStyle.S(420), UiStyle.S(50)), "Commencer", LobbyRows, true, 1f)) Activate(LobbyRows);
            y += UiStyle.S(56);
            if (Entry(new Rect(x, y, UiStyle.S(420), UiStyle.S(38)), "Retour", LobbyRows + 1, false, 1f)) Activate(LobbyRows + 1);

            Footer("↑ ↓  choisir     ← →  régler     Entrée  valider     Échap  retour");
        }

        /// <summary>Une ligne a regler : "Joueurs   ‹ 4 ›". Les fleches se cliquent ; au clavier, gauche/droite.</summary>
        void ValueRow(float x, float y, string label, string value, int index, System.Action<int> adjust)
        {
            Rect row = new Rect(x, y, UiStyle.S(520), UiStyle.S(44));
            Entry(row, label, index, false, 1f);
            bool on = selected == index;
            GUIStyle vs = Style(UiStyle.Head, 24, TextAnchor.MiddleCenter);
            Color vc = on ? new Color(1f, 0.84f, 0.5f) : UiStyle.Ink;
            float vx = x + UiStyle.S(270);
            Rect minus = new Rect(vx, y, UiStyle.S(40), row.height);
            Rect val = new Rect(vx + UiStyle.S(40), y, UiStyle.S(130), row.height);
            Rect plus = new Rect(vx + UiStyle.S(170), y, UiStyle.S(40), row.height);
            Shadow(minus, "‹", vs, new Color(vc.r, vc.g, vc.b, on ? 0.9f : 0.35f));
            Shadow(val, value, vs, vc);
            Shadow(plus, "›", vs, new Color(vc.r, vc.g, vc.b, on ? 0.9f : 0.35f));
            if (GUI.Button(minus, GUIContent.none, GUIStyle.none)) { selected = index; adjust.Invoke(-1); }
            if (GUI.Button(plus, GUIContent.none, GUIStyle.none)) { selected = index; adjust.Invoke(1); }
        }

        // ------------------------------------------------------------------ les reglages

        /// <summary>LES REGLAGES : sensibilite, volume, champ de vision, plein ecran. Garde d'une partie a l'autre.</summary>
        void DrawSettings()
        {
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(170);
            Shadow(new Rect(x, y, UiStyle.S(600), UiStyle.S(50)), UiStyle.Spaced("RÉGLAGES"), Style(UiStyle.Title, 34, TextAnchor.MiddleLeft), Palette.Gold);
            y += UiStyle.S(80);
            for (int i = 0; i < Settings.Labels.Length; i++)
            {
                int row = i;
                ValueRow(x, y, Settings.Labels[i], Settings.Value(i), i, step => { Settings.Step(row, step); Sfx.Pop(); });
                y += UiStyle.S(48);
            }
            y += UiStyle.S(24);
            if (Entry(new Rect(x, y, UiStyle.S(300), UiStyle.S(40)), "Retour", Settings.Labels.Length, false, 1f)) { showSettings = false; selected = 0; }
            Footer("↑ ↓  choisir     ← →  régler     Échap  retour");
        }

        // ------------------------------------------------------------------ en ligne

        /// <summary>
        /// EN LIGNE : prepare, pas encore branche. Le jeu est deja coupe en places
        /// (Match.Slots) et tout passe par des methodes que l'hote appellera -- mais
        /// le transport (Steam) vient en Phase 3. On le dit franchement.
        /// </summary>
        void DrawOnline()
        {
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(140);
            Shadow(new Rect(x, y, UiStyle.S(600), UiStyle.S(50)), UiStyle.Spaced("EN LIGNE"), Style(UiStyle.Title, 34, TextAnchor.MiddleLeft), Palette.Gold);
            y += UiStyle.S(70);
            GUIStyle line = Style(UiStyle.Head, 20, TextAnchor.MiddleLeft);
            Shadow(new Rect(x, y, UiStyle.S(700), UiStyle.S(30)), "Pas encore. Quatre joueurs par Steam, bientôt.", line, UiStyle.Ink);
            y += UiStyle.S(34);
            Shadow(new Rect(x, y, UiStyle.S(700), UiStyle.S(30)), "Les bots jouent déjà avec les mêmes règles que toi.", line, UiStyle.InkDim);
            y += UiStyle.S(70);
            if (Entry(new Rect(x, y, UiStyle.S(420), UiStyle.S(40)), "Retour", 0, false, 1f)) Activate(0);
            Footer("Entrée ou Échap  retour");
        }

        // ------------------------------------------------------------------ l'intro de manche

        float BriefingLength { get { return Match.Played == 0 && !Match.IsTieBreak ? 7f : 3.8f; } }

        /// <summary>
        /// L'INTRO DE MANCHE, toute seule, quelques secondes : "MANCHE 2", et tes
        /// capacites avec leurs touches. La premiere fois, la regle en deux lignes.
        /// Echap passe.
        /// </summary>
        void DrawBriefing()
        {
            UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.01f, 0.02f, 0.72f));
            float t = stateTime;
            float a = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((BriefingLength - t) / 0.5f);
            float y = Screen.height * 0.3f;

            string head = Match.IsTieBreak ? UiStyle.Spaced("DÉPARTAGE") : UiStyle.Spaced("MANCHE " + Match.RoundNumber);
            Headline(y, 64, head, new Color(0.93f, 0.78f, 0.45f, a));
            y += UiStyle.S(100);

            if (Match.IsTieBreak)
            {
                string who = "";
                for (int i = 0; i < Match.TieBreakers.Count; i++) who += (i > 0 ? "  ·  " : "") + Match.Slots[Match.TieBreakers[i]].Name;
                Centered(y, UiStyle.S(30), "Seuls comptent : " + who, UiStyle.Head, new Color(0.95f, 0.9f, 0.8f, a));
                y += UiStyle.S(60);
            }
            else if (Match.Played == 0)
            {
                float b = Mathf.Clamp01((t - 0.8f) / 0.5f) * a;
                Centered(y, UiStyle.S(30), "La Couronne est au sommet de la tour, au centre du château.", UiStyle.Head, new Color(0.95f, 0.9f, 0.8f, b));
                float c = Mathf.Clamp01((t - 1.8f) / 0.5f) * a;
                Centered(y + UiStyle.S(38), UiStyle.S(30), "Porte-la au Monument : la colonne bleue.", UiStyle.Head, new Color(0.6f, 0.8f, 1f, c));
                float d = Mathf.Clamp01((t - 2.8f) / 0.5f) * a;
                Centered(y + UiStyle.S(76), UiStyle.S(30), "Clic gauche pousse. Qui porte la Couronne la lâche.", UiStyle.Head, new Color(0.95f, 0.7f, 0.6f, d));
                y += UiStyle.S(130);
            }
            else
            {
                ScoreLine(y, a);
                y += UiStyle.S(60);
            }

            // Tes capacites, avec leurs touches : on ne les cherche jamais.
            PlayerSlot me = Match.Local;
            if (me != null)
            {
                List<Ability> actives = me.Actives;
                float e = Mathf.Clamp01((t - (Match.Played == 0 ? 3.4f : 0.6f)) / 0.5f) * a;
                for (int i = 0; i < actives.Count; i++)
                {
                    Centered(y, UiStyle.S(26), AbilityInfo.Keys[Mathf.Min(i, 2)].ToUpperInvariant() + "   " + AbilityInfo.Name(actives[i]) + " — " + AbilityInfo.Line(actives[i]),
                             UiStyle.Label, new Color(0.95f, 0.88f, 0.7f, e));
                    y += UiStyle.S(28);
                }
            }

            Footer("Échap  passer");
        }

        /// <summary>Une ligne : chaque joueur, a sa couleur, et ses manches gagnees en chiffre.</summary>
        static void ScoreLine(float y, float a)
        {
            GUIStyle s = Style(UiStyle.Head, 22, TextAnchor.MiddleCenter);
            float cell = UiStyle.S(180);
            float x = (Screen.width - cell * Match.Slots.Count) * 0.5f;
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot p = Match.Slots[i];
                Color c = p.IsLocal ? Palette.Gold : p.Colour;
                Shadow(new Rect(x + i * cell, y, cell, UiStyle.S(30)), p.Name + "   " + p.Wins, s, new Color(c.r, c.g, c.b, a));
            }
        }

        // ------------------------------------------------------------------ la pause

        void DrawPause()
        {
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(170);
            Shadow(new Rect(x, y, UiStyle.S(600), UiStyle.S(50)), UiStyle.Spaced("PAUSE"), Style(UiStyle.Title, 34, TextAnchor.MiddleLeft), Palette.Gold);
            y += UiStyle.S(52);
            Season s = Game.Season;
            if (s != null)
            {
                string round = Match.IsTieBreak ? "Départage" : "Manche " + Match.RoundNumber + " sur " + Match.Rounds;
                Shadow(new Rect(x, y, UiStyle.S(600), UiStyle.S(22)), round + "  ·  " + Hud.Clock(s.Remaining) + " restantes", Style(UiStyle.Small, 0, TextAnchor.MiddleLeft), UiStyle.InkDim);
            }
            y += UiStyle.S(50);
            for (int i = 0; i < PauseItems.Length; i++)
            {
                bool primary = i == 0;
                float h = UiStyle.S(primary ? 48 : 38);
                string label = i == 3 && confirmAbandon ? "Abandonner ? Encore une fois pour confirmer" : PauseItems[i];
                if (Entry(new Rect(x, y, UiStyle.S(620), h), label, i, primary, 1f)) Activate(i);
                y += h + UiStyle.S(4);
            }
            Footer("↑ ↓  choisir     Entrée  valider     Échap  reprendre");
        }

        // ------------------------------------------------------------------ fin de manche

        /// <summary>
        /// FIN DE MANCHE : qui l'a gagnee (son nom, a sa couleur, en grand), et le
        /// score en une ligne. Puis le choix des capacites -- tout seul, ou Entree.
        /// </summary>
        bool endedByTime, byTime;
        float roundTime;

        void DrawRoundOver()
        {
            // Le ralenti d'abord (le monde, la Couronne sur l'autel), puis le verdict.
            float a = Mathf.Clamp01((stateTime - 1.2f) / 0.6f);
            float y = Screen.height * 0.3f;
            PlayerSlot w = roundWinner >= 0 && roundWinner < Match.Slots.Count ? Match.Slots[roundWinner] : null;
            if (w != null)
            {
                Color c = w.IsLocal ? Palette.Gold : w.Colour;
                Headline(y, 60, w.IsLocal ? "TU REMPORTES LA MANCHE" : w.Name.ToUpperInvariant() + " REMPORTE LA MANCHE", new Color(c.r, c.g, c.b, a));
            }
            else Headline(y, 52, "PERSONNE N'A RAMENÉ LA COURONNE", new Color(0.8f, 0.76f, 0.7f, a));
            y += UiStyle.S(84);
            // Comment : on doit comprendre pourquoi la manche s'arrete.
            string how = w == null ? "Le temps s'est écoulé, et personne ne tenait la Couronne."
                       : byTime ? (w.IsLocal ? "Tu tenais la Couronne quand le temps s'est écoulé." : w.Name + " tenait la Couronne quand le temps s'est écoulé.")
                       : (w.IsLocal ? "Tu as porté la Couronne au Monument" : w.Name + " a porté la Couronne au Monument") + " en " + Hud.Clock(roundTime) + ".";
            Centered(y, UiStyle.S(26), how, UiStyle.Label, new Color(0.9f, 0.86f, 0.78f, a));
            y += UiStyle.S(50);
            ScoreLine(y, a);
            y += UiStyle.S(70);

            if (stateTime > 1.8f)
            {
                string label = Match.Over ? "Le podium" : "Choisir une capacité";
                float bw = UiStyle.S(420);
                if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, UiStyle.S(48)), label, 0, true, a)) Activate(0);
                Footer("Entrée  continuer");
            }
        }

        // ------------------------------------------------------------------ le choix

        /// <summary>
        /// LE CHOIX DES CAPACITES : quelques cartes, que du texte -- le nom, "ACTIVE,
        /// touche R" ou "PASSIVE", une phrase qui dit exactement ce qu'elle fait, et
        /// si elle remplace une de tes actives (trois au plus). Chacun son tour, le
        /// vainqueur de la manche en dernier. Gauche/droite, Entree -- ou la souris.
        /// </summary>
        void DrawDraft()
        {
            float y = Screen.height * 0.14f;
            string title = Match.Played == 0 ? "TA PREMIÈRE CAPACITÉ" : "UNE CAPACITÉ DE PLUS";
            Headline(y, 40, UiStyle.Spaced(title), Palette.Gold);
            y += UiStyle.S(64);

            // L'ordre de passage, en une ligne : les noms, celui dont c'est le tour en clair.
            List<int> order = Match.Draft.Order;
            GUIStyle os = Style(UiStyle.Label, 0, TextAnchor.MiddleLeft);
            float total = 0f;
            for (int i = 0; i < order.Count; i++) total += os.CalcSize(new GUIContent(Match.Slots[order[i]].Name)).x + UiStyle.S(36);
            float ox = (Screen.width - total) * 0.5f;
            for (int i = 0; i < order.Count; i++)
            {
                PlayerSlot s = Match.Slots[order[i]];
                bool now = !Match.Draft.Done && Match.Draft.Current == order[i];
                bool done = i < Match.Draft.Turn;
                string n = s.Name;
                float nw = os.CalcSize(new GUIContent(n)).x;
                Color c = s.IsLocal ? Palette.Gold : s.Colour;
                Shadow(new Rect(ox, y, nw + 4f, UiStyle.S(24)), n, os, new Color(c.r, c.g, c.b, now ? 1f : done ? 0.35f : 0.6f));
                if (now) UiStyle.Fill(new Rect(ox, y + UiStyle.S(24), nw, 2f), c);
                ox += nw + UiStyle.S(36);
            }
            y += UiStyle.S(56);

            // Les cartes.
            List<Ability> offer = Match.Draft.Offer;
            int me = Match.Local != null ? Match.Local.Index : 0;
            bool myTurn = !Match.Draft.Done && Match.Draft.Current == me;
            int n2 = Mathf.Max(1, offer.Count);
            float gap = UiStyle.S(20);
            float cw = Mathf.Min(UiStyle.S(250), (Screen.width - UiStyle.S(80) - gap * (n2 - 1)) / n2);
            float ch = UiStyle.S(230);
            float cx = (Screen.width - (cw * n2 + gap * (n2 - 1))) * 0.5f;
            for (int i = 0; i < offer.Count; i++)
            {
                Ability p = offer[i];
                Rect card = new Rect(cx + i * (cw + gap), y, cw, ch);
                bool owned = Match.Local != null && Match.Local.Has(p);
                bool hover = card.Contains(Event.current.mousePosition);
                if (hover && hoverFollows && myTurn) selected = i;
                bool on = myTurn && selected == i && !owned;
                Card(card, p, owned, on, me);
                if (myTurn && GUI.Button(card, GUIContent.none, GUIStyle.none)) { selected = i; PickCard(i); }
            }
            y += ch + UiStyle.S(30);

            if (myTurn) Centered(y, UiStyle.S(28), "À toi de choisir", UiStyle.Head, Palette.Gold);
            else if (!Match.Draft.Done)
            {
                PlayerSlot who = Match.Slots[Match.Draft.Current];
                Centered(y, UiStyle.S(28), who.Name + " choisit…", UiStyle.Head, who.Colour);
            }
            else
            {
                string label = Match.IsTieBreak ? "Le départage" : "Manche " + Match.RoundNumber;
                float bw = UiStyle.S(360);
                if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, UiStyle.S(48)), label, 0, true, 1f)) FinishDraft();
            }

            // Ce que les autres viennent de prendre : on sait ce qui nous attend.
            string taken = "";
            for (int i = 0; i < Match.Draft.Picked.Count; i++)
            {
                PlayerSlot who = Match.Slots[Match.Draft.PickedBy[i]];
                if (who.IsLocal) continue;
                taken += (taken.Length > 0 ? "   ·   " : "") + who.Name + " a pris " + AbilityInfo.Name(Match.Draft.Picked[i]);
            }
            if (taken.Length > 0) Centered(Screen.height - UiStyle.S(122), UiStyle.S(24), taken, UiStyle.Label, new Color(0.86f, 0.8f, 0.7f, 0.85f));

            // Ce que tu as deja, en bas : on sait ce qu'on echange.
            if (Match.Local != null && Match.Local.Abilities.Count > 0)
            {
                string mine = "";
                List<Ability> actives = Match.Local.Actives;
                for (int i = 0; i < actives.Count; i++) mine += (mine.Length > 0 ? "   ·   " : "") + AbilityInfo.Keys[Mathf.Min(i, 2)] + " " + AbilityInfo.Name(actives[i]);
                for (int i = 0; i < Match.Local.Abilities.Count; i++)
                    if (!AbilityInfo.IsActive(Match.Local.Abilities[i])) mine += (mine.Length > 0 ? "   ·   " : "") + AbilityInfo.Name(Match.Local.Abilities[i]);
                Centered(Screen.height - UiStyle.S(90), UiStyle.S(24), "Tu as : " + mine, UiStyle.Label, UiStyle.InkDim);
            }
            Footer(Match.Draft.Done ? "Entrée  jouer" : "← →  choisir     Entrée  prendre");
        }

        /// <summary>Une carte : que des mots, sur un fond a peine plus sombre.</summary>
        void Card(Rect card, Ability p, bool owned, bool on, int me)
        {
            Color tint = AbilityInfo.Tint(p);
            UiStyle.Fill(card, new Color(0f, 0f, 0f, on ? 0.5f : 0.32f));
            UiStyle.Fill(new Rect(card.x, card.y, card.width, UiStyle.S(on ? 4 : 2)), new Color(tint.r, tint.g, tint.b, owned ? 0.3f : 0.9f));
            float pad = UiStyle.S(16);
            float x = card.x + pad, w = card.width - pad * 2f;
            float y = card.y + UiStyle.S(18);
            float fade = owned ? 0.35f : 1f;

            bool active = AbilityInfo.IsActive(p);
            string kind;
            if (owned) kind = "TU L'AS DÉJÀ";
            else if (active)
            {
                int count = Match.Local != null ? Match.Local.Actives.Count : 0;
                kind = "ACTIVE · touche " + AbilityInfo.Keys[Mathf.Min(count, AbilityInfo.MaxActives - 1)].ToUpperInvariant();
            }
            else kind = "PASSIVE · toujours là";
            Shadow(new Rect(x, y, w, UiStyle.S(18)), kind, Style(UiStyle.Tiny, 0, TextAnchor.MiddleLeft), new Color(tint.r, tint.g, tint.b, fade));
            y += UiStyle.S(24);
            Shadow(new Rect(x, y, w, UiStyle.S(36)), AbilityInfo.Name(p), Style(UiStyle.Title, 26, TextAnchor.MiddleLeft),
                   on ? new Color(1f, 0.88f, 0.6f) : new Color(0.95f, 0.9f, 0.8f, fade));
            y += UiStyle.S(46);
            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(100)), AbilityInfo.Line(p), Wrapped(), new Color(0.88f, 0.84f, 0.76f, fade * 0.95f));

            if (!owned && Match.Local != null)
            {
                int lost = Match.Draft.WouldReplace(me, p);
                if (lost >= 0)
                    Shadow(new Rect(x, card.yMax - UiStyle.S(32), w, UiStyle.S(20)), "remplace " + AbilityInfo.Name((Ability)lost),
                           Style(UiStyle.Small, 0, TextAnchor.MiddleLeft), new Color(1f, 0.6f, 0.45f, 0.9f));
            }
        }

        // ------------------------------------------------------------------ fin du match

        void DrawEnd()
        {
            float a = Mathf.Clamp01(stateTime / 0.8f);
            float y = Screen.height * 0.16f;
            PlayerSlot champion = Match.Champion;
            if (champion != null)
            {
                Color c = champion.IsLocal ? Palette.Gold : champion.Colour;
                Headline(y, 64, champion.IsLocal ? "TU GAGNES LE MATCH" : champion.Name.ToUpperInvariant() + " GAGNE LE MATCH", new Color(c.r, c.g, c.b, a));
            }
            else Headline(y, 56, "PAS DE VAINQUEUR", new Color(0.85f, 0.8f, 0.72f, a));
            y += UiStyle.S(110);

            // Le classement, en lignes.
            List<PlayerSlot> order = new List<PlayerSlot>(Match.Slots);
            order.Sort((p, q) => q.Wins.CompareTo(p.Wins));
            GUIStyle row = Style(UiStyle.Head, 22, TextAnchor.MiddleLeft);
            GUIStyle rowRight = Style(UiStyle.Head, 22, TextAnchor.MiddleRight);
            float w = UiStyle.S(460);
            float x = (Screen.width - w) * 0.5f;
            for (int i = 0; i < order.Count; i++)
            {
                PlayerSlot s = order[i];
                Color c = s.IsLocal ? Palette.Gold : s.Colour;
                Shadow(new Rect(x, y, w, UiStyle.S(32)), (i + 1) + ".   " + s.Name, row, new Color(c.r, c.g, c.b, a));
                Shadow(new Rect(x, y, w, UiStyle.S(32)), s.Wins + (s.Wins > 1 ? " manches" : " manche"), rowRight, new Color(0.9f, 0.86f, 0.78f, a));
                y += UiStyle.S(36);
            }
            y += UiStyle.S(24);

            // Ce que TU as fait, en une phrase par ligne.
            string[] lines =
            {
                Stats.Delivered + " Couronne" + (Stats.Delivered > 1 ? "s" : "") + " posée" + (Stats.Delivered > 1 ? "s" : "") + " au Monument",
                Stats.CrownsTaken + " fois la Couronne en main,  " + Stats.CrownsStolen + " arrachée" + (Stats.CrownsStolen > 1 ? "s" : "") + " à un autre",
                Stats.Shoves + " poussées,  " + Stats.Casts + " capacités lancées,  " + Stats.Shrines + " don" + (Stats.Shrines > 1 ? "s" : "") + " pris"
            };
            for (int i = 0; i < lines.Length; i++)
            {
                Centered(y, UiStyle.S(24), lines[i], UiStyle.Label, new Color(0.8f, 0.76f, 0.68f, a));
                y += UiStyle.S(26);
            }
            y += UiStyle.S(30);

            if (stateTime > 1.5f)
            {
                float bw = UiStyle.S(320);
                for (int i = 0; i < EndItems.Length; i++)
                {
                    bool primary = i == 0;
                    float h = UiStyle.S(primary ? 48 : 38);
                    if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, h), EndItems[i], i, primary, a)) Activate(i);
                    y += h + UiStyle.S(4);
                }
                Footer("↑ ↓  choisir     Entrée  valider");
            }
        }

        // ------------------------------------------------------------------ les commandes

        public static readonly string[,] Controls =
        {
            { "ZQSD / WASD", "Se déplacer" },
            { "Souris", "Regarder" },
            { "Maj", "Courir" },
            { "Espace", "Sauter (maintenu en l'air : planer, si tu as le Planeur)" },
            { "Clic gauche", "Pousser — qui porte la Couronne la lâche" },
            { "Clic droit", "Ta première capacité" },
            { "R", "Ta deuxième capacité" },
            { "C", "Ta troisième capacité" },
            { "V", "Le don d'un sanctuaire (pour la manche)" },
            { "E (maintenu)", "Prendre la Couronne, un don, poser au Monument" },
            { "F", "Grimper à un arbre" },
            { "Tab", "Le score et les capacités de chacun" },
            { "Échap", "Pause" }
        };

        void DrawControls()
        {
            float x = Left;
            int count = Controls.GetLength(0);
            float y = Screen.height * 0.5f - UiStyle.S(40 + count * 15);
            Shadow(new Rect(x, y, UiStyle.S(600), UiStyle.S(50)), UiStyle.Spaced("COMMANDES"), Style(UiStyle.Title, 34, TextAnchor.MiddleLeft), Palette.Gold);
            y += UiStyle.S(66);
            GUIStyle key = Style(UiStyle.Label, 0, TextAnchor.MiddleLeft);
            for (int i = 0; i < count; i++)
            {
                Shadow(new Rect(x, y, UiStyle.S(180), UiStyle.S(26)), Controls[i, 0], key, Palette.Gold);
                Shadow(new Rect(x + UiStyle.S(190), y, UiStyle.S(700), UiStyle.S(26)), Controls[i, 1], key, UiStyle.Ink);
                y += UiStyle.S(28);
            }
            y += UiStyle.S(24);
            if (Entry(new Rect(x, y, UiStyle.S(300), UiStyle.S(40)), "Retour", 0, false, 1f)) { showControls = false; selected = 0; }
            Footer("Entrée ou Échap  retour");
        }
    }
}
