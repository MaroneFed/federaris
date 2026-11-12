#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fief
{
    /// <summary>
    /// LA NUIT EN .EXE (12/11). Fabrique une version jouable de La Nuit, hors d'Unity :
    /// Builds/FIEF-La-Nuit/FIEF-La-Nuit.exe (le dossier Builds n'est jamais envoye sur GitHub).
    /// Menu FIEF > LA NUIT - fabriquer le jeu (.exe), ou sans fenetre :
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod Fief.NightBuild.Run
    ///
    /// Concept Unity : un BUILD, c'est le jeu transforme en programme autonome. Unity y range les
    /// scenes qu'on lui donne et tout ce qui se trouve dans les dossiers "Resources". Ici on ne
    /// lui donne QUE la scene de La Nuit : la liste des scenes du projet (Main) n'est pas touchee.
    /// </summary>
    public static class NightBuild
    {
        const string NightPath = "Assets/_Fief/Scenes/Nuit.unity";

        [MenuItem("FIEF/LA NUIT - fabriquer le jeu (.exe)", false, 3)]
        public static void Run()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("FIEF (La Nuit)").AddComponent<NightBootstrap>();
            Directory.CreateDirectory("Assets/_Fief/Scenes");
            EditorSceneManager.SaveScene(scene, NightPath);

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "FIEF-La-Nuit");
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = new[] { NightPath };
            options.locationPathName = Path.Combine(dir, "FIEF-La-Nuit.exe");
            options.target = BuildTarget.StandaloneWindows64;
            options.options = BuildOptions.None;
            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[FIEF] Build de La Nuit : " + report.summary.result + ", " + (report.summary.totalSize / (1024 * 1024)) + " Mo, "
                + report.summary.totalErrors + " erreur(s) -> " + options.locationPathName);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
#endif
