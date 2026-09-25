using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QUI VIT DANS LA SYLVE, EN PLUS DES ARBRES.
    ///
    ///   - LES CORBEAUX : des petites bandes posees au sol ou sur les souches. Ils
    ///     t'ignorent... jusqu'a neuf metres. Alors ils s'envolent tous d'un coup,
    ///     en croassant. C'est beau, et c'est un SIGNAL : un envol de corbeaux au
    ///     loin, c'est que quelqu'un passe. En multijoueur, ils trahiront les autres.
    ///     Ils s'envolent aussi devant les rivaux.
    ///   - LES RUINES : des colonnes brisees, un arc, des dalles -- les restes d'un
    ///     ancien sanctuaire, une douzaine dans la foret. Des reperes, et des coins
    ///     pour poser sa stele.
    ///   - LES FLEURS-LUNE : de petites fleurs pales qui luisent autour des creux.
    ///     Encore un indice pour qui sait regarder.
    /// </summary>
    public static class Nature
    {
        public static void Build(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("NATURE");
            root.transform.SetParent(parent, false);
            System.Random rng = new System.Random((cfg != null ? cfg.worldSeed : 1) * 71 + 5);
            float half = (cfg != null ? cfg.mapSize : 420f) * 0.5f - 50f;

            // --- les ruines
            int ruins = 0;
            for (int tries = 0; tries < 600 && ruins < 8; tries++)
            {
                float x = R(rng, -half, half), z = R(rng, -half, half);
                if (Castle.Covers(x, z, 20f) || Landmarks.Near(x, z, 15f) || Gathering.NearHollow(x, z, 10f) || Monument.Near(x, z, 8f)) continue;
                if (Ground.Slope(x, z) > 0.25f || Blocked(Ground.Place(x, z, 0f), 4f)) continue;
                Ruin(root.transform, Ground.Place(x, z, 0f), rng);
                ruins++;
            }

            // --- les fleurs-lune, autour des creux
            Material petal = MaterialFactory.GetGlow(new Color(0.7f, 0.85f, 1f), 1.1f);
            Proto.BeginVisualOnly();
            for (int i = 0; i < Gathering.HollowSpotCount; i++)
            {
                Vector2 h = Gathering.HollowSpot(i);
                int n = 6 + rng.Next(6);
                for (int k = 0; k < n; k++)
                {
                    float a = R(rng, 0f, 6.28f), d = R(rng, 4f, 9f);
                    Vector3 p = Ground.Place(h.x + Mathf.Cos(a) * d, h.y + Mathf.Sin(a) * d, 0f);
                    Proto.Cube(root.transform, p + new Vector3(0f, 0.12f, 0f), new Vector3(0.03f, 0.24f, 0.03f), new Color(0.2f, 0.3f, 0.2f), "Tige");
                    GameObject flower = Proto.Cube(root.transform, p + new Vector3(0f, 0.26f, 0f), new Vector3(0.1f, 0.04f, 0.1f), Color.white, "Fleur-lune");
                    flower.transform.localRotation = Quaternion.Euler(0f, R(rng, 0f, 90f), 0f);
                    flower.GetComponent<Renderer>().sharedMaterial = petal;
                }
            }
            Proto.EndVisualOnly();

            // --- les corbeaux
            int flocks = 0;
            for (int tries = 0; tries < 400 && flocks < 14; tries++)
            {
                float x = R(rng, -half, half), z = R(rng, -half, half);
                if (Castle.Covers(x, z, 5f) || Landmarks.Near(x, z, 2f) || Monument.Near(x, z, 2f)) continue;
                CrowFlock.Build(root.transform, Ground.Place(x, z, 0f), 3 + rng.Next(4), rng.Next());
                flocks++;
            }
            // Et quelques-uns sur les creneaux du chateau.
            CrowFlock.Build(root.transform, new Vector3(-30f, Castle.WallHeight + 0.05f, Castle.HalfSize), 4, rng.Next(), true);
            CrowFlock.Build(root.transform, new Vector3(Castle.HalfSize, Castle.WallHeight + 0.05f, -24f), 3, rng.Next(), true);
        }

        /// <summary>
        /// Une ruine : un socle de dalles, deux ou trois colonnes (dont une brisee et
        /// une couchee), parfois un arc. Les colonnes ont un collider.
        /// </summary>
        static void Ruin(Transform parent, Vector3 at, System.Random rng)
        {
            GameObject go = new GameObject("Ruine");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, R(rng, 0f, 360f), 0f);
            Transform t = go.transform;
            Color stone = new Color(0.34f, 0.34f, 0.32f);
            Color moss = new Color(0.22f, 0.28f, 0.18f);

            Proto.BeginVisualOnly();
            for (int i = 0; i < 9; i++)
            {
                if (rng.NextDouble() < 0.3) continue;
                float x = (i % 3 - 1) * 1.6f, z = (i / 3 - 1) * 1.6f;
                GameObject slab = Proto.Cube(t, new Vector3(x, 0.02f, z), new Vector3(1.5f, 0.12f, 1.5f), rng.NextDouble() < 0.3 ? moss : stone, "Dalle");
                slab.transform.localRotation = Quaternion.Euler(R(rng, -3f, 3f), R(rng, -6f, 6f), R(rng, -3f, 3f));
            }
            Proto.EndVisualOnly();

            bool arch = rng.NextDouble() < 0.4;
            float[] heights = { 3.4f, R(rng, 1.2f, 2.4f), 3.4f };
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3((i - 1) * 2.4f, 0f, 1.6f);
                if (i == 1 && !arch)
                {
                    // Couchee dans l'herbe.
                    GameObject fallen = Proto.Cube(t, p + new Vector3(0.4f, 0.3f, -1.4f), new Vector3(0.6f, 0.6f, 3.2f), stone, "Colonne tombée");
                    fallen.transform.localRotation = Quaternion.Euler(0f, 25f, 4f);
                    continue;
                }
                float h = arch ? 3.4f : heights[i];
                Proto.Cube(t, p + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.6f, h, 0.6f), i == 2 ? moss : stone, "Colonne");
                Proto.BeginVisualOnly();
                Proto.Cube(t, p + new Vector3(0f, 0.12f, 0f), new Vector3(0.9f, 0.24f, 0.9f), stone, "Base");
                if (h > 3f) Proto.Cube(t, p + new Vector3(0f, h + 0.1f, 0f), new Vector3(0.85f, 0.2f, 0.85f), stone, "Chapiteau");
                Proto.EndVisualOnly();
            }
            if (arch)
            {
                Proto.BeginVisualOnly();
                Proto.Cube(t, new Vector3(0f, 3.75f, 1.6f), new Vector3(5.6f, 0.5f, 0.7f), stone, "Architrave");
                Proto.EndVisualOnly();
            }
        }

        static bool Blocked(Vector3 at, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(at + Vector3.up * 1.5f, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++) if (!(hits[i] is MeshCollider)) return true;
            return false;
        }

        static float R(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }

    /// <summary>
    /// Une bande de corbeaux. Au repos : ils picorent, tournent la tete. Quelqu'un
    /// approche a moins de 9 m (toi ou un rival) : envol general, croassements,
    /// battements d'ailes. Ils reviennent se poser ailleurs, une minute plus tard.
    /// </summary>
    public class CrowFlock : MonoBehaviour
    {
        class Crow
        {
            public Transform body;
            public Transform wingL;
            public Transform wingR;
            public Vector3 rest;
            public Vector3 flight;
            public float phase;
        }

        readonly List<Crow> crows = new List<Crow>();
        bool flying;
        float timer;
        float checkTimer;
        bool perched;
        System.Random rng;

        static readonly Color Black = new Color(0.05f, 0.05f, 0.06f);
        static readonly Color Beak = new Color(0.12f, 0.11f, 0.1f);

        public static CrowFlock Build(Transform parent, Vector3 at, int count, int seed, bool perched = false)
        {
            GameObject go = new GameObject("Corbeaux");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            CrowFlock f = go.AddComponent<CrowFlock>();
            f.rng = new System.Random(seed);
            f.perched = perched;

            Proto.BeginVisualOnly();
            for (int i = 0; i < count; i++)
            {
                Crow c = new Crow();
                GameObject b = new GameObject("Corbeau");
                b.transform.SetParent(go.transform, false);
                float a = (float)f.rng.NextDouble() * 6.28f, d = perched ? i * 1.3f : 0.6f + (float)f.rng.NextDouble() * 2.2f;
                c.rest = perched ? new Vector3(d, 0f, 0f) : new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                if (!perched) c.rest.y = Ground.Sample(at.x + c.rest.x, at.z + c.rest.z) - at.y;
                b.transform.localPosition = c.rest;
                b.transform.localRotation = Quaternion.Euler(0f, (float)f.rng.NextDouble() * 360f, 0f);
                Proto.Cube(b.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.14f, 0.13f, 0.3f), Black, "Corps");
                Proto.Cube(b.transform, new Vector3(0f, 0.26f, 0.15f), new Vector3(0.1f, 0.1f, 0.11f), Black, "Tête");
                Proto.Cube(b.transform, new Vector3(0f, 0.25f, 0.24f), new Vector3(0.03f, 0.03f, 0.09f), Beak, "Bec");
                Proto.Cube(b.transform, new Vector3(0f, 0.15f, -0.2f), new Vector3(0.1f, 0.03f, 0.14f), Black, "Queue");
                GameObject wl = new GameObject("Aile");
                wl.transform.SetParent(b.transform, false);
                wl.transform.localPosition = new Vector3(-0.07f, 0.2f, 0f);
                Proto.Cube(wl.transform, new Vector3(-0.14f, 0f, 0f), new Vector3(0.28f, 0.02f, 0.2f), Black, "Plumes");
                GameObject wr = new GameObject("Aile");
                wr.transform.SetParent(b.transform, false);
                wr.transform.localPosition = new Vector3(0.07f, 0.2f, 0f);
                Proto.Cube(wr.transform, new Vector3(0.14f, 0f, 0f), new Vector3(0.28f, 0.02f, 0.2f), Black, "Plumes");
                c.body = b.transform;
                c.wingL = wl.transform;
                c.wingR = wr.transform;
                c.phase = (float)f.rng.NextDouble() * 10f;
                f.crows.Add(c);
            }
            Proto.EndVisualOnly();
            return f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float t = Time.time;

            if (!flying)
            {
                // Au sol : ils picorent (la tete plonge), et tournent sur eux-memes.
                for (int i = 0; i < crows.Count; i++)
                {
                    Crow c = crows[i];
                    float peck = Mathf.Max(0f, Mathf.Sin(t * 2.3f + c.phase * 3f)) * 25f;
                    c.body.localRotation = Quaternion.Euler(peck, c.body.localEulerAngles.y + Mathf.Sin(t * 0.4f + c.phase) * 20f * dt, 0f);
                    c.wingL.localRotation = Quaternion.identity;
                    c.wingR.localRotation = Quaternion.identity;
                }
                checkTimer -= dt;
                if (checkTimer <= 0f)
                {
                    checkTimer = 0.25f;
                    if (SomeoneNear(9f)) TakeOff();
                }
                return;
            }

            // En vol : chacun file dans sa direction, en montant, ailes battantes.
            timer -= dt;
            for (int i = 0; i < crows.Count; i++)
            {
                Crow c = crows[i];
                c.body.localPosition += c.flight * dt;
                c.flight += Vector3.up * dt * 1.5f;
                c.body.localRotation = Quaternion.LookRotation(c.flight.normalized, Vector3.up);
                float flap = Mathf.Sin(t * 22f + c.phase) * 55f;
                c.wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                c.wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
            if (timer <= 0f) Land();
        }

        bool SomeoneNear(float metres)
        {
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Transform b = Game.Seekers[i].Body;
                if (b == null) continue;
                Vector3 d = b.position - transform.position;
                d.y = 0f;
                if (d.magnitude < metres) return true;
            }
            return false;
        }

        void TakeOff()
        {
            flying = true;
            timer = 6f;
            for (int i = 0; i < crows.Count; i++)
            {
                Crow c = crows[i];
                float a = (float)rng.NextDouble() * 6.28f;
                c.flight = new Vector3(Mathf.Cos(a) * 5f, 3.5f + (float)rng.NextDouble() * 2f, Mathf.Sin(a) * 5f);
            }
            if (!Sfx.Muted)
            {
                AudioSource.PlayClipAtPoint(Sfx.Wings(), transform.position + Vector3.up, 0.9f);
                AudioSource.PlayClipAtPoint(Sfx.Caw(), transform.position + Vector3.up * 2f, 0.8f);
            }
        }

        /// <summary>Ils reviennent se poser, un peu plus loin (ou au meme endroit s'ils sont sur un mur).</summary>
        void Land()
        {
            flying = false;
            checkTimer = 20f;          // un moment de calme avant de pouvoir s'effrayer a nouveau
            if (!perched)
            {
                float a = (float)rng.NextDouble() * 6.28f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 35f;
                if (!Castle.Covers(p.x, p.z, 5f)) transform.position = Ground.Place(p.x, p.z, 0f);
            }
            for (int i = 0; i < crows.Count; i++)
            {
                Crow c = crows[i];
                Vector3 rest = c.rest;
                if (!perched) rest.y = Ground.Sample(transform.position.x + rest.x, transform.position.z + rest.z) - transform.position.y;
                c.body.localPosition = rest;
            }
        }
    }
}
