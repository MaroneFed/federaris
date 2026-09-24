using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN CORPS QUI MARCHE, pour les PNJ (gardes, mage, Ermite, Veilleur).
    ///
    /// Avant, un PNJ etait une robe en tranches de cubes qui glissait sur le sol
    /// (Martin : "les PNJ sont horribles"). Maintenant c'est un squelette, comme
    /// celui du joueur : bassin, jambes, genoux, torse, bras, coudes, tete. Il
    /// s'anime TOUT SEUL a partir de sa vitesse -- il mesure de combien il a bouge
    /// depuis l'image precedente. Le script du PNJ n'a qu'a le deplacer.
    ///
    /// Trois details font la difference :
    ///   - la ROBE, s'il en porte une, est faite de pans qui suivent les jambes :
    ///     elle s'ouvre a chaque pas au lieu d'etre un bloc ;
    ///   - ce qu'il tient DROIT (hallebarde, baton, lanterne) reste droit quel que
    ///     soit l'angle du bras -- une lanterne pend, elle ne tourne pas avec le coude ;
    ///   - sa TETE suit ce qu'il regarde (toi, souvent), dans la limite du cou.
    ///
    /// Concept Unity : une HIERARCHIE de Transform. Tourner le genou fait tourner
    /// tout ce qui est dessous (le tibia, le pied) -- c'est exactement comme un vrai
    /// squelette, et c'est ce qu'une animation fait, en fait, image par image.
    /// </summary>
    public class Walker : MonoBehaviour
    {
        /// <summary>Les couleurs et la coupe d'un corps.</summary>
        public struct Look
        {
            public Color skin;
            public Color shirt;
            public Color legs;
            public Color boots;
            /// <summary>Une robe longue au lieu de jambes visibles.</summary>
            public bool robe;
            public Color robeColor;
            public Color robeDark;
            /// <summary>1 = normal, 1,15 = carrure de soldat.</summary>
            public float bulk;
            /// <summary>Hauteur totale en metres (1,8 par defaut).</summary>
            public float height;
        }

        // Les os, pour y accrocher casque, tabard, hallebarde...
        public Transform Hips;
        public Transform Torso;
        public Transform Neck;
        public Transform Head;
        public Transform ArmL;
        public Transform ArmR;
        public Transform ElbowL;
        public Transform ElbowR;
        public Transform HandL;
        public Transform HandR;
        public Transform LegL;
        public Transform LegR;
        public Transform KneeL;
        public Transform KneeR;

        /// <summary>Vitesse de course : au-dela, il court (grandes foulees, buste penche).</summary>
        public float RunSpeed = 6f;
        /// <summary>Le dos courbe (degres) : l'Ermite est voute.</summary>
        public float Stoop;
        /// <summary>Bras droit tenant une arme d'hast, levee devant lui.</summary>
        public bool HoldPole;
        /// <summary>Bras gauche tenant une lanterne, a hauteur de hanche.</summary>
        public bool HoldLantern;
        /// <summary>Ce qu'il regarde (tete seulement). null : droit devant.</summary>
        public Transform Gaze;
        /// <summary>Force la vitesse (sinon elle est mesuree). Negatif : mesuree.</summary>
        public float ForcedSpeed = -1f;

        readonly List<Transform> upright = new List<Transform>();
        readonly List<Transform> skirt = new List<Transform>();
        readonly List<float> skirtAngle = new List<float>();

        float cycle;
        float speed;
        float swingTimer;
        float baseHipsY;
        float seed;
        Vector3 lastPosition;
        bool hasLast;
        float scale = 1f;

        // ================================================================== montage

        public static Walker Build(Transform parent, string name, Look look)
        {
            if (look.bulk <= 0f) look.bulk = 1f;
            if (look.height <= 0f) look.height = 1.8f;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Walker w = go.AddComponent<Walker>();
            w.scale = look.height / 1.8f;
            go.transform.localScale = Vector3.one * w.scale;
            w.seed = (name.Length * 1.37f) % 6.28f;

            Proto.BeginVisualOnly();
            w.Assemble(look);
            Proto.EndVisualOnly();
            return w;
        }

        void Assemble(Look l)
        {
            float b = l.bulk;
            Color shirtDark = Palette.Shade(l.shirt, 0.82f);

            Hips = Node(transform, new Vector3(0f, 0.98f, 0f), "Bassin");
            baseHipsY = Hips.localPosition.y;
            Proto.Cube(Hips, new Vector3(0f, 0.03f, 0f), new Vector3(0.36f * b, 0.2f, 0.24f * b), l.legs, "Hanches");

            // --- jambes
            LegL = Node(Hips, new Vector3(-0.12f * b, -0.03f, 0f), "JambeG");
            LegR = Node(Hips, new Vector3(0.12f * b, -0.03f, 0f), "JambeD");
            KneeL = Leg(LegL, l, b);
            KneeR = Leg(LegR, l, b);

            // --- torse : un buste qui s'evase vers les epaules, pas une boite droite
            Torso = Node(Hips, new Vector3(0f, 0.12f, 0f), "Torse");
            Proto.Cube(Torso, new Vector3(0f, 0.14f, 0f), new Vector3(0.38f * b, 0.28f, 0.24f * b), shirtDark, "Ventre");
            Proto.Cube(Torso, new Vector3(0f, 0.38f, 0f), new Vector3(0.46f * b, 0.24f, 0.27f * b), l.shirt, "Poitrine");
            Proto.Cube(Torso, new Vector3(0f, 0.5f, 0f), new Vector3(0.54f * b, 0.1f, 0.28f * b), l.shirt, "Épaules");
            Proto.Cube(Torso, new Vector3(0f, 0.02f, 0f), new Vector3(0.4f * b, 0.07f, 0.27f * b),
                       new Color(0.16f, 0.12f, 0.09f), "Ceinture");
            Proto.Cube(Torso, new Vector3(0.08f, 0.02f, 0.14f * b), new Vector3(0.08f, 0.06f, 0.02f),
                       new Color(0.55f, 0.47f, 0.3f), "Boucle");

            // --- cou et tete : une sphere, pas un cube -- c'est ce qui humanise le plus
            Neck = Node(Torso, new Vector3(0f, 0.56f, 0f), "Cou");
            Proto.Cube(Neck, new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.1f, 0.12f), Palette.Shade(l.skin, 0.85f), "Nuque");
            Head = Node(Neck, new Vector3(0f, 0.1f, 0f), "Tête");
            Proto.Sphere(Head, new Vector3(0f, 0.12f, 0f), new Vector3(0.23f, 0.27f, 0.25f), l.skin, "Crâne");
            Proto.Cube(Head, new Vector3(0f, 0.1f, 0.12f), new Vector3(0.05f, 0.07f, 0.05f), Palette.Shade(l.skin, 0.9f), "Nez");
            for (int side = -1; side <= 1; side += 2)
                Proto.Cube(Head, new Vector3(side * 0.055f, 0.155f, 0.115f), new Vector3(0.045f, 0.022f, 0.02f),
                           new Color(0.08f, 0.07f, 0.06f), "Oeil");

            // --- bras
            ArmL = Node(Torso, new Vector3(-0.29f * b, 0.49f, 0f), "BrasG");
            ArmR = Node(Torso, new Vector3(0.29f * b, 0.49f, 0f), "BrasD");
            ElbowL = Arm(ArmL, l, b, out HandL);
            ElbowR = Arm(ArmR, l, b, out HandR);

            if (l.robe) Robe(l);
        }

        Transform Leg(Transform pivot, Look l, float b)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.23f, 0f), new Vector3(0.16f * b, 0.46f, 0.17f * b), l.legs, "Cuisse");
            Transform knee = Node(pivot, new Vector3(0f, -0.46f, 0f), "Genou");
            Proto.Cube(knee, new Vector3(0f, -0.13f, 0f), new Vector3(0.14f * b, 0.26f, 0.15f * b), Palette.Shade(l.legs, 0.9f), "Tibia");
            Proto.Cube(knee, new Vector3(0f, -0.33f, 0f), new Vector3(0.16f * b, 0.2f, 0.17f * b), l.boots, "Botte");
            Proto.Cube(knee, new Vector3(0f, -0.43f, 0.05f), new Vector3(0.16f * b, 0.1f, 0.28f), l.boots, "Pied");
            Proto.Cube(knee, new Vector3(0f, -0.23f, 0f), new Vector3(0.18f * b, 0.05f, 0.19f * b), Palette.Shade(l.boots, 1.25f), "Revers");
            return knee;
        }

        Transform Arm(Transform pivot, Look l, float b, out Transform hand)
        {
            Proto.Cube(pivot, new Vector3(0f, -0.16f, 0f), new Vector3(0.14f * b, 0.34f, 0.15f * b), l.shirt, "Bras");
            Transform elbow = Node(pivot, new Vector3(0f, -0.33f, 0f), "Coude");
            Proto.Cube(elbow, new Vector3(0f, -0.14f, 0f), new Vector3(0.12f * b, 0.28f, 0.13f * b), Palette.Shade(l.shirt, 0.85f), "AvantBras");
            Proto.Cube(elbow, new Vector3(0f, -0.26f, 0f), new Vector3(0.14f * b, 0.06f, 0.15f * b), Palette.Shade(l.shirt, 0.7f), "Poignet");
            hand = Node(elbow, new Vector3(0f, -0.34f, 0.01f), "Main");
            Proto.Cube(hand, new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.12f, 0.12f), l.skin, "Paume");
            return elbow;
        }

        /// <summary>
        /// Douze pans autour du bassin, legerement evases. Chacun suit la jambe de
        /// son cote : la robe s'ouvre a chaque pas.
        /// </summary>
        void Robe(Look l)
        {
            const int count = 12;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Transform pivot = Node(Hips, new Vector3(Mathf.Sin(a) * 0.2f, 0.06f, Mathf.Cos(a) * 0.15f), "Pan");
                pivot.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
                Proto.Cube(pivot, new Vector3(0f, -0.46f, 0f), new Vector3(0.13f, 0.96f, 0.035f),
                           i % 2 == 0 ? l.robeColor : l.robeDark, "Tissu");
                skirt.Add(pivot);
                skirtAngle.Add(a);
            }
            // Le haut de la robe recouvre le torse.
            Proto.Cube(Torso, new Vector3(0f, 0.25f, 0f), new Vector3(0.42f, 0.5f, 0.28f), l.robeColor, "Corsage");
            Proto.Cube(Torso, new Vector3(0f, 0.48f, 0f), new Vector3(0.56f, 0.12f, 0.3f), l.robeDark, "Col");
            // Manches larges.
            Proto.Cube(ArmL, new Vector3(0f, -0.2f, 0f), new Vector3(0.18f, 0.4f, 0.19f), l.robeColor, "Manche");
            Proto.Cube(ArmR, new Vector3(0f, -0.2f, 0f), new Vector3(0.18f, 0.4f, 0.19f), l.robeColor, "Manche");
            Proto.Cube(ElbowL, new Vector3(0f, -0.18f, 0f), new Vector3(0.2f, 0.26f, 0.21f), l.robeDark, "Manchette");
            Proto.Cube(ElbowR, new Vector3(0f, -0.18f, 0f), new Vector3(0.2f, 0.26f, 0.21f), l.robeDark, "Manchette");
        }

        /// <summary>
        /// Un objet tenu qui reste droit quoi que fasse le bras (hallebarde, baton,
        /// lanterne). On l'accroche a la main, on le tient debout a chaque image.
        /// </summary>
        public Transform Holder(Transform hand, string name)
        {
            Transform t = Node(hand, Vector3.zero, name);
            upright.Add(t);
            return t;
        }

        public static Transform Node(Transform parent, Vector3 localPosition, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        /// <summary>Un coup (de hallebarde, de baton) : le bras droit leve, puis frappe.</summary>
        public void PlaySwing()
        {
            swingTimer = 0.5f;
            if (Skin != null) Skin.Attack();
        }

        /// <summary>Le vrai modele qui l'habille, s'il y en a un (voir ModelSkin).</summary>
        public ModelSkin Skin;

        // ================================================================== animation

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Sa vitesse : mesuree, sauf si le PNJ la donne.
            Vector3 p = transform.position;
            float measured = 0f;
            if (hasLast)
            {
                Vector3 d = p - lastPosition;
                d.y = 0f;
                measured = Mathf.Min(d.magnitude / dt, 12f);
            }
            lastPosition = p;
            hasLast = true;
            float target = ForcedSpeed >= 0f ? ForcedSpeed : measured;
            speed = Mathf.Lerp(speed, target, 1f - Mathf.Exp(-8f * dt));

            float moving = Mathf.Clamp01(speed / 1f);
            float effort = Mathf.Clamp01((speed - 1.5f) / Mathf.Max(0.5f, RunSpeed - 1.5f));
            cycle += speed / scale * (Mathf.PI / 1.7f) * dt;
            if (cycle > Mathf.PI * 200f) cycle -= Mathf.PI * 200f;
            float s = Mathf.Sin(cycle);
            float c = Mathf.Cos(cycle);
            float t = Time.time + seed;

            // --- jambes
            float swing = Mathf.Lerp(20f, 44f, effort) * moving;
            float legL = s * swing;
            float legR = -s * swing;
            LegL.localRotation = Quaternion.Euler(legL, 0f, 0f);
            LegR.localRotation = Quaternion.Euler(legR, 0f, 0f);
            float bend = Mathf.Lerp(24f, 70f, effort) * moving;
            KneeL.localRotation = Quaternion.Euler(-Mathf.Max(0f, -s) * bend, 0f, 0f);
            KneeR.localRotation = Quaternion.Euler(-Mathf.Max(0f, s) * bend, 0f, 0f);

            // --- bassin, buste, respiration
            float breathe = Mathf.Sin(t * 1.5f);
            float bob = Mathf.Abs(c) * Mathf.Lerp(0.02f, 0.06f, effort) * moving;
            Hips.localPosition = new Vector3(0f, baseHipsY + bob, 0f);
            Hips.localRotation = Quaternion.Euler(0f, s * 5f * moving, s * 2f * moving);
            Torso.localRotation = Quaternion.Euler(Stoop + Mathf.Lerp(2f, 16f, effort) * moving + breathe * 0.8f * (1f - moving),
                                                   -s * 7f * moving, 0f);

            // --- bras gauche
            float idle = breathe * 2.5f * (1f - moving);
            float armSwing = Mathf.Lerp(18f, 50f, effort) * moving;
            if (HoldLantern)
            {
                ArmL.localRotation = Quaternion.Euler(-24f + s * 6f * moving, 0f, -10f);
                ElbowL.localRotation = Quaternion.Euler(-38f, 0f, 0f);
            }
            else
            {
                ArmL.localRotation = Quaternion.Euler(-s * armSwing + idle, 0f, -6f - moving * 3f);
                ElbowL.localRotation = Quaternion.Euler(-12f - Mathf.Max(0f, -s) * armSwing * 0.6f, 0f, 0f);
            }

            // --- bras droit
            if (swingTimer > 0f)
            {
                swingTimer -= dt;
                float k = 1f - Mathf.Clamp01(swingTimer / 0.5f);
                float raise = k < 0.4f ? Mathf.SmoothStep(0f, 1f, k / 0.4f) : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.4f) / 0.6f);
                ArmR.localRotation = Quaternion.Euler(Mathf.Lerp(-30f, -150f, raise), 0f, 12f);
                ElbowR.localRotation = Quaternion.Euler(Mathf.Lerp(-20f, -50f, raise), 0f, 0f);
            }
            else if (HoldPole)
            {
                ArmR.localRotation = Quaternion.Euler(-20f + s * 5f * moving + idle * 0.5f, 0f, 8f);
                ElbowR.localRotation = Quaternion.Euler(-62f, 0f, 0f);
            }
            else
            {
                ArmR.localRotation = Quaternion.Euler(s * armSwing + idle, 0f, 6f + moving * 3f);
                ElbowR.localRotation = Quaternion.Euler(-12f - Mathf.Max(0f, s) * armSwing * 0.6f, 0f, 0f);
            }

            // --- la tete : elle suit ce qu'il regarde, dans la limite du cou
            float yaw = s * 3f * moving;
            float pitch = -Mathf.Lerp(0f, 8f, effort) - Stoop * 0.6f;
            if (Gaze != null)
            {
                Vector3 local = transform.InverseTransformPoint(Gaze.position + Vector3.up * 1.5f);
                float want = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(want) < 110f) yaw = Mathf.Clamp(want, -65f, 65f);
                float flat = new Vector2(local.x, local.z).magnitude;
                pitch = Mathf.Clamp(-Mathf.Atan2(local.y - 1.6f, Mathf.Max(0.5f, flat)) * Mathf.Rad2Deg, -25f, 25f) - Stoop * 0.6f;
            }
            Quaternion headWant = Quaternion.Euler(pitch, yaw, 0f);
            Head.localRotation = Quaternion.Slerp(Head.localRotation, headWant, 1f - Mathf.Exp(-6f * dt));

            // --- la robe : chaque pan suit la jambe de son cote
            for (int i = 0; i < skirt.Count; i++)
            {
                float a = skirtAngle[i];
                float leg = Mathf.Sin(a) < 0f ? legL : legR;
                float flare = 7f + moving * 4f;
                float kick = leg * 0.55f * Mathf.Cos(a);
                float wave = Mathf.Sin(t * 2.1f + a * 3f) * 1.5f;
                float x = Mathf.Min(-2f, -flare + kick + wave);
                skirt[i].localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f) * Quaternion.Euler(x, 0f, 0f);
            }

            // --- ce qu'il tient reste droit
            Quaternion straight = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            for (int i = 0; i < upright.Count; i++)
                if (upright[i] != null) upright[i].rotation = straight;
        }
    }
}
