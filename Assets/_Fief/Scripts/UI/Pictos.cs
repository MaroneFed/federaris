using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES PICTOGRAMMES de l'inventaire : une hache, une epee, un faisceau de bois,
    /// un eclat de pierre-lune, deux lingots, une piece, une relique. Pas d'image a
    /// importer : chacun est dessine avec deux ou trois barres tournees et les
    /// formes d'UiStyle (point, losange). Lisibles a 40 pixels, dans le style plat
    /// du reste de l'interface.
    ///
    /// Le truc : GUIUtility.RotateAroundPivot tourne tout ce qu'on dessine ensuite
    /// autour d'un point. On tourne, on dessine un rectangle, on remet la matrice.
    /// </summary>
    public static class Pictos
    {
        public enum Kind { Bois, Pierre, Fer, Or, Hache, Epee, Piege, Relique }

        public static Kind Of(ResourceType t)
        {
            return t == ResourceType.Deadwood ? Kind.Bois : t == ResourceType.Moonstone ? Kind.Pierre : Kind.Fer;
        }

        public static Kind Of(ToolKind t)
        {
            return t == ToolKind.Epee ? Kind.Epee : t == ToolKind.Piege ? Kind.Piege : Kind.Hache;
        }

        /// <summary>Le pictogramme d'une ressource suivi de son nom, dans une ligne de panneau.</summary>
        public static void Named(Rect r, ResourceType type, GUIStyle style, Color color)
        {
            float d = Mathf.Min(r.height + UiStyle.S(4), UiStyle.S(28));
            Draw(new Rect(r.x, r.center.y - d * 0.5f, d, d), Of(type), color.a < 0.9f || color == UiStyle.InkFaint);
            UiStyle.Tinted(new Rect(r.x + d + UiStyle.S(6), r.y, r.width - d - UiStyle.S(6), r.height), ResourceInfo.Name(type), style, color);
        }

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
            float d = s * r.width;
            UiStyle.Icon(new Rect(r.x + cx * r.width - d * 0.5f, r.y + cy * r.height - d * 0.5f, d, d), shape, c);
        }

        /// <summary>Dessine le pictogramme dans r ; faded le rend pale (case vide).</summary>
        public static void Draw(Rect r, Kind kind, bool faded)
        {
            float a = faded ? 0.28f : 1f;
            switch (kind)
            {
                case Kind.Bois:
                {
                    Color pale = new Color(0.82f, 0.77f, 0.66f, a), mid = new Color(0.66f, 0.6f, 0.5f, a);
                    Bar(r, 0.42f, 0.52f, 0.78f, 0.1f, -38f, mid);
                    Bar(r, 0.52f, 0.48f, 0.8f, 0.11f, -38f, pale);
                    Bar(r, 0.6f, 0.56f, 0.7f, 0.1f, -38f, mid);
                    Bar(r, 0.5f, 0.52f, 0.1f, 0.4f, -38f, new Color(0.45f, 0.36f, 0.24f, a));   // le lien
                    break;
                }
                case Kind.Pierre:
                {
                    Shape(r, 0.5f, 0.5f, 0.78f, UiStyle.Shape.Dot, new Color(0.45f, 0.65f, 1f, 0.18f * a));
                    Shape(r, 0.5f, 0.52f, 0.56f, UiStyle.Shape.Diamond, new Color(0.45f, 0.62f, 0.9f, a));
                    Shape(r, 0.45f, 0.45f, 0.26f, UiStyle.Shape.Diamond, new Color(0.8f, 0.9f, 1f, a));
                    break;
                }
                case Kind.Fer:
                {
                    Color dark = new Color(0.36f, 0.35f, 0.34f, a), lit = new Color(0.62f, 0.6f, 0.58f, a);
                    Bar(r, 0.44f, 0.62f, 0.56f, 0.2f, 0f, dark);
                    Bar(r, 0.44f, 0.54f, 0.5f, 0.05f, 0f, lit);
                    Bar(r, 0.58f, 0.42f, 0.56f, 0.2f, 0f, dark);
                    Bar(r, 0.58f, 0.34f, 0.5f, 0.05f, 0f, lit);
                    break;
                }
                case Kind.Or:
                {
                    Shape(r, 0.5f, 0.5f, 0.6f, UiStyle.Shape.Dot, new Color(0.95f, 0.76f, 0.3f, a));
                    Shape(r, 0.5f, 0.5f, 0.38f, UiStyle.Shape.Dot, new Color(0.72f, 0.52f, 0.18f, a));
                    Shape(r, 0.42f, 0.42f, 0.14f, UiStyle.Shape.Dot, new Color(1f, 0.95f, 0.75f, a));
                    break;
                }
                case Kind.Hache:
                {
                    Bar(r, 0.5f, 0.52f, 0.8f, 0.08f, -50f, new Color(0.55f, 0.4f, 0.24f, a));      // le manche
                    Bar(r, 0.64f, 0.34f, 0.18f, 0.34f, -50f, new Color(0.7f, 0.72f, 0.75f, a));   // le fer
                    Bar(r, 0.71f, 0.28f, 0.05f, 0.36f, -50f, new Color(0.9f, 0.92f, 0.95f, a));   // le fil
                    break;
                }
                case Kind.Epee:
                {
                    Bar(r, 0.56f, 0.44f, 0.72f, 0.09f, -45f, new Color(0.8f, 0.82f, 0.86f, a));    // la lame
                    Bar(r, 0.33f, 0.67f, 0.07f, 0.36f, -45f, new Color(0.86f, 0.68f, 0.3f, a));    // la garde
                    Bar(r, 0.25f, 0.75f, 0.18f, 0.08f, -45f, new Color(0.45f, 0.3f, 0.2f, a));     // la poignee
                    break;
                }
                case Kind.Piege:
                {
                    Shape(r, 0.5f, 0.55f, 0.66f, UiStyle.Shape.Dot, new Color(0.3f, 0.29f, 0.28f, a));
                    Shape(r, 0.5f, 0.55f, 0.44f, UiStyle.Shape.Dot, new Color(0.08f, 0.07f, 0.06f, a));
                    for (int i = 0; i < 6; i++)
                        Bar(r, 0.5f + Mathf.Cos(i * 1.047f) * 0.24f, 0.55f + Mathf.Sin(i * 1.047f) * 0.24f, 0.05f, 0.14f,
                            i * 60f + 90f, new Color(0.75f, 0.73f, 0.7f, a));
                    break;
                }
                case Kind.Relique:
                {
                    Shape(r, 0.5f, 0.5f, 0.86f, UiStyle.Shape.Dot, new Color(1f, 0.8f, 0.4f, 0.16f * a));
                    Shape(r, 0.5f, 0.5f, 0.62f, UiStyle.Shape.Diamond, new Color(0.93f, 0.76f, 0.38f, a));
                    Shape(r, 0.5f, 0.5f, 0.3f, UiStyle.Shape.Diamond, new Color(0.55f, 0.75f, 1f, a));
                    break;
                }
            }
        }
    }
}
