using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN RIVAL : un autre chercheur, qui joue avec TES regles -- et qui, en Phase 3,
    /// sera un vrai joueur (Martin, 26/09 : "c'est cense etre des vrais joueurs,
    /// t'es pas cense leur parler"). On ne lui parle donc pas. On le croise, on le
    /// suit, on le pille, on se bat.
    ///
    /// Ce qu'il fait, comme toi :
    ///   - il ramasse la pierre-lune (★2) et le bois ;
    ///   - il MONTE AU DONJON prendre les tresors, par les escaliers, sous le nez
    ///     des gardes -- les plus hardis jusqu'a la couronne ;
    ///   - il rapporte son butin a sa stele des qu'il en porte assez ;
    ///   - il pille les steles qu'il connait (la tienne comprise) quand leur
    ///     maitre est loin ;
    ///   - il pose des pieges autour de la sienne ;
    ///   - il se bat s'il est arme, fuit sinon ; il fouille les depouilles.
    ///
    /// COMMENT IL PENSE. Toutes les 0,7 s il choisit un BUT, du plus urgent au moins
    /// urgent. Puis, chaque image, il marche vers la cible de ce but -- en suivant,
    /// s'il le faut, un CHEMIN de points (la grande porte, les escaliers du donjon).
    ///
    /// COMMENT IL MARCHE. Pres de toi (moins de 70 m) il a un vrai corps
    /// (CharacterController) : il bute sur les troncs et les contourne. Loin de toi,
    /// personne ne le voit : il glisse en ligne droite, pour presque rien.
    /// </summary>
    public class Rival : MonoBehaviour
    {
        public static readonly List<Rival> All = new List<Rival>();

        /// <summary>Le dernier a avoir pille TA stele, et jusqu'a quand on le voit sur la boussole.</summary>
        public static Rival ThiefOfMe;

        /// <summary>Vrai si un rival, au moins, te court apres l'epee a la main.</summary>
        public static bool HuntingPlayer
        {
            get
            {
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].aggro != null && All[i].aggro.IsPlayer && All[i].aggroTimer > 0f && All[i].seeker.Alive) return true;
                return false;
            }
        }
        public static float ThiefUntil;
        public bool IsHuntedThief { get { return ThiefOfMe == this && Time.time < ThiefUntil && seeker.Hoard.Carried > 0 && seeker.Alive; } }

        enum Goal { Gather, Raid, Bank, Pillage, Scavenge, Fight, Flee }

        [System.NonSerialized] public Seeker seeker;

        // --- caractere
        float aggression;       // envie de piller et de se battre (0-1)
        float daring;           // envie de monter au donjon (0-1)

        // --- etat
        Goal goal = Goal.Gather;
        Vector3 target;
        float think;
        float work;
        ResourceNode node;
        Treasure prize;
        Stele victim;
        Remains carcass;
        float raidCooldown = 40f;
        float pillageCooldown = 120f;
        float barkTimer;
        System.Random rng;
        readonly List<Vector3> path = new List<Vector3>();

        // --- le combat
        Seeker aggro;
        float aggroTimer;
        float strikeTimer;
        float fleeTimer;
        Vector3 fleeFrom;
        float deadTimer;
        float rearmTimer;
        Vector3 dangerAt;
        float dangerTimer;

        // --- corps
        CharacterController body;
        float fallSpeed;
        float stuck;
        float detourTimer;
        float detourSign = 1f;
        float wallTimer;
        Transform figure;
        CharacterRig rig;
        Vector3 lastPosition;

        const float WalkSpeed = 4.4f;
        const float RunSpeed = 6.6f;

        // ================================================================== construction

        public static Rival Build(Transform parent, string name, Color colour, Vector3 spawn, float aggression, float daring, int seed)
        {
            Inventory bag = new Inventory();
            bag.MaxWeight = 60f;
            Hoard hoard = new Hoard();
            Seeker seeker = new Seeker(name, colour, false, bag, hoard);
            Game.Seekers.Add(seeker);

            GameObject root = new GameObject("RIVAL " + name);
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
            r.aggression = aggression;
            r.daring = daring;
            r.rng = new System.Random(seed);
            r.lastPosition = spawn;
            r.raidCooldown = 30f + (float)r.rng.NextDouble() * 60f;

            // LE CORPS : exactement celui du joueur. Sa couleur passe par la bande du
            // poncho, une echarpe et un ruban au baton.
            CharacterRig rig = CharacterRig.Build(root.transform, colour, Palette.Shade(colour, 0.62f));
            rig.RunSpeed = RunSpeed;
            r.rig = rig;
            r.figure = rig.transform;

            Proto.BeginVisualOnly();
            Transform neck = rig.HeadBone;
            Proto.Cube(neck, new Vector3(0f, -0.07f, 0f), new Vector3(0.36f, 0.09f, 0.32f), colour, "Écharpe");
            GameObject tail = Proto.Cube(neck, new Vector3(0.08f, -0.24f, -0.17f), new Vector3(0.1f, 0.34f, 0.03f), Palette.Shade(colour, 0.85f), "Pan");
            tail.transform.localRotation = Quaternion.Euler(-12f, 0f, 8f);

            // La lanterne pend au bout du baton : de loin, on voit une lueur qui se
            // balance a hauteur de tete. C'est comme ca qu'on repere un rival.
            Transform staff = rig.StaffBone;
            Vector3 lantern = new Vector3(0.16f, 1.08f, 0.04f);
            if (staff != null)
            {
                Proto.Cube(staff, new Vector3(0.08f, 1.24f, 0.03f), new Vector3(0.18f, 0.03f, 0.03f), new Color(0.3f, 0.23f, 0.16f), "Potence");
                Proto.Cube(staff, new Vector3(0.02f, 0.72f, 0f), new Vector3(0.08f, 0.14f, 0.08f), colour, "Ruban");
                Proto.Cube(staff, lantern + new Vector3(0f, 0.1f, 0f), new Vector3(0.14f, 0.03f, 0.14f), new Color(0.15f, 0.15f, 0.16f), "Lanterne");
                Proto.Cube(staff, lantern - new Vector3(0f, 0.09f, 0f), new Vector3(0.14f, 0.03f, 0.14f), new Color(0.15f, 0.15f, 0.16f), "Lanterne");
                GameObject flame = Proto.Cube(staff, lantern, new Vector3(0.09f, 0.13f, 0.09f), Color.white, "Flamme");
                flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.72f, 0.38f), 2.6f);
                flame.AddComponent<Flame>();
            }
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Lanterne de " + name);
            lightGo.transform.SetParent(staff != null ? staff : rig.transform, false);
            lightGo.transform.localPosition = lantern;
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.76f, 0.48f);
            light.intensity = 1.2f;
            light.range = 9f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();

            All.Add(r);
            return r;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>Deplace d'un coup.</summary>
        public void Teleport(Vector3 position)
        {
            body.enabled = false;
            transform.position = position;
            lastPosition = position;
            path.Clear();
            node = null;
            think = 0f;
        }

        /// <summary>Le rival de ce chercheur, ou null.</summary>
        public static Rival Of(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].seeker == s) return All[i];
            return null;
        }

        /// <summary>Quelqu'un vient de piller la stele de "victim" : s'il est arme, il le chasse.</summary>
        public static void NotifyTheft(Seeker victim, Seeker thief)
        {
            Rival r = Of(victim);
            if (r == null || thief == null) return;
            if (r.armed && r.seeker.Alive)
            {
                r.aggro = thief;
                r.aggroTimer = 60f;
                r.think = 0f;
            }
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
            if (aggroTimer > 0f) aggroTimer -= dt;
            if (fleeTimer > 0f) fleeTimer -= dt;
            if (strikeTimer > 0f) strikeTimer -= dt;
            if (raidCooldown > 0f) raidCooldown -= dt;
            if (pillageCooldown > 0f) pillageCooldown -= dt;
            if (dangerTimer > 0f) dangerTimer -= dt;
            if (barkTimer > 0f) barkTimer -= dt;
            if (seeker.Alive && Stele.NearOwn(seeker, 8f) && Time.time - seeker.LastHurt > 2f) seeker.Heal(18f * dt);
            else if (seeker.Alive && Time.time - seeker.LastHurt > 8f) seeker.Heal(3f * dt);
            if (rearmTimer > 0f)
            {
                rearmTimer -= dt;
                // Il s'est refait une epee (du bois et du fer, quelque part).
                if (rearmTimer <= 0f && armed && seeker.Kit.FreeSlot >= 0 && !seeker.Kit.Holding(ToolKind.Epee))
                {
                    seeker.Kit.Slots[seeker.Kit.FreeSlot] = new Tool(ToolKind.Epee);
                    seeker.Kit.Select(0);
                }
            }

            think -= dt;
            if (think <= 0f) { think = 0.7f; Think(season); }

            Act(dt);
            Notice();
            Animate(dt);
        }

        /// <summary>Choisir un but, du plus urgent au moins urgent.</summary>
        void Think(Season season)
        {
            Hoard h = seeker.Hoard;
            Goal was = goal;

            // Il fuit : on l'a frappe alors qu'il ne peut pas se battre.
            if (fleeTimer > 0f)
            {
                Vector3 away = transform.position - fleeFrom;
                away.y = 0f;
                goal = Goal.Flee;
                target = transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 20f;
                path.Clear();
                return;
            }

            // Il se bat : on l'a frappe, on l'a pille, ou un porteur de butin passe a portee.
            if (aggro == null) LookForPrey();
            if (aggro != null && aggroTimer > 0f && aggro.Alive && aggro.Body != null && seeker.Kit.Holding(ToolKind.Epee))
            {
                goal = Goal.Fight;
                target = aggro.Body.position;
                path.Clear();
                return;
            }
            aggro = null;

            if (!h.StelePlanted) { goal = Goal.Gather; target = transform.position; return; }

            // Du butin sur lui : il rentre le deposer (vite, s'il en a beaucoup ou si la cloche approche).
            bool heavy = h.Carried >= 10 || seeker.Bag.Load01 > 0.8f || h.Carried > 0 && season.Remaining < 100f
                         || seeker.Bag.Get(ResourceType.Moonstone) >= 8;
            if (heavy || goal == Goal.Bank && (h.Carried > 0 || seeker.Bag.Get(ResourceType.Moonstone) > 0))
            {
                SetGoal(Goal.Bank, h.StelePosition, was);
                return;
            }

            // Une depouille avec du butin, pas loin : il la fouille.
            if (goal == Goal.Scavenge && carcass != null && carcass.HasLoot) { SetGoal(Goal.Scavenge, carcass.transform.position, was); return; }
            carcass = NearestCarcass();
            if (carcass != null) { SetGoal(Goal.Scavenge, carcass.transform.position, was); return; }

            // Il continue le raid en cours tant que le tresor est la.
            if (goal == Goal.Raid && prize != null && prize.Available) { SetGoal(Goal.Raid, prize.transform.position, was); return; }
            if (goal == Goal.Pillage && victim != null && victim.owner.Hoard.Banked > 0 && !victim.Guarded) { SetGoal(Goal.Pillage, victim.transform.position, was); return; }

            // Piller une stele connue, pleine, dont le maitre est loin.
            if (pillageCooldown <= 0f)
            {
                pillageCooldown = 25f;
                Stele best = PillageTarget();
                if (best != null && rng.NextDouble() < 0.35 + aggression * 0.5)
                {
                    victim = best;
                    SetGoal(Goal.Pillage, best.transform.position, Goal.Gather);
                    return;
                }
            }

            // Monter au chateau prendre un tresor.
            if (raidCooldown <= 0f && h.Carried < 6)
            {
                raidCooldown = 20f;
                Treasure t = ChooseTreasure();
                if (t != null && rng.NextDouble() < 0.3 + daring * 0.6)
                {
                    prize = t;
                    SetGoal(Goal.Raid, t.transform.position, Goal.Gather);
                    return;
                }
            }

            goal = Goal.Gather;
            if (was != Goal.Gather) path.Clear();
            if (node == null || node.IsDepleted || seeker.Bag.SpaceFor(node.type) <= 0) node = ChooseNode();
            target = node != null ? node.transform.position : h.StelePosition;
        }

        /// <summary>Changer de but : on recalcule le chemin (portes, escaliers) quand la cible change.</summary>
        void SetGoal(Goal g, Vector3 at, Goal was)
        {
            bool fresh = g != was || (at - target).sqrMagnitude > 4f;
            goal = g;
            target = at;
            if (fresh) PlanPath(at);
        }

        // ================================================================== les chemins

        /// <summary>
        /// Le chemin vers "to" : sortir du donjon s'il y est (par les escaliers, a
        /// l'envers), sortir de l'enceinte par la porte la plus proche s'il faut, y
        /// entrer de meme, puis monter au bon etage.
        /// </summary>
        void PlanPath(Vector3 to)
        {
            path.Clear();
            Vector3 from = transform.position;
            bool fromKeep = Castle.InKeep(from);
            bool toKeep = Castle.InKeep(to);
            bool fromCastle = Castle.Covers(from.x, from.z, -3f);
            bool toCastle = Castle.Covers(to.x, to.z, -3f);

            if (fromKeep && !(toKeep && Keep.LevelOf(to.y) == Keep.LevelOf(from.y)))
            {
                List<Vector3> down = Keep.PathTo(Keep.LevelOf(from.y));
                for (int i = down.Count - 1; i >= 0; i--) path.Add(down[i]);
            }
            Vector3[] storeFrom = fromCastle ? Castle.StoreroomDoor(from) : null;
            if (storeFrom != null && Castle.InStoreroom(from.x, from.z)) { path.Add(storeFrom[1]); path.Add(storeFrom[0]); }

            if (fromCastle && !toCastle)
            {
                Vector3[] exit = Castle.EntryFrom(to);
                path.Add(exit[1]);
                path.Add(exit[0]);
            }
            else if (!fromCastle && toCastle)
            {
                Vector3[] entry = Castle.EntryFrom(from);
                path.Add(entry[0]);
                path.Add(entry[1]);
            }

            if (toKeep && !(fromKeep && Keep.LevelOf(to.y) == Keep.LevelOf(from.y)))
                path.AddRange(Keep.PathTo(Keep.LevelOf(to.y)));
            else if (toCastle && Castle.InStoreroom(to.x, to.z))
            {
                Vector3[] door = Castle.StoreroomDoor(to);
                if (door != null) { path.Add(door[0]); path.Add(door[1]); }
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
                if (d.magnitude < 1.1f && sameFloor) { path.RemoveAt(0); continue; }
                return p;
            }
            return target;
        }

        // ================================================================== choisir

        /// <summary>Un tresor disponible : le plus rentable, compte tenu de la distance et de son audace.</summary>
        Treasure ChooseTreasure()
        {
            Treasure best = null;
            float bestScore = 0f;
            for (int i = 0; i < Treasure.All.Count; i++)
            {
                Treasure t = Treasure.All[i];
                if (t == null || !t.Available) continue;
                if (t.kind == Treasure.Kind.Couronne && daring < 0.6f) continue;
                int level = Castle.InKeep(t.transform.position) ? Keep.LevelOf(t.transform.position.y) : 0;
                if (level > 1 && daring < 0.45f) continue;
                float d = Flat(t.transform.position - transform.position).magnitude + level * 25f;
                if (dangerTimer > 0f && Flat(t.transform.position - dangerAt).magnitude < 25f) continue;
                float score = t.Value / (30f + d);
                if (score > bestScore) { bestScore = score; best = t; }
            }
            return best;
        }

        /// <summary>Une stele connue, a piller : de l'or dessus, et son maitre a plus de 40 m.</summary>
        Stele PillageTarget()
        {
            Stele best = null;
            int bestLoot = 14;
            for (int i = 0; i < Stele.All.Count; i++)
            {
                Stele s = Stele.All[i];
                if (s == null || s.owner == seeker || !seeker.Knows(s.owner)) continue;
                int loot = s.owner.Hoard.Banked;
                if (loot <= bestLoot) continue;
                if (s.owner.Body != null && Flat(s.owner.Body.position - s.transform.position).magnitude < 40f) continue;
                if (Flat(s.transform.position - transform.position).magnitude > 220f) continue;
                bestLoot = loot;
                best = s;
            }
            return best;
        }

        Remains NearestCarcass()
        {
            Remains best = null;
            float bestD = 45f;
            for (int i = 0; i < Remains.All.Count; i++)
            {
                Remains r = Remains.All[i];
                if (r == null || !r.HasLoot) continue;
                float d = Flat(r.transform.position - transform.position).magnitude;
                if (d < bestD) { bestD = d; best = r; }
            }
            return best;
        }

        /// <summary>
        /// Choisir un gisement : la pierre-lune d'abord (c'est du butin), le bois quand
        /// il en manque pour ses pieges, le fer du chateau pour les plus hardis.
        /// </summary>
        ResourceNode ChooseNode()
        {
            ResourceType wanted = ResourceType.Moonstone;
            if (seeker.Bag.Get(ResourceType.Deadwood) < 6 && rng.NextDouble() < 0.35) wanted = ResourceType.Deadwood;
            else if (seeker.Bag.Get(ResourceType.Iron) < 2 && rng.NextDouble() < daring * 0.3) wanted = ResourceType.Iron;

            ResourceNode best = null;
            float bestD = float.MaxValue;
            float limit = wanted == ResourceType.Iron ? 400f : wanted == ResourceType.Moonstone ? 200f : 80f;
            for (int pass = 0; pass < 2 && best == null; pass++)
            {
                for (int i = 0; i < ResourceNode.All.Count; i++)
                {
                    ResourceNode n = ResourceNode.All[i];
                    if (n == null || n.IsDepleted) continue;
                    if (pass == 0 && n.type != wanted) continue;
                    if (pass == 1 && n.type == ResourceType.Iron) continue;
                    if (seeker.Bag.SpaceFor(n.type) <= 0) continue;
                    if (dangerTimer > 0f && Flat(n.transform.position - dangerAt).magnitude < 30f) continue;
                    float d = Flat(n.transform.position - transform.position).magnitude;
                    if (pass == 0 && d > limit) continue;
                    if (d < bestD) { bestD = d; best = n; }
                }
            }
            if (best != null) PlanPath(best.transform.position);
            return best;
        }

        // ================================================================== agir

        void Act(float dt)
        {
            Hoard h = seeker.Hoard;
            float speed = Mathf.Lerp(WalkSpeed, WalkSpeed * 0.75f, seeker.Bag.Load01);
            if (goal == Goal.Fight || goal == Goal.Flee || goal == Goal.Bank && h.Carried >= 20) speed = RunSpeed * (goal == Goal.Bank ? 0.85f : 1f);

            if (goal == Goal.Fight && aggro != null && aggro.Body != null) target = aggro.Body.position;
            Vector3 step = Waypoint();
            float distance = Flat(target - transform.position).magnitude;
            float dy = Mathf.Abs(target.y - transform.position.y);
            float reach = goal == Goal.Gather ? 2.2f : goal == Goal.Fight ? 1.9f : goal == Goal.Raid ? 1.9f : 2.4f;
            bool arrived = path.Count == 0 && distance <= reach && (dy < 2f || !Castle.InKeep(target));

            if (!arrived)
            {
                work = 0f;
                Walk(step, speed, dt);
                return;
            }

            switch (goal)
            {
                case Goal.Gather:
                    Harvest(dt);
                    break;

                case Goal.Raid:
                    work += dt;
                    if (prize == null || !prize.Available) { think = 0f; break; }
                    if (work < 1.2f) break;
                    if (prize.TryTakeFor(seeker)) Bark("Il est à moi !");
                    prize = null;
                    think = 0f;
                    break;

                case Goal.Bank:
                    h.RequestBank(seeker.Bag);
                    seeker.SyncWeight();
                    SetTraps();
                    think = 0f;
                    break;

                case Goal.Pillage:
                    work += dt;
                    if (victim == null || victim.Guarded) { think = 0f; break; }
                    if (work < 3f) break;
                    work = 0f;
                    Pillage(victim);
                    victim = null;
                    think = 0f;
                    break;

                case Goal.Scavenge:
                    work += dt;
                    if (work < 1.5f || carcass == null) break;
                    carcass.TakeFor(seeker);
                    carcass = null;
                    think = 0f;
                    break;

                case Goal.Fight:
                    // A portee : un coup d'epee toutes les 1,1 s.
                    if (strikeTimer <= 0f && aggro != null && seeker.CanStrike)
                    {
                        strikeTimer = 1.1f;
                        if (rig != null) rig.PlaySwing();
                        if (PlayerWithin(25f)) Sfx.HarvestTap(ResourceType.Iron);
                        if (seeker.Kit.Wear(1)) rearmTimer = 60f;
                        Combat.Hit(aggro, seeker, Combat.SwordDamage * 0.8f);
                        if (!aggro.Alive) { aggro = null; Bark("Et voilà."); }
                    }
                    if (aggro != null && aggro.Body != null) Figures.Face(transform, aggro.Body.position, 360f);
                    break;

                case Goal.Flee:
                    think = 0f;
                    break;
            }
        }

        void Harvest(float dt)
        {
            if (node == null || node.IsDepleted) { think = 0f; return; }
            float penalty = Mathf.Lerp(1f, 2.4f, seeker.Bag.Load01);
            work += dt;
            if (work < node.harvestDuration * penalty * 1.2f + 0.3f) return;
            work = 0f;
            node.TryTakeFor(seeker.Bag, node.yieldPerHarvest);
            if (node.IsDepleted || seeker.Bag.SpaceFor(node.type) <= 0) think = 0f;
        }

        void Pillage(Stele s)
        {
            int taken = seeker.Hoard.RequestPillage(s.owner.Hoard);
            seeker.SyncWeight();
            if (taken <= 0) return;
            Bark("Merci bien !");
            if (s.owner == Game.Me)
            {
                Stats.Robbed += taken;
                ThiefOfMe = this;
                ThiefUntil = Time.time + 90f;
                Sfx.Alarm();
                Me().Discover(seeker);
                if (Game.Hud != null && Game.Me.Body != null)
                    Game.Hud.ShowDiscovery("", "-★" + taken, seeker.Name, "", new Color(1f, 0.4f, 0.3f));
            }
            NotifyTheft(s.owner, seeker);
        }

        static Seeker Me() { return Game.Me; }

        /// <summary>
        /// Les rivaux posent des pieges autour de leur stele avec le bois et le fer de
        /// leur sac (memes prix que toi) : piller Mahaut, c'est regarder ou l'on pose
        /// les pieds.
        /// </summary>
        void SetTraps()
        {
            Hoard h = seeker.Hoard;
            if (aggression < 0.3f) return;
            int wanted = aggression >= 0.6f ? 3 : 2;
            int[] cost = Builder.Cost(Builder.Kind.Machoires);
            for (int n = Trap.CountOf(seeker); n < wanted; n++)
            {
                for (int i = 0; i < cost.Length; i++) if (seeker.Bag.Get((ResourceType)i) < cost[i]) return;
                Vector3 at = Vector3.zero;
                bool found = false;
                for (int tries = 0; tries < 12 && !found; tries++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float r = 2.5f + (float)rng.NextDouble() * 3.5f;
                    at = h.StelePosition + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    found = Trap.WhyNot(seeker, at) == null;
                }
                if (!found) return;
                for (int i = 0; i < cost.Length; i++) seeker.Bag.TryRemove((ResourceType)i, cost[i]);
                Trap.Place(seeker, at, (float)rng.NextDouble() * 360f);
            }
        }

        /// <summary>Ce qu'il remarque en passant : les steles des autres.</summary>
        void Notice()
        {
            for (int i = 0; i < Stele.All.Count; i++)
            {
                Stele s = Stele.All[i];
                if (s == null || s.owner == seeker || seeker.Knows(s.owner)) continue;
                if (Flat(s.transform.position - transform.position).magnitude < 12f) seeker.Discover(s.owner);
            }
        }

        // ================================================================== le combat

        /// <summary>Il a une epee (les deux plus hardis partent armes).</summary>
        public bool armed;

        public void Arm()
        {
            armed = true;
            seeker.Kit.Slots[0] = new Tool(ToolKind.Epee);
            seeker.Kit.Select(0);
        }

        /// <summary>La silhouette (ce qu'on voit), pour le recul d'un coup.</summary>
        public Transform Figure { get { return figure; } }

        /// <summary>Une bete le mord et il est a bout : il fuit, et evite l'endroit.</summary>
        public void FleeFrom(Vector3 from)
        {
            fleeTimer = 7f;
            fleeFrom = from;
            dangerAt = from;
            dangerTimer = 60f;
            node = null;
            prize = null;
            think = 0f;
            Bark("Au diable cette bête !");
        }

        /// <summary>On vient de le frapper. S'il peut se battre, il se retourne ; sinon il fuit.</summary>
        public void OnHit(Seeker attacker)
        {
            if (!seeker.Alive) return;
            if (attacker == null)
            {
                // Un garde : il lache son raid et rentre, butin sous le bras.
                dangerAt = transform.position;
                dangerTimer = 90f;
                prize = null;
                goal = seeker.Hoard.Carried > 0 ? Goal.Bank : Goal.Gather;
                if (goal == Goal.Bank) PlanPath(seeker.Hoard.StelePosition);
                think = 0.7f;
                Bark("Aïe !");
                return;
            }
            bool canFight = seeker.Kit.Holding(ToolKind.Epee) && seeker.Health > 35f;
            if (canFight)
            {
                aggro = attacker;
                aggroTimer = 25f;
                Bark("Tu vas le regretter !");
            }
            else
            {
                fleeTimer = 6f;
                fleeFrom = attacker.Body != null ? attacker.Body.position : transform.position;
                Bark("Laisse-moi !");
            }
            think = 0f;
        }

        /// <summary>
        /// Quelqu'un porte du butin a moins de douze metres : les rivaux armes et
        /// agressifs tentent leur chance -- toi comme les autres.
        /// </summary>
        void LookForPrey()
        {
            if (!armed || !seeker.Kit.Holding(ToolKind.Epee) || seeker.Health < 50f) return;
            // La Couronne se voit de partout : a moins de 90 m, il y va.
            Seeker crown = Treasure.CrownHolder;
            if (crown != null && crown != seeker && crown.Body != null && Flat(crown.Body.position - transform.position).magnitude < 90f)
            {
                aggro = crown;
                aggroTimer = 30f;
                Bark("La couronne !");
                return;
            }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == seeker || !s.Alive || s.Body == null || s.Hoard.Carried < 8) continue;
                // Plus il porte, plus on le sent de loin.
                float range = s.Hoard.Carried >= 15 ? 35f : 14f;
                if (Flat(s.Body.position - transform.position).magnitude > range) continue;
                if (rng.NextDouble() > aggression * 0.2f) continue;
                aggro = s;
                aggroTimer = 20f;
                Bark("Donne-moi ça !");
                return;
            }
        }

        public void Die()
        {
            deadTimer = 20f;
            aggro = null;
            prize = null;
            path.Clear();
            if (body != null) body.enabled = false;
            if (figure != null) figure.gameObject.SetActive(false);
            transform.position += Vector3.down * 50f;          // hors de vue, le temps de se relever
        }

        void Revive()
        {
            seeker.Health = Seeker.MaxHealth;
            Vector3 at = Combat.RespawnPoint(seeker, transform.position + Vector3.up * 50f);
            transform.position = at;
            lastPosition = at;
            if (figure != null) figure.gameObject.SetActive(true);
            if (armed) rearmTimer = 45f;
            goal = Goal.Gather;
            think = 0f;
        }

        // ================================================================== marcher

        void Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            float d = to.magnitude;
            if (d < 0.05f) return;
            Vector3 dir = to / d;
            if (detourTimer > 0f)
            {
                detourTimer -= dt;
                dir = Quaternion.Euler(0f, 70f * detourSign, 0f) * dir;
            }

            bool seen = PlayerWithin(70f);
            if (body.enabled != seen) body.enabled = seen;

            if (seen)
            {
                fallSpeed = body.isGrounded ? -1f : fallSpeed - 22f * dt;
                Vector3 before = transform.position;
                body.Move((dir * speed + Vector3.up * fallSpeed) * dt);
                float moved = Flat(transform.position - before).magnitude;
                if (moved < speed * dt * 0.3f)
                {
                    stuck += dt;
                    // Une barricade qui n'est pas la sienne lui barre la route : il la casse.
                    Barricade wall = stuck > 0.35f ? Barricade.Blocking(seeker, transform.position, 1.3f) : null;
                    if (wall != null)
                    {
                        wallTimer -= dt;
                        if (wallTimer <= 0f)
                        {
                            wallTimer = 1.1f;
                            if (rig != null) rig.PlaySwing();
                            wall.Hit();
                        }
                    }
                    else if (stuck > 0.35f) { detourTimer = 1.1f; detourSign = -detourSign; stuck = 0f; }
                }
                else stuck = 0f;
            }
            else
            {
                // Loin de toi : il glisse. Dans le donjon, il suit la hauteur du chemin
                // (escaliers) au lieu du sol.
                Vector3 p = transform.position + dir * speed * dt;
                p.y = Castle.InKeep(p) ? Mathf.MoveTowards(transform.position.y, destination.y, speed * dt) : Ground.Sample(p.x, p.z);
                transform.position = p;
            }

            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 360f * dt);
        }

        void Animate(float dt)
        {
            if (figure == null) return;
            // Loin de toi, le corps s'eteint : personne ne le voit.
            bool near = PlayerWithin(45f);
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
}
