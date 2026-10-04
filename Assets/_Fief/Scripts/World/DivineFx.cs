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
    ///   Mark     LA CIBLE AU SOL : un cercle de lumiere a la couleur du pouvoir, un second qui
    ///            GRANDIT jusqu'a l'impact, et un rayon qui monte au centre. On voit ou ca tombe,
    ///            et quand -- on peut fuir.
    ///   Debris   des MORCEAUX DE PIERRE qui volent, retombent, roulent : le poids de l'impact.
    ///   Scorch   un CERCLE DE BRAISES au sol, qui s'eteint en quelques secondes : ca s'est passe ici.
    ///   Mushroom le CHAMPIGNON (la bombe atomique, la lune) : une colonne de fumee et sa tete.
    ///   Bolt     un ECLAIR en zigzag entre deux points (la foudre, les sorts qui relient).
    ///   Halo     une AUREOLE qui tourne autour d'un corps pendant quelques secondes.
    ///   Impact   tout ensemble, dose selon la taille : eclair de lumiere, onde, debris, fumee,
    ///            trace, secousse. Chaque grosse explosion divine passe par lui : elles ont toutes
    ///            le meme "langage", et on les reconnait.
    ///
    /// (11/10, v35 -- Martin : "des taches noires en plein milieu de la map, on ne capte rien")
    /// Les traces brulees etaient des DISQUES SOMBRES, et la fumee des BOULES GRISES, dans un
    /// materiau transparent fabrique a la main : sous la lumiere du jeu, elles sortaient noires
    /// et opaques, larges comme l'explosion, et restaient dix secondes. Plus rien de sombre ni
    /// d'opaque ici : que de la LUMIERE (lignes et etincelles additives) et de la POUSSIERE
    /// claire en particules, comme la mer de nuages (le materiau eprouve d'Ambiance).
    /// </summary>
    public static class DivineFx
    {
        static readonly Color Stone = new Color(0.46f, 0.42f, 0.38f);

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

        /// <summary>Une ligne de lumiere additive (anneau, rayon) : jamais sombre, jamais opaque.</summary>
        public static LineRenderer Line(Transform parent, string name, int points, bool loop)
        {
            Material m = Ambiance.Additive;
            if (m == null) return null;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = m;
            l.loop = loop;
            l.useWorldSpace = false;
            l.positionCount = points;
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            return l;
        }

        /// <summary>Pose un cercle de rayon "radius" dans une ligne en boucle.</summary>
        public static void Circle(LineRenderer l, float radius)
        {
            if (l == null) return;
            int n = l.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                l.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        // ================================================================== la cible au sol

        /// <summary>LA CIBLE : un cercle, un second qui grandit en "seconds", un rayon au centre (et suit "follow" s'il y en a un).</summary>
        public static TargetMark Mark(Vector3 at, float radius, Color c, float seconds, Transform follow = null)
        {
            GameObject go = new GameObject("Cible divine");
            go.transform.position = OnGround(at) + Vector3.up * 0.06f;
            TargetMark m = go.AddComponent<TargetMark>();
            m.Setup(radius, c, seconds, follow);
            // (v36) Qui l'a lancee et quoi : l'alerte a l'ecran (Warnings) montre l'icone.
            m.Icon = MarkIcon;
            m.Owner = MarkBy;
            // (v35) On l'ENTEND arriver : un sifflement qui descend jusqu'a l'impact.
            Sfx.Incoming(go.transform.position, seconds);
            return m;
        }

        // ================================================================== les debris

        public static int liveDebris;

        /// <summary>(v36) La capacite en train d'etre lancee (AbilityCaster.Cast) : ses cibles au sol portent son icone.</summary>
        public static string MarkIcon = "cible";
        public static Seeker MarkBy;

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

        /// <summary>Un cercle de braises au sol, qui rougeoie puis s'eteint en "seconds".</summary>
        public static void Scorch(Vector3 at, float radius, float seconds)
        {
            if (Far(at)) return;
            GameObject go = new GameObject("Braises");
            go.transform.position = OnGround(at) + Vector3.up * 0.12f;
            GlowRing g = go.AddComponent<GlowRing>();
            g.line = Line(go.transform, "Cercle", 56, true);
            if (g.line == null) { Object.Destroy(go); return; }
            Circle(g.line, radius);
            g.radius = radius;
            g.colour = new Color(1f, 0.55f, 0.2f);
            g.life = Mathf.Min(seconds, 4f);
        }

        // ================================================================== la fumee

        static readonly Color DustLight = new Color(0.9f, 0.86f, 0.8f, 0.75f);
        static readonly Color DustWarm = new Color(0.82f, 0.76f, 0.68f, 0.75f);

        /// <summary>De la poussiere claire qui monte et gonfle (des particules, plus des boules).</summary>
        public static void Smoke(Vector3 at, int count, float size, float rise)
        {
            if (Far(at)) return;
            Cloud(at, count * 3, size, Vector3.up * rise, size * 0.9f, DustLight, DustWarm, 2.6f);
        }

        /// <summary>Combien de nuages de poussiere existent : au-dela de 24, on n'en ajoute plus (fluidite).</summary>
        public static int liveSmoke;

        /// <summary>
        /// UN NUAGE de "count" bouffees claires (materiau melange d'Ambiance, celui de la mer de
        /// nuages) : elles partent a "velocity", gonflent jusqu'a 2,5 fois "size" et s'effacent.
        /// </summary>
        static void Cloud(Vector3 at, int count, float size, Vector3 velocity, float spread, Color a, Color b, float life)
        {
            if (liveSmoke >= 24 || count <= 0) return;
            Material m = Ambiance.Blended;
            if (m == null) return;
            liveSmoke++;
            ParticleSystem ps = Ambiance.NewSystem("Poussiere divine", null, at, m);
            ps.gameObject.AddComponent<CloudCount>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.maxParticles = count + 4;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Min(count, 120)) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(0.3f, spread);
            ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(velocity.x);
            v.y = new ParticleSystem.MinMaxCurve(velocity.y);
            v.z = new ParticleSystem.MinMaxCurve(velocity.z);
            ParticleSystem.SizeOverLifetimeModule grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.5f));
            Ambiance.FadeInOut(ps, 0.85f);
            ps.Play();
        }

        /// <summary>LE CHAMPIGNON : une colonne de poussiere, puis une tete large et claire -- la bombe, la lune.</summary>
        public static void Mushroom(Vector3 at, float size, Color fire)
        {
            if (Far(at)) return;
            Vector3 g = OnGround(at);
            Color hot = Color.Lerp(fire, Color.white, 0.35f);
            hot.a = 0.8f;
            // La colonne : des bouffees en file, de plus en plus claires vers le haut.
            for (int i = 0; i < 3; i++)
                Cloud(g + Vector3.up * size * (0.4f + i * 0.6f), 10, size * 0.6f, Vector3.up * size * 0.45f, size * 0.25f,
                      Color.Lerp(hot, DustLight, 0.3f + i * 0.25f), DustWarm, 4f);
            // La tete : large, qui s'etale.
            Cloud(g + Vector3.up * size * 2.3f, 26, size * 0.9f, Vector3.up * size * 0.3f, size * 0.8f, DustLight, Color.Lerp(hot, DustLight, 0.6f), 4.5f);
            // Le coeur de feu, qui s'eteint le premier (de la lumiere, pas de la fumee).
            Fx.Burst(g + Vector3.up * size * 2.3f, fire, 40, size * 0.8f, size * 0.7f, 1.4f, -0.1f, Vector3.zero, 0f);
            Fx.Burst(g + Vector3.up * size, fire, 30, size * 1.2f, size * 0.4f, 1.2f, -0.2f, Vector3.up, 12f);
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
            Sfx.ZapAt(to);
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
            // (v35, "il n'y a pas de son, mets-en a fond") : le BOUM a la taille du coup, qui porte loin.
            Sfx.Blast(g, power);
            if (loud) Sfx.KoBoom(g, false);
            // (v34) Pres de toi, ca te frappe : l'image se resserre, et pour les tres grosses, un
            // eclair blanc d'une image (l'"impact frame" des animes).
            Camera cam = Camera.main;
            if (cam != null && Game.Hud != null)
            {
                float d = (cam.transform.position - g).magnitude;
                float near = Mathf.Clamp01(1f - d / (radius * 2.5f + 15f));
                if (near > 0f && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Punch(4f + 6f * power * near);
                if (power >= 2f && near > 0.2f) Game.Hud.Flash(new Color(1f, 1f, 1f, 0.55f * near));
            }
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

    /// <summary>Un cercle de braises : il rougeoie, palpite, crache quelques etincelles, et s'eteint.</summary>
    public class GlowRing : MonoBehaviour
    {
        public LineRenderer line;
        public Color colour;
        public float radius;
        public float life;
        float age, spark;

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float k = Mathf.Clamp01(1f - age / life);
            if (line != null)
            {
                float flick = 0.8f + 0.2f * Mathf.Sin(age * 17f);
                line.startWidth = line.endWidth = Mathf.Lerp(0.15f, 0.55f, k) * flick;
                Color c = Color.Lerp(new Color(0.9f, 0.2f, 0.05f), colour, k);
                line.startColor = line.endColor = new Color(c.r, c.g, c.b, k * 0.9f);
            }
            spark -= dt;
            if (spark <= 0f && k > 0.2f)
            {
                spark = 0.18f;
                float a = Random.value * Mathf.PI * 2f;
                Fx.Burst(transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius, colour, 3, 2.5f, 0.2f, 0.7f, -0.2f, Vector3.up, 30f);
            }
            if (age > life) Destroy(gameObject);
        }
    }

    /// <summary>Tient le compte des nuages de poussiere vivants (voir DivineFx.liveSmoke).</summary>
    public class CloudCount : MonoBehaviour
    {
        void OnDestroy() { DivineFx.liveSmoke = Mathf.Max(0, DivineFx.liveSmoke - 1); }
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

    /// <summary>
    /// LA CIBLE AU SOL (v35 : plus de disques, qui sortaient noirs) : un cercle de lumiere a la
    /// taille du coup, un second qui grandit du centre jusqu'au bord a l'impact, une croix, et
    /// un rayon qui monte du centre -- on voit de loin ou ca va tomber.
    /// </summary>
    public class TargetMark : MonoBehaviour
    {
        /// <summary>(v36) Toutes les cibles au sol du moment (Warnings : es-tu dedans ?).</summary>
        public static readonly System.Collections.Generic.List<TargetMark> All = new System.Collections.Generic.List<TargetMark>();
        public string Icon = "cible";
        public Seeker Owner;
        public float Radius { get { return radius; } }
        public Color Colour { get { return colour; } }
        public float Remaining { get { return Mathf.Max(0f, life - age); } }
        /// <summary>Le lanceur ne craint pas sa propre cible (ses pouvoirs l'epargnent).</summary>
        public bool Harmless(Seeker s) { return Owner != null && Owner == s; }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        LineRenderer outer, inner, beam, crossA, crossB;
        Transform follow;
        float radius, life, age, ring;
        Color colour;

        public void Setup(float r, Color c, float seconds, Transform followThis)
        {
            radius = r;
            colour = c;
            life = Mathf.Max(0.1f, seconds);
            follow = followThis;
            outer = DivineFx.Line(transform, "Bord", 64, true);
            DivineFx.Circle(outer, r);
            inner = DivineFx.Line(transform, "Remplissage", 48, true);
            DivineFx.Circle(inner, 1f);
            crossA = DivineFx.Line(transform, "Croix", 2, false);
            crossB = DivineFx.Line(transform, "Croix", 2, false);
            if (crossA != null) { crossA.SetPosition(0, new Vector3(-r * 0.35f, 0.05f, 0f)); crossA.SetPosition(1, new Vector3(r * 0.35f, 0.05f, 0f)); }
            if (crossB != null) { crossB.SetPosition(0, new Vector3(0f, 0.05f, -r * 0.35f)); crossB.SetPosition(1, new Vector3(0f, 0.05f, r * 0.35f)); }
            beam = DivineFx.Line(transform, "Rayon", 2, false);
            if (beam != null) { beam.SetPosition(0, Vector3.zero); beam.SetPosition(1, Vector3.up * 30f); }
            Fx.GroundRing(transform.position, c, r, 0.3f);
        }

        static void Paint(LineRenderer l, Color c, float a, float width)
        {
            if (l == null) return;
            l.startWidth = l.endWidth = width;
            l.startColor = l.endColor = new Color(c.r, c.g, c.b, a);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (follow != null) transform.position = DivineFx.OnGround(follow.position) + Vector3.up * 0.06f;
            float k = Mathf.Clamp01(age / life);
            // Le bord bat de plus en plus vite a l'approche du coup, et blanchit.
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(age * Mathf.Lerp(4f, 20f, k)));
            Color hot = Color.Lerp(colour, Color.white, k * 0.6f);
            Paint(outer, hot, pulse, 0.3f + 0.25f * k);
            if (inner != null) inner.transform.localScale = new Vector3(Mathf.Max(0.05f, radius * k), 1f, Mathf.Max(0.05f, radius * k));
            Paint(inner, colour, 0.9f, 0.4f);
            Paint(crossA, hot, 0.5f * pulse, 0.18f);
            Paint(crossB, hot, 0.5f * pulse, 0.18f);
            if (beam != null)
            {
                beam.startWidth = 0.5f + 1.2f * k;
                beam.endWidth = 0.05f;
                beam.startColor = new Color(hot.r, hot.g, hot.b, 0.7f * pulse);
                beam.endColor = new Color(colour.r, colour.g, colour.b, 0f);
            }
            ring -= dt;
            if (ring <= 0f)
            {
                ring = Mathf.Lerp(0.4f, 0.08f, k);
                Fx.Ring(transform.position + Vector3.up * 0.1f, colour, radius * 0.98f, radius, 0.2f, 0.25f, Vector3.up);
            }
            if (age >= life) Destroy(gameObject);
        }
    }
}
