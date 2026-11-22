using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// FIEF, LA NUIT. Ce fichier fabrique tout (le chateau, le chevalier, les monstres, les vivants) et mene la partie.
    ///
    /// (21/11) L'HISTOIRE. Un Veilleur agonise dans le vestibule : l'Abbe a ouvert la crypte et la nuit est entree ; il
    /// reste trois vivants (la cuisiniere aux cuisines, le palefrenier aux ecuries, le novice en haut du donjon). Il te
    /// donne SA TORCHE. Tu la gardes allumee (aux feux, E tenu), tu allumes les torches et les bougies des couloirs
    /// derriere toi (aucun monstre ne passe la lumiere), tu retrouves les vivants. Le novice a la cle de la chapelle ; les
    /// quatre cierges ouvrent la crypte ; l'Abbe y attend, et ne supporte pas le feu.
    ///
    /// Concept Unity : un MonoBehaviour pose sur un objet vide de la scene. Awake() est appele une fois au lancement :
    /// on y fabrique tout le niveau par le code. Update() est appele a chaque image ; OnGUI() dessine l'ecran.
    /// </summary>
    public class NightBootstrap : MonoBehaviour
    {
        static readonly Color Night = new Color(0.011f, 0.015f, 0.028f);
        static readonly Color Flame = new Color(1f, 0.68f, 0.38f);

        Transform world;
        NightPlayer player;
        Penitent penitent;
        readonly List<Candle> candles = new List<Candle>();
        float dawn;                 // 0 : la nuit ; monte vers 1 quand l'Abbe a brule
        float caught;               // le noir qui retombe quand un monstre t'a eu
        string message = "";
        float messageUntil;
        GUIStyle text;
        char lastRoom = '.';
        Abbot abbot;
        bool bossFight;
        NightDoor chapel;
        NightDoor sacristy;
        Vector3 AltarAt, start, lastSafe;
        float fear, grab;
        float scare;          // (21/11 soir) l'eclat rouge du jumpscare
        float dead;           // (22/11) l'ecran de la mort, puis tout recommence
        string deathWhy = "";
        Vector3 entrance;
        Transform entranceLeaf;
        bool grabByAbbot;
        Transform grabber;
        string grabWhy = "";
        bool hitThisSwing, began;
        float refillTick;
        // (21/11) Les vivants, le chien, les nouveaux monstres qui arrivent au fil de la nuit.
        Survivor cook;
        Survivor groom;
        Survivor novice;
        Hound dog;
        bool soulsOut, demonOut, flayerOut;

        void Awake()
        {
            Settings.Load();
            Gfx.Prepare();
            Time.timeScale = 1f;
            try { Sfx.Init(gameObject); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Sons ignorés : " + e.Message); }

            // --- la nuit : aucune lumiere du ciel, un brouillard noir qui mange les distances
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Night;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogDensity = 0.055f;

            world = new GameObject("=== LE CHÂTEAU, LA NUIT ===").transform;
            Prowler.Forget();
            Beast.Forget();
            NightQuest.Clear();
            if (!NightMap.Build(world)) Debug.LogError("[FIEF] Kit absent (Resources/Modeles/Kit) : pas de château.");
            try { NightIntro.Build(world); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Dehors ignoré : " + e.Message); }
            Journal.Reset();
            NightUI.Reset();
            NightIntro.Playing = false;
            NightIntro.Finished = false;
            NightTalk.Open = false;
            HeldTorch.Raise = 0f;          // la torche n'est pas encore a toi : le Veilleur te la donnera
            // (22/11) Quand on meurt, la scene est rechargee : tout ce qui est "static" doit repartir de zero ici.
            NightPlayer.Lantern = false; NightPlayer.Punch = 0f; NightPlayer.Shake = 0f; NightPlayer.TorchOut = 0f;
            NightCinema.Playing = false; Beast.Dread = 0f; Abbot.Snuffed = false;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.shadowDistance = 90f;
            BuildChapel();
            start = NightMap.Center('V') + new Vector3(0f, 0.1f, -1.6f);
            lastSafe = start;
            player = NightPlayer.Spawn(start);
            penitent = Penitent.Spawn(world, NightMap.Center('N'), player);
            NightDirector.Begin(player);
            try { NightKit.Ambience(player.transform); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Poussière ignorée : " + e.Message); }
            Faith.Reset();
            NightGames.Abort();
            BuildSacristy();
            BuildChapelDoor();
            BuildEntrance();
            try { NightSecrets.Build(world); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Passages secrets ignorés : " + e.Message); }
            NightQuest.Deal(start);
            try { People(); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Survivants ignorés : " + e.Message); }
            try { NightAudio.Init(gameObject); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Sons de la nuit ignorés : " + e.Message); }
            // Le menu, devant le chateau ; puis l'arrivee a cheval ; puis l'ouverture. (Pas pour les photos de Claude.)
            if (NightAutoTest.Wanted) gameObject.AddComponent<NightAutoTest>();
            if (Application.isBatchMode) BeginInside();
            else { NightUI.OpenMenu(); try { NightIntro.MenuView(player); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Menu dehors ignoré : " + e.Message); } }
            Debug.Log("[FIEF] La Nuit : " + Beast.All.Count + " bêtes, " + Survivor.All.Count + " survivants, " + NightWard.All.Count + " feux à allumer.");
        }

        // ------------------------------------------------------------------ les vivants et les betes

        void People()
        {
            Vector3 q = NightMap.Center('Q'), b = NightMap.Center('B'), u = NightMap.Center('U');
            cook = Survivor.Spawn(world, "Cuisiniere", q + new Vector3(3.2f, 0f, 3.8f), 200f, 1.62f, "cook", "Mathilde, la cuisinière", new[]
            {
                "Ne me fais pas de mal... Oh. Un vivant. Un vrai.",
                "Je suis Mathilde. Je me cache près du feu depuis que les cloches se sont tues : ils n'aiment pas le feu.",
                "Tiens. De la viande, pour le chien des écuries : il a faim, le pauvre. Et de l'huile, pour ta torche.",
                NightSecrets.KitchenClue + " Il y a peut-être encore de quoi manger, là-derrière.",
                "Je vais à la chapelle, avec les autres. Le palefrenier est aux écuries, dans la grande cour. Fais vite."
            }, player);
            groom = Survivor.Spawn(world, "Palefrenier", b + new Vector3(-7f, 0f, 1.2f), 160f, 1.78f, "groom", "Aubin, le palefrenier", new[]
            {
                "Halte ! ... Pardon. J'ai cru que t'étais l'un d'eux.",
                "Les chevaux sont devenus fous à minuit. Un seul est resté, et Croc, mon chien. Il ne me suit plus : il a peur, il a faim.",
                "Nourris-le, il te suivra jusqu'en enfer. Il les sent venir, tu sais. Même ceux qu'on ne voit pas.",
                "Prends mes bougies. Pose-les derrière toi quand on te suit : ils ne passent pas la lumière.",
                "Le petit novice s'est barricadé en haut du donjon, de l'autre côté de la cour. Je file à la chapelle."
            }, player);
            novice = Survivor.Spawn(world, "Novice", u + new Vector3(-2.5f, 0f, 3.5f), 150f, 1.6f, "novice", "Frère Anselme, le novice", new[]
            {
                "Ne... ne regarde pas la fenêtre. Il y a des choses, dehors.",
                "J'ai vu l'Abbé descendre à la crypte avec des cierges noirs. Il a chanté... et les morts ont répondu.",
                "Voici la clé de la chapelle. Rallume ses quatre cierges : la lumière ouvrira le passage derrière l'autel.",
                "En bas, il t'attendra. Il ne supporte pas le feu : les brasiers de la crypte, allume-les tous.",
                "Et prends l'eau bénite de l'Abbé. Je prierai pour toi, à la chapelle."
            }, player);
            dog = Hound.Spawn(world, b + new Vector3(5f, 0f, -0.8f), player);

            // Les betes du debut : le Boucher dans les caves, la Bete qui dort dans l'ossuaire, deux loups dans la grande cour.
            Beast.Spawn(world, Beast.Kind.Butcher, NightMap.Center('W'), player);
            Beast.Spawn(world, Beast.Kind.Brute, NightMap.BeastLair, player);
            Beast.Spawn(world, Beast.Kind.Wolf, NightMap.Center('J') + new Vector3(-6f, 0f, 5f), player);
            Beast.Spawn(world, Beast.Kind.Wolf, NightMap.Center('J') + new Vector3(5f, 0f, -6f), player);
            // (22/11) Martin : "un niveau de difficulte beaucoup plus haut". Le Demon rode des le debut, deux Ames aussi,
            // et un troisieme loup.
            Beast.Spawn(world, Beast.Kind.Wolf, NightMap.Center('J') + new Vector3(0f, 0f, 7f), player);
            Beast.Spawn(world, Beast.Kind.Demon, NightMap.Center('H') + new Vector3(8f, 0f, 4f), player);
            Beast.Spawn(world, Beast.Kind.Soul, NightMap.CellIn('D'), player);
            Beast.Spawn(world, Beast.Kind.Soul, NightMap.CellIn('A'), player);
            demonOut = true;
            Beast.HoldAll(true);
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

        /// <summary>(21/11) La porte de la chapelle, fermee a cle (le novice a la cle).</summary>
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

        /// <summary>La sacristie, fermee a cle : on la crochete. Un coffre y attend.</summary>
        void BuildSacristy()
        {
            Vector3 door;
            float yaw;
            if (!NightMap.TryDoor('S', 'V', out door, out yaw)) return;
            GameObject leaf = NightKit.Place("porte", door.x, door.z - 0.95f, 90f, 0f, false);
            if (leaf == null) return;
            sacristy = NightDoor.Build(world, leaf.transform, door + Vector3.up * 1.5f, new Vector3(0.35f, 3f, 2f), 90f, 195f);
            NightMap.Lock('S');
        }

        // ------------------------------------------------------------------ la partie

        /// <summary>On entre : l'ouverture (le Veilleur mourant), puis la nuit commence.</summary>
        void BeginInside()
        {
            player.Teleport(start, 0f);
            // La grande porte claque derriere toi.
            if (entranceLeaf != null) { entranceLeaf.rotation = Quaternion.Euler(0f, -100f, 0f) * entranceLeaf.rotation; NightAudio.Bang(entrance + Vector3.up * 2f, 0.9f); entranceLeaf = null; }
            Journal.Fire = true;
            try { NightCinema.Begin(player, world); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Ouverture ignorée : " + e.Message); NightCinema.Playing = false; }
            Journal.ChangedAt = Time.unscaledTime + 28f;
            // Deux Rodeurs : un qui fait le tour du grand couloir, un qui tourne dans la grande salle.
            Vector3[] ring = { NightMap.CellCenter(12, 8), NightMap.CellCenter(1, 8), NightMap.CellCenter(1, 3), NightMap.CellCenter(12, 3) };
            Vector3[] hall = { NightMap.CellCenter(9, 7), NightMap.CellCenter(4, 7), NightMap.CellCenter(4, 4), NightMap.CellCenter(9, 4) };
            Vector3[] yard = { NightMap.CellCenter(18, 4), NightMap.CellCenter(21, 7), NightMap.CellCenter(20, 10), NightMap.CellCenter(14, 8) };
            try { Prowler.Spawn(world, ring, player); Prowler.Spawn(world, hall, player); Prowler.Spawn(world, yard, player); }
            catch (System.Exception e) { Debug.LogWarning("[FIEF] Rôdeurs ignorés : " + e.Message); }
        }

        bool Calm { get { return !NightCinema.Playing && !NightIntro.Playing && !NightUI.MenuOpen; } }

        void Update()
        {
            if (player == null) return;
            bool playingNow = !NightUI.MenuOpen && !NightIntro.Playing && !NightCinema.Playing && grab <= 0f;
            if (NightUI.Tick(playingNow)) Application.Quit();
            if (NightUI.MenuOpen) NightIntro.MenuTick(Time.unscaledDeltaTime);
            if (NightUI.StartRequested) { NightUI.StartRequested = false; NightIntro.Begin(player); }
            NightIntro.Tick(Time.deltaTime);
            if (NightIntro.Finished) { NightIntro.Finished = false; BeginInside(); }
            NightTalk.Tick();
            NightMap.Tick(Time.deltaTime, player.transform.position);
            if (bossFight && abbot != null && abbot.Burnt) EndNight();
            if (Abbot.Snuffed) { Abbot.Snuffed = false; Say("D'un revers de bras, il éteint un brasier. Rallume-le !", 4f); }
            float dt = Time.deltaTime;
            bool night = dawn <= 0f;
            bool hunted = Prowler.AnyHunting;
            NightCinema.Tick(dt);
            NightGames.Tick(dt);
            // Les monstres attendent la fin de l'ouverture.
            bool hold = !Calm || grab > 0f;
            if (!began && Calm) { began = true; Beast.HoldAll(false); penitent.Wake(); Say("Trouve Mathilde, aux cuisines. Garde ta torche allumée.", 7f); }
            if (night && Calm) Faith.Tick(dt, hunted);
            Beast.ResetDread();
            float want = Mathf.Max(Mathf.Max(Penitent.Dread, Beast.Dread), Mathf.Max(hunted ? 0.85f : 0f, Mathf.Clamp01((0.25f - Faith.Value) / 0.25f)));
            fear = Mathf.MoveTowards(fear, night ? want : 0f, dt * 0.8f);
            NightAudio.Tick(dt, fear, hunted, player.transform.position, night);
            NightDirector.Tick(dt, player, fear, night);
            if (Hound.Said.Length > 0) { Say(Hound.Said, 3.5f); Hound.Said = ""; }
            NightSecrets.Tick(dt);
            if (NightSecrets.Message.Length > 0) { Say(NightSecrets.Message, 5f); NightSecrets.Message = ""; SecretsProgress(); }
            if (NightSecrets.Descend) { NightSecrets.Descend = false; player.Teleport(NightSecrets.DescendTo, player.Yaw); NightAudio.Step(player.transform.position, 1f); }
            Brightness();

            // --- fouiller : tenir E pres d'un coffre, d'un tombeau, d'une bibliotheque
            NightQuest.Find found;
            Vector3 where;
            bool canAct = night && !NightGames.Busy && !NightTalk.Open && caught <= 0f && grab <= 0f;
            if (NightQuest.Tick(dt, player.transform.position, canAct && FiefInput.InteractHeld, out found, out where)) OnFind(found, where);
            // --- chaque salle, la premiere fois qu'on y entre, a son evenement
            if (night && Calm)
            {
                char room = NightMap.RoomAt(player.transform.position);
                if (room != lastRoom)
                {
                    lastRoom = room;
                    string happened = NightQuest.Enter(room, player.transform.position);
                    if (happened != null) Say(happened, 4.5f);

                    if (room == 'W' || room == 'O') Journal.Show("bete");
                    if (room == 'B' && !dog.Tamed && Journal.Meat) Journal.Show("chien");
                    if (room == 'V' && sacristy != null && sacristy.Locked) Journal.Show("sacristie");
                    if (room == 'R' && !bossFight && abbot == null) StartBoss();
                }
            }

            if (night && canAct && !hold)
            {
                Gestures(dt);
                Strike();
                Items();
            }
            else refillTick = 0f;
            Grabs(dt);

            // --- l'aube : la lumiere revient, le brouillard se leve
            if (dawn > 0f)
            {
                dawn = Mathf.MoveTowards(dawn, 1f, dt / 6f);
                RenderSettings.ambientLight = Color.Lerp(Night, new Color(0.55f, 0.42f, 0.32f), dawn);
                RenderSettings.fogColor = Color.Lerp(Color.black, new Color(0.75f, 0.55f, 0.4f), dawn);
                RenderSettings.fogDensity = Mathf.Lerp(0.055f, 0.012f, dawn);
                if (player.View != null) player.View.backgroundColor = RenderSettings.fogColor;
            }
            // Tant qu'il fait nuit : plus le Penitent est pres, ou plus ta torche est faible, plus le noir se referme.
            else if (!NightIntro.Playing && !NightUI.MenuOpen)
                RenderSettings.fogDensity = 0.07f + 0.045f * Penitent.Dread + 0.05f * Mathf.Clamp01((0.3f - Faith.Value) / 0.3f);
        }

        // ------------------------------------------------------------------ les gestes (E)

        void Gestures(float dt)
        {
            Vector3 me = player.transform.position;
            bool press = FiefInput.InteractPressed;
            Candle near = NearestUnlit();
            NightWard ward = NightWard.NearestUnlit(me);
            Survivor who = NearSurvivor();
            if (press && who != null) { Talk(who); return; }
            if (press && NightSecrets.Interact(me)) return;
            if (press && entrance != Vector3.zero && Near(entrance, 2.4f)) { Say("La porte s'est refermée derrière toi. Elle ne s'ouvrira plus avant l'aube.", 4f); try { Sfx.Deny(); } catch (System.Exception) { } return; }
            if (press && dog != null && dog.Near(me))
            {
                if (Journal.Meat) { Journal.Meat = false; dog.Tame(); Journal.Done("chien"); Say("Croc dévore la viande... puis vient se coller à ta jambe.", 5f); }
                else Say("Il gronde, les crocs dehors. Il a faim.", 3.5f);
                return;
            }
            if (press && sacristy != null && sacristy.Locked && Near(sacristy.Center, 2.3f)) { NightGames.StartLock(sacristy.Center, OnLock); return; }
            if (press && chapel != null && chapel.Locked && Near(chapel.Center, 2.3f))
            {
                if (Journal.Key) { chapel.Unlock(); NightMap.Unlock('K'); Journal.Done("cle"); Journal.Show("cierges"); Say("La clé tourne. La chapelle est ouverte.", 5f); }
                else { Say("Fermée à clé. Le novice, en haut du donjon, a la clé.", 4f); try { Sfx.Deny(); } catch (System.Exception) { } }
                return;
            }
            if (press && bossFight && NearestCryptFire() != null)
            {
                if (Faith.Value < 0.03f) { Say("Ta torche est morte. Ravive-la.", 3f); return; }
                OnBrazier(NearestCryptFire());
                return;
            }
            if (press && near != null)
            {
                if (chapel != null && chapel.Locked) return;
                if (Faith.Value < 0.03f) { Say("Ta torche est morte. Ravive-la.", 3f); return; }
                LightCandle(near);
                return;
            }
            if (press && ward != null)
            {
                if (Faith.Value < 0.03f) { Say("Ta torche est morte. Ravive-la d'abord à un feu.", 3f); return; }
                if (ward.Ignite()) Faith.Add(-0.02f);
                return;
            }
            // Raviver sa torche : E tenu pres d'une cheminee, d'un feu allume (qui perd ce que tu lui prends).
            bool searching = NightQuest.Hint(me) != null;
            if (FiefInput.InteractHeld && !searching && Faith.Value < 0.999f)
            {
                float got = 0f;
                if (NearHearth(me)) got = dt * 0.35f;
                else
                {
                    NightWard fire = NightWard.NearestBurning(me, 2.6f);
                    if (fire != null) got = fire.Take(dt * 0.3f);
                }
                if (got > 0f)
                {
                    Faith.Add(got);
                    refillTick -= dt;
                    if (refillTick <= 0f) { refillTick = 0.35f; try { NightAudio.Bang(me + player.transform.forward, 0.3f); } catch (System.Exception) { } }
                    if (NearHearth(me)) lastSafe = me;
                }
            }
            else refillTick = 0f;
        }

        bool NearHearth(Vector3 me)
        {
            foreach (Vector3 h in NightMap.Hearths) if (Near(h, 2.6f) && Mathf.Abs(h.y - me.y) < 2f) return true;
            return false;
        }

        Survivor NearSurvivor()
        {
            foreach (Survivor s in Survivor.All) if (s != null && s.Near(player.transform.position)) return s;
            return null;
        }

        void Talk(Survivor who)
        {
            NightTalk.Start(who.Name, who.Lines, () => Rescued(who));
        }

        /// <summary>Un survivant a parle : il te donne ce qu'il a et part se cacher a la chapelle.</summary>
        void Rescued(Survivor who)
        {
            who.Leave();
            Journal.Rescued++;
            try { NightAudio.Treasure(who.transform.position); } catch (System.Exception) { }
            if (who == cook) { Journal.Meat = true; Journal.Oil++; Journal.Done("cuisiniere"); Journal.Show("secrets"); Say("Viande et huile. Et un secret : les anneaux du mur sud.", 5f); }
            else if (who == groom) { Journal.Candles += 3; Journal.Done("palefrenier"); Say("Trois bougies. Pose-les quand on te suit (touche 3).", 5f); }
            else if (who == novice)
            {
                Journal.Key = true; Journal.HolyWater++;
                Journal.Done("novice");
                Say("La clé de la chapelle. Quelque chose s'est réveillé dans le château.", 6f);
                NightAudio.Sting(0.9f);
                if (!soulsOut)
                {
                    soulsOut = true;
                    char[] rooms = { 'L', 'D', 'H' };
                    foreach (char r in rooms) Beast.Spawn(world, Beast.Kind.Soul, NightMap.CellIn(r), player);
                }
            }
            if (Journal.Rescued == 1)
            {
                penitent.Wake();
                if (!demonOut) { demonOut = true; Beast.Spawn(world, Beast.Kind.Demon, NightMap.RandomCell(player.transform.position, 24f, 40f), player); }
            }
            // La suite : la prochaine quete n'apparait que quand la precedente est faite.
            if (!Journal.IsDone("cuisiniere")) Journal.Show("cuisiniere");
            else if (!Journal.IsDone("palefrenier")) Journal.Show("palefrenier");
            else if (!Journal.IsDone("novice")) Journal.Show("novice");
            else Journal.Show("cle");
            if (Journal.Rescued >= 3) Say("Les trois vivants sont à l'abri. Reste la chapelle.", 6f);
        }

        // ------------------------------------------------------------------ le coup de torche, les objets

        void Strike()
        {
            HeldTorch torch = player.Torch;
            if (torch == null) return;
            if (FiefInput.ShootPressed && !torch.Swinging && Faith.Value > 0.02f)
            {
                torch.Swing();
                hitThisSwing = false;
                Faith.Add(-0.015f);
                NightAudio.Fire(player.transform.position + player.transform.forward);
            }
            if (!torch.Striking || hitThisSwing) return;
            hitThisSwing = true;
            Transform eye = player.View.transform;
            // (22/11) L'Abbe : un coup de torche le fait reculer deux secondes.
            if (abbot != null && abbot.Active)
            {
                Vector3 ta = abbot.transform.position + Vector3.up * 2f - eye.position;
                if (Puppet.Flat(ta).magnitude < 3.4f && Vector3.Angle(Puppet.Flat(eye.forward), Puppet.Flat(ta)) < 60f) { abbot.Hit(player.transform.position); Faith.Add(-0.06f); Say("Il recule. Pas pour longtemps.", 2.5f); return; }
            }
            Beast b = Beast.InFront(eye, 2.9f);
            if (b != null)
            {
                string said = b.Burn(player.transform.position);
                if (said.Length > 0) Say(said, 3.5f);
                if (b.Dead && b.kind == Beast.Kind.Brute) Journal.Done("bete");
                return;
            }
            foreach (Prowler p in Prowler.All)
            {
                if (p == null || !p.isActiveAndEnabled) continue;
                Vector3 to = p.transform.position + Vector3.up - eye.position;
                if (Puppet.Flat(to).magnitude < 2.9f && Vector3.Angle(Puppet.Flat(eye.forward), Puppet.Flat(to)) < 55f) { p.Burn(player.transform.position); return; }
            }
            Vector3 pt = penitent.transform.position + Vector3.up - eye.position;
            if (Puppet.Flat(pt).magnitude < 2.6f && Vector3.Angle(Puppet.Flat(eye.forward), Puppet.Flat(pt)) < 55f) Say("La pierre ne brûle pas.", 2.5f);
        }

        void Items()
        {
            Vector3 me = player.transform.position;
            if (FiefInput.Pressed(KeyCode.Alpha1))
            {
                if (Journal.Oil <= 0) { Say("Plus d'huile.", 2f); return; }
                Journal.Oil--; Faith.Add(0.6f);
                Say("L'huile crépite. Ta torche reprend vie.", 3f);
            }
            if (FiefInput.Pressed(KeyCode.Alpha2))
            {
                if (Journal.HolyWater <= 0) { Say("Plus d'eau bénite.", 2f); return; }
                Journal.HolyWater--;
                int n = Beast.Scare(me, 12f, 15f);
                foreach (Prowler p in Prowler.All) if (p != null && Puppet.Flat(p.transform.position - me).magnitude < 12f) { p.Burn(me); n++; }
                NightQuest.Flash = 0.6f;
                try { NightAudio.Treasure(me); } catch (System.Exception) { }
                Say(n > 0 ? "L'eau bénite siffle sur les dalles. Ils s'enfuient." : "L'eau bénite tombe sur la pierre. Rien ne bouge.", 3.5f);
            }
            if (FiefInput.Pressed(KeyCode.Alpha3))
            {
                if (Journal.Candles <= 0) { Say("Plus de bougies.", 2f); return; }
                Journal.Candles--;
                NightLight.Candles(null, me + player.transform.forward * 0.6f, 90f, true);
                Say("Une bougie, sur les dalles. Rien ne passera sa lumière.", 3f);
            }
            if (FiefInput.Pressed(KeyCode.Alpha4))
            {
                if (!Journal.Eternal) return;
                Journal.Eternal = false;
                NightLight.Eternal(null, me + player.transform.forward * 0.7f);
                Say("Le cierge éternel. Ici, la nuit n'entrera plus jamais.", 4f);
            }
        }

        // ------------------------------------------------------------------ la prise

        void Grabs(float dt)
        {
            bool night = dawn <= 0f;
            Prowler prowler = Prowler.Toucher;
            Beast beast = Beast.Toucher;
            bool byPenitent = penitent.Touching, byAbbot = abbot != null && abbot.Touching;
            // LA PRISE : il est devant toi, il te tient, tu le vois en face pendant presque une seconde. Puis le noir.
            if ((byPenitent || prowler != null || beast != null || byAbbot) && caught <= 0f && grab <= 0f && night && Calm)
            {
                grab = 0.95f;
                grabByAbbot = byAbbot;
                grabber = byAbbot ? abbot.transform : beast != null ? beast.transform : prowler != null ? prowler.transform : penitent.transform;
                grabWhy = byAbbot ? "Il t'a eu. Reste dans la lumière des brasiers."
                    : beast != null ? Lesson(beast)
                    : prowler != null ? "Sa lanterne t'a vu. Passe derrière lui, ou repousse-le d'un coup de torche."
                    : "Il t'a eu. Il n'avance que quand tu ne le regardes pas.";
                if (abbot != null) abbot.Active = false;
                NightGames.Abort();
                NightTalk.Open = false;
                NightCinema.Playing = true;
                NightAudio.Sting(1f);
                penitent.Held = true;
                Prowler.HoldAll(true);
                Beast.HoldAll(true);
                if (byPenitent) { penitent.ShowPose(2); penitent.Glare(3.4f); }
                // (21/11 soir) LE JUMPSCARE : un hurlement en pleine face, l'image se resserre et saute, un eclat rouge.
                NightAudio.Scream(player.transform.position + player.transform.forward * 0.6f + Vector3.up * 1.6f, 1f);
                NightAudio.Roar(player.transform.position + player.transform.forward, 1f);
                NightPlayer.Punch = 1f;
                scare = 1f;
            }
            if (grab > 0f)
            {
                grab -= dt;
                Vector3 ahead = player.transform.forward;
                if (grabber != null)
                {
                    // Il fond sur toi : de deux metres a un demi-metre en un clin d'oeil, le visage plein l'ecran.
                    float k = Mathf.Clamp01((0.95f - grab) / 0.18f);
                    grabber.position = player.transform.position + ahead * Mathf.Lerp(grabByAbbot ? 2.4f : 2f, grabByAbbot ? 1.1f : 0.55f, k * k);
                    grabber.rotation = Quaternion.LookRotation(-ahead, Vector3.up);
                }
                // On leve les yeux vers sa tete (ils sont tous plus grands que toi), la tete secouee.
                player.Face(player.Yaw + Random.Range(-2f, 2f), Mathf.MoveTowards(player.Pitch, -24f + Random.Range(-3f, 3f), dt * 260f));
                NightPlayer.Shake = 1f;
                if (grab <= 0f) Release();
            }
            if (caught > 0f) caught -= dt;
            if (dead > 0f)
            {
                dead -= dt;
                if (dead <= 0f)
                {
                    // On efface tout le chateau, et un nouveau "FIEF" le rebatit de zero (comme au lancement).
                    foreach (GameObject root in gameObject.scene.GetRootGameObjects()) Destroy(root);
                    new GameObject("FIEF (La Nuit)").AddComponent<NightBootstrap>();
                }
            }
        }

        static string Lesson(Beast b)
        {
            switch (b.kind)
            {
                case Beast.Kind.Soul: return "Une Âme a bu ta flamme. Frappe-les (clic gauche), elles se déchirent.";
                case Beast.Kind.Butcher: return "Le Boucher est aveugle : il t'a ENTENDU. Ne cours pas près de lui.";
                case Beast.Kind.Demon: return "Le Démon n'approche que quand ta torche faiblit. Garde-la haute.";
                case Beast.Kind.Brute: return "La Bête charge tout droit. Écarte-toi au dernier moment.";
                case Beast.Kind.Flayer: return "L'Écorcheur ne vient que dans ton dos. Retourne-toi.";
                default: return "Les loups n'osent pas une flamme haute. La tienne baissait.";
            }
        }

        void Release()
        {
            // (22/11) Martin : "quand on meurt, on perd tout". Plus de reveil pres du feu : l'ecran de la mort, puis la nuit
            // recommence depuis le debut (la scene est rechargee, tout est perdu).
            if (!Application.isBatchMode && !NightAutoTest.Wanted)
            {
                dead = 7f;
                deathWhy = grabWhy;
                NightCinema.Playing = true;
                NightPlayer.Shake = 0f;
                try { NightAudio.Sting(1f); } catch (System.Exception) { }
                return;
            }
            penitent.Held = false;
            Prowler.HoldAll(false);
            Beast.HoldAll(false);
            NightCinema.Playing = false;
            NightPlayer.Shake = 0f;
            caught = 1.6f;
            try { Sfx.Thud(); } catch (System.Exception) { }
            if (bossFight)
            {
                player.Teleport(NightMap.CryptEntry + Vector3.up * 0.1f, 0f);
                if (abbot != null) { abbot.SendHome(NightMap.AbbotHome); abbot.Active = true; }
            }
            else
            {
                // Tu te reveilles pres du dernier feu ou tu as ravive ta torche ; eux sont repartis.
                player.Teleport(lastSafe + Vector3.up * 0.1f, player.Yaw);
                penitent.Reset();
                Prowler.AllHome();
                Beast.Scare(player.transform.position, 999f, 8f);
            }
            Faith.Value = Mathf.Max(Faith.Value, 0.45f);
            Say(grabWhy, 6f);
        }

        // ------------------------------------------------------------------ la chapelle, la crypte

        bool Near(Vector3 point, float within)
        {
            Vector3 d = player.transform.position - point;
            if (Mathf.Abs(d.y) > 2.5f) return false;
            d.y = 0f;
            return d.magnitude < within;
        }

        /// <summary>Ce qu'on a trouve en fouillant.</summary>
        void OnFind(NightQuest.Find found, Vector3 where)
        {
            switch (found)
            {
                case NightQuest.Find.Secret:
                    NightMap.Reveal(NightQuest.LastSecret);
                    NightAudio.Bang(where + Vector3.up, 0.9f);
                    Say(NightQuest.LastSecret == "IW" ? "La bouteille bascule. Tout le râtelier glisse sur le côté..." : "Le livre s'enfonce. Tout le rayon pivote sur le côté...", 5f);
                    SecretsProgress();
                    break;
                case NightQuest.Find.Lantern:
                    Journal.Lantern = true;
                    NightPlayer.Lantern = true;
                    Faith.Scale *= 0.7f;
                    Faith.Add(0.3f);
                    try { NightAudio.Treasure(where); } catch (System.Exception) { }
                    Say("La lanterne des Veilleurs. Ta lumière porte plus loin, et dure plus longtemps.", 5f);
                    break;
                case NightQuest.Find.Relic:
                    Journal.Relics++;
                    Journal.Show("reliques");
                    if (Journal.Relics == 3) Journal.Done("reliques");
                    Faith.Scale *= 0.85f;
                    Faith.Add(0.25f);
                    try { NightAudio.Treasure(where); } catch (System.Exception) { }
                    Say(Journal.Relics == 3 ? "Une troisième relique. Ta flamme ne craint plus grand-chose." : "Une relique. Ta flamme se redresse.", 4f);
                    break;
                case NightQuest.Find.Oil:
                    Journal.Oil++;
                    Say("Une fiole d'huile (touche 1).", 3f);
                    break;
                case NightQuest.Find.HolyWater:
                    Journal.HolyWater++;
                    Say("De l'eau bénite (touche 2). Tout ce qui rôde autour de toi fuira.", 4f);
                    break;
                case NightQuest.Find.Candles:
                    Journal.Candles += 2;
                    Say("Deux bougies (touche 3). Pose-les derrière toi.", 3.5f);
                    break;
                case NightQuest.Find.Eternal:
                    Journal.Eternal = true;
                    Journal.Done("bete");
                    try { NightAudio.Treasure(where); } catch (System.Exception) { }
                    Say("Sous les os, un cierge qui brûle encore. Il brûle depuis cent ans. (touche 4)", 6f);
                    break;
                case NightQuest.Find.Tomb:
                    Say("La dalle glisse... Quelque chose en sort. Mais il y a de l'huile, dans la main du mort.", 5f);
                    Journal.Oil++;
                    NightAudio.Sting(1f);
                    NightPlayer.Shake = 1f;
                    try { Beast.Spawn(world, Beast.Kind.Soul, where + Vector3.up * 0.4f, player); }
                    catch (System.Exception e) { Debug.LogWarning("[FIEF] Le mort du tombeau ignoré : " + e.Message); }
                    break;
                default:
                    Say("Rien.", 2f);
                    break;
            }
        }

        void OnLock(bool opened)
        {
            if (opened && sacristy != null) { sacristy.Unlock(); NightMap.Unlock('S'); Journal.Done("sacristie"); Say("Le pêne cède. La sacristie.", 3f); }
            else if (!opened) Say("Raté. Trouve l'angle (la souris), puis force doucement (Espace).", 4f);
        }

        void LightCandle(Candle candle)
        {
            candle.Ignite();
            Faith.Add(-0.02f);
            int lit = Lit();
            try { NightAudio.Fire(candle.transform.position); } catch (System.Exception) { }
            if (lit < candles.Count) { Say(lit + " cierge" + (lit > 1 ? "s" : "") + " sur " + candles.Count + ".", 3f); return; }
            // Derriere l'autel, le mur s'enfonce dans le sol. La crypte.
            NightMap.Reveal("KR");
            Journal.Done("cierges");
            Journal.Show("crypte");
            NightAudio.Sting(0.8f);
            NightAudio.Bang(NightMap.Center('K') + new Vector3(-4f, 1f, 6f), 1f);
            NightPlayer.Shake = 0.6f;
            Say("Derrière l'autel, un pan de mur s'enfonce dans le sol...", 6f);
            if (!flayerOut) { flayerOut = true; Beast.Spawn(world, Beast.Kind.Flayer, NightMap.RandomCell(player.transform.position, 20f, 36f), player); }
        }

        /// <summary>On descend dans la crypte : le mur remonte derriere toi, et l'Abbe se leve.</summary>
        void StartBoss()
        {
            bossFight = true;
            Journal.Done("crypte");
            Journal.Show("abbe");
            NightMap.Seal("KR");
            penitent.Crumble();
            Prowler.AllOff();
            foreach (Beast b in Beast.All) if (b != null) b.gameObject.SetActive(false);
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

        void OnBrazier(GameObject fire)
        {
            fire.SetActive(true);
            Faith.Add(0.1f);
            int count = 0;
            foreach (GameObject f in NightMap.CryptFires) if (f != null && f.activeSelf) count++;
            try { NightAudio.Fire(fire.transform.position); } catch (System.Exception) { }
            if (abbot == null) return;
            if (count >= NightMap.CryptFires.Count) { abbot.Burn(); Say("Il brûle.", 4f); }
            else { abbot.Stagger(count); Say(count + " brasier" + (count > 1 ? "s" : "") + " sur " + NightMap.CryptFires.Count + ". Il recule... et revient plus vite.", 4f); }
        }

        /// <summary>L'Abbe a brule : le mur s'ouvre, l'aube se leve, les vivants sortent de la chapelle.</summary>
        void EndNight()
        {
            bossFight = false;
            dawn = 0.001f;
            NightMap.Reveal("KR");
            Journal.Done("abbe");
            Prowler.AllOff();
            NightKit.LightDawnFires();
            NightAudio.Dawn();
            float x0, z0, x1, z1;
            NightMap.Bounds('K', out x0, out z0, out x1, out z1);
            int i = 0;
            foreach (Survivor s in Survivor.All) if (s != null && s.Met) s.ShowInChapel(new Vector3(x0 + 3f + i++ * 3f, 0f, z1 - 5f));
            Say(Journal.Rescued == 3 ? "L'aube se lève. Les trois vivants t'attendent dans la chapelle. Le château est délivré."
                : "L'aube se lève. Tu as tenu. " + (Journal.Rescued > 0 ? Journal.Rescued + " vivant" + (Journal.Rescued > 1 ? "s" : "") + " t'attend" + (Journal.Rescued > 1 ? "ent" : "") + " dans la chapelle." : "Mais personne ne t'attend."), 14f);
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

        /// <summary>(22/11) La porte par ou l'on est entre, vue de l'interieur : refermee, pour toute la nuit.</summary>
        void BuildEntrance()
        {
            float x0, z0, x1, z1;
            NightMap.Bounds('V', out x0, out z0, out x1, out z1);
            entrance = new Vector3(26f, 0f, z0 + 0.3f);
            // Le vantail, ferme, dans l'arche : on vient d'entrer par la, il s'est referme.
            GameObject leaf = NightKit.Place("porte", 26f + 0.95f, z0, 0f, 0f, false);
            if (leaf == null) { entrance = Vector3.zero; return; }
            // Grande ouverte pendant l'arrivee a cheval ; elle claquera derriere toi (BeginInside).
            entranceLeaf = leaf.transform;
            entranceLeaf.rotation = Quaternion.Euler(0f, 100f, 0f) * entranceLeaf.rotation;
        }

        /// <summary>(22/11) La quete des passages secrets : combien sont ouverts.</summary>
        void SecretsProgress()
        {
            Journal.Show("secrets");
            Journal.SetDetail("secrets", NightSecrets.Opened + " sur " + NightSecrets.Total + " ouverts. Les murs, les anneaux, les bougeoirs, les tombes : tout peut cacher un passage.");
            if (NightSecrets.Opened >= NightSecrets.Total) Journal.Done("secrets");
        }

        /// <summary>(22/11) La luminosite choisie dans le menu (Reglages) : la nuit reste noire, mais on y voit un peu plus.</summary>
        void Brightness()
        {
            if (dawn > 0f) return;
            float b = NightUI.Brightness;
            RenderSettings.ambientLight = Night * (0.5f + 2.5f * b * b);
        }

        /// <summary>L'ecran de la mort : tout est perdu, la nuit recommence.</summary>
        void DrawDeath(float w, float h)
        {
            float k = Mathf.Clamp01((7f - dead) / 1.2f);
            GUI.color = new Color(0f, 0f, 0f, k);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUIStyle t = new GUIStyle(text);
            t.font = NightFont.Title();
            t.fontSize = Mathf.RoundToInt(h * 0.12f);
            t.normal.textColor = new Color(0.55f, 0.03f, 0.02f, k);
            NightFont.Label(new Rect(0f, h * 0.3f, w, h * 0.16f), "Tu es mort", t);
            GUIStyle d = new GUIStyle(text);
            d.fontSize = Mathf.RoundToInt(h * 0.026f);
            d.fontStyle = FontStyle.Italic;
            d.wordWrap = true;
            d.normal.textColor = new Color(0.8f, 0.72f, 0.6f, k);
            NightFont.Label(new Rect(w * 0.2f, h * 0.5f, w * 0.6f, h * 0.1f), deathWhy, d);
            d.normal.textColor = new Color(0.6f, 0.55f, 0.5f, k * Mathf.Clamp01((5f - dead) / 1f));
            NightFont.Label(new Rect(0f, h * 0.68f, w, h * 0.05f), "Tout est perdu. La nuit recommence.", d);
        }

        // ------------------------------------------------------------------ l'ecran

        string Hint()
        {
            Vector3 me = player.transform.position;
            Survivor who = NearSurvivor();
            if (who != null) return "E  —  parler à " + who.Name.Split(',')[0];
            if (dog != null && dog.Near(me)) return Journal.Meat ? "E  —  donner la viande au chien" : "E  —  approcher la main du chien";
            string secret = NightSecrets.Hint(me);
            if (secret != null) return secret;
            if (sacristy != null && sacristy.Locked && Near(sacristy.Center, 2.3f)) return "E  —  crocheter la serrure";
            if (entrance != Vector3.zero && Near(entrance, 2.4f)) return "E  —  la grande porte";
            if (chapel != null && chapel.Locked && Near(chapel.Center, 2.3f)) return Journal.Key ? "E  —  ouvrir la chapelle" : "E  —  la porte de la chapelle";
            if (bossFight && NearestCryptFire() != null) return "E  —  allumer le brasier";
            string search = NightQuest.Hint(me);
            if (search != null) return search;
            if (NearestUnlit() != null && (chapel == null || !chapel.Locked)) return "E  —  allumer le cierge";
            NightWard w = NightWard.NearestUnlit(me);
            if (w != null) return w.kind == NightWard.Kind.Torch ? "E  —  allumer la torche" : w.kind == NightWard.Kind.Brazier ? "E  —  allumer le brasero" : "E  —  allumer les bougies";
            if (Faith.Value < 0.98f && (NearHearth(me) || NightWard.NearestBurning(me, 2.6f) != null)) return "E (tenir)  —  raviver ta torche";
            return null;
        }

        void OnGUI()
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label);
                text.font = NightFont.Get();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = Mathf.RoundToInt(Screen.height * 0.032f);
                text.normal.textColor = new Color(0.95f, 0.88f, 0.75f);
            }
            float w = Screen.width, h = Screen.height;

            if (caught > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(caught / 0.8f));
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            // Le jumpscare : un eclat rouge, et des images noires qui sautent, comme un coeur qui rate.
            if (scare > 0f)
            {
                if (Event.current.type == EventType.Repaint) scare = Mathf.MoveTowards(scare, 0f, Time.unscaledDeltaTime * 1.1f);
                bool cut = scare > 0.4f && Mathf.Repeat(Time.unscaledTime * 17f, 1f) < 0.18f;
                GUI.color = cut ? new Color(0f, 0f, 0f, 0.85f) : new Color(0.55f, 0f, 0f, scare * 0.45f);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            NightVeil.Draw(w, h, fear);
            if (dead > 0f) { DrawDeath(w, h); return; }

            bool inGame = !NightUI.MenuOpen && !NightIntro.Playing && !NightCinema.Playing;
            if (dawn <= 0f && inGame) Faith.Draw(w, h);
            NightGames.Draw(w, h);
            NightQuest.Draw(w, h);
            if (NightQuest.Flash > 0f)
            {
                GUI.color = new Color(0.85f, 0.9f, 1f, NightQuest.Flash * 0.55f);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (dawn <= 0f && !NightGames.Busy && !NightTalk.Open && inGame)
            {
                string hint = Hint();
                if (hint != null)
                {
                    GUIStyle s = new GUIStyle(text);
                    s.fontSize = Mathf.RoundToInt(h * 0.026f);
                    s.normal.textColor = new Color(1f, 0.9f, 0.7f);
                    NightFont.Label(new Rect(0f, h * 0.58f, w, h * 0.05f), hint, s);
                }
            }
            else if (NightGames.Current == NightGames.Kind.Lock)
            {
                GUIStyle s = new GUIStyle(text);
                s.fontSize = Mathf.RoundToInt(h * 0.024f);
                NightFont.Label(new Rect(0f, h * 0.88f, w, h * 0.05f), "Souris : chercher l'angle     ·     Espace (tenir) : tourner     ·     E : lâcher", s);
            }
            if (Time.time < messageUntil && inGame && !NightUI.Blocking && !NightTalk.Open)
            {
                GUIStyle s = new GUIStyle(text);
                s.wordWrap = true;
                s.fontStyle = FontStyle.Italic;
                NightFont.Label(new Rect(w * 0.18f, h * 0.76f, w * 0.64f, h * 0.1f), message, s);
            }
            NightCinema.Draw(w, h, text);
            NightIntro.Draw(w, h);
            NightUI.Draw(w, h, text, player != null ? player.transform.position : Vector3.zero, player != null ? player.Yaw : 0f);
            NightTalk.Draw(w, h, text);
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
            // (21/11) Un vrai souffle de bete, en boucle (le grondement fabrique par le calcul sonnait faux).
            AudioClip[] breaths = Resources.LoadAll<AudioClip>("Sons/Nuit/Souffle");
            drone.clip = breaths.Length > 0 ? breaths[0] : Rumble();
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
            // (21/11) Les feux allumes : il ne les passe pas. Les escaliers : ses pieds suivent le sol.
            Vector3 next = NightWard.Hold(transform.position, transform.position + step.normalized * Mathf.Min(pace * Time.deltaTime, step.magnitude));
            next.y = NightMap.FloorY(next);
            transform.position = next;
            transform.rotation = Quaternion.LookRotation(Flat(prey.transform.position - transform.position).normalized, Vector3.up);
            // Le raclement de la pierre : tu sais qu'il bouge, tu ne sais pas ou il est.
            scrape -= Time.deltaTime;
            if (scrape <= 0f)
            {
                scrape = 0.55f;
                try { NightAudio.Bang(transform.position, 0.3f); } catch (System.Exception) { }
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
        /// <summary>(21/11 soir) Le coup de camera d'une apparition : l'image se resserre d'un coup, puis revient.</summary>
        public static float Punch;
        public static bool Lantern;
        /// <summary>(21/11) La torche tenue en main (NightLight.cs).</summary>
        public HeldTorch Torch;

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

            // (21/11) La torche, dans la main droite.
            try { p.Torch = HeldTorch.Build(p.View); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Torche en main ignorée : " + e.Message); }
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
            if (Punch > 0f)
            {
                Punch = Mathf.MoveTowards(Punch, 0f, Time.deltaTime * 1.6f);
                if (!NightCinema.Playing && !NightIntro.Playing) View.fieldOfView = 72f - 16f * Punch * Punch;
            }
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
            float dread = Mathf.Max(Penitent.Dread, Beast.Dread);
            float cough = dread > 0.3f && Mathf.PerlinNoise(t * 11f, 3f) > 0.70f ? 0.6f * dread : 0f;
            // (21/11) Sa lumiere suit la reserve : pleine, elle porte a neuf metres ; presque vide, a deux ; morte, une braise.
            float fuel = Faith.Value;
            torch.intensity = flame * (1f - 0.45f * dread) * (1f - cough) * (fuel > 0.001f ? 0.35f + 0.65f * Mathf.Sqrt(fuel) : 0.05f);
            torch.range = 2.2f + 6.8f * fuel - 3f * dread * fuel + (Lantern ? 3f : 0f);
            if (HeldTorch.Raise < 0.5f) torch.intensity *= 0.3f;
            // La lumiere part de la flamme : les ombres bougent avec ta main.
            if (Torch != null && Torch.FlamePoint != null && HeldTorch.Raise > 0.5f) torch.transform.position = Torch.FlamePoint.position + View.transform.forward * 0.05f;
            if (Lantern) torch.intensity *= 1.25f;
            if (TorchOut > 0f) { TorchOut -= Time.deltaTime; torch.intensity *= 0.04f; }
        }
    }
}
