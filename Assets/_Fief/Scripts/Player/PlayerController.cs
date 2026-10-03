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
    ///   - le DOUBLE SAUT, le REBOND ;
    ///   - LE VOL PLANE (28/09) : les ailes s'ouvrent seules au-dessus du vide, on
    ///     dirige a la souris (voir World/Wings.cs) ; Espace les replie ou les rouvre ;
    ///   - l'ETOURDISSEMENT (on ne bouge plus), le GIVRE (moitie moins vite) ;
    ///   - la COURONNE : si son porteur tombe assomme (etourdi, ejecte), elle reste la ou
    ///     il a quitte le sol (Crown.Slip). Replier ses ailes ne la fait plus lacher (04/10).
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
        /// <summary>La vitesse en vol plane (m/s) : le HUD, la camera, le vent s'en servent.</summary>
        public float Airspeed { get { return Gliding ? airspeed : 0f; } }
        /// <summary>L'inclinaison de la camera en vol (degres) : on penche dans les virages.</summary>
        public float FlightRoll { get; private set; }
        /// <summary>L'ouverture du champ de vision en vol (degres) : plus on va vite, plus il s'ouvre.</summary>
        public float FlightFov { get; private set; }
        float airspeed;
        bool folded;                // Espace en l'air : ailes repliees jusqu'au prochain appui (ou au sol)
        float lastYaw;
        GlideFeel feel;

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
            if (velocity.y > 20f && orbitCamera != null) orbitCamera.Kick(14f);
            dashTime = 0f;
            pullTime = 0f;
        }

        public void Dash(Vector3 direction, float speed, float seconds)
        {
            dashVelocity = direction.normalized * speed;
            dashTime = seconds;
            if (verticalVelocity < 1f) verticalVelocity = 1f;
            if (orbitCamera != null) { orbitCamera.Shake(0.1f); orbitCamera.Kick(10f); }
        }

        public void PullTo(Vector3 point, float speed)
        {
            pullPoint = point;
            pullSpeed = speed;
            pullTime = 1.4f;
            dashTime = 0f;
            if (orbitCamera != null) orbitCamera.Kick(12f);
        }

        /// <summary>
        /// ETRE LANCE (une arbaleste) : une vraie trajectoire balistique, qui ne
        /// s'amortit pas comme une poussee -- on vole jusqu'a toucher quelque chose.
        /// </summary>
        public void Launch(Vector3 velocity)
        {
            ballistic = true;
            launchAge = 0f;
            flight = new Vector3(velocity.x, 0f, velocity.z);
            verticalVelocity = velocity.y;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
            airTop = transform.position.y;
            if (orbitCamera != null) { orbitCamera.Kick(18f); orbitCamera.Shake(0.25f); }
        }

        bool ballistic;
        float launchAge;
        Vector3 flight;
        Seeker diveTarget;
        float diveTime;

        /// <summary>LE PIQUE D'AIGLE : on fond sur le porteur (voir Combat.Dive).</summary>
        public void Dive(Seeker target)
        {
            diveTarget = target;
            diveTime = Combat.DiveSeconds;
            Gliding = false;
            ballistic = false;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
            if (orbitCamera != null) { orbitCamera.Kick(22f); orbitCamera.Shake(0.15f); }
        }

        /// <summary>En l'air pour de bon (pas un petit saut) : le pique d'aigle est possible.</summary>
        public bool Airborne { get { return !controller.isGrounded && (Gliding || ballistic || airTop - transform.position.y > 1.5f || verticalVelocity < -4f); } }

        /// <summary>Vrai pendant un pique.</summary>
        public bool Diving { get { return diveTime > 0f; } }

        /// <summary>Vrai pendant un vol d'arbaleste (le HUD, les bots s'en servent).</summary>
        public bool Flying { get { return ballistic; } }

        public void Blink(Vector3 position)
        {
            Teleport(position, transform.eulerAngles.y);
        }

        public Vector3 LastGround { get { return lastGround; } }

        /// <summary>Oublier ou l'on etait (apres un respawn : le Rappel ne renvoie pas dans le vide).</summary>
        public void Forget()
        {
            for (int i = 0; i < trail.Length; i++) trail[i] = transform.position;
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

            if (me != null && me.Has(Ability.Colosse)) GiantAura.Keep(me);
            Vector2 input = InputLocked ? Vector2.zero : FiefInput.Move;
            // (06/10) TETE A L'ENVERS : avant/arriere, gauche/droite, tout s'inverse.
            if (me != null && me.Inverted) input = -input;
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
            // (07/10) HYPNOSE : tes pieds marchent tout seuls vers celui qui t'a hypnotise.
            if (me != null && me.Charmed)
            {
                Vector3 to = me.CharmedBy.Body.position - transform.position;
                to.y = 0f;
                wish = to.sqrMagnitude > 1f ? to.normalized : Vector3.zero;
            }

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
            Wings.Tick(me, grounded);
            launchAge += dt;
            if (ballistic && grounded && launchAge > 0.2f) ballistic = false;

            // --- le saut, le second saut, le vol plane
            // (30/09 -- "le saut, il bug") Trois corrections classiques des jeux de
            // plateforme : on COLLE a la pente quand on la descend en courant (avant, on
            // decollait a chaque pas et le saut ne partait pas) ; un appui un poil trop
            // tot est GARDE 0,15 s ; un appui un poil trop tard, juste apres le bord,
            // SAUTE quand meme (le "coyote time"). Et Espace n'ouvre plus les ailes au
            // ras de la rampe : seulement s'il y a du vide dessous.
            bool canAct = !InputLocked && factor > 0f;
            // (06/10) Enchaine, englue ou en ballon : pas de saut.
            if (me != null && me.NoJump) canAct = false;
            if (canAct && FiefInput.JumpPressed) jumpPressedAt = Time.time;
            // (06/10) LE NINJA : immobile une seconde au sol (sans la Couronne), il disparait.
            if (me != null && me.Has(Ability.Ninja) && grounded && CurrentSpeed < 0.3f && !me.CarriesCrown)
            {
                stillFor += dt;
                if (stillFor > 1f) me.HiddenUntil = Mathf.Max(me.HiddenUntil, Time.time + 0.15f);
            }
            else stillFor = 0f;
            if (grounded)
            {
                groundedAt = Time.time;
                if (Gliding) Sfx.Land();
                Gliding = false;
                folded = false;
                airJumpUsed = false;
                lastGround = transform.position;
                airTop = transform.position.y;
                if (verticalVelocity < 0f) verticalVelocity = -Mathf.Max(2f, CurrentSpeed * 0.65f);
                if (canAct && Time.time - jumpPressedAt < 0.15f) Jump(cfg);
            }
            else
            {
                airTop = Mathf.Max(airTop, transform.position.y);
                // Une vraie chute : le vent siffle (une fois).
                if (verticalVelocity < -16f && !windPlayed && !Gliding) { windPlayed = true; Sfx.Whoosh(); }
                bool jump = canAct && FiefInput.JumpPressed;
                bool coyote = Time.time - groundedAt < 0.14f && verticalVelocity <= 0.5f && !Gliding && !ballistic;
                if (jump && coyote) Jump(cfg);
                else if (jump && !Gliding && !airJumpUsed && me != null && me.Has(Ability.DoubleSaut))
                {
                    airJumpUsed = true;
                    verticalVelocity = cfg.jumpSpeed * 1.05f;
                    Sfx.Whoosh();
                    Fx.Ring(transform.position + Vector3.up * 0.1f, AbilityInfo.Tint(Ability.DoubleSaut), 0.3f, 2.4f, 0.3f, 0.2f, Vector3.up);
                    Fx.Burst(transform.position, AbilityInfo.Tint(Ability.DoubleSaut), 25, 5f, 0.14f, 0.5f, 0.3f, Vector3.down, 40f);
                }
                // Espace en vol : on replie les ailes (on tombe comme une pierre).
                else if (jump && Gliding) { Gliding = false; folded = true; Sfx.Whoosh(); }
                // Espace en tombant : on les rouvre (ou on les ouvre plus tot).
                else if (jump && me != null && me.CanGlide && verticalVelocity < 2f && Wings.VoidBelow(transform.position, 4f)) OpenWings(me);
                // LES AILES S'OUVRENT TOUTES SEULES : on tombe, et il y a du vide dessous.
                // (Tire par une arbaleste : seulement au-dessus du grand vide, pour
                // retomber la ou la ligne l'avait dit.)
                else if (!Gliding && !folded && me != null && me.CanGlide && verticalVelocity < -6f
                         && (!ballistic || launchAge > 3.6f)
                         && Wings.VoidBelow(transform.position, ballistic ? 45f : Wings.OpenAbove))
                    OpenWings(me);
                // (04/10) LE VOL LIBRE : tire par une arbaleste, les ailes s'ouvrent des le haut de
                // la courbe (plus besoin d'attendre le grand vide) -- et on vole.
                else if (!Gliding && !folded && me != null && me.FreeFlight && me.CanGlide && launchAge > 0.6f && verticalVelocity < 4f
                         && Wings.VoidBelow(transform.position, 3f))
                    OpenWings(me);
                // Etourdi en plein vol : les ailes se ferment (on tombe), elles se rouvriront.
                if (Gliding && me != null && !me.CanGlide) Gliding = false;
            }
            Vector3 glide = Vector3.zero;
            if (Gliding)
            {
                // ON VOLE COMME ON REGARDE : en bas on pique, en haut on remonte.
                Vector3 look = cameraTransform != null ? cameraTransform.forward : transform.forward;
                glide = Wings.Fly(ref airspeed, look, InputLocked ? 0f : input.x, !InputLocked && input.y < -0.5f, me, dt);
                verticalVelocity = glide.y;
                glide.y = 0f;
                ballistic = false;
                // Un mur en face : on perd sa vitesse.
                if ((controller.collisionFlags & CollisionFlags.Sides) != 0) airspeed = Mathf.Max(Wings.MinSpeed, airspeed * 0.5f);
            }
            else verticalVelocity += cfg.gravity * dt;
            FlightFeel(input, dt);

            // --- la Couronne glisse des mains de qui tombe ASSOMME (etourdi, ejecte de la tour).
            // (04/10, Martin : "quand j'appuie sur espace, quand je vole, hop, elle s'enleve") :
            // avant, elle glissait des qu'on tombait sans ailes -- Espace en vol, ou la descente
            // apres une arbaleste avant que les ailes s'ouvrent. Replier ses ailes, sauter,
            // tomber : elle reste dans tes mains. Seul un coup la fait lacher.
            if (me != null && me.CarriesCrown && !grounded && !me.CanGlide && !ballistic && verticalVelocity < -13f)
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
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-Combat.KnockDrag(me) * dt));

            // Pendant un gros recul, on ne contre-marche pas : le coup porte vraiment.
            float control = me != null && me.Tumbling ? 0.15f : Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(1f - knock.magnitude / 16f));
            Vector3 walk = wish * speed * control;
            if (Gliding) walk = glide;
            else if (ballistic)
            {
                // Tire par une arbaleste : on suit exactement la courbe dessinee (a peine de controle).
                walk = flight + wish * 1.5f;
            }
            Vector3 motion = walk + extra + knock + Vector3.up * verticalVelocity;
            if (pullTime > 0f) motion.y = Mathf.Max(motion.y, (pullPoint - transform.position).normalized.y * pullSpeed);
            // Le pique d'aigle remplace tout le reste.
            if (diveTime > 0f)
            {
                Vector3 dv;
                if (!Combat.DiveStep(me, diveTarget, transform.position, ref diveTime, dt, out dv))
                {
                    motion = dv;
                    verticalVelocity = 0f;
                }
                else verticalVelocity = Mathf.Max(verticalVelocity, 5f);
            }
            Vector3 before = transform.position;
            controller.Move(motion * dt);
            // La tete cogne : on redescend tout de suite (sinon on restait colle au plafond).
            if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            // Le sceau de la citadelle : on n'y entre pas par les airs.
            // (05/10 -- "il spamme Espace et il passe a travers le mur invisible") : avant, le sceau
            // ne regardait que celui qui PLANE ; ailes repliees d'un appui sur Espace, on passait.
            // (05/10, suite -- "il y en a qui passent par les petits toits des tours bleues") : on se
            // posait sur un toit de tour, a cheval sur la muraille, et on entrait EN MARCHANT. Le
            // sceau arrete desormais TOUT passage de dehors a dedans au-dessus des murailles (ou
            // sous l'ile) -- en vol, en tombant, a pied sur un toit. On entre par une porte.
            if (Ward.Crossing(before, transform.position))
            {
                diveTime = 0f;
                Vector3 push = Ward.Repel(me, transform.position);
                controller.enabled = false;
                transform.position = before;
                controller.enabled = true;
                Gliding = false;
                ballistic = false;
                knock = new Vector3(push.x, 0f, push.z);
                verticalVelocity = push.y;
            }

            Remember(dt);
            Footsteps();
            DriveRig(cfg);
            KeepInsideMap(cfg);
        }

        float groundedAt = -9f;
        float jumpPressedAt = -9f;

        float stillFor;

        void Jump(GameConfig cfg)
        {
            // (06/10) LE KANGOUROU saute une fois et demie plus haut (x1,22 en vitesse).
            Seeker me = Game.Me;
            verticalVelocity = cfg.jumpSpeed * (me != null && me.Has(Ability.Kangourou) ? 1.22f : 1f);
            jumpPressedAt = -9f;
            groundedAt = -9f;
        }

        /// <summary>OUVRIR LES AILES : on garde son elan, un claquement de toile, un anneau.</summary>
        void OpenWings(Seeker me)
        {
            Gliding = true;
            folded = false;
            airspeed = Wings.OpeningSpeed(controller.velocity);
            ballistic = false;
            Sfx.Whoosh();
            Color c = me != null && (me.HasWings || me.Has(Ability.Planeur)) ? Wings.Gold : Wings.Glow;
            Fx.Ring(transform.position + Vector3.up * 1.2f, c, 0.5f, 4f, 0.35f, 0.18f, Vector3.up);
            if (orbitCamera != null) orbitCamera.Kick(6f);
            // (04/10) Cette astuce n'avait pas d'icones : elle ne s'affichait jamais -- d'ou "je n'ai
            // toujours rien capte au vol". Elle s'affiche maintenant (et celle du vol libre).
            if (me != null && Game.Hud != null)
            {
                if (me.FreeFlight) Game.Hud.Tip("vollibre", "VOL LIBRE ! Tu voles où tu regardes, sans tomber, jusqu'à te poser. S pour freiner.");
                else Game.Hud.Tip("vol", "TU VOLES ! Regarde en bas pour piquer et prendre de la vitesse, en haut pour remonter. Espace replie les ailes.");
            }
        }

        /// <summary>
        /// Les SENSATIONS du vol : la camera penche dans les virages, le champ de vision
        /// s'ouvre avec la vitesse, le vent souffle et des filets d'air filent autour.
        /// </summary>
        void FlightFeel(Vector2 input, float dt)
        {
            float yaw = transform.eulerAngles.y;
            float turn = Mathf.DeltaAngle(lastYaw, yaw) / Mathf.Max(dt, 0.001f);
            lastYaw = yaw;
            float roll = Gliding ? Mathf.Clamp(-turn * 0.08f - input.x * 8f, -22f, 22f) : 0f;
            FlightRoll = Mathf.Lerp(FlightRoll, roll, 1f - Mathf.Exp(-5f * dt));
            float fast = Gliding ? Mathf.Clamp01((airspeed - Wings.Cruise) / (Wings.MaxSpeed - Wings.Cruise)) : 0f;
            FlightFov = Mathf.Lerp(FlightFov, Gliding ? 6f + fast * 20f : 0f, 1f - Mathf.Exp(-4f * dt));
            if (feel == null && cameraTransform != null) feel = GlideFeel.Attach(cameraTransform);
            if (feel != null) feel.Set(Gliding ? 0.25f + fast * 0.75f : 0f, Gliding ? airspeed : 0f);
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
                Fx.GroundRing(transform.position, AbilityInfo.Tint(Ability.Rebond), 6f, 0.45f);
                Fx.Shock(transform.position + Vector3.up * 0.5f, AbilityInfo.Tint(Ability.Rebond), 4f, 0.35f);
                Sfx.Crash();
            }
            if (fall > 3f) Sfx.Land();
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
            rig.Tumbling = Game.Me != null && (Game.Me.Tumbling || Game.Me.Launched);
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
            // (02/10) UN pas, selon le sol : l'herbe dehors, la pierre dans la citadelle et
            // sur la tour (avant : la pierre ET l'herbe a chaque pas dehors, deux bruits).
            Vector3 p = transform.position;
            if (Castle.Covers(p.x, p.z, 0f) || Tower.On(p)) Sfx.Step();
            else Sfx.LeafStep();
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
            ballistic = false;
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            verticalVelocity = 0f;
            airTop = position.y;
            controller.enabled = true;
            if (orbitCamera != null) orbitCamera.yaw = yaw;
        }

        /// <summary>
        /// Filet de securite : on ne sort pas de l'espace de jeu ; si on passe a travers
        /// le sol on est remis DESSUS ; si on tombe de l'ile, on reapparait (Respawn).
        /// </summary>
        void KeepInsideMap(GameConfig cfg)
        {
            float limit = cfg.mapSize * 0.5f - 3f;
            Vector3 p = transform.position;
            bool clamped = false;
            if (Mathf.Abs(p.x) > limit) { p.x = Mathf.Sign(p.x) * limit; clamped = true; }
            if (Mathf.Abs(p.z) > limit) { p.z = Mathf.Sign(p.z) * limit; clamped = true; }
            // Tombe dans les nuages : on reapparait sur sa zone de depart.
            if (p.y < Ground.FallLine)
            {
                Respawn.Of(Game.Me);
                return;
            }
            // Passe a travers le sol (juste en dessous, pas en tombant de l'ile) : remis dessus.
            float ground = Ground.Sample(p.x, p.z);
            if (p.y < ground - 4f && p.y > ground - 9f)
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
