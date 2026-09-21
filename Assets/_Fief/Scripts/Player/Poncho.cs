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
        const int Rings = 9;

        /// <summary>
        /// Le profil du vetement, de l'encolure a l'ourlet.
        ///
        /// Les deux premiers anneaux sont l'ENCOLURE et les EPAULES : le tissu part
        /// d'un petit trou juste sous le menton et s'evase a plat sur les epaules.
        /// Sans eux, le poncho etait un cone dont le haut faisait 30 cm de rayon :
        /// en baissant les yeux on regardait par le trou et on voyait ses jambes
        /// au lieu de son vetement.
        /// </summary>
        static readonly float[] RingY = { 1.55f, 1.49f, 1.40f, 1.22f, 0.99f, 0.74f, 0.50f, 0.28f, 0.08f };
        static readonly float[] RingR = { 0.115f, 0.34f, 0.46f, 0.58f, 0.66f, 0.71f, 0.74f, 0.75f, 0.76f };

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

        public static Poncho Build(Transform parent, Color cloth, Color band, Color patch)
        {
            GameObject go = new GameObject("Poncho");
            go.transform.SetParent(parent, false);

            Poncho poncho = go.AddComponent<Poncho>();
            poncho.Create(cloth, band, patch);
            return poncho;
        }

        void Create(Color cloth, Color band, Color patch)
        {
            int count = Rings * Segments;
            rest = new Vector3[count];
            vertices = new Vector3[count];
            ringT = new float[count];
            segAngle = new float[count];

            for (int r = 0; r < Rings; r++)
            {
                float t = r / (float)(Rings - 1);
                float radius = RingR[r];
                float y = RingY[r];

                for (int s = 0; s < Segments; s++)
                {
                    float angle = (s / (float)Segments) * Mathf.PI * 2f;
                    int i = r * Segments + s;

                    // OURLET DECHIRE : sur les deux derniers etages, chaque pan
                    // descend d'une hauteur differente. C'est ce qui separe un
                    // vetement taille net d'une loque de mendiant.
                    float ragged = 0f;
                    if (r >= Rings - 2)
                    {
                        float tear = Hash(s * 7 + r * 31);
                        ragged = tear * (r == Rings - 1 ? 0.30f : 0.12f);
                    }
                    // l'encolure reste nette : c'est le bas qui est mange par l'usure
                    float wobble = r < 2 ? 0f : (Hash(s * 13 + r * 5) - 0.5f) * 0.05f;

                    rest[i] = new Vector3(Mathf.Sin(angle) * (radius + wobble), y + ragged,
                                          Mathf.Cos(angle) * (radius + wobble));
                    vertices[i] = rest[i];
                    ringT[i] = t;
                    segAngle[i] = angle;
                }
            }

            // Trois sous-maillages : la laine, une bande usee, et des PIECES
            // RAPIECEES semees au hasard sur le tissu. Une loque, c'est d'abord
            // un vetement qui a ete repare trop de fois.
            int quadsPerRing = Segments;
            var main = new System.Collections.Generic.List<int>();
            var stripe = new System.Collections.Generic.List<int>();
            var patches = new System.Collections.Generic.List<int>();

            for (int r = 0; r < Rings - 1; r++)
            {
                for (int s = 0; s < quadsPerRing; s++)
                {
                    var target = main;
                    if (r == Rings - 3) target = stripe;
                    else if (Hash(s * 17 + r * 101) < 0.11f) target = patches;

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
            mesh.subMeshCount = 3;
            mesh.SetTriangles(main, 0);
            mesh.SetTriangles(stripe, 1);
            mesh.SetTriangles(patches, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[]
            {
                MaterialFactory.Get(cloth),
                MaterialFactory.Get(band),
                MaterialFactory.Get(patch)
            };
        }

        /// <summary>Bruit reproductible entre 0 et 1 : les dechirures sont toujours les memes.</summary>
        static float Hash(int n)
        {
            float v = Mathf.Sin(n * 12.9898f) * 43758.5453f;
            return v - Mathf.Floor(v);
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
                // Le bas traine, les epaules ne bougent pas : l'encolure est cousue au corps.
                float lag = Mathf.Max(0f, t - 0.14f);
                lag = lag * lag * 1.35f;
                Vector3 p = rest[i];

                // vague qui fait le tour du corps et descend le long du tissu
                float phase = time * (4.5f + fast * 5f) + segAngle[i] * 2f + t * 3.4f;
                float wave = Mathf.Sin(phase) * (flutter + fast * 0.035f) * lag;

                p.z += back * lag + wave * 0.55f;
                p.x += side * lag + Mathf.Cos(phase * 0.8f) * wave;
                p.y += lag * fast * hemLift + wave * 0.35f;

                // l'ourlet ne traverse jamais le sol
                if (p.y < 0.01f) p.y = 0.01f;

                vertices[i] = p;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
