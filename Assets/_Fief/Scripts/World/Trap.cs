using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN PIEGE A MACHOIRES (Martin, 25/09 : "quand un gars va dessus, il meurt et
    /// perd tout son stuff").
    ///
    /// On le fabrique (Tab, Artisanat : 3 bois mort, 2 fer), on le tient en main,
    /// clic : il est pose devant soi. Deux machoires de fer ouvertes a plat, a
    /// moitie sous les feuilles. Qui marche dessus -- rival, joueur, bete -- meurt
    /// sur le coup et lache tout dans sa depouille. Son proprietaire ne craint rien.
    ///
    /// ON NE LE VOIT PAS DE LOIN : a plus de 3,5 m il disparait sous les feuilles.
    /// Les tiens, tu les vois toujours (tu sais ou tu les as mis). C'est ce qui en
    /// fait la defense naturelle d'une stele : autour, on avance a pas comptes.
    ///
    /// Un piege ne sert qu'une fois. Trois poses au plus (plus avec les Collets).
    /// </summary>
    public class Trap : MonoBehaviour
    {
        public static readonly List<Trap> All = new List<Trap>();

        const float Trigger = 0.85f;
        const float SeenFrom = 3.5f;

        [System.NonSerialized] public Seeker owner;
        Transform jawA, jawB;
        Renderer[] parts;
        Renderer glint;
        bool shown = true;
        bool sprung;
        float closing;

        public static readonly Color Iron = new Color(0.2f, 0.19f, 0.18f);

        public static int MaxFor(Seeker s)
        {
            return s == null ? 3 : 3 + UpgradeInfo.ColletsPerLevel * s.Hoard.Level(UpgradeKind.Collets);
        }

        public static int CountOf(Seeker s)
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && !All[i].sprung && All[i].owner == s) n++;
            return n;
        }

        /// <summary>Null si l'endroit convient, sinon pourquoi.</summary>
        public static string WhyNot(Seeker owner, Vector3 at)
        {
            if (CountOf(owner) >= MaxFor(owner)) return "Tu as déjà " + MaxFor(owner) + " pièges posés.";
            if (Castle.Covers(at.x, at.z, 2f)) return "Pas dans le château : les gardes les verraient.";
            if (Ground.Slope(at.x, at.z) > 0.5f) return "Le sol est trop en pente.";
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && Flat(All[i].transform.position - at).magnitude < 1.5f) return "Trop près d'un autre piège.";
            return null;
        }

        public static Trap Place(Seeker owner, Vector3 at, float yaw)
        {
            GameObject go = new GameObject("PIÈGE de " + owner.Name);
            go.transform.position = Ground.Place(at.x, at.z, 0.02f);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Trap trap = go.AddComponent<Trap>();
            trap.owner = owner;
            trap.Assemble();
            All.Add(trap);
            return trap;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        void Assemble()
        {
            Transform t = transform;
            Proto.BeginVisualOnly();
            // La plaque et le ressort.
            Proto.Cylinder(t, new Vector3(0f, 0.015f, 0f), new Vector3(0.22f, 0.012f, 0.22f), Iron, "Plaque");
            Proto.Cube(t, new Vector3(0f, 0.03f, 0f), new Vector3(0.6f, 0.03f, 0.05f), Palette.Shade(Iron, 1.3f), "Ressort");
            // Deux machoires, ouvertes a plat, dents vers le ciel.
            jawA = Jaw(t, 1f);
            jawB = Jaw(t, -1f);
            // La chaine et le piquet.
            for (int i = 0; i < 4; i++)
                Proto.Cube(t, new Vector3(0.36f + i * 0.08f, 0.02f, -0.05f), new Vector3(0.07f, 0.025f, 0.04f), Iron, "Maillon");
            Proto.Cube(t, new Vector3(0.7f, 0.08f, -0.05f), new Vector3(0.04f, 0.18f, 0.04f), new Color(0.3f, 0.23f, 0.15f), "Piquet");
            // Des feuilles jetees dessus : c'est ce qui le cache.
            System.Random rng = new System.Random(Mathf.RoundToInt(t.position.x * 13f + t.position.z * 7f));
            Color[] leaves = { new Color(0.30f, 0.21f, 0.13f), new Color(0.42f, 0.31f, 0.14f), new Color(0.24f, 0.19f, 0.14f) };
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 0.1f + (float)rng.NextDouble() * 0.38f;
                GameObject leaf = Proto.Cube(t, new Vector3(Mathf.Cos(a) * r, 0.05f + (float)rng.NextDouble() * 0.02f, Mathf.Sin(a) * r),
                                             new Vector3(0.14f, 0.008f, 0.09f), leaves[i % leaves.Length], "Feuille");
                leaf.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 20f - 10f, (float)rng.NextDouble() * 360f, 0f);
            }
            GameObject spark = Proto.Cube(t, new Vector3(0.18f, 0.06f, 0.05f), new Vector3(0.03f, 0.03f, 0.03f), Color.white, "Reflet");
            spark.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            Proto.EndVisualOnly();
            glint = spark.GetComponent<Renderer>();
            glint.sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.95f, 0.85f), 3f);
            parts = GetComponentsInChildren<Renderer>(true);
            // Le reflet ne fait pas partie de ce qu'on montre de pres : il est a part.
            List<Renderer> rest = new List<Renderer>(parts);
            rest.Remove(glint);
            parts = rest.ToArray();
            glint.enabled = false;
        }

        /// <summary>Une machoire : un demi-cercle de fer herisse de dents, pivotant sur l'axe du ressort.</summary>
        static Transform Jaw(Transform parent, float side)
        {
            GameObject pivot = new GameObject("Mâchoire");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            Transform p = pivot.transform;
            const int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float u = Mathf.PI * i / n;
                Vector3 at = new Vector3(Mathf.Cos(u) * 0.28f, 0f, Mathf.Sin(u) * 0.28f * side);
                GameObject seg = Proto.Cube(p, at, new Vector3(0.09f, 0.03f, 0.03f), Iron, "Arc");
                seg.transform.localRotation = Quaternion.Euler(0f, -u * Mathf.Rad2Deg * side + 90f, 0f);
                if (i > 0 && i < n)
                {
                    GameObject tooth = Proto.Cone(p, at + new Vector3(0f, 0.015f, 0f), 0.022f, 0.07f, new Color(0.5f, 0.48f, 0.45f), "Dent", 4);
                    tooth.transform.localRotation = Quaternion.Euler(-12f * side, 0f, 0f);
                }
            }
            return p;
        }

        void Update()
        {
            if (sprung)
            {
                // Les machoires se referment d'un coup, puis le piege reste la, ferme.
                closing = Mathf.Min(1f, closing + Time.deltaTime * 14f);
                float angle = 82f * closing;
                if (jawA != null) jawA.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                if (jawB != null) jawB.localRotation = Quaternion.Euler(angle, 0f, 0f);
                return;
            }

            // Visible de pres seulement -- sauf pour son proprietaire.
            Transform player = Game.PlayerTransform;
            float away = player != null ? Flat(player.position - transform.position).magnitude : 99f;
            bool see = owner == Game.Me || away < SeenFrom;
            if (see != shown)
            {
                shown = see;
                for (int i = 0; i < parts.Length; i++) if (parts[i] != null) parts[i].enabled = see;
            }
            // Le REFLET : entre 3,5 et 7 m, le fer accroche de temps en temps la
            // lumiere de ta lanterne. Un point qui scintille dans les feuilles --
            // celui qui regarde ou il marche a une chance.
            if (glint != null)
            {
                bool twinkle = !see && away < 7f && Mathf.Sin(Time.time * 2.3f + transform.position.x) > 0.8f;
                if (glint.enabled != twinkle) glint.enabled = twinkle;
            }

            // Quelqu'un marche dessus ?
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == null || s == owner || !s.Alive || s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                if (Mathf.Abs(d.y) > 1.2f) continue;
                d.y = 0f;
                if (d.magnitude > Trigger) continue;
                Spring(s);
                return;
            }
            for (int i = 0; i < Beast.All.Count; i++)
            {
                Beast b = Beast.All[i];
                if (b == null || !b.Alive) continue;
                if (Flat(b.transform.position - transform.position).magnitude > Trigger) continue;
                sprung = true;
                Snap();
                b.Die(owner);
                Destroy(gameObject, 25f);
                return;
            }
        }

        void Spring(Seeker victim)
        {
            sprung = true;
            Snap();
            if (victim.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.6f);
            if (owner == Game.Me) Stats.TrapKills++;
            Combat.Kill(victim, owner, "dans un piège de " + (owner != null ? owner.Name : "quelqu'un"));
            if (owner == Game.Me && !victim.IsPlayer)
            {
                string where = Game.PlayerTransform != null ? Hud.Direction(Game.PlayerTransform.position, transform.position) : "";
                Toasts.Show(victim.Name + " est tombé dans ton piège, " + where + ". Sa dépouille t'attend.", new Color(0.95f, 0.55f, 0.3f));
            }
            Destroy(gameObject, 25f);
        }

        void Snap()
        {
            for (int i = 0; i < parts.Length; i++) if (parts[i] != null) parts[i].enabled = true;
            shown = true;
            // De pres, le claquement en plein ; de loin (jusqu'a 120 m), un claquement
            // lointain, spatialise : on sait qu'un piege s'est referme, et de quel cote.
            Transform player = Game.PlayerTransform;
            float far = player != null ? Flat(player.position - transform.position).magnitude : 999f;
            if (far < 12f) Sfx.TrapSnap();
            else if (far < 120f && !Sfx.Muted) AudioSource.PlayClipAtPoint(Sfx.TrapSnapClip(), transform.position + Vector3.up, 1f);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
