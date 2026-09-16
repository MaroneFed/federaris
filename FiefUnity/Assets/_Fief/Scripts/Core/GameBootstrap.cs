using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE point d'entree du jeu.
    ///
    /// La scene Main.unity ne contient qu'UN seul objet : celui qui porte ce script.
    /// Tout le reste (sol, lumiere, marche, fiefs, gisements, joueur, camera, HUD)
    /// est fabrique ici, au lancement.
    ///
    /// Pourquoi ce choix, alors qu'Unity permet de tout poser a la main dans l'editeur ?
    ///  - Une scene construite par code se lit et se relit dans Git (une scene .unity
    ///    binaire/YAML est illisible en diff, et c'est LA source des conflits quand
    ///    on est deux a bosser dessus).
    ///  - Tu changes une valeur dans GameConfig, tu relances : la map entiere suit.
    ///  - Quand ton frere aura les assets Kenney/Synty, on remplacera les primitives
    ///    par des Prefabs dans NodeFactory / BuildingFactory. Rien d'autre ne bouge.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        GameConfig config;
        Transform worldRoot;
        OrbitCamera orbitCamera;
        Camera viewCamera;
        System.Random rng;

        readonly List<Vector3> occupied = new List<Vector3>();

        void Awake()
        {
            config = GetComponent<GameConfig>();
            if (config == null) config = gameObject.AddComponent<GameConfig>();
            if (config.zones == null || config.zones.Count == 0) config.zones = GameConfig.DefaultZones();

            Game.Reset();
            Toasts.Clear();

            Game.Config = config;
            Game.Inventory = new Inventory();
            Game.Inventory.MaxWeight = config.maxWeight;
            Game.Wallet = new Wallet(config.startingGold);
            Game.Market = new Market(config);
            Game.Fief = new FiefState();

            rng = new System.Random(config.worldSeed);
            worldRoot = new GameObject("=== MONDE ===").transform;

            QualitySettings.shadowDistance = 90f;

            BuildEnvironment();
            BuildMarket();
            BuildFiefs();
            BuildResourceNodes();

            PlayerController player = BuildPlayer();
            BuildHud(player);

            Toasts.Show("Recolte, vends au marche, construis ton fief.", Palette.Gold);
            Toasts.Show("F1 pour les commandes.", UiStyle.Ink);
        }

        void Update()
        {
            // Le marche vit meme quand personne ne le regarde : les stocks derivent
            // vers leur equilibre, donc les prix se remettent d'un krach.
            if (Game.Market != null) Game.Market.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            Game.Reset();
        }

        // ================================================================ decor

        void BuildEnvironment()
        {
            GameObject sunGo = new GameObject("Soleil");
            sunGo.transform.SetParent(worldRoot, false);
            sunGo.transform.rotation = Quaternion.Euler(46f, 38f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.87f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.64f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.46f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.23f, 0.20f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Sky;
            RenderSettings.fogStartDistance = 130f;
            RenderSettings.fogEndDistance = 430f;

            float size = config.mapSize;
            Proto.Make(PrimitiveType.Plane, worldRoot, Vector3.zero,
                       new Vector3(size / 10f, 1f, size / 10f), Palette.Grass, "Sol");

            // Quelques taches d'herbe plus sombre : casse la platitude sans rien couter.
            for (int i = 0; i < 26; i++)
            {
                Vector3 p = new Vector3(RandomRange(-size * 0.45f, size * 0.45f), 0f,
                                        RandomRange(-size * 0.45f, size * 0.45f));
                Proto.Pad(worldRoot, p, RandomRange(9f, 22f), Palette.GrassDark, "Herbe", 0.02f);
            }

            // Falaises de bordure : on ne tombe pas de la carte.
            float half = size * 0.5f;
            BuildBorder(new Vector3(0f, 3f, half), new Vector3(size + 8f, 6f, 4f));
            BuildBorder(new Vector3(0f, 3f, -half), new Vector3(size + 8f, 6f, 4f));
            BuildBorder(new Vector3(half, 3f, 0f), new Vector3(4f, 6f, size + 8f));
            BuildBorder(new Vector3(-half, 3f, 0f), new Vector3(4f, 6f, size + 8f));
        }

        void BuildBorder(Vector3 position, Vector3 scale)
        {
            Proto.Cube(worldRoot, position, scale, Palette.Cliff, "Falaise");
        }

        // ================================================================ marche

        void BuildMarket()
        {
            Vector3 center = Vector3.zero;
            Game.MarketPosition = center;

            GameObject root = new GameObject("MARCHE CENTRAL");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = center;

            Proto.Pad(root.transform, Vector3.zero, config.marketRadius + 5f, Palette.Plaza, "Place", 0.07f);

            // Le puits central : le point de repere visuel de toute la carte.
            Proto.Cylinder(root.transform, new Vector3(0f, 0.6f, 0f),
                           new Vector3(3.2f, 0.6f, 3.2f), Palette.Stone, "Puits");
            GameObject roofPost = Proto.Cylinder(root.transform, new Vector3(0f, 2.4f, 0f),
                                                 new Vector3(0.25f, 1.8f, 0.25f), Palette.Trunk, "Poteau");
            Proto.StripCollider(roofPost);
            GameObject wellRoof = Proto.Cube(root.transform, new Vector3(0f, 4.4f, 0f),
                                             new Vector3(3.4f, 1.4f, 3.4f), Palette.Roof, "Toiture");
            wellRoof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Proto.StripCollider(wellRoof);

            // Les etals, en cercle autour du puits.
            int stalls = 6;
            for (int i = 0; i < stalls; i++)
            {
                float angle = (360f / stalls) * i * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (config.marketRadius - 3.5f);

                GameObject stall = new GameObject("Etal_" + (i + 1));
                stall.transform.SetParent(root.transform, false);
                stall.transform.localPosition = p;
                stall.transform.localRotation = Quaternion.LookRotation(-p.normalized, Vector3.up);

                Proto.Cube(stall.transform, new Vector3(0f, 0.55f, 0f),
                           new Vector3(3.2f, 1.1f, 1.4f), Palette.Trunk, "Comptoir");
                GameObject canopy = Proto.Cube(stall.transform, new Vector3(0f, 2.3f, 0f),
                                               new Vector3(3.6f, 0.25f, 2.2f),
                                               i % 2 == 0 ? Palette.Canvas : Palette.Shade(Palette.Canvas, 0.82f),
                                               "Bache");
                canopy.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
                Proto.StripCollider(canopy);

                for (int s = -1; s <= 1; s += 2)
                {
                    GameObject post = Proto.Cylinder(stall.transform, new Vector3(s * 1.5f, 1.15f, 0f),
                                                     new Vector3(0.14f, 1.15f, 0.14f), Palette.Trunk, "Montant");
                    Proto.StripCollider(post);
                }

                GameObject crate = Proto.Cube(stall.transform, new Vector3(1.0f, 0.35f, 1.1f),
                                              new Vector3(0.7f, 0.7f, 0.7f),
                                              ResourceInfo.Tint(ResourceInfo.All[i % ResourceInfo.Count]), "Caisse");
                Proto.StripCollider(crate);
            }

            // La zone d'interaction : un trigger large, on n'a pas a viser un PNJ.
            GameObject zoneGo = new GameObject("ZoneDeNegoce");
            zoneGo.transform.SetParent(root.transform, false);
            SphereCollider trigger = zoneGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = config.marketRadius;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            zoneGo.AddComponent<MarketZone>();
        }

        // ================================================================ fiefs

        void BuildFiefs()
        {
            for (int i = 0; i < config.fiefCount; i++)
            {
                Vector3 center = config.FiefPosition(i);
                bool isPlayer = (i == config.playerFiefIndex);

                GameObject root = new GameObject(isPlayer ? "TON FIEF" : "Fief_" + (i + 1) + "_(Phase 3)");
                root.transform.SetParent(worldRoot, false);
                root.transform.position = center;

                Proto.Pad(root.transform, Vector3.zero, 30f, Palette.Dirt, "Terrasse", 0.05f);
                Proto.Banner(root.transform, new Vector3(0f, 0f, -12f), Palette.Banner(i), 7f, "Banniere");

                if (isPlayer)
                {
                    Game.HomeFiefPosition = center;
                    Game.Fief.center = center;
                    BuildPlots(root.transform);
                }
                else
                {
                    // Les fiefs rivaux existent deja visuellement : c'est la place
                    // que prendront les autres joueurs en Phase 3. Rien de jouable ici.
                    BuildRivalDecor(root.transform, Palette.Banner(i));
                }
            }
        }

        void BuildPlots(Transform parent)
        {
            GameObject plotsRoot = new GameObject("Emplacements");
            plotsRoot.transform.SetParent(parent, false);

            for (int i = 0; i < 6; i++)
            {
                float x = ((i % 3) - 1) * 8.6f;
                float z = ((i / 3) - 0.5f) * 9.2f + 3f;
                BuildingFactory.CreatePlot(plotsRoot.transform, parent.position + new Vector3(x, 0f, z), i);
            }
        }

        void BuildRivalDecor(Transform parent, Color banner)
        {
            for (int i = -1; i <= 1; i++)
            {
                GameObject wall = Proto.Cube(parent, new Vector3(i * 4.2f, 1.1f, 6f),
                                             new Vector3(4f, 2.2f, 0.8f), Palette.Structure, "MurRuine");
                wall.transform.localRotation = Quaternion.Euler(0f, 0f, i * 2.5f);
            }
            GameObject keep = Proto.Cube(parent, new Vector3(0f, 1.6f, 0f),
                                         new Vector3(5f, 3.2f, 5f), Palette.Shade(Palette.Structure, 0.9f), "Donjon");
            GameObject roof = Proto.Cube(parent, new Vector3(0f, 3.8f, 0f),
                                         new Vector3(4f, 1.6f, 4f), banner, "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Proto.StripCollider(roof);
            Proto.StripCollider(keep);
        }

        // ================================================================ gisements

        void BuildResourceNodes()
        {
            GameObject root = new GameObject("GISEMENTS");
            root.transform.SetParent(worldRoot, false);

            occupied.Clear();
            occupied.Add(Game.MarketPosition);
            for (int i = 0; i < config.fiefCount; i++) occupied.Add(config.FiefPosition(i));

            int total = 0;
            for (int z = 0; z < config.zones.Count; z++)
            {
                ResourceZone zone = config.zones[z];
                if (zone == null) continue;

                GameObject zoneRoot = new GameObject(zone.name);
                zoneRoot.transform.SetParent(root.transform, false);

                for (int n = 0; n < zone.nodeCount; n++)
                {
                    Vector3 position;
                    if (!FindFreeSpot(zone, out position)) continue;
                    NodeFactory.Create(zoneRoot.transform, zone.type, position, rng);
                    occupied.Add(position);
                    total++;
                }
            }

            Debug.Log("[FIEF] Monde genere : " + total + " gisements, graine " + config.worldSeed + ".");
        }

        /// <summary>
        /// Tire une position au hasard DANS la zone, en refusant ce qui tombe trop pres
        /// du marche, d'un fief ou d'un autre gisement. Tirage seedé = map reproductible.
        /// </summary>
        bool FindFreeSpot(ResourceZone zone, out Vector3 position)
        {
            float limit = config.mapSize * 0.5f - 12f;

            for (int attempt = 0; attempt < 40; attempt++)
            {
                float angle = RandomRange(0f, Mathf.PI * 2f);
                float radius = zone.radius * Mathf.Sqrt(RandomRange(0f, 1f));
                Vector3 candidate = new Vector3(zone.center.x + Mathf.Cos(angle) * radius, 0f,
                                                zone.center.y + Mathf.Sin(angle) * radius);

                if (Mathf.Abs(candidate.x) > limit || Mathf.Abs(candidate.z) > limit) continue;
                if (candidate.magnitude < config.marketRadius + 14f) continue;

                bool clear = true;
                for (int i = 0; i < occupied.Count; i++)
                {
                    float minDistance = (i <= config.fiefCount) ? 34f : 5.5f;
                    if ((occupied[i] - candidate).sqrMagnitude < minDistance * minDistance)
                    {
                        clear = false;
                        break;
                    }
                }

                if (!clear) continue;
                position = candidate;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        float RandomRange(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        // ================================================================ joueur

        PlayerController BuildPlayer()
        {
            // On apparait en bord de fief, tourne vers lui : la premiere image du jeu
            // montre ta banniere et tes 6 emplacements de construction.
            Vector3 spawn = Game.HomeFiefPosition + new Vector3(0f, 1.2f, -24f);

            GameObject go = new GameObject("JOUEUR");
            go.transform.position = spawn;
            go.transform.rotation = Quaternion.LookRotation(
                (Game.HomeFiefPosition - spawn).normalized, Vector3.up);

            CharacterController controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 52f;
            controller.stepOffset = 0.42f;
            controller.skinWidth = 0.03f;

            // Apparence : un petit bonhomme lisible, sans aucun asset.
            GameObject visual = new GameObject("Visuel");
            visual.transform.SetParent(go.transform, false);
            Color tunic = Palette.Banner(config.playerFiefIndex);

            Proto.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0f, 0.92f, 0f),
                       new Vector3(0.78f, 0.6f, 0.78f), tunic, "Torse");
            Proto.Sphere(visual.transform, new Vector3(0f, 1.66f, 0f),
                         new Vector3(0.48f, 0.5f, 0.48f), new Color(0.88f, 0.75f, 0.62f), "Tete");
            Proto.Cube(visual.transform, new Vector3(0f, 1.78f, 0.06f),
                       new Vector3(0.52f, 0.2f, 0.54f), Palette.Shade(tunic, 0.7f), "Chapeau");
            Proto.Cube(visual.transform, new Vector3(0f, 1.62f, 0.24f),
                       new Vector3(0.2f, 0.12f, 0.12f), new Color(0.25f, 0.2f, 0.18f), "Regard");
            Proto.Cube(visual.transform, new Vector3(0f, 0.95f, -0.32f),
                       new Vector3(0.6f, 0.9f, 0.12f), Palette.Shade(tunic, 0.75f), "Cape");
            Proto.Cube(visual.transform, new Vector3(0.42f, 0.9f, 0f),
                       new Vector3(0.18f, 0.6f, 0.18f), tunic, "BrasD");
            Proto.Cube(visual.transform, new Vector3(-0.42f, 0.9f, 0f),
                       new Vector3(0.18f, 0.6f, 0.18f), tunic, "BrasG");
            Proto.StripCollidersRecursive(visual);

            // La camera. AudioListener dessus : c'est l'oreille du jeu.
            GameObject camGo = new GameObject("CAMERA");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 800f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";

            viewCamera = cam;

            OrbitCamera orbit = camGo.AddComponent<OrbitCamera>();
            orbit.target = go.transform;
            orbit.yaw = go.transform.eulerAngles.y;
            orbitCamera = orbit;

            PlayerController player = go.AddComponent<PlayerController>();
            player.cameraTransform = camGo.transform;
            go.AddComponent<PlayerInteractor>();

            Game.Player = player;
            Game.PlayerTransform = go.transform;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            return player;
        }

        void BuildHud(PlayerController player)
        {
            GameObject go = new GameObject("HUD");
            Hud hud = go.AddComponent<Hud>();
            hud.orbitCamera = orbitCamera;
            hud.interactor = player.GetComponent<PlayerInteractor>();
            hud.viewCamera = viewCamera;
            Game.Hud = hud;
        }
    }
}
