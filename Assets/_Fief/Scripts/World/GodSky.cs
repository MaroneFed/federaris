using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CIEL DU MODE DIEU (08/10 -- Martin : "quand on selectionne le mode Dieu, tout l'ecran se
    /// met dans un autre truc : des flammes dans le ciel, des trucs de fou ; quand tu appuies sur
    /// la petite pastille qui change, c'est un truc de malade mental").
    ///
    /// Des qu'on passe en Mode Dieu (le bouton du titre, la pastille "Mode" du salon) -- et pendant
    /// tout un match en Mode Dieu :
    ///   - un BOUM : un eclair blanc puis orange sur tout l'ecran, une colonne de feu sur la tour,
    ///     une onde, la camera qui tremble ;
    ///   - le CIEL vire au rouge sang et a l'or, la brume au braise, le soleil grossit ;
    ///   - la MER DE NUAGES prend feu ;
    ///   - une PLUIE DE METEORES traverse le ciel, une COURONNE DE FEU tourne au-dessus de la tour,
    ///     des FLAMMES montent de l'horizon, des BRAISES volent partout, des ECLAIRS tombent au loin.
    /// On repasse en Normal : tout revient, doucement.
    ///
    /// (11/10, v35 -- Martin : "pas que du rouge, ca fait enfer, j'aime pas ; fais que ca change de
    /// couleur pendant les trucs") : six CIELS DIVINS qui se succedent, fondus l'un dans l'autre --
    /// le feu (rouge et or), l'aurore (violet et rose), le cosmos (bleu nuit et cyan), l'emeraude
    /// (vert et turquoise), la rose (rose et peche), l'or (or pale et blanc). Toutes les 22 s au
    /// match, toutes les 9 s dans les menus ; meteores, couronne, flammes, braises et eclairs
    /// prennent la couleur du moment.
    ///
    /// Concept Unity : RenderSettings, c'est l'ambiance de la scene (le ciel, la brume, la lumiere
    /// ambiante) ; on la change par le code, d'une image a l'autre, en melangeant deux palettes.
    /// </summary>
    public class GodSky : MonoBehaviour
    {
        /// <summary>Les menus le mettent a vrai tant qu'un salon est en Mode Dieu.</summary>
        public static bool MenuGod;
        static GodSky instance;

        /// <summary>De 0 (le ciel normal) a 1 (le ciel du Mode Dieu).</summary>
        public static float Amount { get { return instance != null ? instance.k : 0f; } }
        /// <summary>L'eclair du passage en Mode Dieu (1 au BOUM, puis 0).</summary>
        public static float Flash { get; private set; }

        static bool Want { get { return MenuGod || Match.GodMode && Match.Active; } }

        float k;
        bool wasOn;
        bool captured;
        Material sky;
        Color skyTint, skyGround, fogColor, ambSky, ambEq, ambGround, sunColor;
        float exposure, thickness, sunSize, fogDensity, sunIntensity;
        ParticleSystem[] clouds;
        Color[] cloudTint;
        ParticleSystem meteors, ring, horizon, embers;
        float nextBolt;

        /// <summary>Un ciel divin : ciel, sol, brume, ambiance (haut, milieu, bas), soleil, nuages, flammes (a, b).</summary>
        struct SkyTheme
        {
            public Color sky;
            public Color ground;
            public Color fog;
            public Color ambSky;
            public Color ambEq;
            public Color ambGround;
            public Color sun;
            public Color cloud;
            public Color fireA;
            public Color fireB;

            public static SkyTheme Make(Color sky, Color ground, Color fog, Color ambSky, Color ambEq, Color ambGround, Color sun, Color cloud, Color fireA, Color fireB)
            {
                SkyTheme t = new SkyTheme();
                t.sky = sky;
                t.ground = ground;
                t.fog = fog;
                t.ambSky = ambSky;
                t.ambEq = ambEq;
                t.ambGround = ambGround;
                t.sun = sun;
                t.cloud = cloud;
                t.fireA = fireA;
                t.fireB = fireB;
                return t;
            }

            public static SkyTheme Mix(SkyTheme a, SkyTheme b, float t)
            {
                return Make(Color.Lerp(a.sky, b.sky, t), Color.Lerp(a.ground, b.ground, t), Color.Lerp(a.fog, b.fog, t),
                    Color.Lerp(a.ambSky, b.ambSky, t), Color.Lerp(a.ambEq, b.ambEq, t), Color.Lerp(a.ambGround, b.ambGround, t),
                    Color.Lerp(a.sun, b.sun, t), Color.Lerp(a.cloud, b.cloud, t), Color.Lerp(a.fireA, b.fireA, t), Color.Lerp(a.fireB, b.fireB, t));
            }
        }

        static readonly SkyTheme[] Themes =
        {
            // Le feu
            SkyTheme.Make(new Color(0.95f, 0.18f, 0.12f), new Color(0.55f, 0.08f, 0.06f), new Color(0.62f, 0.16f, 0.08f),
                      new Color(0.95f, 0.4f, 0.25f), new Color(0.85f, 0.3f, 0.18f), new Color(0.45f, 0.1f, 0.1f),
                      new Color(1f, 0.45f, 0.2f), new Color(0.95f, 0.32f, 0.12f), new Color(1f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.35f)),
            // L'aurore
            SkyTheme.Make(new Color(0.62f, 0.2f, 0.95f), new Color(0.3f, 0.08f, 0.45f), new Color(0.45f, 0.2f, 0.62f),
                      new Color(0.75f, 0.5f, 1f), new Color(0.7f, 0.4f, 0.85f), new Color(0.3f, 0.15f, 0.4f),
                      new Color(1f, 0.6f, 0.95f), new Color(0.75f, 0.4f, 0.95f), new Color(0.85f, 0.35f, 1f), new Color(1f, 0.6f, 0.85f)),
            // Le cosmos
            SkyTheme.Make(new Color(0.12f, 0.35f, 1f), new Color(0.05f, 0.1f, 0.35f), new Color(0.15f, 0.28f, 0.6f),
                      new Color(0.45f, 0.65f, 1f), new Color(0.35f, 0.5f, 0.9f), new Color(0.12f, 0.15f, 0.35f),
                      new Color(0.6f, 0.85f, 1f), new Color(0.35f, 0.6f, 1f), new Color(0.25f, 0.75f, 1f), new Color(0.75f, 0.95f, 1f)),
            // L'emeraude
            SkyTheme.Make(new Color(0.15f, 0.9f, 0.5f), new Color(0.05f, 0.35f, 0.25f), new Color(0.2f, 0.5f, 0.42f),
                      new Color(0.5f, 1f, 0.75f), new Color(0.4f, 0.85f, 0.65f), new Color(0.12f, 0.3f, 0.22f),
                      new Color(0.7f, 1f, 0.75f), new Color(0.35f, 0.95f, 0.65f), new Color(0.3f, 1f, 0.55f), new Color(0.75f, 1f, 0.9f)),
            // La rose
            SkyTheme.Make(new Color(1f, 0.35f, 0.6f), new Color(0.5f, 0.15f, 0.3f), new Color(0.75f, 0.38f, 0.5f),
                      new Color(1f, 0.65f, 0.75f), new Color(0.95f, 0.55f, 0.6f), new Color(0.45f, 0.2f, 0.28f),
                      new Color(1f, 0.7f, 0.65f), new Color(1f, 0.55f, 0.7f), new Color(1f, 0.4f, 0.7f), new Color(1f, 0.8f, 0.6f)),
            // L'or
            SkyTheme.Make(new Color(1f, 0.75f, 0.25f), new Color(0.5f, 0.35f, 0.12f), new Color(0.8f, 0.62f, 0.3f),
                      new Color(1f, 0.9f, 0.6f), new Color(0.95f, 0.8f, 0.5f), new Color(0.45f, 0.35f, 0.18f),
                      new Color(1f, 0.92f, 0.6f), new Color(1f, 0.85f, 0.45f), new Color(1f, 0.8f, 0.25f), new Color(1f, 1f, 0.85f)),
        };

        /// <summary>Le ciel divin du moment (un melange de deux, pendant les fondus).</summary>
        static SkyTheme current = Themes[0];
        /// <summary>Les deux couleurs de flamme du moment (pour les menus et les effets).</summary>
        public static Color FireA { get { return current.fireA; } }
        public static Color FireB { get { return current.fireB; } }

        /// <summary>Le ciel du moment : chaque palette tient "hold" secondes, puis fond 5 s dans la suivante.</summary>
        static SkyTheme Cycle()
        {
            // (v43, le designer) En manche, UN ciel par manche (il changeait toutes les 22 s : distrayant).
            if (Match.Active) return Themes[(Match.RoundNumber - 1) % Themes.Length];
            float hold = MenuGod && !Match.Active ? 9f : 22f;
            float fade = 5f;
            float t = Time.unscaledTime / (hold + fade);
            int i = Mathf.FloorToInt(t) % Themes.Length;
            float inside = (t - Mathf.Floor(t)) * (hold + fade);
            float m = Mathf.Clamp01((inside - hold) / fade);
            m = m * m * (3f - 2f * m);
            return SkyTheme.Mix(Themes[i], Themes[(i + 1) % Themes.Length], m);
        }

        /// <summary>Le batir avec le monde (GameBootstrap).</summary>
        public static void Build(Transform worldRoot)
        {
            GameObject go = new GameObject("CIEL DU MODE DIEU");
            if (worldRoot != null) go.transform.SetParent(worldRoot, false);
            instance = go.AddComponent<GodSky>();
            // Deja en Mode Dieu (une nouvelle manche) : pas de BOUM, on y est tout de suite.
            if (Want) { instance.wasOn = true; instance.k = 1f; }
        }

        void Capture()
        {
            captured = true;
            sky = RenderSettings.skybox;
            if (sky != null)
            {
                skyTint = sky.HasProperty("_SkyTint") ? sky.GetColor("_SkyTint") : Color.white;
                skyGround = sky.HasProperty("_GroundColor") ? sky.GetColor("_GroundColor") : Color.white;
                exposure = sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1.3f;
                thickness = sky.HasProperty("_AtmosphereThickness") ? sky.GetFloat("_AtmosphereThickness") : 1f;
                sunSize = sky.HasProperty("_SunSize") ? sky.GetFloat("_SunSize") : 0.045f;
            }
            fogColor = RenderSettings.fogColor;
            fogDensity = RenderSettings.fogDensity;
            ambSky = RenderSettings.ambientSkyColor;
            ambEq = RenderSettings.ambientEquatorColor;
            ambGround = RenderSettings.ambientGroundColor;
            if (Atmosphere.Sun != null) { sunColor = Atmosphere.Sun.color; sunIntensity = Atmosphere.Sun.intensity; }
            GameObject sea = GameObject.Find("MER DE NUAGES");
            if (sea != null)
            {
                clouds = sea.GetComponentsInChildren<ParticleSystem>();
                cloudTint = new Color[clouds.Length];
                for (int i = 0; i < clouds.Length; i++)
                {
                    Renderer r = clouds[i].GetComponent<Renderer>();
                    Material m = r != null ? r.material : null;
                    cloudTint[i] = m != null && m.HasProperty("_TintColor") ? m.GetColor("_TintColor") : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
        }

        void Update()
        {
            if (!captured) Capture();
            bool on = Want;
            if (on && !wasOn) Boom();
            wasOn = on;
            float dt = Time.unscaledDeltaTime;
            k = Mathf.MoveTowards(k, on ? 1f : 0f, dt * (on ? 1.4f : 0.6f));
            Flash = Mathf.MoveTowards(Flash, 0f, dt * 0.9f);
            current = Cycle();
            Blend(k);
            if (k > 0.01f) { EnsureFx(); Tint(); }
            SetRate(meteors, 9f * k);
            SetRate(ring, 160f * k);
            SetRate(horizon, 70f * k);
            SetRate(embers, 45f * k);
            // Les braises suivent la camera.
            Camera cam = Camera.main;
            if (embers != null && cam != null) embers.transform.position = cam.transform.position + cam.transform.forward * 6f;
            if (ring != null) ring.transform.Rotate(0f, 12f * dt, 0f, Space.World);
            // Les eclairs, au loin.
            if (k > 0.8f && Time.unscaledTime > nextBolt)
            {
                nextBolt = Time.unscaledTime + Random.Range(1.2f, 3.5f);
                float a = Random.value * Mathf.PI * 2f;
                float r = Random.Range(220f, 420f);
                Vector3 p = new Vector3(Mathf.Cos(a) * r, -45f, Mathf.Sin(a) * r);
                Color c = Random.value < 0.5f ? current.fireB : current.fireA;
                Fx.Column(p, c, 320f, 0.25f, 3f);
                Fx.Flash(p + Vector3.up * 120f, c, 400f, 6f, 0.25f);
                if (Random.value < 0.5f) Sfx.Thunder(p, false);   // (v37) le tonnerre roule au loin
            }
        }

        /// <summary>LE PASSAGE EN MODE DIEU : une colonne de feu sur la tour, un BOUM. (v37.2 : plus d'eclair
        /// blanc puis orange sur tout l'ecran -- Martin : "pas le flash du debut du mode".)</summary>
        void Boom()
        {
            Color fire = current.fireA;
            Vector3 top = new Vector3(0f, 105f, 0f);
            Fx.Column(new Vector3(0f, -40f, 0f), fire, 400f, 1.5f, 10f);
            Fx.Shock(top, fire, 120f, 1.2f);
            Fx.Shock(top, Color.white, 60f, 0.6f);
            Fx.Burst(top, fire, 400, 60f, 1f, 2f, -0.2f, Vector3.up, 180f);
            Fx.Flash(top, fire, 600f, 10f, 1f);
            Sfx.KoBoom(Camera.main != null ? Camera.main.transform.position : Vector3.zero, true);
            Sfx.Discovery();
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.6f);
            nextBolt = Time.unscaledTime + 0.4f;
        }

        void Blend(float t)
        {
            float e = t * t * (3f - 2f * t);
            if (sky != null)
            {
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(skyTint, current.sky, e));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", Color.Lerp(skyGround, current.ground, e));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(exposure, 1.8f, e));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(thickness, 3.2f, e));
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", Mathf.Lerp(sunSize, 0.16f, e));
            }
            RenderSettings.fogColor = Color.Lerp(fogColor, current.fog, e);
            RenderSettings.fogDensity = Mathf.Lerp(fogDensity, fogDensity * 0.8f, e);
            RenderSettings.ambientSkyColor = Color.Lerp(ambSky, current.ambSky, e);
            RenderSettings.ambientEquatorColor = Color.Lerp(ambEq, current.ambEq, e);
            RenderSettings.ambientGroundColor = Color.Lerp(ambGround, current.ambGround, e);
            Camera cam = Camera.main;
            if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor) cam.backgroundColor = RenderSettings.fogColor;
            if (Atmosphere.Sun != null)
            {
                Atmosphere.Sun.color = Color.Lerp(sunColor, current.sun, e);
                Atmosphere.Sun.intensity = Mathf.Lerp(sunIntensity, sunIntensity * 1.3f, e);
            }
            if (clouds != null)
                for (int i = 0; i < clouds.Length; i++)
                {
                    if (clouds[i] == null) continue;
                    Renderer r = clouds[i].GetComponent<Renderer>();
                    if (r == null || r.material == null || !r.material.HasProperty("_TintColor")) continue;
                    r.material.SetColor("_TintColor", Color.Lerp(cloudTint[i], new Color(current.cloud.r, current.cloud.g, current.cloud.b, cloudTint[i].a), e));
                }
        }

        /// <summary>Meteores, couronne, flammes et braises a la couleur du ciel du moment.</summary>
        void Tint()
        {
            Color a = current.fireA, b = current.fireB;
            Paint(meteors, a, b, 1f);
            Paint(ring, a, b, 0.9f);
            Paint(horizon, a, b, 0.8f);
            Paint(embers, a, b, 1f);
        }

        static void Paint(ParticleSystem ps, Color a, Color b, float alpha)
        {
            if (ps == null) return;
            ParticleSystem.MainModule m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(a.r, a.g, a.b, alpha), new Color(b.r, b.g, b.b, alpha));
        }

        static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;
        }

        void EnsureFx()
        {
            if (meteors != null || !Ambiance.EnsureMaterials()) return;
            Material add = Ambiance.Additive;

            // LA PLUIE DE METEORES : des trainees de feu qui traversent le ciel, de haut en bas.
            meteors = Ambiance.NewSystem("Pluie de meteores", transform, new Vector3(0f, 320f, 0f), add);
            ParticleSystem.MainModule m = meteors.main;
            m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(70f, 110f);
            m.startSize = new ParticleSystem.MinMaxCurve(5f, 9f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f, 1f), new Color(1f, 0.85f, 0.4f, 1f));
            m.maxParticles = 80;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule sh = meteors.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(900f, 10f, 900f);
            meteors.transform.rotation = Quaternion.Euler(140f, 30f, 0f);
            ParticleSystemRenderer mr = meteors.GetComponent<ParticleSystemRenderer>();
            mr.renderMode = ParticleSystemRenderMode.Stretch;
            mr.lengthScale = 4f;
            mr.velocityScale = 0.25f;
            meteors.Play();

            // LA COURONNE DE FEU : un anneau de flammes qui tourne a 160 m au-dessus de la tour.
            ring = Ambiance.NewSystem("Couronne de feu", transform, new Vector3(0f, 165f, 0f), add);
            ParticleSystem.MainModule rm = ring.main;
            rm.loop = true;
            rm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            rm.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            rm.startSize = new ParticleSystem.MinMaxCurve(10f, 22f);
            rm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.1f, 0.9f), new Color(1f, 0.8f, 0.3f, 0.9f));
            rm.maxParticles = 600;
            rm.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.ShapeModule rs = ring.shape;
            rs.shapeType = ParticleSystemShapeType.Circle;
            rs.radius = 140f;
            rs.radiusThickness = 0f;
            ring.transform.rotation = Quaternion.identity;
            ring.transform.Rotate(-90f, 0f, 0f, Space.Self);
            ring.Play();

            // LES FLAMMES DE L'HORIZON : elles montent de la mer de nuages, tout autour de l'ile.
            horizon = Ambiance.NewSystem("Flammes de l'horizon", transform, new Vector3(0f, -45f, 0f), add);
            ParticleSystem.MainModule hm = horizon.main;
            hm.loop = true;
            hm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            hm.startSpeed = new ParticleSystem.MinMaxCurve(15f, 35f);
            hm.startSize = new ParticleSystem.MinMaxCurve(30f, 70f);
            hm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.35f, 0.08f, 0.8f), new Color(1f, 0.7f, 0.25f, 0.8f));
            hm.maxParticles = 500;
            hm.gravityModifier = -0.3f;
            ParticleSystem.ShapeModule hs = horizon.shape;
            hs.shapeType = ParticleSystemShapeType.Circle;
            hs.radius = 420f;
            hs.radiusThickness = 0.25f;
            horizon.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            horizon.Play();

            // LES BRAISES : de petites etincelles qui montent devant la camera.
            embers = Ambiance.NewSystem("Braises", null, Vector3.zero, add);
            embers.transform.SetParent(transform, true);
            ParticleSystem.MainModule em = embers.main;
            em.loop = true;
            em.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            em.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            em.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            em.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.15f, 1f), new Color(1f, 0.9f, 0.4f, 1f));
            em.maxParticles = 300;
            em.gravityModifier = -0.15f;
            em.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule es = embers.shape;
            es.shapeType = ParticleSystemShapeType.Box;
            es.scale = new Vector3(20f, 8f, 14f);
            ParticleSystem.NoiseModule noise = embers.noise;
            noise.enabled = true;
            noise.strength = 1f;
            noise.frequency = 0.5f;
            embers.Play();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ================================================================== l'ecran

        static readonly Vector4[] sparks = new Vector4[40];   // x, y (0-1), vitesse, taille
        static bool sparksReady;

        /// <summary>
        /// PAR-DESSUS L'ECRAN (Menus) : en Mode Dieu, les bords rougeoient, des flammes montent du
        /// bas, des braises volent ; et au BOUM, tout l'ecran blanchit puis vire a l'orange.
        /// </summary>
        public static void DrawOverlay(bool menu)
        {
            float a = Amount;
            float w = Screen.width, h = Screen.height;
            if (menu && a > 0.01f)
            {
                // Les bords rougeoient (un degrade, en bandes).
                float edge = h * 0.18f;
                for (int i = 0; i < 8; i++)
                {
                    float f = 1f - i / 8f;
                    float al = 0.09f * f * a * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f));
                    float d = edge * i / 8f;
                    Color lo = current.fireA, hi = current.sky;
                    UiStyle.Fill(new Rect(0f, h - d - edge / 8f, w, edge / 8f), new Color(lo.r, lo.g, lo.b, al * 1.6f));
                    UiStyle.Fill(new Rect(0f, d, w, edge / 8f), new Color(hi.r, hi.g, hi.b, al));
                    UiStyle.Fill(new Rect(d, 0f, edge / 8f, h), new Color(hi.r, hi.g, hi.b, al));
                    UiStyle.Fill(new Rect(w - d - edge / 8f, 0f, edge / 8f, h), new Color(hi.r, hi.g, hi.b, al));
                }
                // Des flammes qui montent du bas de l'ecran (trois tailles fixes : nettes).
                int[] sizes = { UiStyle.S(90), UiStyle.S(130), UiStyle.S(170) };
                int n = Mathf.CeilToInt(w / UiStyle.S(110f)) + 1;
                for (int i = 0; i < n; i++)
                {
                    int s = sizes[(i * 7) % 3];
                    float bob = Mathf.Sin(Time.unscaledTime * (3f + (i % 4)) + i * 1.7f) * s * 0.12f;
                    float x = i * UiStyle.S(110f) - s * 0.3f;
                    float y = h - s * 0.78f + bob + s * 0.25f * (1f - a);
                    Color c = (i % 2 == 0) ? new Color(current.fireA.r, current.fireA.g, current.fireA.b, 0.85f * a) : new Color(current.fireB.r, current.fireB.g, current.fireB.b, 0.75f * a);
                    Icons.Draw(Icons.Snap(new Rect(x, y, s, s)), "flammes", c);
                }
                // Les braises qui montent.
                if (!sparksReady)
                {
                    sparksReady = true;
                    for (int i = 0; i < sparks.Length; i++) sparks[i] = new Vector4(Random.value, Random.value, Random.Range(0.05f, 0.18f), i % 3);
                }
                bool repaint = Event.current.type == EventType.Repaint;
                int[] dots = { UiStyle.S(4), UiStyle.S(6), UiStyle.S(9) };
                for (int i = 0; i < sparks.Length; i++)
                {
                    Vector4 p = sparks[i];
                    if (repaint)
                    {
                        p.y -= p.z * Time.unscaledDeltaTime;
                        p.x += Mathf.Sin(Time.unscaledTime * 2f + i) * 0.0006f;
                        if (p.y < -0.02f) { p.y = 1.02f; p.x = Random.value; }
                        sparks[i] = p;
                    }
                    int ds = dots[(int)p.w];
                    Color sc = Color.Lerp(current.fireA, current.fireB, ((i * 13) % 10) / 10f);
                    Icons.Pill(new Rect(Mathf.Round(p.x * w), Mathf.Round(p.y * h), ds, ds), new Color(sc.r, sc.g, sc.b, 0.9f * a));
                }
            }
        }

        /// <summary>Le BOUM du passage en Mode Dieu, par-dessus tout : blanc, puis orange, puis rien.</summary>
        public static void DrawFlash()
        {
            float w = Screen.width, h = Screen.height;
            if (Flash > 0.01f)
            {
                float f = Flash;
                Color c = f > 0.75f ? new Color(1f, 1f, 0.95f, Mathf.Clamp01((f - 0.6f) * 2.5f)) : new Color(current.fireA.r, current.fireA.g, current.fireA.b, f * 0.8f);
                UiStyle.Fill(new Rect(0f, 0f, w, h), c);
            }
        }
    }
}
