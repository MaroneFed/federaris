using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CAPACITES DIVINES (08/10 -- Martin : "une version God Mode, un bouton ou tu cliques et tu
    /// n'as que des gods capacites, des trucs de malade mental, de vraiment malade mental").
    /// Seulement en MODE DIEU (Match.GodMode). Ce qui vit quelques secondes dans le monde :
    ///
    ///   la BOMBE ATOMIQUE (2,5 s, puis tout saute a 30 m), le RAYON DIVIN (un laser qui balaie ce
    ///   que tu regardes), la MAIN DE DIEU (une main geante tombe du ciel sur ta cible).
    ///
    /// (L'Apocalypse, la Tempete, l'Essaim, le Temps arrete, la Gravite zero, la Teleportation et
    /// l'Invincible reutilisent les meteores, les tornades, les missiles, la glace, les ballons,
    /// le clignement et le geant : voir AbilityCaster.Cast.)
    /// Tout ce qui frappe passe par Combat.Hit / Combat.Blast, comme le reste.
    /// </summary>
    public class NukeBomb : MonoBehaviour
    {
        public const float Fuse = 2.5f;
        public const float Radius = 30f;
        Seeker by;
        float age, beep;
        Transform core;

        public static void Arm(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("BOMBE ATOMIQUE");
            go.transform.position = at;
            NukeBomb n = go.AddComponent<NukeBomb>();
            n.by = by;
            Color c = AbilityInfo.Tint(Ability.Nuke);
            Proto.BeginVisualOnly();
            GameObject ball = Proto.Sphere(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 1.2f), c, "Coeur");
            ball.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 1.5f);
            Proto.EndVisualOnly();
            n.core = ball.transform;
            Sfx.Alarm();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            Color c = AbilityInfo.Tint(Ability.Nuke);
            if (core != null)
            {
                float s = 1.2f + age * 1.4f + Mathf.Sin(age * 30f) * 0.15f;
                core.localScale = new Vector3(s, s, s);
            }
            beep -= dt;
            if (beep <= 0f && age < Fuse)
            {
                beep = Mathf.Lerp(0.5f, 0.08f, age / Fuse);
                Fx.GroundRing(p, c, Radius, 0.3f);
                Sfx.ChipAt(p);
            }
            if (age < Fuse) return;
            // BOUM : tout ce qui est a 30 m s'envole (pas celui qui l'a posee).
            Combat.Blast(p, Radius, 40f, 22f, by);
            Fx.Flash(p + Vector3.up * 4f, Color.white, 120f, 14f, 0.8f);
            Fx.Shock(p + Vector3.up * 2f, c, Radius, 0.9f);
            Fx.Shock(p + Vector3.up * 2f, Color.white, Radius * 0.5f, 0.5f);
            Fx.Column(p, c, 90f, 1.4f, 4f);
            Fx.Burst(p + Vector3.up * 2f, c, 400, 40f, 0.5f, 1.6f, -0.2f, Vector3.up, 60f);
            Fx.Burst(p + Vector3.up * 2f, new Color(0.35f, 0.3f, 0.3f), 200, 20f, 0.9f, 2.5f, -0.4f, Vector3.up, 30f);
            AbilityCaster.ShakeNear(p, 1.2f);
            Sfx.KoBoom(p, true);
            Sfx.CrashAt(p);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// LE RAYON DIVIN : trois secondes, un laser d'or part de ses yeux sur 90 m, la ou il regarde ;
    /// tout ce qu'il touche est projete (une fois par 0,6 s chacun).
    /// </summary>
    public class DivineBeam : MonoBehaviour
    {
        public const float Seconds = 3f;
        public const float Reach = 90f;
        Seeker who;
        float until;
        Transform beam;
        readonly Dictionary<Seeker, float> burnt = new Dictionary<Seeker, float>();

        public static void Fire(Seeker s)
        {
            DivineBeam b = s.Body.GetComponent<DivineBeam>();
            if (b == null) b = s.Body.gameObject.AddComponent<DivineBeam>();
            b.who = s;
            b.until = Time.time + Seconds;
            if (b.beam == null)
            {
                Color c = AbilityInfo.Tint(Ability.Rayon);
                Proto.BeginVisualOnly();
                GameObject go = Proto.Cylinder(null, Vector3.zero, new Vector3(0.6f, Reach * 0.5f, 0.6f), c, "Rayon");
                go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 2f);
                Proto.EndVisualOnly();
                b.beam = go.transform;
            }
            Sfx.Alarm();
        }

        Vector3 Aim()
        {
            if (who.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) return Game.Hud.orbitCamera.transform.forward;
            return who.Body.forward;
        }

        void Update()
        {
            if (who == null || who.Body == null || Time.time > until)
            {
                if (beam != null) Destroy(beam.gameObject);
                Destroy(this);
                return;
            }
            Vector3 eye = who.Body.position + Vector3.up * 1.5f;
            Vector3 dir = Aim().normalized;
            // Le rayon part un peu devant les yeux (sinon, en premiere personne, il remplit l'ecran).
            Vector3 from = eye + dir * 1.5f;
            float length = Reach;
            RaycastHit hit;
            if (Physics.Raycast(from, dir, out hit, Reach, ~0, QueryTriggerInteraction.Ignore)) length = hit.distance;
            if (beam != null)
            {
                float w = 0.5f + 0.15f * Mathf.Sin(Time.time * 40f);
                beam.position = from + dir * length * 0.5f;
                beam.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                beam.localScale = new Vector3(w, length * 0.5f, w);
            }
            if (length < Reach) Fx.Burst(from + dir * length, AbilityInfo.Tint(Ability.Rayon), 6, 8f, 0.2f, 0.4f, 0.3f, -dir, 60f);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == who || s.Body == null) continue;
                Vector3 c = s.Body.position + Vector3.up;
                float along = Vector3.Dot(c - from, dir);
                if (along < 0f || along > length) continue;
                if ((from + dir * along - c).magnitude > 2f) continue;
                float last;
                if (burnt.TryGetValue(s, out last) && Time.time - last < 0.6f) continue;
                burnt[s] = Time.time;
                Combat.Hit(s, Combat.Flat(dir).normalized * 30f + Vector3.up * 12f, 0.4f, true, who);
            }
        }

        void OnDestroy() { if (beam != null) Destroy(beam.gameObject); }
    }

    /// <summary>
    /// LA MAIN DE DIEU : une ombre au sol sous la cible (0,7 s), puis une main geante tombe du ciel
    /// et l'envoie valser vers le vide -- loin du centre de l'ile.
    /// </summary>
    public class HandOfGod : MonoBehaviour
    {
        const float Fall = 0.7f;
        Seeker by, target;
        float age;
        Transform hand;

        public static void Strike(Seeker by, Seeker target)
        {
            if (target == null || target.Body == null) return;
            GameObject go = new GameObject("MAIN DE DIEU");
            go.transform.position = target.Body.position;
            HandOfGod h = go.AddComponent<HandOfGod>();
            h.by = by;
            h.target = target;
            Color c = AbilityInfo.Tint(Ability.MainDeDieu);
            Proto.BeginVisualOnly();
            GameObject palm = Proto.Cube(null, target.Body.position + Vector3.up * 30f, new Vector3(4f, 1.2f, 5f), c, "Main");
            palm.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.7f, 0.3f, 0.6f);
            for (int i = 0; i < 4; i++)
                Proto.Capsule(palm.transform, new Vector3(-0.36f + i * 0.24f, 0f, 0.62f), new Vector3(0.18f, 0.35f, 0.18f), c, "Doigt")
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Proto.Capsule(palm.transform, new Vector3(0.6f, 0f, -0.1f), new Vector3(0.2f, 0.3f, 0.2f), c, "Pouce")
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Proto.EndVisualOnly();
            h.hand = palm.transform;
            Sfx.WhooshAt(target.Body.position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (target == null || target.Body == null) { Clean(); return; }
            Vector3 p = target.Body.position;
            if (age < Fall)
            {
                transform.position = p;
                Fx.GroundRing(p, new Color(0.1f, 0.08f, 0.15f), 2.5f, 0.1f);
                if (hand != null) hand.position = p + Vector3.up * Mathf.Lerp(30f, 2.5f, age / Fall);
                return;
            }
            // La main frappe : vers le vide, loin du centre de l'ile.
            Vector3 out1 = Combat.Flat(p);
            out1 = out1.sqrMagnitude > 0.01f ? out1.normalized : Vector3.forward;
            Combat.Hit(target, out1 * 42f + Vector3.up * 20f, 0.7f, true, by);
            Color c = AbilityInfo.Tint(Ability.MainDeDieu);
            Fx.Shock(p + Vector3.up, c, 6f, 0.4f);
            Fx.Burst(p + Vector3.up, c, 150, 20f, 0.3f, 0.9f, 0.3f, Vector3.up, 80f);
            Fx.Flash(p + Vector3.up * 2f, c, 30f, 10f, 0.4f);
            AbilityCaster.ShakeNear(p, 0.6f);
            Sfx.KoBoom(p, (by != null && by.IsPlayer) || target.IsPlayer);
            Clean();
        }

        void Clean()
        {
            if (hand != null) Destroy(hand.gameObject, 0.4f);
            Destroy(gameObject);
        }
    }

    /// <summary>LE BOMBARDEMENT : douze explosions en ligne, de 6 a 45 m devant, en 2 s.</summary>
    public class BombCarpet : MonoBehaviour
    {
        Seeker by;
        Vector3 from, dir;
        float age;
        int done;
        const int Count = 12;

        public static void Drop(Seeker by, Vector3 from, Vector3 dir)
        {
            GameObject go = new GameObject("BOMBARDEMENT");
            BombCarpet b = go.AddComponent<BombCarpet>();
            b.by = by;
            b.from = from;
            b.dir = dir;
            Sfx.Alarm();
        }

        void Update()
        {
            age += Time.deltaTime;
            while (done < Count && age > 0.4f + done * 0.14f)
            {
                Vector3 p = from + dir * (6f + done * 3.5f) + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                RaycastHit hit;
                if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out hit, 80f, ~0, QueryTriggerInteraction.Ignore)) p = hit.point;
                Combat.Blast(p, 5f, 26f, 14f, by);
                Color c = AbilityInfo.Tint(Ability.Bombardement);
                Fx.Shock(p + Vector3.up, c, 5f, 0.3f);
                Fx.Burst(p + Vector3.up * 0.5f, c, 70, 14f, 0.25f, 0.7f, 0.3f, Vector3.up, 60f);
                AbilityCaster.ShakeNear(p, 0.25f);
                Sfx.CrashAt(p);
                done++;
            }
            if (done >= Count) Destroy(gameObject);
        }
    }

    /// <summary>LA SINGULARITE : un trou noir geant -- trois aspirations a 45 m, puis une explosion de 18 m.</summary>
    public class Singularity : MonoBehaviour
    {
        Seeker by;
        float age, ringT;
        int pulls;

        public static void Open(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("SINGULARITE");
            go.transform.position = at;
            Singularity v = go.AddComponent<Singularity>();
            v.by = by;
            Proto.BeginVisualOnly();
            GameObject core = Proto.Sphere(go.transform, new Vector3(0f, 3f, 0f), new Vector3(4f, 4f, 4f), Color.black, "Coeur");
            core.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Get(new Color(0.02f, 0f, 0.05f));
            Proto.EndVisualOnly();
            Sfx.WhooshAt(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 c = transform.position + Vector3.up * 3f;
            Color tint = AbilityInfo.Tint(Ability.Singularite);
            ringT -= dt;
            if (ringT <= 0f && age < 2.4f) { ringT = 0.12f; Fx.Ring(c, tint, 45f, 1f, 0.5f, 0.3f, Vector3.up); }
            if (pulls < 3 && age > 0.2f + pulls * 0.7f)
            {
                pulls++;
                for (int i = 0; i < Game.Seekers.Count; i++)
                {
                    Seeker s = Game.Seekers[i];
                    if (s == by || s.Body == null) continue;
                    Vector3 d = transform.position - s.Body.position;
                    float flat = new Vector2(d.x, d.z).magnitude;
                    if (flat > 45f || Mathf.Abs(d.y) > 15f || flat < 1f) continue;
                    Vector3 toward = new Vector3(d.x, 0f, d.z) / flat;
                    Combat.Hit(s, toward * Mathf.Clamp(flat * 1.1f, 10f, 34f) + Vector3.up * 6f, 0.2f, false, by);
                }
            }
            if (age < 2.5f) return;
            Combat.Blast(transform.position, 18f, 42f, 22f, by);
            Fx.Shock(c, tint, 18f, 0.6f);
            Fx.Shock(c, Color.white, 9f, 0.35f);
            Fx.Burst(c, tint, 300, 34f, 0.3f, 1.1f, 0f, Vector3.zero, 0f);
            Fx.Flash(c, tint, 60f, 12f, 0.6f);
            AbilityCaster.ShakeNear(c, 0.9f);
            Sfx.KoBoom(c, by != null && by.IsPlayer);
            Destroy(gameObject);
        }
    }

    /// <summary>LE SOUFFLE DU DRAGON : trois secondes, un cone de feu de 25 m la ou il regarde.</summary>
    public class DragonBreath : MonoBehaviour
    {
        Seeker who;
        float until, puff;
        readonly Dictionary<Seeker, float> burnt = new Dictionary<Seeker, float>();

        public static void Breathe(Seeker s)
        {
            DragonBreath d = s.Body.GetComponent<DragonBreath>();
            if (d == null) d = s.Body.gameObject.AddComponent<DragonBreath>();
            d.who = s;
            d.until = Time.time + 3f;
            Sfx.WhooshAt(s.Body.position);
        }

        void Update()
        {
            if (who == null || who.Body == null || Time.time > until) { Destroy(this); return; }
            Vector3 dir = who.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null ? Game.Hud.orbitCamera.transform.forward : who.Body.forward;
            Vector3 flat = Combat.Flat(dir).normalized;
            Vector3 mouth = who.Body.position + Vector3.up * 1.4f + flat * 1.5f;
            puff -= Time.deltaTime;
            if (puff <= 0f)
            {
                puff = 0.05f;
                Color c = Random.value < 0.5f ? AbilityInfo.Tint(Ability.Dragon) : new Color(1f, 0.85f, 0.3f);
                Fx.Burst(mouth, c, 12, 30f, 0.6f, 0.7f, -0.3f, dir, 18f);
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == who || s.Body == null || !Combat.InArc(who.Body.position, flat, s.Body.position, 25f, 35f)) continue;
                float last;
                if (burnt.TryGetValue(s, out last) && Time.time - last < 0.6f) continue;
                burnt[s] = Time.time;
                Combat.Hit(s, flat * 20f + Vector3.up * 10f, 0.3f, true, who);
            }
        }
    }

    /// <summary>LA COMETE : une cible geante au sol (1,5 s), une boule de feu qui tombe du ciel, et BOUM (18 m).</summary>
    public class CometStrike : MonoBehaviour
    {
        Seeker by;
        float age, ring;
        Transform rock;
        const float Fall = 1.5f;

        public static void Call(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("COMETE");
            go.transform.position = at;
            CometStrike c = go.AddComponent<CometStrike>();
            c.by = by;
            Color t = AbilityInfo.Tint(Ability.Comete);
            Proto.BeginVisualOnly();
            GameObject r = Proto.Sphere(null, at + Vector3.up * 120f, new Vector3(9f, 9f, 9f), t, "Comete");
            r.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(t, 2f);
            Proto.EndVisualOnly();
            c.rock = r.transform;
            Sfx.Alarm();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            Color t = AbilityInfo.Tint(Ability.Comete);
            ring -= dt;
            if (ring <= 0f && age < Fall) { ring = 0.15f; Fx.GroundRing(p, t, 18f, 0.15f); }
            if (rock != null)
            {
                rock.position = p + Vector3.up * Mathf.Lerp(120f, 0f, age / Fall);
                Fx.Burst(rock.position, t, 6, 6f, 1.2f, 0.6f, 0f, Vector3.up, 40f);
            }
            if (age < Fall) return;
            if (rock != null) Destroy(rock.gameObject);
            Combat.Blast(p, 18f, 45f, 25f, by);
            Fx.Shock(p + Vector3.up, t, 18f, 0.6f);
            Fx.Burst(p + Vector3.up, t, 300, 36f, 0.4f, 1.2f, 0.3f, Vector3.up, 80f);
            Fx.Flash(p + Vector3.up * 3f, t, 80f, 12f, 0.6f);
            Fx.Column(p, t, 80f, 0.8f, 6f);
            AbilityCaster.ShakeNear(p, 1f);
            Sfx.KoBoom(p, by != null && by.IsPlayer);
            Destroy(gameObject);
        }

        void OnDestroy() { if (rock != null) Destroy(rock.gameObject); }
    }

    /// <summary>L'ANNEAU DE FEU : 5 s, un cercle de flammes de 12 m autour de son lanceur ; qui le traverse est ejecte.</summary>
    public class FireRingZone : MonoBehaviour
    {
        const float Radius = 12f;
        Seeker by;
        float age, puff;
        readonly Dictionary<Seeker, float> burnt = new Dictionary<Seeker, float>();

        public static void Light(Seeker by, Vector3 at)
        {
            GameObject go = new GameObject("ANNEAU DE FEU");
            go.transform.position = at;
            FireRingZone f = go.AddComponent<FireRingZone>();
            f.by = by;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age > 5f) { Destroy(gameObject); return; }
            Vector3 c = transform.position;
            Color t = AbilityInfo.Tint(Ability.AnneauFeu);
            puff -= Time.deltaTime;
            if (puff <= 0f)
            {
                puff = 0.08f;
                for (int k = 0; k < 6; k++)
                {
                    float a = Random.value * Mathf.PI * 2f;
                    Fx.Burst(c + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * Radius, Random.value < 0.5f ? t : new Color(1f, 0.85f, 0.3f), 3, 3f, 0.5f, 0.6f, -0.8f, Vector3.up, 20f);
                }
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                Vector3 d = s.Body.position - c;
                float flat = new Vector2(d.x, d.z).magnitude;
                if (Mathf.Abs(d.y) > 4f || Mathf.Abs(flat - Radius) > 1.3f) continue;
                float last;
                if (burnt.TryGetValue(s, out last) && Time.time - last < 0.8f) continue;
                burnt[s] = Time.time;
                Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                Combat.Hit(s, away * 26f + Vector3.up * 12f, 0.3f, true, by);
            }
        }
    }

    /// <summary>LE CORPS DE LAVE (passive divine) : qui le touche (2,2 m) est projete, une fois par 0,8 s.</summary>
    public class LavaBody : MonoBehaviour
    {
        Seeker who;
        float puff;
        readonly Dictionary<Seeker, float> burnt = new Dictionary<Seeker, float>();

        public static void Keep(Seeker s)
        {
            if (s == null || s.Body == null || s.Body.GetComponent<LavaBody>() != null) return;
            LavaBody l = s.Body.gameObject.AddComponent<LavaBody>();
            l.who = s;
        }

        void Update()
        {
            if (who == null || who.Body == null || !who.Has(Ability.Lave)) { Destroy(this); return; }
            puff -= Time.deltaTime;
            if (puff <= 0f)
            {
                puff = 0.12f;
                Fx.Burst(who.Body.position + Vector3.up * Random.Range(0.3f, 1.8f), AbilityInfo.Tint(Ability.Lave), 2, 1.5f, 0.25f, 0.5f, -0.5f, Vector3.up, 40f);
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == who || s.Body == null) continue;
                Vector3 d = s.Body.position - who.Body.position;
                if (Mathf.Abs(d.y) > 2.4f || new Vector2(d.x, d.z).magnitude > 2.2f) continue;
                float last;
                if (burnt.TryGetValue(s, out last) && Time.time - last < 0.8f) continue;
                burnt[s] = Time.time;
                Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : who.Body.forward;
                Combat.Hit(s, away * 18f + Vector3.up * 8f, 0.2f, true, who);
            }
        }
    }

    /// <summary>L'ECHO (passive divine) : la capacite active repart une seconde fois, 0,5 s apres.</summary>
    public class EchoCast : MonoBehaviour
    {
        public static bool Echoing;
        Seeker who;
        Ability what;
        float at;

        public static void Schedule(Seeker s, Ability a)
        {
            EchoCast e = s.Body.gameObject.AddComponent<EchoCast>();
            e.who = s;
            e.what = a;
            e.at = Time.time + 0.5f;
        }

        void Update()
        {
            if (who == null || who.Body == null) { Destroy(this); return; }
            if (Time.time < at) return;
            Vector3 eye = who.Body.position + Vector3.up * 1.6f;
            Vector3 aim = who.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null ? Game.Hud.orbitCamera.transform.forward : who.Body.forward;
            Echoing = true;
            try { AbilityCaster.Cast(who, what, eye, aim); }
            finally { Echoing = false; }
            Destroy(this);
        }
    }
}
