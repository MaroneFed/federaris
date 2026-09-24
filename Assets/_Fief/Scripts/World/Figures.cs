using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les silhouettes des PNJ, en cubes. Une robe en tranches qui se resserrent
    /// (comme le mage et les rois de pierre : c'est la "famille" visuelle du monde),
    /// des epaules, une tete ou une capuche. Chaque PNJ ajoute ensuite ce qui le
    /// distingue : un casque et une hallebarde, une barbe et un baton.
    ///
    /// Tout est visuel : le collider, un seul, est pose par le PNJ lui-meme.
    /// </summary>
    public static class Figures
    {
        public struct Shape
        {
            /// <summary>Le pivot a tourner pour que le PNJ regarde quelqu'un.</summary>
            public Transform root;
            /// <summary>Hauteur des epaules, en metres, pour y accrocher le reste.</summary>
            public float shoulders;
        }

        public static Shape Robed(Transform parent, float height, float width, Color cloth, Color clothDark,
                                 Color skin, bool hooded)
        {
            GameObject go = new GameObject("Silhouette");
            go.transform.SetParent(parent, false);
            Transform f = go.transform;

            float k = height / 2.6f;
            float[] widths = { 1f, 0.84f, 0.68f, 0.56f };
            for (int i = 0; i < widths.Length; i++)
            {
                GameObject slice = Proto.Cube(f, new Vector3(0f, (0.26f + i * 0.44f) * k, 0f),
                                              new Vector3(widths[i] * width, 0.48f * k, widths[i] * width * 0.9f),
                                              i % 2 == 0 ? cloth : clothDark, "Robe");
                slice.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }
            float shoulders = 1.95f * k;
            Proto.Cube(f, new Vector3(0f, shoulders, 0f), new Vector3(0.78f * width, 0.22f * k, 0.46f * width), clothDark, "Épaules");

            if (hooded)
            {
                GameObject hood = Proto.Cube(f, new Vector3(0f, shoulders + 0.3f * k, 0f), new Vector3(0.42f * k, 0.46f * k, 0.42f * k), cloth, "Capuche");
                hood.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                Proto.Cube(f, new Vector3(0f, shoulders + 0.27f * k, 0.17f * k), new Vector3(0.26f * k, 0.26f * k, 0.06f * k), skin, "Visage");
            }
            else
            {
                Proto.Cube(f, new Vector3(0f, shoulders + 0.3f * k, 0f), new Vector3(0.3f * k, 0.36f * k, 0.3f * k), skin, "Tête");
            }

            // Les bras, pendants, legerement en avant.
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject arm = Proto.Cube(f, new Vector3(side * 0.36f * width, shoulders - 0.4f * k, 0.06f),
                                            new Vector3(0.16f * width, 0.8f * k, 0.18f * width), clothDark, "Bras");
                arm.transform.localRotation = Quaternion.Euler(-10f, 0f, side * 6f);
            }

            Shape body = new Shape();
            body.root = f;
            body.shoulders = shoulders;
            return body;
        }

        /// <summary>Tourne doucement un pivot vers une cible, a plat.</summary>
        public static void Face(Transform pivot, Vector3 target, float degreesPerSecond)
        {
            Vector3 to = target - pivot.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.05f) return;
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            pivot.rotation = Quaternion.RotateTowards(pivot.rotation, look, degreesPerSecond * Time.deltaTime);
        }

        /// <summary>Une respiration : la silhouette gonfle d'un pour cent, lentement.</summary>
        public static void Breathe(Transform pivot, float seed)
        {
            float b = 1f + Mathf.Sin(Time.time * 1.3f + seed) * 0.012f;
            pivot.localScale = new Vector3(b, 1f + (b - 1f) * 0.5f, b);
        }
    }
}
