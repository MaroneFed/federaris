using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES PHRASES DU FIL (27/09) : ce qui vient d'arriver, en francais correct. Le
    /// joueur de cette machine est "tu" (« Tu as pris la Couronne »), les autres par
    /// leur nom (« Mahaut a pris la Couronne »). Affichees par Toasts, a gauche.
    ///
    /// Ici, rien ne se decide : on raconte. Les gestes eux-memes sont ailleurs
    /// (Crown, Combat, Shrine...), qui appellent ces fonctions apres coup.
    /// </summary>
    public static class Feed
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        static readonly Color Loss = new Color(1f, 0.55f, 0.45f);
        static readonly Color Calm = new Color(0.86f, 0.82f, 0.74f);

        /// <summary>"Tu as" / "Mahaut a" : le sujet et son verbe avoir.</summary>
        static string Has(Seeker s) { return s.IsPlayer ? "Tu as" : s.Name + " a"; }
        /// <summary>"Tu es" / "Mahaut est".</summary>
        static string Is(Seeker s) { return s.IsPlayer ? "Tu es" : s.Name + " est"; }
        static Color Of(Seeker s) { return s.IsPlayer ? Gold : s.Colour; }

        static bool Live { get { return Game.Season != null && Game.Season.Running; } }

        public static void CrownTaken(Seeker s, bool fromPedestal)
        {
            if (s == null || !Live) return;
            Toasts.Show(Has(s) + (fromPedestal ? " pris la Couronne au sommet !" : " ramassé la Couronne !"), Of(s));
        }

        /// <summary>"by" a fait lacher la Couronne a "victim" (by : null pour un Oeil, un pendule, une mine...).</summary>
        public static void CrownKnocked(Seeker victim, Seeker by)
        {
            if (victim == null || !Live) return;
            string line;
            if (by == null) line = victim.IsPlayer ? "Tu as lâché la Couronne !" : victim.Name + " a lâché la Couronne !";
            else if (victim.IsPlayer) line = by.Name + " t'a fait lâcher la Couronne !";
            else if (by.IsPlayer) line = "Tu as fait lâcher la Couronne à " + victim.Name + " !";
            else line = by.Name + " a fait lâcher la Couronne à " + victim.Name + " !";
            Toasts.Show(line, victim.IsPlayer ? Loss : by != null ? Of(by) : Calm);
        }

        public static void CrownSlipped(Seeker s)
        {
            if (s == null || !Live) return;
            Toasts.Show(s.IsPlayer ? "Tu as sauté : la Couronne est restée en haut." : "La Couronne a glissé des mains de " + s.Name + ".", s.IsPlayer ? Loss : Calm);
        }

        public static void CrownHome()
        {
            if (!Live) return;
            Toasts.Show("La Couronne est revenue au sommet de la tour.", Calm);
        }

        public static void GiftTaken(Seeker s, Ability a)
        {
            if (s == null || !Live || s.IsPlayer) return;     // le sien, on le voit en grand
            Toasts.Show(s.Name + " a pris un don : " + AbilityInfo.Name(a), s.Colour);
        }

        public static void FellFromTower(Seeker s)
        {
            if (s == null || !Live) return;
            Toasts.Show(Is(s) + " tombé de la tour.", s.IsPlayer ? Loss : Calm);
        }

        public static void Said(string line, Color c)
        {
            if (!Live) return;
            Toasts.Show(line, c);
        }
    }
}
