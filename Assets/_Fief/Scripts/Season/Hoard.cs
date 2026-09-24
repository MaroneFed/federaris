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

        /// <summary>
        /// Ce que contient la tente. Plus grande qu'une cache, mais VISIBLE : une
        /// tente se voit, une butte de terre non. En solo ca ne change rien ; en
        /// Phase 2, c'est toute la difference entre ce qu'on vole et ce qu'on trouve.
        /// </summary>
        public Cache CampStash { get; private set; }
        public float CampCapacity = 60f;

        public readonly List<Cache> Caches = new List<Cache>();
        public int MaxCaches = 3;
        public float CacheCapacity = 40f;

        /// <summary>Nulle tant que le mage n'a rien forge.</summary>
        public Relic Relic { get; private set; }
        public bool RelicOnStele { get; private set; }

        readonly bool[] talismans = new bool[TalismanInfo.Count];

        public bool CanDig { get { return Caches.Count < MaxCaches; } }

        public bool Has(Talisman t) { return talismans[(int)t]; }

        public int TalismanCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < talismans.Length; i++) if (talismans[i]) n++;
                return n;
            }
        }

        /// <summary>
        /// Prendre un talisman. Une seule fois chacun : en Phase 3, le premier arrive
        /// l'emporte, et c'est le serveur qui tranche ici.
        /// La Pelle d'os donne une cache de plus : c'est ici, pas dans l'objet du
        /// monde, que la regle change.
        /// </summary>
        public bool TryTakeTalisman(Talisman t)
        {
            if (talismans[(int)t]) return false;
            talismans[(int)t] = true;
            if (t == Talisman.Pelle) MaxCaches++;
            return true;
        }
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
            CampStash = new Cache(at, 0, CampCapacity);
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

        /// <summary>
        /// Le score de fin : seule compte une relique POSEE sur la stele. La Couronne
        /// sans tete la majore de 15 %.
        /// </summary>
        public int FinalScore
        {
            get
            {
                if (Relic == null || !RelicOnStele) return 0;
                float bonus = Has(Talisman.Couronne) ? TalismanInfo.CouronneBonus : 1f;
                return Mathf.RoundToInt(Relic.Power * bonus);
            }
        }
    }
}
