using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON RAMASSE VOLE JUSQU'A SOI. Quand on prend du bois, de la pierre-lune
    /// ou du fer, quelques morceaux quittent le tas, font un petit arc et filent
    /// vers le sac (sous la camera). Ca dure une demi-seconde, et ca dit, sans un
    /// mot, "c'est a toi maintenant".
    ///
    /// Chaque morceau est un petit GameObject sans collider qui se detruit en
    /// arrivant : rien ne reste dans la scene.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        Vector3 from, lift;
        float delay, age, spin;
        Vector3 size;
        const float Duration = 0.42f;

        public static void Fly(Vector3 origin, ResourceType type, int count)
        {
            int pieces = Mathf.Clamp(count, 1, 6);
            Proto.BeginVisualOnly();
            for (int i = 0; i < pieces; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0f, 0.2f), Random.Range(-0.35f, 0.35f));
                GameObject go;
                if (type == ResourceType.Deadwood)
                {
                    go = Proto.Cylinder(null, origin + jitter, new Vector3(0.05f, 0.22f, 0.05f), Gathering.Bleached, "Brindille");
                }
                else if (type == ResourceType.Moonstone)
                {
                    go = Proto.Cube(null, origin + jitter, new Vector3(0.1f, 0.14f, 0.1f), Color.white, "Éclat");
                    go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.62f, 0.8f, 1f), 2.2f);
                }
                else
                {
                    go = Proto.Cube(null, origin + jitter, new Vector3(0.16f, 0.07f, 0.08f), new Color(0.3f, 0.29f, 0.28f), "Lingot");
                }
                go.transform.rotation = Random.rotation;
                Pickup p = go.AddComponent<Pickup>();
                p.from = go.transform.position;
                p.lift = new Vector3(Random.Range(-0.4f, 0.4f), 1.1f + Random.Range(0f, 0.4f), Random.Range(-0.4f, 0.4f));
                p.delay = i * 0.05f;
                p.spin = Random.Range(360f, 720f) * (Random.value < 0.5f ? -1f : 1f);
                p.size = go.transform.localScale;
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Du butin : des pieces d'or qui volent jusqu'au sac (une par tranche de 5 etoiles).</summary>
        public static void FlyLoot(Vector3 origin, int stars)
        {
            int pieces = Mathf.Clamp(stars / 5 + 2, 2, 8);
            Proto.BeginVisualOnly();
            Material gold = MaterialFactory.GetGlow(new Color(0.95f, 0.76f, 0.3f), 1.6f);
            for (int i = 0; i < pieces; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.2f), Random.Range(-0.3f, 0.3f));
                GameObject go = Proto.Cylinder(null, origin + jitter, new Vector3(0.12f, 0.012f, 0.12f), Color.white, "Pièce");
                go.GetComponent<Renderer>().sharedMaterial = gold;
                go.transform.rotation = Random.rotation;
                Pickup p = go.AddComponent<Pickup>();
                p.from = go.transform.position;
                p.lift = new Vector3(Random.Range(-0.4f, 0.4f), 1.1f + Random.Range(0f, 0.4f), Random.Range(-0.4f, 0.4f));
                p.delay = i * 0.04f;
                p.spin = Random.Range(360f, 720f);
                p.size = go.transform.localScale;
            }
            Proto.EndVisualOnly();
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01((age - delay) / Duration);
            Transform eye = Game.Hud != null && Game.Hud.viewCamera != null ? Game.Hud.viewCamera.transform : Game.PlayerTransform;
            if (eye == null) { Destroy(gameObject); return; }
            // Le sac : un peu sous le regard, devant soi.
            Vector3 to = eye.position - Vector3.up * 0.55f + eye.forward * 0.3f;
            Vector3 mid = Vector3.Lerp(from, to, 0.5f) + lift;
            float e = t * t * (3f - 2f * t);
            Vector3 a = Vector3.Lerp(from, mid, e);
            Vector3 b = Vector3.Lerp(mid, to, e);
            transform.position = Vector3.Lerp(a, b, e);
            transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
            transform.localScale = size * Mathf.Lerp(1f, 0.25f, Mathf.Clamp01((t - 0.6f) / 0.4f));
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
