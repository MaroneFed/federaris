namespace Fief
{
    /// <summary>
    /// CE QUE TU AS FAIT PENDANT LE MATCH, pour le podium. Classe C# pure, remise a zero
    /// au debut du match (pas a chaque manche).
    /// </summary>
    public static class Stats
    {
        public static int Delivered;        // Couronnes posees au Monument
        public static int CrownsTaken;      // Couronnes prises (sur le socle ou a terre)
        public static int CrownsStolen;     // Couronnes arrachees a un autre
        public static int Shoves;           // poussees qui ont touche
        public static int Casts;            // capacites lancees
        public static int EyeHits;          // rayons des Yeux encaisses
        public static int MineHits;         // joueurs envoles par tes mines
        public static int Shrines;          // dons pris aux sanctuaires

        public static void Reset()
        {
            Delivered = 0;
            CrownsTaken = 0;
            CrownsStolen = 0;
            Shoves = 0;
            Casts = 0;
            EyeHits = 0;
            MineHits = 0;
            Shrines = 0;
        }
    }
}
