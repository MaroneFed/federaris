using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le poncho : un vrai tissu, simule sommet par sommet.
    ///
    /// C'est un cone ouvert (20 pans sur 7 etages) qui part des epaules et tombe
    /// jusqu'au sol. A chaque image, on recalcule la position de chaque sommet :
    ///
    ///   - le bas TRAINE derriere le mouvement (plus on descend, plus le retard
    ///     est grand : c'est ce qui fait qu'un tissu "suit" au lieu d'etre rigide) ;
    ///   - il part vers l'exterieur dans les virages, par inertie ;
    ///   - il ondule en permanence, avec une vague qui fait le tour du corps ;
    ///   - l'ourlet se souleve quand on court.
    ///
    /// Tout est en espace LOCAL du personnage : avant = +Z, droite = +X. Le tissu
    /// n'a donc pas besoin de savoir ou on regarde, seulement a quelle vitesse on va.
    /// </summary>
    public class Poncho : MonoBehaviour
    {
        const int Segments = 20;
        const int Rings = 7;

        [Header("Silhouette")]
        public float topY = 1.42f;
        public float bottomY = 0.08f;
        public float topRadius = 0.30f;
        public float bottomRadius = 0.72f;

        [Header("Tissu")]
        public float trail = 0.085f;        // recul du bas quand on avance
        public float turnSway = 0.075f;     // ecart dans les virages
        public float flutter = 0.030f;      // ondulation permanente
        public float hemLift = 0.055f;      // l'ourlet se souleve a la course

        Mesh mesh;
        Vector3[] rest;
        Vector3[] vertices;
        float[] ringT;
        float[] segAngle;

        float speed;
        float turnRate;
        float previousYaw;
        float time;

        public void SetMotion(float currentSpeed, float yawDegrees)
        {
            speed = currentSpeed;
            float delta = Mathf.DeltaAngle(previousYaw, yawDegrees);
            previousYaw = yawDegrees;
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            turnRate = Mathf.Lerp(turnRate, Mathf.Clamp(delta / dt, -260f, 260f), 1f - Mathf.Exp(-9f * dt));
        }

        public static Poncho Build(Transform parent, Color cloth, Color band)
        {
            GameObject go = new GameObject("Poncho");
            go.transform.SetParent(parent, false);

            Poncho poncho = go.AddComponent<Poncho>();
            poncho.Create(cloth, band);
            return poncho;
        }

        void Create(Color cloth, Color band)
        {
            int count = Rings * Segments;
            rest = new Vector3[count];
            vertices = new Vector3[count];
            ringT = new float[count];
            segAngle = new float[count];

            for (int r = 0; r < Rings; r++)
            {
                float t = r / (float)(Rings - 1);
                // Le tissu s'evase vite en haut puis tombe droit : silhouette de cape.
                float radius = Mathf.Lerp(topRadius, bottomRadius, Mathf.Pow(t, 0.62f));
                float y = Mathf.Lerp(topY, bottomY, t);

                for (int s = 0; s < Segments; s++)
                {
                    float angle = (s / (float)Segments) * Mathf.PI * 2f;
                    int i = r * Segments + s;
                    rest[i] = new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius);
                    vertices[i] = rest[i];
                    ringT[i] = t;
                    segAngle[i] = angle;
                }
            }

            // Deux sous-maillages : le tissu, et une bande claire vers le bas.
            int quadsPerRing = Segments;
            var main = new System.Collections.Generic.List<int>();
            var stripe = new System.Collections.Generic.List<int>();

            for (int r = 0; r < Rings - 1; r++)
            {
                var target = (r == Rings - 3) ? stripe : main;
                for (int s = 0; s < quadsPerRing; s++)
                {
                    int a = r * Segments + s;
                    int b = r * Segments + (s + 1) % Segments;
                    int c = (r + 1) * Segments + s;
                    int d = (r + 1) * Segments + (s + 1) % Segments;

                    target.Add(a); target.Add(c); target.Add(b);
                    target.Add(b); target.Add(c); target.Add(d);
                    // face interieure : un tissu se voit des deux cotes
                    target.Add(b); target.Add(c); target.Add(a);
                    target.Add(d); target.Add(c); target.Add(b);
                }
            }

            mesh = new Mesh();
            mesh.name = "Poncho";
            mesh.vertices = vertices;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(main, 0);
            mesh.SetTriangles(stripe, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[]
            {
                MaterialFactory.Get(cloth),
                MaterialFactory.Get(band)
            };
        }

        void LateUpdate()
        {
            if (mesh == null) return;

            float dt = Time.deltaTime;
            time += dt;

            float fast = Mathf.Clamp01(speed / 11f);
            float back = -speed * trail;
            float side = -turnRate * turnSway * 0.01f;

            for (int i = 0; i < rest.Length; i++)
            {
                float t = ringT[i];
                float lag = t * t;                       // le bas traine, le haut suit l'epaule
                Vector3 p = rest[i];

                // vague qui fait le tour du corps et descend le long du tissu
                float phase = time * (4.5f + fast * 5f) + segAngle[i] * 2f + t * 3.4f;
                float wave = Mathf.Sin(phase) * (flutter + fast * 0.035f) * lag;

                p.z += back * lag + wave * 0.55f;
                p.x += side * lag + Mathf.Cos(phase * 0.8f) * wave;
                p.y += lag * fast * hemLift + wave * 0.35f;

                // l'ourlet ne traverse jamais le sol
                if (p.y < 0.015f) p.y = 0.015f;

                vertices[i] = p;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
