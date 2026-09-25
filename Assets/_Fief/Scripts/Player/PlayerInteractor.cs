using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Cherche en permanence l'interactif le plus pertinent autour du joueur,
    /// et declenche l'action sur E (appui court ou maintien selon l'objet).
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public bool InputLocked;

        readonly Collider[] overlap = new Collider[48];

        IInteractable current;
        float holdTimer;
        float swingTimer;

        public IInteractable Current { get { return current; } }
        public float HoldDuration { get; private set; }
        public float HoldProgress01
        {
            get { return HoldDuration <= 0f ? 0f : Mathf.Clamp01(holdTimer / HoldDuration); }
        }

        void Update()
        {
            GameConfig cfg = Game.Config;
            float radius = cfg != null ? cfg.interactRadius : 3.4f;

            IInteractable found = FindBest(radius);
            if (!ReferenceEquals(found, current))
            {
                current = found;
                holdTimer = 0f;
            }

            if (current == null || InputLocked || Game.Me != null && !Game.Me.Alive)
            {
                holdTimer = 0f;
                HoldDuration = 0f;
                return;
            }

            HoldDuration = current.HoldDuration;

            if (HoldDuration <= 0f)
            {
                if (FiefInput.InteractPressed) current.Interact();
                return;
            }

            if (FiefInput.InteractHeld)
            {
                // Un coffre, une depouille, la Couronne tombee : on se baisse pour les prendre.
                if (current is Chest || current is Remains) OrbitCamera.Crouch = Mathf.Max(OrbitCamera.Crouch, 1f);

                // Pendant le maintien, le personnage s'active vraiment : un geste toutes
                // les 0,55 s. Sans ca, maintenir E est une barre de chargement.
                if (holdTimer <= 0f) Swing();
                holdTimer += Time.deltaTime;
                swingTimer -= Time.deltaTime;
                if (swingTimer <= 0f) Swing();

                if (holdTimer >= HoldDuration)
                {
                    holdTimer = 0f;
                    current.Interact();
                }
            }
            else
            {
                holdTimer = 0f;
                swingTimer = 0f;
            }
        }

        void Swing()
        {
            swingTimer = 0.55f;
            if (Game.Rig != null) Game.Rig.PlaySwing();
            Sfx.Rustle();
        }

        /// <summary>
        /// On prend l'interactif le plus proche, avec un bonus pour ce qui est devant nous.
        /// Sans ce bonus, on recolte l'arbre dans notre dos, ce qui est desagreable.
        /// </summary>
        IInteractable FindBest(float radius)
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, overlap, ~0, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider c = overlap[i];
                if (c == null) continue;

                IInteractable candidate = c.GetComponentInParent<IInteractable>();
                if (candidate == null) continue;
                if (!candidate.CanInteract) continue;

                Transform anchor = candidate.Anchor;
                if (anchor == null) continue;

                // On mesure la distance au POINT LE PLUS PROCHE du collider, pas a son centre.
                // Sans ca, une grande zone (le marche fait 14 m de rayon) serait
                // injoignable : on serait dedans tout en etant a 14 m de son centre.
                Vector3 closest = c.ClosestPoint(origin);
                Vector3 to = closest - origin;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > radius + 0.5f) continue;

                float facing = 1f;
                if (distance > 0.35f)
                {
                    float dot = Vector3.Dot(transform.forward, to / distance);
                    facing = Mathf.Lerp(1.6f, 0.7f, (dot + 1f) * 0.5f);
                }

                float score = distance * facing;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
