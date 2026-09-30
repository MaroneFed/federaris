using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ECRAN DE JEU (refait le 30/09, sans un mot -- "plus pro, comme Fall Guys" -- et
    /// net au pixel le 01/10). Des pastilles et des icones :
    ///
    ///            [chrono]                                  [toi      2]
    ///          o o o o o     <- une pastille par manche     [Mahaut   1]
    ///       [Couronne  Oswin]  <- ou elle est / qui l'a     [Oswin    0]
    ///                              .      <- le point de visee
    ///            (O)  <- LE REPERE DE LA COURONNE, a sa place dans le monde (02/10)
    ///                         [E  couronne]  <- l'invite
    ///
    ///            (passive)   ( TA CAPACITE )   (pousser)
    ///
    /// Quand TU portes la Couronne (02/10) : l'ecran se borde d'or, et les trois
    /// Monuments ont leur repere. Reglages > Aide ecrite : quelques mots sous les icones.
    ///
    /// Il ne decide RIEN : il lit l'etat du jeu et le montre. La pause, les ecrans entre
    /// les manches : Menus.cs. Les icones : Icons.cs / IconArt.cs.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public OrbitCamera orbitCamera;
        public PlayerInteractor interactor;
        public Camera viewCamera;
        public Menus menus;

        bool showDiagnostic;

        // --- le grand moment (Couronne prise, don recu...) : une icone
        string cardIcon;
        Color cardTint;
        float cardTimer;
        const float CardDuration = 3.2f;

        /// <summary>Un titre au milieu du haut de l'ecran, pour les moments qui comptent.</summary>
        public void ShowSplash(string icon, Color tint) { ShowSplash(icon, tint, null); }

        /// <summary>La meme chose, avec quelques mots dessous (seulement si Reglages > Aide ecrite).</summary>
        public void ShowSplash(string icon, Color tint, string words)
        {
            splashCross = false;
            splashWords = words;
            cardIcon = icon;
            cardTint = tint;
            cardTimer = CardDuration;
        }

        // --- les voiles
        float hurtFlash;
        float flash;
        Color flashTint;

        /// <summary>Un coup : l'ecran blanchit, et le bord d'ou il vient rougit.</summary>
        public void Hurt(Vector3 velocity)
        {
            hurtFlash = 1f;
            Vector3 from = -new Vector3(velocity.x, 0f, velocity.z);
            if (from.sqrMagnitude < 0.01f || viewCamera == null) return;
            Vector3 f = viewCamera.transform.forward, r = viewCamera.transform.right;
            f.y = 0f; r.y = 0f;
            hitSide = new Vector2(Vector3.Dot(from.normalized, r.normalized), Vector3.Dot(from.normalized, f.normalized));
            hitSideTimer = 0.8f;
        }
        Vector2 hitSide;
        float hitSideTimer;

        public void Flash(Color tint)
        {
            flash = 1f;
            flashTint = tint;
        }

        /// <summary>
        /// LA MICRO-PAUSE D'IMPACT : quand ta poussee touche, le temps s'arrete un
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

        void Update()
        {
            Toasts.Tick(Time.unscaledDeltaTime);
            if (hitStop > 0f)
            {
                hitStop -= Time.unscaledDeltaTime;
                if (hitStop <= 0f && Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = 1f;
            }
            if (cardTimer > 0f) cardTimer -= Time.unscaledDeltaTime;
            if (tipTimer > 0f) tipTimer -= Time.unscaledDeltaTime;
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.4f);
            if (hurtFlash > 0f) hurtFlash = Mathf.Max(0f, hurtFlash - Time.unscaledDeltaTime * 2f);
            if (hitSideTimer > 0f) hitSideTimer -= Time.unscaledDeltaTime;
            if (FiefInput.DiagnosticPressed) showDiagnostic = !showDiagnostic;
            if (FiefInput.KeysPressed && !Hidden) keysOpen = !keysOpen;
            if (Game.Season != null && Game.Season.Running && !Hidden)
            {
                WatchReady();
                WatchTips();
                TickLastSeconds(Game.Season.Remaining);
            }
        }

        // ================================================================== ce qui se surveille

        readonly Dictionary<Ability, bool> wasReady = new Dictionary<Ability, bool>();
        readonly Dictionary<Ability, float> readyFlash = new Dictionary<Ability, float>();

        /// <summary>Une capacite revient : une note claire, et sa ligne s'eclaire un instant.</summary>
        void WatchReady()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            List<Ability> list = new List<Ability>();
            if (me.HasActive) list.Add(me.CurrentActive);
            for (int i = 0; i < list.Count; i++)
            {
                Ability a = list[i];
                bool r = me.Ready(a, Time.time);
                bool was;
                if (wasReady.TryGetValue(a, out was) && !was && r)
                {
                    readyFlash[a] = Time.unscaledTime;
                    Sfx.Beep(1.5f);
                }
                wasReady[a] = r;
            }
        }

        int lastTick = -1;

        /// <summary>Les dix dernieres secondes : un battement par seconde.</summary>
        void TickLastSeconds(float left)
        {
            int s = Mathf.CeilToInt(left);
            if (s == lastTick) return;
            lastTick = s;
            if (s <= 10 && s > 0) Sfx.Beep(s <= 3 ? 1.2f : 0.8f);
        }

        // ================================================================== les astuces

        /// <summary>Les astuces deja montrees pendant ce match (une seule fois chacune).</summary>
        static readonly HashSet<string> tipsShown = new HashSet<string>();
        static int tipsMatch = -1;

        /// <summary>Une astuce, une seule fois par match, au moment ou elle sert.</summary>
        public void Tip(string key, string text)
        {
            // (30/09 -- "je deteste le texte") : plus d'astuces ECRITES. La phrase reste
            // dans le code (elle dit l'intention) ; a l'ecran, une pastille d'ICONES :
            // la touche, puis ce qu'elle fait (v13).
            if (tipsShown.Contains(key)) return;
            tipsShown.Add(key);
            string[] icons;
            if (!TipIcons.TryGetValue(key, out icons)) return;
            tipIcons = icons;
            TipWords.TryGetValue(key, out tipWords);
            tipTimer = TipSeconds;
        }

        /// <summary>
        /// LES ASTUCES EN ICONES : "k:E" une touche, "k:use" la touche d'interaction,
        /// "k:active" celle de la capacite, "k:push" celle de la poussee ; le reste, une icone.
        /// </summary>
        static readonly Dictionary<string, string[]> TipIcons = new Dictionary<string, string[]>
        {
            { "plateforme", new[] { "k:use", "arbaleste", "k:Clic gauche", "haut" } },
            { "arbaleste", new[] { "k:use", "arbaleste", "k:Clic gauche", "cible" } },
            { "rampe", new[] { "tour", "haut", "couronne" } },
            { "chasse", new[] { "k:push", "pousser", "couronne" } },
            { "porte", new[] { "couronne", "courant", "monument", "sacre" } },
            { "don", new[] { "don", "k:active" } },
            { "sceau", new[] { "ailes", "croix" } },
        };
        /// <summary>Les memes astuces en quelques mots (sous les icones, si l'aide ecrite est la).</summary>
        static readonly Dictionary<string, string> TipWords = new Dictionary<string, string>
        {
            { "plateforme", "E : MONTE SUR TON ARBALESTE, CLIC : TIRE" },
            { "arbaleste", "E : MONTE DESSUS, CLIC : TIRE" },
            { "rampe", "MONTE JUSQU'À LA COURONNE, TOUT EN HAUT" },
            { "chasse", "POUSSE LE PORTEUR : TU LUI VOLES LA COURONNE" },
            { "porte", "VA À UN MONUMENT (COLONNE BLEUE) ET RESTES-Y 3 S" },
            { "don", "TON CLIC GAUCHE A CHANGÉ POUR CETTE MANCHE" },
            { "sceau", "ON N'ENTRE PAS EN VOLANT : PASSE PAR UNE PORTE" },
        };
        const float TipSeconds = 5f;
        string tipWords;
        string[] tipIcons;
        float tipTimer;

        /// <summary>
        /// LES ASTUCES AU BON MOMENT (27/09 -- « on comprend rien ») : pas de tutoriel,
        /// une phrase quand on en a besoin. Le premier pas sur la rampe, le premier
        /// courant, la premiere Couronne en main, le premier Oeil qui te voit.
        /// </summary>
        void WatchTips()
        {
            if (tipsMatch != Match.MatchId) { tipsMatch = Match.MatchId; tipsShown.Clear(); }
            Seeker me = Game.Me;
            if (me == null || me.Body == null) return;
            // Le panneau des touches est la : une chose a la fois a l'ecran.
            if (KeysAlpha() > 0.01f) return;
            Vector3 p = me.Body.position;
            if (me.CarriesCrown) Tip("porte", "La Couronne est LOURDE : tu planes mal. Tourne dans un courant d'air (colonne blanche) pour remonter, ou prends une arbaleste sur l'île. Au Monument (colonne bleue), reste 3 s dans le cercle.");
            else if (Spawns.OnPad(p)) Tip("plateforme", "Ta plateforme. E : monte sur TON arbaleste, clic gauche : elle te pose devant le château. Puis passe la porte et monte la tour.");
            else if (Ballista.NearestFree(p, 7f) != null) Tip("arbaleste", "Une arbaleste géante : E pour monter dessus, maintiens le clic gauche pour tendre, relâche pour être tiré.");
            else if (Tower.On(p) && Tower.Progress(p) > 0.2f) Tip("obstacle", "Un obstacle qui te touche t'éjecte de la tour : regarde les bandes ambre au sol, et passe entre deux coups.");
            else if (Tower.On(p)) Tip("rampe", "La rampe monte jusqu'à la Couronne. Pousse les autres dans le vide : " + AbilityInfo.PushKey.ToLowerInvariant() + ".");
            else if (Crown.Holder != null) Tip("chasse", Crown.Holder.Name + " porte la Couronne : pousse-le (" + AbilityInfo.PushKey.ToLowerInvariant() + ") pour la lui VOLER.");
            else if (me.HasGift) Tip("don", "Le don du sanctuaire remplace ton clic gauche, pour cette manche.");
        }

        bool Hidden { get { return menus != null && menus.Blocking; } }

        void OnGUI()
        {
            UiStyle.Ensure();
            if (Hidden) return;

            // (02/10 -- "des fois on ne voit plus les touches, des fois tout se barre, on ne
            // voit plus rien") : chaque morceau du HUD est dessine A PART. Avant, si un seul
            // plantait (un joueur qui disparait entre deux images...), tout ce qui venait
            // apres disparaissait -- et s'il plantait en pleine transparence, il laissait
            // GUI.color a zero : plus rien ne se voyait. Maintenant, un morceau qui plante
            // ne cache que lui, et la couleur est remise a chaque fois (l'erreur va une seule
            // fois dans la Console, pour qu'on la corrige).
            Part(0); Part(1); Part(2); Part(3); Part(4); Part(5); Part(6); Part(7);
            Part(8); Part(9); Part(10); Part(11); Part(12); Part(13);
            if (showDiagnostic) Part(14);
            if (FiefInput.ScoresHeld) Part(15);
        }

        static readonly string[] PartNames = { "voiles", "porteur", "pseudos", "repere de la Couronne", "haut", "scores", "capacites", "centre",
                                               "invite", "carte", "astuce", "touches", "fil", "erreur", "diagnostic", "tableau des scores" };
        readonly bool[] partFailed = new bool[16];

        void Part(int k)
        {
            try
            {
                switch (k)
                {
                    case 0: DrawVeils(); break;
                    case 1: DrawCarrying(); break;
                    case 2: DrawNames(); break;
                    case 3: DrawCrownMarker(); break;
                    case 4: DrawTop(); break;
                    case 5: DrawStandings(); break;
                    case 6: DrawAbilities(); break;
                    case 7: DrawCentre(); break;
                    case 8: DrawPrompt(); break;
                    case 9: DrawCard(); break;
                    case 10: DrawTip(); break;
                    case 11: DrawKeys(); break;
                    case 12: Toasts.Draw(); break;
                    case 13: DrawBuildError(); break;
                    case 14: DrawDiagnostic(); break;
                    case 15: DrawScores(); break;
                }
            }
            catch (System.Exception e)
            {
                if (!partFailed[k]) { partFailed[k] = true; Debug.LogError("[FIEF] HUD : « " + PartNames[k] + " » a plante (le reste du HUD continue) -- " + e); }
            }
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
        }

        // ================================================================== le haut

        /// <summary>
        /// EN HAUT (30/09, sans un mot) : le chrono dans sa pastille, une pastille par
        /// manche (a la couleur de son gagnant), et la Couronne -- ou elle est, en icones.
        /// </summary>
        void DrawTop()
        {
            Season season = Game.Season;
            if (season == null) return;
            float left = season.Remaining;
            float cx = Screen.width * 0.5f;

            // LE CHRONO : bleu roi ; les dix dernieres secondes, il rougit et bat.
            float pw = UiStyle.S(176), ph = UiStyle.S(58);
            Rect pill = new Rect(cx - pw * 0.5f, UiStyle.S(14), pw, ph);
            bool late = left <= 10f && left > 0f;
            float beat = late ? Mathf.Pow(1f - Mathf.Repeat(left, 1f), 2f) : 0f;
            Color fill = late ? Color.Lerp(new Color(0.78f, 0.16f, 0.22f), new Color(1f, 0.42f, 0.3f), beat) : new Color(0.22f, 0.3f, 0.72f);
            if (late) { float g = UiStyle.S(10) * beat; pill = new Rect(pill.x - g, pill.y - g * 0.4f, pill.width + g * 2f, pill.height + g * 0.8f); }
            Icons.Pill(pill, fill);
            float ic = pill.height * 0.7f;
            Icons.Draw(new Rect(pill.x + pill.height * 0.2f, pill.y + (pill.height - ic) * 0.5f, ic, ic), "chrono", Color.white);
            Icons.Number(new Rect(pill.x + ic * 0.8f, pill.y, pill.width - ic * 0.8f, pill.height), Clock(left), Mathf.RoundToInt(pill.height * 0.6f), Color.white, TextAnchor.MiddleCenter);

            // LES MANCHES : une pastille chacune.
            float y = pill.yMax + UiStyle.S(12);
            if (Match.IsTieBreak) Icons.Draw(new Rect(cx - UiStyle.S(15), y - UiStyle.S(4), UiStyle.S(30), UiStyle.S(30)), "drapeau", new Color(1f, 0.5f, 0.35f));
            else
            {
                int n = Match.Rounds;
                float dot = UiStyle.S(16), gap = UiStyle.S(7);
                float x = cx - (n * dot + (n - 1) * gap) * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    Rect d = new Rect(x + i * (dot + gap), y, dot, dot);
                    int won = i < Match.History.Count ? Match.History[i] : -2;
                    bool current = i == Match.Played;
                    if (current) { float p = UiStyle.S(3) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)); d = new Rect(d.x - p, d.y - p, d.width + p * 2f, d.height + p * 2f); }
                    Color c = won >= 0 && won < Match.Slots.Count ? Match.ColourOf(won) : won == -1 ? new Color(0.55f, 0.55f, 0.62f) : current ? Wings.Gold : new Color(0.16f, 0.17f, 0.3f);
                    Icons.Pill(d, c);
                }
            }
            y += UiStyle.S(30);

            // LA COURONNE : une pastille a la couleur de qui la tient ; dedans, la Couronne et ou elle est.
            CrownBadge(cx, y);

            // Sur une arbaleste : ou l'on va atterrir, en icone.
            if (Ballista.PlayerOn != null)
            {
                Ballista b = Ballista.PlayerOn;
                string where = b.Landing == Ballista.LandingKind.Ward ? "croix" : b.Landing == Ballista.LandingKind.Monument ? "monument"
                             : b.Landing == Ballista.LandingKind.Ground ? "coche" : "ailes";
                Color wc = b.Landing == Ballista.LandingKind.Ward ? new Color(0.95f, 0.3f, 0.3f) : b.Landing == Ballista.LandingKind.Monument ? Monument.Blue
                         : b.Landing == Ballista.LandingKind.Ground ? new Color(0.45f, 0.9f, 0.5f) : Wings.Glow;
                float s = UiStyle.S(64);
                Rect chip = new Rect(cx - s * 1.3f, Screen.height * 0.5f + UiStyle.S(60), s * 2.6f, s);
                Icons.Pill(chip, new Color(0.18f, 0.2f, 0.34f, 0.92f));
                Icons.Draw(new Rect(chip.x + s * 0.2f, chip.y + s * 0.12f, s * 0.76f, s * 0.76f), "arbaleste", Color.white);
                Icons.Draw(new Rect(chip.x + s * 1.02f, chip.y + s * 0.2f, s * 0.6f, s * 0.6f), "retour", new Color(1f, 1f, 1f, 0.6f), false);
                Icons.Draw(new Rect(chip.xMax - s * 0.96f, chip.y + s * 0.12f, s * 0.76f, s * 0.76f), where, wc);
                // (02/10 -- "on ne voit pas tres bien ou on vise") : OU TU VAS TOMBER, un repere a
                // l'ecran (la cible, a la couleur de l'arrivee, et la distance) -- colle au bord
                // s'il sort de l'image. L'arbaleste ne le cache plus jamais.
                Camera view = viewCamera != null ? viewCamera : Camera.main;
                if (b.HasLanding && view != null && Game.PlayerTransform != null)
                    Pin(view, b.LandingPoint + Vector3.up * 1.2f, "cible", Color.white, wc, UiStyle.S(54), 1f, (b.LandingPoint - Game.PlayerTransform.position).magnitude);
                if (b.Charging)
                {
                    Rect bar = new Rect(chip.x + s * 0.3f, chip.yMax + UiStyle.S(10), chip.width - s * 0.6f, UiStyle.S(12));
                    Icons.Pill(bar, new Color(0.1f, 0.1f, 0.18f));
                    if (b.Tension > 0.02f) Icons.Pill(new Rect(bar.x, bar.y, Mathf.Max(bar.height, bar.width * b.Tension), bar.height), Wings.Gold);
                }
            }
            DrawTowerGauge();
        }

        /// <summary>
        /// OU EST LA COURONNE, en haut : au sommet (la Couronne et la tour), a terre (et
        /// les secondes avant qu'elle rentre), sur une tete -- et alors LE PSEUDO du
        /// porteur, a sa couleur (02/10 : "on ne comprenait rien" ; un pseudo, ca se lit).
        /// Quand c'est toi : la pastille grossit, en or, et bat.
        /// </summary>
        void CrownBadge(float cx, float y)
        {
            Seeker holder = Crown.Holder;
            Crown.State where = Crown.Where;
            bool mine = where == Crown.State.Carried && holder != null && holder.IsPlayer;
            float h = UiStyle.S(mine ? 66 : 50);
            float ic = h * 0.8f;
            int fs = Mathf.RoundToInt(h * 0.48f / 2f) * 2;
            string name = where == Crown.State.Carried && holder != null ? holder.Name : null;
            string second = null;
            string number = null;
            Color fill;
            if (name != null) fill = mine ? Wings.Gold : holder.Colour;
            else if (where == Crown.State.Dropped)
            {
                // A terre (02/10 : elle ne rentre plus au sommet) : a quelle distance de toi.
                fill = new Color(0.95f, 0.55f, 0.2f);
                Seeker m = Game.Me;
                number = m != null && m.Body != null ? Mathf.RoundToInt((Crown.Position - m.Body.position).magnitude) + "m" : "";
            }
            else if (where == Crown.State.Delivered) { fill = Monument.Blue; second = "monument"; }
            else { fill = new Color(0.36f, 0.3f, 0.62f); second = "tour"; }
            float inner = name != null ? Icons.Width(name, fs) + UiStyle.S(10) : number != null ? Icons.Width(number, fs) + UiStyle.S(10) : ic;
            float w = h * 0.2f + ic + UiStyle.S(8) + inner + h * 0.3f;
            Rect r = new Rect(cx - w * 0.5f, y, w, h);
            if (mine)
            {
                float pulse = UiStyle.S(5) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
                Icons.Pill(new Rect(r.x - pulse - UiStyle.S(4), r.y - pulse * 0.5f - UiStyle.S(4), r.width + pulse * 2f + UiStyle.S(8), r.height + pulse + UiStyle.S(8)), new Color(1f, 0.95f, 0.7f, 0.9f));
            }
            Icons.Pill(r, fill);
            Rect crownIcon = new Rect(r.x + h * 0.2f, r.y + (h - ic) * 0.5f, ic, ic);
            Icons.Draw(crownIcon, "couronne", CrownGold);
            // (v13) TON VERROU : on vient de te la voler (ou tu viens de la lacher) -- tu ne
            // peux pas la reprendre tout de suite. Une croix rouge sur la Couronne, le temps
            // du verrou (avant : rien, on ne comprenait pas pourquoi on ne la reprenait pas).
            Seeker me = Game.Me;
            if (me != null && !me.CarriesCrown && Time.time < me.CrownLockUntil)
            {
                float k = ic * 0.62f;
                Icons.Draw(new Rect(crownIcon.xMax - k * 0.7f, crownIcon.yMax - k * 0.8f, k, k), "croix", new Color(1f, 0.35f, 0.3f));
            }
            Rect right = new Rect(crownIcon.xMax + UiStyle.S(8), r.y, inner, h);
            if (name != null) Icons.Text(right, name, fs, Color.white, TextAnchor.MiddleLeft, true);
            if (mine) Caption(cx, r.yMax + UiStyle.S(10), "TU AS LA COURONNE : VA À UN MONUMENT", UiStyle.S(22), CrownGold);
            else if (number != null) Icons.Number(right, number, fs, Color.white, TextAnchor.MiddleLeft);
            else Icons.Draw(new Rect(right.x, r.y + (h - ic) * 0.5f, ic, ic), second, Color.white);
        }

        /// <summary>
        /// LA JAUGE DE LA TOUR (a gauche, quand on y est) : les six bandes de couleur, du
        /// pied au sommet, et ta pastille a ta hauteur. La Couronne en haut.
        /// </summary>
        void DrawTowerGauge()
        {
            Seeker me = Game.Me;
            if (me == null || me.Body == null || !Tower.On(me.Body.position)) return;
            float x = UiStyle.S(30), w = UiStyle.S(16), h = Mathf.Min(UiStyle.S(300), Screen.height * 0.38f);
            float top = Screen.height * 0.5f - h * 0.5f;
            Icons.Pill(new Rect(x - UiStyle.S(4), top - UiStyle.S(4), w + UiStyle.S(8), h + UiStyle.S(8)), new Color(0.1f, 0.1f, 0.18f, 0.85f));
            for (int k = 0; k < Tower.Turns; k++)
            {
                float seg = h / Tower.Turns;
                Color c = Tower.ColourAt((k + 0.5f) * Tower.Height / Tower.Turns);
                UiStyle.Fill(new Rect(x, top + h - (k + 1) * seg + 1f, w, seg - 2f), c);
            }
            Icons.Draw(new Rect(x + w * 0.5f - UiStyle.S(20), top - UiStyle.S(46), UiStyle.S(40), UiStyle.S(40)), "couronne", new Color(1f, 0.86f, 0.35f));
            float p = Mathf.Clamp01(Tower.Progress(me.Body.position));
            float my = top + h * (1f - p);
            float s = UiStyle.S(34);
            Rect mark = new Rect(x + w * 0.5f - s * 0.5f, my - s * 0.5f, s, s);
            Icons.Pill(mark, Wings.Gold);
            Icons.Draw(new Rect(mark.x + s * 0.14f, mark.y + s * 0.14f, s * 0.72f, s * 0.72f), "joueur", Color.white, false);
        }

        /// <summary>
        /// EN HAUT A DROITE : une pastille par joueur, a sa couleur, et ses manches gagnees
        /// (une petite Couronne et le chiffre). Toi : cerne d'or.
        /// </summary>
        void DrawStandings()
        {
            // (02/10) Le pseudo de chacun dans sa pastille : une couleur seule ne disait
            // pas QUI etait qui. Puis la petite Couronne et ses manches gagnees.
            float h = UiStyle.S(34);
            int fs = Mathf.RoundToInt(h * 0.5f / 2f) * 2;
            float nameW = 0f;
            for (int i = 0; i < Match.Slots.Count; i++) nameW = Mathf.Max(nameW, Icons.Width(Match.Slots[i].Name, fs));
            nameW = Mathf.Min(nameW, UiStyle.S(170));
            float w = h * 0.3f + nameW + UiStyle.S(8) + h * 0.84f + h * 0.8f;
            float x = Screen.width - w - UiStyle.S(22), y = UiStyle.S(20);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                Rect r = new Rect(x, y, w, h);
                // (02/10) Qui a la Couronne : sa ligne sort de la colonne et bat, en or.
                bool holds = Crown.Holder != null && Crown.Holder.Index == s.Index;
                if (holds) r.x -= UiStyle.S(18) + UiStyle.S(4) * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
                if (s.IsLocal || holds) Icons.Pill(new Rect(r.x - UiStyle.S(4), r.y - UiStyle.S(4), r.width + UiStyle.S(8), r.height + UiStyle.S(8)), holds ? new Color(1f, 0.93f, 0.6f) : Wings.Gold);
                Icons.Pill(r, s.Colour);
                int size = fs;
                while (size > 10 && Icons.Width(s.Name, size) > nameW) size -= 2;
                Icons.Text(new Rect(r.x + h * 0.3f, r.y, nameW, h), s.Name, size, Color.white, TextAnchor.MiddleLeft, true);
                float cx = r.x + h * 0.3f + nameW + UiStyle.S(8);
                Icons.Draw(new Rect(cx, r.y + h * 0.08f, h * 0.84f, h * 0.84f), "couronne", CrownGold);
                Icons.Number(new Rect(cx + h * 0.84f, r.y, h * 0.8f, h), s.Wins.ToString(), Mathf.RoundToInt(h * 0.66f), Color.white, TextAnchor.MiddleCenter);
                y += h + UiStyle.S(10);
            }
        }

        // ================================================================== les capacites

        /// <summary>
        /// EN BAS (30/09, sans un mot) : au milieu, ton clic gauche -- un gros rond a sa
        /// couleur, son icone, sa touche (une souris ou une lettre), sa recharge qui
        /// descend ; a gauche, ta passive ; a droite, la poussee. Au-dessus : ton etat du
        /// moment en petites pastilles (etourdi, gele, invisible, protege, en vol...).
        /// </summary>
        void DrawAbilities()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            float now = Time.time;
            float cx = Screen.width * 0.5f;
            float big = UiStyle.S(96), small = UiStyle.S(66);
            float y = Screen.height - UiStyle.S(30) - big;

            if (me.HasActive) AbilityTile(new Rect(cx - big * 0.5f, y, big, big), AbilityInfo.Keys[0], me.CurrentActive, me, now, me.HasGift, AbilityUser.AimingSlot == 0);

            // La passive, a gauche.
            List<Ability> all = me.Slot.Abilities;
            for (int i = 0; i < all.Count; i++)
            {
                if (AbilityInfo.IsActive(all[i])) continue;
                Rect pr = new Rect(cx - big * 0.5f - UiStyle.S(26) - small, y + big - small, small, small);
                Icons.Pill(pr, Color.Lerp(AbilityInfo.Tint(all[i]), new Color(0.2f, 0.18f, 0.36f), 0.55f));
                Icons.Draw(new Rect(pr.x + small * 0.16f, pr.y + small * 0.16f, small * 0.68f, small * 0.68f), Icons.Of(all[i]), Color.white);
                break;
            }

            // La poussee, a droite : la main, sa touche, sa recharge.
            Rect sr = new Rect(cx + big * 0.5f + UiStyle.S(26), y + big - small, small, small);
            Icons.Pill(sr, new Color(0.85f, 0.32f, 0.3f));
            Icons.Draw(new Rect(sr.x + small * 0.16f, sr.y + small * 0.16f, small * 0.68f, small * 0.68f), "pousser", Color.white);
            if (Time.time < me.ShoveReadyAt)
            {
                float total = Seeker.ShoveCooldown * (me.Has(Ability.Poigne) ? 0.6f : 1f);
                Icons.Cooldown(sr, Mathf.Clamp01((me.ShoveReadyAt - Time.time) / total));
            }
            Icons.Key(new Rect(sr.xMax - small * 0.36f, sr.yMax - small * 0.36f, small * 0.5f, small * 0.5f), AbilityInfo.PushKey, 1f);

            // L'etat du moment : de petites pastilles, au-dessus.
            List<string> states = new List<string>();
            List<Color> tints = new List<Color>();
            if (me.Stunned) { states.Add("etourdi"); tints.Add(new Color(1f, 0.85f, 0.4f)); }
            if (me.Slowed) { states.Add("gel"); tints.Add(AbilityInfo.Tint(Ability.Gel)); }
            if (me.Hidden) { states.Add("voile"); tints.Add(AbilityInfo.Tint(Ability.Voile)); }
            if (me.Graced) { states.Add("bouclier"); tints.Add(new Color(0.7f, 0.85f, 1f)); }
            if (Game.Player != null && Game.Player.Gliding && Thermal.LiftAt(me.Body.position) > 0.5f) { states.Add("courant"); tints.Add(new Color(0.75f, 0.92f, 1f)); }
            else if (me.HasWings || me.Has(Ability.Planeur)) { states.Add("ailes"); tints.Add(Wings.Gold); }
            if (me.CarriesCrown) { states.Add("couronne"); tints.Add(new Color(1f, 0.86f, 0.35f)); }
            float st = UiStyle.S(42);
            float sx = cx - (states.Count * st + (states.Count - 1) * UiStyle.S(8)) * 0.5f;
            for (int i = 0; i < states.Count; i++)
            {
                Rect r = new Rect(sx + i * (st + UiStyle.S(8)), y - st - UiStyle.S(18), st, st);
                Icons.Pill(r, new Color(0.14f, 0.15f, 0.28f, 0.9f));
                Icons.Draw(new Rect(r.x + st * 0.14f, r.y + st * 0.14f, st * 0.72f, st * 0.72f), states[i], tints[i]);
            }
            // En vol : la vitesse, en chiffres, a droite des pastilles.
            if (Game.Player != null && Game.Player.Gliding)
                Icons.Number(new Rect(cx + UiStyle.S(140), y - st - UiStyle.S(18), UiStyle.S(160), st), Mathf.RoundToInt(Game.Player.Airspeed * 3.6f).ToString(), UiStyle.S(26), Wings.Glow, TextAnchor.MiddleLeft);
        }

        /// <summary>Le gros rond de ta capacite active.</summary>
        void AbilityTile(Rect r, string key, Ability a, Seeker me, float now, bool gift, bool aimingThis)
        {
            bool ready = me.Ready(a, now);
            bool blocked = AbilityCaster.WhyNot(me, a) == "Mains prises";
            bool live = ready && !blocked;
            Color tint = AbilityInfo.Tint(a);
            if (aimingThis) r = new Rect(r.x - UiStyle.S(6), r.y - UiStyle.S(16), r.width + UiStyle.S(12), r.height + UiStyle.S(12));

            // Prete : un halo a sa couleur qui respire ; revenue : un eclat.
            float glow = aimingThis ? 1f : live ? 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
            float lit;
            if (readyFlash.TryGetValue(a, out lit) && Time.unscaledTime - lit < 0.6f) glow = Mathf.Max(glow, 1f - (Time.unscaledTime - lit) / 0.6f);
            if (glow > 0.01f)
            {
                float g = UiStyle.S(10) * glow;
                Icons.Dot(new Rect(r.x - g, r.y - g, r.width + g * 2f, r.height + g * 2f), new Color(tint.r, tint.g, tint.b, 0.45f * glow));
            }
            Icons.Pill(r, live ? Color.Lerp(tint, new Color(0.15f, 0.12f, 0.3f), 0.25f) : new Color(0.28f, 0.28f, 0.36f));
            Icons.Draw(new Rect(r.x + r.width * 0.17f, r.y + r.height * 0.17f, r.width * 0.66f, r.height * 0.66f), Icons.Of(a), live ? Color.white : new Color(0.8f, 0.8f, 0.86f));
            if (!ready)
            {
                Icons.Cooldown(r, 1f - me.Ready01(a, now));
                float rem = me.Remaining(a, now);
                Icons.Number(r, rem < 1f ? rem.ToString("0.0") : Mathf.CeilToInt(rem).ToString(), Mathf.RoundToInt(r.height * 0.36f), Color.white, TextAnchor.MiddleCenter);
            }
            if (blocked) Icons.Draw(new Rect(r.x + r.width * 0.25f, r.y + r.height * 0.25f, r.width * 0.5f, r.height * 0.5f), "croix", new Color(1f, 0.4f, 0.35f));
            // La touche, en bas a gauche ; le don, une etoile en haut a droite.
            float k = r.width * 0.42f;
            Icons.Key(new Rect(r.x - k * 0.2f, r.yMax - k * 0.8f, k, k), key, 1f);
            if (gift) Icons.Draw(new Rect(r.xMax - k * 0.7f, r.y - k * 0.2f, k * 0.8f, k * 0.8f), "don", new Color(0.7f, 0.95f, 1f));
        }

        // ================================================================== le centre

        void DrawCentre()
        {
            Seeker me = Game.Me;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;

            // Le point de visee : blanc, et rouge quand quelqu'un est a portee de poussee.
            bool foe = AbilityUser.FoeInReach;
            float d = UiStyle.S(foe ? 8 : 5);
            Icons.Dot(new Rect(cx - d * 0.5f - 1.5f, cy - d * 0.5f - 1.5f, d + 3f, d + 3f), new Color(0f, 0f, 0f, 0.5f));
            Icons.Dot(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), foe ? new Color(1f, 0.35f, 0.28f, 0.95f) : new Color(1f, 1f, 1f, 0.85f));
            // Le pique d'aigle : le porteur est dans ton viseur, en l'air -- la cible d'or, et l'aigle.
            if (AbilityUser.DiveAt != null)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 10f);
                float s = UiStyle.S(70) * (0.95f + 0.05f * pulse);
                Icons.Draw(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), "cible", new Color(Wings.Gold.r, Wings.Gold.g, Wings.Gold.b, pulse));
                Icons.Draw(new Rect(cx + s * 0.5f, cy - s * 0.2f, s * 0.6f, s * 0.6f), "pique", Wings.Gold);
                Icons.Key(new Rect(cx + s * 1.1f, cy - s * 0.1f, s * 0.46f, s * 0.46f), AbilityInfo.PushKey, pulse);
            }
            // Refuse : une petite croix rouge sous le point.
            float since = Time.time - AbilityUser.RefusalAt;
            if (!string.IsNullOrEmpty(AbilityUser.Refusal) && since < 0.9f)
            {
                float a = 1f - Mathf.Clamp01((since - 0.5f) / 0.4f);
                float s = UiStyle.S(30);
                Icons.Draw(new Rect(cx - s * 0.5f, cy + UiStyle.S(18), s, s), "croix", new Color(1f, 0.4f, 0.35f, a));
            }
            DrawSacre(cy);
        }

        /// <summary>
        /// LE SACRE, a l'ecran de tout le monde : la Couronne et une barre qui se remplit en
        /// trois secondes, a la couleur de qui se fait sacrer. Toi : or. Un autre : sa couleur, et ca bat.
        /// </summary>
        void DrawSacre(float cy)
        {
            Seeker who = Monument.Sacring;
            if (who == null) return;
            float p = Monument.SacreProgress;
            Color c = who.IsPlayer ? Wings.Gold : who.Colour;
            float w = UiStyle.S(360), h = UiStyle.S(26);
            float y = Screen.height * 0.25f;
            float beat = who.IsPlayer ? 0f : UiStyle.S(4) * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));
            Rect bar = new Rect((Screen.width - w) * 0.5f - beat, y - beat * 0.5f, w + beat * 2f, h + beat);
            Icons.Pill(bar, new Color(0.1f, 0.1f, 0.18f, 0.92f));
            if (p > 0.02f) Icons.Pill(new Rect(bar.x, bar.y, Mathf.Max(bar.height, bar.width * p), bar.height), c);
            float s = UiStyle.S(66);
            Icons.Draw(new Rect(Screen.width * 0.5f - s * 0.5f, bar.y - s - UiStyle.S(4), s, s), "sacre", new Color(1f, 0.86f, 0.35f));
            if (!who.IsPlayer) Icons.Draw(new Rect(bar.xMax + UiStyle.S(10), bar.y - UiStyle.S(10), UiStyle.S(46), UiStyle.S(46)), "pousser", c);
            Caption(Screen.width * 0.5f, bar.yMax + UiStyle.S(6), who.IsPlayer ? "RESTE DANS LE CERCLE !" : who.Name + " VA GAGNER : POUSSE-LE !", UiStyle.S(24), who.IsPlayer ? CrownGold : Color.white);
        }

        /// <summary>L'invite d'interaction : la touche E, et l'icone de ce qu'on va prendre ; le cercle qui se remplit pendant le maintien.</summary>
        void DrawPrompt()
        {
            if (interactor == null) return;
            IInteractable target = interactor.Current;
            if (target == null || !target.CanInteract) return;
            string icon = target is Crown ? "couronne" : target is Ballista ? "arbaleste" : target is Shrine ? "don" : target is Monument ? "monument" : "touche";
            float s = UiStyle.S(58);
            float cx = Screen.width * 0.5f, y = Screen.height * 0.5f + UiStyle.S(70);
            Rect chip = new Rect(cx - s * 1.1f, y, s * 2.2f, s);
            Icons.Pill(chip, new Color(0.18f, 0.2f, 0.34f, 0.92f));
            Icons.Key(new Rect(chip.x + s * 0.12f, chip.y + s * 0.1f, s * 0.8f, s * 0.8f), AbilityInfo.UseKey, 1f);
            Icons.Draw(new Rect(chip.xMax - s * 0.96f, chip.y + s * 0.1f, s * 0.8f, s * 0.8f), icon, icon == "couronne" ? new Color(1f, 0.86f, 0.35f) : Color.white);
            if (target.HoldDuration > 0f && interactor.HoldProgress01 > 0f)
            {
                Rect bar = new Rect(chip.x + s * 0.2f, chip.yMax + UiStyle.S(8), chip.width - s * 0.4f, UiStyle.S(10));
                Icons.Pill(bar, new Color(0.1f, 0.1f, 0.18f));
                Icons.Pill(new Rect(bar.x, bar.y, Mathf.Max(bar.height, bar.width * interactor.HoldProgress01), bar.height), Wings.Gold);
            }
        }

        // ================================================================== dans le monde

        /// <summary>Le nom des autres joueurs, a leur couleur, quand ils sont a moins de 22 m.</summary>
        /// <summary>
        /// LES PSEUDOS au-dessus des tetes (29/09 -- Martin : "qu'on voie au-dessus des
        /// personnages leur pseudo, pas leurs conneries") : le nom, rien d'autre, a sa
        /// couleur (en or pour qui porte la Couronne), plus gros de pres, lisible de loin.
        /// </summary>
        void DrawNames()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || Game.PlayerTransform == null) return;
            Vector3 me = cam.transform.position;
            bool flair = Game.Me != null && Game.Me.Has(Ability.Flair);
            // (29/09) Du plus proche au plus loin, et un nom ne se pose jamais sur un autre :
            // on le monte d'un cran, et s'il n'y a pas la place, on ne l'ecrit pas.
            nameOrder.Clear();
            nameDist.Clear();
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.IsPlayer || s.Body == null || s.Hidden && !flair) continue;
                float d = (me - s.Body.position).magnitude;
                int at = 0;
                while (at < nameDist.Count && nameDist[at] < d) at++;
                nameOrder.Insert(at, i);
                nameDist.Insert(at, d);
            }
            namePlaced.Clear();
            for (int n = 0; n < nameOrder.Count; n++)
            {
                Seeker s = Game.Seekers[nameOrder[n]];
                Vector3 head = s.Body.position + Vector3.up * 2.9f;
                float d = (me - head).magnitude;
                if (d > 170f) continue;
                Vector3 sp = cam.WorldToScreenPoint(head);
                if (sp.z <= 0f) continue;
                float a = Mathf.Clamp01((170f - d) / 40f);
                // (01/10) Plus que quelques tailles, en lettres rondes cernees : nettes et lisibles.
                int fs = Mathf.RoundToInt(Mathf.Lerp(UiStyle.S(24), UiStyle.S(14), Mathf.Clamp01(d / 90f)) / 2f) * 2;
                Color c = s.CarriesCrown ? new Color(1f, 0.82f, 0.35f) : Color.Lerp(s.Colour, Color.white, 0.25f);
                Vector2 size = new Vector2(Icons.Width(s.Name, fs) + 8f, fs * 1.4f);
                Rect r = new Rect(sp.x - size.x * 0.5f, Screen.height - sp.y - size.y * 0.5f, size.x, size.y);
                bool free = false;
                for (int tries = 0; tries < 3 && !free; tries++)
                {
                    free = true;
                    for (int k = 0; k < namePlaced.Count; k++)
                        if (namePlaced[k].Overlaps(r)) { free = false; r.y = namePlaced[k].y - r.height - 2f; break; }
                }
                if (!free) continue;
                namePlaced.Add(r);
                Icons.Text(r, s.Name, fs, new Color(c.r, c.g, c.b, a), TextAnchor.MiddleCenter, true);
            }
        }
        readonly List<int> nameOrder = new List<int>();
        readonly List<float> nameDist = new List<float>();
        readonly List<Rect> namePlaced = new List<Rect>();

        // ================================================================== la Couronne, toujours

        /// <summary>
        /// LA COURONNE, TOUJOURS VISIBLE (02/10 -- Martin : "il faut qu'on voie tout le temps
        /// ou est la couronne", "quand il ne la voyait pas, on ne savait jamais ou elle
        /// etait"). Un REPERE a sa place a l'ecran, pour tout le monde, a travers les murs :
        /// une pastille ronde et la Couronne dedans, la distance dessous. Sur son socle ou a
        /// terre : or ; sur une tete : a la couleur du porteur, et elle bat. Hors de l'ecran :
        /// collee au bord, trois points qui montent vers elle. (Avant : seulement avec la
        /// capacite Flair.) Toi, quand tu la portes : pas de repere, le cadre d'or (DrawCarrying).
        /// </summary>
        void DrawCrownMarker()
        {
            Seeker me = Game.Me;
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (me == null || cam == null || me.Body == null || Crown.Instance == null) return;
            if (me.CarriesCrown || Crown.Where == Crown.State.Delivered) return;
            Seeker holder = Crown.Holder;
            Vector3 world = Crown.Position + Vector3.up * (holder != null ? 1.1f : 1.5f);
            float dist = (world - me.Body.position).magnitude;
            Color fill = holder != null ? holder.Colour : Crown.Where == Crown.State.Dropped ? new Color(0.95f, 0.55f, 0.2f) : new Color(0.62f, 0.44f, 0.12f);
            float beat = holder != null ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) : 0f;
            float size = UiStyle.S(50) * (1f + 0.1f * beat);
            // Elle vient de tomber : le repere grossit et bat trois secondes, pour tout le monde.
            float fell = Time.time - Crown.DroppedAt;
            if (holder == null && Crown.Where == Crown.State.Dropped && fell < 3f)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f));
                size *= 1f + (1f - fell / 3f) * (0.6f + 0.3f * pulse);
                fill = Color.Lerp(fill, new Color(1f, 0.3f, 0.2f), pulse);
            }
            // Tout pres (on la voit tres bien) : le repere s'efface, pour ne pas la cacher.
            float alpha = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01((dist - 6f) / 10f));
            Pin(cam, world, "couronne", CrownGold, fill, size, alpha, dist);
        }

        static readonly Color CrownGold = new Color(1f, 0.86f, 0.35f);

        /// <summary>
        /// UN REPERE sur une chose du monde : une pastille ronde a sa couleur, son icone, la
        /// distance dessous. Dans l'image : pose au-dessus de la chose, deux points dessous
        /// comme la pointe d'une epingle. Hors de l'image (ou derriere toi) : colle au bord,
        /// et trois points qui filent vers le bord ou elle se trouve.
        /// </summary>
        static void Pin(Camera cam, Vector3 world, string icon, Color iconTint, Color fill, float size, float alpha, float distance)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z <= 0f;
            float gx = sp.x, gy = Screen.height - sp.y;
            if (behind) { gx = Screen.width - gx; gy = Screen.height - gy; }
            float m = size * 0.5f + UiStyle.S(40);
            Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = new Vector2(gx, gy) - c;
            bool edge = behind || gx < m || gx > Screen.width - m || gy < m || gy > Screen.height - m;
            if (edge)
            {
                if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                float k = Mathf.Min((c.x - m) / Mathf.Max(1f, Mathf.Abs(dir.x)), (c.y - m) / Mathf.Max(1f, Mathf.Abs(dir.y)));
                gx = c.x + dir.x * k;
                gy = c.y + dir.y * k;
                alpha = Mathf.Max(alpha, 0.85f);
            }
            Rect r = new Rect(gx - size * 0.5f, gy - size * 0.5f, size, size);
            Color light = Color.Lerp(fill, Color.white, 0.45f);
            Icons.Dot(new Rect(r.x - UiStyle.S(4), r.y - UiStyle.S(4), r.width + UiStyle.S(8), r.height + UiStyle.S(8)), new Color(light.r, light.g, light.b, 0.9f * alpha));
            Icons.Pill(r, fill, alpha);
            Icons.Draw(new Rect(r.x + size * 0.14f, r.y + size * 0.12f, size * 0.72f, size * 0.72f), icon, new Color(iconTint.r, iconTint.g, iconTint.b, alpha));
            Vector2 n = edge ? dir.normalized : new Vector2(0f, 1f);     // (l'ecran compte y vers le bas)
            for (int i = 0; i < (edge ? 3 : 2); i++)
            {
                float ds = size * (0.2f - i * 0.05f);
                Vector2 p = new Vector2(gx, gy) + n * (size * 0.5f + UiStyle.S(10) + i * UiStyle.S(12));
                Icons.Dot(new Rect(p.x - ds * 0.5f, p.y - ds * 0.5f, ds, ds), new Color(light.r, light.g, light.b, alpha));
            }
            if (distance < 0f) return;
            // La distance, du cote oppose aux points : dessus dans l'image (la pointe est
            // dessous), et au bord, la ou les points ne sont pas.
            bool above = !edge || n.y > 0.3f;
            float ty = above ? r.y - UiStyle.S(26) : r.yMax + UiStyle.S(4);
            Icons.Number(new Rect(gx - UiStyle.S(60), ty, UiStyle.S(120), UiStyle.S(24)), Mathf.RoundToInt(distance) + "m", UiStyle.S(20), new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleCenter);
        }

        // ================================================================== tu l'as

        /// <summary>
        /// TU PORTES LA COURONNE (02/10 -- "quand il l'a, il ne sait meme pas qu'il l'a") :
        /// l'ecran se borde d'OR et bat doucement, tant que tu l'as ; la pastille de la
        /// Couronne, en haut, grossit et porte ton pseudo ; et les TROIS MONUMENTS
        /// s'affichent a leur place, colonnes bleues, avec leur distance (le plus proche,
        /// plus gros) : on sait ou aller. Quand tu la perds : l'or s'eteint d'un coup.
        /// </summary>
        void DrawCarrying()
        {
            Seeker me = Game.Me;
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (me == null || me.Body == null || !me.CarriesCrown) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            EdgeGlow(UiStyle.S(64) + UiStyle.S(18) * pulse, new Color(1f, 0.78f, 0.25f, 0.34f + 0.14f * pulse));
            if (cam == null) return;
            Monument nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < Monument.All.Count; i++)
            {
                Monument mo = Monument.All[i];
                if (mo == null) continue;
                float d = (mo.transform.position - me.Body.position).magnitude;
                if (d < best) { best = d; nearest = mo; }
            }
            for (int i = 0; i < Monument.All.Count; i++)
            {
                Monument mo = Monument.All[i];
                if (mo == null) continue;
                bool close = mo == nearest;
                float d = (mo.transform.position - me.Body.position).magnitude;
                float size = UiStyle.S(close ? 56 : 42) * (close ? 1f + 0.08f * pulse : 1f);
                Color fill = new Color(Monument.Blue.r * 0.55f, Monument.Blue.g * 0.55f, Monument.Blue.b * 0.7f);
                Pin(cam, mo.transform.position + Vector3.up * 8f, "monument", Color.white, fill, size, close ? 1f : 0.8f, d);
            }
        }

        static Texture2D rampV, rampH;

        /// <summary>
        /// Un liseré de couleur sur les quatre bords, qui s'efface vers le milieu (un vrai
        /// degrade, pas une bande). Les coins, ou deux bords se croisent, sont plus vifs.
        /// </summary>
        static void EdgeGlow(float e, Color c)
        {
            if (rampV == null)
            {
                rampV = new Texture2D(1, 64, TextureFormat.RGBA32, false);
                rampH = new Texture2D(64, 1, TextureFormat.RGBA32, false);
                rampV.wrapMode = rampH.wrapMode = TextureWrapMode.Clamp;
                rampV.hideFlags = rampH.hideFlags = HideFlags.HideAndDontSave;
                for (int k = 0; k < 64; k++)
                {
                    float t = k / 63f;
                    float a = t * t * t;
                    rampV.SetPixel(0, k, new Color(1f, 1f, 1f, a));         // haut du rect = bord de l'ecran
                    rampH.SetPixel(63 - k, 0, new Color(1f, 1f, 1f, a));    // gauche du rect = bord de l'ecran
                }
                rampV.Apply();
                rampH.Apply();
            }
            Color was = GUI.color;
            GUI.color = c;
            float w = Screen.width, h = Screen.height;
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, w, e), rampV, new Rect(0f, 0f, 1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(0f, h - e, w, e), rampV, new Rect(0f, 1f, 1f, -1f));
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, e, h), rampH, new Rect(0f, 0f, 1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(w - e, 0f, e, h), rampH, new Rect(1f, 0f, -1f, 1f));
            GUI.color = was;
        }

        /// <summary>
        /// ON TE L'A PRISE : l'or s'eteint, un eclair rouge, la Couronne barree au milieu
        /// de l'ecran (Crown l'appelle, que ce soit un vol, un coup ou une chute).
        /// </summary>
        public void CrownLost()
        {
            ShowSplash("couronne", new Color(0.85f, 0.25f, 0.22f), "TU AS PERDU LA COURONNE");
            splashCross = true;
            Flash(new Color(0.9f, 0.15f, 0.1f, 0.55f));
            Sfx.Lost();
        }
        bool splashCross;
        string splashWords;

        /// <summary>
        /// QUELQUES MOTS SOUS UNE ICONE (Reglages > Aide ecrite ; 02/10). Gros, blancs,
        /// cernes -- le meme dessin que les chiffres. Rien si l'aide est coupee.
        /// </summary>
        static void Caption(float cx, float y, string words, int size, Color c)
        {
            if (!Settings.Help || string.IsNullOrEmpty(words) || c.a <= 0.01f) return;
            float w = Icons.Width(words, size) + UiStyle.S(20);
            Icons.Text(new Rect(cx - w * 0.5f, y, w, size * 1.5f), words, size, c, TextAnchor.MiddleCenter, true);
        }

        // ================================================================== les voiles

        void DrawVeils()
        {
            Seeker me = Game.Me;
            if (flash > 0f)
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(flashTint.r, flashTint.g, flashTint.b, flash * flash * 0.5f * flashTint.a));
            if (hurtFlash > 0f)
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(1f, 0.95f, 0.85f, hurtFlash * hurtFlash * 0.22f));
            if (me == null) return;

            // D'ou vient le coup : le bord de l'ecran de ce cote rougit.
            if (hitSideTimer > 0f)
            {
                float a = Mathf.Clamp01(hitSideTimer / 0.8f) * 0.55f;
                Color red = new Color(0.9f, 0.08f, 0.05f, a);
                float e = UiStyle.S(90);
                if (hitSide.x > 0.4f) UiStyle.FadeBand(new Rect(Screen.width - e, 0f, e, Screen.height), red);
                if (hitSide.x < -0.4f) UiStyle.FadeBand(new Rect(0f, 0f, e, Screen.height), red);
                if (hitSide.y > 0.4f) UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, e), red);
                if (hitSide.y < -0.4f) UiStyle.FadeBand(new Rect(0f, Screen.height - e, Screen.width, e), red);
            }

            // Un Oeil charge sur toi : les bords battent en rouge, de plus en plus vite.
            if (Eye.ChargingAt(me))
            {
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f)), 3f);
                Color c = new Color(0.8f, 0.05f, 0.03f, 0.25f + 0.3f * beat);
                Edges(UiStyle.S(70), c);
                float s = UiStyle.S(54) * (1f + 0.12f * beat);
                Icons.Draw(new Rect(Screen.width * 0.5f - s * 0.5f, Screen.height * 0.5f - UiStyle.S(70) - s * 0.5f, s, s), "oeil", new Color(1f, 0.35f, 0.25f, 0.7f + 0.3f * beat));
            }
            else if (Rival.HuntingPlayer && me.CarriesCrown)
            {
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)), 6f);
                Edges(UiStyle.S(40), new Color(0.7f, 0.05f, 0.03f, 0.12f + 0.16f * beat));
            }

            if (me.Stunned) UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.9f, 0.85f, 0.7f, 0.12f));
            if (me.Slowed) Edges(UiStyle.S(60), new Color(0.5f, 0.8f, 1f, 0.22f));
        }

        static void Edges(float e, Color c)
        {
            UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, e), c);
            UiStyle.FadeBand(new Rect(0f, Screen.height - e, Screen.width, e), c);
        }

        // ================================================================== les touches

        bool keysOpen;

        /// <summary>0 : cache ; 1 : visible (F1 ou H).</summary>
        float KeysAlpha()
        {
            Season season = Game.Season;
            if (season == null || Game.Me == null) return 0f;
            // (30/09 -- "je veux pas le truc a l'avant") : plus jamais tout seul, seulement sur F1 ou H.
            return keysOpen ? 1f : 0f;
        }

        /// <summary>
        /// LES TOUCHES (F1 ou H), SANS UN MOT (30/09) : trois colonnes de pastilles --
        /// BOUGER, TES POUVOIRS, VOLER -- chacune une touche (une souris ou une lettre) et
        /// l'icone de ce qu'elle fait.
        /// </summary>
        void DrawKeys()
        {
            Seeker me = Game.Me;
            if (KeysAlpha() <= 0.01f || me == null) return;
            float row = UiStyle.S(58);
            float colW = UiStyle.S(210);
            float w = colW * 3f + UiStyle.S(60), h = row * 5f + UiStyle.S(70);
            Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Icons.Pill(new Rect(panel.x, panel.y, panel.width, panel.height), new Color(0.13f, 0.15f, 0.3f, 0.94f));
            string passive = null;
            for (int k = 0; k < me.Slot.Abilities.Count; k++)
                if (!AbilityInfo.IsActive(me.Slot.Abilities[k])) passive = Icons.Of(me.Slot.Abilities[k]);
            string[] moveKeys = { "Z", "Maj", "Espace", "E", "E" };
            string[] moveIcons = { "joueur", "coureur", "haut", "couronne", "arbaleste" };
            string[] powerKeys = { AbilityInfo.Keys[0], AbilityInfo.PushKey, AbilityInfo.PushKey, "", "Tab" };
            string[] powerIcons = { me.HasActive ? Icons.Of(me.CurrentActive) : "cible", "pousser", "couronne", passive ?? "", "manches" };
            string[] flyKeys = { "", "", "Espace", AbilityInfo.PushKey, "" };
            string[] flyIcons = { "ailes", "courant", "croix", "pique", "" };
            float x = panel.x + UiStyle.S(30), y = panel.y + UiStyle.S(34);
            Column(x, y, colW, row, moveKeys, moveIcons, new Color(0.45f, 0.75f, 1f));
            Column(x + colW, y, colW, row, powerKeys, powerIcons, new Color(1f, 0.55f, 0.4f));
            Column(x + colW * 2f, y, colW, row, flyKeys, flyIcons, Wings.Gold);
        }

        void Column(float x, float y, float w, float row, string[] keys, string[] icons, Color c)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (icons[i].Length == 0) continue;
                float s = row * 0.86f;
                Rect r = new Rect(x, y + i * row, w - UiStyle.S(24), s);
                Icons.Pill(r, new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.45f, 0.95f));
                if (keys[i].Length > 0) Icons.Key(new Rect(r.x + s * 0.08f, r.y + s * 0.08f, s * 0.84f, s * 0.84f), keys[i], 1f);
                else Icons.Draw(new Rect(r.x + s * 0.14f, r.y + s * 0.14f, s * 0.72f, s * 0.72f), "haut", new Color(1f, 1f, 1f, 0.35f), false);
                Icons.Draw(new Rect(r.xMax - s * 0.96f, r.y + s * 0.08f, s * 0.84f, s * 0.84f), icons[i], i == 0 ? c : Color.white);
            }
        }

        // ================================================================== l'astuce

        /// <summary>(30/09 -- "je deteste le texte" : plus d'astuces ecrites.)</summary>
        /// <summary>La pastille d'astuce, sous le viseur : des touches et des icones, sans un mot.</summary>
        void DrawTip()
        {
            if (tipTimer <= 0f || tipIcons == null || tipIcons.Length == 0) return;
            float alpha = Mathf.Clamp01(tipTimer / 0.6f) * Mathf.Clamp01((TipSeconds - tipTimer) / 0.25f);
            float s = UiStyle.S(50), gap = UiStyle.S(10);
            float w = tipIcons.Length * s + (tipIcons.Length - 1) * gap + s * 0.6f;
            Rect chip = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f + UiStyle.S(150), w, s + UiStyle.S(12));
            Icons.Pill(chip, new Color(0.18f, 0.2f, 0.34f, 0.9f), alpha);
            float x = chip.x + s * 0.3f;
            for (int i = 0; i < tipIcons.Length; i++)
            {
                Rect r = new Rect(x, chip.y + UiStyle.S(6), s, s);
                string id = tipIcons[i];
                if (id.StartsWith("k:"))
                {
                    string bind = id.Substring(2);
                    if (bind == "use") bind = AbilityInfo.UseKey;
                    else if (bind == "active") bind = AbilityInfo.Keys[0];
                    else if (bind == "push") bind = AbilityInfo.PushKey;
                    Icons.Key(r, bind, alpha);
                }
                else Icons.Draw(r, id, id == "couronne" ? new Color(1f, 0.86f, 0.35f, alpha) : id == "monument" ? new Color(0.55f, 0.75f, 1f, alpha) : new Color(1f, 1f, 1f, alpha));
                x += s + gap;
            }
            Caption(Screen.width * 0.5f, chip.yMax + UiStyle.S(6), tipWords, UiStyle.S(22), new Color(1f, 1f, 1f, alpha));
        }

        // ================================================================== le grand titre

        /// <summary>
        /// LE GRAND MOMENT, SANS UN MOT (30/09) : une grosse icone qui claque au milieu du
        /// haut de l'ecran (la Couronne prise, un don recu), dans une pastille a sa couleur.
        /// </summary>
        void DrawCard()
        {
            if (cardTimer <= 0f || string.IsNullOrEmpty(cardIcon)) return;
            float age = CardDuration - cardTimer;
            float alpha = Mathf.Clamp01(cardTimer / 0.5f);
            float punch = age < 0.15f ? Mathf.Lerp(1.6f, 1f, age / 0.15f) : 1f;
            // (02/10) Plus gros, et un anneau qui s'ouvre : on ne peut pas le rater.
            float s = UiStyle.S(150) * punch;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.3f;
            if (age < 0.6f)
            {
                float k = age / 0.6f;
                float ring = s * (1.3f + 1.4f * k);
                Icons.Dot(new Rect(cx - ring * 0.5f, cy - ring * 0.5f, ring, ring), new Color(cardTint.r, cardTint.g, cardTint.b, 0.45f * (1f - k)));
            }
            Icons.Dot(new Rect(cx - s * 0.7f, cy - s * 0.7f, s * 1.4f, s * 1.4f), new Color(1f, 1f, 1f, 0.9f * alpha));
            Icons.Pill(new Rect(cx - s * 0.62f, cy - s * 0.62f, s * 1.24f, s * 1.24f), new Color(cardTint.r * 0.7f, cardTint.g * 0.7f, cardTint.b * 0.7f, 1f), alpha);
            Icons.Draw(new Rect(cx - s * 0.45f, cy - s * 0.45f, s * 0.9f, s * 0.9f), cardIcon, cardIcon == "couronne" && !splashCross ? new Color(CrownGold.r, CrownGold.g, CrownGold.b, alpha) : new Color(1f, 1f, 1f, alpha));
            if (splashCross) Icons.Draw(new Rect(cx - s * 0.36f, cy - s * 0.36f, s * 0.72f, s * 0.72f), "croix", new Color(1f, 0.3f, 0.25f, alpha));
            Caption(cx, cy + s * 0.78f, splashWords, UiStyle.S(34), new Color(splashCross ? 1f : CrownGold.r, splashCross ? 0.55f : CrownGold.g, splashCross ? 0.5f : CrownGold.b, alpha));
        }

        // ================================================================== Tab : le match

        /// <summary>Tab maintenu : chacun (sa pastille, son pseudo), ses manches gagnees, ses capacites en icones.</summary>
        void DrawScores()
        {
            float row = UiStyle.S(60), w = Mathf.Min(UiStyle.S(620), Screen.width - UiStyle.S(40));
            float h = UiStyle.S(40) + row * Match.Slots.Count;
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.22f, w, h);
            Icons.Pill(new Rect(box.x - UiStyle.S(20), box.y, box.width + UiStyle.S(40), box.height), new Color(0.13f, 0.15f, 0.3f, 0.94f));
            float y = box.y + UiStyle.S(20);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                float ph = row * 0.8f;
                Rect r = new Rect(box.x, y, UiStyle.S(250), ph);
                if (s.IsLocal) Icons.Pill(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f), Wings.Gold);
                Icons.Pill(r, s.Colour);
                Icons.Number(new Rect(r.x + ph * 0.4f, r.y, r.width - ph * 0.8f, r.height), s.Name, Mathf.RoundToInt(ph * 0.46f), Color.white, TextAnchor.MiddleLeft);
                float cx = r.xMax + UiStyle.S(16);
                Icons.Draw(new Rect(cx, y + ph * 0.05f, ph * 0.9f, ph * 0.9f), "couronne", new Color(1f, 0.86f, 0.35f));
                Icons.Number(new Rect(cx + ph * 0.9f, y, ph, ph), s.Wins.ToString(), Mathf.RoundToInt(ph * 0.6f), Color.white, TextAnchor.MiddleLeft);
                float ax = cx + ph * 2f;
                for (int k = 0; k < s.Abilities.Count; k++)
                {
                    Rect ar = new Rect(ax + k * (ph + UiStyle.S(8)), y, ph, ph);
                    Icons.Pill(ar, Color.Lerp(AbilityInfo.Tint(s.Abilities[k]), new Color(0.2f, 0.18f, 0.36f), 0.4f));
                    Icons.Draw(new Rect(ar.x + ph * 0.16f, ar.y + ph * 0.16f, ph * 0.68f, ph * 0.68f), Icons.Of(s.Abilities[k]), Color.white);
                }
                y += row;
            }
        }

        // ================================================================== outils

        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        /// <summary>Un texte avec une ombre portee d'un pixel : lisible sur le noir comme sur la brume.</summary>
        static void Text(Rect r, string text, GUIStyle style, Color c)
        {
            UiStyle.Tinted(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style, new Color(0f, 0f, 0f, 0.75f * c.a));
            UiStyle.Tinted(r, text, style, c);
        }

        static GUIStyle wrapped;

        static GUIStyle Wrapped()
        {
            if (wrapped == null || wrapped.fontSize != UiStyle.Small.fontSize)
            {
                wrapped = new GUIStyle(UiStyle.Small);
                wrapped.wordWrap = true;
            }
            return wrapped;
        }

        // ================================================================== diagnostic

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
            UiStyle.Fill(box, new Color(0.22f, 0.05f, 0.05f, 0.96f));
            float x = box.x + UiStyle.S(16);
            Text(new Rect(x, box.y + UiStyle.S(10), w, UiStyle.S(26)), "LA CONSTRUCTION DU MONDE A ÉCHOUÉ", UiStyle.Head, new Color(1f, 0.55f, 0.45f));
            GUI.Label(new Rect(x, box.y + UiStyle.S(38), w - UiStyle.S(32), h - UiStyle.S(48)), Game.BuildError, Wrapped());
        }

        /// <summary>
        /// Panneau F3. Il repond a la question "je suis dans quoi ?" : ce qui colle a
        /// l'oeil, ce que touche un rayon tire vers l'avant, et quelques compteurs.
        /// </summary>
        void DrawDiagnostic()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            float w = UiStyle.S(500);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(110), w, UiStyle.S(250));
            UiStyle.Fill(box, UiStyle.Scrim);
            float x = box.x + UiStyle.S(16);
            float y = box.y + UiStyle.S(12);
            float inner = w - UiStyle.S(32);
            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "DIAGNOSTIC  (F3)", UiStyle.Head);
            y += UiStyle.S(32);
            y = Line(x, y, inner, "Version", Game.Version);
            y = Line(x, y, inner, "Monde construit en", Game.BuildMilliseconds + " ms");
            y = Line(x, y, inner, "Gargouilles / bots / sanctuaires", Eye.All.Count + " / " + Rival.All.Count + " / " + Shrine.All.Count);
            string crown = Crown.Holder != null ? "portée par " + Crown.Holder.Name : Crown.Where == Crown.State.Dropped ? "à terre" : "au sommet";
            y = Line(x, y, inner, "Couronne", crown);
            if (Game.PlayerTransform != null)
            {
                Vector3 p = Game.PlayerTransform.position;
                y = Line(x, y, inner, "Joueur", p.x.ToString("0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0"));
                y = Line(x, y, inner, "Sur la tour", Tower.On(p) ? Mathf.RoundToInt(Tower.Progress(p) * 100f) + " %" : "non");
            }
            if (cam != null)
            {
                RaycastHit hit;
                string ahead = "rien à moins de 40 m";
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                    ahead = hit.collider.gameObject.name + " à " + hit.distance.ToString("0.0") + " m";
                y = Line(x, y, inner, "Devant toi", ahead);
            }
        }

        float Line(float x, float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, width * 0.5f, UiStyle.S(20)), label, UiStyle.Small);
            UiStyle.Tinted(new Rect(x + width * 0.5f, y, width * 0.5f, UiStyle.S(20)), value, UiStyle.Small, Palette.Gold);
            return y + UiStyle.S(21);
        }
    }
}
