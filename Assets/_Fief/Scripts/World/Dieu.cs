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
}
