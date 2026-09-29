using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>Un obstacle qui sait dire s'il va frapper a tel endroit dans les secondes qui viennent.</summary>
    public interface IHazard
    {
        /// <summary>Vrai si l'obstacle frappera (ou frappe deja) un joueur qui se tient en "feet" d'ici "within" secondes.</summary>
        bool Danger(Vector3 feet, float within);
    }

    /// <summary>
    /// LE RADAR DES OBSTACLES (02/10 -- Martin : "les bots n'arrivent pas a monter la tour,
    /// elle est trop compliquee pour eux"). Chaque pendule, belier, herse et marteau
    /// s'inscrit ici ; un bot demande "si je fais un pas la, est-ce que ca frappe ?" --
    /// et s'il le faut, il ATTEND son tour, comme un joueur qui regarde le pendule passer.
    /// Avant, les bots ne connaissaient que les balayeurs (ils sautaient) et les boulets
    /// (ils changeaient de cote) : ils fonçaient dans tout le reste et se faisaient
    /// ejecter de la tour, encore et encore.
    ///
    /// Classe C# pure : les obstacles eux-memes calculent l'avenir (leur mouvement est une
    /// fonction du temps -- un sinus, un cycle -- donc on peut le lire en avance).
    /// </summary>
    public static class Hazards
    {
        static readonly List<IHazard> All = new List<IHazard>();

        public static void Add(IHazard h) { if (h != null && !All.Contains(h)) All.Add(h); }
        public static void Remove(IHazard h) { All.Remove(h); }

        /// <summary>Vrai si un obstacle frappera "feet" d'ici "within" secondes.</summary>
        public static bool Danger(Vector3 feet, float within)
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                IHazard h = All[i];
                if (h == null || h is Object && (Object)h == null) { All.RemoveAt(i); continue; }
                if (h.Danger(feet, within)) return true;
            }
            return false;
        }

        /// <summary>Le meme test, sur quelques instants entre maintenant et "within" (pour les obstacles qui balaient).</summary>
        public static bool Sweeps(Vector3 feet, float within, System.Func<float, Vector3> headAt, float radius)
        {
            Vector3 body = feet + Vector3.up * 1f;
            for (int k = 0; k <= 4; k++)
            {
                float t = within * k / 4f;
                if ((body - headAt.Invoke(t)).magnitude < radius) return true;
            }
            return false;
        }
    }
}
