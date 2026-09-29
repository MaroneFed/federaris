using System.Collections.Generic;
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
    /// derriere lui, et quand il gagne une manche, il DANSE sur la musique (Celebrate).
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

        // (02/10) LES YEUX CLIGNENT : toutes les 2,5 a 6 s, un battement de 0,14 s. Des yeux
        // qui ne clignent jamais, c'est ce qui rend un personnage "mort" (designer chiant).
        readonly List<Transform> eyes = new List<Transform>();
        readonly List<Vector3> eyeScales = new List<Vector3>();
        float blinkIn = 2f;
        float blinkAge = -1f;
        Renderer[] parts;
        ModelCharacter model;
        bool firstPerson;
        bool viewApplied;

        public float Speed;
        public bool Grounded = true;
        /// <summary>Ejecte par un obstacle : il tournoie dans les airs, bras et jambes ecartes.</summary>
        public bool Tumbling;
        float tumbleAngle;
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
            if (model != null) model.Swing();
        }

        /// <summary>LA JOIE DU VAINQUEUR : il danse sur la musique (voir Party).</summary>
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
            // LE PERSONNAGE DE MARTIN (s'il y en a un dans Resources/Modeles) : on garde le
            // squelette (les pivots qu'on anime : sauts, saltos, danse), on retire le haricot,
            // et le modele prend sa place (voir ModelCharacter).
            if (ModelCharacter.Available)
            {
                Renderer[] drawn = rigGo.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < drawn.Length; i++) Object.Destroy(drawn[i].gameObject);
                rig.model = ModelCharacter.Attach(rig.pivot, tunic, 1.85f);
            }
            return rig;
        }

        static GameObject Paint(GameObject go, Material m)
        {
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>
        /// LE CORPS (refait le 01/10 -- Martin : "je n'ai jamais vu des personnages aussi
        /// horribles"). Fini le bonhomme de neige (un casque pose sur un tonneau) : UN SEUL
        /// HARICOT a sa couleur, comme Fall Guys, et le visage DANS le haricot -- deux grands
        /// yeux blancs a pupilles noires (avec leur reflet), des joues roses, un petit casque
        /// d'acier qui descend sur le front, son cimier a sa couleur, une cape. Des moufles
        /// blanches, des bottes. Tout rond, tout satine.
        /// </summary>
        void Assemble(Color colour, Color accent)
        {
            Material suit = MaterialFactory.GetShiny(colour, 0.6f, 0f);
            Material suitDark = MaterialFactory.GetShiny(Palette.Shade(colour, 0.8f), 0.55f, 0f);
            Material belly = MaterialFactory.GetShiny(Color.Lerp(colour, new Color(1f, 0.97f, 0.9f), 0.6f), 0.5f, 0f);
            Material steel = MaterialFactory.GetShiny(new Color(0.8f, 0.82f, 0.86f), 0.85f, 0.85f);
            Material leather = MaterialFactory.GetShiny(new Color(0.26f, 0.17f, 0.11f), 0.4f, 0f);
            Material gold = MaterialFactory.GetShiny(new Color(1f, 0.78f, 0.34f), 0.85f, 1f);
            Material white = MaterialFactory.GetShiny(new Color(0.98f, 0.98f, 0.98f), 0.7f, 0f);
            Material pupil = MaterialFactory.GetShiny(new Color(0.04f, 0.04f, 0.06f), 0.9f, 0f);
            Material spark = MaterialFactory.GetGlow(Color.white, 2f);
            Material blush = MaterialFactory.GetShiny(new Color(1f, 0.55f, 0.6f), 0.3f, 0f);
            Material capeMat = MaterialFactory.GetShiny(accent, 0.35f, 0f);
            Material plume = MaterialFactory.GetShiny(Color.Lerp(colour, Color.white, 0.15f), 0.45f, 0f);

            pivot = Node(transform, Vector3.zero, "Pivot");

            // --- les jambes : courtes et rondes, des bottes de cuir
            legL = Node(pivot, new Vector3(-0.18f, 0.66f, 0f), "JambeG");
            legR = Node(pivot, new Vector3(0.18f, 0.66f, 0f), "JambeD");
            BuildLeg(legL, suitDark, leather);
            BuildLeg(legR, suitDark, leather);

            // --- LE HARICOT : le corps et la tete d'un seul tenant
            body = Node(pivot, new Vector3(0f, BodyY, 0f), "Corps");
            Paint(Proto.Capsule(body, new Vector3(0f, 0.6f, 0f), new Vector3(0.88f, 0.62f, 0.8f), colour, "Haricot"), suit);
            Paint(Proto.Sphere(body, new Vector3(0f, 0.36f, 0.24f), new Vector3(0.6f, 0.62f, 0.36f), colour, "Ventre"), belly);
            Paint(Proto.Cylinder(body, new Vector3(0f, 0.16f, 0f), new Vector3(0.9f, 0.04f, 0.82f), colour, "Ceinture"), leather);
            Paint(Proto.Sphere(body, new Vector3(0f, 0.16f, 0.41f), new Vector3(0.14f, 0.11f, 0.05f), colour, "Boucle"), gold);
            hip = Node(body, new Vector3(-0.42f, 0.12f, 0.06f), "Hanche");

            // --- le visage, dans le haut du haricot (la "tete" est un pivot : elle hoche)
            head = Node(body, new Vector3(0f, 0.92f, 0f), "Tête");
            for (int side = -1; side <= 1; side += 2)
            {
                Eye(Paint(Proto.Sphere(head, new Vector3(side * 0.14f, 0.02f, 0.36f), new Vector3(0.21f, 0.26f, 0.12f), colour, "Œil"), white));
                Eye(Paint(Proto.Sphere(head, new Vector3(side * 0.13f, 0.0f, 0.415f), new Vector3(0.11f, 0.15f, 0.05f), colour, "Pupille"), pupil));
                GameObject glint = Eye(Paint(Proto.Sphere(head, new Vector3(side * 0.11f + 0.02f, 0.05f, 0.44f), Vector3.one * 0.04f, colour, "Reflet"), spark));
                glint.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Paint(Proto.Sphere(head, new Vector3(side * 0.28f, -0.14f, 0.3f), new Vector3(0.14f, 0.07f, 0.06f), colour, "Joue"), blush);
            }
            // Le petit casque d'acier qui descend sur le front, un rivet d'or, le cimier.
            Paint(Proto.Sphere(head, new Vector3(0f, 0.26f, -0.02f), new Vector3(0.94f, 0.52f, 0.9f), colour, "Casque"), steel);
            Paint(Proto.Cylinder(head, new Vector3(0f, 0.17f, -0.02f), new Vector3(0.95f, 0.035f, 0.91f), colour, "Bord du casque"), gold);
            Paint(Proto.Sphere(head, new Vector3(0f, 0.3f, 0.44f), Vector3.one * 0.08f, colour, "Rivet"), gold);
            Paint(Proto.Sphere(head, new Vector3(0f, 0.55f, 0.02f), new Vector3(0.14f, 0.19f, 0.14f), colour, "Cimier"), plume);
            Paint(Proto.Sphere(head, new Vector3(0f, 0.6f, -0.12f), new Vector3(0.13f, 0.16f, 0.17f), colour, "Cimier"), plume);
            Paint(Proto.Sphere(head, new Vector3(0f, 0.57f, -0.26f), new Vector3(0.11f, 0.12f, 0.17f), colour, "Cimier"), plume);

            // --- les bras : ronds, des moufles blanches
            armL = Node(body, new Vector3(-0.46f, 0.62f, 0f), "BrasG");
            armR = Node(body, new Vector3(0.46f, 0.62f, 0f), "BrasD");
            BuildArm(armL, suit, white);
            handR = BuildArm(armR, suit, white);
            for (int side = -1; side <= 1; side += 2)
                Paint(Proto.Sphere(body, new Vector3(side * 0.4f, 0.7f, 0f), new Vector3(0.3f, 0.2f, 0.3f), colour, "Épaulière"), steel);

            // --- la cape, accrochee aux epaules, fermee par une broche d'or
            cape = Node(body, new Vector3(0f, 0.76f, -0.43f), "Cape");
            Paint(Proto.Sphere(cape, new Vector3(0f, -0.36f, 0f), new Vector3(0.7f, 0.78f, 0.05f), accent, "Tissu"), capeMat);
            Paint(Proto.Sphere(body, new Vector3(0f, 0.8f, -0.42f), new Vector3(0.1f, 0.1f, 0.06f), colour, "Broche"), gold);
        }

        GameObject Eye(GameObject part)
        {
            eyes.Add(part.transform);
            eyeScales.Add(part.transform.localScale);
            return part;
        }

        /// <summary>Le clignement : les yeux s'aplatissent un instant (0,14 s), puis se rouvrent.</summary>
        void Blink(float dt)
        {
            if (eyes.Count == 0) return;
            blinkIn -= dt;
            if (blinkIn <= 0f) { blinkIn = Random.Range(2.5f, 6f); blinkAge = 0f; }
            float shut = 0f;
            if (blinkAge >= 0f)
            {
                blinkAge += dt;
                shut = blinkAge < 0.14f ? Mathf.Sin(blinkAge / 0.14f * Mathf.PI) : 0f;
                if (blinkAge >= 0.14f) blinkAge = -1f;
            }
            for (int i = 0; i < eyes.Count; i++)
            {
                if (eyes[i] == null) continue;
                Vector3 s = eyeScales[i];
                eyes[i].localScale = new Vector3(s.x, s.y * (1f - 0.9f * shut), s.z);
            }
        }

        void BuildLeg(Transform pivotLeg, Material suit, Material boot)
        {
            Paint(Proto.Capsule(pivotLeg, new Vector3(0f, -0.28f, 0f), new Vector3(0.22f, 0.22f, 0.22f), Color.white, "Jambe"), suit);
            Paint(Proto.Sphere(pivotLeg, new Vector3(0f, -0.58f, 0.06f), new Vector3(0.24f, 0.16f, 0.34f), Color.white, "Botte"), boot);
        }

        Transform BuildArm(Transform shoulder, Material suit, Material glove)
        {
            Paint(Proto.Capsule(shoulder, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.19f, 0.16f), Color.white, "Bras"), suit);
            Transform hand = Node(shoulder, new Vector3(0f, -0.42f, 0f), "Main");
            Paint(Proto.Sphere(hand, Vector3.zero, new Vector3(0.21f, 0.21f, 0.21f), Color.white, "Moufle"), glove);
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
            if (model != null)
            {
                model.Speed = Speed;
                model.Grounded = Grounded;
                model.Dancing = celebrate > 0f;
            }

            Blink(dt);
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

            // --- ejecte : il fait des saltos en arriere, bras et jambes en etoile
            if (Tumbling && !Grounded)
            {
                tumbleAngle += 560f * dt;
                Vector3 centre = new Vector3(0f, 0.95f, 0f);
                Quaternion roll = Quaternion.Euler(-tumbleAngle, 0f, 0f);
                pivot.localRotation = roll;
                pivot.localPosition = centre - roll * centre;
                float flail = Mathf.Sin(Time.time * 18f) * 20f;
                armL.localRotation = Quaternion.Euler(flail, 0f, -120f);
                armR.localRotation = Quaternion.Euler(-flail, 0f, 120f);
                legL.localRotation = Quaternion.Euler(-flail, 0f, -35f);
                legR.localRotation = Quaternion.Euler(flail, 0f, 35f);
                cape.localRotation = Quaternion.Euler(-70f + flail, 0f, 0f);
            }
            else tumbleAngle = 0f;
        }

        /// <summary>
        /// LA DANSE DU VAINQUEUR (01/10 -- Martin : "notre perso qui danse avec la musique").
        /// Un pas par TEMPS de la musique (MusicDirector.DanceBeat ; sans elle, 120 temps par
        /// minute), et six figures de huit temps qui s'enchainent :
        ///   1. le balance    : un pas a gauche, un pas a droite, on tape dans les mains en l'air ;
        ///   2. le disco      : le doigt pointe le ciel, puis le sol en croisant, les hanches suivent ;
        ///   3. le fil        : les bras balancent devant-derriere, les hanches a l'oppose ;
        ///   4. le french cancan : les mains aux hanches, une jambe lancee a chaque temps ;
        ///   5. les poings    : les deux poings au ciel a chaque temps, la tete qui bat ;
        ///   6. saut et tour  : des petits bonds, et un tour complet sur soi-meme.
        /// A chaque temps, il plie les genoux (un petit ecrasement) : c'est ce qui fait qu'on
        /// "sent" qu'il est sur la musique.
        /// </summary>
        void Party(float dt)
        {
            celebrate -= dt;
            celebrateAge += Time.unscaledDeltaTime;
            float music = MusicDirector.DanceBeat;
            float beat = music >= 0f ? music : celebrateAge * 2f;
            if (model != null && model.HasDance)
            {
                // Le modele a sa propre danse : on le fait juste rebondir sur le temps.
                float bob = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 3f);
                pivot.localPosition = new Vector3(0f, 0.05f * bob, 0f);
                pivot.localRotation = Quaternion.identity;
                return;
            }
            int move = Mathf.FloorToInt(beat / 8f) % 6;
            float inMove = Mathf.Repeat(beat, 8f);
            int step = Mathf.FloorToInt(inMove);
            float ph = Mathf.Repeat(beat, 1f);
            // L'accent de chaque temps : fort sur le temps, qui retombe.
            float hit = Mathf.Pow(1f - ph, 3f);
            float side = step % 2 == 0 ? 1f : -1f;
            float sway = Mathf.Sin(beat * Mathf.PI);              // gauche-droite, un aller par temps

            Vector3 pos = new Vector3(0f, 0f, 0f);
            Quaternion rot = Quaternion.identity;
            Vector3 aL = new Vector3(0f, 0f, -12f), aR = new Vector3(0f, 0f, 12f);
            Vector3 lL = Vector3.zero, lR = Vector3.zero;
            Vector3 bodyRot = Vector3.zero;
            float headNod = -10f * hit;

            switch (move)
            {
                case 0:     // le balance, et on tape dans les mains en l'air
                {
                    pos.x = sway * 0.16f;
                    bodyRot.z = -sway * 9f;
                    float clap = step % 2 == 1 ? hit : 0f;
                    aL = new Vector3(0f, 0f, -150f + 22f * clap);
                    aR = new Vector3(0f, 0f, 150f - 22f * clap);
                    lL = new Vector3(0f, 0f, sway > 0f ? -8f * sway : 0f);
                    lR = new Vector3(0f, 0f, sway < 0f ? -8f * sway : 0f);
                    break;
                }
                case 1:     // le disco
                {
                    bool up = step % 2 == 0;
                    aR = up ? new Vector3(-25f, 0f, 145f) : new Vector3(-30f, 0f, -35f);
                    aL = new Vector3(0f, 0f, -38f);
                    bodyRot.z = (up ? -8f : 8f) * (0.6f + 0.4f * hit);
                    pos.x = (up ? 0.08f : -0.08f);
                    lR = new Vector3(up ? -18f : 0f, 0f, up ? 12f : 0f);
                    break;
                }
                case 2:     // le fil (deux allers par temps)
                {
                    float f = Mathf.Sin(beat * Mathf.PI * 2f);
                    float g = Mathf.Cos(beat * Mathf.PI * 2f);
                    aL = new Vector3(g * 35f, 0f, -20f + f * 32f);
                    aR = new Vector3(-g * 35f, 0f, 20f + f * 32f);
                    pos.x = -f * 0.12f;
                    bodyRot.z = f * 9f;
                    lL = new Vector3(0f, 0f, f * 6f);
                    lR = new Vector3(0f, 0f, f * 6f);
                    break;
                }
                case 3:     // le french cancan
                {
                    float kick = Mathf.Sin(ph * Mathf.PI);
                    aL = new Vector3(10f, 0f, -42f);
                    aR = new Vector3(10f, 0f, 42f);
                    if (side > 0f) lR = new Vector3(-95f * kick, 0f, 0f); else lL = new Vector3(-95f * kick, 0f, 0f);
                    bodyRot.x = -8f * kick;
                    pos.y = 0.06f * kick;
                    break;
                }
                case 4:     // les poings au ciel
                {
                    float punch = Mathf.Sin(Mathf.Clamp01(ph * 1.6f) * Mathf.PI);
                    aL = new Vector3(-15f, 0f, -100f - 60f * punch);
                    aR = new Vector3(-15f, 0f, 100f + 60f * punch);
                    pos.y = 0.18f * punch;
                    headNod = -18f * hit;
                    lL = new Vector3(-20f * punch, 0f, 0f);
                    lR = new Vector3(-20f * punch, 0f, 0f);
                    break;
                }
                default:    // saut et tour
                {
                    float lift = Mathf.Sin(ph * Mathf.PI);
                    pos.y = lift * 0.55f;
                    float spin = inMove >= 6f ? Mathf.SmoothStep(0f, 360f, (inMove - 6f) / 2f) : 0f;
                    rot = Quaternion.Euler(0f, spin, 0f);
                    float wave = Mathf.Sin(beat * Mathf.PI * 4f) * 14f;
                    aL = new Vector3(0f, 0f, -150f + wave);
                    aR = new Vector3(0f, 0f, 150f + wave);
                    lL = new Vector3(-30f * lift, 0f, -6f * lift);
                    lR = new Vector3(-30f * lift, 0f, 6f * lift);
                    break;
                }
            }

            // Plier les genoux sur chaque temps (l'ecrasement), et la tete qui bat.
            float squat = hit * 0.08f;
            pivot.localPosition = pos - new Vector3(0f, squat, 0f);
            pivot.localRotation = rot;
            body.localScale = new Vector3(1f + 0.1f * hit, 1f - 0.1f * hit, 1f + 0.1f * hit);
            body.localPosition = new Vector3(0f, BodyY, 0f);
            body.localRotation = Quaternion.Euler(bodyRot);
            head.localRotation = Quaternion.Euler(headNod, sway * 10f, -sway * 6f);
            armL.localRotation = Quaternion.Euler(aL);
            armR.localRotation = Quaternion.Euler(aR);
            legL.localRotation = Quaternion.Euler(lL);
            legR.localRotation = Quaternion.Euler(lR);
            cape.localRotation = Quaternion.Euler(-18f - 20f * hit + Mathf.Sin(beat * Mathf.PI) * 8f, 0f, 0f);
            if (celebrate <= 0f)
            {
                pivot.localPosition = Vector3.zero;
                pivot.localRotation = Quaternion.identity;
            }
        }
    }
}
