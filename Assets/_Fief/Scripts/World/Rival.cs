using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN BOT : un autre joueur, qui joue avec TES regles -- et qui, en Phase 3, cedera
    /// sa place a un vrai joueur en ligne (voir docs/RESEAU.md). On ne lui parle pas.
    ///
    /// Ce qu'il fait, comme toi :
    ///   - au debut, il FOUILLE un peu la foret (les coffres proches ; avec detecteur
    ///     et pelle, un tresor enterre) -- plus ou moins longtemps selon son caractere ;
    ///   - puis il MONTE A LA COURONNE : il entre par la herse si elle est levee
    ///     (sinon par la poterne ou la breche -- et il tire le levier en passant), il
    ///     traverse le donjon par les escaliers, ou prend l'escalier derobe s'il a la cle ;
    ///   - s'il la tient, il file au MONUMENT et la pose ;
    ///   - si un autre la tient, il le CHASSE : il le pousse (clic droit), il frappe ;
    ///   - si elle roule par terre, il se jette dessus ;
    ///   - il se sert de ses objets : elixir quand il saigne, fumigene quand les gardes
    ///     le talonnent, fiole de lenteur sur le porteur, piege sur la route du Monument.
    ///
    /// COMMENT IL PENSE. Toutes les 0,5 s il choisit un BUT, du plus urgent au moins
    /// urgent. Puis, chaque image, il marche vers la cible de ce but -- en suivant,
    /// s'il le faut, un CHEMIN de points (une entree du chateau, les escaliers).
    ///
    /// COMMENT IL MARCHE. Pres de toi (moins de 70 m) il a un vrai corps
    /// (CharacterController) : il bute sur les troncs et les contourne. Loin de toi,
    /// personne ne le voit : il glisse en ligne droite, pour presque rien.
    ///
    /// Tout ce qu'il fait passe par les memes portes que toi (Crown.TryTakeFor,
    /// Monument.TryDeliver, Combat.Shove, Chest.TryOpenFor) : c'est ce qui permettra
    /// a un joueur en ligne de prendre sa place sans rien changer au reste.
    /// </summary>
    public class Rival : MonoBehaviour
    {
        public static readonly List<Rival> All = new List<Rival>();

        /// <summary>Vrai si un bot, au moins, te court apres.</summary>
        public static bool HuntingPlayer
        {
            get
            {
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && (All[i].goal == Goal.Hunt || All[i].goal == Goal.Fight) && All[i].prey != null
                        && All[i].prey.IsPlayer && All[i].seeker.Alive) return true;
                return false;
            }
        }

        enum Goal { Scout, Chest, Dig, Raid, Grab, Deliver, Hunt, Fight, Flee, Lever }

        [System.NonSerialized] public Seeker seeker;

        // --- caractere
        float boldness;         // envie d'aller vite au chateau (0-1)
        float temper;           // envie de se battre (0-1)
        float scoutUntil;       // jusqu'ou (temps de manche) il fouille la foret

        // --- etat
        Goal goal = Goal.Scout;
        Vector3 target;
        float think;
        float work;
        Chest chest;
        Chest buried;
        Remains carcass;
        Seeker prey;
        float preyTimer;
        float strikeTimer;
        float shoveReadyAt;
        float fleeTimer;
        Vector3 fleeFrom;
        float deadTimer;
        float itemTimer;
        float barkTimer;
        System.Random rng;
        readonly List<Vector3> path = new List<Vector3>();
        Vector3 wander;

        // --- corps
        CharacterController body;
        float fallSpeed;
        Vector3 knock;
        float stuck;
        float detourTimer;
        float detourSign = 1f;
        Transform figure;
        CharacterRig rig;
        Vector3 lastPosition;
        Light lantern;

        const float WalkSpeed = 5.2f;
        const float RunSpeed = 7.4f;

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
            r.scoutUntil = Mathf.Lerp(100f, 25f, r.boldness);
            r.lastPosition = spawn;
            r.wander = spawn;

            // LE CORPS : exactement celui du joueur, a sa couleur.
            Color colour = slot.Colour;
            CharacterRig rig = CharacterRig.Build(root.transform, colour, Palette.Shade(colour, 0.62f));
            rig.RunSpeed = RunSpeed;
            r.rig = rig;
            r.figure = rig.transform;
            r.lantern = PlayerLook.Dress(rig, root.transform, colour);

            All.Add(r);
            return r;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>Le bot de ce joueur, ou null (toi, ou un joueur en ligne).</summary>
        public static Rival Of(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].seeker == s) return All[i];
            return null;
        }

        /// <summary>La silhouette (ce qu'on voit), pour le recul d'un coup.</summary>
        public Transform Figure { get { return figure; } }

        /// <summary>Deplace d'un coup (l'escalier derobe, la releve).</summary>
        public void Teleport(Vector3 position)
        {
            bool was = body.enabled;
            body.enabled = false;
            transform.position = position;
            lastPosition = position;
            body.enabled = was;
            path.Clear();
            think = 0f;
        }

        /// <summary>On le projette (une poussee, un coup du Roi).</summary>
        public void Push(Vector3 velocity)
        {
            knock += new Vector3(velocity.x, 0f, velocity.z);
            if (velocity.y > 0f) fallSpeed = Mathf.Max(fallSpeed, velocity.y);
        }

        // ================================================================== boucle

        void Update()
        {
            Season season = Game.Season;
            if (season == null || !season.Running || Time.deltaTime <= 0f) return;
            float dt = Time.deltaTime;

            if (deadTimer > 0f)
            {
                deadTimer -= dt;
                if (deadTimer <= 0f) Revive();
                return;
            }
            if (preyTimer > 0f) preyTimer -= dt;
            if (fleeTimer > 0f) fleeTimer -= dt;
            if (strikeTimer > 0f) strikeTimer -= dt;
            if (barkTimer > 0f) barkTimer -= dt;
            if (itemTimer > 0f) itemTimer -= dt;
            // La vie remonte apres un moment de calme (trois fois plus vite avec Sang vif).
            if (seeker.Alive && Time.time - seeker.LastHurt > 6f) seeker.Heal((seeker.Has(Power.SangVif) ? 9f : 3f) * dt);

            think -= dt;
            if (think <= 0f) { think = 0.5f; Think(season); }

            UseItems();
            Act(dt);
            Animate(dt);
        }

        /// <summary>Choisir un but, du plus urgent au moins urgent.</summary>
        void Think(Season season)
        {
            Goal was = goal;
            Vector3 me = transform.position;

            // 1. Il porte la Couronne : au Monument, et vite.
            if (seeker.CarriesCrown && Monument.Instance != null)
            {
                SetGoal(Goal.Deliver, Monument.Instance.transform.position, was);
                return;
            }

            // 2. Il fuit : a bout de forces sous les coups.
            if (fleeTimer > 0f)
            {
                Vector3 away = me - fleeFrom;
                away.y = 0f;
                goal = Goal.Flee;
                target = me + (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 20f;
                path.Clear();
                return;
            }

            // 3. La Couronne roule par terre : il se jette dessus.
            if (Crown.Where == Crown.State.Dropped && Flat(Crown.Position - me).magnitude < 150f)
            {
                SetGoal(Goal.Grab, Crown.Position, was);
                return;
            }

            // 4. Un autre la porte : il le chasse.
            Seeker holder = Crown.Holder;
            if (holder != null && holder != seeker && holder.Body != null)
            {
                // Par les portes et les escaliers s'il le faut (le chemin est refait a
                // chaque pensee : le porteur bouge).
                prey = holder;
                preyTimer = 5f;
                SetGoal(Goal.Hunt, holder.Body.position, was);
                return;
            }

            // 5. On l'a frappe : il rend les coups (un moment).
            if (prey != null && preyTimer > 0f && prey.Alive && prey.Body != null && seeker.CanStrike)
            {
                SetGoal(Goal.Fight, prey.Body.position, was);
                return;
            }
            prey = null;

            // 6. La herse est baissee, il est dans la cour, pres du levier : il le tire
            //    (c'est son chemin de retour, avec la Couronne).
            if (Lever.Instance != null && !Portcullis.IsOpen && Castle.Inside(me) && me.y < 3f
                && Flat(Lever.Instance.transform.position - me).magnitude < 16f && Lever.Instance.CanInteract)
            {
                SetGoal(Goal.Lever, Lever.Instance.transform.position, was);
                return;
            }

            // 7. Le debut de la manche : il fouille la foret.
            bool early = season.Elapsed < scoutUntil && Crown.Where == Crown.State.OnPedestal;
            if (early)
            {
                // Un tresor enterre : detecteur et pelle en main.
                if (seeker.Items.Has(Item.Detecteur) && seeker.Items.Has(Item.Pelle))
                {
                    float d;
                    Chest b = Chest.NearestBuried(me, out d);
                    if (b != null && d < 60f) { buried = b; SetGoal(Goal.Dig, b.transform.position, was); return; }
                }
                // Un coffre, pas trop loin.
                if (!seeker.Items.Full)
                {
                    if (goal == Goal.Chest && chest != null && !chest.Opened) { SetGoal(Goal.Chest, chest.transform.position, was); return; }
                    chest = NearestChest(90f);
                    if (chest != null) { SetGoal(Goal.Chest, chest.transform.position, was); return; }
                    carcass = NearestCarcass();
                    if (carcass != null) { SetGoal(Goal.Chest, carcass.transform.position, was); return; }
                }
                // Rien a portee : il avance au hasard, vers le chateau en gros.
                if (Flat(wander - me).magnitude < 4f || was != Goal.Scout)
                {
                    Vector3 towards = -Flat(me).normalized;
                    float a = ((float)rng.NextDouble() - 0.5f) * 140f;
                    wander = me + Quaternion.Euler(0f, a, 0f) * towards * (25f + (float)rng.NextDouble() * 25f);
                }
                goal = Goal.Scout;
                target = wander;
                path.Clear();
                return;
            }

            // 8. A la Couronne.
            if (Crown.Where == Crown.State.OnPedestal)
            {
                SetGoal(Goal.Raid, Keep.CrownSpot, was);
                return;
            }

            // Rien d'autre : il rode pres du Monument (la Couronne finira par y venir).
            Vector3 camp = Monument.Instance != null ? Monument.Instance.transform.position : Vector3.zero;
            float t = Time.time * 0.1f + seeker.Index;
            SetGoal(Goal.Scout, camp + new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * 12f, was);
        }

        void SetGoal(Goal g, Vector3 at, Goal was)
        {
            bool fresh = g != was || (at - target).sqrMagnitude > 9f;
            goal = g;
            target = at;
            if (fresh) PlanPath(at);
        }

        // ================================================================== les chemins

        /// <summary>
        /// Le chemin vers "to" : descendre du donjon s'il y est (par les escaliers, a
        /// l'envers), sortir de l'enceinte par l'entree la plus proche s'il faut, y
        /// entrer de meme, puis monter au bon etage -- ou prendre l'escalier derobe.
        /// </summary>
        void PlanPath(Vector3 to)
        {
            path.Clear();
            Vector3 from = transform.position;
            bool fromKeep = Castle.InKeep(from);
            bool toKeep = Castle.InKeep(to);
            bool fromCastle = Castle.Inside(from);
            bool toCastle = Castle.Inside(to);
            int fromLevel = Keep.LevelOf(from.y), toLevel = Keep.LevelOf(to.y);

            if (fromKeep && !(toKeep && toLevel == fromLevel))
            {
                List<Vector3> down = Keep.PathTo(fromLevel);
                for (int i = down.Count - 1; i >= 0; i--) path.Add(down[i]);
            }
            if (fromCastle && !toCastle)
            {
                Vector3[] exit = Castle.EntryFrom(to);
                for (int i = exit.Length - 1; i >= 0; i--) path.Add(exit[i]);
            }
            else if (!fromCastle && toCastle)
            {
                path.AddRange(Castle.EntryFrom(from));
            }

            if (toKeep && !(fromKeep && toLevel == fromLevel))
            {
                SecretDoor door = SecretDoor.Instance;
                bool secret = toLevel == 3 && door != null && (door.Open || seeker.Items.Has(Item.Cle));
                if (secret) path.Add(door.transform.position + door.transform.forward * 0.8f);
                else path.AddRange(Keep.PathTo(toLevel));
            }
        }

        /// <summary>La prochaine etape : le premier point du chemin, sinon la cible.</summary>
        Vector3 Waypoint()
        {
            while (path.Count > 0)
            {
                Vector3 p = path[0];
                Vector3 d = p - transform.position;
                bool sameFloor = Mathf.Abs(d.y) < 1.6f || !Castle.InKeep(transform.position);
                d.y = 0f;
                if (d.magnitude < 1.2f && sameFloor)
                {
                    path.RemoveAt(0);
                    // La porte derobee : il l'ouvre (ou la prend) et se retrouve la-haut.
                    SecretDoor door = SecretDoor.Instance;
                    if (door != null && Flat(p - door.transform.position).magnitude < 1.5f && door.UseFor(seeker)) { path.Clear(); return target; }
                    continue;
                }
                return p;
            }
            return target;
        }

        // ================================================================== choisir

        Chest NearestChest(float range)
        {
            Chest best = null;
            float bestD = range;
            for (int i = 0; i < Chest.All.Count; i++)
            {
                Chest c = Chest.All[i];
                if (c == null || c.Opened || c.Hidden) continue;
                if (Castle.Inside(c.transform.position)) continue;      // ceux du chateau, en passant seulement
                float d = Flat(c.transform.position - transform.position).magnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        Remains NearestCarcass()
        {
            Remains best = null;
            float bestD = 40f;
            for (int i = 0; i < Remains.All.Count; i++)
            {
                Remains r = Remains.All[i];
                if (r == null || !r.HasLoot) continue;
                float d = Flat(r.transform.position - transform.position).magnitude;
                if (d < bestD) { bestD = d; best = r; }
            }
            return best;
        }

        // ================================================================== agir

        void Act(float dt)
        {
            float speed = (goal == Goal.Scout || goal == Goal.Chest ? WalkSpeed : RunSpeed) * seeker.SpeedFactor;

            if ((goal == Goal.Hunt || goal == Goal.Fight) && prey != null && prey.Body != null) target = prey.Body.position;
            if (goal == Goal.Grab) target = Crown.Position;
            Vector3 step = Waypoint();
            float distance = Flat(target - transform.position).magnitude;
            float dy = Mathf.Abs(target.y - transform.position.y);
            float reach = goal == Goal.Hunt || goal == Goal.Fight ? 1.9f : goal == Goal.Deliver ? 3.2f : goal == Goal.Raid ? 2f : 1.8f;
            bool arrived = path.Count == 0 && distance <= reach && (dy < 2.4f || !Castle.InKeep(target));

            // Pendant la chasse, la poussee part des qu'il est a portee -- meme en courant.
            if (goal == Goal.Hunt && prey != null && prey.Body != null && distance < 2.5f && Time.time >= shoveReadyAt && !seeker.CarriesCrown)
            {
                shoveReadyAt = Time.time + (seeker.Has(Power.Poigne) ? 1.5f : 3f);
                if (rig != null) rig.PlaySwing();
                Combat.Shove(seeker, prey.Body.position - transform.position);
            }

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
                    if (work < 2f) break;
                    work = 0f;
                    if (Monument.Instance != null && Monument.Instance.TryDeliver(seeker)) Bark("Victoire !");
                    think = 0f;
                    break;

                case Goal.Raid:
                case Goal.Grab:
                    work += dt;
                    if (work < (Crown.Where == Crown.State.OnPedestal ? 1.2f : 0.5f)) break;
                    work = 0f;
                    if (Crown.Instance != null && Crown.Instance.TryTakeFor(seeker)) Bark("À moi !");
                    think = 0f;
                    break;

                case Goal.Chest:
                    work += dt;
                    if (work < 0.8f) break;
                    work = 0f;
                    if (chest != null && !chest.Opened) chest.TryOpenFor(seeker);
                    else if (carcass != null) carcass.TakeFor(seeker);
                    chest = null;
                    carcass = null;
                    think = 0f;
                    break;

                case Goal.Dig:
                    work += dt;
                    if (work < 1.6f) break;
                    work = 0f;
                    if (buried != null && buried.Hidden) { buried.Unearth(); chest = buried; goal = Goal.Chest; }
                    buried = null;
                    break;

                case Goal.Lever:
                    work += dt;
                    if (work < 1f) break;
                    work = 0f;
                    if (Lever.Instance != null) Lever.Instance.PullFor(seeker);
                    think = 0f;
                    break;

                case Goal.Hunt:
                case Goal.Fight:
                    if (prey != null && prey.Body != null) Figures.Face(transform, prey.Body.position, 360f);
                    if (strikeTimer <= 0f && prey != null && seeker.CanStrike)
                    {
                        strikeTimer = 0.9f;
                        if (rig != null) rig.PlaySwing();
                        if (PlayerWithin(25f)) Sfx.Whoosh();
                        Combat.Hit(prey, seeker, Combat.SwordDamage);
                        if (prey != null && !prey.Alive) { prey = null; Bark("Et voilà."); think = 0f; }
                    }
                    break;

                default:
                    think = 0f;
                    break;
            }
        }

        /// <summary>Ses objets, au bon moment -- comme un joueur qui sait ce qu'il fait.</summary>
        void UseItems()
        {
            if (itemTimer > 0f || !seeker.Alive || seeker.Rooted) return;
            Loadout kit = seeker.Items;
            itemTimer = 0.6f;
            Vector3 me = transform.position;

            if (kit.Has(Item.Elixir) && seeker.Health < seeker.MaxHealth * 0.4f)
            {
                kit.TryRemove(Item.Elixir);
                seeker.Heal(seeker.MaxHealth);
                Burst(ItemInfo.Tint(Item.Elixir));
                return;
            }
            if (kit.Has(Item.Plume)) { kit.TryRemove(Item.Plume); seeker.FeatherUntil = Time.time + 30f; return; }
            if (seeker.CarriesCrown) return;          // les deux mains prises

            int chasers = Guard.ChasersOf(seeker);
            if (kit.Has(Item.CapeOmbre) && (chasers > 0 || goal == Goal.Raid && Castle.Inside(me)))
            {
                kit.TryRemove(Item.CapeOmbre);
                seeker.HiddenUntil = Time.time + 10f;
                Burst(ItemInfo.Tint(Item.CapeOmbre));
                return;
            }
            if (kit.Has(Item.Fumigene) && chasers >= 2)
            {
                kit.TryRemove(Item.Fumigene);
                Thrown.Launch(seeker, Item.Fumigene, me + Vector3.up * 1.5f, Vector3.down * 2f + transform.forward);
                return;
            }
            Seeker holder = Crown.Holder;
            if (holder != null && holder != seeker && holder.Body != null)
            {
                Vector3 to = holder.Body.position - me;
                float d = Flat(to).magnitude;
                if (kit.Has(Item.Lenteur) && d > 4f && d < 15f)
                {
                    kit.TryRemove(Item.Lenteur);
                    Thrown.Launch(seeker, Item.Lenteur, me + Vector3.up * 1.6f, Thrown.Lob(to.normalized, d));
                    return;
                }
                // Un piege sur la route du Monument, s'il y est avant le porteur.
                if (kit.Has(Item.Piege) && Monument.Instance != null && Monument.Instance.Within(me, 25f))
                {
                    Vector3 at = me + Flat(to).normalized * 2f;
                    if (Trap.WhyNot(seeker, at) == null)
                    {
                        kit.TryRemove(Item.Piege);
                        Trap.Place(seeker, at, (float)rng.NextDouble() * 360f);
                    }
                }
            }
        }

        void Burst(Color c)
        {
            if (PlayerWithin(40f)) Ambiance.Burst(null, transform.position + Vector3.up * 1.2f, c);
        }

        // ================================================================== les coups

        /// <summary>On vient de le frapper. S'il peut se battre, il se retourne ; sinon il fuit.</summary>
        public void OnHit(Seeker attacker)
        {
            if (!seeker.Alive) return;
            if (attacker == null)
            {
                // Un garde, une bete : a bout de forces, il decroche.
                if (seeker.Health < 30f && !seeker.CarriesCrown)
                {
                    fleeTimer = 6f;
                    fleeFrom = transform.position + transform.forward;
                    think = 0f;
                }
                Bark("Aïe !");
                return;
            }
            if (seeker.CanStrike && (seeker.Health > 30f || rng.NextDouble() < temper))
            {
                prey = attacker;
                preyTimer = 8f + temper * 10f;
                Bark("Tu vas le regretter !");
            }
            else if (!seeker.CarriesCrown)
            {
                fleeTimer = 5f;
                fleeFrom = attacker.Body != null ? attacker.Body.position : transform.position;
                Bark("Laisse-moi !");
            }
            think = 0f;
        }

        /// <summary>On vient de le pousser : il se retourne contre celui qui l'a fait.</summary>
        public void OnShoved(Seeker by)
        {
            if (!seeker.Alive || by == null || seeker.CarriesCrown) return;
            if (rng.NextDouble() < 0.4 + temper * 0.5) { prey = by; preyTimer = 6f; think = 0f; }
        }

        /// <summary>Une bete le mord et il est a bout : il fuit.</summary>
        public void FleeFrom(Vector3 from)
        {
            if (seeker.CarriesCrown) return;
            fleeTimer = 6f;
            fleeFrom = from;
            think = 0f;
        }

        public void Die()
        {
            deadTimer = Combat.RespawnSeconds;
            prey = null;
            path.Clear();
            knock = Vector3.zero;
            if (body != null) body.enabled = false;
            if (figure != null) figure.gameObject.SetActive(false);
            if (lantern != null) lantern.enabled = false;
            transform.position += Vector3.down * 50f;          // hors de vue, le temps de se relever
        }

        void Revive()
        {
            seeker.Health = seeker.MaxHealth;
            Vector3 at = Combat.RespawnPoint(seeker, transform.position + Vector3.up * 50f);
            transform.position = at;
            lastPosition = at;
            if (figure != null) figure.gameObject.SetActive(true);
            if (lantern != null) lantern.enabled = true;
            goal = Goal.Scout;
            wander = at;
            think = 0f;
        }

        // ================================================================== marcher

        void Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            float d = to.magnitude;
            Vector3 dir = d > 0.05f ? to / d : transform.forward;
            if (d < 0.05f) speed = 0f;
            if (detourTimer > 0f)
            {
                detourTimer -= dt;
                dir = Quaternion.Euler(0f, 70f * detourSign, 0f) * dir;
            }
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-5f * dt));

            bool seen = PlayerWithin(70f) || knock.sqrMagnitude > 1f;
            if (body.enabled != seen) body.enabled = seen;

            if (seen)
            {
                fallSpeed = body.isGrounded && fallSpeed <= 0f ? -1f : fallSpeed - 22f * dt;
                // Bloque par un rebord : il saute (plus haut avec la plume).
                if (stuck > 0.25f && body.isGrounded && speed > 0f) fallSpeed = 7f * (Time.time < seeker.FeatherUntil ? 1.8f : 1f);
                Vector3 before = transform.position;
                body.Move((dir * speed + knock + Vector3.up * fallSpeed) * dt);
                float moved = Flat(transform.position - before).magnitude;
                if (speed > 0f && moved < speed * dt * 0.3f)
                {
                    stuck += dt;
                    if (stuck > 0.6f) { detourTimer = 1.1f; detourSign = -detourSign; stuck = 0f; }
                }
                else stuck = 0f;
            }
            else if (speed > 0f)
            {
                // Loin de toi : il glisse. Dans le donjon (et sur la breche), il suit la
                // hauteur du chemin au lieu du sol.
                Vector3 p = transform.position + dir * speed * dt;
                float ground = Ground.Sample(p.x, p.z);
                p.y = Castle.InKeep(p) || destination.y > ground + 1f ? Mathf.MoveTowards(transform.position.y, destination.y, speed * dt) : ground;
                transform.position = p;
            }

            if (speed > 0f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 360f * dt);
            }
        }

        void Animate(float dt)
        {
            if (figure == null) return;
            // Loin de toi, le corps s'eteint : personne ne le voit. La lanterne et le
            // halo, eux, restent : c'est comme ca qu'on repere un joueur dans la brume.
            bool near = PlayerWithin(60f);
            if (figure.gameObject.activeSelf != near) figure.gameObject.SetActive(near);
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
    /// se voir"). Chaque joueur porte :
    ///   - une ECHARPE et un ruban a sa couleur ;
    ///   - une LANTERNE a sa couleur au bout du baton, qui eclaire autour de lui ;
    ///   - un HALO au-dessus de la tete, une petite flamme a sa couleur qui perce la
    ///     brume : on voit ou sont les autres a cinquante metres.
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
