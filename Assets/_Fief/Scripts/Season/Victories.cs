namespace Fief
{
    /// <summary>
    /// QUI GAGNE : celui qui a le plus de butin (★) dans sa stele quand la cloche
    /// sonne. Une seule facon de gagner (26/09 : les quatre victoires "comme dans
    /// Civilization" demandaient trop de lecture).
    ///
    /// Classe C# pure. Elle ne fait que noter le vainqueur.
    /// </summary>
    public static class Victories
    {
        public static Seeker Winner { get; private set; }
        public static bool Decided { get; private set; }

        public static void Reset()
        {
            Winner = null;
            Decided = false;
        }

        /// <summary>A la cloche : le plus gros butin. Personne si tout le monde est a zero.</summary>
        public static void Decide()
        {
            if (Decided) return;
            Decided = true;
            Seeker best = null;
            for (int i = 0; i < Game.Seekers.Count; i++)
                if (best == null || Game.Seekers[i].Score > best.Score) best = Game.Seekers[i];
            Winner = best != null && best.Score > 0 ? best : null;
        }
    }
}
