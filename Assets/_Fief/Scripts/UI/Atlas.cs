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

        static bool castleShown;

        public static void Reset(float mapSize)
        {
            size = mapSize;
            seen = new bool[Cells * Cells];
            castleShown = false;
        }

        static void Reveal(Vector3 p, int radius)
        {
            float cell = size / Cells;
            int cx = Mathf.FloorToInt((p.x + size * 0.5f) / cell);
            int cz = Mathf.FloorToInt((p.z + size * 0.5f) / cell);
            for (int dz = -radius; dz <= radius; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx, z = cz + dz;
                    if (x < 0 || z < 0 || x >= Cells || z >= Cells) continue;
                    seen[z * Cells + x] = true;
                }
        }

        /// <summary>Dévoiler les cases autour de soi (rayon ~25 m).</summary>
        public static void Track(Vector3 p) { Track(p, 1); }

        /// <summary>Devoiler autour de soi, sur "radius" cases (plus quand on est en hauteur).</summary>
        public static void Track(Vector3 p, int radius)
        {
            // Le chateau, on le connait avant d'y etre alle : on le voit depassant des
            // arbres, et tout le monde en parle. Ses abords sont dessines d'office.
            if (!castleShown)
            {
                castleShown = true;
                Reveal(Vector3.zero, Mathf.CeilToInt((Castle.HalfSize + 20f) / (size / Cells)));
            }
            Reveal(p, radius);
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
                for (int i = 0; i < h.Caches.Count; i++)
                {
                    Mark(r, h.Caches[i].Position, UiStyle.Shape.Dot, new Color(0.55f, 0.35f, 0.18f), 9f);
                    Label(r, h.Caches[i].Position, h.Caches[i].Number.ToString(), new Color(0.35f, 0.2f, 0.1f), 11f);
                }
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

            // Les geants : on les voit depasser de la canopee, ils sont toujours sur la carte.
            for (int i = 0; i < Forest.GiantSpots.Count; i++)
                Mark(r, Forest.GiantSpots[i], UiStyle.Shape.Triangle, new Color(0.18f, 0.3f, 0.16f), 10f);

            // Tes pieges : de petites croix (toi seul sais ou ils sont).
            for (int i = 0; i < Trap.All.Count; i++)
            {
                Trap t = Trap.All[i];
                if (t == null || t.owner != me || t.Sprung) continue;
                Vector2 at = ToMap(r, t.transform.position);
                float s = UiStyle.S(4);
                UiStyle.Fill(new Rect(at.x - s, at.y - 1f, s * 2f, 2f), new Color(0.35f, 0.12f, 0.08f));
                UiStyle.Fill(new Rect(at.x - 1f, at.y - s, 2f, s * 2f), new Color(0.35f, 0.12f, 0.08f));
            }

            // Le voleur de ton or, tant qu'il court avec.
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival rv = Rival.All[i];
                if (rv != null && rv.IsHuntedThief && Mathf.Sin(Time.unscaledTime * 8f) > -0.3f)
                    Mark(r, rv.transform.position, UiStyle.Shape.Diamond, new Color(0.9f, 0.2f, 0.15f), 14f);
            }
            // Tes alarmes, et celles qui viennent de sonner.
            for (int i = 0; i < Alarm.All.Count; i++)
            {
                Alarm al = Alarm.All[i];
                if (al == null || al.owner != me) continue;
                bool rang = Time.time - al.RangAt < 12f;
                Mark(r, al.transform.position, UiStyle.Shape.Dot, rang ? new Color(1f, 0.5f, 0.2f) : new Color(0.55f, 0.45f, 0.2f), rang ? 12f : 7f);
            }
            // Ta depouille.
            for (int i = 0; i < Remains.All.Count; i++)
                if (Remains.All[i] != null && Remains.All[i].IsMine)
                {
                    Mark(r, Remains.All[i].transform.position, UiStyle.Shape.Dot, new Color(0.8f, 0.2f, 0.15f), 11f);
                    Label(r, Remains.All[i].transform.position, "ta dépouille", new Color(0.55f, 0.12f, 0.08f), 13f);
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
            GUIStyle head = UiStyle.Head;
            TextAnchor was2 = head.alignment;
            head.alignment = TextAnchor.MiddleCenter;
            UiStyle.Tinted(new Rect(r.x, r.y + 6f, r.width, UiStyle.S(24)), "N", head, new Color(0.45f, 0.12f, 0.08f));
            head.alignment = was2;
            Legend(new Rect(r.x, r.yMax + UiStyle.S(6), r.width, UiStyle.S(20)));
        }

        /// <summary>La legende, sous le parchemin : une forme, un mot.</summary>
        static void Legend(Rect r)
        {
            string[] words = { "ta stèle", "camp", "cache", "autel", "lieu-dit", "géant", "toi" };
            UiStyle.Shape[] shapes = { UiStyle.Shape.Diamond, UiStyle.Shape.Triangle, UiStyle.Shape.Dot, UiStyle.Shape.Square,
                                       UiStyle.Shape.Dot, UiStyle.Shape.Triangle, UiStyle.Shape.Triangle };
            Color[] colors = { new Color(0.35f, 0.5f, 0.9f), new Color(0.45f, 0.7f, 0.35f), new Color(0.7f, 0.48f, 0.26f), new Color(0.85f, 0.7f, 0.4f),
                               new Color(0.6f, 0.55f, 0.48f), new Color(0.3f, 0.5f, 0.28f), new Color(0.9f, 0.3f, 0.2f) };
            float step = r.width / words.Length;
            for (int i = 0; i < words.Length; i++)
            {
                float x = r.x + step * i;
                float d = UiStyle.S(9);
                UiStyle.Icon(new Rect(x + UiStyle.S(6), r.center.y - d * 0.5f, d, d), shapes[i], colors[i]);
                UiStyle.Tinted(new Rect(x + UiStyle.S(19), r.y, step - UiStyle.S(19), r.height), words[i], UiStyle.Tiny, UiStyle.InkDim);
            }
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
            float side = Mathf.Min(Screen.height - UiStyle.S(110), UiStyle.S(720));
            Rect frame = new Rect((Screen.width - side) * 0.5f - 10f, (Screen.height - side) * 0.5f - 22f, side + 20f, side + 20f + UiStyle.S(28));
            UiStyle.Frame(frame);
            Atlas.Draw(new Rect(frame.x + 10f, frame.y + 10f, side, side));
        }
    }
}
