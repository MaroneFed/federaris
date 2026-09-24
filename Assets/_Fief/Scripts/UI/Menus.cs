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
        public enum State { Title, Briefing, Playing, Paused, Ended }

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
                // Une victoire immediate (Trahison, Couronne, Offrande) arrete tout.
                if (season.Over || Victories.Decided) EndSeason();
            }
            if (Current == State.Ended) ended = Mathf.Min(1f, ended + dt * 0.4f);

            if (FiefInput.CancelPressed && !entering)
            {
                if (showControls) showControls = false;
                else if (Current == State.Briefing) Current = State.Title;
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
                if (Current == State.Title || Current == State.Briefing)
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
            bool dead = Game.Hud != null && Game.Hud.Dead;
            bool blocked = Blocking || panelOpen || entering || dead;
            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = blocked && !entering && !dead;

            if (Game.Player != null) Game.Player.InputLocked = blocked;
            if (cam != null) cam.InputLocked = blocked;
            if (Game.Hud != null && Game.Hud.interactor != null) Game.Hud.interactor.InputLocked = blocked;
            // Le HUD se cache tout seul tant que le menu bloque (Hud.Hidden lit Blocking).

            float wantVeil = Current == State.Paused || showControls ? 1f : Current == State.Ended ? 0.85f : 0f;
            veil = Mathf.MoveTowards(veil, wantVeil, dt * (Current == State.Ended ? 0.5f : 5f));
        }

        // ------------------------------------------------------------------ transitions

        /// <summary>
        /// "Entrer dans la sylve" : d'abord le BRIEFING (trois pages : le but, la
        /// boucle, les gestes), puis seulement le fondu au noir et la partie.
        /// </summary>
        public void StartSeason()
        {
            if (entering || Current != State.Title) return;
            Current = State.Briefing;
            briefingPage = 0;
            showControls = false;
        }

        int briefingPage;

        void DrawBriefing()
        {
            DrawEmbers(0.5f);
            float w = Mathf.Min(UiStyle.S(820), Screen.width - UiStyle.S(40));
            float h = UiStyle.S(520);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);
            float x = box.x + UiStyle.S(44);
            float y = box.y + UiStyle.S(30);
            float bw = w - UiStyle.S(88);

            string[] titles = { "LA SAISON", "LA BOUCLE", "TES GESTES" };
            GUIStyle title = UiStyle.Title;
            TextAnchor previous = title.alignment;
            title.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(box.x, y, w, UiStyle.S(40)), UiStyle.Spaced(titles[briefingPage]), title);
            title.alignment = previous;
            y += UiStyle.S(48);
            UiStyle.Rule(new Rect(x, y, bw, UiStyle.S(8)));
            y += UiStyle.S(24);

            GUIStyle body = UiStyle.Label;
            bool wrap = body.wordWrap;
            body.wordWrap = true;
            if (briefingPage == 0)
            {
                GUI.Label(new Rect(x, y, bw, UiStyle.S(50)),
                          "Trente minutes. Au centre de la foret, un chateau mort. Autour de toi, trois rivaux qui cherchent la meme chose que toi. "
                          + "Il y a quatre facons de gagner :", body);
                y += UiStyle.S(62);
                VictoryKind[] kinds = { VictoryKind.Relique, VictoryKind.Trahison, VictoryKind.Couronne, VictoryKind.Offrande };
                Color[] tints = { Stele.RuneBlue, new Color(0.95f, 0.78f, 0.35f), new Color(0.9f, 0.5f, 0.4f), new Color(0.62f, 0.86f, 0.48f) };
                for (int i = 0; i < kinds.Length; i++)
                {
                    float d = UiStyle.S(12);
                    UiStyle.Icon(new Rect(x, y + UiStyle.S(8), d, d), UiStyle.Shape.Diamond, tints[i]);
                    UiStyle.Tinted(new Rect(x + UiStyle.S(24), y, bw, UiStyle.S(26)), Victories.Title(kinds[i]), UiStyle.Head, tints[i]);
                    UiStyle.Tinted(new Rect(x + UiStyle.S(24), y + UiStyle.S(26), bw, UiStyle.S(20)), Victories.How(kinds[i]), UiStyle.Small, UiStyle.InkDim);
                    y += UiStyle.S(58);
                }
                UiStyle.Tinted(new Rect(x, y, bw, UiStyle.S(20)), "Les trois dernieres sont des courses : la premiere tombee arrete tout. Sinon, la Relique tranche a la cloche.",
                               UiStyle.Tiny, UiStyle.InkFaint);
            }
            else if (briefingPage == 1)
            {
                string[] lines =
                {
                    "1.  RECOLTE.  Bois mort au pied des arbres morts, pierre-lune dans les creux qui luisent, fer ancien dans les reserves du chateau (gardees).",
                    "2.  CACHE.  Ton sac est limite par le POIDS, et plus il est lourd, plus tes gestes sont lents. Creuse des caches (G), plante ton camp (C).",
                    "3.  LE MAGE.  Six fois par Saison, une colonne bleue monte au-dessus des arbres : tout le monde y court. Il fond ce que tu PORTES en une relique.",
                    "4.  TA STELE.  Plante-la une fois (P), cachee. Pose ta relique dessus : elle comptera a la cloche. Elle chante doucement -- on peut la trouver, et la PILLER.",
                    "5.  LES AUTRES.  Les rivaux pillent les steles qu'ils trouvent. Toi aussi, tu peux. Les gardes du chateau ne voient pas l'or : ils l'empochent."
                };
                for (int i = 0; i < lines.Length; i++)
                {
                    GUI.Label(new Rect(x, y, bw, UiStyle.S(54)), lines[i], body);
                    y += UiStyle.S(62);
                }
            }
            else
            {
                string[,] keys =
                {
                    { "ZQSD + souris", "marcher, regarder  --  Maj pour courir (si le sac n'est pas trop lourd)" },
                    { "E", "ramasser, parler, poser, voler (maintenir)" },
                    { "P", "planter ta stele (une seule fois)" },
                    { "C  /  G", "planter ton camp  /  creuser une cache (maintenir)" },
                    { "Tab", "ta besace : victoires, artisanat (hache, epee), talismans" },
                    { "1 / 2  +  clic", "prendre un outil, frapper  --  F : grimper dans un arbre" },
                    { "Echap", "pause   --   F1 : toutes les commandes" }
                };
                for (int i = 0; i < keys.GetLength(0); i++)
                {
                    Rect key = new Rect(x, y + UiStyle.S(2), UiStyle.S(170), UiStyle.S(28));
                    UiStyle.Pill(key);
                    UiStyle.Tinted(key, keys[i, 0], UiStyle.Centered, Palette.Gold);
                    UiStyle.Tinted(new Rect(x + UiStyle.S(190), y, bw - UiStyle.S(190), UiStyle.S(32)), keys[i, 1], UiStyle.Label, UiStyle.Ink);
                    y += UiStyle.S(44);
                }
                UiStyle.Tinted(new Rect(x, y + UiStyle.S(10), bw, UiStyle.S(22)),
                               "En haut a droite, les PREMIERS PAS te guident. Suis-les.", UiStyle.Label, Palette.Gold);
            }
            body.wordWrap = wrap;

            float bh = UiStyle.S(44);
            float by = box.yMax - bh - UiStyle.S(24);
            if (briefingPage > 0 && GUI.Button(new Rect(x, by, UiStyle.S(160), bh), "Retour", UiStyle.Button)) briefingPage--;
            bool last = briefingPage == 2;
            if (GUI.Button(new Rect(box.xMax - UiStyle.S(44) - UiStyle.S(260), by, UiStyle.S(260), bh),
                           last ? "Entrer dans la sylve" : "Suivant", UiStyle.ButtonPrimary))
            {
                if (last) { entering = true; Current = State.Title; }
                else briefingPage++;
            }
            for (int i = 0; i < 3; i++)
            {
                float d = UiStyle.S(8);
                UiStyle.Icon(new Rect(box.center.x - UiStyle.S(24) + i * UiStyle.S(20), by + bh * 0.5f - d * 0.5f, d, d),
                             UiStyle.Shape.Diamond, i == briefingPage ? Palette.Gold : UiStyle.InkFaint);
            }
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
            Victories.DecideByRelic();
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
            else if (Current == State.Briefing) DrawBriefing();
            else if (Current == State.Paused) DrawPause();
            else if (Current == State.Ended) DrawEnd();

            // Le rideau passe par-dessus tout, y compris le texte.
            if (curtain > 0.001f) UiStyle.Fill(screen, new Color(0f, 0f, 0f, curtain));
        }

        // ------------------------------------------------------------------ les braises

        /// <summary>
        /// Des braises qui montent lentement devant l'ecran-titre et la pause, comme
        /// d'un feu hors champ. Dessinees a la main par l'interface : ce ne sont pas
        /// des particules du monde, elles vivent meme quand le temps est fige.
        /// </summary>
        static readonly Vector3[] Embers = new Vector3[70];     // x, y (0-1), graine
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
                float life = Mathf.Sin(y * Mathf.PI);                   // naissent en bas, meurent en haut
                float flicker = 0.6f + 0.4f * Mathf.Sin(t * (3f + e.z % 5f) + e.z);
                float size = UiStyle.S(2f + (e.z % 3f));
                Color c = Color.Lerp(new Color(1f, 0.45f, 0.15f), new Color(1f, 0.8f, 0.45f), (e.z % 10f) / 10f);
                c.a = life * flicker * 0.75f * strength;
                UiStyle.Icon(new Rect(x * Screen.width, y * Screen.height, size, size), UiStyle.Shape.Dot, c);
            }
        }

        // ------------------------------------------------------------------ les entrees

        /// <summary>
        /// Une entree de menu : du texte, pas une boite. Au survol, elle s'eclaire en
        /// or, un losange apparait a gauche et un filet se dessine dessous. Renvoie
        /// vrai quand on clique.
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

        void DrawTitle()
        {
            // Le texte monte doucement pendant que la foret sort du noir.
            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.15f) / 0.85f));
            float late = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((appear - 0.45f) / 0.55f));
            DrawEmbers(ease);

            float x = UiStyle.S(110);
            float w = Mathf.Min(UiStyle.S(640), Screen.width - x * 2f);
            float y = Screen.height - UiStyle.S(500) + (1f - ease) * UiStyle.S(18);

            // Le titre : grave, espace, avec un halo sombre derriere.
            GUIStyle big = UiStyle.Big;
            int previous = big.fontSize;
            big.fontSize = UiStyle.S(112);
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            UiStyle.Tinted(new Rect(x + 4f, y + 5f, w, UiStyle.S(130)), UiStyle.Spaced("FIEF"), big, new Color(0f, 0f, 0f, 0.8f));
            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(130)), UiStyle.Spaced("FIEF"), big, new Color(0.93f, 0.78f, 0.45f));
            GUI.color = was;
            big.fontSize = previous;
            y += UiStyle.S(132);

            GUI.color = new Color(1f, 1f, 1f, ease);
            UiStyle.Rule(new Rect(x, y, UiStyle.S(420) * ease, UiStyle.S(8)));
            GUI.color = was;
            y += UiStyle.S(22);

            UiStyle.Tinted(new Rect(x + UiStyle.S(4), y, w, UiStyle.S(30)),
                           "Ce que tu caches, un autre le cherche.", UiStyle.Head,
                           new Color(UiStyle.InkDim.r, UiStyle.InkDim.g, UiStyle.InkDim.b, ease));
            y += UiStyle.S(64);

            float bw = UiStyle.S(420);
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(48)), "Entrer dans la sylve", true, late) && late > 0.9f)
                StartSeason();
            y += UiStyle.S(54);
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(36)), "Commandes", false, late) && late > 0.9f)
                showControls = true;
            y += UiStyle.S(40);
            if (Entry(new Rect(x - UiStyle.S(18), y, bw, UiStyle.S(36)), "Quitter", false, late) && late > 0.9f)
                Quit();

            UiStyle.Tinted(new Rect(UiStyle.S(24), Screen.height - UiStyle.S(34), UiStyle.S(700), UiStyle.S(24)),
                           "Prototype   --   la sylve, le chateau, trois rivaux   --   Unity 6", UiStyle.Tiny,
                           new Color(UiStyle.InkFaint.r, UiStyle.InkFaint.g, UiStyle.InkFaint.b, late));
        }

        void DrawPause()
        {
            DrawEmbers(0.6f);
            float w = UiStyle.S(440);
            float h = UiStyle.S(360);
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
            y += UiStyle.S(30);

            if (Entry(new Rect(x, y, bw, UiStyle.S(44)), "Reprendre", true, 1f)) Resume();
            y += UiStyle.S(52);
            if (Entry(new Rect(x, y, bw, UiStyle.S(36)), "Commandes", false, 1f)) showControls = true;
            y += UiStyle.S(40);
            if (Entry(new Rect(x, y, bw, UiStyle.S(36)), "Recommencer la Saison", false, 1f)) Restart();
            y += UiStyle.S(40);
            if (Entry(new Rect(x, y, bw, UiStyle.S(36)), "Quitter le jeu", false, 1f)) Quit();
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
            float h = UiStyle.S(520);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f + (1f - ease) * UiStyle.S(20), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(32);
            float y = box.y + UiStyle.S(26);
            float bw = w - UiStyle.S(64);

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), UiStyle.Spaced("LA CLOCHE"), UiStyle.Title);
            y += UiStyle.S(42);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(20);

            // --- le classement : toi et les rivaux, du plus puissant au plus faible.
            System.Collections.Generic.List<Seeker> order = new System.Collections.Generic.List<Seeker>(Game.Seekers);
            order.Sort((a, b) => b.Score.CompareTo(a.Score));
            bool won = Victories.Winner != null && Victories.Winner.IsPlayer;

            // Le titre de la victoire, puis qui, puis comment.
            UiStyle.Tinted(new Rect(x, y, bw, UiStyle.S(30)), Victories.Title(Victories.Kind), UiStyle.Head,
                           won ? Palette.Gold : new Color(0.9f, 0.5f, 0.4f));
            y += UiStyle.S(28);
            string verdict;
            if (Victories.Winner == null) verdict = "Aucune relique posee, aucun serment, aucune couronne.";
            else if (won) verdict = "C'est toi. " + Victories.How(Victories.Kind);
            else verdict = Victories.Winner.Name + " l'emporte. " + (hoard.RelicOnStele ? "Ta relique : " + Rank(hoard.FinalScore) + "." : "");
            GUIStyle wrappedVerdict = UiStyle.Small;
            bool wrapV = wrappedVerdict.wordWrap;
            wrappedVerdict.wordWrap = true;
            GUI.Label(new Rect(x, y, bw, UiStyle.S(36)), verdict, wrappedVerdict);
            wrappedVerdict.wordWrap = wrapV;
            y += UiStyle.S(42);

            for (int i = 0; i < order.Count; i++)
            {
                Seeker sk = order[i];
                float rowH = UiStyle.S(i == 0 ? 50 : 38);
                Rect row = new Rect(x, y, bw, rowH - UiStyle.S(6));
                UiStyle.CardFrame(row);
                UiStyle.Fill(new Rect(row.x, row.y + UiStyle.S(6), UiStyle.S(4), row.height - UiStyle.S(12)), sk.Colour);
                GUIStyle nameStyle = i == 0 ? UiStyle.Head : UiStyle.Label;
                UiStyle.Tinted(new Rect(row.x + UiStyle.S(16), row.y, UiStyle.S(40), row.height), (i + 1) + ".", nameStyle, UiStyle.InkDim);
                UiStyle.Tinted(new Rect(row.x + UiStyle.S(48), row.y, bw * 0.5f, row.height), sk.Name, nameStyle,
                               sk.IsPlayer ? Palette.Gold : UiStyle.Ink);
                GUIStyle right = UiStyle.Label;
                TextAnchor previous = right.alignment;
                right.alignment = TextAnchor.MiddleRight;
                string what = sk.Score > 0 ? sk.Score + "   " + Relic.TierName(Relic.Tier(sk.Score)) : "rien sur sa stele";
                GUI.Label(new Rect(row.x, row.y, row.width - UiStyle.S(16), row.height), what, right);
                right.alignment = previous;
                y += rowH;
            }
            y += UiStyle.S(6);
            GUI.Label(new Rect(x, y, bw, UiStyle.S(20)),
                      "Talismans trouves : " + hoard.TalismanCount + " / " + TalismanInfo.Count, UiStyle.Small);
            y += UiStyle.S(22);

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
            { "P", "Planter ta stele (une fois)" },
            { "Tab", "Ta besace : victoires, artisanat, talismans" },
            { "1 / 2", "Prendre en main un outil (hache, epee)" },
            { "Clic gauche", "Frapper : abattre un arbre, se battre" },
            { "F", "Grimper dans un arbre / redescendre" },
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

            GUI.Label(new Rect(x, y, bw, UiStyle.S(38)), UiStyle.Spaced("COMMANDES"), UiStyle.Title);
            y += UiStyle.S(42);
            UiStyle.Rule(new Rect(x, y, bw, 1f));
            y += UiStyle.S(16);

            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0) UiStyle.Fill(new Rect(x - UiStyle.S(8), y - UiStyle.S(2), bw + UiStyle.S(16), UiStyle.S(26)),
                                             new Color(1f, 1f, 1f, 0.03f));
                Rect key = new Rect(x, y + UiStyle.S(1), UiStyle.S(150), UiStyle.S(22));
                UiStyle.Pill(key);
                GUIStyle keyStyle = UiStyle.CenteredSmall;
                UiStyle.Tinted(key, Controls[i, 0], keyStyle, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(170), y, bw - UiStyle.S(170), UiStyle.S(24)), Controls[i, 1], UiStyle.Small);
                y += UiStyle.S(26);
            }

            y = box.yMax - UiStyle.S(58);
            if (GUI.Button(new Rect(x, y, bw, UiStyle.S(40)), "Retour", UiStyle.ButtonPrimary)) showControls = false;
        }
    }
}
