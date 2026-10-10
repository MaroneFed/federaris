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
    /// Un dallage, deux braseros bleus, six pierres levees (rien qui gene : v36), et au sol un CERCLE
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

        static readonly Color Stone = new Color(0.88f, 0.87f, 0.9f);   // (30/09) du marbre blanc
        static readonly Color StoneDark = new Color(0.6f, 0.62f, 0.72f);
        public static readonly Color Blue = new Color(0.45f, 0.7f, 1f);

        LightBeam beam;
        Transform circle;
        Renderer circleGlow;
        Transform fill;                 // le disque d'or qui s'etend pendant le sacre
        float sacre;                    // secondes passees dans le cercle, Couronne en main
        Seeker sacreBy;
        bool sacreReported;             // le "sacre arrache" n'est annonce qu'une fois
        float lastTick;
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
            banked.Clear();
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

            // Une clairiere dallee, et le cercle.
            Proto.BeginVisualOnly();
            Proto.Cylinder(t, new Vector3(0f, 0.04f, 0f), new Vector3(9f, 0.06f, 9f), StoneDark, "Dallage");
            Proto.Cylinder(t, new Vector3(0f, 0.08f, 0f), new Vector3(6f, 0.06f, 6f), Stone, "Dallage");
            Proto.EndVisualOnly();
            // (12/10, v36 -- Martin : "les autels sur le truc ou on doit deposer la couronne, il
            // faut les degager ; quand on essaye de pousser, c'est un bordel pas possible") :
            // plus d'arc, plus de piliers, plus d'autel au milieu du cercle -- rien a heurter.
            // Deux braseros bleus, sans collision, au bord du dallage.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 b = new Vector3(side * 8.4f, 0f, 0f);
                Proto.BeginVisualOnly();
                Proto.Cylinder(t, b + new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.5f), StoneDark, "Brasero");
                GameObject fire = Proto.Sphere(t, b + new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.56f, 0.42f), Color.white, "Feu bleu");
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
                l.renderMode = LightRenderMode.ForceVertex;   // (03/10) fluide : jamais une passe de plus pour elle
                lg.AddComponent<LampFlicker>();
            }

            // LE CERCLE : on le voit de haut en planant -- c'est la qu'il faut entrer.
            Proto.BeginVisualOnly();
            GameObject ring = Proto.Cylinder(t, new Vector3(0f, 0.12f, 0f), new Vector3(DeliverRadius * 2f, 0.02f, DeliverRadius * 2f), Color.white, "Cercle");
            m.circleGlow = ring.GetComponent<Renderer>();
            m.circleGlow.sharedMaterial = MaterialFactory.GetGlow(Blue, 1.2f);
            m.circle = ring.transform;
            Proto.Cylinder(t, new Vector3(0f, 0.13f, 0f), new Vector3(DeliverRadius * 2f - 0.5f, 0.02f, DeliverRadius * 2f - 0.5f), StoneDark, "Coeur du cercle");
            GameObject fillGo = Proto.Cylinder(t, new Vector3(0f, 0.17f, 0f), new Vector3(0.01f, 0.015f, 0.01f), Color.white, "Sacre");
            fillGo.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.8f, 0.35f), 1.6f);
            m.fill = fillGo.transform;
            // Six pierres levees tout autour, avec une rune bleue chacune.
            for (int k = 0; k < 6; k++)
            {
                float a = (k + 0.5f) / 6f * Mathf.PI * 2f;
                Vector3 sp = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7.2f;
                GameObject stone = Proto.Capsule(t, sp + Vector3.up * 1.4f, new Vector3(0.8f, 1.4f, 0.5f), Stone, "Pierre levée");
                stone.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, (float)(rng.NextDouble() - 0.5) * 8f);
                GameObject glyph = Proto.Cube(stone.transform, new Vector3(0f, 0.15f, -0.52f), new Vector3(0.45f, 0.25f, 0.05f), Color.white, "Rune");
                glyph.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Blue, 1.8f);
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
            MaterialFactory.Polish(go.transform, 0.45f);
            return m;
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (Winner == this) Winner = null;
            if (sacringAt == this) { sacringAt = null; Sacring = null; SacreProgress = 0f; }
            if (All.Count == 0) chosen.Clear();
        }

        /// <summary>Le cercle ou l'on se fait sacrer.</summary>
        public const float DeliverRadius = 3.6f;

        /// <summary>
        /// LE SACRE (30/09 -- "trop facile de gagner : tu voles et c'est gagne") : il faut
        /// RESTER trois secondes dans le cercle avec la Couronne. Un disque d'or s'etend,
        /// une cloche sonne chaque seconde, tout le monde le voit a l'ecran -- et a trois
        /// secondes pour venir le pousser. Sorti du cercle, le sacre retombe vite.
        /// (Toujours aucune touche a tenir : il suffit d'etre dedans.)
        /// </summary>
        public const float SacreSeconds = 3f;

        /// <summary>Qui est en train de se faire sacrer (null : personne), et ou il en est (0-1).</summary>
        public static Seeker Sacring { get; private set; }
        public static float SacreProgress { get; private set; }
        static Monument sacringAt;

        /// <summary>
        /// (v43.2 -- Martin : "plus on reste sur la plateforme, plus le temps pour y rester apres
        /// diminue, sinon c'est impossible de gagner") : LE SACRE S'ACCUMULE. Chaque seconde passee
        /// dans un cercle avec la Couronne est ACQUISE pour toute la manche, quel que soit le
        /// Monument : pousse dehors a 2 s, tu reviens et il ne te reste qu'une seconde. Avant, il
        /// fallait 3 s d'un coup, et avec cinq bots en embuscade, on n'y arrivait jamais.
        /// </summary>
        static readonly Dictionary<Seeker, float> banked = new Dictionary<Seeker, float>();
        /// <summary>Les secondes de sacre deja acquises par "s" cette manche.</summary>
        public static float BankedOf(Seeker s) { float b; return s != null && banked.TryGetValue(s, out b) ? b : 0f; }

        /// <summary>
        /// Quand quelqu'un porte la Couronne, la colonne s'embrase : le Monument l'appelle.
        /// Et s'il entre dans le cercle, c'est gagne (27/09 : tenir E deux secondes, avec
        /// trois joueurs dans le dos, c'etait perdre la manche sur un bouton).
        /// </summary>
        void Update()
        {
            if (Game.Season == null || !Game.Season.Running) return;
            Seeker holder = Crown.Holder;
            float dt = Time.deltaTime;
            bool inside = holder != null && holder.Body != null && Within(holder.Body.position, DeliverRadius)
                && Mathf.Abs(holder.Body.position.y - transform.position.y) < 4f
                // En manche de departage, seuls les ex aequo peuvent etre sacres : pour
                // les autres, pas de barre, pas de cloches (avant, la barre montait
                // jusqu'au bout... et rien ne se passait).
                && (!Match.IsTieBreak || Match.TieBreakers.Contains(holder.Index));
            if (inside)
            {
                if (sacreBy != holder) { sacreBy = holder; sacre = 0f; lastTick = 0f; sacreReported = false; Sfx.Alarm(); }
                // (v43.2) Il repart de ce qu'il a deja acquis.
                float had = BankedOf(holder);
                if (sacre < had) { sacre = had; lastTick = Mathf.Floor(had); }
                sacre += dt;
                banked[holder] = sacre;
                if (Mathf.Floor(sacre) > lastTick) { lastTick = Mathf.Floor(sacre); Sfx.SacreTick((int)lastTick, transform.position); Ambiance.Burst(null, transform.position + Vector3.up * 1.5f, new Color(1f, 0.8f, 0.35f)); }
                if (sacre >= SacreSeconds) TryDeliver(holder);
            }
            else
            {
                // (02/10, le clipper) Un sacre aux deux tiers qui s'arrete parce que la Couronne
                // a quitte son porteur : SACRE ARRACHE (une fois).
                if (sacreBy != null && !sacreReported && Crown.Holder != sacreBy && sacre >= SacreSeconds * (Crown.Holder != null ? 0.25f : 0.66f))
                {
                    sacreReported = true;
                    Highlights.SacreStopped(sacreBy, sacre / SacreSeconds, transform.position);
                }
                sacre = Mathf.MoveTowards(sacre, 0f, dt * 2f);
                if (sacre <= 0f) { sacreBy = null; lastTick = 0f; }
            }
            if (sacreBy != null && sacre > 0f) { Sacring = sacreBy; SacreProgress = Mathf.Clamp01(sacre / SacreSeconds); sacringAt = this; }
            else if (sacringAt == this) { Sacring = null; SacreProgress = 0f; sacringAt = null; }
            if (fill != null)
            {
                float f = Mathf.Clamp01(sacre / SacreSeconds) * (DeliverRadius * 2f - 0.6f);
                fill.localScale = new Vector3(Mathf.Max(0.01f, f), 0.015f, Mathf.Max(0.01f, f));
            }
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
            // (04/10, en ligne) Chez un invite, la barre monte pareil -- mais c'est l'hote qui sacre.
            if (NetGame.IsClient) return false;
            if (s == null || !s.CarriesCrown || !Within(s.Body.position, 4.5f)) return false;
            if (sacreBy != s || sacre < SacreSeconds) return false;
            if (Game.Season == null || !Game.Season.Running) return false;
            if (Match.IsTieBreak && !Match.TieBreakers.Contains(s.Index)) return false;
            Winner = this;
            Sfx.Bell();
            Sfx.Discovery();
            if (Crown.Instance != null) Crown.Instance.PlaceOn(transform.position + Vector3.up * 1.55f);
            Ambiance.Burst(null, transform.position + Vector3.up * 1.5f, Blue);
            Ambiance.Burst(null, transform.position + Vector3.up * 3f, new Color(1f, 0.8f, 0.35f));
            if (beam != null) { beam.targetAlpha = 1f; beam.fadeSpeed = 4f; }
            Sacring = null;
            SacreProgress = 0f;
            Highlights.Crowned(s);
            if (Game.Menus != null) Game.Menus.EndRound(s.Index);
            return true;
        }

        /// <summary>(04/10, en ligne) L'hote a sacre "s" : chez l'invite, le Monument le plus proche de lui est le bon.</summary>
        public static void MirrorWinner(Seeker s)
        {
            Sacring = null;
            SacreProgress = 0f;
            if (s == null || s.Body == null) return;
            Monument m = Nearest(s.Body.position);
            if (m == null) return;
            Winner = m;
            if (m.beam != null) { m.beam.targetAlpha = 1f; m.beam.fadeSpeed = 4f; }
            Sfx.Discovery();
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
