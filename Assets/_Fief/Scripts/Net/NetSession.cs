using UnityEngine;
using Fief.Net;

namespace Fief
{
    /// <summary>
    /// LA SESSION EN LIGNE : le lien reseau (NetLink, du C# pur) branche sur Unity. Un objet
    /// qui ne meurt jamais (DontDestroyOnLoad : il survit au rechargement des manches) et qui,
    /// a chaque image, fait lire au lien ce qui est arrive.
    ///
    /// Etape 1 (04/10) : HEBERGER, REJOINDRE, et voir le meme SALON (les pseudos de tous).
    /// Etape 2 (04/10, NetGame) : lancer le match ensemble, se voir bouger, se pousser ; l'hote
    /// decide de la Couronne et de la fin de manche. Voir docs/RESEAU.md.
    ///
    /// Concept Unity : un MonoBehaviour vit sur un GameObject de la scene ; "DontDestroyOnLoad"
    /// le sort de la scene : il reste quand on en charge une autre (comme la musique).
    /// </summary>
    public class NetSession : MonoBehaviour
    {
        static NetSession runner;

        /// <summary>Le lien en cours (null : pas en ligne).</summary>
        public static NetLink Link { get; private set; }
        /// <summary>La derniere erreur (une adresse inconnue, un port deja pris...), a montrer au salon.</summary>
        public static string Problem { get; private set; }

        const string AddressKey = "fief.net.adresse";

        /// <summary>L'adresse de l'hote qu'on a tapee la derniere fois (gardee d'une partie a l'autre).</summary>
        public static string LastAddress
        {
            get { return PlayerPrefs.GetString(AddressKey, "127.0.0.1"); }
            set { PlayerPrefs.SetString(AddressKey, value ?? ""); PlayerPrefs.Save(); }
        }

        /// <summary>(v31) Le lien passe par Steam (Internet) et non par la box (reseau local).</summary>
        public static bool OverSteam { get; private set; }

        /// <summary>
        /// (v31) Au lancement : la session reseau existe tout de suite et dit bonjour a Steam --
        /// pour qu'une invitation d'un ami arrive meme si l'on n'a pas encore ouvert En ligne.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            Ensure();
            try { SteamNet.Boot(); }
            catch (System.Exception e) { Debug.LogWarning("[FIEF] Steam : " + e.Message); }
        }

        static void Ensure()
        {
            if (runner != null) return;
            GameObject go = new GameObject("RESEAU");
            DontDestroyOnLoad(go);
            runner = go.AddComponent<NetSession>();
        }

        /// <summary>HEBERGER une partie (on attend les autres au salon).</summary>
        public static void Host()
        {
            Leave();
            Problem = null;
            try
            {
                Ensure();
                Settings.Load();
                Link = NetLink.Host(NetLink.DefaultPort, Settings.Shown, Match.MaxPlayers);
            }
            catch (System.Exception e)
            {
                Link = null;
                Problem = "Impossible d'héberger (port " + NetLink.DefaultPort + " déjà pris ?)";
                Debug.LogWarning("[FIEF] Réseau : " + e.Message);
            }
        }

        /// <summary>(v31) HEBERGER SUR INTERNET : un salon Steam, visible dans le monde entier.</summary>
        public static void HostInternet()
        {
            Leave();
            Problem = null;
            if (!SteamNet.Ready) { Problem = SteamNet.Why; return; }
            try
            {
                Ensure();
                Settings.Load();
                Link = SteamNet.Host(Settings.Shown, Match.MaxPlayers);
                OverSteam = Link != null;
            }
            catch (System.Exception e)
            {
                Link = null;
                OverSteam = false;
                Problem = "Steam n'a pas pu créer la partie.";
                Debug.LogWarning("[FIEF] Steam : " + e.Message);
            }
        }

        /// <summary>(v31) REJOINDRE un salon Steam (la liste mondiale, une invitation).</summary>
        public static void JoinInternet(ulong lobby)
        {
            Leave();
            Problem = null;
            Ensure();
            SteamNet.Join(lobby);
        }

        /// <summary>(v31) Steam a fait entrer dans le salon : on prend le lien qu'il a ouvert.</summary>
        public static void Adopt(NetLink link, bool steam)
        {
            if (Link != null) Link.Dispose();
            Link = link;
            OverSteam = steam && link != null;
            Problem = null;
        }

        /// <summary>REJOINDRE la partie de "address" : une adresse IP, un CODE de partie (06/10), ou un code Steam de 6 signes (v31).</summary>
        public static void Join(string address)
        {
            Leave();
            Problem = null;
            LastAddress = address;
            if (SteamNet.LooksLikeCode(address))
            {
                Ensure();
                SteamNet.JoinCode(address);
                return;
            }
            try
            {
                Ensure();
                Settings.Load();
                Link = NetLink.Join(NetCode.ToAddress(address), NetLink.DefaultPort, Settings.Shown, Time.unscaledTime);
            }
            catch (System.Exception e)
            {
                Link = null;
                Problem = "Adresse inconnue : " + address;
                Debug.LogWarning("[FIEF] Réseau : " + e.Message);
            }
        }

        /// <summary>Quitter le salon (on previent les autres).</summary>
        public static void Leave()
        {
            if (Link != null) Link.Dispose();
            Link = null;
            OverSteam = false;
            SteamNet.LeaveLobby();
            NetGame.LocalPhase = NetGame.Phase.Lobby;
            if (Match.Online) Match.Abandon();
        }

        // ================================================================== la liste des parties (06/10)

        /// <summary>Le chercheur de parties (seulement tant que l'ecran En ligne le demande).</summary>
        public static NetFinder Finder { get; private set; }
        static float findUntil;

        /// <summary>L'ecran En ligne l'appelle a chaque image : on cherche les parties du reseau.</summary>
        public static void KeepFinding()
        {
            findUntil = Time.unscaledTime + 0.5f;
            SteamNet.WantList(Time.unscaledTime);
            if (Finder != null) return;
            try { Ensure(); Finder = NetFinder.Start(NetLink.DefaultPort); }
            catch (System.Exception e) { Finder = null; Debug.LogWarning("[FIEF] Réseau (recherche) : " + e.Message); }
        }

        /// <summary>
        /// LE CODE DE TA PARTIE (06/10 -- "c'est chiant de mettre ton IP, trouve un code") : ton
        /// adresse sur le reseau local en 7 signes. On prefere celle de la box (192.168..., 10...),
        /// puis celle de Radmin VPN (26...).
        /// </summary>
        public static string MyCode
        {
            get
            {
                if (OverSteam) return SteamNet.Code ?? "…";
                string best = null;
                foreach (string a in NetLink.LocalAddresses())
                {
                    if (a.StartsWith("192.168.") || a.StartsWith("10.") || a.StartsWith("172.")) { best = a; break; }
                    if (best == null) best = a;
                }
                return NetCode.Encode(best);
            }
        }

        void Update()
        {
            SteamNet.Pump(Time.unscaledTime);
            if (Finder != null)
            {
                if (Time.unscaledTime > findUntil || Link != null) { Finder.Dispose(); Finder = null; }
                else Finder.Poll(Time.unscaledTime);
            }
            if (Link == null) return;
            Link.Poll(Time.unscaledTime);
            // (04/10, etape 2) Les messages du jeu : l'etat du match, les positions, les coups.
            NetGame.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>Dire au salon ce qui ne va pas (l'hote est parti...).</summary>
        public static void Report(string problem) { Problem = problem; }

        void OnApplicationQuit()
        {
            Leave();
            SteamNet.Shutdown();
        }
    }
}
