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
        public float mapSize = 2200f;
        [Tooltip("Graine du generateur aleatoire : meme graine = meme map.")]
        public int worldSeed = 1337;
        [Tooltip("Distance entre le marche central et chaque fief.")]
        public float fiefRingRadius = 750f;
        [Tooltip("Nombre d'emplacements de fief disposes en etoile (cible du brief : 6).")]
        public int fiefCount = 6;
        [Tooltip("Index du fief occupe par le joueur en solo.")]
        public int playerFiefIndex = 0;
        [Tooltip("Nombre de touffes de decor semees sur la carte (buissons, rochers, fleurs...).")]
        public int decorCount = 3600;
        [Tooltip("Arbres decoratifs semes en bosquets sur toute la carte.")]
        public int forestCount = 5200;
        [Tooltip("Afficher les 6 fiefs rivaux. En solo ils ne servent a rien : laisse decoche.")]
        public bool showRivalFiefs = false;
        [Tooltip("Lieux a decouvrir : moulins, chapelles, camps, mines effondrees, fermes en ruine, postes de guet.")]
        public int placeCount = 22;
        [Tooltip("Caisses a fouiller semees loin de tout.")]
        public int lootCount = 110;
        [Tooltip("Troupeaux de cerfs et de moutons.")]
        public int herdCount = 26;

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
        [Tooltip("Demarrer en vue a la premiere personne. V bascule en jeu.")]
        public bool firstPerson = true;
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

            list.Add(NewZone("Futaie du Nord-Est", ResourceType.Wood, new Vector2(350f, 606f), 92f, 26));
            list.Add(NewZone("Carriere du Levant", ResourceType.Stone, new Vector2(690f, 0f), 78f, 20));
            list.Add(NewZone("Mine du Sud-Est", ResourceType.Iron, new Vector2(340f, -589f), 66f, 16));
            list.Add(NewZone("Bois du Sud-Ouest", ResourceType.Wood, new Vector2(-350f, -606f), 92f, 26));
            list.Add(NewZone("Eboulis du Couchant", ResourceType.Stone, new Vector2(-690f, -0f), 78f, 20));
            list.Add(NewZone("Veine du Nord-Ouest", ResourceType.Iron, new Vector2(-340f, 589f), 66f, 16));

            list.Add(NewZone("Bosquet du Levant", ResourceType.Wood, new Vector2(200f, 346f), 60f, 16));
            list.Add(NewZone("Bosquet du Midi", ResourceType.Wood, new Vector2(200f, -346f), 60f, 16));
            list.Add(NewZone("Bosquet du Ponant", ResourceType.Wood, new Vector2(-400f, -0f), 60f, 16));

            list.Add(NewZone("Grande Mine du Nord", ResourceType.Iron, new Vector2(950f, 0f), 72f, 18));
            list.Add(NewZone("Falaises du Sud", ResourceType.Stone, new Vector2(-480f, -831f), 76f, 20));
            list.Add(NewZone("Sylve Profonde", ResourceType.Wood, new Vector2(-480f, 831f), 88f, 24));

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
