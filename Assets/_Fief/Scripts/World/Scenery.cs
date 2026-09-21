using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Tout ce qui remplit le monde : chemins, forets, sous-bois, detail, grands reperes.
    ///
    /// Le decor ne cree quasiment aucun GameObject : il est fusionne par le Batcher en
    /// quelques centaines de maillages. C'est ce qui permet de passer de quelques
    /// centaines de touffes a plusieurs milliers d'arbres sans que rien ne rame.
    ///
    /// Regle de lisibilite : les arbres DECORATIFS sont anguleux et sombres (feuillage
    /// en cubes). Les GISEMENTS a recolter sont ronds, clairs, et portent un marqueur.
    /// On doit pouvoir dire au premier coup d'oeil ce qui se recolte.
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

        // ================================================================ chemins

        public static void BuildRoads(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("Chemins");
            root.transform.SetParent(parent, false);

            for (int i = 0; i < cfg.fiefCount; i++)
            {
                Vector3 fief = cfg.FiefPosition(i);
                BuildRoad(root.transform, Vector3.zero, fief, 9f, "Chemin_Fief" + (i + 1));

                Segment s = new Segment();
                s.a = Vector2.zero;
                s.b = new Vector2(fief.x, fief.z);
                s.width = 9f;
                roads.Add(s);
            }
        }

        static void BuildRoad(Transform parent, Vector3 from, Vector3 to, float width, string name)
        {
            float length = Vector3.Distance(from, to);
            int steps = Mathf.Max(2, Mathf.CeilToInt(length / 6f));

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
                centre += side * Mathf.Sin(t * Mathf.PI * 3.2f) * 7f;

                float half = width * 0.5f * (0.85f + 0.3f * Mathf.Sin(t * Mathf.PI * 5f));
                Vector3 left = centre - side * half;
                Vector3 right = centre + side * half;

                vertices[i * 2] = Ground.Place(left.x, left.z, 0.12f);
                vertices[i * 2 + 1] = Ground.Place(right.x, right.z, 0.12f);
            }

            for (int i = 0; i < steps; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Get(Palette.Path);
        }

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

        // ================================================================ placement

        /// <summary>Un endroit est libre s'il n'est ni sur un chemin, ni sur un objet deja pose,
        /// ni dans un lac, ni sur une pente trop raide.</summary>
        public static bool IsFree(float x, float z, List<Vector3> occupied, float clearance, float roadClearance)
        {
            if (DistanceToRoad(x, z) < roadClearance) return false;

            float sq = clearance * clearance;
            for (int i = 0; i < occupied.Count; i++)
            {
                float dx = occupied[i].x - x;
                float dz = occupied[i].z - z;
                if (dx * dx + dz * dz < sq) return false;
            }
            return true;
        }

        static bool Plantable(float x, float z, float maxSlope, float maxHeight)
        {
            float y = Ground.Height(x, z);
            if (y < -1.5f) return false;
            if (y > maxHeight) return false;
            return Ground.Slope(x, z) <= maxSlope;
        }

        // ================================================================ forets

        /// <summary>
        /// Des milliers d'arbres, semes en bosquets avec des clairieres entre eux.
        /// Feuillage en cubes inclines : anguleux, sombre, et surtout tres leger
        /// (un arbre coute environ 170 sommets au lieu de 1 600 avec des spheres).
        /// </summary>
        public static void PlantForests(Transform parent, GameConfig cfg, System.Random rng,
                                        List<Vector3> occupied, int count)
        {
            Batcher batcher = new Batcher(300f);
            float half = cfg.mapSize * 0.5f - 40f;

            int planted = 0;
            int guard = 0;

            while (planted < count && guard < count * 8)
            {
                guard++;

                float gx = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float gz = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                if (!IsFree(gx, gz, occupied, 26f, 26f)) continue;

                // densite variable : certains bosquets sont des futaies epaisses
                int clump = 8 + rng.Next(26);
                float spread = 14f + (float)rng.NextDouble() * 34f;

                for (int i = 0; i < clump && planted < count; i++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float d = spread * Mathf.Sqrt((float)rng.NextDouble());
                    float x = gx + Mathf.Cos(a) * d;
                    float z = gz + Mathf.Sin(a) * d;

                    if (Mathf.Abs(x) > half || Mathf.Abs(z) > half) continue;
                    if (DistanceToRoad(x, z) < 8f) continue;
                    if (!Plantable(x, z, 0.55f, 62f)) continue;
                    if (!IsFree(x, z, occupied, 7f, 8f)) continue;

                    AddTree(batcher, new Vector3(x, Ground.Height(x, z), z), rng);
                    planted++;
                }
            }

            batcher.Flush(parent, "Forets");
        }

        static void AddTree(Batcher b, Vector3 at, System.Random rng)
        {
            float s = 0.85f + (float)rng.NextDouble() * 0.9f;
            float spin = (float)rng.NextDouble() * 360f;
            Color bark = Palette.Shade(Palette.Trunk, 0.8f + (float)rng.NextDouble() * 0.4f);
            int species = rng.Next(3);

            if (species == 0)
            {
                // sapin : quatre etages qui retrecissent
                Color needle = Palette.Shade(Palette.Wood, 0.52f + (float)rng.NextDouble() * 0.18f);
                float trunk = 2.2f * s;
                b.Add(PrimitiveType.Cylinder, at + new Vector3(0f, trunk * 0.5f, 0f),
                      new Vector3(0.3f * s, trunk * 0.5f, 0.3f * s), bark);
                for (int i = 0; i < 4; i++)
                {
                    float t = i / 3f;
                    float w = Mathf.Lerp(3.4f, 0.9f, t) * s;
                    b.Add(PrimitiveType.Cube, at + new Vector3(0f, trunk + 0.6f + i * 1.45f * s, 0f),
                          new Vector3(w, 1.3f * s, w),
                          Quaternion.Euler(0f, spin + i * 22f, 0f),
                          Palette.Shade(needle, 1f - i * 0.06f));
                }
            }
            else if (species == 1)
            {
                // chene : houppier large en trois blocs inclines
                Color leaf = Palette.Shade(Palette.Wood, 0.72f + (float)rng.NextDouble() * 0.3f);
                float trunk = 2.8f * s;
                b.Add(PrimitiveType.Cylinder, at + new Vector3(0f, trunk * 0.5f, 0f),
                      new Vector3(0.44f * s, trunk * 0.5f, 0.44f * s), bark);
                b.Add(PrimitiveType.Cube, at + new Vector3(0f, trunk + 1.5f * s, 0f),
                      new Vector3(4.4f * s, 3.0f * s, 4.4f * s),
                      Quaternion.Euler(9f, spin, 7f), leaf);
                b.Add(PrimitiveType.Cube, at + new Vector3(1.1f * s, trunk + 3.0f * s, -0.5f * s),
                      new Vector3(2.8f * s, 2.2f * s, 2.8f * s),
                      Quaternion.Euler(-8f, spin + 40f, 12f), Palette.Shade(leaf, 1.1f));
                b.Add(PrimitiveType.Cube, at + new Vector3(-1.0f * s, trunk + 2.4f * s, 0.7f * s),
                      new Vector3(2.4f * s, 1.9f * s, 2.4f * s),
                      Quaternion.Euler(11f, spin - 30f, -9f), Palette.Shade(leaf, 0.88f));
            }
            else
            {
                // bouleau : elance et clair
                Color pale = Palette.Shade(Palette.Wood, 0.95f + (float)rng.NextDouble() * 0.25f);
                float trunk = 4.4f * s;
                b.Add(PrimitiveType.Cylinder, at + new Vector3(0f, trunk * 0.5f, 0f),
                      new Vector3(0.26f * s, trunk * 0.5f, 0.26f * s),
                      Quaternion.Euler(3f, 0f, 2f), Palette.Shade(bark, 1.7f));
                b.Add(PrimitiveType.Cube, at + new Vector3(0f, trunk + 1.2f * s, 0f),
                      new Vector3(2.6f * s, 3.4f * s, 2.6f * s),
                      Quaternion.Euler(6f, spin, 5f), pale);
                b.Add(PrimitiveType.Cube, at + new Vector3(0.4f * s, trunk + 3.2f * s, 0.2f * s),
                      new Vector3(1.8f * s, 1.9f * s, 1.8f * s),
                      Quaternion.Euler(-7f, spin + 55f, 8f), Palette.Shade(pale, 1.12f));
            }
        }

        // ================================================================ sous-bois

        /// <summary>Buissons, fougeres, hautes herbes, fleurs, cailloux, souches, troncs couches.</summary>
        public static void Scatter(Transform parent, GameConfig cfg, System.Random rng,
                                   List<Vector3> occupied, int count)
        {
            Batcher batcher = new Batcher(300f);
            float half = cfg.mapSize * 0.5f - 30f;

            int placed = 0;
            int guard = 0;

            while (placed < count && guard < count * 8)
            {
                guard++;

                float gx = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float gz = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                if (!IsFree(gx, gz, occupied, 8f, 7f)) continue;

                int clump = 2 + rng.Next(5);
                for (int i = 0; i < clump && placed < count; i++)
                {
                    float x = gx + ((float)rng.NextDouble() - 0.5f) * 18f;
                    float z = gz + ((float)rng.NextDouble() - 0.5f) * 18f;
                    if (Mathf.Abs(x) > half || Mathf.Abs(z) > half) continue;
                    if (DistanceToRoad(x, z) < 5f) continue;
                    if (!Plantable(x, z, 0.75f, 72f)) continue;

                    AddUndergrowth(batcher, new Vector3(x, Ground.Height(x, z), z),
                                   Ground.Slope(x, z), rng);
                    placed++;
                }
            }

            batcher.Flush(parent, "SousBois");
        }

        static void AddUndergrowth(Batcher b, Vector3 at, float slope, System.Random rng)
        {
            float spin = (float)rng.NextDouble() * 360f;

            if (slope > 0.35f)
            {
                AddRocks(b, at, rng);
                return;
            }

            double roll = rng.NextDouble();
            if (roll < 0.26) AddBush(b, at, rng, spin);
            else if (roll < 0.46) AddFern(b, at, rng);
            else if (roll < 0.66) AddTallGrass(b, at, rng);
            else if (roll < 0.80) AddFlowers(b, at, rng);
            else if (roll < 0.90) AddRocks(b, at, rng);
            else if (roll < 0.96) AddStump(b, at, rng, spin);
            else AddFallenLog(b, at, rng, spin);
        }

        static void AddBush(Batcher b, Vector3 at, System.Random rng, float spin)
        {
            Color leaf = Palette.Shade(Palette.Wood, 0.6f + (float)rng.NextDouble() * 0.3f);
            int blobs = 2 + rng.Next(3);
            for (int i = 0; i < blobs; i++)
            {
                float s = 0.9f + (float)rng.NextDouble() * 1.0f;
                b.Add(PrimitiveType.Cube,
                      at + new Vector3(((float)rng.NextDouble() - 0.5f) * 1.8f, s * 0.42f,
                                       ((float)rng.NextDouble() - 0.5f) * 1.8f),
                      new Vector3(s, s * 0.85f, s),
                      Quaternion.Euler(12f, spin + i * 37f, 9f), leaf);
            }
        }

        static void AddFern(Batcher b, Vector3 at, System.Random rng)
        {
            Color frond = Palette.Shade(Palette.Wood, 0.66f + (float)rng.NextDouble() * 0.2f);
            int count = 4 + rng.Next(4);
            for (int i = 0; i < count; i++)
            {
                float a = (360f / count) * i + (float)rng.NextDouble() * 20f;
                b.Add(PrimitiveType.Cube,
                      at + new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.35f, 0.34f,
                                       Mathf.Cos(a * Mathf.Deg2Rad) * 0.35f),
                      new Vector3(0.18f, 0.08f, 1.15f),
                      Quaternion.Euler(-34f, a, 0f), frond);
            }
        }

        static void AddTallGrass(Batcher b, Vector3 at, System.Random rng)
        {
            Color tone = Palette.Shade(Palette.Grass1, 1.0f + (float)rng.NextDouble() * 0.25f);
            int blades = 5 + rng.Next(6);
            for (int i = 0; i < blades; i++)
            {
                float h = 0.6f + (float)rng.NextDouble() * 0.8f;
                b.Add(PrimitiveType.Cube,
                      at + new Vector3(((float)rng.NextDouble() - 0.5f) * 2.2f, h * 0.5f,
                                       ((float)rng.NextDouble() - 0.5f) * 2.2f),
                      new Vector3(0.1f, h, 0.1f),
                      Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 30f,
                                       (float)rng.NextDouble() * 360f,
                                       ((float)rng.NextDouble() - 0.5f) * 30f), tone);
            }
        }

        static void AddFlowers(Batcher b, Vector3 at, System.Random rng)
        {
            Color[] palette = { Palette.Flower1, Palette.Flower2, Palette.Flower3 };
            int count = 4 + rng.Next(5);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = at + new Vector3(((float)rng.NextDouble() - 0.5f) * 3f, 0f,
                                             ((float)rng.NextDouble() - 0.5f) * 3f);
                b.Add(PrimitiveType.Cube, p + new Vector3(0f, 0.26f, 0f),
                      new Vector3(0.05f, 0.5f, 0.05f), Palette.Shade(Palette.Wood, 0.8f));
                b.Add(PrimitiveType.Cube, p + new Vector3(0f, 0.55f, 0f),
                      new Vector3(0.22f, 0.12f, 0.22f),
                      Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f),
                      palette[rng.Next(palette.Length)]);
            }
        }

        static void AddRocks(Batcher b, Vector3 at, System.Random rng)
        {
            int count = 1 + rng.Next(4);
            for (int i = 0; i < count; i++)
            {
                float s = 0.6f + (float)rng.NextDouble() * 1.7f;
                b.Add(PrimitiveType.Cube,
                      at + new Vector3(((float)rng.NextDouble() - 0.5f) * 2.4f, s * 0.28f,
                                       ((float)rng.NextDouble() - 0.5f) * 2.4f),
                      new Vector3(s, s * 0.7f, s * 0.9f),
                      Quaternion.Euler((float)rng.NextDouble() * 34f, (float)rng.NextDouble() * 360f,
                                       (float)rng.NextDouble() * 34f),
                      Palette.Shade(Palette.Rock1, 0.75f + (float)rng.NextDouble() * 0.45f));
            }
        }

        static void AddStump(Batcher b, Vector3 at, System.Random rng, float spin)
        {
            float h = 0.45f + (float)rng.NextDouble() * 0.4f;
            b.Add(PrimitiveType.Cylinder, at + new Vector3(0f, h * 0.5f, 0f),
                  new Vector3(0.8f, h * 0.5f, 0.8f), Palette.Trunk);
            b.Add(PrimitiveType.Cylinder, at + new Vector3(0f, h, 0f),
                  new Vector3(0.74f, 0.03f, 0.74f), Palette.Shade(Palette.Trunk, 1.4f));
        }

        static void AddFallenLog(Batcher b, Vector3 at, System.Random rng, float spin)
        {
            float length = 2.4f + (float)rng.NextDouble() * 2.6f;
            b.Add(PrimitiveType.Cylinder, at + new Vector3(0f, 0.42f, 0f),
                  new Vector3(0.8f, length * 0.5f, 0.8f),
                  Quaternion.Euler(90f, spin, 0f), Palette.Shade(Palette.Trunk, 0.85f));
            b.Add(PrimitiveType.Cube, at + new Vector3(0.4f, 0.8f, 0.3f),
                  new Vector3(0.7f, 0.25f, 0.7f),
                  Quaternion.Euler(0f, spin + 20f, 0f), Palette.Shade(Palette.Wood, 0.55f));
        }

        // ================================================================ ciel

        public static void BuildClouds(Transform parent, GameConfig cfg, System.Random rng)
        {
            GameObject root = new GameObject("Nuages");
            root.transform.SetParent(parent, false);

            Color white = new Color(0.97f, 0.97f, 0.99f);
            float span = cfg.mapSize * 0.7f;

            Proto.BeginVisualOnly();
            for (int i = 0; i < 34; i++)
            {
                GameObject cloud = new GameObject("Nuage");
                cloud.transform.SetParent(root.transform, false);
                cloud.transform.position = new Vector3(
                    (float)(rng.NextDouble() * 2.0 - 1.0) * span,
                    260f + (float)rng.NextDouble() * 150f,
                    (float)(rng.NextDouble() * 2.0 - 1.0) * span);

                int puffs = 3 + rng.Next(4);
                float scale = 44f + (float)rng.NextDouble() * 70f;
                for (int p = 0; p < puffs; p++)
                {
                    Proto.Cube(cloud.transform,
                        new Vector3(((float)rng.NextDouble() - 0.5f) * scale * 1.7f,
                                    ((float)rng.NextDouble() - 0.5f) * scale * 0.2f,
                                    ((float)rng.NextDouble() - 0.5f) * scale * 1.1f),
                        new Vector3(scale * (0.6f + (float)rng.NextDouble() * 0.6f),
                                    scale * 0.3f,
                                    scale * (0.6f + (float)rng.NextDouble() * 0.5f)),
                        white, "Masse").transform.localRotation =
                        Quaternion.Euler(0f, (float)rng.NextDouble() * 60f, 0f);
                }

                Drift drift = cloud.AddComponent<Drift>();
                drift.velocity = new Vector3(2.2f + (float)rng.NextDouble() * 2.4f, 0f, 0.5f);
                drift.wrapDistance = span * 1.3f;
            }
            Proto.EndVisualOnly();
        }

        // ================================================================ grands reperes

        static readonly Vector2[] Marks =
        {
            new Vector2(-445f, -415f),
            new Vector2(-145f, 605f),
            new Vector2(200f, -715f),
            new Vector2(575f, 185f),
            new Vector2(455f, -430f),
            new Vector2(-580f, 170f),
            new Vector2(-760f, -190f),
            new Vector2(200f, 770f)
        };

        public static void BuildLandmarks(Transform parent, GameConfig cfg, List<Vector3> occupied)
        {
            GameObject root = new GameObject("Reperes");
            root.transform.SetParent(parent, false);

            StoneCircle(root.transform, new Vector2(-445f, -415f));
            StoneCircle(root.transform, new Vector2(-145f, 605f));
            RuinedArch(root.transform, new Vector2(200f, -715f));
            RuinedArch(root.transform, new Vector2(575f, 185f));
            RuinedArch(root.transform, new Vector2(455f, -430f));
            DeadTree(root.transform, new Vector2(-580f, 170f));
            DeadTree(root.transform, new Vector2(-760f, -190f));
            DeadTree(root.transform, new Vector2(200f, 770f));

            for (int i = 0; i < 8; i++) occupied.Add(Ground.Place(Marks[i].x, Marks[i].y, 0f));
        }

        static void StoneCircle(Transform parent, Vector2 centre)
        {
            GameObject go = new GameObject("PierresLevees");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Place(centre.x, centre.y, 0f);

            for (int i = 0; i < 9; i++)
            {
                float angle = (360f / 9f) * i * Mathf.Deg2Rad;
                float radius = 9f;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                float h = 5.2f + (i % 3) * 1.1f;

                Vector3 local = Ground.Place(centre.x + x, centre.y + z, 0f) - go.transform.position;
                GameObject stone = Proto.Cube(go.transform, local + new Vector3(0f, h * 0.5f, 0f),
                                              new Vector3(1.7f, h, 1.1f),
                                              Palette.Shade(Palette.Rock1, 0.82f), "Menhir");
                stone.transform.localRotation = Quaternion.Euler(
                    (i % 2 == 0) ? 4f : -3f, -Mathf.Rad2Deg * angle, (i % 3 == 0) ? 3f : -2f);
            }
        }

        static void RuinedArch(Transform parent, Vector2 centre)
        {
            GameObject go = new GameObject("RuineAncienne");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Place(centre.x, centre.y, 0f);

            Color stone = Palette.Shade(Palette.Structure, 0.8f);
            Proto.Cube(go.transform, new Vector3(-4.2f, 4.2f, 0f), new Vector3(2f, 8.4f, 2f), stone, "Pilier");
            Proto.Cube(go.transform, new Vector3(4.2f, 3.6f, 0f), new Vector3(2f, 7.2f, 2f), stone, "Pilier2");
            GameObject lintel = Proto.Cube(go.transform, new Vector3(0f, 8.8f, 0f),
                                           new Vector3(10.6f, 1.5f, 1.8f), stone, "Linteau");
            lintel.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            GameObject fallen = Proto.Cube(go.transform, new Vector3(7.2f, 0.8f, 2.6f),
                                           new Vector3(5.2f, 1.5f, 1.8f), stone, "Bloc");
            fallen.transform.localRotation = Quaternion.Euler(0f, 24f, 8f);
        }

        static void DeadTree(Transform parent, Vector2 centre)
        {
            GameObject go = new GameObject("ArbreMort");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Place(centre.x, centre.y, 0f);

            Color bark = Palette.Shade(Palette.Trunk, 0.66f);
            Proto.Cylinder(go.transform, new Vector3(0f, 5.2f, 0f), new Vector3(1f, 5.2f, 1f), bark, "Tronc");
            for (int i = 0; i < 4; i++)
            {
                GameObject branch = Proto.Cylinder(go.transform,
                    new Vector3(0f, 7.4f + i * 1.3f, 0f), new Vector3(0.34f, 2.4f, 0.34f), bark, "Branche");
                branch.transform.localRotation = Quaternion.Euler(0f, i * 95f, 54f - i * 8f);
            }
        }
    }
}
