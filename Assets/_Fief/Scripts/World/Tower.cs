using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA TOUR DE LA COURONNE (27/09 ; refaite le 29/09 -- Martin : "il faut que ce soit
    /// plus fair pour tout le monde, que tout le monde soit sur un meme pied d'egalite").
    ///
    ///   - un fut de pierre de 26 m de large et 100 m de haut ;
    ///   - QUATRE RAMPES en spirale a l'exterieur, entrelacees : chacune part du pied de
    ///     la tour EN FACE D'UNE PORTE, fait deux tours et arrive au sommet. Qu'on entre
    ///     par n'importe quelle porte, le chemin est exactement le meme. Entre deux
    ///     rampes, 12,5 m de haut : tomber d'une rampe, c'est atterrir sur celle du
    ///     dessous (ou dans le vide) ;
    ///   - SANS PARAPET ; chaque hauteur a sa COULEUR (six bandes, du bleu au violet) ;
    ///   - les OBSTACLES, tires au hasard a chaque manche, et PLUS NOMBREUX a chaque
    ///     manche (voir Randomize et Hardness) : des PENDULES, des BELIERS, des
    ///     BALAYEURS, des HERSES, des BOULETS (une volee sur les quatre rampes a la
    ///     fois) -- et les gargouilles. (Plus de trous ni de courants sur la rampe.)
    ///   - au SOMMET : la Couronne sur son socle ; on en repart en planant.
    /// </summary>
    public static class Tower
    {
        public const float Radius = 13f;
        public const float OuterRadius = 19.5f;
        public const float Height = 100f;
        /// <summary>Les bandes de couleur (hauteur), du pied au sommet.</summary>
        public const int Turns = 6;
        /// <summary>Le nombre de rampes (une par porte) et leurs tours chacune.</summary>
        public const int Ramps = 4;
        public const int RampTurns = 2;
        const int SegmentsPerTurn = 56;
        const float StartAngle = -Mathf.PI * 0.5f;        // la rampe 0 part au sud (porte sud)
        public static float Centre { get { return (Radius + OuterRadius) * 0.5f; } }
        public static float RampWidth { get { return OuterRadius - Radius; } }
        static int SegmentsPerRamp { get { return SegmentsPerTurn * RampTurns; } }

        /// <summary>
        /// LA DIFFICULTE (29/09 -- "plus on avance dans les manches, plus ca devient
        /// complique") : 1 a la premiere manche, puis plus a chaque manche. Les obstacles
        /// sont plus nombreux et plus rapides, les gargouilles tirent plus vite.
        /// </summary>
        public static float Hardness { get; private set; }

        // Les obstacles de chaque rampe (0-1 le long de la rampe), tires au hasard.
        static readonly List<float>[] Pendulums = NewLists();
        static readonly List<float>[] Rams = NewLists();
        static readonly List<float>[] Sweepers = NewLists();
        static readonly List<float>[] Spikes = NewLists();

        static List<float>[] NewLists()
        {
            List<float>[] l = new List<float>[Ramps];
            for (int i = 0; i < Ramps; i++) l[i] = new List<float>();
            return l;
        }

        /// <summary>La couleur de chaque bande de hauteur, du pied au sommet.</summary>
        public static readonly Color[] TurnColour =
        {
            new Color(0.35f, 0.62f, 1f),     // bleu
            new Color(0.40f, 0.85f, 0.55f),  // vert
            new Color(1f, 0.78f, 0.30f),     // or
            new Color(1f, 0.50f, 0.25f),     // orange
            new Color(0.95f, 0.30f, 0.35f),  // rouge
            new Color(0.78f, 0.50f, 1f)      // violet
        };

        /// <summary>La couleur de la hauteur "y".</summary>
        public static Color ColourAt(float y) { return TurnColour[Mathf.Clamp(Mathf.FloorToInt(y / (Height / Turns)), 0, Turns - 1)]; }

        /// <summary>
        /// TIRER LES OBSTACLES DE LA MANCHE, rampe par rampe (les quatre rampes ont le
        /// MEME NOMBRE de chaque obstacle : personne n'a la rampe facile). "level" : le
        /// nombre de manches deja jouees -- plus il monte, plus il y en a.
        /// </summary>
        public static void Randomize(int seed, int level)
        {
            Hardness = 1f + 0.14f * Mathf.Clamp(level, 0, 8);
            System.Random rng = new System.Random(seed * 7919 + 11);
            // (30/09 -- "les trous etaient trop compliques") : plus de trous dans la rampe.
            // (02/10, gamer chiant n° 61) La PREMIERE manche est plus douce : cinq obstacles par
            // rampe au lieu de sept (un pendule, un belier) -- le frere de Martin n'arrivait pas
            // en haut. Des la troisieme, on retrouve les memes nombres qu'avant.
            int pendulums = Mathf.Min(1 + (level + 1) / 2, 4);
            int rams = Mathf.Min(1 + (level + 1) / 3, 3);
            int sweepers = Mathf.Min(2 + level / 2, 4);
            int spikes = Mathf.Min(1 + level / 2, 3);
            for (int r = 0; r < Ramps; r++)
            {
                List<float> taken = new List<float>();
                Fill(rng, Pendulums[r], taken, pendulums, 0.045f);
                Fill(rng, Rams[r], taken, rams, 0.04f);
                Fill(rng, Sweepers[r], taken, sweepers, 0.045f);
                Fill(rng, Spikes[r], taken, spikes, 0.04f);
            }
        }

        static void Fill(System.Random rng, List<float> into, List<float> taken, int count, float gap)
        {
            into.Clear();
            for (int k = 0; k < count; k++)
            {
                for (int tries = 0; tries < 60; tries++)
                {
                    float f = 0.05f + (float)rng.NextDouble() * 0.9f;
                    bool ok = true;
                    for (int i = 0; i < taken.Count; i++) if (Mathf.Abs(taken[i] - f) < gap) { ok = false; break; }
                    if (!ok) continue;
                    taken.Add(f);
                    into.Add(f);
                    break;
                }
            }
        }

        static readonly Color Stone = new Color(0.80f, 0.73f, 0.62f);
        static readonly Color StoneLight = new Color(0.86f, 0.80f, 0.69f);
        static readonly Color StoneDark = new Color(0.55f, 0.47f, 0.40f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.4f);

        /// <summary>Le socle de la Couronne : au centre du sommet.</summary>
        public static Vector3 CrownSpot { get { return new Vector3(0f, Height, 0f); } }

        /// <summary>Vrai si ce point est sur la tour (ses rampes ou son sommet).</summary>
        public static bool On(Vector3 p)
        {
            float r = new Vector2(p.x, p.z).magnitude;
            return r < OuterRadius + 0.6f && p.y > 1.2f && p.y < Height + 6f;
        }

        /// <summary>Vrai si ce point est sur le SOMMET (la plate-forme de la Couronne).</summary>
        public static bool Summit(Vector3 p)
        {
            return p.y > Height - 1.5f && p.y < Height + 8f && new Vector2(p.x, p.z).magnitude < Radius + 0.8f;
        }

        /// <summary>L'angle (radians) d'un point de la rampe "ramp" a "u" (0 : le pied, 1 : le sommet).</summary>
        static float AngleOf(int ramp, float u) { return StartAngle + ramp * Mathf.PI * 0.5f + Mathf.Clamp01(u) * RampTurns * Mathf.PI * 2f; }

        /// <summary>Un point de la ligne milieu de la rampe "ramp".</summary>
        public static Vector3 RampPoint(int ramp, float u) { return RampPoint(ramp, u, 0f); }

        /// <summary>Un point de la rampe "ramp", decale de "lane" metres (+ vers le vide, - vers le mur).</summary>
        public static Vector3 RampPoint(int ramp, float u, float lane)
        {
            float a = AngleOf(ramp, u);
            float r = Centre + lane;
            return new Vector3(Mathf.Cos(a) * r, Mathf.Clamp01(u) * Height, Mathf.Sin(a) * r);
        }

        /// <summary>La hauteur de ce point sur la tour (0 au pied, 1 au sommet) : les quatre rampes montent pareil.</summary>
        public static float Progress(Vector3 p)
        {
            if (Summit(p)) return 1f;
            return Mathf.Clamp01(p.y / Height);
        }

        /// <summary>La rampe (0 a 3) sur laquelle se trouve ce point.</summary>
        public static int RampOf(Vector3 p)
        {
            float u = Mathf.Clamp01(p.y / Height);
            float diff = Mathf.Atan2(p.z, p.x) - (StartAngle + u * RampTurns * Mathf.PI * 2f);
            int k = Mathf.RoundToInt(diff / (Mathf.PI * 0.5f));
            return ((k % Ramps) + Ramps) % Ramps;
        }

        /// <summary>La rampe dont le pied fait face a la direction de "p" (depuis le centre) : celle de sa porte.</summary>
        public static int RampFacing(Vector3 p)
        {
            float diff = Mathf.Atan2(p.z, p.x) - StartAngle;
            int k = Mathf.RoundToInt(diff / (Mathf.PI * 0.5f));
            return ((k % Ramps) + Ramps) % Ramps;
        }

        /// <summary>La bande de hauteur (0 a 5) ou se trouve ce point.</summary>
        public static int TurnOf(Vector3 p) { return Mathf.Clamp(Mathf.FloorToInt(Progress(p) * Turns), 0, Turns - 1); }

        /// <summary>Le chemin de la rampe "ramp", de "from" a "to" (0-1), un point tous les dix degres.</summary>
        public static List<Vector3> Path(int ramp, float from, float to)
        {
            List<Vector3> p = new List<Vector3>();
            int steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(to - from) * RampTurns * 36f));
            for (int i = 1; i <= steps; i++) p.Add(RampPoint(ramp, Mathf.Lerp(from, to, i / (float)steps)) + Vector3.up * 0.1f);
            return p;
        }

        /// <summary>Le point au sol, devant le pied de la rampe "ramp".</summary>
        public static Vector3 FootOf(int ramp)
        {
            Vector3 start = RampPoint(ramp, 0f);
            Vector3 outward = new Vector3(start.x, 0f, start.z).normalized;
            return start + outward * 6f;
        }

        /// <summary>Le pied de la rampe la plus proche de "p" (celle qui fait face a sa porte).</summary>
        public static Vector3 FootNear(Vector3 p) { return FootOf(RampFacing(p)); }

        /// <summary>(Compatibilite) le pied de la rampe 0.</summary>
        public static Vector3 Foot { get { return FootOf(0); } }

        /// <summary>La longueur d'une rampe, en metres (pour les boulets).</summary>
        public static float RampLength { get { return RampTurns * Mathf.Sqrt(Mathf.Pow(2f * Mathf.PI * Centre, 2f) + Mathf.Pow(Height / RampTurns, 2f)); } }

        /// <summary>La direction de la rampe EN PENTE (qui monte) : pour poser a plat dessus ce qui doit l'etre.</summary>
        public static Vector3 Slope(int ramp, float u)
        {
            Vector3 d = RampPoint(ramp, u + 0.002f) - RampPoint(ramp, u - 0.002f);
            return d.normalized;
        }

        public static Vector3 Tangent(int ramp, float u)
        {
            Vector3 a = RampPoint(ramp, u - 0.002f), b = RampPoint(ramp, u + 0.002f);
            Vector3 d = b - a;
            d.y = 0f;
            return d.normalized;
        }

        /// <summary>
        /// LA ZONE DE DANGER (29/09) : des bandes ambre et noires peintes en travers de la
        /// rampe, la ou un obstacle frappe. On lit le danger avant d'y marcher.
        /// </summary>
        public static void DangerStripes(Transform parent, int ramp, float u)
        {
            // (03/10) De vraies bandes de chantier : ambre qui luit et ardoise, jointives, avec un
            // liseré d'or de chaque cote -- la meme famille que les obstacles (ObstacleKit).
            float du = 0.5f / RampLength;
            for (int k = -3; k <= 3; k++)
            {
                float at = u + k * du;
                Vector3 slope = RampPoint(ramp, at + du * 0.5f) - RampPoint(ramp, at - du * 0.5f);
                ObstacleKit.Slab(parent, RampPoint(ramp, at) + Vector3.up * 0.06f, Quaternion.LookRotation(slope.normalized, Vector3.up),
                    new Vector3(RampWidth * 0.8f, 0.04f, 0.5f), k % 2 == 0 ? ObstacleKit.Amber : ObstacleKit.SlateDark, "Zone de danger");
            }
            for (int e = -1; e <= 1; e += 2)
            {
                float at = u + e * du * 3.7f;
                Vector3 slope = RampPoint(ramp, at + du * 0.5f) - RampPoint(ramp, at - du * 0.5f);
                ObstacleKit.Slab(parent, RampPoint(ramp, at) + Vector3.up * 0.065f, Quaternion.LookRotation(slope.normalized, Vector3.up),
                    new Vector3(RampWidth * 0.8f, 0.05f, 0.12f), ObstacleKit.Gold, "Liseré d'or");
            }
        }

        // ================================================================== construction

        public static void Build(Transform parent)
        {
            if (Hardness <= 0f) Randomize(1, 0);
            GameObject root = new GameObject("TOUR DE LA COURONNE");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;

            // --- le fut (collider exact : MeshCollider sur le cylindre)
            Proto.BeginVisualOnly();
            GameObject core = Proto.Cylinder(t, new Vector3(0f, Height * 0.5f, 0f), new Vector3(Radius * 2f, Height * 0.5f, Radius * 2f), Stone, "Fût");
            Proto.EndVisualOnly();
            MeshCollider mc = core.AddComponent<MeshCollider>();
            mc.sharedMesh = Proto.SharedMesh(PrimitiveType.Cylinder);

            Proto.BeginVisualOnly();
            // Un soubassement plus large, un bandeau d'or entre deux bandes de couleur.
            // PAS au sommet (k < Turns) : le dernier bandeau tombait pile au niveau du sol
            // du sommet, deux disques au meme endroit, d'ou les traits blancs qui clignotent.
            Proto.Cylinder(t, new Vector3(0f, 2f, 0f), new Vector3(Radius * 2f + 1.2f, 2f, Radius * 2f + 1.2f), StoneDark, "Soubassement");
            for (int k = 1; k < Turns; k++)
            {
                Proto.Cylinder(t, new Vector3(0f, k * Height / Turns - 0.4f, 0f), new Vector3(Radius * 2f + 0.5f, 0.3f, Radius * 2f + 0.5f), StoneDark, "Bandeau");
                GameObject gilt = Proto.Cylinder(t, new Vector3(0f, k * Height / Turns - 0.05f, 0f), new Vector3(Radius * 2f + 0.55f, 0.05f, Radius * 2f + 0.55f), Color.white, "Filet d'or");
                gilt.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(Gold, 0.8f, 1f);
            }
            // Des fenetres hautes ; un tiers luisent.
            for (int k = 0; k < 26; k++)
            {
                float a = k * 2.399f;
                float y = 8f + (k * 3.7f) % (Height - 14f);
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                GameObject win = Proto.Cube(t, outward * (Radius + 0.02f) + Vector3.up * y, new Vector3(1f, 2.8f, 0.1f), new Color(0.06f, 0.06f, 0.07f), "Fenêtre");
                win.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                if (k % 3 == 0) win.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.72f, 0.4f), 0.55f);
            }
            Proto.EndVisualOnly();

            for (int r = 0; r < Ramps; r++)
            {
                BuildRamp(t, r);
                BuildBanners(t, r);
                // Au pied de chaque rampe, une arche d'or : "c'est ici qu'on monte".
                BuildArch(t, r);
            }
            BuildTop(t);

            // --- les obstacles, rampe par rampe
            float h = Hardness;
            for (int r = 0; r < Ramps; r++)
            {
                for (int i = 0; i < Pendulums[r].Count; i++)
                {
                    float u = Pendulums[r][i];
                    Pendulum.Build(t, RampPoint(r, u), Tangent(r, u), r * 1.7f + i * 2.3f);
                    DangerStripes(t, r, u);
                }
                for (int i = 0; i < Rams[r].Count; i++) Ram.Build(t, r, Rams[r][i], r * 0.9f + i * 0.37f);
                for (int i = 0; i < Sweepers[r].Count; i++)
                {
                    // Le balayeur : son pied contre le fut, sa barre balaie la rampe vers le vide.
                    float u = Sweepers[r][i];
                    Vector3 foot = RampPoint(r, u, -(RampWidth * 0.5f - 0.5f));
                    Vector3 outward = new Vector3(foot.x, 0f, foot.z).normalized;
                    Sweeper.Build(t, foot, outward, RampWidth - 0.6f, (0.8f + u * 0.6f) * h, r * 1.3f + i * 0.8f, true);
                }
                for (int i = 0; i < Spikes[r].Count; i++)
                {
                    float u = Spikes[r][i];
                    SpikeTrap.Build(t, RampPoint(r, u), Slope(r, u), RampWidth - 0.4f, 2.6f, 3.2f / h, r * 0.7f + i);
                }
            }
            BoulderChute.Build(t);
        }

        /// <summary>La rampe "ramp" : une dalle par pas, inclinee dans la pente, bordee de la couleur de sa hauteur.</summary>
        static void BuildRamp(Transform t, int ramp)
        {
            int total = SegmentsPerRamp;
            float width = RampWidth;
            for (int i = 0; i < total; i++)
            {
                float ua = i / (float)total, ub = (i + 1) / (float)total;
                Vector3 a = RampPoint(ramp, ua), b = RampPoint(ramp, ub);
                Vector3 run = b - a;
                Color band = ColourAt(a.y);
                // Les dalles se chevauchent un peu : une sur deux 3 cm plus haut, sinon leurs
                // dessus, au meme niveau, clignotent l'un a travers l'autre (z-fighting).
                GameObject slab = Proto.Cube(t, (a + b) * 0.5f - Vector3.up * (i % 2 == 0 ? 0.3f : 0.27f), new Vector3(width, 0.6f, run.magnitude * 1.14f), i % 2 == 0 ? Stone : StoneLight, "Rampe");
                slab.transform.localRotation = Quaternion.LookRotation(run.normalized, Vector3.up);

                Proto.BeginVisualOnly();
                Vector3 m = (a + b) * 0.5f;
                Vector3 outward = new Vector3(m.x, 0f, m.z).normalized;
                // Le liseré du bord, a la couleur de la hauteur : on voit ou finit la rampe.
                if (i % 2 == 0)
                {
                    GameObject edge = Proto.Cube(t, m + outward * (width * 0.5f - 0.12f) + Vector3.up * 0.03f, new Vector3(0.22f, 0.08f, run.magnitude * 2.1f), Color.white, "Liseré");
                    edge.transform.localRotation = slab.transform.localRotation;
                    edge.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(band, 0.8f);
                }
                // Une console sous la dalle, tous les quatre pas : la rampe est portee.
                if (i % 4 == 0)
                {
                    GameObject corbel = Proto.Cube(t, m - Vector3.up * 1.3f, new Vector3(width * 0.7f, 1.6f, 0.8f), StoneDark, "Console");
                    corbel.transform.localRotation = slab.transform.localRotation;
                }
                Proto.EndVisualOnly();
                // Une lanterne contre le fut, tous les quatorze pas (02/10 : sans vraie lumiere -- invisible en plein jour).
                if (i % 14 == 7)
                {
                    Proto.BeginVisualOnly();
                    // Une lanterne ronde sur une console de fer (plus de cube qui luit).
                    Proto.Cylinder(t, m - outward * (width * 0.5f - 0.1f) + Vector3.up * 2.05f, new Vector3(0.22f, 0.06f, 0.22f), new Color(0.16f, 0.15f, 0.17f), "Console");
                    GameObject lamp = Proto.Sphere(t, m - outward * (width * 0.5f - 0.1f) + Vector3.up * 2.35f, new Vector3(0.36f, 0.44f, 0.36f), Color.white, "Lanterne");
                    lamp.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.78f, 0.45f), 1.4f);
                    Proto.Cone(t, m - outward * (width * 0.5f - 0.1f) + Vector3.up * 2.55f, 0.24f, 0.3f, new Color(0.16f, 0.15f, 0.17f), "Chapeau");
                    Proto.EndVisualOnly();
                }
            }
        }

        /// <summary>Des bannieres le long de la rampe, a la couleur de la hauteur, pendues au fut.</summary>
        static void BuildBanners(Transform t, int ramp)
        {
            Proto.BeginVisualOnly();
            for (int k = 0; k < 6; k++)
            {
                float u = (k + 0.5f) / 6f;
                Vector3 p = RampPoint(ramp, u);
                Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
                Vector3 on = outward * (Radius + 0.08f) + Vector3.up * (p.y + 4.2f);
                Color c = ColourAt(p.y);
                GameObject rod = Proto.Cube(t, on + Vector3.up * 1.7f, new Vector3(0.1f, 0.1f, 1.8f), new Color(0.3f, 0.24f, 0.16f), "Tringle");
                rod.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(outward, Vector3.up), Vector3.up);
                GameObject cloth = Proto.Cube(t, on + outward * 0.05f, new Vector3(1.6f, 3.3f, 0.06f), c, "Bannière");
                cloth.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                cloth.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.25f, 0f);
                GameObject stripe = Proto.Cube(t, on + outward * 0.09f + Vector3.down * 0.4f, new Vector3(0.3f, 2.2f, 0.02f), Color.white, "Blason");
                stripe.transform.localRotation = cloth.transform.localRotation;
                stripe.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(Gold, 0.8f, 1f);
            }
            Proto.EndVisualOnly();
        }

        /// <summary>
        /// LE PIED D'UNE RAMPE (02/10 -- Martin : "revois le debut de l'escalier") : un seuil
        /// rond cercle a la couleur de la porte d'en face, deux colonnes tournees coiffees
        /// d'un petit toit en cloche, un arc de pierre et sa banniere. Avant : deux piliers
        /// carres, un linteau-cube et un filet d'or qui brillait -- et la dalle de la rampe
        /// qui sortait du sol en biseau.
        /// </summary>
        static void BuildArch(Transform t, int ramp)
        {
            Vector3 foot = RampPoint(ramp, 0f);
            Vector3 along = Tangent(ramp, 0.004f);
            Vector3 outward = new Vector3(foot.x, 0f, foot.z).normalized;
            Color gate = Castle.GateColourToward(outward);
            Material stone = MaterialFactory.Get(Stone);
            Proto.BeginVisualOnly();
            // Le seuil : un anneau a la couleur de la porte, une dalle ronde dedans (il cache
            // le bout de la premiere dalle de la rampe).
            Vector3 sill = foot - along * 1.2f + Vector3.up * 0.04f;
            Proto.Lathe(t, sill, new[] { new Vector2(2.9f, 0f), new Vector2(2.9f, 0.07f), new Vector2(2.9f, 0.07f), new Vector2(0f, 0.07f) }, 32, gate, "Anneau du seuil")
                .GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(gate, 0.3f, 0f);
            Proto.Lathe(t, sill, new[] { new Vector2(2.6f, 0f), new Vector2(2.6f, 0.1f), new Vector2(2.45f, 0.13f), new Vector2(2.45f, 0.13f), new Vector2(0f, 0.13f) }, 32, StoneLight, "Seuil");
            // Les deux colonnes et leur toit en cloche : l'une adossee au soubassement de la
            // tour (le bord interieur de la rampe longe le fut), l'autre dehors, au bord du vide.
            Vector3 ground = new Vector3(0f, 0.04f, 0f);
            Vector3 inner = outward * (Radius + 0.35f) + ground;
            Vector3 outer = outward * (OuterRadius + 0.75f) + ground;
            Vector3[] columns = { inner, outer };
            for (int k = 0; k < columns.Length; k++)
            {
                GameObject col = Castle.Column(t, columns[k], 0.5f, 5.4f, Stone, "Colonne");
                col.GetComponent<Renderer>().sharedMaterial = stone;
                Castle.BellRoof(t, columns[k] + Vector3.up * 5.4f, 0.85f, 1.9f);
                CapsuleCollider body = col.AddComponent<CapsuleCollider>();
                body.radius = 0.5f;
                body.height = 5.4f;
                body.center = new Vector3(0f, 2.7f, 0f);
            }
            // L'arc : une anse de panier de pierre, d'une colonne a l'autre, sa clef d'or mat,
            // et la banniere de la porte qui pend dessous.
            Vector3 mid = (inner + outer) * 0.5f;
            float half = (outer - inner).magnitude * 0.5f;
            Vector3 prev = Vector3.zero;
            for (int k = 0; k <= 8; k++)
            {
                float th = k / 8f * Mathf.PI;
                Vector3 p = mid + outward * (-Mathf.Cos(th) * half) + Vector3.up * (4.6f + Mathf.Sin(th) * 2.1f);
                if (k > 0) Beam(t, prev, p, 0.62f, stone);
                prev = p;
            }
            Vector3 apex = mid + Vector3.up * 6.7f;
            Proto.Sphere(t, apex - along * 0.1f, new Vector3(0.7f, 0.8f, 0.75f), Color.white, "Clef").GetComponent<Renderer>().sharedMaterial = Castle.MatteGold;
            GameObject cloth = Proto.Cube(t, apex + Vector3.down * 1.55f, new Vector3(1.5f, 2.2f, 0.06f), gate, "Bannière de la rampe");
            cloth.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
            cloth.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(gate, 0.3f, 0f);
            GameObject point = Proto.Cone(t, apex + Vector3.down * 2.65f, 0.75f, 0.45f, gate, "Pointe de la bannière", 4);
            point.transform.rotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(180f, 0f, 45f);
            point.transform.localScale = new Vector3(0.75f, 0.45f, 0.03f);
            point.GetComponent<Renderer>().sharedMaterial = cloth.GetComponent<Renderer>().sharedMaterial;
            Proto.EndVisualOnly();
        }

        /// <summary>Une poutre ronde (une gelule) de "a" a "b".</summary>
        static void Beam(Transform t, Vector3 a, Vector3 b, float thick, Material m)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.001f) return;
            GameObject g = Proto.Capsule(t, (a + b) * 0.5f, new Vector3(thick, (len + thick) * 0.5f, thick), Color.white, "Voussoir");
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / len);
            g.GetComponent<Renderer>().sharedMaterial = m;
        }

        /// <summary>
        /// Le sommet : des creneaux (sauf aux quatre arrivees), un cercle de runes,
        /// et quatre braseros. (Plus de planeurs sur chevalets : les ailes s'ouvrent seules.)
        /// </summary>
        static void BuildTop(Transform t)
        {
            Proto.BeginVisualOnly();
            for (int k = 0; k < 44; k++)
            {
                float a = k / 44f * Mathf.PI * 2f;
                if (k % 2 == 1) continue;
                bool arrival = false;
                for (int r = 0; r < Ramps; r++)
                {
                    Vector3 arr = RampPoint(r, 1f);
                    if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, Mathf.Atan2(arr.z, arr.x) * Mathf.Rad2Deg)) < 22f) arrival = true;
                }
                if (arrival) continue;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Radius - 0.4f) + Vector3.up * (Height + 0.6f);
                // Des merlons de pierre claire coiffes d'un chaperon sombre, comme ceux du
                // chateau (avant : de simples paves sombres, un autre style que la muraille).
                Quaternion face = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), Vector3.up);
                GameObject m = Proto.Cube(t, p, new Vector3(0.8f, 1.2f, 1.6f), Stone, "Merlon");
                m.transform.localRotation = face;
                GameObject cap = Proto.Cube(t, p + Vector3.up * 0.67f, new Vector3(0.96f, 0.14f, 1.76f), StoneDark, "Chaperon");
                cap.transform.localRotation = face;
            }
            // LES ARRIVEES (02/10 -- "revois la fin de l'escalier") : de chaque cote de la
            // breche dans les creneaux, une borne tournee a boule d'or mat, baguee a la couleur
            // de la porte de sa rampe -- on sait par ou l'on est monte, et ou redescendre.
            for (int r = 0; r < Ramps; r++)
            {
                Vector3 arr = RampPoint(r, 1f);
                float aa = Mathf.Atan2(arr.z, arr.x);
                Color gate = Castle.GateColourToward(new Vector3(RampPoint(r, 0f).x, 0f, RampPoint(r, 0f).z));
                for (int side = -1; side <= 1; side += 2)
                {
                    float a = aa + side * 21f * Mathf.Deg2Rad;
                    Vector3 at = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Radius - 0.5f) + Vector3.up * Height;
                    Castle.Column(t, at, 0.34f, 1.7f, StoneLight, "Borne d'arrivée");
                    Proto.Cylinder(t, at + Vector3.up * 1.05f, new Vector3(0.66f, 0.1f, 0.66f), Color.white, "Bague").GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(gate, 0.3f, 0f);
                    Proto.Sphere(t, at + Vector3.up * 1.95f, Vector3.one * 0.5f, Color.white, "Boule d'or").GetComponent<Renderer>().sharedMaterial = Castle.MatteGold;
                }
            }
            // Deux disques bien separes en hauteur (3 cm), jamais au meme niveau : sinon
            // la carte graphique ne sait pas lequel dessiner devant (z-fighting).
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, Height, 0f), new Vector3(9f, 0.015f, 9f), Color.white, "Cercle");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.95f, 0.72f, 0.3f), 0.55f);
            Proto.Cylinder(t, new Vector3(0f, Height + 0.02f, 0f), new Vector3(8.2f, 0.04f, 8.2f), StoneDark, "Dalle");
            Proto.EndVisualOnly();
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                Castle.Torch(t, new Vector3(Mathf.Cos(a) * 10.2f, Height, Mathf.Sin(a) * 10.2f), 2f);
            }
            GameObject glowGo = new GameObject("Lueur du sommet");
            glowGo.transform.SetParent(t, false);
            glowGo.transform.localPosition = new Vector3(0f, Height + 7f, 0f);
            Light glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.45f);
            glow.intensity = 0.8f;
            glow.range = 22f;
            glow.shadows = LightShadows.None;
            glow.renderMode = LightRenderMode.ForceVertex;   // (03/10) fluide : jamais une passe de plus pour elle
        }
    }

    // ====================================================================== les obstacles

    /// <summary>
    /// UN PENDULE de la tour (un butoir rouge au bout d'un bras rigide, 02/10), qui balaie la
    /// rampe du bord interieur vers le vide et retour. Qui est dans sa course est
    /// projete -- et lache la Couronne.
    /// </summary>
    public class Pendulum : MonoBehaviour, IHazard
    {
        Transform arm;
        float phase;
        bool lastLow;
        Vector3 lastHead;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();

        const float Length = 6f;
        // Du bord interieur (18 degres : au-dela, le butoir entrerait dans le mur -- 24 avant
        // le butoir du 03/10, plus large) jusqu'au-dessus du vide (68).
        const float SwingIn = 18f;
        const float SwingOut = 68f;
        const float Speed = 1.6f;

        public static Pendulum Build(Transform parent, Vector3 rampCentre, Vector3 tangent, float phase)
        {
            Vector3 pivot = rampCentre + Vector3.up * 7.4f;
            GameObject go = new GameObject("PENDULE");
            go.transform.SetParent(parent, false);
            go.transform.position = pivot;
            go.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            Pendulum p = go.AddComponent<Pendulum>();
            p.phase = phase;

            // (02/10) Un BUTOIR de jeu televise au bout d'un bras RIGIDE, facon Fall Guys.
            // (03/10 -- "rends-les magnifiques") Refait avec la boite a obstacles : une
            // POTENCE d'ardoise qui sort d'une platine ronde cerclee d'or, tenue par une
            // jambe de force ; un MOYEU d'or ; un bras d'acier bague d'or ; et le butoir, un
            // gros palet ROUGE aux bords ronds, ceinture d'or, une face de frappe CREME et sa
            // cible rouge de chaque cote -- la ou il frappe.
            Vector3 inward = -new Vector3(pivot.x, 0f, pivot.z).normalized;
            float toWall = new Vector2(pivot.x, pivot.z).magnitude - Tower.Radius;
            Vector3 wall = pivot + inward * toWall;
            Vector3 along = go.transform.forward;
            // La potence : immobile, sous la tour (elle est figee avec le decor).
            ObstacleKit.Drum(parent, wall, inward, 1.0f, 0.5f, 0.1f, ObstacleKit.Slate, "Platine");
            ObstacleKit.Ring(parent, wall - inward * 0.2f, -inward, 0.94f, 1.1f, 0.14f, ObstacleKit.Gold, "Cercle de la platine");
            float beamLength = toWall + 0.45f;
            ObstacleKit.Drum(parent, wall - inward * (beamLength * 0.5f), inward, 0.3f, beamLength, 0.12f, ObstacleKit.Slate, "Potence");
            ObstacleKit.Drum(parent, wall - inward * 0.5f, inward, 0.36f, 0.18f, 0.05f, ObstacleKit.Gold, "Bague de la potence");
            Vector3 braceFoot = wall - inward * 0.3f + Vector3.down * 2.4f;
            Vector3 braceTop = wall - inward * (toWall * 0.62f);
            ObstacleKit.Drum(parent, (braceFoot + braceTop) * 0.5f, braceTop - braceFoot, 0.14f, (braceTop - braceFoot).magnitude, 0.07f, ObstacleKit.Steel, "Jambe de force");
            for (int k = 0; k < 4; k++)
            {
                Vector3 round = Quaternion.AngleAxis(45f + k * 90f, inward) * Vector3.up;
                ObstacleKit.Ball(parent, wall - inward * 0.27f + round * 0.72f, 0.1f, ObstacleKit.Gold, "Rivet");
            }
            ObstacleKit.Drum(parent, pivot, along, 0.44f, 0.84f, 0.14f, ObstacleKit.Gold, "Moyeu");
            ObstacleKit.Drum(parent, pivot, along, 0.2f, 0.96f, 0.06f, ObstacleKit.Red, "Axe");

            GameObject armGo = new GameObject("Bras");
            armGo.transform.SetParent(go.transform, false);
            p.arm = armGo.transform;
            // Le bras (il tourne autour du z local : la boule va du mur vers le vide, en x).
            ObstacleKit.Drum(p.arm, new Vector3(0f, -(Length - 1.1f) * 0.5f - 0.35f, 0f), Vector3.up, 0.15f, Length - 1.4f, 0.06f, ObstacleKit.Steel, "Bras");
            ObstacleKit.Drum(p.arm, new Vector3(0f, -0.62f, 0f), Vector3.up, 0.25f, 0.24f, 0.08f, ObstacleKit.Gold, "Bague");
            ObstacleKit.Drum(p.arm, new Vector3(0f, -Length + 1.2f, 0f), Vector3.up, 0.3f, 0.34f, 0.1f, ObstacleKit.Gold, "Bague");
            Vector3 head = new Vector3(0f, -Length, 0f);
            ObstacleKit.Drum(p.arm, head, Vector3.right, 1.0f, 1.7f, 0.42f, ObstacleKit.Red, "Butoir");
            ObstacleKit.Drum(p.arm, head, Vector3.right, 1.03f, 0.18f, 0.06f, ObstacleKit.Gold, "Ceinture");
            for (int side = -1; side <= 1; side += 2)
            {
                ObstacleKit.Drum(p.arm, head + Vector3.right * side * 0.86f, Vector3.right, 0.56f, 0.09f, 0.03f, ObstacleKit.Cream, "Face de frappe");
                ObstacleKit.Drum(p.arm, head + Vector3.right * side * 0.915f, Vector3.right, 0.24f, 0.05f, 0.015f, ObstacleKit.Red, "Cible");
            }
            // Une trainee de braise suit la boule : on voit sa course.
            Fx.KeepTrail(p.arm, new Vector3(0f, -Length, 0f), new Color(1f, 0.55f, 0.5f), 1.2f, 0.35f);
            p.lastHead = p.Head;
            Proto.Weld(p.arm, "Butoir soudé", null);
            return p;
        }

        Vector3 Head { get { return arm.TransformPoint(new Vector3(0f, -Length, 0f)); } }

        /// <summary>L'angle du bras a l'instant "time" (un sinus : on peut le lire en avance).</summary>
        float AngleAt(float time)
        {
            float wave = Mathf.Sin(time * Speed * Tower.Hardness + phase);
            float mid = (SwingOut - SwingIn) * 0.5f, half = (SwingOut + SwingIn) * 0.5f;
            return mid + half * wave;
        }

        void OnEnable() { Hazards.Add(this); }
        void OnDisable() { Hazards.Remove(this); }

        /// <summary>Pour les bots : la boule passera-t-elle sur "feet" d'ici "within" secondes ?</summary>
        public bool Danger(Vector3 feet, float within)
        {
            if ((feet - transform.position).sqrMagnitude > 14f * 14f) return false;
            float now = Time.time;
            return Hazards.Sweeps(feet, within, t => transform.TransformPoint(Quaternion.Euler(0f, 0f, AngleAt(now + t)) * new Vector3(0f, -Length, 0f)), 2.4f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float wave = Mathf.Sin(Time.time * Speed * Tower.Hardness + phase);
            arm.localRotation = Quaternion.Euler(0f, 0f, AngleAt(Time.time));
            // Il siffle quand il passe a pleine vitesse, si tu es tout pres.
            bool low = wave > 0f;
            if (low != lastLow)
            {
                lastLow = low;
                Transform pl = Game.PlayerTransform;
                if (pl != null && (pl.position - transform.position).magnitude < 40f) Sfx.WhooshAt(Head);
            }
            Vector3 head = Head;
            Vector3 velocity = (head - lastHead) / dt;
            lastHead = head;
            if (Game.Season == null || !Game.Season.Running) return;

            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                if ((s.Body.position + Vector3.up * 1f - head).magnitude > 1.9f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 1f) continue;
                lastHit[s] = Time.time;
                Vector3 push = velocity.sqrMagnitude > 1f ? new Vector3(velocity.x, 0f, velocity.z).normalized : transform.right;
                Combat.Hit(s, push * 30f + Vector3.up * 9f, 0.35f, true, null);
                Fx.ObstacleHit(s, new Color(1f, 0.55f, 0.3f));
                Sfx.ClangAt(Head);
            }
        }
    }

    /// <summary>
    /// UN BELIER : un bloc de pierre cerclé de fer, loge dans le fut, qui JAILLIT en
    /// travers de la rampe toutes les quatre secondes et repousse vers le vide ce qui
    /// se trouve devant lui. Sa rune passe du bleu au ROUGE une demi-seconde avant.
    /// </summary>
    public class Ram : MonoBehaviour, IHazard
    {
        Transform block;
        Renderer rune;
        Vector3 inward, outward;
        float phase;
        int shown = -1;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();

        const float Period = 4f;
        const float Reach = 5.2f;                  // jusqu'ou il sort (la rampe fait 6,5 m)
        // (x : vers le vide.) Aussi long que sa course : rentre, il est tout entier dans le
        // fut ; sorti, il barre la rampe depuis le mur.
        static readonly Vector3 Size = new Vector3(5.6f, 2.2f, 1.8f);

        public static Ram Build(Transform parent, int ramp, float u, float phase)
        {
            Vector3 centre = Tower.RampPoint(ramp, u);
            Vector3 outDir = new Vector3(centre.x, 0f, centre.z).normalized;
            GameObject go = new GameObject("BÉLIER");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(outDir.x * Tower.Radius, centre.y, outDir.z * Tower.Radius);
            go.transform.rotation = Quaternion.LookRotation(Tower.Tangent(ramp, u), Vector3.up);
            Ram r = go.AddComponent<Ram>();
            r.phase = phase;
            r.outward = outDir;
            Tower.DangerStripes(parent, ramp, u);

            // (03/10 -- "rends-les magnifiques") LE LOGEMENT : un portail rond d'ardoise dans
            // le fut, cercle d'or, quatre rivets, un trou noir d'ou le belier jaillit.
            float cy = Size.y * 0.5f + 0.1f;
            ObstacleKit.Ring(go.transform, new Vector3(0f, cy, 0f), Vector3.right, 1.12f, 1.45f, 0.5f, ObstacleKit.Slate, "Portail");
            ObstacleKit.Ring(go.transform, new Vector3(0.06f, cy, 0f), Vector3.right, 1.43f, 1.57f, 0.44f, ObstacleKit.Gold, "Cercle");
            ObstacleKit.Drum(go.transform, new Vector3(-0.35f, cy, 0f), Vector3.right, 1.16f, 0.2f, 0f, ObstacleKit.Hole, "Trou");
            for (int k = 0; k < 4; k++)
            {
                float a = (45f + k * 90f) * Mathf.Deg2Rad;
                ObstacleKit.Ball(go.transform, new Vector3(0.27f, cy + Mathf.Sin(a) * 1.29f, Mathf.Cos(a) * 1.29f), 0.1f, ObstacleKit.Gold, "Rivet");
            }
            Proto.Weld(go.transform, "Portail soudé", null);

            GameObject b = new GameObject("Bloc");
            b.transform.SetParent(go.transform, false);
            r.block = b.transform;
            BoxCollider box = b.AddComponent<BoxCollider>();
            box.size = new Vector3(Size.x, Size.y, Size.z);
            box.center = new Vector3(-Size.x * 0.5f, Size.y * 0.5f + 0.1f, 0f);
            // LE BELIER : un fut d'ardoise bague d'or, et au bout un gros POING rouge aux bords
            // ronds, sa face creme, et au centre la rune (bleue au repos, ROUGE avant de frapper).
            ObstacleKit.Drum(r.block, new Vector3(-Size.x * 0.5f + 0.1f, cy, 0f), Vector3.right, 0.82f, Size.x - 0.4f, 0.1f, ObstacleKit.Slate, "Fût");
            ObstacleKit.Drum(r.block, new Vector3(-1.5f, cy, 0f), Vector3.right, 0.88f, 0.2f, 0.06f, ObstacleKit.Gold, "Bague");
            ObstacleKit.Drum(r.block, new Vector3(-3.3f, cy, 0f), Vector3.right, 0.88f, 0.2f, 0.06f, ObstacleKit.Gold, "Bague");
            ObstacleKit.Drum(r.block, new Vector3(-0.2f, cy, 0f), Vector3.right, 1.04f, 0.76f, 0.3f, ObstacleKit.Red, "Poing");
            ObstacleKit.Drum(r.block, new Vector3(0.2f, cy, 0f), Vector3.right, 0.7f, 0.08f, 0.03f, ObstacleKit.Cream, "Face de frappe");
            GameObject runeGo = ObstacleKit.Drum(r.block, new Vector3(0.25f, cy, 0f), Vector3.right, 0.32f, 0.06f, 0.02f, ObstacleKit.Amber, "Rune");
            r.rune = runeGo.GetComponent<Renderer>();
            Proto.Weld(r.block, "Bélier soudé", new List<Renderer> { r.rune });
            r.Place(0f);
            return r;
        }

        void OnEnable() { Hazards.Add(this); }
        void OnDisable() { Hazards.Remove(this); }

        /// <summary>Pour les bots : "feet" est-il devant le belier, et sort-il (ou va-t-il sortir) d'ici "within" secondes ?</summary>
        public bool Danger(Vector3 feet, float within)
        {
            Vector3 d = feet - transform.position;
            if (d.sqrMagnitude > 12f * 12f) return false;
            float along = Vector3.Dot(d, outward);
            float side = Vector3.Dot(d, transform.forward);
            if (Mathf.Abs(side) > Size.z * 0.5f + 0.9f || d.y < -1f || d.y > Size.y + 0.6f || along < -0.5f || along > Reach + 1f) return false;
            // Danger : le bloc est encore sorti (il barre la rampe), ou il va JAILLIR d'ici
            // "within" secondes (c'est en sortant qu'il frappe). Entre les deux, on passe.
            float period = Period / Mathf.Clamp(Tower.Hardness, 1f, 1.6f);
            float t = Mathf.Repeat(Time.time + phase * period, period);
            float warn;
            return StrokeAt(Time.time, out warn) > 0.15f || t > period - within - 0.1f || t < 0.3f;
        }

        /// <summary>Ou en est le bloc : 0 rentre, 1 sorti. Et 0-1 : l'alerte avant la frappe.</summary>
        float Stroke(out float warn) { return StrokeAt(Time.time, out warn); }

        float StrokeAt(float time, out float warn)
        {
            float period = Period / Mathf.Clamp(Tower.Hardness, 1f, 1.6f);
            float t = Mathf.Repeat(time + phase * period, period);
            warn = t > period - 0.6f ? 1f : 0f;
            if (t < 0.3f) return Mathf.SmoothStep(0f, 1f, t / 0.3f);          // il jaillit
            if (t < 0.9f) return 1f;                                          // il reste
            if (t < 2.4f) return 1f - Mathf.SmoothStep(0f, 1f, (t - 0.9f) / 1.5f);   // il rentre
            return 0f;
        }

        /// <summary>Le bloc glisse vers le vide (le "x" local du belier regarde vers le vide ; sa face est en x = 0).</summary>
        void Place(float k)
        {
            block.position = transform.position + outward * (Reach * k);
        }

        void Update()
        {
            float warn;
            float k = Stroke(out warn);
            float before = (block.position - transform.position).magnitude;
            Place(k);
            int mood = warn > 0f ? 1 : 0;
            if (mood != shown)
            {
                shown = mood;
                rune.sharedMaterial = MaterialFactory.GetGlow(mood == 1 ? new Color(1f, 0.2f, 0.12f) : new Color(0.4f, 0.65f, 1f), mood == 1 ? 3.5f : 1.6f);
            }
            float after = (block.position - transform.position).magnitude;
            if (Game.Season == null || !Game.Season.Running || after <= before + 0.001f) return;

            // Il frappe en sortant : tout ce qui est devant sa face part vers le vide.
            Vector3 face = block.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                float along = Vector3.Dot(d, outward);
                float side = Vector3.Dot(d, transform.forward);
                if (Mathf.Abs(side) > Size.z * 0.5f + 0.5f || d.y < -0.5f || d.y > Size.y + 0.4f) continue;
                if (along < 0f || along > (face - transform.position).magnitude + 0.6f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 1f) continue;
                lastHit[s] = Time.time;
                Combat.Hit(s, outward * 32f + Vector3.up * 8f, 0.4f, true, null);
                Fx.ObstacleHit(s, new Color(1f, 0.4f, 0.25f));
                Sfx.CrashAt(transform.position);
            }
        }
    }

    /// <summary>
    /// LES BOULETS : toutes les trente-quatre secondes (moins aux manches suivantes),
    /// une VOLEE -- une grosse boule de pierre par rampe, du meme cote pour toutes --
    /// part du sommet et DEVALE, pres du mur ou pres du vide, en alternance. Elle
    /// gronde, ses runes luisent : on la voit venir, on change de cote.
    /// </summary>
    public class BoulderChute : MonoBehaviour
    {
        public static readonly List<Boulder> Rolling = new List<Boulder>();
        float timer = 6f;
        int count;
        Transform parent;

        const float Every = 34f;

        public static void Build(Transform parent)
        {
            GameObject go = new GameObject("BOULETS");
            go.transform.SetParent(parent, false);
            BoulderChute c = go.AddComponent<BoulderChute>();
            c.parent = go.transform;
            Rolling.Clear();
        }

        void Update()
        {
            if (Game.Season == null || !Game.Season.Running) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Every / Tower.Hardness;
            count++;
            // UNE VOLEE : un boulet sur CHAQUE rampe, du meme cote, au meme instant -- la
            // meme course pour tout le monde (avant, les rampes 1 et 3 avaient toujours
            // leur boulet a l'exterieur, les rampes 0 et 2 toujours a l'interieur).
            float lane = count % 2 == 0 ? -1.9f : 1.9f;
            for (int r = 0; r < Tower.Ramps; r++) Boulder.Launch(parent, r, lane);
        }

        /// <summary>Un boulet arrive sur "p" (a moins de "metres", sur son cote) : de quel cote s'ecarter.</summary>
        public static bool Threat(Vector3 p, float metres, out float laneToAvoid)
        {
            laneToAvoid = 0f;
            for (int i = 0; i < Rolling.Count; i++)
            {
                Boulder b = Rolling[i];
                if (b == null) continue;
                Vector3 d = b.transform.position - p;
                if (Mathf.Abs(d.y) > 3f || new Vector2(d.x, d.z).magnitude > metres) continue;
                laneToAvoid = b.Lane;
                return true;
            }
            return false;
        }
    }

    public class Boulder : MonoBehaviour
    {
        public float Lane { get; private set; }
        int ramp;
        float u = 1f;
        Transform ball;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();

        const float Speed = 9f;
        const float BallRadius = 1.15f;

        public static void Launch(Transform parent, int ramp, float lane)
        {
            GameObject go = new GameObject("BOULET");
            go.transform.SetParent(parent, false);
            Boulder b = go.AddComponent<Boulder>();
            b.Lane = lane;
            b.ramp = ramp;
            GameObject ballGo = new GameObject("Boule");
            ballGo.transform.SetParent(go.transform, false);
            b.ball = ballGo.transform;
            // (03/10) Une grosse boule ROUGE a deux bandes CREME croisees et une bande de braise :
            // de la famille des butoirs (le rouge frappe), et on la voit ROULER (les bandes
            // tournent). Soudee : un seul dessin.
            ObstacleKit.Ball(b.ball, Vector3.zero, BallRadius, ObstacleKit.Red, "Boule");
            ObstacleKit.Drum(b.ball, Vector3.zero, Vector3.right, BallRadius + 0.02f, 0.3f, 0.08f, ObstacleKit.Cream, "Bande");
            ObstacleKit.Drum(b.ball, Vector3.zero, Vector3.forward, BallRadius + 0.02f, 0.3f, 0.08f, ObstacleKit.Cream, "Bande");
            ObstacleKit.Drum(b.ball, Vector3.zero, Vector3.up, BallRadius + 0.015f, 0.12f, 0.03f, ObstacleKit.Amber, "Braise");
            Proto.Weld(b.ball, "Boulet soudé", null);
            b.Place();
            BoulderChute.Rolling.Add(b);
            Fx.Sparks(go.transform.position, new Color(1f, 0.55f, 0.25f), 30, 6f);
        }

        void OnDestroy() { BoulderChute.Rolling.Remove(this); }

        void Place()
        {
            transform.position = Tower.RampPoint(ramp, u, Lane) + Vector3.up * BallRadius;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Game.Season == null || !Game.Season.Running) return;
            Vector3 before = transform.position;
            u -= Speed * Mathf.Sqrt(Tower.Hardness) / Tower.RampLength * dt;
            if (u <= 0f) { Fx.Sparks(transform.position, new Color(1f, 0.55f, 0.25f), 40, 7f); Destroy(gameObject); return; }
            Place();
            Vector3 moved = transform.position - before;
            // Elle roule : elle tourne sur elle-meme dans le sens de la marche.
            if (moved.sqrMagnitude > 0.0001f)
            {
                Vector3 axis = Vector3.Cross(Vector3.up, moved.normalized);
                ball.Rotate(axis, moved.magnitude / BallRadius * Mathf.Rad2Deg, Space.World);
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 d = s.Body.position + Vector3.up - transform.position;
                if (Mathf.Abs(d.y) > 2f || new Vector2(d.x, d.z).magnitude > BallRadius + 0.5f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 1.2f) continue;
                lastHit[s] = Time.time;
                Vector3 away = new Vector3(d.x, 0f, d.z);
                if (away.sqrMagnitude < 0.01f) away = moved;
                Combat.Hit(s, (away.normalized + moved.normalized).normalized * 26f + Vector3.up * 10f, 0.45f, true, null);
                Fx.ObstacleHit(s, new Color(1f, 0.55f, 0.25f));
                Sfx.CrashAt(transform.position);
            }
        }
    }
}
