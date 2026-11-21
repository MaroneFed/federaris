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
        // (21/11) Agrandi : la crypte (R) derriere l'autel, le tresor (T) derriere la bibliotheque, les cachots (P),
        // les cuisines (Q). Les salles d'avant gardent leurs cases : on n'a ajoute que des rangees au nord et des
        // colonnes a l'est.
        static readonly string[] Plan =
        {
            "..................",
            ".RRRR.............",
            ".RRRR.............",
            ".RRRR.TT..........",
            ".rRRR.tT..........",
            ".kKK.LlLL.DDD.....",
            ".KKK.LLLL.DDD.....",
            ".KkK.LlLL.DdDQQQQ.",
            ".CcCCCcCCCCccqQQQ.",
            ".C..HHhHHHYYCQQQQ.",
            ".C..HHHHHHYyc.....",
            ".cn.HHHHHHAacPPPP.",
            ".C..HHhHHH..cpPPP.",
            ".CCCCCcCCCCCCPPPP.",
            "...SsvvV..........",
            ".....VVV..........",
            ".................."
        };

        static char[,] grid;
        static int wide, deep;
        public static bool Ready;
        static readonly Dictionary<string, Vector4> doors = new Dictionary<string, Vector4>();      // x, z, yaw, -
        static readonly HashSet<char> locked = new HashSet<char>();
        /// <summary>(16/11) Les armures vides : NightDirector les tourne vers le joueur quand il ne regarde pas.</summary>
        public static readonly List<Transform> Armors = new List<Transform>();

        static void Keep(GameObject armor) { if (armor != null) Armors.Add(armor.transform); }

        /// <summary>La lune (NightQuest la fait flamber pendant les eclairs de la cour).</summary>
        public static Light MoonLight;
        /// <summary>La salle (sa lettre majuscule) ou se trouve ce point ; '.' hors du chateau.</summary>
        public static char RoomAt(Vector3 p) { return grid == null ? '.' : Room(Col(p), Row(p)); }
        static readonly List<Vector3> taken = new List<Vector3>();

        /// <summary>Une case du chateau prise au hasard, entre "min" et "max" metres de "near" (jamais dans une salle fermee a cle).</summary>
        public static Vector3 RandomCell(Vector3 near, float min, float max)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                int c = Random.Range(0, wide), r = Random.Range(0, deep);
                char room = Room(c, r);
                if (room == '.' || locked.Contains(room) || room == 'N' || room == 'S' || room == 'R' || room == 'T') continue;
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
        static bool Tall(char room) { return room == 'H' || room == 'K' || room == 'Y' || room == 'R'; }
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
            taken.Clear();
            secrets.Clear(); revealed.Clear(); covers.Clear(); CryptFires.Clear();
            seen = null; lastC = lastR = -1;
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
                                if (IsSecret(key)) secrets[key] = new Vector4(ex, ez, yaw, d);
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
            BuildSecrets(blocks);
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
            Furniture(blocks);

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
        static readonly string[] SecretKeys = { "KR", "LT" };
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

        static void BuildSecrets(Transform blocks)
        {
            foreach (KeyValuePair<string, Vector4> s in secrets)
            {
                Vector4 v = s.Value;
                int d = (int)v.w;
                Vector3 door = new Vector3(v.x, 0f, v.y), dir = new Vector3(DX[d], 0f, DZ[d]);
                Cover cv = new Cover();
                GameObject piece;
                if (s.Key == "LT")
                {
                    // Une bibliotheque comme les autres, collee devant l'arche, cote bibliotheque. Elle glissera de cote.
                    Vector3 at = door - dir * 0.5f;
                    piece = NightKit.Place("bibliotheque", at.x, at.z, d == 0 ? 180f : d == 1 ? -90f : d == 2 ? 0f : 90f, 0f, false);
                    cv.closed = at;
                    cv.open = at + new Vector3(DZ[d], 0f, DX[d]) * 2.05f;
                    cv.speed = 1f / 1.6f;
                    if (piece != null) NightQuest.Add(piece.transform, 3, null, 'L');
                }
                else
                {
                    // Un pan de mur, pile dans l'arche. Il s'enfoncera dans le sol.
                    piece = NightKit.Place("mur_droit", door.x, door.z, v.z, 0f, false);
                    cv.closed = door;
                    cv.open = door + Vector3.down * 4.1f;
                    cv.speed = 1f / 4f;
                }
                if (piece == null) continue;
                cv.piece = piece.transform;
                BoxCollider box = new GameObject("Passage secret").AddComponent<BoxCollider>();
                box.transform.SetParent(blocks, false);
                box.transform.position = cv.closed + Vector3.up * 2f;
                box.transform.rotation = Quaternion.Euler(0f, v.z, 0f);
                box.size = new Vector3(4.2f, 4f, 0.6f);
                cv.wall = box;
                covers[s.Key] = cv;
                Lock(s.Key[1]);
            }
        }

        /// <summary>Le passage s'ouvre (le mur s'enfonce, la bibliotheque glisse) ; la salle cachee apparait sur la carte.</summary>
        public static void Reveal(string key)
        {
            Cover cv;
            if (!covers.TryGetValue(key, out cv)) return;
            revealed.Add(key);
            cv.target = 1f;
            Unlock(key[1]);
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

        // ------------------------------------------------------------------ (21/11) la carte du carnet

        static bool[,] seen;
        static int lastC = -1, lastR = -1;
        static readonly Dictionary<char, string> Names = new Dictionary<char, string>
        {
            { 'V', "Vestibule" }, { 'S', "Sacristie" }, { 'N', "Niche" }, { 'H', "Grande salle" }, { 'K', "Chapelle" },
            { 'Y', "Cour" }, { 'L', "Bibliothèque" }, { 'D', "Dortoir" }, { 'A', "Armurerie" }, { 'P', "Cachots" },
            { 'Q', "Cuisines" }, { 'R', "Crypte" }, { 'T', "Trésor" }
        };

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

        public static void DrawMap(Rect area, Vector3 player, float yaw, GUIStyle label)
        {
            if (grid == null || seen == null) return;
            float s = Mathf.Floor(Mathf.Min(area.width / wide, area.height / deep));
            float ox = Mathf.Round(area.x + (area.width - s * wide) * 0.5f), oy = Mathf.Round(area.y + (area.height - s * deep) * 0.5f);
            float t = Mathf.Max(2f, Mathf.Round(s * 0.09f));
            Color ink = new Color(0.2f, 0.11f, 0.05f, 0.9f), wash = new Color(0.45f, 0.3f, 0.15f, 0.22f), red = new Color(0.6f, 0.05f, 0.03f, 0.9f);
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    if (!seen[c, r]) continue;
                    char room = Room(c, r);
                    Rect cell = new Rect(ox + c * s, oy + (deep - 1 - r) * s, s, s);
                    GUI.color = room == 'C' ? new Color(wash.r, wash.g, wash.b, 0.12f) : room == 'R' ? new Color(0.35f, 0.05f, 0.02f, 0.25f) : wash;
                    GUI.DrawTexture(cell, Texture2D.whiteTexture);
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
            // Le nom des salles deja vues.
            GUIStyle ls = new GUIStyle(label);
            ls.fontSize = Mathf.Max(10, Mathf.RoundToInt(s * 0.38f));
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

        static void SolidBox(Transform blocks, Vector3 p, float yaw, Vector3 size)
        {
            BoxCollider box = new GameObject("Meuble").AddComponent<BoxCollider>();
            box.transform.SetParent(blocks, false);
            box.transform.position = p + Vector3.up * size.y * 0.5f;
            box.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            box.size = size;
        }

        /// <summary>Pose un meuble au sol, avec son collider (largeur, hauteur, profondeur).</summary>
        static GameObject Put(Transform blocks, string piece, float x, float z, float yaw, Vector3 size)
        {
            GameObject go = NightKit.Place(piece, x, z, yaw, 0f, false);
            if (go == null) return null;
            Vector3 p = new Vector3(x, 0f, z);
            taken.Add(p);
            SolidBox(blocks, p, yaw, size);
            return go;
        }

        /// <summary>
        /// Pose "piece" dos aux murs de la salle, face a la piece. sides : 1 nord, 2 est, 4 sud, 8 ouest.
        /// every : un emplacement de mur sur "every". Jamais devant une porte, jamais sur un autre meuble.
        /// </summary>
        static List<GameObject> AlongWalls(Transform blocks, char room, string piece, Vector3 size, int sides, int every, int salt)
        {
            List<GameObject> list = new List<GameObject>();
            int n = 0;
            for (int c = 0; c < wide; c++)
                for (int r = 0; r < deep; r++)
                {
                    if (Room(c, r) != room) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        if ((sides & (1 << d)) == 0 || Room(c + DX[d], r + DZ[d]) == room || Door(c, r, d)) continue;
                        n++;
                        if ((n + salt) % every != 0) continue;
                        Vector3 p = CellCenter(c, r) + new Vector3(DX[d], 0f, DZ[d]) * (2f - 0.27f - size.z * 0.5f);
                        if (NearDoor(p, 2f) || Taken(p, 1.3f)) continue;
                        float yaw = d == 0 ? 180f : d == 1 ? -90f : d == 2 ? 0f : 90f;
                        GameObject go = Put(blocks, piece, p.x, p.z, yaw, size);
                        if (go != null) list.Add(go);
                    }
                }
            return list;
        }

        /// <summary>Un coffre qu'on peut fouiller : son couvercle tourne sur sa charniere.</summary>
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

        /// <summary>Un tombeau qu'on peut ouvrir : sa dalle glisse.</summary>
        static void Tomb(Transform blocks, float x, float z, float yaw, char room)
        {
            GameObject body = Put(blocks, "tombeau", x, z, yaw, new Vector3(2.4f, 0.95f, 1.2f));
            if (body == null) return;
            GameObject lid = NightKit.Place("tombeau_couvercle", 0f, 0f, 0f, 0f, false);
            if (lid != null)
            {
                lid.transform.SetParent(body.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.80f, 0f);
                lid.transform.localRotation = Quaternion.identity;
            }
            NightQuest.Add(body.transform, 1, lid != null ? lid.transform : null, room);
        }

        static readonly Vector3 ChestSize = new Vector3(0.95f, 0.62f, 0.56f);
        static readonly Vector3 TableSize = new Vector3(2.45f, 0.86f, 0.92f);
        static readonly Vector3 BenchSize = new Vector3(2.0f, 0.46f, 0.36f);

        /// <summary>Une longue table et ses deux bancs (le long de x).</summary>
        static void Feast(Transform blocks, float x, float z)
        {
            Put(blocks, "table", x, z, 0f, TableSize);
            Put(blocks, "banc", x, z - 0.85f, 0f, BenchSize);
            Put(blocks, "banc", x, z + 0.85f, 180f, BenchSize);
        }

        /// <summary>Plus une seule salle vide : chaque piece a ses meubles, et ses cachettes a fouiller.</summary>
        static void Furniture(Transform blocks)
        {
            float x0, z0, x1, z1;
            // La grande salle : le festin abandonne (deux longues tables), et une grande cheminee ou le feu brule encore.
            Bounds('H', out x0, out z0, out x1, out z1);
            float zc = (z0 + z1) * 0.5f;
            for (float x = x0 + 6f; x < x1 - 3f; x += 6f) { Feast(blocks, x, zc - 3f); Feast(blocks, x, zc + 3f); }
            NightKit.FireAt("Cheminee_Feu", x0 + 0.72f, zc, 90f, true);
            NightKit.KeepLit();
            SolidBox(blocks, new Vector3(x0 + 0.72f, 0f, zc), 90f, new Vector3(2.7f, 2f, 0.95f));
            taken.Add(new Vector3(x0 + 0.72f, 0f, zc));
            foreach (GameObject c in AlongWalls(blocks, 'H', "coffre", ChestSize, 2, 2, 0)) Chest(c, 'H');

            // La bibliotheque : des rayonnages sur tous les murs, deux tables de lecture.
            Bounds('L', out x0, out z0, out x1, out z1);
            int shelf = 0;
            foreach (GameObject b in AlongWalls(blocks, 'L', "bibliotheque", new Vector3(1.95f, 2.6f, 0.46f), 15, 1, 0))
                if (shelf++ % 3 == 0) NightQuest.Add(b.transform, 2, null, 'L');
            Feast(blocks, (x0 + x1) * 0.5f - 3f, (z0 + z1) * 0.5f);
            Feast(blocks, (x0 + x1) * 0.5f + 3f, (z0 + z1) * 0.5f);

            // Le dortoir : des lits contre les murs, un coffre au fond.
            AlongWalls(blocks, 'D', "lit", new Vector3(2.15f, 0.6f, 0.98f), 10, 1, 0);
            foreach (GameObject c in AlongWalls(blocks, 'D', "coffre", ChestSize, 1, 1, 0)) Chest(c, 'D');

            // L'armurerie : des rateliers d'armes, un coffre.
            AlongWalls(blocks, 'A', "ratelier", new Vector3(1.75f, 2.2f, 0.5f), 4, 1, 0);
            foreach (GameObject c in AlongWalls(blocks, 'A', "coffre", ChestSize, 8, 1, 0)) Chest(c, 'A');

            // La cour : deux tombes sous la lune.
            Bounds('Y', out x0, out z0, out x1, out z1);
            Tomb(blocks, x0 + 2.5f, (z0 + z1) * 0.5f, 90f, 'Y');
            Tomb(blocks, x0 + 5.5f, (z0 + z1) * 0.5f, 90f, 'Y');

            // La chapelle : deux gisants le long des murs, des bancs devant l'autel.
            Bounds('K', out x0, out z0, out x1, out z1);
            float kc = (x0 + x1) * 0.5f, kz = (z0 + z1) * 0.5f;
            Tomb(blocks, x0 + 1.15f, kz, 90f, 'K');
            Tomb(blocks, x1 - 1.15f, kz, 90f, 'K');
            for (int i = 0; i < 2; i++)
                for (int s = -1; s <= 1; s += 2) Put(blocks, "banc", kc + s * 2f, z0 + 2.9f + i * 1.7f, 180f, BenchSize);

            // Le vestibule : des bancs pour ceux qui attendaient, un coffre. La sacristie : un coffre.
            AlongWalls(blocks, 'V', "banc", BenchSize, 2, 1, 0);
            foreach (GameObject c in AlongWalls(blocks, 'V', "coffre", ChestSize, 8, 1, 0)) Chest(c, 'V');
            foreach (GameObject c in AlongWalls(blocks, 'S', "coffre", ChestSize, 1, 2, 0)) Chest(c, 'S');

            // Les cachots : des paillasses, des coffres ou l'on jetait ce qu'on prenait aux prisonniers.
            AlongWalls(blocks, 'P', "lit", new Vector3(2.15f, 0.6f, 0.98f), 15, 2, 1);
            foreach (GameObject c in AlongWalls(blocks, 'P', "coffre", ChestSize, 15, 3, 0)) Chest(c, 'P');

            // Les cuisines : une grande cheminee qui brule encore, la table des cuisiniers, des coffres.
            Bounds('Q', out x0, out z0, out x1, out z1);
            float qz = (z0 + z1) * 0.5f;
            NightKit.FireAt("Cheminee_Feu", x1 - 0.72f, qz, -90f, true);
            NightKit.KeepLit();
            SolidBox(blocks, new Vector3(x1 - 0.72f, 0f, qz), -90f, new Vector3(2.7f, 2f, 0.95f));
            taken.Add(new Vector3(x1 - 0.72f, 0f, qz));
            Feast(blocks, (x0 + x1) * 0.5f - 1.5f, qz);
            foreach (GameObject c in AlongWalls(blocks, 'Q', "coffre", ChestSize, 5, 2, 0)) Chest(c, 'Q');

            // Le tresor : trois coffres au fond, sous une lueur d'or.
            foreach (GameObject c in AlongWalls(blocks, 'T', "coffre", ChestSize, 1, 1, 0)) Chest(c, 'T');
            Bounds('T', out x0, out z0, out x1, out z1);
            Light gold = new GameObject("Lueur du trésor").AddComponent<Light>();
            gold.transform.SetParent(blocks, false);
            gold.transform.position = new Vector3((x0 + x1) * 0.5f, 2.6f, (z0 + z1) * 0.5f);
            gold.color = new Color(1f, 0.75f, 0.35f);
            gold.range = 6f;
            gold.intensity = 0.8f;

            // La crypte : deux gisants au milieu, quatre piliers, quatre brasiers eteints aux coins (le combat).
            Bounds('R', out x0, out z0, out x1, out z1);
            float rx = (x0 + x1) * 0.5f, rz = (z0 + z1) * 0.5f;
            Tomb(blocks, rx, rz - 2.6f, 0f, 'R');
            Tomb(blocks, rx, rz + 2.6f, 0f, 'R');
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                {
                    Vector3 p = new Vector3(rx + i * 4f, 0f, rz + j * 4f);
                    NightKit.Place("pilier", p.x, p.z, 0f, 0f, false);
                    NightKit.Place("pilier", p.x, p.z, 0f, 4f, false);
                    SolidBox(blocks, p, 0f, new Vector3(1f, 8f, 1f));
                }
            Vector3[] fires = { new Vector3(x0 + 1.6f, 0f, rz), new Vector3(x0 + 1.6f, 0f, z1 - 1.6f), new Vector3(x1 - 1.6f, 0f, z1 - 1.6f), new Vector3(x1 - 1.6f, 0f, z0 + 1.6f) };
            foreach (Vector3 f in fires)
            {
                NightKit.FireAt("Brasero_Feu", f.x, f.z, 0f, false);
                if (NightKit.LastFire != null) CryptFires.Add(NightKit.LastFire);
                SolidBox(blocks, f, 0f, new Vector3(0.8f, 1.2f, 0.8f));
            }
            CryptEntry = new Vector3(x0 + 2f, 0f, z0 + 1.6f);
            AbbotHome = new Vector3(x1 - 3f, 0f, z1 - 4f);
            Light cold = new GameObject("Froid de la crypte").AddComponent<Light>();
            cold.transform.SetParent(blocks, false);
            cold.transform.position = new Vector3(rx, 6f, rz);
            cold.color = new Color(0.4f, 0.5f, 0.9f);
            cold.range = 14f;
            cold.intensity = 0.35f;
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
