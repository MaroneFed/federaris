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
        static readonly Color StoneDark = new Color(0.27f, 0.26f, 0.26f);
        static readonly Color Flame = new Color(1f, 0.62f, 0.25f);

        Transform world;
        NightPlayer player;
        Penitent penitent;
        readonly List<Candle> candles = new List<Candle>();
        float dawn;                 // 0 : la nuit ; monte vers 1 quand la quete est finie
        float caught;               // le noir qui retombe quand le Penitent t'a eu
        string message = "Rallume les quatre cierges de la chapelle.";
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
            RenderSettings.ambientLight = new Color(0.012f, 0.013f, 0.02f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogDensity = 0.055f;
            QualitySettings.pixelLightCount = 6;
            QualitySettings.shadowDistance = 40f;

            world = new GameObject("=== LE CHÂTEAU, LA NUIT ===").transform;
            BuildRooms();
            try { Masonry.Apply(world); } catch (System.Exception e) { Debug.LogWarning("[FIEF] Maçonnerie ignorée : " + e.Message); }
            // (12/11) Le kit fait dans Blender habille les blocs. S'il manque, on reste en blocs.
            try
            {
                if (NightKit.Dress(world)) QualitySettings.pixelLightCount = 8;
                else Debug.Log("[FIEF] Kit absent (Resources/Modeles/Kit) : le niveau reste en blocs.");
            }
            catch (System.Exception e) { Debug.LogWarning("[FIEF] Kit ignoré : " + e.Message); }

            BuildChapel();
            player = NightPlayer.Spawn(new Vector3(0f, 0.1f, 3f));
            penitent = Penitent.Spawn(world, new Vector3(4.2f, 0f, 22f), player);
            messageUntil = Time.time + 7f;
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
            Wall(4f, 0f, 4f, 8f, 0f, 4f);
            Wall(-4f, 8f, -2f, 8f, 0f, 4f);
            Wall(2f, 8f, 4f, 8f, 0f, 4f);

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
            // L'autel, et quatre cierges sur leurs pieds de fer, aux quatre coins.
            Proto.Cube(world, new Vector3(0f, 0.55f, 41.5f), new Vector3(3f, 1.1f, 1.2f), Stone, "Autel");
            Vector3[] spots = { new Vector3(-4.2f, 0f, 34.5f), new Vector3(4.2f, 0f, 34.5f), new Vector3(-4.2f, 0f, 41.5f), new Vector3(4.2f, 0f, 41.5f) };
            for (int i = 0; i < spots.Length; i++) candles.Add(Candle.Build(world, spots[i], Flame));
            // Une lueur froide derriere l'autel (un vitrail, la lune) : on voit ou aller du bout du couloir.
            GameObject glass = Proto.Cube(world, new Vector3(0f, 3.6f, 43.66f), new Vector3(1.4f, 3f, 0.05f), Color.white, "Vitrail");
            Proto.StripCollider(glass);
            glass.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.35f, 0.5f, 0.9f), 0.9f);
            Light moon = new GameObject("Lune du vitrail").AddComponent<Light>();
            moon.transform.SetParent(world, false);
            moon.transform.position = new Vector3(0f, 3.6f, 42.6f);
            moon.type = LightType.Point;
            moon.color = new Color(0.4f, 0.55f, 1f);
            moon.range = 9f;
            moon.intensity = 0.7f;
        }

        // ------------------------------------------------------------------ la partie

        void Update()
        {
            if (player == null) return;
            float dt = Time.deltaTime;

            // --- allumer un cierge : s'approcher, appuyer sur E
            Candle near = NearestUnlit();
            if (near != null && FiefInput.InteractPressed && dawn <= 0f)
            {
                near.Ignite();
                int lit = Lit();
                try { Sfx.BellAt(near.transform.position); } catch (System.Exception) { }
                if (lit == 1) { penitent.Wake(); Say("Quelque chose a bougé dans le couloir. Ne le quitte pas des yeux.", 6f); }
                if (lit >= candles.Count) { dawn = 0.001f; penitent.Crumble(); NightKit.LightDawnFires(); Say("L'aube se lève. Le château est à toi.", 12f); }
            }

            // --- le Penitent t'a touche : le noir, et tu te reveilles dans le vestibule
            if (penitent.Touching && caught <= 0f && dawn <= 0f)
            {
                caught = 1.6f;
                try { Sfx.Thud(); } catch (System.Exception) { }
                player.Teleport(new Vector3(0f, 0.1f, 3f), 0f);
                penitent.Reset();
                Say("Il t'a eu. Il n'avance que quand tu ne le regardes pas.", 6f);
            }
            if (caught > 0f) caught -= dt;

            // --- l'aube : la lumiere revient, le brouillard se leve
            if (dawn > 0f)
            {
                dawn = Mathf.MoveTowards(dawn, 1f, dt / 6f);
                RenderSettings.ambientLight = Color.Lerp(new Color(0.012f, 0.013f, 0.02f), new Color(0.55f, 0.42f, 0.32f), dawn);
                RenderSettings.fogColor = Color.Lerp(Color.black, new Color(0.75f, 0.55f, 0.4f), dawn);
                RenderSettings.fogDensity = Mathf.Lerp(0.055f, 0.012f, dawn);
                if (player.View != null) player.View.backgroundColor = RenderSettings.fogColor;
            }
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
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = Mathf.RoundToInt(Screen.height * 0.028f);
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

            // Les quatre cierges, en haut : un point par cierge, dore quand il brule.
            float size = Mathf.Round(h * 0.018f), gap = size * 1.8f;
            float x0 = Mathf.Round(w * 0.5f - (candles.Count - 1) * gap * 0.5f - size * 0.5f);
            for (int i = 0; i < candles.Count; i++)
            {
                GUI.color = candles[i].Burning ? new Color(1f, 0.75f, 0.3f) : new Color(1f, 1f, 1f, 0.22f);
                GUI.DrawTexture(new Rect(x0 + i * gap, Mathf.Round(h * 0.035f), size, size), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            if (dawn <= 0f && NearestUnlit() != null) GUI.Label(new Rect(0f, h * 0.56f, w, h * 0.06f), "E  —  allumer le cierge", text);
            if (Time.time < messageUntil) GUI.Label(new Rect(w * 0.1f, h * 0.8f, w * 0.8f, h * 0.1f), message, text);
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
            Proto.BeginVisualOnly();
            Proto.Cylinder(go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.05f, 0.5f), new Color(0.1f, 0.1f, 0.12f), "Pied");
            Proto.Cylinder(go.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.07f, 0.6f, 0.07f), new Color(0.1f, 0.1f, 0.12f), "Tige");
            Proto.Cylinder(go.transform, new Vector3(0f, 1.26f, 0f), new Vector3(0.3f, 0.03f, 0.3f), new Color(0.1f, 0.1f, 0.12f), "Coupelle");
            Proto.Cylinder(go.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 0.22f, 0.12f), new Color(0.85f, 0.8f, 0.68f), "Cire");
            c.flame = Proto.Sphere(go.transform, new Vector3(0f, 1.84f, 0f), new Vector3(0.1f, 0.22f, 0.1f), tint, "Flamme");
            Proto.EndVisualOnly();
            c.flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(tint, 2.2f);
            c.flame.SetActive(false);
            GameObject lamp = new GameObject("Lueur");
            lamp.transform.SetParent(go.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 1.9f, 0f);
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
            new Vector3(0f, 0f, 4f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 22f), new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, 38f), new Vector3(4.2f, 0f, 22f)
        };
        static readonly int[][] Links = { new[] { 1 }, new[] { 0, 2 }, new[] { 1, 3, 5 }, new[] { 2, 4 }, new[] { 3 }, new[] { 2 } };

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
        public void Crumble() { gone = true; gameObject.SetActive(false); }
        public void Reset() { transform.position = home; transform.rotation = Quaternion.Euler(0f, -90f, 0f); }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Vrai si rien de solide ne separe ces deux points (a hauteur de poitrine).</summary>
        static bool Clear(Vector3 a, Vector3 b)
        {
            return !Physics.Linecast(a + Vector3.up * 1.4f, b + Vector3.up * 1.4f, Solid, QueryTriggerInteraction.Ignore);
        }

        bool Watched()
        {
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
            if (!awake || gone || prey == null || Watched()) return;
            Vector3 target = prey.transform.position;
            if (!Clear(transform.position, target)) target = NextNode(target);
            Vector3 step = Flat(target - transform.position);
            if (step.magnitude < 0.05f) return;
            transform.position += step.normalized * Mathf.Min(Speed * Time.deltaTime, step.magnitude);
            transform.rotation = Quaternion.LookRotation(Flat(prey.transform.position - transform.position).normalized, Vector3.up);
            // Le raclement de la pierre : tu sais qu'il bouge, tu ne sais pas ou il est.
            scrape -= Time.deltaTime;
            if (scrape <= 0f)
            {
                scrape = 0.55f;
                try { Sfx.ChipAt(transform.position); } catch (System.Exception) { }
            }
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
        float yaw, pitch, fall, stride;

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
            p.torch.color = new Color(1f, 0.66f, 0.34f);
            p.torch.range = 11f;
            p.torch.intensity = 1.5f;
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
            if (FiefInput.CancelPressed) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (FiefInput.ShootPressed) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = FiefInput.Look * (0.13f * Settings.Sensitivity);
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            View.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 input = FiefInput.Move;
            float speed = FiefInput.SprintHeld ? 5.6f : 3.4f;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;
            fall = body.isGrounded ? -2f : fall - 22f * Time.deltaTime;
            move.y = fall;
            body.Move(move * Time.deltaTime);

            // Les pas (l'armure s'entend), et la torche qui tremble.
            if (input.sqrMagnitude > 0.01f && body.isGrounded)
            {
                stride += speed * Time.deltaTime;
                if (stride > 2.1f) { stride = 0f; try { Sfx.Step(); } catch (System.Exception) { } }
            }
            float t = Time.time;
            torch.intensity = 1.5f + Mathf.Sin(t * 9f) * 0.1f + Mathf.Sin(t * 21f) * 0.07f + Mathf.PerlinNoise(t * 3f, 0f) * 0.2f;
        }
    }
}
