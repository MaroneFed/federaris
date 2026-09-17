#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// Outils de l'editeur, dans le menu "FIEF" en haut de la fenetre Unity.
    ///
    /// Concept Unity : tout script place dans un dossier nomme "Editor" ne part PAS
    /// dans le jeu final. Il ne sert qu'a l'editeur.
    ///
    /// Le bouton "Reparer la scene" est un filet de securite : si Main.unity est
    /// corrompue ou perdue, il la recree a l'identique en un clic.
    /// </summary>
    public static class FiefMenu
    {
        const string ScenePath = "Assets/_Fief/Scenes/Main.unity";

        [MenuItem("FIEF/Ouvrir la scene Main %#m", false, 0)]
        public static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[FIEF] Main.unity introuvable. Utilise FIEF > Reparer la scene Main.");
            }
        }

        [MenuItem("FIEF/Reparer la scene Main", false, 20)]
        public static void RebuildScene()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Reparer la scene Main",
                "La scene Main.unity va etre recreee depuis zero :\n\n"
                + "un seul objet, 'FIEF (Bootstrap)', qui porte GameConfig et GameBootstrap.\n\n"
                + "Tout le reste du monde est genere au lancement par le code, "
                + "il n'y a donc rien a perdre.",
                "Recreer", "Annuler");
            if (!ok) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("FIEF (Bootstrap)");
            root.AddComponent<GameConfig>();
            root.AddComponent<GameBootstrap>();

            System.IO.Directory.CreateDirectory("Assets/_Fief/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            AddSceneToBuild();
            Debug.Log("[FIEF] Scene recreee : " + ScenePath + ". Appuie sur Play.");
        }

        [MenuItem("FIEF/Ajouter la scene au Build Settings", false, 21)]
        public static void AddSceneToBuild()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[1];
            scenes[0] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[FIEF] Main.unity est la scene de build.");
        }

        [MenuItem("FIEF/Ou sont les reglages ?", false, 40)]
        public static void SelectConfig()
        {
            GameConfig config = UnityEngine.Object.FindAnyObjectByType<GameConfig>();
            if (config == null)
            {
                EditorUtility.DisplayDialog("Reglages",
                    "Ouvre d'abord la scene Main (menu FIEF > Ouvrir la scene Main).", "OK");
                return;
            }

            Selection.activeGameObject = config.gameObject;
            EditorGUIUtility.PingObject(config.gameObject);
        }
    }
}
#endif
