using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES QUATRE FACONS DE GAGNER (demandees par Martin le 24/09/2026, "comme dans
    /// Civilization"). Chacune est un chemin different a travers la meme Saison :
    ///
    ///   LA RELIQUE   a la cloche, la plus puissante relique posee sur sa stele.
    ///                Le chemin de tout le monde, et celui des rivaux.
    ///   LA TRAHISON  acheter le SERMENT des six gardes. Le chateau t'appartient :
    ///                victoire immediate. Le chemin de l'or.
    ///   LA COURONNE  reunir les six talismans et s'asseoir sur le trone du roi
    ///                sans tete. Victoire immediate. Le chemin de l'explorateur.
    ///   L'OFFRANDE   deposer au Registre 60 bois mort, 20 pierres-lune et 10 fer
    ///                ancien. Victoire immediate. Le chemin du recolteur.
    ///
    /// Les trois victoires immediates sont des COURSES contre la cloche : si
    /// personne n'en decroche une, c'est la Relique qui tranche.
    ///
    /// Classe C# pure. Elle ne fait que noter qui a gagne, et comment.
    /// </summary>
    public enum VictoryKind { None, Relique, Trahison, Couronne, Offrande }

    public static class Victories
    {
        public static readonly int[] Offering = { 60, 20, 10 };   // bois mort, pierre-lune, fer ancien

        public static VictoryKind Kind { get; private set; }
        public static Seeker Winner { get; private set; }
        public static bool Decided { get { return Kind != VictoryKind.None; } }

        public static void Reset()
        {
            Kind = VictoryKind.None;
            Winner = null;
        }

        /// <summary>Une victoire immediate. La premiere declaree l'emporte.</summary>
        public static bool Declare(Seeker who, VictoryKind kind)
        {
            if (Decided || who == null) return false;
            Kind = kind;
            Winner = who;
            return true;
        }

        /// <summary>A la cloche, sans victoire immediate : la plus puissante relique.</summary>
        public static void DecideByRelic()
        {
            if (Decided) return;
            Seeker best = null;
            for (int i = 0; i < Game.Seekers.Count; i++)
                if (best == null || Game.Seekers[i].Score > best.Score) best = Game.Seekers[i];
            if (best != null && best.Score > 0) Declare(best, VictoryKind.Relique);
        }

        public static string Title(VictoryKind kind)
        {
            switch (kind)
            {
                case VictoryKind.Relique: return "Victoire par la Relique";
                case VictoryKind.Trahison: return "Victoire par la Trahison";
                case VictoryKind.Couronne: return "Victoire par la Couronne";
                case VictoryKind.Offrande: return "Victoire par l'Offrande";
                default: return "Personne n'a gagne";
            }
        }

        public static string How(VictoryKind kind)
        {
            switch (kind)
            {
                case VictoryKind.Relique: return "A la cloche, la plus puissante relique posee sur sa stele.";
                case VictoryKind.Trahison: return "Acheter le serment des six gardes : le chateau est a toi.";
                case VictoryKind.Couronne: return "Reunir les six talismans, puis s'asseoir sur le trone.";
                case VictoryKind.Offrande: return "Deposer au Registre 60 bois mort, 20 pierres-lune, 10 fer ancien.";
                default: return "";
            }
        }
    }
}
