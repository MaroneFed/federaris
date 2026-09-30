using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES MOMENTS A CLIPPER (02/10 -- Martin : "mets-toi dans la peau du clippeur
    /// Instagram, TikTok ; rajoute plein de trucs a clipper, des moments ou tu sais tres
    /// bien qu'a ce moment-la, il y aura un clip").
    ///
    /// Ce fichier REPERE les moments qu'on aura envie de montrer -- et les fait CLAQUER pour
    /// que le clip soit bon tout seul : un gros son, un eclair, la camera qui encaisse,
    /// l'icone du moment au milieu de l'ecran (et quelques mots si l'aide ecrite est la),
    /// et une ligne dans le fil, marquee de la claquette de cinema, pour tout le monde.
    ///
    ///   KO               tu pousses quelqu'un et il tombe dans les nuages (8 s au plus apres) :
    ///                    une COLONNE DE LUMIERE jaillit des nuages a sa couleur, un boum de
    ///                    canon -- le KO de Smash ;
    ///   DOUBLE KO        deux KO en moins de 12 s ;
    ///   VOL EN PLEIN CIEL la Couronne arrachee par un pique d'aigle ;
    ///   SACRE ARRACHE    le porteur perd la Couronne alors que son sacre etait aux deux tiers ;
    ///   AU BUZZER        un sacre dans les 15 dernieres secondes de la manche ;
    ///   REMONTADA        une manche gagnee par quelqu'un qui n'en avait aucune, contre un
    ///                    meneur qui en avait deja deux ;
    ///   DOUBLE POUSSEE   deux joueurs differents pousses en moins de 2,5 s ;
    ///   REVANCHE         tu pousses celui qui venait de te pousser (10 s) ;
    ///   PATATE CHAUDE    la Couronne change quatre fois de mains en 15 s ;
    ///   ABATTU EN VOL    une gargouille touche le porteur en plein vol ;
    ///   ESQUIVE          le porteur s'echappe de la cible d'une gargouille verrouillee ;
    ///   VIRE DU SOMMET   une poussee au sommet de la tour (elle envoie 35 % plus loin).
    ///
    /// PAS D'AURA (30/09, Martin : "je deteste ca") : ni ralenti, ni "+1000", ni flammes --
    /// des moments qui claquent, pas un ecran qui se met en pause pour se feliciter. La seule
    /// pause est la micro-pause d'impact d'un dixieme de seconde qui existait deja.
    ///
    /// Classe C# pure (sauf les effets) : les gestes eux-memes sont ailleurs (Combat, Crown,
    /// Monument, Eye) et appellent ces fonctions apres coup -- en Phase 3, sur l'hote.
    /// </summary>
    public static class Highlights
    {
        public enum Kind { KO, DoubleKO, AirSteal, SacreStopped, Buzzer, Remontada, DoublePush, Revenge, HotPotato, Sniped, Dodge, Summit }

        static readonly Dictionary<Seeker, float> lastShoveAt = new Dictionary<Seeker, float>();
        static readonly Dictionary<Seeker, Seeker> lastShoved = new Dictionary<Seeker, Seeker>();
        static readonly Dictionary<Seeker, float> lastKoAt = new Dictionary<Seeker, float>();
        static readonly List<float> crownChanges = new List<float>();
        static float hotPotatoAt = -99f;

        /// <summary>Les moments de chacun (par place dans le match), pour la fin du match.</summary>
        static readonly Dictionary<int, int> count = new Dictionary<int, int>();
        public static int CountOf(int slot) { int n; return count.TryGetValue(slot, out n) ? n : 0; }

        /// <summary>Le joueur aux plus beaux moments du match (-1 : personne).</summary>
        public static int Best
        {
            get
            {
                int best = -1, most = 0;
                foreach (KeyValuePair<int, int> kv in count) if (kv.Value > most) { most = kv.Value; best = kv.Key; }
                return best;
            }
        }

        /// <summary>Un nouveau match : on remet les compteurs a zero.</summary>
        public static void Reset()
        {
            count.Clear();
            lastShoveAt.Clear();
            lastShoved.Clear();
            lastKoAt.Clear();
            crownChanges.Clear();
        }

        static bool Live { get { return Game.Season != null && Game.Season.Running; } }

        // ================================================================== les detecteurs

        /// <summary>Une poussee a porte (Combat.Shove) : doublee ? revanche ?</summary>
        public static void Shoved(Seeker by, Seeker victim, bool stole)
        {
            if (!Live || by == null || victim == null) return;
            float now = Time.time;
            Seeker before;
            float at;
            if (!stole && Tower.Summit(victim.Body.position))
                Show(Kind.Summit, by, victim, victim.Body.position);
            else if (lastShoved.TryGetValue(by, out before) && before != victim && lastShoveAt.TryGetValue(by, out at) && now - at < 2.5f)
                Show(Kind.DoublePush, by, victim, victim.Body.position);
            else if (by.LastHitBy == victim && now - by.LastHurt < 10f)
                Show(Kind.Revenge, by, victim, victim.Body.position);
            lastShoved[by] = victim;
            lastShoveAt[by] = now;
        }

        /// <summary>Quelqu'un tombe dans les nuages (Respawn.Of, AVANT qu'on oublie qui l'a frappe).</summary>
        public static void Fell(Seeker victim)
        {
            if (!Live || victim == null || victim.Body == null) return;
            Seeker by = victim.LastHitBy;
            if (by == null || by == victim || Time.time - victim.LastHurt > 8f) return;
            Vector3 p = victim.Body.position;
            Vector3 exit = new Vector3(p.x, -22f, p.z);
            // LA COLONNE DU KO : elle jaillit des nuages a la couleur de la victime, on la voit
            // de toute l'ile -- c'est l'image du clip.
            Fx.Column(exit, victim.Colour, 150f, 1.4f, 3.2f);
            Fx.Column(exit, Color.white, 150f, 0.5f, 1.4f);
            Fx.Shock(exit + Vector3.up * 4f, victim.Colour, 14f, 0.6f);
            Fx.Ring(exit + Vector3.up * 2f, Color.white, 2f, 26f, 0.7f, 0.6f, Vector3.up);
            Fx.Burst(exit + Vector3.up * 3f, victim.Colour, 160, 30f, 0.5f, 1.2f, -0.4f, Vector3.up, 55f);
            Fx.Flash(exit + Vector3.up * 6f, victim.Colour, 60f, 8f, 0.6f);
            Sfx.KoBoom(exit, by.IsPlayer || victim.IsPlayer);
            float last;
            bool twice = lastKoAt.TryGetValue(by, out last) && Time.time - last < 12f;
            lastKoAt[by] = Time.time;
            Show(twice ? Kind.DoubleKO : Kind.KO, by, victim, exit);
        }

        /// <summary>La Couronne arrachee par un pique d'aigle (Combat.DiveStrike).</summary>
        public static void AirSteal(Seeker by, Seeker victim)
        {
            if (!Live || by == null || victim == null || victim.Body == null) return;
            Vector3 p = victim.Body.position + Vector3.up;
            Fx.Shock(p, Wings.Gold, 9f, 0.45f);
            Fx.Ring(p, Color.white, 1f, 16f, 0.5f, 0.4f, Vector3.up);
            Show(Kind.AirSteal, by, victim, p);
        }

        /// <summary>La Couronne change de mains (prise, volee) : quatre fois en 15 s, c'est la patate chaude.</summary>
        public static void CrownChanged(Seeker to)
        {
            if (!Live || to == null) return;
            // (03/10, le clipper) La Couronne change de mains : un eclair d'or sur la tete du
            // nouveau porteur, pour tous ceux qui le voient -- dans un clip, on voit OU elle est passee.
            if (to.Body != null) Fx.Flash(to.Body.position + Vector3.up * 2f, new Color(1f, 0.82f, 0.4f), 9f, 3.2f, 0.25f);
            float now = Time.time;
            crownChanges.Add(now);
            crownChanges.RemoveAll(t => now - t > 15f);
            if (crownChanges.Count >= 4 && now - hotPotatoAt > 20f)
            {
                hotPotatoAt = now;
                Show(Kind.HotPotato, to, null, to.Body != null ? to.Body.position : Vector3.zero);
            }
        }

        /// <summary>Un sacre bien avance s'arrete : la Couronne a quitte son porteur (Monument).</summary>
        public static void SacreStopped(Seeker victim, float progress, Vector3 at)
        {
            if (!Live || victim == null || progress < 0.66f) return;
            Seeker by = Crown.Holder != null && Crown.Holder != victim ? Crown.Holder : victim.LastHitBy;
            if (by == null || by == victim) return;
            Fx.Shock(at + Vector3.up * 1.5f, Monument.Blue, 8f, 0.4f);
            Show(Kind.SacreStopped, by, victim, at);
        }

        /// <summary>La manche est gagnee (Monument.TryDeliver) : au buzzer ? une remontada ?</summary>
        public static void Crowned(Seeker s)
        {
            if (s == null || Game.Season == null) return;
            if (Game.Season.Remaining < 15f) { Show(Kind.Buzzer, s, null, s.Body != null ? s.Body.position : Vector3.zero); return; }
            int lead = 0;
            for (int i = 0; i < Match.Slots.Count; i++) if (Match.Slots[i] != s.Slot) lead = Mathf.Max(lead, Match.Slots[i].Wins);
            if (s.Slot.Wins == 0 && lead >= 2) Show(Kind.Remontada, s, null, s.Body != null ? s.Body.position : Vector3.zero);
        }

        /// <summary>Une gargouille a touche le porteur en plein vol (Eye.Fire).</summary>
        public static void Sniped(Seeker victim)
        {
            if (!Live || victim == null || victim.Body == null) return;
            Show(Kind.Sniped, null, victim, victim.Body.position);
        }

        /// <summary>Le porteur a esquive une gargouille verrouillee (Eye.Fire).</summary>
        public static void Dodged(Seeker s)
        {
            if (!Live || s == null || s.Body == null) return;
            Show(Kind.Dodge, s, null, s.Body.position);
        }

        // ================================================================== le moment

        static string IconOf(Kind k)
        {
            switch (k)
            {
                case Kind.KO: case Kind.DoubleKO: return "ko";
                case Kind.AirSteal: return "pique";
                case Kind.SacreStopped: return "sacre";
                case Kind.Buzzer: return "chrono";
                case Kind.Remontada: return "haut";
                case Kind.DoublePush: case Kind.Revenge: return "pousser";
                case Kind.HotPotato: return "couronne";
                case Kind.Sniped: return "chute";
                case Kind.Summit: return "tour";
                default: return "ailes";
            }
        }

        static string WordsOf(Kind k)
        {
            switch (k)
            {
                case Kind.KO: return "KO !";
                case Kind.DoubleKO: return "DOUBLE KO !";
                case Kind.AirSteal: return "VOLÉE EN PLEIN CIEL !";
                case Kind.SacreStopped: return "SACRE ARRACHÉ !";
                case Kind.Buzzer: return "AU BUZZER !";
                case Kind.Remontada: return "REMONTADA !";
                case Kind.DoublePush: return "DOUBLÉ !";
                case Kind.Revenge: return "REVANCHE !";
                case Kind.HotPotato: return "PATATE CHAUDE !";
                case Kind.Sniped: return "LE PORTEUR ABATTU !";
                case Kind.Summit: return "VIRÉ DU SOMMET !";
                default: return "ESQUIVE !";
            }
        }

        static Color TintOf(Kind k)
        {
            switch (k)
            {
                case Kind.KO: case Kind.DoubleKO: return new Color(1f, 0.45f, 0.3f);
                case Kind.SacreStopped: return Monument.Blue;
                case Kind.Sniped: return new Color(1f, 0.55f, 0.2f);
                case Kind.Dodge: return Wings.Glow;
                default: return Wings.Gold;
            }
        }

        /// <summary>
        /// LE MOMENT : le fil pour tout le monde (claquette + icone + pseudos) ; si c'est TOI
        /// qui l'as fait, l'icone en grand, le coup de cymbale, la camera qui encaisse ; si
        /// c'est toi qui l'as subi, l'icone quand meme (on veut savoir ce qui nous est arrive).
        /// </summary>
        static void Show(Kind k, Seeker actor, Seeker victim, Vector3 at)
        {
            string icon = IconOf(k);
            Color tint = TintOf(k);
            Seeker who = actor ?? victim;
            if (who != null)
            {
                int n;
                count[who.Index] = (count.TryGetValue(who.Index, out n) ? n : 0) + (actor != null ? 1 : 0);
            }
            Color gold = new Color(1f, 0.82f, 0.4f);
            if (actor != null)
                Toasts.Show(true, actor.IsPlayer ? gold : actor.Colour, actor.Name, new[] { "clip", icon }, new[] { Color.white, tint },
                            victim != null, victim != null ? (victim.IsPlayer ? gold : victim.Colour) : Color.white, victim != null ? victim.Name : null);
            else if (victim != null)
                Toasts.Show(true, victim.IsPlayer ? gold : victim.Colour, victim.Name, new[] { "clip", icon }, new[] { Color.white, tint }, false, Color.white, null);

            bool mine = actor != null && actor.IsPlayer;
            bool against = !mine && victim != null && victim.IsPlayer;
            if (!mine && !against) return;
            if (Game.Hud != null)
            {
                Game.Hud.ShowSplash(icon, tint, WordsOf(k));
                if (mine) Game.Hud.Flash(new Color(tint.r, tint.g, tint.b, 0.35f));
                if (Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Kick(mine ? 12f : 6f);
            }
            if (mine)
            {
                Sfx.Moment();
                if (k == Kind.KO || k == Kind.DoubleKO || k == Kind.AirSteal) Hud.HitStop(0.1f);
            }
        }
    }
}
