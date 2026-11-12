#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// LE KIT DU CHATEAU (12/11) : sept pieces faites dans Blender (mur, mur a arche, dalle, voute,
    /// pilier, torche, brasero), rangees dans Assets/_Fief/Resources/Modeles/Kit.
    ///
    /// Menu FIEF > Kit - refaire les matieres : fabrique les matieres Pierre et Fer a partir des
    /// textures, les branche sur les modeles, et fabrique deux prefabs de feu (flamme animee,
    /// braises, lumiere qui tremble). A relancer seulement si on remplace les textures ou les modeles.
    ///
    /// Concept Unity : une MATIERE (Material) dit comment une surface reagit a la lumiere. Elle lit
    /// plusieurs images : la couleur (Albedo), le relief (Normal), et un "masque" qui dit ou c'est
    /// du metal et ou ca brille. Un PREFAB est un objet tout monte, range comme un fichier, qu'on
    /// peut poser autant de fois qu'on veut.
    /// </summary>
    public static class KitFiefSetup
    {
        public const string Root = "Assets/_Fief/Resources/Modeles/Kit";

        [MenuItem("FIEF/Kit - refaire les matieres", false, 30)]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                Debug.LogError("[FIEF] Kit introuvable : " + Root);
                return;
            }
            Material pierre = MakeMaterial("Pierre", false);
            Material fer = MakeMaterial("Fer", true);
            Material vitrail = MakeGlass();
            Material statue = MakeMaterial("Statue", true);       // le Penitent : sa pierre, et ses yeux qui brillent

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) continue;
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Pierre"), pierre);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Fer"), fer);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Statue"), statue);
                if (vitrail != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Vitrail"), vitrail);
                mi.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();

            Texture2D flameTex = Tex("Flamme_Flipbook.png", false, false);
            Texture2D sparkTex = Tex("Braise_Point.png", false, false);
            Material flameMat = MakeAdditive("Flamme", flameTex);
            Material sparkMat = MakeAdditive("Braise", sparkTex);
            if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
            // Le dernier vecteur : ou nait la flamme (le point "socket_flamme" du modele, s'il est retrouve).
            MakeFirePrefab("torche_murale", "Torche_Murale_Feu", 0.45f, 1.6f, 9f, flameMat, sparkMat, new Vector3(0f, 2.06f, 0.52f));
            MakeFirePrefab("brasero", "Brasero_Feu", 1.0f, 2.4f, 13f, flameMat, sparkMat, new Vector3(0f, 1.24f, 0f));
            MakeFirePrefab("cierge", "Cierge_Feu", 0.22f, 0f, 0f, flameMat, sparkMat, new Vector3(0f, 1.61f, 0f));     // sa lumiere : celle de Candle
            AssetDatabase.SaveAssets();
            Debug.Log("[FIEF] Kit : matieres et prefabs de feu prets dans " + Root);
        }

        static bool IsPipeline(string word)
        {
            return GraphicsSettings.currentRenderPipeline != null
                && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains(word);
        }

        static Texture2D Tex(string file, bool normal, bool linear)
        {
            string path = Root + "/Textures/" + file;
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                Debug.LogWarning("[FIEF] Kit : texture absente " + path);
                return null;
            }
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !(normal || linear);
            ti.maxTextureSize = 4096;
            ti.anisoLevel = 8;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Asset(string name, Shader shader)
        {
            string dir = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Root, "Materials");
            string matPath = dir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, matPath);
            }
            else
            {
                m.shader = shader;
            }
            return m;
        }

        static Material MakeMaterial(string name, bool emissive)
        {
            Texture2D albedo = Tex(name + "_Albedo.png", false, false);
            Texture2D normal = Tex(name + "_Normal.png", true, true);
            Texture2D mask = Tex(name + "_Mask.png", false, true);
            Texture2D emission = emissive ? Tex(name + "_Emission.png", false, false) : null;

            Shader shader = null;
            if (IsPipeline("Universal")) shader = Shader.Find("Universal Render Pipeline/Lit");
            else if (IsPipeline("HD")) shader = Shader.Find("HDRP/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = Asset(name, shader);

            Set(m, "_BaseMap", albedo); Set(m, "_MainTex", albedo); Set(m, "_BaseColorMap", albedo);
            SetColor(m, "_BaseColor", Color.white); SetColor(m, "_Color", Color.white);
            Set(m, "_BumpMap", normal); Set(m, "_NormalMap", normal);
            SetFloat(m, "_BumpScale", 1f); SetFloat(m, "_NormalScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            // Le masque : rouge = metal, vert = ombre des creux, alpha = lisse.
            Set(m, "_MetallicGlossMap", mask); Set(m, "_OcclusionMap", mask); Set(m, "_MaskMap", mask);
            SetFloat(m, "_Smoothness", 1f); SetFloat(m, "_GlossMapScale", 1f); SetFloat(m, "_Glossiness", 1f);
            SetFloat(m, "_Metallic", 1f); SetFloat(m, "_OcclusionStrength", 1f);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.EnableKeyword("_METALLICGLOSSMAP");
            m.EnableKeyword("_OCCLUSIONMAP");
            m.EnableKeyword("_MASKMAP");
            if (emission != null)
            {
                // Les yeux du Penitent restent ROUGES : trop fort, le halo les blanchit.
                Color glow = name == "Statue" ? new Color(1f, 0.10f, 0.05f) * 1.7f : new Color(1f, 1f, 1f) * 3f;
                Set(m, "_EmissionMap", emission); Set(m, "_EmissiveColorMap", emission);
                SetColor(m, "_EmissionColor", glow); SetColor(m, "_EmissiveColor", glow);
                m.EnableKeyword("_EMISSION");
                m.EnableKeyword("_EMISSIVE_COLOR_MAP");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Le verre du vitrail : presque noir a la lumiere, mais il BRILLE (emission) comme si la lune passait au travers.</summary>
        static Material MakeGlass()
        {
            Texture2D albedo = Tex("Vitrail_Albedo.png", false, false);
            Texture2D emission = Tex("Vitrail_Emission.png", false, false);
            if (emission == null) return null;
            Material m = Asset("Vitrail", Shader.Find("Standard"));
            Set(m, "_MainTex", albedo);
            SetColor(m, "_Color", Color.white);
            SetFloat(m, "_Glossiness", 0.75f);
            SetFloat(m, "_Metallic", 0f);
            Set(m, "_EmissionMap", emission);
            SetColor(m, "_EmissionColor", Color.white * 2.4f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MakeAdditive(string name, Texture2D tex)
        {
            Shader shader = null;
            if (IsPipeline("Universal")) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            else if (IsPipeline("HD")) shader = Shader.Find("HDRP/Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            Material m = Asset(name, shader);
            Set(m, "_BaseMap", tex); Set(m, "_MainTex", tex); Set(m, "_UnlitColorMap", tex);
            Color tint = new Color(1.6f, 1.6f, 1.6f, 1f);
            SetColor(m, "_BaseColor", tint); SetColor(m, "_Color", tint); SetColor(m, "_UnlitColor", tint);
            SetFloat(m, "_Surface", 1f); SetFloat(m, "_Blend", 2f);
            SetFloat(m, "_SurfaceType", 1f); SetFloat(m, "_BlendMode", 1f);
            SetFloat(m, "_SrcBlend", (float)BlendMode.SrcAlpha); SetFloat(m, "_DstBlend", (float)BlendMode.One);
            SetFloat(m, "_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_BLENDMODE_ADD");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                Transform r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        static void MakeFirePrefab(string model, string prefabName, float size, float intensity, float range,
            Material flameMat, Material sparkMat, Vector3 flameAt)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + model + ".fbx");
            if (fbx == null)
            {
                Debug.LogWarning("[FIEF] Kit : modele absent " + model);
                return;
            }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            Transform socket = FindDeep(inst.transform, "socket_flamme");
            Vector3 p = socket != null ? inst.transform.InverseTransformPoint(socket.position) : flameAt;

            // Tout le feu sous un seul objet "Feu" : on l'eteint ou on l'allume d'un SetActive.
            GameObject fire = new GameObject("Feu");
            fire.transform.SetParent(inst.transform, false);
            fire.transform.localPosition = p;

            // La flamme : quelques images de la planche se recouvrent et se fondent.
            GameObject flame = new GameObject("Flamme");
            flame.transform.SetParent(fire.transform, false);
            flame.transform.localPosition = new Vector3(0f, size * 0.44f, 0f);
            ParticleSystem ps = flame.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = 2.6f;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startRotation = 0f;
            main.startColor = Color.white;
            main.maxParticles = 16;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.prewarm = true;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 1.6f;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = false;
            ParticleSystem.TextureSheetAnimationModule tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = 8;
            tsa.numTilesY = 8;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            tsa.cycleCount = 1;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f, 0.25f), new GradientAlphaKey(0.75f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystemRenderer pr = flame.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            pr.sharedMaterial = flameMat;

            // Les braises qui montent.
            GameObject sparks = new GameObject("Braises");
            sparks.transform.SetParent(fire.transform, false);
            sparks.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem sp = sparks.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule sm = sp.main;
            sm.loop = true;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.9f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            sm.startColor = new Color(1f, 0.55f, 0.15f, 1f);
            sm.gravityModifier = -0.06f;
            sm.maxParticles = 64;
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule se = sp.emission;
            se.rateOverTime = size * 14f;
            ParticleSystem.ShapeModule ss = sp.shape;
            ss.shapeType = ParticleSystemShapeType.Cone;
            ss.angle = 16f;
            ss.radius = size * 0.12f;
            ParticleSystem.NoiseModule sn = sp.noise;
            sn.enabled = true;
            sn.strength = 0.25f;
            sn.frequency = 0.8f;
            ParticleSystem.ColorOverLifetimeModule sc = sp.colorOverLifetime;
            sc.enabled = true;
            Gradient sg = new Gradient();
            sg.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            sc.color = new ParticleSystem.MinMaxGradient(sg);
            sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMat;

            // La lumiere chaude qui tremble (pas pour le cierge : Candle a deja la sienne).
            if (intensity > 0f)
            {
                GameObject lightGo = new GameObject("Lumiere");
                lightGo.transform.SetParent(fire.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, size * 0.35f, 0f);
                Light l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.68f, 0.42f);
                l.range = range;
                l.intensity = IsPipeline("HD") ? intensity * 600f : intensity;
                l.shadows = LightShadows.None;
                lightGo.AddComponent<KitFiefFlicker>();
            }

            PrefabUtility.SaveAsPrefabAsset(inst, Root + "/Prefabs/" + prefabName + ".prefab");
            Object.DestroyImmediate(inst);
        }

        static void Set(Material m, string prop, Texture t)
        {
            if (t != null && m.HasProperty(prop)) m.SetTexture(prop, t);
        }

        static void SetColor(Material m, string prop, Color c)
        {
            if (m.HasProperty(prop)) m.SetColor(prop, c);
        }

        static void SetFloat(Material m, string prop, float v)
        {
            if (m.HasProperty(prop)) m.SetFloat(prop, v);
        }
    }
}
#endif
