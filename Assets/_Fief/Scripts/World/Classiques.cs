using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CLASSIQUES (12/10, v36 -- Martin : "tu regardes dans tous les autres jeux qui existent
    /// sur Terre ce qui plait le plus comme capa, et tu les mets, en version FIEF, du chateau ; je
    /// veux une dinguerie"). Les capacites que tout le monde reconnait, refaites a notre facon :
    ///
    ///   Boulet bleu        la carapace bleue de Mario Kart : elle vole toute seule jusqu'au porteur
    ///   Mouton explosif    le mouton de Worms : il fonce en sautillant, puis BOUM
    ///   Sainte grenade     la Sainte Grenade de Worms : "Alleluia", puis une enorme explosion
    ///   Poing du faucon    le Falcon Punch de Smash : on charge, on frappe, il part dans le decor
    ///   Gobe-tout          Kirby : on aspire, on recrache
    ///   Roue folle         la roue de Junkrat (Overwatch) : elle fonce et explose
    ///   Charge du chevalier la charge de Reinhardt (Overwatch) : on emporte, on ecrase
    ///   Caisse de TNT      Minecraft : elle clignote, elle saute
    ///   La buche           Clash Royale : elle roule et renverse tout
    ///   Tonneau de haricots le tonneau de Clash Royale : il eclate, les haricots kamikazes sortent
    ///   Bombe disco        la Boogie Bomb de Fortnite : tout le monde danse
    ///   Force imparable    Malphite (League of Legends) : un saut, et tout decolle a l'arrivee
    ///   Saut sur la tete   (passive) Mario : on ecrase en retombant dessus
    ///   Home run           (passive) la batte de Smash : la 5e poussee envoie trois fois plus loin
    ///
    /// Tout passe par Combat (Hit, Blast, Afflict) : la Couronne tombe sur un coup, les protections
    /// et le reseau marchent comme pour les autres capacites. Chaque coup est annonce (Warnings) et
    /// les gros ont leur cible au sol (DivineFx.Mark).
    /// </summary>
    public static class Classiques
    {
        /// <summary>Les passives classiques, a tenir chaque image (le joueur, les bots).</summary>
        public static void KeepPassives(Seeker s)
        {
            if (s == null || s.Body == null) return;
            if (s.Has(Ability.SautMario)) MarioStomp.Keep(s);
        }

        /// <summary>Le point du sol vise, a "range" metres au plus (le sol devant, si on vise le ciel).</summary>
        public static Vector3 AimPoint(Seeker s, Vector3 eye, Vector3 aim, float range)
        {
            RaycastHit hit;
            Vector3 p;
            if (AbilityCaster.RayFrom(s, eye, aim, range, out hit)) p = hit.point;
            else
            {
                Vector3 f = new Vector3(aim.x, 0f, aim.z);
                f = f.sqrMagnitude > 0.01f ? f.normalized : s.Body.forward;
                p = s.Body.position + f * range;
            }
            Vector3 g = p;
            if (Divin.Ground(ref g, 6f, 40f)) p = g;
            return p;
        }

        /// <summary>Le sol sous "p" en suivant le terrain ; faux s'il n'y a que du vide.</summary>
        public static bool Follow(ref Vector3 p)
        {
            Vector3 g = p;
            if (!Divin.Ground(ref g, 3f, 5f)) return false;
            p.y = Mathf.Lerp(p.y, g.y, 0.5f);
            return true;
        }

        /// <summary>Un gros boum de capacite : l'onde (Blast), l'impact (DivineFx) et la secousse.</summary>
        public static void Boom(Vector3 at, float radius, float force, float up, Seeker by, Color c, float power)
        {
            Combat.Blast(at, radius, force, up, by);
            DivineFx.Impact(at, radius, c, power, power >= 1.5f);
        }

        public static Material Mat(Color c) { return MaterialFactory.GetShiny(c, 0.55f, 0.05f); }
    }

    // ====================================================================== un objet lance en cloche

    /// <summary>Un objet lance en cloche de "from" a "to" ; il tourne en l'air et appelle Land a l'arrivee.</summary>
    public class LobbedThing : MonoBehaviour
    {
        protected Seeker by;
        Vector3 from, to;
        float t, time, apex;
        bool landed;

        protected void Begin(Seeker who, Vector3 start, Vector3 end, float height)
        {
            by = who;
            from = start;
            to = end;
            apex = height;
            time = Mathf.Clamp(0.45f + (end - start).magnitude / 28f, 0.5f, 1.6f);
            transform.position = start;
        }

        /// <summary>Le temps de vol (pour poser la cible au sol a temps).</summary>
        protected float FlightTime { get { return time; } }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (landed) { Rest(dt); return; }
            t += dt;
            float k = Mathf.Clamp01(t / time);
            transform.position = Vector3.Lerp(from, to, k) + Vector3.up * apex * 4f * k * (1f - k);
            transform.Rotate(new Vector3(260f, 140f, 0f) * dt, Space.Self);
            Fly(dt);
            if (k >= 1f) { landed = true; Land(to); }
        }

        protected virtual void Fly(float dt) { }
        protected virtual void Land(Vector3 at) { Destroy(gameObject); }
        protected virtual void Rest(float dt) { }
    }

    // ====================================================================== le boulet bleu

    /// <summary>LE BOULET BLEU (la carapace bleue) : il monte, file au-dessus de tout, et pique sur le porteur.</summary>
    public class BlueShell : MonoBehaviour
    {
        const float Cruise = 34f;
        const float High = 26f;
        Seeker by;
        Seeker target;
        float age, trail;
        bool diving, marked;

        public static void Fire(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("BOULET BLEU");
            go.transform.position = at + Vector3.up * 2.5f;
            BlueShell b = go.AddComponent<BlueShell>();
            b.by = by;
            Color blue = AbilityInfo.Tint(Ability.BouletBleu);
            Proto.BeginVisualOnly();
            GameObject shell = Proto.Sphere(go.transform, Vector3.zero, new Vector3(1.8f, 1.1f, 1.8f), blue, "Carapace");
            shell.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(blue, 0.8f, 0.1f, 0.6f);
            GameObject rim = Proto.Cylinder(go.transform, new Vector3(0f, -0.2f, 0f), new Vector3(2.1f, 0.12f, 2.1f), Color.white, "Bord");
            rim.GetComponent<Renderer>().sharedMaterial = Classiques.Mat(Color.white);
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2f;
                GameObject spike = Proto.Cone(go.transform, new Vector3(Mathf.Cos(a) * 0.55f, 0.35f, Mathf.Sin(a) * 0.55f), 0.18f, 0.4f, Color.white, "Pique", 6);
                spike.GetComponent<Renderer>().sharedMaterial = Classiques.Mat(Color.white);
            }
            GameObject wingL = Proto.Sphere(go.transform, new Vector3(-1.1f, 0.2f, 0f), new Vector3(1.2f, 0.12f, 0.6f), Color.white, "Aile");
            GameObject wingR = Proto.Sphere(go.transform, new Vector3(1.1f, 0.2f, 0f), new Vector3(1.2f, 0.12f, 0.6f), Color.white, "Aile");
            wingL.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.7f, 0.9f, 1f), 1.4f);
            wingR.GetComponent<Renderer>().sharedMaterial = wingL.GetComponent<Renderer>().sharedMaterial;
            Proto.EndVisualOnly();
            Fx.Shock(at + Vector3.up, blue, 4f, 0.3f);
            Sfx.Boost(at, by != null && by.IsPlayer);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (target == null || target.Body == null || !diving && age % 0.5f < dt)
            {
                Seeker holder = Crown.Holder;
                target = holder != null && holder != by && holder.Body != null ? holder : target;
                if (target == null || target.Body == null) target = Divin.NearestOther(by, transform.position, 260f);
            }
            Vector3 p = transform.position;
            transform.Rotate(0f, 720f * dt, 0f, Space.Self);
            trail -= dt;
            if (trail <= 0f)
            {
                trail = 0.04f;
                Fx.Burst(p, AbilityInfo.Tint(Ability.BouletBleu), 4, 3f, 0.35f, 0.6f, 0f, Vector3.zero, 0f);
                Fx.Sparks(p, Color.white, 2, 2f);
            }
            if (target == null || target.Body == null || age > 14f) { Explode(p); return; }
            Vector3 aim = target.Body.position + Vector3.up;
            Vector3 flat = Combat.Flat(aim - p);
            if (!diving)
            {
                // Il monte au-dessus de tout, et file droit sur lui.
                float wantY = Mathf.Max(aim.y + High, 20f);
                p.y = Mathf.MoveTowards(p.y, wantY, 22f * dt);
                p += flat.normalized * Mathf.Min(flat.magnitude, Cruise * dt);
                if (flat.magnitude < 3f && p.y > aim.y + 4f)
                {
                    diving = true;
                    Sfx.Incoming(aim, 0.7f);
                }
                if (!marked && flat.magnitude < 40f)
                {
                    marked = true;
                    DivineFx.MarkIcon = "boulet-bleu";
                    DivineFx.MarkBy = by;
                    DivineFx.Mark(target.Body.position, 6f, AbilityInfo.Tint(Ability.BouletBleu), 2.2f, target.Body);
                }
            }
            else
            {
                p = Vector3.MoveTowards(p, aim, 46f * dt);
                if ((p - aim).magnitude < 1.4f) { transform.position = p; Explode(p); return; }
            }
            transform.position = p;
        }

        void Explode(Vector3 p)
        {
            Color blue = AbilityInfo.Tint(Ability.BouletBleu);
            Classiques.Boom(p, 7f, 34f, 18f, by, blue, 1.6f);
            Fx.Shock(p, Color.white, 9f, 0.5f);
            Fx.Column(p - Vector3.up * 2f, blue, 30f, 0.5f, 2f);
            Destroy(gameObject);
        }
    }

    // ====================================================================== le mouton

    /// <summary>LE MOUTON EXPLOSIF : il fonce en sautillant, fait demi-tour contre un mur, et saute au bout de 4 s.</summary>
    public class Sheep : MonoBehaviour
    {
        const float Life = 4f;
        Seeker by;
        Vector3 dir;
        float age, hop, bleat;
        bool marked;

        public static void Release(Seeker by, Vector3 at, Vector3 dir)
        {
            GameObject go = new GameObject("MOUTON");
            go.transform.position = at;
            Sheep s = go.AddComponent<Sheep>();
            s.by = by;
            s.bleat = 1.1f;
            s.dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            Color wool = AbilityInfo.Tint(Ability.Mouton);
            Color dark = new Color(0.15f, 0.13f, 0.14f);
            Proto.BeginVisualOnly();
            Transform t = go.transform;
            // La laine : des boules blanches serrees.
            for (int k = 0; k < 7; k++)
            {
                float a = k / 7f * Mathf.PI * 2f;
                Proto.Sphere(t, new Vector3(Mathf.Cos(a) * 0.32f, 0.85f + Mathf.Sin(a * 2f) * 0.08f, Mathf.Sin(a) * 0.42f), Vector3.one * 0.62f, wool, "Laine")
                    .GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(wool);
            }
            Proto.Sphere(t, new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 0.75f, 1.1f), wool, "Laine").GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(wool);
            Proto.Sphere(t, new Vector3(0f, 1.05f, 0.68f), new Vector3(0.42f, 0.46f, 0.5f), dark, "Tete");
            Proto.Sphere(t, new Vector3(-0.13f, 1.15f, 0.9f), new Vector3(0.16f, 0.18f, 0.08f), Color.white, "Oeil");
            Proto.Sphere(t, new Vector3(0.13f, 1.15f, 0.9f), new Vector3(0.16f, 0.18f, 0.08f), Color.white, "Oeil");
            Proto.Sphere(t, new Vector3(-0.24f, 1.18f, 0.62f), new Vector3(0.24f, 0.08f, 0.14f), dark, "Oreille");
            Proto.Sphere(t, new Vector3(0.24f, 1.18f, 0.62f), new Vector3(0.24f, 0.08f, 0.14f), dark, "Oreille");
            for (int k = 0; k < 4; k++)
                Proto.Cylinder(t, new Vector3(k < 2 ? -0.22f : 0.22f, 0.25f, k % 2 == 0 ? -0.3f : 0.3f), new Vector3(0.12f, 0.25f, 0.12f), dark, "Patte");
            GameObject fuse = Proto.Sphere(t, new Vector3(0f, 1.5f, -0.2f), Vector3.one * 0.16f, new Color(1f, 0.5f, 0.1f), "Meche");
            fuse.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.5f, 0.1f), 2.2f);
            Proto.EndVisualOnly();
            Sfx.Sheep(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            // Un mur devant : demi-tour (un peu de biais), comme le vrai.
            RaycastHit wall;
            if (Physics.Raycast(p + Vector3.up * 0.8f, dir, out wall, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                dir = Quaternion.Euler(0f, 160f + Random.Range(-30f, 30f), 0f) * dir;
            p += dir * 10f * dt;
            if (!Classiques.Follow(ref p)) p.y -= 14f * dt;
            hop += dt * 9f;
            transform.position = p + Vector3.up * Mathf.Abs(Mathf.Sin(hop)) * 0.7f;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(Mathf.Sin(hop) * 10f, 0f, 0f);
            bleat -= dt;
            if (bleat <= 0f) { bleat = 1.1f; Sfx.Sheep(p); }
            if (!marked && age > Life - 1.2f)
            {
                marked = true;
                DivineFx.MarkIcon = "mouton";
                DivineFx.MarkBy = by;
                DivineFx.Mark(p, 7f, new Color(1f, 0.45f, 0.2f), Life - age, transform);
            }
            // Il saute sur le premier qui s'approche.
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null || o.Graced || age < 0.5f) continue;
                if ((o.Body.position - p).magnitude < 1.6f) { Explode(); return; }
            }
            if (age > Life || p.y < -40f) Explode();
        }

        void Explode()
        {
            Vector3 p = transform.position;
            Classiques.Boom(p, 7f, 30f, 15f, by, new Color(1f, 0.55f, 0.25f), 1.1f);
            // De la laine partout.
            Fx.Burst(p + Vector3.up, Color.white, 80, 12f, 0.5f, 1.2f, 0.3f, Vector3.zero, 0f);
            Destroy(gameObject);
        }
    }

    // ====================================================================== la sainte grenade

    /// <summary>LA SAINTE GRENADE : elle tombe, le ciel s'ouvre, le choeur chante... BOUM, 12 m.</summary>
    public class HolyGrenade : LobbedThing
    {
        const float Radius = 12f;
        const float Hymn = 1.7f;
        float rest, glow;

        public static void Throw(Seeker by, Vector3 from, Vector3 to)
        {
            GameObject go = new GameObject("SAINTE GRENADE");
            HolyGrenade g = go.AddComponent<HolyGrenade>();
            Color gold = AbilityInfo.Tint(Ability.SainteGrenade);
            Proto.BeginVisualOnly();
            GameObject body = Proto.Sphere(go.transform, Vector3.zero, new Vector3(0.7f, 0.8f, 0.7f), gold, "Grenade");
            body.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(gold, 0.85f, 0.8f, 0.4f);
            GameObject cross = Proto.Cube(go.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 0.4f, 0.1f), gold, "Croix");
            cross.GetComponent<Renderer>().sharedMaterial = body.GetComponent<Renderer>().sharedMaterial;
            Proto.Cube(go.transform, new Vector3(0f, 0.66f, 0f), new Vector3(0.3f, 0.1f, 0.1f), gold, "Croix").GetComponent<Renderer>().sharedMaterial = body.GetComponent<Renderer>().sharedMaterial;
            Proto.EndVisualOnly();
            g.Begin(by, from, to, 5f);
            DivineFx.Mark(to, Radius, gold, g.FlightTime + Hymn);
            Sfx.WhooshAt(from);
        }

        protected override void Fly(float dt)
        {
            Fx.Sparks(transform.position, AbilityInfo.Tint(Ability.SainteGrenade), 2, 1.5f);
        }

        protected override void Land(Vector3 at)
        {
            transform.rotation = Quaternion.identity;
            Color gold = AbilityInfo.Tint(Ability.SainteGrenade);
            Fx.Column(at, new Color(1f, 0.95f, 0.7f), 80f, Hymn, 2.2f);
            DivineFx.Halo(transform, gold, Hymn, 1.6f);
            Sfx.Choir(at);
        }

        protected override void Rest(float dt)
        {
            rest += dt;
            glow -= dt;
            if (glow <= 0f)
            {
                glow = 0.12f;
                Fx.Flash(transform.position + Vector3.up, AbilityInfo.Tint(Ability.SainteGrenade), 14f, 2f + rest * 3f, 0.15f);
                Fx.Burst(transform.position + Vector3.up * 0.5f, Color.white, 6, 3f, 0.25f, 0.6f, -0.3f, Vector3.up, 30f);
            }
            if (rest < Hymn) return;
            Vector3 p = transform.position;
            Color gold = AbilityInfo.Tint(Ability.SainteGrenade);
            Classiques.Boom(p, Radius, 40f, 18f, by, gold, 2.2f);
            Fx.Confetti(p + Vector3.up, new[] { gold, Color.white, new Color(1f, 0.95f, 0.7f) }, 80, 16f, Vector3.up, 70f);
            Destroy(gameObject);
        }
    }

    // ====================================================================== le poing du faucon

    /// <summary>LE POING DU FAUCON : une demi-seconde de flammes qui montent, puis le coup qui part.</summary>
    public class FalconPunch : MonoBehaviour
    {
        const float Windup = 0.55f;
        Seeker by;
        Vector3 dir;
        float age, flare;

        public static void Begin(Seeker by, Vector3 dir)
        {
            FalconPunch f = by.Body.gameObject.AddComponent<FalconPunch>();
            f.by = by;
            f.dir = dir;
            Sfx.Inhale(by.Body.position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (by == null || by.Body == null || by.Stunned) { Destroy(this); return; }
            age += dt;
            Vector3 fist = by.Body.position + Vector3.up * 1.2f + dir * 0.8f;
            Color fire = AbilityInfo.Tint(Ability.PoingFaucon);
            flare -= dt;
            if (flare <= 0f)
            {
                flare = 0.03f;
                Fx.Burst(fist, fire, 6, 3f, 0.3f, 0.35f, -0.6f, Vector3.up, 40f);
            }
            if (age < Windup) return;
            // LE COUP : un bond en avant, et le premier devant part dans le decor.
            IMover m = AbilityCaster.MoverOf(by);
            if (m != null) m.Dash(dir, 30f, 0.18f);
            Seeker best = null;
            float bestD = 6f;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null) continue;
                Vector3 rel = o.Body.position - by.Body.position;
                if (Mathf.Abs(rel.y) > 2.5f) continue;
                float along = Vector3.Dot(Combat.Flat(rel), dir);
                if (along < -0.5f || along > 6f || Vector3.Angle(Combat.Flat(rel), dir) > 55f && Combat.Flat(rel).magnitude > 1.5f) continue;
                if (along < bestD) { bestD = along; best = o; }
            }
            Fx.Burst(fist, fire, 80, 18f, 0.35f, 0.5f, 0f, dir, 25f);
            Fx.Ring(fist + dir * 1.5f, fire, 0.4f, 5f, 0.3f, 0.35f, dir);
            if (best != null)
            {
                Combat.Hit(best, dir * 48f + Vector3.up * 14f, 0.6f, true, by);
                DivineFx.Impact(best.Body.position, 4f, fire, 1.2f, true);
                Fx.Trail(best.Body, fire, 1.2f, 1.4f);
                Sfx.BigPush(best.Body.position, by.IsPlayer || best.IsPlayer);
                if (by.IsPlayer || best.IsPlayer) Hud.HitStop(0.16f);
            }
            else Sfx.WhooshAt(fist);
            Destroy(this);
        }
    }

    // ====================================================================== le gobe-tout

    /// <summary>LE GOBE-TOUT (Kirby) : on aspire le joueur devant soi, on le garde une demi-seconde, on le recrache.</summary>
    public class Inhale : MonoBehaviour
    {
        Seeker by;
        Seeker victim;
        Vector3 dir;
        float age, swirl;

        public static void Begin(Seeker by, Seeker victim, Vector3 dir)
        {
            Inhale h = by.Body.gameObject.AddComponent<Inhale>();
            h.by = by;
            h.victim = victim;
            h.dir = dir;
            IMover m = AbilityCaster.MoverOf(victim);
            if (m != null) m.PullTo(by.Body.position + dir * 1.6f + Vector3.up * 0.5f, 26f);
            Sfx.Inhale(by.Body.position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (by == null || by.Body == null) { Destroy(this); return; }
            age += dt;
            Color pink = AbilityInfo.Tint(Ability.GobeTout);
            Vector3 mouth = by.Body.position + Vector3.up * 1.2f + dir * 0.8f;
            swirl -= dt;
            if (swirl <= 0f && age < 0.7f)
            {
                swirl = 0.06f;
                Fx.Ring(mouth + dir * 4f, pink, 3f, 0.3f, 0.25f, 0.18f, dir);
                Fx.Burst(mouth + dir * 6f, Color.white, 6, -8f, 0.2f, 0.5f, 0f, -dir, 30f);
            }
            if (age < 0.75f) return;
            // On recrache : il part comme une etoile.
            if (victim != null && victim.Body != null)
            {
                Combat.Hit(victim, dir * 36f + Vector3.up * 12f, 0.5f, true, by);
                Fx.Burst(victim.Body.position + Vector3.up, new Color(1f, 0.95f, 0.5f), 50, 12f, 0.3f, 0.6f, 0f, dir, 30f);
                Fx.Trail(victim.Body, new Color(1f, 0.95f, 0.5f), 1f, 1.1f);
            }
            Fx.Shock(mouth, pink, 3f, 0.3f);
            Sfx.PopAt(mouth);
            Destroy(this);
        }
    }

    // ====================================================================== la roue folle

    /// <summary>LA ROUE FOLLE (Junkrat) : une roue en feu, cloutee, qui fonce et explose au contact.</summary>
    public class RipTire : MonoBehaviour
    {
        const float Speed = 24f;
        Seeker by;
        Vector3 dir;
        float age, fire;
        Transform wheel;

        public static void Roll(Seeker by, Vector3 at, Vector3 dir)
        {
            GameObject go = new GameObject("ROUE FOLLE");
            go.transform.position = at;
            RipTire r = go.AddComponent<RipTire>();
            r.by = by;
            r.dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(r.dir);
            Proto.BeginVisualOnly();
            GameObject w = new GameObject("Roue");
            w.transform.SetParent(go.transform, false);
            w.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            r.wheel = w.transform;
            Color dark = new Color(0.18f, 0.16f, 0.17f);
            GameObject tyre = Proto.Cylinder(w.transform, Vector3.zero, new Vector3(2.2f, 0.35f, 2.2f), dark, "Pneu");
            tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject hub = Proto.Cylinder(w.transform, Vector3.zero, new Vector3(0.9f, 0.4f, 0.9f), AbilityInfo.Tint(Ability.RoueFolle), "Moyeu");
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hub.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(AbilityInfo.Tint(Ability.RoueFolle), 1.8f);
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f;
                GameObject stud = Proto.Sphere(w.transform, new Vector3(0f, Mathf.Cos(a) * 1.12f, Mathf.Sin(a) * 1.12f), Vector3.one * 0.26f, Color.white, "Clou");
                stud.GetComponent<Renderer>().sharedMaterial = Classiques.Mat(new Color(0.8f, 0.8f, 0.85f));
            }
            Proto.EndVisualOnly();
            Sfx.RumbleAt(at, 0.8f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            RaycastHit wall;
            if (Physics.Raycast(p + Vector3.up * 1.1f, dir, out wall, 1.4f, ~0, QueryTriggerInteraction.Ignore)) { Explode(); return; }
            p += dir * Speed * dt;
            if (!Classiques.Follow(ref p)) p.y -= 16f * dt;
            transform.position = p;
            if (wheel != null) wheel.Rotate(Speed * dt / 1.1f * Mathf.Rad2Deg, 0f, 0f, Space.Self);
            fire -= dt;
            if (fire <= 0f)
            {
                fire = 0.03f;
                Fx.Burst(p + Vector3.up * 1.1f, AbilityInfo.Tint(Ability.RoueFolle), 5, 3f, 0.4f, 0.5f, -0.4f, Vector3.up - dir, 40f);
                Fx.Burst(p + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 2, 3f, 0.4f, 0.6f, 0.2f, Vector3.up, 60f);
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null || o.Graced) continue;
                if ((o.Body.position + Vector3.up - (p + Vector3.up)).magnitude < 2f) { Explode(); return; }
            }
            if (age > 2.6f || p.y < -40f) Explode();
        }

        void Explode()
        {
            Classiques.Boom(transform.position + Vector3.up, 8f, 34f, 16f, by, AbilityInfo.Tint(Ability.RoueFolle), 1.4f);
            Destroy(gameObject);
        }
    }

    // ====================================================================== la charge du chevalier

    /// <summary>LA CHARGE DU CHEVALIER (Reinhardt) : on fonce ; le premier touche est emporte, puis ecrase au bout.</summary>
    public class KnightCharge : MonoBehaviour
    {
        const float Length = 1.05f;
        Seeker by;
        Seeker carried;
        Vector3 dir;
        float age, step;
        readonly List<Seeker> bumped = new List<Seeker>();

        public static void Begin(Seeker by, Vector3 dir)
        {
            KnightCharge c = by.Body.gameObject.AddComponent<KnightCharge>();
            c.by = by;
            c.dir = dir;
            IMover m = AbilityCaster.MoverOf(by);
            if (m != null) m.Dash(dir, 24f, Length);
            Sfx.WhooshAt(by.Body.position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (by == null || by.Body == null) { Destroy(this); return; }
            age += dt;
            Vector3 p = by.Body.position;
            Color steel = AbilityInfo.Tint(Ability.Charge);
            step -= dt;
            if (step <= 0f)
            {
                step = 0.12f;
                Fx.Ring(p + Vector3.up * 1.1f + dir * 1.2f, steel, 1.4f, 1.8f, 0.15f, 0.25f, dir);
                Fx.Burst(p + Vector3.up * 0.1f, new Color(0.7f, 0.62f, 0.52f), 12, 5f, 0.45f, 0.7f, 0.3f, Vector3.up - dir, 50f);
                Sfx.ThudAt(p);
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null || o == carried || bumped.Contains(o)) continue;
                Vector3 rel = o.Body.position - p;
                if (Mathf.Abs(rel.y) > 2.2f || Combat.Flat(rel).magnitude > 2.4f || Vector3.Dot(Combat.Flat(rel), dir) < -0.5f) continue;
                if (carried == null)
                {
                    // Le premier : EMPORTE devant le chevalier (il vole avec lui).
                    carried = o;
                    Combat.Hit(o, dir * 25f + Vector3.up * 3f, Length - age + 0.3f, true, by);
                    Fx.Impact(o.Body.position + Vector3.up, steel, 1.4f);
                    Sfx.PunchAt(o.Body.position);
                }
                else
                {
                    // Les autres : bouscules sur le cote.
                    bumped.Add(o);
                    Vector3 side = Vector3.Cross(Vector3.up, dir) * (Vector3.Dot(rel, Vector3.Cross(Vector3.up, dir)) >= 0f ? 1f : -1f);
                    Combat.Hit(o, side * 18f + Vector3.up * 8f, 0.3f, true, by);
                }
            }
            if (age < Length) return;
            // L'ECRASEMENT : au bout de la charge, contre le mur ou le sol.
            if (carried != null && carried.Body != null && (carried.Body.position - p).magnitude < 7f)
            {
                Combat.Hit(carried, dir * 30f + Vector3.up * 16f, 0.6f, true, by);
                DivineFx.Impact(carried.Body.position, 4f, steel, 1.2f, true);
                if (by.IsPlayer || carried.IsPlayer) Hud.HitStop(0.12f);
            }
            Destroy(this);
        }
    }

    // ====================================================================== la caisse de TNT

    /// <summary>LA CAISSE DE TNT : posee devant toi ; elle clignote 3 s en sifflant, puis saute (10 m).</summary>
    public class TntCrate : MonoBehaviour
    {
        const float Fuse = 3f;
        const float Radius = 10f;
        Seeker by;
        float age, blink, hiss;
        Renderer body;
        Material red, white;
        bool lit;

        public static void Place(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("TNT");
            go.transform.position = at;
            TntCrate t = go.AddComponent<TntCrate>();
            t.by = by;
            Color r = AbilityInfo.Tint(Ability.Tnt);
            Proto.BeginVisualOnly();
            GameObject box = Proto.Cube(go.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.5f, 1.5f, 1.5f), r, "Caisse");
            t.body = box.GetComponent<Renderer>();
            t.red = MaterialFactory.GetShiny(r, 0.4f, 0f);
            t.white = MaterialFactory.GetGlow(Color.white, 2.5f);
            t.body.sharedMaterial = t.red;
            Color band = new Color(0.95f, 0.92f, 0.85f);
            Proto.Cube(go.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.54f, 0.42f, 1.54f), band, "Bande");
            Proto.Cube(go.transform, new Vector3(0f, 0.75f, 0.78f), new Vector3(0.9f, 0.22f, 0.02f), new Color(0.1f, 0.1f, 0.1f), "Marque");
            Proto.Cube(go.transform, new Vector3(0f, 0.75f, -0.78f), new Vector3(0.9f, 0.22f, 0.02f), new Color(0.1f, 0.1f, 0.1f), "Marque");
            Proto.Cylinder(go.transform, new Vector3(0f, 1.65f, 0f), new Vector3(0.08f, 0.2f, 0.08f), new Color(0.3f, 0.25f, 0.2f), "Meche");
            Proto.EndVisualOnly();
            DivineFx.Mark(at, Radius, r, Fuse);
            Sfx.BuildAt(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 top = transform.position + Vector3.up * 1.85f;
            blink -= dt;
            if (blink <= 0f)
            {
                blink = Mathf.Lerp(0.35f, 0.07f, age / Fuse);
                lit = !lit;
                if (body != null) body.sharedMaterial = lit ? white : red;
            }
            hiss -= dt;
            if (hiss <= 0f) { hiss = 0.5f; Sfx.Fuse(top); }
            Fx.Sparks(top, new Color(1f, 0.7f, 0.3f), 2, 2.5f);
            transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(age * 30f) * (age / Fuse));
            if (age < Fuse) return;
            Vector3 p = transform.position + Vector3.up * 0.8f;
            Classiques.Boom(p, Radius, 38f, 18f, by, new Color(1f, 0.5f, 0.2f), 1.8f);
            Destroy(gameObject);
        }
    }

    // ====================================================================== la buche

    /// <summary>LA BUCHE (Clash Royale) : un tronc geant qui roule 40 m et renverse tout ce qu'il croise.</summary>
    public class GiantLog : MonoBehaviour
    {
        const float Speed = 16f;
        const float HalfWidth = 4.5f;
        Seeker by;
        Vector3 dir;
        float age, dust, roll;
        Transform log;
        readonly List<Seeker> hit = new List<Seeker>();

        public static void Roll(Seeker by, Vector3 at, Vector3 dir)
        {
            GameObject go = new GameObject("BUCHE");
            go.transform.position = at;
            GiantLog g = go.AddComponent<GiantLog>();
            g.by = by;
            g.dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(g.dir);
            Color bark = AbilityInfo.Tint(Ability.Buche);
            Color wood = new Color(0.92f, 0.78f, 0.55f);
            Proto.BeginVisualOnly();
            GameObject l = new GameObject("Tronc");
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            g.log = l.transform;
            GameObject trunk = Proto.Cylinder(l.transform, Vector3.zero, new Vector3(2.6f, HalfWidth, 2.6f), bark, "Ecorce");
            trunk.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject end = Proto.Cylinder(l.transform, new Vector3(s * HalfWidth, 0f, 0f), new Vector3(2.4f, 0.04f, 2.4f), wood, "Coeur");
                end.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            for (int k = 0; k < 5; k++)
            {
                float a = k / 5f * Mathf.PI * 2f;
                Proto.Cube(l.transform, new Vector3(-3f + k * 1.5f, Mathf.Cos(a) * 1.3f, Mathf.Sin(a) * 1.3f), new Vector3(0.6f, 0.2f, 0.2f), new Color(0.45f, 0.32f, 0.2f), "Noeud");
            }
            Proto.EndVisualOnly();
            Sfx.RumbleAt(at, 1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position + dir * Speed * dt;
            if (!Classiques.Follow(ref p)) p.y -= 18f * dt;
            transform.position = p;
            roll += Speed * dt / 1.3f * Mathf.Rad2Deg;
            if (log != null) log.localRotation = Quaternion.Euler(roll, 0f, 0f);
            dust -= dt;
            if (dust <= 0f)
            {
                dust = 0.05f;
                Vector3 side = Vector3.Cross(Vector3.up, dir);
                Fx.Burst(p + side * Random.Range(-HalfWidth, HalfWidth) + Vector3.up * 0.2f, new Color(0.7f, 0.6f, 0.45f), 4, 4f, 0.5f, 0.7f, 0.2f, Vector3.up - dir, 50f);
            }
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null || hit.Contains(o)) continue;
                Vector3 rel = o.Body.position - p;
                if (Mathf.Abs(rel.y) > 3f || Mathf.Abs(Vector3.Dot(rel, right)) > HalfWidth + 0.5f || Mathf.Abs(Vector3.Dot(Combat.Flat(rel), dir)) > 1.8f) continue;
                hit.Add(o);
                Combat.Hit(o, dir * 24f + Vector3.up * 11f, 0.4f, true, by);
                Fx.Impact(o.Body.position + Vector3.up, AbilityInfo.Tint(Ability.Buche), 1.2f);
                Sfx.ThudAt(o.Body.position);
            }
            if (age > 2.6f || p.y < -40f)
            {
                Fx.Burst(p + Vector3.up, new Color(0.7f, 0.5f, 0.3f), 60, 10f, 0.4f, 0.9f, 0.4f, Vector3.zero, 0f);
                DivineFx.Debris(p + Vector3.up, 14, AbilityInfo.Tint(Ability.Buche), 9f);
                Sfx.CrashAt(p);
                Destroy(gameObject);
            }
        }
    }

    // ====================================================================== le tonneau de haricots

    /// <summary>LE TONNEAU DE HARICOTS : il vole, eclate, et quatre petits haricots kamikazes en sortent.</summary>
    public class BeanBarrel : LobbedThing
    {
        public static void Throw(Seeker by, Vector3 from, Vector3 to)
        {
            GameObject go = new GameObject("TONNEAU");
            BeanBarrel b = go.AddComponent<BeanBarrel>();
            Color wood = new Color(0.62f, 0.42f, 0.24f);
            Proto.BeginVisualOnly();
            Proto.Cylinder(go.transform, Vector3.zero, new Vector3(1.2f, 0.75f, 1.2f), wood, "Tonneau");
            Color hoop = new Color(0.35f, 0.33f, 0.35f);
            Proto.Cylinder(go.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.26f, 0.08f, 1.26f), hoop, "Cercle");
            Proto.Cylinder(go.transform, new Vector3(0f, -0.45f, 0f), new Vector3(1.26f, 0.08f, 1.26f), hoop, "Cercle");
            Proto.EndVisualOnly();
            b.Begin(by, from, to, 6f);
            DivineFx.Mark(to, 6f, AbilityInfo.Tint(Ability.Tonneau), b.FlightTime);
            Sfx.WhooshAt(from);
        }

        protected override void Land(Vector3 at)
        {
            DivineFx.Debris(at + Vector3.up * 0.5f, 16, new Color(0.62f, 0.42f, 0.24f), 9f);
            Fx.Burst(at + Vector3.up, new Color(0.62f, 0.42f, 0.24f), 40, 8f, 0.4f, 0.8f, 0.4f, Vector3.zero, 0f);
            Combat.Blast(at, 4f, 16f, 8f, by);
            Sfx.CrashAt(at);
            Sfx.BoingAt(at);
            for (int k = 0; k < 4; k++)
            {
                Vector3 d = Quaternion.Euler(0f, k * 90f + 45f, 0f) * Vector3.forward;
                BeanBomber.Release(by, at + d * 1.2f, d);
            }
            Destroy(gameObject);
        }
    }

    // ====================================================================== la bombe disco

    /// <summary>LA BOMBE DISCO (Fortnite) : une boule a facettes eclate ; tous ceux a 9 m dansent 3 s.</summary>
    public class DiscoBomb : LobbedThing
    {
        const float Radius = 9f;
        const float Party = 3f;
        float rest, lights, restY;
        int colour;
        static readonly Color[] Lights = { new Color(1f, 0.3f, 0.6f), new Color(0.3f, 0.8f, 1f), new Color(1f, 0.9f, 0.3f), new Color(0.5f, 1f, 0.5f), new Color(0.8f, 0.4f, 1f) };

        public static void Throw(Seeker by, Vector3 from, Vector3 to)
        {
            GameObject go = new GameObject("BOMBE DISCO");
            DiscoBomb d = go.AddComponent<DiscoBomb>();
            Proto.BeginVisualOnly();
            GameObject ball = Proto.Sphere(go.transform, Vector3.zero, Vector3.one * 1.1f, Color.white, "Boule a facettes");
            ball.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(new Color(0.85f, 0.85f, 0.95f), 0.95f, 0.9f, 0.3f);
            for (int k = 0; k < 10; k++)
            {
                Vector3 n = Random.onUnitSphere * 0.52f;
                GameObject f = Proto.Cube(go.transform, n, Vector3.one * 0.18f, Color.white, "Facette");
                f.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Lights[k % Lights.Length], 1.6f);
            }
            Proto.EndVisualOnly();
            d.Begin(by, from, to, 5f);
            DivineFx.Mark(to, Radius, AbilityInfo.Tint(Ability.Disco), d.FlightTime);
            Sfx.WhooshAt(from);
        }

        protected override void Land(Vector3 at)
        {
            restY = at.y + 3f;
            Sfx.Disco(at);
            Fx.Shock(at + Vector3.up, AbilityInfo.Tint(Ability.Disco), Radius, 0.5f);
            Fx.Confetti(at + Vector3.up, Lights, 90, 12f, Vector3.up, 80f);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null) continue;
                Vector3 d = o.Body.position - at;
                if (Mathf.Abs(d.y) > 4f || Combat.Flat(d).magnitude > Radius) continue;
                Combat.Afflict(o, Combat.Affliction.Dance, Party, by);
            }
        }

        protected override void Rest(float dt)
        {
            rest += dt;
            Vector3 bp = transform.position;
            bp.y = Mathf.MoveTowards(bp.y, restY, 4f * dt);
            transform.position = bp;
            transform.Rotate(0f, 240f * dt, 0f, Space.World);
            lights -= dt;
            if (lights <= 0f)
            {
                lights = 0.25f;
                colour = (colour + 1) % Lights.Length;
                Fx.Flash(transform.position, Lights[colour], 18f, 5f, 0.25f);
                Fx.Ring(transform.position - Vector3.up * 2.5f, Lights[colour], 1f, Radius, 0.3f, 0.3f, Vector3.up);
            }
            if (rest < Party) return;
            Fx.Burst(transform.position, Color.white, 60, 10f, 0.25f, 0.6f, 0.3f, Vector3.zero, 0f);
            Sfx.PopAt(transform.position);
            Destroy(gameObject);
        }
    }

    // ====================================================================== la force imparable

    /// <summary>LA FORCE IMPARABLE (Malphite) : un bond jusqu'ou l'on vise, et a l'arrivee tout le monde decolle.</summary>
    public class Unstoppable : MonoBehaviour
    {
        const float Radius = 8f;
        Seeker by;
        Vector3 to;
        float age, maxTime;

        public static void Leap(Seeker by, Vector3 to)
        {
            Vector3 from = by.Body.position;
            Vector3 v = Ballista.Lob(from, to, 7f);
            IMover m = AbilityCaster.MoverOf(by);
            if (m == null) return;
            m.Launch(v);
            Unstoppable u = by.Body.gameObject.AddComponent<Unstoppable>();
            u.by = by;
            u.to = to;
            float flat = Combat.Flat(to - from).magnitude;
            u.maxTime = Mathf.Clamp(flat / Mathf.Max(1f, Combat.Flat(v).magnitude) + 0.6f, 0.8f, 3f);
            DivineFx.Mark(to, Radius, AbilityInfo.Tint(Ability.ForceImparable), u.maxTime - 0.5f);
            Fx.Burst(from, new Color(0.7f, 0.62f, 0.52f), 40, 8f, 0.45f, 0.8f, 0.3f, Vector3.up, 60f);
            Sfx.WhooshAt(from);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (by == null || by.Body == null) { Destroy(this); return; }
            age += dt;
            Vector3 p = by.Body.position;
            Fx.Burst(p + Vector3.up, AbilityInfo.Tint(Ability.ForceImparable), 3, 2f, 0.4f, 0.5f, 0f, Vector3.zero, 0f);
            bool there = age > 0.35f && Combat.Flat(p - to).magnitude < 2.5f && Mathf.Abs(p.y - to.y) < 2.5f;
            if (!there && age < maxTime) return;
            // L'ARRIVEE : tout le monde autour decolle.
            Color c = AbilityInfo.Tint(Ability.ForceImparable);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null) continue;
                Vector3 d = o.Body.position - p;
                if (Mathf.Abs(d.y) > 4f || Combat.Flat(d).magnitude > Radius) continue;
                Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                Combat.Hit(o, away * 12f + Vector3.up * 24f, 0.6f, true, by);
            }
            DivineFx.Impact(p, Radius, c, 1.5f, true);
            DivineFx.Debris(p + Vector3.up * 0.5f, 18, c, 12f);
            Destroy(this);
        }
    }

    // ====================================================================== les passives

    /// <summary>LE SAUT SUR LA TETE (Mario) : retomber sur quelqu'un l'ecrase une seconde, et on rebondit haut.</summary>
    public class MarioStomp : MonoBehaviour
    {
        Seeker who;
        Vector3 last;
        float ready;

        public static void Keep(Seeker s)
        {
            if (s.Body.GetComponent<MarioStomp>() != null) return;
            MarioStomp m = s.Body.gameObject.AddComponent<MarioStomp>();
            m.who = s;
            m.last = s.Body.position;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (who == null || who.Body == null || !who.Has(Ability.SautMario)) { Destroy(this); return; }
            Vector3 p = who.Body.position;
            float vy = (p.y - last.y) / dt;
            last = p;
            if (Time.time < ready || vy > -3f) return;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == who || o.Body == null || o.Graced) continue;
                Vector3 head = o.Body.position + Vector3.up * 1.9f;
                if (Combat.Flat(p - head).magnitude > 1.2f || p.y < head.y - 0.6f || p.y > head.y + 0.9f) continue;
                ready = Time.time + 0.6f;
                // ECRASE : il s'aplatit une seconde (le haricot s'ecrase, des etoiles).
                Combat.Hit(o, Vector3.down * 4f, 1f, true, who);
                CharacterRig rig = CharacterRig.Of(o);
                if (rig != null) rig.PlayHit(Vector3.down * 30f, 1.5f);
                IMover m = AbilityCaster.MoverOf(who);
                if (m != null) m.Push(Vector3.up * 16f);
                Color c = AbilityInfo.Tint(Ability.SautMario);
                Fx.Ring(head, c, 0.4f, 2.6f, 0.25f, 0.25f, Vector3.up);
                Fx.Burst(head, new Color(1f, 0.95f, 0.5f), 30, 6f, 0.25f, 0.5f, 0.3f, Vector3.zero, 0f);
                Sfx.BoingAt(head);
                Sfx.PopAt(head);
                return;
            }
        }
    }
}
