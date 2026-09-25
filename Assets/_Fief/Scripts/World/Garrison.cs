using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA GARNISON : ou se tient chaque garde de la Garde Pale (voir Guard.cs).
    ///
    ///   la cour        huit sentinelles (herse, reserves, poterne, breche, deux rondes)
    ///                  et trois molosses ;
    ///   les remparts   cinq arbaletriers (au-dessus de la porte, sur les tours du
    ///                  chatelet, au nord, a l'ouest, a l'est pres de la breche) ;
    ///   le donjon      une sentinelle par salle, un arbaletrier sur le palier du
    ///                  premier etage, deux gardes royaux sur la terrasse ;
    ///   la terrasse    LE ROI CREUX, devant son trone ;
    ///   la foret       trois rodeurs, qui tournent autour du chateau et courent
    ///                  apres le porteur de la Couronne.
    ///
    /// Vingt-sept en tout. Les positions sont ecrites a la main, a partir des
    /// dimensions de Castle et Keep : si le chateau change, c'est ici qu'on les suit.
    /// </summary>
    public static class Garrison
    {
        static readonly string[] Names = { "Bertrand", "Aubin", "Lambert", "Jehan", "Thibaut", "Enguerrand", "Gaspard", "Hugues",
                                           "Renaud", "Gautier", "Arnaud", "Mathieu", "Grégoire", "Tristan", "Baudouin", "Anseau",
                                           "Foulques", "Godefroy", "Raoul", "Amaury", "Guy", "Eudes", "Josselin", "Hervé" };
        static int named;

        static string Next() { return Names[named++ % Names.Length]; }

        static Vector3 G(float x, float z) { return Ground.Place(x, z, 0.05f); }
        static Vector3 Wall(float x, float z) { return new Vector3(x, Castle.WallHeight + 0.05f, z); }

        public static void Build(Transform parent)
        {
            named = 0;
            GameObject root = new GameObject("GARDE PÂLE");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            float h = Castle.HalfSize;

            // --- la cour : sentinelles
            Vector3[][] yard =
            {
                new[] { G(-5f, -35f), G(-5f, -27f) },                               // la herse
                new[] { G(5f, -27f), G(5f, -34f) },
                new[] { G(-26f, -20f), G(-26f, -8f) },                              // reserve ouest
                new[] { G(26f, -20f), G(26f, -8f) },                                // reserve est
                new[] { G(-31f, 26f), G(-19f, 26f) },                               // reserve nord
                new[] { G(-13f, -6f), G(-13f, 6f), G(9f, 6f), G(9f, -6f) },         // ronde du parvis
                new[] { G(-4f, 35f), G(4f, 35f) },                                  // la poterne
                new[] { G(24f, 6f), G(24f, 18f) },                                  // la breche
            };
            for (int i = 0; i < yard.Length; i++) Guard.Build(t, Next(), yard[i], Guard.Rank.Sentinelle, false);

            // --- la cour : molosses, qui font vite le tour
            Guard.Build(t, "Croc", new[] { G(-20f, -30f), G(20f, -30f), G(20f, -14f), G(-20f, -14f) }, Guard.Rank.Molosse, false);
            Guard.Build(t, "Suie", new[] { G(-30f, 0f), G(-30f, 30f), G(-12f, 30f), G(-12f, 0f) }, Guard.Rank.Molosse, false);
            Guard.Build(t, "Morne", new[] { G(14f, 26f), G(30f, 26f), G(30f, 0f), G(14f, 0f) }, Guard.Rank.Molosse, false);

            // --- les remparts : arbaletriers
            float o = Castle.GateWidth * 0.5f + 4.2f;
            Guard.Build(t, Next(), new[] { Wall(-1.5f, -h), Wall(1.5f, -h) }, Guard.Rank.Arbaletrier, false);                      // au-dessus de la porte
            Guard.Build(t, Next(), new[] { new Vector3(-o, Castle.GatehouseHeight + 0.05f, -h - 0.5f) }, Guard.Rank.Arbaletrier, false);
            Guard.Build(t, Next(), new[] { new Vector3(o, Castle.GatehouseHeight + 0.05f, -h - 0.5f) }, Guard.Rank.Arbaletrier, false);
            Guard.Build(t, Next(), new[] { Wall(-14f, h), Wall(14f, h) }, Guard.Rank.Arbaletrier, false);                           // nord
            Guard.Build(t, Next(), new[] { Wall(-h, -16f), Wall(-h, 16f) }, Guard.Rank.Arbaletrier, false);                         // ouest
            Guard.Build(t, Next(), new[] { Wall(h, 20f), Wall(h, 30f) }, Guard.Rank.Arbaletrier, false);                            // est, au-dessus de la breche

            // --- le donjon : une sentinelle par salle, deux gardes royaux sur la terrasse
            for (int i = 0; i < Keep.GuardRoutes.Count; i++)
            {
                Vector3[] route = Keep.GuardRoutes[i];
                Guard.Build(t, Next(), route, Guard.Rank.Sentinelle, false);
                if (i == Keep.GuardRoutes.Count - 1)
                {
                    // Le second garde royal fait la meme ronde, a l'oppose.
                    Vector3[] other = new Vector3[route.Length];
                    for (int k = 0; k < route.Length; k++) other[k] = route[(k + route.Length / 2) % route.Length];
                    Guard.Build(t, Next(), other, Guard.Rank.Sentinelle, false);
                }
            }
            Guard.Build(t, Next(), new[] { Keep.ArcherPost }, Guard.Rank.Arbaletrier, false);

            // --- LE ROI CREUX
            Guard.Build(t, "le Roi Creux", new[] { Keep.KingHome }, Guard.Rank.Roi, false);

            // --- les rodeurs de la foret
            for (int r = 0; r < 3; r++)
            {
                Vector3[] ring = new Vector3[6];
                for (int k = 0; k < ring.Length; k++)
                {
                    float a = (r * 120f + k * 60f + 20f) * Mathf.Deg2Rad;
                    float radius = 72f + (k % 2) * 14f;
                    ring[k] = G(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
                }
                Guard.Build(t, Next(), ring, Guard.Rank.Sentinelle, true);
            }
        }
    }
}
