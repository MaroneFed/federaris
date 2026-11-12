#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// LES PHOTOS DE LA NUIT (12/11). Lance La Nuit, attend deux secondes, prend quatre images du
    /// jeu tel qu'il tourne (vestibule, couloir, niche, chapelle), les range dans Logs/Captures,
    /// puis arrete. Menu FIEF > LA NUIT - prendre des photos.
    ///
    /// C'est aussi par la que Claude voit le jeu sur le PC de Martin, sans ouvrir la fenetre :
    ///   Unity.exe -batchmode -projectPath . -executeMethod Fief.NightCapture.Run
    ///
    /// Concept Unity : quand on appuie sur Play, Unity recharge tous les scripts et oublie leurs
    /// variables. SessionState garde une note d'un rechargement a l'autre ; InitializeOnLoad
    /// relance ce fichier juste apres le rechargement.
    /// </summary>
    [InitializeOnLoad]
    public static class NightCapture
    {
        const string Key = "fief.capture";
        const string StartKey = "fief.capture.debut";
        const string NightPath = "Assets/_Fief/Scenes/Nuit.unity";
        static int frames;

        static NightCapture()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        [MenuItem("FIEF/LA NUIT - prendre des photos", false, 2)]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("FIEF (La Nuit)").AddComponent<NightBootstrap>();
            Directory.CreateDirectory("Assets/_Fief/Scenes");
            EditorSceneManager.SaveScene(scene, NightPath);
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(StartKey, (float)EditorApplication.timeSinceStartup);
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                // Le jeu ne demarre pas (une erreur de compilation, par exemple) : on n'attend pas pour rien.
                if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartKey, 0f) > 180f) Finish(1);
                return;
            }
            frames++;
            if (frames != 150 && frames != 400) return;
            int code = 0;
            try
            {
                if (frames == 150)
                {
                    // La nuit telle qu'on la trouve, puis on allume tout pour la seconde serie.
                    Shoot();
                    foreach (Candle c in Object.FindObjectsByType<Candle>(FindObjectsSortMode.None)) c.Ignite();
                    NightKit.LightDawnFires();
                    return;
                }
                ShootLit();
            }
            catch (System.Exception e) { Debug.LogError("[FIEF] Photos ratées : " + e); code = 1; }
            Finish(code);
        }

        static void ShootLit()
        {
            Camera cam = Camera.main;
            if (cam == null) throw new System.Exception("pas de caméra");
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Captures");
            Shot(cam, Path.Combine(dir, "6_chapelle_allumee.png"), new Vector3(0f, 1.65f, 33.5f), new Vector3(0f, 2.8f, 44f));
            Shot(cam, Path.Combine(dir, "7_cierge.png"), new Vector3(-2.6f, 1.55f, 36.2f), new Vector3(-4.2f, 1.45f, 34.5f));
            Shot(cam, Path.Combine(dir, "8_autel.png"), new Vector3(1.8f, 1.6f, 37.5f), new Vector3(-0.6f, 2.6f, 44f));
        }

        static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        static void Shoot()
        {
            Camera cam = Camera.main;
            if (cam == null) throw new System.Exception("pas de caméra");
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Captures");
            Directory.CreateDirectory(dir);
            Shot(cam, Path.Combine(dir, "1_vestibule.png"), new Vector3(0f, 1.65f, 1.2f), new Vector3(0f, 2f, 8f));
            Shot(cam, Path.Combine(dir, "2_couloir.png"), new Vector3(0f, 1.65f, 8.6f), new Vector3(0f, 2.3f, 30f));
            Shot(cam, Path.Combine(dir, "3_niche.png"), new Vector3(-0.9f, 1.65f, 18.2f), new Vector3(4.2f, 1.5f, 22f));
            Shot(cam, Path.Combine(dir, "3b_penitent.png"), new Vector3(0.6f, 1.65f, 22f), new Vector3(4.2f, 1.75f, 22f));
            Shot(cam, Path.Combine(dir, "3c_penitent_pres.png"), new Vector3(2.6f, 1.75f, 22.5f), new Vector3(4.2f, 2.0f, 22f));
            Shot(cam, Path.Combine(dir, "4_chapelle.png"), new Vector3(0f, 1.65f, 33.5f), new Vector3(0f, 2.6f, 44f));
            Shot(cam, Path.Combine(dir, "5_mur.png"), new Vector3(0.6f, 1.6f, 15.2f), new Vector3(-2f, 1.7f, 13.6f));
            Debug.Log("[FIEF] Photos de la nuit dans " + dir);
        }

        static void Shot(Camera cam, string file, Vector3 from, Vector3 lookAt)
        {
            Vector3 p = cam.transform.position;
            Quaternion r = cam.transform.rotation;
            cam.transform.position = from;
            cam.transform.LookAt(lookAt);
            // En HDR, comme la vraie camera du jeu : sans ca, le halo des flammes ne se declenche pas.
            RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            cam.transform.position = p;
            cam.transform.rotation = r;
        }
    }
}
#endif
