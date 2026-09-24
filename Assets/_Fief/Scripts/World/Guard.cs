using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN GARDE DU CHATEAU.
    ///
    /// Il fait sa ronde, sa lanterne braquee devant lui : le cone de lumiere qu'on
    /// voit balayer le sol, c'est EXACTEMENT ce qu'il voit. Reste hors du cone, et
    /// il ne te voit pas.
    ///
    /// Il ne s'occupe pas des promeneurs. Il s'occupe des VOLEURS : de qui porte du
    /// Fer ancien, et de qui entre dans une reserve. S'il en voit un, un "?"
    /// apparait au-dessus de sa tete, puis un "!" -- et il court. S'il te
    /// rattrape, il te prend ton fer et te jette dehors, devant la grande porte.
    /// Chargee de fer, tu cours moins vite que lui. C'est voulu.
    ///
    /// ET IL EST MAL PAYE. Parle-lui : pour quelques pieces il regarde ailleurs
    /// trois minutes (son cone palit), et pour davantage il ouvre la POTERNE, la
    /// porte derobee du mur nord. Rien d'autre ne l'ouvre. C'est le coeur du jeu.
    ///
    /// Les rivaux aussi volent du fer. Les gardes les chassent aussi.
    /// </summary>
    public class Guard : MonoBehaviour, IInteractable, IDialogue
    {
        public static readonly List<Guard> All = new List<Guard>();

        enum State { Patrol, Chase, Return }

        public GuardInfo info;

        State state = State.Patrol;
        Vector3[] route;
        int next = 1;
        float pause;
        CharacterController body;
        float fall;
        Transform figure;
        Light cone;
        float lookSweep;

        Seeker chased;
        float chaseTimer;
        float barkTimer;

        public float Suspicion { get; private set; }
        public bool Chasing { get { return state == State.Chase; } }

        const float SightRange = 16f;
        const float SightAngle = 34f;       // demi-angle : le cone fait 68 degres
        const float WalkSpeed = 1.7f;
        const float RunSpeed = 6.4f;

        static readonly Color Tabard = new Color(0.42f, 0.10f, 0.10f);
        static readonly Color TabardDark = new Color(0.28f, 0.07f, 0.07f);
        static readonly Color Steel = new Color(0.40f, 0.41f, 0.43f);
        static readonly Color Skin = new Color(0.55f, 0.46f, 0.40f);
        static readonly Color ConeCalm = new Color(1f, 0.82f, 0.55f);
        static readonly Color ConeAlarm = new Color(1f, 0.3f, 0.2f);
        static readonly Color ConeBribed = new Color(0.5f, 0.6f, 0.75f);

        // ================================================================== construction

        public static Guard Build(Transform parent, GuardInfo info, Vector3[] route)
        {
            GameObject root = new GameObject("GARDE " + info.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = route[0];

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.4f;

            Guard g = root.AddComponent<Guard>();
            g.info = info;
            g.route = route;
            g.body = cc;

            Proto.BeginVisualOnly();
            Figures.Shape f = Figures.Robed(root.transform, 2.0f, 1.0f, Tabard, TabardDark, Skin, false);
            g.figure = f.root;
            float head = f.shoulders + 0.3f * (2.0f / 2.6f);
            // Le camail et le casque.
            Proto.Cube(f.root, new Vector3(0f, f.shoulders + 0.02f, 0f), new Vector3(0.7f, 0.16f, 0.46f), Steel, "Camail");
            Proto.Cube(f.root, new Vector3(0f, head + 0.12f, 0f), new Vector3(0.31f, 0.14f, 0.31f), Steel, "Casque");
            Proto.Cone(f.root, new Vector3(0f, head + 0.19f, 0f), 0.2f, 0.16f, Steel, "Timbre", 6);
            // La croix blanche sur le tabard : c'est la garde, on la reconnait de loin.
            Proto.Cube(f.root, new Vector3(0f, 1.3f, 0.3f), new Vector3(0.08f, 0.5f, 0.02f), new Color(0.8f, 0.78f, 0.72f), "Croix");
            Proto.Cube(f.root, new Vector3(0f, 1.4f, 0.3f), new Vector3(0.34f, 0.08f, 0.02f), new Color(0.8f, 0.78f, 0.72f), "Croix");
            // La hallebarde.
            Proto.Cube(f.root, new Vector3(0.46f, 1.5f, 0.12f), new Vector3(0.06f, 3.0f, 0.06f), new Color(0.25f, 0.19f, 0.13f), "Hampe");
            Proto.Cube(f.root, new Vector3(0.46f, 2.9f, 0.22f), new Vector3(0.04f, 0.4f, 0.26f), Steel, "Fer");
            Proto.Cone(f.root, new Vector3(0.46f, 3.0f, 0.12f), 0.05f, 0.35f, Steel, "Pique", 4);
            // La lanterne, tenue haut devant lui.
            Vector3 lantern = new Vector3(-0.4f, 1.25f, 0.34f);
            GameObject flame = Proto.Cube(f.root, lantern, new Vector3(0.1f, 0.14f, 0.1f), Color.white, "Flamme");
            flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.74f, 0.4f), 2.6f);
            flame.AddComponent<Flame>();
            Proto.Cube(f.root, lantern + new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.04f, 0.18f), Steel, "Lanterne");
            Proto.EndVisualOnly();

            // LE CONE : une lumiere "spot", exactement l'angle et la portee de sa vue.
            GameObject coneGo = new GameObject("Regard de " + info.Name);
            coneGo.transform.SetParent(f.root, false);
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

            bool talking = Game.Hud != null && Game.Hud.PanelOpen && NearPlayer(4.5f);
            if (talking)
            {
                Figures.Face(transform, Game.PlayerTransform.position, 120f);
                ColourCone(season);
                return;
            }

            Watch(season, dt);

            switch (state)
            {
                case State.Patrol: Patrol(dt); break;
                case State.Chase: Chase(dt); break;
                case State.Return:
                    if (Walk(route[next], WalkSpeed * 1.6f, dt)) state = State.Patrol;
                    break;
            }
            ColourCone(season);
            Figures.Breathe(figure, info.Name.Length);
        }

        /// <summary>
        /// Regarder. Pour chaque chercheur (toi, les rivaux) : est-il SUSPECT (du fer
        /// sur lui, ou dans une reserve) et VISIBLE (dans le cone, pas cache par un
        /// mur) ? Si oui, la suspicion monte -- vite de pres, lentement de loin.
        /// </summary>
        void Watch(Season season, float dt)
        {
            if (state == State.Chase) return;

            Seeker seen = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || !Suspect(s)) continue;
                if (s.IsPlayer && info.Bribed(season.Elapsed)) continue;
                float d;
                if (!InSight(s.Body.position, out d)) continue;
                if (d < bestDistance) { bestDistance = d; seen = s; }
            }

            if (seen != null)
            {
                float rate = bestDistance < 6f ? 2.6f : 1.1f;
                Suspicion = Mathf.Min(1f, Suspicion + rate * dt);
                if (Suspicion > 0.2f) Figures.Face(transform, seen.Body.position, 90f);
                if (Suspicion > 0.35f) Bark("Qui va la ?");
                if (Suspicion >= 1f)
                {
                    state = State.Chase;
                    chased = seen;
                    chaseTimer = 0f;
                    barkTimer = 0f;
                    Bark(seen.IsPlayer ? "HALTE ! Voleur !" : "Halte, " + seen.Name + " !");
                    if (seen.IsPlayer) Sfx.Deny();
                }
            }
            else
            {
                Suspicion = Mathf.Max(0f, Suspicion - 0.45f * dt);
            }
        }

        static bool Suspect(Seeker s)
        {
            return s.Bag.Get(ResourceType.Iron) > 0 || Castle.InStoreroom(s.Body.position.x, s.Body.position.z);
        }

        bool InSight(Vector3 target, out float distance)
        {
            Vector3 eye = transform.position + Vector3.up * 1.7f;
            Vector3 to = target + Vector3.up * 1.1f - eye;
            distance = to.magnitude;
            if (distance > SightRange) return false;
            Vector3 flatTo = new Vector3(to.x, 0f, to.z);
            if (distance > 2f && Vector3.Angle(transform.forward, flatTo) > SightAngle) return false;

            RaycastHit hit;
            if (Physics.Raycast(eye, to / distance, out hit, distance - 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Touche quelqu'un d'autre que lui-meme avant la cible : un mur, un tronc.
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
                transform.Rotate(0f, Mathf.Sin(lookSweep * 0.9f) * 40f * dt, 0f);
                return;
            }
            if (Walk(route[next], WalkSpeed, dt))
            {
                next = (next + 1) % route.Length;
                pause = 3.5f;
            }
        }

        void Chase(float dt)
        {
            chaseTimer += dt;
            if (chased == null || chased.Body == null) { GiveUp(); return; }
            Vector3 p = chased.Body.position;

            // Hors du chateau, ou plus de fer et sorti des reserves : il abandonne.
            if (!Castle.Covers(p.x, p.z, 10f) || chaseTimer > 30f || !Suspect(chased) && chaseTimer > 3f)
            {
                Bark("Et que je ne te revoie pas !");
                GiveUp();
                return;
            }
            Walk(p, RunSpeed, dt);
            if (Flat(p - transform.position).magnitude < 1.8f) Seize(chased);
        }

        void GiveUp()
        {
            state = State.Return;
            chased = null;
            Suspicion = 0f;
        }

        /// <summary>Rattrape : il prend tout le fer, et jette le voleur devant la grande porte.</summary>
        void Seize(Seeker s)
        {
            int iron = s.Bag.Get(ResourceType.Iron);
            int taken = s.Bag.TryRemove(ResourceType.Iron, iron);
            if (Game.Garrison != null) Game.Garrison.IronSeized += taken;

            float x = s.IsPlayer ? 0f : (Random.value - 0.5f) * 8f;
            Vector3 outside = Ground.Place(x, -Castle.HalfSize - 9f, 0.1f);
            if (s.IsPlayer)
            {
                if (Game.Player != null) Game.Player.Teleport(outside, 180f);
                Sfx.Deny();
                if (Game.Hud != null)
                {
                    Game.Hud.ClosePanel();
                    Game.Hud.ShowDiscovery("LA GARDE", info.Name + " t'a jete dehors",
                                           taken > 0 ? "Il garde tes " + taken + " fer ancien." : "Tu n'avais rien a lui prendre. Cette fois.",
                                           "Les gardes sont mal payes. Parle-leur avant de voler.", ConeAlarm);
                }
            }
            else
            {
                Rival r = Rival.Of(s);
                if (r != null) r.Teleport(outside);
                if (NearPlayer(35f)) Toasts.Show("La garde jette " + s.Name + " hors du chateau.", s.Colour);
            }
            GiveUp();
        }

        void ColourCone(Season season)
        {
            if (cone == null) return;
            bool bribed = info.Bribed(season.Elapsed);
            Color want = state == State.Chase ? ConeAlarm : bribed ? ConeBribed : Color.Lerp(ConeCalm, ConeAlarm, Suspicion);
            cone.color = Color.Lerp(cone.color, want, Time.deltaTime * 6f);
            cone.intensity = bribed ? 0.8f : 2.2f;
        }

        // ================================================================== marcher

        /// <summary>Marche vers un point ; vrai une fois arrive.</summary>
        bool Walk(Vector3 destination, float speed, float dt)
        {
            Vector3 to = Flat(destination - transform.position);
            if (to.magnitude < 0.3f) return true;
            Vector3 dir = to.normalized;
            fall = body.isGrounded ? -1f : fall - 22f * dt;
            body.Move((dir * speed + Vector3.up * fall) * dt);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 240f * dt);
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
            FloatingTexts.Spawn(transform.position + Vector3.up * 2.5f, info.Name + " : " + line, new Color(1f, 0.7f, 0.55f));
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return state == State.Patrol && Suspicion < 0.3f; } }
        public string Prompt { get { return "Parler au garde " + info.Name; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null) return;
            Game.Hud.OpenPanel(new DialoguePanel(this));
            Sfx.Pop();
        }

        // ================================================================== IDialogue

        public string Speaker { get { return "LE GARDE " + info.Name.ToUpperInvariant(); } }
        public Color Tint { get { return new Color(0.9f, 0.55f, 0.45f); } }

        float Now { get { return Game.Season != null ? Game.Season.Elapsed : 0f; } }

        public string Body
        {
            get
            {
                if (info.Bribed(Now))
                    return "\"Je ne t'ai pas vu. Je ne te vois pas. Et dans " + Hud.Clock(info.BribedUntil - Now)
                         + ", je recommencerai a te voir. File.\"";

                string mood = info.Loyalty < 0.3f ? "Il crache par terre en parlant du chateau."
                            : info.Loyalty < 0.6f ? "Il hesite avant de repondre, et regarde derriere lui."
                            : "Il se tient droit. Il croit encore a quelque chose.";
                string wage = "\"On m'a promis " + info.Wage + " deniers par jour. "
                            + (info.MonthsUnpaid > 0 ? "Je n'ai rien vu depuis " + info.MonthsUnpaid + " mois.\"" : "Et on me paie. Pour l'instant.\"");
                string rule = "\"Tu peux traverser la cour. Mais du fer sur toi, ou un pied dans une reserve, et je te jette dehors.\"";
                string gold = "\n\nTu as " + (Game.Wallet != null ? Game.Wallet.Gold : 0) + " or.";
                string posterne = Game.Garrison != null && Game.Garrison.PosterneOpen
                    ? "\n\nLa poterne du mur nord est ouverte. " + Game.Garrison.PosterneOpenedBy + " a tire le verrou."
                    : "";
                return wage + "\n\n" + mood + "\n\n" + rule + gold + posterne;
            }
        }

        public int ChoiceCount { get { return 3; } }

        public string ChoiceLabel(int index)
        {
            if (index == 0) return "Lui glisser " + info.LookAwayPrice + " or : qu'il regarde ailleurs trois minutes";
            if (index == 1) return "Lui glisser " + info.PosternePrice + " or : qu'il ouvre la poterne du mur nord";
            return "Partir";
        }

        public bool ChoiceEnabled(int index)
        {
            Wallet purse = Game.Wallet;
            if (purse == null || Game.Garrison == null) return index == 2;
            if (index == 0) return !info.Bribed(Now) && purse.CanAfford(info.LookAwayPrice);
            if (index == 1) return !Game.Garrison.PosterneOpen && purse.CanAfford(info.PosternePrice);
            return true;
        }

        public bool Choose(int index)
        {
            if (index == 0 && Game.Garrison.RequestLookAway(Game.Wallet, info, Now))
            {
                Sfx.Coin();
                Toasts.Show(info.Name + " empoche les pieces et se tourne vers le mur.", new Color(0.95f, 0.8f, 0.4f));
                return false;
            }
            if (index == 1 && Game.Garrison.RequestPosterne(Game.Wallet, info))
            {
                Sfx.Coin();
                Poterne.OpenAll();
                if (Game.Hud != null)
                    Game.Hud.ShowDiscovery("LA POTERNE", "est ouverte",
                                           info.Name + " a tire le verrou pour " + info.PosternePrice + " or.",
                                           "Une porte derobee ne se force pas. Elle s'achete.", new Color(0.95f, 0.8f, 0.4f));
                return true;
            }
            return true;
        }
    }
}
