using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE GRAND CHATEAU (15/11, agrandi le 21/11). Le chateau est DESSINE dans Plan, une lettre par case de 4 m,
    /// et ce fichier le batit tout seul avec les pieces du kit (sols, plafonds, murs, arches, voutes, piliers),
    /// pose les murs invisibles qui arretent le joueur, et les torches qu'on peut allumer.
    ///
    ///   .  de la roche (rien)        C  couloir (voute)          V  vestibule (on y entre)
    ///   S  sacristie                 N  niche du Penitent        H  grande salle (8 m de haut)
    ///   K  chapelle (8 m)            Y  petite cour (ciel)       L  bibliotheque   D  dortoir   A  armurerie
    ///   P  cachots                   Q  cuisines                 R  crypte         T  tresor
    ///   (21/11, Martin : "un chateau beaucoup plus grand, une cour, les ecuries, le donjon, un sous-sol, des etages")
    ///   J  la grande cour (ciel)     B  les ecuries              G  le donjon (rez-de-chaussee)
    ///   E  l'escalier qui MONTE      U  le donjon, A L'ETAGE (sol a 4 m)
    ///   F  l'escalier qui DESCEND    W  les caves (sol a -4 m)   O  l'ossuaire (sol a -4 m)
    ///
    /// Deux cases de lettres differentes sont separees par un mur. Une PORTE (une arche) s'ouvre entre deux
    /// cases voisines ecrites en MINUSCULES. La premiere ligne du plan est le nord.
    ///
    /// LES NIVEAUX : chaque salle a un sol (Base) et un plafond (Top). Un mur entre deux salles monte du plus bas
    /// des deux sols au plus haut des deux plafonds : vu des caves, le mur descend sous le sol des cuisines.
    /// Un escalier (E, F) est une rampe de deux cases : elle part du sol d'une salle et arrive a celui de l'autre.
    ///
    /// Les monstres s'y retrouvent tout seuls : Toward() cherche le chemin de case en case, et FloorY() leur dit
    /// a quelle hauteur poser les pieds.
    /// </summary>
    public static class NightMap
    {
        public const float Cell = 4f;
        static readonly string[] Plan =
        {
            "...........................",
            "...........................",
            ".................OOOO......",
            ".RRRR........WWWWOOOO......",
            ".RRRR......IiwWWwoOOO......",
            ".RRRR.TT...IIWWWWOOOO..UUU.",
            ".rRRR.tT.....WwWW......UUU.",
            ".kKK.LlLL.DDD.f..BBBBBBUUU.",
            ".KKK.LLLlxdDD.f..BBBBBBUuU.",
            ".KkK.LlLL.DdDQqQQBBbBBB.e..",
            ".CcCCCcCCCCccqQQQJJjJJJ.e..",
            ".C..HHhHHHYYCQQqqjJJJJJGgG.",
            ".C..HHHHHHYycZzzZJJJJJJGGG.",
            ".cn.HHHHHHAacPpPPJJJJJjgGG.",
            ".C..HHhHHH..cpPPpjJJJJJGGG.",
            ".CCCCCcCCCCCCPPPPJJJJJJGGG.",
            "...SsvvV...................",
            ".....VVV...................",
            "...........................",
        };

        static char[,] grid;
        static int wide, deep;
        public static bool Ready;
        static readonly Dictionary<string, Vector4> doors = new Dictionary<string, Vector4>();      // x, z, yaw, hauteur
        static readonly HashSet<char> locked = new HashSet<char>();
        /// <summary>(16/11) Les armures vides : NightDirector les tourne vers le joueur quand il ne regarde pas.</summary>
        public static readonly List<Transform> Armors = new List<Transform>();
        /// <summary>(21/11) Les grands feux qui ne s'eteignent jamais (cheminees) : on y ravive sa torche.</summary>
        public static readonly List<Vector3> Hearths = new List<Vector3>();
        /// <summary>(21/11) Les escaliers : ou ils partent (bas) et ou ils arrivent (haut), hauteur comprise.</summary>
        static readonly Dictionary<char, Vector3[]> flights = new Dictionary<char, Vector3[]>();
        static Transform blocks;

        static void Keep(GameObject armor) { if (armor != null) Armors.Add(armor.transform); }

        /// <summary>La lune (NightQuest la fait flamber pendant les eclairs de la cour).</summary>
        public static Light MoonLight;
        /// <summary>La salle (sa lettre majuscule) ou se trouve ce point ; '.' hors du chateau.</summary>
        public static char RoomAt(Vector3 p) { return grid == null ? '.' : Room(Col(p), Row(p)); }
        static readonly List<Vector3> taken = new List<Vector3>();

        /// <summary>Une case du chateau prise au hasard, entre "min" et "max" metres de "near" (jamais dans une salle fermee a cle).</summary>
        public static Vector3 RandomCell(Vector3 near, float min, float max)
        {
            for (int tries = 0; tries < 60; tries++)
            {
                int c = Random.Range(0, wide), r = Random.Range(0, deep);
                char room = Room(c, r);
                if (room == '.' || locked.Contains(room) || room == 'N' || room == 'S' || room == 'R' || room == 'T' || room == 'O' || Stair(room)) continue;
                Vector3 p = CellCenter(c, r);
                float d = new Vector2(p.x - near.x, p.z - near.z).magnitude;
                if (d >= min && d <= max) return p;
            }
            return CellCenter(Col(near), Row(near));
        }

        /// <summary>(21/11) Une case au hasard DANS cette salle.</summary>
        public static Vector3 CellIn(char room)
        {
            List<Vector3> cells = new List<Vector3>();
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                    if (Room(c, r) == room) cells.Add(CellCenter(c, r));
            return cells.Count == 0 ? Vector3.zero : cells[Random.Range(0, cells.Count)];
        }

        const int Solid = ~(1 << 2);
        static readonly int[] DX = { 0, 1, 0, -1 }, DZ = { 1, 0, -1, 0 };       // nord, est, sud, ouest

        static char At(int c, int r) { return c < 0 || r < 0 || c >= wide || r >= deep ? '.' : grid[c, r]; }
        static char Room(int c, int r) { return char.ToUpperInvariant(At(c, r)); }
        static int Col(Vector3 p) { return Mathf.FloorToInt(p.x / Cell); }
        static int Row(Vector3 p) { return Mathf.FloorToInt(p.z / Cell); }

        // ------------------------------------------------------------------ (21/11) les niveaux

        /// <summary>Un escalier (une rampe de deux cases).</summary>
        static bool Stair(char room) { return room == 'E' || room == 'F'; }
        /// <summary>A ciel ouvert : pas de plafond.</summary>
        static bool Open(char room) { return room == 'Y' || room == 'J'; }

        /// <summary>La hauteur du sol de la salle (le bas de la rampe pour un escalier).</summary>
        static float Base(char room)
        {
            switch (room)
            {
                case 'U': return 4f;
                case 'W': case 'O': case 'F': case 'I': return -4f;
                default: return 0f;
            }
        }

        /// <summary>La hauteur du plafond (le haut des murs pour une cour).</summary>
        static float Top(char room)
        {
            switch (room)
            {
                case 'U': case 'E': return 8f;
                case 'W': case 'O': case 'I': return 0f;
                case 'F': return 4f;
                case 'H': case 'K': case 'Y': case 'R': case 'J': return 8f;
                default: return 4f;
            }
        }

        public static Vector3 CellCenter(int c, int r)
        {
            Vector3 p = new Vector3(c * Cell + 2f, 0f, r * Cell + 2f);
            p.y = FloorAt(Room(c, r), p);
            return p;
        }

        /// <summary>A quelle hauteur poser les pieds, ici ? (le sol de la salle, ou la pente de l'escalier)</summary>
        public static float FloorY(Vector3 p)
        {
            if (grid == null) return 0f;
            return FloorAt(Room(Col(p), Row(p)), p);
        }

        static float FloorAt(char room, Vector3 p)
        {
            Vector3[] f;
            if (!Stair(room) || !flights.TryGetValue(room, out f)) return Base(room);
            Vector3 lo = f[0], hi = f[1];
            Vector2 axis = new Vector2(hi.x - lo.x, hi.z - lo.z);
            float t = axis.sqrMagnitude < 0.01f ? 0f : Mathf.Clamp01(Vector2.Dot(new Vector2(p.x - lo.x, p.z - lo.z), axis) / axis.sqrMagnitude);
            return Mathf.Lerp(lo.y, hi.y, t);
        }

        static bool Door(int c, int r, int d)
        {
            char a = At(c, r), b = At(c + DX[d], r + DZ[d]);
            return a != '.' && b != '.' && char.IsLower(a) && char.IsLower(b) && char.ToUpperInvariant(a) != char.ToUpperInvariant(b);
        }

        /// <summary>Le couloir est-il droit ici ? 1 : nord-sud, 2 : est-ouest, 0 : un coude (pas de voute).</summary>
        static int Straight(int c, int r)
        {
            if (Room(c, r) != 'C') return 0;
            bool n = Room(c, r + 1) == 'C', s = Room(c, r - 1) == 'C', e = Room(c + 1, r) == 'C', w = Room(c - 1, r) == 'C';
            if (n && s && !e && !w) return 1;
            if (e && w && !n && !s) return 2;
            return 0;
        }

        /// <summary>Les deux bouts de chaque escalier : la ou il touche la salle du bas, la ou il touche celle du haut.</summary>
        static void FindFlights()
        {
            flights.Clear();
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    char room = Room(c, r);
                    if (!Stair(room)) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        if (!Door(c, r, d)) continue;
                        char other = Room(c + DX[d], r + DZ[d]);
                        Vector3 edge = new Vector3(c * Cell + 2f + DX[d] * 2f, Base(other), r * Cell + 2f + DZ[d] * 2f);
                        Vector3[] f;
                        if (!flights.TryGetValue(room, out f)) { f = new[] { edge, edge }; flights[room] = f; }
                        if (edge.y <= f[0].y) f[0] = edge;
                        if (edge.y >= f[1].y) f[1] = edge;
                    }
                }
        }

        // ------------------------------------------------------------------ batir

        public static bool Build(Transform world)
        {
            Ready = false;
            doors.Clear();
            locked.Clear();
            Armors.Clear();
            Hearths.Clear();
            taken.Clear();
            secrets.Clear(); revealed.Clear(); covers.Clear(); CryptFires.Clear(); TrapTomb = null;
            seen = null; lastC = lastR = -1;
            deep = Plan.Length;
            wide = Plan[0].Length;
            grid = new char[wide, deep];
            for (int i = 0; i < deep; i++)
                for (int c = 0; c < wide; c++) grid[c, deep - 1 - i] = Plan[i][c];
            FindFlights();
            if (!NightKit.Begin(world)) return false;
            NightLight.Clear();
            blocks = new GameObject("Murs invisibles").transform;
            blocks.SetParent(world, false);
            Material stone = NightKit.Mat("Pierre");

            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    char room = Room(c, r);
                    if (room == '.') continue;
                    Vector3 p = CellCenter(c, r);
                    float floor = Base(room), top = Top(room);
                    int straight = Straight(c, r);

                    // Le sol (une dalle et sa plaque invisible), le plafond ou la voute. Un escalier a ses marches.
                    if (!Stair(room))
                    {
                        NightKit.Place("dalle_sol", p.x, p.z, 0f, floor, false);
                        Box(new Vector3(p.x, floor - 0.25f, p.z), new Vector3(Cell, 0.5f, Cell), 0f, "Sol");
                    }
                    if (straight != 0) NightKit.Place("voute_couloir", p.x, p.z, straight == 1 ? 0f : 90f, 0f, false);
                    else if (!Open(room)) NightKit.Place("dalle_sol", p.x, p.z, 0f, top, true);

                    for (int d = 0; d < 4; d++)
                    {
                        int nc = c + DX[d], nr = r + DZ[d];
                        char other = Room(nc, nr);
                        float ex = p.x + DX[d] * 2f, ez = p.z + DZ[d] * 2f, yaw = d % 2 == 0 ? 0f : 90f;
                        if (other == room)
                        {
                            // Une voute qui s'arrete contre un coude : on bouche le haut.
                            if (straight != 0 && Straight(nc, nr) == 0) NightKit.Place("mur_droit", ex, ez, yaw, 4f, false);
                            continue;
                        }
                        bool mine = other == '.' || d < 2;          // chaque mur partage n'est pose qu'une fois
                        if (!mine) continue;
                        float lo = floor, hi = top;
                        if (other != '.') { lo = Mathf.Min(lo, Base(other)); hi = Mathf.Max(hi, Top(other)); }
                        float doorY = float.NaN;
                        if (Door(c, r, d))
                        {
                            // La porte s'ouvre au niveau du sol de la salle qui n'est pas un escalier.
                            doorY = Stair(room) ? Base(other) : floor;
                            string key = room < other ? "" + room + other : "" + other + room;
                            doors[key] = new Vector4(ex, ez, yaw, doorY);
                            if (IsSecret(key)) secrets[key] = new Vector4(ex, ez, yaw, d);
                        }
                        // (22/11) La grande porte du chateau : la ou l'arrivee a cheval entre, au sud du vestibule (x = 26).
                        // Avant, la porte de dehors s'ouvrait sur un mur de briques.
                        if (room == 'V' && d == 2 && other == '.' && Mathf.Abs(ex - 26f) < 0.1f)
                        {
                            NightKit.ArchAtY(ex, ez, yaw, 0f);
                            Box(new Vector3(ex, 2f, ez), new Vector3(4.5f, 4f, 0.5f), yaw, "Grande porte");
                            continue;
                        }
                        for (float y = lo; y < hi - 0.01f; y += 4f)
                        {
                            if (!float.IsNaN(doorY) && Mathf.Abs(y - doorY) < 0.01f) { NightKit.ArchAtY(ex, ez, yaw, y); continue; }
                            NightKit.Place("mur_droit", ex, ez, yaw, y, false);
                            Box(new Vector3(ex, y + 2f, ez), new Vector3(4.5f, 4f, 0.5f), yaw, "Mur");
                        }
                        // (21/11) Une torche qu'on peut allumer, de loin en loin, le long des couloirs droits.
                        if (float.IsNaN(doorY) && straight != 0 && (c * 7 + r * 13) % 3 == 0)
                            NightLight.WallTorch(blocks, ex, ez, d, floor);
                    }

                    // Les piliers des couloirs : un de chaque cote, a chaque case.
                    if (straight != 0)
                        for (int s = -1; s <= 1; s += 2)
                        {
                            float px = straight == 1 ? p.x + s * 1.55f : p.x - 2f, pz = straight == 1 ? p.z - 2f : p.z + s * 1.55f;
                            NightKit.Place("pilier", px, pz, 0f, 0f, false);
                            Box(new Vector3(px, 2f, pz), new Vector3(0.55f, 4f, 0.55f), 0f, "Pilier");
                        }
                    // (21/11) Aux coudes des couloirs : un bouquet de bougies a allumer.
                    if (room == 'C' && straight == 0 && (c + r) % 2 == 0)
                        NightLight.Candles(blocks, p + new Vector3(1.2f, 0f, 1.2f), 70f, false);
                }

            foreach (KeyValuePair<char, Vector3[]> f in flights) Stairs(f.Value[0], f.Value[1], stone);
            Furnish();
            BuildSecrets();
            Ready = true;
            return true;
        }

        /// <summary>Un mur invisible (BoxCollider) : c'est lui qui arrete le joueur, pas la piece du kit.</summary>
        static void Box(Vector3 center, Vector3 size, float yaw, string name)
        {
            BoxCollider box = new GameObject(name).AddComponent<BoxCollider>();
            box.transform.SetParent(blocks, false);
            box.transform.position = center;
            box.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            box.size = size;
        }

        /// <summary>
        /// (21/11) UN ESCALIER : seize marches de pierre (rien qu'une image) et, dessous, une RAMPE invisible
        /// (un BoxCollider penche) : le CharacterController monte une pente bien plus doucement que des marches.
        /// </summary>
        static void Stairs(Vector3 lo, Vector3 hi, Material stone)
        {
            Vector3 run = hi - lo;
            Vector3 flat = new Vector3(run.x, 0f, run.z);
            const int Steps = 16;
            Proto.BeginVisualOnly();
            for (int i = 0; i < Steps; i++)
            {
                float t = (i + 0.5f) / Steps;
                float stepTop = lo.y + run.y * (i + 1f) / Steps;
                float bottom = lo.y - 0.05f;
                Vector3 at = lo + flat * t;
                GameObject step = Proto.Cube(blocks, new Vector3(at.x, (stepTop + bottom) * 0.5f, at.z), Vector3.one, Color.gray, "Marche");
                step.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
                step.transform.localScale = new Vector3(3.5f, stepTop - bottom, flat.magnitude / Steps + 0.02f);
                if (stone != null) step.GetComponent<Renderer>().sharedMaterial = stone;
            }
            Proto.EndVisualOnly();
            BoxCollider ramp = new GameObject("Rampe").AddComponent<BoxCollider>();
            ramp.transform.SetParent(blocks, false);
            Quaternion along = Quaternion.LookRotation(run.normalized, Vector3.up);
            ramp.transform.rotation = along;
            ramp.transform.position = (lo + hi) * 0.5f - along * Vector3.up * 0.2f;
            ramp.size = new Vector3(3.6f, 0.4f, run.magnitude + 0.6f);
        }

        /// <summary>Ce qu'il y a dans les salles : l'autel et le vitrail, les armures vides, les braseros, la lune.</summary>
        static void Furnish()
        {
            float x0, z0, x1, z1;
            // La chapelle : la fenetre au nord, l'autel devant, deux braseros qui attendent l'aube.
            Bounds('K', out x0, out z0, out x1, out z1);
            float cx = (x0 + x1) * 0.5f;
            NightKit.Place("autel", cx, z1 - 2.5f, 0f, 0f, false);
            Block(new Vector3(cx, 0.55f, z1 - 2.5f), new Vector3(3.1f, 1.1f, 1.3f));
            NightKit.FireAt("Brasero_Feu", cx - 2.6f, z1 - 2.8f, 0f, false);
            NightKit.FireAt("Brasero_Feu", cx + 2.6f, z1 - 2.8f, 0f, false);
            NightKit.Place("fenetre", cx, z1, 180f, 0f, false);
            NightKit.Place("vitrail", cx, z1, 180f, 0f, false);
            NightKit.Moon(new Vector3(cx, 0f, z1));

            // La grande salle : une haie d'armures vides le long des murs, quatre braseros a allumer.
            Bounds('H', out x0, out z0, out x1, out z1);
            for (float x = x0 + 2f; x < x1; x += 4f)
            {
                Keep(NightKit.Place("chevalier", x, z1 - 0.75f, 180f, 0f, false));
                Keep(NightKit.Place("chevalier", x, z0 + 0.75f, 0f, 0f, false));
            }
            NightLight.Brazier(blocks, new Vector3(x0 + 3f, 0f, z0 + 3f));
            NightLight.Brazier(blocks, new Vector3(x1 - 3f, 0f, z1 - 3f));

            // L'armurerie : encore des armures.
            Bounds('A', out x0, out z0, out x1, out z1);
            for (float x = x0 + 1.2f; x < x1; x += 1.9f) Keep(NightKit.Place("chevalier", x, z1 - 0.8f, 180f, 0f, false));
            Furniture();

            // Le vestibule : deux torches allumees de part et d'autre de la porte (l'ouverture les soufflera).
            Vector3 door;
            float yaw;
            if (TryDoor('C', 'V', out door, out yaw))
            {
                NightKit.FireAt("Torche_Murale_Feu", door.x - 3f, door.z, 180f, true);
                NightKit.FireAt("Torche_Murale_Feu", door.x + 3f, door.z, 180f, true);
            }

            // La lune : elle n'eclaire que les cours, les plafonds font de l'ombre partout ailleurs.
            Light moon = new GameObject("Lune").AddComponent<Light>();
            MoonLight = moon;
            moon.transform.SetParent(blocks, false);
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.6f, 1f);
            moon.intensity = 0.55f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 1f;
            moon.transform.rotation = Quaternion.Euler(58f, 35f, 0f);
        }

        // ------------------------------------------------------------------ (21/11) les passages secrets

        /// <summary>Les portes cachees : derriere l'autel vers la crypte (KR), derriere un rayon de la bibliotheque vers le tresor (LT).</summary>
        static readonly string[] SecretKeys = { "KR", "LT", "QZ", "PZ", "LX", "DX", "IW" };
        /// <summary>(22/11) La salle que cache chaque passage (fermee aux monstres tant qu'aucun de ses passages n'est ouvert).</summary>
        static char Hidden(string key) { return key == "KR" ? 'R' : key == "LT" ? 'T' : key == "IW" ? 'I' : key[1]; }
        /// <summary>Combien de passages secrets sont ouverts.</summary>
        public static int SecretsOpen { get { return revealed.Count; } }
        public static int SecretsTotal { get { return SecretKeys.Length; } }
        static readonly Dictionary<string, Vector4> secrets = new Dictionary<string, Vector4>();     // x, z, yaw, direction
        static readonly HashSet<string> revealed = new HashSet<string>();

        class Cover
        {
            public Transform piece;
            public Collider wall;
            public Vector3 closed;
            public Vector3 open;
            public float k;
            public float target;
            public float speed;
        }
        static readonly Dictionary<string, Cover> covers = new Dictionary<string, Cover>();

        /// <summary>Les brasiers de la crypte (le boss) et l'endroit ou l'on arrive dans la crypte.</summary>
        public static readonly List<GameObject> CryptFires = new List<GameObject>();
        public static Vector3 CryptEntry;
        public static Vector3 AbbotHome;

        static bool IsSecret(string key) { return System.Array.IndexOf(SecretKeys, key) >= 0; }
        public static bool Revealed(string key) { return revealed.Contains(key); }

        static void BuildSecrets()
        {
            foreach (KeyValuePair<string, Vector4> s in secrets)
            {
                Vector4 v = s.Value;
                int d = (int)v.w;
                Vector3 door = new Vector3(v.x, 0f, v.y), dir = new Vector3(DX[d], 0f, DZ[d]);
                Cover cv = new Cover();
                GameObject piece;
                if (s.Key == "LT" || s.Key == "IW")
                {
                    // Un meuble comme les autres, colle devant l'arche, cote public. Il glissera de cote.
                    // (LT : une bibliotheque, un livre depasse ; IW : un ratelier de bouteilles, une bouteille vide.)
                    bool fromHigh = s.Key == "IW";      // pour IW, la case "c,r" est dans la cave cachee : le cote public est en face
                    Vector3 side = fromHigh ? dir : -dir;
                    float y = door.y + (s.Key == "IW" ? -4f : 0f);
                    Vector3 at = door + side * 0.5f;
                    float face = fromHigh ? (d == 0 ? 0f : d == 1 ? 90f : d == 2 ? 180f : -90f) : (d == 0 ? 180f : d == 1 ? -90f : d == 2 ? 0f : 90f);
                    piece = NightKit.Place(s.Key == "LT" ? "bibliotheque" : "ratelier", at.x, at.z, face, y, false);
                    cv.closed = new Vector3(at.x, y, at.z);
                    cv.open = cv.closed + new Vector3(DZ[d], 0f, DX[d]) * 2.05f;
                    cv.speed = 1f / 1.6f;
                    if (piece != null) NightQuest.AddSecret(piece.transform, s.Key, s.Key == "LT" ? 'L' : 'W');
                }
                else
                {
                    // Un pan de mur, pile dans l'arche. Il s'enfoncera dans le sol.
                    float y0 = FloorY(door - dir * 0.6f);
                    piece = NightKit.Place("mur_droit", door.x, door.z, v.z, y0, false);
                    cv.closed = new Vector3(door.x, y0, door.z);
                    cv.open = cv.closed + Vector3.down * 4.1f;
                    cv.speed = 1f / 4f;
                }
                if (piece == null) continue;
                cv.piece = piece.transform;
                BoxCollider box = new GameObject("Passage secret").AddComponent<BoxCollider>();
                box.transform.SetParent(blocks, false);
                box.transform.position = new Vector3(door.x, cv.closed.y + 2f, door.z);
                box.transform.rotation = Quaternion.Euler(0f, v.z, 0f);
                box.size = new Vector3(4.2f, 4f, 0.6f);
                cv.wall = box;
                covers[s.Key] = cv;
                Lock(Hidden(s.Key));
            }
        }

        /// <summary>Le passage s'ouvre (le mur s'enfonce, la bibliotheque glisse) ; la salle cachee apparait sur la carte.</summary>
        public static void Reveal(string key)
        {
            Cover cv;
            if (!covers.TryGetValue(key, out cv)) return;
            revealed.Add(key);
            cv.target = 1f;
            Unlock(Hidden(key));
        }

        /// <summary>Le passage se referme (le mur remonte) : on est enferme.</summary>
        public static void Seal(string key)
        {
            Cover cv;
            if (!covers.TryGetValue(key, out cv)) return;
            cv.target = 0f;
            cv.speed = 1f / 1.2f;
            if (cv.wall != null) cv.wall.enabled = true;
        }

        public static void Tick(float dt, Vector3 player)
        {
            foreach (Cover cv in covers.Values)
            {
                if (Mathf.Approximately(cv.k, cv.target) || cv.piece == null) continue;
                cv.k = Mathf.MoveTowards(cv.k, cv.target, dt * cv.speed);
                float e = cv.k * cv.k * (3f - 2f * cv.k);
                Vector3 p = Vector3.Lerp(cv.closed, cv.open, e);
                cv.piece.position = new Vector3(p.x, p.y + Mathf.Sin(cv.k * 40f) * 0.01f * (1f - cv.k) * cv.k * 4f, p.z);
                if (cv.k >= 1f && cv.wall != null) cv.wall.enabled = false;
            }
            Visit(player);
        }

        // ------------------------------------------------------------------ (21/11) la carte du carnet, et la petite carte

        static bool[,] seen;
        static int lastC = -1, lastR = -1;
        static readonly Dictionary<char, string> Names = new Dictionary<char, string>
        {
            { 'V', "Vestibule" }, { 'S', "Sacristie" }, { 'N', "Niche" }, { 'H', "Grande salle" }, { 'K', "Chapelle" },
            { 'Y', "Préau" }, { 'L', "Bibliothèque" }, { 'D', "Dortoir" }, { 'A', "Armurerie" }, { 'P', "Cachots" },
            { 'Q', "Cuisines" }, { 'R', "Crypte" }, { 'T', "Trésor" }, { 'J', "Grande cour" }, { 'B', "Écuries" },
            { 'G', "Donjon" }, { 'U', "Donjon, étage" }, { 'W', "Caves" }, { 'O', "Ossuaire" },
            { 'Z', "Garde-manger secret" }, { 'X', "Passage dérobé" }, { 'I', "Cave cachée" }
        };

        /// <summary>Le nom d'une salle (pour l'ecran), ou "".</summary>
        public static string NameOf(char room) { string n; return Names.TryGetValue(room, out n) ? n : ""; }

        /// <summary>Le joueur est ici : la salle entiere apparait sur la carte (dans un couloir, les cases autour de lui).</summary>
        static void Visit(Vector3 p)
        {
            if (grid == null) return;
            if (seen == null || seen.GetLength(0) != wide || seen.GetLength(1) != deep) seen = new bool[wide, deep];
            int c0 = Col(p), r0 = Row(p);
            if (c0 == lastC && r0 == lastR) return;
            lastC = c0; lastR = r0;
            char room = Room(c0, r0);
            if (room == '.') return;
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    if (Room(c, r) != room) continue;
                    if (room == 'C' && (Mathf.Abs(c - c0) > 2 || Mathf.Abs(r - r0) > 2)) continue;
                    seen[c, r] = true;
                }
        }

        /// <summary>Le carnet : tout le chateau deja vu, a la taille de la page.</summary>
        public static void DrawMap(Rect area, Vector3 player, float yaw, GUIStyle label)
        {
            if (grid == null || seen == null) return;
            float s = Mathf.Floor(Mathf.Min(area.width / wide, area.height / deep));
            float ox = Mathf.Round(area.x + (area.width - s * wide) * 0.5f), oy = Mathf.Round(area.y + (area.height - s * deep) * 0.5f);
            Paint(s, ox, oy, player, yaw, label, true);
        }

        /// <summary>
        /// (21/11) LA PETITE CARTE, toujours a l'ecran (Martin : "une carte qui est toujours la, en bas") : les cases autour
        /// de toi, toi au milieu. Concept Unity : GUI.BeginGroup coupe tout ce qui depasse du rectangle.
        /// </summary>
        public static void DrawMini(Rect area, Vector3 player, float yaw, GUIStyle label, float cellsAcross)
        {
            if (grid == null || seen == null) return;
            float s = Mathf.Round(area.width / cellsAcross);
            GUI.BeginGroup(area);
            float ox = Mathf.Round(area.width * 0.5f - player.x / Cell * s), oy = Mathf.Round(area.height * 0.5f - (deep - player.z / Cell) * s);
            Paint(s, ox, oy, player, yaw, label, false);
            GUI.EndGroup();
        }

        static void Paint(float s, float ox, float oy, Vector3 player, float yaw, GUIStyle label, bool names)
        {
            float t = Mathf.Max(2f, Mathf.Round(s * 0.09f));
            Color ink = new Color(0.2f, 0.11f, 0.05f, 0.9f), wash = new Color(0.45f, 0.3f, 0.15f, 0.22f), red = new Color(0.6f, 0.05f, 0.03f, 0.9f);
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    if (!seen[c, r]) continue;
                    char room = Room(c, r);
                    Rect cell = new Rect(ox + c * s, oy + (deep - 1 - r) * s, s, s);
                    float b = Base(room);
                    // Les caves plus sombres, l'etage plus clair, les escaliers hachures.
                    GUI.color = room == 'C' ? new Color(wash.r, wash.g, wash.b, 0.12f) : room == 'R' ? new Color(0.35f, 0.05f, 0.02f, 0.25f)
                        : b < 0f ? new Color(0.15f, 0.1f, 0.06f, 0.35f) : b > 0f ? new Color(0.6f, 0.45f, 0.2f, 0.3f) : wash;
                    GUI.DrawTexture(cell, Texture2D.whiteTexture);
                    if (Stair(room))
                    {
                        GUI.color = ink;
                        for (int k = 1; k < 4; k++) GUI.DrawTexture(new Rect(cell.x + s * 0.2f, cell.y + k * s * 0.25f, s * 0.6f, Mathf.Max(1f, t * 0.5f)), Texture2D.whiteTexture);
                    }
                    for (int d = 0; d < 4; d++)
                    {
                        char other = Room(c + DX[d], r + DZ[d]);
                        if (other == room) continue;
                        Rect line = d == 0 ? new Rect(cell.x, cell.y, s, t) : d == 1 ? new Rect(cell.xMax - t, cell.y, t, s)
                            : d == 2 ? new Rect(cell.x, cell.yMax - t, s, t) : new Rect(cell.x, cell.y, t, s);
                        string key = room < other ? "" + room + other : "" + other + room;
                        bool door = Door(c, r, d) && (!IsSecret(key) || revealed.Contains(key));
                        GUI.color = ink;
                        if (!door) { GUI.DrawTexture(line, Texture2D.whiteTexture); continue; }
                        // Une porte : deux bouts de mur et un trou ; en rouge si elle est fermee a cle.
                        float gap = s * 0.5f, side = (s - gap) * 0.5f;
                        if (d % 2 == 0)
                        {
                            GUI.DrawTexture(new Rect(line.x, line.y, side, t), Texture2D.whiteTexture);
                            GUI.DrawTexture(new Rect(line.xMax - side, line.y, side, t), Texture2D.whiteTexture);
                            if (locked.Contains(room) || locked.Contains(other)) { GUI.color = red; GUI.DrawTexture(new Rect(line.x + side, line.y, gap, t), Texture2D.whiteTexture); }
                        }
                        else
                        {
                            GUI.DrawTexture(new Rect(line.x, line.y, t, side), Texture2D.whiteTexture);
                            GUI.DrawTexture(new Rect(line.x, line.yMax - side, t, side), Texture2D.whiteTexture);
                            if (locked.Contains(room) || locked.Contains(other)) { GUI.color = red; GUI.DrawTexture(new Rect(line.x, line.y + side, t, gap), Texture2D.whiteTexture); }
                        }
                    }
                }
            // Les feux qui brulent : de petits points d'or (la ou les monstres ne passent pas).
            float dot = Mathf.Max(3f, Mathf.Round(s * 0.16f));
            foreach (NightWard w in NightWard.All)
            {
                if (w == null || !w.Burning) continue;
                int wc = Col(w.Center), wr = Row(w.Center);
                if (wc < 0 || wr < 0 || wc >= wide || wr >= deep || !seen[wc, wr]) continue;
                GUI.color = new Color(1f, 0.7f, 0.25f, 0.95f);
                GUI.DrawTexture(new Rect(ox + w.Center.x / Cell * s - dot * 0.5f, oy + (deep - w.Center.z / Cell) * s - dot * 0.5f, dot, dot), Texture2D.whiteTexture);
            }
            // Le nom des salles deja vues.
            if (names)
            {
                GUIStyle ls = new GUIStyle(label);
                ls.fontSize = Mathf.Max(9, Mathf.RoundToInt(s * 0.4f));
                foreach (KeyValuePair<char, string> n in Names)
                {
                    float minC = 999f, maxC = -1f, minR = 999f, maxR = -1f;
                    bool any = false;
                    for (int c = 0; c < wide; c++)
                        for (int r = 0; r < deep; r++)
                            if (seen[c, r] && Room(c, r) == n.Key) { any = true; minC = Mathf.Min(minC, c); maxC = Mathf.Max(maxC, c); minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r); }
                    if (!any) continue;
                    float cx = ox + (minC + maxC + 1f) * 0.5f * s, cy = oy + (deep - (minR + maxR + 1f) * 0.5f) * s;
                    GUI.color = Color.white;
                    GUI.Label(new Rect(cx - s * 3f, cy - s * 0.4f, s * 6f, s * 0.8f), n.Value, ls);
                }
            }
            // Toi : une fleche rouge qui regarde ou tu regardes.
            Vector2 me = new Vector2(ox + player.x / Cell * s, oy + (deep - player.z / Cell) * s);
            Matrix4x4 keep = GUI.matrix;
            GUIUtility.RotateAroundPivot(yaw, me);
            GUI.color = red;
            float a = Mathf.Max(6f, s * 0.35f);
            GUI.DrawTexture(new Rect(me.x - a * 0.18f, me.y - a, a * 0.36f, a * 1.1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(me.x - a * 0.4f, me.y - a * 0.1f, a * 0.8f, a * 0.36f), Texture2D.whiteTexture);
            GUI.matrix = keep;
            GUI.color = Color.white;
        }

        // ------------------------------------------------------------------ (21/11) les meubles

        static bool NearDoor(Vector3 p, float r)
        {
            foreach (Vector4 d in doors.Values) if (new Vector2(d.x - p.x, d.y - p.z).magnitude < r) return true;
            return false;
        }

        static bool Taken(Vector3 p, float r)
        {
            foreach (Vector3 t in taken) if (new Vector2(t.x - p.x, t.z - p.z).magnitude < r) return true;
            return false;
        }

        static void SolidBox(Vector3 p, float yaw, Vector3 size)
        {
            Box(p + Vector3.up * size.y * 0.5f, size, yaw, "Meuble");
        }

        /// <summary>Pose un meuble au sol (a la hauteur du sol de sa salle), avec son collider (largeur, hauteur, profondeur).</summary>
        static GameObject Put(string piece, float x, float z, float yaw, Vector3 size)
        {
            Vector3 p = new Vector3(x, 0f, z);
            p.y = FloorY(p);
            GameObject go = NightKit.Place(piece, x, z, yaw, p.y, false);
            if (go == null) return null;
            taken.Add(p);
            SolidBox(p, yaw, size);
            return go;
        }

        /// <summary>
        /// Pose "piece" dos aux murs de la salle, face a la piece. sides : 1 nord, 2 est, 4 sud, 8 ouest.
        /// every : un emplacement de mur sur "every". Jamais devant une porte, jamais sur un autre meuble.
        /// </summary>
        static List<GameObject> AlongWalls(char room, string piece, Vector3 size, int sides, int every, int salt)
        {
            return AlongWalls(room, piece, size, sides, every, salt, 999);
        }

        static List<GameObject> AlongWalls(char room, string piece, Vector3 size, int sides, int every, int salt, int most)
        {
            List<GameObject> list = new List<GameObject>();
            int n = 0;
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    if (Room(c, r) != room) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        if (list.Count >= most) return list;
                        if ((sides & (1 << d)) == 0 || Room(c + DX[d], r + DZ[d]) == room || Door(c, r, d)) continue;
                        n++;
                        if ((n + salt) % every != 0) continue;
                        Vector3 p = CellCenter(c, r) + new Vector3(DX[d], 0f, DZ[d]) * (2f - 0.27f - size.z * 0.5f);
                        if (NearDoor(p, 2f) || Taken(p, 1.3f)) continue;
                        float yaw = d == 0 ? 180f : d == 1 ? -90f : d == 2 ? 0f : 90f;
                        GameObject go = Put(piece, p.x, p.z, yaw, size);
                        if (go != null) list.Add(go);
                    }
                }
            return list;
        }

        /// <summary>
        /// Un coffre qu'on peut fouiller : son couvercle tourne sur sa charniere. (21/11, Martin : "il y a trop de coffres,
        /// ils sont degueulasses, on n'a rien dedans" : il n'y en a plus qu'un ou deux par salle, et CHACUN a quelque chose.
        /// Une lueur d'or s'en echappe par la fente : on sait qu'il vaut le detour.)
        /// </summary>
        static void Chest(GameObject body, char room)
        {
            if (body == null) return;
            GameObject lid = NightKit.Place("coffre_couvercle", 0f, 0f, 0f, 0f, false);
            if (lid != null)
            {
                lid.transform.SetParent(body.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.42f, -0.26f);
                lid.transform.localRotation = Quaternion.identity;
            }
            NightQuest.Add(body.transform, 0, lid != null ? lid.transform : null, room);
        }

        /// <summary>(22/11) La tombe de la cour qui cache un escalier vers l'ossuaire.</summary>
        public static Transform TrapTomb;

        /// <summary>Un tombeau qu'on peut ouvrir : sa dalle glisse.</summary>
        static GameObject Tomb(float x, float z, float yaw, char room)
        {
            GameObject body = Put("tombeau", x, z, yaw, new Vector3(2.4f, 0.95f, 1.2f));
            if (body == null) return null;
            GameObject lid = NightKit.Place("tombeau_couvercle", 0f, 0f, 0f, 0f, false);
            if (lid != null)
            {
                lid.transform.SetParent(body.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.80f, 0f);
                lid.transform.localRotation = Quaternion.identity;
            }
            NightQuest.Add(body.transform, 1, lid != null ? lid.transform : null, room);
            return body;
        }

        static readonly Vector3 ChestSize = new Vector3(0.95f, 0.62f, 0.56f);
        static readonly Vector3 TableSize = new Vector3(2.45f, 0.86f, 0.92f);
        static readonly Vector3 BenchSize = new Vector3(2.0f, 0.46f, 0.36f);
        static readonly Vector3 BedSize = new Vector3(2.15f, 0.6f, 0.98f);

        /// <summary>Une longue table et ses deux bancs (le long de x).</summary>
        static void Feast(float x, float z)
        {
            Put("table", x, z, 0f, TableSize);
            Put("banc", x, z - 0.85f, 0f, BenchSize);
            Put("banc", x, z + 0.85f, 180f, BenchSize);
        }

        /// <summary>Une grande cheminee qui brule toujours : on y ravive sa torche.</summary>
        static void Hearth(float x, float z, float yaw)
        {
            Vector3 p = new Vector3(x, 0f, z);
            p.y = FloorY(p);
            NightKit.FireAtY("Cheminee_Feu", x, z, yaw, true, p.y);
            NightKit.KeepLit();
            SolidBox(p, yaw, new Vector3(2.7f, 2f, 0.95f));
            taken.Add(p);
            Hearths.Add(p + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 1.2f);
        }

        /// <summary>Une lumiere sans source (le plein air, l'or du tresor).</summary>
        static void Glow(Vector3 at, Color color, float range, float intensity, string name)
        {
            Light l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(blocks, false);
            l.transform.position = at;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
        }

        /// <summary>Plus une seule salle vide : chaque piece a ses meubles, et ses cachettes a fouiller.</summary>
        static void Furniture()
        {
            float x0, z0, x1, z1;
            // La grande salle : le festin abandonne (deux longues tables), et une grande cheminee ou le feu brule encore.
            Bounds('H', out x0, out z0, out x1, out z1);
            float zc = (z0 + z1) * 0.5f;
            for (float x = x0 + 6f; x < x1 - 3f; x += 6f) { Feast(x, zc - 3f); Feast(x, zc + 3f); }
            Hearth(x0 + 0.72f, zc, 90f);
            foreach (GameObject c in AlongWalls('H', "coffre", ChestSize, 2, 2, 0, 1)) Chest(c, 'H');

            // La bibliotheque : des rayonnages sur tous les murs, deux tables de lecture ; UN rayon cache quelque chose.
            Bounds('L', out x0, out z0, out x1, out z1);
            int shelf = 0;
            foreach (GameObject b in AlongWalls('L', "bibliotheque", new Vector3(1.95f, 2.6f, 0.46f), 15, 1, 0))
                if (shelf++ == 4) NightQuest.Add(b.transform, 2, null, 'L');
            Feast((x0 + x1) * 0.5f - 3f, (z0 + z1) * 0.5f);
            Feast((x0 + x1) * 0.5f + 3f, (z0 + z1) * 0.5f);

            // Le dortoir : des lits contre les murs, un coffre au fond.
            AlongWalls('D', "lit", BedSize, 10, 1, 0);
            foreach (GameObject c in AlongWalls('D', "coffre", ChestSize, 1, 1, 0, 1)) Chest(c, 'D');

            // L'armurerie : des rateliers d'armes, un coffre.
            AlongWalls('A', "ratelier", new Vector3(1.75f, 2.2f, 0.5f), 4, 1, 0);
            foreach (GameObject c in AlongWalls('A', "coffre", ChestSize, 8, 1, 0, 1)) Chest(c, 'A');

            // Le preau : deux tombes sous la lune.
            Bounds('Y', out x0, out z0, out x1, out z1);
            Tomb(x0 + 2.5f, (z0 + z1) * 0.5f, 90f, 'Y');
            Tomb(x0 + 5.5f, (z0 + z1) * 0.5f, 90f, 'Y');

            // La chapelle : deux gisants le long des murs, des bancs devant l'autel.
            Bounds('K', out x0, out z0, out x1, out z1);
            float kc = (x0 + x1) * 0.5f, kz = (z0 + z1) * 0.5f;
            Tomb(x0 + 1.15f, kz, 90f, 'K');
            Tomb(x1 - 1.15f, kz, 90f, 'K');
            for (int i = 0; i < 2; i++)
                for (int s = -1; s <= 1; s += 2) Put("banc", kc + s * 2f, z0 + 2.9f + i * 1.7f, 180f, BenchSize);

            // Le vestibule : des bancs pour ceux qui attendaient. La sacristie (fermee a cle) : un coffre.
            AlongWalls('V', "banc", BenchSize, 2, 1, 0);
            foreach (GameObject c in AlongWalls('S', "coffre", ChestSize, 1, 2, 0, 1)) Chest(c, 'S');

            // Les cachots : des paillasses, un coffre ou l'on jetait ce qu'on prenait aux prisonniers.
            AlongWalls('P', "lit", BedSize, 15, 2, 1);
            foreach (GameObject c in AlongWalls('P', "coffre", ChestSize, 15, 3, 0, 1)) Chest(c, 'P');

            // Les cuisines : une grande cheminee qui brule encore, la table des cuisiniers.
            Bounds('Q', out x0, out z0, out x1, out z1);
            float qz = (z0 + z1) * 0.5f;
            Hearth(x1 - 0.72f, qz + 2f, -90f);
            Feast((x0 + x1) * 0.5f - 1.5f, qz);

            // Le tresor : deux coffres au fond, sous une lueur d'or.
            foreach (GameObject c in AlongWalls('T', "coffre", ChestSize, 1, 1, 0, 2)) Chest(c, 'T');
            Bounds('T', out x0, out z0, out x1, out z1);
            Glow(new Vector3((x0 + x1) * 0.5f, 2.6f, (z0 + z1) * 0.5f), new Color(1f, 0.75f, 0.35f), 6f, 0.8f, "Lueur du trésor");

            BuildCrypt();
            BuildYard();
            // (22/11) Les salles cachees : chacune a son coffre.
            foreach (GameObject c in AlongWalls('Z', "coffre", ChestSize, 1, 2, 0, 1)) Chest(c, 'Z');
            foreach (GameObject c in AlongWalls('X', "coffre", ChestSize, 4, 1, 0, 1)) Chest(c, 'X');
            foreach (GameObject c in AlongWalls('I', "coffre", ChestSize, 1, 1, 0, 1)) Chest(c, 'I');
            AlongWalls('I', "ratelier", new Vector3(1.75f, 2.2f, 0.5f), 8, 1, 0, 1);
            BuildStables();
            BuildKeep();
            BuildCellars();
        }

        static void BuildCrypt()
        {
            float x0, z0, x1, z1;
            // La crypte : deux gisants au milieu, quatre piliers, quatre brasiers eteints aux coins (le combat).
            Bounds('R', out x0, out z0, out x1, out z1);
            float rx = (x0 + x1) * 0.5f, rz = (z0 + z1) * 0.5f;
            Tomb(rx, rz - 2.6f, 0f, 'R');
            Tomb(rx, rz + 2.6f, 0f, 'R');
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                {
                    Vector3 p = new Vector3(rx + i * 4f, 0f, rz + j * 4f);
                    NightKit.Place("pilier", p.x, p.z, 0f, 0f, false);
                    NightKit.Place("pilier", p.x, p.z, 0f, 4f, false);
                    SolidBox(p, 0f, new Vector3(1f, 8f, 1f));
                }
            Vector3[] fires = { new Vector3(x0 + 1.6f, 0f, rz), new Vector3(x0 + 1.6f, 0f, z1 - 1.6f), new Vector3(x1 - 1.6f, 0f, z1 - 1.6f), new Vector3(x1 - 1.6f, 0f, z0 + 1.6f) };
            foreach (Vector3 f in fires)
            {
                NightKit.FireAt("Brasero_Feu", f.x, f.z, 0f, false);
                if (NightKit.LastFire != null) CryptFires.Add(NightKit.LastFire);
                SolidBox(f, 0f, new Vector3(0.8f, 1.2f, 0.8f));
            }
            CryptEntry = new Vector3(x0 + 2f, 0f, z0 + 1.6f);
            AbbotHome = new Vector3(x1 - 3f, 0f, z1 - 4f);
            Glow(new Vector3(rx, 6f, rz), new Color(0.4f, 0.5f, 0.9f), 14f, 0.35f, "Froid de la crypte");
        }

        /// <summary>(21/11) LA GRANDE COUR : a ciel ouvert, des arbres morts, un puits, de vieilles tombes, deux braseros a allumer.</summary>
        static void BuildYard()
        {
            float x0, z0, x1, z1;
            Bounds('J', out x0, out z0, out x1, out z1);
            float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;
            Vector3[] trees = { new Vector3(x0 + 4f, 0f, z1 - 3f), new Vector3(x1 - 4f, 0f, z0 + 8f), new Vector3(x0 + 12f, 0f, z1 - 6f) };
            for (int i = 0; i < trees.Length; i++)
            {
                GameObject tree = NightKit.Place("arbre_mort", trees[i].x, trees[i].z, i * 97f, 0f, false);
                if (tree != null) tree.transform.localScale = Vector3.one * (1.1f + 0.25f * i);
                SolidBox(trees[i], 0f, new Vector3(0.7f, 3f, 0.7f));
                taken.Add(trees[i]);
            }
            // Le puits : un anneau de pierres, une margelle, deux montants et sa poulie.
            Material stone = NightKit.Mat("Pierre");
            Material iron = NightKit.Mat("Fer");
            Vector3 well = new Vector3(cx + 1f, 0f, cz + 1f);
            Proto.BeginVisualOnly();
            for (int k = 0; k < 14; k++)
            {
                float a = k / 14f * Mathf.PI * 2f;
                GameObject block = Proto.Cube(blocks, well + new Vector3(Mathf.Cos(a) * 1.05f, 0.42f, Mathf.Sin(a) * 1.05f), new Vector3(0.55f, 0.84f, 0.32f), Color.gray, "Puits");
                block.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                if (stone != null) block.GetComponent<Renderer>().sharedMaterial = stone;
            }
            GameObject hole = Proto.Cylinder(blocks, well + Vector3.up * 0.5f, new Vector3(1.85f, 0.02f, 1.85f), new Color(0.01f, 0.01f, 0.012f), "Le noir du puits");
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject post = Proto.Cube(blocks, well + new Vector3(s * 1.05f, 1.35f, 0f), new Vector3(0.16f, 2.7f, 0.16f), new Color(0.2f, 0.13f, 0.08f), "Montant");
                post.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(new Color(0.2f, 0.13f, 0.08f), 0.1f, 0f);
            }
            GameObject beam = Proto.Cylinder(blocks, well + new Vector3(0f, 2.55f, 0f), new Vector3(0.12f, 1.15f, 0.12f), Color.gray, "Treuil");
            beam.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            if (iron != null) beam.GetComponent<Renderer>().sharedMaterial = iron;
            Proto.EndVisualOnly();
            hole.name = "Puits";
            SolidBox(well, 0f, new Vector3(2.5f, 1f, 2.5f));
            taken.Add(well);
            // Le cimetiere des Veilleurs, contre le mur sud (aucune porte de ce cote).
            for (int i = 0; i < 3; i++)
            {
                GameObject t = Tomb(x0 + 6f + i * 6f, z0 + 1.4f, 0f, 'J');
                if (i == 1 && t != null) TrapTomb = t.transform;      // (22/11) la tombe creuse (NightSecrets)
            }
            NightLight.Brazier(blocks, new Vector3(cx - 4f, 0f, cz - 4f));
            NightLight.Brazier(blocks, new Vector3(cx + 5f, 0f, z1 - 3f));
        }

        /// <summary>(21/11) LES ECURIES : des stalles de bois, de la paille, le vieux cheval qui tremble, un coffre.</summary>
        static void BuildStables()
        {
            float x0, z0, x1, z1;
            Bounds('B', out x0, out z0, out x1, out z1);
            Material wood = MaterialFactory.GetShiny(new Color(0.22f, 0.14f, 0.08f), 0.12f, 0f);
            Material straw = MaterialFactory.GetShiny(new Color(0.42f, 0.33f, 0.16f), 0.05f, 0f);
            Proto.BeginVisualOnly();
            // Six stalles le long du mur nord : une cloison tous les 4 m, une mangeoire.
            for (float x = x0; x <= x1 + 0.01f; x += 4f)
            {
                if (x <= x0 + 0.1f || x >= x1 - 0.1f) continue;
                GameObject wall = Proto.Cube(blocks, new Vector3(x, 0.8f, z1 - 1.6f), new Vector3(0.14f, 1.6f, 3.2f), Color.gray, "Cloison");
                wall.GetComponent<Renderer>().sharedMaterial = wood;
                Box(new Vector3(x, 0.8f, z1 - 1.6f), new Vector3(0.14f, 1.6f, 3.2f), 0f, "Cloison");
            }
            for (float x = x0 + 2f; x < x1; x += 4f)
            {
                GameObject trough = Proto.Cube(blocks, new Vector3(x, 0.45f, z1 - 0.45f), new Vector3(2.8f, 0.5f, 0.5f), Color.gray, "Mangeoire");
                trough.GetComponent<Renderer>().sharedMaterial = wood;
                for (int k = 0; k < 3; k++)
                {
                    GameObject hay = Proto.Cube(blocks, new Vector3(x - 1f + k * 0.9f + Random.Range(-0.2f, 0.2f), 0.12f, z1 - 1.8f + Random.Range(-0.6f, 0.6f)),
                        new Vector3(Random.Range(0.8f, 1.3f), Random.Range(0.12f, 0.3f), Random.Range(0.7f, 1.1f)), Color.gray, "Paille");
                    hay.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    hay.GetComponent<Renderer>().sharedMaterial = straw;
                }
            }
            // Des bottes de paille empilees dans un coin.
            for (int k = 0; k < 5; k++)
            {
                GameObject bale = Proto.Cube(blocks, new Vector3(x0 + 1.2f + (k % 3) * 1.15f, 0.35f + (k / 3) * 0.7f, z0 + 1f), new Vector3(1.1f, 0.7f, 0.75f), Color.gray, "Botte");
                bale.GetComponent<Renderer>().sharedMaterial = straw;
            }
            Proto.EndVisualOnly();
            Box(new Vector3(x0 + 2.35f, 0.7f, z0 + 1f), new Vector3(3.5f, 1.4f, 0.8f), 0f, "Bottes");
            taken.Add(new Vector3(x0 + 2.35f, 0f, z0 + 1f));
            GameObject horse = NightKit.Place("cheval", x1 - 6f, z1 - 1.7f, 180f, 0f, false);
            if (horse != null) Box(new Vector3(x1 - 6f, 1f, z1 - 1.7f), new Vector3(0.8f, 2f, 2.4f), 0f, "Cheval");
            StableHorse = horse != null ? horse.transform : null;
            // Une lanterne oubliee, accrochee a une poutre : on devine les stalles, le cheval, la paille.
            Glow(new Vector3(x1 - 7.5f, 3.1f, z1 - 3.2f), new Color(1f, 0.66f, 0.35f), 11f, 0.75f, "Lanterne des écuries");
            foreach (GameObject c in AlongWalls('B', "coffre", ChestSize, 4, 3, 1, 1)) Chest(c, 'B');
        }

        /// <summary>Le cheval des ecuries (il tremble quand un monstre approche).</summary>
        public static Transform StableHorse;

        /// <summary>(21/11) LE DONJON : en bas, la salle des gardes et sa cheminee ; un escalier ; en haut, la chambre du novice.</summary>
        static void BuildKeep()
        {
            float x0, z0, x1, z1;
            Bounds('G', out x0, out z0, out x1, out z1);
            Hearth(x1 - 0.72f, (z0 + z1) * 0.5f - 2f, -90f);
            Feast((x0 + x1) * 0.5f - 1f, z0 + 4f);
            AlongWalls('G', "ratelier", new Vector3(1.75f, 2.2f, 0.5f), 8, 2, 0);
            for (float z = z0 + 2f; z < z1 - 3f; z += 4f) Keep(NightKit.Place("chevalier", x0 + 0.8f, z, 90f, 0f, false));

            // L'etage : des lits, des livres, une table, une fenetre sur la nuit, un coffre.
            Bounds('U', out x0, out z0, out x1, out z1);
            AlongWalls('U', "lit", BedSize, 1, 2, 0);
            AlongWalls('U', "bibliotheque", new Vector3(1.95f, 2.6f, 0.46f), 8, 2, 1);
            foreach (GameObject c in AlongWalls('U', "coffre", ChestSize, 2, 2, 0, 1)) Chest(c, 'U');
            Put("table", (x0 + x1) * 0.5f, (z0 + z1) * 0.5f, 90f, new Vector3(0.92f, 0.86f, 2.45f));
            NightKit.Place("fenetre", x1, (z0 + z1) * 0.5f + 2f, -90f, 4f, false);
            Glow(new Vector3(x1 - 1.2f, 6.2f, (z0 + z1) * 0.5f + 2f), new Color(0.45f, 0.6f, 1f), 7f, 0.7f, "Lune par la fenêtre");
        }

        /// <summary>(21/11) LE SOUS-SOL : les caves (des rateliers de bouteilles, des tables) et l'ossuaire (des tombeaux, la Bete).</summary>
        static void BuildCellars()
        {
            float x0, z0, x1, z1;
            Bounds('W', out x0, out z0, out x1, out z1);
            AlongWalls('W', "ratelier", new Vector3(1.75f, 2.2f, 0.5f), 9, 1, 0);
            Put("table", (x0 + x1) * 0.5f, (z0 + z1) * 0.5f - 1f, 0f, TableSize);
            foreach (GameObject c in AlongWalls('W', "coffre", ChestSize, 8, 2, 1, 1)) Chest(c, 'W');
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 p = new Vector3((x0 + x1) * 0.5f + i * 4f, -4f, (z0 + z1) * 0.5f + 2f);
                NightKit.Place("pilier", p.x, p.z, 0f, -4f, false);
                SolidBox(p, 0f, new Vector3(0.6f, 4f, 0.6f));
                taken.Add(p);
            }

            Bounds('O', out x0, out z0, out x1, out z1);
            float ox = (x0 + x1) * 0.5f, oz = (z0 + z1) * 0.5f;
            Tomb(ox - 3f, oz + 3f, 0f, 'O');
            Tomb(ox + 3f, oz + 3f, 0f, 'O');
            Tomb(ox, oz - 3.5f, 90f, 'O');
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 p = new Vector3(ox + i * 4f, -4f, oz);
                NightKit.Place("pilier", p.x, p.z, 0f, -4f, false);
                SolidBox(p, 0f, new Vector3(0.6f, 4f, 0.6f));
                taken.Add(p);
            }
            Glow(new Vector3(ox, -1.2f, oz), new Color(0.35f, 0.55f, 0.4f), 9f, 0.25f, "Phosphore de l'ossuaire");
            BeastLair = new Vector3(ox, -4f, oz);
        }

        /// <summary>L'antre de la Bete (le milieu de l'ossuaire).</summary>
        public static Vector3 BeastLair;

        static void Block(Vector3 at, Vector3 size)
        {
            Box(at, size, 0f, "Meuble");
        }

        // ------------------------------------------------------------------ se reperer

        /// <summary>Le rectangle qu'occupe une salle (en metres).</summary>
        public static void Bounds(char room, out float x0, out float z0, out float x1, out float z1)
        {
            x0 = z0 = float.MaxValue; x1 = z1 = float.MinValue;
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                    if (Room(c, r) == room)
                    {
                        x0 = Mathf.Min(x0, c * Cell); x1 = Mathf.Max(x1, c * Cell + Cell);
                        z0 = Mathf.Min(z0, r * Cell); z1 = Mathf.Max(z1, r * Cell + Cell);
                    }
        }

        /// <summary>Le milieu d'une salle, a la hauteur de son sol.</summary>
        public static Vector3 Center(char room)
        {
            float x0, z0, x1, z1;
            Bounds(room, out x0, out z0, out x1, out z1);
            return new Vector3((x0 + x1) * 0.5f, Base(room), (z0 + z1) * 0.5f);
        }

        /// <summary>Ou est la porte entre ces deux salles, et dans quel sens est son mur (0 : est-ouest, 90 : nord-sud) ?</summary>
        public static bool TryDoor(char a, char b, out Vector3 at, out float yaw)
        {
            Vector4 d;
            bool found = doors.TryGetValue(a < b ? "" + a + b : "" + b + a, out d);
            at = new Vector3(d.x, d.w, d.y);
            yaw = d.z;
            return found;
        }

        /// <summary>Une salle fermee a cle : les monstres n'y entrent pas.</summary>
        public static void Lock(char room) { locked.Add(room); }
        public static void Unlock(char room) { locked.Remove(room); }

        static bool Passable(int c, int r, int d)
        {
            char a = Room(c, r), b = Room(c + DX[d], r + DZ[d]);
            if (b == '.') return false;
            if (a != b && !(Door(c, r, d) && !locked.Contains(a) && !locked.Contains(b))) return false;
            if (a != b)
            {
                string key = a < b ? "" + a + b : "" + b + a;
                if (IsSecret(key) && !revealed.Contains(key)) return false;
            }
            // (21/11) Une case ou brule une torche, des bougies : les monstres n'y passent pas.
            return !NightWard.Blocks(CellCenter(c + DX[d], r + DZ[d]));
        }

        /// <summary>
        /// Pour aller de "from" a "goal" : tout droit si rien ne barre, sinon vers le milieu de la case
        /// suivante sur le plus court chemin (une recherche en largeur sur les cases). "ok" : un chemin existe.
        /// </summary>
        public static Vector3 Toward(Vector3 from, Vector3 goal, out bool ok)
        {
            ok = true;
            Vector3 up = Vector3.up * 1.4f;
            int c0 = Col(from), r0 = Row(from), c1 = Col(goal), r1 = Row(goal);
            bool sameLevel = Mathf.Abs(FloorY(from) - FloorY(goal)) < 0.5f;
            if (sameLevel && !Physics.Linecast(from + up, goal + up, Solid, QueryTriggerInteraction.Ignore)) return goal;
            if (At(c0, r0) == '.' || At(c1, r1) == '.') { ok = false; return from; }
            if (c0 == c1 && r0 == r1) return goal;
            int[] prev = new int[wide * deep];
            for (int i = 0; i < prev.Length; i++) prev[i] = -1;
            Queue<int> open = new Queue<int>();
            int start = c0 + r0 * wide, end = c1 + r1 * wide;
            open.Enqueue(start);
            prev[start] = start;
            while (open.Count > 0)
            {
                int n = open.Dequeue();
                if (n == end) break;
                int c = n % wide, r = n / wide;
                for (int d = 0; d < 4; d++)
                {
                    if (!Passable(c, r, d)) continue;
                    int m = (c + DX[d]) + (r + DZ[d]) * wide;
                    if (prev[m] >= 0) continue;
                    prev[m] = n;
                    open.Enqueue(m);
                }
            }
            if (prev[end] < 0) { ok = false; return from; }
            int hop = end;
            while (prev[hop] != start) hop = prev[hop];
            // D'abord le milieu de sa propre case (pour passer les portes bien en face), puis la case suivante.
            Vector3 mine = CellCenter(c0, r0);
            Vector3 flat = mine - from;
            flat.y = 0f;
            return flat.magnitude > 0.5f && (hop % wide != c0 ? Mathf.Abs(from.z - mine.z) > 0.4f : Mathf.Abs(from.x - mine.x) > 0.4f)
                ? mine : CellCenter(hop % wide, hop / wide);
        }
    }
}
