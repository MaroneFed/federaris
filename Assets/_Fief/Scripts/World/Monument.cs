using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE MONUMENT : la ou il faut porter la Couronne. Un seul, et il change de place a
    /// chaque manche -- quelque part dans la foret, loin du chateau. Pas de boussole :
    /// on le trouve a sa COLONNE BLEUE, qu'on voit au-dessus des arbres.
    ///
    /// Un grand arc de pierre, deux braseros bleus, et au milieu un autel vide qui
    /// attend. Le porteur de la Couronne maintient E deux secondes : manche gagnee.
    /// </summary>
    public class Monument : MonoBehaviour, IInteractable
    {
        public static Monument Instance { get; private set; }

        static readonly Color Stone = new Color(0.3f, 0.31f, 0.34f);
        static readonly Color StoneDark = new Color(0.18f, 0.19f, 0.22f);
        public static readonly Color Blue = new Color(0.45f, 0.7f, 1f);

        LightBeam beam;

        /// <summary>Sa place pour cette manche (choisie par Choose, avant la foret).</summary>
        public static Vector3 Site { get; private set; }
        static bool sited;

        /// <summary>
        /// Choisir sa place : 100-138 m du chateau, sur un sol plat. A appeler AVANT la
        /// foret (elle lui laisse une clairiere) et avant les lieux-dits (qui s'en
        /// ecartent) : chaque manche, il est ailleurs.
        /// </summary>
        public static void Choose(int seed)
        {
            System.Random rng = new System.Random(seed);
            sited = false;
            Site = new Vector3(0f, 0f, -118f);
            for (int tries = 0; tries < 400; tries++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                // Carte de 320 m (27/09) : entre la citadelle et la lisiere.
                float r = 100f + (float)rng.NextDouble() * 38f;
                float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                if (Mathf.Abs(x) > 138f || Mathf.Abs(z) > 138f) continue;
                if (Castle.Covers(x, z, 20f) || Ground.Slope(x, z) > 0.25f) continue;
                Site = new Vector3(x, 0f, z);
                break;
            }
            sited = true;
        }

        /// <summary>Vrai si (x, z) tombe dans la clairiere du Monument (plus une marge).</summary>
        public static bool Near(float x, float z, float margin)
        {
            if (!sited) return false;
            float dx = x - Site.x, dz = z - Site.z;
            float r = 10f + margin;
            return dx * dx + dz * dz < r * r;
        }

        /// <summary>Le batir a sa place (apres la foret).</summary>
        public static Monument Build(Transform parent, int seed)
        {
            System.Random rng = new System.Random(seed ^ 0x77);
            if (!sited) Choose(seed);
            Vector3 at = Ground.Place(Site.x, Site.z, 0f);

            GameObject go = new GameObject("LE MONUMENT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            Transform t = go.transform;
            Monument m = go.AddComponent<Monument>();
            Instance = m;

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
            if (Instance == this) { Instance = null; sited = false; }
        }

        /// <summary>Quand quelqu'un porte la Couronne, la colonne s'embrase : le Monument l'appelle.</summary>
        void Update()
        {
            if (beam == null || Game.Season == null || !Game.Season.Running) return;
            bool called = Crown.Holder != null;
            beam.targetAlpha = called ? 0.85f + 0.15f * Mathf.Sin(Time.time * 3f) : 0.55f;
            beam.fadeSpeed = 1.5f;
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
        public bool CanInteract { get { return Game.Me != null && Game.Me.CarriesCrown; } }
        public string Prompt { get { return "Poser la Couronne"; } }
        public float HoldDuration { get { return 2f; } }

        public void Interact()
        {
            TryDeliver(Game.Me);
        }
    }
}
