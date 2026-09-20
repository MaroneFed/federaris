using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les chemins et le decor : ce qui transforme une plaine vide en carte.
    ///
    /// Deux idees, et c'est tout :
    ///  1. Des CHEMINS du marche vers chaque fief. Un trajet sans repere est long ;
    ///     le meme trajet avec une route a suivre est une balade.
    ///  2. Du DECOR seme partout (buissons, rochers, fleurs, souches) + quelques
    ///     grands reperes visibles de loin, pour savoir ou on est sans regarder le HUD.
    /// </summary>
    public static class Scenery
    {
        struct Segment
        {
            public Vector2 a;
            public Vector2 b;
            public float width;
        }

        static readonly List<Segment> roads = new List<Segment>();

        public static void Reset()
        {
            roads.Clear();
        }

        // ------------------------------------------------------------------ chemins

        public static void BuildRoads(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("Chemins");
            root.transform.SetParent(parent, false);

            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 fief = cfg.FiefPosition(i);
                BuildRoad(root.transform, Vector3.zero, fief, 7f, "Chemin_Fief" + (i + 1));

                Segment s = new Segment();
                s.a = Vector2.zero;
                s.b = new Vector2(fief.x, fief.z);
                s.width = 7f;
                roads.Add(s);
            }
        }

        /// <summary>
        /// Un ruban de terre qui epouse le relief. On echantillonne le sol tous les 4 m
        /// et on pose deux sommets de chaque cote : c'est tout ce qu'est une route.
        /// </summary>
        static void BuildRoad(Transform parent, Vector3 from, Vector3 to, float width, string name)
        {
            float length = Vector3.Distance(from, to);
            int steps = Mathf.Max(2, Mathf.CeilToInt(length / 4f));

            Vector3 direction = to - from;
            direction.y = 0f;
            direction.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, direction);

            Vector3[] vertices = new Vector3[(steps + 1) * 2];
            int[] triangles = new int[steps * 6];

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 centre = Vector3.Lerp(from, to, t);

                // Legere sinuosite : une route parfaitement droite fait artificiel.
                float wobble = Mathf.Sin(t * Mathf.PI * 2.6f) * 3.2f;
                centre += side * wobble;

                float half = width * 0.5f * (0.85f + 0.3f * Mathf.Sin(t * Mathf.PI * 4f));
                Vector3 left = centre - side * half;
                Vector3 right = centre + side * half;

                vertices[i * 2] = Ground.Place(left.x, left.z, 0.10f);
                vertices[i * 2 + 1] = Ground.Place(right.x, right.z, 0.10f);
            }

            for (int i = 0; i < steps; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Get(Palette.Path);
        }

        /// <summary>Distance d'un point au chemin le plus proche (pour ne pas semer dessus).</summary>
        public static float DistanceToRoad(float x, float z)
        {
            if (roads.Count == 0) return 9999f;

            Vector2 p = new Vector2(x, z);
            float best = 9999f;
            for (int i = 0; i < roads.Count; i++)
            {
                Vector2 a = roads[i].a;
                Vector2 ab = roads[i].b - a;
                float lengthSq = ab.sqrMagnitude;
                float t = lengthSq <= 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best) best = d;
            }
            return best;
        }

        // ------------------------------------------------------------------ decor seme

        public static void Scatter(Transform parent, GameConfig cfg, System.Random rng,
                                   List<Vector3> occupied, int count)
        {
            Proto.BeginVisualOnly();
            GameObject root = new GameObject("Decor");
            root.transform.SetParent(parent, false);

            float half = cfg.mapSize * 0.5f - 14f;
            int placed = 0;
            int attempts = 0;

            while (placed < count && attempts < count * 12)
            {
                attempts++;

                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * half;

                if (DistanceToRoad(x, z) < 6f) continue;

                bool clear = true;
                for (int i = 0; i < occupied.Count; i++)
                {
                    float dx = occupied[i].x - x;
                    float dz = occupied[i].z - z;
                    if (dx * dx + dz * dz < 36f) { clear = false; break; }
                }
                if (!clear) continue;

                Vector3 position = Ground.Place(x, z, 0f);
                if (position.y < -1.5f) continue;          // rien a planter dans un lac
                float slope = Ground.Slope(x, z);

                CreateProp(root.transform, position, slope, rng);
                placed++;
            }

            Proto.EndVisualOnly();
        }

        static void CreateProp(Transform parent, Vector3 position, float slope, System.Random rng)
        {
            GameObject go = new GameObject("Decor");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            double roll = rng.NextDouble();

            // En pente, on met de la caillasse. Sur le plat, de la vegetation.
            if (slope > 0.34)
            {
                Rocks(go.transform, rng);
            }
            else if (roll < 0.34) Bush(go.transform, rng);
            else if (roll < 0.58) Flowers(go.transform, rng);
            else if (roll < 0.74) TallGrass(go.transform, rng);
            else if (roll < 0.88) Rocks(go.transform, rng);
            else Stump(go.transform, rng);

            Proto.StripCollidersRecursive(go);
        }

        static void Bush(Transform parent, System.Random rng)
        {
            Color leaf = Palette.Shade(Palette.Wood, 0.72f + (float)rng.NextDouble() * 0.3f);
            int blobs = 2 + rng.Next(2);
            for (int i = 0; i < blobs; i++)
            {
                float s = 0.7f + (float)rng.NextDouble() * 0.7f;
                Proto.Sphere(parent, new Vector3(((float)rng.NextDouble() - 0.5f) * 1.1f,
                                                 s * 0.4f,
                                                 ((float)rng.NextDouble() - 0.5f) * 1.1f),
                             new Vector3(s, s * 0.8f, s), leaf, "Buisson");
            }
        }

        static void Flowers(Transform parent, System.Random rng)
        {
            Color[] palette = { Palette.Flower1, Palette.Flower2, Palette.Flower3 };
            int count = 3 + rng.Next(4);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = new Vector3(((float)rng.NextDouble() - 0.5f) * 2.4f, 0.22f,
                                        ((float)rng.NextDouble() - 0.5f) * 2.4f);
                Proto.Cube(parent, p, new Vector3(0.05f, 0.42f, 0.05f),
                           Palette.Shade(Palette.Wood, 0.8f), "Tige");
                Proto.Sphere(parent, p + new Vector3(0f, 0.26f, 0f), Vector3.one * 0.18f,
                             palette[rng.Next(palette.Length)], "Fleur");
            }
        }

        static void TallGrass(Transform parent, System.Random rng)
        {
            Color tone = Palette.Shade(Palette.Grass, 1.15f + (float)rng.NextDouble() * 0.2f);
            int blades = 4 + rng.Next(5);
            for (int i = 0; i < blades; i++)
            {
                float h = 0.5f + (float)rng.NextDouble() * 0.6f;
                GameObject blade = Proto.Cube(parent,
                    new Vector3(((float)rng.NextDouble() - 0.5f) * 1.6f, h * 0.5f,
                                ((float)rng.NextDouble() - 0.5f) * 1.6f),
                    new Vector3(0.09f, h, 0.09f), tone, "Herbe");
                blade.transform.localRotation = Quaternion.Euler(
                    ((float)rng.NextDouble() - 0.5f) * 26f,
                    (float)rng.NextDouble() * 360f,
                    ((float)rng.NextDouble() - 0.5f) * 26f);
            }
        }

        static void Rocks(Transform parent, System.Random rng)
        {
            int count = 1 + rng.Next(3);
            for (int i = 0; i < count; i++)
            {
                float s = 0.5f + (float)rng.NextDouble() * 1.1f;
                GameObject rock = Proto.Cube(parent,
                    new Vector3(((float)rng.NextDouble() - 0.5f) * 1.6f, s * 0.3f,
                                ((float)rng.NextDouble() - 0.5f) * 1.6f),
                    new Vector3(s, s * 0.7f, s * 0.9f),
                    Palette.Shade(Palette.Stone, 0.7f + (float)rng.NextDouble() * 0.35f), "Caillou");
                rock.transform.localRotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 30f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 30f);
            }
        }

        static void Stump(Transform parent, System.Random rng)
        {
            float h = 0.35f + (float)rng.NextDouble() * 0.3f;
            Proto.Cylinder(parent, new Vector3(0f, h * 0.5f, 0f),
                           new Vector3(0.62f, h * 0.5f, 0.62f), Palette.Trunk, "Souche");
            Proto.Cylinder(parent, new Vector3(0f, h, 0f),
                           new Vector3(0.58f, 0.03f, 0.58f),
                           Palette.Shade(Palette.Trunk, 1.35f), "Coupe");
        }

        // ------------------------------------------------------------------ forets

        /// <summary>
        /// Seme des arbres decoratifs partout sur la carte, en paquets.
        ///
        /// C'est le changement qui compte le plus visuellement : avant, il n'y avait
        /// d'arbres que dans les zones de recolte, et le reste du monde etait une
        /// prairie nue. Des bosquets partout, ca devient un pays.
        /// </summary>
        public static void PlantForests(Transform parent, GameConfig cfg, System.Random rng,
                                        List<Vector3> occupied, int count)
        {
            Proto.BeginVisualOnly();
            GameObject root = new GameObject("Forets");
            root.transform.SetParent(parent, false);

            float half = cfg.mapSize * 0.5f - 18f;
            int planted = 0;
            int guard = 0;

            while (planted < count && guard < count * 10)
            {
                guard++;

                // On plante par bosquets de 4 a 9 arbres : une foret n'est pas
                // une distribution uniforme, c'est des paquets et des clairieres.
                float gx = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float gz = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                if (!SpotIsFree(gx, gz, occupied, 14f)) continue;

                int clump = 4 + rng.Next(6);
                float spread = 7f + (float)rng.NextDouble() * 12f;

                for (int i = 0; i < clump && planted < count; i++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float d = spread * Mathf.Sqrt((float)rng.NextDouble());
                    float x = gx + Mathf.Cos(a) * d;
                    float z = gz + Mathf.Sin(a) * d;

                    if (Mathf.Abs(x) > half || Mathf.Abs(z) > half) continue;
                    if (!SpotIsFree(x, z, occupied, 6f)) continue;
                    if (Ground.Slope(x, z) > 0.5f) continue;

                    float y = Ground.Height(x, z);
                    if (y < -1.5f) continue;          // pas d'arbre dans un lac
                    if (y > 46f) continue;            // ni au-dessus de la limite des arbres

                    NodeFactory.DecorTree(root.transform, new Vector3(x, y, z), rng);
                    planted++;
                }
            }

            Proto.EndVisualOnly();
        }

        static bool SpotIsFree(float x, float z, List<Vector3> occupied, float clearance)
        {
            if (DistanceToRoad(x, z) < clearance + 2f) return false;
            float sq = clearance * clearance;
            for (int i = 0; i < occupied.Count; i++)
            {
                float dx = occupied[i].x - x;
                float dz = occupied[i].z - z;
                if (dx * dx + dz * dz < sq) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ ciel

        /// <summary>Des nuages plats et lents, tres haut. Ils donnent son echelle au ciel.</summary>
        public static void BuildClouds(Transform parent, GameConfig cfg, System.Random rng)
        {
            Proto.BeginVisualOnly();
            GameObject root = new GameObject("Nuages");
            root.transform.SetParent(parent, false);

            Color white = new Color(0.97f, 0.97f, 0.99f);
            float span = cfg.mapSize * 0.75f;

            for (int i = 0; i < 26; i++)
            {
                GameObject cloud = new GameObject("Nuage");
                cloud.transform.SetParent(root.transform, false);
                cloud.transform.position = new Vector3(
                    (float)(rng.NextDouble() * 2.0 - 1.0) * span,
                    150f + (float)rng.NextDouble() * 90f,
                    (float)(rng.NextDouble() * 2.0 - 1.0) * span);

                int puffs = 3 + rng.Next(4);
                float scale = 22f + (float)rng.NextDouble() * 34f;
                for (int p = 0; p < puffs; p++)
                {
                    Proto.Sphere(cloud.transform,
                        new Vector3(((float)rng.NextDouble() - 0.5f) * scale * 1.7f,
                                    ((float)rng.NextDouble() - 0.5f) * scale * 0.22f,
                                    ((float)rng.NextDouble() - 0.5f) * scale * 1.1f),
                        new Vector3(scale * (0.6f + (float)rng.NextDouble() * 0.6f),
                                    scale * 0.32f,
                                    scale * (0.6f + (float)rng.NextDouble() * 0.5f)),
                        white, "Masse");
                }

                Proto.StripCollidersRecursive(cloud);
                Drift drift = cloud.AddComponent<Drift>();
                drift.velocity = new Vector3(1.4f + (float)rng.NextDouble() * 1.6f, 0f, 0.35f);
                drift.wrapDistance = span * 1.35f;
            }

            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ grands reperes

        /// <summary>
        /// Des monuments visibles de loin. Leur seul role est de te permettre de dire
        /// "je suis pres des pierres levees" au lieu de "je suis quelque part".
        /// </summary>
        public static void BuildLandmarks(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("Reperes");
            root.transform.SetParent(parent, false);

            // Positions choisies sur les hauteurs, a l'ecart des chemins et des zones
            // de jeu : un repere sert a se situer, il doit se voir de loin et ne gener personne.
            StoneCircle(root.transform, new Vector2(122f, -22f));
            StoneCircle(root.transform, new Vector2(230f, -250f));
            RuinedArch(root.transform, new Vector2(-40f, 116f));
            RuinedArch(root.transform, new Vector2(104f, -322f));
            RuinedArch(root.transform, new Vector2(-328f, -76f));
            DeadTree(root.transform, new Vector2(-46f, -124f));
            DeadTree(root.transform, new Vector2(-328f, 80f));
            DeadTree(root.transform, new Vector2(92f, 326f));
        }

        static void StoneCircle(Transform parent, Vector2 centre)
        {
            GameObject go = new GameObject("PierresLevees");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Place(centre.x, centre.y, 0f);

            for (int i = 0; i < 7; i++)
            {
                float angle = (360f / 7f) * i * Mathf.Deg2Rad;
                float radius = 5.5f;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                float h = 3.4f + (i % 3) * 0.7f;

                Vector3 local = Ground.Place(centre.x + x, centre.y + z, 0f) - go.transform.position;
                GameObject stone = Proto.Cube(go.transform, local + new Vector3(0f, h * 0.5f, 0f),
                                              new Vector3(1.1f, h, 0.7f),
                                              Palette.Shade(Palette.Stone, 0.78f), "Menhir");
                stone.transform.localRotation = Quaternion.Euler(
                    (i % 2 == 0) ? 4f : -3f, -Mathf.Rad2Deg * angle, (i % 3 == 0) ? 3f : -2f);
            }
        }

        static void RuinedArch(Transform parent, Vector2 centre)
        {
            GameObject go = new GameObject("RuineAncienne");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Place(centre.x, centre.y, 0f);

            Color stone = Palette.Shade(Palette.Structure, 0.82f);
            Proto.Cube(go.transform, new Vector3(-2.6f, 2.6f, 0f), new Vector3(1.2f, 5.2f, 1.2f), stone, "Pilier");
            Proto.Cube(go.transform, new Vector3(2.6f, 2.2f, 0f), new Vector3(1.2f, 4.4f, 1.2f), stone, "Pilier2");
            GameObject lintel = Proto.Cube(go.transform, new Vector3(0f, 5.4f, 0f),
                                           new Vector3(6.6f, 0.9f, 1.1f), stone, "Linteau");
            lintel.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);

            GameObject fallen = Proto.Cube(go.transform, new Vector3(4.4f, 0.5f, 1.6f),
                                           new Vector3(3.2f, 0.9f, 1.1f), stone, "Bloc");
            fallen.transform.localRotation = Quaternion.Euler(0f, 24f, 8f);
        }

        static void DeadTree(Transform parent, Vector2 centre)
        {
            GameObject go = new GameObject("ArbreMort");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Place(centre.x, centre.y, 0f);

            Color bark = Palette.Shade(Palette.Trunk, 0.72f);
            Proto.Cylinder(go.transform, new Vector3(0f, 3.1f, 0f), new Vector3(0.6f, 3.1f, 0.6f), bark, "Tronc");

            for (int i = 0; i < 3; i++)
            {
                GameObject branch = Proto.Cylinder(go.transform,
                    new Vector3(0f, 4.4f + i * 0.8f, 0f), new Vector3(0.22f, 1.5f, 0.22f), bark, "Branche");
                branch.transform.localRotation = Quaternion.Euler(0f, i * 120f, 52f - i * 9f);
            }
        }
    }
}
