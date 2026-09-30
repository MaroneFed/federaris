#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// PLUS D'ICONES DE SON ET DE LAMPE DANS LE JEU (03/10, Martin : "quand il y a du son,
    /// il y a une sorte d'emoji son qui apparait, et pareil pour la lumiere, c'est horrible").
    ///
    /// Ce ne sont pas des objets du jeu : ce sont les GIZMOS d'Unity, les petits dessins
    /// que l'editeur pose sur les objets invisibles pour qu'on les retrouve (un haut-parleur
    /// sur chaque source de son, une ampoule sur chaque lumiere). Depuis que les bruits
    /// viennent de l'endroit ou ils arrivent (v19), chaque son pose un haut-parleur la ou il
    /// sonne -- d'ou l'emoji "peu importe d'ou il vient". Dans le jeu exporte, ils n'existent
    /// pas ; dans l'editeur, on les coupe ici, tout seuls, a chaque ouverture et a chaque Play :
    ///   - l'icone des sources de son, des lumieres et des sondes de reflets ;
    ///   - le bouton "Gizmos" de la fenetre Game.
    ///
    /// Concept Unity : [InitializeOnLoad] lance ce code des que l'editeur a fini de compiler.
    /// Les fonctions de l'editeur qui reglent les gizmos changent de nom d'une version a
    /// l'autre : on les appelle "par leur nom" (reflexion) et on ne plante jamais si l'une
    /// manque -- au pire, il reste le bouton Gizmos a decocher a la main (fenetre Game, en
    /// haut a droite).
    /// </summary>
    [InitializeOnLoad]
    public static class NoGizmos
    {
        static readonly Type[] Hidden = { typeof(AudioSource), typeof(Light), typeof(ReflectionProbe), typeof(AudioReverbZone) };

        static NoGizmos()
        {
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) Apply();
            };
        }

        [MenuItem("FIEF/Cacher les icones son et lumiere", false, 40)]
        public static void Apply()
        {
            HideIcons();
            GameViewGizmosOff();
        }

        static void HideIcons()
        {
            Type utility = typeof(Editor).Assembly.GetType("UnityEditor.GizmoUtility");
            MethodInfo icon = utility != null ? utility.GetMethod("SetIconEnabled", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type), typeof(bool) }, null) : null;
            MethodInfo gizmo = utility != null ? utility.GetMethod("SetGizmoEnabled", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type), typeof(bool) }, null) : null;
            for (int i = 0; i < Hidden.Length; i++)
            {
                try
                {
                    if (icon != null) icon.Invoke(null, new object[] { Hidden[i], false });
                    if (gizmo != null) gizmo.Invoke(null, new object[] { Hidden[i], false });
                }
                catch (Exception e) { Debug.Log("[FIEF] Gizmos : " + e.Message); }
            }
        }

        /// <summary>Decoche le bouton "Gizmos" de chaque fenetre Game ouverte.</summary>
        static void GameViewGizmosOff()
        {
            Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView == null) return;
            FieldInfo flag = gameView.GetField("m_Gizmos", BindingFlags.NonPublic | BindingFlags.Instance);
            if (flag == null || flag.FieldType != typeof(bool)) return;
            UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gameView);
            for (int i = 0; i < views.Length; i++)
            {
                try
                {
                    flag.SetValue(views[i], false);
                    ((EditorWindow)views[i]).Repaint();
                }
                catch (Exception e) { Debug.Log("[FIEF] Gizmos : " + e.Message); }
            }
        }
    }
}
#endif
