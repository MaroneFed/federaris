using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA GARDE DU CHATEAU, en regles. C'est le differenciateur du jeu (voir
    /// CLAUDE.md) : "la meilleure facon d'entrer n'est pas la force, mais la
    /// trahison achetee".
    ///
    /// Chaque garde a une SOLDE promise, un ARRIERE (ce qu'on ne lui a pas paye) et
    /// une LOYAUTE. Plus il est loyal, plus il coute cher. Un garde paye pour
    /// regarder ailleurs ne te voit plus pendant trois minutes. Un garde paye pour
    /// OUVRIR LA POTERNE l'ouvre pour de bon : c'est la porte derobee, et elle ne
    /// s'ouvre QUE comme ca -- jamais par la force.
    ///
    /// Classe C# pure : l'or ne bouge que par Wallet.TrySpend, ici et nulle part
    /// ailleurs.
    /// </summary>
    public class GuardInfo
    {
        public readonly string Name;
        public readonly int Wage;             // deniers promis par jour
        public readonly int MonthsUnpaid;     // l'arriere
        public readonly float Loyalty;        // 0 : achetable pour rien ; 1 : incorruptible ou presque
        public float BribedUntil = -1f;

        /// <summary>A qui il a prete serment (null : au chateau, c'est-a-dire a personne).</summary>
        public Seeker SwornTo;

        /// <summary>Le prix de son SERMENT : il est a toi pour toute la Saison.</summary>
        public int OathPrice { get { return Mathf.RoundToInt(45f + 130f * Loyalty - MonthsUnpaid * 4f); } }

        public GuardInfo(string name, int wage, int monthsUnpaid, float loyalty)
        {
            Name = name;
            Wage = wage;
            MonthsUnpaid = monthsUnpaid;
            Loyalty = Mathf.Clamp01(loyalty);
        }

        public bool Bribed(float now) { return now < BribedUntil; }

        /// <summary>Le prix pour qu'il regarde ailleurs : un garde mal paye ne coute presque rien.</summary>
        public int LookAwayPrice { get { return Mathf.RoundToInt(10f + 55f * Loyalty - MonthsUnpaid * 2f); } }

        /// <summary>Le prix de la poterne : bien plus cher, c'est une trahison, pas une sieste.</summary>
        public int PosternePrice { get { return Mathf.RoundToInt(35f + 110f * Loyalty - MonthsUnpaid * 3f); } }
    }

    public class Garrison
    {
        public const float LookAwaySeconds = 180f;

        public readonly List<GuardInfo> Guards = new List<GuardInfo>();
        public bool PosterneOpen { get; private set; }
        public string PosterneOpenedBy { get; private set; }

        /// <summary>Iron confisque par la garde depuis le debut de la Saison (pour l'ecran de fin).</summary>
        public int IronSeized;

        public bool RequestLookAway(Wallet purse, GuardInfo guard, float now)
        {
            if (purse == null || guard == null || guard.Bribed(now)) return false;
            if (!purse.TrySpend(guard.LookAwayPrice)) return false;
            guard.BribedUntil = now + LookAwaySeconds;
            return true;
        }

        /// <summary>
        /// Acheter le serment d'un garde. Quand les six ont jure au meme chercheur,
        /// c'est la victoire par la Trahison.
        /// </summary>
        public bool RequestOath(Seeker buyer, GuardInfo guard)
        {
            if (buyer == null || guard == null || guard.SwornTo == buyer) return false;
            if (!buyer.Money.TrySpend(guard.OathPrice)) return false;
            guard.SwornTo = buyer;
            return true;
        }

        public int SwornCount(Seeker who)
        {
            int n = 0;
            for (int i = 0; i < Guards.Count; i++) if (Guards[i].SwornTo == who) n++;
            return n;
        }

        public bool RequestPosterne(Wallet purse, GuardInfo guard)
        {
            if (purse == null || guard == null || PosterneOpen) return false;
            if (!purse.TrySpend(guard.PosternePrice)) return false;
            PosterneOpen = true;
            PosterneOpenedBy = guard.Name;
            return true;
        }
    }
}
