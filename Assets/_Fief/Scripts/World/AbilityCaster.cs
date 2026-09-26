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
        /// <summary>Etre lance sur une trajectoire balistique (une arbaleste geante).</summary>
        void Launch(Vector3 velocity);
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
        // Les portees des capacites qui visent (le HUD s'en sert pour dire "-> Mahaut").
        public const float GrappinRange = 34f;
        public const float CrochetRange = 24f;
        public const float EchangeRange = 32f;
        public const float AimAngle = 12f;

        /// <summary>Qui (ou quoi) une capacite visee toucherait maintenant : le nom d'un joueur, "accroche", ou null.</summary>
        public static string AimedAt(Seeker s, Ability a, Vector3 eye, Vector3 aim)
        {
            if (s == null || s.Body == null) return null;
            if (a == Ability.Crochet || a == Ability.Echange)
            {
                Seeker t = Combat.Aimed(s, eye, aim, a == Ability.Crochet ? CrochetRange : EchangeRange, AimAngle);
                return t != null ? t.Name : null;
            }
            if (a == Ability.Grappin)
            {
                RaycastHit hit;
                return RayFrom(s, eye, aim, GrappinRange, out hit) ? "accroche" : null;
            }
            return null;
        }

        /// <summary>Vrai pour les capacites qui visent quelque chose (le HUD dit si la visee est bonne).</summary>
        public static bool Aims(Ability a) { return a == Ability.Crochet || a == Ability.Echange || a == Ability.Grappin; }

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

            Vector3 chest = pos + Vector3.up * 1.1f;
            switch (a)
            {
                case Ability.Ruee:
                    // UNE COMETE : une trainee de lumiere, un anneau qui claque derriere soi, des etincelles.
                    m.Dash(flat, 28f, 0.28f);
                    Fx.Trail(s.Body, tint, 0.45f, 1.1f);
                    Fx.Ring(chest - flat * 0.5f, tint, 0.3f, 2.6f, 0.3f, 0.28f, flat);
                    Fx.Burst(chest, tint, 40, 9f, 0.18f, 0.5f, 0f, -flat, 25f);
                    Fx.Flash(chest, tint, 8f, 3f, 0.25f);
                    break;

                case Ability.Grappin:
                {
                    RaycastHit hit;
                    if (!RayFrom(s, eye, aim, GrappinRange, out hit)) { s.Refund(a); return false; }
                    // Contre un mur ou un rebord (normale a l'horizontale) : on vise un peu
                    // au-dessus, pour arriver PAR-DESSUS le rebord et s'y hisser.
                    Vector3 grip = hit.point + hit.normal * 0.6f;
                    if (Mathf.Abs(hit.normal.y) < 0.5f) grip += Vector3.up * 1.4f;
                    m.PullTo(grip, 26f);
                    Tether.Show(s.Body, null, hit.point, 0.8f, tint);
                    // Le croc qui mord la pierre : gerbe, anneau, eclair.
                    Fx.Burst(hit.point, tint, 35, 7f, 0.15f, 0.5f, 0.3f, hit.normal, 45f);
                    Fx.Ring(hit.point + hit.normal * 0.05f, tint, 0.2f, 1.8f, 0.35f, 0.18f, hit.normal);
                    Fx.Flash(hit.point + hit.normal * 0.5f, tint, 7f, 3f, 0.3f);
                    Fx.Trail(s.Body, tint, 0.9f, 0.7f);
                    break;
                }

                case Ability.Crochet:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, CrochetRange, AimAngle);
                    if (t == null) { s.Refund(a); return false; }
                    Vector3 toMe = Combat.Flat(pos - t.Body.position);
                    float d = toMe.magnitude;
                    Combat.Hit(t, toMe.normalized * Mathf.Clamp(d * 2.2f, 10f, 30f) + Vector3.up * 5f, 0.35f, false, s);
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.6f, tint);
                    Fx.Ring(t.Body.position + Vector3.up * 1.1f, tint, 0.2f, 2.2f, 0.4f, 0.2f, toMe);
                    Fx.Sparks(t.Body.position + Vector3.up * 1.1f, tint, 40, 6f);
                    Fx.Trail(t.Body, tint, 0.6f, 0.8f);
                    break;
                }

                case Ability.Onde:
                    // L'ONDE DE CHOC : une sphere qui gonfle, trois anneaux au sol, la poussiere
                    // qui part en couronne, un eclair qui illumine tout autour.
                    Combat.Blast(pos, 6.5f, 17f, 6f, s);
                    Fx.Shock(chest, tint, 6.5f, 0.45f);
                    Fx.GroundRing(pos, tint, 7f, 0.45f);
                    Fx.GroundRing(pos, Color.white, 5f, 0.3f);
                    Fx.Ring(pos + Vector3.up * 0.2f, tint, 1f, 9f, 0.7f, 0.5f, Vector3.up);
                    Fx.Burst(pos + Vector3.up * 0.3f, tint, 90, 14f, 0.22f, 0.7f, 0.2f, Vector3.zero, 0f);
                    Fx.Burst(pos + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 40, 8f, 0.45f, 0.9f, 0.3f, Vector3.up, 80f);
                    Fx.Flash(chest, tint, 16f, 7f, 0.4f);
                    ShakeNear(pos, 0.35f);
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
                    // On implose ici, on explose la-bas, et un fil de lumiere relie les deux.
                    Fx.Shock(chest, tint, 1.2f, 0.25f);
                    Fx.Burst(chest, tint, 50, 5f, 0.15f, 0.5f, 0f, Vector3.zero, 0f);
                    m.Blink(dest);
                    Fx.Shock(dest + Vector3.up * 1.1f, Color.white, 1.8f, 0.3f);
                    Fx.Burst(dest + Vector3.up * 1.1f, tint, 60, 7f, 0.18f, 0.6f, 0f, Vector3.zero, 0f);
                    Fx.Flash(dest + Vector3.up * 1.1f, tint, 10f, 5f, 0.3f);
                    Tether.Show(s.Body, null, chest, 0.35f, tint);
                    break;
                }

                case Ability.Bond:
                    m.Push(Vector3.up * 16f + flat * 4f);
                    Fx.GroundRing(pos, tint, 4.5f, 0.4f);
                    Fx.Column(pos, tint, 12f, 0.25f, 0.6f);
                    Fx.Burst(pos + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 30, 6f, 0.4f, 0.8f, 0.4f, Vector3.up, 60f);
                    Fx.Trail(s.Body, tint, 0.8f, 0.8f);
                    break;

                case Ability.Mur:
                    StoneWall.Raise(pos + flat * 3.5f, flat);
                    Fx.Burst(pos + flat * 3.5f, new Color(0.65f, 0.58f, 0.5f), 70, 9f, 0.4f, 1f, 0.6f, Vector3.up, 50f);
                    Fx.GroundRing(pos + flat * 3.5f, tint, 5f, 0.4f);
                    ShakeNear(pos, 0.25f);
                    break;

                case Ability.Nuee:
                    Smoke.Make(pos);
                    Fx.Burst(chest, new Color(0.6f, 0.6f, 0.65f), 50, 6f, 0.5f, 1.2f, -0.05f, Vector3.zero, 0f);
                    break;

                case Ability.Mine:
                    Mine.Place(s, pos);
                    Fx.GroundRing(pos, tint, 1.8f, 0.35f);
                    Fx.Sparks(pos + Vector3.up * 0.2f, tint, 20, 3f);
                    break;

                case Ability.Gel:
                    Thrown.Launch(s, eye + aim * 0.8f, Thrown.Lob(aim, 18f));
                    Fx.Burst(eye + aim * 0.8f, tint, 25, 4f, 0.14f, 0.4f, 0f, aim, 20f);
                    break;

                case Ability.Voile:
                    s.HiddenUntil = now + 6f;
                    Fx.Shock(chest, tint, 1.6f, 0.4f);
                    Fx.Burst(chest, tint, 60, 4f, 0.16f, 0.9f, -0.2f, Vector3.zero, 0f);
                    break;

                case Ability.Echange:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, EchangeRange, AimAngle);
                    IMover other = MoverOf(t);
                    if (t == null || other == null) { s.Refund(a); return false; }
                    Vector3 mine = pos, theirs = t.Body.position;
                    Fx.Column(mine, tint, 14f, 0.3f, 0.7f);
                    Fx.Column(theirs, tint, 14f, 0.3f, 0.7f);
                    m.Blink(theirs);
                    other.Blink(mine);
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.6f, tint);
                    Fx.Flash(mine + Vector3.up, tint, 10f, 4f, 0.35f);
                    Fx.Flash(theirs + Vector3.up, tint, 10f, 4f, 0.35f);
                    break;
                }

                case Ability.Rappel:
                {
                    Vector3 back = m.PastPosition(4f);
                    // Un fil de lumiere qui remonte le temps, de la ou l'on est a la ou l'on etait.
                    Tether.Show(s.Body, null, back + Vector3.up * 1.1f, 0.5f, tint);
                    Fx.Burst(chest, tint, 40, 5f, 0.15f, 0.5f, 0f, Vector3.zero, 0f);
                    m.Blink(back);
                    Fx.Shock(back + Vector3.up * 1.1f, tint, 1.5f, 0.3f);
                    Fx.Flash(back + Vector3.up, tint, 9f, 4f, 0.3f);
                    break;
                }

                case Ability.Souffle:
                {
                    // LE SOUFFLE : une rafale en cone -- des anneaux qui filent devant soi en
                    // s'elargissant, un torrent d'etincelles, un eclair.
                    Combat.Cone(s, pos, aim, 12f, 35f, 18f);
                    Vector3 dirF = flat;
                    for (int k = 0; k < 4; k++)
                        Fx.Ring(chest + dirF * (1.2f + k * 2.2f), Color.Lerp(tint, Color.white, k * 0.15f), 0.4f + k * 0.5f, 1.6f + k * 1.4f, 0.28f + k * 0.07f, 0.3f - k * 0.05f, dirF);
                    Fx.Burst(chest + dirF * 0.6f, tint, 120, 24f, 0.2f, 0.55f, 0f, dirF, 32f);
                    Fx.Burst(chest + dirF * 0.6f, Color.white, 40, 18f, 0.12f, 0.4f, 0f, dirF, 18f);
                    Fx.Flash(chest + dirF * 2f, tint, 12f, 5f, 0.3f);
                    ShakeNear(pos, 0.25f);
                    Sfx.Crash();
                    break;
                }
            }
            Sfx.Whoosh();
            if (s.IsPlayer) Stats.Casts++;
            return true;
        }

        /// <summary>Une secousse de camera si l'effet eclate pres de toi.</summary>
        static void ShakeNear(Vector3 at, float amount)
        {
            if (Game.PlayerTransform == null || Game.Hud == null || Game.Hud.orbitCamera == null) return;
            float d = (Game.PlayerTransform.position - at).magnitude;
            if (d < 18f) Game.Hud.orbitCamera.Shake(amount * (1f - d / 18f));
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
