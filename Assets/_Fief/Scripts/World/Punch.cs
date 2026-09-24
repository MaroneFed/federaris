using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE COUP ENCAISSE : quand on touche quelqu'un (rival, loup, revenant), sa
    /// silhouette recule d'un coup, se tasse, puis revient. Un dixieme de seconde --
    /// assez pour que l'oeil voie que le coup a porte. Purement visuel : on bouge le
    /// dessin (la silhouette), jamais le corps physique.
    /// </summary>
    public class Punch : MonoBehaviour
    {
        Vector3 rest;
        Vector3 kick;
        float t = 1f;
        bool ready;

        public static void Apply(Transform visual, Vector3 fromWorld)
        {
            if (visual == null) return;
            Punch p = visual.GetComponent<Punch>();
            if (p == null) p = visual.gameObject.AddComponent<Punch>();
            if (!p.ready) { p.rest = visual.localPosition; p.ready = true; }
            Vector3 away = visual.position - fromWorld;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : -visual.forward;
            p.kick = visual.parent != null ? visual.parent.InverseTransformDirection(away) * 0.28f : away * 0.28f;
            p.t = 0f;
        }

        void LateUpdate()
        {
            if (!ready || t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / 0.28f);
            float k = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
            transform.localPosition = rest + kick * k;
            float squash = 1f - 0.12f * k;
            transform.localScale = new Vector3(1f + 0.06f * k, squash, 1f + 0.06f * k);
            if (t >= 1f)
            {
                transform.localPosition = rest;
                transform.localScale = Vector3.one;
            }
        }
    }
}
