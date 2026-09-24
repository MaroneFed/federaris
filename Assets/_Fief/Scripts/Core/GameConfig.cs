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
        [Header("Monde")]
        [Tooltip("Cote de la carte en metres. 700 m se traverse en 90 s a pied -- " +
                 "mais avec 20 m de visibilite, on n'en a jamais l'impression.")]
        public float mapSize = 700f;
        [Tooltip("Graine du generateur aleatoire : meme graine = meme map.")]
        public int worldSeed = 1337;

        [Header("La sylve")]
        [Tooltip("Distance a laquelle la brume efface tout, en metres. C'est le reglage " +
                 "le plus important du jeu : il decide de l'enfermement. A 20 m un tronc " +
                 "est a moitie efface a 10 m -- on ne voit jamais ce qu'il y a deux arbres " +
                 "plus loin. Descends a 15 pour etouffer, remonte a 30 pour respirer.")]
        public float sightDistance = 20f;

        [Tooltip("Couleur de la brume et du fond. Gris-vert, et PLUS CLAIRE que les " +
                 "troncs proches : c'est ce qui les decoupe en silhouettes. Une brume plus " +
                 "sombre que les arbres donne un vide noir, pas une foret.")]
        public Color hazeColor = new Color(0.17f, 0.19f, 0.17f);

        [Tooltip("Hauteur de la lumiere au-dessus de l'horizon, en degres. Sous un couvert " +
                 "la lumiere tombe d'en haut : en dessous de 35 elle eclaire les troncs de " +
                 "cote, comme un projecteur, et plus rien n'a l'air naturel.")]
        public float sunElevation = 52f;

        [Tooltip("Force de la lumiere du ciel. Faible : c'est un temps couvert.")]
        public float sunIntensity = 0.45f;

        [Tooltip("Force de la lanterne que tu portes. Sans elle, sombre veut dire " +
                 "'on ne voit rien' et le jeu devient penible. Trop forte, elle repeint " +
                 "la foret en orange.")]
        public float lampIntensity = 1.0f;

        [Tooltip("Portee de la lanterne, en metres.")]
        public float lampRange = 13f;

        [Tooltip("Ecart moyen entre deux emplacements d'arbre, en metres. Plus petit = " +
                 "plus dense, mais aussi plus long a construire.")]
        public float treeSpacing = 4.6f;

        [Tooltip("Proportion des emplacements reellement plantes, module par le couvert. " +
                 "Monte-le pour un fourre, descends-le pour une futaie claire.")]
        public float treeDensity = 0.80f;

        [Tooltip("Densite des touffes et blocs au sol. Ils poussent la ou le couvert " +
                 "s'ouvre, donc ils remplissent les clairieres au lieu de les vider.")]
        public float undergrowthDensity = 0.55f;

        [Header("Deplacement")]
        public float moveSpeedEmpty = 7.6f;
        [Tooltip("Vitesse a 100% de charge. Volontairement PROCHE de la vitesse a vide : "
               + "etre charge doit couter, pas enliser. Le vrai cout est sur les gestes (voir plus bas).")]
        public float moveSpeedFull = 5.6f;
        [Tooltip("Courbure du ralentissement : 1 = lineaire, >1 = on ne sent la charge que tard.")]
        public float loadCurve = 1.15f;
        [Tooltip("Vitesse x N en courant.")]
        public float sprintMultiplier = 1.55f;
        [Tooltip("Charge (0-1) au-dela de laquelle on ne peut plus courir. Un joueur tres charge "
               + "reste donc rattrapable : c'est ce qui le rend vulnerable en Phase 2.")]
        public float sprintMaxLoad = 0.75f;
        public float turnSpeed = 720f;
        public float jumpSpeed = 5.0f;
        public float gravity = -22f;

        [Header("Camera")]
        [Tooltip("Hauteur des yeux, en metres.")]
        public float eyeHeight = 1.78f;
        [Tooltip("Avancee des yeux. Doit rester sous le rayon de l'encolure du poncho (0,145) "
               + "pour qu'on voie le tissu autour de soi en baissant les yeux.")]
        public float eyeForward = 0.13f;
        [Tooltip("Amplitude du balancement de tete a la marche.")]
        public float headBob = 0.045f;
        public float cameraDistance = 10f;
        public float cameraMinDistance = 3.5f;
        public float cameraMaxDistance = 32f;
        public float mouseSensitivity = 0.13f;

        [Header("Inventaire")]
        [Tooltip("Charge maximale en kg. Bois mort = 1 kg, Fer ancien = 2, Pierre-lune = 3, la relique = 5.")]
        public float maxWeight = 60f;
        public int startingGold = 200;

        [Header("Recolte")]
        public float interactRadius = 3.6f;
        [Tooltip("Duree d'un coup, sac VIDE.")]
        public float harvestDuration = 1.15f;
        [Tooltip("Multiplicateur de duree quand le sac est PLEIN. C'est la nouvelle mecanique de "
               + "poids : plus tu es charge, plus tes gestes sont lourds et lents. "
               + "Tu restes mobile, mais tu deviens lent a l'ouvrage.")]
        public float actionPenaltyFull = 2.4f;
        public int harvestYield = 2;
        public int nodeCapacity = 30;
        public float nodeRespawnDelay = 55f;

        [Header("La Saison (voir docs/LA-SAISON.md)")]
        [Tooltip("Duree d'une Saison, en minutes. A la cloche, seule compte la relique posee sur la stele.")]
        public float seasonMinutes = 30f;
        [Tooltip("Premiere apparition du mage, en secondes. Assez tot pour qu'on le rencontre " +
                 "avant d'avoir oublie qu'il existe.")]
        public float mageFirstAppearance = 120f;
        [Tooltip("Ecart entre deux apparitions, en secondes. Le rater, c'est attendre ca.")]
        public float mageInterval = 270f;
        [Tooltip("Duree d'une apparition, en secondes. Il faut le trouver avant qu'il parte.")]
        public float mageStay = 150f;
        [Tooltip("Le mage apparait au moins a cette distance de toi : il faut marcher.")]
        public float mageMinDistance = 110f;
        [Tooltip("Et au plus a celle-ci : il doit rester atteignable dans le temps imparti.")]
        public float mageMaxDistance = 260f;
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
