using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Fait danser une flamme : elle s'etire, se tasse, penche un peu. Trois ondes
    /// de periodes differentes, comme pour la lanterne, pour que l'oeil ne trouve
    /// jamais la boucle. A poser sur le cube lumineux d'une torche ou d'un brasero.
    /// </summary>
    public class Flame : MonoBehaviour
    {
        Vector3 baseScale;
        Quaternion baseRotation;
        float seed;

        void Start()
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            float t = Time.time * 1.0f + seed;
            float stretch = 1f + Mathf.Sin(t * 7.3f) * 0.12f + Mathf.Sin(t * 13.1f) * 0.07f;
            float squash = 1f - (stretch - 1f) * 0.5f;
            transform.localScale = new Vector3(baseScale.x * squash, baseScale.y * stretch, baseScale.z * squash);
            transform.localRotation = baseRotation * Quaternion.Euler(Mathf.Sin(t * 3.7f) * 6f, t * 40f, Mathf.Sin(t * 5.1f) * 6f);
        }
    }
}
