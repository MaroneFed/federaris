using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Construit l'apparence low-poly des gisements a partir de primitives.
    /// Un seul endroit a modifier le jour ou on branche de vrais modeles 3D.
    /// </summary>
    public static class NodeFactory
    {
        public static ResourceNode Create(Transform parent, ResourceType type, Vector3 position, System.Random rng)
        {
            GameObject root = new GameObject("Gisement_" + ResourceInfo.Name(type));
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            GameObject visual = new GameObject("Visuel");
            visual.transform.SetParent(root.transform, false);

            float variation = 0.85f + (float)rng.NextDouble() * 0.35f;

            switch (type)
            {
                case ResourceType.Wood: BuildTree(visual.transform, variation, rng); break;
                case ResourceType.Stone: BuildRocks(visual.transform, variation, rng); break;
                case ResourceType.Iron: BuildIronVein(visual.transform, variation, rng); break;
            }

            // Un seul collider, sur la racine : c'est lui que le joueur "voit" pour interagir.
            // Les colliders des primitives decoratives sont retires pour ne pas polluer la detection.
            Proto.StripCollidersRecursive(visual);

            CapsuleCollider body = root.AddComponent<CapsuleCollider>();
            body.height = 3.2f * variation;
            body.radius = 0.75f * variation;
            body.center = new Vector3(0f, body.height * 0.5f, 0f);

            ResourceNode node = root.AddComponent<ResourceNode>();
            GameConfig cfg = Game.Config;
            if (cfg != null)
            {
                node.yieldPerHarvest = cfg.harvestYield;
                node.harvestDuration = cfg.harvestDuration;
                node.respawnDelay = cfg.nodeRespawnDelay;
                node.Initialise(type, cfg.nodeCapacity, visual.transform);
            }
            else
            {
                node.Initialise(type, 24, visual.transform);
            }

            return node;
        }

        static void BuildTree(Transform parent, float variation, System.Random rng)
        {
            float height = 2.2f * variation;
            Proto.Cylinder(parent, new Vector3(0f, height * 0.5f, 0f),
                           new Vector3(0.28f, height * 0.5f, 0.28f), Palette.Trunk, "Tronc");

            Color leaf = Palette.Shade(Palette.Wood, 0.9f + (float)rng.NextDouble() * 0.25f);
            Proto.Sphere(parent, new Vector3(0f, height + 0.55f, 0f),
                         new Vector3(2.1f * variation, 1.7f * variation, 2.1f * variation), leaf, "Feuillage");
            Proto.Sphere(parent, new Vector3(0.25f, height + 1.45f, -0.15f),
                         new Vector3(1.35f * variation, 1.2f * variation, 1.35f * variation), leaf, "Feuillage2");
        }

        static void BuildRocks(Transform parent, float variation, System.Random rng)
        {
            for (int i = 0; i < 3; i++)
            {
                float s = (0.9f + (float)rng.NextDouble() * 0.8f) * variation;
                Vector3 pos = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.4f,
                                          s * 0.35f,
                                          ((float)rng.NextDouble() - 0.5f) * 1.4f);
                GameObject rock = Proto.Cube(parent, pos, new Vector3(s, s * 0.8f, s),
                                             Palette.Shade(Palette.Stone, 0.85f + (float)rng.NextDouble() * 0.3f),
                                             "Rocher" + i);
                rock.transform.localRotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 22f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 22f);
            }
        }

        static void BuildIronVein(Transform parent, float variation, System.Random rng)
        {
            GameObject baseRock = Proto.Cube(parent, new Vector3(0f, 0.6f * variation, 0f),
                                             new Vector3(1.9f * variation, 1.2f * variation, 1.6f * variation),
                                             Palette.Shade(Palette.Stone, 0.72f), "Roche");
            baseRock.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 6f);

            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.5f,
                                          0.9f * variation + (float)rng.NextDouble() * 0.5f,
                                          ((float)rng.NextDouble() - 0.5f) * 1.2f);
                Proto.Sphere(parent, pos, Vector3.one * (0.45f + (float)rng.NextDouble() * 0.3f),
                             Palette.Iron, "Minerai" + i);
            }
        }
    }
}
