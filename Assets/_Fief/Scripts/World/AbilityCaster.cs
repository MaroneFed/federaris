using System.Collections.Generic;
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
        /// <summary>LE PIQUE D'AIGLE : fondre sur "target" (le porteur), guide, jusqu'au contact.</summary>
        void Dive(Seeker target);
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
        // Les portees (28/09 : tout plus fort -- Martin : "des pouvoirs beaucoup plus mieux").
        // Le HUD et l'apercu de visee s'en servent.
        public const float GrappinRange = 48f;
        public const float CrochetRange = 32f;
        public const float EchangeRange = 45f;
        public const float AimAngle = 12f;
        public const float RueeReach = 12f;
        public const float BlinkReach = 15f;
        public const float OndeRadius = 9f;
        public const float WallAhead = 4.5f;
        public const float FrostReach = 26f;

        /// <summary>
        /// Les capacites qu'on VISE : touche maintenue, un apercu montre ou elle ira ;
        /// on relache, elle part. Les autres partent des qu'on appuie.
        /// </summary>
        public static bool NeedsAim(Ability a)
        {
            return a == Ability.Ruee || a == Ability.Grappin || a == Ability.Crochet || a == Ability.Clignement
                || a == Ability.Mur || a == Ability.Gel || a == Ability.Echange || a == Ability.Souffle;
        }

        /// <summary>Ou la Ruee s'arrete (un mur la coupe).</summary>
        public static Vector3 RueeEnd(Seeker s, Vector3 flat)
        {
            Vector3 pos = s.Body.position;
            float reach = RueeReach;
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 1f, flat, out hit, reach, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(s.Body))
                reach = Mathf.Max(0f, hit.distance - 0.6f);
            return pos + flat * reach;
        }

        /// <summary>Ou le Clignement fait reapparaitre (devant soi, avant un mur, pose sur le sol).</summary>
        public static Vector3 BlinkDestination(Seeker s, Vector3 flat)
        {
            Vector3 pos = s.Body.position;
            float reach = BlinkReach;
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 1.2f, flat, out hit, reach, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(s.Body))
                reach = Mathf.Max(0f, hit.distance - 0.8f);
            Vector3 dest = pos + flat * reach;
            if (Physics.Raycast(dest + Vector3.up * 2.5f, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) dest.y = hit.point.y + 0.05f;
            return dest;
        }

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
                {
                    // UNE COMETE : douze metres d'un trait, et qui est sur la route est
                    // BOUSCULE sur le cote (une trainee, un anneau qui claque, des etincelles).
                    Vector3 end = RueeEnd(s, flat);
                    float length = (end - pos).magnitude;
                    m.Dash(flat, 36f, Mathf.Max(0.05f, length / 36f));
                    Vector3 side = new Vector3(flat.z, 0f, -flat.x);
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null) continue;
                        Vector3 rel = o.Body.position - pos;
                        float along = Vector3.Dot(Combat.Flat(rel), flat);
                        if (along < 0f || along > length + 1f || Mathf.Abs(rel.y) > 2.2f) continue;
                        float lateral = Vector3.Dot(Combat.Flat(rel), side);
                        if (Mathf.Abs(lateral) > 1.8f) continue;
                        Vector3 away = side * (lateral >= 0f ? 1f : -1f);
                        Combat.Hit(o, (away * 1.2f + flat).normalized * 18f + Vector3.up * 6f, 0.3f, true, s);
                        Fx.Impact(o.Body.position + Vector3.up * 1.1f, tint, 1f);
                    }
                    Fx.Trail(s.Body, tint, 0.45f, 1.1f);
                    Fx.Ring(chest - flat * 0.5f, tint, 0.3f, 2.6f, 0.3f, 0.28f, flat);
                    Fx.Burst(chest, tint, 40, 9f, 0.18f, 0.5f, 0f, -flat, 25f);
                    Fx.Flash(chest, tint, 8f, 3f, 0.25f);
                    Fx.Burst(pos + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 25, 6f, 0.35f, 0.7f, 0.3f, Vector3.up, 60f);
                    break;
                }

                case Ability.Grappin:
                {
                    RaycastHit hit;
                    if (!RayFrom(s, eye, aim, GrappinRange, out hit)) { s.Refund(a); return false; }
                    // Contre un mur ou un rebord (normale a l'horizontale) : on vise un peu
                    // au-dessus, pour arriver PAR-DESSUS le rebord et s'y hisser.
                    Vector3 grip = hit.point + hit.normal * 0.6f;
                    if (Mathf.Abs(hit.normal.y) < 0.5f) grip += Vector3.up * 1.4f;
                    m.PullTo(grip, 32f);
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
                    Combat.Hit(t, toMe.normalized * Mathf.Clamp(d * 2.2f, 12f, 36f) + Vector3.up * 6f, 0.35f, false, s);
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.6f, tint);
                    Fx.Ring(t.Body.position + Vector3.up * 1.1f, tint, 0.2f, 2.2f, 0.4f, 0.2f, toMe);
                    Fx.Sparks(t.Body.position + Vector3.up * 1.1f, tint, 40, 6f);
                    Fx.Trail(t.Body, tint, 0.6f, 0.8f);
                    break;
                }

                case Ability.Onde:
                    // L'ONDE DE CHOC : une sphere qui gonfle, trois anneaux au sol, la poussiere
                    // qui part en couronne, un eclair qui illumine tout autour.
                    int blasted = Combat.Blast(pos, OndeRadius, 24f, 10f, s);
                    Fx.Shock(chest, tint, OndeRadius, 0.5f);
                    Fx.Shock(chest, Color.white, OndeRadius * 0.5f, 0.3f);
                    Fx.GroundRing(pos, tint, OndeRadius + 1f, 0.5f);
                    Fx.GroundRing(pos, Color.white, OndeRadius * 0.7f, 0.35f);
                    Fx.Ring(pos + Vector3.up * 0.2f, tint, 1f, OndeRadius + 4f, 0.8f, 0.6f, Vector3.up);
                    Fx.Ring(pos + Vector3.up * 2.5f, tint, 0.5f, OndeRadius, 0.6f, 0.3f, Vector3.up);
                    Fx.Burst(pos + Vector3.up * 0.3f, tint, 160, 18f, 0.24f, 0.8f, 0.2f, Vector3.zero, 0f);
                    Fx.Burst(pos + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 70, 10f, 0.5f, 1f, 0.3f, Vector3.up, 80f);
                    Fx.Flash(chest, tint, 22f, 8f, 0.45f);
                    ShakeNear(pos, 0.45f);
                    Sfx.Crash();
                    break;

                case Ability.Clignement:
                {
                    Vector3 dest = BlinkDestination(s, flat);
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
                    // UN SAUT IMMENSE : on s'arrache du sol, et le souffle du depart
                    // repousse ceux qui sont tout pres.
                    m.Push(Vector3.up * 22f + flat * 5f);
                    Combat.Blast(pos, 4f, 12f, 4f, s);
                    Fx.GroundRing(pos, tint, 6f, 0.45f);
                    Fx.Shock(pos + Vector3.up * 0.3f, tint, 3.5f, 0.3f);
                    Fx.Column(pos, tint, 20f, 0.3f, 0.7f);
                    Fx.Burst(pos + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 30, 6f, 0.4f, 0.8f, 0.4f, Vector3.up, 60f);
                    Fx.Trail(s.Body, tint, 0.8f, 0.8f);
                    break;

                case Ability.Mur:
                    StoneWall.Raise(pos + flat * WallAhead, flat, s);
                    Fx.Burst(pos + flat * WallAhead, new Color(0.65f, 0.58f, 0.5f), 110, 11f, 0.45f, 1.1f, 0.6f, Vector3.up, 50f);
                    Fx.GroundRing(pos + flat * WallAhead, tint, 8f, 0.45f);
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
                    Thrown.Launch(s, eye + aim * 0.8f, Thrown.Lob(aim, FrostReach));
                    Fx.Burst(eye + aim * 0.8f, tint, 25, 4f, 0.14f, 0.4f, 0f, aim, 20f);
                    break;

                case Ability.Voile:
                    s.HiddenUntil = now + 7f;
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
                    // LE SOUFFLE (28/09 -- Martin : "que ca passe toute la map") : une VAGUE
                    // DE VENT qui part devant toi et traverse l'ile d'un bout a l'autre en
                    // s'elargissant. Tout ce qu'elle rencontre est emporte -- meme en vol.
                    Vector3 dir = aim;
                    dir.y = Mathf.Clamp(dir.y, -0.3f, 0.45f);
                    dir = dir.sqrMagnitude > 0.01f ? dir.normalized : flat;
                    Gale.Launch(s, chest + flat * 1.2f, dir);
                    for (int k = 0; k < 4; k++)
                        Fx.Ring(chest + dir * (1.2f + k * 2.2f), Color.Lerp(tint, Color.white, k * 0.15f), 0.4f + k * 0.5f, 1.6f + k * 1.4f, 0.28f + k * 0.07f, 0.3f - k * 0.05f, dir);
                    Fx.Burst(chest + dir * 0.6f, tint, 120, 24f, 0.2f, 0.55f, 0f, dir, 32f);
                    Fx.Flash(chest + dir * 2f, tint, 14f, 6f, 0.3f);
                    ShakeNear(pos, 0.3f);
                    Sfx.Crash();
                    break;
                }
            }
            Sfx.Whoosh();
            if (s.IsPlayer) Stats.Casts++;
            return true;
        }

        /// <summary>Une secousse de camera si l'effet eclate pres de toi.</summary>
        public static void ShakeNear(Vector3 at, float amount)
        {
            if (Game.PlayerTransform == null || Game.Hud == null || Game.Hud.orbitCamera == null) return;
            float d = (Game.PlayerTransform.position - at).magnitude;
            if (d < 18f) Game.Hud.orbitCamera.Shake(amount * (1f - d / 18f));
        }

        /// <summary>Un rayon depuis l'oeil, qui ne s'arrete pas sur son propre corps.</summary>
        public static bool RayFrom(Seeker s, Vector3 eye, Vector3 dir, float range, out RaycastHit best)
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

    /// <summary>
    /// LA VAGUE DU SOUFFLE (28/09) : un croissant de vent qui file a 60 m/s sur 150 m
    /// -- toute l'ile -- en s'elargissant de 3 a 18 m. Il passe a travers tout (c'est du
    /// vent) et emporte chaque joueur qu'il rencontre, une fois, meme en plein vol
    /// (etourdi, on replie ses ailes : on tombe).
    ///
    /// Ce qu'on voit : trois croissants de lumiere superposes, un torrent de filets
    /// d'air derriere, la poussiere qui se leve la ou il rase le sol.
    /// </summary>
    public class Gale : MonoBehaviour
    {
        public const float Speed = 60f;
        public const float Range = 150f;
        public const float Height = 7f;
        const int Points = 24;

        Seeker by;
        Vector3 dir;
        Vector3 side;
        float travelled;
        float dustTimer;
        bool heard;
        Color tint;
        readonly List<Seeker> struck = new List<Seeker>();
        readonly List<LineRenderer> arcs = new List<LineRenderer>();
        ParticleSystem wake;

        /// <summary>La largeur du front, selon le chemin parcouru.</summary>
        public float Width { get { return Mathf.Lerp(3f, 18f, Mathf.Clamp01(travelled / Range)); } }

        public static void Launch(Seeker by, Vector3 from, Vector3 direction)
        {
            GameObject go = new GameObject("VAGUE DU SOUFFLE");
            go.transform.position = from;
            Gale g = go.AddComponent<Gale>();
            g.by = by;
            g.dir = direction.normalized;
            Vector3 flat = new Vector3(g.dir.x, 0f, g.dir.z);
            if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
            flat.Normalize();
            g.side = new Vector3(flat.z, 0f, -flat.x);
            g.tint = AbilityInfo.Tint(Ability.Souffle);
            Material m = Ambiance.Additive;
            if (m == null) return;
            for (int k = 0; k < 3; k++)
            {
                GameObject a = new GameObject("Croissant");
                a.transform.SetParent(go.transform, false);
                LineRenderer l = a.AddComponent<LineRenderer>();
                l.sharedMaterial = m;
                l.useWorldSpace = true;
                l.positionCount = Points;
                l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                l.receiveShadows = false;
                g.arcs.Add(l);
            }
            g.wake = Ambiance.NewSystem("Sillage", go.transform, Vector3.zero, m);
            ParticleSystem.MainModule main = g.wake.main;
            main.loop = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(g.tint, Color.white);
            main.maxParticles = 900;
            ParticleSystem.EmissionModule emission = g.wake.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 14f;
            ParticleSystem.ShapeModule shape = g.wake.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3f, 4f, 0.5f);
            g.wake.transform.rotation = Quaternion.LookRotation(g.dir);
            ParticleSystemRenderer rd = g.wake.GetComponent<ParticleSystemRenderer>();
            rd.renderMode = ParticleSystemRenderMode.Stretch;
            rd.velocityScale = 0.05f;
            rd.lengthScale = 3f;
            Ambiance.FadeInOut(g.wake, 0.9f);
            g.wake.Play();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float step = Speed * dt;
            travelled += step;
            transform.position += dir * step;
            Vector3 front = transform.position;
            float width = Width;
            float fade = 1f - Mathf.Clamp01((travelled - Range * 0.75f) / (Range * 0.25f));

            // Qui est dans la vague ? (une fois chacun)
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || struck.Contains(s)) continue;
                Vector3 rel = s.Body.position + Vector3.up * 1f - front;
                float along = Vector3.Dot(rel, dir);
                if (along > 1.5f || along < -step - 2f) continue;
                if (Mathf.Abs(Vector3.Dot(rel, side)) > width * 0.5f) continue;
                if (rel.y < -3.5f || rel.y > Height) continue;
                struck.Add(s);
                Vector3 push = new Vector3(dir.x, 0f, dir.z).normalized;
                Combat.Hit(s, push * 30f + Vector3.up * 9f, 0.35f, true, by);
                Fx.Impact(s.Body.position + Vector3.up * 1.1f, tint, 1.2f);
            }

            // Pres de toi : le vent hurle, l'ecran tremble.
            Transform me = Game.PlayerTransform;
            if (!heard && me != null && (me.position - front).magnitude < 18f)
            {
                heard = true;
                Sfx.Whoosh();
                AbilityCaster.ShakeNear(front, 0.3f);
            }

            // La poussiere, la ou il rase le sol.
            dustTimer -= dt;
            if (dustTimer <= 0f)
            {
                dustTimer = 0.07f;
                RaycastHit hit;
                if (Physics.Raycast(front + Vector3.up * 2f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                    Fx.Burst(hit.point + Vector3.up * 0.2f, new Color(0.75f, 0.68f, 0.58f), 10, 7f, 0.5f, 0.7f, 0.1f, dir + Vector3.up * 0.6f, 40f);
            }

            // Les trois croissants : courbes vers l'arriere sur les cotes.
            for (int k = 0; k < arcs.Count; k++)
            {
                LineRenderer l = arcs[k];
                float h = 0.4f + k * 2.6f;
                for (int i = 0; i < Points; i++)
                {
                    float u = i / (float)(Points - 1) * 2f - 1f;
                    Vector3 p = front + side * u * width * 0.5f - dir * (u * u * width * 0.35f) + Vector3.up * (h - 1f);
                    l.SetPosition(i, p);
                }
                l.widthMultiplier = (0.9f - k * 0.2f) * (0.6f + width / 18f);
                Color c = Color.Lerp(tint, Color.white, 0.25f * k);
                l.startColor = new Color(c.r, c.g, c.b, 0.15f * fade);
                l.endColor = new Color(c.r, c.g, c.b, 0.15f * fade);
                // (le milieu plus vif que les bouts)
                Gradient gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(c, 1f) },
                           new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f * fade, 0.5f), new GradientAlphaKey(0f, 1f) });
                l.colorGradient = gr;
            }
            if (wake != null)
            {
                ParticleSystem.ShapeModule shape = wake.shape;
                shape.scale = new Vector3(width, Height * 0.8f, 0.5f);
            }

            if (travelled >= Range)
            {
                if (wake != null)
                {
                    wake.transform.SetParent(null, true);
                    wake.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    Destroy(wake.gameObject, 1f);
                }
                Destroy(gameObject);
            }
        }
    }
}
