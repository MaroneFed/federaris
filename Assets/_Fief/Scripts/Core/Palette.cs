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

        // --- les 9 teintes du terrain a facettes, du rivage au sommet enneige
        public static readonly Color Sand = new Color(0.79f, 0.71f, 0.54f);
        public static readonly Color Grass1 = new Color(0.50f, 0.66f, 0.31f);
        public static readonly Color Grass2 = new Color(0.43f, 0.59f, 0.28f);
        public static readonly Color Grass3 = new Color(0.36f, 0.51f, 0.25f);
        public static readonly Color Grass4 = new Color(0.29f, 0.43f, 0.22f);
        public static readonly Color Scree = new Color(0.54f, 0.48f, 0.36f);
        public static readonly Color Rock1 = new Color(0.48f, 0.48f, 0.51f);
        public static readonly Color Rock2 = new Color(0.37f, 0.37f, 0.41f);
        public static readonly Color Snow = new Color(0.91f, 0.93f, 0.96f);
        public static readonly Color Water = new Color(0.20f, 0.44f, 0.56f, 0.80f);
        public static readonly Color WaterDeep = new Color(0.12f, 0.29f, 0.42f, 0.88f);

        public static readonly Color Wood = new Color(0.45f, 0.67f, 0.35f);
        public static readonly Color Trunk = new Color(0.40f, 0.29f, 0.19f);
        public static readonly Color Stone = new Color(0.62f, 0.63f, 0.66f);
        public static readonly Color Iron = new Color(0.72f, 0.45f, 0.36f);

        public static readonly Color Path = new Color(0.60f, 0.50f, 0.36f);
        public static readonly Color Flower1 = new Color(0.93f, 0.86f, 0.36f);
        public static readonly Color Flower2 = new Color(0.86f, 0.44f, 0.52f);
        public static readonly Color Flower3 = new Color(0.62f, 0.55f, 0.88f);

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

        // ------------------------------------------------------------------
        //  NUANCIERS FIXES
        //
        //  Le decor est fusionne par couleur (voir Batcher.cs) : une couleur =
        //  un maillage + un materiau. Tirer une teinte continue au hasard pour
        //  chaque arbre creait donc un maillage PAR ARBRE - 17 600 au lieu de
        //  quelques centaines, et le jeu bloquait au lancement.
        //
        //  On pioche desormais dans des nuanciers de 3 a 5 teintes. La foret
        //  reste variee a l'oeil, et tout se regroupe proprement.
        // ------------------------------------------------------------------

        public static readonly Color[] Barks =
        {
            new Color(0.34f, 0.25f, 0.17f), new Color(0.40f, 0.29f, 0.19f),
            new Color(0.29f, 0.21f, 0.15f), new Color(0.45f, 0.34f, 0.23f)
        };

        public static readonly Color[] Needles =
        {
            new Color(0.20f, 0.33f, 0.18f), new Color(0.24f, 0.38f, 0.20f),
            new Color(0.17f, 0.28f, 0.16f), new Color(0.27f, 0.42f, 0.22f)
        };

        public static readonly Color[] Leaves =
        {
            new Color(0.33f, 0.48f, 0.24f), new Color(0.38f, 0.54f, 0.27f),
            new Color(0.29f, 0.43f, 0.22f), new Color(0.43f, 0.58f, 0.30f),
            new Color(0.35f, 0.50f, 0.20f)
        };

        public static readonly Color[] PaleLeaves =
        {
            new Color(0.50f, 0.65f, 0.34f), new Color(0.56f, 0.70f, 0.38f),
            new Color(0.46f, 0.60f, 0.31f), new Color(0.61f, 0.73f, 0.42f)
        };

        public static readonly Color[] BushLeaves =
        {
            new Color(0.26f, 0.40f, 0.20f), new Color(0.31f, 0.45f, 0.23f),
            new Color(0.22f, 0.35f, 0.18f), new Color(0.36f, 0.50f, 0.26f)
        };

        public static readonly Color[] Fronds =
        {
            new Color(0.29f, 0.44f, 0.22f), new Color(0.34f, 0.50f, 0.25f),
            new Color(0.25f, 0.39f, 0.20f)
        };

        public static readonly Color[] GrassTones =
        {
            new Color(0.50f, 0.66f, 0.31f), new Color(0.56f, 0.71f, 0.35f),
            new Color(0.45f, 0.60f, 0.28f), new Color(0.61f, 0.75f, 0.39f)
        };

        public static readonly Color[] Rocks =
        {
            new Color(0.40f, 0.40f, 0.43f), new Color(0.48f, 0.48f, 0.51f),
            new Color(0.34f, 0.34f, 0.38f), new Color(0.54f, 0.54f, 0.57f),
            new Color(0.44f, 0.43f, 0.40f)
        };

        public static readonly Color[] DeadWood =
        {
            new Color(0.31f, 0.24f, 0.18f), new Color(0.37f, 0.29f, 0.21f),
            new Color(0.26f, 0.20f, 0.15f)
        };

        public static Color Pick(Color[] set, System.Random rng)
        {
            if (set == null || set.Length == 0) return Color.white;
            return set[rng.Next(set.Length)];
        }

        /// <summary>
        /// Ramene une couleur sur une grille. Filet de securite : meme si du code
        /// futur fabrique des teintes continues, elles se regroupent quand meme.
        /// </summary>
        public static Color Quantize(Color c, int steps)
        {
            float s = Mathf.Max(2, steps);
            return new Color(Mathf.Round(c.r * s) / s,
                             Mathf.Round(c.g * s) / s,
                             Mathf.Round(c.b * s) / s, c.a);
        }

        public static Color Shade(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }
    }
}
