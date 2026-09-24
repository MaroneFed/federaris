using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// L'ecran-titre, le menu pause et la liste des commandes.
    ///
    /// L'ECRAN-TITRE, C'EST LE PERSONNAGE. Le mendiant se tient seul dans la brume,
    /// sa lanterne allumee, et la camera tourne lentement autour de lui a six
    /// metres. C'est le seul moment du jeu ou on le voit de dehors : on le montre.
    ///
    /// Deux choses corrigees par rapport a la version precedente :
    ///
    ///   - la camera tournait a 26 metres. Avec une brume qui efface tout a 26
    ///     metres, le personnage aurait ete invisible sur son propre ecran-titre ;
    ///   - un voile noir a 85 % recouvrait tout l'ecran. Il cachait justement ce
    ///     qu'on a de plus beau. Il est remplace par un degrade sous le texte, a
    ///     gauche, et un vignettage : on lit le texte, on voit la foret.
    ///
    /// LE MONDE VIT DERRIERE LE TITRE. Le temps n'est plus gele : la lanterne
    /// vacille, le poncho bouge, le mendiant respire. Seul le joueur est bloque.
    /// Le menu pause, lui, gele le temps -- c'est ce qu'on attend d'une pause.
    ///
    /// ENTRER DANS LE JEU est un fondu au noir : la camera quitte l'orbite et se
    /// place dans les yeux pendant que l'ecran est noir, puis l'image revient. Sans
    /// ca, le saut de la vue de dehors a la vue de dedans est un a-coup brutal.
    ///
    /// LA CLOCHE. Quand la Saison s'acheve, le jeu se fige (le joueur, pas le monde :
    /// la lanterne vacille toujours) et l'ecran de fin dit ce que vaut la relique
    /// posee sur la stele. Seule une relique POSEE compte.
    ///
    /// Concept Unity : Time.timeScale = 0 met le temps du jeu en pause ; les Update
    /// tournent toujours mais Time.deltaTime vaut 0. Un menu doit donc s'animer
    /// avec Time.unscaledDeltaTime, qui avance toujours.
    /// </summary>
    public class Menus : MonoBehaviour
    {
        public enum State { Title, Playing, Paused, Ended }

        public State Current { get; private set; }

        public bool Blocking { get { return Current != State.Playing; } }

        bool showControls;
        float appear;           // 0 -> 1 : apparition du titre
        float veil;             // 0 -> 1 : voile de la pause
        float curtain;          // 0 -> 1 : noir complet, pendant l'entree en jeu
        bool entering;          // le rideau descend ; on bascule quand il est noir
        float ended;            // 0 -> 1 : apparition de l'ecran de fin
        int bellWarnings;       // rappels "la cloche approche" deja donnes

        static Texture2D sideShade;
        static Texture2D vignette;

        void Start()
        {
            Current = State.Title;
            Time.timeScale = 1f;
            appear = 0f;
            curtain = 1f;       // on ouvre sur du noir, la foret apparait en fondu
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            appear = Mathf.Min(1f, appear + dt * 0.55f);

            bool panelOpen = Game.Hud != null && Game.Hud.PanelOpen;

            Season season = Game.Season;
            if (Current == State.Playing && season != null)
            {
                WarnOfBell(season);
                if (season.Over) EndSeason();
            }
            if (Current == State.Ended) ended = Mathf.Min(1f, ended + dt * 0.4f);

            if (FiefInput.CancelPressed && !entering)
            {
                if (showControls) showControls = false;
                else if (Current == State.Playing)
                {
                    if (panelOpen) Game.Hud.ClosePanel();
                    else Pause();
                }
                else if (Current == State.Paused) Resume();
            }

            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null)
            {
                if (Current == State.Title)
                {
                    // Six metres, a peine au-dessus de la tete : assez pres pour que
                    // la brume ne l'efface pas, assez loin pour voir la silhouette.
                    cam.autoOrbitSpeed = 3.2f;
                    cam.SetCinematic(6.2f, 6f);
                }
                else
                {
                    cam.autoOrbitSpeed = 0f;
                }
            }

            // --- le rideau : il descend, on bascule dans le noir, il remonte
            if (entering)
            {
                curtain = Mathf.MoveTowards(curtain, 1f, dt * 2.6f);
                if (curtain >= 1f)
                {
                    entering = false;
                    BeginPlaying();
                }
            }
            else
            {
                curtain = Mathf.MoveTowards(curtain, 0f, dt * (Current == State.Title ? 0.7f : 0.9f));
            }

            // Un seul endroit decide qui a la main : souris libre et joueur fige
            // des qu'un menu OU un panneau est ouvert.
            bool blocked = Blocking || panelOpen || entering;
            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked && !entering;

            if (Game.Player != null) Game.Player.InputLocked = blocked;
            if (cam != null) cam.InputLocked = blocked;
            if (Game.Hud != null && Game.Hud.interactor != null) Game.Hud.interactor.InputLocked = blocked;
            // Le HUD se cache tout seul tant que le menu bloque (Hud.Hidden lit Blocking).

            float wantVeil = Current == State.Paused || showControls ? 1f : Current == State.Ended ? 0.85f : 0f;
            veil = Mathf.MoveTowards(veil, wantVeil, dt * (Current == State.Ended ? 0.5f : 5f));
        }

        // ------------------------------------------------------------------ transitions

        public void StartSeason()
        {
            if (entering || Current != State.Title) return;
            entering = true;
            showControls = false;
        }

        void BeginPlaying()
        {
            Current = State.Playing;
            Time.timeScale = 1f;

            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null)
            {
                cam.ReleaseCinematic();
                // On rouvre les yeux dans la direction ou regarde le personnage.
                if (cam.target != null) cam.yaw = cam.target.eulerAngles.y;
                cam.pitch = 3f;
            }

            // L'horloge ne part qu'ici, pas au chargement : l'ecran-titre ne mange
            // pas les trente minutes de la Saison.
            if (Game.Season != null) Game.Season.Begin();

            Toasts.Clear();
            Toasts.Show("La brume se referme derriere toi.", Palette.Gold);
            Toasts.Show("F1 pour les commandes.", UiStyle.Ink);
        }

        /// <summary>Deux rappels : a cinq minutes, puis a une minute de la cloche.</summary>
        void WarnOfBell(Season season)
        {
            float left = season.Remaining;
            if (bellWarnings == 0 && left <= 300f)
            {
                bellWarnings = 1;
                Toasts.Show("La cloche sonnera dans cinq minutes. Seule une relique posee sur la stele comptera.",
                            new Color(0.92f, 0.62f, 0.32f));
            }
            else if (bellWarnings == 1 && left <= 60f)
            {
                bellWarnings = 2;
                Toasts.Show("Une minute avant la cloche.", new Color(0.92f, 0.45f, 0.32f));
            }
        }

        void EndSeason()
        {
            Current = State.Ended;
            ended = 0f;
            if (Game.Hud != null) Game.Hud.ClosePanel();
            Sfx.Bell();
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
                Toasts.Show("Ajoute la scene au Build Settings pour relancer.", Palette.Iron);
                if (Current == State.Paused) Resume();
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

        // ------------------------------------------------------------------ textures

        /// <summary>
        /// Deux degrades fabriques une fois : un voile qui assombrit la gauche de
        /// l'ecran (sous le texte) et un vignettage. Tout se lit, rien n'est cache.
        /// </summary>
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

        // ------------------------------------------------------------------ dessin

        void OnGUI()
        {
            UiStyle.Ensure();
            EnsureTextures();
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);

            if (Current == State.Title || entering)
            {
                GUI.color = new Color(1f, 1f, 1f, 1f - curtain * (entering ? 0f : 1f));
                GUI.DrawTexture(screen, vignette, ScaleMode.StretchToFill);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), sideShade, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }

            if (veil > 0.001f)
            {
                UiStyle.Fill(screen, new Color(UiStyle.Scrim.r, UiStyle.Scrim.g, UiStyle.Scrim.b,
                                               UiStyle.Scrim.a * veil));
            }

            if (showControls) DrawControls();
            else if (Current == State.Title && !entering) DrawTitle();
            else if (Current == State.Paused) DrawPause();
            else if (Current == State.Ended) DrawEnd();

            // Le rideau passe par-dessus tout, y compris le texte.
            if (curtain > 0.001f) UiStyle.Fill(screen, new Color(0f, 0f, 0f, curtain));
        }

        void DrawTitle()
        {
            // Le texte monte doucement pendant que la foret sort du noir.
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.15f) / 0.85f));
            float late = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.45f) / 0.55f));

            float x = UiStyle.S(96);
            float w = Mathf.Min(UiStyle.S(620), Screen.width - x * 2f);
            float y = Screen.height - UiStyle.S(430) + (1f - ease) * UiStyle.S(18);

            GUIStyle big = UiStyle.Big;
            int previous = big.fontSize;
            big.fontSize = UiStyle.S(92);
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            UiStyle.Shadowed(new Rect(x, y, w, UiStyle.S(110)), "F I E F", big);
            GUI.color = was;
            big.fontSize = previous;
            y += UiStyle.S(112);

            UiStyle.Fill(new Rect(x + UiStyle.S(4), y, UiStyle.S(150) * ease, 2f),
                         new Color(Palette.Gold.r, Palette.Gold.g, Palette.Gold.b, ease));
            y += UiStyle.S(18);

            UiStyle.Tinted(new Rect(x + UiStyle.S(4), y, w, UiStyle.S(30)),
                           "Ce que tu caches, un autre le cherche.", UiStyle.Head,
                           new Color(UiStyle.Ink.r, UiStyle.Ink.g, UiStyle.Ink.b, ease));
            y += UiStyle.S(58);

            GUI.color = new Color(1f, 1f, 1f, late);
            float bw = UiStyle.S(300);
            float bh = UiStyle.S(46);
            if (GUI.Button(new Rect(x, y, bw, bh), "ENTRER DANS LA SYLVE", UiStyle.ButtonPrimary) && late > 0.9f)
                StartSeason();
            y += bh + UiStyle.S(10);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.8f), "Commandes", UiStyle.Button) && late > 0.9f)
                showControls = true;
            y += bh * 0.8f + UiStyle.S(8);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.8f), "Quitter", UiStyle.Button) && late > 0.9f)
                Quit();
            GUI.color = Color.white;

            UiStyle.Tinted(new Rect(UiStyle.S(24), Screen.height - UiStyle.S(34), UiStyle.S(700), UiStyle.S(24)),
                           "Prototype   |   la sylve   |   Unity 6", UiStyle.Tiny,
                           new Color(UiStyle.InkFaint.r, UiStyle.InkFaint.g, UiStyle.InkFaint.b, late));
        }

        void DrawPause()
        {
            float w = UiStyle.S(400);
            float h = UiStyle.S(300);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(30);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(60);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), "EN PAUSE", UiStyle.Title);
            y += UiStyle.S(40);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(22);

            float bh = UiStyle.S(42);
            if (GUI.Button(new Rect(x, y, bw, bh), "REPRENDRE", UiStyle.ButtonPrimary)) Resume();
            y += bh + UiStyle.S(9);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.85f), "Commandes", UiStyle.Button)) showControls = true;
            y += bh * 0.85f + UiStyle.S(7);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.85f), "Recommencer", UiStyle.Button)) Restart();
            y += bh * 0.85f + UiStyle.S(7);
            if (GUI.Button(new Rect(x, y, bw, bh * 0.85f), "Quitter le jeu", UiStyle.Button)) Quit();
        }

        /// <summary>
        /// Ce que vaut la relique, dit avec des mots. En solo, il n'y a personne a
        /// battre : ces paliers donnent un but a la partie suivante. Ils sont regles
        /// par la simulation de Saison (Tools/saison.py) : un flaneur fait un
        /// talisman (~140), un joueur regulier un tresor (~990), un expert qui va
        /// aux six apparitions ~1340 -- et seul celui qui se sert AUSSI de ses caches
        /// pour faire deux voyages par apparition atteint la legende (~1550).
        /// </summary>
        public static string Rank(int power)
        {
            // "fetiche" et plus "talisman" : les talismans sont devenus des objets.
            return Relic.TierName(Relic.Tier(power));
        }

        void DrawEnd()
        {
            Hoard hoard = Game.Hoard;
            if (hoard == null) return;

            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ended - 0.2f) / 0.8f));
            if (ease <= 0f) return;
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);

            float w = UiStyle.S(560);
            float h = UiStyle.S(444);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f + (1f - ease) * UiStyle.S(20), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(32);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(64);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), "LA CLOCHE A SONNE", UiStyle.Title);
            y += UiStyle.S(42);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(20);

            int score = hoard.FinalScore;
            Relic relic = hoard.Relic;
            string verdict;
            string detail;
            if (relic == null)
            {
                verdict = "Tu n'as rien forge.";
                detail = "Le mage chante six fois par Saison. Suis sa voix, les bras charges.";
            }
            else if (!hoard.RelicOnStele)
            {
                verdict = "Ta relique n'etait pas sur la stele.";
                detail = "Puissance " + relic.Power + ", mais elle ne compte pas : il fallait la poser avant la cloche.";
            }
            else
            {
                verdict = "Sur la stele : " + Rank(score) + ".";
                detail = "Forgee " + relic.Forgings + " fois : "
                         + relic.Get(ResourceType.Deadwood) + " bois mort, "
                         + relic.Get(ResourceType.Moonstone) + " pierre-lune, "
                         + relic.Get(ResourceType.Iron) + " fer ancien."
                         + (hoard.Has(Talisman.Couronne) ? "  La Couronne sans tete ajoute 15 %." : "");
            }

            GUIStyle big = UiStyle.Big;
            UiStyle.Shadowed(new Rect(x, y, bw, UiStyle.S(56)), score.ToString(), big);
            GUI.Label(new Rect(x, y + UiStyle.S(52), bw, UiStyle.S(20)), "puissance", UiStyle.Small);
            y += UiStyle.S(84);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(26)), verdict, UiStyle.Head);
            y += UiStyle.S(30);

            GUIStyle wrapped = UiStyle.Small;
            bool wrap = wrapped.wordWrap;
            wrapped.wordWrap = true;
            GUI.Label(new Rect(x, y, bw, UiStyle.S(40)), detail, wrapped);
            y += UiStyle.S(46);
            GUI.Label(new Rect(x, y, bw, UiStyle.S(20)),
                      "Talismans trouves : " + hoard.TalismanCount + " / " + TalismanInfo.Count, UiStyle.Small);
            y += UiStyle.S(22);
            GUI.Label(new Rect(x, y, bw, UiStyle.S(40)),
                      "Astuce : une cache pleine pres de l'endroit ou le mage chante, c'est deux voyages au lieu d'un.",
                      UiStyle.Tiny);
            wrapped.wordWrap = wrap;

            float bh = UiStyle.S(42);
            float by = box.yMax - bh - UiStyle.S(26);
            if (GUI.Button(new Rect(x, by, bw * 0.55f, bh), "UNE AUTRE SAISON", UiStyle.ButtonPrimary) && ease > 0.9f)
                Restart();
            if (GUI.Button(new Rect(x + bw * 0.6f, by, bw * 0.4f, bh), "Quitter", UiStyle.Button) && ease > 0.9f)
                Quit();

            GUI.color = was;
        }

        /// <summary>La liste des commandes. A tenir a jour a chaque nouvelle action.</summary>
        public static readonly string[,] Controls =
        {
            { "ZQSD / WASD", "Se deplacer" },
            { "Souris", "Regarder" },
            { "Maj", "Courir (sac pas trop lourd)" },
            { "Espace", "Sauter" },
            { "E", "Interagir, ramasser" },
            { "C", "Planter ton camp (une fois)" },
            { "G (maintenir)", "Creuser une cache (trois)" },
            { "Echap", "Pause" },
            { "F1", "Aide a l'ecran" },
            { "F3", "Diagnostic" }
        };

        void DrawControls()
        {
            int count = Controls.GetLength(0);
            float w = UiStyle.S(520);
            float h = UiStyle.S(170) + count * UiStyle.S(26);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(32);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(64);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), "COMMANDES", UiStyle.Title);
            y += UiStyle.S(42);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(16);

            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0) UiStyle.Fill(new Rect(x - UiStyle.S(8), y - UiStyle.S(2), bw + UiStyle.S(16), UiStyle.S(26)),
                                             new Color(1f, 1f, 1f, 0.03f));
                UiStyle.Tinted(new Rect(x, y, UiStyle.S(160), UiStyle.S(24)), Controls[i, 0], UiStyle.Label, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(170), y, bw - UiStyle.S(170), UiStyle.S(24)), Controls[i, 1], UiStyle.Small);
                y += UiStyle.S(26);
            }

            y = box.yMax - UiStyle.S(58);
            if (GUI.Button(new Rect(x, y, bw, UiStyle.S(40)), "RETOUR", UiStyle.ButtonPrimary)) showControls = false;
        }
    }
}
