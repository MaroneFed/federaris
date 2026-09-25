using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA camera du jeu. Une seule vue : on regarde par les yeux du personnage.
    ///
    /// Le mode orbital ne sert plus qu'a l'ecran-titre, ou la camera tourne autour
    /// du mendiant pour le montrer. Il n'y a plus de bascule : "firstPerson" etait
    /// un etat qu'on pouvait changer en jeu, ce n'est plus qu'une consequence --
    /// on est dans les yeux des que la scene n'est pas cinematique.
    ///
    /// Elle travaille dans LateUpdate : le joueur bouge d'abord (Update), la camera
    /// se replace ensuite. Sinon l'image tremble.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.55f, 0f);
        public bool InputLocked;

        public float yaw;
        public float pitch = 22f;

        /// <summary>Rotation automatique, en degres par seconde. Utilise par l'ecran-titre.</summary>
        public float autoOrbitSpeed;

        /// <summary>La camera elle-meme : sert a elargir le champ de vision en courant.</summary>
        public Camera view;
        public CharacterRig rig;

        /// <summary>Vrai des que la camera n'est pas en cadrage d'ecran-titre.</summary>
        public bool ThroughEyes { get { return !cinematic; } }

        /// <summary>
        /// 0 = debout, 1 = accroupi. Mis a 1 par PlayerInteractor pendant qu'on
        /// ramasse quelque chose au sol : la camera descend et plonge vers le sol.
        /// On SENT le geste, meme sans voir ses mains.
        /// </summary>
        public static float Crouch;
        float crouched;
        public float baseFieldOfView = 62f;
        public float sprintFieldOfView = 4f;

        float shake;
        float bobPhase;
        float bobOffset;
        public float minPitch = -8f;
        public float maxPitch = 72f;

        float distance = 8f;
        float currentDistance = 8f;

        bool cinematic;
        float cineDistance, cinePitch;

        readonly RaycastHit[] hits = new RaycastHit[8];

        void Start()
        {
            GameConfig cfg = Game.Config;
            if (cfg != null) distance = cfg.cameraDistance;
            currentDistance = distance;
            if (target != null) yaw = target.eulerAngles.y;
        }

        /// <summary>Cadrage large pour l'ecran-titre : on recule et on prend de la hauteur.</summary>
        public void SetCinematic(float distanceOut, float pitchOut)
        {
            cinematic = true;
            cineDistance = distanceOut;
            cinePitch = pitchOut;
        }

        float kick;

        /// <summary>Le champ de vision s'ouvre d'un coup (ruee, grappin, courant) : la vitesse se sent.</summary>
        public void Kick(float degrees)
        {
            kick = Mathf.Max(kick, degrees);
        }

        /// <summary>Secousse breve (atterrissage). L'amplitude est en metres.</summary>
        public void Shake(float amount)
        {
            if (amount > shake) shake = Mathf.Min(0.5f, amount);
        }

        public void ReleaseCinematic()
        {
            cinematic = false;
            pitch = Mathf.Clamp(pitch, -82f, 82f);
            GameConfig cfg = Game.Config;
            if (cfg != null) distance = cfg.cameraDistance;
            pitch = 20f;
        }

        void LateUpdate()
        {
            if (target == null) return;
            GameConfig cfg = Game.Config;
            float sensitivity = cfg != null ? cfg.mouseSensitivity : 0.13f;
            float minD = cfg != null ? cfg.cameraMinDistance : 3f;
            float maxD = cfg != null ? cfg.cameraMaxDistance : 16f;

            // Temps NON mis a l'echelle : la camera continue de vivre quand le jeu est en pause.
            float dt = Time.unscaledDeltaTime;

            // Pendant l'ecran-titre la camera tourne autour du personnage : on montre
            // alors le mendiant entier, pas le corps subjectif. Partout ailleurs on
            // est dans ses yeux.
            bool throughEyes = !cinematic;
            if (rig != null) rig.SetFirstPerson(throughEyes);

            if (!InputLocked)
            {
                Vector2 look = FiefInput.Look;
                yaw += look.x * sensitivity;
                float lowLimit = cinematic ? minPitch : -82f;
                float highLimit = cinematic ? maxPitch : 82f;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, lowLimit, highLimit);
                distance = Mathf.Clamp(distance - FiefInput.ZoomNotches * 1.6f, minD, maxD);
            }

            if (autoOrbitSpeed != 0f) yaw += autoOrbitSpeed * dt;

            if (cinematic)
            {
                distance = Mathf.Lerp(distance, cineDistance, 1f - Mathf.Exp(-2.2f * dt));
                pitch = Mathf.Lerp(pitch, cinePitch, 1f - Mathf.Exp(-2.2f * dt));
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // ---------------------------------------------------------- premiere personne
            if (throughEyes)
            {
                GameConfig config = Game.Config;
                float eye = config != null ? config.eyeHeight : 1.74f;
                float ahead = config != null ? config.eyeForward : 0.14f;
                float bobAmount = config != null ? config.headBob : 0.045f;

                float walk = Game.Player != null ? Game.Player.CurrentSpeed : 0f;

                // Le balancement suit la foulee : deux appuis par enjambee, comme les jambes.
                bobPhase += walk * (Mathf.PI / 1.9f) * dt;
                float moving = Mathf.Clamp01(walk / 1.2f);
                float vertical = Mathf.Abs(Mathf.Sin(bobPhase)) * bobAmount * moving;
                float lateral = Mathf.Sin(bobPhase * 0.5f) * bobAmount * 0.35f * moving;
                bobOffset = Mathf.Lerp(bobOffset, vertical, 1f - Mathf.Exp(-16f * dt));

                // S'accroupir : on descend de 70 cm et on regarde vers ses mains.
                // Chaque frame, ceux qui font se baisser (ramasser, creuser) remettent
                // Crouch a 1 ; on le lit, puis on le remet a zero pour la frame suivante.
                crouched = Mathf.Lerp(crouched, Crouch, 1f - Mathf.Exp(-9f * dt));
                Crouch = 0f;
                float dip = crouched * 0.7f;

                Vector3 forward = target.forward;
                Vector3 right = target.right;
                Vector3 head = target.position
                             + Vector3.up * (eye + bobOffset - dip)
                             + forward * ahead
                             + right * lateral;

                if (view != null)
                {
                    bool running = Game.Player != null && Game.Player.IsSprinting;
                    float wantedFov = baseFieldOfView + (running ? sprintFieldOfView : 0f) + kick;
                    kick = Mathf.MoveTowards(kick, 0f, dt * 30f);
                    view.fieldOfView = Mathf.Lerp(view.fieldOfView, wantedFov, 1f - Mathf.Exp(-9f * dt));
                }

                Vector3 jolt1 = Vector3.zero;
                if (shake > 0.001f)
                {
                    shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
                    jolt1 = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shake * 0.5f;
                }

                transform.position = head + jolt1;
                transform.rotation = Quaternion.Euler(pitch + crouched * 22f, yaw, lateral * 40f + crouched * 3f);
                return;
            }

            Vector3 pivot = target.position + pivotOffset;
            Vector3 direction = rotation * Vector3.back;

            // Si un obstacle est entre le joueur et la camera, on rapproche la camera.
            float wanted = distance;
            float blocked = SweepDistance(pivot, direction, distance);
            if (blocked < wanted) wanted = blocked;

            currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-14f * dt));

            // --- champ de vision : il s'ouvre quand on court, ca donne la sensation de vitesse
            if (view != null)
            {
                bool sprinting = Game.Player != null && Game.Player.IsSprinting && !cinematic;
                float wantedFov = baseFieldOfView + (sprinting ? sprintFieldOfView : 0f);
                view.fieldOfView = Mathf.Lerp(view.fieldOfView, wantedFov, 1f - Mathf.Exp(-5f * dt));
            }

            // --- secousse
            Vector3 jolt = Vector3.zero;
            if (shake > 0.001f)
            {
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
                jolt = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shake;
            }

            transform.position = pivot + direction * currentDistance + jolt;
            transform.rotation = rotation;
        }

        /// <summary>Distance libre devant la camera, en ignorant le joueur lui-meme.</summary>
        float SweepDistance(Vector3 pivot, Vector3 direction, float maxDistance)
        {
            Transform targetRoot = target.root;
            int count = Physics.SphereCastNonAlloc(pivot, 0.28f, direction, hits, maxDistance,
                                                   ~0, QueryTriggerInteraction.Ignore);
            float best = maxDistance;
            for (int i = 0; i < count; i++)
            {
                Transform t = hits[i].transform;
                if (t == null) continue;
                if (t.root == targetRoot) continue;
                float d = hits[i].distance - 0.15f;
                if (d < best) best = d;
            }
            return Mathf.Max(0.6f, best);
        }
    }
}
