using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Camera orbitale 3e personne : la souris tourne autour du joueur, la molette zoome.
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
        public float baseFieldOfView = 62f;
        public float sprintFieldOfView = 7.5f;

        float shake;
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

        /// <summary>Secousse breve (atterrissage). L'amplitude est en metres.</summary>
        public void Shake(float amount)
        {
            if (amount > shake) shake = Mathf.Min(0.5f, amount);
        }

        public void ReleaseCinematic()
        {
            cinematic = false;
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

            if (!InputLocked)
            {
                Vector2 look = FiefInput.Look;
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
                distance = Mathf.Clamp(distance - FiefInput.ZoomNotches * 1.6f, minD, maxD);
            }

            if (autoOrbitSpeed != 0f) yaw += autoOrbitSpeed * dt;

            if (cinematic)
            {
                distance = Mathf.Lerp(distance, cineDistance, 1f - Mathf.Exp(-2.2f * dt));
                pitch = Mathf.Lerp(pitch, cinePitch, 1f - Mathf.Exp(-2.2f * dt));
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
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
