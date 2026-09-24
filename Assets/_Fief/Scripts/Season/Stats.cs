namespace Fief
{
    /// <summary>
    /// LE JOURNAL DE TA SAISON : ce qui s'est passe, pour l'ecran de la cloche.
    /// Sans lui, la fin disait qui a gagne, jamais POURQUOI tu as perdu : combien
    /// de fois tu es tombe (et sous quoi), ce que la Malediction t'a pris, ce qu'on
    /// t'a pille, ce que tu as pille, ce que tu as abattu.
    ///
    /// Classe C# pure, remise a zero a chaque Saison. On n'y ecrit qu'en ajoutant.
    /// </summary>
    public static class Stats
    {
        public static int Deaths;
        public static string LastDeath = "";
        public static int CurseLost;
        public static int Robbed;
        public static int Looted;
        public static int RivalsDowned;
        public static int BeastsDowned;
        public static int TrapKills;

        public static void Reset()
        {
            Deaths = 0;
            LastDeath = "";
            CurseLost = 0;
            Robbed = 0;
            Looted = 0;
            RivalsDowned = 0;
            BeastsDowned = 0;
            TrapKills = 0;
        }
    }
}
