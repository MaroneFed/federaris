using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA COURONNE : l'enjeu de la manche. Il n'y en a qu'une.
    ///
    /// Elle attend sur son socle, en haut du donjon, sous la garde du Roi Creux. Qui la
    /// prend (E maintenu) la porte au-dessus de sa tete : une COLONNE DOREE monte
    /// au-dessus de lui, tout le monde sait ou il est. Il marche moins vite, ne frappe
    /// plus. S'il tombe -- ou s'il se fait POUSSER --, elle roule par terre, et la
    /// colonne marque l'endroit. Le premier qui la porte au Monument gagne la manche.
    ///
    /// Toi et les bots passez par les memes methodes (TryTakeFor, Drop) : en Phase 3,
    /// c'est l'hote qui les appellera.
    /// </summary>
    public class Crown : MonoBehaviour, IInteractable
    {
        public static Crown Instance { get; private set; }

        /// <summary>Qui la porte (null : sur son socle, ou par terre).</summary>
        public static Seeker Holder { get; private set; }

        public enum State { OnPedestal, Carried, Dropped, Delivered }
        public static State Where { get { return Instance == null ? State.OnPedestal : Instance.state; } }

        /// <summary>Ou elle est, quoi qu'il arrive (sur son socle, sur une tete, par terre).</summary>
        public static Vector3 Position
        {
            get
            {
                if (Instance == null) return Vector3.zero;
                if (Holder != null && Holder.Body != null) return Holder.Body.position + Vector3.up * 2.2f;
                return Instance.visual.position;
            }
        }

        State state = State.OnPedestal;
        Transform visual;
        Vector3 home;
        Light glow;
        LightBeam beam;
        float droppedAt;

        static readonly Color Gold = new Color(0.95f, 0.76f, 0.3f);
        static readonly Color Ruby = new Color(0.9f, 0.18f, 0.2f);
        static readonly Color Sapphire = new Color(0.3f, 0.45f, 1f);

        // ================================================================== construction

        public static Crown Build(Transform parent, Vector3 pedestal)
        {
            Holder = null;
            GameObject go = new GameObject("LA COURONNE");
            go.transform.SetParent(parent, false);
            go.transform.position = pedestal;
            Crown c = go.AddComponent<Crown>();
            Instance = c;

            // Le socle (a part : la couronne, elle, peut quitter la terrasse) : une
            // colonne de pierre noire, un bord d'or, un coussin pourpre.
            GameObject socle = new GameObject("Socle de la Couronne");
            socle.transform.SetParent(parent, false);
            socle.transform.position = pedestal;
            Color stone = new Color(0.16f, 0.16f, 0.18f);
            Proto.Cylinder(socle.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.55f, 1.1f), stone, "Socle");
            Proto.BeginVisualOnly();
            Proto.Cylinder(socle.transform, new Vector3(0f, 1.12f, 0f), new Vector3(1.3f, 0.04f, 1.3f), new Color(0.72f, 0.58f, 0.3f), "Bord doré");
            Proto.Cube(socle.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.7f, 0.14f, 0.7f), new Color(0.35f, 0.06f, 0.12f), "Coussin");
            Proto.EndVisualOnly();

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.3f, 0f);
            trigger.size = new Vector3(1.6f, 1.2f, 1.6f);

            GameObject v = new GameObject("Couronne");
            v.transform.SetParent(go.transform, false);
            v.transform.localPosition = new Vector3(0f, 1.42f, 0f);
            c.visual = v.transform;
            c.home = v.transform.position;
            c.pedestal = pedestal;
            Proto.BeginVisualOnly();
            Model(v.transform, 1f);
            Proto.EndVisualOnly();
            // Des paillettes d'or qui tournent autour d'elle, ou qu'elle aille.
            Ambiance.Sparkles(v.transform, Vector3.zero, Gold);

            GameObject lightGo = new GameObject("Éclat");
            lightGo.transform.SetParent(v.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            c.glow = lightGo.AddComponent<Light>();
            c.glow.type = LightType.Point;
            c.glow.color = new Color(1f, 0.8f, 0.45f);
            c.glow.range = 8f;
            c.glow.intensity = 2.4f;
            c.glow.shadows = LightShadows.None;

            // La colonne doree : on la voit de toute la foret.
            c.beam = LightBeam.Build(parent, c.home, new Color(1f, 0.8f, 0.35f), 1.4f, 70f);
            if (c.beam != null) c.beam.targetAlpha = 0.45f;
            return c;
        }

        /// <summary>La couronne elle-meme : un bandeau d'or a dix pans, des fleurons, des joyaux.</summary>
        public static void Model(Transform t, float s)
        {
            Material gold = MaterialFactory.GetGlow(Gold, 1.6f);
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.22f, 0.07f, Mathf.Sin(a) * 0.22f) * s;
                GameObject band = Proto.Cube(t, p, new Vector3(0.15f, 0.14f, 0.035f) * s, Gold, "Bandeau");
                band.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                band.GetComponent<Renderer>().sharedMaterial = gold;
                if (i % 2 == 0)
                {
                    GameObject point = Proto.Cone(t, p + new Vector3(0f, 0.07f, 0f) * s, 0.05f * s, 0.2f * s, Gold, "Fleuron", 4);
                    point.GetComponent<Renderer>().sharedMaterial = gold;
                    GameObject gem = Proto.Cube(t, p * 1.08f + new Vector3(0f, 0.06f, 0f) * s, new Vector3(0.05f, 0.05f, 0.02f) * s,
                                                i % 4 == 0 ? Ruby : Sapphire, "Joyau");
                    gem.transform.localRotation = band.transform.localRotation;
                    gem.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(i % 4 == 0 ? Ruby : Sapphire, 2.4f);
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this) { Instance = null; Holder = null; }
        }

        // ================================================================== vie

        void Update()
        {
            if (visual == null) return;
            if (state == State.Carried)
            {
                if (Holder == null || Holder.Body == null) { Drop(visual.position); return; }
                // Au-dessus de sa tete : tout le monde la voit briller.
                visual.position = Holder.Body.position + Vector3.up * 2.25f + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.05f;
            }
            // Quand c'est TOI qui la portes, on ne la montre pas au-dessus de ta tete ni
            // sa colonne (la camera serait dedans) : l'ecran te le dit, et tu brilles.
            bool mine = state == State.Carried && Holder != null && Holder.IsPlayer;
            if (mine != hiddenForMe)
            {
                hiddenForMe = mine;
                Renderer[] parts = visual.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < parts.Length; i++) parts[i].enabled = !mine;
            }
            if (beam != null && state != State.Delivered) { beam.targetAlpha = mine ? 0f : 0.45f; beam.fadeSpeed = mine ? 30f : 0.5f; }
            // Tombee et oubliee (45 s), ou tombee hors d'atteinte : elle retourne sur son socle.
            if (state == State.Dropped && (Time.time - droppedAt > ReturnSeconds || visual.position.y < Ground.Sample(visual.position.x, visual.position.z) - 3f))
                ReturnHome();
            // L'AIMANT : la Couronne a terre vole vers celui qui a la capacite (8 m).
            if (state == State.Dropped) Attract();
            visual.Rotate(0f, (state == State.Carried ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
            if (state != State.Carried) visual.position = new Vector3(visual.position.x, BaseHeight() + Mathf.Sin(Time.time * 1.6f) * 0.05f, visual.position.z);
            if (beam != null) beam.source = new Vector3(visual.position.x, visual.position.y - 1.5f, visual.position.z);
            if (glow != null) glow.intensity = 2.4f * (0.85f + 0.15f * Mathf.Sin(Time.time * 4f));
        }

        float groundY;
        bool hiddenForMe;
        Vector3 pedestal;

        /// <summary>Tombee, la Couronne attend 45 s qu'on la ramasse -- puis elle rentre au donjon.</summary>
        public const float ReturnSeconds = 45f;

        /// <summary>Encore combien de temps avant qu'elle retourne sur son socle (0 si elle n'est pas par terre).</summary>
        public static float ReturnIn { get { return Instance == null || Instance.state != State.Dropped ? 0f : Mathf.Max(0f, ReturnSeconds - (Time.time - Instance.droppedAt)); } }

        void ReturnHome()
        {
            state = State.OnPedestal;
            transform.position = pedestal;
            visual.position = home;
            Sfx.Bell();
            if (Game.Hud != null) Game.Hud.ShowDiscovery("", "LA COURONNE", "revient au sommet de la tour", "", Gold);
        }
        float BaseHeight() { return state == State.OnPedestal ? home.y : state == State.Delivered ? deliveredY : groundY; }

        // ================================================================== les gestes

        /// <summary>Prendre la couronne. Vrai si prise.</summary>
        public bool TryTakeFor(Seeker s)
        {
            if (s == null || s.Body == null || s.Stunned || state == State.Carried || state == State.Delivered) return false;
            bool fromPedestal = state == State.OnPedestal;
            state = State.Carried;
            Holder = s;
            s.GripUsed = false;
            Sfx.Bell();
            if (s.IsPlayer) Stats.CrownsTaken++;
            if (fromPedestal) Sfx.Alarm();
            if (Game.Hud != null) Game.Hud.ShowDiscovery("", "LA COURONNE", s.IsPlayer ? "Au Monument !" : s.Name, "", s.Colour);
            if (s.IsPlayer && Game.Hud != null) Game.Hud.Flash(new Color(1f, 0.8f, 0.35f, 0.7f));
            return true;
        }

        /// <summary>
        /// Posee au Monument : elle quitte les mains du porteur et se pose sur l'autel,
        /// dans un eclat bleu. La manche est gagnee (voir Monument.TryDeliver).
        /// </summary>
        public void PlaceOn(Vector3 altar)
        {
            if (state != State.Carried) return;
            state = State.Delivered;
            Holder = null;
            transform.position = altar - Vector3.up * 1.3f;
            visual.position = altar;
            deliveredY = altar.y;
            if (beam != null) { beam.color = Monument.Blue; beam.targetAlpha = 1f; }
        }

        float deliveredY;

        /// <summary>La lacher, la ou l'on est (tombe, pousse, pris au piege).</summary>
        public void Drop(Vector3 at)
        {
            if (state != State.Carried) return;
            Seeker was = Holder;
            state = State.Dropped;
            Holder = null;
            droppedAt = Time.time;
            // Par terre, un peu devant : on la voit rouler.
            float y = Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y : Ground.Sample(at.x, at.z);
            groundY = y + 0.35f;
            // Le declencheur d'abord (la couronne visible est son enfant : le bouger
            // apres elle la decalerait d'autant), puis la couronne elle-meme.
            transform.position = new Vector3(at.x, y - 1.05f, at.z);
            visual.position = new Vector3(at.x, groundY, at.z);
            Sfx.Thud();
            Ambiance.Burst(null, visual.position, Gold);
            if (was != null && was.IsPlayer && Game.Hud != null) Game.Hud.ShowDiscovery("", "Couronne perdue", "", "", new Color(1f, 0.45f, 0.35f));
        }

        /// <summary>Pousse : le porteur la lache, elle roule dans la direction du coup.</summary>
        /// <summary>
        /// LA COURONNE GLISSE : son porteur tombe (sans planer). Elle reste la ou il a
        /// quitte le sol -- on ne redescend pas la tour d'un saut.
        /// </summary>
        public static void Slip(Seeker holder, Vector3 lastGround)
        {
            if (Instance == null || Holder != holder) return;
            Instance.Drop(lastGround);
            if (holder.IsPlayer && Game.Hud != null) Game.Hud.ShowDiscovery("", "La Couronne a glissé", "elle est restée en haut", "", new Color(1f, 0.6f, 0.4f));
        }

        void Attract()
        {
            Seeker best = null;
            float bestD = 8f;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || s.Stunned || !s.Has(Ability.Aimant)) continue;
                float d = (s.Body.position + Vector3.up - visual.position).magnitude;
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null) return;
            if (bestD < 1.4f) { TryTakeFor(best); return; }
            Vector3 to = best.Body.position + Vector3.up - visual.position;
            Vector3 step = to.normalized * Mathf.Min(to.magnitude, 9f * Time.deltaTime);
            visual.position += step;
            transform.position += step;
            groundY = visual.position.y;
        }

        public static void KnockOff(Seeker victim, Vector3 direction)
        {
            if (Instance == null || Holder != victim || victim.Body == null) return;
            Vector3 flat = new Vector3(direction.x, 0f, direction.z).normalized;
            Instance.Drop(victim.Body.position + flat * 2.2f);
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return visual != null ? visual : transform; } }
        public bool CanInteract { get { return state != State.Carried && state != State.Delivered && Game.Season != null && Game.Season.Running && Game.Me != null && !Game.Me.Stunned; } }
        public string Prompt { get { return "La Couronne"; } }
        public float HoldDuration { get { return state == State.OnPedestal ? 1.2f : 0.5f; } }

        public void Interact()
        {
            TryTakeFor(Game.Me);
        }
    }
}
