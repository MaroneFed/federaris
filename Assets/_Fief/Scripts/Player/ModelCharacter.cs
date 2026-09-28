using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fief
{
    /// <summary>
    /// LE PERSONNAGE DE MARTIN (01/10 -- "si tu veux, je vais chercher des designs 3D
    /// gratuits"). Si un modele 3D ANIME est range dans Assets/_Fief/Resources/Modeles sous
    /// le nom "Personnage" (Personnage.fbx), il remplace le haricot dessine par le code --
    /// pour toi comme pour les bots. Sans fichier, rien ne change.
    ///
    /// Ce qu'il faut (voir docs/MODELES.md) :
    ///   - un personnage CC0 (libre), par exemple l'"Ultimate Animated Character Pack" de
    ///     Quaternius, ou les "Animated Characters" de Kenney ;
    ///   - dans l'Inspector du fichier : onglet Rig, Animation Type = Generic ;
    ///   - ses animations gardent leur nom anglais : Idle, Walk, Run, Jump, Dance (ou
    ///     Victory), Punch. Le code les trouve tout seul.
    ///
    /// Concept Unity : les "Playables" jouent des animations SANS Animator Controller (le
    /// graphe qu'on dessine d'habitude dans l'editeur). On branche les clips dans un
    /// melangeur (AnimationMixerPlayable) et, a chaque image, on dit combien chacun pese :
    /// 100 % "Run" quand il court, un fondu vers "Idle" quand il s'arrete.
    /// </summary>
    public class ModelCharacter : MonoBehaviour
    {
        const int Idle = 0, Walk = 1, Run = 2, Air = 3, Dance = 4, Punch = 5, Slots = 6;

        static bool looked;
        static GameObject prefab;
        static readonly AnimationClip[] clips = new AnimationClip[Slots];

        /// <summary>Vrai si Martin a range un personnage dans Resources/Modeles.</summary>
        public static bool Available { get { Load(); return prefab != null && clips[Idle] != null; } }

        /// <summary>Vrai si ce personnage a sa propre danse (la danse du code ne fait alors que le porter).</summary>
        public bool HasDance { get { return clips[Dance] != null; } }

        public float Speed;
        public bool Grounded = true;
        public bool Dancing;

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] players = new AnimationClipPlayable[Slots];
        readonly float[] weights = new float[Slots];
        float punchTimer;

        static void Load()
        {
            if (looked) return;
            looked = true;
            prefab = Resources.Load<GameObject>("Modeles/Personnage");
            if (prefab == null) return;
            AnimationClip[] found = Resources.LoadAll<AnimationClip>("Modeles/Personnage");
            for (int i = 0; i < found.Length; i++)
            {
                AnimationClip c = found[i];
                if (c == null || c.legacy || c.name.StartsWith("__")) continue;
                string n = c.name.ToLowerInvariant();
                if (clips[Dance] == null && (n.Contains("dance") || n.Contains("danse") || n.Contains("victory") || n.Contains("cheer"))) clips[Dance] = c;
                else if (clips[Punch] == null && (n.Contains("punch") || n.Contains("attack") || n.Contains("push") || n.Contains("slash"))) clips[Punch] = c;
                else if (clips[Run] == null && n.Contains("run") && !n.Contains("carry")) clips[Run] = c;
                else if (clips[Walk] == null && n.Contains("walk") && !n.Contains("carry")) clips[Walk] = c;
                else if (clips[Air] == null && (n.Contains("jump") || n.Contains("fall") || n.Contains("air"))) clips[Air] = c;
                else if (clips[Idle] == null && n.Contains("idle")) clips[Idle] = c;
            }
            if (clips[Idle] == null)
            {
                Debug.LogWarning("[FIEF] Resources/Modeles/Personnage : pas d'animation \"Idle\" trouvee (Rig > Animation Type = Generic ?). On garde le haricot.");
                return;
            }
            Debug.Log("[FIEF] Personnage trouve dans Resources/Modeles (" + found.Length + " animations).");
        }

        /// <summary>
        /// Poser le modele sous "parent", a la taille "height" (metres), les pieds au sol,
        /// teinte a "colour" la ou ses matieres s'y pretent. Null s'il n'y a pas de modele.
        /// </summary>
        public static ModelCharacter Attach(Transform parent, Color colour, float height)
        {
            if (!Available) return null;
            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = "Modèle";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Collider[] cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) Object.Destroy(cols[i]);

            // A la bonne taille, les pieds au sol.
            Renderer[] parts = go.GetComponentsInChildren<Renderer>(true);
            if (parts.Length > 0)
            {
                Bounds b = parts[0].bounds;
                for (int i = 1; i < parts.Length; i++) b.Encapsulate(parts[i].bounds);
                if (b.size.y > 0.01f) go.transform.localScale *= height / b.size.y;
                b = parts[0].bounds;
                for (int i = 1; i < parts.Length; i++) b.Encapsulate(parts[i].bounds);
                go.transform.position += Vector3.up * (parent.position.y - b.min.y);
            }
            Tint(parts, colour);

            ModelCharacter m = go.AddComponent<ModelCharacter>();
            Animator animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            m.Play(animator);
            return m;
        }

        /// <summary>Teindre les matieres "principales" (Main, Body, Shirt...) a la couleur du joueur.</summary>
        static void Tint(Renderer[] parts, Color colour)
        {
            string[] keys = { "main", "body", "shirt", "cloth", "primary", "suit", "tunic", "armor", "armour", "jacket" };
            bool any = false;
            for (int i = 0; i < parts.Length; i++)
            {
                Material[] mats = parts[i].materials;
                for (int k = 0; k < mats.Length; k++)
                {
                    string n = mats[k].name.ToLowerInvariant();
                    for (int j = 0; j < keys.Length; j++)
                        if (n.Contains(keys[j])) { mats[k].color = Color.Lerp(mats[k].color, colour, 0.75f); any = true; break; }
                }
                parts[i].materials = mats;
            }
            if (any || parts.Length == 0) return;
            // Aucune matiere nommee : on teinte la premiere du plus gros morceau.
            Renderer biggest = parts[0];
            for (int i = 1; i < parts.Length; i++) if (parts[i].bounds.size.sqrMagnitude > biggest.bounds.size.sqrMagnitude) biggest = parts[i];
            Material first = biggest.material;
            first.color = Color.Lerp(first.color, colour, 0.6f);
        }

        void Play(Animator animator)
        {
            graph = PlayableGraph.Create("Personnage");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, Slots);
            for (int i = 0; i < Slots; i++)
            {
                AnimationClip c = clips[i] != null ? clips[i] : i == Run ? (clips[Walk] ?? clips[Idle]) : i == Walk ? (clips[Run] ?? clips[Idle]) : clips[Idle];
                players[i] = AnimationClipPlayable.Create(graph, c);
                graph.Connect(players[i], 0, mixer, i);
                mixer.SetInputWeight(i, i == Idle ? 1f : 0f);
            }
            weights[Idle] = 1f;
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        /// <summary>Un coup (pousser, lancer une capacite) : l'animation "Punch", une fois.</summary>
        public void Swing()
        {
            if (clips[Punch] == null) return;
            punchTimer = Mathf.Min(0.6f, clips[Punch].length);
            players[Punch].SetTime(0.0);
        }

        void Update()
        {
            if (!graph.IsValid()) return;
            float dt = Time.deltaTime;
            punchTimer -= dt;
            int want = Dancing ? Dance : punchTimer > 0f ? Punch : !Grounded ? Air : Speed > 5f ? Run : Speed > 0.6f ? Walk : Idle;
            float total = 0f;
            for (int i = 0; i < Slots; i++)
            {
                weights[i] = Mathf.MoveTowards(weights[i], i == want ? 1f : 0f, dt * 7f);
                total += weights[i];
            }
            for (int i = 0; i < Slots; i++)
            {
                mixer.SetInputWeight(i, total > 0f ? weights[i] / total : i == Idle ? 1f : 0f);
                // Les boucles (marcher, courir, danser...) recommencent : bien des fichiers
                // arrivent sans la case "Loop Time" cochee.
                AnimationClip c = players[i].GetAnimationClip();
                if (c != null && c.length > 0.01f && i != Punch && i != Air)
                {
                    double t = players[i].GetTime();
                    if (t > c.length) players[i].SetTime(t % c.length);
                }
            }
            // La course suit la vitesse (les pieds ne glissent pas trop).
            players[Run].SetSpeed(Mathf.Clamp(Speed / 8f, 0.6f, 1.5f));
            players[Walk].SetSpeed(Mathf.Clamp(Speed / 3f, 0.6f, 1.5f));
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
