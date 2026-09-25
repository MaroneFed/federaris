using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CONSTRUIRE (touche T). Martin, 26/09 : "faut pouvoir construire des pieges
    /// pour les joueurs, avec un menu facile d'edition, un truc sympa".
    ///
    /// T ouvre une barre de cinq cases au-dessus de l'inventaire :
    ///
    ///   1  Machoires   3 bois 1 fer   qui marche dessus tombe et lache tout
    ///   2  Barricade   5 bois         un mur de pieux : il faut le casser pour passer
    ///   3  Alarme      2 bois         un fil a clochettes : il sonne quand on passe
    ///   4  Epee        2 bois 3 fer   dans ta main
    ///   5  Hache       3 bois 1 fer   dans ta main
    ///
    /// Pour les trois premiers, un FANTOME apparait devant toi : vert si l'endroit
    /// convient, rouge sinon. La molette le fait tourner, le clic le pose, le clic
    /// droit (ou T, ou Echap) referme le menu. L'epee et la hache se fabriquent d'un
    /// clic. Le bois et le fer se prennent dans le sac : c'est a ca qu'ils servent.
    /// </summary>
    public class Builder : MonoBehaviour
    {
        public enum Kind { Machoires, Barricade, Alarme, Epee, Hache }
        public static readonly Kind[] Kinds = { Kind.Machoires, Kind.Barricade, Kind.Alarme, Kind.Epee, Kind.Hache };

        public static bool IsOpen { get; private set; }
        static int selected;
        static string why;

        PlayerController player;
        GameObject ghost;
        Kind ghostKind;
        Renderer[] ghostParts = new Renderer[0];
        float extraYaw;
        static Material ghostOk, ghostBad;

        // ------------------------------------------------------------------ regles

        /// <summary>Ce que coute chaque construction : bois mort, pierre-lune, fer ancien.</summary>
        public static int[] Cost(Kind k)
        {
            switch (k)
            {
                case Kind.Machoires: return new[] { 3, 0, 1 };
                case Kind.Barricade: return new[] { 5, 0, 0 };
                case Kind.Alarme: return new[] { 2, 0, 0 };
                case Kind.Epee: return new[] { 2, 0, 3 };
                default: return new[] { 3, 0, 1 };
            }
        }

        public static string Name(Kind k)
        {
            switch (k)
            {
                case Kind.Machoires: return "Piège";
                case Kind.Barricade: return "Barricade";
                case Kind.Alarme: return "Alarme";
                case Kind.Epee: return "Épée";
                default: return "Hache";
            }
        }

        public static Pictos.Kind Picto(Kind k)
        {
            switch (k)
            {
                case Kind.Machoires: return Pictos.Kind.Piege;
                case Kind.Barricade: return Pictos.Kind.Barricade;
                case Kind.Alarme: return Pictos.Kind.Alarme;
                case Kind.Epee: return Pictos.Kind.Epee;
                default: return Pictos.Kind.Hache;
            }
        }

        static bool Placed(Kind k) { return k == Kind.Machoires || k == Kind.Barricade || k == Kind.Alarme; }

        public static bool CanAfford(Kind k, Inventory bag)
        {
            if (bag == null) return false;
            int[] cost = Cost(k);
            for (int i = 0; i < cost.Length; i++) if (bag.Get((ResourceType)i) < cost[i]) return false;
            return true;
        }

        /// <summary>Payer : par Inventory.TryRemove, comme tout le reste.</summary>
        static bool TryPay(Kind k, Inventory bag)
        {
            if (!CanAfford(k, bag)) return false;
            int[] cost = Cost(k);
            for (int i = 0; i < cost.Length; i++) bag.TryRemove((ResourceType)i, cost[i]);
            return true;
        }

        public static void Close()
        {
            IsOpen = false;
        }

        // ------------------------------------------------------------------ boucle

        void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        void Update()
        {
            Seeker me = Game.Me;
            bool locked = player == null || player.InputLocked || me == null || !me.Alive;
            if (!locked && FiefInput.BuildPressed)
            {
                IsOpen = !IsOpen;
                Sfx.Pop();
            }
            if (IsOpen && !locked && FiefInput.AltPressed) { IsOpen = false; Sfx.Pop(); }
            if (!IsOpen || locked)
            {
                if (ghost != null) ghost.SetActive(false);
                return;
            }

            int was = selected;
            if (FiefInput.Slot1Pressed) selected = 0;
            if (FiefInput.Slot2Pressed) selected = 1;
            if (FiefInput.Slot3Pressed) selected = 2;
            if (FiefInput.Slot4Pressed) selected = 3;
            if (FiefInput.Slot5Pressed) selected = 4;
            if (selected != was) { Sfx.Pop(); extraYaw = 0f; }
            Kind kind = Kinds[selected];

            if (!Placed(kind))
            {
                if (ghost != null) ghost.SetActive(false);
                why = me.Kit.FreeSlot < 0 ? "Tes deux mains sont prises" : CanAfford(kind, me.Bag) ? null : "Pas assez";
                if (FiefInput.UsePressed) Craft(me, kind);
                return;
            }

            // --- le fantome, devant soi
            float wheel = FiefInput.ZoomNotches;
            if (Mathf.Abs(wheel) > 0.01f) extraYaw += wheel > 0f ? 15f : -15f;
            Transform eye = player.cameraTransform != null ? player.cameraTransform : transform;
            Vector3 forward = eye.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.01f ? forward.normalized : transform.forward;
            float ahead = kind == Kind.Barricade ? 3f : 2.2f;
            Vector3 at = Ground.Place(transform.position.x + forward.x * ahead, transform.position.z + forward.z * ahead, 0.02f);
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + extraYaw;

            EnsureGhost(kind);
            ghost.SetActive(true);
            ghost.transform.position = at;
            ghost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            why = !CanAfford(kind, me.Bag) ? "Pas assez" : WhyNot(me, kind, at, yaw);
            Material m = why == null ? ghostOk : ghostBad;
            for (int i = 0; i < ghostParts.Length; i++) if (ghostParts[i] != null) ghostParts[i].sharedMaterial = m;

            if (FiefInput.UsePressed)
            {
                if (why != null) { Sfx.Deny(); Toasts.Show(why + ".", new Color(0.95f, 0.5f, 0.4f)); return; }
                if (!TryPay(kind, me.Bag)) return;
                me.SyncWeight();
                if (kind == Kind.Machoires) Trap.Place(me, at, yaw);
                else if (kind == Kind.Barricade) Barricade.Place(me, at, yaw);
                else Alarm.Place(me, at, yaw);
                Stats.Built++;
                Sfx.Build();
            }
        }

        void Craft(Seeker me, Kind kind)
        {
            if (why != null) { Sfx.Deny(); Toasts.Show(why + ".", new Color(0.95f, 0.5f, 0.4f)); return; }
            int slot = me.Kit.FreeSlot;
            if (slot < 0 || !TryPay(kind, me.Bag)) return;
            me.Kit.Slots[slot] = new Tool(kind == Kind.Epee ? ToolKind.Epee : ToolKind.Hache);
            me.Kit.Active = slot;
            me.SyncWeight();
            Stats.Built++;
            Sfx.Build();
            IsOpen = false;
        }

        static string WhyNot(Seeker me, Kind kind, Vector3 at, float yaw)
        {
            if (kind == Kind.Machoires) return Trap.WhyNot(me, at);
            if (kind == Kind.Barricade) return Barricade.WhyNot(me, at, yaw);
            return Alarm.WhyNot(me, at);
        }

        // ------------------------------------------------------------------ le fantome

        void EnsureGhost(Kind kind)
        {
            if (ghost != null && ghostKind == kind) return;
            if (ghost != null) Destroy(ghost);
            if (ghostOk == null)
            {
                Shader s = Shader.Find("Sprites/Default");
                ghostOk = new Material(s);
                ghostOk.color = new Color(0.45f, 1f, 0.55f, 0.38f);
                ghostBad = new Material(s);
                ghostBad.color = new Color(1f, 0.35f, 0.3f, 0.38f);
            }
            ghostKind = kind;
            ghost = new GameObject("Fantôme");
            Proto.BeginVisualOnly();
            if (kind == Kind.Barricade) Barricade.Shape(ghost.transform);
            else if (kind == Kind.Alarme) Alarm.Shape(ghost.transform);
            else
            {
                Proto.Cylinder(ghost.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.7f, 0.02f, 0.7f), Color.white, "Piège");
                Proto.Cube(ghost.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.6f, 0.06f, 0.08f), Color.white, "Ressort");
            }
            Proto.EndVisualOnly();
            ghostParts = ghost.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < ghostParts.Length; i++)
            {
                ghostParts[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ghostParts[i].receiveShadows = false;
            }
        }

        void OnDisable()
        {
            if (ghost != null) ghost.SetActive(false);
        }

        // ------------------------------------------------------------------ l'ecran

        /// <summary>La barre du menu, au-dessus de l'inventaire. Appelee par le Hud.</summary>
        public static void Draw()
        {
            if (!IsOpen) return;
            Seeker me = Game.Me;
            if (me == null) return;
            float size = UiStyle.S(64), gap = UiStyle.S(8);
            float total = Kinds.Length * size + (Kinds.Length - 1) * gap;
            float x = (Screen.width - total) * 0.5f;
            float y = Screen.height - UiStyle.S(54) - UiStyle.S(18) - UiStyle.S(40) - size - UiStyle.S(24);

            Rect back = new Rect(x - UiStyle.S(14), y - UiStyle.S(30), total + UiStyle.S(28), size + UiStyle.S(58));
            UiStyle.Fill(back, new Color(0.03f, 0.025f, 0.02f, 0.7f));
            UiStyle.Tinted(new Rect(back.x, back.y + UiStyle.S(4), back.width, UiStyle.S(20)), UiStyle.Spaced("CONSTRUIRE"), UiStyle.CenteredSmall, Palette.Gold);

            for (int i = 0; i < Kinds.Length; i++)
            {
                Kind k = Kinds[i];
                Rect r = new Rect(x + i * (size + gap), y, size, size);
                bool on = i == selected;
                bool afford = CanAfford(k, me.Bag);
                UiStyle.Fill(r, on ? new Color(0.86f, 0.7f, 0.36f, 0.16f) : new Color(0.04f, 0.035f, 0.03f, 0.8f));
                Color edge = on ? Palette.Gold : new Color(0.55f, 0.44f, 0.26f, 0.45f);
                UiStyle.Fill(new Rect(r.x, r.y, r.width, 1f), edge);
                UiStyle.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), edge);
                UiStyle.Fill(new Rect(r.x, r.y, 1f, r.height), edge);
                UiStyle.Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), edge);
                UiStyle.Tinted(new Rect(r.x + UiStyle.S(5), r.y + UiStyle.S(2), UiStyle.S(20), UiStyle.S(16)), (i + 1).ToString(), UiStyle.Tiny,
                               on ? Palette.Gold : UiStyle.InkFaint);
                float inset = r.width * 0.2f;
                Pictos.Draw(new Rect(r.x + inset, r.y + inset * 0.7f, r.width - inset * 2f, r.height - inset * 2f), Picto(k), !afford);

                // Le prix, en petits pictogrammes sous la case.
                int[] cost = Cost(k);
                float cx = r.x + UiStyle.S(2);
                for (int c = 0; c < cost.Length; c++)
                {
                    if (cost[c] <= 0) continue;
                    ResourceType t = (ResourceType)c;
                    float d = UiStyle.S(16);
                    Pictos.Draw(new Rect(cx, r.yMax + UiStyle.S(3), d, d), Pictos.Of(t), false);
                    bool enough = me.Bag.Get(t) >= cost[c];
                    UiStyle.Tinted(new Rect(cx + d, r.yMax + UiStyle.S(2), UiStyle.S(18), UiStyle.S(18)), cost[c].ToString(), UiStyle.Tiny,
                                   enough ? UiStyle.Ink : new Color(0.95f, 0.45f, 0.35f));
                    cx += d + UiStyle.S(16);
                }
            }

            // Le nom de ce qu'on tient, et ce qui ne va pas (en rouge).
            Kind sel = Kinds[selected];
            string line = Name(sel) + (why != null ? "   ·   " + why : "");
            UiStyle.Tinted(new Rect(back.x, back.yMax - UiStyle.S(2), back.width, UiStyle.S(20)), line, UiStyle.CenteredSmall,
                           why != null ? new Color(0.95f, 0.5f, 0.4f) : UiStyle.Ink);
            UiStyle.Tinted(new Rect(back.x, back.yMax + UiStyle.S(16), back.width, UiStyle.S(18)),
                           Placed(sel) ? "clic : poser   ·   molette : tourner   ·   clic droit : fermer" : "clic : fabriquer   ·   clic droit : fermer",
                           UiStyle.CenteredSmall, UiStyle.InkFaint);
        }
    }
}
