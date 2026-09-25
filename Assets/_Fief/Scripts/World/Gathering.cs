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
    /// arbre mort sur trois. On le trouve donc là où on s'attend a le trouver, et il
    /// y en a partout -- c'est la ressource commune.
    ///
    /// LA PIERRE-LUNE pousse dans les CREUX : les endroits plus bas que leur
    /// voisinage. On les cherche sur le relief reel, on en garde vingt-quatre espaces
    /// d'au moins 32 m, et on y pose une a trois pierres qui luisent. Un creux devient
    /// une petite clairiere bleue qu'on repere de loin dans la penombre -- un lieu,
    /// dont on se souvient, et vers lequel on revient.
    ///
    /// (Quarante creux sur la carte de 700 m ; vingt-quatre depuis qu'elle fait 420 m,
    /// le 26/09 : la densite reste la meme, un peu plus rare.)
    /// </summary>
    public static class Gathering
    {
        public static int MoonstoneCount;
        public static int HollowCount;
        public static int FagotCount;
        public static int LogSourceCount;

        /// <summary>Le bois mort, blanchi par les pluies : pale, presque os. Il sort de la penombre.</summary>
        public static readonly Color Bleached = new Color(0.74f, 0.7f, 0.62f);
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
            node.yieldPerHarvest = 4;
            // Aussi vif que le faisceau : casser des branches d'un tronc couche.
            node.harvestDuration = 0.5f;
            node.respawnDelay = 150f;
            // Pas de visuel a faire fondre : un tronc ne retrecit pas quand on en casse
            // des branches. Le nombre restant s'affiche dans l'invite.
            node.Initialise(ResourceType.Deadwood, 8, null);
            LogSourceCount++;
        }

        /// <summary>
        /// Un FAISCEAU de bois mort (refait le 26/09 -- Martin : "le bois, j'aimerais
        /// bien qu'il change"). Plus un fagot couche qu'on confondait avec les
        /// racines : des branches blanchies, DRESSEES les unes contre les autres en
        /// petite hutte, comme les laissent les bucherons. On le voit debout dans la
        /// brume, pale sur le sol sombre, avec une touffe de lichen vert-de-gris.
        ///
        /// On le prend d'un appui (a peine un temps de maintien, plus long si le sac
        /// est lourd), et les branches volent jusqu'a soi.
        /// Son collider est un DECLENCHEUR : on marche au travers sans buter.
        /// </summary>
        public static void Fagot(Transform parent, Vector3 at, System.Random rng, GameConfig cfg)
        {
            GameObject go = new GameObject("Faisceau de bois mort");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.6f, 0f);
            trigger.size = new Vector3(1.3f, 1.2f, 1.3f);

            Proto.BeginVisualOnly();
            GameObject visual = new GameObject("Visuel");
            visual.transform.SetParent(go.transform, false);
            Color[] barks = { Bleached, new Color(0.64f, 0.6f, 0.53f), new Color(0.8f, 0.77f, 0.7f), new Color(0.55f, 0.5f, 0.43f) };

            // Les perches : un cercle au sol, penchees vers un meme sommet.
            int poles = 6 + rng.Next(3);
            float height = 1.05f + (float)rng.NextDouble() * 0.3f;
            Vector3 top = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.1f, height, ((float)rng.NextDouble() - 0.5f) * 0.1f);
            for (int i = 0; i < poles; i++)
            {
                float a = (i + (float)rng.NextDouble() * 0.4f) / poles * Mathf.PI * 2f;
                float spread = 0.38f + (float)rng.NextDouble() * 0.12f;
                Vector3 foot = new Vector3(Mathf.Cos(a) * spread, 0f, Mathf.Sin(a) * spread);
                Vector3 tip = top + (top - foot).normalized * (0.1f + (float)rng.NextDouble() * 0.15f);
                Vector3 axis = tip - foot;
                float r = 0.025f + (float)rng.NextDouble() * 0.015f;
                GameObject pole = Proto.Cylinder(visual.transform, (foot + tip) * 0.5f, new Vector3(r * 2f, axis.magnitude * 0.5f, r * 2f),
                                                 barks[i % barks.Length], "Perche");
                pole.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
                // Un moignon de rameau sur une perche sur deux.
                if (i % 2 == 0)
                {
                    GameObject twig = Proto.Cube(visual.transform, foot + axis * 0.55f, new Vector3(0.018f, 0.018f, 0.22f), barks[(i + 1) % barks.Length], "Rameau");
                    twig.transform.localRotation = Quaternion.Euler(-30f, a * Mathf.Rad2Deg, 0f);
                }
            }
            // Un lien de corde pres du sommet.
            GameObject band = Proto.Cylinder(visual.transform, top - new Vector3(0f, 0.12f, 0f), new Vector3(0.13f, 0.025f, 0.13f), Twine, "Lien");
            band.transform.localRotation = Quaternion.identity;
            // Le lichen vert-de-gris au pied, et deux buches courtes posees contre.
            Color lichen = new Color(0.46f, 0.55f, 0.46f);
            for (int k = 0; k < 3; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                Proto.Sphere(visual.transform, new Vector3(Mathf.Cos(a) * 0.3f, 0.04f, Mathf.Sin(a) * 0.3f),
                             new Vector3(0.18f, 0.07f, 0.14f), lichen, "Lichen");
            }
            for (int k = 0; k < 2; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                GameObject log = Proto.Cylinder(visual.transform, new Vector3(Mathf.Cos(a) * 0.55f, 0.06f, Mathf.Sin(a) * 0.55f),
                                                new Vector3(0.12f, 0.22f, 0.12f), barks[k + 1], "Bûche");
                log.transform.localRotation = Quaternion.Euler(90f, a * Mathf.Rad2Deg + 90f, 0f);
                Proto.Cylinder(visual.transform, new Vector3(Mathf.Cos(a) * 0.55f, 0.06f, Mathf.Sin(a) * 0.55f) + Quaternion.Euler(0f, a * Mathf.Rad2Deg + 90f, 0f) * new Vector3(0f, 0f, 0.221f),
                               new Vector3(0.1f, 0.004f, 0.1f), new Color(0.82f, 0.7f, 0.5f), "Cerne").transform.localRotation = Quaternion.Euler(90f, a * Mathf.Rad2Deg + 90f, 0f);
            }
            // Des feuilles mortes autour.
            Color[] leaves = { new Color(0.42f, 0.31f, 0.14f), new Color(0.36f, 0.15f, 0.10f), new Color(0.30f, 0.21f, 0.13f) };
            for (int k = 0; k < 6; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = 0.35f + (float)rng.NextDouble() * 0.45f;
                GameObject leaf = Proto.Cube(visual.transform, new Vector3(Mathf.Cos(a) * d, 0.012f, Mathf.Sin(a) * d),
                                             new Vector3(0.12f, 0.006f, 0.08f), leaves[k % leaves.Length], "Feuille");
                leaf.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
            Proto.EndVisualOnly();

            // UN SEUL APPUI pour tout le faisceau : on repart avec les six branches.
            ResourceNode node = go.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 6;
            node.harvestDuration = 0.35f;
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
            const int Target = 24;          // 40 sur 700 m ; la carte fait 420 m (26/09)

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

        /// <summary>
        /// Deux eclats de pierre-lune a quelques pas de chaque stele (26/09 : que la
        /// premiere minute ait deja quelque chose a faire). Petits : ce n'est qu'une
        /// mise en route, le vrai gisement est dans les creux.
        /// </summary>
        public static void SeedNear(Transform parent, Vector3 stele, GameConfig cfg, System.Random rng)
        {
            int placed = 0;
            for (int tries = 0; tries < 40 && placed < 2; tries++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = 10f + (float)rng.NextDouble() * 10f;
                float x = stele.x + Mathf.Cos(a) * d, z = stele.z + Mathf.Sin(a) * d;
                if (Physics.CheckSphere(Ground.Place(x, z, 0.8f), 0.7f, ~0, QueryTriggerInteraction.Ignore)) continue;
                Moonstone(parent, Ground.Place(x, z, 0f), rng, cfg);
                placed++;
            }
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

            // UNE GRAPPE DE CRISTAUX : des prismes a six pans, pointus, qui sortent de
            // la roche en eventail -- un grand au centre, des plus petits autour --
            // et quelques eclats tombes a terre qui luisent aussi.
            GameObject visual = new GameObject("Éclats");
            visual.transform.SetParent(go.transform, false);
            Material shine = MaterialFactory.GetGlow(MoonGlow, 1.6f);
            Material deep = MaterialFactory.GetGlow(new Color(0.42f, 0.58f, 0.95f), 1.3f);
            int shards = 4 + rng.Next(3);
            for (int i = 0; i < shards; i++)
            {
                bool main = i == 0;
                float h = main ? 0.9f + (float)rng.NextDouble() * 0.35f : 0.35f + (float)rng.NextDouble() * 0.45f;
                float r = main ? 0.13f : 0.06f + (float)rng.NextDouble() * 0.05f;
                float a = i * 2.4f + (float)rng.NextDouble();
                float spread = main ? 0f : 0.18f + (float)rng.NextDouble() * 0.12f;
                Vector3 foot = new Vector3(Mathf.Cos(a) * spread, 0.28f, Mathf.Sin(a) * spread);
                GameObject crystal = new GameObject("Cristal");
                crystal.transform.SetParent(visual.transform, false);
                crystal.transform.localPosition = foot;
                // Chaque cristal penche vers l'exterieur de la grappe.
                crystal.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * (main ? 6f : 28f), 0f, -Mathf.Cos(a) * (main ? 6f : 28f));
                Material m = i % 3 == 1 ? deep : shine;
                GameObject prism = Proto.Cylinder(crystal.transform, new Vector3(0f, h * 0.35f, 0f), new Vector3(r * 2f, h * 0.35f, r * 2f), MoonGlow, "Prisme");
                prism.GetComponent<Renderer>().sharedMaterial = m;
                GameObject tip = Proto.Cone(crystal.transform, new Vector3(0f, h * 0.7f, 0f), r, h * 0.3f, MoonGlow, "Pointe", 6);
                tip.GetComponent<Renderer>().sharedMaterial = m;
            }
            for (int i = 0; i < 4; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = 0.55f + (float)rng.NextDouble() * 0.3f;
                GameObject chip = Proto.Cone(visual.transform, new Vector3(Mathf.Cos(a) * d, 0.02f, Mathf.Sin(a) * d), 0.04f, 0.1f, MoonGlow, "Éclat", 4);
                chip.transform.localRotation = Quaternion.Euler(70f, (float)rng.NextDouble() * 360f, 0f);
                chip.GetComponent<Renderer>().sharedMaterial = shine;
            }
            Proto.EndVisualOnly();

            ResourceNode node = go.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 2;         // une grappe se prend en deux gestes, pas quatre
            node.harvestDuration = 0.9f;
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
