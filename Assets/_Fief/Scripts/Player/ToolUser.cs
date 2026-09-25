using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON FAIT AVEC SES MAINS (La Couronne, 26/09).
    ///
    ///   clic gauche   avec l'EPEE (rien d'autre en main) : frapper (voir Combat) ;
    ///                 avec un OBJET : s'en servir (lancer, poser, boire, creuser) ;
    ///   clic droit    POUSSER : l'autre part en arriere, et lache la Couronne ;
    ///   1 / 2 / 3     prendre en main l'objet de l'emplacement (ou revenir a l'epee) ;
    ///   molette       passer de l'epee aux objets ;
    ///   F             face a un arbre : GRIMPER (F encore pour redescendre).
    ///
    /// Qui porte la Couronne la tient a deux mains : ni epee, ni poussee, ni objet.
    ///
    /// Ce qu'on tient se voit en bas a droite de l'ecran -- l'objet seulement, pas de
    /// mains (la regle du corps invisible tient). Il bouge quand on s'en sert.
    /// </summary>
    public class ToolUser : MonoBehaviour
    {
        /// <summary>Ce que le HUD affiche pres du centre de l'ecran ("F|↑").</summary>
        public static string Hint;
        public static bool Aiming;
        /// <summary>Un ennemi a portee d'epee, dans l'axe : le reticule rougit.</summary>
        public static bool FoeInReach;
        /// <summary>0 : on vient de pousser ; 1 : la poussee est prete.</summary>
        public static float ShoveReady01 = 1f;
        /// <summary>Le detecteur vient de biper (pour faire clignoter son voyant a l'ecran).</summary>
        public static float LastBeep = -9f;
        /// <summary>Distance du tresor enterre le plus proche, quand on tient le detecteur (-1 sinon).</summary>
        public static float DetectorDistance = -1f;

        PlayerController player;
        Transform viewModel;
        int shownKind = -99;          // ce que montre la vue : -2 la Couronne, sinon l'objet (0 : l'epee)
        Renderer detectorLamp;
        float swingTimer;
        float swing;                 // 0 -> 1 : l'animation du geste en cours
        float shoveReadyAt;
        float beepTimer;
        Transform perch;             // la plate-forme dans l'arbre, si l'on y est

        public const float DetectorRange = 45f;
        const float DigReach = 2.6f;

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        void Update()
        {
            Hint = null;
            Aiming = false;
            FoeInReach = false;
            DetectorDistance = -1f;
            Seeker me = Game.Me;
            if (me == null || player == null) return;
            Loadout kit = me.Items;
            float shoveCooldown = me.Has(Power.Poigne) ? 1.5f : 3f;
            ShoveReady01 = Mathf.Clamp01(1f - (shoveReadyAt - Time.time) / shoveCooldown);

            UpdateViewModel(me.CarriesCrown, kit.Held);
            swing = Mathf.Max(0f, swing - Time.deltaTime * 3.2f);
            swingTimer -= Time.deltaTime;
            if (player.InputLocked || !me.Alive) { AnimateViewModel(); return; }

            int was = kit.Active;
            if (FiefInput.Slot1Pressed) kit.Select(0);
            if (FiefInput.Slot2Pressed) kit.Select(1);
            if (FiefInput.Slot3Pressed) kit.Select(2);
            float wheel = FiefInput.ZoomNotches;
            if (Mathf.Abs(wheel) > 0.01f) kit.Cycle(wheel > 0f ? -1 : 1);
            if (kit.Active != was) Sfx.Pop();

            Transform eye = player.cameraTransform;
            if (eye == null) return;

            // Une escalade interrompue (tombe, pousse, releve) : on oublie le perchoir.
            if (climbing && !player.Scripted) climbing = false;
            if (!climbing && perch != null && (transform.position - perch.position).magnitude > 3f)
            {
                Destroy(perch.gameObject);
                perch = null;
            }
            if (climbing) { Climb(); return; }
            if (perch != null)
            {
                Hint = "F|↓";
                if (FiefInput.ClimbPressed) ClimbDown();
                AnimateViewModel();
                return;
            }

            // --- grimper
            RaycastHit hit;
            bool tree = Physics.Raycast(eye.position, eye.forward, out hit, 3.2f, ~0, QueryTriggerInteraction.Ignore)
                        && Forest.IsTree(hit.collider);
            if (tree)
            {
                if (FiefInput.ClimbPressed && !me.CarriesCrown) { ClimbUp(hit.collider); return; }
                Hint = me.CarriesCrown ? null : Forest.IsGiant(hit.collider) ? "F|↑↑" : "F|↑";
            }

            // --- la poussee (clic droit) : tout le monde, sauf le porteur
            if (FiefInput.ShovePressed && !me.CarriesCrown && !me.Rooted)
            {
                if (Time.time >= shoveReadyAt)
                {
                    shoveReadyAt = Time.time + shoveCooldown;
                    swing = 1f;
                    if (!Combat.Shove(me, eye.forward)) Sfx.Whoosh();
                }
                else Sfx.Deny();
            }

            // --- le detecteur bipe tout seul, tant qu'on le tient
            if (kit.Held == Item.Detecteur && !me.CarriesCrown) Detect(me);

            if (me.CarriesCrown) { AnimateViewModel(); return; }

            // --- l'epee
            if (kit.HoldingSword)
            {
                FoeInReach = Combat.FoeAhead(eye);
                Aiming = true;
                if (FiefInput.UseHeld && swingTimer <= 0f && me.CanStrike)
                {
                    swingTimer = 0.5f;
                    swing = 1f;
                    Combat.PlayerStrike(eye);
                }
            }
            // --- un objet
            else if (FiefInput.UsePressed && swingTimer <= 0f)
            {
                Use(me, kit, eye);
            }
            AnimateViewModel();
        }

        // ================================================================== les objets

        /// <summary>Se servir de l'objet en main.</summary>
        void Use(Seeker me, Loadout kit, Transform eye)
        {
            Item held = kit.Held;
            swingTimer = 0.6f;
            swing = 1f;
            switch (held)
            {
                case Item.Pelle:
                    Dig(me, eye);
                    break;
                case Item.Detecteur:
                    beepTimer = 0f;                 // un bip tout de suite
                    break;
                case Item.Fumigene:
                case Item.Lenteur:
                    Thrown.Launch(me, held, eye.position + eye.forward * 0.6f, Thrown.Lob(eye.forward, 14f));
                    kit.ConsumeHeld();
                    break;
                case Item.Piege:
                    PlaceTrap(me, kit);
                    break;
                case Item.Elixir:
                    me.Heal(me.MaxHealth);
                    Sfx.Discovery();
                    if (Game.Hud != null) Game.Hud.Flash(ItemInfo.Tint(Item.Elixir));
                    kit.ConsumeHeld();
                    break;
                case Item.Plume:
                    me.FeatherUntil = Time.time + 30f;
                    Sfx.Whoosh();
                    if (Game.Hud != null) Game.Hud.Flash(ItemInfo.Tint(Item.Plume));
                    kit.ConsumeHeld();
                    break;
                case Item.CapeOmbre:
                    me.HiddenUntil = Time.time + 10f;
                    Sfx.Whoosh();
                    if (Game.Hud != null) Game.Hud.Flash(ItemInfo.Tint(Item.CapeOmbre));
                    kit.ConsumeHeld();
                    break;
                default:
                    // La cle ne sert qu'a la porte derobee du donjon (E devant elle).
                    Sfx.Deny();
                    break;
            }
        }

        /// <summary>
        /// LE DETECTEUR : il bipe de plus en plus vite (et de plus en plus aigu) a
        /// l'approche d'un tresor enterre. Au-dela de 45 m, il se tait.
        /// </summary>
        void Detect(Seeker me)
        {
            float d;
            Chest near = Chest.NearestBuried(transform.position, out d);
            if (near == null || d > DetectorRange) return;
            DetectorDistance = d;
            float k = Mathf.Clamp01(d / DetectorRange);
            beepTimer -= Time.deltaTime;
            if (beepTimer > 0f) return;
            beepTimer = Mathf.Lerp(0.08f, 1.2f, k * k);
            LastBeep = Time.time;
            Sfx.Beep(Mathf.Lerp(2f, 1f, k));
            if (detectorLamp != null) detectorLamp.sharedMaterial = MaterialFactory.GetGlow(new Color(0.5f, 1f, 0.55f), 3f);
            if (d < DigReach) Hint = "clic|⛏";
        }

        /// <summary>LA PELLE : on creuse devant soi. Sur un tresor enterre, il sort de terre.</summary>
        void Dig(Seeker me, Transform eye)
        {
            Vector3 f = new Vector3(eye.forward.x, 0f, eye.forward.z).normalized;
            Vector3 spot = transform.position + f * 1.2f;
            OrbitCamera.Crouch = Mathf.Max(OrbitCamera.Crouch, 1f);
            Sfx.Dig();
            Ambiance.Burst(null, Ground.Place(spot.x, spot.z, 0.1f), new Color(0.36f, 0.26f, 0.16f));
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.08f);
            for (int i = 0; i < Chest.All.Count; i++)
            {
                Chest c = Chest.All[i];
                if (c == null || !c.Hidden) continue;
                Vector3 d = c.transform.position - spot;
                d.y = 0f;
                if (d.magnitude > DigReach) continue;
                c.Unearth();
                return;
            }
        }

        void PlaceTrap(Seeker me, Loadout kit)
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
            kit.ConsumeHeld();
            Sfx.Build();
            OrbitCamera.Crouch = Mathf.Max(OrbitCamera.Crouch, 1f);
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
        int Pulls = 5;

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
            // Un GEANT : on monte tout en haut, au-dessus de la canopee (voir Forest).
            float giant = Forest.GiantPerch(trunk);
            CapsuleCollider cap = trunk as CapsuleCollider;
            float trunkRadius = cap != null ? cap.radius * trunk.transform.lossyScale.x : 0.3f;
            float perchHeight = giant > 0f ? giant : 4.2f;
            Vector3 spot = new Vector3(centre.x, ground + perchHeight, centre.z) + toMe * (trunkRadius + 0.6f);
            Pulls = giant > 0f ? Mathf.RoundToInt(perchHeight / 1.6f) : 5;

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
            Vector3 hug = centre + toMe * (trunkRadius + 0.45f);
            StartClimb(true, transform.position,
                       new Vector3(hug.x, ground + 0.05f, hug.z),
                       new Vector3(hug.x, spot.y - 0.2f, hug.z),
                       spot + Vector3.up * 0.05f, giant > 0f ? 1.6f + perchHeight * 0.12f : 1.6f);
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
            float height = perch.position.y - Ground.Sample(perch.position.x, perch.position.z);
            Pulls = height > 8f ? Mathf.RoundToInt(height / 2f) : 5;
            Vector3 trunkSide = perch.position - perch.forward * 0.15f;
            Vector3 landing = Ground.Place(perch.position.x + perch.forward.x * 1.2f, perch.position.z + perch.forward.z * 1.2f, 0.1f);
            float ground = Ground.Sample(trunkSide.x, trunkSide.z);
            StartClimb(false, transform.position,
                       new Vector3(trunkSide.x, transform.position.y - 0.1f, trunkSide.z),
                       new Vector3(trunkSide.x, ground + 0.1f, trunkSide.z),
                       landing, height > 8f ? 1.1f + height * 0.06f : 1.1f);
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
            Sfx.Rustle();
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
                    Sfx.Rustle();
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

        // ================================================================== ce qu'on tient

        void UpdateViewModel(bool crown, Item item)
        {
            int kind = crown ? -2 : (int)item;
            if (kind == shownKind) return;
            shownKind = kind;
            if (viewModel != null) Destroy(viewModel.gameObject);
            viewModel = null;
            detectorLamp = null;
            if (player.cameraTransform == null) return;

            GameObject go = new GameObject(crown ? "En main : la Couronne" : "En main : " + (item == Item.None ? "l'épée" : ItemInfo.Name(item)));
            go.transform.SetParent(player.cameraTransform, false);
            viewModel = go.transform;
            Transform t = go.transform;
            Proto.BeginVisualOnly();
            Color wood = new Color(0.36f, 0.26f, 0.16f);
            Color steel = new Color(0.62f, 0.64f, 0.68f);
            Color iron = new Color(0.3f, 0.31f, 0.33f);
            if (crown)
            {
                // Tenue a deux mains, devant soi, un peu bas : on la voit briller.
                GameObject hold = new GameObject("Couronne");
                hold.transform.SetParent(t, false);
                hold.transform.localPosition = new Vector3(-0.3f, 0.02f, 0.1f);
                hold.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                Crown.Model(hold.transform, 0.7f);
            }
            else switch (item)
            {
                case Item.Detecteur:
                    // La canne, le disque de recherche en bas, le boitier et son voyant.
                    Proto.Cylinder(t, new Vector3(0f, -0.02f, 0f), new Vector3(0.025f, 0.3f, 0.025f), steel, "Canne");
                    GameObject coil = Proto.Cylinder(t, new Vector3(0f, -0.32f, 0.04f), new Vector3(0.22f, 0.012f, 0.22f), iron, "Disque");
                    coil.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                    Proto.Cube(t, new Vector3(0f, 0.12f, -0.04f), new Vector3(0.08f, 0.1f, 0.06f), new Color(0.2f, 0.2f, 0.22f), "Boîtier");
                    GameObject lamp = Proto.Sphere(t, new Vector3(0f, 0.15f, -0.075f), Vector3.one * 0.035f, Color.white, "Voyant");
                    detectorLamp = lamp.GetComponent<Renderer>();
                    detectorLamp.sharedMaterial = MaterialFactory.GetGlow(new Color(0.2f, 0.4f, 0.22f), 0.6f);
                    Proto.Cylinder(t, new Vector3(0f, 0.3f, 0f), new Vector3(0.035f, 0.06f, 0.035f), new Color(0.15f, 0.12f, 0.1f), "Poignée");
                    break;
                case Item.Pelle:
                    Proto.Cylinder(t, new Vector3(0f, 0.05f, 0f), new Vector3(0.032f, 0.32f, 0.032f), wood, "Manche");
                    Proto.Cube(t, new Vector3(0f, 0.38f, 0f), new Vector3(0.14f, 0.03f, 0.03f), wood, "Poignée");
                    GameObject blade = Proto.Cube(t, new Vector3(0f, -0.34f, 0.01f), new Vector3(0.17f, 0.2f, 0.02f), steel, "Lame");
                    blade.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                    GameObject point = Proto.Cube(t, new Vector3(0f, -0.44f, 0.03f), new Vector3(0.09f, 0.09f, 0.018f), steel, "Pointe");
                    point.transform.localRotation = Quaternion.Euler(12f, 0f, 45f);
                    break;
                case Item.Fumigene:
                    Proto.Sphere(t, Vector3.zero, Vector3.one * 0.13f, new Color(0.2f, 0.2f, 0.22f), "Boule");
                    Proto.Cylinder(t, new Vector3(0f, 0.08f, 0f), new Vector3(0.03f, 0.02f, 0.03f), iron, "Bouchon");
                    GameObject fuse = Proto.Cube(t, new Vector3(0.01f, 0.12f, 0f), new Vector3(0.008f, 0.05f, 0.008f), Color.white, "Mèche");
                    fuse.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.55f, 0.2f), 2.5f);
                    break;
                case Item.Lenteur:
                case Item.Elixir:
                    Color tint = ItemInfo.Tint(item);
                    GameObject belly = Proto.Sphere(t, Vector3.zero, Vector3.one * 0.12f, tint, "Panse");
                    belly.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(tint, 1.6f);
                    Proto.Cylinder(t, new Vector3(0f, 0.08f, 0f), new Vector3(0.04f, 0.04f, 0.04f), new Color(0.75f, 0.8f, 0.85f), "Col");
                    Proto.Cylinder(t, new Vector3(0f, 0.13f, 0f), new Vector3(0.045f, 0.015f, 0.045f), wood, "Bouchon");
                    break;
                case Item.Piege:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f;
                        GameObject seg = Proto.Cube(t, new Vector3(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.09f, 0f),
                                                    new Vector3(0.05f, 0.02f, 0.02f), Trap.Iron, "Mâchoire");
                        seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
                    }
                    Proto.Cube(t, new Vector3(0f, -0.14f, 0f), new Vector3(0.015f, 0.12f, 0.015f), Trap.Iron, "Chaîne");
                    break;
                case Item.Plume:
                    GameObject rachis = Proto.Cube(t, new Vector3(0f, 0.05f, 0f), new Vector3(0.008f, 0.36f, 0.008f), Color.white, "Rachis");
                    rachis.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(ItemInfo.Tint(Item.Plume), 1.2f);
                    for (int i = 0; i < 7; i++)
                    {
                        float y = -0.06f + i * 0.035f;
                        float w = 0.05f + Mathf.Sin(i / 6f * Mathf.PI) * 0.03f;
                        GameObject vane = Proto.Cube(t, new Vector3(0f, y, 0f), new Vector3(w * 2f, 0.03f, 0.004f), ItemInfo.Tint(Item.Plume), "Barbe");
                        vane.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    }
                    break;
                case Item.CapeOmbre:
                    GameObject cloth = Proto.Cube(t, Vector3.zero, new Vector3(0.2f, 0.1f, 0.16f), ItemInfo.Tint(Item.CapeOmbre), "Cape pliée");
                    cloth.transform.localRotation = Quaternion.Euler(0f, 20f, 8f);
                    Proto.Cube(t, new Vector3(0f, 0.055f, 0f), new Vector3(0.18f, 0.01f, 0.14f), Palette.Shade(ItemInfo.Tint(Item.CapeOmbre), 1.4f), "Pli");
                    break;
                case Item.Cle:
                    Color gold = ItemInfo.Tint(Item.Cle);
                    Proto.Cylinder(t, new Vector3(0f, 0.1f, 0f), new Vector3(0.09f, 0.01f, 0.09f), gold, "Anneau").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Proto.Cube(t, new Vector3(0f, -0.04f, 0f), new Vector3(0.018f, 0.2f, 0.018f), gold, "Tige");
                    Proto.Cube(t, new Vector3(0.025f, -0.12f, 0f), new Vector3(0.04f, 0.018f, 0.012f), gold, "Dent");
                    Proto.Cube(t, new Vector3(0.025f, -0.09f, 0f), new Vector3(0.03f, 0.018f, 0.012f), gold, "Dent");
                    break;
                default:
                    // L'EPEE : pommeau rond, poignee filetee, garde aux quillons evases,
                    // lame a gouttiere sombre qui s'effile en pointe.
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
                    break;
            }
            Proto.EndVisualOnly();
            // Ce qu'on tient ne projette pas d'ombre : collee a la camera, elle
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
            // Le voyant du detecteur s'eteint entre deux bips.
            if (detectorLamp != null && Time.time - LastBeep > 0.09f)
                detectorLamp.sharedMaterial = MaterialFactory.GetGlow(new Color(0.2f, 0.4f, 0.22f), 0.6f);
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
}
