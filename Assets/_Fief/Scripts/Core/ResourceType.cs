using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les trois ressources de la Saison. L'ordre compte : il sert d'index dans les
    /// tableaux (sac, caches, composition d'une relique).
    ///
    /// Renommees le 23/09/2026 avec le nouveau jeu (voir docs/LA-SAISON.md). Il y en
    /// a toujours trois, toujours dans des lieux fixes -- mais chacune est liee a un
    /// LIEU, et c'est ce qui fait voyager : la foret, les creux, le chateau.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>Bois mort : fagots au pied des arbres morts. Partout, leger, peu precieux.</summary>
        Deadwood = 0,
        /// <summary>Pierre-lune : elle luit dans les creux. Rare, lourde.</summary>
        Moonstone = 1,
        /// <summary>Fer ancien : dans les reserves du chateau, et nulle part ailleurs.</summary>
        Iron = 2
    }

    /// <summary>
    /// Donnees statiques attachees a chaque ressource. Une classe statique plutot
    /// qu'un ScriptableObject : zero asset a gerer.
    /// </summary>
    public static class ResourceInfo
    {
        public const int Count = 3;

        public static readonly ResourceType[] All =
        {
            ResourceType.Deadwood,
            ResourceType.Moonstone,
            ResourceType.Iron
        };

        public static string Name(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Deadwood: return "Bois mort";
                case ResourceType.Moonstone: return "Pierre-lune";
                case ResourceType.Iron: return "Fer ancien";
            }
            return "?";
        }

        /// <summary>
        /// Poids d'une unite, en kg. Le sac porte 60 kg : c'est le poids qui oblige a
        /// cacher, et c'est lui qui rend lent quand on traverse la foret pour le mage.
        /// </summary>
        public static float UnitWeight(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Deadwood: return 1.0f;
                case ResourceType.Moonstone: return 3.0f;
                case ResourceType.Iron: return 2.0f;
            }
            return 1f;
        }

        /// <summary>
        /// Ce que vaut une unite une fois fondue dans une relique. Le fer vaut dix fois
        /// le bois : il faut aller le chercher au chateau.
        /// </summary>
        public static int ForgeValue(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Deadwood: return 1;
                case ResourceType.Moonstone: return 4;
                case ResourceType.Iron: return 10;
            }
            return 0;
        }

        public static Color Tint(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Deadwood: return new Color(0.66f, 0.55f, 0.40f);
                case ResourceType.Moonstone: return new Color(0.62f, 0.78f, 0.95f);
                case ResourceType.Iron: return new Color(0.74f, 0.52f, 0.40f);
            }
            return Color.white;
        }
    }
}
