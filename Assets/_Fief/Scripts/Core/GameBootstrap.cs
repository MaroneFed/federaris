using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE point d'entree du jeu.
    ///
    /// La scene Main.unity ne contient qu'UN seul objet : celui qui porte ce script.
    /// Tout le reste -- relief, foret, lumiere, joueur, camera -- est fabrique ici au
    /// lancement. Une scene construite par code se relit dans Git ; une scene .unity
    /// ne se relit pas, et c'est la source des conflits quand on est deux dessus.
    ///
    /// CE QUE CE MONDE EST DEVENU. On batissait ici un marche, six fiefs, un chateau,
    /// des lacs, des routes, des PNJ et des coffres, sur 484 hectares degages. Martin
    /// a tranche : il ne garde que le personnage et sa vue. Tout le reste est reparti
    /// de zero autour d'une seule idee -- une sylve dense ou l'on ne voit pas a
    /// quarante metres.
    ///
    /// L'ordre compte : le relief doit exister avant qu'on plante quoi que ce soit
    /// dessus, et la brume doit connaitre la camera pour caler sa portee.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        GameConfig config;
        Transform worldRoot;
        OrbitCamera orbitCamera;
        Camera viewCamera;
        System.Random rng;

        void Awake()
        {
            config = GetComponent<GameConfig>();
            if (config == null) config = gameObject.AddComponent<GameConfig>();

            Game.Reset();
            Toasts.Clear();

            Game.Config = config;
            Game.Inventory = new Inventory();
            Game.Inventory.MaxWeight = config.maxWeight;
            Game.Wallet = new Wallet(config.startingGold);
            Game.Season = new Season(config);
            Game.Hoard = new Hoard();
            Game.Hoard.MaxCaches = Mathf.Max(0, config.maxCaches);
            Game.Hoard.CacheCapacity = Mathf.Max(1f, config.cacheCapacity);
            Game.Hoard.CampCapacity = Mathf.Max(1f, config.campCapacity);

            System.Diagnostics.Stopwatch chrono = System.Diagnostics.Stopwatch.StartNew();

            rng = new System.Random(config.worldSeed);
            worldRoot = new GameObject("=== SYLVE ===").transform;

            try
            {
                Ground.Prepare(config);
                Ground.Build(worldRoot, config);
                Castle.Build(worldRoot, config);

                // Les creux d'abord (ils ne dependent que du relief), pour que la foret
                // les laisse degages ; les pierres-lune y sont posees ensuite.
                Gathering.Reset();
                Gathering.FindHollows(config);
                Forest.Plant(worldRoot, config, rng);
                Gathering.PlaceMoonstones(worldRoot, config, rng);

                // Le mage existe des le debut, invisible et muet : c'est l'agenda de
                // la Saison qui le fait apparaitre. Il a besoin des colliders de la
                // foret pour choisir une place libre, il vient donc apres elle.
                Game.Mage = Mage.Build(worldRoot, config);
            }
            catch (System.Exception error)
            {
                // Une panne pendant la construction laissait un monde a moitie fait,
                // sans le moindre message. Elle s'affiche desormais en rouge a l'ecran.
                Game.BuildError = error.GetType().Name + " dans " + error.StackTrace;
                Debug.LogError("[FIEF] Construction interrompue : " + error);
            }

            PlayerController player = BuildPlayer();

            // La brume a besoin de la camera (pour caler le plan lointain) et du
            // joueur (pour lui accrocher la lanterne) : elle vient donc en dernier.
            Atmosphere.Apply(config, viewCamera, player != null ? player.transform : null);

            BuildHud(player);

            Game.BuildMilliseconds = chrono.ElapsedMilliseconds;
            Debug.Log("[FIEF] Sylve construite en " + chrono.ElapsedMilliseconds + " ms : "
                      + Forest.TreeCount + " arbres, " + Forest.PlantCount + " touffes et blocs, "
                      + Gathering.FagotCount + " fagots, " + Gathering.LogSourceCount + " troncs a bois mort, "
                      + Gathering.MoonstoneCount + " pierres-lune dans " + Gathering.HollowCount + " creux.");

            // Les messages d'accueil sont affiches par Menus, a l'entree en jeu : ici
            // ils s'eteignaient pendant l'ecran-titre sans que personne les voie.
        }

        void Update()
        {
            // Time.deltaTime, pas le temps reel : la pause arrete l'horloge de la Saison.
            if (Game.Season != null) Game.Season.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            Game.Reset();
        }

        // ================================================================ joueur

        /// <summary>
        /// Ou l'on apparait : A LA LISIERE, loin du chateau. On arrive de l'exterieur,
        /// on ne sait pas encore ou il est ; le trouver est le premier voyage.
        ///
        /// On cherche sur un anneau entre 220 et 290 m du centre l'endroit le plus
        /// degage et le plus plat -- une clairiere, pas un fourre : sinon la premiere
        /// image du jeu est un tronc a cinquante centimetres du nez.
        /// </summary>
        Vector3 FindClearing()
        {
            float bestScore = 99f;
            Vector2 best = new Vector2(0f, -250f);

            for (int i = 0; i < 240; i++)
            {
                float a = i * 2.39996f;                 // angle d'or : repartition reguliere
                float r = Mathf.Lerp(220f, 290f, (i % 12) / 11f);
                float x = Mathf.Cos(a) * r;
                float z = Mathf.Sin(a) * r;

                float score = Forest.Canopy(x, z) + Ground.Slope(x, z) * 0.6f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new Vector2(x, z);
                }
            }
            return Ground.Place(best.x, best.y, 1.2f);
        }

        PlayerController BuildPlayer()
        {
            // On apparait en bord de fief, tourne vers lui : la premiere image du jeu
            // montre ta banniere et tes 6 emplacements de construction.
            // On apparait dans une CLAIRIERE, pas au milieu d'un fourre : sinon la
            // premiere image du jeu est un tronc a cinquante centimetres du nez.
            Vector3 spawn = FindClearing();

            GameObject go = new GameObject("JOUEUR");
            go.transform.position = spawn;

            // On regarde vers l'interieur de la foret, a peu pres vers le chateau --
            // a quarante degres pres : on sait d'ou l'on vient, pas exactement ou aller.
            Vector3 inward = -new Vector3(spawn.x, 0f, spawn.z);
            float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg + ((float)rng.NextDouble() - 0.5f) * 80f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            CharacterController controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 52f;
            controller.stepOffset = 0.42f;
            controller.skinWidth = 0.03f;

            // Le personnage : squelette articule, anime par le code (voir CharacterRig.cs).
            //
            // Sa laine ne prend plus la couleur d'un blason : les six fiefs n'existent
            // plus, et de toute facon on ne le voit qu'a l'ecran-titre et dans sa propre
            // ombre. Une laine ecrue sale, qui est ce qu'elle aurait du etre des le debut.
            Color wool = new Color(0.42f, 0.41f, 0.37f);
            CharacterRig rig = CharacterRig.Build(go.transform, wool, Palette.Shade(wool, 0.62f));
            Game.Rig = rig;


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
            orbit.baseFieldOfView = cam.fieldOfView;
            orbitCamera = orbit;

            PlayerController player = go.AddComponent<PlayerController>();
            player.cameraTransform = camGo.transform;
            player.rig = rig;
            player.orbitCamera = orbit;
            go.AddComponent<PlayerInteractor>();
            // C plante le camp, G creuse une cache. Apres PlayerController : son Awake
            // va chercher ce composant pour savoir quand les entrees sont figees.
            go.AddComponent<CampActions>();

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

            // L'ecran-titre, la pause, et l'ecran de fin de Saison.
            Menus menus = go.AddComponent<Menus>();
            hud.menus = menus;
            Game.Menus = menus;
        }
    }
}
