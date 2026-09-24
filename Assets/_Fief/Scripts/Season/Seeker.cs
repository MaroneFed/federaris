using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN CHERCHEUR DE RELIQUE : toi, ou l'un de tes rivaux.
    ///
    /// Tout ce qu'un joueur possede est ici, dans une classe C# pure : son sac, sa
    /// bourse, ses caches, sa stele, sa relique. Toi et les rivaux avez EXACTEMENT
    /// les memes regles -- c'est ce qui rendra la Phase 3 (multijoueur) simple : un
    /// vrai joueur prendra la place d'un rival, sans rien reecrire.
    ///
    /// Body est la presence dans le monde (ton corps, ou celui du rival) : c'est le
    /// seul lien avec Unity, et il peut etre nul.
    /// </summary>
    public class Seeker
    {
        public readonly string Name;
        public readonly Color Colour;
        public readonly bool IsPlayer;
        public readonly Inventory Bag;
        public readonly Wallet Money;
        public readonly Hoard Hoard;
        public Transform Body;

        /// <summary>Ses deux emplacements d'outils (hache, epee).</summary>
        public readonly Kit Kit = new Kit();

        /// <summary>Les steles des autres qu'on a vues de ses yeux (on peut y revenir).</summary>
        public readonly System.Collections.Generic.List<Seeker> KnownSteles =
            new System.Collections.Generic.List<Seeker>();

        public Seeker(string name, Color colour, bool isPlayer, Inventory bag, Wallet purse, Hoard hoard)
        {
            Name = name;
            Colour = colour;
            IsPlayer = isPlayer;
            Bag = bag;
            Money = purse;
            Hoard = hoard;
        }

        public bool Knows(Seeker other)
        {
            return KnownSteles.Contains(other);
        }

        public void Discover(Seeker other)
        {
            if (other != null && other != this && !KnownSteles.Contains(other)) KnownSteles.Add(other);
        }

        /// <summary>Le poids porte hors du sac suit la relique et le trophee.</summary>
        public void SyncWeight()
        {
            if (Bag != null && Hoard != null) Bag.ExtraWeight = Hoard.CarriedWeight;
        }

        public int Score { get { return Hoard != null ? Hoard.FinalScore : 0; } }

        // ------------------------------------------------------------------ la vie

        public const float MaxHealth = 100f;
        public float Health = MaxHealth;
        public bool Alive { get { return Health > 0f; } }
        /// <summary>Heure (Time.time) du dernier coup recu : la vie ne remonte qu'apres un moment de calme.</summary>
        public float LastHurt = -99f;

        /// <summary>
        /// LA REGLE DU PORTEUR (Martin, 24/09) : qui porte une relique -- la sienne en
        /// main, ou une volee -- ne peut PAS frapper. Les autres, si. Porter, c'est
        /// etre une cible.
        /// </summary>
        public bool CanStrike { get { return Alive && Hoard != null && !Hoard.RelicInHand && Hoard.Trophy == null; } }

        /// <summary>Encaisser un coup. Vrai si ce coup est mortel.</summary>
        public bool TakeDamage(float amount, float now)
        {
            if (!Alive || amount <= 0f) return false;
            Health = Mathf.Max(0f, Health - amount);
            LastHurt = now;
            return Health <= 0f;
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }
    }
}
