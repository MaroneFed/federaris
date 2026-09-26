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
            // une colonne torsadee, un coussin pourpre -- et quatre cristaux qui
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
                rim.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(trim, 1.4f);
                Proto.EndVisualOnly();
            }
            Proto.Cylinder(socle.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.9f, 0.45f, 0.9f), stone, "Colonne");
            Proto.BeginVisualOnly();
            for (int k = 0; k < 6; k++)
            {
                GameObject twist = Proto.Cube(socle.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 0.95f, 0.95f), Color.white, "Torsade");
                twist.transform.localRotation = Quaternion.Euler(0f, k * 30f, 18f);
                twist.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(trim, 1.1f);
            }
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
            c.runeRing.localPosition = new Vector3(0f, 0.05f, 0f);
            Proto.BeginVisualOnly();
            for (int k = 0; k < 24; k++)
            {
                float a = k / 24f * Mathf.PI * 2f;
                GameObject r = Proto.Cube(c.runeRing, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.4f, new Vector3(0.18f, 0.04f, k % 3 == 0 ? 0.9f : 0.45f), Color.white, "Rune");
                r.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                r.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(trim, 2f);
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
                gem.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(gems[k], 3f);
            }
            Proto.EndVisualOnly();

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
            c.glow.range = 12f;
            c.glow.intensity = 2.4f;
            c.glow.shadows = LightShadows.None;

            // La colonne doree : on la voit de toute l'ile.
            c.beam = LightBeam.Build(parent, c.home, new Color(1f, 0.8f, 0.35f), 1.6f, 90f);
            if (c.beam != null) c.beam.targetAlpha = 0.45f;
            return c;
        }

        /// <summary>
        /// LA COURONNE ELLE-MEME (27/09, refaite) : un bandeau d'or a seize pans entre
        /// deux filets, huit fleurons -- quatre croix et quatre pointes perlees --, une
        /// rangee de joyaux (rubis, saphirs, emeraudes) tailles en losange, un bonnet de
        /// velours pourpre, et au sommet un globe d'or surmonte d'une croix.
        /// </summary>
        public static void Model(Transform t, float s)
        {
            Material gold = MaterialFactory.GetGlow(Gold, 1.7f);
            Material paleGold = MaterialFactory.GetGlow(new Color(1f, 0.9f, 0.6f), 2.2f);
            const int n = 16;
            const float R = 0.26f;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Quaternion face = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                GameObject band = Proto.Cube(t, dir * R * s + Vector3.up * 0.08f * s, new Vector3(0.11f, 0.14f, 0.03f) * s, Gold, "Bandeau");
                band.transform.localRotation = face;
                band.GetComponent<Renderer>().sharedMaterial = gold;
                for (int k = 0; k < 2; k++)
                {
                    GameObject fillet = Proto.Cube(t, dir * (R + 0.012f) * s + Vector3.up * (k == 0 ? 0.015f : 0.15f) * s, new Vector3(0.115f, 0.025f, 0.035f) * s, Gold, "Filet");
                    fillet.transform.localRotation = face;
                    fillet.GetComponent<Renderer>().sharedMaterial = paleGold;
                }
                if (i % 2 == 0)
                {
                    // Un fleuron sur deux pans : croix et pointes perlees en alternance.
                    Vector3 root = dir * R * s + Vector3.up * 0.16f * s;
                    if (i % 4 == 0)
                    {
                        GameObject stem = Proto.Cube(t, root + Vector3.up * 0.09f * s, new Vector3(0.035f, 0.18f, 0.03f) * s, Gold, "Croix");
                        stem.transform.localRotation = face;
                        stem.GetComponent<Renderer>().sharedMaterial = gold;
                        GameObject bar = Proto.Cube(t, root + Vector3.up * 0.13f * s, new Vector3(0.1f, 0.035f, 0.03f) * s, Gold, "Croix");
                        bar.transform.localRotation = face;
                        bar.GetComponent<Renderer>().sharedMaterial = gold;
                    }
                    else
                    {
                        GameObject point = Proto.Cone(t, root, 0.045f * s, 0.2f * s, Gold, "Pointe", 4);
                        point.GetComponent<Renderer>().sharedMaterial = gold;
                        GameObject pearl = Proto.Sphere(t, root + Vector3.up * 0.21f * s, Vector3.one * 0.055f * s, Color.white, "Perle");
                        pearl.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.97f, 0.9f), 2.6f);
                    }
                }
                else
                {
                    // Entre deux fleurons, un joyau taille en losange.
                    Color jewel = (i / 2) % 3 == 0 ? Ruby : (i / 2) % 3 == 1 ? Sapphire : Emerald;
                    GameObject gem = Proto.Cube(t, dir * (R + 0.02f) * s + Vector3.up * 0.085f * s, new Vector3(0.055f, 0.055f, 0.025f) * s, jewel, "Joyau");
                    gem.transform.localRotation = face * Quaternion.Euler(0f, 0f, 45f);
                    gem.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(jewel, 2.6f);
                }
            }
            // Le bonnet de velours, les arceaux, le globe et sa croix.
            Proto.Sphere(t, Vector3.up * 0.14f * s, new Vector3(0.46f, 0.34f, 0.46f) * s, new Color(0.45f, 0.05f, 0.16f), "Velours");
            for (int k = 0; k < 2; k++)
            {
                GameObject arch = Proto.Cube(t, Vector3.up * 0.3f * s, new Vector3(0.5f, 0.03f, 0.035f) * s, Gold, "Arceau");
                arch.transform.localRotation = Quaternion.Euler(0f, k * 90f, 0f);
                arch.GetComponent<Renderer>().sharedMaterial = gold;
            }
            GameObject orb = Proto.Sphere(t, Vector3.up * 0.36f * s, Vector3.one * 0.09f * s, Gold, "Globe");
            orb.GetComponent<Renderer>().sharedMaterial = paleGold;
            GameObject up = Proto.Cube(t, Vector3.up * 0.46f * s, new Vector3(0.03f, 0.12f, 0.03f) * s, Gold, "Croix du globe");
            up.GetComponent<Renderer>().sharedMaterial = gold;
            GameObject cross = Proto.Cube(t, Vector3.up * 0.47f * s, new Vector3(0.08f, 0.03f, 0.03f) * s, Gold, "Croix du globe");
            cross.GetComponent<Renderer>().sharedMaterial = gold;
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
            if (state == State.Dropped && (Time.time - droppedAt > ReturnSeconds || visual.position.y < Ground.FallLine))
                ReturnHome();
            // L'AIMANT : la Couronne a terre vole vers celui qui a la capacite (8 m).
            if (state == State.Dropped) { Attract(); PickUpByTouch(); }
            visual.Rotate(0f, (state == State.Carried ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
            if (state != State.Carried) visual.position = new Vector3(visual.position.x, BaseHeight() + Mathf.Sin(Time.time * 1.6f) * 0.05f, visual.position.z);
            if (beam != null) beam.source = new Vector3(visual.position.x, visual.position.y - 1.5f, visual.position.z);
            if (glow != null) glow.intensity = 2.4f * (0.85f + 0.15f * Mathf.Sin(Time.time * 4f));
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
