using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QUI VIENT D'ARRIVER, EN ICONES (30/09 -- "je deteste le texte") : qui, quoi, a
    /// qui. Qui : son pseudo dans une pastille a sa couleur (toi : en or). Affiche par Toasts.
    ///
    /// Ici, rien ne se decide : on raconte. Les gestes eux-memes sont ailleurs
    /// (Crown, Combat, Shrine...), qui appellent ces fonctions apres coup.
    /// </summary>
    public static class Feed
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        static readonly Color Crown = new Color(1f, 0.86f, 0.35f);
        static readonly Color White = Color.white;
        static readonly Color Loss = new Color(1f, 0.45f, 0.4f);

        static Color Of(Seeker s) { return s.IsPlayer ? Gold : s.Colour; }
        static string N(Seeker s) { return s != null ? s.Name : null; }

        static bool Live { get { return Game.Season != null && Game.Season.Running; } }

        public static void CrownTaken(Seeker s, bool fromPedestal)
        {
            if (s == null || !Live) return;
            Toasts.Show(true, Of(s), N(s), fromPedestal ? new[] { "couronne", "tour" } : new[] { "couronne" }, new[] { Crown, White }, false, White, null);
        }

        /// <summary>"by" a fait lacher la Couronne a "victim" (by : null pour une gargouille, un pendule, une mine...).</summary>
        public static void CrownKnocked(Seeker victim, Seeker by)
        {
            if (victim == null || !Live) return;
            if (by == null) Toasts.Show(true, Of(victim), N(victim), new[] { "chute", "couronne" }, new[] { Loss, Crown }, false, White, null);
            else Toasts.Show(true, Of(by), N(by), new[] { "pousser", "couronne" }, new[] { White, Crown }, true, Of(victim), N(victim));
        }

        public static void CrownStolen(Seeker thief, Seeker victim)
        {
            if (thief == null || victim == null || !Live) return;
            Toasts.Show(true, Of(thief), N(thief), new[] { "pousser", "couronne" }, new[] { White, Crown }, true, Of(victim), N(victim));
        }

        public static void CrownSlipped(Seeker s)
        {
            if (s == null || !Live) return;
            Toasts.Show(true, Of(s), N(s), new[] { "couronne", "haut" }, new[] { Crown, Loss }, false, White, null);
        }

        public static void CrownHome()
        {
            if (!Live) return;
            Toasts.Show(false, White, new[] { "couronne", "tour" }, new[] { Crown, White }, false, White);
        }

        // (06/10 -- Martin : "il y a trop d'infos, juste les trucs simples") : le fil ne raconte
        // plus que la COURONNE (qui la prend, qui la vole, qui la fait tomber). Les dons des
        // autres, chaque chute de la tour, chaque plongeon dans les nuages : c'etait un defile
        // permanent avec sept bots. Les fonctions restent (on les appelle encore), muettes.
        public static void GiftTaken(Seeker s, Ability a) { }

        public static void FellFromTower(Seeker s) { }

        /// <summary>Seulement quand c'est TOI qui tombes (sinon, rien).</summary>
        public static void FellIntoClouds(Seeker s) { }
    }
}
