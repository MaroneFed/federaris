using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// L'ECRAN DE JEU (La Couronne, 26/09 -- Martin : "pas de boussole, rien : un bel
    /// ecran avec une belle barre de vie, des beaux trucs, ta capacite").
    ///
    ///       [ Manche 2/5 ]   [ 4:12 ]   [ ● ● ○ ]  <- les manches gagnees, par joueur
    ///                         [couronne]           <- ou elle est : socle, porteur, par terre
    ///
    ///                            +                 <- le reticule
    ///                        [E] Coffre            <- ce qu'on peut prendre
    ///
    ///   (◆)(◆)                                     <- tes pouvoirs, en medaillons
    ///   ♥ ██████████████░░░░                       <- LA BARRE DE VIE
    ///                     [épée][1][2][3]          <- ce que tu tiens
    ///
    /// Presque pas de texte : des formes et des couleurs. Il ne decide RIEN. Il lit
    /// l'etat du jeu et l'affiche. La souris et la pause sont dans Menus.cs.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public OrbitCamera orbitCamera;
        public PlayerInteractor interactor;
        public Camera viewCamera;
        public Menus menus;

        bool showDiagnostic;

        // --- la carte de trouvaille
        string cardKicker, cardTitle, cardLine1;
        Color cardTint;
        float cardTimer;
        const float CardDuration = 5f;

        /// <summary>Une grande carte en haut de l'ecran, pour les moments qui comptent.</summary>
        public void ShowDiscovery(string kicker, string title, string line1, string line2, Color tint)
        {
            cardKicker = kicker;
            cardTitle = title;
            cardLine1 = line1;
            cardTint = tint;
            cardTimer = CardDuration;
        }

        // --- les coups, la chute
        float hurtFlash;
        float deathTimer;
        string killedBy;
        float ghostHealth = -1f;    // la trainee claire qui suit la vie qui descend

        /// <summary>Vrai pendant les quelques secondes ou l'on est tombe : les entrees sont figees.</summary>
        public bool Dead { get { return deathTimer > 0f; } }

        public void Hurt()
        {
            hurtFlash = 1f;
        }

        /// <summary>"how" : comment on est tombe ("sous le glaive de la Garde Pale", "dans un piege").</summary>
        public void ShowDeath(string how)
        {
            killedBy = string.IsNullOrEmpty(how) ? "" : char.ToUpperInvariant(how[0]) + how.Substring(1);
            deathTimer = Combat.RespawnSeconds;
            Sfx.Bell();
        }

        // --- un voile de couleur sur tout l'ecran (un objet utilise, l'escalier derobe)
        float flash;
        Color flashTint;

        public void Flash(Color tint)
        {
            flash = 1f;
            flashTint = tint;
        }

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
            if (hurtFlash > 0f) hurtFlash = Mathf.Max(0f, hurtFlash - Time.unscaledDeltaTime * 1.6f);
            if (FiefInput.DiagnosticPressed) showDiagnostic = !showDiagnostic;
            TickLife();
        }

        void TickLife()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            if (ghostHealth < 0f) ghostHealth = me.Health;
            ghostHealth = ghostHealth > me.Health ? Mathf.MoveTowards(ghostHealth, me.Health, Time.deltaTime * 40f * (Time.time - me.LastHurt > 0.6f ? 1f : 0f)) : me.Health;

            if (deathTimer > 0f)
            {
                deathTimer -= Time.deltaTime;
                if (deathTimer <= 0f)
                {
                    // Se relever, les mains vides, a son point de depart.
                    Vector3 at = Combat.RespawnPoint(me, me.Body != null ? me.Body.position : Vector3.zero);
                    Vector3 look = -new Vector3(at.x, 0f, at.z);
                    if (Game.Player != null) Game.Player.Teleport(at, Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg);
                    me.Health = me.MaxHealth;
                    me.SecondChanceUsed = false;
                    ghostHealth = me.Health;
                }
                return;
            }
            // La vie remonte apres six secondes sans coup (trois fois plus vite avec Sang vif).
            if (me.Alive && Time.time - me.LastHurt > 6f) me.Heal((me.Has(Power.SangVif) ? 9f : 3f) * Time.deltaTime);
        }

        bool Hidden { get { return menus != null && menus.Blocking; } }

        void OnGUI()
        {
            UiStyle.Ensure();
            if (Hidden) return;

            DrawMarkers();
            FloatingTexts.Draw(viewCamera != null ? viewCamera : Camera.main);
            DrawTop();
            DrawKing();
            DrawLifeBar();
            DrawHands();
            DrawPrompt();
            DrawCentre();
            DrawDiscovery();
            Toasts.Draw();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();
            DrawDanger();
            if (FiefInput.ScoresHeld) DrawScores();
            DrawVeils();
        }

        // ================================================================== le haut

        /// <summary>
        /// Le haut de l'ecran : le numero de la manche, le chrono, et une banniere par
        /// joueur -- sa couleur et ses manches gagnees en points. En dessous, la
        /// Couronne : ou elle est.
        /// </summary>
        void DrawTop()
        {
            Season season = Game.Season;
            if (season == null) return;

            // --- le chrono
            float left = season.Remaining;
            bool late = left < 60f;
            float cw = UiStyle.S(112), ch = UiStyle.S(38);
            Rect plate = new Rect((Screen.width - cw) * 0.5f, UiStyle.S(16), cw, ch);
            UiStyle.DropShadow(plate, UiStyle.S(10));
            GUI.Box(plate, GUIContent.none, UiStyle.CardBox);
            GUIStyle clockStyle = UiStyle.Value;
            TextAnchor previous = clockStyle.alignment;
            clockStyle.alignment = TextAnchor.MiddleCenter;
            Color clock = late ? Color.Lerp(new Color(0.95f, 0.42f, 0.3f), UiStyle.Ink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : UiStyle.Ink;
            UiStyle.Tinted(plate, Clock(left), clockStyle, clock);
            clockStyle.alignment = previous;

            // --- la manche (a gauche du chrono)
            string round = Match.IsTieBreak ? "DÉPARTAGE" : Match.RoundNumber + " / " + Match.Rounds;
            GUIStyle tiny = RightTiny();
            UiStyle.Tinted(new Rect(plate.x - UiStyle.S(130), plate.y, UiStyle.S(120), ch), round, tiny, UiStyle.InkDim);

            // --- les joueurs (a droite du chrono) : une banniere et des points
            float x = plate.xMax + UiStyle.S(12);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                float bw = UiStyle.S(16) + Mathf.Max(1, Match.Rounds / 2 + 1) * UiStyle.S(9);
                bw = Mathf.Min(bw, UiStyle.S(64));
                Rect b = new Rect(x, plate.y + UiStyle.S(4), bw, ch - UiStyle.S(8));
                UiStyle.Fill(b, new Color(0.04f, 0.035f, 0.03f, 0.72f));
                UiStyle.Fill(new Rect(b.x, b.y, UiStyle.S(4), b.height), s.Colour);
                if (s.IsLocal) UiStyle.Fill(new Rect(b.x, b.yMax - 2f, b.width, 2f), Palette.Gold);
                int shown = Mathf.Min(s.Wins, 5);
                float d = UiStyle.S(7);
                for (int k = 0; k < Mathf.Max(1, shown); k++)
                {
                    Rect dot = new Rect(b.x + UiStyle.S(10) + k * UiStyle.S(9), b.center.y - d * 0.5f, d, d);
                    UiStyle.Icon(dot, UiStyle.Shape.Dot, k < s.Wins ? s.Colour : new Color(1f, 1f, 1f, 0.12f));
                }
                if (s.Wins > 5) UiStyle.Tinted(new Rect(b.x, b.y, b.width - UiStyle.S(3), b.height), s.Wins.ToString(), RightTiny(), s.Colour);
                x = b.xMax + UiStyle.S(5);
            }

            // --- la Couronne, sous le chrono
            float cs = UiStyle.S(34);
            Rect crown = new Rect((Screen.width - cs) * 0.5f, plate.yMax + UiStyle.S(6), cs, cs * 0.8f);
            Crown.State where = Crown.Where;
            Seeker holder = Crown.Holder;
            if (where == Crown.State.Carried && holder != null)
            {
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
                UiStyle.Icon(new Rect(crown.x - cs * 0.3f, crown.y - cs * 0.3f, cs * 1.6f, cs * 1.4f), UiStyle.Shape.Dot,
                             new Color(holder.Colour.r, holder.Colour.g, holder.Colour.b, 0.35f * pulse));
                Pictos.Crown(crown, 1f);
                UiStyle.Fill(new Rect(crown.x, crown.yMax + UiStyle.S(2), crown.width, UiStyle.S(3)), holder.Colour);
            }
            else if (where == Crown.State.Dropped)
            {
                // Par terre : elle clignote, et le compte a rebours avant qu'elle rentre au donjon.
                Pictos.Crown(crown, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)));
                UiStyle.Tinted(new Rect(crown.x - UiStyle.S(30), crown.yMax, crown.width + UiStyle.S(60), UiStyle.S(18)),
                               Mathf.CeilToInt(Crown.ReturnIn).ToString(), UiStyle.CenteredSmall, new Color(1f, 0.8f, 0.4f, 0.8f));
            }
            else Pictos.Crown(crown, 0.4f);
        }

        /// <summary>LE ROI CREUX, eveille et proche : sa barre en haut, comme un boss.</summary>
        void DrawKing()
        {
            Guard king = Guard.King;
            if (king == null || !king.Alive || king.Asleep || Game.PlayerTransform == null) return;
            if ((king.transform.position - Game.PlayerTransform.position).magnitude > 45f) return;
            float w = Mathf.Min(UiStyle.S(560), Screen.width - UiStyle.S(80));
            Rect bar = new Rect((Screen.width - w) * 0.5f, UiStyle.S(118), w, UiStyle.S(9));
            GUIStyle title = UiStyle.CenteredSmall;
            UiStyle.Tinted(new Rect(bar.x, bar.y - UiStyle.S(20), w, UiStyle.S(18)), UiStyle.Spaced("LE ROI CREUX"), title, new Color(0.75f, 0.85f, 1f));
            UiStyle.Bar(bar, king.Health01, new Color(0.55f, 0.1f, 0.12f), UiStyle.BarBg);
        }

        // ================================================================== la vie

        /// <summary>
        /// LA BARRE DE VIE, en bas a gauche : longue, cerclee de bronze, graduee tous
        /// les 25 points. Quand on encaisse, la partie perdue reste un instant en clair
        /// avant de fondre -- on VOIT combien le coup a coute. Au-dessus : les pouvoirs,
        /// et les etats (ralenti, pris, invisible, plume).
        /// </summary>
        void DrawLifeBar()
        {
            Seeker me = Game.Me;
            if (me == null || deathTimer > 0f) return;
            float pad = UiStyle.S(28);
            float w = UiStyle.S(me.Has(Power.Colosse) ? 390 : 320);
            float h = UiStyle.S(16);
            Rect bar = new Rect(pad + UiStyle.S(30), Screen.height - pad - h, w, h);

            // Le coeur, a gauche.
            float hd = UiStyle.S(26);
            Rect heart = new Rect(pad - UiStyle.S(4), bar.center.y - hd * 0.5f, hd, hd);
            float beat = me.Health < me.MaxHealth * 0.3f ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 1f;
            Rect beating = new Rect(heart.center.x - hd * beat * 0.5f, heart.center.y - hd * beat * 0.5f, hd * beat, hd * beat);
            Heart(beating, new Color(0.85f, 0.16f, 0.16f));

            // Le cadre : ombre, fond creuse, filets de bronze.
            UiStyle.DropShadow(bar, UiStyle.S(10));
            Rect frame = new Rect(bar.x - 3f, bar.y - 3f, bar.width + 6f, bar.height + 6f);
            UiStyle.Fill(frame, new Color(0.55f, 0.44f, 0.26f, 0.9f));
            UiStyle.Fill(new Rect(frame.x + 1f, frame.y + 1f, frame.width - 2f, frame.height - 2f), new Color(0.03f, 0.02f, 0.02f, 0.95f));
            float max = me.MaxHealth;
            float f = Mathf.Clamp01(me.Health / max);
            float g = Mathf.Clamp01(ghostHealth / max);
            if (g > f) UiStyle.Fill(new Rect(bar.x, bar.y, bar.width * g, bar.height), new Color(0.95f, 0.85f, 0.7f, 0.85f));
            if (f > 0f)
            {
                Color deep = new Color(0.55f, 0.06f, 0.07f), bright = new Color(0.86f, 0.18f, 0.16f);
                UiStyle.Fill(new Rect(bar.x, bar.y, bar.width * f, bar.height), deep);
                UiStyle.Fill(new Rect(bar.x, bar.y, bar.width * f, bar.height * 0.55f), bright);
                UiStyle.Fill(new Rect(bar.x, bar.y + 1f, bar.width * f, 1f), new Color(1f, 0.7f, 0.6f, 0.45f));
            }
            for (float k = 25f; k < max - 1f; k += 25f)
                UiStyle.Fill(new Rect(bar.x + bar.width * (k / max), bar.y, 1f, bar.height), new Color(0f, 0f, 0f, 0.55f));
            // La vie qui remonte : un petit reflet qui glisse.
            if (me.Health < max && Time.time - me.LastHurt > 6f)
            {
                float u = Mathf.Repeat(Time.unscaledTime * 0.8f, 1f);
                UiStyle.Fill(new Rect(bar.x + bar.width * f * u, bar.y, UiStyle.S(10), bar.height), new Color(1f, 0.8f, 0.7f, 0.2f));
            }

            // --- les pouvoirs, en medaillons au-dessus de la barre
            float md = UiStyle.S(40);
            float mx = bar.x;
            float my = bar.y - md - UiStyle.S(14);
            List<Power> powers = me.Slot.Powers;
            for (int i = 0; i < powers.Count; i++)
            {
                Rect m = new Rect(mx + i * (md + UiStyle.S(8)), my, md, md);
                Medallion(m, powers[i], me);
            }

            // --- les etats, apres les pouvoirs
            float sx = mx + powers.Count * (md + UiStyle.S(8)) + (powers.Count > 0 ? UiStyle.S(10) : 0f);
            float sd = UiStyle.S(26);
            float sy = my + (md - sd) * 0.5f;
            if (Time.time < me.SlowUntil) { Status(new Rect(sx, sy, sd, sd), Item.Lenteur, me.SlowUntil - Time.time, Thrown.SlowSeconds); sx += sd + UiStyle.S(6); }
            if (me.Rooted) { Status(new Rect(sx, sy, sd, sd), Item.Piege, me.RootedUntil - Time.time, Trap.HoldSeconds); sx += sd + UiStyle.S(6); }
            if (me.Hidden) { Status(new Rect(sx, sy, sd, sd), Item.CapeOmbre, me.HiddenUntil - Time.time, 10f); sx += sd + UiStyle.S(6); }
            if (Time.time < me.FeatherUntil) { Status(new Rect(sx, sy, sd, sd), Item.Plume, me.FeatherUntil - Time.time, 30f); }
        }

        /// <summary>Un pouvoir : un rond sombre cercle de sa couleur, son pictogramme, et sa recharge.</summary>
        void Medallion(Rect m, Power p, Seeker me)
        {
            Color tint = PowerInfo.Tint(p);
            UiStyle.Icon(new Rect(m.x - 2f, m.y - 2f, m.width + 4f, m.height + 4f), UiStyle.Shape.Dot, new Color(tint.r, tint.g, tint.b, 0.75f));
            UiStyle.Icon(m, UiStyle.Shape.Dot, new Color(0.05f, 0.04f, 0.04f, 0.95f));
            float ready = 1f;
            if (p == Power.Ruee && Game.Player != null) ready = Game.Player.DashReady01;
            if (p == Power.Poigne) ready = ToolUser.ShoveReady01;
            if (p == Power.SecondeChance && me.SecondChanceUsed) ready = 0f;
            Pictos.Draw(Inset(m, 0.2f), p, ready < 1f);
            if (ready < 1f && ready > 0f)
                UiStyle.Fill(new Rect(m.x + m.width * 0.2f, m.yMax - UiStyle.S(5), m.width * 0.6f * ready, UiStyle.S(3)), tint);
            if (p == Power.Ruee)
                UiStyle.Tinted(new Rect(m.xMax - UiStyle.S(12), m.yMax - UiStyle.S(14), UiStyle.S(14), UiStyle.S(14)), "R", UiStyle.Tiny, Palette.Gold);
        }

        void Status(Rect r, Item item, float left, float total)
        {
            UiStyle.Icon(r, UiStyle.Shape.Dot, new Color(0.05f, 0.04f, 0.04f, 0.9f));
            Pictos.Draw(Inset(r, 0.14f), item, false);
            UiStyle.Fill(new Rect(r.x, r.yMax + 2f, r.width * Mathf.Clamp01(left / total), 2f), ItemInfo.Tint(item));
        }

        static void Heart(Rect r, Color c)
        {
            float d = r.width * 0.56f;
            UiStyle.Icon(new Rect(r.x, r.y + r.height * 0.08f, d, d), UiStyle.Shape.Dot, c);
            UiStyle.Icon(new Rect(r.xMax - d, r.y + r.height * 0.08f, d, d), UiStyle.Shape.Dot, c);
            UiStyle.Icon(new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.2f, r.width * 0.84f, r.height * 0.8f), UiStyle.Shape.Diamond, c);
        }

        // ================================================================== les mains

        /// <summary>
        /// CE QUE TU TIENS, en bas au centre : l'epee (toujours la), puis les trois
        /// emplacements d'objets (1, 2, 3). Celui en main a un bord dore. Quand tu
        /// portes la Couronne, tout s'efface derriere elle.
        /// </summary>
        void DrawHands()
        {
            Seeker me = Game.Me;
            if (me == null || deathTimer > 0f) return;
            Loadout kit = me.Items;
            float size = UiStyle.S(52), gap = UiStyle.S(6);
            float total = size * (Loadout.Size + 1) + gap * Loadout.Size + UiStyle.S(10);
            float x = (Screen.width - total) * 0.5f;
            float y = Screen.height - size - UiStyle.S(22);

            if (me.CarriesCrown)
            {
                Rect big = new Rect((Screen.width - size * 1.6f) * 0.5f, y - size * 0.3f, size * 1.6f, size * 1.3f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                UiStyle.Icon(new Rect(big.x - size * 0.4f, big.y - size * 0.4f, big.width + size * 0.8f, big.height + size * 0.8f), UiStyle.Shape.Dot,
                             new Color(1f, 0.8f, 0.35f, 0.18f + 0.12f * pulse));
                Pictos.Crown(big, 1f);
                return;
            }

            // L'epee.
            Rect sword = new Rect(x, y, size, size);
            Slot(sword, kit.HoldingSword);
            Pictos.Sword(Inset(sword, 0.14f), 1f);
            x += size + gap + UiStyle.S(10);

            for (int i = 0; i < Loadout.Size; i++)
            {
                Rect r = new Rect(x, y, size, size);
                bool active = kit.Active == i;
                Slot(r, active);
                UiStyle.Tinted(new Rect(r.x + UiStyle.S(5), r.y + UiStyle.S(2), UiStyle.S(20), UiStyle.S(16)), (i + 1).ToString(), UiStyle.Tiny,
                               active ? Palette.Gold : UiStyle.InkFaint);
                Item it = kit.Slots[i];
                if (it != Item.None) Pictos.Draw(Inset(r, 0.16f), it, false);
                x += size + gap;
            }

            // Le detecteur en main : a droite, une jauge qui monte a l'approche d'un tresor.
            if (kit.Held == Item.Detecteur)
            {
                float d = ToolUser.DetectorDistance;
                float k = d < 0f ? 0f : 1f - Mathf.Clamp01(d / ToolUser.DetectorRange);
                Rect meter = new Rect(x + UiStyle.S(8), y + UiStyle.S(6), UiStyle.S(12), size - UiStyle.S(12));
                UiStyle.Fill(meter, UiStyle.BarBg);
                float lit = Time.time - ToolUser.LastBeep < 0.08f ? 1f : 0.7f;
                UiStyle.Fill(new Rect(meter.x, meter.yMax - meter.height * k, meter.width, meter.height * k), new Color(0.45f, 1f, 0.5f, lit));
            }
        }

        // ================================================================== le centre

        void DrawCentre()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            bool foe = ToolUser.FoeInReach;
            bool usable = interactor != null && interactor.Current != null;
            if (foe || usable)
            {
                float d = UiStyle.S(foe ? 12 : 10);
                Color c = foe ? new Color(1f, 0.35f, 0.28f, 0.9f) : new Color(1f, 0.85f, 0.5f, 0.75f);
                UiStyle.Icon(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), UiStyle.Shape.Diamond, c);
                float inner = d * 0.5f;
                UiStyle.Icon(new Rect(cx - inner * 0.5f, cy - inner * 0.5f, inner, inner), UiStyle.Shape.Diamond, new Color(0.05f, 0.04f, 0.03f, 0.8f));
            }
            else
            {
                float d = UiStyle.S(4);
                UiStyle.Icon(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), UiStyle.Shape.Dot, new Color(1f, 1f, 1f, 0.6f));
            }
            // La poussee se recharge : un petit arc sous le reticule.
            if (ToolUser.ShoveReady01 < 1f)
            {
                float w = UiStyle.S(26);
                UiStyle.Fill(new Rect(cx - w * 0.5f, cy + UiStyle.S(12), w * ToolUser.ShoveReady01, 2f), new Color(1f, 1f, 1f, 0.45f));
            }
            if (!string.IsNullOrEmpty(ToolUser.Hint)) KeyHints(ToolUser.Hint, cy + UiStyle.S(24));
        }

        void DrawPrompt()
        {
            if (interactor == null || deathTimer > 0f) return;
            IInteractable target = interactor.Current;
            if (target == null) return;

            GUIStyle label = UiStyle.Label;
            float textW = label.CalcSize(new GUIContent(target.Prompt)).x;
            float capW = UiStyle.S(30);
            float w = Mathf.Min(Screen.width - UiStyle.S(40), textW + capW + UiStyle.S(40));
            float h = UiStyle.S(40);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.5f + UiStyle.S(64), w, h);
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

        /// <summary>"F|↑;clic|⛏" : une rangee de touches dessinees, chacune suivie d'un signe.</summary>
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

        // ================================================================== les autres

        /// <summary>
        /// Au-dessus des tetes : les gardes ("?" quand ils se doutent, "!" quand ils
        /// courent), et les autres joueurs -- leur nom a leur couleur quand on est pres.
        /// </summary>
        void DrawMarkers()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || Game.PlayerTransform == null) return;
            Vector3 me = Game.PlayerTransform.position;

            for (int i = 0; i < Guard.All.Count; i++)
            {
                Guard g = Guard.All[i];
                if (g == null || !g.Alive || g.Asleep || (me - g.transform.position).magnitude > 40f) continue;
                float h = g.rank == Guard.Rank.Roi ? 4f : g.rank == Guard.Rank.Molosse ? 1.6f : 2.8f;
                if (g.Chasing && g.Target != null && g.Target.IsPlayer) DrawAlert(cam, g.transform.position + Vector3.up * h, "!", new Color(1f, 0.3f, 0.2f), 1f);
                else if (!g.Chasing && g.Suspicion > 0.03f) DrawAlert(cam, g.transform.position + Vector3.up * h, "?", new Color(1f, 0.85f, 0.3f), g.Suspicion);
            }

            // LE FLAIR (un pouvoir) : on sent la Couronne a travers les murs et la brume.
            // Sa place s'affiche toujours -- au bord de l'ecran si elle est hors champ.
            Seeker mine = Game.Me;
            if (mine != null && mine.Has(Power.Flair) && Crown.Instance != null && !mine.CarriesCrown)
            {
                Vector3 sp = cam.WorldToScreenPoint(Crown.Position + Vector3.up * 0.4f);
                bool behind = sp.z <= 0f;
                float gx = sp.x, gy = Screen.height - sp.y;
                if (behind) { gx = Screen.width - gx; gy = Screen.height - gy; }
                float m = UiStyle.S(46);
                bool off = behind || gx < m || gx > Screen.width - m || gy < m || gy > Screen.height - m;
                if (off)
                {
                    // Sur le bord, dans la bonne direction.
                    Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 d = new Vector2(gx, gy) - c;
                    if (behind && d.sqrMagnitude < 1f) d = Vector2.down;
                    float k = Mathf.Min((Screen.width * 0.5f - m) / Mathf.Max(1f, Mathf.Abs(d.x)), (Screen.height * 0.5f - m) / Mathf.Max(1f, Mathf.Abs(d.y)));
                    gx = c.x + d.x * k;
                    gy = c.y + d.y * k;
                }
                float cs = UiStyle.S(off ? 26 : 32);
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f);
                UiStyle.Icon(new Rect(gx - cs, gy - cs, cs * 2f, cs * 2f), UiStyle.Shape.Dot, new Color(1f, 0.8f, 0.35f, 0.18f * pulse));
                Pictos.Crown(new Rect(gx - cs * 0.5f, gy - cs * 0.4f, cs, cs * 0.8f), 0.85f);
                if (Crown.Holder != null)
                    UiStyle.Fill(new Rect(gx - cs * 0.5f, gy + cs * 0.45f, cs, UiStyle.S(3)), Crown.Holder.Colour);
            }

            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.IsPlayer || !s.Alive || s.Body == null) continue;
                float d = (me - s.Body.position).magnitude;
                if (d > 22f) continue;
                Vector3 sp = cam.WorldToScreenPoint(s.Body.position + Vector3.up * 2.75f);
                if (sp.z <= 0f) continue;
                float a = Mathf.Clamp01((22f - d) / 6f);
                Rect r = new Rect(sp.x - UiStyle.S(80), Screen.height - sp.y - UiStyle.S(10), UiStyle.S(160), UiStyle.S(20));
                UiStyle.Tinted(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), s.Name, UiStyle.CenteredSmall, new Color(0f, 0f, 0f, 0.8f * a));
                UiStyle.Tinted(r, s.Name, UiStyle.CenteredSmall, new Color(s.Colour.r, s.Colour.g, s.Colour.b, a));
            }
        }

        /// <summary>Un grand signe au-dessus d'une tete ("?", "!"), qui grossit avec l'alerte.</summary>
        void DrawAlert(Camera cam, Vector3 world, string sign, Color color, float level)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            float size = UiStyle.S(24 + 18 * Mathf.Clamp01(level));
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

        /// <summary>
        /// On te court apres (un garde, un bot) : les bords de l'ecran battent en rouge,
        /// au rythme d'un coeur. Pas un mot : on comprend.
        /// </summary>
        void DrawDanger()
        {
            if (!Guard.HuntingPlayer && !Rival.HuntingPlayer) return;
            float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)), 6f);
            Color c = new Color(0.7f, 0.05f, 0.03f, 0.16f + 0.2f * beat);
            float e = UiStyle.S(40);
            UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, e), c);
            UiStyle.FadeBand(new Rect(0f, Screen.height - e, Screen.width, e), c);
        }

        /// <summary>Tab maintenu : le tableau du match (manches gagnees, pouvoirs de chacun).</summary>
        void DrawScores()
        {
            float w = UiStyle.S(520), row = UiStyle.S(46);
            float h = UiStyle.S(60) + row * Match.Slots.Count;
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.28f, w, h);
            UiStyle.Frame(box);
            float x = box.x + UiStyle.S(18), y = box.y + UiStyle.S(14);
            UiStyle.Tinted(new Rect(x, y, w, UiStyle.S(24)), UiStyle.Spaced("LE MATCH"), UiStyle.Head, Palette.Gold);
            y += UiStyle.S(36);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                UiStyle.Fill(new Rect(x, y + UiStyle.S(6), UiStyle.S(5), row - UiStyle.S(12)), s.Colour);
                UiStyle.Tinted(new Rect(x + UiStyle.S(14), y, UiStyle.S(140), row), s.Name, UiStyle.Label, s.IsLocal ? Palette.Gold : UiStyle.Ink);
                for (int k = 0; k < s.Wins; k++)
                    Pictos.Crown(new Rect(x + UiStyle.S(150) + k * UiStyle.S(24), y + UiStyle.S(12), UiStyle.S(22), UiStyle.S(18)), 1f);
                float px = box.xMax - UiStyle.S(18);
                for (int k = s.Powers.Count - 1; k >= 0; k--)
                {
                    px -= UiStyle.S(30);
                    Rect m = new Rect(px, y + UiStyle.S(9), UiStyle.S(28), UiStyle.S(28));
                    UiStyle.Icon(m, UiStyle.Shape.Dot, new Color(0.05f, 0.04f, 0.04f, 0.95f));
                    Pictos.Draw(Inset(m, 0.18f), s.Powers[k], false);
                }
                y += row;
            }
        }

        // ================================================================== trouvaille

        void DrawDiscovery()
        {
            if (cardTimer <= 0f || string.IsNullOrEmpty(cardTitle)) return;

            float age = CardDuration - cardTimer;
            float alpha = Mathf.Clamp01(age / 0.35f) * Mathf.Clamp01(cardTimer / 0.8f);
            bool detailed = !string.IsNullOrEmpty(cardLine1);

            float w = UiStyle.S(460);
            float h = UiStyle.S(detailed ? 104 : 74);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(150) - (1f - Mathf.Clamp01(age / 0.35f)) * UiStyle.S(12), w, h);

            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            UiStyle.Frame(box);
            UiStyle.Fill(new Rect(box.x + UiStyle.S(10), box.y, box.width - UiStyle.S(20), 2f), cardTint);
            float y = box.y + UiStyle.S(8);
            if (!string.IsNullOrEmpty(cardKicker))
                UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(16)), cardKicker, UiStyle.CenteredSmall, UiStyle.InkDim);
            y += UiStyle.S(10);
            GUIStyle title = UiStyle.Title;
            TextAnchor previous = title.alignment;
            title.alignment = TextAnchor.MiddleCenter;
            UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(40)), cardTitle, title, cardTint);
            title.alignment = previous;
            y += UiStyle.S(44);
            if (detailed) UiStyle.Tinted(new Rect(box.x, y, w, UiStyle.S(22)), cardLine1, UiStyle.Centered, UiStyle.Ink);
            GUI.color = was;
        }

        // ================================================================== les voiles

        void DrawVeils()
        {
            if (flash > 0f)
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(flashTint.r, flashTint.g, flashTint.b, flash * flash * 0.5f * flashTint.a));
            if (hurtFlash > 0f)
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.6f, 0.02f, 0.02f, hurtFlash * 0.32f));
            Seeker me = Game.Me;
            if (me != null && me.Alive && me.Health < me.MaxHealth * 0.3f)
            {
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.5f)), 4f);
                float e = UiStyle.S(90);
                Color c = new Color(0.5f, 0.02f, 0.02f, 0.25f + 0.2f * beat);
                UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, e), c);
                UiStyle.FadeBand(new Rect(0f, Screen.height - e, Screen.width, e), c);
            }

            if (deathTimer > 0f)
            {
                float a = Mathf.Clamp01((Combat.RespawnSeconds - deathTimer) / 0.8f);
                UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.01f, 0.01f, a * 0.9f));
                GUIStyle big = UiStyle.Big;
                TextAnchor previous = big.alignment;
                big.alignment = TextAnchor.MiddleCenter;
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.38f, Screen.width, UiStyle.S(70)), UiStyle.Spaced("TU ES TOMBÉ"), big,
                               new Color(0.85f, 0.3f, 0.25f, a));
                big.alignment = previous;
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.38f + UiStyle.S(70), Screen.width, UiStyle.S(24)), killedBy, UiStyle.Centered,
                               new Color(0.8f, 0.72f, 0.6f, a * 0.8f));
                UiStyle.Tinted(new Rect(0f, Screen.height * 0.38f + UiStyle.S(104), Screen.width, UiStyle.S(24)), Mathf.CeilToInt(deathTimer).ToString(),
                               UiStyle.Centered, new Color(1f, 1f, 1f, a * 0.6f));
            }
        }

        // ================================================================== outils

        public static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
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
            if (active) UiStyle.Fill(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), new Color(0.86f, 0.7f, 0.36f, 0.1f));
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

            UiStyle.DropShadow(box, UiStyle.S(18));
            UiStyle.Fill(box, new Color(0.22f, 0.05f, 0.05f, 0.96f));
            UiStyle.Fill(new Rect(box.x, box.y, box.width, 3f), new Color(0.90f, 0.25f, 0.20f));

            float x = box.x + UiStyle.S(16);
            UiStyle.Tinted(new Rect(x, box.y + UiStyle.S(10), w, UiStyle.S(26)),
                           "LA CONSTRUCTION DU MONDE A ÉCHOUÉ", UiStyle.Head, new Color(1f, 0.55f, 0.45f));

            GUIStyle wrapped = UiStyle.Small;
            bool previousWrap = wrapped.wordWrap;
            wrapped.wordWrap = true;
            GUI.Label(new Rect(x, box.y + UiStyle.S(38), w - UiStyle.S(32), h - UiStyle.S(48)), Game.BuildError, wrapped);
            wrapped.wordWrap = previousWrap;
        }

        /// <summary>
        /// Panneau F3. Il repond a la question "je suis dans quoi ?" : ce qui colle a
        /// l'oeil, ce que touche un rayon tire vers l'avant, et quelques compteurs.
        /// </summary>
        void DrawDiagnostic()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            float w = UiStyle.S(500);
            float h = UiStyle.S(300);
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
            y = Line(x, y, inner, "Espace colorimetrique", QualitySettings.activeColorSpace == ColorSpace.Linear ? "linéaire" : "gamma");
            y = Line(x, y, inner, "Gardes / bots / coffres", Guard.All.Count + " / " + Rival.All.Count + " / " + Chest.All.Count);
            string crown = Crown.Holder != null ? "portée par " + Crown.Holder.Name : Crown.Where == Crown.State.Dropped ? "par terre" : "sur son socle";
            y = Line(x, y, inner, "Couronne", crown);
            if (Game.PlayerTransform != null)
            {
                Vector3 p = Game.PlayerTransform.position;
                y = Line(x, y, inner, "Joueur", p.x.ToString("0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0"));
                y = Line(x, y, inner, "Sol sous les pieds", Ground.Sample(p.x, p.z).ToString("0.0") + " m");
            }
            if (cam != null)
            {
                Vector3 c = cam.transform.position;
                Collider[] touching = Physics.OverlapSphere(c, 0.25f, ~0, QueryTriggerInteraction.Ignore);
                y = Line(x, y, inner, "Solides autour de l'oeil",
                         touching.Length == 0 ? "aucun" : touching[0].gameObject.name + (touching.Length > 1 ? " +" + (touching.Length - 1) : ""));
                RaycastHit hit;
                string ahead = "rien à moins de 40 m";
                if (Physics.Raycast(c, cam.transform.forward, out hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                    ahead = hit.collider.gameObject.name + " a " + hit.distance.ToString("0.0") + " m";
                y = Line(x, y, inner, "Devant toi", ahead);
            }
        }

        float Line(float x, float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, width * 0.52f, UiStyle.S(20)), label, UiStyle.Small);
            UiStyle.Tinted(new Rect(x + width * 0.52f, y, width * 0.48f, UiStyle.S(20)), value, UiStyle.Small, Palette.Gold);
            return y + UiStyle.S(21);
        }
    }
}
