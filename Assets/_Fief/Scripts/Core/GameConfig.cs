using UnityEngine;

namespace Fief
{

    /// <summary>
    /// Tous les reglages du jeu, exposes dans l'Inspector Unity.
    /// Selectionne l'objet "FIEF (Bootstrap)" dans la Hierarchy pour les modifier,
    /// meme pendant que le jeu tourne (les valeurs reviennent a leur etat initial en sortant du Play).
    /// </summary>
    [DisallowMultipleComponent]
    public class GameConfig : MonoBehaviour
    {
        /// <summary>
        /// Unity ENREGISTRE les valeurs de l'Inspector dans la scene. Si la scene a ete
        /// sauvegardee avec d'anciens reglages (une carte de 700 m...), ils l'emportent
        /// sur le code, sans rien dire. Decoche (par defaut) : au lancement, on remet
        /// les valeurs du code. Coche : on garde celles de l'Inspector.
        /// </summary>
        [Tooltip("Coché : garder les valeurs réglées dans l'Inspector. Décoché : le code fait foi à chaque lancement.")]
        public bool keepInspectorValues;

        /// <summary>Remet toutes les valeurs de ce composant a celles ecrites dans le code.</summary>
        public static void RestoreDefaults(GameConfig target)
        {
            GameObject temp = new GameObject("reglages par defaut");
            temp.SetActive(false);
            GameConfig fresh = temp.AddComponent<GameConfig>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), target);
            Destroy(temp);
        }

        [Header("Monde")]
        [Tooltip("Côté de l'espace de jeu en mètres (l'île fait ~200 m, les îlots flottent " +
                 "jusqu'à 175 m du centre). Au-delà, on est ramené dedans.")]
        public float mapSize = 440f;
        [Tooltip("Graine du générateur aléatoire : même graine = même île, mêmes îlots.")]
        public int worldSeed = 1337;

        [Header("Le ciel (27/09 : l'île flottante)")]
        [Tooltip("Distance à laquelle la brume efface tout, en mètres. Grande : on doit voir " +
                 "les planeurs, les îlots et la tour de partout.")]
        public float sightDistance = 320f;

        [Tooltip("Couleur de la brume à l'horizon : l'or du soir, comme le ciel.")]
        public Color hazeColor = new Color(0.78f, 0.66f, 0.58f);

        [Tooltip("Hauteur du soleil au-dessus de l'horizon, en degrés. Bas : lumière dorée, " +
                 "longues ombres, la tour se découpe.")]
        public float sunElevation = 20f;

        [Tooltip("Force du soleil.")]
        public float sunIntensity = 1.15f;

        [Tooltip("Force de la lanterne que tu portes (discrète en plein jour).")]
        public float lampIntensity = 0.5f;

        [Tooltip("Portée de la lanterne, en mètres.")]
        public float lampRange = 10f;

        [Header("Déplacement")]
        [Tooltip("Vitesse de marche, en m/s (le pouvoir Coureur ajoute 15 %).")]
        public float moveSpeed = 7.2f;
        [Tooltip("Vitesse x N en courant (Maj).")]
        public float sprintMultiplier = 1.5f;
        public float turnSpeed = 720f;
        [Tooltip("Vitesse du saut. 7 m/s avec une gravité de 22 : un saut d'1,1 m -- " +
                 "assez pour une caisse, pas pour un mur. Le Double saut double la mise.")]
        public float jumpSpeed = 7.0f;
        public float gravity = -22f;

        [Header("Caméra")]
        [Tooltip("Hauteur des yeux, en mètres.")]
        public float eyeHeight = 1.78f;
        [Tooltip("Avancée des yeux. Doit rester sous le rayon de l'encolure du poncho (0,145) "
               + "pour qu'on voie le tissu autour de soi en baissant les yeux.")]
        public float eyeForward = 0.13f;
        [Tooltip("Amplitude du balancement de tête à la marche.")]
        public float headBob = 0.022f;          // divise par deux : le balancement donnait mal a la tete
        public float cameraDistance = 10f;
        public float cameraMinDistance = 3.5f;
        public float cameraMaxDistance = 32f;
        public float mouseSensitivity = 0.13f;

        [Header("Interaction")]
        [Tooltip("Portée de la touche E (sanctuaires, Couronne, Monument), en mètres.")]
        public float interactRadius = 3.6f;

        [Header("Le match (voir docs/LA-SAISON.md)")]
        [Tooltip("Durée d'une manche quand on lance la scène sans passer par le salon, en minutes.")]
        public float seasonMinutes = 6f;
    }
}
