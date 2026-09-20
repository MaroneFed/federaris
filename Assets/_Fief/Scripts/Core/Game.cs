using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Point d'acces unique aux systemes de la partie en cours.
    ///
    /// ATTENTION / PHASE 3 : ces champs statiques marchent tant qu'il n'y a qu'un
    /// seul joueur dans le processus. En multijoueur, Inventory / Wallet / Fief
    /// deviendront des composants par joueur, et Market restera cote hote uniquement.
    /// C'est pour ca que TOUTES les mutations d'economie passent deja par
    /// des methodes "demande -> validation -> application" (voir Market.cs) :
    /// le jour ou on branche Netcode, ces methodes deviennent des ServerRpc et
    /// le reste du code ne bouge pas.
    /// </summary>
    public static class Game
    {
        public static GameConfig Config;
        public static Inventory Inventory;
        public static Wallet Wallet;
        public static Market Market;
        public static FiefState Fief;
        public static Hud Hud;
        public static PlayerController Player;
        public static CharacterRig Rig;
        public static Transform PlayerTransform;

        public static Vector3 MarketPosition;
        public static Vector3 HomeFiefPosition;

        public static bool Ready
        {
            get { return Config != null && Inventory != null && Market != null; }
        }

        public static void Reset()
        {
            Config = null;
            Inventory = null;
            Wallet = null;
            Market = null;
            Fief = null;
            Hud = null;
            Player = null;
            Rig = null;
            PlayerTransform = null;
            MarketPosition = Vector3.zero;
            HomeFiefPosition = Vector3.zero;
            FloatingTexts.Clear();
        }
    }
}
