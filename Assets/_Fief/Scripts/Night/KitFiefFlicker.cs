using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Fait trembler la lumiere d'un feu du kit (torche, brasero) : l'intensite monte et descend
    /// sans jamais se repeter, et la lumiere bouge de quelques centimetres, donc les ombres dansent.
    ///
    /// Concept Unity : RequireComponent oblige l'objet a porter aussi une Light ; Unity l'ajoute
    /// tout seul si elle manque.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class KitFiefFlicker : MonoBehaviour
    {
        [Range(0f, 1f)] public float amplitude = 0.28f;
        public float vitesse = 7f;
        public float deplacement = 0.03f;

        Light lumiere;
        float intensiteBase;
        Vector3 positionBase;
        float graine;

        void Awake()
        {
            lumiere = GetComponent<Light>();
            intensiteBase = lumiere.intensity;
            positionBase = transform.localPosition;
            graine = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * vitesse + graine;
            float n = Mathf.PerlinNoise(t, graine) * 0.7f + Mathf.PerlinNoise(t * 2.7f, graine + 5f) * 0.3f;
            lumiere.intensity = intensiteBase * (1f + (n - 0.5f) * 2f * amplitude);
            Vector3 d = new Vector3(
                Mathf.PerlinNoise(t * 0.6f, 1f) - 0.5f,
                Mathf.PerlinNoise(t * 0.6f, 7f) - 0.5f,
                Mathf.PerlinNoise(t * 0.6f, 13f) - 0.5f);
            transform.localPosition = positionBase + d * 2f * deplacement;
        }
    }
}
