using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le poncho : un vrai tissu, simule sommet par sommet.
    ///
    /// Un cone ouvert qui tombe jusqu'au sol. A chaque image, chaque sommet est
    /// recalcule : le bas TRAINE derriere le mouvement, s'ecarte dans les virages,
    /// une vague permanente en fait le tour, et l'ourlet se souleve a la course
    /// sans jamais traverser le sol.
    ///
    /// DEUX PROFILS. Le vetement n'a pas la meme forme selon qui le regarde :
    ///
    ///   - DEHORS, il part de l'encolure : c'est la silhouette du mendiant, celle
    ///     qu'on voit sur l'ecran-titre et que verront les autres joueurs.
    ///   - PAR SES PROPRES YEUX, il part sous la poitrine. Le haut n'existe tout
    ///     simplement pas : a 20 cm de l'oeil il ne serait qu'une masse, et on n'en
    ///     verrait que la face interne. C'est le principe du "viewmodel" : le corps
    ///     qu'on voit de l'interieur n'est pas le meme objet que celui qu'on voit
    ///     de l'exterieur.
    ///
    /// TROIS PIEGES, tous rencontres en chemin :
    ///
    /// 1. Les faces sont doublees pour que le tissu se voie des deux cotes. Du coup
    ///    RecalculateNormals moyennait chaque normale avec son opposee, donc ZERO :
    ///    l'eclairage s'effondrait et le poncho devenait une masse noire. Les
    ///    normales sont calculees a la main sur les seules faces exterieures.
    ///
    /// 2. Le tissu etait le seul objet LISSE du jeu : ses quads partageaient leurs
    ///    sommets, les normales se moyennaient, on obtenait un degrade continu au
    ///    milieu d'un monde facette. Chaque quad a donc ses quatre sommets a lui.
    ///
    /// 3. Dupliquer des sommets sur un tissu simule, c'est risquer une fente a la
    ///    couture. La position au repos est donc une fonction PURE de (etage, pan) :
    ///    deux copies d'un meme point bougent forcement ensemble.
    /// </summary>
    public class Poncho : MonoBehaviour
    {
        const int Segments = 28;

        /// <summary>Profil vu de dehors : depuis l'encolure.</summary>
        static readonly float[] OutsideY = { 1.60f, 1.52f, 1.41f, 1.22f, 0.99f, 0.74f, 0.50f, 0.28f, 0.08f };
        static readonly float[] OutsideR = { 0.135f, 0.34f, 0.42f, 0.50f, 0.55f, 0.58f, 0.60f, 0.61f, 0.62f };

        /// <summary>
        /// Profil vu par ses propres yeux : commence a 1,20 m, soit 58 cm sous l'oeil.
        /// Le premier etage est a 66 cm de la lentille et apparait des qu'on baisse
        /// les yeux de 31 degres -- assez tot pour se sentir habille, assez loin pour
        /// ne jamais faire mur.
        /// </summary>
        static readonly float[] InsideY = { 1.20f, 1.06f, 0.88f, 0.68f, 0.47f, 0.26f, 0.06f };
        static readonly float[] InsideR = { 0.44f, 0.52f, 0.58f, 0.62f, 0.64f, 0.65f, 0.65f };

        [Header("Tissu")]
        public float trail = 0.085f;
        public float turnSway = 0.075f;
        public float flutter = 0.024f;
        public float hemLift = 0.075f;

        float[] ringY;
        float[] ringR;
        int rings;

        Mesh mesh;
        Vector3[] rest;
        Vector3[] vertices;
        Vector3[] normals;
        float[] ringT;
        float[] segAngle;

        int[] outward;          // une seule orientation : sert au calcul des normales

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
        /// Efface le vetement (touche F4). Outil de diagnostic : si la masse qui
        /// bouche l'ecran disparait, c'est le poncho ; sinon c'est autre chose.
        /// </summary>
        public void ToggleVisible()
        {
            MeshRenderer r = GetComponent<MeshRenderer>();
            if (r != null) r.enabled = !r.enabled;
        }

        /// <summary>Le poncho tel qu'on le voit de l'exterieur.</summary>
        public static Poncho Build(Transform parent, Color cloth, Color band, Color patch)
        {
            return Build(parent, cloth, band, patch, false);
        }

        /// <summary>
        /// <paramref name="throughOwnEyes"/> choisit le profil : le vetement complet
        /// vu de dehors, ou seulement sa partie basse quand on le porte.
        /// </summary>
        public static Poncho Build(Transform parent, Color cloth, Color band, Color patch, bool throughOwnEyes)
        {
            GameObject go = new GameObject(throughOwnEyes ? "PonchoSubjectif" : "Poncho");
            go.transform.SetParent(parent, false);

            Poncho poncho = go.AddComponent<Poncho>();
            poncho.ringY = throughOwnEyes ? InsideY : OutsideY;
            poncho.ringR = throughOwnEyes ? InsideR : OutsideR;
            poncho.rings = poncho.ringY.Length;
            poncho.Create(cloth, band, patch);
            return poncho;
        }

        /// <summary>
        /// Position au repos d'un point du vetement. Fonction PURE de (etage, pan) :
        /// c'est ce qui permet de dupliquer les sommets sans jamais ouvrir de fente,
        /// puisque deux copies du meme point donnent toujours le meme resultat.
        /// </summary>
        Vector3 RestPosition(int r, int s)
        {
            float angle = (s % Segments / (float)Segments) * Mathf.PI * 2f;

            // OURLET DECHIRE : sur les deux derniers etages, chaque pan descend d'une
            // hauteur differente. C'est ce qui separe un vetement taille net d'une
            // loque de mendiant.
            float ragged = 0f;
            if (r >= rings - 2) ragged = Hash((s % Segments) * 7 + r * 31) * (r == rings - 1 ? 0.30f : 0.12f);

            // le haut reste net : c'est le bas qui est mange par l'usure
            float wobble = r < 2 ? 0f : (Hash((s % Segments) * 13 + r * 5) - 0.5f) * 0.05f;

            float radius = ringR[r] + wobble;
            return new Vector3(Mathf.Sin(angle) * radius, ringY[r] + ragged, Mathf.Cos(angle) * radius);
        }

        void Create(Color cloth, Color band, Color patch)
        {
            // FACETTES, PAS DE LISSAGE : chaque quad recoit ses quatre sommets a lui,
            // garde sa propre normale, et le vetement parle la meme langue visuelle
            // que le reste du jeu -- des plis nets, pas un degrade.
            int quads = (rings - 1) * Segments;
            int count = quads * 4;

            rest = new Vector3[count];
            vertices = new Vector3[count];
            normals = new Vector3[count];
            ringT = new float[count];
            segAngle = new float[count];

            List<int> main = new List<int>();
            List<int> stripe = new List<int>();
            List<int> patches = new List<int>();
            List<int> single = new List<int>();

            int v = 0;
            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < Segments; s++)
                {
                    // les quatre coins du quad : (r,s) (r,s+1) (r+1,s) (r+1,s+1)
                    int a = v, b = v + 1, c = v + 2, d = v + 3;

                    Place(a, r, s);
                    Place(b, r, s + 1);
                    Place(c, r + 1, s);
                    Place(d, r + 1, s + 1);
                    v += 4;

                    List<int> target;
                    if (r == rings - 3) target = stripe;
                    else if (Hash(s * 17 + r * 101) < 0.11f) target = patches;
                    else target = main;

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

            mesh = new Mesh();
            mesh.name = "Poncho";
            mesh.vertices = vertices;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(main, 0);
            mesh.SetTriangles(stripe, 1);
            mesh.SetTriangles(patches, 2);
            RebuildNormals();
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

        /// <summary>Inscrit un sommet du quad : position au repos et coordonnees.</summary>
        void Place(int index, int r, int s)
        {
            rest[index] = RestPosition(r, s);
            vertices[index] = rest[index];
            ringT[index] = r / (float)(rings - 1);
            segAngle[index] = (s % Segments / (float)Segments) * Mathf.PI * 2f;
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
