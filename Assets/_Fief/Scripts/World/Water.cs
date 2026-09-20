using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Un lac : un disque de facettes qui ondule.
    ///
    /// La surface est un maillage radial dont on recalcule la hauteur de chaque sommet
    /// a chaque image, a partir de deux vagues croisees. Ca ne coute presque rien
    /// (quelques centaines de sommets) et ca suffit pour que l'eau ait l'air vivante.
    /// </summary>
    public class Water : MonoBehaviour
    {
        const int Rings = 7;
        const int Segments = 26;

        Mesh mesh;
        Vector3[] vertices;
        Vector3[] rest;

        public float waveHeight = 0.16f;
        public float waveSpeed = 0.9f;

        public static Water Create(Transform parent, Vector3 centre, float radius, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = centre;

            Water water = go.AddComponent<Water>();
            water.BuildMesh(radius);
            return water;
        }

        void BuildMesh(float radius)
        {
            int count = 1 + Rings * Segments;
            vertices = new Vector3[count];
            rest = new Vector3[count];

            vertices[0] = Vector3.zero;

            for (int r = 0; r < Rings; r++)
            {
                // Les anneaux se resserrent vers le bord : la rive reste nette.
                float t = (r + 1) / (float)Rings;
                float ringRadius = radius * Mathf.Pow(t, 0.82f);

                for (int seg = 0; seg < Segments; seg++)
                {
                    float angle = (seg / (float)Segments) * Mathf.PI * 2f;
                    vertices[1 + r * Segments + seg] =
                        new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
                }
            }

            for (int i = 0; i < count; i++) rest[i] = vertices[i];

            // --- triangles : un eventail au centre, puis des anneaux
            int triangleCount = Segments + (Rings - 1) * Segments * 2;
            int[] triangles = new int[triangleCount * 3];
            int t2 = 0;

            for (int seg = 0; seg < Segments; seg++)
            {
                int a = 1 + seg;
                int b = 1 + (seg + 1) % Segments;
                triangles[t2++] = 0; triangles[t2++] = a; triangles[t2++] = b;
            }

            for (int r = 0; r < Rings - 1; r++)
            {
                for (int seg = 0; seg < Segments; seg++)
                {
                    int inner = 1 + r * Segments + seg;
                    int innerNext = 1 + r * Segments + (seg + 1) % Segments;
                    int outer = 1 + (r + 1) * Segments + seg;
                    int outerNext = 1 + (r + 1) * Segments + (seg + 1) % Segments;

                    triangles[t2++] = inner; triangles[t2++] = outer; triangles[t2++] = innerNext;
                    triangles[t2++] = innerNext; triangles[t2++] = outer; triangles[t2++] = outerNext;
                }
            }

            mesh = new Mesh();
            mesh.name = "Lac";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialFactory.GetTransparent(Palette.Water);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            if (mesh == null) return;

            float time = Time.time * waveSpeed;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = rest[i];
                float wave = Mathf.Sin(p.x * 0.18f + time) * 0.6f
                           + Mathf.Sin(p.z * 0.24f + time * 1.35f) * 0.4f;
                vertices[i] = new Vector3(p.x, wave * waveHeight, p.z);
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }
    }
}
