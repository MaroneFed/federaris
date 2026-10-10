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
    /// Un corps en haricot (une capsule, facon Fall Guys) a la couleur du joueur, le
    /// visage dedans (grands yeux, joues roses, une bouche qui sourit), un petit casque
    /// d'acier satine, un cimier a sa couleur (on le reconnait de loin), une cape, des
    /// bras et des jambes courts et ronds. Rien que des formes lisses (spheres,
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
        Transform mouth;

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
        float wide = 1f;
        Vector2 look;
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

        /// <summary>
        /// LANCER UN POUVOIR (10/10 -- Martin : "les animations pour les nouvelles capacites, tout
        /// est nul") : avant, le haricot donnait le meme petit coup de poing que pour pousser.
        /// Maintenant il s'ACCROUPIT (l'elan), puis jaillit : les deux bras au ciel, le corps
        /// etire vers le haut, un petit bond, les yeux ecarquilles -- et redescend. "divine" :
        /// plus haut, plus long (Mode Dieu).
        /// </summary>
        public void PlayCast(bool divine)
        {
            castLength = divine ? 0.75f : 0.5f;
            castTimer = castLength;
            castBig = divine;
            if (model != null) model.Swing();
        }

        float castTimer, castLength = 0.5f;
        float swingTwist, swingLean;
        bool castBig;

        // ================================================================== v34 : les sensations
        // (10/10 -- Martin : "je veux que tu fasses les meilleures animations du jeu video ever").
        // Ce qui fait qu'un personnage de Fall Guys ou de Smash a l'air VIVANT, c'est d'abord ce
        // qu'il fait entre les grandes actions :
        //   - il ENCAISSE les coups (ecrase du cote du choc, la tete qui part, les yeux ronds) ;
        //   - il PREND SON ELAN avant de pousser (le bras recule, puis part, le corps suit) ;
        //   - ses pieds levent de la POUSSIERE quand il court, et il en fait un nuage en atterrissant ;
        //   - il se PENCHE dans les virages, comme un coureur ;
        //   - ses YEUX suivent la Couronne (ou celui qui la porte) : on sait ou il veut aller ;
        //   - etourdi, des ETOILES lui tournent autour de la tete ;
        //   - immobile, il S'IMPATIENTE (un petit bond, un regard de cote).

        /// <summary>Un coup recu : "dir" (monde) d'ou il est pousse, "power" 0 a 1.</summary>
        public void PlayHit(Vector3 dir, float power)
        {
            hitTimer = 0.32f;
            hitPower = Mathf.Clamp01(power);
            Vector3 local = transform.InverseTransformDirection(dir);
            hitDir = new Vector3(local.x, 0f, local.z).sqrMagnitude > 0.01f ? new Vector3(local.x, 0f, local.z).normalized : Vector3.back;
            if (model != null) model.Swing();
        }

        /// <summary>Etourdi (les etoiles) -- les bots seulement : en premiere personne, on ne voit pas sa tete.</summary>
        public bool Dizzy;

        /// <summary>Ou il regarde (la Couronne, son porteur). Pas de cible : il regarde devant lui.</summary>
        public Vector3? LookAt;

        float hitTimer, hitPower;
        Vector3 hitDir;
        float airTime, lastYaw, bank, idleTime, fidget = -1f, fidgetIn = 5f, starAngle;
        bool lastStepSide;
        readonly List<Transform> pupils = new List<Transform>();
        readonly List<Vector3> pupilHome = new List<Vector3>();

        /// <summary>Le squelette d'un joueur (toi ou un bot) -- null s'il n'en a pas.</summary>
        public static CharacterRig Of(Seeker s)
        {
            if (s == null) return null;
            if (s.IsPlayer) return Game.Rig;
            Rival r = Rival.Of(s);
            return r != null ? r.Rig : null;
        }

        static bool NearCamera(Vector3 p, float metres)
        {
            Camera c = Camera.main;
            return c != null && (c.transform.position - p).sqrMagnitude < metres * metres;
        }

        /// <summary>(v44) Combien de danses on peut choisir avec B (les six du vainqueur + le dab, le moonwalk, le robot).</summary>
        public const int DanceCount = 9;
        int emote = -1;
        /// <summary>La danse choisie (B), -1 : aucune.</summary>
        public int Emote { get { return emote; } }

        /// <summary>
        /// (v44 -- Martin : "rajoute des danses, avec la musique, quand on appuie sur B") : danser
        /// UNE figure, en boucle, jusqu'a ce qu'on bouge (-1 : arreter).
        /// </summary>
        public void SetEmote(int move)
        {
            if (move == emote) return;
            bool was = emote >= 0;
            emote = move;
            if (move >= 0) { celebrate = Mathf.Max(celebrate, 0.5f); celebrateAge = 0f; }
            else if (was) celebrate = 0f;
        }

        /// <summary>LA JOIE DU VAINQUEUR : il danse sur la musique (voir Party).</summary>
        public void Celebrate(float seconds)
        {
            celebrate = seconds;
            celebrateAge = 0f;
        }

        float sulk;

        /// <summary>
        /// PERDU (03/10, le clipper fou n° 412 : "les perdants ne reagissent pas") : pendant que le
        /// gagnant danse, les autres haricots s'affaissent -- tasses, penches en avant, les bras
        /// qui pendent, un gros soupir de temps en temps. La victime en arriere-plan, c'est la
        /// moitie du clip.
        /// </summary>
        public void Disappointed(float seconds) { if (celebrate <= 0f) sulk = seconds; }

        void Sulk(float dt)
        {
            sulk -= dt;
            float sigh = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 1.1f)), 6f);
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            body.localScale = new Vector3(1.08f, 0.88f - 0.04f * sigh, 1.08f);
            body.localPosition = new Vector3(0f, BodyY - 0.08f, 0f);
            body.localRotation = Quaternion.Euler(16f + 6f * sigh, 0f, 0f);
            head.localRotation = Quaternion.Euler(14f + 8f * sigh, Mathf.Sin(Time.time * 0.7f) * 6f, 0f);
            armL.localRotation = Quaternion.Euler(4f, 0f, -3f);
            armR.localRotation = Quaternion.Euler(4f, 0f, 3f);
            legL.localRotation = Quaternion.identity;
            legR.localRotation = Quaternion.identity;
            if (sulk <= 0f) { body.localScale = Vector3.one; body.localRotation = Quaternion.identity; head.localRotation = Quaternion.identity; }
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
        /// LE CORPS. (01/10) Un haricot a sa couleur, facon Fall Guys. (03/10 -- Martin : "rends
        /// les persos hyper simples ; garde le haricot et ses yeux") : il ne reste QUE le
        /// haricot et ses deux grands yeux (blanc, pupille, reflet), et de tout petits pieds et
        /// mains ronds DE LA MEME COULEUR -- pour qu'il marche et danse. Plus de casque, de
        /// cimier, de cape, de moufles, de joues ni de bouche : de loin, sur un telephone, on
        /// ne voit qu'une forme et une couleur, et c'est ce qui se lit. L'expression passe par
        /// les YEUX (ils s'ecarquillent quand il est projete ou qu'il danse).
        /// </summary>
        void Assemble(Color colour, Color accent)
        {
            Material suit = MaterialFactory.GetShiny(colour, 0.62f, 0f);
            Material suitDark = MaterialFactory.GetShiny(Palette.Shade(colour, 0.78f), 0.55f, 0f);
            Material white = MaterialFactory.GetShiny(new Color(0.98f, 0.98f, 0.98f), 0.7f, 0f);
            Material pupil = MaterialFactory.GetShiny(new Color(0.04f, 0.04f, 0.06f), 0.9f, 0f);
            Material spark = MaterialFactory.GetGlow(Color.white, 2f);

            pivot = Node(transform, Vector3.zero, "Pivot");

            // --- les pieds : deux petites boules un peu plus sombres (on les voit trottiner)
            legL = Node(pivot, new Vector3(-0.2f, 0.66f, 0f), "JambeG");
            legR = Node(pivot, new Vector3(0.2f, 0.66f, 0f), "JambeD");
            BuildLeg(legL, suitDark);
            BuildLeg(legR, suitDark);

            // --- LE HARICOT : une seule forme, une seule couleur.
            body = Node(pivot, new Vector3(0f, BodyY, 0f), "Corps");
            // Sans les jambes, le haricot descend jusqu'aux pieds (1,7 m, de 0,18 a 1,86 m du sol) :
            // une seule forme, du sol au sommet, comme Fall Guys.
            Paint(Proto.Capsule(body, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.84f, 0.82f), colour, "Haricot"), suit);
            hip = Node(body, new Vector3(-0.42f, 0.12f, 0.06f), "Hanche");

            // --- LES YEUX, grands, dans le haut du haricot (la "tete" est un pivot : elle hoche).
            head = Node(body, new Vector3(0f, 0.92f, 0f), "Tête");
            for (int side = -1; side <= 1; side += 2)
            {
                Eye(Paint(Proto.Sphere(head, new Vector3(side * 0.16f, 0f, 0.36f), new Vector3(0.27f, 0.34f, 0.13f), colour, "Œil"), white));
                GameObject pup = Eye(Paint(Proto.Sphere(head, new Vector3(side * 0.15f, -0.01f, 0.425f), new Vector3(0.13f, 0.19f, 0.05f), colour, "Pupille"), pupil));
                GameObject glint = Eye(Paint(Proto.Sphere(head, new Vector3(side * 0.13f + 0.025f, 0.06f, 0.45f), Vector3.one * 0.05f, colour, "Reflet"), spark));
                pupils.Add(pup.transform); pupilHome.Add(pup.transform.localPosition);
                pupils.Add(glint.transform); pupilHome.Add(glint.transform.localPosition);
                glint.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // --- les mains : de petites boules de sa couleur (pour les bras en l'air de la danse)
            armL = Node(body, new Vector3(-0.47f, 0.55f, 0f), "BrasG");
            armR = Node(body, new Vector3(0.47f, 0.55f, 0f), "BrasD");
            BuildArm(armL, suit);
            handR = BuildArm(armR, suit);

            // (La cape n'existe plus : son pivot reste, vide, pour que l'animation ne change pas.)
            cape = Node(body, new Vector3(0f, 0.76f, -0.43f), "Cape");
        }

        static readonly Vector3 MouthSmile = new Vector3(0.15f, 0.055f, 0.04f);
        static readonly Vector3 MouthOpen = new Vector3(0.11f, 0.12f, 0.05f);

        /// <summary>La bouche : un sourire ; un "O" quand il est projete, qu'il tournoie ou qu'il danse.</summary>
        void Mouth(float dt)
        {
            if (mouth == null) return;
            bool open = Tumbling || celebrate > 0f || !Grounded && squash <= 0f && Speed > RunSpeed * 1.4f;
            mouth.localScale = Vector3.Lerp(mouth.localScale, open ? MouthOpen : MouthSmile, 1f - Mathf.Exp(-14f * dt));
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
            // (03/10 : plus de bouche) Projete ou en fete : les yeux s'ECARQUILLENT.
            bool excited = Tumbling || celebrate > 0f || castTimer > 0f || hitTimer > 0f || !Grounded && squash <= 0f && Speed > RunSpeed * 1.4f;
            wide = Mathf.Lerp(wide, excited ? 1.22f : 1f, 1f - Mathf.Exp(-14f * dt));
            for (int i = 0; i < eyes.Count; i++)
            {
                if (eyes[i] == null) continue;
                Vector3 s = eyeScales[i];
                eyes[i].localScale = new Vector3(s.x * wide, s.y * wide * (1f - 0.9f * shut), s.z);
            }
            // (v34) LES YEUX SUIVENT : les pupilles glissent vers ce qu'il regarde (la Couronne).
            Vector2 want = Vector2.zero;
            if (LookAt.HasValue && head != null)
            {
                Vector3 local = head.InverseTransformPoint(LookAt.Value);
                if (local.z > 0.2f) want = new Vector2(Mathf.Clamp(local.x / local.z, -1f, 1f), Mathf.Clamp(local.y / local.z, -1f, 1f));
                else want = new Vector2(Mathf.Sign(local.x), 0f);
            }
            look = Vector2.Lerp(look, want, 1f - Mathf.Exp(-10f * dt));
            for (int i = 0; i < pupils.Count; i++)
                if (pupils[i] != null) pupils[i].localPosition = pupilHome[i] + new Vector3(look.x * 0.05f, look.y * 0.045f, 0f);
        }

        void BuildLeg(Transform pivotLeg, Material foot)
        {
            Paint(Proto.Sphere(pivotLeg, new Vector3(0f, -0.56f, 0.05f), new Vector3(0.26f, 0.18f, 0.32f), Color.white, "Pied"), foot);
        }

        Transform BuildArm(Transform shoulder, Material suit)
        {
            Transform hand = Node(shoulder, new Vector3(0f, -0.3f, 0f), "Main");
            Paint(Proto.Sphere(hand, Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f), Color.white, "Main"), suit);
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
            Mouth(dt);
            if (emote >= 0) celebrate = Mathf.Max(celebrate, 0.5f);
            if (celebrate > 0f)
            {
                Party(dt);
                return;
            }
            if (sulk > 0f)
            {
                Sulk(dt);
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
            if (Grounded && !wasGrounded)
            {
                squash = Mathf.Clamp(0.5f + airTime * 0.8f, 0.5f, 1.3f);
                // L'atterrissage souleve un nuage de poussiere -- plus gros si l'on tombait de haut.
                if (airTime > 0.35f && NearCamera(transform.position, 45f))
                {
                    Vector3 f = transform.position + Vector3.up * 0.15f;
                    int n = Mathf.RoundToInt(Mathf.Lerp(8f, 26f, Mathf.Clamp01(airTime / 1.5f)));
                    Fx.Burst(f, new Color(0.78f, 0.72f, 0.62f), n, 3.5f + airTime * 2f, 0.45f, 0.6f, -0.05f, Vector3.up, 88f);
                    if (airTime > 0.9f) Fx.Ring(f, new Color(0.85f, 0.8f, 0.7f), 0.3f, 2.2f, 0.35f, 0.18f, Vector3.up);
                }
            }
            airTime = Grounded ? 0f : airTime + dt;
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
                // (v34) LE COUP AVEC ELAN : 25 % du temps le bras RECULE (l'anticipation), puis il
                // part droit devant, le corps tourne et se jette dans le coup, et revient.
                swingTimer -= dt;
                float t = 1f - Mathf.Clamp01(swingTimer / 0.36f);
                float wind = t < 0.25f ? Mathf.Sin(t / 0.25f * Mathf.PI * 0.5f) : 0f;
                float blow = t < 0.25f ? 0f : Mathf.Sin(Mathf.Clamp01((t - 0.25f) / 0.75f) * Mathf.PI);
                armR.localRotation = Quaternion.Euler(45f * wind - 115f * blow, 0f, 12f + 20f * wind);
                armL.localRotation = Quaternion.Euler(30f * blow, 0f, -20f);
                swingTwist = -18f * wind + 22f * blow;
                swingLean = 14f * blow;
            }
            else
            {
                swingTwist = 0f;
                swingLean = 0f;
                armR.localRotation = Quaternion.Euler(s * armSwing + idle, 0f, 8f + spread);
            }

            // --- corps : il se dandine, rebondit, se penche quand il court
            float bob = Mathf.Abs(c) * Mathf.Lerp(0.02f, 0.07f, effort) * moving;
            float breathe = (1f - moving) * Mathf.Sin(Time.time * 2f) * 0.012f;
            body.localPosition = new Vector3(0f, BodyY + bob + breathe - 0.08f * bounce, 0f);
            body.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 12f, effort) + swingLean, -s * 5f * moving + swingTwist, s * 5f * moving);
            head.localRotation = Quaternion.Euler(-Mathf.Lerp(0f, 8f, effort), s * 3f * moving, -s * 2f * moving);

            // --- la cape : elle flotte en arriere avec la vitesse, ondule, se souleve en l'air
            // --- (v34) LES VIRAGES : il se penche vers l'interieur, comme un coureur.
            float yaw = transform.eulerAngles.y;
            float turn = Mathf.DeltaAngle(lastYaw, yaw) / Mathf.Max(dt, 0.001f);
            lastYaw = yaw;
            bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * 0.045f, -16f, 16f) * moving * (Grounded ? 1f : 0.5f), 1f - Mathf.Exp(-8f * dt));
            body.localRotation = body.localRotation * Quaternion.Euler(0f, 0f, bank);

            // --- (v34) LA POUSSIERE DES PIEDS : un petit nuage a chaque pas, quand il court.
            if (Grounded && effort > 0.55f)
            {
                bool side = s > 0f;
                if (side != lastStepSide && NearCamera(transform.position, 30f))
                {
                    Transform foot = side ? legL : legR;
                    Fx.Burst(foot.position + Vector3.down * 0.5f, new Color(0.8f, 0.74f, 0.64f), 3, 1.4f, 0.3f, 0.45f, -0.05f, Vector3.up - transform.forward * 0.6f, 50f);
                }
                lastStepSide = side;
            }

            float flutter = Mathf.Sin(Time.time * 7f + cycle) * (3f + 6f * effort);
            cape.localRotation = Quaternion.Euler(-(Mathf.Lerp(4f, 48f, effort) + air * 30f) + flutter, 0f, 0f);

            // --- (v34) LE COUP RECU : ecrase du cote du choc, penche a l'oppose, la tete qui part.
            if (hitTimer > 0f)
            {
                hitTimer -= dt;
                float k = Mathf.Sin(Mathf.Clamp01(1f - hitTimer / 0.32f) * Mathf.PI) * (0.5f + 0.5f * hitPower);
                body.localRotation = body.localRotation * Quaternion.Euler(hitDir.z * 28f * k, 0f, -hitDir.x * 28f * k);
                body.localScale = new Vector3(body.localScale.x * (1f + 0.22f * k), body.localScale.y * (1f - 0.18f * k), body.localScale.z * (1f + 0.22f * k));
                head.localRotation = head.localRotation * Quaternion.Euler(hitDir.z * 22f * k, 0f, -hitDir.x * 22f * k);
                armL.localRotation = Quaternion.Euler(-40f * k, 0f, -60f * k - 10f);
                armR.localRotation = Quaternion.Euler(-40f * k, 0f, 60f * k + 10f);
            }

            // --- (v34) ETOURDI : la tete fait des ronds, des etoiles tournent au-dessus.
            if (Dizzy && !Tumbling)
            {
                starAngle += dt * 360f;
                head.localRotation = head.localRotation * Quaternion.Euler(Mathf.Sin(Time.time * 9f) * 10f, 0f, Mathf.Cos(Time.time * 9f) * 10f);
                if (NearCamera(transform.position, 40f) && Mathf.Repeat(starAngle, 45f) < dt * 360f)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float a = (starAngle + k * 120f) * Mathf.Deg2Rad;
                        Vector3 p = head.position + Vector3.up * 0.55f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.5f;
                        Fx.Sparks(p, new Color(1f, 0.9f, 0.3f), 2, 0.4f);
                    }
                }
            }

            // --- (v34) IMMOBILE, IL S'IMPATIENTE : de temps en temps un petit bond et un regard de cote.
            if (moving < 0.1f && Grounded && hitTimer <= 0f && castTimer <= 0f)
            {
                idleTime += dt;
                fidgetIn -= dt;
                if (fidgetIn <= 0f && idleTime > 2f) { fidget = 0f; fidgetIn = Random.Range(4f, 8f); }
            }
            else { idleTime = 0f; fidget = -1f; }
            if (fidget >= 0f)
            {
                fidget += dt;
                float f = fidget / 0.9f;
                if (f >= 1f) fidget = -1f;
                else
                {
                    pivot.localPosition = new Vector3(0f, 0.12f * Mathf.Max(0f, Mathf.Sin(f * Mathf.PI * 2f)), 0f);
                    head.localRotation = head.localRotation * Quaternion.Euler(0f, 35f * Mathf.Sin(f * Mathf.PI), 0f);
                }
            }

            // --- le POUVOIR : accroupi (l'elan, 20 % du temps), puis il jaillit, bras au ciel.
            if (castTimer > 0f)
            {
                castTimer -= dt;
                float t = 1f - Mathf.Clamp01(castTimer / castLength);
                float crouch = t < 0.2f ? Mathf.Sin(t / 0.2f * Mathf.PI * 0.5f) : Mathf.Max(0f, 1f - (t - 0.2f) * 6f);
                float rise = t < 0.2f ? 0f : Mathf.Sin(Mathf.Clamp01((t - 0.2f) / 0.8f) * Mathf.PI);
                float big = castBig ? 1.35f : 1f;
                float up = Mathf.Lerp(-20f, -170f, rise);
                armL.localRotation = Quaternion.Euler(up * 0.25f, 0f, Mathf.Lerp(-30f, -160f, rise));
                armR.localRotation = Quaternion.Euler(up * 0.25f, 0f, Mathf.Lerp(30f, 160f, rise));
                body.localScale = new Vector3(1f + 0.16f * crouch - 0.08f * rise, 1f - 0.2f * crouch + 0.16f * rise * big, 1f + 0.16f * crouch - 0.08f * rise);
                body.localPosition = new Vector3(0f, BodyY - 0.1f * crouch, 0f);
                body.localRotation = Quaternion.Euler(-10f * rise, 0f, 0f);
                head.localRotation = Quaternion.Euler(-22f * rise, 0f, 0f);
                pivot.localPosition = new Vector3(0f, 0.28f * rise * big, 0f);
                legL.localRotation = Quaternion.Euler(-25f * crouch, 0f, -10f * rise);
                legR.localRotation = Quaternion.Euler(-25f * crouch, 0f, 10f * rise);
                cape.localRotation = Quaternion.Euler(-60f * rise, 0f, 0f);
            }

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
            int move = emote >= 0 ? emote % DanceCount : Mathf.FloorToInt(beat / 8f) % 6;
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
                case 6:     // (v44) LE DAB : la tete dans le coude, l'autre bras tendu vers le ciel, un cote puis l'autre
                {
                    bool left = Mathf.FloorToInt(beat / 2f) % 2 == 0;
                    float snap = Mathf.Clamp01(ph * 5f);
                    float sgn = left ? 1f : -1f;
                    Vector3 up = new Vector3(-20f, 0f, 125f * snap);
                    Vector3 bent = new Vector3(-85f * snap, 0f, 55f * snap);
                    if (left) { aR = up; aL = new Vector3(bent.x, 0f, -bent.z); }
                    else { aL = new Vector3(up.x, 0f, -up.z); aR = bent; }
                    headNod = 28f * snap;
                    bodyRot = new Vector3(10f * snap, 0f, -14f * sgn * snap);
                    pos.x = 0.1f * sgn * snap;
                    lL = new Vector3(0f, 0f, -10f * snap);
                    lR = new Vector3(0f, 0f, 10f * snap);
                    break;
                }
                case 7:     // (v44) LE MOONWALK : il glisse en arriere, une jambe puis l'autre, penche
                {
                    float slide = ph;
                    pos.z = 0.25f - slide * 0.5f;
                    bodyRot.x = -8f;
                    float lift = Mathf.Sin(ph * Mathf.PI);
                    if (side > 0f) { lL = new Vector3(-25f * lift, 0f, 0f); lR = new Vector3(18f, 0f, 0f); }
                    else { lR = new Vector3(-25f * lift, 0f, 0f); lL = new Vector3(18f, 0f, 0f); }
                    aL = new Vector3(20f * side, 0f, -25f);
                    aR = new Vector3(-20f * side, 0f, 25f);
                    headNod = -6f;
                    break;
                }
                case 8:     // (v44) LE ROBOT : une pose seche par temps, sans transition
                {
                    int pose = step % 4;
                    aL = pose == 0 ? new Vector3(-90f, 0f, -10f) : pose == 1 ? new Vector3(0f, 0f, -90f) : pose == 2 ? new Vector3(-90f, 0f, -90f) : new Vector3(0f, 0f, -12f);
                    aR = pose == 0 ? new Vector3(0f, 0f, 12f) : pose == 1 ? new Vector3(-90f, 0f, 10f) : pose == 2 ? new Vector3(-90f, 0f, 90f) : new Vector3(0f, 0f, 90f);
                    rot = Quaternion.Euler(0f, pose == 1 ? 25f : pose == 3 ? -25f : 0f, 0f);
                    headNod = pose == 2 ? 15f : 0f;
                    hit *= 0.3f;
                    break;
                }
                case 5:
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
