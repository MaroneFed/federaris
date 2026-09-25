using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE PLACE dans le match : un joueur, humain ou bot. En Phase 3, une place tenue
    /// par un bot pourra etre prise par un joueur en ligne sans rien changer d'autre
    /// (voir docs/RESEAU.md) : tout ce qui dure d'une manche a l'autre est ici.
    /// </summary>
    public class PlayerSlot
    {
        public int Index;
        public string Name;
        public Color Colour;
        public bool IsBot;
        /// <summary>Tenu par cette machine (toi). En ligne, une seule place par machine.</summary>
        public bool IsLocal;
        public int Wins;
        /// <summary>Les capacites choisies, dans l'ordre (les actives prennent les touches dans cet ordre).</summary>
        public readonly List<Ability> Abilities = new List<Ability>();

        public bool Has(Ability a) { return Abilities.Contains(a); }

        /// <summary>Les capacites actives (trois au plus), dans l'ordre des touches.</summary>
        public List<Ability> Actives
        {
            get
            {
                List<Ability> list = new List<Ability>();
                for (int i = 0; i < Abilities.Count; i++) if (AbilityInfo.IsActive(Abilities[i])) list.Add(Abilities[i]);
                return list;
            }
        }
    }

    /// <summary>
    /// LE MATCH : les places, le nombre de manches, leur duree, les victoires, et le
    /// choix des pouvoirs entre deux manches.
    ///
    /// Classe C# pure et STATIQUE : elle survit au rechargement de la scene (chaque
    /// manche recharge le monde a neuf). En Phase 3, c'est l'hote qui la tient et qui
    /// l'envoie aux autres : c'est le seul etat qui traverse les manches.
    /// </summary>
    public static class Match
    {
        public static readonly int[] RoundChoices = { 3, 5, 7, 10 };
        public static readonly int[] MinuteChoices = { 4, 6, 8, 10 };

        public static readonly List<PlayerSlot> Slots = new List<PlayerSlot>();
        public static bool Active { get; private set; }
        public static int Rounds { get; private set; }
        public static float RoundSeconds { get; private set; }
        /// <summary>Nombre de manches deja jouees.</summary>
        public static int Played { get; private set; }
        /// <summary>Place du vainqueur de la derniere manche (-1 : personne).</summary>
        public static int LastWinner { get; private set; }
        /// <summary>Manche de departage : seuls ces joueurs peuvent la gagner.</summary>
        public static readonly List<int> TieBreakers = new List<int>();
        /// <summary>Une graine par manche : le monument change de place, les coffres aussi.</summary>
        public static int RoundSeed { get; private set; }

        static readonly string[] BotNames = { "Mahaut", "Oswin", "Guerin" };

        /// <summary>
        /// LE NIVEAU DES BOTS (choisi au salon, garde d'un match a l'autre) : 0 faciles,
        /// 1 normaux, 2 coriaces. Il change leur vitesse, leurs reflexes, et la
        /// frequence de leurs capacites -- jamais les regles.
        /// </summary>
        public static int BotLevel = 1;
        public static readonly string[] BotLevels = { "Faciles", "Normaux", "Coriaces" };
        static readonly Color[] Colours =
        {
            new Color(0.95f, 0.78f, 0.35f),     // toi : or
            new Color(0.88f, 0.35f, 0.28f),     // rouge
            new Color(0.35f, 0.6f, 0.95f),      // bleu
            new Color(0.45f, 0.82f, 0.4f)       // vert
        };

        public static Color ColourOf(int index) { return Colours[Mathf.Clamp(index, 0, Colours.Length - 1)]; }

        /// <summary>Commencer un match : toi, puis "bots" adversaires (1 a 3).</summary>
        public static void Begin(int bots, int rounds, int minutes)
        {
            Slots.Clear();
            int total = Mathf.Clamp(bots, 1, 3) + 1;
            for (int i = 0; i < total; i++)
            {
                PlayerSlot s = new PlayerSlot();
                s.Index = i;
                s.IsBot = i > 0;
                s.IsLocal = i == 0;
                s.Name = i == 0 ? "Toi" : BotNames[i - 1];
                s.Colour = Colours[i];
                Slots.Add(s);
            }
            Rounds = Mathf.Max(1, rounds);
            RoundSeconds = Mathf.Max(60f, minutes * 60f);
            Played = 0;
            LastWinner = -1;
            TieBreakers.Clear();
            Draft.Clear();
            RoundSeed = System.Environment.TickCount;
            Active = true;
            Launched = false;
        }

        /// <summary>
        /// Le match est LANCE (le salon a dit "Commencer") : chaque chargement de la
        /// scene est une manche. Faux a l'ecran-titre, ou un match "d'apercu" (trois
        /// bots) peuple la foret derriere le menu.
        /// </summary>
        public static bool Launched { get; private set; }

        public static void Launch()
        {
            if (Active) Launched = true;
        }

        public static void Abandon()
        {
            Active = false;
            Launched = false;
            Slots.Clear();
            Draft.Clear();
            TieBreakers.Clear();
        }

        public static PlayerSlot Local
        {
            get
            {
                for (int i = 0; i < Slots.Count; i++) if (Slots[i].IsLocal) return Slots[i];
                return null;
            }
        }

        /// <summary>Numero de la manche en cours, a partir de 1.</summary>
        public static int RoundNumber { get { return Played + 1; } }
        public static bool IsTieBreak { get { return TieBreakers.Count > 0; } }

        /// <summary>
        /// Fin de manche. "winner" : la place gagnante, ou -1 si personne. Une manche de
        /// departage ne compte que pour les ex aequo.
        /// </summary>
        public static void EndRound(int winner)
        {
            if (winner >= 0 && IsTieBreak && !TieBreakers.Contains(winner)) winner = -1;
            LastWinner = winner;
            if (winner >= 0 && winner < Slots.Count) Slots[winner].Wins++;
            Played++;
            RoundSeed = RoundSeed * 31 + 7;
            if (IsTieBreak && winner >= 0) TieBreakers.Clear();
            else if (!IsTieBreak && Played >= Rounds) CheckTie();
        }

        /// <summary>A la derniere manche, s'il y a des ex aequo en tete : une manche de plus, entre eux.</summary>
        static void CheckTie()
        {
            int best = -1;
            for (int i = 0; i < Slots.Count; i++) best = Mathf.Max(best, Slots[i].Wins);
            List<int> top = new List<int>();
            for (int i = 0; i < Slots.Count; i++) if (Slots[i].Wins == best) top.Add(i);
            if (top.Count > 1 && best > 0) { TieBreakers.Clear(); TieBreakers.AddRange(top); }
        }

        /// <summary>Le match est fini : toutes les manches jouees, et pas de departage en cours.</summary>
        public static bool Over
        {
            get { return Active && Played >= Rounds && !IsTieBreak || Played >= Rounds + 3; }
        }

        /// <summary>Le vainqueur du match (null s'il n'y en a pas, ou pas encore).</summary>
        public static PlayerSlot Champion
        {
            get
            {
                PlayerSlot best = null;
                bool tie = false;
                for (int i = 0; i < Slots.Count; i++)
                {
                    if (best == null || Slots[i].Wins > best.Wins) { best = Slots[i]; tie = false; }
                    else if (Slots[i].Wins == best.Wins) tie = true;
                }
                return tie || best == null || best.Wins == 0 ? null : best;
            }
        }

        // ================================================================== le choix des capacites

        /// <summary>
        /// LE CHOIX DES CAPACITES, entre deux manches. On etale (joueurs + 1) cartes ;
        /// chacun en prend une, le moins de victoires d'abord, le vainqueur de la
        /// manche en dernier (a victoires egales, le hasard de la graine).
        /// </summary>
        public static class Draft
        {
            public static readonly List<Ability> Offer = new List<Ability>();
            public static readonly List<int> Order = new List<int>();
            public static int Turn { get; private set; }

            public static bool Done { get { return Turn >= Order.Count; } }
            public static int Current { get { return Done ? -1 : Order[Turn]; } }

            public static void Clear()
            {
                Offer.Clear();
                Order.Clear();
                Turn = 0;
            }

            /// <summary>
            /// Etaler les cartes. Avant la premiere manche aussi : on commence le match
            /// avec une capacite en main, jamais les mains vides. A la premiere, une
            /// ACTIVE au moins sur la table pour chacun (sinon on ne ferait rien).
            /// </summary>
            public static void Prepare()
            {
                Clear();
                System.Random rng = new System.Random(RoundSeed);
                List<Ability> pool = new List<Ability>();
                for (int i = 0; i < AbilityInfo.Count; i++) pool.Add((Ability)i);
                // Une capacite que TOUT LE MONDE a deja ne sert a rien sur la table.
                pool.RemoveAll(p => { for (int k = 0; k < Slots.Count; k++) if (!Slots[k].Has(p)) return false; return true; });
                int cards = Mathf.Min(pool.Count, Slots.Count + 1);
                bool first = Played == 0;
                for (int i = 0; i < cards; i++)
                {
                    // Au premier choix, que des actives : tout le monde a quelque chose a essayer.
                    List<Ability> from = pool;
                    if (first)
                    {
                        from = pool.FindAll(AbilityInfo.IsActive);
                        if (from.Count == 0) from = pool;
                    }
                    Ability pick = from[rng.Next(from.Count)];
                    Offer.Add(pick);
                    pool.Remove(pick);
                }
                // L'ordre : le moins de manches d'abord, le vainqueur en dernier ; a
                // egalite, le hasard (sinon tu choisirais toujours le premier).
                List<int> shuffle = new List<int>();
                for (int i = 0; i < Slots.Count; i++) shuffle.Add(rng.Next(1000));
                for (int i = 0; i < Slots.Count; i++) Order.Add(i);
                Order.Sort((a, b) =>
                {
                    if (a == b) return 0;
                    if (a == LastWinner) return 1;
                    if (b == LastWinner) return -1;
                    int w = Slots[a].Wins.CompareTo(Slots[b].Wins);
                    return w != 0 ? w : shuffle[a].CompareTo(shuffle[b]);
                });
                while (!Done && !AnyNewFor(Current)) Turn++;
            }

            /// <summary>Si "slot" prend cette carte, quelle active perd-il (sinon : -1) ?</summary>
            public static int WouldReplace(int slot, Ability card)
            {
                if (!AbilityInfo.IsActive(card)) return -1;
                List<Ability> actives = Slots[slot].Actives;
                return actives.Count >= AbilityInfo.MaxActives ? (int)actives[0] : -1;
            }

            /// <summary>La place "slot" prend la carte "card". Vrai si c'etait son tour et que la carte existe.</summary>
            public static bool TryPick(int slot, int card)
            {
                if (Done || slot != Current || card < 0 || card >= Offer.Count) return false;
                Ability p = Offer[card];
                if (Slots[slot].Has(p)) return false;
                int lost = WouldReplace(slot, p);
                if (lost >= 0) Slots[slot].Abilities.Remove((Ability)lost);
                Slots[slot].Abilities.Add(p);
                Offer.RemoveAt(card);
                Turn++;
                while (!Done && !AnyNewFor(Current)) Turn++;
                return true;
            }

            /// <summary>Un bot choisit : ce qu'il n'a pas, dans son ordre de preference.</summary>
            public static int BotChoice(int slot)
            {
                Ability[] taste = { Ability.Grappin, Ability.Ruee, Ability.Crochet, Ability.Onde, Ability.DoubleSaut, Ability.Planeur,
                                    Ability.Clignement, Ability.Souffle, Ability.Coureur, Ability.Echange, Ability.Bond, Ability.Porteur,
                                    Ability.Ancrage, Ability.Poigne, Ability.Mine, Ability.Gel, Ability.Voile, Ability.Nuee, Ability.PriseFerme,
                                    Ability.Recharge, Ability.Mur, Ability.Rappel, Ability.Rebond, Ability.Aimant, Ability.Flair, Ability.Ombre };
                int shift = slot * 5;
                for (int t = 0; t < taste.Length; t++)
                {
                    Ability want = taste[(t + shift) % taste.Length];
                    int at = Offer.IndexOf(want);
                    if (at >= 0 && !Slots[slot].Has(want)) return at;
                }
                for (int i = 0; i < Offer.Count; i++) if (!Slots[slot].Has(Offer[i])) return i;
                return -1;
            }

            static bool AnyNewFor(int slot)
            {
                for (int i = 0; i < Offer.Count; i++) if (!Slots[slot].Has(Offer[i])) return true;
                return false;
            }
        }
    }
}
