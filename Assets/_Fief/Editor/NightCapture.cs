#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// LES PHOTOS DE LA NUIT (12/11, refait le 15/11 pour le grand chateau). Lance La Nuit, attend, prend
    /// des images du jeu tel qu'il tourne, les range dans Logs/Captures, puis arrete.
    /// Menu FIEF > LA NUIT - prendre des photos.
    ///
    /// C'est par la que Claude voit le jeu sur le PC de Martin, sans ouvrir la fenetre :
    ///   Unity.exe -batchmode -projectPath . -executeMethod Fief.NightCapture.Run   (ou Tools/photos-nuit.ps1)
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
        static string dir;

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

        static void Finish(int code)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        static Vector3 Eye(Vector3 ground) { return new Vector3(ground.x, 1.65f, ground.z); }

        static void Shoot()
        {
            Camera cam = Camera.main;
            if (cam == null) throw new System.Exception("pas de caméra");
            dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Captures");
            if (Directory.Exists(dir)) foreach (string old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
            Directory.CreateDirectory(dir);
            Vector3 v = NightMap.Center('V'), h = NightMap.Center('H'), k = NightMap.Center('K'), y = NightMap.Center('Y');
            Vector3 n = NightMap.Center('N'), s = NightMap.Center('S'), a = NightMap.Center('A'), l = NightMap.Center('L');
            Vector3 north = Vector3.forward, east = Vector3.right, up = Vector3.up;

            // L'ouverture : l'homme devant, la forme noire derriere.
            Vector3 start = Eye(v) - north * 1.6f;
            NightCinema.Aim(start);
            NightCinema.Stage(null, true, false);
            Shot(cam, "01_ouverture_homme", start, start + north * 3.4f - up * 0.1f);
            NightCinema.Stage(null, false, true);
            Shot(cam, "02_ouverture_forme", start, start - north * 1.9f + up * 0.6f);
            NightCinema.Clear();

            Shot(cam, "03_vestibule", Eye(v) - north * 2.5f + east * 2f, Eye(v) + north * 4f - east * 3f);
            Shot(cam, "04_porte_sacristie", Eye(v) + east * 1f, Eye(s) + east * 3.5f);
            Shot(cam, "05_couloir_sud", Eye(NightMap.CellCenter(7, 3)), Eye(NightMap.CellCenter(12, 3)));
            Shot(cam, "06_grande_salle", Eye(h) - north * 6.5f - east * 9f, Eye(h) + north * 5f + east * 8f + up * 1.5f);
            Shot(cam, "07_grande_salle_armures", Eye(h) - east * 6f, Eye(h) + east * 6f + north * 7.2f);
            Shot(cam, "08_cour", Eye(y) - north * 3f - east * 3f, Eye(y) + north * 3f + east * 3f + up * 2.5f);
            Shot(cam, "09_armurerie", Eye(a) - north * 1.4f - east * 2.5f, Eye(a) + north * 1.6f + east * 1f);
            Shot(cam, "10_bibliotheque", Eye(l) - north * 4f - east * 6f, Eye(l) + north * 2f + east * 4f);
            Shot(cam, "11_couloir_ouest", Eye(NightMap.CellCenter(1, 4)), Eye(NightMap.CellCenter(1, 8)));
            Shot(cam, "12_chapelle", Eye(k) - north * 4.5f, Eye(k) + north * 6f + up * 1.2f);

            // Le Penitent : endormi dans sa niche, puis ses deux autres poses, yeux allumes.
            Penitent monster = Object.FindAnyObjectByType<Penitent>();
            Vector3 hallway = Eye(NightMap.CellCenter(1, 5));
            Shot(cam, "13_penitent_dort", hallway + north * 0.2f, Eye(n) + up * 0.3f);
            if (monster != null)
            {
                monster.ShowPose(1); monster.Glare(1.4f);
                Shot(cam, "14_penitent_vu", hallway - north * 1.2f, Eye(n) + up * 0.3f);
                monster.ShowPose(2); monster.Glare(2.6f);
                Shot(cam, "15_penitent_veut", Eye(n) - east * 2.6f + north * 0.3f, Eye(n) + up * 0.5f);
                monster.ShowPose(0); monster.Glare(0f);
            }

            // Un Rodeur dans le couloir du nord : de face (sa lanterne sur nous), puis de dos.
            NightPlayer knight = Object.FindAnyObjectByType<NightPlayer>();
            if (knight != null)
            {
                Vector3 from = NightMap.CellCenter(8, 8), to = NightMap.CellCenter(4, 8);
                Prowler rodeur = Prowler.Spawn(null, new[] { from, to }, knight);
                rodeur.transform.rotation = Quaternion.LookRotation(to - from);
                Shot(cam, "16_rodeur_face", Eye(to) + east * 1.5f, Eye(from) + up * 0.1f);
                Shot(cam, "17_rodeur_dos", Eye(from) + east * 5f, Eye(from));
                Object.Destroy(rodeur.gameObject);
                Prowler.Forget();
            }
        }

        static void ShootLit()
        {
            Camera cam = Camera.main;
            if (cam == null) throw new System.Exception("pas de caméra");
            Vector3 k = NightMap.Center('K'), h = NightMap.Center('H');
            Vector3 north = Vector3.forward, east = Vector3.right, up = Vector3.up;
            Shot(cam, "18_chapelle_allumee", Eye(k) - north * 4.5f, Eye(k) + north * 6f + up * 1.2f);
            Shot(cam, "19_autel", Eye(k) + east * 2.2f, Eye(k) + north * 6f - east * 0.6f + up * 1f);
            Shot(cam, "20_grande_salle_allumee", Eye(h) - north * 6.5f - east * 9f, Eye(h) + north * 5f + east * 8f + up * 1.5f);
            Debug.Log("[FIEF] Photos de la nuit dans " + dir);
        }

        static void Shot(Camera cam, string name, Vector3 from, Vector3 lookAt)
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
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
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
