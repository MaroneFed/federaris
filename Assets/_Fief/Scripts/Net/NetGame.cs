using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Fief.Net;

namespace Fief
{
    /// <summary>
    /// LE MATCH EN LIGNE (04/10, etape 2 -- Martin : "on arrive a se connecter, sauf qu'apres on ne
    /// peut pas lancer de game"). Ce que les machines se disent, au-dessus du tuyau (NetLink) :
    ///
    ///   - L'ETAT DU MATCH (l'hote, 4 fois par seconde, et tout de suite quand il change) : ou on
    ///     en est (choix des cartes, manche, fin de manche, podium), la graine, les victoires, les
    ///     capacites de chacun, les cartes sur la table. Les invites le RECOPIENT : ils voient ce
    ///     que l'hote voit, et chargent la meme ile (la meme graine) quand il en charge une.
    ///   - LES POSITIONS (20 fois par seconde) : chaque invite dit ou il est a l'hote ; l'hote dit a
    ///     tout le monde ou sont tous les joueurs (le sien, ses bots, les invites) et la Couronne.
    ///     Chez chacun, les autres sont des MARIONNETTES (Rival en mode Remote).
    ///   - LES COUPS : chacun joue SON joueur chez lui (pas de latence pour bouger). Quand tu
    ///     pousses la marionnette d'un ami, le coup part chez lui (par l'hote) et c'est sa machine
    ///     qui le projette. Les obstacles et les gargouilles, chacun les a chez soi.
    ///   - LA COURONNE : l'HOTE decide (docs/RESEAU.md : "autorite absolue de l'hote"). Un invite
    ///     DEMANDE (je la prends, je l'ai lachee, je suis tombe) ; l'hote passe par les memes portes
    ///     que tout le monde (Crown.TryTakeFor, TrySteal, KnockOff...) et le dit a tous.
    ///   - LA FIN DE MANCHE : chez l'hote seulement (le sacre, le chrono) ; les invites suivent.
    ///
    /// Concept : on n'envoie pas "ce qui s'est passe" mais "ou on en est" (l'etat), plusieurs fois
    /// par seconde. Un paquet perdu ? Le suivant dit la meme chose. Seuls les coups, les demandes
    /// et les choix de cartes partent en "fiable" (renvoyes jusqu'a l'accuse de reception).
    /// </summary>
    public static class NetGame
    {
        // ------------------------------------------------------------------ ce que fait un joueur (en bits)

        public const int FlagGrounded = 1;
        public const int FlagGliding = 2;
        public const int FlagTumbling = 4;
        public const int FlagHidden = 8;
        public const int FlagGraced = 16;
        public const int FlagStunned = 32;
        public const int FlagGoldWings = 64;
        public const int FlagSlowed = 128;

        enum Kind : byte { State = 1, Snapshot = 2, Self = 3, Hit = 4, Blink = 5, CrownAsk = 6, Pick = 7, Afflict = 8 }

        /// <summary>Ou en est le match chez l'hote.</summary>
        public enum Phase : byte { Lobby = 0, Draft = 1, Round = 2, RoundOver = 3, Ended = 4 }

        /// <summary>Ce qu'un invite demande a l'hote au sujet de la Couronne.</summary>
        public enum Ask : byte { Take = 1, KnockOff = 2, Slip = 3, FellWith = 4 }

        static NetLink Link { get { return NetSession.Link; } }

        /// <summary>Vrai pendant un match en ligne.</summary>
        public static bool Active { get { return Link != null && Match.Online && Match.Active; } }
        public static bool IsHost { get { return Active && Link.IsHost; } }
        public static bool IsClient { get { return Active && !Link.IsHost; } }

        // ------------------------------------------------------------------ ce que l'hote annonce (cote invite)

        public static Phase HostPhase { get; private set; }
        public static int HostToken { get; private set; }
        public static bool HostLaunched { get; private set; }
        /// <summary>Vrai si l'hote est parti (ou ne repond plus) : le menu ramene au titre.</summary>
        public static bool HostLost { get { return Link != null && !Link.IsHost && (Link.Status == NetLink.State.Lost || Link.Status == NetLink.State.Refused); } }

        /// <summary>
        /// LE NUMERO DE L'ILE : l'hote l'augmente a chaque fois qu'il charge une manche ; l'invite
        /// recharge la sienne quand il change. Les positions d'une autre ile sont ignorees.
        /// </summary>
        public static int RoundToken;

        /// <summary>La phase de l'hote (Menus la donne a chaque image).</summary>
        public static Phase LocalPhase = Phase.Lobby;

        static float stateTimer, snapTimer;
        static int lastSignature;
        static int pickSentFor = -1;
        static int matchCounter = 1;

        /// <summary>Ce qu'un invite a dit de lui en dernier (l'hote le relaie tel quel aux autres).</summary>
        struct Pose
        {
            public Vector3 Position;
            public float Yaw;
            public int Flags;
        }
        static readonly Dictionary<int, Pose> poses = new Dictionary<int, Pose>();

        // ================================================================== chaque image

        /// <summary>Appele par NetSession, apres que le lien a lu ses paquets.</summary>
        public static void Tick(float dt)
        {
            NetLink link = Link;
            if (link == null) return;
            while (link.Inbox.Count > 0)
            {
                NetLink.Incoming msg = link.Inbox.Dequeue();
                try { Read(msg.From, msg.Data); }
                catch (System.Exception e) { Debug.LogWarning("[FIEF] Réseau : message illisible (" + e.Message + ")"); }
            }
            if (link.IsHost) HostTick(dt);
            else ClientTick(dt);
        }

        static void HostTick(float dt)
        {
            NetLink link = Link;
            // Un ami parti en plein match : sa place redevient un bot (chez l'hote, il reprend la ou il est).
            if (Match.Online)
            {
                for (int i = 0; i < Match.Slots.Count; i++)
                {
                    PlayerSlot s = Match.Slots[i];
                    if (s.NetOwner > 0 && !link.HasPeer(s.NetOwner))
                    {
                        s.NetOwner = -1;
                        s.IsBot = true;
                        s.IsRemote = false;
                        Debug.Log("[FIEF] Réseau : " + s.Name + " est parti, un bot prend sa place.");
                    }
                }
            }
            // L'etat du match : 4 fois par seconde, et TOUT DE SUITE (fiable) quand il change.
            stateTimer -= dt;
            int signature = Signature();
            bool changed = signature != lastSignature;
            if (stateTimer <= 0f || changed)
            {
                stateTimer = 0.25f;
                lastSignature = signature;
                link.Broadcast(WriteState(), changed);
            }
            // Les positions : 20 fois par seconde, pendant une manche.
            snapTimer -= dt;
            if (snapTimer <= 0f && InRound)
            {
                snapTimer = 0.05f;
                link.Broadcast(WriteSnapshot(), false);
            }
        }

        static void ClientTick(float dt)
        {
            snapTimer -= dt;
            if (snapTimer <= 0f && IsClient && InRound && Game.Me != null && Game.Me.Body != null)
            {
                snapTimer = 0.05f;
                Link.Send(0, WriteSelf(), false);
            }
        }

        /// <summary>Vrai quand une manche est construite et que l'ile est celle du numero en cours.</summary>
        static bool InRound { get { return Match.Online && Match.Launched && Game.Ready && Game.Seekers.Count > 0; } }

        /// <summary>Ce qui, s'il change, merite un envoi immediat (phase, ile, tour du choix...).</summary>
        static int Signature()
        {
            int h = (int)LocalPhase * 7919 + RoundToken * 31 + Match.Played * 131 + (Match.Launched ? 1 : 0);
            h = h * 17 + Match.Draft.Turn + Match.Draft.Stage * 1000 + Match.Draft.Offer.Count * 100;
            h = h * 17 + (Match.Online ? Match.MatchId : -1);
            for (int i = 0; i < Match.Slots.Count; i++) h = h * 3 + Match.Slots[i].NetOwner;
            return h;
        }

        // ================================================================== lancer le match (l'hote)

        /// <summary>
        /// L'HOTE LANCE LE MATCH : les joueurs du salon (lui d'abord), puis "bots" bots. Le salon se
        /// ferme (plus personne n'entre). Vrai si c'est parti.
        /// </summary>
        public static bool HostBegin(int bots, int rounds, int minutes)
        {
            NetLink link = Link;
            if (link == null || !link.IsHost) return false;
            List<string> names = new List<string>();
            List<int> owners = new List<int>();
            for (int i = 0; i < link.Roster.Count && names.Count < Match.MaxPlayers; i++)
            {
                names.Add(link.Roster[i].Name);
                owners.Add(link.Roster[i].Slot);
            }
            bots = Mathf.Clamp(bots, 0, Match.MaxPlayers - names.Count);
            if (names.Count + bots < 2) bots = 1;
            HashSet<string> taken = new HashSet<string>();
            for (int i = 0; i < names.Count; i++) taken.Add(names[i].ToLowerInvariant());
            for (int k = 1; bots > 0 && k < 30; k++)
            {
                string bot = Match.NameOf(Mathf.Min(k, Match.MaxPlayers - 1));
                if (k >= Match.MaxPlayers) bot = "Bot " + k;
                if (taken.Contains(bot.ToLowerInvariant())) continue;
                taken.Add(bot.ToLowerInvariant());
                names.Add(bot);
                owners.Add(-1);
                bots--;
            }
            link.Locked = true;
            matchCounter++;
            Match.BeginOnline(names, owners, 0, true, rounds, minutes, System.Environment.TickCount, 1000 + matchCounter);
            RoundToken = 0;
            poses.Clear();
            return true;
        }

        /// <summary>L'hote charge une nouvelle ile (une manche) : les invites suivront.</summary>
        public static void HostNewRound()
        {
            if (!IsHost) return;
            RoundToken++;
            poses.Clear();
        }

        /// <summary>Fin du match en ligne (Nouveau match, abandon) : le salon se rouvre.</summary>
        public static void HostReopen()
        {
            if (Link != null && Link.IsHost) Link.Locked = false;
            LocalPhase = Phase.Lobby;
            poses.Clear();
        }

        // ================================================================== l'etat du match

        static byte[] WriteState()
        {
            return Pack(w =>
            {
                w.Write((byte)Kind.State);
                Phase phase = Match.Online ? LocalPhase : Phase.Lobby;
                w.Write((byte)phase);
                w.Write(Match.Online ? Match.MatchId : 0);
                w.Write(RoundToken);
                w.Write(Match.Launched);
                if (phase == Phase.Lobby) return;
                w.Write((byte)Match.Rounds);
                w.Write(Match.RoundSeconds);
                w.Write((byte)Match.Played);
                w.Write(Match.RoundSeed);
                w.Write((sbyte)Match.LastWinner);
                w.Write((byte)Match.BotLevel);
                w.Write(Match.GodMode);
                WriteInts(w, Match.History);
                WriteInts(w, Match.TieBreakers);
                w.Write((byte)Match.Slots.Count);
                for (int i = 0; i < Match.Slots.Count; i++)
                {
                    PlayerSlot s = Match.Slots[i];
                    w.Write(s.Name);
                    w.Write((sbyte)s.NetOwner);
                    w.Write((byte)s.Wins);
                    WriteAbilities(w, s.Abilities);
                }
                w.Write((byte)Match.Draft.Stage);
                w.Write((byte)Match.Draft.Turn);
                WriteAbilities(w, Match.Draft.Offer);
                WriteInts(w, Match.Draft.Order);
                WriteInts(w, Match.Draft.PickedBy);
                WriteAbilities(w, Match.Draft.Picked);
                w.Write(Game.Season != null ? Game.Season.Elapsed : 0f);
            });
        }

        static void ReadState(BinaryReader r)
        {
            Phase phase = (Phase)r.ReadByte();
            int matchId = r.ReadInt32();
            int token = r.ReadInt32();
            bool launched = r.ReadBoolean();
            HostPhase = phase;
            if (phase == Phase.Lobby) { HostLaunched = false; return; }
            int rounds = r.ReadByte();
            float seconds = r.ReadSingle();
            int played = r.ReadByte();
            int seed = r.ReadInt32();
            int lastWinner = r.ReadSByte();
            int botLevel = r.ReadByte();
            bool god = r.ReadBoolean();
            List<int> history = ReadInts(r);
            List<int> ties = ReadInts(r);
            int count = r.ReadByte();
            List<string> names = new List<string>();
            List<int> owners = new List<int>();
            List<int> wins = new List<int>();
            List<List<Ability>> abilities = new List<List<Ability>>();
            for (int i = 0; i < count; i++)
            {
                names.Add(r.ReadString());
                owners.Add(r.ReadSByte());
                wins.Add(r.ReadByte());
                abilities.Add(ReadAbilities(r));
            }
            int stage = r.ReadByte();
            int turn = r.ReadByte();
            List<Ability> offer = ReadAbilities(r);
            List<int> order = ReadInts(r);
            List<int> pickedBy = ReadInts(r);
            List<Ability> picked = ReadAbilities(r);
            float elapsed = r.ReadSingle();

            // Un nouveau match : on le cree comme l'hote (meme ordre, memes noms ; notre place).
            if (!Match.Online || Match.MatchId != matchId || Match.Slots.Count != count)
            {
                Match.BeginOnline(names, owners, Link.MySlot, false, rounds, Mathf.RoundToInt(seconds / 60f), seed, matchId);
                Stats.Reset();
                RoundToken = token;
                poses.Clear();
            }
            HostToken = token;
            HostLaunched = launched;
            Match.BotLevel = botLevel;
            Match.GodMode = god;
            Match.Mirror(rounds, seconds, played, seed, lastWinner, history, ties);
            for (int i = 0; i < count; i++)
            {
                PlayerSlot s = Match.Slots[i];
                s.Wins = wins[i];
                s.Abilities.Clear();
                s.Abilities.AddRange(abilities[i]);
                // Un ami parti : sa place est un bot de l'hote (une marionnette, ici).
                if (owners[i] < 0 && s.NetOwner >= 0) { s.NetOwner = -1; s.IsBot = true; s.IsRemote = true; }
            }
            Match.Draft.Mirror(stage, turn, offer, order, pickedBy, picked);
            if (Game.Season != null && token == RoundToken && Match.Launched) Game.Season.Sync(elapsed);
        }

        // ================================================================== les positions

        static byte[] WriteSnapshot()
        {
            return Pack(w =>
            {
                w.Write((byte)Kind.Snapshot);
                w.Write(RoundToken);
                // La Couronne : ou elle en est, qui la porte, ou elle est posee.
                w.Write((byte)Crown.Where);
                w.Write((sbyte)(Crown.Holder != null ? Crown.Holder.Index : -1));
                WriteVector(w, Crown.Instance != null ? Crown.Instance.RestingPosition : Vector3.zero);
                int n = 0;
                for (int i = 0; i < Game.Seekers.Count; i++) if (Game.Seekers[i].Body != null) n++;
                w.Write((byte)n);
                for (int i = 0; i < Game.Seekers.Count; i++)
                {
                    Seeker s = Game.Seekers[i];
                    if (s.Body == null) continue;
                    Pose p;
                    if (s.Remote && poses.TryGetValue(s.Index, out p)) { }
                    else
                    {
                        p.Position = s.Body.position;
                        p.Yaw = s.Body.eulerAngles.y;
                        p.Flags = FlagsOf(s);
                    }
                    w.Write((byte)s.Index);
                    WriteVector(w, p.Position);
                    w.Write(Yaw16(p.Yaw));
                    w.Write((ushort)p.Flags);
                }
            });
        }

        static void ReadSnapshot(BinaryReader r)
        {
            int token = r.ReadInt32();
            Crown.State state = (Crown.State)r.ReadByte();
            int holder = r.ReadSByte();
            Vector3 crownAt = ReadVector(r);
            int n = r.ReadByte();
            bool here = token == RoundToken && InRound;
            for (int i = 0; i < n; i++)
            {
                int slot = r.ReadByte();
                Vector3 pos = ReadVector(r);
                float yaw = Yaw(r.ReadUInt16());
                int flags = r.ReadUInt16();
                if (!here) continue;
                Seeker s = Game.SeekerOf(slot);
                if (s == null || !s.Remote) continue;
                Rival puppet = Rival.Of(s);
                if (puppet != null) puppet.NetState(pos, yaw, flags);
            }
            if (here && Crown.Instance != null) Crown.Instance.Mirror(state, holder >= 0 ? Game.SeekerOf(holder) : null, crownAt);
        }

        static byte[] WriteSelf()
        {
            return Pack(w =>
            {
                w.Write((byte)Kind.Self);
                w.Write(RoundToken);
                w.Write((byte)Game.Me.Index);
                WriteVector(w, Game.Me.Body.position);
                w.Write(Yaw16(Game.Me.Body.eulerAngles.y));
                w.Write((ushort)FlagsOf(Game.Me));
            });
        }

        static void ReadSelf(int from, BinaryReader r)
        {
            int token = r.ReadInt32();
            int slot = r.ReadByte();
            Pose p;
            p.Position = ReadVector(r);
            p.Yaw = Yaw(r.ReadUInt16());
            p.Flags = r.ReadUInt16();
            if (token != RoundToken || !InRound) return;
            Seeker s = Owned(from, slot);
            if (s == null) return;
            poses[slot] = p;
            Rival puppet = Rival.Of(s);
            if (puppet != null) puppet.NetState(p.Position, p.Yaw, p.Flags);
        }

        /// <summary>Le joueur de la place "slot", s'il appartient bien a la machine "from" (sinon null).</summary>
        static Seeker Owned(int from, int slot)
        {
            if (slot < 0 || slot >= Match.Slots.Count || Match.Slots[slot].NetOwner != from) return null;
            return Game.SeekerOf(slot);
        }

        /// <summary>Ce que fait un joueur joue ICI, en bits.</summary>
        public static int FlagsOf(Seeker s)
        {
            if (s == null) return 0;
            if (s.IsPlayer && Game.Player != null)
            {
                int f = CommonFlags(s);
                if (!Game.Player.Airborne) f |= FlagGrounded;
                if (Game.Player.Gliding) f |= FlagGliding;
                return f;
            }
            Rival r = Rival.Of(s);
            return r != null ? r.NetFlags : CommonFlags(s);
        }

        public static int CommonFlags(Seeker s)
        {
            int f = 0;
            if (s.Tumbling || s.Launched) f |= FlagTumbling;
            if (s.Hidden) f |= FlagHidden;
            if (s.Graced) f |= FlagGraced;
            if (s.Stunned) f |= FlagStunned;
            if (s.HasWings) f |= FlagGoldWings;
            if (s.Slowed) f |= FlagSlowed;
            return f;
        }

        // ================================================================== les coups

        /// <summary>
        /// UN COUP SUR LE JOUEUR D'UNE AUTRE MACHINE (Combat.Hit le confie ici). Seul le coup d'un
        /// joueur joue ICI part (un obstacle, une gargouille, une marionnette : sa machine a les
        /// siens). "steal" : une poussee ou un pique sur le porteur -- l'hote decide du vol.
        /// </summary>
        public static void RemoteHit(Seeker victim, Vector3 velocity, float stun, bool dropsCrown, Seeker by, bool steal)
        {
            if (!Active || victim == null || by == null || by.Remote) return;
            float launch = Mathf.Max(0f, victim.LaunchUntil - Time.time);
            float slow = Mathf.Max(0f, victim.SlowUntil - Time.time);
            if (IsHost) HostHit(victim, by, velocity, stun, launch, slow, dropsCrown, steal);
            else Link.Send(0, WriteHit(victim, by, velocity, stun, launch, slow, dropsCrown, steal), true);
        }

        /// <summary>L'hote tranche : le vol, la Couronne qui tombe -- puis le coup part chez la victime.</summary>
        static void HostHit(Seeker v, Seeker by, Vector3 velocity, float stun, float launch, float slow, bool drops, bool steal)
        {
            if (steal && v.CarriesCrown && !v.Graced && Crown.TrySteal(by, v))
            {
                velocity *= 0.7f;
                drops = false;
                launch = Mathf.Min(launch, 0.6f);
            }
            if (!v.Remote) { ApplyHit(v, by, velocity, stun, launch, slow, drops); return; }
            if (drops && v.CarriesCrown && !v.Graced)
            {
                if (v.Has(Ability.PriseFerme) && !v.GripUsed) v.GripUsed = true;
                else { Crown.KnockOff(v, velocity); Feed.CrownKnocked(v, by); }
            }
            if (v.Slot.NetOwner > 0) Link.Send(v.Slot.NetOwner, WriteHit(v, by, velocity, stun, launch, slow, false, false), true);
        }

        /// <summary>Le coup arrive chez la victime : sa machine la projette (Combat.Hit, comme d'habitude).</summary>
        static void ApplyHit(Seeker v, Seeker by, Vector3 velocity, float stun, float launch, float slow, bool drops)
        {
            if (v == null || v.Remote || v.Body == null) return;
            if (!v.Graced && launch > 0f) v.Launch(launch);
            if (!v.Graced && slow > 0f) v.SlowUntil = Mathf.Max(v.SlowUntil, Time.time + slow);
            Combat.Hit(v, velocity, stun, drops, by);
        }

        static byte[] WriteHit(Seeker v, Seeker by, Vector3 velocity, float stun, float launch, float slow, bool drops, bool steal)
        {
            return Pack(w =>
            {
                w.Write((byte)Kind.Hit);
                w.Write(RoundToken);
                w.Write((byte)v.Index);
                w.Write((sbyte)(by != null ? by.Index : -1));
                WriteVector(w, velocity);
                w.Write(stun);
                w.Write(launch);
                w.Write(slow);
                w.Write(drops);
                w.Write(steal);
            });
        }

        static void ReadHit(int from, BinaryReader r)
        {
            int token = r.ReadInt32();
            Seeker v = Game.SeekerOf(r.ReadByte());
            int bySlot = r.ReadSByte();
            Vector3 velocity = ReadVector(r);
            float stun = Mathf.Clamp(r.ReadSingle(), 0f, 0.7f);
            float launch = Mathf.Clamp(r.ReadSingle(), 0f, 2f);
            float slow = Mathf.Clamp(r.ReadSingle(), 0f, 5f);
            bool drops = r.ReadBoolean();
            bool steal = r.ReadBoolean();
            if (token != RoundToken || !InRound || v == null) return;
            velocity = Vector3.ClampMagnitude(velocity, 80f);
            Seeker by = bySlot >= 0 ? Game.SeekerOf(bySlot) : null;
            if (Link.IsHost)
            {
                // Un invite ne frappe qu'avec SON joueur.
                if (by == null || by.Slot.NetOwner != from) return;
                HostHit(v, by, velocity, stun, launch, slow, drops, steal);
            }
            else if (from == 0) ApplyHit(v, by, velocity, stun, launch, slow, drops);
        }

        /// <summary>L'ECHANGE sur la marionnette d'un ami : c'est chez lui qu'il change de place.</summary>
        public static void RemoteBlink(Seeker victim, Vector3 position)
        {
            if (!Active || victim == null || !victim.Remote) return;
            byte[] data = Pack(w =>
            {
                w.Write((byte)Kind.Blink);
                w.Write(RoundToken);
                w.Write((byte)victim.Index);
                WriteVector(w, position);
            });
            if (IsHost) { if (victim.Slot.NetOwner > 0) Link.Send(victim.Slot.NetOwner, data, true); }
            else Link.Send(0, data, true);
        }

        /// <summary>
        /// (06/10) UN SORT DE FOU sur la marionnette d'un ami (prison, glu, tete a l'envers, mini,
        /// encre, ballon) : c'est chez lui qu'il est enchaine, retreci... (Combat.Afflict).
        /// </summary>
        public static void RemoteAfflict(Seeker victim, Combat.Affliction what, float seconds, Seeker by)
        {
            if (!Active || victim == null || !victim.Remote || by == null || by.Remote) return;
            byte[] data = Pack(w =>
            {
                w.Write((byte)Kind.Afflict);
                w.Write(RoundToken);
                w.Write((byte)victim.Index);
                w.Write((byte)by.Index);
                w.Write((byte)what);
                w.Write(seconds);
            });
            if (IsHost) { if (victim.Slot.NetOwner > 0) Link.Send(victim.Slot.NetOwner, data, true); }
            else Link.Send(0, data, true);
        }

        static void ReadAfflict(int from, BinaryReader r, byte[] raw)
        {
            int token = r.ReadInt32();
            Seeker v = Game.SeekerOf(r.ReadByte());
            Seeker by = Game.SeekerOf(r.ReadByte());
            Combat.Affliction what = (Combat.Affliction)r.ReadByte();
            float seconds = Mathf.Clamp(r.ReadSingle(), 0f, 12f);
            if (token != RoundToken || !InRound || v == null) return;
            // Un invite ne lance de sort qu'avec SON joueur.
            if (Link.IsHost && from > 0 && (by == null || by.Slot.NetOwner != from)) return;
            if (!v.Remote) Combat.Afflict(v, what, seconds, by);
            else if (Link.IsHost && v.Slot.NetOwner > 0) Link.Send(v.Slot.NetOwner, raw, true);
        }

        static void ReadBlink(int from, BinaryReader r, byte[] raw)
        {
            int token = r.ReadInt32();
            Seeker v = Game.SeekerOf(r.ReadByte());
            Vector3 at = ReadVector(r);
            if (token != RoundToken || !InRound || v == null) return;
            if (!v.Remote)
            {
                IMover m = AbilityCaster.MoverOf(v);
                if (m != null) m.Blink(at);
            }
            else if (Link.IsHost && v.Slot.NetOwner > 0) Link.Send(v.Slot.NetOwner, raw, true);
        }

        // ================================================================== la Couronne (l'invite demande)

        /// <summary>
        /// UN INVITE DEMANDE, l'hote decide : "je la prends", "on me l'a fait tomber", "je l'ai
        /// lachee", "je suis tombe dans les nuages avec" (et ou etait mon dernier sol).
        /// </summary>
        public static void AskCrown(Ask what, Seeker s, Vector3 where)
        {
            if (!IsClient || s == null || s.Remote) return;
            // Une demande par quart de seconde et par sorte (on la redemande a chaque image sinon).
            if (Time.unscaledTime - lastAsk[(int)what] < 0.25f) return;
            lastAsk[(int)what] = Time.unscaledTime;
            Link.Send(0, Pack(w =>
            {
                w.Write((byte)Kind.CrownAsk);
                w.Write(RoundToken);
                w.Write((byte)what);
                w.Write((byte)s.Index);
                WriteVector(w, where);
            }), true);
        }
        static readonly float[] lastAsk = { -99f, -99f, -99f, -99f, -99f };

        static void ReadCrownAsk(int from, BinaryReader r)
        {
            int token = r.ReadInt32();
            Ask what = (Ask)r.ReadByte();
            int slot = r.ReadByte();
            Vector3 where = ReadVector(r);
            if (!Link.IsHost || token != RoundToken || !InRound || Crown.Instance == null) return;
            Seeker s = Owned(from, slot);
            if (s == null || s.Body == null) return;
            switch (what)
            {
                case Ask.Take:
                    // Il l'a touchee chez lui : on le croit, s'il n'en est pas a l'autre bout de l'ile.
                    if ((Crown.Position - s.Body.position).magnitude < 12f || (Crown.Position - where).magnitude < 4f)
                        Crown.Instance.TryTakeFor(s);
                    break;
                case Ask.KnockOff:
                    if (Crown.Holder == s) { Crown.KnockOff(s, where); Feed.CrownKnocked(s, null); }
                    break;
                case Ask.Slip:
                    Crown.Slip(s, where);
                    break;
                case Ask.FellWith:
                    Crown.FellWith(s, where);
                    break;
            }
        }

        // ================================================================== le choix des cartes

        /// <summary>L'invite choisit sa carte : il le dit a l'hote (une fois par tour).</summary>
        public static void SendPick(int slot, int card)
        {
            if (!IsClient || card < 0 || card >= Match.Draft.Offer.Count) return;
            int turnKey = Match.Draft.Stage * 100 + Match.Draft.Turn + Match.Played * 1000;
            if (pickSentFor == turnKey) return;
            pickSentFor = turnKey;
            Ability a = Match.Draft.Offer[card];
            Link.Send(0, Pack(w =>
            {
                w.Write((byte)Kind.Pick);
                w.Write((byte)slot);
                w.Write((byte)card);
                w.Write((byte)a);
            }), true);
        }

        static void ReadPick(int from, BinaryReader r)
        {
            int slot = r.ReadByte();
            int card = r.ReadByte();
            Ability a = (Ability)r.ReadByte();
            if (!Link.IsHost || !Match.Online || slot >= Match.Slots.Count || Match.Slots[slot].NetOwner != from) return;
            // La carte par son nom (sa place a pu bouger si deux choix se croisent).
            int at = Match.Draft.Offer.IndexOf(a);
            if (Match.Draft.TryPick(slot, at >= 0 ? at : card)) Sfx.Pop();
        }

        // ================================================================== lire

        static void Read(int from, byte[] data)
        {
            using (BinaryReader r = new BinaryReader(new MemoryStream(data), Encoding.UTF8))
            {
                Kind kind = (Kind)r.ReadByte();
                bool host = Link.IsHost;
                switch (kind)
                {
                    case Kind.State: if (!host && from == 0) ReadState(r); break;
                    case Kind.Snapshot: if (!host && from == 0) ReadSnapshot(r); break;
                    case Kind.Self: if (host) ReadSelf(from, r); break;
                    case Kind.Hit: ReadHit(from, r); break;
                    case Kind.Blink: ReadBlink(from, r, data); break;
                    case Kind.Afflict: ReadAfflict(from, r, data); break;
                    case Kind.CrownAsk: ReadCrownAsk(from, r); break;
                    case Kind.Pick: ReadPick(from, r); break;
                }
            }
        }

        // ================================================================== outils

        static byte[] Pack(System.Action<BinaryWriter> body)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8))
            {
                body.Invoke(w);
                w.Flush();
                return ms.ToArray();
            }
        }

        static void WriteVector(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        static Vector3 ReadVector(BinaryReader r) { return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); }
        static ushort Yaw16(float yaw) { return (ushort)Mathf.RoundToInt(Mathf.Repeat(yaw, 360f) / 360f * 65535f); }
        static float Yaw(ushort v) { return v / 65535f * 360f; }

        static void WriteInts(BinaryWriter w, List<int> list)
        {
            w.Write((byte)list.Count);
            for (int i = 0; i < list.Count; i++) w.Write((sbyte)list[i]);
        }

        static List<int> ReadInts(BinaryReader r)
        {
            int n = r.ReadByte();
            List<int> list = new List<int>();
            for (int i = 0; i < n; i++) list.Add(r.ReadSByte());
            return list;
        }

        static void WriteAbilities(BinaryWriter w, List<Ability> list)
        {
            w.Write((byte)list.Count);
            for (int i = 0; i < list.Count; i++) w.Write((byte)list[i]);
        }

        static List<Ability> ReadAbilities(BinaryReader r)
        {
            int n = r.ReadByte();
            List<Ability> list = new List<Ability>();
            for (int i = 0; i < n; i++)
            {
                int a = r.ReadByte();
                if (a < AbilityInfo.Count) list.Add((Ability)a);
            }
            return list;
        }
    }
}
