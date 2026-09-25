using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN TRESOR : ce qu'on vient chercher (26/09 -- Martin : "tu vas dans le chateau,
    /// tu sais meme pas pourquoi"). Maintenant on sait : c'est la qu'est le butin.
    ///
    ///   Calice    ★5    au rez-de-chaussee du donjon et dans la cour
    ///   Coffret   ★12   aux etages du donjon
    ///   Couronne  ★40   tout en haut, sur la terrasse du donjon -- une seule
    ///   Coffre    ★8    enfoui a un lieu-dit de la foret
    ///
    /// E maintenu (plus long pour les gros), et le tresor passe dans ton butin
    /// PORTE : il pese, il ne compte pas encore. Il faut le ramener a ta stele.
    /// Prendre un tresor au chateau fait du bruit : les gardes proches accourent.
    ///
    /// Un tresor pris revient apres quelques minutes : la Saison ne s'assèche pas.
    /// Toi et les rivaux passez par la meme methode (TryTakeFor).
    /// </summary>
    public class Treasure : MonoBehaviour, IInteractable
    {
        public enum Kind { Calice, Coffret, Couronne, Coffre }

        public static readonly List<Treasure> All = new List<Treasure>();

        [System.NonSerialized] public Kind kind;
        Transform visual;
        Light glow;
        float respawnAt = -1f;
        Vector3 visualHome;

        public static int Stars(Kind k)
        {
            switch (k)
            {
                case Kind.Calice: return 5;
                case Kind.Coffret: return 12;
                case Kind.Couronne: return 40;
                default: return 8;
            }
        }

        public static string Name(Kind k)
        {
            switch (k)
            {
                case Kind.Calice: return "Calice";
                case Kind.Coffret: return "Coffret";
                case Kind.Couronne: return "La Couronne";
                default: return "Coffre enfoui";
            }
        }

        static float Respawn(Kind k)
        {
            switch (k)
            {
                case Kind.Calice: return 120f;
                case Kind.Coffret: return 180f;
                case Kind.Couronne: return 300f;
                default: return 240f;
            }
        }

        public int Value { get { return Stars(kind); } }
        public bool Available { get { return respawnAt < 0f; } }
        /// <summary>Dans l'enceinte du chateau (les gardes le protegent).</summary>
        public bool Guarded { get { return Castle.Covers(transform.position.x, transform.position.z, 0f); } }

        static readonly Color Gold = new Color(0.95f, 0.76f, 0.3f);
        static readonly Color GoldDark = new Color(0.7f, 0.5f, 0.18f);
        static readonly Color Wood = new Color(0.36f, 0.22f, 0.12f);
        static readonly Color Ruby = new Color(0.9f, 0.18f, 0.2f);
        static readonly Color Sapphire = new Color(0.3f, 0.45f, 1f);
        static readonly Color PlinthStone = new Color(0.26f, 0.25f, 0.24f);

        // ================================================================== construction

        /// <summary>Un tresor pose a "at" (le sol sous lui). plinth : sur un socle de pierre.</summary>
        public static Treasure Build(Transform parent, Vector3 at, Kind kind, bool plinth)
        {
            GameObject go = new GameObject("TRÉSOR " + Name(kind));
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Treasure t = go.AddComponent<Treasure>();
            t.kind = kind;

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            trigger.size = new Vector3(1.4f, 1.6f, 1.4f);

            float lift = 0f;
            if (plinth)
            {
                // Le socle est solide : on ne marche pas dessus, on s'en approche.
                Proto.Cube(go.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), PlinthStone, "Socle");
                Proto.BeginVisualOnly();
                Proto.Cube(go.transform, new Vector3(0f, 0.93f, 0f), new Vector3(1.05f, 0.08f, 1.05f), Palette.Shade(PlinthStone, 1.2f), "Table");
                Proto.Cube(go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(1.1f, 0.08f, 1.1f), Palette.Shade(PlinthStone, 0.8f), "Base");
                Proto.EndVisualOnly();
                lift = 0.97f;
            }

            GameObject v = new GameObject("Visuel");
            v.transform.SetParent(go.transform, false);
            v.transform.localPosition = new Vector3(0f, lift, 0f);
            t.visual = v.transform;
            t.visualHome = v.transform.localPosition;

            Proto.BeginVisualOnly();
            switch (kind)
            {
                case Kind.Calice: Chalice(v.transform); break;
                case Kind.Coffret: Casket(v.transform, 1f); break;
                case Kind.Couronne: Crown(v.transform); break;
                default: Chest(v.transform); break;
            }
            Proto.EndVisualOnly();

            // Une petite lueur doree : dans une salle noire, on voit briller l'or
            // avant de voir la table.
            GameObject lightGo = new GameObject("Éclat");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, lift + 0.6f, 0f);
            t.glow = lightGo.AddComponent<Light>();
            t.glow.type = LightType.Point;
            t.glow.color = new Color(1f, 0.8f, 0.45f);
            t.glow.range = kind == Kind.Couronne ? 7f : 3.2f;
            t.glow.intensity = kind == Kind.Couronne ? 2.2f : 1.1f;
            t.glow.shadows = LightShadows.None;

            All.Add(t);
            return t;
        }

        static Material Shine(Color c, float power) { return MaterialFactory.GetGlow(c, power); }

        static void Chalice(Transform t)
        {
            Proto.Cylinder(t, new Vector3(0f, 0.03f, 0f), new Vector3(0.26f, 0.03f, 0.26f), GoldDark, "Pied");
            Proto.Cylinder(t, new Vector3(0f, 0.18f, 0f), new Vector3(0.06f, 0.14f, 0.06f), Gold, "Tige");
            Proto.Sphere(t, new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.07f, 0.1f), GoldDark, "Noeud");
            GameObject cup = Proto.Cone(t, new Vector3(0f, 0.52f, 0f), 0.17f, 0.24f, Gold, "Coupe", 10);
            cup.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            GameObject rim = Proto.Cylinder(t, new Vector3(0f, 0.52f, 0f), new Vector3(0.36f, 0.02f, 0.36f), Gold, "Bord");
            rim.GetComponent<Renderer>().sharedMaterial = Shine(Gold, 1.3f);
            GameObject gem = Proto.Cube(t, new Vector3(0f, 0.42f, 0.13f), new Vector3(0.06f, 0.06f, 0.03f), Ruby, "Rubis");
            gem.GetComponent<Renderer>().sharedMaterial = Shine(Ruby, 1.8f);
        }

        static void Casket(Transform t, float s)
        {
            Proto.Cube(t, new Vector3(0f, 0.16f * s, 0f), new Vector3(0.6f, 0.3f, 0.4f) * s, Wood, "Coffret");
            GameObject lid = Proto.Cube(t, new Vector3(0f, 0.35f * s, 0f), new Vector3(0.62f, 0.1f, 0.42f) * s, Palette.Shade(Wood, 1.2f), "Couvercle");
            lid.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            for (int i = -1; i <= 1; i += 2)
                Proto.Cube(t, new Vector3(i * 0.2f * s, 0.2f * s, 0f), new Vector3(0.05f, 0.42f, 0.43f) * s, Gold, "Ferrure");
            GameObject lockPlate = Proto.Cube(t, new Vector3(0f, 0.25f * s, -0.21f * s), new Vector3(0.1f, 0.12f, 0.02f) * s, Gold, "Serrure");
            lockPlate.GetComponent<Renderer>().sharedMaterial = Shine(Gold, 1.2f);
            // Des pieces qui debordent, et un joyau.
            for (int i = 0; i < 5; i++)
            {
                GameObject coin = Proto.Cylinder(t, new Vector3(-0.2f + i * 0.1f, 0.42f * s, (i % 2) * 0.06f - 0.03f), new Vector3(0.09f, 0.01f, 0.09f) * s, Gold, "Pièce");
                coin.transform.localRotation = Quaternion.Euler(i * 11f, 0f, 20f - i * 9f);
                coin.GetComponent<Renderer>().sharedMaterial = Shine(Gold, 1.4f);
            }
            GameObject gem = Proto.Cube(t, new Vector3(0.05f, 0.45f * s, 0.05f), new Vector3(0.07f, 0.07f, 0.07f) * s, Sapphire, "Saphir");
            gem.transform.localRotation = Quaternion.Euler(45f, 30f, 45f);
            gem.GetComponent<Renderer>().sharedMaterial = Shine(Sapphire, 2f);
        }

        static void Chest(Transform t)
        {
            // Un coffre de bois a moitie enterre : la terre remuee autour.
            Proto.Cylinder(t, new Vector3(0f, 0.02f, 0f), new Vector3(1.3f, 0.04f, 1.1f), new Color(0.2f, 0.15f, 0.1f), "Terre remuée");
            GameObject box = new GameObject("Coffre");
            box.transform.SetParent(t, false);
            box.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            box.transform.localRotation = Quaternion.Euler(-8f, 20f, 4f);
            Casket(box.transform, 1.35f);
        }

        static void Crown(Transform t)
        {
            // Un coussin pourpre, et la couronne dessus.
            Proto.Cube(t, new Vector3(0f, 0.08f, 0f), new Vector3(0.6f, 0.14f, 0.6f), new Color(0.35f, 0.08f, 0.12f), "Coussin");
            GameObject ring = new GameObject("Couronne");
            ring.transform.SetParent(t, false);
            ring.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            Material gold = Shine(Gold, 1.5f);
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.2f, 0.06f, Mathf.Sin(a) * 0.2f);
                GameObject band = Proto.Cube(ring.transform, p, new Vector3(0.13f, 0.12f, 0.03f), Gold, "Bandeau");
                band.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                band.GetComponent<Renderer>().sharedMaterial = gold;
                if (i % 2 == 0)
                {
                    GameObject point = Proto.Cone(ring.transform, p + new Vector3(0f, 0.06f, 0f), 0.045f, 0.16f, Gold, "Fleuron", 4);
                    point.GetComponent<Renderer>().sharedMaterial = gold;
                    GameObject gem = Proto.Cube(ring.transform, p * 1.08f + new Vector3(0f, 0.05f, 0f), new Vector3(0.045f, 0.045f, 0.02f),
                                                i % 4 == 0 ? Ruby : Sapphire, "Joyau");
                    gem.transform.localRotation = band.transform.localRotation;
                    gem.GetComponent<Renderer>().sharedMaterial = Shine(i % 4 == 0 ? Ruby : Sapphire, 2.2f);
                }
            }
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        // ================================================================== vie

        void Update()
        {
            Season season = Game.Season;
            float now = season != null ? season.Elapsed : Time.time;
            if (respawnAt >= 0f && now >= respawnAt)
            {
                respawnAt = -1f;
                if (visual != null) visual.gameObject.SetActive(true);
                if (glow != null) glow.enabled = true;
            }
            if (visual != null && visual.gameObject.activeSelf)
            {
                // Il tourne doucement et respire : un tresor, ca se remarque.
                visual.Rotate(0f, 25f * Time.deltaTime, 0f, Space.Self);
                visual.localPosition = visualHome + Vector3.up * (0.03f + Mathf.Sin(Time.time * 1.6f + transform.position.x) * 0.03f);
                if (glow != null) glow.intensity = (kind == Kind.Couronne ? 2.2f : 1.1f) * (0.85f + 0.15f * Mathf.Sin(Time.time * 3f));
            }
        }

        /// <summary>
        /// Prendre le tresor : il passe dans le butin porte du chercheur, par
        /// Hoard.TryPickLoot. Vrai si pris. Au chateau, les gardes proches l'entendent.
        /// </summary>
        public bool TryTakeFor(Seeker s)
        {
            if (!Available || s == null || !s.Alive) return false;
            s.Hoard.TryPickLoot(Value);
            s.SyncWeight();
            Season season = Game.Season;
            respawnAt = (season != null ? season.Elapsed : Time.time) + Respawn(kind);
            if (visual != null) visual.gameObject.SetActive(false);
            if (glow != null) glow.enabled = false;
            if (Guarded) Guard.Alert(transform.position, kind == Kind.Couronne ? 40f : 18f, s);
            return true;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return Available && Game.Season != null && !Game.Season.Over; } }
        public string Prompt { get { return Name(kind) + "   ★" + Value; } }
        public float HoldDuration
        {
            get { return kind == Kind.Couronne ? 1.6f : kind == Kind.Coffret ? 0.9f : kind == Kind.Coffre ? 1.1f : 0.4f; }
        }

        public void Interact()
        {
            Seeker me = Game.Me;
            if (!TryTakeFor(me)) return;
            Sfx.Coin();
            Pickup.FlyLoot(transform.position + Vector3.up * 1.1f, Value);
            FloatingTexts.Spawn(transform.position + Vector3.up * 1.8f, "★" + Value, Palette.Gold);
            Stats.Treasures++;
            if (kind == Kind.Couronne && Game.Hud != null)
                Game.Hud.ShowDiscovery("LA COURONNE", "★40 dans ton sac", "Cours à ta stèle !", "", Palette.Gold);
        }
    }
}
