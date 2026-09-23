using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA SYLVE. Elle plante la foret et tout ce qui pousse dessous.
    ///
    /// L'ARCHITECTURE, ET POURQUOI ELLE EST DIFFERENTE DU RESTE DU DECOR.
    ///
    /// Ailleurs on FUSIONNE les formes en gros maillages (voir Batcher) pour eviter
    /// des milliers d'objets. Ici on fait l'inverse : on fabrique une petite
    /// bibliotheque de maillages -- seize arbres, six buissons, quatre rochers --
    /// et on cree des milliers d'objets qui la PARTAGENT.
    ///
    /// C'est le bon choix ici pour deux raisons, et elles tiennent toutes les deux
    /// au fait qu'on ne voit pas loin :
    ///
    ///   1. Un objet fusionne ne peut pas sortir du champ tout seul : il est dessine
    ///      en entier des qu'un bout est visible. Des objets separes sont elimines un
    ///      par un. Avec une brume a 40 m et un plan lointain a 95 m, l'immense
    ///      majorite de la foret n'est jamais dessinee.
    ///   2. La memoire ne depend plus du nombre d'arbres mais du nombre de MODELES.
    ///      Neuf mille arbres coutent autant que seize.
    ///
    /// Et comme les materiaux ont l'instanciation GPU activee (MaterialFactory), les
    /// arbres visibles partent en quelques appels de dessin au lieu d'un par arbre.
    ///
    /// LA DENSITE N'EST PAS UNIFORME. Un bruit doux ouvre des clairieres et epaissit
    /// des fourres. Une foret d'epaisseur constante n'a pas de lieux : on n'y
    /// reconnait rien, donc on ne s'y perd meme pas -- on y erre.
    /// </summary>
    public static class Forest
    {
        class Model
        {
            public Mesh mesh;
            public Material[] materials;
            public float height;
        }

        static readonly List<Model> Trees = new List<Model>();
        static readonly List<Model> Undergrowth = new List<Model>();
        static readonly List<Model> Stones = new List<Model>();

        public static int TreeCount;
        public static int PlantCount;

        public static void Reset()
        {
            Trees.Clear();
            Undergrowth.Clear();
            Stones.Clear();
            TreeCount = 0;
            PlantCount = 0;
        }

        // ------------------------------------------------------------------ modeles

        static void BuildLibrary(System.Random rng)
        {
            // Quatre essences, quatre exemplaires chacune. Seize maillages pour toute
            // la foret : au-dela, on ne distingue plus, et chaque modele coute de la
            // memoire pour rien.
            for (int i = 0; i < 4; i++)
            {
                Trees.Add(MakeTree(TreeKind.Fir, 100 + i, Palette.Pick(Palette.DarkBarks, rng),
                                   Palette.Pick(Palette.DarkNeedles, rng)));
                Trees.Add(MakeTree(TreeKind.Beech, 200 + i, Palette.Pick(Palette.PaleBarks, rng),
                                   Palette.Pick(Palette.DarkLeaves, rng)));
                Trees.Add(MakeTree(TreeKind.Birch, 300 + i, Palette.Pick(Palette.PaleBarks, rng),
                                   Palette.Pick(Palette.DarkLeaves, rng)));
                Trees.Add(MakeTree(TreeKind.Dead, 400 + i, Palette.Pick(Palette.DarkBarks, rng),
                                   Palette.Pick(Palette.DarkBarks, rng)));
            }

            for (int i = 0; i < 6; i++) Undergrowth.Add(MakeBush(500 + i, rng));
            for (int i = 0; i < 4; i++) Stones.Add(MakeStone(600 + i, rng));
        }

        static Model MakeTree(TreeKind kind, int seed, Color bark, Color foliage)
        {
            Model m = new Model();
            m.mesh = TreeMesh.Build(kind, seed, out m.height);
            m.materials = new Material[] { MaterialFactory.Get(bark), MaterialFactory.Get(foliage) };
            return m;
        }

        /// <summary>
        /// Une touffe : quelques frondes plates qui partent d'un meme point. C'est le
        /// detail qui compte le plus, parce que c'est le seul qu'on voie DE PRES.
        /// </summary>
        static Model MakeBush(int seed, System.Random pick)
        {
            System.Random rng = new System.Random(seed);
            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();

            int fronds = 5 + rng.Next(4);
            for (int i = 0; i < fronds; i++)
            {
                float a = (i / (float)fronds) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.6f;
                float len = 0.55f + (float)rng.NextDouble() * 0.75f;
                float rise = 0.35f + (float)rng.NextDouble() * 0.7f;
                float wide = 0.11f + (float)rng.NextDouble() * 0.09f;

                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0f, dir.x) * wide;
                Vector3 foot = dir * 0.06f;
                Vector3 bend = dir * len + new Vector3(0f, rise, 0f);
                Vector3 tip = dir * len * 1.5f + new Vector3(0f, rise * 0.72f, 0f);

                // Deux quads par fronde, dans les deux sens : une feuille se voit des
                // deux cotes, et a plat un seul sens la rendrait invisible de dessus.
                Face(v, t, foot - side, foot + side, bend + side, bend - side);
                Point(v, t, bend - side * 0.6f, bend + side * 0.6f, tip);
                Face(v, t, foot + side, foot - side, bend - side, bend + side);
                Point(v, t, bend + side * 0.6f, bend - side * 0.6f, tip);
            }

            Mesh mesh = new Mesh();
            mesh.name = "Touffe_" + seed;
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Model m = new Model();
            m.mesh = mesh;
            m.height = 1f;
            m.materials = new Material[] { MaterialFactory.Get(Palette.Pick(Palette.Moss, pick)) };
            return m;
        }

        /// <summary>
        /// La pointe d'une fronde. Un vrai triangle, pas un quad dont deux sommets
        /// seraient confondus : une face d'aire nulle donne une normale nulle, et la
        /// pointe ressortait noire.
        /// </summary>
        static void Point(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        static void Face(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>Un bloc erratique : une boite cabossee, a moitie enfoncee.</summary>
        static Model MakeStone(int seed, System.Random pick)
        {
            System.Random rng = new System.Random(seed);
            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();

            Vector3[] c = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                c[i] = new Vector3(
                    ((i & 1) == 0 ? -1f : 1f) * (0.6f + (float)rng.NextDouble() * 0.5f),
                    ((i & 2) == 0 ? -1f : 1f) * (0.4f + (float)rng.NextDouble() * 0.35f),
                    ((i & 4) == 0 ? -1f : 1f) * (0.6f + (float)rng.NextDouble() * 0.5f));
            }
            Face(v, t, c[0], c[4], c[6], c[2]);   // bas
            Face(v, t, c[1], c[3], c[7], c[5]);   // haut
            Face(v, t, c[0], c[1], c[5], c[4]);
            Face(v, t, c[2], c[6], c[7], c[3]);
            Face(v, t, c[0], c[2], c[3], c[1]);
            Face(v, t, c[4], c[5], c[7], c[6]);

            Mesh mesh = new Mesh();
            mesh.name = "Bloc_" + seed;
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Model m = new Model();
            m.mesh = mesh;
            m.height = 1f;
            m.materials = new Material[] { MaterialFactory.Get(Palette.Pick(Palette.WetRocks, pick)) };
            return m;
        }

        // ------------------------------------------------------------------ semis

        /// <summary>
        /// Densite du couvert en un point : 0 = clairiere, 1 = fourre impenetrable.
        /// Deux ondes croisees de periodes differentes -- assez simple pour etre lu
        /// d'un coup d'oeil, assez irregulier pour qu'on n'y voie pas de motif.
        /// </summary>
        public static float Canopy(float x, float z)
        {
            float a = Mathf.Sin(x * 0.0121f) * Mathf.Cos(z * 0.0095f);
            float b = Mathf.Sin((x + z) * 0.0052f + 1.7f);
            float c = Mathf.Cos(x * 0.0271f - z * 0.0233f) * 0.45f;
            return Mathf.Clamp01(0.52f + (a * 0.42f + b * 0.34f + c) * 0.55f);
        }

        public static void Plant(Transform parent, GameConfig cfg, System.Random rng)
        {
            Reset();
            BuildLibrary(rng);

            GameObject root = new GameObject("SYLVE");
            root.transform.SetParent(parent, false);

            float half = cfg.mapSize * 0.5f - 12f;

            // Ce reglage est expose dans l'Inspector et modifiable EN COURS DE JEU.
            // A zero, les deux boucles ci-dessous ne finiraient jamais et Unity se
            // figerait sans message. Un plancher vaut mieux qu'un plantage.
            float step = Mathf.Max(1.2f, cfg.treeSpacing);
            float density = Mathf.Clamp01(cfg.treeDensity);
            float floorDensity = Mathf.Clamp01(cfg.undergrowthDensity);

            for (float x = -half; x <= half; x += step)
            {
                for (float z = -half; z <= half; z += step)
                {
                    float cover = Canopy(x, z);

                    // Position brouillee dans la maille : sans ca on lit la grille,
                    // et une foret en quadrillage n'est plus une foret.
                    float px = x + ((float)rng.NextDouble() - 0.5f) * step * 1.6f;
                    float pz = z + ((float)rng.NextDouble() - 0.5f) * step * 1.6f;
                    if (Mathf.Abs(px) > half || Mathf.Abs(pz) > half) continue;

                    if (rng.NextDouble() < cover * density)
                        PlaceTree(root.transform, px, pz, cover, rng);

                    // Le sous-bois prospere la ou le couvert s'ouvre : c'est l'inverse
                    // des arbres, et ca remplit les clairieres au lieu de les vider.
                    float floorChance = (1.05f - cover) * floorDensity;
                    if (rng.NextDouble() < floorChance)
                        PlaceGround(root.transform, px + step * 0.4f, pz - step * 0.4f, rng);
                }
            }
        }

        static void PlaceTree(Transform parent, float x, float z, float density, System.Random rng)
        {
            // Les arbres morts se tiennent la ou la foret s'eclaircit : un squelette
            // isole dans une trouee se voit, noye dans un fourre il ne sert a rien.
            Model model;
            double roll = rng.NextDouble();
            if (density < 0.34f && roll < 0.22f) model = Trees[3 + rng.Next(4) * 4];
            else model = Trees[rng.Next(Trees.Count)];

            float scale = 0.78f + (float)rng.NextDouble() * 0.62f;
            Spawn(parent, model, new Vector3(x, Ground.Sample(x, z) - 0.25f, z),
                  Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 5f,
                                   (float)rng.NextDouble() * 360f,
                                   ((float)rng.NextDouble() - 0.5f) * 5f),
                  scale, "Arbre");
            TreeCount++;
        }

        static void PlaceGround(Transform parent, float x, float z, System.Random rng)
        {
            bool stone = rng.NextDouble() < 0.18;
            Model model = stone
                ? Stones[rng.Next(Stones.Count)]
                : Undergrowth[rng.Next(Undergrowth.Count)];

            float scale = stone
                ? 0.5f + (float)rng.NextDouble() * 1.7f
                : 0.7f + (float)rng.NextDouble() * 1.0f;

            float sink = stone ? -scale * 0.35f : -0.08f;
            Spawn(parent, model, new Vector3(x, Ground.Sample(x, z) + sink, z),
                  Quaternion.Euler(stone ? ((float)rng.NextDouble() - 0.5f) * 40f : 0f,
                                   (float)rng.NextDouble() * 360f,
                                   stone ? ((float)rng.NextDouble() - 0.5f) * 40f : 0f),
                  scale, stone ? "Bloc" : "Touffe");
            PlantCount++;
        }

        static void Spawn(Transform parent, Model model, Vector3 at, Quaternion turn,
                          float scale, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = turn;
            go.transform.localScale = new Vector3(scale, scale, scale);

            go.AddComponent<MeshFilter>().sharedMesh = model.mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = model.materials;

            // Les arbres ne recoivent pas d'ombre portee : dans une foret aussi dense
            // tout serait noir, et on a deja la penombre de la brume. Ils en PROJETTENT
            // en revanche, c'est ce qui donne les rais de lumiere entre les troncs.
            r.receiveShadows = false;
        }
    }
}
