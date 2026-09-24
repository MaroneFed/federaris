using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Fief
{
    /// <summary>
    /// DE VRAIS MODELES, quand tu en fournis. Les PNJ sont batis en cubes par le
    /// code (Walker) ; si tu deposes un modele 3D anime dans le bon dossier, il
    /// prend sa place, tout seul, au lancement.
    ///
    ///     Assets/_Fief/Resources/Modeles/Gardes     les six gardes
    ///     Assets/_Fief/Resources/Modeles/Mage       le mage
    ///     Assets/_Fief/Resources/Modeles/Ermite     l'Ermite
    ///     Assets/_Fief/Resources/Modeles/Veilleur   le Veilleur
    ///
    /// Plusieurs modeles dans un dossier : chaque PNJ en prend un different.
    /// Formats : .fbx (le mieux), .obj (sans animation). Les packs gratuits CC0
    /// de KayKit, Quaternius ou Kenney conviennent : leurs personnages ont leurs
    /// animations DANS le fichier. On les reconnait a leur nom :
    ///     "idle"                                   -> immobile
    ///     "walk"                                   -> marche
    ///     "run", "sprint"                          -> course
    ///     "attack", "slash", "chop", "punch", "hit"-> un coup
    ///
    /// Taille : le modele est remis a la bonne hauteur, quelle que soit son echelle
    /// d'origine. Rien ne marche ? Le PNJ garde son corps en cubes, jamais rien
    /// de casse -- et la Console dit pourquoi.
    ///
    /// Concept Unity : les PLAYABLES. Un petit graphe qui melange des animations :
    /// ici, "immobile", "marche" et "course" dosees selon la vitesse, plus un coup
    /// joue par-dessus quand il frappe. C'est ce que fait un Animator Controller,
    /// mais construit par le code -- donc rien a regler dans l'editeur.
    /// </summary>
    public class ModelSkin : MonoBehaviour
    {
        static readonly Dictionary<string, GameObject[]> Found = new Dictionary<string, GameObject[]>();

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        bool hasGraph;
        int idle = -1, walk = -1, run = -1, attack = -1;
        float attackTimer;
        float attackLength = 0.8f;
        AnimationClipPlayable attackPlayable;

        Animation legacy;
        string legacyIdle, legacyWalk, legacyRun, legacyAttack;

        Vector3 lastPosition;
        bool hasLast;
        float speed;
        float runSpeed = 6f;

        /// <summary>
        /// Essaie d'habiller ce corps avec un modele du dossier. Vrai si c'est fait :
        /// le corps en cubes est alors cache (il reste la, invisible).
        /// </summary>
        public static bool TryDress(Walker body, string folder, float height, int variant)
        {
            if (body == null) return false;
            GameObject[] models = Load(folder);
            if (models.Length == 0) return false;

            GameObject prefab = models[Mathf.Abs(variant) % models.Length];
            GameObject model = null;
            try
            {
                model = Instantiate(prefab, body.transform.parent, false);
                model.name = "Modèle " + prefab.name;
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                Fit(model, height);

                // Ses colliders eventuels genent le CharacterController : dehors.
                Collider[] cols = model.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);

                ModelSkin skin = model.AddComponent<ModelSkin>();
                skin.runSpeed = body.RunSpeed;
                skin.Animate(folder, prefab);
                body.Skin = skin;

                Renderer[] cubes = body.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < cubes.Length; i++) cubes[i].enabled = false;
                return true;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[FIEF] Modèle " + prefab.name + " ignore : " + error.Message);
                if (model != null) Destroy(model);
                return false;
            }
        }

        static GameObject[] Load(string folder)
        {
            GameObject[] list;
            if (Found.TryGetValue(folder, out list)) return list;
            list = Resources.LoadAll<GameObject>("Modeles/" + folder);
            Found[folder] = list;
            if (list.Length > 0) Debug.Log("[FIEF] Modeles/" + folder + " : " + list.Length + " modèle(s).");
            return list;
        }

        /// <summary>Met le modele a la hauteur voulue, les pieds au sol.</summary>
        static void Fit(GameObject model, float height)
        {
            Renderer[] rs = model.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y < 0.01f) return;
            float k = height / b.size.y;
            model.transform.localScale *= k;
            float feet = (b.min.y - model.transform.position.y) * k;
            model.transform.localPosition -= new Vector3(0f, feet, 0f);
        }

        // ================================================================== animation

        void Animate(string folder, GameObject prefab)
        {
            // Les clips : ceux du fichier du modele, et ceux d'un eventuel sous-dossier
            // "Animations" partage.
            List<AnimationClip> clips = new List<AnimationClip>();
            clips.AddRange(Resources.LoadAll<AnimationClip>("Modeles/" + folder));
            clips.AddRange(Resources.LoadAll<AnimationClip>("Modeles/Animations"));
            if (clips.Count == 0) return;

            AnimationClip cIdle = Pick(clips, "idle");
            AnimationClip cWalk = Pick(clips, "walk");
            AnimationClip cRun = Pick(clips, "run", "sprint");
            AnimationClip cAttack = Pick(clips, "attack", "slash", "chop", "punch", "hit");
            AnimationClip any = cIdle != null ? cIdle : cWalk != null ? cWalk : clips[0];
            if (cIdle == null) cIdle = any;

            if (any.legacy)
            {
                legacy = GetComponent<Animation>();
                if (legacy == null) legacy = gameObject.AddComponent<Animation>();
                legacyIdle = AddLegacy(cIdle);
                legacyWalk = AddLegacy(cWalk);
                legacyRun = AddLegacy(cRun);
                legacyAttack = AddLegacy(cAttack);
                if (legacyIdle != null) legacy.CrossFade(legacyIdle);
                return;
            }

            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;

            graph = PlayableGraph.Create("FIEF " + name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            List<AnimationClip> used = new List<AnimationClip>();
            idle = Slot(used, cIdle);
            walk = Slot(used, cWalk);
            run = Slot(used, cRun);
            attack = Slot(used, cAttack);

            mixer = AnimationMixerPlayable.Create(graph, used.Count);
            for (int i = 0; i < used.Count; i++)
            {
                AnimationClipPlayable p = AnimationClipPlayable.Create(graph, used[i]);
                graph.Connect(p, 0, mixer, i);
                mixer.SetInputWeight(i, i == idle ? 1f : 0f);
                if (i == attack)
                {
                    attackPlayable = p;
                    attackLength = Mathf.Max(0.2f, used[i].length);
                }
            }
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Corps", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();
            hasGraph = true;
        }

        static int Slot(List<AnimationClip> used, AnimationClip clip)
        {
            if (clip == null) return -1;
            int at = used.IndexOf(clip);
            if (at >= 0) return at;
            used.Add(clip);
            return used.Count - 1;
        }

        string AddLegacy(AnimationClip clip)
        {
            if (clip == null) return null;
            if (legacy.GetClip(clip.name) == null) legacy.AddClip(clip, clip.name);
            return clip.name;
        }

        static AnimationClip Pick(List<AnimationClip> clips, params string[] keys)
        {
            for (int k = 0; k < keys.Length; k++)
                for (int i = 0; i < clips.Count; i++)
                {
                    string n = clips[i].name.ToLowerInvariant();
                    if (n.Contains(keys[k]) && !n.StartsWith("__preview")) return clips[i];
                }
            return null;
        }

        /// <summary>Un coup : l'animation d'attaque, jouee une fois par-dessus.</summary>
        public void Attack()
        {
            if (legacy != null && legacyAttack != null)
            {
                legacy.CrossFade(legacyAttack, 0.1f);
                attackTimer = legacy[legacyAttack].length;
                return;
            }
            if (!hasGraph || attack < 0) return;
            attackPlayable.SetTime(0);
            attackTimer = attackLength;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.position;
            float measured = 0f;
            if (hasLast)
            {
                Vector3 d = p - lastPosition;
                d.y = 0f;
                measured = Mathf.Min(d.magnitude / dt, 12f);
            }
            lastPosition = p;
            hasLast = true;
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-8f * dt));
            if (attackTimer > 0f) attackTimer -= dt;

            if (legacy != null)
            {
                if (attackTimer > 0f) return;
                string want = speed > runSpeed * 0.7f && legacyRun != null ? legacyRun
                            : speed > 0.4f && legacyWalk != null ? legacyWalk : legacyIdle;
                if (want != null && !legacy.IsPlaying(want)) legacy.CrossFade(want, 0.25f);
                return;
            }
            if (!hasGraph) return;

            // Les poids : immobile -> marche -> course, selon la vitesse ; le coup
            // prend le dessus le temps de frapper.
            float wWalk = walk >= 0 ? Mathf.Clamp01(speed / 1.2f) : 0f;
            float wRun = run >= 0 ? Mathf.Clamp01((speed - 2.5f) / Mathf.Max(0.5f, runSpeed - 2.5f)) : 0f;
            float wAttack = attack >= 0 && attackTimer > 0f ? Mathf.Clamp01(attackTimer / 0.15f) : 0f;
            float[] w = new float[mixer.GetInputCount()];
            if (idle >= 0) w[idle] += 1f - wWalk;
            if (walk >= 0) w[walk] += wWalk * (1f - wRun);
            if (run >= 0) w[run] += wWalk * wRun;
            float total = 0f;
            for (int i = 0; i < w.Length; i++) { w[i] *= 1f - wAttack; total += w[i]; }
            if (attack >= 0) { w[attack] += wAttack; total += wAttack; }
            for (int i = 0; i < w.Length; i++) mixer.SetInputWeight(i, total > 0f ? w[i] / total : 0f);
        }

        void OnDestroy()
        {
            if (hasGraph && graph.IsValid()) graph.Destroy();
        }
    }
}
