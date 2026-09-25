using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON LANCE : le fumigene et la fiole de lenteur. Une petite boule qui part
    /// en cloche, tombe sous son poids, et eclate la ou elle touche (sol, mur, tronc).
    ///
    /// Pas de Rigidbody : on avance la boule a la main, image par image, et on lance
    /// un rayon sur le bout de chemin parcouru. Si le rayon touche quelque chose, c'est
    /// la qu'elle eclate. C'est le plus simple, et ca ne rate jamais un mur fin.
    ///
    /// Toi et les bots passez par Launch : en Phase 3, c'est l'hote qui l'appellera.
    /// </summary>
    public class Thrown : MonoBehaviour
    {
        Seeker by;
        Item kind;
        Vector3 velocity;
        float age;

        const float Gravity = -16f;
        public const float SlowSeconds = 5f;
        public const float SlowRadius = 3.8f;

        /// <summary>Lancer "kind" depuis "from", a la vitesse "velocity".</summary>
        public static void Launch(Seeker by, Item kind, Vector3 from, Vector3 velocity)
        {
            GameObject go = new GameObject(ItemInfo.Name(kind) + " (lancé)");
            go.transform.position = from;
            Thrown t = go.AddComponent<Thrown>();
            t.by = by;
            t.kind = kind;
            t.velocity = velocity;
            Proto.BeginVisualOnly();
            GameObject ball = Proto.Sphere(go.transform, Vector3.zero, Vector3.one * 0.22f, ItemInfo.Tint(kind), "Boule");
            ball.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(ItemInfo.Tint(kind), kind == Item.Lenteur ? 2.2f : 0.6f);
            Proto.EndVisualOnly();
            Sfx.Whoosh();
        }

        /// <summary>La vitesse de lancer, pour viser a peu pres "reach" metres devant, en cloche.</summary>
        public static Vector3 Lob(Vector3 forward, float reach)
        {
            Vector3 flat = new Vector3(forward.x, 0f, forward.z).normalized;
            float up = Mathf.Clamp(forward.y, -0.3f, 0.8f);
            float speed = Mathf.Sqrt(Mathf.Max(1f, reach) * -Gravity * 0.75f);
            return (flat + Vector3.up * (0.45f + up)).normalized * speed;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            velocity += Vector3.up * Gravity * dt;
            Vector3 step = velocity * dt;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, step.normalized, out hit, step.magnitude + 0.05f, ~0, QueryTriggerInteraction.Ignore)
                && (by == null || by.Body == null || !hit.collider.transform.IsChildOf(by.Body)))
            {
                Land(hit.point + hit.normal * 0.1f);
                return;
            }
            transform.position += step;
            transform.Rotate(400f * dt, 200f * dt, 0f);
            if (age > 5f || transform.position.y < Ground.Sample(transform.position.x, transform.position.z) - 1f)
                Land(transform.position);
        }

        void Land(Vector3 at)
        {
            if (kind == Item.Fumigene) Smoke.Make(at);
            else if (kind == Item.Lenteur) Splash(at);
            Destroy(gameObject);
        }

        /// <summary>La fiole eclate : qui est dans la flaque (sauf le lanceur) est ralenti.</summary>
        void Splash(Vector3 at)
        {
            Color purple = ItemInfo.Tint(Item.Lenteur);
            Ambiance.Burst(null, at + Vector3.up * 0.3f, purple);
            Sfx.Chip();
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || !s.Alive || s.Body == null) continue;
                if ((s.Body.position - at).magnitude > SlowRadius) continue;
                s.SlowUntil = Time.time + SlowSeconds;
                if (s.IsPlayer && Game.Hud != null) Game.Hud.Flash(purple);
            }
            // Une flaque luisante, qui s'efface.
            Proto.BeginVisualOnly();
            GameObject pool = Proto.Cylinder(null, Ground.Place(at.x, at.z, 0.03f), new Vector3(SlowRadius * 1.6f, 0.01f, SlowRadius * 1.6f), purple, "Flaque");
            pool.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(purple * 0.6f, 0.8f);
            Proto.EndVisualOnly();
            Destroy(pool, SlowSeconds);
        }
    }

    /// <summary>
    /// LA FUMEE du fumigene : dix metres de nuage gris qui tiennent douze secondes. Un
    /// garde ne voit pas au travers (Guard.InSight demande a Blocks) : c'est ce qui
    /// permet de lui passer sous le nez -- ou de semer une poursuite.
    /// </summary>
    public static class Smoke
    {
        struct Cloud
        {
            public Vector3 at;
            public float until;
        }

        public const float Radius = 5f;
        public const float Seconds = 12f;

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
