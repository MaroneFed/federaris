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

            QualitySettings.shadowDistance = 160f;

            // L'ordre compte : le relief doit exister avant qu'on pose quoi que ce soit
            // dessus, et les chemins avant qu'on seme le decor (pour ne pas semer sur la route).
            Ground.Prepare(config);
            Scenery.Reset();

            BuildEnvironment();
            Scenery.BuildRoads(worldRoot, config);
            BuildMarket();
            BuildFiefs();
            Scenery.BuildLandmarks(worldRoot, config);
            BuildResourceNodes();
            Scenery.Scatter(worldRoot, config, rng, occupied, config.decorCount);

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
            sunGo.transform.rotation = Quaternion.Euler(42f, 35f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;

            // Un vrai ciel degrade au lieu d'un aplat de couleur.
            // Le shader "Skybox/Procedural" est fourni avec Unity : aucun asset a importer.
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.045f);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.85f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.55f, 0.68f, 0.86f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.42f, 0.42f, 0.38f));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.25f);
                RenderSettings.skybox = sky;
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            }
            else
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            }

            RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.48f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.25f, 0.21f);
            RenderSettings.ambientIntensity = 1.05f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.68f, 0.76f, 0.85f);
            RenderSettings.fogStartDistance = 260f;
            RenderSettings.fogEndDistance = 1050f;

            // Le terrain en relief. Les bords remontent en cuvette : plus besoin
            // de murs gris pour dire ou s'arrete le monde.
            Ground.Build(worldRoot, config);
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

                Proto.Pad(root.transform, Vector3.zero, 44f, Palette.Dirt, "Terrasse", 0.05f);
                Proto.Banner(root.transform, new Vector3(0f, 0f, -17f), Palette.Banner(i), 9f, "Banniere");

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
                float x = ((i % 3) - 1) * 9.4f;
                float z = ((i / 3) - 0.5f) * 10.4f + 4f;
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
                    position = Ground.Place(position, 0f);
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
                if (candidate.magnitude < config.marketRadius + 26f) continue;
                if (Scenery.DistanceToRoad(candidate.x, candidate.z) < 6f) continue;
                if (Ground.Slope(candidate.x, candidate.z) > 0.45f) continue;

                bool clear = true;
                for (int i = 0; i < occupied.Count; i++)
                {
                    float minDistance = (i <= config.fiefCount) ? 52f : 6.5f;
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
            Vector3 spawn = Ground.Place(Game.HomeFiefPosition + new Vector3(0f, 0f, -30f), 1.2f);

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

            // Le personnage : squelette articule, anime par le code (voir CharacterRig.cs).
            Color tunic = Palette.Banner(config.playerFiefIndex);
            CharacterRig rig = CharacterRig.Build(go.transform, tunic, Palette.Shade(tunic, 0.62f));
            Game.Rig = rig;

            // La camera. AudioListener dessus : c'est l'oreille du jeu.
            GameObject camGo = new GameObject("CAMERA");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null
                ? CameraClearFlags.Skybox
                : CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 1600f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";

            viewCamera = cam;

            OrbitCamera orbit = camGo.AddComponent<OrbitCamera>();
            orbit.target = go.transform;
            orbit.yaw = go.transform.eulerAngles.y;
            orbitCamera = orbit;

            PlayerController player = go.AddComponent<PlayerController>();
            player.cameraTransform = camGo.transform;
            player.rig = rig;
            go.AddComponent<PlayerInteractor>();

            Game.Player = player;
            Game.PlayerTransform = go.transform;

            // Les sons sont synthetises par le code et joues depuis le joueur.
            Sfx.Init(go);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            return player;
        }

        void BuildHud(PlayerController player)
        {
            GameObject go = new GameObject("INTERFACE");
            Hud hud = go.AddComponent<Hud>();
            hud.orbitCamera = orbitCamera;
            hud.interactor = player.GetComponent<PlayerInteractor>();
            hud.viewCamera = viewCamera;
            Game.Hud = hud;

            // L'ecran-titre. Il met le jeu en pause (Time.timeScale = 0) jusqu'a ce que
            // le joueur clique sur "Commencer la Saison".
            Menus menus = go.AddComponent<Menus>();
            hud.menus = menus;
        }
    }
}
