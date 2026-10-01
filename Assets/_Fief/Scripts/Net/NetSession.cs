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

        /// <summary>REJOINDRE la partie de "address" (une adresse IP).</summary>
        public static void Join(string address)
        {
            Leave();
            Problem = null;
            LastAddress = address;
            try
            {
                Ensure();
                Settings.Load();
                Link = NetLink.Join(address, NetLink.DefaultPort, Settings.Shown, Time.unscaledTime);
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
            NetGame.LocalPhase = NetGame.Phase.Lobby;
            if (Match.Online) Match.Abandon();
        }

        void Update()
        {
            if (Link == null) return;
            Link.Poll(Time.unscaledTime);
            // (04/10, etape 2) Les messages du jeu : l'etat du match, les positions, les coups.
            NetGame.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>Dire au salon ce qui ne va pas (l'hote est parti...).</summary>
        public static void Report(string problem) { Problem = problem; }

        void OnApplicationQuit() { Leave(); }
    }
}
