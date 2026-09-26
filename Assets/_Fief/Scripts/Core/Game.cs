using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Point d'acces unique aux systemes de la manche en cours.
    ///
    /// ATTENTION / PHASE 3 : ces champs statiques marchent tant qu'il n'y a qu'un
    /// seul joueur local par machine -- c'est justement le cas en ligne. Ce qui
    /// traverse les manches vit dans Match ; ce qui vit une manche vit ici, et
    /// disparait au rechargement de la scene.
    /// </summary>
    public static class Game
    {
        /// <summary>
        /// LA VERSION, affichee en bas de l'ecran-titre et dans la Console. Si tu vois
        /// autre chose que ce texte, Unity ne fait pas tourner le dernier code (voir
        /// README.md, "Recuperer la derniere version").
        /// </summary>
        public const string Version = "La Couronne · v5 · l'île · 27/09";

        public static GameConfig Config;
        public static Season Season;

        /// <summary>Toi.</summary>
        public static Seeker Me;
        /// <summary>Tous les joueurs de la manche, dans l'ordre des places du match.</summary>
        public static readonly System.Collections.Generic.List<Seeker> Seekers =
            new System.Collections.Generic.List<Seeker>();
        public static Hud Hud;
        public static Menus Menus;
        public static PlayerController Player;
        public static CharacterRig Rig;
        public static Transform PlayerTransform;

        /// <summary>Le centre du chateau.</summary>
        public static Vector3 CastleCentre;

        /// <summary>Renseigne si la construction du monde a echoue : affiche en rouge a l'ecran.</summary>
        public static string BuildError;
        public static long BuildMilliseconds;

        public static bool Ready { get { return Config != null && Season != null; } }

        public static Seeker SeekerOf(int slot)
        {
            for (int i = 0; i < Seekers.Count; i++) if (Seekers[i].Index == slot) return Seekers[i];
            return null;
        }

        public static void Reset()
        {
            Config = null;
            Season = null;
            Hud = null;
            Menus = null;
            Player = null;
            Rig = null;
            PlayerTransform = null;
            Me = null;
            Seekers.Clear();
            CastleCentre = Vector3.zero;
            BuildError = null;
            FloatingTexts.Clear();
        }
    }
}
