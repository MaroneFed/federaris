using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES MILLE DETAILS DU CHATEAU. Castle.cs pose la masse, CastleDecor les
    /// grands moments ; ici, ce qui fait qu'on croit qu'on y a VECU :
    ///
    ///   - le lierre qui mange les murs, dedans et dehors ;
    ///   - des feux sur le haut des remparts (sans lumiere : de simples braises
    ///     qu'on devine en levant la tete) ;
    ///   - la FORGE du chateau : un foyer et sa cheminee, une enclume, un soufflet,
    ///     un ratelier d'outils, un baquet ;
    ///   - les ECURIES sous un appentis, contre le mur sud : stalles, foin, abreuvoir ;
    ///   - une CHAPELLE a ciel ouvert : une sainte de pierre, des cierges ;
    ///   - deux MANNEQUINS d'entrainement pour la garde ;
    ///   - dans la salle du trone, deux longues TABLES et leurs bancs, des cierges,
    ///     des ECUS aux murs ;
    ///   - des FLAMMES sur les fleches des tours, des HOURDS de bois sur les tours
    ///     de la porte ;
    ///   - devant la porte, une CAGE vide qui pend a une potence.
    ///
    /// Tout ce qui se touche a un collider simple ; le reste est visuel.
    /// </summary>
    public static class CastleDetails
    {
        static readonly Color Stone = new Color(0.31f, 0.31f, 0.30f);
        static readonly Color StoneDark = new Color(0.22f, 0.22f, 0.22f);
        static readonly Color Ivy = new Color(0.14f, 0.21f, 0.12f);
        static readonly Color IvyLight = new Color(0.19f, 0.27f, 0.15f);
        static readonly Color Wood = new Color(0.24f, 0.18f, 0.12f);
        static readonly Color WoodDark = new Color(0.16f, 0.12f, 0.09f);
        static readonly Color Iron = new Color(0.13f, 0.13f, 0.14f);
        static readonly Color Hay = new Color(0.46f, 0.39f, 0.2f);
        static readonly Color Crimson = new Color(0.40f, 0.08f, 0.07f);
        static readonly Color Gold = new Color(0.62f, 0.5f, 0.24f);
        static readonly Color Sack = new Color(0.42f, 0.36f, 0.26f);

        public static void Build(Transform t, System.Random rng)
        {
            IvyOnWalls(t, rng);
            RampartFires(t);
            Smithy(t, new Vector3(-17f, 0f, -31f));
            Stables(t);
            Chapel(t, new Vector3(26f, 0f, 31f));
            Dummy(t, new Vector3(-12f, 0f, -18f), 20f);
            Dummy(t, new Vector3(-14.5f, 0f, -21f), -15f);
            Banquet(t);
            TowerPennants(t);
            Hoardings(t);
            HangingCage(t, new Vector3(-9f, 0f, -48f));
        }

        // ------------------------------------------------------------------ le lierre

        static void IvyOnWalls(Transform t, System.Random rng)
        {
            float h = Castle.HalfSize;
            float inner = h - Castle.WallThickness * 0.5f - 0.03f;
            float outer = h + Castle.WallThickness * 0.5f + 0.03f;
            Proto.BeginVisualOnly();
            for (int i = 0; i < 46; i++)
            {
                int side = rng.Next(4);
                bool inside = rng.NextDouble() < 0.5;
                float along = R(rng, -h + 8f, h - 8f);
                // Pas sur la grande porte (sud), ni sur la poterne (nord).
                if ((side == 1 || side == 0) && Mathf.Abs(along) < 6f) continue;
                float face = inside ? inner : outer;
                float height = R(rng, 2.5f, 9f);
                float width = R(rng, 0.6f, 1.6f);
                Vector3 p, size;
                switch (side)
                {
                    case 0: p = new Vector3(along, height * 0.5f, face); size = new Vector3(width, height, 0.06f); break;
                    case 1: p = new Vector3(along, height * 0.5f, -face); size = new Vector3(width, height, 0.06f); break;
                    case 2: p = new Vector3(face, height * 0.5f, along); size = new Vector3(0.06f, height, width); break;
                    default: p = new Vector3(-face, height * 0.5f, along); size = new Vector3(0.06f, height, width); break;
                }
                Proto.Cube(t, p, size, rng.NextDouble() < 0.5 ? Ivy : IvyLight, "Lierre");
                // Quelques touffes qui depassent, pour casser le rectangle.
                for (int k = 0; k < 3; k++)
                {
                    Vector3 tuft = p + new Vector3(0f, R(rng, -height * 0.4f, height * 0.5f), 0f);
                    Vector3 tsize = size * 0.5f + new Vector3(0.12f, 0.12f, 0.12f);
                    Proto.Cube(t, tuft, tsize, IvyLight, "Feuilles");
                }
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ feux des remparts

        static void RampartFires(Transform t)
        {
            // Sur la face exterieure, juste sous les creneaux : au-dessus, les merlons
            // les cacheraient.
            float edge = Castle.HalfSize + Castle.WallThickness * 0.5f + 0.3f;
            Material fire = MaterialFactory.GetGlow(new Color(1f, 0.55f, 0.2f), 2.6f);
            Proto.BeginVisualOnly();
            for (int side = 0; side < 4; side++)
            {
                for (float a = -24f; a <= 24f; a += 16f)
                {
                    if (side == 1 && Mathf.Abs(a) < 8f) continue;      // les tours de la porte
                    Vector3 p = side == 0 ? new Vector3(a, 0f, edge) : side == 1 ? new Vector3(a, 0f, -edge)
                              : side == 2 ? new Vector3(edge, 0f, a) : new Vector3(-edge, 0f, a);
                    if (side == 2 && a > Castle.BreachFrom - 2f && a < Castle.BreachTo + 2f) continue;   // la breche
                    Proto.Cylinder(t, p + new Vector3(0f, Castle.WallHeight - 0.9f, 0f), new Vector3(0.5f, 0.1f, 0.5f), Iron, "Réchaud");
                    GameObject flame = Proto.Cube(t, p + new Vector3(0f, Castle.WallHeight - 0.6f, 0f), new Vector3(0.25f, 0.45f, 0.25f), Color.white, "Feu");
                    flame.GetComponent<Renderer>().sharedMaterial = fire;
                    flame.AddComponent<Flame>();
                }
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ la forge

        static void Smithy(Transform t, Vector3 at)
        {
            // Le foyer et sa hotte, contre rien : une forge a ciel ouvert.
            Proto.Cube(t, at + new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.1f, 1.5f), StoneDark, "Foyer");
            Proto.Cube(t, at + new Vector3(0f, 2.8f, 0.35f), new Vector3(1.2f, 3.4f, 0.8f), Stone, "Cheminée");
            Proto.BeginVisualOnly();
            GameObject coals = Proto.Cube(t, at + new Vector3(0f, 1.12f, -0.2f), new Vector3(1.4f, 0.06f, 0.8f), Color.white, "Braises");
            coals.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.8f, 0.22f, 0.08f), 1.2f);
            Proto.Cube(t, at + new Vector3(0f, 1.9f, -0.2f), new Vector3(2.0f, 0.12f, 1.1f), StoneDark, "Hotte");
            // Le soufflet.
            GameObject bellows = Proto.Cube(t, at + new Vector3(1.5f, 0.9f, 0f), new Vector3(0.5f, 0.25f, 1.1f), new Color(0.3f, 0.2f, 0.13f), "Soufflet");
            bellows.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            Proto.EndVisualOnly();

            // L'enclume sur sa souche.
            Vector3 anvil = at + new Vector3(0.4f, 0f, -2.4f);
            Proto.BeginVisualOnly();
            Proto.Cylinder(t, anvil + new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.3f, 0.7f), WoodDark, "Souche");
            Proto.Cube(t, anvil + new Vector3(0f, 0.72f, 0f), new Vector3(0.3f, 0.25f, 0.5f), Iron, "Enclume");
            Proto.Cube(t, anvil + new Vector3(0f, 0.9f, 0f), new Vector3(0.36f, 0.12f, 0.8f), Iron, "Table");
            Proto.Cone(t, anvil + new Vector3(0f, 0.9f, 0.4f), 0.12f, 0.35f, Iron, "Bigorne", 4)
                 .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject hammer = Proto.Cube(t, anvil + new Vector3(0.08f, 1.0f, -0.1f), new Vector3(0.05f, 0.05f, 0.45f), Wood, "Marteau");
            hammer.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            Proto.EndVisualOnly();
            Proto.Blocker(t, anvil + new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1f, 1f), "Enclume");

            // Le ratelier : deux montants, une barre, des outils qui pendent.
            Vector3 rack = at + new Vector3(-2.2f, 0f, -1.2f);
            Proto.Cube(t, rack + new Vector3(0f, 0.9f, -0.7f), new Vector3(0.12f, 1.8f, 0.12f), Wood, "Montant");
            Proto.Cube(t, rack + new Vector3(0f, 0.9f, 0.7f), new Vector3(0.12f, 1.8f, 0.12f), Wood, "Montant");
            Proto.BeginVisualOnly();
            Proto.Cube(t, rack + new Vector3(0f, 1.75f, 0f), new Vector3(0.1f, 0.1f, 1.6f), Wood, "Barre");
            for (int i = 0; i < 4; i++)
            {
                float z = -0.5f + i * 0.33f;
                Proto.Cube(t, rack + new Vector3(0.08f, 1.3f, z), new Vector3(0.03f, 0.8f, 0.03f), Wood, "Manche");
                Proto.Cube(t, rack + new Vector3(0.08f, 0.9f, z), new Vector3(0.06f, 0.14f, i % 2 == 0 ? 0.2f : 0.08f), Iron, "Outil");
            }
            // Le baquet d'eau pour tremper le fer.
            Proto.Cylinder(t, at + new Vector3(-1.4f, 0.35f, -2.6f), new Vector3(0.9f, 0.35f, 0.9f), WoodDark, "Baquet");
            Proto.Cylinder(t, at + new Vector3(-1.4f, 0.66f, -2.6f), new Vector3(0.78f, 0.01f, 0.78f), new Color(0.05f, 0.06f, 0.07f), "Eau");
            Proto.EndVisualOnly();
            Proto.Blocker(t, at + new Vector3(-1.4f, 0.35f, -2.6f), new Vector3(0.9f, 0.7f, 0.9f), "Baquet");
        }

        // ------------------------------------------------------------------ les ecuries

        static void Stables(Transform t)
        {
            float wallIn = -Castle.HalfSize + Castle.WallThickness * 0.5f;
            float x0 = 12f, x1 = 24f, depth = 3.6f;
            float zBack = wallIn + 0.1f, zFront = wallIn + depth;

            // Les poteaux de facade et l'appentis.
            for (float x = x0; x <= x1 + 0.01f; x += 4f)
                Proto.Cube(t, new Vector3(x, 1.4f, zFront), new Vector3(0.22f, 2.8f, 0.22f), Wood, "Poteau");
            Proto.BeginVisualOnly();
            GameObject roof = Proto.Cube(t, new Vector3((x0 + x1) * 0.5f, 3.2f, (zBack + zFront) * 0.5f),
                                         new Vector3(x1 - x0 + 1f, 0.12f, depth + 0.8f), WoodDark, "Appentis");
            roof.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            // Les cloisons des stalles.
            for (float x = x0 + 4f; x < x1; x += 4f)
                Proto.Cube(t, new Vector3(x, 0.7f, (zBack + zFront) * 0.5f), new Vector3(0.1f, 1.4f, depth - 0.4f), Wood, "Cloison");
            // Le foin, et l'abreuvoir.
            for (int i = 0; i < 3; i++)
            {
                float x = x0 + 2f + i * 4f;
                GameObject hay = Proto.Cube(t, new Vector3(x, 0.3f, zBack + 1.1f), new Vector3(1.8f, 0.6f, 1.4f), Hay, "Foin");
                hay.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
            }
            Proto.Cube(t, new Vector3(x0 + 6f, 0.35f, zFront + 0.9f), new Vector3(2.4f, 0.5f, 0.6f), WoodDark, "Abreuvoir");
            Proto.EndVisualOnly();
            Proto.Blocker(t, new Vector3(x0 + 6f, 0.35f, zFront + 0.9f), new Vector3(2.4f, 0.7f, 0.6f), "Abreuvoir");
        }

        // ------------------------------------------------------------------ la chapelle

        static void Chapel(Transform t, Vector3 at)
        {
            // Une sainte de pierre sur un socle, face a la cour ; des cierges a ses pieds.
            Proto.Cube(t, at + new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 1.1f), StoneDark, "Socle");
            GameObject statue = new GameObject("Sainte");
            statue.transform.SetParent(t, false);
            statue.transform.localPosition = at + new Vector3(0f, 1f, 0f);
            statue.transform.localRotation = Quaternion.Euler(0f, 200f, 0f);
            Proto.BeginVisualOnly();
            Figures.Robed(statue.transform, 1.9f, 0.85f, new Color(0.4f, 0.4f, 0.38f), new Color(0.33f, 0.33f, 0.31f),
                          new Color(0.42f, 0.42f, 0.4f), true);
            Material candle = MaterialFactory.GetGlow(new Color(1f, 0.8f, 0.5f), 2.2f);
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f - 0.5f) * 2.4f;
                Vector3 p = at + new Vector3(Mathf.Sin(a) * 1.1f, 0f, -Mathf.Cos(a) * 1.1f);
                float hgt = 0.18f + (i % 3) * 0.08f;
                Proto.Cube(t, p + new Vector3(0f, hgt * 0.5f, 0f), new Vector3(0.06f, hgt, 0.06f), new Color(0.86f, 0.82f, 0.7f), "Cierge");
                GameObject flame = Proto.Cube(t, p + new Vector3(0f, hgt + 0.04f, 0f), new Vector3(0.04f, 0.07f, 0.04f), Color.white, "Flamme");
                flame.GetComponent<Renderer>().sharedMaterial = candle;
                flame.AddComponent<Flame>();
            }
            // Le prie-Dieu.
            Proto.Cube(t, at + new Vector3(0f, 0.2f, -2.2f), new Vector3(1.1f, 0.15f, 0.35f), Wood, "Prie-Dieu");
            Proto.Cube(t, at + new Vector3(0f, 0.55f, -2.0f), new Vector3(1.1f, 0.5f, 0.08f), Wood, "Accoudoir");
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Cierges");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = at + new Vector3(0f, 0.6f, -1.2f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.intensity = 0.9f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
        }

        // ------------------------------------------------------------------ les mannequins

        static void Dummy(Transform t, Vector3 at, float yaw)
        {
            GameObject go = new GameObject("Mannequin");
            go.transform.SetParent(t, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform d = go.transform;
            Proto.BeginVisualOnly();
            Proto.Cube(d, new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f), Wood, "Poteau");
            Proto.Cube(d, new Vector3(0f, 1.45f, 0f), new Vector3(1.1f, 0.1f, 0.1f), Wood, "Bras");
            Proto.Cube(d, new Vector3(0f, 1.25f, 0f), new Vector3(0.5f, 0.6f, 0.34f), Sack, "Sac");
            Proto.Cube(d, new Vector3(0f, 1.78f, 0f), new Vector3(0.3f, 0.3f, 0.3f), Sack, "Tête");
            Proto.Cube(d, new Vector3(0f, 1.25f, 0.18f), new Vector3(0.3f, 0.3f, 0.02f), Crimson, "Cible");
            Proto.EndVisualOnly();
            Proto.Blocker(d, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 0.5f), "Mannequin");
        }

        // ------------------------------------------------------------------ le banquet

        static void Banquet(Transform t)
        {
            Vector3 c = Castle.KeepCentre;
            float floor = Castle.HallFloor;
            Material candle = MaterialFactory.GetGlow(new Color(1f, 0.8f, 0.5f), 2f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 2.55f;
                float z0 = c.z - 5.5f, z1 = c.z + 0.5f;
                Proto.Cube(t, new Vector3(x, floor + 0.75f, (z0 + z1) * 0.5f), new Vector3(1.0f, 0.1f, z1 - z0), Wood, "Table");
                Proto.BeginVisualOnly();
                Proto.Cube(t, new Vector3(x, floor + 0.35f, z0 + 0.4f), new Vector3(0.8f, 0.7f, 0.1f), WoodDark, "Pied");
                Proto.Cube(t, new Vector3(x, floor + 0.35f, z1 - 0.4f), new Vector3(0.8f, 0.7f, 0.1f), WoodDark, "Pied");
                Proto.Cube(t, new Vector3(x + side * 0.85f, floor + 0.42f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.08f, z1 - z0 - 0.4f), WoodDark, "Banc");
                for (int k = 0; k < 4; k++)
                {
                    float z = z0 + 0.8f + k * 1.45f;
                    Proto.Cube(t, new Vector3(x, floor + 0.9f, z), new Vector3(0.06f, 0.2f, 0.06f), new Color(0.86f, 0.82f, 0.7f), "Cierge");
                    GameObject flame = Proto.Cube(t, new Vector3(x, floor + 1.04f, z), new Vector3(0.04f, 0.07f, 0.04f), Color.white, "Flamme");
                    flame.GetComponent<Renderer>().sharedMaterial = candle;
                    flame.AddComponent<Flame>();
                    // Une coupe, une assiette d'etain.
                    Proto.Cylinder(t, new Vector3(x + 0.25f, floor + 0.82f, z + 0.5f), new Vector3(0.22f, 0.01f, 0.22f), new Color(0.4f, 0.4f, 0.42f), "Assiette");
                }
                Proto.EndVisualOnly();
                Proto.Blocker(t, new Vector3(x + side * 0.85f, floor + 0.2f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.4f, z1 - z0 - 0.4f), "Banc");

                // Des ecus aux murs lateraux de la salle, en cramoisi et or.
                float wall = side * (Castle.KeepHalfWidth - 1.6f - 0.05f);
                Proto.BeginVisualOnly();
                for (int k = 0; k < 3; k++)
                {
                    float z = c.z - 4f + k * 4f;
                    Proto.Cube(t, new Vector3(wall, 4.2f, z), new Vector3(0.06f, 1.0f, 0.8f), k == 1 ? Gold : Crimson, "Écu");
                    Proto.Cube(t, new Vector3(wall - side * 0.02f, 4.25f, z), new Vector3(0.04f, 0.6f, 0.12f), k == 1 ? Crimson : Gold, "Pal");
                    GameObject s1 = Proto.Cube(t, new Vector3(wall - side * 0.05f, 4.2f, z), new Vector3(0.03f, 1.6f, 0.06f), new Color(0.55f, 0.56f, 0.58f), "Épée");
                    s1.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
                    GameObject s2 = Proto.Cube(t, new Vector3(wall - side * 0.05f, 4.2f, z), new Vector3(0.03f, 1.6f, 0.06f), new Color(0.55f, 0.56f, 0.58f), "Épée");
                    s2.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
                }
                Proto.EndVisualOnly();
            }
        }

        // ------------------------------------------------------------------ fanions et hourds

        static void TowerPennants(Transform t)
        {
            float h = Castle.HalfSize;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            float top = Castle.TowerHeight + 0.55f + Castle.TowerSize * 1.0f + 2.6f;
            Proto.BeginVisualOnly();
            for (int i = 0; i < corners.Length; i++)
            {
                GameObject pennant = Proto.Cube(t, corners[i] + new Vector3(0.55f, top, 0f), new Vector3(1.0f, 0.4f, 0.03f), Crimson, "Fanion");
                pennant.transform.localRotation = Quaternion.Euler(0f, i * 35f, -8f);
                pennant.AddComponent<Flutter>();
            }
            Proto.EndVisualOnly();
        }

        static void Hoardings(Transform t)
        {
            float off = Castle.GateWidth * 0.5f + 4.2f;
            float face = -Castle.HalfSize - 0.5f - 4f;
            Proto.BeginVisualOnly();
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = new Vector3(side * off, Castle.GatehouseHeight - 0.8f, face - 0.6f);
                Proto.Cube(t, p, new Vector3(6.4f, 1.6f, 1.2f), WoodDark, "Hourd");
                GameObject roof = Proto.Cube(t, p + new Vector3(0f, 1.05f, -0.1f), new Vector3(6.8f, 0.1f, 1.7f), Wood, "Toit");
                roof.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
                for (int k = -2; k <= 2; k++)
                    Proto.Cube(t, p + new Vector3(k * 1.3f, -1.1f, 0.3f), new Vector3(0.14f, 0.7f, 0.14f), Wood, "Corbeau");
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ la cage

        static void HangingCage(Transform t, Vector3 at)
        {
            float ground = Ground.Sample(at.x, at.z);
            Vector3 b = new Vector3(at.x, ground, at.z);
            Proto.Cube(t, b + new Vector3(0f, 2.4f, 0f), new Vector3(0.25f, 4.8f, 0.25f), WoodDark, "Potence");
            Proto.BeginVisualOnly();
            Proto.Cube(t, b + new Vector3(0.9f, 4.7f, 0f), new Vector3(2.0f, 0.2f, 0.2f), WoodDark, "Bras");
            GameObject brace = Proto.Cube(t, b + new Vector3(0.45f, 4.2f, 0f), new Vector3(0.12f, 1.2f, 0.12f), WoodDark, "Jambe");
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            Proto.Cube(t, b + new Vector3(1.7f, 4.3f, 0f), new Vector3(0.03f, 0.8f, 0.03f), Iron, "Chaîne");
            GameObject cage = new GameObject("Cage");
            cage.transform.SetParent(t, false);
            cage.transform.localPosition = b + new Vector3(1.7f, 2.9f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Proto.Cube(cage.transform, new Vector3(Mathf.Cos(a) * 0.45f, 0f, Mathf.Sin(a) * 0.45f), new Vector3(0.04f, 1.6f, 0.04f), Iron, "Barreau");
            }
            Proto.Cylinder(cage.transform, new Vector3(0f, 0.8f, 0f), new Vector3(1f, 0.03f, 1f), Iron, "Dessus");
            Proto.Cylinder(cage.transform, new Vector3(0f, -0.8f, 0f), new Vector3(1f, 0.03f, 1f), Iron, "Dessous");
            cage.AddComponent<Flutter>();
            Proto.EndVisualOnly();
        }

        static float R(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }

    /// <summary>Un tissu (ou une cage) qui bouge un peu au vent. Trois ondes, jamais la meme boucle.</summary>
    public class Flutter : MonoBehaviour
    {
        Quaternion baseRotation;
        float seed;

        void Start()
        {
            baseRotation = transform.localRotation;
            seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            float t = Time.time + seed;
            float a = Mathf.Sin(t * 1.7f) * 6f + Mathf.Sin(t * 3.9f) * 3f;
            float b = Mathf.Sin(t * 1.1f) * 4f;
            transform.localRotation = baseRotation * Quaternion.Euler(b, a, 0f);
        }
    }
}
