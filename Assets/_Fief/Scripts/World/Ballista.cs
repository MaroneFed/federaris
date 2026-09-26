using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ARBALESTE GEANTE (27/09 -- Martin : "a la place de monter la tour, tu peux te
    /// mettre dans une sorte de canon, comme on est au Moyen Age une enorme arbalete,
    /// tu te mets dessus et BAM tu tires a fond").
    ///
    /// On monte dessus (E). On VISE a la souris : la trajectoire se dessine en
    /// lumiere, et un anneau marque ou l'on va retomber. Clic gauche : on est TIRE.
    /// E ou Espace : on redescend. Elle tire toujours aussi fort (50 m/s) : on regle
    /// la distance avec l'angle -- de quoi atteindre le milieu de la tour, ou un ilot.
    ///
    /// Les bots s'en servent aussi, par les memes methodes (Mount, Fire) : ils
    /// calculent l'angle qu'il faut pour tomber ou ils veulent (Solve).
    ///
    /// Concept Unity : pendant qu'on est monte, le CharacterController du joueur est
    /// coupe (PlayerController.BeginScripted) : c'est l'arbaleste qui le place.
    /// </summary>
    public class Ballista : MonoBehaviour, IInteractable
    {
        public static readonly List<Ballista> All = new List<Ballista>();
        /// <summary>L'arbaleste ou tu es monte (null sinon) : le HUD et tes capacites s'en servent.</summary>
        public static Ballista PlayerOn { get; private set; }

        public const float Power = 50f;
        const float Gravity = 22f;
        const float Reload = 1.5f;

        Transform yawPivot, pitchPivot, seat, bow, marker;
        LineRenderer arc;
        Seeker rider;
        Rival riderBot;
        float mountedAt;
        float readyAt;
        Vector3 botAim;
        float botFireAt;
        float yaw;
        float pitch = 35f;
        float recoil;

        static readonly Color Wood = new Color(0.38f, 0.27f, 0.17f);
        static readonly Color WoodDark = new Color(0.25f, 0.18f, 0.12f);
        static readonly Color Iron = new Color(0.18f, 0.18f, 0.2f);
        static readonly Color Rune = new Color(1f, 0.7f, 0.3f);

        public bool Free { get { return rider == null && Time.time >= readyAt; } }

        // ================================================================== construction

        public static Ballista Build(Transform parent, Vector3 at, float facing)
        {
            GameObject go = new GameObject("ARBALESTE");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Ballista b = go.AddComponent<Ballista>();
            b.yaw = facing;
            Transform t = go.transform;

            // Le socle : une plate-forme de pierre (on marche dessus), une couronne de fer.
            Proto.Cylinder(t, new Vector3(0f, 0.3f, 0f), new Vector3(3.6f, 0.3f, 3.6f), new Color(0.35f, 0.33f, 0.31f), "Socle");
            Proto.BeginVisualOnly();
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, 0.62f, 0f), new Vector3(3.2f, 0.03f, 3.2f), Color.white, "Couronne de runes");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 1.2f);
            Proto.EndVisualOnly();

            b.yawPivot = new GameObject("Tourelle").transform;
            b.yawPivot.SetParent(t, false);
            b.yawPivot.localPosition = new Vector3(0f, 0.65f, 0f);
            Proto.BeginVisualOnly();
            Proto.Cube(b.yawPivot, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 2.2f), Wood, "Affût");
            for (int k = -1; k <= 1; k += 2)
                Proto.Cube(b.yawPivot, new Vector3(k * 0.7f, 1.2f, 0f), new Vector3(0.2f, 1.4f, 0.5f), WoodDark, "Flasque");
            Proto.EndVisualOnly();

            b.pitchPivot = new GameObject("Bras").transform;
            b.pitchPivot.SetParent(b.yawPivot, false);
            b.pitchPivot.localPosition = new Vector3(0f, 1.7f, 0f);
            Proto.BeginVisualOnly();
            // Le fut : une longue poutre ; au bout, l'arc de fer (deux bras en fleche) et sa corde.
            Proto.Cube(b.pitchPivot, new Vector3(0f, 0f, 1.4f), new Vector3(0.45f, 0.35f, 5f), Wood, "Fût");
            b.bow = new GameObject("Arc").transform;
            b.bow.SetParent(b.pitchPivot, false);
            b.bow.localPosition = new Vector3(0f, 0.1f, 3.6f);
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject arm = Proto.Cube(b.bow, new Vector3(k * 1.6f, 0f, -0.35f), new Vector3(3.2f, 0.22f, 0.28f), Iron, "Bras de l'arc");
                arm.transform.localRotation = Quaternion.Euler(0f, k * -14f, 0f);
                GameObject tip = Proto.Cube(b.bow, new Vector3(k * 3.1f, 0f, -0.75f), new Vector3(0.18f, 0.3f, 0.18f), Color.white, "Embout");
                tip.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2f);
            }
            GameObject cord = Proto.Cube(b.bow, new Vector3(0f, 0f, -0.9f), new Vector3(6.1f, 0.05f, 0.05f), new Color(0.9f, 0.85f, 0.7f), "Corde");
            cord.name = "Corde";
            // Le berceau : c'est la qu'on s'installe.
            Proto.Cube(b.pitchPivot, new Vector3(0f, 0.3f, -0.4f), new Vector3(1.1f, 0.12f, 1.4f), WoodDark, "Berceau");
            GameObject rune = Proto.Cube(b.pitchPivot, new Vector3(0f, 0.38f, -0.4f), new Vector3(0.6f, 0.02f, 0.6f), Color.white, "Rune du berceau");
            rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2.4f);
            Proto.EndVisualOnly();
            b.seat = new GameObject("Siège").transform;
            b.seat.SetParent(b.pitchPivot, false);
            b.seat.localPosition = new Vector3(0f, 0.4f, -0.4f);

            // La trajectoire (une ligne de lumiere) et l'anneau d'arrivee.
            GameObject line = new GameObject("Trajectoire");
            line.transform.SetParent(t, false);
            b.arc = line.AddComponent<LineRenderer>();
            b.arc.sharedMaterial = Ambiance.Additive;
            b.arc.useWorldSpace = true;
            b.arc.widthMultiplier = 0.18f;
            b.arc.startColor = new Color(1f, 0.85f, 0.5f, 0.9f);
            b.arc.endColor = new Color(1f, 0.6f, 0.3f, 0.5f);
            b.arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.arc.enabled = false;
            Proto.BeginVisualOnly();
            GameObject m = Proto.Cylinder(t, Vector3.zero, new Vector3(2.4f, 0.02f, 2.4f), Color.white, "Arrivée");
            m.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.75f, 0.35f), 2.5f);
            Proto.EndVisualOnly();
            b.marker = m.transform;
            b.marker.gameObject.SetActive(false);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.4f, 0f);
            trigger.size = new Vector3(3.6f, 2.8f, 3.6f);

            b.Pose();
            All.Add(b);
            return b;
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (PlayerOn == this) PlayerOn = null;
        }

        /// <summary>Les huit arbalestes de l'ile : quatre dans la cour, quatre dehors, entre les portes et les tours d'angle.</summary>
        public static void PlaceAll(Transform parent)
        {
            GameObject root = new GameObject("ARBALESTES");
            root.transform.SetParent(parent, false);
            for (int k = 0; k < 4; k++)
            {
                // Dans la cour, aux quatre diagonales : elles visent la tour.
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 inside = Ground.Place(Mathf.Cos(a) * 34f, Mathf.Sin(a) * 34f, 0f);
                Build(root.transform, inside, YawTowards(inside, Vector3.zero));
                // Dehors, sur l'herbe : elles visent les ilots.
                float b = (k * 90f + 64f) * Mathf.Deg2Rad;
                float r = Mathf.Min(Ground.EdgeAt(b) - 11f, 80f);
                Vector3 outside = Ground.Place(Mathf.Cos(b) * r, Mathf.Sin(b) * r, 0f);
                Build(root.transform, outside, YawTowards(Vector3.zero, outside));
            }
        }

        static float YawTowards(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // ================================================================== monter, tirer

        /// <summary>Monter sur l'arbaleste. Vrai si elle etait libre.</summary>
        public bool Mount(Seeker s)
        {
            if (!Free || s == null || s.Body == null || s.Stunned) return false;
            rider = s;
            mountedAt = Time.time;
            if (s.IsPlayer)
            {
                if (Game.Player == null) { rider = null; return false; }
                Game.Player.BeginScripted();
                PlayerOn = this;
                if (Game.Hud != null) Game.Hud.Tip("arbaleste", "Vise à la souris : la ligne montre ta course. Clic gauche pour tirer, E pour descendre.");
            }
            else
            {
                riderBot = Rival.Of(s);
                if (riderBot == null) { rider = null; return false; }
            }
            Sfx.Build();
            return true;
        }

        /// <summary>Un bot monte, vise "velocity" (calculee par Solve) et tire au bout d'un instant.</summary>
        public bool MountAndAim(Seeker s, Vector3 velocity)
        {
            if (!Mount(s)) return false;
            botAim = velocity;
            botFireAt = Time.time + 0.9f;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            pitch = Mathf.Atan2(velocity.y, flat.magnitude) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>Descendre sans tirer.</summary>
        public void Dismount()
        {
            if (rider == null) return;
            Vector3 off = transform.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 2.4f + Vector3.up * 0.3f;
            if (rider.IsPlayer && Game.Player != null) Game.Player.EndScripted(off);
            else if (riderBot != null) riderBot.Dismounted(off);
            Release();
        }

        void Release()
        {
            if (rider != null && rider.IsPlayer) PlayerOn = null;
            rider = null;
            riderBot = null;
            arc.enabled = false;
            marker.gameObject.SetActive(false);
        }

        /// <summary>TIRER : le cavalier part sur la trajectoire, dans un fracas de corde.</summary>
        void Fire(Vector3 velocity)
        {
            if (rider == null) return;
            Vector3 from = seat.position + Vector3.up * 0.2f;
            Seeker who = rider;
            if (who.IsPlayer && Game.Player != null)
            {
                Game.Player.EndScripted(from);
                Game.Player.Launch(velocity);
            }
            else if (riderBot != null)
            {
                riderBot.Dismounted(from);
                riderBot.Launch(velocity);
            }
            Release();
            readyAt = Time.time + Reload;
            recoil = 1f;
            Color c = who.Colour;
            Fx.Burst(from, new Color(1f, 0.8f, 0.45f), 60, 16f, 0.2f, 0.7f, 0.1f, velocity, 18f);
            Fx.Ring(from, Color.white, 0.4f, 4f, 0.35f, 0.25f, velocity);
            Fx.Flash(from, new Color(1f, 0.8f, 0.5f), 18f, 6f, 0.3f);
            Fx.Trail(who.Body, c, 1.6f, 0.9f);
            Sfx.Crash();
            Sfx.Whoosh();
            if (who.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.35f);
        }

        // ================================================================== a chaque image

        void Update()
        {
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 3f);
            if (rider == null || rider.Body == null) { if (rider != null) Release(); Pose(); return; }
            if (rider.Stunned) { Dismount(); return; }

            if (rider.IsPlayer)
            {
                OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
                if (cam != null)
                {
                    yaw = cam.yaw;
                    // La camera regarde vers le haut quand son "pitch" est negatif.
                    pitch = Mathf.Clamp(-cam.pitch + 12f, 6f, 72f);
                }
                Pose();
                if (Game.Player != null) Game.Player.ScriptedMove(seat.position);
                Vector3 v = Aim();
                DrawArc(seat.position + Vector3.up * 0.2f, v);
                bool locked = Game.Player != null && Game.Player.InputLocked;
                if (!locked && Time.time - mountedAt > 0.3f)
                {
                    if (FiefInput.PushPressed) { Fire(v); return; }
                    if (FiefInput.InteractPressed || FiefInput.JumpPressed) { Dismount(); return; }
                }
            }
            else
            {
                Pose();
                if (riderBot != null) riderBot.transform.position = seat.position;
                if (Time.time >= botFireAt) Fire(botAim);
            }
        }

        /// <summary>La vitesse de tir, dans la direction ou l'arbaleste pointe.</summary>
        Vector3 Aim()
        {
            return Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward * Power;
        }

        void Pose()
        {
            yawPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(-pitch + recoil * 6f, 0f, 0f);
            bow.localScale = new Vector3(1f - recoil * 0.1f, 1f, 1f);
        }

        /// <summary>La trajectoire, dessinee point par point jusqu'a ce qu'elle touche quelque chose.</summary>
        void DrawArc(Vector3 from, Vector3 v)
        {
            const float step = 0.05f;
            const int max = 160;
            Vector3[] pts = new Vector3[max];
            int n = 0;
            Vector3 p = from;
            Vector3 vel = v;
            bool hit = false;
            RaycastHit rh = new RaycastHit();
            for (int i = 0; i < max; i++)
            {
                pts[n++] = p;
                Vector3 next = p + vel * step;
                vel += Vector3.down * Gravity * step;
                if (Physics.Linecast(p, next, out rh, ~0, QueryTriggerInteraction.Ignore)) { pts[n++] = rh.point; hit = true; break; }
                if (next.y < Ground.FallLine) break;
                p = next;
                if (n >= max - 1) break;
            }
            arc.enabled = true;
            arc.positionCount = n;
            for (int i = 0; i < n; i++) arc.SetPosition(i, pts[i]);
            marker.gameObject.SetActive(hit);
            if (hit)
            {
                marker.position = rh.point + rh.normal * 0.05f;
                marker.rotation = Quaternion.FromToRotation(Vector3.up, rh.normal);
                marker.localScale = new Vector3(2.4f, 0.02f, 2.4f) * (1f + 0.1f * Mathf.Sin(Time.time * 6f));
            }
        }

        // ================================================================== pour les bots

        /// <summary>
        /// La vitesse de tir qui fait passer par "to" en partant de "from" (courbe haute :
        /// on retombe DESSUS). Faux si c'est trop loin pour cette arbaleste.
        /// </summary>
        public static bool Solve(Vector3 from, Vector3 to, bool high, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            Vector3 d = to - from;
            Vector3 flat = new Vector3(d.x, 0f, d.z);
            float x = flat.magnitude, y = d.y, v2 = Power * Power;
            if (x < 1f) return false;
            float disc = v2 * v2 - Gravity * (Gravity * x * x + 2f * y * v2);
            if (disc < 0f) return false;
            float root = Mathf.Sqrt(disc);
            float angle = Mathf.Atan((v2 + (high ? root : -root)) / (Gravity * x));
            velocity = flat.normalized * Mathf.Cos(angle) * Power + Vector3.up * Mathf.Sin(angle) * Power;
            return true;
        }

        /// <summary>La trajectoire depuis cette arbaleste retombe-t-elle bien pres de "target" (rien ne la coupe avant) ?</summary>
        public bool Lands(Vector3 velocity, Vector3 target, float tolerance)
        {
            Vector3 p = seat.position + Vector3.up * 0.2f;
            Vector3 vel = velocity;
            for (int i = 0; i < 200; i++)
            {
                Vector3 next = p + vel * 0.05f;
                vel += Vector3.down * Gravity * 0.05f;
                RaycastHit rh;
                if (Physics.Linecast(p, next, out rh, ~0, QueryTriggerInteraction.Ignore)) return (rh.point - target).magnitude < tolerance;
                if (next.y < Ground.FallLine) return false;
                p = next;
            }
            return false;
        }

        public Vector3 Seat { get { return seat.position; } }

        /// <summary>L'arbaleste libre la plus proche de "p", a moins de "range" metres (null sinon).</summary>
        public static Ballista NearestFree(Vector3 p, float range)
        {
            Ballista best = null;
            float bestD = range;
            for (int i = 0; i < All.Count; i++)
            {
                Ballista b = All[i];
                if (b == null || !b.Free) continue;
                float d = (b.transform.position - p).magnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return Free && Game.Me != null && !Game.Me.Stunned && Game.Season != null && Game.Season.Running; } }
        public string Prompt { get { return "Monter sur l'arbaleste"; } }
        public float HoldDuration { get { return 0f; } }
        public void Interact() { if (!Mount(Game.Me)) Sfx.Deny(); }
    }
}
