using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA TOUR DE LA COURONNE (27/09 -- Martin : "une tour enorme, avec des obstacles
    /// bien faits"). Au milieu de la citadelle, sur l'ile flottante.
    ///
    ///   - un fut de pierre de 26 m de large et 100 m de haut ;
    ///   - une RAMPE EN SPIRALE a l'exterieur, six metres et demi de large, SIX tours
    ///     du pied au sommet, SANS PARAPET : on tombe, ou on y est pousse ;
    ///   - chaque tour a sa COULEUR (ses bannieres) : on lit sa hauteur d'un regard ;
    ///   - les OBSTACLES, tous annonces avant de frapper :
    ///       CINQ TROUS a sauter en courant -- et sous chacun, un tour plus bas, un
    ///       COURANT qui renvoie d'un tour vers le haut, a travers le trou ;
    ///       CINQ PENDULES qui balaient la rampe du mur vers le vide ;
    ///       SIX BELIERS qui jaillissent du mur (leur rune rougit une demi-seconde avant) ;
    ///       des BOULETS qui devalent la rampe depuis le sommet, sur un cote ou l'autre
    ///       (on change de cote, ou on saute) ;
    ///   - au SOMMET : la Couronne sur son socle, et tout autour, les PLANEURS -- qui
    ///     monte la-haut prend des ailes (voir Wings) pour redescendre en planant.
    /// </summary>
    public static class Tower
    {
        public const float Radius = 13f;
        public const float OuterRadius = 19.5f;
        public const float Height = 100f;
        public const int Turns = 6;
        const int SegmentsPerTurn = 56;
        const float StartAngle = -Mathf.PI * 0.5f;        // le pied de la rampe : au sud
        public static float Centre { get { return (Radius + OuterRadius) * 0.5f; } }
        public static float RampWidth { get { return OuterRadius - Radius; } }
        static int TotalSegments { get { return SegmentsPerTurn * Turns; } }

        // Ou, dans chaque tour de rampe (0 = le pied du tour, 1 = un tour plus loin),
        // se trouvent les obstacles. Les trous sont a ~0,58 tour les uns des autres : on
        // marche un peu entre deux courants.
        static readonly float[] GapAt = { -1f, 0.36f, 0.95f, 0.52f, 0.10f, 0.68f };        // (tour 0 : pas de trou)
        static readonly float[] PendulumAt = { -1f, 0.72f, 0.30f, 0.84f, 0.46f, 0.22f };
        static readonly float[] RamAt = { 0.55f, 0.12f, 0.64f, 0.18f, 0.80f, 0.40f };

        /// <summary>La couleur de chaque tour de rampe, du pied au sommet.</summary>
        public static readonly Color[] TurnColour =
        {
            new Color(0.35f, 0.62f, 1f),     // bleu
            new Color(0.40f, 0.85f, 0.55f),  // vert
            new Color(1f, 0.78f, 0.30f),     // or
            new Color(1f, 0.50f, 0.25f),     // orange
            new Color(0.95f, 0.30f, 0.35f),  // rouge
            new Color(0.78f, 0.50f, 1f)      // violet
        };
        public static readonly string[] TurnName = { "bleu", "vert", "or", "orange", "rouge", "violet" };

        static readonly Color Stone = new Color(0.46f, 0.43f, 0.40f);
        static readonly Color StoneLight = new Color(0.52f, 0.49f, 0.45f);
        static readonly Color StoneDark = new Color(0.30f, 0.28f, 0.27f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.4f);

        /// <summary>Le socle de la Couronne : au centre du sommet.</summary>
        public static Vector3 CrownSpot { get { return new Vector3(0f, Height, 0f); } }

        /// <summary>Vrai si ce point est sur la tour (sa rampe ou son sommet).</summary>
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

        /// <summary>Un point de la ligne milieu de la rampe ; u va de 0 (le pied) a 1 (le sommet).</summary>
        public static Vector3 RampPoint(float u) { return RampPoint(u, 0f); }

        /// <summary>Un point de la rampe, decale de "lane" metres (+ vers le vide, - vers le mur).</summary>
        public static Vector3 RampPoint(float u, float lane)
        {
            float phi = Mathf.Clamp01(u) * Turns * Mathf.PI * 2f;
            float a = StartAngle + phi;
            float r = Centre + lane;
            return new Vector3(Mathf.Cos(a) * r, Mathf.Clamp01(u) * Height, Mathf.Sin(a) * r);
        }

        /// <summary>Ou en est ce point sur la rampe (0 au pied, 1 au sommet).</summary>
        public static float Progress(Vector3 p)
        {
            if (Summit(p)) return 1f;
            float a = Mathf.Atan2(p.z, p.x) - StartAngle;
            a = Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 2f);          // 0..1 dans le tour
            float turn = Mathf.Round(p.y / (Height / Turns) - a);           // le tour dont la hauteur colle le mieux
            return Mathf.Clamp01((Mathf.Clamp(turn, 0f, Turns - 1) + a) / Turns);
        }

        /// <summary>Le tour de rampe (0 a 5) ou se trouve ce point.</summary>
        public static int TurnOf(Vector3 p) { return Mathf.Clamp(Mathf.FloorToInt(Progress(p) * Turns), 0, Turns - 1); }

        /// <summary>
        /// Le chemin de la rampe, de "from" (0-1) a "to" (0-1), un point tous les dix
        /// degres. Pour monter : from &lt; to ; pour descendre, l'inverse.
        /// </summary>
        public static List<Vector3> Path(float from, float to)
        {
            List<Vector3> p = new List<Vector3>();
            int steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(to - from) * Turns * 36f));
            for (int i = 1; i <= steps; i++) p.Add(RampPoint(Mathf.Lerp(from, to, i / (float)steps)) + Vector3.up * 0.1f);
            return p;
        }

        /// <summary>Le point au sol, devant le pied de la rampe.</summary>
        public static Vector3 Foot
        {
            get
            {
                Vector3 start = RampPoint(0f);
                Vector3 outward = new Vector3(start.x, 0f, start.z).normalized;
                return start + outward * 6f;
            }
        }

        /// <summary>La longueur de la rampe, en metres (pour les boulets).</summary>
        public static float RampLength { get { return Turns * Mathf.Sqrt(Mathf.Pow(2f * Mathf.PI * Centre, 2f) + Mathf.Pow(Height / Turns, 2f)); } }

        static bool IsGap(int segment)
        {
            for (int t = 1; t < Turns; t++)
            {
                if (GapAt[t] < 0f) continue;
                int g = Mathf.RoundToInt((t + GapAt[t]) * SegmentsPerTurn);
                if (segment == g || segment == g + 1) return true;
            }
            return false;
        }

        static float U(int turn, float frac) { return (turn + frac) / Turns; }

        // ================================================================== construction

        public static void Build(Transform parent)
        {
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
            // Un soubassement plus large, et un bandeau a chaque tour.
            Proto.Cylinder(t, new Vector3(0f, 2f, 0f), new Vector3(Radius * 2f + 1.2f, 2f, Radius * 2f + 1.2f), StoneDark, "Soubassement");
            for (int k = 1; k <= Turns; k++)
                Proto.Cylinder(t, new Vector3(0f, k * Height / Turns - 0.4f, 0f), new Vector3(Radius * 2f + 0.5f, 0.3f, Radius * 2f + 0.5f), StoneDark, "Bandeau");
            // Des fenetres hautes ; un tiers luisent.
            for (int k = 0; k < 26; k++)
            {
                float a = k * 2.399f;
                float y = 8f + (k * 3.7f) % (Height - 14f);
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                GameObject win = Proto.Cube(t, outward * (Radius + 0.02f) + Vector3.up * y, new Vector3(1f, 2.8f, 0.1f), new Color(0.06f, 0.06f, 0.07f), "Fenêtre");
                win.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                if (k % 3 == 0) win.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.72f, 0.4f), 1.3f);
            }
            Proto.EndVisualOnly();

            BuildRamp(t);
            BuildBanners(t);
            BuildTop(t);

            // --- les obstacles
            for (int turn = 0; turn < Turns; turn++)
            {
                if (PendulumAt[turn] >= 0f)
                {
                    float u = U(turn, PendulumAt[turn]);
                    Pendulum.Build(t, RampPoint(u), Tangent(u), turn * 1.7f);
                }
                Ram.Build(t, U(turn, RamAt[turn]), turn * 0.9f);
            }
            BoulderChute.Build(t);

            // Sous chaque trou, un tour plus bas : un COURANT (au bord interieur).
            Updraft.All.Clear();
            for (int turn = 1; turn < Turns; turn++)
            {
                float u = (Mathf.RoundToInt((turn + GapAt[turn]) * SegmentsPerTurn) + 1f) / TotalSegments - 1f / Turns;
                Vector3 below = RampPoint(u, -(RampWidth * 0.5f - 1.3f));
                Updraft.Build(t, below, TurnColour[turn]);
            }
        }

        public static Vector3 Tangent(float u)
        {
            Vector3 a = RampPoint(u - 0.002f), b = RampPoint(u + 0.002f);
            Vector3 d = b - a;
            d.y = 0f;
            return d.normalized;
        }

        /// <summary>La rampe : une dalle par pas, inclinee dans la pente, bordee de sa couleur.</summary>
        static void BuildRamp(Transform t)
        {
            int total = TotalSegments;
            float width = RampWidth;
            for (int i = 0; i < total; i++)
            {
                if (IsGap(i)) continue;
                float ua = i / (float)total, ub = (i + 1) / (float)total;
                Vector3 a = RampPoint(ua), b = RampPoint(ub);
                Vector3 run = b - a;
                int turn = Mathf.Clamp(i / SegmentsPerTurn, 0, Turns - 1);
                GameObject slab = Proto.Cube(t, (a + b) * 0.5f - Vector3.up * 0.3f, new Vector3(width, 0.6f, run.magnitude * 1.14f), i % 2 == 0 ? Stone : StoneLight, "Rampe");
                slab.transform.localRotation = Quaternion.LookRotation(run.normalized, Vector3.up);

                Proto.BeginVisualOnly();
                Vector3 m = (a + b) * 0.5f;
                Vector3 outward = new Vector3(m.x, 0f, m.z).normalized;
                // Le liseré du bord, a la couleur du tour : on voit ou finit la rampe.
                if (i % 2 == 0)
                {
                    GameObject edge = Proto.Cube(t, m + outward * (width * 0.5f - 0.12f) + Vector3.up * 0.03f, new Vector3(0.22f, 0.08f, run.magnitude * 2.1f), Color.white, "Liseré");
                    edge.transform.localRotation = slab.transform.localRotation;
                    edge.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(TurnColour[turn], 1.2f);
                }
                // Une console sous la dalle, tous les quatre pas : la rampe est portee.
                if (i % 4 == 0)
                {
                    GameObject corbel = Proto.Cube(t, m - Vector3.up * 1.3f, new Vector3(width * 0.7f, 1.6f, 0.8f), StoneDark, "Console");
                    corbel.transform.localRotation = slab.transform.localRotation;
                }
                Proto.EndVisualOnly();
                // Une lanterne contre le fut, tous les quatorze pas, et une vraie lumiere une fois sur deux.
                if (i % 14 == 7)
                {
                    Proto.BeginVisualOnly();
                    GameObject lamp = Proto.Cube(t, m - outward * (width * 0.5f - 0.1f) + Vector3.up * 2.4f, new Vector3(0.3f, 0.45f, 0.3f), Color.white, "Lanterne");
                    lamp.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.78f, 0.45f), 2.2f);
                    Proto.EndVisualOnly();
                    if (i % 28 == 7)
                    {
                        GameObject lg = new GameObject("Lueur");
                        lg.transform.SetParent(t, false);
                        lg.transform.localPosition = m - outward * (width * 0.5f - 0.8f) + Vector3.up * 2.6f;
                        Light l = lg.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1f, 0.74f, 0.45f);
                        l.intensity = 1.2f;
                        l.range = 12f;
                        l.shadows = LightShadows.None;
                    }
                }
            }
            // Les trous : une barre rouge luisante au bord, pour qu'on les voie venir.
            Proto.BeginVisualOnly();
            for (int turn = 1; turn < Turns; turn++)
            {
                int g = Mathf.RoundToInt((turn + GapAt[turn]) * SegmentsPerTurn);
                for (int side = 0; side < 2; side++)
                {
                    float u = (g + side * 2) / (float)total;
                    GameObject warn = Proto.Cube(t, RampPoint(u) + Vector3.up * 0.05f, new Vector3(width * 0.9f, 0.08f, 0.3f), Color.white, "Bord du trou");
                    warn.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.3f, 0.2f), 2.5f);
                    warn.transform.localRotation = Quaternion.LookRotation(Tangent(u), Vector3.up);
                }
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Deux bannieres par tour, a la couleur du tour, pendues au fut.</summary>
        static void BuildBanners(Transform t)
        {
            Proto.BeginVisualOnly();
            for (int turn = 0; turn < Turns; turn++)
            {
                for (int k = 0; k < 3; k++)
                {
                    float u = U(turn, 0.18f + k * 0.33f);
                    Vector3 p = RampPoint(u);
                    Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
                    Vector3 on = outward * (Radius + 0.08f) + Vector3.up * (p.y + 4.2f);
                    Color c = TurnColour[turn];
                    GameObject rod = Proto.Cube(t, on + Vector3.up * 1.7f, new Vector3(0.1f, 0.1f, 1.8f), new Color(0.3f, 0.24f, 0.16f), "Tringle");
                    rod.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(outward, Vector3.up), Vector3.up);
                    GameObject cloth = Proto.Cube(t, on + outward * 0.05f, new Vector3(1.6f, 3.3f, 0.06f), c, "Bannière");
                    cloth.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                    cloth.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 0.55f);
                    GameObject stripe = Proto.Cube(t, on + outward * 0.09f + Vector3.down * 0.4f, new Vector3(0.3f, 2.2f, 0.02f), Color.white, "Blason");
                    stripe.transform.localRotation = cloth.transform.localRotation;
                    stripe.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Gold, 1.2f);
                }
            }
            Proto.EndVisualOnly();
        }

        /// <summary>
        /// Le sommet : des creneaux (sauf a l'arrivee de la rampe), un cercle de runes,
        /// quatre braseros, et HUIT PLANEURS sur leurs chevalets, tout autour.
        /// </summary>
        static void BuildTop(Transform t)
        {
            Proto.BeginVisualOnly();
            Vector3 arrival = RampPoint(1f);
            float arrivalAngle = Mathf.Atan2(arrival.z, arrival.x);
            for (int k = 0; k < 44; k++)
            {
                float a = k / 44f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, arrivalAngle * Mathf.Rad2Deg)) < 22f || k % 2 == 1) continue;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Radius - 0.4f) + Vector3.up * (Height + 0.6f);
                GameObject m = Proto.Cube(t, p, new Vector3(0.8f, 1.2f, 1.6f), StoneDark, "Merlon");
                m.transform.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), Vector3.up);
            }
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, Height + 0.02f, 0f), new Vector3(9f, 0.02f, 9f), Color.white, "Cercle");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.95f, 0.72f, 0.3f), 0.9f);
            Proto.Cylinder(t, new Vector3(0f, Height + 0.03f, 0f), new Vector3(8.2f, 0.02f, 8.2f), StoneDark, "Dalle");
            Proto.EndVisualOnly();
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                Castle.Torch(t, new Vector3(Mathf.Cos(a) * 10.2f, Height, Mathf.Sin(a) * 10.2f), 2f);
            }
            for (int k = 0; k < 8; k++)
            {
                float a = (k * 45f + 22.5f) * Mathf.Deg2Rad;
                Wings.BuildRack(t, new Vector3(Mathf.Cos(a) * 7.2f, Height, Mathf.Sin(a) * 7.2f), a);
            }
            GameObject glowGo = new GameObject("Lueur du sommet");
            glowGo.transform.SetParent(t, false);
            glowGo.transform.localPosition = new Vector3(0f, Height + 7f, 0f);
            Light glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.45f);
            glow.intensity = 2.4f;
            glow.range = 30f;
            glow.shadows = LightShadows.None;
        }
    }

    // ====================================================================== les obstacles

    /// <summary>
    /// UN COURANT DE LA TOUR : un disque pale, a la couleur du tour du dessus, au bord
    /// interieur de la rampe, et une colonne de lumiere qui monte a travers le trou.
    /// Qui marche dedans est projete un tour plus haut, par le trou.
    /// </summary>
    public class Updraft : MonoBehaviour
    {
        public static readonly List<Updraft> All = new List<Updraft>();
        readonly Dictionary<Seeker, float> lastLaunch = new Dictionary<Seeker, float>();
        Transform disc;
        Color tint;

        public const float Radius = 1.1f;
        const float Lift = 29f;         // assez pour monter de 17 m et depasser le rebord

        public static Updraft Build(Transform parent, Vector3 at, Color tint)
        {
            GameObject go = new GameObject("COURANT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Updraft u = go.AddComponent<Updraft>();
            u.tint = Color.Lerp(tint, Color.white, 0.4f);
            Proto.BeginVisualOnly();
            GameObject d = Proto.Cylinder(go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(Radius * 2f, 0.03f, Radius * 2f), Color.white, "Disque");
            d.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(u.tint, 2f);
            u.disc = d.transform;
            Proto.EndVisualOnly();
            LightBeam beam = LightBeam.Build(parent, at, u.tint, 1.4f, 18f);
            if (beam != null) beam.targetAlpha = 0.4f;
            All.Add(u);
            return u;
        }

        void OnDestroy() { All.Remove(this); }

        /// <summary>Le courant le plus proche de "p" a moins de "metres" (null sinon).</summary>
        public static Updraft Near(Vector3 p, float metres)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && (All[i].transform.position - p).magnitude < metres) return All[i];
            return null;
        }

        void Update()
        {
            if (disc != null) disc.localScale = new Vector3(Radius * 2f, 0.03f, Radius * 2f) * (1f + 0.08f * Mathf.Sin(Time.time * 5f));
            if (Game.Season == null || !Game.Season.Running) return;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                if (Mathf.Abs(d.y) > 1.2f || new Vector2(d.x, d.z).magnitude > Radius) continue;
                float last;
                if (lastLaunch.TryGetValue(s, out last) && Time.time - last < 1.5f) continue;
                IMover m = AbilityCaster.MoverOf(s);
                if (m == null) continue;
                lastLaunch[s] = Time.time;
                m.Push(Vector3.up * Lift);
                Fx.Column(transform.position, tint, 18f, 0.4f, 0.9f);
                Fx.GroundRing(transform.position, tint, 3f, 0.4f);
                if (s.IsPlayer || (Game.PlayerTransform != null && (Game.PlayerTransform.position - transform.position).magnitude < 30f)) Sfx.Whoosh();
            }
        }
    }

    /// <summary>
    /// UN PENDULE de la tour : une boule de pierre au bout d'une chaine, qui balaie la
    /// rampe du bord interieur vers le vide et retour. Qui est dans sa course est
    /// projete -- et lache la Couronne.
    /// </summary>
    public class Pendulum : MonoBehaviour
    {
        Transform arm;
        float phase;
        bool lastLow;
        Vector3 lastHead;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();

        const float Length = 6f;
        // Du bord interieur (24 degres : au-dela, la boule entrerait dans le mur) jusqu'au-dessus du vide (68).
        const float SwingIn = 24f;
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

            Color iron = new Color(0.14f, 0.14f, 0.15f);
            Proto.BeginVisualOnly();
            Vector3 inward = -new Vector3(pivot.x, 0f, pivot.z).normalized;
            GameObject beam = Proto.Cube(parent, pivot + inward * 1.7f + Vector3.up * 0.3f, new Vector3(0.7f, 0.7f, 3.8f), new Color(0.26f, 0.24f, 0.22f), "Potence");
            beam.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);
            GameObject armGo = new GameObject("Bras");
            armGo.transform.SetParent(go.transform, false);
            p.arm = armGo.transform;
            for (int k = 0; k < 8; k++)
            {
                GameObject link = Proto.Cube(p.arm, new Vector3(0f, -0.4f - k * 0.62f, 0f), new Vector3(0.14f, 0.55f, 0.14f), iron, "Maillon");
                link.transform.localRotation = Quaternion.Euler(0f, k % 2 == 0 ? 0f : 90f, 0f);
            }
            Proto.Sphere(p.arm, new Vector3(0f, -Length, 0f), new Vector3(1.8f, 1.8f, 1.8f), new Color(0.34f, 0.32f, 0.3f), "Boule");
            for (int k = 0; k < 6; k++)
            {
                GameObject spike = Proto.Cone(p.arm, new Vector3(0f, -Length, 0f), 0.22f, 0.7f, iron, "Pointe", 4);
                spike.transform.localRotation = Quaternion.Euler(k < 4 ? 90f : (k == 4 ? 0f : 180f), k * 90f, 0f);
                spike.transform.localPosition = new Vector3(0f, -Length, 0f) + spike.transform.localRotation * Vector3.up * 0.85f;
            }
            GameObject band = Proto.Cylinder(p.arm, new Vector3(0f, -Length, 0f), new Vector3(1.9f, 0.12f, 1.9f), Color.white, "Rune");
            band.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.45f, 0.25f), 2.4f);
            band.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Proto.EndVisualOnly();
            p.lastHead = p.Head;
            return p;
        }

        Vector3 Head { get { return arm.TransformPoint(new Vector3(0f, -Length, 0f)); } }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float wave = Mathf.Sin(Time.time * Speed + phase);
            float mid = (SwingOut - SwingIn) * 0.5f, half = (SwingOut + SwingIn) * 0.5f;
            arm.localRotation = Quaternion.Euler(0f, 0f, mid + half * wave);
            // Il siffle quand il passe a pleine vitesse, si tu es tout pres.
            bool low = wave > 0f;
            if (low != lastLow)
            {
                lastLow = low;
                Transform pl = Game.PlayerTransform;
                if (pl != null && (pl.position - transform.position).magnitude < 16f) Sfx.Whoosh();
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
                Combat.Hit(s, push * 18f + Vector3.up * 6f, 0.3f, true, null);
                Fx.Impact(s.Body.position + Vector3.up * 1.1f, new Color(1f, 0.55f, 0.3f), 1f);
                Sfx.Clang();
            }
        }
    }

    /// <summary>
    /// UN BELIER : un bloc de pierre cerclé de fer, loge dans le fut, qui JAILLIT en
    /// travers de la rampe toutes les quatre secondes et repousse vers le vide ce qui
    /// se trouve devant lui. Sa rune passe du bleu au ROUGE une demi-seconde avant.
    /// </summary>
    public class Ram : MonoBehaviour
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

        public static Ram Build(Transform parent, float u, float phase)
        {
            Vector3 centre = Tower.RampPoint(u);
            Vector3 outDir = new Vector3(centre.x, 0f, centre.z).normalized;
            GameObject go = new GameObject("BÉLIER");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(outDir.x * Tower.Radius, centre.y, outDir.z * Tower.Radius);
            go.transform.rotation = Quaternion.LookRotation(Tower.Tangent(u), Vector3.up);
            Ram r = go.AddComponent<Ram>();
            r.phase = phase;
            r.outward = outDir;

            // Le logement dans le mur : un cadre sombre.
            Proto.BeginVisualOnly();
            GameObject frame = Proto.Cube(go.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 2.8f, 2.4f), new Color(0.12f, 0.11f, 0.11f), "Logement");
            frame.transform.localPosition = new Vector3(0.1f, 1.3f, 0f);
            Proto.EndVisualOnly();

            GameObject b = new GameObject("Bloc");
            b.transform.SetParent(go.transform, false);
            r.block = b.transform;
            BoxCollider box = b.AddComponent<BoxCollider>();
            box.size = new Vector3(Size.x, Size.y, Size.z);
            box.center = new Vector3(-Size.x * 0.5f, Size.y * 0.5f + 0.1f, 0f);
            Proto.BeginVisualOnly();
            Proto.Cube(r.block, new Vector3(-Size.x * 0.5f, Size.y * 0.5f + 0.1f, 0f), Size, new Color(0.38f, 0.36f, 0.34f), "Pierre");
            Proto.Cube(r.block, new Vector3(0.05f, Size.y * 0.5f + 0.1f, 0f), new Vector3(0.2f, Size.y + 0.1f, Size.z + 0.1f), new Color(0.16f, 0.16f, 0.17f), "Face de fer");
            for (int k = -1; k <= 1; k += 2)
                Proto.Cube(r.block, new Vector3(-Size.x * 0.5f, Size.y * 0.5f + 0.1f + k * 0.8f, 0f), new Vector3(Size.x + 0.05f, 0.15f, Size.z + 0.05f), new Color(0.16f, 0.16f, 0.17f), "Cerclage");
            GameObject runeGo = Proto.Cube(r.block, new Vector3(0.17f, Size.y * 0.5f + 0.1f, 0f), new Vector3(0.05f, 0.7f, 0.7f), Color.white, "Rune");
            r.rune = runeGo.GetComponent<Renderer>();
            Proto.EndVisualOnly();
            r.Place(0f);
            return r;
        }

        /// <summary>Ou en est le bloc : 0 rentre, 1 sorti. Et 0-1 : l'alerte avant la frappe.</summary>
        float Stroke(out float warn)
        {
            float t = Mathf.Repeat(Time.time + phase * Period, Period);
            warn = t > Period - 0.6f ? 1f : 0f;
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
                Combat.Hit(s, outward * 20f + Vector3.up * 5f, 0.35f, true, null);
                Fx.Impact(s.Body.position + Vector3.up * 1.1f, new Color(1f, 0.4f, 0.25f), 1f);
                Sfx.Crash();
            }
        }
    }

    /// <summary>
    /// LES BOULETS : toutes les vingt secondes, une grosse boule de pierre part du
    /// sommet et DEVALE la rampe, d'un cote (pres du mur) ou de l'autre (pres du vide).
    /// Elle gronde, ses runes luisent : on la voit venir, on change de cote -- ou on
    /// saute par-dessus un trou au bon moment.
    /// </summary>
    public class BoulderChute : MonoBehaviour
    {
        public static readonly List<Boulder> Rolling = new List<Boulder>();
        float timer = 6f;
        int count;
        Transform parent;

        const float Every = 20f;

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
            timer = Every;
            count++;
            Boulder.Launch(parent, count % 2 == 0 ? -1.9f : 1.9f);
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
        float u = 1f;
        Transform ball;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();

        const float Speed = 9f;
        const float BallRadius = 1.15f;

        public static void Launch(Transform parent, float lane)
        {
            GameObject go = new GameObject("BOULET");
            go.transform.SetParent(parent, false);
            Boulder b = go.AddComponent<Boulder>();
            b.Lane = lane;
            GameObject ballGo = new GameObject("Boule");
            ballGo.transform.SetParent(go.transform, false);
            b.ball = ballGo.transform;
            Proto.BeginVisualOnly();
            Proto.Sphere(b.ball, Vector3.zero, Vector3.one * BallRadius * 2f, new Color(0.33f, 0.3f, 0.28f), "Pierre");
            for (int k = 0; k < 3; k++)
            {
                GameObject band = Proto.Cylinder(b.ball, Vector3.zero, new Vector3(BallRadius * 2.04f, 0.08f, BallRadius * 2.04f), Color.white, "Rune");
                band.transform.localRotation = Quaternion.Euler(k * 60f, 0f, 90f);
                band.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.5f, 0.2f), 2.2f);
            }
            Proto.EndVisualOnly();
            b.Place();
            BoulderChute.Rolling.Add(b);
            Fx.Sparks(go.transform.position, new Color(1f, 0.55f, 0.25f), 30, 6f);
        }

        void OnDestroy() { BoulderChute.Rolling.Remove(this); }

        void Place()
        {
            transform.position = Tower.RampPoint(u, Lane) + Vector3.up * BallRadius;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Game.Season == null || !Game.Season.Running) return;
            Vector3 before = transform.position;
            u -= Speed / Tower.RampLength * dt;
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
                Combat.Hit(s, (away.normalized + moved.normalized).normalized * 16f + Vector3.up * 7f, 0.4f, true, null);
                Fx.Impact(s.Body.position + Vector3.up, new Color(1f, 0.55f, 0.25f), 1.2f);
                Sfx.Crash();
            }
        }
    }
}
