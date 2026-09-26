using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ARBALESTE GEANTE (27/09 -- Martin : "une enorme arbalete, tu te mets dessus et
    /// BAM tu tires a fond" ; refaite le 28/09 : "des meilleures arbaletes").
    ///
    /// On monte dessus (F) : on s'assoit sur le CARREAU, un trait de cinq metres. On
    /// VISE a la souris : la trajectoire se dessine en perles de lumiere et un anneau
    /// marque l'arrivee -- VERT sur la terre ferme, BLEU sur un Monument ; si la ligne
    /// file dans le vide, pas d'anneau : tu planeras.
    ///
    /// On TEND en maintenant le clic gauche : les bras de l'arc plient, la corde
    /// recule, le treuil tourne, la rune du fut se remplit -- et la courbe s'allonge.
    /// On RELACHE : BAM. E ou Espace : on redescend.
    ///
    /// On part avec des AILES D'OR (voir Wings) : au-dessus du vide, elles s'ouvrent
    /// seules en redescendant. Il y en a huit sur l'ile et une sur chaque ilot.
    ///
    /// Les bots s'en servent aussi, par les memes methodes (Mount, MountAndAim) : ils
    /// calculent l'angle qu'il faut pour tomber ou ils veulent (Solve), a pleine tension.
    ///
    /// Concept Unity : pendant qu'on est monte, le CharacterController du joueur est
    /// coupe (PlayerController.BeginScripted) : c'est l'arbaleste qui le place. Et les
    /// bras de l'arc sont des CHAINES de Transform : chaque segment est l'enfant du
    /// precedent, on tourne chaque articulation d'un petit angle et tout l'arc plie.
    /// </summary>
    public class Ballista : MonoBehaviour, IInteractable
    {
        public static readonly List<Ballista> All = new List<Ballista>();
        /// <summary>L'arbaleste ou tu es monte (null sinon) : le HUD et tes capacites s'en servent.</summary>
        public static Ballista PlayerOn { get; private set; }

        /// <summary>La vitesse de tir a pleine tension (m/s).</summary>
        public const float Power = 56f;
        /// <summary>La tension la plus faible (un clic bref) : la moitie de la puissance.</summary>
        const float MinTension = 0.5f;
        const float TensionTime = 0.8f;
        const float Gravity = 22f;
        const float Reload = 1.5f;

        /// <summary>Ou la trajectoire retombe (le HUD le dit, l'anneau en prend la couleur).</summary>
        public enum LandingKind { None, Ground, Monument, Void }
        public LandingKind Landing { get; private set; }
        /// <summary>La tension montree (0 a 1), pour le HUD.</summary>
        public float Tension { get { return charging ? charge : 1f; } }
        public bool Charging { get { return charging; } }

        Transform yawPivot, pitchPivot, seat, bolt, chargeBar, marker, markerBeam;
        Transform tipL, tipR;
        readonly List<Transform> jointsL = new List<Transform>();
        readonly List<Transform> jointsR = new List<Transform>();
        readonly List<Transform> wheels = new List<Transform>();
        LineRenderer arc, cord, markerRing;
        Renderer markerDisc;
        Seeker rider;
        Rival riderBot;
        float mountedAt;
        float readyAt;
        Vector3 botAim;
        float botFireAt;
        float yaw;
        float pitch = 35f;
        float recoil;
        float charge;
        bool charging;
        bool loaded = true;
        float wheelTurn;
        readonly Vector3[] points = new Vector3[200];

        static readonly Color Wood = new Color(0.42f, 0.29f, 0.17f);
        static readonly Color WoodDark = new Color(0.26f, 0.18f, 0.11f);
        static readonly Color Iron = new Color(0.2f, 0.2f, 0.23f);
        static readonly Color Bronze = new Color(0.72f, 0.5f, 0.25f);
        static readonly Color StoneC = new Color(0.4f, 0.38f, 0.35f);
        static readonly Color Rune = new Color(1f, 0.66f, 0.25f);
        static readonly Color GroundOk = new Color(0.5f, 1f, 0.55f);

        const int Segments = 5;
        const float SegmentLength = 0.72f;
        const float BowZ = 4.4f;

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

            // LE SOCLE : une plate-forme de pierre octogonale (on marche dessus), un
            // cercle de runes, huit petits merlons.
            Proto.Cylinder(t, new Vector3(0f, 0.35f, 0f), new Vector3(4.6f, 0.35f, 4.6f), StoneC, "Socle");
            Proto.BeginVisualOnly();
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, 0.71f, 0f), new Vector3(4.2f, 0.02f, 4.2f), Color.white, "Cercle de runes");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 1.1f);
            Proto.Cylinder(t, new Vector3(0f, 0.72f, 0f), new Vector3(3.9f, 0.02f, 3.9f), StoneC, "Dalle");
            for (int k = 0; k < 8; k++)
            {
                float a = k / 8f * Mathf.PI * 2f;
                GameObject m = Proto.Cube(t, new Vector3(Mathf.Cos(a) * 2.15f, 0.9f, Mathf.Sin(a) * 2.15f), new Vector3(0.5f, 0.45f, 0.5f), StoneC, "Merlon");
                m.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
            Proto.EndVisualOnly();

            // LA TOURELLE : un plateau tournant cercle de bronze, deux flasques en A, un
            // treuil a deux roues a l'arriere, un fanion.
            b.yawPivot = new GameObject("Tourelle").transform;
            b.yawPivot.SetParent(t, false);
            b.yawPivot.localPosition = new Vector3(0f, 0.74f, 0f);
            Transform y = b.yawPivot;
            Proto.BeginVisualOnly();
            Proto.Cylinder(y, new Vector3(0f, 0.1f, 0f), new Vector3(3.3f, 0.1f, 3.3f), WoodDark, "Plateau");
            Proto.Cylinder(y, new Vector3(0f, 0.08f, 0f), new Vector3(3.45f, 0.06f, 3.45f), Bronze, "Cerclage");
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject front = Proto.Cube(y, new Vector3(k * 0.8f, 1.15f, 0.45f), new Vector3(0.24f, 2.4f, 0.32f), Wood, "Flasque");
                front.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                GameObject back = Proto.Cube(y, new Vector3(k * 0.8f, 1.15f, -0.45f), new Vector3(0.24f, 2.4f, 0.32f), Wood, "Flasque");
                back.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                Proto.Cube(y, new Vector3(k * 0.8f, 2.3f, 0f), new Vector3(0.34f, 0.3f, 0.6f), Iron, "Chape");
                // Les roues du treuil.
                Transform wheel = new GameObject("Roue").transform;
                wheel.SetParent(y, false);
                wheel.localPosition = new Vector3(k * 0.98f, 0.95f, -1.25f);
                GameObject disc = Proto.Cylinder(wheel, Vector3.zero, new Vector3(1.3f, 0.05f, 1.3f), WoodDark, "Jante");
                disc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                for (int s = 0; s < 4; s++)
                {
                    GameObject spoke = Proto.Cube(wheel, Vector3.zero, new Vector3(0.08f, 1.2f, 0.1f), Wood, "Rayon");
                    spoke.transform.localRotation = Quaternion.Euler(s * 45f, 0f, 0f);
                }
                b.wheels.Add(wheel);
            }
            GameObject drum = Proto.Cylinder(y, new Vector3(0f, 0.95f, -1.25f), new Vector3(0.45f, 0.9f, 0.45f), Wood, "Treuil");
            drum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject axle = Proto.Cylinder(y, new Vector3(0f, 2.3f, 0f), new Vector3(0.16f, 0.95f, 0.16f), Iron, "Axe");
            axle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Proto.Banner(y, new Vector3(-1.5f, 0f, -1.3f), new Color(0.85f, 0.45f, 0.18f), 4.8f, "Fanion");
            Proto.EndVisualOnly();

            // LE BRAS : le fut, ses cerclages de fer, la rune de tension, l'arc et le carreau.
            b.pitchPivot = new GameObject("Bras").transform;
            b.pitchPivot.SetParent(y, false);
            b.pitchPivot.localPosition = new Vector3(0f, 2.3f, 0f);
            Transform p = b.pitchPivot;
            Proto.BeginVisualOnly();
            Proto.Cube(p, new Vector3(0f, 0f, 1.1f), new Vector3(0.6f, 0.42f, 7f), Wood, "Fût");
            for (int k = 0; k < 4; k++)
                Proto.Cube(p, new Vector3(0f, 0f, -1.7f + k * 1.9f), new Vector3(0.68f, 0.5f, 0.16f), Iron, "Cerclage");
            b.chargeBar = new GameObject("Tension").transform;
            b.chargeBar.SetParent(p, false);
            b.chargeBar.localPosition = new Vector3(0f, 0.22f, -2.3f);
            GameObject bar = Proto.Cube(b.chargeBar, new Vector3(0f, 0f, 0.5f), new Vector3(0.14f, 0.02f, 1f), Color.white, "Rune de tension");
            bar.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2.6f);
            // Le corps de l'arc, et ses deux bras en chaine.
            Proto.Cube(p, new Vector3(0f, 0.05f, BowZ), new Vector3(1f, 0.6f, 0.55f), Iron, "Noix");
            b.tipL = BuildArm(p, -1, b.jointsL);
            b.tipR = BuildArm(p, 1, b.jointsR);
            // Le carreau : un trait de cinq metres, sa pointe de fer, ses ailettes.
            b.bolt = new GameObject("Carreau").transform;
            b.bolt.SetParent(p, false);
            b.bolt.localPosition = new Vector3(0f, 0.34f, 0f);
            BoltModel(b.bolt);
            Proto.EndVisualOnly();

            b.seat = new GameObject("Siège").transform;
            b.seat.SetParent(p, false);
            b.seat.localPosition = new Vector3(0f, 0.5f, -0.6f);

            // La corde.
            GameObject cordGo = new GameObject("Corde");
            cordGo.transform.SetParent(t, false);
            b.cord = cordGo.AddComponent<LineRenderer>();
            b.cord.useWorldSpace = true;
            b.cord.positionCount = 3;
            b.cord.widthMultiplier = 0.08f;
            b.cord.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.9f, 0.7f), 1.2f);
            b.cord.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // La trajectoire : des perles de lumiere (la texture ronde repetee le long de la ligne).
            GameObject line = new GameObject("Trajectoire");
            line.transform.SetParent(t, false);
            b.arc = line.AddComponent<LineRenderer>();
            b.arc.sharedMaterial = Ambiance.Additive;
            b.arc.useWorldSpace = true;
            b.arc.textureMode = LineTextureMode.Tile;
            b.arc.widthMultiplier = 0.5f;
            b.arc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.arc.enabled = false;

            // L'arrivee : un anneau, un disque, une colonne de lumiere.
            GameObject mk = new GameObject("Arrivée");
            mk.transform.SetParent(t, false);
            b.marker = mk.transform;
            b.markerRing = mk.AddComponent<LineRenderer>();
            b.markerRing.sharedMaterial = Ambiance.Additive;
            b.markerRing.useWorldSpace = false;
            b.markerRing.loop = true;
            b.markerRing.positionCount = 40;
            b.markerRing.widthMultiplier = 0.25f;
            b.markerRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f * Mathf.PI * 2f;
                b.markerRing.SetPosition(i, new Vector3(Mathf.Cos(a) * 2.2f, 0.1f, Mathf.Sin(a) * 2.2f));
            }
            Proto.BeginVisualOnly();
            GameObject disc2 = Proto.Cylinder(mk.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1.2f, 0.02f, 1.2f), Color.white, "Disque");
            b.markerDisc = disc2.GetComponent<Renderer>();
            GameObject col = Proto.Cylinder(mk.transform, new Vector3(0f, 4f, 0f), new Vector3(0.12f, 4f, 0.12f), Color.white, "Colonne");
            b.markerBeam = col.transform;
            Proto.EndVisualOnly();
            b.marker.gameObject.SetActive(false);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.6f, 0f);
            trigger.size = new Vector3(4.6f, 3.2f, 4.6f);

            b.Pose();
            All.Add(b);
            return b;
        }

        /// <summary>Un bras de l'arc : cinq segments en chaine, de plus en plus fins, et un embout qui luit.</summary>
        static Transform BuildArm(Transform stock, int side, List<Transform> joints)
        {
            Transform joint = new GameObject("Bras de l'arc").transform;
            joint.SetParent(stock, false);
            joint.localPosition = new Vector3(side * 0.5f, 0.05f, BowZ);
            for (int i = 0; i < Segments; i++)
            {
                joints.Add(joint);
                float thick = Mathf.Lerp(0.34f, 0.16f, i / (float)(Segments - 1));
                Proto.Cube(joint, new Vector3(side * SegmentLength * 0.5f, 0f, 0f), new Vector3(SegmentLength + 0.04f, thick, thick * 1.2f), i % 2 == 0 ? Iron : WoodDark, "Segment");
                Transform next = new GameObject("Articulation").transform;
                next.SetParent(joint, false);
                next.localPosition = new Vector3(side * SegmentLength, 0f, 0f);
                joint = next;
            }
            GameObject tip = Proto.Cube(joint, Vector3.zero, new Vector3(0.22f, 0.4f, 0.22f), Color.white, "Embout");
            tip.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2.2f);
            return joint;
        }

        /// <summary>Le carreau geant (aussi celui qui vole avec les bots tires, voir BoltRide).</summary>
        public static void BoltModel(Transform t)
        {
            Proto.Cube(t, new Vector3(0f, 0f, 1.5f), new Vector3(0.18f, 0.18f, 5.2f), new Color(0.6f, 0.46f, 0.3f), "Hampe");
            GameObject head = Proto.Cube(t, new Vector3(0f, 0f, 4.3f), new Vector3(0.42f, 0.42f, 0.8f), Iron, "Pointe");
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject edge = Proto.Cube(t, new Vector3(0f, 0f, 4.75f), new Vector3(0.12f, 0.12f, 0.3f), Color.white, "Fil");
            edge.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2.5f);
            for (int k = 0; k < 3; k++)
            {
                GameObject fin = Proto.Cube(t, new Vector3(0f, 0f, -0.8f), new Vector3(0.05f, 0.8f, 0.8f), new Color(0.85f, 0.3f, 0.2f), "Ailette");
                fin.transform.localRotation = Quaternion.Euler(0f, 0f, k * 60f);
            }
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (PlayerOn == this) PlayerOn = null;
        }

        /// <summary>
        /// Les arbalestes : huit sur l'ile (quatre dans la cour, quatre dehors, entre les
        /// portes et les tours d'angle) et une sur chaque ilot, tournee vers la tour --
        /// pour revenir.
        /// </summary>
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
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Ground.Islet it = Ground.GetIslet(i);
                Vector3 toTower = new Vector3(-it.Top.x, 0f, -it.Top.z).normalized;
                // Pres du bord, cote tour, un peu de biais (le Monument ou le sanctuaire est au centre).
                Vector3 side = Quaternion.Euler(0f, 40f, 0f) * toTower;
                Vector3 at = it.Top + side * (it.Radius - 3.2f);
                at = Ground.Place(at.x, at.z, 0f);
                Build(root.transform, at, YawTowards(at, Vector3.zero));
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
            charging = false;
            charge = 0f;
            if (s.IsPlayer)
            {
                if (Game.Player == null) { rider = null; return false; }
                Game.Player.BeginScripted();
                PlayerOn = this;
                if (Game.Hud != null) Game.Hud.Tip("arbaleste", "Vise à la souris. MAINTIENS le clic gauche pour tendre (la courbe s'allonge), RELÂCHE pour tirer (F : descendre). Anneau vert : terre ferme ; bleu : un Monument !");
            }
            else
            {
                riderBot = Rival.Of(s);
                if (riderBot == null) { rider = null; return false; }
            }
            Sfx.Build();
            Fx.Ring(transform.position + Vector3.up * 0.8f, Rune, 0.5f, 3f, 0.3f, 0.15f, Vector3.up);
            return true;
        }

        /// <summary>Un bot monte, vise "velocity" (calculee par Solve) et tire au bout d'un instant.</summary>
        public bool MountAndAim(Seeker s, Vector3 velocity)
        {
            if (!Mount(s)) return false;
            botAim = velocity;
            botFireAt = Time.time + 1f;
            charging = true;
            charge = 0f;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            pitch = Mathf.Atan2(velocity.y, flat.magnitude) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>Descendre sans tirer.</summary>
        public void Dismount()
        {
            if (rider == null) return;
            Vector3 off = transform.position + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 2.8f + Vector3.up * 0.9f;
            if (rider.IsPlayer && Game.Player != null) Game.Player.EndScripted(off);
            else if (riderBot != null) riderBot.Dismounted(off);
            Release();
        }

        void Release()
        {
            if (rider != null && rider.IsPlayer) PlayerOn = null;
            rider = null;
            riderBot = null;
            charging = false;
            arc.enabled = false;
            marker.gameObject.SetActive(false);
            Landing = LandingKind.None;
        }

        /// <summary>TIRER : le cavalier part sur la trajectoire, avec des ailes d'or, dans un fracas de corde.</summary>
        void Fire(Vector3 velocity)
        {
            if (rider == null) return;
            Vector3 from = Launcher;
            Seeker who = rider;
            Wings.Grant(who, false);
            if (who.IsPlayer && Game.Player != null)
            {
                Game.Player.EndScripted(from);
                Game.Player.Launch(velocity);
            }
            else if (riderBot != null)
            {
                riderBot.Dismounted(from);
                riderBot.Launch(velocity);
                BoltRide.Follow(who);
            }
            Release();
            readyAt = Time.time + Reload;
            recoil = 1f;
            loaded = false;
            charge = 0f;
            Color c = who.Colour;
            Vector3 nock = pitchPivot.TransformPoint(new Vector3(0f, 0.3f, BowZ));
            Fx.Burst(nock, new Color(1f, 0.8f, 0.45f), 90, 20f, 0.22f, 0.7f, 0.1f, velocity, 16f);
            Fx.Burst(nock, Color.white, 30, 12f, 0.12f, 0.4f, 0f, velocity, 30f);
            Fx.Ring(nock, Color.white, 0.4f, 5f, 0.35f, 0.3f, velocity);
            Fx.Ring(nock + velocity.normalized * 2.5f, Rune, 0.3f, 3.5f, 0.3f, 0.2f, velocity);
            Fx.Shock(nock, Rune, 2.2f, 0.25f);
            Fx.Flash(nock, new Color(1f, 0.8f, 0.5f), 22f, 7f, 0.35f);
            Fx.GroundRing(transform.position + Vector3.up * 0.75f, Rune, 5f, 0.4f);
            Fx.Burst(transform.position + Vector3.up * 0.8f, new Color(0.7f, 0.62f, 0.52f), 40, 7f, 0.4f, 0.9f, 0.3f, Vector3.up, 80f);
            Fx.Trail(who.Body, c, 2.2f, 1.1f);
            Sfx.Crash();
            Sfx.Thud();
            Sfx.Whoosh();
            if (Game.PlayerTransform != null && Game.Hud != null && Game.Hud.orbitCamera != null)
            {
                float d = (Game.PlayerTransform.position - transform.position).magnitude;
                if (who.IsPlayer) Game.Hud.orbitCamera.Shake(0.4f);
                else if (d < 25f) Game.Hud.orbitCamera.Shake(0.2f * (1f - d / 25f));
            }
        }

        // ================================================================== a chaque image

        void Update()
        {
            float dt = Time.deltaTime;
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 2.5f);
            // Rechargee : le carreau reapparait dans un eclat.
            if (!loaded && Time.time >= readyAt)
            {
                loaded = true;
                Fx.Sparks(bolt.position + bolt.forward * 1.5f, Rune, 20, 3f);
            }
            if (rider == null || rider.Body == null) { if (rider != null) Release(); Pose(); return; }
            if (rider.Stunned) { Dismount(); return; }

            if (rider.IsPlayer)
            {
                OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
                if (cam != null)
                {
                    yaw = cam.yaw;
                    // La camera regarde vers le haut quand son "pitch" est negatif.
                    pitch = Mathf.Clamp(-cam.pitch + 12f, 4f, 72f);
                }
                bool locked = Game.Player != null && Game.Player.InputLocked;
                bool ready = !locked && Time.time - mountedAt > 0.3f;
                if (ready && FiefInput.ShootPressed && !charging) { charging = true; charge = 0f; Sfx.Build(); }
                if (charging)
                {
                    float before = charge;
                    charge = Mathf.MoveTowards(charge, 1f, dt / TensionTime);
                    if (before < 1f && charge >= 1f) { Sfx.Pop(); Fx.Sparks(pitchPivot.TransformPoint(new Vector3(0f, 0.3f, 1f)), Rune, 16, 2f); }
                }
                Pose();
                if (Game.Player != null) Game.Player.ScriptedMove(seat.position);
                Vector3 v = Aim(charging ? charge : 1f);
                DrawArc(Launcher, v, charging ? 1f : 0.45f);
                if (ready && charging && !FiefInput.ShootHeld) { Fire(v); return; }
                if (ready && (FiefInput.InteractPressed || FiefInput.JumpPressed)) { Dismount(); return; }
            }
            else
            {
                charge = Mathf.MoveTowards(charge, 1f, dt / 0.9f);
                Pose();
                if (riderBot != null) riderBot.transform.position = seat.position;
                if (Time.time >= botFireAt) Fire(botAim);
            }
        }

        /// <summary>La vitesse de tir, dans la direction ou l'arbaleste pointe, a la tension "tension".</summary>
        Vector3 Aim(float tension)
        {
            return Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward * Power * Mathf.Lerp(MinTension, 1f, tension);
        }

        /// <summary>
        /// La pose : tourelle, inclinaison (et le recul), l'arc qui plie avec la tension,
        /// la corde qui recule, le carreau qui suit la corde, les roues qui tournent.
        /// </summary>
        void Pose()
        {
            float c = rider != null ? charge : 0f;
            yawPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(-pitch + recoil * 8f, 0f, 0f);
            // L'arc plie : chaque articulation tourne un peu plus quand on tend (et il vibre au tir).
            float bend = Mathf.Lerp(5f, 15f, c) + Mathf.Sin(Time.time * 60f) * recoil * 6f;
            for (int i = 0; i < jointsR.Count; i++) jointsR[i].localRotation = Quaternion.Euler(0f, i == 0 ? bend * 0.5f : bend, 0f);
            for (int i = 0; i < jointsL.Count; i++) jointsL[i].localRotation = Quaternion.Euler(0f, i == 0 ? -bend * 0.5f : -bend, 0f);
            // La corde : d'un embout a l'autre, tiree en arriere par la tension.
            Vector3 tl = pitchPivot.InverseTransformPoint(tipL.position);
            Vector3 tr = pitchPivot.InverseTransformPoint(tipR.position);
            float rest = (tl.z + tr.z) * 0.5f;
            float nockZ = Mathf.Lerp(rest, 0.9f, c);
            Vector3 nock = new Vector3(0f, 0.34f, nockZ);
            cord.SetPosition(0, tipL.position);
            cord.SetPosition(1, pitchPivot.TransformPoint(nock));
            cord.SetPosition(2, tipR.position);
            // Le carreau suit la corde ; il n'est la que charge.
            bolt.localPosition = new Vector3(0f, 0.34f, nockZ - rest + 0.1f);
            if (bolt.gameObject.activeSelf != loaded) bolt.gameObject.SetActive(loaded);
            // On est assis a cheval sur le carreau.
            seat.localPosition = new Vector3(0f, 0.55f, bolt.localPosition.z + 1.2f);
            chargeBar.localScale = new Vector3(1f, 1f, Mathf.Max(0.01f, c * 6.4f));
            // Le treuil tourne pendant qu'on tend.
            if (charging && c < 1f) wheelTurn += Time.deltaTime * 400f;
            for (int i = 0; i < wheels.Count; i++) wheels[i].localRotation = Quaternion.Euler(wheelTurn, 0f, 0f);
        }

        /// <summary>
        /// La trajectoire, dessinee point par point jusqu'a ce qu'elle touche quelque
        /// chose ; l'anneau d'arrivee prend la couleur de ce qu'on va toucher.
        /// </summary>
        void DrawArc(Vector3 from, Vector3 v, float brightness)
        {
            const float step = 0.05f;
            int max = points.Length;
            int n = 0;
            Vector3 p = from;
            Vector3 vel = v;
            bool hit = false;
            RaycastHit rh = new RaycastHit();
            for (int i = 0; i < max - 1; i++)
            {
                points[n++] = p;
                Vector3 next = p + vel * step;
                vel += Vector3.down * Gravity * step;
                if (Physics.Linecast(p, next, out rh, ~0, QueryTriggerInteraction.Ignore) && (rider == null || rider.Body == null || !rh.collider.transform.IsChildOf(rider.Body)))
                {
                    points[n++] = rh.point;
                    hit = true;
                    break;
                }
                if (next.y < Ground.FallLine) break;
                p = next;
            }
            Landing = !hit ? LandingKind.Void
                    : Monument.NearestDistance(rh.point) < Monument.DeliverRadius + 1.5f && rh.point.y > 0f ? LandingKind.Monument
                    : LandingKind.Ground;
            Color c = Landing == LandingKind.Monument ? Monument.Blue : Landing == LandingKind.Ground ? GroundOk : new Color(1f, 0.55f, 0.35f);
            arc.enabled = true;
            arc.positionCount = n;
            for (int i = 0; i < n; i++) arc.SetPosition(i, points[i]);
            Color start = Color.Lerp(new Color(1f, 0.85f, 0.5f), c, 0.3f);
            arc.startColor = new Color(start.r, start.g, start.b, 0.9f * brightness);
            // Dans le vide : la ligne s'eteint vers le bout.
            arc.endColor = new Color(c.r, c.g, c.b, (Landing == LandingKind.Void ? 0.05f : 0.9f) * brightness);
            arc.widthMultiplier = 0.45f;
            marker.gameObject.SetActive(hit);
            if (!hit) return;
            marker.position = rh.point + rh.normal * 0.05f;
            marker.rotation = Quaternion.FromToRotation(Vector3.up, rh.normal);
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 7f);
            marker.localScale = Vector3.one * pulse;
            markerRing.startColor = new Color(c.r, c.g, c.b, 0.9f);
            markerRing.endColor = new Color(c.r, c.g, c.b, 0.9f);
            markerDisc.sharedMaterial = MaterialFactory.GetGlow(c, 2.2f);
            markerBeam.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(c, 1.6f);
        }

        // ================================================================== pour les bots

        /// <summary>
        /// La vitesse de tir (pleine tension) qui fait passer par "to" en partant de
        /// "from" (courbe haute : on retombe DESSUS). Faux si c'est trop loin.
        /// </summary>
        public static bool Solve(Vector3 from, Vector3 to, bool high, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            Vector3 d = to - from;
            Vector3 flat = new Vector3(d.x, 0f, d.z);
            float x = flat.magnitude;
            float y = d.y;
            float v2 = Power * Power;
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
            Vector3 p = Launcher;
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

        /// <summary>D'ou part le tir (fixe, quelle que soit la tension) : au-dessus de l'axe du bras.</summary>
        public Vector3 Launcher { get { return pitchPivot.position + Vector3.up * 0.9f; } }
        public Vector3 Seat { get { return Launcher; } }

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

    /// <summary>
    /// LE CARREAU QUI VOLE avec un bot tire par une arbaleste : on le voit passer dans
    /// le ciel, a cheval sur son trait, jusqu'a ce qu'il se pose ou ouvre ses ailes.
    /// </summary>
    public class BoltRide : MonoBehaviour
    {
        Seeker who;
        Vector3 last;
        float age;

        public static void Follow(Seeker s)
        {
            if (s == null || s.Body == null) return;
            GameObject go = new GameObject("Carreau en vol");
            go.transform.position = s.Body.position;
            BoltRide r = go.AddComponent<BoltRide>();
            r.who = s;
            r.last = s.Body.position;
            Transform model = new GameObject("Modèle").transform;
            model.SetParent(go.transform, false);
            model.localPosition = new Vector3(0f, 0f, -2.2f);
            Proto.BeginVisualOnly();
            Ballista.BoltModel(model);
            Proto.EndVisualOnly();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Rival r = who != null ? Rival.Of(who) : null;
            if (who == null || who.Body == null || age > 8f || age > 0.3f && (r == null || !r.Flying))
            {
                Fx.Sparks(transform.position, new Color(1f, 0.7f, 0.35f), 20, 4f);
                Destroy(gameObject);
                return;
            }
            Vector3 p = who.Body.position + Vector3.up * 0.4f;
            Vector3 v = (p - last) / dt;
            last = p;
            transform.position = p;
            if (v.sqrMagnitude > 1f) transform.rotation = Quaternion.LookRotation(v.normalized);
        }
    }
}
