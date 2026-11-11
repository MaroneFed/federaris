using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE PAYSAGE (v49 -- "tout de malade"). Ce qui fait qu'une ile a une histoire :
    ///
    ///   LES RUINES      au bord de l'ile, des colonnades effondrees, des arches, des pierres
    ///                   levees : un vieux temple d'avant le chateau ;
    ///   LES BUISSONS    et les rochers moussus, par bosquets, dans l'herbe ;
    ///   LES ILES-RUINES au loin dans le ciel, de gros rochers qui portent une tour brisee ou
    ///                   une arche : l'horizon n'est plus vide ;
    ///   LES OISEAUX     une volee qui tourne autour de la tour de la Couronne.
    ///
    /// Tout est SANS COLLIDER : on ne s'y cogne pas, les bots non plus. Rien ne change au jeu.
    /// </summary>
    public static class Scenery
    {
        static readonly Color Old = new Color(0.74f, 0.70f, 0.62f);
        static readonly Color OldDark = new Color(0.52f, 0.49f, 0.45f);
        static readonly Color Moss = new Color(0.36f, 0.50f, 0.24f);
        static readonly Color[] Bush = { new Color(0.22f, 0.46f, 0.20f), new Color(0.30f, 0.56f, 0.24f), new Color(0.40f, 0.64f, 0.28f) };
        static readonly float[] Rivers = { 52f, 142f, 232f, 322f };

        public static void Build(Transform worldRoot)
        {
            GameObject root = new GameObject("PAYSAGE");
            root.transform.SetParent(worldRoot, false);
            Transform t = root.transform;
            Proto.BeginVisualOnly();
            try
            {
                Ruins(t);
                Thickets(t);
                SkyRuins(t);
            }
            finally { Proto.EndVisualOnly(); }
            Birds.Spawn(worldRoot);
        }

        /// <summary>Rien de solide ici, et pas dans une riviere.</summary>
        static bool Free(Vector3 p, float radius)
        {
            float a = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg;
            for (int i = 0; i < Rivers.Length; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(a, Rivers[i])) < 7f) return false;
            return !Physics.CheckCapsule(p + Vector3.up * (radius + 0.6f), p + Vector3.up * (radius + 4f), radius, ~0, QueryTriggerInteraction.Ignore);
        }

        // ------------------------------------------------------------------ ruines

        static void Ruins(Transform t)
        {
            System.Random rng = new System.Random(9091);
            for (int i = 0; i < 14; i++)
            {
                float a = (i / 14f + (float)rng.NextDouble() * 0.03f) * Mathf.PI * 2f;
                float r = Ground.EdgeAt(a) - 6f - (float)rng.NextDouble() * 5f;
                Vector3 at = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (Castle.Covers(at.x, at.z, 12f) || !Free(at, 3.5f)) continue;
                // Le long du bord (la tangente du cercle).
                Vector3 along = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                int kind = rng.Next(3);
                if (kind == 0) Colonnade(t, at, along, rng);
                else if (kind == 1) Arch(t, at, along, rng);
                else StandingStones(t, at, rng);
            }
        }

        /// <summary>Une colonne ancienne, entiere ou brisee (son haut git alors a cote).</summary>
        static void OldColumn(Transform t, Vector3 at, float height, bool broken, System.Random rng)
        {
            Material stone = MaterialFactory.GetShiny(Old, 0.14f, 0f);
            Proto.Cylinder(t, at + Vector3.up * 0.18f, new Vector3(1.5f, 0.18f, 1.5f), OldDark, "Base de colonne");
            float h = broken ? height * (0.3f + (float)rng.NextDouble() * 0.35f) : height;
            if (broken)
            {
                Proto.Cylinder(t, at + Vector3.up * (0.36f + h * 0.5f), new Vector3(0.95f, h * 0.5f, 0.95f), Old, "Colonne brisée").GetComponent<Renderer>().sharedMaterial = stone;
                // Le tambour tombe, couche dans l'herbe.
                float turn = (float)rng.NextDouble() * 360f;
                Vector3 fall = Quaternion.Euler(0f, turn, 0f) * Vector3.forward * (1.6f + (float)rng.NextDouble() * 1.4f);
                GameObject drum = Proto.Cylinder(t, at + fall + Vector3.up * 0.42f, new Vector3(0.92f, 0.9f + (float)rng.NextDouble() * 0.8f, 0.92f), Old, "Tambour tombé");
                drum.transform.rotation = Quaternion.Euler(90f, turn + 40f, 0f);
                drum.GetComponent<Renderer>().sharedMaterial = stone;
            }
            else
            {
                Castle.Column(t, at + Vector3.up * 0.36f, 0.5f, h, Old, "Colonne ancienne").GetComponent<Renderer>().sharedMaterial = stone;
            }
            // La mousse au pied.
            for (int k = 0; k < 3; k++)
            {
                float m = (float)rng.NextDouble() * Mathf.PI * 2f;
                CastleGlory.Leaf(t, at + new Vector3(Mathf.Cos(m), 0f, Mathf.Sin(m)) * 0.6f + Vector3.up * 0.3f,
                                 new Vector3(1f, 0.5f, 0.9f), Quaternion.Euler(0f, m * Mathf.Rad2Deg, 0f), Moss);
            }
        }

        static void Colonnade(Transform t, Vector3 at, Vector3 along, System.Random rng)
        {
            int n = 4 + rng.Next(3);
            float height = 5.5f + (float)rng.NextDouble() * 2f;
            bool lastWhole = false;
            for (int k = 0; k < n; k++)
            {
                Vector3 p = at + along * ((k - (n - 1) * 0.5f) * 3.2f);
                if (!Ground.OnIsland(p.x, p.z) || !Free(p, 0.8f)) { lastWhole = false; continue; }
                bool broken = rng.NextDouble() < 0.45;
                OldColumn(t, p, height, broken, rng);
                // Deux colonnes entieres qui se suivent portent encore leur linteau.
                if (!broken && lastWhole)
                {
                    GameObject beam = Proto.Cube(t, p - along * 1.6f + Vector3.up * (height + 0.36f + 0.35f), new Vector3(1.3f, 0.7f, 4.6f), Old, "Architrave");
                    beam.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
                }
                lastWhole = !broken;
            }
        }

        /// <summary>Une arche de pierre, seule debout : deux piles et un cintre en claveaux.</summary>
        static void Arch(Transform t, Vector3 at, Vector3 along, System.Random rng)
        {
            Quaternion face = Quaternion.LookRotation(Vector3.Cross(Vector3.up, along), Vector3.up);
            Vector3 normal = face * Vector3.forward;
            float w = 2.6f + (float)rng.NextDouble() * 0.8f, spring = 4.2f;
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject pier = Proto.Cube(t, at + along * (k * (w + 0.6f)) + Vector3.up * (spring * 0.5f), new Vector3(1.3f, spring, 1.5f), Old, "Pile d'arche");
                pier.transform.rotation = face;
            }
            int stones = 11;
            // Une arche sur trois a perdu sa clef : il manque le haut.
            int missing = rng.NextDouble() < 0.35 ? stones / 2 : -1;
            for (int k = 0; k < stones; k++)
            {
                if (k == missing || k == missing + 1) continue;
                float th = Mathf.PI * (k + 0.5f) / stones;
                Vector3 p = at + along * (Mathf.Cos(th) * (w + 0.6f)) + Vector3.up * (spring + Mathf.Sin(th) * (w + 0.6f));
                GameObject s = Proto.Cube(t, p, new Vector3(1.25f, (w + 0.6f) * Mathf.PI / stones + 0.08f, 1.4f), k % 2 == 0 ? Old : OldDark, "Claveau ancien");
                s.transform.rotation = Quaternion.AngleAxis(-th * Mathf.Rad2Deg, normal) * face;
            }
            CastleGlory.Leaf(t, at + along * (w + 0.6f) + Vector3.up * (spring + 0.4f), new Vector3(2.2f, 1.1f, 1.9f), face, Moss);
            CastleGlory.Leaf(t, at - along * (w + 0.7f) + Vector3.up * 0.5f, new Vector3(2f, 1f, 1.8f), face, Bush[1]);
        }

        /// <summary>Un cercle de pierres levees, penchees par les siecles.</summary>
        static void StandingStones(Transform t, Vector3 at, System.Random rng)
        {
            int n = 5 + rng.Next(3);
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f;
                Vector3 p = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.6f;
                if (!Ground.OnIsland(p.x, p.z) || !Free(p, 0.7f)) continue;
                float h = 2.2f + (float)rng.NextDouble() * 2.4f;
                GameObject s = Proto.Cube(t, p + Vector3.up * (h * 0.5f - 0.2f), new Vector3(1.1f + (float)rng.NextDouble() * 0.5f, h, 0.7f), rng.NextDouble() < 0.5 ? Old : OldDark, "Pierre levée");
                s.transform.rotation = Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 14f, -a * Mathf.Rad2Deg + 90f, (float)(rng.NextDouble() - 0.5) * 12f);
            }
            GameObject slab = Proto.Cylinder(t, at + Vector3.up * 0.12f, new Vector3(2.6f, 0.12f, 2.6f), OldDark, "Dalle ancienne");
            slab.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(OldDark, 0.12f, 0f);
        }

        // ------------------------------------------------------------------ bosquets

        static void Thickets(Transform t)
        {
            System.Random rng = new System.Random(5150);
            Material rock = MaterialFactory.GetShiny(new Color(0.60f, 0.56f, 0.52f), 0.14f, 0f);
            for (int i = 0; i < 70; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float edge = Ground.EdgeAt(a);
                float r = Mathf.Lerp(Castle.FlatRadius * 0.9f, edge - 3f, (float)rng.NextDouble());
                Vector3 at = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (Castle.Covers(at.x, at.z, 9f) || !Ground.OnIsland(at.x, at.z) || !Free(at, 1.6f)) continue;
                if (rng.NextDouble() < 0.6)
                {
                    // Un buisson : quatre a sept touffes serrees.
                    int n = 4 + rng.Next(4);
                    for (int k = 0; k < n; k++)
                    {
                        Vector3 off = new Vector3((float)(rng.NextDouble() - 0.5) * 1.8f, 0f, (float)(rng.NextDouble() - 0.5) * 1.8f);
                        float s = 0.9f + (float)rng.NextDouble() * 1.1f;
                        CastleGlory.Leaf(t, at + off + Vector3.up * (s * 0.32f), new Vector3(s * 1.2f, s * 0.85f, s * 1.1f),
                                         Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Bush[rng.Next(Bush.Length)]);
                    }
                }
                else
                {
                    // Un rocher bas, coiffe de mousse (on l'enjambe sans le sentir : il est dans l'herbe).
                    float s = 0.8f + (float)rng.NextDouble() * 1.0f;
                    GameObject stone = CastleGlory.Leaf(t, at + Vector3.up * (s * 0.12f), new Vector3(s * 1.6f, s * 0.6f, s * 1.25f),
                                                        Quaternion.Euler((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 360f, 0f), Color.white);
                    stone.name = "Rocher";
                    stone.GetComponent<Renderer>().sharedMaterial = rock;
                    CastleGlory.Leaf(t, at + Vector3.up * (s * 0.38f), new Vector3(s * 1.1f, s * 0.22f, s * 0.9f), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Moss);
                }
            }
        }

        // ------------------------------------------------------------------ iles-ruines

        /// <summary>De gros rochers loin dans le ciel, qui portent les restes d'un autre royaume.</summary>
        static void SkyRuins(Transform t)
        {
            System.Random rng = new System.Random(7331);
            Material rock = MaterialFactory.GetShiny(new Color(0.58f, 0.47f, 0.40f), 0.12f, 0f);
            Material grass = MaterialFactory.GetShiny(new Color(0.40f, 0.64f, 0.29f), 0.15f, 0f);
            Material stone = MaterialFactory.GetShiny(Old, 0.14f, 0f);
            Material slate = MaterialFactory.GetShiny(new Color(0.22f, 0.29f, 0.46f), 0.4f, 0.05f);
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f + (float)rng.NextDouble() * 0.08f) * Mathf.PI * 2f;
                float r = 215f + (float)rng.NextDouble() * 55f;
                float y = 4f + (float)rng.NextDouble() * 60f;
                float size = 13f + (float)rng.NextDouble() * 10f;
                Vector3 at = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                Proto.Sphere(t, at - Vector3.up * size * 0.3f, new Vector3(size * 1.5f, size * 1.1f, size * 1.4f), Color.white, "Île lointaine").GetComponent<Renderer>().sharedMaterial = rock;
                GameObject tip = Proto.Cone(t, at - Vector3.up * size * 0.35f, size * 0.62f, size * 1.9f, Color.white, "Pointe", 7);
                tip.transform.rotation = Quaternion.Euler(180f, (float)rng.NextDouble() * 360f, 0f);
                tip.GetComponent<Renderer>().sharedMaterial = rock;
                Proto.Sphere(t, at + Vector3.up * size * 0.14f, new Vector3(size * 1.52f, size * 0.3f, size * 1.42f), Color.white, "Herbe").GetComponent<Renderer>().sharedMaterial = grass;
                float top = at.y + size * 0.26f;
                if (i % 2 == 0)
                {
                    // Une tour brisee : un fut, un etage plus etroit, la moitie d'un toit.
                    float h = size * 1.1f;
                    Proto.Cylinder(t, new Vector3(at.x, top + h * 0.5f, at.z), new Vector3(size * 0.42f, h * 0.5f, size * 0.42f), Old, "Tour en ruine").GetComponent<Renderer>().sharedMaterial = stone;
                    Proto.Cylinder(t, new Vector3(at.x, top + h, at.z), new Vector3(size * 0.5f, 0.5f, size * 0.5f), OldDark, "Couronnement");
                    if (rng.NextDouble() < 0.6)
                        Proto.Cone(t, new Vector3(at.x, top + h + 0.5f, at.z), size * 0.3f, size * 0.7f, Color.white, "Toit", 10).GetComponent<Renderer>().sharedMaterial = slate;
                    Proto.Cylinder(t, new Vector3(at.x + size * 0.45f, top + h * 0.2f, at.z + size * 0.1f), new Vector3(size * 0.2f, h * 0.2f, size * 0.2f), Old, "Tourelle").GetComponent<Renderer>().sharedMaterial = stone;
                }
                else
                {
                    // Une grande arche, face a l'ile.
                    Vector3 along = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                    Quaternion face = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up);
                    float w = size * 0.3f, spring = size * 0.55f;
                    for (int k = -1; k <= 1; k += 2)
                    {
                        GameObject pier = Proto.Cube(t, new Vector3(at.x, top + spring * 0.5f, at.z) + along * (k * w), new Vector3(size * 0.14f, spring, size * 0.16f), Old, "Pile");
                        pier.transform.rotation = face;
                    }
                    for (int k = 0; k < 9; k++)
                    {
                        float th = Mathf.PI * (k + 0.5f) / 9f;
                        Vector3 p = new Vector3(at.x, top + spring, at.z) + along * (Mathf.Cos(th) * w) + Vector3.up * (Mathf.Sin(th) * w);
                        GameObject s = Proto.Cube(t, p, new Vector3(size * 0.13f, w * Mathf.PI / 9f + 0.2f, size * 0.15f), k % 2 == 0 ? Old : OldDark, "Claveau");
                        s.transform.rotation = Quaternion.AngleAxis(-th * Mathf.Rad2Deg, face * Vector3.forward) * face;
                    }
                }
            }
        }
    }

    /// <summary>
    /// UNE VOLEE D'OISEAUX autour de la tour. Chaque oiseau : un petit corps et deux ailes qui
    /// battent, sur un grand cercle qui monte et descend. Un seul script pour toute la volee.
    /// </summary>
    public class Birds : MonoBehaviour
    {
        const int Count = 16;
        readonly Transform[] body = new Transform[Count];
        readonly Transform[] left = new Transform[Count];
        readonly Transform[] right = new Transform[Count];
        readonly float[] radius = new float[Count];
        readonly float[] height = new float[Count];
        readonly float[] phase = new float[Count];
        readonly float[] speed = new float[Count];

        public static void Spawn(Transform worldRoot)
        {
            GameObject go = new GameObject("OISEAUX");
            go.transform.SetParent(worldRoot, false);
            Birds b = go.AddComponent<Birds>();
            Material dark = MaterialFactory.Get(new Color(0.16f, 0.15f, 0.2f));
            Material pale = MaterialFactory.Get(new Color(0.92f, 0.9f, 0.86f));
            System.Random rng = new System.Random(2024);
            Proto.BeginVisualOnly();
            for (int i = 0; i < Count; i++)
            {
                // Deux volees : des oiseaux sombres en haut de la tour, des blancs plus bas et plus loin.
                bool high = i < Count / 2;
                Material m = high ? dark : pale;
                GameObject bird = new GameObject("Oiseau");
                bird.transform.SetParent(go.transform, false);
                Proto.Sphere(bird.transform, Vector3.zero, new Vector3(0.22f, 0.2f, 0.7f), Color.white, "Corps").GetComponent<Renderer>().sharedMaterial = m;
                b.body[i] = bird.transform;
                b.left[i] = Wing(bird.transform, -1f, m);
                b.right[i] = Wing(bird.transform, 1f, m);
                b.radius[i] = high ? 30f + (float)rng.NextDouble() * 22f : 70f + (float)rng.NextDouble() * 45f;
                b.height[i] = high ? 84f + (float)rng.NextDouble() * 30f : 26f + (float)rng.NextDouble() * 30f;
                b.phase[i] = (high ? 0f : 2.2f) + (float)rng.NextDouble() * 1.1f;
                b.speed[i] = (high ? 0.2f : 0.12f) * (0.9f + (float)rng.NextDouble() * 0.2f);
            }
            Proto.EndVisualOnly();
        }

        static Transform Wing(Transform bird, float side, Material m)
        {
            GameObject hinge = new GameObject("Aile");
            hinge.transform.SetParent(bird, false);
            Proto.Cube(hinge.transform, new Vector3(side * 0.55f, 0f, 0f), new Vector3(1.1f, 0.04f, 0.4f), Color.white, "Plume").GetComponent<Renderer>().sharedMaterial = m;
            return hinge.transform;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < Count; i++)
            {
                if (body[i] == null) continue;
                float a = phase[i] + t * speed[i];
                float r = radius[i] + Mathf.Sin(t * 0.21f + i) * 5f;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, height[i] + Mathf.Sin(t * 0.33f + i * 1.7f) * 4f, Mathf.Sin(a) * r);
                body[i].localPosition = p;
                // Il regarde ou il va (la tangente du cercle), penche vers l'interieur du virage.
                body[i].localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), Vector3.up) * Quaternion.Euler(0f, 0f, 18f);
                // Il bat des ailes par salves, puis plane.
                float beat = Mathf.Sin(t * 0.7f + i * 2.3f) > -0.2f ? Mathf.Sin(t * 9f + i) * 38f : 8f;
                left[i].localRotation = Quaternion.Euler(0f, 0f, -beat);
                right[i].localRotation = Quaternion.Euler(0f, 0f, beat);
            }
        }
    }
}
