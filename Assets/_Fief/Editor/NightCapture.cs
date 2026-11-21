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
                    Prepare();
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

        static Vector3 Eye(Vector3 ground) { return new Vector3(ground.x, ground.y + 1.65f, ground.z); }

        /// <summary>(21/11) Le portrait d'un etre (monstre, survivant, chien) : la camera devant lui, a hauteur d'yeux.</summary>
        static void Portrait(Camera cam, string name, Transform who, float distance, float height)
        {
            if (who == null) return;
            Vector3 front = who.forward;
            front.y = 0f;
            Vector3 from = who.position + front.normalized * distance + Vector3.up * 1.6f;
            Shot(cam, name, from, who.position + Vector3.up * height);
        }

        static void Shoot()
        {
            Camera cam = Camera.main;
            if (cam == null) throw new System.Exception("pas de caméra");
            dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "Captures");
            if (Directory.Exists(dir)) foreach (string old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
            Directory.CreateDirectory(dir);
            Vector3 v = NightMap.Center('V'), h = NightMap.Center('H'), k = NightMap.Center('K');
            Vector3 north = Vector3.forward, east = Vector3.right, up = Vector3.up;

            // L'ouverture : le Veilleur agonisant, a nos pieds.
            Vector3 start = Eye(v) - north * 1.6f;
            NightCinema.Aim(start);
            NightCinema.Stage(null, true, false);
            Shot(cam, "01_ouverture_veilleur", start, start + north * 1.6f - up * 1.45f);
            NightCinema.Clear();

            Shot(cam, "02_vestibule", Eye(v) - north * 2.5f + east * 2f, Eye(v) + north * 4f - east * 3f);
            Shot(cam, "03_couloir_torches", Eye(NightMap.CellCenter(7, 3)), Eye(NightMap.CellCenter(12, 3)));
            Shot(cam, "04_grande_salle", Eye(h) - north * 6.5f - east * 9f, Eye(h) + north * 5f + east * 8f + up * 1.5f);
            Vector3 j = NightMap.Center('J');
            Shot(cam, "05_grande_cour", Eye(j) - north * 10f - east * 9f, Eye(j) + north * 6f + east * 6f + up * 2f);
            Vector3 b = NightMap.Center('B');
            Shot(cam, "06_ecuries", Eye(b) - east * 10f - north * 1.2f, Eye(b) + east * 6f + north * 1.5f);
            Vector3 g = NightMap.Center('G');
            Shot(cam, "07_donjon", Eye(g) - north * 7f, Eye(g) + north * 6f + up * 1f);
            Shot(cam, "08_escalier_monte", new Vector3(98f, 1.65f, 29f), new Vector3(98f, 5.5f, 42f));
            Vector3 u = NightMap.Center('U');
            Shot(cam, "09_donjon_etage", Eye(u) - north * 6f + east * 2f, Eye(u) + north * 5f - east * 2f);
            Shot(cam, "10_escalier_descend", new Vector3(58f, 1.65f, 37f), new Vector3(58f, -3.5f, 50f));
            Vector3 w = NightMap.Center('W');
            Shot(cam, "11_caves", Eye(w) - north * 6f - east * 5f, Eye(w) + north * 4f + east * 4f);
            Vector3 o = NightMap.Center('O');
            Shot(cam, "12_ossuaire", Eye(o) - north * 6.5f - east * 4f, Eye(o) + north * 2f + east * 2f);
            Shot(cam, "13_cuisines", Eye(NightMap.Center('Q')) - east * 5f - north * 3f, Eye(NightMap.Center('Q')) + east * 6f);
            Shot(cam, "14_chapelle", Eye(k) - north * 4.5f, Eye(k) + north * 6f + up * 1.2f);

            // Les vivants, et le chien.
            foreach (Survivor s in Survivor.All) Portrait(cam, "15_survivant_" + s.Id, s.transform, 2.6f, 1.3f);
            if (Hound.Dog != null) Portrait(cam, "16_chien", Hound.Dog.transform, 2.4f, 0.5f);

            // Le Penitent dans sa niche, un Rodeur.
            Penitent monster = Object.FindAnyObjectByType<Penitent>();
            Vector3 n = NightMap.Center('N');
            if (monster != null) { monster.ShowPose(2); monster.Glare(2.6f); Shot(cam, "17_penitent", Eye(n) - east * 2.6f + north * 0.3f, Eye(n) + up * 0.5f); monster.ShowPose(0); monster.Glare(0f); }
            NightPlayer knight = Object.FindAnyObjectByType<NightPlayer>();
            if (knight != null)
            {
                Vector3 from = NightMap.CellCenter(8, 8), to = NightMap.CellCenter(4, 8);
                Prowler rodeur = Prowler.Spawn(null, new[] { from, to }, knight);
                rodeur.transform.rotation = Quaternion.LookRotation(to - from);
                Shot(cam, "18_rodeur", Eye(to) + east * 1.5f, Eye(from) + up * 0.1f);
                Object.Destroy(rodeur.gameObject);
                Prowler.Forget();
            }
        }

        /// <summary>
        /// (21/11) Entre les deux series : les monstres en rang dans la grande cour (on attend 250 images que leurs
        /// animations jouent), et tous les feux allumes.
        /// </summary>
        static void Prepare()
        {
            NightPlayer knight = Object.FindAnyObjectByType<NightPlayer>();
            Vector3 j = NightMap.Center('J');
            float[] xs = { -10f, -6f, -1.5f, 3f, 6.5f, 10f };
            Beast.Kind[] kinds = { Beast.Kind.Soul, Beast.Kind.Butcher, Beast.Kind.Demon, Beast.Kind.Brute, Beast.Kind.Flayer, Beast.Kind.Wolf };
            for (int i = 0; i < kinds.Length; i++)
            {
                Beast found = null;
                foreach (Beast b in Beast.All) if (b != null && b.kind == kinds[i]) { found = b; break; }
                if (found == null && knight != null) found = Beast.Spawn(null, kinds[i], j, knight);
                if (found == null) continue;
                found.gameObject.SetActive(true);
                Vector3 at = new Vector3(j.x + xs[i], 0f, j.z - 6f);
                found.transform.position = at + (kinds[i] == Beast.Kind.Soul ? Vector3.up * 0.4f : Vector3.zero);
                found.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                found.Held = true;
                Animation a = found.GetComponentInChildren<Animation>();
                Puppet.Play(a, 1f, false, "|Walk;|Flying_Idle;|Idle");
            }
            foreach (Candle c in Object.FindObjectsByType<Candle>(FindObjectsSortMode.None)) c.Ignite();
            foreach (NightWard w in NightWard.All) if (w != null) w.Ignite();
            if (knight != null)
            {
                NightLight.Candles(null, NightMap.CellCenter(9, 3) + Vector3.right * 1f, 90f, true);
                NightLight.Eternal(null, NightMap.CellCenter(5, 3) + Vector3.forward * 1f);
            }
            NightKit.LightDawnFires();
        }

        static void ShootLit()
        {
            Camera cam = Camera.main;
            if (cam == null) throw new System.Exception("pas de caméra");
            Vector3 k = NightMap.Center('K'), h = NightMap.Center('H'), j = NightMap.Center('J');
            Vector3 north = Vector3.forward, east = Vector3.right, up = Vector3.up;
            // Les monstres, un par un, de face : la camera a trois metres, a hauteur d'homme.
            float[] xs = { -10f, -6f, -1.5f, 3f, 6.5f, 10f };
            string[] names = { "20_ame", "21_boucher", "22_demon", "23_bete", "24_ecorcheur", "25_loup" };
            for (int i = 0; i < xs.Length; i++)
            {
                Vector3 at = new Vector3(j.x + xs[i], 0f, j.z - 6f);
                Shot(cam, names[i], at + new Vector3(0f, 1.65f, -3.4f), at + up * (i == 5 ? 0.5f : 1.3f));
            }
            Shot(cam, "26_cour_rangee", Eye(j) - north * 11.5f, Eye(j) - north * 6f + up * 0.4f);
            Shot(cam, "27_couloir_allume", Eye(NightMap.CellCenter(7, 3)), Eye(NightMap.CellCenter(12, 3)));
            Shot(cam, "28_bougies_cierge", Eye(NightMap.CellCenter(7, 3)) + north * 0.3f, NightMap.CellCenter(5, 3) + north * 1f + up * 0.5f);
            Shot(cam, "29_chapelle_allumee", Eye(k) - north * 4.5f, Eye(k) + north * 6f + up * 1.2f);
            Shot(cam, "30_grande_salle_allumee", Eye(h) - north * 6.5f - east * 9f, Eye(h) + north * 5f + east * 8f + up * 1.5f);
            Shot(cam, "31_grande_cour_allumee", Eye(j) + north * 8f + east * 8f, Eye(j) - north * 4f - east * 6f);
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
