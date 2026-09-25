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
        [Tooltip("Côté de la carte en mètres. 420 m se traverse en une minute à pied -- " +
                 "assez petit pour s'y retrouver, assez grand pour s'y perdre dans la brume.")]
        public float mapSize = 420f;
        [Tooltip("Graine du générateur aléatoire : même graine = même map.")]
        public int worldSeed = 1337;

        [Header("La sylve")]
        [Tooltip("Distance à laquelle la brume efface tout, en mètres. C'est le réglage " +
                 "le plus important du jeu : il décide de l'enfermement. À 14 m un tronc " +
                 "est à moitié effacé à 7 m -- on ne voit jamais ce qu'il y a deux arbres " +
                 "plus loin. Remonte à 20 ou 30 pour respirer.")]
        public float sightDistance = 14f;

        [Tooltip("Couleur de la brume et du fond. Gris-vert, et PLUS CLAIRE que les " +
                 "troncs proches : c'est ce qui les découpe en silhouettes. Une brume plus " +
                 "sombre que les arbres donne un vide noir, pas une forêt.")]
        public Color hazeColor = new Color(0.17f, 0.19f, 0.17f);

        [Tooltip("Hauteur de la lumière au-dessus de l'horizon, en degrés. Sous un couvert " +
                 "la lumière tombe d'en haut : en dessous de 35 elle éclaire les troncs de " +
                 "côté, comme un projecteur, et plus rien n'a l'air naturel.")]
        public float sunElevation = 52f;

        [Tooltip("Force de la lumière du ciel. Faible : c'est un temps couvert.")]
        public float sunIntensity = 0.45f;

        [Tooltip("Force de la lanterne que tu portes. Sans elle, sombre veut dire " +
                 "'on ne voit rien' et le jeu devient pénible. Trop forte, elle repeint " +
                 "la forêt en orange.")]
        public float lampIntensity = 1.0f;

        [Tooltip("Portée de la lanterne, en mètres.")]
        public float lampRange = 13f;

        [Tooltip("Écart moyen entre deux emplacements d'arbre, en mètres. Plus petit = " +
                 "plus dense, mais aussi plus long à construire.")]
        public float treeSpacing = 4.6f;

        [Tooltip("Proportion des emplacements réellement plantes, module par le couvert. " +
                 "Monte-le pour un fourre, descends-le pour une futaie claire.")]
        public float treeDensity = 0.80f;

        [Tooltip("Densité des touffes et blocs au sol. Ils poussent là où le couvert " +
                 "s'ouvre, donc ils remplissent les clairières au lieu de les vider.")]
        public float undergrowthDensity = 0.55f;

        [Header("Déplacement")]
        public float moveSpeedEmpty = 7.6f;
        [Tooltip("Vitesse à 100% de charge. Volontairement PROCHE de la vitesse à vide : "
               + "être charge doit coûter, pas enliser. Le vrai coût est sur les gestes (voir plus bas).")]
        public float moveSpeedFull = 5.6f;
        [Tooltip("Courbure du ralentissement : 1 = linéaire, >1 = on ne sent la charge que tard.")]
        public float loadCurve = 1.15f;
        [Tooltip("Vitesse x N en courant.")]
        public float sprintMultiplier = 1.55f;
        [Tooltip("Charge (0-1) au-delà de laquelle on ne peut plus courir. Un joueur très charge "
               + "reste donc rattrapable : c'est ce qui le rend vulnérable en Phase 2.")]
        public float sprintMaxLoad = 0.75f;
        public float turnSpeed = 720f;
        public float jumpSpeed = 5.0f;
        public float gravity = -22f;

        [Header("Caméra")]
        [Tooltip("Hauteur des yeux, en mètres.")]
        public float eyeHeight = 1.78f;
        [Tooltip("Avancée des yeux. Doit rester sous le rayon de l'encolure du poncho (0,145) "
               + "pour qu'on voie le tissu autour de soi en baissant les yeux.")]
        public float eyeForward = 0.13f;
        [Tooltip("Amplitude du balancement de tête à la marche.")]
        public float headBob = 0.045f;
        public float cameraDistance = 10f;
        public float cameraMinDistance = 3.5f;
        public float cameraMaxDistance = 32f;
        public float mouseSensitivity = 0.13f;

        [Header("Inventaire")]
        [Tooltip("Charge maximale en kg. Bois mort = 1 kg, Fer ancien = 2, Pierre-lune = 3, la relique = 5.")]
        public float maxWeight = 60f;
        [Tooltip("L'or au départ. Il ne sert qu'a SOUDOYER les gardes du château. On en trouve " +
                 "d'autre dans les bourses perdues de la forêt.")]
        public int startingGold = 40;

        [Header("Récolte")]
        public float interactRadius = 3.6f;
        [Tooltip("Durée d'un coup, sac VIDE.")]
        public float harvestDuration = 1.15f;
        [Tooltip("Multiplicateur de durée quand le sac est PLEIN. C'est la nouvelle mécanique de "
               + "poids : plus tu es charge, plus tes gestes sont lourds et lents. "
               + "Tu restes mobile, mais tu deviens lent à l'ouvrage.")]
        public float actionPenaltyFull = 2.4f;
        public int harvestYield = 2;
        public int nodeCapacity = 30;
        public float nodeRespawnDelay = 55f;

        [Header("La Saison (voir docs/LA-SAISON.md)")]
        [Tooltip("Durée d'une Saison, en minutes. À la cloche, seule compte la relique posée sur la stèle.")]
        public float seasonMinutes = 30f;
        [Tooltip("Première apparition du mage, en secondes. Assez tôt pour qu'on le rencontre " +
                 "avant d'avoir oublié qu'il existe.")]
        public float mageFirstAppearance = 120f;
        [Tooltip("Écart entre deux apparitions, en secondes. Le rater, c'est attendre ça.")]
        public float mageInterval = 270f;
        [Tooltip("Durée d'une apparition, en secondes. Il faut le trouver avant qu'il parte.")]
        public float mageStay = 150f;
        [Tooltip("Le mage apparaît au moins à cette distance de toi : il faut marcher.")]
        public float mageMinDistance = 60f;
        [Tooltip("Et au plus à celle-ci : il doit rester atteignable dans le temps imparti.")]
        public float mageMaxDistance = 150f;
        [Tooltip("Ce que la tente du camp peut contenir, en kg. Plus qu'une cache, mais une " +
                 "tente se voit.")]
        public float campCapacity = 60f;
        [Tooltip("Nombre de caches qu'on peut creuser dans une Saison.")]
        public int maxCaches = 3;
        [Tooltip("Ce qu'une cache peut contenir, en kg.")]
        public float cacheCapacity = 40f;
        [Tooltip("Temps pour creuser une cache, en secondes (touche maintenue).")]
        public float digDuration = 3.5f;

    }
}
