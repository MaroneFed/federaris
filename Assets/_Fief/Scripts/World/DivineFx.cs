using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LE KIT DES EFFETS DIVINS (10/10, v33 -- Martin : "refais toutes les animations des capacites
    /// divines"). Avant, chaque pouvoir n'etait fait que d'etincelles et d'anneaux : ca brillait,
    /// mais rien n'avait de POIDS, on ne savait pas ou ca allait tomber, et rien ne restait.
    /// Les briques qui manquaient, communes a tous les pouvoirs divins :
    ///
    ///   Mark     LA CIBLE AU SOL : un disque a la couleur du pouvoir, qui se REMPLIT jusqu'a
    ///            l'impact. On voit ou ca tombe, et quand -- on peut fuir.
    ///   Debris   des MORCEAUX DE PIERRE qui volent, retombent, roulent : le poids de l'impact.
    ///   Scorch   une TRACE BRULEE au sol, qui s'efface en quelques secondes : ca s'est passe ici.
    ///   Mushroom le CHAMPIGNON (la bombe atomique, la lune) : une colonne de fumee et sa tete.
    ///   Bolt     un ECLAIR en zigzag entre deux points (la foudre, les sorts qui relient).
    ///   Halo     une AUREOLE qui tourne autour d'un corps pendant quelques secondes.
    ///   Impact   tout ensemble, dose selon la taille : eclair de lumiere, onde, debris, fumee,
    ///            trace, secousse. Chaque grosse explosion divine passe par lui : elles ont toutes
    ///            le meme "langage", et on les reconnait.
    /// </summary>
    public static class DivineFx
    {
        static readonly Color Stone = new Color(0.46f, 0.42f, 0.38f);
        static readonly Color SmokeGrey = new Color(0.28f, 0.25f, 0.24f);

        static bool Far(Vector3 at)
        {
            Camera c = Camera.main;
            return c != null && (c.transform.position - at).sqrMagnitude > 140f * 140f;
        }

        /// <summary>Le sol sous "p" (ou "p" s'il n'y en a pas).</summary>
        public static Vector3 OnGround(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out hit, 30f, ~0, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        /// <summary>Un disque plat (visuel seul) d'un materiau transparent a lui.</summary>
        static Renderer Disc(Transform parent, Vector3 local, float diameter, Color c, string name)
        {
            Proto.BeginVisualOnly();
            GameObject d = Proto.Cylinder(parent, local, new Vector3(diameter, 0.01f, diameter), c, name);
            Proto.EndVisualOnly();
            Renderer r = d.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.GetTransparent(c);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        // ================================================================== la cible au sol

        /// <summary>LA CIBLE : un disque qui se remplit en "seconds" (et suit "follow" s'il y en a un).</summary>
        public static TargetMark Mark(Vector3 at, float radius, Color c, float seconds, Transform follow = null)
        {
            GameObject go = new GameObject("Cible divine");
            go.transform.position = OnGround(at) + Vector3.up * 0.06f;
            TargetMark m = go.AddComponent<TargetMark>();
            m.Setup(radius, c, seconds, follow);
            return m;
        }

        // ================================================================== les debris

        public static int liveDebris;

        /// <summary>Des morceaux de pierre (et quelques braises a la couleur) qui volent et retombent.</summary>
        public static void Debris(Vector3 at, int count, Color c, float speed)
        {
            if (Far(at)) return;
            count = Mathf.Min(count, 90 - liveDebris);
            for (int i = 0; i < count; i++)
            {
                bool ember = i % 4 == 0;
                Color col = ember ? c : Color.Lerp(Stone, Color.black, Random.value * 0.35f);
                float size = Random.Range(0.18f, 0.55f) * (ember ? 0.6f : 1f);
                Proto.BeginVisualOnly();
                GameObject g = Proto.Cube(null, at + Random.insideUnitSphere * 0.6f, new Vector3(size, size * Random.Range(0.6f, 1f), size), col, "Debris");
                Proto.EndVisualOnly();
                Renderer r = g.GetComponent<Renderer>();
                if (ember) r.sharedMaterial = MaterialFactory.GetGlow(c, 2f);
                r.shadowCastingMode = ShadowCastingMode.Off;
                Chunk k = g.AddComponent<Chunk>();
                Vector3 dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 1.4f + 0.5f;
                k.velocity = dir.normalized * speed * Random.Range(0.45f, 1f);
                k.spin = Random.insideUnitSphere * 720f;
                k.floor = OnGround(at).y;
                liveDebris++;
            }
        }



        // ================================================================== la trace brulee

        /// <summary>Une trace sombre au sol, qui s'efface en "seconds".</summary>
        public static void Scorch(Vector3 at, float radius, float seconds)
        {
            if (Far(at)) return;
            GameObject go = new GameObject("Trace");
            go.transform.position = OnGround(at) + Vector3.up * 0.04f;
            Fader f = go.AddComponent<Fader>();
            f.r = Disc(go.transform, Vector3.zero, radius * 2f, new Color(0.06f, 0.04f, 0.04f, 0.6f), "Brulure");
            f.r2 = Disc(go.transform, Vector3.up * 0.01f, radius * 1.1f, new Color(0.02f, 0.01f, 0.01f, 0.5f), "Coeur");
            f.life = seconds;
        }



        // ================================================================== la fumee

        /// <summary>Des boules de fumee qui montent et grossissent.</summary>
        public static void Smoke(Vector3 at, int count, float size, float rise)
        {
            if (Far(at)) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = at + new Vector3(Random.Range(-1f, 1f) * size, Random.value * size * 0.5f, Random.Range(-1f, 1f) * size);
                Puff(p, size * Random.Range(0.6f, 1.1f), SmokeGrey, new Vector3(Random.Range(-0.5f, 0.5f), rise * Random.Range(0.6f, 1.2f), Random.Range(-0.5f, 0.5f)), Random.Range(2f, 3.5f));
            }
        }

        /// <summary>Combien de boules de fumee existent : au-dela de 90, on n'en ajoute plus (fluidite).</summary>
        public static int liveSmoke;

        static void Puff(Vector3 at, float size, Color c, Vector3 velocity, float life)
        {
            if (liveSmoke >= 90) return;
            liveSmoke++;
            Proto.BeginVisualOnly();
            GameObject g = Proto.Sphere(null, at, Vector3.one * size * 0.4f, c, "Fumee");
            Proto.EndVisualOnly();
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.GetTransparent(new Color(c.r, c.g, c.b, 0.55f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            PuffMotion m = g.AddComponent<PuffMotion>();
            m.velocity = velocity;
            m.life = life;
            m.size = size;
            m.r = r;
        }



        /// <summary>LE CHAMPIGNON : une colonne de fumee, puis une tete large -- la bombe, la lune.</summary>
        public static void Mushroom(Vector3 at, float size, Color fire)
        {
            if (Far(at)) return;
            Vector3 g = OnGround(at);
            for (int i = 0; i < 6; i++)
                Puff(g + Vector3.up * i * size * 0.35f, size * 0.55f, Color.Lerp(fire, SmokeGrey, 0.4f + i * 0.1f), Vector3.up * size * 0.5f, 4f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 0.2f;
                Vector3 off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size * 0.7f;
                Puff(g + Vector3.up * size * 2.2f + off * 0.4f, size * 0.8f, Color.Lerp(fire, SmokeGrey, 0.65f), Vector3.up * size * 0.45f + off * 0.35f, 4.5f);
            }
            Puff(g + Vector3.up * size * 2.4f, size * 1.1f, Color.Lerp(fire, Color.white, 0.2f), Vector3.up * size * 0.5f, 2f);
        }

        // ================================================================== l'eclair

        /// <summary>UN ECLAIR en zigzag de "from" a "to", qui crepite "seconds".</summary>
        public static void Bolt(Vector3 from, Vector3 to, Color c, float width, float seconds)
        {
            if (Far(from) && Far(to)) return;
            Material m = Ambiance.Additive;
            if (m == null) return;
            GameObject go = new GameObject("Eclair divin");
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = m;
            line.useWorldSpace = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            BoltMotion b = go.AddComponent<BoltMotion>();
            b.line = line;
            b.from = from;
            b.to = to;
            b.colour = c;
            b.width = width;
            b.life = seconds;
            b.Shape();
        }



        // ================================================================== l'aureole

        /// <summary>UNE AUREOLE : deux cercles de lumiere qui tournent autour d'un corps, "seconds".</summary>
        public static void Halo(Transform body, Color c, float seconds, float radius)
        {
            if (body == null) return;
            Material m = Ambiance.Additive;
            if (m == null) return;
            GameObject go = new GameObject("Aureole");
            go.transform.SetParent(body, false);
            HaloMotion h = go.AddComponent<HaloMotion>();
            h.colour = c;
            h.life = seconds;
            h.radius = radius;
            for (int k = 0; k < 2; k++)
            {
                GameObject child = new GameObject("Cercle");
                child.transform.SetParent(go.transform, false);
                LineRenderer l = child.AddComponent<LineRenderer>();
                l.sharedMaterial = m;
                l.loop = true;
                l.useWorldSpace = false;
                l.positionCount = 40;
                l.shadowCastingMode = ShadowCastingMode.Off;
                for (int i = 0; i < 40; i++)
                {
                    float a = i / 40f * Mathf.PI * 2f;
                    l.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
                }
                h.rings[k] = l;
            }
        }



        // ================================================================== l'impact

        /// <summary>
        /// UN IMPACT DIVIN. "power" : 0,5 (une petite bombe) a 3 (la lune). Eclair de lumiere,
        /// onde, anneau au sol, debris, poussiere, fumee, trace, secousse -- tout a l'echelle.
        /// </summary>
        public static void Impact(Vector3 at, float radius, Color c, float power, bool loud)
        {
            Vector3 g = OnGround(at);
            Vector3 mid = g + Vector3.up * Mathf.Min(3f, radius * 0.2f);
            Fx.Flash(mid, c, radius * 3f, 4f + 4f * power, 0.25f + 0.15f * power);
            Fx.Flash(mid, Color.white, radius * 1.5f, 6f * power, 0.12f);
            Fx.Shock(mid, c, radius, 0.35f + 0.15f * power);
            Fx.Shock(mid, Color.white, radius * 0.45f, 0.2f + 0.1f * power);
            Fx.GroundRing(g, c, radius * 1.15f, 0.5f + 0.2f * power);
            Fx.GroundRing(g, Color.white, radius * 0.7f, 0.35f + 0.1f * power);
            Fx.Burst(mid, c, Mathf.RoundToInt(60 + 80 * power), 10f + radius, 0.35f, 0.9f, 0.4f, Vector3.up, 75f);
            Fx.Burst(g + Vector3.up * 0.3f, new Color(0.62f, 0.55f, 0.46f), Mathf.RoundToInt(30 + 40 * power), 4f + radius * 0.4f, 0.9f, 1.4f, 0.1f, Vector3.up, 88f);
            Debris(g + Vector3.up * 0.5f, Mathf.RoundToInt(8 + 12 * power), c, 8f + 5f * power);
            Smoke(g + Vector3.up * 0.5f, Mathf.RoundToInt(3 + 3 * power), Mathf.Max(1.5f, radius * 0.25f), 2f + power);
            Scorch(g, Mathf.Max(1.5f, radius * 0.45f), 5f + 2f * power);
            AbilityCaster.ShakeNear(g, 0.3f + 0.35f * power);
            if (loud) Sfx.KoBoom(g, true); else Sfx.CrashAt(g);
        }
    }

    /// <summary>Un morceau : il vole, rebondit une fois, glisse, et rapetisse.</summary>
    public class Chunk : MonoBehaviour
    {
        public Vector3 velocity;
        public Vector3 spin;
        public float floor;
        float age;
        bool bounced;
        Vector3 size;

        void Start() { size = transform.localScale; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            velocity += Vector3.down * 26f * dt;
            Vector3 p = transform.position + velocity * dt;
            if (p.y < floor + 0.1f && velocity.y < 0f)
            {
                p.y = floor + 0.1f;
                if (!bounced) { bounced = true; velocity = new Vector3(velocity.x * 0.45f, -velocity.y * 0.3f, velocity.z * 0.45f); spin *= 0.4f; }
                else { velocity = new Vector3(velocity.x * 0.8f, 0f, velocity.z * 0.8f); spin *= 0.9f; }
            }
            transform.position = p;
            transform.Rotate(spin * dt, Space.World);
            if (age > 1.8f) transform.localScale = size * Mathf.Clamp01((2.4f - age) / 0.6f);
            if (age > 2.4f || p.y < -60f) Destroy(gameObject);
        }

        void OnDestroy() { DivineFx.liveDebris = Mathf.Max(0, DivineFx.liveDebris - 1); }
    }

    /// <summary>Ce qui s'efface tout seul (les traces).</summary>
    public class Fader : MonoBehaviour
    {
        public Renderer r;
        public Renderer r2;
        public float life;
        float age;
        Color c1, c2;

        void Start()
        {
            if (r != null) c1 = r.sharedMaterial.color;
            if (r2 != null) c2 = r2.sharedMaterial.color;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(1f - (age - life * 0.5f) / (life * 0.5f));
            if (r != null) r.sharedMaterial.color = new Color(c1.r, c1.g, c1.b, c1.a * k);
            if (r2 != null) r2.sharedMaterial.color = new Color(c2.r, c2.g, c2.b, c2.a * k);
            if (age > life) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (r != null) Destroy(r.sharedMaterial);
            if (r2 != null) Destroy(r2.sharedMaterial);
        }
    }

    /// <summary>Une boule de fumee : elle monte, gonfle, s'eclaircit.</summary>
    public class PuffMotion : MonoBehaviour
    {
        public Vector3 velocity;
        public float life;
        public float size;
        public Renderer r;
        float age;
        Color c;

        void Start() { c = r.sharedMaterial.color; }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float k = Mathf.Clamp01(age / life);
            transform.position += velocity * dt;
            velocity *= 1f - dt * 0.6f;
            transform.localScale = Vector3.one * size * Mathf.Lerp(0.4f, 1.6f, Mathf.Sqrt(k));
            r.sharedMaterial.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
            if (age > life) Destroy(gameObject);
        }

        void OnDestroy() { if (r != null) Destroy(r.sharedMaterial); DivineFx.liveSmoke = Mathf.Max(0, DivineFx.liveSmoke - 1); }
    }

    public class BoltMotion : MonoBehaviour
    {
        public LineRenderer line;
        public Vector3 from;
        public Vector3 to;
        public Color colour;
        public float width;
        public float life;
        float age, next;

        public void Shape()
        {
            int n = Mathf.Clamp(Mathf.RoundToInt((to - from).magnitude / 2.5f), 4, 40);
            line.positionCount = n + 1;
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            Vector3 up2 = Vector3.Cross(side, (to - from).normalized);
            float jag = Mathf.Min(2.2f, (to - from).magnitude * 0.06f);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < n) p += side * Random.Range(-jag, jag) + up2 * Random.Range(-jag, jag);
                line.SetPosition(i, p);
            }
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(1f - age / life);
            next -= Time.deltaTime;
            if (next <= 0f) { next = 0.05f; Shape(); }
            float flick = 0.6f + 0.4f * Random.value;
            line.startWidth = line.endWidth = width * k * flick;
            Color c = Color.Lerp(colour, Color.white, 0.5f);
            line.startColor = line.endColor = new Color(c.r, c.g, c.b, k);
            if (age > life) Destroy(gameObject);
        }
    }

    public class HaloMotion : MonoBehaviour
    {
        public readonly LineRenderer[] rings = new LineRenderer[2];
        public Color colour;
        public float life;
        public float radius;
        float age, spark;

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float k = Mathf.Clamp01(Mathf.Min(age / 0.2f, (life - age) / 0.4f));
            for (int i = 0; i < 2; i++)
            {
                if (rings[i] == null) continue;
                Transform t = rings[i].transform;
                t.localPosition = Vector3.up * (0.4f + 1.3f * Mathf.Repeat(age * 0.8f + i * 0.5f, 1f));
                t.localRotation = Quaternion.Euler(12f * Mathf.Sin(age * 3f + i), age * (i == 0 ? 160f : -220f), 0f);
                rings[i].startWidth = rings[i].endWidth = 0.12f * k;
                rings[i].startColor = rings[i].endColor = new Color(colour.r, colour.g, colour.b, k);
            }
            spark -= dt;
            if (spark <= 0f && transform.parent != null)
            {
                spark = 0.1f;
                Fx.Burst(transform.parent.position + Vector3.up * Random.Range(0.3f, 2f), colour, 2, 2f, 0.25f, 0.5f, -0.4f, Vector3.up, 40f);
            }
            if (age > life) Destroy(gameObject);
        }
    }

    /// <summary>LA CIBLE AU SOL : un disque pale a la taille du coup, et dedans un disque qui se remplit.</summary>
    public class TargetMark : MonoBehaviour
    {
        Renderer outer, inner;
        Transform follow;
        float radius, life, age, ring;
        Color colour;

        public void Setup(float r, Color c, float seconds, Transform followThis)
        {
            radius = r;
            colour = c;
            life = Mathf.Max(0.1f, seconds);
            follow = followThis;
            outer = MakeDisc(r * 2f, new Color(c.r, c.g, c.b, 0.16f), "Zone");
            inner = MakeDisc(0.01f, new Color(c.r, c.g, c.b, 0.38f), "Remplissage");
            Fx.GroundRing(transform.position, c, r, 0.3f);
        }

        Renderer MakeDisc(float d, Color c, string name)
        {
            Proto.BeginVisualOnly();
            GameObject g = Proto.Cylinder(transform, Vector3.zero, new Vector3(d, 0.01f, d), c, name);
            Proto.EndVisualOnly();
            Renderer r = g.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.GetTransparent(c);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (follow != null) transform.position = DivineFx.OnGround(follow.position) + Vector3.up * 0.06f;
            float k = Mathf.Clamp01(age / life);
            float d = radius * 2f * k;
            if (inner != null) inner.transform.localScale = new Vector3(d, 0.01f, d);
            // Le bord bat de plus en plus vite a l'approche du coup.
            ring -= dt;
            if (ring <= 0f)
            {
                ring = Mathf.Lerp(0.4f, 0.08f, k);
                Fx.Ring(transform.position + Vector3.up * 0.1f, colour, radius * 0.98f, radius, 0.2f, 0.25f, Vector3.up);
            }
            if (outer != null)
            {
                float pulse = 0.16f + 0.1f * Mathf.Abs(Mathf.Sin(age * Mathf.Lerp(4f, 18f, k)));
                outer.sharedMaterial.color = new Color(colour.r, colour.g, colour.b, pulse);
            }
            if (age >= life) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (outer != null) Destroy(outer.sharedMaterial);
            if (inner != null) Destroy(inner.sharedMaterial);
        }
    }
}
