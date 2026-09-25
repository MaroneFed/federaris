using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA CARTE (touche M). Un parchemin de la Sylve, vue de dessus : le château au
    /// centre, les Autels, ta stèle, ton camp, tes caches, les lieux-dits que tu as
    /// trouvés, les stèles rivales que tu connais -- et toi, une flèche.
    ///
    /// Elle ne montre que ce que tu as PARCOURU : la Sylve est découpée en cases de
    /// 21 m, et une case se dévoile quand tu passes à côté. Au début, la carte est
    /// presque noire ; à la fin, c'est ton propre dessin de la forêt.
    ///
    /// Pourquoi (Martin, 26/09) : "on se perd complètement dans la map". La carte
    /// ne fait pas le chemin à ta place -- il n'y a pas de sentier dessus -- mais on
    /// sait toujours où l'on est.
    /// </summary>
    public static class Atlas
    {
        const int Cells = 20;
        static bool[] seen = new bool[Cells * Cells];
        static float size = 420f;

        public static void Reset(float mapSize)
        {
            size = mapSize;
            seen = new bool[Cells * Cells];
        }

        /// <summary>Dévoiler les cases autour de soi (rayon ~25 m).</summary>
        public static void Track(Vector3 p)
        {
            float cell = size / Cells;
            int cx = Mathf.FloorToInt((p.x + size * 0.5f) / cell);
            int cz = Mathf.FloorToInt((p.z + size * 0.5f) / cell);
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = cx + dx, z = cz + dz;
                    if (x < 0 || z < 0 || x >= Cells || z >= Cells) continue;
                    seen[z * Cells + x] = true;
                }
        }

        static Vector2 ToMap(Rect r, Vector3 world)
        {
            float u = (world.x + size * 0.5f) / size;
            float v = (world.z + size * 0.5f) / size;
            return new Vector2(r.x + u * r.width, r.yMax - v * r.height);
        }

        public static void Draw(Rect r)
        {
            // Le parchemin.
            UiStyle.Fill(r, new Color(0.62f, 0.54f, 0.4f));
            UiStyle.Fill(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f), new Color(0.7f, 0.62f, 0.47f));
            float cell = r.width / Cells;
            // La forêt (des taches plus sombres, fixes) puis le brouillard de ce qu'on n'a pas vu.
            for (int z = 0; z < Cells; z++)
                for (int x = 0; x < Cells; x++)
                {
                    Rect c = new Rect(r.x + x * cell, r.yMax - (z + 1) * cell, cell + 0.5f, cell + 0.5f);
                    float n = Mathf.PerlinNoise(x * 0.4f, z * 0.4f);
                    if (seen[z * Cells + x]) UiStyle.Fill(c, new Color(0.28f, 0.33f, 0.22f, 0.25f + n * 0.25f));
                    else UiStyle.Fill(c, new Color(0.14f, 0.12f, 0.1f, 0.93f));
                }

            // Le château : un carré de murs, toujours visible (on le devine de partout).
            Vector2 a = ToMap(r, new Vector3(-Castle.HalfSize, 0f, Castle.HalfSize));
            Vector2 b = ToMap(r, new Vector3(Castle.HalfSize, 0f, -Castle.HalfSize));
            Rect castle = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
            UiStyle.Fill(castle, new Color(0.36f, 0.33f, 0.3f));
            UiStyle.Fill(new Rect(castle.x + 3f, castle.y + 3f, castle.width - 6f, castle.height - 6f), new Color(0.55f, 0.5f, 0.42f));
            Label(r, Vector3.zero, "Château", new Color(0.2f, 0.16f, 0.12f), 0f);

            // Les Autels.
            for (int i = 0; i < Monument.All.Count; i++)
            {
                Monument m = Monument.All[i];
                if (m == null) continue;
                Color c = m.Owner != null ? m.Owner.Colour : Monument.Tint(m.kind);
                Mark(r, m.transform.position, UiStyle.Shape.Square, c, 11f);
            }

            // Les lieux-dits découverts.
            for (int i = 0; i < Landmarks.All.Count; i++)
            {
                Landmark lm = Landmarks.All[i];
                if (lm == null || !lm.Discovered) continue;
                Mark(r, lm.transform.position, UiStyle.Shape.Dot, new Color(0.3f, 0.25f, 0.2f), 9f);
                Label(r, lm.transform.position, Landmarks.Name(lm.kind), new Color(0.25f, 0.2f, 0.15f), 12f);
            }

            Seeker me = Game.Me;
            Hoard h = me != null ? me.Hoard : null;
            if (h != null)
            {
                if (h.CampPlanted) Mark(r, h.CampPosition, UiStyle.Shape.Triangle, new Color(0.3f, 0.55f, 0.25f), 13f);
                for (int i = 0; i < h.Caches.Count; i++) Mark(r, h.Caches[i].Position, UiStyle.Shape.Dot, new Color(0.55f, 0.35f, 0.18f), 9f);
                if (h.StelePlanted)
                {
                    Mark(r, h.StelePosition, UiStyle.Shape.Diamond, new Color(0.2f, 0.35f, 0.75f), 16f);
                    Label(r, h.StelePosition, "Ta stèle", new Color(0.15f, 0.25f, 0.55f), 14f);
                }
            }
            for (int i = 0; i < Stele.All.Count; i++)
            {
                Stele st = Stele.All[i];
                if (st == null || st.owner == null || st.owner == me || me == null || !me.Knows(st.owner)) continue;
                Mark(r, st.transform.position, UiStyle.Shape.Diamond, st.owner.Colour, 12f);
            }

            // Toi : une flèche qui pointe où tu regardes.
            Transform p = Game.PlayerTransform;
            if (p != null)
            {
                Vector2 at = ToMap(r, p.position);
                float yaw = Game.Hud != null && Game.Hud.viewCamera != null ? Game.Hud.viewCamera.transform.eulerAngles.y : p.eulerAngles.y;
                Matrix4x4 was = GUI.matrix;
                GUIUtility.RotateAroundPivot(yaw, at);
                float s = UiStyle.S(18);
                UiStyle.Icon(new Rect(at.x - s * 0.5f - 1f, at.y - s * 0.5f - 1f, s + 2f, s + 2f), UiStyle.Shape.Triangle, new Color(0f, 0f, 0f, 0.8f));
                UiStyle.Icon(new Rect(at.x - s * 0.5f, at.y - s * 0.5f, s, s), UiStyle.Shape.Triangle, new Color(0.9f, 0.3f, 0.2f));
                GUI.matrix = was;
            }

            // Le nord, en haut.
            UiStyle.Tinted(new Rect(r.x, r.y + 6f, r.width, UiStyle.S(24)), "N", UiStyle.Head, new Color(0.45f, 0.12f, 0.08f));
        }

        static void Mark(Rect r, Vector3 world, UiStyle.Shape shape, Color c, float px)
        {
            Vector2 at = ToMap(r, world);
            float s = UiStyle.S(px);
            UiStyle.Icon(new Rect(at.x - s * 0.5f - 1f, at.y - s * 0.5f - 1f, s + 2f, s + 2f), shape, new Color(0f, 0f, 0f, 0.6f));
            UiStyle.Icon(new Rect(at.x - s * 0.5f, at.y - s * 0.5f, s, s), shape, c);
        }

        static void Label(Rect r, Vector3 world, string text, Color c, float below)
        {
            Vector2 at = ToMap(r, world);
            UiStyle.Tinted(new Rect(at.x - UiStyle.S(80), at.y + UiStyle.S(below) - UiStyle.S(8), UiStyle.S(160), UiStyle.S(18)), text,
                           UiStyle.CenteredSmall, c);
        }
    }

    /// <summary>La carte, ouverte (M ou Échap pour la refermer).</summary>
    public class MapPanel : IPanel
    {
        public bool IsStillValid { get { return true; } }

        public void Draw()
        {
            float side = Mathf.Min(Screen.height - UiStyle.S(80), UiStyle.S(720));
            Rect frame = new Rect((Screen.width - side) * 0.5f - 10f, (Screen.height - side) * 0.5f - 10f, side + 20f, side + 20f);
            UiStyle.Frame(frame);
            Atlas.Draw(new Rect(frame.x + 10f, frame.y + 10f, side, side));
        }
    }
}
