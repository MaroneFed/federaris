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

            float variation = 0.95f + (float)rng.NextDouble() * 0.55f;

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
            body.height = 3.6f * variation;
            body.radius = 0.85f * variation;
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

        /// <summary>
        /// Trois especes d'arbres au lieu d'une seule. Une foret ou tous les arbres sont
        /// identiques se lit comme un decor de carton-pate ; trois silhouettes suffisent
        /// a donner l'impression d'un bois.
        /// </summary>
        static void BuildTree(Transform parent, float variation, System.Random rng)
        {
            Color bark = Palette.Shade(Palette.Trunk, 0.85f + (float)rng.NextDouble() * 0.35f);
            Color leaf = Palette.Shade(Palette.Wood, 0.82f + (float)rng.NextDouble() * 0.4f);
            int species = rng.Next(3);

            if (species == 0)
            {
                // Chene : tronc court, houppier large et touffu.
                float height = 2.4f * variation;
                Proto.Cylinder(parent, new Vector3(0f, height * 0.5f, 0f),
                               new Vector3(0.34f, height * 0.5f, 0.34f), bark, "Tronc");
                Proto.Sphere(parent, new Vector3(0f, height + 0.7f, 0f),
                             new Vector3(2.9f * variation, 2.1f * variation, 2.9f * variation), leaf, "Houppier");
                Proto.Sphere(parent, new Vector3(0.7f * variation, height + 1.5f, -0.3f),
                             new Vector3(1.7f * variation, 1.4f * variation, 1.7f * variation), leaf, "Houppier2");
                Proto.Sphere(parent, new Vector3(-0.6f * variation, height + 1.2f, 0.4f),
                             new Vector3(1.5f * variation, 1.3f * variation, 1.5f * variation),
                             Palette.Shade(leaf, 0.88f), "Houppier3");
            }
            else if (species == 1)
            {
                // Sapin : haut, etage, silhouette pointue. C'est lui qui donne l'echelle.
                float height = 1.9f * variation;
                Proto.Cylinder(parent, new Vector3(0f, height * 0.5f, 0f),
                               new Vector3(0.26f, height * 0.5f, 0.26f), bark, "Tronc");
                Color needle = Palette.Shade(leaf, 0.78f);
                for (int i = 0; i < 4; i++)
                {
                    float t = i / 3f;
                    float w = Mathf.Lerp(2.7f, 0.8f, t) * variation;
                    GameObject tier = Proto.Cube(parent,
                        new Vector3(0f, height + 0.5f + i * 1.15f * variation, 0f),
                        new Vector3(w, 1.0f * variation, w),
                        Palette.Shade(needle, 1f - i * 0.05f), "Etage" + i);
                    tier.transform.localRotation = Quaternion.Euler(0f, 45f + i * 12f, 0f);
                }
            }
            else
            {
                // Bouleau : elance, feuillage leger, un peu penche.
                float height = 3.4f * variation;
                GameObject trunk = Proto.Cylinder(parent, new Vector3(0f, height * 0.5f, 0f),
                                                  new Vector3(0.2f, height * 0.5f, 0.2f),
                                                  Palette.Shade(bark, 1.5f), "Tronc");
                trunk.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 8f, 0f,
                                                                 ((float)rng.NextDouble() - 0.5f) * 8f);
                Color pale = Palette.Shade(leaf, 1.12f);
                Proto.Sphere(parent, new Vector3(0f, height + 0.6f, 0f),
                             new Vector3(1.9f * variation, 2.3f * variation, 1.9f * variation), pale, "Feuillage");
                Proto.Sphere(parent, new Vector3(0.35f, height + 1.7f, 0.2f),
                             new Vector3(1.2f * variation, 1.3f * variation, 1.2f * variation), pale, "Feuillage2");
            }
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
