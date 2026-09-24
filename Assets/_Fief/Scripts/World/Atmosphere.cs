using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LA PENOMBRE. Lumiere, brume, portee du regard.
    ///
    /// L'ambiance visee : un sous-bois par temps couvert, en fin d'apres-midi.
    /// C'est naturel -- chacun a deja marche dans une foret comme ca -- et c'est
    /// inquietant sans effet special.
    ///
    /// LA PREMIERE VERSION NE L'ETAIT PAS, et Martin l'a vu tout de suite : "la
    /// lumiere pas tres naturelle". Trois erreurs :
    ///
    ///   1. Un soleil a 14 degres, presque horizontal. Sous un couvert dense il ne
    ///      passerait pas ; il eclairait les troncs de cote, comme un projecteur.
    ///   2. Tout etait bleu froid, sauf la lanterne, orange sature. Ce contraste de
    ///      complementaires est un effet de jeu video, pas de foret.
    ///   3. La brume etait PLUS SOMBRE que les arbres : au loin tout plongeait dans
    ///      le noir, comme un vide. Dans une vraie foret brumeuse c'est l'inverse :
    ///      l'air charge d'humidite diffuse la lumiere, il est plus CLAIR que les
    ///      troncs, et les arbres se decoupent en silhouettes sombres sur un fond
    ///      gris-vert. C'est plus naturel, et c'est justement ce qui fait peur : on
    ///      devine des formes, on ne les voit pas.
    ///
    /// CE QUI EST FAIT MAINTENANT
    ///
    ///   - La lumiere vient d'EN HAUT (52 degres), neutre et un peu chaude, faible,
    ///     avec des ombres adoucies : un ciel couvert filtre par les feuilles.
    ///   - L'ambiant est vert-gris en haut, brun en bas : c'est la lumiere qui
    ///     rebondit sur les feuilles et sur l'humus. Une foret eclaire en vert.
    ///   - La brume est gris-vert, plus claire que les ombres proches.
    ///   - La lanterne est plus douce et moins orange : elle aide a lire le sol, elle
    ///     ne repeint pas la foret.
    ///
    /// LA PORTEE DU REGARD SERT AUSSI LA PERFORMANCE. Le plan lointain de la camera
    /// est cale juste au-dela de la brume : ce qui est plus loin n'est pas dessine.
    /// Une foret dense coute donc MOINS cher qu'une plaine degagee.
    /// </summary>
    public static class Atmosphere
    {
        public static Light Sun;
        public static Light Lamp;

        public static void Apply(GameConfig cfg, Camera view, Transform player)
        {
            Color haze = cfg != null ? cfg.hazeColor : Palette.Haze;
            float sight = cfg != null ? cfg.sightDistance : 40f;

            // --- la brume -------------------------------------------------------
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = haze;

            // Exp2 : le facteur vaut exp(-(d * densite)^2). A 5 % de visible il reste
            // d * densite = 1.73. On regle donc la densite sur la portee VOULUE au
            // lieu de tatonner sur un nombre qui ne veut rien dire.
            RenderSettings.fogDensity = 1.7308f / Mathf.Max(5f, sight);

            // --- le ciel et l'ambiant -------------------------------------------
            // Pas de skybox : sous un couvert pareil on ne voit pas le ciel, et un
            // degrade bleu au-dessus des cimes casserait tout l'enfermement.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;

            // Vert-gris en haut (le ciel couvert vu a travers les feuilles), vert
            // sourd a hauteur d'homme, brun en bas (l'humus). Une foret eclaire en
            // vert : c'est ce qui manquait pour que ca paraisse vrai.
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.25f, 0.23f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.16f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.07f, 0.06f);
            RenderSettings.reflectionIntensity = 0f;

            if (view != null)
            {
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = haze;

                // Juste au-dela de la brume : a cette distance il ne reste que 0,4 %
                // de l'objet, donc le supprimer ne se voit pas -- et tout ce qui est
                // derriere n'est jamais dessine.
                view.farClipPlane = sight * 2.4f;
                view.nearClipPlane = 0.08f;
            }

            ApplySun(cfg);
            ApplyLamp(cfg, player);
        }

        static void ApplySun(GameConfig cfg)
        {
            if (Sun == null)
            {
                GameObject go = new GameObject("LUMIERE RASANTE");
                Sun = go.AddComponent<Light>();
            }
            Sun.type = LightType.Directional;

            // D'en haut, pas de cote : sous un couvert, la lumiere tombe. Neutre et un
            // peu chaude, faible, ombres adoucies -- un ciel couvert en fin de journee.
            float elevation = cfg != null ? cfg.sunElevation : 52f;
            Sun.transform.rotation = Quaternion.Euler(elevation, 38f, 0f);
            Sun.color = new Color(0.88f, 0.85f, 0.76f);
            Sun.intensity = cfg != null ? cfg.sunIntensity : 0.45f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.62f;
            RenderSettings.sun = Sun;

            // Les ombres ne portent pas plus loin que la vue : au-dela c'est du calcul
            // jete a la poubelle par la brume.
            Sun.shadowNearPlane = 0.2f;
            QualitySettings.shadowDistance = (cfg != null ? cfg.sightDistance : 40f) * 1.2f;
        }

        static void ApplyLamp(GameConfig cfg, Transform player)
        {
            if (player == null) return;

            if (Lamp == null)
            {
                GameObject go = new GameObject("LANTERNE");
                go.transform.SetParent(player, false);

                // A hauteur de hanche et legerement devant : la flaque de lumiere
                // tombe la ou on pose les pieds, et les troncs proches se detachent.
                go.transform.localPosition = new Vector3(0.18f, 1.15f, 0.30f);
                Lamp = go.AddComponent<Light>();
                go.AddComponent<LampFlicker>();
            }

            // Une flamme a travers un verre sale : chaude, mais pas orange. Plus
            // saturee, elle repeignait tout ce qu'elle touchait.
            Lamp.type = LightType.Point;
            Lamp.color = new Color(1f, 0.89f, 0.72f);
            Lamp.intensity = cfg != null ? cfg.lampIntensity : 1.0f;
            Lamp.range = cfg != null ? cfg.lampRange : 13f;
            Lamp.shadows = LightShadows.None;   // une seconde passe d'ombres coute cher pour rien
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
            if (lamp != null) baseIntensity = lamp.intensity;
            seed = Random.Range(0f, 100f);
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
