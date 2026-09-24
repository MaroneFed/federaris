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
            Seeker me = Game.Me;
            if (me == null || player == null) return;
            Kit kit = me.Kit;

            UpdateViewModel(kit.Held != null ? kit.Held.Kind : ToolKind.None);
            if (player.InputLocked) return;

            if (FiefInput.Slot1Pressed) kit.Select(0);
            if (FiefInput.Slot2Pressed) kit.Select(1);

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
                Hint = "Clic : poser le piege devant toi   (" + Trap.CountOf(me) + " / " + Trap.MaxFor(me) + " poses)";
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
                else Sfx.Step();
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
                Toasts.Show("Ta hache s'est brisee.", new Color(0.8f, 0.6f, 0.4f));
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
            Toasts.Show("Piege pose, sous les feuilles. De loin, toi seul sais qu'il est la.", new Color(0.95f, 0.62f, 0.35f));
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
                                                new Vector3(0.05f, 0.02f, 0.02f), Trap.Iron, "Machoire");
                    seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
                }
                Proto.Cube(go.transform, new Vector3(0f, -0.14f, 0f), new Vector3(0.015f, 0.12f, 0.015f), Trap.Iron, "Chaine");
            }
            else if (kind == ToolKind.Hache)
            {
                Proto.Cube(go.transform, new Vector3(0f, 0f, 0f), new Vector3(0.035f, 0.5f, 0.035f), wood, "Manche");
                Proto.Cube(go.transform, new Vector3(0.06f, 0.22f, 0f), new Vector3(0.12f, 0.1f, 0.02f), new Color(0.35f, 0.5f, 0.75f), "Lame");
                Proto.Cube(go.transform, new Vector3(-0.02f, 0.22f, 0f), new Vector3(0.05f, 0.06f, 0.04f), new Color(0.3f, 0.3f, 0.32f), "Tete");
            }
            else
            {
                Proto.Cube(go.transform, new Vector3(0f, -0.12f, 0f), new Vector3(0.035f, 0.14f, 0.035f), wood, "Poignee");
                Proto.Cube(go.transform, new Vector3(0f, -0.04f, 0f), new Vector3(0.16f, 0.025f, 0.03f), new Color(0.5f, 0.42f, 0.2f), "Garde");
                Proto.Cube(go.transform, new Vector3(0f, 0.26f, 0f), new Vector3(0.045f, 0.58f, 0.012f), steel, "Lame");
            }
            Proto.EndVisualOnly();
        }

        void AnimateViewModel()
        {
            if (viewModel == null) return;
            // Au repos, en bas a droite, legerement penche ; le coup la fait plonger.
            float s = Mathf.Sin(swing * Mathf.PI);
            viewModel.localPosition = new Vector3(0.32f - s * 0.12f, -0.3f - s * 0.05f, 0.55f + s * 0.1f);
            viewModel.localRotation = Quaternion.Euler(-20f + s * 75f, -15f, 20f - s * 30f);
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
            if (c != null) Destroy(c);
            TreeFall f = tree.AddComponent<TreeFall>();
            f.pivot = foot;
            f.axis = Vector3.Cross(Vector3.up, away).normalized;
            Sfx.Creak3D(foot + Vector3.up * 2f);
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
            node.yieldPerHarvest = 2;
            node.harvestDuration = Game.Config != null ? Game.Config.harvestDuration : 1.15f;
            node.respawnDelay = 0f;
            node.Initialise(ResourceType.Deadwood, 14, null);
        }
    }
}
