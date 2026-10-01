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
        /// <summary>
        /// (04/10, en ligne) Joue par une AUTRE machine : un joueur en ligne, ou un bot quand on
        /// est invite (les bots vivent chez l'hote). Ici, une marionnette.
        /// </summary>
        public bool IsRemote;
        /// <summary>En ligne : la place reseau du joueur qui la tient (0 : l'hote), -1 pour un bot.</summary>
        public int NetOwner = -1;
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
        /// <summary>Une graine par manche : les Monuments, les sanctuaires et les obstacles changent de place.</summary>
        public static int RoundSeed { get; private set; }
        /// <summary>Change a chaque nouveau match (les astuces du HUD se remontrent).</summary>
        public static int MatchId { get; private set; }

        /// <summary>Jusqu'a HUIT joueurs (27/09 -- Martin : "tu peux monter le nombre de joueurs").</summary>
        public const int MaxPlayers = 8;
        static readonly string[] BotNames = { "Mahaut", "Oswin", "Guérin", "Aliénor", "Tancrède", "Isaure", "Bohémond" };

        /// <summary>Le nom de la place "index" (0 : toi).</summary>
        public static string NameOf(int index)
        {
            Settings.Load();
            if (index <= 0) return Settings.Shown;
            string bot = BotNames[Mathf.Clamp(index - 1, 0, BotNames.Length - 1)];
            // Ton pseudo est celui d'un bot ("Oswin") : le bot en prend un autre -- deux
            // "Oswin" au-dessus des tetes, on ne sait plus qui est qui.
            return string.Equals(bot, Settings.Shown, System.StringComparison.OrdinalIgnoreCase) ? SpareName : bot;
        }
        const string SpareName = "Enguerrand";

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
            new Color(0.72f, 0.48f, 0.98f),     // violet (27/09 : le vert se confondait avec le rouge pour un daltonien)
            new Color(0.30f, 0.86f, 0.86f),     // turquoise
            new Color(1f, 0.55f, 0.18f),        // orange
            new Color(1f, 0.48f, 0.78f),        // rose
            new Color(0.92f, 0.93f, 0.97f)      // blanc
        };

        public static Color ColourOf(int index) { return Colours[Mathf.Clamp(index, 0, Colours.Length - 1)]; }

        /// <summary>Commencer un match : toi, puis "bots" adversaires (1 a 7).</summary>
        public static void Begin(int bots, int rounds, int minutes)
        {
            Slots.Clear();
            Highlights.Reset();
            int total = Mathf.Clamp(bots, 1, MaxPlayers - 1) + 1;
            for (int i = 0; i < total; i++)
            {
                PlayerSlot s = new PlayerSlot();
                s.Index = i;
                s.IsBot = i > 0;
                s.IsLocal = i == 0;
                s.Name = NameOf(i);
                s.Colour = Colours[i];
                Slots.Add(s);
            }
            Rounds = Mathf.Max(1, rounds);
            RoundSeconds = Mathf.Max(60f, minutes * 60f);
            Played = 0;
            LastWinner = -1;
            History.Clear();
            TieBreakers.Clear();
            Draft.Clear();
            RoundSeed = System.Environment.TickCount;
            MatchId++;
            Online = false;
            Active = true;
            Launched = false;
        }

        /// <summary>
        /// Le match est LANCE (le salon a dit "Commencer") : chaque chargement de la
        /// scene est une manche. Faux a l'ecran-titre, ou un match "d'apercu" (trois
        /// bots) peuple l'ile derriere le menu.
        /// </summary>
        public static bool Launched { get; private set; }

        public static void Launch()
        {
            if (Active) Launched = true;
        }

        /// <summary>
        /// (04/10) LE MATCH EN LIGNE : les joueurs du salon (l'hote d'abord), puis "bots" bots.
        /// Chaque machine le cree pareil ; "mySlot" dit laquelle est la sienne (sa place reseau).
        /// Les bots vivent chez l'hote : chez un invite, ce sont des marionnettes.
        /// </summary>
        public static void BeginOnline(List<string> names, List<int> owners, int mySlot, bool host, int rounds, int minutes, int seed, int matchId)
        {
            Slots.Clear();
            Highlights.Reset();
            for (int i = 0; i < names.Count && i < MaxPlayers; i++)
            {
                PlayerSlot s = new PlayerSlot();
                s.Index = i;
                s.NetOwner = owners[i];
                s.IsBot = owners[i] < 0;
                s.IsLocal = owners[i] == mySlot && owners[i] >= 0;
                s.IsRemote = !s.IsLocal && (owners[i] >= 0 || !host);
                s.Name = names[i];
                s.Colour = Colours[i];
                Slots.Add(s);
            }
            Rounds = Mathf.Max(1, rounds);
            RoundSeconds = Mathf.Max(60f, minutes * 60f);
            Played = 0;
            LastWinner = -1;
            History.Clear();
            TieBreakers.Clear();
            Draft.Clear();
            RoundSeed = seed;
            MatchId = matchId;
            Online = true;
            Active = true;
            Launched = false;
        }

        /// <summary>Vrai pendant un match en ligne (NetGame le tient).</summary>
        public static bool Online { get; private set; }

        /// <summary>
        /// (04/10) L'INVITE RECOPIE LE MATCH DE L'HOTE (NetGame) : les manches jouees, la graine,
        /// les victoires, l'historique. L'hote seul les fait avancer.
        /// </summary>
        public static void Mirror(int rounds, float roundSeconds, int played, int seed, int lastWinner, List<int> history, List<int> tieBreakers)
        {
            Rounds = rounds;
            RoundSeconds = roundSeconds;
            Played = played;
            RoundSeed = seed;
            LastWinner = lastWinner;
            History.Clear();
            History.AddRange(history);
            TieBreakers.Clear();
            TieBreakers.AddRange(tieBreakers);
        }

        public static void Abandon()
        {
            Online = false;
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

        /// <summary>Qui a gagne chaque manche jouee (-1 : personne) -- les pastilles du HUD.</summary>
        public static readonly List<int> History = new List<int>();

        /// <summary>
        /// Fin de manche. "winner" : la place gagnante, ou -1 si personne. Une manche de
        /// departage ne compte que pour les ex aequo.
        /// </summary>
        public static void EndRound(int winner)
        {
            if (winner >= 0 && IsTieBreak && !TieBreakers.Contains(winner)) winner = -1;
            LastWinner = winner;
            History.Add(winner);
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
            /// <summary>Ce que chacun a pris a ce choix, dans l'ordre (l'ecran le raconte).</summary>
            public static readonly List<int> PickedBy = new List<int>();
            public static readonly List<Ability> Picked = new List<Ability>();
            public static int Turn { get; private set; }
            /// <summary>
            /// A CHAQUE MANCHE, DEUX tours de table (29/09 -- Martin : "qu'on n'ait qu'un
            /// passif et un clic gauche", "que les competences changent a chaque manche") :
            /// 0 = une PASSIVE chacun, 1 = une ACTIVE chacun (le clic gauche). Chaque carte
            /// prise REMPLACE celle de la manche d'avant.
            /// </summary>
            public static int Stage { get; private set; }
            /// <summary>Vrai s'il reste le tour des actives.</summary>
            public static bool SecondStageNext { get { return Stage == 0; } }

            public static bool Done { get { return Turn >= Order.Count; } }
            public static int Current { get { return Done ? -1 : Order[Turn]; } }

            /// <summary>(04/10) L'invite recopie le choix en cours chez l'hote.</summary>
            public static void Mirror(int stage, int turn, List<Ability> offer, List<int> order, List<int> pickedBy, List<Ability> picked)
            {
                Stage = stage;
                Turn = turn;
                Offer.Clear(); Offer.AddRange(offer);
                Order.Clear(); Order.AddRange(order);
                PickedBy.Clear(); PickedBy.AddRange(pickedBy);
                Picked.Clear(); Picked.AddRange(picked);
            }

            public static void Clear()
            {
                Offer.Clear();
                Order.Clear();
                PickedBy.Clear();
                Picked.Clear();
                Turn = 0;
            }

            /// <summary>
            /// Etaler les cartes. Avant la premiere manche aussi : on commence le match
            /// avec une passive ET une active, jamais les mains vides.
            /// </summary>
            public static void Prepare() { Build(0); }

            /// <summary>Le second tour de table avant la manche 1 : les actives.</summary>
            public static void PrepareSecondStage() { Build(1); }

            static void Build(int stage)
            {
                Clear();
                Stage = stage;
                // Une graine par tour de table : a egalite de victoires, celui qui choisit sa
                // passive en premier n'est pas forcement le premier pour l'active.
                System.Random rng = new System.Random(RoundSeed + stage * 7919);
                List<Ability> pool = new List<Ability>();
                for (int i = 0; i < AbilityInfo.Count; i++) pool.Add((Ability)i);
                // Une capacite que TOUT LE MONDE a deja ne sert a rien sur la table.
                pool.RemoveAll(p => { for (int k = 0; k < Slots.Count; k++) if (!Slots[k].Has(p)) return false; return true; });
                int cards = Mathf.Min(pool.Count, Slots.Count + 1);
                for (int i = 0; i < cards; i++)
                {
                    // D'abord que des passives, puis que des actives.
                    List<Ability> from = stage == 0 ? pool.FindAll(a => !AbilityInfo.IsActive(a)) : pool.FindAll(AbilityInfo.IsActive);
                    if (from.Count == 0) from = pool;
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

            /// <summary>Si "slot" prend cette carte, quelle capacite perd-il (sinon : -1) ? Une active remplace l'active, une passive la passive.</summary>
            public static int WouldReplace(int slot, Ability card)
            {
                bool active = AbilityInfo.IsActive(card);
                List<Ability> mine = Slots[slot].Abilities;
                for (int i = 0; i < mine.Count; i++)
                    if (AbilityInfo.IsActive(mine[i]) == active && mine[i] != card) return (int)mine[i];
                return -1;
            }

            /// <summary>La place "slot" prend la carte "card". Vrai si c'etait son tour et que la carte existe.</summary>
            public static bool TryPick(int slot, int card)
            {
                if (Done || slot != Current || card < 0 || card >= Offer.Count) return false;
                Ability p = Offer[card];
                if (Slots[slot].Has(p)) return false;
                // La nouvelle remplace TOUTES celles du meme genre (une passive, une active).
                bool kind = AbilityInfo.IsActive(p);
                Slots[slot].Abilities.RemoveAll(a => AbilityInfo.IsActive(a) == kind);
                Slots[slot].Abilities.Add(p);
                PickedBy.Add(slot);
                Picked.Add(p);
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
