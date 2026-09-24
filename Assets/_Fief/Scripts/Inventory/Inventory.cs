using System;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'inventaire du joueur. Ce n'est PAS un MonoBehaviour : c'est une classe C# pure.
    /// Avantage : elle est testable, serialisable, et facile a repliquer en reseau plus tard.
    /// Le MonoBehaviour (PlayerController) ne fait que la lire.
    ///
    /// Regle du brief : la limite n'est pas un nombre de cases, c'est un POIDS.
    /// </summary>
    public class Inventory
    {
        readonly int[] amounts = new int[ResourceInfo.Count];

        public float MaxWeight = 60f;

        /// <summary>
        /// Poids porte en plus des ressources : la relique, quand on l'a en main. Elle
        /// ne se range pas dans le sac, mais elle pese -- et c'est voulu : porter sa
        /// relique jusqu'a la stele doit se sentir.
        /// </summary>
        public float ExtraWeight
        {
            get { return extraWeight; }
            set
            {
                float v = Mathf.Max(0f, value);
                if (Mathf.Approximately(v, extraWeight)) return;
                extraWeight = v;
                RaiseChanged();
            }
        }
        float extraWeight;

        /// <summary>Leve a chaque modification : le HUD s'y abonne au lieu de sonder chaque frame.</summary>
        public event Action Changed;

        public int Get(ResourceType type)
        {
            return amounts[(int)type];
        }

        public float Weight
        {
            get
            {
                float w = extraWeight;
                for (int i = 0; i < amounts.Length; i++)
                    w += amounts[i] * ResourceInfo.UnitWeight((ResourceType)i);
                return w;
            }
        }

        /// <summary>Charge de 0 (vide) a 1 (plein). C'est cette valeur qui pilote la vitesse.</summary>
        public float Load01
        {
            get { return MaxWeight <= 0f ? 0f : Mathf.Clamp01(Weight / MaxWeight); }
        }

        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < amounts.Length; i++)
                    if (amounts[i] > 0) return false;
                return true;
            }
        }

        public int TotalUnits
        {
            get
            {
                int total = 0;
                for (int i = 0; i < amounts.Length; i++) total += amounts[i];
                return total;
            }
        }

        /// <summary>Combien d'unites de ce type on peut encore porter.</summary>
        public int SpaceFor(ResourceType type)
        {
            float unit = ResourceInfo.UnitWeight(type);
            if (unit <= 0f) return int.MaxValue;
            float free = MaxWeight - Weight;
            if (free <= 0f) return 0;
            return Mathf.FloorToInt(free / unit);
        }

        /// <summary>Ajoute ce qui rentre, retourne la quantite reellement ajoutee.</summary>
        public int TryAdd(ResourceType type, int quantity)
        {
            if (quantity <= 0) return 0;
            int added = Mathf.Min(quantity, SpaceFor(type));
            if (added <= 0) return 0;
            amounts[(int)type] += added;
            RaiseChanged();
            return added;
        }

        /// <summary>Retire ce qu'il y a, retourne la quantite reellement retiree.</summary>
        public int TryRemove(ResourceType type, int quantity)
        {
            if (quantity <= 0) return 0;
            int removed = Mathf.Min(quantity, amounts[(int)type]);
            if (removed <= 0) return 0;
            amounts[(int)type] -= removed;
            RaiseChanged();
            return removed;
        }

        public void Clear()
        {
            for (int i = 0; i < amounts.Length; i++) amounts[i] = 0;
            RaiseChanged();
        }

        void RaiseChanged()
        {
            if (Changed != null) Changed();
        }
    }
}
