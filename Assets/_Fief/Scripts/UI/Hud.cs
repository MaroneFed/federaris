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

        // --- le grand titre du moment (Couronne prise, don recu...)
        string cardKicker, cardTitle, cardLine;
        Color cardTint;
        float cardTimer;
        const float CardDuration = 3.2f;

        /// <summary>Un titre au milieu du haut de l'ecran, pour les moments qui comptent.</summary>
        public void ShowDiscovery(string kicker, string title, string line1, string line2, Color tint)
        {
            cardKicker = kicker;
            cardTitle = title;
            cardLine = line1;
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
            List<Ability> list = me.Slot.Actives;
            if (me.HasGift) list.Add(me.Gift);
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
            Vector3 p = me.Body.position;
            if (me.CarriesCrown) Tip("porte", "Tu brilles : tout le monde te voit. File au Monument — si tu sautes de haut, la Couronne reste là-haut.");
            else if (Updraft.Near(p, 5f) != null) Tip("courant", "Un courant : marche dans le disque pour monter d'un tour.");
            else if (Tower.On(p) && Tower.Progress(p) > 0.2f) Tip("trou", "Les trous se sautent en courant : Maj + Espace. Attention aux pendules.");
            else if (Tower.On(p)) Tip("rampe", "La rampe monte jusqu'à la Couronne. Pousse les autres dans le vide : clic gauche.");
            else if (Eye.ChargingAt(me)) Tip("oeil", "Un Œil devient rouge quand il vise : fais un pas de côté au dernier moment.");
            else if (Crown.Holder != null) Tip("chasse", Crown.Holder.Name + " porte la Couronne : pousse-le pour qu'il la lâche.");
            else if (me.HasGift) Tip("don", "Ton don est sur la touche V, pour cette manche.");
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
            Toasts.Draw();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();
            if (FiefInput.ScoresHeld) DrawScores();
        }

        // ================================================================== le haut

        /// <summary>Le chrono, la manche, et ou est la Couronne -- en toutes lettres.</summary>
        void DrawTop()
        {
            Season season = Game.Season;
            if (season == null) return;

            float left = season.Remaining;
            bool late = left < 60f;
            Color clock = late ? Color.Lerp(new Color(1f, 0.4f, 0.3f), UiStyle.Ink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : UiStyle.Ink;
            Rect clockRect = new Rect(0f, UiStyle.S(14), Screen.width, UiStyle.S(40));
            // Les dix dernieres secondes : le chrono grossit a chaque battement.
            if (left <= 10f && left > 0f)
            {
                float beat = 1f - Mathf.Repeat(left, 1f);
                GUIStyle huge = Sized(UiStyle.Title, Mathf.Lerp(1.9f, 1.4f, beat));
                Text(new Rect(0f, UiStyle.S(10), Screen.width, UiStyle.S(64)), Mathf.CeilToInt(left).ToString(), huge, new Color(1f, 0.45f, 0.32f));
                clockRect.y += UiStyle.S(20);
            }
            else Text(clockRect, Clock(left), BigCentered(), clock);

            string round = Match.IsTieBreak ? "DÉPARTAGE" : "MANCHE " + Match.RoundNumber + " / " + Match.Rounds;
            Text(new Rect(0f, clockRect.yMax, Screen.width, UiStyle.S(18)), UiStyle.Spaced(round), UiStyle.CenteredSmall, UiStyle.InkDim);

            string line;
            Color tint;
            CrownLine(out line, out tint);
            Text(new Rect(0f, clockRect.yMax + UiStyle.S(22), Screen.width, UiStyle.S(24)), line, UiStyle.Centered, tint);

            // Ou j'en suis : mon tour de rampe, ou la distance du Monument si je porte.
            Seeker me = Game.Me;
            if (me == null || me.Body == null) return;
            string mine = null;
            if (me.CarriesCrown && Monument.Instance != null)
                mine = "Monument à " + Mathf.RoundToInt(Combat.Flat(Monument.Instance.transform.position - me.Body.position).magnitude) + " m";
            else if (Tower.On(me.Body.position))
                mine = Tower.Progress(me.Body.position) >= 0.999f ? "Au sommet" : "Tour " + (Mathf.FloorToInt(Tower.Progress(me.Body.position) * Tower.Turns) + 1) + " sur " + Tower.Turns;
            if (mine != null)
                Text(new Rect(0f, clockRect.yMax + UiStyle.S(46), Screen.width, UiStyle.S(20)), mine, UiStyle.CenteredSmall, new Color(0.9f, 0.86f, 0.76f, 0.8f));
        }

        /// <summary>Ou se trouve un joueur, en quelques mots.</summary>
        static string Where(Seeker s)
        {
            if (s == null || s.Body == null) return "";
            Vector3 p = s.Body.position;
            if (Tower.On(p)) return Tower.Progress(p) >= 0.999f ? "au sommet de la tour" : "sur la rampe";
            if (Castle.Inside(p)) return "dans la citadelle";
            if (Monument.Instance != null && Combat.Flat(Monument.Instance.transform.position - p).magnitude < 40f) return "près du Monument";
            return "en forêt";
        }

        /// <summary>Une phrase qui dit ou est la Couronne, et quoi faire.</summary>
        static void CrownLine(out string line, out Color tint)
        {
            Seeker holder = Crown.Holder;
            Seeker me = Game.Me;
            Crown.State where = Crown.Where;
            if (where == Crown.State.Carried && holder != null)
            {
                if (holder == me)
                {
                    float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
                    line = "TU PORTES LA COURONNE — entre dans le cercle du Monument (la colonne bleue)";
                    tint = new Color(1f, 0.82f * pulse + 0.1f, 0.4f);
                }
                else
                {
                    line = holder.Name + " porte la Couronne, " + Where(holder) + " — pousse-le";
                    tint = holder.Colour;
                }
                return;
            }
            if (where == Crown.State.Dropped)
            {
                line = "La Couronne est à terre — passe dessus ! (retour au sommet dans " + Mathf.CeilToInt(Crown.ReturnIn) + " s)";
                tint = new Color(1f, 0.78f, 0.4f);
                return;
            }
            line = "La Couronne est au sommet de la tour";
            tint = new Color(0.86f, 0.8f, 0.64f, 0.75f);
        }

        /// <summary>En haut a droite : les manches gagnees, un chiffre par joueur.</summary>
        void DrawStandings()
        {
            float w = UiStyle.S(170), row = UiStyle.S(20);
            float x = Screen.width - w - UiStyle.S(24), y = UiStyle.S(18);
            GUIStyle right = RightSmall();
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                Color c = s.IsLocal ? Palette.Gold : s.Colour;
                Text(new Rect(x, y, w - UiStyle.S(30), row), s.IsLocal ? "TOI" : s.Name, UiStyle.Small, c);
                Text(new Rect(x, y, w, row), s.Wins.ToString(), right, c);
                y += row;
            }
        }

        // ================================================================== les capacites

        /// <summary>
        /// En bas a gauche : une ligne par capacite active -- sa touche, son nom, et
        /// "prete" ou le temps qui reste (un trait qui se remplit). Puis le don, puis
        /// les passifs en une ligne. Pas d'icone : un nom se comprend tout de suite.
        /// </summary>
        void DrawAbilities()
        {
            Seeker me = Game.Me;
            if (me == null) return;
            List<Ability> actives = me.Slot.Actives;
            float now = Time.time;

            float row = UiStyle.S(26);
            float x = UiStyle.S(28);
            float keyW = UiStyle.S(96), nameW = UiStyle.S(150), stateW = UiStyle.S(110);
            int lines = actives.Count + (me.HasGift ? 1 : 0);
            float y = Screen.height - UiStyle.S(40) - row * lines;

            for (int i = 0; i < actives.Count; i++)
            {
                AbilityRow(x, y, keyW, nameW, stateW, AbilityInfo.Keys[Mathf.Min(i, AbilityInfo.Keys.Length - 1)], actives[i], me, now, false);
                y += row;
            }
            if (me.HasGift)
            {
                AbilityRow(x, y, keyW, nameW, stateW, AbilityInfo.GiftKey, me.Gift, me, now, true);
                y += row;
            }

            // Les passifs, en une ligne.
            string passives = "";
            List<Ability> all = me.Slot.Abilities;
            for (int i = 0; i < all.Count; i++)
            {
                if (AbilityInfo.IsActive(all[i])) continue;
                passives += (passives.Length > 0 ? "  ·  " : "") + AbilityInfo.Name(all[i]);
            }
            if (passives.Length > 0) Text(new Rect(x, y + UiStyle.S(4), UiStyle.S(600), UiStyle.S(18)), passives, UiStyle.Small, UiStyle.InkDim);

            // L'etat du moment, en mots, a cote.
            string state = null;
            Color sc = UiStyle.Ink;
            if (me.Stunned) { state = "ÉTOURDI"; sc = new Color(1f, 0.85f, 0.4f); }
            else if (me.Slowed) { state = "GELÉ — " + Mathf.CeilToInt(me.SlowUntil - now) + " s"; sc = AbilityInfo.Tint(Ability.Gel); }
            else if (me.Hidden) { state = "INVISIBLE — " + Mathf.CeilToInt(me.HiddenUntil - now) + " s"; sc = AbilityInfo.Tint(Ability.Voile); }
            else if (Game.Player != null && Game.Player.Gliding) { state = "PLANÉ"; sc = AbilityInfo.Tint(Ability.Planeur); }
            if (state != null)
                Text(new Rect(x, Screen.height - UiStyle.S(40) - row * lines - UiStyle.S(26), UiStyle.S(300), UiStyle.S(22)), state, UiStyle.Label, sc);
        }

        void AbilityRow(float x, float y, float keyW, float nameW, float stateW, string key, Ability a, Seeker me, float now, bool gift)
        {
            bool ready = me.Ready(a, now);
            bool blocked = AbilityCaster.WhyNot(me, a) == "Mains prises";
            Color tint = AbilityInfo.Tint(a);
            float h = UiStyle.S(24);

            // Elle vient de revenir : sa ligne s'eclaire une demi-seconde.
            float lit;
            if (readyFlash.TryGetValue(a, out lit) && Time.unscaledTime - lit < 0.5f)
                UiStyle.Fill(new Rect(x - UiStyle.S(6), y, keyW + nameW + UiStyle.S(80), h), new Color(tint.r, tint.g, tint.b, 0.25f * (1f - (Time.unscaledTime - lit) / 0.5f)));
            Text(new Rect(x, y, keyW, h), key.ToUpperInvariant(), UiStyle.Small, ready && !blocked ? Palette.Gold : UiStyle.InkFaint);
            Text(new Rect(x + keyW, y, nameW, h), AbilityInfo.Name(a), UiStyle.Label, ready && !blocked ? UiStyle.Ink : UiStyle.InkFaint);

            float sx = x + keyW + nameW;
            if (blocked) Text(new Rect(sx, y, stateW, h), "mains prises", UiStyle.Small, new Color(1f, 0.6f, 0.4f, 0.8f));
            else if (ready)
            {
                // Le trait plein, a la couleur de la capacite.
                UiStyle.Fill(new Rect(sx, y + h * 0.5f - 1f, UiStyle.S(60), UiStyle.S(3)), new Color(tint.r, tint.g, tint.b, 0.9f));
                // Une capacite qui vise : ce qu'elle toucherait maintenant.
                string note = gift ? "don" : null;
                Color nc = UiStyle.InkFaint;
                if (AbilityCaster.Aims(a) && viewCamera != null)
                {
                    string at = AbilityCaster.AimedAt(me, a, viewCamera.transform.position, viewCamera.transform.forward);
                    note = at != null ? "→ " + at : "rien en vue";
                    nc = at != null ? new Color(tint.r, tint.g, tint.b, 1f) : UiStyle.InkFaint;
                }
                if (note != null) Text(new Rect(sx + UiStyle.S(68), y, stateW + UiStyle.S(60), h), note, UiStyle.Small, nc);
            }
            else
            {
                float f = me.Ready01(a, now);
                UiStyle.Fill(new Rect(sx, y + h * 0.5f - 1f, UiStyle.S(60), UiStyle.S(3)), new Color(1f, 1f, 1f, 0.12f));
                UiStyle.Fill(new Rect(sx, y + h * 0.5f - 1f, UiStyle.S(60) * f, UiStyle.S(3)), new Color(tint.r, tint.g, tint.b, 0.55f));
                float rem = me.Remaining(a, now);
                Text(new Rect(sx + UiStyle.S(68), y, stateW, h), rem < 1f ? rem.ToString("0.0") : Mathf.CeilToInt(rem).ToString(), UiStyle.Small, UiStyle.InkDim);
            }
        }

        // ================================================================== le centre

        void DrawCentre()
        {
            Seeker me = Game.Me;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;

            // Le point de visee : blanc, et rouge quand quelqu'un est a portee de poussee.
            bool foe = AbilityUser.FoeInReach;
            float d = UiStyle.S(foe ? 6 : 3);
            Color c = foe ? new Color(1f, 0.35f, 0.28f, 0.95f) : new Color(1f, 1f, 1f, 0.7f);
            UiStyle.Fill(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), c);
            if (foe && Stats.Shoves < 3) Text(new Rect(0f, cy + UiStyle.S(14), Screen.width, UiStyle.S(18)), "clic gauche  pousser", UiStyle.CenteredSmall, new Color(1f, 0.6f, 0.5f, 0.85f));

            // La recharge de la poussee : un trait fin sous le point.
            if (me != null && Time.time < me.ShoveReadyAt)
            {
                float total = Seeker.ShoveCooldown * (me.Has(Ability.Poigne) ? 0.6f : 1f);
                float f = 1f - Mathf.Clamp01((me.ShoveReadyAt - Time.time) / total);
                float w = UiStyle.S(24);
                UiStyle.Fill(new Rect(cx - w * 0.5f, cy + UiStyle.S(9), w * f, 2f), new Color(1f, 1f, 1f, 0.45f));
            }

            // Le dernier refus ("Recharge", "Mains prises") : une seconde, sous le point.
            float since = Time.time - AbilityUser.RefusalAt;
            if (!string.IsNullOrEmpty(AbilityUser.Refusal) && since < 1.1f)
            {
                float a = 1f - Mathf.Clamp01((since - 0.7f) / 0.4f);
                Text(new Rect(0f, cy + UiStyle.S(34), Screen.width, UiStyle.S(22)), AbilityUser.Refusal, UiStyle.Centered, new Color(1f, 0.55f, 0.45f, a));
            }

            if (!string.IsNullOrEmpty(AbilityUser.Hint)) KeyHint(AbilityUser.Hint, cy + UiStyle.S(58));
        }

        /// <summary>"F|grimper" : la touche en or, le verbe en clair, centres.</summary>
        static void KeyHint(string hint, float y)
        {
            string[] kv = hint.Split('|');
            string text = kv.Length > 1 ? kv[0] + "   " + kv[1] : kv[0];
            Text(new Rect(0f, y, Screen.width, UiStyle.S(22)), text, UiStyle.Centered, new Color(0.95f, 0.88f, 0.7f, 0.9f));
        }

        /// <summary>"E  Prendre le don : Grappin", et le trait qui se remplit pendant le maintien.</summary>
        void DrawPrompt()
        {
            if (interactor == null) return;
            IInteractable target = interactor.Current;
            if (target == null || !target.CanInteract) return;
            float y = Screen.height * 0.5f + UiStyle.S(86);
            Text(new Rect(0f, y, Screen.width, UiStyle.S(24)), "E    " + target.Prompt, UiStyle.Centered, new Color(1f, 0.9f, 0.68f));
            if (target.HoldDuration > 0f && interactor.HoldProgress01 > 0f)
            {
                float w = UiStyle.S(160);
                Rect bar = new Rect((Screen.width - w) * 0.5f, y + UiStyle.S(26), w, UiStyle.S(2));
                UiStyle.Fill(bar, new Color(1f, 1f, 1f, 0.15f));
                UiStyle.Fill(new Rect(bar.x, bar.y, bar.width * interactor.HoldProgress01, bar.height), Palette.Gold);
            }
        }

        // ================================================================== dans le monde

        /// <summary>Le nom des autres joueurs, a leur couleur, quand ils sont a moins de 22 m.</summary>
        void DrawNames()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null || Game.PlayerTransform == null) return;
            Vector3 me = Game.PlayerTransform.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.IsPlayer || s.Body == null || s.Hidden) continue;
                float d = (me - s.Body.position).magnitude;
                if (d > 22f) continue;
                Vector3 sp = cam.WorldToScreenPoint(s.Body.position + Vector3.up * 2.75f);
                if (sp.z <= 0f) continue;
                float a = Mathf.Clamp01((22f - d) / 6f);
                string name = s.CarriesCrown ? s.Name + "  ·  COURONNE" : s.Stunned ? s.Name + "  ·  étourdi" : s.Name;
                Rect r = new Rect(sp.x - UiStyle.S(120), Screen.height - sp.y - UiStyle.S(10), UiStyle.S(240), UiStyle.S(20));
                Text(r, name, UiStyle.CenteredSmall, new Color(s.Colour.r, s.Colour.g, s.Colour.b, a));
            }
        }

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
            Text(new Rect(gx - UiStyle.S(90), gy - UiStyle.S(10), UiStyle.S(180), UiStyle.S(20)), "COURONNE  " + dist + " m", UiStyle.CenteredSmall,
                 new Color(1f, 0.82f, 0.4f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f)));
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
                Text(new Rect(0f, Screen.height * 0.5f - UiStyle.S(60), Screen.width, UiStyle.S(24)), "UN ŒIL TE VISE — BOUGE", UiStyle.Centered, new Color(1f, 0.4f, 0.3f, 0.6f + 0.4f * beat));
            }
            else if (Rival.HuntingPlayer && me.CarriesCrown)
            {
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)), 6f);
                Edges(UiStyle.S(40), new Color(0.7f, 0.05f, 0.03f, 0.12f + 0.16f * beat));
            }

            if (me.Stunned) UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.9f, 0.85f, 0.7f, 0.12f));
            if (me.Slowed) Edges(UiStyle.S(60), new Color(0.5f, 0.8f, 1f, 0.22f));
            if (me.CarriesCrown)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                Edges(UiStyle.S(30), new Color(1f, 0.78f, 0.3f, 0.06f + 0.06f * pulse));
            }
        }

        static void Edges(float e, Color c)
        {
            UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, e), c);
            UiStyle.FadeBand(new Rect(0f, Screen.height - e, Screen.width, e), c);
        }

        // ================================================================== l'astuce

        void DrawTip()
        {
            if (tipTimer <= 0f || string.IsNullOrEmpty(tipText)) return;
            float a = Mathf.Clamp01((TipDuration - tipTimer) / 0.3f) * Mathf.Clamp01(tipTimer / 0.6f);
            Rect r = new Rect(Screen.width * 0.2f, Screen.height * 0.68f, Screen.width * 0.6f, UiStyle.S(50));
            Text(r, tipText, WrappedCentered(), new Color(0.96f, 0.92f, 0.8f, a));
        }

        // ================================================================== le grand titre

        void DrawCard()
        {
            if (cardTimer <= 0f || string.IsNullOrEmpty(cardTitle)) return;
            float age = CardDuration - cardTimer;
            float alpha = Mathf.Clamp01(age / 0.25f) * Mathf.Clamp01(cardTimer / 0.6f);
            float y = Screen.height * 0.26f - (1f - Mathf.Clamp01(age / 0.25f)) * UiStyle.S(10);

            if (!string.IsNullOrEmpty(cardKicker))
                Text(new Rect(0f, y - UiStyle.S(20), Screen.width, UiStyle.S(18)), UiStyle.Spaced(cardKicker.ToUpperInvariant()), UiStyle.CenteredSmall, new Color(0.8f, 0.75f, 0.65f, alpha));
            GUIStyle title = BigCentered();
            Text(new Rect(0f, y, Screen.width, UiStyle.S(44)), cardTitle, title, new Color(cardTint.r, cardTint.g, cardTint.b, alpha));
            if (!string.IsNullOrEmpty(cardLine))
                Text(new Rect(0f, y + UiStyle.S(46), Screen.width, UiStyle.S(22)), cardLine, UiStyle.Centered, new Color(0.95f, 0.9f, 0.8f, alpha));
        }

        // ================================================================== Tab : le match

        /// <summary>Tab maintenu : chacun, ses manches gagnees, ses capacites -- en toutes lettres.</summary>
        void DrawScores()
        {
            float w = Mathf.Min(UiStyle.S(720), Screen.width - UiStyle.S(40)), row = UiStyle.S(50);
            float h = UiStyle.S(64) + row * Match.Slots.Count;
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.24f, w, h);
            UiStyle.Fill(box, UiStyle.Scrim);
            float x = box.x + UiStyle.S(24), y = box.y + UiStyle.S(16);
            Text(new Rect(x, y, w, UiStyle.S(24)), UiStyle.Spaced("LE MATCH") + "     " + (Match.IsTieBreak ? "départage" : "manche " + Match.RoundNumber + " sur " + Match.Rounds),
                 UiStyle.Head, Palette.Gold);
            y += UiStyle.S(40);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                Color c = s.IsLocal ? Palette.Gold : s.Colour;
                Text(new Rect(x, y, UiStyle.S(150), UiStyle.S(24)), s.Name, UiStyle.Label, c);
                Text(new Rect(x + UiStyle.S(150), y, UiStyle.S(140), UiStyle.S(24)), s.Wins + (s.Wins > 1 ? " manches" : " manche"), UiStyle.Label, UiStyle.Ink);
                string abilities = "";
                for (int k = 0; k < s.Abilities.Count; k++) abilities += (k > 0 ? ", " : "") + AbilityInfo.Name(s.Abilities[k]);
                Text(new Rect(x + UiStyle.S(290), y + UiStyle.S(2), w - UiStyle.S(320), UiStyle.S(40)), abilities.Length > 0 ? abilities : "aucune capacité", Wrapped(), UiStyle.InkDim);
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
            y = Line(x, y, inner, "Yeux / bots / sanctuaires", Eye.All.Count + " / " + Rival.All.Count + " / " + Shrine.All.Count);
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
