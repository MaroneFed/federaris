using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA CITADELLE (27/09 -- Martin : "un giga chateau"). Au centre de la sylve.
    ///
    ///   - une ENCEINTE de cent metres de cote, murs de dix-huit metres, quatre tours
    ///     d'angle coiffees d'ardoise ;
    ///   - QUATRE PORTES, ouvertes, une par face, chacune entre deux tours de garde :
    ///     on entre de partout, la bataille commence dans la cour ;
    ///   - QUATRE ESCALIERS montent aux remparts : la-haut, on voit loin, et on saute ;
    ///   - au milieu, LA TOUR DE LA COURONNE (voir Tower.cs) : soixante-quatre metres,
    ///     une rampe en spirale a l'exterieur, la Couronne au sommet ;
    ///   - les YEUX (voir Eye.cs) sur les tours et les portes : pas de gardes, des
    ///     sentinelles de pierre qui chargent un rayon qu'on voit venir.
    ///
    /// Tout est bati en cubes, comme le reste du decor. Les murs, les tours et les
    /// escaliers ont un collider ; les creneaux et les ornements, non.
    /// </summary>
    public static class Castle
    {
        /// <summary>Demi-cote de l'enceinte : cent metres de cote.</summary>
        public const float HalfSize = 50f;
        public const float WallHeight = 18f;
        public const float WallThickness = 3f;
        public const float TowerSize = 14f;
        public const float TowerHeight = 34f;
        public const float GateWidth = 8f;
        public const float GateHeight = 10f;
        public const float GateTowerSize = 8f;
        public const float GateTowerHeight = 26f;

        /// <summary>Rayon du sol aplani sous la citadelle, coins et tours compris.</summary>
        public const float FlatRadius = HalfSize * 1.42f + TowerSize * 0.5f + 10f;

        /// <summary>Le centre de la tour (pour les sons, la musique...).</summary>
        public static readonly Vector3 KeepCentre = Vector3.zero;

        // (30/09 -- "ca fait IA le chateau, je veux des dingueries") UN CHATEAU DE CONTE : de
        // la pierre claire et chaude, des tours RONDES coiffees de toits pointus bleu roi
        // a pointe d'or et fanion, de grandes bannieres rouges, bleues et or sur les
        // murailles, un jardin a la francaise dans la cour.
        // (01/10 -- "on dirait un truc fait par IA") : une palette SOBRE et tenue -- la pierre
        // creme, un soubassement plus sombre, des toits d'ardoise bleu nuit (plus le bleu roi
        // criard), l'or mat, et presque plus rien qui brille : c'etait l'accumulation de
        // lueurs dorees partout qui faisait faux.
        static readonly Color Stone = new Color(0.82f, 0.75f, 0.64f);
        static readonly Color StoneDark = new Color(0.56f, 0.49f, 0.42f);
        static readonly Color StoneMoss = new Color(0.74f, 0.71f, 0.58f);
        static readonly Color Slate = new Color(0.22f, 0.29f, 0.46f);
        static readonly Color Timber = new Color(0.36f, 0.24f, 0.15f);
        static readonly Color IronDark = new Color(0.14f, 0.14f, 0.18f);
        static readonly Color Paving = new Color(0.66f, 0.61f, 0.54f);
        static readonly Color GoldTrim = new Color(1f, 0.8f, 0.32f);
        static readonly Color[] Heraldry = { new Color(0.66f, 0.13f, 0.16f), new Color(0.16f, 0.25f, 0.55f), new Color(0.82f, 0.6f, 0.16f) };

        /// <summary>Vrai si ce point est dans l'emprise de la citadelle (plus une marge) : la foret n'y pousse pas.</summary>
        public static bool Covers(float x, float z, float margin)
        {
            float reach = HalfSize + TowerSize * 0.5f + margin;
            return Mathf.Abs(x) < reach && Mathf.Abs(z) < reach;
        }

        /// <summary>Vrai si ce point est DANS l'enceinte (cour, tour, remparts compris).</summary>
        public static bool Inside(Vector3 p)
        {
            return Mathf.Abs(p.x) < HalfSize + WallThickness * 0.5f && Mathf.Abs(p.z) < HalfSize + WallThickness * 0.5f;
        }

        /// <summary>Vrai si ce point est sur la tour (sa rampe ou son sommet).</summary>
        public static bool InKeep(Vector3 p) { return Tower.On(p); }

        /// <summary>
        /// Pour entrer depuis "from" : la porte la plus proche. Deux points, dehors puis
        /// dedans. (Pour sortir, on les lit a l'envers.)
        /// </summary>
        public static Vector3[] EntryFrom(Vector3 from)
        {
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            Vector3 best = Vector3.back;
            float bestD = float.MaxValue;
            for (int i = 0; i < dirs.Length; i++)
            {
                float d = (from - dirs[i] * HalfSize).sqrMagnitude;
                if (d < bestD) { bestD = d; best = dirs[i]; }
            }
            return new[] { best * (HalfSize + 9f), best * (HalfSize - 9f) };
        }

        public static void Build(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("CITADELLE");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            Game.CastleCentre = Vector3.zero;

            BuildCurtain(t);
            BuildCorners(t);
            BuildGates(t);
            BuildStairs(t);
            BuildCourtyard(t);
            Tower.Build(t);

            int dressed = Masonry.Apply(t);
            Debug.Log("[FIEF] Citadelle : " + dressed + " pans de pierre appareillés.");
        }

        // ------------------------------------------------------------------ outils

        /// <summary>Un pan de mur entre deux points du sol, de y0 a y1.</summary>
        static GameObject Segment(Transform parent, Vector3 a, Vector3 b, float y0, float y1, float thick, Color color, bool solid, string name)
        {
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = flat.magnitude;
            if (length < 0.01f) return null;
            if (!solid) Proto.BeginVisualOnly();
            GameObject go = Proto.Cube(parent, Vector3.zero, new Vector3(thick, y1 - y0, length), color, name);
            if (!solid) Proto.EndVisualOnly();
            go.transform.localPosition = new Vector3((a.x + b.x) * 0.5f, (y0 + y1) * 0.5f, (a.z + b.z) * 0.5f);
            go.transform.localRotation = Quaternion.LookRotation(flat / length, Vector3.up);
            return go;
        }

        /// <summary>Des creneaux sur le haut d'un mur. Quelques-uns sont tombes.</summary>
        static void Crenellate(Transform parent, Vector3 a, Vector3 b, float top, float thick, int seed)
        {
            // (01/10) Des merlons REGULIERS (plus de trous au hasard : ca faisait abime et
            // bacle), chacun coiffe d'un chaperon plus sombre qui deborde un peu.
            System.Random rng = new System.Random(seed);
            Vector3 dir = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = dir.magnitude;
            if (length < 1f) return;
            dir /= length;
            Proto.BeginVisualOnly();
            int count = Mathf.Max(1, Mathf.RoundToInt((length - 0.6f) / 2.6f));
            float step = (length - 0.6f) / count;
            for (int k = 0; k < count; k++)
            {
                Vector3 p = a + dir * (0.3f + step * (k + 0.5f));
                Quaternion face = Quaternion.LookRotation(dir, Vector3.up);
                GameObject m = Proto.Cube(parent, new Vector3(p.x, top + 0.75f, p.z), new Vector3(thick * 0.34f, 1.5f, step * 0.58f),
                                          rng.NextDouble() < 0.12 ? StoneMoss : Stone, "Merlon");
                m.transform.localRotation = face;
                GameObject cap = Proto.Cube(parent, new Vector3(p.x, top + 1.56f, p.z), new Vector3(thick * 0.34f + 0.16f, 0.14f, step * 0.58f + 0.16f), StoneDark, "Chaperon");
                cap.transform.localRotation = face;
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ enceinte

        static void BuildCurtain(Transform t)
        {
            float h = HalfSize, y0 = -1.5f, gap = GateWidth * 0.5f;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % 4];
                Vector3 mid = (a + b) * 0.5f, dir = (b - a).normalized;
                Vector3 left = mid - dir * gap, right = mid + dir * gap;
                Segment(t, a, left, y0, WallHeight, WallThickness, Stone, true, "Courtine");
                Segment(t, right, b, y0, WallHeight, WallThickness, Stone, true, "Courtine");
                Segment(t, left, right, GateHeight, WallHeight, WallThickness, Stone, true, "Linteau");
                // Le merlon cote exterieur seulement : le chemin de ronde reste libre.
                Vector3 outward = Vector3.Cross(Vector3.up, dir) * -1f;
                Vector3 edge = outward * (WallThickness * 0.5f - 0.3f);
                Crenellate(t, a + edge, left + edge, WallHeight, WallThickness, 11 + i);
                Crenellate(t, right + edge, b + edge, WallHeight, WallThickness, 21 + i);
                // Une plinthe sombre au pied (l'humidite qui remonte), un bandeau sous le
                // chemin de ronde, un petit parapet cote cour.
                Segment(t, a, left, y0, 1.8f, WallThickness + 0.4f, StoneDark, false, "Plinthe");
                Segment(t, right, b, y0, 1.8f, WallThickness + 0.4f, StoneDark, false, "Plinthe");
                Segment(t, a, left, WallHeight - 1.6f, WallHeight - 1.2f, WallThickness + 0.3f, StoneDark, false, "Bandeau");
                Segment(t, right, b, WallHeight - 1.6f, WallHeight - 1.2f, WallThickness + 0.3f, StoneDark, false, "Bandeau");
                Vector3 inEdge = outward * -(WallThickness * 0.5f - 0.2f);
                Segment(t, a + inEdge, left + inEdge, WallHeight, WallHeight + 0.8f, 0.4f, Stone, false, "Parapet");
                Segment(t, right + inEdge, b + inEdge, WallHeight, WallHeight + 0.8f, 0.4f, Stone, false, "Parapet");
                // Des meurtrieres : de fines fentes sombres, deux rangs.
                Proto.BeginVisualOnly();
                for (float u = 7f; u < h * 2f - 6f; u += 6f)
                {
                    if (Mathf.Abs(u - h) < gap + 6f) continue;
                    for (int row = 0; row < 2; row++)
                    {
                        Vector3 p = a + dir * (u + row * 3f) + outward * (WallThickness * 0.5f + 0.02f) + Vector3.up * (row == 0 ? 7f : 12.5f);
                        GameObject slit = Proto.Cube(t, p, new Vector3(0.28f, 1.7f, 0.06f), IronDark, "Meurtrière");
                        slit.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                    }
                }
                Proto.EndVisualOnly();
                // Des contreforts dehors, tous les douze metres.
                for (float u = 12f; u < h * 2f - 12f; u += 12f)
                {
                    if (Mathf.Abs(u - h) < gap + 8f) continue;
                    Vector3 p = a + dir * u + outward * (WallThickness * 0.5f + 1f);
                    GameObject butt = Proto.Cube(t, p + Vector3.up * 3.5f, new Vector3(2.6f, 8.5f, 2f), StoneDark, "Contrefort");
                    butt.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                }
                // (30/09) DE GRANDES BANNIERES entre les contreforts, rouge, bleu, or.
                int n = 0;
                for (float u = 18f; u < h * 2f - 12f; u += 12f)
                {
                    if (Mathf.Abs(u - h) < gap + 10f) continue;
                    Banner(t, a + dir * u + outward * (WallThickness * 0.5f + 0.12f), outward, Heraldry[(i + n) % 3]);
                    n++;
                }
            }
        }

        static void BuildCorners(Transform t)
        {
            float h = HalfSize;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            for (int i = 0; i < corners.Length; i++) BigTower(t, corners[i], TowerSize, TowerHeight, true, 30 + i);
        }

        /// <summary>
        /// UNE TOUR RONDE (30/09) : un fut de pierre claire cercle de bandeaux, des fenetres
        /// dont certaines luisent, une couronne de machicoulis, et soit un grand toit conique
        /// bleu roi a pointe d'or et fanion, soit des creneaux (tours de garde).
        /// </summary>
        static void BigTower(Transform t, Vector3 at, float size, float height, bool roofed, int seed)
        {
            float r = size * 0.5f;
            Proto.Cylinder(t, new Vector3(at.x, (height - 1.5f) * 0.5f, at.z), new Vector3(size, (height + 1.5f) * 0.5f, size), Stone, "Tour");
            Proto.BeginVisualOnly();
            Material stone = MaterialFactory.GetShiny(Stone, 0.18f, 0f);
            Material dark = MaterialFactory.GetShiny(StoneDark, 0.2f, 0f);
            Material gold = MaterialFactory.GetShiny(GoldTrim, 0.85f, 1f, 0.35f);
            // Le soubassement evase, les bandeaux.
            Proto.Cylinder(t, new Vector3(at.x, 1.2f, at.z), new Vector3(size + 1.4f, 1.6f, size + 1.4f), StoneDark, "Soubassement").GetComponent<Renderer>().sharedMaterial = dark;
            for (float y = 8f; y < height - 4f; y += 9f)
                Proto.Cylinder(t, new Vector3(at.x, y, at.z), new Vector3(size + 0.35f, 0.25f, size + 0.35f), StoneDark, "Bandeau").GetComponent<Renderer>().sharedMaterial = dark;
            // Des fenetres en arc, tout autour, a deux hauteurs ; une sur trois luit.
            System.Random rng = new System.Random(seed);
            for (int k = 0; k < 6; k++)
            {
                float a = (k / 6f + (seed % 3) * 0.05f) * Mathf.PI * 2f;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int level = 0; level < 2; level++)
                {
                    float wy = height * (0.45f + level * 0.28f);
                    Vector3 p = at + outward * (r - 0.05f) + Vector3.up * wy;
                    GameObject win = Proto.Cube(t, p, new Vector3(0.9f, 1.8f, 0.3f), IronDark, "Fenêtre");
                    win.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                    GameObject arch = Proto.Cylinder(t, p + Vector3.up * 0.9f, new Vector3(0.9f, 0.15f, 0.9f), IronDark, "Arc de fenêtre");
                    arch.transform.localRotation = Quaternion.LookRotation(Vector3.up, outward);
                    if (rng.NextDouble() < 0.35)
                    {
                        Material lit = MaterialFactory.GetGlow(new Color(1f, 0.74f, 0.4f), 0.55f);
                        win.GetComponent<Renderer>().sharedMaterial = lit;
                        arch.GetComponent<Renderer>().sharedMaterial = lit;
                    }
                }
            }
            // La couronne de machicoulis : un anneau plus large, porte par des consoles.
            Proto.Cylinder(t, new Vector3(at.x, height - 0.2f, at.z), new Vector3(size + 1.6f, 0.9f, size + 1.6f), Stone, "Machicoulis").GetComponent<Renderer>().sharedMaterial = stone;
            for (int k = 0; k < 16; k++)
            {
                float a = k / 16f * Mathf.PI * 2f;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                GameObject corbel = Proto.Cube(t, at + outward * (r + 0.4f) + Vector3.up * (height - 1.6f), new Vector3(0.5f, 1.2f, 0.8f), StoneDark, "Console");
                corbel.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                corbel.GetComponent<Renderer>().sharedMaterial = dark;
            }
            if (roofed)
            {
                // LE TOIT EN CLOCHE (01/10) : une jupe evasee qui deborde du mur, puis la
                // pointe elancee -- l'ardoise d'une tour a l'autre change un peu de ton.
                Color slate = Color.Lerp(Slate, seed % 2 == 0 ? new Color(0.2f, 0.34f, 0.44f) : new Color(0.28f, 0.27f, 0.42f), 0.35f);
                Material slateMat = MaterialFactory.GetShiny(slate, 0.45f, 0.05f);
                Proto.Cylinder(t, new Vector3(at.x, height + 0.75f, at.z), new Vector3(size + 1.9f, 0.16f, size + 1.9f), StoneDark, "Corniche").GetComponent<Renderer>().sharedMaterial = dark;
                GameObject skirt = Proto.Cone(t, new Vector3(at.x, height + 0.8f, at.z), r + 1.6f, size * 0.32f, slate, "Jupe du toit", 24);
                skirt.GetComponent<Renderer>().sharedMaterial = slateMat;
                GameObject roof = Proto.Cone(t, new Vector3(at.x, height + 0.8f + size * 0.12f, at.z), r + 0.75f, size * 1.3f, slate, "Toit", 24);
                roof.GetComponent<Renderer>().sharedMaterial = slateMat;
                float tip = height + 0.8f + size * 0.12f + size * 1.3f;
                Proto.Sphere(t, new Vector3(at.x, tip - 0.3f, at.z), Vector3.one * 0.9f, GoldTrim, "Pommeau").GetComponent<Renderer>().sharedMaterial = gold;
                Proto.Cylinder(t, new Vector3(at.x, tip + 2.2f, at.z), new Vector3(0.18f, 2.4f, 0.18f), GoldTrim, "Hampe").GetComponent<Renderer>().sharedMaterial = gold;
                GameObject flag = Proto.Cube(t, new Vector3(at.x + 1.3f, tip + 3.6f, at.z), new Vector3(2.4f, 1.3f, 0.08f), Heraldry[seed % 3], "Fanion");
                flag.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(Heraldry[seed % 3], 0.3f, 0f);
                flag.AddComponent<Flutter>();
                Proto.EndVisualOnly();
                return;
            }
            // Pas de toit : des creneaux tout autour.
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                GameObject m = Proto.Cube(t, at + outward * (r + 0.4f) + Vector3.up * (height + 0.95f), new Vector3(1.1f, 1.4f, 0.8f), Stone, "Merlon");
                m.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                m.GetComponent<Renderer>().sharedMaterial = stone;
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Les quatre portes, au milieu de chaque muraille : deux tours de garde, la herse relevee, un pont-levis, un arc d'or, une banniere.</summary>
        static void BuildGates(Transform t)
        {
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 d = dirs[i];
                Vector3 side = Vector3.Cross(Vector3.up, d);
                Vector3 gate = d * HalfSize;
                float off = GateWidth * 0.5f + GateTowerSize * 0.5f;
                BigTower(t, gate + side * off + d * 1f, GateTowerSize, GateTowerHeight, true, 40 + i * 5);
                BigTower(t, gate - side * off + d * 1f, GateTowerSize, GateTowerHeight, true, 43 + i * 5);
                // L'arc : un bandeau sombre au-dessus du passage, et la herse relevee.
                Proto.BeginVisualOnly();
                for (float s = -GateWidth * 0.5f + 0.5f; s < GateWidth * 0.5f; s += 0.8f)
                    Proto.Cube(t, gate + side * s + Vector3.up * (GateHeight - 0.7f), new Vector3(0.14f, 1.4f, 0.14f), IronDark, "Herse levée");
                Proto.EndVisualOnly();
                // LE PONT-LEVIS (29/09 : "quatre portes au milieu de chaque muraille" -- qu'on
                // les voie) : un tablier de planches cerclees de fer, abaisse, tenu par deux
                // chaines ; l'arc cercle d'or ; une grande banniere au-dessus.
                Quaternion face = Quaternion.LookRotation(d, Vector3.up);
                GameObject deck = Proto.Cube(t, gate + d * 4.6f + Vector3.up * 0.12f, new Vector3(GateWidth - 0.4f, 0.24f, 6.2f), Timber, "Pont-levis");
                deck.transform.rotation = face;
                Proto.BeginVisualOnly();
                for (int k = 0; k < 7; k++)
                {
                    GameObject plank = Proto.Cube(t, gate + d * (1.8f + k * 0.9f) + Vector3.up * 0.25f, new Vector3(GateWidth - 0.5f, 0.04f, 0.08f), new Color(0.15f, 0.11f, 0.08f), "Joint");
                    plank.transform.rotation = face;
                }
                for (int k = -1; k <= 1; k += 2)
                {
                    GameObject band = Proto.Cube(t, gate + d * 4.6f + side * k * (GateWidth * 0.5f - 0.6f) + Vector3.up * 0.26f, new Vector3(0.25f, 0.05f, 6.2f), IronDark, "Ferrure");
                    band.transform.rotation = face;
                    // La chaine : du bout du tablier jusqu'au haut de l'arc.
                    Vector3 low = gate + d * 7.4f + side * k * (GateWidth * 0.5f - 0.4f) + Vector3.up * 0.3f;
                    Vector3 high = gate + d * 1.6f + side * k * (GateWidth * 0.5f + 0.3f) + Vector3.up * (GateHeight + 0.5f);
                    GameObject chain = Proto.Cube(t, (low + high) * 0.5f, new Vector3(0.12f, 0.12f, (high - low).magnitude), IronDark, "Chaîne");
                    chain.transform.rotation = Quaternion.LookRotation(high - low, Vector3.up);
                    // Le montant d'or de l'arc.
                    GameObject gilt = Proto.Cube(t, gate + d * 1.65f + side * k * (GateWidth * 0.5f + 0.1f) + Vector3.up * (GateHeight * 0.5f), new Vector3(0.3f, GateHeight, 0.08f), Color.white, "Arc d'or");
                    gilt.transform.rotation = face;
                    gilt.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(GoldTrim, 0.8f, 1f);
                }
                GameObject lintel = Proto.Cube(t, gate + d * 1.65f + Vector3.up * (GateHeight + 0.15f), new Vector3(GateWidth + 0.5f, 0.3f, 0.08f), Color.white, "Arc d'or");
                lintel.transform.rotation = face;
                lintel.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(GoldTrim, 0.8f, 1f);
                // La grande banniere, au-dessus de l'arc.
                GameObject cloth = Proto.Cube(t, gate + d * 1.75f + Vector3.up * (GateHeight + 4.2f), new Vector3(4.2f, 6f, 0.08f), Heraldry[0], "Bannière");
                cloth.transform.rotation = face;
                GameObject stripe = Proto.Cube(t, gate + d * 1.8f + Vector3.up * (GateHeight + 4.2f), new Vector3(0.7f, 5.2f, 0.04f), Color.white, "Blason");
                stripe.transform.rotation = face;
                stripe.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(GoldTrim, 0.8f, 1f);
                GameObject cross = Proto.Cube(t, gate + d * 1.8f + Vector3.up * (GateHeight + 5.2f), new Vector3(3f, 0.6f, 0.04f), Color.white, "Blason");
                cross.transform.rotation = face;
                cross.GetComponent<Renderer>().sharedMaterial = stripe.GetComponent<Renderer>().sharedMaterial;
                Proto.EndVisualOnly();
                Torch(t, gate + d * 8f + side * (GateWidth * 0.5f + 1.2f), 3.4f);
                Torch(t, gate + d * 8f - side * (GateWidth * 0.5f + 1.2f), 3.4f);
                Torch(t, gate - d * 5f + side * (GateWidth * 0.5f + 1.2f), 3.4f);
                Torch(t, gate - d * 5f - side * (GateWidth * 0.5f + 1.2f), 3.4f);
            }
        }

        /// <summary>
        /// LES ESCALIERS DES REMPARTS : une rampe de pierre contre chaque courtine, cote
        /// cour, finie par un palier. Du chemin de ronde, on domine la cour -- et on
        /// peut sauter de dix-huit metres (on ne meurt pas : on atterrit).
        /// </summary>
        static void BuildStairs(Transform t)
        {
            float inner = HalfSize - WallThickness * 0.5f - 1.3f;
            Vector3[] dirs = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 d = dirs[i];
                Vector3 along = Vector3.Cross(Vector3.up, d);
                // (Entre la tour de garde de la porte, jusqu'a x = 12, et la tour d'angle, a 43.)
                Vector3 from = d * inner + along * 13f;
                Vector3 to = d * inner + along * 37f + Vector3.up * WallHeight;
                Stair(t, from, to);
                Proto.Cube(t, d * inner + along * 39f + Vector3.up * (WallHeight - 0.2f), Rot(d, new Vector3(2.6f, 0.4f, 4f)), StoneDark, "Palier");
            }
        }

        static Vector3 Rot(Vector3 d, Vector3 size)
        {
            return Mathf.Abs(d.x) > 0.5f ? new Vector3(size.x, size.y, size.z) : new Vector3(size.z, size.y, size.x);
        }

        /// <summary>Un escalier droit : une rampe pleine (le collider) et des marches dessinees.</summary>
        public static void Stair(Transform t, Vector3 from, Vector3 to)
        {
            Vector3 run = to - from;
            float length = run.magnitude + 0.4f;
            GameObject ramp = Proto.Cube(t, (from + to) * 0.5f - new Vector3(0f, 0.17f, 0f), new Vector3(2.6f, 0.34f, length), StoneDark, "Escalier");
            ramp.transform.localRotation = Quaternion.LookRotation(run.normalized, Vector3.up);
            Proto.BeginVisualOnly();
            int steps = Mathf.RoundToInt(run.magnitude / 0.45f);
            Vector3 flatDir = new Vector3(run.x, 0f, run.z).normalized;
            for (int i = 0; i < steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, (i + 0.5f) / steps) + Vector3.up * 0.02f;
                GameObject step = Proto.Cube(t, p, new Vector3(2.5f, 0.06f, 0.1f), Paving, "Nez de marche");
                step.transform.localRotation = Quaternion.LookRotation(flatDir, Vector3.up);
            }
            // Sous la rampe, la maconnerie qui la porte.
            for (int i = 1; i < 6; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / 6f);
                Proto.Cube(t, new Vector3(p.x, p.y * 0.5f - 0.2f, p.z), new Vector3(2.4f, Mathf.Max(0.1f, p.y), 0.8f), Stone, "Pile").transform.localRotation =
                    Quaternion.LookRotation(flatDir, Vector3.up);
            }
            Proto.EndVisualOnly();
        }

        static void BuildCourtyard(Transform t)
        {
            float inner = HalfSize - WallThickness * 0.5f;
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(0f, -0.01f, 0f), new Vector3(inner * 2f, 0.1f, inner * 2f), Paving, "Dallage");
            // Quatre allees claires, des portes a la tour : elles guident sans rien dire.
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                Vector3 mid = d * (inner + Tower.OuterRadius) * 0.5f;
                Proto.Cube(t, mid + Vector3.up * 0.01f, Rot(d, new Vector3(inner - Tower.OuterRadius, 0.1f, 5f)), StoneDark, "Allée");
            }
            Proto.EndVisualOnly();
            // (30/09) UN JARDIN DANS CHAQUE QUART DE LA COUR : une pelouse ronde bordee de
            // pierre, une haie basse, des massifs de fleurs, un arbre rond au milieu -- et les
            // torches le long des allees.
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward;
                Garden(t, d * 33f, 11 + i * 7);
                Torch(t, d * 22f, 3.2f);
            }
        }

        /// <summary>Un jardin rond : pelouse, bordure, haie, fleurs, et un arbre (son tronc a un collider).</summary>
        static void Garden(Transform t, Vector3 at, int seed)
        {
            System.Random rng = new System.Random(seed);
            Material lawn = MaterialFactory.GetShiny(new Color(0.38f, 0.66f, 0.28f), 0.15f, 0f);
            Material hedge = MaterialFactory.GetShiny(new Color(0.22f, 0.5f, 0.24f), 0.25f, 0f);
            Material leaves = MaterialFactory.GetShiny(new Color(0.3f, 0.62f, 0.28f), 0.3f, 0f);
            Material leaves2 = MaterialFactory.GetShiny(new Color(0.38f, 0.7f, 0.3f), 0.3f, 0f);
            Color[] flowers = { new Color(0.95f, 0.3f, 0.35f), new Color(1f, 0.85f, 0.3f), new Color(0.96f, 0.96f, 1f), new Color(0.7f, 0.45f, 0.95f) };
            Proto.BeginVisualOnly();
            Proto.Cylinder(t, at + Vector3.up * 0.14f, new Vector3(13.4f, 0.14f, 13.4f), StoneDark, "Bordure");
            Proto.Cylinder(t, at + Vector3.up * 0.2f, new Vector3(12.4f, 0.12f, 12.4f), Color.white, "Pelouse").GetComponent<Renderer>().sharedMaterial = lawn;
            for (int k = 0; k < 20; k++)
            {
                float a = k / 20f * Mathf.PI * 2f;
                if (k % 5 == 0) continue;                  // des passages dans la haie
                Vector3 p = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 5.6f;
                GameObject h = Proto.Capsule(t, p + Vector3.up * 0.7f, new Vector3(1.9f, 0.6f, 1.2f), Color.white, "Haie");
                h.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 90f);
                h.GetComponent<Renderer>().sharedMaterial = hedge;
            }
            for (int k = 0; k < 26; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 2.2f + (float)rng.NextDouble() * 2.6f;
                Color c = flowers[rng.Next(flowers.Length)];
                Proto.Sphere(t, at + new Vector3(Mathf.Cos(a) * r, 0.4f, Mathf.Sin(a) * r), Vector3.one * (0.28f + (float)rng.NextDouble() * 0.14f), c, "Fleur")
                    .GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.35f, 0f);
            }
            Proto.EndVisualOnly();
            // L'arbre : un tronc (collider), trois boules de feuillage.
            Proto.Cylinder(t, at + Vector3.up * 2.2f, new Vector3(0.8f, 2.2f, 0.8f), Timber, "Tronc");
            Proto.BeginVisualOnly();
            Proto.Sphere(t, at + Vector3.up * 5.6f, new Vector3(5.2f, 4.4f, 5.2f), Color.white, "Feuillage").GetComponent<Renderer>().sharedMaterial = leaves;
            Proto.Sphere(t, at + new Vector3(1.4f, 6.8f, 0.6f), new Vector3(3.2f, 2.8f, 3.2f), Color.white, "Feuillage").GetComponent<Renderer>().sharedMaterial = leaves2;
            Proto.Sphere(t, at + new Vector3(-1.2f, 6.4f, -0.9f), new Vector3(3f, 2.6f, 3f), Color.white, "Feuillage").GetComponent<Renderer>().sharedMaterial = leaves2;
            Proto.EndVisualOnly();
        }

        /// <summary>Une banniere pendue en haut d'un mur : un grand drap, une bande d'or, un losange, une pointe.</summary>
        static void Banner(Transform t, Vector3 at, Vector3 outward, Color c)
        {
            Proto.BeginVisualOnly();
            Quaternion face = Quaternion.LookRotation(outward, Vector3.up);
            Material cloth = MaterialFactory.GetShiny(c, 0.3f, 0f);
            Material gold = MaterialFactory.GetShiny(GoldTrim, 0.8f, 1f);
            float top = WallHeight - 0.6f;
            GameObject pole = Proto.Cylinder(t, at + outward * 0.2f + Vector3.up * top, new Vector3(0.16f, 1.7f, 0.16f), GoldTrim, "Hampe");
            pole.transform.rotation = face * Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, 90f, 0f);
            pole.GetComponent<Renderer>().sharedMaterial = gold;
            GameObject drape = Proto.Cube(t, at + outward * 0.1f + Vector3.up * (top - 4f), new Vector3(2.8f, 8f, 0.08f), c, "Bannière");
            drape.transform.rotation = face;
            drape.GetComponent<Renderer>().sharedMaterial = cloth;
            GameObject tip = Proto.Cube(t, at + outward * 0.1f + Vector3.up * (top - 8.3f), new Vector3(1.98f, 1.98f, 0.08f), c, "Pointe");
            tip.transform.rotation = face * Quaternion.Euler(0f, 0f, 45f);
            tip.GetComponent<Renderer>().sharedMaterial = cloth;
            GameObject band = Proto.Cube(t, at + outward * 0.15f + Vector3.up * (top - 1.2f), new Vector3(2.82f, 0.3f, 0.05f), GoldTrim, "Bande d'or");
            band.transform.rotation = face;
            band.GetComponent<Renderer>().sharedMaterial = gold;
            GameObject gem = Proto.Cube(t, at + outward * 0.15f + Vector3.up * (top - 4.2f), new Vector3(1.1f, 1.1f, 0.05f), GoldTrim, "Losange");
            gem.transform.rotation = face * Quaternion.Euler(0f, 0f, 45f);
            gem.GetComponent<Renderer>().sharedMaterial = gold;
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ torches

        /// <summary>
        /// Une torche : un poteau, une flamme, et une lumiere qui vacille. "at" est son
        /// pied (hauteur comprise : il y en a sur la tour).
        /// </summary>
        public static void Torch(Transform t, Vector3 at, float height)
        {
            Proto.BeginVisualOnly();
            // (01/10) Un poteau rond, une coupe de fer, une flamme en goutte (plus de cubes).
            Proto.Cylinder(t, new Vector3(at.x, at.y + height * 0.5f, at.z), new Vector3(0.18f, height * 0.5f, 0.18f), Timber, "Torche");
            Proto.Sphere(t, new Vector3(at.x, at.y + height + 0.1f, at.z), new Vector3(0.42f, 0.22f, 0.42f), IronDark, "Coupe");
            GameObject flame = Proto.Sphere(t, new Vector3(at.x, at.y + height + 0.4f, at.z), new Vector3(0.26f, 0.5f, 0.26f), new Color(1f, 0.62f, 0.22f), "Flamme");
            Proto.EndVisualOnly();
            Renderer r = flame.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.6f, 0.22f), 2.2f);
            flame.AddComponent<Flame>();
            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(at.x, at.y + height + 0.6f, at.z);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.70f, 0.40f);
            light.intensity = 1.5f;
            light.range = 11f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
        }
    }

    /// <summary>Un fanion qui claque au vent : il ondule et se tord un peu, sans fin.</summary>
    public class Flutter : MonoBehaviour
    {
        Vector3 baseScale;
        Quaternion baseRotation;
        float seed;

        void Start()
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            seed = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * 3.2f + seed;
            transform.localRotation = baseRotation * Quaternion.Euler(Mathf.Sin(t * 1.3f) * 4f, Mathf.Sin(t) * 14f, Mathf.Sin(t * 0.7f) * 3f);
            transform.localScale = new Vector3(baseScale.x * (0.92f + 0.08f * Mathf.Sin(t * 2.1f)), baseScale.y, baseScale.z);
        }
    }
}
