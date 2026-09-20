using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Une zone de ressources dessinee a la main : un centre, un rayon, un type, un nombre
    /// de noeuds. Les positions exactes des noeuds sont tirees au hasard DANS la zone,
    /// avec une graine fixe (worldSeed) pour que la map soit la meme a chaque lancement.
    /// C'est la decision verrouillee du brief : "pseudo-aleatoire dans des zones fixes".
    /// </summary>
    [System.Serializable]
    public class ResourceZone
    {
        public string name = "Zone";
        public ResourceType type = ResourceType.Wood;
        public Vector2 center = Vector2.zero;
        public float radius = 22f;
        public int nodeCount = 8;
    }

    /// <summary>
    /// Tous les reglages du jeu, exposes dans l'Inspector Unity.
    /// Selectionne l'objet "FIEF (Bootstrap)" dans la Hierarchy pour les modifier,
    /// meme pendant que le jeu tourne (les valeurs reviennent a leur etat initial en sortant du Play).
    /// </summary>
    [DisallowMultipleComponent]
    public class GameConfig : MonoBehaviour
    {
        [Header("Monde")]
        [Tooltip("Cote de la carte en metres. Le brief dit 400x400.")]
        public float mapSize = 800f;
        [Tooltip("Graine du generateur aleatoire : meme graine = meme map.")]
        public int worldSeed = 1337;
        [Tooltip("Distance entre le marche central et chaque fief.")]
        public float fiefRingRadius = 270f;
        [Tooltip("Nombre d'emplacements de fief disposes en etoile (cible du brief : 6).")]
        public int fiefCount = 6;
        [Tooltip("Index du fief occupe par le joueur en solo.")]
        public int playerFiefIndex = 0;
        [Tooltip("Nombre de touffes de decor semees sur la carte (buissons, rochers, fleurs...).")]
        public int decorCount = 620;

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
        public float cameraDistance = 9f;
        public float cameraMinDistance = 3.5f;
        public float cameraMaxDistance = 24f;
        public float mouseSensitivity = 0.13f;

        [Header("Inventaire")]
        [Tooltip("Charge maximale en kg. Bois = 1 kg/u, Pierre = 2, Fer = 3.")]
        public float maxWeight = 60f;
        public int startingGold = 140;

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
        public float marketRadius = 20f;
        [Tooltip("Marge du marchand : tu achetes plus cher que tu ne vends.")]
        public float buySpread = 1.18f;
        [Tooltip("Vitesse a laquelle les stocks du marche reviennent a l'equilibre (unites/seconde).")]
        public float marketDriftPerSecond = 0.35f;
        [Tooltip("Sensibilite du prix au stock. Plus c'est haut, plus les prix bougent fort.")]
        public float priceElasticity = 0.62f;

        [Header("Zones de ressources (dessinees a la main)")]
        public List<ResourceZone> zones = DefaultZones();

        /// <summary>
        /// Le plan de la map v1. Tout est ici, en clair : deplace un centre, change un rayon,
        /// relance le jeu, la map a change. C'est le fichier que ton frere editera.
        /// </summary>
        public static List<ResourceZone> DefaultZones()
        {
            List<ResourceZone> list = new List<ResourceZone>();

            list.Add(NewZone("Futaie du Nord-Est", ResourceType.Wood, new Vector2(128f, 222f), 52f, 16));
            list.Add(NewZone("Carriere du Levant", ResourceType.Stone, new Vector2(252f, 0f), 44f, 13));
            list.Add(NewZone("Mine du Sud-Est", ResourceType.Iron, new Vector2(124f, -215f), 38f, 10));
            list.Add(NewZone("Bois du Sud-Ouest", ResourceType.Wood, new Vector2(-128f, -222f), 52f, 16));
            list.Add(NewZone("Eboulis du Couchant", ResourceType.Stone, new Vector2(-252f, -0f), 44f, 13));
            list.Add(NewZone("Veine du Nord-Ouest", ResourceType.Iron, new Vector2(-124f, 215f), 38f, 10));

            list.Add(NewZone("Bosquet du Levant", ResourceType.Wood, new Vector2(72f, 125f), 34f, 10));
            list.Add(NewZone("Bosquet du Midi", ResourceType.Wood, new Vector2(72f, -125f), 34f, 10));
            list.Add(NewZone("Bosquet du Ponant", ResourceType.Wood, new Vector2(-144f, -0f), 34f, 10));

            list.Add(NewZone("Grande Mine du Nord", ResourceType.Iron, new Vector2(330f, 0f), 40f, 11));
            list.Add(NewZone("Falaises du Sud", ResourceType.Stone, new Vector2(-169f, -293f), 42f, 12));
            list.Add(NewZone("Sylve Profonde", ResourceType.Wood, new Vector2(-169f, 293f), 50f, 15));

            return list;
        }

        static ResourceZone NewZone(string name, ResourceType type, Vector2 center, float radius, int count)
        {
            ResourceZone z = new ResourceZone();
            z.name = name;
            z.type = type;
            z.center = center;
            z.radius = radius;
            z.nodeCount = count;
            return z;
        }

        /// <summary>Position au sol du fief numero i, dispose en etoile autour du marche.</summary>
        public Vector3 FiefPosition(int index)
        {
            int count = Mathf.Max(1, fiefCount);
            float angle = (360f / count) * index * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle) * fiefRingRadius, 0f, Mathf.Cos(angle) * fiefRingRadius);
        }
    }
}
