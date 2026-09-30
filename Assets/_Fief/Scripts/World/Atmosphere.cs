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
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.95f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.46f, 0.58f, 0.8f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.93f, 0.86f, 0.86f));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.3f);
                RenderSettings.skybox = sky;
            }
            else RenderSettings.skybox = null;

            // L'ambiant : bleu du ciel en haut, or a l'horizon, rose-brun des nuages en bas.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // (30/09 -- "plus pro") Plus clair, plus franc : un ciel bleu, un horizon peche,
            // des nuages blancs et roses en dessous. Les ombres restent chaudes.
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.74f, 0.66f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.44f, 0.38f, 0.40f);
            RenderSettings.reflectionIntensity = 0.9f;

            if (view != null)
            {
                view.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                view.backgroundColor = haze;
                view.farClipPlane = Mathf.Max(900f, sight * 3f);
                view.nearClipPlane = 0.15f;   // (29/09) 0,08 : trop peu de precision au loin, les surfaces proches clignotaient
            }

            ApplySun(cfg);
            ApplyLamp(cfg, player);
            Smooth(view);
            CaptureReflections();
        }

        /// <summary>
        /// LISSE (30/09 -- Martin et son frere : "lisse, lisse"). Ce qui fait qu'une image
        /// 3D a l'air propre :
        ///   - l'ANTICRENELAGE (MSAA x8) : sans lui, chaque bord d'objet est un escalier de
        ///     pixels qui scintille quand on bouge ;
        ///   - le FILTRAGE ANISOTROPE : le sol vu de biais reste net au loin ;
        ///   - des OMBRES fines (tres haute resolution, 4 cascades : nettes de pres, presentes
        ///     de loin) ;
        ///   - la SYNCHRO VERTICALE : une image par rafraichissement de l'ecran, pas de
        ///     dechirure horizontale quand on tourne la tete.
        /// Concept Unity : QualitySettings, ce sont les reglages de Edit > Project Settings >
        /// Quality ; on les force ici par le code pour qu'ils soient les memes chez tout le monde.
        /// </summary>
        ///
        /// FLUIDE (03/10, Martin : "le jeu n'est pas fluide du tout") : l'anticrenelage passe
        /// de x8 a x4 (a l'oeil, la meme image ; pour la carte graphique, moitie moins de
        /// travail sur chaque pixel, la mer de nuages surtout) ; deux lumieres "au pixel" au
        /// plus sur chaque objet (le soleil et la plus proche) -- les autres (lanternes des
        /// bots, sanctuaires, gargouilles) eclairent quand meme, mais sans redessiner
        /// l'objet une fois de plus pour chacune ; les ombres jusqu'a 130 m au lieu de 160.
        /// Concept Unity : en rendu "Forward", chaque lumiere au pixel qui touche un objet
        /// le fait redessiner EN ENTIER. Vingt lumieres sur le chateau, c'etaient vingt
        /// chateaux par image.
        static void Smooth(Camera view)
        {
            QualitySettings.antiAliasing = 4;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.softParticles = true;
            QualitySettings.vSyncCount = 1;
            if (view != null) { view.allowMSAA = true; view.allowHDR = true; }
        }

        /// <summary>
        /// LES REFLETS : une sonde de reflexion photographie une fois le ciel et l'ile ; l'or
        /// poli de la Couronne, l'acier des casques les refletent. Sans elle, le metal
        /// parait noir.
        /// Concept Unity : un ReflectionProbe est un appareil photo a 360 degres ; les
        /// materiaux lisses a proximite y lisent ce qui les entoure.
        /// </summary>
        static void CaptureReflections()
        {
            GameObject go = GameObject.Find("REFLETS");
            if (go == null) go = new GameObject("REFLETS");
            go.transform.position = new Vector3(0f, 60f, 0f);
            ReflectionProbe probe = go.GetComponent<ReflectionProbe>();
            if (probe == null) probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(2400f, 1200f, 2400f);
            probe.resolution = 128;
            probe.hdr = true;
            probe.intensity = 1f;
            probe.RenderProbe();
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
            Sun.color = new Color(1f, 0.9f, 0.74f);
            Sun.intensity = cfg != null ? cfg.sunIntensity : 1.15f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.75f;
            Sun.shadowNearPlane = 0.2f;
            RenderSettings.sun = Sun;
            QualitySettings.shadowDistance = 130f;
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
            // La tienne, toujours au pixel (c'est elle qu'on voit de pres) ; les autres se partagent le reste.
            Lamp.renderMode = LightRenderMode.ForcePixel;
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
