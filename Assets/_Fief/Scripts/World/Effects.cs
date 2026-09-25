using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE MINE (la capacite Mine) : un disque de fer a moitie enterre, une rune qui
    /// luit faiblement. Qui marche dessus -- sauf son poseur -- est projete en l'air
    /// et lache la Couronne. On ne la voit qu'a cinq metres ; les siennes, toujours.
    /// Deux au plus par joueur : la troisieme remplace la plus vieille.
    /// </summary>
    public class Mine : MonoBehaviour
    {
        public static readonly List<Mine> All = new List<Mine>();
        Seeker owner;
        Renderer[] parts;
        bool shown = true;

        const float Trigger = 1.2f;
        static readonly Color Rune = new Color(1f, 0.45f, 0.15f);

        public static Mine Place(Seeker owner, Vector3 at)
        {
            // Deux au plus : la plus vieille saute.
            int mine = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == owner) mine++;
            if (mine >= 2)
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].owner == owner) { Destroy(All[i].gameObject); break; }

            GameObject go = new GameObject("MINE de " + owner.Name);
            go.transform.position = Ground.Place(at.x, at.z, 0.02f);
            RaycastHit hit;
            if (Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) go.transform.position = hit.point + Vector3.up * 0.02f;
            Mine m = go.AddComponent<Mine>();
            m.owner = owner;
            Proto.BeginVisualOnly();
            Proto.Cylinder(go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.04f, 0.9f), new Color(0.16f, 0.15f, 0.15f), "Disque");
            GameObject rune = Proto.Cylinder(go.transform, new Vector3(0f, 0.09f, 0f), new Vector3(0.4f, 0.02f, 0.4f), Color.white, "Rune");
            rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Rune, 2f);
            Proto.EndVisualOnly();
            m.parts = go.GetComponentsInChildren<Renderer>();
            All.Add(m);
            Sfx.Build();
            return m;
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            Transform player = Game.PlayerTransform;
            bool see = owner == Game.Me || player != null && (player.position - transform.position).magnitude < 5f;
            if (see != shown)
            {
                shown = see;
                for (int i = 0; i < parts.Length; i++) if (parts[i] != null) parts[i].enabled = see;
            }
            if (Game.Season == null || !Game.Season.Running) return;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == owner || s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                if (Mathf.Abs(d.y) > 1.5f) continue;
                d.y = 0f;
                if (d.magnitude > Trigger) continue;
                Combat.Hit(s, Vector3.up * 15f + d.normalized * 3f, 0.4f, true, owner);
                Ambiance.Burst(null, transform.position + Vector3.up * 0.4f, Rune);
                Sfx.TrapSnap();
                if (owner != null && owner.IsPlayer) Stats.MineHits++;
                Destroy(gameObject);
                return;
            }
        }
    }

    /// <summary>
    /// UN MUR DE PIERRE (la capacite Mur) : il jaillit du sol devant toi en un quart de
    /// seconde, six metres de large, trois de haut, et redescend huit secondes plus
    /// tard. Pour couper la route d'un porteur, ou se mettre a l'abri d'un Oeil.
    /// </summary>
    public class StoneWall : MonoBehaviour
    {
        float age;
        Vector3 down, up;
        const float Life = 8f;

        public static void Raise(Vector3 at, Vector3 facing)
        {
            Vector3 f = new Vector3(facing.x, 0f, facing.z).normalized;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            float ground = at.y;
            RaycastHit hit;
            if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) ground = hit.point.y;
            GameObject go = new GameObject("MUR");
            go.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            StoneWall w = go.AddComponent<StoneWall>();
            w.up = new Vector3(at.x, ground, at.z);
            w.down = w.up - Vector3.up * 3.4f;
            go.transform.position = w.down;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.6f, 0f);
            box.size = new Vector3(6f, 3.2f, 0.9f);
            Proto.BeginVisualOnly();
            Color stone = new Color(0.34f, 0.33f, 0.31f);
            for (int i = 0; i < 5; i++)
                Proto.Cube(go.transform, new Vector3(-2.4f + i * 1.2f, 1.6f + (i % 2) * 0.12f, 0f), new Vector3(1.18f, 3.2f + (i % 2) * 0.24f, 0.9f),
                           i % 2 == 0 ? stone : Palette.Shade(stone, 0.85f), "Pierre");
            GameObject rune = Proto.Cube(go.transform, new Vector3(0f, 2.2f, -0.46f), new Vector3(0.8f, 0.8f, 0.02f), Color.white, "Rune");
            rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(AbilityInfo.Tint(Ability.Mur), 1.6f);
            Proto.EndVisualOnly();
            Ambiance.Burst(null, w.up + Vector3.up * 0.5f, new Color(0.5f, 0.45f, 0.4f));
            Sfx.Crash();
        }

        void Update()
        {
            age += Time.deltaTime;
            float rise = Mathf.Clamp01(age / 0.25f);
            float sink = Mathf.Clamp01((age - Life) / 0.6f);
            transform.position = Vector3.Lerp(down, up, rise * (1f - sink));
            if (sink >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// UNE CHAINE LUMINEUSE entre deux points (le grappin, le crochet, l'echange) :
    /// elle dure le temps du geste. On voit qui a tire qui.
    /// </summary>
    public class Tether : MonoBehaviour
    {
        Transform from, to;
        Vector3 fixedTo;
        float life;
        LineRenderer line;

        public static void Show(Transform from, Transform to, Vector3 fixedTo, float seconds, Color colour)
        {
            if (from == null) return;
            GameObject go = new GameObject("Chaîne");
            Tether t = go.AddComponent<Tether>();
            t.from = from;
            t.to = to;
            t.fixedTo = fixedTo;
            t.life = seconds;
            t.line = go.AddComponent<LineRenderer>();
            t.line.positionCount = 2;
            t.line.useWorldSpace = true;
            t.line.startWidth = 0.08f;
            t.line.endWidth = 0.05f;
            t.line.sharedMaterial = MaterialFactory.GetGlow(colour, 2.5f);
            t.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            t.LateUpdate();
        }

        void LateUpdate()
        {
            life -= Time.deltaTime;
            if (life <= 0f || from == null) { Destroy(gameObject); return; }
            line.SetPosition(0, from.position + Vector3.up * 1.2f);
            line.SetPosition(1, to != null ? to.position + Vector3.up * 1.1f : fixedTo);
        }
    }
}
