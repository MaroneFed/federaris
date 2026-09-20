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
            BuildLakes();
            Scenery.BuildLandmarks(worldRoot, config);
            BuildResourceNodes();
            Scenery.PlantForests(worldRoot, config, rng, occupied, config.forestCount);
            Scenery.Scatter(worldRoot, config, rng, occupied, config.decorCount);
            Scenery.BuildClouds(worldRoot, config, rng);

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

                Proto.Pad(root.transform, Vector3.zero, 40f, Palette.Dirt, "Terrasse", 0.05f);
                Proto.Pad(root.transform, Vector3.zero, 26f, Palette.Shade(Palette.Dirt, 1.1f), "Cour", 0.07f);

                if (isPlayer)
                {
                    Game.HomeFiefPosition = center;
                    Game.Fief.center = center;
                    BuildKeep(root.transform, Palette.Banner(i));
                    BuildPlots(root.transform);
                }
            }
        }

        /// <summary>
        /// Le coeur du fief : une porte, deux tours, des bannieres, des torches.
        /// Rien de tout cela n'est jouable en Phase 1 : c'est ce qui fait que l'endroit
        /// ressemble a CHEZ TOI plutot qu'a six dalles posees sur l'herbe.
        /// </summary>
        void BuildKeep(Transform parent, Color banner)
        {
            Color stone = Palette.Structure;

            // --- porterie, face au marche (le marche est vers -Z depuis le fief 0)
            GameObject gate = new GameObject("Porterie");
            gate.transform.SetParent(parent, false);
            gate.transform.localPosition = new Vector3(0f, 0f, -22f);

            for (int side = -1; side <= 1; side += 2)
            {
                Proto.Cylinder(gate.transform, new Vector3(side * 6.0f, 3.6f, 0f),
                               new Vector3(3.2f, 3.6f, 3.2f), stone, "Tour");
                GameObject crown = Proto.Cylinder(gate.transform, new Vector3(side * 6.0f, 7.5f, 0f),
                                                  new Vector3(3.6f, 0.4f, 3.6f), Palette.Shade(stone, 0.85f), "Couronne");
                Proto.StripCollider(crown);
                for (int m = 0; m < 6; m++)
                {
                    float a = (360f / 6f) * m * Mathf.Deg2Rad;
                    GameObject merlon = Proto.Cube(gate.transform,
                        new Vector3(side * 6.0f + Mathf.Sin(a) * 1.55f, 8.1f, Mathf.Cos(a) * 1.55f),
                        new Vector3(0.6f, 0.9f, 0.6f), stone, "Creneau");
                    merlon.transform.localRotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * a, 0f);
                    Proto.StripCollider(merlon);
                }
                Proto.Banner(gate.transform, new Vector3(side * 6.0f, 8.2f, 0f), banner, 3.6f, "Etendard");
            }

            GameObject lintel = Proto.Cube(gate.transform, new Vector3(0f, 6.2f, 0f),
                                           new Vector3(13.6f, 1.6f, 2.6f), stone, "Linteau");
            Proto.StripCollider(lintel);
            GameObject arch = Proto.Cube(gate.transform, new Vector3(0f, 5.1f, 0f),
                                         new Vector3(9.4f, 0.7f, 2.8f), Palette.Shade(stone, 0.78f), "Arc");
            Proto.StripCollider(arch);

            // --- murets de part et d'autre, pour fermer la cour sans l'enfermer
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject wall = Proto.Cube(gate.transform,
                        new Vector3(side * (9.8f + i * 5.4f), 1.6f, 1.6f + i * 3.4f),
                        new Vector3(5.2f, 3.2f, 1.1f), Palette.Shade(stone, 0.94f), "Muret");
                    wall.transform.localRotation = Quaternion.Euler(0f, side * (12f + i * 14f), 0f);
                }
            }

            BuildTorch(gate.transform, new Vector3(-9.2f, 0f, 1.2f));
            BuildTorch(gate.transform, new Vector3(9.2f, 0f, 1.2f));

            // --- panneau de bienvenue
            GameObject sign = new GameObject("Poteau");
            sign.transform.SetParent(parent, false);
            sign.transform.localPosition = new Vector3(-9f, 0f, -14f);
            GameObject mast = Proto.Cylinder(sign.transform, new Vector3(0f, 1.4f, 0f),
                                             new Vector3(0.2f, 1.4f, 0.2f), Palette.Trunk, "Mat");
            Proto.StripCollider(mast);
            GameObject plank = Proto.Cube(sign.transform, new Vector3(0.5f, 2.5f, 0f),
                                          new Vector3(2.2f, 0.7f, 0.12f), Palette.Shade(Palette.Trunk, 1.35f), "Planche");
            plank.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            Proto.StripCollider(plank);
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
                if (candidate.magnitude < config.marketRadius + 26f) continue;
                if (Scenery.DistanceToRoad(candidate.x, candidate.z) < 6f) continue;
                if (Ground.Slope(candidate.x, candidate.z) > 0.45f) continue;
                if (Ground.Height(candidate.x, candidate.z) < -1.5f) continue;

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
            orbit.view = cam;
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
