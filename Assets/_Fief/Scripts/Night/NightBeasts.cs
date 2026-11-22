using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// (21/11) LES MODELES ANIMES : les monstres et les vivants viennent des packs gratuits (CC0) de Quaternius, convertis
    /// par Blender (Resources/Modeles/Monstres et Gens). Leurs animations sont en "Legacy" (Editor/ModelesAnimes.cs) :
    /// le composant Animation les joue par leur nom.
    ///
    /// Concept Unity : Animation.CrossFade("Walk") passe en douceur de l'animation en cours a "Walk". Les noms viennent de
    /// Blender ("CharacterArmature|Walk") : on les cherche par la fin du nom.
    /// </summary>
    public static class Puppet
    {
        /// <summary>Pose un modele anime sous "parent", a la bonne taille (hauteur en metres), les pieds au sol.</summary>
        public static GameObject Spawn(string path, Transform parent, float height)
        {
            GameObject src = Resources.Load<GameObject>(path);
            if (src == null) return null;
            GameObject go = Object.Instantiate(src, parent);
            go.name = src.name;
            go.transform.localPosition = Vector3.zero;
            // Le FBX de Blender arrive avec sa propre rotation (l'axe Z de Blender devient le Y d'Unity) : on la garde.
            go.transform.localRotation = src.transform.localRotation;
            // Blender, en important le glTF, ajoute une boule ("Icosphere") pour dessiner les os : elle n'a rien a faire ici.
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.name.StartsWith("Icosphere") || (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Icosphere"))) Object.DestroyImmediate(mf.gameObject);
            // La taille d'un modele qui a un squelette ne se lit pas dans renderer.bounds (ce sont des bornes de fichier,
            // parfois enormes) : on pose le modele dans sa premiere animation, on "cuit" sa forme (BakeMesh) et on la mesure.
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Bounds b;
            bool measured = Measure(go, out b) && b.size.y > 0.05f;
            // Plan B : si la forme ne se mesure pas ici, le modele se met a la bonne taille a sa premiere image animee.
            if (!measured) go.AddComponent<PuppetFit>().Height = height;
            if (measured)
            {
                float k = height / b.size.y;
                go.transform.localScale = go.transform.localScale * k;
                Bounds after;
                if (Measure(go, out after))
                {
                    float ground = parent != null ? parent.position.y : 0f;
                    Vector3 mid = parent != null ? parent.position : Vector3.zero;
                    go.transform.position += new Vector3(mid.x - after.center.x, ground - after.min.y, mid.z - after.center.z);
                }
            }
            Animation a = go.GetComponentInChildren<Animation>();
            if (Application.isBatchMode)
            {
                int n = go.GetComponentsInChildren<Renderer>(true).Length;
                Debug.Log("[FIEF] Modele " + path + " : " + n + " rendus, taille " + (Measure(go, out b) ? b.size : Vector3.zero) + ", echelle " + go.transform.localScale.x + ", pieds a " + go.transform.position + ", anim " + (a != null ? a.GetClipCount().ToString() : "aucune"));
            }
            if (a != null)
            {
                a.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (AnimationState st in a) st.wrapMode = WrapMode.Loop;
            }
            return go;
        }

        /// <summary>La forme reelle du modele, posee (os compris), en coordonnees du monde.</summary>
        static bool Measure(GameObject go, out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            Animation a = go.GetComponentInChildren<Animation>();
            if (a != null)
            {
                foreach (AnimationState st in a) { a.Play(st.name); break; }
                a.Sample();
            }
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                SkinnedMeshRenderer s = r as SkinnedMeshRenderer;
                if (s != null && s.sharedMesh != null)
                {
                    Mesh baked = new Mesh();
                    s.BakeMesh(baked, true);
                    Vector3[] vs = baked.vertices;
                    for (int i = 0; i < vs.Length; i++)
                    {
                        Vector3 w = s.transform.position + s.transform.rotation * vs[i];
                        if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
                    }
                    Object.Destroy(baked);
                }
                else if (r is MeshRenderer)
                {
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
            }
            return any;
        }

        /// <summary>Le vrai nom de l'animation qui finit par "suffix" (ex. "|Walk"), ou null.</summary>
        public static string Clip(Animation a, string suffix)
        {
            if (a == null) return null;
            foreach (AnimationState st in a) if (st.name.EndsWith(suffix)) return st.name;
            return null;
        }

        /// <summary>Joue l'animation (la premiere des fins proposees, separees par des ;, qui existe), en fondu. "once" : une seule fois.</summary>
        public static void Play(Animation a, float speed, bool once, string choices)
        {
            if (a == null) return;
            string[] suffixes = choices.Split(';');
            for (int i = 0; i < suffixes.Length; i++)
            {
                string n = Clip(a, suffixes[i]);
                if (n == null) continue;
                AnimationState st = a[n];
                st.speed = speed;
                st.wrapMode = once ? WrapMode.ClampForever : WrapMode.Loop;
                if (!a.IsPlaying(n) || once) a.CrossFade(n, 0.2f);
                return;
            }
        }

        /// <summary>Assombrit toutes les matieres (une copie pour ce modele) ; les yeux (matieres "Eye") brulent de "eyes".</summary>
        public static Material[] Darken(GameObject model, float keep, Color eyes)
        {
            List<Material> all = new List<Material>();
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    bool eye = m.name.ToLowerInvariant().Contains("eye");
                    Color c = m.HasProperty("_Color") ? m.color : Color.gray;
                    float grey = (c.r + c.g + c.b) / 3f;
                    Color dull = Color.Lerp(new Color(grey, grey, grey), c, 0.35f) * keep;
                    dull.a = 1f;
                    if (m.HasProperty("_Color")) m.color = eye ? Color.black : dull;
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
                    if (eye && m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", eyes);
                    }
                    all.Add(m);
                }
                r.materials = mats;
            }
            return all.ToArray();
        }

        /// <summary>
        /// (21/11 soir) Une SILHOUETTE : toutes les matieres remplacees par l'Ombre du kit (noire, mate). On lit la forme,
        /// jamais les details : le cerveau invente le reste, et ce qu'il invente est pire.
        /// </summary>
        public static Material Silhouette(GameObject model, float grey)
        {
            Material m = NightKit.Shadow();
            if (m == null) m = new Material(Shader.Find("Standard"));
            m.color = new Color(grey, grey, grey * 1.15f);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.35f);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
            return m;
        }

        /// <summary>Deux points de braise, minuscules, poses sur l'os de la tete (ils suivent ses mouvements).</summary>
        public static Material EmberEyes(GameObject model, float size)
        {
            Transform head = null;
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.EndsWith("head") || n.Contains("|head") || n == "head") { head = t; break; }
            }
            Material glow = MaterialFactory.GetGlow(new Color(1f, 0.1f, 0.02f), 6f);
            Material mine = new Material(glow);
            Transform holder = new GameObject("Yeux de braise").transform;
            if (head != null)
            {
                holder.SetParent(head, false);
                holder.position = head.position + model.transform.parent.forward * 0.16f + Vector3.up * 0.06f;
            }
            else
            {
                holder.SetParent(model.transform.parent, false);
                holder.localPosition = new Vector3(0f, 0.86f, 0.2f);
            }
            holder.rotation = model.transform.parent.rotation;
            Proto.BeginVisualOnly();
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject eye = Proto.Sphere(null, Vector3.zero, Vector3.one, Color.red, "Oeil");
                eye.transform.SetParent(holder, false);
                Vector3 ls = holder.lossyScale;
                eye.transform.localScale = new Vector3(size / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), size * 0.55f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), size / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
                eye.transform.localPosition = new Vector3(s * 0.055f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), 0f, 0f);
                Renderer r = eye.GetComponent<Renderer>();
                r.sharedMaterial = mine;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Proto.EndVisualOnly();
            return mine;
        }

        /// <summary>Un spectre : une matiere qui s'ajoute a l'image (on voit au travers), d'un bleu glace.</summary>
        public static Material Ghostly(GameObject model, Color tint)
        {
            Shader add = Shader.Find("Legacy Shaders/Particles/Additive");
            if (add == null) return null;
            Material m = new Material(add);
            m.SetColor("_TintColor", tint);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return m;
        }

        public static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }

    /// <summary>
    /// (21/11) Le plan B de Puppet.Spawn : une image apres l'apparition, Unity a anime et "skinne" le modele ; ses bornes
    /// (updateWhenOffscreen) sont alors vraies. On le met a la bonne hauteur, les pieds au sol, puis ce composant s'en va.
    /// </summary>
    public class PuppetFit : MonoBehaviour
    {
        public float Height = 2f;
        int frames;

        void LateUpdate()
        {
            if (++frames < 2) return;
            Bounds b = new Bounds();
            bool any = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (any && b.size.y > 0.001f)
            {
                transform.localScale = transform.localScale * (Height / b.size.y);
                Bounds after = new Bounds();
                bool first = true;
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                {
                    if (first) { after = r.bounds; first = false; } else after.Encapsulate(r.bounds);
                }
                float ground = transform.parent != null ? transform.parent.position.y : 0f;
                transform.position += Vector3.up * (ground - after.min.y);
                if (Application.isBatchMode) Debug.Log("[FIEF] Modele ajuste (plan B) : " + name + " " + b.size + " -> " + Height + " m");
            }
            Destroy(this);
        }
    }

    /// <summary>
    /// LES BETES (21/11). Martin : "des monstres plus compliques avec une mecanique speciale, plus flippants, plus de
    /// monstres, et des monstres qu'on puisse combattre des moments". Chacune a SA regle, qu'on apprend en la survivant :
    ///
    ///   LES AMES       (les cranes spectraux) traversent les murs, invisibles au-dela de ta lumiere ; pres de toi, elles
    ///                  BOIVENT ta flamme. Un coup de torche les dissipe (elles reviennent plus loin).
    ///   LE BOUCHER     (la brute des cuisines et des caves) est AVEUGLE : il entend. Tes pas de course, un geste rate,
    ///                  une porte : il fonce vers le bruit. Marche doucement. Trois coups de torche : il fuit.
    ///   LE DEMON       vit dans le noir. Tant que ta torche brule fort, il garde ses distances, deux yeux rouges au loin ;
    ///                  qu'elle faiblisse, et il vient. Il souffle les bougies qu'il frole quand tu ne le regardes pas.
    ///   LA BETE        (l'ossuaire) charge en ligne droite des qu'elle te voit, et ne sait pas tourner : esquive, elle
    ///                  s'ecrase contre le mur, sonnee. C'est LA que tes coups de torche comptent. Cinq : elle tombe.
    ///   L'ECORCHEUR    ne bouge que dans ton dos ; il CRAINT ton regard : regarde-le, il recule dans l'ombre.
    ///   LES LOUPS      de la grande cour : rapides, en meute, ils tournent autour de ta flamme sans oser la toucher ;
    ///                  qu'elle baisse, ils mordent. Un coup de torche : un glapissement, ils s'enfuient.
    ///
    /// Toutes : aucun feu allume ne se passe (NightWard.Hold), et ta torche, tant qu'elle brule fort, tient les autres a deux
    /// metres et demi. Ce qui te tue, c'est la NUIT : une torche qui meurt.
    /// </summary>
    public class Beast : MonoBehaviour
    {
        public enum Kind { Soul, Butcher, Demon, Brute, Flayer, Wolf }
        public static readonly List<Beast> All = new List<Beast>();
        public static void Forget() { All.Clear(); }

        public Kind kind;
        NightPlayer prey;
        Animation anim;
        Material ghost;
        Light eyes;
        Vector3 home;
        Vector3 roam;
        Vector3 noise;
        Vector3 charge;
        float hp, flee, stun, think, heard, step, bob, gone, lunge;
        // (21/11 soir) La peur : la matiere noire, les yeux de braise, les saccades, l'apparition.
        Material skin;
        Material pupils;
        GameObject model;
        CreepyRig rig;
        int age;
        float frozen, twitch, revealCooldown;
        bool revealed;
        /// <summary>De 0 (aucune bete pres de toi) a 1 (une bete sur toi). La torche, le brouillard, le coeur la lisent.</summary>
        public static float Dread;
        bool roaming, hunting, charging, dead, sleeping;
        public bool Held;

        public bool Active { get { return !dead && gone <= 0f && gameObject.activeSelf; } }
        public bool Touching
        {
            get
            {
                if (!Active || prey == null || Held || stun > 0f || flee > 0f) return false;
                Vector3 d = prey.transform.position - transform.position;
                return Mathf.Abs(d.y) < 1.8f && Puppet.Flat(d).magnitude < (kind == Kind.Brute ? 1.6f : 1.05f);
            }
        }
        public string Title
        {
            get
            {
                switch (kind)
                {
                    case Kind.Soul: return "une Âme";
                    case Kind.Butcher: return "le Boucher";
                    case Kind.Demon: return "le Démon";
                    case Kind.Brute: return "la Bête";
                    case Kind.Flayer: return "l'Écorcheur";
                    default: return "les loups";
                }
            }
        }

        static string Model(Kind k)
        {
            switch (k)
            {
                case Kind.Soul: return "Modeles/Monstres/Monstre_GhostSkull";
                case Kind.Butcher: return "Modeles/Monstres/Monstre_Orc";
                case Kind.Demon: return "Modeles/Monstres/Monstre_Demon";
                case Kind.Brute: return "Modeles/Monstres/Monstre_Yeti";
                case Kind.Flayer: return "Modeles/Monstres/Monstre_Ninja";
                default: return "Modeles/Monstres/Monstre_Loup";
            }
        }

        /// <summary>De combien on etire le corps : grand et maigre, ou bas et long. L'inquietant, c'est le "presque humain".</summary>
        static Vector3 Stretch(Kind k)
        {
            switch (k)
            {
                case Kind.Demon: return new Vector3(0.7f, 1.45f, 0.7f);
                case Kind.Flayer: return new Vector3(0.6f, 1.5f, 0.6f);
                case Kind.Butcher: return new Vector3(0.95f, 1.2f, 0.85f);
                case Kind.Brute: return new Vector3(1.1f, 1.05f, 1.25f);
                case Kind.Soul: return new Vector3(0.75f, 1.6f, 0.75f);
                default: return new Vector3(0.8f, 1.1f, 1.35f);
            }
        }

        static float Height(Kind k)
        {
            switch (k)
            {
                case Kind.Soul: return 1.5f;
                case Kind.Butcher: return 2.5f;
                case Kind.Demon: return 2.9f;
                case Kind.Brute: return 3.1f;
                case Kind.Flayer: return 2.6f;
                default: return 1.15f;
            }
        }

        public static Beast Spawn(Transform parent, Kind kind, Vector3 at, NightPlayer prey)
        {
            GameObject go = new GameObject("BÊTE");
            go.transform.SetParent(parent, false);
            at.y = NightMap.FloorY(at);
            go.transform.position = at;
            Beast b = go.AddComponent<Beast>();
            b.kind = kind; b.prey = prey; b.home = at;
            b.hp = kind == Kind.Brute ? 5f : kind == Kind.Butcher ? 3f : 1f;
            b.sleeping = kind == Kind.Brute;
            // (21/11 soir) Martin : "rends les monstres beaucoup plus flippants". Plus de couleurs, plus de bonnes joues :
            //   - un CORPS ETIRE (plus grand, plus maigre) : le corps est pose dans un "holder" etire, la taille finale reste juste ;
            //   - une SILHOUETTE NOIRE (la matiere Ombre, comme le Penitent), dans une fumee epaisse ;
            //   - plus de gros yeux de dessin anime : deux POINTS DE BRAISE, minuscules, sur la tete (l'os "Head").
            Transform body = new GameObject("Corps").transform;
            body.SetParent(go.transform, false);
            body.localScale = Stretch(kind);
            GameObject model = Puppet.Spawn(Model(kind), body, Height(kind));
            if (model != null)
            {
                b.anim = model.GetComponentInChildren<Animation>();
                if (kind == Kind.Soul) b.ghost = Puppet.Ghostly(model, new Color(0.25f, 0.45f, 0.7f, 0.5f));
                else b.skin = Puppet.Silhouette(model, kind == Kind.Wolf ? 0.03f : 0.012f);
                if (kind != Kind.Wolf) { NightKit.Smoke(go.transform, Height(kind)); NightKit.Smoke(go.transform, Height(kind) * 0.5f); }
                b.model = model;      // les yeux de braise viendront quand le modele aura sa taille finale (Update)
                // (22/11) La nuque cassee, le dos voute, les bras qui pendent, la tete qui te suit.
                b.rig = CreepyRig.Add(model, go.transform, prey != null ? prey.transform : null, kind == Kind.Wolf ? 18f : kind == Kind.Brute ? 30f : Random.Range(45f, 70f), kind == Kind.Wolf ? 0f : 24f);
            }
            // Une lueur rouge, faible, la ou sont les yeux (le Boucher est aveugle : rien).
            if (kind != Kind.Butcher && kind != Kind.Soul)
            {
                b.eyes = new GameObject("Lueur des yeux").AddComponent<Light>();
                b.eyes.transform.SetParent(go.transform, false);
                b.eyes.transform.localPosition = new Vector3(0f, Height(kind) * 0.86f, 0.35f);
                b.eyes.type = LightType.Point;
                b.eyes.color = new Color(1f, 0.12f, 0.05f);
                b.eyes.range = 1.6f;
                b.eyes.intensity = 0.6f;
            }
            Puppet.Play(b.anim, 1f, false, "|Idle;|Flying_Idle;|Idle_2");
            All.Add(b);
            return b;
        }

        void OnDestroy() { All.Remove(this); }

        /// <summary>Un bruit : le Boucher fonce, les autres tendent l'oreille.</summary>
        public static void Hear(Vector3 at)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Beast b = All[i];
                if (b == null || !b.Active) continue;
                float d = Puppet.Flat(at - b.transform.position).magnitude;
                if (b.kind == Kind.Butcher && d < 30f) { b.noise = at; b.heard = 9f; }
                else if (b.kind == Kind.Brute && d < 14f) b.sleeping = false;
            }
        }

        /// <summary>Toutes figees (la prise, les cinematiques).</summary>
        public static void HoldAll(bool held) { for (int i = 0; i < All.Count; i++) if (All[i] != null) All[i].Held = held; }

        /// <summary>Le monstre qui te tient, s'il y en a un.</summary>
        public static Beast Toucher { get { for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].Touching) return All[i]; return null; } }

        /// <summary>Le plus proche, devant toi, a portee de torche (pour le coup).</summary>
        public static Beast InFront(Transform eye, float reach)
        {
            Beast best = null;
            float bestD = reach;
            for (int i = 0; i < All.Count; i++)
            {
                Beast b = All[i];
                if (b == null || !b.Active) continue;
                Vector3 to = b.transform.position + Vector3.up * 1f - eye.position;
                float d = Puppet.Flat(to).magnitude;
                if (d > bestD || Mathf.Abs(to.y) > 2.5f) continue;
                if (Vector3.Angle(Puppet.Flat(eye.forward), Puppet.Flat(to)) > 55f) continue;
                best = b; bestD = d;
            }
            return best;
        }

        /// <summary>Elles s'enfuient toutes, sur "radius" metres (l'eau benite).</summary>
        public static int Scare(Vector3 at, float radius, float seconds)
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++)
            {
                Beast b = All[i];
                if (b == null || !b.Active || Puppet.Flat(b.transform.position - at).magnitude > radius) continue;
                if (b.kind == Kind.Soul) b.Dissolve(); else b.flee = Mathf.Max(b.flee, seconds);
                n++;
            }
            return n;
        }

        /// <summary>
        /// UN COUP DE TORCHE. Renvoie ce qu'il s'est passe ("" : rien de special). Les ames se dissipent, les loups
        /// glapissent et fuient, le Boucher compte ses brulures, la Bete ne sent rien... sauf quand elle est sonnee.
        /// </summary>
        public string Burn(Vector3 from)
        {
            Vector3 away = Puppet.Flat(transform.position - from);
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            try { NightAudio.Bang(transform.position + Vector3.up, 0.9f); } catch (System.Exception) { }
            Puppet.Play(anim, 1.2f, true, "|HitReact;|HitRecieve;|Idle_HitReact_Left;|Death");
            switch (kind)
            {
                case Kind.Soul:
                    Dissolve();
                    return "L'âme se déchire dans un cri.";
                case Kind.Wolf:
                    flee = 14f;
                    NightAudio.Scream(transform.position + Vector3.up, 0.6f);
                    return "";
                case Kind.Brute:
                    if (stun <= 0f) { NightAudio.Bang(transform.position + Vector3.up, 0.8f); return "Elle ne sent rien. Fais-la charger contre un mur."; }
                    hp -= 1f;
                    stun = Mathf.Max(stun, 1.2f);
                    if (hp <= 0f) { Die(); return "La Bête s'effondre. L'ossuaire est à toi."; }
                    return "Encore. Elle chancelle.";
                case Kind.Butcher:
                    hp -= 1f;
                    Push(away, 1.8f);
                    if (hp <= 0f) { flee = 40f; hp = 3f; NightAudio.Scream(transform.position + Vector3.up * 1.8f, 1f); return "Le Boucher s'enfuit en hurlant."; }
                    flee = 1.5f;
                    return "";
                default:
                    flee = kind == Kind.Demon ? 18f : 10f;
                    Push(away, 2f);
                    NightAudio.Scream(transform.position + Vector3.up * 1.8f, 0.8f);
                    return "";
            }
        }

        public bool Dead { get { return dead; } }

        void Push(Vector3 dir, float metres)
        {
            Vector3 to = transform.position + dir.normalized * metres;
            if (Physics.Linecast(transform.position + Vector3.up, to + Vector3.up, ~(1 << 2), QueryTriggerInteraction.Ignore)) return;
            to.y = NightMap.FloorY(to);
            transform.position = to;
        }

        void Die()
        {
            dead = true;
            Puppet.Play(anim, 0.8f, true, "|Death");
            if (eyes != null) eyes.enabled = false;
            NightAudio.Bang(transform.position + Vector3.up, 1f);
            NightAudio.Scream(transform.position + Vector3.up * 2f, 1f);
        }

        /// <summary>Une ame se dissipe ; elle reviendra dans une minute, ailleurs, loin de toi.</summary>
        void Dissolve()
        {
            gone = 60f;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            NightAudio.Scream(transform.position + Vector3.up, 0.5f);
        }

        // ------------------------------------------------------------------ marcher

        /// <summary>Avance vers "goal" a "speed" m/s : par les cases du chateau (ou tout droit a travers les murs pour une ame).</summary>
        bool Go(Vector3 goal, float speed)
        {
            Vector3 target;
            if (kind == Kind.Soul) target = goal;
            else
            {
                bool ok;
                target = NightMap.Toward(transform.position, goal, out ok);
                if (!ok) return false;
            }
            Vector3 flat = Puppet.Flat(target - transform.position);
            if (flat.magnitude < 0.05f) return true;
            Quaternion face = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, face, (kind == Kind.Brute && charging ? 30f : 360f) * Time.deltaTime);
            Vector3 next = transform.position + flat.normalized * Mathf.Min(speed * Time.deltaTime, flat.magnitude);
            // Ta torche : tant qu'elle brule fort, rien ne s'approche a moins de deux metres et demi (sauf une Bete qui charge).
            float hold = kind == Kind.Wolf ? 4f : 2.5f;
            bool scaresIt = kind != Kind.Brute && Faith.Value > (kind == Kind.Wolf ? 0.2f : 0.3f);      // (22/11) reequilibre : le jeu etait impossible
            if (scaresIt && prey != null)
            {
                float dNext = Puppet.Flat(prey.transform.position - next).magnitude;
                if (dNext < hold && dNext < Puppet.Flat(prey.transform.position - transform.position).magnitude) next = transform.position;
            }
            next = NightWard.Hold(transform.position, next);
            next.y = kind == Kind.Soul ? Mathf.Lerp(transform.position.y, NightMap.FloorY(next) + 0.4f + Mathf.Sin(Time.time * 1.3f) * 0.15f, Time.deltaTime * 2f) : NightMap.FloorY(next);
            bool moved = (next - transform.position).sqrMagnitude > 0.0001f;
            transform.position = next;
            if (moved && kind != Kind.Soul && kind != Kind.Wolf)
            {
                step -= speed * Time.deltaTime;
                if (step <= 0f) { step = kind == Kind.Brute ? 1.6f : 1.1f; try { NightAudio.Step(transform.position, 0.9f); } catch (System.Exception) { } }
            }
            return true;
        }

        void Wander(float speed)
        {
            if (!roaming) { roam = kind == Kind.Butcher ? NearHome() : NightMap.RandomCell(transform.position, 6f, 30f); roaming = true; }
            if (!Go(roam, speed) || Puppet.Flat(roam - transform.position).magnitude < 0.7f) roaming = false;
        }

        /// <summary>Le Boucher ne quitte pas son coin (les cuisines, les caves) : il y tourne en rond.</summary>
        Vector3 NearHome()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = NightMap.RandomCell(home, 0f, 18f);
                if (Mathf.Abs(p.y - home.y) < 0.5f || i == 9) return p;
            }
            return home;
        }

        bool Sees(float range)
        {
            if (prey == null) return false;
            Vector3 from = transform.position + Vector3.up * 1.5f, to = prey.transform.position + Vector3.up * 1.2f;
            if ((to - from).magnitude > range) return false;
            return !Physics.Linecast(from, to, ~(1 << 2), QueryTriggerInteraction.Ignore);
        }

        /// <summary>Le joueur le regarde-t-il ? (dans l'image et rien devant)</summary>
        bool Watched() { return prey != null && NightDirector.Sees(prey.View, transform.position + Vector3.up * 1.2f); }

        // ------------------------------------------------------------------ chaque image

        void Update()
        {
            age++;
            if (age == 4 && model != null && kind != Kind.Butcher && kind != Kind.Soul) pupils = Puppet.EmberEyes(model, kind == Kind.Wolf ? 0.03f : 0.04f);
            if (prey == null || Held || dead) return;
            float dt = Time.deltaTime;
            if (gone > 0f)
            {
                gone -= dt;
                if (gone <= 0f)
                {
                    transform.position = NightMap.RandomCell(prey.transform.position, 18f, 40f) + Vector3.up * 0.4f;
                    foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
                }
                return;
            }
            Vector3 me = prey.transform.position;
            float dist = Puppet.Flat(me - transform.position).magnitude;
            bool sameFloor = Mathf.Abs(me.y - transform.position.y) < 2.5f;

            if (flee > 0f)
            {
                flee -= dt;
                Vector3 away = transform.position + Puppet.Flat(transform.position - me).normalized * 6f;
                Go(away, kind == Kind.Wolf ? 7f : 4f);
                Puppet.Play(anim, 1.3f, false, "|Run;|Gallop;|Fast_Flying;|Walk");
                return;
            }
            if (stun > 0f)
            {
                stun -= dt;
                Puppet.Play(anim, 0.6f, false, "|Duck;|HitReact;|Idle");
                return;
            }
            if (eyes != null) eyes.intensity = 0.4f + 0.5f * Mathf.PerlinNoise(Time.time * 3f, transform.position.x);
            if (Scare(dist, sameFloor, dt)) return;

            switch (kind)
            {
                case Kind.Soul: TickSoul(dist, sameFloor); break;
                case Kind.Butcher: TickButcher(dist, sameFloor); break;
                case Kind.Demon: TickDemon(dist, sameFloor); break;
                case Kind.Brute: TickBrute(dist, sameFloor); break;
                case Kind.Flayer: TickFlayer(dist, sameFloor); break;
                default: TickWolf(dist, sameFloor); break;
            }
        }

        /// <summary>
        /// (21/11 soir) CE QUI FAIT PEUR, pour toutes les betes :
        ///   la SAISIE    il se fige net (l'animation s'arrete en plein geste), puis repart d'un coup, plus pres : jamais
        ///                un mouvement regulier qu'on pourrait lire ;
        ///   le TIC       regarde-le : sa tete, son corps tressaillent, sans raison ;
        ///   l'APPARITION la premiere fois qu'il entre dans ta lumiere, pres de toi : un hurlement, l'image tremble ;
        ///   les FEUX     pres de lui, les bougies vacillent, ta torche tousse (Dread).
        /// Renvoie true quand il est fige (il ne fait rien d'autre cette image).
        /// </summary>
        bool Scare(float dist, bool sameFloor, float dt)
        {
            if (kind == Kind.Wolf && dist > 10f) return false;
            float near = sameFloor ? Mathf.InverseLerp(14f, 2f, dist) : 0f;
            if (near > Dread) Dread = near;
            bool watched = Watched();

            // L'apparition.
            revealCooldown -= dt;
            if (!revealed && watched && dist < 9f && revealCooldown <= 0f && kind != Kind.Wolf)
            {
                revealed = true;
                revealCooldown = 25f;
                NightAudio.Scream(transform.position + Vector3.up * 1.8f, 1f);
                NightAudio.Sting(1f);
                NightPlayer.Shake = Mathf.Max(NightPlayer.Shake, 0.75f);
                NightPlayer.Punch = 1f;
                frozen = 0.35f;
            }
            if (dist > 22f) revealed = false;

            // La saisie : fige, l'animation arretee en plein geste.
            if (frozen > 0f)
            {
                frozen -= dt;
                if (anim != null) anim.enabled = false;
                if (frozen <= 0f && anim != null) anim.enabled = true;
                if (frozen > 0f) return true;
            }
            else if (dist < 20f && sameFloor && kind != Kind.Wolf && Random.value < dt * (watched ? 0.25f : 0.6f))
                frozen = Random.Range(0.12f, 0.5f);

            // Le tic : quand tu le regardes, il tressaille.
            twitch -= dt;
            if (watched && dist < 16f && twitch <= 0f)
            {
                twitch = Random.Range(0.4f, 1.6f);
                transform.Rotate(0f, Random.Range(-14f, 14f), 0f);
                if (anim != null) foreach (AnimationState st in anim) st.speed = Random.Range(0.5f, 2.4f);
            }

            // Les feux tremblent pres de lui ; il les eteint s'il est un Demon ou l'Ecorcheur.
            if (dist < 30f)
            {
                NightWard w = NightWard.NearestBurning(transform.position, 4f);
                if (w != null && (kind == Kind.Demon || kind == Kind.Flayer) && !watched && Random.value < dt * 0.3f) w.Snuff();
            }
            if (rig != null) rig.Shiver = near;
            if (pupils != null) pupils.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.02f) * (3f + 5f * near + Mathf.Sin(Time.time * 13f) * near));
            return false;
        }

        /// <summary>Chaque image, la peur repart de zero : les betes la remontent dans leur Update.</summary>
        public static void ResetDread() { Dread = Mathf.MoveTowards(Dread, 0f, Time.deltaTime * 2f); }

        void TickSoul(float dist, bool sameFloor)
        {
            // Invisible loin de ta lumiere : elle apparait en s'approchant, comme un souffle de givre.
            if (ghost != null)
            {
                float seen = Mathf.Clamp01((8f - dist) / 5f);
                ghost.SetColor("_TintColor", new Color(0.25f, 0.45f, 0.7f, 0.04f + 0.5f * seen * (0.8f + 0.2f * Mathf.Sin(Time.time * 4f))));
            }
            // Elle boit ta flamme.
            if (dist < 5f) Faith.Value = Mathf.Clamp01(Faith.Value - Time.deltaTime * 0.012f * (5f - dist));
            bool hunt = dist < 24f;
            heard -= Time.deltaTime;
            if (hunt && dist < 14f && heard <= 0f) { heard = Random.Range(4f, 9f); NightAudio.Moan(transform.position, 0.7f); }
            if (hunt) Go(prey.transform.position, 1.6f + (Faith.Value < 0.3f ? 0.8f : 0f));
            else Wander(1.1f);
            Puppet.Play(anim, 1f, false, hunt ? "|Fast_Flying" : "|Flying_Idle");
        }

        void TickButcher(float dist, bool sameFloor)
        {
            // Aveugle. Il entend : courir a 18 m, marcher a 4 m, rester immobile : rien.
            bool loud = FiefInput.Move.sqrMagnitude > 0.01f && sameFloor && (FiefInput.SprintHeld ? dist < 20f : dist < 4.5f);
            if (loud) { if (heard <= 0f) NightAudio.Roar(transform.position + Vector3.up * 2f, 0.9f); noise = prey.transform.position; heard = 6f; }
            if (heard > 0f)
            {
                heard -= Time.deltaTime;
                Go(noise, 5.4f);
                Puppet.Play(anim, 1.2f, false, "|Run");
                if (Puppet.Flat(noise - transform.position).magnitude < 1f) heard = Mathf.Min(heard, 1.5f);
                return;
            }
            Wander(1.3f);
            Puppet.Play(anim, 0.8f, false, "|Walk");
        }

        void TickDemon(float dist, bool sameFloor)
        {
            // Il te suit toujours, de loin. Torche forte : il reste a 10 m et plus. Torche faible : il vient.
            bool bold = Faith.Value < 0.45f;
            float keep = bold ? 0f : 10f;
            if (dist > 40f || !sameFloor)
            {
                // Trop loin : il se glisse, hors de ta vue, quelque part derriere toi.
                think -= Time.deltaTime;
                if (think <= 0f)
                {
                    think = 6f;
                    Vector3 spot = NightMap.RandomCell(prey.transform.position, 14f, 22f);
                    if (!NightDirector.Sees(prey.View, spot + Vector3.up * 1.5f)) transform.position = spot;
                }
                Puppet.Play(anim, 1f, false, "|Idle");
                return;
            }
            if (dist > keep + 0.5f) { Go(prey.transform.position, bold ? 4.2f : 2.2f); Puppet.Play(anim, bold ? 1.3f : 0.8f, false, bold ? "|Run" : "|Walk"); }
            else if (!bold && dist < keep - 1.5f) { Go(transform.position + Puppet.Flat(transform.position - prey.transform.position).normalized * 3f, 2.2f); Puppet.Play(anim, 0.8f, false, "|Walk"); }
            else Puppet.Play(anim, 1f, false, "|Idle");
            think -= Time.deltaTime;
            if (think <= 0f && dist < 18f) { think = Random.Range(5f, 10f); NightAudio.Growl(transform.position + Vector3.up * 2f, 0.6f); }
            // Il souffle les bougies qu'il frole quand tu ne le regardes pas.
            if (!Watched())
            {
                NightWard w = NightWard.NearestBurning(transform.position, 5f);
                if (w != null && w.kind == NightWard.Kind.Candles) w.Snuff();
            }
        }

        void TickBrute(float dist, bool sameFloor)
        {
            if (sleeping)
            {
                Puppet.Play(anim, 0.5f, false, "|Idle");
                if (sameFloor && dist < 9f) { sleeping = false; NightAudio.Roar(transform.position + Vector3.up * 2.5f, 1f); NightAudio.Sting(1f); }
                return;
            }
            if (charging)
            {
                // Tout droit, sans tourner. Contre un mur : sonnee quatre secondes.
                Vector3 dir = Puppet.Flat(charge - transform.position);
                Vector3 next = transform.position + dir.normalized * 10f * Time.deltaTime;
                bool wall = Physics.Linecast(transform.position + Vector3.up * 1.2f, next + Vector3.up * 1.2f + dir.normalized * 0.9f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                if (wall || NightWard.Hold(transform.position, next) != next)
                {
                    charging = false; stun = 4f;
                    NightAudio.Bang(transform.position + Vector3.up, 1f);
                    NightPlayer.Shake = Mathf.Max(NightPlayer.Shake, 0.6f);
                    return;
                }
                next.y = NightMap.FloorY(next);
                transform.position = next;
                lunge -= Time.deltaTime;
                if (lunge <= 0f || dir.magnitude < 0.5f) { charging = false; think = 1.2f; }
                step -= 8.5f * Time.deltaTime;
                if (step <= 0f) { step = 2f; try { NightAudio.Step(transform.position, 0.9f); } catch (System.Exception) { } }
                Puppet.Play(anim, 1.6f, false, "|Run");
                return;
            }
            think -= Time.deltaTime;
            Vector3 to = Puppet.Flat(prey.transform.position - transform.position);
            if (sameFloor && Sees(16f))
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 120f * Time.deltaTime);
                Puppet.Play(anim, 1f, false, "|Idle");
                if (think <= 0f && Vector3.Angle(transform.forward, to) < 10f)
                {
                    // Elle gratte le sol, gronde, et part. Elle vise ou tu ES, pas ou tu vas : bouge au dernier moment.
                    charging = true; lunge = 3.5f;
                    charge = prey.transform.position + to.normalized * 10f;
                    NightAudio.Roar(transform.position + Vector3.up * 2.5f, 1f);
                }
                return;
            }
            if (Puppet.Flat(home - transform.position).magnitude > 1f) { Go(home, 2f); Puppet.Play(anim, 0.9f, false, "|Walk"); }
            else Puppet.Play(anim, 0.6f, false, "|Idle");
        }

        void TickFlayer(float dist, bool sameFloor)
        {
            // Il craint ton regard : regarde-le, il recule dans l'ombre. Dos tourne, il fond sur toi.
            bool watched = Watched() && dist < 16f && Faith.Value > 0.05f;
            if (watched)
            {
                Go(transform.position + Puppet.Flat(transform.position - prey.transform.position).normalized * 4f, 3f);
                Puppet.Play(anim, 1.2f, false, "|Run");
                return;
            }
            if (dist > 30f || !sameFloor)
            {
                think -= Time.deltaTime;
                if (think <= 0f)
                {
                    think = 8f;
                    Vector3 spot = NightMap.RandomCell(prey.transform.position, 10f, 16f);
                    if (!NightDirector.Sees(prey.View, spot + Vector3.up * 1.5f)) transform.position = spot;
                }
                return;
            }
            Go(prey.transform.position, 5f);
            Puppet.Play(anim, 1.5f, false, "|Run");
            // Son souffle, tout pres, dans ton dos.
            heard -= Time.deltaTime;
            if (dist < 7f && heard <= 0f) { heard = 4f; NightAudio.Growl(transform.position + Vector3.up, 0.35f); }
        }

        void TickWolf(float dist, bool sameFloor)
        {
            bool near = sameFloor && dist < 22f && NightMap.RoomAt(prey.transform.position) == NightMap.RoomAt(transform.position);
            if (near)
            {
                // Ils tournent autour de ta flamme : chacun garde son angle, a quatre metres.
                bob += Time.deltaTime * 0.6f;
                float a = bob + home.x;
                Vector3 ring = prey.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Faith.Value > 0.2f ? 4.3f : 0.5f);
                Go(ring, 6.2f);
                Puppet.Play(anim, 1.2f, false, "|Gallop;|Walk");
                heard -= Time.deltaTime;
                if (heard <= 0f) { heard = Random.Range(3f, 6f); try { NightAudio.Roar(transform.position, 0.8f); } catch (System.Exception) { } }
                return;
            }
            if (Puppet.Flat(home - transform.position).magnitude > 1.5f) { Go(home, 3f); Puppet.Play(anim, 1f, false, "|Walk"); }
            else Puppet.Play(anim, 1f, false, "|Idle;|Idle_2");
        }
    }

    /// <summary>
    /// (21/11) UN SURVIVANT. Martin : "il faut des PNJ trop styles". Il se cache, tremble, se leve quand tu arrives. On lui
    /// parle (E) ; il te donne ce qu'il a ; puis il part se cacher dans la chapelle (il disparait quand tu ne le regardes pas,
    /// et on le retrouve la-bas a la fin).
    /// </summary>
    public class Survivor : MonoBehaviour
    {
        public static readonly List<Survivor> All = new List<Survivor>();
        public string Id;
        public string Name;
        public string[] Lines;
        public bool Met;
        public bool Safe;
        Animation anim;
        NightPlayer prey;
        float leaving = -1f;

        public static Survivor Spawn(Transform parent, string model, Vector3 at, float yaw, float height, string id, string name, string[] lines, NightPlayer prey)
        {
            GameObject go = new GameObject("Survivant : " + name);
            go.transform.SetParent(parent, false);
            at.y = NightMap.FloorY(at);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Survivor s = go.AddComponent<Survivor>();
            s.Id = id; s.Name = name; s.Lines = lines; s.prey = prey;
            GameObject m = Puppet.Spawn("Modeles/Gens/" + model, go.transform, height);
            if (m != null)
            {
                s.anim = m.GetComponentInChildren<Animation>();
                Puppet.Darken(m, 0.55f, Color.black);
            }
            // Une petite lueur de chandelle a ses pieds : on le trouve dans le noir.
            Light l = new GameObject("Chandelle").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0.4f, 0.5f, 0.6f);
            l.color = new Color(1f, 0.7f, 0.4f);
            l.range = 3.5f;
            l.intensity = 0.7f;
            Puppet.Play(s.anim, 1f, false, "|Duck;|Man_Sitting;|Idle_Neutral;|Idle");
            All.Add(s);
            return s;
        }

        void OnDestroy() { All.Remove(this); }

        public bool Near(Vector3 p) { return !Met && gameObject.activeSelf && Puppet.Flat(p - transform.position).magnitude < 2.6f && Mathf.Abs(p.y - transform.position.y) < 2f; }

        /// <summary>Apres avoir parle : il part vers la chapelle.</summary>
        public void Leave() { Met = true; leaving = 4f; Puppet.Play(anim, 1f, false, "|Idle;|Man_Idle;|Idle_Neutral"); }

        /// <summary>A la fin : il attend dans la chapelle.</summary>
        public void ShowInChapel(Vector3 at)
        {
            gameObject.SetActive(true);
            at.y = NightMap.FloorY(at);
            transform.position = at;
            Puppet.Play(anim, 1f, false, "|Man_Clapping;|Idle;|Idle_Neutral");
        }

        void Update()
        {
            if (prey == null) return;
            Vector3 to = Puppet.Flat(prey.transform.position - transform.position);
            if (!Met)
            {
                // Il se tourne vers toi quand tu approches, et se releve.
                if (to.magnitude < 6f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 120f * Time.deltaTime);
                    Puppet.Play(anim, 1f, false, "|Idle_Neutral;|Man_Standing;|Idle");
                }
                return;
            }
            if (leaving > 0f)
            {
                leaving -= Time.deltaTime;
                if (leaving <= 0f && !NightDirector.Sees(prey.View, transform.position + Vector3.up * 1.4f)) { Safe = true; gameObject.SetActive(false); }
                else if (leaving <= 0f) leaving = 0.5f;
            }
        }
    }

    /// <summary>
    /// (21/11) CROC, LE CHIEN DES ECURIES. Martin : "des animaux a recruter". Il a peur et grogne ; donne-lui la viande de la
    /// cuisiniere (E) et il te suit. Il SENT les monstres : il gronde quand l'un approche (meme ceux qu'on ne voit pas), aboie
    /// quand il est la, et se jette dessus pour le faire fuir (une fois toutes les vingt secondes).
    /// </summary>
    public class Hound : MonoBehaviour
    {
        public bool Tamed;
        NightPlayer master;
        Animation anim;
        float bark, bite;
        AudioSource voice;
        static AudioClip barkClip;
        /// <summary>Ce qu'il vient de faire, pour l'ecran ("" : rien).</summary>
        public static string Said = "";
        public static Hound Dog;

        public static Hound Spawn(Transform parent, Vector3 at, NightPlayer master)
        {
            GameObject go = new GameObject("CROC, le chien");
            go.transform.SetParent(parent, false);
            at.y = NightMap.FloorY(at);
            go.transform.position = at;
            Hound h = go.AddComponent<Hound>();
            h.master = master;
            GameObject m = Puppet.Spawn("Modeles/Gens/Chien", go.transform, 0.85f);
            if (m != null) { h.anim = m.GetComponentInChildren<Animation>(); Puppet.Darken(m, 0.6f, Color.black); }
            h.voice = go.AddComponent<AudioSource>();
            h.voice.spatialBlend = 0.8f;
            h.voice.playOnAwake = false;
            h.voice.clip = Bark();
            Puppet.Play(h.anim, 1f, false, "|Idle_2_HeadLow;|Idle");
            Dog = h;
            Said = "";
            return h;
        }

        public bool Near(Vector3 p) { return !Tamed && Puppet.Flat(p - transform.position).magnitude < 2.4f; }

        public void Tame() { Tamed = true; Woof(1f); Puppet.Play(anim, 1f, true, "|Eating"); bark = 2f; }

        void Woof(float volume) { NightAudio.Bark(transform.position + Vector3.up * 0.6f, volume); }

        /// <summary>Un aboiement fabrique par le code : un souffle bref, une note qui tombe, deux fois.</summary>
        static AudioClip Bark()
        {
            if (barkClip != null) return barkClip;
            const int rate = 44100;
            int n = rate / 2;
            float[] d = new float[n];
            System.Random rnd = new System.Random(3);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float local = t < 0.25f ? t : t - 0.25f;
                float env = local < 0.012f ? local / 0.012f : Mathf.Exp(-(local - 0.012f) * 22f);
                float f = 430f - 900f * local;
                float tone = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f * 2.02f * t) * 0.25f;
                float hiss = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.35f;
                d[i] = (tone + hiss) * env * 0.7f;
            }
            barkClip = AudioClip.Create("Aboiement", n, 1, rate, false);
            barkClip.SetData(d, 0);
            return barkClip;
        }

        void Update()
        {
            if (master == null) return;
            float dt = Time.deltaTime;
            Vector3 me = master.transform.position;
            Vector3 to = Puppet.Flat(me - transform.position);
            // Le monstre le plus proche.
            Beast threat = null;
            float tD = 14f;
            foreach (Beast b in Beast.All)
            {
                if (b == null || !b.Active) continue;
                float d = Puppet.Flat(b.transform.position - transform.position).magnitude;
                if (d < tD && Mathf.Abs(b.transform.position.y - transform.position.y) < 2.5f) { tD = d; threat = b; }
            }
            if (!Tamed)
            {
                if (to.magnitude < 7f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 90f * dt);
                bark -= dt;
                if (to.magnitude < 5f && bark <= 0f) { bark = 3.5f; Woof(0.5f); }
                return;
            }
            bark -= dt; bite -= dt;
            if (threat != null)
            {
                Vector3 at = Puppet.Flat(threat.transform.position - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(at.normalized, Vector3.up), 300f * dt);
                if (bark <= 0f) { bark = tD < 6f ? 0.8f : 2.2f; Woof(tD < 6f ? 1f : 0.6f); if (Said == "") Said = "Croc gronde : " + threat.Title + ", tout près."; }
                if (tD < 6f && bite <= 0f && threat.kind != Beast.Kind.Brute)
                {
                    // Il se jette dessus.
                    bite = 20f;
                    Vector3 lunge = threat.transform.position - at.normalized * 0.8f;
                    lunge.y = NightMap.FloorY(lunge);
                    transform.position = lunge;
                    threat.Burn(transform.position);
                    Puppet.Play(anim, 1.2f, true, "|Attack");
                    Said = "Croc se jette sur " + threat.Title + " !";
                    return;
                }
            }
            // Il te suit, a deux metres, un peu sur le cote.
            Vector3 heel = me - master.transform.forward * 1.6f + master.transform.right * 1.1f;
            Vector3 gap = Puppet.Flat(heel - transform.position);
            if (gap.magnitude > 18f || Mathf.Abs(me.y - transform.position.y) > 3f && gap.magnitude > 3f)
            {
                // Trop loin (ou reste en bas d'un escalier) : il te rejoint quand tu ne regardes pas.
                if (!NightDirector.Sees(master.View, transform.position + Vector3.up * 0.5f)) { heel.y = NightMap.FloorY(heel); transform.position = heel; }
            }
            if (gap.magnitude > 0.6f)
            {
                bool ok;
                NightMap.Friendly = true;
                Vector3 target = NightMap.Toward(transform.position, heel, out ok);
                NightMap.Friendly = false;
                Vector3 flat = Puppet.Flat(target - transform.position);
                if (ok && flat.magnitude > 0.05f)
                {
                    float speed = gap.magnitude > 5f ? 6.5f : 3.6f;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat.normalized, Vector3.up), 400f * dt);
                    Vector3 next = transform.position + flat.normalized * Mathf.Min(speed * dt, flat.magnitude);
                    next.y = NightMap.FloorY(next);
                    transform.position = next;
                    Puppet.Play(anim, 1f, false, speed > 5f ? "|Gallop" : "|Walk");
                }
            }
            else if (threat == null) Puppet.Play(anim, 1f, false, "|Idle;|Idle_2");
        }
    }

    /// <summary>
    /// (21/11) PARLER. Une boite de dialogue en bas de l'ecran : le nom de celui qui parle, sa phrase. E ou Entree : la suite.
    /// Pendant qu'on parle, le temps s'arrete (NightUI met Time.timeScale a 0).
    /// </summary>
    public static class NightTalk
    {
        public static bool Open;
        static string who = "";
        static string[] lines;
        static int index;
        static float shownAt;
        static System.Action done;

        public static void Start(string speaker, string[] say, System.Action onDone)
        {
            who = speaker; lines = say; index = 0; done = onDone; Open = true; shownAt = Time.unscaledTime;
        }

        public static void Tick()
        {
            if (!Open) return;
            if (Time.unscaledTime - shownAt < 0.25f) return;
            if (FiefInput.InteractPressed || FiefInput.ConfirmPressed || FiefInput.JumpPressed || FiefInput.ShootPressed)
            {
                index++;
                shownAt = Time.unscaledTime;
                try { Sfx.Pop(); } catch (System.Exception) { }
                if (index >= lines.Length)
                {
                    Open = false;
                    System.Action callback = done;
                    done = null;
                    if (callback != null) callback.Invoke();
                }
            }
        }

        public static void Draw(float w, float h, GUIStyle text)
        {
            if (!Open || lines == null || index >= lines.Length) return;
            Rect box = new Rect(Mathf.Round(w * 0.16f), Mathf.Round(h * 0.7f), Mathf.Round(w * 0.68f), Mathf.Round(h * 0.2f));
            GUI.color = new Color(1f, 1f, 1f, 0.94f);
            GUI.DrawTexture(box, NightUI.Parchment());
            GUI.color = Color.white;
            GUIStyle name = new GUIStyle(text);
            name.font = NightFont.Title();
            name.fontSize = Mathf.RoundToInt(h * 0.038f);
            name.alignment = TextAnchor.UpperLeft;
            name.normal.textColor = new Color(0.45f, 0.08f, 0.04f);
            GUI.Label(new Rect(box.x + box.width * 0.05f, box.y + box.height * 0.08f, box.width * 0.9f, box.height * 0.25f), who, name);
            GUIStyle say = new GUIStyle(text);
            say.fontSize = Mathf.RoundToInt(h * 0.028f);
            say.alignment = TextAnchor.UpperLeft;
            say.wordWrap = true;
            say.normal.textColor = new Color(0.18f, 0.11f, 0.06f);
            // La phrase s'ecrit lettre par lettre, comme a la plume.
            string line = lines[index];
            int shown = Mathf.Min(line.Length, Mathf.FloorToInt((Time.unscaledTime - shownAt) * 55f));
            GUI.Label(new Rect(box.x + box.width * 0.05f, box.y + box.height * 0.34f, box.width * 0.9f, box.height * 0.5f), line.Substring(0, shown), say);
            GUIStyle foot = new GUIStyle(say);
            foot.fontSize = Mathf.RoundToInt(h * 0.018f);
            foot.alignment = TextAnchor.LowerRight;
            foot.fontStyle = FontStyle.Italic;
            foot.normal.textColor = new Color(0.35f, 0.25f, 0.15f);
            GUI.Label(new Rect(box.x, box.y, box.width * 0.96f, box.height * 0.92f), "E  —  la suite   (" + (index + 1) + "/" + lines.Length + ")", foot);
        }
    }
}
