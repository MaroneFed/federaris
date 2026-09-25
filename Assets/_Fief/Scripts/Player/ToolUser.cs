using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON FAIT AVEC CE QU'ON TIENT.
    ///
    ///   1 / 2     prendre en main l'outil de l'emplacement (ou le ranger) ;
    ///   clic      avec la HACHE, face a un arbre : l'abattre, coup apres coup ;
    ///             avec l'EPEE : frapper (voir Combat) ;
    ///   F         face a un arbre : GRIMPER s'installer sur une branche, a quatre
    ///             metres ; F encore pour redescendre. Personne ne regarde en l'air.
    ///
    /// L'outil tenu se voit en bas a droite de l'ecran -- l'outil seulement, pas
    /// de mains (la regle du corps invisible tient). Il bouge quand on frappe.
    /// </summary>
    public class ToolUser : MonoBehaviour
    {
        /// <summary>Ce que le HUD affiche pres du centre de l'ecran ("Clic : abattre").</summary>
        public static string Hint;
        public static bool Aiming;
        /// <summary>Un ennemi (rival, bete) a portee d'epee, dans l'axe : le reticule rougit.</summary>
        public static bool FoeInReach;

        static readonly Dictionary<Collider, int> Chops = new Dictionary<Collider, int>();

        PlayerController player;
        Transform viewModel;
        ToolKind shownKind = ToolKind.None;
        float swingTimer;
        float swing;                 // 0 -> 1 : l'animation du coup en cours
        Transform perch;             // la plate-forme dans l'arbre, si l'on y est

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        void Update()
        {
            Hint = null;
            Aiming = false;
            FoeInReach = false;
            Seeker me = Game.Me;
            if (me == null || player == null) return;
            Kit kit = me.Kit;

            UpdateViewModel(kit.Held != null ? kit.Held.Kind : ToolKind.None);
            if (player.InputLocked) return;

            int was = kit.Active;
            if (FiefInput.Slot1Pressed) kit.Select(0);
            if (FiefInput.Slot2Pressed) kit.Select(1);
            // La molette change d'outil, comme partout ailleurs.
            float wheel = FiefInput.ZoomNotches;
            if (Mathf.Abs(wheel) > 0.01f) kit.Cycle(wheel > 0f ? -1 : 1);
            if (kit.Active != was) Sfx.Pop();

            Transform eye = player.cameraTransform;
            if (eye == null) return;

            // Une escalade interrompue (tombe, jete dehors, releve a sa stele) : on
            // oublie le perchoir.
            if (climbing && !player.Scripted) climbing = false;
            if (!climbing && perch != null && (transform.position - perch.position).magnitude > 3f)
            {
                Destroy(perch.gameObject);
                perch = null;
            }

            // --- on grimpe (ou on descend) : rien d'autre pendant ce temps
            if (climbing)
            {
                Hint = climbingUp ? "Tu grimpes..." : "Tu redescends...";
                Climb();
                return;
            }

            // --- descendre de l'arbre
            if (perch != null)
            {
                Hint = "F : redescendre";
                if (FiefInput.ClimbPressed) ClimbDown();
                return;
            }

            // --- ce qu'on vise, a portee de bras
            RaycastHit hit;
            bool tree = Physics.Raycast(eye.position, eye.forward, out hit, 3.2f, ~0, QueryTriggerInteraction.Ignore)
                        && Forest.IsTree(hit.collider);
            Aiming = kit.Held != null;
            if (kit.Holding(ToolKind.Epee)) FoeInReach = Combat.FoeAhead(eye);

            if (tree)
            {
                if (FiefInput.ClimbPressed) { ClimbUp(hit.collider); return; }
                if (kit.Holding(ToolKind.Hache))
                {
                    int done;
                    Chops.TryGetValue(hit.collider, out done);
                    Hint = "Clic : abattre l'arbre  (" + done + " / " + ChopsNeeded(hit.collider) + ")      F : grimper";
                }
                else Hint = kit.Held == null ? "F : grimper dans l'arbre      (une hache pour l'abattre : Tab, Artisanat)" : "F : grimper dans l'arbre";
            }

            // --- poser un piege
            swingTimer -= Time.deltaTime;
            if (kit.Holding(ToolKind.Piege))
            {
                Hint = "Clic : poser le piège devant toi   (" + Trap.CountOf(me) + " / " + Trap.MaxFor(me) + " posés)";
                if (FiefInput.UseHeld && swingTimer <= 0f)
                {
                    swingTimer = 0.8f;
                    swing = 1f;
                    PlaceTrap(me, kit);
                }
                swing = Mathf.Max(0f, swing - Time.deltaTime * 3.2f);
                AnimateViewModel();
                return;
            }

            // --- frapper
            if (FiefInput.UseHeld && kit.Held != null && swingTimer <= 0f)
            {
                float penalty = Game.Brewed ? 1f : Mathf.Lerp(1f, 1.8f, me.Bag.Load01);
                swingTimer = (kit.Holding(ToolKind.Epee) ? 0.55f : 0.7f) * penalty;
                swing = 1f;
                if (kit.Holding(ToolKind.Hache) && tree) Chop(hit, kit);
                else if (kit.Holding(ToolKind.Epee)) Combat.PlayerStrike(eye);
                else Sfx.Whoosh();
            }
            swing = Mathf.Max(0f, swing - Time.deltaTime * 3.2f);
            AnimateViewModel();
        }

        // ================================================================== abattre

        static int ChopsNeeded(Collider c)
        {
            CapsuleCollider cap = c as CapsuleCollider;
            float r = cap != null ? cap.radius * c.transform.lossyScale.x : 0.3f;
            return Mathf.Clamp(Mathf.RoundToInt(3f + r * 6f), 3, 7);
        }

        void Chop(RaycastHit hit, Kit kit)
        {
            Collider c = hit.collider;
            int done;
            Chops.TryGetValue(c, out done);
            done++;
            Chops[c] = done;

            Sfx.Harvest(ResourceType.Iron);
            Sfx.HarvestTap(ResourceType.Moonstone);
            Ambiance.Burst(null, hit.point + hit.normal * 0.1f, new Color(0.62f, 0.48f, 0.3f));
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.06f);

            if (kit.Wear(1))
            {
                Sfx.Deny();
                Toasts.Show("Ta hache s'est brisée.", new Color(0.8f, 0.6f, 0.4f));
            }

            if (done < ChopsNeeded(c)) return;

            Chops.Remove(c);
            Vector3 away = c.transform.position - transform.position;
            away.y = 0f;
            TreeFall.Fell(c.gameObject, away.sqrMagnitude > 0.01f ? away.normalized : transform.forward);
        }

        // ================================================================== pieger

        void PlaceTrap(Seeker me, Kit kit)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            Vector3 at = transform.position + forward * 1.6f;
            string why = Trap.WhyNot(me, at);
            if (why != null)
            {
                Sfx.Deny();
                Toasts.Show(why, UiStyle.InkDim);
                return;
            }
            Trap.Place(me, at, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
            kit.Wear(1);                 // le piege quitte la main : l'emplacement se libere
            Sfx.Build();
            OrbitCamera.Crouch = Mathf.Max(OrbitCamera.Crouch, 1f);
            Toasts.Show("Piège posé.", new Color(0.95f, 0.62f, 0.35f));
        }

        // ================================================================== grimper

        // Le mouvement en cours : on monte (ou on descend) le long du tronc, traction
        // apres traction. Pendant ce temps, rien d'autre ne se fait.
        bool climbing;
        bool climbingUp;
        float climbT;
        float climbDuration;
        Vector3 climbStart, climbBase, climbTop, climbEnd;
        int pullsDone;
        const int Pulls = 5;

        /// <summary>
        /// S'installer dans l'arbre : une petite plate-forme de branches a quatre
        /// metres, contre le tronc. On y MONTE (1,6 s) : on s'approche du tronc, puis
        /// cinq tractions, chacune avec son froissement de branches, un peu de
        /// balancement ; enfin on se hisse sur la plate-forme. Personne ne regarde
        /// en l'air dans une foret : c'est le meilleur poste de guet du jeu.
        /// </summary>
        void ClimbUp(Collider trunk)
        {
            Vector3 centre = trunk.bounds.center;
            Vector3 toMe = transform.position - centre;
            toMe.y = 0f;
            toMe = toMe.sqrMagnitude > 0.01f ? toMe.normalized : Vector3.forward;
            float ground = Ground.Sample(centre.x, centre.z);
            Vector3 spot = new Vector3(centre.x, ground + 4.2f, centre.z) + toMe * 0.9f;

            GameObject platform = new GameObject("Perchoir");
            platform.transform.position = spot;
            platform.transform.rotation = Quaternion.LookRotation(toMe, Vector3.up);
            BoxCollider floor = platform.AddComponent<BoxCollider>();
            floor.size = new Vector3(1.5f, 0.2f, 1.5f);
            floor.center = new Vector3(0f, -0.1f, 0f);
            // Un parapet invisible : on ne tombe pas en se retournant.
            AddRail(platform.transform, new Vector3(0f, 0.5f, 0.8f), new Vector3(1.6f, 1f, 0.1f));
            AddRail(platform.transform, new Vector3(0.8f, 0.5f, 0f), new Vector3(0.1f, 1f, 1.6f));
            AddRail(platform.transform, new Vector3(-0.8f, 0.5f, 0f), new Vector3(0.1f, 1f, 1.6f));
            Proto.BeginVisualOnly();
            Color wood = new Color(0.26f, 0.2f, 0.14f);
            for (int i = -1; i <= 1; i++)
            {
                GameObject plank = Proto.Cube(platform.transform, new Vector3(i * 0.45f, -0.08f, 0f), new Vector3(0.4f, 0.08f, 1.5f), wood, "Branche");
                plank.transform.localRotation = Quaternion.Euler(0f, i * 6f, 0f);
            }
            Proto.EndVisualOnly();
            perch = platform.transform;

            // Le chemin : le pied du tronc, puis tout droit le long de l'ecorce,
            // puis le rebord de la plate-forme.
            Vector3 hug = centre + toMe * 0.75f;
            StartClimb(true, transform.position,
                       new Vector3(hug.x, ground + 0.05f, hug.z),
                       new Vector3(hug.x, spot.y - 0.2f, hug.z),
                       spot + Vector3.up * 0.05f, 1.6f);
            Toasts.Show("Tu grimpes. F pour redescendre.", UiStyle.InkDim);
        }

        static void AddRail(Transform parent, Vector3 at, Vector3 size)
        {
            GameObject rail = new GameObject("Rambarde");
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = at;
            rail.AddComponent<BoxCollider>().size = size;
        }

        void ClimbDown()
        {
            Vector3 trunkSide = perch.position - perch.forward * 0.15f;
            Vector3 landing = Ground.Place(perch.position.x + perch.forward.x * 1.2f, perch.position.z + perch.forward.z * 1.2f, 0.1f);
            float ground = Ground.Sample(trunkSide.x, trunkSide.z);
            StartClimb(false, transform.position,
                       new Vector3(trunkSide.x, transform.position.y - 0.1f, trunkSide.z),
                       new Vector3(trunkSide.x, ground + 0.1f, trunkSide.z),
                       landing, 1.1f);
        }

        void StartClimb(bool up, Vector3 start, Vector3 baseAt, Vector3 top, Vector3 end, float duration)
        {
            climbing = true;
            climbingUp = up;
            climbT = 0f;
            climbDuration = duration;
            climbStart = start;
            climbBase = baseAt;
            climbTop = top;
            climbEnd = end;
            pullsDone = 0;
            player.BeginScripted();
            Sfx.HarvestTap(ResourceType.Deadwood);
        }

        /// <summary>Un pas de l'animation. Trois temps : s'approcher, grimper, se poser.</summary>
        void Climb()
        {
            climbT = Mathf.Min(1f, climbT + Time.deltaTime / climbDuration);
            float t = climbT;
            Vector3 p;
            if (t < 0.15f)
            {
                p = Vector3.Lerp(climbStart, climbBase, Mathf.SmoothStep(0f, 1f, t / 0.15f));
            }
            else if (t < 0.88f)
            {
                // Les tractions : la hauteur avance par a-coups (vite pendant la
                // traction, presque rien entre deux), avec un leger balancement.
                float u = (t - 0.15f) / 0.73f;
                float steps = u * Pulls;
                float within = steps - Mathf.Floor(steps);
                float eased = (Mathf.Floor(steps) + Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(within * 1.6f))) / Pulls;
                p = Vector3.Lerp(climbBase, climbTop, Mathf.Clamp01(eased));
                Vector3 side = Vector3.Cross(Vector3.up, climbTop - climbEnd).normalized;
                p += side * Mathf.Sin(steps * Mathf.PI) * 0.06f;
                int pull = Mathf.FloorToInt(steps);
                if (pull > pullsDone && pull <= Pulls)
                {
                    pullsDone = pull;
                    Sfx.HarvestTap(ResourceType.Deadwood);
                    if (pull == 2) Sfx.Creak3D(p + Vector3.up);
                    if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.04f);
                }
            }
            else
            {
                p = Vector3.Lerp(climbTop, climbEnd, Mathf.SmoothStep(0f, 1f, (t - 0.88f) / 0.12f));
            }
            player.ScriptedMove(p);

            if (climbT < 1f) return;
            climbing = false;
            player.EndScripted(climbEnd);
            if (climbingUp)
            {
                Sfx.Step();
            }
            else
            {
                if (perch != null) Destroy(perch.gameObject);
                perch = null;
                Sfx.Step();
            }
        }

        // ================================================================== l'outil en main

        void UpdateViewModel(ToolKind kind)
        {
            if (kind == shownKind) return;
            shownKind = kind;
            if (viewModel != null) Destroy(viewModel.gameObject);
            viewModel = null;
            if (kind == ToolKind.None || player.cameraTransform == null) return;

            GameObject go = new GameObject("Outil en main");
            go.transform.SetParent(player.cameraTransform, false);
            viewModel = go.transform;
            Proto.BeginVisualOnly();
            Color wood = new Color(0.36f, 0.26f, 0.16f);
            Color steel = new Color(0.62f, 0.64f, 0.68f);
            if (kind == ToolKind.Piege)
            {
                // Le piege ferme, tenu par sa chaine : un anneau de fer herisse.
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    GameObject seg = Proto.Cube(go.transform, new Vector3(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.09f, 0f),
                                                new Vector3(0.05f, 0.02f, 0.02f), Trap.Iron, "Mâchoire");
                    seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
                }
                Proto.Cube(go.transform, new Vector3(0f, -0.14f, 0f), new Vector3(0.015f, 0.12f, 0.015f), Trap.Iron, "Chaîne");
            }
            else if (kind == ToolKind.Hache)
            {
                // LA HACHE : un manche de frene legerement courbe, enroule de cuir a
                // la prise, une tete forgee (douille, joue, tranchant courbe et clair).
                Transform t = go.transform;
                GameObject shaft = Proto.Cylinder(t, new Vector3(0f, 0.02f, 0f), new Vector3(0.036f, 0.28f, 0.036f), wood, "Manche");
                shaft.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
                for (int i = 0; i < 4; i++)
                    Proto.Cylinder(t, new Vector3(0f, -0.2f + i * 0.035f, 0f), new Vector3(0.042f, 0.012f, 0.042f),
                                   i % 2 == 0 ? new Color(0.24f, 0.16f, 0.1f) : new Color(0.3f, 0.2f, 0.12f), "Cuir");
                Proto.Cylinder(t, new Vector3(0f, -0.27f, 0f), new Vector3(0.046f, 0.012f, 0.046f), new Color(0.2f, 0.14f, 0.09f), "Talon");
                Color iron = new Color(0.34f, 0.35f, 0.37f);
                Proto.Cube(t, new Vector3(0f, 0.26f, 0f), new Vector3(0.05f, 0.08f, 0.05f), iron, "Douille");
                Proto.Cube(t, new Vector3(0.055f, 0.26f, 0f), new Vector3(0.07f, 0.07f, 0.022f), iron, "Joue");
                GameObject edge = Proto.Cylinder(t, new Vector3(0.1f, 0.26f, 0f), new Vector3(0.13f, 0.011f, 0.13f), iron, "Tranchant");
                edge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GameObject bright = Proto.Cylinder(t, new Vector3(0.115f, 0.26f, 0f), new Vector3(0.115f, 0.012f, 0.115f), new Color(0.72f, 0.74f, 0.78f), "Fil");
                bright.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Proto.Cube(t, new Vector3(-0.035f, 0.26f, 0f), new Vector3(0.03f, 0.05f, 0.04f), iron, "Marteau");
            }
            else
            {
                // L'EPEE : pommeau rond, poignee filetee, garde aux quillons evases,
                // lame a gouttiere sombre qui s'effile en pointe.
                Transform t = go.transform;
                Color bronze = new Color(0.55f, 0.44f, 0.22f);
                Proto.Sphere(t, new Vector3(0f, -0.2f, 0f), new Vector3(0.05f, 0.05f, 0.05f), bronze, "Pommeau");
                for (int i = 0; i < 5; i++)
                    Proto.Cylinder(t, new Vector3(0f, -0.16f + i * 0.022f, 0f), new Vector3(0.034f, 0.012f, 0.034f),
                                   i % 2 == 0 ? new Color(0.22f, 0.15f, 0.1f) : new Color(0.4f, 0.32f, 0.2f), "Fil");
                Proto.Cube(t, new Vector3(0f, -0.045f, 0f), new Vector3(0.15f, 0.022f, 0.032f), bronze, "Garde");
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject q = Proto.Cube(t, new Vector3(side * 0.085f, -0.035f, 0f), new Vector3(0.03f, 0.03f, 0.03f), bronze, "Quillon");
                    q.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
                Proto.Cube(t, new Vector3(0f, 0.22f, 0f), new Vector3(0.046f, 0.5f, 0.01f), steel, "Lame");
                Proto.Cube(t, new Vector3(0f, 0.2f, 0f), new Vector3(0.012f, 0.42f, 0.012f), new Color(0.36f, 0.37f, 0.4f), "Gouttière");
                GameObject tip = Proto.Cone(t, new Vector3(0f, 0.47f, 0f), 0.033f, 0.1f, steel, "Pointe", 4);
                tip.transform.localScale = new Vector3(0.033f, 0.1f, 0.008f);
            }
            Proto.EndVisualOnly();
            // L'outil en main ne projette pas d'ombre : collee a la camera, elle
            // tomberait en grand sur le sol devant soi.
            Renderer[] parts = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < parts.Length; i++) parts[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Le mouvement de l'outil : il suit le pas, respire au repos, et traine un
        // peu derriere le regard quand on tourne la tete.
        float bobPhase;
        Vector2 lag;
        Quaternion lastView = Quaternion.identity;

        void AnimateViewModel()
        {
            if (viewModel == null) return;
            float dt = Time.deltaTime;

            // Le pas : un balancement en huit, plus ample a la course.
            float speed = player != null ? player.CurrentSpeed : 0f;
            float walk = Mathf.Clamp01(speed / 6f);
            bobPhase += speed * dt * 1.6f;
            float bobX = Mathf.Sin(bobPhase) * 0.014f * walk;
            float bobY = -Mathf.Abs(Mathf.Cos(bobPhase)) * 0.016f * walk;
            // Le souffle, a l'arret.
            float breathe = Mathf.Sin(Time.time * 1.7f) * 0.004f * (1f - walk);

            // L'inertie : l'outil traine derriere le regard.
            Quaternion view = player.cameraTransform.rotation;
            Vector3 d = (Quaternion.Inverse(lastView) * view).eulerAngles;
            lastView = view;
            float yaw = Mathf.DeltaAngle(0f, d.y), pitch = Mathf.DeltaAngle(0f, d.x);
            lag = Vector2.Lerp(lag, new Vector2(Mathf.Clamp(-yaw * 0.9f, -7f, 7f), Mathf.Clamp(-pitch * 0.9f, -6f, 6f)), 1f - Mathf.Exp(-10f * dt));

            // Le coup : l'outil monte, puis plonge.
            float s = Mathf.Sin(swing * Mathf.PI);
            float windup = swing > 0.75f ? (swing - 0.75f) * 4f : 0f;
            viewModel.localPosition = new Vector3(0.32f - s * 0.12f + bobX + lag.x * 0.003f,
                                                  -0.3f - s * 0.05f + bobY + breathe + windup * 0.04f + lag.y * 0.003f,
                                                  0.55f + s * 0.1f);
            viewModel.localRotation = Quaternion.Euler(-20f + s * 75f - windup * 25f + lag.y, -15f + lag.x, 20f - s * 30f + bobX * 200f);
        }
    }

    /// <summary>
    /// Un arbre qui tombe : il bascule autour de son pied, de plus en plus vite,
    /// puis s'ecrase avec un bruit sourd. Couche, il devient un gisement de bois
    /// mort (beaucoup : 14 unites) qu'on ramasse comme un fagot.
    /// </summary>
    public class TreeFall : MonoBehaviour
    {
        Vector3 pivot;
        Vector3 axis;
        float angle;
        float speed;
        bool landed;

        public static void Fell(GameObject tree, Vector3 away)
        {
            Collider c = tree.GetComponent<Collider>();
            Vector3 foot = c != null ? new Vector3(c.bounds.center.x, tree.transform.position.y, c.bounds.center.z) : tree.transform.position;
            float radius = c != null ? Mathf.Min(c.bounds.extents.x, c.bounds.extents.z) : 0.3f;
            if (c != null) Destroy(c);
            Stump(foot, Mathf.Clamp(radius, 0.18f, 0.6f));
            TreeFall f = tree.AddComponent<TreeFall>();
            f.pivot = foot;
            f.axis = Vector3.Cross(Vector3.up, away).normalized;
            Sfx.Creak3D(foot + Vector3.up * 2f);
        }

        /// <summary>
        /// La souche : ce qui reste debout quand l'arbre est tombe. Ecorce autour,
        /// bois clair a cru sur le dessus, cernes, et elle garde un collider -- on ne
        /// traverse pas une souche.
        /// </summary>
        static void Stump(Vector3 foot, float radius)
        {
            GameObject go = new GameObject("Souche");
            go.transform.position = foot;
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.height = 1f;
            col.center = new Vector3(0f, 0.3f, 0f);
            Proto.BeginVisualOnly();
            GameObject trunk = Proto.Cylinder(go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(radius * 2.1f, 0.35f, radius * 2.1f), Palette.DarkBarks[0], "Écorce");
            trunk.GetComponent<Renderer>().sharedMaterial = Surfaces.Bark(Palette.DarkBarks[0]);
            GameObject top = Proto.Cylinder(go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(radius * 1.9f, 0.012f, radius * 1.9f), new Color(0.62f, 0.5f, 0.34f), "Bois à cru");
            top.transform.localRotation = Quaternion.Euler(4f, 0f, 3f);
            Proto.Cylinder(go.transform, new Vector3(0f, 0.565f, 0f), new Vector3(radius * 1.2f, 0.01f, radius * 1.2f), new Color(0.52f, 0.4f, 0.26f), "Cerne");
            Proto.Cylinder(go.transform, new Vector3(0f, 0.572f, 0f), new Vector3(radius * 0.5f, 0.01f, radius * 0.5f), new Color(0.44f, 0.32f, 0.2f), "Coeur");
            // Des echardes dressees, là où le tronc a cede.
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.7f;
                GameObject splinter = Proto.Cube(go.transform, new Vector3(Mathf.Cos(a) * radius * 0.6f, 0.68f, Mathf.Sin(a) * radius * 0.6f),
                                                 new Vector3(0.05f, 0.26f, 0.03f), new Color(0.58f, 0.46f, 0.3f), "Écharde");
                splinter.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 15f, a * 57f, Mathf.Cos(a) * 15f);
            }
            Proto.EndVisualOnly();
        }

        void Update()
        {
            if (landed) return;
            speed += Time.deltaTime * 55f;
            float step = Mathf.Min(speed * Time.deltaTime, 86f - angle);
            transform.RotateAround(pivot, axis, step);
            angle += step;
            if (angle < 86f) return;

            landed = true;
            Sfx.Harvest(ResourceType.Moonstone);
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.25f);

            // Couche, il devient un gisement : un declencheur le long du tronc.
            Vector3 along = Vector3.Cross(axis, Vector3.up).normalized;
            GameObject heap = new GameObject("Arbre abattu");
            heap.transform.position = pivot + along * 2.5f + Vector3.up * 0.6f;
            heap.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
            BoxCollider trigger = heap.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.6f, 1.4f, 5f);
            ResourceNode node = heap.AddComponent<ResourceNode>();
            node.yieldPerHarvest = 4;
            node.harvestDuration = 0.5f;
            node.respawnDelay = 0f;
            node.Initialise(ResourceType.Deadwood, 14, null);
        }
    }
}
