using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN GARDE (refait le 26/09 -- Martin : "faut qu'il y ait plein de gardes, des
    /// gardes qui rodent pour essayer de te choper" ; et "les gardes, t'es pas cense
    /// les acheter"). On ne leur parle plus. Ils GARDENT.
    ///
    /// Il fait sa ronde, sa lanterne braquee devant lui : le cone de lumiere qu'on
    /// voit balayer le sol, c'est EXACTEMENT ce qu'il voit. Reste hors du cone.
    ///
    /// Qui l'interesse :
    ///   - au chateau : quiconque est DANS le donjon ou une reserve, et quiconque
    ///     porte du butin dans l'enceinte ;
    ///   - les RODEURS, qui tournent dans la foret autour du chateau : quiconque
    ///     porte du butin.
    /// S'il en voit un, sa lanterne rougit, il crie, et il COURT. S'il te rattrape,
    /// il frappe (quatre coups te couchent). On peut le fuir, le semer dans la
    /// brume -- ou le tuer : quatre coups d'epee. Il revient a son poste une minute
    /// et demie plus tard.
    ///
    /// Prendre un tresor fait du bruit : les gardes proches accourent (Alert).
    /// </summary>
    public class Guard : MonoBehaviour
    {
        public static readonly List<Guard> All = new List<Guard>();

        enum State { Patrol, Chase, Return, Dead }

        [System.NonSerialized] public string guardName;
        [System.NonSerialized] public bool roamer;

        State state = State.Patrol;
        Vector3[] route;
        int next = 1;
        float pause;
        CharacterController body;
        float fall;
        Walker walker;
        Light cone;
        float lookSweep;
        float stuck, detour, detourSign = 1f;

        Seeker chased;
        float chaseTimer;
        float lostTimer;
        float strikeTimer;
        float barkTimer;
        float deadTimer;
        float health = MaxHealth;

        public float Suspicion { get; private set; }
        public bool Chasing { get { return state == State.Chase; } }
        public bool Alive { get { return state != State.Dead; } }

        /// <summary>Vrai si un garde, au moins, te court apres.</summary>
        public static bool HuntingPlayer
        {
            get
            {
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].state == State.Chase && All[i].chased != null && All[i].chased.IsPlayer) return true;
                return false;
            }
        }

        const float MaxHealth = 100f;
        const float SightRange = 17f;
        const float SightAngle = 36f;       // demi-angle : le cone fait 72 degres
        const float WalkSpeed = 1.8f;
        const float RunSpeed = 6.2f;
        const float Damage = 22f;
        const float RespawnSeconds = 90f;

        static readonly Color Tabard = new Color(0.42f, 0.10f, 0.10f);
        static readonly Color TabardDark = new Color(0.28f, 0.07f, 0.07f);
        static readonly Color Steel = new Color(0.40f, 0.41f, 0.43f);
        static readonly Color Skin = new Color(0.55f, 0.46f, 0.40f);
        static readonly Color ConeCalm = new Color(1f, 0.82f, 0.55f);
        static readonly Color ConeAlarm = new Color(1f, 0.3f, 0.2f);

        // ================================================================== construction

        public static Guard Build(Transform parent, string name, Vector3[] route, bool roamer)
        {
            GameObject root = new GameObject((roamer ? "RÔDEUR " : "GARDE ") + name);
            root.transform.SetParent(parent, false);
            root.transform.position = route[0];

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;

            Guard g = root.AddComponent<Guard>();
            g.guardName = name;
            g.roamer = roamer;
            g.route = route;
            g.body = cc;

            // Le corps : un soldat articule qui marche vraiment (voir Walker.cs).
            Walker.Look look = new Walker.Look();
            look.skin = Skin;
            look.shirt = new Color(0.30f, 0.27f, 0.24f);       // gambison matelasse
            look.legs = new Color(0.22f, 0.2f, 0.19f);
            look.boots = new Color(0.16f, 0.12f, 0.09f);
            look.bulk = 1.12f;
            look.height = 1.9f;
            Walker w = Walker.Build(root.transform, "Soldat", look);
            w.HoldPole = true;
            w.HoldLantern = true;
            w.RunSpeed = RunSpeed;
            g.walker = w;
            // Un vrai modele dans Resources/Modeles/Gardes ? Il remplace les cubes.
            ModelSkin.TryDress(w, "Gardes", look.height, name.Length + All.Count);

            Color cross = new Color(0.8f, 0.78f, 0.72f);
            Color wood = new Color(0.25f, 0.19f, 0.13f);
            Color leather = new Color(0.2f, 0.14f, 0.1f);
            Proto.BeginVisualOnly();
            // Le tabard : devant et dans le dos, par-dessus le gambison, avec la croix.
            Proto.Cube(w.Torso, new Vector3(0f, 0.2f, 0.15f), new Vector3(0.44f, 0.62f, 0.03f), Tabard, "Tabard");
            Proto.Cube(w.Torso, new Vector3(0f, 0.2f, -0.15f), new Vector3(0.44f, 0.62f, 0.03f), TabardDark, "Tabard");
            Proto.Cube(w.Hips, new Vector3(0f, -0.2f, 0.15f), new Vector3(0.36f, 0.34f, 0.03f), Tabard, "Pan");
            Proto.Cube(w.Hips, new Vector3(0f, -0.2f, -0.15f), new Vector3(0.36f, 0.34f, 0.03f), TabardDark, "Pan");
            Proto.Cube(w.Torso, new Vector3(0f, 0.24f, 0.17f), new Vector3(0.07f, 0.4f, 0.01f), cross, "Croix");
            Proto.Cube(w.Torso, new Vector3(0f, 0.32f, 0.17f), new Vector3(0.28f, 0.07f, 0.01f), cross, "Croix");
            Proto.Cube(w.Torso, new Vector3(0f, 0.02f, 0f), new Vector3(0.46f, 0.07f, 0.31f), leather, "Baudrier");
            // Epauleres et gantelets de fer.
            Proto.Cube(w.ArmL, new Vector3(-0.02f, -0.03f, 0f), new Vector3(0.2f, 0.13f, 0.2f), Steel, "Epauliere");
            Proto.Cube(w.ArmR, new Vector3(0.02f, -0.03f, 0f), new Vector3(0.2f, 0.13f, 0.2f), Steel, "Epauliere");
            Proto.Cube(w.HandL, Vector3.zero, new Vector3(0.12f, 0.13f, 0.13f), Steel, "Gantelet");
            Proto.Cube(w.HandR, Vector3.zero, new Vector3(0.12f, 0.13f, 0.13f), Steel, "Gantelet");
            // Le camail (la cagoule de mailles) et le chapel de fer a large bord.
            Proto.Cube(w.Neck, new Vector3(0f, 0.02f, 0f), new Vector3(0.3f, 0.14f, 0.28f), Steel, "Camail");
            Proto.Cube(w.Head, new Vector3(0f, 0.1f, -0.02f), new Vector3(0.26f, 0.24f, 0.25f), Palette.Shade(Steel, 0.8f), "Coiffe");
            Proto.Cylinder(w.Head, new Vector3(0f, 0.26f, 0f), new Vector3(0.44f, 0.012f, 0.44f), Steel, "Bord");
            Proto.Sphere(w.Head, new Vector3(0f, 0.27f, 0f), new Vector3(0.27f, 0.2f, 0.27f), Steel, "Chapel");
            Proto.Cube(w.Head, new Vector3(0f, 0.37f, 0f), new Vector3(0.03f, 0.03f, 0.03f), Palette.Shade(Steel, 1.3f), "Pointe");
            // Une epee au cote.
            GameObject sheath = Proto.Cube(w.Hips, new Vector3(-0.24f, -0.26f, -0.04f), new Vector3(0.05f, 0.62f, 0.07f), leather, "Fourreau");
            sheath.transform.localRotation = Quaternion.Euler(-18f, 0f, 6f);
            Proto.Cube(w.Hips, new Vector3(-0.25f, 0.06f, 0.06f), new Vector3(0.16f, 0.03f, 0.03f), Steel, "Garde");

            // La hallebarde, tenue droite : hampe, fer de hache, crochet, pique.
            Transform pole = w.Holder(w.HandR, "Hallebarde");
            // (La main est a 1,2 m du sol : la hampe descend jusqu'aux chevilles.)
            Proto.Cube(pole, new Vector3(0f, 0.12f, 0f), new Vector3(0.05f, 2.5f, 0.05f), wood, "Hampe");
            Proto.Cube(pole, new Vector3(0f, 1.2f, 0.12f), new Vector3(0.025f, 0.3f, 0.2f), Steel, "Hache");
            Proto.Cube(pole, new Vector3(0f, 1.24f, -0.09f), new Vector3(0.025f, 0.08f, 0.14f), Steel, "Crochet");
            Proto.Cone(pole, new Vector3(0f, 1.37f, 0f), 0.045f, 0.34f, Steel, "Pique", 4);
            Proto.Cube(pole, new Vector3(0f, 1.04f, 0f), new Vector3(0.07f, 0.06f, 0.07f), Steel, "Virole");

            // La lanterne, pendue a la main gauche.
            Transform hang = w.Holder(w.HandL, "Lanterne");
            Vector3 lantern = new Vector3(0f, -0.2f, 0f);
            Proto.Cube(hang, new Vector3(0f, -0.04f, 0f), new Vector3(0.015f, 0.12f, 0.015f), Steel, "Anse");
            Proto.Cube(hang, lantern + new Vector3(0f, 0.1f, 0f), new Vector3(0.17f, 0.04f, 0.17f), Steel, "Chapeau");
            Proto.Cube(hang, lantern - new Vector3(0f, 0.1f, 0f), new Vector3(0.17f, 0.03f, 0.17f), Steel, "Fond");
            GameObject flame = Proto.Cube(hang, lantern, new Vector3(0.1f, 0.14f, 0.1f), Color.white, "Flamme");
            flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.74f, 0.4f), 2.6f);
            flame.AddComponent<Flame>();
            Proto.EndVisualOnly();

            // LE CONE : une lumiere "spot", exactement l'angle et la portee de sa vue.
            GameObject coneGo = new GameObject("Regard de " + name);
            coneGo.transform.SetParent(root.transform, false);
            coneGo.transform.localPosition = new Vector3(0f, 1.7f, 0.2f);
            coneGo.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            g.cone = coneGo.AddComponent<Light>();
            g.cone.type = LightType.Spot;
            g.cone.spotAngle = SightAngle * 2f;
            g.cone.range = SightRange;
            g.cone.intensity = 2.2f;
            g.cone.color = ConeCalm;
            g.cone.shadows = LightShadows.None;

            All.Add(g);
            return g;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        // ================================================================== boucle

        void Update()
        {
            Season season = Game.Season;
            if (season == null || !season.Running || Time.deltaTime <= 0f) return;
            float dt = Time.deltaTime;
            if (barkTimer > 0f) barkTimer -= dt;
            if (strikeTimer > 0f) strikeTimer -= dt;

            if (state == State.Dead)
            {
                deadTimer -= dt;
                if (deadTimer <= 0f) Revive();
                return;
            }

            // Loin de toi, un rodeur ne coute presque rien : il glisse sur sa ronde.
            Watch(dt);

            switch (state)
            {
                case State.Patrol: Patrol(dt); break;
                case State.Chase: Chase(dt); break;
                case State.Return:
                    if (Walk(route[next], WalkSpeed * 1.6f, dt)) state = State.Patrol;
                    break;
            }
            ColourCone();
            walker.Gaze = state == State.Chase && chased != null ? chased.Body : NearPlayer(8f) ? Game.PlayerTransform : null;
        }

        /// <summary>
        /// Regarder. Pour chaque chercheur : est-il SUSPECT et VISIBLE (dans le cone,
        /// pas cache par un mur) ? Si oui, la suspicion monte -- vite de pres,
        /// lentement de loin. A 1, il court.
        /// </summary>
        void Watch(float dt)
        {
            if (state == State.Chase) return;

            Seeker seen = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || !s.Alive || !Suspect(s)) continue;
                float d;
                if (!InSight(s.Body.position, out d)) continue;
                if (d < bestDistance) { bestDistance = d; seen = s; }
            }

            if (seen != null)
            {
                float rate = bestDistance < 6f ? 3f : 1.2f;
                Suspicion = Mathf.Min(1f, Suspicion + rate * dt);
                if (Suspicion > 0.2f) Figures.Face(transform, seen.Body.position, 90f);
                if (Suspicion > 0.35f) Bark("Qui va là ?");
                if (Suspicion >= 1f) StartChase(seen);
            }
            else
            {
                Suspicion = Mathf.Max(0f, Suspicion - 0.45f * dt);
            }
        }

        bool Suspect(Seeker s)
        {
            Vector3 p = s.Body.position;
            if (roamer) return s.Hoard.Carried > 0;
            if (!Castle.Covers(p.x, p.z, 2f)) return false;
            return s.Hoard.Carried > 0 || Castle.InKeep(p) || Castle.InStoreroom(p.x, p.z);
        }

        void StartChase(Seeker s)
        {
            if (state == State.Dead || s == null) return;
            bool fresh = state != State.Chase;
            state = State.Chase;
            chased = s;
            chaseTimer = 0f;
            lostTimer = 0f;
            Suspicion = 1f;
            if (fresh)
            {
                barkTimer = 0f;
                Bark("HALTE ! Au voleur !");
                if (s.IsPlayer) Sfx.Alarm();
            }
        }

        /// <summary>Un tresor vient d'etre pris : les gardes a portee accourent.</summary>
        public static void Alert(Vector3 at, float radius, Seeker culprit)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Guard g = All[i];
                if (g == null || !g.Alive || g.roamer) continue;
                Vector3 d = g.transform.position - at;
                if (Mathf.Abs(d.y) > 7f) continue;          // un autre etage n'entend pas
                d.y = 0f;
                if (d.magnitude < radius) g.StartChase(culprit);
            }
        }

        bool InSight(Vector3 target, out float distance)
        {
            Vector3 eye = transform.position + Vector3.up * 1.7f;
            Vector3 to = target + Vector3.up * 1.1f - eye;
            distance = to.magnitude;
            if (distance > SightRange) return false;
            Vector3 flatTo = new Vector3(to.x, 0f, to.z);
            if (distance > 2.2f && Vector3.Angle(transform.forward, flatTo) > SightAngle) return false;

            RaycastHit hit;
            if (Physics.Raycast(eye, to / distance, out hit, distance - 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Touche autre chose que lui-meme avant la cible : un mur, un tronc, un plancher.
                if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform)) return false;
            }
            return true;
        }

        void Patrol(float dt)
        {
            if (pause > 0f)
            {
                pause -= dt;
                // Il regarde a gauche, a droite, pendant ses pauses.
                lookSweep += dt;
                transform.Rotate(0f, Mathf.Sin(lookSweep * 0.9f) * 50f * dt, 0f);
                return;
            }
            if (Walk(route[next], roamer ? WalkSpeed * 1.4f : WalkSpeed, dt))
            {
                next = (next + 1) % route.Length;
                pause = roamer ? 2f : 3f;
            }
        }

        void Chase(float dt)
        {
            chaseTimer += dt;
            if (chased == null || chased.Body == null || !chased.Alive) { GiveUp(); return; }
            Vector3 p = chased.Body.position;

            // Perdu de vue (la brume, un mur, un autre etage) : il cherche un moment.
            float d;
            bool seen = InSight(p, out d) || d < 3f;
            lostTimer = seen ? 0f : lostTimer + dt;
            float leash = roamer ? 90f : 30f;
            bool tooFar = roamer ? Flat(p - route[0]).magnitude > leash + 60f : !Castle.Covers(p.x, p.z, leash);
            if (tooFar || lostTimer > 7f || chaseTimer > 45f || Mathf.Abs(p.y - transform.position.y) > 4f && lostTimer > 2.5f)
            {
                Bark("Et que je ne te revoie pas !");
                GiveUp();
                return;
            }

            float flat = Flat(p - transform.position).magnitude;
            if (flat > 1.6f) Walk(p, RunSpeed, dt);
            else Figures.Face(transform, p, 360f);
            if (flat < 2.1f && strikeTimer <= 0f && Mathf.Abs(p.y - transform.position.y) < 1.8f)
            {
                strikeTimer = 1.25f;
                if (walker != null) walker.PlaySwing();
                if (NearPlayer(25f)) Sfx.HarvestTap(ResourceType.Iron);
                Combat.Hit(chased, null, Damage, "sous la hallebarde de " + guardName);
                if (chased != null && chased.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.3f);
                if (chased == null || !chased.Alive) GiveUp();
            }
        }

        void GiveUp()
        {
            state = State.Return;
            chased = null;
            Suspicion = 0f;
        }

        // ================================================================== les coups

        /// <summary>On le frappe : il encaisse, se retourne contre toi, et tombe au quatrieme coup.</summary>
        public void Hurt(float amount, Seeker by)
        {
            if (state == State.Dead) return;
            health -= amount;
            Sfx.Thud();
            FloatingTexts.Spawn(transform.position + Vector3.up * 2.2f, "-" + Mathf.RoundToInt(amount), new Color(1f, 0.35f, 0.3f));
            Ambiance.Burst(null, transform.position + Vector3.up * 1.2f, new Color(0.55f, 0.1f, 0.08f));
            if (walker != null) Punch.Apply(walker.transform, by != null && by.Body != null ? by.Body.position : transform.position - transform.forward);
            if (health <= 0f) { Die(by); return; }
            if (by != null) StartChase(by);
        }

        void Die(Seeker by)
        {
            state = State.Dead;
            deadTimer = RespawnSeconds;
            chased = null;
            Suspicion = 0f;
            if (by != null && by.IsPlayer) Stats.GuardsDowned++;
            if (body != null) body.enabled = false;
            if (walker != null) walker.gameObject.SetActive(false);
            if (cone != null) cone.enabled = false;
            if (NearPlayer(40f) && !Sfx.Muted) AudioSource.PlayClipAtPoint(Sfx.Moan(), transform.position + Vector3.up, 0.8f);
            // Il laisse tomber sa bourse : quelques pieces pour qui l'a abattu.
            if (by != null)
            {
                by.Hoard.TryPickLoot(3);
                by.SyncWeight();
                if (by.IsPlayer) { Sfx.Coin(); FloatingTexts.Spawn(transform.position + Vector3.up * 1.4f, "★3", Palette.Gold); }
            }
        }

        void Revive()
        {
            state = State.Patrol;
            health = MaxHealth;
            next = 1 % route.Length;
            transform.position = route[0];
            if (body != null) body.enabled = true;
            if (walker != null) walker.gameObject.SetActive(true);
            if (cone != null) cone.enabled = true;
        }

        void ColourCone()
        {
            if (cone == null) return;
            Color want = state == State.Chase ? ConeAlarm : Color.Lerp(ConeCalm, ConeAlarm, Suspicion);
            cone.color = Color.Lerp(cone.color, want, Time.deltaTime * 6f);
        }

        // ================================================================== marcher

        /// <summary>Marche vers un point ; vrai une fois arrive.</summary>
        bool Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            if (to.magnitude < 0.3f) return true;
            Vector3 dir = to.normalized;
            // Coince contre un tronc (les rodeurs, en foret) : il contourne un moment.
            if (detour > 0f)
            {
                detour -= dt;
                dir = Quaternion.Euler(0f, 70f * detourSign, 0f) * dir;
            }
            if (body.enabled)
            {
                fall = body.isGrounded ? -1f : fall - 22f * dt;
                Vector3 before = transform.position;
                body.Move((dir * speed + Vector3.up * fall) * dt);
                if (Flat(transform.position - before).magnitude < speed * dt * 0.3f)
                {
                    stuck += dt;
                    if (stuck > 0.4f) { detour = 1f; detourSign = -detourSign; stuck = 0f; }
                }
                else stuck = 0f;
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 300f * dt);
            return false;
        }

        bool NearPlayer(float metres)
        {
            Transform p = Game.PlayerTransform;
            return p != null && Flat(p.position - transform.position).magnitude < metres;
        }

        void Bark(string line)
        {
            if (barkTimer > 0f || !NearPlayer(25f)) return;
            barkTimer = 5f;
            // Une voix, pas un texte sur la tete (voir Sfx.Voice).
            Sfx.Voice(transform.position, guardName.Length + 1, line.EndsWith("!"));
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
