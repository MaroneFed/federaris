using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON RAMASSE VOLE JUSQU'A SOI. Quand on ouvre un coffre, l'objet en sort,
    /// fait un petit arc et file vers soi (sous la camera), avec quelques eclats a sa
    /// couleur. Ca dure une demi-seconde, et ca dit, sans un mot, "c'est a toi".
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

        /// <summary>Un objet (et trois eclats a sa couleur) qui vole jusqu'a soi.</summary>
        public static void Fly(Vector3 origin, Item item)
        {
            Color tint = ItemInfo.Tint(item);
            Material glow = MaterialFactory.GetGlow(tint, 1.8f);
            Proto.BeginVisualOnly();
            for (int i = 0; i < 4; i++)
            {
                Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.2f), Random.Range(-0.3f, 0.3f));
                float d = i == 0 ? 0.22f : 0.07f;
                GameObject go = Proto.Cube(null, origin + jitter, new Vector3(d, d, d), tint, i == 0 ? "Objet" : "Éclat");
                go.GetComponent<Renderer>().sharedMaterial = glow;
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
