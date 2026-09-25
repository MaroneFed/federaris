using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN SANCTUAIRE dans la foret (27/09 -- ils remplacent les coffres et les objets) :
    /// un cercle de pierres levees, et au milieu un cristal qui flotte, a la couleur du
    /// DON qu'il renferme. E maintenu une seconde : ce don devient ta capacite de la
    /// manche, sur la touche V. Un seul don a la fois, un seul passage par sanctuaire.
    ///
    /// C'est la raison d'aller dans la foret plutot que de foncer a la tour : une
    /// capacite de plus, c'est un grappin pour sauter un tour de rampe, une onde pour
    /// faire tomber le porteur...
    /// </summary>
    public class Shrine : MonoBehaviour, IInteractable
    {
        public static readonly List<Shrine> All = new List<Shrine>();

        Ability gift;
        bool spent;
        Transform crystal;
        Light glow;

        public bool Spent { get { return spent; } }
        public Ability Gift { get { return gift; } }

        static readonly Color Stone = new Color(0.3f, 0.31f, 0.33f);

        public static Shrine Build(Transform parent, Vector3 at, Ability gift, bool compact = false)
        {
            GameObject go = new GameObject("SANCTUAIRE");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Shrine s = go.AddComponent<Shrine>();
            s.gift = gift;
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.2f, 0f);
            trigger.size = new Vector3(3f, 2.4f, 3f);

            Color tint = AbilityInfo.Tint(gift);
            Transform t = go.transform;
            // (Compact, dans un lieu-dit : pas de cercle de pierres, la place manque.)
            for (int i = 0; i < (compact ? 0 : 6); i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.2f;
                GameObject stone = Proto.Cube(t, p + Vector3.up * 1.1f, new Vector3(0.7f, 2.2f + (i % 2) * 0.5f, 0.5f), Stone, "Pierre levée");
                stone.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, (i % 3 - 1) * 4f);
                Proto.BeginVisualOnly();
                GameObject rune = Proto.Cube(t, p * 0.93f + Vector3.up * 1.6f, new Vector3(0.18f, 0.5f, 0.05f), Color.white, "Rune");
                rune.transform.localRotation = stone.transform.localRotation;
                rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(tint, 1.6f);
                Proto.EndVisualOnly();
            }
            Proto.BeginVisualOnly();
            if (!compact) Proto.Cylinder(t, new Vector3(0f, 0.05f, 0f), new Vector3(4.6f, 0.05f, 4.6f), new Color(0.2f, 0.2f, 0.22f), "Dalle");
            Proto.Cylinder(t, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.35f, 1.2f), Stone, "Autel");
            GameObject c = new GameObject("Cristal");
            c.transform.SetParent(t, false);
            c.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            s.crystal = c.transform;
            GameObject top = Proto.Cone(s.crystal, Vector3.zero, 0.3f, 0.6f, tint, "Pointe", 6);
            top.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(tint, 2.8f);
            GameObject bottom = Proto.Cone(s.crystal, Vector3.zero, 0.3f, 0.6f, tint, "Pointe", 6);
            bottom.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            bottom.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(tint, 2.8f);
            Proto.EndVisualOnly();

            GameObject lg = new GameObject("Lueur");
            lg.transform.SetParent(t, false);
            lg.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            s.glow = lg.AddComponent<Light>();
            s.glow.type = LightType.Point;
            s.glow.color = tint;
            s.glow.intensity = 2.2f;
            s.glow.range = 11f;
            s.glow.shadows = LightShadows.None;

            All.Add(s);
            return s;
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (crystal == null) return;
            crystal.localPosition = new Vector3(0f, 1.6f + Mathf.Sin(Time.time * 1.4f) * 0.12f, 0f);
            crystal.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
        }

        /// <summary>Prendre le don : "s" le recoit pour la manche. Vrai si pris.</summary>
        public bool TryTakeFor(Seeker s)
        {
            if (spent || s == null || s.Body == null || s.Stunned) return false;
            spent = true;
            s.Gift = gift;
            s.HasGift = true;
            if (crystal != null) crystal.gameObject.SetActive(false);
            if (glow != null) glow.intensity = 0.4f;
            Ambiance.Burst(null, transform.position + Vector3.up * 1.6f, AbilityInfo.Tint(gift));
            if (s.IsPlayer)
            {
                Sfx.Discovery();
                if (Game.Hud != null) Game.Hud.ShowDiscovery("DON — touche V", AbilityInfo.Name(gift), AbilityInfo.Line(gift), "", AbilityInfo.Tint(gift));
                Stats.Shrines++;
            }
            return true;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return !spent && Game.Me != null && !Game.Me.Stunned; } }
        public string Prompt { get { return "Prendre le don : " + AbilityInfo.Name(gift); } }
        public float HoldDuration { get { return 1f; } }
        public void Interact() { if (!TryTakeFor(Game.Me)) Sfx.Deny(); }

        // ================================================================== la manche

        /// <summary>Un don au hasard (une capacite active), tire de la graine de la manche.</summary>
        public static Ability RandomGift(System.Random rng)
        {
            return (Ability)rng.Next((int)Ability.DoubleSaut);
        }

        /// <summary>Quelques sanctuaires de plus dans les clairieres (chaque lieu-dit a deja le sien).</summary>
        public static void Scatter(Transform parent, int seed)
        {
            System.Random rng = new System.Random(seed ^ 0x51);
            GameObject root = new GameObject("SANCTUAIRES");
            root.transform.SetParent(parent, false);
            float half = (Game.Config != null ? Game.Config.mapSize : 320f) * 0.5f - 25f;
            int placed = 0;
            for (int tries = 0; tries < 600 && placed < 4; tries++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                if (Castle.Covers(x, z, 12f) || Landmarks.Near(x, z, 8f) || Monument.Near(x, z, 6f) || Ground.Slope(x, z) > 0.25f) continue;
                Vector3 at = Ground.Place(x, z, 0f);
                if (Physics.CheckSphere(at + Vector3.up * 1.2f, 2.6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool far = true;
                for (int k = 0; k < All.Count && far; k++) if ((All[k].transform.position - at).magnitude < 45f) far = false;
                if (!far) continue;
                Build(root.transform, at, RandomGift(rng));
                placed++;
            }
        }
    }
}
