using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LA PENOMBRE. Lumiere, brume, portee du regard.
    ///
    /// Une foret sombre ne se fabrique pas en baissant la luminosite -- ca donne du
    /// gris sale et illisible. Elle tient a quatre choses, et il faut les quatre :
    ///
    ///   1. UNE BRUME DENSE ET COLOREE. C'est elle qui ferme le monde. A 40 m on ne
    ///      voit plus rien, donc on ne sait jamais ce qu'il y a derriere les arbres.
    ///      Elle est bleu-vert tres sombre, pas grise : une brume grise a l'air d'un
    ///      bug de rendu, une brume teintee a l'air d'un lieu.
    ///   2. UNE LUMIERE RASANTE ET FROIDE, faible, presque horizontale. Elle ne sert
    ///      pas a eclairer mais a DECOUPER : les futs pales des hetres l'accrochent,
    ///      et les troncs projettent de longues ombres entre lesquelles on avance.
    ///   3. UN AMBIANT SOMBRE MAIS PAS NOIR, legerement bleu. Sans lui les faces a
    ///      l'ombre seraient du noir pur, et le low-poly devient une bouillie.
    ///   4. UNE LANTERNE SUR LE JOUEUR. C'est le point crucial : sans elle, sombre
    ///      veut dire "on ne voit rien" et le jeu devient penible. Avec elle, on
    ///      emmene une flaque de lumiere, le sol reste lisible sous ses pieds, et la
    ///      foret reste noire au-dela. C'est ca, l'inquietude : voir un peu.
    ///
    /// LA PORTEE DU REGARD SERT AUSSI LA PERFORMANCE. Le plan lointain de la camera
    /// est cale juste au-dela de la brume : tout ce qui est plus loin n'est pas
    /// dessine du tout. Une foret dense coute donc MOINS cher qu'une plaine degagee.
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
            RenderSettings.ambientSkyColor = new Color(0.13f, 0.16f, 0.18f);
            RenderSettings.ambientEquatorColor = new Color(0.09f, 0.11f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.06f);
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

            // Presque horizontale : les ombres sont longues et traversent tout le
            // champ de vision. Une lumiere zenithale aplatirait la foret.
            Sun.transform.rotation = Quaternion.Euler(14f, 38f, 0f);
            Sun.color = new Color(0.62f, 0.70f, 0.82f);
            Sun.intensity = cfg != null ? cfg.sunIntensity : 0.5f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.82f;

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

            Lamp.type = LightType.Point;
            Lamp.color = new Color(1f, 0.82f, 0.56f);
            Lamp.intensity = cfg != null ? cfg.lampIntensity : 1.35f;
            Lamp.range = cfg != null ? cfg.lampRange : 15f;
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
