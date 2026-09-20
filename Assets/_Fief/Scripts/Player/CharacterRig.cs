using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le personnage : un vrai bonhomme articule, assemble en primitives et anime
    /// entierement par le code. Jambes, genoux, bras, coudes, cape, capuche, ceinture,
    /// et une epee dans le dos.
    ///
    /// Pourquoi par le code plutot qu'avec une animation importee : une animation Unity
    /// demande un modele riggue, un Animator et des fichiers .anim. Ici, trois lignes de
    /// trigonometrie donnent une marche, une course et un coup de hache credibles, et
    /// ca marchera encore quand on remplacera les cubes par de vrais modeles.
    ///
    /// Concept Unity : chaque membre est un GameObject enfant. Faire tourner le parent
    /// fait tourner tout ce qui est dessous : c'est exactement un squelette.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        // --- articulations
        Transform root, hips, torso, head, hood;
        Transform legL, legR, kneeL, kneeR;
        Transform armL, armR, elbowL, elbowR;
        Transform cape, capeLower, scabbard;

        // --- etat d'animation
        float cycle;
        float speedSmoothed;
        float swingTimer;
        float baseHipsY;

        /// <summary>Vitesse horizontale en m/s, fournie par le controleur.</summary>
        public float Speed;
        public bool Grounded = true;

        public float RunSpeed = 11f;

        /// <summary>Declenche un coup de bras (recolte). Le bras droit part devant puis revient.</summary>
        public void PlaySwing()
        {
            swingTimer = 0.42f;
        }

        // ------------------------------------------------------------------ montage

        public static CharacterRig Build(Transform parent, Color tunic, Color accent)
        {
            GameObject rigGo = new GameObject("Personnage");
            rigGo.transform.SetParent(parent, false);

            CharacterRig rig = rigGo.AddComponent<CharacterRig>();
            rig.Assemble(tunic, accent);
            Proto.StripCollidersRecursive(rigGo);
            return rig;
        }

        void Assemble(Color tunic, Color accent)
        {
            Color skin = new Color(0.86f, 0.71f, 0.57f);
            Color cloth = Palette.Shade(tunic, 0.72f);
            Color leather = new Color(0.34f, 0.24f, 0.17f);
            Color darkLeather = new Color(0.22f, 0.16f, 0.12f);
            Color steel = new Color(0.72f, 0.74f, 0.78f);

            root = transform;

            hips = Node(root, new Vector3(0f, 1.02f, 0f), "Bassin");
            baseHipsY = hips.localPosition.y;
            Proto.Cube(hips, new Vector3(0f, 0.04f, 0f), new Vector3(0.40f, 0.20f, 0.26f), cloth, "Hanches");

            // --- jambes
            legL = Node(hips, new Vector3(-0.13f, -0.02f, 0f), "JambeG");
            legR = Node(hips, new Vector3(0.13f, -0.02f, 0f), "JambeD");
            kneeL = BuildLeg(legL, leather, darkLeather);
            kneeR = BuildLeg(legR, leather, darkLeather);

            // --- torse
            torso = Node(hips, new Vector3(0f, 0.12f, 0f), "Torse");
            Proto.Cube(torso, new Vector3(0f, 0.26f, 0f), new Vector3(0.50f, 0.52f, 0.30f), tunic, "Buste");
            Proto.Cube(torso, new Vector3(0f, 0.46f, 0f), new Vector3(0.54f, 0.16f, 0.33f), Palette.Shade(tunic, 1.12f), "Epaules");
            Proto.Cube(torso, new Vector3(0f, 0.02f, 0f), new Vector3(0.52f, 0.09f, 0.32f), darkLeather, "Ceinture");
            Proto.Cube(torso, new Vector3(0f, 0.02f, 0.17f), new Vector3(0.10f, 0.11f, 0.04f), Palette.Gold, "Boucle");
            Proto.Cube(torso, new Vector3(0.20f, -0.04f, 0.13f), new Vector3(0.13f, 0.16f, 0.09f), leather, "Bourse");

            // --- tete
            head = Node(torso, new Vector3(0f, 0.60f, 0f), "Tete");
            Proto.Cube(head, new Vector3(0f, 0.10f, 0f), new Vector3(0.28f, 0.30f, 0.27f), skin, "Crane");
            Proto.Cube(head, new Vector3(0f, 0.05f, 0.14f), new Vector3(0.14f, 0.05f, 0.03f), new Color(0.24f, 0.19f, 0.16f), "Regard");
            hood = Node(head, new Vector3(0f, 0.12f, -0.02f), "Capuche");
            Proto.Cube(hood, new Vector3(0f, 0.09f, 0f), new Vector3(0.33f, 0.20f, 0.33f), accent, "Coiffe");
            Proto.Cube(hood, new Vector3(0f, -0.02f, -0.14f), new Vector3(0.30f, 0.24f, 0.10f), Palette.Shade(accent, 0.8f), "Nuque");

            // --- bras
            armL = Node(torso, new Vector3(-0.32f, 0.44f, 0f), "BrasG");
            armR = Node(torso, new Vector3(0.32f, 0.44f, 0f), "BrasD");
            elbowL = BuildArm(armL, tunic, skin);
            elbowR = BuildArm(armR, tunic, skin);

            // --- cape
            cape = Node(torso, new Vector3(0f, 0.46f, -0.16f), "Cape");
            Proto.Cube(cape, new Vector3(0f, -0.20f, 0f), new Vector3(0.48f, 0.42f, 0.05f), accent, "CapeHaut");
            capeLower = Node(cape, new Vector3(0f, -0.41f, 0f), "CapeBas");
            Proto.Cube(capeLower, new Vector3(0f, -0.18f, 0f), new Vector3(0.44f, 0.38f, 0.05f), Palette.Shade(accent, 0.86f), "CapePan");

            // --- epee dans le dos (decor : le combat arrive en Phase 2)
            scabbard = Node(torso, new Vector3(-0.06f, 0.30f, -0.20f), "Epee");
            scabbard.localRotation = Quaternion.Euler(0f, 0f, 32f);
            Proto.Cube(scabbard, new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0.62f, 0.05f), darkLeather, "Fourreau");
            Proto.Cube(scabbard, new Vector3(0f, 0.36f, 0f), new Vector3(0.07f, 0.16f, 0.05f), leather, "Poignee");
            Proto.Cube(scabbard, new Vector3(0f, 0.28f, 0f), new Vector3(0.22f, 0.05f, 0.06f), steel, "Garde");
            Proto.Cube(scabbard, new Vector3(0f, 0.46f, 0f), new Vector3(0.09f, 0.08f, 0.07f), Palette.Gold, "Pommeau");
        }

        Transform BuildLeg(Transform pivot, Color leather, Color boot)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.23f, 0f), new Vector3(0.19f, 0.46f, 0.19f), leather, "Cuisse");
            Transform knee = Node(pivot, new Vector3(0f, -0.46f, 0f), "Genou");
            Proto.Cube(knee, new Vector3(0f, -0.21f, 0f), new Vector3(0.17f, 0.42f, 0.17f), Palette.Shade(leather, 0.88f), "Tibia");
            Proto.Cube(knee, new Vector3(0f, -0.45f, 0.04f), new Vector3(0.20f, 0.12f, 0.28f), boot, "Botte");
            return knee;
        }

        Transform BuildArm(Transform pivot, Color sleeve, Color skin)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.19f, 0f), new Vector3(0.15f, 0.38f, 0.15f), sleeve, "Bras");
            Transform elbow = Node(pivot, new Vector3(0f, -0.38f, 0f), "Coude");
            Proto.Cube(elbow, new Vector3(0f, -0.17f, 0f), new Vector3(0.13f, 0.34f, 0.13f), Palette.Shade(sleeve, 0.85f), "AvantBras");
            Proto.Cube(elbow, new Vector3(0f, -0.37f, 0f), new Vector3(0.15f, 0.13f, 0.16f), skin, "Main");
            return elbow;
        }

        static Transform Node(Transform parent, Vector3 localPosition, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        // ------------------------------------------------------------------ animation

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            speedSmoothed = Mathf.Lerp(speedSmoothed, Speed, 1f - Mathf.Exp(-11f * dt));
            float moving = Mathf.Clamp01(speedSmoothed / 1.2f);
            float effort = Mathf.Clamp01(speedSmoothed / Mathf.Max(1f, RunSpeed));

            // Une foulee par ~1,9 m parcouru : le pas reste cale sur le sol,
            // quelle que soit la vitesse. C'est ce qui evite l'effet "patinage".
            cycle += speedSmoothed * (Mathf.PI / 1.9f) * dt;
            if (cycle > Mathf.PI * 200f) cycle -= Mathf.PI * 200f;

            float s = Mathf.Sin(cycle);
            float c = Mathf.Cos(cycle);

            // --- jambes : balancier, genou qui plie sur la phase arriere
            float swing = Mathf.Lerp(16f, 42f, effort) * moving;
            legL.localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
            kneeL.localRotation = Quaternion.Euler(-Mathf.Max(0f, -s) * Mathf.Lerp(20f, 58f, effort) * moving, 0f, 0f);
            kneeR.localRotation = Quaternion.Euler(-Mathf.Max(0f, s) * Mathf.Lerp(20f, 58f, effort) * moving, 0f, 0f);

            // --- bras : contre-balancier, sauf pendant un coup de hache
            float armSwing = Mathf.Lerp(12f, 36f, effort) * moving;
            float idle = (1f - moving) * Mathf.Sin(Time.time * 1.7f) * 2.2f;

            armL.localRotation = Quaternion.Euler(-s * armSwing + idle, 0f, -6f - moving * 3f);
            elbowL.localRotation = Quaternion.Euler(-Mathf.Abs(s) * armSwing * 0.5f - 8f, 0f, 0f);

            if (swingTimer > 0f)
            {
                swingTimer -= dt;
                // 0 -> 1 -> 0 : le bras part en arriere, frappe, revient.
                float t = 1f - Mathf.Clamp01(swingTimer / 0.42f);
                float blow = Mathf.Sin(t * Mathf.PI);
                armR.localRotation = Quaternion.Euler(Mathf.Lerp(40f, -125f, Mathf.SmoothStep(0f, 1f, t)), 0f, 8f);
                elbowR.localRotation = Quaternion.Euler(-70f * (1f - blow) - 10f, 0f, 0f);
            }
            else
            {
                armR.localRotation = Quaternion.Euler(s * armSwing - idle, 0f, 6f + moving * 3f);
                elbowR.localRotation = Quaternion.Euler(-Mathf.Abs(s) * armSwing * 0.5f - 8f, 0f, 0f);
            }

            // --- corps : rebond a chaque appui, buste penche en avant a la course
            float bob = Mathf.Abs(c) * Mathf.Lerp(0.015f, 0.055f, effort) * moving;
            float breathe = (1f - moving) * Mathf.Sin(Time.time * 1.9f) * 0.008f;
            hips.localPosition = new Vector3(0f, baseHipsY + bob + breathe - (Grounded ? 0f : 0.06f), 0f);
            hips.localRotation = Quaternion.Euler(0f, 0f, s * 2.2f * moving);

            torso.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 13f, effort), -s * 5f * moving, 0f);
            head.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -9f, effort) + breathe * 40f, s * 4f * moving, 0f);

            // --- cape : elle traine derriere, d'autant plus qu'on va vite
            float flow = Mathf.Lerp(4f, 34f, effort);
            float flutter = Mathf.Sin(Time.time * (3f + effort * 7f)) * (1.5f + effort * 6f);
            cape.localRotation = Quaternion.Euler(flow + flutter * 0.4f, s * 3f * moving, 0f);
            capeLower.localRotation = Quaternion.Euler(flow * 0.55f + flutter, 0f, 0f);
        }
    }
}
