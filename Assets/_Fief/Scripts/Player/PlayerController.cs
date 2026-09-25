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
    /// Ce qui change la facon de bouger (voir Match/Abilities.cs) :
    ///   - les CAPACITES passent par l'interface IMover : Dash (ruee), PullTo (grappin),
    ///     Blink (clignement, echange, rappel), Push (poussees, bond, onde...) ;
    ///   - le DOUBLE SAUT, le PLANEUR (Espace maintenu en l'air), le REBOND ;
    ///   - l'ETOURDISSEMENT (on ne bouge plus), le GIVRE (moitie moins vite) ;
    ///   - la COURONNE : si son porteur tombe (sans planer), elle reste la ou il a
    ///     quitte le sol (Crown.Slip). On ne redescend pas la tour d'un saut.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IMover
    {
        public Transform cameraTransform;
        public CharacterRig rig;
        public OrbitCamera orbitCamera;

        /// <summary>Mis a vrai quand un menu est ouvert : le joueur ne bouge plus.</summary>
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

        public void ScriptedMove(Vector3 position) { transform.position = position; }

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

        public float CurrentSpeed { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool Gliding { get; private set; }

        float strideAccumulator;
        Vector3 knock;              // ce qui pousse le joueur de l'exterieur
        bool airJumpUsed;
        float dashTime;
        Vector3 dashVelocity;
        float pullTime;
        Vector3 pullPoint;
        float pullSpeed;
        Vector3 lastGround;         // le dernier point ou l'on touchait le sol (la Couronne y reste)
        float airTop;               // le plus haut atteint depuis qu'on a quitte le sol
        bool windPlayed;

        // --- le rappel : ou l'on etait, un point tous les dixiemes de seconde, sur cinq secondes
        readonly Vector3[] trail = new Vector3[50];
        int trailAt;
        float trailTimer;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            for (int i = 0; i < trail.Length; i++) trail[i] = transform.position;
            lastGround = transform.position;
        }

        // ================================================================== IMover

        public void Push(Vector3 velocity)
        {
            knock += new Vector3(velocity.x, 0f, velocity.z);
            if (velocity.y > 0f) verticalVelocity = Mathf.Max(verticalVelocity, velocity.y);
            dashTime = 0f;
            pullTime = 0f;
        }

        public void Dash(Vector3 direction, float speed, float seconds)
        {
            dashVelocity = direction.normalized * speed;
            dashTime = seconds;
            if (verticalVelocity < 1f) verticalVelocity = 1f;
            if (orbitCamera != null) orbitCamera.Shake(0.1f);
        }

        public void PullTo(Vector3 point, float speed)
        {
            pullPoint = point;
            pullSpeed = speed;
            pullTime = 1.4f;
            dashTime = 0f;
        }

        public void Blink(Vector3 position)
        {
            Teleport(position, transform.eulerAngles.y);
        }

        public Vector3 PastPosition(float seconds)
        {
            int back = Mathf.Clamp(Mathf.RoundToInt(seconds / 0.1f), 1, trail.Length - 1);
            return trail[((trailAt - back) % trail.Length + trail.Length) % trail.Length];
        }

        // ================================================================== boucle

        void Update()
        {
            GameConfig cfg = Game.Config;
            if (cfg == null || Scripted) return;
            Seeker me = Game.Me;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 input = InputLocked ? Vector2.zero : FiefInput.Move;
            Vector3 forward = Vector3.forward, right = Vector3.right;
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

            float factor = me != null ? me.SpeedFactor : 1f;
            float speed = cfg.moveSpeed * factor;
            IsSprinting = !InputLocked && FiefInput.SprintHeld && wish.sqrMagnitude > 0.01f && factor > 0f;
            if (IsSprinting) speed *= cfg.sprintMultiplier;
            CurrentSpeed = wish.magnitude * speed;

            if (orbitCamera != null && orbitCamera.ThroughEyes) transform.rotation = Quaternion.Euler(0f, orbitCamera.yaw, 0f);
            else if (wish.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(wish, Vector3.up), cfg.turnSpeed * dt);

            bool grounded = controller.isGrounded;
            if (grounded && !wasGrounded) Land(me);
            wasGrounded = grounded;

            // --- le saut, le second saut, le planeur
            bool canAct = !InputLocked && factor > 0f;
            Gliding = false;
            if (grounded)
            {
                airJumpUsed = false;
                lastGround = transform.position;
                airTop = transform.position.y;
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (canAct && FiefInput.JumpPressed) verticalVelocity = cfg.jumpSpeed;
            }
            else
            {
                airTop = Mathf.Max(airTop, transform.position.y);
                // Une vraie chute : le vent siffle (une fois).
                if (verticalVelocity < -16f && !windPlayed) { windPlayed = true; Sfx.Whoosh(); }
                if (canAct && FiefInput.JumpPressed && !airJumpUsed && me != null && me.Has(Ability.DoubleSaut))
                {
                    airJumpUsed = true;
                    verticalVelocity = cfg.jumpSpeed * 1.05f;
                    Sfx.Whoosh();
                    Ambiance.Burst(null, transform.position + Vector3.up * 0.2f, AbilityInfo.Tint(Ability.DoubleSaut));
                }
                else if (canAct && FiefInput.JumpHeld && me != null && me.Has(Ability.Planeur) && verticalVelocity < -2.5f)
                {
                    Gliding = true;
                    verticalVelocity = -2.5f;
                }
            }
            verticalVelocity += cfg.gravity * dt;

            // --- la Couronne glisse des mains de qui tombe (sans planer)
            if (me != null && me.CarriesCrown && !grounded && !Gliding && verticalVelocity < -13f)
                Crown.Slip(me, lastGround);

            // --- les mouvements imposes : ruee, grappin, poussee
            Vector3 extra = Vector3.zero;
            if (dashTime > 0f) { dashTime -= dt; extra += dashVelocity; }
            if (pullTime > 0f)
            {
                pullTime -= dt;
                Vector3 to = pullPoint - transform.position;
                if (to.magnitude < 1.6f)
                {
                    // Arrive : on se HISSE sur le rebord (sinon on restait colle dessous
                    // et on retombait) -- un elan vers l'avant, un petit saut.
                    pullTime = 0f;
                    Vector3 on = new Vector3(to.x, 0f, to.z);
                    if (on.sqrMagnitude < 0.01f) on = transform.forward;
                    knock += on.normalized * 7f;
                    verticalVelocity = Mathf.Max(verticalVelocity, 4f);
                }
                else
                {
                    extra += to.normalized * pullSpeed;
                    verticalVelocity = Mathf.Max(verticalVelocity, to.normalized.y * pullSpeed * 0.5f);
                    airTop = transform.position.y;
                }
            }
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-4.5f * dt));

            // Pendant un gros recul, on ne contre-marche pas : le coup porte vraiment.
            float control = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(1f - knock.magnitude / 16f));
            Vector3 motion = wish * speed * control + extra + knock + Vector3.up * verticalVelocity;
            if (pullTime > 0f) motion.y = Mathf.Max(motion.y, (pullPoint - transform.position).normalized.y * pullSpeed);
            controller.Move(motion * dt);

            Remember(dt);
            Footsteps();
            DriveRig(cfg);
            KeepInsideMap(cfg);
        }

        /// <summary>Retomber : une secousse, et avec le Rebond une onde de choc si l'on tombait de haut.</summary>
        void Land(Seeker me)
        {
            float fall = airTop - transform.position.y;
            windPlayed = false;
            if (orbitCamera != null) orbitCamera.Shake(Mathf.Clamp01(fall / 20f) * 0.3f);
            if (fall > 3f) Ambiance.Burst(null, transform.position + Vector3.up * 0.1f, new Color(0.45f, 0.42f, 0.38f));
            if (fall > 10f && Tower.On(lastGround) && !Tower.On(transform.position)) Feed.FellFromTower(me);
            if (fall > 4f && me != null && me.Has(Ability.Rebond))
            {
                Combat.Blast(transform.position, 5f, 13f, 5f, me);
                Ambiance.Burst(null, transform.position + Vector3.up * 0.3f, AbilityInfo.Tint(Ability.Rebond));
                Sfx.Crash();
            }
            if (fall > 3f) Sfx.Thud();
        }

        void Remember(float dt)
        {
            trailTimer += dt;
            if (trailTimer < 0.1f) return;
            trailTimer = 0f;
            trailAt = (trailAt + 1) % trail.Length;
            trail[trailAt] = transform.position;
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

        /// <summary>Un bruit de pas tous les 1,9 m parcourus au sol.</summary>
        void Footsteps()
        {
            if (!controller.isGrounded) return;
            Vector3 flat = controller.velocity;
            flat.y = 0f;
            strideAccumulator += flat.magnitude * Time.deltaTime;
            if (strideAccumulator < 1.9f) return;
            strideAccumulator = 0f;
            Sfx.Step();
            Vector3 p = transform.position;
            if (!Castle.Covers(p.x, p.z, 0f)) Sfx.LeafStep();
        }

        /// <summary>
        /// Deplacer le joueur d'un coup. Un CharacterController ignore qu'on change sa
        /// position a la main : on le coupe, on deplace, on le rallume.
        /// </summary>
        public void Teleport(Vector3 position, float yaw)
        {
            Scripted = false;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            verticalVelocity = 0f;
            airTop = position.y;
            controller.enabled = true;
            if (orbitCamera != null) orbitCamera.yaw = yaw;
        }

        /// <summary>
        /// Filet de securite : on ne sort pas de la carte, et si on passe a travers le
        /// sol on est remis DESSUS (regle relative au sol, jamais une altitude en dur).
        /// </summary>
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
            if (!clamped) return;
            controller.enabled = false;
            transform.position = p;
            controller.enabled = true;
        }
    }
}
