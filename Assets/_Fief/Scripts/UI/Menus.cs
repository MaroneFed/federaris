using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// TOUS LES ECRANS HORS DU JEU (refaits le 27/09 -- Martin : "les trucs en dehors
    /// du jeu, style menu, c'est tout moche et bugge").
    ///
    /// Le parti pris (30/09 et 01/10 -- "je deteste le texte, je veux des icones, comme
    /// Fall Guys") : DES ICONES ET DES PASTILLES. De gros boutons ronds (jaunes quand on
    /// les vise), des pastilles a la couleur de chaque joueur, des chiffres cernes. Seuls
    /// restent ecrits les pseudos, les mots des boutons, les noms des capacites.
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
        int lobbyBots = 5;
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
            Icons.WarmNext();
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
                // Le temps est ecoule : PERSONNE ne gagne (01/10 -- Martin : "la victoire, il ne
                // faut pas la donner s'il a la couronne a la fin"). On gagne au Monument, point.
                if (season.Over) EndRound(-1);
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
            bool establishing = !Match.Launched && (Current == State.Title || Current == State.Lobby || Current == State.Online);
            bool outside = !Match.Launched && Current == State.Draft;
            if (!establishing && cam.wide) LeaveEstablishingShot(cam);
            if (establishing)
            {
                // LE PLAN D'ENSEMBLE (29/09 -- l'ecran-titre collait au chateau, "tout est
                // bugge") : tres loin, un peu au-dessus, on tourne lentement autour de l'ile --
                // la tour, la citadelle, les plateformes, les ilots et la mer de nuages.
                if (titleAnchor == null)
                {
                    titleAnchor = new GameObject("Pivot du plan d'ensemble").transform;
                    titleAnchor.position = new Vector3(0f, 48f, 0f);
                }
                if (cam.target != titleAnchor) { cam.target = titleAnchor; cam.pitch = 12f; }
                cam.wide = true;
                cam.autoOrbitSpeed = 2.4f;
                cam.SetCinematic(165f, 12f);
            }
            else if (outside)
            {
                // Six metres, a peine au-dessus de la tete : assez pres pour que la
                // brume ne l'efface pas, assez loin pour voir la silhouette.
                cam.autoOrbitSpeed = 3.2f;
                cam.SetCinematic(6.2f, 6f);
            }
            else if ((Current == State.RoundOver || Current == State.Draft || Current == State.Ended) && WinnerBody() != null)
            {
                // LA VICTOIRE (29/09 -- Martin : "quand tu gagnes le round, on te voit TOI,
                // avec ton pseudo") : la camera quitte tes yeux et tourne autour du gagnant,
                // en contre-plongee, pendant qu'il flambe d'aura.
                Transform w = WinnerBody();
                if (cam.target != w) cam.target = w;
                cam.autoOrbitSpeed = 16f;
                cam.SetCinematic(5.2f, 4f);
            }
            else if ((Current == State.RoundOver || Current == State.Draft || Current == State.Ended) && Monument.Focus != null)
            {
                // La manche est finie : la camera quitte tes yeux et tourne lentement
                // autour du Monument -- on voit la Couronne posee sur l'autel.
                if (cam.target != Monument.Focus.transform) cam.target = Monument.Focus.transform;
                cam.autoOrbitSpeed = 9f;
                cam.SetCinematic(10f, 16f);
            }
            else cam.autoOrbitSpeed = 0f;
        }

        Transform titleAnchor;

        /// <summary>Fin du plan d'ensemble : la camera revient sur toi.</summary>
        void LeaveEstablishingShot(OrbitCamera cam)
        {
            cam.wide = false;
            if (Game.PlayerTransform != null) cam.target = Game.PlayerTransform;
        }

        /// <summary>Le corps du gagnant de la manche (null s'il n'y en a pas).</summary>
        Transform WinnerBody()
        {
            if (roundWinner < 0) return null;
            for (int i = 0; i < Game.Seekers.Count; i++)
                if (Game.Seekers[i].Index == roundWinner && Game.Seekers[i].Body != null) return Game.Seekers[i].Body;
            return null;
        }

        /// <summary>
        /// Vrai pendant que le vainqueur de la manche DANSE (fin de manche, puis le choix des
        /// capacites) : la musique passe a la danse (MusicDirector), il danse dessus.
        /// </summary>
        public bool Dancing { get { return (Current == State.RoundOver || Current == State.Draft) && roundWinner >= 0 && WinnerBody() != null; } }

        /// <summary>
        /// LA FETE DU VAINQUEUR (01/10 -- Martin : "quand je gagne une manche, un effet, avec
        /// notre perso qui danse avec la musique") : il DANSE sur la musique (CharacterRig,
        /// un pas par temps), la Couronne flotte au-dessus de sa tete, et autour de lui, sur
        /// le rythme : des confettis, des feux d'artifice, un anneau d'or au sol, un
        /// projecteur (VictoryShow).
        /// </summary>
        static void Celebrate(Seeker s)
        {
            if (s == null || s.Body == null) return;
            CharacterRig rig = s.IsPlayer ? Game.Rig : null;
            if (!s.IsPlayer)
            {
                Rival r = Rival.Of(s);
                if (r != null) rig = r.Rig;
            }
            if (rig != null) rig.Celebrate(90f);
            if (Crown.Instance != null) Crown.Instance.ShowOff(s.Body);
            Vector3 at = s.Body.position + Vector3.up * 1.6f;
            Color[] confetti = { s.Colour, new Color(1f, 0.82f, 0.36f), Color.white, Color.Lerp(s.Colour, Color.white, 0.5f), new Color(0.45f, 0.8f, 1f) };
            Fx.Confetti(at, confetti, 180, 13f, Vector3.up, 40f);
            Fx.Shock(at, new Color(1f, 0.82f, 0.36f), 5f, 0.5f);
            VictoryShow.Begin(s);
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
                if (showSettings) { showSettings = false; selected = Current == State.Title ? 2 : 1; }
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
                // Les lignes : les reglages, puis le PSEUDO (on y tape au clavier), puis Retour.
                int rows = Settings.Labels.Length + 2;
                bool typing = selected == Settings.Labels.Length;
                if (typing ? FiefInput.ArrowUpPressed : FiefInput.UpPressed) { selected = (selected + rows - 1) % rows; Sfx.Pop(); }
                else if (typing ? FiefInput.ArrowDownPressed || FiefInput.ConfirmPressed : FiefInput.DownPressed) { selected = (selected + 1) % rows; Sfx.Pop(); }
                else if (typing) { }
                else if (selected < Settings.Labels.Length)
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
            if (row == 0) lobbyBots = Mathf.Clamp(lobbyBots + step, 1, Match.MaxPlayers - 1);
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
        /// SA PASSIVE, puis SON ATTAQUE (deux tours de table). On ne part jamais les
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
                if (cam.wide) LeaveEstablishingShot(cam);
                cam.ReleaseCinematic();
                if (cam.target != null) cam.yaw = cam.target.eulerAngles.y;
                cam.pitch = 3f;
            }
            // Le compte a rebours : trois secondes ou tout le monde est fige sur sa
            // plateforme. L'horloge (et les bots, les gargouilles) ne partent qu'au "PARTEZ".
            countdown = 3f;
            lastCount = 4;
            Toasts.Clear();
        }

        float countdown;
        /// <summary>Vrai pendant le 3, 2, 1 du depart (le HUD montre alors les touches).</summary>
        public bool CountingDown { get { return countdown > 0f; } }
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
            // Trois secondes de protection au depart : on quitte sa zone sans se faire
            // pousser ni tirer dessus avant d'avoir fait un pas.
            for (int i = 0; i < Game.Seekers.Count; i++) Game.Seekers[i].GraceUntil = Time.time + 3f;
            Sfx.Bell();
        }

        void DrawCountdown()
        {
            // 3, 2, 1 : un gros chiffre qui claque dans sa pastille ; puis le depart : le
            // triangle "jouer" qui eclate en or. (30/09 : plus de "PARTEZ !" ecrit.)
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.34f;
            if (countdown > 0f)
            {
                float frac = countdown - Mathf.Floor(countdown);
                float s = UiStyle.S(130) * Mathf.Lerp(1f, 1.35f, frac * frac);
                Icons.Pill(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), new Color(0.22f, 0.3f, 0.72f));
                Icons.Number(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), Mathf.CeilToInt(countdown).ToString(), Mathf.RoundToInt(s * 0.62f), Color.white, TextAnchor.MiddleCenter);
            }
            else if (goFlash > 0f)
            {
                float s = UiStyle.S(150) * (1f + (1f - goFlash) * 0.6f);
                Icons.Pill(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), new Color(1f, 0.78f, 0.25f), goFlash);
                Icons.Draw(new Rect(cx - s * 0.3f, cy - s * 0.3f, s * 0.6f, s * 0.6f), "jouer", new Color(1f, 1f, 1f, goFlash));
            }
        }

        /// <summary>
        /// FIN DE MANCHE : "winner" a sacre la Couronne sur un Monument (-1 : personne --
        /// le temps s'est ecoule ; 01/10 : la tenir a la fin ne suffit plus). C'est ici, et
        /// nulle part ailleurs, qu'une manche se termine -- en Phase 3, sur l'hote seulement.
        /// </summary>
        public void EndRound(int winner)
        {
            if (Current != State.Playing && Current != State.Paused) return;
            if (Game.Season != null) { roundTime = Game.Season.Elapsed; Game.Season.Stop(); }
            // (30/09 : plus de ralenti -- la fete du vainqueur se joue a vitesse normale.)
            Time.timeScale = 1f;
            slowMotion = 0f;
            if (Match.IsTieBreak && winner >= 0 && !Match.TieBreakers.Contains(winner)) winner = -1;
            roundWinner = winner;
            if (winner >= 0 && Match.Local != null && winner == Match.Local.Index) Stats.Delivered++;
            for (int i = 0; i < Game.Seekers.Count; i++)
                if (Game.Seekers[i].Index == winner) Celebrate(Game.Seekers[i]);
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
            // Apres la passive, le tour des actives (a chaque manche).
            if (Match.Draft.SecondStageNext)
            {
                Match.Draft.PrepareSecondStage();
                botPickTimer = 0.9f;
                selected = 0;
                Sfx.Pop();
                Go(State.Draft);
                return;
            }
            Sfx.Pop();
            Match.Launch();
            NextRound();
        }

        void PickCard(int card)
        {
            int me = Match.Local != null ? Match.Local.Index : 0;
            if (Match.Draft.Done || Match.Draft.Current != me) return;
            if (card < 0 || card >= Match.Draft.Offer.Count || Match.Local.Has(Match.Draft.Offer[card])) { Sfx.Deny(); return; }
            Ability chosen = Match.Draft.Offer[card];
            if (Match.Draft.TryPick(me, card))
            {
                CardArt.Taken(cardRects[card], AbilityInfo.Tint(chosen), true);
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
            Ability botTook = card < Match.Draft.Offer.Count ? Match.Draft.Offer[card] : Ability.Ruee;
            Rect botRect = card < cardRects.Length ? cardRects[card] : new Rect();
            if (Match.Draft.TryPick(slot, card))
            {
                CardArt.Taken(botRect, AbilityInfo.Tint(botTook), false);
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
            // (30/09 -- "plus pro, comme Fall Guys") UN GROS BOUTON ROND : bleu, jaune quand
            // on le vise (il se souleve), une icone a gauche, le mot cerne de sombre.
            float lift = on ? UiStyle.S(4) : 0f;
            Rect b = new Rect(r.x, r.y + UiStyle.S(3) - lift, r.width, r.height - UiStyle.S(6));
            Color fill = on ? new Color(1f, 0.78f, 0.18f) : primary ? new Color(0.34f, 0.4f, 0.95f) : new Color(0.2f, 0.25f, 0.56f);
            Icons.Pill(b, fill, alpha);
            string icon = IconFor(text);
            float ic = b.height * 0.7f;
            float tx = b.x + b.height * 0.32f;
            Color ink = new Color(0.12f, 0.09f, 0.28f, alpha);
            if (icon != null)
            {
                Icons.Draw(new Rect(tx, b.y + (b.height - ic) * 0.5f, ic, ic), icon, on ? ink : new Color(1f, 1f, 1f, alpha), !on);
                tx += ic + UiStyle.S(12);
            }
            int size = Mathf.RoundToInt(b.height * (primary ? 0.5f : 0.46f));
            Rect tr = new Rect(tx, b.y, b.xMax - tx - b.height * 0.3f, b.height);
            Icons.Text(tr, text, size, on ? ink : new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleLeft, !on);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>L'icone de chaque entree de menu (par son mot).</summary>
        static string IconFor(string text)
        {
            if (text.StartsWith("Jouer") || text.StartsWith("Commencer") || text.StartsWith("Reprendre") || text.StartsWith("Nouveau")) return "jouer";
            if (text.StartsWith("En ligne")) return "en-ligne";
            if (text.StartsWith("Réglages")) return "reglages";
            if (text.StartsWith("Commandes")) return "commandes";
            if (text.StartsWith("Quitter")) return "quitter";
            if (text.StartsWith("Abandonner")) return "drapeau";
            if (text.StartsWith("Retour")) return "retour";
            if (text.StartsWith("Le podium")) return "couronne";
            if (text.StartsWith("Choisir") || text.StartsWith("Prendre")) return "don";
            if (text.StartsWith("Joueurs")) return "joueur";
            if (text.StartsWith("Bots")) return "bot";
            if (text.StartsWith("Manches")) return "manches";
            if (text.StartsWith("Durée")) return "duree";
            if (text.StartsWith("Sensibilité")) return "souris-g";
            if (text.StartsWith("Volume")) return "volume";
            if (text.StartsWith("Champ")) return "vue";
            if (text.StartsWith("Taille")) return "texte";
            if (text.StartsWith("Plein")) return "ecran";
            if (text.StartsWith("Touche capacité")) return "cible";
            if (text.StartsWith("Touche pousser")) return "pousser";
            if (text.StartsWith("Pseudo")) return "pseudo";
            return null;
        }

        /// <summary>
        /// Une bande de couleur qui s'efface de gauche a droite. (30/09 : dessinee en 24
        /// tranches, leurs bords se chevauchaient d'un pixel et faisaient des RAIES
        /// verticales sur tout l'ecran-titre ; maintenant une seule texture en degrade.)
        /// </summary>
        static void Glide(Rect r, Color c)
        {
            if (fadeRight == null)
            {
                fadeRight = new Texture2D(256, 1, TextureFormat.RGBA32, false);
                fadeRight.wrapMode = TextureWrapMode.Clamp;
                fadeRight.hideFlags = HideFlags.HideAndDontSave;
                for (int x = 0; x < 256; x++)
                {
                    float k = 1f - x / 255f;
                    fadeRight.SetPixel(x, 0, new Color(1f, 1f, 1f, k * k * (3f - 2f * k)));
                }
                fadeRight.Apply();
            }
            Color was = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, fadeRight, ScaleMode.StretchToFill, true);
            GUI.color = was;
        }
        static Texture2D fadeRight;

        /// <summary>Un grand titre, centre, de la taille voulue.</summary>
        static void Headline(float y, float size, string text, Color c)
        {
            // (30/09) Les grands titres : gros, ronds, cernes de sombre (facon Fall Guys).
            Icons.Number(new Rect(0f, y, Screen.width, UiStyle.S(size * 1.3f)), text, UiStyle.S(size), c, TextAnchor.MiddleCenter);
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

        float Left { get { return Mathf.Max(UiStyle.S(40), Screen.width * 0.09f); } }

        // ------------------------------------------------------------------ le titre

        void DrawTitle()
        {
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.15f) / 0.85f));
            float late = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.45f) / 0.55f));
            float x = Left;
            float w = Mathf.Min(UiStyle.S(640), Screen.width - x * 2f);
            float y = Screen.height * 0.5f - UiStyle.S(230) + (1f - ease) * UiStyle.S(18);

            // (30/09 -- "un meilleur menu") Un voile sombre a gauche, sur le plan de l'ile
            // qui tourne ; le nom en or avec un halo ; un filet ; une phrase ; puis les mots.
            Glide(new Rect(0f, 0f, Mathf.Max(UiStyle.S(760), Screen.width * 0.55f), Screen.height), new Color(0.02f, 0.015f, 0.03f, 0.8f * ease));
            // (30/09 -- "plus pro, comme Fall Guys") La Couronne, "FIEF" en lettres rondes
            // cernees, un ruban bleu, puis les gros boutons.
            float glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.4f);
            float cs = UiStyle.S(92) * (1f + 0.04f * glow);
            Icons.Draw(new Rect(x + UiStyle.S(8), y - UiStyle.S(70), cs, cs), "couronne", new Color(1f, 0.86f, 0.35f, ease));
            Icons.Number(new Rect(x, y, w, UiStyle.S(150)), "FIEF", UiStyle.S(140), new Color(1f, 0.84f, 0.3f, ease), TextAnchor.MiddleLeft);
            y += UiStyle.S(150);
            Rect ribbon = new Rect(x + UiStyle.S(8), y, UiStyle.S(280), UiStyle.S(46));
            Icons.Pill(ribbon, new Color(0.3f, 0.36f, 0.9f), ease);
            Icons.Number(ribbon, "LA COURONNE", UiStyle.S(26), new Color(1f, 1f, 1f, ease), TextAnchor.MiddleCenter);
            y += UiStyle.S(84);

            for (int i = 0; i < TitleItems.Length; i++)
            {
                bool primary = i == 0;
                float h = UiStyle.S(primary ? 70 : 56);
                if (Entry(new Rect(x, y, UiStyle.S(primary ? 440 : 400), h), TitleItems[i], i, primary, late) && late > 0.9f) Activate(i);
                y += h + UiStyle.S(10);
            }

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
            Glide(new Rect(0f, 0f, Mathf.Max(UiStyle.S(760), Screen.width * 0.55f), Screen.height), new Color(0.02f, 0.015f, 0.03f, 0.8f));
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(230);
            Icons.Number(new Rect(x, y, UiStyle.S(600), UiStyle.S(56)), "NOUVEAU MATCH", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
            y += UiStyle.S(80);

            string[] labels = { "Joueurs", "Bots", "Manches", "Durée max" };
            string[] values = { (lobbyBots + 1).ToString(), Match.BotLevels[Match.BotLevel], lobbyRounds.ToString(), lobbyMinutes + " min" };
            for (int i = 0; i < LobbyRows; i++)
            {
                int row = i;
                ValueRow(x, y, labels[i], values[i], i, step => Adjust(row, step));
                y += UiStyle.S(58);

                // Sous "Joueurs" : qui joue, en pastilles a sa couleur (01/10 : plus de petites
                // lignes grises floues -- des pastilles nettes).
                if (i == 0)
                {
                    float wx = x + UiStyle.S(8);
                    float ph = UiStyle.S(28);
                    int fs = UiStyle.S(15);
                    for (int k = 0; k <= lobbyBots; k++)
                    {
                        string name = Match.NameOf(k);
                        float nw = Icons.Width(name, fs) + ph * 0.9f;
                        if (wx + nw > x + UiStyle.S(640)) { wx = x + UiStyle.S(8); y += ph + UiStyle.S(8); }
                        Rect pr = new Rect(wx, y - UiStyle.S(4), nw, ph);
                        Icons.Pill(pr, Match.ColourOf(k));
                        Icons.Number(pr, name, fs, Color.white, TextAnchor.MiddleCenter);
                        wx += nw + UiStyle.S(10);
                    }
                    y += ph + UiStyle.S(10);
                }
            }

            // La duree du match, a peu pres (une manche dure rarement tout son temps) : un chrono.
            int estimate = Mathf.RoundToInt(lobbyRounds * lobbyMinutes * 0.7f);
            Rect est = new Rect(x + UiStyle.S(8), y, UiStyle.S(150), UiStyle.S(40));
            Icons.Pill(est, new Color(0.14f, 0.16f, 0.36f));
            Icons.Draw(new Rect(est.x + UiStyle.S(6), est.y + UiStyle.S(4), UiStyle.S(32), UiStyle.S(32)), "chrono", Color.white);
            Icons.Number(new Rect(est.x + UiStyle.S(40), est.y, est.width - UiStyle.S(46), est.height), "~" + estimate + " min", UiStyle.S(19), Color.white, TextAnchor.MiddleCenter);
            y += UiStyle.S(64);

            if (Entry(new Rect(x, y, UiStyle.S(420), UiStyle.S(50)), "Commencer", LobbyRows, true, 1f)) Activate(LobbyRows);
            y += UiStyle.S(56);
            if (Entry(new Rect(x, y, UiStyle.S(420), UiStyle.S(38)), "Retour", LobbyRows + 1, false, 1f)) Activate(LobbyRows + 1);
        }

        /// <summary>Une ligne a regler : "Joueurs   ‹ 4 ›". Les fleches se cliquent ; au clavier, gauche/droite.</summary>
        void ValueRow(float x, float y, string label, string value, int index, System.Action<int> adjust)
        {
            Rect row = new Rect(x, y, UiStyle.S(300), UiStyle.S(48));
            Entry(row, label, index, false, 1f);
            bool on = selected == index;
            // La valeur, dans sa pastille, entre deux fleches.
            float vx = x + UiStyle.S(316);
            Rect minus = new Rect(vx, y + UiStyle.S(6), UiStyle.S(36), UiStyle.S(36));
            Rect val = new Rect(vx + UiStyle.S(44), y + UiStyle.S(3), UiStyle.S(170), UiStyle.S(42));
            Rect plus = new Rect(vx + UiStyle.S(222), y + UiStyle.S(6), UiStyle.S(36), UiStyle.S(36));
            Icons.Pill(val, on ? new Color(0.3f, 0.34f, 0.72f) : new Color(0.14f, 0.16f, 0.36f));
            Icons.Number(val, value, UiStyle.S(22), on ? new Color(1f, 0.86f, 0.35f) : Color.white, TextAnchor.MiddleCenter);
            Color arrow = new Color(1f, 1f, 1f, on ? 1f : 0.45f);
            Matrix4x4 keep = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), minus.center);
            Icons.Draw(minus, "jouer", arrow);
            GUI.matrix = keep;
            Icons.Draw(plus, "jouer", arrow);
            if (GUI.Button(minus, GUIContent.none, GUIStyle.none)) { selected = index; adjust.Invoke(-1); }
            if (GUI.Button(plus, GUIContent.none, GUIStyle.none)) { selected = index; adjust.Invoke(1); }
        }

        GUIStyle pseudoStyle;

        /// <summary>La ligne du pseudo : un champ ou l'on tape (16 lettres au plus).</summary>
        void PseudoRow(float x, float y, int index)
        {
            Rect row = new Rect(x, y, UiStyle.S(300), UiStyle.S(48));
            Entry(row, "Pseudo", index, false, 1f);
            bool on = selected == index;
            if (pseudoStyle == null || pseudoStyle.fontSize != UiStyle.S(24))
            {
                pseudoStyle = new GUIStyle(UiStyle.Head);
                pseudoStyle.fontSize = UiStyle.S(24);
                pseudoStyle.alignment = TextAnchor.MiddleCenter;
                pseudoStyle.normal.textColor = new Color(1f, 0.84f, 0.5f);
                pseudoStyle.focused.textColor = new Color(1f, 0.9f, 0.6f);
            }
            Rect field = new Rect(x + UiStyle.S(316), y + UiStyle.S(3), UiStyle.S(258), UiStyle.S(42));
            Icons.Pill(field, on ? new Color(0.3f, 0.34f, 0.72f) : new Color(0.14f, 0.16f, 0.36f));
            GUI.SetNextControlName("pseudo");
            string typed = GUI.TextField(field, Settings.Pseudo, Settings.PseudoLength, pseudoStyle);
            if (typed != Settings.Pseudo) Settings.SetPseudo(typed);
            if (on && GUI.GetNameOfFocusedControl() != "pseudo") GUI.FocusControl("pseudo");
            else if (!on && GUI.GetNameOfFocusedControl() == "pseudo") GUI.FocusControl(null);
            if (GUI.Button(new Rect(x, y, UiStyle.S(300), row.height), GUIContent.none, GUIStyle.none)) selected = index;
        }

        // ------------------------------------------------------------------ les reglages

        /// <summary>LES REGLAGES : sensibilite, volume, champ de vision, plein ecran. Garde d'une partie a l'autre.</summary>
        void DrawSettings()
        {
            Glide(new Rect(0f, 0f, Mathf.Max(UiStyle.S(760), Screen.width * 0.55f), Screen.height), new Color(0.02f, 0.015f, 0.03f, 0.8f));
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(170);
            Icons.Number(new Rect(x, y, UiStyle.S(600), UiStyle.S(56)), "RÉGLAGES", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
            y += UiStyle.S(80);
            for (int i = 0; i < Settings.Labels.Length; i++)
            {
                int row = i;
                ValueRow(x, y, Settings.Labels[i], Settings.Value(i), i, step => { Settings.Step(row, step); Sfx.Pop(); });
                y += UiStyle.S(56);
            }
            // LE PSEUDO : on le tape. Il s'affiche au-dessus de ta tete et quand tu gagnes.
            PseudoRow(x, y, Settings.Labels.Length);
            y += UiStyle.S(56);
            y += UiStyle.S(24);
            if (Entry(new Rect(x, y, UiStyle.S(300), UiStyle.S(40)), "Retour", Settings.Labels.Length + 1, false, 1f)) { showSettings = false; selected = 0; }        }

        // ------------------------------------------------------------------ en ligne

        /// <summary>
        /// EN LIGNE : prepare, pas encore branche. Le jeu est deja coupe en places
        /// (Match.Slots) et tout passe par des methodes que l'hote appellera -- mais
        /// le transport (Steam) vient en Phase 3. On le dit franchement.
        /// </summary>
        void DrawOnline()
        {
            Glide(new Rect(0f, 0f, Mathf.Max(UiStyle.S(760), Screen.width * 0.55f), Screen.height), new Color(0.02f, 0.015f, 0.03f, 0.8f));
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(140);
            Icons.Number(new Rect(x, y, UiStyle.S(600), UiStyle.S(56)), "EN LIGNE", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
            y += UiStyle.S(70);
            // Pas encore branche : l'icone "en ligne" barree, et "BIENTOT". Les bots, eux, jouent deja.
            float s = UiStyle.S(84);
            Rect r = new Rect(x, y, s * 2.4f, s);
            Icons.Pill(r, new Color(0.14f, 0.16f, 0.36f));
            Icons.Draw(new Rect(r.x + s * 0.12f, r.y + s * 0.1f, s * 0.8f, s * 0.8f), "en-ligne", Color.white);
            Icons.Draw(new Rect(r.x + s * 0.55f, r.y + s * 0.45f, s * 0.46f, s * 0.46f), "chrono", Wings.Gold);
            Icons.Draw(new Rect(r.xMax - s * 0.95f, r.y + s * 0.1f, s * 0.8f, s * 0.8f), "bot", new Color(0.6f, 0.9f, 1f));
            Icons.Number(new Rect(r.xMax + UiStyle.S(20), r.y, UiStyle.S(400), s), "BIENTÔT", UiStyle.S(40), Wings.Gold, TextAnchor.MiddleLeft);
            y += s + UiStyle.S(50);
            if (Entry(new Rect(x, y, UiStyle.S(420), UiStyle.S(40)), "Retour", 0, false, 1f)) Activate(0);        }

        // ------------------------------------------------------------------ l'intro de manche

        float BriefingLength { get { return Match.Played == 0 && !Match.IsTieBreak ? 7f : 3.8f; } }

        /// <summary>
        /// L'INTRO DE MANCHE, toute seule, quelques secondes : "MANCHE 2", et tes
        /// capacites avec leurs touches. La premiere fois, la regle en deux lignes.
        /// Echap passe.
        /// </summary>
        void DrawBriefing()
        {
            UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.01f, 0.02f, 0.82f));
            float t = stateTime;
            float a = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((BriefingLength - t) / 0.5f);
            float y = Screen.height * 0.3f;

            string head = Match.IsTieBreak ? "DÉPARTAGE" : "MANCHE " + Match.RoundNumber;
            Headline(y, 64, head, new Color(0.93f, 0.78f, 0.45f, a));
            y += UiStyle.S(100);

            if (Match.IsTieBreak)
            {
                // Qui peut gagner le departage : leurs pastilles, cote a cote.
                float ph = UiStyle.S(44), gap = UiStyle.S(14);
                int fs = UiStyle.S(20);
                float total = 0f;
                for (int i = 0; i < Match.TieBreakers.Count; i++) total += Icons.Width(Match.Slots[Match.TieBreakers[i]].Name, fs) + ph + (i > 0 ? gap : 0f);
                float px = (Screen.width - total) * 0.5f;
                for (int i = 0; i < Match.TieBreakers.Count; i++)
                {
                    PlayerSlot p = Match.Slots[Match.TieBreakers[i]];
                    float w = Icons.Width(p.Name, fs) + ph;
                    Rect r = new Rect(px, y, w, ph);
                    Icons.Pill(r, p.Colour, a);
                    Icons.Number(r, p.Name, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
                    px += w + gap;
                }
                y += ph + UiStyle.S(30);
            }
            else if (Match.Played == 0)
            {
                // (30/09 -- "aucun texte") LA REGLE EN ICONES, qui arrivent une a une :
                // la tour -> la Couronne -> les ailes -> le courant d'air -> le Monument (3 s),
                // et en dessous : la main + la Couronne (pousser le porteur, c'est la lui voler).
                string[] steps = { "tour", "couronne", "ailes", "courant", "sacre" };
                Color[] tints = { Color.white, new Color(1f, 0.86f, 0.35f), Wings.Glow, new Color(0.75f, 0.92f, 1f), Monument.Blue };
                float s = UiStyle.S(92), gap = UiStyle.S(56);
                float x = (Screen.width - (steps.Length * s + (steps.Length - 1) * gap)) * 0.5f;
                for (int i = 0; i < steps.Length; i++)
                {
                    float k = Mathf.Clamp01((t - 0.6f - i * 0.45f) / 0.35f) * a;
                    float pop = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01((t - 0.6f - i * 0.45f) / 0.35f) * Mathf.PI);
                    Rect r = new Rect(x + i * (s + gap), y, s, s);
                    Rect rr = new Rect(r.center.x - s * pop * 0.5f, r.center.y - s * pop * 0.5f, s * pop, s * pop);
                    Icons.Pill(rr, new Color(0.22f, 0.26f, 0.56f), k);
                    Icons.Draw(new Rect(rr.x + rr.width * 0.15f, rr.y + rr.height * 0.15f, rr.width * 0.7f, rr.height * 0.7f), steps[i], new Color(tints[i].r, tints[i].g, tints[i].b, k));
                    if (i < steps.Length - 1) Icons.Draw(new Rect(r.xMax + gap * 0.15f, r.y + s * 0.32f, gap * 0.7f, s * 0.36f), "jouer", new Color(1f, 1f, 1f, 0.6f * k), false);
                }
                y += s + UiStyle.S(30);
                float d = Mathf.Clamp01((t - 3.2f) / 0.4f) * a;
                float cx = Screen.width * 0.5f;
                Rect chip = new Rect(cx - s * 1.6f, y, s * 3.2f, s * 0.9f);
                Icons.Pill(chip, new Color(0.62f, 0.2f, 0.25f), d);
                Icons.Key(new Rect(chip.x + s * 0.12f, chip.y + s * 0.08f, s * 0.74f, s * 0.74f), AbilityInfo.PushKey, d);
                Icons.Draw(new Rect(chip.x + s * 1.0f, chip.y + s * 0.08f, s * 0.74f, s * 0.74f), "pousser", new Color(1f, 1f, 1f, d));
                Icons.Draw(new Rect(chip.x + s * 1.9f, chip.y + s * 0.08f, s * 0.74f, s * 0.74f), "couronne", new Color(1f, 0.86f, 0.35f, d));
                y += s + UiStyle.S(20);
            }
            else
            {
                ScoreLine(y, a);
                y += UiStyle.S(60);
            }

            // Ta capacite : son rond, son icone, sa touche.
            PlayerSlot me = Match.Local;
            if (me != null)
            {
                List<Ability> actives = me.Actives;
                float e = Mathf.Clamp01((t - (Match.Played == 0 ? 3.6f : 0.6f)) / 0.5f) * a;
                float s = UiStyle.S(84);
                float cx = Screen.width * 0.5f;
                for (int i = 0; i < actives.Count; i++)
                {
                    Rect r = new Rect(cx - s * 0.5f, y, s, s);
                    Icons.Pill(r, AbilityInfo.Tint(actives[i]), e);
                    Icons.Draw(new Rect(r.x + s * 0.17f, r.y + s * 0.17f, s * 0.66f, s * 0.66f), Icons.Of(actives[i]), new Color(1f, 1f, 1f, e));
                    Icons.Key(new Rect(r.x - s * 0.2f, r.yMax - s * 0.46f, s * 0.5f, s * 0.5f), AbilityInfo.Keys[0], e);
                    y += s + UiStyle.S(10);
                }
            }        }

        /// <summary>
        /// Le score en une ligne : une pastille par joueur, a sa couleur (toi : cerne d'or),
        /// son pseudo, une petite Couronne et ses manches gagnees.
        /// </summary>
        static void ScoreLine(float y, float a)
        {
            int n = Match.Slots.Count;
            float h = UiStyle.S(40), gap = UiStyle.S(12);
            float cell = Mathf.Min(UiStyle.S(200), (Screen.width - UiStyle.S(60) - gap * (n - 1)) / Mathf.Max(1, n));
            float x = (Screen.width - (cell * n + gap * (n - 1))) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                PlayerSlot p = Match.Slots[i];
                Rect r = new Rect(x + i * (cell + gap), y, cell, h);
                if (p.IsLocal) Icons.Pill(new Rect(r.x - UiStyle.S(4), r.y - UiStyle.S(4), r.width + UiStyle.S(8), r.height + UiStyle.S(8)), Wings.Gold, a);
                Icons.Pill(r, p.Colour, a);
                int fs = Mathf.RoundToInt(h * 0.42f);
                string name = p.Name;
                float room = cell - h * 1.9f;
                while (name.Length > 3 && Icons.Width(name, fs) > room) name = name.Substring(0, name.Length - 1);
                Icons.Number(new Rect(r.x + h * 0.35f, r.y, room, h), name, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleLeft);
                Icons.Draw(new Rect(r.xMax - h * 1.45f, r.y + h * 0.12f, h * 0.76f, h * 0.76f), "couronne", new Color(1f, 0.86f, 0.35f, a));
                Icons.Number(new Rect(r.xMax - h * 0.75f, r.y, h * 0.6f, h), p.Wins.ToString(), Mathf.RoundToInt(h * 0.58f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
            }
        }

        // ------------------------------------------------------------------ la pause

        void DrawPause()
        {
            Glide(new Rect(0f, 0f, Mathf.Max(UiStyle.S(760), Screen.width * 0.55f), Screen.height), new Color(0.02f, 0.015f, 0.03f, 0.8f));
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(170);
            Icons.Number(new Rect(x, y, UiStyle.S(600), UiStyle.S(56)), "PAUSE", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
            y += UiStyle.S(66);
            Season s = Game.Season;
            if (s != null)
            {
                // Ou en est le match : la manche (ou le drapeau du departage), et le temps restant.
                float ph = UiStyle.S(40);
                Rect mr = new Rect(x, y, UiStyle.S(130), ph);
                Icons.Pill(mr, new Color(0.14f, 0.16f, 0.36f));
                Icons.Draw(new Rect(mr.x + UiStyle.S(6), mr.y + UiStyle.S(4), ph - UiStyle.S(8), ph - UiStyle.S(8)), Match.IsTieBreak ? "drapeau" : "manches", Color.white);
                if (!Match.IsTieBreak) Icons.Number(new Rect(mr.x + ph, mr.y, mr.width - ph - UiStyle.S(6), ph), Match.RoundNumber + "/" + Match.Rounds, UiStyle.S(19), Color.white, TextAnchor.MiddleCenter);
                Rect tr = new Rect(mr.xMax + UiStyle.S(14), y, UiStyle.S(130), ph);
                Icons.Pill(tr, new Color(0.14f, 0.16f, 0.36f));
                Icons.Draw(new Rect(tr.x + UiStyle.S(6), tr.y + UiStyle.S(4), ph - UiStyle.S(8), ph - UiStyle.S(8)), "chrono", Color.white);
                Icons.Number(new Rect(tr.x + ph, tr.y, tr.width - ph - UiStyle.S(6), ph), Hud.Clock(s.Remaining), UiStyle.S(19), Color.white, TextAnchor.MiddleCenter);
            }
            y += UiStyle.S(66);
            for (int i = 0; i < PauseItems.Length; i++)
            {
                bool primary = i == 0;
                float h = UiStyle.S(primary ? 48 : 38);
                string label = i == 3 && confirmAbandon ? "Abandonner ? Encore !" : PauseItems[i];
                if (Entry(new Rect(x, y, UiStyle.S(620), h), label, i, primary, 1f)) Activate(i);
                y += h + UiStyle.S(4);
            }        }

        // ------------------------------------------------------------------ fin de manche

        /// <summary>
        /// FIN DE MANCHE : qui l'a gagnee (son nom, a sa couleur, en grand), et le
        /// score en une ligne. Puis le choix des capacites -- tout seul, ou Entree.
        /// </summary>
        float roundTime;

        void DrawRoundOver()
        {
            // (30/09) Le vainqueur fete au MILIEU de l'ecran : son nom en haut, le reste en
            // bas, rien par-dessus lui.
            float a = Mathf.Clamp01((stateTime - 0.8f) / 0.6f);
            float y = Screen.height * 0.07f;
            PlayerSlot w = roundWinner >= 0 && roundWinner < Match.Slots.Count ? Match.Slots[roundWinner] : null;
            // Un voile en haut et en bas seulement, pour lire les mots.
            UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, Screen.height * 0.3f), new Color(0f, 0f, 0f, 0.55f * a));
            UiStyle.FadeBand(new Rect(0f, Screen.height * 0.66f, Screen.width, Screen.height * 0.34f), new Color(0f, 0f, 0f, 0.6f * a));
            float cx = Screen.width * 0.5f;
            if (w != null)
            {
                // SON PSEUDO, EN GRAND, EN OR : il claque et se pose. Dessous : la Couronne,
                // le Monument et le temps qu'il a mis -- en icones.
                float punch = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((stateTime - 0.8f) / 0.2f));
                Color gold = new Color(1f, 0.82f, 0.38f, a);
                Headline(y, 88 * punch, UiStyle.Spaced(w.Name.ToUpperInvariant()), gold);
                float s = UiStyle.S(52);
                Rect how = new Rect(cx - s * 2.1f, y + UiStyle.S(112), s * 4.2f, s);
                Icons.Pill(how, new Color(0.18f, 0.2f, 0.42f), a);
                Icons.Draw(new Rect(how.x + s * 0.12f, how.y + s * 0.08f, s * 0.84f, s * 0.84f), "couronne", new Color(1f, 0.86f, 0.35f, a));
                Icons.Draw(new Rect(how.x + s * 1.02f, how.y + s * 0.1f, s * 0.8f, s * 0.8f), "sacre", new Color(Monument.Blue.r, Monument.Blue.g, Monument.Blue.b, a));
                Icons.Number(new Rect(how.x + s * 1.9f, how.y, how.width - s * 2f, s), Hud.Clock(roundTime), Mathf.RoundToInt(s * 0.55f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
            }
            else
            {
                // PERSONNE : le temps s'est ecoule (01/10 : tenir la Couronne ne suffit plus) --
                // le chrono, et la Couronne barree.
                float s = UiStyle.S(120);
                Rect r = new Rect(cx - s * 1.1f, y + UiStyle.S(20), s * 2.2f, s);
                Icons.Pill(r, new Color(0.3f, 0.3f, 0.4f), a);
                Icons.Draw(new Rect(r.x + s * 0.12f, r.y + s * 0.1f, s * 0.8f, s * 0.8f), "chrono", new Color(1f, 1f, 1f, a));
                Icons.Draw(new Rect(r.xMax - s * 0.95f, r.y + s * 0.1f, s * 0.8f, s * 0.8f), "couronne", new Color(1f, 0.86f, 0.35f, a * 0.6f));
                Icons.Draw(new Rect(r.xMax - s * 0.8f, r.y + s * 0.25f, s * 0.5f, s * 0.5f), "croix", new Color(1f, 0.4f, 0.35f, a));
            }

            // En bas : le score, et la suite.
            y = Screen.height * 0.72f;
            ScoreLine(y, a);
            y += UiStyle.S(74);

            if (stateTime > 1.8f)
            {
                string label = Match.Over ? "Le podium" : "Choisir une capacité";
                float bw = UiStyle.S(420);
                if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, UiStyle.S(48)), label, 0, true, a)) Activate(0);
            }
        }

        // ------------------------------------------------------------------ le choix

        /// <summary>
        /// LE CHOIX DES CAPACITES (refait le 01/10). En haut : PASSIVE ou CLIC GAUCHE, et la
        /// file des joueurs en pastilles (qui choisit bat ; sous qui a choisi, l'icone de ce
        /// qu'il a pris). Au milieu : les cartes (voir CardArt) -- l'icone, le nom, la phrase
        /// qui n'est jamais coupee. En bas : tes capacites, en ronds.
        /// </summary>
        void DrawDraft()
        {
            // Des rayons qui tournent lentement, des braises qui montent (voir CardArt).
            CardArt.Background(Match.Draft.Stage == 0 ? new Color(0.55f, 0.75f, 1f) : Palette.Gold, 0.68f);

            // (01/10 -- plus de phrase d'explication) : un mot, et la souris du clic gauche
            // pour l'active. L'ordre de passage se lit dans la file des pastilles.
            float y = Screen.height * 0.06f;
            string title = Match.Draft.Stage == 0 ? "PASSIVE" : "CLIC GAUCHE";
            Headline(y, 46, UiStyle.Spaced(title), Palette.Gold);
            if (Match.Draft.Stage == 1)
            {
                float ks = UiStyle.S(54);
                float tw = Icons.Width(UiStyle.Spaced(title), UiStyle.S(46));
                Icons.Key(new Rect(Screen.width * 0.5f - tw * 0.5f - ks - UiStyle.S(14), y + UiStyle.S(4), ks, ks), AbilityInfo.Keys[0], 1f);
            }
            y += UiStyle.S(74);

            y = DrawDraftOrder(y);
            y += UiStyle.S(26);

            // Les cartes.
            List<Ability> offer = Match.Draft.Offer;
            int me = Match.Local != null ? Match.Local.Index : 0;
            bool myTurn = !Match.Draft.Done && Match.Draft.Current == me;
            int n2 = Mathf.Max(1, offer.Count);
            float gap = UiStyle.S(18);
            float cw = Mathf.Min(UiStyle.S(250), (Screen.width - UiStyle.S(60) - gap * (n2 - 1)) / n2);
            float ch = Mathf.Min(cw * 1.45f, Screen.height * 0.46f);
            float cx = (Screen.width - (cw * n2 + gap * (n2 - 1))) * 0.5f;
            if (cardLift.Length < n2) cardLift = new float[Match.MaxPlayers + 2];
            for (int i = 0; i < offer.Count; i++)
            {
                Ability p = offer[i];
                // Elles arrivent face cachee, puis se retournent une a une.
                float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((stateTime - 0.25f - i * 0.16f) / 0.5f));
                bool owned = Match.Local != null && Match.Local.Has(p);
                Rect hit = new Rect(cx + i * (cw + gap), y, cw, ch);
                bool hover = hit.Contains(Event.current.mousePosition);
                if (hover && hoverFollows && myTurn) selected = i;
                bool on = myTurn && selected == i && !owned;
                if (Event.current.type == EventType.Repaint)
                    cardLift[i] = Mathf.MoveTowards(cardLift[i], on ? 1f : 0f, Time.unscaledDeltaTime * 6f);
                float lift = Mathf.SmoothStep(0f, 1f, cardLift[i]);
                Rect card = new Rect(hit.x, hit.y + (1f - enter) * UiStyle.S(40) - lift * UiStyle.S(22), cw, ch);
                cardRects[i] = card;
                Card(card, p, owned, on, lift, enter, me);
                if (myTurn && enter > 0.9f && GUI.Button(hit, GUIContent.none, GUIStyle.none)) { selected = i; PickCard(i); }
            }
            y += ch + UiStyle.S(26);

            if (myTurn)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
                Headline(y, 34, "À TOI !", new Color(1f, 0.85f, 0.4f, pulse));
            }
            else if (!Match.Draft.Done)
            {
                // Qui choisit : sa pastille, et un sablier (le chrono) qui bat.
                PlayerSlot who = Match.Slots[Match.Draft.Current];
                int fs = UiStyle.S(22);
                float ph = UiStyle.S(46);
                float w = Icons.Width(who.Name, fs) + ph * 1.9f;
                Rect r = new Rect((Screen.width - w) * 0.5f, y, w, ph);
                Icons.Pill(r, who.Colour);
                float beat = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 8f);
                Icons.Draw(new Rect(r.x + ph * 0.14f, r.y + ph * 0.12f, ph * 0.76f * beat, ph * 0.76f * beat), "chrono", Color.white);
                Icons.Number(new Rect(r.x + ph, r.y, r.width - ph * 1.3f, ph), who.Name, fs, Color.white, TextAnchor.MiddleCenter);
            }
            else
            {
                string label = Match.IsTieBreak ? "Le départage" : "Manche " + Match.RoundNumber;
                float bw = UiStyle.S(360);
                if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, UiStyle.S(48)), label, 0, true, 1f)) FinishDraft();
            }

            // Ce que tu as deja, en bas : tes ronds de capacites, comme dans le HUD.
            if (Match.Local != null && Match.Local.Abilities.Count > 0)
            {
                List<Ability> mine = Match.Local.Abilities;
                float s = UiStyle.S(58), gap2 = UiStyle.S(14);
                float bx = (Screen.width - (mine.Count * s + (mine.Count - 1) * gap2)) * 0.5f;
                float by = Screen.height - UiStyle.S(24) - s;
                for (int i = 0; i < mine.Count; i++)
                {
                    Rect r = new Rect(bx + i * (s + gap2), by, s, s);
                    Icons.Pill(r, Color.Lerp(AbilityInfo.Tint(mine[i]), new Color(0.2f, 0.18f, 0.36f), 0.35f));
                    Icons.Draw(new Rect(r.x + s * 0.16f, r.y + s * 0.16f, s * 0.68f, s * 0.68f), Icons.Of(mine[i]), Color.white);
                    if (AbilityInfo.IsActive(mine[i])) Icons.Key(new Rect(r.x - s * 0.18f, r.yMax - s * 0.46f, s * 0.5f, s * 0.5f), AbilityInfo.Keys[0], 1f);
                }
            }            CardArt.Sparks();
        }

        float[] cardLift = new float[Match.MaxPlayers + 2];
        readonly Rect[] cardRects = new Rect[Match.MaxPlayers + 2];

        /// <summary>La file des joueurs : une pastille par joueur, a sa couleur ; sous celles qui ont choisi, ce qu'elles ont pris.</summary>
        float DrawDraftOrder(float y)
        {
            // (01/10) Des pastilles rondes a la couleur de chacun (toi : cerne d'or), celle qui
            // choisit grossit et bat ; sous qui a choisi, l'icone de ce qu'il a pris.
            List<int> order = Match.Draft.Order;
            float gap = UiStyle.S(12);
            float chipW = Mathf.Min(UiStyle.S(160), (Screen.width - UiStyle.S(80)) / Mathf.Max(1, order.Count) - gap);
            float ch = UiStyle.S(36);
            float total = order.Count * chipW + (order.Count - 1) * gap;
            float ox = (Screen.width - total) * 0.5f;
            for (int i = 0; i < order.Count; i++)
            {
                PlayerSlot s = Match.Slots[order[i]];
                bool now = !Match.Draft.Done && Match.Draft.Current == order[i];
                bool done = i < Match.Draft.Turn;
                Rect chip = new Rect(ox + i * (chipW + gap), y, chipW, ch);
                if (now)
                {
                    float g = UiStyle.S(4) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
                    chip = new Rect(chip.x - g, chip.y - g, chip.width + g * 2f, chip.height + g * 2f);
                }
                if (s.IsLocal) Icons.Pill(new Rect(chip.x - UiStyle.S(4), chip.y - UiStyle.S(4), chip.width + UiStyle.S(8), chip.height + UiStyle.S(8)), Wings.Gold, done ? 0.6f : 1f);
                Icons.Pill(chip, done ? Color.Lerp(s.Colour, new Color(0.2f, 0.2f, 0.3f), 0.5f) : s.Colour);
                int fs = Mathf.RoundToInt(ch * 0.44f);
                string name = s.Name;
                while (name.Length > 3 && Icons.Width(name, fs) > chip.width - ch * 0.9f) name = name.Substring(0, name.Length - 1);
                Icons.Number(chip, name, fs, new Color(1f, 1f, 1f, done ? 0.75f : 1f), TextAnchor.MiddleCenter);
                // Ce qu'il a pris a ce tour-ci : l'icone, dans un petit rond a sa couleur.
                for (int k = 0; k < Match.Draft.Picked.Count; k++)
                {
                    if (Match.Draft.PickedBy[k] != order[i]) continue;
                    float ps = UiStyle.S(34);
                    Rect pr = new Rect(chip.center.x - ps * 0.5f, chip.yMax + UiStyle.S(8), ps, ps);
                    Icons.Pill(pr, AbilityInfo.Tint(Match.Draft.Picked[k]));
                    Icons.Draw(new Rect(pr.x + ps * 0.14f, pr.y + ps * 0.14f, ps * 0.72f, ps * 0.72f), Icons.Of(Match.Draft.Picked[k]), Color.white);
                }
            }
            return y + UiStyle.S(86);
        }

        /// <summary>UNE CARTE (28/09 : dessinee par CardArt -- dos, retournement, cadre d'or, rayons, etincelles).</summary>
        void Card(Rect card, Ability p, bool owned, bool on, float lift, float enter, int me)
        {
            bool active = AbilityInfo.IsActive(p);
            string replaces = null;
            if (!owned && Match.Local != null)
            {
                int lost = Match.Draft.WouldReplace(me, p);
                if (lost >= 0) replaces = AbilityInfo.Name((Ability)lost);
            }
            CardArt.Draw(card, AbilityInfo.Tint(p), AbilityInfo.Name(p), AbilityInfo.Line(p),
                         active ? AbilityInfo.Cooldown(p) : 0f, active ? AbilityInfo.Keys[0] : null, replaces, owned, on, lift, enter, Icons.Of(p));
        }

        // ------------------------------------------------------------------ fin du match

        void DrawEnd()
        {
            // (01/10 -- sans phrases) Le champion : son pseudo en or et la Couronne ; le
            // classement en pastilles ; ce que TU as fait, en icones et en chiffres.
            float a = Mathf.Clamp01(stateTime / 0.8f);
            float y = Screen.height * 0.1f;
            float cx = Screen.width * 0.5f;
            PlayerSlot champion = Match.Champion;
            if (champion != null)
            {
                float cs = UiStyle.S(96) * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 2f));
                Icons.Draw(new Rect(cx - cs * 0.5f, y - UiStyle.S(20), cs, cs), "couronne", new Color(1f, 0.86f, 0.35f, a));
                Headline(y + UiStyle.S(78), 72, UiStyle.Spaced(champion.Name.ToUpperInvariant()), new Color(1f, 0.82f, 0.38f, a));
            }
            else
            {
                float cs = UiStyle.S(96);
                Icons.Draw(new Rect(cx - cs * 0.5f, y, cs, cs), "couronne", new Color(1f, 0.86f, 0.35f, a * 0.5f));
                Icons.Draw(new Rect(cx - cs * 0.3f, y + cs * 0.2f, cs * 0.6f, cs * 0.6f), "croix", new Color(1f, 0.4f, 0.35f, a));
            }
            y += UiStyle.S(190);

            // Le classement : une pastille par joueur, la premiere plus grande.
            List<PlayerSlot> order = new List<PlayerSlot>(Match.Slots);
            order.Sort((p, q) => q.Wins.CompareTo(p.Wins));
            float w = UiStyle.S(420);
            for (int i = 0; i < order.Count; i++)
            {
                PlayerSlot s = order[i];
                float h = UiStyle.S(i == 0 ? 50 : 40);
                Rect r = new Rect(cx - w * 0.5f, y, w, h);
                if (s.IsLocal) Icons.Pill(new Rect(r.x - UiStyle.S(4), r.y - UiStyle.S(4), r.width + UiStyle.S(8), r.height + UiStyle.S(8)), Wings.Gold, a);
                Icons.Pill(r, s.Colour, a);
                Icons.Number(new Rect(r.x + h * 0.3f, r.y, h, h), (i + 1).ToString(), Mathf.RoundToInt(h * 0.56f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
                Icons.Number(new Rect(r.x + h * 1.4f, r.y, w - h * 3.4f, h), s.Name, Mathf.RoundToInt(h * 0.46f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleLeft);
                Icons.Draw(new Rect(r.xMax - h * 1.8f, r.y + h * 0.12f, h * 0.76f, h * 0.76f), "couronne", new Color(1f, 0.86f, 0.35f, a));
                Icons.Number(new Rect(r.xMax - h * 1.0f, r.y, h * 0.7f, h), s.Wins.ToString(), Mathf.RoundToInt(h * 0.56f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
                y += h + UiStyle.S(10);
            }
            y += UiStyle.S(20);

            // Ce que TU as fait : cinq pastilles, une icone et un chiffre chacune.
            string[] icons = { "sacre", "couronne", "pousser", "cible", "don" };
            int[] counts = { Stats.Delivered, Stats.CrownsTaken, Stats.Shoves, Stats.Casts, Stats.Shrines };
            float ps = UiStyle.S(44), pw = UiStyle.S(104), pg = UiStyle.S(12);
            float px = cx - (icons.Length * pw + (icons.Length - 1) * pg) * 0.5f;
            for (int i = 0; i < icons.Length; i++)
            {
                Rect r = new Rect(px + i * (pw + pg), y, pw, ps);
                Icons.Pill(r, new Color(0.14f, 0.16f, 0.36f), a);
                Icons.Draw(new Rect(r.x + ps * 0.12f, r.y + ps * 0.1f, ps * 0.8f, ps * 0.8f), icons[i], i < 2 ? new Color(1f, 0.86f, 0.35f, a) : new Color(1f, 1f, 1f, a));
                Icons.Number(new Rect(r.x + ps, r.y, pw - ps * 1.15f, ps), counts[i].ToString(), Mathf.RoundToInt(ps * 0.5f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
            }
            y += ps + UiStyle.S(34);

            if (stateTime > 1.5f)
            {
                float bw = UiStyle.S(320);
                for (int i = 0; i < EndItems.Length; i++)
                {
                    bool primary = i == 0;
                    float h = UiStyle.S(primary ? 48 : 38);
                    if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, h), EndItems[i], i, primary, a)) Activate(i);
                    y += h + UiStyle.S(4);
                }            }
        }

        // ------------------------------------------------------------------ les commandes

        /// <summary>
        /// LES COMMANDES, SANS UNE PHRASE (01/10 -- Martin : "comment tu veux qu'un joueur lise
        /// tout ca ? il a la flemme"). Trois colonnes -- BOUGER, TES POUVOIRS, VOLER -- et dans
        /// chacune, des pastilles : la touche, une fleche, ce qu'elle fait en icone. Ca se lit
        /// en deux secondes, comme l'ecran des touches de Fall Guys.
        /// ("#icone" a la place d'une touche : rien a appuyer, ca se fait tout seul.)
        /// </summary>
        static readonly string[] MoveKeys = { "ZQSD", "Maj", "Espace", "E" };
        static readonly string[] MoveIcons = { "joueur", "coureur", "haut", "couronne" };
        static readonly string[] MoveIcons2 = { null, null, null, "arbaleste" };
        static readonly string[] FlyKeys = { "#vue", "Espace", "#courant", "" };
        static readonly string[] FlyIcons = { "ailes", "ailes", "haut", "pique" };
        static readonly string[] FlyIcons2 = { null, "croix", null, "couronne" };

        void DrawControls()
        {
            Glide(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.015f, 0.05f, 0.82f));
            float col = UiStyle.S(270), row = UiStyle.S(66), gap = UiStyle.S(26);
            float total = col * 3f + gap * 2f;
            float x = Mathf.Max(Left, (Screen.width - total) * 0.5f);
            float y = Screen.height * 0.5f - UiStyle.S(260);
            Icons.Number(new Rect(x, y, UiStyle.S(600), UiStyle.S(56)), "COMMANDES", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
            y += UiStyle.S(84);

            string[] powerKeys = { AbilityInfo.Keys[0], AbilityInfo.PushKey, AbilityInfo.PushKey, "Tab" };
            string[] powerIcons = { "cible", "pousser", "pousser", "manches" };
            string[] powerIcons2 = { null, null, "couronne", null };
            FlyKeys[3] = AbilityInfo.PushKey;
            ControlColumn(x, y, col, row, "joueur", new Color(0.45f, 0.72f, 1f), MoveKeys, MoveIcons, MoveIcons2);
            ControlColumn(x + col + gap, y, col, row, "cible", new Color(1f, 0.5f, 0.42f), powerKeys, powerIcons, powerIcons2);
            ControlColumn(x + (col + gap) * 2f, y, col, row, "ailes", Wings.Gold, FlyKeys, FlyIcons, FlyIcons2);
            y += UiStyle.S(78) + row * 4f + UiStyle.S(10);

            // En bas : la pause, et ce panneau pendant la partie.
            float small = row * 0.86f;
            ControlRow(new Rect(x, y, col, small), "Échap", "reglages", null, Color.white);
            ControlRow(new Rect(x + col + gap, y, col, small), "F1", "commandes", null, Color.white);
            y += small + UiStyle.S(30);
            if (Entry(new Rect(x, y, UiStyle.S(300), UiStyle.S(48)), "Retour", 0, false, 1f)) { showControls = false; selected = 0; }
        }

        /// <summary>Une colonne : son icone en tete, dans un gros rond a sa couleur, puis ses lignes.</summary>
        static void ControlColumn(float x, float y, float w, float row, string head, Color tint, string[] keys, string[] icons, string[] icons2)
        {
            float hs = UiStyle.S(64);
            Rect hr = new Rect(x + (w - hs) * 0.5f, y, hs, hs);
            Icons.Pill(hr, new Color(tint.r * 0.55f, tint.g * 0.55f, tint.b * 0.6f));
            Icons.Draw(new Rect(hr.x + hs * 0.14f, hr.y + hs * 0.14f, hs * 0.72f, hs * 0.72f), head, Color.white);
            y += UiStyle.S(78);
            for (int i = 0; i < keys.Length; i++)
                ControlRow(new Rect(x, y + i * row, w, row - UiStyle.S(10)), keys[i], icons[i], icons2[i], tint);
        }

        /// <summary>Une ligne : la touche, une petite fleche, ce qu'elle fait (une ou deux icones).</summary>
        static void ControlRow(Rect r, string key, string icon, string icon2, Color tint)
        {
            Icons.Pill(r, new Color(0.12f + tint.r * 0.12f, 0.12f + tint.g * 0.12f, 0.24f + tint.b * 0.14f, 0.96f));
            float s = r.height;
            Rect k = new Rect(r.x + s * 0.72f, r.y + s * 0.1f, s * 0.8f, s * 0.8f);
            if (key.StartsWith("#")) Icons.Draw(new Rect(r.x + s * 0.5f, r.y + s * 0.12f, s * 0.76f, s * 0.76f), key.Substring(1), new Color(1f, 1f, 1f, 0.85f));
            else if (key.Length > 0) Icons.Key(k, key, 1f);
            Icons.Draw(new Rect(r.x + s * 1.95f, r.y + s * 0.33f, s * 0.34f, s * 0.34f), "jouer", new Color(1f, 1f, 1f, 0.5f), false);
            Icons.Draw(new Rect(r.xMax - s * 0.98f, r.y + s * 0.1f, s * 0.8f, s * 0.8f), icon, tint);
            if (icon2 != null)
            {
                bool cross = icon2 == "croix";
                float c2 = cross ? s * 0.46f : s * 0.66f;
                Rect r2 = cross ? new Rect(r.xMax - s * 0.62f, r.y + s * 0.4f, c2, c2) : new Rect(r.xMax - s * 1.72f, r.y + s * 0.17f, c2, c2);
                Icons.Draw(r2, icon2, cross ? new Color(1f, 0.4f, 0.35f) : icon2 == "couronne" ? new Color(1f, 0.86f, 0.35f) : Color.white);
            }
        }
    }
}
