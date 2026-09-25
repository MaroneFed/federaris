using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA HERSE de la grande porte. Elle est BAISSEE : pour entrer par la grande porte,
    /// il faut qu'un joueur (deja entre par la poterne ou la breche) tire le LEVIER,
    /// dans la cour, au pied du chatelet. Elle monte, reste levee 45 secondes, puis
    /// retombe -- sauf si quelqu'un est dessous.
    ///
    /// Ouvrir la herse, c'est ouvrir la route a tout le monde : les autres joueurs,
    /// et le chemin du retour de celui qui portera la Couronne.
    /// </summary>
    public class Portcullis : MonoBehaviour
    {
        public static Portcullis Instance { get; private set; }
        /// <summary>Levee (assez pour passer dessous).</summary>
        public static bool IsOpen { get { return Instance != null && Instance.open01 > 0.85f; } }

        public const float OpenSeconds = 45f;

        float open01;
        float target;
        float openUntil;
        Vector3 closed;
        float width, height;

        static readonly Color Iron = new Color(0.12f, 0.12f, 0.13f);

        public static Portcullis Build(Transform parent, Vector3 foot, float width, float height)
        {
            GameObject go = new GameObject("HERSE");
            go.transform.SetParent(parent, false);
            go.transform.position = foot;
            Portcullis p = go.AddComponent<Portcullis>();
            Instance = p;
            p.closed = foot;
            p.width = width;
            p.height = height;

            BoxCollider wall = go.AddComponent<BoxCollider>();
            wall.center = new Vector3(0f, height * 0.5f, 0f);
            wall.size = new Vector3(width, height, 0.35f);

            Proto.BeginVisualOnly();
            Transform t = go.transform;
            for (float x = -width * 0.5f + 0.4f; x < width * 0.5f; x += 0.7f)
            {
                Proto.Cube(t, new Vector3(x, height * 0.5f, 0f), new Vector3(0.14f, height, 0.14f), Iron, "Barreau");
                GameObject spike = Proto.Cube(t, new Vector3(x, -0.08f, 0f), new Vector3(0.14f, 0.14f, 0.14f), Iron, "Pointe");
                spike.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            }
            for (float y = 0.9f; y < height; y += 1.3f)
                Proto.Cube(t, new Vector3(0f, y, 0f), new Vector3(width, 0.12f, 0.16f), Iron, "Traverse");
            Proto.EndVisualOnly();
            return p;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Lever la herse (le levier). Vrai si elle se leve.</summary>
        public bool Raise()
        {
            if (target >= 1f && open01 > 0.1f) { openUntil = Time.time + OpenSeconds; return false; }
            target = 1f;
            openUntil = Time.time + OpenSeconds;
            Sfx.Clang();
            Sfx.Creak3D(transform.position + Vector3.up * height);
            return true;
        }

        void Update()
        {
            if (target >= 1f && Time.time > openUntil && !Blocked()) target = 0f;
            float was = open01;
            open01 = Mathf.MoveTowards(open01, target, Time.deltaTime * (target > open01 ? 0.45f : 0.3f));
            if (Mathf.Approximately(was, open01)) return;
            transform.position = closed + Vector3.up * open01 * (height - 0.4f);
            if (open01 <= 0f && was > 0f) Sfx.Thud();
        }

        /// <summary>Quelqu'un (joueur ou garde) est sous la herse : elle attend.</summary>
        bool Blocked()
        {
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body != null && Under(s.Body.position)) return true;
            }
            for (int i = 0; i < Guard.All.Count; i++)
                if (Guard.All[i] != null && Guard.All[i].Alive && Under(Guard.All[i].transform.position)) return true;
            return false;
        }

        bool Under(Vector3 p)
        {
            return Mathf.Abs(p.x - closed.x) < width * 0.5f + 0.3f && Mathf.Abs(p.z - closed.z) < 1.8f;
        }

        /// <summary>Encore combien de temps levee (0 si baissee).</summary>
        public static float Remaining { get { return Instance == null || Instance.target < 1f ? 0f : Mathf.Max(0f, Instance.openUntil - Time.time); } }
    }

    /// <summary>LE LEVIER de la herse : dans la cour, contre le chatelet. E maintenu une seconde.</summary>
    public class Lever : MonoBehaviour, IInteractable
    {
        public static Lever Instance { get; private set; }
        Transform arm;

        public static Lever Build(Transform parent, Vector3 at, float yaw)
        {
            GameObject go = new GameObject("LEVIER DE LA HERSE");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Lever l = go.AddComponent<Lever>();
            Instance = l;
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            trigger.size = new Vector3(1.4f, 1.6f, 1.4f);
            Color stone = new Color(0.23f, 0.23f, 0.23f), iron = new Color(0.14f, 0.14f, 0.15f);
            Proto.Cube(go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 1f, 0.5f), stone, "Socle");
            Proto.BeginVisualOnly();
            GameObject pivot = new GameObject("Bras");
            pivot.transform.SetParent(go.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1f, 0f);
            l.arm = pivot.transform;
            Proto.Cube(l.arm, new Vector3(0f, 0.45f, 0f), new Vector3(0.08f, 0.9f, 0.08f), iron, "Levier");
            GameObject knob = Proto.Sphere(l.arm, new Vector3(0f, 0.92f, 0f), Vector3.one * 0.14f, Color.white, "Pommeau");
            knob.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.95f, 0.62f, 0.3f), 1.4f);
            // Une chaine qui monte au chatelet.
            for (int i = 0; i < 6; i++) Proto.Cube(go.transform, new Vector3(0f, 1.2f + i * 0.5f, -0.3f), new Vector3(0.05f, 0.35f, 0.05f), iron, "Chaîne");
            Proto.EndVisualOnly();
            return l;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            float want = Portcullis.IsOpen || Portcullis.Remaining > 0f ? 50f : -50f;
            if (arm != null) arm.localRotation = Quaternion.Slerp(arm.localRotation, Quaternion.Euler(want, 0f, 0f), Time.deltaTime * 5f);
        }

        /// <summary>Tirer le levier (toi ou un bot).</summary>
        public bool PullFor(Seeker s)
        {
            if (s == null || !s.Alive || Portcullis.Instance == null) return false;
            return Portcullis.Instance.Raise();
        }

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return Portcullis.Instance != null && Portcullis.Remaining < Portcullis.OpenSeconds - 5f; } }
        public string Prompt { get { return "Levier"; } }
        public float HoldDuration { get { return 1f; } }
        public void Interact() { PullFor(Game.Me); }
    }

    /// <summary>
    /// LA PORTE DEROBEE du donjon : une petite porte de fer au pied du mur nord,
    /// fermee a cle. Avec la CLE DU DONJON (un tresor enterre), on l'ouvre -- et
    /// derriere, un escalier dans l'epaisseur du mur monte droit a la TERRASSE, sans
    /// traverser une seule salle. Une fois ouverte, elle le reste pour tout le monde.
    /// </summary>
    public class SecretDoor : MonoBehaviour, IInteractable
    {
        public static SecretDoor Instance { get; private set; }
        bool open;
        Vector3 landing;
        float landingYaw;
        Transform leaf;

        public bool Open { get { return open; } }
        public Vector3 Landing { get { return landing; } }

        public static SecretDoor Build(Transform parent, Vector3 foot, float yaw, Vector3 landing, float landingYaw)
        {
            GameObject go = new GameObject("PORTE DÉROBÉE");
            go.transform.SetParent(parent, false);
            go.transform.position = foot;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            SecretDoor d = go.AddComponent<SecretDoor>();
            Instance = d;
            d.landing = landing;
            d.landingYaw = landingYaw;
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0.6f);
            trigger.size = new Vector3(1.8f, 2.2f, 1.4f);

            Color iron = new Color(0.14f, 0.13f, 0.13f);
            Proto.BeginVisualOnly();
            Proto.Cube(go.transform, new Vector3(0f, 1.05f, -0.02f), new Vector3(1.3f, 2.1f, 0.06f), new Color(0.03f, 0.03f, 0.03f), "Ombre");
            GameObject hinge = new GameObject("Battant");
            hinge.transform.SetParent(go.transform, false);
            hinge.transform.localPosition = new Vector3(-0.55f, 0f, 0.04f);
            d.leaf = hinge.transform;
            Proto.Cube(d.leaf, new Vector3(0.55f, 1.0f, 0f), new Vector3(1.1f, 2f, 0.08f), iron, "Porte de fer");
            for (int i = 0; i < 4; i++) Proto.Cube(d.leaf, new Vector3(0.55f, 0.3f + i * 0.5f, 0.05f), new Vector3(1.12f, 0.06f, 0.03f), new Color(0.22f, 0.2f, 0.18f), "Pentures");
            GameObject lockGlow = Proto.Cube(d.leaf, new Vector3(0.95f, 1f, 0.06f), new Vector3(0.1f, 0.14f, 0.02f), Color.white, "Serrure");
            lockGlow.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(ItemInfo.Tint(Item.Cle), 1.6f);
            Proto.EndVisualOnly();
            return d;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (leaf != null) leaf.localRotation = Quaternion.Slerp(leaf.localRotation, Quaternion.Euler(0f, open ? -100f : 0f, 0f), Time.deltaTime * 4f);
        }

        /// <summary>
        /// "s" ouvre la porte (avec la cle) ou, si elle l'est deja, prend l'escalier.
        /// Vrai s'il est monte. En Phase 3, c'est l'hote qui l'appellera.
        /// </summary>
        public bool UseFor(Seeker s)
        {
            if (s == null || !s.Alive || s.CarriesCrown) return false;
            if (!open)
            {
                if (!s.Items.TryRemove(Item.Cle)) return false;
                open = true;
                Sfx.Clang();
                Sfx.Creak3D(transform.position + Vector3.up);
            }
            Vector3 top = landing;
            if (s.IsPlayer && Game.Player != null)
            {
                Game.Player.Teleport(top, landingYaw);
                if (Game.Hud != null) Game.Hud.Flash(new Color(0f, 0f, 0f, 1f));
                Sfx.Rustle();
            }
            else
            {
                Rival r = Rival.Of(s);
                if (r != null) r.Teleport(top);
            }
            return true;
        }

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return Game.Me != null && Game.Me.Alive && !Game.Me.CarriesCrown && (open || Game.Me.Items.Has(Item.Cle)); } }
        public string Prompt { get { return open ? "Escalier dérobé" : "Porte dérobée"; } }
        public float HoldDuration { get { return open ? 0.6f : 1.2f; } }
        public void Interact() { if (!UseFor(Game.Me)) Sfx.Deny(); }
    }
}
