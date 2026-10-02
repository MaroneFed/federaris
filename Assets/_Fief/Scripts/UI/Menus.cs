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
        /// <summary>(04/10, en ligne) Apres un match en ligne (ou l'hote parti) : on rouvre l'ecran En ligne.</summary>
        static bool openOnline;

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
        /// <summary>
        /// Le temps qu'un bot met a choisir sa carte (02/10, gamer chiant : "entre deux manches,
        /// j'attends quinze secondes que sept bots choisissent" -- 0,9 s chacun, deux tours de
        /// table, plus quatre secondes a la fin). Assez pour voir la carte partir.
        /// </summary>
        const float BotPickDelay = 0.55f;

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
            curtain = 1f;       // on ouvre sur du noir, l'ile apparait en fondu
            if (Match.Launched) Go(State.Briefing);
            else Go(openOnline ? State.Online : openLobby ? State.Lobby : State.Title);
            openLobby = false;
            openOnline = false;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Go(State s)
        {
            // La voix de l'arene (Kenney) : "round 2", "final round", "tie breaker" a l'annonce
            // d'une manche ; "winner" ou "game over" au podium.
            if (s == State.Briefing && Current != State.Briefing)
                Sfx.Announce(Match.IsTieBreak ? "tie_breaker" : Match.RoundNumber >= Match.Rounds && Match.Rounds > 1 ? "final_round" : "round_" + Match.RoundNumber);
            if (s == State.Ended && Current != State.Ended)
                Sfx.Announce(Match.Champion != null && Match.Local != null && Match.Champion == Match.Local ? "winner" : "game_over");
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

            // (04/10, en ligne) L'invite suit l'hote ; l'hote dit ou il en est.
            if (NetSession.Link != null && !NetSession.Link.IsHost) FollowHost();
            NetGame.LocalPhase = PhaseNow();

            Season season = Game.Season;
            if (Current == State.Playing && season != null)
            {
                WarnOfTime(season);
                // Le temps est ecoule : PERSONNE ne gagne (01/10 -- Martin : "la victoire, il ne
                // faut pas la donner s'il a la couronne a la fin"). On gagne au Monument, point.
                // (En ligne, c'est l'hote qui le dit.)
                if (season.Over && !NetGame.IsClient) EndRound(-1);
            }
            if (Current == State.Briefing && stateTime > BriefingLength && !leaving) Enter();
            if (Current == State.Draft) TickDraft(dt);
            if (Current == State.Playing) TickCountdown(dt);
            if (goFlash > 0f) goFlash = Mathf.Max(0f, goFlash - dt * 1.2f);
            if (Current == State.RoundOver && stateTime > 9f && !leaving && !NetGame.IsClient) AfterRound();

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
                           // (02/10 -- "il n'y a meme plus l'animation quand on gagne") : plus de voile
                           // noir sur la fin de manche -- il recouvrait la danse du vainqueur a 86 % des
                           // 1,3 s. Les deux bandeaux de DrawRoundOver suffisent a lire les mots.
                           : Current == State.RoundOver ? (roundWinner >= 0 ? 0f : 0.35f)
                           // (03/10) Le podium du match ne se joue plus dans le noir : un voile
                           // leger, on voit le champion danser derriere le classement.
                           : Current == State.Ended ? (roundWinner >= 0 ? 0.3f : 0.6f)
                           : Current == State.Draft ? 0.86f : 0f;
            // On entre dans le voile doucement (le choix des cartes ne tombe plus comme un rideau).
            veil = Mathf.MoveTowards(veil, wantVeil, dt * (wantVeil > veil ? 1.4f : 3f));
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
        /// (03/10 -- "la musique, elle continue, c'est vraiment bizarre") : la danse, c'est la fin
        /// de manche SEULEMENT. Au choix des cartes, la musique du menu revient ; au podium du
        /// match, sa musique de fin -- et le champion, lui, danse encore (Celebrating).
        public bool Dancing { get { return Current == State.RoundOver && roundWinner >= 0 && WinnerBody() != null; } }

        /// <summary>Vrai tant qu'on filme le vainqueur : fin de manche, et podium du match.</summary>
        public bool Celebrating { get { return (Current == State.RoundOver || Current == State.Ended) && roundWinner >= 0 && WinnerBody() != null; } }

        /// <summary>
        /// LA FETE DU VAINQUEUR (01/10 -- Martin : "quand je gagne une manche, un effet, avec
        /// notre perso qui danse avec la musique") : il DANSE sur la musique (CharacterRig,
        /// un pas par temps), la Couronne flotte au-dessus de sa tete, et autour de lui, sur
        /// le rythme : des confettis, des feux d'artifice, un anneau d'or au sol, un
        /// projecteur (VictoryShow).
        /// </summary>
        /// <summary>Les perdants s'affaissent pendant que le gagnant danse (CharacterRig.Disappointed).</summary>
        static void Disappoint(Seeker s)
        {
            if (s == null || s.Body == null) return;
            CharacterRig rig = s.IsPlayer ? Game.Rig : null;
            if (!s.IsPlayer)
            {
                Rival r = Rival.Of(s);
                if (r != null) rig = r.Rig;
            }
            if (rig != null) rig.Disappointed(90f);
        }

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
            // La fenetre du pseudo : on tape ; Entree valide, Echap annule.
            if (editingPseudo)
            {
                if (FiefInput.CancelPressed) { editingPseudo = false; Sfx.Pop(); }
                else if (FiefInput.ConfirmPressed) ValidatePseudo();
                return;
            }
            if (FiefInput.CancelPressed)
            {
                confirmAbandon = false;
                if (showSettings) { showSettings = false; selected = Current == State.Title ? 2 : 1; }
                else if (showControls) { showControls = false; selected = 0; }
                else if (Current == State.Online && NetSession.Link != null) NetSession.Leave();
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
                ControlsKeyboard();
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
                    // Deux rangees (plus de six cartes) : haut et bas changent de rangee.
                    int perRow = DraftPerRow(cards);
                    if (cards > perRow && (FiefInput.UpPressed || FiefInput.DownPressed))
                    {
                        selected = Mathf.Clamp(selected < perRow ? selected + perRow : selected - perRow, 0, cards - 1);
                        Sfx.Pop();
                    }
                    if (FiefInput.ConfirmPressed) PickCard(selected);
                }
                else if (Match.Draft.Done && FiefInput.ConfirmPressed && !NetGame.IsClient) FinishDraft();
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
            if (Current == State.Online && HostingSalon && selected < OnlineRows)
            {
                if (FiefInput.LeftPressed) AdjustOnline(selected, -1);
                if (FiefInput.RightPressed) AdjustOnline(selected, 1);
            }
            // (04/10) Entree en tapant l'adresse : on rejoint (pas "Heberger", vise par defaut).
            if (FiefInput.ConfirmPressed && Current == State.Online && addressFocused && NetSession.Link == null) { Activate(1); return; }
            if (FiefInput.ConfirmPressed) Activate(selected);
        }

        bool addressFocused;

        /// <summary>Combien d'entrees dans l'ecran courant.</summary>
        int Entries()
        {
            switch (Current)
            {
                case State.Title: return TitleItems.Length;
                case State.Lobby: return LobbyRows + 2;
                case State.Online: return NetSession.Link == null ? 3 : HostingSalon ? OnlineRows + 2 : 1;
                case State.Paused: return PauseItems.Length;
                case State.RoundOver: return stateTime > 3f ? 1 : 0;
                case State.Ended: return stateTime > 1.5f ? EndItems.Length : 0;
            }
            return 0;
        }

        static readonly string[] TitleItems = { "Jouer", "En ligne", "Réglages", "Commandes", "Quitter" };
        static readonly string[] PauseItems = { "Reprendre", "Réglages", "Commandes", "Abandonner le match", "Quitter le jeu" };
        static readonly string[] EndItems = { "Nouveau match", "Quitter" };
        static readonly string[] ClientEndItems = { "Quitter le salon", "Quitter" };

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
                    // (04/10, le jeu en ligne, etape 1) Heberger, Rejoindre (l'adresse tapee au-dessus),
                    // Retour ; une fois dans un salon : le quitter.
                    if (HostingSalon)
                    {
                        if (i < OnlineRows) AdjustOnline(i, 1);
                        else if (i == OnlineRows) StartOnlineMatch();
                        else NetSession.Leave();
                        break;
                    }
                    if (NetSession.Link != null) { NetSession.Leave(); break; }
                    if (i == 0) NetSession.Host();
                    else if (i == 1) NetSession.Join(joinAddress);
                    else Go(State.Title);
                    break;
                case State.Paused:
                    if (i == 0) Resume();
                    else if (i == 1) { showSettings = true; selected = 0; }
                    else if (i == 2) { showControls = true; selected = 0; }
                    else if (i == 3)
                    {
                        if (!confirmAbandon) { confirmAbandon = true; break; }
                        confirmAbandon = false;
                        LeaveOnlineMatch();
                        Match.Abandon();
                        Curtain(Reload);
                    }
                    else Quit();
                    break;
                case State.RoundOver:
                    if (!NetGame.IsClient) AfterRound();
                    break;
                case State.Ended:
                    if (i == 0 && Match.Online) { LeaveOnlineMatch(); Match.Abandon(); Curtain(Reload); }
                    else if (i == 0) { Match.Abandon(); openLobby = true; Curtain(Reload); }
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
            if (n != lastCount && n > 0) { lastCount = n; if (!Sfx.Announce(n.ToString())) Sfx.Beep(1f); }
            if (countdown > 0f) return;
            countdown = 0f;
            goFlash = 1f;
            if (Game.Season != null) Game.Season.Begin();
            // Trois secondes de protection au depart : on quitte sa zone sans se faire
            // pousser ni tirer dessus avant d'avoir fait un pas.
            for (int i = 0; i < Game.Seekers.Count; i++) Game.Seekers[i].GraceUntil = Time.time + 3f;
            if (!Sfx.Announce("fight")) Sfx.Bell();
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
            if (Current != State.Playing && Current != State.Paused && !(NetGame.IsClient && Current == State.Briefing)) return;
            if (Game.Season != null) { roundTime = Game.Season.Elapsed; Game.Season.Stop(); }
            // (30/09 : plus de ralenti -- la fete du vainqueur se joue a vitesse normale.)
            Time.timeScale = 1f;
            slowMotion = 0f;
            if (!NetGame.IsClient && Match.IsTieBreak && winner >= 0 && !Match.TieBreakers.Contains(winner)) winner = -1;
            roundWinner = winner;
            if (winner >= 0 && Match.Local != null && winner == Match.Local.Index) Stats.Delivered++;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                if (Game.Seekers[i].Index == winner) Celebrate(Game.Seekers[i]);
                else if (winner >= 0) Disappoint(Game.Seekers[i]);
            }
            // La voix de l'arene : "you win", "you lose" -- ou "time" quand personne n'a gagne.
            // ("you lose" seulement a la derniere manche : l'entendre sept fois de suite, c'est dur.)
            bool mine = Match.Local != null && winner == Match.Local.Index;
            bool last = (NetGame.IsClient ? Match.Played : Match.Played + 1) >= Match.Rounds;
            if (winner < 0) Sfx.Announce("time");
            else if (mine) Sfx.Announce("you_win");
            else if (last) Sfx.Announce("you_lose");
            // (En ligne, l'invite a deja recopie le score de l'hote.)
            if (!NetGame.IsClient) Match.EndRound(winner);
            Go(State.RoundOver);
            Sfx.Bell();
            // La foule exulte (une manche gagnee) -- rien quand le temps s'est ecoule.
            if (winner >= 0) Sfx.Crowd(1f);
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.4f);
        }

        /// <summary>Apres la fin de manche : le podium, ou le choix des capacites, ou la manche suivante.</summary>
        void AfterRound()
        {
            if (Current != State.RoundOver || stateTime < 3f) return;
            if (Match.Over) { Go(State.Ended); return; }
            Match.Draft.Prepare();
            if (Match.Draft.Done) { NextRound(); return; }
            botPickTimer = 1.2f;
            Go(State.Draft);
        }

        void NextRound()
        {
            // En ligne : une nouvelle ile -- les invites chargent la meme (NetGame).
            if (NetGame.IsHost) NetGame.HostNewRound();
            Curtain(Reload);
        }

        /// <summary>Le choix est fini : la manche suivante (ou la premiere : le match part).</summary>
        void FinishDraft()
        {
            if (leaving || NetGame.IsClient) return;
            // Apres la passive, le tour des actives (a chaque manche).
            if (Match.Draft.SecondStageNext)
            {
                Match.Draft.PrepareSecondStage();
                botPickTimer = BotPickDelay;
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
            // (04/10, en ligne) L'invite dit son choix a l'hote, qui le pose (et le dit a tous).
            if (NetGame.IsClient)
            {
                NetGame.SendPick(me, card);
                if (card < cardRects.Length) CardArt.Taken(cardRects[card], AbilityInfo.Tint(chosen), true);
                Sfx.CardPick();
                return;
            }
            if (Match.Draft.TryPick(me, card))
            {
                if (card < cardRects.Length) CardArt.Taken(cardRects[card], AbilityInfo.Tint(chosen), true);
                Sfx.CardPick();
                botPickTimer = BotPickDelay;
                selected = 0;
            }
        }

        void TickDraft(float dt)
        {
            // (04/10, en ligne) L'invite ne fait que regarder : l'hote fait choisir les bots.
            if (NetGame.IsClient) return;
            int turnKey = Match.Draft.Stage * 100 + Match.Draft.Turn;
            if (turnKey != draftTurnKey)
            {
                draftTurnKey = turnKey;
                draftWait = 0f;
                if (botPickTimer < BotPickDelay) botPickTimer = BotPickDelay;
            }
            if (Match.Draft.Done)
            {
                // Tout le monde a choisi : on part tout seul apres un temps de lecture.
                botPickTimer -= dt;
                if (botPickTimer < -2.5f) FinishDraft();
                return;
            }
            int slot = Match.Draft.Current;
            if (slot < 0 || slot >= Match.Slots.Count) return;
            // Un ami en ligne choisit chez lui : on l'attend (30 s au plus, puis on choisit pour lui).
            if (Match.Slots[slot].IsRemote && !Match.Slots[slot].IsBot)
            {
                draftWait += dt;
                if (draftWait < 30f) return;
            }
            else if (!Match.Slots[slot].IsBot) return;
            botPickTimer -= dt;
            if (botPickTimer > 0f) return;
            botPickTimer = BotPickDelay;
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

        int draftTurnKey = -1;
        float draftWait;

        // ================================================================== en ligne (04/10)

        /// <summary>Vrai dans le salon en ligne, quand c'est nous qui hebergeons (et que le match n'a pas commence).</summary>
        bool HostingSalon { get { return NetSession.Link != null && NetSession.Link.IsHost && !Match.Online; } }

        /// <summary>Le salon en ligne : les bots, leur niveau, les manches, la duree.</summary>
        const int OnlineRows = 4;
        int onlineBots;

        void AdjustOnline(int row, int step)
        {
            int humans = NetSession.Link != null ? NetSession.Link.Roster.Count : 1;
            if (row == 0) onlineBots = Mathf.Clamp(onlineBots + step, 0, Match.MaxPlayers - humans);
            else if (row == 1) Match.BotLevel = Mathf.Clamp(Match.BotLevel + step, 0, Match.BotLevels.Length - 1);
            else if (row == 2) lobbyRounds = Cycle(Match.RoundChoices, lobbyRounds, step);
            else if (row == 3) lobbyMinutes = Cycle(Match.MinuteChoices, lobbyMinutes, step);
            else return;
            Sfx.Pop();
        }

        /// <summary>
        /// L'HOTE LANCE LE MATCH EN LIGNE (04/10 -- "on peut rien faire, on est juste dans le truc") :
        /// les joueurs du salon et les bots choisis, puis le choix des cartes -- chacun choisit chez
        /// lui, a son tour. Les invites suivent tout seuls (FollowHost).
        /// </summary>
        void StartOnlineMatch()
        {
            if (!NetGame.HostBegin(onlineBots, lobbyRounds, lobbyMinutes)) { Sfx.Deny(); return; }
            Stats.Reset();
            Match.Draft.Prepare();
            botPickTimer = 0.8f;
            draftTurnKey = -1;
            Go(State.Draft);
        }

        /// <summary>On quitte un match en ligne : l'hote rouvre le salon (les invites y reviennent), l'invite s'en va.</summary>
        static void LeaveOnlineMatch()
        {
            if (!Match.Online) return;
            if (NetSession.Link != null && NetSession.Link.IsHost) NetGame.HostReopen();
            else NetSession.Leave();
            openOnline = true;
        }

        /// <summary>Ou en est le match, pour l'envoyer aux invites.</summary>
        NetGame.Phase PhaseNow()
        {
            if (!Match.Online) return NetGame.Phase.Lobby;
            switch (Current)
            {
                case State.Draft: return NetGame.Phase.Draft;
                case State.RoundOver: return NetGame.Phase.RoundOver;
                case State.Ended: return NetGame.Phase.Ended;
            }
            return Match.Launched ? NetGame.Phase.Round : NetGame.Phase.Draft;
        }

        int followDraftKey = -1;

        /// <summary>
        /// L'INVITE SUIT L'HOTE (04/10) : il charge la meme ile quand l'hote en charge une, il passe
        /// a la fin de manche, au choix des cartes, au podium en meme temps que lui. Si l'hote part,
        /// retour a l'ecran En ligne.
        /// </summary>
        void FollowHost()
        {
            if (leaving) return;
            if (NetGame.HostLost)
            {
                bool inMatch = Match.Online;
                Fief.Net.NetLink link = NetSession.Link;
                // Dire POURQUOI (la cause n°1 : pas la meme version du jeu des deux cotes).
                string why = "L'hôte a quitté la partie.";
                if (link.Status == Fief.Net.NetLink.State.Refused)
                    why = link.RefusedFor == Fief.Net.NetLink.Refusal.BadVersion ? "Pas la même version du jeu que l'hôte : refaites le Build tous les deux."
                        : link.RefusedFor == Fief.Net.NetLink.Refusal.Full ? "Le salon est plein."
                        : "Le match a déjà commencé.";
                else if (link.Roster.Count == 0) why = "Pas de réponse de l'hôte : vérifie l'adresse, et que son pare-feu autorise le jeu.";
                NetSession.Leave();
                NetSession.Report(why);
                if (inMatch || Current != State.Online) { openOnline = true; Curtain(Reload); }
                return;
            }
            if (!Match.Online) return;      // le salon : on attend que l'hote lance
            NetGame.Phase phase = NetGame.HostPhase;
            if (phase == NetGame.Phase.Lobby)
            {
                // L'hote a rouvert le salon (nouveau match, abandon) : on y retourne.
                Match.Abandon();
                openOnline = true;
                Curtain(Reload);
                return;
            }
            // L'hote charge une nouvelle ile : on charge la meme (meme graine, meme manche).
            if (NetGame.HostLaunched && NetGame.HostToken != NetGame.RoundToken)
            {
                NetGame.RoundToken = NetGame.HostToken;
                Match.Launch();
                Curtain(Reload);
                return;
            }
            bool inRound = Current == State.Briefing || Current == State.Playing || Current == State.Paused;
            if ((phase == NetGame.Phase.RoundOver || phase == NetGame.Phase.Ended) && inRound) { ClientRoundOver(); return; }
            if (phase == NetGame.Phase.Ended && Current == State.RoundOver && stateTime > 3f) { Go(State.Ended); return; }
            int key = Match.Draft.Stage + Match.Played * 10;
            if (phase == NetGame.Phase.Draft && !inRound && (Current != State.Draft || key != followDraftKey) && (Current != State.RoundOver || stateTime > 3f))
            {
                followDraftKey = key;
                Go(State.Draft);
            }
        }

        /// <summary>L'hote a dit "fin de manche" : chez l'invite, le meme vainqueur, la meme fete.</summary>
        void ClientRoundOver()
        {
            int w = Match.LastWinner;
            Seeker ws = w >= 0 ? Game.SeekerOf(w) : null;
            if (ws != null) Monument.MirrorWinner(ws);
            EndRound(w);
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
            // En ligne, le monde ne s'arrete pas pour un seul joueur.
            Time.timeScale = Match.Online ? 1f : 0f;
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

            // (02/10) Un ecran qui plante ne laisse plus la couleur ou le zoom de travers (sinon
            // "tout se barre") : on remet tout d'aplomb, et l'erreur va une fois dans la Console.
            try
            {
                if (editingPseudo) DrawPseudoEditor();
                else if (showSettings) DrawSettings();
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
            }
            catch (System.Exception e)
            {
                if (!menuFailed) { menuFailed = true; Debug.LogError("[FIEF] Menus : l'ecran " + Current + " a plante -- " + e); }
            }
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;

            // Le rideau passe par-dessus tout, y compris le texte.
            if (curtain > 0.001f) UiStyle.Fill(screen, new Color(0f, 0f, 0f, curtain));
        }

        bool hoverFollows;
        bool menuFailed;

        // ------------------------------------------------------------------ outils de dessin

        /// <summary>
        /// UNE ENTREE : un mot. Choisie, elle passe en or, se decale un peu et un trait
        /// fin se dessine dessous. Rien d'autre. Vrai si on a clique dessus.
        /// </summary>
        bool Entry(Rect r, string text, int index, bool primary, float alpha)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover && hoverFollows && selected != index) { selected = index; Sfx.Hover(); }
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
            if (text.StartsWith("En ligne") || text.StartsWith("Héberger")) return "en-ligne";
            if (text.StartsWith("Rejoindre")) return "joueur";
            if (text.StartsWith("Quitter le salon")) return "retour";
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
            if (text.StartsWith("Volume") || text.StartsWith("Musique")) return "volume";
            if (text.StartsWith("Champ")) return "vue";
            if (text.StartsWith("Taille")) return "texte";
            if (text.StartsWith("Aide")) return "commandes";
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

            PseudoBadge(late);

            // La version, en bas a droite : c'est elle qui dit quel code tourne.
            UiStyle.Tinted(new Rect(0f, Screen.height - UiStyle.S(30), Screen.width - UiStyle.S(24), UiStyle.S(20)), Game.Version,
                           Style(UiStyle.Tiny, 0, TextAnchor.MiddleRight), new Color(UiStyle.InkFaint.r, UiStyle.InkFaint.g, UiStyle.InkFaint.b, late));
        }

        // ------------------------------------------------------------------ le pseudo

        // (05/10 -- Martin : "j'aimerais bien qu'on puisse mieux modifier notre pseudo") : avant,
        // il etait cache au fond des Reglages, une ligne parmi d'autres. Desormais il est EN HAUT
        // A DROITE de l'ecran-titre, du salon et de l'ecran En ligne : un clic, une grande
        // fenetre, on tape, Entree.
        bool editingPseudo;
        string pseudoDraft;
        GUIStyle pseudoEditStyle;

        /// <summary>Ton pseudo, en haut a droite : cliquer dessus pour le changer.</summary>
        void PseudoBadge(float alpha)
        {
            if (alpha < 0.05f) return;
            Settings.Load();
            string name = Settings.Shown;
            int fs = Mathf.RoundToInt(UiStyle.S(24));
            float h = Mathf.Round(UiStyle.S(52));
            float w = Mathf.Round(Icons.Width(name, fs) + h * 2.3f);
            Rect r = new Rect(Mathf.Round(Screen.width - w - UiStyle.S(28)), Mathf.Round(UiStyle.S(26)), w, h);
            bool hover = r.Contains(Event.current.mousePosition);
            Icons.Pill(r, hover ? new Color(0.35f, 0.4f, 0.9f, alpha) : new Color(0.16f, 0.18f, 0.42f, alpha));
            Icons.Draw(new Rect(r.x + h * 0.18f, r.y + h * 0.16f, h * 0.68f, h * 0.68f), "pseudo", new Color(1f, 1f, 1f, alpha));
            Icons.Text(new Rect(r.x + h, r.y, Icons.Width(name, fs) + UiStyle.S(4), h), name, fs, new Color(1f, 0.86f, 0.4f, alpha), TextAnchor.MiddleLeft, true);
            Icons.Draw(new Rect(r.xMax - h * 0.95f, r.y + h * 0.22f, h * 0.56f, h * 0.56f), "reglages", new Color(1f, 1f, 1f, 0.8f * alpha));
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) OpenPseudo();
        }

        void OpenPseudo()
        {
            Settings.Load();
            pseudoDraft = Settings.Pseudo;
            editingPseudo = true;
            Sfx.Pop();
        }

        void ValidatePseudo()
        {
            string clean = (pseudoDraft ?? "").Trim();
            if (clean.Length == 0) { Sfx.Deny(); return; }
            Settings.SetPseudo(clean);
            editingPseudo = false;
            Sfx.CardPick();
        }

        /// <summary>LA FENETRE DU PSEUDO : un grand champ ou l'on tape (16 lettres), Valider, Annuler.</summary>
        void DrawPseudoEditor()
        {
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            UiStyle.Fill(screen, new Color(0f, 0f, 0f, 0.55f));
            float w = Mathf.Round(Mathf.Min(UiStyle.S(680), Screen.width - UiStyle.S(40)));
            float h = Mathf.Round(UiStyle.S(330));
            Rect panel = new Rect(Mathf.Round((Screen.width - w) * 0.5f), Mathf.Round((Screen.height - h) * 0.5f), w, h);
            Icons.Pill(panel, new Color(0.12f, 0.14f, 0.32f, 0.97f));
            float y = panel.y + UiStyle.S(26);
            Icons.Number(new Rect(panel.x, y, w, UiStyle.S(50)), "TON PSEUDO", UiStyle.S(40), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleCenter);
            y += UiStyle.S(70);
            if (pseudoEditStyle == null || pseudoEditStyle.fontSize != Mathf.RoundToInt(UiStyle.S(40)))
            {
                pseudoEditStyle = new GUIStyle(UiStyle.Head);
                pseudoEditStyle.fontSize = Mathf.RoundToInt(UiStyle.S(40));
                pseudoEditStyle.alignment = TextAnchor.MiddleCenter;
                pseudoEditStyle.normal.textColor = Color.white;
                pseudoEditStyle.focused.textColor = Color.white;
            }
            Rect field = new Rect(panel.x + UiStyle.S(40), y, w - UiStyle.S(80), UiStyle.S(70));
            Icons.Pill(field, new Color(0.3f, 0.34f, 0.72f));
            GUI.SetNextControlName("pseudoEdit");
            pseudoDraft = GUI.TextField(field, pseudoDraft ?? "", Settings.PseudoLength, pseudoEditStyle);
            if (GUI.GetNameOfFocusedControl() != "pseudoEdit") GUI.FocusControl("pseudoEdit");
            y += UiStyle.S(80);
            string count = (pseudoDraft ?? "").Length + " / " + Settings.PseudoLength;
            Icons.Text(new Rect(panel.x, y, w, UiStyle.S(26)), count, Mathf.RoundToInt(UiStyle.S(18)), new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, true);
            y += UiStyle.S(40);
            float bw = Mathf.Round((w - UiStyle.S(100)) * 0.5f);
            if (Entry(new Rect(panel.x + UiStyle.S(40), y, bw, UiStyle.S(50)), "Valider", 0, true, 1f)) ValidatePseudo();
            if (Entry(new Rect(panel.xMax - UiStyle.S(40) - bw, y, bw, UiStyle.S(50)), "Annuler", 1, false, 1f)) { editingPseudo = false; Sfx.Pop(); }
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
            // Centre sur sa vraie hauteur (titre, quatre lignes, les pseudos, le chrono, les boutons).
            float height = UiStyle.S(80) + LobbyRows * UiStyle.S(58) + UiStyle.S(38) * (lobbyBots > 5 ? 2 : 1) + UiStyle.S(64) + UiStyle.S(56) + UiStyle.S(38);
            float y = Mathf.Round(Mathf.Max(UiStyle.S(20), (Screen.height - height) * 0.5f));
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
            PseudoBadge(1f);
        }

        /// <summary>Une ligne a regler : "Joueurs   ‹ 4 ›". Les fleches se cliquent ; au clavier, gauche/droite.</summary>
        void ValueRow(float x, float y, string label, string value, int index, System.Action<int> adjust)
        {
            Rect row = new Rect(x, y, UiStyle.S(300), UiStyle.S(48));
            Entry(row, label, index, false, 1f);
            bool on = selected == index;
            // La valeur, dans sa pastille, entre deux fleches.
            float vx = x + UiStyle.S(316);
            // (02/10) Des pixels entiers et une largeur paire : la fleche "moins" est la meme
            // que "plus", retournee autour de son milieu -- un milieu entre deux pixels la floutait.
            int side = UiStyle.S(36) / 2 * 2;
            Rect minus = new Rect(Mathf.Round(vx), Mathf.Round(y + UiStyle.S(6)), side, side);
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
            // (02/10 -- "Reglages, c'est beaucoup trop en bas") : centre sur sa VRAIE hauteur
            // (le titre, les reglages, le pseudo, Retour) -- avant, il partait du milieu de
            // l'ecran moins 170 px, pour 650 px de haut : le bas sortait de l'ecran.
            float height = UiStyle.S(80) + Settings.Labels.Length * UiStyle.S(56) + UiStyle.S(56) + UiStyle.S(24) + UiStyle.S(40);
            float y = Mathf.Round(Mathf.Max(UiStyle.S(20), (Screen.height - height) * 0.5f));
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
        string joinAddress;
        GUIStyle addressStyle;

        /// <summary>
        /// EN LIGNE (04/10 -- Martin : "on peut faire le en ligne"). ETAPE 1 : heberger une
        /// partie, la rejoindre par son adresse, et voir le meme SALON (les pseudos de tous, leur
        /// ping). Lancer le match ensemble, c'est l'etape 2 (docs/RESEAU.md).
        /// </summary>
        void DrawOnline()
        {
            Glide(new Rect(0f, 0f, Mathf.Max(UiStyle.S(760), Screen.width * 0.55f), Screen.height), new Color(0.02f, 0.015f, 0.03f, 0.8f));
            float x = Left;
            float y = Screen.height * 0.5f - UiStyle.S(220);
            // Le salon de l'hote est plus haut (les reglages et Lancer) : centre sur sa vraie hauteur.
            if (HostingSalon) y = Mathf.Round(Mathf.Max(UiStyle.S(24), (Screen.height - UiStyle.S(510 + 52 * NetSession.Link.Roster.Count)) * 0.5f));
            Icons.Number(new Rect(x, y, UiStyle.S(600), UiStyle.S(56)), "EN LIGNE", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
            y += UiStyle.S(74);
            Fief.Net.NetLink link = NetSession.Link;
            float bw = UiStyle.S(420), bh = UiStyle.S(46);

            if (link == null)
            {
                if (joinAddress == null) joinAddress = NetSession.LastAddress;
                if (Entry(new Rect(x, y, bw, bh), "Héberger", 0, true, 1f)) Activate(0);
                y += bh + UiStyle.S(16);
                if (Entry(new Rect(x, y, bw, bh), "Rejoindre", 1, true, 1f)) Activate(1);
                // L'adresse de l'hote, a taper (elle est gardee d'une fois sur l'autre).
                if (addressStyle == null)
                {
                    addressStyle = new GUIStyle(GUI.skin.textField);
                    addressStyle.alignment = TextAnchor.MiddleCenter;
                }
                addressStyle.fontSize = Mathf.RoundToInt(UiStyle.S(24));
                Rect field = new Rect(x + bw + UiStyle.S(14), y + UiStyle.S(4), UiStyle.S(260), bh - UiStyle.S(8));
                Icons.Pill(new Rect(field.x - UiStyle.S(6), field.y - UiStyle.S(4), field.width + UiStyle.S(12), field.height + UiStyle.S(8)), new Color(0.14f, 0.16f, 0.36f));
                GUI.SetNextControlName("adresse");
                joinAddress = GUI.TextField(field, joinAddress ?? "", 64, addressStyle);
                addressFocused = GUI.GetNameOfFocusedControl() == "adresse";
                y += bh + UiStyle.S(16);
                if (Entry(new Rect(x, y, bw, bh), "Retour", 2, false, 1f)) Activate(2);
                PseudoBadge(1f);
                y += bh + UiStyle.S(24);
                if (!string.IsNullOrEmpty(NetSession.Problem))
                    Icons.Text(new Rect(x, y, UiStyle.S(700), UiStyle.S(40)), NetSession.Problem, Mathf.RoundToInt(UiStyle.S(22)), new Color(1f, 0.5f, 0.42f), TextAnchor.MiddleLeft, true);
                return;
            }

            // --- dans un salon : l'etat (icones), l'adresse a donner si l'on heberge, les joueurs.
            float s = UiStyle.S(64);
            Rect state = new Rect(x, y, s, s);
            bool ok = link.Status == Fief.Net.NetLink.State.Hosting || link.Status == Fief.Net.NetLink.State.Connected;
            bool waiting = link.Status == Fief.Net.NetLink.State.Connecting;
            Icons.Pill(state, ok ? new Color(0.2f, 0.62f, 0.32f) : waiting ? new Color(0.3f, 0.36f, 0.7f) : new Color(0.72f, 0.2f, 0.2f));
            Icons.Draw(new Rect(state.x + s * 0.15f, state.y + s * 0.15f, s * 0.7f, s * 0.7f), ok ? "coche" : waiting ? "chrono" : "croix", Color.white);
            if (link.IsHost)
            {
                // L'adresse a donner a l'ami (sur le meme reseau : la box, le wifi de la maison).
                string all = string.Join("   ", Fief.Net.NetLink.LocalAddresses().ToArray());
                Icons.Text(new Rect(state.xMax + UiStyle.S(16), y, UiStyle.S(700), s), all, Mathf.RoundToInt(UiStyle.S(30)), Color.white, TextAnchor.MiddleLeft, true);
            }
            else if (link.Status == Fief.Net.NetLink.State.Connected)
                Icons.Number(new Rect(state.xMax + UiStyle.S(16), y, UiStyle.S(300), s), Mathf.RoundToInt(link.PingToHost) + " ms", Mathf.RoundToInt(UiStyle.S(30)), Color.white, TextAnchor.MiddleLeft);
            y += s + UiStyle.S(24);

            // Les joueurs du salon, une pastille chacun, a la couleur de sa place.
            float pw = UiStyle.S(420), ph = UiStyle.S(44);
            for (int i = 0; i < link.Roster.Count; i++)
            {
                Fief.Net.NetLink.Member m = link.Roster[i];
                Rect r = new Rect(x, y, pw, ph);
                if (m.Slot == link.MySlot) Icons.Pill(new Rect(r.x - UiStyle.S(4), r.y - UiStyle.S(4), r.width + UiStyle.S(8), r.height + UiStyle.S(8)), Wings.Gold);
                Icons.Pill(r, Match.ColourOf(m.Slot));
                Icons.Draw(new Rect(r.x + ph * 0.18f, r.y + ph * 0.14f, ph * 0.72f, ph * 0.72f), m.Slot == 0 ? "couronne" : "joueur", Color.white);
                Icons.Number(new Rect(r.x + ph * 1.1f, r.y, pw - ph * 2.6f, ph), m.Name, Mathf.RoundToInt(ph * 0.46f), Color.white, TextAnchor.MiddleLeft);
                if (m.Slot != 0 && m.Ping > 0f)
                    Icons.Number(new Rect(r.xMax - ph * 1.6f, r.y, ph * 1.5f, ph), Mathf.RoundToInt(m.Ping) + "", Mathf.RoundToInt(ph * 0.4f), new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
                y += ph + UiStyle.S(8);
            }
            y += UiStyle.S(18);
            if (HostingSalon)
            {
                // (04/10, etape 2) L'HOTE REGLE ET LANCE : les bots en plus des amis, leur niveau,
                // les manches, la duree. Puis "Lancer" : tout le monde passe au choix des cartes.
                string[] labels = { "Bots", "Niveau", "Manches", "Durée max" };
                string[] values = { onlineBots.ToString(), Match.BotLevels[Match.BotLevel], lobbyRounds.ToString(), lobbyMinutes + " min" };
                for (int i = 0; i < OnlineRows; i++)
                {
                    int row = i;
                    ValueRow(x, y, labels[i], values[i], i, step => AdjustOnline(row, step));
                    y += UiStyle.S(54);
                }
                y += UiStyle.S(10);
                if (Entry(new Rect(x, y, bw, UiStyle.S(50)), "Lancer", OnlineRows, true, 1f)) Activate(OnlineRows);
                y += UiStyle.S(56);
                if (Entry(new Rect(x, y, bw, bh), "Quitter le salon", OnlineRows + 1, false, 1f)) Activate(OnlineRows + 1);
                return;
            }
            if (link.Status == Fief.Net.NetLink.State.Connected)
            {
                // L'invite attend que l'hote lance : un sablier qui bat.
                float ph2 = UiStyle.S(44);
                Rect wait = new Rect(x, y, UiStyle.S(420), ph2);
                Icons.Pill(wait, new Color(0.14f, 0.16f, 0.36f));
                float beat = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 6f);
                Icons.Draw(new Rect(wait.x + ph2 * 0.14f, wait.y + ph2 * 0.12f, ph2 * 0.76f * beat, ph2 * 0.76f * beat), "chrono", Color.white);
                Icons.Number(new Rect(wait.x + ph2, wait.y, wait.width - ph2 * 1.2f, ph2), "L'hôte va lancer", Mathf.RoundToInt(ph2 * 0.42f), Color.white, TextAnchor.MiddleCenter);
                y += ph2 + UiStyle.S(14);
            }
            if (Entry(new Rect(x, y, bw, bh), "Quitter le salon", 0, false, 1f)) Activate(0);
        }

        // ------------------------------------------------------------------ l'intro de manche

        float BriefingLength { get { return Match.Played == 0 && !Match.IsTieBreak ? (Settings.Help ? 9f : 7f) : 3.8f; } }

        /// <summary>Les mots sous la regle en icones (Reglages > Aide ecrite ; 02/10).</summary>
        static readonly string[] StepWords = { "MONTE LA TOUR", "PRENDS-LA", "PLANE", "COURANT D'AIR", "MONUMENT : 3 S" };

        /// <summary>Quelques mots centres (rien si l'aide ecrite est coupee).</summary>
        static void Words(float cx, float y, string words, int size, Color c)
        {
            if (!Settings.Help || c.a <= 0.01f) return;
            float w = Icons.Width(words, size) + UiStyle.S(16);
            Icons.Text(new Rect(cx - w * 0.5f, y, w, size * 1.5f), words, size, c, TextAnchor.MiddleCenter, true);
        }

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
                float s = UiStyle.S(92), gap = UiStyle.S(Settings.Help ? 84 : 56);
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
                    Words(r.center.x, r.yMax + UiStyle.S(12), StepWords[i], UiStyle.S(17), new Color(1f, 1f, 1f, k));
                }
                y += s + UiStyle.S(Settings.Help ? 62 : 30);
                float d = Mathf.Clamp01((t - 3.2f) / 0.4f) * a;
                float cx = Screen.width * 0.5f;
                Rect chip = new Rect(cx - s * 1.6f, y, s * 3.2f, s * 0.9f);
                Icons.Pill(chip, new Color(0.62f, 0.2f, 0.25f), d);
                Icons.Key(new Rect(chip.x + s * 0.12f, chip.y + s * 0.08f, s * 0.74f, s * 0.74f), AbilityInfo.PushKey, d);
                Icons.Draw(new Rect(chip.x + s * 1.0f, chip.y + s * 0.08f, s * 0.74f, s * 0.74f), "pousser", new Color(1f, 1f, 1f, d));
                Icons.Draw(new Rect(chip.x + s * 1.9f, chip.y + s * 0.08f, s * 0.74f, s * 0.74f), "couronne", new Color(1f, 0.86f, 0.35f, d));
                Words(cx, chip.yMax + UiStyle.S(10), "POUSSE CELUI QUI L'A : TU LA LUI VOLES", UiStyle.S(19), new Color(1f, 1f, 1f, d));
                y += s + UiStyle.S(Settings.Help ? 52 : 20);
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
            // (03/10 -- Martin : "quand on gagne, c'est tronque, t'as deux trucs noirs qui
            // arrivent") : plus AUCUN voile, ni en haut ni en bas. Les mots sont cernes (ils se
            // lisent sur le ciel), le score est dans ses pastilles. L'ecran entier est a la fete.
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

            if (stateTime > 3f)
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
            // (05/10, v26 -- "ouvre le choix beaucoup plus large") Jusqu'a onze cartes : au-dela
            // de six, deux rangees, pour qu'elles restent grandes et lisibles.
            int n2 = Mathf.Max(1, offer.Count);
            int perRow = DraftPerRow(n2);
            int rows = (n2 + perRow - 1) / perRow;
            float gap = UiStyle.S(18);
            float cw = Mathf.Min(UiStyle.S(250), (Screen.width - UiStyle.S(60) - gap * (perRow - 1)) / perRow);
            float ch = Mathf.Min(cw * 1.45f, Screen.height * 0.46f);
            if (rows > 1)
            {
                ch = Mathf.Min(ch, (Screen.height - y - UiStyle.S(190) - gap * (rows - 1)) / rows);
                cw = Mathf.Min(cw, ch / 1.1f);   // une carte reste une carte : plus haute que large
            }
            if (cardLift.Length < n2) cardLift = new float[n2];
            for (int i = 0; i < offer.Count; i++)
            {
                Ability p = offer[i];
                // Elles arrivent face cachee, puis se retournent une a une.
                float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((stateTime - 0.25f - i * 0.16f) / 0.5f));
                bool owned = Match.Local != null && Match.Local.Has(p);
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, n2 - row * perRow);
                float cx = (Screen.width - (cw * inRow + gap * (inRow - 1))) * 0.5f;
                Rect hit = new Rect(cx + col * (cw + gap), y + row * (ch + gap), cw, ch);
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
            y += ch * rows + gap * (rows - 1) + UiStyle.S(26);

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

        // (v26) Jusqu'a onze cartes (joueurs + 3) : de la place pour seize.
        float[] cardLift = new float[16];
        readonly Rect[] cardRects = new Rect[16];

        /// <summary>Combien de cartes par rangee : toutes sur une ligne jusqu'a six, puis deux rangees.</summary>
        static int DraftPerRow(int cards) { return cards > 6 ? (cards + 1) / 2 : Mathf.Max(1, cards); }

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
            // (01/10) Plus de bande rouge "remplace ..." : les capacites changent a chaque
            // manche, elle s'affichait sur TOUTES les cartes et ne disait rien.
            bool active = AbilityInfo.IsActive(p);
            CardArt.Draw(card, AbilityInfo.Tint(p), AbilityInfo.Name(p), AbilityInfo.Line(p),
                         active ? AbilityInfo.Cooldown(p) : 0f, active ? AbilityInfo.Keys[0] : null, null, owned, on, lift, enter, Icons.Of(p));
        }

        // ------------------------------------------------------------------ fin du match

        /// <summary>Une medaille : une pastille d'or ronde, une icone blanche dedans.</summary>
        static void Medal(Rect r, string icon, float a)
        {
            Icons.Pill(r, new Color(0.86f, 0.62f, 0.2f), a);
            Icons.Draw(new Rect(r.x + r.width * 0.16f, r.y + r.height * 0.16f, r.width * 0.68f, r.height * 0.68f), icon, new Color(1f, 1f, 1f, a));
        }

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
                // (02/10, le clipper) Ses MOMENTS du match : la claquette et leur nombre.
                int clips = Highlights.CountOf(s.Index);
                if (clips > 0)
                {
                    Icons.Draw(new Rect(r.xMax - h * 3.3f, r.y + h * 0.16f, h * 0.68f, h * 0.68f), "clip", new Color(1f, 1f, 1f, a));
                    Icons.Number(new Rect(r.xMax - h * 2.6f, r.y, h * 0.7f, h), clips.ToString(), Mathf.RoundToInt(h * 0.5f), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
                }
                // (03/10, le clipper fou n° 427) LES TITRES, en medaille d'or a gauche de la ligne :
                // le roi des KO (l'etoile d'impact), le roi des moments (la claquette).
                float mx = r.x - h * 0.95f;
                if (s.Index == Highlights.KoKing) { Medal(new Rect(mx, r.y + h * 0.05f, h * 0.9f, h * 0.9f), "ko", a); mx -= h * 0.95f; }
                if (s.Index == Highlights.Best && Highlights.CountOf(s.Index) >= 2) Medal(new Rect(mx, r.y + h * 0.05f, h * 0.9f, h * 0.9f), "clip", a);
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
                string[] items = NetGame.IsClient ? ClientEndItems : EndItems;
                for (int i = 0; i < items.Length; i++)
                {
                    bool primary = i == 0;
                    float h = UiStyle.S(primary ? 48 : 38);
                    if (Entry(new Rect((Screen.width - bw) * 0.5f, y, bw, h), items[i], i, primary, a)) Activate(i);
                    y += h + UiStyle.S(4);
                }            }
        }

        // ------------------------------------------------------------------ les commandes

        /// <summary>
        /// LES COMMANDES, EN LISTE (02/10 -- Martin : "on ne comprend absolument rien ; dans
        /// tous les autres jeux, t'as un truc tout lisse, ca descend, et tu peux changer").
        /// Fini les trois colonnes d'icones : une liste comme partout, par rubriques -- ce que
        /// ca fait, en toutes lettres, et la touche a droite. Elle defile en douceur (molette,
        /// fleches) ; la touche de capacite et celle de la poussee se changent ici meme (gauche
        /// / droite, ou les fleches a la souris), comme dans les Reglages.
        /// </summary>
        struct ControlLine
        {
            public string Head;         // une rubrique (sinon null)
            public string Icon;         // l'icone de la rubrique ou de la ligne
            public string What;         // ce que ca fait
            public string Key;          // la touche ("" : ca se fait tout seul)
            public int Bind;            // -1, ou la ligne des Reglages qui la change (5 : capacite, 6 : pousser)
        }

        static ControlLine Head(string icon, string text) { ControlLine l = new ControlLine(); l.Head = text; l.Icon = icon; l.Bind = -1; return l; }
        static ControlLine Line(string icon, string what, string key, int bind) { ControlLine l = new ControlLine(); l.Icon = icon; l.What = what; l.Key = key; l.Bind = bind; return l; }

        static ControlLine[] ControlLines()
        {
            string act = FiefInput.BindNames[Settings.ActiveBind];
            string push = FiefInput.BindNames[Settings.PushBind];
            return new[] {
                Head("joueur", "SE DÉPLACER"),
                Line("joueur", "Avancer, reculer, aller à gauche ou à droite", "Z Q S D", -1),
                Line("coureur", "Courir", "Maj", -1),
                Line("haut", "Sauter", "Espace", -1),
                Line("arbaleste", "Monter sur une arbaleste, prendre un sanctuaire", "E", -1),
                Head("cible", "SE BATTRE"),
                Line("cible", "Ta capacité", act, 5),
                Line("pousser", "Pousser (sur le porteur : tu lui voles la Couronne)", push, 6),
                Line("pique", "En vol : fondre sur le porteur (piqué d'aigle)", push, -1),
                Head("ailes", "VOLER"),
                Line("ailes", "Les ailes s'ouvrent toutes seules au-dessus du vide", "", -1),
                Line("vue", "Diriger le vol : regarde où tu veux aller", "Souris", -1),
                Line("ailes", "Replier ou rouvrir les ailes", "Espace", -1),
                Line("courant", "Remonter : tourne dans un courant d'air", "", -1),
                Line("ailes", "Te propulser : passe dans les anneaux de vent", "", -1),
                Head("reglages", "LE RESTE"),
                Line("manches", "Les scores", "Tab", -1),
                Line("reglages", "Pause et réglages", "Échap", -1),
                Line("commandes", "Cet écran", "F1", -1),
                Line("clip", "Écran propre pour filmer (sans rien par-dessus)", "F10", -1),
            };
        }

        float controlsScroll, controlsScrollShown;
        bool draggingBar;

        /// <summary>Les lignes qu'on peut viser au clavier (les rubriques, non), puis Retour.</summary>
        static int ControlRows(ControlLine[] lines)
        {
            int n = 0;
            for (int i = 0; i < lines.Length; i++) if (lines[i].Head == null) n++;
            return n + 1;
        }

        /// <summary>Le clavier sur la liste des commandes : haut/bas, gauche/droite sur les touches a changer, Entree.</summary>
        void ControlsKeyboard()
        {
            ControlLine[] lines = ControlLines();
            int rows = ControlRows(lines);
            if (FiefInput.UpPressed) { selected = (selected + rows - 1) % rows; Sfx.Pop(); return; }
            if (FiefInput.DownPressed) { selected = (selected + 1) % rows; Sfx.Pop(); return; }
            if (selected == rows - 1) { if (FiefInput.ConfirmPressed) { showControls = false; selected = 0; Sfx.Pop(); } return; }
            int bind = BindOfRow(lines, selected);
            if (bind < 0) return;
            if (FiefInput.LeftPressed) { Settings.Step(bind, -1); Sfx.Pop(); }
            else if (FiefInput.RightPressed || FiefInput.ConfirmPressed) { Settings.Step(bind, 1); Sfx.Pop(); }
        }

        static int BindOfRow(ControlLine[] lines, int row)
        {
            int n = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Head != null) continue;
                if (n == row) return lines[i].Bind;
                n++;
            }
            return -1;
        }

        void DrawControls()
        {
            Glide(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.015f, 0.05f, 0.86f));
            ControlLine[] lines = ControlLines();
            int rows = ControlRows(lines);
            float w = Mathf.Min(UiStyle.S(900), Screen.width - UiStyle.S(60));
            float x = Mathf.Round((Screen.width - w) * 0.5f);
            float top = Mathf.Round(Mathf.Max(UiStyle.S(30), Screen.height * 0.08f));
            Icons.Number(new Rect(x, top, w, UiStyle.S(56)), "COMMANDES", UiStyle.S(44), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);

            // La fenetre de la liste : entre le titre et le bouton Retour. Ce qui depasse defile.
            float viewTop = top + UiStyle.S(76);
            float viewBottom = Screen.height - UiStyle.S(110);
            Rect view = new Rect(x - UiStyle.S(10), Mathf.Round(viewTop), w + UiStyle.S(20), Mathf.Round(viewBottom - viewTop));
            float rowH = UiStyle.S(50), headH = UiStyle.S(58);
            float content = 0f;
            for (int i = 0; i < lines.Length; i++) content += lines[i].Head != null ? headH : rowH;
            float maxScroll = Mathf.Max(0f, content - view.height + UiStyle.S(10));

            // La molette fait defiler ; au clavier, la ligne visee reste toujours en vue.
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && view.Contains(e.mousePosition)) { controlsScroll += e.delta.y * UiStyle.S(20); e.Use(); }
            if (e.type == EventType.Repaint && selected < rows - 1)
            {
                float at = 0f; int n = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    float h = lines[i].Head != null ? headH : rowH;
                    if (lines[i].Head == null && n++ == selected)
                    {
                        if (at - controlsScroll < 0f) controlsScroll = at - headH;
                        if (at + h - controlsScroll > view.height) controlsScroll = at + h - view.height + UiStyle.S(10);
                        break;
                    }
                    at += h;
                }
            }
            controlsScroll = Mathf.Clamp(controlsScroll, 0f, maxScroll);
            if (e.type == EventType.Repaint) controlsScrollShown = Mathf.Lerp(controlsScrollShown, controlsScroll, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            float scroll = Mathf.Round(controlsScrollShown);

            GUI.BeginGroup(view);
            float y = -scroll;
            int row = 0;
            int keySize = UiStyle.S(20), textSize = UiStyle.S(22);
            for (int i = 0; i < lines.Length; i++)
            {
                ControlLine l = lines[i];
                if (l.Head != null)
                {
                    // Une rubrique : son icone dans un rond, son nom en or.
                    float hs = UiStyle.S(40);
                    Rect ic = new Rect(UiStyle.S(10), Mathf.Round(y + UiStyle.S(12)), hs, hs);
                    Icons.Pill(ic, new Color(0.22f, 0.26f, 0.58f));
                    Icons.Draw(new Rect(ic.x + hs * 0.15f, ic.y + hs * 0.15f, hs * 0.7f, hs * 0.7f), l.Icon, Color.white);
                    Icons.Number(new Rect(ic.xMax + UiStyle.S(14), ic.y, w, hs), l.Head, UiStyle.S(26), new Color(1f, 0.84f, 0.3f), TextAnchor.MiddleLeft);
                    y += headH;
                    continue;
                }
                int index = row++;
                bool on = selected == index;
                Rect r = new Rect(UiStyle.S(10), Mathf.Round(y + UiStyle.S(3)), w, Mathf.Round(rowH - UiStyle.S(6)));
                // (Pas d'evenement MouseMove en jeu -- seulement dans l'editeur : on suit la
                // souris quand elle a bouge, comme les boutons des autres menus.)
                if (hoverFollows && r.Contains(e.mousePosition) && selected != index) { selected = index; Sfx.Hover(); }
                Icons.Pill(r, on ? new Color(0.36f, 0.4f, 0.86f) : new Color(0.13f, 0.15f, 0.34f, 0.95f));
                float s = r.height;
                Icons.Draw(new Rect(r.x + s * 0.3f, r.y + s * 0.16f, s * 0.68f, s * 0.68f), l.Icon, new Color(1f, 1f, 1f, 0.9f));
                Icons.Text(new Rect(r.x + s * 1.2f, r.y, r.width * 0.64f, s), l.What, textSize, on ? new Color(1f, 0.92f, 0.6f) : Color.white, TextAnchor.MiddleLeft, true);
                // La touche, a droite, dans sa pastille claire ; les fleches si on peut la changer.
                float kw = Mathf.Max(s * 1.4f, Icons.Width(l.Key.Length > 0 ? l.Key : "auto", keySize) + s * 0.8f);
                Rect kr = new Rect(Mathf.Round(r.xMax - kw - s * (l.Bind >= 0 ? 1.1f : 0.3f)), r.y + s * 0.14f, Mathf.Round(kw), Mathf.Round(s * 0.72f));
                // Un clic sur la touche d'une ligne qu'on peut changer : la touche suivante.
                if (l.Bind >= 0 && GUI.Button(kr, GUIContent.none, GUIStyle.none)) { selected = index; Settings.Step(l.Bind, 1); Sfx.Pop(); }
                if (l.Key.Length > 0)
                {
                    Icons.Pill(kr, l.Bind >= 0 && on ? new Color(1f, 0.86f, 0.4f) : new Color(0.93f, 0.93f, 0.97f));
                    Icons.Text(kr, l.Key, keySize, new Color(0.1f, 0.1f, 0.2f), TextAnchor.MiddleCenter, false);
                }
                else Icons.Text(kr, "tout seul", keySize, new Color(0.7f, 0.9f, 1f), TextAnchor.MiddleCenter, true);
                if (l.Bind >= 0)
                {
                    int side = Mathf.RoundToInt(s * 0.62f) / 2 * 2;
                    Rect minus = new Rect(Mathf.Round(kr.x - side - s * 0.12f), Mathf.Round(r.y + (s - side) * 0.5f), side, side);
                    Rect plus = new Rect(Mathf.Round(kr.xMax + s * 0.12f), minus.y, side, side);
                    Color arrow = new Color(1f, 1f, 1f, on ? 1f : 0.5f);
                    Matrix4x4 keep = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), minus.center);
                    Icons.Draw(minus, "jouer", arrow);
                    GUI.matrix = keep;
                    Icons.Draw(plus, "jouer", arrow);
                    if (GUI.Button(minus, GUIContent.none, GUIStyle.none)) { selected = index; Settings.Step(l.Bind, -1); Sfx.Pop(); }
                    if (GUI.Button(plus, GUIContent.none, GUIStyle.none)) { selected = index; Settings.Step(l.Bind, 1); Sfx.Pop(); }
                }
                y += rowH;
            }
            GUI.EndGroup();

            // LA BARRE DE DEFILEMENT, fine, a droite (04/10 -- Martin : "le truc a droite pour
            // defiler, il est enorme et on capte rien") : elle etait dessinee avec la pastille des
            // boutons, faite pour des formes larges -- haute et fine, son liseré noir faisait
            // 50 pixels d'epaisseur. Maintenant : un rail sombre de 10 pixels, un curseur d'or, et
            // on peut l'ATTRAPER a la souris (cliquer ou glisser sur le rail fait defiler).
            if (maxScroll > 0f)
            {
                float track = view.height;
                float bar = Mathf.Round(UiStyle.S(10));
                float thumb = Mathf.Round(Mathf.Max(UiStyle.S(48), track * view.height / (content + UiStyle.S(10))));
                Rect rail = new Rect(Mathf.Round(view.xMax + UiStyle.S(10)), view.y, bar, track);
                // La zone qu'on peut attraper est plus large que le rail (on vise sans peine).
                Rect grab = new Rect(rail.x - UiStyle.S(12), rail.y, rail.width + UiStyle.S(24), rail.height);
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && (grab.Contains(e.mousePosition) || draggingBar && e.type == EventType.MouseDrag))
                {
                    draggingBar = true;
                    float k = Mathf.Clamp01((e.mousePosition.y - rail.y - thumb * 0.5f) / Mathf.Max(1f, track - thumb));
                    controlsScroll = controlsScrollShown = k * maxScroll;
                    e.Use();
                }
                if (e.type == EventType.MouseUp) draggingBar = false;
                float ty = Mathf.Round(rail.y + (track - thumb) * (Mathf.Round(controlsScrollShown) / maxScroll));
                UiStyle.Fill(new Rect(rail.x - 2f, rail.y - 2f, rail.width + 4f, rail.height + 4f), new Color(0f, 0f, 0f, 0.45f));
                UiStyle.Fill(rail, new Color(1f, 1f, 1f, 0.14f));
                bool hot = draggingBar || grab.Contains(e.mousePosition);
                UiStyle.Fill(new Rect(rail.x, ty, rail.width, thumb), hot ? new Color(1f, 0.92f, 0.55f) : new Color(1f, 0.82f, 0.3f));
            }
            if (Entry(new Rect(x, Screen.height - UiStyle.S(90), UiStyle.S(300), UiStyle.S(50)), "Retour", rows - 1, false, 1f)) { showControls = false; selected = 0; }
        }
    }
}
