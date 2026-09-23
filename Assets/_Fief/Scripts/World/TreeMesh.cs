using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    public enum TreeKind
    {
        /// <summary>Sapin noir : haut, etroit, branches en etoile qui retombent. L'essence dominante.</summary>
        Fir,
        /// <summary>Hetre : fut pale et nu sur 6 m, houppier haut et touffu. Il capte la lumiere.</summary>
        Beech,
        /// <summary>Bouleau tordu : penche, clairseme, il casse la verticalite.</summary>
        Birch,
        /// <summary>Arbre mort : troncs et branches nues. C'est lui qui fait peur.</summary>
        Dead
    }

    /// <summary>Ce que le placement a besoin de savoir d'un modele.</summary>
    public struct TreeInfo
    {
        public float height;
        /// <summary>Rayon du fut au-dessus des racines. Sert au collider.</summary>
        public float trunkRadius;
        /// <summary>
        /// Ou se trouve vraiment le fut a hauteur d'homme. Il serpente : a 1,5 m il
        /// est decale d'environ un demi-rayon par rapport au pied, et une capsule
        /// centree sur le pied laisserait mordre dans l'ecorce d'un cote.
        /// </summary>
        public Vector3 trunkCentre;
    }

    /// <summary>
    /// Fabrique les maillages d'arbres.
    ///
    /// CE QUI REND UN ARBRE LOW-POLY BEAU, dans l'ordre de ce que ca rapporte :
    ///
    ///   1. LE FEUILLAGE EST OMBRE PAR SES PROPRES FACES. Chaque triangle de
    ///      feuillage est range selon la direction ou il regarde : vers le bas il
    ///      est sombre, de cote il est moyen, vers le ciel il est clair. C'est la
    ///      lumiere du ciel peinte dans le modele. Sans ca un houppier est un aplat,
    ///      avec ca il a du volume meme sous une lumiere faible -- et justement, la
    ///      lumiere est faible ici.
    ///   2. LES SAPINS SONT EN ETOILE. Chaque etage alterne des pointes longues qui
    ///      retombent et des creux plus hauts : on lit des branches, pas des cones.
    ///   3. LE PIED EST VIVANT. Racines qui plongent dans le sol, et la mousse qui
    ///      monte sur le premier metre : c'est ce qui dit "humide", donc "sombre".
    ///   4. LE FUT SERPENTE ET S'EVASE. Jamais un poteau.
    ///
    /// CA NE COUTE RIEN, parce qu'on n'en fabrique que vingt-quatre. Les milliers
    /// d'arbres du monde partagent ces maillages ; seules position, rotation et
    /// echelle changent. Un arbre peut donc se permettre 300 a 600 triangles.
    ///
    /// CINQ SOUS-MAILLAGES : ecorce, mousse, feuillage a l'ombre, au milieu, au
    /// soleil. Chacun a son materiau. Un arbre mort n'a que les deux premiers.
    ///
    /// LE PIEGE DES FACES A L'ENVERS. Un maillage genere a la main dont les triangles
    /// tournent dans le mauvais sens est INVISIBLE de l'exterieur -- et impossible a
    /// deviner sans lancer le jeu. Le triangle (v0, v1, v2) est vu de face quand
    /// Cross(v1 - v0, v2 - v0) pointe VERS L'EXTERIEUR. Tous les assemblages
    /// ci-dessous (Tube, Cone, Floor, Roof, etoile) ont ete verifies en calculant
    /// leur volume signe, qui doit etre positif : ne pas les reordonner.
    /// </summary>
    public static class TreeMesh
    {
        public const int BarkPart = 0;
        public const int MossPart = 1;
        public const int ShadePart = 2;
        public const int MidPart = 3;
        public const int LitPart = 4;
        public const int PartCount = 5;

        // ------------------------------------------------------------------ atelier

        class Shape
        {
            public readonly List<Vector3> points = new List<Vector3>();
            public readonly List<int> bark = new List<int>();
            public readonly List<int> moss = new List<int>();
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

            /// <summary>Fond, tourne a l'oppose de la direction de l'anneau.</summary>
            public void Floor(Vector3[] ring, Vector3 centre, List<int> into)
            {
                int n = ring.Length;
                for (int i = 0; i < n; i++) Tri(centre, ring[(i + 1) % n], ring[i], into);
            }

            /// <summary>Couvercle, tourne DANS la direction de l'anneau.</summary>
            public void Roof(Vector3[] ring, Vector3 centre, List<int> into)
            {
                int n = ring.Length;
                for (int i = 0; i < n; i++) Tri(centre, ring[i], ring[(i + 1) % n], into);
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

        static float R(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>
        /// Un fut : une suite d'anneaux qui montent en s'affinant, serres en bas pour
        /// dessiner l'evasement des racines. Le premier troncon est moussu.
        /// Renvoie le sommet.
        /// </summary>
        static Vector3 Trunk(Shape s, float height, float radius, Vector3 lean, int sides,
                             System.Random rng, float taper, ref TreeInfo info)
        {
            // Meme derive que les anneaux ci-dessous, evaluee a hauteur d'homme.
            float tm = Mathf.Clamp01(1.5f / Mathf.Max(1f, height));
            info.trunkCentre = lean * (tm * tm) + new Vector3(
                Mathf.Sin(tm * 3.1f) * radius * 0.7f, 0f, Mathf.Cos(tm * 2.3f) * radius * 0.6f);

            // Anneaux plus rapproches en bas : c'est la que la forme change vite.
            float[] level = { 0f, 0.05f, 0.13f, 0.28f, 0.48f, 0.71f, 1f };
            Vector3[] previous = null;
            Vector3 top = Vector3.zero;

            for (int i = 0; i < level.Length; i++)
            {
                float t = level[i];
                float flare = t < 0.13f ? Mathf.Lerp(1.9f, 1f, t / 0.13f) : 1f;
                float r = radius * Mathf.Lerp(1f, taper, t) * flare;

                // Derive en S : une courbe, pas une inclinaison uniforme.
                Vector3 drift = lean * (t * t) + new Vector3(
                    Mathf.Sin(t * 3.1f) * radius * 0.7f, 0f, Mathf.Cos(t * 2.3f) * radius * 0.6f);

                Vector3 centre = new Vector3(0f, height * t, 0f) + drift;
                Vector3[] ring = Ring(centre, Vector3.up + lean * 0.4f, r, sides,
                                      i * 0.21f, rng, 0.16f);

                if (previous != null) s.Tube(previous, ring, i == 1 ? s.moss : s.bark);
                previous = ring;
                top = centre;
            }

            Roots(s, radius, sides, rng);
            return top;
        }

        /// <summary>Des racines qui partent du pied et plongent sous la mousse.</summary>
        static void Roots(Shape s, float radius, int count, System.Random rng)
        {
            float phase = R(rng, 0f, 6.28f);
            for (int i = 0; i < count; i++)
            {
                if (rng.NextDouble() < 0.3) continue;
                float a = phase + (i / (float)count) * Mathf.PI * 2f + R(rng, -0.25f, 0.25f);
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 from = outward * radius * 0.9f + new Vector3(0f, 0.30f, 0f);
                Vector3 dir = outward + new Vector3(0f, -0.42f, 0f);
                Limb(s, from, dir, radius * R(rng, 2.2f, 3.4f), radius * 0.46f, rng, s.moss);
            }
        }

        /// <summary>Une branche ou une racine : un cone effile, plante en biais.</summary>
        static Vector3 Limb(Shape s, Vector3 from, Vector3 direction, float length,
                            float radius, System.Random rng, List<int> into)
        {
            Vector3 d = direction.normalized;
            Vector3 tip = from + d * length;
            Vector3[] ring = Ring(from, d, radius, 4, 0.4f, rng, 0.2f);
            Vector3[] mid = Ring(from + d * length * 0.55f, d, radius * 0.5f, 4, 0.9f, rng, 0.25f);
            s.Tube(ring, mid, into);
            s.Cone(mid, tip, into);
            return tip;
        }

        /// <summary>
        /// Un volume de feuillage : une sphere cabossee, faite d'anneaux. On empile
        /// ces volumes au bout des branches : le houppier a une forme au lieu d'un cube.
        /// </summary>
        static void Foliage(Shape s, Vector3 centre, float radius, float squash, System.Random rng)
        {
            const int Sides = 6;
            float[] level = { -0.62f, -0.12f, 0.42f };

            Vector3[] previous = null;
            for (int i = 0; i < level.Length; i++)
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

        /// <summary>
        /// Un etage de sapin EN ETOILE : des pointes longues qui retombent, des creux
        /// courts et plus hauts entre elles. Le repere est le meme que Ring() autour
        /// de la verticale -- (sin, 0, cos) -- pour garder le meme sens de faces.
        /// </summary>
        static void StarSkirt(Shape s, Vector3 centre, float radius, float height,
                              int arms, System.Random rng)
        {
            float phase = R(rng, 0f, 6.28f);
            float droop = radius * 0.34f;
            int n = arms * 2;
            Vector3[] ring = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                bool tip = (i % 2) == 0;
                float a = phase + (i / (float)n) * Mathf.PI * 2f;
                float r = tip ? radius * R(rng, 0.88f, 1.12f) : radius * R(rng, 0.46f, 0.58f);
                float y = tip ? -droop * R(rng, 0.8f, 1.2f) : droop * 0.15f;
                ring[i] = centre + new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
            }
            s.Floor(ring, centre + new Vector3(0f, -droop * 0.25f, 0f), s.leaf);
            s.Cone(ring, centre + new Vector3(0f, height, 0f), s.leaf);
        }

        // ------------------------------------------------------------------ essences

        public static Mesh Build(TreeKind kind, int seed, out TreeInfo info)
        {
            System.Random rng = new System.Random(seed);
            Shape s = new Shape();
            info = new TreeInfo();

            switch (kind)
            {
                case TreeKind.Fir: Fir(s, rng, ref info); break;
                case TreeKind.Beech: Beech(s, rng, ref info); break;
                case TreeKind.Birch: Birch(s, rng, ref info); break;
                default: Dead(s, rng, ref info); break;
            }
            return Finish(s, kind + "_" + seed, info.height);
        }

        /// <summary>
        /// Range chaque triangle de feuillage selon la direction ou il regarde, avec
        /// un leger biais de hauteur : le bas du houppier est plus sombre que le haut,
        /// comme sous un vrai couvert ou la lumiere vient d'en haut.
        /// </summary>
        static Mesh Finish(Shape s, string name, float height)
        {
            List<int> shade = new List<int>();
            List<int> mid = new List<int>();
            List<int> lit = new List<int>();
            float h = Mathf.Max(1f, height);

            for (int i = 0; i < s.leaf.Count; i += 3)
            {
                Vector3 a = s.points[s.leaf[i]];
                Vector3 b = s.points[s.leaf[i + 1]];
                Vector3 c = s.points[s.leaf[i + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                float y = (a.y + b.y + c.y) / 3f;

                float light = n.y * 0.8f + (y / h - 0.55f) * 0.6f;
                List<int> into = light < -0.18f ? shade : (light > 0.32f ? lit : mid);
                into.Add(s.leaf[i]); into.Add(s.leaf[i + 1]); into.Add(s.leaf[i + 2]);
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(s.points);
            mesh.subMeshCount = PartCount;
            mesh.SetTriangles(s.bark, BarkPart);
            mesh.SetTriangles(s.moss, MossPart);
            mesh.SetTriangles(shade, ShadePart);
            mesh.SetTriangles(mid, MidPart);
            mesh.SetTriangles(lit, LitPart);

            // Aucun sommet n'est partage entre deux faces : chaque face garde sa
            // propre normale et l'arbre reste facette, comme le reste du jeu.
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Fir(Shape s, System.Random rng, ref TreeInfo info)
        {
            float h = R(rng, 11f, 18f);
            Vector3 lean = new Vector3(R(rng, -0.25f, 0.25f), 0f, R(rng, -0.25f, 0.25f));
            Trunk(s, h, 0.30f, lean, 6, rng, 0.22f, ref info);

            // Les etages commencent bas et retrecissent. Chacun est une etoile de
            // sept pointes : on lit des branches, pas un cone.
            int tiers = 8 + rng.Next(3);
            float start = h * 0.15f;
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)(tiers - 1);
                float y = Mathf.Lerp(start, h * 0.97f, t);
                float r = Mathf.Lerp(2.7f, 0.42f, t * t * 0.85f + t * 0.15f);
                Vector3 off = lean * Mathf.Pow(y / h, 2f) * 0.9f;
                StarSkirt(s, new Vector3(off.x, y, off.z), r, 1.9f - t * 1.0f, 7, rng);
            }
            info.height = h;
            info.trunkRadius = 0.30f;
        }

        static void Beech(Shape s, System.Random rng, ref TreeInfo info)
        {
            float h = R(rng, 13f, 21f);
            Vector3 lean = new Vector3(R(rng, -0.55f, 0.55f), 0f, R(rng, -0.55f, 0.55f));
            Vector3 top = Trunk(s, h, 0.42f, lean, 7, rng, 0.30f, ref info);

            // Fut nu jusqu'aux deux tiers, puis une couronne faite de nombreux volumes
            // plus petits : c'est leur nombre qui donne l'air touffu, pas leur taille.
            int arms = 5 + rng.Next(3);
            for (int i = 0; i < arms; i++)
            {
                float a = (i / (float)arms) * Mathf.PI * 2f + R(rng, 0f, 0.7f);
                Vector3 dir = new Vector3(Mathf.Cos(a) * 0.85f, R(rng, 0.85f, 1.25f), Mathf.Sin(a) * 0.85f);
                float len = R(rng, 2.6f, 4.8f);
                Vector3 from = top + new Vector3(0f, R(rng, -1.6f, -0.4f), 0f);
                Vector3 tip = Limb(s, from, dir, len, 0.17f, rng, s.bark);

                Foliage(s, tip + dir.normalized * 0.7f, R(rng, 1.7f, 2.5f), R(rng, 0.66f, 0.86f), rng);
                Foliage(s, Vector3.Lerp(from, tip, 0.55f) + new Vector3(0f, 0.9f, 0f),
                        R(rng, 1.2f, 1.7f), 0.8f, rng);
            }
            Foliage(s, top + new Vector3(0f, 1.6f, 0f), 2.4f, 0.72f, rng);
            info.height = h;
            info.trunkRadius = 0.42f;
        }

        static void Birch(Shape s, System.Random rng, ref TreeInfo info)
        {
            float h = R(rng, 9f, 14f);
            Vector3 lean = new Vector3(R(rng, -1.2f, 1.2f), 0f, R(rng, -1.2f, 1.2f));
            Vector3 top = Trunk(s, h, 0.19f, lean, 5, rng, 0.42f, ref info);

            int arms = 3 + rng.Next(3);
            for (int i = 0; i < arms; i++)
            {
                float a = (i / (float)arms) * Mathf.PI * 2f + R(rng, 0f, 1f);
                Vector3 dir = new Vector3(Mathf.Cos(a), 1.3f, Mathf.Sin(a));
                Vector3 tip = Limb(s, top + new Vector3(0f, -1.4f - i * 0.8f, 0f), dir,
                                   R(rng, 1.8f, 3.0f), 0.09f, rng, s.bark);
                Foliage(s, tip, R(rng, 1.1f, 1.8f), 0.85f, rng);
            }
            Foliage(s, top + new Vector3(0f, 0.7f, 0f), 1.5f, 0.9f, rng);
            info.height = h;
            info.trunkRadius = 0.19f;
        }

        static void Dead(Shape s, System.Random rng, ref TreeInfo info)
        {
            float h = R(rng, 7f, 15f);
            Vector3 lean = new Vector3(R(rng, -0.9f, 0.9f), 0f, R(rng, -0.9f, 0.9f));
            Trunk(s, h, 0.34f, lean, 5, rng, 0.14f, ref info);

            // Des moignons casses, jamais symetriques. Pas une feuille.
            int arms = 3 + rng.Next(4);
            for (int i = 0; i < arms; i++)
            {
                float a = R(rng, 0f, Mathf.PI * 2f);
                float y = Mathf.Lerp(h * 0.35f, h * 0.95f, (float)rng.NextDouble());
                Vector3 dir = new Vector3(Mathf.Cos(a), R(rng, 0.3f, 1.2f), Mathf.Sin(a));
                Vector3 from = new Vector3(lean.x * Mathf.Pow(y / h, 2f), y, lean.z * Mathf.Pow(y / h, 2f));
                Limb(s, from, dir, R(rng, 1.4f, 3.8f), 0.13f, rng, s.bark);
            }
            info.height = h;
            info.trunkRadius = 0.34f;
        }

        // ------------------------------------------------------------------ bois mort

        /// <summary>
        /// Un tronc couche : l'axe est X, la mousse prend tout ce qui regarde le ciel.
        /// On s'en sert pour se cacher derriere, et c'est un obstacle.
        /// </summary>
        public static Mesh BuildLog(int seed, out float length, out float radius)
        {
            System.Random rng = new System.Random(seed);
            Shape s = new Shape();
            length = R(rng, 4.5f, 8.5f);
            radius = R(rng, 0.28f, 0.46f);

            const int Steps = 5;
            Vector3 axis = Vector3.right;
            Vector3[] first = null;
            Vector3[] previous = null;
            Vector3 startCentre = Vector3.zero;
            Vector3 endCentre = Vector3.zero;

            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps;
                float r = radius * Mathf.Lerp(1f, 0.72f, t) * R(rng, 0.92f, 1.08f);
                Vector3 c = new Vector3((t - 0.5f) * length, Mathf.Sin(t * 2.7f) * 0.08f,
                                        Mathf.Cos(t * 3.3f) * 0.10f);
                Vector3[] ring = Ring(c, axis, r, 7, 0.3f, rng, 0.14f);
                if (previous == null) { first = ring; startCentre = c; }
                else s.Tube(previous, ring, s.bark);
                previous = ring;
                endCentre = c;
            }
            s.Floor(first, startCentre, s.bark);    // la souche, tournee vers -X
            s.Roof(previous, endCentre, s.bark);    // la cassure, tournee vers +X

            // Deux ou trois chicots de branches, tous vers le haut ou le cote.
            int stubs = 2 + rng.Next(2);
            for (int i = 0; i < stubs; i++)
            {
                float x = R(rng, -0.35f, 0.35f) * length;
                float a = R(rng, -1.3f, 1.3f);
                Vector3 dir = new Vector3(R(rng, -0.4f, 0.4f), Mathf.Cos(a), Mathf.Sin(a));
                Limb(s, new Vector3(x, 0f, 0f), dir, R(rng, 0.6f, 1.3f), radius * 0.35f, rng, s.bark);
            }

            // La mousse prend le dessus : ce qui regarde le ciel est vert.
            List<int> bark = new List<int>();
            for (int i = 0; i < s.bark.Count; i += 3)
            {
                Vector3 a = s.points[s.bark[i]];
                Vector3 b = s.points[s.bark[i + 1]];
                Vector3 c = s.points[s.bark[i + 2]];
                bool up = Vector3.Cross(b - a, c - a).normalized.y > 0.35f;
                List<int> into = up ? s.moss : bark;
                into.Add(s.bark[i]); into.Add(s.bark[i + 1]); into.Add(s.bark[i + 2]);
            }
            s.bark.Clear();
            s.bark.AddRange(bark);

            return Finish(s, "Souche_" + seed, radius * 2f);
        }
    }
}
