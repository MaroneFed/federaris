using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE CACHE : un trou creuse dans la foret, avec ce qu'on y a enterre.
    ///
    /// Classe C# pure. Son contenu est un Inventory, exactement comme le sac -- le
    /// meme code compte les poids, la meme regle interdit de le modifier autrement
    /// que par TryAdd / TryRemove. La cache ne fait que DEPLACER des unites d'un
    /// inventaire a l'autre, et elle le fait en une seule methode par sens, pour que
    /// le passage au reseau (Phase 3) n'ait qu'un endroit a proteger.
    /// </summary>
    public class Cache
    {
        public readonly Inventory Contents = new Inventory();
        public readonly Vector3 Position;
        public readonly int Number;

        public Cache(Vector3 position, int number, float capacity)
        {
            Position = position;
            Number = number;
            Contents.MaxWeight = capacity;
        }

        /// <summary>Du sac vers la cache. Renvoie ce qui a reellement change de main.</summary>
        public int RequestDeposit(Inventory bag, ResourceType type, int quantity)
        {
            return Move(bag, Contents, type, quantity);
        }

        /// <summary>De la cache vers le sac.</summary>
        public int RequestWithdraw(Inventory bag, ResourceType type, int quantity)
        {
            return Move(Contents, bag, type, quantity);
        }

        /// <summary>
        /// On ne retire que ce que l'autre cote peut recevoir : jamais d'unite perdue
        /// entre deux inventaires, meme si le sac se remplit en cours de route.
        /// </summary>
        static int Move(Inventory from, Inventory to, ResourceType type, int quantity)
        {
            if (from == null || to == null || quantity <= 0) return 0;
            int wanted = Mathf.Min(quantity, Mathf.Min(from.Get(type), to.SpaceFor(type)));
            if (wanted <= 0) return 0;
            int taken = from.TryRemove(type, wanted);
            int given = to.TryAdd(type, taken);
            if (given < taken) from.TryAdd(type, taken - given);
            return given;
        }
    }
}
