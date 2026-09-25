using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES LIEUX-DITS. Cinq endroits de la sylve qu'on n'oublie pas :
    ///
    ///   LE GRAND CHENE      un hetre trois fois et demie plus grand que les autres,
    ///                       dont le haut se perd dans la brume ;
    ///   LE CERCLE DE PIERRES neuf pierres dressees, runes bleues, un autel ;
    ///   LA CABANE DU BRACONNIER, toit creve, peaux pendues, table encore mise ;
    ///   LE TERTRE           une butte funeraire, et sa porte de pierre ;
    ///   LA TOUR EFFONDREE   ce qui reste d'une tour de guet (l'Ermite y vit).
    ///
    /// Dans une foret ou tout se ressemble a vingt metres, les lieux-dits sont ce
    /// qui permet de SE REPERER : une fois decouvert, un lieu-dit apparait a l'ecran
    /// comme ton camp, et on peut dire "ma cache est entre le Chene et le Cercle".
    ///
    /// ORDRE DE CONSTRUCTION (voir GameBootstrap) : Find() choisit les places AVANT
    /// la foret, pour qu'elle leur laisse une clairiere ; Build() les construit
    /// APRES, une fois le sol et les arbres poses.
    /// </summary>
    public static class Landmarks
    {
        public enum Kind { GrandChene, Cercle, Cabane, Tertre, Tour }

        struct Spot
        {
            public Kind kind;
            public Vector2 at;
            public float clear;
        }

        static readonly List<Spot> Spots = new List<Spot>();
        public static readonly List<Landmark> All = new List<Landmark>();

        /// <summary>La Tour effondree : l'Ermite s'y installe (null si elle n'a pas trouve de place).</summary>
        public static Transform Tour { get; private set; }

        public static string Name(Kind kind)
        {
            switch (kind)
            {
                case Kind.GrandChene: return "Le Grand Chêne";
                case Kind.Cercle: return "Le Cercle de pierres";
                case Kind.Cabane: return "La cabane du braconnier";
                case Kind.Tertre: return "Le Tertre";
                default: return "La Tour effondrée";
            }
        }

        static float ClearRadius(Kind kind)
        {
            switch (kind)
            {
                case Kind.GrandChene: return 13f;
                case Kind.Cercle: return 13f;
                case Kind.Cabane: return 9f;
                case Kind.Tertre: return 14f;
                default: return 11f;
            }
        }

        // ================================================================== placement

        /// <summary>
        /// Choisit cinq places, loin les unes des autres (150 m au moins), loin du
        /// chateau et des creux, sur un sol presque plat. Meme graine, memes places.
        /// </summary>
        public static void Find(GameConfig cfg)
        {
            Spots.Clear();
            All.Clear();
            Tour = null;
            int seed = cfg != null ? cfg.worldSeed : 1;
            System.Random rng = new System.Random(seed * 13 + 5);
            // Pas dans le bourrelet du bord de carte (ou le sol remonte) : 80 m de marge.
            float half = (cfg != null ? cfg.mapSize : 420f) * 0.5f - 40f;

            Kind[] kinds = { Kind.GrandChene, Kind.Cercle, Kind.Cabane, Kind.Tertre, Kind.Tour };
            for (int k = 0; k < kinds.Length; k++)
            {
                for (float spacing = 110f; spacing >= 50f; spacing -= 20f)
                {
                    if (TryPlace(kinds[k], rng, half, spacing)) break;
                }
            }
        }

        static bool TryPlace(Kind kind, System.Random rng, float half, float spacing)
        {
            float clear = ClearRadius(kind);
            for (int i = 0; i < 500; i++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                if (new Vector2(x, z).magnitude < 95f) continue;
                if (Castle.Covers(x, z, clear + 10f) || Monument.Near(x, z, clear + 10f)) continue;
                if (Gathering.NearHollow(x, z, clear + 12f)) continue;
                if (Ground.Slope(x, z) > 0.22f) continue;

                bool far = true;
                for (int s = 0; s < Spots.Count; s++)
                    if ((Spots[s].at - new Vector2(x, z)).magnitude < spacing) { far = false; break; }
                if (!far) continue;

                Spot spot = new Spot();
                spot.kind = kind;
                spot.at = new Vector2(x, z);
                spot.clear = clear;
                Spots.Add(spot);
                return true;
            }
            return false;
        }

        /// <summary>Vrai si ce point tombe dans la clairiere d'un lieu-dit (plus une marge).</summary>
        public static bool Near(float x, float z, float margin)
        {
            for (int i = 0; i < Spots.Count; i++)
            {
                float r = Spots[i].clear + margin;
                if ((Spots[i].at - new Vector2(x, z)).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        // ================================================================== construction

        public static void Build(Transform worldRoot, GameConfig cfg)
        {
            GameObject rootGo = new GameObject("LIEUX-DITS");
            rootGo.transform.SetParent(worldRoot, false);
            Transform root = rootGo.transform;
            System.Random rng = new System.Random((cfg != null ? cfg.worldSeed : 1) * 17 + 11);

            for (int i = 0; i < Spots.Count; i++)
            {
                Spot s = Spots[i];
                Vector3 at = Ground.Place(s.at.x, s.at.y, 0f);
                GameObject go = new GameObject(Name(s.kind).ToUpperInvariant());
                go.transform.SetParent(root, false);
                go.transform.localPosition = at;
                go.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                Landmark mark = go.AddComponent<Landmark>();
                mark.kind = s.kind;
                All.Add(mark);

                switch (s.kind)
                {
                    case Kind.GrandChene: GrandChene(go.transform, rng); break;
                    case Kind.Cercle: Cercle(go.transform, rng); break;
                    case Kind.Cabane: Cabane(go.transform, rng); break;
                    case Kind.Tertre: Tertre(go.transform, rng); break;
                    default:
                        BuildTour(go.transform, rng);
                        Masonry.Apply(go.transform);        // la tour en pierres taillees
                        Tour = go.transform;
                        break;
                }
            }

            // Les deux talismans du chateau.
            Transform castle = root;
            Vector3 throne = new Vector3(0f, Castle.HallFloor + 1.75f,
                                         Castle.KeepCentre.z + Castle.KeepHalfDepth - 1.6f - 0.75f);
            TalismanPickup.Build(castle, throne, Talisman.Lanterne);
            // Aux pieds du roi sans tete (troisieme paire, a gauche), au bord de l'allee.
            TalismanPickup.Build(castle, new Vector3(-3.9f, Ground.Sample(-3.9f, -74f) + 1.0f, -74f), Talisman.Couronne);
        }

        // ------------------------------------------------------------------ le Grand Chene

        static void GrandChene(Transform t, System.Random rng)
        {
            const float scale = 3.4f;
            TreeInfo info;
            Mesh mesh = TreeMesh.Build(TreeKind.Beech, 777, out info);
            GameObject tree = new GameObject("Grand Chêne");
            tree.transform.SetParent(t, false);
            tree.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            tree.transform.localScale = Vector3.one * scale;
            tree.AddComponent<MeshFilter>().sharedMesh = mesh;
            tree.AddComponent<MeshRenderer>().sharedMaterials = Forest.TreeMaterials(Palette.DarkBarks[0], Palette.DarkLeaves[0]);
            CapsuleCollider trunk = tree.AddComponent<CapsuleCollider>();
            trunk.radius = info.trunkRadius * 1.05f;
            trunk.height = 4f;
            trunk.center = new Vector3(info.trunkCentre.x, 2f, info.trunkCentre.z);

            // Le creux dans le tronc, cote sud, ou dort la Corne.
            float r = info.trunkRadius * scale;
            Vector3 centre = new Vector3(info.trunkCentre.x * scale, 0f, info.trunkCentre.z * scale);
            Proto.BeginVisualOnly();
            Proto.Cube(t, centre + new Vector3(0f, 1.3f, -r + 0.05f), new Vector3(r * 0.8f, 1.9f, 0.3f), new Color(0.03f, 0.03f, 0.03f), "Creux");

            // Un anneau de champignons pales qui luisent a peine. On ne sait pas s'ils
            // sont la parce que l'arbre est magique, ou l'inverse.
            Material glow = MaterialFactory.GetGlow(new Color(0.72f, 0.92f, 0.62f), 0.9f);
            for (int i = 0; i < 18; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = r + 1.2f + (float)rng.NextDouble() * 4f;
                Vector3 p = centre + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                float h = 0.1f + (float)rng.NextDouble() * 0.14f;
                Proto.Cube(t, p + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.05f, h, 0.05f), new Color(0.8f, 0.8f, 0.72f), "Pied");
                GameObject cap = Proto.Cube(t, p + new Vector3(0f, h, 0f), new Vector3(0.16f, 0.06f, 0.16f), Color.white, "Chapeau");
                cap.GetComponent<Renderer>().sharedMaterial = glow;
            }
            Proto.EndVisualOnly();

            TalismanPickup.Build(t, centre + new Vector3(0f, 1.3f, -r - 0.7f), Talisman.Corne);
            Ambiance.Fireflies(t, t.TransformPoint(centre + new Vector3(0f, 2.5f, 0f)), 900);
        }

        // ------------------------------------------------------------------ le Cercle

        static void Cercle(Transform t, System.Random rng)
        {
            Color stone = new Color(0.33f, 0.34f, 0.33f);
            Color stoneDark = new Color(0.25f, 0.26f, 0.26f);
            Color rune = new Color(0.55f, 0.75f, 1f);
            Material runeGlow = MaterialFactory.GetGlow(rune, 1.8f);

            const int count = 9;
            const float radius = 7f;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float h = 3f + (float)rng.NextDouble() * 1.4f;
                GameObject s = Proto.Cube(t, dir * radius + new Vector3(0f, h * 0.5f - 0.3f, 0f),
                                          new Vector3(1.1f, h, 0.7f), i % 2 == 0 ? stone : stoneDark, "Pierre dressée");
                s.transform.localRotation = Quaternion.LookRotation(-dir, Vector3.up)
                                            * Quaternion.Euler(R(rng, -5f, 5f), R(rng, -8f, 8f), R(rng, -4f, 4f));
                if (i % 3 == 0)
                {
                    Proto.BeginVisualOnly();
                    GameObject r = Proto.Cube(s.transform, new Vector3(0f, 0.1f, 0.52f), new Vector3(0.28f, 0.3f, 0.04f), rune, "Rune");
                    r.GetComponent<Renderer>().sharedMaterial = runeGlow;
                    Proto.EndVisualOnly();
                }
            }

            // Un linteau pose sur deux pierres voisines : un trilithe.
            Proto.BeginVisualOnly();
            float a0 = 0f, a1 = 1f / count * Mathf.PI * 2f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
            Vector3 p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
            GameObject lintel = Proto.Cube(t, (p0 + p1) * 0.5f + new Vector3(0f, 3.1f, 0f),
                                           new Vector3(0.9f, 0.6f, (p1 - p0).magnitude + 1.2f), stoneDark, "Linteau");
            lintel.transform.localRotation = Quaternion.LookRotation(p1 - p0, Vector3.up);
            Proto.EndVisualOnly();

            // L'autel.
            Proto.Cube(t, new Vector3(-0.8f, 0.4f, 0f), new Vector3(0.5f, 0.9f, 1.2f), stoneDark, "Pied d'autel");
            Proto.Cube(t, new Vector3(0.8f, 0.4f, 0f), new Vector3(0.5f, 0.9f, 1.2f), stoneDark, "Pied d'autel");
            Proto.Cube(t, new Vector3(0f, 0.95f, 0f), new Vector3(2.4f, 0.25f, 1.5f), stone, "Autel");

            GameObject lightGo = new GameObject("Lueur du cercle");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = rune;
            light.intensity = 1.3f;
            light.range = 11f;
            light.shadows = LightShadows.None;

            TalismanPickup.Build(t, new Vector3(0f, 1.9f, 0f), Talisman.Coeur);
        }

        // ------------------------------------------------------------------ la cabane

        static void Cabane(Transform t, System.Random rng)
        {
            Color log = new Color(0.24f, 0.18f, 0.13f);
            Color logDark = new Color(0.18f, 0.14f, 0.10f);
            Color pelt = new Color(0.36f, 0.27f, 0.19f);
            Color straw = new Color(0.34f, 0.30f, 0.20f);

            const float hx = 2.2f, hz = 2.8f, h = 2.6f, thick = 0.3f;
            Vector3 p1 = new Vector3(-hx, 0f, hz), p2 = new Vector3(hx, 0f, hz);
            Vector3 p3 = new Vector3(hx, 0f, -hz), p4 = new Vector3(-hx, 0f, -hz);

            Wall(t, p1, p2, -0.3f, h, thick, log, "Mur");
            Wall(t, p2, p3, -0.3f, h, thick, logDark, "Mur");
            Wall(t, p4, p1, -0.3f, h, thick, logDark, "Mur");
            // La facade sud : une porte d'un metre vingt.
            Wall(t, p3, new Vector3(0.6f, 0f, -hz), -0.3f, h, thick, log, "Mur");
            Wall(t, new Vector3(-0.6f, 0f, -hz), p4, -0.3f, h, thick, log, "Mur");
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(0f, 2.25f, -hz), new Vector3(1.3f, 0.7f, thick), log, "Linteau");

            // Des rondins horizontaux sur les murs : on lit la construction.
            for (int k = 0; k < 5; k++)
            {
                float y = 0.3f + k * 0.5f;
                Proto.Cube(t, new Vector3(-hx - 0.12f, y, 0f), new Vector3(0.12f, 0.08f, hz * 2f + 0.3f), logDark, "Rondin");
                Proto.Cube(t, new Vector3(hx + 0.12f, y, 0f), new Vector3(0.12f, 0.08f, hz * 2f + 0.3f), logDark, "Rondin");
            }

            // Le toit : des planches en pente, dont un tiers manque et une est tombee.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int k = 0; k < 7; k++)
                {
                    if (rng.NextDouble() < 0.3) continue;
                    float z = -hz - 0.2f + k * (hz * 2f + 0.4f) / 6f;
                    GameObject plank = Proto.Cube(t, new Vector3(side * hx * 0.5f, h + 0.55f, z),
                                                  new Vector3(hx * 1.25f, 0.08f, 0.8f), straw, "Planche");
                    plank.transform.localRotation = Quaternion.Euler(0f, 0f, side * -26f);
                }
            }
            GameObject fallen = Proto.Cube(t, new Vector3(0.6f, 1.0f, 0.8f), new Vector3(2.6f, 0.08f, 0.7f), straw, "Planche tombée");
            fallen.transform.localRotation = Quaternion.Euler(8f, 20f, 38f);

            // La table, le tabouret, les peaux pendues au mur, des bois de cerf.
            Proto.EndVisualOnly();
            Proto.Cube(t, new Vector3(-0.9f, 0.45f, 1.2f), new Vector3(1.4f, 0.9f, 0.9f), logDark, "Table");
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(0.1f, 0.25f, 1.1f), new Vector3(0.4f, 0.5f, 0.4f), log, "Tabouret");
            for (int k = 0; k < 3; k++)
            {
                GameObject skin = Proto.Cube(t, new Vector3(hx - 0.2f, 1.6f, -1.4f + k * 1.2f), new Vector3(0.04f, 1.1f, 0.8f), pelt, "Peau");
                skin.transform.localRotation = Quaternion.Euler(R(rng, -6f, 6f), 0f, 0f);
            }
            Vector3 antlers = new Vector3(0f, 2.75f, -hz - 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject beam = Proto.Cube(t, antlers + new Vector3(side * 0.3f, 0.25f, 0f), new Vector3(0.07f, 0.6f, 0.07f),
                                             new Color(0.8f, 0.76f, 0.66f), "Bois de cerf");
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, side * -35f);
                GameObject tine = Proto.Cube(t, antlers + new Vector3(side * 0.45f, 0.45f, 0f), new Vector3(0.05f, 0.3f, 0.05f),
                                             new Color(0.8f, 0.76f, 0.66f), "Andouiller");
                tine.transform.localRotation = Quaternion.Euler(0f, 0f, side * 20f);
            }
            // Un feu mort devant la porte.
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2f;
                Proto.Cube(t, new Vector3(Mathf.Cos(a) * 0.5f + 0.4f, 0.08f, Mathf.Sin(a) * 0.5f - hz - 2.2f),
                           new Vector3(0.24f, 0.16f, 0.2f), new Color(0.24f, 0.24f, 0.25f), "Pierre");
            }
            Proto.Cube(t, new Vector3(0.4f, 0.03f, -hz - 2.2f), new Vector3(0.6f, 0.04f, 0.6f), new Color(0.08f, 0.08f, 0.08f), "Cendres");
            Proto.EndVisualOnly();

            TalismanPickup.Build(t, new Vector3(-0.9f, 1.5f, 1.2f), Talisman.Besace);
        }

        // ------------------------------------------------------------------ le Tertre

        static void Tertre(Transform t, System.Random rng)
        {
            Color earth = new Color(0.19f, 0.20f, 0.14f);
            Color stone = new Color(0.30f, 0.30f, 0.29f);

            // La butte : un cone aplati, enfonce de deux metres pour qu'aucun bord ne
            // flotte si le sol penche. On peut gravir ses flancs (27 degres). Son
            // collider suit sa forme exacte (MeshCollider "convexe").
            GameObject mound = Proto.Cone(t, new Vector3(0f, -2.2f, 0f), 11f, 5.6f, earth, "Butte", 9);
            MeshCollider shape = mound.AddComponent<MeshCollider>();
            shape.sharedMesh = mound.GetComponent<MeshFilter>().sharedMesh;
            shape.convex = true;

            // La porte : deux pierres debout et une table, au pied de la butte.
            Vector3 door = new Vector3(0f, 0f, -8.3f);
            Proto.Cube(t, door + new Vector3(-1.1f, 1.0f, 0f), new Vector3(0.6f, 2.4f, 0.9f), stone, "Montant");
            Proto.Cube(t, door + new Vector3(1.1f, 1.0f, 0f), new Vector3(0.6f, 2.4f, 0.9f), stone, "Montant");
            Proto.Cube(t, door + new Vector3(0f, 2.35f, 0.1f), new Vector3(3.2f, 0.5f, 1.4f), stone, "Table");
            Proto.BeginVisualOnly();
            Proto.Cube(t, door + new Vector3(0f, 0.9f, 0.4f), new Vector3(1.6f, 2.2f, 0.2f), new Color(0.02f, 0.02f, 0.02f), "Nuit");
            Proto.EndVisualOnly();

            // Des pierres couchees en couronne autour de la butte.
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + 0.13f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 270f)) < 22f) continue;   // pas devant la porte
                float hgt = R(rng, 0.5f, 1.3f);
                GameObject s = Proto.Cube(t, new Vector3(Mathf.Cos(a) * 11.2f, hgt * 0.5f - 0.1f, Mathf.Sin(a) * 11.2f),
                                          new Vector3(0.8f, hgt, 0.6f), stone, "Borne");
                s.transform.localRotation = Quaternion.Euler(R(rng, -6f, 6f), -a * Mathf.Rad2Deg, R(rng, -6f, 6f));
            }

            TalismanPickup.Build(t, door + new Vector3(0f, 1.2f, -1.2f), Talisman.Pelle);
        }

        // ------------------------------------------------------------------ la Tour

        static void BuildTour(Transform t, System.Random rng)
        {
            Color stone = new Color(0.30f, 0.30f, 0.29f);
            Color moss = new Color(0.21f, 0.26f, 0.17f);
            const int sides = 12;
            const float radius = 3.8f;
            for (int i = 0; i < sides; i++)
            {
                if (i == 9) continue;                         // la porte, au sud
                float a0 = i / (float)sides * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
                Vector3 p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                // Les pans sont d'autant plus hauts qu'ils sont loin de la porte : la
                // tour s'est effondree de ce cote-la.
                float height = 2f + 7f * Mathf.Abs(Mathf.Sin((i - 9) / (float)sides * Mathf.PI)) + R(rng, -0.8f, 0.8f);
                Wall(t, p0, p1, -0.5f, height, 0.9f, i % 4 == 0 ? moss : stone, "Pan");
            }
            // Un reste de plancher, tout la-haut, qui pend.
            Proto.BeginVisualOnly();
            GameObject floor = Proto.Cube(t, new Vector3(0.8f, 6.2f, 1.6f), new Vector3(3.2f, 0.12f, 1.6f), new Color(0.2f, 0.15f, 0.1f), "Plancher");
            floor.transform.localRotation = Quaternion.Euler(12f, 0f, -18f);
            for (int i = 0; i < 16; i++)
            {
                Vector3 p = new Vector3(R(rng, -7f, 7f), 0f, R(rng, -7f, 2f));
                if (p.magnitude < radius + 0.8f) continue;
                float s = R(rng, 0.3f, 0.8f);
                GameObject block = Proto.Cube(t, p + new Vector3(0f, s * 0.35f, 0f), new Vector3(s * 1.3f, s * 0.7f, s), stone, "Éboulis");
                block.transform.localRotation = Quaternion.Euler(R(rng, -20f, 20f), R(rng, 0f, 180f), R(rng, -20f, 20f));
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ outils

        static void Wall(Transform t, Vector3 a, Vector3 b, float y0, float y1, float thick, Color color, string name)
        {
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = flat.magnitude;
            if (length < 0.01f) return;
            GameObject go = Proto.Cube(t, new Vector3((a.x + b.x) * 0.5f, (y0 + y1) * 0.5f, (a.z + b.z) * 0.5f),
                                       new Vector3(thick, y1 - y0, length + thick * 0.5f), color, name);
            go.transform.localRotation = Quaternion.LookRotation(flat / length, Vector3.up);
        }

        static float R(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }

    /// <summary>
    /// Un lieu-dit dans le monde. Il sait s'il a ete DECOUVERT : la premiere fois
    /// qu'on s'en approche a moins de 16 m, un titre s'affiche, et il rejoint les
    /// reperes du HUD pour le reste de la Saison.
    /// </summary>
    public class Landmark : MonoBehaviour
    {
        public Landmarks.Kind kind;
        public bool Discovered { get; private set; }
        float checkTimer;

        void Update()
        {
            if (Discovered || Game.PlayerTransform == null) return;
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 0.4f;

            Vector3 d = Game.PlayerTransform.position - transform.position;
            d.y = 0f;
            if (d.magnitude > 16f) return;
            if (Game.Menus != null && Game.Menus.Blocking) return;

            Discovered = true;
            Sfx.Discovery();
            if (Game.Hud != null)
                Game.Hud.ShowDiscovery("LIEU-DIT", Landmarks.Name(kind), "", "", new Color(0.86f, 0.80f, 0.64f));
        }
    }
}
