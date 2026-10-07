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
            // (v33) La zone qui va sauter : un disque qui se remplit jusqu'au BOUM.
            DivineFx.Mark(at, Radius, c, Fuse);
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
            DivineFx.Impact(p, Radius, c, 3f, true);
            DivineFx.Mushroom(p, 7f, c);
            Fx.Flash(p + Vector3.up * 4f, Color.white, 120f, 14f, 0.8f);
            Fx.Column(p, c, 90f, 1.4f, 4f);
            Sfx.CrashAt(p);
            if (Game.Hud != null) Game.Hud.Flash(new Color(1f, 0.95f, 0.8f, 0.35f));
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
        float until, scorch;
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
                // (v33) Le coeur blanc du rayon, plus fin, dans le rayon de couleur.
                Proto.BeginVisualOnly();
                GameObject core = Proto.Cylinder(go.transform, Vector3.zero, new Vector3(0.4f, 1f, 0.4f), Color.white, "Coeur");
                core.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Color.white, 3f);
                Proto.EndVisualOnly();
            }
            DivineFx.Halo(s.Body, AbilityInfo.Tint(Ability.Rayon), Seconds, 1.1f);
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
            if (length < Reach)
            {
                Vector3 spot = from + dir * length;
                Fx.Burst(spot, AbilityInfo.Tint(Ability.Rayon), 6, 8f, 0.2f, 0.4f, 0.3f, -dir, 60f);
                scorch -= Time.deltaTime;
                if (scorch <= 0f)
                {
                    scorch = 0.12f;
                    DivineFx.Scorch(spot, 1.1f, 3f);
                    DivineFx.Debris(spot, 2, AbilityInfo.Tint(Ability.Rayon), 6f);
                    DivineFx.Smoke(spot, 1, 0.8f, 2f);
                }
            }
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
            DivineFx.Mark(target.Body.position, 3f, new Color(0.25f, 0.2f, 0.35f), Fall, target.Body);
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
                // Elle tombe de plus en plus vite (au carre), et grossit en approchant.
                float k = age / Fall;
                if (hand != null) { hand.position = p + Vector3.up * Mathf.Lerp(30f, 2.5f, k * k); hand.localScale = new Vector3(4f, 1.2f, 5f) * Mathf.Lerp(0.6f, 1.15f, k); }
                return;
            }
            // La main frappe : vers le vide, loin du centre de l'ile.
            Vector3 out1 = Combat.Flat(p);
            out1 = out1.sqrMagnitude > 0.01f ? out1.normalized : Vector3.forward;
            Combat.Hit(target, out1 * 42f + Vector3.up * 20f, 0.7f, true, by);
            Color c = AbilityInfo.Tint(Ability.MainDeDieu);
            DivineFx.Impact(p, 6f, c, 1.5f, (by != null && by.IsPlayer) || target.IsPlayer);
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
        // (v33) Chaque bombe a sa CIBLE au sol, et on la VOIT tomber du ciel (avant : des
        // explosions qui sortaient du sol sans prevenir).
        Seeker by;
        float age;
        const int Count = 12;
        const float FallTime = 0.45f;
        readonly Vector3[] spots = new Vector3[Count];
        readonly float[] at = new float[Count];
        readonly Transform[] bombs = new Transform[Count];
        readonly bool[] done = new bool[Count];

        public static void Drop(Seeker by, Vector3 from, Vector3 dir)
        {
            GameObject go = new GameObject("BOMBARDEMENT");
            BombCarpet b = go.AddComponent<BombCarpet>();
            b.by = by;
            Color c = AbilityInfo.Tint(Ability.Bombardement);
            for (int i = 0; i < Count; i++)
            {
                Vector3 p = from + dir * (6f + i * 3.5f) + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                p = DivineFx.OnGround(p);
                b.spots[i] = p;
                b.at[i] = 0.7f + i * 0.14f;
                DivineFx.Mark(p, 5f, c, b.at[i]);
            }
            Sfx.Alarm();
        }

        void Update()
        {
            age += Time.deltaTime;
            Color c = AbilityInfo.Tint(Ability.Bombardement);
            bool all = true;
            for (int i = 0; i < Count; i++)
            {
                if (done[i]) continue;
                all = false;
                float left = at[i] - age;
                if (left > FallTime) continue;
                if (bombs[i] == null)
                {
                    Proto.BeginVisualOnly();
                    GameObject g = Proto.Capsule(null, spots[i] + Vector3.up * 30f, new Vector3(0.9f, 1.2f, 0.9f), new Color(0.15f, 0.14f, 0.18f), "Bombe");
                    Proto.Sphere(g.transform, new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 0.25f, 1.4f), new Color(0.25f, 0.24f, 0.3f), "Ailette");
                    Proto.EndVisualOnly();
                    bombs[i] = g.transform;
                }
                bombs[i].position = spots[i] + Vector3.up * (30f * Mathf.Max(0f, left) / FallTime);
                Fx.Burst(bombs[i].position + Vector3.up, new Color(0.6f, 0.6f, 0.65f), 1, 1f, 0.5f, 0.5f, 0f, Vector3.up, 20f);
                if (left > 0f) continue;
                done[i] = true;
                Destroy(bombs[i].gameObject);
                Combat.Blast(spots[i], 5f, 26f, 14f, by);
                DivineFx.Impact(spots[i], 5f, c, 0.8f, false);
            }
            if (all) Destroy(gameObject);
        }

        void OnDestroy()
        {
            for (int i = 0; i < Count; i++) if (bombs[i] != null) Destroy(bombs[i].gameObject);
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
            // (v35) Un coeur qui BRILLE (violet et blanc) : la boule noire faisait une tache noire.
            GameObject core = Proto.Sphere(go.transform, new Vector3(0f, 3f, 0f), new Vector3(4f, 4f, 4f), AbilityInfo.Tint(Ability.Singularite), "Coeur");
            core.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Color.Lerp(AbilityInfo.Tint(Ability.Singularite), Color.white, 0.3f), 2.5f);
            Proto.EndVisualOnly();
            v.core = core.transform;
            DivineFx.Mark(at, 45f, AbilityInfo.Tint(Ability.Singularite), 2.5f);
            Sfx.WhooshAt(at);
        }

        Transform core;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 c = transform.position + Vector3.up * 3f;
            Color tint = AbilityInfo.Tint(Ability.Singularite);
            ringT -= dt;
            if (ringT <= 0f && age < 2.4f)
            {
                ringT = 0.12f;
                Fx.Ring(c, tint, 45f, 1f, 0.5f, 0.3f, Vector3.up);
                // (v33) Des eclairs violets du bord vers le coeur, et des pierres arrachees au sol.
                float a = Random.value * Mathf.PI * 2f;
                Vector3 edge = DivineFx.OnGround(transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(10f, 40f));
                DivineFx.Bolt(edge + Vector3.up * 0.5f, c, tint, 0.4f, 0.25f);
                DivineFx.Debris(edge + Vector3.up * 0.5f, 1, tint, 4f);
            }
            if (core != null)
            {
                float grow = Mathf.Lerp(4f, 9f, Mathf.Clamp01(age / 2.4f)) * (1f + 0.06f * Mathf.Sin(age * 30f));
                core.localScale = Vector3.one * grow;
                core.Rotate(0f, 300f * dt, 0f);
            }
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
            DivineFx.Impact(transform.position, 18f, tint, 2.2f, by != null && by.IsPlayer);
            Fx.Burst(c, tint, 300, 34f, 0.3f, 1.1f, 0f, Vector3.zero, 0f);
            if (core != null) Destroy(core.gameObject);
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
            DivineFx.Halo(s.Body, AbilityInfo.Tint(Ability.Dragon), 3f, 1f);
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
                Fx.Burst(mouth, Color.white, 4, 22f, 0.35f, 0.4f, -0.2f, dir, 8f);
                // (v33) Le sol brule la ou le feu passe, et la fumee monte.
                if (Random.value < 0.35f)
                {
                    Vector3 spot = who.Body.position + Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * flat * Random.Range(6f, 22f);
                    DivineFx.Scorch(spot, Random.Range(1f, 2f), 4f);
                    DivineFx.Smoke(DivineFx.OnGround(spot), 1, 1.2f, 2.5f);
                }
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
        const float Fall = 2.2f;     // (v43, le clipper) 2,2 s : on l'entend arriver (1,5 s avant)

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
            DivineFx.Mark(at, 18f, t, Fall);
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
                if (Random.value < 0.5f) DivineFx.Smoke(rock.position + Vector3.up * 4f, 1, 3f, 0.5f);
            }
            if (age < Fall) return;
            if (rock != null) Destroy(rock.gameObject);
            Combat.Blast(p, 18f, 45f, 25f, by);
            DivineFx.Impact(p, 18f, t, 2.4f, by != null && by.IsPlayer);
            Fx.Column(p, t, 80f, 0.8f, 6f);
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
            // (v33) UN VRAI MUR DE FLAMMES : 28 langues de feu qui dansent sur le cercle (avant,
            // des etincelles eparses -- on ne voyait pas ou etait le cercle).
            Color t = AbilityInfo.Tint(Ability.AnneauFeu);
            Proto.BeginVisualOnly();
            for (int k = 0; k < 28; k++)
            {
                float a = k / 28f * Mathf.PI * 2f;
                Vector3 p = DivineFx.OnGround(at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Radius);
                GameObject flame = Proto.Capsule(null, p + Vector3.up, new Vector3(0.9f, 1.2f, 0.9f), t, "Flamme");
                flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(k % 3 == 0 ? new Color(1f, 0.85f, 0.3f) : t, 2.2f);
                flame.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                f.flames.Add(flame.transform);
            }
            Proto.EndVisualOnly();
            DivineFx.Scorch(at, Radius + 1f, 7f);
            Fx.Flash(at + Vector3.up * 2f, t, 30f, 6f, 0.4f);
        }

        readonly List<Transform> flames = new List<Transform>();

        void OnDestroy() { for (int i = 0; i < flames.Count; i++) if (flames[i] != null) Destroy(flames[i].gameObject); }

        void Update()
        {
            age += Time.deltaTime;
            if (age > 5f) { Destroy(gameObject); return; }
            Vector3 c = transform.position;
            Color t = AbilityInfo.Tint(Ability.AnneauFeu);
            float rise = Mathf.Clamp01(age / 0.3f) * Mathf.Clamp01((5f - age) / 0.4f);
            for (int k = 0; k < flames.Count; k++)
            {
                if (flames[k] == null) continue;
                float h = rise * (1.2f + 0.5f * Mathf.Sin(age * 9f + k * 1.7f) + 0.3f * Mathf.Sin(age * 23f + k));
                flames[k].localScale = new Vector3(0.9f * rise, Mathf.Max(0.01f, h), 0.9f * rise);
            }
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
