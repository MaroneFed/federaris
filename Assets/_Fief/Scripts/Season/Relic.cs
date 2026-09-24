using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA RELIQUE : ce que le mage forge avec ce qu'on lui apporte.
    ///
    /// Ce n'est PAS un MonoBehaviour : c'est une classe C# pure, qui ne sait rien
    /// d'Unity a part Mathf. Toute la regle du jeu tient ici, testable sans lancer
    /// la moindre scene. Le monde (le mage, la stele) ne fait que l'appeler.
    ///
    /// SA PUISSANCE est la somme des valeurs de ce qu'on y a fondu, multipliee par
    /// un bonus de VARIETE :
    ///
    ///     une seule ressource    x 1
    ///     deux ressources        x 1,25
    ///     les trois              x 1,6
    ///
    /// Sans ce bonus, tout le monde ne ferait que du fer, la ressource la plus
    /// chere, et la foret ne servirait a rien. Avec lui, il faut les trois lieux :
    /// la foret, les creux, le chateau.
    ///
    /// Une relique se RENFORCE : a chaque apparition du mage on peut y fondre ce
    /// qu'on porte. Sa composition s'accumule, et le bonus se calcule sur le total.
    /// </summary>
    public class Relic
    {
        /// <summary>Ce qu'elle pese dans le sac. Porter sa relique ralentit : elle se merite.</summary>
        public const float Weight = 5f;

        readonly int[] parts = new int[ResourceInfo.Count];

        public int Get(ResourceType type)
        {
            return parts[(int)type];
        }

        public int Power
        {
            get { return PowerOf(parts); }
        }

        /// <summary>
        /// Le PALIER d'une puissance : 0 rien, 1 babiole, 2 fetiche, 3 relique,
        /// 4 tresor de mage, 5 legende. Regle par Tools/saison.py : seul un joueur qui
        /// se sert de ses caches atteint la legende. L'ecran de fin en tire son
        /// verdict, la stele en tire l'allure de la relique posee.
        /// </summary>
        public static int Tier(int power)
        {
            if (power <= 0) return 0;
            if (power < 120) return 1;
            if (power < 400) return 2;
            if (power < 800) return 3;
            if (power < 1450) return 4;
            return 5;
        }

        public static string TierName(int tier)
        {
            switch (tier)
            {
                case 0: return "rien";
                case 1: return "une babiole";
                case 2: return "un fétiche";
                case 3: return "une relique";
                case 4: return "un trésor de mage";
                default: return "une légende";
            }
        }

        /// <summary>Nombre de fois qu'on l'a fait passer par la forge.</summary>
        public int Forgings { get; private set; }

        public static float VarietyBonus(int kinds)
        {
            if (kinds >= 3) return 1.6f;
            if (kinds == 2) return 1.25f;
            return 1f;
        }

        public static int PowerOf(int[] composition)
        {
            int sum = 0;
            int kinds = 0;
            for (int i = 0; i < composition.Length; i++)
            {
                if (composition[i] <= 0) continue;
                sum += composition[i] * ResourceInfo.ForgeValue((ResourceType)i);
                kinds++;
            }
            return Mathf.RoundToInt(sum * VarietyBonus(kinds));
        }

        /// <summary>La puissance qu'aurait la relique si l'on y fondait ce sac maintenant.</summary>
        public int PreviewPower(Inventory bag)
        {
            int[] next = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                next[i] = parts[i] + (bag != null ? bag.Get((ResourceType)i) : 0);
            }
            return PowerOf(next);
        }

        /// <summary>
        /// Fondre une relique VOLEE dans la sienne. On n'en garde qu'une part (60 %) :
        /// voler rapporte, mais jamais autant que ce que l'autre a perdu. Sans cette
        /// perte, le vol serait toujours le meilleur plan.
        /// </summary>
        public int Absorb(Relic stolen, float share)
        {
            if (stolen == null || stolen == this) return 0;
            int before = Power;
            for (int i = 0; i < parts.Length; i++)
                parts[i] += Mathf.FloorToInt(stolen.parts[i] * share);
            Forgings++;
            return Power - before;
        }

        /// <summary>
        /// Fond tout le contenu du sac dans la relique. Passe par Inventory.TryRemove :
        /// la regle du depot veut que rien ne modifie un inventaire autrement.
        /// Renvoie le nombre d'unites fondues.
        /// </summary>
        public int RequestForge(Inventory bag)
        {
            if (bag == null) return 0;
            int melted = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                ResourceType type = (ResourceType)i;
                int removed = bag.TryRemove(type, bag.Get(type));
                parts[i] += removed;
                melted += removed;
            }
            if (melted > 0) Forgings++;
            return melted;
        }
    }
}
