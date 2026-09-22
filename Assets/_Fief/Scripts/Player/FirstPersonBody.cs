using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CORPS VU DE L'INTERIEUR.
    ///
    /// Ce n'est PAS le meme objet que CharacterRig. Le personnage qu'on voit de
    /// dehors et celui qu'on voit de dedans sont deux modeles separes : c'est ce
    /// que fait tout jeu a la premiere personne, et c'est ce qui evite de masquer
    /// un corps exterieur piece par piece en en oubliant toujours une.
    ///
    /// CE QU'IL CONTIENT, ET RIEN DE PLUS : deux avant-bras, deux mains, deux
    /// jambes, deux pieds.
    ///
    /// Le poncho et le baton en ont ete RETIRES, et c'est une decision, pas un
    /// oubli. Un poncho porte est un cone dont on occupe le centre : en baissant
    /// les yeux on n'en voit pas un vetement mais un ANNEAU qui encercle l'image.
    /// Il couvrait 39 % de l'ecran -- pas enorme en surface, illisible en forme,
    /// parce qu'un anneau enferme le regard meme quand son centre est libre.
    /// Aucune mesure de surface ne rattrape ca ; il fallait l'enlever.
    ///
    /// Les deux gardent toute leur place sur CharacterRig : la silhouette du
    /// mendiant existe toujours, on la voit sur l'ecran-titre, et les autres
    /// joueurs la verront en multijoueur. On ne la porte simplement pas devant
    /// ses propres yeux.
    ///
    /// LA REGLE, verifiee par le jeu lui-meme au montage puis a chaque image :
    /// aucune piece a moins de 40 cm de l'oeil, aucune couvrant plus de 60 % de
    /// la hauteur d'ecran. Elle a deja rattrape deux erreurs de ma main.
    /// </summary>
    public class FirstPersonBody : MonoBehaviour
    {
        /// <summary>Rien ne doit approcher l'oeil de plus pres que ca.</summary>
        public const float Clearance = 0.40f;

        /// <summary>
        /// Part de la hauteur d'ecran qu'une piece a le droit d'occuper.
        ///
        /// C'est la largeur rapportee a la distance qui compte, pas l'altitude :
        /// une piece fine peut passer au-dessus de l'oeil sans gener, une piece
        /// large bouche tout en restant plus bas.
        /// </summary>
        public const float MaxScreenShare = 0.60f;

        [Header("Reglage a chaud (Hierarchy > JOUEUR > CorpsSubjectif)")]
        [Tooltip("Decale les deux bras. Z positif = plus loin devant.")]
        public Vector3 armOffset = Vector3.zero;

        Transform armL, armR;
        Transform legL, legR;
        Renderer[] parts;
        bool[] reported;

        float cycle;
        float speedSmoothed;
        float swingTimer;

        Vector3 armHomeL, armHomeR;

        public float Speed;
        public float RunSpeed = 11f;

        // ------------------------------------------------------------------ montage

        public static FirstPersonBody Build(Transform parent, Color cloth, Color band, Color patch)
        {
            GameObject go = new GameObject("CorpsSubjectif");
            go.transform.SetParent(parent, false);

            FirstPersonBody body = go.AddComponent<FirstPersonBody>();

            Proto.BeginVisualOnly();
            body.Assemble(cloth, band, patch);
            Proto.EndVisualOnly();

            body.parts = go.GetComponentsInChildren<Renderer>(true);
            body.CheckRules();
            return body;
        }

        /// <summary>
        /// Verifie DEGAGEMENT et ENCOMBREMENT sur le corps reellement construit, au
        /// montage PUIS a chaque image : une pose au repos correcte ne dit rien des
        /// gestes. Le coup de baton, arme en arriere, passait a 28 cm de l'oeil.
        ///
        /// Les regles en commentaire ne servent a rien : c'est en ajoutant une piece
        /// six mois plus tard, sans y penser, qu'on remet une masse devant l'oeil.
        /// Celle-ci se signale toute seule.
        /// </summary>
        public void CheckRules()
        {
            if (parts == null) return;
            if (reported == null || reported.Length != parts.Length) reported = new bool[parts.Length];

            GameConfig config = Game.Config;
            float eyeHeight = config != null ? config.eyeHeight : 1.78f;
            float eyeForward = config != null ? config.eyeForward : 0.13f;
            Vector3 eye = transform.TransformPoint(new Vector3(0f, eyeHeight, eyeForward));

            // demi-hauteur du champ : un objet de taille h a la distance d couvre
            // h / (2 d tan(fov/2)) de la hauteur de l'ecran
            float halfFov = Mathf.Tan(31f * Mathf.Deg2Rad);

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null || reported[i]) continue;

                // BOITE ORIENTEE, pas Renderer.bounds.
                //
                // Renderer.bounds est une boite alignee sur les axes du monde. Pour un
                // baton incline elle englobe toute la diagonale : elle annoncait 39 cm
                // la ou la hampe est a 48 cm, et declenchait une fausse alerte. On
                // ramene donc l'oeil dans le repere de la piece, ou la boite du
                // maillage est exacte.
                Transform t = parts[i].transform;
                MeshFilter filter = parts[i].GetComponent<MeshFilter>();
                Bounds local = filter != null && filter.sharedMesh != null
                             ? filter.sharedMesh.bounds
                             : new Bounds(Vector3.zero, Vector3.one);

                Vector3 nearest = t.TransformPoint(local.ClosestPoint(t.InverseTransformPoint(eye)));
                float distance = Vector3.Distance(nearest, eye);

                if (distance < Clearance)
                {
                    reported[i] = true;
                    Debug.LogWarning("[FIEF] Corps subjectif : " + parts[i].gameObject.name + " est a "
                                     + Mathf.RoundToInt(distance * 100f) + " cm de l'oeil, minimum "
                                     + Mathf.RoundToInt(Clearance * 100f) + " cm.");
                    continue;
                }

                Vector3 scale = t.lossyScale;
                float width = Mathf.Min(local.size.x * Mathf.Abs(scale.x),
                                        local.size.y * Mathf.Abs(scale.y));
                float share = width / (2f * distance * halfFov);
                if (share > MaxScreenShare)
                {
                    reported[i] = true;
                    Debug.LogWarning("[FIEF] Corps subjectif : " + parts[i].gameObject.name + " couvre "
                                     + Mathf.RoundToInt(share * 100f) + " % de la hauteur d'ecran a "
                                     + Mathf.RoundToInt(distance * 100f) + " cm, maximum "
                                     + Mathf.RoundToInt(MaxScreenShare * 100f) + " %.");
                }
            }
        }

        void Assemble(Color cloth, Color band, Color patch)
        {
            Color skin = new Color(0.72f, 0.58f, 0.46f);
            Color sleeve = Color.Lerp(cloth, new Color(0.30f, 0.26f, 0.22f), 0.62f);
            Color rag = Color.Lerp(patch, new Color(0.46f, 0.41f, 0.34f), 0.5f);
            Color boot = new Color(0.18f, 0.14f, 0.11f);

            // --- LES AVANT-BRAS. Ils pendent le long du corps, en avant de l'axe pour
            //     entrer dans le cadre des qu'on baisse un peu les yeux. Pas d'epaule,
            //     pas de bras : au-dessus du coude on serait deja dans la lentille.
            armL = Node(transform, new Vector3(-0.30f, 1.32f, 0.26f), "BrasGauche");
            armR = Node(transform, new Vector3(0.30f, 1.32f, 0.26f), "BrasDroit");
            BuildArm(armL, sleeve, skin);
            BuildArm(armR, sleeve, skin);
            armHomeL = armL.localPosition;
            armHomeR = armR.localPosition;

            // --- LES JAMBES. Sans elles le sol se voit a travers soi et on flotte.
            legL = Node(transform, new Vector3(-0.13f, 1.00f, 0f), "JambeGauche");
            legR = Node(transform, new Vector3(0.13f, 1.00f, 0f), "JambeDroite");
            BuildLeg(legL, sleeve, rag, boot);
            BuildLeg(legR, sleeve, rag, boot);
        }

        void BuildArm(Transform pivot, Color sleeve, Color skin)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, 0.30f, 0.12f),
                       sleeve, "AvantBras");
            Proto.Cube(pivot, new Vector3(0f, -0.36f, 0.015f), new Vector3(0.135f, 0.15f, 0.145f),
                       skin, "Main");
        }

        void BuildLeg(Transform pivot, Color cloth, Color rag, Color boot)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.23f, 0f), new Vector3(0.17f, 0.46f, 0.17f), cloth, "Cuisse");

            Transform knee = Node(pivot, new Vector3(0f, -0.46f, 0f), "Genou");
            Proto.Cube(knee, new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.42f, 0.16f),
                       Palette.Shade(cloth, 0.9f), "Tibia");

            // Des bandes de chiffon enroulees, decalees : c'est ce qu'on voit sous l'ourlet.
            for (int i = 0; i < 3; i++)
            {
                GameObject wrap = Proto.Cube(knee, new Vector3(0f, -0.14f - i * 0.11f, 0f),
                                             new Vector3(0.19f - i * 0.01f, 0.09f, 0.19f - i * 0.01f),
                                             i % 2 == 0 ? rag : Palette.Shade(rag, 0.82f), "Bande");
                wrap.transform.localRotation = Quaternion.Euler(0f, i * 19f, (i % 2 == 0 ? 4f : -4f));
            }
            Proto.Cube(knee, new Vector3(0f, -0.45f, 0.04f), new Vector3(0.19f, 0.12f, 0.29f), boot, "Pied");
        }

        static Transform Node(Transform parent, Vector3 localPosition, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        // ------------------------------------------------------------------ etat

        /// <summary>
        /// Visible uniquement quand on regarde par ses propres yeux. Hors de la vue
        /// subjective le corps subjectif ne porte meme pas d'ombre : celle du
        /// personnage est deja portee par CharacterRig, et deux corps au meme endroit
        /// donneraient une ombre double.
        /// </summary>
        public void SetVisible(bool value)
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null) parts[i].enabled = value;
            }
        }

        /// <summary>Touche F4 : efface le corps subjectif pour savoir d'ou vient une gene.</summary>
        public void Toggle()
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null) parts[i].enabled = !parts[i].enabled;
            }
        }

        public void PlaySwing()
        {
            swingTimer = 0.42f;
        }

        // ------------------------------------------------------------------ animation

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            speedSmoothed = Mathf.Lerp(speedSmoothed, Speed, 1f - Mathf.Exp(-11f * dt));
            float moving = Mathf.Clamp01(speedSmoothed / 1.2f);
            float effort = Mathf.Clamp01(speedSmoothed / Mathf.Max(1f, RunSpeed));

            // Une enjambee tous les 1,9 m : la cadence est verrouillee sur la distance
            // parcourue, jamais sur le temps. C'est ce qui empeche les pieds de patiner.
            cycle += speedSmoothed * (Mathf.PI / 1.9f) * dt;
            if (cycle > Mathf.PI * 200f) cycle -= Mathf.PI * 200f;

            float s = Mathf.Sin(cycle);
            float breathe = (1f - moving) * Mathf.Sin(Time.time * 1.7f);

            // --- jambes
            float swing = Mathf.Lerp(15f, 40f, effort) * moving;
            if (legL != null) legL.localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-s * swing, 0f, 0f);

            // --- les bras balancent a contretemps l'un de l'autre. Pendant un geste
            //     de recolte, le droit part en avant et revient.
            float reach = swingTimer > 0f ? Mathf.Sin((1f - swingTimer / 0.42f) * Mathf.PI) : 0f;
            float armSwing = Mathf.Lerp(10f, 30f, effort) * moving;

            if (armL != null)
            {
                armL.localPosition = armHomeL + armOffset + new Vector3(0f, breathe * 0.008f, -s * 0.06f * moving);
                armL.localRotation = Quaternion.Euler(-s * armSwing - 6f, 0f, 0f);
            }
            if (armR != null)
            {
                armR.localPosition = armHomeR + armOffset
                                   + new Vector3(0f, breathe * 0.008f + reach * 0.10f,
                                                 s * 0.06f * moving + reach * 0.22f);
                armR.localRotation = Quaternion.Euler(s * armSwing - 6f - reach * 38f, 0f, 0f);
            }

            if (swingTimer > 0f) swingTimer -= dt;

            // Le controle tourne aussi PENDANT le jeu : une pose au repos correcte ne
            // dit rien des gestes. Chaque piece ne se plaint qu'une fois.
            if (parts != null && parts.Length > 0 && parts[0] != null && parts[0].enabled) CheckRules();
        }
    }
}
