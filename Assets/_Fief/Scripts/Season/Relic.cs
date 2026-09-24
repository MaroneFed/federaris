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
