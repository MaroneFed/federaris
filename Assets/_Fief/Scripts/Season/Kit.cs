using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES OUTILS : ce qu'on tient en main. Deux emplacements seulement (touches 1
    /// et 2) -- on choisit ce qu'on emporte.
    ///
    ///   HACHE  3 bois mort + 2 pierres-lune.  Abat les arbres (beaucoup de bois
    ///          d'un coup), et casse au bout de 12 coups.
    ///   EPEE   2 bois mort + 3 fer ancien.    Pour se battre. 25 coups, puis elle
    ///          casse aussi. Le fer vient du chateau : il faut passer les gardes.
    ///   PIEGE  3 bois mort + 2 fer ancien.    Des machoires de fer qu'on pose au
    ///          sol (clic). Qui marche dessus meurt et lache TOUT (Martin, 25/09).
    ///          Il occupe un emplacement jusqu'a ce qu'on le pose.
    ///
    /// Classe C# pure, comme le sac. On ne fabrique que par TryCraft (qui passe
    /// par Inventory.TryRemove), on n'use que par Wear.
    /// </summary>
    public enum ToolKind { None = 0, Hache = 1, Epee = 2, Piege = 3 }

    public class Tool
    {
        public readonly ToolKind Kind;
        public readonly int Max;
        public int Durability;

        public Tool(ToolKind kind)
        {
            Kind = kind;
            Max = Kit.MaxDurability(kind);
            Durability = Max;
        }
    }

    public class Kit
    {
        public readonly Tool[] Slots = new Tool[2];
        public int Active = -1;

        public Tool Held { get { return Active >= 0 ? Slots[Active] : null; } }
        public bool Holding(ToolKind k) { return Held != null && Held.Kind == k; }

        public static string Name(ToolKind k)
        {
            return k == ToolKind.Hache ? "Hache" : k == ToolKind.Epee ? "Épée" : k == ToolKind.Piege ? "Piège" : "";
        }

        public static int MaxDurability(ToolKind k)
        {
            return k == ToolKind.Hache ? 12 : k == ToolKind.Epee ? 25 : k == ToolKind.Piege ? 1 : 0;
        }

        /// <summary>Ce que coute un outil : bois mort, pierre-lune, fer ancien.</summary>
        public static int[] Cost(ToolKind k)
        {
            if (k == ToolKind.Hache) return new[] { 3, 0, 1 };
            if (k == ToolKind.Epee) return new[] { 2, 0, 3 };
            if (k == ToolKind.Piege) return new[] { 3, 0, 2 };
            return new[] { 0, 0, 0 };
        }

        public static bool CanAfford(ToolKind k, Inventory bag)
        {
            if (bag == null) return false;
            int[] cost = Cost(k);
            for (int i = 0; i < cost.Length; i++) if (bag.Get((ResourceType)i) < cost[i]) return false;
            return true;
        }

        public int FreeSlot
        {
            get
            {
                for (int i = 0; i < Slots.Length; i++) if (Slots[i] == null) return i;
                return -1;
            }
        }

        public bool TryCraft(ToolKind k, Inventory bag)
        {
            int slot = FreeSlot;
            if (slot < 0 || !CanAfford(k, bag)) return false;
            int[] cost = Cost(k);
            for (int i = 0; i < cost.Length; i++) bag.TryRemove((ResourceType)i, cost[i]);
            Slots[slot] = new Tool(k);
            Active = slot;
            return true;
        }

        /// <summary>Prendre en main l'emplacement 0 ou 1 -- ou le ranger s'il l'etait deja.</summary>
        public void Select(int slot)
        {
            if (slot < 0 || slot >= Slots.Length) return;
            Active = Active == slot || Slots[slot] == null ? -1 : slot;
        }

        /// <summary>La molette : passer a l'outil suivant (ou precedent), en sautant les cases vides.</summary>
        public void Cycle(int direction)
        {
            int start = Active < 0 ? (direction > 0 ? -1 : Slots.Length) : Active;
            for (int k = 1; k <= Slots.Length; k++)
            {
                int i = ((start + direction * k) % Slots.Length + Slots.Length) % Slots.Length;
                if (Slots[i] != null && i != Active) { Active = i; return; }
            }
        }

        /// <summary>User l'outil en main. Vrai s'il vient de casser.</summary>
        public bool Wear(int amount)
        {
            Tool t = Held;
            if (t == null) return false;
            t.Durability -= amount;
            if (t.Durability > 0) return false;
            Slots[Active] = null;
            Active = -1;
            return true;
        }

        /// <summary>Tout perdre (en mourant, les outils tombent avec le sac).</summary>
        public void Clear()
        {
            for (int i = 0; i < Slots.Length; i++) Slots[i] = null;
            Active = -1;
        }
    }
}
