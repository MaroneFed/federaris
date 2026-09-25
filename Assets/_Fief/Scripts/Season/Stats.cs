namespace Fief
{
    /// <summary>
    /// CE QUE TU AS FAIT PENDANT LE MATCH, pour l'ecran de fin. Classe C# pure,
    /// remise a zero au debut du match (pas a chaque manche).
    /// </summary>
    public static class Stats
    {
        public static int Deaths;
        public static int PlayersDowned;
        public static int GuardsDowned;
        public static int BeastsDowned;
        public static int CrownsTaken;
        public static int Chests;
        public static int KingDowned;
        public static int Delivered;
        public static string LastDeath = "";
        // (anciens compteurs, gardes pour les pieges)
        public static int TrapKills;
        public static int RivalsDowned;

        public static void Reset()
        {
            Deaths = 0;
            PlayersDowned = 0;
            GuardsDowned = 0;
            BeastsDowned = 0;
            CrownsTaken = 0;
            Chests = 0;
            KingDowned = 0;
            Delivered = 0;
            LastDeath = "";
            TrapKills = 0;
            RivalsDowned = 0;
        }
    }
}
