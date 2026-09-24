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
        bool wasBrewed;
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
            cardLine2 = line2;
            cardTint = tint;
            cardTimer = CardDuration;
            cardItem = -1;
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
        }

        /// <summary>"how" : comment on est tombe ("sous les coups de Mahaut", "dans un piege").</summary>
        public void ShowDeath(string how)
        {
            killedBy = how;
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
                    Toasts.Show("Tu te releves pres de ta stele. Ta depouille est la ou tu es tombe.", UiStyle.InkDim);
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
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.38f, Screen.width, UiStyle.S(70)), UiStyle.Spaced("TU ES TOMBE"), big,
                               new Color(0.85f, 0.3f, 0.25f, a));
                big.alignment = previous;
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.38f + UiStyle.S(76), Screen.width, UiStyle.S(24)),
                               killedBy + ". Tout ce que tu portais est reste la-bas.", UiStyle.Centered,
                               new Color(0.9f, 0.85f, 0.78f, a));
                return;
            }

            // La barre de vie : seulement quand elle n'est pas pleine.
            if (me.Health < Seeker.MaxHealth - 0.5f)
            {
                float w = UiStyle.S(260);
                Rect bar = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(110), w, UiStyle.S(10));
                UiStyle.Bar(bar, me.Health / Seeker.MaxHealth, new Color(0.75f, 0.16f, 0.12f), UiStyle.BarBg);
            }
        }

        int cardItem = -1;
        float flash;
        Color flashTint;
        float slowMo;

        /// <summary>
        /// LA TROUVAILLE D'UN TALISMAN : l'ecran flashe a sa couleur, le temps ralentit
        /// une seconde, et la carte montre l'objet EN 3D qui tourne (voir Showcase).
        /// </summary>
        public void ShowItem(Talisman t, int count)
        {
            ShowDiscovery("TALISMAN  " + count + " / " + TalismanInfo.Count, TalismanInfo.Name(t),
                          TalismanInfo.Effect(t), TalismanInfo.Lore(t), TalismanInfo.Tint(t));
            cardItem = (int)t;
            cardTimer = CardDuration + 2f;
            flash = 1f;
            flashTint = TalismanInfo.Tint(t);
            if (menus != null && !menus.Blocking)
            {
                slowMo = 1.1f;
                Time.timeScale = 0.3f;
            }
        }

        public void OpenPanel(IPanel newPanel) { panel = newPanel; }
        public void ClosePanel() { panel = null; }

        void Update()
        {
            Toasts.Tick(Time.unscaledDeltaTime);
            if (cardTimer > 0f) cardTimer -= Time.unscaledDeltaTime;
            TickLife();
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.3f);
            if (slowMo > 0f)
            {
                slowMo -= Time.unscaledDeltaTime;
                // On ne rend le temps normal que s'il est toujours ralenti par nous
                // (la pause, elle, le met a zero : on n'y touche pas).
                if (slowMo <= 0f && Mathf.Approximately(Time.timeScale, 0.3f)) Time.timeScale = 1f;
            }
            if (FiefInput.SatchelPressed && menus != null && !menus.Blocking)
            {
                if (panel is TalismanPanel) ClosePanel();
                else if (panel == null) { OpenPanel(new TalismanPanel()); Objectives.VictoriesSeen(); }
            }

            bool brewed = Game.Brewed;
            if (wasBrewed && !brewed) Toasts.Show("L'infusion de l'Ermite ne fait plus effet.", new Color(0.66f, 0.84f, 0.56f));
            wasBrewed = brewed;
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
            DrawPrompt();
            DrawDigging();
            DrawTools();
            DrawDiscovery();
            Toasts.Draw();
            if (!showHelp) Objectives.Draw();
            DrawHelp();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();

            if (panel != null) panel.Draw();
            DrawLife();
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
            string[] names = { "au nord", "au nord-est", "a l'est", "au sud-est",
                               "au sud", "au sud-ouest", "a l'ouest", "au nord-ouest" };
            return names[Mathf.RoundToInt(angle / 45f) % 8];
        }

        /// <summary>
        /// Le haut de l'ecran : la boussole, puis l'horloge de la Saison dans son
        /// cartouche, puis une ligne sur le mage.
        /// </summary>
        void DrawSeason()
        {
            Season season = Game.Season;
            if (season == null) return;

            Transform eye = viewCamera != null ? viewCamera.transform : null;
            float bandW = Mathf.Min(UiStyle.S(620), Screen.width - UiStyle.S(40));
            Rect band = new Rect((Screen.width - bandW) * 0.5f, UiStyle.S(14), bandW, UiStyle.S(30));
            if (eye != null && Game.PlayerTransform != null) Compass.Draw(band, eye, Game.PlayerTransform.position);

            // --- le cartouche de l'horloge
            float left = season.Remaining;
            bool late = left < 180f;
            float cw = UiStyle.S(128), ch = UiStyle.S(38);
            Rect plate = new Rect((Screen.width - cw) * 0.5f, band.yMax + UiStyle.S(34), cw, ch);
            GUI.Box(plate, GUIContent.none, UiStyle.CardBox);
            GUIStyle clockStyle = UiStyle.Value;
            TextAnchor previous = clockStyle.alignment;
            clockStyle.alignment = TextAnchor.MiddleCenter;
            Color clock = late ? Color.Lerp(new Color(0.95f, 0.42f, 0.3f), UiStyle.Ink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)) : UiStyle.Ink;
            UiStyle.Tinted(plate, Clock(left), clockStyle, clock);
            clockStyle.alignment = previous;

            // --- le mage
            string mage;
            Color tint;
            if (season.MagePresent)
            {
                mage = "Le mage chante  --  il repart dans " + Clock(season.MageTimeLeft);
                tint = new Color(0.62f, 0.76f, 1f);
            }
            else if (season.NextMageIn >= 0f)
            {
                mage = "Le mage reviendra dans " + Clock(season.NextMageIn);
                tint = UiStyle.InkDim;
            }
            else
            {
                mage = "Le mage ne reviendra plus. Pose ta relique.";
                tint = new Color(0.92f, 0.62f, 0.32f);
            }
            UiStyle.Tinted(new Rect(0f, plate.yMax + UiStyle.S(4), Screen.width, UiStyle.S(20)), mage, UiStyle.CenteredSmall, tint);

            // --- la Malediction : visible des qu'elle approche (deux minutes), et
            // qui palpite dans les vingt dernieres secondes.
            float curse = season.NextCurseIn;
            if (curse >= 120f)
            {
                Color quiet = Curse.Violet;
                quiet.a = 0.55f;
                UiStyle.Tinted(new Rect(0f, plate.yMax + UiStyle.S(24), Screen.width, UiStyle.S(18)),
                               "Malediction dans " + Clock(curse), UiStyle.CenteredSmall, quiet);
            }
            if (curse >= 0f && curse < 120f && !season.MagePresent)
            {
                bool urgent = curse < 20f;
                float pulse = urgent ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f) : 1f;
                Color c = Curse.Violet;
                c.a = pulse;
                string line = "LA MALEDICTION dans " + Clock(curse)
                              + (Game.Inventory != null && !Game.Inventory.IsEmpty ? "  --  vide ton sac a ta stele" : "  --  ton sac est vide");
                UiStyle.Tinted(new Rect(0f, plate.yMax + UiStyle.S(24), Screen.width, UiStyle.S(20)), line,
                               urgent ? UiStyle.Centered : UiStyle.CenteredSmall, c);
            }
        }

        // ---------------------------------------------------------------- le sac

        void DrawPack()
        {
            Inventory inv = Game.Inventory;
            if (inv == null) return;

            float pad = UiStyle.S(16);
            float w = UiStyle.S(270);
            float h = UiStyle.S(198);
            Rect box = new Rect(pad, Screen.height - h - pad, w, h);
            UiStyle.Frame(box);

            // Les Autels que tu tiens, et leur dernier versement (plus de message a
            // chaque versement : un son de pieces, et cette ligne).
            string held = Monument.HeldBy(Game.Me);
            if (held.Length > 0)
            {
                string pay = Monument.PayFlash > 0f ? "   " + Monument.LastPay : "";
                Color gold = new Color(0.95f, 0.78f, 0.35f, 0.6f + 0.4f * Mathf.Clamp01(Monument.PayFlash));
                UiStyle.Tinted(new Rect(box.x, box.y - UiStyle.S(24), UiStyle.S(560), UiStyle.S(20)), "Tu tiens : " + held + pay, UiStyle.Small, gold);
            }

            float x = box.x + UiStyle.S(16);
            float inner = w - UiStyle.S(32);
            float y = box.y + UiStyle.S(12);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "SAC", UiStyle.Head);
            if (Game.Wallet != null)
                UiStyle.Tinted(new Rect(x + UiStyle.S(52), y, UiStyle.S(120), UiStyle.S(24)),
                               Game.Wallet.Gold + " or", UiStyle.Label, new Color(0.95f, 0.78f, 0.35f));

            GUIStyle right = UiStyle.Small;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)),
                      Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg", right);
            right.alignment = previous;

            y += UiStyle.S(28);
            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int amount = inv.Get(type);

                UiStyle.Chip(new Rect(x, y + UiStyle.S(5), UiStyle.S(12), UiStyle.S(12)), ResourceInfo.Tint(type));
                UiStyle.Tinted(new Rect(x + UiStyle.S(20), y, UiStyle.S(140), UiStyle.S(22)),
                               ResourceInfo.Name(type), UiStyle.Label,
                               amount > 0 ? UiStyle.Ink : UiStyle.InkFaint);

                right.alignment = TextAnchor.MiddleRight;
                UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(22)), amount.ToString(), right,
                               amount > 0 ? UiStyle.Ink : UiStyle.InkFaint);
                right.alignment = previous;
                y += UiStyle.S(22);
            }

            // --- la jauge de charge : c'est elle qui dit "va cacher"
            y += UiStyle.S(6);
            float load = inv.Load01;
            Color fill = Color.Lerp(new Color(0.44f, 0.78f, 0.40f),
                                    new Color(0.88f, 0.31f, 0.25f), Mathf.Pow(load, 0.85f));
            if (load >= 0.98f)
            {
                // Plein : la jauge clignote, et le dit.
                fill = Color.Lerp(fill, Color.white, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f));
                UiStyle.Tinted(new Rect(x, y - UiStyle.S(24), inner, UiStyle.S(20)), "PLEIN  --  va vider ton sac a ta stele", RightSmall(),
                               new Color(0.95f, 0.45f, 0.35f));
            }
            UiStyle.Bar(new Rect(x, y, inner, UiStyle.S(10)), load, fill, UiStyle.BarBg);
            y += UiStyle.S(16);

            // --- la relique
            Hoard hoard = Game.Hoard;
            string relic;
            Color tint;
            if (hoard != null && hoard.Trophy != null)
            {
                relic = "Relique VOLEE a " + hoard.TrophyFrom.Name + " : cours a ta stele";
                tint = new Color(1f, 0.55f, 0.4f);
            }
            else if (hoard == null || hoard.Relic == null)
            {
                relic = "Pas encore de relique";
                tint = UiStyle.InkFaint;
            }
            else
            {
                relic = "Relique  " + hoard.Relic.Power + (hoard.RelicOnStele ? "   sur ta stele" : "   en main");
                tint = hoard.RelicOnStele ? new Color(0.62f, 0.78f, 0.95f) : Palette.Gold;
            }
            UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(20)), relic, UiStyle.Small, tint);

            // L'infusion de l'Ermite, tant qu'elle agit.
            if (Game.Brewed)
            {
                right.alignment = TextAnchor.MiddleRight;
                UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(20)),
                               "infusion " + Clock(hoard.BrewUntil - Game.Season.Elapsed), right,
                               new Color(0.66f, 0.84f, 0.56f));
                right.alignment = previous;
            }

            // --- les talismans : six pastilles, allumees quand on les a.
            y += UiStyle.S(24);
            float chip = UiStyle.S(14);
            for (int i = 0; i < TalismanInfo.Count; i++)
            {
                Talisman tal = TalismanInfo.All[i];
                bool owned = hoard != null && hoard.Has(tal);
                Rect r = new Rect(x + i * (chip + UiStyle.S(6)), y + UiStyle.S(3), chip, chip);
                if (owned) UiStyle.Chip(r, TalismanInfo.Tint(tal));
                else UiStyle.Fill(r, new Color(1f, 1f, 1f, 0.07f));
            }
            int count = hoard != null ? hoard.TalismanCount : 0;
            right.alignment = TextAnchor.MiddleRight;
            UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(20)), "talismans " + count + " / " + TalismanInfo.Count,
                           right, count > 0 ? UiStyle.Ink : UiStyle.InkFaint);
            right.alignment = previous;
        }

        // ---------------------------------------------------------------- invite

        void DrawPrompt()
        {
            if (panel != null || interactor == null) return;

            IInteractable target = interactor.Current;
            if (target == null) return;

            float w = UiStyle.S(430);
            float h = UiStyle.S(50);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(216), w, h);

            UiStyle.DropShadow(box, UiStyle.S(14));
            GUI.Box(box, GUIContent.none, UiStyle.CardBox);

            float capW = UiStyle.S(34);
            Rect cap = new Rect(box.x + UiStyle.S(13), box.y + (h - capW) * 0.5f, capW, capW);
            UiStyle.Pill(cap);
            UiStyle.Tinted(cap, "E", UiStyle.Centered, Palette.Gold);

            GUIStyle label = UiStyle.Label;
            GUI.Label(new Rect(cap.xMax + UiStyle.S(13), box.y, box.width - capW - UiStyle.S(40), h * 0.62f),
                      target.Prompt, label);

            string hint = target.HoldDuration > 0f ? "maintenir" : "appuyer";
            GUI.Label(new Rect(cap.xMax + UiStyle.S(13), box.y + h * 0.54f,
                               box.width - capW - UiStyle.S(40), h * 0.42f), hint, UiStyle.Tiny);

            if (target.HoldDuration > 0f && interactor.HoldProgress01 > 0.001f)
            {
                Rect bar = new Rect(box.x + UiStyle.S(6), box.yMax - UiStyle.S(5),
                                    box.width - UiStyle.S(12), UiStyle.S(4));
                UiStyle.Bar(bar, interactor.HoldProgress01, Palette.Gold, new Color(0f, 0f, 0f, 0.5f));
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
            Kit kit = me.Kit;
            float size = UiStyle.S(54);
            float gap = UiStyle.S(10);
            float x = (Screen.width - size * 2f - gap) * 0.5f;
            float y = Screen.height - size - UiStyle.S(20);
            for (int i = 0; i < 2; i++)
            {
                Rect r = new Rect(x + i * (size + gap), y, size, size);
                GUI.Box(r, GUIContent.none, kit.Active == i ? UiStyle.PanelBox : UiStyle.CardBox);
                if (kit.Active == i) UiStyle.FadeBand(new Rect(r.x, r.yMax - 2f, r.width, 2f), Palette.Gold);
                UiStyle.Tinted(new Rect(r.x + UiStyle.S(6), r.y + UiStyle.S(2), UiStyle.S(20), UiStyle.S(16)), (i + 1).ToString(), UiStyle.Tiny, UiStyle.InkDim);
                Tool t = kit.Slots[i];
                if (t == null) continue;
                UiStyle.Tinted(new Rect(r.x, r.y + UiStyle.S(10), r.width, r.height - UiStyle.S(24)), Kit.Name(t.Kind), UiStyle.CenteredSmall,
                               kit.Active == i ? Palette.Gold : UiStyle.Ink);
                UiStyle.Bar(new Rect(r.x + UiStyle.S(8), r.yMax - UiStyle.S(12), r.width - UiStyle.S(16), UiStyle.S(4)),
                            (float)t.Durability / t.Max, new Color(0.7f, 0.72f, 0.75f), UiStyle.BarBg);
            }

            // Une relique dans les mains : on ne peut pas frapper, et ca se voit.
            Hoard carried = me.Hoard;
            if (carried.RelicInHand || carried.Trophy != null)
            {
                Color blue = Stele.RuneBlue;
                blue.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
                string what = carried.Trophy != null ? "LA RELIQUE DE " + carried.TrophyFrom.Name.ToUpperInvariant() : "TA RELIQUE EN MAIN";
                UiStyle.Tinted(new Rect(0f, y - UiStyle.S(30), Screen.width, UiStyle.S(22)),
                               what + "  --  tu ne peux pas frapper, et on te voit venir", UiStyle.CenteredSmall, blue);
            }

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
            if (!string.IsNullOrEmpty(ToolUser.Hint) && panel == null)
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.5f + UiStyle.S(26), Screen.width, UiStyle.S(20)), ToolUser.Hint,
                               UiStyle.CenteredSmall, new Color(0.95f, 0.9f, 0.8f, 0.9f));
        }

        // ---------------------------------------------------------------- trouvaille

        void DrawDiscovery()
        {
            if (cardTimer <= 0f || string.IsNullOrEmpty(cardTitle)) return;

            // Entree en 0,4 s, sortie en 0,8 s.
            float age = CardDuration - cardTimer;
            float alpha = Mathf.Clamp01(age / 0.4f) * Mathf.Clamp01(cardTimer / 0.8f);
            bool detailed = !string.IsNullOrEmpty(cardLine1);

            bool item = cardItem >= 0;
            float w = UiStyle.S(item ? 620 : 520);
            float h = UiStyle.S(item ? 200 : detailed ? 150 : 86);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(156) - (1f - Mathf.Clamp01(age / 0.4f)) * UiStyle.S(12), w, h);

            // L'eclair de la trouvaille : tout l'ecran, a la couleur de l'objet.
            if (flash > 0f) UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height),
                                         new Color(flashTint.r, flashTint.g, flashTint.b, flash * flash * 0.45f));

            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (item)
            {
                // L'objet en 3D, a gauche ; les mots a droite.
                Showcase.Show((Talisman)cardItem, 0.2f);
                UiStyle.Frame(box);
                UiStyle.Fill(new Rect(box.x + UiStyle.S(10), box.y, box.width - UiStyle.S(20), 2f), cardTint);
                float pic = h - UiStyle.S(24);
                Rect frame = new Rect(box.x + UiStyle.S(14), box.y + UiStyle.S(12), pic, pic);
                GUI.Box(frame, GUIContent.none, UiStyle.CardBox);
                if (Showcase.Image != null) GUI.DrawTexture(frame, Showcase.Image, ScaleMode.ScaleToFit, true);
                float tx = frame.xMax + UiStyle.S(18);
                float tw = box.xMax - tx - UiStyle.S(18);
                float ty = box.y + UiStyle.S(16);
                UiStyle.Tinted(new Rect(tx, ty, tw, UiStyle.S(16)), cardKicker, UiStyle.Small, UiStyle.InkDim);
                ty += UiStyle.S(22);
                UiStyle.Tinted(new Rect(tx, ty, tw, UiStyle.S(40)), cardTitle, UiStyle.Title, cardTint);
                ty += UiStyle.S(46);
                GUIStyle body = UiStyle.Label;
                bool wrap = body.wordWrap;
                body.wordWrap = true;
                UiStyle.Tinted(new Rect(tx, ty, tw, UiStyle.S(44)), cardLine1, body, UiStyle.Ink);
                body.wordWrap = wrap;
                ty += UiStyle.S(48);
                UiStyle.Tinted(new Rect(tx, ty, tw, UiStyle.S(18)), cardLine2, UiStyle.Tiny, UiStyle.InkFaint);
                GUI.color = was;
                return;
            }
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
                bool thief = r.seeker.Hoard.Trophy != null && r.seeker.Hoard.TrophyFrom == Game.Me;
                float d = FlatDistance(me, r.transform.position);
                if (thief)
                    DrawMarker(cam, r.transform.position + Vector3.up * 2.4f, "VOLEUR : " + r.seeker.Name, new Color(1f, 0.4f, 0.3f));
                else if (d < 16f)
                    DrawMarker(cam, r.transform.position + Vector3.up * 2.4f, r.seeker.Name, r.seeker.Colour);
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
                           "LA CONSTRUCTION DU MONDE A ECHOUE", UiStyle.Head, new Color(1f, 0.55f, 0.45f));

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
                     QualitySettings.activeColorSpace == ColorSpace.Linear ? "lineaire" : "gamma");
            y = Line(x, y, inner, "Vue",
                     orbitCamera != null && orbitCamera.ThroughEyes ? "premiere personne" : "ecran-titre");

            if (Game.PlayerTransform != null)
            {
                Vector3 p = Game.PlayerTransform.position;
                y = Line(x, y, inner, "Joueur",
                         p.x.ToString("0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0"));
                y = Line(x, y, inner, "Sol sous les pieds", Ground.Sample(p.x, p.z).ToString("0.0") + " m");

                // Pour tester sans chercher une demi-heure : ou est le mage. En jeu,
                // aucun repere ne le montre -- on le trouve a l'oreille.
                Mage mage = Game.Mage;
                y = Line(x, y, inner, "Mage",
                         mage != null && mage.Present
                             ? "a " + FlatDistance(p, mage.transform.position).ToString("0") + " m, "
                               + Direction(p, mage.transform.position)
                             : "absent");
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
                y = Line(x, y, inner, "Camera a l'interieur de", inside);
                y = Line(x, y, inner, "Colle a l'oeil", nearest);

                Collider[] touching = Physics.OverlapSphere(c, 0.25f, ~0, QueryTriggerInteraction.Ignore);
                y = Line(x, y, inner, "Solides autour de l'oeil",
                         touching.Length == 0 ? "aucun" : touching[0].gameObject.name
                             + (touching.Length > 1 ? " +" + (touching.Length - 1) : ""));

                RaycastHit hit;
                string ahead = "rien a moins de 40 m";
                if (Physics.Raycast(c, cam.transform.forward, out hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                    ahead = hit.collider.gameObject.name + " a " + hit.distance.ToString("0.0") + " m";
                y = Line(x, y, inner, "Devant toi", ahead);
            }

            y += UiStyle.S(6);
            GUI.Label(new Rect(x, y, inner, UiStyle.S(34)),
                      "Lis-moi \"Colle a l'oeil\" : c'est ce qui est devant la camera.", UiStyle.Tiny);
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
                { "ZQSD", "se deplacer" },
                { "Maj", "courir" },
                { "Souris", "camera" },
                { "E", "recolter, interagir, ta stele" },
                { "C", "planter le camp" },
                { "G", "creuser une cache" },
                { "Tab", "ta besace" },
                { "1 / 2 + clic", "outil : abattre, frapper, poser un piege" },
                { "F", "grimper dans un arbre" },
                { "H", "tendre l'oreille : ta stele chante" },
                { "F3", "diagnostic" },
                { "Echap", "pause" }
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
