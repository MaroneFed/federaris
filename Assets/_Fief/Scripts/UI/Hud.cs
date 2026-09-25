using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// Le HUD : l'horloge de la Saison, le sac, la relique, l'invite d'interaction,
    /// et des reperes vers ce qui t'appartient.
    ///
    /// PRESQUE RIEN A L'ECRAN, et c'est voulu : ce qu'on aime dans ce jeu, c'est la
    /// vue. Chaque element ici doit meriter sa place.
    ///
    /// LES REPERES. On voit ou sont TON camp et TES caches -- tu sais ou tu as
    /// enterre tes affaires, meme dans la brume. On voit le chateau, qui est le seul
    /// lieu que tout le monde connait. On ne voit PAS le mage : on le trouve a
    /// l'oreille. Un repere sur le mage tuerait la chasse.
    ///
    /// Il ne decide RIEN. Il lit l'etat du jeu et l'affiche. La souris, la pause et
    /// le verrouillage des entrees sont geres au meme endroit, dans Menus.cs.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public OrbitCamera orbitCamera;
        public PlayerInteractor interactor;
        public Camera viewCamera;
        public Menus menus;

        IPanel panel;
        // La liste des commandes ne s'affiche plus d'office (Martin : "je ne veux pas
        // la liste en haut") : F1 l'ouvre. Les objectifs du debut guident a la place.
        bool showHelp = false;
        bool showDiagnostic;
        float helpTimer = 0f;

        public bool PanelOpen { get { return panel != null; } }

        // --- la carte de trouvaille (talisman, lieu-dit)
        string cardKicker, cardTitle, cardLine1, cardLine2;
        Color cardTint;
        float cardTimer;
        const float CardDuration = 6.5f;

        /// <summary>
        /// Une grande carte au milieu du haut de l'ecran, pour les moments qui
        /// comptent : un talisman trouve, un lieu-dit decouvert. Un toast se rate ;
        /// ca, non.
        /// </summary>
        public void ShowDiscovery(string kicker, string title, string line1, string line2, Color tint)
        {
            cardKicker = kicker;
            cardTitle = title;
            cardLine1 = line1;
            // Trois lignes au plus (Martin, 26/09 : "trop de texte") : la
            // quatrieme n'est plus affichee, quel que soit l'appelant.
            cardLine2 = "";
            cardTint = tint;
            cardTimer = CardDuration;
        }

        // --- les coups, la chute
        float hurtFlash;
        float deathTimer;
        string killedBy;

        /// <summary>Vrai pendant les quelques secondes ou l'on est tombe : les entrees sont figees.</summary>
        public bool Dead { get { return deathTimer > 0f; } }

        public void Hurt()
        {
            hurtFlash = 1f;
            // Un panneau ouvert fige le joueur : mordu devant sa stele ou sur la carte,
            // il ne pouvait ni fuir ni frapper. Un coup referme tout.
            if (panel != null) ClosePanel();
        }

        /// <summary>"how" : comment on est tombe ("sous les coups de Mahaut", "dans un piege").</summary>
        public void ShowDeath(string how)
        {
            killedBy = string.IsNullOrEmpty(how) ? "" : char.ToUpperInvariant(how[0]) + how.Substring(1);
            deathTimer = Combat.RespawnSeconds;
            ClosePanel();
            Sfx.Bell();
        }

        void TickLife()
        {
            if (hurtFlash > 0f) hurtFlash = Mathf.Max(0f, hurtFlash - Time.unscaledDeltaTime * 1.6f);
            Seeker me = Game.Me;
            if (me == null) return;

            if (deathTimer > 0f)
            {
                deathTimer -= Time.deltaTime;
                if (deathTimer <= 0f)
                {
                    // Se relever, les mains vides, a sa stele.
                    Vector3 at = Combat.RespawnPoint(me, me.Body != null ? me.Body.position : Vector3.zero);
                    if (Game.Player != null) Game.Player.Teleport(at, Game.PlayerTransform.eulerAngles.y);
                    me.Health = Seeker.MaxHealth;
                }
                return;
            }
            // La vie remonte apres huit secondes sans coup -- et vite, pres de sa stele
            // (le seul endroit ou l'on est chez soi).
            if (me.Alive && Stele.NearOwn(me, 8f) && Time.time - me.LastHurt > 2f) me.Heal(18f * Time.deltaTime);
            else if (me.Alive && Time.time - me.LastHurt > 8f) me.Heal(3f * Time.deltaTime);
        }

        void DrawLife()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            if (hurtFlash > 0f)
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.6f, 0.02f, 0.02f, hurtFlash * 0.35f));

            if (deathTimer > 0f)
            {
                float a = Mathf.Clamp01((Combat.RespawnSeconds - deathTimer) / 0.8f);
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.01f, 0.01f, a * 0.92f));
                GUIStyle big = UiStyle.Big;
                TextAnchor previous = big.alignment;
                big.alignment = TextAnchor.MiddleCenter;
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.38f, Screen.width, UiStyle.S(70)), UiStyle.Spaced("TU ES TOMBÉ"), big,
                               new Color(0.85f, 0.3f, 0.25f, a));
                big.alignment = previous;

                return;
            }

            // La barre de vie : seulement quand elle n'est pas pleine.
            if (me.Health < Seeker.MaxHealth - 0.5f)
            {
                float w = UiStyle.S(260);
                Rect bar = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(114), w, UiStyle.S(8));
                UiStyle.Bar(bar, me.Health / Seeker.MaxHealth, new Color(0.75f, 0.16f, 0.12f), UiStyle.BarBg);
            }
        }

        float flash;
        Color flashTint;

        public void OpenPanel(IPanel newPanel) { panel = newPanel; }

        /// <summary>On vient de deposer : sa pastille du classement s'illumine.</summary>
        public void FlashScore() { scoreFlash = 1f; }
        float scoreFlash;

        /// <summary>
        /// LA MICRO-PAUSE D'IMPACT : quand ton epee touche, le temps s'arrete un
        /// vingtieme de seconde. C'est ce qui fait qu'un coup "porte" dans tous les
        /// jeux d'action. (La pause, elle, met le temps a 0 : on n'y touche pas.)
        /// </summary>
        public static void HitStop(float seconds)
        {
            if (!Mathf.Approximately(Time.timeScale, 1f)) return;
            Time.timeScale = 0.05f;
            hitStop = seconds;
        }
        static float hitStop;

        /// <summary>Le sac est plein et on a voulu prendre : ses cases clignotent en rouge.</summary>
        public static void FlashBag() { bagFlash = 1f; }
        static float bagFlash;
        public void ClosePanel() { panel = null; }

        void Update()
        {
            Toasts.Tick(Time.unscaledDeltaTime);
            if (hitStop > 0f)
            {
                hitStop -= Time.unscaledDeltaTime;
                if (hitStop <= 0f && Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = 1f;
            }
            if (scoreFlash > 0f) scoreFlash = Mathf.Max(0f, scoreFlash - Time.unscaledDeltaTime * 0.8f);
            if (cardTimer > 0f) cardTimer -= Time.unscaledDeltaTime;
            TickLife();
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.3f);
            // La carte : on dévoile ce qu'on traverse, et M l'ouvre.
            if (Game.PlayerTransform != null)
            {
                // En haut d'un grand arbre ou sur la terrasse du donjon, on voit loin :
                // la carte se devoile largement.
                Vector3 p = Game.PlayerTransform.position;
                float above = p.y - Ground.Sample(p.x, p.z);
                Atlas.Track(p, above > 12f ? 4 : above > 6f ? 2 : 1);
            }
            if (FiefInput.MapPressed && menus != null && !menus.Blocking)
            {
                if (panel is MapPanel) ClosePanel();
                else if (panel == null) { OpenPanel(new MapPanel()); Sfx.Pop(); }
            }

            FloatingTexts.Tick(Time.unscaledDeltaTime);

            if (panel != null && !panel.IsStillValid) panel = null;

            if (FiefInput.DiagnosticPressed) showDiagnostic = !showDiagnostic;

            if (FiefInput.HelpPressed)
            {
                showHelp = !showHelp;
                helpTimer = showHelp ? 99999f : 0f;
            }
            else if (helpTimer > 0f && helpTimer < 99999f)
            {
                helpTimer -= Time.deltaTime;
                if (helpTimer <= 0f) showHelp = false;
            }
        }

        bool Hidden { get { return menus != null && menus.Blocking; } }

        void OnGUI()
        {
            UiStyle.Ensure();
            if (Hidden) return;

            DrawMarkers();
            FloatingTexts.Draw(viewCamera != null ? viewCamera : Camera.main);
            DrawSeason();
            DrawPack();
            Builder.Draw();
            DrawPrompt();
            DrawDigging();
            DrawTools();
            DrawDiscovery();
            Toasts.Draw();
            if (!showHelp) Objectives.Draw();
            DrawHelp();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();

            DrawDanger();
            if (panel != null) panel.Draw();
            DrawLife();
        }

        /// <summary>
        /// On te court apres (un garde, un rival arme) : les bords de l'ecran battent
        /// en rouge, au rythme d'un coeur. Pas un mot : on comprend.
        /// </summary>
        void DrawDanger()
        {
            if (!Guard.HuntingPlayer && !Rival.HuntingPlayer) return;
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)), 6f);
            Color c = new Color(0.7f, 0.05f, 0.03f, 0.18f + 0.22f * beat);
            float e = UiStyle.S(40);
            UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, e), c);
            UiStyle.FadeBand(new Rect(0f, Screen.height - e, Screen.width, e), c);
            UiStyle.Fill(new Rect(0f, 0f, e * 0.5f, Screen.height), new Color(c.r, c.g, c.b, c.a * 0.7f));
            UiStyle.Fill(new Rect(Screen.width - e * 0.5f, 0f, e * 0.5f, Screen.height), new Color(c.r, c.g, c.b, c.a * 0.7f));
        }

        // ---------------------------------------------------------------- horloge

        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        /// <summary>Huit directions, pour dire "au nord-est" plutot qu'un angle.</summary>
        public static string Direction(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float angle = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            string[] names = { "au nord", "au nord-est", "à l'est", "au sud-est",
                               "au sud", "au sud-ouest", "à l'ouest", "au nord-ouest" };
            return names[Mathf.RoundToInt(angle / 45f) % 8];
        }

        /// <summary>
        /// Le haut de l'ecran : la boussole, l'horloge de la Saison, et dessous LE
        /// CLASSEMENT : quatre pastilles, une par chercheur, a sa couleur, avec le
        /// butin (★) qui dort dans sa stele. La tienne a un bord dore. On sait
        /// toujours qui mene -- et qui aller piller.
        /// </summary>
        void DrawSeason()
        {
            Season season = Game.Season;
            if (season == null) return;

            Transform eye = viewCamera != null ? viewCamera.transform : null;
            float bandW = Mathf.Min(UiStyle.S(620), Screen.width - UiStyle.S(40));
            Rect band = new Rect((Screen.width - bandW) * 0.5f, UiStyle.S(14), bandW, UiStyle.S(30));
            Compass.UrgeStele = Game.Hoard != null && Game.Hoard.Carried >= 10;
            if (eye != null && Game.PlayerTransform != null) Compass.Draw(band, eye, Game.PlayerTransform.position);

            // --- la cloche
            float left = season.Remaining;
            bool late = left < 180f;
            float cw = UiStyle.S(104), ch = UiStyle.S(34);
            float top = band.yMax + UiStyle.S(34);
            Rect plate = new Rect((Screen.width - cw) * 0.5f, top, cw, ch);
            GUI.Box(plate, GUIContent.none, UiStyle.CardBox);
            GUIStyle clockStyle = UiStyle.Value;
            TextAnchor previous = clockStyle.alignment;
            clockStyle.alignment = TextAnchor.MiddleCenter;
            Color clock = late ? Color.Lerp(new Color(0.95f, 0.42f, 0.3f), UiStyle.Ink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)) : UiStyle.Ink;
            UiStyle.Tinted(plate, Clock(left), clockStyle, clock);
            clockStyle.alignment = previous;

            // --- le classement
            ranking.Clear();
            ranking.AddRange(Game.Seekers);
            ranking.Sort((a, b) => b.Score.CompareTo(a.Score));
            float pw = UiStyle.S(78), ph = UiStyle.S(24), gap = UiStyle.S(6);
            float total = ranking.Count * pw + (ranking.Count - 1) * gap;
            float x = (Screen.width - total) * 0.5f;
            float y = plate.yMax + UiStyle.S(6);
            for (int i = 0; i < ranking.Count; i++)
            {
                Seeker s = ranking[i];
                Rect r = new Rect(x + i * (pw + gap), y, pw, ph);
                UiStyle.Fill(r, new Color(0.04f, 0.035f, 0.03f, 0.72f));
                if (s.IsPlayer && scoreFlash > 0f) UiStyle.Fill(r, new Color(1f, 0.8f, 0.35f, 0.5f * scoreFlash));
                if (s.IsPlayer)
                {
                    Color g = Palette.Gold;
                    UiStyle.Fill(new Rect(r.x, r.y, r.width, 1f), g);
                    UiStyle.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), g);
                    UiStyle.Fill(new Rect(r.x, r.y, 1f, r.height), g);
                    UiStyle.Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), g);
                }
                UiStyle.Fill(new Rect(r.x + 2f, r.y + 3f, UiStyle.S(4), r.height - 6f), s.Colour);
                UiStyle.Tinted(new Rect(r.x + UiStyle.S(10), r.y, UiStyle.S(24), r.height), (i + 1).ToString(), UiStyle.Tiny, UiStyle.InkFaint);
                GUIStyle st = UiStyle.Label;
                TextAnchor was = st.alignment;
                st.alignment = TextAnchor.MiddleRight;
                UiStyle.Tinted(new Rect(r.x, r.y, r.width - UiStyle.S(8), r.height), "★" + s.Score, st, s.IsPlayer ? Palette.Gold : UiStyle.Ink);
                st.alignment = was;
            }
        }

        readonly System.Collections.Generic.List<Seeker> ranking = new System.Collections.Generic.List<Seeker>();

        /// <summary>Une pastille : une icone, un chiffre, et un filet qui se vide (fill &lt; 0 : pas de filet).</summary>
        static void Pill(Rect r, UiStyle.Shape shape, Color icon, string text, Color ink, float fill)
        {
            GUI.Box(r, GUIContent.none, UiStyle.CardBox);
            float d = UiStyle.S(11);
            UiStyle.Icon(new Rect(r.x + UiStyle.S(10), r.center.y - d * 0.5f, d, d), shape, icon);
            GUIStyle st = UiStyle.Label;
            TextAnchor was = st.alignment;
            st.alignment = TextAnchor.MiddleRight;
            UiStyle.Tinted(new Rect(r.x, r.y, r.width - UiStyle.S(10), r.height), text, st, ink);
            st.alignment = was;
            if (fill >= 0f)
                UiStyle.Fill(new Rect(r.x + 2f, r.yMax - 3f, (r.width - 4f) * Mathf.Clamp01(fill), 2f), new Color(icon.r, icon.g, icon.b, 0.8f));
        }

        // ---------------------------------------------------------------- le sac

        /// <summary>
        /// L'INVENTAIRE, refait le 26/09 (Martin : "que l'inventaire soit mieux") :
        /// une seule barre de cases en bas de l'ecran, comme dans tous les jeux de
        /// survie, au lieu d'un cadre de texte dans un coin.
        ///
        ///   [1 hache][2 epee]   [bois 12][pierre 3][fer 0]   [relique][or 45]
        ///                        ======== poids ========
        ///
        /// Chaque case a son pictogramme (voir Pictos) et un chiffre. Au-dessus du
        /// sac, la jauge de poids ; au-dessus de la relique, les six talismans ;
        /// au-dessus des outils, les Autels que tu tiens.
        /// </summary>
        void DrawPack()
        {
            Inventory inv = Game.Inventory;
            Seeker me = Game.Me;
            if (inv == null || me == null) return;
            Hoard hoard = me.Hoard;
            Kit kit = me.Kit;

            float size = UiStyle.S(54), gap = UiStyle.S(5), group = UiStyle.S(20);
            float total = size * 6f + gap * 3f + group * 2f;
            float x = (Screen.width - total) * 0.5f;
            float y = Screen.height - size - UiStyle.S(18);

            // --- les outils (1, 2)
            float toolsX = x;
            for (int i = 0; i < 2; i++)
            {
                Rect r = new Rect(x, y, size, size);
                bool active = kit.Active == i;
                Slot(r, active);
                UiStyle.Tinted(new Rect(r.x + UiStyle.S(5), r.y + UiStyle.S(2), UiStyle.S(20), UiStyle.S(16)), (i + 1).ToString(), UiStyle.Tiny,
                               active ? Palette.Gold : UiStyle.InkFaint);
                Tool t = kit.Slots[i];
                if (t != null)
                {
                    Pictos.Draw(Inset(r, 0.14f), Pictos.Of(t.Kind), false);
                    UiStyle.Bar(new Rect(r.x + UiStyle.S(7), r.yMax - UiStyle.S(8), r.width - UiStyle.S(14), UiStyle.S(3)),
                                (float)t.Durability / t.Max, t.Durability * 4 <= t.Max ? new Color(0.95f, 0.45f, 0.35f) : new Color(0.75f, 0.77f, 0.8f), UiStyle.BarBg);
                }
                x += size + gap;
            }
            x += group - gap;

            // --- le sac : trois ressources
            float bagX = x;
            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int amount = inv.Get(type);
                Rect r = new Rect(x, y, size, size);
                Slot(r, false);
                Pictos.Draw(Inset(r, 0.16f), Pictos.Of(type), amount <= 0);
                if (amount > 0) Count(r, amount.ToString(), UiStyle.Ink);
                x += size + gap;
            }
            float bagW = x - gap - bagX;
            x += group - gap;

            // Le sac plein qui refuse : ses trois cases rougissent un instant.
            if (bagFlash > 0f)
            {
                bagFlash = Mathf.Max(0f, bagFlash - Time.unscaledDeltaTime * 1.6f);
                UiStyle.Fill(new Rect(bagX, y, bagW, size), new Color(0.9f, 0.2f, 0.15f, 0.35f * bagFlash));
            }

            // La jauge de poids, au-dessus des trois cases du sac.
            float load = inv.Load01;
            Color fill = Color.Lerp(new Color(0.5f, 0.78f, 0.45f), new Color(0.9f, 0.33f, 0.26f), Mathf.Pow(load, 0.85f));
            bool full = load >= 0.98f;
            if (full) fill = Color.Lerp(fill, Color.white, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f));
            Rect gauge = new Rect(bagX, y - UiStyle.S(9), bagW, UiStyle.S(4));
            UiStyle.Bar(gauge, load, fill, UiStyle.BarBg);
            UiStyle.Tinted(new Rect(bagX, gauge.y - UiStyle.S(18), bagW, UiStyle.S(16)),
                           full ? "PLEIN" : Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg",
                           RightSmall(), full ? new Color(0.95f, 0.45f, 0.35f) : UiStyle.InkFaint);
            // --- le butin porte : ce qu'il faut rapporter a sa stele
            {
                Rect r = new Rect(x, y, size, size);
                int carried = hoard.Carried;
                Slot(r, carried > 0);
                Pictos.Draw(Inset(r, 0.16f), Pictos.Kind.Or, carried <= 0);
                if (carried > 0)
                {
                    Count(r, "★" + carried, Palette.Gold);
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                    UiStyle.Fill(new Rect(r.x, r.y - UiStyle.S(4), r.width, 2f), new Color(0.95f, 0.78f, 0.35f, 0.4f + 0.5f * pulse));
                }
                x += size;
            }

            // Les Autels que tu tiens, au-dessus des outils : un carre par autel, et
            // l'or qu'ils viennent de verser.
            int held = 0;
            for (int i = 0; i < Monument.All.Count; i++)
            {
                Monument m = Monument.All[i];
                if (m == null || m.Owner != me) continue;
                float d = UiStyle.S(10);
                UiStyle.Icon(new Rect(toolsX + held * (d + UiStyle.S(5)), y - UiStyle.S(15), d, d), UiStyle.Shape.Square, Monument.Tint(m.kind));
                held++;
            }
            if (held > 0 && Monument.PayFlash > 0f)
            {
                Color gold = new Color(0.95f, 0.78f, 0.35f, Mathf.Clamp01(Monument.PayFlash));
                UiStyle.Tinted(new Rect(toolsX + held * UiStyle.S(15) + UiStyle.S(4), y - UiStyle.S(20), UiStyle.S(80), UiStyle.S(18)),
                               Monument.LastPay, UiStyle.Small, gold);
            }
        }

        static Rect Inset(Rect r, float f)
        {
            float d = r.width * f;
            return new Rect(r.x + d, r.y + d, r.width - d * 2f, r.height - d * 2f);
        }

        /// <summary>Une case : fond sombre, bord dore quand elle est active.</summary>
        static void Slot(Rect r, bool active)
        {
            UiStyle.Fill(r, new Color(0.04f, 0.035f, 0.03f, 0.72f));
            Color edge = active ? Palette.Gold : new Color(0.55f, 0.44f, 0.26f, 0.45f);
            UiStyle.Fill(new Rect(r.x, r.y, r.width, 1f), edge);
            UiStyle.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), edge);
            UiStyle.Fill(new Rect(r.x, r.y, 1f, r.height), edge);
            UiStyle.Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), edge);
            if (active) UiStyle.Fill(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), new Color(0.86f, 0.7f, 0.36f, 0.08f));
        }

        /// <summary>Le chiffre en bas a droite d'une case, avec une ombre.</summary>
        static void Count(Rect r, string text, Color c)
        {
            GUIStyle st = RightSmall();
            Rect at = new Rect(r.x, r.yMax - UiStyle.S(19), r.width - UiStyle.S(5), UiStyle.S(18));
            UiStyle.Tinted(new Rect(at.x + 1f, at.y + 1f, at.width, at.height), text, st, new Color(0f, 0f, 0f, 0.9f));
            UiStyle.Tinted(at, text, st, c);
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

        // ---------------------------------------------------------------- invite

        void DrawPrompt()
        {
            if (panel != null || interactor == null) return;

            IInteractable target = interactor.Current;
            if (target == null) return;

            // Une touche et quelques mots ; la barre dessous se remplit si on doit maintenir.
            GUIStyle label = UiStyle.Label;
            float textW = label.CalcSize(new GUIContent(target.Prompt)).x;
            float capW = UiStyle.S(30);
            float w = Mathf.Min(Screen.width - UiStyle.S(40), textW + capW + UiStyle.S(40));
            float h = UiStyle.S(40);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(210), w, h);
            UiStyle.Fill(box, new Color(0.03f, 0.025f, 0.02f, 0.7f));

            Rect cap = new Rect(box.x + UiStyle.S(8), box.y + (h - capW) * 0.5f, capW, capW);
            UiStyle.Pill(cap);
            UiStyle.Tinted(cap, "E", UiStyle.Centered, Palette.Gold);
            GUI.Label(new Rect(cap.xMax + UiStyle.S(10), box.y, box.width - capW - UiStyle.S(24), h), target.Prompt, label);

            if (target.HoldDuration > 0f)
            {
                Rect bar = new Rect(box.x, box.yMax - UiStyle.S(3), box.width, UiStyle.S(3));
                UiStyle.Bar(bar, interactor.HoldProgress01, Palette.Gold, new Color(0f, 0f, 0f, 0.5f));
            }
        }

        /// <summary>"F|↑;clic|3/5" : une rangee de touches dessinees, chacune suivie d'un signe.</summary>
        static void KeyHints(string hint, float y)
        {
            string[] pairs = hint.Split(';');
            float cap = UiStyle.S(24), gap = UiStyle.S(18);
            float total = 0f;
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] kv = pairs[i].Split('|');
                float keyW = Mathf.Max(cap, UiStyle.Tiny.CalcSize(new GUIContent(kv[0])).x + UiStyle.S(12));
                float valW = kv.Length > 1 ? UiStyle.Label.CalcSize(new GUIContent(kv[1])).x + UiStyle.S(6) : 0f;
                total += keyW + valW + (i > 0 ? gap : 0f);
            }
            float x = (Screen.width - total) * 0.5f;
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] kv = pairs[i].Split('|');
                float keyW = Mathf.Max(cap, UiStyle.Tiny.CalcSize(new GUIContent(kv[0])).x + UiStyle.S(12));
                Rect k = new Rect(x, y, keyW, cap);
                UiStyle.Pill(k);
                UiStyle.Tinted(k, kv[0], UiStyle.CenteredSmall, Palette.Gold);
                x += keyW + UiStyle.S(6);
                if (kv.Length > 1)
                {
                    float valW = UiStyle.Label.CalcSize(new GUIContent(kv[1])).x;
                    UiStyle.Tinted(new Rect(x, y, valW + 4f, cap), kv[1], UiStyle.Label, new Color(0.95f, 0.9f, 0.8f, 0.9f));
                    x += valW;
                }
                x += gap;
            }
        }

        // ---------------------------------------------------------------- les outils

        /// <summary>
        /// La barre d'outils : deux emplacements en bas au centre (1 et 2), l'outil,
        /// son usure. Et au centre de l'ecran, un point de visee quand on tient un
        /// outil, avec ce qu'on peut faire ("Clic : abattre l'arbre").
        /// </summary>
        void DrawTools()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            // Les cases d'outils sont dessinees avec le sac (DrawPack) ; ici, ce qui
            // se passe au centre de l'ecran.
            float y = Screen.height - UiStyle.S(54) - UiStyle.S(18) - UiStyle.S(26);

            // Le reticule : un point discret ; un losange dore quand quelque chose est a
            // portee de main (E) ; rouge quand un ennemi est a portee d'epee.
            {
                float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
                bool foe = ToolUser.FoeInReach;
                bool usable = interactor != null && interactor.Current != null && panel == null;
                if (foe || usable)
                {
                    float d = UiStyle.S(foe ? 12 : 10);
                    Color c = foe ? new Color(1f, 0.35f, 0.28f, 0.9f) : new Color(1f, 0.85f, 0.5f, 0.75f);
                    UiStyle.Icon(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), UiStyle.Shape.Diamond, c);
                    float inner2 = d * 0.5f;
                    UiStyle.Icon(new Rect(cx - inner2 * 0.5f, cy - inner2 * 0.5f, inner2, inner2), UiStyle.Shape.Diamond, new Color(0.05f, 0.04f, 0.03f, 0.8f));
                }
                else if (ToolUser.Aiming)
                {
                    float d = UiStyle.S(4);
                    UiStyle.Icon(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), UiStyle.Shape.Dot, new Color(1f, 1f, 1f, 0.7f));
                }
            }
            // Sous le reticule : des touches dessinees, pas des phrases ("F ↑" : grimper).
            if (!string.IsNullOrEmpty(ToolUser.Hint) && panel == null) KeyHints(ToolUser.Hint, Screen.height * 0.5f + UiStyle.S(24));
        }

        // ---------------------------------------------------------------- trouvaille

        void DrawDiscovery()
        {
            if (cardTimer <= 0f || string.IsNullOrEmpty(cardTitle)) return;

            // Entree en 0,4 s, sortie en 0,8 s.
            float age = CardDuration - cardTimer;
            float alpha = Mathf.Clamp01(age / 0.4f) * Mathf.Clamp01(cardTimer / 0.8f);
            bool detailed = !string.IsNullOrEmpty(cardLine1);

            float w = UiStyle.S(520);
            float h = UiStyle.S(detailed ? 130 : 86);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(170) - (1f - Mathf.Clamp01(age / 0.4f)) * UiStyle.S(12), w, h);

            if (flash > 0f) UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height),
                                         new Color(flashTint.r, flashTint.g, flashTint.b, flash * flash * 0.45f));

            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            UiStyle.Frame(box);
            UiStyle.Fill(new Rect(box.x + UiStyle.S(10), box.y, box.width - UiStyle.S(20), 2f), cardTint);

            float y = box.y + UiStyle.S(12);
            UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(16)), cardKicker, UiStyle.CenteredSmall, UiStyle.InkDim);
            y += UiStyle.S(18);
            GUIStyle title = UiStyle.Title;
            TextAnchor previous = title.alignment;
            title.alignment = TextAnchor.MiddleCenter;
            UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(40)), cardTitle, title, cardTint);
            title.alignment = previous;
            y += UiStyle.S(44);
            if (detailed)
            {
                UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(22)), cardLine1, UiStyle.Centered, UiStyle.Ink);
                y += UiStyle.S(26);
                UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(18)), cardLine2, UiStyle.CenteredSmall, UiStyle.InkFaint);
            }
            GUI.color = was;
        }

        // ---------------------------------------------------------------- creusage

        /// <summary>La jauge de G maintenu : a la place de l'invite, au meme endroit.</summary>
        void DrawDigging()
        {
            if (!CampActions.Digging) return;

            float w = UiStyle.S(300);
            float h = UiStyle.S(44);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(276), w, h);
            UiStyle.DropShadow(box, UiStyle.S(14));
            GUI.Box(box, GUIContent.none, UiStyle.CardBox);

            float pad = UiStyle.S(14);
            GUI.Label(new Rect(box.x + pad, box.y + UiStyle.S(4), w - pad * 2f, UiStyle.S(22)),
                      "Tu creuses...", UiStyle.Label);
            UiStyle.Bar(new Rect(box.x + pad, box.y + UiStyle.S(28), w - pad * 2f, UiStyle.S(8)),
                        CampActions.Progress01, new Color(0.80f, 0.66f, 0.46f), UiStyle.BarBg);
        }

        // ---------------------------------------------------------------- reperes

        void DrawMarkers()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || Game.PlayerTransform == null) return;
            Vector3 me = Game.PlayerTransform.position;

            // Le chateau, le camp, les caches, les steles et les lieux-dits sont sur
            // la BOUSSOLE (Compass.cs). Ici ne restent que les gens : les gardes, les
            // rivaux, et le voleur qu'on poursuit.

            // Les gardes : "?" quand ils se doutent, "!" quand ils courent.
            for (int i = 0; i < Guard.All.Count; i++)
            {
                Guard g = Guard.All[i];
                if (g == null || FlatDistance(me, g.transform.position) > 40f) continue;
                if (g.Chasing) DrawAlert(cam, g.transform.position + Vector3.up * 2.9f, "!", new Color(1f, 0.3f, 0.2f), 1f);
                else if (g.Suspicion > 0.03f)
                    DrawAlert(cam, g.transform.position + Vector3.up * 2.9f, "?", new Color(1f, 0.85f, 0.3f), g.Suspicion);
            }

            // Les rivaux : leur nom au-dessus de la tete quand on est pres. Et celui
            // qui emporte TA relique, on le voit toujours : c'est une chasse.
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r == null) continue;
                float d = FlatDistance(me, r.transform.position);
                if (d < 16f && r.seeker.Alive)
                    DrawMarker(cam, r.transform.position + Vector3.up * 2.4f,
                               r.seeker.Hoard.Carried > 0 ? r.seeker.Name + "  ★" + r.seeker.Hoard.Carried : r.seeker.Name, r.seeker.Colour);
            }
        }

        /// <summary>Un grand signe au-dessus d'une tete ("?", "!"), qui grossit avec l'alerte.</summary>
        void DrawAlert(Camera cam, Vector3 world, string sign, Color color, float level)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            float size = UiStyle.S(26 + 20 * Mathf.Clamp01(level));
            Rect r = new Rect(sp.x - size, Screen.height - sp.y - size, size * 2f, size * 2f);
            GUIStyle big = UiStyle.Big;
            int previous = big.fontSize;
            TextAnchor align = big.alignment;
            big.fontSize = Mathf.RoundToInt(size);
            big.alignment = TextAnchor.MiddleCenter;
            UiStyle.Tinted(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), sign, big, new Color(0f, 0f, 0f, 0.7f));
            UiStyle.Tinted(r, sign, big, color);
            big.fontSize = previous;
            big.alignment = align;
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void DrawMarker(Camera cam, Vector3 world, string text, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;

            float margin = UiStyle.S(66);
            float x = Mathf.Clamp(sp.x, margin, Screen.width - margin);
            float y = Mathf.Clamp(Screen.height - sp.y, margin, Screen.height - margin);

            string label = text;
            if (Game.PlayerTransform != null)
                label += "  " + Mathf.RoundToInt(FlatDistance(Game.PlayerTransform.position, world)) + " m";

            float dotSize = UiStyle.S(8);
            UiStyle.Fill(new Rect(x - dotSize * 0.5f, y - dotSize * 0.5f, dotSize, dotSize),
                         new Color(0f, 0f, 0f, 0.5f));
            UiStyle.Fill(new Rect(x - dotSize * 0.5f + 1f, y - dotSize * 0.5f + 1f, dotSize - 2f, dotSize - 2f), color);

            GUIStyle style = UiStyle.CenteredSmall;
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(x - UiStyle.S(75) + 1f, y - UiStyle.S(27) + 1f, UiStyle.S(150), UiStyle.S(20)), label, style);
            style.normal.textColor = new Color(color.r, color.g, color.b, 0.9f);
            GUI.Label(new Rect(x - UiStyle.S(75), y - UiStyle.S(27), UiStyle.S(150), UiStyle.S(20)), label, style);
            style.normal.textColor = original;
        }

        // ---------------------------------------------------------------- diagnostic

        /// <summary>
        /// Une panne pendant la construction du monde s'affiche en grand, en rouge.
        /// Plus besoin d'aller chercher dans la Console : le jeu dit ce qui a casse.
        /// </summary>
        void DrawBuildError()
        {
            if (string.IsNullOrEmpty(Game.BuildError)) return;

            float w = Mathf.Min(Screen.width - UiStyle.S(40), UiStyle.S(760));
            float h = UiStyle.S(150);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(120), w, h);

            UiStyle.DropShadow(box, UiStyle.S(18));
            UiStyle.Fill(box, new Color(0.22f, 0.05f, 0.05f, 0.96f));
            UiStyle.Fill(new Rect(box.x, box.y, box.width, 3f), new Color(0.90f, 0.25f, 0.20f));

            float x = box.x + UiStyle.S(16);
            UiStyle.Tinted(new Rect(x, box.y + UiStyle.S(10), w, UiStyle.S(26)),
                           "LA CONSTRUCTION DU MONDE A ÉCHOUÉ", UiStyle.Head, new Color(1f, 0.55f, 0.45f));

            GUIStyle wrapped = UiStyle.Small;
            bool previousWrap = wrapped.wordWrap;
            wrapped.wordWrap = true;
            GUI.Label(new Rect(x, box.y + UiStyle.S(38), w - UiStyle.S(32), h - UiStyle.S(48)),
                      Game.BuildError, wrapped);
            wrapped.wordWrap = previousWrap;
        }

        /// <summary>
        /// Panneau F3. Il repond a la question "je suis dans quoi ?" : il liste ce
        /// dont la boite englobante contient la camera, et ce que touche un rayon
        /// tire vers l'avant. C'est ce qui remplace les allers-retours a l'aveugle.
        /// </summary>
        void DrawDiagnostic()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;

            float w = UiStyle.S(500);
            float h = UiStyle.S(377);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(90), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float y = box.y + UiStyle.S(12);
            float inner = w - UiStyle.S(32);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "DIAGNOSTIC  (F3)", UiStyle.Head);
            y += UiStyle.S(26);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            y = Line(x, y, inner, "Monde construit en", Game.BuildMilliseconds + " ms");

            // Tout le reglage de la lumiere depend de cet espace, et le depot ne le
            // versionne pas : c'est Unity qui le choisit sur chaque machine.
            y = Line(x, y, inner, "Espace colorimetrique",
                     QualitySettings.activeColorSpace == ColorSpace.Linear ? "linéaire" : "gamma");
            y = Line(x, y, inner, "Vue",
                     orbitCamera != null && orbitCamera.ThroughEyes ? "première personne" : "écran-titre");

            if (Game.PlayerTransform != null)
            {
                Vector3 p = Game.PlayerTransform.position;
                y = Line(x, y, inner, "Joueur",
                         p.x.ToString("0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0"));
                y = Line(x, y, inner, "Sol sous les pieds", Ground.Sample(p.x, p.z).ToString("0.0") + " m");

            }

            if (cam != null)
            {
                Vector3 c = cam.transform.position;
                y = Line(x, y, inner, "Oeil",
                         c.x.ToString("0.0") + " / " + c.y.ToString("0.0") + " / " + c.z.ToString("0.0"));

                // --- DANS QUOI SOMMES-NOUS ?
                //
                // "Contains" ne repondait qu'a moitie : un objet peut remplir l'ecran
                // sans contenir l'oeil -- c'est le cas d'un vetement, dont l'oeil sort
                // par le col. On mesure donc aussi la DISTANCE a chaque morceau du
                // corps, et on nomme les deux plus proches. C'est cette ligne-la qui
                // dit en un mot ce qui bouche la vue.
                string inside = "rien";
                string nearest = "rien";
                if (Game.Rig != null)
                {
                    Renderer[] parts = Game.Rig.GetComponentsInChildren<Renderer>(false);

                    string firstName = null, secondName = null;
                    float firstDist = 99f, secondDist = 99f;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        // ShadowsOnly = la piece porte encore son ombre mais n'est
                        // plus dessinee : elle ne peut donc plus boucher la vue.
                        if (parts[i] == null || !parts[i].enabled) continue;
                        if (parts[i].shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;

                        if (inside == "rien" && parts[i].bounds.Contains(c))
                            inside = parts[i].gameObject.name + " (ton personnage)";

                        float d = Vector3.Distance(parts[i].bounds.ClosestPoint(c), c);
                        if (d < firstDist)
                        {
                            secondDist = firstDist; secondName = firstName;
                            firstDist = d; firstName = parts[i].gameObject.name;
                        }
                        else if (d < secondDist)
                        {
                            secondDist = d; secondName = parts[i].gameObject.name;
                        }
                    }

                    if (firstName != null)
                    {
                        nearest = firstName + " a " + Mathf.RoundToInt(firstDist * 100f) + " cm";
                        if (secondName != null)
                            nearest += ",  " + secondName + " a " + Mathf.RoundToInt(secondDist * 100f) + " cm";
                    }
                }
                y = Line(x, y, inner, "Caméra à l'intérieur de", inside);
                y = Line(x, y, inner, "Colle à l'oeil", nearest);

                Collider[] touching = Physics.OverlapSphere(c, 0.25f, ~0, QueryTriggerInteraction.Ignore);
                y = Line(x, y, inner, "Solides autour de l'oeil",
                         touching.Length == 0 ? "aucun" : touching[0].gameObject.name
                             + (touching.Length > 1 ? " +" + (touching.Length - 1) : ""));

                RaycastHit hit;
                string ahead = "rien à moins de 40 m";
                if (Physics.Raycast(c, cam.transform.forward, out hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                    ahead = hit.collider.gameObject.name + " a " + hit.distance.ToString("0.0") + " m";
                y = Line(x, y, inner, "Devant toi", ahead);
            }

            y += UiStyle.S(6);
            GUI.Label(new Rect(x, y, inner, UiStyle.S(34)),
                      "Lis-moi \"Colle à l'oeil\" : c'est ce qui est devant la caméra.", UiStyle.Tiny);
        }

        float Line(float x, float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, width * 0.52f, UiStyle.S(20)), label, UiStyle.Small);
            UiStyle.Tinted(new Rect(x + width * 0.52f, y, width * 0.48f, UiStyle.S(20)), value,
                           UiStyle.Small, Palette.Gold);
            return y + UiStyle.S(21);
        }

        // ---------------------------------------------------------------- aide

        void DrawHelp()
        {
            if (!showHelp) return;

            float w = UiStyle.S(340);
            float h = UiStyle.S(384);
            Rect box = new Rect(Screen.width - w - UiStyle.S(16), UiStyle.S(16), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float inner = w - UiStyle.S(32);
            float y = box.y + UiStyle.S(12);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(22)), "COMMANDES", UiStyle.Head);
            GUIStyle right = UiStyle.Tiny;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(22)), "F1", right);
            right.alignment = previous;

            y += UiStyle.S(26);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            string[,] rows =
            {
                { "ZQSD", "se déplacer" },
                { "Maj", "courir" },
                { "Souris", "caméra" },
                { "E", "prendre, déposer, piller" },
                { "C", "planter le camp" },
                { "G", "creuser une cache" },
                { "T", "construire" },
                { "1 / 2 + clic", "épée, hache" },
                { "F", "grimper dans un arbre" },
                { "H", "écouter ta stèle" },
                { "M", "la carte" },
                { "Échap", "pause" }
            };

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                UiStyle.Tinted(new Rect(x, y, UiStyle.S(78), UiStyle.S(20)), rows[i, 0], UiStyle.Small, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(84), y, inner - UiStyle.S(84), UiStyle.S(20)), rows[i, 1], UiStyle.Small);
                y += UiStyle.S(21);
            }
        }

        static GUIStyle rightSmall;
        static GUIStyle RightSmall()
        {
            if (rightSmall == null || rightSmall.fontSize != UiStyle.Small.fontSize)
            {
                rightSmall = new GUIStyle(UiStyle.Small);
                rightSmall.alignment = TextAnchor.MiddleRight;
            }
            return rightSmall;
        }
    }
}
