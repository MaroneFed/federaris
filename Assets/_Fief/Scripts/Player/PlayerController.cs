using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Deplacement en premiere personne, relatif au regard, sur un CharacterController.
    ///
    /// Concept Unity : le CharacterController est un composant de collision "capsule"
    /// fait pour les personnages. On ne lui applique pas de forces physiques : on lui
    /// dit ou aller avec Move(), il gere les murs et les pentes.
    ///
    /// Ce qui change la facon de bouger (La Couronne, 26/09) :
    ///   - les POUVOIRS : Double saut (un second saut en l'air), Ruee (R, un bond de
    ///     8 m), Coureur (+15 %), Porteur (la Couronne ne ralentit plus) ;
    ///   - la COURONNE, qui ralentit (18 %) ; la fiole de lenteur (50 %) ; le piege
    ///     (cloue sur place) -- tout ca est dans Seeker.SpeedFactor ;
    ///   - la PLUME : des sauts presque deux fois plus hauts ;
    ///   - la POUSSEE : Push() projette le joueur (un autre joueur, le Roi Creux).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public CharacterRig rig;

        /// <summary>Mis a vrai quand un panneau d'interface est ouvert : le joueur ne bouge plus.</summary>
        public bool InputLocked;

        /// <summary>
        /// Vrai pendant un mouvement joue par le code (grimper a un arbre) : ni
        /// deplacement, ni gravite ; c'est ScriptedMove qui place le corps.
        /// </summary>
        public bool Scripted { get; private set; }

        public void BeginScripted()
        {
            Scripted = true;
            controller.enabled = false;
            verticalVelocity = 0f;
        }

        /// <summary>Placer le corps sans toucher au regard (la souris reste libre).</summary>
        public void ScriptedMove(Vector3 position)
        {
            transform.position = position;
        }

        public void EndScripted(Vector3 position)
        {
            Scripted = false;
            transform.position = position;
            verticalVelocity = 0f;
            controller.enabled = true;
        }

        CharacterController controller;
        float verticalVelocity;
        bool wasGrounded = true;
        public OrbitCamera orbitCamera;

        public float CurrentSpeed { get; private set; }
        public float TargetSpeed { get; private set; }
        public bool IsSprinting { get; private set; }

        float strideAccumulator;

        // --- ce qui pousse le joueur de l'exterieur (une poussee, un coup du Roi)
        Vector3 knock;
        // --- le second saut, deja pris depuis qu'on a quitte le sol
        bool airJumpUsed;
        // --- la Ruee
        float dashTime;
        Vector3 dashDir;
        float dashReadyAt;

        public const float DashCooldown = 6f;
        const float DashDuration = 0.28f;
        const float DashSpeed = 28f;

        /// <summary>0 : la Ruee vient de servir ; 1 : elle est prete.</summary>
        public float DashReady01 { get { return Mathf.Clamp01(1f - (dashReadyAt - Time.time) / DashCooldown); } }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Projeter le joueur (on le pousse, le Roi le balaie). La partie horizontale
        /// s'amortit en une demi-seconde ; la verticale le soulege du sol.
        /// </summary>
        public void Push(Vector3 velocity)
        {
            knock += new Vector3(velocity.x, 0f, velocity.z);
            if (velocity.y > 0f) verticalVelocity = Mathf.Max(verticalVelocity, velocity.y);
            dashTime = 0f;
        }

        void Update()
        {
            GameConfig cfg = Game.Config;
            if (cfg == null || Scripted) return;
            Seeker me = Game.Me;
            float dt = Time.deltaTime;

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

            // --- LA VITESSE : pouvoirs, couronne, lenteur, piege (voir Seeker.SpeedFactor).
            float factor = me != null ? me.SpeedFactor : 1f;
            if (me != null && !me.Alive) factor = 0f;
            float speed = cfg.moveSpeed * factor;
            IsSprinting = !InputLocked && FiefInput.SprintHeld && wish.sqrMagnitude > 0.01f && factor > 0f;
            if (IsSprinting) speed *= cfg.sprintMultiplier;

            TargetSpeed = speed;
            CurrentSpeed = wish.magnitude * speed;

            // En premiere personne, le corps DOIT suivre le regard : sinon on
            // avancerait de cote pendant que la camera regarde ailleurs.
            if (orbitCamera != null && orbitCamera.ThroughEyes)
            {
                transform.rotation = Quaternion.Euler(0f, orbitCamera.yaw, 0f);
            }
            else if (wish.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, cfg.turnSpeed * dt);
            }

            // Atterrissage : une petite secousse de camera. C'est du "juice" :
            // ca ne change rien au jeu, mais le saut cesse d'etre mou.
            if (controller.isGrounded && !wasGrounded && orbitCamera != null)
            {
                orbitCamera.Shake(Mathf.Clamp01(-verticalVelocity / 24f) * 0.22f);
            }
            wasGrounded = controller.isGrounded;

            // --- LE SAUT (et le second, en l'air, avec le pouvoir)
            bool canAct = !InputLocked && factor > 0f;
            float lift = me != null && Time.time < me.FeatherUntil ? 1.8f : 1f;
            if (controller.isGrounded)
            {
                airJumpUsed = false;
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (canAct && FiefInput.JumpPressed) verticalVelocity = cfg.jumpSpeed * lift;
            }
            else if (canAct && FiefInput.JumpPressed && !airJumpUsed && me != null && me.Has(Power.DoubleSaut))
            {
                airJumpUsed = true;
                verticalVelocity = cfg.jumpSpeed * 1.05f * lift;
                Sfx.Whoosh();
                Ambiance.Burst(null, transform.position + Vector3.up * 0.2f, PowerInfo.Tint(Power.DoubleSaut));
            }
            verticalVelocity += cfg.gravity * dt;

            // --- LA RUEE (R) : un bond droit devant, toutes les 6 s.
            if (canAct && FiefInput.DashPressed && me != null && me.Has(Power.Ruee) && Time.time >= dashReadyAt)
            {
                dashReadyAt = Time.time + DashCooldown;
                dashTime = DashDuration;
                dashDir = wish.sqrMagnitude > 0.01f ? wish.normalized : forward;
                if (verticalVelocity < 1f) verticalVelocity = 1f;
                Sfx.Whoosh();
                if (orbitCamera != null) orbitCamera.Shake(0.12f);
                Ambiance.Burst(null, transform.position + Vector3.up, PowerInfo.Tint(Power.Ruee));
            }
            Vector3 dash = Vector3.zero;
            if (dashTime > 0f)
            {
                dashTime -= dt;
                dash = dashDir * DashSpeed;
            }

            // La poussee s'amortit.
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-5f * dt));

            Vector3 motion = wish * speed + dash + knock + Vector3.up * verticalVelocity;
            controller.Move(motion * dt);

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
            rig.RunSpeed = cfg.moveSpeed * cfg.sprintMultiplier;
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
                // Hors du chateau, le sol est jonche de feuilles : elles froissent.
                Vector3 p = transform.position;
                if (!Castle.Covers(p.x, p.z, 0f)) Sfx.LeafStep();
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
            Scripted = false;                   // un teleport interrompt une escalade
            knock = Vector3.zero;
            dashTime = 0f;
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
