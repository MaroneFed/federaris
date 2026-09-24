using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QUE LA SYLVE DONNE : le bois mort et la pierre-lune.
    /// (Le fer ancien, lui, est au chateau : voir Castle.IronCrate.)
    ///
    /// LE BOIS MORT n'a pas d'emplacements inventes. Il vient de ce que la foret a
    /// deja : chaque tronc couche est un gisement, et un fagot attend au pied d'un
    /// arbre mort sur trois. On le trouve donc la ou on s'attend a le trouver, et il
    /// y en a partout -- c'est la ressource commune.
    ///
    /// LA PIERRE-LUNE pousse dans les CREUX : les endroits plus bas que leur
    /// voisinage. On les cherche sur le relief reel, on en garde quarante espaces
    /// d'au moins 32 m, et on y pose une a trois pierres qui luisent. Un creux devient
    /// une petite clairiere bleue qu'on repere de loin dans la penombre -- un lieu,
    /// dont on se souvient, et vers lequel on revient.
    ///
    /// Reglages verifies sur le terrain exact (Tools/monde.py) : depuis n'importe ou,
    /// la pierre-lune la plus proche est a 72 m en moyenne, 187 m au pire. Rare, mais
    /// jamais introuvable.
    /// </summary>
    public static class Gathering
    {
        public static int MoonstoneCount;
        public static int HollowCount;
        public static int FagotCount;
        public static int LogSourceCount;

        static readonly Color Stick = new Color(0.36f, 0.29f, 0.21f);
        static readonly Color StickDark = new Color(0.25f, 0.20f, 0.15f);
        static readonly Color Twine = new Color(0.44f, 0.38f, 0.27f);
        static readonly Color RockWet = new Color(0.17f, 0.18f, 0.19f);
        static readonly Color MoonGlow = new Color(0.62f, 0.80f, 1f);

        public static void Reset()
        {
            Hollows.Clear();
            MoonstoneCount = 0;
            HollowCount = 0;
            FagotCount = 0;
            LogSourceCount = 0;
        }

        // ------------------------------------------------------------------ bois mort

        /// <summary>Un tronc couche devient un gisement de bois mort. Son collider sert deja.</summary>
        public static void MakeLogHarvestable(GameObject log, GameConfig cfg)
        {
            ResourceNode node = log.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 2;
            node.harvestDuration = cfg != null ? cfg.harvestDuration : 1.15f;
            node.respawnDelay = 150f;
            // Pas de visuel a faire fondre : un tronc ne retrecit pas quand on en casse
            // des branches. Le nombre restant s'affiche dans l'invite.
            node.Initialise(ResourceType.Deadwood, 8, null);
            LogSourceCount++;
        }

        /// <summary>
        /// Un fagot : quelques branches croisees et liees. Son collider est un DECLENCHEUR :
        /// on le trouve avec E, mais on marche dessus sans buter.
        /// </summary>
        public static void Fagot(Transform parent, Vector3 at, System.Random rng, GameConfig cfg)
        {
            GameObject go = new GameObject("Fagot");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.3f, 0f);
            trigger.size = new Vector3(1.4f, 0.7f, 1.4f);

            Proto.BeginVisualOnly();
            GameObject visual = new GameObject("Visuel");
            visual.transform.SetParent(go.transform, false);
            int sticks = 5 + rng.Next(3);
            for (int i = 0; i < sticks; i++)
            {
                float yaw = (i * 37f + (float)rng.NextDouble() * 20f);
                GameObject s = Proto.Cube(visual.transform,
                                          new Vector3(((float)rng.NextDouble() - 0.5f) * 0.25f, 0.08f + i * 0.06f,
                                                      ((float)rng.NextDouble() - 0.5f) * 0.25f),
                                          new Vector3(0.07f, 0.07f, 1.1f + (float)rng.NextDouble() * 0.4f),
                                          i % 2 == 0 ? Stick : StickDark, "Branche");
                s.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 14f, yaw, 0f);
            }
            Proto.Cube(visual.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.28f, 0.1f, 0.28f), Twine, "Lien");
            Proto.EndVisualOnly();

            ResourceNode node = go.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 2;
            node.harvestDuration = cfg != null ? cfg.harvestDuration : 1.15f;
            node.respawnDelay = 150f;
            node.Initialise(ResourceType.Deadwood, 6, visual.transform);
            FagotCount++;
        }

        // ------------------------------------------------------------------ pierre-lune

        struct Hollow
        {
            public float depth;
            public Vector2 at;
        }

        /// <summary>Les creux retenus. Calcules AVANT la foret, pour qu'elle les laisse degages.</summary>
        static readonly List<Vector2> Hollows = new List<Vector2>();

        /// <summary>Centre de chaque creux (x, z). Lu par Ambiance pour y poser des lucioles.</summary>
        public static int HollowSpotCount { get { return Hollows.Count; } }
        public static Vector2 HollowSpot(int index) { return Hollows[index]; }

        /// <summary>
        /// Cherche les creux sur une grille de 5 m : un point est un creux s'il est plus
        /// bas que la moyenne de huit points pris a 18 m autour de lui. On garde les
        /// plus profonds d'abord, a 32 m les uns des autres au moins.
        ///
        /// Ne depend que du relief : a appeler apres Ground.Build et AVANT Forest.Plant.
        /// </summary>
        public static void FindHollows(GameConfig cfg)
        {
            Hollows.Clear();
            float half = cfg.mapSize * 0.5f - 15f;
            const float Step = 5f;
            const float Probe = 18f;
            const float MinDepth = 0.5f;
            const float Spacing = 32f;
            const int Target = 40;

            List<Hollow> found = new List<Hollow>();
            for (float x = -half; x <= half; x += Step)
            {
                for (float z = -half; z <= half; z += Step)
                {
                    if (Castle.Covers(x, z, 14f) || Monument.Near(x, z, 10f)) continue;
                    float h = Ground.Sample(x, z);
                    float around = 0f;
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * Mathf.PI * 0.25f;
                        around += Ground.Sample(x + Mathf.Cos(a) * Probe, z + Mathf.Sin(a) * Probe);
                    }
                    float depth = around / 8f - h;
                    if (depth > MinDepth)
                    {
                        Hollow hollow = new Hollow();
                        hollow.depth = depth;
                        hollow.at = new Vector2(x, z);
                        found.Add(hollow);
                    }
                }
            }

            found.Sort((a, b) => b.depth.CompareTo(a.depth));

            for (int i = 0; i < found.Count && Hollows.Count < Target; i++)
            {
                Vector2 p = found[i].at;
                bool clear = true;
                for (int k = 0; k < Hollows.Count; k++)
                {
                    if ((Hollows[k] - p).sqrMagnitude < Spacing * Spacing) { clear = false; break; }
                }
                if (clear) Hollows.Add(p);
            }
        }

        /// <summary>Vrai si ce point tombe dans un creux : la foret n'y plante pas d'arbre.</summary>
        public static bool NearHollow(float x, float z, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < Hollows.Count; i++)
            {
                float dx = Hollows[i].x - x, dz = Hollows[i].y - z;
                if (dx * dx + dz * dz < r2) return true;
            }
            return false;
        }

        public static void PlaceMoonstones(Transform parent, GameConfig cfg, System.Random rng)
        {
            GameObject root = new GameObject("PIERRES-LUNE");
            root.transform.SetParent(parent, false);
            for (int i = 0; i < Hollows.Count; i++)
            {
                BuildHollow(root.transform, Hollows[i], rng, cfg);
            }
        }

        static void BuildHollow(Transform parent, Vector2 centre, System.Random rng, GameConfig cfg)
        {
            GameObject hollow = new GameObject("Creux");
            hollow.transform.SetParent(parent, false);
            hollow.transform.position = Ground.Place(centre.x, centre.y, 0f);

            List<ResourceNode> nodes = new List<ResourceNode>();
            int stones = 1 + rng.Next(3);
            for (int i = 0; i < stones; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = i == 0 ? 0f : 2.5f + (float)rng.NextDouble() * 4.5f;
                float x = centre.x + Mathf.Cos(a) * d;
                float z = centre.y + Mathf.Sin(a) * d;
                nodes.Add(Moonstone(hollow.transform, Ground.Place(x, z, 0f), rng, cfg));
            }

            // Une seule lumiere par creux, pas par pierre : quarante lumieres, pas cent.
            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(hollow.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = MoonGlow;
            light.range = 7f;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;

            HollowGlow glow = hollow.AddComponent<HollowGlow>();
            glow.glowLight = light;
            glow.nodes = nodes.ToArray();
            glow.full = light.intensity;
            HollowCount++;
        }

        static ResourceNode Moonstone(Transform parent, Vector3 at, System.Random rng, GameConfig cfg)
        {
            GameObject go = new GameObject("Pierre-lune");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            CapsuleCollider body = go.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.45f, 0f);
            body.radius = 0.55f;
            body.height = 1.1f;

            Proto.BeginVisualOnly();
            GameObject rock = Proto.Cube(go.transform, new Vector3(0f, 0.12f, 0f),
                                         new Vector3(1.2f, 0.45f, 1.0f), RockWet, "Socle");
            rock.transform.localRotation = Quaternion.Euler(6f, 20f, -4f);

            GameObject visual = new GameObject("Eclats");
            visual.transform.SetParent(go.transform, false);
            Material shine = MaterialFactory.GetGlow(MoonGlow, 1.6f);
            int shards = 2 + rng.Next(3);
            for (int i = 0; i < shards; i++)
            {
                float h = 0.45f + (float)rng.NextDouble() * 0.55f;
                GameObject shard = Proto.Cube(visual.transform,
                                              new Vector3(((float)rng.NextDouble() - 0.5f) * 0.5f, 0.3f + h * 0.5f,
                                                          ((float)rng.NextDouble() - 0.5f) * 0.4f),
                                              new Vector3(0.16f, h, 0.16f), MoonGlow, "Eclat");
                shard.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 36f,
                                                                 (float)rng.NextDouble() * 90f,
                                                                 ((float)rng.NextDouble() - 0.5f) * 36f);
                Renderer r = shard.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = shine;
            }
            Proto.EndVisualOnly();

            ResourceNode node = go.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 1;
            node.harvestDuration = (cfg != null ? cfg.harvestDuration : 1.15f) * 1.4f;
            node.respawnDelay = 180f;
            node.Initialise(ResourceType.Moonstone, 4, visual.transform);
            MoonstoneCount++;
            return node;
        }
    }

    /// <summary>
    /// La lueur d'un creux s'eteint quand toutes ses pierres ont ete prises, et se
    /// rallume quand elles repoussent. Sinon on marcherait vers une lumiere vide.
    /// </summary>
    public class HollowGlow : MonoBehaviour
    {
        public Light glowLight;
        public ResourceNode[] nodes;
        public float full = 1f;

        void Update()
        {
            if (glowLight == null || nodes == null) return;
            bool any = false;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && !nodes[i].IsDepleted) { any = true; break; }
            }
            glowLight.intensity = Mathf.MoveTowards(glowLight.intensity, any ? full : 0f, Time.deltaTime * 0.8f);
        }
    }
}
