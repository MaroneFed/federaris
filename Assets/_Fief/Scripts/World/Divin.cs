using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA TROISIEME FOURNEE DIVINE (09/10, v31 -- Martin : "encore plus de capacites divines, des
    /// trucs de malade"). Seulement en MODE DIEU, comme les autres divines (World/Dieu.cs) :
    ///
    ///   l'ARMEE DE HARICOTS (six petits kamikazes qui courent sur les autres), le VOLCAN (il
    ///   crache des bombes de lave cinq secondes), la FRAPPE ORBITALE (un laser du ciel qui suit
    ///   sa cible), le ROCHER GEANT (il roule et ecrase tout), la CHUTE DE LA LUNE (elle tombe,
    ///   lentement, et souffle 45 m), l'OURAGAN (une tornade geante autour de toi), la FRAPPE DU
    ///   CIEL (tu bondis tres haut et tu t'ecrases), la PLUIE D'ENCLUMES (une sur CHAQUE autre
    ///   joueur, ou qu'il soit) ; passives : l'ORAGE (la foudre frappe pres de toi), les ORBES DE
    ///   FEU (trois boules qui tournent autour de toi), les PAS DE TITAN (chaque atterrissage
    ///   fait trembler le sol).
    ///
    /// Tout ce qui frappe passe par Combat.Hit / Combat.Blast, comme le reste : un protege n'est
    /// pas touche, un joueur d'une autre machine l'est chez lui.
    /// </summary>
    public static class Divin
    {
        /// <summary>Les passives de la fournee, a tenir chaque image (le joueur, les bots).</summary>
        public static void KeepPassives(Seeker s)
        {
            if (s == null || s.Body == null) return;
            if (s.Has(Ability.Orage)) StormAura.Keep(s);
            if (s.Has(Ability.Orbes)) FireOrbs.Keep(s);
            if (s.Has(Ability.Titan)) TitanSteps.Keep(s);
        }

        /// <summary>Le plus proche des autres (pas protege) a moins de "range" m de "at" ; null s'il n'y a personne.</summary>
        public static Seeker NearestOther(Seeker by, Vector3 at, float range)
        {
            Seeker best = null;
            float bestD = range;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == by || o.Body == null || o.Graced) continue;
                float d = (o.Body.position - at).magnitude;
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }

        /// <summary>Le sol sous "p" (ou "p" tel quel s'il n'y en a pas).</summary>
        public static bool Ground(ref Vector3 p, float above, float depth)
        {
            RaycastHit hit;
            if (!Physics.Raycast(p + Vector3.up * above, Vector3.down, out hit, above + depth, ~0, QueryTriggerInteraction.Ignore)) return false;
            p = hit.point;
            return true;
        }

        /// <summary>Un petit objet qui ne cogne rien (que du visuel).</summary>
        public static GameObject Ball(Vector3 at, float size, Color c, float glow)
        {
            Proto.BeginVisualOnly();
            GameObject b = Proto.Sphere(null, at, new Vector3(size, size, size), c, "Boule");
            if (glow > 0f) b.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, glow);
            Proto.EndVisualOnly();
            return b;
        }
    }

    /// <summary>L'ARMEE DE HARICOTS : un petit haricot kamikaze, qui court sur le plus proche et explose.</summary>
    public class BeanBomber : MonoBehaviour
    {
        const float Speed = 13f;
        const float Life = 8f;
        Seeker by;
        Seeker prey;
        Vector3 dir;
        float age, hop;

        public static void Release(Seeker by, Vector3 at, Vector3 dir)
        {
            GameObject go = new GameObject("HARICOT KAMIKAZE");
            go.transform.position = at;
            BeanBomber b = go.AddComponent<BeanBomber>();
            b.by = by;
            b.dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            Color c = by != null ? by.Colour : AbilityInfo.Tint(Ability.Armee);
            Proto.BeginVisualOnly();
            Proto.Capsule(go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.55f, 0.7f), c, "Haricot");
            Proto.Sphere(go.transform, new Vector3(-0.16f, 0.85f, 0.3f), new Vector3(0.22f, 0.26f, 0.12f), Color.white, "Oeil");
            Proto.Sphere(go.transform, new Vector3(0.16f, 0.85f, 0.3f), new Vector3(0.22f, 0.26f, 0.12f), Color.white, "Oeil");
            Proto.Sphere(go.transform, new Vector3(-0.16f, 0.85f, 0.36f), new Vector3(0.1f, 0.12f, 0.06f), Color.black, "Pupille");
            Proto.Sphere(go.transform, new Vector3(0.16f, 0.85f, 0.36f), new Vector3(0.1f, 0.12f, 0.06f), Color.black, "Pupille");
            GameObject fuse = Proto.Sphere(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.18f, 0.18f, 0.18f), new Color(1f, 0.5f, 0.1f), "Meche");
            fuse.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.5f, 0.1f), 2f);
            Proto.EndVisualOnly();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            if (prey == null || prey.Body == null || prey.Graced || age % 1f < dt) prey = Divin.NearestOther(by, p, 60f);
            if (prey != null)
            {
                Vector3 to = Combat.Flat(prey.Body.position - p);
                if (to.sqrMagnitude > 0.01f) dir = Vector3.Slerp(dir, to.normalized, 6f * dt).normalized;
                if ((prey.Body.position - p).magnitude < 1.7f) { Explode(); return; }
            }
            p += dir * Speed * dt;
            // Il court au sol ; au-dessus du vide, il tombe.
            Vector3 g = p;
            if (Divin.Ground(ref g, 3f, 4f)) p.y = Mathf.Lerp(p.y, g.y, 0.5f);
            else p.y -= 12f * dt;
            hop += dt * 14f;
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, Mathf.Sin(hop) * 12f);
            if (age > Life || p.y < -40f) Explode();
        }

        void Explode()
        {
            Vector3 p = transform.position + Vector3.up * 0.6f;
            Color c = AbilityInfo.Tint(Ability.Armee);
            Combat.Blast(transform.position, 5f, 26f, 14f, by);
            Fx.Shock(p, c, 5f, 0.3f);
            Fx.Burst(p, c, 80, 14f, 0.3f, 0.7f, 0.3f, Vector3.up, 80f);
            Fx.Flash(p, c, 14f, 4f, 0.25f);
            AbilityCaster.ShakeNear(p, 0.3f);
            Sfx.CrashAt(p);
            Destroy(gameObject);
        }
    }

    /// <summary>LE VOLCAN : il surgit la ou tu vises, et cinq secondes il crache des bombes de lave a 22 m.</summary>
    public class Volcano : MonoBehaviour
    {
        const float Life = 5.2f;
        const float Reach = 22f;
        Seeker by;
        float age, spit;
        Transform cone;

        sealed class LavaBomb
        {
            public Transform Ball;
            public Vector3 From, To;
            public float T;
        }
        readonly List<LavaBomb> bombs = new List<LavaBomb>();

        public static void Raise(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("VOLCAN");
            go.transform.position = at;
            Volcano v = go.AddComponent<Volcano>();
            v.by = by;
            Proto.BeginVisualOnly();
            GameObject c = Proto.Cone(go.transform, Vector3.zero, 5f, 6f, new Color(0.25f, 0.18f, 0.16f), "Cone", 10);
            // La lave qui deborde du cratere, et deux coulees sur les flancs.
            Color lava = AbilityInfo.Tint(Ability.Volcan);
            GameObject cap = Proto.Sphere(c.transform, new Vector3(0f, 6f, 0f), new Vector3(2.6f, 1f, 2.6f), lava, "Lave");
            cap.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(lava, 2.2f);
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.1f;
                GameObject flow = Proto.Capsule(c.transform, new Vector3(Mathf.Cos(a) * 2.4f, 3f, Mathf.Sin(a) * 2.4f), new Vector3(0.6f, 2.6f, 0.6f), lava, "Coulee");
                flow.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))) * Quaternion.Euler(40f, 0f, 0f);
                flow.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(lava, 1.6f);
            }
            Proto.EndVisualOnly();
            v.cone = c.transform;
            v.cone.localScale = new Vector3(1f, 0.05f, 1f);
            Combat.Blast(at, 6f, 24f, 18f, by);
            Fx.GroundRing(at, AbilityInfo.Tint(Ability.Volcan), 8f, 0.5f);
            AbilityCaster.ShakeNear(at, 0.6f);
            Sfx.CrashAt(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 top = transform.position + Vector3.up * 6f;
            Color t = AbilityInfo.Tint(Ability.Volcan);
            if (cone != null)
            {
                float grow = age < 5f ? Mathf.Clamp01(age / 0.4f) : Mathf.Clamp01((Life - age) / 0.2f + 0.05f);
                cone.localScale = new Vector3(1f, Mathf.Max(0.05f, grow), 1f);
            }
            spit -= dt;
            if (age < 5f && spit <= 0f)
            {
                spit = 0.22f;
                Fx.Burst(top, t, 20, 10f, 0.5f, 0.8f, 0.6f, Vector3.up, 30f);
                // La fumee noire qui monte du cratere.
                Fx.Burst(top + Vector3.up, new Color(0.18f, 0.15f, 0.15f), 6, 4f, 1.6f, 2.2f, -0.6f, Vector3.up, 20f);
                LavaBomb b = new LavaBomb();
                b.From = top;
                float a = Random.value * Mathf.PI * 2f;
                Vector3 to = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(4f, Reach);
                Divin.Ground(ref to, 20f, 40f);
                b.To = to;
                b.Ball = Divin.Ball(top, 1.3f, t, 2f).transform;
                bombs.Add(b);
            }
            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                LavaBomb b = bombs[i];
                b.T += dt / 1.1f;
                if (b.Ball != null)
                {
                    Vector3 p = Vector3.Lerp(b.From, b.To, b.T);
                    p.y += Mathf.Sin(b.T * Mathf.PI) * 14f;
                    b.Ball.position = p;
                    if (Random.value < 0.6f) Fx.Burst(p, t, 2, 1f, 0.5f, 0.5f, -0.3f, Vector3.up, 30f);
                }
                if (b.T < 1f) continue;
                Combat.Blast(b.To, 4f, 24f, 13f, by);
                Fx.Shock(b.To + Vector3.up * 0.5f, t, 4f, 0.25f);
                Fx.Burst(b.To + Vector3.up * 0.3f, t, 40, 10f, 0.3f, 0.6f, 0.5f, Vector3.up, 70f);
                Sfx.CrashAt(b.To);
                if (b.Ball != null) Destroy(b.Ball.gameObject);
                bombs.RemoveAt(i);
            }
            if (age > Life && bombs.Count == 0) Destroy(gameObject);
        }

        void OnDestroy()
        {
            for (int i = 0; i < bombs.Count; i++) if (bombs[i].Ball != null) Destroy(bombs[i].Ball.gameObject);
        }
    }

    /// <summary>
    /// LA FRAPPE ORBITALE : un laser tombe du ciel sur le joueur vise, et le SUIT quatre secondes
    /// (a 8,5 m/s : qui court bien en sort). La premiere seconde, une cible au sol : on est prevenu.
    /// </summary>
    public class OrbitalStrike : MonoBehaviour
    {
        const float Life = 4.8f;
        const float Warn = 0.9f;
        const float Follow = 8.5f;
        Seeker by, target;
        float age, tick;
        Transform beam, core, disc;

        public static void Lock(Seeker by, Seeker target)
        {
            GameObject go = new GameObject("FRAPPE ORBITALE");
            go.transform.position = target.Body.position;
            OrbitalStrike o = go.AddComponent<OrbitalStrike>();
            o.by = by;
            o.target = target;
            // (10/10) UN VRAI RAYON : un tube de lumiere de 120 m, un coeur blanc dedans, et au
            // sol un disque qui brule -- avant, ce n'etaient que des eclats sans corps.
            Color c = AbilityInfo.Tint(Ability.FrappeOrbitale);
            Proto.BeginVisualOnly();
            GameObject b = Proto.Cylinder(go.transform, new Vector3(0f, 60f, 0f), new Vector3(0.3f, 60f, 0.3f), c, "Rayon");
            b.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 2.5f);
            GameObject k = Proto.Cylinder(go.transform, new Vector3(0f, 60f, 0f), new Vector3(0.12f, 60f, 0.12f), Color.white, "Coeur");
            k.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Color.white, 3f);
            GameObject d = Proto.Cylinder(go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(9f, 0.02f, 9f), c, "Disque");
            d.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 1.2f);
            Proto.EndVisualOnly();
            o.beam = b.transform;
            o.core = k.transform;
            o.disc = d.transform;
            Sfx.Alarm();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (age > Life) { Destroy(gameObject); return; }
            Vector3 p = transform.position;
            if (target != null && target.Body != null)
            {
                Vector3 to = target.Body.position - p;
                p += Vector3.ClampMagnitude(to, Follow * dt);
            }
            Divin.Ground(ref p, 4f, 10f);
            transform.position = p;
            Color c = AbilityInfo.Tint(Ability.FrappeOrbitale);
            // Pendant l'alerte, un fil mince qui tremble ; puis le rayon s'ouvre et pulse.
            float open = age < Warn ? 0.15f + 0.1f * Mathf.Sin(age * 40f) : Mathf.Min(1f, (age - Warn) * 6f) * (1f + 0.15f * Mathf.Sin(age * 30f));
            float fade = Mathf.Clamp01((Life - age) / 0.3f);
            if (beam != null) beam.localScale = new Vector3(3.2f * open * fade, 60f, 3.2f * open * fade);
            if (core != null) core.localScale = new Vector3(1.2f * open * fade, 60f, 1.2f * open * fade);
            if (disc != null) { float r = age < Warn ? 9f * (age / Warn) : 9f; disc.localScale = new Vector3(r * fade, 0.02f, r * fade); disc.Rotate(0f, 200f * dt, 0f); }
            tick -= dt;
            if (tick > 0f) return;
            tick = age < Warn ? 0.12f : 0.3f;
            if (age < Warn) { Fx.GroundRing(p, c, 4.5f, 0.15f); return; }
            Combat.Blast(p, 4.5f, 22f, 16f, by);
            Fx.Column(p, Color.white, 120f, 0.2f, 1.2f);
            Fx.Column(p, c, 120f, 0.35f, 2.6f);
            Fx.Shock(p + Vector3.up * 0.4f, c, 4.5f, 0.25f);
            Fx.Burst(p + Vector3.up * 0.3f, c, 30, 12f, 0.25f, 0.5f, 0.3f, Vector3.up, 80f);
            Sfx.ChipAt(p);
            if (Random.value < 0.4f) AbilityCaster.ShakeNear(p, 0.25f);
        }
    }

    /// <summary>LE ROCHER GEANT : 7 m de pierre qui roule devant toi a 20 m/s, et ecrase tout sur 90 m.</summary>
    public class GiantBoulder : MonoBehaviour
    {
        const float Speed = 20f;
        const float Life = 4.5f;
        const float Radius = 3.5f;
        Seeker by;
        Vector3 dir;
        float age, fall;
        Transform rock;
        readonly List<Seeker> struck = new List<Seeker>();

        public static void Roll(Seeker by, Vector3 from, Vector3 dir)
        {
            GameObject go = new GameObject("ROCHER GEANT");
            go.transform.position = from;
            GiantBoulder b = go.AddComponent<GiantBoulder>();
            b.by = by;
            b.dir = Combat.Flat(dir).sqrMagnitude > 0.01f ? Combat.Flat(dir).normalized : Vector3.forward;
            Proto.BeginVisualOnly();
            GameObject r = Proto.Sphere(go.transform, new Vector3(0f, Radius, 0f), new Vector3(Radius * 2f, Radius * 2f, Radius * 2f), new Color(0.48f, 0.44f, 0.4f), "Rocher");
            Proto.Sphere(r.transform, new Vector3(0.3f, 0.25f, 0.3f), new Vector3(0.35f, 0.35f, 0.35f), new Color(0.38f, 0.35f, 0.32f), "Bosse");
            Proto.Sphere(r.transform, new Vector3(-0.35f, -0.1f, 0.25f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.38f, 0.35f, 0.32f), "Bosse");
            Proto.EndVisualOnly();
            b.rock = r.transform;
            Sfx.CrashAt(from);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position + dir * Speed * dt;
            Vector3 g = p;
            if (Divin.Ground(ref g, 6f, 8f)) { p.y = Mathf.Lerp(p.y, g.y, 0.4f); fall = 0f; }
            else { fall += 22f * dt; p.y -= fall * dt; }
            transform.position = p;
            if (rock != null) rock.Rotate(Vector3.Cross(Vector3.up, dir), Speed * dt / Radius * Mathf.Rad2Deg, Space.World);
            if (Random.value < 0.5f) Fx.Burst(p + Vector3.up * 0.2f, new Color(0.6f, 0.52f, 0.42f), 4, 5f, 0.4f, 0.5f, 0.3f, Vector3.up, 60f);
            if (age % 0.25f < dt) AbilityCaster.ShakeNear(p, 0.15f);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || struck.Contains(s)) continue;
                Vector3 d = s.Body.position - (p + Vector3.up * Radius);
                if (d.magnitude > Radius + 1.2f) continue;
                struck.Add(s);
                Vector3 side = Vector3.Cross(Vector3.up, dir) * (Vector3.Dot(d, Vector3.Cross(Vector3.up, dir)) >= 0f ? 1f : -1f);
                Combat.Hit(s, dir * 30f + side * 8f + Vector3.up * 14f, 0.5f, true, by);
                Fx.Impact(s.Body.position + Vector3.up, new Color(0.6f, 0.52f, 0.42f), 1.6f);
                Sfx.PunchAt(s.Body.position);
            }
            if (age > Life || p.y < -60f) { Fx.Burst(p + Vector3.up * Radius, new Color(0.5f, 0.45f, 0.4f), 120, 14f, 0.6f, 1f, 0.6f, Vector3.up, 90f); Destroy(gameObject); }
        }
    }

    /// <summary>LA CHUTE DE LA LUNE : elle tombe du ciel en 3,5 s (une ombre qui grandit), puis tout saute a 45 m.</summary>
    public class MoonFall : MonoBehaviour
    {
        const float Fall = 3.5f;
        const float Radius = 45f;
        const float Size = 26f;
        Seeker by;
        float age, ring;
        Transform moon, shadow;

        public static void Drop(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("LUNE");
            go.transform.position = at;
            MoonFall m = go.AddComponent<MoonFall>();
            m.by = by;
            Color t = AbilityInfo.Tint(Ability.Lune);
            Proto.BeginVisualOnly();
            GameObject r = Proto.Sphere(null, at + Vector3.up * 240f, new Vector3(Size, Size, Size), t, "Lune");
            r.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(t, 0.6f);
            Color crater = new Color(0.62f, 0.62f, 0.7f);
            Proto.Sphere(r.transform, new Vector3(0.25f, 0.2f, -0.38f), new Vector3(0.22f, 0.22f, 0.12f), crater, "Cratere");
            Proto.Sphere(r.transform, new Vector3(-0.2f, -0.15f, -0.42f), new Vector3(0.16f, 0.16f, 0.1f), crater, "Cratere");
            Proto.Sphere(r.transform, new Vector3(-0.05f, 0.32f, -0.36f), new Vector3(0.1f, 0.1f, 0.08f), crater, "Cratere");
            // Son OMBRE au sol : un disque sombre qui grandit -- on voit ou elle va tomber.
            GameObject sh = Proto.Cylinder(go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1f, 0.01f, 1f), new Color(0.05f, 0.05f, 0.1f), "Ombre");
            Proto.EndVisualOnly();
            m.moon = r.transform;
            m.shadow = sh.transform;
            Sfx.Alarm();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            Color t = AbilityInfo.Tint(Ability.Lune);
            float k = Mathf.Clamp01(age / Fall);
            ring -= dt;
            if (ring <= 0f && age < Fall) { ring = Mathf.Lerp(0.35f, 0.08f, k); Fx.GroundRing(p, new Color(0.2f, 0.2f, 0.3f), Radius * Mathf.Lerp(0.3f, 1f, k), 0.2f); }
            if (moon != null)
            {
                // Lente, puis de plus en plus vite (k au carre).
                moon.position = p + Vector3.up * Mathf.Lerp(240f, Size * 0.3f, k * k);
                moon.Rotate(Vector3.up, 20f * dt, Space.World);
                // Elle brule en entrant dans le ciel : une traine de feu derriere elle.
                Fx.Burst(moon.position + Vector3.up * Size * 0.4f, new Color(1f, 0.55f, 0.2f), 8, 6f, 2.5f, 1f, -0.3f, Vector3.up, 40f);
                if (age % 0.6f < dt) AbilityCaster.ShakeNear(p, 0.1f + 0.4f * k);
            }
            if (shadow != null) { float r = Radius * 2f * Mathf.Lerp(0.15f, 1f, k * k); shadow.localScale = new Vector3(r, 0.01f, r); }
            if (age < Fall) return;
            if (moon != null) Destroy(moon.gameObject);
            Combat.Blast(p, Radius, 48f, 26f, by);
            Fx.Flash(p + Vector3.up * 6f, Color.white, 160f, 16f, 0.9f);
            Fx.Shock(p + Vector3.up * 2f, t, Radius, 1f);
            Fx.Shock(p + Vector3.up * 2f, Color.white, Radius * 0.6f, 0.6f);
            Fx.GroundRing(p, t, Radius * 1.3f, 1.2f);
            Fx.Burst(p + Vector3.up * 3f, t, 500, 45f, 0.7f, 1.8f, 0.2f, Vector3.up, 90f);
            Fx.Burst(p + Vector3.up * 2f, new Color(0.5f, 0.45f, 0.4f), 250, 25f, 1f, 2.5f, 0.6f, Vector3.up, 60f);
            AbilityCaster.ShakeNear(p, 1.5f);
            Sfx.KoBoom(p, true);
            Sfx.CrashAt(p);
            Destroy(gameObject);
        }

        void OnDestroy() { if (moon != null) Destroy(moon.gameObject); }
    }

    /// <summary>L'OURAGAN : six secondes, une tornade geante de 14 m tourne AUTOUR de toi et envoie valser qui s'y trouve.</summary>
    public class Hurricane : MonoBehaviour
    {
        const float Life = 6f;
        const float Radius = 14f;
        Seeker who;
        float until, ringT, soundT;
        readonly Dictionary<Seeker, float> flung = new Dictionary<Seeker, float>();

        public static void Spin(Seeker s)
        {
            Hurricane h = s.Body.GetComponent<Hurricane>();
            if (h == null) h = s.Body.gameObject.AddComponent<Hurricane>();
            h.who = s;
            h.until = Time.time + Life;
        }

        void Update()
        {
            if (who == null || who.Body == null || Time.time > until) { Destroy(this); return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 c = who.Body.position;
            Color t = AbilityInfo.Tint(Ability.Ouragan);
            ringT -= dt;
            if (ringT <= 0f)
            {
                ringT = 0.04f;
                float h = Random.value * 28f;
                float r = Mathf.Lerp(3f, Radius + 4f, h / 28f);
                Fx.Ring(c + Vector3.up * h, Color.Lerp(t, Color.white, Random.value * 0.6f), r * 0.85f, r * 1.15f, 0.35f, 0.2f, Vector3.up);
                if (Random.value < 0.5f)
                {
                    float a = Random.value * Mathf.PI * 2f;
                    Fx.Burst(c + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * Radius, new Color(0.65f, 0.6f, 0.52f), 6, 6f, 0.4f, 0.6f, 0.1f, Vector3.up, 60f);
                }
            }
            soundT -= dt;
            if (soundT <= 0f) { soundT = 0.35f; Sfx.WhooshAt(c); }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == who || s.Body == null) continue;
                Vector3 d = s.Body.position - c;
                float flat = Combat.Flat(d).magnitude;
                if (flat > Radius || d.y < -3f || d.y > 25f) continue;
                float last;
                if (flung.TryGetValue(s, out last) && Time.time - last < 1.1f) continue;
                flung[s] = Time.time;
                Vector3 away = flat > 0.1f ? Combat.Flat(d) / flat : Vector3.forward;
                Vector3 swirl = new Vector3(-away.z, 0f, away.x);
                Combat.Hit(s, swirl * 18f + away * 10f + Vector3.up * 22f, 0.35f, true, who);
                Fx.Impact(s.Body.position + Vector3.up, t, 1.2f);
            }
        }
    }

    /// <summary>LA FRAPPE DU CIEL : on est en l'air (AbilityCaster l'a lance) ; a l'atterrissage, une onde de 18 m.</summary>
    public class SkySlam : MonoBehaviour
    {
        Seeker who;
        float age;

        public static void Arm(Seeker s)
        {
            SkySlam k = s.Body.GetComponent<SkySlam>();
            if (k == null) k = s.Body.gameObject.AddComponent<SkySlam>();
            k.who = s;
            k.age = 0f;
        }

        void Update()
        {
            if (who == null || who.Body == null) { Destroy(this); return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = who.Body.position;
            Color t = AbilityInfo.Tint(Ability.FrappeCiel);
            if (Random.value < 0.6f) Fx.Burst(p + Vector3.up, t, 3, 2f, 0.4f, 0.4f, 0f, Vector3.zero, 0f);
            if (age > 6f) { Destroy(this); return; }
            if (age < 0.5f) return;
            RaycastHit hit;
            if (!Physics.Raycast(p + Vector3.up * 0.4f, Vector3.down, out hit, 1.1f, ~0, QueryTriggerInteraction.Ignore)) return;
            Combat.Blast(hit.point, 18f, 38f, 20f, who);
            Fx.Shock(hit.point + Vector3.up, t, 18f, 0.6f);
            Fx.GroundRing(hit.point, Color.white, 18f, 0.5f);
            Fx.Burst(hit.point + Vector3.up * 0.3f, new Color(0.6f, 0.52f, 0.42f), 200, 18f, 0.5f, 1.2f, 0.6f, Vector3.up, 80f);
            Fx.Flash(hit.point + Vector3.up * 2f, t, 40f, 8f, 0.4f);
            AbilityCaster.ShakeNear(hit.point, 1f);
            Sfx.KoBoom(hit.point, who.IsPlayer);
            Destroy(this);
        }
    }

    /// <summary>LA PLUIE D'ENCLUMES : une ombre suit chaque autre joueur 1,2 s... puis une enclume lui tombe dessus.</summary>
    public class AnvilDrop : MonoBehaviour
    {
        const float Warn = 1.2f;
        const float Drop = 0.35f;
        Seeker by, victim;
        float age, ring;
        Transform anvil;
        Vector3 spot;

        public static void On(Seeker by, Seeker victim)
        {
            GameObject go = new GameObject("ENCLUME");
            AnvilDrop a = go.AddComponent<AnvilDrop>();
            a.by = by;
            a.victim = victim;
            a.spot = victim.Body.position;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (victim != null && victim.Body != null && age < Warn) spot = victim.Body.position;
            Color dark = new Color(0.16f, 0.16f, 0.2f);
            ring -= dt;
            if (age < Warn && ring <= 0f) { ring = 0.1f; Fx.GroundRing(spot, dark, 2.2f, 0.12f); }
            if (age >= Warn && anvil == null)
            {
                Proto.BeginVisualOnly();
                GameObject a = Proto.Cube(null, spot + Vector3.up * 30f, new Vector3(1.8f, 1f, 1f), new Color(0.22f, 0.22f, 0.26f), "Enclume");
                Proto.Cube(a.transform, new Vector3(0f, -0.65f, 0f), new Vector3(0.45f, 0.4f, 0.6f), new Color(0.22f, 0.22f, 0.26f), "Pied");
                Proto.Cube(a.transform, new Vector3(0f, -1.05f, 0f), new Vector3(0.8f, 0.3f, 1.1f), new Color(0.22f, 0.22f, 0.26f), "Socle");
                Proto.EndVisualOnly();
                anvil = a.transform;
            }
            if (anvil != null) anvil.position = spot + Vector3.up * Mathf.Lerp(30f, 1.2f, Mathf.Clamp01((age - Warn) / Drop));
            if (age < Warn + Drop) return;
            if (victim != null && victim.Body != null && (victim.Body.position - spot).magnitude < 2.4f)
            {
                Vector3 side = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                Combat.Hit(victim, side.normalized * 10f + Vector3.up * 7f, 0.7f, true, by);
            }
            Fx.Shock(spot + Vector3.up * 0.5f, Color.white, 3f, 0.25f);
            Fx.Burst(spot + Vector3.up * 0.3f, new Color(0.6f, 0.55f, 0.5f), 50, 9f, 0.3f, 0.6f, 0.6f, Vector3.up, 70f);
            Sfx.ClangAt(spot);
            if (anvil != null) Destroy(anvil.gameObject, 0.8f);
            Destroy(gameObject);
        }
    }

    // ================================================================== les passives

    /// <summary>L'ORAGE (passive divine) : toutes les 3 s, la foudre tombe sur le plus proche des autres a 18 m.</summary>
    public class StormAura : MonoBehaviour
    {
        Seeker who;
        float next, puff;

        public static void Keep(Seeker s)
        {
            if (s.Body.GetComponent<StormAura>() != null) return;
            StormAura a = s.Body.gameObject.AddComponent<StormAura>();
            a.who = s;
            a.next = Time.time + 3f;
        }

        void Update()
        {
            if (who == null || who.Body == null || !who.Has(Ability.Orage)) { Destroy(this); return; }
            puff -= Time.deltaTime;
            if (puff <= 0f) { puff = 0.25f; Fx.Sparks(who.Body.position + Vector3.up * 2.4f, AbilityInfo.Tint(Ability.Orage), 3, 2f); }
            if (Time.time < next || !Match.Active) return;
            next = Time.time + 3f;
            Seeker t = Divin.NearestOther(who, who.Body.position, 18f);
            if (t != null) Lightning.Call(who, t.Body.position);
        }
    }

    /// <summary>LES ORBES DE FEU (passive divine) : trois boules tournent autour de toi ; qui les touche est projete.</summary>
    public class FireOrbs : MonoBehaviour
    {
        const float Orbit = 2.6f;
        Seeker who;
        float angle;
        readonly Transform[] orbs = new Transform[3];
        readonly Dictionary<Seeker, float> burnt = new Dictionary<Seeker, float>();

        public static void Keep(Seeker s)
        {
            if (s.Body.GetComponent<FireOrbs>() != null) return;
            FireOrbs f = s.Body.gameObject.AddComponent<FireOrbs>();
            f.who = s;
            for (int i = 0; i < 3; i++) f.orbs[i] = Divin.Ball(s.Body.position, 0.7f, AbilityInfo.Tint(Ability.Orbes), 2.5f).transform;
        }

        void Update()
        {
            if (who == null || who.Body == null || !who.Has(Ability.Orbes)) { Destroy(this); return; }
            float dt = Time.deltaTime;
            angle += dt * 3.2f;
            Vector3 c = who.Body.position + Vector3.up * 1.1f;
            for (int k = 0; k < 3; k++)
            {
                if (orbs[k] == null) continue;
                float a = angle + k * 2.0944f;
                Vector3 p = c + new Vector3(Mathf.Cos(a), Mathf.Sin(angle * 1.7f + k) * 0.25f, Mathf.Sin(a)) * Orbit;
                orbs[k].position = p;
                if (Random.value < 0.3f) Fx.Burst(p, AbilityInfo.Tint(Ability.Orbes), 1, 1f, 0.3f, 0.3f, -0.5f, Vector3.up, 30f);
                for (int i = 0; i < Game.Seekers.Count; i++)
                {
                    Seeker s = Game.Seekers[i];
                    if (s == who || s.Body == null) continue;
                    Vector3 d = s.Body.position + Vector3.up - p;
                    if (d.magnitude > 1.3f) continue;
                    float last;
                    if (burnt.TryGetValue(s, out last) && Time.time - last < 0.8f) continue;
                    burnt[s] = Time.time;
                    Vector3 away = Combat.Flat(s.Body.position - c);
                    away = away.sqrMagnitude > 0.01f ? away.normalized : who.Body.forward;
                    Combat.Hit(s, away * 20f + Vector3.up * 9f, 0.25f, true, who);
                    Fx.Impact(p, AbilityInfo.Tint(Ability.Orbes), 1f);
                }
            }
        }

        void OnDestroy()
        {
            for (int k = 0; k < 3; k++) if (orbs[k] != null) Destroy(orbs[k].gameObject);
        }
    }

    /// <summary>LES PAS DE TITAN (passive divine) : chaque atterrissage d'au moins 4 m fait trembler le sol (onde de 7 m).</summary>
    public class TitanSteps : MonoBehaviour
    {
        Seeker who;
        bool airborne;
        float top;

        public static void Keep(Seeker s)
        {
            if (s.Body.GetComponent<TitanSteps>() != null) return;
            TitanSteps t = s.Body.gameObject.AddComponent<TitanSteps>();
            t.who = s;
        }

        void Update()
        {
            if (who == null || who.Body == null || !who.Has(Ability.Titan)) { Destroy(this); return; }
            Vector3 p = who.Body.position;
            bool grounded = Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, 0.7f, ~0, QueryTriggerInteraction.Ignore);
            if (!grounded)
            {
                if (!airborne) { airborne = true; top = p.y; }
                top = Mathf.Max(top, p.y);
                return;
            }
            if (!airborne) return;
            airborne = false;
            if (top - p.y < 4f || !Match.Active) return;
            Color t = AbilityInfo.Tint(Ability.Titan);
            Combat.Blast(p, 7f, 24f, 12f, who);
            Fx.GroundRing(p, t, 7f, 0.4f);
            Fx.Burst(p + Vector3.up * 0.2f, new Color(0.6f, 0.52f, 0.42f), 60, 9f, 0.4f, 0.7f, 0.6f, Vector3.up, 80f);
            AbilityCaster.ShakeNear(p, 0.4f);
            Sfx.CrashAt(p);
        }
    }
}
