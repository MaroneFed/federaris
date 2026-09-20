using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// L'ecran-titre et le menu pause.
    ///
    /// La partie ne demarre plus au lancement : on arrive sur un titre, avec le monde
    /// qui defile derriere (la camera tourne lentement autour du fief). C'est ce qui fait
    /// qu'un projet ressemble a un jeu plutot qu'a une scene de test.
    ///
    /// Concept Unity : Time.timeScale = 0 met le temps du jeu en pause. Les Update
    /// continuent de tourner, mais Time.deltaTime vaut 0, donc plus rien ne bouge.
    /// Un menu doit donc s'animer avec Time.unscaledDeltaTime, qui, lui, avance toujours.
    /// </summary>
    public class Menus : MonoBehaviour
    {
        public enum State { Title, Playing, Paused }

        public State Current { get; private set; }

        bool showControls;
        float fade;
        float appear;

        public bool Blocking { get { return Current != State.Playing; } }

        void Start()
        {
            Current = State.Title;
            Time.timeScale = 0f;
            appear = 0f;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            appear = Mathf.Min(1f, appear + dt * 1.6f);

            bool panelOpen = Game.Hud != null && Game.Hud.PanelOpen;

            if (FiefInput.CancelPressed)
            {
                if (showControls) showControls = false;
                else if (Current == State.Playing)
                {
                    if (panelOpen) Game.Hud.ClosePanel();
                    else Pause();
                }
                else if (Current == State.Paused) Resume();
            }

            // Le titre : la camera tourne doucement autour du fief.
            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null)
            {
                if (Current == State.Title)
                {
                    cam.autoOrbitSpeed = 5.5f;
                    cam.SetCinematic(26f, 14f);
                }
                else
                {
                    cam.autoOrbitSpeed = 0f;
                }
            }

            // Un seul endroit decide qui a la main : souris libre et joueur fige
            // des qu'un menu OU un panneau est ouvert.
            bool blocked = Blocking || panelOpen;
            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked;

            if (Game.Player != null) Game.Player.InputLocked = blocked;
            if (cam != null) cam.InputLocked = blocked;
            if (Game.Hud != null && Game.Hud.interactor != null) Game.Hud.interactor.InputLocked = blocked;

            fade = Mathf.MoveTowards(fade, Blocking ? 1f : 0f, dt * 4f);
        }

        // ------------------------------------------------------------------ transitions

        public void StartSeason()
        {
            Current = State.Playing;
            Time.timeScale = 1f;
            showControls = false;
            if (Game.Hud != null && Game.Hud.orbitCamera != null)
                Game.Hud.orbitCamera.ReleaseCinematic();
            Toasts.Clear();
            Toasts.Show("La Saison commence. Recolte, vends, batis.", Palette.Gold);
        }

        public void Pause()
        {
            Current = State.Paused;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            Current = State.Playing;
            Time.timeScale = 1f;
            showControls = false;
        }

        void Restart()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
            else
            {
                Toasts.Show("Ajoute la scene au Build Settings (menu FIEF) pour relancer.", Palette.Iron);
                Resume();
            }
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ dessin

        void OnGUI()
        {
            if (fade <= 0.001f) return;
            UiStyle.Ensure();

            // Voile sombre sur le jeu.
            UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height),
                         new Color(UiStyle.Scrim.r, UiStyle.Scrim.g, UiStyle.Scrim.b,
                                   UiStyle.Scrim.a * fade * (Current == State.Title ? 0.85f : 1f)));

            if (showControls) { DrawControls(); return; }
            if (Current == State.Title) DrawTitle();
            else if (Current == State.Paused) DrawPause();
        }

        void DrawTitle()
        {
            float ease = Mathf.SmoothStep(0f, 1f, appear);
            float w = UiStyle.S(560);
            float x = Mathf.Min(UiStyle.S(90), (Screen.width - w) * 0.5f);
            float y = Screen.height * 0.5f - UiStyle.S(220) + (1f - ease) * UiStyle.S(24);

            // Filet dore au-dessus du titre
            UiStyle.Fill(new Rect(x, y, UiStyle.S(120), 2f),
                         new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, ease));
            y += UiStyle.S(22);

            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(40)), "ROYAUME DE MARCHANDS ET DE TRAITRES",
                           UiStyle.Small, new Color(UiStyle.InkDim.r, UiStyle.InkDim.g, UiStyle.InkDim.b, ease));
            y += UiStyle.S(30);

            GUIStyle big = UiStyle.Big;
            int previous = big.fontSize;
            big.fontSize = UiStyle.S(78);
            UiStyle.Shadowed(new Rect(x, y, w, UiStyle.S(96)), "FIEF", big);
            big.fontSize = previous;
            y += UiStyle.S(104);

            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(30)),
                           "Batis ton fief. Domine le marche.", UiStyle.Head,
                           new Color(UiStyle.Ink.r, UiStyle.Ink.g, UiStyle.Ink.b, ease));
            y += UiStyle.S(26);
            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(30)),
                           "Prends les chateaux de tes rivaux.", UiStyle.Head,
                           new Color(UiStyle.Ink.r, UiStyle.Ink.g, UiStyle.Ink.b, ease));
            y += UiStyle.S(46);

            float bw = UiStyle.S(280);
            float bh = UiStyle.S(46);

            if (GUI.Button(new Rect(x, y, bw, bh), "COMMENCER LA SAISON", UiStyle.ButtonPrimary)) StartSeason();
            y += bh + UiStyle.S(10);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.82f), "Commandes", UiStyle.Button)) showControls = true;
            y += bh * 0.82f + UiStyle.S(8);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.82f), "Quitter", UiStyle.Button)) Quit();

            GUIStyle tiny = UiStyle.Tiny;
            GUI.Label(new Rect(UiStyle.S(24), Screen.height - UiStyle.S(34), UiStyle.S(700), UiStyle.S(24)),
                      "Phase 1 - boucle economique solo   |   4 a 6 joueurs vises   |   Unity 6", tiny);
        }

        void DrawPause()
        {
            float w = UiStyle.S(420);
            float h = UiStyle.S(330);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(30);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(60);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), "SAISON EN PAUSE", UiStyle.Title);
            y += UiStyle.S(40);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(18);

            if (Game.Wallet != null && Game.Fief != null)
            {
                GUI.Label(new Rect(x, y, bw, UiStyle.S(22)),
                          Game.Wallet.Gold + " or   |   " + Game.Fief.prestige + " prestige   |   "
                          + Game.Fief.buildingCount + "/6 batiments", UiStyle.Small);
                y += UiStyle.S(30);
            }

            float bh = UiStyle.S(42);
            if (GUI.Button(new Rect(x, y, bw, bh), "REPRENDRE", UiStyle.ButtonPrimary)) Resume();
            y += bh + UiStyle.S(9);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.85f), "Commandes", UiStyle.Button)) showControls = true;
            y += bh * 0.85f + UiStyle.S(7);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.85f), "Recommencer la Saison", UiStyle.Button)) Restart();
            y += bh * 0.85f + UiStyle.S(7);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.85f), "Quitter le jeu", UiStyle.Button)) Quit();
        }

        void DrawControls()
        {
            float w = UiStyle.S(520);
            float h = UiStyle.S(400);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(32);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(64);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), "COMMANDES", UiStyle.Title);
            y += UiStyle.S(42);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(16);

            string[,] rows =
            {
                { "ZQSD / WASD", "Se deplacer" },
                { "Maj", "Courir (sac pas trop lourd)" },
                { "Espace", "Sauter" },
                { "Souris", "Orienter la camera" },
                { "Molette", "Zoom" },
                { "E (maintenu)", "Recolter" },
                { "E", "Marche, coffre, construction" },
                { "Echap", "Pause / fermer" },
                { "F1", "Aide a l'ecran" }
            };

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                if (i % 2 == 0) UiStyle.Fill(new Rect(x - UiStyle.S(8), y - UiStyle.S(2), bw + UiStyle.S(16), UiStyle.S(26)),
                                             new Color(1f, 1f, 1f, 0.03f));
                UiStyle.Tinted(new Rect(x, y, UiStyle.S(160), UiStyle.S(24)), rows[i, 0], UiStyle.Label, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(170), y, bw - UiStyle.S(170), UiStyle.S(24)), rows[i, 1], UiStyle.Small);
                y += UiStyle.S(26);
            }

            y = box.yMax - UiStyle.S(58);
            if (GUI.Button(new Rect(x, y, bw, UiStyle.S(40)), "RETOUR", UiStyle.ButtonPrimary)) showControls = false;
        }
    }
}
