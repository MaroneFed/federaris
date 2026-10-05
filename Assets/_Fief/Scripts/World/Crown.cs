using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA COURONNE : l'enjeu de la manche. Il n'y en a qu'une.
    ///
    /// Elle attend sur son socle, au sommet de la tour. Qui la prend (en montant sur le
    /// socle, ou d'un appui sur E) la porte au-dessus de sa tete : une COLONNE DOREE monte au-dessus de
    /// lui, tout le monde sait ou il est. Il va moins vite et ne pousse plus. Si on le
    /// POUSSE, elle roule par terre ; s'il SAUTE de haut ou TOMBE DANS LES NUAGES, elle
    /// reste la ou il a quitte le sol -- et elle Y RESTE, aussi longtemps qu'il faut
    /// (02/10 -- Martin : "il faut la laisser bien ou elle est, c'est horrible de tout
    /// remonter a chaque fois" : plus de retour au sommet, ni a la chute ni au bout de
    /// 20 s). A terre, il suffit de lui PASSER DESSUS pour la ramasser (27/09 : on ne
    /// cherche pas la touche E en pleine bagarre). Le premier qui entre dans le cercle
    /// du Monument avec elle, et y reste trois secondes, gagne la manche.
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
                if (Holder != null && Holder.Body != null) return Holder.Body.position + Vector3.up * 2.6f;
                return Instance.visual.position;
            }
        }

        State state = State.OnPedestal;
        Transform visual;
        Vector3 home;
        Light glow;
        LightBeam beam;

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
            c.beam = LightBeam.Build(parent, c.home, new Color(1f, 0.8f, 0.35f), 2.2f, 120f);
            if (c.beam != null) c.beam.targetAlpha = 0.7f;
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
            // LA FETE DU VAINQUEUR : elle vient flotter au-dessus de sa tete, et RIEN d'autre ne
            // la bouge. (10/10 -- Martin : "la couronne se retrouve je sais pas ou". Avant, la
            // ligne "reste a la hauteur de ton socle" s'appliquait encore a chaque image : la
            // Couronne etait tiree vers le Monument ET vers la tete, et flottait entre les deux.)
            if (showOff != null) { FollowWinner(); return; }
            // (04/10, en ligne) Chez un invite, la Couronne RECOPIE celle de l'hote (Mirror) : elle
            // ne decide de rien -- ni de tomber, ni de rentrer, ni de qui la prend.
            bool mirror = NetGame.IsClient;
            if (state == State.Carried)
            {
                if (Holder == null || Holder.Body == null) { if (!mirror) Drop(visual.position); return; }
                // Au-dessus de sa tete : tout le monde la voit briller.
                visual.position = Holder.Body.position + Vector3.up * 2.6f + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.05f;
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
            // (02/10) La colonne d'or plus franche : on la voit de toute l'ile (0,45 -> 0,7).
            if (beam != null && state != State.Delivered) { beam.targetAlpha = mine ? 0f : 0.7f; beam.fadeSpeed = mine ? 30f : 0.5f; }
            // (02/10) A terre, elle RESTE : plus de retour au sommet au bout de 20 s. Seul filet :
            // si par malheur elle est passee sous l'ile, elle rentre au socle.
            if (!mirror && state == State.Dropped && visual.position.y < Ground.FallLine)
                ReturnHome();
            // (01/10) Sur son socle comme a terre : on la prend EN PASSANT DESSUS.
            if (state == State.Dropped || state == State.OnPedestal) PickUpByTouch();
            visual.Rotate(0f, (state == State.Carried ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
            if (state != State.Carried) visual.position = new Vector3(visual.position.x, BaseHeight() + Mathf.Sin(Time.time * 1.6f) * 0.05f, visual.position.z);
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

        void FollowWinner()
        {
            if (hiddenForMe || !shownForWinner)
            {
                hiddenForMe = false;
                shownForWinner = true;
                Renderer[] all = visual.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < all.Length; i++) all[i].enabled = true;
                if (crystals != null) crystals.gameObject.SetActive(false);
                // Elle part d'ou elle est, mais jamais de plus de 12 m : sinon on la verrait
                // traverser l'ile (la danse est filmee de pres).
                Vector3 head = showOff.position + Vector3.up * 3.1f;
                if ((visual.position - head).magnitude > 12f) visual.position = head + Vector3.up * 6f;
            }
            float dt = Time.unscaledDeltaTime;
            Vector3 above = showOff.position + Vector3.up * (3.1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.12f);
            visual.position = Vector3.Lerp(visual.position, above, 1f - Mathf.Exp(-8f * dt));
            visual.Rotate(0f, 70f * dt, 0f, Space.World);
            if (beam != null) { beam.targetAlpha = 0f; beam.fadeSpeed = 3f; beam.source = visual.position; }
            if (glow != null) glow.intensity = 1.3f * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f));
        }

        bool shownForWinner;
        float groundY;
        bool hiddenForMe;
        Vector3 pedestal;


        /// <summary>
        /// SON PORTEUR EST TOMBE DANS LES NUAGES (02/10) : elle ne rentre plus au sommet --
        /// elle se pose la ou il a quitte le sol pour la derniere fois (le bord de l'ile, la
        /// rampe d'ou on l'a pousse). Tout le monde la voit (son repere) et court la chercher.
        /// </summary>
        public static void FellWith(Seeker s)
        {
            // (v37) La ou il est tombe : la terre la plus proche, sous lui (plus son dernier sol --
            // le sommet, s'il avait saute de la tour avec elle).
            if (s != null) FellWith(s, s.Body != null ? Below(s.Body.position) : LastGroundOf(s));
        }

        /// <summary>
        /// (v37) Ou retombe une Couronne lachee en "p" : le sol juste EN DESSOUS (l'ile, un ilot, la
        /// rampe en dessous) ; au-dessus du vide, la terre LA PLUS PROCHE a l'horizontale -- le bord
        /// de l'ile, ou le dessus d'un ilot. Jamais le sommet s'il n'est pas sous elle.
        /// </summary>
        public static Vector3 Below(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out hit, 400f, ~0, QueryTriggerInteraction.Ignore) && Reachable(hit.point))
                return hit.point;
            Vector3 flat = new Vector3(p.x, 0f, p.z);
            Vector3 best = Vector3.zero;
            float bestD = float.MaxValue;
            // L'ile : en allant de "p" vers son centre, le premier point de terre, trois metres a l'interieur.
            Vector3 toCentre = -flat.normalized;
            for (float d = 0f; d < flat.magnitude; d += 2f)
            {
                Vector3 q = flat + toCentre * d;
                if (!Ground.OnIsland(q.x, q.z)) continue;
                q += toCentre * 3f;
                Vector3 g = Ground.Place(q.x, q.z, 0f);
                if (Physics.Raycast(new Vector3(q.x, g.y + 40f, q.z), Vector3.down, out hit, 80f, ~0, QueryTriggerInteraction.Ignore)) g = hit.point;
                best = g;
                bestD = d;
                break;
            }
            // Les ilots : le bord du dessus le plus proche.
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Ground.Islet it = Ground.GetIslet(i);
                Vector3 c = new Vector3(it.Top.x, 0f, it.Top.z);
                Vector3 away = flat - c;
                float d = away.magnitude - it.Radius;
                if (d >= bestD) continue;
                Vector3 q = c + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * Mathf.Max(0f, Mathf.Min(away.magnitude, it.Radius - 1.5f));
                Vector3 g = new Vector3(q.x, it.Top.y, q.z);
                if (Physics.Raycast(g + Vector3.up * 6f, Vector3.down, out hit, 12f, ~0, QueryTriggerInteraction.Ignore)) g = hit.point;
                best = g;
                bestD = d;
            }
            return bestD < float.MaxValue ? best : Tower.CrownSpot;
        }

        /// <summary>Pareil, en disant ou etait son dernier sol (un invite le dit a l'hote).</summary>
        public static void FellWith(Seeker s, Vector3 ground)
        {
            if (Instance == null || Holder != s || s == null) return;
            if (NetGame.IsClient) { NetGame.AskCrown(NetGame.Ask.FellWith, s, ground); return; }
            Instance.Drop(ground, ground);
            Feed.CrownKnocked(s, null);
        }

        /// <summary>Le dernier sol d'un joueur (toi ou un bot) : la ou la Couronne doit rester s'il tombe.</summary>
        static Vector3 LastGroundOf(Seeker s)
        {
            IMover m = AbilityCaster.MoverOf(s);
            if (m != null) return m.LastGround;
            return s.Body != null ? s.Body.position : Tower.CrownSpot;
        }

        /// <summary>
        /// Un endroit ou l'on peut aller a pied (ou en planant) : l'ile (pas les toits, au-dessus
        /// de 30 m), la tour, le dessus d'un ilot. Sinon, une Couronne posee la resterait pour
        /// toujours -- maintenant qu'elle ne rentre plus au sommet.
        /// </summary>
        static bool Reachable(Vector3 p)
        {
            if (Tower.On(p)) return true;
            if (Ground.OnIsland(p.x, p.z)) return p.y > -2f && p.y < 30f;
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Ground.Islet it = Ground.GetIslet(i);
                if (new Vector2(p.x - it.Top.x, p.z - it.Top.z).magnitude <= it.Radius + 1f && Mathf.Abs(p.y - it.Top.y) < 3f) return true;
            }
            return false;
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
            // (11/10 -- Martin : "la couronne se TP sur un gars, il finit le truc en 10 secondes")
            // LA CAUSE : rien ne verifiait qu'on etait A COTE d'elle. Un bot qui "arrivait" au bout
            // de son chemin -- parfois loin d'elle -- la prenait d'ou il etait, et elle sautait
            // dans ses mains. Maintenant, il faut la toucher (un peu de marge pour le reseau).
            if (!Within(s, 1.5f)) return false;
            // (04/10, en ligne) Un invite DEMANDE a l'hote ; c'est sa reponse (Mirror) qui la lui donne.
            if (NetGame.IsClient) { if (!s.Remote) NetGame.AskCrown(NetGame.Ask.Take, s, s.Body.position); return false; }
            bool fromPedestal = state == State.OnPedestal;
            state = State.Carried;
            Holder = s;
            s.GripUsed = false;
            // (06/10) Pas d'immunite qui dure avec la Couronne (le Fantome) : 1,5 s au plus.
            s.GraceUntil = Mathf.Min(s.GraceUntil, Time.time + 1.5f);
            Sfx.Bell();
            if (s.IsPlayer) Stats.CrownsTaken++;
            if (fromPedestal) Sfx.Alarm();
            if (s.IsPlayer && Game.Hud != null) Game.Hud.ShowSplash("couronne", Gold, "LA COURONNE EST À TOI !");
            // (02/10 -- "quand il l'a, il ne sait meme pas qu'il l'a") : une fanfare, et la
            // camera s'ouvre d'un coup ; le cadre d'or reste tant qu'on la porte (Hud).
            if (s.IsPlayer) { Sfx.Discovery(); if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Kick(8f); }
            Feed.CrownTaken(s, fromPedestal);
            if (s.IsPlayer && Game.Hud != null) Game.Hud.Flash(new Color(1f, 0.8f, 0.35f, 0.7f));
            Highlights.CrownChanged(s);
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
            // Par terre, un peu devant : on la voit rouler.
            at = SafeSpot(at, fallback);
            // (05/10 -- Martin : "la couronne, des fois elle spawn je sais pas ou ; normalement
            // elle tombe a cote") : A COTE, au meme niveau. Avant, on cherchait le sol jusqu'a
            // 30 m plus bas : pousse au bord de la rampe, elle tombait sur la rampe d'en dessous,
            // dans la cour, introuvable. Desormais : a cote (4 m plus bas au plus), sinon sous
            // ses pieds, sinon son dernier sol.
            RaycastHit hit;
            float y;
            if (Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 5.5f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
            else if (Physics.Raycast(fallback + Vector3.up * 1.5f, Vector3.down, out hit, 5.5f, ~0, QueryTriggerInteraction.Ignore)) { at = fallback; y = hit.point.y; }
            else
            {
                // (13/10, v37 -- Martin : "quand tu perds la couronne en l'air, elle revient sur la
                // tour ; elle doit redescendre EN DESSOUS de nous, le plus pres possible") : en l'air,
                // elle TOMBE -- sur le sol juste en dessous, ou, au-dessus du vide, sur la terre la
                // plus proche (le bord de l'ile, un ilot). Avant : le dernier sol de son porteur --
                // le sommet de la tour, s'il en avait saute.
                at = Below(fallback);
                y = at.y;
            }
            // (02/10) Au-dessus du vide, sur un toit, hors d'atteinte : elle se pose la ou son
            // porteur a touche le sol pour la derniere fois (elle ne rentre plus au sommet).
            if (!Reachable(new Vector3(at.x, y, at.z)))
            {
                at = Below(new Vector3(at.x, y, at.z));
                y = at.y;
                if (!Reachable(at) && was != null)
                {
                    at = LastGroundOf(was);
                    y = Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : at.y;
                }
            }
            // Celui qui vient de la perdre ne la reprend pas tout de suite (trois secondes).
            if (was != null) was.CrownLockUntil = Time.time + LockSeconds;
            if (was != null && was.IsPlayer && Game.Hud != null) Game.Hud.CrownLost();
            groundY = y + 0.35f;
            // Le declencheur d'abord (la couronne visible est son enfant : le bouger
            // apres elle la decalerait d'autant), puis la couronne elle-meme.
            transform.position = new Vector3(at.x, y - 1.05f, at.z);
            visual.position = new Vector3(at.x, groundY, at.z);
            Sfx.ThudAt(visual.position);
            Ambiance.Burst(null, visual.position, Gold);
            // (02/10 -- "on ne voit pas tres bien quand on perd la Couronne") : une gerbe d'or
            // qui monte haut et un anneau au sol, la ou elle tombe ; le repere a l'ecran de
            // tout le monde grossit et bat trois secondes (Hud.DrawCrownMarker).
            DroppedAt = Time.time;
            Fx.Column(visual.position, Gold, 16f, 0.5f, 1.2f);
            Fx.Shock(visual.position, Gold, 4f, 0.45f);
        }

        /// <summary>Quand elle est tombee pour la derniere fois (le repere a l'ecran bat trois secondes).</summary>
        public static float DroppedAt = -99f;

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
        /// LA COURONNE GLISSE : son porteur tombe assomme (etourdi, ejecte). Elle reste la
        /// ou il a quitte le sol. (04/10 : plus quand il replie ses ailes ou sort d'une arbaleste.)
        /// </summary>
        public static void Slip(Seeker holder, Vector3 lastGround)
        {
            if (Instance == null || Holder != holder) return;
            if (NetGame.IsClient) { NetGame.AskCrown(NetGame.Ask.Slip, holder, lastGround); return; }
            // (v37) Assomme en plein vol : elle tombe sous lui (ou sur la terre la plus proche),
            // plus la ou il avait quitte le sol (le sommet, s'il avait saute de la tour).
            Vector3 spot = holder.Body != null ? Below(holder.Body.position) : lastGround;
            Instance.Drop(spot, spot);
            Feed.CrownSlipped(holder);
        }

        /// <summary>
        /// ON LA PREND EN PASSANT DESSUS (01/10 -- Martin : "quand on passe sur la couronne,
        /// ca nous la recupere") : a terre, en la touchant ; sur son socle, en montant sur
        /// les marches jusqu'a la colonne. Pas de touche a chercher en pleine bagarre.
        /// </summary>
        void PickUpByTouch()
        {
            if (Game.Season == null || !Game.Season.Running) return;
            bool onPedestal = state == State.OnPedestal;
            if (!onPedestal && state != State.Dropped) return;
            Seeker best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || s.Stunned || Time.time < s.CrownLockUntil) continue;
                // Chez un invite, seul SON joueur la touche (les autres, l'hote les voit).
                if (NetGame.IsClient && s.Remote) continue;
                float d;
                if (onPedestal)
                {
                    Vector3 p = s.Body.position;
                    d = new Vector2(p.x - pedestal.x, p.z - pedestal.z).magnitude;
                    if (d > TouchPedestal || p.y < pedestal.y - 0.5f || p.y > pedestal.y + 4f) continue;
                }
                else
                {
                    d = (s.Body.position + Vector3.up * 0.9f - visual.position).magnitude;
                    if (d > TouchGround) continue;
                }
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best != null) TryTakeFor(best);
        }

        /// <summary>A quelle distance on ramasse la Couronne en passant : a terre, et sur son socle (du centre).</summary>
        public const float TouchGround = 2.3f;

        /// <summary>(11/10) "s" est-il assez pres pour la toucher (marge "slack" en metres) ?</summary>
        bool Within(Seeker s, float slack)
        {
            Vector3 p = s.Body.position;
            if (state == State.OnPedestal)
                return new Vector2(p.x - pedestal.x, p.z - pedestal.z).magnitude <= TouchPedestal + slack && p.y > pedestal.y - 1.5f && p.y < pedestal.y + 5f;
            return (p + Vector3.up * 0.9f - visual.position).magnitude <= TouchGround + slack;
        }
        // (v37) 3,1 m : il suffit de poser le pied sur la premiere marche (2,6 m de rayon) -- les
        // bots qui butaient contre la marche ne l'atteignaient jamais.
        public const float TouchPedestal = 3.1f;

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
            if (NetGame.IsClient) return false;         // l'hote decide du vol (NetGame.RemoteHit)
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
            if (thief.IsPlayer)
            {
                Stats.CrownsStolen++;
                Stats.CrownsTaken++;
                Sfx.Discovery();
                if (Game.Hud != null) { Game.Hud.Flash(new Color(1f, 0.8f, 0.35f, 0.7f)); Game.Hud.ShowSplash("couronne", Gold, "TU AS VOLÉ LA COURONNE !"); }
            }
            if (victim.IsPlayer && Game.Hud != null) Game.Hud.CrownLost();
            // (07/10) PICKPOCKET : le voleur disparait deux secondes.
            if (thief.Has(Ability.Pickpocket)) { thief.HiddenUntil = Mathf.Max(thief.HiddenUntil, Time.time + 2f); Fx.Burst(thief.Body.position + Vector3.up, AbilityInfo.Tint(Ability.Pickpocket), 40, 5f, 0.3f, 0.8f, 0f, Vector3.zero, 0f); }
            Shouts.Stolen(thief, victim);
            Feed.CrownStolen(thief, victim);
            Highlights.CrownChanged(thief);
            return true;
        }

        public static void KnockOff(Seeker victim, Vector3 direction)
        {
            if (Instance == null || Holder != victim || victim.Body == null) return;
            if (NetGame.IsClient) { NetGame.AskCrown(NetGame.Ask.KnockOff, victim, direction); return; }
            Vector3 flat = new Vector3(direction.x, 0f, direction.z).normalized;
            Instance.Drop(victim.Body.position + flat * 2.2f, victim.Body.position);
        }

        // ================================================================== en ligne : le miroir

        /// <summary>Ou elle est posee (a terre, sur son socle, sur l'autel) : l'hote l'envoie.</summary>
        public Vector3 RestingPosition { get { return visual != null ? new Vector3(visual.position.x, BaseHeight(), visual.position.z) : transform.position; } }

        /// <summary>
        /// (04/10) CHEZ UN INVITE : la Couronne devient ce que l'hote dit -- qui la porte, ou elle
        /// est tombee. Avec ce qu'il faut de bruit et de lumiere quand ca change (la fanfare si
        /// c'est toi qui l'as, le cadre d'or qui s'eteint si on te l'a prise).
        /// </summary>
        public void Mirror(State st, Seeker holder, Vector3 at)
        {
            if (visual == null || showOff != null) return;
            Seeker was = Holder;
            if (st == State.Carried)
            {
                if (holder == null || holder == was) return;
                bool fromPedestal = state == State.OnPedestal;
                state = State.Carried;
                Holder = holder;
                Sfx.Bell();
                if (holder.IsPlayer)
                {
                    Stats.CrownsTaken++;
                    Sfx.Discovery();
                    if (Game.Hud != null)
                    {
                        Game.Hud.ShowSplash("couronne", Gold, was != null ? "TU AS VOLÉ LA COURONNE !" : "LA COURONNE EST À TOI !");
                        Game.Hud.Flash(new Color(1f, 0.8f, 0.35f, 0.7f));
                        if (Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Kick(8f);
                    }
                }
                else if (fromPedestal) Sfx.Alarm();
                if (was != null)
                {
                    if (was.IsPlayer) LostIt(was);
                    if (was.Body != null && holder.Body != null) Tether.Show(was.Body, holder.Body, Vector3.zero, 0.5f, Gold);
                    Feed.CrownStolen(holder, was);
                    Shouts.Stolen(holder, was);
                }
                else Feed.CrownTaken(holder, fromPedestal);
                Highlights.CrownChanged(holder);
                return;
            }
            Holder = null;
            if (was != null && was.IsPlayer) LostIt(was);
            if (st == State.Dropped)
            {
                bool fell = state == State.Carried || state != State.Dropped;
                state = State.Dropped;
                groundY = at.y;
                transform.position = new Vector3(at.x, at.y - 1.4f, at.z);
                visual.position = new Vector3(at.x, at.y, at.z);
                if (fell && was != null)
                {
                    DroppedAt = Time.time;
                    Sfx.ThudAt(visual.position);
                    Fx.Column(visual.position, Gold, 16f, 0.5f, 1.2f);
                    Fx.Shock(visual.position, Gold, 4f, 0.45f);
                }
            }
            else if (st == State.OnPedestal && state != State.OnPedestal)
            {
                state = State.OnPedestal;
                transform.position = pedestal;
                visual.position = home;
            }
            else if (st == State.Delivered && state != State.Delivered)
            {
                state = State.Delivered;
                deliveredY = at.y;
                transform.position = at - Vector3.up * 1.3f;
                visual.position = at;
                if (beam != null) { beam.color = Monument.Blue; beam.targetAlpha = 1f; }
            }
        }

        /// <summary>On te l'a prise (ou tu l'as lachee) : tu ne la reprends pas tout de suite.</summary>
        static void LostIt(Seeker me)
        {
            me.CrownLockUntil = Time.time + LockSeconds;
            if (Game.Hud != null) Game.Hud.CrownLost();
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return visual != null ? visual : transform; } }
        /// <summary>
        /// (05/10 -- Martin : "il est marque E, c'est chiant, et quand tu fais E des fois tu montes
        /// dans l'arbaleste") : plus de touche du tout. On la prend EN PASSANT DESSUS, point.
        /// </summary>
        public bool CanInteract { get { return false; } }
        public string Prompt { get { return state == State.OnPedestal ? "Prendre la Couronne" : "Ramasser la Couronne"; } }
        /// <summary>(01/10 -- "il ne faut pas appuyer longtemps, juste appuyer sur E") : un simple appui.</summary>
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            TryTakeFor(Game.Me);
        }
    }
}
