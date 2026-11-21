using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// FIEF, LA NUIT -- V0 "le frisson" (11/11). Le tout premier test du nouveau jeu :
    ///
    ///   un vestibule, un long couloir, une chapelle, dans le NOIR ; ta TORCHE ; UNE quete
    ///   (rallumer les quatre cierges de la chapelle, touche E) ; et LE PENITENT, une statue
    ///   qui n'avance QUE QUAND TU NE LA REGARDES PAS. Les quatre cierges allumes : l'aube.
    ///
    /// Tout est en BLOCS PROVISOIRES (des cubes) : le kit fait dans Blender les remplacera.
    /// Ce fichier ne touche a rien de l'ancien FIEF (La Couronne) : il a sa propre scene
    /// (menu FIEF > LA NUIT), son propre joueur, sa propre lumiere.
    ///
    /// Concept Unity : un MonoBehaviour pose sur un objet vide de la scene. Awake() est
    /// appele une fois au lancement : on y fabrique tout le niveau par le code.
    /// </summary>
    public class NightBootstrap : MonoBehaviour
    {
        static readonly Color Stone = new Color(0.40f, 0.39f, 0.38f);
        static readonly Color Night = new Color(0.011f, 0.015f, 0.028f);
        static readonly Color StoneDark = new Color(0.27f, 0.26f, 0.26f);
        static readonly Color Flame = new Color(1f, 0.68f, 0.38f);

        Transform world;
        NightPlayer player;
        Penitent penitent;
        readonly List<Candle> candles = new List<Candle>();
        float dawn;                 // 0 : la nuit ; monte vers 1 quand la quete est finie
        float caught;               // le noir qui retombe quand le Penitent t'a eu
        string message = "Survis.";
        // (14/11) LA SURVIE : la foi, la sacristie verrouillee, le silex, le Rodeur. Voir NightSurvival.cs.
        Prowler grabbed;
        NightDoor chapel;          // (21/11) la chapelle est fermee a cle : la cle est cachee dans le chateau
        char lastRoom = '.';
        // (21/11) Le boss de la crypte, et le brasier qu'on est en train de rallumer.
        Abbot abbot;
        bool bossFight, grabAbbot;
        GameObject lightingFire;        // (15/11) celui qui te tient : il y a maintenant plusieurs Rodeurs (Prowler.All)
        NightDoor sacristy;
        GameObject flint;
        bool hasFlint;
        float prayHold, prayedAt = -999f;
        // (14/11) LA PEUR : voir NightFear.cs. "fear" va de 0 a 1 ; "grab" : le monstre te tient, juste avant le noir.
        float fear, grab;
        bool grabProwler, grabFaithless;
        Vector3 FlintAt, AltarAt, start;      // (15/11) lus sur le plan du chateau (NightMap)
        float messageUntil;
        GUIStyle text;

        void Awake()
        {
            Settings.Load();
            Gfx.Prepare();
            Time.timeScale = 1f;
            try { Sfx.Init(gameObject); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Sons ignorés : " + e.Message); }

            // --- la nuit : aucune lumiere du ciel, un brouillard noir qui mange les distances
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Night;      // (12/11) un fond bleu tres faible : les ombres sont froides, les feux chauds
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogDensity = 0.055f;
            QualitySettings.pixelLightCount = 6;
            QualitySettings.shadowDistance = 40f;

            world = new GameObject("=== LE CHÂTEAU, LA NUIT ===").transform;
            // (15/11) LE GRAND CHATEAU : bati tout seul a partir du plan de NightMap (les blocs d'avant ne servent plus).
            Prowler.Forget();
            NightQuest.Clear();
            if (!NightMap.Build(world)) Debug.LogError("[FIEF] Kit absent (Resources/Modeles/Kit) : pas de château.");
            try { NightIntro.Build(world); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Dehors ignoré : " + e.Message); }
            Journal.Reset();
            NightUI.Reset();
            NightIntro.Playing = false;
            NightIntro.Finished = false;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.shadowDistance = 90f;
            BuildChapel();
            start = NightMap.Center('V') + new Vector3(0f, 0.1f, -1.6f);
            player = NightPlayer.Spawn(start);
            penitent = Penitent.Spawn(world, NightMap.Center('N'), player);
            NightDirector.Begin(player);
            try { NightKit.Ambience(player.transform); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Poussière ignorée : " + e.Message); }
            Faith.Reset();
            NightGames.Abort();
            BuildSacristy();
            BuildChapelDoor();
            NightQuest.Deal(start);
            try { NightAudio.Init(gameObject); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Sons de la nuit ignorés : " + e.Message); }
            message = "Survis.";
            messageUntil = 0f;
            // (21/11) Le menu, devant le chateau ; puis l'arrivee a cheval ; puis l'ouverture. (Pas pour les photos de Claude.)
            if (Application.isBatchMode) BeginInside();
            else { NightUI.OpenMenu(); try { NightIntro.MenuView(player); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Menu dehors ignoré : " + e.Message); } }
            Debug.Log("[FIEF] La Nuit, V0 : quatre cierges à rallumer. Le Pénitent s'éveille au premier.");
        }

        // ------------------------------------------------------------------ le niveau

        /// <summary>Un mur droit (le long de X ou de Z), du sol a la hauteur h.</summary>
        void Wall(float x0, float z0, float x1, float z1, float y0, float y1)
        {
            const float thick = 0.6f;
            Vector3 size = new Vector3(Mathf.Abs(x1 - x0) + thick, y1 - y0, Mathf.Abs(z1 - z0) + thick);
            Proto.Cube(world, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), size, Stone, "Mur");
        }

        /// <summary>Le sol et le plafond d'une piece.</summary>
        void Slab(float x0, float z0, float x1, float z1, float height)
        {
            Vector3 size = new Vector3(x1 - x0 + 0.6f, 0.5f, z1 - z0 + 0.6f);
            Vector3 mid = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            Proto.Cube(world, mid + Vector3.down * 0.25f, size, StoneDark, "Sol");
            Proto.Cube(world, mid + Vector3.up * (height + 0.25f), size, StoneDark, "Plafond");
        }

        void BuildRooms()
        {
            // LE VESTIBULE (8 x 8 m) : on y entre, on y revient quand le Penitent nous a eus.
            Slab(-4f, 0f, 4f, 8f, 4f);
            Wall(-4f, 0f, 4f, 0f, 0f, 4f);
            Wall(-4f, 0f, -4f, 8f, 0f, 4f);
            Wall(4f, 0f, 4f, 4f, 0f, 4f);             // (14/11) plus loin, de z 5 a 7 : la porte de la sacristie
            Wall(-4f, 8f, -2f, 8f, 0f, 4f);
            Wall(2f, 8f, 4f, 8f, 0f, 4f);

            // LA SACRISTIE (4 x 4 m), a droite du vestibule, derriere une porte verrouillee : le silex y est.
            Slab(4f, 4f, 8f, 8f, 4f);
            Wall(4f, 4f, 8f, 4f, 0f, 4f);
            Wall(4f, 8f, 8f, 8f, 0f, 4f);
            Wall(8f, 4f, 8f, 8f, 0f, 4f);
            Wall(4f, 4f, 4f, 4.7f, 0f, 4f);           // les deux montants de la porte
            Wall(4f, 7.3f, 4f, 8f, 0f, 4f);

            // LE COULOIR (4 m de large, 24 m de long), avec une niche a droite, a mi-chemin.
            Slab(-2f, 8f, 2f, 32f, 4f);
            Wall(-2f, 8f, -2f, 32f, 0f, 4f);
            Wall(2f, 8f, 2f, 20f, 0f, 4f);
            Wall(2f, 24f, 2f, 32f, 0f, 4f);
            // La niche du Penitent (12/11 : decalee de 2 m, de z 20 a 24, pour tomber sur la grille de 4 m du kit).
            Slab(2f, 20f, 6f, 24f, 4f);
            Wall(2f, 20f, 6f, 20f, 0f, 4f);
            Wall(2f, 24f, 6f, 24f, 0f, 4f);
            Wall(6f, 20f, 6f, 24f, 0f, 4f);

            // LA CHAPELLE (12 x 12 m, 6 m sous voute).
            Slab(-6f, 32f, 6f, 44f, 6f);
            Wall(-6f, 32f, -2f, 32f, 0f, 6f);
            Wall(2f, 32f, 6f, 32f, 0f, 6f);
            Wall(-2f, 32f, 2f, 32f, 4f, 6f);          // le linteau au-dessus de l'entree
            Wall(-6f, 32f, -6f, 44f, 0f, 6f);
            Wall(6f, 32f, 6f, 44f, 0f, 6f);
            Wall(-6f, 44f, 6f, 44f, 0f, 6f);

            // Des piliers dans le couloir : ce sont eux qui cachent ce qui te suit.
            // (12/11 : un tous les 4 m, sur la grille du kit ; cinq paires au lieu de quatre.)
            for (int i = 0; i < 5; i++)
                for (int s = -1; s <= 1; s += 2)
                    Proto.Cube(world, new Vector3(s * 1.55f, 2f, 12f + i * 4f), new Vector3(0.5f, 4f, 0.5f), StoneDark, "Pilier");
        }

        void BuildChapel()
        {
            // Les quatre cierges, aux quatre coins de la chapelle ; l'autel et le vitrail sont poses par NightMap.
            float x0, z0, x1, z1;
            NightMap.Bounds('K', out x0, out z0, out x1, out z1);
            AltarAt = new Vector3((x0 + x1) * 0.5f, 0f, z1 - 3.7f);
            Vector3[] spots = { new Vector3(x0 + 1.8f, 0f, z0 + 2.5f), new Vector3(x1 - 1.8f, 0f, z0 + 2.5f), new Vector3(x0 + 1.8f, 0f, z1 - 2.5f), new Vector3(x1 - 1.8f, 0f, z1 - 2.5f) };
            for (int i = 0; i < spots.Length; i++) candles.Add(Candle.Build(world, spots[i], Flame));
        }

        // ------------------------------------------------------------------ la partie

        /// <summary>On entre : l'ouverture (l'homme, le noir), puis la nuit commence.</summary>
        void BeginInside()
        {
            player.Teleport(start, 0f);
            try { NightCinema.Begin(player, world); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Ouverture ignorée : " + e.Message); NightCinema.Playing = false; }
            Say("Survis.", 20f);
            Journal.ChangedAt = Time.unscaledTime + 12f;
        }

        void Update()
        {
            if (player == null) return;
            // (21/11) Le menu, la pause, le carnet ; l'arrivee a cheval.
            bool playingNow = !NightUI.MenuOpen && !NightIntro.Playing && !NightCinema.Playing && grab <= 0f;
            if (NightUI.Tick(playingNow)) Application.Quit();
            if (NightUI.MenuOpen) NightIntro.MenuTick(Time.unscaledDeltaTime);
            if (NightUI.StartRequested) { NightUI.StartRequested = false; NightIntro.Begin(player); }
            NightIntro.Tick(Time.deltaTime);
            if (NightIntro.Finished) { NightIntro.Finished = false; BeginInside(); }
            NightMap.Tick(Time.deltaTime, player.transform.position);
            if (bossFight && abbot != null && abbot.Burnt) EndNight();
            float dt = Time.deltaTime;
            bool night = dawn <= 0f;
            bool hunted = Prowler.AnyHunting;
            NightCinema.Tick(dt);
            NightGames.Tick(dt);
            if (night && !NightCinema.Playing && !NightIntro.Playing && !NightUI.MenuOpen) Faith.Tick(dt, hunted);
            // La peur : le Penitent tout pres, le Rodeur qui te chasse, ou la foi qui s'eteint.
            float want = Mathf.Max(Penitent.Dread, Mathf.Max(hunted ? 0.85f : 0f, Mathf.Clamp01((0.3f - Faith.Value) / 0.3f)));
            fear = Mathf.MoveTowards(fear, night ? want : 0f, dt * 0.8f);
            NightAudio.Tick(dt, fear, hunted, player.transform.position, night);
            NightDirector.Tick(dt, player, fear, night);

            // --- (21/11) fouiller : tenir E pres d'un coffre, d'un tombeau, d'une bibliotheque
            NightQuest.Find found;
            Vector3 where;
            bool canAct = night && !NightGames.Busy && caught <= 0f && grab <= 0f;
            if (NightQuest.Tick(dt, player.transform.position, canAct && FiefInput.InteractHeld, out found, out where)) OnFind(found, where);
            // --- (21/11) chaque salle, la premiere fois qu'on y entre, a son evenement
            if (night && !NightCinema.Playing && !NightIntro.Playing && !NightUI.MenuOpen)
            {
                char room = NightMap.RoomAt(player.transform.position);
                if (room != lastRoom)
                {
                    lastRoom = room;
                    string happened = NightQuest.Enter(room, player.transform.position);
                    if (happened != null) Say(happened, 4f);
                    if (room == 'L') Journal.Show("secret");
                    if (room == 'R' && !bossFight && abbot == null) StartBoss();
                }
            }

            // --- les gestes (E) : la serrure, le silex, les cierges, la priere
            if (night && !NightGames.Busy && caught <= 0f)
            {
                Candle near = NearestUnlit();
                bool press = FiefInput.InteractPressed;
                if (press && sacristy != null && sacristy.Locked && Near(sacristy.Center, 2.3f))
                    NightGames.StartLock(sacristy.Center, OnLock);
                else if (press && !hasFlint && Near(FlintAt, 2f) && (sacristy == null || sacristy.Open))
                    TakeFlint();
                else if (press && chapel != null && chapel.Locked && Near(chapel.Center, 2.3f))
                {
                    if (NightQuest.HasKey) { chapel.Unlock(); NightMap.Unlock('K'); Say("La clé tourne. La chapelle est ouverte.", 5f); }
                    else { Say("Fermée à clé. Fouille le château.", 4f); try { Sfx.Deny(); } catch (System.Exception) { } }
                }
                else if (press && bossFight && NearestCryptFire() != null)
                {
                    lightingFire = NearestCryptFire();
                    NightGames.StartFlint(lightingFire.transform.position, OnBrazier);
                }
                else if (press && near != null)
                {
                    if (!hasFlint) Say("Il te faut du feu. La sacristie, à gauche du vestibule.", 5f);
                    else { lighting = near; NightGames.StartFlint(near.transform.position, OnFlint); }
                }
                // Prier a l'autel : tenir E deux secondes et demie. La foi remonte ; pas plus d'une fois par minute.
                if (Near(AltarAt, 2.4f) && FiefInput.InteractHeld && Time.time > prayedAt + 60f)
                {
                    prayHold += dt;
                    if (prayHold >= 2.5f)
                    {
                        prayHold = 0f; prayedAt = Time.time;
                        Faith.Add(0.4f);
                        try { Sfx.Choir(AltarAt); } catch (System.Exception) { }
                    }
                }
                else prayHold = 0f;
            }

            // --- attrape (le Penitent, le Rodeur), ou la foi perdue : le noir, et tu te reveilles dans le vestibule
            bool byPenitent = penitent.Touching, byProwler = Prowler.Toucher != null, faithless = Faith.Value <= 0f;
            bool byAbbot = abbot != null && abbot.Touching;
            // LA PRISE : il ne te renvoie pas gentiment au vestibule. Il est devant toi, il te tient, tu le vois
            // en face pendant presque une seconde, l'image tremble, l'accord grince. Puis le noir.
            if ((byPenitent || byProwler || byAbbot || faithless) && caught <= 0f && grab <= 0f && night && !NightCinema.Playing && !NightIntro.Playing && !NightUI.MenuOpen)
            {
                grab = 0.95f; grabProwler = byProwler; grabFaithless = faithless; grabbed = Prowler.Toucher; grabAbbot = byAbbot;
                if (abbot != null) abbot.Active = false;
                NightGames.Abort();
                NightCinema.Playing = true;
                NightAudio.Sting(1f);
                penitent.Held = true;
                Prowler.HoldAll(true);
                if (!byProwler && !byAbbot) { penitent.ShowPose(2); penitent.Glare(3.4f); }
            }
            if (grab > 0f)
            {
                grab -= dt;
                Transform monster = grabAbbot && abbot != null ? abbot.transform : grabProwler && grabbed != null ? grabbed.transform : penitent.transform;
                Vector3 ahead = player.transform.forward;
                monster.position = player.transform.position + ahead * (grabAbbot ? 1.5f : 0.95f);
                monster.rotation = Quaternion.LookRotation(-ahead, Vector3.up);
                player.Face(player.Yaw, Mathf.MoveTowards(player.Pitch, -24f, dt * 170f));
                NightPlayer.Shake = 1f;
            }
            if (grab <= 0f && penitent.Held)
            {
                bool byProwler2 = grabProwler; faithless = grabFaithless;
                byProwler = byProwler2;
                penitent.Held = false;
                Prowler.HoldAll(false);
                NightCinema.Playing = false;
                NightPlayer.Shake = 0f;
                caught = 1.6f;
                try { Sfx.Thud(); } catch (System.Exception) { }
                if (bossFight)
                {
                    // Dans la crypte : on se reveille contre le mur referme, et lui retourne au fond.
                    player.Teleport(NightMap.CryptEntry + Vector3.up * 0.1f, 0f);
                    if (abbot != null) { abbot.SendHome(NightMap.AbbotHome); abbot.Active = true; }
                }
                else
                {
                    player.Teleport(start, 0f);
                    penitent.Reset();
                    Prowler.AllHome();
                }
                if (faithless) Faith.Add(0.5f); else Faith.Add(-0.2f);
                Say(faithless ? "Ta foi s'est éteinte. Prie à l'autel, rallume les cierges."
                    : grabAbbot ? "Il t'a eu. Reste dans la lumière des brasiers."
                    : byProwler ? "Sa lanterne t'a vu. Reste derrière lui."
                    : "Il t'a eu. Il n'avance que quand tu ne le regardes pas.", 6f);
            }
            if (caught > 0f) caught -= dt;

            // --- l'aube : la lumiere revient, le brouillard se leve
            if (dawn > 0f)
            {
                dawn = Mathf.MoveTowards(dawn, 1f, dt / 6f);
                RenderSettings.ambientLight = Color.Lerp(Night, new Color(0.55f, 0.42f, 0.32f), dawn);
                RenderSettings.fogColor = Color.Lerp(Color.black, new Color(0.75f, 0.55f, 0.4f), dawn);
                RenderSettings.fogDensity = Mathf.Lerp(0.055f, 0.012f, dawn);
                if (player.View != null) player.View.backgroundColor = RenderSettings.fogColor;
            }
            // (12/11) Tant qu'il fait nuit : plus le Penitent est pres, plus le noir se referme.
            else if (!NightIntro.Playing && !NightUI.MenuOpen) RenderSettings.fogDensity = 0.072f + 0.045f * Penitent.Dread;
        }

        Candle lighting;

        bool Near(Vector3 point, float within)
        {
            Vector3 d = player.transform.position - point;
            d.y = 0f;
            return d.magnitude < within;
        }

        /// <summary>(21/11) Ce qu'on a trouve en fouillant.</summary>
        void OnFind(NightQuest.Find found, Vector3 where)
        {
            switch (found)
            {
                case NightQuest.Find.Secret:
                    NightMap.Reveal("LT");
                    Journal.Done("secret");
                    NightAudio.Bang(where + Vector3.up, 0.9f);
                    Say("Le livre s'enfonce. Tout le rayon pivote sur le côté...", 5f);
                    break;
                case NightQuest.Find.Lantern:
                    Journal.Lantern = true;
                    NightPlayer.Lantern = true;
                    Faith.Scale *= 0.7f;
                    Faith.Add(0.2f);
                    try { Sfx.Choir(where); } catch (System.Exception) { }
                    Say("La lanterne des Veilleurs. Ta lumière porte plus loin.", 5f);
                    break;
                case NightQuest.Find.Key:
                    Journal.Key = true;
                    Journal.Done("cle");
                    Journal.Show("cierges");
                    Say("Une clé de fer, froide. Celle de la chapelle. Quelque chose s'est réveillé.", 6f);
                    Faith.Add(0.1f);
                    NightAudio.Sting(0.9f);
                    NightAudio.Bang(NightMap.Center('K') + Vector3.up * 2f, 1f);
                    penitent.Wake();
                    Prowler.Hear(where);
                    break;
                case NightQuest.Find.Relic:
                    Journal.Relics++;
                    Journal.Show("reliques");
                    Say(Journal.Relics == 3 ? "Une troisième relique. Tu te sens béni." : "Une relique. Ta foi se réchauffe.", 4f);
                    if (Journal.Relics == 3) { Journal.Done("reliques"); Faith.Scale *= 0.5f; }
                    Faith.Add(0.3f);
                    try { Sfx.Choir(where); } catch (System.Exception) { }
                    break;
                case NightQuest.Find.Trap:
                    Say("Une aiguille dans la serrure. Le poison te brûle.", 4f);
                    Faith.Add(-0.25f);
                    NightAudio.Sting(0.8f);
                    NightPlayer.Shake = 0.8f;
                    Prowler.Hear(where);
                    break;
                case NightQuest.Find.Tomb:
                    Say("La dalle glisse... Quelque chose en sort.", 5f);
                    NightAudio.Sting(1f);
                    NightPlayer.Shake = 1f;
                    try { Prowler.Spawn(world, new[] { where, NightMap.RandomCell(where, 6f, 20f) }, player); }
                    catch (System.Exception e) { Debug.LogWarning("[FIEF] Le mort du tombeau ignoré : " + e.Message); }
                    break;
                case NightQuest.Find.Whisper:
                    Say("Vide. Mais quelque chose a bougé, tout près.", 4f);
                    NightAudio.Bang(where + Random.onUnitSphere * 5f + Vector3.up, 0.8f);
                    break;
                default:
                    Say("Rien.", 2f);
                    break;
            }
        }

        /// <summary>(21/11) La porte de la chapelle, fermee a cle.</summary>
        void BuildChapelDoor()
        {
            Vector3 door;
            float yaw;
            if (!NightMap.TryDoor('C', 'K', out door, out yaw)) return;
            bool alongX = Mathf.Abs(yaw) < 1f;
            GameObject leaf = alongX ? NightKit.Place("porte", door.x + 0.95f, door.z, 0f, 0f, false)
                : NightKit.Place("porte", door.x, door.z - 0.95f, 90f, 0f, false);
            if (leaf == null) return;
            chapel = NightDoor.Build(world, leaf.transform, door + Vector3.up * 1.5f,
                alongX ? new Vector3(2f, 3f, 0.35f) : new Vector3(0.35f, 3f, 2f), alongX ? 0f : 90f, alongX ? 100f : 195f);
            NightMap.Lock('K');
        }

        void OnLock(bool opened)
        {
            if (opened && sacristy != null) { sacristy.Unlock(); NightMap.Unlock('S'); }
            else if (!opened) Say("Raté. Le bon angle, puis force doucement.", 4f);
        }

        void OnFlint(bool lit)
        {
            if (lit && lighting != null) LightCandle(lighting);
            lighting = null;
        }

        /// <summary>Le silex pris : c'est la que la nuit commence vraiment. Le Rodeur sort de la chapelle.</summary>
        void TakeFlint()
        {
            hasFlint = true;
            Journal.Fire = true;
            Journal.Done("feu");
            Journal.Show("cle");
            if (flint != null) flint.SetActive(false);
            Faith.Add(0.1f);
            NightAudio.Bang(player.transform.position + Vector3.forward * 8f, 0.9f);
            // Deux Rodeurs : un qui fait le tour du grand couloir, un qui tourne dans la grande salle.
            Vector3[] ring = { NightMap.CellCenter(12, 8), NightMap.CellCenter(1, 8), NightMap.CellCenter(1, 3), NightMap.CellCenter(12, 3) };
            Vector3[] hall = { NightMap.CellCenter(9, 7), NightMap.CellCenter(4, 7), NightMap.CellCenter(4, 4), NightMap.CellCenter(9, 4) };
            try { Prowler.Spawn(world, ring, player); Prowler.Spawn(world, hall, player); }
            catch (System.Exception e) { Debug.LogWarning("[FIEF] Rôdeurs ignorés : " + e.Message); }
            Say("Quelque chose rôde. Trouve la clé de la chapelle.", 7f);
        }

        void LightCandle(Candle candle)
        {
            candle.Ignite();
            Faith.Add(0.2f);
            int lit = Lit();
            try { Sfx.BellAt(candle.transform.position); } catch (System.Exception) { }
            if (lit == 1) { penitent.Wake(); Say("Quelque chose a bougé dans le couloir. Ne le quitte pas des yeux.", 6f); }
            if (lit >= candles.Count)
            {
                // (21/11) Ce n'est pas fini : derriere l'autel, le mur s'enfonce dans le sol. La crypte.
                NightMap.Reveal("KR");
                Journal.Done("cierges");
                Journal.Show("crypte");
                NightAudio.Sting(0.8f);
                NightAudio.Bang(NightMap.Center('K') + new Vector3(-4f, 1f, 6f), 1f);
                NightPlayer.Shake = 0.6f;
                Say("Derrière l'autel, un pan de mur s'enfonce dans le sol...", 6f);
            }
        }

        /// <summary>(21/11) On descend dans la crypte : le mur remonte derriere toi, et l'Abbe se leve.</summary>
        void StartBoss()
        {
            bossFight = true;
            Journal.Done("crypte");
            Journal.Show("abbe");
            NightMap.Seal("KR");
            penitent.Crumble();
            Prowler.AllOff();
            abbot = Abbot.Spawn(world, NightMap.AbbotHome, player);
            NightAudio.Sting(1f);
            NightAudio.Bang(NightMap.CryptEntry + Vector3.up, 1f);
            Say("Le mur se referme derrière toi. Tu n'es pas seul.", 6f);
        }

        GameObject NearestCryptFire()
        {
            foreach (GameObject f in NightMap.CryptFires)
                if (f != null && !f.activeSelf && Near(f.transform.position, 2.1f)) return f;
            return null;
        }

        void OnBrazier(bool lit)
        {
            if (!lit || lightingFire == null) { lightingFire = null; return; }
            lightingFire.SetActive(true);
            lightingFire = null;
            int count = 0;
            foreach (GameObject f in NightMap.CryptFires) if (f != null && f.activeSelf) count++;
            Faith.Add(0.15f);
            if (abbot == null) return;
            if (count >= NightMap.CryptFires.Count) { abbot.Burn(); Say("Il brûle.", 4f); }
            else { abbot.Stagger(count); Say(count + " brasier" + (count > 1 ? "s" : "") + " sur " + NightMap.CryptFires.Count + ". Il recule... et revient plus vite.", 4f); }
        }

        /// <summary>L'Abbe a brule : le mur s'ouvre, l'aube se leve.</summary>
        void EndNight()
        {
            bossFight = false;
            dawn = 0.001f;
            NightMap.Reveal("KR");
            Journal.Done("abbe");
            Prowler.AllOff();
            NightKit.LightDawnFires();
            NightAudio.Dawn();
            Say("L'aube se lève. Tu as tenu. Le château est délivré.", 14f);
        }

        /// <summary>La sacristie : sa porte (si le kit en a une), un socle, et le silex qui luit dessus.</summary>
        void BuildSacristy()
        {
            Vector3 at = NightMap.Center('S');
            FlintAt = at + Vector3.up * 1.05f;
            Vector3 door;
            float yaw;
            if (NightMap.TryDoor('S', 'V', out door, out yaw))
            {
                // Le vantail : ses gonds sont a 95 cm du milieu de l'arche.
                GameObject leaf = NightKit.Place("porte", door.x, door.z - 0.95f, 90f, 0f, false);
                if (leaf != null)
                {
                    sacristy = NightDoor.Build(world, leaf.transform, door + Vector3.up * 1.5f, new Vector3(0.35f, 3f, 2f), 90f, 195f);
                    NightMap.Lock('S');
                }
            }
            Proto.Cube(world, new Vector3(at.x, 0.45f, at.z), new Vector3(0.7f, 0.9f, 0.7f), Stone, "Socle").GetComponent<Renderer>().enabled = false;
            GameObject plinth = NightKit.Place("autel", at.x, at.z, 0f, 0f, false);
            if (plinth != null) plinth.transform.localScale = new Vector3(0.3f, 0.82f, 0.62f);
            flint = new GameObject("Le silex");
            flint.transform.SetParent(world, false);
            flint.transform.position = FlintAt;
            // (16/11) Martin : "le silex est moche". C'est maintenant un pot a braises en fer, pose sur un socle de pierre.
            GameObject pot = NightKit.Model("braisier", flint.transform);
            if (pot != null) pot.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            Light glow = flint.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.4f);
            glow.range = 3.5f;
            glow.intensity = 0.9f;
        }

        int Lit()
        {
            int n = 0;
            for (int i = 0; i < candles.Count; i++) if (candles[i].Burning) n++;
            return n;
        }

        Candle NearestUnlit()
        {
            Candle best = null;
            float bestD = 2.4f;
            for (int i = 0; i < candles.Count; i++)
            {
                if (candles[i].Burning) continue;
                float d = Vector3.Distance(player.transform.position, candles[i].transform.position);
                if (d < bestD) { bestD = d; best = candles[i]; }
            }
            return best;
        }

        void Say(string line, float seconds) { message = line; messageUntil = Time.time + seconds; }

        // ------------------------------------------------------------------ l'ecran

        void OnGUI()
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label);
                text.font = NightFont.Get();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = Mathf.RoundToInt(Screen.height * 0.034f);
                text.normal.textColor = new Color(0.95f, 0.88f, 0.75f);
            }
            float w = Screen.width, h = Screen.height;

            // Le noir quand il t'a eu.
            if (caught > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(caught / 0.8f));
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            NightBlink.Draw(w, h);
            NightVeil.Draw(w, h, fear);

            // (21/11) Plus de petits cubes en haut de l'ecran : les quetes et l'inventaire sont dans le carnet (Tab).
            bool inGame = !NightUI.MenuOpen && !NightIntro.Playing;
            if (dawn <= 0f && inGame) Faith.Draw(w, h);
            NightGames.Draw(w, h);
            NightQuest.Draw(w, h);
            if (NightQuest.Flash > 0f)
            {
                GUI.color = new Color(0.85f, 0.9f, 1f, NightQuest.Flash * 0.55f);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (dawn <= 0f && !NightGames.Busy && inGame)
            {
                string hint = null;
                if (sacristy != null && sacristy.Locked && Near(sacristy.Center, 2.3f)) hint = "E  —  crocheter la serrure";
                else if (!hasFlint && Near(FlintAt, 2f) && (sacristy == null || sacristy.Open)) hint = "E  —  prendre les braises";
                else if (chapel != null && chapel.Locked && Near(chapel.Center, 2.3f)) hint = NightQuest.HasKey ? "E  —  ouvrir la chapelle" : "E  —  la porte de la chapelle";
                else if (bossFight && NearestCryptFire() != null) hint = "E  —  rallumer le brasier";
                else if (NightQuest.Hint(player.transform.position) != null) hint = NightQuest.Hint(player.transform.position);
                else if (NearestUnlit() != null) hint = "E  —  ranimer les braises";
                else if (Near(AltarAt, 2.4f) && Time.time > prayedAt + 60f) hint = "E (tenir)  —  prier";
                if (hint != null) NightFont.Label(new Rect(0f, h * 0.56f, w, h * 0.06f), hint, text);
            }
            else if (NightGames.Current != NightGames.Kind.None)
                NightFont.Label(new Rect(0f, h * 0.72f, w, h * 0.06f), NightGames.Current == NightGames.Kind.Lock
                    ? "Souris : le crochet     Espace : forcer     E : lâcher" : "Espace : souffler dans l'or     E : lâcher", text);
            if (Time.time < messageUntil && !NightCinema.Playing && inGame && !NightUI.Blocking) NightFont.Label(new Rect(w * 0.1f, h * 0.8f, w * 0.8f, h * 0.1f), message, text);
            NightCinema.Draw(w, h, text);
            NightIntro.Draw(w, h);
            NightUI.Draw(w, h, text, player != null ? player.transform.position : Vector3.zero, player != null ? player.Yaw : 0f);
        }
    }

    /// <summary>Un cierge sur son pied de fer. Eteint, il ne se voit presque pas ; allume, il eclaire six metres.</summary>
    public class Candle : MonoBehaviour
    {
        public bool Burning { get; private set; }
        GameObject flame;
        Light glow;
        float seed;

        public static Candle Build(Transform parent, Vector3 at, Color tint)
        {
            GameObject go = new GameObject("Cierge");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Candle c = go.AddComponent<Candle>();
            // (12/11) Le cierge en fer forge du kit, avec sa vraie flamme ; sans le kit, les cylindres d'avant.
            float lampHeight = 1.9f;
            GameObject kitFire = null;
            try { kitFire = NightKit.CandleStand(go.transform); } catch (System.Exception) { }
            if (kitFire != null)
            {
                c.flame = kitFire;
                lampHeight = 1.78f;
            }
            else
            {
                Proto.BeginVisualOnly();
                Proto.Cylinder(go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.05f, 0.5f), new Color(0.1f, 0.1f, 0.12f), "Pied");
                Proto.Cylinder(go.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.07f, 0.6f, 0.07f), new Color(0.1f, 0.1f, 0.12f), "Tige");
                Proto.Cylinder(go.transform, new Vector3(0f, 1.26f, 0f), new Vector3(0.3f, 0.03f, 0.3f), new Color(0.1f, 0.1f, 0.12f), "Coupelle");
                Proto.Cylinder(go.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 0.22f, 0.12f), new Color(0.85f, 0.8f, 0.68f), "Cire");
                c.flame = Proto.Sphere(go.transform, new Vector3(0f, 1.84f, 0f), new Vector3(0.1f, 0.22f, 0.1f), tint, "Flamme");
                Proto.EndVisualOnly();
                c.flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(tint, 2.2f);
            }
            c.flame.SetActive(false);
            GameObject lamp = new GameObject("Lueur");
            lamp.transform.SetParent(go.transform, false);
            lamp.transform.localPosition = new Vector3(0f, lampHeight, 0f);
            c.glow = lamp.AddComponent<Light>();
            c.glow.type = LightType.Point;
            c.glow.color = tint;
            c.glow.range = 7f;
            c.glow.intensity = 0f;
            c.glow.enabled = false;
            c.seed = at.x * 3.1f + at.z * 1.7f;
            return c;
        }

        public void Ignite()
        {
            Burning = true;
            flame.SetActive(true);
            glow.enabled = true;
        }

        void Update()
        {
            if (!Burning) return;
            // La flamme tremble : deux ondes de vitesses differentes, jamais reguliere.
            float t = Time.time + seed;
            glow.intensity = 1.25f + Mathf.Sin(t * 11f) * 0.12f + Mathf.Sin(t * 23f) * 0.08f;
        }
    }

    /// <summary>
    /// LE PENITENT. Sa regle, la seule : il n'avance que quand PERSONNE NE LE REGARDE.
    /// "Regarde" = il est dans l'image ET rien ne le cache (un mur, un pilier). Tourne le dos,
    /// passe derriere un pilier : il vient. Tu l'entends racler la pierre.
    /// </summary>
    public class Penitent : MonoBehaviour
    {
        const float Speed = 3.1f;
        /// <summary>Tout sauf le calque 2 ("Ignore Raycast") : le corps du joueur ne cache rien et ne barre aucun chemin.</summary>
        const int Solid = ~(1 << 2);
        NightPlayer prey;
        Vector3 home;
        bool awake;
        bool gone;
        float scrape;
        /// <summary>Les carrefours du niveau : quand un mur le separe de toi, il passe par la.</summary>
        static readonly Vector3[] Nodes =
        {
            new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 22f), new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, 38f), new Vector3(4.2f, 0f, 22f),
            new Vector3(6f, 0f, 6f)        // (14/11) la sacristie
        };
        static readonly int[][] Links = { new[] { 1, 6 }, new[] { 0, 2 }, new[] { 1, 3, 5 }, new[] { 2, 4 }, new[] { 3 }, new[] { 2 }, new[] { 0 } };

        public bool Touching { get { return awake && !gone && prey != null && Flat(prey.transform.position - transform.position).magnitude < 1.05f; } }

        public static Penitent Spawn(Transform parent, Vector3 at, NightPlayer prey)
        {
            GameObject go = new GameObject("LE PÉNITENT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            Penitent p = go.AddComponent<Penitent>();
            p.prey = prey;
            p.home = at;
            Dread = 0f;
            // (12/11) Les statues sculptees dans Blender (trois poses) ; sans le kit, la silhouette en cones d'avant.
            try { p.poses = NightKit.PenitentStatues(go.transform); } catch (System.Exception) { p.poses = null; }
            if (p.poses != null)
            {
                try { p.PrepareFear(); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Peur du Pénitent ignorée : " + e.Message); }
                return p;
            }
            Material robe = MaterialFactory.GetShiny(new Color(0.2f, 0.2f, 0.22f), 0.2f, 0f);
            Material bone = MaterialFactory.GetShiny(new Color(0.62f, 0.6f, 0.55f), 0.25f, 0f);
            Proto.BeginVisualOnly();
            // Une haute silhouette en robe (2,4 m), la tete penchee sous sa capuche, les mains jointes.
            Proto.Cone(go.transform, Vector3.zero, 0.62f, 1.9f, Color.white, "Robe", 10).GetComponent<Renderer>().sharedMaterial = robe;
            Proto.Capsule(go.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.62f, 0.5f, 0.5f), Color.white, "Buste").GetComponent<Renderer>().sharedMaterial = robe;
            Proto.Sphere(go.transform, new Vector3(0f, 2.12f, 0.05f), new Vector3(0.52f, 0.56f, 0.56f), Color.white, "Capuche").GetComponent<Renderer>().sharedMaterial = robe;
            Proto.Sphere(go.transform, new Vector3(0f, 2.06f, 0.2f), new Vector3(0.3f, 0.36f, 0.26f), Color.white, "Visage").GetComponent<Renderer>().sharedMaterial = bone;
            Proto.Sphere(go.transform, new Vector3(0f, 1.42f, 0.3f), new Vector3(0.22f, 0.26f, 0.2f), Color.white, "Mains jointes").GetComponent<Renderer>().sharedMaterial = bone;
            Material eye = MaterialFactory.GetGlow(new Color(0.9f, 0.15f, 0.1f), 1.4f);
            for (int s = -1; s <= 1; s += 2)
                Proto.Sphere(go.transform, new Vector3(s * 0.07f, 2.1f, 0.32f), Vector3.one * 0.035f, Color.white, "Œil").GetComponent<Renderer>().sharedMaterial = eye;
            Proto.EndVisualOnly();
            return p;
        }

        public void Wake() { awake = true; }
        public void Crumble() { gone = true; Dread = 0f; gameObject.SetActive(false); }
        public void Reset() { transform.position = home; transform.rotation = Quaternion.Euler(0f, -90f, 0f); ShowPose(0); }

        // ------------------------------------------------------------------ la peur (12/11)
        //
        // Martin : "rends le Penitent vraiment flippant, quitte a mettre moins de lumiere ; la, t'as
        // presque envie de lui faire un calin". Sa regle ne change pas (il n'avance que quand on ne le
        // regarde pas). Ce qui change, c'est tout ce qu'on RESSENT :
        //   - il a TROIS POSES, et il en change dans ton dos : il dort, il t'a vu, il te veut ;
        //   - ses yeux sont eteints tant qu'il dort, s'allument au premier cierge, et brulent de pres ;
        //   - plus il est pres, plus ta torche faiblit et tousse, plus le brouillard s'epaissit ;
        //   - un grondement sourd monte avec lui.

        /// <summary>De 0 (il est loin, ou il dort) a 1 (il est sur toi). La torche et le brouillard le lisent.</summary>
        public static float Dread;
        /// <summary>Tenu par la mise en scene (la prise) : il ne decide plus de rien.</summary>
        public bool Held;

        GameObject[] poses;
        int pose;
        float stall, hop = 12f;
        Material skin;
        AudioSource drone;

        void PrepareFear()
        {
            // Une copie de la matiere, rien que pour lui : on peut changer l'eclat de ses yeux sans toucher au fichier.
            Renderer first = poses[0].GetComponentInChildren<Renderer>();
            if (first != null && first.sharedMaterial != null)
            {
                skin = NightKit.Shadow();
                if (skin == null) skin = new Material(first.sharedMaterial);
                for (int i = 0; i < poses.Length; i++)
                    foreach (Renderer r in poses[i].GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = skin;
                skin.SetColor("_EmissionColor", Color.black);
                // (15/11) Martin : "le monstre n'est pas du tout realiste". On ne le montre plus : une SILHOUETTE noire
                // dans une fumee noire, et deux points rouges. Ce qu'on devine fait plus peur que ce qu'on voit.
                skin.color = new Color(0.010f, 0.010f, 0.013f);
                NightKit.Smoke(transform, 2.7f);
            }
            drone = gameObject.AddComponent<AudioSource>();
            drone.clip = Rumble();
            drone.loop = true;
            drone.playOnAwake = false;
            drone.spatialBlend = 0.6f;
            drone.rolloffMode = AudioRolloffMode.Linear;
            drone.minDistance = 2f;
            drone.maxDistance = 20f;
            drone.volume = 0f;
            drone.Play();
            ShowPose(0);
        }

        /// <summary>Montre une des trois statues (0 il dort, 1 il t'a vu, 2 il te veut).</summary>
        public void ShowPose(int which)
        {
            if (poses == null) return;
            pose = Mathf.Clamp(which, 0, poses.Length - 1);
            for (int i = 0; i < poses.Length; i++) poses[i].SetActive(i == pose);
        }

        /// <summary>L'eclat de ses yeux (0 : eteints).</summary>
        public void Glare(float strength)
        {
            if (skin != null) skin.SetColor("_EmissionColor", new Color(1f, 0.09f, 0.04f) * strength);
        }

        void Feel(float distance)
        {
            float near = awake ? Mathf.InverseLerp(13f, 2.5f, distance) : 0f;
            Dread = Mathf.MoveTowards(Dread, near, Time.deltaTime * 0.9f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (2f + 7f * Dread));
            Glare(awake ? 0.45f + 1.7f * Dread + 0.5f * Dread * pulse : 0f);
            if (drone != null)
            {
                drone.volume = Dread * Dread * 0.6f * Settings.Volume;
                drone.pitch = 0.82f + 0.3f * Dread;
            }
        }

        /// <summary>
        /// Un grondement grave fabrique par le code (aucun fichier) : trois notes tres basses qui battent
        /// l'une contre l'autre, et un souffle. Concept Unity : un AudioClip est une liste de nombres entre
        /// -1 et 1, 44 100 par seconde ; on peut la remplir soi-meme avec SetData.
        /// </summary>
        static AudioClip Rumble()
        {
            const int rate = 44100;
            int total = rate * 4, fade = rate / 3, length = total - fade;
            float[] raw = new float[total];
            System.Random random = new System.Random(7);
            float breath = 0f;
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)rate;
                breath += ((float)(random.NextDouble() * 2.0 - 1.0) - breath) * 0.02f;
                raw[i] = (Mathf.Sin(6.2832f * 41f * t) * 0.34f + Mathf.Sin(6.2832f * 55.5f * t) * 0.22f
                    + Mathf.Sin(6.2832f * 82.3f * t + Mathf.Sin(6.2832f * 0.25f * t) * 2f) * 0.12f + breath * 1.5f) * 0.5f;
            }
            // La fin se fond dans le debut : la boucle ne claque pas.
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float w = i < fade ? i / (float)fade : 1f;
                data[i] = raw[i] * w + (i < fade ? raw[length + i] * (1f - w) : 0f);
            }
            AudioClip clip = AudioClip.Create("Grondement du Pénitent", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Vrai si rien de solide ne separe ces deux points (a hauteur de poitrine).</summary>
        static bool Clear(Vector3 a, Vector3 b)
        {
            return !Physics.Linecast(a + Vector3.up * 1.4f, b + Vector3.up * 1.4f, Solid, QueryTriggerInteraction.Ignore);
        }

        bool Watched()
        {
            if (NightBlink.Closed) return false;       // (16/11) tes yeux sont fermes : il en profite
            Camera cam = prey.View;
            if (cam == null) return false;
            // Trois points de sa silhouette : il suffit d'en voir un.
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = transform.position + Vector3.up * (0.5f + i * 0.8f);
                Vector3 v = cam.WorldToViewportPoint(p);
                if (v.z <= 0.1f || v.x < -0.02f || v.x > 1.02f || v.y < -0.02f || v.y > 1.02f) continue;
                if (!Physics.Linecast(cam.transform.position, p, Solid, QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
        }

        void Update()
        {
            if (gone || prey == null || Held) return;
            float distance = Flat(prey.transform.position - transform.position).magnitude;
            if (poses != null) Feel(distance);
            if (!awake || Watched()) return;
            // Personne ne le regarde : il change de pose selon la distance, puis il avance.
            if (poses != null) ShowPose(distance < 4.5f ? 2 : distance < 10f ? 1 : 0);
            // (16/11) Imprevisible. Parfois il attend sans raison (on croit qu'il a lache). Parfois, loin de toi et hors
            // de ta vue, il n'est plus ou tu l'avais laisse : il t'attend plus loin.
            if (stall > 0f) { stall -= Time.deltaTime; return; }
            if (!NightBlink.Closed && Random.value < Time.deltaTime * 0.12f) { stall = Random.Range(1f, 3.5f); return; }
            hop -= Time.deltaTime;
            if (hop <= 0f)
            {
                hop = Random.Range(9f, 17f);
                if (distance > 13f)
                {
                    Vector3 spot = NightMap.RandomCell(prey.transform.position, 7f, 13f);
                    if (!NightDirector.Sees(prey.View, spot + Vector3.up * 1.6f)) { transform.position = spot; return; }
                }
            }
            Vector3 target = prey.transform.position;
            bool reach;
            target = Toward(transform.position, target, out reach);
            Vector3 step = Flat(target - transform.position);
            if (step.magnitude < 0.05f) return;
            // Pendant un clignement il BONDIT : quatre fois plus vite, un quart de seconde.
            float pace = NightBlink.Closed ? Speed * 4.5f : Speed;
            transform.position += step.normalized * Mathf.Min(pace * Time.deltaTime, step.magnitude);
            transform.rotation = Quaternion.LookRotation(Flat(prey.transform.position - transform.position).normalized, Vector3.up);
            // Le raclement de la pierre : tu sais qu'il bouge, tu ne sais pas ou il est.
            scrape -= Time.deltaTime;
            if (scrape <= 0f)
            {
                scrape = 0.55f;
                try { Sfx.ChipAt(transform.position); } catch (System.Exception) { }
            }
        }

        /// <summary>
        /// (14/11) Pour les autres monstres : ou aller, depuis "from", pour rejoindre "goal" ? Tout droit si rien
        /// ne barre, sinon par les carrefours. "ok" vaut false quand aucun chemin n'existe (une porte fermee).
        /// </summary>
        public static Vector3 Toward(Vector3 from, Vector3 goal, out bool ok)
        {
            if (NightMap.Ready) return NightMap.Toward(from, goal, out ok);       // (15/11) le grand chateau
            ok = true;
            if (Clear(from, goal)) return goal;
            int a = Nearest(from), b = Nearest(goal);
            if (a < 0 || b < 0) { ok = false; return from; }
            if (a == b) return Nodes[a];
            int[] prev = new int[Nodes.Length];
            for (int i = 0; i < prev.Length; i++) prev[i] = -1;
            Queue<int> open = new Queue<int>();
            open.Enqueue(a);
            prev[a] = a;
            while (open.Count > 0)
            {
                int n = open.Dequeue();
                if (n == b) break;
                for (int k = 0; k < Links[n].Length; k++)
                {
                    int m = Links[n][k];
                    if (prev[m] >= 0) continue;
                    prev[m] = n;
                    open.Enqueue(m);
                }
            }
            if (prev[b] < 0) { ok = false; return from; }
            int hop = b;
            while (prev[hop] != a) hop = prev[hop];
            return Flat(Nodes[a] - from).magnitude < 0.4f ? Nodes[hop] : Nodes[a];
        }

        /// <summary>Le prochain carrefour sur le plus court chemin vers "goal" (une petite recherche en largeur).</summary>
        Vector3 NextNode(Vector3 goal)
        {
            int from = Nearest(transform.position), to = Nearest(goal);
            if (from < 0 || to < 0) return goal;
            if (from == to) return Nodes[from];
            int[] prev = new int[Nodes.Length];
            for (int i = 0; i < prev.Length; i++) prev[i] = -1;
            Queue<int> open = new Queue<int>();
            open.Enqueue(from);
            prev[from] = from;
            while (open.Count > 0)
            {
                int n = open.Dequeue();
                if (n == to) break;
                for (int k = 0; k < Links[n].Length; k++)
                {
                    int m = Links[n][k];
                    if (prev[m] >= 0) continue;
                    prev[m] = n;
                    open.Enqueue(m);
                }
            }
            if (prev[to] < 0) return goal;
            int hop = to;
            while (prev[hop] != from) hop = prev[hop];
            // Deja sur son carrefour : il vise le suivant ; sinon il rejoint d'abord le sien.
            return Flat(Nodes[from] - transform.position).magnitude < 0.4f ? Nodes[hop] : Nodes[from];
        }

        /// <summary>Le carrefour le plus proche qu'on voit depuis "p".</summary>
        static int Nearest(Vector3 p)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < Nodes.Length; i++)
            {
                float d = Flat(Nodes[i] - p).sqrMagnitude;
                if (d < bestD && (d < 0.2f || Clear(p, Nodes[i]))) { bestD = d; best = i; }
            }
            return best;
        }
    }

    /// <summary>
    /// LE CHEVALIER, vu par ses yeux : ZQSD / WASD, la souris pour regarder, Maj pour courir.
    /// Sa torche est une lumiere accrochee a la camera, un peu a droite : les ombres bougent avec lui.
    ///
    /// Concept Unity : le CharacterController est une "gelule" que l'on deplace avec Move() ;
    /// il s'arrete tout seul contre les murs et monte les petites marches.
    /// </summary>
    public class NightPlayer : MonoBehaviour
    {
        public Camera View { get; private set; }
        CharacterController body;
        Light torch;
        float yaw, pitch, fall, stride, bob;
        /// <summary>(14/11) L'image tremble (0 : non, 1 : fort). Retombe toute seule.</summary>
        public static float Shake;
        /// <summary>(21/11) La torche s'etouffe pendant ce nombre de secondes.</summary>
        public static float TorchOut;
        /// <summary>(21/11) La hauteur des yeux (2,45 m a cheval), le balancement du galop, la lanterne trouvee au tresor.</summary>
        public static float EyeHeight = 1.65f;
        public static float RideBob;
        public static bool Lantern;

        /// <summary>Pose le chevalier ailleurs, sans toucher a son regard.</summary>
        public void Place(Vector3 at)
        {
            body.enabled = false;
            transform.position = at;
            body.enabled = true;
            fall = 0f;
        }
        public float Yaw { get { return yaw; } }
        public float Pitch { get { return pitch; } }
        /// <summary>La mise en scene tourne la tete du chevalier (cinematique, prise).</summary>
        public void Face(float newYaw, float newPitch) { yaw = newYaw; pitch = newPitch; }

        public static NightPlayer Spawn(Vector3 at)
        {
            GameObject go = new GameObject("LE CHEVALIER");
            go.transform.position = at;
            go.layer = 2;      // "Ignore Raycast" : voir Penitent.Solid
            NightPlayer p = go.AddComponent<NightPlayer>();
            p.body = go.AddComponent<CharacterController>();
            p.body.height = 1.8f;
            p.body.radius = 0.35f;
            p.body.center = new Vector3(0f, 0.9f, 0f);

            GameObject eye = new GameObject("Yeux");
            eye.transform.SetParent(go.transform, false);
            eye.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            p.View = eye.AddComponent<Camera>();
            p.View.clearFlags = CameraClearFlags.SolidColor;
            p.View.backgroundColor = Color.black;
            p.View.nearClipPlane = 0.08f;
            p.View.farClipPlane = 120f;
            p.View.fieldOfView = 72f;
            p.View.allowHDR = true;
            eye.tag = "MainCamera";
            eye.AddComponent<AudioListener>();
            if (Gfx.Fancy) eye.AddComponent<PostFx>();

            GameObject fire = new GameObject("Torche");
            fire.transform.SetParent(eye.transform, false);
            fire.transform.localPosition = new Vector3(0.35f, -0.15f, 0.3f);
            p.torch = fire.AddComponent<Light>();
            p.torch.type = LightType.Point;
            p.torch.color = new Color(1f, 0.75f, 0.50f);
            p.torch.range = 8f;          // (14/11) encore moins de lumiere
            p.torch.intensity = 1.05f;
            p.torch.shadows = LightShadows.Soft;
            p.torch.shadowStrength = 0.9f;
            p.torch.shadowNearPlane = 0.3f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return p;
        }

        public void Teleport(Vector3 at, float facing)
        {
            body.enabled = false;
            transform.position = at;
            body.enabled = true;
            yaw = facing;
            pitch = 0f;
            fall = 0f;
        }

        void Update()
        {
            // Echap rend la souris ; un clic la reprend.
            // (12/11) Un second Echap, souris deja rendue, quitte le jeu (dans le .exe ; sans effet dans l'editeur).
            // (21/11) Echap, la souris et les menus : c'est NightUI qui s'en occupe.

            if (Cursor.lockState == CursorLockMode.Locked && !NightGames.Busy)
            {
                Vector2 look = FiefInput.Look * (0.13f * Settings.Sensitivity);
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            View.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            // (14/11) La tete qui balance en marchant, et l'image qui tremble quand on est pris.
            Shake = Mathf.MoveTowards(Shake, 0f, Time.deltaTime * 2.5f);
            View.transform.localPosition = new Vector3(Mathf.Cos(bob * 0.5f) * 0.018f, EyeHeight + RideBob + Mathf.Sin(bob) * 0.032f, 0f)
                + Random.insideUnitSphere * (Shake * 0.045f);

            Vector2 input = NightGames.Busy ? Vector2.zero : FiefInput.Move;      // les mains prises : on ne bouge plus
            float speed = FiefInput.SprintHeld ? 5.6f : 3.4f;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;
            fall = body.isGrounded ? -2f : fall - 22f * Time.deltaTime;
            move.y = fall;
            body.Move(move * Time.deltaTime);

            // Les pas (l'armure s'entend), et la torche qui tremble.
            if (input.sqrMagnitude > 0.01f && body.isGrounded)
            {
                stride += speed * Time.deltaTime;
                bob += speed * Time.deltaTime * 3.0f;
                if (stride > 2.1f) { stride = 0f; try { Sfx.Step(); } catch (System.Exception) { } }
            }
            float t = Time.time;
            float flame = 1.05f + Mathf.Sin(t * 9f) * 0.09f + Mathf.Sin(t * 21f) * 0.06f + Mathf.PerlinNoise(t * 3f, 0f) * 0.18f;
            // (12/11) Le Penitent approche : la flamme faiblit, puis tousse (de courts trous noirs).
            float dread = Penitent.Dread;
            float cough = dread > 0.3f && Mathf.PerlinNoise(t * 11f, 3f) > 0.70f ? 0.6f * dread : 0f;
            torch.intensity = flame * (1f - 0.45f * dread) * (1f - cough);
            torch.range = 8f - 2.5f * dread + (Lantern ? 3.5f : 0f);
            if (Lantern) torch.intensity *= 1.25f;
            if (TorchOut > 0f) { TorchOut -= Time.deltaTime; torch.intensity *= 0.04f; }
        }
    }
}
