using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA GARDE PALE (La Couronne, 26/09 -- Martin : "le chateau hyper bien garde",
    /// "les PNJ, soit hyper detailles dans un bon theme, soit un truc lisse").
    ///
    /// Des chevaliers d'ivoire, lisses et SANS VISAGE : un heaume en oeuf perce d'une
    /// fente qui luit comme une braise (voir Walker.AssembleSmooth). La fente dit tout :
    ///   braise     il fait sa ronde ;
    ///   orange     il t'a apercu, il regarde ;
    ///   ROUGE      il te court apres ;
    ///   blanc-rouge   il frappe -- le coup part une demi-seconde plus tard : ecarte-toi.
    ///
    /// Quatre sortes :
    ///   SENTINELLE   ronde, cone de vue, poursuit, frappe au glaive (coup annonce) ;
    ///   ARBALETRIER  sur les remparts et les etages ; il VISE (un trait rouge relie
    ///                son arbalete a toi pendant une seconde), puis tire un carreau
    ///                qu'on voit partir -- on l'esquive en bougeant ;
    ///   MOLOSSE      chien de garde de la cour : rapide, fragile, il sent de pres ;
    ///   LE ROI CREUX le boss de la terrasse : trois metres, il dort pres de la
    ///                Couronne et se leve quand on l'approche. Il balaie devant lui,
    ///                et FRAPPE LE SOL (un cercle rouge grandit sous lui : sors-en).
    ///
    /// Qui les interesse : QUICONQUE EST DANS L'ENCEINTE, et LE PORTEUR DE LA COURONNE
    /// jusqu'a 60 m hors les murs (les rodeurs de la foret aussi). La Cape d'ombre
    /// les aveugle, le fumigene leur bouche la vue, le pouvoir Ombre les ralentit.
    /// Un garde abattu revient a son poste une minute et demie plus tard.
    /// </summary>
    public class Guard : MonoBehaviour
    {
        public enum Rank { Sentinelle, Arbaletrier, Molosse, Roi }

        public static readonly List<Guard> All = new List<Guard>();
        /// <summary>Le Roi Creux (null s'il n'y en a pas).</summary>
        public static Guard King { get; private set; }

        enum State { Patrol, Chase, Return, Dead, Asleep }

        [System.NonSerialized] public string guardName;
        [System.NonSerialized] public bool roamer;
        [System.NonSerialized] public Rank rank;

        State state = State.Patrol;
        Vector3[] route;
        int next = 1;
        float pause;
        Vector3 home;
        CharacterController body;
        float fall;
        Walker walker;
        HoundBody hound;
        Light cone;
        float lookSweep;
        float stuck, detour, detourSign = 1f;

        Seeker chased;
        float chaseTimer, lostTimer, strikeTimer, barkTimer, deadTimer, calmTimer;
        float health, maxHealth;
        float windup = -1f;          // un coup annonce est en cours depuis "windup" secondes (-1 : aucun)
        int attack;                  // le Roi : 0 balayage, 1 frappe au sol
        float slamTimer;
        GameObject ring;             // le cercle annonce de la frappe au sol
        LineRenderer aimLine;        // la visee de l'arbaletrier
        Transform muzzle;
        float aim;
        int visorShown = -1;

        // --- ce que chaque sorte sait faire (rempli par Setup)
        float sightRange, sightAngle, nearSense, walkSpeed, runSpeed, damage, reach, windupTime, cooldown;

        public float Suspicion { get; private set; }
        public bool Chasing { get { return state == State.Chase; } }
        public bool Alive { get { return state != State.Dead; } }
        public bool Asleep { get { return state == State.Asleep; } }
        public float Health01 { get { return Mathf.Clamp01(health / Mathf.Max(1f, maxHealth)); } }
        /// <summary>Le rayon du corps : on le touche de plus loin s'il est gros (le Roi).</summary>
        public float Girth { get { return rank == Rank.Roi ? 1.1f : rank == Rank.Molosse ? 0.35f : 0.45f; } }
        public Seeker Target { get { return state == State.Chase ? chased : null; } }

        /// <summary>Vrai si un garde, au moins, te court apres (ou te vise).</summary>
        public static bool HuntingPlayer
        {
            get
            {
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].state == State.Chase && All[i].chased != null && All[i].chased.IsPlayer) return true;
                return false;
            }
        }

        /// <summary>Combien de gardes pourchassent "s" (les bots s'en servent pour fuir).</summary>
        public static int ChasersOf(Seeker s)
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].state == State.Chase && All[i].chased == s) n++;
            return n;
        }

        static readonly Color Ember = new Color(1f, 0.55f, 0.22f);
        static readonly Color Wary = new Color(1f, 0.38f, 0.12f);
        static readonly Color Alarm = new Color(1f, 0.1f, 0.06f);
        static readonly Color Strike = new Color(1f, 0.75f, 0.7f);
        static readonly Color Dormant = new Color(0.3f, 0.16f, 0.1f);
        static readonly Color KingEye = new Color(0.6f, 0.8f, 1f);

        // ================================================================== construction

        public static Guard Build(Transform parent, string name, Vector3[] route, Rank rank, bool roamer)
        {
            string title = rank == Rank.Roi ? "LE ROI CREUX" : rank == Rank.Molosse ? "MOLOSSE " : rank == Rank.Arbaletrier ? "ARBALÉTRIER " : roamer ? "RÔDEUR " : "SENTINELLE ";
            GameObject root = new GameObject(rank == Rank.Roi ? title : title + name);
            root.transform.SetParent(parent, false);
            root.transform.position = route[0];

            Guard g = root.AddComponent<Guard>();
            g.guardName = name;
            g.roamer = roamer;
            g.rank = rank;
            g.route = route;
            g.home = route[0];
            g.next = 1 % route.Length;
            g.Setup();

            CharacterController cc = root.AddComponent<CharacterController>();
            bool king = rank == Rank.Roi, dog = rank == Rank.Molosse;
            cc.height = king ? 3.2f : dog ? 1.0f : 1.9f;
            cc.radius = king ? 0.75f : dog ? 0.35f : 0.38f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;
            g.body = cc;

            if (dog) g.hound = HoundBody.Build(root.transform);
            else g.walker = Knight(root.transform, rank, name, g);

            // LE CONE : une lumiere "spot", l'angle et la portee de sa vue. On voit ou il regarde.
            if (!king)
            {
                GameObject coneGo = new GameObject("Regard");
                coneGo.transform.SetParent(root.transform, false);
                coneGo.transform.localPosition = new Vector3(0f, dog ? 0.9f : 1.7f, 0.2f);
                coneGo.transform.localRotation = Quaternion.Euler(dog ? 10f : 22f, 0f, 0f);
                g.cone = coneGo.AddComponent<Light>();
                g.cone.type = LightType.Spot;
                g.cone.spotAngle = g.sightAngle * 2f;
                g.cone.range = g.sightRange * (rank == Rank.Arbaletrier ? 0.7f : 1f);
                g.cone.intensity = dog ? 1.2f : 2f;
                g.cone.color = Ember;
                g.cone.shadows = LightShadows.None;
            }
            else
            {
                // Le Roi : une lueur froide autour de lui, qui s'allume a son reveil.
                GameObject glowGo = new GameObject("Aura");
                glowGo.transform.SetParent(root.transform, false);
                glowGo.transform.localPosition = new Vector3(0f, 2.8f, 0.5f);
                g.cone = glowGo.AddComponent<Light>();
                g.cone.type = LightType.Point;
                g.cone.range = 9f;
                g.cone.intensity = 0.3f;
                g.cone.color = KingEye;
                g.cone.shadows = LightShadows.None;
                g.state = State.Asleep;
                King = g;
            }

            All.Add(g);
            return g;
        }

        void Setup()
        {
            switch (rank)
            {
                case Rank.Arbaletrier:
                    maxHealth = 70f; sightRange = 30f; sightAngle = 42f; nearSense = 3f;
                    walkSpeed = 1.4f; runSpeed = 0f; damage = 18f; reach = 0f; windupTime = 0.95f; cooldown = 2.4f;
                    break;
                case Rank.Molosse:
                    maxHealth = 45f; sightRange = 16f; sightAngle = 60f; nearSense = 8f;
                    walkSpeed = 2.6f; runSpeed = 8.4f; damage = 12f; reach = 1.7f; windupTime = 0.25f; cooldown = 0.8f;
                    break;
                case Rank.Roi:
                    maxHealth = 800f; sightRange = 14f; sightAngle = 180f; nearSense = 14f;
                    walkSpeed = 2.4f; runSpeed = 3.8f; damage = 35f; reach = 3.4f; windupTime = 0.7f; cooldown = 1.6f;
                    break;
                default:
                    maxHealth = 100f; sightRange = 17f; sightAngle = 36f; nearSense = 2.2f;
                    walkSpeed = 1.8f; runSpeed = 6.1f; damage = 22f; reach = 2.2f; windupTime = 0.5f; cooldown = 1.3f;
                    break;
            }
            health = maxHealth;
        }

        /// <summary>Un chevalier de la Garde Pale : l'armure lisse, et ce qu'il tient selon son rang.</summary>
        static Walker Knight(Transform root, Rank rank, string name, Guard g)
        {
            bool king = rank == Rank.Roi;
            Walker.Look look = new Walker.Look();
            look.smooth = true;
            look.shirt = king ? new Color(0.56f, 0.53f, 0.48f) : new Color(0.8f, 0.76f, 0.68f);
            look.legs = king ? new Color(0.38f, 0.36f, 0.34f) : new Color(0.6f, 0.57f, 0.51f);
            look.boots = new Color(0.2f, 0.19f, 0.2f);
            look.robeColor = king ? new Color(0.3f, 0.06f, 0.07f) : new Color(0.1f, 0.09f, 0.11f);
            look.bulk = king ? 1.3f : rank == Rank.Arbaletrier ? 0.95f : 1.08f;
            look.height = king ? 3.3f : rank == Rank.Arbaletrier ? 1.85f : 1.95f;
            Walker w = Walker.Build(root, king ? "Le Roi Creux" : "Chevalier pâle", look);
            w.RunSpeed = g.runSpeed > 0f ? g.runSpeed : 4f;
            Color ivory = look.shirt;
            Color haft = new Color(0.16f, 0.13f, 0.12f);

            Proto.BeginVisualOnly();
            if (rank == Rank.Arbaletrier)
            {
                // L'arbalete, tenue devant la poitrine, toujours pointee ou il regarde.
                Transform bow = Walker.Node(w.Torso, new Vector3(0.1f, 0.36f, 0.26f), "Arbalète");
                Proto.Cube(bow, new Vector3(0f, 0f, 0.1f), new Vector3(0.06f, 0.07f, 0.56f), haft, "Fût");
                GameObject limbs = Proto.Capsule(bow, new Vector3(0f, 0.02f, 0.34f), new Vector3(0.035f, 0.3f, 0.035f), ivory, "Arc");
                limbs.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Proto.Cube(bow, new Vector3(0f, 0.03f, 0.3f), new Vector3(0.56f, 0.008f, 0.008f), new Color(0.8f, 0.78f, 0.7f), "Corde");
                GameObject tip = Proto.Cube(bow, new Vector3(0f, 0.05f, 0.36f), new Vector3(0.02f, 0.02f, 0.1f), Color.white, "Carreau");
                tip.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Ember, 2.2f);
                g.muzzle = Walker.Node(bow, new Vector3(0f, 0.05f, 0.45f), "Bouche");
                w.HoldLantern = false;
            }
            else if (king)
            {
                // LA GRANDE EPEE du Roi, tenue droite ; une couronne de pointes de fer
                // noir sur le heaume, chacune rougie au bout.
                w.HoldPole = true;
                Transform blade = w.Holder(w.HandR, "Grande épée");
                Proto.Cylinder(blade, new Vector3(0f, -0.1f, 0f), new Vector3(0.06f, 0.18f, 0.06f), haft, "Poignée");
                Proto.Capsule(blade, new Vector3(0f, 0.12f, 0f), new Vector3(0.08f, 0.26f, 0.08f), look.legs, "Garde");
                blade.GetChild(1).localRotation = Quaternion.Euler(0f, 0f, 90f);
                Proto.Capsule(blade, new Vector3(0f, 1.05f, 0f), new Vector3(0.12f, 0.9f, 0.03f), new Color(0.62f, 0.62f, 0.64f), "Lame");
                GameObject edge = Proto.Cube(blade, new Vector3(0f, 1.05f, 0.017f), new Vector3(0.025f, 1.6f, 0.005f), Color.white, "Fil");
                edge.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(KingEye, 1.2f);
                for (int i = 0; i < 7; i++)
                {
                    float a = i / 7f * Mathf.PI * 2f;
                    Vector3 at = new Vector3(Mathf.Cos(a) * 0.13f, 0.28f, Mathf.Sin(a) * 0.14f);
                    GameObject spike = Proto.Cone(w.Head, at, 0.03f, 0.2f, new Color(0.08f, 0.07f, 0.08f), "Pointe", 5);
                    spike.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * -15f, 0f, Mathf.Cos(a) * 15f);
                    GameObject hot = Proto.Sphere(w.Head, at + spike.transform.localRotation * new Vector3(0f, 0.2f, 0f), Vector3.one * 0.025f, Color.white, "Braise");
                    hot.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Alarm, 2f);
                }
            }
            else
            {
                // LE GLAIVE : une longue hampe sombre, une lame d'ivoire en feuille.
                w.HoldPole = true;
                Transform pole = w.Holder(w.HandR, "Glaive");
                Proto.Cylinder(pole, new Vector3(0f, 0.15f, 0f), new Vector3(0.045f, 1.25f, 0.045f), haft, "Hampe");
                Proto.Sphere(pole, new Vector3(0f, 1.4f, 0f), new Vector3(0.08f, 0.08f, 0.08f), look.legs, "Virole");
                Proto.Sphere(pole, new Vector3(0f, 1.72f, 0.02f), new Vector3(0.05f, 0.62f, 0.15f), ivory, "Lame");
                // Une lanterne ronde a la ceinture (main gauche) : la braise de la ronde.
                w.HoldLantern = true;
                Transform hang = w.Holder(w.HandL, "Lanterne");
                Proto.Cylinder(hang, new Vector3(0f, -0.05f, 0f), new Vector3(0.012f, 0.06f, 0.012f), haft, "Anse");
                GameObject glass = Proto.Sphere(hang, new Vector3(0f, -0.2f, 0f), Vector3.one * 0.15f, Color.white, "Verre");
                glass.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Ember, 1.6f);
                Proto.Sphere(hang, new Vector3(0f, -0.12f, 0f), new Vector3(0.12f, 0.05f, 0.12f), haft, "Chapeau");
            }
            Proto.EndVisualOnly();
            if (!king && rank != Rank.Arbaletrier) ModelSkin.TryDress(w, "Gardes", look.height, name.Length + All.Count);
            return w;
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (King == this) King = null;
            if (ring != null) Destroy(ring);
        }

        Transform Figure { get { return walker != null ? walker.transform : hound != null ? hound.transform : transform; } }
        Vector3 Eye { get { return transform.position + Vector3.up * (rank == Rank.Molosse ? 0.9f : rank == Rank.Roi ? 2.9f : 1.7f); } }

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

            Cull();
            if (state == State.Asleep) Slumber(dt);
            else
            {
                Watch(dt);
                switch (state)
                {
                    case State.Patrol: Patrol(dt); break;
                    case State.Chase: Chase(dt); break;
                    case State.Return:
                        if (Walk(rank == Rank.Roi ? home : route[next], walkSpeed * 1.6f, dt))
                        {
                            state = State.Patrol;
                            calmTimer = 0f;
                        }
                        break;
                }
            }
            ShowMood();
            if (walker != null) walker.Gaze = state == State.Chase && chased != null ? chased.Body : NearPlayer(8f) && state != State.Asleep ? Game.PlayerTransform : null;
        }

        /// <summary>Loin de toi, le corps s'eteint : personne ne le voit (le cerveau, lui, tourne).</summary>
        void Cull()
        {
            bool near = NearPlayer(rank == Rank.Roi ? 140f : 95f);
            GameObject fig = walker != null ? walker.gameObject : hound != null ? hound.gameObject : null;
            if (fig != null && fig.activeSelf != near) fig.SetActive(near);
        }

        /// <summary>
        /// Regarder. Pour chaque joueur : est-il SUSPECT et VISIBLE (dans le cone, pas
        /// cache par un mur ou la fumee) ? Si oui, la suspicion monte -- vite de pres,
        /// lentement de loin, moitie moins vite pour qui a le pouvoir Ombre. A 1, il court.
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
                if (!InSight(s, out d)) continue;
                if (d < bestDistance) { bestDistance = d; seen = s; }
            }

            if (seen != null)
            {
                float rate = bestDistance < 6f ? 3f : 1.2f;
                if (rank == Rank.Molosse) rate *= 1.6f;
                if (seen.CarriesCrown) rate *= 2f;
                if (seen.Has(Power.Ombre)) rate *= 0.5f;
                Suspicion = Mathf.Min(1f, Suspicion + rate * dt);
                if (Suspicion > 0.2f) Figures.Face(transform, seen.Body.position, 90f);
                if (Suspicion > 0.35f) Bark("Qui va là ?");
                if (Suspicion >= 1f) StartChase(seen, true);
            }
            else
            {
                Suspicion = Mathf.Max(0f, Suspicion - 0.45f * dt);
            }
        }

        bool Suspect(Seeker s)
        {
            if (s.Hidden) return false;
            Vector3 p = s.Body.position;
            if (s.CarriesCrown) return roamer ? Flat(p - transform.position).magnitude < 60f : Castle.Covers(p.x, p.z, 60f);
            if (roamer) return false;
            return Castle.Inside(p);
        }

        bool InSight(Seeker s, out float distance)
        {
            Vector3 eye = Eye;
            Vector3 target = s.Body.position + Vector3.up * 1.1f;
            Vector3 to = target - eye;
            distance = to.magnitude;
            float range = sightRange * (s.Has(Power.Ombre) ? 0.75f : 1f);
            if (distance > range) return false;
            Vector3 flatTo = new Vector3(to.x, 0f, to.z);
            if (distance > nearSense && Vector3.Angle(transform.forward, flatTo) > sightAngle) return false;
            if (Smoke.Blocks(eye, target)) return false;

            RaycastHit hit;
            if (Physics.Raycast(eye, to / distance, out hit, distance - 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Touche autre chose que lui-meme avant la cible : un mur, un tronc, un plancher.
                if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform)
                    && (s.Body == null || !hit.collider.transform.IsChildOf(s.Body))) return false;
            }
            return true;
        }

        void StartChase(Seeker s, bool rally)
        {
            if (state == State.Dead || s == null) return;
            bool fresh = state != State.Chase;
            if (state == State.Asleep) Wake(s);
            state = State.Chase;
            chased = s;
            chaseTimer = 0f;
            lostTimer = 0f;
            Suspicion = 1f;
            if (!fresh) return;
            barkTimer = 0f;
            Bark("HALTE !");
            if (s.IsPlayer) Sfx.Alarm();
            // Il crie : ceux qui l'entendent (meme etage, 18 m) accourent.
            if (!rally) return;
            for (int i = 0; i < All.Count; i++)
            {
                Guard g = All[i];
                if (g == null || g == this || !g.Alive || g.state == State.Chase || g.rank == Rank.Roi || g.roamer != roamer) continue;
                Vector3 d = g.transform.position - transform.position;
                if (Mathf.Abs(d.y) > 4f || Flat(d).magnitude > 18f) continue;
                g.StartChase(s, false);
            }
        }

        /// <summary>La Couronne vient d'etre prise (ou un grand bruit) : les gardes a portee accourent.</summary>
        public static void Alert(Vector3 at, float radius, Seeker culprit)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Guard g = All[i];
                if (g == null || !g.Alive || g.roamer) continue;
                Vector3 d = g.transform.position - at;
                if (g.rank != Rank.Roi && Mathf.Abs(d.y) > 7f) continue;      // un autre etage n'entend pas
                d.y = 0f;
                if (d.magnitude < radius) g.StartChase(culprit, false);
            }
        }

        void Patrol(float dt)
        {
            if (route.Length < 2) { Sweep(dt); return; }
            if (pause > 0f)
            {
                pause -= dt;
                Sweep(dt);
                return;
            }
            if (Walk(route[next], roamer ? walkSpeed * 1.4f : walkSpeed, dt))
            {
                next = (next + 1) % route.Length;
                pause = rank == Rank.Molosse ? 1f : roamer ? 2f : 3f;
            }
        }

        /// <summary>Il regarde a gauche, a droite, pendant ses pauses.</summary>
        void Sweep(float dt)
        {
            lookSweep += dt;
            transform.Rotate(0f, Mathf.Sin(lookSweep * 0.9f) * 50f * dt, 0f);
        }

        void Chase(float dt)
        {
            chaseTimer += dt;
            if (chased == null || chased.Body == null || !chased.Alive) { GiveUp(); return; }
            Vector3 p = chased.Body.position;

            // Perdu de vue (la brume, un mur, la fumee, un autre etage) : il cherche un moment.
            float d;
            bool seen = !chased.Hidden && (InSight(chased, out d) || d < 3f);
            lostTimer = seen ? 0f : lostTimer + dt;
            bool tooFar;
            if (rank == Rank.Roi) tooFar = !OnTerrace(p) && lostTimer > 1.5f;
            else if (roamer) tooFar = Flat(p - home).magnitude > (chased.CarriesCrown ? 130f : 80f);
            else tooFar = !Castle.Covers(p.x, p.z, chased.CarriesCrown ? 60f : 14f);
            float patience = rank == Rank.Arbaletrier ? 4f : 7f;
            if (tooFar || lostTimer > patience || chaseTimer > 60f && !chased.CarriesCrown
                || Mathf.Abs(p.y - transform.position.y) > 4f && lostTimer > 2.5f && rank != Rank.Arbaletrier)
            {
                GiveUp();
                return;
            }

            if (rank == Rank.Arbaletrier) { Shoot(dt, seen); return; }
            if (rank == Rank.Roi) { Fight(dt); return; }

            float flat = Flat(p - transform.position).magnitude;
            if (windup >= 0f)
            {
                // LE COUP ANNONCE : il leve son arme ; le coup tombe a la fin.
                windup += dt;
                Figures.Face(transform, p, 200f);
                if (windup < windupTime) return;
                windup = -1f;
                strikeTimer = cooldown;
                if (flat < reach + 0.5f && Mathf.Abs(p.y - transform.position.y) < 1.8f
                    && Vector3.Angle(transform.forward, Flat(p - transform.position)) < 75f)
                {
                    if (NearPlayer(25f)) Sfx.Clang();
                    Combat.Hit(chased, null, damage, rank == Rank.Molosse ? "sous les crocs d'un molosse" : "sous le glaive de la Garde Pâle");
                    if (chased != null && chased.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.3f);
                }
                else if (NearPlayer(20f)) Sfx.Whoosh();
                if (chased == null || !chased.Alive) GiveUp();
                return;
            }

            if (flat > reach * 0.8f) Walk(p, runSpeed, dt);
            else Figures.Face(transform, p, 360f);
            if (flat < reach && strikeTimer <= 0f && Mathf.Abs(p.y - transform.position.y) < 1.8f)
            {
                windup = 0f;
                if (walker != null) walker.PlaySwing();
                if (hound != null) hound.Lunge();
            }
        }

        // ================================================================== l'arbaletrier

        /// <summary>Il vise (un trait rouge le relie a sa cible), puis tire. Il ne quitte pas son poste.</summary>
        void Shoot(float dt, bool seen)
        {
            Vector3 p = chased.Body.position + Vector3.up * 1.1f;
            Figures.Face(transform, p, 240f);
            if (!seen || strikeTimer > 0f)
            {
                aim = 0f;
                if (aimLine != null) aimLine.enabled = false;
                return;
            }
            if (aimLine == null) aimLine = MakeLine();
            aim += dt;
            Vector3 from = muzzle != null ? muzzle.position : Eye;
            aimLine.enabled = true;
            aimLine.SetPosition(0, from);
            aimLine.SetPosition(1, Vector3.Lerp(from, p, Mathf.Clamp01(aim / windupTime * 1.4f)));
            float w = Mathf.Lerp(0.01f, 0.035f, aim / windupTime);
            aimLine.startWidth = w;
            aimLine.endWidth = w * 0.5f;
            if (aim < windupTime) return;
            aim = 0f;
            aimLine.enabled = false;
            strikeTimer = cooldown;
            Bolt.Fire(from, (p - from).normalized * Bolt.Speed, guardName);
            if (NearPlayer(35f)) Sfx.Whoosh();
        }

        LineRenderer MakeLine()
        {
            GameObject go = new GameObject("Visée");
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sharedMaterial = MaterialFactory.GetGlow(Alarm, 2.5f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;
            return line;
        }

        // ================================================================== le Roi Creux

        /// <summary>Il dort pres de la Couronne. Qui s'en approche le reveille.</summary>
        void Slumber(float dt)
        {
            if (cone != null) cone.intensity = Mathf.MoveTowards(cone.intensity, 0.3f, dt);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || !s.Alive || s.Hidden) continue;
                Vector3 d = s.Body.position - Keep.CrownSpot;
                if (Mathf.Abs(d.y) < 2.5f && Flat(d).magnitude < 9f) { StartChase(s, false); return; }
            }
        }

        /// <summary>Le Roi se leve : un grondement, la terrasse tremble.</summary>
        void Wake(Seeker s)
        {
            if (rank != Rank.Roi || state != State.Asleep) return;
            state = State.Chase;
            slamTimer = 2.5f;
            if (cone != null) cone.intensity = 2.5f;
            if (NearPlayer(60f))
            {
                if (!Sfx.Muted) AudioSource.PlayClipAtPoint(Sfx.Moan(), transform.position + Vector3.up * 3f, 1f);
                Sfx.Alarm();
                if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.5f);
            }
        }

        /// <summary>Le Roi a qui l'on a pris la Couronne se reveille, ou qu'il soit.</summary>
        public static void WakeKing(Seeker s)
        {
            if (King != null && King.Alive) King.StartChase(s, false);
        }

        static bool OnTerrace(Vector3 p)
        {
            return Castle.InKeep(p) && p.y > Keep.Roof - 1.5f;
        }

        void Fight(float dt)
        {
            Vector3 p = chased.Body.position;
            float flat = Flat(p - transform.position).magnitude;
            slamTimer -= dt;

            if (windup >= 0f)
            {
                windup += dt;
                float total = attack == 1 ? 1.1f : windupTime;
                if (attack == 0) Figures.Face(transform, p, 90f);
                if (attack == 1 && ring != null)
                {
                    float r = Mathf.Lerp(0.5f, 5f, windup / total);
                    ring.transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
                }
                if (windup < total) return;
                windup = -1f;
                strikeTimer = cooldown;
                if (attack == 1) Slam(); else SweepBlow();
                return;
            }

            if (flat > 3f && OnTerrace(p)) Walk(p, runSpeed, dt);
            else Figures.Face(transform, p, 120f);

            if (slamTimer <= 0f && flat < 7f)
            {
                // LA FRAPPE AU SOL : un cercle rouge grandit sous lui. Sors-en.
                slamTimer = 6f;
                attack = 1;
                windup = 0f;
                if (walker != null) walker.PlaySwing();
                ring = MakeRing();
                if (NearPlayer(40f)) Sfx.Alarm();
            }
            else if (flat < reach && strikeTimer <= 0f)
            {
                attack = 0;
                windup = 0f;
                if (walker != null) walker.PlaySwing();
            }
        }

        GameObject MakeRing()
        {
            Proto.BeginVisualOnly();
            GameObject r = Proto.Cylinder(null, transform.position + Vector3.up * 0.06f, new Vector3(1f, 0.02f, 1f), Alarm, "Cercle du Roi");
            r.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.8f, 0.08f, 0.05f), 1.4f);
            Proto.EndVisualOnly();
            return r;
        }

        /// <summary>Le balayage : tout ce qui est devant lui, a 3,8 m, est touche et projete.</summary>
        void SweepBlow()
        {
            Sfx.Whoosh();
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (!s.Alive || s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                if (Mathf.Abs(d.y) > 2.5f || Flat(d).magnitude > 3.8f || Vector3.Angle(transform.forward, Flat(d)) > 70f) continue;
                Vector3 away = Flat(d).normalized;
                Combat.Hit(s, null, damage, "sous l'épée du Roi Creux");
                Combat.Knockback(s, away * 11f + Vector3.up * 4f);
                if (s.CarriesCrown) Crown.KnockOff(s, away);
            }
        }

        /// <summary>La frappe au sol : tout ce qui est dans le cercle est touche et projete au loin.</summary>
        void Slam()
        {
            if (ring != null) Destroy(ring);
            Sfx.Thud();
            Ambiance.Burst(null, transform.position + Vector3.up * 0.3f, new Color(0.8f, 0.2f, 0.1f));
            if (NearPlayer(30f) && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.7f);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (!s.Alive || s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                if (Mathf.Abs(d.y) > 2f || Flat(d).magnitude > 5f) continue;
                Vector3 away = Flat(d).sqrMagnitude > 0.01f ? Flat(d).normalized : transform.forward;
                Combat.Hit(s, null, 45f, "écrasé par le Roi Creux");
                Combat.Knockback(s, away * 15f + Vector3.up * 6f);
                if (s.CarriesCrown) Crown.KnockOff(s, away);
            }
        }

        void GiveUp()
        {
            state = State.Return;
            chased = null;
            Suspicion = 0f;
            windup = -1f;
            aim = 0f;
            if (aimLine != null) aimLine.enabled = false;
            if (ring != null) Destroy(ring);
        }

        // ================================================================== les coups

        /// <summary>On le frappe : il encaisse, se retourne contre toi, et tombe a bout de forces.</summary>
        public void Hurt(float amount, Seeker by)
        {
            if (state == State.Dead) return;
            health -= amount;
            Sfx.Clang();
            FloatingTexts.Spawn(transform.position + Vector3.up * (rank == Rank.Roi ? 3.6f : 2.2f), "-" + Mathf.RoundToInt(amount), new Color(1f, 0.35f, 0.3f));
            Ambiance.Burst(null, transform.position + Vector3.up * 1.2f, new Color(0.85f, 0.8f, 0.7f));
            if (rank != Rank.Roi) Punch.Apply(Figure, by != null && by.Body != null ? by.Body.position : transform.position - transform.forward);
            if (health <= 0f) { Die(by); return; }
            // Un coup rompt l'annonce d'un simple garde (pas celle du Roi).
            if (rank != Rank.Roi && windup >= 0f && windup < windupTime * 0.5f) { windup = -1f; strikeTimer = 0.4f; }
            if (by != null && (state != State.Chase || chased != by && rank != Rank.Roi)) StartChase(by, true);
        }

        void Die(Seeker by)
        {
            state = State.Dead;
            deadTimer = rank == Rank.Roi ? 240f : 90f;
            chased = null;
            Suspicion = 0f;
            windup = -1f;
            if (ring != null) Destroy(ring);
            if (aimLine != null) aimLine.enabled = false;
            if (by != null && by.IsPlayer) { Stats.GuardsDowned++; if (rank == Rank.Roi) Stats.KingDowned++; }
            if (body != null) body.enabled = false;
            GameObject fig = walker != null ? walker.gameObject : hound != null ? hound.gameObject : null;
            if (fig != null) fig.SetActive(false);
            if (cone != null) cone.enabled = false;
            Ambiance.Burst(null, transform.position + Vector3.up * (rank == Rank.Roi ? 2f : 1.2f), new Color(0.9f, 0.85f, 0.75f));
            if (NearPlayer(40f) && !Sfx.Muted) AudioSource.PlayClipAtPoint(Sfx.Moan(), transform.position + Vector3.up, 0.8f);
            if (rank == Rank.Roi && Game.Hud != null && NearPlayer(80f))
                Game.Hud.ShowDiscovery("", "LE ROI CREUX EST TOMBÉ", "", "", KingEye);
        }

        void Revive()
        {
            state = rank == Rank.Roi ? State.Asleep : State.Patrol;
            health = maxHealth;
            next = 1 % route.Length;
            if (body != null) body.enabled = false;
            transform.position = route[0];
            if (body != null) body.enabled = true;
            GameObject fig = walker != null ? walker.gameObject : hound != null ? hound.gameObject : null;
            if (fig != null) fig.SetActive(true);
            if (cone != null) cone.enabled = true;
        }

        /// <summary>La fente du heaume (et le cone) dit son humeur : braise, orange, rouge, blanc au coup.</summary>
        void ShowMood()
        {
            int mood = state == State.Asleep ? 0 : windup >= 0f ? 4 : state == State.Chase ? 3 : Suspicion > 0.25f ? 2 : 1;
            if (mood != visorShown)
            {
                visorShown = mood;
                Color c = mood == 0 ? Dormant : mood == 4 ? Strike : mood == 3 ? Alarm : mood == 2 ? Wary : Ember;
                if (rank == Rank.Roi && mood == 1) c = KingEye;
                float glow = mood == 4 ? 5f : mood == 3 ? 3.2f : mood == 0 ? 0.8f : 2.4f;
                Material m = MaterialFactory.GetGlow(c, glow);
                if (walker != null && walker.Visor != null) walker.Visor.sharedMaterial = m;
                if (hound != null && hound.Eyes != null) hound.Eyes.sharedMaterial = m;
            }
            if (cone != null && rank != Rank.Roi)
            {
                Color want = state == State.Chase ? Alarm : Color.Lerp(Ember, Alarm, Suspicion);
                cone.color = Color.Lerp(cone.color, want, Time.deltaTime * 6f);
            }
        }

        // ================================================================== marcher

        /// <summary>Marche vers un point ; vrai une fois arrive.</summary>
        bool Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            if (to.magnitude < 0.35f) return true;
            if (speed <= 0f) return false;
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
            // Une voix, pas un texte sur la tete (voir Sfx.Voice). Les molosses grondent.
            Sfx.Voice(transform.position, rank == Rank.Molosse ? 1 : guardName.Length + 1, line.EndsWith("!"));
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }

    /// <summary>
    /// UN CARREAU d'arbalete : il part droit, vite (34 m/s), et laisse une trainee de
    /// braise. S'il passe a moins d'un demi-metre d'un joueur, il le touche ; s'il
    /// touche un mur ou un tronc, il s'y plante (et y reste six secondes).
    /// </summary>
    public class Bolt : MonoBehaviour
    {
        public const float Speed = 34f;
        Vector3 velocity;
        float age;
        bool stuck;
        string shooter;

        public static void Fire(Vector3 from, Vector3 velocity, string shooter)
        {
            GameObject go = new GameObject("Carreau");
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(velocity);
            Bolt b = go.AddComponent<Bolt>();
            b.velocity = velocity;
            b.shooter = shooter;
            Proto.BeginVisualOnly();
            Proto.Cube(go.transform, new Vector3(0f, 0f, -0.2f), new Vector3(0.025f, 0.025f, 0.5f), new Color(0.2f, 0.16f, 0.12f), "Fût");
            GameObject tip = Proto.Cube(go.transform, new Vector3(0f, 0f, 0.06f), new Vector3(0.05f, 0.05f, 0.1f), Color.white, "Pointe");
            tip.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.4f, 0.15f), 3f);
            Proto.EndVisualOnly();
            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.05f;
            trail.endWidth = 0f;
            trail.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.45f, 0.2f), 1.5f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || stuck) return;
            age += dt;
            if (age > 2f) { Destroy(gameObject); return; }
            Vector3 from = transform.position;
            Vector3 step = velocity * dt;

            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (!s.Alive || s.Body == null) continue;
                Vector3 c = s.Body.position + Vector3.up * 1.1f;
                float t = Mathf.Clamp01(Vector3.Dot(c - from, step) / Mathf.Max(0.0001f, step.sqrMagnitude));
                if ((from + step * t - c).magnitude > 0.55f) continue;
                Combat.Hit(s, null, 18f, "sous un carreau de " + shooter);
                Destroy(gameObject);
                return;
            }

            RaycastHit hit;
            if (Physics.Raycast(from, step.normalized, out hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<Guard>() == null)
            {
                transform.position = hit.point - step.normalized * 0.15f;
                stuck = true;
                TrailRenderer trail = GetComponent<TrailRenderer>();
                if (trail != null) trail.emitting = false;
                Destroy(gameObject, 6f);
                return;
            }
            transform.position += step;
        }
    }

    /// <summary>
    /// LE MOLOSSE : un grand chien noir sous une barde d'ivoire -- la meme que les
    /// chevaliers, le meme masque sans yeux, la meme fente qui luit. Il s'anime tout
    /// seul, comme Walker : il mesure sa vitesse et fait trotter ses quatre pattes.
    /// </summary>
    public class HoundBody : MonoBehaviour
    {
        readonly Transform[] legs = new Transform[4];
        Transform head, torso;
        public Renderer Eyes;
        float cycle, speed, lunge;
        Vector3 last;
        bool hasLast;

        public static HoundBody Build(Transform parent)
        {
            GameObject go = new GameObject("Molosse");
            go.transform.SetParent(parent, false);
            HoundBody h = go.AddComponent<HoundBody>();
            Transform t = go.transform;
            Color fur = new Color(0.08f, 0.07f, 0.08f);
            Color ivory = new Color(0.78f, 0.74f, 0.66f);
            Color shade = new Color(0.58f, 0.55f, 0.5f);

            Proto.BeginVisualOnly();
            h.torso = Walker.Node(t, new Vector3(0f, 0.66f, 0f), "Corps");
            GameObject trunk = Proto.Capsule(h.torso, Vector3.zero, new Vector3(0.4f, 0.5f, 0.44f), fur, "Tronc");
            trunk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Proto.Sphere(h.torso, new Vector3(0f, 0.08f, 0.22f), new Vector3(0.48f, 0.46f, 0.5f), ivory, "Barde");
            Proto.Sphere(h.torso, new Vector3(0f, 0.12f, -0.18f), new Vector3(0.4f, 0.26f, 0.44f), shade, "Croupière");
            GameObject tail = Proto.Capsule(h.torso, new Vector3(0f, 0.1f, -0.52f), new Vector3(0.07f, 0.2f, 0.07f), fur, "Queue");
            tail.transform.localRotation = Quaternion.Euler(-50f, 0f, 0f);

            h.head = Walker.Node(h.torso, new Vector3(0f, 0.26f, 0.5f), "Tête");
            Proto.Capsule(h.head, new Vector3(0f, -0.08f, -0.06f), new Vector3(0.18f, 0.16f, 0.18f), fur, "Cou");
            GameObject skull = Proto.Sphere(h.head, new Vector3(0f, 0.06f, 0.12f), new Vector3(0.26f, 0.25f, 0.44f), ivory, "Masque");
            skull.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            Proto.Sphere(h.head, new Vector3(0f, -0.04f, 0.26f), new Vector3(0.16f, 0.12f, 0.24f), fur, "Gueule");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject ear = Proto.Capsule(h.head, new Vector3(side * 0.09f, 0.2f, 0.02f), new Vector3(0.05f, 0.08f, 0.04f), fur, "Oreille");
                ear.transform.localRotation = Quaternion.Euler(-20f, 0f, side * 15f);
            }
            GameObject slit = Proto.Cube(h.head, new Vector3(0f, 0.1f, 0.33f), new Vector3(0.17f, 0.025f, 0.03f), Color.white, "Fente");
            h.Eyes = slit.GetComponent<Renderer>();
            h.Eyes.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.55f, 0.22f), 2.6f);

            Vector3[] hips = { new Vector3(-0.14f, 0f, 0.3f), new Vector3(0.14f, 0f, 0.3f), new Vector3(-0.14f, 0f, -0.3f), new Vector3(0.14f, 0f, -0.3f) };
            for (int i = 0; i < 4; i++)
            {
                h.legs[i] = Walker.Node(h.torso, hips[i], "Patte");
                Proto.Capsule(h.legs[i], new Vector3(0f, -0.3f, 0f), new Vector3(0.1f, 0.3f, 0.11f), fur, "Jambe");
                Proto.Sphere(h.legs[i], new Vector3(0f, -0.14f, 0.02f), new Vector3(0.13f, 0.14f, 0.14f), shade, "Jambière");
            }
            Proto.EndVisualOnly();
            return h;
        }

        /// <summary>Il se jette en avant : c'est l'annonce de sa morsure.</summary>
        public void Lunge() { lunge = 0.3f; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position;
            float measured = 0f;
            if (hasLast) { Vector3 d = p - last; d.y = 0f; measured = Mathf.Min(d.magnitude / dt, 12f); }
            last = p;
            hasLast = true;
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-8f * dt));
            float moving = Mathf.Clamp01(speed / 1.5f);
            cycle += speed * dt * 4.2f;
            float s = Mathf.Sin(cycle);
            float swing = Mathf.Lerp(18f, 42f, Mathf.Clamp01(speed / 8f)) * moving;
            legs[0].localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            legs[3].localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            legs[1].localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
            legs[2].localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
            if (lunge > 0f) lunge -= dt;
            float jump = lunge > 0f ? Mathf.Sin(lunge / 0.3f * Mathf.PI) : 0f;
            torso.localPosition = new Vector3(0f, 0.66f + Mathf.Abs(Mathf.Cos(cycle)) * 0.05f * moving + jump * 0.12f, jump * 0.35f);
            torso.localRotation = Quaternion.Euler(-jump * 12f + Mathf.Sin(Time.time * 1.3f) * 1.5f * (1f - moving), 0f, 0f);
            head.localRotation = Quaternion.Euler(Mathf.Sin(cycle * 2f) * 4f * moving - jump * 10f, 0f, 0f);
        }
    }
}
