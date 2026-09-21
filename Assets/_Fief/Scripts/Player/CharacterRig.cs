using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ERRANT. Un personnage articule en poncho, anime entierement par le code.
    ///
    /// La silhouette d'abord : une capuche profonde, un poncho qui tombe jusqu'au sol
    /// et le balaye en marchant, un baton de marche, et juste ce qu'il faut de bottes
    /// qui apparaissent sous l'ourlet a chaque foulee.
    ///
    /// Le squelette (bassin, jambes, genoux, torse, bras, coudes, tete) existe toujours
    /// dessous : c'est lui qui cadence la marche. Le poncho, lui, est un vrai tissu
    /// simule (voir Poncho.cs) : il traine derriere le mouvement, s'ecarte dans les
    /// virages et ondule en permanence.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        Transform hips, torso, head, hood;
        Transform legL, legR, kneeL, kneeR;
        Transform armL, armR, elbowL, elbowR;
        Transform staffPivot;
        Poncho poncho;

        float cycle;
        float speedSmoothed;
        float swingTimer;
        float baseHipsY;

        public float Speed;
        public bool Grounded = true;
        public float RunSpeed = 11f;

        public void PlaySwing()
        {
            swingTimer = 0.42f;
        }

        // ------------------------------------------------------------------ montage

        public static CharacterRig Build(Transform parent, Color tunic, Color accent)
        {
            GameObject rigGo = new GameObject("Errant");
            rigGo.transform.SetParent(parent, false);

            CharacterRig rig = rigGo.AddComponent<CharacterRig>();

            Proto.BeginVisualOnly();
            rig.Assemble(tunic, accent);
            Proto.EndVisualOnly();

            return rig;
        }

        void Assemble(Color tunic, Color accent)
        {
            // Laine sale, delavee, presque grise. La couleur du joueur ne survit qu'a
            // 22 % : assez pour se reconnaitre en multijoueur, pas assez pour avoir
            // l'air d'un seigneur. C'est un gueux sur les routes, pas un herault.
            Color cloth = Color.Lerp(tunic, new Color(0.30f, 0.27f, 0.24f), 0.78f);
            Color band = Color.Lerp(tunic, new Color(0.44f, 0.40f, 0.34f), 0.72f);
            Color patch = Color.Lerp(tunic, new Color(0.38f, 0.32f, 0.25f), 0.60f);
            Color skin = new Color(0.72f, 0.58f, 0.46f);
            Color underCloth = new Color(0.22f, 0.19f, 0.17f);
            Color leather = new Color(0.18f, 0.14f, 0.11f);
            Color rag = new Color(0.46f, 0.41f, 0.34f);
            Color rope = new Color(0.52f, 0.45f, 0.32f);
            Color wood = new Color(0.30f, 0.23f, 0.16f);

            hips = Node(transform, new Vector3(0f, 1.02f, 0f), "Bassin");
            baseHipsY = hips.localPosition.y;
            Proto.Cube(hips, new Vector3(0f, 0.04f, 0f), new Vector3(0.36f, 0.20f, 0.24f), underCloth, "Hanches");

            // --- jambes : on ne verra que les bottes sous l'ourlet, mais elles donnent
            //     la cadence et trahissent la foulee. C'est ce qui rend la marche credible.
            legL = Node(hips, new Vector3(-0.13f, -0.02f, 0f), "JambeG");
            legR = Node(hips, new Vector3(0.13f, -0.02f, 0f), "JambeD");
            kneeL = BuildLeg(legL, underCloth, leather, rag);
            kneeR = BuildLeg(legR, underCloth, leather, rag);

            // --- torse, largement cache par le poncho
            torso = Node(hips, new Vector3(0f, 0.12f, 0f), "Torse");
            Proto.Cube(torso, new Vector3(0f, 0.26f, 0f), new Vector3(0.44f, 0.52f, 0.28f), underCloth, "Buste");
            Proto.Cube(torso, new Vector3(0f, 0.46f, 0f), new Vector3(0.50f, 0.14f, 0.30f), underCloth, "Epaules");
            Proto.Cube(torso, new Vector3(0f, 0.30f, 0.15f), new Vector3(0.26f, 0.20f, 0.04f),
                       Palette.Shade(rag, 0.85f), "Piece");

            // --- tete et CAPUCHE profonde : le visage reste dans l'ombre
            head = Node(torso, new Vector3(0f, 0.60f, 0f), "Tete");
            Proto.Cube(head, new Vector3(0f, 0.09f, 0f), new Vector3(0.25f, 0.28f, 0.25f), skin, "Crane");
            Proto.Cube(head, new Vector3(0f, -0.02f, 0.06f), new Vector3(0.20f, 0.12f, 0.16f),
                       Palette.Shade(skin, 0.72f), "Barbe");
            Proto.Cube(head, new Vector3(0f, 0.06f, 0.12f), new Vector3(0.17f, 0.10f, 0.05f),
                       new Color(0.09f, 0.08f, 0.08f), "Ombre");

            hood = Node(head, new Vector3(0f, 0.10f, -0.02f), "Capuche");
            Proto.Cube(hood, new Vector3(0f, 0.11f, -0.02f), new Vector3(0.34f, 0.24f, 0.36f), cloth, "Coiffe");
            GameObject peak = Proto.Cube(hood, new Vector3(0f, 0.16f, 0.13f), new Vector3(0.30f, 0.16f, 0.22f), cloth, "Visiere");
            peak.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
            GameObject nape = Proto.Cube(hood, new Vector3(0f, -0.04f, -0.20f), new Vector3(0.30f, 0.30f, 0.16f),
                                         Palette.Shade(cloth, 0.84f), "Nuque");
            nape.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);

            // --- bras : les mains sortent du poncho
            armL = Node(torso, new Vector3(-0.28f, 0.44f, 0f), "BrasG");
            armR = Node(torso, new Vector3(0.28f, 0.44f, 0f), "BrasD");
            elbowL = BuildArm(armL, underCloth, skin);
            elbowR = BuildArm(armR, underCloth, skin);

            // --- corde a la taille, nouee. Un mendiant n'a pas de ceinturon.
            for (int i = 0; i < 8; i++)
            {
                float a = (360f / 8f) * i * Mathf.Deg2Rad;
                GameObject strand = Proto.Cube(torso, new Vector3(Mathf.Sin(a) * 0.23f, 0.03f, Mathf.Cos(a) * 0.19f),
                                               new Vector3(0.10f, 0.05f, 0.05f), rope, "Corde");
                strand.transform.localRotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * a, 6f);
            }
            Proto.Cube(torso, new Vector3(0.05f, -0.03f, 0.19f), new Vector3(0.07f, 0.22f, 0.05f),
                       Palette.Shade(rope, 0.9f), "NoeudPendant");

            // --- un baluchon dans le dos : tout ce qu'il possede
            GameObject bundle = Proto.Cube(torso, new Vector3(-0.04f, 0.22f, -0.26f),
                                           new Vector3(0.36f, 0.34f, 0.24f), rag, "Baluchon");
            bundle.transform.localRotation = Quaternion.Euler(9f, 12f, -7f);
            Proto.Cube(torso, new Vector3(-0.04f, 0.40f, -0.26f), new Vector3(0.10f, 0.12f, 0.08f),
                       rope, "NoeudBaluchon");
            GameObject strap = Proto.Cube(torso, new Vector3(0.10f, 0.28f, 0f), new Vector3(0.06f, 0.52f, 0.30f),
                                          rope, "Bretelle");
            strap.transform.localRotation = Quaternion.Euler(0f, 0f, 21f);

            // --- le baton : une branche tordue ramassee en chemin, pas une canne
            staffPivot = Node(elbowR, new Vector3(0f, -0.36f, 0.04f), "Baton");
            GameObject shaft = Proto.Cube(staffPivot, new Vector3(0f, 0.30f, 0f),
                                          new Vector3(0.062f, 1.05f, 0.062f), wood, "Hampe");
            shaft.transform.localRotation = Quaternion.Euler(2f, 0f, -3f);
            GameObject upper = Proto.Cube(staffPivot, new Vector3(0.05f, 0.95f, 0.02f),
                                          new Vector3(0.055f, 0.55f, 0.055f),
                                          Palette.Shade(wood, 1.12f), "Bout");
            upper.transform.localRotation = Quaternion.Euler(-4f, 0f, 9f);
            Proto.Cube(staffPivot, new Vector3(0.09f, 1.21f, 0.03f), new Vector3(0.08f, 0.09f, 0.08f),
                       Palette.Shade(wood, 0.8f), "Noeud");
            Proto.Cube(staffPivot, new Vector3(0f, 0.62f, 0f), new Vector3(0.09f, 0.06f, 0.09f),
                       rope, "Ligature");

            // --- LE PONCHO. Attache a la racine, pas au torse : il reste vertical
            //     pendant que le buste se penche, exactement comme un vrai tissu.
            poncho = Poncho.Build(transform, cloth, band, patch);
        }

        Transform BuildLeg(Transform pivot, Color cloth, Color boot, Color rag)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.23f, 0f), new Vector3(0.17f, 0.46f, 0.17f), cloth, "Cuisse");
            Transform knee = Node(pivot, new Vector3(0f, -0.46f, 0f), "Genou");
            Proto.Cube(knee, new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.42f, 0.16f),
                       Palette.Shade(cloth, 0.9f), "Tibia");

            // Pas de bottes : des bandes de chiffon enroulees autour du mollet,
            // decalees les unes des autres. C'est ce qu'on voit sous l'ourlet.
            for (int i = 0; i < 3; i++)
            {
                GameObject wrap = Proto.Cube(knee, new Vector3(0f, -0.14f - i * 0.11f, 0f),
                                             new Vector3(0.19f - i * 0.01f, 0.09f, 0.19f - i * 0.01f),
                                             i % 2 == 0 ? rag : Palette.Shade(rag, 0.82f), "Bande");
                wrap.transform.localRotation = Quaternion.Euler(0f, i * 19f, (i % 2 == 0 ? 4f : -4f));
            }
            Proto.Cube(knee, new Vector3(0f, -0.45f, 0.04f), new Vector3(0.19f, 0.12f, 0.29f), boot, "Pied");
            return knee;
        }

        Transform BuildArm(Transform pivot, Color sleeve, Color skin)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.36f, 0.14f), sleeve, "Bras");
            Transform elbow = Node(pivot, new Vector3(0f, -0.36f, 0f), "Coude");
            Proto.Cube(elbow, new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, 0.32f, 0.12f),
                       Palette.Shade(sleeve, 0.88f), "AvantBras");
            Proto.Cube(elbow, new Vector3(0f, -0.35f, 0f), new Vector3(0.14f, 0.13f, 0.15f), skin, "Main");
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

            cycle += speedSmoothed * (Mathf.PI / 1.9f) * dt;
            if (cycle > Mathf.PI * 200f) cycle -= Mathf.PI * 200f;

            float s = Mathf.Sin(cycle);
            float c = Mathf.Cos(cycle);

            // --- jambes
            float swing = Mathf.Lerp(15f, 40f, effort) * moving;
            legL.localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
            kneeL.localRotation = Quaternion.Euler(-Mathf.Max(0f, -s) * Mathf.Lerp(20f, 56f, effort) * moving, 0f, 0f);
            kneeR.localRotation = Quaternion.Euler(-Mathf.Max(0f, s) * Mathf.Lerp(20f, 56f, effort) * moving, 0f, 0f);

            // --- bras. Le gauche balance, le droit tient le baton et le plante
            //     a chaque foulee : c'est ce petit appui qui donne l'air "errant".
            float armSwing = Mathf.Lerp(9f, 26f, effort) * moving;
            float idle = (1f - moving) * Mathf.Sin(Time.time * 1.6f) * 2f;

            armL.localRotation = Quaternion.Euler(-s * armSwing + idle, 0f, -7f - moving * 3f);
            elbowL.localRotation = Quaternion.Euler(-Mathf.Abs(s) * armSwing * 0.5f - 10f, 0f, 0f);

            if (swingTimer > 0f)
            {
                swingTimer -= dt;
                float t = 1f - Mathf.Clamp01(swingTimer / 0.42f);
                float blow = Mathf.Sin(t * Mathf.PI);
                armR.localRotation = Quaternion.Euler(Mathf.Lerp(38f, -118f, Mathf.SmoothStep(0f, 1f, t)), 0f, 8f);
                elbowR.localRotation = Quaternion.Euler(-66f * (1f - blow) - 10f, 0f, 0f);
                if (staffPivot != null) staffPivot.localRotation = Quaternion.Euler(0f, 0f, 20f);
            }
            else
            {
                float plant = Mathf.Max(0f, s);
                armR.localRotation = Quaternion.Euler(-18f - plant * 16f * moving + idle, 0f, 9f);
                elbowR.localRotation = Quaternion.Euler(-28f - plant * 10f * moving, 0f, 0f);
                if (staffPivot != null)
                    staffPivot.localRotation = Quaternion.Euler(12f + plant * 9f * moving, 0f, -9f);
            }

            // --- corps
            float bob = Mathf.Abs(c) * Mathf.Lerp(0.015f, 0.05f, effort) * moving;
            float breathe = (1f - moving) * Mathf.Sin(Time.time * 1.8f) * 0.008f;
            hips.localPosition = new Vector3(0f, baseHipsY + bob + breathe - (Grounded ? 0f : 0.06f), 0f);
            hips.localRotation = Quaternion.Euler(0f, 0f, s * 2f * moving);

            torso.localRotation = Quaternion.Euler(Mathf.Lerp(2f, 14f, effort), -s * 4f * moving, 0f);
            head.localRotation = Quaternion.Euler(Mathf.Lerp(-2f, -11f, effort) + breathe * 40f, s * 3f * moving, 0f);
            if (hood != null) hood.localRotation = Quaternion.Euler(-s * 2.5f * moving, 0f, s * 2f * moving);

            // --- le tissu n'a besoin que de la vitesse et du cap
            if (poncho != null) poncho.SetMotion(speedSmoothed, transform.eulerAngles.y);
        }
    }
}
