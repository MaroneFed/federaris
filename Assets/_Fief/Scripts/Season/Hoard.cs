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

        // ------------------------------------------------------------------ l'offrande

        /// <summary>Ce qui a deja ete depose au Registre, par ressource.</summary>
        public readonly int[] Offered = new int[ResourceInfo.Count];

        public bool OfferingComplete
        {
            get
            {
                for (int i = 0; i < Offered.Length; i++) if (Offered[i] < Victories.Offering[i]) return false;
                return true;
            }
        }

        /// <summary>
        /// Deposer au Registre ce qu'on porte, jusqu'a ce qui manque encore. Par
        /// Inventory.TryRemove, comme toujours. Renvoie le nombre d'unites deposees.
        /// </summary>
        public int RequestOffer(Inventory bag)
        {
            if (bag == null) return 0;
            int total = 0;
            for (int i = 0; i < Offered.Length; i++)
            {
                int missing = Victories.Offering[i] - Offered[i];
                if (missing <= 0) continue;
                int moved = bag.TryRemove((ResourceType)i, Mathf.Min(missing, bag.Get((ResourceType)i)));
                Offered[i] += moved;
                total += moved;
            }
            return total;
        }

        // ------------------------------------------------------------------ l'infusion

        public const int BrewCost = 12;
        public const float BrewSeconds = 180f;

        /// <summary>Heure de la Saison (Season.Elapsed) a laquelle l'infusion cesse.</summary>
        public float BrewUntil { get; private set; }

        public bool BrewActive(float now) { return now < BrewUntil; }

        /// <summary>
        /// L'infusion de l'Ermite : douze bois mort, et pendant trois minutes le poids
        /// du sac ne ralentit plus les gestes. C'est ce qui donne une vraie valeur au
        /// bois mort, la ressource la plus commune.
        /// Le bois sort du sac par Inventory.TryRemove : rien ne touche un inventaire
        /// autrement.
        /// </summary>
        public bool RequestBrew(Inventory bag, float now)
        {
            if (bag == null || BrewActive(now)) return false;
            if (bag.Get(ResourceType.Deadwood) < BrewCost) return false;
            if (bag.TryRemove(ResourceType.Deadwood, BrewCost) < BrewCost) return false;
            BrewUntil = now + BrewSeconds;
            return true;
        }

        /// <summary>Premiere forge : la relique nait. Les suivantes la renforcent.</summary>
        public Relic EnsureRelic()
        {
            if (Relic == null) Relic = new Relic();
            return Relic;
        }

        // ------------------------------------------------------------------ la stele

        /// <summary>
        /// CHACUN SA STELE (decide par Martin le 24/09/2026). On la plante UNE fois,
        /// ou l'on veut. Seule une relique posee sur SA stele compte a la cloche. Et
        /// une stele se voit : qui la trouve peut voler ce qu'il y a dessus.
        /// </summary>
        public bool StelePlanted { get; private set; }
        public Vector3 StelePosition { get; private set; }

        public bool TryPlantStele(Vector3 at)
        {
            if (StelePlanted) return false;
            StelePlanted = true;
            StelePosition = at;
            return true;
        }

        public bool TryPlaceOnStele()
        {
            if (!RelicInHand || !StelePlanted) return false;
            RelicOnStele = true;
            return true;
        }

        // ------------------------------------------------------------------ le vol

        /// <summary>Poids d'une relique volee qu'on porte : lourde, pour qu'on puisse te rattraper.</summary>
        public const float TrophyWeight = 8f;
        public const float StolenShare = 0.6f;

        /// <summary>Une relique volee, qu'on porte vers sa propre stele. Nulle sinon.</summary>
        public Relic Trophy { get; private set; }
        /// <summary>A qui on l'a prise.</summary>
        public Seeker TrophyFrom { get; private set; }

        /// <summary>Ce qu'on porte en plus du sac : sa relique en main, et un trophee.</summary>
        public float CarriedWeight
        {
            get
            {
                float w = RelicInHand ? Relic.Weight : 0f;
                if (Trophy != null) w += TrophyWeight;
                return w;
            }
        }

        /// <summary>
        /// Se faire prendre sa relique : sur la stele, ou dans les mains si l'on se
        /// fait rattraper. Elle quitte ce Hoard entierement.
        /// </summary>
        public Relic TrySurrenderRelic()
        {
            if (Relic == null) return null;
            Relic taken = Relic;
            Relic = null;
            RelicOnStele = false;
            return taken;
        }

        /// <summary>Lacher le trophee qu'on porte (rattrape par son proprietaire).</summary>
        public Relic TrySurrenderTrophy()
        {
            Relic taken = Trophy;
            Trophy = null;
            TrophyFrom = null;
            return taken;
        }

        /// <summary>Ramasser une relique prise a quelqu'un. Un seul trophee a la fois.</summary>
        public bool TryTakeTrophy(Relic relic, Seeker from)
        {
            if (relic == null || Trophy != null) return false;
            Trophy = relic;
            TrophyFrom = from;
            return true;
        }

        /// <summary>
        /// Reprendre SA relique a un voleur : elle revient en main, entiere. Si l'on
        /// en avait deja reforge une autre entre-temps, la volee s'y fond sans perte.
        /// </summary>
        public bool TryRecover(Relic mine)
        {
            if (mine == null) return false;
            if (Relic == null) { Relic = mine; RelicOnStele = false; return true; }
            Relic.Absorb(mine, 1f);
            return true;
        }

        /// <summary>
        /// A sa stele, fondre le trophee dans sa relique (60 %). S'il n'y a pas
        /// encore de relique, le trophee en devient une -- amputee de 40 %.
        /// Renvoie la puissance gagnee.
        /// </summary>
        public int RequestAbsorbTrophy()
        {
            if (Trophy == null) return 0;
            Relic stolen = Trophy;
            Trophy = null;
            TrophyFrom = null;
            bool fresh = Relic == null;
            Relic mine = EnsureRelic();
            int gained = mine.Absorb(stolen, StolenShare);
            if (fresh) RelicOnStele = false;
            return gained;
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
