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

            System.Diagnostics.Stopwatch chrono = System.Diagnostics.Stopwatch.StartNew();

            rng = new System.Random(config.worldSeed);
            worldRoot = new GameObject("=== MONDE ===").transform;

            QualitySettings.shadowDistance = 230f;

            // L'ordre compte : le relief doit exister avant qu'on pose quoi que ce soit
            // dessus, et les chemins avant qu'on seme le decor (pour ne pas semer sur la route).
            Ground.Prepare(config);
            Scenery.Reset();

            // L'ordre compte. Les gisements remplissent la liste des endroits occupes ;
            // tout ce qui vient apres s'en sert pour ne rien poser par-dessus.
            try
            {
            BuildEnvironment();
            Scenery.BuildRoads(worldRoot, config);
            BuildMarket();
            BuildFiefs();
            BuildLakes();
            BuildResourceNodes();
            Scenery.BuildLandmarks(worldRoot, config, occupied);
            Places.Build(worldRoot, config, rng, occupied, config.placeCount);
            Places.ScatterLoot(worldRoot, config, rng, occupied, config.lootCount);
            Wildlife.Populate(worldRoot, config, rng, occupied, config.herdCount);
            Scenery.PlantForests(worldRoot, config, rng, occupied, config.forestCount);
            Scenery.Scatter(worldRoot, config, rng, occupied, config.decorCount);
            Scenery.BuildClouds(worldRoot, config, rng);

            }
            catch (System.Exception error)
            {
                // Une panne pendant la construction laissait un monde a moitie fait,
                // sans le moindre message. Elle s'affiche desormais en rouge a l'ecran.
                Game.BuildError = error.GetType().Name + " dans " + error.StackTrace;
                Debug.LogError("[FIEF] Construction interrompue : " + error);
            }

            PlayerController player = BuildPlayer();
            BuildHud(player);

            Game.BuildMilliseconds = chrono.ElapsedMilliseconds;
            Debug.Log("[FIEF] Monde construit en " + chrono.ElapsedMilliseconds + " ms.");

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
            sunGo.transform.rotation = Quaternion.Euler(38f, 28f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.80f);
            sun.intensity = 1.32f;
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

            // Lumiere d'appoint froide venant de l'autre cote : les faces a l'ombre
            // ne sont plus des aplats noirs, elles prennent un bleu de ciel.
            // C'est ce contraste chaud/froid qui fait "ressortir" le low-poly.
            GameObject fillGo = new GameObject("Lumiere d'appoint");
            fillGo.transform.SetParent(worldRoot, false);
            fillGo.transform.rotation = Quaternion.Euler(28f, 212f, 0f);
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.56f, 0.68f, 0.88f);
            fill.intensity = 0.42f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.48f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.25f, 0.21f);
            RenderSettings.ambientIntensity = 1.05f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.68f, 0.76f, 0.85f);
            RenderSettings.fogStartDistance = 520f;
            RenderSettings.fogEndDistance = 2400f;

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

            float plaza = config.marketRadius + 9f;
            Proto.Pad(root.transform, Vector3.zero, plaza, Palette.Plaza, "Place", 0.07f);
            Proto.Pad(root.transform, Vector3.zero, plaza * 0.58f, Palette.Shade(Palette.Plaza, 0.9f), "Paves", 0.09f);

            // --- le puits, repere visuel de toute la carte
            Proto.Cylinder(root.transform, new Vector3(0f, 0.7f, 0f),
                           new Vector3(4.2f, 0.7f, 4.2f), Palette.Stone, "Puits");
            Proto.Cylinder(root.transform, new Vector3(0f, 1.45f, 0f),
                           new Vector3(3.4f, 0.12f, 3.4f), Palette.Shade(Palette.Water, 1.2f), "Eau");
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject post = Proto.Cylinder(root.transform, new Vector3(i * 1.6f, 3.1f, 0f),
                                                 new Vector3(0.28f, 1.7f, 0.28f), Palette.Trunk, "Montant");
                Proto.StripCollider(post);
            }
            GameObject wellRoof = Proto.Cube(root.transform, new Vector3(0f, 5.2f, 0f),
                                             new Vector3(4.6f, 1.7f, 4.6f), Palette.Roof, "Toiture");
            wellRoof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Proto.StripCollider(wellRoof);
            Proto.Banner(root.transform, new Vector3(0f, 5.6f, 0f), Palette.Gold, 3.4f, "Fanion");

            // --- les etals, en cercle
            int stalls = 6;
            for (int i = 0; i < stalls; i++)
            {
                float angle = (360f / stalls) * i * Mathf.Deg2Rad + 0.26f;
                Vector3 p = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (config.marketRadius - 3f);
                BuildStall(root.transform, p, i);
            }

            // --- les maisons du bourg, plus loin, dos tourne a la place
            int houses = 7;
            for (int i = 0; i < houses; i++)
            {
                float angle = (360f / houses) * i * Mathf.Deg2Rad + 0.55f;
                Vector3 p = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (plaza + 7f);
                BuildHouse(root.transform, p, i);
            }

            // --- torches : de la lumiere chaude qui accroche l'oeil de loin
            for (int i = 0; i < 5; i++)
            {
                float angle = (360f / 5f) * i * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (config.marketRadius - 7f);
                BuildTorch(root.transform, p);
            }

            GameObject zoneGo = new GameObject("ZoneDeNegoce");
            zoneGo.transform.SetParent(root.transform, false);
            SphereCollider trigger = zoneGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = config.marketRadius;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            zoneGo.AddComponent<MarketZone>();
        }

        void BuildStall(Transform parent, Vector3 position, int index)
        {
            GameObject stall = new GameObject("Etal_" + (index + 1));
            stall.transform.SetParent(parent, false);
            stall.transform.localPosition = position;
            stall.transform.localRotation = Quaternion.LookRotation(-position.normalized, Vector3.up);

            Proto.Cube(stall.transform, new Vector3(0f, 0.55f, 0f),
                       new Vector3(3.4f, 1.1f, 1.5f), Palette.Trunk, "Comptoir");

            GameObject canopy = Proto.Cube(stall.transform, new Vector3(0f, 2.5f, -0.1f),
                                           new Vector3(4f, 0.22f, 2.4f),
                                           index % 2 == 0 ? Palette.Canvas : Palette.Shade(Palette.Canvas, 0.8f),
                                           "Bache");
            canopy.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Proto.StripCollider(canopy);

            // liseres de couleur sur la bache : un marche, c'est bariole
            GameObject stripe = Proto.Cube(stall.transform, new Vector3(0f, 2.36f, 1.0f),
                                           new Vector3(4f, 0.1f, 0.5f), Palette.Banner(index), "Lisere");
            Proto.StripCollider(stripe);

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject post = Proto.Cylinder(stall.transform, new Vector3(side * 1.7f, 1.2f, 0f),
                                                 new Vector3(0.15f, 1.2f, 0.15f), Palette.Trunk, "Montant");
                Proto.StripCollider(post);
            }

            // marchandises
            for (int i = 0; i < 3; i++)
            {
                Color tint = ResourceInfo.Tint(ResourceInfo.All[(index + i) % ResourceInfo.Count]);
                GameObject crate = Proto.Cube(stall.transform,
                    new Vector3(-1.1f + i * 1.1f, 1.38f, 0.1f),
                    new Vector3(0.62f, 0.5f, 0.62f), tint, "Caisse");
                crate.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
                Proto.StripCollider(crate);
            }

            GameObject barrel = Proto.Cylinder(stall.transform, new Vector3(2.2f, 0.45f, 0.5f),
                                               new Vector3(0.75f, 0.45f, 0.75f), Palette.Shade(Palette.Trunk, 1.2f), "Tonneau");
            Proto.StripCollider(barrel);
        }

        void BuildHouse(Transform parent, Vector3 position, int index)
        {
            GameObject house = new GameObject("Maison_" + (index + 1));
            house.transform.SetParent(parent, false);
            house.transform.localPosition = position;
            house.transform.localRotation = Quaternion.LookRotation(-position.normalized, Vector3.up);

            float w = 5.5f + (index % 3) * 1.2f;
            float h = 3.4f + (index % 2) * 0.9f;

            Proto.Cube(house.transform, new Vector3(0f, h * 0.5f, 0f),
                       new Vector3(w, h, w * 0.8f), Palette.Structure, "Murs");

            // colombages : deux poutres croisees, ca suffit a dire "medieval"
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject beam = Proto.Cube(house.transform, new Vector3(i * w * 0.3f, h * 0.5f, w * 0.41f),
                                             new Vector3(0.22f, h * 0.95f, 0.1f), Palette.Trunk, "Poutre");
                Proto.StripCollider(beam);
            }
            GameObject cross = Proto.Cube(house.transform, new Vector3(0f, h * 0.55f, w * 0.41f),
                                          new Vector3(w * 0.8f, 0.2f, 0.1f), Palette.Trunk, "Traverse");
            Proto.StripCollider(cross);

            GameObject roof = Proto.Cube(house.transform, new Vector3(0f, h + w * 0.28f, 0f),
                                         new Vector3(w * 0.78f, w * 0.78f, w * 0.95f),
                                         Palette.Shade(Palette.Roof, 0.9f + (index % 3) * 0.12f), "Toit");
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Proto.StripCollider(roof);

            GameObject chimney = Proto.Cube(house.transform, new Vector3(w * 0.28f, h + w * 0.42f, -w * 0.2f),
                                            new Vector3(0.6f, 1.5f, 0.6f), Palette.Shade(Palette.Stone, 0.8f), "Cheminee");
            Proto.StripCollider(chimney);

            GameObject door = Proto.Cube(house.transform, new Vector3(0f, 1.05f, w * 0.41f),
                                         new Vector3(1.1f, 2.1f, 0.12f), Palette.Shade(Palette.Trunk, 0.8f), "Porte");
            Proto.StripCollider(door);
        }

        void BuildTorch(Transform parent, Vector3 position)
        {
            GameObject torch = new GameObject("Torche");
            torch.transform.SetParent(parent, false);
            torch.transform.localPosition = position;

            GameObject pole = Proto.Cylinder(torch.transform, new Vector3(0f, 1.5f, 0f),
                                             new Vector3(0.16f, 1.5f, 0.16f), Palette.Trunk, "Mat");
            Proto.StripCollider(pole);

            GameObject basket = Proto.Cylinder(torch.transform, new Vector3(0f, 3.15f, 0f),
                                               new Vector3(0.55f, 0.28f, 0.55f), Palette.Shade(Palette.Stone, 0.6f), "Panier");
            Proto.StripCollider(basket);

            GameObject flame = Proto.Sphere(torch.transform, new Vector3(0f, 3.6f, 0f),
                                            new Vector3(0.5f, 0.75f, 0.5f), new Color(1f, 0.72f, 0.3f), "Flamme");
            Proto.StripCollider(flame);

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(torch.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.7f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.74f, 0.42f);
            light.range = 16f;
            light.intensity = 2.1f;
            light.shadows = LightShadows.None;
        }

        // ================================================================ fiefs

        void BuildFiefs()
        {
            for (int i = 0; i < config.fiefCount; i++)
            {
                bool isPlayer = (i == config.playerFiefIndex);

                // En solo, les fiefs rivaux ne servent a rien et polluent le paysage.
                // Le terrain reste aplani a leur emplacement : la Phase 3 n'aura qu'a
                // y poser les fiefs des autres joueurs.
                if (!isPlayer && !config.showRivalFiefs) continue;

                Vector3 center = config.FiefPosition(i);
                GameObject root = new GameObject(isPlayer ? "TON FIEF" : "Fief_" + (i + 1));
                root.transform.SetParent(worldRoot, false);
                root.transform.position = center;

                Proto.Pad(root.transform, Vector3.zero, 86f, Palette.Dirt, "Terrasse", 0.05f);
                Proto.Pad(root.transform, Vector3.zero, 40f, Palette.Shade(Palette.Dirt, 1.1f), "Cour", 0.07f);

                if (isPlayer)
                {
                    Game.HomeFiefPosition = center;
                    Game.Fief.center = center;
                    BuildCastle(root.transform, Palette.Banner(i));
                    BuildPlots(root.transform);
                }
            }
        }

        /// <summary>
        /// Les 6 emplacements de construction, en deux rangees de trois dans la cour,
        /// derriere la porterie. Decision verrouillee du brief : on ne construit pas
        /// librement, on construit sur des emplacements definis.
        /// </summary>
        void BuildPlots(Transform parent)
        {
            GameObject plotsRoot = new GameObject("Emplacements");
            plotsRoot.transform.SetParent(parent, false);

            for (int i = 0; i < 6; i++)
            {
                float x = ((i % 3) - 1) * 23f;
                float z = ((i / 3) == 0) ? -26f : -9f;
                BuildingFactory.CreatePlot(plotsRoot.transform, parent.position + new Vector3(x, 0f, z), i);
            }
        }

        /// <summary>
        /// LE CHATEAU. Une vraie forteresse : une enceinte carree de 76 m de cote,
        /// quatre tours d'angle, une porterie avec son arche, un chemin de ronde
        /// crenele, et un donjon de 28 m au milieu de la cour.
        ///
        /// Repere : le joueur mesure 1,8 m. Les courtines font 9 m, les tours 19 m,
        /// le donjon 28 m. On passe la porte en se sentant petit, ce qui est le but.
        /// </summary>
        void BuildCastle(Transform parent, Color banner)
        {
            Color stone = Palette.Structure;
            Color darkStone = Palette.Shade(stone, 0.86f);

            const float half = 38f;        // demi-cote de l'enceinte
            const float wallHeight = 9f;
            const float wallThickness = 3.2f;
            const float gateGap = 11f;     // ouverture de la porte, au sud

            GameObject castle = new GameObject("Chateau");
            castle.transform.SetParent(parent, false);

            // ---------- les quatre courtines
            // Le sud est perce : c'est par la qu'on entre, face au marche.
            BuildWall(castle.transform, new Vector3(0f, 0f, half), half * 2f, wallHeight, wallThickness, true, stone);
            BuildWall(castle.transform, new Vector3(-half, 0f, 0f), half * 2f, wallHeight, wallThickness, false, stone);
            BuildWall(castle.transform, new Vector3(half, 0f, 0f), half * 2f, wallHeight, wallThickness, false, stone);

            float sideLength = half - gateGap * 0.5f;
            float sideCentre = gateGap * 0.5f + sideLength * 0.5f;
            BuildWall(castle.transform, new Vector3(-sideCentre, 0f, -half), sideLength, wallHeight, wallThickness, true, stone);
            BuildWall(castle.transform, new Vector3(sideCentre, 0f, -half), sideLength, wallHeight, wallThickness, true, stone);

            // ---------- les quatre tours d'angle
            for (int i = 0; i < 4; i++)
            {
                float tx = (i % 2 == 0) ? -half : half;
                float tz = (i < 2) ? -half : half;
                BuildTower(castle.transform, new Vector3(tx, 0f, tz), 5.4f, 19f, stone, banner, i == 0 || i == 1);
            }

            // ---------- la porterie
            GameObject gate = new GameObject("Porterie");
            gate.transform.SetParent(castle.transform, false);
            gate.transform.localPosition = new Vector3(0f, 0f, -half);

            for (int side = -1; side <= 1; side += 2)
            {
                BuildTower(gate.transform, new Vector3(side * (gateGap * 0.5f + 3.4f), 0f, 0f),
                           4.2f, 23f, darkStone, banner, true);
            }

            GameObject lintel = Proto.Cube(gate.transform, new Vector3(0f, 10.5f, 0f),
                                           new Vector3(gateGap + 8f, 3f, wallThickness + 1.2f), stone, "Linteau");
            Proto.BeginVisualOnly();
            Proto.Cube(gate.transform, new Vector3(0f, 8.4f, 0f),
                       new Vector3(gateGap - 0.6f, 1.2f, wallThickness + 1.6f), darkStone, "Arc");
            // la herse, remontee
            for (int i = 0; i < 6; i++)
            {
                Proto.Cube(gate.transform, new Vector3(-gateGap * 0.4f + i * (gateGap * 0.16f), 9.6f, 0f),
                           new Vector3(0.28f, 2.4f, 0.28f), new Color(0.32f, 0.33f, 0.36f), "Herse");
            }
            for (int i = 0; i < 9; i++)
            {
                Proto.Cube(gate.transform, new Vector3(-(gateGap * 0.5f + 5.5f) + i * 1.45f, 12.6f, 0f),
                           new Vector3(0.85f, 1.3f, wallThickness + 1.4f), stone, "Creneau");
            }
            Proto.EndVisualOnly();

            // le pont, poursuivant le chemin jusqu'a la porte
            GameObject bridge = Proto.Cube(castle.transform, new Vector3(0f, 0.16f, -half - 7f),
                                           new Vector3(gateGap - 1f, 0.3f, 15f),
                                           Palette.Shade(Palette.Trunk, 1.05f), "Pont");
            Proto.StripCollider(bridge);

            BuildTorch(gate.transform, new Vector3(-(gateGap * 0.5f + 1.4f), 0f, -2.6f));
            BuildTorch(gate.transform, new Vector3(gateGap * 0.5f + 1.4f, 0f, -2.6f));

            // ---------- le donjon
            GameObject keep = new GameObject("Donjon");
            keep.transform.SetParent(castle.transform, false);
            keep.transform.localPosition = new Vector3(0f, 0f, 16f);

            Proto.Cube(keep.transform, new Vector3(0f, 14f, 0f), new Vector3(21f, 28f, 21f), stone, "Corps");
            Proto.BeginVisualOnly();
            Proto.Cube(keep.transform, new Vector3(0f, 28.6f, 0f), new Vector3(23.5f, 1.4f, 23.5f), darkStone, "Corniche");
            for (int i = 0; i < 24; i++)
            {
                float t = i / 6f;
                int edge = i / 6;
                float along = (t - edge) * 2f - 1f;
                float ex = edge == 0 ? along * 10.5f : (edge == 1 ? 10.5f : (edge == 2 ? -along * 10.5f : -10.5f));
                float ez = edge == 0 ? -10.5f : (edge == 1 ? along * 10.5f : (edge == 2 ? 10.5f : -along * 10.5f));
                Proto.Cube(keep.transform, new Vector3(ex, 30.2f, ez), new Vector3(2.2f, 2.2f, 2.2f), stone, "Creneau");
            }
            // quatre echauguettes aux angles du donjon
            for (int i = 0; i < 4; i++)
            {
                float ex = (i % 2 == 0) ? -10.5f : 10.5f;
                float ez = (i < 2) ? -10.5f : 10.5f;
                Proto.Cylinder(keep.transform, new Vector3(ex, 30f, ez), new Vector3(4.4f, 3f, 4.4f), darkStone, "Echauguette");
                GameObject cone = Proto.Cube(keep.transform, new Vector3(ex, 35.4f, ez),
                                             new Vector3(3.6f, 3.6f, 3.6f), Palette.Roof, "Toiture");
                cone.transform.localRotation = Quaternion.Euler(0f, 45f, 35f);
            }
            // fenetres
            for (int i = 0; i < 3; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Proto.Cube(keep.transform, new Vector3(side * 5.5f, 9f + i * 7f, -10.6f),
                               new Vector3(1.3f, 3f, 0.4f), new Color(0.08f, 0.07f, 0.09f), "Fenetre");
                }
            }
            Proto.EndVisualOnly();

            Proto.Banner(keep.transform, new Vector3(0f, 30f, -8f), banner, 9f, "GrandEtendard");

            // ---------- la cour : puits, charrette, tas de bois
            Proto.BeginVisualOnly();
            Proto.Cylinder(castle.transform, new Vector3(-24f, 0.9f, -6f), new Vector3(3.4f, 0.9f, 3.4f),
                           Palette.Shade(Palette.Rock1, 0.9f), "Puits");
            Proto.Cube(castle.transform, new Vector3(24f, 0.8f, -4f), new Vector3(4.4f, 1.6f, 2.6f),
                       Palette.Shade(Palette.Trunk, 0.9f), "Charrette");
            for (int i = 0; i < 5; i++)
            {
                GameObject log = Proto.Cylinder(castle.transform, new Vector3(26f + (i % 2) * 0.9f, 0.5f + i * 0.85f, 4f),
                                                new Vector3(0.85f, 2.4f, 0.85f),
                                                Palette.Shade(Palette.Trunk, 0.8f), "Rondin");
                log.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Proto.EndVisualOnly();

            // torches le long des courtines
            for (int i = 0; i < 6; i++)
            {
                float a = (360f / 6f) * i * Mathf.Deg2Rad;
                BuildTorch(castle.transform, new Vector3(Mathf.Sin(a) * 30f, 0f, Mathf.Cos(a) * 30f));
            }
        }

        /// <summary>Une courtine : le mur plein, son chemin de ronde et ses creneaux.</summary>
        void BuildWall(Transform parent, Vector3 centre, float length, float height, float thickness,
                       bool alongX, Color stone)
        {
            Vector3 size = alongX ? new Vector3(length, height, thickness)
                                  : new Vector3(thickness, height, length);
            Proto.Cube(parent, centre + new Vector3(0f, height * 0.5f, 0f), size, stone, "Courtine");

            Proto.BeginVisualOnly();
            Vector3 walkSize = alongX ? new Vector3(length, 0.5f, thickness + 1.6f)
                                      : new Vector3(thickness + 1.6f, 0.5f, length);
            Proto.Cube(parent, centre + new Vector3(0f, height + 0.25f, 0f), walkSize,
                       Palette.Shade(stone, 0.88f), "CheminDeRonde");

            int merlons = Mathf.Max(3, Mathf.RoundToInt(length / 4.2f));
            for (int i = 0; i < merlons; i++)
            {
                float t = (i + 0.5f) / merlons - 0.5f;
                Vector3 offset = alongX ? new Vector3(t * length, 0f, 0f) : new Vector3(0f, 0f, t * length);
                Proto.Cube(parent, centre + offset + new Vector3(0f, height + 1.4f, 0f),
                           new Vector3(2f, 2f, 2f), stone, "Creneau");
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Une tour ronde, crenelee, coiffee, avec son etendard.</summary>
        void BuildTower(Transform parent, Vector3 at, float radius, float height, Color stone,
                        Color banner, bool withBanner)
        {
            Proto.Cylinder(parent, at + new Vector3(0f, height * 0.5f, 0f),
                           new Vector3(radius * 2f, height * 0.5f, radius * 2f), stone, "Tour");

            Proto.BeginVisualOnly();
            Proto.Cylinder(parent, at + new Vector3(0f, height + 0.3f, 0f),
                           new Vector3(radius * 2.4f, 0.4f, radius * 2.4f), Palette.Shade(stone, 0.85f), "Corniche");

            int merlons = 8;
            for (int i = 0; i < merlons; i++)
            {
                float a = (360f / merlons) * i * Mathf.Deg2Rad;
                GameObject merlon = Proto.Cube(parent,
                    at + new Vector3(Mathf.Sin(a) * radius * 1.05f, height + 1.6f, Mathf.Cos(a) * radius * 1.05f),
                    new Vector3(1.7f, 2.2f, 1.7f), stone, "Creneau");
                merlon.transform.localRotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * a, 0f);
            }

            GameObject roof = Proto.Cube(parent, at + new Vector3(0f, height + 4.6f, 0f),
                                         new Vector3(radius * 1.9f, radius * 1.9f, radius * 1.9f),
                                         Palette.Roof, "Toiture");
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 38f);
            Proto.EndVisualOnly();

            if (withBanner) Proto.Banner(parent, at + new Vector3(0f, height + 2f, 0f), banner, 5.5f, "Etendard");
        }

        // ================================================================ lacs

        void BuildLakes()
        {
            if (Ground.LakeCount <= 0) return;

            GameObject root = new GameObject("Lacs");
            root.transform.SetParent(worldRoot, false);

            for (int i = 0; i < Ground.LakeCount; i++)
            {
                Vector2 centre = Ground.LakeCenter(i);
                float radius = Ground.LakeRadius(i);
                float level = Ground.WaterLevel(i);
                Water.Create(root.transform, new Vector3(centre.x, level, centre.y),
                             radius + 8f, "Lac_" + (i + 1));
            }
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
                if (candidate.magnitude < config.marketRadius + 70f) continue;
                if (Scenery.DistanceToRoad(candidate.x, candidate.z) < 14f) continue;
                if (Ground.Slope(candidate.x, candidate.z) > 0.45f) continue;
                if (Ground.Sample(candidate.x, candidate.z) < -1.5f) continue;

                bool clear = true;
                for (int i = 0; i < occupied.Count; i++)
                {
                    float minDistance = (i <= config.fiefCount) ? 115f : 9f;
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
            Vector3 spawn = Ground.Place(Game.HomeFiefPosition + new Vector3(0f, 0f, -62f), 1.2f);

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

            // Et le corps qu'on voit par ses propres yeux : un objet SEPARE, construit
            // depuis l'oeil. Les deux ne sont jamais visibles en meme temps.
            Color cloth = Color.Lerp(tunic, new Color(0.47f, 0.48f, 0.43f), 0.88f);
            Color band = Color.Lerp(tunic, new Color(0.62f, 0.58f, 0.50f), 0.70f);
            Color patch = Color.Lerp(tunic, new Color(0.44f, 0.38f, 0.31f), 0.62f);
            FirstPersonBody body = FirstPersonBody.Build(go.transform, cloth, band, patch);
            body.SetVisible(false);
            Game.Body = body;

            // La camera. AudioListener dessus : c'est l'oreille du jeu.
            GameObject camGo = new GameObject("CAMERA");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null
                ? CameraClearFlags.Skybox
                : CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.10f;
            cam.farClipPlane = 3000f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";

            viewCamera = cam;

            OrbitCamera orbit = camGo.AddComponent<OrbitCamera>();
            orbit.target = go.transform;
            orbit.yaw = go.transform.eulerAngles.y;
            orbit.view = cam;
            orbit.rig = rig;
            orbit.body = body;
            orbit.baseFieldOfView = cam.fieldOfView;
            orbitCamera = orbit;

            PlayerController player = go.AddComponent<PlayerController>();
            player.cameraTransform = camGo.transform;
            player.rig = rig;
            player.orbitCamera = orbit;
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
