using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES OBJETS qu'on trouve dans la foret (voir docs/LA-SAISON.md). Trois
    /// emplacements (1, 2, 3) ; l'epee, elle, est toujours la (clic gauche quand on
    /// ne tient rien d'autre).
    /// </summary>
    public enum Item { None, Detecteur, Pelle, Fumigene, Lenteur, Piege, Elixir, Plume, CapeOmbre, Cle }

    public static class ItemInfo
    {
        public static string Name(Item i)
        {
            switch (i)
            {
                case Item.Detecteur: return "Détecteur";
                case Item.Pelle: return "Pelle";
                case Item.Fumigene: return "Fumigène";
                case Item.Lenteur: return "Fiole de lenteur";
                case Item.Piege: return "Piège";
                case Item.Elixir: return "Élixir";
                case Item.Plume: return "Plume";
                case Item.CapeOmbre: return "Cape d'ombre";
                case Item.Cle: return "Clé du donjon";
                default: return "";
            }
        }

        public static Color Tint(Item i)
        {
            switch (i)
            {
                case Item.Detecteur: return new Color(0.75f, 0.78f, 0.82f);
                case Item.Pelle: return new Color(0.7f, 0.55f, 0.35f);
                case Item.Fumigene: return new Color(0.6f, 0.62f, 0.66f);
                case Item.Lenteur: return new Color(0.55f, 0.4f, 0.9f);
                case Item.Piege: return new Color(0.5f, 0.48f, 0.45f);
                case Item.Elixir: return new Color(0.95f, 0.35f, 0.4f);
                case Item.Plume: return new Color(0.85f, 0.95f, 1f);
                case Item.CapeOmbre: return new Color(0.35f, 0.3f, 0.55f);
                case Item.Cle: return new Color(0.95f, 0.78f, 0.35f);
                default: return Color.white;
            }
        }

        /// <summary>Les objets qu'on n'use pas : on les garde en main.</summary>
        public static bool Tool(Item i) { return i == Item.Detecteur || i == Item.Pelle; }

        /// <summary>Ce que contient un coffre ordinaire (tire au hasard).</summary>
        public static readonly Item[] Common = { Item.Detecteur, Item.Pelle, Item.Fumigene, Item.Lenteur, Item.Piege, Item.Elixir, Item.Pelle, Item.Detecteur };
        /// <summary>Ce qu'on deterre (plus rare, plus fort).</summary>
        public static readonly Item[] Rare = { Item.Plume, Item.CapeOmbre, Item.Cle, Item.Elixir, Item.Plume, Item.CapeOmbre };
    }

    /// <summary>Les trois emplacements d'objets d'un joueur. Classe C# pure.</summary>
    public class Loadout
    {
        public const int Size = 3;
        public readonly Item[] Slots = new Item[Size];
        /// <summary>L'emplacement en main, ou -1 : l'epee.</summary>
        public int Active = -1;

        public Item Held { get { return Active >= 0 ? Slots[Active] : Item.None; } }
        public bool HoldingSword { get { return Held == Item.None; } }

        public bool Has(Item i)
        {
            for (int k = 0; k < Size; k++) if (Slots[k] == i) return true;
            return false;
        }

        public bool Full
        {
            get
            {
                for (int k = 0; k < Size; k++) if (Slots[k] == Item.None) return false;
                return true;
            }
        }

        /// <summary>Ranger un objet dans le premier emplacement libre. Faux si tout est plein.</summary>
        public bool TryAdd(Item i)
        {
            if (i == Item.None) return false;
            for (int k = 0; k < Size; k++)
            {
                if (Slots[k] != Item.None) continue;
                Slots[k] = i;
                return true;
            }
            return false;
        }

        /// <summary>L'objet en main est use : il disparait, on reprend l'epee.</summary>
        public void ConsumeHeld()
        {
            if (Active < 0) return;
            Slots[Active] = Item.None;
            Active = -1;
        }

        /// <summary>Retirer un objet precis (la cle, a la porte).</summary>
        public bool TryRemove(Item i)
        {
            for (int k = 0; k < Size; k++)
            {
                if (Slots[k] != i) continue;
                Slots[k] = Item.None;
                if (Active == k) Active = -1;
                return true;
            }
            return false;
        }

        /// <summary>Prendre en main l'emplacement "slot" -- ou revenir a l'epee s'il l'etait deja, ou s'il est vide.</summary>
        public void Select(int slot)
        {
            if (slot < 0 || slot >= Size) { Active = -1; return; }
            Active = Active == slot || Slots[slot] == Item.None ? -1 : slot;
        }

        /// <summary>La molette : de l'epee aux objets, en sautant les cases vides.</summary>
        public void Cycle(int direction)
        {
            int n = Size + 1;           // l'epee compte comme une case
            int at = Active + 1;
            for (int k = 1; k <= n; k++)
            {
                int i = ((at + direction * k) % n + n) % n;
                if (i == 0 || Slots[i - 1] != Item.None) { Active = i - 1; return; }
            }
        }

        public void Clear()
        {
            for (int k = 0; k < Size; k++) Slots[k] = Item.None;
            Active = -1;
        }
    }
}
