using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA TOUR DE LA COURONNE (27/09 -- "un giga chateau"). Au milieu de la citadelle.
    ///
    ///   - un fut de pierre de 22 m de large et 64 m de haut : on le voit de partout,
    ///     il perce la brume ;
    ///   - une RAMPE EN SPIRALE a l'exterieur, cinq metres et demi de large, quatre
    ///     tours complets du pied au sommet. Pas de parapet : on peut tomber -- ou y
    ///     etre pousse. Tomber, c'est retomber un tour plus bas (ou dans la cour) ;
    ///   - trois TROUS dans la rampe, a sauter en courant ;
    ///   - quatre PENDULES, un par tour, qui balaient la rampe de part en part ;
    ///   - au SOMMET, la Couronne sur son socle, quatre braseros.
    ///
    /// C'est une course (Fall Guys, Mario 64) : quatre joueurs montent en meme temps,
    /// se poussent, se crochetent, se grappinent d'un tour a l'autre. Et redescendre
    /// avec la Couronne est une autre course : si le porteur saute, elle reste la ou
    /// il a quitte le sol (voir Crown.Slip) -- sauf avec le Planeur.
    /// </summary>
    public static class Tower
    {
        public const float Radius = 11f;
        public const float OuterRadius = 16.5f;
        public const float Height = 64f;
        public const int Turns = 4;
        const int SegmentsPerTurn = 48;
        const float StartAngle = -Mathf.PI * 0.5f;        // le pied de la rampe : au sud
        static float Centre { get { return (Radius + OuterRadius) * 0.5f; } }

        /// <summary>Les segments sautes : un trou par tour, a partir du deuxieme.</summary>
        static readonly int[] Gaps = { 62, 110, 158 };
        static readonly int[] Pendulums = { 34, 84, 130, 178 };

        static readonly Color Stone = new Color(0.32f, 0.32f, 0.31f);
        static readonly Color StoneDark = new Color(0.22f, 0.22f, 0.22f);
        static readonly Color Rune = new Color(1f, 0.78f, 0.4f);

        /// <summary>Le socle de la Couronne : au centre du sommet.</summary>
        public static Vector3 CrownSpot { get { return new Vector3(0f, Height, 0f); } }

        /// <summary>Vrai si ce point est sur la tour (sa rampe ou son sommet).</summary>
        public static bool On(Vector3 p)
        {
            float r = new Vector2(p.x, p.z).magnitude;
            return r < OuterRadius + 0.6f && p.y > 1.2f;
        }

        /// <summary>Un point de la ligne milieu de la rampe ; u va de 0 (le pied) a 1 (le sommet).</summary>
        public static Vector3 RampPoint(float u)
        {
            float phi = Mathf.Clamp01(u) * Turns * Mathf.PI * 2f;
            float a = StartAngle + phi;
            return new Vector3(Mathf.Cos(a) * Centre, u * Height, Mathf.Sin(a) * Centre);
        }

        /// <summary>Ou en est ce point sur la rampe (0 au pied, 1 au sommet).</summary>
        public static float Progress(Vector3 p)
        {
            if (p.y > Height - 1.5f && new Vector2(p.x, p.z).magnitude < Radius + 0.5f) return 1f;
            float a = Mathf.Atan2(p.z, p.x) - StartAngle;
            a = Mathf.Repeat(a, Mathf.PI * 2f) / (Mathf.PI * 2f);          // 0..1 dans le tour
            float turn = Mathf.Round(p.y / (Height / Turns) - a);           // le tour dont la hauteur colle le mieux
            return Mathf.Clamp01((Mathf.Clamp(turn, 0f, Turns - 1) + a) / Turns);
        }

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
        public static Vector3 Foot { get { return RampPoint(0f) + new Vector3(0f, 0f, -6f); } }

        // ================================================================== construction

        public static void Build(Transform parent)
        {
            GameObject root = new GameObject("TOUR DE LA COURONNE");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;

            // --- le fut : un cylindre (visuel) et son collider exact (MeshCollider :
            //     la gelule que Unity met d'office sur un cylindre arrondirait le sommet).
            Proto.BeginVisualOnly();
            GameObject core = Proto.Cylinder(t, new Vector3(0f, Height * 0.5f, 0f), new Vector3(Radius * 2f, Height * 0.5f, Radius * 2f), Stone, "Fût");
            Proto.EndVisualOnly();
            MeshCollider mc = core.AddComponent<MeshCollider>();
            mc.sharedMesh = Proto.SharedMesh(PrimitiveType.Cylinder);

            Proto.BeginVisualOnly();
            // Des bandeaux tous les seize metres, un peu en saillie : on lit les tours.
            for (int k = 0; k <= Turns; k++)
                Proto.Cylinder(t, new Vector3(0f, k * Height / Turns + 0.3f, 0f), new Vector3(Radius * 2f + 0.5f, 0.3f, Radius * 2f + 0.5f), StoneDark, "Bandeau");
            // Des fenetres hautes et noires ; certaines luisent.
            for (int k = 0; k < 14; k++)
            {
                float a = k * 2.399f;
                float y = 6f + (k * 4.3f) % (Height - 10f);
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                GameObject win = Proto.Cube(t, outward * (Radius + 0.02f) + Vector3.up * y, new Vector3(0.9f, 2.6f, 0.1f), new Color(0.05f, 0.05f, 0.06f), "Fenêtre");
                win.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
                if (k % 3 == 0) win.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.72f, 0.4f), 1.3f);
            }
            Proto.EndVisualOnly();

            BuildRamp(t);
            BuildTop(t);

            for (int i = 0; i < Pendulums.Length; i++)
            {
                float u = (Pendulums[i] + 0.5f) / (SegmentsPerTurn * Turns);
                Pendulum.Build(t, RampPoint(u), Tangent(u), i * 1.7f);
            }

            // Sous chaque trou, un tour plus bas : un COURANT qui renvoie d'un tour vers
            // le haut, a travers le trou. Au bord interieur de la rampe : on y entre
            // expres, on ne tombe pas dedans en courant.
            Updraft.All.Clear();
            for (int g = 0; g < Gaps.Length; g++)
            {
                float u = (Gaps[g] + 1f) / (SegmentsPerTurn * Turns) - 1f / Turns;
                Vector3 mid = RampPoint(u);
                Vector3 inward = -new Vector3(mid.x, 0f, mid.z).normalized;
                Updraft.Build(t, mid + inward * (Centre - Radius - 1.3f));
            }
        }

        static Vector3 Tangent(float u)
        {
            Vector3 a = RampPoint(u - 0.002f), b = RampPoint(u + 0.002f);
            Vector3 d = b - a;
            d.y = 0f;
            return d.normalized;
        }

        /// <summary>La rampe : une dalle par pas de 7,5 degres, inclinee dans la pente.</summary>
        static void BuildRamp(Transform t)
        {
            int total = SegmentsPerTurn * Turns;
            float width = OuterRadius - Radius;
            for (int i = 0; i < total; i++)
            {
                bool gap = false;
                for (int g = 0; g < Gaps.Length; g++) if (i == Gaps[g] || i == Gaps[g] + 1) gap = true;
                if (gap) continue;
                float ua = i / (float)total, ub = (i + 1) / (float)total;
                Vector3 a = RampPoint(ua), b = RampPoint(ub);
                Vector3 run = b - a;
                GameObject slab = Proto.Cube(t, (a + b) * 0.5f - Vector3.up * 0.25f, new Vector3(width, 0.5f, run.magnitude * 1.12f), i % 2 == 0 ? Stone : new Color(0.29f, 0.29f, 0.28f), "Rampe");
                slab.transform.localRotation = Quaternion.LookRotation(run.normalized, Vector3.up);

                Proto.BeginVisualOnly();
                // Une console sous la dalle, tous les quatre pas : la rampe est portee.
                if (i % 4 == 0)
                {
                    GameObject corbel = Proto.Cube(t, (a + b) * 0.5f - Vector3.up * 1.1f, new Vector3(width * 0.7f, 1.4f, 0.7f), StoneDark, "Console");
                    corbel.transform.localRotation = slab.transform.localRotation;
                }
                // Une pierre au bord exterieur, tous les trois pas : on voit le vide.
                if (i % 3 == 0)
                {
                    Vector3 edge = (a + b) * 0.5f;
                    Vector3 outward = new Vector3(edge.x, 0f, edge.z).normalized;
                    Proto.Cube(t, edge + outward * (width * 0.5f - 0.25f) + Vector3.up * 0.25f, new Vector3(0.4f, 0.5f, 0.4f), StoneDark, "Borne");
                }
                // Une lanterne contre le fut, tous les douze pas.
                if (i % 12 == 6)
                {
                    Vector3 m = (a + b) * 0.5f;
                    Vector3 inward = -new Vector3(m.x, 0f, m.z).normalized;
                    GameObject lamp = Proto.Cube(t, m + inward * (width * 0.5f - 0.1f) + Vector3.up * 2.4f, new Vector3(0.3f, 0.45f, 0.3f), Color.white, "Lanterne");
                    lamp.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2.2f);
                }
                Proto.EndVisualOnly();
                if (i % 24 == 6)
                {
                    Vector3 m = (a + b) * 0.5f;
                    Vector3 inward = -new Vector3(m.x, 0f, m.z).normalized;
                    GameObject lg = new GameObject("Lueur");
                    lg.transform.SetParent(t, false);
                    lg.transform.localPosition = m + inward * (width * 0.5f - 0.8f) + Vector3.up * 2.6f;
                    Light l = lg.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.72f, 0.42f);
                    l.intensity = 1.4f;
                    l.range = 12f;
                    l.shadows = LightShadows.None;
                    lg.AddComponent<LampFlicker>();
                }
            }
            // Les trous : une lueur rouge au bord, pour qu'on les voie venir.
            Proto.BeginVisualOnly();
            for (int g = 0; g < Gaps.Length; g++)
            {
                Vector3 before = RampPoint(Gaps[g] / (float)total);
                GameObject warn = Proto.Cube(t, before + Vector3.up * 0.05f, new Vector3(0.3f, 0.06f, 0.3f), Color.white, "Bord du trou");
                warn.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.3f, 0.2f), 2.5f);
                warn.transform.localScale = new Vector3(width * 0.9f, 0.06f, 0.25f);
                warn.transform.localRotation = Quaternion.LookRotation(Tangent(Gaps[g] / (float)total), Vector3.up);
            }
            Proto.EndVisualOnly();
        }

        /// <summary>Le sommet : des creneaux (sauf a l'arrivee de la rampe), quatre braseros, une lueur d'or.</summary>
        static void BuildTop(Transform t)
        {
            Proto.BeginVisualOnly();
            Vector3 arrival = RampPoint(1f);
            float arrivalAngle = Mathf.Atan2(arrival.z, arrival.x);
            for (int k = 0; k < 36; k++)
            {
                float a = k / 36f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, arrivalAngle * Mathf.Rad2Deg)) < 25f || k % 2 == 1) continue;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Radius - 0.4f) + Vector3.up * (Height + 0.6f);
                GameObject m = Proto.Cube(t, p, new Vector3(0.8f, 1.2f, 1.6f), StoneDark, "Merlon");
                m.transform.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), Vector3.up);
            }
            // Un cercle de runes autour du socle.
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, Height + 0.02f, 0f), new Vector3(7f, 0.02f, 7f), Color.white, "Cercle");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.9f, 0.7f, 0.3f), 0.6f);
            Proto.Cylinder(t, new Vector3(0f, Height + 0.03f, 0f), new Vector3(6.4f, 0.02f, 6.4f), StoneDark, "Dalle");
            Proto.EndVisualOnly();
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                Castle.Torch(t, new Vector3(Mathf.Cos(a) * 7.5f, Height, Mathf.Sin(a) * 7.5f), 2f);
            }
            GameObject glowGo = new GameObject("Lueur du sommet");
            glowGo.transform.SetParent(t, false);
            glowGo.transform.localPosition = new Vector3(0f, Height + 6f, 0f);
            Light glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.8f, 0.45f);
            glow.intensity = 2f;
            glow.range = 26f;
            glow.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// UN COURANT DE LA TOUR (27/09 -- "tomber de la tour, c'etait perdre trente
    /// secondes") : un disque pale au bord interieur de la rampe, une colonne de
    /// lumiere qui monte a travers le trou du dessus. Qui marche dedans est projete
    /// seize metres plus haut, par le trou, sur le tour suivant. Un raccourci, et un
    /// moyen de revenir quand on est tombe par un trou.
    /// </summary>
    public class Updraft : MonoBehaviour
    {
        public static readonly List<Updraft> All = new List<Updraft>();
        readonly Dictionary<Seeker, float> lastLaunch = new Dictionary<Seeker, float>();
        Transform disc;

        public const float Radius = 1.05f;
        const float Lift = 28f;         // assez pour monter de 16 m et depasser le rebord
        static readonly Color Air = new Color(0.7f, 0.9f, 1f);

        public static Updraft Build(Transform parent, Vector3 at)
        {
            GameObject go = new GameObject("COURANT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Updraft u = go.AddComponent<Updraft>();
            Proto.BeginVisualOnly();
            GameObject d = Proto.Cylinder(go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(Radius * 2f, 0.03f, Radius * 2f), Color.white, "Disque");
            d.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Air, 1.8f);
            u.disc = d.transform;
            Proto.EndVisualOnly();
            LightBeam beam = LightBeam.Build(parent, at, Air, 1.3f, 17f);
            if (beam != null) beam.targetAlpha = 0.35f;
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
            if (disc != null) disc.localScale = new Vector3(Radius * 2f, 0.03f, Radius * 2f) * (1f + 0.06f * Mathf.Sin(Time.time * 5f));
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
                Ambiance.Burst(null, transform.position + Vector3.up * 0.5f, Air);
                if (s.IsPlayer || (Game.PlayerTransform != null && (Game.PlayerTransform.position - transform.position).magnitude < 30f)) Sfx.Whoosh();
            }
        }
    }

    /// <summary>
    /// UN PENDULE de la tour : une boule de pierre au bout d'une chaine, qui balaie la
    /// rampe de l'interieur vers le vide et retour. Qui est dans sa course est
    /// projete -- et lache la Couronne. On passe en le regardant, au bon moment.
    /// </summary>
    public class Pendulum : MonoBehaviour
    {
        Transform arm;
        float phase;
        bool lastLow;
        Vector3 lastHead;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();

        const float Length = 5.4f;
        // Il balaie du bord interieur (22 degres : au-dela, la boule entrait dans le mur
        // de la tour) jusqu'au-dessus du vide (68 degres).
        const float SwingIn = 22f;
        const float SwingOut = 68f;
        const float Speed = 1.7f;

        public static Pendulum Build(Transform parent, Vector3 rampCentre, Vector3 tangent, float phase)
        {
            Vector3 pivot = rampCentre + Vector3.up * 6.6f;
            GameObject go = new GameObject("PENDULE");
            go.transform.SetParent(parent, false);
            go.transform.position = pivot;
            go.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            Pendulum p = go.AddComponent<Pendulum>();
            p.phase = phase;

            Color iron = new Color(0.12f, 0.12f, 0.13f);
            Proto.BeginVisualOnly();
            // La potence : une poutre qui sort du fut jusqu'au-dessus de la rampe.
            Vector3 inward = -new Vector3(pivot.x, 0f, pivot.z).normalized;
            GameObject beam = Proto.Cube(parent, pivot + inward * 1.6f + Vector3.up * 0.3f, new Vector3(0.6f, 0.6f, 3.6f), new Color(0.22f, 0.22f, 0.22f), "Potence");
            beam.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);
            GameObject armGo = new GameObject("Bras");
            armGo.transform.SetParent(go.transform, false);
            p.arm = armGo.transform;
            Proto.Cube(p.arm, new Vector3(0f, -Length * 0.5f, 0f), new Vector3(0.12f, Length, 0.12f), iron, "Chaîne");
            Proto.Sphere(p.arm, new Vector3(0f, -Length, 0f), new Vector3(1.6f, 1.6f, 1.6f), new Color(0.3f, 0.3f, 0.3f), "Boule");
            GameObject band = Proto.Cylinder(p.arm, new Vector3(0f, -Length, 0f), new Vector3(1.7f, 0.1f, 1.7f), Color.white, "Rune");
            band.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.45f, 0.25f), 2f);
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
            float mid = (SwingOut - SwingIn) * 0.5f, half = (SwingOut + SwingIn) * 0.5f;
            float wave = Mathf.Sin(Time.time * Speed + phase);
            float angle = mid + half * wave;
            // Il siffle quand il passe a pleine vitesse (au milieu de sa course), si tu
            // es tout pres : on l'entend venir.
            bool low = wave > 0f;
            if (low != lastLow)
            {
                lastLow = low;
                Transform p = Game.PlayerTransform;
                if (p != null && (p.position - transform.position).magnitude < 14f) Sfx.Whoosh();
            }
            arm.localRotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 head = Head;
            Vector3 velocity = (head - lastHead) / dt;
            lastHead = head;
            if (Game.Season == null || !Game.Season.Running) return;

            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                if ((s.Body.position + Vector3.up * 1f - head).magnitude > 1.7f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 1f) continue;
                lastHit[s] = Time.time;
                Vector3 push = velocity.sqrMagnitude > 1f ? new Vector3(velocity.x, 0f, velocity.z).normalized : transform.right;
                Combat.Hit(s, push * 17f + Vector3.up * 6f, 0.3f, true, null);
                Sfx.Clang();
            }
        }
    }
}
