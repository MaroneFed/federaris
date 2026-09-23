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
                 "mais avec 26 m de visibilite, on n'en a jamais l'impression.")]
        public float mapSize = 700f;
        [Tooltip("Graine du generateur aleatoire : meme graine = meme map.")]
        public int worldSeed = 1337;

        [Header("La sylve")]
        [Tooltip("Distance a laquelle la brume efface tout, en metres. C'est le reglage " +
                 "le plus important du jeu : il decide de l'enfermement. A 26 m un tronc " +
                 "est a moitie efface a 13 m -- on ne voit jamais ce qu'il y a deux arbres " +
                 "plus loin. Descends a 18 pour etouffer, remonte a 40 pour respirer.")]
        public float sightDistance = 26f;

        [Tooltip("Couleur de la brume et du fond. Bleu-vert tres sombre : une brume " +
                 "grise a l'air d'un bug de rendu, une brume teintee a l'air d'un lieu.")]
        public Color hazeColor = new Color(0.10f, 0.12f, 0.13f);

        [Tooltip("Force de la lumiere rasante. Elle ne sert pas a eclairer mais a decouper.")]
        public float sunIntensity = 0.5f;

        [Tooltip("Force de la lanterne que tu portes. Sans elle, sombre veut dire " +
                 "'on ne voit rien' et le jeu devient penible.")]
        public float lampIntensity = 1.35f;

        [Tooltip("Portee de la lanterne, en metres.")]
        public float lampRange = 15f;

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
        [Tooltip("Charge maximale en kg. Bois = 1 kg/u, Pierre = 2, Fer = 3.")]
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

        [Header("Marche")]
        public float marketRadius = 30f;
        [Tooltip("Marge du marchand : tu achetes plus cher que tu ne vends.")]
        public float buySpread = 1.18f;
        [Tooltip("Temps qu'il faut a un prix casse pour remonter a mi-chemin, en secondes. " +
                 "Plus c'est long, plus brader une cargaison coute cher, et plus on a interet " +
                 "a changer de ressource. 300 s = 5 minutes.")]
        public float marketRecoveryHalfLife = 300f;
        [Tooltip("Sensibilite du prix au stock. Plus c'est haut, plus les prix bougent fort.")]
        public float priceElasticity = 0.62f;

    }
}
