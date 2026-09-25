using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QUE LE MAGE MURMURE. A chaque forge, il te dit un secret -- a toi seul :
    ///
    ///   1re forge   ou dort un talisman qui te manque ;
    ///   2e forge    ou se cache la stele d'un rival (tu la connais desormais) ;
    ///   3e forge    ou est enterré un tresor d'or (60 pieces) ;
    ///   puis        on recommence la ronde.
    ///
    /// Il le dit EN MOTS, une fois (direction et distance, depuis là où tu te
    /// tiens) : rien n'apparait sur la boussole (Martin, 25/09). A toi de retenir.
    /// </summary>
    public static class Secrets
    {
        public class Secret
        {
            public Vector3 at;
            public string label;
            public Talisman talisman;
            public bool isTalisman;
            public Purse treasure;
            public bool isTreasure;

            public bool Resolved
            {
                get
                {
                    if (isTalisman) return Game.Hoard != null && Game.Hoard.Has(talisman);
                    if (isTreasure) return treasure == null;
                    return false;
                }
            }
        }

        public static readonly List<Secret> All = new List<Secret>();
        static int told;

        public static void Reset()
        {
            All.Clear();
            told = 0;
        }

        /// <summary>Le murmure qui suit une forge. Renvoie la phrase a afficher.</summary>
        public static string Whisper(Seeker me)
        {
            if (me == null) return "";
            int kind = told % 3;
            told++;
            for (int attempt = 0; attempt < 3; attempt++, kind = (kind + 1) % 3)
            {
                string said = kind == 0 ? TalismanSecret(me) : kind == 1 ? SteleSecret(me) : TreasureSecret(me);
                if (!string.IsNullOrEmpty(said)) return said;
            }
            return "\"Je n'ai plus rien à te dire. Pour l'instant.\"";
        }

        static string TalismanSecret(Seeker me)
        {
            TalismanPickup best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < TalismanPickup.All.Count; i++)
            {
                TalismanPickup p = TalismanPickup.All[i];
                if (p == null || me.Hoard.Has(p.talisman) || Known(p.talisman)) continue;
                float d = (p.transform.position - me.Body.position).magnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            if (best == null) return null;
            Secret s = new Secret();
            s.at = best.transform.position;
            s.label = TalismanInfo.Name(best.talisman);
            s.talisman = best.talisman;
            s.isTalisman = true;
            All.Add(s);
            return "\"" + TalismanInfo.Name(best.talisman) + " dort " + Hud.Direction(me.Body.position, s.at) + ".\"";
        }

        static bool Known(Talisman t)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].isTalisman && All[i].talisman == t) return true;
            return false;
        }

        static string SteleSecret(Seeker me)
        {
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker other = Game.Seekers[i];
                if (other == me || !other.Hoard.StelePlanted || me.Knows(other)) continue;
                me.Discover(other);
                return "\"La stèle de " + other.Name + " est " + Hud.Direction(me.Body.position, other.Hoard.StelePosition)
                       + ". Tu la connais, maintenant. Fais-en ce que tu veux.\"";
            }
            return null;
        }

        static string TreasureSecret(Seeker me)
        {
            // Un tresor enfoui, quelque part entre 60 et 160 m de toi.
            System.Random rng = new System.Random(told * 977 + 13);
            for (int i = 0; i < 60; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = 60f + (float)rng.NextDouble() * 100f;
                Vector3 p = me.Body.position + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                float half = (Game.Config != null ? Game.Config.mapSize : 420f) * 0.5f - 40f;
                if (Mathf.Abs(p.x) > half || Mathf.Abs(p.z) > half || Castle.Covers(p.x, p.z, 5f)) continue;
                Secret s = new Secret();
                s.at = Ground.Place(p.x, p.z, 0f);
                s.label = "Trésor";
                s.treasure = Purse.Build(null, s.at, 60, rng.Next(360));
                s.isTreasure = true;
                All.Add(s);
                return "\"Un trésor d'or est enterré " + Hud.Direction(me.Body.position, s.at) + ". Soixante pièces. Les gardes aiment l'or.\"";
            }
            return null;
        }
    }
}
