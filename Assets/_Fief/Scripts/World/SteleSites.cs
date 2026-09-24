using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// OU SONT LES STELES. Decision de Martin (25/09/2026) : la stele n'est plus
    /// plantee par le joueur. Elle est FIXE, et tiree AU HASARD a chaque partie --
    /// une par chercheur (toi, chaque rival). On nait a cote d'elle.
    ///
    /// Et rien ne l'indique sur la boussole : il faut RETENIR ou elle est. Le chemin
    /// du retour, c'est a toi de le garder en tete (un rocher, un arbre mort, le
    /// chant de la pierre qu'on entend a quinze metres).
    ///
    /// Les regles de placement :
    ///   - entre 110 et 290 m du centre : ni au pied du chateau, ni au bord du monde ;
    ///   - au moins 120 m entre deux steles : chacun son coin de foret ;
    ///   - pas sur un lieu-dit, pas dans un creux, pas en pente ;
    ///   - de la place autour (on y nait : pas un tronc dans le dos).
    /// </summary>
    public static class SteleSites
    {
        const float MinRadius = 110f;
        const float MaxRadius = 290f;
        const float Apart = 120f;

        /// <summary>Plante une stele pour chaque chercheur, et amene chaque rival a la sienne.</summary>
        public static void PlaceAll(Transform parent, List<Seeker> seekers)
        {
            // Une graine differente a chaque lancement : la stele change de place a
            // chaque partie, meme si la foret, elle, reste la meme.
            System.Random rng = new System.Random(System.Environment.TickCount);
            // Les troncs viennent d'etre poses dans cette meme image : on force la
            // physique a connaitre leur place avant de chercher un coin libre.
            Physics.SyncTransforms();
            List<Vector3> taken = new List<Vector3>();

            for (int i = 0; i < seekers.Count; i++)
            {
                Seeker s = seekers[i];
                if (s == null || s.Hoard.StelePlanted) continue;
                Vector3 at = Find(rng, taken);
                taken.Add(at);
                s.Hoard.TryPlantStele(at);
                Stele.Build(parent, at, (float)rng.NextDouble() * 360f, s);

                // Chaque rival nait a cote de sa stele, comme toi.
                Rival r = Rival.Of(s);
                if (r != null) r.Teleport(SpawnBeside(at, (float)rng.NextDouble() * Mathf.PI * 2f));
            }
        }

        static Vector3 Find(System.Random rng, List<Vector3> taken)
        {
            float half = (Game.Config != null ? Game.Config.mapSize : 700f) * 0.5f - 60f;
            float apart = Apart;
            // On relache les exigences si la foret est trop pleine : mieux vaut une
            // stele un peu trop pres qu'une stele introuvable.
            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < 400; i++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float r = Mathf.Lerp(MinRadius, MaxRadius, Mathf.Sqrt((float)rng.NextDouble()));
                    float x = Mathf.Clamp(Mathf.Cos(a) * r, -half, half);
                    float z = Mathf.Clamp(Mathf.Sin(a) * r, -half, half);
                    if (Castle.Covers(x, z, 25f) || Landmarks.Near(x, z, 18f) || Gathering.NearHollow(x, z, 12f) || Monument.Near(x, z, 40f)) continue;
                    if (Ground.Slope(x, z) > 0.25f) continue;

                    bool far = true;
                    for (int k = 0; k < taken.Count; k++)
                    {
                        Vector3 d = taken[k] - new Vector3(x, 0f, z);
                        d.y = 0f;
                        if (d.magnitude < apart) { far = false; break; }
                    }
                    if (!far) continue;

                    Vector3 at = Ground.Place(x, z, 0f);
                    if (Clear(at)) return at;
                }
                apart *= 0.7f;
            }
            return Ground.Place(0f, -MinRadius - 20f, 0f);
        }

        /// <summary>Quatre metres libres autour : ni tronc, ni rocher (le sol ne compte pas).</summary>
        static bool Clear(Vector3 at)
        {
            Collider[] hits = Physics.OverlapBox(at + Vector3.up * 1.2f, new Vector3(3.5f, 1f, 3.5f), Quaternion.identity,
                                                 ~0, QueryTriggerInteraction.Ignore);
            for (int h = 0; h < hits.Length; h++) if (!(hits[h] is MeshCollider)) return false;
            return true;
        }

        /// <summary>Ou naitre a cote de sa stele : deux pas devant elle, dos a elle.</summary>
        public static Vector3 SpawnBeside(Vector3 stele, float angle)
        {
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.4f;
            return Ground.Place(stele.x + offset.x, stele.z + offset.z, 1.0f);
        }
    }
}
