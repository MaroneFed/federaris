#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE NUMERO STEAM A COTE DU JEU EXPORTE (09/10, v31). Pour que Steam reconnaisse le jeu
    /// lance hors de Steam (le .exe de File ▸ Build), il faut le fichier steam_appid.txt (480,
    /// l'appli d'essai de Valve) A COTE du .exe. Dans l'editeur, il est a la racine du projet ;
    /// apres chaque Build, on le recopie ici tout seul.
    /// </summary>
    public static class SteamAppId
    {
        [PostProcessBuild]
        public static void AfterBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64
                && target != BuildTarget.StandaloneLinux64 && target != BuildTarget.StandaloneOSX) return;
            string source = Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt");
            string dir = Path.GetDirectoryName(pathToBuiltProject);
            try
            {
                if (File.Exists(source)) File.Copy(source, Path.Combine(dir, "steam_appid.txt"), true);
                else File.WriteAllText(Path.Combine(dir, "steam_appid.txt"), "480");
            }
            catch (System.Exception e) { Debug.LogWarning("[FIEF] steam_appid.txt pas copie : " + e.Message); }
        }
    }
}
#endif
