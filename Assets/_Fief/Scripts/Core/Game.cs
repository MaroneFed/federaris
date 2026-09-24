using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Point d'acces unique aux systemes de la partie en cours.
    ///
    /// ATTENTION / PHASE 3 : ces champs statiques marchent tant qu'il n'y a qu'un
    /// seul joueur dans le processus. En multijoueur, Inventory, Wallet et Hoard
    /// deviendront des composants PAR JOUEUR, et Season restera cote hote. C'est
    /// pour ca que toute la regle du jeu vit dans des classes C# pures (Season,
    /// Hoard, Relic, Cache, Inventory) et que tout changement passe par une methode
    /// Request* ou Try* : le jour du reseau, ce sont elles qu'on protege.
    /// </summary>
    public static class Game
    {
        public static GameConfig Config;
        public static Inventory Inventory;
        public static Wallet Wallet;
        public static Season Season;
        public static Hoard Hoard;
        public static Hud Hud;
        public static Menus Menus;
        public static PlayerController Player;
        public static CharacterRig Rig;
        public static Transform PlayerTransform;
        public static Mage Mage;

        /// <summary>Le centre du chateau, et donc de la stele.</summary>
        public static Vector3 CastleCentre;

        /// <summary>Renseigne si la construction du monde a echoue : affiche en rouge a l'ecran.</summary>
        public static string BuildError;
        public static long BuildMilliseconds;

        /// <summary>
        /// Vrai tant que l'infusion de l'Ermite fait effet : le poids du sac ne
        /// ralentit plus les gestes (recolter, creuser).
        /// </summary>
        public static bool Brewed
        {
            get { return Hoard != null && Season != null && Hoard.BrewActive(Season.Elapsed); }
        }

        public static bool Ready
        {
            get { return Config != null && Inventory != null && Season != null && Hoard != null; }
        }

        public static void Reset()
        {
            Config = null;
            Inventory = null;
            Wallet = null;
            Season = null;
            Hoard = null;
            Hud = null;
            Menus = null;
            Player = null;
            Rig = null;
            PlayerTransform = null;
            Mage = null;
            CastleCentre = Vector3.zero;
            BuildError = null;
            FloatingTexts.Clear();
        }
    }
}
