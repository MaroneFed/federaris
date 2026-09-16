using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les 3 ressources de la v1. L'ordre compte : il sert d'index dans les tableaux
    /// (inventaire, stock du marche, coffre du fief).
    /// </summary>
    public enum ResourceType
    {
        Wood = 0,
        Stone = 1,
        Iron = 2
    }

    /// <summary>
    /// Donnees statiques attachees a chaque ressource (nom affiche, couleur, poids unitaire).
    /// Une classe statique plutot qu'un ScriptableObject : zero asset a gerer pour la Phase 1.
    /// </summary>
    public static class ResourceInfo
    {
        public const int Count = 3;

        public static readonly ResourceType[] All =
        {
            ResourceType.Wood,
            ResourceType.Stone,
            ResourceType.Iron
        };

        public static string Name(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return "Bois";
                case ResourceType.Stone: return "Pierre";
                case ResourceType.Iron: return "Fer";
            }
            return "?";
        }

        /// <summary>Poids d'une unite, en kg. C'est ce qui rend le Fer precieux mais lourd.</summary>
        public static float UnitWeight(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return 1.0f;
                case ResourceType.Stone: return 2.0f;
                case ResourceType.Iron: return 3.0f;
            }
            return 1f;
        }

        public static Color Tint(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return Palette.Wood;
                case ResourceType.Stone: return Palette.Stone;
                case ResourceType.Iron: return Palette.Iron;
            }
            return Color.white;
        }
    }
}
