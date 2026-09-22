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

            // Le corps subjectif suit exactement la meme cadence : une enjambee tous
            // les 1,9 m. Les deux corps marchent du meme pied.
            if (Game.Body != null)
            {
                Game.Body.Speed = flat.magnitude;
                Game.Body.RunSpeed = rig.RunSpeed;
            }
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

        /// <summary>Filet de securite : on ne sort pas de la carte, meme si un mur manque.</summary>
        void KeepInsideMap(GameConfig cfg)
        {
            float limit = cfg.mapSize * 0.5f - 3f;
            Vector3 p = transform.position;
            bool clamped = false;
            if (Mathf.Abs(p.x) > limit) { p.x = Mathf.Sign(p.x) * limit; clamped = true; }
            if (Mathf.Abs(p.z) > limit) { p.z = Mathf.Sign(p.z) * limit; clamped = true; }
            if (p.y < -20f) { p = new Vector3(p.x, 2f, p.z); clamped = true; verticalVelocity = 0f; }
            if (clamped)
            {
                controller.enabled = false;
                transform.position = p;
                controller.enabled = true;
            }
        }
    }
}
