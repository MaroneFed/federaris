using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA COURSE D'ELYTRES (v48, 28/10 -- Martin : "le jeu est invendable ; j'adore les elytres,
    /// je vole comme dans Minecraft ; je veux que le jeu se transforme en course, des courses
    /// d'elytres ; on garde les persos, on peut se pousser, c'est incroyable" ; puis "ameliore le
    /// full et fais des dingueries").
    ///
    /// Le PROTOTYPE, a cote du jeu de la Couronne (rien n'est perdu : au salon, Mode : COURSE /
    /// Normal / DIEU). La meme ile, les memes haricots, les memes ailes ; mais :
    ///   - un CIRCUIT de PORTES dans le ciel autour de l'ile : au ras des falaises, entre les
    ///     ilots, au-dessus des murailles, en slalom autour de la tour ; 3 tours ;
    ///   - le DEPART sur une plateforme dans le ciel : 3, 2, 1, et tout le monde plonge ;
    ///   - le VOL d'ELYTRES (Wings.Fly, reglages de course) : on pique pour prendre de la vitesse,
    ///     on remonte en la depensant ; chaque porte RELANCE (les portes d'or, deux fois plus) ;
    ///   - L'ASPIRATION : dans le sillage d'un autre, a moins de 25 m derriere lui, l'air te tire ;
    ///   - LA POUSSEE en plein vol (clic droit) : le pousse part en vrille, ailes fermees ;
    ///   - tomber dans les nuages : on repart de la DERNIERE PORTE passee, deja en vol ;
    ///   - le premier qui boucle ses 3 tours gagne la manche (sur l'hote, en ligne).
    /// </summary>
    public class Race : MonoBehaviour
    {
        public static Race Instance { get; private set; }
        public static bool On { get { return Match.RaceMode && Instance != null; } }

        public const int Laps = 3;
        public const float GateRadius = 11f;
        /// <summary>L'elan donne par une porte (m/s) ; une porte d'or, le double.</summary>
        public const float GateBoost = 7f;

        public sealed class Gate
        {
            public Vector3 Centre;
            public Vector3 Normal;
            public bool Gold;
            public Transform Root;
            public List<LineRenderer> Lines = new List<LineRenderer>();
        }

        public static readonly List<Gate> Gates = new List<Gate>();

        sealed class Runner
        {
            public Seeker Who;
            public int Next = 0;        // la prochaine porte a passer
            public int Lap = 0;         // tours finis
            public Vector3 Last;        // sa position a l'image d'avant
            public bool Has;            // "Last" est valable
            public bool Done;
            public float DoneAt;
            public int Place;
        }

        static readonly List<Runner> runners = new List<Runner>();
        static readonly List<Seeker> finish = new List<Seeker>();
        static Vector3 startPos;
        static Vector3 startDir;
        static bool launched;
        static float bannerAt = -9f;
        static string banner;
        static Color bannerColour;

        // ================================================================ le circuit

        /// <summary>
        /// LE CIRCUIT (angle autour de l'ile en degres, distance au centre, hauteur). Il tourne
        /// dans le sens des angles croissants. Les portes d'or relancent deux fois plus.
        /// </summary>
        static readonly Vector3[] Layout =
        {
            new Vector3(0f, 175f, 62f),        // 0 : la ligne (depart, arrivee)
            new Vector3(28f, 150f, 46f),
            new Vector3(55f, 122f, 14f),       // au ras de la falaise
            new Vector3(85f, 165f, 4f),        // tout en bas, au-dessus des nuages
            new Vector3(118f, 192f, 34f),
            new Vector3(150f, 150f, 72f),
            new Vector3(178f, 78f, 92f),       // on rentre, haut, au-dessus des murailles
            new Vector3(215f, 36f, 72f),       // le slalom autour de la tour
            new Vector3(258f, 36f, 52f),
            new Vector3(292f, 84f, 42f),       // on ressort par-dessus le mur
            new Vector3(318f, 140f, 24f),
            new Vector3(340f, 178f, 44f),
        };
        static bool IsGold(int i) { return i == 3 || i == 7 || i == 10; }

        /// <summary>A l'ouverture de la manche (GameBootstrap), en mode course seulement.</summary>
        public static void Build(Transform root)
        {
            Gates.Clear();
            runners.Clear();
            finish.Clear();
            launched = false;
            banner = null;
            GameObject go = new GameObject("=== LA COURSE ===");
            go.transform.SetParent(root, false);
            Instance = go.AddComponent<Race>();

            // Le jeu de la Couronne se range : Couronne, Monuments, sanctuaires, arbalestes, anneaux.
            if (Crown.Instance != null) Crown.Instance.gameObject.SetActive(false);
            for (int i = 0; i < Monument.All.Count; i++) if (Monument.All[i] != null) Monument.All[i].gameObject.SetActive(false);
            for (int i = 0; i < Shrine.All.Count; i++) if (Shrine.All[i] != null) Shrine.All[i].gameObject.SetActive(false);
            for (int i = 0; i < Ballista.All.Count; i++) if (Ballista.All[i] != null) Ballista.All[i].gameObject.SetActive(false);
            for (int i = 0; i < WindRing.All.Count; i++) if (WindRing.All[i] != null) WindRing.All[i].gameObject.SetActive(false);

            Physics.SyncTransforms();
            int n = Layout.Length;
            Vector3[] points = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = Layout[i].x * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * Layout[i].y, Layout[i].z, Mathf.Sin(a) * Layout[i].y);
                // Une porte ne rentre jamais dans le decor : on la remonte tant qu'elle touche.
                for (int k = 0; k < 14 && Physics.CheckSphere(p, GateRadius * 0.9f, ~0, QueryTriggerInteraction.Ignore); k++) p.y += 5f;
                points[i] = p;
            }
            for (int i = 0; i < n; i++)
            {
                Vector3 prev = points[(i - 1 + n) % n], next = points[(i + 1) % n];
                Gate g = new Gate { Centre = points[i], Normal = (next - prev).normalized, Gold = IsGold(i) };
                BuildGate(go.transform, g, i);
                Gates.Add(g);
            }

            // LE DEPART : une plateforme dans le ciel, 70 m avant la ligne, face a elle.
            startDir = Gates[0].Normal;
            Vector3 flat = new Vector3(startDir.x, 0f, startDir.z).normalized;
            startPos = Gates[0].Centre - flat * 70f + Vector3.up * 14f;
            Ground.BuildPad(go.transform, startPos, 11f, 9001);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                runners.Add(new Runner { Who = s });
                Place(s, GridSpot(i), flat);
            }
            Debug.Log("[FIEF] Course d'élytres : " + Gates.Count + " portes, " + Laps + " tours, " + runners.Count + " coureurs.");
        }

        static Vector3 GridSpot(int i)
        {
            Vector3 flat = new Vector3(startDir.x, 0f, startDir.z).normalized;
            Vector3 side = new Vector3(flat.z, 0f, -flat.x);
            int row = i / 4, col = i % 4;
            return startPos + Vector3.up * 0.3f + side * ((col - 1.5f) * 3.2f) - flat * (row * 3.5f - 2f);
        }

        static void Place(Seeker s, Vector3 at, Vector3 dir)
        {
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (s.IsPlayer && Game.Player != null) Game.Player.Teleport(at, yaw);
            else
            {
                Rival r = Rival.Of(s);
                if (r == null) return;
                r.Teleport(at);
                r.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        static void BuildGate(Transform parent, Gate g, int index)
        {
            GameObject go = new GameObject("PORTE " + index);
            go.transform.SetParent(parent, false);
            go.transform.position = g.Centre;
            go.transform.rotation = Quaternion.LookRotation(g.Normal, Vector3.up);
            g.Root = go.transform;
            Material m = Ambiance.Additive;
            if (m == null) return;
            for (int k = 0; k < 3; k++)
            {
                GameObject ring = new GameObject("Cercle");
                ring.transform.SetParent(go.transform, false);
                LineRenderer line = ring.AddComponent<LineRenderer>();
                line.sharedMaterial = m;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 56;
                line.widthMultiplier = k == 0 ? 1.1f : 0.45f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                float radius = GateRadius * (k == 0 ? 1f : k == 1 ? 0.88f : 1.12f);
                for (int i = 0; i < 56; i++)
                {
                    float a = i / 56f * Mathf.PI * 2f;
                    float notch = k == 1 ? 1f + 0.06f * Mathf.Sin(a * 8f) : 1f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * notch, Mathf.Sin(a) * radius * notch, 0f));
                }
                g.Lines.Add(line);
            }
        }

        // ================================================================ chaque image

        void Update()
        {
            Season season = Game.Season;
            bool running = season != null && season.Running;
            // "PARTEZ" : tout le monde plonge de la plateforme.
            if (running && !launched)
            {
                launched = true;
                for (int i = 0; i < runners.Count; i++) Launch(runners[i].Who, startDir, 26f);
                Banner("PLONGEZ !", new Color(1f, 0.82f, 0.3f));
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Kick(10f);
            }
            if (running) for (int i = 0; i < runners.Count; i++) Track(runners[i]);
            Rank();
            Paint();
        }

        static void Track(Runner r)
        {
            Seeker s = r.Who;
            if (s == null || s.Body == null || r.Done) return;
            Vector3 p = s.Body.position + Vector3.up * 1f;
            if (r.Has && Gates.Count > 0)
            {
                Gate g = Gates[r.Next];
                float d0 = Vector3.Dot(r.Last - g.Centre, g.Normal), d1 = Vector3.Dot(p - g.Centre, g.Normal);
                if (d0 < 0f && d1 >= 0f)
                {
                    Vector3 hit = r.Last + (p - r.Last) * (d0 / (d0 - d1));
                    if ((hit - g.Centre).magnitude <= GateRadius + 1.5f) Passed(r, g);
                }
                // L'ASPIRATION : derriere un autre, dans son sillage, l'air te tire.
                Slipstream(s);
            }
            r.Last = p;
            r.Has = true;
        }

        static void Passed(Runner r, Gate g)
        {
            Seeker s = r.Who;
            int was = r.Next;
            r.Next = (r.Next + 1) % Gates.Count;
            s.PendingBoost += g.Gold ? GateBoost * 2f : GateBoost;
            s.BoostUntil = Mathf.Max(s.BoostUntil, Time.time + 0.6f);
            Color c = g.Gold ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 0.85f, 1f);
            Fx.Ring(g.Centre, c, GateRadius * 0.9f, GateRadius * 2.2f, 0.45f, 0.6f, g.Normal);
            Fx.Burst(g.Centre, c, g.Gold ? 90 : 40, 16f, 0.25f, 0.7f, 0f, g.Normal, 30f);
            Sfx.Boost(g.Centre, s.IsPlayer);
            // Une porte d'or : une trainee d'or derriere soi, le temps de l'elan.
            if (g.Gold) Fx.Trail(s.Body, new Color(1f, 0.8f, 0.3f), 1.2f, 1.3f);
            if (s.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Kick(g.Gold ? 9f : 5f);
            if (was != 0) return;
            // La ligne : un tour de plus (le premier passage, au depart, ne compte pas).
            if (!r.Done && LapStarted(r))
            {
                r.Lap++;
                if (r.Lap >= Laps) Finish(r);
                else if (s.IsPlayer)
                {
                    if (r.Lap == Laps - 1) { Banner("DERNIER TOUR !", new Color(1f, 0.45f, 0.3f)); Sfx.Announce("final_round"); }
                    else Banner("TOUR " + (r.Lap + 1) + " / " + Laps, Color.white);
                    Sfx.Discovery();
                }
            }
        }

        // Un tour ne compte que si on a passe toutes les portes : "Next" ne revient a 1 qu'en les
        // passant dans l'ordre, et le premier passage de la ligne ne vaut pas un tour.
        static readonly HashSet<Runner> crossedOnce = new HashSet<Runner>();
        static bool LapStarted(Runner r)
        {
            if (crossedOnce.Contains(r)) return true;
            crossedOnce.Add(r);
            return false;
        }

        static void Finish(Runner r)
        {
            r.Done = true;
            r.DoneAt = Time.time;
            finish.Add(r.Who);
            Seeker s = r.Who;
            Vector3 at = s.Body.position + Vector3.up;
            Fx.Confetti(at, new[] { new Color(1f, 0.8f, 0.3f), new Color(1f, 0.4f, 0.5f), new Color(0.4f, 0.8f, 1f), Color.white }, 160, 14f, Vector3.up, 80f);
            Fx.Column(at, new Color(1f, 0.85f, 0.4f), 120f, 1.4f, 2.5f);
            Fx.Flash(at, new Color(1f, 0.85f, 0.4f), 40f, 6f, 0.6f);
            if (s.IsPlayer) Banner(finish.Count == 1 ? "VICTOIRE !" : "ARRIVÉ " + finish.Count + (finish.Count == 1 ? "er" : "e"), new Color(1f, 0.82f, 0.3f));
            // Le premier gagne la manche (en ligne, c'est l'hote qui le dit).
            if (finish.Count == 1 && !NetGame.IsClient && Game.Menus != null) Game.Menus.EndRound(s.Index);
        }

        /// <summary>L'ASPIRATION : a moins de 25 m derriere un autre coureur, dans son axe, +3 m/s par seconde.</summary>
        static void Slipstream(Seeker s)
        {
            Vector3 p = s.Body.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == s || o.Body == null) continue;
                Vector3 d = o.Body.position - p;
                float dist = d.magnitude;
                if (dist < 3f || dist > 25f) continue;
                Vector3 fwd = Heading(s);
                if (Vector3.Dot(d / dist, fwd) < 0.94f) continue;
                s.PendingBoost += 3f * Time.deltaTime;
                if (s.IsPlayer && Time.frameCount % 6 == 0) Fx.Sparks(p + fwd * 3f + Vector3.up, new Color(0.8f, 0.95f, 1f), 3, 6f);
                return;
            }
        }

        static Vector3 Heading(Seeker s)
        {
            if (s.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) return Game.Hud.orbitCamera.transform.forward;
            return s.Body.forward;
        }

        // ================================================================ le classement

        static float Progress(Runner r)
        {
            if (r.Done) return 100000f - r.DoneAt;
            int passed = crossedOnce.Contains(r) ? r.Lap * Gates.Count + (r.Next == 0 ? Gates.Count : r.Next) : 0;
            float toNext = r.Who != null && r.Who.Body != null && Gates.Count > 0 ? (Gates[r.Next].Centre - r.Who.Body.position).magnitude : 999f;
            return passed * 1000f - toNext;
        }

        static void Rank()
        {
            List<Runner> sorted = new List<Runner>(runners);
            sorted.Sort((a, b) => Progress(b).CompareTo(Progress(a)));
            for (int i = 0; i < sorted.Count; i++) sorted[i].Place = i + 1;
        }

        static Runner Of(Seeker s)
        {
            for (int i = 0; i < runners.Count; i++) if (runners[i].Who == s) return runners[i];
            return null;
        }

        /// <summary>La porte que "s" doit passer maintenant (les bots la visent).</summary>
        public static Gate NextGate(Seeker s)
        {
            Runner r = Of(s);
            if (r == null || Gates.Count == 0) return null;
            return Gates[r.Next];
        }

        /// <summary>Ou un bot regarde en vol : sa prochaine porte (en anticipant la suivante quand il est pres).</summary>
        public static Vector3 BotLook(Seeker s, Vector3 here, float airspeed)
        {
            Runner r = Of(s);
            if (r == null || Gates.Count == 0) return s.Body != null ? s.Body.forward : Vector3.forward;
            Gate g = Gates[r.Next];
            Gate after = Gates[(r.Next + 1) % Gates.Count];
            Vector3 aim = g.Centre;
            float dist = (g.Centre - here).magnitude;
            // Tout pres : il pense deja a la suivante (une trajectoire coulee, pas en zigzag).
            if (dist < 22f) aim = Vector3.Lerp(g.Centre, after.Centre, Mathf.InverseLerp(22f, 6f, dist) * 0.3f);
            // Trop en dessous et trop lent pour remonter : il pique un peu d'abord pour prendre de l'elan.
            Vector3 to = aim - (here + Vector3.up);
            if (to.y > 8f && airspeed < 18f) to.y = -Flat(to).magnitude * 0.15f;
            return to.normalized;
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        // ================================================================ chute et depart

        static void Launch(Seeker s, Vector3 dir, float speed)
        {
            if (s == null || s.Body == null) return;
            if (s.IsPlayer && Game.Player != null) Game.Player.StartFlight(dir, speed);
            else { Rival r = Rival.Of(s); if (r != null) r.StartFlight(dir, speed); }
            s.GraceUntil = Time.time + 2f;
        }

        /// <summary>Tombe dans les nuages : il repart de la derniere porte passee, deja en vol.</summary>
        public static void Respawn(Seeker s)
        {
            Runner r = Of(s);
            if (r == null || Gates.Count == 0) return;
            Gate last = Gates[(r.Next - 1 + Gates.Count) % Gates.Count];
            Vector3 dir = (Gates[r.Next].Centre - last.Centre).normalized;
            Vector3 at = last.Centre + dir * 4f - Vector3.up * 1f;
            Place(s, at, new Vector3(dir.x, 0f, dir.z));
            Launch(s, dir, 20f);
            r.Last = at + Vector3.up;
            Fx.Ring(at + Vector3.up, Color.white, 0.5f, 6f, 0.4f, 0.3f, dir);
            if (s.IsPlayer) Banner("RETOUR EN PISTE", new Color(0.6f, 0.85f, 1f));
        }

        // ================================================================ ce qu'on voit

        /// <summary>Les portes : la prochaine brille en or et bat, la suivante en blanc, les autres discretes.</summary>
        static void Paint()
        {
            Runner me = Of(Game.Me);
            int next = me != null ? me.Next : 0;
            float t = Time.time;
            for (int i = 0; i < Gates.Count; i++)
            {
                Gate g = Gates[i];
                bool isNext = i == next && (me == null || !me.Done);
                bool isAfter = i == (next + 1) % Gates.Count;
                Color baseC = g.Gold ? new Color(1f, 0.78f, 0.25f) : new Color(0.4f, 0.8f, 1f);
                float a = isNext ? 0.75f + 0.25f * Mathf.Sin(t * 8f) : isAfter ? 0.55f : 0.16f;
                Color c = isNext ? Color.Lerp(baseC, Color.white, 0.35f) : baseC;
                for (int k = 0; k < g.Lines.Count; k++)
                {
                    LineRenderer l = g.Lines[k];
                    if (l == null) continue;
                    l.startColor = new Color(c.r, c.g, c.b, a);
                    l.endColor = new Color(c.r, c.g, c.b, a * 0.6f);
                    l.transform.localRotation = Quaternion.Euler(0f, 0f, t * (k == 1 ? -50f : 30f + k * 12f));
                    float s = isNext ? 1f + 0.06f * Mathf.Sin(t * 8f + k) : 1f;
                    l.transform.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        static void Banner(string text, Color c)
        {
            banner = text;
            bannerColour = c;
            bannerAt = Time.unscaledTime;
        }

        /// <summary>Le HUD de la course (Warnings.Draw) : ta place, ton tour, la porte, le repere de la prochaine.</summary>
        public static void Draw()
        {
            if (!On || Game.Menus == null || Game.Menus.Current != Menus.State.Playing) return;
            Runner me = Of(Game.Me);
            if (me == null) return;
            int fs = UiStyle.S(64);
            // TA PLACE, en gros, en haut a gauche.
            string place = me.Place + (me.Place == 1 ? "er" : "e");
            Color pc = me.Place == 1 ? new Color(1f, 0.82f, 0.3f) : me.Place == 2 ? new Color(0.85f, 0.9f, 1f) : me.Place == 3 ? new Color(1f, 0.6f, 0.35f) : Color.white;
            Icons.Text(new Rect(UiStyle.S(30), UiStyle.S(20), UiStyle.S(260), fs * 1.3f), place + " / " + runners.Count, fs, pc, TextAnchor.UpperLeft, true);
            int small = UiStyle.S(30);
            int lap = Mathf.Min(Laps, me.Lap + 1);
            Icons.Text(new Rect(UiStyle.S(32), UiStyle.S(20) + fs * 1.2f, UiStyle.S(400), small * 1.4f), "TOUR " + lap + " / " + Laps + "   ·   PORTE " + (me.Next == 0 ? Gates.Count : me.Next) + " / " + Gates.Count, small, Color.white, TextAnchor.UpperLeft, true);
            // TA VITESSE, en bas au centre.
            if (Game.Player != null && Game.Player.Gliding)
            {
                int kmh = Mathf.RoundToInt(Game.Player.Airspeed * 3.6f);
                Icons.Text(new Rect(Screen.width * 0.5f - UiStyle.S(150), Screen.height - UiStyle.S(120), UiStyle.S(300), UiStyle.S(60)), kmh + " km/h", UiStyle.S(42), kmh > 160 ? new Color(1f, 0.6f, 0.3f) : Color.white, TextAnchor.MiddleCenter, true);
            }
            // LE REPERE de la prochaine porte (colle au bord quand elle est hors de vue).
            Camera cam = Camera.main;
            if (cam != null && Gates.Count > 0 && !me.Done)
            {
                Gate g = Gates[me.Next];
                Vector3 sp = cam.WorldToScreenPoint(g.Centre);
                bool behind = sp.z < 0f;
                Vector2 c = new Vector2(sp.x, Screen.height - sp.y);
                if (behind) c = new Vector2(Screen.width - c.x, Screen.height - c.y);
                float m = UiStyle.S(50);
                bool off = behind || c.x < m || c.x > Screen.width - m || c.y < m || c.y > Screen.height - m;
                if (off)
                {
                    Vector2 mid = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 d = (c - mid);
                    if (d.sqrMagnitude < 1f) d = Vector2.up;
                    float k = Mathf.Min((Screen.width * 0.5f - m) / Mathf.Max(1f, Mathf.Abs(d.x)), (Screen.height * 0.5f - m) / Mathf.Max(1f, Mathf.Abs(d.y)));
                    c = mid + d * k;
                }
                float size = UiStyle.S(off ? 34 : 26);
                Color gc = g.Gold ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 0.85f, 1f);
                Icons.Pill(new Rect(Mathf.Round(c.x - size * 0.5f), Mathf.Round(c.y - size * 0.5f), size, size), gc, 0.9f);
                int dist = Mathf.RoundToInt((g.Centre - (Game.Me != null && Game.Me.Body != null ? Game.Me.Body.position : g.Centre)).magnitude);
                Icons.Text(new Rect(Mathf.Round(c.x - UiStyle.S(60)), Mathf.Round(c.y + size * 0.5f), UiStyle.S(120), UiStyle.S(30)), dist + " m", UiStyle.S(20), Color.white, TextAnchor.UpperCenter, true);
            }
            // LE BANDEAU (plongez, dernier tour, victoire...).
            if (banner != null)
            {
                float age = Time.unscaledTime - bannerAt;
                if (age < 2.2f)
                {
                    float a = Mathf.Clamp01((2.2f - age) / 0.4f);
                    float pop = 1f + 0.35f * Mathf.Clamp01(1f - age / 0.15f);
                    int bs = Mathf.RoundToInt(UiStyle.S(70) * pop);
                    Icons.Text(new Rect(0f, Mathf.Round(Screen.height * 0.24f), Screen.width, bs * 1.4f), banner, bs, new Color(bannerColour.r, bannerColour.g, bannerColour.b, a), TextAnchor.MiddleCenter, true);
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            crossedOnce.Clear();
        }
    }
}
