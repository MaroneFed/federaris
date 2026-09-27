using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ECRAN DE JEU (27/09 -- Martin : "j'aime pas les icones"). Donc PLUS UNE SEULE
    /// icone : des mots, des chiffres, des traits. C'est le parti pris des jeux qui
    /// ont l'ecran le plus propre (Journey, Mirror's Edge, les premiers Doom) :
    /// ce qui compte est ecrit, le reste disparait.
    ///
    ///                          4:12
    ///                      MANCHE 2 / 5                     TOI    2
    ///               BRUME porte la Couronne                 Brume  1
    ///                                                       Sorbe  0
    ///                            ·            <- le point de visee
    ///                      E  Prendre le don
    ///
    ///   CLIC DROIT  Ruée          prête
    ///   R           Grappin       ▬▬▬▬▬▬ 3
    ///   C           Onde          prête
    ///   V           Clignement    (don)
    ///   Double saut · Coureur                 <- les passifs, en une ligne
    ///
    /// Il ne decide RIEN : il lit l'etat du jeu et l'ecrit. La pause, les ecrans entre
    /// les manches : Menus.cs.
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
        public void ShowSplash(string icon, Color tint)
        {
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
            FloatingTexts.Tick(Time.unscaledDeltaTime);
            if (hitStop > 0f)
            {
                hitStop -= Time.unscaledDeltaTime;
                if (hitStop <= 0f && Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = 1f;
            }
            if (cardTimer > 0f) cardTimer -= Time.unscaledDeltaTime;
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.4f);
            if (hurtFlash > 0f) hurtFlash = Mathf.Max(0f, hurtFlash - Time.unscaledDeltaTime * 2f);
            if (hitSideTimer > 0f) hitSideTimer -= Time.unscaledDeltaTime;
            if (tipTimer > 0f) tipTimer -= Time.unscaledDeltaTime;
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
        string tipText;
        float tipTimer;
        const float TipDuration = 5.5f;

        /// <summary>Une astuce, une seule fois par match, au moment ou elle sert.</summary>
        public void Tip(string key, string text)
        {
            // (30/09 -- "je deteste le texte") : plus d'astuces ecrites.
            return;
            if (tipsShown.Contains(key) || tipTimer > 1f) return;
            tipsShown.Add(key);
            tipText = text;
            tipTimer = TipDuration;
        }

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
            else if (Updraft.Near(p, 5f) != null) Tip("courant", "Un courant : marche dans le disque pour monter d'un tour.");
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

            DrawVeils();
            DrawNames();
            DrawFlair();
            FloatingTexts.Draw(viewCamera != null ? viewCamera : Camera.main);
            DrawTop();
            DrawStandings();
            DrawAbilities();
            DrawCentre();
            DrawPrompt();
            DrawCard();
            DrawTip();
            DrawKeys();
            Toasts.Draw();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();
            if (FiefInput.ScoresHeld) DrawScores();
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
                if (b.Charging)
                {
                    Rect bar = new Rect(chip.x + s * 0.3f, chip.yMax + UiStyle.S(10), chip.width - s * 0.6f, UiStyle.S(12));
                    Icons.Pill(bar, new Color(0.1f, 0.1f, 0.18f));
                    if (b.Tension > 0.02f) Icons.Pill(new Rect(bar.x, bar.y, Mathf.Max(bar.height, bar.width * b.Tension), bar.height), Wings.Gold);
                }
            }
            DrawTowerGauge();
        }

        /// <summary>Ou est la Couronne : au sommet (tour), dans des mains (la couleur du porteur), a terre (et son retour).</summary>
        void CrownBadge(float cx, float y)
        {
            Seeker holder = Crown.Holder;
            Crown.State where = Crown.Where;
            float h = UiStyle.S(50), w = UiStyle.S(112);
            Rect r = new Rect(cx - w * 0.5f, y, w, h);
            Color fill;
            string second;
            Color secondTint = Color.white;
            if (where == Crown.State.Carried && holder != null)
            {
                fill = holder.IsPlayer ? Wings.Gold : holder.Colour;
                second = "joueur";
                if (holder.IsPlayer)
                {
                    float pulse = UiStyle.S(4) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
                    r = new Rect(r.x - pulse, r.y - pulse * 0.5f, r.width + pulse * 2f, r.height + pulse);
                }
            }
            else if (where == Crown.State.Dropped) { fill = new Color(0.95f, 0.55f, 0.2f); second = null; }
            else if (where == Crown.State.Delivered) { fill = Monument.Blue; second = "monument"; }
            else { fill = new Color(0.36f, 0.3f, 0.62f); second = "tour"; }
            Icons.Pill(r, fill);
            float ic = r.height * 0.8f;
            Icons.Draw(new Rect(r.x + r.height * 0.18f, r.y + (r.height - ic) * 0.5f, ic, ic), "couronne", new Color(1f, 0.86f, 0.35f));
            Rect right = new Rect(r.xMax - r.height * 0.18f - ic, r.y + (r.height - ic) * 0.5f, ic, ic);
            if (second != null) Icons.Draw(right, second, secondTint);
            else Icons.Number(right, Mathf.CeilToInt(Crown.ReturnIn).ToString(), Mathf.RoundToInt(r.height * 0.55f), Color.white, TextAnchor.MiddleCenter);
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
            float h = UiStyle.S(34), w = UiStyle.S(78);
            float x = Screen.width - w - UiStyle.S(22), y = UiStyle.S(20);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                Rect r = new Rect(x, y, w, h);
                if (s.IsLocal) Icons.Pill(new Rect(r.x - UiStyle.S(4), r.y - UiStyle.S(4), r.width + UiStyle.S(8), r.height + UiStyle.S(8)), Wings.Gold);
                Icons.Pill(r, s.Colour);
                Icons.Draw(new Rect(r.x + h * 0.12f, r.y + h * 0.08f, h * 0.84f, h * 0.84f), "couronne", new Color(1f, 0.86f, 0.35f));
                Icons.Number(new Rect(r.x + h * 0.9f, r.y, r.width - h, r.height), s.Wins.ToString(), Mathf.RoundToInt(h * 0.66f), Color.white, TextAnchor.MiddleCenter);
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
            if (me.Stunned) { states.Add("clignement"); tints.Add(new Color(1f, 0.85f, 0.4f)); }
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
        }

        /// <summary>L'invite d'interaction : la touche E, et l'icone de ce qu'on va prendre ; le cercle qui se remplit pendant le maintien.</summary>
        void DrawPrompt()
        {
            if (interactor == null) return;
            IInteractable target = interactor.Current;
            if (target == null || !target.CanInteract) return;
            string icon = target is Crown ? "couronne" : target is Ballista ? "arbaleste" : target is Shrine ? "don" : target is Monument ? "monument" : "main";
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
            if (nameStyle == null || nameBase != UiStyle.Label.fontSize)
            {
                nameBase = UiStyle.Label.fontSize;
                nameStyle = new GUIStyle(UiStyle.Label);
                nameStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.fontStyle = FontStyle.Bold;
                nameStyle.wordWrap = false;
            }
            // (29/09) Du plus proche au plus loin, et un nom ne se pose jamais sur un autre :
            // on le monte d'un cran, et s'il n'y a pas la place, on ne l'ecrit pas.
            nameOrder.Clear();
            nameDist.Clear();
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.IsPlayer || s.Body == null || s.Hidden) continue;
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
                nameStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(UiStyle.S(22), UiStyle.S(13), Mathf.Clamp01(d / 90f)));
                Color c = s.CarriesCrown ? new Color(1f, 0.82f, 0.35f) : Color.Lerp(s.Colour, Color.white, 0.25f);
                Vector2 size = nameStyle.CalcSize(new GUIContent(s.Name));
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
                GUI.color = new Color(0f, 0f, 0f, 0.75f * a);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s.Name, nameStyle);
                GUI.color = new Color(c.r, c.g, c.b, a);
                GUI.Label(r, s.Name, nameStyle);
                GUI.color = Color.white;
            }
        }
        readonly List<int> nameOrder = new List<int>();
        readonly List<float> nameDist = new List<float>();
        readonly List<Rect> namePlaced = new List<Rect>();
        GUIStyle nameStyle;
        int nameBase = -1;

        /// <summary>
        /// LE FLAIR (passif) : la Couronne est ecrite a sa place a l'ecran, avec sa
        /// distance -- "COURONNE 84 m" -- meme a travers les murs.
        /// </summary>
        void DrawFlair()
        {
            Seeker me = Game.Me;
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (me == null || cam == null || me.CarriesCrown || !me.Has(Ability.Flair) || Crown.Instance == null || me.Body == null) return;
            Vector3 world = Crown.Position + Vector3.up * 0.6f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z <= 0f;
            float gx = sp.x, gy = Screen.height - sp.y;
            if (behind) { gx = Screen.width - gx; gy = Screen.height - gy; }
            float m = UiStyle.S(60);
            if (behind || gx < m || gx > Screen.width - m || gy < m || gy > Screen.height - m)
            {
                Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 d = new Vector2(gx, gy) - c;
                if (d.sqrMagnitude < 1f) d = Vector2.down;
                float k = Mathf.Min((Screen.width * 0.5f - m) / Mathf.Max(1f, Mathf.Abs(d.x)), (Screen.height * 0.5f - m) / Mathf.Max(1f, Mathf.Abs(d.y)));
                gx = c.x + d.x * k;
                gy = c.y + d.y * k;
            }
            int dist = Mathf.RoundToInt((world - me.Body.position).magnitude);
            float s = UiStyle.S(40);
            float a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
            Icons.Draw(new Rect(gx - s * 0.5f, gy - s * 0.5f, s, s), "couronne", new Color(1f, 0.86f, 0.35f, a));
            Icons.Number(new Rect(gx - UiStyle.S(60), gy + s * 0.5f, UiStyle.S(120), UiStyle.S(20)), dist + "m", UiStyle.S(18), new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter);
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
        void DrawTip() { }

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
            float s = UiStyle.S(120) * punch;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.3f;
            Icons.Pill(new Rect(cx - s * 0.62f, cy - s * 0.62f, s * 1.24f, s * 1.24f), new Color(cardTint.r * 0.7f, cardTint.g * 0.7f, cardTint.b * 0.7f, 1f), alpha);
            Icons.Draw(new Rect(cx - s * 0.45f, cy - s * 0.45f, s * 0.9f, s * 0.9f), cardIcon, new Color(1f, 1f, 1f, alpha));
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

        static GUIStyle bigCentered, rightSmall, wrapped, wrappedCentered;
        static readonly Dictionary<int, GUIStyle> sized = new Dictionary<int, GUIStyle>();

        /// <summary>Une copie du style "from", "k" fois plus grande et centree (jamais le style partage lui-meme).</summary>
        static GUIStyle Sized(GUIStyle from, float k)
        {
            int px = Mathf.RoundToInt(from.fontSize * k);
            GUIStyle s;
            if (!sized.TryGetValue(px, out s))
            {
                s = new GUIStyle(from);
                s.fontSize = px;
                s.alignment = TextAnchor.MiddleCenter;
                sized[px] = s;
            }
            return s;
        }

        static GUIStyle WrappedCentered()
        {
            if (wrappedCentered == null || wrappedCentered.fontSize != UiStyle.Label.fontSize)
            {
                wrappedCentered = new GUIStyle(UiStyle.Label);
                wrappedCentered.wordWrap = true;
                wrappedCentered.alignment = TextAnchor.UpperCenter;
            }
            return wrappedCentered;
        }

        static GUIStyle BigCentered()
        {
            if (bigCentered == null || bigCentered.fontSize != UiStyle.Title.fontSize)
            {
                bigCentered = new GUIStyle(UiStyle.Title);
                bigCentered.alignment = TextAnchor.MiddleCenter;
            }
            return bigCentered;
        }

        static GUIStyle RightSmall()
        {
            if (rightSmall == null || rightSmall.fontSize != UiStyle.Small.fontSize)
            {
                rightSmall = new GUIStyle(UiStyle.Small);
                rightSmall.alignment = TextAnchor.MiddleRight;
            }
            return rightSmall;
        }

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
