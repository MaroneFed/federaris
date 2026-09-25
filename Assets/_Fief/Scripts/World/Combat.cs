using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CONTACT (27/09 : plus d'epee, plus de vie). Tout ce qui touche un joueur le
    /// PROJETTE : la poussee (clic gauche), l'onde de choc, le souffle, le rayon d'un
    /// Oeil, une mine, un pendule de la tour. On ne meurt pas -- on perd sa place, et
    /// la Couronne si on la portait. C'est Smash dans une foret noire.
    ///
    /// Tout passe par Hit() : un seul endroit decide de ce qu'un coup fait (Ancrage,
    /// Prise ferme, etourdissement, Couronne qui tombe). En Phase 3, sur l'hote seulement.
    /// </summary>
    public static class Combat
    {
        public const float ShoveReach = 3f;
        public const float ShoveForce = 14f;

        /// <summary>
        /// POUSSER (clic gauche) : le premier joueur devant soi, a 3 m, part en arriere
        /// et en l'air. Le porteur de la Couronne la lache. Vrai si on a touche quelqu'un.
        /// </summary>
        public static bool Shove(Seeker by, Vector3 forward)
        {
            if (by == null || by.Body == null || !by.CanShove) return false;
            Vector3 f = Flat(forward).normalized;
            float force = by.Has(Ability.Poigne) ? ShoveForce * 2f : ShoveForce;
            Seeker best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || !InArc(by.Body.position, f, s.Body.position, ShoveReach, 65f)) continue;
                float d = Flat(s.Body.position - by.Body.position).magnitude;
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null) return false;
            Vector3 push = Flat(best.Body.position - by.Body.position).normalized;
            if (push.sqrMagnitude < 0.01f) push = f;
            // Un court etourdissement : on ne contre-marche pas une poussee (c'est ce
            // qui la rendait molle -- on reculait de deux metres en appuyant sur Z).
            Hit(best, push * force + Vector3.up * 4.5f, 0.2f, true, by);
            if (by.IsPlayer) { Stats.Shoves++; Hud.HitStop(0.05f); }
            return true;
        }

        /// <summary>
        /// UN COUP : "velocity" projette le joueur, "stun" l'etourdit (secondes), et s'il
        /// porte la Couronne et que "dropsCrown", il la lache -- sauf Prise ferme.
        /// </summary>
        public static void Hit(Seeker victim, Vector3 velocity, float stun, bool dropsCrown, Seeker by)
        {
            if (victim == null || victim.Body == null) return;
            if (victim.Has(Ability.Ancrage)) velocity = new Vector3(velocity.x * 0.5f, velocity.y * 0.7f, velocity.z * 0.5f);
            Knockback(victim, velocity);
            victim.LastHurt = Time.time;
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

            Sfx.Thud();
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
                if (s == by || s.Body == null || s.Hidden) continue;
                Vector3 to = s.Body.position + Vector3.up * 1.1f - eye;
                if (to.magnitude > range) continue;
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
                if (s != me && s.Body != null && InArc(me.Body.position, f, s.Body.position, ShoveReach, 65f)) return true;
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
    /// LES POINTS DE DEPART : un par place, a la lisiere, a egale distance du chateau,
    /// tournes d'un angle au hasard a chaque manche.
    /// </summary>
    public static class Spawns
    {
        static readonly Dictionary<int, Vector3> Points = new Dictionary<int, Vector3>();

        public static void Place(int players, int seed)
        {
            Points.Clear();
            System.Random rng = new System.Random(seed ^ 0x51a);
            float turn = (float)rng.NextDouble() * 360f;
            for (int i = 0; i < players; i++)
            {
                float a = (turn + i * 360f / Mathf.Max(1, players)) * Mathf.Deg2Rad;
                Vector3 best = Vector3.zero;
                float bestScore = float.MaxValue;
                for (int k = 0; k < 24; k++)
                {
                    float aa = a + (k - 12) * 0.02f;
                    float r = 118f + (k % 4) * 5f;
                    float x = Mathf.Cos(aa) * r, z = Mathf.Sin(aa) * r;
                    if (Landmarks.Near(x, z, 10f)) continue;
                    float score = Forest.Canopy(x, z) + Ground.Slope(x, z);
                    if (score < bestScore) { bestScore = score; best = new Vector3(x, 0f, z); }
                }
                Points[i] = Ground.Place(best.x, best.z, 1f);
            }
        }

        public static Vector3 Of(int slot, Vector3 fallback)
        {
            Vector3 p;
            return Points.TryGetValue(slot, out p) ? p : fallback;
        }
    }
}
