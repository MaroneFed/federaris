using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le coffre du fief. Deposer vide ton sac (donc te rend ta vitesse),
    /// retirer recharge ton sac dans la limite du poids.
    /// </summary>
    public class Chest : MonoBehaviour, IInteractable
    {
        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return Game.Fief != null; } }
        public float HoldDuration { get { return 0f; } }

        public string Prompt
        {
            get
            {
                int n = Game.Fief != null ? Game.Fief.TotalStock : 0;
                return "Ouvrir le coffre  (" + n + " unites stockees)";
            }
        }

        public void Interact()
        {
            if (Game.Hud == null) return;
            Game.Hud.OpenPanel(new ChestPanel(this));
        }

        public bool PlayerIsClose()
        {
            if (Game.PlayerTransform == null) return false;
            Vector3 a = Game.PlayerTransform.position;
            Vector3 b = transform.position;
            a.y = 0f; b.y = 0f;
            return (a - b).sqrMagnitude <= 9f * 9f;
        }
    }
}
