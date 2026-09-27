using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LE PETIT CHEVALIER (30/09 -- Martin et son frere : "les graphismes des persos, des
    /// bots... tout doit etre exceptionnel, LISSE"). Il remplace le mendiant en poncho,
    /// fait de boites.
    ///
    /// Un corps en haricot (une capsule, facon Fall Guys) a la couleur du joueur, un
    /// casque d'acier poli avec sa fente et deux yeux qui luisent, un cimier a sa couleur
    /// (on le reconnait de loin), des epaulieres, une ceinture a boucle d'or, une cape,
    /// des bras et des jambes courts et ronds. Rien que des formes lisses (spheres,
    /// capsules) et des materiaux satines (MaterialFactory.GetShiny).
    ///
    /// Tout est anime par le code : il se dandine en marchant, s'ecrase a l'atterrissage
    /// et s'etire en l'air (le "squash and stretch" des dessins animes), sa cape flotte
    /// derriere lui, et quand il gagne une manche, il FETE (Celebrate) : il saute les
    /// bras en l'air et tourne sur lui-meme.
    ///
    /// Concept Unity : un "squelette" n'est qu'une hierarchie de Transform vides (des
    /// pivots). On tourne le pivot de l'epaule : tout ce qui est accroche dessous (le bras,
    /// la main) suit. C'est exactement ce que fait un Animator avec des os, mais a la main.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        Transform pivot;                  // tout le corps : le saut de joie, le tour sur soi, l'ecrasement
        Transform body, head, cape;
        Transform legL, legR, armL, armR, handR, hip;

        float cycle;
        float speedSmoothed;
        float swingTimer;
        float squash;                     // 1 a l'atterrissage, retombe a 0
        bool wasGrounded = true;
        float celebrate;                  // secondes de fete restantes
        float celebrateAge;
        const float BodyY = 0.62f;
        Renderer[] parts;
        bool firstPerson;
        bool viewApplied;

        public float Speed;
        public bool Grounded = true;
        public float RunSpeed = 11f;

        /// <summary>La tete (le casque) : on y noue l'echarpe.</summary>
        public Transform HeadBone { get { return head; } }
        /// <summary>La main droite.</summary>
        public Transform StaffBone { get { return handR; } }
        /// <summary>La hanche gauche : la petite lanterne y pend.</summary>
        public Transform HipBone { get { return hip; } }
        /// <summary>Vrai pendant qu'il fete sa victoire.</summary>
        public bool Celebrating { get { return celebrate > 0f; } }

        /// <summary>
        /// Bascule vue subjective / camera exterieure. En premiere personne ce corps
        /// disparait EN ENTIER (on ne voit rien de soi, voir CLAUDE.md) : il reste en
        /// ShadowsOnly, c'est son OMBRE au sol qui dit qu'on a un corps.
        /// </summary>
        public void SetFirstPerson(bool value)
        {
            if (viewApplied && firstPerson == value) return;
            firstPerson = value;
            viewApplied = true;
            if (parts == null) parts = GetComponentsInChildren<Renderer>(true);
            ShadowCastingMode mode = value ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                parts[i].enabled = true;
                parts[i].shadowCastingMode = mode;
            }
        }

        /// <summary>Un geste du bras droit (pousser, lancer une capacite).</summary>
        public void PlaySwing()
        {
            swingTimer = 0.36f;
        }

        /// <summary>LA JOIE DU VAINQUEUR : sauter les bras en l'air, tourner sur soi.</summary>
        public void Celebrate(float seconds)
        {
            celebrate = seconds;
            celebrateAge = 0f;
        }

        // ------------------------------------------------------------------ montage

        public static CharacterRig Build(Transform parent, Color tunic, Color accent)
        {
            GameObject rigGo = new GameObject("Chevalier");
            rigGo.transform.SetParent(parent, false);
            CharacterRig rig = rigGo.AddComponent<CharacterRig>();
            Proto.BeginVisualOnly();
            rig.Assemble(tunic, accent);
            Proto.EndVisualOnly();
            return rig;
        }

        static GameObject Paint(GameObject go, Material m)
        {
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        void Assemble(Color colour, Color accent)
        {
            // Les matieres : le corps satine a sa couleur, l'acier poli, le cuir, l'or.
            Material suit = MaterialFactory.GetShiny(colour, 0.55f, 0f);
            Material suitDark = MaterialFactory.GetShiny(Palette.Shade(colour, 0.78f), 0.5f, 0f);
            Material belly = MaterialFactory.GetShiny(Color.Lerp(colour, new Color(1f, 0.96f, 0.88f), 0.55f), 0.45f, 0f);
            Material steel = MaterialFactory.GetShiny(new Color(0.78f, 0.8f, 0.84f), 0.82f, 0.85f);
            Material leather = MaterialFactory.GetShiny(new Color(0.24f, 0.16f, 0.11f), 0.35f, 0f);
            Material gold = MaterialFactory.GetShiny(new Color(1f, 0.78f, 0.34f), 0.85f, 1f);
            Material visor = MaterialFactory.GetShiny(new Color(0.05f, 0.05f, 0.07f), 0.9f, 0.2f);
            Material capeMat = MaterialFactory.GetShiny(accent, 0.3f, 0f);
            Material eyes = MaterialFactory.GetGlow(new Color(1f, 0.93f, 0.78f), 2.4f);
            Material plume = MaterialFactory.GetShiny(Color.Lerp(colour, Color.white, 0.12f), 0.4f, 0f);

            pivot = Node(transform, Vector3.zero, "Pivot");

            // --- les jambes : courtes et rondes, des bottes de cuir
            legL = Node(pivot, new Vector3(-0.16f, 0.66f, 0f), "JambeG");
            legR = Node(pivot, new Vector3(0.16f, 0.66f, 0f), "JambeD");
            BuildLeg(legL, suitDark, leather);
            BuildLeg(legR, suitDark, leather);

            // --- le corps : un haricot satine
            body = Node(pivot, new Vector3(0f, BodyY, 0f), "Corps");
            Paint(Proto.Capsule(body, new Vector3(0f, 0.48f, 0f), new Vector3(0.74f, 0.5f, 0.64f), colour, "Haricot"), suit);
            Paint(Proto.Sphere(body, new Vector3(0f, 0.32f, 0.2f), new Vector3(0.5f, 0.5f, 0.3f), colour, "Plastron"), belly);
            Paint(Proto.Cylinder(body, new Vector3(0f, 0.12f, 0f), new Vector3(0.77f, 0.035f, 0.67f), colour, "Ceinture"), leather);
            Paint(Proto.Cube(body, new Vector3(0f, 0.12f, 0.335f), new Vector3(0.12f, 0.09f, 0.03f), colour, "Boucle"), gold);
            hip = Node(body, new Vector3(-0.36f, 0.08f, 0.06f), "Hanche");
            for (int side = -1; side <= 1; side += 2)
                Paint(Proto.Sphere(body, new Vector3(side * 0.35f, 0.62f, 0f), new Vector3(0.28f, 0.18f, 0.28f), colour, "Épaulière"), steel);

            // --- le casque (la tete) : un dome d'acier, une fente noire, deux yeux qui luisent
            head = Node(body, new Vector3(0f, 0.7f, 0f), "Tête");
            Paint(Proto.Sphere(head, new Vector3(0f, 0.08f, 0f), new Vector3(0.78f, 0.52f, 0.7f), colour, "Casque"), steel);
            GameObject slit = Paint(Proto.Capsule(head, new Vector3(0f, 0.04f, 0.33f), new Vector3(0.1f, 0.2f, 0.08f), colour, "Fente"), visor);
            slit.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject eye = Paint(Proto.Sphere(head, new Vector3(side * 0.085f, 0.045f, 0.37f), new Vector3(0.075f, 0.06f, 0.04f), colour, "Œil"), eyes);
                eye.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Paint(Proto.Cylinder(head, new Vector3(0f, -0.13f, 0f), new Vector3(0.74f, 0.03f, 0.66f), colour, "Bord du casque"), gold);
            // Le cimier, a sa couleur : trois boules qui filent vers l'arriere.
            Paint(Proto.Sphere(head, new Vector3(0f, 0.37f, 0.02f), new Vector3(0.13f, 0.17f, 0.13f), colour, "Cimier"), plume);
            Paint(Proto.Sphere(head, new Vector3(0f, 0.43f, -0.1f), new Vector3(0.12f, 0.15f, 0.16f), colour, "Cimier"), plume);
            Paint(Proto.Sphere(head, new Vector3(0f, 0.42f, -0.23f), new Vector3(0.1f, 0.11f, 0.16f), colour, "Cimier"), plume);

            // --- les bras : ronds, des gants de cuir
            armL = Node(body, new Vector3(-0.42f, 0.52f, 0f), "BrasG");
            armR = Node(body, new Vector3(0.42f, 0.52f, 0f), "BrasD");
            BuildArm(armL, suitDark, leather);
            handR = BuildArm(armR, suitDark, leather);

            // --- la cape, accrochee aux epaules, fermee par une broche d'or
            cape = Node(body, new Vector3(0f, 0.64f, -0.27f), "Cape");
            Paint(Proto.Cube(cape, new Vector3(0f, -0.33f, 0f), new Vector3(0.62f, 0.68f, 0.035f), accent, "Tissu"), capeMat);
            Paint(Proto.Sphere(body, new Vector3(0f, 0.66f, -0.3f), new Vector3(0.1f, 0.1f, 0.06f), colour, "Broche"), gold);
        }

        void BuildLeg(Transform pivotLeg, Material suit, Material boot)
        {
            Paint(Proto.Capsule(pivotLeg, new Vector3(0f, -0.28f, 0f), new Vector3(0.2f, 0.22f, 0.2f), Color.white, "Jambe"), suit);
            Paint(Proto.Sphere(pivotLeg, new Vector3(0f, -0.58f, 0.06f), new Vector3(0.22f, 0.15f, 0.32f), Color.white, "Botte"), boot);
        }

        Transform BuildArm(Transform shoulder, Material suit, Material glove)
        {
            Paint(Proto.Capsule(shoulder, new Vector3(0f, -0.2f, 0f), new Vector3(0.15f, 0.19f, 0.15f), Color.white, "Bras"), suit);
            Transform hand = Node(shoulder, new Vector3(0f, -0.42f, 0f), "Main");
            Paint(Proto.Sphere(hand, Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), Color.white, "Gant"), glove);
            return hand;
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
            if (dt <= 0f || pivot == null) return;

            if (celebrate > 0f)
            {
                Party(dt);
                return;
            }
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;

            speedSmoothed = Mathf.Lerp(speedSmoothed, Speed, 1f - Mathf.Exp(-11f * dt));
            float moving = Mathf.Clamp01(speedSmoothed / 1.2f);
            float effort = Mathf.Clamp01(speedSmoothed / Mathf.Max(1f, RunSpeed));
            cycle += speedSmoothed * (Mathf.PI / 1.7f) * dt;
            if (cycle > Mathf.PI * 200f) cycle -= Mathf.PI * 200f;
            float s = Mathf.Sin(cycle);
            float c = Mathf.Cos(cycle);

            // L'atterrissage l'ecrase ; en l'air il s'etire (squash and stretch).
            if (Grounded && !wasGrounded) squash = 1f;
            wasGrounded = Grounded;
            squash = Mathf.MoveTowards(squash, 0f, dt * 4.5f);
            float bounce = Mathf.Sin(squash * Mathf.PI) * squash;
            float air = Grounded ? 0f : 1f;
            body.localScale = new Vector3(1f + 0.14f * bounce - 0.04f * air, 1f - 0.16f * bounce + 0.07f * air, 1f + 0.14f * bounce - 0.04f * air);

            // --- jambes : en l'air, elles se replient
            float swing = Mathf.Lerp(18f, 46f, effort) * moving;
            float tuck = air * 28f;
            legL.localRotation = Quaternion.Euler(s * swing - tuck, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-s * swing - tuck * 0.6f, 0f, 0f);

            // --- bras : ils balancent ; en l'air ils s'ecartent (l'equilibre)
            float armSwing = Mathf.Lerp(14f, 44f, effort) * moving;
            float idle = (1f - moving) * Mathf.Sin(Time.time * 1.7f) * 3f;
            float spread = air * 38f;
            armL.localRotation = Quaternion.Euler(-s * armSwing + idle, 0f, -8f - spread);
            if (swingTimer > 0f)
            {
                // Le coup : le bras droit part droit devant, poing en avant.
                swingTimer -= dt;
                float t = 1f - Mathf.Clamp01(swingTimer / 0.36f);
                float blow = Mathf.Sin(t * Mathf.PI);
                armR.localRotation = Quaternion.Euler(-100f * blow, 0f, 12f);
            }
            else armR.localRotation = Quaternion.Euler(s * armSwing + idle, 0f, 8f + spread);

            // --- corps : il se dandine, rebondit, se penche quand il court
            float bob = Mathf.Abs(c) * Mathf.Lerp(0.02f, 0.07f, effort) * moving;
            float breathe = (1f - moving) * Mathf.Sin(Time.time * 2f) * 0.012f;
            body.localPosition = new Vector3(0f, BodyY + bob + breathe - 0.08f * bounce, 0f);
            body.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 12f, effort), -s * 5f * moving, s * 5f * moving);
            head.localRotation = Quaternion.Euler(-Mathf.Lerp(0f, 8f, effort), s * 3f * moving, -s * 2f * moving);

            // --- la cape : elle flotte en arriere avec la vitesse, ondule, se souleve en l'air
            float flutter = Mathf.Sin(Time.time * 7f + cycle) * (3f + 6f * effort);
            cape.localRotation = Quaternion.Euler(-(Mathf.Lerp(4f, 48f, effort) + air * 30f) + flutter, 0f, 0f);
        }

        /// <summary>
        /// LA FETE : des petits sauts les bras en V qui s'agitent, puis un tour complet
        /// sur soi-meme, et on recommence ; il s'ecrase a chaque reception.
        /// </summary>
        void Party(float dt)
        {
            celebrate -= dt;
            celebrateAge += dt;
            float t = celebrateAge;
            const float Hop = 0.55f;
            float phase = Mathf.Repeat(t, Hop) / Hop;
            float lift = Mathf.Sin(phase * Mathf.PI);
            // Le rebond : haut en l'air, ecrase au sol.
            float land = phase < 0.18f ? 1f - phase / 0.18f : 0f;
            pivot.localPosition = new Vector3(0f, lift * 0.55f, 0f);
            body.localScale = new Vector3(1f + 0.16f * land - 0.05f * lift, 1f - 0.18f * land + 0.1f * lift, 1f + 0.16f * land - 0.05f * lift);
            body.localPosition = new Vector3(0f, BodyY - 0.08f * land, 0f);
            // Un tour complet toutes les quatre sauts.
            float loop = Mathf.Repeat(t, Hop * 4f) / (Hop * 4f);
            float spin = loop > 0.75f ? Mathf.SmoothStep(0f, 360f, (loop - 0.75f) / 0.25f) : 0f;
            pivot.localRotation = Quaternion.Euler(0f, spin, 0f);
            // Les bras en V, qui s'agitent ; les jambes se replient en l'air.
            float wave = Mathf.Sin(t * 14f) * 14f;
            armL.localRotation = Quaternion.Euler(0f, 0f, -150f + wave);
            armR.localRotation = Quaternion.Euler(0f, 0f, 150f + wave);
            legL.localRotation = Quaternion.Euler(-30f * lift, 0f, -6f * lift);
            legR.localRotation = Quaternion.Euler(-30f * lift, 0f, 6f * lift);
            body.localRotation = Quaternion.Euler(-8f * lift, 0f, Mathf.Sin(t * 7f) * 6f);
            head.localRotation = Quaternion.Euler(-14f * lift, 0f, 0f);
            cape.localRotation = Quaternion.Euler(-20f - 35f * lift + Mathf.Sin(t * 9f) * 6f, 0f, 0f);
            if (celebrate <= 0f)
            {
                pivot.localPosition = Vector3.zero;
                pivot.localRotation = Quaternion.identity;
            }
        }
    }
}
