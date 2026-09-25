using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA COURONNE : l'enjeu de la manche. Il n'y en a qu'une.
    ///
    /// Elle attend sur son socle, au sommet de la tour. Qui la prend (E maintenu une
    /// seconde) la porte au-dessus de sa tete : une COLONNE DOREE monte au-dessus de
    /// lui, tout le monde sait ou il est. Il va moins vite et ne pousse plus. Si on le
    /// POUSSE, elle roule par terre ; s'il SAUTE de haut, elle reste la ou il a quitte
    /// le sol. A terre, il suffit de lui PASSER DESSUS pour la ramasser (27/09 : on ne
    /// cherche pas la touche E en pleine bagarre). Le premier qui entre dans le cercle
    /// du Monument avec elle gagne la manche.
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
            if (state == State.Dropped) { Attract(); PickUpByTouch(); }
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
            Feed.CrownHome();
        }
        float BaseHeight() { return state == State.OnPedestal ? home.y : state == State.Delivered ? deliveredY : groundY; }

        // ================================================================== les gestes

        /// <summary>Prendre la couronne. Vrai si prise.</summary>
        public bool TryTakeFor(Seeker s)
        {
            if (s == null || s.Body == null || s.Stunned || state == State.Carried || state == State.Delivered) return false;
            if (Game.Season == null || !Game.Season.Running) return false;
            bool fromPedestal = state == State.OnPedestal;
            state = State.Carried;
            Holder = s;
            s.GripUsed = false;
            Sfx.Bell();
            if (s.IsPlayer) Stats.CrownsTaken++;
            if (fromPedestal) Sfx.Alarm();
            if (s.IsPlayer && Game.Hud != null) Game.Hud.ShowDiscovery("", "LA COURONNE", "Au Monument : la colonne bleue !", "", Gold);
            Feed.CrownTaken(s, fromPedestal);
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
        public void Drop(Vector3 at) { Drop(at, at); }

        /// <summary>La lacher en "at" ; si "at" est dans un mur, en "fallback" (la ou etait le porteur).</summary>
        public void Drop(Vector3 at, Vector3 fallback)
        {
            if (state != State.Carried) return;
            Seeker was = Holder;
            state = State.Dropped;
            Holder = null;
            droppedAt = Time.time;
            // Par terre, un peu devant : on la voit rouler.
            at = SafeSpot(at, fallback);
            float y = Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y : Ground.Sample(at.x, at.z);
            // Celui qui vient de la perdre ne la rattrape pas en retombant dessus.
            if (was != null) was.CrownLockUntil = Time.time + 1.2f;
            groundY = y + 0.35f;
            // Le declencheur d'abord (la couronne visible est son enfant : le bouger
            // apres elle la decalerait d'autant), puis la couronne elle-meme.
            transform.position = new Vector3(at.x, y - 1.05f, at.z);
            visual.position = new Vector3(at.x, groundY, at.z);
            Sfx.Thud();
            Ambiance.Burst(null, visual.position, Gold);
        }

        /// <summary>
        /// Un endroit ou la Couronne peut tomber : jamais DANS le fut de la tour (un
        /// coup venu de dehors poussait le porteur contre le mur, et la Couronne
        /// traversait la pierre jusqu'au pied de la tour, introuvable), jamais dans un
        /// mur. Sinon, la ou etait son porteur.
        /// </summary>
        static Vector3 SafeSpot(Vector3 at, Vector3 fallback)
        {
            Vector2 flat = new Vector2(at.x, at.z);
            if (flat.magnitude < Tower.Radius + 1f && at.y < Tower.Height - 0.5f)
            {
                Vector2 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector2.up;
                flat = dir * (Tower.Radius + 1.4f);
                at = new Vector3(flat.x, at.y, flat.y);
            }
            if (Physics.CheckSphere(at + Vector3.up * 0.6f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) at = fallback;
            return at;
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
            Feed.CrownSlipped(holder);
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

        /// <summary>A terre, la Couronne se ramasse en passant dessus.</summary>
        void PickUpByTouch()
        {
            if (state != State.Dropped || Game.Season == null || !Game.Season.Running) return;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || s.Stunned || Time.time < s.CrownLockUntil) continue;
                if ((s.Body.position + Vector3.up * 0.9f - visual.position).magnitude > 1.7f) continue;
                if (TryTakeFor(s)) return;
            }
        }

        public static void KnockOff(Seeker victim, Vector3 direction)
        {
            if (Instance == null || Holder != victim || victim.Body == null) return;
            Vector3 flat = new Vector3(direction.x, 0f, direction.z).normalized;
            Instance.Drop(victim.Body.position + flat * 2.2f, victim.Body.position);
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return visual != null ? visual : transform; } }
        public bool CanInteract { get { return state != State.Carried && state != State.Delivered && Game.Season != null && Game.Season.Running && Game.Me != null && !Game.Me.Stunned; } }
        public string Prompt { get { return state == State.OnPedestal ? "Prendre la Couronne" : "Ramasser la Couronne"; } }
        public float HoldDuration { get { return state == State.OnPedestal ? 1f : 0.2f; } }

        public void Interact()
        {
            TryTakeFor(Game.Me);
        }
    }
}
