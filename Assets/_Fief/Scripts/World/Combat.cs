using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CONTACT (27/09 : plus d'epee, plus de vie). Tout ce qui touche un joueur le
    /// PROJETTE : la poussee (clic droit), l'onde de choc, le souffle, le rayon d'un
    /// Oeil, une mine, un pendule de la tour. On ne meurt pas -- on perd sa place, et
    /// la Couronne si on la portait. C'est Smash au-dessus des nuages.
    ///
    /// Tout passe par Hit() : un seul endroit decide de ce qu'un coup fait (Ancrage,
    /// Prise ferme, etourdissement, Couronne qui tombe). En Phase 3, sur l'hote seulement.
    /// </summary>
    public static class Combat
    {
        public const float ShoveReach = 3.2f;
        /// <summary>(01/10 -- "quand ca pousse, que ca pousse bien" : 20 -> 26, et l'elan ne se freine plus en l'air.)</summary>
        public const float ShoveForce = 26f;
        public const float ShoveLift = 10f;

        /// <summary>La portee de la poussee de "s" (06/10 : les BRAS LONGS portent a 5 m).</summary>
        public static float ReachOf(Seeker s)
        {
            if (s == null) return ShoveReach;
            if (s.Has(Ability.MainLourde)) return 5.5f;      // (08/10) divine
            return s.Has(Ability.BrasLongs) ? 5f : ShoveReach;
        }
        /// <summary>Vrai pendant le coup d'une poussee : Hit ne joue pas son petit son, la poussee a le sien.</summary>
        static bool quietHit;
        /// <summary>(04/10, en ligne) Vrai pendant une poussee ou un pique d'invite sur le porteur : l'hote decidera du vol.</summary>
        static bool stealAttempt;
        static bool riposting;
        static bool exploding;
        static bool mirroring;

        /// <summary>
        /// POUSSER (clic droit) : le premier joueur devant soi, a 3 m, part en arriere
        /// et en l'air. Le porteur de la Couronne la lache. Vrai si on a touche quelqu'un.
        /// </summary>
        public static bool Shove(Seeker by, Vector3 forward)
        {
            if (by == null || by.Body == null || !by.CanShove) return false;
            Vector3 f = Flat(forward).normalized;
            float force = by.Has(Ability.Poigne) ? ShoveForce * 2f : ShoveForce;
            if (by.Giant) force *= 1.6f;
            if (by.Has(Ability.MainLourde)) force *= 2.5f;
            // (06/10) RAGE : chaque coup recu depuis ta derniere poussee la rend plus forte (x2,5 au plus).
            if (by.Rage > 0) { force *= 1f + 0.25f * by.Rage; by.Rage = 0; }
            // (07/10) COUP DE PIED : cette poussee-ci envoie trois fois plus loin.
            bool kick = Time.time < by.SuperShoveUntil;
            float reach = ReachOf(by);
            Seeker best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || !InArc(by.Body.position, f, s.Body.position, reach, 65f)) continue;
                float d = Flat(s.Body.position - by.Body.position).magnitude;
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null) return false;
            if (kick) { force *= 3f; by.SuperShoveUntil = -1f; Fx.Shock(best.Body.position + Vector3.up, AbilityInfo.Tint(Ability.CoupDePied), 3f, 0.3f); }
            Vector3 push = Flat(best.Body.position - by.Body.position).normalized;
            if (push.sqrMagnitude < 0.01f) push = f;
            // POUSSER LE PORTEUR, C'EST LUI VOLER LA COURONNE (27/09 -- Martin : "il se la
            // reprend en une demi-seconde"). Elle passe directement dans tes mains.
            bool stole = best.CarriesCrown && !best.Graced && Crown.TrySteal(by, best);
            // Un court etourdissement : on ne contre-marche pas une poussee (c'est ce
            // qui la rendait molle -- on reculait de deux metres en appuyant sur Z).
            // PROJETE : il part en cloche d'une quinzaine de metres (Seeker.Launch). Le voleur,
            // lui, garde sa victime pres de lui : elle ne vole qu'a moitie.
            if (!best.Graced) best.Launch(stole ? 0.6f : 1.4f);
            // (02/10, le clipper) LE HOME RUN : pousse du SOMMET de la tour, on part 35 % plus
            // loin -- 100 m de vide sous soi, le plus beau vol du jeu (Highlights : "vire du sommet").
            if (!stole && Tower.Summit(best.Body.position)) force *= 1.35f;
            quietHit = true;
            stealAttempt = NetGame.IsClient && best.Remote && best.CarriesCrown;
            Hit(best, push * force * (stole ? 0.7f : 1f) + Vector3.up * ShoveLift, 0.3f, !stole, by);
            stealAttempt = false;
            quietHit = false;
            Fx.Impact(best.Body.position + Vector3.up * 1.1f, by.Colour, stole ? 1.4f : 1f);
            Fx.Shock(best.Body.position + Vector3.up * 1.1f, by.Colour, 2.6f, 0.25f);
            // (02/10 -- "quand ca pousse, je veux un ENORME bruit") : trois couches, pleine
            // puissance si c'est toi qui pousses ou toi qu'on pousse.
            if (!best.Graced) Sfx.BigPush(best.Body.position, by.IsPlayer || best.IsPlayer);
            if (by.IsPlayer) { Stats.Shoves++; Hud.HitStop(0.07f); }
            if (by.IsPlayer && !best.Graced && !stole) Shouts.IPushed(best);
            // (08/10) EXPLOSIF (divin) : celui qu'on pousse explose -- ses voisins s'envolent.
            if (by.Has(Ability.Explosif) && !best.Graced && !stole)
            {
                Vector3 c = best.Body.position;
                for (int i = 0; i < Game.Seekers.Count; i++)
                {
                    Seeker o = Game.Seekers[i];
                    if (o == by || o == best || o.Body == null) continue;
                    Vector3 d = o.Body.position - c;
                    if (Mathf.Abs(d.y) > 3f || Flat(d).magnitude > 7f) continue;
                    Vector3 away = Flat(d).sqrMagnitude > 0.01f ? Flat(d).normalized : Vector3.forward;
                    Hit(o, away * 22f + Vector3.up * 11f, 0.3f, true, by);
                }
                Color k = AbilityInfo.Tint(Ability.Explosif);
                DivineFx.Impact(c, 7f, k, 1f, true);
            }
            if (!best.Graced) Highlights.Shoved(by, best, stole);
            return true;
        }

        // ================================================================== le pique d'aigle

        /// <summary>
        /// LE PIQUE D'AIGLE (28/09 -- Martin : "une fois qu'on est dans l'air, qu'on puisse
        /// facilement choper la couronne"). EN L'AIR, la touche pour pousser, le porteur
        /// dans le viseur (a 45 m, dans un cone de 30 degres) : on FOND SUR LUI, guide, a
        /// 48 m/s. Au contact, c'est un vol (comme une poussee). Recharge 3 s.
        /// </summary>
        public const float DiveRange = 45f;
        public const float DiveAngle = 30f;
        public const float DiveSpeed = 48f;
        public const float DiveCooldown = 3f;
        public const float DiveSeconds = 1.2f;

        /// <summary>Le porteur sur qui "by" peut piquer maintenant (null sinon). A appeler seulement en l'air.</summary>
        public static Seeker DiveTarget(Seeker by, Vector3 eye, Vector3 forward)
        {
            if (by == null || by.Body == null || by.Stunned || Time.time < by.DiveReadyAt) return null;
            Seeker h = Crown.Holder;
            if (h == null || h == by || h.Body == null || h.Hidden) return null;
            Vector3 to = h.Body.position + Vector3.up * 1f - eye;
            if (to.magnitude > DiveRange || Vector3.Angle(forward, to) > DiveAngle) return null;
            RaycastHit hit;
            if (Physics.Raycast(eye, to.normalized, out hit, to.magnitude - 0.8f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(h.Body) && !hit.collider.transform.IsChildOf(by.Body)) return null;
            return h;
        }

        /// <summary>Lancer le pique (toi comme un bot). Vrai s'il part.</summary>
        public static bool Dive(Seeker by, Seeker target)
        {
            IMover m = AbilityCaster.MoverOf(by);
            if (m == null || target == null) return false;
            by.DiveReadyAt = Time.time + DiveCooldown;
            by.DiveTarget = target;
            by.DiveUntil = Time.time + 1.6f;
            m.Dive(target);
            Fx.Trail(by.Body, Wings.Gold, 1.4f, 1.3f);
            Fx.Ring(by.Body.position + Vector3.up * 1.2f, Wings.Gold, 0.5f, 5f, 0.35f, 0.3f, target.Body.position - by.Body.position);
            Fx.Burst(by.Body.position + Vector3.up * 1.2f, Wings.Gold, 50, 12f, 0.2f, 0.5f, 0f, by.Body.position - target.Body.position, 30f);
            Sfx.WhooshAt(by.Body.position);
            return true;
        }

        /// <summary>
        /// Une image de pique : la vitesse vers la cible. Vrai quand c'est fini (touche,
        /// rate, ou le temps ecoule) ; "time" decompte.
        /// </summary>
        public static bool DiveStep(Seeker by, Seeker target, Vector3 from, ref float time, float dt, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            time -= dt;
            if (by == null || target == null || target.Body == null || by.Stunned || time <= 0f) { time = 0f; if (by != null) by.DiveUntil = -1f; return true; }
            Vector3 to = target.Body.position + Vector3.up * 0.9f - (from + Vector3.up * 0.9f);
            if (to.magnitude < 2.4f)
            {
                DiveStrike(by, target);
                by.DiveUntil = -1f;
                time = 0f;
                return true;
            }
            velocity = to.normalized * DiveSpeed;
            return false;
        }

        /// <summary>LE CHOC du pique : s'il porte la Couronne, elle passe dans tes mains.</summary>
        static void DiveStrike(Seeker by, Seeker target)
        {
            Vector3 dir = Flat(target.Body.position - by.Body.position);
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : by.Body.forward;
            bool stole = target.CarriesCrown && !target.Graced && Crown.TrySteal(by, target);
            quietHit = true;
            stealAttempt = NetGame.IsClient && target.Remote && target.CarriesCrown;
            Hit(target, dir * 24f + Vector3.up * 6f, 0.3f, !stole, by);
            stealAttempt = false;
            quietHit = false;
            Fx.Impact(target.Body.position + Vector3.up * 1.1f, Wings.Gold, stole ? 1.8f : 1f);
            Fx.Shock(target.Body.position + Vector3.up * 1.1f, Wings.Gold, 3f, 0.3f);
            Sfx.BigPush(target.Body.position, by.IsPlayer || target.IsPlayer);
            if (by.IsPlayer) Hud.HitStop(0.08f);
            if (stole) Highlights.AirSteal(by, target);
        }

        /// <summary>
        /// UN COUP : "velocity" projette le joueur, "stun" l'etourdit (secondes), et s'il
        /// porte la Couronne et que "dropsCrown", il la lache -- sauf Prise ferme.
        /// </summary>
        /// <summary>
        /// (10/10) Le porteur, en Mode Dieu, face au pouvoir d'un autre joueur : vrai si le coup
        /// glisse sur lui. La poussee (quietHit) passe : c'est elle qui vole la Couronne.
        /// </summary>
        static bool GodShield(Seeker victim, Seeker by)
        {
            if (!Match.GodMode || !victim.CarriesCrown || by == null || by == victim || quietHit) return false;
            if (Time.time - victim.ShieldFxAt > 0.25f)
            {
                victim.ShieldFxAt = Time.time;
                Fx.Sparks(victim.Body.position + Vector3.up * 1.2f, new Color(1f, 0.82f, 0.36f), 16, 4f);
                Fx.Ring(victim.Body.position + Vector3.up * 1.2f, new Color(1f, 0.82f, 0.36f), 0.6f, 2.2f, 0.25f, 0.12f, Vector3.up);
            }
            return true;
        }

        public static void Hit(Seeker victim, Vector3 velocity, float stun, bool dropsCrown, Seeker by)
        {
            if (victim == null || victim.Body == null) return;
            // (10/10 -- Martin : "en God Mode, il faut pas taper la couronne, c'est trop cheate")
            // EN MODE DIEU, LES POUVOIRS NE TOUCHENT PAS LE PORTEUR : la Couronne se vole a la
            // main -- la poussee (clic droit) et le pique d'aigle -- et les pieges de la tour
            // restent des pieges. Le reste glisse sur lui dans une gerbe d'or.
            if (GodShield(victim, by)) return;
            // (04/10, en ligne) LE JOUEUR D'UNE AUTRE MACHINE : le coup part chez lui (par l'hote,
            // qui decide de la Couronne). Ici, on n'en montre que le choc.
            if (victim.Remote) { HitElsewhere(victim, velocity, stun, dropsCrown, by); return; }
            // Protege (au depart, apres un respawn, juste apres un vol) : rien ne le touche.
            if (victim.Graced)
            {
                Fx.Sparks(victim.Body.position + Vector3.up * 1.1f, new Color(1f, 1f, 1f, 0.8f), 12, 3f);
                return;
            }
            if (victim.Has(Ability.Ancrage)) velocity = new Vector3(velocity.x * 0.5f, velocity.y * 0.7f, velocity.z * 0.5f);
            // (05/10) Le GEANT ne bouge presque pas.
            if (victim.Giant) velocity *= 0.3f;
            // (07/10) L'ARMURE : le premier coup d'un joueur dans la manche ne fait rien.
            if (by != null && by != victim && victim.Has(Ability.Armure) && !victim.ArmorUsed)
            {
                victim.ArmorUsed = true;
                Fx.Shock(victim.Body.position + Vector3.up * 1.1f, AbilityInfo.Tint(Ability.Armure), 2.2f, 0.3f);
                Fx.Sparks(victim.Body.position + Vector3.up * 1.1f, Color.white, 20, 5f);
                Sfx.ClangAt(victim.Body.position);
                return;
            }
            // (06/10) Le MINI part deux fois plus loin.
            if (victim.Tiny) velocity = new Vector3(velocity.x * 2f, velocity.y * 1.3f, velocity.z * 2f);
            // La PRISON : le premier coup brise la cage (sinon dix secondes, c'est horrible).
            if (victim.Rooted) victim.RootedUntil = -1f;
            // La RAGE monte a chaque coup recu d'un joueur.
            if (by != null && by != victim && victim.Has(Ability.Rage)) victim.Rage = Mathf.Min(6, victim.Rage + 1);
            // TETE DURE : les pieges ne t'ejectent plus de la tour, ils te bousculent.
            if (by == null && victim.Has(Ability.TeteDure)) velocity *= 0.6f;
            // UN OBSTACLE SUR LA TOUR TE RENVOIE EN BAS (29/09) : jete hors de la rampe,
            // ailes fermees jusqu'au sol. (Les coups des joueurs, eux, ne font que projeter.)
            else if (by == null && Tower.On(victim.Body.position) && !Tower.Summit(victim.Body.position))
                velocity = Tumble(victim, velocity);
            // VAMPIRE : chaque coup donne fait courir plus vite.
            if (by != null && by != victim && by.Has(Ability.Vampire)) by.RushUntil = Time.time + 3f;
            // RIPOSTE : qui te frappe se prend un retour de baton (pas en cascade).
            if (by != null && by != victim && by.Body != null && victim.Has(Ability.Riposte) && !riposting)
            {
                Vector3 back = Flat(by.Body.position - victim.Body.position);
                back = back.sqrMagnitude > 0.01f ? back.normalized : -Flat(velocity).normalized;
                riposting = true;
                Hit(by, back * 16f + Vector3.up * 6f, 0.25f, true, victim);
                riposting = false;
                Fx.Ring(victim.Body.position + Vector3.up * 1.1f, AbilityInfo.Tint(Ability.Riposte), 0.4f, 2.6f, 0.3f, 0.2f, back);
            }
            // KAMIKAZE : on le pousse, il explose (tout le monde autour, sauf lui ; pas en cascade).
            if (by != null && by != victim && victim.Has(Ability.Kamikaze) && !exploding)
            {
                exploding = true;
                Vector3 c = victim.Body.position;
                Blast(c, 7f, 26f, 12f, victim);
                Color k = AbilityInfo.Tint(Ability.Kamikaze);
                Fx.Shock(c + Vector3.up, k, 7f, 0.45f);
                Fx.Burst(c + Vector3.up, k, 120, 18f, 0.25f, 0.8f, 0f, Vector3.zero, 0f);
                Fx.Flash(c + Vector3.up, k, 20f, 8f, 0.4f);
                Sfx.KoBoom(c, victim.IsPlayer || by.IsPlayer);
                exploding = false;
            }
            Knockback(victim, velocity);
            victim.LastHurt = Time.time;
            if (by != null && by != victim) { victim.LastHitBy = by; victim.LastHitByAt = Time.time; }
            // (05/10) "GOTAGA T'A DEGAGE !" -- en toutes lettres, en haut (Shouts).
            if (victim.IsPlayer && by != null && by != victim && velocity.magnitude > 18f) Shouts.PushedMe(by);
            // Un gros coup sur TOI : la tete se tourne vers d'ou il vient (OrbitCamera.Glance).
            if (victim.IsPlayer && velocity.magnitude > 18f && Game.Hud != null && Game.Hud.orbitCamera != null)
            {
                Vector3 from = by != null && by.Body != null ? by.Body.position : victim.Body.position - new Vector3(velocity.x, 0f, velocity.z).normalized * 6f;
                Game.Hud.orbitCamera.Glance(from + Vector3.up, 0.5f);
            }
            // Tes coups PORTENT : une micro-pause, un petit tremblement (28/09 : "que les capacites soient vraiment impactantes").
            if (by != null && by.IsPlayer)
            {
                Hud.HitStop(0.06f);
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.12f);
            }
            if (stun > 0f) victim.StunnedUntil = Mathf.Max(victim.StunnedUntil, Time.time + stun);

            if (dropsCrown && victim.CarriesCrown)
            {
                if (victim.Has(Ability.PriseFerme) && !victim.GripUsed)
                {
                    victim.GripUsed = true;
                    Ambiance.Burst(null, victim.Body.position + Vector3.up * 2f, AbilityInfo.Tint(Ability.PriseFerme));
                }
                else
                {
                    Crown.KnockOff(victim, velocity);
                    if (by != null && by.IsPlayer) Stats.CrownsStolen++;
                    Feed.CrownKnocked(victim, by);
                }
            }

            // Un joueur qui frappe : un coup de poing ; un obstacle : un choc (le sien sonne deja).
            if (quietHit) { }                   // la poussee joue son propre gros bruit (BigPush)
            else if (by != null) Sfx.PunchAt(victim.Body.position);
            else Sfx.ThudAt(victim.Body.position);
            if (victim.IsPlayer)
            {
                if (Game.Hud != null) Game.Hud.Hurt(velocity);
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(Mathf.Clamp(velocity.magnitude / 40f, 0.15f, 0.5f));
            }
            else
            {
                Ambiance.Burst(null, victim.Body.position + Vector3.up * 1.2f, victim.Colour);
                Rival r = Rival.Of(victim);
                if (r != null)
                {
                    if (by != null && by.Body != null) Punch.Apply(r.Figure, by.Body.position);
                    r.OnHit(by);
                }
            }
        }

        /// <summary>
        /// Un coup sur une marionnette (un joueur en ligne, ou un bot de l'hote chez un invite).
        /// Seuls les coups des joueurs joues ICI partent : un obstacle, une gargouille d'ici ne
        /// touchent pas un ami -- il a les siens, chez lui, au bon endroit.
        /// </summary>
        /// <summary>Ce que les capacites de fou font a leur cible (06/10).</summary>
        public enum Affliction : byte { Prison = 1, Glue = 2, Inverted = 3, Tiny = 4, Ink = 5, Balloon = 6, Charmed = 7, Frozen = 8 }

        /// <summary>
        /// LES SORTS DES CAPACITES DE FOU (06/10 -- Martin : "une prison qui t'enchaine au sol
        /// pendant dix secondes, plein de conneries comme ca") : la prison, la glu, la tete a
        /// l'envers, le mini, l'encre, le ballon. Une seule porte, comme Combat.Hit : un protege
        /// n'est pas touche ; le joueur d'une autre machine l'est CHEZ LUI (NetGame.RemoteAfflict).
        /// </summary>
        public static bool Afflict(Seeker victim, Affliction what, float seconds, Seeker by)
        {
            if (victim == null || victim.Body == null || victim.Graced) return false;
            if (GodShield(victim, by)) return false;
            // (07/10) MIROIR : le sort revient a l'envoyeur (une seule fois, pas de ping-pong).
            if (victim.Has(Ability.Miroir) && by != null && by != victim && by.Body != null && !mirroring)
            {
                mirroring = true;
                Fx.Shock(victim.Body.position + Vector3.up * 1.2f, AbilityInfo.Tint(Ability.Miroir), 2f, 0.3f);
                bool back = Afflict(by, what, seconds, victim);
                mirroring = false;
                return back;
            }
            // INCREVABLE : les sorts durent deux fois moins longtemps.
            if (victim.Has(Ability.Increvable)) seconds *= 0.5f;
            seconds = Mathf.Clamp(seconds, 0f, 12f);
            float until = Time.time + seconds;
            switch (what)
            {
                case Affliction.Prison: victim.RootedUntil = Mathf.Max(victim.RootedUntil, until); break;
                // (08/10) LE TEMPS S'ARRETE : comme la prison (un coup libere), dans un bloc de glace.
                case Affliction.Frozen: victim.RootedUntil = Mathf.Max(victim.RootedUntil, until); break;
                case Affliction.Glue: victim.GluedUntil = Mathf.Max(victim.GluedUntil, until); break;
                case Affliction.Inverted: victim.InvertedUntil = Mathf.Max(victim.InvertedUntil, until); break;
                case Affliction.Tiny: victim.TinyUntil = Mathf.Max(victim.TinyUntil, until); break;
                case Affliction.Ink: victim.InkUntil = Mathf.Max(victim.InkUntil, until); break;
                case Affliction.Balloon: victim.BalloonUntil = Mathf.Max(victim.BalloonUntil, until); break;
                case Affliction.Charmed:
                    if (by == null) return false;
                    victim.CharmedUntil = Mathf.Max(victim.CharmedUntil, until);
                    victim.CharmedBy = by;
                    break;
            }
            Mayhem.Show(victim, what, by);
            if (victim.Remote && by != null && !by.Remote) NetGame.RemoteAfflict(victim, what, seconds, by);
            if (victim.IsPlayer && by != null && what != Affliction.Glue) Shouts.Cursed(by, what);
            return true;
        }

        static void HitElsewhere(Seeker victim, Vector3 velocity, float stun, bool dropsCrown, Seeker by)
        {
            if (by == null || by.Remote || victim.Graced) return;
            // (06/10) La cage de sa marionnette se brise ici aussi (chez lui, le coup la brise).
            victim.RootedUntil = -1f;
            NetGame.RemoteHit(victim, velocity, stun, dropsCrown, by, stealAttempt);
            if (!quietHit) Sfx.PunchAt(victim.Body.position);
            Ambiance.Burst(null, victim.Body.position + Vector3.up * 1.2f, victim.Colour);
            Rival r = Rival.Of(victim);
            if (r != null && by.Body != null) Punch.Apply(r.Figure, by.Body.position);
            if (by.IsPlayer)
            {
                Hud.HitStop(0.06f);
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.12f);
            }
        }

        /// <summary>
        /// L'EJECTION (30/09 -- "quand ils touchent, qu'ils te fassent VRAIMENT partir de la
        /// tour") : la poussee d'un obstacle devient un vol plein vers l'exterieur -- 22 m/s
        /// dehors, 11 vers le haut, un peu de cote -- et pendant la chute, l'elan ne
        /// retombe presque pas (Seeker.Tumbling) : on part en cloche d'une vingtaine de
        /// metres, en tournoyant, et on s'ecrase dans la cour. Jamais au-dela de la muraille.
        /// Les ailes restent fermees jusqu'au sol.
        /// </summary>
        static Vector3 Tumble(Seeker victim, Vector3 velocity)
        {
            Vector3 p = victim.Body.position;
            Vector3 outward = new Vector3(p.x, 0f, p.z);
            outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 side = Vector3.ClampMagnitude(flat - outward * Vector3.Dot(flat, outward), 8f);
            velocity = outward * 22f + side + Vector3.up * (velocity.y < 0f ? 2f : 11f);
            victim.Tumble(7f);
            Sfx.WhooshAt(victim.Body.position);
            return velocity;
        }

        /// <summary>
        /// Le frottement de l'elan (par seconde) : fort d'habitude (on s'arrete en un
        /// quart de seconde), presque nul pendant une ejection (on vole loin).
        /// </summary>
        public static float KnockDrag(Seeker s) { return s != null && (s.Tumbling || s.Launched) ? 0.95f : 4.5f; }

        /// <summary>Projeter un joueur (sans autre effet).</summary>
        public static void Knockback(Seeker s, Vector3 velocity)
        {
            if (s == null || s.Body == null) return;
            if (s.IsPlayer && Game.Player != null) Game.Player.Push(velocity);
            else
            {
                Rival r = Rival.Of(s);
                if (r != null) r.Push(velocity);
            }
        }

        /// <summary>L'ONDE DE CHOC : tout le monde dans le rayon (sauf "by") part loin du centre.</summary>
        public static int Blast(Vector3 centre, float radius, float force, float up, Seeker by)
        {
            int n = 0;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                Vector3 d = s.Body.position - centre;
                if (Mathf.Abs(d.y) > 3f || Flat(d).magnitude > radius) continue;
                Vector3 away = Flat(d).sqrMagnitude > 0.01f ? Flat(d).normalized : Vector3.forward;
                float k = 1f - 0.4f * Flat(d).magnitude / radius;
                Hit(s, away * force * k + Vector3.up * up, 0.2f, true, by);
                n++;
            }
            return n;
        }

        /// <summary>LE SOUFFLE : tout ce qui est dans le cone devant, jusqu'a "range", est repousse.</summary>
        public static int Cone(Seeker by, Vector3 origin, Vector3 forward, float range, float angle, float force)
        {
            int n = 0;
            Vector3 f = Flat(forward).normalized;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || !InArc(origin, f, s.Body.position, range, angle)) continue;
                Vector3 away = Flat(s.Body.position - origin).normalized;
                Hit(s, (away + f).normalized * force + Vector3.up * 5f, 0.2f, true, by);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Le joueur VISE par "by" (crochet, echange) : le plus proche de l'axe du
        /// regard, dans un cone de "angle" degres, jusqu'a "range" metres, sans mur entre.
        /// </summary>
        public static Seeker Aimed(Seeker by, Vector3 eye, Vector3 dir, float range, float angle)
        {
            Seeker best = null;
            float bestAngle = angle;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                // Le Flair voit les voiles et vise a travers la fumee.
                bool flair = by != null && by.Has(Ability.Flair);
                if (s == by || s.Body == null || s.Hidden && !flair) continue;
                Vector3 to = s.Body.position + Vector3.up * 1.1f - eye;
                if (to.magnitude > range) continue;
                // On ne vise pas a travers la fumee de la Nuee (les gargouilles non plus).
                if (!flair && Smoke.Blocks(eye, s.Body.position + Vector3.up * 1.1f)) continue;
                float a = Vector3.Angle(dir, to);
                if (a > bestAngle) continue;
                RaycastHit hit;
                if (Physics.Raycast(eye, to.normalized, out hit, to.magnitude - 0.6f, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.collider.transform.IsChildOf(s.Body) && (by.Body == null || !hit.collider.transform.IsChildOf(by.Body))) continue;
                bestAngle = a;
                best = s;
            }
            return best;
        }

        /// <summary>Y a-t-il quelqu'un a portee de poussee, devant soi ?</summary>
        public static bool FoeAhead(Seeker me, Vector3 forward)
        {
            if (me == null || me.Body == null) return false;
            Vector3 f = Flat(forward).normalized;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s != me && s.Body != null && InArc(me.Body.position, f, s.Body.position, ReachOf(me), 65f)) return true;
            }
            return false;
        }

        public static bool InArc(Vector3 from, Vector3 forward, Vector3 target, float reach, float angle)
        {
            Vector3 to = target - from;
            if (Mathf.Abs(to.y) > 2.2f) return false;
            to.y = 0f;
            return to.magnitude <= reach && (to.magnitude < 0.5f || Vector3.Angle(forward, to) <= angle);
        }

        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }

    /// <summary>
    /// LES PLATEFORMES DE DEPART (28/09 -- Martin : "on doit chacun commencer depuis sa
    /// plateforme, depuis les petits ilots, pour apres aller dans le chateau ; les
    /// arbaletes pour remonter direct tout en haut c'est hyper cheate ; chacun a une
    /// arbalete dans son truc, on fonce dans le chateau, on monte").
    ///
    /// Chaque joueur a SA plateforme : un petit rocher volant, a 42 m de haut, a
    /// 116 m du centre, en face d'une des quatre portes (deux plateformes par porte,
    /// de part et d'autre de son axe) : TOUT LE MONDE est a la meme distance de sa porte
    /// et de la tour. Un disque a sa couleur, un fanion, une colonne de lumiere, et SON
    /// ARBALESTE : un clic, et elle te pose en cloche sur le PARVIS devant ta porte
    /// (29/09 : "sur la terre ferme devant le chateau, pas deja sur les trucs"). Puis
    /// le couloir, la porte, la rampe en face. Le SCEAU empeche d'entrer en volant.
    ///
    /// C'est aussi la qu'on REAPPARAIT quand on tombe dans les nuages (voir Respawn).
    /// </summary>
    public static class Spawns
    {
        static readonly Dictionary<int, Vector3> Points = new Dictionary<int, Vector3>();
        static readonly Dictionary<int, Ballista> Ballistas = new Dictionary<int, Ballista>();
        static readonly Dictionary<int, Vector3> Landings = new Dictionary<int, Vector3>();
        /// <summary>La distance de chaque plateforme au centre, sa hauteur, son rayon.</summary>
        public const float Distance = 116f;
        public const float Altitude = 42f;
        public const float PadRadius = 7f;

        public static void Place(int players, int seed)
        {
            Points.Clear();
            Ballistas.Clear();
            Landings.Clear();
            System.Random rng = new System.Random(seed ^ 0x51a);
            // (29/09) DEUX PLATEFORMES PAR PORTE, placees pareil de chaque cote de son axe :
            // tout le monde est a la meme distance de sa porte, et de la tour. Qui a quelle
            // porte change a chaque manche.
            int shift = rng.Next(4);
            for (int i = 0; i < players; i++)
            {
                int gate = (i + shift) % 4;
                int pair = i / 4;                                    // 0 : premier de sa porte, 1 : second
                float side = players <= 4 ? 0f : (pair == 0 ? -1f : 1f);
                Vector3 axis = Course.Axis(gate);
                float a = Mathf.Atan2(axis.z, axis.x) + side * 17f * Mathf.Deg2Rad;
                Points[i] = new Vector3(Mathf.Cos(a) * Distance, Altitude, Mathf.Sin(a) * Distance);
                Landings[i] = Course.Plaza(gate, side * 3.5f);
            }
        }

        /// <summary>Le centre de la plateforme de "slot".</summary>
        public static Vector3 PadOf(int slot)
        {
            Vector3 p;
            return Points.TryGetValue(slot, out p) ? p : new Vector3(0f, Altitude, -Distance);
        }

        /// <summary>
        /// Ou l'on apparait : sur sa plateforme, un peu en arriere et A COTE de l'arbaleste
        /// (29/09 : juste derriere, sa crosse bouchait toute la vue).
        /// </summary>
        public static Vector3 Of(int slot, Vector3 fallback)
        {
            Vector3 p;
            if (!Points.TryGetValue(slot, out p)) return fallback;
            Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
            Vector3 side = new Vector3(-outward.z, 0f, outward.x);
            return p + outward * 1.6f + side * 2.6f + Vector3.up * 0.05f;
        }

        /// <summary>L'orientation de depart : face a la citadelle.</summary>
        public static float YawOf(int slot)
        {
            Vector3 p = PadOf(slot);
            return Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;
        }

        /// <summary>Ou l'arbaleste de la plateforme de "slot" le pose : le parvis devant sa porte.</summary>
        public static Vector3 LandingOf(int slot)
        {
            Vector3 p;
            return Landings.TryGetValue(slot, out p) ? p : Course.Plaza(0, 0f);
        }

        /// <summary>L'arbaleste de la plateforme de "slot" (null s'il n'y en a pas).</summary>
        public static Ballista BallistaOf(int slot)
        {
            Ballista b;
            return Ballistas.TryGetValue(slot, out b) ? b : null;
        }

        /// <summary>Vrai si "p" est sur une plateforme de depart.</summary>
        public static bool OnPad(Vector3 p) { return Ground.OnPad(p); }

        /// <summary>Les plateformes : le rocher, un cercle a la couleur du joueur, un fanion, une colonne, l'arbaleste.</summary>
        public static void Build(Transform parent)
        {
            GameObject root = new GameObject("PLATEFORMES DE DÉPART");
            root.transform.SetParent(parent, false);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                Vector3 at = PadOf(i);
                Color c = Match.Slots[i].Colour;
                Ground.BuildPad(root.transform, at, PadRadius, i);
                Transform t = new GameObject("Plateforme de " + Match.Slots[i].Name).transform;
                t.SetParent(root.transform, false);
                t.position = at;
                t.rotation = Quaternion.Euler(0f, YawOf(i), 0f);
                Proto.BeginVisualOnly();
                GameObject ring = Proto.Cylinder(t, new Vector3(0f, 0.04f, 0f), new Vector3(PadRadius * 2f - 0.6f, 0.02f, PadRadius * 2f - 0.6f), Color.white, "Cercle");
                ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 1.2f);
                Proto.Cylinder(t, new Vector3(0f, 0.08f, 0f), new Vector3(PadRadius * 2f - 1.4f, 0.02f, PadRadius * 2f - 1.4f), new Color(0.2f, 0.19f, 0.2f), "Dalle");
                // Le fanion, derriere : on retrouve sa plateforme de loin.
                // (02/10) Une hampe ronde, et un fanion qui claque autour d'elle (avant : un
                // poteau carre et un drapeau fige, lumineux).
                Proto.Cylinder(t, new Vector3(-3.4f, 2.6f, -4.6f), new Vector3(0.16f, 2.6f, 0.16f), new Color(0.25f, 0.2f, 0.16f), "Hampe");
                GameObject hinge = new GameObject("Charnière du fanion");
                hinge.transform.SetParent(t, false);
                hinge.transform.localPosition = new Vector3(-3.35f, 4.6f, -4.6f);
                GameObject flag = Proto.Cube(hinge.transform, new Vector3(0.75f, 0f, 0f), new Vector3(1.5f, 1f, 0.05f), c, "Fanion");
                flag.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.35f, 0f, 0.35f);
                hinge.AddComponent<Flutter>();
                Proto.EndVisualOnly();
                // La colonne de lumiere monte du fanion, au bord : pas en plein milieu, ou
                // l'on apparait (on ne voyait qu'elle).
                LightBeam beam = LightBeam.Build(root.transform, t.TransformPoint(new Vector3(-3.4f, 0f, -4.6f)), c, 0.9f, 24f);
                if (beam != null) beam.targetAlpha = 0.3f;
                // SON arbaleste, au bord, tournee vers la citadelle.
                Vector3 inward = new Vector3(-at.x, 0f, -at.z).normalized;
                Ballista b = Ballista.Build(root.transform, at + inward * 2.6f, YawOf(i));
                // Elle ne vise pas : elle te pose en cloche sur le parvis, devant ta porte (29/09).
                b.SetFixedTarget(LandingOf(i));
                Ballistas[i] = b;
            }
        }
    }

    /// <summary>
    /// TOMBER DANS LES NUAGES (27/09 -- "un beau respawn"). On ne meurt pas : on
    /// reapparait sur sa plateforme de depart (28/09), dans une colonne de lumiere a sa couleur,
    /// protege trois secondes. Si l'on portait la Couronne, elle reste la ou l'on a quitte
    /// le sol (02/10 : plus de retour au sommet).
    /// </summary>
    public static class Respawn
    {
        public const float Grace = 3f;

        public static void Of(Seeker s)
        {
            if (s == null || s.Body == null) return;
            // (02/10 -- Martin : "quand tu meurs, elle respawn a chaque fois au-dessus, c'est
            // horrible de tout remonter") : la Couronne RESTE ou il a quitte le sol.
            // (02/10, le clipper) Pousse dans les nuages juste avant : c'est un KO.
            Highlights.Fell(s);
            if (s.CarriesCrown) Crown.FellWith(s);
            s.LastHitBy = null;
            Vector3 at = Spawns.Of(s.Index, Spawns.PadOf(s.Index)) + Vector3.up * 0.1f;
            // (06/10) L'ANGE GARDIEN : une fois par manche, on revient la ou l'on touchait le sol
            // (jamais sur la tour : pas de point de reprise sur la tour, refuse le 02/10).
            IMover mover = AbilityCaster.MoverOf(s);
            bool angel = false;
            // (08/10) Le PHENIX (divin) : la meme chose, a CHAQUE chute -- et il renait dans une explosion.
            bool phoenix = s.Has(Ability.Phenix);
            if ((phoenix || s.Has(Ability.AngeGardien) && !s.AngelUsed) && mover != null)
            {
                Vector3 g = mover.LastGround;
                if (g.y > Ground.FallLine + 2f && !Tower.On(g))
                {
                    if (!phoenix) s.AngelUsed = true;
                    angel = true;
                    at = g + Vector3.up * 0.3f;
                }
            }
            float yaw = angel && s.Body != null ? s.Body.eulerAngles.y : Spawns.YawOf(s.Index);
            if (s.IsPlayer && Game.Player != null) { Game.Player.Teleport(at, yaw); Game.Player.Forget(); }
            else
            {
                Rival r = Rival.Of(s);
                if (r == null) return;
                r.Teleport(at);
                r.Forget();
                r.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            s.GraceUntil = Time.time + Grace;
            // (05/10) SECOND SOUFFLE : on repart protege six secondes, avec des ailes d'or.
            if (s.Has(Ability.SecondSouffle)) { s.GraceUntil = Time.time + 6f; Wings.Grant(s, true); }
            s.StunnedUntil = -1f;
            s.SlowUntil = -1f;
            // Les sorts des capacites de fou ne survivent pas au plongeon.
            s.RootedUntil = s.GluedUntil = s.InvertedUntil = s.TinyUntil = s.InkUntil = s.BalloonUntil = s.CharmedUntil = -1f;
            if (angel) Fx.Column(at, AbilityInfo.Tint(phoenix ? Ability.Phenix : Ability.AngeGardien), 30f, 0.6f, 1.2f);
            if (angel && phoenix)
            {
                Combat.Blast(at, 9f, 26f, 14f, s);
                // (v33) Il renait en flammes : l'impact divin, des ailes de feu qui s'ouvrent, une aureole.
                Color ph = AbilityInfo.Tint(Ability.Phenix);
                DivineFx.Impact(at, 9f, ph, 1.4f, s.IsPlayer);
                Fx.Burst(at + Vector3.up * 1.5f, ph, 80, 16f, 0.5f, 0.9f, -0.5f, Vector3.left, 25f);
                Fx.Burst(at + Vector3.up * 1.5f, ph, 80, 16f, 0.5f, 0.9f, -0.5f, Vector3.right, 25f);
                DivineFx.Halo(s.Body, ph, 2.5f, 1.2f);
            }
            Fx.Respawn(at, s.Colour);
            Feed.FellIntoClouds(s);
            if (s.IsPlayer && Game.Hud != null) Game.Hud.Flash(new Color(s.Colour.r, s.Colour.g, s.Colour.b, 0.6f));
        }
    }
}
