using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LE CIEL DE L'ILE (27/09 : la foret est partie, on voit le ciel).
    ///
    /// Une fin de journee dorée : soleil bas (20 degres), lumiere chaude qui dessine
    /// de longues ombres, brume lointaine couleur d'or, et un vrai ciel.
    ///
    /// Concept Unity : la SKYBOX est ce qu'Unity dessine derriere tout le reste. Le
    /// shader "Skybox/Procedural" fabrique un ciel a partir de quelques reglages
    /// (teinte, epaisseur de l'atmosphere, taille du soleil) : pas d'image a
    /// importer, et le soleil du ciel suit la lumiere "Sun".
    /// </summary>
    public static class Atmosphere
    {
        public static Light Sun;
        public static Light Lamp;

        public static void Apply(GameConfig cfg, Camera view, Transform player)
        {
            Color haze = cfg != null ? cfg.hazeColor : new Color(0.78f, 0.66f, 0.58f);
            float sight = cfg != null ? cfg.sightDistance : 320f;

            // --- la brume : loin, doree, elle fond l'horizon dans le ciel
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = haze;
            RenderSettings.fogDensity = 1.7308f / Mathf.Max(5f, sight);

            // --- le ciel
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                sky.name = "Ciel du soir";
                if (sky.HasProperty("_SunDisk")) sky.SetFloat("_SunDisk", 2f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.045f);
                if (sky.HasProperty("_SunSizeConvergence")) sky.SetFloat("_SunSizeConvergence", 3.5f);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.25f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.52f, 0.55f, 0.66f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.62f, 0.52f, 0.5f));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.25f);
                RenderSettings.skybox = sky;
            }
            else RenderSettings.skybox = null;

            // L'ambiant : bleu du ciel en haut, or a l'horizon, rose-brun des nuages en bas.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.52f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.47f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.25f, 0.25f);
            RenderSettings.reflectionIntensity = 0.3f;

            if (view != null)
            {
                view.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                view.backgroundColor = haze;
                view.farClipPlane = Mathf.Max(900f, sight * 3f);
                view.nearClipPlane = 0.08f;
            }

            ApplySun(cfg);
            ApplyLamp(cfg, player);
        }

        static void ApplySun(GameConfig cfg)
        {
            if (Sun == null)
            {
                GameObject go = new GameObject("SOLEIL");
                Sun = go.AddComponent<Light>();
            }
            Sun.type = LightType.Directional;
            float elevation = cfg != null ? cfg.sunElevation : 20f;
            Sun.transform.rotation = Quaternion.Euler(elevation, 35f, 0f);
            Sun.color = new Color(1f, 0.84f, 0.64f);
            Sun.intensity = cfg != null ? cfg.sunIntensity : 1.15f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.75f;
            Sun.shadowNearPlane = 0.2f;
            RenderSettings.sun = Sun;
            QualitySettings.shadowDistance = 160f;
        }

        static void ApplyLamp(GameConfig cfg, Transform player)
        {
            if (player == null) return;
            if (Lamp == null)
            {
                GameObject go = new GameObject("LANTERNE");
                go.transform.SetParent(player, false);
                go.transform.localPosition = new Vector3(0.18f, 1.15f, 0.30f);
                Lamp = go.AddComponent<Light>();
                go.AddComponent<LampFlicker>();
            }
            Lamp.type = LightType.Point;
            Lamp.color = new Color(1f, 0.89f, 0.72f);
            Lamp.intensity = cfg != null ? cfg.lampIntensity : 0.5f;
            Lamp.range = cfg != null ? cfg.lampRange : 10f;
            Lamp.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// Fait respirer la lanterne. Trois ondes de periodes incommensurables : l'oeil
    /// ne trouve pas de boucle, donc la lumiere parait vivante plutot que cyclique.
    /// L'amplitude reste faible -- une lumiere qui clignote fort donne mal au coeur
    /// et, surtout, empeche de se reperer.
    /// </summary>
    public class LampFlicker : MonoBehaviour
    {
        Light lamp;
        float baseIntensity;
        float seed;

        void Start()
        {
            lamp = GetComponent<Light>();
            if (lamp != null && baseIntensity <= 0f) baseIntensity = lamp.intensity;
            seed = Random.Range(0f, 100f);
        }

        /// <summary>
        /// Change l'intensite de reference. Sans ca, ecrire lamp.intensity ne servirait
        /// a rien : a l'image suivante, le vacillement repartirait de l'ancienne valeur.
        /// </summary>
        public void Rebase(float intensity)
        {
            baseIntensity = intensity;
        }

        void Update()
        {
            if (lamp == null) return;
            float t = Time.time + seed;
            float wave = Mathf.Sin(t * 2.7f) * 0.5f
                       + Mathf.Sin(t * 6.31f) * 0.3f
                       + Mathf.Sin(t * 11.7f) * 0.2f;
            lamp.intensity = baseIntensity * (1f + wave * 0.07f);
        }
    }
}
