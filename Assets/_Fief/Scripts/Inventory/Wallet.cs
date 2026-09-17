using System;

namespace Fief
{
    /// <summary>
    /// L'Or, monnaie unique du jeu.
    ///
    /// Le solde est en lecture seule de l'exterieur : on ne peut le modifier que par
    /// Add / TrySpend. C'est volontaire. En Phase 3, seul l'hote executera ces deux
    /// methodes ; les clients ne feront qu'afficher une valeur repliquee.
    /// </summary>
    public class Wallet
    {
        public event Action Changed;

        public int Gold { get; private set; }

        public Wallet(int startingGold)
        {
            Gold = startingGold;
        }

        public bool CanAfford(int amount)
        {
            return Gold >= amount;
        }

        public void Add(int amount)
        {
            if (amount == 0) return;
            Gold += amount;
            if (Gold < 0) Gold = 0;
            if (Changed != null) Changed();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Gold < amount) return false;
            Gold -= amount;
            if (Changed != null) Changed();
            return true;
        }
    }
}
