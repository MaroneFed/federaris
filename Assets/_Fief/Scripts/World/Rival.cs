using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN RIVAL : un autre chercheur de relique, qui joue avec TES regles.
    ///
    /// Il plante sa stele quelque part dans la foret, recolte aux memes gisements
    /// que toi (ce qu'il prend, tu ne le trouveras plus), court au mage quand il
    /// chante, forge, pose sa relique. Et il n'est pas honnete :
    ///
    ///   - s'il passe pres de TA stele, il s'en souvient ; plus tard, quand tu es
    ///     loin, il revient la piller ;
    ///   - si tu voles la sienne, il te CHASSE. S'il te rattrape avant que tu
    ///     l'aies fondue a ta stele, il la reprend ;
    ///   - quand il n'a rien a faire, il GARDE sa stele : impossible de la piller
    ///     sous son nez.
    ///
    /// On voit sa lanterne bouger dans la brume bien avant de le voir, lui.
    ///
    /// COMMENT IL PENSE. Toutes les 0,7 s il choisit un BUT (planter, recolter,
    /// aller au mage, rentrer, piller, chasser, garder), dans cet ordre de
    /// priorite. Puis, chaque image, il marche vers la cible de ce but. C'est un
    /// "automate a buts" : simple a lire, simple a regler, et on peut ajouter un
    /// but sans casser les autres.
    ///
    /// COMMENT IL MARCHE. Pres de toi (moins de 70 m) il a un vrai corps
    /// (CharacterController) : il bute sur les troncs et les contourne. Loin de
    /// toi, personne ne le voit : il avance en ligne droite en suivant le sol,
    /// pour presque rien.
    /// </summary>
    public class Rival : MonoBehaviour, IInteractable, IDialogue
    {
        public static readonly List<Rival> All = new List<Rival>();

        enum Goal { Deposit, Resupply, Gather, FetchRelic, ToMage, ToStele, Steal, Hunt, Guard, Fight, Flee }

        [System.NonSerialized] public Seeker seeker;

        // --- caractere
        float aggression;       // envie de piller (0-1)
        float ironLove;         // attirance pour le fer du chateau (0-1)
        string[] taunts;

        // --- etat
        Goal goal = Goal.Gather;
        Vector3 target;
        float think;
        float work;
        ResourceNode node;
        Seeker huntTarget;
        float huntTimer;
        float stunTimer;
        float stealTimer;
        float stealCooldown = 90f;
        float barkTimer;
        int talks;
        System.Random rng;

        // --- le combat
        Seeker aggro;
        float aggroTimer;
        float strikeTimer;
        float fleeTimer;
        Vector3 fleeFrom;
        float deadTimer;
        float rearmTimer;

        // --- corps
        CharacterController body;
        float fallSpeed;
        float stuck;
        float detourTimer;
        float detourSign = 1f;
        Transform figure;
        CharacterRig rig;
        Vector3 lastPosition;

        const float WalkSpeed = 4.3f;
        const float RunSpeed = 6.6f;

        // ================================================================== construction

        public static Rival Build(Transform parent, string name, Color colour, Vector3 spawn,
                                  float aggression, float ironLove, string[] taunts, int seed)
        {
            Inventory bag = new Inventory();
            bag.MaxWeight = 60f;
            Hoard hoard = new Hoard();
            Seeker seeker = new Seeker(name, colour, false, bag, new Wallet(40), hoard);
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
            cc.stepOffset = 0.4f;

            Rival r = root.AddComponent<Rival>();
            r.seeker = seeker;
            r.body = cc;
            r.aggression = aggression;
            r.ironLove = ironLove;
            r.taunts = taunts;
            r.rng = new System.Random(seed);
            r.lastPosition = spawn;

            // LE CORPS : exactement celui du joueur -- poncho, capuche, baton. Un rival
            // joue avec les memes regles que toi ; en multijoueur, ce sera un joueur.
            // Sa couleur passe par la bande du poncho, une echarpe et un ruban au baton.
            CharacterRig rig = CharacterRig.Build(root.transform, colour, Palette.Shade(colour, 0.62f));
            rig.RunSpeed = RunSpeed;
            r.rig = rig;
            r.figure = rig.transform;

            Proto.BeginVisualOnly();
            Transform neck = rig.HeadBone;
            Proto.Cube(neck, new Vector3(0f, -0.07f, 0f), new Vector3(0.36f, 0.09f, 0.32f), colour, "Echarpe");
            GameObject tail = Proto.Cube(neck, new Vector3(0.08f, -0.24f, -0.17f), new Vector3(0.1f, 0.34f, 0.03f), Palette.Shade(colour, 0.85f), "Pan");
            tail.transform.localRotation = Quaternion.Euler(-12f, 0f, 8f);

            // La lanterne pend au bout du baton : de loin, on voit une lueur qui
            // se balance a hauteur de tete. C'est comme ca qu'on repere un rival.
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

        /// <summary>Deplace d'un coup (jete dehors par la garde).</summary>
        public void Teleport(Vector3 position)
        {
            body.enabled = false;
            transform.position = position;
            lastPosition = position;
            node = null;
            think = 0f;
        }

        /// <summary>Le rival dont c'est la stele, ou null.</summary>
        public static Rival Of(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].seeker == s) return All[i];
            return null;
        }

        /// <summary>Quelqu'un vient de voler la relique posee sur la stele de victim.</summary>
        public static void NotifyTheft(Seeker victim, Seeker thief)
        {
            Rival r = Of(victim);
            if (r == null) return;
            r.huntTarget = thief;
            r.huntTimer = 75f;
            r.goal = Goal.Hunt;
            if (thief == Game.Me)
                Toasts.Show(victim.Name + " a vu sa stele pillee. Il te cherche.", victim.Colour);
        }

        /// <summary>Vrai si le rival se tient pres de sa stele : on ne pille pas sous son nez.</summary>
        public static bool IsGuarding(Seeker owner, Vector3 stele)
        {
            Rival r = Of(owner);
            if (r == null || r.stunTimer > 0f) return false;
            Vector3 d = r.transform.position - stele;
            d.y = 0f;
            return d.magnitude < 9f;
        }

        // ================================================================== boucle

        void Update()
        {
            Season season = Game.Season;
            if (season == null || !season.Running || Time.deltaTime <= 0f) return;
            float dt = Time.deltaTime;

            // Tombe : il se relevera a sa stele, vingt secondes plus tard.
            if (deadTimer > 0f)
            {
                deadTimer -= dt;
                if (deadTimer <= 0f) Revive();
                return;
            }
            if (stunTimer > 0f) { stunTimer -= dt; return; }
            if (aggroTimer > 0f) aggroTimer -= dt;
            if (fleeTimer > 0f) fleeTimer -= dt;
            if (strikeTimer > 0f) strikeTimer -= dt;
            if (seeker.Alive && Time.time - seeker.LastHurt > 8f) seeker.Heal(3f * dt);
            if (rearmTimer > 0f)
            {
                rearmTimer -= dt;
                // Il s'est refait une epee (il a du bois et du fer quelque part).
                if (rearmTimer <= 0f && armed && seeker.Kit.FreeSlot >= 0 && seeker.Kit.Held == null)
                {
                    seeker.Kit.Slots[seeker.Kit.FreeSlot] = new Tool(ToolKind.Epee);
                    seeker.Kit.Select(0);
                }
            }
            if (huntTimer > 0f) huntTimer -= dt;
            if (stealCooldown > 0f) stealCooldown -= dt;
            if (barkTimer > 0f) barkTimer -= dt;

            think -= dt;
            if (think <= 0f) { think = 0.7f; Think(season); }

            Act(season, dt);
            Notice();
            Animate(dt);
        }

        /// <summary>Choisir un but, du plus urgent au moins urgent.</summary>
        void Think(Season season)
        {
            Hoard h = seeker.Hoard;
            Mage mage = Game.Mage;

            // Il fuit : on l'a frappe alors qu'il ne peut pas se battre.
            if (fleeTimer > 0f)
            {
                Vector3 away = transform.position - fleeFrom;
                away.y = 0f;
                goal = Goal.Flee;
                target = transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 20f;
                return;
            }

            // Il se bat : on l'a frappe, ou un porteur de relique passe a sa portee.
            if (aggro == null) LookForPrey();
            if (aggro != null && aggroTimer > 0f && aggro.Alive && aggro.Body != null && seeker.CanStrike && seeker.Kit.Holding(ToolKind.Epee))
            {
                goal = Goal.Fight;
                target = aggro.Body.position;
                return;
            }
            aggro = null;

            // Sa stele est plantee d'office (SteleSites) ; sans elle, rien a faire.
            if (!h.StelePlanted) { goal = Goal.Gather; target = transform.position; return; }

            // LA MALEDICTION approche : il rentre vider son sac a sa stele, a temps.
            float curse = season.NextCurseIn;
            if (curse >= 0f && !seeker.Bag.IsEmpty && !season.MagePresent)
            {
                float eta = Flat(h.StelePosition - transform.position).magnitude / (WalkSpeed * 0.8f);
                if (curse < eta + 25f) { goal = Goal.Deposit; target = h.StelePosition; return; }
            }

            // Un trophee dans les mains : rentrer le fondre, vite.
            if (h.Trophy != null) { goal = Goal.ToStele; target = h.StelePosition; return; }

            // Il chasse celui qui l'a pille.
            if (huntTarget != null && huntTimer > 0f && huntTarget.Hoard.Trophy != null && huntTarget.Hoard.TrophyFrom == seeker)
            {
                goal = Goal.Hunt;
                if (huntTarget.Body != null) target = huntTarget.Body.position;
                return;
            }
            huntTarget = null;

            // La fin approche : la relique doit etre posee.
            bool noMageSoon = !season.MagePresent && (season.NextMageIn > 50f || season.NextMageIn < 0f);
            if (h.RelicInHand && (season.Remaining < 120f || noMageSoon || seeker.Bag.IsEmpty))
            {
                goal = Goal.ToStele; target = h.StelePosition; return;
            }

            // Le mage arrive et son sac est presque vide, mais sa reserve est pleine :
            // il passe d'abord la reprendre.
            bool mageSoon = mage != null && (mage.Announced || season.MagePresent && season.MageTimeLeft > 50f);
            if (mageSoon && seeker.Bag.Weight < 8f && h.Store != null && h.Store.Contents.TotalUnits >= 6)
            {
                goal = Goal.Resupply; target = h.StelePosition; return;
            }

            // Le mage chante -- ou sa colonne annonce ou il descendra -- et il a de
            // quoi forger : il y court, comme tout le monde.
            if (mage != null && !seeker.Bag.IsEmpty && !h.RelicOnStele
                && (season.MagePresent && season.MageTimeLeft > 12f || mage.Announced))
            {
                goal = Goal.ToMage; target = mage.Destination; return;
            }

            // Le mage va revenir : il va chercher sa relique sur sa stele pour la renforcer.
            if (h.RelicOnStele && season.NextMageIn >= 0f && season.NextMageIn < 45f && seeker.Bag.Weight > 20f)
            {
                goal = Goal.FetchRelic; target = h.StelePosition; return;
            }
            if (h.RelicOnStele && season.MagePresent && seeker.Bag.Weight > 20f && season.MageTimeLeft > 60f)
            {
                goal = Goal.FetchRelic; target = h.StelePosition; return;
            }

            // Piller la stele du joueur, s'il la connait et que le joueur est loin.
            if (goal == Goal.Steal || WantsToSteal()) { goal = Goal.Steal; target = Game.Me.Hoard.StelePosition; return; }

            // Sac lourd : il rentre le deposer (la Malediction ne pardonne pas).
            if (seeker.Bag.Load01 > 0.75f) { goal = Goal.Deposit; target = h.StelePosition; return; }

            goal = Goal.Gather;
            if (node == null || node.IsDepleted || seeker.Bag.SpaceFor(node.type) <= 0) node = ChooseNode();
            target = node != null ? node.transform.position : h.StelePosition + GuardOffset();
        }

        bool WantsToSteal()
        {
            Seeker me = Game.Me;
            if (me == null || stealCooldown > 0f || !seeker.Knows(me)) return false;
            Hoard mine = me.Hoard;
            if (!mine.StelePlanted) return false;
            bool relic = mine.RelicOnStele && mine.Relic != null && seeker.Hoard.Trophy == null;
            bool store = mine.Store != null && mine.Store.Contents.TotalUnits >= 8 && seeker.Bag.Load01 < 0.5f;
            if (!relic && !store) return false;
            if (me.Body != null && Flat(me.Body.position - mine.StelePosition).magnitude < 45f) return false;
            stealCooldown = 25f;
            return rng.NextDouble() < aggression;
        }

        Vector3 GuardOffset()
        {
            float a = (seeker.Name.Length * 1.3f) % 6.28f;
            return new Vector3(Mathf.Cos(a) * 3f, 0f, Mathf.Sin(a) * 3f);
        }

        // ================================================================== agir

        void Act(Season season, float dt)
        {
            Hoard h = seeker.Hoard;
            float speed = Mathf.Lerp(WalkSpeed, WalkSpeed * 0.75f, seeker.Bag.Load01);
            if (goal == Goal.Hunt || goal == Goal.ToStele && h.Trophy != null) speed = RunSpeed * (h.Trophy != null ? 0.85f : 1f);

            float distance = Flat(target - transform.position).magnitude;
            float reach = goal == Goal.Gather ? 2.2f : goal == Goal.Hunt ? 1.6f : goal == Goal.Fight ? 1.9f : 2.4f;
            if (goal == Goal.Fight || goal == Goal.Flee) speed = RunSpeed;

            if (distance > reach)
            {
                work = 0f;
                if (goal == Goal.ToMage && Game.Mage != null) target = Game.Mage.Destination;
                if (goal == Goal.Hunt && huntTarget != null && huntTarget.Body != null) target = huntTarget.Body.position;
                if (goal == Goal.Fight && aggro != null && aggro.Body != null) target = aggro.Body.position;
                Walk(target, speed, dt);
                return;
            }

            switch (goal)
            {
                case Goal.Gather:
                    Harvest(dt);
                    break;

                case Goal.Deposit:
                    h.RequestStoreAll(seeker.Bag);
                    if (h.RelicInHand) h.TryPlaceOnStele();
                    seeker.SyncWeight();
                    SetTraps();
                    think = 0f;
                    break;

                case Goal.Resupply:
                    h.RequestTakeAll(seeker.Bag);
                    seeker.SyncWeight();
                    think = 0f;
                    break;

                case Goal.ToMage:
                    if (Game.Mage != null && Game.Mage.Present)
                    {
                        Relic relic = h.EnsureRelic();
                        int before = relic.Power;
                        if (relic.RequestForge(seeker.Bag) > 0)
                        {
                            seeker.SyncWeight();
                            if (PlayerWithin(30f))
                            {
                                Game.Mage.PlayForge();
                                FloatingTexts.Spawn(transform.position + Vector3.up * 2.4f,
                                                    seeker.Name + " forge  +" + (relic.Power - before), seeker.Colour);
                            }
                        }
                    }
                    think = 0f;
                    break;

                case Goal.ToStele:
                    if (h.Trophy != null)
                    {
                        int gained = h.RequestAbsorbTrophy();
                        if (h.RelicInHand) h.TryPlaceOnStele();
                        seeker.SyncWeight();
                        Toasts.Show(seeker.Name + " a fondu une relique volee dans la sienne (+" + gained + ").", seeker.Colour);
                    }
                    else if (h.RelicInHand) h.TryPlaceOnStele();
                    seeker.SyncWeight();
                    think = 0f;
                    break;

                case Goal.FetchRelic:
                    h.TryTakeFromStele();
                    seeker.SyncWeight();
                    think = 0f;
                    break;

                case Goal.Steal:
                    StealFromPlayer(dt);
                    break;

                case Goal.Hunt:
                    Catch();
                    break;

                case Goal.Fight:
                    // A portee : un coup d'epee toutes les 1,1 s.
                    if (strikeTimer <= 0f && aggro != null && seeker.CanStrike)
                    {
                        strikeTimer = 1.1f;
                        if (rig != null) rig.PlaySwing();
                        if (Game.Rig != null && PlayerWithin(25f)) Sfx.HarvestTap(ResourceType.Iron);
                        if (seeker.Kit.Wear(1)) rearmTimer = 60f;
                        Combat.Hit(aggro, seeker, 20f);
                        if (!aggro.Alive) { aggro = null; Bark("Et voila."); }
                    }
                    if (aggro != null && aggro.Body != null) Figures.Face(transform, aggro.Body.position, 360f);
                    break;

                case Goal.Flee:
                    think = 0f;
                    break;

                case Goal.Guard:
                    // Il reste la, et se tourne de temps en temps.
                    transform.Rotate(0f, 20f * dt * Mathf.Sin(Time.time * 0.3f + seeker.Name.Length), 0f);
                    break;
            }
        }

        /// <summary>
        /// Les rivaux agressifs piegent les abords de leur stele, avec ce qui dort
        /// dans leur reserve (3 bois mort, 2 fer par piege) : piller Mahaut, c'est
        /// regarder ou l'on pose les pieds.
        /// </summary>
        void SetTraps()
        {
            Hoard h = seeker.Hoard;
            if (aggression < 0.3f || h.Store == null) return;
            int wanted = aggression >= 0.6f ? 3 : 2;
            int[] cost = Kit.Cost(ToolKind.Piege);
            for (int n = Trap.CountOf(seeker); n < wanted; n++)
            {
                for (int i = 0; i < cost.Length; i++) if (h.Store.Contents.Get((ResourceType)i) < cost[i]) return;
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
                for (int i = 0; i < cost.Length; i++) h.Store.Contents.TryRemove((ResourceType)i, cost[i]);
                Trap.Place(seeker, at, (float)rng.NextDouble() * 360f);
            }
        }

        void Harvest(float dt)
        {
            if (node == null || node.IsDepleted) { think = 0f; return; }
            float penalty = Mathf.Lerp(1f, 2.4f, seeker.Bag.Load01);
            work += dt;
            if (work < node.harvestDuration * penalty * 1.15f) return;
            work = 0f;
            node.TryTakeFor(seeker.Bag, node.type == ResourceType.Deadwood ? 2 : 1);
            if (node.IsDepleted || seeker.Bag.SpaceFor(node.type) <= 0) think = 0f;
        }

        /// <summary>
        /// Choisir un gisement. Fer du chateau pour ceux qui l'aiment (s'il en reste),
        /// sinon pierre-lune a portee, sinon bois mort. Le plus proche qui a encore
        /// quelque chose.
        /// </summary>
        ResourceNode ChooseNode()
        {
            ResourceType wanted = ResourceType.Deadwood;
            double roll = rng.NextDouble();
            if (roll < ironLove) wanted = ResourceType.Iron;
            else if (roll < ironLove + 0.55) wanted = ResourceType.Moonstone;

            ResourceNode best = null;
            float bestD = float.MaxValue;
            float limit = wanted == ResourceType.Iron ? 600f : wanted == ResourceType.Moonstone ? 180f : 90f;
            for (int pass = 0; pass < 2 && best == null; pass++)
            {
                for (int i = 0; i < ResourceNode.All.Count; i++)
                {
                    ResourceNode n = ResourceNode.All[i];
                    if (n == null || n.IsDepleted) continue;
                    if (pass == 0 && n.type != wanted) continue;
                    if (seeker.Bag.SpaceFor(n.type) <= 0) continue;
                    float d = Flat(n.transform.position - transform.position).magnitude;
                    if (pass == 0 && d > limit) continue;
                    if (d < bestD) { bestD = d; best = n; }
                }
            }
            return best;
        }

        void StealFromPlayer(float dt)
        {
            Seeker me = Game.Me;
            Hoard mine = me != null ? me.Hoard : null;
            bool relic = mine != null && mine.RelicOnStele && mine.Relic != null && seeker.Hoard.Trophy == null;
            bool store = mine != null && mine.Store != null && !mine.Store.Contents.IsEmpty && seeker.Bag.Load01 < 0.95f;
            if (!relic && !store)
            {
                goal = Goal.Gather; think = 0f; stealTimer = 0f;
                return;
            }
            // Le joueur arrive : il file.
            if (PlayerWithin(10f)) { goal = Goal.Gather; think = 0f; stealTimer = 0f; Bark("Rien. Je passais."); return; }

            stealTimer += dt;
            if (stealTimer < 3f) return;
            stealTimer = 0f;

            string took = "";
            if (relic)
            {
                Relic taken = mine.TrySurrenderRelic();
                if (taken != null && seeker.Hoard.TryTakeTrophy(taken, me)) took = "ta relique (puissance " + taken.Power + ")";
            }
            int units = mine.RequestLoot(seeker.Bag);
            if (units > 0) took += (took.Length > 0 ? " et " : "") + units + " ressources de ta reserve";
            me.SyncWeight();
            seeker.SyncWeight();
            goal = seeker.Hoard.Trophy != null ? Goal.ToStele : Goal.Deposit;
            target = seeker.Hoard.StelePosition;
            think = 0.7f;
            if (took.Length == 0) return;

            Sfx.Deny();
            if (Game.Hud != null && me.Body != null)
                Game.Hud.ShowDiscovery("ALERTE", seeker.Name + " a pille ta stele",
                                       "Il emporte " + took + ". Il file " + Hud.Direction(me.Body.position, transform.position) + ".",
                                       seeker.Hoard.Trophy != null ? "Rattrape-le avant qu'il la fonde a sa stele : E pour la reprendre."
                                                                   : "Abats-le : tout ce qu'il porte tombera dans sa depouille.",
                                       new Color(1f, 0.4f, 0.3f));
        }

        void Catch()
        {
            Seeker prey = huntTarget;
            if (prey == null || prey.Hoard.Trophy == null || prey.Hoard.TrophyFrom != seeker) { huntTarget = null; think = 0f; return; }
            Relic back = prey.Hoard.TrySurrenderTrophy();
            seeker.Hoard.TryRecover(back);
            prey.SyncWeight();
            seeker.SyncWeight();
            huntTarget = null;
            goal = Goal.ToStele;
            think = 0.7f;
            Bark("C'est a moi.");
            if (prey == Game.Me && Game.Hud != null)
                Game.Hud.ShowDiscovery("RATTRAPE", seeker.Name + " reprend sa relique", "",
                                       "", seeker.Colour);
            Sfx.Deny();
        }

        /// <summary>Ce qu'il remarque en passant : la stele du joueur.</summary>
        void Notice()
        {
            Seeker me = Game.Me;
            if (me == null || seeker.Knows(me) || !me.Hoard.StelePlanted) return;
            if (Flat(me.Hoard.StelePosition - transform.position).magnitude < 9f)
            {
                seeker.Discover(me);
                if (PlayerWithin(25f)) Bark("Tiens. Une stele.");
            }
        }

        // ================================================================== le combat

        /// <summary>Il en a une (Mahaut et Oswin partent armes, Guerin non).</summary>
        public bool armed;

        public void Arm()
        {
            armed = true;
            seeker.Kit.Slots[0] = new Tool(ToolKind.Epee);
            seeker.Kit.Select(0);
        }

        /// <summary>On vient de le frapper. S'il peut se battre, il se retourne ; sinon il fuit.</summary>
        public void OnHit(Seeker attacker)
        {
            if (attacker == null || !seeker.Alive) return;
            bool canFight = seeker.CanStrike && seeker.Kit.Holding(ToolKind.Epee) && seeker.Health > 35f;
            if (canFight)
            {
                aggro = attacker;
                aggroTimer = 25f;
                Bark("Tu vas le regretter.");
            }
            else
            {
                fleeTimer = 6f;
                fleeFrom = attacker.Body != null ? attacker.Body.position : transform.position;
                Bark(seeker.Hoard.RelicInHand || seeker.Hoard.Trophy != null ? "Pas la relique !" : "Laisse-moi !");
            }
            think = 0f;
        }

        /// <summary>
        /// Un porteur de relique (toi, avec la tienne ou une volee) passe a moins de
        /// douze metres : les rivaux armes et agressifs tentent leur chance.
        /// </summary>
        void LookForPrey()
        {
            Seeker me = Game.Me;
            if (me == null || !me.Alive || me.Body == null || !armed || !seeker.CanStrike) return;
            bool carrying = me.Hoard.RelicInHand || me.Hoard.Trophy != null;
            if (!carrying || !PlayerWithin(12f)) return;
            if (rng.NextDouble() > aggression * 0.08f) return;       // une chance par reflexion, selon son caractere
            aggro = me;
            aggroTimer = 20f;
            Bark("Donne-moi ca.");
        }

        public void Die()
        {
            deadTimer = 20f;
            aggro = null;
            huntTarget = null;
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
            if (armed) rearmTimer = 60f;
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
                    if (stuck > 0.35f) { detourTimer = 1.1f; detourSign = -detourSign; stuck = 0f; }
                }
                else stuck = 0f;
            }
            else
            {
                Vector3 p = transform.position + dir * speed * dt;
                p.y = Ground.Sample(p.x, p.z);
                transform.position = p;
            }

            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 360f * dt);
        }

        void Animate(float dt)
        {
            if (figure == null) return;

            // Loin de toi (plus de 45 m, bien au-dela de la brume), le corps s'eteint :
            // son poncho n'a plus a etre simule, sa lanterne n'eclaire personne.
            bool near = PlayerWithin(45f);
            if (figure.gameObject.activeSelf != near) figure.gameObject.SetActive(near);

            // Le corps s'anime tout seul : il lui suffit de savoir a quelle vitesse on va.
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
            barkTimer = 8f;
            FloatingTexts.Spawn(transform.position + Vector3.up * 2.3f, seeker.Name + " : " + line, seeker.Colour);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }

        bool CarriesMine { get { return Game.Me != null && seeker.Hoard.Trophy != null && seeker.Hoard.TrophyFrom == Game.Me; } }
        bool Pickable { get { return seeker.Hoard.RelicInHand && goal == Goal.Gather && work > 0f && Game.Me != null && Game.Me.Hoard.Trophy == null; } }

        public bool CanInteract { get { return Game.Season == null || !Game.Season.Over; } }

        public string Prompt
        {
            get
            {
                if (CarriesMine) return "REPRENDRE ta relique a " + seeker.Name;
                if (Pickable) return "Detrousser " + seeker.Name + " (il a sa relique sur lui)";
                return "Parler a " + seeker.Name;
            }
        }

        public float HoldDuration { get { return CarriesMine ? 1.2f : Pickable ? 2.5f : 0f; } }

        public void Interact()
        {
            Seeker me = Game.Me;
            if (me == null) return;

            if (CarriesMine)
            {
                Relic mine = seeker.Hoard.TrySurrenderTrophy();
                me.Hoard.TryRecover(mine);
                me.SyncWeight();
                seeker.SyncWeight();
                stunTimer = 4f;
                goal = Goal.Gather;
                Sfx.Discovery();
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("REPRISE", "Ta relique est a toi", "Repose-la vite sur ta stele.", "", Stele.RuneBlue);
                return;
            }

            if (Pickable)
            {
                Relic taken = seeker.Hoard.TrySurrenderRelic();
                if (taken == null || !me.Hoard.TryTakeTrophy(taken, seeker)) return;
                me.SyncWeight();
                seeker.SyncWeight();
                NotifyTheft(seeker, me);
                Sfx.Discovery();
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("DETROUSSE", "La relique de " + seeker.Name,
                                           "Puissance " + taken.Power + ". Cours a ta stele.", "Il est juste derriere toi.", seeker.Colour);
                return;
            }

            talks++;
            if (Game.Hud != null) Game.Hud.OpenPanel(new DialoguePanel(this));
            Sfx.Pop();
        }

        // ================================================================== IDialogue

        public string Speaker { get { return seeker.Name.ToUpperInvariant(); } }
        public Color Tint { get { return seeker.Colour; } }

        public string Body
        {
            get
            {
                string line = taunts[(talks - 1 + taunts.Length) % taunts.Length];
                int mine = Game.Me != null ? Game.Me.Score : 0;
                int his = seeker.Score;
                string compare;
                if (his <= 0 && mine <= 0) compare = "\"Personne n'a rien pose. Ca ne durera pas.\"";
                else if (his > mine) compare = "\"Ma relique vaut " + his + ". La tienne ? Je ne la vois pas d'ici.\"";
                else compare = "\"Tu menes. Profite. La nuit est longue.\"";
                string doing = goal == Goal.Gather ? "Il ramasse, sans te quitter des yeux."
                             : goal == Goal.Guard ? "Il garde sa stele, adosse a la pierre."
                             : goal == Goal.ToMage ? "Il a l'air presse : il entend le mage."
                             : "Il a l'air de savoir ou il va.";
                return "\"" + line + "\"\n\n" + compare + "\n\n" + doing;
            }
        }

        public int ChoiceCount { get { return 1; } }
        public string ChoiceLabel(int index) { return "Partir"; }
        public bool ChoiceEnabled(int index) { return true; }
        public bool Choose(int index) { return true; }
    }
}
