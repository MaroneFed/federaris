using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    public enum TreeKind
    {
        /// <summary>Sapin noir : haut, etroit, jupes etagees. L'essence dominante.</summary>
        Fir,
        /// <summary>Hetre : fut pale et nu sur 6 m, houppier tres haut. Il capte la lumiere.</summary>
        Beech,
        /// <summary>Bouleau tordu : penche, clairseme, il casse la verticalite.</summary>
        Birch,
        /// <summary>Arbre mort : troncs et branches nues. C'est lui qui fait peur.</summary>
        Dead
    }

    /// <summary>
    /// Fabrique les maillages d'arbres.
    ///
    /// POURQUOI UN MAILLAGE ET PLUS DES CUBES. Les arbres etaient empiles a partir
    /// de cubes et de cylindres : de loin ca passait, mais dans une foret sombre ou
    /// on marche a trois metres des troncs, on voit des boites. Ici chaque arbre est
    /// un vrai maillage -- fut qui s'affine et serpente, racines evasees au pied,
    /// branches qui partent en biais, houppier en volumes irreguliers.
    ///
    /// CA NE COUTE RIEN, parce qu'on n'en fabrique que SEIZE. Les milliers d'arbres
    /// plantes dans le monde partagent ces seize maillages ; seules leur position,
    /// leur rotation et leur echelle changent. Unity les dessine par paquets
    /// (instanciation GPU) et les elimine un par un quand ils sortent du champ.
    /// Un arbre peut donc se permettre 300 triangles.
    ///
    /// DEUX SOUS-MAILLAGES : le bois et le feuillage. Ils ont leur materiau propre.
    ///
    /// LE PIEGE DES FACES A L'ENVERS. Un maillage genere a la main dont les triangles
    /// tournent dans le mauvais sens est INVISIBLE de l'exterieur -- et impossible a
    /// deviner sans lancer le jeu. La regle, verifiee sur le poncho qui, lui,
    /// s'affiche correctement : le triangle (v0, v1, v2) est vu de face quand
    /// Cross(v1 - v0, v2 - v0) pointe VERS L'EXTERIEUR. Tous les assemblages
    /// ci-dessous (Tube, Cone, Cap) en decoulent et ne doivent pas etre reordonnes.
    /// </summary>
    public static class TreeMesh
    {
        // ------------------------------------------------------------------ atelier

        class Shape
        {
            public readonly List<Vector3> points = new List<Vector3>();
            public readonly List<int> wood = new List<int>();
            public readonly List<int> leaf = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, List<int> into)
            {
                int i = points.Count;
                points.Add(a); points.Add(b); points.Add(c); points.Add(d);
                into.Add(i); into.Add(i + 1); into.Add(i + 2);
                into.Add(i); into.Add(i + 2); into.Add(i + 3);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, List<int> into)
            {
                int i = points.Count;
                points.Add(a); points.Add(b); points.Add(c);
                into.Add(i); into.Add(i + 1); into.Add(i + 2);
            }

            /// <summary>Relie deux anneaux. Sens verifie : les faces regardent dehors.</summary>
            public void Tube(Vector3[] lower, Vector3[] upper, List<int> into)
            {
                int n = lower.Length;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    Quad(lower[i], lower[j], upper[j], upper[i], into);
                }
            }

            public void Cone(Vector3[] ring, Vector3 tip, List<int> into)
            {
                int n = ring.Length;
                for (int i = 0; i < n; i++) Tri(ring[i], ring[(i + 1) % n], tip, into);
            }

            /// <summary>Fond plat, tourne vers le bas.</summary>
            public void Floor(Vector3[] ring, Vector3 centre, List<int> into)
            {
                int n = ring.Length;
                for (int i = 0; i < n; i++) Tri(centre, ring[(i + 1) % n], ring[i], into);
            }
        }

        /// <summary>
        /// Repere perpendiculaire a une direction. Le sens de "right" est choisi pour
        /// que les anneaux tournent dans le bon sens -- voir la note sur les faces.
        /// </summary>
        static void Basis(Vector3 direction, out Vector3 right, out Vector3 forward)
        {
            Vector3 d = direction.normalized;
            Vector3 up = Mathf.Abs(d.y) > 0.95f ? Vector3.right : Vector3.up;
            right = Vector3.Cross(up, d).normalized;
            forward = Vector3.Cross(d, right).normalized;
        }

        static Vector3[] Ring(Vector3 centre, Vector3 direction, float radius, int sides,
                              float phase, System.Random rng, float wobble)
        {
            Vector3 right, forward;
            Basis(direction, out right, out forward);

            Vector3[] ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = phase + (i / (float)sides) * Mathf.PI * 2f;
                float r = radius * (1f + ((float)rng.NextDouble() - 0.5f) * wobble);
                ring[i] = centre + (Mathf.Cos(a) * right + Mathf.Sin(a) * forward) * r;
            }
            return ring;
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>
        /// Un fut : une suite d'anneaux qui montent en s'affinant, avec une derive
        /// laterale pour que l'arbre ne soit jamais un poteau. Renvoie le sommet.
        /// </summary>
        static Vector3 Trunk(Shape s, Vector3 foot, float height, float radius,
                             Vector3 lean, int sides, System.Random rng, float taper)
        {
            const int Steps = 6;
            Vector3[] previous = null;
            Vector3 top = foot;

            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps;

                // Les racines s'evasent : le pied est bien plus large que le fut.
                float flare = t < 0.14f ? Mathf.Lerp(1.9f, 1f, t / 0.14f) : 1f;
                float r = radius * Mathf.Lerp(1f, taper, t) * flare;

                // Derive en S : une courbe, pas une inclinaison uniforme.
                Vector3 drift = lean * (t * t) + new Vector3(
                    Mathf.Sin(t * 3.1f) * radius * 0.7f, 0f, Mathf.Cos(t * 2.3f) * radius * 0.6f);

                Vector3 centre = foot + new Vector3(0f, height * t, 0f) + drift;
                Vector3[] ring = Ring(centre, Vector3.up + lean * 0.4f, r, sides,
                                      i * 0.21f, rng, 0.16f);

                if (previous != null) s.Tube(previous, ring, s.wood);
                previous = ring;
                top = centre;
            }
            return top;
        }

        /// <summary>Une branche : un cone effile, plante en biais.</summary>
        static Vector3 Branch(Shape s, Vector3 from, Vector3 direction, float length,
                              float radius, System.Random rng)
        {
            Vector3 tip = from + direction.normalized * length;
            Vector3[] ring = Ring(from, direction, radius, 4, 0.4f, rng, 0.2f);
            Vector3[] mid = Ring(from + direction.normalized * length * 0.55f, direction,
                                 radius * 0.5f, 4, 0.9f, rng, 0.25f);
            s.Tube(ring, mid, s.wood);
            s.Cone(mid, tip, s.wood);
            return tip;
        }

        /// <summary>
        /// Un volume de feuillage : une sphere cabossee, faite d'anneaux. On empile
        /// ces volumes au bout des branches, ce qui donne un houppier qui a une forme
        /// au lieu d'un cube.
        /// </summary>
        static void Foliage(Shape s, Vector3 centre, float radius, float squash,
                            System.Random rng)
        {
            const int Sides = 6;
            const int Rows = 3;
            float[] level = { -0.62f, -0.12f, 0.42f };

            Vector3[] previous = null;
            for (int i = 0; i < Rows; i++)
            {
                float t = level[i];
                float r = radius * Mathf.Sqrt(Mathf.Max(0.08f, 1f - t * t));
                Vector3 c = centre + new Vector3(0f, t * radius * squash, 0f);
                Vector3[] ring = Ring(c, Vector3.up, r, Sides, i * 0.37f, rng, 0.34f);

                if (previous == null) s.Floor(ring, centre + new Vector3(0f, -radius * squash, 0f), s.leaf);
                else s.Tube(previous, ring, s.leaf);
                previous = ring;
            }
            s.Cone(previous, centre + new Vector3(0f, radius * squash * 1.05f, 0f), s.leaf);
        }

        /// <summary>Une jupe de sapin : un cone large, legerement de travers.</summary>
        static void Skirt(Shape s, Vector3 centre, float radius, float height,
                          System.Random rng)
        {
            Vector3 tilt = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.18f, 1f,
                                       ((float)rng.NextDouble() - 0.5f) * 0.18f);
            Vector3[] ring = Ring(centre, tilt, radius, 7,
                                  (float)rng.NextDouble() * 6.28f, rng, 0.30f);
            s.Floor(ring, centre, s.leaf);
            s.Cone(ring, centre + tilt.normalized * height, s.leaf);
        }

        // ------------------------------------------------------------------ essences

        /// <summary>Fabrique un arbre. <paramref name="height"/> ressort pour le placement.</summary>
        public static Mesh Build(TreeKind kind, int seed, out float height)
        {
            System.Random rng = new System.Random(seed);
            Shape s = new Shape();
            height = 0f;

            switch (kind)
            {
                case TreeKind.Fir: height = Fir(s, rng); break;
                case TreeKind.Beech: height = Beech(s, rng); break;
                case TreeKind.Birch: height = Birch(s, rng); break;
                default: height = Dead(s, rng); break;
            }

            Mesh mesh = new Mesh();
            mesh.name = kind + "_" + seed;
            mesh.SetVertices(s.points);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(s.wood, 0);
            mesh.SetTriangles(s.leaf, 1);

            // Aucun sommet n'est partage entre deux faces : chaque face garde donc sa
            // propre normale et l'arbre reste facette, comme le reste du jeu.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static float Fir(Shape s, System.Random rng)
        {
            float h = 11f + (float)rng.NextDouble() * 7f;
            Vector3 lean = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.5f, 0f,
                                       ((float)rng.NextDouble() - 0.5f) * 0.5f);
            Trunk(s, Vector3.zero, h, 0.30f, lean, 6, rng, 0.22f);

            // Les jupes commencent bas et retrecissent : la silhouette classique, mais
            // chacune est de travers, donc le profil n'est jamais un triangle parfait.
            int skirts = 7 + rng.Next(3);
            float start = h * 0.16f;
            for (int i = 0; i < skirts; i++)
            {
                float t = i / (float)(skirts - 1);
                float y = Mathf.Lerp(start, h * 0.97f, t);
                float r = Mathf.Lerp(2.5f, 0.45f, t * t * 0.85f + t * 0.15f);
                Vector3 off = lean * Mathf.Pow(y / h, 2f) * 0.9f;
                Skirt(s, new Vector3(off.x, y, off.z), r, 2.1f - t * 1.1f, rng);
            }
            return h;
        }

        static float Beech(Shape s, System.Random rng)
        {
            float h = 13f + (float)rng.NextDouble() * 8f;
            Vector3 lean = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.1f, 0f,
                                       ((float)rng.NextDouble() - 0.5f) * 1.1f);
            Vector3 top = Trunk(s, Vector3.zero, h, 0.42f, lean, 7, rng, 0.30f);

            // Fut nu jusqu'aux deux tiers, puis une couronne large : c'est ce qui fait
            // une futaie ou le regard porte au ras du sol mais bute sur le plafond.
            int arms = 4 + rng.Next(3);
            for (int i = 0; i < arms; i++)
            {
                float a = (i / (float)arms) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.7f;
                Vector3 dir = new Vector3(Mathf.Cos(a) * 0.85f, 0.85f + (float)rng.NextDouble() * 0.4f,
                                          Mathf.Sin(a) * 0.85f);
                float len = 2.6f + (float)rng.NextDouble() * 2.2f;
                Vector3 tip = Branch(s, top + new Vector3(0f, -0.6f, 0f), dir, len, 0.17f, rng);
                Foliage(s, tip + dir.normalized * 0.9f, 2.2f + (float)rng.NextDouble() * 1.1f,
                        0.78f, rng);
            }
            Foliage(s, top + new Vector3(0f, 1.5f, 0f), 2.6f, 0.72f, rng);
            return h;
        }

        static float Birch(Shape s, System.Random rng)
        {
            float h = 9f + (float)rng.NextDouble() * 5f;
            Vector3 lean = new Vector3(((float)rng.NextDouble() - 0.5f) * 2.4f, 0f,
                                       ((float)rng.NextDouble() - 0.5f) * 2.4f);
            Vector3 top = Trunk(s, Vector3.zero, h, 0.19f, lean, 5, rng, 0.42f);

            int arms = 3 + rng.Next(2);
            for (int i = 0; i < arms; i++)
            {
                float a = (i / (float)arms) * Mathf.PI * 2f + (float)rng.NextDouble();
                Vector3 dir = new Vector3(Mathf.Cos(a), 1.3f, Mathf.Sin(a));
                Vector3 tip = Branch(s, top + new Vector3(0f, -1.4f - i * 0.8f, 0f), dir,
                                     1.8f + (float)rng.NextDouble() * 1.2f, 0.09f, rng);
                Foliage(s, tip, 1.3f + (float)rng.NextDouble() * 0.7f, 0.85f, rng);
            }
            Foliage(s, top + new Vector3(0f, 0.7f, 0f), 1.6f, 0.9f, rng);
            return h;
        }

        static float Dead(Shape s, System.Random rng)
        {
            float h = 7f + (float)rng.NextDouble() * 8f;
            Vector3 lean = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.8f, 0f,
                                       ((float)rng.NextDouble() - 0.5f) * 1.8f);
            Vector3 top = Trunk(s, Vector3.zero, h, 0.34f, lean, 5, rng, 0.14f);

            // Des moignons casses, jamais symetriques. Pas une feuille.
            int arms = 3 + rng.Next(4);
            for (int i = 0; i < arms; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float y = Mathf.Lerp(h * 0.35f, h * 0.95f, (float)rng.NextDouble());
                Vector3 dir = new Vector3(Mathf.Cos(a), 0.3f + (float)rng.NextDouble() * 0.9f,
                                          Mathf.Sin(a));
                Vector3 from = new Vector3(lean.x * Mathf.Pow(y / h, 2f), y,
                                           lean.z * Mathf.Pow(y / h, 2f));
                Branch(s, from, dir, 1.4f + (float)rng.NextDouble() * 2.4f, 0.13f, rng);
            }
            return h;
        }
    }
}
