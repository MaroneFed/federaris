using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES MONUMENTS : la ou l'on POSE la Couronne pour gagner la manche (28/09 --
    /// Martin : "des endroits ou poser la couronne, ou on veut"). Il y en a TROIS par
    /// manche, chacun sur un ilot flottant different (tires de la graine de la manche) :
    /// le porteur CHOISIT ou aller -- le plus proche, ou celui que personne ne garde.
    /// Pas de boussole : on les trouve a leurs COLONNES BLEUES.
    ///
    /// Un grand arc de pierre, deux braseros bleus, un autel vide, et au sol un CERCLE
    /// qui luit : le porteur y entre, c'est gagne.
    /// </summary>
    public class Monument : MonoBehaviour, IInteractable
    {
        /// <summary>Les Monuments de la manche.</summary>
        public static readonly List<Monument> All = new List<Monument>();
        /// <summary>Combien de Monuments par manche (s'il y a assez d'ilots).</summary>
        public const int PerRound = 3;
        /// <summary>Celui ou la Couronne a ete posee (la camera de fin de manche le filme).</summary>
        public static Monument Winner { get; private set; }

        /// <summary>Le Monument a filmer : celui de la victoire, sinon le premier.</summary>
        public static Monument Focus { get { return Winner != null ? Winner : All.Count > 0 ? All[0] : null; } }

        static readonly Color Stone = new Color(0.3f, 0.31f, 0.34f);
        static readonly Color StoneDark = new Color(0.18f, 0.19f, 0.22f);
        public static readonly Color Blue = new Color(0.45f, 0.7f, 1f);

        LightBeam beam;
        Transform circle;
        Renderer circleGlow;
        readonly List<Transform> stones = new List<Transform>();

        /// <summary>L'ilot de ce Monument (voir Ground.GetIslet).</summary>
        public int Islet { get; private set; }

        /// <summary>Les ilots choisis pour cette manche.</summary>
        static readonly List<int> chosen = new List<int>();

        /// <summary>
        /// Choisir leurs places : TROIS ILOTS FLOTTANTS differents, tires de la graine de
        /// la manche. On y va en planant depuis la tour, ou tire par une arbaleste.
        /// </summary>
        public static void Choose(int seed)
        {
            System.Random rng = new System.Random(seed);
            chosen.Clear();
            Winner = null;
            int n = Mathf.Min(PerRound, Ground.IsletCount);
            int guard = 0;
            while (chosen.Count < n && guard++ < 200)
            {
                int i = rng.Next(Ground.IsletCount);
                if (!chosen.Contains(i)) chosen.Add(i);
            }
        }

        /// <summary>Vrai si l'ilot "i" porte un Monument cette manche.</summary>
        public static bool OnIslet(int i) { return chosen.Contains(i); }

        /// <summary>Le Monument le plus proche de "p" (a plat), null s'il n'y en a pas.</summary>
        public static Monument Nearest(Vector3 p)
        {
            Monument best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == null) continue;
                Vector3 d = All[i].transform.position - p;
                d.y = 0f;
                if (d.magnitude < bestD) { bestD = d.magnitude; best = All[i]; }
            }
            return best;
        }

        /// <summary>La distance a plat jusqu'au Monument le plus proche (tres grand s'il n'y en a pas).</summary>
        public static float NearestDistance(Vector3 p)
        {
            Monument m = Nearest(p);
            if (m == null) return float.MaxValue;
            Vector3 d = m.transform.position - p;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>Les batir tous, un par ilot choisi.</summary>
        public static void BuildAll(Transform parent, int seed)
        {
            if (chosen.Count == 0) Choose(seed);
            for (int k = 0; k < chosen.Count; k++) Build(parent, chosen[k], seed + k * 131);
        }

        /// <summary>Le batir sur son ilot.</summary>
        static Monument Build(Transform parent, int islet, int seed)
        {
            System.Random rng = new System.Random(seed ^ 0x77);
            Vector3 site = Ground.GetIslet(islet).Top;
            Vector3 at = Ground.Place(site.x, site.z, 0f);

            GameObject go = new GameObject("MONUMENT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            // L'arc fait face a la tour : on le voit de face en arrivant.
            Vector3 toTower = new Vector3(-at.x, 0f, -at.z);
            go.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(toTower.x, toTower.z) * Mathf.Rad2Deg + 90f + ((float)rng.NextDouble() - 0.5f) * 20f, 0f);
            Transform t = go.transform;
            Monument m = go.AddComponent<Monument>();
            m.Islet = islet;
            All.Add(m);

            // Une clairiere dallee, un arc de six metres, un autel.
            Proto.BeginVisualOnly();
            Proto.Cylinder(t, new Vector3(0f, 0.04f, 0f), new Vector3(9f, 0.06f, 9f), StoneDark, "Dallage");
            Proto.Cylinder(t, new Vector3(0f, 0.08f, 0f), new Vector3(6f, 0.06f, 6f), Stone, "Dallage");
            Proto.EndVisualOnly();
            for (int side = -1; side <= 1; side += 2)
            {
                Proto.Cube(t, new Vector3(side * 2.6f, 3f, 0f), new Vector3(1.1f, 6f, 1.1f), Stone, "Pilier");
                Proto.BeginVisualOnly();
                Proto.Cube(t, new Vector3(side * 2.6f, 0.3f, 0f), new Vector3(1.5f, 0.6f, 1.5f), StoneDark, "Base");
                Proto.Cube(t, new Vector3(side * 2.6f, 6.1f, 0f), new Vector3(1.4f, 0.4f, 1.4f), StoneDark, "Chapiteau");
                Proto.EndVisualOnly();
                // Un brasero bleu de chaque cote.
                Vector3 b = new Vector3(side * 4.6f, 0f, 1.5f);
                Proto.Cylinder(t, b + new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.5f), StoneDark, "Brasero");
                Proto.BeginVisualOnly();
                GameObject fire = Proto.Cube(t, b + new Vector3(0f, 1.15f, 0f), new Vector3(0.4f, 0.5f, 0.4f), Color.white, "Feu bleu");
                fire.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Blue, 3f);
                fire.AddComponent<Flame>();
                Proto.EndVisualOnly();
                GameObject lg = new GameObject("Lueur");
                lg.transform.SetParent(t, false);
                lg.transform.localPosition = b + new Vector3(0f, 1.6f, 0f);
                Light l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = Blue;
                l.intensity = 2f;
                l.range = 12f;
                l.shadows = LightShadows.None;
                lg.AddComponent<LampFlicker>();
            }
            Proto.Cube(t, new Vector3(0f, 6.6f, 0f), new Vector3(6.6f, 0.9f, 1.3f), Stone, "Linteau");
            Proto.BeginVisualOnly();
            GameObject rune = Proto.Cube(t, new Vector3(0f, 6.6f, -0.66f), new Vector3(1.2f, 0.5f, 0.04f), Color.white, "Rune");
            rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Blue, 2.2f);
            Proto.EndVisualOnly();
            // L'autel, avec la place vide de la couronne.
            Proto.Cube(t, new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 1.1f, 0.9f), StoneDark, "Autel");
            Proto.BeginVisualOnly();
            GameObject slot = Proto.Cylinder(t, new Vector3(0f, 1.12f, 0f), new Vector3(0.6f, 0.02f, 0.6f), Color.white, "Empreinte");
            slot.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Blue, 1.4f);
            Proto.EndVisualOnly();

            // LE CERCLE : on le voit de haut en planant -- c'est la qu'il faut entrer.
            Proto.BeginVisualOnly();
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, 0.12f, 0f), new Vector3(DeliverRadius * 2f, 0.02f, DeliverRadius * 2f), Color.white, "Cercle");
            m.circleGlow = ring.GetComponent<Renderer>();
            m.circleGlow.sharedMaterial = MaterialFactory.GetGlow(Blue, 1.2f);
            m.circle = ring.transform;
            Proto.Cylinder(t, new Vector3(0f, 0.13f, 0f), new Vector3(DeliverRadius * 2f - 0.5f, 0.02f, DeliverRadius * 2f - 0.5f), StoneDark, "Coeur du cercle");
            // Six pierres levees tout autour, avec une rune bleue chacune.
            for (int k = 0; k < 6; k++)
            {
                float a = (k + 0.5f) / 6f * Mathf.PI * 2f;
                Vector3 sp = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7.2f;
                GameObject stone = Proto.Cube(t, sp + Vector3.up * 1.4f, new Vector3(0.8f, 2.8f, 0.5f), Stone, "Pierre levée");
                stone.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, (float)(rng.NextDouble() - 0.5) * 8f);
                GameObject glyph = Proto.Cube(stone.transform, new Vector3(0f, 0.15f, -0.52f), new Vector3(0.45f, 0.25f, 0.05f), Color.white, "Rune");
                glyph.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Blue, 2.5f);
                m.stones.Add(stone.transform);
            }
            Proto.EndVisualOnly();

            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.2f, 0f);
            trigger.size = new Vector3(5f, 2.4f, 5f);

            // La colonne bleue : on la voit de partout.
            m.beam = LightBeam.Build(parent, at, Blue, 2.2f, 90f);
            if (m.beam != null) m.beam.targetAlpha = 0.55f;
            return m;
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (Winner == this) Winner = null;
            if (All.Count == 0) chosen.Clear();
        }

        /// <summary>Le porteur entre dans ce cercle : la manche est gagnee, sans touche a tenir.</summary>
        public const float DeliverRadius = 3.6f;

        /// <summary>
        /// Quand quelqu'un porte la Couronne, la colonne s'embrase : le Monument l'appelle.
        /// Et s'il entre dans le cercle, c'est gagne (27/09 : tenir E deux secondes, avec
        /// trois joueurs dans le dos, c'etait perdre la manche sur un bouton).
        /// </summary>
        void Update()
        {
            if (Game.Season == null || !Game.Season.Running) return;
            Seeker holder = Crown.Holder;
            if (holder != null && holder.Body != null && Within(holder.Body.position, DeliverRadius)
                && Mathf.Abs(holder.Body.position.y - transform.position.y) < 4f) TryDeliver(holder);
            bool called = Crown.Holder != null;
            // Le cercle bat quand quelqu'un porte la Couronne ; plus vite s'il approche.
            if (circle != null)
            {
                float near = holder != null && holder.Body != null ? Mathf.Clamp01(1f - Within01(holder.Body.position, 60f)) : 0f;
                float beat = called ? 1f + 0.06f * Mathf.Sin(Time.time * (4f + near * 8f)) : 1f;
                circle.localScale = new Vector3(DeliverRadius * 2f * beat, 0.02f, DeliverRadius * 2f * beat);
            }
            if (beam == null) return;
            beam.targetAlpha = called ? 0.85f + 0.15f * Mathf.Sin(Time.time * 3f) : 0.55f;
            beam.fadeSpeed = 1.5f;
        }

        /// <summary>La distance a plat, rapportee a "metres" (0 : dessus, 1 : a "metres" ou plus).</summary>
        float Within01(Vector3 p, float metres)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            return Mathf.Clamp01(d.magnitude / metres);
        }

        /// <summary>A portee du monument (pour les bots comme pour toi).</summary>
        public bool Within(Vector3 p, float metres)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            return d.magnitude < metres;
        }

        /// <summary>
        /// Deposer la Couronne. Vrai si c'etait bien le porteur. C'est ici que la manche
        /// se gagne -- en Phase 3, sur l'hote seulement.
        /// </summary>
        public bool TryDeliver(Seeker s)
        {
            if (s == null || !s.CarriesCrown || !Within(s.Body.position, 4.5f)) return false;
            if (Game.Season == null || !Game.Season.Running) return false;
            if (Match.IsTieBreak && !Match.TieBreakers.Contains(s.Index)) return false;
            Winner = this;
            Sfx.Bell();
            Sfx.Discovery();
            if (Crown.Instance != null) Crown.Instance.PlaceOn(transform.position + Vector3.up * 1.55f);
            Ambiance.Burst(null, transform.position + Vector3.up * 1.5f, Blue);
            Ambiance.Burst(null, transform.position + Vector3.up * 3f, new Color(1f, 0.8f, 0.35f));
            if (beam != null) { beam.targetAlpha = 1f; beam.fadeSpeed = 4f; }
            if (Game.Menus != null) Game.Menus.EndRound(s.Index);
            return true;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return false; } }       // plus de touche : on entre dans le cercle
        public string Prompt { get { return "Poser la Couronne"; } }
        public float HoldDuration { get { return 2f; } }

        public void Interact()
        {
            TryDeliver(Game.Me);
        }
    }
}
