using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES AMELIORATIONS, qu'on achete a sa stele (Martin, 25/09 : "faut trouver
    /// plein d'upgrades"). On les paie avec ce qui dort dans sa RESERVE -- pas avec
    /// le sac : c'est une raison de plus de tout rapporter a la stele -- et parfois
    /// avec de l'or.
    ///
    ///   Besace renforcee   +20 kg dans le sac (deux niveaux)
    ///   Amulette du glas   la Malediction te laisse la moitie de ton sac
    ///   Sentinelle         ta stele sonne quand un etranger s'en approche
    ///   Verre poli         ta lanterne eclaire un tiers plus loin
    ///   Lame trempee       tes coups d'epee font 40 % de degats en plus
    ///   Bottes de cerf     tu marches 10 % plus vite (deux niveaux)
    ///   Collets            deux pieges de plus poses en meme temps (deux niveaux)
    ///
    /// Aucune n'est indispensable, aucune ne gagne la partie : elles donnent des
    /// FACONS de jouer (le pillard qui court, le batisseur qui piege, le prudent
    /// qui garde sa moitie de sac). Classe C# pure.
    /// </summary>
    public enum UpgradeKind { Besace = 0, Amulette = 1, Sentinelle = 2, Lanterne = 3, Lame = 4, Bottes = 5, Collets = 6 }

    public static class UpgradeInfo
    {
        public const int Count = 7;

        public const float BesaceKilos = 20f;
        public const float AmuletteKeeps = 0.5f;
        public const float LanternFactor = 1.35f;
        public const float LameFactor = 1.4f;
        public const float BottesFactor = 0.1f;
        public const int ColletsPerLevel = 2;

        public static string Name(UpgradeKind k)
        {
            switch (k)
            {
                case UpgradeKind.Besace: return "Besace renforcée";
                case UpgradeKind.Amulette: return "Amulette du glas";
                case UpgradeKind.Sentinelle: return "Sentinelle";
                case UpgradeKind.Lanterne: return "Verre poli";
                case UpgradeKind.Lame: return "Lame trempée";
                case UpgradeKind.Bottes: return "Bottes de cerf";
                default: return "Collets";
            }
        }

        public static string Effect(UpgradeKind k)
        {
            switch (k)
            {
                case UpgradeKind.Besace: return "+" + Mathf.RoundToInt(BesaceKilos) + " kg";
                case UpgradeKind.Amulette: return "La Malédiction ne prend que la moitié";
                case UpgradeKind.Sentinelle: return "Ta stèle t'alerte des intrus";
                case UpgradeKind.Lanterne: return "Lanterne +33 %";
                case UpgradeKind.Lame: return "Épée +40 %";
                case UpgradeKind.Bottes: return "Tu marches 10 % plus vite.";
                default: return "Deux pièges de plus posés en même temps.";
            }
        }

        public static int MaxLevel(UpgradeKind k)
        {
            return k == UpgradeKind.Besace || k == UpgradeKind.Bottes || k == UpgradeKind.Collets ? 2 : 1;
        }

        /// <summary>Le prix du niveau suivant : bois mort, pierre-lune, fer ancien -- puis l'or.</summary>
        public static int[] Cost(UpgradeKind k, int level)
        {
            bool second = level >= 1;
            switch (k)
            {
                case UpgradeKind.Besace: return second ? new[] { 16, 4, 0 } : new[] { 10, 2, 0 };
                case UpgradeKind.Amulette: return new[] { 0, 4, 2 };
                case UpgradeKind.Sentinelle: return new[] { 8, 2, 0 };
                case UpgradeKind.Lanterne: return new[] { 0, 3, 1 };
                case UpgradeKind.Lame: return new[] { 0, 0, 4 };
                case UpgradeKind.Bottes: return second ? new[] { 12, 2, 0 } : new[] { 8, 1, 0 };
                default: return second ? new[] { 8, 0, 3 } : new[] { 6, 0, 2 };
            }
        }

        public static int GoldCost(UpgradeKind k, int level)
        {
            switch (k)
            {
                case UpgradeKind.Besace: return level >= 1 ? 10 : 0;
                case UpgradeKind.Amulette: return 15;
                case UpgradeKind.Lame: return 10;
                case UpgradeKind.Bottes: return 10;
                default: return 0;
            }
        }
    }
}
