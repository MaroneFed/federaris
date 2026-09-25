using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE BARRICADE : cinq pieux appointes et deux traverses, trois metres de large.
    /// On ne la traverse pas ; on la contourne, ou on la CASSE (trois coups de hache
    /// ou d'epee). Autour de sa stele, deux barricades et un piege dans le seul
    /// passage : voila une vraie defense.
    /// </summary>
    public class Barricade : MonoBehaviour
    {
        public static readonly List<Barricade> All = new List<Barricade>();
        public const int Max = 6;
        const float Width = 3f;
        const float Height = 1.8f;

        [System.NonSerialized] public Seeker owner;
        int hits = 3;

        static readonly Color Stake = new Color(0.4f, 0.3f, 0.2f);
        static readonly Color StakeDark = new Color(0.28f, 0.2f, 0.13f);

        public static int CountOf(Seeker s)
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == s) n++;
            return n;
        }

        /// <summary>Null si l'endroit convient, sinon pourquoi.</summary>
        public static string WhyNot(Seeker owner, Vector3 at, float yaw)
        {
            if (CountOf(owner) >= Max) return Max + " barricades déjà";
            if (Castle.Covers(at.x, at.z, 2f)) return "Pas au château";
            if (Ground.Slope(at.x, at.z) > 0.6f) return "Trop en pente";
            // Rien dans le volume : pas de tronc, de rocher, d'autre barricade, de chercheur.
            Vector3 centre = at + Vector3.up * (Height * 0.5f + 0.25f);
            Vector3 half = new Vector3(Width * 0.5f, Height * 0.5f - 0.2f, 0.3f);
            if (Physics.CheckBox(centre, half, Quaternion.Euler(0f, yaw, 0f), ~0, QueryTriggerInteraction.Ignore)) return "Quelque chose gêne";
            return null;
        }

        /// <summary>Les pieux et les traverses (le Builder s'en sert aussi pour son fantome).</summary>
        public static void Shape(Transform t)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -Width * 0.5f + 0.3f + i * (Width - 0.6f) / 4f;
                float h = Height - (i % 2) * 0.2f;
                GameObject stake = Proto.Cylinder(t, new Vector3(x, h * 0.5f, 0f), new Vector3(0.22f, h * 0.5f, 0.22f), i % 2 == 0 ? Stake : StakeDark, "Pieu");
                stake.transform.localRotation = Quaternion.Euler((i - 2) * 1.5f, 0f, (i % 2 == 0 ? 3f : -2f));
                Proto.Cone(t, new Vector3(x, h, 0f), 0.11f, 0.35f, Stake, "Pointe", 5);
            }
            Proto.Cube(t, new Vector3(0f, 0.55f, -0.14f), new Vector3(Width, 0.12f, 0.1f), StakeDark, "Traverse");
            Proto.Cube(t, new Vector3(0f, 1.25f, -0.14f), new Vector3(Width, 0.12f, 0.1f), StakeDark, "Traverse");
        }

        public static Barricade Place(Seeker owner, Vector3 at, float yaw)
        {
            GameObject go = new GameObject("BARRICADE de " + owner.Name);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Proto.BeginVisualOnly();
            Shape(go.transform);
            Proto.EndVisualOnly();
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, Height * 0.5f, 0f);
            box.size = new Vector3(Width, Height, 0.4f);
            Barricade b = go.AddComponent<Barricade>();
            b.owner = owner;
            All.Add(b);
            return b;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>Un coup de hache ou d'epee. Vrai si elle cede.</summary>
        public bool Hit()
        {
            hits--;
            Sfx.HarvestTap(ResourceType.Deadwood);
            Punch.Apply(transform, transform.position - transform.forward);
            if (hits > 0) return false;
            Sfx.Thud();
            Ambiance.Burst(null, transform.position + Vector3.up, new Color(0.4f, 0.3f, 0.2f));
            Destroy(gameObject);
            return true;
        }

        /// <summary>La barricade qui bloque ce chercheur (a moins de "reach"), s'il y en a une qui n'est pas la sienne.</summary>
        public static Barricade Blocking(Seeker s, Vector3 at, float reach)
        {
            for (int i = 0; i < All.Count; i++)
            {
                Barricade b = All[i];
                if (b == null || b.owner == s) continue;
                Vector3 local = b.transform.InverseTransformPoint(at);
                if (Mathf.Abs(local.x) < Width * 0.5f + reach && Mathf.Abs(local.z) < reach + 0.2f) return b;
            }
            return null;
        }
    }

    /// <summary>
    /// UNE ALARME : deux piquets, un fil tendu a hauteur de cheville, des clochettes.
    /// Qui passe dessus -- rival, joueur, bete -- la fait sonner. Si c'est la tienne,
    /// tu l'entends ou que tu sois, et la boussole te montre ou. Elle se rearme toute
    /// seule. Poses-en sur les chemins de ta stele.
    /// </summary>
    public class Alarm : MonoBehaviour
    {
        public static readonly List<Alarm> All = new List<Alarm>();
        public const int Max = 4;
        const float Reach = 1.4f;

        [System.NonSerialized] public Seeker owner;
        float rearm;
        /// <summary>Heure (Time.time) du dernier tintement : la boussole la montre un moment.</summary>
        public float RangAt = -99f;

        public static int CountOf(Seeker s)
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == s) n++;
            return n;
        }

        public static string WhyNot(Seeker owner, Vector3 at)
        {
            if (CountOf(owner) >= Max) return Max + " alarmes déjà";
            if (Castle.Covers(at.x, at.z, 2f)) return "Pas au château";
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null && (All[i].transform.position - at).magnitude < 3f) return "Trop près d'une autre";
            return null;
        }

        public static void Shape(Transform t)
        {
            Color post = new Color(0.34f, 0.26f, 0.17f);
            Color brass = new Color(0.8f, 0.64f, 0.3f);
            for (int side = -1; side <= 1; side += 2)
            {
                Proto.Cube(t, new Vector3(side * 1.1f, 0.3f, 0f), new Vector3(0.08f, 0.6f, 0.08f), post, "Piquet");
                Proto.Sphere(t, new Vector3(side * 0.55f, 0.24f, 0f), new Vector3(0.1f, 0.12f, 0.1f), brass, "Clochette");
            }
            Proto.Cube(t, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.015f, 0.015f), new Color(0.75f, 0.72f, 0.64f), "Fil");
            Proto.Sphere(t, new Vector3(0f, 0.24f, 0f), new Vector3(0.1f, 0.12f, 0.1f), brass, "Clochette");
        }

        public static Alarm Place(Seeker owner, Vector3 at, float yaw)
        {
            GameObject go = new GameObject("ALARME de " + owner.Name);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Proto.BeginVisualOnly();
            Shape(go.transform);
            Proto.EndVisualOnly();
            Alarm a = go.AddComponent<Alarm>();
            a.owner = owner;
            All.Add(a);
            return a;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        void Update()
        {
            if (rearm > 0f) { rearm -= Time.deltaTime; return; }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == null || s == owner || !s.Alive || s.Body == null) continue;
                Vector3 local = transform.InverseTransformPoint(s.Body.position);
                if (Mathf.Abs(local.x) > 1.2f || Mathf.Abs(local.z) > Reach || Mathf.Abs(local.y) > 2f) continue;
                Ring(s);
                return;
            }
        }

        void Ring(Seeker intruder)
        {
            rearm = 12f;
            RangAt = Time.time;
            if (!Sfx.Muted) AudioSource.PlayClipAtPoint(Sfx.AlarmClip(), transform.position + Vector3.up, 1f);
            if (owner == Game.Me && Game.Me.Body != null)
            {
                Sfx.Alarm();
                Toasts.Show("Alarme ! " + intruder.Name, new Color(1f, 0.6f, 0.35f));
            }
            else
            {
                // Un rival dont l'alarme sonne court voir, arme au poing.
                Rival.NotifyTheft(owner, intruder);
            }
        }
    }
}
