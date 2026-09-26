using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE point d'entree du jeu -- et de chaque MANCHE.
    ///
    /// La scene Main.unity ne contient qu'UN seul objet : celui qui porte ce script.
    /// Tout le reste -- l'ile, la citadelle, la tour, les joueurs, la camera -- est
    /// fabrique ici au lancement. Une scene construite par code se relit dans Git ;
    /// une scene .unity ne se relit pas.
    ///
    /// CHAQUE MANCHE RECHARGE LA SCENE (voir Menus) : ce script refait tout. Ce qui
    /// ne change pas d'une manche a l'autre -- l'ile, les ilots, la citadelle -- est
    /// tire de la graine du MONDE (config.worldSeed). Ce qui change -- l'ilot du
    /// Monument, les dons des sanctuaires, l'ordre sur la ligne de depart -- est tire
    /// de la graine de la MANCHE (Match.RoundSeed).
    ///
    /// L'ordre compte : l'ile avant ce qui se pose dessus ; la brume apres la camera.
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
            else if (!config.keepInspectorValues) GameConfig.RestoreDefaults(config);

            // A l'ecran-titre, pas de match : on en prepare un "d'apercu" (toi et trois
            // bots) pour que la foret vive derriere le menu. Le salon le remplacera.
            if (!Match.Active) Match.Begin(5, 5, Mathf.RoundToInt(config.seasonMinutes));
            if (!Match.Launched) Stats.Reset();

            Game.Reset();
            Toasts.Clear();
            Smoke.Clear();
            Game.Config = config;
            Game.Season = new Season(config);

            System.Diagnostics.Stopwatch chrono = System.Diagnostics.Stopwatch.StartNew();
            rng = new System.Random(config.worldSeed);
            worldRoot = new GameObject("=== L'ÎLE ===").transform;
            int round = Match.RoundSeed;

            try
            {
                Ground.Prepare(config);
                Ground.Build(worldRoot, config);
                Castle.Build(worldRoot, config);

                // Le Monument se pose sur un des ilots flottants -- un autre a chaque manche.
                Monument.Choose(round);
                Monument.Build(worldRoot, round);

                // La Couronne, au sommet de la tour.
                Crown.Build(worldRoot, Tower.CrownSpot);

                // Les sanctuaires de la manche (un don chacun, touche V).
                Shrine.Scatter(worldRoot, round);

                // La ligne de depart : une petite zone par joueur, toutes a la meme
                // distance du pied de la rampe.
                Spawns.Place(Match.Slots.Count, round);
                Spawns.Build(worldRoot);

                BuildInhabitants();
            }
            catch (System.Exception error)
            {
                // Une panne pendant la construction laissait un monde a moitie fait,
                // sans le moindre message. Elle s'affiche desormais en rouge a l'ecran.
                Game.BuildError = error.GetType().Name + " dans " + error.StackTrace;
                Debug.LogError("[FIEF] Construction interrompue : " + error);
            }

            PlayerController player = BuildPlayer();
            BuildRivals();

            // La brume a besoin de la camera (pour caler le plan lointain) et du
            // joueur (pour lui accrocher la lanterne) : elle vient donc en dernier.
            Atmosphere.Apply(config, viewCamera, player != null ? player.transform : null);

            try { Ambiance.Build(worldRoot, player != null ? player.transform : null, config); }
            catch (System.Exception error) { Debug.LogWarning("[FIEF] Ambiance ignorée : " + error.Message); }

            BuildHud(player);

            try { MusicDirector.Build(); }
            catch (System.Exception error) { Debug.LogWarning("[FIEF] Musique ignorée : " + error.Message); }

            Game.BuildMilliseconds = chrono.ElapsedMilliseconds;
            Debug.Log("[FIEF] " + Game.Version + " -- manche " + Match.RoundNumber + " construite en " + chrono.ElapsedMilliseconds + " ms : "
                      + Eye.All.Count + " Yeux, " + Shrine.All.Count + " sanctuaires, "
                      + Game.Seekers.Count + " joueurs.");
        }

        void Update()
        {
            // Time.deltaTime, pas le temps reel : la pause arrete l'horloge de la manche.
            if (Game.Season != null) Game.Season.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            Game.Reset();
        }

        // ================================================================ habitants

        /// <summary>
        /// LES YEUX (les sentinelles de la citadelle, voir Eye), les feux-follets et ce
        /// qu'on entend. Plus de PNJ humains (27/09) : rien qui parle, rien qui marche.
        /// </summary>
        void BuildInhabitants()
        {
            GameObject folk = new GameObject("HABITANTS");
            folk.transform.SetParent(worldRoot, false);
            Eye.PlaceAll(folk.transform);
            Ballista.PlaceAll(folk.transform);
        }

        /// <summary>
        /// Les autres joueurs : des bots en Phase 1. Chacun part de son point de
        /// depart, a la lisiere, tourne vers le chateau.
        /// </summary>
        void BuildRivals()
        {
            GameObject root = new GameObject("JOUEURS");
            root.transform.SetParent(worldRoot, false);
            for (int i = 0; i < Match.Slots.Count; i++)
            {
                PlayerSlot slot = Match.Slots[i];
                if (slot.IsLocal) continue;
                Vector3 spawn = Spawns.Of(slot.Index, Tower.Foot + Vector3.back * Spawns.Distance) + Vector3.up * 0.1f;
                Rival r = Rival.Build(root.transform, slot, spawn, config.worldSeed * 41 + Match.RoundSeed + i);
                r.transform.rotation = Quaternion.Euler(0f, Spawns.YawOf(slot.Index), 0f);
            }
            // Game.Seekers dans l'ordre des places : toi d'abord (place 0), puis les autres.
            Game.Seekers.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        // ================================================================ joueur

        PlayerController BuildPlayer()
        {
            PlayerSlot mine = Match.Local;
            Seeker me = new Seeker(mine);
            Game.Me = me;
            Game.Seekers.Add(me);

            // On apparait SUR SA ZONE, sur la ligne de depart, face au pied de la rampe.
            Vector3 spawn = Spawns.Of(mine.Index, Tower.Foot + Vector3.back * Spawns.Distance) + Vector3.up * 1.2f;
            GameObject go = new GameObject("JOUEUR");
            go.transform.position = spawn;
            float yaw = Spawns.YawOf(mine.Index);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            CharacterController controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 52f;
            controller.stepOffset = 0.42f;
            controller.skinWidth = 0.03f;

            // Le personnage : squelette articule, anime par le code (voir CharacterRig.cs).
            // En premiere personne on ne le voit pas -- seulement son ombre. Sa laine
            // prend la couleur de sa place (l'or, pour toi).
            Color wool = Color.Lerp(new Color(0.42f, 0.41f, 0.37f), mine.Colour, 0.25f);
            CharacterRig rig = CharacterRig.Build(go.transform, wool, Palette.Shade(wool, 0.62f));
            Game.Rig = rig;

            // La camera. AudioListener dessus : c'est l'oreille du jeu.
            GameObject camGo = new GameObject("CAMÉRA");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            // 78 degres : un champ trop etroit en premiere personne donne la nausee.
            Settings.Load();
            cam.fieldOfView = Settings.Fov;
            cam.nearClipPlane = 0.10f;
            cam.farClipPlane = 3000f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            viewCamera = cam;

            OrbitCamera orbit = camGo.AddComponent<OrbitCamera>();
            orbit.target = go.transform;
            orbit.yaw = yaw;
            orbit.view = cam;
            orbit.rig = rig;
            orbit.baseFieldOfView = cam.fieldOfView;
            orbitCamera = orbit;

            PlayerController player = go.AddComponent<PlayerController>();
            player.cameraTransform = camGo.transform;
            player.rig = rig;
            player.orbitCamera = orbit;
            go.AddComponent<PlayerInteractor>();
            // La poussee, les capacites, le don, grimper (voir AbilityUser).
            go.AddComponent<AbilityUser>();

            Game.Player = player;
            Game.PlayerTransform = go.transform;
            me.Body = go.transform;

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

            // Le titre, le salon, l'intro, la pause, la fin de manche, le choix, le podium.
            Menus menus = go.AddComponent<Menus>();
            hud.menus = menus;
            Game.Menus = menus;
            // Les reglages du joueur (sensibilite, volume, champ de vision) : apres la
            // camera et la config, qu'ils modifient.
            Settings.Apply();
        }
    }
}
