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
        static readonly Color Emerald = new Color(0.2f, 0.9f, 0.45f);

        Transform runeRing;     // le cercle de runes du socle (il tourne)
        Transform crystals;     // les quatre cristaux qui tournent autour de la Couronne posee

        // ================================================================== construction

        public static Crown Build(Transform parent, Vector3 pedestal)
        {
            Holder = null;
            GameObject go = new GameObject("LA COURONNE");
            go.transform.SetParent(parent, false);
            go.transform.position = pedestal;
            Crown c = go.AddComponent<Crown>();
            Instance = c;

            // LE SOCLE (27/09 -- "une belle couronne, un beau truc") : trois marches de
            // pierre noire cerclees d'or, un cercle de runes qui tourne au ras du sol,
            // une colonne baguee d'or, un coussin pourpre -- et quatre cristaux qui
            // tournent autour de la Couronne tant qu'elle est la.
            GameObject socle = new GameObject("Socle de la Couronne");
            socle.transform.SetParent(parent, false);
            socle.transform.position = pedestal;
            Color stone = new Color(0.14f, 0.13f, 0.16f);
            Color trim = new Color(0.95f, 0.74f, 0.32f);
            float[] steps = { 2.6f, 1.9f, 1.25f };
            for (int k = 0; k < steps.Length; k++)
            {
                Proto.Cylinder(socle.transform, new Vector3(0f, 0.15f + k * 0.3f, 0f), new Vector3(steps[k] * 2f, 0.15f, steps[k] * 2f), stone, "Marche");
                Proto.BeginVisualOnly();
                GameObject rim = Proto.Cylinder(socle.transform, new Vector3(0f, 0.31f + k * 0.3f, 0f), new Vector3(steps[k] * 2f + 0.06f, 0.025f, steps[k] * 2f + 0.06f), Color.white, "Liseré d'or");
                rim.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(trim, 0.45f);
                Proto.EndVisualOnly();
            }
            Proto.Cylinder(socle.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.9f, 0.45f, 0.9f), stone, "Colonne");
            Proto.BeginVisualOnly();
            // (29/09) Plus de torsades lumineuses : six plaques d'or en travers de la colonne,
            // ca faisait des rayures jaunes. Deux bagues d'or mat, en haut et en bas, suffisent.
            Proto.Cylinder(socle.transform, new Vector3(0f, 0.95f, 0f), new Vector3(1.02f, 0.06f, 1.02f), trim, "Bague");
            Proto.Cylinder(socle.transform, new Vector3(0f, 1.66f, 0f), new Vector3(1.02f, 0.06f, 1.02f), trim, "Bague");
            Proto.Cylinder(socle.transform, new Vector3(0f, 1.78f, 0f), new Vector3(1.4f, 0.05f, 1.4f), trim, "Plateau");
            Proto.Cube(socle.transform, new Vector3(0f, 1.9f, 0f), new Vector3(0.95f, 0.2f, 0.95f), new Color(0.42f, 0.05f, 0.14f), "Coussin");
            for (int k = 0; k < 4; k++)
            {
                GameObject tassel = Proto.Cube(socle.transform, new Vector3(k < 2 ? -0.5f : 0.5f, 1.82f, k % 2 == 0 ? -0.5f : 0.5f), new Vector3(0.1f, 0.22f, 0.1f), trim, "Gland");
                tassel.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
            Proto.EndVisualOnly();
            c.runeRing = new GameObject("Cercle de runes").transform;
            c.runeRing.SetParent(socle.transform, false);
            c.runeRing.localPosition = new Vector3(0f, 0.12f, 0f);
            Proto.BeginVisualOnly();
            for (int k = 0; k < 24; k++)
            {
                float a = k / 24f * Mathf.PI * 2f;
                GameObject r = Proto.Cube(c.runeRing, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.4f, new Vector3(0.18f, 0.04f, k % 3 == 0 ? 0.9f : 0.45f), Color.white, "Rune");
                r.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                r.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(trim, 0.8f);
            }
            Proto.EndVisualOnly();
            c.crystals = new GameObject("Cristaux").transform;
            c.crystals.SetParent(socle.transform, false);
            c.crystals.localPosition = new Vector3(0f, 2.6f, 0f);
            Proto.BeginVisualOnly();
            Color[] gems = { Ruby, Sapphire, Emerald, Ruby };
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f;
                GameObject gem = Proto.Cube(c.crystals, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.4f, new Vector3(0.22f, 0.38f, 0.22f), Color.white, "Cristal");
                gem.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
                gem.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(gems[k], 1.6f);
            }
            Proto.EndVisualOnly();

            MaterialFactory.Polish(socle.transform, 0.6f);

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 2.2f, 0f);
            trigger.size = new Vector3(2f, 1.6f, 2f);

            GameObject v = new GameObject("Couronne");
            v.transform.SetParent(go.transform, false);
            v.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            c.visual = v.transform;
            c.home = v.transform.position;
            c.pedestal = pedestal;
            Proto.BeginVisualOnly();
            Model(v.transform, 1.7f);
            Proto.EndVisualOnly();
            // Des paillettes d'or qui tournent autour d'elle, ou qu'elle aille.
            Ambiance.Sparkles(v.transform, Vector3.zero, Gold);

            GameObject lightGo = new GameObject("Éclat");
            lightGo.transform.SetParent(v.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            c.glow = lightGo.AddComponent<Light>();
            c.glow.type = LightType.Point;
            c.glow.color = new Color(1f, 0.8f, 0.45f);
            c.glow.range = 9f;
            c.glow.intensity = 1.1f;
            c.glow.shadows = LightShadows.None;

            // La colonne doree : on la voit de toute l'ile.
            c.beam = LightBeam.Build(parent, c.home, new Color(1f, 0.8f, 0.35f), 1.6f, 90f);
            if (c.beam != null) c.beam.targetAlpha = 0.45f;
            return c;
        }

        /// <summary>
        /// LA COURONNE ELLE-MEME (30/09, refaite une troisieme fois -- "des meilleurs designs
        /// de couronne"). Rien que des formes rondes et de l'or POLI qui reflete le ciel :
        /// un bandeau lisse entre deux joncs, huit joyaux ronds, huit fleurons (des lys a
        /// trois branches et des pointes perlees), deux arceaux perles qui se croisent au-dessus
        /// d'un bonnet de velours, et le globe a la croix.
        ///
        /// Concept Unity : une courbe lisse, ici, c'est une suite de gelules (Capsule) mises
        /// bout a bout (Segment) -- leurs bouts ronds se fondent l'un dans l'autre.
        /// </summary>
        public static void Model(Transform t, float s)
        {
            Material gold = MaterialFactory.GetShiny(Gold, 0.88f, 1f, 0.45f);
            Material pearl = MaterialFactory.GetGlow(new Color(1f, 0.96f, 0.88f), 1.8f);
            Material velvet = MaterialFactory.GetShiny(new Color(0.5f, 0.05f, 0.14f), 0.4f, 0f);
            const float R = 0.27f;

            // Le bandeau, lisse, et ses deux joncs (bas, epais ; haut, fin).
            Paint(Proto.Cylinder(t, Vector3.up * 0.09f * s, new Vector3(R * 2f, 0.07f, R * 2f) * s, Gold, "Bandeau"), gold);
            Ring(t, R * 1.02f, 0.02f, 0.055f, s, gold);
            Ring(t, R * 1.01f, 0.165f, 0.035f, s, gold);

            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0f, dir.x);
                Vector3 root = dir * R + Vector3.up * 0.165f;
                if (i % 2 == 0)
                {
                    // Un lys : une branche droite perlee, deux qui s'ecartent en courbe.
                    Segment(t, root, root + Vector3.up * 0.2f - dir * 0.01f, 0.045f, s, gold);
                    Paint(Proto.Sphere(t, (root + Vector3.up * 0.23f) * s, Vector3.one * 0.06f * s, Color.white, "Perle"), pearl);
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Vector3 mid = root + side * (0.05f * k) + Vector3.up * 0.08f + dir * 0.012f;
                        Vector3 tip = root + side * (0.07f * k) + Vector3.up * 0.15f;
                        Segment(t, root + Vector3.up * 0.02f, mid, 0.03f, s, gold);
                        Segment(t, mid, tip, 0.026f, s, gold);
                    }
                }
                else
                {
                    Segment(t, root, root + Vector3.up * 0.11f, 0.038f, s, gold);
                    Paint(Proto.Sphere(t, (root + Vector3.up * 0.135f) * s, Vector3.one * 0.045f * s, Color.white, "Perle"), pearl);
                }
                // Un joyau rond sur le bandeau, entre deux fleurons.
                float g = a + Mathf.PI / 8f;
                Vector3 gd = new Vector3(Mathf.Cos(g), 0f, Mathf.Sin(g));
                Color jewel = i % 3 == 0 ? Ruby : i % 3 == 1 ? Sapphire : Emerald;
                GameObject gem = Paint(Proto.Sphere(t, (gd * (R + 0.012f) + Vector3.up * 0.09f) * s, new Vector3(0.062f, 0.07f, 0.035f) * s, jewel, "Joyau"),
                                       MaterialFactory.GetShiny(jewel, 0.92f, 0.2f, 1.5f));
                gem.transform.localRotation = Quaternion.LookRotation(gd, Vector3.up);
            }

            // Le bonnet de velours, et les deux arceaux perles qui se croisent au-dessus.
            Paint(Proto.Sphere(t, Vector3.up * 0.16f * s, new Vector3(0.5f, 0.36f, 0.5f) * s, Color.white, "Velours"), velvet);
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 last = dir * R * 0.97f + Vector3.up * 0.17f;
                for (int step = 1; step <= 5; step++)
                {
                    float th = step / 5f * Mathf.PI * 0.5f;
                    Vector3 p = dir * R * 0.97f * Mathf.Cos(th) + Vector3.up * (0.17f + 0.24f * Mathf.Sin(th));
                    Segment(t, last, p, 0.032f, s, gold);
                    if (step == 2 || step == 4) Paint(Proto.Sphere(t, p * s + dir * 0.02f * s, Vector3.one * 0.03f * s, Color.white, "Perle"), pearl);
                    last = p;
                }
            }
            // Le globe, et sa croix.
            Paint(Proto.Sphere(t, Vector3.up * 0.45f * s, Vector3.one * 0.1f * s, Gold, "Globe"), gold);
            Segment(t, Vector3.up * 0.49f, Vector3.up * 0.61f, 0.028f, s, gold);
            Segment(t, new Vector3(-0.045f, 0.565f, 0f), new Vector3(0.045f, 0.565f, 0f), 0.028f, s, gold);
        }

        static GameObject Paint(GameObject go, Material m)
        {
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>Une gelule de "a" a "b" (en unites de Couronne, mises a l'echelle "s").</summary>
        static void Segment(Transform t, Vector3 a, Vector3 b, float thick, float s, Material m)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            GameObject g = Paint(Proto.Capsule(t, (a + b) * 0.5f * s, new Vector3(thick, (len + thick) * 0.5f, thick) * s, Color.white, "Or"), m);
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / len);
        }

        /// <summary>Un jonc : un anneau de gelules.</summary>
        static void Ring(Transform t, float radius, float y, float thick, float s, Material m)
        {
            const int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                Segment(t, new Vector3(Mathf.Cos(a0) * radius, y, Mathf.Sin(a0) * radius), new Vector3(Mathf.Cos(a1) * radius, y, Mathf.Sin(a1) * radius), thick, s, m);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) { Instance = null; Holder = null; }
        }

        // ================================================================== vie

        Transform showOff;

        /// <summary>La manche est gagnee : la Couronne vole au-dessus de la tete du vainqueur.</summary>
        public void ShowOff(Transform winner) { showOff = winner; }

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
            bool mine = showOff == null && state == State.Carried && Holder != null && Holder.IsPlayer;
            if (mine != hiddenForMe)
            {
                hiddenForMe = mine;
                Renderer[] parts = visual.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < parts.Length; i++) parts[i].enabled = !mine;
            }
            if (beam != null && state != State.Delivered) { beam.targetAlpha = mine ? 0f : 0.45f; beam.fadeSpeed = mine ? 30f : 0.5f; }
            // Tombee et oubliee (45 s), ou tombee hors d'atteinte : elle retourne sur son socle.
            if (state == State.Dropped && (Time.time - droppedAt > ReturnSeconds || visual.position.y < Ground.FallLine))
                ReturnHome();
            // L'AIMANT : la Couronne a terre vole vers celui qui a la capacite (8 m).
            if (state == State.Dropped) { Attract(); PickUpByTouch(); }
            visual.Rotate(0f, (state == State.Carried ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
            if (state != State.Carried) visual.position = new Vector3(visual.position.x, BaseHeight() + Mathf.Sin(Time.time * 1.6f) * 0.05f, visual.position.z);
            // La fete du vainqueur : elle vient flotter au-dessus de sa tete, bien visible.
            if (showOff != null)
            {
                if (hiddenForMe)
                {
                    hiddenForMe = false;
                    Renderer[] all = visual.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < all.Length; i++) all[i].enabled = true;
                }
                Vector3 above = showOff.position + Vector3.up * (2.9f + Mathf.Sin(Time.time * 3f) * 0.12f);
                visual.position = Vector3.Lerp(visual.position, above, 1f - Mathf.Exp(-5f * Time.deltaTime));
            }
            if (beam != null) beam.source = new Vector3(visual.position.x, visual.position.y - 1.5f, visual.position.z);
            if (glow != null) glow.intensity = 1.1f * (0.85f + 0.15f * Mathf.Sin(Time.time * 4f));
            if (runeRing != null) runeRing.Rotate(0f, 12f * Time.deltaTime, 0f, Space.Self);
            if (crystals != null)
            {
                bool home2 = state == State.OnPedestal;
                if (crystals.gameObject.activeSelf != home2) crystals.gameObject.SetActive(home2);
                crystals.Rotate(0f, -40f * Time.deltaTime, 0f, Space.Self);
                crystals.localPosition = new Vector3(0f, 2.6f + Mathf.Sin(Time.time * 1.3f) * 0.15f, 0f);
            }
        }

        float groundY;
        bool hiddenForMe;
        Vector3 pedestal;

        /// <summary>Tombee, la Couronne attend 20 s qu'on la ramasse -- puis elle rentre au sommet.</summary>
        public const float ReturnSeconds = 20f;

        /// <summary>Encore combien de temps avant qu'elle retourne sur son socle (0 si elle n'est pas par terre).</summary>
        public static float ReturnIn { get { return Instance == null || Instance.state != State.Dropped ? 0f : Mathf.Max(0f, ReturnSeconds - (Time.time - Instance.droppedAt)); } }

        /// <summary>La Couronne rentre au sommet tout de suite (son porteur est tombe dans les nuages).</summary>
        public static void BackToTop()
        {
            if (Instance == null || Instance.state == State.Delivered) return;
            if (Holder != null) Holder.CrownLockUntil = Time.time + 1f;
            Holder = null;
            Instance.state = State.Dropped;
            Instance.ReturnHome();
        }

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
            if (Time.time < s.CrownLockUntil) return false;
            bool fromPedestal = state == State.OnPedestal;
            state = State.Carried;
            Holder = s;
            s.GripUsed = false;
            Sfx.Bell();
            if (s.IsPlayer) Stats.CrownsTaken++;
            if (fromPedestal) Sfx.Alarm();
            if (s.IsPlayer && Game.Hud != null) Game.Hud.ShowSplash("couronne", Gold);
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
            // Celui qui vient de la perdre ne la reprend pas tout de suite (trois secondes).
            if (was != null) was.CrownLockUntil = Time.time + LockSeconds;
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

        /// <summary>Qui vient de perdre (ou de se faire voler) la Couronne ne peut pas la reprendre avant...</summary>
        public const float LockSeconds = 3f;
        /// <summary>Le voleur est protege un instant : on ne la lui reprend pas dans la foulee.</summary>
        public const float StealGrace = 1.5f;

        /// <summary>
        /// LE VOL : "thief" pousse le porteur "victim" -- la Couronne passe directement
        /// dans ses mains. Le voleur est protege 1,5 s, la victime ne peut pas la
        /// reprendre pendant 3 s. Faux si le vol est impossible (Prise ferme, voleur
        /// encore "verrouille") : elle tombe alors normalement.
        /// </summary>
        public static bool TrySteal(Seeker thief, Seeker victim)
        {
            if (Instance == null || Holder != victim || thief == null || thief.Body == null) return false;
            if (Time.time < thief.CrownLockUntil || thief.Stunned) return false;
            if (victim.Has(Ability.PriseFerme) && !victim.GripUsed) return false;       // Combat.Hit s'en charge
            Holder = thief;
            thief.GripUsed = false;
            thief.GraceUntil = Time.time + StealGrace;
            victim.CrownLockUntil = Time.time + LockSeconds;
            // La Couronne saute d'une tete a l'autre : un trait d'or, une gerbe.
            Tether.Show(victim.Body, thief.Body, Vector3.zero, 0.5f, Gold);
            Fx.Sparks(thief.Body.position + Vector3.up * 2.3f, Gold, 50, 6f);
            Fx.Flash(thief.Body.position + Vector3.up * 2f, Gold, 14f, 5f, 0.4f);
            Sfx.Bell();
            if (thief.IsPlayer) { Stats.CrownsStolen++; Stats.CrownsTaken++; if (Game.Hud != null) Game.Hud.Flash(new Color(1f, 0.8f, 0.35f, 0.7f)); }
            Feed.CrownStolen(thief, victim);
            return true;
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
