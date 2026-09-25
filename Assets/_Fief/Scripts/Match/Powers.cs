using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES POUVOIRS qu'on choisit entre deux manches (voir docs/LA-SAISON.md). On les
    /// garde jusqu'a la fin du match. Chacun est lu la ou il agit : PlayerController
    /// pour le saut et la vitesse, Seeker pour la vie, Guard pour l'Ombre, etc.
    /// </summary>
    public enum Power { DoubleSaut, Ruee, Coureur, Poigne, Colosse, Ombre, Flair, Porteur, SangVif, SecondeChance }

    public static class PowerInfo
    {
        public const int Count = 10;

        public static string Name(Power p)
        {
            switch (p)
            {
                case Power.DoubleSaut: return "Double saut";
                case Power.Ruee: return "Ruée";
                case Power.Coureur: return "Coureur";
                case Power.Poigne: return "Poigne";
                case Power.Colosse: return "Colosse";
                case Power.Ombre: return "Ombre";
                case Power.Flair: return "Flair";
                case Power.Porteur: return "Porteur";
                case Power.SangVif: return "Sang vif";
                default: return "Seconde chance";
            }
        }

        /// <summary>Quelques mots, pas une phrase : ce qui s'affiche sur la carte du choix.</summary>
        public static string Effect(Power p)
        {
            switch (p)
            {
                case Power.DoubleSaut: return "sauter en l'air";
                case Power.Ruee: return "R : bond de 8 m";
                case Power.Coureur: return "+15 % vitesse";
                case Power.Poigne: return "poussée ×2";
                case Power.Colosse: return "+50 % vie";
                case Power.Ombre: return "gardes aveugles";
                case Power.Flair: return "voir la Couronne";
                case Power.Porteur: return "couronne légère";
                case Power.SangVif: return "vie ×3 plus vite";
                default: return "se relever une fois";
            }
        }

        public static Color Tint(Power p)
        {
            switch (p)
            {
                case Power.DoubleSaut: return new Color(0.55f, 0.8f, 1f);
                case Power.Ruee: return new Color(1f, 0.6f, 0.3f);
                case Power.Coureur: return new Color(0.6f, 1f, 0.55f);
                case Power.Poigne: return new Color(0.95f, 0.4f, 0.35f);
                case Power.Colosse: return new Color(0.85f, 0.3f, 0.3f);
                case Power.Ombre: return new Color(0.55f, 0.5f, 0.85f);
                case Power.Flair: return new Color(1f, 0.85f, 0.35f);
                case Power.Porteur: return new Color(0.95f, 0.75f, 0.35f);
                case Power.SangVif: return new Color(0.9f, 0.35f, 0.5f);
                default: return new Color(0.9f, 0.9f, 0.95f);
            }
        }
    }
}
