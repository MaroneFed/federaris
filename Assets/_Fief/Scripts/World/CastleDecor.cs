using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QUI REND LE CHATEAU INOUBLIABLE. Castle.cs pose la masse (murs, tours,
    /// donjon) ; ici on pose ce qu'on regarde.
    ///
    /// Avec vingt metres de brume, on ne voit JAMAIS le chateau en entier. Ce qui le
    /// rend grandiose, c'est donc ce qu'on croise a moins de vingt metres, dans
    /// l'ordre ou on le croise :
    ///
    ///   1. L'ALLEE DES ROIS. Deux braseros au bout, dans la foret, puis des dalles
    ///      brisees et huit rois de pierre encapuchonnes, l'epee plantee devant eux.
    ///      L'un n'a plus de tete, un autre est tombe. On sait qu'on approche avant
    ///      d'avoir vu le moindre mur.
    ///   2. LA PORTE, grande ouverte, ses deux vantaux rabattus vers la cour.
    ///   3. LA COUR : un puits, un arbre mort geant, une charrette brisee, des
    ///      tonneaux, des eboulis sous la breche.
    ///   4. LA SALLE DU TRONE, au fond du donjon : des piliers, un tapis use, un
    ///      lustre eteint, deux braseros, et un trone vide.
    ///
    /// Tout est fait de cubes et de cones : zero modele 3D sur mesure (loi n°1).
    /// Les colliders sont poses a la main, simples : un bloc par objet, jamais un
    /// par piece. Rien ne se traverse, et la physique ne coute rien.
    /// </summary>
    public static class CastleDecor
    {
        static readonly Color StatueStone = new Color(0.36f, 0.36f, 0.34f);
        static readonly Color StatueShade = new Color(0.29f, 0.29f, 0.28f);
        static readonly Color Moss = new Color(0.22f, 0.27f, 0.18f);
        static readonly Color Stone = new Color(0.31f, 0.31f, 0.30f);
        static readonly Color StoneDark = new Color(0.23f, 0.23f, 0.23f);
        static readonly Color Paving = new Color(0.25f, 0.25f, 0.24f);
        static readonly Color Timber = new Color(0.22f, 0.16f, 0.11f);
        static readonly Color TimberDark = new Color(0.16f, 0.12f, 0.09f);
        static readonly Color Iron = new Color(0.12f, 0.12f, 0.13f);
        static readonly Color Crimson = new Color(0.34f, 0.07f, 0.07f);
        static readonly Color CrimsonDark = new Color(0.24f, 0.05f, 0.05f);
        static readonly Color FadedGold = new Color(0.52f, 0.42f, 0.20f);
        static readonly Color Water = new Color(0.03f, 0.04f, 0.05f);
        static readonly Color Fire = new Color(1f, 0.58f, 0.2f);

        public static void Build(Transform t, GameConfig cfg)
        {
            System.Random rng = new System.Random(cfg != null ? cfg.worldSeed * 7 + 3 : 7);

            Avenue(t, rng);
            GateLeaves(t);
            Banners(t);
            ThroneRoom(t);
            Courtyard(t, rng);
            CastleDetails.Build(t, rng);
        }

        // ================================================================== 1. l'allee

        static void Avenue(Transform t, System.Random rng)
        {
            // Des dalles brisees, une sur cinq manquante, qui suivent le sol.
            Proto.BeginVisualOnly();
            for (float z = -45.5f; z > Castle.AvenueEnd + 2f; z -= 1.55f)
            {
                for (float x = -2.2f; x <= 2.21f; x += 1.47f)
                {
                    if (rng.NextDouble() < 0.2) continue;
                    float px = x + R(rng, -0.15f, 0.15f), pz = z + R(rng, -0.15f, 0.15f);
                    float size = R(rng, 1.1f, 1.45f);
                    Color c = rng.NextDouble() < 0.25 ? Moss : (rng.NextDouble() < 0.5 ? Paving : StoneDark);
                    GameObject slab = Proto.Cube(t, new Vector3(px, Ground.Sample(px, pz) + 0.0f, pz),
                                                 new Vector3(size, 0.12f, size * R(rng, 0.85f, 1.1f)), c, "Dalle");
                    slab.transform.localRotation = Quaternion.Euler(R(rng, -3f, 3f), R(rng, -9f, 9f), R(rng, -3f, 3f));
                }
            }
            Proto.EndVisualOnly();

            // Quatre paires de rois. Celui de la troisieme paire, a gauche, n'a plus
            // de tete ; le dernier a droite est tombe de son socle.
            float[] rows = { -54f, -64f, -74f, -84f };
            for (int i = 0; i < rows.Length; i++)
            {
                Statue(t, new Vector3(-6.5f, 0f, rows[i]), 90f, i == 2 ? StatueState.Headless : StatueState.Standing, rng);
                Statue(t, new Vector3(6.5f, 0f, rows[i]), -90f, i == 3 ? StatueState.Fallen : StatueState.Standing, rng);
            }

            // Deux braseros au bout de l'allee, dans la foret : ce sont eux qu'on voit
            // d'abord, deux taches de feu dans la brume.
            float endZ = Castle.AvenueEnd + 3f;
            Brazier(t, new Vector3(-3.6f, Ground.Sample(-3.6f, endZ), endZ));
            Brazier(t, new Vector3(3.6f, Ground.Sample(3.6f, endZ), endZ));

            // Et deux au pied du perron du donjon.
            Brazier(t, new Vector3(-4.6f, 0f, Castle.KeepCentre.z - Castle.KeepHalfDepth - 3.6f));
            Brazier(t, new Vector3(4.6f, 0f, Castle.KeepCentre.z - Castle.KeepHalfDepth - 3.6f));
        }

        enum StatueState { Standing, Headless, Fallen }

        /// <summary>
        /// Un roi de pierre, quatre metres et demi, encapuchonne, les mains sur la
        /// garde d'une epee plantee devant lui. Meme silhouette que le mage, en
        /// pierre : on se demande si ce sont des rois, ou d'autres mages.
        /// </summary>
        static void Statue(Transform t, Vector3 at, float yaw, StatueState state, System.Random rng)
        {
            float ground = Ground.Sample(at.x, at.z);
            GameObject root = new GameObject("Roi de pierre");
            root.transform.SetParent(t, false);
            root.transform.localPosition = new Vector3(at.x, ground, at.z);
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform r = root.transform;

            // Le socle bloque, la statue aussi (un seul bloc pour toute la figure).
            Proto.Cube(r, new Vector3(0f, 0.35f, 0f), new Vector3(2f, 1.1f, 2f), StatueShade, "Socle");
            Proto.BeginVisualOnly();
            Proto.Cube(r, new Vector3(0f, 0.95f, 0f), new Vector3(1.8f, 0.14f, 1.8f), StatueStone, "Socle");
            Proto.EndVisualOnly();

            GameObject figureGo = new GameObject("Figure");
            figureGo.transform.SetParent(r, false);
            Transform f = figureGo.transform;
            if (state == StatueState.Fallen)
            {
                // Tombe a cote de son socle, couche le long de l'allee, a moitie enfoui.
                f.localPosition = new Vector3(1.9f, 0.75f, -1.2f);
                f.localRotation = Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(0f, 18f, 0f);
            }
            else
            {
                f.localPosition = new Vector3(0f, 1.02f, 0f);
            }

            const float k = 1.9f;
            Proto.BeginVisualOnly();
            float[] widths = { 1.05f, 0.86f, 0.68f, 0.52f };
            for (int i = 0; i < widths.Length; i++)
            {
                GameObject slice = Proto.Cube(f, new Vector3(0f, (0.25f + i * 0.46f) * k, 0f),
                                              new Vector3(widths[i] * k, 0.5f * k, widths[i] * k),
                                              rng.NextDouble() < 0.2 ? Moss : (i % 2 == 0 ? StatueStone : StatueShade), "Robe");
                slice.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }
            Proto.Cube(f, new Vector3(0f, 2.0f * k, 0f), new Vector3(0.8f * k, 0.22f * k, 0.52f * k), StatueShade, "Epaules");
            if (state != StatueState.Headless)
            {
                GameObject hood = Proto.Cube(f, new Vector3(0f, 2.3f * k, 0f), new Vector3(0.46f * k, 0.5f * k, 0.46f * k), StatueStone, "Capuche");
                hood.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                GameObject peak = Proto.Cube(f, new Vector3(0f, 2.6f * k, -0.06f * k), new Vector3(0.26f * k, 0.3f * k, 0.26f * k), StatueStone, "Pointe");
                peak.transform.localRotation = Quaternion.Euler(-14f, 45f, 0f);
                Proto.Cube(f, new Vector3(0f, 2.26f * k, 0.2f * k), new Vector3(0.3f * k, 0.3f * k, 0.08f * k), new Color(0.08f, 0.08f, 0.08f), "Visage");
            }
            else
            {
                // La tete est par terre, devant le socle.
                GameObject head = Proto.Cube(r, new Vector3(-0.8f, 0.4f, 1.6f), new Vector3(0.8f, 0.9f, 0.8f), StatueStone, "Tete tombee");
                head.transform.localRotation = Quaternion.Euler(20f, 50f, 74f);
            }

            // L'epee plantee devant, les mains sur la garde.
            Proto.Cube(f, new Vector3(0f, 1.05f * k, 0.62f * k), new Vector3(0.1f * k, 1.9f * k, 0.04f * k), StatueShade, "Lame");
            Proto.Cube(f, new Vector3(0f, 2.0f * k, 0.62f * k), new Vector3(0.62f * k, 0.08f * k, 0.08f * k), StatueShade, "Garde");
            Proto.Cube(f, new Vector3(0f, 2.12f * k, 0.6f * k), new Vector3(0.34f * k, 0.18f * k, 0.2f * k), StatueStone, "Mains");
            Proto.EndVisualOnly();

            if (state == StatueState.Fallen)
                Proto.Blocker(f, new Vector3(0f, 2.3f * k * 0.5f, 0f), new Vector3(1.9f, 2.3f * k, 1.9f), "Statue");
            else
                Proto.Blocker(f, new Vector3(0f, 2.5f * k * 0.5f, 0.2f), new Vector3(1.9f, 2.5f * k, 2.2f), "Statue");
        }

        // ================================================================== 2. la porte

        /// <summary>Les deux vantaux, rabattus vers la cour. Solides : on ne passe pas au travers.</summary>
        static void GateLeaves(Transform t)
        {
            float inner = -Castle.HalfSize + Castle.WallThickness * 0.5f + 0.15f;
            float half = Castle.GateWidth * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject hinge = new GameObject("Vantail");
                hinge.transform.SetParent(t, false);
                hinge.transform.localPosition = new Vector3(side * half, 0f, inner);
                // Ferme, le vantail irait vers le centre (-side en x). Ouvert a 80
                // degres, il pointe dans la cour et laisse cinq metres et demi de passage.
                hinge.transform.localRotation = Quaternion.Euler(0f, side * 80f, 0f);
                Transform h = hinge.transform;

                float w = half - 0.1f;
                Proto.Cube(h, new Vector3(-side * w * 0.5f, 3.4f, 0f), new Vector3(w, 6.8f, 0.24f), Timber, "Vantail");
                Proto.BeginVisualOnly();
                for (int b = 0; b < 3; b++)
                    Proto.Cube(h, new Vector3(-side * w * 0.5f, 1.2f + b * 2.3f, 0.14f), new Vector3(w + 0.04f, 0.2f, 0.06f), Iron, "Penture");
                for (int p = 1; p < 4; p++)
                    Proto.Cube(h, new Vector3(-side * w * p / 4f, 3.4f, 0.13f), new Vector3(0.05f, 6.7f, 0.03f), TimberDark, "Joint");
                Proto.EndVisualOnly();
            }
        }

        // ================================================================== les etendards

        static void Banners(Transform t)
        {
            float face = Castle.KeepCentre.z - Castle.KeepHalfDepth - 0.06f;
            Banner(t, new Vector3(-2.8f, 14.5f, face), 0f);
            Banner(t, new Vector3(2.8f, 14.5f, face), 0f);

            float gateFace = -Castle.HalfSize - 0.5f - 4f - 0.06f;
            float off = Castle.GateWidth * 0.5f + 4.2f;
            Banner(t, new Vector3(-off, 12.5f, gateFace), 0f);
            Banner(t, new Vector3(off, 12.5f, gateFace), 0f);

            // Dans la salle du trone, au mur du fond, tournes vers l'entree.
            float back = Castle.KeepCentre.z + Castle.KeepHalfDepth - 1.6f - 0.12f;
            Banner(t, new Vector3(-3.8f, 8.2f, back), 180f);
            Banner(t, new Vector3(3.8f, 8.2f, back), 180f);
        }

        /// <summary>
        /// Un etendard en lambeaux : une tringle, puis trois bandes de longueurs
        /// differentes, comme dechirees. Une bande d'or passe, au milieu.
        /// </summary>
        static void Banner(Transform t, Vector3 top, float yaw)
        {
            GameObject root = new GameObject("Etendard");
            root.transform.SetParent(t, false);
            root.transform.localPosition = top;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform b = root.transform;

            Proto.BeginVisualOnly();
            Proto.Cube(b, new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0.12f, 0.12f), Iron, "Tringle");
            float[] lengths = { 5.2f, 6.1f, 4.4f };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.62f;
                float len = lengths[i];
                Proto.Cube(b, new Vector3(x, -len * 0.5f, -0.02f), new Vector3(0.6f, len, 0.04f),
                           i == 1 ? CrimsonDark : Crimson, "Toile");
                Proto.Cube(b, new Vector3(x, -1.3f, -0.05f), new Vector3(0.6f, 0.22f, 0.02f), FadedGold, "Galon");
            }
            // Le blason : un losange d'or passe.
            GameObject crest = Proto.Cube(b, new Vector3(0f, -2.6f, -0.06f), new Vector3(0.7f, 0.7f, 0.02f), FadedGold, "Blason");
            crest.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Proto.EndVisualOnly();
        }

        // ================================================================== 4. la salle du trone

        static void ThroneRoom(Transform t)
        {
            Vector3 c = Castle.KeepCentre;
            float floor = Castle.HallFloor;
            float ceiling = Castle.HallCeiling;
            float back = c.z + Castle.KeepHalfDepth - 1.6f;      // face interieure du mur du fond

            // Six piliers.
            float[] rows = { c.z - 4.5f, c.z - 0.2f, c.z + 4.1f };
            for (int i = 0; i < rows.Length; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 p = new Vector3(side * 4.8f, 0f, rows[i]);
                    Proto.Cube(t, new Vector3(p.x, (floor + ceiling) * 0.5f, p.z), new Vector3(1.1f, ceiling - floor, 1.1f), Stone, "Pilier");
                    Proto.BeginVisualOnly();
                    Proto.Cube(t, new Vector3(p.x, floor + 0.3f, p.z), new Vector3(1.5f, 0.6f, 1.5f), StoneDark, "Base");
                    Proto.Cube(t, new Vector3(p.x, ceiling - 0.35f, p.z), new Vector3(1.5f, 0.7f, 1.5f), StoneDark, "Chapiteau");
                    Proto.EndVisualOnly();
                }
            }

            // Le tapis, use jusqu'a la trame au milieu.
            Proto.BeginVisualOnly();
            float carpetFrom = c.z - Castle.KeepHalfDepth + 0.2f, carpetTo = back - 2.2f;
            Proto.Cube(t, new Vector3(0f, floor + 0.02f, (carpetFrom + carpetTo) * 0.5f),
                       new Vector3(2.4f, 0.04f, carpetTo - carpetFrom), CrimsonDark, "Tapis");
            Proto.Cube(t, new Vector3(0f, floor + 0.035f, (carpetFrom + carpetTo) * 0.5f),
                       new Vector3(2.0f, 0.04f, carpetTo - carpetFrom - 0.4f), Crimson, "Tapis");
            Proto.Cube(t, new Vector3(0.3f, floor + 0.05f, c.z - 1f), new Vector3(1.1f, 0.04f, 2.4f), new Color(0.2f, 0.13f, 0.1f), "Usure");
            Proto.EndVisualOnly();

            // L'estrade, deux marches, et le trone.
            // Marches de 25 cm (le joueur en monte 42 seul) : dessus a +0,25 puis +0,5.
            Proto.Cube(t, new Vector3(0f, floor + 0.025f, back - 1.0f), new Vector3(5.2f, 0.45f, 2.0f), StoneDark, "Estrade");
            Proto.Cube(t, new Vector3(0f, floor + 0.15f, back - 0.6f), new Vector3(3.6f, 0.7f, 1.2f), StoneDark, "Estrade");
            Throne(t, new Vector3(0f, floor + 0.5f, back - 0.55f));

            // Deux braseros de part et d'autre de l'estrade.
            Brazier(t, new Vector3(-2.9f, floor, back - 2.6f));
            Brazier(t, new Vector3(2.9f, floor, back - 2.6f));

            // Un lustre de fer, eteint, pendu au plafond par quatre chaines.
            Proto.BeginVisualOnly();
            Vector3 hub = new Vector3(0f, ceiling - 2.6f, c.z - 0.2f);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                GameObject seg = Proto.Cube(t, hub + new Vector3(Mathf.Cos(a) * 1.4f, 0f, Mathf.Sin(a) * 1.4f),
                                            new Vector3(0.12f, 0.12f, 0.95f), Iron, "Lustre");
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                if (i % 2 == 0)
                    Proto.Cube(t, hub + new Vector3(Mathf.Cos(a) * 1.4f, 0.2f, Mathf.Sin(a) * 1.4f),
                               new Vector3(0.1f, 0.3f, 0.1f), new Color(0.55f, 0.52f, 0.44f), "Chandelle");
            }
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 from = hub + new Vector3(Mathf.Cos(a) * 1.4f, 0f, Mathf.Sin(a) * 1.4f);
                Vector3 to = new Vector3(hub.x, ceiling, hub.z);
                Vector3 mid = (from + to) * 0.5f;
                GameObject chain = Proto.Cube(t, mid, new Vector3(0.05f, (to - from).magnitude, 0.05f), Iron, "Chaine");
                chain.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Un trone de bois noir, haut dossier, garni de fer. Vide.</summary>
        static void Throne(Transform t, Vector3 seatBase)
        {
            Proto.BeginVisualOnly();
            Proto.Cube(t, seatBase + new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 0.2f, 1.0f), TimberDark, "Siege");
            Proto.Cube(t, seatBase + new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.4f, 0.9f), Timber, "Coffre");
            Proto.Cube(t, seatBase + new Vector3(0f, 1.9f, 0.42f), new Vector3(1.3f, 3.2f, 0.2f), TimberDark, "Dossier");
            Proto.Cube(t, seatBase + new Vector3(-0.72f, 0.85f, 0f), new Vector3(0.16f, 0.6f, 1.0f), Timber, "Accoudoir");
            Proto.Cube(t, seatBase + new Vector3(0.72f, 0.85f, 0f), new Vector3(0.16f, 0.6f, 1.0f), Timber, "Accoudoir");
            Proto.Cube(t, seatBase + new Vector3(0f, 2.6f, 0.3f), new Vector3(1.36f, 0.14f, 0.06f), FadedGold, "Ferrure");
            Proto.Cube(t, seatBase + new Vector3(0f, 1.4f, 0.3f), new Vector3(1.36f, 0.14f, 0.06f), Iron, "Ferrure");
            Proto.EndVisualOnly();
            Proto.Cone(t, seatBase + new Vector3(-0.55f, 3.5f, 0.42f), 0.16f, 0.7f, FadedGold, "Pinacle", 4);
            Proto.Cone(t, seatBase + new Vector3(0.55f, 3.5f, 0.42f), 0.16f, 0.7f, FadedGold, "Pinacle", 4);
            Proto.Cone(t, seatBase + new Vector3(0f, 3.5f, 0.42f), 0.22f, 1.1f, FadedGold, "Pinacle", 4);
            Proto.Blocker(t, seatBase + new Vector3(0f, 1.7f, 0.1f), new Vector3(1.6f, 3.4f, 1.2f), "Trone");
            Throne.Build(t, seatBase + new Vector3(0f, 0.6f, -0.9f));
        }

        // ================================================================== 3. la cour

        static void Courtyard(Transform t, System.Random rng)
        {
            Well(t, new Vector3(-18f, 0f, 4f));
            GiantDeadTree(t, new Vector3(20f, 0f, -24f));
            Cart(t, new Vector3(13f, 0f, 5f), 28f);
            Barrels(t, new Vector3(-25.5f, 0f, -24.5f), rng);
            Barrels(t, new Vector3(26.5f, 0f, -3.5f), rng);

            // Les eboulis de la breche, dedans et dehors.
            float x = Castle.HalfSize;
            float mid = (Castle.BreachFrom + Castle.BreachTo) * 0.5f;
            Rubble(t, new Vector3(x - 3.2f, 0f, mid), rng);
            Rubble(t, new Vector3(x + 3.2f, 0f, mid), rng);

            // Au sommet de la breche, des blocs dechausses, en dents de scie.
            Proto.BeginVisualOnly();
            for (float z = Castle.BreachFrom + 0.4f; z < Castle.BreachTo; z += 1.1f)
            {
                float hgt = R(rng, 0.3f, 1.6f);
                GameObject block = Proto.Cube(t, new Vector3(x + R(rng, -0.6f, 0.6f), Castle.BreachHeight + hgt * 0.5f, z),
                                              new Vector3(R(rng, 1.2f, 2.2f), hgt, 1f), Stone, "Bloc");
                block.transform.localRotation = Quaternion.Euler(R(rng, -8f, 8f), R(rng, -12f, 12f), R(rng, -10f, 10f));
            }
            Proto.EndVisualOnly();
        }

        static void Well(Transform t, Vector3 at)
        {
            Proto.BeginVisualOnly();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                GameObject s = Proto.Cube(t, at + new Vector3(Mathf.Cos(a) * 1.15f, 0.45f, Mathf.Sin(a) * 1.15f),
                                          new Vector3(0.45f, 0.9f, 1.0f), i % 3 == 0 ? Moss : Stone, "Margelle");
                s.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            Proto.Cylinder(t, at + new Vector3(0f, 0.35f, 0f), new Vector3(2.0f, 0.02f, 2.0f), Water, "Eau noire");
            for (int side = -1; side <= 1; side += 2)
                Proto.Cube(t, at + new Vector3(side * 1.25f, 1.6f, 0f), new Vector3(0.18f, 2.4f, 0.18f), Timber, "Montant");
            Proto.Cube(t, at + new Vector3(0f, 2.75f, 0f), new Vector3(2.8f, 0.14f, 0.14f), Timber, "Treuil");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject roof = Proto.Cube(t, at + new Vector3(0f, 3.15f, side * 0.55f), new Vector3(3.0f, 0.08f, 1.35f), TimberDark, "Toit");
                roof.transform.localRotation = Quaternion.Euler(side * -32f, 0f, 0f);
            }
            Proto.Cube(t, at + new Vector3(0f, 1.9f, 0f), new Vector3(0.02f, 1.7f, 0.02f), Iron, "Corde");
            Proto.Cube(t, at + new Vector3(0f, 0.95f, 0f), new Vector3(0.36f, 0.4f, 0.36f), Timber, "Seau");
            Proto.EndVisualOnly();
            Proto.Blocker(t, at + new Vector3(0f, 0.8f, 0f), new Vector3(2.9f, 1.6f, 2.9f), "Puits");
        }

        /// <summary>
        /// Le meme arbre mort que dans la foret, deux fois et demie plus grand. Il a
        /// pousse dans la cour quand plus personne ne la balayait.
        /// </summary>
        static void GiantDeadTree(Transform t, Vector3 at)
        {
            TreeInfo info;
            Mesh mesh = TreeMesh.Build(TreeKind.Dead, 4242, out info);
            GameObject go = new GameObject("Arbre mort de la cour");
            go.transform.SetParent(t, false);
            go.transform.localPosition = at + new Vector3(0f, -0.2f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 37f, 0f);
            go.transform.localScale = Vector3.one * 2.5f;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = Forest.TreeMaterials(Palette.DeadWood[2], Palette.DeadWood[0]);

            // Valeurs locales : elles sont multipliees par l'echelle de l'objet.
            CapsuleCollider trunk = go.AddComponent<CapsuleCollider>();
            trunk.radius = info.trunkRadius * 1.1f;
            trunk.height = 4f;
            trunk.center = new Vector3(info.trunkCentre.x, 2f, info.trunkCentre.z);
        }

        static void Cart(Transform t, Vector3 at, float yaw)
        {
            GameObject root = new GameObject("Charrette");
            root.transform.SetParent(t, false);
            root.transform.localPosition = at;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform c = root.transform;

            // Une roue manque : la charrette s'est affaissee sur un cote.
            Proto.BeginVisualOnly();
            GameObject bed = Proto.Cube(c, new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 0.14f, 3.0f), Timber, "Plateau");
            bed.transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject rail = Proto.Cube(c, new Vector3(side * 0.85f, 1.05f + (side > 0 ? -0.33f : 0.33f), 0f),
                                             new Vector3(0.1f, 0.5f, 3.0f), TimberDark, "Ridelle");
                rail.transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            }
            GameObject wheel = Proto.Cylinder(c, new Vector3(-1.05f, 0.62f, -0.4f), new Vector3(1.25f, 0.06f, 1.25f), TimberDark, "Roue");
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject lost = Proto.Cylinder(c, new Vector3(1.9f, 0.05f, 0.9f), new Vector3(1.25f, 0.05f, 1.25f), TimberDark, "Roue tombee");
            lost.transform.localRotation = Quaternion.Euler(4f, 0f, 2f);
            GameObject shaft = Proto.Cube(c, new Vector3(0.3f, 0.3f, 2.3f), new Vector3(0.1f, 0.1f, 2.2f), Timber, "Brancard");
            shaft.transform.localRotation = Quaternion.Euler(-12f, 6f, 0f);
            Proto.EndVisualOnly();
            Proto.Blocker(c, new Vector3(0f, 0.7f, 0.2f), new Vector3(2.2f, 1.4f, 3.4f), "Charrette");
        }

        static void Barrels(Transform t, Vector3 at, System.Random rng)
        {
            Proto.BeginVisualOnly();
            Vector3[] spots = { new Vector3(0f, 0f, 0f), new Vector3(0.95f, 0f, 0.2f), new Vector3(0.4f, 0f, 0.95f) };
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 p = at + spots[i];
                Proto.Cylinder(t, p + new Vector3(0f, 0.55f, 0f), new Vector3(0.85f, 0.55f, 0.85f), Timber, "Tonneau");
                Proto.Cylinder(t, p + new Vector3(0f, 0.25f, 0f), new Vector3(0.88f, 0.04f, 0.88f), Iron, "Cercle");
                Proto.Cylinder(t, p + new Vector3(0f, 0.85f, 0f), new Vector3(0.88f, 0.04f, 0.88f), Iron, "Cercle");
            }
            // Un tonneau renverse, a cote.
            GameObject lying = Proto.Cylinder(t, at + new Vector3(-0.9f, 0.42f, 0.6f), new Vector3(0.85f, 0.55f, 0.85f), TimberDark, "Tonneau");
            lying.transform.localRotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 180f, 0f);
            Proto.EndVisualOnly();
            Proto.Blocker(t, at + new Vector3(0.2f, 0.6f, 0.45f), new Vector3(2.8f, 1.2f, 2.2f), "Tonneaux");
        }

        /// <summary>
        /// Un tas d'eboulis : quelques gros blocs SOLIDES (on ne traverse pas une
        /// pierre de soixante centimetres), et du menu gravier purement visuel.
        /// </summary>
        static void Rubble(Transform t, Vector3 at, System.Random rng)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = at + new Vector3(R(rng, -1.2f, 1.2f), 0f, R(rng, -3f, 3f));
                float s = R(rng, 0.7f, 1.3f);
                GameObject block = Proto.Cube(t, p + new Vector3(0f, s * 0.35f, 0f), new Vector3(s * 1.3f, s * 0.8f, s), i % 2 == 0 ? Stone : StoneDark, "Bloc");
                block.transform.localRotation = Quaternion.Euler(R(rng, -15f, 15f), R(rng, 0f, 90f), R(rng, -15f, 15f));
            }
            Proto.BeginVisualOnly();
            for (int i = 0; i < 14; i++)
            {
                Vector3 p = at + new Vector3(R(rng, -2f, 2f), 0f, R(rng, -4f, 4f));
                float s = R(rng, 0.2f, 0.45f);
                GameObject pebble = Proto.Cube(t, p + new Vector3(0f, s * 0.3f, 0f), new Vector3(s * 1.2f, s * 0.7f, s), i % 3 == 0 ? Moss : Stone, "Gravat");
                pebble.transform.localRotation = Quaternion.Euler(R(rng, -30f, 30f), R(rng, 0f, 180f), R(rng, -30f, 30f));
            }
            Proto.EndVisualOnly();
        }

        // ================================================================== le feu

        /// <summary>
        /// Un brasero : une vasque de fer sur trois pieds, des braises, des flammes qui
        /// dansent, des etincelles qui montent, et une lumiere qui vacille. C'est la
        /// plus belle lumiere du jeu -- et la seule, avec les torches, qui soit chaude.
        /// </summary>
        public static void Brazier(Transform t, Vector3 at)
        {
            Proto.BeginVisualOnly();
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * 360f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                GameObject leg = Proto.Cube(t, at + dir * 0.28f + new Vector3(0f, 0.5f, 0f), new Vector3(0.07f, 1.05f, 0.07f), Iron, "Pied");
                leg.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-14f, 0f, 0f);
            }
            Proto.Cylinder(t, at + new Vector3(0f, 1.05f, 0f), new Vector3(0.95f, 0.12f, 0.95f), Iron, "Vasque");
            Proto.Cylinder(t, at + new Vector3(0f, 1.2f, 0f), new Vector3(1.05f, 0.04f, 1.05f), Iron, "Rebord");
            GameObject coals = Proto.Cylinder(t, at + new Vector3(0f, 1.2f, 0f), new Vector3(0.8f, 0.03f, 0.8f), Fire, "Braises");
            coals.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.9f, 0.3f, 0.1f), 1.8f);

            Material flameMat = MaterialFactory.GetGlow(Fire, 2.4f);
            Material coreMat = MaterialFactory.GetGlow(new Color(1f, 0.82f, 0.45f), 2.6f);
            Vector3[] flames = { new Vector3(0f, 1.55f, 0f), new Vector3(0.16f, 1.42f, 0.1f), new Vector3(-0.15f, 1.45f, -0.08f) };
            float[] heights = { 0.7f, 0.45f, 0.5f };
            for (int i = 0; i < flames.Length; i++)
            {
                GameObject flame = Proto.Cube(t, at + flames[i], new Vector3(0.24f, heights[i], 0.24f), Fire, "Flamme");
                flame.GetComponent<Renderer>().sharedMaterial = i == 0 ? coreMat : flameMat;
                flame.transform.localRotation = Quaternion.Euler(0f, i * 30f, 0f);
                flame.AddComponent<Flame>();
            }
            Proto.EndVisualOnly();
            Proto.Blocker(t, at + new Vector3(0f, 0.65f, 0f), new Vector3(1.0f, 1.3f, 1.0f), "Brasero");

            GameObject lightGo = new GameObject("Lueur du brasero");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = at + new Vector3(0f, 1.9f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.32f);
            light.intensity = 1.8f;
            light.range = 12f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();

            Ambiance.Embers(t, at + new Vector3(0f, 1.4f, 0f));
        }

        static float R(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
