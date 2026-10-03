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
        /// <summary>Le dernier point ou l'on touchait le sol (la Couronne y reste quand on tombe).</summary>
        Vector3 LastGround { get; }
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
        // (05/10 -- "qu'on pousse un peu plus loin") : tout encore un cran au-dessus.
        public const float GrappinRange = 60f;
        public const float CrochetRange = 40f;
        public const float EchangeRange = 45f;
        public const float AimAngle = 12f;
        public const float RueeReach = 15f;
        public const float BlinkReach = 20f;
        public const float OndeRadius = 11f;
        public const float BouletReach = 30f;
        public const float FoudreRange = 60f;
        /// <summary>(06/10) La portee des sorts qu'on vise sur un joueur (prison, bombe, ballon...).</summary>
        public const float CurseRange = 40f;
        public const float TaupeReach = 20f;
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
        public static Vector3 RueeEnd(Seeker s, Vector3 flat) { return DashEnd(s, flat, RueeReach); }

        /// <summary>Ou s'arrete un elan de "reach" metres (un mur le coupe).</summary>
        public static Vector3 DashEnd(Seeker s, Vector3 flat, float reach)
        {
            Vector3 pos = s.Body.position;
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
            if (a == Ability.Crochet || a == Ability.Echange || a == Ability.Foudre || Curses(a))
            {
                Seeker t = Combat.Aimed(s, eye, aim, a == Ability.Crochet ? CrochetRange : a == Ability.Foudre ? FoudreRange : Curses(a) ? CurseRange : EchangeRange, AimAngle);
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
        public static bool Aims(Ability a) { return a == Ability.Crochet || a == Ability.Echange || a == Ability.Grappin || a == Ability.Foudre || Curses(a); }

        /// <summary>(06/10) Les sorts qu'on vise sur un joueur : la prison, la bombe, la tete a l'envers, le mini, le ballon, l'encre.</summary>
        public static bool Curses(Ability a)
        {
            return a == Ability.Prison || a == Ability.Bombe || a == Ability.Inversion || a == Ability.Mini || a == Ability.Ballon || a == Ability.Encre
                || a == Ability.Lasso || a == Ability.Hypnose || a == Ability.Geyser || a == Ability.MainDeDieu || a == Ability.FoudreChaine || a == Ability.FrappeOrbitale;
        }

        public static IMover MoverOf(Seeker s)
        {
            if (s == null) return null;
            if (s.IsPlayer) return Game.Player;
            return Rival.Of(s);
        }

        /// <summary>Une capacite offensive (interdite au porteur de la Couronne, sauf Porteur).</summary>
        public static bool Offensive(Ability a)
        {
            return a == Ability.Crochet || a == Ability.Onde || a == Ability.Souffle || a == Ability.Gel
                || a == Ability.Meteore || a == Ability.Tornade || a == Ability.TrouNoir || a == Ability.Foudre
                || a == Ability.Boulet || Curses(a) || a == Ability.Seisme || a == Ability.Gant || a == Ability.Deluge
                || a == Ability.Toupie || a == Ability.Taupe
                || a == Ability.Missile || a == Ability.Apesanteur || a == Ability.Boomerang || a == Ability.Oreillers
                || a == Ability.Raz || a == Ability.Cri
                || a == Ability.Apocalypse || a == Ability.ArretTemps || a == Ability.Rayon || a == Ability.Tempete
                || a == Ability.Nuke || a == Ability.MainDeDieu || a == Ability.Essaim || a == Ability.GraviteZero
                || a == Ability.Bombardement || a == Ability.Singularite || a == Ability.Dragon || a == Ability.Comete
                || a == Ability.Cataclysme || a == Ability.Chaos || a == Ability.Geole || a == Ability.Tsunami || a == Ability.FoudreChaine
                || a == Ability.Armee || a == Ability.Volcan || a == Ability.FrappeOrbitale || a == Ability.Rocher || a == Ability.Lune
                || a == Ability.Ouragan || a == Ability.FrappeCiel || a == Ability.Enclumes || a == Ability.Lilliput || a == Ability.Demence;
        }

        /// <summary>Pourquoi "s" ne peut pas lancer "a" maintenant (null : il peut).</summary>
        public static string WhyNot(Seeker s, Ability a)
        {
            if (s == null || s.Body == null) return "";
            if (s.Stunned) return "Étourdi";
            if (s.CarriesCrown && Offensive(a) && !s.Has(Ability.Porteur)) return "Mains prises";
            // La Fusee : la Couronne est trop lourde pour elle, et pas de raccourci dans la citadelle.
            if (a == Ability.Fusee && s.CarriesCrown) return "Trop lourd";
            if (a == Ability.FrappeCiel && s.CarriesCrown) return "Trop lourd";
            if (a == Ability.Fusee && Castle.Inside(s.Body.position)) return "Pas ici";
            // (06/10) Le Fantome : avec la Couronne, il gagnerait au Monument sans qu'on puisse rien faire.
            if ((a == Ability.Fantome || a == Ability.Catapulte || a == Ability.Teleport || a == Ability.Invincible) && s.CarriesCrown) return "Trop lourd";
            if (s.Rooted) return "Enchaîné";
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
                    m.Dash(flat, 40f, Mathf.Max(0.05f, length / 40f));
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
                        Combat.Hit(o, (away * 1.2f + flat).normalized * 22f + Vector3.up * 7f, 0.3f, true, s);
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
                    // Pas de grappin sur la muraille depuis dehors : on se hissait sur le
                    // rempart et on sautait dans la cour -- le couloir et la porte contournes.
                    if (!Castle.Inside(pos) && Castle.Inside(hit.point - hit.normal * 0.4f))
                    {
                        s.Refund(a);
                        Fx.Sparks(hit.point, Ward.Rune, 14, 3f);
                        return false;
                    }
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
                    int blasted = Combat.Blast(pos, OndeRadius, 30f, 12f, s);
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
                    Sfx.CrashAt(pos);
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
                    m.Push(Vector3.up * 26f + flat * 6f);
                    Combat.Blast(pos, 5f, 16f, 6f, s);
                    Fx.GroundRing(pos, tint, 6f, 0.45f);
                    Fx.Shock(pos + Vector3.up * 0.3f, tint, 3.5f, 0.3f);
                    Fx.Column(pos, tint, 20f, 0.3f, 0.7f);
                    Fx.Burst(pos + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 30, 6f, 0.4f, 0.8f, 0.4f, Vector3.up, 60f);
                    Fx.Trail(s.Body, tint, 0.8f, 0.8f);
                    break;

                case Ability.Mur:
                    // Pas de mur en plein vol : il sort du SOL.
                    if (!StoneWall.Raise(pos + flat * WallAhead, flat, s)) { s.Refund(a); return false; }
                    Fx.Burst(pos + flat * WallAhead, new Color(0.65f, 0.58f, 0.5f), 110, 11f, 0.45f, 1.1f, 0.6f, Vector3.up, 50f);
                    Fx.GroundRing(pos + flat * WallAhead, tint, 8f, 0.45f);
                    ShakeNear(pos, 0.25f);
                    break;

                case Ability.Nuee:
                    Smoke.Make(pos);
                    Fx.Burst(chest, new Color(0.6f, 0.6f, 0.65f), 50, 6f, 0.5f, 1.2f, -0.05f, Vector3.zero, 0f);
                    break;

                case Ability.Mine:
                    // Pas de mine en plein vol : elle flotterait dans le vide.
                    if (Mine.Place(s, pos) == null) { s.Refund(a); return false; }
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
                    // Pas d'echange a travers la muraille : on entre dans la citadelle par une
                    // porte, jamais en prenant la place de quelqu'un qui y est deja. Et un
                    // protege ne se deplace pas.
                    if (t.Graced || Castle.Inside(mine) != Castle.Inside(theirs))
                    {
                        s.Refund(a);
                        Fx.Sparks(t.Body.position + Vector3.up * 1.1f, Ward.Rune, 14, 3f);
                        return false;
                    }
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

                // ================================================ les capacites de malade (05/10)
                case Ability.Meteore:
                    MeteorStrike.Begin(s, m, flat);
                    Fx.GroundRing(pos, tint, 5f, 0.35f);
                    break;

                case Ability.Tornade:
                    Twister.Launch(s, pos + flat * 3f, flat);
                    Fx.Ring(chest + flat * 2f, tint, 0.5f, 3f, 0.3f, 0.25f, flat);
                    Fx.Burst(pos + Vector3.up * 0.2f, new Color(0.65f, 0.58f, 0.5f), 40, 8f, 0.4f, 0.8f, 0.3f, flat + Vector3.up, 50f);
                    break;

                case Ability.TrouNoir:
                {
                    // Au sol, la ou l'on regarde (24 m au plus) ; sinon seize metres devant.
                    RaycastHit hit;
                    Vector3 at = RayFrom(s, eye, aim, 24f, out hit) ? hit.point : pos + flat * 16f;
                    if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 10f, ~0, QueryTriggerInteraction.Ignore)) at = hit.point;
                    Vortex.Open(s, at);
                    Tether.Show(s.Body, null, at + Vector3.up * 1.2f, 0.4f, tint);
                    break;
                }

                case Ability.Boulet:
                {
                    // LE BOULET DE CANON : trente metres d'un coup, un peu en l'air ; qui est sur la
                    // route DECOLLE (bien plus fort que la Ruee).
                    Vector3 end = DashEnd(s, flat, BouletReach);
                    float length = (end - pos).magnitude;
                    m.Dash(flat + Vector3.up * 0.08f, 48f, Mathf.Max(0.05f, length / 48f));
                    Vector3 side = new Vector3(flat.z, 0f, -flat.x);
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null) continue;
                        Vector3 rel = o.Body.position - pos;
                        float along = Vector3.Dot(Combat.Flat(rel), flat);
                        if (along < 0f || along > length + 1.5f || Mathf.Abs(rel.y) > 2.5f) continue;
                        float lateral = Vector3.Dot(Combat.Flat(rel), side);
                        if (Mathf.Abs(lateral) > 2.2f) continue;
                        Vector3 away = side * (lateral >= 0f ? 1f : -1f);
                        Combat.Hit(o, (away * 0.7f + flat).normalized * 30f + Vector3.up * 12f, 0.45f, true, s);
                        Fx.Impact(o.Body.position + Vector3.up * 1.1f, tint, 1.6f);
                    }
                    Fx.Trail(s.Body, tint, 0.7f, 1.6f);
                    Fx.Shock(chest, tint, 3f, 0.3f);
                    Fx.Burst(chest, tint, 70, 14f, 0.25f, 0.6f, 0f, -flat, 30f);
                    Fx.Flash(chest, tint, 14f, 5f, 0.3f);
                    Sfx.KoBoom(pos, s.IsPlayer);
                    break;
                }

                case Ability.Geant:
                    GiantAura.Grow(s);
                    break;

                case Ability.Fusee:
                    // LA FUSEE : on decolle tout droit (une vingtaine de metres), et la-haut, les
                    // ailes d'or s'ouvrent en VOL LIBRE (comme apres une arbaleste).
                    m.Push(Vector3.up * 32f + flat * 3f);
                    Wings.Grant(s, false);
                    s.FreeFlight = true;
                    Fx.Column(pos, tint, 30f, 0.4f, 0.9f);
                    Fx.Burst(pos + Vector3.up * 0.2f, new Color(1f, 0.6f, 0.25f), 90, 10f, 0.35f, 0.8f, 0.4f, Vector3.down, 40f);
                    Fx.Trail(s.Body, tint, 1.5f, 1f);
                    Sfx.KoBoom(pos, s.IsPlayer);
                    break;

                case Ability.Ressort:
                {
                    // Le trampoline a tes pieds -- et tu rebondis dessus tout de suite.
                    if (Spring.Place(s, pos) == null) { s.Refund(a); return false; }
                    m.Push(Vector3.up * 26f + flat * 4f);
                    Sfx.BoingAt(pos);
                    break;
                }

                // ---- les capacites de fou (06/10)
                case Ability.Prison:
                case Ability.Inversion:
                case Ability.Mini:
                case Ability.Ballon:
                case Ability.Encre:
                case Ability.Bombe:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, CurseRange, AimAngle);
                    if (t == null || t.Graced) { s.Refund(a); return false; }
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.3f, tint);
                    Fx.Flash(chest, tint, 8f, 3f, 0.2f);
                    if (a == Ability.Bombe) StickyBomb.Stick(s, t);
                    else
                    {
                        Combat.Affliction what = a == Ability.Prison ? Combat.Affliction.Prison : a == Ability.Inversion ? Combat.Affliction.Inverted
                            : a == Ability.Mini ? Combat.Affliction.Tiny : a == Ability.Ballon ? Combat.Affliction.Balloon : Combat.Affliction.Ink;
                        float secs = a == Ability.Prison ? 10f : a == Ability.Inversion ? 6f : a == Ability.Mini ? 7f : a == Ability.Ballon ? 3.5f : 5f;
                        Combat.Afflict(t, what, secs, s);
                    }
                    break;
                }

                case Ability.Glu:
                    if (!GluePuddle.Pour(s, pos + flat * 6f)) { s.Refund(a); return false; }
                    break;

                case Ability.Banane:
                {
                    // Trois peaux en eventail DERRIERE toi : pour qui te court apres.
                    Vector3 side = new Vector3(flat.z, 0f, -flat.x);
                    for (int k = -1; k <= 1; k++) BananaPeel.Drop(s, pos - flat * 2.5f + side * (k * 1.6f));
                    Sfx.PafAt(pos);
                    break;
                }

                case Ability.Seisme:
                {
                    // Tous ceux qui sont DEBOUT a 25 m (pas ceux qui volent) decollent.
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null) continue;
                        Vector3 d = o.Body.position - pos;
                        if (Mathf.Abs(d.y) > 4f || Combat.Flat(d).magnitude > 25f) continue;
                        Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                        Combat.Hit(o, Vector3.up * 17f + away * 4f, 0.5f, true, s);
                    }
                    Fx.GroundRing(pos, tint, 25f, 0.7f);
                    Fx.GroundRing(pos, Color.white, 14f, 0.45f);
                    Fx.Burst(pos + Vector3.up * 0.2f, new Color(0.6f, 0.5f, 0.4f), 120, 10f, 0.35f, 1f, 0.6f, Vector3.up, 80f);
                    ShakeNear(pos, 0.7f);
                    Sfx.KoBoom(pos, s.IsPlayer);
                    break;
                }

                case Ability.Gant:
                {
                    // UN COUP MONSTRUEUX : tout ce qui est devant, a 8 m, dans un cone de 50 degres.
                    BoxingGlove.Throw(chest + flat * 0.8f, flat);
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || !Combat.InArc(pos, flat, o.Body.position, 8f, 50f)) continue;
                        Combat.Hit(o, flat * 38f + Vector3.up * 13f, 0.4f, true, s);
                    }
                    Fx.Shock(chest + flat * 4f, tint, 3f, 0.25f);
                    ShakeNear(pos, 0.35f);
                    Sfx.BigPush(pos + flat * 3f, s.IsPlayer);
                    break;
                }

                case Ability.Fantome:
                    // Quatre secondes : plus rien ne le touche (Combat.Hit le saute, comme un protege).
                    s.GraceUntil = Mathf.Max(s.GraceUntil, now + 4f);
                    Fx.Burst(chest, new Color(0.85f, 0.95f, 1f), 70, 5f, 0.3f, 1f, -0.3f, Vector3.zero, 0f);
                    Fx.Shock(chest, tint, 2.5f, 0.35f);
                    Sfx.WhooshAt(pos);
                    break;

                case Ability.Taupe:
                {
                    // Sous terre (un nuage de terre), puis on ressort 20 m plus loin et tout s'envole.
                    Vector3 dest = DashEnd(s, flat, TaupeReach);
                    RaycastHit g;
                    if (Physics.Raycast(dest + Vector3.up * 2.5f, Vector3.down, out g, 6f, ~0, QueryTriggerInteraction.Ignore)) dest.y = g.point.y + 0.05f;
                    else dest = BlinkDestination(s, flat);
                    Color dirt = new Color(0.55f, 0.42f, 0.3f);
                    Fx.Burst(pos + Vector3.up * 0.2f, dirt, 70, 7f, 0.35f, 0.8f, 0.6f, Vector3.up, 70f);
                    m.Blink(dest);
                    Combat.Blast(dest, 5f, 24f, 16f, s);
                    Fx.Burst(dest + Vector3.up * 0.2f, dirt, 110, 12f, 0.4f, 1f, 0.7f, Vector3.up, 60f);
                    Fx.GroundRing(dest, tint, 5f, 0.4f);
                    Sfx.CrashAt(dest);
                    break;
                }

                case Ability.Deluge:
                {
                    RaycastHit hit;
                    Vector3 where = RayFrom(s, eye, aim, 45f, out hit) ? hit.point : pos + flat * 18f;
                    MeteorShower.Rain(s, where);
                    Fx.Flash(chest, tint, 8f, 3f, 0.2f);
                    break;
                }

                case Ability.Toupie:
                    SpinAura.Spin(s);
                    break;

                // ---- les DIVINES (08/10, Mode Dieu)
                case Ability.Apocalypse:
                    MeteorShower.Rain(s, pos, 25, 28f, 3.5f, true);
                    Fx.Flash(chest, tint, 30f, 8f, 0.4f);
                    ShakeNear(pos, 0.5f);
                    break;
                case Ability.ArretTemps:
                {
                    // Tous les autres (a 70 m) sont figes dans la glace -- un coup les libere.
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || (o.Body.position - pos).magnitude > 70f) continue;
                        if (Combat.Afflict(o, Combat.Affliction.Frozen, 3.5f, s)) n++;
                    }
                    if (n == 0) { s.Refund(a); return false; }
                    Fx.Shock(chest, tint, 70f, 0.8f);
                    if (Game.Hud != null) Game.Hud.Flash(new Color(0.6f, 0.85f, 1f, 0.5f));
                    Sfx.Alarm();
                    break;
                }
                case Ability.Rayon:
                    DivineBeam.Fire(s);
                    break;
                case Ability.Teleport:
                {
                    // La ou tu vises (150 m) -- mais pas dans la citadelle, ni sur la tour : le sceau
                    // et "la tour se monte a pied" tiennent, meme pour un dieu.
                    RaycastHit hit;
                    if (!RayFrom(s, eye, aim, 150f, out hit)) { s.Refund(a); return false; }
                    Vector3 dest = hit.point + hit.normal * 0.3f;
                    if (Castle.Inside(dest) || Castle.Inside(pos) || Tower.On(dest)) { s.Refund(a); Fx.Sparks(hit.point, Ward.Rune, 14, 3f); return false; }
                    Fx.Column(pos, tint, 25f, 0.4f, 1f);
                    m.Blink(dest);
                    Fx.Column(dest, tint, 25f, 0.4f, 1f);
                    Fx.Shock(dest + Vector3.up, Color.white, 3f, 0.3f);
                    Tether.Show(s.Body, null, chest, 0.4f, tint);
                    break;
                }
                case Ability.Tempete:
                    for (int k = 0; k < 6; k++)
                    {
                        float ang = k * 60f;
                        Vector3 d = Quaternion.Euler(0f, ang, 0f) * flat;
                        Twister.Launch(s, pos + d * 3f, d);
                    }
                    Fx.Shock(chest, tint, 8f, 0.4f);
                    break;
                case Ability.Nuke:
                    NukeBomb.Arm(s, pos);
                    break;
                case Ability.MainDeDieu:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, 60f, AimAngle);
                    if (t == null || t.Graced) { s.Refund(a); return false; }
                    HandOfGod.Strike(s, t);
                    break;
                }
                case Ability.Essaim:
                    for (int k = 0; k < 8; k++)
                    {
                        Vector3 d = Quaternion.Euler(-20f - (k % 2) * 15f, -70f + k * 20f, 0f) * flat;
                        HomingMissile.Fire(s, chest + d * 1.2f, d);
                    }
                    break;
                case Ability.GraviteZero:
                {
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || (o.Body.position - pos).magnitude > 40f) continue;
                        if (Combat.Afflict(o, Combat.Affliction.Balloon, 4f, s)) n++;
                    }
                    if (n == 0) { s.Refund(a); return false; }
                    Fx.Shock(chest, tint, 40f, 0.7f);
                    break;
                }
                // ---- la deuxieme fournee divine (08/10 au soir)
                case Ability.Bombardement:
                    BombCarpet.Drop(s, pos, flat);
                    break;
                case Ability.Singularite:
                {
                    RaycastHit hit;
                    Vector3 where = RayFrom(s, eye, aim, 60f, out hit) ? hit.point : pos + flat * 25f;
                    Singularity.Open(s, where);
                    break;
                }
                case Ability.Dragon:
                    DragonBreath.Breathe(s);
                    break;
                case Ability.Comete:
                {
                    RaycastHit hit;
                    Vector3 where = RayFrom(s, eye, aim, 90f, out hit) ? hit.point : pos + flat * 30f;
                    CometStrike.Call(s, where);
                    break;
                }
                case Ability.Cataclysme:
                {
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null) continue;
                        Vector3 d = o.Body.position - pos;
                        if (Combat.Flat(d).magnitude > 100f || Mathf.Abs(d.y) > 8f) continue;
                        Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                        Combat.Hit(o, Vector3.up * 30f + away * 6f, 0.6f, true, s);
                        n++;
                    }
                    Fx.GroundRing(pos, tint, 100f, 1.2f);
                    Fx.GroundRing(pos, Color.white, 50f, 0.8f);
                    Fx.Burst(pos + Vector3.up * 0.3f, new Color(0.55f, 0.45f, 0.35f), 300, 20f, 0.5f, 1.5f, 0.7f, Vector3.up, 80f);
                    ShakeNear(pos, 1.2f);
                    Sfx.KoBoom(pos, s.IsPlayer);
                    if (n == 0) { s.Refund(a); return false; }
                    break;
                }
                case Ability.Chaos:
                {
                    // Tout le monde DEHORS (pas dans la citadelle) echange de place au hasard -- sauf
                    // le porteur : la Couronne ne se teleporte pas (06/10 : "il faut monter la tour").
                    List<Seeker> who = new List<Seeker>();
                    List<Vector3> where = new List<Vector3>();
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o.Body == null || o.CarriesCrown || o.Graced && o != s || Castle.Inside(o.Body.position)) continue;
                        who.Add(o);
                        where.Add(o.Body.position);
                    }
                    if (who.Count < 2) { s.Refund(a); return false; }
                    for (int i = where.Count - 1; i > 0; i--) { int j = Random.Range(0, i); Vector3 t = where[i]; where[i] = where[j]; where[j] = t; }
                    for (int i = 0; i < who.Count; i++)
                    {
                        IMover om = MoverOf(who[i]);
                        if (om == null) continue;
                        Fx.Column(who[i].Body.position, tint, 20f, 0.3f, 0.8f);
                        om.Blink(where[i] + Vector3.up * 0.2f);
                        Fx.Shock(where[i] + Vector3.up, tint, 2.5f, 0.3f);
                    }
                    if (Game.Hud != null) Game.Hud.Flash(new Color(0.7f, 0.4f, 1f, 0.4f));
                    Sfx.WhooshAt(pos);
                    break;
                }
                case Ability.AnneauFeu:
                    FireRingZone.Light(s, pos);
                    break;
                case Ability.Geole:
                {
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || (o.Body.position - pos).magnitude > 50f) continue;
                        if (Combat.Afflict(o, Combat.Affliction.Prison, 4f, s)) n++;
                    }
                    if (n == 0) { s.Refund(a); return false; }
                    Fx.Shock(chest, tint, 50f, 0.6f);
                    Sfx.ClangAt(pos);
                    break;
                }
                case Ability.Tsunami:
                    TidalWave.Roll(s, pos, 60f, 2.2f, 32f);
                    break;
                case Ability.FoudreChaine:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, 60f, AimAngle);
                    if (t == null) { s.Refund(a); return false; }
                    Lightning.Call(s, t.Body.position);
                    // Puis elle saute sur les quatre voisins les plus proches (a 20 m de la cible).
                    List<Seeker> near = new List<Seeker>();
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o == t || o.Body == null || (o.Body.position - t.Body.position).magnitude > 20f) continue;
                        near.Add(o);
                    }
                    near.Sort((x, y) => (x.Body.position - t.Body.position).sqrMagnitude.CompareTo((y.Body.position - t.Body.position).sqrMagnitude));
                    for (int i = 0; i < near.Count && i < 4; i++)
                    {
                        Lightning.Call(s, near[i].Body.position);
                        Tether.Show(t.Body, near[i].Body, Vector3.zero, 0.4f, tint);
                    }
                    break;
                }

                // ---- la troisieme fournee divine (09/10, v31 : "des trucs de malade")
                case Ability.Armee:
                    for (int k = 0; k < 6; k++)
                    {
                        Vector3 d = Quaternion.Euler(0f, -50f + k * 20f, 0f) * flat;
                        BeanBomber.Release(s, pos + d * 1.5f, d);
                    }
                    Fx.Shock(chest, tint, 4f, 0.3f);
                    Sfx.BoingAt(pos);
                    break;
                case Ability.Volcan:
                {
                    RaycastHit hit;
                    Vector3 where = RayFrom(s, eye, aim, 60f, out hit) ? hit.point : pos + flat * 20f;
                    Volcano.Raise(s, where);
                    break;
                }
                case Ability.FrappeOrbitale:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, 80f, AimAngle);
                    if (t == null || t.Graced) { s.Refund(a); return false; }
                    OrbitalStrike.Lock(s, t);
                    Fx.Column(pos, tint, 40f, 0.3f, 0.6f);
                    break;
                }
                case Ability.Rocher:
                    GiantBoulder.Roll(s, pos + flat * 5f, flat);
                    break;
                case Ability.Lune:
                {
                    RaycastHit hit;
                    Vector3 where = RayFrom(s, eye, aim, 120f, out hit) ? hit.point : pos + flat * 40f;
                    MoonFall.Drop(s, where);
                    if (Game.Hud != null && s.IsPlayer) Game.Hud.Flash(new Color(0.85f, 0.88f, 1f, 0.35f));
                    break;
                }
                case Ability.Ouragan:
                    Hurricane.Spin(s);
                    break;
                case Ability.FrappeCiel:
                {
                    // Tu bondis tres haut et tu t'ecrases la ou tu regardes (60 m au plus). Jamais
                    // depuis ou vers la citadelle (le sceau), jamais sur la tour (elle se monte a pied).
                    if (Castle.Inside(pos)) { s.Refund(a); return false; }
                    RaycastHit hit;
                    Vector3 dest = RayFrom(s, eye, aim, 60f, out hit) ? hit.point : pos + flat * 25f;
                    if (Castle.Inside(dest) || Tower.On(dest)) { s.Refund(a); Fx.Sparks(dest, Ward.Rune, 14, 3f); return false; }
                    const float T = 3.2f;
                    const float G = 22f;
                    Vector3 d = dest - pos;
                    m.Launch(new Vector3(d.x / T, (d.y + 0.5f * G * T * T) / T, d.z / T));
                    SkySlam.Arm(s);
                    Fx.Column(pos, tint, 35f, 0.4f, 1.2f);
                    Fx.Burst(pos + Vector3.up * 0.2f, new Color(0.6f, 0.52f, 0.42f), 90, 12f, 0.4f, 0.8f, 0.5f, Vector3.up, 60f);
                    Sfx.KoBoom(pos, s.IsPlayer);
                    break;
                }
                case Ability.Enclumes:
                {
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || o.Graced) continue;
                        AnvilDrop.On(s, o);
                        n++;
                    }
                    if (n == 0) { s.Refund(a); return false; }
                    Sfx.Alarm();
                    break;
                }
                case Ability.Lilliput:
                case Ability.Demence:
                {
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || (o.Body.position - pos).magnitude > 60f) continue;
                        bool hit = a == Ability.Lilliput ? Combat.Afflict(o, Combat.Affliction.Tiny, 6f, s)
                                                         : Combat.Afflict(o, Combat.Affliction.Inverted, 5f, s);
                        if (a == Ability.Demence && hit) Combat.Afflict(o, Combat.Affliction.Ink, 4f, s);
                        if (hit) n++;
                    }
                    if (n == 0) { s.Refund(a); return false; }
                    Fx.Shock(chest, tint, 60f, 0.7f);
                    Fx.Burst(chest, tint, 120, 20f, 0.3f, 0.8f, 0f, Vector3.zero, 0f);
                    break;
                }

                case Ability.Invincible:
                    s.GraceUntil = Mathf.Max(s.GraceUntil, now + 6f);
                    GiantAura.Grow(s);
                    Fx.Column(pos, tint, 40f, 0.6f, 2f);
                    break;

                // ---- la deuxieme fournee (07/10)
                case Ability.Lasso:
                {
                    // Tu l'attrapes au lasso, et tu le jettes LA OU TU REGARDES.
                    Seeker t = Combat.Aimed(s, eye, aim, CurseRange, AimAngle);
                    if (t == null || t.Graced) { s.Refund(a); return false; }
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.6f, tint);
                    Combat.Hit(t, flat * 28f + Vector3.up * 14f, 0.4f, true, s);
                    Fx.Ring(t.Body.position + Vector3.up, tint, 0.4f, 2f, 0.3f, 0.12f, Vector3.up);
                    Sfx.WhooshAt(t.Body.position);
                    break;
                }
                case Ability.Hypnose:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, CurseRange, AimAngle);
                    if (t == null || t.Graced) { s.Refund(a); return false; }
                    Tether.Show(s.Body, t.Body, Vector3.zero, 0.5f, tint);
                    Combat.Afflict(t, Combat.Affliction.Charmed, 3f, s);
                    break;
                }
                case Ability.Geyser:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, 45f, AimAngle);
                    if (t == null) { s.Refund(a); return false; }
                    Geyser.Erupt(s, t.Body.position);
                    break;
                }
                case Ability.Missile:
                    HomingMissile.Fire(s, chest + flat * 1.2f, aim);
                    break;
                case Ability.Apesanteur:
                {
                    // Tous ceux qui sont a 14 m s'envolent comme des ballons.
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || (o.Body.position - pos).magnitude > 14f) continue;
                        if (Combat.Afflict(o, Combat.Affliction.Balloon, 2.5f, s)) n++;
                    }
                    Fx.Shock(chest, tint, 14f, 0.5f);
                    Fx.Burst(chest, Color.white, 80, 6f, 0.2f, 1.2f, -0.6f, Vector3.up, 80f);
                    if (n == 0) { s.Refund(a); return false; }
                    break;
                }
                case Ability.Flammes:
                    FireTrail.Light(s);
                    break;
                case Ability.Pogo:
                    PogoStick.Jump(s);
                    break;
                case Ability.Boomerang:
                    BoomerangThrow.Throw(s, chest + flat, flat);
                    break;
                case Ability.PiegeLoup:
                    if (!JawTrap.Place(s, pos)) { s.Refund(a); return false; }
                    break;
                case Ability.Catapulte:
                    // En cloche, loin devant (l'elan ne se freine presque pas : comme une poussee).
                    m.Push(flat * 24f + Vector3.up * 15f);
                    s.Launch(1.6f);
                    Fx.Burst(pos + Vector3.up * 0.2f, tint, 50, 8f, 0.3f, 0.7f, 0.4f, -flat, 50f);
                    Fx.Trail(s.Body, tint, 1.2f, 0.8f);
                    break;
                case Ability.Oreillers:
                    PillowVolley.Begin(s);
                    break;
                case Ability.Raz:
                    TidalWave.Roll(s, pos);
                    break;
                case Ability.CoupDePied:
                    s.SuperShoveUntil = now + 5f;
                    Fx.Ring(pos + Vector3.up * 0.3f, tint, 0.3f, 2f, 0.3f, 0.15f, Vector3.up);
                    Fx.Sparks(pos + Vector3.up * 0.4f, tint, 20, 4f);
                    break;
                case Ability.Cri:
                {
                    // Un cri : tout ce qui est devant, a 12 m, est sonne (0,7 s) et recule un peu.
                    int n = 0;
                    for (int i = 0; i < Game.Seekers.Count; i++)
                    {
                        Seeker o = Game.Seekers[i];
                        if (o == s || o.Body == null || !Combat.InArc(pos, flat, o.Body.position, 12f, 70f)) continue;
                        Combat.Hit(o, flat * 7f + Vector3.up * 4f, 0.7f, false, s);
                        n++;
                    }
                    for (int k = 0; k < 5; k++) Fx.Ring(chest + flat * (1f + k * 2.2f), tint, 0.5f + k * 0.6f, 1.5f + k * 1.2f, 0.25f + k * 0.05f, 0.18f, flat);
                    Sfx.BigPush(pos, s.IsPlayer);
                    break;
                }

                case Ability.Foudre:
                {
                    Seeker t = Combat.Aimed(s, eye, aim, FoudreRange, AimAngle);
                    if (t == null) { s.Refund(a); return false; }
                    Lightning.Call(s, t.Body.position);
                    Fx.Flash(chest, tint, 8f, 3f, 0.2f);
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
                    Sfx.CrashAt(pos);
                    break;
                }
            }
            Sfx.WhooshAt(pos);
            Flourish(s, a, pos, tint);
            if (s.IsPlayer) Stats.Casts++;
            // (08/10) L'ECHO (divin) : elle repart une seconde fois, une demi-seconde apres.
            if (s.Has(Ability.Echo) && !EchoCast.Echoing) { s.Refund(a); EchoCast.Schedule(s, a); }
            // (07/10) CHANCEUX : une fois sur trois, la capacite revient tout de suite.
            else if (s.Has(Ability.Chanceux) && Random.value < 0.33f)
            {
                s.Refund(a);
                Fx.Sparks(chest, AbilityInfo.Tint(Ability.Chanceux), 25, 4f);
                if (s.IsPlayer) Sfx.Pop();
            }
            return true;
        }

        /// <summary>
        /// LE GESTE DU LANCEUR (10/10 -- "les animations, tout est nul") : le haricot jaillit bras
        /// au ciel (CharacterRig.PlayCast), un cercle magique a sa couleur s'ouvre sous ses pieds,
        /// des etincelles montent de ses mains. Une divine : un double cercle d'or qui tourne, une
        /// colonne de lumiere, un eclair au sol -- on sait QUI vient de le faire, de loin.
        /// </summary>
        static void Flourish(Seeker s, Ability a, Vector3 pos, Color tint)
        {
            if (s == null || s.Body == null || EchoCast.Echoing) return;
            bool divine = AbilityInfo.IsGod(a);
            CharacterRig rig = s.IsPlayer ? Game.Rig : null;
            if (!s.IsPlayer) { Rival r = Rival.Of(s); if (r != null) rig = r.Rig; }
            if (rig != null) rig.PlayCast(divine);
            Vector3 hands = pos + Vector3.up * 2.2f;
            Fx.GroundRing(pos, tint, divine ? 5f : 3f, divine ? 0.6f : 0.4f);
            Fx.Ring(pos + Vector3.up * 0.1f, Color.Lerp(tint, Color.white, 0.4f), 0.5f, divine ? 3.5f : 2.2f, 0.35f, 0.08f, Vector3.up);
            Fx.Burst(hands, tint, divine ? 50 : 24, divine ? 7f : 4.5f, 0.22f, 0.7f, -0.4f, Vector3.up, 35f);
            Fx.Sparks(hands, Color.white, divine ? 18 : 8, 3f);
            if (!divine) return;
            Color gold = new Color(1f, 0.82f, 0.36f);
            Fx.Ring(pos + Vector3.up * 0.15f, gold, 4.5f, 1.2f, 0.5f, 0.1f, Vector3.up);
            Fx.Column(pos, Color.Lerp(tint, gold, 0.5f), 24f, 0.35f, 1.1f);
            Fx.Flash(hands, tint, 14f, 4f, 0.25f);
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
        readonly Gradient gradient = new Gradient();
        readonly GradientColorKey[] colourKeys = new GradientColorKey[3];
        readonly GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
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
                Combat.Hit(s, push * 34f + Vector3.up * 10f, 0.35f, true, by);
                Fx.Impact(s.Body.position + Vector3.up * 1.1f, tint, 1.2f);
            }

            // Pres de toi : le vent hurle, l'ecran tremble.
            Transform me = Game.PlayerTransform;
            if (!heard && me != null && (me.position - front).magnitude < 18f)
            {
                heard = true;
                Sfx.WhooshAt(front);
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
                // (le milieu plus vif que les bouts ; un seul degrade, reutilise : avant,
                // trois degrades neufs par image, pour le ramasse-miettes)
                colourKeys[0] = new GradientColorKey(c, 0f);
                colourKeys[1] = new GradientColorKey(Color.white, 0.5f);
                colourKeys[2] = new GradientColorKey(c, 1f);
                alphaKeys[0] = new GradientAlphaKey(0f, 0f);
                alphaKeys[1] = new GradientAlphaKey(0.85f * fade, 0.5f);
                alphaKeys[2] = new GradientAlphaKey(0f, 1f);
                gradient.SetKeys(colourKeys, alphaKeys);
                l.colorGradient = gradient;
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
