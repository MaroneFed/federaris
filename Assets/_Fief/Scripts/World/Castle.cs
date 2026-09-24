using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CHATEAU. Au centre de la sylve, mort, et immense.
    ///
    /// Quatre-vingts metres de cote, des murs de dix, des tours de dix-huit, un donjon
    /// de vingt-huit. Avec une brume qui efface tout a vingt-six metres, on ne le voit
    /// JAMAIS en entier : on arrive au pied d'un mur sombre qui se perd dans le
    /// brouillard des deux cotes. C'est ca qui le rend enorme -- pas sa taille, ce
    /// qu'on n'en voit pas.
    ///
    /// SES TORCHES sont les seules lumieres du monde, en dehors de ta lanterne. Quand
    /// on approche, on les voit d'abord comme des taches chaudes dans la brume.
    ///
    /// CE QU'IL CONTIENT (voir docs/LA-SAISON.md) :
    ///   - la STELE, au milieu de la cour : la ou l'on pose sa relique ;
    ///   - trois RESERVES le long des murs, avec des caisses de Fer ancien -- la
    ///     ressource la plus chere, et la seule qu'on ne trouve nulle part ailleurs ;
    ///   - une seule entree, la grande porte au sud. La poterne viendra en Phase 2,
    ///     avec les gardes qu'on soudoie pour l'ouvrir.
    ///
    /// Tout est bati en cubes, comme le reste du decor. Les murs, les tours, le donjon
    /// et les reserves ont un collider : on ne passe pas au travers. Les creneaux, les
    /// meurtrieres et la herse sont purement visuels -- personne ne les touche, et des
    /// centaines de colliders inutiles couteraient pour rien.
    /// </summary>
    public static class Castle
    {
        /// <summary>Demi-cote de l'enceinte : le chateau fait 80 m de cote.</summary>
        public const float HalfSize = 40f;
        public const float WallHeight = 15f;
        public const float WallThickness = 2.6f;
        public const float TowerSize = 13f;
        public const float TowerHeight = 30f;
        /// <summary>Les deux tours de la grande porte.</summary>
        public const float GatehouseHeight = 22f;
        /// <summary>Le donjon : on n'en voit jamais le sommet, il se perd dans la brume.</summary>
        public const float KeepHeight = 48f;
        public const float GateWidth = 6.5f;
        public const float GateHeight = 7.5f;
        public const float PosterneWidth = 1.6f;
        public const float PosterneHeight = 2.6f;
        public const float BreachFrom = 8f;
        public const float BreachTo = 16f;
        public const float BreachHeight = 6.5f;

        /// <summary>Rayon du sol aplani sous le chateau, coins et tours compris.</summary>
        public const float FlatRadius = HalfSize * 1.42f + TowerSize * 0.5f + 10f;

        /// <summary>La stele : un peu au sud du centre, pour degager le parvis du donjon.</summary>
        public static readonly Vector3 StelePosition = new Vector3(0f, 0f, -4f);

        static readonly Color Stone = new Color(0.31f, 0.31f, 0.30f);
        static readonly Color StoneDark = new Color(0.23f, 0.23f, 0.23f);
        static readonly Color StoneMoss = new Color(0.22f, 0.26f, 0.19f);
        static readonly Color Slate = new Color(0.17f, 0.18f, 0.20f);
        static readonly Color Timber = new Color(0.22f, 0.16f, 0.11f);
        static readonly Color IronDark = new Color(0.12f, 0.12f, 0.13f);
        static readonly Color Paving = new Color(0.20f, 0.20f, 0.19f);

        /// <summary>L'allee des rois : de la porte vers le sud, bordee de statues.</summary>
        public const float AvenueHalfWidth = 10f;
        public const float AvenueEnd = -92f;

        /// <summary>
        /// Vrai si ce point est dans l'emprise du chateau (plus une marge) -- allee
        /// des rois comprise : la foret n'y pousse pas, le mage n'y apparait pas.
        /// </summary>
        public static bool Covers(float x, float z, float margin)
        {
            float reach = HalfSize + TowerSize * 0.5f + margin;
            if (Mathf.Abs(x) < reach && Mathf.Abs(z) < reach) return true;
            return Mathf.Abs(x) < AvenueHalfWidth + margin && z < -HalfSize && z > AvenueEnd - margin;
        }

        /// <summary>
        /// Vrai si ce point est DANS une des trois reserves a fer. Les gardes jettent
        /// dehors qui y met les pieds.
        /// </summary>
        public static bool InStoreroom(float x, float z)
        {
            float west = -HalfSize + WallThickness * 0.5f + 4.6f;
            float north = HalfSize - WallThickness * 0.5f - 4.6f;
            if (Mathf.Abs(x - west) < 4.5f && Mathf.Abs(z + 14f) < 6f) return true;
            if (Mathf.Abs(x + west) < 4.5f && Mathf.Abs(z + 14f) < 6f) return true;
            return Mathf.Abs(x + 25f) < 6f && Mathf.Abs(z - north) < 4.5f;
        }

        /// <summary>Le donjon : sa salle du trone se visite.</summary>
        public static readonly Vector3 KeepCentre = new Vector3(0f, 0f, 19f);
        public const float KeepHalfWidth = 10f;
        public const float KeepHalfDepth = 8f;
        public const float HallFloor = 0.9f;
        public const float HallCeiling = 9f;

        public static void Build(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("CHATEAU");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            Game.CastleCentre = Vector3.zero;

            BuildCurtain(t);
            BuildTowers(t);
            BuildGatehouse(t);
            BuildKeep(t);
            BuildCourtyard(t);

            Storeroom(t, new Vector3(-HalfSize + WallThickness * 0.5f + 4.6f, 0f, -14f), 9f, 12f, Side.PlusX, cfg);
            Storeroom(t, new Vector3(HalfSize - WallThickness * 0.5f - 4.6f, 0f, -14f), 9f, 12f, Side.MinusX, cfg);
            Storeroom(t, new Vector3(-25f, 0f, HalfSize - WallThickness * 0.5f - 4.6f), 12f, 9f, Side.MinusZ, cfg);

            // L'ancienne stele du chateau est devenue le Registre : chacun a
            // maintenant la sienne, dans la foret. Celle-ci grave le classement.
            Registry.Build(t, StelePosition);
            BuildTorches(t);

            // Tout ce qui fait qu'on s'arrete pour regarder : l'allee des rois, les
            // portes ouvertes, les braseros, la salle du trone, la cour en ruine.
            CastleDecor.Build(t, cfg);

            // Et pour finir, la pierre : chaque mur recoit son appareil de pierres
            // taillees et son relief (voir Masonry.cs).
            int dressed = Masonry.Apply(t);
            Debug.Log("[FIEF] Chateau : " + dressed + " pans de pierre appareilles.");
        }

        // ------------------------------------------------------------------ outils

        enum Side { PlusX, MinusX, PlusZ, MinusZ }

        /// <summary>
        /// Un pan de mur entre deux points du sol, de la hauteur y0 a y1. Tout le
        /// chateau est fait de ca. Le mur descend sous le sol : si le terrain n'est pas
        /// parfaitement plat, on ne voit jamais de jour entre le mur et la terre.
        /// </summary>
        static GameObject Segment(Transform parent, Vector3 a, Vector3 b, float y0, float y1,
                                  float thick, Color color, bool solid, string name)
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

        /// <summary>Un mur perce d'une porte en son milieu : deux pans et un linteau.</summary>
        static void WallWithDoor(Transform parent, Vector3 a, Vector3 b, float y0, float y1,
                                 float thick, float doorWidth, float doorHeight, Color color, string name)
        {
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = (b - a).normalized;
            Vector3 left = mid - dir * doorWidth * 0.5f;
            Vector3 right = mid + dir * doorWidth * 0.5f;
            Segment(parent, a, left, y0, y1, thick, color, true, name);
            Segment(parent, right, b, y0, y1, thick, color, true, name);
            Segment(parent, left, right, doorHeight, y1, thick, color, true, name + "_Linteau");
        }

        /// <summary>
        /// Des creneaux sur le haut d'un mur. Un sur deux manque, et quelques-uns sont
        /// tombes : le chateau est mort depuis longtemps.
        /// </summary>
        static void Crenellate(Transform parent, Vector3 a, Vector3 b, float top, float thick, int seed)
        {
            System.Random rng = new System.Random(seed);
            Vector3 dir = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = dir.magnitude;
            if (length < 1f) return;
            dir /= length;

            Proto.BeginVisualOnly();
            for (float s = 0.9f; s < length - 0.5f; s += 2.4f)
            {
                if (rng.NextDouble() < 0.14) continue;
                Vector3 p = a + dir * s;
                GameObject m = Proto.Cube(parent, new Vector3(p.x, top + 0.65f, p.z),
                                          new Vector3(thick * 0.92f, 1.3f, 1.25f),
                                          rng.NextDouble() < 0.2 ? StoneMoss : Stone, "Merlon");
                m.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ enceinte

        static void BuildCurtain(Transform t)
        {
            float h = HalfSize;
            float y0 = -1.5f;
            Vector3 nw = new Vector3(-h, 0f, h), ne = new Vector3(h, 0f, h);
            Vector3 sw = new Vector3(-h, 0f, -h), se = new Vector3(h, 0f, -h);

            // Au milieu du mur nord, la POTERNE : une porte basse, fermee de
            // l'interieur. Seul un garde soudoye l'ouvre (voir Poterne et Guard).
            WallWithDoor(t, nw, ne, y0, WallHeight, WallThickness, PosterneWidth, PosterneHeight, Stone, "Courtine_Nord");
            Poterne.Build(t, new Vector3(0f, 0f, h));

            // A l'est, un pan s'est effondre : entre z = 8 et z = 16, le mur ne monte
            // plus qu'a six metres et demi. Trop haut pour passer, assez bas pour qu'on
            // se dise que ce chateau a perdu une guerre.
            Vector3 breachS = new Vector3(h, 0f, BreachFrom), breachN = new Vector3(h, 0f, BreachTo);
            Segment(t, ne, breachN, y0, WallHeight, WallThickness, Stone, true, "Courtine_Est");
            Segment(t, breachN, breachS, y0, BreachHeight, WallThickness, Stone, true, "Breche");
            Segment(t, breachS, se, y0, WallHeight, WallThickness, Stone, true, "Courtine_Est");
            Segment(t, sw, nw, y0, WallHeight, WallThickness, Stone, true, "Courtine_Ouest");
            WallWithDoor(t, sw, se, y0, WallHeight, WallThickness, GateWidth, GateHeight, Stone, "Courtine_Sud");

            // Une plinthe plus sombre au pied : l'humidite qui remonte dans la pierre.
            Segment(t, nw, new Vector3(-PosterneWidth * 0.5f, 0f, h), y0, 1.6f, WallThickness + 0.3f, StoneDark, false, "Plinthe");
            Segment(t, new Vector3(PosterneWidth * 0.5f, 0f, h), ne, y0, 1.6f, WallThickness + 0.3f, StoneDark, false, "Plinthe");
            Segment(t, ne, se, y0, 1.6f, WallThickness + 0.3f, StoneDark, false, "Plinthe");
            Segment(t, sw, nw, y0, 1.6f, WallThickness + 0.3f, StoneDark, false, "Plinthe");

            Crenellate(t, nw, ne, WallHeight, WallThickness, 11);
            Crenellate(t, ne, new Vector3(h, 0f, BreachTo + 1f), WallHeight, WallThickness, 12);
            Crenellate(t, new Vector3(h, 0f, BreachFrom - 1f), se, WallHeight, WallThickness, 16);
            Crenellate(t, sw, nw, WallHeight, WallThickness, 13);
            Crenellate(t, sw, new Vector3(-GateWidth * 0.5f - 4f, 0f, -h), WallHeight, WallThickness, 14);
            Crenellate(t, new Vector3(GateWidth * 0.5f + 4f, 0f, -h), se, WallHeight, WallThickness, 15);

            Buttresses(t);
            Machicolations(t);
        }

        /// <summary>
        /// Des contreforts au pied des courtines, tous les dix metres : un mur de
        /// quinze metres parfaitement lisse ressemble a un decor de theatre. Deux
        /// blocs en retrait l'un sur l'autre, comme un talus de pierre.
        /// </summary>
        static void Buttresses(Transform t)
        {
            float h = HalfSize;
            float out1 = WallThickness * 0.5f + 0.8f;
            float reach = h - TowerSize * 0.5f - 3f;
            for (float u = -reach; u <= reach + 0.01f; u += 10f)
            {
                // Nord, sauf devant la poterne.
                if (Mathf.Abs(u) > 3f) Buttress(t, new Vector3(u, 0f, h + out1), Vector3.forward);
                // Sud, sauf entre les tours de la grande porte.
                if (Mathf.Abs(u) > GateWidth * 0.5f + 9f) Buttress(t, new Vector3(u, 0f, -h - out1), Vector3.back);
                // Ouest, partout.
                Buttress(t, new Vector3(-h - out1, 0f, u), Vector3.left);
                // Est, sauf dans la breche.
                if (u < BreachFrom - 2f || u > BreachTo + 2f) Buttress(t, new Vector3(h + out1, 0f, u), Vector3.right);
            }
        }

        static void Buttress(Transform t, Vector3 at, Vector3 outward)
        {
            float y = Ground.Sample(at.x, at.z);
            Quaternion face = Quaternion.LookRotation(outward, Vector3.up);
            GameObject low = Proto.Cube(t, new Vector3(at.x, y + 2.6f, at.z), new Vector3(2.4f, 7f, 1.6f), StoneDark, "Contrefort");
            low.transform.localRotation = face;
            GameObject high = Proto.Cube(t, new Vector3(at.x, y + 7.8f, at.z) - outward * 0.35f, new Vector3(2f, 3.6f, 0.9f), Stone, "Contrefort");
            high.transform.localRotation = face;
            Proto.BeginVisualOnly();
            GameObject cap = Proto.Cube(t, new Vector3(at.x, y + 9.7f, at.z) - outward * 0.5f, new Vector3(2.1f, 0.5f, 0.9f), StoneDark, "Glacis");
            cap.transform.localRotation = face * Quaternion.Euler(-35f, 0f, 0f);
            Proto.EndVisualOnly();
        }

        /// <summary>
        /// Les MACHICOULIS : sous les creneaux, la galerie de pierre en surplomb,
        /// posee sur des consoles. C'est par ses trous qu'on versait la poix sur les
        /// assaillants -- et c'est ce qui donne a un rempart son air menacant.
        /// </summary>
        static void Machicolations(Transform t)
        {
            float h = HalfSize;
            float o = WallThickness * 0.5f + 0.45f;
            float reach = h - TowerSize * 0.5f;
            Proto.BeginVisualOnly();
            // Nord et ouest d'un seul tenant ; sud en deux (la porte) ; est en deux (la breche).
            Gallery(t, new Vector3(-reach, 0f, h + o), new Vector3(reach, 0f, h + o));
            Gallery(t, new Vector3(-h - o, 0f, -reach), new Vector3(-h - o, 0f, reach));
            Gallery(t, new Vector3(-reach, 0f, -h - o), new Vector3(-GateWidth * 0.5f - 8.4f, 0f, -h - o));
            Gallery(t, new Vector3(GateWidth * 0.5f + 8.4f, 0f, -h - o), new Vector3(reach, 0f, -h - o));
            Gallery(t, new Vector3(h + o, 0f, -reach), new Vector3(h + o, 0f, BreachFrom - 1f));
            Gallery(t, new Vector3(h + o, 0f, BreachTo + 1f), new Vector3(h + o, 0f, reach));
            Proto.EndVisualOnly();
        }

        static void Gallery(Transform t, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float length = d.magnitude;
            if (length < 1f) return;
            Vector3 dir = d / length;
            Quaternion along = Quaternion.LookRotation(dir, Vector3.up);
            GameObject band = Proto.Cube(t, (a + b) * 0.5f + Vector3.up * (WallHeight - 0.4f), new Vector3(0.9f, 0.8f, length), Stone, "Machicoulis");
            band.transform.localRotation = along;
            int count = Mathf.FloorToInt(length / 1.6f);
            for (int i = 0; i <= count; i++)
            {
                Vector3 p = a + dir * (i * length / Mathf.Max(1, count)) + Vector3.up * (WallHeight - 1.25f);
                GameObject console = Proto.Cube(t, p, new Vector3(0.7f, 0.9f, 0.4f), StoneDark, "Console");
                console.transform.localRotation = along;
            }
        }

        static void BuildTowers(Transform t)
        {
            float h = HalfSize;
            Vector3[] corners =
            {
                new Vector3(-h, 0f, h), new Vector3(h, 0f, h),
                new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                Tower(t, corners[i], TowerSize, TowerHeight, 20 + i, true);
            }
        }

        /// <summary>
        /// Une tour. Les quatre tours d'angle portent un TOIT POINTU d'ardoise : c'est
        /// la silhouette qu'on devine au-dessus de la brume en s'approchant, et c'est
        /// ce qui dit "chateau" avant meme qu'on voie un mur. Les tours du chatelet
        /// gardent leurs creneaux : le contraste rend les deux plus lisibles.
        /// </summary>
        static void Tower(Transform t, Vector3 at, float size, float height, int seed, bool roofed = false)
        {
            Proto.Cube(t, new Vector3(at.x, (height - 1.5f) * 0.5f, at.z),
                       new Vector3(size, height + 1.5f, size), Stone, "Tour");

            // Meurtrieres : des fentes noires. De loin, ce sont elles qui font "chateau".
            Proto.BeginVisualOnly();
            for (int k = 0; k < 4; k++)
            {
                float a = k * 90f;
                Vector3 outward = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                int levels = Mathf.Max(2, Mathf.FloorToInt((height - 4f) / 6f));
                for (int level = 0; level < levels; level++)
                {
                    Vector3 p = at + outward * (size * 0.5f + 0.02f) + Vector3.up * (6f + level * 6f);
                    GameObject slit = Proto.Cube(t, p, new Vector3(0.45f, 1.9f, 0.08f), IronDark, "Meurtriere");
                    slit.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                }
            }
            Proto.EndVisualOnly();

            float half = size * 0.5f;
            if (roofed)
            {
                // Un bandeau sombre sous l'avant-toit, puis le cone d'ardoise, puis une
                // fine fleche. Octogonal : une arete tombe sur chaque coin de la tour.
                Proto.BeginVisualOnly();
                Proto.Cube(t, new Vector3(at.x, height + 0.3f, at.z), new Vector3(size + 0.5f, 0.6f, size + 0.5f), StoneDark, "Corniche");
                Proto.EndVisualOnly();
                Proto.Cone(t, new Vector3(at.x, height + 0.55f, at.z), size * 0.78f, size * 1.05f, Slate, "Toit");
                Proto.Cone(t, new Vector3(at.x, height + 0.55f + size * 1.0f, at.z), 0.18f, 3.2f, IronDark, "Fleche", 4);
                return;
            }

            Vector3 a1 = at + new Vector3(-half, 0f, half), a2 = at + new Vector3(half, 0f, half);
            Vector3 a3 = at + new Vector3(half, 0f, -half), a4 = at + new Vector3(-half, 0f, -half);
            Crenellate(t, a1, a2, height, 1.2f, seed);
            Crenellate(t, a2, a3, height, 1.2f, seed + 100);
            Crenellate(t, a3, a4, height, 1.2f, seed + 200);
            Crenellate(t, a4, a1, height, 1.2f, seed + 300);
        }

        static void BuildGatehouse(Transform t)
        {
            float z = -HalfSize;
            float off = GateWidth * 0.5f + 4.2f;
            Tower(t, new Vector3(-off, 0f, z - 0.5f), 8f, GatehouseHeight, 40);
            Tower(t, new Vector3(off, 0f, z - 0.5f), 8f, GatehouseHeight, 41);

            // La herse, relevee : on n'en voit que le bas, suspendu dans l'ouverture.
            // Purement visuelle : elle ne ferme rien, et ne gene pas le passage.
            Proto.BeginVisualOnly();
            for (float x = -GateWidth * 0.5f + 0.45f; x < GateWidth * 0.5f; x += 0.75f)
            {
                Proto.Cube(t, new Vector3(x, GateHeight - 0.9f, z), new Vector3(0.12f, 1.8f, 0.12f), IronDark, "Herse");
            }
            Proto.Cube(t, new Vector3(0f, GateHeight - 1.3f, z), new Vector3(GateWidth, 0.12f, 0.12f), IronDark, "Herse");
            Proto.EndVisualOnly();
        }

        /// <summary>
        /// LE DONJON. Il n'est plus plein : sa porte est ouverte, et derriere il y a
        /// la SALLE DU TRONE -- dix-sept metres de long, huit de haut, des piliers,
        /// deux braseros, et un trone vide au fond (meublee par CastleDecor).
        ///
        /// Construction : quatre murs de 1,6 m (celui du sud perce de la porte), un
        /// plancher sureleve de 0,9 m auquel on monte par trois marches, et au-dessus
        /// de la salle un bloc plein de dix-neuf metres -- les etages qu'on ne visite
        /// pas. Vu de dehors, rien n'a change : c'est toujours une masse de 28 m.
        /// </summary>
        static void BuildKeep(Transform t)
        {
            Vector3 c = KeepCentre;
            float hx = KeepHalfWidth, hz = KeepHalfDepth;
            float thick = 1.6f;
            Vector3 p1 = c + new Vector3(-hx + thick * 0.5f, 0f, hz - thick * 0.5f);
            Vector3 p2 = c + new Vector3(hx - thick * 0.5f, 0f, hz - thick * 0.5f);
            Vector3 p3 = c + new Vector3(hx - thick * 0.5f, 0f, -hz + thick * 0.5f);
            Vector3 p4 = c + new Vector3(-hx + thick * 0.5f, 0f, -hz + thick * 0.5f);

            // Les murs de la salle, du sol jusqu'au plafond. Les murs lateraux sont
            // allonges d'une demi-epaisseur : sinon chaque coin aurait un trou carre.
            Vector3 ext = new Vector3(0f, 0f, thick * 0.5f);
            Segment(t, p1 - new Vector3(thick * 0.5f, 0f, 0f), p2 + new Vector3(thick * 0.5f, 0f, 0f),
                    -1.5f, HallCeiling, thick, Stone, true, "Donjon_Nord");
            Segment(t, p2 + ext, p3 - ext, -1.5f, HallCeiling, thick, Stone, true, "Donjon_Est");
            Segment(t, p4 - ext, p1 + ext, -1.5f, HallCeiling, thick, Stone, true, "Donjon_Ouest");
            WallWithDoor(t, p3 + new Vector3(thick * 0.5f, 0f, 0f), p4 - new Vector3(thick * 0.5f, 0f, 0f),
                         -1.5f, HallCeiling, thick, 3.6f, HallFloor + 5.2f, Stone, "Donjon_Sud");

            // Le plancher de la salle, et les trois marches du perron.
            // Il va jusqu'au bord EXTERIEUR du seuil (z = 11) : arrete au milieu du
            // mur, il laissait sous la porte une fosse de 0,9 m ou le joueur restait
            // coince (on ne remonte seul que 0,42 m).
            float floorFront = c.z - hz, floorBack = c.z + hz - thick;
            Proto.Cube(t, new Vector3(c.x, HallFloor * 0.5f - 0.25f, (floorFront + floorBack) * 0.5f),
                       new Vector3(hx * 2f - thick, HallFloor + 0.5f, floorBack - floorFront), Paving, "Plancher");
            float doorZ = c.z - hz;
            for (int i = 0; i < 3; i++)
            {
                float top = HallFloor * (i + 1) / 3f;
                float z0 = doorZ - 2.7f + i * 0.9f;
                Proto.Cube(t, new Vector3(c.x, top * 0.5f - 0.1f, z0 + 0.45f),
                           new Vector3(7f - i * 0.6f, top + 0.2f, 0.9f), StoneDark, "Marche");
            }

            // Au-dessus de la salle, les etages pleins.
            Proto.Cube(t, new Vector3(c.x, (HallCeiling + KeepHeight) * 0.5f, c.z),
                       new Vector3(hx * 2f, KeepHeight - HallCeiling, hz * 2f), Stone, "Donjon");

            // Contreforts : ils cassent la facade, et le pied du donjon parait ancre.
            for (int i = -1; i <= 1; i += 2)
            {
                Proto.Cube(t, new Vector3(c.x + i * 6f, 6f, c.z - 8.7f), new Vector3(2.2f, 13.5f, 1.8f), StoneDark, "Contrefort");
                Proto.Cube(t, new Vector3(c.x + i * 6f, 13.2f, c.z - 8.4f), new Vector3(1.8f, 2.4f, 1.2f), StoneDark, "Contrefort");
            }

            Proto.BeginVisualOnly();
            // Des fenetres hautes, noires. Sauf une, tout en haut a droite, qui luit
            // d'une lumiere froide. Il y a quelqu'un la-haut.
            for (int k = -1; k <= 1; k++)
            {
                for (int row = 0; row < 6; row++)
                {
                    float y = 16f + row * 5.4f;
                    GameObject win = Proto.Cube(t, new Vector3(c.x + k * 5.5f, y, c.z - 8.02f), new Vector3(1.1f, 2.6f, 0.08f), IronDark, "Fenetre");
                    Proto.Cube(t, new Vector3(c.x + k * 5.5f, y - 1.45f, c.z - 8.12f), new Vector3(1.6f, 0.22f, 0.3f), StoneDark, "Appui");
                    if (k == 1 && row == 4) win.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.62f, 0.74f, 0.95f), 1.4f);
                }
            }
            // Deux cordons de pierre qui ceinturent le donjon : ils coupent sa hauteur
            // en etages lisibles.
            Proto.Cube(t, new Vector3(c.x, 14.2f, c.z), new Vector3(hx * 2f + 0.5f, 0.45f, hz * 2f + 0.5f), StoneDark, "Cordon");
            Proto.Cube(t, new Vector3(c.x, 29.5f, c.z), new Vector3(hx * 2f + 0.5f, 0.45f, hz * 2f + 0.5f), StoneDark, "Cordon");
            Proto.EndVisualOnly();

            Vector3 a1 = c + new Vector3(-10f, 0f, 8f), a2 = c + new Vector3(10f, 0f, 8f);
            Vector3 a3 = c + new Vector3(10f, 0f, -8f), a4 = c + new Vector3(-10f, 0f, -8f);
            Crenellate(t, a1, a2, KeepHeight, 1.4f, 60);
            Crenellate(t, a2, a3, KeepHeight, 1.4f, 61);
            Crenellate(t, a3, a4, KeepHeight, 1.4f, 62);
            Crenellate(t, a4, a1, KeepHeight, 1.4f, 63);

            // Le grand toit d'ardoise du donjon, et sa fleche : la silhouette qui
            // domine tout, qu'on devine au-dessus des arbres quand la brume s'ouvre.
            Proto.Cone(t, new Vector3(c.x, KeepHeight + 0.2f, c.z), 7.2f, 14f, Slate, "Toit du donjon", 8);
            Proto.Cone(t, new Vector3(c.x, KeepHeight + 13.6f, c.z), 0.3f, 7f, IronDark, "Fleche du donjon", 4);

            // Quatre echauguettes aux coins du sommet, coiffees de fleches d'ardoise.
            Vector3[] corners = { a1, a2, a3, a4 };
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 q = corners[i];
                Proto.BeginVisualOnly();
                Proto.Cylinder(t, new Vector3(q.x, KeepHeight + 2f, q.z), new Vector3(2.8f, 2.2f, 2.8f), Stone, "Echauguette");
                Proto.EndVisualOnly();
                Proto.Cone(t, new Vector3(q.x, KeepHeight + 4.2f, q.z), 1.9f, 4.2f, Slate, "Fleche");
            }
        }

        static void BuildCourtyard(Transform t)
        {
            // Un dallage sombre sur toute la cour. Pas de collider : le sol du terrain,
            // aplani a zero, est juste dessous.
            Proto.BeginVisualOnly();
            float inner = HalfSize - WallThickness * 0.5f;
            // Dessus du dallage a 4 cm, de l'allee a 6 cm : deux surfaces a la meme
            // hauteur que le sol se disputeraient chaque pixel et scintilleraient.
            Proto.Cube(t, new Vector3(0f, -0.01f, 0f), new Vector3(inner * 2f, 0.1f, inner * 2f), Paving, "Dallage");
            // L'allee de la porte a la stele, un ton plus clair : elle guide sans rien dire.
            Proto.Cube(t, new Vector3(0f, 0.01f, -HalfSize * 0.5f - 2f),
                       new Vector3(GateWidth - 1f, 0.1f, HalfSize - 4f), StoneDark, "Allee");
            Proto.EndVisualOnly();
        }

        // ------------------------------------------------------------------ reserves

        /// <summary>
        /// Une reserve : quatre murs, un toit, une porte, et des caisses de Fer ancien
        /// au fond. On y entre vraiment -- c'est un lieu, pas un objet.
        /// </summary>
        static void Storeroom(Transform t, Vector3 centre, float width, float depth, Side door, GameConfig cfg)
        {
            float hx = width * 0.5f, hz = depth * 0.5f;
            float height = 4.6f, thick = 0.5f;
            Vector3 c = centre;

            Vector3 p1 = c + new Vector3(-hx, 0f, hz), p2 = c + new Vector3(hx, 0f, hz);
            Vector3 p3 = c + new Vector3(hx, 0f, -hz), p4 = c + new Vector3(-hx, 0f, -hz);

            // Nord (p1-p2), Est (p2-p3), Sud (p3-p4), Ouest (p4-p1)
            Build(t, p1, p2, door == Side.PlusZ, height, thick);
            Build(t, p2, p3, door == Side.PlusX, height, thick);
            Build(t, p3, p4, door == Side.MinusZ, height, thick);
            Build(t, p4, p1, door == Side.MinusX, height, thick);

            Proto.Cube(t, new Vector3(c.x, height + 0.25f, c.z), new Vector3(width + 0.6f, 0.5f, depth + 0.6f), Slate, "Toit");

            // Les caisses, contre le mur du fond (a l'oppose de la porte).
            Vector3 back = -DoorOutward(door);
            Vector3 along = new Vector3(back.z, 0f, -back.x);
            float reach = (door == Side.PlusX || door == Side.MinusX ? hx : hz) - 1.3f;
            float span = (door == Side.PlusX || door == Side.MinusX ? hz : hx) - 1.6f;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 at = c + back * reach + along * (i * span * 0.7f);
                IronCrate(t, at, cfg);
            }

            // Une torche a la porte : on repere les reserves a leur lueur.
            Vector3 outward = DoorOutward(door);
            float doorReach = door == Side.PlusX || door == Side.MinusX ? hx : hz;
            Torch(t, c + outward * (doorReach + 0.5f) + along * 1.8f, 2.6f);
        }

        static void Build(Transform t, Vector3 a, Vector3 b, bool hasDoor, float height, float thick)
        {
            if (hasDoor) WallWithDoor(t, a, b, -0.5f, height, thick, 2.2f, 2.8f, StoneDark, "Reserve");
            else Segment(t, a, b, -0.5f, height, thick, StoneDark, true, "Reserve");
        }

        static Vector3 DoorOutward(Side side)
        {
            switch (side)
            {
                case Side.PlusX: return Vector3.right;
                case Side.MinusX: return Vector3.left;
                case Side.PlusZ: return Vector3.forward;
                default: return Vector3.back;
            }
        }

        /// <summary>
        /// Une caisse de Fer ancien. C'est un ResourceNode comme un autre : on maintient
        /// E pour en tirer du fer, elle se vide, puis elle se remplit a nouveau, lentement.
        /// </summary>
        static void IronCrate(Transform t, Vector3 at, GameConfig cfg)
        {
            GameObject go = new GameObject("Caisse de fer");
            go.transform.SetParent(t, false);
            go.transform.localPosition = at;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = new Vector3(1.1f, 1f, 1.1f);

            // UNE CAISSE DE FER ANCIEN : des planches (avec des jours entre elles),
            // des cornieres de fer rivetees, le couvercle entrouvert, et dedans des
            // lingots qui accrochent la lumiere des torches.
            Proto.BeginVisualOnly();
            GameObject visual = new GameObject("Visuel");
            visual.transform.SetParent(go.transform, false);
            Transform v = visual.transform;
            Color plank = Palette.Shade(Timber, 1.15f);
            Proto.Cube(v, new Vector3(0f, 0.06f, 0f), new Vector3(1f, 0.12f, 1f), Timber, "Fond");
            for (int side = 0; side < 4; side++)
            {
                Quaternion q = Quaternion.Euler(0f, side * 90f, 0f);
                for (int k = 0; k < 3; k++)
                {
                    GameObject board = Proto.Cube(v, q * new Vector3(0f, 0.2f + k * 0.26f, 0.47f), new Vector3(0.98f, 0.22f, 0.06f),
                                                  k % 2 == 0 ? plank : Timber, "Planche");
                    board.transform.localRotation = q;
                }
            }
            // Les cornieres, et une rangee de rivets.
            for (int c = 0; c < 4; c++)
            {
                float a = c * 90f + 45f;
                Vector3 corner = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 0.69f);
                Proto.Cube(v, new Vector3(corner.x, 0.45f, corner.z), new Vector3(0.1f, 0.92f, 0.1f), IronDark, "Corniere");
                for (int k = 0; k < 3; k++)
                    Proto.Cube(v, new Vector3(corner.x * 1.02f, 0.18f + k * 0.28f, corner.z * 1.02f), new Vector3(0.04f, 0.04f, 0.04f),
                               new Color(0.35f, 0.33f, 0.3f), "Rivet");
            }
            Proto.Cube(v, new Vector3(0f, 0.3f, 0f), new Vector3(1.04f, 0.06f, 1.04f), IronDark, "Cerclage");
            // Le couvercle, souleve d'un cote et pose de travers.
            GameObject lid = Proto.Cube(v, new Vector3(0.15f, 0.98f, -0.1f), new Vector3(1.02f, 0.07f, 1.02f), plank, "Couvercle");
            lid.transform.localRotation = Quaternion.Euler(14f, 8f, -6f);
            // Les lingots : trapezes d'acier sombre, un reflet chaud sur l'arete.
            Color ingot = ResourceInfo.Tint(ResourceType.Iron);
            Material edge = MaterialFactory.GetGlow(new Color(0.9f, 0.62f, 0.35f), 0.5f);
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = new Vector3(-0.22f + (k % 2) * 0.4f, 0.86f + (k / 2) * 0.1f, -0.15f + (k / 2) * 0.22f);
                GameObject bar = Proto.Cube(v, p, new Vector3(0.34f, 0.1f, 0.15f), ingot, "Lingot");
                bar.transform.localRotation = Quaternion.Euler(0f, k * 12f - 10f, 0f);
                GameObject rim = Proto.Cube(v, p + new Vector3(0f, 0.052f, 0f), new Vector3(0.3f, 0.01f, 0.11f), ingot, "Reflet");
                rim.transform.localRotation = bar.transform.localRotation;
                rim.GetComponent<Renderer>().sharedMaterial = edge;
            }
            Proto.EndVisualOnly();

            ResourceNode node = go.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 1;
            node.harvestDuration = cfg != null ? cfg.harvestDuration : 1.15f;
            // Le fer du chateau NE REVIENT PAS : 54 lingots pour toute la Saison.
            // Sans ca, le meilleur plan etait de faire la navette chateau-mage avec du
            // fer et d'ignorer la foret. Rare veut dire fini ; en Phase 2, c'est ce
            // qui fera se croiser les joueurs dans les reserves.
            node.respawnDelay = 0f;
            node.Initialise(ResourceType.Iron, 6, visual.transform);
        }

        // ------------------------------------------------------------------ torches

        static void BuildTorches(Transform t)
        {
            float inner = HalfSize - WallThickness * 0.5f - 0.4f;
            float gz = -HalfSize;

            // La porte, dehors, devant les tours du chatelet (qui descendent jusqu'a
            // z = -44,5) : ce sont les premieres lumieres qu'on voit en arrivant.
            Torch(t, new Vector3(-GateWidth * 0.5f - 1.2f, 0f, gz - 6.2f), 3.2f);
            Torch(t, new Vector3(GateWidth * 0.5f + 1.2f, 0f, gz - 6.2f), 3.2f);

            // Autour de la stele.
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Torch(t, StelePosition + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 5.2f, 2.8f);
            }

            // Le long des murs, cote cour, espacees de facon irreguliere : certaines se
            // sont eteintes depuis longtemps, et c'est tant mieux. Positions choisies a
            // la main pour ne tomber ni dans une reserve ni dans une tour.
            float[] north = { 2f, 22f };
            float[] east = { 2f, 26f };
            float[] west = { -27f, -1f, 19f };
            for (int i = 0; i < north.Length; i++) Torch(t, new Vector3(north[i], 0f, inner), 3.4f);
            for (int i = 0; i < east.Length; i++) Torch(t, new Vector3(inner, 0f, east[i]), 3.4f);
            for (int i = 0; i < west.Length; i++) Torch(t, new Vector3(-inner, 0f, west[i]), 3.4f);
        }

        /// <summary>
        /// Une torche : un poteau, une flamme, et une lumiere qui vacille. Pas d'ombre
        /// portee : une vingtaine de lumieres a ombres couterait tres cher.
        /// </summary>
        public static void Torch(Transform t, Vector3 at, float height)
        {
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(at.x, height * 0.5f, at.z), new Vector3(0.16f, height, 0.16f), Timber, "Torche");
            Proto.Cube(t, new Vector3(at.x, height + 0.12f, at.z), new Vector3(0.3f, 0.12f, 0.3f), IronDark, "Coupe");
            GameObject flame = Proto.Cube(t, new Vector3(at.x, height + 0.38f, at.z),
                                          new Vector3(0.22f, 0.4f, 0.22f), new Color(1f, 0.62f, 0.22f), "Flamme");
            Proto.EndVisualOnly();

            Renderer r = flame.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.6f, 0.22f), 2.2f);
            flame.AddComponent<Flame>();

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(at.x, height + 0.6f, at.z);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.70f, 0.40f);
            light.intensity = 1.5f;
            light.range = 10f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
        }
    }
}
