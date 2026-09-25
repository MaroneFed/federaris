using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN JOUEUR dans la manche : toi, ou un bot (qui sera un joueur en ligne en Phase 3).
    ///
    /// Tout ce qu'il possede et subit est ici, dans une classe C# pure : ses objets, sa
    /// vie, ses etats (ralenti, pris au piege, invisible). Ses POUVOIRS viennent de sa
    /// place dans le match (PlayerSlot), qui, elle, traverse les manches.
    ///
    /// Body est la presence dans le monde : c'est le seul lien avec Unity.
    /// </summary>
    public class Seeker
    {
        public readonly PlayerSlot Slot;
        public readonly string Name;
        public readonly Color Colour;
        /// <summary>Le joueur de cette machine (les autres sont des bots, ou en ligne).</summary>
        public readonly bool IsPlayer;
        public Transform Body;
        public readonly Loadout Items = new Loadout();

        public Seeker(PlayerSlot slot)
        {
            Slot = slot;
            Name = slot.Name;
            Colour = slot.Colour;
            IsPlayer = slot.IsLocal;
            Health = MaxHealth;
        }

        public int Index { get { return Slot.Index; } }
        public bool Has(Power p) { return Slot.Has(p); }

        // ------------------------------------------------------------------ la vie

        public float MaxHealth { get { return Has(Power.Colosse) ? 150f : 100f; } }
        public float Health;
        public bool Alive { get { return Health > 0f; } }
        /// <summary>Heure (Time.time) du dernier coup recu : la vie ne remonte qu'apres un moment de calme.</summary>
        public float LastHurt = -99f;
        /// <summary>La Seconde chance a deja servi dans cette manche.</summary>
        public bool SecondChanceUsed;

        /// <summary>Qui porte la Couronne ne frappe pas : il la tient a deux mains.</summary>
        public bool CarriesCrown { get { return Crown.Holder == this; } }
        public bool CanStrike { get { return Alive && !CarriesCrown && Time.time >= RootedUntil; } }

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

        // ------------------------------------------------------------------ les etats

        public float SlowUntil = -1f;       // fiole de lenteur
        public float RootedUntil = -1f;     // piege
        public float HiddenUntil = -1f;     // cape d'ombre : les gardes ne le voient pas
        public float FeatherUntil = -1f;    // plume : sauts tres hauts

        public bool Hidden { get { return Time.time < HiddenUntil; } }
        public bool Rooted { get { return Time.time < RootedUntil; } }

        /// <summary>Multiplicateur de vitesse : pouvoirs, couronne, lenteur.</summary>
        public float SpeedFactor
        {
            get
            {
                float f = Has(Power.Coureur) ? 1.15f : 1f;
                if (CarriesCrown && !Has(Power.Porteur)) f *= 0.82f;
                if (Time.time < SlowUntil) f *= 0.5f;
                if (Rooted) f = 0f;
                return f;
            }
        }
    }
}
