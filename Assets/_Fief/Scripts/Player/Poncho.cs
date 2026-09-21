using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le poncho : un vrai tissu, simule sommet par sommet.
    ///
    /// Un cone ouvert qui part des epaules et tombe jusqu'au sol. A chaque image,
    /// chaque sommet est recalcule :
    ///   - le bas TRAINE derriere le mouvement, d'autant plus qu'on descend ;
    ///   - il s'ecarte dans les virages, par inertie ;
    ///   - une vague permanente en fait le tour ;
    ///   - l'ourlet se souleve a la course et ne traverse jamais le sol.
    ///
    /// DEUX PIEGES, tous deux rencontres :
    ///
    /// 1. Les faces sont doublees pour que le tissu se voie aussi de l'interieur.
    ///    Du coup RecalculateNormals faisait la moyenne entre une normale et son
    ///    opposee, donc ZERO : l'eclairage s'effondrait et le poncho devenait une
    ///    masse noire. Les normales sont donc calculees a la main, a partir des
    ///    seules faces exterieures.
    ///
    /// 2. En premiere personne, tout ce qui est au-dessus de la taille est a moins
    ///    d'un metre de l'oeil et remplissait l'ecran d'une masse sombre. Les quatre
    ///    premiers etages sont donc ranges dans leur propre sous-maillage, qu'on
    ///    vide quand on regarde par ses yeux.
    /// </summary>
    public class Poncho : MonoBehaviour
    {
        const int Segments = 28;
        const int Rings = 9;

        /// <summary>Profil du vetement, de l'encolure a l'ourlet.</summary>
        static readonly float[] RingY = { 1.60f, 1.52f, 1.41f, 1.22f, 0.99f, 0.74f, 0.50f, 0.28f, 0.08f };
        static readonly float[] RingR = { 0.135f, 0.34f, 0.42f, 0.50f, 0.55f, 0.58f, 0.60f, 0.61f, 0.62f };

        [Header("Tissu")]
        public float trail = 0.085f;
        public float turnSway = 0.075f;
        public float flutter = 0.024f;
        public float hemLift = 0.075f;

        Mesh mesh;
        Vector3[] rest;
        Vector3[] vertices;
        Vector3[] normals;
        float[] ringT;
        float[] segAngle;

        /// <summary>
        /// Etages caches en premiere personne. Tout ce qui est au-dessus de la taille
        /// est retire : de l'interieur, ces pans-la sont a moins d'un metre de l'oeil,
        /// ils bouchent l'ecran, et on n'en voit que la face INTERNE -- qui est par
        /// definition toujours a l'ombre. C'est ce que font tous les jeux en vue
        /// subjective : on ne montre que le bas du vetement, les jambes et les mains.
        /// </summary>
        const int HiddenRowsInFirstPerson = 4;

        int[] outward;          // une seule orientation : sert au calcul des normales
        int[] upperTriangles;   // le haut du vetement, masque en premiere personne
        static readonly int[] Nothing = new int[0];

        bool topVisible = true;

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

        /// <summary>
        /// En premiere personne on retire l'encolure et les epaules : elles sont trop
        /// pres de l'oeil et couvriraient tout l'ecran. On garde tout le reste, donc
        /// on voit bien son poncho en baissant les yeux.
        /// </summary>
        public void SetTopVisible(bool value)
        {
            if (topVisible == value || mesh == null) return;
            topVisible = value;
            mesh.SetTriangles(value ? upperTriangles : Nothing, 3);
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
            normals = new Vector3[count];
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

                    // OURLET DECHIRE : sur les deux derniers etages, chaque pan descend
                    // d'une hauteur differente. C'est ce qui separe un vetement taille
                    // net d'une loque de mendiant.
                    float ragged = 0f;
                    if (r >= Rings - 2) ragged = Hash(s * 7 + r * 31) * (r == Rings - 1 ? 0.30f : 0.12f);

                    // l'encolure reste nette : c'est le bas qui est mange par l'usure
                    float wobble = r < 2 ? 0f : (Hash(s * 13 + r * 5) - 0.5f) * 0.05f;

                    rest[i] = new Vector3(Mathf.Sin(angle) * (radius + wobble), y + ragged,
                                          Mathf.Cos(angle) * (radius + wobble));
                    vertices[i] = rest[i];
                    ringT[i] = t;
                    segAngle[i] = angle;
                }
            }

            // Quatre sous-maillages : la laine, une bande usee, des pieces rapiecees,
            // et l'encolure a part pour pouvoir la retirer en premiere personne.
            List<int> main = new List<int>();
            List<int> stripe = new List<int>();
            List<int> patches = new List<int>();
            List<int> upper = new List<int>();
            List<int> single = new List<int>();

            for (int r = 0; r < Rings - 1; r++)
            {
                for (int s = 0; s < Segments; s++)
                {
                    List<int> target;
                    if (r < HiddenRowsInFirstPerson) target = upper;
                    else if (r == Rings - 3) target = stripe;
                    else if (Hash(s * 17 + r * 101) < 0.11f) target = patches;
                    else target = main;

                    int a = r * Segments + s;
                    int b = r * Segments + (s + 1) % Segments;
                    int c = (r + 1) * Segments + s;
                    int d = (r + 1) * Segments + (s + 1) % Segments;

                    // faces exterieures : ce sont elles qui donnent l'eclairage
                    single.Add(a); single.Add(c); single.Add(b);
                    single.Add(b); single.Add(c); single.Add(d);

                    target.Add(a); target.Add(c); target.Add(b);
                    target.Add(b); target.Add(c); target.Add(d);
                    // faces interieures : un tissu se voit des deux cotes
                    target.Add(b); target.Add(c); target.Add(a);
                    target.Add(d); target.Add(c); target.Add(b);
                }
            }

            outward = single.ToArray();
            upperTriangles = upper.ToArray();

            mesh = new Mesh();
            mesh.name = "Poncho";
            mesh.vertices = vertices;
            mesh.subMeshCount = 4;
            mesh.SetTriangles(main, 0);
            mesh.SetTriangles(stripe, 1);
            mesh.SetTriangles(patches, 2);
            mesh.SetTriangles(upper, 3);
            RebuildNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[]
            {
                MaterialFactory.Get(cloth),
                MaterialFactory.Get(band),
                MaterialFactory.Get(patch),
                MaterialFactory.Get(cloth)
            };
        }

        /// <summary>
        /// Normales calculees a la main sur les seules faces exterieures.
        /// RecalculateNormals ne peut pas le faire : avec les faces doublees, il
        /// moyennerait chaque normale avec son opposee et obtiendrait zero.
        /// </summary>
        void RebuildNormals()
        {
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.zero;

            for (int i = 0; i < outward.Length; i += 3)
            {
                int a = outward[i];
                int b = outward[i + 1];
                int c = outward[i + 2];
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                normals[a] += face;
                normals[b] += face;
                normals[c] += face;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = normals[i].sqrMagnitude > 0.0000001f
                    ? normals[i].normalized
                    : Vector3.up;
            }

            mesh.normals = normals;
        }

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

                float phase = time * (4.5f + fast * 5f) + segAngle[i] * 2f + t * 3.4f;
                float wave = Mathf.Sin(phase) * (flutter + fast * 0.03f) * lag;

                p.z += back * lag + wave * 0.55f;
                p.x += side * lag + Mathf.Cos(phase * 0.8f) * wave;
                p.y += lag * fast * hemLift + wave * 0.35f;

                if (p.y < 0.01f) p.y = 0.01f;

                vertices[i] = p;
            }

            mesh.vertices = vertices;
            RebuildNormals();
            mesh.RecalculateBounds();
        }
    }
}
