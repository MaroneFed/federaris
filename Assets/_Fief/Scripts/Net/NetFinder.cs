using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Fief.Net
{
    /// <summary>
    /// LA LISTE DES PARTIES (06/10 -- Martin : "un truc comme dans FPS Chess, ou t'as toutes les
    /// games qui sont la et que tu peux rejoindre ; c'est chiant de mettre ton IP").
    ///
    /// Sur l'ecran En ligne, le jeu CRIE sur le reseau toutes les 1,5 s : "il y a une partie ?"
    /// (un paquet UDP en "broadcast", que toutes les machines du reseau recoivent). Chaque hote
    /// repond : son pseudo, combien de joueurs, s'il a deja lance. On les affiche ; un clic, on
    /// rejoint. Aucun serveur : ca marche sur le meme reseau (la box, le wifi) et sur un reseau
    /// virtuel qui imite un reseau local (Radmin VPN). Pour INTERNET, il faudra Steam (ses
    /// "lobbies" font exactement ca a l'echelle du monde -- voir docs/RESEAU.md, etape 4).
    ///
    /// Du C# PUR, comme NetLink : teste hors d'Unity (Tools/reseau).
    /// </summary>
    public sealed class NetFinder : IDisposable
    {
        /// <summary>Une partie trouvee.</summary>
        public sealed class Found
        {
            public string Address;
            public string Host;
            public int Players;
            public int Max;
            public bool Started;
            public bool SameVersion;
            public float SeenAt;
            /// <summary>Son code (le meme que l'hote affiche dans son salon).</summary>
            public string Code { get { return NetCode.Encode(Address); } }
        }

        public const float AskEvery = 1.5f;
        public const float ForgetAfter = 4.5f;

        /// <summary>Les parties entendues, la plus ancienne d'abord (l'ordre ne saute pas).</summary>
        public readonly List<Found> Games = new List<Found>();

        Socket socket;
        readonly int port;
        readonly byte[] buffer = new byte[1024];
        float lastAsk = -99f;
        /// <summary>Change quand la liste change (le menu s'en sert).</summary>
        public int Version { get; private set; }

        NetFinder(int port) { this.port = port; }

        /// <summary>Commencer a chercher les parties qui ecoutent sur "port".</summary>
        public static NetFinder Start(int port)
        {
            NetFinder f = new NetFinder(port);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Blocking = false;
            s.EnableBroadcast = true;
            s.Bind(new IPEndPoint(IPAddress.Any, 0));
            f.socket = s;
            return f;
        }

        /// <summary>A chaque image : redemander de temps en temps, lire les reponses, oublier les parties muettes.</summary>
        public void Poll(float now)
        {
            if (socket == null) return;
            if (now - lastAsk > AskEvery)
            {
                lastAsk = now;
                Ask(IPAddress.Broadcast);
                // Plusieurs cartes reseau (le wifi, Radmin...) : on crie aussi sur chacune.
                foreach (IPAddress b in DirectedBroadcasts()) Ask(b);
                Ask(IPAddress.Loopback);     // deux fenetres du jeu sur le meme PC
            }
            Receive(now);
            for (int i = Games.Count - 1; i >= 0; i--)
                if (now - Games[i].SeenAt > ForgetAfter) { Games.RemoveAt(i); Version++; }
        }

        void Ask(IPAddress to)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8))
                {
                    w.Write(NetLink.WireMagic);
                    w.Write(NetLink.QueryType);
                    w.Write(NetLink.Version);
                    w.Flush();
                    socket.SendTo(ms.GetBuffer(), 0, (int)ms.Length, SocketFlags.None, new IPEndPoint(to, port));
                }
            }
            catch (Exception) { /* pas de reseau : on reessaiera */ }
        }

        void Receive(float now)
        {
            for (int guard = 0; guard < 64; guard++)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int n;
                try
                {
                    if (socket.Available <= 0) return;
                    n = socket.ReceiveFrom(buffer, ref from);
                }
                catch (SocketException) { continue; }
                catch (Exception) { return; }
                if (n < 6) continue;
                try
                {
                    using (BinaryReader r = new BinaryReader(new MemoryStream(buffer, 0, n), Encoding.UTF8))
                    {
                        if (r.ReadUInt32() != NetLink.WireMagic || r.ReadByte() != NetLink.InfoType) continue;
                        int version = r.ReadInt32();
                        string host = r.ReadString();
                        int players = r.ReadByte();
                        int max = r.ReadByte();
                        bool started = r.ReadBoolean();
                        string address = ((IPEndPoint)from).Address.ToString();
                        Found g = Games.Find(x => x.Address == address);
                        // La meme partie entendue par la boucle locale ET par le reseau : on la garde une fois.
                        bool loop = IPAddress.IsLoopback(((IPEndPoint)from).Address);
                        if (g == null) g = Games.Find(x => x.Host == host && (loop || x.Address == "127.0.0.1"));
                        // On garde l'adresse du reseau (plutot que 127.0.0.1) : elle marche aussi pour les autres.
                        if (g != null && !loop && g.Address == "127.0.0.1") g.Address = address;
                        if (g == null)
                        {
                            g = new Found();
                            g.Address = address;
                            Games.Add(g);
                            Version++;
                        }
                        else if (g.Players != players || g.Started != started || g.Host != host) Version++;
                        g.Host = host;
                        g.Players = players;
                        g.Max = max;
                        g.Started = started;
                        g.SameVersion = version == NetLink.Version;
                        g.SeenAt = now;
                    }
                }
                catch (Exception) { /* un paquet abime */ }
            }
        }

        /// <summary>L'adresse de "broadcast" de chaque carte reseau (en supposant un reseau en /24, le cas des box).</summary>
        static List<IPAddress> DirectedBroadcasts()
        {
            List<IPAddress> list = new List<IPAddress>();
            foreach (string a in NetLink.LocalAddresses())
            {
                IPAddress ip;
                if (!IPAddress.TryParse(a, out ip) || IPAddress.IsLoopback(ip)) continue;
                byte[] b = ip.GetAddressBytes();
                b[3] = 255;
                list.Add(new IPAddress(b));
            }
            return list;
        }

        public void Dispose()
        {
            if (socket == null) return;
            try { socket.Close(); } catch (Exception) { }
            socket = null;
        }
    }

    /// <summary>
    /// LE CODE DE PARTIE (06/10 -- "c'est chiant de mettre ton IP, trouve un code") : l'adresse de
    /// l'hote, ecrite en 7 lettres et chiffres faciles a dicter (ni O ni 0, ni I ni 1) : "K7Q-2MXA".
    /// C'est la MEME chose qu'une adresse IP, juste plus courte a dire au telephone : il faut
    /// toujours etre sur le meme reseau (ou sur Radmin VPN). Le champ "Rejoindre" accepte le code
    /// comme l'adresse.
    /// </summary>
    public static class NetCode
    {
        const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";   // 32 signes

        /// <summary>Le code d'une adresse IPv4 ("192.168.1.23" -> "8FK-J2QX"), ou "" si ce n'en est pas une.</summary>
        public static string Encode(string address)
        {
            IPAddress ip;
            if (string.IsNullOrEmpty(address) || !IPAddress.TryParse(address.Trim(), out ip) || ip.AddressFamily != AddressFamily.InterNetwork) return "";
            byte[] b = ip.GetAddressBytes();
            uint v = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
            // On melange les bits (pour que deux PC de la meme box n'aient pas des codes presque pareils).
            v = Mix(v);
            char[] c = new char[7];
            for (int i = 6; i >= 0; i--) { c[i] = Alphabet[(int)(v % 32)]; v /= 32; }
            return new string(c, 0, 3) + "-" + new string(c, 3, 4);
        }

        /// <summary>L'adresse d'un code ; null si ce n'est pas un code (alors c'est peut-etre une IP).</summary>
        public static string Decode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            StringBuilder sb = new StringBuilder();
            foreach (char ch in code.ToUpperInvariant())
            {
                if (ch == '-' || ch == ' ') continue;
                char k = ch == 'O' ? '0' : ch == 'I' ? '1' : ch;
                if (Alphabet.IndexOf(k) < 0) return null;
                sb.Append(k);
            }
            if (sb.Length != 7) return null;
            ulong v = 0;
            for (int i = 0; i < 7; i++) v = v * 32 + (ulong)Alphabet.IndexOf(sb[i]);
            if (v > uint.MaxValue) return null;
            uint ip = Unmix((uint)v);
            return ((ip >> 24) & 255) + "." + ((ip >> 16) & 255) + "." + ((ip >> 8) & 255) + "." + (ip & 255);
        }

        /// <summary>Une adresse a partir de ce que le joueur a tape : un code, ou une adresse telle quelle.</summary>
        public static string ToAddress(string typed)
        {
            string fromCode = Decode(typed);
            return fromCode ?? (typed ?? "").Trim();
        }

        // Un melange reversible : XOR avec une constante, puis rotation de 13 bits.
        const uint Key = 0x5A3C96E1;
        static uint Mix(uint v) { v ^= Key; return (v << 13) | (v >> 19); }
        static uint Unmix(uint v) { v = (v >> 13) | (v << 19); return v ^ Key; }
    }
}
