using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// TOUT CE QUE LE JOUEUR POSSEDE DANS LE MONDE : son camp, ses caches, sa relique.
    ///
    /// Classe C# pure. Les objets du monde (la tente, les trous, la stele) la lisent
    /// et lui demandent de changer ; ils ne gardent rien pour eux. En Phase 3 il y
    /// aura un Hoard par joueur, et c'est lui que le serveur protegera.
    /// </summary>
    public class Hoard
    {
        public bool CampPlanted { get; private set; }
        public Vector3 CampPosition { get; private set; }

        public readonly List<Cache> Caches = new List<Cache>();
        public int MaxCaches = 3;
        public float CacheCapacity = 40f;

        /// <summary>Nulle tant que le mage n'a rien forge.</summary>
        public Relic Relic { get; private set; }
        public bool RelicOnStele { get; private set; }

        public bool CanDig { get { return Caches.Count < MaxCaches; } }
        public bool RelicInHand { get { return Relic != null && !RelicOnStele; } }

        /// <summary>
        /// Le camp ne se plante qu'une fois. Le choix de son emplacement est une
        /// vraie decision : c'est la qu'on reviendra, et c'est la qu'on sera trouve.
        /// </summary>
        public bool TryPlantCamp(Vector3 at)
        {
            if (CampPlanted) return false;
            CampPlanted = true;
            CampPosition = at;
            return true;
        }

        public Cache TryDig(Vector3 at)
        {
            if (!CanDig) return null;
            Cache cache = new Cache(at, Caches.Count + 1, CacheCapacity);
            Caches.Add(cache);
            return cache;
        }

        /// <summary>Premiere forge : la relique nait. Les suivantes la renforcent.</summary>
        public Relic EnsureRelic()
        {
            if (Relic == null) Relic = new Relic();
            return Relic;
        }

        public bool TryPlaceOnStele()
        {
            if (!RelicInHand) return false;
            RelicOnStele = true;
            return true;
        }

        public bool TryTakeFromStele()
        {
            if (Relic == null || !RelicOnStele) return false;
            RelicOnStele = false;
            return true;
        }

        /// <summary>Le score de fin : seule compte une relique POSEE sur la stele.</summary>
        public int FinalScore
        {
            get { return Relic != null && RelicOnStele ? Relic.Power : 0; }
        }
    }
}
