using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CONTACT (27/09 : plus d'epee, plus de vie). Tout ce qui touche un joueur le
    /// PROJETTE : la poussee (clic droit), l'onde de choc, le souffle, le rayon d'un
    /// Oeil, une mine, un pendule de la tour. On ne meurt pas -- on perd sa place, et
    /// la Couronne si on la portait. C'est Smash dans une foret noire.
    ///
    /// Tout passe par Hit() : un seul endroit decide de ce qu'un coup fait (Ancrage,
    /// Prise ferme, etourdissement, Couronne qui tombe). En Phase 3, sur l'hote seulement.
    /// </summary>
    public static class Combat
    {
        public const float ShoveReach = 3f;
        public const float ShoveForce = 20f;

        /// <summary>
        /// POUSSER (clic droit) : le premier joueur devant soi, a 3 m, part en arriere
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
            // POUSSER LE PORTEUR, C'EST LUI VOLER LA COURONNE (27/09 -- Martin : "il se la
            // reprend en une demi-seconde"). Elle passe directement dans tes mains.
            bool stole = best.CarriesCrown && !best.Graced && Crown.TrySteal(by, best);
            if (stole) Aura.Moment(by, "COURONNE VOLÉE", Wings.Gold, 1f);
            // Un court etourdissement : on ne contre-marche pas une poussee (c'est ce
            // qui la rendait molle -- on reculait de deux metres en appuyant sur Z).
            Hit(best, push * force + Vector3.up * 7f, 0.25f, !stole, by);
            Fx.Impact(best.Body.position + Vector3.up * 1.1f, by.Colour, stole ? 1.4f : 0.7f);
            if (by.IsPlayer) { Stats.Shoves++; Hud.HitStop(0.05f); }
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
            m.Dive(target);
            Fx.Trail(by.Body, Wings.Gold, 1.4f, 1.3f);
            Fx.Ring(by.Body.position + Vector3.up * 1.2f, Wings.Gold, 0.5f, 5f, 0.35f, 0.3f, target.Body.position - by.Body.position);
            Fx.Burst(by.Body.position + Vector3.up * 1.2f, Wings.Gold, 50, 12f, 0.2f, 0.5f, 0f, by.Body.position - target.Body.position, 30f);
            Sfx.Whoosh();
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
            if (by == null || target == null || target.Body == null || by.Stunned || time <= 0f) { time = 0f; return true; }
            Vector3 to = target.Body.position + Vector3.up * 0.9f - (from + Vector3.up * 0.9f);
            if (to.magnitude < 2.4f)
            {
                DiveStrike(by, target);
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
            if (stole) Aura.Moment(by, "PIQUÉ D'AIGLE", Wings.Gold, 1.3f);
            Hit(target, dir * 24f + Vector3.up * 6f, 0.3f, !stole, by);
            Fx.Impact(target.Body.position + Vector3.up * 1.1f, Wings.Gold, stole ? 1.8f : 1f);
            Fx.Shock(target.Body.position + Vector3.up * 1.1f, Wings.Gold, 3f, 0.3f);
            if (by.IsPlayer) Hud.HitStop(0.08f);
        }

        /// <summary>
        /// UN COUP : "velocity" projette le joueur, "stun" l'etourdit (secondes), et s'il
        /// porte la Couronne et que "dropsCrown", il la lache -- sauf Prise ferme.
        /// </summary>
        public static void Hit(Seeker victim, Vector3 velocity, float stun, bool dropsCrown, Seeker by)
        {
            if (victim == null || victim.Body == null) return;
            // Protege (au depart, apres un respawn, juste apres un vol) : rien ne le touche.
            if (victim.Graced)
            {
                Fx.Sparks(victim.Body.position + Vector3.up * 1.1f, new Color(1f, 1f, 1f, 0.8f), 12, 3f);
                return;
            }
            if (victim.Has(Ability.Ancrage)) velocity = new Vector3(velocity.x * 0.5f, velocity.y * 0.7f, velocity.z * 0.5f);
            // UN OBSTACLE SUR LA TOUR TE RENVOIE EN BAS (29/09) : jete hors de la rampe,
            // ailes fermees jusqu'au sol. (Les coups des joueurs, eux, ne font que projeter.)
            if (by == null && Tower.On(victim.Body.position) && !Tower.Summit(victim.Body.position))
                velocity = Tumble(victim, velocity);
            Knockback(victim, velocity);
            victim.LastHurt = Time.time;
            if (by != null && by != victim) victim.LastHitBy = by;
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

        /// <summary>
        /// LA CHUTE : la poussee d'un obstacle, redressee pour sortir de la rampe (au moins
        /// 24 m/s vers l'exterieur de la tour, ca fait ~5 m avant que le frottement ne la
        /// mange : juste de quoi passer le bord), et les ailes fermees jusqu'au sol.
        /// </summary>
        static Vector3 Tumble(Seeker victim, Vector3 velocity)
        {
            Vector3 p = victim.Body.position;
            Vector3 outward = new Vector3(p.x, 0f, p.z);
            outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
            float along = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), outward);
            if (along < 24f) velocity += outward * (24f - along);
            if (velocity.y > 0.5f) velocity.y = Mathf.Clamp(velocity.y, 5f, 9f);
            victim.Tumble(6f);
            return velocity;
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
        public const float PadRadius = 6f;

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

        /// <summary>Ou l'on apparait : sur sa plateforme, un peu en arriere de l'arbaleste.</summary>
        public static Vector3 Of(int slot, Vector3 fallback)
        {
            Vector3 p;
            if (!Points.TryGetValue(slot, out p)) return fallback;
            Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
            return p + outward * 2.2f + Vector3.up * 0.05f;
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
                Proto.Cylinder(t, new Vector3(0f, 0.06f, 0f), new Vector3(PadRadius * 2f - 1.4f, 0.02f, PadRadius * 2f - 1.4f), new Color(0.2f, 0.19f, 0.2f), "Dalle");
                // Le fanion, derriere : on retrouve sa plateforme de loin.
                Proto.Cube(t, new Vector3(-2.6f, 2.6f, -3.8f), new Vector3(0.14f, 5.2f, 0.14f), new Color(0.25f, 0.2f, 0.16f), "Hampe");
                GameObject flag = Proto.Cube(t, new Vector3(-1.95f, 4.4f, -3.8f), new Vector3(1.3f, 0.9f, 0.05f), c, "Fanion");
                flag.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 0.9f);
                Proto.EndVisualOnly();
                LightBeam beam = LightBeam.Build(root.transform, at, c, 1.4f, 24f);
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
    /// protege trois secondes. Si l'on portait la Couronne, elle rentre au sommet.
    /// </summary>
    public static class Respawn
    {
        public const float Grace = 3f;

        public static void Of(Seeker s)
        {
            if (s == null || s.Body == null) return;
            if (s.CarriesCrown) Crown.BackToTop();
            // Pousse dans le vide il y a moins de six secondes : l'aura est pour qui l'a ejecte.
            if (s.LastHitBy != null && Time.time - s.LastHurt < 6f)
                Aura.Moment(s.LastHitBy, "ÉJECTÉ : " + s.Name.ToUpperInvariant(), s.LastHitBy.Colour, 0.8f);
            s.LastHitBy = null;
            Vector3 at = Spawns.Of(s.Index, Spawns.PadOf(s.Index)) + Vector3.up * 0.1f;
            float yaw = Spawns.YawOf(s.Index);
            if (s.IsPlayer && Game.Player != null) Game.Player.Teleport(at, yaw);
            else
            {
                Rival r = Rival.Of(s);
                if (r == null) return;
                r.Teleport(at);
                r.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            s.GraceUntil = Time.time + Grace;
            s.StunnedUntil = -1f;
            s.SlowUntil = -1f;
            Fx.Respawn(at, s.Colour);
            Feed.FellIntoClouds(s);
            if (s.IsPlayer && Game.Hud != null) Game.Hud.Flash(new Color(s.Colour.r, s.Colour.g, s.Colour.b, 0.6f));
        }
    }
}
