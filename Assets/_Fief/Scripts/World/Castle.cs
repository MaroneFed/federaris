using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA CITADELLE (27/09 -- Martin : "un giga chateau"). Au centre de la sylve.
    ///
    ///   - une ENCEINTE de cent metres de cote, murs de dix-huit metres, quatre tours
    ///     d'angle coiffees d'ardoise ;
    ///   - QUATRE PORTES, ouvertes, une par face, chacune entre deux tours de garde :
    ///     on entre de partout, la bataille commence dans la cour ;
    ///   - QUATRE ESCALIERS montent aux remparts : la-haut, on voit loin, et on saute ;
    ///   - au milieu, LA TOUR DE LA COURONNE (voir Tower.cs) : soixante-quatre metres,
    ///     une rampe en spirale a l'exterieur, la Couronne au sommet ;
    ///   - les YEUX (voir Eye.cs) sur les tours et les portes : pas de gardes, des
    ///     sentinelles de pierre qui chargent un rayon qu'on voit venir.
    ///
    /// Tout est bati en cubes, comme le reste du decor. Les murs, les tours et les
    /// escaliers ont un collider ; les creneaux et les ornements, non.
    /// </summary>
    public static class Castle
    {
        /// <summary>Demi-cote de l'enceinte : cent metres de cote.</summary>
        public const float HalfSize = 50f;
        public const float WallHeight = 18f;
        public const float WallThickness = 3f;
        public const float TowerSize = 14f;
        public const float TowerHeight = 34f;
        public const float GateWidth = 8f;
        public const float GateHeight = 10f;
        public const float GateTowerSize = 8f;
        public const float GateTowerHeight = 26f;

        /// <summary>Rayon du sol aplani sous la citadelle, coins et tours compris.</summary>
        public const float FlatRadius = HalfSize * 1.42f + TowerSize * 0.5f + 10f;

        /// <summary>Le centre de la tour (pour les sons, la musique...).</summary>
        public static readonly Vector3 KeepCentre = Vector3.zero;

        static readonly Color Stone = new Color(0.31f, 0.31f, 0.30f);
        static readonly Color StoneDark = new Color(0.23f, 0.23f, 0.23f);
        static readonly Color StoneMoss = new Color(0.22f, 0.26f, 0.19f);
        static readonly Color Slate = new Color(0.17f, 0.18f, 0.20f);
        static readonly Color Timber = new Color(0.22f, 0.16f, 0.11f);
        static readonly Color IronDark = new Color(0.12f, 0.12f, 0.13f);
        static readonly Color Paving = new Color(0.20f, 0.20f, 0.19f);

        /// <summary>Vrai si ce point est dans l'emprise de la citadelle (plus une marge) : la foret n'y pousse pas.</summary>
        public static bool Covers(float x, float z, float margin)
        {
            float reach = HalfSize + TowerSize * 0.5f + margin;
            return Mathf.Abs(x) < reach && Mathf.Abs(z) < reach;
        }

        /// <summary>Vrai si ce point est DANS l'enceinte (cour, tour, remparts compris).</summary>
        public static bool Inside(Vector3 p)
        {
            return Mathf.Abs(p.x) < HalfSize + WallThickness * 0.5f && Mathf.Abs(p.z) < HalfSize + WallThickness * 0.5f;
        }

        /// <summary>Vrai si ce point est sur la tour (sa rampe ou son sommet).</summary>
        public static bool InKeep(Vector3 p) { return Tower.On(p); }

        /// <summary>
        /// Pour entrer depuis "from" : la porte la plus proche. Deux points, dehors puis
        /// dedans. (Pour sortir, on les lit a l'envers.)
        /// </summary>
        public static Vector3[] EntryFrom(Vector3 from)
        {
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            Vector3 best = Vector3.back;
            float bestD = float.MaxValue;
            for (int i = 0; i < dirs.Length; i++)
            {
                float d = (from - dirs[i] * HalfSize).sqrMagnitude;
                if (d < bestD) { bestD = d; best = dirs[i]; }
            }
            return new[] { best * (HalfSize + 9f), best * (HalfSize - 9f) };
        }

        public static void Build(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("CITADELLE");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            Game.CastleCentre = Vector3.zero;

            BuildCurtain(t);
            BuildCorners(t);
            BuildGates(t);
            BuildStairs(t);
            BuildCourtyard(t);
            Tower.Build(t);

            int dressed = Masonry.Apply(t);
            Debug.Log("[FIEF] Citadelle : " + dressed + " pans de pierre appareillés.");
        }

        // ------------------------------------------------------------------ outils

        /// <summary>Un pan de mur entre deux points du sol, de y0 a y1.</summary>
        static GameObject Segment(Transform parent, Vector3 a, Vector3 b, float y0, float y1, float thick, Color color, bool solid, string name)
        {
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = flat.magnitude;
            if (length < 0.01f) return null;
            if (!solid) Proto.BeginVisualOnly();
            GameObject go = Proto.Cube(parent, Vector3.zero, new Vector3(thick, y1 - y0, length), color, name);
            if (!solid) Proto.EndVisualOnly();
            go.transform.localPosition = new Vector3((a.x + b.x) * 0.5f, (y0 + y1) * 0.5f, (a.z + b.z) * 0.5f);
            go.transform.localRotation = Quaternion.LookRotation(flat / length, Vector3.up);
            return go;
        }

        /// <summary>Des creneaux sur le haut d'un mur. Quelques-uns sont tombes.</summary>
        static void Crenellate(Transform parent, Vector3 a, Vector3 b, float top, float thick, int seed)
        {
            System.Random rng = new System.Random(seed);
            Vector3 dir = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = dir.magnitude;
            if (length < 1f) return;
            dir /= length;
            Proto.BeginVisualOnly();
            for (float s = 0.9f; s < length - 0.5f; s += 2.4f)
            {
                if (rng.NextDouble() < 0.1) continue;
                Vector3 p = a + dir * s;
                GameObject m = Proto.Cube(parent, new Vector3(p.x, top + 0.65f, p.z), new Vector3(thick * 0.3f, 1.3f, 1.25f),
                                          rng.NextDouble() < 0.2 ? StoneMoss : Stone, "Merlon");
                m.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ enceinte

        static void BuildCurtain(Transform t)
        {
            float h = HalfSize, y0 = -1.5f, gap = GateWidth * 0.5f;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % 4];
                Vector3 mid = (a + b) * 0.5f, dir = (b - a).normalized;
                Vector3 left = mid - dir * gap, right = mid + dir * gap;
                Segment(t, a, left, y0, WallHeight, WallThickness, Stone, true, "Courtine");
                Segment(t, right, b, y0, WallHeight, WallThickness, Stone, true, "Courtine");
                Segment(t, left, right, GateHeight, WallHeight, WallThickness, Stone, true, "Linteau");
                // Le merlon cote exterieur seulement : le chemin de ronde reste libre.
                Vector3 outward = Vector3.Cross(Vector3.up, dir) * -1f;
                Vector3 edge = outward * (WallThickness * 0.5f - 0.3f);
                Crenellate(t, a + edge, left + edge, WallHeight, WallThickness, 11 + i);
                Crenellate(t, right + edge, b + edge, WallHeight, WallThickness, 21 + i);
                // Une plinthe sombre au pied : l'humidite qui remonte.
                Segment(t, a, left, y0, 1.8f, WallThickness + 0.4f, StoneDark, false, "Plinthe");
                Segment(t, right, b, y0, 1.8f, WallThickness + 0.4f, StoneDark, false, "Plinthe");
                // Des contreforts dehors, tous les douze metres.
                for (float u = 12f; u < h * 2f - 12f; u += 12f)
                {
                    if (Mathf.Abs(u - h) < gap + 8f) continue;
                    Vector3 p = a + dir * u + outward * (WallThickness * 0.5f + 1f);
                    GameObject butt = Proto.Cube(t, p + Vector3.up * 3.5f, new Vector3(2.6f, 8.5f, 2f), StoneDark, "Contrefort");
                    butt.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                }
            }
        }

        static void BuildCorners(Transform t)
        {
            float h = HalfSize;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            for (int i = 0; i < corners.Length; i++) BigTower(t, corners[i], TowerSize, TowerHeight, true, 30 + i);
        }

        /// <summary>Une tour carree, meurtrieres et, s'il le faut, un toit d'ardoise pointu.</summary>
        static void BigTower(Transform t, Vector3 at, float size, float height, bool roofed, int seed)
        {
            Proto.Cube(t, new Vector3(at.x, (height - 1.5f) * 0.5f, at.z), new Vector3(size, height + 1.5f, size), Stone, "Tour");
            Proto.BeginVisualOnly();
            for (int k = 0; k < 4; k++)
            {
                Vector3 outward = Quaternion.Euler(0f, k * 90f, 0f) * Vector3.forward;
                for (int level = 0; level < Mathf.FloorToInt((height - 4f) / 6f); level++)
                {
                    GameObject slit = Proto.Cube(t, at + outward * (size * 0.5f + 0.02f) + Vector3.up * (6f + level * 6f),
                                                 new Vector3(0.5f, 2f, 0.08f), IronDark, "Meurtrière");
                    slit.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                    if (level == 2 && k % 2 == seed % 2) slit.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.7f, 0.4f), 1.2f);
                }
            }
            Proto.Cube(t, new Vector3(at.x, height + 0.3f, at.z), new Vector3(size + 0.6f, 0.6f, size + 0.6f), StoneDark, "Corniche");
            Proto.EndVisualOnly();
            if (roofed)
            {
                Proto.Cone(t, new Vector3(at.x, height + 0.6f, at.z), size * 0.78f, size * 1.1f, Slate, "Toit");
                Proto.Cone(t, new Vector3(at.x, height + 0.6f + size * 1.05f, at.z), 0.2f, 3.6f, IronDark, "Flèche", 4);
                return;
            }
            float half = size * 0.5f;
            Vector3 c1 = at + new Vector3(-half, 0f, half), c2 = at + new Vector3(half, 0f, half);
            Vector3 c3 = at + new Vector3(half, 0f, -half), c4 = at + new Vector3(-half, 0f, -half);
            Crenellate(t, c1, c2, height, 1.2f, seed);
            Crenellate(t, c2, c3, height, 1.2f, seed + 1);
            Crenellate(t, c3, c4, height, 1.2f, seed + 2);
            Crenellate(t, c4, c1, height, 1.2f, seed + 3);
        }

        /// <summary>Les quatre portes : deux tours de garde, un arc sombre, deux braseros.</summary>
        static void BuildGates(Transform t)
        {
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 d = dirs[i];
                Vector3 side = Vector3.Cross(Vector3.up, d);
                Vector3 gate = d * HalfSize;
                float off = GateWidth * 0.5f + GateTowerSize * 0.5f;
                BigTower(t, gate + side * off + d * 1f, GateTowerSize, GateTowerHeight, false, 40 + i * 5);
                BigTower(t, gate - side * off + d * 1f, GateTowerSize, GateTowerHeight, false, 42 + i * 5);
                // L'arc : un bandeau sombre au-dessus du passage, et la herse relevee.
                Proto.BeginVisualOnly();
                for (float s = -GateWidth * 0.5f + 0.5f; s < GateWidth * 0.5f; s += 0.8f)
                    Proto.Cube(t, gate + side * s + Vector3.up * (GateHeight - 0.7f), new Vector3(0.14f, 1.4f, 0.14f), IronDark, "Herse levée");
                Proto.EndVisualOnly();
                Torch(t, gate + d * 5f + side * (GateWidth * 0.5f + 1.2f), 3.4f);
                Torch(t, gate + d * 5f - side * (GateWidth * 0.5f + 1.2f), 3.4f);
                Torch(t, gate - d * 5f + side * (GateWidth * 0.5f + 1.2f), 3.4f);
                Torch(t, gate - d * 5f - side * (GateWidth * 0.5f + 1.2f), 3.4f);
            }
        }

        /// <summary>
        /// LES ESCALIERS DES REMPARTS : une rampe de pierre contre chaque courtine, cote
        /// cour, finie par un palier. Du chemin de ronde, on domine la cour -- et on
        /// peut sauter de dix-huit metres (on ne meurt pas : on atterrit).
        /// </summary>
        static void BuildStairs(Transform t)
        {
            float inner = HalfSize - WallThickness * 0.5f - 1.3f;
            Vector3[] dirs = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 d = dirs[i];
                Vector3 along = Vector3.Cross(Vector3.up, d);
                // (Entre la tour de garde de la porte, jusqu'a x = 12, et la tour d'angle, a 43.)
                Vector3 from = d * inner + along * 13f;
                Vector3 to = d * inner + along * 37f + Vector3.up * WallHeight;
                Stair(t, from, to);
                Proto.Cube(t, d * inner + along * 39f + Vector3.up * (WallHeight - 0.2f), Rot(d, new Vector3(2.6f, 0.4f, 4f)), StoneDark, "Palier");
            }
        }

        static Vector3 Rot(Vector3 d, Vector3 size)
        {
            return Mathf.Abs(d.x) > 0.5f ? new Vector3(size.x, size.y, size.z) : new Vector3(size.z, size.y, size.x);
        }

        /// <summary>Un escalier droit : une rampe pleine (le collider) et des marches dessinees.</summary>
        public static void Stair(Transform t, Vector3 from, Vector3 to)
        {
            Vector3 run = to - from;
            float length = run.magnitude + 0.4f;
            GameObject ramp = Proto.Cube(t, (from + to) * 0.5f - new Vector3(0f, 0.17f, 0f), new Vector3(2.6f, 0.34f, length), StoneDark, "Escalier");
            ramp.transform.localRotation = Quaternion.LookRotation(run.normalized, Vector3.up);
            Proto.BeginVisualOnly();
            int steps = Mathf.RoundToInt(run.magnitude / 0.45f);
            Vector3 flatDir = new Vector3(run.x, 0f, run.z).normalized;
            for (int i = 0; i < steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, (i + 0.5f) / steps) + Vector3.up * 0.02f;
                GameObject step = Proto.Cube(t, p, new Vector3(2.5f, 0.06f, 0.1f), Paving, "Nez de marche");
                step.transform.localRotation = Quaternion.LookRotation(flatDir, Vector3.up);
            }
            // Sous la rampe, la maconnerie qui la porte.
            for (int i = 1; i < 6; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / 6f);
                Proto.Cube(t, new Vector3(p.x, p.y * 0.5f - 0.2f, p.z), new Vector3(2.4f, Mathf.Max(0.1f, p.y), 0.8f), Stone, "Pile").transform.localRotation =
                    Quaternion.LookRotation(flatDir, Vector3.up);
            }
            Proto.EndVisualOnly();
        }

        static void BuildCourtyard(Transform t)
        {
            float inner = HalfSize - WallThickness * 0.5f;
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(0f, -0.01f, 0f), new Vector3(inner * 2f, 0.1f, inner * 2f), Paving, "Dallage");
            // Quatre allees claires, des portes a la tour : elles guident sans rien dire.
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                Vector3 mid = d * (inner + Tower.OuterRadius) * 0.5f;
                Proto.Cube(t, mid + Vector3.up * 0.01f, Rot(d, new Vector3(inner - Tower.OuterRadius, 0.1f, 5f)), StoneDark, "Allée");
            }
            Proto.EndVisualOnly();
            // Des torches le long des allees, et des colonnes brisees ou se cacher.
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward;
                Vector3 at = d * 30f;
                Proto.Cube(t, at + Vector3.up * 2.2f, new Vector3(2f, 4.4f, 2f), Stone, "Colonne brisée");
                Proto.Cube(t, at + d * 5f + Vector3.up * 0.6f, new Vector3(2.4f, 1.2f, 1.4f), StoneDark, "Bloc tombé");
                Torch(t, d * 22f, 3.2f);
            }
        }

        // ------------------------------------------------------------------ torches

        /// <summary>
        /// Une torche : un poteau, une flamme, et une lumiere qui vacille. "at" est son
        /// pied (hauteur comprise : il y en a sur la tour).
        /// </summary>
        public static void Torch(Transform t, Vector3 at, float height)
        {
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(at.x, at.y + height * 0.5f, at.z), new Vector3(0.16f, height, 0.16f), Timber, "Torche");
            Proto.Cube(t, new Vector3(at.x, at.y + height + 0.12f, at.z), new Vector3(0.3f, 0.12f, 0.3f), IronDark, "Coupe");
            GameObject flame = Proto.Cube(t, new Vector3(at.x, at.y + height + 0.38f, at.z), new Vector3(0.22f, 0.4f, 0.22f), new Color(1f, 0.62f, 0.22f), "Flamme");
            Proto.EndVisualOnly();
            Renderer r = flame.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.6f, 0.22f), 2.2f);
            flame.AddComponent<Flame>();
            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(at.x, at.y + height + 0.6f, at.z);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.70f, 0.40f);
            light.intensity = 1.5f;
            light.range = 11f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
        }
    }
}
