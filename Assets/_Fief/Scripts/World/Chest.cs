using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN COFFRE dans la foret : E, et un objet passe dans tes mains (s'il te reste une
    /// place). Une quinzaine par manche, dans les clairieres et aux lieux-dits.
    ///
    /// UN TRESOR ENTERRE, lui, ne se voit pas : le DETECTEUR bipe de plus en plus vite
    /// quand on s'en approche, et la PELLE le sort de terre -- un objet rare.
    /// </summary>
    public class Chest : MonoBehaviour, IInteractable
    {
        public static readonly List<Chest> All = new List<Chest>();

        [System.NonSerialized] public bool buried;
        [System.NonSerialized] public Item content;
        bool open;
        Transform lid;
        Light glint;

        public bool Opened { get { return open; } }
        /// <summary>Encore sous terre (on ne le voit pas, on ne l'ouvre pas).</summary>
        public bool Hidden { get { return buried; } }

        static readonly Color Wood = new Color(0.38f, 0.26f, 0.15f);
        static readonly Color Iron = new Color(0.22f, 0.22f, 0.24f);

        public static Chest Build(Transform parent, Vector3 at, Item content, bool buried, float yaw)
        {
            GameObject go = new GameObject(buried ? "TRÉSOR ENTERRÉ" : "COFFRE");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Chest c = go.AddComponent<Chest>();
            c.content = content;
            c.buried = buried;
            c.Assemble();
            if (buried) c.Show(false);
            All.Add(c);
            return c;
        }

        void Assemble()
        {
            Transform t = transform;
            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.5f, 0f);
            trigger.size = new Vector3(1.4f, 1f, 1.2f);
            Proto.BeginVisualOnly();
            Proto.Cube(t, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.6f), Wood, "Caisse");
            for (int i = -1; i <= 1; i += 2)
                Proto.Cube(t, new Vector3(i * 0.36f, 0.26f, 0f), new Vector3(0.06f, 0.52f, 0.62f), Iron, "Ferrure");
            GameObject pivot = new GameObject("Couvercle");
            pivot.transform.SetParent(t, false);
            pivot.transform.localPosition = new Vector3(0f, 0.5f, 0.3f);
            lid = pivot.transform;
            Proto.Cube(lid, new Vector3(0f, 0.08f, -0.3f), new Vector3(0.92f, 0.16f, 0.62f), Palette.Shade(Wood, 1.15f), "Planche");
            GameObject lockPlate = Proto.Cube(t, new Vector3(0f, 0.42f, -0.31f), new Vector3(0.12f, 0.14f, 0.02f), new Color(0.85f, 0.68f, 0.3f), "Serrure");
            lockPlate.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(0.9f, 0.7f, 0.3f), 1.2f);
            if (buried) Proto.Cylinder(t, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.05f, 1.3f), new Color(0.22f, 0.16f, 0.1f), "Terre retournée");
            Proto.EndVisualOnly();

            GameObject lg = new GameObject("Reflet");
            lg.transform.SetParent(t, false);
            lg.transform.localPosition = new Vector3(0f, 0.9f, -0.5f);
            glint = lg.AddComponent<Light>();
            glint.type = LightType.Point;
            glint.color = new Color(1f, 0.8f, 0.45f);
            glint.intensity = 0.8f;
            glint.range = 3.5f;
            glint.shadows = LightShadows.None;
        }

        void Show(bool on)
        {
            Renderer[] parts = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < parts.Length; i++) parts[i].enabled = on;
            if (glint != null) glint.enabled = on && !open;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        void Update()
        {
            if (open && lid != null) lid.localRotation = Quaternion.Slerp(lid.localRotation, Quaternion.Euler(-100f, 0f, 0f), Time.deltaTime * 6f);
        }

        /// <summary>La pelle a touche un tresor enterre : il sort de terre.</summary>
        public void Unearth()
        {
            if (!buried) return;
            buried = false;
            Show(true);
            Sfx.Discovery();
            Ambiance.Burst(null, transform.position + Vector3.up * 0.5f, new Color(0.45f, 0.33f, 0.2f));
        }

        /// <summary>Ouvrir : l'objet passe dans les mains de "s" s'il a une place. Vrai si pris.</summary>
        public bool TryOpenFor(Seeker s)
        {
            if (open || buried || s == null || !s.Items.TryAdd(content)) return false;
            open = true;
            if (glint != null) glint.enabled = false;
            if (s.IsPlayer)
            {
                Sfx.Discovery();
                Stats.Chests++;
                Pickup.Fly(transform.position + Vector3.up * 0.7f, content);
                FloatingTexts.Spawn(transform.position + Vector3.up * 1.2f, ItemInfo.Name(content), ItemInfo.Tint(content));
            }
            return true;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return !open && !buried; } }
        public string Prompt { get { return Game.Me != null && Game.Me.Items.Full ? "Mains pleines" : "Coffre"; } }
        public float HoldDuration { get { return 0.6f; } }

        public void Interact()
        {
            if (!TryOpenFor(Game.Me)) Sfx.Deny();
        }

        // ================================================================== la manche

        /// <summary>
        /// Semer les coffres de la manche (graine du match) : une douzaine dans les
        /// clairieres, et une dizaine de tresors enterres. (Plus un par lieu-dit.)
        /// </summary>
        public static void Scatter(Transform parent, int seed)
        {
            System.Random rng = new System.Random(seed ^ 0x3c1);
            GameObject root = new GameObject("COFFRES");
            root.transform.SetParent(parent, false);
            GameConfig cfg = Game.Config;
            float half = (cfg != null ? cfg.mapSize : 420f) * 0.5f - 25f;

            // (Chaque lieu-dit a deja le sien : voir Landmarks.Chest.)
            int visible = 0, hidden = 0;
            for (int tries = 0; tries < 900 && (visible < 12 || hidden < 10); tries++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                if (Castle.Covers(x, z, 10f) || Landmarks.Near(x, z, 6f) || Monument.Near(x, z, 4f)) continue;
                Vector3 at = Ground.Place(x, z, 0f);
                if (Physics.CheckSphere(at + Vector3.up * 0.8f, 0.8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool far = true;
                for (int k = 0; k < All.Count && far; k++) if ((All[k].transform.position - at).magnitude < 30f) far = false;
                if (!far) continue;
                bool bury = visible >= 12 || hidden < 10 && rng.NextDouble() < 0.45;
                if (bury) { Build(root.transform, at, ItemInfo.Rare[rng.Next(ItemInfo.Rare.Length)], true, rng.Next(360)); hidden++; }
                else { Build(root.transform, at, ItemInfo.Common[rng.Next(ItemInfo.Common.Length)], false, rng.Next(360)); visible++; }
            }
        }

        /// <summary>Le tresor enterre le plus proche (pour le detecteur), et sa distance.</summary>
        public static Chest NearestBuried(Vector3 p, out float distance)
        {
            Chest best = null;
            distance = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                Chest c = All[i];
                if (c == null || !c.buried) continue;
                Vector3 d = c.transform.position - p;
                d.y = 0f;
                if (d.magnitude < distance) { distance = d.magnitude; best = c; }
            }
            return best;
        }
    }
}
