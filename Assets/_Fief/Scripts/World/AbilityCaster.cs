using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Ce qui fait BOUGER un joueur : toi (PlayerController) ou un bot (Rival). Les
    /// capacites ne connaissent que ca -- en Phase 3, un joueur en ligne l'implementera
    /// a son tour, et rien d'autre ne changera.
    /// </summary>
    public interface IMover
    {
        /// <summary>Projeter (la partie horizontale s'amortit, la verticale souleve).</summary>
        void Push(Vector3 velocity);
        /// <summary>Filer droit dans une direction, a "speed" m/s, pendant "seconds".</summary>
        void Dash(Vector3 direction, float speed, float seconds);
        /// <summary>Etre tire vers un point (le grappin), a "speed" m/s.</summary>
        void PullTo(Vector3 point, float speed);
        /// <summary>Disparaitre et reapparaitre ailleurs (clignement, echange, rappel).</summary>
        void Blink(Vector3 position);
        /// <summary>Ou l'on etait il y a "seconds" secondes (le rappel).</summary>
        Vector3 PastPosition(float seconds);
    }

    /// <summary>
    /// LANCER UNE CAPACITE ACTIVE -- pour toi comme pour un bot, par la meme methode.
    /// Renvoie vrai si elle est partie (elle part alors en recharge).
    ///
    /// Qui porte la Couronne n'a pas les mains libres : ni crochet, ni onde, ni
    /// souffle, ni givre (sauf avec le Porteur). Il peut fuir, pas frapper.
    /// </summary>
    public static class AbilityCaster
    {
        public static IMover MoverOf(Seeker s)
        {
            if (s == null) return null;
            if (s.IsPlayer) return Game.Player;
            return Rival.Of(s);
        }

        /// <summary>Une capacite offensive (interdite au porteur de la Couronne, sauf Porteur).</summary>
        public static bool Offensive(Ability a)
        {
            return a == Ability.Crochet || a == Ability.Onde || a == Ability.Souffle || a == Ability.Gel;
        }

        /// <summary>Pourquoi "s" ne peut pas lancer "a" maintenant (null : il peut).</summary>
        public static string WhyNot(Seeker s, Ability a)
        {
            if (s == null || s.Body == null) return "";
            if (s.Stunned) return "Étourdi";
            if (s.CarriesCrown && Offensive(a) && !s.Has(Ability.Porteur)) return "Mains prises";
            if (!s.Ready(a, Time.time)) return "Recharge";
            return null;
        }

        public static bool Cast(Seeker s, Ability a, Vector3 eye, Vector3 aim)
        {
            if (!AbilityInfo.IsActive(a) || WhyNot(s, a) != null) return false;
            IMover m = MoverOf(s);
            if (m == null) return false;
            float now = Time.time;
            Vector3 pos = s.Body.position;
            Vector3 flat = new Vector3(aim.x, 0f, aim.z);
            flat = flat.sqrMagnitude > 0.001f ? flat.normalized : s.Body.forward;
            Color tint = AbilityInfo.Tint(a);
            if (!s.TrySpend(a, now)) return false;

            switch (a)
            {
                case Ability.Ruee:
                    m.Dash(flat, 28f, 0.28f);
                    break;

                case Ability.Grappin:
                {
                    RaycastHit hit;
                    if (!RayFrom(s, eye, aim, 34f, out hit)) { s.Refund(a); return false; }
                    m.PullTo(hit.point + hit.normal * 0.6f, 26f);
                    Tether.Show(s.Body, null, hit.point, 0.7f, tint);
                    break;
                }

                case Ability.Crochet:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, 24f, 12f);
                    if (t == null) { s.Refund(a); return false; }
                    Vector3 toMe = Combat.Flat(pos - t.Body.position);
                    float d = toMe.magnitude;
                    Combat.Hit(t, toMe.normalized * Mathf.Clamp(d * 2.2f, 10f, 30f) + Vector3.up * 5f, 0.35f, false, s);
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.5f, tint);
                    break;
                }

                case Ability.Onde:
                    Combat.Blast(pos, 6.5f, 17f, 6f, s);
                    Ambiance.Burst(null, pos + Vector3.up * 0.6f, tint);
                    Sfx.Crash();
                    break;

                case Ability.Clignement:
                {
                    float reach = 10f;
                    RaycastHit hit;
                    if (Physics.Raycast(pos + Vector3.up * 1.2f, flat, out hit, reach, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(s.Body))
                        reach = Mathf.Max(0f, hit.distance - 0.8f);
                    Vector3 dest = pos + flat * reach;
                    if (Physics.Raycast(dest + Vector3.up * 2.5f, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) dest.y = hit.point.y + 0.05f;
                    Ambiance.Burst(null, pos + Vector3.up, tint);
                    m.Blink(dest);
                    Ambiance.Burst(null, dest + Vector3.up, tint);
                    break;
                }

                case Ability.Bond:
                    m.Push(Vector3.up * 16f + flat * 4f);
                    Ambiance.Burst(null, pos, tint);
                    break;

                case Ability.Mur:
                    StoneWall.Raise(pos + flat * 3.5f, flat);
                    break;

                case Ability.Nuee:
                    Smoke.Make(pos);
                    break;

                case Ability.Mine:
                    Mine.Place(s, pos);
                    break;

                case Ability.Gel:
                    Thrown.Launch(s, eye + aim * 0.8f, Thrown.Lob(aim, 18f));
                    break;

                case Ability.Voile:
                    s.HiddenUntil = now + 6f;
                    Ambiance.Burst(null, pos + Vector3.up, tint);
                    break;

                case Ability.Echange:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, 32f, 12f);
                    IMover other = MoverOf(t);
                    if (t == null || other == null) { s.Refund(a); return false; }
                    Vector3 mine = pos, theirs = t.Body.position;
                    Ambiance.Burst(null, mine + Vector3.up, tint);
                    Ambiance.Burst(null, theirs + Vector3.up, tint);
                    m.Blink(theirs);
                    other.Blink(mine);
                    break;
                }

                case Ability.Rappel:
                {
                    Vector3 back = m.PastPosition(4f);
                    Ambiance.Burst(null, pos + Vector3.up, tint);
                    m.Blink(back);
                    break;
                }

                case Ability.Souffle:
                    Combat.Cone(s, pos, aim, 12f, 35f, 18f);
                    Ambiance.Burst(null, pos + flat * 2f + Vector3.up, tint);
                    break;
            }
            Sfx.Whoosh();
            if (s.IsPlayer) Stats.Casts++;
            return true;
        }

        /// <summary>Un rayon depuis l'oeil, qui ne s'arrete pas sur son propre corps.</summary>
        static bool RayFrom(Seeker s, Vector3 eye, Vector3 dir, float range, out RaycastHit best)
        {
            RaycastHit[] hits = Physics.RaycastAll(eye, dir.normalized, range, ~0, QueryTriggerInteraction.Ignore);
            best = new RaycastHit();
            float bestD = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (s.Body != null && hits[i].collider.transform.IsChildOf(s.Body)) continue;
                if (hits[i].distance < bestD) { bestD = hits[i].distance; best = hits[i]; found = true; }
            }
            return found;
        }
    }
}
