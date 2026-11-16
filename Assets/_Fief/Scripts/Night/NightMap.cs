using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE GRAND CHATEAU (15/11). Martin, apres avoir joue : "le truc est beaucoup trop petit".
    /// Le chateau n'est plus pose mur par mur : il est DESSINE dans Plan, une lettre par case de 4 m,
    /// et ce fichier le batit tout seul avec les pieces du kit (sols, plafonds, murs, arches, voutes,
    /// piliers) et pose les murs invisibles qui arretent le joueur. Agrandir le chateau = ajouter des lettres.
    ///
    ///   .  de la roche (rien)         C  couloir (voute)        V  vestibule (on y entre)
    ///   S  sacristie                  N  niche du Penitent      H  grande salle (8 m de haut)
    ///   K  chapelle (8 m)             Y  cour (a ciel ouvert)   L  bibliotheque   D  dortoir   A  armurerie
    ///
    /// Deux cases de lettres differentes sont separees par un mur. Une PORTE (une arche) s'ouvre entre
    /// deux cases voisines ecrites en MINUSCULES. La premiere ligne du plan est le nord.
    ///
    /// Les monstres s'y retrouvent tout seuls : Toward() cherche le chemin de case en case.
    /// </summary>
    public static class NightMap
    {
        public const float Cell = 4f;
        static readonly string[] Plan =
        {
            "..............",
            ".KKK.LLLL.DDD.",
            ".KKK.LLLL.DDD.",
            ".KkK.LlLL.DdD.",
            ".CcCCCcCCCCcC.",
            ".C..HHhHHHYYC.",
            ".C..HHHHHHYyc.",
            ".cn.HHHHHHAac.",
            ".C..HHhHHH..C.",
            ".CCCCCcCCCCCC.",
            "...SsvvV......",
            ".....VVV......",
            ".............."
        };

        static char[,] grid;
        static int wide, deep;
        public static bool Ready;
        static readonly Dictionary<string, Vector4> doors = new Dictionary<string, Vector4>();      // x, z, yaw, -
        static readonly HashSet<char> locked = new HashSet<char>();
        /// <summary>(16/11) Les armures vides : NightDirector les tourne vers le joueur quand il ne regarde pas.</summary>
        public static readonly List<Transform> Armors = new List<Transform>();

        static void Keep(GameObject armor) { if (armor != null) Armors.Add(armor.transform); }

        /// <summary>Une case du chateau prise au hasard, entre "min" et "max" metres de "near" (jamais dans une salle fermee a cle).</summary>
        public static Vector3 RandomCell(Vector3 near, float min, float max)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                int c = Random.Range(0, wide), r = Random.Range(0, deep);
                char room = Room(c, r);
                if (room == '.' || locked.Contains(room) || room == 'N' || room == 'S') continue;
                Vector3 p = CellCenter(c, r);
                float d = new Vector2(p.x - near.x, p.z - near.z).magnitude;
                if (d >= min && d <= max) return p;
            }
            return CellCenter(Col(near), Row(near));
        }
        const int Solid = ~(1 << 2);
        static readonly int[] DX = { 0, 1, 0, -1 }, DZ = { 1, 0, -1, 0 };       // nord, est, sud, ouest

        static char At(int c, int r) { return c < 0 || r < 0 || c >= wide || r >= deep ? '.' : grid[c, r]; }
        static char Room(int c, int r) { return char.ToUpperInvariant(At(c, r)); }
        static bool Tall(char room) { return room == 'H' || room == 'K' || room == 'Y'; }
        public static Vector3 CellCenter(int c, int r) { return new Vector3(c * Cell + 2f, 0f, r * Cell + 2f); }
        static int Col(Vector3 p) { return Mathf.FloorToInt(p.x / Cell); }
        static int Row(Vector3 p) { return Mathf.FloorToInt(p.z / Cell); }

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

        // ------------------------------------------------------------------ batir

        public static bool Build(Transform world)
        {
            Ready = false;
            doors.Clear();
            locked.Clear();
            Armors.Clear();
            deep = Plan.Length;
            wide = Plan[0].Length;
            grid = new char[wide, deep];
            for (int i = 0; i < deep; i++)
                for (int c = 0; c < wide; c++) grid[c, deep - 1 - i] = Plan[i][c];
            if (!NightKit.Begin(world)) return false;
            Transform blocks = new GameObject("Murs invisibles").transform;
            blocks.SetParent(world, false);

            // Le sol : une seule grande plaque invisible sous tout le chateau.
            BoxCollider ground = new GameObject("Sol").AddComponent<BoxCollider>();
            ground.transform.SetParent(blocks, false);
            ground.transform.position = new Vector3(wide * 2f, -0.25f, deep * 2f);
            ground.size = new Vector3(wide * Cell, 0.5f, deep * Cell);

            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    char room = Room(c, r);
                    if (room == '.') continue;
                    Vector3 p = CellCenter(c, r);
                    float h = Tall(room) ? 8f : 4f;
                    int straight = Straight(c, r);
                    NightKit.Place("dalle_sol", p.x, p.z, 0f, 0f, false);
                    if (straight != 0) NightKit.Place("voute_couloir", p.x, p.z, straight == 1 ? 0f : 90f, 0f, false);
                    else if (room != 'Y') NightKit.Place("dalle_sol", p.x, p.z, 0f, h, true);

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
                        if (mine)
                        {
                            if (Door(c, r, d))
                            {
                                NightKit.ArchAt(ex, ez, yaw);
                                string key = room < other ? "" + room + other : "" + other + room;
                                doors[key] = new Vector4(ex, ez, yaw, 0f);
                            }
                            else
                            {
                                NightKit.Place("mur_droit", ex, ez, yaw, 0f, false);
                                BoxCollider wall = new GameObject("Mur").AddComponent<BoxCollider>();
                                wall.transform.SetParent(blocks, false);
                                wall.transform.position = new Vector3(ex, 2f, ez);
                                wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                                wall.size = new Vector3(4.5f, 4f, 0.5f);
                                // Une torche eteinte, de loin en loin, le long des couloirs droits.
                                if (straight != 0 && (c * 7 + r * 13) % 3 == 0)
                                    NightKit.Place("torche_murale", ex, ez, d == 0 ? 180f : d == 1 ? -90f : d == 2 ? 0f : 90f, 0f, false);
                            }
                        }
                        if (Tall(room) && (mine || !Tall(other))) NightKit.Place("mur_droit", ex, ez, yaw, 4f, false);
                    }

                    // Les piliers des couloirs : un de chaque cote, a chaque case.
                    if (straight != 0)
                        for (int s = -1; s <= 1; s += 2)
                        {
                            float px = straight == 1 ? p.x + s * 1.55f : p.x - 2f, pz = straight == 1 ? p.z - 2f : p.z + s * 1.55f;
                            NightKit.Place("pilier", px, pz, 0f, 0f, false);
                            BoxCollider post = new GameObject("Pilier").AddComponent<BoxCollider>();
                            post.transform.SetParent(blocks, false);
                            post.transform.position = new Vector3(px, 2f, pz);
                            post.size = new Vector3(0.55f, 4f, 0.55f);
                        }
                }

            Furnish(blocks);
            Ready = true;
            return true;
        }

        /// <summary>Ce qu'il y a dans les salles : l'autel et le vitrail, les armures vides, les braseros, la lune.</summary>
        static void Furnish(Transform blocks)
        {
            float x0, z0, x1, z1;
            // La chapelle : la fenetre au nord, l'autel devant, deux braseros qui attendent l'aube.
            Bounds('K', out x0, out z0, out x1, out z1);
            float cx = (x0 + x1) * 0.5f;
            NightKit.Place("autel", cx, z1 - 2.5f, 0f, 0f, false);
            Block(blocks, new Vector3(cx, 0.55f, z1 - 2.5f), new Vector3(3.1f, 1.1f, 1.3f));
            NightKit.FireAt("Brasero_Feu", cx - 2.6f, z1 - 2.8f, 0f, false);
            NightKit.FireAt("Brasero_Feu", cx + 2.6f, z1 - 2.8f, 0f, false);
            NightKit.Place("fenetre", cx, z1, 180f, 0f, false);
            NightKit.Place("vitrail", cx, z1, 180f, 0f, false);
            NightKit.Moon(new Vector3(cx, 0f, z1));

            // La grande salle : une haie d'armures vides le long des murs, quatre braseros eteints.
            Bounds('H', out x0, out z0, out x1, out z1);
            for (float x = x0 + 2f; x < x1; x += 4f)
            {
                Keep(NightKit.Place("chevalier", x, z1 - 0.75f, 180f, 0f, false));
                Keep(NightKit.Place("chevalier", x, z0 + 0.75f, 0f, 0f, false));
            }
            NightKit.FireAt("Brasero_Feu", x0 + 3f, z0 + 3f, 0f, false);
            NightKit.FireAt("Brasero_Feu", x1 - 3f, z0 + 3f, 0f, false);
            NightKit.FireAt("Brasero_Feu", x0 + 3f, z1 - 3f, 0f, false);
            NightKit.FireAt("Brasero_Feu", x1 - 3f, z1 - 3f, 0f, false);

            // L'armurerie : encore des armures. La bibliotheque : de longues tables de pierre. Le dortoir : des cierges morts.
            Bounds('A', out x0, out z0, out x1, out z1);
            for (float x = x0 + 1.2f; x < x1; x += 1.9f) Keep(NightKit.Place("chevalier", x, z1 - 0.8f, 180f, 0f, false));
            Bounds('L', out x0, out z0, out x1, out z1);
            for (float x = x0 + 4f; x < x1 - 1f; x += 5f)
            {
                NightKit.Place("autel", x, (z0 + z1) * 0.5f + 1f, 90f, 0f, false);
                Block(blocks, new Vector3(x, 0.55f, (z0 + z1) * 0.5f + 1f), new Vector3(1.3f, 1.1f, 3.1f));
                NightKit.Place("cierge", x + 1.2f, z1 - 1.2f, 0f, 0f, false);
            }
            Bounds('D', out x0, out z0, out x1, out z1);
            for (float z = z0 + 2f; z < z1; z += 3f)
            {
                NightKit.Place("cierge", x0 + 1f, z, 0f, 0f, false);
                NightKit.Place("cierge", x1 - 1f, z, 0f, 0f, false);
            }

            // Le vestibule : deux torches allumees de part et d'autre de la porte (l'ouverture les soufflera).
            Bounds('V', out x0, out z0, out x1, out z1);
            Vector3 door;
            float yaw;
            if (TryDoor('C', 'V', out door, out yaw))
            {
                NightKit.FireAt("Torche_Murale_Feu", door.x - 3f, door.z, 180f, true);
                NightKit.FireAt("Torche_Murale_Feu", door.x + 3f, door.z, 180f, true);
            }

            // La lune : elle n'eclaire que la cour, les plafonds font de l'ombre partout ailleurs.
            Light moon = new GameObject("Lune").AddComponent<Light>();
            moon.transform.SetParent(blocks, false);
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.6f, 1f);
            moon.intensity = 0.55f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 1f;
            moon.transform.rotation = Quaternion.Euler(58f, 35f, 0f);
        }

        static void Block(Transform parent, Vector3 at, Vector3 size)
        {
            BoxCollider box = new GameObject("Meuble").AddComponent<BoxCollider>();
            box.transform.SetParent(parent, false);
            box.transform.position = at;
            box.size = size;
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

        public static Vector3 Center(char room)
        {
            float x0, z0, x1, z1;
            Bounds(room, out x0, out z0, out x1, out z1);
            return new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
        }

        /// <summary>Ou est la porte entre ces deux salles, et dans quel sens est son mur (0 : est-ouest, 90 : nord-sud) ?</summary>
        public static bool TryDoor(char a, char b, out Vector3 at, out float yaw)
        {
            Vector4 d;
            bool found = doors.TryGetValue(a < b ? "" + a + b : "" + b + a, out d);
            at = new Vector3(d.x, 0f, d.y);
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
            if (a == b) return true;
            return Door(c, r, d) && !locked.Contains(a) && !locked.Contains(b);
        }

        /// <summary>
        /// Pour aller de "from" a "goal" : tout droit si rien ne barre, sinon vers le milieu de la case
        /// suivante sur le plus court chemin (une recherche en largeur sur les cases). "ok" : un chemin existe.
        /// </summary>
        public static Vector3 Toward(Vector3 from, Vector3 goal, out bool ok)
        {
            ok = true;
            Vector3 up = Vector3.up * 1.4f;
            if (!Physics.Linecast(from + up, goal + up, Solid, QueryTriggerInteraction.Ignore)) return goal;
            int c0 = Col(from), r0 = Row(from), c1 = Col(goal), r1 = Row(goal);
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
