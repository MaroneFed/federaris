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
            Victories.Reset();
            Objectives.Reset();
            Secrets.Reset();

            Game.Config = config;
            Game.Inventory = new Inventory();
            Game.Inventory.MaxWeight = config.maxWeight;
            Game.Wallet = new Wallet(config.startingGold);
            Game.Season = new Season(config);
            Game.Hoard = new Hoard();
            Game.Hoard.MaxCaches = Mathf.Max(0, config.maxCaches);
            Game.Hoard.CacheCapacity = Mathf.Max(1f, config.cacheCapacity);
            Game.Hoard.CampCapacity = Mathf.Max(1f, config.campCapacity);
            // La garde du chateau : six hommes, six soldes, six loyautes. Bertrand
            // n'a rien touche depuis cinq mois ; Jehan, lui, croit encore au roi.
            Game.Garrison = new Garrison();
            Game.Garrison.Guards.Add(new GuardInfo("Bertrand", 4, 5, 0.15f));
            Game.Garrison.Guards.Add(new GuardInfo("Aubin", 5, 1, 0.7f));
            Game.Garrison.Guards.Add(new GuardInfo("Lambert", 3, 3, 0.35f));
            Game.Garrison.Guards.Add(new GuardInfo("Jehan", 6, 0, 0.85f));
            Game.Garrison.Guards.Add(new GuardInfo("Thibaut", 4, 2, 0.5f));
            Game.Garrison.Guards.Add(new GuardInfo("Enguerrand", 3, 4, 0.25f));

            Game.Me = new Seeker("Toi", new Color(0.92f, 0.78f, 0.42f), true, Game.Inventory, Game.Wallet, Game.Hoard);
            Game.Seekers.Add(Game.Me);

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
                // Les lieux-dits choisissent leur place avant la foret, qui leur
                // laisse une clairiere ; ils se construisent apres elle.
                Landmarks.Find(config);
                Forest.Plant(worldRoot, config, rng);
                Gathering.PlaceMoonstones(worldRoot, config, rng);
                Landmarks.Build(worldRoot, config);
                // Ruines, fleurs-lune, corbeaux : apres la foret et les lieux-dits,
                // pour tomber dans les trous qu'ils laissent.
                Nature.Build(worldRoot, config);

                // Le mage existe des le debut, invisible et muet : c'est l'agenda de
                // la Saison qui le fait apparaitre. Il a besoin des colliders de la
                // foret pour choisir une place libre, il vient donc apres elle.
                Game.Mage = Mage.Build(worldRoot, config);

                // Les autres habitants de la sylve.
                BuildInhabitants();

                // Les steles, une par chercheur, tirees au hasard a chaque partie.
                // Apres les rivaux (il faut leurs Seeker) et apres la foret (il faut
                // ses troncs pour trouver une place libre).
                SteleSites.PlaceAll(worldRoot, Game.Seekers);

                // Ce qui veut ton mal : trois meutes de loups (apres les steles, pour
                // ne pas naitre au milieu d'elles).
                Beast.SpawnPacks(worldRoot, 3);
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

            // Le ciel qui tourne pendant la Saison (crepuscule, nuit) et l'orage.
            // Il part des valeurs qu'Atmosphere vient de poser : il vient donc apres.
            Sky.Build(config, viewCamera, player != null ? player.transform : null);

            // Ce qui flotte dans l'air : poussieres, brume rasante, lucioles des creux.
            // Rate, il n'y a pas de particules -- jamais de monde a moitie construit.
            try { Ambiance.Build(worldRoot, player != null ? player.transform : null, config); }
            catch (System.Exception error) { Debug.LogWarning("[FIEF] Ambiance ignoree : " + error.Message); }

            // Le tapis de la foret : feuilles mortes, brindilles, champignons, autour de toi.
            try { if (player != null) GroundCover.Build(worldRoot, player.transform, config); }
            catch (System.Exception error) { Debug.LogWarning("[FIEF] Tapis de foret ignore : " + error.Message); }

            BuildHud(player);

            // La musique : tes morceaux s'ils sont dans Resources/Music, sinon la sienne.
            Curse.Build();

            try { MusicDirector.Build(); }
            catch (System.Exception error) { Debug.LogWarning("[FIEF] Musique ignoree : " + error.Message); }

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

        // ================================================================ habitants

        /// <summary>
        /// Le Veilleur au chateau, l'Ermite dans sa tour, neuf feux-follets, et le
        /// cerf blanc. Chacun est optionnel : s'il manque, le jeu tourne quand meme.
        /// </summary>
        void BuildInhabitants()
        {
            GameObject folk = new GameObject("HABITANTS");
            folk.transform.SetParent(worldRoot, false);

            Veilleur.Build(folk.transform);

            // Les gardes et leurs rondes : deux a la grande porte, un devant chaque
            // reserve, un qui fait le tour de la cour.
            Vector3[][] routes =
            {
                new[] { new Vector3(-5f, 0f, -35f), new Vector3(-5f, 0f, -27f) },
                new[] { new Vector3(5f, 0f, -35f), new Vector3(5f, 0f, -27f) },
                new[] { new Vector3(-26f, 0f, -20f), new Vector3(-26f, 0f, -8f) },
                new[] { new Vector3(26f, 0f, -20f), new Vector3(26f, 0f, -8f) },
                new[] { new Vector3(-31f, 0f, 26f), new Vector3(-19f, 0f, 26f) },
                new[] { new Vector3(-13f, 0f, -6f), new Vector3(-13f, 0f, 6f), new Vector3(9f, 0f, 6f), new Vector3(9f, 0f, -6f) }
            };
            for (int i = 0; i < routes.Length && i < Game.Garrison.Guards.Count; i++)
            {
                for (int k = 0; k < routes[i].Length; k++) routes[i][k] = Ground.Place(routes[i][k], 0.05f);
                Guard.Build(folk.transform, Game.Garrison.Guards[i], routes[i]);
            }

            // L'or, pour les acheter.
            Purse.Scatter(worldRoot, config);
            if (Landmarks.Tour != null) Ermite.Build(Landmarks.Tour);
            Wisp.SpawnAll(folk.transform, config, 9);
            WhiteStag.Build(folk.transform, config);

            // Tes trois rivaux. Chacun son caractere : Mahaut pille, Oswin aime le
            // fer du chateau, Guerin reste dans ses creux et ne vole presque jamais.
            BuildRival(folk.transform, 0, "Mahaut la Rousse", new Color(0.86f, 0.36f, 0.26f), 0.7f, 0.25f, new[]
            {
                "Ne traine pas dans mes pattes.",
                "J'ai vu ta lanterne. Tout le monde l'a vue.",
                "Le mage m'aime bien. Il me le dit en chantant.",
                "Ta stele ? Je sais ou elle est. Peut-etre."
            });
            BuildRival(folk.transform, 1, "Oswin le Borgne", new Color(0.36f, 0.58f, 0.88f), 0.35f, 0.6f, new[]
            {
                "Le fer du chateau, c'est pour ceux qui osent.",
                "Un oeil me suffit pour te voir venir.",
                "Les gardes ? Ils me connaissent.",
                "Ta relique pese combien ? Pas assez."
            });
            BuildRival(folk.transform, 2, "Guerin des Marais", new Color(0.46f, 0.76f, 0.36f), 0.15f, 0.1f, new[]
            {
                "Chut. Tu entends ? Non ? Tant mieux.",
                "Je ne prends que ce que la foret donne.",
                "Les pierres-lune chantent, la nuit. Tu les as ecoutees ?",
                "Laisse ma stele tranquille, et je laisserai la tienne."
            });

            // Et ce qu'on entend : le vent, les betes, la cloche du chateau.
            Soundscape.Build(folk.transform);
        }

        /// <summary>Un rival, qui part de la lisiere, a un tiers de tour des autres.</summary>
        void BuildRival(Transform parent, int index, string name, Color colour, float aggression, float ironLove, string[] taunts)
        {
            float a = (index * 120f + 60f) * Mathf.Deg2Rad;
            Vector3 spawn = Ground.Place(Mathf.Cos(a) * 240f, Mathf.Sin(a) * 240f, 0.1f);
            Rival rival = Rival.Build(parent, name, colour, spawn, aggression, ironLove, taunts, config.worldSeed * 41 + index);
            // Les deux plus agressifs partent avec une epee.
            if (aggression >= 0.3f) rival.Arm();
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

                // Jamais au milieu d'un lieu-dit : on n'apparait pas dans une pierre.
                if (Landmarks.Near(x, z, 12f)) continue;

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
            //
            // Depuis le 25/09 : on nait A COTE DE SA STELE, et on la regarde. C'est la
            // premiere chose qu'on voit -- et la seule fois ou on la trouve sans la
            // chercher. Rien ne l'indiquera plus ensuite.
            Hoard mine = Game.Me != null ? Game.Me.Hoard : null;
            bool atStele = mine != null && mine.StelePlanted;
            Vector3 spawn = atStele
                ? SteleSites.SpawnBeside(mine.StelePosition, (float)rng.NextDouble() * Mathf.PI * 2f)
                : FindClearing();

            GameObject go = new GameObject("JOUEUR");
            go.transform.position = spawn;

            Vector3 look = atStele ? mine.StelePosition - spawn : -new Vector3(spawn.x, 0f, spawn.z);
            look.y = 0f;
            float yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
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
            // 1 / 2 : les outils ; clic : frapper ; F : grimper.
            go.AddComponent<ToolUser>();

            Game.Player = player;
            Game.PlayerTransform = go.transform;
            if (Game.Me != null) Game.Me.Body = go.transform;

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
