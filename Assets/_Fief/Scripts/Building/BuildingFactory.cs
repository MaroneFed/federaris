using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Fabrique les emplacements et les batiments en primitives low-poly.
    /// C'est le SEUL fichier a remplacer quand on passera aux vrais modeles 3D :
    /// la logique de construction (BuildPlot) n'y touche pas.
    /// </summary>
    public static class BuildingFactory
    {
        /// <summary>Les batiments sont agrandis pour tenir l'echelle de la cour du chateau.</summary>
        public const float BuildingScale = 1.7f;


        /// <summary>Cree un emplacement libre : une dalle + 4 piquets + un halo au sol.</summary>
        public static BuildPlot CreatePlot(Transform parent, Vector3 position, int index)
        {
            GameObject root = new GameObject("Emplacement_" + (index + 1));
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            GameObject slab = Proto.Cube(root.transform, new Vector3(0f, 0.06f, 0f),
                                         new Vector3(9f, 0.14f, 9f), Palette.PlotFree, "Dalle");
            Proto.StripCollider(slab);

            GameObject marker = new GameObject("MarqueurLibre");
            marker.transform.SetParent(root.transform, false);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -4f : 4f;
                float z = (i < 2) ? -4f : 4f;
                GameObject post = Proto.Cube(marker.transform, new Vector3(x, 0.5f, z),
                                             new Vector3(0.26f, 1.5f, 0.26f), Palette.Trunk, "Piquet");
                Proto.StripCollider(post);
            }

            // Zone de detection : un trigger, il ne bloque pas le deplacement.
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(9.4f, 5f, 9.4f);
            trigger.center = new Vector3(0f, 2.3f, 0f);

            BuildPlot plot = root.AddComponent<BuildPlot>();
            plot.Initialise(index, marker);
            return plot;
        }

        public static void Spawn(BuildPlot plot, BuildingDef def)
        {
            GameObject root = new GameObject(def.name);
            root.transform.SetParent(plot.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one * BuildingScale;

            switch (def.id)
            {
                case BuildingId.Chest: BuildChest(root.transform, def); break;
                case BuildingId.Wall: BuildWall(root.transform, def); break;
                case BuildingId.Watchtower: BuildWatchtower(root.transform, def); break;
                case BuildingId.Sawmill: BuildSawmill(root.transform, def); break;
                case BuildingId.Workshop: BuildWorkshop(root.transform, def); break;
            }
        }

        static void BuildChest(Transform parent, BuildingDef def)
        {
            Proto.Cube(parent, new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.1f, 1.5f), def.color, "Caisse");
            GameObject lid = Proto.Cube(parent, new Vector3(0f, 1.22f, 0f),
                                        new Vector3(2.3f, 0.28f, 1.6f), Palette.Shade(def.color, 0.75f), "Couvercle");
            Proto.StripCollider(lid);
            GameObject band = Proto.Cube(parent, new Vector3(0f, 0.8f, 0f),
                                         new Vector3(2.35f, 0.16f, 1.55f), Palette.Gold, "Ferrure");
            Proto.StripCollider(band);

            parent.gameObject.AddComponent<Chest>();
        }

        static void BuildWall(Transform parent, BuildingDef def)
        {
            for (int i = -1; i <= 1; i++)
            {
                Proto.Cube(parent, new Vector3(i * 1.7f, 1.2f, 0f),
                           new Vector3(1.65f, 2.4f, 0.7f),
                           Palette.Shade(def.color, 0.94f + i * 0.04f), "Section" + (i + 1));
                GameObject merlon = Proto.Cube(parent, new Vector3(i * 1.7f, 2.6f, 0f),
                                               new Vector3(0.7f, 0.5f, 0.7f), def.color, "Creneau" + (i + 1));
                Proto.StripCollider(merlon);
            }
        }

        static void BuildWatchtower(Transform parent, BuildingDef def)
        {
            Proto.Cylinder(parent, new Vector3(0f, 2.6f, 0f), new Vector3(1.5f, 2.6f, 1.5f), def.color, "Fut");
            GameObject deck = Proto.Cylinder(parent, new Vector3(0f, 5.35f, 0f),
                                             new Vector3(2.1f, 0.22f, 2.1f), Palette.Trunk, "Plateforme");
            Proto.StripCollider(deck);
            GameObject roof = Proto.Cube(parent, new Vector3(0f, 6.1f, 0f),
                                         new Vector3(2.0f, 1.2f, 2.0f), Palette.Roof, "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Proto.StripCollider(roof);
            Proto.Banner(parent, new Vector3(0.9f, 5.5f, 0f), Palette.Banner(0), 2.4f);
        }

        static void BuildSawmill(Transform parent, BuildingDef def)
        {
            Proto.Cube(parent, new Vector3(0f, 1.1f, 0f), new Vector3(3.4f, 2.2f, 2.6f), def.color, "Hangar");
            GameObject roof = Proto.Cube(parent, new Vector3(0f, 2.5f, 0f),
                                         new Vector3(3.8f, 0.6f, 3.0f), Palette.Roof, "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Proto.StripCollider(roof);

            GameObject blade = Proto.Cylinder(parent, new Vector3(1.9f, 1.2f, 0f),
                                              new Vector3(1.6f, 0.06f, 1.6f), Palette.Stone, "Lame");
            blade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Proto.StripCollider(blade);
            Spinner spin = blade.AddComponent<Spinner>();
            spin.axis = Vector3.up;
            spin.degreesPerSecond = 150f;

            GameObject logs = Proto.Cylinder(parent, new Vector3(-1.9f, 0.35f, 0.6f),
                                             new Vector3(0.7f, 1.0f, 0.7f), Palette.Trunk, "Grume");
            logs.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Proto.StripCollider(logs);

            PassiveProducer producer = parent.gameObject.AddComponent<PassiveProducer>();
            producer.type = ResourceType.Wood;
            producer.amountPerCycle = 2;
            producer.interval = 8f;
        }

        static void BuildWorkshop(Transform parent, BuildingDef def)
        {
            Proto.Cube(parent, new Vector3(0f, 1.2f, 0f), new Vector3(3.6f, 2.4f, 3.0f), def.color, "Atelier");
            GameObject roof = Proto.Cube(parent, new Vector3(0f, 2.9f, 0f),
                                         new Vector3(2.8f, 1.4f, 3.4f), Palette.Roof, "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Proto.StripCollider(roof);

            GameObject anvil = Proto.Cube(parent, new Vector3(2.1f, 0.45f, 0.8f),
                                          new Vector3(0.9f, 0.55f, 0.55f), Palette.Shade(Palette.Stone, 0.55f), "Enclume");
            Proto.StripCollider(anvil);

            GameObject chimney = Proto.Cube(parent, new Vector3(-1.2f, 3.3f, 0f),
                                            new Vector3(0.5f, 1.2f, 0.5f), Palette.Shade(def.color, 0.7f), "Cheminee");
            Proto.StripCollider(chimney);
        }
    }
}
