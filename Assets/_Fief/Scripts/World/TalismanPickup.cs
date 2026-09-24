using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN TALISMAN dans le monde : il flotte au-dessus de sa place, tourne lentement,
    /// luit, et laisse tomber des paillettes. On le voit avant de comprendre ce que
    /// c'est -- c'est voulu, c'est ce qui attire.
    ///
    /// On le prend en maintenant E (0,8 s). Ce composant ne decide pas s'il peut
    /// etre pris : il le demande a Hoard.TryTakeTalisman. Puis il applique l'effet
    /// qui touche le monde (la lanterne, le sac) et s'eteint.
    /// </summary>
    public class TalismanPickup : MonoBehaviour, IInteractable
    {
        public Talisman talisman;

        Transform model;
        Light halo;
        float baseY;
        bool taken;

        public static TalismanPickup Build(Transform parent, Vector3 localPosition, Talisman t)
        {
            GameObject root = new GameObject("TALISMAN " + TalismanInfo.Name(t));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            // Un declencheur : trouve par PlayerInteractor, traversable.
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.2f, 1.4f, 1.2f);

            TalismanPickup pickup = root.AddComponent<TalismanPickup>();
            pickup.talisman = t;

            GameObject modelGo = new GameObject("Objet");
            modelGo.transform.SetParent(root.transform, false);
            pickup.model = modelGo.transform;
            Proto.BeginVisualOnly();
            TalismanModels.Build(modelGo.transform, t);
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Halo");
            lightGo.transform.SetParent(root.transform, false);
            pickup.halo = lightGo.AddComponent<Light>();
            pickup.halo.type = LightType.Point;
            pickup.halo.color = TalismanInfo.Tint(t);
            pickup.halo.range = 5.5f;
            pickup.halo.intensity = 1.4f;
            pickup.halo.shadows = LightShadows.None;

            Ambiance.Sparkles(root.transform, Vector3.zero, TalismanInfo.Tint(t));
            return pickup;
        }

        void Start()
        {
            if (model != null) baseY = model.localPosition.y;
        }

        void Update()
        {
            if (taken || model == null) return;
            float t = Time.time;
            model.localRotation = Quaternion.Euler(0f, t * 35f, Mathf.Sin(t * 0.9f) * 6f);
            Vector3 p = model.localPosition;
            p.y = baseY + Mathf.Sin(t * 1.4f) * 0.12f;
            model.localPosition = p;
            if (halo != null) halo.intensity = 1.25f + Mathf.Sin(t * 2.1f) * 0.25f;
        }

        // ------------------------------------------------------------------ IInteractable

        public Transform Anchor { get { return transform; } }

        public bool CanInteract { get { return !taken && Game.Hoard != null && !Game.Hoard.Has(talisman); } }

        public string Prompt { get { return "Prendre : " + TalismanInfo.Name(talisman); } }

        public float HoldDuration { get { return 0.8f; } }

        public void Interact()
        {
            if (taken || Game.Hoard == null) return;
            if (!Game.Hoard.TryTakeTalisman(talisman)) return;

            taken = true;
            TalismanEffects.Apply(talisman);
            Sfx.Discovery();
            if (Game.Hud != null)
                Game.Hud.ShowDiscovery("TALISMAN  " + Game.Hoard.TalismanCount + " / " + TalismanInfo.Count,
                                       TalismanInfo.Name(talisman), TalismanInfo.Effect(talisman),
                                       TalismanInfo.Lore(talisman), TalismanInfo.Tint(talisman));

            // L'objet disparait ; les paillettes s'eteignent d'elles-memes.
            if (model != null) model.gameObject.SetActive(false);
            if (halo != null) halo.enabled = false;
            ParticleSystem sparkles = GetComponentInChildren<ParticleSystem>();
            if (sparkles != null) sparkles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// Ce que chaque talisman change dans le MONDE (la lanterne, la taille du sac).
    /// Les effets purement "regle" (la cache de plus, le bonus de score) vivent dans
    /// Hoard ; ceux qui se lisent au moment d'agir (creuser, ramasser, la boussole)
    /// sont testes la ou l'on agit, avec Game.Hoard.Has(...).
    /// </summary>
    public static class TalismanEffects
    {
        public static void Apply(Talisman t)
        {
            switch (t)
            {
                case Talisman.Lanterne:
                    Light lamp = Atmosphere.Lamp;
                    if (lamp == null) break;
                    lamp.range = TalismanInfo.LanternRange;
                    lamp.color = new Color(1f, 0.84f, 0.62f);
                    float boosted = (Game.Config != null ? Game.Config.lampIntensity : 1f) * TalismanInfo.LanternBoost;
                    lamp.intensity = boosted;
                    LampFlicker flicker = lamp.GetComponent<LampFlicker>();
                    if (flicker != null) flicker.Rebase(boosted);
                    break;

                case Talisman.Besace:
                    if (Game.Inventory != null) Game.Inventory.MaxWeight += TalismanInfo.BesaceKilos;
                    break;
            }
        }
    }

    /// <summary>
    /// Les six objets, en cubes et en cones. Chacun doit se reconnaitre de loin a sa
    /// SILHOUETTE et a sa couleur, sans texture : une corne courbe, un joyau bleu,
    /// une besace, une pelle, une lanterne, une couronne.
    /// </summary>
    public static class TalismanModels
    {
        static readonly Color Gold = new Color(0.9f, 0.72f, 0.3f);
        static readonly Color Bone = new Color(0.86f, 0.83f, 0.74f);
        static readonly Color Leather = new Color(0.40f, 0.28f, 0.17f);
        static readonly Color Iron = new Color(0.16f, 0.16f, 0.18f);

        public static void Build(Transform t, Talisman kind)
        {
            switch (kind)
            {
                case Talisman.Lanterne: Lantern(t); break;
                case Talisman.Corne: Horn(t); break;
                case Talisman.Coeur: Heart(t); break;
                case Talisman.Besace: Satchel(t); break;
                case Talisman.Pelle: Spade(t); break;
                default: Crown(t); break;
            }
        }

        static void Glow(GameObject go, Color c, float intensity)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.GetGlow(c, intensity);
        }

        static void Lantern(Transform t)
        {
            Proto.Cube(t, new Vector3(0f, -0.22f, 0f), new Vector3(0.34f, 0.05f, 0.34f), Iron, "Socle");
            Proto.Cube(t, new Vector3(0f, 0.22f, 0f), new Vector3(0.34f, 0.05f, 0.34f), Iron, "Chapeau");
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 0.15f, z = (i < 2 ? -1f : 1f) * 0.15f;
                Proto.Cube(t, new Vector3(x, 0f, z), new Vector3(0.04f, 0.44f, 0.04f), Gold, "Montant");
            }
            GameObject flame = Proto.Cube(t, new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.22f, 0.14f), Color.white, "Flamme");
            Glow(flame, new Color(1f, 0.7f, 0.3f), 3f);
            flame.AddComponent<Flame>();
            Proto.Cone(t, new Vector3(0f, 0.245f, 0f), 0.16f, 0.16f, Iron, "Toit", 4);
            Proto.Cube(t, new Vector3(0f, 0.44f, 0f), new Vector3(0.14f, 0.03f, 0.03f), Gold, "Anse");
        }

        static void Horn(Transform t)
        {
            // Une courbe de six segments qui s'amincissent, le long d'un arc.
            for (int i = 0; i < 6; i++)
            {
                float u = i / 5f;
                float a = Mathf.Lerp(-40f, 120f, u) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.3f, 0f);
                float s = Mathf.Lerp(0.2f, 0.07f, u);
                GameObject seg = Proto.Cube(t, p, new Vector3(s, 0.16f, s), i == 0 ? Gold : Bone, "Corne");
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            Proto.Cube(t, new Vector3(Mathf.Cos(-0.3f) * 0.3f, Mathf.Sin(-0.3f) * 0.3f, 0f),
                       new Vector3(0.23f, 0.04f, 0.23f), Gold, "Bague");
        }

        static void Heart(Transform t)
        {
            // Deux pyramides pointe contre pointe : un joyau taille, bleu, qui luit.
            GameObject top = Proto.Cone(t, Vector3.zero, 0.26f, 0.34f, Color.white, "Joyau", 6);
            GameObject bottom = Proto.Cone(t, Vector3.zero, 0.26f, 0.42f, Color.white, "Joyau", 6);
            bottom.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            Color blue = new Color(0.55f, 0.75f, 1f);
            Glow(top, blue, 2.4f);
            Glow(bottom, Palette.Shade(blue, 0.8f), 2f);
        }

        static void Satchel(Transform t)
        {
            Proto.Cube(t, new Vector3(0f, -0.05f, 0f), new Vector3(0.5f, 0.38f, 0.2f), Leather, "Sac");
            GameObject flap = Proto.Cube(t, new Vector3(0f, 0.12f, 0.08f), new Vector3(0.52f, 0.2f, 0.06f), Palette.Shade(Leather, 0.8f), "Rabat");
            flap.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Proto.Cube(t, new Vector3(0f, 0.05f, 0.13f), new Vector3(0.08f, 0.08f, 0.02f), Gold, "Boucle");
            for (int side = -1; side <= 1; side += 2)
                Proto.Cube(t, new Vector3(side * 0.2f, 0.32f, 0f), new Vector3(0.04f, 0.4f, 0.04f), Palette.Shade(Leather, 0.7f), "Sangle");
            Proto.Cube(t, new Vector3(0f, 0.52f, 0f), new Vector3(0.44f, 0.04f, 0.04f), Palette.Shade(Leather, 0.7f), "Sangle");
        }

        static void Spade(Transform t)
        {
            Proto.Cube(t, new Vector3(0f, 0.18f, 0f), new Vector3(0.05f, 0.7f, 0.05f), Bone, "Manche");
            Proto.Cube(t, new Vector3(0f, 0.55f, 0f), new Vector3(0.2f, 0.05f, 0.05f), Bone, "Poignee");
            Proto.Cube(t, new Vector3(0f, -0.26f, 0f), new Vector3(0.26f, 0.3f, 0.03f), Bone, "Lame");
            Proto.Cone(t, new Vector3(0f, -0.41f, 0f), 0.13f, 0.12f, Bone, "Pointe", 4)
                 .transform.localRotation = Quaternion.Euler(180f, 45f, 0f);
            GameObject rune = Proto.Cube(t, new Vector3(0f, -0.24f, 0.02f), new Vector3(0.08f, 0.14f, 0.01f), Color.white, "Rune");
            Glow(rune, new Color(0.7f, 0.9f, 0.8f), 1.8f);
        }

        static void Crown(Transform t)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.24f, 0f, Mathf.Sin(a) * 0.24f);
                GameObject seg = Proto.Cube(t, p, new Vector3(0.1f, 0.12f, 0.2f), Gold, "Cercle");
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                Proto.Cone(t, p + new Vector3(0f, 0.06f, 0f), 0.05f, i % 2 == 0 ? 0.22f : 0.13f, Gold, "Fleuron", 4);
                if (i % 2 == 0)
                {
                    GameObject gem = Proto.Cube(t, p * 1.08f, new Vector3(0.05f, 0.05f, 0.05f), Color.white, "Gemme");
                    Glow(gem, new Color(0.9f, 0.25f, 0.2f), 2f);
                }
            }
        }
    }
}
