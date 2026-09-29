using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE PARCOURS JUSQU'AU CHATEAU (28/09 -- Martin : "que la route pour aller jusqu'au
    /// chateau soit plus compliquee", "vraiment plus d'obstacles, que ce soit aleatoire").
    ///
    /// Devant chacune des quatre portes, un COULOIR de trente metres, borde de murets
    /// qu'on ne saute pas : c'est le seul chemin vers la porte. Dedans, quatre STATIONS
    /// tirees au hasard a chaque manche, parmi :
    ///   - une CHICANE : un mur en travers, avec un passage d'un cote ou de l'autre ;
    ///   - un MOULINET : une barre qui tourne a hauteur de genou (on saute) ;
    ///   - une HERSE : des pointes qui jaillissent du sol (on attend, ou on court) ;
    ///   - un MARTEAU : une masse de fer qui balaie le couloir d'un mur a l'autre.
    /// Tout ce qui frappe projette fort -- par-dessus les murets, parfois.
    ///
    /// Les bots passent par les memes couloirs (Route, Exit : les points de passage).
    /// </summary>
    public static class Course
    {
        /// <summary>La longueur des couloirs, depuis le pied du rempart.</summary>
        public const float Length = 30f;
        /// <summary>La demi-largeur interieure d'un couloir.</summary>
        public const float HalfWidth = 9f;
        const int Stations = 4;

        static readonly Vector3[] Axes = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        // Pour chaque porte : ou passer, de l'entree du couloir jusqu'a la porte.
        static readonly List<Vector3>[] Openings = { new List<Vector3>(), new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };

        static readonly Color Stone = new Color(0.78f, 0.71f, 0.60f);
        static readonly Color StoneDark = new Color(0.55f, 0.47f, 0.40f);
        static readonly Color Rune = new Color(1f, 0.55f, 0.25f);

        /// <summary>Le point, pour la porte "gate", a "d" metres du rempart et "lateral" metres de l'axe.</summary>
        public static Vector3 At(int gate, float d, float lateral)
        {
            Vector3 axis = Axes[gate];
            Vector3 side = new Vector3(axis.z, 0f, -axis.x);
            Vector3 p = axis * (Castle.HalfSize + d) + side * lateral;
            return Ground.Place(p.x, p.z, 0f);
        }

        public static void Build(Transform parent, int seed)
        {
            System.Random rng = new System.Random(seed * 131 + 7);
            GameObject root = new GameObject("PARCOURS DES PORTES");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            // LES MEMES STATIONS DEVANT LES QUATRE PORTES (comme les rampes de la tour :
            // personne n'a le couloir facile). On tire la suite une fois, puis chaque
            // porte la construit a l'identique.
            int[] kinds = new int[Stations];
            int[] sides = new int[Stations];
            float[] speeds = new float[Stations];
            float[] phases = new float[Stations];
            int lastChicane = 0;
            for (int k = 0; k < Stations; k++)
            {
                kinds[k] = rng.Next(4);
                // Jamais deux chicanes de suite du meme cote : on zigzague.
                if (kinds[k] == 0)
                {
                    sides[k] = lastChicane == 0 ? (rng.NextDouble() < 0.5 ? -1 : 1) : -lastChicane;
                    lastChicane = sides[k];
                }
                else
                {
                    sides[k] = rng.NextDouble() < 0.5 ? -1 : 1;
                    lastChicane = 0;
                }
                speeds[k] = (float)rng.NextDouble();
                phases[k] = (float)rng.NextDouble();
            }
            for (int g = 0; g < 4; g++)
            {
                Openings[g].Clear();
                Vector3 axis = Axes[g];
                Quaternion face = Quaternion.LookRotation(axis, Vector3.up);
                // Les murets du couloir : 3 m de haut (on ne les saute pas), des creneaux, des torches.
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 a = At(g, 2f, side * (HalfWidth + 0.8f));
                    Vector3 b = At(g, Length, side * (HalfWidth + 0.8f));
                    Vector3 mid = (a + b) * 0.5f + Vector3.up * 1.5f;
                    GameObject wall = Proto.Cube(t, mid, new Vector3(1.6f, 3f, (b - a).magnitude), StoneDark, "Muret");
                    wall.transform.rotation = face;
                    Proto.BeginVisualOnly();
                    for (int k = 0; k < 9; k++)
                    {
                        Vector3 m = Vector3.Lerp(a, b, (k + 0.5f) / 9f) + Vector3.up * 3.3f;
                        GameObject merlon = Proto.Cube(t, m, new Vector3(1.6f, 0.7f, 1.6f), Stone, "Merlon");
                        merlon.transform.rotation = face;
                        GameObject cap = Proto.Cube(t, m + Vector3.up * 0.42f, new Vector3(1.76f, 0.14f, 1.76f), StoneDark, "Chaperon");
                        cap.transform.rotation = face;
                    }
                    Proto.EndVisualOnly();
                    Castle.Torch(t, b + Vector3.up * 3f - axis * 0.5f, 1.4f);
                }
                // L'entree du couloir : une arche de lumiere, on la voit du ciel.
                Proto.BeginVisualOnly();
                GameObject arch = Proto.Cube(t, At(g, Length, 0f) + Vector3.up * 5.5f, new Vector3(HalfWidth * 2f + 3.2f, 1f, 1.4f), Stone, "Arche");
                arch.transform.rotation = face;
                GameObject glow = Proto.Cube(t, At(g, Length + 0.75f, 0f) + Vector3.up * 5.5f, new Vector3(HalfWidth * 1.4f, 0.3f, 0.05f), Color.white, "Rune de l'arche");
                glow.transform.rotation = face;
                glow.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 1.1f);
                for (int side = -1; side <= 1; side += 2)
                {
                    // Des piliers ronds, coiffes d'une boule (plus de poteaux carres).
                    Vector3 foot = At(g, Length, side * (HalfWidth + 0.8f));
                    Proto.Cylinder(t, foot + Vector3.up * 3f, new Vector3(1.9f, 3f, 1.9f), Stone, "Pilier");
                    Proto.Cylinder(t, foot + Vector3.up * 6.1f, new Vector3(2.3f, 0.2f, 2.3f), StoneDark, "Chapiteau");
                    Proto.Sphere(t, foot + Vector3.up * 6.75f, Vector3.one * 0.9f, Stone, "Boule");
                }
                Proto.EndVisualOnly();

                // Les stations, de l'entree vers la porte.
                Openings[g].Add(At(g, Length + 4f, 0f));
                for (int k = 0; k < Stations; k++)
                {
                    float d = Length - 4f - k * 5.5f;
                    int kind = kinds[k];
                    if (kind == 0)
                    {
                        int sideOpen = sides[k];
                        Chicane(t, g, d, sideOpen, face);
                        Openings[g].Add(At(g, d + 2.5f, sideOpen * (HalfWidth - 2.5f)));
                        Openings[g].Add(At(g, d - 2.5f, sideOpen * (HalfWidth - 2.5f)));
                        continue;
                    }
                    Vector3 c = At(g, d, 0f);
                    if (kind == 1)
                    {
                        Sweeper.Build(t, c, Axes[g], HalfWidth - 0.4f, (1.3f + speeds[k] * 0.6f) * Tower.Hardness, phases[k] * 6f, false);
                        // (Les bots contournent son pied.)
                        Openings[g].Add(At(g, d, sides[k] * 3.5f));
                    }
                    else if (kind == 2) SpikeTrap.Build(t, c, Axes[g], HalfWidth * 2f, 4f, (2.6f + speeds[k]) / Tower.Hardness, phases[k] * 3f);
                    else Maul.Build(t, c, Axes[g], phases[k] * 6f);
                }
                Openings[g].Add(At(g, 9f, 0f));
            }
        }

        /// <summary>Un mur en travers du couloir, un passage de cinq metres d'un cote.</summary>
        static void Chicane(Transform t, int gate, float d, int openSide, Quaternion face)
        {
            // Le mur couvre de l'autre bord jusqu'a 5 m du bord ouvert.
            float from = -openSide * HalfWidth;
            float to = openSide * (HalfWidth - 5f);
            Vector3 a = At(gate, d, from);
            Vector3 b = At(gate, d, to);
            GameObject wall = Proto.Cube(t, (a + b) * 0.5f + Vector3.up * 1.6f, new Vector3((b - a).magnitude, 3.2f, 1.2f), Stone, "Chicane");
            wall.transform.rotation = face;
            Proto.BeginVisualOnly();
            GameObject rune = Proto.Cube(t, (a + b) * 0.5f + Vector3.up * 2.2f + Axes[gate] * 0.62f, new Vector3(1f, 1f, 0.04f), Color.white, "Rune");
            rune.transform.rotation = face;
            rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 1.1f);
            Proto.EndVisualOnly();
        }

        /// <summary>Le parvis devant le couloir de la porte "gate" (la ou les arbalestes des plateformes posent).</summary>
        public static Vector3 Plaza(int gate, float lateral) { return At(gate, Length + 5f, lateral) + Vector3.up * 0.05f; }

        /// <summary>La direction (depuis le centre) de la porte "gate".</summary>
        public static Vector3 Axis(int gate) { return Axes[gate]; }

        public static int GateFor(Vector3 from)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < Axes.Length; i++)
            {
                float d = (from - Axes[i] * Castle.HalfSize).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Pour un bot qui entre : les points de passage de "from" jusqu'a la porte (le
        /// couloir, ses chicanes). S'il est deja dans le couloir, ceux qui restent devant.
        /// </summary>
        public static List<Vector3> Route(Vector3 from)
        {
            List<Vector3> list = new List<Vector3>();
            int g = GateFor(from);
            List<Vector3> o = Openings[g];
            Vector3 side = new Vector3(Axes[g].z, 0f, -Axes[g].x);
            float mine = Vector3.Dot(new Vector3(from.x, 0f, from.z), Axes[g]) - Castle.HalfSize;
            // Hors du couloir (a cote de lui) : il faut passer par son entree.
            if (Mathf.Abs(Vector3.Dot(from, side)) > HalfWidth) mine = float.MaxValue;
            for (int i = 0; i < o.Count; i++)
            {
                float d = Vector3.Dot(new Vector3(o[i].x, 0f, o[i].z), Axes[g]) - Castle.HalfSize;
                if (d < mine + 1f) list.Add(o[i]);
            }
            return list;
        }

        /// <summary>Pour un bot qui sort par la porte "towards" : les points de passage a l'envers.</summary>
        public static List<Vector3> Exit(Vector3 towards)
        {
            List<Vector3> list = new List<Vector3>();
            List<Vector3> o = Openings[GateFor(towards)];
            for (int i = o.Count - 1; i >= 0; i--) list.Add(o[i]);
            return list;
        }
    }

    /// <summary>
    /// UN MOULINET ou UN BALAYEUR : une barre de fer cloutee a hauteur de genou. Le
    /// moulinet tourne sur lui-meme (au milieu d'un couloir) ; le balayeur, pose contre
    /// le fut de la tour, balaie la rampe d'avant en arriere, VERS LE VIDE. On saute
    /// par-dessus. Ses bouts luisent ; il siffle en passant.
    /// </summary>
    public class Sweeper : MonoBehaviour
    {
        public static readonly List<Sweeper> All = new List<Sweeper>();
        Transform bar;
        float length;
        float speed;
        float phase;
        bool wiper;
        Vector3 lastTip;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();
        const float BarHeight = 0.8f;

        public static Sweeper Build(Transform parent, Vector3 foot, Vector3 facing, float length, float speed, float phase, bool wiper)
        {
            GameObject go = new GameObject(wiper ? "BALAYEUR" : "MOULINET");
            go.transform.SetParent(parent, false);
            go.transform.position = foot;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z).normalized, Vector3.up);
            Sweeper w = go.AddComponent<Sweeper>();
            w.length = length;
            w.speed = speed;
            w.phase = phase;
            w.wiper = wiper;
            Color iron = new Color(0.16f, 0.16f, 0.18f);
            // Le pied (on s'y cogne : un vrai collider fin).
            Proto.Cylinder(go.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 0.7f, 0.6f), new Color(0.3f, 0.28f, 0.26f), "Pied");
            Proto.BeginVisualOnly();
            w.bar = new GameObject("Barre").transform;
            w.bar.SetParent(go.transform, false);
            w.bar.localPosition = new Vector3(0f, BarHeight, 0f);
            float reach = length;
            float start = wiper ? 0f : -length;
            float span = wiper ? length : length * 2f;
            Proto.Cube(w.bar, new Vector3(0f, 0f, start + span * 0.5f), new Vector3(0.28f, 0.28f, span), new Color(0.34f, 0.24f, 0.15f), "Poutre");
            int studs = Mathf.RoundToInt(span / 0.9f);
            for (int k = 0; k < studs; k++)
            {
                GameObject stud = Proto.Cube(w.bar, new Vector3(0f, 0.18f, start + (k + 0.5f) * span / studs), new Vector3(0.12f, 0.18f, 0.12f), iron, "Clou");
                stud.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
            // Les bouts : des boules de fer a bande ambre (on les voit venir sans qu'elles brillent comme des lampes).
            GameObject tipA = Proto.Sphere(w.bar, new Vector3(0f, 0f, reach), Vector3.one * 0.5f, Color.white, "Bout");
            tipA.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.45f, 0.2f), 1.3f);
            if (!wiper)
            {
                GameObject tipB = Proto.Sphere(w.bar, new Vector3(0f, 0f, -reach), Vector3.one * 0.5f, Color.white, "Bout");
                tipB.GetComponent<Renderer>().sharedMaterial = tipA.GetComponent<Renderer>().sharedMaterial;
            }
            Proto.EndVisualOnly();
            Fx.KeepTrail(w.bar, new Vector3(0f, 0f, reach), new Color(1f, 0.45f, 0.2f), 0.5f, 0.25f);
            if (!wiper) Fx.KeepTrail(w.bar, new Vector3(0f, 0f, -reach), new Color(1f, 0.45f, 0.2f), 0.5f, 0.25f);
            w.lastTip = w.Tip;
            All.Add(w);
            MaterialFactory.Polish(w.transform, 0.6f);
            return w;
        }

        void OnDestroy() { All.Remove(this); }

        Vector3 Tip { get { return bar.TransformPoint(new Vector3(0f, 0f, length)); } }

        /// <summary>L'angle de la barre (degres) a l'instant.</summary>
        float Angle(float time)
        {
            // Le balayeur : de -80 a +80 degres autour de "vers le vide". Le moulinet : il tourne.
            if (wiper) return Mathf.Sin(time * speed * 1.6f + phase) * 80f;
            return (time * speed * 90f + phase * 57f) % 360f;
        }

        /// <summary>Pour un bot : une barre passe-t-elle bientot a "p" (il doit sauter) ?</summary>
        public static bool Threat(Vector3 p)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Sweeper w = All[i];
                if (w == null) continue;
                Vector3 d = p - w.transform.position;
                if (Mathf.Abs(d.y) > 1.5f) continue;
                d.y = 0f;
                float r = d.magnitude;
                if (r > w.length + 0.5f || r < 0.6f) continue;
                // La barre sera-t-elle sur lui dans le quart de seconde qui vient ?
                Vector3 local = w.transform.InverseTransformDirection(d.normalized);
                float at = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                for (int k = 0; k < 3; k++)
                {
                    float a = w.Angle(Time.time + k * 0.12f);
                    if (Mathf.Abs(Mathf.DeltaAngle(a, at)) < 20f || !w.wiper && Mathf.Abs(Mathf.DeltaAngle(a + 180f, at)) < 20f) return true;
                }
            }
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bar.localRotation = Quaternion.Euler(0f, Angle(Time.time), 0f);
            Vector3 tip = Tip;
            Vector3 tipVelocity = (tip - lastTip) / dt;
            lastTip = tip;
            if (Game.Season == null || !Game.Season.Running) return;
            Vector3 barDir = bar.forward;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                // Au-dessus de la barre (en plein saut) : elle passe dessous.
                if (d.y > BarHeight + 0.1f || d.y < -1f) continue;
                Vector3 flat = new Vector3(d.x, 0f, d.z);
                float along = Vector3.Dot(flat, barDir);
                if (wiper ? (along < 0f || along > length) : Mathf.Abs(along) > length) continue;
                float off = (flat - barDir * along).magnitude;
                if (off > 0.75f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 0.8f) continue;
                lastHit[s] = Time.time;
                // Pousse dans le sens ou la barre tourne (et, le balayeur, vers le vide).
                Vector3 push = new Vector3(tipVelocity.x, 0f, tipVelocity.z);
                if (!wiper && along < 0f) push = -push;
                push = push.sqrMagnitude > 0.1f ? push.normalized : transform.forward;
                if (wiper) push = (push + transform.forward).normalized;
                Combat.Hit(s, push * 26f + Vector3.up * 9f, 0.35f, true, null);
                Fx.ObstacleHit(s, new Color(1f, 0.5f, 0.25f));
                Sfx.Clang();
            }
        }
    }

    /// <summary>
    /// UNE HERSE : une grille de fer au ras du sol. Toutes les quelques secondes, ses
    /// runes ROUGISSENT (trois quarts de seconde) puis des POINTES jaillissent et
    /// envoient en l'air ce qui est dessus.
    /// </summary>
    public class SpikeTrap : MonoBehaviour
    {
        Transform spikes;
        Renderer grid;
        float width, depth, period, phase;
        int shown = -1;
        bool fired;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();
        const float Warn = 0.75f;
        // Des pointes de 55 cm, rentrees de 60 : completement dans la dalle (60 cm d'epaisseur).
        const float Tall = 0.55f;
        const float Hidden = -0.6f;

        public static SpikeTrap Build(Transform parent, Vector3 centre, Vector3 along, float width, float depth, float period, float phase)
        {
            GameObject go = new GameObject("HERSE");
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            // (30/09 -- "les piquots bug") Posee A PLAT SUR LA PENTE : avant, une herse
            // horizontale sur la rampe (qui monte de 26 degres) avait la moitie de ses
            // pointes rentrees qui depassaient de la dalle, en rangee.
            go.transform.rotation = Quaternion.LookRotation(along.normalized, Vector3.up);
            SpikeTrap h = go.AddComponent<SpikeTrap>();
            h.width = width;
            h.depth = depth;
            h.period = Mathf.Max(1.8f, period);
            h.phase = phase;
            Proto.BeginVisualOnly();
            Proto.Cube(go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(width, 0.06f, depth), new Color(0.12f, 0.12f, 0.13f), "Grille");
            GameObject g = Proto.Cube(go.transform, new Vector3(0f, 0.07f, 0f), new Vector3(width * 0.94f, 0.02f, depth * 0.9f), Color.white, "Runes");
            h.grid = g.GetComponent<Renderer>();
            h.spikes = new GameObject("Pointes").transform;
            h.spikes.SetParent(go.transform, false);
            int cols = Mathf.Max(2, Mathf.RoundToInt(width / 0.8f));
            int rows = Mathf.Max(2, Mathf.RoundToInt(depth / 0.8f));
            Color iron = new Color(0.55f, 0.55f, 0.6f);
            for (int x = 0; x < cols; x++)
                for (int z = 0; z < rows; z++)
                {
                    Vector3 at = new Vector3(-width * 0.5f + (x + 0.5f) * width / cols, 0f, -depth * 0.5f + (z + 0.5f) * depth / rows);
                    Proto.Cone(h.spikes, at, 0.14f, Tall, iron, "Pointe", 10);
                }
            Proto.EndVisualOnly();
            h.spikes.localPosition = new Vector3(0f, Hidden, 0f);
            MaterialFactory.Polish(h.transform, 0.6f);
            return h;
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.time + phase, period);
            // 0 -> period-Warn : cache ; puis l'alerte ; puis les pointes (0,6 s).
            float up = t < 0.12f ? t / 0.12f : t < 0.6f ? 1f : t < 0.9f ? 1f - (t - 0.6f) / 0.3f : 0f;
            spikes.localPosition = new Vector3(0f, Mathf.Lerp(Hidden, 0.05f, up), 0f);
            // Rentrees : on ne les dessine pas du tout (rien ne depasse, jamais).
            bool show = up > 0.01f;
            if (spikes.gameObject.activeSelf != show) spikes.gameObject.SetActive(show);
            int mood = t > period - Warn ? 2 : up > 0.5f ? 1 : 0;
            if (mood != shown)
            {
                shown = mood;
                Color c = mood == 2 ? new Color(1f, 0.15f, 0.1f) : mood == 1 ? new Color(1f, 0.7f, 0.3f) : new Color(0.55f, 0.3f, 0.2f);
                grid.sharedMaterial = MaterialFactory.GetGlow(c, mood == 2 ? 3f : 1f);
            }
            // Les pointes piquent TANT QU'ELLES SONT SORTIES (avant, seulement l'instant ou
            // elles jaillissaient : on traversait ensuite une herse herissee sans rien sentir).
            if (up < 0.5f) { fired = false; return; }
            if (Game.Season == null || !Game.Season.Running) return;
            bool near = false;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 local = transform.InverseTransformPoint(s.Body.position);
                if (Mathf.Abs(local.x) > width * 0.5f + 0.3f || Mathf.Abs(local.z) > depth * 0.5f + 0.3f || local.y < -0.5f || local.y > 1.2f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 1f) continue;
                lastHit[s] = Time.time;
                Vector3 side = transform.right * (local.x >= 0f ? 1f : -1f);
                Combat.Hit(s, Vector3.up * 20f + side * 6f, 0.35f, true, null);
                Fx.ObstacleHit(s, new Color(1f, 0.35f, 0.2f));
                near = true;
            }
            if (fired && !near) return;
            Transform p = Game.PlayerTransform;
            if (near || p != null && (p.position - transform.position).magnitude < 14f) Sfx.TrapSnap();
            if (!fired) Fx.Burst(transform.position + Vector3.up * 0.2f, new Color(0.7f, 0.62f, 0.52f), 16, 5f, 0.3f, 0.6f, 0.3f, Vector3.up, 50f);
            fired = true;
        }
    }

    /// <summary>
    /// UN MARTEAU : deux montants, une traverse, et une masse de fer au bout d'un
    /// manche qui balaie le couloir d'un mur a l'autre. Sa tete luit ; au passage,
    /// elle projette tres loin sur le cote.
    /// </summary>
    public class Maul : MonoBehaviour
    {
        Transform arm;
        float phase;
        Vector3 lastHead;
        readonly Dictionary<Seeker, float> lastHit = new Dictionary<Seeker, float>();
        const float Height = 8f;
        const float Length = 6.8f;
        const float Swing = 70f;
        const float Speed = 1.4f;

        public static Maul Build(Transform parent, Vector3 centre, Vector3 along, float phase)
        {
            GameObject go = new GameObject("MARTEAU");
            go.transform.SetParent(parent, false);
            go.transform.position = centre + Vector3.up * Height;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(along.x, 0f, along.z).normalized, Vector3.up);
            Maul m = go.AddComponent<Maul>();
            m.phase = phase;
            Color wood = new Color(0.34f, 0.24f, 0.15f);
            Color iron = new Color(0.15f, 0.15f, 0.17f);
            // Le portique (les montants arretent les joueurs : contre les murets du couloir).
            for (int side = -1; side <= 1; side += 2)
            {
                Proto.Cube(parent, centre + go.transform.right * side * (Course.HalfWidth + 0.2f) + Vector3.up * Height * 0.5f, new Vector3(0.8f, Height, 0.8f), wood, "Montant").transform.rotation = go.transform.rotation;
            }
            Proto.BeginVisualOnly();
            GameObject beam = Proto.Cube(parent, centre + Vector3.up * (Height + 0.3f), new Vector3(Course.HalfWidth * 2f + 1.6f, 0.7f, 0.9f), wood, "Traverse");
            beam.transform.rotation = go.transform.rotation;
            m.arm = new GameObject("Manche").transform;
            m.arm.SetParent(go.transform, false);
            Proto.Cube(m.arm, new Vector3(0f, -Length * 0.5f, 0f), new Vector3(0.3f, Length, 0.3f), wood, "Manche");
            // La masse : un tonneau de fer couche (plus de pave), cercle d'une bande ambre.
            GameObject head = Proto.Cylinder(m.arm, new Vector3(0f, -Length, 0f), new Vector3(1.55f, 1.3f, 1.55f), iron, "Masse");
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject cap = Proto.Sphere(m.arm, new Vector3(side * 1.3f, -Length, 0f), new Vector3(0.5f, 1.3f, 1.3f), iron, "Bout");
                cap.transform.localRotation = Quaternion.identity;
            }
            GameObject band = Proto.Cylinder(m.arm, new Vector3(0f, -Length, 0f), new Vector3(1.62f, 0.14f, 1.62f), Color.white, "Rune");
            band.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            band.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.45f, 0.2f), 1.3f);
            Proto.EndVisualOnly();
            Fx.KeepTrail(m.arm, new Vector3(0f, -Length, 0f), new Color(1f, 0.5f, 0.2f), 1.6f, 0.3f);
            m.lastHead = m.Head;
            MaterialFactory.Polish(m.transform, 0.6f);
            return m;
        }

        Vector3 Head { get { return arm.TransformPoint(new Vector3(0f, -Length, 0f)); } }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            arm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * Speed * Tower.Hardness + phase) * Swing);
            Vector3 head = Head;
            Vector3 velocity = (head - lastHead) / dt;
            lastHead = head;
            if (Game.Season == null || !Game.Season.Running) return;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                if ((s.Body.position + Vector3.up * 1f - head).magnitude > 2f) continue;
                float last;
                if (lastHit.TryGetValue(s, out last) && Time.time - last < 1f) continue;
                lastHit[s] = Time.time;
                Vector3 push = new Vector3(velocity.x, 0f, velocity.z);
                push = push.sqrMagnitude > 1f ? push.normalized : transform.right;
                Combat.Hit(s, push * 32f + Vector3.up * 12f, 0.45f, true, null);
                Fx.ObstacleHit(s, new Color(1f, 0.5f, 0.25f));
                Sfx.Crash();
            }
        }
    }
}
