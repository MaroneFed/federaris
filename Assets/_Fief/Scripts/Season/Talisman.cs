using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES TALISMANS : six objets uniques, caches dans la sylve et dans le chateau.
    ///
    /// Ce ne sont PAS des ressources (la regle "trois ressources" tient) : on ne les
    /// recolte pas, on ne les forge pas, ils ne pesent rien. On les TROUVE, une fois
    /// par Saison, et chacun change une chose, franchement :
    ///
    ///   Lanterne ardente   sur le trone          ta lanterne porte bien plus loin
    ///   Corne d'appel      le Grand Chene        la boussole montre le mage
    ///   Coeur de lune      le Cercle de pierres  chaque pierre-lune en donne deux
    ///   Besace ciree       la cabane du braconnier  +15 kg dans le sac
    ///   Pelle d'os         le Tertre             creuser 3x plus vite, une cache de plus
    ///   Couronne sans tete l'allee des rois      la relique posee vaut 15 % de plus
    ///
    /// Pourquoi c'est bon pour le jeu : la foret cesse d'etre un decor uniforme.
    /// Il y a des LIEUX, et une raison d'aller les voir entre deux apparitions du
    /// mage. Le temps mort devient de l'exploration.
    ///
    /// Classe C# pure : les noms, les textes, les effets chiffres. Les objets du
    /// monde (TalismanPickup) lisent ceci et demandent a Hoard.TryTakeTalisman.
    /// </summary>
    public enum Talisman
    {
        Lanterne = 0,
        Corne = 1,
        Coeur = 2,
        Besace = 3,
        Pelle = 4,
        Couronne = 5
    }

    public static class TalismanInfo
    {
        public const int Count = 6;

        public const float LanternRange = 21f;
        public const float LanternBoost = 1.35f;
        public const float BesaceKilos = 15f;
        public const float PelleSpeed = 3f;
        public const float CouronneBonus = 1.15f;

        public static readonly Talisman[] All =
        {
            Talisman.Lanterne, Talisman.Corne, Talisman.Coeur,
            Talisman.Besace, Talisman.Pelle, Talisman.Couronne
        };

        public static string Name(Talisman t)
        {
            switch (t)
            {
                case Talisman.Lanterne: return "Lanterne ardente";
                case Talisman.Corne: return "Corne d'appel";
                case Talisman.Coeur: return "Coeur de lune";
                case Talisman.Besace: return "Besace ciree";
                case Talisman.Pelle: return "Pelle d'os";
                default: return "Couronne sans tete";
            }
        }

        /// <summary>Ce qu'il fait, en une ligne. C'est ce que le joueur retient.</summary>
        public static string Effect(Talisman t)
        {
            switch (t)
            {
                case Talisman.Lanterne: return "Ta lanterne eclaire bien plus loin.";
                case Talisman.Corne: return "Quand le mage chante, ta boussole le montre.";
                case Talisman.Coeur: return "Chaque pierre-lune ramassee en donne deux.";
                case Talisman.Besace: return "Ton sac porte " + Mathf.RoundToInt(BesaceKilos) + " kg de plus.";
                case Talisman.Pelle: return "Tu creuses trois fois plus vite, et une cache de plus.";
                default: return "Ta relique posee sur la stele vaut 15 % de plus.";
            }
        }

        /// <summary>Une phrase d'histoire. Le monde a existe avant toi.</summary>
        public static string Lore(Talisman t)
        {
            switch (t)
            {
                case Talisman.Lanterne: return "Le dernier roi ne dormait qu'avec elle allumee.";
                case Talisman.Corne: return "Taillee dans la corne d'un cerf que personne n'a jamais vu.";
                case Talisman.Coeur: return "Les pierres du cercle la gardaient. Elles ont cesse.";
                case Talisman.Besace: return "Le braconnier est parti sans elle. Il n'est pas revenu la chercher.";
                case Talisman.Pelle: return "Ceux du tertre l'ont laissee pour qu'on les enterre bien.";
                default: return "Il l'avait encore quand ils lui ont pris la tete.";
            }
        }

        /// <summary>Ou il se trouve : dit par le Veilleur, montre a l'ecran de fin.</summary>
        public static string Where(Talisman t)
        {
            switch (t)
            {
                case Talisman.Lanterne: return "sur le trone, dans le donjon";
                case Talisman.Corne: return "dans le creux du Grand Chene";
                case Talisman.Coeur: return "sur l'autel du Cercle de pierres";
                case Talisman.Besace: return "dans la cabane du braconnier";
                case Talisman.Pelle: return "a l'entree du Tertre";
                default: return "aux pieds du roi sans tete, dans l'allee";
            }
        }

        public static Color Tint(Talisman t)
        {
            switch (t)
            {
                case Talisman.Lanterne: return new Color(1f, 0.66f, 0.3f);
                case Talisman.Corne: return new Color(0.92f, 0.86f, 0.7f);
                case Talisman.Coeur: return new Color(0.55f, 0.75f, 1f);
                case Talisman.Besace: return new Color(0.72f, 0.56f, 0.36f);
                case Talisman.Pelle: return new Color(0.88f, 0.86f, 0.8f);
                default: return new Color(0.95f, 0.78f, 0.32f);
            }
        }
    }
}
