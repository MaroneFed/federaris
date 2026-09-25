using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN CHERCHEUR : toi, ou l'un de tes rivaux.
    ///
    /// Tout ce qu'un joueur possede est ici, dans une classe C# pure : son sac, ses
    /// caches, sa stele, son butin. Toi et les rivaux avez EXACTEMENT
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
        public readonly Hoard Hoard;
        public Transform Body;

        /// <summary>Ses deux emplacements d'outils (hache, epee).</summary>
        public readonly Kit Kit = new Kit();

        /// <summary>Les steles des autres qu'on a vues de ses yeux (on peut y revenir).</summary>
        public readonly System.Collections.Generic.List<Seeker> KnownSteles =
            new System.Collections.Generic.List<Seeker>();

        public Seeker(string name, Color colour, bool isPlayer, Inventory bag, Hoard hoard)
        {
            Name = name;
            Colour = colour;
            IsPlayer = isPlayer;
            Bag = bag;
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

        /// <summary>Le poids porte hors du sac : le butin.</summary>
        public void SyncWeight()
        {
            if (Bag != null && Hoard != null) Bag.ExtraWeight = Hoard.CarriedWeight;
        }

        /// <summary>Le score : le butin depose a sa stele.</summary>
        public int Score { get { return Hoard != null ? Hoard.Banked : 0; } }

        // ------------------------------------------------------------------ la vie

        public const float MaxHealth = 100f;
        public float Health = MaxHealth;
        public bool Alive { get { return Health > 0f; } }
        /// <summary>Heure (Time.time) du dernier coup recu : la vie ne remonte qu'apres un moment de calme.</summary>
        public float LastHurt = -99f;

        public bool CanStrike { get { return Alive; } }

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
