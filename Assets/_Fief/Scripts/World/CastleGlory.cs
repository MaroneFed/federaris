using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CHATEAU "DE MALADE" (v49 -- Martin : "je veux un chateau de malade, digne d'Elden Ring").
    /// Une couche de DECOR posee par-dessus la citadelle, sans toucher a une seule regle du jeu :
    /// tout ici est sans collider (sauf le socle des statues, colle au mur comme un contrefort),
    /// les murs, les portes, le sceau et les chemins des bots restent exactement les memes.
    ///
    ///   LES ECHAUGUETTES   quatre tourelles accrochees en haut de chaque tour d'angle, sur un
    ///                      cul-de-lampe, chacune sous son petit toit en cloche, fenetre allumee ;
    ///   LE GOTHIQUE        chaque contrefort monte en pile jusqu'au chemin de ronde et finit
    ///                      par un PINACLE a boule d'or : la muraille devient une dentelle ;
    ///   LES CHATELETS      au-dessus de chaque porte : un arc brise en claveaux, un pignon a
    ///                      redents entre les deux tours, sa ROSACE de vitrail a la couleur de
    ///                      la porte (elle brille : le halo de l'image s'en charge) ;
    ///   LES ROIS-HARICOTS  deux colosses de pierre couronnes d'or mat de part et d'autre de
    ///                      chaque porte -- nos personnages, en statues de huit metres ;
    ///   LE LIERRE          qui grimpe par plaques sur les murailles et au pied des tours ;
    ///   LES BRASEROS       de grandes vasques de feu sur les tours des portes.
    ///
    /// Concept Unity : rien n'est dessine a la main. Ce sont toujours nos briques (cubes,
    /// cylindres, volumes tournes), regroupees ensuite par le "static batching" : des
    /// centaines de pieces, quelques appels a la carte graphique.
    /// </summary>
    public static class CastleGlory
    {
        static readonly Color Stone = new Color(0.82f, 0.75f, 0.64f);
        static readonly Color StoneDark = new Color(0.56f, 0.49f, 0.42f);
        static readonly Color Marble = new Color(0.72f, 0.70f, 0.68f);
        static readonly Color Slate = new Color(0.22f, 0.29f, 0.46f);
        static readonly Color IronDark = new Color(0.14f, 0.14f, 0.18f);
        static readonly Color Warm = new Color(1f, 0.74f, 0.4f);
        static readonly Color[] Leaves = { new Color(0.20f, 0.42f, 0.18f), new Color(0.27f, 0.52f, 0.22f), new Color(0.36f, 0.60f, 0.26f) };

        public static void Build(Transform castle)
        {
            GameObject root = new GameObject("GLOIRE DU CHÂTEAU");
            root.transform.SetParent(castle, false);
            Transform t = root.transform;

            float h = Castle.HalfSize;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

            Proto.BeginVisualOnly();
            try
            {
                for (int i = 0; i < corners.Length; i++)
                {
                    for (int k = 0; k < 4; k++) Bartizan(t, corners[i], Castle.TowerSize * 0.5f, Castle.TowerHeight, (k * 90f + 45f) * Mathf.Deg2Rad);
                    TowerIvy(t, corners[i], Castle.TowerSize * 0.5f, 70 + i);
                }
                for (int i = 0; i < 4; i++)
                {
                    Vector3 a = corners[i], b = corners[(i + 1) % 4];
                    Vector3 dir = (b - a).normalized;
                    Vector3 outward = Vector3.Cross(Vector3.up, dir) * -1f;
                    Gothic(t, a, dir, outward);
                    WallIvy(t, a, dir, outward, 90 + i * 13);
                }
                for (int i = 0; i < dirs.Length; i++)
                {
                    Gatehouse(t, dirs[i], Castle.GateColours[i]);
                    for (int k = -1; k <= 1; k += 2) BeanKing(t, dirs[i], k, Castle.GateColours[i]);
                }
            }
            finally { Proto.EndVisualOnly(); }

            // Les socles des statues, eux, sont solides (on ne traverse pas huit metres de pierre).
            for (int i = 0; i < dirs.Length; i++)
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, dirs[i]);
                    Vector3 at = dirs[i] * (Castle.HalfSize + 2.9f) + side * (k * KingOffset);
                    GameObject plinth = Proto.Cube(t, at + Vector3.up * 1.5f, new Vector3(2.6f, 3f, 2.4f), StoneDark, "Socle du roi");
                    plinth.transform.rotation = Quaternion.LookRotation(dirs[i], Vector3.up);
                }
        }

        // ------------------------------------------------------------------ outils

        static Material Mat(Color c, float smooth) { return MaterialFactory.GetShiny(c, smooth, 0f); }

        static GameObject Box(Transform t, Vector3 pos, Vector3 size, Quaternion rot, Color c, string name)
        {
            GameObject go = Proto.Cube(t, pos, size, c, name);
            go.transform.rotation = rot;
            return go;
        }

        static Mesh blob;

        /// <summary>
        /// Une boule a vingt faces (un "icosaedre") : douze sommets seulement. Une sphere d'Unity
        /// en a cinq cents -- pour des centaines de feuilles de lierre, ca compte.
        /// </summary>
        static Mesh BlobMesh()
        {
            if (blob != null) return blob;
            float p = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] v =
            {
                new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
                new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
                new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1)
            };
            for (int i = 0; i < v.Length; i++) v[i] = v[i].normalized * 0.5f;
            int[] tris =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            blob = new Mesh();
            blob.name = "Feuillage";
            blob.vertices = v;
            blob.triangles = tris;
            blob.RecalculateNormals();
            blob.RecalculateBounds();
            return blob;
        }

        /// <summary>Une touffe de feuillage (lierre, buisson) : la boule a vingt faces, etiree.</summary>
        public static GameObject Leaf(Transform t, Vector3 pos, Vector3 size, Quaternion rot, Color c)
        {
            GameObject go = new GameObject("Feuillage");
            go.transform.SetParent(t, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = BlobMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(c, 0.22f);
            return go;
        }

        // ------------------------------------------------------------------ echauguettes

        /// <summary>Une tourelle en encorbellement sur le haut d'une tour : cul-de-lampe, fut, bandeau, toit en cloche, fenetre.</summary>
        static void Bartizan(Transform t, Vector3 tower, float r, float height, float angle)
        {
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 at = tower + outward * (r + 0.9f);
            float foot = height - 7.6f;
            // Le cul-de-lampe : une goutte de pierre qui s'evase, en trois bourrelets.
            Proto.Lathe(t, new Vector3(at.x, foot, at.z), new[] {
                new Vector2(0.12f, 0f), new Vector2(0.45f, 0.5f), new Vector2(0.6f, 0.9f), new Vector2(0.6f, 0.9f),
                new Vector2(1.0f, 1.5f), new Vector2(1.2f, 1.9f), new Vector2(1.2f, 1.9f), new Vector2(1.62f, 2.6f),
                new Vector2(1.72f, 2.75f), new Vector2(1.72f, 2.75f), new Vector2(0f, 2.75f) }, 14, StoneDark, "Cul-de-lampe")
                .GetComponent<Renderer>().sharedMaterial = Mat(StoneDark, 0.2f);
            Proto.Cylinder(t, new Vector3(at.x, foot + 2.75f + 3.2f, at.z), new Vector3(3.2f, 3.2f, 3.2f), Stone, "Échauguette")
                .GetComponent<Renderer>().sharedMaterial = Mat(Stone, 0.18f);
            Proto.Cylinder(t, new Vector3(at.x, foot + 9.2f, at.z), new Vector3(3.7f, 0.16f, 3.7f), StoneDark, "Bandeau")
                .GetComponent<Renderer>().sharedMaterial = Mat(StoneDark, 0.2f);
            Castle.BellRoof(t, new Vector3(at.x, foot + 9.36f, at.z), 2.15f, 5.4f);
            // Sa fenetre, allumee : de loin, quatre lueurs en haut de chaque tour.
            Quaternion face = Quaternion.LookRotation(outward, Vector3.up);
            GameObject win = Box(t, at + outward * 1.58f + Vector3.up * (foot + 6.6f), new Vector3(0.5f, 1.2f, 0.12f), face, Warm, "Fenêtre allumée");
            win.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Warm, 1.1f);
            Box(t, at + outward * 1.6f + Vector3.up * (foot + 5.85f), new Vector3(0.8f, 0.14f, 0.2f), face, StoneDark, "Appui");
        }

        // ------------------------------------------------------------------ contreforts gothiques

        /// <summary>Chaque contrefort monte en pile, puis en pinacle a boule d'or (les memes places que dans Castle.BuildCurtain).</summary>
        static void Gothic(Transform t, Vector3 a, Vector3 dir, Vector3 outward)
        {
            float h = Castle.HalfSize, gap = Castle.GateWidth * 0.5f, half = Castle.WallThickness * 0.5f;
            Quaternion face = Quaternion.LookRotation(outward, Vector3.up);
            Material gold = Castle.MatteGold;
            for (float u = 12f; u < h * 2f - 12f; u += 12f)
            {
                if (Mathf.Abs(u - h) < gap + 8f) continue;
                Vector3 low = a + dir * u + outward * (half + 1f);
                Vector3 high = a + dir * u + outward * (half + 0.55f);
                Box(t, low + Vector3.up * 7.9f, new Vector3(2.9f, 0.3f, 2.3f), face, StoneDark, "Larmier");
                Box(t, high + Vector3.up * 12.45f, new Vector3(1.5f, 8.9f, 1.1f), face, Stone, "Pile");
                Box(t, high + Vector3.up * 13f, new Vector3(1.7f, 0.22f, 1.3f), face, StoneDark, "Bague");
                Box(t, high + Vector3.up * 17f, new Vector3(1.9f, 0.26f, 1.5f), face, StoneDark, "Tailloir");
                GameObject spire = Proto.Cone(t, high + Vector3.up * 17.12f, 0.78f, 3.9f, Stone, "Pinacle", 4);
                spire.transform.rotation = face * Quaternion.Euler(0f, 45f, 0f);
                spire.GetComponent<Renderer>().sharedMaterial = Mat(Stone, 0.2f);
                // Quatre petits fleurons au pied du pinacle, la boule d'or en haut.
                for (int k = 0; k < 4; k++)
                {
                    Vector3 off = face * new Vector3(k < 2 ? -0.62f : 0.62f, 0f, k % 2 == 0 ? -0.42f : 0.42f);
                    Proto.Cone(t, high + off + Vector3.up * 17.12f, 0.2f, 1.0f, StoneDark, "Fleuron", 4);
                }
                Proto.Sphere(t, high + Vector3.up * 21.05f, Vector3.one * 0.34f, Color.white, "Boule d'or").GetComponent<Renderer>().sharedMaterial = gold;
            }
        }

        // ------------------------------------------------------------------ chatelets

        /// <summary>
        /// La porte devient un CHATELET : un arc brise en claveaux devant le passage (qui reste
        /// aussi large et aussi haut qu'avant, au sol), et, entre les deux tours, un pignon a
        /// redents perce d'une rosace, sous un toit d'ardoise. On passe dessous par le chemin de ronde.
        /// </summary>
        static void Gatehouse(Transform t, Vector3 d, Color colour)
        {
            Vector3 side = Vector3.Cross(Vector3.up, d);
            Vector3 gate = d * Castle.HalfSize;
            Quaternion face = Quaternion.LookRotation(d, Vector3.up);
            float w = Castle.GateWidth * 0.5f, top = Castle.GateHeight;
            Material gold = Castle.MatteGold;

            // --- l'arc brise : deux arcs de cercle qui se rejoignent en pointe
            const float spring = 4.5f;
            float rise = top - spring;
            float c = (rise * rise - w * w) / (2f * w);
            float R = w + c;
            float max = Mathf.Atan2(rise, c);
            const int stones = 9;
            for (int k = 0; k < stones; k++)
            {
                float th = max * (k + 0.5f) / stones;
                float x = -c + (R + 0.34f) * Mathf.Cos(th), y = spring + (R + 0.34f) * Mathf.Sin(th);
                float deg = th * Mathf.Rad2Deg;
                Color tone = k % 2 == 0 ? Stone : StoneDark;
                Vector3 size = new Vector3(0.72f, R * max / stones + 0.06f, 0.5f);
                Box(t, gate + d * 1.72f + side * x + Vector3.up * y, size, Quaternion.AngleAxis(deg, d) * face, tone, "Claveau");
                Box(t, gate + d * 1.72f - side * x + Vector3.up * y, size, Quaternion.AngleAxis(180f - deg, d) * face, tone, "Claveau");
            }
            Box(t, gate + d * 1.78f + Vector3.up * (top + 0.45f), new Vector3(0.9f, 1.3f, 0.6f), face, Color.white, "Clef de voûte").GetComponent<Renderer>().sharedMaterial = gold;
            // Le tympan : la pierre qui remplit les coins, entre l'arc et le linteau.
            for (float x = -w + 0.25f; x < w; x += 0.5f)
            {
                float y = spring + Mathf.Sqrt(Mathf.Max(0f, R * R - (Mathf.Abs(x) + c) * (Mathf.Abs(x) + c)));
                if (top + 0.05f - y < 0.08f) continue;
                Box(t, gate + d * 1.36f + side * x + Vector3.up * ((y + top + 0.05f) * 0.5f), new Vector3(0.5f, top + 0.05f - y, 0.25f), face, Stone, "Tympan");
            }
            // Les impostes : la pierre d'ou part l'arc, de chaque cote.
            for (int k = -1; k <= 1; k += 2)
                Box(t, gate + d * 1.74f + side * (k * (w + 0.3f)) + Vector3.up * (spring - 0.2f), new Vector3(0.95f, 0.4f, 0.62f), face, StoneDark, "Imposte");

            // --- le pignon a redents, au-dessus du chemin de ronde (3,5 m de passage dessous)
            float baseY = Castle.WallHeight + 3.5f;
            Box(t, gate + d * 1f + Vector3.up * (baseY + 0.5f), new Vector3(8.6f, 1f, 3.9f), face, StoneDark, "Poutre du châtelet");
            for (int k = 0; k < 5; k++)
            {
                float width = 8.6f - k * 1.7f;
                float y = baseY + 1.45f + k * 0.9f;
                Box(t, gate + d * 2.6f + Vector3.up * y, new Vector3(width, 0.9f, 0.6f), face, Stone, "Pignon");
                Box(t, gate - d * 0.6f + Vector3.up * y, new Vector3(width, 0.9f, 0.6f), face, Stone, "Pignon");
                // Le chaperon de chaque redent.
                Box(t, gate + d * 2.6f + Vector3.up * (y + 0.5f), new Vector3(width + 0.12f, 0.1f, 0.72f), face, StoneDark, "Chaperon");
            }
            // Le toit d'ardoise : deux pans.
            Material slate = MaterialFactory.GetShiny(Slate, 0.45f, 0.05f);
            float eave = baseY + 1f, ridge = baseY + 4.7f, reach = 4.4f;
            float len = Mathf.Sqrt(reach * reach + (ridge - eave) * (ridge - eave));
            float tilt = Mathf.Atan2(ridge - eave, reach) * Mathf.Rad2Deg;
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject pan = Box(t, gate + d * 1f + side * (k * reach * 0.5f) + Vector3.up * ((eave + ridge) * 0.5f), new Vector3(len + 0.3f, 0.24f, 2.7f),
                                     Quaternion.AngleAxis(-k * tilt, d) * face, Slate, "Pan de toit");
                pan.GetComponent<Renderer>().sharedMaterial = slate;
            }
            Box(t, gate + d * 1f + Vector3.up * (ridge + 0.1f), new Vector3(0.26f, 0.26f, 2.9f), face, StoneDark, "Faîtage");
            Proto.Sphere(t, gate + d * 2.6f + Vector3.up * (baseY + 5.85f), Vector3.one * 0.42f, Color.white, "Épi").GetComponent<Renderer>().sharedMaterial = gold;
            Proto.Sphere(t, gate - d * 0.6f + Vector3.up * (baseY + 5.85f), Vector3.one * 0.42f, Color.white, "Épi").GetComponent<Renderer>().sharedMaterial = gold;

            // --- la rosace : un vitrail rond a la couleur de la porte, qui brille
            Vector3 rose = gate + d * 2.92f + Vector3.up * (baseY + 2.55f);
            Color glassTint = Color.Lerp(colour, Warm, 0.4f);
            GameObject glass = Proto.Cylinder(t, rose, new Vector3(2.5f, 0.03f, 2.5f), glassTint, "Rosace");
            glass.transform.rotation = face * Quaternion.Euler(90f, 0f, 0f);
            glass.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(glassTint, 1.25f);
            for (int k = 0; k < 14; k++)
            {
                float a = k / 14f * 360f;
                Vector3 off = Quaternion.AngleAxis(a, d) * side * 1.36f;
                Box(t, rose + off + d * 0.02f, new Vector3(0.3f, 0.66f, 0.3f), Quaternion.AngleAxis(a, d) * face, k % 2 == 0 ? StoneDark : Stone, "Pierre de rosace");
            }
            for (int k = 0; k < 3; k++)
                Box(t, rose + d * 0.05f, new Vector3(2.5f, 0.09f, 0.06f), Quaternion.AngleAxis(k * 60f, d) * face, IronDark, "Plomb du vitrail");
            Proto.Sphere(t, rose + d * 0.06f, new Vector3(0.44f, 0.44f, 0.44f), Color.white, "Cœur de rosace").GetComponent<Renderer>().sharedMaterial = gold;

            // --- deux vasques de feu sur le chemin de ronde, de chaque cote du passage
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 p = gate + d * 2.45f + side * (k * 3.4f) + Vector3.up * (Castle.WallHeight + 1.6f);
                Proto.Sphere(t, p, new Vector3(0.9f, 0.42f, 0.9f), IronDark, "Vasque");
                GameObject fire = Proto.Sphere(t, p + Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.6f), Warm, "Feu de vasque");
                fire.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.58f, 0.2f), 1.8f);
                fire.AddComponent<Flame>();
            }
        }

        // ------------------------------------------------------------------ rois-haricots

        const float KingOffset = 18f;

        /// <summary>
        /// UN ROI-HARICOT DE PIERRE : notre personnage en colosse, couronne d'or mat, sur son
        /// socle au blason de la porte. Deux par porte ; ils regardent ceux qui arrivent.
        /// </summary>
        static void BeanKing(Transform t, Vector3 d, int k, Color colour)
        {
            Vector3 side = Vector3.Cross(Vector3.up, d);
            Vector3 foot = d * (Castle.HalfSize + 2.9f) + side * (k * KingOffset);
            Quaternion face = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(0f, -k * 14f, 0f);
            Vector3 fwd = face * Vector3.forward, right = face * Vector3.right;
            Material marble = Mat(Marble, 0.3f);
            Material pale = Mat(new Color(0.9f, 0.88f, 0.84f), 0.35f);
            Material deep = Mat(new Color(0.3f, 0.29f, 0.3f), 0.4f);
            Material gold = Castle.MatteGold;

            Box(t, foot + Vector3.up * 3.12f, new Vector3(3f, 0.28f, 2.8f), Quaternion.LookRotation(d, Vector3.up), Stone, "Dalle du socle");
            Vector3 mid = foot + Vector3.up * 5.6f;
            GameObject body = Proto.Capsule(t, mid, new Vector3(2.5f, 2.3f, 2.3f), Marble, "Roi-haricot");
            body.transform.rotation = face;
            body.GetComponent<Renderer>().sharedMaterial = marble;
            // Les grands yeux.
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 eye = mid + Vector3.up * 0.95f + right * (e * 0.5f) + fwd * 1.0f;
                GameObject white = Proto.Sphere(t, eye, new Vector3(0.66f, 0.84f, 0.34f), Color.white, "Œil");
                white.transform.rotation = face;
                white.GetComponent<Renderer>().sharedMaterial = pale;
                GameObject pupil = Proto.Sphere(t, eye + fwd * 0.13f - Vector3.up * 0.06f, new Vector3(0.34f, 0.46f, 0.16f), Color.white, "Pupille");
                pupil.transform.rotation = face;
                pupil.GetComponent<Renderer>().sharedMaterial = deep;
                // Les petites mains rondes, et les pieds.
                Proto.Sphere(t, mid + right * (e * 1.36f) - Vector3.up * 0.25f + fwd * 0.2f, Vector3.one * 0.62f, Color.white, "Main").GetComponent<Renderer>().sharedMaterial = marble;
                GameObject shoe = Proto.Sphere(t, foot + Vector3.up * 3.42f + right * (e * 0.62f) + fwd * 0.5f, new Vector3(0.9f, 0.5f, 1.2f), Color.white, "Pied");
                shoe.transform.rotation = face;
                shoe.GetComponent<Renderer>().sharedMaterial = marble;
            }
            // La couronne : un bandeau et cinq pointes.
            Proto.Cylinder(t, mid + Vector3.up * 1.93f, new Vector3(1.86f, 0.2f, 1.86f), Color.white, "Bandeau de couronne").GetComponent<Renderer>().sharedMaterial = gold;
            for (int s = 0; s < 6; s++)
            {
                float a = s / 6f * Mathf.PI * 2f;
                Proto.Cone(t, mid + Vector3.up * 2.1f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.8f, 0.2f, 0.62f, Color.white, "Pointe de couronne", 6)
                    .GetComponent<Renderer>().sharedMaterial = gold;
            }
            // Le blason de la porte, sur le devant du socle.
            Quaternion flat = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            GameObject ring = Proto.Cylinder(t, foot + d * 1.21f + Vector3.up * 1.7f, new Vector3(1.7f, 0.02f, 1.7f), Color.white, "Cercle du blason");
            ring.transform.rotation = flat;
            ring.GetComponent<Renderer>().sharedMaterial = gold;
            GameObject crest = Proto.Cylinder(t, foot + d * 1.24f + Vector3.up * 1.7f, new Vector3(1.4f, 0.02f, 1.4f), colour, "Blason");
            crest.transform.rotation = flat;
            crest.GetComponent<Renderer>().sharedMaterial = Mat(colour, 0.3f);
        }

        // ------------------------------------------------------------------ lierre

        static readonly float[] IvySpots = { 6.5f, 21f, 33.5f, 62f, 75f, 90.5f };

        /// <summary>Le lierre monte par plaques sur la muraille, entre les contreforts et les bannieres.</summary>
        static void WallIvy(Transform t, Vector3 a, Vector3 dir, Vector3 outward, int seed)
        {
            System.Random rng = new System.Random(seed);
            Quaternion face = Quaternion.LookRotation(outward, Vector3.up);
            float half = Castle.WallThickness * 0.5f;
            for (int s = 0; s < IvySpots.Length; s++)
            {
                if (rng.NextDouble() < 0.3) continue;
                float u = IvySpots[s] + (float)(rng.NextDouble() - 0.5) * 2f;
                float climb = 5f + (float)rng.NextDouble() * 9f;
                float drift = 0f;
                int count = Mathf.RoundToInt(climb * 2.2f);
                for (int k = 0; k < count; k++)
                {
                    float y = 0.3f + climb * k / count;
                    drift += (float)(rng.NextDouble() - 0.5) * 0.9f;
                    drift = Mathf.Clamp(drift, -2.4f, 2.4f);
                    // Large au pied, plus maigre en montant.
                    float spread = Mathf.Lerp(1.5f, 0.35f, y / climb);
                    float x = drift + (float)(rng.NextDouble() - 0.5) * 2f * spread;
                    Vector3 p = a + dir * (u + x) + outward * (half + 0.14f) + Vector3.up * y;
                    Vector3 size = new Vector3(1.1f + (float)rng.NextDouble() * 1.1f, 0.8f + (float)rng.NextDouble() * 0.9f, 0.3f + (float)rng.NextDouble() * 0.16f);
                    Leaf(t, p, size, face * Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 180f), Leaves[rng.Next(Leaves.Length)]);
                }
            }
        }

        /// <summary>Une collerette de lierre au pied d'une tour d'angle, cote dehors.</summary>
        static void TowerIvy(Transform t, Vector3 tower, float r, int seed)
        {
            System.Random rng = new System.Random(seed);
            // Dehors seulement : la direction qui s'eloigne du centre du chateau.
            float outwardAngle = Mathf.Atan2(tower.z, tower.x);
            for (int k = 0; k < 26; k++)
            {
                float a = outwardAngle + (float)(rng.NextDouble() - 0.5) * 3.6f;
                float y = 0.4f + (float)rng.NextDouble() * (float)rng.NextDouble() * 9f;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float bulge = y < 2.2f ? 0.72f : 0.12f;          // le soubassement est plus large
                Vector3 p = tower + outward * (r + bulge) + Vector3.up * y;
                Vector3 size = new Vector3(1.2f + (float)rng.NextDouble() * 1.2f, 0.9f + (float)rng.NextDouble() * 0.9f, 0.34f);
                Leaf(t, p, size, Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 180f), Leaves[rng.Next(Leaves.Length)]);
            }
        }
    }
}
