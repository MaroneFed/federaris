using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Un animal : il broute, il se deplace, et il fuit quand tu approches.
    ///
    /// Rien de tout cela n'est du gameplay. C'est la pour une seule raison : un monde
    /// ou rien ne bouge est un decor, un monde ou quelque chose detale quand tu
    /// arrives est un endroit.
    /// </summary>
    public class Animal : MonoBehaviour
    {
        public float walkSpeed = 2.2f;
        public float runSpeed = 9f;
        public float fleeRadius = 26f;
        public float wanderRadius = 40f;

        Vector3 home;
        Vector3 target;
        float retarget;
        float bob;
        Transform body;

        public void Initialise(Vector3 origin, Transform bodyTransform, System.Random rng)
        {
            home = origin;
            body = bodyTransform;
            target = origin;
            retarget = (float)rng.NextDouble() * 4f;
            bob = (float)rng.NextDouble() * 6.283f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool fleeing = false;
            Vector3 position = transform.position;

            if (Game.PlayerTransform != null)
            {
                Vector3 away = position - Game.PlayerTransform.position;
                away.y = 0f;
                if (away.sqrMagnitude < fleeRadius * fleeRadius)
                {
                    fleeing = true;
                    target = position + away.normalized * 40f;
                }
            }

            if (!fleeing)
            {
                retarget -= dt;
                if (retarget <= 0f)
                {
                    retarget = 3f + Random.value * 6f;
                    Vector2 offset = Random.insideUnitCircle * wanderRadius;
                    target = home + new Vector3(offset.x, 0f, offset.y);
                }
            }

            Vector3 delta = target - position;
            delta.y = 0f;
            float distance = delta.magnitude;

            float speed = fleeing ? runSpeed : walkSpeed;
            bool moving = distance > 1.2f;

            if (moving)
            {
                Vector3 direction = delta / distance;
                position += direction * speed * dt;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 300f * dt);
            }

            // On reste colle au sol, et on ne quitte pas la carte.
            GameConfig cfg = Game.Config;
            float limit = cfg != null ? cfg.mapSize * 0.5f - 30f : 380f;
            position.x = Mathf.Clamp(position.x, -limit, limit);
            position.z = Mathf.Clamp(position.z, -limit, limit);
            position.y = Ground.Height(position.x, position.z);
            transform.position = position;

            // petit balancement : ca suffit a donner l'illusion de la marche
            if (body != null)
            {
                bob += (moving ? speed * 2.4f : 1.2f) * dt;
                float amount = moving ? 0.08f : 0.02f;
                body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bob)) * amount, 0f);
                body.localRotation = Quaternion.Euler(Mathf.Sin(bob * 0.5f) * (moving ? 5f : 1.5f), 0f, 0f);
            }
        }
    }

    /// <summary>Fabrique les troupeaux et les vols d'oiseaux.</summary>
    public static class Wildlife
    {
        public static void Populate(Transform parent, GameConfig cfg, System.Random rng,
                                    List<Vector3> occupied, int herds)
        {
            GameObject root = new GameObject("Faune");
            root.transform.SetParent(parent, false);

            float half = cfg.mapSize * 0.5f - 90f;
            int built = 0;
            int guard = 0;

            while (built < herds && guard < herds * 40)
            {
                guard++;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * half;

                if (Ground.Height(x, z) < 0.5f) continue;
                if (Ground.Slope(x, z) > 0.3f) continue;
                if (!Scenery.IsFree(x, z, occupied, 40f, 20f)) continue;

                bool deer = rng.Next(2) == 0;
                int size = deer ? 2 + rng.Next(3) : 4 + rng.Next(5);

                for (int i = 0; i < size; i++)
                {
                    float ox = x + ((float)rng.NextDouble() - 0.5f) * 22f;
                    float oz = z + ((float)rng.NextDouble() - 0.5f) * 22f;
                    Spawn(root.transform, new Vector3(ox, Ground.Height(ox, oz), oz), deer, rng);
                }
                built++;
            }

            // les oiseaux : deux vols qui tournent lentement au-dessus du monde
            for (int i = 0; i < 2; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = cfg.mapSize * 0.22f;
                Flock(root.transform, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), rng);
            }
        }

        static void Spawn(Transform parent, Vector3 at, bool deer, System.Random rng)
        {
            GameObject go = new GameObject(deer ? "Cerf" : "Mouton");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            GameObject body = new GameObject("Corps");
            body.transform.SetParent(go.transform, false);

            Proto.BeginVisualOnly();
            if (deer)
            {
                Color hide = new Color(0.52f, 0.36f, 0.22f);
                Proto.Cube(body.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.62f, 0.66f, 1.5f), hide, "Tronc");
                Proto.Cube(body.transform, new Vector3(0f, 1.5f, 0.82f), new Vector3(0.38f, 0.42f, 0.5f), hide, "Tete");
                Proto.Cube(body.transform, new Vector3(0f, 1.32f, 0.62f), new Vector3(0.24f, 0.42f, 0.24f), hide, "Cou");
                for (int i = 0; i < 4; i++)
                {
                    float lx = (i % 2 == 0) ? -0.23f : 0.23f;
                    float lz = (i < 2) ? -0.5f : 0.52f;
                    Proto.Cube(body.transform, new Vector3(lx, 0.37f, lz), new Vector3(0.14f, 0.74f, 0.14f),
                               Palette.Shade(hide, 0.75f), "Patte");
                }
                for (int i = -1; i <= 1; i += 2)
                {
                    GameObject horn = Proto.Cube(body.transform, new Vector3(i * 0.14f, 1.82f, 0.78f),
                                                 new Vector3(0.08f, 0.5f, 0.08f),
                                                 Palette.Shade(hide, 1.5f), "Bois");
                    horn.transform.localRotation = Quaternion.Euler(-18f, 0f, i * 22f);
                }
                Proto.Cube(body.transform, new Vector3(0f, 1.2f, -0.8f), new Vector3(0.2f, 0.24f, 0.16f),
                           new Color(0.92f, 0.9f, 0.85f), "Queue");
            }
            else
            {
                Color wool = new Color(0.9f, 0.89f, 0.84f);
                Proto.Cube(body.transform, new Vector3(0f, 0.78f, 0f), new Vector3(0.78f, 0.7f, 1.2f), wool, "Toison");
                Proto.Cube(body.transform, new Vector3(0f, 0.92f, 0.72f), new Vector3(0.32f, 0.34f, 0.36f),
                           new Color(0.24f, 0.22f, 0.21f), "Tete");
                for (int i = 0; i < 4; i++)
                {
                    float lx = (i % 2 == 0) ? -0.26f : 0.26f;
                    float lz = (i < 2) ? -0.38f : 0.4f;
                    Proto.Cube(body.transform, new Vector3(lx, 0.22f, lz), new Vector3(0.13f, 0.44f, 0.13f),
                               new Color(0.24f, 0.22f, 0.21f), "Patte");
                }
            }
            Proto.EndVisualOnly();

            Animal animal = go.AddComponent<Animal>();
            animal.walkSpeed = deer ? 2.8f : 1.5f;
            animal.runSpeed = deer ? 11f : 6f;
            animal.fleeRadius = deer ? 34f : 16f;
            animal.wanderRadius = deer ? 55f : 26f;
            animal.Initialise(at, body.transform, rng);
        }

        static void Flock(Transform parent, Vector3 centre, System.Random rng)
        {
            GameObject flock = new GameObject("VolDOiseaux");
            flock.transform.SetParent(parent, false);
            flock.transform.position = centre + new Vector3(0f, 120f + (float)rng.NextDouble() * 60f, 0f);

            Proto.BeginVisualOnly();
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 30f + (float)rng.NextDouble() * 60f;
                GameObject bird = new GameObject("Oiseau");
                bird.transform.SetParent(flock.transform, false);
                bird.transform.localPosition = new Vector3(Mathf.Cos(a) * r,
                                                           ((float)rng.NextDouble() - 0.5f) * 20f,
                                                           Mathf.Sin(a) * r);
                Color feather = new Color(0.12f, 0.12f, 0.14f);
                for (int w = -1; w <= 1; w += 2)
                {
                    GameObject wing = Proto.Cube(bird.transform, new Vector3(w * 1.5f, 0f, 0f),
                                                 new Vector3(3f, 0.25f, 0.9f), feather, "Aile");
                    wing.transform.localRotation = Quaternion.Euler(0f, 0f, w * 16f);
                }
            }
            Proto.EndVisualOnly();

            Spinner spin = flock.AddComponent<Spinner>();
            spin.axis = Vector3.up;
            spin.degreesPerSecond = 7f;
        }
    }
}
