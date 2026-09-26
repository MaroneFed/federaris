using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN BOT : un autre joueur, qui joue avec TES regles -- et qui, en Phase 3, cedera
    /// sa place a un vrai joueur en ligne (voir docs/RESEAU.md). On ne lui parle pas.
    ///
    /// Ce qu'il fait, comme toi :
    ///   - au debut, il passe par un SANCTUAIRE proche s'il n'a pas de don ;
    ///   - puis il MONTE LA TOUR : une porte de la citadelle, le pied de la rampe, la
    ///     spirale (il saute les trous), la Couronne ;
    ///   - s'il la tient, il redescend la rampe et file au MONUMENT ;
    ///   - si un autre la tient, il le CHASSE : il le pousse, le crochete, le gele ;
    ///   - si elle roule par terre, il se jette dessus ;
    ///   - il se sert de TOUTES ses capacites, par le meme code que toi
    ///     (AbilityCaster.Cast) : ruee pour rattraper, onde quand on l'entoure, voile
    ///     quand un Oeil le vise, mur et mine pour couvrir sa fuite...
    ///
    /// COMMENT IL PENSE. Toutes les 0,5 s il choisit un BUT, du plus urgent au moins
    /// urgent, et un CHEMIN de points (une porte, la rampe). Chaque image, il marche
    /// vers le prochain point.
    ///
    /// COMMENT IL MARCHE. Pres de toi (moins de 70 m) il a un vrai corps
    /// (CharacterController) : il bute, saute les trous, tombe si on le pousse. Loin de
    /// toi, personne ne le voit : il glisse le long du chemin, pour presque rien.
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

        enum Goal { Shrine, Raid, Grab, Deliver, Hunt, Fight, Guard, Roam }

        [System.NonSerialized] public Seeker seeker;

        // --- caractere
        float boldness;         // envie d'aller vite a la tour (0-1)
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
        float castTimer;
        float barkTimer;
        float fellAt = -99f;
        System.Random rng;
        readonly List<Vector3> path = new List<Vector3>();

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
        Vector3 lastGround;
        float airTop;
        Transform figure;
        CharacterRig rig;
        Vector3 lastPosition;
        Light lantern;
        readonly Vector3[] trail = new Vector3[50];
        int trailAt;
        float trailTimer;

        // 27/09 : ils couraient a 7,6 m/s, toi a 10,8. Tu les semais TOUJOURS avec la
        // Couronne, et tu les rattrapais toujours sans : la course-poursuite n'existait
        // pas. Ils courent maintenant presque aussi vite que toi -- selon leur niveau.
        const float WalkSpeed = 6.2f;
        static float RunSpeed { get { return Match.BotLevel == 0 ? 9f : Match.BotLevel == 1 ? 10.2f : 10.7f; } }
        /// <summary>Le temps entre deux capacites, selon le niveau.</summary>
        static float Reflex { get { return Match.BotLevel == 0 ? 1.4f : Match.BotLevel == 1 ? 0.45f : 0.25f; } }

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
            r.scoutUntil = Mathf.Lerp(45f, 10f, r.boldness);
            r.lastPosition = spawn;
            r.lastGround = spawn;
            for (int i = 0; i < r.trail.Length; i++) r.trail[i] = spawn;

            Color colour = slot.Colour;
            CharacterRig rig = CharacterRig.Build(root.transform, colour, Palette.Shade(colour, 0.62f));
            rig.RunSpeed = RunSpeed;
            r.rig = rig;
            r.figure = rig.transform;
            r.lantern = PlayerLook.Dress(rig, root.transform, colour);

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
        float launchedUntil;
        /// <summary>Vrai quand il a DECIDE de sauter dans le vide (pour planer ou apres un tir d'arbaleste).</summary>
        bool Leaping { get { return false; } }

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

        public void Blink(Vector3 position) { Teleport(position); }

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
            body.enabled = was;
            knock = Vector3.zero;
            dashTime = 0f;
            pullTime = 0f;
            fallSpeed = 0f;
            airTop = position.y;
            path.Clear();
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

            think -= dt;
            if (think <= 0f) { think = Match.BotLevel == 2 ? 0.3f : 0.5f; Think(season); }
            if (castTimer <= 0f) { castTimer = Reflex; UseAbilities(); }

            Act(dt);
            Remember(dt);
            Animate(dt);
        }

        /// <summary>Choisir un but, du plus urgent au moins urgent.</summary>
        void Think(Season season)
        {
            Goal was = goal;
            Vector3 me = transform.position;

            if (seeker.CarriesCrown && Monument.Instance != null) { SetGoal(Goal.Deliver, Monument.Instance.transform.position, was); return; }
            if (Crown.Where == Crown.State.Dropped && (Crown.Position - me).magnitude < 160f) { SetGoal(Goal.Grab, Crown.Position, was); return; }

            Seeker holder = Crown.Holder;
            if (holder != null && holder != seeker && holder.Body != null)
            {
                prey = holder;
                preyTimer = 5f;
                // L'un d'eux (le plus pres du Monument) va l'y attendre, au lieu de
                // courir derriere avec les autres : on coupe la route du porteur.
                if (Guardian() == this && Monument.Instance != null)
                {
                    Vector3 m = Monument.Instance.transform.position;
                    Vector3 toward = Flat(holder.Body.position - m);
                    if (toward.magnitude > 35f)
                    {
                        SetGoal(Goal.Guard, m + toward.normalized * 6f, was);
                        return;
                    }
                }
                SetGoal(Goal.Hunt, holder.Body.position, was);
                return;
            }
            if (prey != null && preyTimer > 0f && prey.Body != null && !prey.Hidden && seeker.CanShove && (prey.Body.position - me).magnitude < 25f)
            {
                SetGoal(Goal.Fight, prey.Body.position, was);
                return;
            }
            prey = null;

            // Le debut : un sanctuaire proche, s'il n'a pas de don.
            if (season.Elapsed < scoutUntil && !seeker.HasGift)
            {
                if (shrine == null || shrine.Spent) shrine = NearestShrine(90f);
                if (shrine != null) { SetGoal(Goal.Shrine, shrine.transform.position, was); return; }
            }

            if (Crown.Where == Crown.State.OnPedestal) { SetGoal(Goal.Raid, Tower.CrownSpot, was); return; }

            Vector3 camp = Monument.Instance != null ? Monument.Instance.transform.position : Vector3.zero;
            float t = Time.time * 0.1f + seeker.Index;
            SetGoal(Goal.Roam, camp + new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * 10f, was);
        }

        void SetGoal(Goal g, Vector3 at, Goal was)
        {
            bool fresh = g != was || (at - target).sqrMagnitude > 9f || path.Count == 0 && (at - transform.position).magnitude > 6f;
            goal = g;
            target = at;
            if (fresh) PlanPath(at);
        }

        /// <summary>Le bot qui garde le Monument pendant que les autres chassent : le plus proche du Monument (null s'ils sont moins de deux).</summary>
        static Rival Guardian()
        {
            if (Monument.Instance == null || Match.BotLevel == 0) return null;
            Rival best = null;
            float bestD = float.MaxValue;
            int free = 0;
            for (int i = 0; i < All.Count; i++)
            {
                Rival r = All[i];
                if (r == null || r.seeker.CarriesCrown) continue;
                free++;
                float d = Flat(r.transform.position - Monument.Instance.transform.position).magnitude;
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

        // ================================================================== les chemins

        /// <summary>
        /// Le chemin vers "to" : redescendre la rampe s'il est sur la tour (et que "to"
        /// n'y est pas), sortir ou entrer par la porte la plus proche, puis monter la
        /// rampe jusqu'a la hauteur voulue.
        /// </summary>
        void PlanPath(Vector3 to)
        {
            path.Clear();
            Vector3 from = transform.position;
            bool fromTower = Tower.On(from), toTower = Tower.On(to);
            if (fromTower && toTower)
            {
                path.AddRange(Climb(Tower.Progress(from), Tower.Progress(to)));
                return;
            }
            if (fromTower)
            {
                path.AddRange(Tower.Path(Tower.Progress(from), 0f));
                path.Add(Tower.Foot);
                from = Tower.Foot;
            }
            bool fromIn = Castle.Inside(from), toIn = Castle.Inside(to);
            if (fromIn && !toIn)
            {
                Vector3[] exit = Castle.EntryFrom(to);
                for (int i = exit.Length - 1; i >= 0; i--) path.Add(exit[i]);
            }
            else if (!fromIn && toIn) path.AddRange(Castle.EntryFrom(from));
            if (toTower)
            {
                path.Add(Tower.Foot);
                path.AddRange(Climb(0f, Tower.Progress(to)));
            }
        }

        /// <summary>
        /// Monter la rampe de "from" a "to" (0-1) -- en passant par les COURANTS quand
        /// ils font gagner un tour (sauf les bots faciles, qui font tout a pied).
        /// </summary>
        List<Vector3> Climb(float from, float to)
        {
            List<Vector3> list = new List<Vector3>();
            if (to <= from || Match.BotLevel == 0) { list.AddRange(Tower.Path(from, to)); return list; }
            float cur = from;
            for (int i = 0; i < Updraft.All.Count; i++)
            {
                Updraft u = Updraft.All[i];
                if (u == null) continue;
                float at = Tower.Progress(u.transform.position);
                float landed = at + 1f / Tower.Turns;
                if (at <= cur + 0.01f || landed > to + 0.02f) continue;
                list.AddRange(Tower.Path(cur, at));
                list.Add(u.transform.position);
                cur = landed;
            }
            list.AddRange(Tower.Path(cur, to));
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
                if (Mathf.Abs(d.y) > 6f && Tower.On(p) && (!body.enabled || body.isGrounded)) { PlanPath(target); if (path.Count == 0) break; p = path[0]; d = p - transform.position; sameLevel = Mathf.Abs(d.y) < 2.5f; }
                d.y = 0f;
                // Un courant : il faut marcher DEDANS, pas a cote.
                float close = Updraft.Near(p, 0.1f) != null ? 0.4f : 1.5f;
                if (d.magnitude < close && sameLevel) { path.RemoveAt(0); continue; }
                return p;
            }
            return target;
        }

        // ================================================================== agir

        void Act(float dt)
        {
            if ((goal == Goal.Hunt || goal == Goal.Fight) && prey != null && prey.Body != null) target = prey.Body.position;
            if (goal == Goal.Grab) target = Crown.Position;
            Vector3 step = Waypoint();
            float distance = Flat(target - transform.position).magnitude;
            float dy = Mathf.Abs(target.y - transform.position.y);
            float reach = goal == Goal.Deliver ? 3.2f : goal == Goal.Hunt || goal == Goal.Fight ? 1.8f : 1.8f;
            bool arrived = path.Count == 0 && distance <= reach && dy < 2.5f;

            // La poussee : des qu'il est a portee de sa proie.
            if ((goal == Goal.Hunt || goal == Goal.Fight || goal == Goal.Guard) && prey != null && prey.Body != null && seeker.CanShove
                && Time.time >= seeker.ShoveReadyAt && (prey.Body.position - transform.position).magnitude < 2.7f)
            {
                seeker.ShoveReadyAt = Time.time + Seeker.ShoveCooldown * (seeker.Has(Ability.Poigne) ? 0.6f : 1f) * (Match.BotLevel == 0 ? 2f : Match.BotLevel == 1 ? 1.3f : 1.05f);
                if (rig != null) rig.PlaySwing();
                Combat.Shove(seeker, prey.Body.position - transform.position);
                if (goal == Goal.Fight && rng.NextDouble() < 0.4) preyTimer = 0f;
            }

            float speed = (goal == Goal.Roam ? WalkSpeed : RunSpeed) * seeker.SpeedFactor;
            if (!arrived)
            {
                work = 0f;
                Walk(step, speed, dt);
                return;
            }
            Walk(transform.position, 0f, dt);

            switch (goal)
            {
                case Goal.Deliver:
                    work += dt;
                    // (Le Monument le prend tout seul des qu'il entre dans le cercle.)
                    if (Monument.Instance != null && Monument.Instance.TryDeliver(seeker)) Bark("Victoire !");
                    think = 0f;
                    break;
                case Goal.Raid:
                case Goal.Grab:
                    work += dt;
                    if (work < (Crown.Where == Crown.State.OnPedestal ? 1f : 0.2f)) break;
                    work = 0f;
                    if (Crown.Instance != null && Crown.Instance.TryTakeFor(seeker)) Bark("À moi !");
                    think = 0f;
                    break;
                case Goal.Shrine:
                    work += dt;
                    if (work < 1f) break;
                    work = 0f;
                    if (shrine != null) shrine.TryTakeFor(seeker);
                    shrine = null;
                    think = 0f;
                    break;
                default:
                    think = 0f;
                    break;
            }
        }

        // ================================================================== les capacites

        /// <summary>
        /// Ses capacites, au bon moment -- comme un joueur qui sait ce qu'il fait. Il
        /// passe par AbilityCaster.Cast, exactement comme toi.
        /// </summary>
        void UseAbilities()
        {
            if (seeker.Stunned) return;
            List<Ability> list = seeker.Slot.Actives;
            if (seeker.HasGift) list.Add(seeker.Gift);
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
                        // Pour rattraper, ou pour fuir -- jamais sur la rampe (le vide).
                        go = !Tower.On(me) && body.isGrounded && (farToGo > 14f && (goal == Goal.Hunt || goal == Goal.Grab || goal == Goal.Deliver || goal == Goal.Raid));
                        aim = Flat(toWaypoint);
                        break;
                    case Ability.Grappin:
                        go = prey != null && preyD > 9f && preyD < 30f && (goal == Goal.Hunt);
                        aim = toPrey;
                        break;
                    case Ability.Crochet:
                        go = prey != null && prey.CarriesCrown && preyD > 5f && preyD < 22f;
                        aim = toPrey;
                        break;
                    case Ability.Souffle:
                        go = prey != null && preyD < 10f && (prey.CarriesCrown || goal == Goal.Fight);
                        aim = toPrey;
                        break;
                    case Ability.Gel:
                        go = prey != null && prey.CarriesCrown && preyD > 6f && preyD < 18f;
                        aim = toPrey;
                        break;
                    case Ability.Onde:
                        go = near > 0 && (carrying || holder != null && holder.Body != null && (holder.Body.position - me).magnitude < 5.5f || near >= 2);
                        break;
                    case Ability.Bond:
                        go = prey != null && prey.Body.position.y - me.y > 4f && Flat(toPrey).magnitude < 8f;
                        aim = Flat(toPrey);
                        break;
                    case Ability.Echange:
                        go = prey != null && prey.CarriesCrown && Monument.Instance != null && preyD > 12f && preyD < 32f
                             && Flat(prey.Body.position - Monument.Instance.transform.position).magnitude < Flat(me - Monument.Instance.transform.position).magnitude - 15f;
                        aim = toPrey;
                        break;
                    case Ability.Voile:
                        go = carrying && (Eye.ChargingAt(seeker) || Chasers(12f) > 0) || Eye.ChargingAt(seeker);
                        break;
                    case Ability.Nuee:
                        go = Eye.ChargingAt(seeker) || carrying && Chasers(8f) > 0;
                        break;
                    case Ability.Mur:
                        go = carrying && Chasers(10f) > 0 && !Tower.On(me);
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

        // ================================================================== marcher

        void Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            float d = to.magnitude;
            Vector3 dir = d > 0.05f ? to / d : transform.forward;
            if (d < 0.05f || Time.time < launchedUntil) speed = 0f;
            if (detourTimer > 0f)
            {
                detourTimer -= dt;
                dir = Quaternion.Euler(0f, 70f * detourSign, 0f) * dir;
            }
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-4.5f * dt));
            Vector3 extra = Vector3.zero;
            if (dashTime > 0f) { dashTime -= dt; extra += dashVelocity; }
            if (pullTime > 0f)
            {
                pullTime -= dt;
                Vector3 p = pullPoint - transform.position;
                if (p.magnitude < 1.6f) { pullTime = 0f; knock += Flat(p).normalized * 7f; fallSpeed = Mathf.Max(fallSpeed, 4f); }
                else { extra += p.normalized * pullSpeed; fallSpeed = Mathf.Max(fallSpeed, p.normalized.y * pullSpeed); }
            }

            // (27/09 : plus de "glisse sans physique" loin de toi -- c'etait pour la grande
            // foret. Sur l'ile, tout le monde a toujours un vrai corps.)
            if (!body.enabled) body.enabled = true;
            {
                bool grounded = body.isGrounded;
                gliding = false;
                if (grounded)
                {
                    if (airTop - transform.position.y > 4f) Land(airTop - transform.position.y);
                    airJumped = false;
                    lastGround = transform.position;
                    airTop = transform.position.y;
                    if (fallSpeed <= 0f) fallSpeed = -1f;
                }
                else
                {
                    airTop = Mathf.Max(airTop, transform.position.y);
                    fallSpeed -= 22f * dt;
                    if (seeker.Has(Ability.Planeur) && fallSpeed < -2.5f && (seeker.CarriesCrown || airTop - transform.position.y > 5f)) { fallSpeed = -2.5f; gliding = true; }
                }
                // Au bord de l'ile (pas sur la tour) : il ne saute pas dans le vide, il s'arrete.
                bool cliff = grounded && speed > 0f && !Tower.On(transform.position) && EdgeAhead(dir) && !Leaping;
                if (cliff) speed = 0f;
                // Au bord d'un trou de la rampe (ou bloque) : il saute. Deux fois, s'il sait.
                if (grounded && speed > 0f && (stuck > 0.25f || EdgeAhead(dir))) fallSpeed = 7f;
                else if (!grounded && !airJumped && speed > 0f && seeker.Has(Ability.DoubleSaut) && fallSpeed < 0f && (stuck > 0.2f || EdgeAhead(dir)))
                {
                    airJumped = true;
                    fallSpeed = 7.3f;
                }
                // La Couronne glisse s'il tombe.
                if (seeker.CarriesCrown && !grounded && !gliding && fallSpeed < -13f) Crown.Slip(seeker, lastGround);

                Vector3 before = transform.position;
                body.Move((dir * speed + extra + knock + Vector3.up * fallSpeed) * dt);
                float moved = Flat(transform.position - before).magnitude;
                if (speed > 0f && moved < speed * dt * 0.3f)
                {
                    stuck += dt;
                    if (stuck > 0.7f) { detourTimer = 1f; detourSign = -detourSign; stuck = 0f; }
                }
                else stuck = 0f;
            }
            if (speed > 0f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 360f * dt);
        }

        /// <summary>Vrai s'il n'y a plus de sol un metre et demi devant (le bord d'un trou de la rampe).</summary>
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
            if (PlayerWithin(40f)) Ambiance.Burst(null, transform.position + Vector3.up * 0.1f, new Color(0.45f, 0.42f, 0.38f));
            if (fall > 4f && seeker.Has(Ability.Rebond)) Combat.Blast(transform.position, 5f, 13f, 5f, seeker);
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
            // Loin de toi, ou sous le Voile, le corps s'eteint. La lanterne et le halo
            // restent (sauf sous le Voile) : c'est comme ca qu'on repere un joueur.
            bool near = PlayerWithin(300f) && !seeker.Hidden;
            if (figure.gameObject.activeSelf != near) figure.gameObject.SetActive(near);
            if (lantern != null) lantern.enabled = !seeker.Hidden;
            Vector3 moved = Flat(transform.position - lastPosition);
            lastPosition = transform.position;
            if (rig != null) rig.Speed = Mathf.Min(moved.magnitude / Mathf.Max(dt, 0.001f), 12f);
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
    /// se voir"). Chaque joueur porte une echarpe et un ruban a sa couleur, une LANTERNE
    /// a sa couleur au bout du baton, et un HALO au-dessus de la tete.
    /// </summary>
    public static class PlayerLook
    {
        /// <summary>Habiller un corps a sa couleur. Renvoie la lumiere de sa lanterne.</summary>
        public static Light Dress(CharacterRig rig, Transform root, Color colour)
        {
            Proto.BeginVisualOnly();
            Transform neck = rig.HeadBone;
            Proto.Cube(neck, new Vector3(0f, -0.07f, 0f), new Vector3(0.36f, 0.09f, 0.32f), colour, "Écharpe");
            GameObject tail = Proto.Cube(neck, new Vector3(0.08f, -0.24f, -0.17f), new Vector3(0.1f, 0.34f, 0.03f), Palette.Shade(colour, 0.85f), "Pan");
            tail.transform.localRotation = Quaternion.Euler(-12f, 0f, 8f);

            Transform staff = rig.StaffBone;
            Vector3 lamp = new Vector3(0.16f, 1.08f, 0.04f);
            Color flameColour = Color.Lerp(colour, new Color(1f, 0.8f, 0.5f), 0.35f);
            if (staff != null)
            {
                Proto.Cube(staff, new Vector3(0.08f, 1.24f, 0.03f), new Vector3(0.18f, 0.03f, 0.03f), new Color(0.3f, 0.23f, 0.16f), "Potence");
                Proto.Cube(staff, new Vector3(0.02f, 0.72f, 0f), new Vector3(0.08f, 0.14f, 0.08f), colour, "Ruban");
                Proto.Cube(staff, lamp + new Vector3(0f, 0.1f, 0f), new Vector3(0.14f, 0.03f, 0.14f), new Color(0.15f, 0.15f, 0.16f), "Lanterne");
                Proto.Cube(staff, lamp - new Vector3(0f, 0.09f, 0f), new Vector3(0.14f, 0.03f, 0.14f), new Color(0.15f, 0.15f, 0.16f), "Lanterne");
                GameObject flame = Proto.Cube(staff, lamp, new Vector3(0.09f, 0.13f, 0.09f), Color.white, "Flamme");
                flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(flameColour, 2.6f);
                flame.AddComponent<Flame>();
            }
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Lanterne");
            lightGo.transform.SetParent(staff != null ? staff : rig.transform, false);
            lightGo.transform.localPosition = lamp;
            Light lantern = lightGo.AddComponent<Light>();
            lantern.type = LightType.Point;
            lantern.color = flameColour;
            lantern.intensity = 1.3f;
            lantern.range = 10f;
            lantern.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();

            Halo(root, colour);
            return lantern;
        }

        /// <summary>
        /// Une petite flamme a sa couleur, qui flotte a 2,4 m au-dessus de lui. Pas un
        /// marqueur d'interface : une lueur dans le monde, que la brume avale au loin.
        /// </summary>
        public static void Halo(Transform root, Color colour)
        {
            Proto.BeginVisualOnly();
            GameObject orb = Proto.Sphere(root, new Vector3(0f, 2.45f, 0f), Vector3.one * 0.16f, Color.white, "Halo");
            Renderer r = orb.GetComponent<Renderer>();
            r.sharedMaterial = MaterialFactory.GetGlow(colour, 4f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Bobber bob = orb.AddComponent<Bobber>();
            bob.amplitude = 0.07f;
            bob.spin = 90f;
            Proto.EndVisualOnly();
        }
    }
}
