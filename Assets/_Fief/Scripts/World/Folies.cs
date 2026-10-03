using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA DEUXIEME FOURNEE DE CAPACITES DE FOU (07/10 -- Martin : "fais encore plus de capas,
    /// plus, plus, plus") : ce qui vit quelques secondes dans le monde.
    ///
    ///   le GEYSER sous les pieds de la cible, le MISSILE qui poursuit, la TRAINEE DE FEU, le
    ///   POGO, le BOOMERANG, le PIEGE A LOUP, la BATAILLE D'OREILLERS, le RAZ-DE-MAREE.
    ///
    /// Tout ce qui frappe passe par Combat.Hit (ou Combat.Afflict pour le piege) : les
    /// protections, le vol de la Couronne et le jeu en ligne s'appliquent comme pour le reste.
    /// </summary>
    public class Geyser : MonoBehaviour
    {
        const float Warn = 0.6f;
        const float Radius = 2.8f;
        Seeker by;
        float age, spray;

        public static void Erupt(Seeker by, Vector3 at)
        {
            RaycastHit hit;
            if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) at = hit.point;
            GameObject go = new GameObject("GEYSER");
            go.transform.position = at;
            Geyser g = go.AddComponent<Geyser>();
            g.by = by;
            Sfx.ChipAt(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            Color c = AbilityInfo.Tint(Ability.Geyser);
            spray -= dt;
            if (age < Warn)
            {
                if (spray <= 0f) { spray = 0.08f; Fx.Burst(p, c, 8, 3f, 0.15f, 0.4f, 0.4f, Vector3.up, 30f); Fx.GroundRing(p, c, Radius, 0.12f); }
                return;
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                Vector3 d = s.Body.position - p;
                if (Mathf.Abs(d.y) > 3f || new Vector2(d.x, d.z).magnitude > Radius) continue;
                Combat.Hit(s, Vector3.up * 30f + Combat.Flat(d) * 2f, 0.3f, true, by);
            }
            Fx.Column(p, c, 30f, 0.7f, 1.4f);
            Fx.Burst(p + Vector3.up * 0.3f, c, 160, 22f, 0.25f, 1f, 0.8f, Vector3.up, 20f);
            Fx.Burst(p + Vector3.up * 0.3f, Color.white, 60, 14f, 0.2f, 0.8f, 0.6f, Vector3.up, 35f);
            AbilityCaster.ShakeNear(p, 0.3f);
            Sfx.CrashAt(p);
            Destroy(gameObject);
        }
    }

    /// <summary>LE MISSILE : il part devant, puis tourne vers le joueur le plus proche ; au contact (ou au bout de 4 s), BOUM.</summary>
    public class HomingMissile : MonoBehaviour
    {
        const float Speed = 22f;
        const float Life = 4f;
        Seeker by, target;
        Vector3 vel;
        float age, retarget;

        public static void Fire(Seeker by, Vector3 from, Vector3 dir)
        {
            Color c = AbilityInfo.Tint(Ability.Missile);
            Proto.BeginVisualOnly();
            GameObject go = Proto.Capsule(null, from, new Vector3(0.35f, 0.5f, 0.35f), c, "Missile");
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.7f, 0.2f, 0.8f);
            Proto.EndVisualOnly();
            HomingMissile m = go.AddComponent<HomingMissile>();
            m.by = by;
            m.vel = (dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward) * Speed;
            Sfx.WhooshAt(from);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            retarget -= dt;
            if (retarget <= 0f)
            {
                retarget = 0.25f;
                target = null;
                float best = 40f;
                for (int i = 0; i < Game.Seekers.Count; i++)
                {
                    Seeker s = Game.Seekers[i];
                    if (s == by || s.Body == null || s.Graced || s.Hidden) continue;
                    float d = (s.Body.position - transform.position).magnitude;
                    if (d < best) { best = d; target = s; }
                }
            }
            if (target != null && age > 0.25f)
            {
                Vector3 want = (target.Body.position + Vector3.up - transform.position).normalized * Speed;
                vel = Vector3.RotateTowards(vel, want, 3.2f * dt, 0f);
            }
            transform.position += vel * dt;
            transform.rotation = Quaternion.LookRotation(vel, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            if (Random.value < 0.8f) Fx.Sparks(transform.position, new Color(1f, 0.7f, 0.3f), 2, 2f);
            bool touch = target != null && (target.Body.position + Vector3.up - transform.position).magnitude < 1.6f;
            bool wall = Physics.Raycast(transform.position, vel.normalized, vel.magnitude * dt + 0.3f, ~0, QueryTriggerInteraction.Ignore) && age > 0.2f;
            if (!touch && !wall && age < Life) return;
            Vector3 p = transform.position;
            Combat.Blast(p, 4.5f, 24f, 12f, by);
            Color c = AbilityInfo.Tint(Ability.Missile);
            DivineFx.Impact(p, 4.5f, c, 0.8f, by != null && by.IsPlayer);
            Destroy(gameObject);
        }
    }

    /// <summary>LA TRAINEE DE FEU : 4 s, il seme des flammes derriere lui ; elles brulent 3 s ; qui les touche est projete.</summary>
    public class FireTrail : MonoBehaviour
    {
        const float Seconds = 4f;
        const float Burn = 3f;
        Seeker who;
        float until, drop, glow;
        readonly List<Vector3> spots = new List<Vector3>();
        readonly List<float> born = new List<float>();
        readonly Dictionary<Seeker, float> burnt = new Dictionary<Seeker, float>();

        public static void Light(Seeker s)
        {
            FireTrail f = s.Body.GetComponent<FireTrail>();
            if (f == null) f = s.Body.gameObject.AddComponent<FireTrail>();
            f.who = s;
            f.until = Time.time + Seconds;
            Fx.Burst(s.Body.position, AbilityInfo.Tint(Ability.Flammes), 50, 6f, 0.25f, 0.6f, -0.5f, Vector3.up, 40f);
        }

        void Update()
        {
            if (who == null || who.Body == null) { Destroy(this); return; }
            float dt = Time.deltaTime;
            Color c = AbilityInfo.Tint(Ability.Flammes);
            drop -= dt;
            if (Time.time < until && drop <= 0f)
            {
                drop = 0.15f;
                RaycastHit hit;
                Vector3 p = who.Body.position;
                if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) { spots.Add(hit.point); born.Add(Time.time); }
            }
            for (int i = spots.Count - 1; i >= 0; i--) if (Time.time - born[i] > Burn) { spots.RemoveAt(i); born.RemoveAt(i); }
            if (spots.Count == 0 && Time.time > until) { Destroy(this); return; }
            glow -= dt;
            if (glow <= 0f)
            {
                glow = 0.1f;
                for (int i = 0; i < spots.Count; i += 2)
                    Fx.Burst(spots[i] + Vector3.up * 0.2f, Random.value < 0.5f ? c : new Color(1f, 0.85f, 0.3f), 2, 2f, 0.3f, 0.5f, -0.8f, Vector3.up, 30f);
            }
            for (int k = 0; k < Game.Seekers.Count; k++)
            {
                Seeker s = Game.Seekers[k];
                if (s == who || s.Body == null) continue;
                float last;
                if (burnt.TryGetValue(s, out last) && Time.time - last < 0.8f) continue;
                for (int i = 0; i < spots.Count; i++)
                {
                    Vector3 d = s.Body.position - spots[i];
                    if (d.y < -0.5f || d.y > 1.5f || new Vector2(d.x, d.z).magnitude > 1.2f) continue;
                    burnt[s] = Time.time;
                    Vector3 back = -Combat.Flat(s.Body.forward).normalized;
                    Combat.Hit(s, back * 12f + Vector3.up * 13f, 0.3f, true, who);
                    Fx.Burst(s.Body.position + Vector3.up, c, 30, 6f, 0.2f, 0.5f, -0.5f, Vector3.up, 50f);
                    break;
                }
            }
        }
    }

    /// <summary>LE POGO : 5 s, des qu'il touche le sol, il rebondit tres haut (boing).</summary>
    public class PogoStick : MonoBehaviour
    {
        const float Seconds = 5f;
        Seeker who;
        float until, lastBounce;

        public static void Jump(Seeker s)
        {
            PogoStick p = s.Body.GetComponent<PogoStick>();
            if (p == null) p = s.Body.gameObject.AddComponent<PogoStick>();
            p.who = s;
            p.until = Time.time + Seconds;
            p.lastBounce = -9f;
        }

        void Update()
        {
            if (who == null || who.Body == null || Time.time > until) { Destroy(this); return; }
            if (who.Remote || Time.time - lastBounce < 0.45f || who.NoJump || who.Stunned) return;
            if (!Physics.Raycast(who.Body.position + Vector3.up * 0.3f, Vector3.down, 0.5f, ~0, QueryTriggerInteraction.Ignore)) return;
            IMover m = AbilityCaster.MoverOf(who);
            if (m == null) return;
            lastBounce = Time.time;
            m.Push(Vector3.up * 16f + Combat.Flat(who.Body.forward).normalized * 3f);
            Fx.Ring(who.Body.position + Vector3.up * 0.1f, AbilityInfo.Tint(Ability.Pogo), 0.3f, 1.6f, 0.25f, 0.15f, Vector3.up);
            Sfx.BoingAt(who.Body.position);
        }
    }

    /// <summary>LE BOOMERANG : 20 m devant en 0,6 s, puis il revient a son lanceur ; il frappe a l'aller ET au retour.</summary>
    public class BoomerangThrow : MonoBehaviour
    {
        const float Reach = 20f;
        const float Half = 0.6f;
        Seeker by;
        Vector3 from, dir;
        float age;
        readonly HashSet<Seeker> hitOut = new HashSet<Seeker>();
        readonly HashSet<Seeker> hitBack = new HashSet<Seeker>();

        public static void Throw(Seeker by, Vector3 from, Vector3 dir)
        {
            Color c = AbilityInfo.Tint(Ability.Boomerang);
            Proto.BeginVisualOnly();
            GameObject go = new GameObject("BOOMERANG");
            GameObject a = Proto.Cube(go.transform, new Vector3(0.35f, 0f, 0.2f), new Vector3(0.9f, 0.08f, 0.22f), c, "Bras");
            a.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            GameObject b = Proto.Cube(go.transform, new Vector3(-0.35f, 0f, 0.2f), new Vector3(0.9f, 0.08f, 0.22f), c, "Bras");
            b.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
            Proto.EndVisualOnly();
            go.transform.position = from;
            BoomerangThrow t = go.AddComponent<BoomerangThrow>();
            t.by = by;
            t.from = from;
            t.dir = dir;
            Sfx.WhooshAt(from);
        }

        void Update()
        {
            age += Time.deltaTime;
            bool back = age > Half;
            Vector3 home = by != null && by.Body != null ? by.Body.position + Vector3.up * 1.2f : from;
            Vector3 far = from + dir * Reach;
            transform.position = back ? Vector3.Lerp(far, home, Mathf.Clamp01((age - Half) / Half)) : Vector3.Lerp(from, far, Mathf.SmoothStep(0f, 1f, age / Half));
            transform.Rotate(0f, 1440f * Time.deltaTime, 0f, Space.World);
            HashSet<Seeker> done = back ? hitBack : hitOut;
            Vector3 travel = back ? (home - far).normalized : dir;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || done.Contains(s)) continue;
                if ((s.Body.position + Vector3.up - transform.position).magnitude > 1.7f) continue;
                done.Add(s);
                Combat.Hit(s, Combat.Flat(travel).normalized * 18f + Vector3.up * 9f, 0.3f, true, by);
                Sfx.PafAt(s.Body.position);
            }
            if (age > Half * 2f) Destroy(gameObject);
        }
    }

    /// <summary>LE PIEGE A LOUP : pose a tes pieds (deux au plus), arme en une seconde ; qui marche dessus est coince 3 s.</summary>
    public class JawTrap : MonoBehaviour
    {
        static readonly List<JawTrap> All = new List<JawTrap>();
        Seeker owner;
        float born;
        Transform jawA, jawB;

        public static bool Place(Seeker owner, Vector3 at)
        {
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return false;
            int mine = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == owner) mine++;
            if (mine >= 2)
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].owner == owner) { Destroy(All[i].gameObject); break; }
            GameObject go = new GameObject("PIEGE de " + owner.Name);
            go.transform.position = hit.point + Vector3.up * 0.03f;
            JawTrap t = go.AddComponent<JawTrap>();
            t.owner = owner;
            t.born = Time.time;
            Color iron = new Color(0.45f, 0.46f, 0.52f);
            Proto.BeginVisualOnly();
            Proto.Cylinder(go.transform, Vector3.zero, new Vector3(1.4f, 0.04f, 1.4f), iron, "Plaque");
            t.jawA = Proto.Cube(go.transform, new Vector3(0f, 0.12f, 0.45f), new Vector3(1.3f, 0.2f, 0.08f), iron, "Mâchoire").transform;
            t.jawB = Proto.Cube(go.transform, new Vector3(0f, 0.12f, -0.45f), new Vector3(1.3f, 0.2f, 0.08f), iron, "Mâchoire").transform;
            Proto.EndVisualOnly();
            All.Add(t);
            Sfx.SnapAt(go.transform.position);
            return true;
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (Time.time - born > 30f) { Destroy(gameObject); return; }
            if (Time.time - born < 1f) return;
            Vector3 p = transform.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || s == owner && Time.time - born < 3f) continue;
                Vector3 d = s.Body.position - p;
                if (d.y < -0.4f || d.y > 1f || new Vector2(d.x, d.z).magnitude > 0.85f) continue;
                if (!Combat.Afflict(s, Combat.Affliction.Prison, 3f, s == owner ? null : owner)) continue;
                if (jawA != null) jawA.localPosition = new Vector3(0f, 0.35f, 0.08f);
                if (jawB != null) jawB.localPosition = new Vector3(0f, 0.35f, -0.08f);
                Fx.Sparks(p + Vector3.up * 0.3f, Color.white, 20, 4f);
                Sfx.SnapAt(p);
                Destroy(gameObject, 3f);
                enabled = false;
                return;
            }
        }
    }

    /// <summary>LA BATAILLE D'OREILLERS : six oreillers, un toutes les 0,2 s, droit la ou il regarde.</summary>
    public class PillowVolley : MonoBehaviour
    {
        Seeker who;
        int left;
        float next;

        public static void Begin(Seeker s)
        {
            PillowVolley v = s.Body.GetComponent<PillowVolley>();
            if (v == null) v = s.Body.gameObject.AddComponent<PillowVolley>();
            v.who = s;
            v.left = 6;
            v.next = 0f;
        }

        void Update()
        {
            if (who == null || who.Body == null || left <= 0) { Destroy(this); return; }
            next -= Time.deltaTime;
            if (next > 0f) return;
            next = 0.2f;
            left--;
            Vector3 dir = who.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null ? Game.Hud.orbitCamera.transform.forward : who.Body.forward;
            Pillow.Throw(who, who.Body.position + Vector3.up * 1.4f + Combat.Flat(dir).normalized * 0.8f, dir);
        }
    }

    public class Pillow : MonoBehaviour
    {
        Seeker by;
        Vector3 vel;
        float age;

        public static void Throw(Seeker by, Vector3 from, Vector3 dir)
        {
            Proto.BeginVisualOnly();
            GameObject go = Proto.Cube(null, from, new Vector3(0.7f, 0.25f, 0.5f), Color.white, "Oreiller");
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(new Color(0.98f, 0.92f, 1f), 0.4f, 0f);
            Proto.EndVisualOnly();
            Pillow p = go.AddComponent<Pillow>();
            p.by = by;
            p.vel = (dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward) * 35f;
            Sfx.WhooshAt(from);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            transform.position += vel * dt;
            transform.Rotate(300f * dt, 500f * dt, 0f);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                if ((s.Body.position + Vector3.up - transform.position).magnitude > 1.3f) continue;
                Combat.Hit(s, Combat.Flat(vel).normalized * 14f + Vector3.up * 6f, 0.2f, true, by);
                Fx.Burst(transform.position, Color.white, 30, 5f, 0.15f, 0.8f, -0.2f, Vector3.zero, 0f);
                Sfx.PafAt(transform.position);
                Destroy(gameObject);
                return;
            }
            if (age > 0.8f || Physics.Raycast(transform.position, vel.normalized, vel.magnitude * dt + 0.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                Fx.Burst(transform.position, Color.white, 20, 4f, 0.15f, 0.7f, -0.2f, Vector3.zero, 0f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>LE RAZ-DE-MAREE : un anneau d'eau part de lui et grandit jusqu'a 22 m en 1,1 s ; il emporte qui il traverse.</summary>
    public class TidalWave : MonoBehaviour
    {
        float Reach = 22f;
        float Seconds = 1.1f;
        float Force = 20f;
        Seeker by;
        float age, spray;
        readonly HashSet<Seeker> hit = new HashSet<Seeker>();

        public static void Roll(Seeker by, Vector3 at) { Roll(by, at, 22f, 1.1f, 20f); }

        /// <summary>(08/10) Le TSUNAMI divin : la meme vague, bien plus grande et plus forte.</summary>
        public static void Roll(Seeker by, Vector3 at, float reach, float seconds, float force)
        {
            GameObject go = new GameObject(reach > 30f ? "TSUNAMI" : "RAZ-DE-MAREE");
            go.transform.position = at;
            TidalWave w = go.AddComponent<TidalWave>();
            w.by = by;
            w.Reach = reach;
            w.Seconds = seconds;
            w.Force = force;
            Fx.GroundRing(at, AbilityInfo.Tint(Ability.Raz), reach, seconds);
            // (v33) UN VRAI MUR D'EAU : 36 vagues bleues en cercle, coiffees d'ecume, qui
            // grandissent en avancant puis s'ecroulent (avant : des gouttes eparses).
            Color water = new Color(0.25f, 0.6f, 0.95f, 0.75f);
            float tall = reach > 30f ? 6f : 2.4f;
            w.tall = tall;
            for (int k = 0; k < 36; k++)
            {
                Proto.BeginVisualOnly();
                GameObject seg = Proto.Capsule(null, at, new Vector3(2.4f, tall * 0.5f, 1.4f), water, "Vague");
                GameObject foam = Proto.Sphere(seg.transform, new Vector3(0f, 0.9f, 0.3f), new Vector3(1.1f, 0.25f, 1.6f), Color.white, "Ecume");
                Proto.EndVisualOnly();
                Renderer r = seg.GetComponent<Renderer>();
                r.sharedMaterial = MaterialFactory.GetTransparent(water);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                foam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                w.wall.Add(seg.transform);
            }
            Sfx.CrashAt(at);
        }

        float tall;
        readonly List<Transform> wall = new List<Transform>();

        void OnDestroy()
        {
            for (int i = 0; i < wall.Count; i++)
            {
                if (wall[i] == null) continue;
                Renderer r = wall[i].GetComponent<Renderer>();
                if (r != null) Destroy(r.sharedMaterial);
                Destroy(wall[i].gameObject);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float r = Reach * Mathf.Clamp01(age / Seconds);
            Vector3 c = transform.position;
            Color tint = AbilityInfo.Tint(Ability.Raz);
            float k01 = Mathf.Clamp01(age / Seconds);
            float h = tall * Mathf.Sin(Mathf.Min(1f, k01 * 1.15f) * Mathf.PI * 0.85f + 0.15f);
            for (int k = 0; k < wall.Count; k++)
            {
                if (wall[k] == null) continue;
                float a = k / (float)wall.Count * Mathf.PI * 2f;
                Vector3 outDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = DivineFx.OnGround(c + outDir * Mathf.Max(0.5f, r));
                float hh = Mathf.Max(0.05f, h * (0.85f + 0.15f * Mathf.Sin(age * 8f + k)));
                wall[k].position = p + Vector3.up * hh * 0.5f;
                wall[k].rotation = Quaternion.LookRotation(outDir) * Quaternion.Euler(-15f * k01, 0f, 0f);
                wall[k].localScale = new Vector3(Mathf.Max(1.2f, r * 0.2f), hh * 0.5f, 1.4f);
            }
            spray -= dt;
            if (spray <= 0f)
            {
                spray = 0.06f;
                for (int k = 0; k < 6; k++)
                {
                    float a = Random.value * Mathf.PI * 2f;
                    Fx.Burst(c + new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * r, Random.value < 0.5f ? tint : Color.white, 3, 4f, 0.3f, 0.5f, 0.6f, Vector3.up, 40f);
                }
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || hit.Contains(s)) continue;
                Vector3 d = s.Body.position - c;
                float flat = new Vector2(d.x, d.z).magnitude;
                if (Mathf.Abs(d.y) > 3.5f || flat > r || flat < r - 3f) continue;
                hit.Add(s);
                Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                Combat.Hit(s, away * Force + Vector3.up * Force * 0.45f, 0.3f, true, by);
            }
            if (age > Seconds) Destroy(gameObject);
        }
    }
}
