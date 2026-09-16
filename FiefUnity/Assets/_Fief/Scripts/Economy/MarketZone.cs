using UnityEngine;

namespace Fief
{
    /// <summary>
    /// La zone du marche central. S'approcher + E ouvre le panneau de negoce.
    /// Concept Unity : le collider est en "Is Trigger" -> il ne bloque pas le joueur,
    /// il sert uniquement a etre detecte.
    /// </summary>
    public class MarketZone : MonoBehaviour, IInteractable
    {
        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return Game.Market != null; } }
        public string Prompt { get { return "Ouvrir le marche"; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null) return;
            Game.Hud.OpenPanel(new MarketPanel(this));
        }

        /// <summary>Le panneau se ferme tout seul si on s'eloigne.</summary>
        public bool PlayerIsClose()
        {
            if (Game.PlayerTransform == null) return false;
            Vector3 a = Game.PlayerTransform.position;
            Vector3 b = transform.position;
            a.y = 0f; b.y = 0f;
            float radius = Game.Config != null ? Game.Config.marketRadius : 14f;
            return (a - b).sqrMagnitude <= (radius + 4f) * (radius + 4f);
        }
    }
}
