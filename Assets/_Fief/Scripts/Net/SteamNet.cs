#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
#define FIEF_STEAM
#endif
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using Fief.Net;
#if FIEF_STEAM
using Steamworks;
#endif

namespace Fief
{
    /// <summary>
    /// LE VRAI JEU EN LIGNE : PAR STEAM (09/10, v31 -- Martin : "fais le vrai mode en ligne").
    ///
    /// Jusqu'ici, on ne jouait ensemble que sur le meme reseau (la box de la maison, ou Radmin
    /// VPN) : une box refuse les paquets qui viennent d'Internet sans qu'on les ait demandes.
    /// Steam regle ca : ses SERVEURS RELAIS (Steam Datagram Relay) font passer les paquets entre
    /// deux joueurs n'importe ou dans le monde, a travers toutes les box, sans rien ouvrir.
    ///
    ///   - HEBERGER SUR INTERNET : on cree un "lobby" Steam (un salon public que Steam garde dans
    ///     sa liste mondiale), avec ton pseudo, la version du jeu et un CODE de 6 signes ;
    ///   - la LISTE : l'ecran En ligne demande a Steam tous les salons FIEF du monde ;
    ///   - REJOINDRE : un clic dans la liste, le code tape, ou l'invitation d'un ami (Steam ▸ Amis
    ///     ▸ "Rejoindre la partie", ou le bouton "Inviter un ami" qui ouvre l'overlay de Steam) ;
    ///   - ensuite, c'est EXACTEMENT le meme jeu qu'en reseau local : NetLink et NetGame ne
    ///     changent pas, seul le TUYAU change (SteamWire au lieu d'une prise UDP).
    ///
    /// Il faut STEAM OUVERT sur les deux PC. Le jeu se presente a Steam comme l'appli 480
    /// ("Spacewar", l'appli d'essai de Valve que tout le monde peut utiliser : steam_appid.txt
    /// a la racine du projet). Pour la vraie sortie : un numero a nous (Steam Direct, Phase 5).
    ///
    /// Le paquet Steamworks.NET (MIT) est range dans Packages/com.rlabrecque.steamworks.net :
    /// Unity le charge tout seul, pas besoin de Git ni du Package Manager.
    /// </summary>
    public static class SteamNet
    {
        /// <summary>Un salon FIEF vu dans la liste mondiale de Steam.</summary>
        public sealed class Lobby
        {
            public ulong Id;
            public string Host;
            public string Code;
            public int Players;
            public int Max;
            public bool Started;
            public bool SameVersion;
        }

        /// <summary>Les salons FIEF du monde entier (la derniere reponse de Steam).</summary>
        public static readonly List<Lobby> Lobbies = new List<Lobby>();
        /// <summary>Steam est ouvert et nous a reconnus.</summary>
        public static bool Ready { get; private set; }
        /// <summary>Pourquoi Steam n'est pas la (a montrer sur le bouton).</summary>
        public static string Why { get; private set; }
        /// <summary>Le code de NOTRE salon Steam (6 signes), quand on heberge sur Internet.</summary>
        public static string Code { get; private set; }
        /// <summary>Une invitation acceptee (Steam ▸ Amis) : le menu passe a l'ecran En ligne.</summary>
        public static bool Invited;
        /// <summary>Vrai tant qu'on attend Steam (creation, entree dans un salon, recherche d'un code).</summary>
        public static bool Busy { get; private set; }

        // Pas de I ni de O (on les confond avec 1 et 0), pas de 0 ni de 1 non plus.
        const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        const string Key = "fief";
        static float wantListUntil, nextList;
        static bool booted;

        /// <summary>"ABC123" : ce que le joueur a tape ressemble-t-il a un code Steam (6 signes) ?</summary>
        public static bool LooksLikeCode(string typed)
        {
            string c = CleanCode(typed);
            return c != null && c.Length == 6;
        }

        static string CleanCode(string typed)
        {
            if (string.IsNullOrEmpty(typed)) return null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char ch in typed.ToUpperInvariant())
            {
                if (ch == ' ' || ch == '-') continue;
                char k = ch == 'O' ? '0' : ch == 'I' ? '1' : ch;
                if (Alphabet.IndexOf(k) < 0) return null;
                sb.Append(k);
            }
            return sb.ToString();
        }

        static string NewCode()
        {
            char[] c = new char[6];
            for (int i = 0; i < 6; i++) c[i] = Alphabet[UnityEngine.Random.Range(0, Alphabet.Length)];
            return new string(c);
        }

        /// <summary>L'ecran En ligne le demande a chaque image : on garde la liste mondiale a jour.</summary>
        public static void WantList(float now)
        {
            wantListUntil = now + 0.5f;
        }

#if FIEF_STEAM
        static ulong lobby;
        static bool hosting;
        static bool lastLocked;
        static Callback<SteamNetworkingMessagesSessionRequest_t> sessionRequest;
        static Callback<GameLobbyJoinRequested_t> joinRequested;
        static CallResult<LobbyCreated_t> created;
        static CallResult<LobbyEnter_t> entered;
        static CallResult<LobbyMatchList_t> listed;
        static CallResult<LobbyMatchList_t> coded;

        /// <summary>Au lancement du jeu : on dit bonjour a Steam (s'il est ouvert).</summary>
        public static void Boot()
        {
            if (booted) return;
            booted = true;
            try
            {
                if (!Packsize.Test() || !DllCheck.Test()) { Why = "Steam : fichiers du jeu abîmés"; return; }
                string message;
                ESteamAPIInitResult result = SteamAPI.InitEx(out message);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Why = result == ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient ? "Ouvre Steam pour jouer sur Internet" : "Steam : " + message;
                    Debug.Log("[FIEF] Steam absent (" + result + ") : " + message + " -- le jeu en reseau local marche quand meme.");
                    return;
                }
            }
            catch (Exception e)
            {
                // DllNotFoundException : la bibliotheque de Steam n'a pas ete trouvee.
                Why = "Steam introuvable";
                Debug.LogWarning("[FIEF] Steam : " + e.Message);
                return;
            }
            Ready = true;
            Why = null;
            SteamNetworkingUtils.InitRelayNetworkAccess();
            // Quelqu'un veut nous parler (un invite qui frappe a la porte de l'hote) : on accepte
            // tant qu'on est dans un salon Steam.
            sessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(r =>
            {
                if (SteamWire.Current == null) return;
                SteamNetworkingIdentity who = r.m_identityRemote;
                SteamNetworkingMessages.AcceptSessionWithUser(ref who);
            });
            // Une invitation acceptee dans Steam (ou "Rejoindre la partie" sur un ami).
            joinRequested = Callback<GameLobbyJoinRequested_t>.Create(r =>
            {
                Invited = true;
                NetSession.JoinInternet(r.m_steamIDLobby.m_SteamID);
            });
            created = CallResult<LobbyCreated_t>.Create(OnCreated);
            entered = CallResult<LobbyEnter_t>.Create(OnEntered);
            listed = CallResult<LobbyMatchList_t>.Create(OnListed);
            coded = CallResult<LobbyMatchList_t>.Create(OnCoded);
            Debug.Log("[FIEF] Steam pret : " + SteamFriends.GetPersonaName());

            // Le jeu lance par une invitation alors qu'il etait ferme : "+connect_lobby <numero>".
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                ulong id;
                if (args[i] == "+connect_lobby" && ulong.TryParse(args[i + 1], out id)) { Invited = true; NetSession.JoinInternet(id); }
            }
        }

        /// <summary>A chaque image : les reponses de Steam, et la liste mondiale si l'ecran la demande.</summary>
        public static void Pump(float now)
        {
            if (!Ready) return;
            SteamAPI.RunCallbacks();
            NetLink link = NetSession.Link;
            // L'hote : "deja commence" dans la liste quand le match est lance.
            if (hosting && lobby != 0 && link != null && link.Locked != lastLocked)
            {
                lastLocked = link.Locked;
                SteamMatchmaking.SetLobbyData(new CSteamID(lobby), "started", lastLocked ? "1" : "0");
            }
            if (now < wantListUntil && now > nextList && !listed.IsActive())
            {
                nextList = now + 3f;
                Filter();
                listed.Set(SteamMatchmaking.RequestLobbyList());
            }
        }

        static void Filter()
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter(Key, "1", ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(30);
        }

        /// <summary>HEBERGER SUR INTERNET : le salon Steam, puis le meme lien que d'habitude.</summary>
        public static NetLink Host(string name, int maxPlayers)
        {
            if (!Ready) return null;
            hosting = true;
            lastLocked = false;
            Code = NewCode();
            Busy = true;
            created.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers));
            return NetLink.HostOver(new SteamWire(), name, maxPlayers);
        }

        static void OnCreated(LobbyCreated_t r, bool failure)
        {
            Busy = false;
            if (failure || r.m_eResult != EResult.k_EResultOK || NetSession.Link == null || !hosting)
            {
                if (r.m_ulSteamIDLobby != 0) SteamMatchmaking.LeaveLobby(new CSteamID(r.m_ulSteamIDLobby));
                if (hosting) { NetSession.Leave(); NetSession.Report("Steam n'a pas pu créer la partie (" + r.m_eResult + ")."); }
                return;
            }
            lobby = r.m_ulSteamIDLobby;
            CSteamID id = new CSteamID(lobby);
            SteamMatchmaking.SetLobbyData(id, Key, "1");
            SteamMatchmaking.SetLobbyData(id, "v", NetLink.Version.ToString());
            SteamMatchmaking.SetLobbyData(id, "host", NetSession.Link.MyName ?? "");
            SteamMatchmaking.SetLobbyData(id, "code", Code);
            SteamMatchmaking.SetLobbyData(id, "started", "0");
            Debug.Log("[FIEF] Partie Steam creee, code " + Code);
        }

        /// <summary>REJOINDRE un salon Steam (la liste, une invitation, un code).</summary>
        public static void Join(ulong id)
        {
            if (!Ready) { NetSession.Report(Why); return; }
            hosting = false;
            Busy = true;
            NetSession.Report("Connexion…");
            entered.Set(SteamMatchmaking.JoinLobby(new CSteamID(id)));
        }

        static void OnEntered(LobbyEnter_t r, bool failure)
        {
            Busy = false;
            // 1 = k_EChatRoomEnterResponseSuccess
            if (failure || r.m_EChatRoomEnterResponse != 1)
            {
                NetSession.Report("Impossible d'entrer : la partie est pleine ou fermée.");
                return;
            }
            lobby = r.m_ulSteamIDLobby;
            CSteamID owner = SteamMatchmaking.GetLobbyOwner(new CSteamID(lobby));
            if (owner.m_SteamID == SteamUser.GetSteamID().m_SteamID) { LeaveLobby(); NetSession.Report("C'est ta propre partie."); return; }
            Settings.Load();
            NetSession.Adopt(NetLink.JoinOver(new SteamWire(), new PeerId(owner.m_SteamID), Settings.Shown, Time.unscaledTime), true);
        }

        /// <summary>REJOINDRE PAR LE CODE : on demande a Steam le salon qui porte ce code.</summary>
        public static void JoinCode(string typed)
        {
            if (!Ready) { NetSession.Report(Why); return; }
            string code = CleanCode(typed);
            Busy = true;
            NetSession.Report("Recherche du code " + code + "…");
            Filter();
            SteamMatchmaking.AddRequestLobbyListStringFilter("code", code, ELobbyComparison.k_ELobbyComparisonEqual);
            coded.Set(SteamMatchmaking.RequestLobbyList());
        }

        static void OnCoded(LobbyMatchList_t r, bool failure)
        {
            Busy = false;
            if (failure || r.m_nLobbiesMatching == 0) { NetSession.Report("Aucune partie avec ce code (elle a peut-être commencé)."); return; }
            Join(SteamMatchmaking.GetLobbyByIndex(0).m_SteamID);
        }

        static void OnListed(LobbyMatchList_t r, bool failure)
        {
            Lobbies.Clear();
            if (failure) return;
            for (int i = 0; i < r.m_nLobbiesMatching; i++)
            {
                CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
                if (id.m_SteamID == lobby) continue;
                Lobby l = new Lobby();
                l.Id = id.m_SteamID;
                l.Host = SteamMatchmaking.GetLobbyData(id, "host");
                l.Code = SteamMatchmaking.GetLobbyData(id, "code");
                l.Players = SteamMatchmaking.GetNumLobbyMembers(id);
                l.Max = Mathf.Max(l.Players, SteamMatchmaking.GetLobbyMemberLimit(id));
                l.Started = SteamMatchmaking.GetLobbyData(id, "started") == "1";
                l.SameVersion = SteamMatchmaking.GetLobbyData(id, "v") == NetLink.Version.ToString();
                if (string.IsNullOrEmpty(l.Host)) l.Host = "?";
                Lobbies.Add(l);
            }
            // Les parties ouvertes d'abord.
            Lobbies.Sort((a, b) => (a.Started || !a.SameVersion ? 1 : 0).CompareTo(b.Started || !b.SameVersion ? 1 : 0));
        }

        /// <summary>INVITER UN AMI : l'overlay de Steam s'ouvre sur ta liste d'amis.</summary>
        public static void Invite()
        {
            if (!Ready || lobby == 0) { Sfx.Deny(); return; }
            SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(lobby));
        }

        /// <summary>Quitter le salon Steam (avec le lien : NetSession.Leave).</summary>
        public static void LeaveLobby()
        {
            if (Ready && lobby != 0) SteamMatchmaking.LeaveLobby(new CSteamID(lobby));
            lobby = 0;
            hosting = false;
            Code = null;
            Busy = false;
        }

        /// <summary>A la fermeture du jeu.</summary>
        public static void Shutdown()
        {
            if (!Ready) return;
            LeaveLobby();
            SteamAPI.Shutdown();
            Ready = false;
            booted = false;
        }

        /// <summary>
        /// LE TUYAU STEAM : NetLink lui donne des paquets a envoyer a un numero Steam, il lui rend
        /// ceux qui arrivent. Les paquets partent "sans garantie" (comme en UDP) : NetLink sait
        /// deja repeter ce qui doit arriver a coup sur.
        /// </summary>
        sealed class SteamWire : IWire
        {
            public static SteamWire Current;
            readonly IntPtr[] incoming = new IntPtr[64];
            readonly Queue<KeyValuePair<byte[], ulong>> queue = new Queue<KeyValuePair<byte[], ulong>>();

            public SteamWire() { Current = this; }

            public bool Receive(byte[] buffer, out int length, out EndPoint from)
            {
                length = 0;
                from = null;
                if (queue.Count == 0) Fill();
                if (queue.Count == 0) return false;
                KeyValuePair<byte[], ulong> m = queue.Dequeue();
                length = Math.Min(m.Key.Length, buffer.Length);
                Array.Copy(m.Key, buffer, length);
                from = new PeerId(m.Value);
                return true;
            }

            void Fill()
            {
                if (!Ready) return;
                int n = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, incoming, incoming.Length);
                for (int i = 0; i < n; i++)
                {
                    SteamNetworkingMessage_t msg = SteamNetworkingMessage_t.FromIntPtr(incoming[i]);
                    if (msg.m_cbSize > 0 && msg.m_cbSize <= 4096)
                    {
                        byte[] data = new byte[msg.m_cbSize];
                        Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);
                        queue.Enqueue(new KeyValuePair<byte[], ulong>(data, msg.m_identityPeer.GetSteamID().m_SteamID));
                    }
                    SteamNetworkingMessage_t.Release(incoming[i]);
                }
            }

            public void Send(byte[] data, int length, EndPoint to)
            {
                PeerId peer = to as PeerId;
                if (peer == null || !Ready) return;
                SteamNetworkingIdentity who = new SteamNetworkingIdentity();
                who.SetSteamID(new CSteamID(peer.Id));
                GCHandle pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    SteamNetworkingMessages.SendMessageToUser(ref who, pin.AddrOfPinnedObject(), (uint)length,
                        Constants.k_nSteamNetworkingSend_Unreliable | Constants.k_nSteamNetworkingSend_AutoRestartBrokenSession, 0);
                }
                finally { pin.Free(); }
            }

            public void Close()
            {
                // On ne coupe pas les sessions tout de suite : le "au revoir" de NetLink doit
                // encore partir. Steam les ferme tout seul quand plus rien ne passe.
                if (Current == this) Current = null;
            }
        }
#else
        // Pas de Steam sur cette plate-forme : tout repond "non", le reseau local marche.
        public static void Boot() { booted = true; Why = "Steam indisponible ici"; }
        public static void Pump(float now) { }
        public static NetLink Host(string name, int maxPlayers) { return null; }
        public static void Join(ulong id) { NetSession.Report(Why); }
        public static void JoinCode(string typed) { NetSession.Report(Why); }
        public static void Invite() { }
        public static void LeaveLobby() { Code = null; }
        public static void Shutdown() { }
#endif
    }
}
