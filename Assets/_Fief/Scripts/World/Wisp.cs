using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES FEUX-FOLLETS. Des petites lueurs bleu-vert qui flottent dans la sylve.
    ///
    /// Approche-toi a moins de neuf metres : le feu-follet s'eloigne... puis
    /// s'arrete, et t'attend. Suis-le : il te mene au creux a pierres-lune le plus
    /// proche, puis s'eteint. Si tu traines, il revient un peu vers toi et palpite.
    ///
    /// C'est un guide qui ne dit rien. Le joueur qui le suit apprend quelque chose
    /// sur la foret ; celui qui l'ignore ne perd rien.
    ///
    /// Ils passent a travers les troncs : ce sont des feux-follets.
    /// </summary>
    public class Wisp : MonoBehaviour
    {
        enum State { Idle, Leading, Fading, Gone }

        static readonly Color Glow = new Color(0.55f, 0.95f, 0.88f);
        static bool toldOnce;

        State state;
        Vector3 home;
        Vector3 target;
        float fade = 1f;
        float goneTimer;
        float seed;
        Light halo;
        Transform orb;
        System.Random rng;
        float mapHalf;

        public static void SpawnAll(Transform parent, GameConfig cfg, int count)
        {
            GameObject group = new GameObject("FEUX-FOLLETS");
            group.transform.SetParent(parent, false);
            System.Random rng = new System.Random((cfg != null ? cfg.worldSeed : 1) * 29 + 3);
            float half = (cfg != null ? cfg.mapSize : 420f) * 0.5f - 60f;
            toldOnce = false;

            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Feu-follet");
                go.transform.SetParent(group.transform, false);
                Wisp w = go.AddComponent<Wisp>();
                w.rng = new System.Random(rng.Next());
                w.mapHalf = half;
                w.seed = i * 1.7f;

                Proto.BeginVisualOnly();
                GameObject orb = Proto.Sphere(go.transform, Vector3.zero, Vector3.one * 0.22f, Color.white, "Lueur");
                orb.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Glow, 3f);
                GameObject core = Proto.Sphere(orb.transform, Vector3.zero, Vector3.one * 0.5f, Color.white, "Coeur");
                core.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Color.white, 2.5f);
                Proto.EndVisualOnly();
                w.orb = orb.transform;

                GameObject lightGo = new GameObject("Halo");
                lightGo.transform.SetParent(go.transform, false);
                w.halo = lightGo.AddComponent<Light>();
                w.halo.type = LightType.Point;
                w.halo.color = Glow;
                w.halo.range = 5f;
                w.halo.intensity = 1.3f;
                w.halo.shadows = LightShadows.None;

                // La traine : les paillettes naissent là où il passe et y restent.
                Ambiance.Sparkles(go.transform, Vector3.zero, Glow);

                w.Relocate();
            }
        }

        /// <summary>Une nouvelle place, dans la foret, loin du chateau et des lieux-dits.</summary>
        void Relocate()
        {
            for (int i = 0; i < 60; i++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * mapHalf;
                float z = ((float)rng.NextDouble() * 2f - 1f) * mapHalf;
                if (Castle.Covers(x, z, 25f) || Landmarks.Near(x, z, 5f)) continue;
                if (Game.PlayerTransform != null)
                {
                    Vector3 p = Game.PlayerTransform.position;
                    float d = new Vector2(p.x - x, p.z - z).magnitude;
                    if (d < 40f) continue;                  // jamais sous le nez du joueur
                }
                home = new Vector3(x, Ground.Sample(x, z) + 1.4f, z);
                break;
            }
            transform.position = home;
            state = State.Idle;
            fade = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Transform player = Game.PlayerTransform;

            if (state == State.Gone)
            {
                goneTimer -= dt;
                if (goneTimer <= 0f) Relocate();
                return;
            }

            float distance = player != null ? Flat(player.position - transform.position).magnitude : 99f;
            float t = Time.time + seed;

            if (state == State.Idle)
            {
                fade = Mathf.MoveTowards(fade, 1f, dt * 0.5f);
                Vector3 wander = new Vector3(Mathf.Sin(t * 0.37f) * 1.6f, Mathf.Sin(t * 0.9f) * 0.25f, Mathf.Cos(t * 0.29f) * 1.6f);
                transform.position = Vector3.MoveTowards(transform.position, home + wander, dt * 1.2f);
                if (distance < 9f && ChooseHollow()) state = State.Leading;
            }
            else if (state == State.Leading)
            {
                fade = 1f;
                if (player == null) return;
                Vector3 to = Flat(target - transform.position);
                if (to.magnitude < 2.5f)
                {
                    state = State.Fading;
                }
                else if (distance > 17f)
                {
                    // Trop loin derriere : il revient un peu, et palpite.
                    Vector3 back = Flat(player.position - transform.position).normalized;
                    Move(back, 1.2f, dt);
                }
                else if (distance > 11f)
                {
                    // Il attend, sur place.
                    Move(Vector3.zero, 0f, dt);
                }
                else
                {
                    Move(to.normalized, distance < 5f ? 4.8f : 3.6f, dt);
                }
            }
            else if (state == State.Fading)
            {
                fade = Mathf.MoveTowards(fade, 0f, dt * 0.7f);
                transform.position += Vector3.up * dt * 0.5f;
                if (fade <= 0f)
                {
                    state = State.Gone;
                    goneTimer = 50f + (float)rng.NextDouble() * 40f;
                }
            }

            float pulse = state == State.Leading && distance > 11f ? 0.35f * Mathf.Sin(t * 6f) : 0.12f * Mathf.Sin(t * 2.3f);
            halo.intensity = (1.3f + pulse) * fade;
            orb.localScale = Vector3.one * 0.22f * Mathf.Max(0.05f, fade) * (1f + pulse * 0.4f);
        }

        void Move(Vector3 direction, float speed, float dt)
        {
            Vector3 p = transform.position + direction * speed * dt;
            float ground = Ground.Sample(p.x, p.z);
            p.y = Mathf.Lerp(p.y, ground + 1.4f + Mathf.Sin((Time.time + seed) * 1.8f) * 0.2f, dt * 3f);
            transform.position = p;
        }

        /// <summary>Le creux le plus proche du feu-follet, a moins de 220 m.</summary>
        bool ChooseHollow()
        {
            float best = 220f;
            bool found = false;
            Vector2 me = new Vector2(transform.position.x, transform.position.z);
            for (int i = 0; i < Gathering.HollowSpotCount; i++)
            {
                Vector2 h = Gathering.HollowSpot(i);
                float d = (h - me).magnitude;
                if (d < 12f || d > best) continue;
                best = d;
                target = new Vector3(h.x, 0f, h.y);
                found = true;
            }
            if (found && !toldOnce)
            {
                toldOnce = true;
                Toasts.Show("Un feu-follet s'éloigne... et t'attend. Suis-le.", Glow);
            }
            return found;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
