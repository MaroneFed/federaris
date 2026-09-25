using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QUI VEUT TON MAL (Martin, 25/09 : "faut plus de mechants et de trucs qui
    /// veulent notre mal"). Deux sortes, un seul cerveau :
    ///
    ///   LES LOUPS   en meutes de trois, ils rodent dans la sylve. On voit d'abord
    ///               leurs YEUX, deux points jaunes dans la brume. Plus rapides que
    ///               toi a pied, moins qu'a la course -- a vide. Charge, il faut se
    ///               battre. Deux coups d'epee en viennent a bout.
    ///   LES REVENANTS   ils gardent les Autels, pres du chateau. Lents, durs a
    ///               abattre, ils ne s'eloignent jamais de leur pierre. Chacun porte
    ///               un peu d'or.
    ///
    /// Ils attaquent tout le monde : toi et les rivaux. Un rival arme se defend. Un
    /// piege les tue comme il tue un homme.
    ///
    /// LE CERVEAU, un petit automate : ERRER autour de son repaire -> CHASSER qui
    /// entre dans son champ -> MORDRE a portee -> RENTRER s'il s'eloigne trop du
    /// repaire (la laisse). Mort, il revient au repaire deux a trois minutes plus
    /// tard.
    /// </summary>
    public class Beast : MonoBehaviour
    {
        public static readonly List<Beast> All = new List<Beast>();

        public enum Kind { Loup, Revenant }
        enum State { Roam, Chase, Return, Dead }

        public Kind kind;
        public float Health;
        public bool Alive { get { return state != State.Dead; } }

        float maxHealth, damage, bite, speed, sight, leash, respawn;
        int goldCarried;
        Vector3 home;
        Vector3 wander;
        float wanderTimer;
        State state = State.Roam;
        float windup;
        float fear;
        /// <summary>Angle d'approche de ce loup dans sa meute (-60, 0, +60).</summary>
        float flank;
        /// <summary>Les autres loups de sa meute.</summary>
        List<Beast> pack;

        bool Afraid
        {
            get
            {
                if (pack == null) return false;
                int dead = 0;
                for (int i = 0; i < pack.Count; i++) if (pack[i] != null && !pack[i].Alive) dead++;
                return dead >= 2;
            }
        }
        Seeker prey;
        float biteTimer;
        float deadTimer;
        float growlTimer;
        CharacterController body;
        float fall;
        float stuck, detour;
        float detourSign = 1f;
        Transform figure;
        Transform[] legs = new Transform[0];
        Walker walker;
        float gait;
        System.Random rng;

        static readonly Color Fur = new Color(0.2f, 0.19f, 0.18f);
        static readonly Color FurLight = new Color(0.3f, 0.28f, 0.26f);
        static readonly Color WolfEyes = new Color(1f, 0.82f, 0.3f);
        static readonly Color GhostEyes = new Color(0.55f, 0.95f, 1f);

        // ================================================================== construction

        public static Beast Wolf(Transform parent, Vector3 at, int seed)
        {
            Beast b = Make(parent, at, Kind.Loup, seed, 0.9f, 0.35f);
            b.maxHealth = 45f; b.damage = 14f; b.bite = 1.2f; b.speed = 9.2f; b.sight = 16f; b.leash = 55f; b.respawn = 150f;

            Proto.BeginVisualOnly();
            Transform f = b.figure;
            Proto.Cube(f, new Vector3(0f, 0.62f, -0.05f), new Vector3(0.36f, 0.34f, 0.95f), Fur, "Corps");
            Proto.Cube(f, new Vector3(0f, 0.7f, 0.36f), new Vector3(0.42f, 0.42f, 0.36f), FurLight, "Poitrail");
            Proto.Cube(f, new Vector3(0f, 0.8f, 0.2f), new Vector3(0.3f, 0.12f, 0.5f), Palette.Shade(Fur, 0.8f), "Échine");
            Proto.Cube(f, new Vector3(0f, 0.86f, 0.66f), new Vector3(0.3f, 0.28f, 0.3f), Fur, "Tête");
            Proto.Cube(f, new Vector3(0f, 0.8f, 0.88f), new Vector3(0.16f, 0.14f, 0.24f), FurLight, "Museau");
            Proto.Cube(f, new Vector3(0f, 0.83f, 1.0f), new Vector3(0.07f, 0.06f, 0.04f), new Color(0.05f, 0.05f, 0.05f), "Truffe");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject ear = Proto.Cone(f, new Vector3(side * 0.1f, 0.98f, 0.62f), 0.06f, 0.14f, Fur, "Oreille", 4);
                ear.transform.localRotation = Quaternion.Euler(-10f, 0f, side * 12f);
                GameObject eye = Proto.Cube(f, new Vector3(side * 0.08f, 0.9f, 0.8f), new Vector3(0.05f, 0.03f, 0.02f), WolfEyes, "Oeil");
                eye.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(WolfEyes, 3.2f);
            }
            GameObject tail = Proto.Cube(f, new Vector3(0f, 0.66f, -0.62f), new Vector3(0.1f, 0.1f, 0.44f), Fur, "Queue");
            tail.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            b.legs = new Transform[4];
            Vector3[] hips = { new Vector3(-0.13f, 0.5f, 0.32f), new Vector3(0.13f, 0.5f, 0.32f), new Vector3(-0.13f, 0.5f, -0.38f), new Vector3(0.13f, 0.5f, -0.38f) };
            for (int i = 0; i < 4; i++)
            {
                Transform leg = Walker.Node(f, hips[i], "Patte");
                Proto.Cube(leg, new Vector3(0f, -0.24f, 0f), new Vector3(0.09f, 0.5f, 0.1f), i < 2 ? FurLight : Fur, "Patte");
                b.legs[i] = leg;
            }
            Proto.EndVisualOnly();
            return b;
        }

        /// <summary>Chasse-t-il ce chercheur, en ce moment ?</summary>
        public bool Hunting(Seeker s)
        {
            return state == State.Chase && prey == s;
        }

        public static Beast Revenant(Transform parent, Vector3 at, int seed)
        {
            Beast b = Make(parent, at, Kind.Revenant, seed, 1.9f, 0.4f);
            // Il rale : une voix creuse qu'on entend a vingt-cinq metres.
            AudioSource voice = b.gameObject.AddComponent<AudioSource>();
            voice.clip = Sfx.Moan();
            voice.loop = true;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 3f;
            voice.maxDistance = 25f;
            voice.dopplerLevel = 0f;
            voice.volume = 0.5f;
            voice.pitch = 0.9f + (seed % 5) * 0.05f;
            voice.Play();
            b.maxHealth = 90f; b.damage = 22f; b.bite = 1.6f; b.speed = 4.6f; b.sight = 13f; b.leash = 18f; b.respawn = 180f;
            b.goldCarried = 6;

            Walker.Look look = new Walker.Look();
            look.skin = new Color(0.36f, 0.4f, 0.42f);
            look.shirt = new Color(0.14f, 0.15f, 0.15f);
            look.legs = new Color(0.12f, 0.12f, 0.12f);
            look.boots = new Color(0.1f, 0.1f, 0.1f);
            look.robe = true;
            look.robeColor = new Color(0.17f, 0.18f, 0.17f);
            look.robeDark = new Color(0.1f, 0.11f, 0.11f);
            look.height = 2.0f;
            Walker w = Walker.Build(b.figure, "Revenant", look);
            w.Stoop = 10f;
            w.HoldPole = true;
            w.RunSpeed = b.speed;
            b.walker = w;

            Proto.BeginVisualOnly();
            Transform head = w.Head;
            Proto.Cube(head, new Vector3(0f, 0.2f, -0.03f), new Vector3(0.3f, 0.26f, 0.3f), look.robeDark, "Capuchon");
            Material eyes = MaterialFactory.GetGlow(GhostEyes, 2.6f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject eye = Proto.Cube(head, new Vector3(side * 0.055f, 0.15f, 0.13f), new Vector3(0.05f, 0.025f, 0.02f), GhostEyes, "Oeil");
                eye.GetComponent<Renderer>().sharedMaterial = eyes;
            }
            // Une vieille lame rouillee, tenue droite.
            Transform blade = w.Holder(w.HandR, "Lame");
            Proto.Cube(blade, new Vector3(0f, 0.35f, 0f), new Vector3(0.05f, 0.9f, 0.015f), new Color(0.36f, 0.26f, 0.2f), "Lame");
            Proto.Cube(blade, new Vector3(0f, -0.08f, 0f), new Vector3(0.22f, 0.03f, 0.04f), new Color(0.25f, 0.22f, 0.2f), "Garde");
            Proto.EndVisualOnly();

            // Une lueur froide autour de lui : on le voit venir.
            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(b.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.6f, 0.3f);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = GhostEyes;
            l.range = 4f;
            l.intensity = 0.8f;
            l.shadows = LightShadows.None;
            return b;
        }

        static Beast Make(Transform parent, Vector3 at, Kind kind, int seed, float height, float radius)
        {
            GameObject go = new GameObject(kind == Kind.Loup ? "LOUP" : "REVENANT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = height;
            cc.radius = radius;
            cc.center = new Vector3(0f, height * 0.5f, 0f);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 55f;
            Beast b = go.AddComponent<Beast>();
            b.kind = kind;
            b.home = at;
            b.body = cc;
            b.rng = new System.Random(seed);
            b.figure = Walker.Node(go.transform, Vector3.zero, "Silhouette");
            All.Add(b);
            b.Health = 1f;
            return b;
        }

        void Start()
        {
            Health = maxHealth;
            wander = home;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        // ================================================================== vie

        void Update()
        {
            Season season = Game.Season;
            if (season == null || !season.Running || Time.deltaTime <= 0f) return;
            float dt = Time.deltaTime;

            if (state == State.Dead)
            {
                Collapse(dt);
                deadTimer -= dt;
                if (deadTimer <= 0f) Revive();
                return;
            }
            if (biteTimer > 0f) biteTimer -= dt;
            if (growlTimer > 0f) growlTimer -= dt;

            // Loin de tout le monde, il dort : pas besoin de le faire vivre.
            Transform player = Game.PlayerTransform;
            float toPlayer = player != null ? Flat(player.position - transform.position).magnitude : 999f;
            if (toPlayer > 140f && state == State.Roam) return;

            switch (state)
            {
                case State.Roam: Roam(dt); break;
                case State.Chase: Chase(dt); break;
                case State.Return: Return(dt); break;
            }
            Animate(dt);
        }

        void Roam(float dt)
        {
            wanderTimer -= dt;
            if (wanderTimer <= 0f || Flat(wander - transform.position).magnitude < 1.5f)
            {
                wanderTimer = 6f + (float)rng.NextDouble() * 8f;
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * leash * 0.5f;
                wander = home + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
            Move(wander, speed * 0.3f, dt);
            if (fear > 0f) { fear -= dt; return; }

            Seeker target = Spot();
            if (target != null)
            {
                prey = target;
                state = State.Chase;
                Cry();
            }
        }

        /// <summary>Le plus proche de ceux qu'il voit : toi ou un rival, vivant, dans son champ.</summary>
        Seeker Spot()
        {
            Seeker best = null;
            float bestD = sight;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == null || !s.Alive || s.Body == null) continue;
                float d = Flat(s.Body.position - transform.position).magnitude;
                if (Mathf.Abs(s.Body.position.y - transform.position.y) > 3f) continue;       // perche dans un arbre : hors d'atteinte
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        void Chase(float dt)
        {
            if (prey == null || !prey.Alive || prey.Body == null) { state = State.Return; return; }
            Vector3 p = prey.Body.position;
            // Trop loin du repaire, ou la proie est hors d'atteinte (dans un arbre) : il rentre.
            if (Flat(transform.position - home).magnitude > leash || Mathf.Abs(p.y - transform.position.y) > 3f)
            {
                state = State.Return;
                prey = null;
                return;
            }
            // LA PEUR : une meute qui a perdu deux des siens s'enfuit.
            if (Afraid) { fear = 20f; state = State.Return; prey = null; return; }

            float d = Flat(p - transform.position).magnitude;
            if (d > 1.7f)
            {
                windup = 0f;
                // L'ENCERCLEMENT : de loin, chaque loup vise un point decale autour de
                // la proie (a gauche, en face, a droite) ; il ne ferme qu'a 4 m.
                Vector3 goal = p;
                if (kind == Kind.Loup && d > 4f)
                {
                    Vector3 from = Flat(transform.position - p).normalized;
                    goal = p + Quaternion.Euler(0f, flank, 0f) * from * 3f;
                }
                Move(goal, speed, dt);
                return;
            }

            Figures.Face(transform, p, 540f);
            if (biteTimer > 0f) return;

            // LE TELEGRAPHE du revenant : il leve sa lame 0,6 s avant de frapper. Qui
            // recule a temps n'est pas touche.
            if (kind == Kind.Revenant)
            {
                if (windup <= 0f)
                {
                    windup = 0.001f;
                    if (walker != null) walker.PlaySwing();
                    return;
                }
                windup += dt;
                if (windup < 0.6f) return;
                windup = 0f;
                biteTimer = bite;
                if (Flat(prey.Body.position - transform.position).magnitude > 2.3f) return;     // esquive
            }
            else biteTimer = bite;
            Combat.Hit(prey, null, damage, kind == Kind.Loup ? "sous les crocs d'un loup" : "sous la lame d'un revenant");
            if (!prey.IsPlayer)
            {
                Defend(prey);
                // A bout de forces, un rival s'enfuit.
                Rival r = Rival.Of(prey);
                if (r != null && prey.Alive && (prey.Health < 40f || !prey.Kit.Holding(ToolKind.Epee))) r.FleeFrom(transform.position);
            }
            if (!prey.Alive) { prey = null; state = State.Return; }
        }

        /// <summary>Un rival arme rend les coups (pas de combat simule plus fin : il frappe quand on le mord).</summary>
        void Defend(Seeker rival)
        {
            if (!rival.CanStrike || !rival.Kit.Holding(ToolKind.Epee)) return;
            rival.Kit.Wear(1);
            Hurt(28f, rival);
        }

        void Return(float dt)
        {
            if (fear > 0f) fear -= dt;
            Move(home, fear > 0f ? speed : speed * 0.6f, dt);
            if (Flat(home - transform.position).magnitude < 3f) state = State.Roam;
            // Il ne rentre pas bredouille si quelqu'un repasse sous son nez.
            Seeker target = fear <= 0f && Flat(home - transform.position).magnitude < leash * 0.7f ? Spot() : null;
            if (target != null) { prey = target; state = State.Chase; }
        }

        void Move(Vector3 destination, float v, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            float d = to.magnitude;
            if (d < 0.05f) return;
            Vector3 dir = to / d;
            if (detour > 0f)
            {
                detour -= dt;
                dir = Quaternion.Euler(0f, 70f * detourSign, 0f) * dir;
            }
            fall = body.isGrounded ? -1f : fall - 22f * dt;
            Vector3 before = transform.position;
            body.Move((dir * v + Vector3.up * fall) * dt);
            float moved = Flat(transform.position - before).magnitude;
            if (moved < v * dt * 0.3f)
            {
                stuck += dt;
                if (stuck > 0.4f) { detour = 1f; detourSign = -detourSign; stuck = 0f; }
            }
            else stuck = 0f;
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 400f * dt);
            gait += moved * 3.2f;
        }

        void Animate(float dt)
        {
            if (legs.Length == 4)
            {
                float s = Mathf.Sin(gait);
                float swing = state == State.Chase ? 38f : 22f;
                legs[0].localRotation = Quaternion.Euler(s * swing, 0f, 0f);
                legs[3].localRotation = Quaternion.Euler(s * swing, 0f, 0f);
                legs[1].localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
                legs[2].localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
                figure.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Cos(gait)) * 0.05f, 0f);
            }
        }

        /// <summary>Le cri de chasse : un hurlement pour les loups, un rale pour les revenants.</summary>
        void Cry()
        {
            if (growlTimer > 0f) return;
            growlTimer = 20f;
            Transform player = Game.PlayerTransform;
            if (player == null || Flat(player.position - transform.position).magnitude > 60f) return;
            if (kind == Kind.Loup) AudioSource.PlayClipAtPoint(Sfx.Howl(), transform.position + Vector3.up, 0.9f);
            else Sfx.CurseToll();
            if (prey == Game.Me)
                Toasts.Show(kind == Kind.Loup ? "Des loups !" : "Un revenant se lève.",
                            new Color(0.95f, 0.5f, 0.35f));
        }

        // ================================================================== coups et mort

        /// <summary>Frappe par quelqu'un (toi, un rival arme).</summary>
        public void Hurt(float amount, Seeker by)
        {
            if (state == State.Dead) return;
            Health -= amount;
            Sfx.Thud();
            Ambiance.Burst(null, transform.position + Vector3.up * (kind == Kind.Loup ? 0.7f : 1.4f),
                           kind == Kind.Loup ? new Color(0.5f, 0.1f, 0.08f) : GhostEyes);
            if (by != null && by.Body != null) Punch.Apply(figure, by.Body.position);
            FloatingTexts.Spawn(transform.position + Vector3.up * (kind == Kind.Loup ? 1.3f : 2.4f), "-" + Mathf.RoundToInt(amount),
                                new Color(1f, 0.6f, 0.4f));
            if (by != null) { prey = by; state = State.Chase; }
            if (Health <= 0f) Die(by);
        }

        public void Die(Seeker killer)
        {
            if (state == State.Dead) return;
            state = State.Dead;
            if (killer != null && killer.IsPlayer) Stats.BeastsDowned++;
            deadTimer = respawn;
            prey = null;
            Ambiance.Burst(null, transform.position + Vector3.up * 0.8f, kind == Kind.Loup ? new Color(0.5f, 0.12f, 0.1f) : GhostEyes);
            if (killer != null && goldCarried > 0)
            {
                killer.Money.Add(goldCarried);
                if (killer.IsPlayer) { Sfx.Coin(); Toasts.Show("+" + goldCarried + " or", Palette.Gold); }
            }
            else if (killer != null && killer.IsPlayer)
                Toasts.Show("Le loup s'effondre.", UiStyle.InkDim);
            body.enabled = false;
            dying = 0f;
            AudioSource voiceOff = GetComponent<AudioSource>();
            if (voiceOff != null) voiceOff.Stop();
            Light l = GetComponentInChildren<Light>();
            if (l != null) l.enabled = false;
        }

        // LA CHUTE : la bete bascule sur le flanc (0,4 s), reste la un moment, puis
        // s'enfonce dans la terre et disparait. Plus de disparition d'un coup.
        float dying = -1f;

        void Collapse(float dt)
        {
            if (dying < 0f) return;
            dying += dt;
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dying / 0.4f));
            float sink = Mathf.Clamp01((dying - 2.5f) / 2f);
            figure.localRotation = Quaternion.Euler(0f, 0f, 88f * fall);
            figure.localPosition = new Vector3(0f, (kind == Kind.Loup ? 0.25f : 0.2f) * fall - sink * 1.2f, 0f);
            if (dying > 4.5f)
            {
                figure.gameObject.SetActive(false);
                dying = -1f;
            }
        }

        void Revive()
        {
            state = State.Roam;
            Health = maxHealth;
            transform.position = home + Vector3.up * 0.2f;
            body.enabled = true;
            dying = -1f;
            AudioSource voiceOn = GetComponent<AudioSource>();
            if (voiceOn != null) voiceOn.Play();
            figure.localRotation = Quaternion.identity;
            figure.localPosition = Vector3.zero;
            figure.gameObject.SetActive(true);
            Light l = GetComponentInChildren<Light>(true);
            if (l != null) l.enabled = true;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        // ================================================================== les meutes

        /// <summary>
        /// Trois meutes de trois loups, chacune avec son repaire, tirees au hasard a
        /// chaque partie : loin du chateau, et a bonne distance des steles (on ne nait
        /// pas au milieu des loups).
        /// </summary>
        public static void SpawnPacks(Transform parent, int packs)
        {
            GameObject root = new GameObject("LOUPS");
            root.transform.SetParent(parent, false);
            System.Random rng = new System.Random(System.Environment.TickCount ^ 0x5bd1);
            float half = (Game.Config != null ? Game.Config.mapSize : 420f) * 0.5f - 30f;
            int made = 0;
            for (int attempt = 0; attempt < 600 && made < packs; attempt++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                if (Castle.Covers(x, z, 50f) || Landmarks.Near(x, z, 20f)) continue;
                bool clear = true;
                for (int i = 0; i < Stele.All.Count && clear; i++)
                    if (Stele.All[i] != null && Flat(Stele.All[i].transform.position - new Vector3(x, 0f, z)).magnitude < 60f) clear = false;
                for (int i = 0; i < All.Count && clear; i++)
                    if (All[i].kind == Kind.Loup && Flat(All[i].home - new Vector3(x, 0f, z)).magnitude < 85f) clear = false;
                if (!clear) continue;
                List<Beast> pack = new List<Beast>();
                for (int k = 0; k < 3; k++)
                {
                    Vector3 at = Ground.Place(x + (k - 1) * 2.2f, z + (k % 2) * 1.8f, 0.3f);
                    Beast w = Wolf(root.transform, at, rng.Next());
                    w.flank = (k - 1) * 60f;
                    w.pack = pack;
                    pack.Add(w);
                }
                made++;
            }
        }
    }
}
