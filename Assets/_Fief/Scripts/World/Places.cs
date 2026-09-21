using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les lieux a decouvrir : ce qui donne une raison de s'ecarter du chemin.
    ///
    /// Moulins, chapelles, camps abandonnes, mines effondrees, fermes en ruine,
    /// postes de guet. Chacun cache une ou deux caisses a fouiller, pour que la
    /// curiosite soit recompensee.
    /// </summary>
    public static class Places
    {
        public static void Build(Transform parent, GameConfig cfg, System.Random rng,
                                 List<Vector3> occupied, int count)
        {
            GameObject root = new GameObject("Lieux");
            root.transform.SetParent(parent, false);

            float half = cfg.mapSize * 0.5f - 90f;
            int built = 0;
            int guard = 0;

            while (built < count && guard < count * 60)
            {
                guard++;

                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * half;

                if (Mathf.Sqrt(x * x + z * z) < cfg.marketRadius + 90f) continue;
                if (!Scenery.IsFree(x, z, occupied, 90f, 28f)) continue;
                if (Ground.Slope(x, z) > 0.3f) continue;
                if (Ground.Height(x, z) < 0f) continue;

                Vector3 at = Ground.Place(x, z, 0f);
                GameObject place = new GameObject("Lieu");
                place.transform.SetParent(root.transform, false);
                place.transform.position = at;
                place.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                switch (built % 6)
                {
                    case 0: Windmill(place.transform); break;
                    case 1: Chapel(place.transform); break;
                    case 2: Camp(place.transform, rng); break;
                    case 3: CollapsedMine(place.transform); break;
                    case 4: FarmRuin(place.transform, rng); break;
                    default: WatchPost(place.transform); break;
                }

                int crates = 1 + rng.Next(3);
                for (int i = 0; i < crates; i++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float d = 4f + (float)rng.NextDouble() * 9f;
                    Vector3 spot = Ground.Place(x + Mathf.Cos(a) * d, z + Mathf.Sin(a) * d, 0f);
                    Crate(root.transform, spot, rng);
                }

                occupied.Add(at);
                built++;
            }
        }

        /// <summary>Caisses isolees, semees loin de tout : la vraie recompense de l'explorateur.</summary>
        public static void ScatterLoot(Transform parent, GameConfig cfg, System.Random rng,
                                       List<Vector3> occupied, int count)
        {
            GameObject root = new GameObject("Butin");
            root.transform.SetParent(parent, false);

            float half = cfg.mapSize * 0.5f - 60f;
            int placed = 0;
            int guard = 0;

            while (placed < count && guard < count * 40)
            {
                guard++;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                float z = (float)(rng.NextDouble() * 2.0 - 1.0) * half;

                if (!Scenery.IsFree(x, z, occupied, 30f, 14f)) continue;
                if (Ground.Slope(x, z) > 0.34f) continue;
                if (Ground.Height(x, z) < 0f) continue;

                Crate(root.transform, Ground.Place(x, z, 0f), rng);
                placed++;
            }
        }

        // ------------------------------------------------------------------ caisse

        static void Crate(Transform parent, Vector3 at, System.Random rng)
        {
            GameObject go = new GameObject("Caisse");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            Color wood = Palette.Shade(Palette.Trunk, 1.05f + (float)rng.NextDouble() * 0.3f);
            Proto.Cube(go.transform, new Vector3(0f, 0.42f, 0f), new Vector3(1.1f, 0.84f, 1.1f), wood, "Corps");

            GameObject lidPivot = new GameObject("Charniere");
            lidPivot.transform.SetParent(go.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.84f, -0.55f);
            Proto.BeginVisualOnly();
            Proto.Cube(lidPivot.transform, new Vector3(0f, 0.06f, 0.55f), new Vector3(1.18f, 0.14f, 1.18f),
                       Palette.Shade(wood, 0.8f), "Couvercle");
            Proto.Cube(go.transform, new Vector3(0f, 0.42f, 0f), new Vector3(1.16f, 0.14f, 1.16f),
                       Palette.Shade(Palette.Gold, 0.75f), "Cerclage");
            Proto.EndVisualOnly();

            LootCrate crate = go.AddComponent<LootCrate>();
            crate.Initialise(lidPivot.transform, rng);

            GameObject point = new GameObject("Fouille");
            point.transform.SetParent(go.transform, false);
            BoxCollider trigger = point.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 2.4f, 2.4f);
            trigger.center = new Vector3(0f, 1f, 0f);
            point.AddComponent<LootPoint>().crate = crate;
        }

        // ------------------------------------------------------------------ batiments

        static void Windmill(Transform p)
        {
            Color stone = Palette.Shade(Palette.Structure, 0.92f);
            Proto.Cylinder(p, new Vector3(0f, 5f, 0f), new Vector3(7f, 5f, 7f), stone, "Tour");
            GameObject cap = Proto.Cube(p, new Vector3(0f, 11f, 0f), new Vector3(6.6f, 3f, 6.6f),
                                        Palette.Roof, "Coiffe");
            cap.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            Proto.BeginVisualOnly();
            GameObject hub = new GameObject("Ailes");
            hub.transform.SetParent(p, false);
            hub.transform.localPosition = new Vector3(0f, 10f, 4.2f);
            for (int i = 0; i < 4; i++)
            {
                GameObject sail = Proto.Cube(hub.transform, Vector3.zero, new Vector3(1.4f, 11f, 0.3f),
                                             Palette.Shade(Palette.Trunk, 1.2f), "Aile");
                sail.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                sail.transform.localPosition = sail.transform.localRotation * new Vector3(0f, 5.5f, 0f);
            }
            Spinner spin = hub.AddComponent<Spinner>();
            spin.axis = Vector3.forward;
            spin.degreesPerSecond = 22f;
            Proto.EndVisualOnly();
        }

        static void Chapel(Transform p)
        {
            Color stone = Palette.Shade(Palette.Structure, 0.86f);
            Proto.Cube(p, new Vector3(0f, 2.6f, 0f), new Vector3(7f, 5.2f, 11f), stone, "Nef");
            Proto.BeginVisualOnly();
            GameObject roof = Proto.Cube(p, new Vector3(0f, 6.4f, 0f), new Vector3(5.6f, 5.6f, 11.4f),
                                         Palette.Shade(Palette.Roof, 0.85f), "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Proto.EndVisualOnly();

            Proto.Cube(p, new Vector3(0f, 7f, -6.4f), new Vector3(3.4f, 14f, 3.4f), stone, "Clocher");
            Proto.BeginVisualOnly();
            GameObject spire = Proto.Cube(p, new Vector3(0f, 15.6f, -6.4f), new Vector3(2.6f, 4.4f, 2.6f),
                                          Palette.Roof, "Fleche");
            spire.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Proto.Cube(p, new Vector3(0f, 18.4f, -6.4f), new Vector3(0.3f, 1.8f, 0.3f), Palette.Gold, "Croix");
            Proto.Cube(p, new Vector3(0f, 18.1f, -6.4f), new Vector3(1.1f, 0.3f, 0.3f), Palette.Gold, "Traverse");
            Proto.EndVisualOnly();
        }

        static void Camp(Transform p, System.Random rng)
        {
            Proto.BeginVisualOnly();
            for (int i = 0; i < 3; i++)
            {
                float a = (360f / 3f) * i * Mathf.Deg2Rad;
                Vector3 at = new Vector3(Mathf.Sin(a) * 5.5f, 0f, Mathf.Cos(a) * 5.5f);
                GameObject tent = Proto.Cube(p, at + new Vector3(0f, 1.5f, 0f),
                                             new Vector3(3.2f, 3.2f, 4.6f),
                                             Palette.Shade(Palette.Canvas, 0.8f), "Tente");
                tent.transform.localRotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * a, 45f);
            }
            for (int i = 0; i < 7; i++)
            {
                float a = (360f / 7f) * i * Mathf.Deg2Rad;
                Proto.Cube(p, new Vector3(Mathf.Sin(a) * 1.7f, 0.2f, Mathf.Cos(a) * 1.7f),
                           new Vector3(0.6f, 0.4f, 0.6f), Palette.Shade(Palette.Rock1, 0.8f), "Pierre");
            }
            for (int i = 0; i < 4; i++)
            {
                GameObject log = Proto.Cylinder(p, new Vector3(0f, 0.5f + i * 0.12f, 0f),
                                                new Vector3(0.28f, 1.1f, 0.28f),
                                                Palette.Shade(Palette.Trunk, 0.7f), "Buche");
                log.transform.localRotation = Quaternion.Euler(72f, i * 45f, 0f);
            }
            Proto.Cube(p, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 1.1f, 1.1f),
                       new Color(1f, 0.62f, 0.26f), "Braises");
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Feu");
            lightGo.transform.SetParent(p, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.66f, 0.34f);
            light.range = 22f;
            light.intensity = 2.4f;
            light.shadows = LightShadows.None;
        }

        static void CollapsedMine(Transform p)
        {
            Color rock = Palette.Shade(Palette.Rock2, 0.95f);
            Proto.Cube(p, new Vector3(0f, 3.4f, 0f), new Vector3(12f, 6.8f, 9f), rock, "Eperon");

            Proto.BeginVisualOnly();
            Proto.Cube(p, new Vector3(0f, 1.6f, 4.6f), new Vector3(3.6f, 3.2f, 0.6f),
                       new Color(0.06f, 0.05f, 0.05f), "Entree");
            for (int i = -1; i <= 1; i += 2)
            {
                Proto.Cube(p, new Vector3(i * 2.1f, 1.7f, 4.7f), new Vector3(0.5f, 3.4f, 0.5f),
                           Palette.Trunk, "Etai");
            }
            Proto.Cube(p, new Vector3(0f, 3.5f, 4.7f), new Vector3(4.8f, 0.5f, 0.5f), Palette.Trunk, "Linteau");
            for (int i = 0; i < 6; i++)
            {
                Proto.Cube(p, new Vector3(-5f + i * 2.1f, 0.4f, 6.4f + (i % 3)),
                           new Vector3(1.3f, 0.8f, 1.3f),
                           Palette.Shade(Palette.Rock1, 0.8f + i * 0.05f), "Deblai");
            }
            GameObject cart = Proto.Cube(p, new Vector3(3.6f, 0.7f, 7.4f), new Vector3(2.2f, 1.2f, 1.4f),
                                         Palette.Shade(Palette.Trunk, 0.8f), "Wagonnet");
            cart.transform.localRotation = Quaternion.Euler(0f, 24f, 14f);
            Proto.EndVisualOnly();
        }

        static void FarmRuin(Transform p, System.Random rng)
        {
            Color stone = Palette.Shade(Palette.Structure, 0.78f);
            for (int i = 0; i < 4; i++)
            {
                float h = 1.4f + (float)rng.NextDouble() * 2.6f;
                float a = (360f / 4f) * i * Mathf.Deg2Rad;
                GameObject wall = Proto.Cube(p,
                    new Vector3(Mathf.Sin(a) * 5f, h * 0.5f, Mathf.Cos(a) * 5f),
                    new Vector3(10f, h, 0.9f), stone, "Mur");
                wall.transform.localRotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * a + 90f, 0f);
            }

            Proto.BeginVisualOnly();
            for (int i = 0; i < 5; i++)
            {
                Proto.Cube(p, new Vector3(((float)rng.NextDouble() - 0.5f) * 9f, 0.3f,
                                          ((float)rng.NextDouble() - 0.5f) * 9f),
                           new Vector3(1.2f, 0.6f, 1.2f), Palette.Shade(stone, 0.85f), "Eboulis");
            }
            for (int i = 0; i < 3; i++)
            {
                Proto.Cube(p, new Vector3(-6.5f, 0.6f, -3f + i * 3f), new Vector3(0.22f, 1.2f, 2.6f),
                           Palette.Trunk, "Cloture");
            }
            Proto.Cube(p, new Vector3(6.4f, 1.4f, 0f), new Vector3(0.3f, 2.8f, 0.3f), Palette.Trunk, "Epouvantail");
            Proto.Cube(p, new Vector3(6.4f, 2.2f, 0f), new Vector3(2f, 0.24f, 0.24f), Palette.Trunk, "Bras");
            Proto.Cube(p, new Vector3(6.4f, 3f, 0f), new Vector3(0.7f, 0.7f, 0.7f),
                       Palette.Shade(Palette.Canvas, 0.9f), "Tete");
            Proto.EndVisualOnly();
        }

        static void WatchPost(Transform p)
        {
            Color wood = Palette.Shade(Palette.Trunk, 0.9f);

            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -2f : 2f;
                float z = (i < 2) ? -2f : 2f;
                Proto.Cube(p, new Vector3(x, 4f, z), new Vector3(0.6f, 8f, 0.6f), wood, "Pilotis");
            }

            Proto.Cube(p, new Vector3(0f, 8.2f, 0f), new Vector3(6f, 0.4f, 6f), wood, "Plancher");

            Proto.BeginVisualOnly();
            for (int i = 0; i < 4; i++)
            {
                float angle = 90f * i;
                float rad = angle * Mathf.Deg2Rad;
                GameObject rail = Proto.Cube(p,
                    new Vector3(Mathf.Sin(rad) * 2.8f, 8.9f, Mathf.Cos(rad) * 2.8f),
                    new Vector3(5.6f, 1f, 0.3f), wood, "Garde-corps");
                rail.transform.localRotation = Quaternion.Euler(0f, -angle, 0f);
            }

            GameObject roof = Proto.Cube(p, new Vector3(0f, 10.8f, 0f), new Vector3(5f, 2.4f, 5f),
                                         Palette.Roof, "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            GameObject brazier = Proto.Cube(p, new Vector3(1.6f, 9f, 1.6f), new Vector3(0.9f, 0.7f, 0.9f),
                                            Palette.Shade(Palette.Rock2, 0.8f), "Brasero");
            Proto.Cube(p, new Vector3(1.6f, 9.6f, 1.6f), new Vector3(0.7f, 0.6f, 0.7f),
                       new Color(1f, 0.68f, 0.3f), "Flamme");
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(p, false);
            lightGo.transform.localPosition = new Vector3(1.6f, 9.8f, 1.6f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.7f, 0.38f);
            light.range = 26f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
        }
    }
}
