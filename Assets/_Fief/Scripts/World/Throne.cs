using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE TRONE du roi sans tete, au fond de la salle du donjon.
    ///
    /// Tant qu'on n'a pas les six talismans, s'y asseoir ne fait rien -- la pierre
    /// reste froide, et le trone le dit. Avec les six : on s'assoit, et la Saison
    /// s'arrete. Victoire par la Couronne.
    /// </summary>
    public class Throne : MonoBehaviour, IInteractable
    {
        public static void Build(Transform parent, Vector3 seat)
        {
            GameObject go = new GameObject("TRONE");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = seat;
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.4f, 1.2f, 1.4f);
            go.AddComponent<Throne>();
        }

        public Transform Anchor { get { return transform; } }

        public bool CanInteract { get { return Game.Hoard != null && (Game.Season == null || !Game.Season.Over); } }

        public string Prompt
        {
            get
            {
                int n = Game.Hoard != null ? Game.Hoard.TalismanCount : 0;
                return n >= TalismanInfo.Count ? "S'asseoir sur le trone du roi sans tete"
                                               : "Le trone du roi sans tete  (" + n + " / " + TalismanInfo.Count + " talismans)";
            }
        }

        public float HoldDuration
        {
            get { return Game.Hoard != null && Game.Hoard.TalismanCount >= TalismanInfo.Count ? 2.5f : 0f; }
        }

        public void Interact()
        {
            Hoard h = Game.Hoard;
            if (h == null) return;
            if (h.TalismanCount < TalismanInfo.Count)
            {
                Sfx.Deny();
                Toasts.Show("La pierre reste froide. Il lui faut les six talismans -- il t'en manque "
                            + (TalismanInfo.Count - h.TalismanCount) + ".", new Color(0.95f, 0.78f, 0.35f));
                return;
            }
            Victories.Declare(Game.Me, VictoryKind.Couronne);
        }
    }
}
