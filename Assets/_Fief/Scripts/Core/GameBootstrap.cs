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
            Game.Market = new Market(config);
            Game.Fief = new FiefState();

            System.Diagnostics.Stopwatch chrono = System.Diagnostics.Stopwatch.StartNew();

            rng = new System.Random(config.worldSeed);
            worldRoot = new GameObject("=== SYLVE ===").transform;

            try
            {
                Ground.Prepare(config);
                Ground.Build(worldRoot, config);
                Forest.Plant(worldRoot, config, rng);
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
                      + Forest.TreeCount + " arbres, " + Forest.PlantCount + " touffes et blocs.");

            Toasts.Show("La brume se referme a quelques pas.", Palette.Gold);
            Toasts.Show("F1 pour les commandes.", UiStyle.Ink);
        }

        void Update()
        {
            if (Game.Market != null) Game.Market.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            Game.Reset();
        }

        // ================================================================ joueur

        /// <summary>
        /// Cherche l'endroit le plus degage pres du centre. On teste une spirale de
        /// points et on garde celui ou le couvert est le plus mince : c'est plus sur
        /// que de coder une clairiere en dur, parce que ca suit la foret si on change
        /// sa graine ou sa densite.
        /// </summary>
        Vector3 FindClearing()
        {
            float bestScore = 99f;
            Vector2 best = Vector2.zero;

            for (int i = 0; i < 220; i++)
            {
                float a = i * 2.39996f;                 // angle d'or : repartition reguliere
                float r = 9f * Mathf.Sqrt(i);
                float x = Mathf.Cos(a) * r;
                float z = Mathf.Sin(a) * r;

                // Un couvert mince ET un sol plat : on ne veut pas naitre sur un talus.
                float score = Forest.Canopy(x, z) + Ground.Slope(x, z) * 0.02f;
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
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

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
