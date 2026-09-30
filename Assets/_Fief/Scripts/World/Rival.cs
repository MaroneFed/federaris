using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN BOT : un autre joueur, qui joue avec TES regles -- et qui, en Phase 3, cedera
    /// sa place a un vrai joueur en ligne (voir docs/RESEAU.md). On ne lui parle pas.
    ///
    /// Ce qu'il fait, comme toi (27/09 : sur l'ile flottante) :
    ///   - au debut, il passe par un SANCTUAIRE proche s'il n'a pas de don ;
    ///   - il MONTE LA TOUR : la rampe, les courants, les trous a sauter -- ou il saute
    ///     sur une ARBALESTE qui l'envoie haut sur la rampe ;
    ///   - au sommet il prend la Couronne et des AILES, et il se JETTE dans le vide pour
    ///     PLANER jusqu'a l'un des trois Monuments (le plus commode : pres, et que
    ///     personne ne garde) ; ou, depuis le sol, il se fait tirer
    ///     jusqu'a l'ilot par une arbaleste ;
    ///   - si un autre la tient, il le CHASSE (et le pousse : c'est ainsi qu'on la
    ///     VOLE) ; l'un d'eux va l'attendre au Monument ;
    ///   - il esquive les boulets de la rampe, il se sert de toutes ses capacites
    ///     (AbilityCaster.Cast, exactement comme toi).
    ///
    /// COMMENT IL PENSE. Toutes les 0,3 a 0,5 s il choisit un BUT et un CHEMIN de
    /// points. Chaque image, il marche vers le prochain point -- ou il vole.
    /// </summary>
    public class Rival : MonoBehaviour, IMover
    {
        public static readonly List<Rival> All = new List<Rival>();

        /// <summary>Vrai si un bot, au moins, te court apres.</summary>
        public static bool HuntingPlayer
        {
            get
            {
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].goal == Goal.Hunt && All[i].prey != null && All[i].prey.IsPlayer) return true;
                return false;
            }
        }

        enum Goal { Shrine, Raid, Grab, Deliver, Hunt, Fight, Guard, Ballista, Roam }

        [System.NonSerialized] public Seeker seeker;

        // --- caractere
        float boldness;         // envie d'aller vite, de prendre des risques (0-1)
        float temper;           // envie de se battre (0-1)
        float scoutUntil;

        // --- etat
        Goal goal = Goal.Roam;
        Vector3 target;
        float think;
        float work;
        Shrine shrine;
        Seeker prey;
        float preyTimer;
        Vector3 preyLast, preyVelocity;
        float preyLastAt = -1f;
        // Le chien de garde du chemin : s'il ne se rapproche plus de son but, il replanifie.
        float progressCheck;
        float progressBest = float.MaxValue;
        float noProgress;
        float castTimer;
        float barkTimer;
        float fellAt = -99f;
        System.Random rng;
        readonly List<Vector3> path = new List<Vector3>();

        // --- l'arbaleste visee
        Ballista ballista;
        Monument aimMonument;
        Vector3 ballistaShot;
        float ballistaChosen;
        bool mounted;

        // --- corps
        CharacterController body;
        float fallSpeed;
        Vector3 knock;
        float dashTime;
        Vector3 dashVelocity;
        float pullTime;
        Vector3 pullPoint;
        float pullSpeed;
        float stuck;
        float detourTimer;
        float detourSign = 1f;
        bool airJumped;
        bool gliding;
        float airspeed;
        bool ballistic;
        Vector3 flight;
        float launchAge;
        bool leaping;
        float leapStart;
        float launchedUntil;
        Vector3 lastGround;
        float airTop;
        Transform figure;
        CharacterRig rig;
        /// <summary>Son corps anime (la fete du vainqueur s'en sert).</summary>
        public CharacterRig Rig { get { return rig; } }
        Vector3 lastPosition;
        Light lantern;
        WingsOnBack wings;
        readonly Vector3[] trail = new Vector3[50];
        int trailAt;
        float trailTimer;

        // 27/09 : presque aussi vite que toi (10,8 m/s en courant) -- selon leur niveau.
        const float WalkSpeed = 6.2f;
        static float RunSpeed { get { return Match.BotLevel == 0 ? 9f : Match.BotLevel == 1 ? 10.2f : 10.7f; } }
        /// <summary>Le temps entre deux capacites, selon le niveau.</summary>
        static float Reflex { get { return Match.BotLevel == 0 ? 1.4f : Match.BotLevel == 1 ? 0.45f : 0.25f; } }

        /// <summary>Vrai pendant qu'il plane (le HUD, ses ailes s'en servent).</summary>
        public bool Gliding { get { return gliding; } }
        /// <summary>Vrai pendant un vol d'arbaleste (son carreau le suit).</summary>
        public bool Flying { get { return ballistic; } }

        // ================================================================== construction

        public static Rival Build(Transform parent, PlayerSlot slot, Vector3 spawn, int seed)
        {
            Seeker seeker = new Seeker(slot);
            Game.Seekers.Add(seeker);

            GameObject root = new GameObject("JOUEUR " + slot.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = spawn;
            seeker.Body = root.transform;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 52f;
            cc.stepOffset = 0.42f;

            Rival r = root.AddComponent<Rival>();
            r.seeker = seeker;
            r.body = cc;
            r.rng = new System.Random(seed);
            r.boldness = 0.3f + (float)r.rng.NextDouble() * 0.7f;
            r.temper = 0.3f + (float)r.rng.NextDouble() * 0.7f;
            r.scoutUntil = Mathf.Lerp(25f, 6f, r.boldness);
            r.lastPosition = spawn;
            r.lastGround = spawn;
            for (int i = 0; i < r.trail.Length; i++) r.trail[i] = spawn;

            Color colour = slot.Colour;
            CharacterRig rig = CharacterRig.Build(root.transform, colour, Palette.Shade(colour, 0.62f));
            rig.RunSpeed = RunSpeed;
            r.rig = rig;
            r.figure = rig.transform;
            r.lantern = PlayerLook.Dress(rig, root.transform, colour);
            r.wings = WingsOnBack.Attach(root.transform, seeker);
            GraceShell.Attach(root.transform, seeker);

            All.Add(r);
            return r;
        }

        void OnDestroy() { All.Remove(this); }

        /// <summary>Le bot de ce joueur, ou null (toi, ou un joueur en ligne).</summary>
        public static Rival Of(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].seeker == s) return All[i];
            return null;
        }

        /// <summary>La silhouette (ce qu'on voit), pour le recul d'un coup.</summary>
        public Transform Figure { get { return figure; } }

        // ================================================================== IMover

        public void Push(Vector3 velocity)
        {
            knock += new Vector3(velocity.x, 0f, velocity.z);
            if (velocity.y > 0f) fallSpeed = Mathf.Max(fallSpeed, velocity.y);
            // Lance par un courant : il monte droit, sans marcher, jusqu'a passer le trou.
            if (velocity.y > 20f) launchedUntil = Time.time + 0.9f;
            dashTime = 0f;
            pullTime = 0f;
        }

        public void Dash(Vector3 direction, float speed, float seconds)
        {
            dashVelocity = direction.normalized * speed;
            dashTime = seconds;
            if (fallSpeed < 1f) fallSpeed = 1f;
        }

        public void PullTo(Vector3 point, float speed)
        {
            pullPoint = point;
            pullSpeed = speed;
            pullTime = 1.4f;
        }

        /// <summary>Tire par une arbaleste : il suit sa courbe jusqu'a toucher quelque chose.</summary>
        Seeker diveTarget;
        float diveTime;

        public void Dive(Seeker target)
        {
            diveTarget = target;
            diveTime = Combat.DiveSeconds;
            gliding = false;
            ballistic = false;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
        }

        public void Launch(Vector3 velocity)
        {
            ballistic = true;
            launchAge = 0f;
            flight = new Vector3(velocity.x, 0f, velocity.z);
            fallSpeed = velocity.y;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
            airTop = transform.position.y;
            leaping = true;
            leapStart = Time.time;
            path.Clear();
        }

        public void Blink(Vector3 position) { Teleport(position); }

        public Vector3 LastGround { get { return lastGround; } }

        /// <summary>Oublier ou l'on etait (apres un respawn : le Rappel ne renvoie pas dans le vide).</summary>
        public void Forget()
        {
            for (int i = 0; i < trail.Length; i++) trail[i] = transform.position;
        }

        public Vector3 PastPosition(float seconds)
        {
            int back = Mathf.Clamp(Mathf.RoundToInt(seconds / 0.1f), 1, trail.Length - 1);
            return trail[((trailAt - back) % trail.Length + trail.Length) % trail.Length];
        }

        /// <summary>Deplace d'un coup.</summary>
        public void Teleport(Vector3 position)
        {
            bool was = body.enabled;
            body.enabled = false;
            transform.position = position;
            lastPosition = position;
            body.enabled = was && !mounted;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
            fallSpeed = 0f;
            ballistic = false;
            gliding = false;
            leaping = false;
            airTop = position.y;
            path.Clear();
            think = 0f;
        }

        /// <summary>L'arbaleste le prend en charge : il ne marche plus, elle le place.</summary>
        public void OnMounted()
        {
            mounted = true;
            body.enabled = false;
            path.Clear();
        }

        /// <summary>Il quitte l'arbaleste (en "at"), en tirant ou non.</summary>
        public void Dismounted(Vector3 at)
        {
            mounted = false;
            ballista = null;
            transform.position = at;
            lastPosition = at;
            body.enabled = true;
            think = 0f;
        }

        // ================================================================== boucle

        void Update()
        {
            Season season = Game.Season;
            if (season == null || !season.Running || Time.deltaTime <= 0f) return;
            float dt = Time.deltaTime;
            if (transform.position.y < Ground.FallLine) { Respawn.Of(seeker); return; }
            if (preyTimer > 0f) preyTimer -= dt;
            if (barkTimer > 0f) barkTimer -= dt;
            if (castTimer > 0f) castTimer -= dt;
            if (mounted) { Animate(dt); return; }

            think -= dt;
            if (think <= 0f) { think = Match.BotLevel == 2 ? 0.3f : 0.5f; Think(season); }
            if (castTimer <= 0f) { castTimer = Reflex; UseAbilities(); }

            Act(dt);
            Remember(dt);
            Animate(dt);
        }

        bool Airborne { get { return ballistic || gliding || !body.isGrounded && airTop - transform.position.y > 1.5f; } }

        /// <summary>Choisir un but, du plus urgent au moins urgent.</summary>
        void Think(Season season)
        {
            Goal was = goal;
            Vector3 me = transform.position;
            // En vol, on ne change pas d'avis : on vise ou l'on va.
            if (ballistic || gliding) return;
            // En route vers une arbaleste : on y va (sauf si elle est prise, ou si c'est long).
            if (goal == Goal.Ballista && ballista != null && ballista.Free && Time.time - ballistaChosen < 10f && !(seeker.CarriesCrown && Tower.On(me))) return;

            // Sur sa plateforme de depart (au debut, apres une chute) : il s'elance vers l'ile.
            if (Spawns.OnPad(me)) { LeaveThePad(was); return; }

            if (seeker.CarriesCrown && Monument.All.Count > 0) { PlanDeliver(was); return; }

            if (Crown.Where == Crown.State.Dropped)
            {
                Vector3 c = Crown.Position;
                if ((c - me).magnitude < 170f && ReachTo(c, Goal.Grab, was)) return;
            }

            Seeker holder = Crown.Holder;
            if (holder != null && holder != seeker && holder.Body != null)
            {
                prey = holder;
                preyTimer = 5f;
                Monument watched = Monument.Nearest(holder.Body.position);
                if (watched != null)
                {
                    Vector3 m = watched.transform.position;
                    Vector3 hp = holder.Body.position;
                    bool holderAway = !OnFoot(hp);      // il vole, ou il est sur un ilot
                    // L'un d'eux (le plus pres du Monument) va l'y attendre, par l'arbaleste
                    // ou en planant ; les autres le chassent.
                    // (02/10, gamer chiant n° 183-184) Quand il vole, ils se REPARTISSENT : le
                    // gardien du Monument le plus proche du porteur y va, les autres couvrent les
                    // deux autres Monuments (avant, tout le monde courait au meme, et le porteur
                    // n'avait qu'a en choisir un autre).
                    if (holderAway && Guardian(watched) != this && Monument.All.Count > 1)
                    {
                        Monument other = Monument.All[seeker.Index % Monument.All.Count];
                        if (other != null) m = other.transform.position;
                    }
                    if (holderAway || Guardian(watched) == this && Flat(hp - m).magnitude > 35f)
                    {
                        if (ReachTo(m + Flat(me - m).normalized * 5f, Goal.Guard, was)) return;
                    }
                }
                if (OnFoot(holder.Body.position)) { SetGoal(Goal.Hunt, holder.Body.position, was); return; }
                // Il vole et on ne peut pas le suivre : on monte chercher des ailes.
                SetGoal(Goal.Raid, Tower.CrownSpot, was);
                return;
            }
            if (prey != null && preyTimer > 0f && prey.Body != null && !prey.Hidden && seeker.CanShove && (prey.Body.position - me).magnitude < 25f && OnFoot(prey.Body.position))
            {
                SetGoal(Goal.Fight, prey.Body.position, was);
                return;
            }
            prey = null;

            // Le debut : un sanctuaire proche (sur l'ile), s'il n'a pas de don.
            if (season.Elapsed < scoutUntil && !seeker.HasGift)
            {
                if (shrine == null || shrine.Spent) shrine = NearestShrine(60f);
                if (shrine != null && OnFoot(shrine.transform.position)) { SetGoal(Goal.Shrine, shrine.transform.position, was); return; }
            }

            if (Crown.Where == Crown.State.OnPedestal)
            {
                // Un raccourci : une arbaleste qui l'envoie haut sur la rampe (pas les bots faciles).
                if (goal != Goal.Raid || path.Count > 20)
                {
                    if (!Tower.On(me) && Match.BotLevel > 0 && rng.NextDouble() < 0.35 + boldness * 0.4 && TryBallistaToRamp(was)) return;
                }
                SetGoal(Goal.Raid, Tower.CrownSpot, was);
                return;
            }

            Vector3 foot = Tower.FootNear(me);
            float t = Time.time * 0.1f + seeker.Index;
            SetGoal(Goal.Roam, foot + new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * 12f, was);
        }

        /// <summary>
        /// QUITTER SA PLATEFORME (28/09) : son arbaleste l'envoie devant la porte la plus
        /// proche ; si elle ne peut pas (ou s'il est un bot facile), il saute et plane.
        /// </summary>
        void LeaveThePad(Goal was)
        {
            Vector3 me = transform.position;
            Vector3 gate = Spawns.LandingOf(seeker.Index);
            // Son arbaleste : elle le pose sur le parvis devant sa porte.
            Ballista own = Spawns.BallistaOf(seeker.Index);
            if (own != null && own.Free && own.HasFixedTarget)
            {
                ballista = own;
                ballistaShot = own.FixedVelocity;
                ballistaChosen = Time.time;
                goal = Goal.Ballista;
                target = own.transform.position;
                if (was != Goal.Ballista || path.Count == 0) { path.Clear(); path.Add(target); }
                return;
            }
            goal = Goal.Raid;
            target = gate;
            Leap(gate);
        }

        /// <summary>
        /// PORTER LA COURONNE AU MONUMENT (il est sur un ilot) : sur son ilot, il marche
        /// dans le cercle ; au sommet (des ailes), il saute et plane ; au sol, une
        /// arbaleste l'y envoie -- sinon il monte a la tour prendre des ailes.
        /// </summary>
        void PlanDeliver(Goal was)
        {
            Vector3 me = transform.position;
            // Deja sur l'ilot d'un Monument : c'est celui-la.
            for (int i = 0; i < Monument.All.Count; i++)
                if (Monument.All[i] != null && IsletAt(me) >= 0 && IsletAt(me) == Monument.All[i].Islet) aimMonument = Monument.All[i];
            if (aimMonument == null || IsletAt(me) < 0 || IsletAt(me) != aimMonument.Islet) aimMonument = ChooseMonument();
            if (aimMonument == null) return;
            Vector3 m = aimMonument.transform.position;
            if (IsletAt(me) == aimMonument.Islet) { SetGoal(Goal.Deliver, m, was); return; }
            // (30/09 : la Couronne est lourde.) Il ne saute que s'il atteint le Monument en
            // planant -- ou, a defaut, le courant d'air sur le chemin, pour y remonter.
            if (seeker.CanGlide && (Tower.Summit(me) || Tower.On(me) && me.y > m.y + 25f))
            {
                Thermal lift = Thermal.Nearest(m);
                bool direct = Wings.CanReach(me, m, seeker);
                if (direct || lift != null && Wings.CanReach(me, lift.transform.position + Vector3.up * 6f, seeker))
                {
                    goal = Goal.Deliver;
                    target = m;
                    Leap(direct || lift == null ? m : lift.transform.position);
                    return;
                }
            }
            if ((Ground.OnIsland(me.x, me.z) || IsletAt(me) >= 0) && !Tower.On(me))
            {
                if (TryBallistaTo(m, 6f, Goal.Deliver, was)) return;
                // Trop loin pour cette arbaleste : un autre Monument, peut-etre.
                for (int i = 0; i < Monument.All.Count; i++)
                {
                    Monument other = Monument.All[i];
                    if (other == null || other == aimMonument) continue;
                    if (TryBallistaTo(other.transform.position, 6f, Goal.Deliver, was)) { aimMonument = other; return; }
                }
            }
            // Sinon : au sommet, chercher des ailes.
            SetGoal(Goal.Deliver, Tower.CrownSpot, was);
            PlanPath(Tower.CrownSpot);
        }

        /// <summary>
        /// Aller jusqu'a "to" : a pied s'il est sur l'ile ou la tour, en planant si on a
        /// des ailes et qu'on est haut, par l'arbaleste sinon. Faux si impossible.
        /// </summary>
        bool ReachTo(Vector3 to, Goal g, Goal was)
        {
            Vector3 me = transform.position;
            if (OnFoot(to) && OnFoot(me)) { SetGoal(g, to, was); return true; }
            if (IsletAt(me) >= 0 && IsletAt(me) == IsletAt(to)) { SetGoal(g, to, was); return true; }
            if (seeker.CanGlide && Tower.On(me) && me.y > to.y + 20f)
            {
                goal = g;
                target = to;
                Leap(to);
                return true;
            }
            if ((Ground.OnIsland(me.x, me.z) || IsletAt(me) >= 0) && !Tower.On(me) && TryBallistaTo(to, 7f, g, was)) return true;
            return false;
        }

        /// <summary>Viser "to" avec l'arbaleste libre la plus proche (a 70 m). Vrai si un tir y mene.</summary>
        bool TryBallistaTo(Vector3 to, float tolerance, Goal after, Goal was)
        {
            if (Match.BotLevel == 0 && after != Goal.Deliver) return false;
            Ballista b = Ballista.NearestFree(transform.position, 70f);
            if (b == null) return false;
            Vector3 aim = to + Vector3.up * 1f;
            Vector3 v;
            if (!Ballista.Solve(b.Seat, aim, true, out v) || !b.Lands(v, to, tolerance))
            {
                if (!Ballista.Solve(b.Seat, aim, false, out v) || !b.Lands(v, to, tolerance)) return false;
            }
            ballista = b;
            ballistaShot = v;
            ballistaChosen = Time.time;
            goal = Goal.Ballista;
            target = b.transform.position;
            if (was != Goal.Ballista || path.Count == 0) PlanPath(target);
            return true;
        }

        /// <summary>Un raccourci vers la rampe : un point du tour le plus haut qu'une arbaleste proche atteint.</summary>
        bool TryBallistaToRamp(Goal was)
        {
            // (29/09 : le sceau de la citadelle arrete les tirs -- plus de raccourci par la rampe.)
            return false;
        }

        void SetGoal(Goal g, Vector3 at, Goal was)
        {
            bool fresh = g != was || (at - target).sqrMagnitude > 9f || path.Count == 0 && (at - transform.position).magnitude > 6f;
            goal = g;
            target = at;
            if (fresh) { PlanPath(at); progressBest = float.MaxValue; noProgress = 0f; }
        }

        /// <summary>
        /// OU POSER LA COURONNE (28/09 : trois Monuments) : le plus commode -- pres de
        /// lui, sans personne qui l'y attende. Il garde son idee, sauf si c'est bien mieux ailleurs.
        /// </summary>
        Monument ChooseMonument()
        {
            Vector3 me = transform.position;
            Monument best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < Monument.All.Count; i++)
            {
                Monument mo = Monument.All[i];
                if (mo == null) continue;
                Vector3 at = mo.transform.position;
                float score = Flat(at - me).magnitude;
                for (int k = 0; k < Game.Seekers.Count; k++)
                {
                    Seeker o = Game.Seekers[k];
                    if (o == seeker || o.Body == null) continue;
                    if (Flat(o.Body.position - at).magnitude < 22f && Mathf.Abs(o.Body.position.y - at.y) < 10f) score += 70f;
                }
                if (mo == aimMonument) score -= 30f;
                if (score < bestScore) { bestScore = score; best = mo; }
            }
            return best;
        }

        /// <summary>Le bot qui garde "m" pendant que les autres chassent : le plus proche (null s'ils sont moins de deux).</summary>
        static Rival Guardian(Monument m)
        {
            if (m == null || Match.BotLevel == 0) return null;
            Rival best = null;
            float bestD = float.MaxValue;
            int free = 0;
            for (int i = 0; i < All.Count; i++)
            {
                Rival r = All[i];
                if (r == null || r.seeker.CarriesCrown) continue;
                free++;
                float d = Flat(r.transform.position - m.transform.position).magnitude;
                if (d < bestD) { bestD = d; best = r; }
            }
            return free >= 2 ? best : null;
        }

        Shrine NearestShrine(float range)
        {
            Shrine best = null;
            float bestD = range;
            for (int i = 0; i < Shrine.All.Count; i++)
            {
                Shrine s = Shrine.All[i];
                if (s == null || s.Spent) continue;
                float d = Flat(s.transform.position - transform.position).magnitude;
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        // ================================================================== ou l'on est

        /// <summary>Vrai si ce point est accessible a pied depuis l'ile : l'ile elle-meme ou la tour.</summary>
        static bool OnFoot(Vector3 p)
        {
            return Tower.On(p) || Ground.OnIsland(p.x, p.z) && p.y > -2f && p.y < 30f;
        }

        /// <summary>L'ilot sur lequel se trouve ce point (-1 : aucun).</summary>
        static int IsletAt(Vector3 p)
        {
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Ground.Islet it = Ground.GetIslet(i);
                if (Flat(p - it.Top).magnitude <= it.Radius + 1.5f && Mathf.Abs(p.y - it.Top.y) < 4f) return i;
            }
            return -1;
        }

        // ================================================================== les chemins

        /// <summary>
        /// Le chemin vers "to" : redescendre la rampe s'il est sur la tour (et que "to"
        /// n'y est pas), sortir ou entrer par la porte la plus proche, puis monter la
        /// rampe jusqu'a la hauteur voulue (par les courants s'il sait).
        /// </summary>
        void PlanPath(Vector3 to)
        {
            path.Clear();
            leaping = false;
            Vector3 from = transform.position;
            bool fromTower = Tower.On(from), toTower = Tower.On(to);
            if (fromTower && toTower)
            {
                path.AddRange(Climb(Tower.RampOf(from), Tower.Progress(from), Tower.Progress(to)));
                return;
            }
            if (fromTower)
            {
                int down = Tower.RampOf(from);
                path.AddRange(Tower.Path(down, Tower.Progress(from), 0f));
                path.Add(Tower.FootOf(down));
                from = Tower.FootOf(down);
            }
            bool fromIn = Castle.Inside(from), toIn = Castle.Inside(to);
            if (fromIn && !toIn)
            {
                Vector3[] exit = Castle.EntryFrom(to);
                for (int i = exit.Length - 1; i >= 0; i--) path.Add(exit[i]);
                // Puis le couloir de la porte, a l'envers (28/09).
                path.AddRange(Course.Exit(to));
            }
            else if (!fromIn && toIn)
            {
                // Le couloir de la porte : ses chicanes, ses moulinets (28/09).
                path.AddRange(Course.Route(from));
                path.AddRange(Castle.EntryFrom(from));
            }
            if (toTower)
            {
                // La rampe qui fait face a sa porte (29/09 : quatre rampes, une par porte).
                int up = Tower.RampFacing(path.Count > 0 ? path[path.Count - 1] : from);
                path.Add(Tower.FootOf(up));
                path.AddRange(Climb(up, 0f, Tower.Progress(to)));
            }
        }

        /// <summary>
        /// Se jeter dans le vide vers "toward" (il a des ailes) : au bord du sommet ou de
        /// la rampe, puis un pas de plus. En l'air, il plane vers sa cible.
        /// </summary>
        void Leap(Vector3 toward)
        {
            path.Clear();
            Vector3 me = transform.position;
            Vector3 dir = Flat(toward - me);
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            if (Spawns.OnPad(me))
            {
                // Du bord de sa plateforme, vers la cible -- un peu de biais, pour
                // contourner son arbaleste.
                Vector3 centre = Spawns.PadOf(seeker.Index);
                Vector3 slant = Quaternion.Euler(0f, 45f, 0f) * dir;
                Vector3 edge = centre + slant * (Spawns.PadRadius - 0.4f);
                edge.y = centre.y;
                path.Add(edge);
                path.Add(edge + slant * 8f);
            }
            else if (Tower.Summit(me))
            {
                Vector3 edge = dir * (Tower.Radius - 0.6f);
                edge.y = Tower.Height;
                path.Add(edge);
                path.Add(edge + dir * 8f);
            }
            else
            {
                Vector3 radial = Flat(me).normalized;
                Vector3 edge = me + radial * (Tower.OuterRadius - Flat(me).magnitude + 0.6f);
                path.Add(edge);
                path.Add(edge + radial * 8f);
            }
            leaping = true;
            leapStart = Time.time;
        }

        /// <summary>
        /// Monter (ou descendre) sa rampe de "from" a "to" (0-1), a pied.
        /// </summary>
        List<Vector3> Climb(int ramp, float from, float to)
        {
            // (29/09 : plus de courants -- on monte a pied, sa rampe.)
            List<Vector3> list = new List<Vector3>();
            list.AddRange(Tower.Path(ramp, from, to));
            return list;
        }

        /// <summary>La prochaine etape : le premier point du chemin, sinon la cible.</summary>
        Vector3 Waypoint()
        {
            while (path.Count > 0)
            {
                Vector3 p = path[0];
                Vector3 d = p - transform.position;
                bool sameLevel = Mathf.Abs(d.y) < 2.5f;
                // Loin de son chemin en hauteur (pousse de la rampe, lance par un courant) :
                // il en refait un depuis la ou il est.
                if (!leaping && Mathf.Abs(d.y) > 6f && Tower.On(p) && body.isGrounded) { PlanPath(target); if (path.Count == 0) break; p = path[0]; d = p - transform.position; sameLevel = Mathf.Abs(d.y) < 2.5f; }
                d.y = 0f;
                if (d.magnitude < 1.5f && (sameLevel || leaping)) { path.RemoveAt(0); continue; }
                return p;
            }
            return target;
        }

        // ================================================================== agir

        void Act(float dt)
        {
            // LA PROIE (01/10 -- "qu'ils soient un minimum intelligents") : il ne court pas la
            // ou elle EST, mais la ou elle VA (sa vitesse, une demi-seconde en avance).
            if (prey != null && prey.Body != null)
            {
                Vector3 pp = prey.Body.position;
                if (preyLastAt > 0f && Time.time - preyLastAt < 0.5f && dt > 0f)
                    preyVelocity = Vector3.Lerp(preyVelocity, Flat(pp - preyLast) / Mathf.Max(dt, 0.001f), 1f - Mathf.Exp(-6f * dt));
                preyLast = pp;
                preyLastAt = Time.time;
            }
            if ((goal == Goal.Hunt || goal == Goal.Fight) && prey != null && prey.Body != null)
            {
                Vector3 lead = Vector3.ClampMagnitude(preyVelocity, 14f) * (Match.BotLevel == 0 ? 0f : 0.45f);
                Vector3 aimAt = prey.Body.position + lead;
                target = OnFoot(aimAt) ? aimAt : prey.Body.position;
            }
            if (goal == Goal.Grab) target = Crown.Position;
            if (goal == Goal.Deliver && aimMonument != null && (gliding || ballistic || IsletAt(transform.position) >= 0))
                target = aimMonument.transform.position;
            Vector3 step = Waypoint();
            float distance = Flat(target - transform.position).magnitude;
            float dy = Mathf.Abs(target.y - transform.position.y);
            // (La Couronne sur son socle : il monte sur les marches -- elle se prend en passant.)
            float reach = goal == Goal.Deliver ? 2.5f : goal == Goal.Ballista ? 1.6f
                        : goal == Goal.Raid && Crown.Where == Crown.State.OnPedestal ? 1.6f : 1.8f;
            bool arrived = path.Count == 0 && distance <= reach && dy < 2.5f;

            // La poussee : des qu'il est a portee de sa proie (y compris en l'air) -- pas sur
            // une proie protegee (le coup ne ferait rien, et il perdrait sa recharge).
            if ((goal == Goal.Hunt || goal == Goal.Fight || goal == Goal.Guard) && prey != null && prey.Body != null && seeker.CanShove && !prey.Graced
                && Time.time >= seeker.ShoveReadyAt && (prey.Body.position - transform.position).magnitude < 2.9f)
            {
                seeker.ShoveReadyAt = Time.time + Seeker.ShoveCooldown * (seeker.Has(Ability.Poigne) ? 0.6f : 1f) * (Match.BotLevel == 0 ? 2f : Match.BotLevel == 1 ? 1.3f : 1.05f);
                if (rig != null) rig.PlaySwing();
                Combat.Shove(seeker, prey.Body.position - transform.position);
                if (goal == Goal.Fight && rng.NextDouble() < 0.4) preyTimer = 0f;
            }
            // EN MONTANT, IL SE BAT (29/09 -- Martin : "faut qu'il y ait du combat, j'arrive a
            // monter facilement") : qui passe a portee dans la citadelle ou sur la rampe se
            // fait pousser -- toi le premier.
            // (02/10 -- "les bots n'arrivent pas a monter", "avec tout le monde qui pousse, le
            // jeu n'est pas fluide") : une poussee envoie a 15 m -- sur une rampe sans parapet,
            // c'est le vide. Ils s'y jetaient les uns les autres et personne n'arrivait en
            // haut. Desormais, SUR LA RAMPE, un bot ne pousse jamais un autre bot (sauf le
            // porteur), et toi rarement ; au SOMMET et dans la cour, on se bat comme avant.
            else if ((goal == Goal.Raid || goal == Goal.Grab || goal == Goal.Roam) && Match.BotLevel > 0 && seeker.CanShove
                     && Time.time >= seeker.ShoveReadyAt && Castle.Inside(transform.position))
            {
                Seeker foe = NearestFoe(2.7f);
                bool onRamp = Tower.On(transform.position) && !Tower.Summit(transform.position);
                if (foe != null && onRamp && !foe.IsPlayer && !foe.CarriesCrown) foe = null;
                if (foe != null)
                {
                    seeker.ShoveReadyAt = Time.time + Seeker.ShoveCooldown * (Match.BotLevel == 1 ? 1.6f : 1.15f);
                    // (01/10 : la poussee projette loin maintenant -- ils la gardent pour les bons moments.)
                    double chance = Match.BotLevel == 1 ? 0.3 + temper * 0.35 : 0.45 + temper * 0.4;
                    if (onRamp && !foe.CarriesCrown) chance *= 0.35;
                    if (rng.NextDouble() < chance)
                    {
                        if (rig != null) rig.PlaySwing();
                        Combat.Shove(seeker, foe.Body.position - transform.position);
                        // Il se souvient de toi : il te cherche un moment.
                        if (rng.NextDouble() < temper) { prey = foe; preyTimer = 4f; }
                    }
                }
            }

            float speed = (goal == Goal.Roam ? WalkSpeed : RunSpeed) * seeker.SpeedFactor;
            if (!arrived)
            {
                work = 0f;
                WatchProgress(step, dt);
                Walk(step, speed, dt);
                return;
            }
            noProgress = 0f;
            Walk(transform.position, 0f, dt);

            switch (goal)
            {
                case Goal.Deliver:
                    // (Le Monument le prend tout seul des qu'il entre dans le cercle.)
                    if (aimMonument != null && aimMonument.TryDeliver(seeker)) Bark("Victoire !");
                    think = 0f;
                    break;
                case Goal.Raid:
                case Goal.Grab:
                    // (01/10 : un simple appui, comme toi.)
                    if (Crown.Instance != null && Crown.Instance.TryTakeFor(seeker)) Bark("À moi !");
                    think = 0f;
                    break;
                case Goal.Shrine:
                    work += dt;
                    if (work < 0.25f) break;
                    work = 0f;
                    if (shrine != null) shrine.TryTakeFor(seeker);
                    shrine = null;
                    think = 0f;
                    break;
                case Goal.Ballista:
                    if (ballista != null && ballista.MountAndAim(seeker, ballistaShot)) OnMounted();
                    else { ballista = null; think = 0f; }
                    break;
                default:
                    think = 0f;
                    break;
            }
        }

        /// <summary>
        /// LE CHIEN DE GARDE (01/10 -- "les bots deconnent complet") : s'il ne se rapproche
        /// plus de son but depuis cinq secondes (coince contre un mur, pris dans un coin, un
        /// chemin perime), il en refait un, part de biais et saute. Avant, il pouvait pousser
        /// contre la meme pierre jusqu'a la fin de la manche.
        /// </summary>
        void WatchProgress(Vector3 step, float dt)
        {
            if (!body.enabled || !body.isGrounded || gliding || ballistic || leaping) { noProgress = 0f; return; }
            float remaining = path.Count * 1000f + Flat(step - transform.position).magnitude;
            if (remaining < progressBest - 1f) { progressBest = remaining; noProgress = 0f; return; }
            noProgress += dt;
            if (noProgress < 5f) return;
            noProgress = 0f;
            progressBest = float.MaxValue;
            PlanPath(target);
            detourTimer = 1.2f;
            detourSign = rng.NextDouble() < 0.5 ? -1f : 1f;
            fallSpeed = 7.5f;
        }

        // ================================================================== les capacites

        /// <summary>
        /// Ses capacites, au bon moment -- comme un joueur qui sait ce qu'il fait. Il
        /// passe par AbilityCaster.Cast, exactement comme toi.
        /// </summary>
        void UseAbilities()
        {
            if (seeker.Stunned) return;
            List<Ability> list = new List<Ability>();
            if (seeker.HasActive) list.Add(seeker.CurrentActive);
            Vector3 me = transform.position;
            Vector3 eye = me + Vector3.up * 1.6f;
            Seeker holder = Crown.Holder;
            bool carrying = seeker.CarriesCrown;
            Vector3 toWaypoint = Waypoint() - me;
            float farToGo = Flat(target - me).magnitude;
            int near = 0;
            for (int i = 0; i < Game.Seekers.Count; i++)
                if (Game.Seekers[i] != seeker && Game.Seekers[i].Body != null && (Game.Seekers[i].Body.position - me).magnitude < 5.5f) near++;

            for (int i = 0; i < list.Count; i++)
            {
                Ability a = list[i];
                if (AbilityCaster.WhyNot(seeker, a) != null) continue;
                Vector3 aim = Vector3.zero;
                bool go = false;
                Vector3 toPrey = prey != null && prey.Body != null ? prey.Body.position + Vector3.up - eye : Vector3.zero;
                float preyD = prey != null && prey.Body != null ? toPrey.magnitude : 999f;
                switch (a)
                {
                    case Ability.Ruee:
                    case Ability.Clignement:
                    {
                        // Pour rattraper, ou pour fuir -- jamais sur la rampe, jamais vers le bord de l'ile.
                        Vector3 land = me + Flat(toWaypoint).normalized * 9f;
                        go = !Tower.On(me) && body.isGrounded && Ground.OnIsland(land.x, land.z) && !gliding && !ballistic
                             && farToGo > 14f && (goal == Goal.Hunt || goal == Goal.Grab || goal == Goal.Deliver || goal == Goal.Raid);
                        aim = Flat(toWaypoint);
                        break;
                    }
                    case Ability.Grappin:
                        go = prey != null && preyD > 9f && preyD < AbilityCaster.GrappinRange - 4f && goal == Goal.Hunt;
                        aim = toPrey;
                        break;
                    case Ability.Crochet:
                        go = prey != null && prey.CarriesCrown && preyD > 5f && preyD < AbilityCaster.CrochetRange - 2f;
                        aim = toPrey;
                        break;
                    case Ability.Souffle:
                        // La vague porte a 150 m : il la lache sur le porteur, meme loin (meme en vol).
                        go = prey != null && preyD < 90f && (prey.CarriesCrown || goal == Goal.Fight && preyD < 15f);
                        aim = toPrey;
                        break;
                    case Ability.Gel:
                        go = prey != null && prey.CarriesCrown && preyD > 6f && preyD < 24f;
                        aim = toPrey;
                        break;
                    case Ability.Onde:
                        go = near > 0 && (carrying || holder != null && holder.Body != null && (holder.Body.position - me).magnitude < AbilityCaster.OndeRadius - 1f || near >= 2);
                        break;
                    case Ability.Bond:
                        go = prey != null && prey.Body.position.y - me.y > 4f && Flat(toPrey).magnitude < 8f;
                        aim = Flat(toPrey);
                        break;
                    case Ability.Echange:
                        go = prey != null && prey.CarriesCrown && Monument.All.Count > 0 && preyD > 12f && preyD < 32f
                             && Monument.NearestDistance(prey.Body.position) < Monument.NearestDistance(me) - 15f && preyD < AbilityCaster.EchangeRange;
                        aim = toPrey;
                        break;
                    case Ability.Voile:
                        go = carrying && (Eye.ChargingAt(seeker) || Chasers(12f) > 0) || Eye.ChargingAt(seeker);
                        break;
                    case Ability.Nuee:
                        go = Eye.ChargingAt(seeker) || carrying && Chasers(8f) > 0;
                        break;
                    case Ability.Mur:
                        go = carrying && Chasers(10f) > 0 && !Tower.On(me) && body.isGrounded;
                        aim = -transform.forward;
                        break;
                    case Ability.Mine:
                        go = carrying && Chasers(18f) > 0 && body.isGrounded || goal == Goal.Roam && rng.NextDouble() < 0.05;
                        break;
                    case Ability.Rappel:
                        go = Time.time - fellAt < 3.5f && (goal == Goal.Raid || carrying);
                        break;
                }
                if (!go) continue;
                if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
                if (AbilityCaster.Cast(seeker, a, eye, aim.normalized))
                {
                    if (rig != null) rig.PlaySwing();
                    castTimer = 1f;
                    return;
                }
            }
        }

        /// <summary>Combien d'autres joueurs a moins de "metres".</summary>
        int Chasers(float metres)
        {
            int n = 0;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s != seeker && s.Body != null && (s.Body.position - transform.position).magnitude < metres) n++;
            }
            return n;
        }

        // ================================================================== les coups

        /// <summary>On vient de le projeter. S'il peut, il se retourne contre celui qui l'a fait.</summary>
        public void OnHit(Seeker by)
        {
            if (by == null) { Bark("Aïe !"); return; }
            if (!seeker.CarriesCrown && rng.NextDouble() < 0.35 + temper * 0.5)
            {
                prey = by;
                preyTimer = 5f + temper * 6f;
                think = 0f;
            }
            Bark("Tu vas le regretter !");
        }

        // ================================================================== marcher, voler

        float hazardWait;

        void Walk(Vector3 destination, float speed, float dt)
        {
            if (!body.enabled) body.enabled = true;
            Vector3 to = Flat(destination - transform.position);
            float d = to.magnitude;
            Vector3 dir = d > 0.05f ? to / d : transform.forward;
            if (d < 0.05f || Time.time < launchedUntil) speed = 0f;
            if (detourTimer > 0f)
            {
                detourTimer -= dt;
                dir = Quaternion.Euler(0f, 70f * detourSign, 0f) * dir;
            }
            // Un boulet arrive dans son couloir : il passe de l'autre cote de la rampe.
            float boulderLane;
            if (Tower.On(transform.position) && Match.BotLevel > 0 && BoulderChute.Threat(transform.position, 16f, out boulderLane))
            {
                Vector3 radial = Flat(transform.position).normalized;
                float lane = Flat(transform.position).magnitude - Tower.Centre;
                float want = boulderLane > 0f ? -2f : 2f;
                dir = (dir + radial * Mathf.Clamp(want - lane, -1f, 1f) * 1.4f).normalized;
            }
            // LES OBSTACLES QU'ON ATTEND (02/10 -- "la tour est trop compliquee pour les
            // bots") : avant de faire un pas, il demande au radar (Hazards) si un pendule,
            // un belier, une herse ou un marteau va frapper la. Si oui, il ATTEND son tour
            // (3,5 s au plus), comme un joueur qui regarde le pendule passer. S'il est deja
            // dans la zone, il file. Les faciles regardent moins loin (ils se font avoir).
            if (speed > 0f && body.isGrounded && !leaping && !seeker.Tumbling && Time.time >= launchedUntil)
            {
                float look = Match.BotLevel == 0 ? 0.35f : Match.BotLevel == 1 ? 0.65f : 0.8f;
                if (Hazards.Danger(transform.position + dir * 1.9f, look) && !Hazards.Danger(transform.position, 0.2f))
                {
                    hazardWait += dt;
                    if (hazardWait < 3.5f) speed = 0f;
                }
                else hazardWait = 0f;
            }
            // UNE GARGOUILLE LE VISE (02/10) : dans la derniere demi-seconde, quand elle ne
            // suit plus, il fonce (et s'ecarte du point vise) -- comme on apprend a le faire.
            if (Match.BotLevel > 0 && d >= 0.05f && Time.time >= launchedUntil && Eye.LockedOn(seeker))
            {
                speed = Mathf.Max(speed, RunSpeed) * 1.25f;
                if (Tower.On(transform.position))
                {
                    Vector3 inward = -Flat(transform.position).normalized;
                    dir = (dir + inward * 0.5f).normalized;
                }
            }
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-Combat.KnockDrag(seeker) * dt));
            Vector3 extra = Vector3.zero;
            if (dashTime > 0f) { dashTime -= dt; extra += dashVelocity; }
            if (pullTime > 0f)
            {
                pullTime -= dt;
                Vector3 p = pullPoint - transform.position;
                if (p.magnitude < 1.6f) { pullTime = 0f; knock += Flat(p).normalized * 7f; fallSpeed = Mathf.Max(fallSpeed, 4f); }
                else { extra += p.normalized * pullSpeed; fallSpeed = Mathf.Max(fallSpeed, p.normalized.y * pullSpeed); }
            }

            bool grounded = body.isGrounded;
            launchAge += dt;
            if (ballistic && grounded && launchAge > 0.2f) ballistic = false;
            Wings.Tick(seeker, grounded);
            if (grounded || !seeker.CanGlide) gliding = false;
            Vector3 walk = dir * speed;
            // Ejecte : il ne remonte pas l'elan a la marche (comme toi, presque plus de controle).
            if (seeker.Tumbling) walk *= 0.15f;
            if (grounded)
            {
                if (airTop - transform.position.y > 4f) Land(airTop - transform.position.y);
                airJumped = false;
                lastGround = transform.position;
                airTop = transform.position.y;
                if (fallSpeed <= 0f) fallSpeed = -1f;
                if (leaping && Time.time - leapStart > 0.6f && !Tower.Summit(transform.position))
                {
                    // Pose (ou bloque au bord depuis trop longtemps) : fin du saut.
                    if (Time.time - leapStart > 4f || !Tower.On(transform.position)) { leaping = false; think = 0f; }
                }
            }
            else
            {
                airTop = Mathf.Max(airTop, transform.position.y);
                fallSpeed -= 22f * dt;
                // LE VOL PLANE (28/09) : comme toi, ses ailes s'ouvrent seules au-dessus du vide.
                if (!gliding && seeker.CanGlide && fallSpeed < -6f && (!ballistic || launchAge > 3.6f) && Wings.VoidBelow(transform.position, ballistic ? 45f : Wings.OpenAbove))
                {
                    gliding = true;
                    airspeed = Wings.OpeningSpeed(Flat(flight) + Vector3.up * fallSpeed);
                }
                if (gliding)
                {
                    ballistic = false;
                    Vector3 aimAt = goal == Goal.Hunt && prey != null && prey.Body != null ? prey.Body.position : target;
                    Vector3 look = Wings.LookFor(transform.position, aimAt, seeker);
                    // Trop bas pour y arriver : il va chercher un courant d'air et tourne dedans.
                    // (01/10 : quel que soit son but -- pousse hors de l'ile, il ne se laisse plus
                    // tomber dans les nuages s'il y a un courant a portee.)
                    Vector3 here = transform.position;
                    bool overLand = Ground.OnIsland(here.x, here.z) && here.y > -5f;
                    if ((goal == Goal.Deliver || goal == Goal.Hunt || goal == Goal.Guard || !overLand) && !Wings.CanReach(here, aimAt, seeker))
                    {
                        Thermal t = Thermal.Nearest(transform.position);
                        if (t != null)
                        {
                            Vector3 c = t.transform.position;
                            Vector3 off = Flat(transform.position - c);
                            if (off.magnitude < Thermal.Radius * 0.8f)
                            {
                                Vector3 around = new Vector3(-off.z, 0f, off.x).normalized;
                                look = (around - off.normalized * 0.3f).normalized + Vector3.up * 0.05f;
                            }
                            else if (off.magnitude < 140f) look = Flat(c - transform.position).normalized;
                        }
                    }
                    Vector3 v = Wings.Fly(ref airspeed, look, 0f, false, seeker, dt);
                    fallSpeed = v.y;
                    walk = Flat(v);
                    dir = walk.sqrMagnitude > 0.01f ? walk.normalized : dir;
                }
                else if (ballistic) walk = flight;
            }
            if (wings != null) wings.Flying = gliding;

            // Au bord de l'ile : il ne saute pas dans le vide, il s'arrete. Au bord de la
            // rampe (30/09 : plus de trous a sauter) : il se rabat vers le fut, sans sauter.
            if (grounded && !leaping && speed > 0f && !Tower.On(transform.position) && EdgeAhead(dir)) walk = Vector3.zero;
            else if (grounded && !leaping && speed > 0f && Tower.On(transform.position) && EdgeAhead(dir))
            {
                Vector3 inward = -Flat(transform.position).normalized;
                walk = (Flat(walk).normalized + inward * 1.2f).normalized * speed;
            }
            // Une barre (moulinet, balayeur) arrive : il saute par-dessus.
            else if (grounded && speed > 0f && Sweeper.Threat(transform.position + dir * 1.2f)) fallSpeed = 7.5f;
            // Bloque : il saute. Deux fois, s'il sait.
            else if (grounded && speed > 0f && !leaping && (stuck > 0.25f || !Tower.On(transform.position) && EdgeAhead(dir))) fallSpeed = 7f;
            else if (!grounded && !gliding && !ballistic && !airJumped && speed > 0f && seeker.Has(Ability.DoubleSaut) && fallSpeed < 0f && (stuck > 0.2f || EdgeAhead(dir)))
            {
                airJumped = true;
                fallSpeed = 7.3f;
            }
            // La Couronne glisse s'il tombe (sans planer).
            if (seeker.CarriesCrown && !grounded && !gliding && fallSpeed < -13f) Crown.Slip(seeker, lastGround);

            // Le pique d'aigle : en vol, il fond sur le porteur qu'il a dans le viseur.
            if (diveTime <= 0f && gliding && prey != null && prey.CarriesCrown && Match.BotLevel > 0)
            {
                Seeker t = Combat.DiveTarget(seeker, transform.position + Vector3.up * 1.5f, Flat(prey.Body.position - transform.position).normalized + Vector3.down * 0.2f);
                if (t != null) Combat.Dive(seeker, t);
            }
            if (diveTime > 0f)
            {
                Vector3 dv;
                if (!Combat.DiveStep(seeker, diveTarget, transform.position, ref diveTime, dt, out dv))
                {
                    walk = new Vector3(dv.x, 0f, dv.z);
                    fallSpeed = dv.y;
                    extra = Vector3.zero;
                    knock = Vector3.zero;
                }
                else fallSpeed = Mathf.Max(fallSpeed, 5f);
            }
            Vector3 before = transform.position;
            body.Move((walk + extra + knock + Vector3.up * fallSpeed) * dt);
            // Le sceau de la citadelle : renvoye dehors s'il y entre par les airs.
            if ((gliding || ballistic || diveTime > 0f) && Ward.Crossing(before, transform.position))
            {
                diveTime = 0f;
                Vector3 push = Ward.Repel(seeker, transform.position);
                body.enabled = false;
                transform.position = before;
                body.enabled = true;
                gliding = false;
                ballistic = false;
                leaping = false;
                knock = Flat(push);
                fallSpeed = push.y;
                think = 0f;
            }
            float moved = Flat(transform.position - before).magnitude;
            if (grounded && speed > 0f && moved < speed * dt * 0.3f)
            {
                stuck += dt;
                if (stuck > 0.7f) { detourTimer = 1f; detourSign = -detourSign; stuck = 0f; }
            }
            else stuck = 0f;

            if (walk.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Flat(walk).normalized, Vector3.up), 360f * dt);
        }

        /// <summary>Le joueur le plus proche a moins de "metres" (toi d'abord, a distance egale), null sinon.</summary>
        Seeker NearestFoe(float metres)
        {
            Seeker best = null;
            float bestD = metres;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == seeker || s.Body == null || s.Graced || s.Hidden) continue;
                float d = (s.Body.position - transform.position).magnitude - (s.IsPlayer ? 0.5f : 0f);
                if (Mathf.Abs(s.Body.position.y - transform.position.y) > 2f || d >= bestD) continue;
                bestD = d;
                best = s;
            }
            return best;
        }

        /// <summary>Vrai s'il n'y a plus de sol juste devant (le bord d'un trou, ou de l'ile).</summary>
        bool EdgeAhead(Vector3 dir)
        {
            // A 0,8 m : plus loin, il sautait trop tot et tombait dans le trou.
            Vector3 probe = transform.position + dir * 0.8f + Vector3.up * 0.6f;
            return !Physics.Raycast(probe, Vector3.down, 2.6f, ~0, QueryTriggerInteraction.Ignore);
        }

        void Land(float fall)
        {
            if (fall > 8f) fellAt = Time.time;
            if (fall > 10f && Tower.On(lastGround) && !Tower.On(transform.position)) Feed.FellFromTower(seeker);
            if (PlayerWithin(40f)) Fx.Burst(transform.position + Vector3.up * 0.1f, new Color(0.62f, 0.56f, 0.48f), 18, 3f, 0.3f, 0.8f, 0.2f, Vector3.up, 70f);
            if (fall > 4f && seeker.Has(Ability.Rebond)) Combat.Blast(transform.position, 5f, 13f, 5f, seeker);
            // (02/10) On l'entend retomber, de la ou il est (doux de loin).
            if (fall > 3f && PlayerWithin(60f)) Sfx.LandAt(transform.position);
        }

        void Remember(float dt)
        {
            trailTimer += dt;
            if (trailTimer < 0.1f) return;
            trailTimer = 0f;
            trailAt = (trailAt + 1) % trail.Length;
            trail[trailAt] = transform.position;
        }

        void Animate(float dt)
        {
            if (figure == null) return;
            // Sous le Voile, le corps s'eteint. La lanterne et le halo restent (sauf sous le
            // Voile) : c'est comme ca qu'on repere un joueur.
            bool near = PlayerWithin(320f) && !seeker.Hidden;
            if (figure.gameObject.activeSelf != near) figure.gameObject.SetActive(near);
            if (lantern != null) lantern.enabled = !seeker.Hidden;
            Vector3 moved = Flat(transform.position - lastPosition);
            lastPosition = transform.position;
            if (rig != null)
            {
                rig.Speed = gliding || mounted ? 0f : Mathf.Min(moved.magnitude / Mathf.Max(dt, 0.001f), 12f);
                rig.Grounded = body.enabled && body.isGrounded || mounted;
                rig.Tumbling = seeker.Tumbling || seeker.Launched;
            }
        }

        // ================================================================== outils

        bool PlayerWithin(float metres)
        {
            Transform p = Game.PlayerTransform;
            return p != null && Flat(p.position - transform.position).magnitude < metres;
        }

        void Bark(string line)
        {
            if (barkTimer > 0f || !PlayerWithin(22f)) return;
            barkTimer = 6f;
            // Pas de replique ecrite : sa voix (la ligne dit l'intention, pour qui lit
            // le code ; le joueur, lui, entend le ton).
            Sfx.Voice(transform.position, seeker.Name.Length, line.EndsWith("!"));
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }

    /// <summary>
    /// CE QUI FAIT QU'ON SE RECONNAIT DE LOIN (Martin : "je veux que les gens puissent
    /// se voir"). Chaque joueur est un haricot A SA COULEUR, porte une petite LANTERNE a sa
    /// couleur a la hanche, et un HALO au-dessus de la tete.
    /// </summary>
    public static class PlayerLook
    {
        /// <summary>
        /// Habiller un corps a sa couleur : une petite lanterne a la hanche.
        /// (01/10 : plus d'echarpe -- le haricot entier est a sa couleur.) Renvoie sa lumiere.
        /// </summary>
        public static Light Dress(CharacterRig rig, Transform root, Color colour)
        {
            Proto.BeginVisualOnly();
            Transform hip = rig.HipBone;
            Vector3 lamp = new Vector3(0f, -0.14f, 0f);
            Color flameColour = Color.Lerp(colour, new Color(1f, 0.8f, 0.5f), 0.35f);
            Material iron = MaterialFactory.GetShiny(new Color(0.16f, 0.16f, 0.18f), 0.6f, 0.7f);
            if (hip != null)
            {
                Proto.Cylinder(hip, lamp + new Vector3(0f, 0.1f, 0f), new Vector3(0.13f, 0.02f, 0.13f), Color.black, "Lanterne").GetComponent<Renderer>().sharedMaterial = iron;
                Proto.Cylinder(hip, lamp - new Vector3(0f, 0.09f, 0f), new Vector3(0.13f, 0.02f, 0.13f), Color.black, "Lanterne").GetComponent<Renderer>().sharedMaterial = iron;
                GameObject flame = Proto.Sphere(hip, lamp, new Vector3(0.1f, 0.14f, 0.1f), Color.white, "Flamme");
                flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(flameColour, 1.8f);
                flame.AddComponent<Flame>();
            }
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Lanterne");
            lightGo.transform.SetParent(hip != null ? hip : rig.transform, false);
            lightGo.transform.localPosition = lamp;
            Light lantern = lightGo.AddComponent<Light>();
            lantern.type = LightType.Point;
            lantern.color = flameColour;
            lantern.intensity = 1.1f;
            lantern.range = 8f;
            lantern.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();

            // (02/10) Plus de halo au-dessus de la tete : le pseudo, a sa couleur, dit deja
            // qui c'est -- et la petite flamme se logeait DANS la Couronne quand il la portait.
            return lantern;
        }
    }
}
