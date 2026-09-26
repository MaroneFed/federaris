using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LES EFFETS SPECIAUX (27/09 -- Martin : "que ca fasse des mega effets, que tous
    /// les trucs soient incroyables"). Une boite a outils, et chaque capacite y pioche
    /// sa signature (voir AbilityCaster) :
    ///
    ///   Ring    un anneau de lumiere qui s'elargit (au sol, ou face a soi) ;
    ///   Shock   une sphere de lumiere qui gonfle et s'efface (l'onde de choc) ;
    ///   Flash   une lumiere qui eclate et s'eteint (elle eclaire le decor autour) ;
    ///   Burst   une gerbe d'etincelles, reglable (nombre, vitesse, taille, cone) ;
    ///   Column  une colonne d'etincelles qui monte ;
    ///   Trail   une trainee de lumiere derriere un corps, quelques instants.
    ///
    /// Tout est ADDITIF : la lumiere s'ajoute a l'image, comme dans les jeux qui
    /// brillent. Pas de post-traitement (bloom) : c'est l'accumulation qui fait l'eclat.
    ///
    /// Concept Unity : chaque effet est un petit GameObject qui s'anime tout seul
    /// (un composant avec son Update) puis se DETRUIT. Rien ne reste en memoire.
    /// </summary>
    public static class Fx
    {
        // ================================================================== les briques

        /// <summary>Un anneau qui s'elargit de "from" a "to" metres en "seconds", couche sur le plan de normale "normal".</summary>
        public static void Ring(Vector3 centre, Color c, float from, float to, float seconds, float width, Vector3 normal)
        {
            Material m = Ambiance.Additive;
            if (m == null) return;
            GameObject go = new GameObject("Fx anneau");
            go.transform.position = centre;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up);
            FxRing r = go.AddComponent<FxRing>();
            r.line = go.AddComponent<LineRenderer>();
            r.line.sharedMaterial = m;
            r.line.loop = true;
            r.line.useWorldSpace = false;
            r.line.positionCount = 48;
            r.line.shadowCastingMode = ShadowCastingMode.Off;
            r.line.receiveShadows = false;
            r.from = from;
            r.to = to;
            r.life = seconds;
            r.width = width;
            r.colour = c;
            r.Tick(0f);
        }

        /// <summary>Un anneau au sol.</summary>
        public static void GroundRing(Vector3 at, Color c, float to, float seconds)
        {
            Ring(at + Vector3.up * 0.15f, c, 0.3f, to, seconds, 0.35f, Vector3.up);
        }

        /// <summary>Une sphere de lumiere qui gonfle jusqu'a "radius" et s'efface.</summary>
        public static void Shock(Vector3 at, Color c, float radius, float seconds)
        {
            Material source = Ambiance.Additive;
            if (source == null) return;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Fx choc";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = at;
            Renderer rd = go.GetComponent<Renderer>();
            Material m = new Material(source);
            m.mainTexture = null;
            rd.sharedMaterial = m;
            rd.shadowCastingMode = ShadowCastingMode.Off;
            rd.receiveShadows = false;
            FxShock s = go.AddComponent<FxShock>();
            s.mat = m;
            s.radius = radius;
            s.life = seconds;
            s.colour = c;
            s.Tick(0f);
        }

        /// <summary>Une lumiere qui eclate et s'eteint : elle eclaire tout ce qui est autour.</summary>
        public static void Flash(Vector3 at, Color c, float range, float intensity, float seconds)
        {
            GameObject go = new GameObject("Fx eclair");
            go.transform.position = at;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            FxFlash f = go.AddComponent<FxFlash>();
            f.lamp = l;
            f.peak = intensity;
            f.life = seconds;
        }

        /// <summary>
        /// Une gerbe d'etincelles. "direction" nulle : dans toutes les directions ; sinon
        /// un cone de "spread" degres autour d'elle.
        /// </summary>
        public static void Burst(Vector3 at, Color c, int count, float speed, float size, float life, float gravity, Vector3 direction, float spread)
        {
            Material m = Ambiance.Additive;
            if (m == null) return;
            ParticleSystem ps = Ambiance.NewSystem("Fx gerbe", null, at, m);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.6f));
            main.gravityModifier = gravity;
            main.maxParticles = Mathf.Max(8, count + 10);
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.ShapeModule shape = ps.shape;
            if (direction.sqrMagnitude > 0.01f)
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = spread;
                shape.radius = 0.2f;
                ps.transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.25f;
            }
            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 0.5f;
            drag.dampen = 0.12f;
            ParticleSystem.SizeOverLifetimeModule shrink = ps.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Ambiance.FadeInOut(ps, 1f);
            ps.Play();
        }

        /// <summary>Une gerbe simple, dans toutes les directions.</summary>
        public static void Sparks(Vector3 at, Color c, int count, float speed)
        {
            Burst(at, c, count, speed, 0.16f, 0.9f, 0.4f, Vector3.zero, 0f);
        }

        /// <summary>Une colonne d'etincelles qui monte de "at" sur "height" metres.</summary>
        public static void Column(Vector3 at, Color c, float height, float seconds, float radius)
        {
            Material m = Ambiance.Additive;
            if (m == null) return;
            ParticleSystem ps = Ambiance.NewSystem("Fx colonne", null, at, m);
            ParticleSystem.MainModule main = ps.main;
            main.duration = seconds;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(height * 0.5f, height);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.white);
            main.maxParticles = 400;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 260f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.SizeOverLifetimeModule shrink = ps.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Ambiance.FadeInOut(ps, 1f);
            ps.Play();
        }

        /// <summary>Une trainee de lumiere derriere "body" pendant "seconds".</summary>
        public static void Trail(Transform body, Color c, float seconds, float width)
        {
            Material m = Ambiance.Additive;
            if (m == null || body == null) return;
            GameObject go = new GameObject("Fx trainee");
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, 1f, 0f);
            TrailRenderer tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = m;
            tr.time = 0.35f;
            tr.startWidth = width;
            tr.endWidth = 0f;
            tr.startColor = c;
            tr.endColor = new Color(c.r, c.g, c.b, 0f);
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.minVertexDistance = 0.2f;
            FxTrail t = go.AddComponent<FxTrail>();
            t.trail = tr;
            t.life = seconds;
        }

        // ================================================================== les recettes

        /// <summary>LE RESPAWN : une colonne de lumiere a sa couleur, un anneau, un eclair, une gerbe.</summary>
        public static void Respawn(Vector3 at, Color c)
        {
            Column(at, c, 14f, 0.8f, 0.8f);
            GroundRing(at, c, 5f, 0.7f);
            Ring(at + Vector3.up * 1f, Color.white, 0.2f, 3f, 0.5f, 0.2f, Vector3.up);
            Flash(at + Vector3.up * 1.5f, c, 16f, 6f, 0.8f);
            Sparks(at + Vector3.up, c, 60, 7f);
            LightBeam beam = LightBeam.Build(null, at, c, 1.6f, 30f);
            if (beam != null)
            {
                beam.targetAlpha = 1f;
                beam.fadeSpeed = 5f;
                beam.gameObject.AddComponent<FxBeamFade>().beam = beam;
            }
            Sfx.Discovery();
        }

        /// <summary>UN IMPACT : la ou un coup porte (poussee, rayon, pendule).</summary>
        public static void Impact(Vector3 at, Color c, float strength)
        {
            Sparks(at, c, Mathf.RoundToInt(20 + 30 * strength), 5f + 6f * strength);
            Ring(at, Color.Lerp(c, Color.white, 0.4f), 0.2f, 1.5f + 1.5f * strength, 0.3f, 0.12f, Camera.main != null ? Camera.main.transform.position - at : Vector3.up);
            Flash(at, c, 6f + 4f * strength, 3f + 3f * strength, 0.25f);
        }
    }

    // ====================================================================== les animateurs

    public class FxRing : MonoBehaviour
    {
        public LineRenderer line;
        public float from;
        public float to;
        public float life;
        public float width;
        public float age;
        public Color colour;

        void Update() { Tick(Time.deltaTime); }

        public void Tick(float dt)
        {
            age += dt;
            float k = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
            if (k >= 1f) { Destroy(gameObject); return; }
            float ease = 1f - (1f - k) * (1f - k) * (1f - k);
            float r = Mathf.Lerp(from, to, ease);
            int n = line.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            Color c = new Color(colour.r, colour.g, colour.b, 1f - k);
            line.startColor = c;
            line.endColor = c;
            line.widthMultiplier = width * (1f - 0.6f * k);
        }
    }

    public class FxShock : MonoBehaviour
    {
        public Material mat;
        public float radius;
        public float life;
        public float age;
        public Color colour;

        void Update() { Tick(Time.deltaTime); }

        public void Tick(float dt)
        {
            age += dt;
            float k = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
            if (k >= 1f) { Destroy(mat); Destroy(gameObject); return; }
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, radius * 2f, ease);
            Color c = new Color(colour.r, colour.g, colour.b, 0.5f * (1f - k) * (1f - k));
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
            else mat.color = c;
        }
    }

    public class FxFlash : MonoBehaviour
    {
        public Light lamp;
        public float peak;
        public float life;
        public float age;

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
            if (k >= 1f) { Destroy(gameObject); return; }
            lamp.intensity = peak * (1f - k) * (1f - k);
        }
    }

    /// <summary>Une colonne de lumiere qui s'allume d'un coup, tient une demi-seconde, puis s'eteint.</summary>
    public class FxBeamFade : MonoBehaviour
    {
        public LightBeam beam;
        float age;

        void Update()
        {
            age += Time.deltaTime;
            if (age > 0.5f && beam != null) { beam.targetAlpha = 0f; beam.fadeSpeed = 1.3f; }
            if (age > 2.5f) Destroy(gameObject);
        }
    }

    public class FxTrail : MonoBehaviour
    {
        public TrailRenderer trail;
        public float life;

        void Update()
        {
            life -= Time.deltaTime;
            if (life > 0f) return;
            // On arrete d'emettre, on laisse la trainee s'eteindre, puis on s'en va.
            trail.emitting = false;
            if (life < -trail.time) Destroy(gameObject);
        }
    }

    /// <summary>
    /// LA BULLE DE PROTECTION : une sphere de lumiere pale autour d'un joueur protege
    /// (au depart, apres un respawn, juste apres un vol). On voit qu'il est intouchable.
    /// </summary>
    public class GraceShell : MonoBehaviour
    {
        public Seeker seeker;
        Renderer shell;
        Material mat;

        public static GraceShell Attach(Transform body, Seeker s)
        {
            Material source = Ambiance.Additive;
            if (source == null) return null;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bulle de protection";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, 1f, 0f);
            go.transform.localScale = new Vector3(1.6f, 2.3f, 1.6f);
            GraceShell g = go.AddComponent<GraceShell>();
            g.seeker = s;
            g.shell = go.GetComponent<Renderer>();
            g.mat = new Material(source);
            g.mat.mainTexture = null;
            g.shell.sharedMaterial = g.mat;
            g.shell.shadowCastingMode = ShadowCastingMode.Off;
            g.shell.receiveShadows = false;
            g.shell.enabled = false;
            return g;
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }

        void Update()
        {
            bool on = seeker != null && seeker.Graced && !seeker.Hidden;
            if (shell.enabled != on) shell.enabled = on;
            if (!on) return;
            float left = seeker.GraceUntil - Time.time;
            float blink = left < 0.8f ? 0.5f + 0.5f * Mathf.Sin(Time.time * 30f) : 1f;
            Color c = new Color(0.8f, 0.92f, 1f, 0.16f * blink);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
            else mat.color = c;
        }
    }
}
