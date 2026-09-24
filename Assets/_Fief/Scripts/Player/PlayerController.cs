using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Deplacement 3e personne, relatif a la camera, sur un CharacterController.
    ///
    /// Concept Unity : le CharacterController est un composant de collision "capsule"
    /// fait pour les personnages. On ne lui applique pas de forces physiques : on lui
    /// dit ou aller avec Move(), il gere les murs et les pentes.
    ///
    /// Mecanique centrale du brief : la vitesse depend de la charge portee.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public CharacterRig rig;

        /// <summary>Mis a vrai quand un panneau d'interface est ouvert : le joueur ne bouge plus.</summary>
        public bool InputLocked;

        CharacterController controller;
        float verticalVelocity;
        bool wasGrounded = true;
        public OrbitCamera orbitCamera;

        public float CurrentSpeed { get; private set; }
        public float TargetSpeed { get; private set; }
        public bool IsSprinting { get; private set; }

        float strideAccumulator;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            GameConfig cfg = Game.Config;
            if (cfg == null) return;

            Vector2 input = InputLocked ? Vector2.zero : FiefInput.Move;

            // Direction voulue, exprimee dans le repere de la camera puis aplatie au sol.
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
                if (right.sqrMagnitude > 0.0001f) right.Normalize();
            }

            Vector3 wish = forward * input.y + right * input.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            // --- LE POIDS ---
            // 0 = sac vide -> vitesse max ; 1 = sac plein -> vitesse minimale.
            float load = Game.Inventory != null ? Game.Inventory.Load01 : 0f;
            float t = Mathf.Pow(Mathf.Clamp01(load), Mathf.Max(0.1f, cfg.loadCurve));
            float speed = Mathf.Lerp(cfg.moveSpeedEmpty, cfg.moveSpeedFull, t);

            // La course (Maj) n'est possible que le sac leger. Aller vite a vide,
            // rentrer lentement charge : c'est la mecanique de poids, en plus lisible.
            IsSprinting = !InputLocked
                       && FiefInput.SprintHeld
                       && load <= cfg.sprintMaxLoad
                       && wish.sqrMagnitude > 0.01f;
            if (IsSprinting) speed *= cfg.sprintMultiplier;

            TargetSpeed = speed;
            CurrentSpeed = wish.magnitude * speed;

            // Orientation du personnage.
            // En premiere personne, le corps DOIT suivre le regard : sinon on
            // avancerait de cote pendant que la camera regarde ailleurs.
            if (orbitCamera != null && orbitCamera.ThroughEyes)
            {
                transform.rotation = Quaternion.Euler(0f, orbitCamera.yaw, 0f);
            }
            else if (wish.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, cfg.turnSpeed * Time.deltaTime);
            }

            // Atterrissage : une petite secousse de camera. C'est du "juice" :
            // ca ne change rien au jeu, mais le saut cesse d'etre mou.
            if (controller.isGrounded && !wasGrounded && orbitCamera != null)
            {
                orbitCamera.Shake(Mathf.Clamp01(-verticalVelocity / 24f) * 0.22f);
            }
            wasGrounded = controller.isGrounded;

            // Gravite + saut.
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (!InputLocked && FiefInput.JumpPressed) verticalVelocity = cfg.jumpSpeed;
            }
            verticalVelocity += cfg.gravity * Time.deltaTime;

            Vector3 motion = wish * speed + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            Footsteps();
            DriveRig(cfg);
            KeepInsideMap(cfg);
        }

        /// <summary>Transmet la vitesse reelle au squelette : c'est elle qui cadence la marche.</summary>
        void DriveRig(GameConfig cfg)
        {
            if (rig == null) return;
            Vector3 flat = controller.velocity;
            flat.y = 0f;
            rig.Speed = flat.magnitude;
            rig.Grounded = controller.isGrounded;
            rig.RunSpeed = cfg.moveSpeedEmpty * cfg.sprintMultiplier;
        }

        /// <summary>Un bruit de pas tous les 2,3 m parcourus au sol.</summary>
        void Footsteps()
        {
            if (!controller.isGrounded) return;

            Vector3 flat = controller.velocity;
            flat.y = 0f;
            strideAccumulator += flat.magnitude * Time.deltaTime;

            if (strideAccumulator >= 1.9f)
            {
                strideAccumulator = 0f;
                Sfx.Step();
            }
        }

        /// <summary>
        /// Filet de securite : on ne sort pas de la carte, et si on passe a travers
        /// le sol on est remis DESSUS.
        ///
        /// LE BUG QU'IL A CAUSE. Ce filet remettait le joueur a y = +2 m des qu'il
        /// passait sous y = -20 m -- deux altitudes ABSOLUES, calibrees pour l'ancienne
        /// carte. Le relief de la sylve creuse des vallons jusqu'a -26,1 m (recalcule a
        /// l'identique dans Tools/monde.py). En y entrant, on se retrouvait "sous -20",
        /// donc renvoye a +2 m : 28 m au-dessus du sol, au niveau des cimes. On
        /// retombait, on retouchait le fond du vallon, et ca recommencait. A l'infini.
        ///
        /// La regle est maintenant RELATIVE AU SOL : on n'est secouru que si l'on est
        /// vraiment passe dessous, et on est repose juste au-dessus. Plus aucune
        /// altitude en dur, donc plus rien a recalibrer si le relief change.
        /// </summary>
        /// <summary>
        /// Deplacer le joueur d'un coup (les gardes le jettent dehors). Un
        /// CharacterController ignore qu'on change sa position a la main : on le
        /// coupe, on deplace, on le rallume.
        /// </summary>
        public void Teleport(Vector3 position, float yaw)
        {
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            verticalVelocity = 0f;
            controller.enabled = true;
            if (orbitCamera != null) orbitCamera.yaw = yaw;
        }

        void KeepInsideMap(GameConfig cfg)
        {
            float limit = cfg.mapSize * 0.5f - 3f;
            Vector3 p = transform.position;
            bool clamped = false;
            if (Mathf.Abs(p.x) > limit) { p.x = Mathf.Sign(p.x) * limit; clamped = true; }
            if (Mathf.Abs(p.z) > limit) { p.z = Mathf.Sign(p.z) * limit; clamped = true; }

            float ground = Ground.Sample(p.x, p.z);
            if (p.y < ground - 4f)
            {
                p.y = ground + 1.5f;
                clamped = true;
                verticalVelocity = 0f;
            }
            if (clamped)
            {
                controller.enabled = false;
                transform.position = p;
                controller.enabled = true;
            }
        }
    }
}
