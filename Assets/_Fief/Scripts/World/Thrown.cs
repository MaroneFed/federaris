using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA BOULE DE GIVRE (la capacite Givre) : elle part en cloche, tombe sous son
    /// poids, et eclate la ou elle touche. Ceux qui sont dans l'eclat (sauf le lanceur)
    /// sont ralentis trois secondes.
    ///
    /// Pas de Rigidbody : on avance la boule a la main, image par image, et on lance
    /// un rayon sur le bout de chemin parcouru. C'est le plus simple, et ca ne rate
    /// jamais un mur fin.
    /// </summary>
    public class Thrown : MonoBehaviour
    {
        Seeker by;
        Vector3 velocity;
        float age;

        const float Gravity = -14f;
        public const float SlowSeconds = 3f;
        public const float SlowRadius = 4.5f;
        static readonly Color Frost = new Color(0.6f, 0.9f, 1f);

        public static void Launch(Seeker by, Vector3 from, Vector3 velocity)
        {
            GameObject go = new GameObject("Boule de givre");
            go.transform.position = from;
            Thrown t = go.AddComponent<Thrown>();
            t.by = by;
            t.velocity = velocity;
            Proto.BeginVisualOnly();
            GameObject ball = Proto.Sphere(go.transform, Vector3.zero, Vector3.one * 0.4f, Frost, "Givre");
            ball.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Frost, 2.5f);
            Proto.EndVisualOnly();
            Sfx.Whoosh();
        }

        /// <summary>La vitesse de lancer, pour viser a peu pres "reach" metres devant, en cloche.</summary>
        public static Vector3 Lob(Vector3 forward, float reach)
        {
            Vector3 flat = new Vector3(forward.x, 0f, forward.z).normalized;
            float up = Mathf.Clamp(forward.y, -0.3f, 0.8f);
            float speed = Mathf.Sqrt(Mathf.Max(1f, reach) * -Gravity * 0.75f);
            return (flat + Vector3.up * (0.35f + up)).normalized * speed;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            velocity += Vector3.up * Gravity * dt;
            Vector3 step = velocity * dt;
            // Un joueur sur la trajectoire : elle eclate sur lui.
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                if ((s.Body.position + Vector3.up - transform.position).magnitude < 1.2f) { Burst(transform.position); return; }
            }
            RaycastHit hit;
            if (Physics.Raycast(transform.position, step.normalized, out hit, step.magnitude + 0.05f, ~0, QueryTriggerInteraction.Ignore)
                && (by == null || by.Body == null || !hit.collider.transform.IsChildOf(by.Body)))
            {
                Burst(hit.point + hit.normal * 0.1f);
                return;
            }
            transform.position += step;
            if (age > 5f) Burst(transform.position);
        }

        void Burst(Vector3 at)
        {
            // L'eclat de givre : une sphere glacee, un anneau de gel au sol, une gerbe de cristaux.
            Fx.Shock(at + Vector3.up * 0.5f, Frost, SlowRadius, 0.45f);
            Fx.GroundRing(at, Frost, SlowRadius + 1f, 0.5f);
            Fx.Burst(at + Vector3.up * 0.3f, Frost, 70, 9f, 0.18f, 0.9f, 0.5f, Vector3.zero, 0f);
            Fx.Flash(at + Vector3.up, Frost, 10f, 4f, 0.35f);
            Sfx.Chip();
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                if ((s.Body.position - at).magnitude > SlowRadius) continue;
                s.SlowUntil = Time.time + SlowSeconds;
                if (s.IsPlayer && Game.Hud != null) Game.Hud.Flash(new Color(Frost.r, Frost.g, Frost.b, 0.6f));
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// LA FUMEE de la Nuee : dix metres de nuage gris qui tiennent huit secondes. Un Oeil
    /// ne voit pas au travers (Eye.Sees demande a Blocks), un bot non plus : c'est ce
    /// qui permet de passer sous leur nez -- ou de semer une poursuite.
    /// </summary>
    public static class Smoke
    {
        struct Cloud
        {
            public Vector3 at;
            public float until;
        }

        public const float Radius = 5f;
        public const float Seconds = 8f;

        static readonly List<Cloud> Clouds = new List<Cloud>();

        public static void Make(Vector3 at)
        {
            Cloud c;
            c.at = at + Vector3.up * 1.2f;
            c.until = Time.time + Seconds;
            Clouds.Add(c);
            Ambiance.SmokeCloud(c.at, Radius, Seconds);
            Sfx.Thud();
        }

        public static void Clear() { Clouds.Clear(); }

        /// <summary>Vrai si le regard de "a" vers "b" traverse un nuage.</summary>
        public static bool Blocks(Vector3 a, Vector3 b)
        {
            float now = Time.time;
            for (int i = Clouds.Count - 1; i >= 0; i--)
            {
                if (Clouds[i].until < now) { Clouds.RemoveAt(i); continue; }
                Vector3 c = Clouds[i].at;
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(c - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                if ((a + ab * t - c).magnitude < Radius) return true;
            }
            return false;
        }

        /// <summary>Vrai si ce point est dans un nuage.</summary>
        public static bool Inside(Vector3 p)
        {
            float now = Time.time;
            for (int i = 0; i < Clouds.Count; i++)
                if (Clouds[i].until >= now && (Clouds[i].at - p).magnitude < Radius) return true;
            return false;
        }
    }
}
