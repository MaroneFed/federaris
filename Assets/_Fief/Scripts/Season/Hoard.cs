using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// TOUT CE QU'UN CHERCHEUR POSSEDE DANS LE MONDE : sa stele, son butin, son camp,
    /// ses caches.
    ///
    /// LE BUTIN (26/09, Martin : "tu recoltes du bois, tu sais meme pas pourquoi").
    /// Il n'y a plus qu'UN but : avoir le plus de butin -- des etoiles, ★ -- dans sa
    /// stele quand la cloche sonne. Le butin vient :
    ///   - de la pierre-lune (★2 chacune, dans les creux de la foret) ;
    ///   - des tresors du chateau (calices, coffrets, et la couronne tout en haut) ;
    ///   - des coffres caches aux lieux-dits ;
    ///   - des Autels qu'on tient ;
    ///   - et de la stele des autres, qu'on pille.
    ///
    /// Le butin qu'on PORTE ne compte pas encore : il pese, on le lache si on tombe,
    /// et on peut se le faire prendre. Il ne compte qu'une fois DEPOSE a sa stele.
    ///
    /// Classe C# pure. Les objets du monde la lisent et lui demandent de changer ;
    /// ils ne gardent rien pour eux. En Phase 3 il y aura un Hoard par joueur, et
    /// c'est lui que le serveur protegera.
    /// </summary>
    public class Hoard
    {
        /// <summary>Une pierre-lune deposee vaut deux etoiles.</summary>
        public const int MoonstoneStars = 2;
        /// <summary>Le butin pese : 350 g l'etoile. La couronne (★40) pese 14 kg.</summary>
        public const float KiloPerStar = 0.35f;
        /// <summary>Piller une stele emporte la moitie de ce qui y dort.</summary>
        public const float PillageShare = 0.5f;

        // ------------------------------------------------------------------ le butin

        /// <summary>Le butin qu'on porte sur soi : il ne compte pas encore.</summary>
        public int Carried { get; private set; }

        /// <summary>Le butin depose a sa stele : c'est le score.</summary>
        public int Banked { get; private set; }

        public float CarriedWeight { get { return Carried * KiloPerStar; } }

        /// <summary>Ramasser un tresor (ou une part de pillage). Renvoie les etoiles prises.</summary>
        public int TryPickLoot(int stars)
        {
            if (stars <= 0) return 0;
            Carried += stars;
            return stars;
        }

        /// <summary>
        /// A SA stele : tout le butin porte y entre, et chaque pierre-lune du sac
        /// devient deux etoiles. Le bois et le fer restent dans le sac : ils servent a
        /// construire (touche T). Renvoie les etoiles deposees.
        /// </summary>
        public int RequestBank(Inventory bag)
        {
            int stars = Carried;
            Carried = 0;
            if (bag != null)
            {
                int moon = bag.TryRemove(ResourceType.Moonstone, bag.Get(ResourceType.Moonstone));
                stars += moon * MoonstoneStars;
            }
            Banked += stars;
            return stars;
        }

        /// <summary>Un Autel verse son du directement dans la stele de celui qui le tient.</summary>
        public void RequestTribute(int stars)
        {
            if (stars > 0 && StelePlanted) Banked += stars;
        }

        /// <summary>
        /// Piller la stele d'un autre : on emporte la moitie de ce qui y dort (arrondie
        /// au-dessus). Renvoie les etoiles prises.
        /// </summary>
        public int RequestPillage(Hoard victim)
        {
            if (victim == null || victim == this || victim.Banked <= 0) return 0;
            int take = Mathf.CeilToInt(victim.Banked * PillageShare);
            victim.Banked -= take;
            Carried += take;
            return take;
        }

        /// <summary>Tomber : tout le butin porte tombe dans la depouille.</summary>
        public int DropCarried()
        {
            int n = Carried;
            Carried = 0;
            return n;
        }

        // ------------------------------------------------------------------ la stele

        /// <summary>
        /// CHACUN SA STELE, fixe, tiree au hasard a chaque partie ; on nait a cote.
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

        // ------------------------------------------------------------------ camp et caches

        public bool CampPlanted { get; private set; }
        public Vector3 CampPosition { get; private set; }

        /// <summary>Ce que contient la tente : plus grande qu'une cache, mais visible.</summary>
        public Cache CampStash { get; private set; }
        public float CampCapacity = 60f;

        public readonly List<Cache> Caches = new List<Cache>();
        public int MaxCaches = 3;
        public float CacheCapacity = 40f;

        public bool CanDig { get { return Caches.Count < MaxCaches; } }

        /// <summary>Le camp ne se plante qu'une fois.</summary>
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
    }
}
