using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Toutes les couleurs du jeu au meme endroit.
    /// Loi n1 du projet : look SIMPLE. Des aplats low-poly, pas de textures.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Sky = new Color(0.55f, 0.71f, 0.85f);
        public static readonly Color Grass = new Color(0.42f, 0.58f, 0.33f);
        public static readonly Color GrassDark = new Color(0.34f, 0.48f, 0.28f);
        public static readonly Color Dirt = new Color(0.52f, 0.43f, 0.31f);
        public static readonly Color Cliff = new Color(0.36f, 0.36f, 0.38f);

        public static readonly Color Wood = new Color(0.45f, 0.67f, 0.35f);
        public static readonly Color Trunk = new Color(0.40f, 0.29f, 0.19f);
        public static readonly Color Stone = new Color(0.62f, 0.63f, 0.66f);
        public static readonly Color Iron = new Color(0.72f, 0.45f, 0.36f);

        public static readonly Color Plaza = new Color(0.70f, 0.66f, 0.56f);
        public static readonly Color Canvas = new Color(0.85f, 0.79f, 0.66f);
        public static readonly Color Gold = new Color(0.95f, 0.78f, 0.28f);

        public static readonly Color PlotFree = new Color(0.63f, 0.56f, 0.42f);
        public static readonly Color Structure = new Color(0.78f, 0.73f, 0.62f);
        public static readonly Color Roof = new Color(0.55f, 0.28f, 0.24f);

        /// <summary>Couleur de banniere de chaque fief (6 joueurs cibles).</summary>
        public static readonly Color[] Banners =
        {
            new Color(0.79f, 0.24f, 0.24f),
            new Color(0.24f, 0.42f, 0.79f),
            new Color(0.30f, 0.65f, 0.35f),
            new Color(0.78f, 0.62f, 0.20f),
            new Color(0.55f, 0.32f, 0.72f),
            new Color(0.20f, 0.66f, 0.68f)
        };

        public static Color Banner(int fiefIndex)
        {
            if (Banners.Length == 0) return Color.white;
            int i = fiefIndex % Banners.Length;
            if (i < 0) i += Banners.Length;
            return Banners[i];
        }

        public static Color Shade(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }
    }
}
