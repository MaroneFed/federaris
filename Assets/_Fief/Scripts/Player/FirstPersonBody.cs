using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CORPS VU DE L'INTERIEUR.
    ///
    /// Ce n'est PAS le meme objet que CharacterRig. Tous les jeux a la premiere
    /// personne ont deux modeles : celui que les autres voient, et un corps
    /// subjectif construit pour la camera. On a longtemps essaye de masquer le
    /// premier morceau par morceau pour en faire le second -- torse, epaules,
    /// bretelle, haut des bras -- et il en restait toujours un qu'on avait oublie.
    ///
    /// Ici on prend le probleme a l'envers : on part de l'oeil et on ne pose que
    /// ce qui se voit bien depuis lui. Deux regles, verifiees par le calcul :
    ///
    ///     DEGAGEMENT : rien a moins de 40 cm de l'oeil.
    ///     ENCOMBREMENT : aucune piece ne couvre plus de 60 % de la hauteur d'ecran.
    ///
    /// Un cube de 50 cm vu a 11 cm couvre quatre fois la hauteur de l'ecran -- c'est
    /// exactement ce que faisaient les epaules. Un baton de 6 cm vu a 43 cm n'en
    /// couvre que 12 %, et il peut donc monter aussi haut qu'il veut. Ces deux
    /// regles ne sont pas des precautions : ce sont les seules qui empechent le
    /// corps de faire mur, et le jeu les verifie tout seul au demarrage.
    ///
    /// Ce qu'on voit de soi, mesure par projection dans la camera (62 deg, 16:9).
    /// Part de l'ecran occupee par le corps, et par quoi :
    ///
    ///     droit devant     12 %   baton 9, mains 3
    ///     25 deg plus bas  15 %   baton 10, mains 5
    ///     40 deg plus bas  20 %   baton 9, mains 6, poncho 5
    ///     60 deg plus bas  28 %   poncho 14, mains 11, baton 3
    ///     a fond vers bas  39 %   poncho 28, jambes 6, mains 5
    ///
    /// Le centre de l'ecran n'est JAMAIS couvert, a aucun angle : le corps vit sur
    /// les bords. La piece la plus proche de l'oeil est la hampe, a 48 cm.
    ///
    /// Le baton est la piece importante : c'est la seule qui reste dans le cadre en
    /// marchant. C'est elle qui fait qu'on est DANS le mendiant, et pas derriere une
    /// camera qui flotte.
    ///
    /// </summary>
    public class FirstPersonBody : MonoBehaviour
    {
        /// <summary>Rien ne doit approcher l'oeil de plus pres que ca.</summary>
        public const float Clearance = 0.40f;

        /// <summary>
        /// Part de la hauteur d'ecran qu'une piece a le droit d'occuper.
        ///
        /// C'est le VRAI critere, et pas la hauteur : le baton monte a 1,95 m, bien
        /// au-dessus de l'oeil, et ne gene personne parce qu'il est fin. Les epaules
        /// montaient moins haut et bouchaient tout parce qu'elles font 50 cm de large.
        /// Ce qui compte est donc largeur / distance, pas l'altitude.
        /// </summary>
        public const float MaxScreenShare = 0.60f;

        // Le baton, defini par ses deux bouts plutot que par des angles d'Euler :
        // c'est la seule facon de garantir qu'il passe la ou on veut a l'ecran.
        static readonly Vector3 StaffFoot = new Vector3(0.32f, 0.05f, 0.84f);
        static readonly Vector3 StaffTop = new Vector3(0.38f, 1.95f, 0.46f);

        [Header("Reglage a chaud (Hierarchy > JOUEUR > CorpsSubjectif)")]
        [Tooltip("Decale le baton et la main droite. Z positif = plus loin devant.")]
        public Vector3 staffOffset = Vector3.zero;

        [Tooltip("Decale la main gauche.")]
        public Vector3 handOffset = Vector3.zero;

        [Tooltip("Monte ou descend le poncho.")]
        public float clothRise;

        Transform staff;
        Transform leftArm;
        Transform legL, legR;
        Transform ponchoRoot;
        Poncho poncho;
        Renderer[] parts;
        bool[] reported;

        float cycle;
        float speedSmoothed;
        float swingTimer;

        Vector3 staffHome, handHome, clothHome;

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

                // Le poncho est un cone CREUX : sa boite englobante entoure le porteur
                // et ne dit rien de ce qu'on voit. Son profil est verifie a part, par
                // construction (voir Poncho.InsideY / InsideR).
                if (poncho != null && parts[i].transform.IsChildOf(poncho.transform)) continue;

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
            Color sleeve = new Color(0.30f, 0.26f, 0.22f);
            Color wood = new Color(0.30f, 0.23f, 0.16f);
            Color rag = new Color(0.46f, 0.41f, 0.34f);
            Color boot = new Color(0.18f, 0.14f, 0.11f);

            // --- LE BATON. Pose par ses deux bouts : le pied devant a droite, la tete
            //     en arriere au-dessus de l'epaule. La hampe traverse ainsi le bas
            //     droit du cadre meme quand on regarde droit devant.
            //
            //     LE PIVOT EST LA POIGNEE, pas la racine du corps. Sinon, en levant le
            //     baton pour recolter, on le ferait tourner autour de ses propres pieds
            //     et il balaierait tout l'ecran. On tourne autour de la main, comme
            //     quand on tient vraiment un baton.
            Vector3 grip = Vector3.Lerp(StaffFoot, StaffTop, 0.72f);
            staff = Node(transform, grip, "Baton");
            BuildStaff(staff, grip, wood, rag);
            staffHome = staff.localPosition;

            // --- LA MAIN DROITE, refermee sur la hampe. Elle est fille du baton :
            //     la main suit l'objet, jamais l'inverse.
            Proto.Cube(staff, new Vector3(0.015f, 0f, 0.015f),
                       new Vector3(0.135f, 0.155f, 0.145f), skin, "MainDroite");
            Proto.Cube(staff, new Vector3(0.05f, 0.145f, 0.05f),
                       new Vector3(0.115f, 0.28f, 0.115f), Palette.Shade(sleeve, 0.92f), "AvantBrasDroit");

            // --- LA MAIN GAUCHE, qui pend et balance au rythme de la foulee.
            leftArm = Node(transform, new Vector3(-0.30f, 1.32f, 0.26f), "BrasGauche");
            Proto.Cube(leftArm, new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, 0.30f, 0.12f),
                       Palette.Shade(sleeve, 0.92f), "AvantBrasGauche");
            Proto.Cube(leftArm, new Vector3(0f, -0.36f, 0.015f), new Vector3(0.135f, 0.15f, 0.145f),
                       skin, "MainGauche");
            handHome = leftArm.localPosition;

            // --- LES JAMBES. On ne les voit qu'en regardant ses pieds, mais sans elles
            //     le sol se voit a travers soi, et on flotte.
            legL = Node(transform, new Vector3(-0.13f, 1.00f, 0f), "JambeGauche");
            legR = Node(transform, new Vector3(0.13f, 1.00f, 0f), "JambeDroite");
            BuildLeg(legL, sleeve, rag, boot);
            BuildLeg(legR, sleeve, rag, boot);

            // --- LE PONCHO, profil subjectif : il commence sous la poitrine.
            ponchoRoot = Node(transform, Vector3.zero, "Vetement");
            poncho = Poncho.Build(ponchoRoot, cloth, band, patch, true);
            clothHome = ponchoRoot.localPosition;
        }

        /// <summary>
        /// La branche, posee dans le repere de la poignee : chaque morceau est place
        /// par sa fraction le long de l'axe, moins la position de la main.
        /// </summary>
        void BuildStaff(Transform parent, Vector3 grip, Color wood, Color rag)
        {
            Vector3 axis = StaffTop - StaffFoot;
            float length = axis.magnitude;
            Quaternion lean = Quaternion.FromToRotation(Vector3.up, axis.normalized);

            GameObject shaft = Proto.Cube(parent, (StaffFoot + StaffTop) * 0.5f - grip,
                                          new Vector3(0.062f, length, 0.062f), wood, "Hampe");
            shaft.transform.localRotation = lean;

            // Une branche ramassee en chemin, pas une canne taillee : un noeud, une
            // ligature de chiffon, et le bout use plus clair.
            Along(parent, grip, lean, 0.58f, new Vector3(0.095f, 0.10f, 0.095f),
                  Palette.Shade(wood, 0.78f), "Noeud");
            Along(parent, grip, lean, 0.40f, new Vector3(0.085f, 0.07f, 0.085f), rag, "Ligature");
            Along(parent, grip, lean, 0.035f, new Vector3(0.07f, 0.13f, 0.07f),
                  Palette.Shade(wood, 1.15f), "Bout");
        }

        void Along(Transform parent, Vector3 grip, Quaternion lean, float t,
                   Vector3 size, Color color, string name)
        {
            GameObject piece = Proto.Cube(parent, Vector3.Lerp(StaffFoot, StaffTop, t) - grip,
                                          size, color, name);
            piece.transform.localRotation = lean;
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

            // --- le baton. En marche il se plante a chaque foulee : ce petit appui
            //     fait l'errant. A la recolte il se leve et retombe.
            float plant = Mathf.Max(0f, s);
            if (staff != null)
            {
                if (swingTimer > 0f)
                {
                    // LE COUP PART VERS L'AVANT, jamais vers l'epaule.
                    //
                    // Arme en arriere, la hampe passait a 28 cm de l'oeil au milieu du
                    // geste -- sous la regle, et invisible pour un controle qui ne
                    // regarde que la pose au repos. En poussant la poignee en AVANT
                    // pendant qu'elle monte, le baton s'ecarte du visage au lieu de le
                    // balayer : 45 cm au plus pres, sur toute la duree du geste.
                    float t = 1f - Mathf.Clamp01(swingTimer / 0.42f);
                    float raise = Mathf.Sin(t * Mathf.PI);            // monte puis retombe
                    float strike = Mathf.SmoothStep(0f, 1f, t);       // l'allonge du coup
                    staff.localPosition = staffHome + staffOffset
                                        + new Vector3(0.05f * raise, 0.12f * raise,
                                                      0.08f * raise + 0.22f * strike);
                    staff.localRotation = Quaternion.Euler(-46f * raise, 0f, 0f);
                }
                else
                {
                    staff.localPosition = staffHome + staffOffset
                                        + new Vector3(0f, -plant * 0.05f * moving + breathe * 0.006f,
                                                      plant * 0.09f * moving);
                    staff.localRotation = Quaternion.Euler(plant * 5f * moving, 0f, -plant * 3f * moving);
                }
            }

            // --- la main gauche balance a contretemps
            if (leftArm != null)
            {
                leftArm.localPosition = handHome + handOffset
                                      + new Vector3(0f, breathe * 0.008f, -s * 0.06f * moving);
                leftArm.localRotation = Quaternion.Euler(-s * Mathf.Lerp(10f, 30f, effort) * moving - 6f, 0f, 0f);
            }

            // --- le vetement n'a besoin que de la vitesse et du cap
            if (ponchoRoot != null) ponchoRoot.localPosition = clothHome + new Vector3(0f, clothRise, 0f);
            if (poncho != null) poncho.SetMotion(speedSmoothed, transform.eulerAngles.y);

            if (swingTimer > 0f) swingTimer -= dt;

            // Le controle tourne aussi PENDANT le jeu : une pose au repos correcte ne
            // dit rien des gestes. Chaque piece ne se plaint qu'une fois.
            if (parts != null && parts.Length > 0 && parts[0] != null && parts[0].enabled) CheckRules();
        }
    }
}
