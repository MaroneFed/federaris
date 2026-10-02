using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Fief.Net
{
    /// <summary>
    /// LE RESEAU, ETAPE 1 (04/10 -- Martin : "tout est parfait, donc on peut faire le en ligne").
    ///
    /// Ce fichier est du C# PUR (pas une ligne d'Unity) : il ouvre une "prise" UDP, envoie et
    /// recoit des messages, tient la liste des joueurs. On peut donc le tester HORS d'Unity
    /// (Tools/reseau : un hote et deux invites qui se parlent sur le meme PC).
    ///
    /// Le modele (docs/RESEAU.md) : un HOTE (listen-server) et jusqu'a sept INVITES.
    ///   - l'invite dit "BONJOUR" (sa version, son pseudo) jusqu'a ce que l'hote reponde ;
    ///   - l'hote lui donne sa PLACE et la LISTE des joueurs, et la renvoie a tous des qu'elle
    ///     change (et toutes les secondes : un message UDP peut se perdre, on le redit) ;
    ///   - chacun "pingue" l'autre chaque seconde ; six secondes sans nouvelles : il est parti.
    ///
    /// Concept : UDP envoie des paquets sans garantie (ils peuvent se perdre ou arriver dans
    /// le desordre) mais vite -- c'est ce qu'utilisent les jeux. Ce qui doit arriver a coup sur,
    /// on le REPETE jusqu'a la reponse. Plus tard (Steam), seul ce "tuyau" change : les
    /// messages et la logique restent.
    /// </summary>
    public sealed class NetLink : IDisposable
    {
        public const int Version = 3;     // 3 (05/10) : 39 capacites ; 2 (04/10) : les messages du jeu
        public const int DefaultPort = 7777;
        const uint Magic = 0x46494546;           // "FIEF"
        const float PingEvery = 1f;
        const float RosterEvery = 1f;
        const float HelloEvery = 0.5f;
        const float Timeout = 6f;

        public enum State { Idle, Hosting, Connecting, Connected, Refused, Lost, Closed }
        public enum Refusal : byte { None = 0, Full = 1, BadVersion = 2, Started = 3 }

        enum Msg : byte { Hello = 1, Welcome = 2, Roster = 3, Ping = 4, Pong = 5, Bye = 6, Refused = 7, Data = 8, Reliable = 9, Ack = 10 }

        /// <summary>
        /// UN MESSAGE DU JEU recu (etape 2) : qui l'envoie (sa place ; 0 = l'hote) et son contenu.
        /// NetLink ne sait pas ce qu'il y a dedans : c'est NetGame qui l'ecrit et le lit.
        /// </summary>
        public struct Incoming
        {
            public int From;
            public byte[] Data;
        }

        /// <summary>Les messages du jeu arrives, dans l'ordre d'arrivee (NetGame les vide a chaque image).</summary>
        public readonly Queue<Incoming> Inbox = new Queue<Incoming>();

        /// <summary>
        /// Un message FIABLE en attente de son accuse de reception : on le renvoie toutes les
        /// 0,2 s jusqu'a ce que l'autre dise "recu" (un coup, un choix de carte ne doit pas se perdre).
        /// </summary>
        sealed class Pending
        {
            public EndPoint To;
            public uint Seq;
            public byte[] Data;
            public float SentAt;
            public float FirstAt;
        }
        readonly List<Pending> pending = new List<Pending>();
        uint nextSeq = 1;
        const float ResendEvery = 0.2f;
        // Les numeros deja recus (par expediteur) : un message renvoye n'est traite qu'une fois.
        readonly Dictionary<string, HashSet<uint>> seen = new Dictionary<string, HashSet<uint>>();
        readonly Dictionary<string, Queue<uint>> seenOrder = new Dictionary<string, Queue<uint>>();

        /// <summary>Un joueur dans le salon : sa place (0 : l'hote) et son pseudo.</summary>
        public sealed class Member
        {
            public int Slot;
            public string Name;
            public float Ping;      // ms, vu par l'hote (0 pour l'hote lui-meme)
        }

        sealed class Peer
        {
            public EndPoint Address;
            public int Slot;
            public string Name;
            public float LastHeard;
            public float PingSentAt;
            public float Ping;
        }

        Socket socket;
        readonly byte[] buffer = new byte[2048];
        EndPoint host;
        readonly List<Peer> peers = new List<Peer>();
        int maxPlayers = 8;
        float lastHeardHost, lastHello, lastPing, lastRoster, pingSentAt;

        public State Status { get; private set; }
        public Refusal RefusedFor { get; private set; }
        public bool IsHost { get; private set; }
        public int MySlot { get; private set; }
        public string MyName { get; private set; }
        /// <summary>Le salon : tous les joueurs, l'hote compris, dans l'ordre des places.</summary>
        public readonly List<Member> Roster = new List<Member>();
        /// <summary>Le ping vers l'hote (ms), cote invite.</summary>
        public float PingToHost { get; private set; }
        /// <summary>L'hote n'accepte plus personne (le match a commence).</summary>
        public bool Locked;
        /// <summary>Change a chaque fois que la liste des joueurs change (le menu s'en sert).</summary>
        public int RosterVersion { get; private set; }

        NetLink() { Status = State.Idle; }

        /// <summary>L'heure du dernier Poll (les messages fiables s'en servent pour savoir quand renvoyer).</summary>
        float lastClock;

        // ================================================================== ouvrir

        /// <summary>HEBERGER : on ecoute sur "port", on est la place 0.</summary>
        public static NetLink Host(int port, string name, int maxPlayers)
        {
            NetLink l = new NetLink();
            l.IsHost = true;
            l.MyName = Clean(name);
            l.MySlot = 0;
            l.maxPlayers = Math.Max(2, Math.Min(8, maxPlayers));
            l.socket = Open(port);
            l.Status = State.Hosting;
            l.RebuildRoster();
            return l;
        }

        /// <summary>REJOINDRE l'hote a "address" (une adresse IP, ou un nom de machine).</summary>
        public static NetLink Join(string address, int port, string name, float now)
        {
            NetLink l = new NetLink();
            l.IsHost = false;
            l.MyName = Clean(name);
            l.MySlot = -1;
            l.host = new IPEndPoint(Resolve(address), port);
            l.socket = Open(0);
            l.Status = State.Connecting;
            l.lastHeardHost = now;
            l.lastHello = -99f;
            return l;
        }

        static Socket Open(int port)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Blocking = false;
            // (Windows) Un paquet vers une adresse qui ne repond pas renvoie une erreur "connexion
            // reinitialisee" au prochain Receive : on ignore ce cas plus bas.
            s.Bind(new IPEndPoint(IPAddress.Any, port));
            return s;
        }

        static IPAddress Resolve(string address)
        {
            IPAddress ip;
            if (IPAddress.TryParse(address.Trim(), out ip)) return ip;
            IPAddress[] all = Dns.GetHostAddresses(address.Trim());
            for (int i = 0; i < all.Length; i++) if (all[i].AddressFamily == AddressFamily.InterNetwork) return all[i];
            throw new ArgumentException("adresse inconnue : " + address);
        }

        static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Joueur";
            name = name.Trim();
            return name.Length > 16 ? name.Substring(0, 16) : name;
        }

        /// <summary>Les adresses de cette machine sur le reseau local (a donner a l'ami qui rejoint).</summary>
        public static List<string> LocalAddresses()
        {
            List<string> list = new List<string>();
            try
            {
                IPAddress[] all = Dns.GetHostAddresses(Dns.GetHostName());
                for (int i = 0; i < all.Length; i++)
                    if (all[i].AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(all[i])) list.Add(all[i].ToString());
            }
            catch (Exception) { }
            if (list.Count == 0) list.Add("127.0.0.1");
            return list;
        }

        // ================================================================== chaque image

        /// <summary>A appeler a chaque image : lit ce qui est arrive, repete ce qui doit l'etre. "now" en secondes.</summary>
        public void Poll(float now)
        {
            if (socket == null) return;
            lastClock = now;
            Receive(now);
            if (IsHost) HostTick(now);
            else ClientTick(now);
            Resend(now);
        }

        void Resend(float now)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Pending p = pending[i];
                if (now - p.FirstAt > Timeout) { pending.RemoveAt(i); continue; }
                if (now - p.SentAt < ResendEvery) continue;
                p.SentAt = now;
                SendRaw(p.To, Msg.Reliable, p.Seq, p.Data);
            }
        }

        void HostTick(float now)
        {
            for (int i = peers.Count - 1; i >= 0; i--)
            {
                if (now - peers[i].LastHeard > Timeout)
                {
                    peers.RemoveAt(i);
                    RebuildRoster();
                    lastRoster = -99f;
                }
            }
            if (now - lastPing > PingEvery)
            {
                lastPing = now;
                for (int i = 0; i < peers.Count; i++)
                {
                    peers[i].PingSentAt = now;
                    Send(peers[i].Address, Msg.Ping, w => w.Write(now));
                }
            }
            if (now - lastRoster > RosterEvery)
            {
                lastRoster = now;
                for (int i = 0; i < peers.Count; i++) SendRoster(peers[i].Address, Msg.Roster, peers[i].Slot);
            }
        }

        void ClientTick(float now)
        {
            if (Status == State.Connecting && now - lastHello > HelloEvery)
            {
                lastHello = now;
                Send(host, Msg.Hello, w => { w.Write(Version); w.Write(MyName); });
            }
            if (Status == State.Connected && now - lastPing > PingEvery)
            {
                lastPing = now;
                pingSentAt = now;
                Send(host, Msg.Ping, w => w.Write(now));
            }
            if ((Status == State.Connecting || Status == State.Connected) && now - lastHeardHost > Timeout)
                Status = State.Lost;
        }

        // ================================================================== recevoir

        void Receive(float now)
        {
            for (int guard = 0; guard < 256; guard++)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int n;
                try
                {
                    if (socket.Available <= 0) return;
                    n = socket.ReceiveFrom(buffer, ref from);
                }
                catch (SocketException e)
                {
                    // 10054 (Windows) : la machine d'en face a ferme -- on continue d'ecouter.
                    if (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.WouldBlock) continue;
                    return;
                }
                if (n < 5) continue;
                try
                {
                    using (BinaryReader r = new BinaryReader(new MemoryStream(buffer, 0, n), Encoding.UTF8))
                    {
                        if (r.ReadUInt32() != Magic) continue;
                        Msg type = (Msg)r.ReadByte();
                        if (IsHost) HostReceive(type, r, from, now);
                        else if (SameAddress(from, host)) ClientReceive(type, r, now);
                    }
                }
                catch (Exception) { /* un paquet abime : on l'ignore */ }
            }
        }

        static bool SameAddress(EndPoint a, EndPoint b)
        {
            IPEndPoint x = a as IPEndPoint, y = b as IPEndPoint;
            if (x == null || y == null) return false;
            if (x.Port != y.Port) return false;
            return x.Address.Equals(y.Address) || IPAddress.IsLoopback(x.Address) && IPAddress.IsLoopback(y.Address);
        }

        Peer PeerAt(EndPoint from)
        {
            for (int i = 0; i < peers.Count; i++) if (peers[i].Address.Equals(from)) return peers[i];
            return null;
        }

        void HostReceive(Msg type, BinaryReader r, EndPoint from, float now)
        {
            Peer p = PeerAt(from);
            if (p != null) p.LastHeard = now;
            switch (type)
            {
                case Msg.Hello:
                {
                    int version = r.ReadInt32();
                    string name = Clean(r.ReadString());
                    if (p != null) { SendRoster(from, Msg.Welcome, p.Slot); return; }   // il n'a pas recu la reponse : on la redit
                    if (version != Version) { Send(from, Msg.Refused, w => w.Write((byte)Refusal.BadVersion)); return; }
                    if (Locked) { Send(from, Msg.Refused, w => w.Write((byte)Refusal.Started)); return; }
                    if (peers.Count + 1 >= maxPlayers) { Send(from, Msg.Refused, w => w.Write((byte)Refusal.Full)); return; }
                    p = new Peer();
                    p.Address = from;
                    p.Name = Unique(name);
                    p.Slot = FreeSlot();
                    p.LastHeard = now;
                    peers.Add(p);
                    RebuildRoster();
                    SendRoster(from, Msg.Welcome, p.Slot);
                    lastRoster = -99f;      // tout le monde recoit la nouvelle liste tout de suite
                    break;
                }
                case Msg.Ping:
                {
                    float t = r.ReadSingle();
                    Send(from, Msg.Pong, w => w.Write(t));
                    break;
                }
                case Msg.Pong:
                    if (p != null) p.Ping = (now - p.PingSentAt) * 1000f;
                    break;
                case Msg.Bye:
                    if (p != null) { peers.Remove(p); RebuildRoster(); lastRoster = -99f; }
                    break;
                case Msg.Data:
                case Msg.Reliable:
                case Msg.Ack:
                    if (p != null) ReceiveData(type, r, from, p.Slot);
                    break;
            }
        }

        /// <summary>Un message du jeu : on le range dans la boite (un fiable : on accuse reception, une fois).</summary>
        void ReceiveData(Msg type, BinaryReader r, EndPoint from, int fromSlot)
        {
            if (type == Msg.Ack)
            {
                uint acked = r.ReadUInt32();
                for (int i = pending.Count - 1; i >= 0; i--)
                    if (pending[i].Seq == acked && SameAddress(pending[i].To, from)) pending.RemoveAt(i);
                return;
            }
            if (type == Msg.Reliable)
            {
                uint seq = r.ReadUInt32();
                Send(from, Msg.Ack, w => w.Write(seq));
                if (!FirstTime(from, seq)) return;
            }
            int length = r.ReadUInt16();
            byte[] data = r.ReadBytes(length);
            if (data.Length != length) return;
            Incoming m;
            m.From = fromSlot;
            m.Data = data;
            Inbox.Enqueue(m);
        }

        bool FirstTime(EndPoint from, uint seq)
        {
            string key = from.ToString();
            HashSet<uint> set;
            Queue<uint> order;
            if (!seen.TryGetValue(key, out set)) { set = new HashSet<uint>(); seen[key] = set; seenOrder[key] = new Queue<uint>(); }
            order = seenOrder[key];
            if (set.Contains(seq)) return false;
            set.Add(seq);
            order.Enqueue(seq);
            while (order.Count > 2048) set.Remove(order.Dequeue());
            return true;
        }

        void ClientReceive(Msg type, BinaryReader r, float now)
        {
            lastHeardHost = now;
            switch (type)
            {
                case Msg.Welcome:
                case Msg.Roster:
                {
                    int mine = r.ReadByte();
                    ReadRoster(r);
                    MySlot = mine;
                    if (Status == State.Connecting) Status = State.Connected;
                    break;
                }
                case Msg.Refused:
                    RefusedFor = (Refusal)r.ReadByte();
                    Status = State.Refused;
                    break;
                case Msg.Ping:
                {
                    float t = r.ReadSingle();
                    Send(host, Msg.Pong, w => w.Write(t));
                    break;
                }
                case Msg.Pong:
                    PingToHost = (now - pingSentAt) * 1000f;
                    break;
                case Msg.Bye:
                    Status = State.Lost;
                    break;
                case Msg.Data:
                case Msg.Reliable:
                case Msg.Ack:
                    ReceiveData(type, r, host, 0);
                    break;
            }
        }

        // ================================================================== la liste

        int FreeSlot()
        {
            for (int s = 1; s < maxPlayers; s++)
            {
                bool used = false;
                for (int i = 0; i < peers.Count; i++) if (peers[i].Slot == s) used = true;
                if (!used) return s;
            }
            return peers.Count + 1;
        }

        /// <summary>Deux "Martin" dans le salon : le second devient "Martin 2".</summary>
        string Unique(string name)
        {
            string candidate = name;
            for (int k = 2; k < 10; k++)
            {
                bool taken = string.Equals(candidate, MyName, StringComparison.OrdinalIgnoreCase);
                for (int i = 0; i < peers.Count; i++) if (string.Equals(peers[i].Name, candidate, StringComparison.OrdinalIgnoreCase)) taken = true;
                if (!taken) return candidate;
                candidate = name + " " + k;
            }
            return candidate;
        }

        void RebuildRoster()
        {
            Roster.Clear();
            Member me = new Member();
            me.Slot = 0;
            me.Name = MyName;
            Roster.Add(me);
            List<Peer> sorted = new List<Peer>(peers);
            sorted.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            for (int i = 0; i < sorted.Count; i++)
            {
                Member m = new Member();
                m.Slot = sorted[i].Slot;
                m.Name = sorted[i].Name;
                m.Ping = sorted[i].Ping;
                Roster.Add(m);
            }
            RosterVersion++;
        }

        void SendRoster(EndPoint to, Msg type, int theirSlot)
        {
            Send(to, type, w =>
            {
                w.Write((byte)theirSlot);
                w.Write((byte)Roster.Count);
                for (int i = 0; i < Roster.Count; i++)
                {
                    w.Write((byte)Roster[i].Slot);
                    w.Write(Roster[i].Name);
                    w.Write((short)Math.Min(short.MaxValue, (int)Roster[i].Ping));
                }
            });
        }

        void ReadRoster(BinaryReader r)
        {
            int count = r.ReadByte();
            List<Member> fresh = new List<Member>();
            for (int i = 0; i < count; i++)
            {
                Member m = new Member();
                m.Slot = r.ReadByte();
                m.Name = r.ReadString();
                m.Ping = r.ReadInt16();
                fresh.Add(m);
            }
            bool changed = fresh.Count != Roster.Count;
            for (int i = 0; !changed && i < fresh.Count; i++)
                changed = fresh[i].Slot != Roster[i].Slot || fresh[i].Name != Roster[i].Name;
            Roster.Clear();
            Roster.AddRange(fresh);
            if (changed) RosterVersion++;
        }

        // ================================================================== les messages du jeu

        /// <summary>
        /// Envoyer un message du jeu a la place "toSlot" (cote invite : toujours a l'hote, "toSlot"
        /// est ignore). "reliable" : renvoye jusqu'a l'accuse de reception. 1200 octets au plus.
        /// </summary>
        public void Send(int toSlot, byte[] data, bool reliable)
        {
            if (data == null || data.Length > 1200) return;
            EndPoint to = null;
            if (!IsHost) to = host;
            else for (int i = 0; i < peers.Count; i++) if (peers[i].Slot == toSlot) to = peers[i].Address;
            if (to == null) return;
            SendTo(to, data, reliable);
        }

        /// <summary>Hote : envoyer a tous les invites (sauf "exceptSlot").</summary>
        public void Broadcast(byte[] data, bool reliable, int exceptSlot = -1)
        {
            if (!IsHost || data == null || data.Length > 1200) return;
            for (int i = 0; i < peers.Count; i++) if (peers[i].Slot != exceptSlot) SendTo(peers[i].Address, data, reliable);
        }

        /// <summary>Vrai si la place "slot" est tenue par un invite encore la (cote hote).</summary>
        public bool HasPeer(int slot)
        {
            for (int i = 0; i < peers.Count; i++) if (peers[i].Slot == slot) return true;
            return false;
        }

        /// <summary>Combien de messages fiables attendent encore leur accuse (pour les essais).</summary>
        public int PendingCount { get { return pending.Count; } }

        /// <summary>Pour les essais : perdre volontairement une partie des paquets envoyes (0 a 1).</summary>
        public double DropForTests;
        readonly Random lossRng = new Random(7);

        void SendTo(EndPoint to, byte[] data, bool reliable)
        {
            if (!reliable) { SendRaw(to, Msg.Data, 0, data); return; }
            Pending p = new Pending();
            p.To = to;
            p.Seq = nextSeq++;
            p.Data = data;
            p.SentAt = -99f;
            p.FirstAt = lastClock;
            pending.Add(p);
            // Parti tout de suite (le renvoi, lui, attend 0,2 s).
            p.SentAt = lastClock;
            SendRaw(to, Msg.Reliable, p.Seq, data);
        }

        void SendRaw(EndPoint to, Msg type, uint seq, byte[] data)
        {
            Send(to, type, w =>
            {
                if (type == Msg.Reliable) w.Write(seq);
                w.Write((ushort)data.Length);
                w.Write(data);
            });
        }

        // ================================================================== envoyer

        void Send(EndPoint to, Msg type, Action<BinaryWriter> body)
        {
            if (socket == null || to == null) return;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8))
                {
                    w.Write(Magic);
                    w.Write((byte)type);
                    if (body != null) body.Invoke(w);
                    w.Flush();
                    if (DropForTests > 0 && (type == Msg.Data || type == Msg.Reliable || type == Msg.Ack) && lossRng.NextDouble() < DropForTests) return;
                    socket.SendTo(ms.GetBuffer(), 0, (int)ms.Length, SocketFlags.None, to);
                }
            }
            catch (Exception) { /* le reseau a eternue : le prochain envoi reessaiera */ }
        }

        /// <summary>Partir proprement : on previent les autres (sinon ils attendront six secondes).</summary>
        public void Dispose()
        {
            if (socket == null) return;
            if (IsHost) for (int i = 0; i < peers.Count; i++) Send(peers[i].Address, Msg.Bye, null);
            else if (host != null) Send(host, Msg.Bye, null);
            try { socket.Close(); } catch (Exception) { }
            socket = null;
            Status = State.Closed;
        }
    }
}
