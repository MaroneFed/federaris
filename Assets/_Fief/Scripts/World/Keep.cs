using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'INTERIEUR DU DONJON (26/09 -- Martin : "le chateau, faut vraiment
    /// l'ameliorer. Faut mettre des etages, faut qu'il y ait plein de gardes").
    ///
    ///   TERRASSE  19,8 m   a ciel ouvert. LA COURONNE (★40), et un garde.
    ///   ETAGE 2   13,5 m   la salle des coffres : un coffret, un calice.
    ///   ETAGE 1    7,2 m   l'armurerie : deux coffrets.
    ///   REZ        0,9 m   la grande salle : deux calices, les tables du banquet.
    ///
    /// Les etages sont relies par des ESCALIERS droits, alternes : le long du mur
    /// nord, puis du mur sud, puis du nord. Pour monter a la couronne, il faut
    /// traverser chaque salle -- et chaque salle a son garde. Des garde-corps
    /// bordent les tremies : on ne tombe pas dans la cage d'escalier.
    ///
    /// Vu de dessus (x vers l'est, z vers le nord), interieur de 16,8 x 12,8 m :
    ///
    ///     z 25.4 +--------------------------------+
    ///            | escalier (nord) ----------->   | palier
    ///     z 22.9 +---------- garde-corps ---------+
    ///            |                                |
    ///            |           la salle             |
    ///     z 15.0 +--------------------------------+
    ///      palier|   <----------- escalier (sud)  |
    ///     z 12.6 +------------ porte -------------+
    ///          x -8.4                           x 8.4
    ///
    /// Tout est fait de cubes : planchers et escaliers ont un collider, le decor non.
    /// </summary>
    public static class Keep
    {
        public const float L0 = 0.9f;
        public const float L1 = 7.2f;
        public const float L2 = 13.5f;
        public const float Roof = 19.8f;

        const float Slab = 0.4f;
        const float Strip = 2.4f;          // largeur des escaliers
        const float RampEnd = 6.2f;        // les escaliers vont de x = -6,2 a x = +6,2

        /// <summary>Les rondes des gardes du donjon, une par niveau (remplies par Build).</summary>
        public static readonly List<Vector3[]> GuardRoutes = new List<Vector3[]>();

        static readonly Color Paving = new Color(0.20f, 0.20f, 0.19f);
        static readonly Color Planks = new Color(0.27f, 0.2f, 0.14f);
        static readonly Color PlanksDark = new Color(0.19f, 0.14f, 0.1f);
        static readonly Color StoneDark = new Color(0.23f, 0.23f, 0.23f);
        static readonly Color Iron = new Color(0.14f, 0.14f, 0.15f);
        static readonly Color Crimson = new Color(0.45f, 0.1f, 0.1f);

        static float X0 { get { return Castle.KeepCentre.x - Castle.KeepHalfWidth + 1.6f; } }
        static float X1 { get { return Castle.KeepCentre.x + Castle.KeepHalfWidth - 1.6f; } }
        static float Z0 { get { return Castle.KeepCentre.z - Castle.KeepHalfDepth + 1.6f; } }
        static float Z1 { get { return Castle.KeepCentre.z + Castle.KeepHalfDepth - 1.6f; } }
        static float NorthEdge { get { return Z1 - Strip - 0.1f; } }
        static float SouthEdge { get { return Z0 + Strip; } }

        public static void Build(Transform t)
        {
            GuardRoutes.Clear();
            float north = (NorthEdge + Z1) * 0.5f;
            float south = (Z0 + SouthEdge) * 0.5f;

            // --- les planchers, perces de leurs tremies
            // Etage 1 et terrasse : tremie au nord (l'escalier qui y monte), palier a l'est.
            FloorWithNorthHole(t, L1, "Plancher étage 1");
            FloorWithSouthHole(t, L2, "Plancher étage 2");
            FloorWithNorthHole(t, Roof, "Terrasse");

            // --- les escaliers
            Stair(t, new Vector3(-RampEnd, L0, north), new Vector3(RampEnd, L1, north));
            Stair(t, new Vector3(RampEnd, L1, south), new Vector3(-RampEnd, L2, south));
            Stair(t, new Vector3(-RampEnd, L2, north), new Vector3(RampEnd, Roof, north));

            // --- les garde-corps le long des tremies
            Rail(t, new Vector3(X0, L1, NorthEdge), new Vector3(RampEnd, L1, NorthEdge));
            Rail(t, new Vector3(-RampEnd, L2, SouthEdge), new Vector3(X1, L2, SouthEdge));
            Rail(t, new Vector3(X0, Roof, NorthEdge), new Vector3(RampEnd, Roof, NorthEdge));

            // --- chaque niveau : son decor, sa lumiere, ses tresors, sa ronde
            GreatHall(t);
            Armoury(t);
            Vault(t);
            Terrace(t);
        }

        // ================================================================== le chemin

        /// <summary>Le niveau d'une hauteur : 0 (rez), 1, 2, 3 (terrasse).</summary>
        public static int LevelOf(float y)
        {
            if (y > Roof - 1.5f) return 3;
            if (y > L2 - 1.5f) return 2;
            if (y > L1 - 1.5f) return 1;
            return 0;
        }

        /// <summary>
        /// Le chemin d'un rival, du parvis du donjon jusqu'au niveau voulu : entrer,
        /// traverser la salle par l'allee du tapis, prendre chaque escalier par son
        /// pied. (Pas de NavMesh : quelques points choisis a la main, qui evitent les
        /// piliers, les tables et les tresors.) Pour redescendre, on le lit a l'envers.
        /// </summary>
        public static List<Vector3> PathTo(int level)
        {
            float north = (NorthEdge + Z1) * 0.5f;
            float south = (Z0 + SouthEdge) * 0.5f;
            float east = X1 - 1f, west = X0 + 1f;
            List<Vector3> p = new List<Vector3>();
            p.Add(At(0f, 0f, Z0 - 4.5f));                       // le parvis, au pied du perron
            p.Add(At(0f, L0, Z0 + 0.4f));                       // le seuil
            p.Add(At(0f, L0, Castle.KeepCentre.z + 2.2f));      // l'allee, entre les tables
            if (level <= 0) return p;
            p.Add(At(west, L0, NorthEdge - 0.6f));
            p.Add(At(west, L0, north));                         // le pied du premier escalier
            p.Add(At(east, L1, north));                         // son palier
            p.Add(At(east, L1, NorthEdge - 1.2f));
            if (level == 1) return p;
            p.Add(At(east, L1, south));                         // le pied du deuxieme
            p.Add(At(west, L2, south));
            p.Add(At(west, L2, SouthEdge + 1.2f));
            if (level == 2) return p;
            p.Add(At(west, L2, north));                         // le pied du troisieme
            p.Add(At(east, Roof, north));
            p.Add(At(east, Roof, NorthEdge - 1.2f));
            return p;
        }

        // ================================================================== planchers

        static void Slabs(Transform t, float level, float xa, float xb, float za, float zb, string name, bool wood)
        {
            if (xb - xa < 0.05f || zb - za < 0.05f) return;
            Proto.Cube(t, new Vector3((xa + xb) * 0.5f, level - Slab * 0.5f, (za + zb) * 0.5f),
                       new Vector3(xb - xa, Slab, zb - za), wood ? Planks : Paving, name);
            if (!wood) return;
            // Des lames de parquet, un ton plus sombre, pour qu'on lise le sol.
            Proto.BeginVisualOnly();
            for (float x = xa + 0.9f; x < xb - 0.2f; x += 1.1f)
                Proto.Cube(t, new Vector3(x, level + 0.005f, (za + zb) * 0.5f), new Vector3(0.05f, 0.01f, zb - za - 0.1f), PlanksDark, "Lame");
            Proto.EndVisualOnly();
        }

        static void FloorWithNorthHole(Transform t, float level, string name)
        {
            // La salle entiere au sud de la tremie, et le palier au nord-est.
            Slabs(t, level, X0, X1, Z0, NorthEdge, name, level < Roof);
            Slabs(t, level, RampEnd, X1, NorthEdge, Z1, name + " (palier)", level < Roof);
        }

        static void FloorWithSouthHole(Transform t, float level, string name)
        {
            Slabs(t, level, X0, X1, SouthEdge, Z1, name, true);
            Slabs(t, level, X0, -RampEnd, Z0, SouthEdge, name + " (palier)", true);
        }

        // ================================================================== escaliers

        /// <summary>
        /// Un escalier droit de "from" (bas) a "to" (haut) : une rampe pleine (le
        /// collider, qu'un CharacterController monte sans broncher) et des marches
        /// dessinees dessus.
        /// </summary>
        static void Stair(Transform t, Vector3 from, Vector3 to)
        {
            Vector3 run = to - from;
            float length = run.magnitude + 0.4f;
            Vector3 mid = (from + to) * 0.5f - new Vector3(0f, 0.17f, 0f);
            GameObject ramp = Proto.Cube(t, mid, new Vector3(Strip, 0.34f, length), StoneDark, "Escalier");
            ramp.transform.localRotation = Quaternion.LookRotation(run.normalized, Vector3.up);

            // Les marches, et le limon de bois sur le cote libre.
            Proto.BeginVisualOnly();
            int steps = Mathf.RoundToInt(run.magnitude / 0.42f);
            Vector3 flatDir = new Vector3(run.x, 0f, run.z).normalized;
            for (int i = 0; i < steps; i++)
            {
                float u = (i + 0.5f) / steps;
                Vector3 p = Vector3.Lerp(from, to, u) + Vector3.up * 0.02f;
                GameObject step = Proto.Cube(t, p, new Vector3(Strip - 0.1f, 0.06f, 0.1f), Paving, "Nez de marche");
                step.transform.localRotation = Quaternion.LookRotation(flatDir, Vector3.up);
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Un garde-corps de bois (colliders), d'un metre, avec ses poteaux.</summary>
        static void Rail(Transform t, Vector3 a, Vector3 b)
        {
            Vector3 mid = (a + b) * 0.5f;
            Vector3 d = b - a;
            GameObject bar = Proto.Cube(t, mid + Vector3.up * 1.0f, new Vector3(0.14f, 0.12f, d.magnitude), Planks, "Garde-corps");
            bar.transform.localRotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            // Un pan bas, plein : il arrete aussi bien un joueur qu'un garde en course.
            GameObject low = Proto.Cube(t, mid + Vector3.up * 0.5f, new Vector3(0.1f, 1.0f, d.magnitude), PlanksDark, "Garde-corps");
            low.transform.localRotation = bar.transform.localRotation;
            Proto.BeginVisualOnly();
            int posts = Mathf.Max(2, Mathf.RoundToInt(d.magnitude / 1.6f) + 1);
            for (int i = 0; i < posts; i++)
                Proto.Cube(t, Vector3.Lerp(a, b, i / (float)(posts - 1)) + Vector3.up * 0.55f, new Vector3(0.16f, 1.1f, 0.16f), PlanksDark, "Poteau");
            Proto.EndVisualOnly();
        }

        // ================================================================== les salles

        static Vector3 At(float x, float level, float z) { return new Vector3(x, level, z); }

        /// <summary>Une applique : un bras de fer, une flamme, une lumiere.</summary>
        static void Sconce(Transform t, Vector3 at, Vector3 intoRoom)
        {
            Proto.BeginVisualOnly();
            Proto.Cube(t, at - intoRoom * 0.15f, new Vector3(0.12f, 0.5f, 0.12f), Iron, "Applique");
            Proto.Cube(t, at + Vector3.up * 0.3f, new Vector3(0.26f, 0.08f, 0.26f), Iron, "Coupe");
            GameObject flame = Proto.Cube(t, at + Vector3.up * 0.48f, new Vector3(0.18f, 0.3f, 0.18f), Color.white, "Flamme");
            flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.6f, 0.22f), 2.2f);
            flame.AddComponent<Flame>();
            Proto.EndVisualOnly();
            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = at + Vector3.up * 0.7f + intoRoom * 0.5f;
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.38f);
            light.intensity = 1.4f;
            light.range = 9f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
        }

        /// <summary>Deux appliques par salle, sur les murs est et ouest.</summary>
        static void Lamps(Transform t, float level)
        {
            float z = Castle.KeepCentre.z;
            Sconce(t, At(X0 + 0.1f, level + 2.4f, z), Vector3.right);
            Sconce(t, At(X1 - 0.1f, level + 2.4f, z), Vector3.left);
        }

        /// <summary>Un ratelier d'armes contre un mur : des lances, des epees, des boucliers.</summary>
        static void Rack(Transform t, Vector3 at, bool alongX)
        {
            Proto.BeginVisualOnly();
            Vector3 along = alongX ? Vector3.right : Vector3.forward;
            Proto.Cube(t, at + Vector3.up * 0.3f, alongX ? new Vector3(2.2f, 0.1f, 0.3f) : new Vector3(0.3f, 0.1f, 2.2f), Planks, "Râtelier");
            Proto.Cube(t, at + Vector3.up * 1.5f, alongX ? new Vector3(2.2f, 0.1f, 0.3f) : new Vector3(0.3f, 0.1f, 2.2f), Planks, "Râtelier");
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = at + along * (-0.9f + i * 0.45f);
                Proto.Cube(t, p + Vector3.up * 1.1f, new Vector3(0.05f, 2.2f, 0.05f), PlanksDark, "Lance");
                Proto.Cone(t, p + Vector3.up * 2.2f, 0.05f, 0.25f, new Color(0.5f, 0.5f, 0.52f), "Fer de lance", 4);
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Le rez-de-chaussee : la grande salle, deux calices, un garde.</summary>
        static void GreatHall(Transform t)
        {
            float c = Castle.KeepCentre.z;
            // Quatre piliers qui portent l'etage (deux rangees) : ils cachent, on s'y cache.
            float[] rows = { c - 4.5f, c - 0.2f };
            for (int i = 0; i < rows.Length; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    Proto.Cube(t, At(side * 4.8f, (L0 + L1) * 0.5f, rows[i]), new Vector3(1.1f, L1 - L0, 1.1f), StoneDark, "Pilier");
                    Proto.BeginVisualOnly();
                    Proto.Cube(t, At(side * 4.8f, L0 + 0.3f, rows[i]), new Vector3(1.5f, 0.6f, 1.5f), Paving, "Base");
                    Proto.EndVisualOnly();
                }
            // Le tapis rouge, de la porte au pied de l'escalier.
            Proto.BeginVisualOnly();
            Proto.Cube(t, At(0f, L0 + 0.02f, (Z0 + NorthEdge) * 0.5f), new Vector3(2.2f, 0.04f, NorthEdge - Z0 - 0.4f), Crimson, "Tapis");
            Proto.EndVisualOnly();
            Lamps(t, L0);

            Treasure.Build(t, At(-7.3f, L0, Z0 + 1.2f), Treasure.Kind.Calice, true);
            Treasure.Build(t, At(7.3f, L0, Z0 + 1.2f), Treasure.Kind.Calice, true);

            // Une ronde en U, le long des murs : elle ne traverse pas les tables du banquet.
            float y0 = L0 + 0.05f;
            GuardRoutes.Add(new[] { At(-6f, y0, Z0 + 1.6f), At(-6f, y0, c + 2.2f), At(6f, y0, c + 2.2f), At(6f, y0, Z0 + 1.6f), At(6f, y0, c + 2.2f), At(-6f, y0, c + 2.2f) });
        }

        /// <summary>Le premier etage : l'armurerie. Des rateliers, deux coffrets, un garde.</summary>
        static void Armoury(Transform t)
        {
            float c = Castle.KeepCentre.z;
            Rack(t, At(-2.5f, L1, NorthEdge - 0.5f), true);
            Rack(t, At(2.5f, L1, NorthEdge - 0.5f), true);
            Proto.BeginVisualOnly();
            // Des mannequins d'armure, des boucliers aux murs.
            for (int side = -1; side <= 1; side += 2)
                for (int k = 0; k < 2; k++)
                {
                    Vector3 p = At(side * (X1 - 0.08f), L1 + 2.2f, c - 1f + k * 3.2f);
                    Proto.Cube(t, p, new Vector3(0.06f, 0.9f, 0.8f), k == 0 ? Crimson : new Color(0.72f, 0.58f, 0.25f), "Bouclier");
                }
            Proto.EndVisualOnly();
            Lamps(t, L1);

            // (Le couloir le long du mur est reste libre : c'est le chemin de l'escalier suivant.)
            Treasure.Build(t, At(-7.3f, L1, c + 1.5f), Treasure.Kind.Coffret, true);
            Treasure.Build(t, At(3f, L1, c), Treasure.Kind.Coffret, true);

            GuardRoutes.Add(new[] { At(-5.8f, L1 + 0.05f, SouthEdge + 1.6f), At(5.8f, L1 + 0.05f, SouthEdge + 1.6f), At(5.8f, L1 + 0.05f, NorthEdge - 1.4f), At(-5.8f, L1 + 0.05f, NorthEdge - 1.4f) });
        }

        /// <summary>Le deuxieme etage : la salle des coffres. Un coffret, un calice, des tonneaux.</summary>
        static void Vault(Transform t)
        {
            float c = Castle.KeepCentre.z;
            Proto.BeginVisualOnly();
            for (int i = 0; i < 4; i++)
            {
                Proto.Cylinder(t, At(X1 - 0.7f, L2 + 0.55f, Z1 - 0.7f - i * 1.1f), new Vector3(0.9f, 0.55f, 0.9f), PlanksDark, "Tonneau");
            }
            Proto.EndVisualOnly();
            Proto.Blocker(t, At(X1 - 0.7f, L2 + 0.55f, Z1 - 2.35f), new Vector3(1f, 1.1f, 4.4f), "Tonneaux");
            Lamps(t, L2);

            // (Le couloir le long du mur ouest reste libre : c'est le chemin de la terrasse.)
            Treasure.Build(t, At(-3f, L2, c + 0.5f), Treasure.Kind.Coffret, true);
            Treasure.Build(t, At(4.5f, L2, c + 0.5f), Treasure.Kind.Calice, true);

            GuardRoutes.Add(new[] { At(-5.5f, L2 + 0.05f, SouthEdge + 1.4f), At(5.5f, L2 + 0.05f, SouthEdge + 1.4f), At(5.5f, L2 + 0.05f, c + 3.4f), At(-5.5f, L2 + 0.05f, c + 3.4f) });
        }

        /// <summary>La terrasse, a ciel ouvert : la couronne sur son socle, deux braseros, un garde.</summary>
        static void Terrace(Transform t)
        {
            float c = Castle.KeepCentre.z;
            Treasure.Build(t, At(1.5f, Roof, c), Treasure.Kind.Couronne, true);
            // Deux braseros : la couronne se voit briller d'en bas, par-dessus les creneaux.
            Castle.Torch(t, At(-1.5f, Roof, c - 3.5f), 1.4f);
            Castle.Torch(t, At(4.5f, Roof, c - 3.5f), 1.4f);

            float yr = Roof + 0.05f;
            GuardRoutes.Add(new[] { At(-3.2f, yr, c - 4.5f), At(6.5f, yr, c - 4.5f), At(6.5f, yr, NorthEdge - 1.2f), At(-3.2f, yr, NorthEdge - 1.2f) });
        }
    }
}
