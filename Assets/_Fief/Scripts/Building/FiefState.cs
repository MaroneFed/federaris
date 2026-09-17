using System;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'etat du fief du joueur : son coffre, ses batiments, son Prestige.
    /// Separe de l'inventaire a dessein : le coffre ne pese rien, l'inventaire si.
    /// C'est ce qui cree l'aller-retour "je vide mon sac chez moi puis je repars".
    /// </summary>
    public class FiefState
    {
        public event Action Changed;

        readonly int[] stock = new int[ResourceInfo.Count];
        readonly bool[] built = new bool[5];

        public Vector3 center;
        public int prestige;
        public float buildCostMultiplier = 1f;
        public bool hasChest;
        public bool hasWatchtower;
        public int buildingCount;

        public int Stock(ResourceType type) { return stock[(int)type]; }

        public bool Has(BuildingId id)
        {
            int i = (int)id;
            return i >= 0 && i < built.Length && built[i];
        }

        /// <summary>Cout reel d'une construction, Atelier compris.</summary>
        public int CostOf(BuildingDef def)
        {
            return Mathf.Max(1, Mathf.RoundToInt(def.baseCost * buildCostMultiplier));
        }

        public void AddStock(ResourceType type, int quantity)
        {
            if (quantity <= 0) return;
            stock[(int)type] += quantity;
            Raise();
        }

        public int TakeStock(ResourceType type, int quantity)
        {
            if (quantity <= 0) return 0;
            int taken = Mathf.Min(quantity, stock[(int)type]);
            if (taken <= 0) return 0;
            stock[(int)type] -= taken;
            Raise();
            return taken;
        }

        public int TotalStock
        {
            get
            {
                int total = 0;
                for (int i = 0; i < stock.Length; i++) total += stock[i];
                return total;
            }
        }

        /// <summary>Appelee par BuildPlot une fois la construction payee et posee.</summary>
        public void RegisterBuilt(BuildingDef def)
        {
            int i = (int)def.id;
            if (i >= 0 && i < built.Length) built[i] = true;

            buildingCount++;
            prestige += def.prestige;

            if (def.id == BuildingId.Chest) hasChest = true;
            if (def.id == BuildingId.Watchtower) hasWatchtower = true;
            if (def.id == BuildingId.Workshop) buildCostMultiplier *= 0.85f;

            Raise();
        }

        void Raise()
        {
            if (Changed != null) Changed();
        }
    }
}
