using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES PICTOGRAMMES : les objets de la foret, l'epee, la Couronne, et les dix
    /// pouvoirs. Pas d'image a importer : chacun est dessine avec deux ou trois barres
    /// tournees et les formes d'UiStyle (point, losange, triangle). Lisibles a 40
    /// pixels, dans le style plat du reste de l'interface.
    ///
    /// Le truc : GUIUtility.RotateAroundPivot tourne tout ce qu'on dessine ensuite
    /// autour d'un point. On tourne, on dessine un rectangle, on remet la matrice.
    /// Toutes les positions sont en FRACTIONS de la case (0 a 1) : un pictogramme
    /// se dessine a n'importe quelle taille.
    /// </summary>
    public static class Pictos
    {
        // ================================================================== outils de dessin

        /// <summary>Une barre de longueur l et d'epaisseur e (en fraction de la case), tournee de angle degres.</summary>
        static void Bar(Rect r, float cx, float cy, float l, float e, float angle, Color c)
        {
            Vector2 at = new Vector2(r.x + cx * r.width, r.y + cy * r.height);
            float w = l * r.width, h = e * r.height;
            Matrix4x4 was = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, at);
            UiStyle.Fill(new Rect(at.x - w * 0.5f, at.y - h * 0.5f, w, h), c);
            GUI.matrix = was;
        }

        static void Shape(Rect r, float cx, float cy, float s, UiStyle.Shape shape, Color c)
        {
            Shape(r, cx, cy, s, s, shape, c, 0f);
        }

        /// <summary>Une forme etiree (sx, sy) et tournee. Le triangle d'UiStyle pointe vers le BAS.</summary>
        static void Shape(Rect r, float cx, float cy, float sx, float sy, UiStyle.Shape shape, Color c, float angle)
        {
            Vector2 at = new Vector2(r.x + cx * r.width, r.y + cy * r.height);
            float w = sx * r.width, h = sy * r.height;
            Matrix4x4 was = GUI.matrix;
            if (angle != 0f) GUIUtility.RotateAroundPivot(angle, at);
            UiStyle.Icon(new Rect(at.x - w * 0.5f, at.y - h * 0.5f, w, h), shape, c);
            GUI.matrix = was;
        }

        static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, c.a * a); }

        // ================================================================== les objets

        /// <summary>Dessine l'objet dans r ; faded le rend pale (case vide, objet indisponible).</summary>
        public static void Draw(Rect r, Item item, bool faded)
        {
            float a = faded ? 0.3f : 1f;
            Color tint = A(ItemInfo.Tint(item), a);
            Color dark = A(new Color(0.16f, 0.14f, 0.12f), a);
            Color wood = A(new Color(0.55f, 0.4f, 0.24f), a);
            Color steel = A(new Color(0.78f, 0.8f, 0.84f), a);
            switch (item)
            {
                case Item.Detecteur:
                    Bar(r, 0.46f, 0.44f, 0.72f, 0.07f, -55f, steel);                          // la canne
                    Shape(r, 0.3f, 0.78f, 0.42f, 0.16f, UiStyle.Shape.Dot, A(new Color(0.3f, 0.32f, 0.35f), a), 0f);   // le disque
                    Shape(r, 0.3f, 0.77f, 0.3f, 0.09f, UiStyle.Shape.Dot, A(new Color(0.55f, 0.95f, 0.6f), a), 0f);    // sa lueur
                    Bar(r, 0.68f, 0.16f, 0.2f, 0.09f, -55f, dark);                            // la poignee
                    Shape(r, 0.78f, 0.34f, 0.12f, UiStyle.Shape.Dot, A(new Color(0.55f, 1f, 0.6f), a));             // le voyant
                    break;
                case Item.Pelle:
                    Bar(r, 0.58f, 0.4f, 0.7f, 0.08f, 45f, wood);                              // le manche
                    Bar(r, 0.8f, 0.18f, 0.22f, 0.08f, -45f, dark);                            // la poignee en T
                    Shape(r, 0.28f, 0.72f, 0.3f, 0.36f, UiStyle.Shape.Square, steel, 45f);    // la lame
                    Shape(r, 0.22f, 0.78f, 0.2f, UiStyle.Shape.Diamond, A(new Color(0.9f, 0.92f, 0.95f), a));
                    break;
                case Item.Fumigene:
                    Shape(r, 0.36f, 0.42f, 0.42f, UiStyle.Shape.Dot, A(new Color(0.55f, 0.57f, 0.6f), a));
                    Shape(r, 0.6f, 0.36f, 0.46f, UiStyle.Shape.Dot, A(new Color(0.68f, 0.7f, 0.73f), a));
                    Shape(r, 0.52f, 0.58f, 0.44f, UiStyle.Shape.Dot, A(new Color(0.62f, 0.64f, 0.67f), a));
                    Shape(r, 0.3f, 0.78f, 0.26f, UiStyle.Shape.Dot, dark);                   // la boule
                    Bar(r, 0.38f, 0.66f, 0.04f, 0.12f, 30f, A(new Color(1f, 0.6f, 0.2f), a)); // la meche
                    break;
                case Item.Lenteur:
                case Item.Elixir:
                    Shape(r, 0.5f, 0.64f, 0.56f, UiStyle.Shape.Dot, tint);                   // la panse
                    Shape(r, 0.42f, 0.56f, 0.16f, UiStyle.Shape.Dot, A(Color.white, 0.4f * a));
                    Bar(r, 0.5f, 0.3f, 0.18f, 0.2f, 0f, A(new Color(0.8f, 0.85f, 0.9f), 0.8f * a));  // le col
                    Bar(r, 0.5f, 0.17f, 0.22f, 0.08f, 0f, wood);                              // le bouchon
                    break;
                case Item.Piege:
                    Shape(r, 0.5f, 0.55f, 0.66f, UiStyle.Shape.Dot, A(new Color(0.3f, 0.29f, 0.28f), a));
                    Shape(r, 0.5f, 0.55f, 0.44f, UiStyle.Shape.Dot, A(new Color(0.08f, 0.07f, 0.06f), a));
                    for (int i = 0; i < 6; i++)
                        Bar(r, 0.5f + Mathf.Cos(i * 1.047f) * 0.24f, 0.55f + Mathf.Sin(i * 1.047f) * 0.24f, 0.05f, 0.14f,
                            i * 60f + 90f, A(new Color(0.75f, 0.73f, 0.7f), a));
                    break;
                case Item.Plume:
                    Bar(r, 0.5f, 0.5f, 0.84f, 0.04f, -50f, A(new Color(0.92f, 0.95f, 1f), a));   // le rachis
                    for (int i = 0; i < 6; i++)
                    {
                        float u = 0.2f + i * 0.1f;
                        float x = 0.5f + (u - 0.5f) * 0.64f, y = 0.5f - (u - 0.5f) * 0.77f;
                        Bar(r, x - 0.06f, y - 0.06f, 0.2f, 0.06f, -10f, tint);
                        Bar(r, x + 0.06f, y + 0.06f, 0.2f, 0.06f, -90f, tint);
                    }
                    break;
                case Item.CapeOmbre:
                    Shape(r, 0.5f, 0.6f, 0.76f, 0.7f, UiStyle.Shape.Triangle, tint, 180f);   // la cape
                    Shape(r, 0.5f, 0.28f, 0.3f, UiStyle.Shape.Dot, A(Palette.Shade(ItemInfo.Tint(item), 0.6f), a));  // le capuchon
                    Shape(r, 0.5f, 0.3f, 0.14f, UiStyle.Shape.Dot, A(new Color(0.04f, 0.03f, 0.06f), a));
                    break;
                case Item.Cle:
                    Shape(r, 0.3f, 0.34f, 0.36f, UiStyle.Shape.Dot, tint);                   // l'anneau
                    Shape(r, 0.3f, 0.34f, 0.16f, UiStyle.Shape.Dot, A(new Color(0.1f, 0.08f, 0.06f), a));
                    Bar(r, 0.58f, 0.6f, 0.56f, 0.08f, 45f, tint);                             // la tige
                    Bar(r, 0.72f, 0.66f, 0.06f, 0.16f, 45f, tint);                            // les dents
                    Bar(r, 0.8f, 0.74f, 0.06f, 0.12f, 45f, tint);
                    break;
                default:
                    Sword(r, a);
                    break;
            }
        }

        /// <summary>L'epee : lame, garde, poignee.</summary>
        public static void Sword(Rect r, float a)
        {
            Bar(r, 0.56f, 0.44f, 0.72f, 0.09f, -45f, new Color(0.8f, 0.82f, 0.86f, a));    // la lame
            Bar(r, 0.33f, 0.67f, 0.07f, 0.36f, -45f, new Color(0.86f, 0.68f, 0.3f, a));    // la garde
            Bar(r, 0.25f, 0.75f, 0.18f, 0.08f, -45f, new Color(0.45f, 0.3f, 0.2f, a));     // la poignee
        }

        /// <summary>La Couronne : un bandeau, trois fleurons, des joyaux.</summary>
        public static void Crown(Rect r, float a)
        {
            Color gold = new Color(0.95f, 0.76f, 0.3f, a);
            Bar(r, 0.5f, 0.72f, 0.74f, 0.16f, 0f, gold);
            for (int i = 0; i < 3; i++)
                Shape(r, 0.25f + i * 0.25f, 0.46f - (i == 1 ? 0.06f : 0f), 0.24f, 0.4f, UiStyle.Shape.Triangle, gold, 180f);
            Shape(r, 0.5f, 0.72f, 0.12f, UiStyle.Shape.Diamond, new Color(0.9f, 0.2f, 0.22f, a));
            Shape(r, 0.28f, 0.72f, 0.09f, UiStyle.Shape.Diamond, new Color(0.35f, 0.5f, 1f, a));
            Shape(r, 0.72f, 0.72f, 0.09f, UiStyle.Shape.Diamond, new Color(0.35f, 0.5f, 1f, a));
        }

        // ================================================================== les pouvoirs

        /// <summary>Le pictogramme d'un pouvoir, dans sa couleur.</summary>
        public static void Draw(Rect r, Power p, bool faded)
        {
            float a = faded ? 0.3f : 1f;
            Color c = A(PowerInfo.Tint(p), a);
            Color pale = A(Color.Lerp(PowerInfo.Tint(p), Color.white, 0.5f), a);
            Color dark = A(new Color(0.08f, 0.07f, 0.06f), a);
            switch (p)
            {
                case Power.DoubleSaut:
                    Shape(r, 0.5f, 0.3f, 0.5f, 0.3f, UiStyle.Shape.Triangle, pale, 180f);
                    Shape(r, 0.5f, 0.56f, 0.5f, 0.3f, UiStyle.Shape.Triangle, c, 180f);
                    Bar(r, 0.5f, 0.86f, 0.6f, 0.06f, 0f, c);
                    break;
                case Power.Ruee:
                    for (int i = 0; i < 3; i++) Bar(r, 0.22f + i * 0.06f, 0.36f + i * 0.14f, 0.3f - i * 0.06f, 0.06f, 0f, A(c, 0.5f + i * 0.2f));
                    Shape(r, 0.66f, 0.5f, 0.42f, 0.5f, UiStyle.Shape.Triangle, c, -90f);
                    break;
                case Power.Coureur:
                    for (int i = 0; i < 3; i++) Bar(r, 0.34f, 0.3f + i * 0.2f, 0.34f - i * 0.08f, 0.07f, 0f, A(c, 0.9f - i * 0.2f));
                    Shape(r, 0.7f, 0.5f, 0.3f, UiStyle.Shape.Dot, c);
                    break;
                case Power.Poigne:
                    Shape(r, 0.44f, 0.54f, 0.44f, 0.4f, UiStyle.Shape.Square, c, 0f);                 // le poing
                    for (int i = 0; i < 4; i++) Bar(r, 0.3f + i * 0.095f, 0.34f, 0.08f, 0.12f, 0f, pale);  // les phalanges
                    for (int i = -1; i <= 1; i++) Bar(r, 0.82f, 0.54f + i * 0.16f, 0.16f, 0.05f, i * 25f, pale);   // le choc
                    break;
                case Power.Colosse:
                    Heart(r, c);
                    Bar(r, 0.5f, 0.5f, 0.3f, 0.08f, 0f, dark);
                    Bar(r, 0.5f, 0.5f, 0.08f, 0.3f, 0f, dark);
                    break;
                case Power.Ombre:
                    Shape(r, 0.5f, 0.5f, 0.8f, 0.42f, UiStyle.Shape.Diamond, c, 0f);
                    Shape(r, 0.5f, 0.5f, 0.2f, UiStyle.Shape.Dot, dark);
                    Bar(r, 0.5f, 0.5f, 0.86f, 0.07f, -40f, pale);
                    break;
                case Power.Flair:
                    for (int i = 0; i < 8; i++) Bar(r, 0.5f + Mathf.Cos(i * 0.785f) * 0.36f, 0.5f + Mathf.Sin(i * 0.785f) * 0.36f, 0.14f, 0.04f, i * 45f, A(c, 0.7f));
                    Shape(r, 0.5f, 0.5f, 0.62f, 0.34f, UiStyle.Shape.Diamond, c, 0f);
                    Shape(r, 0.5f, 0.5f, 0.18f, UiStyle.Shape.Dot, dark);
                    break;
                case Power.Porteur:
                    Crown(new Rect(r.x + r.width * 0.15f, r.y + r.height * 0.3f, r.width * 0.7f, r.height * 0.7f), a);
                    Shape(r, 0.5f, 0.18f, 0.32f, 0.22f, UiStyle.Shape.Triangle, pale, 180f);
                    break;
                case Power.SangVif:
                    Heart(r, c);
                    for (int i = 0; i < 3; i++) Shape(r, 0.3f + i * 0.2f, 0.12f + (i % 2) * 0.06f, 0.1f, UiStyle.Shape.Diamond, pale);
                    break;
                default:    // Seconde chance : un cercle de points qui se referme sur une fleche
                    for (int i = 0; i < 7; i++)
                    {
                        float ang = (i / 8f) * Mathf.PI * 2f - Mathf.PI * 0.5f;
                        Shape(r, 0.5f + Mathf.Cos(ang) * 0.3f, 0.52f + Mathf.Sin(ang) * 0.3f, 0.13f, UiStyle.Shape.Dot, A(c, 0.5f + i * 0.07f));
                    }
                    Shape(r, 0.3f, 0.3f, 0.24f, 0.2f, UiStyle.Shape.Triangle, c, -45f);
                    Shape(r, 0.5f, 0.52f, 0.2f, UiStyle.Shape.Dot, pale);
                    break;
            }
        }

        /// <summary>Un coeur : deux ronds et un losange.</summary>
        static void Heart(Rect r, Color c)
        {
            Shape(r, 0.36f, 0.38f, 0.4f, UiStyle.Shape.Dot, c);
            Shape(r, 0.64f, 0.38f, 0.4f, UiStyle.Shape.Dot, c);
            Shape(r, 0.5f, 0.58f, 0.6f, 0.6f, UiStyle.Shape.Diamond, c, 0f);
        }
    }
}
