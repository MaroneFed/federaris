using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES PLANEURS (27/09 -- Martin : "une fois que tu as la couronne, tu prends un
    /// planeur en haut et tu planes jusqu'a un endroit hors du chateau").
    ///
    /// Huit planeurs attendent au sommet de la tour, sur leurs chevalets. Qui arrive
    /// au sommet PREND DES AILES (Seeker.HasWings) et les garde jusqu'a son prochain
    /// atterrissage ailleurs. En l'air, avec des ailes :
    ///   - Espace maintenu : on PLANE (le porteur de la Couronne plane tout seul) ;
    ///   - on regarde vers le bas pour piquer (plus vite, on descend plus vite), vers
    ///     l'horizon pour aller loin.
    ///
    /// Les reglages du vol sont ici, pour toi comme pour les bots.
    /// </summary>
    public static class Wings
    {
        /// <summary>Vitesse de chute en planant : a l'horizontale, en pique.</summary>
        public const float SinkFlat = 2.6f;
        public const float SinkDive = 10f;
        /// <summary>Vitesse d'avancee en planant : a l'horizontale, en pique.</summary>
        public const float SpeedFlat = 14f;
        public const float SpeedDive = 22f;

        static readonly Color Cloth = new Color(0.93f, 0.88f, 0.76f);
        static readonly Color Wood = new Color(0.36f, 0.26f, 0.17f);
        public static readonly Color Glow = new Color(0.7f, 0.9f, 1f);

        /// <summary>
        /// A appeler a chaque image pour un joueur (toi ou un bot) : il prend des ailes
        /// au sommet, il les perd en se posant ailleurs.
        /// </summary>
        public static void Tick(Seeker s, bool grounded)
        {
            if (s == null || s.Body == null) return;
            Vector3 p = s.Body.position;
            if (Tower.Summit(p))
            {
                if (!s.HasWings) Grant(s);
                return;
            }
            if (grounded && s.HasWings) s.HasWings = false;
        }

        static void Grant(Seeker s)
        {
            s.HasWings = true;
            Fx.Sparks(s.Body.position + Vector3.up * 1.4f, Glow, 30, 4f);
            if (s.IsPlayer)
            {
                Sfx.Pop();
                if (Game.Hud != null) Game.Hud.Tip("ailes", "Tu as des AILES : saute du sommet et maintiens Espace pour planer. Regarde en bas pour piquer.");
            }
        }

        /// <summary>
        /// Le vol plane : la vitesse de chute et d'avancee selon qu'on regarde a
        /// l'horizon (0) ou en bas (1).
        /// </summary>
        public static void Glide(float dive, out float sink, out float speed)
        {
            dive = Mathf.Clamp01(dive);
            sink = Mathf.Lerp(SinkFlat, SinkDive, dive * dive);
            speed = Mathf.Lerp(SpeedFlat, SpeedDive, dive);
        }

        // ================================================================== les chevalets

        /// <summary>Un planeur sur son chevalet : deux ailes de toile tendues sur du bois, un liseré qui luit.</summary>
        public static void BuildRack(Transform parent, Vector3 at, float angle)
        {
            GameObject go = new GameObject("PLANEUR");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
            Transform t = go.transform;
            Proto.BeginVisualOnly();
            // Le chevalet : deux montants, une traverse.
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject leg = Proto.Cube(t, new Vector3(k * 0.6f, 0.55f, 0f), new Vector3(0.1f, 1.1f, 0.1f), Wood, "Montant");
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, k * 8f);
            }
            Proto.Cube(t, new Vector3(0f, 1.08f, 0f), new Vector3(1.4f, 0.08f, 0.08f), Wood, "Traverse");
            Proto.EndVisualOnly();
            // Le planeur pose dessus, ailes repliees en V.
            Transform glider = new GameObject("Ailes").transform;
            glider.SetParent(t, false);
            glider.localPosition = new Vector3(0f, 1.2f, 0f);
            glider.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Proto.BeginVisualOnly();
            Model(glider, 0.8f, 26f);
            Proto.EndVisualOnly();
        }

        /// <summary>
        /// Des ailes : deux voiles de toile en fleche, une armature de bois, un liseré
        /// qui luit. "span" : l'envergure (en metres, sur 2) ; "fold" : l'angle du V.
        /// </summary>
        public static void Model(Transform t, float span, float fold)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Transform wing = new GameObject("Aile").transform;
                wing.SetParent(t, false);
                wing.localRotation = Quaternion.Euler(0f, 0f, side * fold);
                GameObject sail = Proto.Cube(wing, new Vector3(side * span * 1.1f, 0f, -0.15f), new Vector3(span * 2.2f, 0.04f, 0.9f), Cloth, "Toile");
                sail.transform.localRotation = Quaternion.Euler(0f, side * 14f, 0f);
                GameObject spar = Proto.Cube(wing, new Vector3(side * span * 1.1f, 0.03f, 0.3f), new Vector3(span * 2.25f, 0.06f, 0.07f), Wood, "Longeron");
                spar.transform.localRotation = sail.transform.localRotation;
                GameObject edge = Proto.Cube(wing, new Vector3(side * span * 1.1f, 0.01f, -0.6f), new Vector3(span * 2f, 0.05f, 0.05f), Color.white, "Liseré");
                edge.transform.localRotation = sail.transform.localRotation;
                edge.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Glow, 1.8f);
            }
            Proto.Cube(t, new Vector3(0f, 0f, 0f), new Vector3(0.12f, 0.1f, 1.2f), Wood, "Quille");
        }
    }

    /// <summary>
    /// LES AILES DANS LE DOS d'un bot (ou d'un joueur en ligne) : repliees tant qu'il
    /// marche, grandes ouvertes quand il plane. On voit de loin qui peut voler.
    /// </summary>
    public class WingsOnBack : MonoBehaviour
    {
        public Seeker seeker;
        /// <summary>Mis a jour chaque image par son proprietaire (Rival) : plane-t-il ?</summary>
        public bool Flying;
        Transform wings;
        float open;

        public static WingsOnBack Attach(Transform body, Seeker s)
        {
            GameObject go = new GameObject("Ailes dans le dos");
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, 1.45f, -0.25f);
            WingsOnBack w = go.AddComponent<WingsOnBack>();
            w.seeker = s;
            w.wings = new GameObject("Ailes").transform;
            w.wings.SetParent(go.transform, false);
            Proto.BeginVisualOnly();
            Wings.Model(w.wings, 1f, 0f);
            Proto.EndVisualOnly();
            w.wings.gameObject.SetActive(false);
            return w;
        }

        void Update()
        {
            bool show = seeker != null && seeker.CanGlide && !seeker.Hidden;
            if (wings.gameObject.activeSelf != show) wings.gameObject.SetActive(show);
            if (!show) return;
            open = Mathf.MoveTowards(open, Flying ? 1f : 0f, Time.deltaTime * 4f);
            // Repliees : petites et relevees en V ; ouvertes : a plat, pleine envergure.
            wings.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.25f, open);
            wings.localRotation = Quaternion.Euler(Mathf.Lerp(-70f, -8f, open), 0f, 0f);
        }
    }
}
