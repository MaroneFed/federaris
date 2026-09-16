using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Un emplacement de construction. Decision verrouillee du brief : on ne construit
    /// PAS librement, on construit sur des emplacements definis. C'est plus lisible
    /// pour un raid (on sait ou taper) et infiniment plus simple a repliquer en reseau.
    /// </summary>
    public class BuildPlot : MonoBehaviour, IInteractable
    {
        public int index;

        GameObject freeMarker;

        public bool IsOccupied { get; private set; }
        public BuildingDef Built { get; private set; }

        public void Initialise(int plotIndex, GameObject marker)
        {
            index = plotIndex;
            freeMarker = marker;
        }

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return !IsOccupied; } }
        public float HoldDuration { get { return 0f; } }

        public string Prompt
        {
            get { return "Construire ici  (emplacement " + (index + 1) + ")"; }
        }

        public void Interact()
        {
            if (Game.Hud == null || IsOccupied) return;
            Game.Hud.OpenPanel(new BuildPanel(this));
        }

        /// <summary>
        /// Demande de construction. Comme pour le marche, c'est LE point unique
        /// qui debite l'or et modifie le monde : en Phase 3 il deviendra un ServerRpc.
        /// </summary>
        public bool TryBuild(BuildingDef def)
        {
            if (def == null) return false;
            if (IsOccupied)
            {
                Toasts.Show("Emplacement deja occupe", Palette.Iron);
                return false;
            }

            FiefState fief = Game.Fief;
            Wallet wallet = Game.Wallet;
            if (fief == null || wallet == null) return false;

            int cost = fief.CostOf(def);
            if (!wallet.TrySpend(cost))
            {
                Toasts.Show("Il te manque " + (cost - wallet.Gold) + " or pour " + def.name, Palette.Iron);
                return false;
            }

            IsOccupied = true;
            Built = def;
            if (freeMarker != null) freeMarker.SetActive(false);

            BuildingFactory.Spawn(this, def);
            fief.RegisterBuilt(def);

            Toasts.Show(def.name + " construit  (-" + cost + " or, +" + def.prestige + " prestige)", Palette.Gold);
            return true;
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
