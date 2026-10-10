using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>Un obstacle qui sait dire s'il va frapper a tel endroit dans les secondes qui viennent.</summary>
    public interface IHazard
    {
        /// <summary>Vrai si l'obstacle frappera (ou frappe deja) un joueur qui se tient en "feet" d'ici "within" secondes.</summary>
        bool Danger(Vector3 feet, float within);

        /// <summary>
        /// (v44) Vrai si l'obstacle touche un joueur qui se tient en "feet" EXACTEMENT dans "t" secondes.
        /// C'est ce qui permet aux bots de calculer leur passage : "si je pars maintenant en
        /// sprintant, ou serai-je a chaque instant, et est-ce que quelque chose y sera aussi ?"
        /// </summary>
        bool HitsAt(Vector3 feet, float t);

        /// <summary>(v44) Ou est l'obstacle (pour ne regarder que ceux d'a cote).</summary>
        Vector3 Where { get; }
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

        static readonly List<IHazard> near = new List<IHazard>();

        /// <summary>
        /// LE PLAN DE PASSAGE (v44 -- Martin : "les bots sont toujours nuls ; qu'ils sachent monter la
        /// tour parfaitement, avec la logique des obstacles : quand ils arrivent et qu'ils veulent,
        /// ils courent direct"). Avant, le bot regardait UN point devant lui et attendait que plus rien
        /// ne s'y passe pendant une seconde -- trop prudent devant un belier lent, trop presse sous un
        /// pendule rapide, et au bout de 4 s il fonçait quand meme, au mauvais moment.
        ///
        /// Maintenant il SIMULE sa course : "si je pars dans 0 s, 0,1 s, 0,2 s... en sprintant droit
        /// devant, ou serai-je a chaque vingtieme de seconde, et un obstacle y sera-t-il au meme
        /// instant ?" (chaque obstacle sait ou il sera : HitsAt). Il prend le PREMIER depart sans
        /// aucun coup -- souvent tout de suite : il court direct. Rien de sur dans les 3 s : il part
        /// quand il sera le moins touche. Renvoie l'attente conseillee (0 : vas-y).
        /// </summary>
        public static float Plan(Vector3 from, Vector3 dir, float speed, float horizon, float maxWait)
        {
            return Plan(from, dir, speed, horizon, maxWait, null);
        }

        /// <summary>
        /// Le meme plan, le long d'un CHEMIN : "offset(d)" donne le deplacement apres "d" metres (sur la
        /// rampe en spirale, une ligne droite de 8 m sortirait de la rampe et lirait les mauvais obstacles).
        /// </summary>
        public static float Plan(Vector3 from, Vector3 dir, float speed, float horizon, float maxWait, System.Func<float, Vector3> offset)
        {
            near.Clear();
            for (int i = All.Count - 1; i >= 0; i--)
            {
                IHazard h = All[i];
                if (h == null || h is Object && (Object)h == null) { All.RemoveAt(i); continue; }
                Vector3 w = h.Where;
                if (Mathf.Abs(w.y - from.y) > 8f) continue;
                w.y = from.y;
                if ((w - from).magnitude < speed * horizon + 9f) near.Add(h);
            }
            if (near.Count == 0) return 0f;
            float bestWait = 0f;
            int bestHits = int.MaxValue;
            const float step = 0.05f;
            for (float wait = 0f; wait <= maxWait + 0.001f; wait += 0.1f)
            {
                int hits = 0;
                for (float t = 0f; t <= wait + horizon; t += step)
                {
                    float d = t < wait ? 0f : speed * (t - wait);
                    Vector3 p = offset != null ? from + offset.Invoke(d) : from + dir * d;
                    for (int k = 0; k < near.Count; k++) if (near[k].HitsAt(p, t)) { hits++; break; }
                    if (hits > 0 && hits >= bestHits) break;
                }
                if (hits == 0) return wait;
                if (hits < bestHits) { bestHits = hits; bestWait = wait; }
            }
            return bestWait;
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
