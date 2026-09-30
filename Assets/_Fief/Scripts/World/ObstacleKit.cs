using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA BOITE A OBSTACLES (03/10 -- Martin : "il faut revoir tous les obstacles pour les
    /// rendre magnifiques. Tous, tous, tous.").
    ///
    /// Tous les pieges de la tour et des couloirs sont faits des MEMES pieces et des MEMES
    /// couleurs : c'est ce qui les fait paraitre d'une seule main, comme les jouets d'un
    /// meme coffre (Fall Guys, Mario Party). La famille :
    ///   - ce qui FRAPPE est ROUGE satine, avec une face de frappe CREME et une cible rouge
    ///     (le butoir, le poing du belier, la masse du marteau, les rayures des barres,
    ///     les boulets) -- on apprend une fois "le rouge frappe", on le lit partout ;
    ///   - ce qui PORTE est en ARDOISE bleu nuit, comme les toits du chateau (potences,
    ///     portails, poteaux, traverses), avec des bagues d'OR MAT ;
    ///   - ce qui PREVIENT LUIT AMBRE (bandes, chevrons, pointes des barres) ;
    ///   - rien de carre qui frappe : des futs aux bords ARRONDIS, tournes d'une piece
    ///     (Proto.Lathe), des domes, des boules.
    ///
    /// Et c'est aussi ce qui les rend LEGERS : chaque obstacle qui bouge est SOUDE
    /// (Proto.Weld) en un seul maillage par piece mobile -- une herse de couloir, c'etaient
    /// cent quinze pointes dessinees une par une ; soudee, c'est un seul dessin.
    /// </summary>
    public static class ObstacleKit
    {
        public static Material Red { get { return MaterialFactory.GetShiny(new Color(0.88f, 0.17f, 0.16f), 0.72f, 0f); } }
        public static Material Cream { get { return MaterialFactory.GetShiny(new Color(0.97f, 0.94f, 0.87f), 0.6f, 0f); } }
        public static Material Slate { get { return MaterialFactory.GetShiny(new Color(0.22f, 0.3f, 0.46f), 0.55f, 0.1f); } }
        public static Material SlateDark { get { return MaterialFactory.GetShiny(new Color(0.13f, 0.17f, 0.27f), 0.45f, 0.1f); } }
        public static Material Steel { get { return MaterialFactory.GetShiny(new Color(0.36f, 0.37f, 0.42f), 0.62f, 0.7f); } }
        public static Material Gold { get { return MaterialFactory.GetShiny(new Color(0.86f, 0.66f, 0.32f), 0.38f, 0.55f); } }
        public static Material Stone { get { return MaterialFactory.GetShiny(new Color(0.86f, 0.8f, 0.69f), 0.12f, 0f); } }
        public static Material Hole { get { return MaterialFactory.GetShiny(new Color(0.06f, 0.06f, 0.08f), 0.2f, 0f); } }
        public static Material Amber { get { return MaterialFactory.GetGlow(new Color(1f, 0.55f, 0.18f), 1.5f); } }

        /// <summary>
        /// UN FUT aux bords arrondis ("round" : le rayon de l'arrondi, 0 = aretes vives),
        /// centre en "centre" (local a "parent"), couche le long de "axis". C'est la piece
        /// a tout faire : un butoir, une bague, un moyeu, un poteau, un disque.
        /// </summary>
        public static GameObject Drum(Transform parent, Vector3 centre, Vector3 axis, float radius, float length, float round, Material m, string name)
        {
            Vector3 dir = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
            round = Mathf.Clamp(round, 0f, Mathf.Min(radius, length * 0.5f) * 0.98f);
            GameObject go = Proto.Lathe(parent, centre - dir * (length * 0.5f), DrumProfile(radius, length, round), Sides(radius), Color.white, name);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>
        /// UN ANNEAU (creux au milieu : on voit a travers, de face), centre en "centre", le long
        /// de "axis" : un portail, un cercle d'or autour d'un trou. (Un fut plein cacherait ce
        /// qu'il y a dedans.) Aretes vives.
        /// </summary>
        public static GameObject Ring(Transform parent, Vector3 centre, Vector3 axis, float inner, float outer, float length, Material m, string name)
        {
            Vector3 dir = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
            Vector2[] p =
            {
                new Vector2(inner, 0f), new Vector2(outer, 0f), new Vector2(outer, 0f), new Vector2(outer, length),
                new Vector2(outer, length), new Vector2(inner, length), new Vector2(inner, length), new Vector2(inner, 0f)
            };
            GameObject go = Proto.Lathe(parent, centre - dir * (length * 0.5f), p, Sides(outer), Color.white, name);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>UN DOME (une demi-boule) pose en "basePos", tourne vers "axis".</summary>
        public static GameObject Dome(Transform parent, Vector3 basePos, Vector3 axis, float radius, Material m, string name)
        {
            Vector3 dir = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
            Vector2[] p = new Vector2[6];
            for (int i = 0; i < p.Length; i++)
            {
                float a = i / (float)(p.Length - 1) * Mathf.PI * 0.5f;
                p[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            }
            GameObject go = Proto.Lathe(parent, basePos, p, Sides(radius), Color.white, name);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>Une boule (sans collider).</summary>
        public static GameObject Ball(Transform parent, Vector3 centre, float radius, Material m, string name)
        {
            bool was = Proto.CollidersEnabled;
            Proto.CollidersEnabled = false;
            GameObject go = Proto.Sphere(parent, centre, Vector3.one * radius * 2f, Color.white, name);
            Proto.CollidersEnabled = was;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>Un pave (sans collider), tourne de "rotation" (locale).</summary>
        public static GameObject Slab(Transform parent, Vector3 centre, Quaternion rotation, Vector3 size, Material m, string name)
        {
            bool was = Proto.CollidersEnabled;
            Proto.CollidersEnabled = false;
            GameObject go = Proto.Cube(parent, centre, size, Color.white, name);
            Proto.CollidersEnabled = was;
            go.transform.localRotation = rotation;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>
        /// UN CHEVRON "&gt;" peint dans le plan (x, y) de "frame" (local a "parent"), qui pointe
        /// vers le +x de "frame" : "par ici". Deux barres a 45 degres.
        /// </summary>
        public static void Chevron(Transform parent, Vector3 centre, Quaternion frame, float size, Material m, string name)
        {
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 local = new Vector3(-size * 0.25f, k * size * 0.25f, 0f);
                Slab(parent, centre + frame * local, frame * Quaternion.Euler(0f, 0f, -k * 45f), new Vector3(size * 0.72f, size * 0.2f, 0.06f), m, name);
            }
        }

        /// <summary>
        /// LA PERCHE (un poteau tourne) : un socle d'ardoise, un fut creme, une bague d'or,
        /// un dome d'ardoise et une boule d'or au sommet -- les colonnes des pieds de rampe,
        /// en petit. "height" : jusqu'au haut du fut.
        /// </summary>
        public static void Post(Transform parent, Vector3 foot, float radius, float height, string name)
        {
            Drum(parent, foot + Vector3.up * 0.14f, Vector3.up, radius * 1.45f, 0.28f, 0.07f, Slate, name + " (socle)");
            Drum(parent, foot + Vector3.up * (height * 0.5f + 0.2f), Vector3.up, radius, height - 0.2f, 0.04f, Stone, name);
            Drum(parent, foot + Vector3.up * (height + 0.02f), Vector3.up, radius * 1.22f, 0.2f, 0.05f, Gold, name + " (bague)");
            Dome(parent, foot + Vector3.up * (height + 0.1f), Vector3.up, radius * 1.1f, Slate, name + " (dôme)");
            Ball(parent, foot + Vector3.up * (height + 0.1f + radius * 1.15f), radius * 0.32f, Gold, name + " (boule)");
        }

        static int Sides(float radius) { return Mathf.Clamp(Mathf.RoundToInt(12f + radius * 10f), 12, 28); }

        /// <summary>La silhouette d'un fut de "radius" x "length", aux bords arrondis de "round".</summary>
        static Vector2[] DrumProfile(float radius, float length, float round)
        {
            if (round < 0.01f)
            {
                // Aretes vives : chaque coin est un point DOUBLE (la lumiere ne glisse pas
                // du flanc au couvercle).
                return new[] { new Vector2(radius, 0f), new Vector2(radius, length), new Vector2(radius, length), new Vector2(0f, length) };
            }
            const int Steps = 3;
            Vector2[] p = new Vector2[(Steps + 1) * 2 + 1];
            int n = 0;
            for (int i = 0; i <= Steps; i++)
            {
                // Le coin du bas : de "vers le bas" a "vers l'exterieur".
                float a = -Mathf.PI * 0.5f + i / (float)Steps * Mathf.PI * 0.5f;
                p[n++] = new Vector2(radius - round + Mathf.Cos(a) * round, round + Mathf.Sin(a) * round);
            }
            for (int i = 0; i <= Steps; i++)
            {
                // Le coin du haut : de "vers l'exterieur" a "vers le haut".
                float a = i / (float)Steps * Mathf.PI * 0.5f;
                p[n++] = new Vector2(radius - round + Mathf.Cos(a) * round, length - round + Mathf.Sin(a) * round);
            }
            p[n] = new Vector2(0f, length);
            return p;
        }
    }
}
