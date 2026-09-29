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
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return null;
            GameObject go = new GameObject("MINE de " + owner.Name);
            go.transform.position = hit.point + Vector3.up * 0.02f;

            // Deux au plus : la plus vieille saute.
            int mine = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == owner) mine++;
            if (mine >= 2)
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].owner == owner) { Destroy(All[i].gameObject); break; }
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
                // Un protege passe dessus sans la declencher (elle l'aurait gaspillee).
                if (s == owner || s.Body == null || s.Graced) continue;
                Vector3 d = s.Body.position - transform.position;
                if (Mathf.Abs(d.y) > 1.5f) continue;
                d.y = 0f;
                if (d.magnitude > Trigger) continue;
                // L'explosion emporte tout le monde a 4 m (28/09 : plus fort).
                for (int k = 0; k < Game.Seekers.Count; k++)
                {
                    Seeker o = Game.Seekers[k];
                    if (o == owner || o.Body == null) continue;
                    Vector3 e = o.Body.position - transform.position;
                    if (e.magnitude > 4f) continue;
                    Vector3 away = new Vector3(e.x, 0f, e.z);
                    away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                    Combat.Hit(o, Vector3.up * 19f + away * 6f, 0.4f, true, owner);
                }
                // L'explosion : une colonne de feu, une sphere, un anneau, un eclair.
                Fx.Column(transform.position, Rune, 22f, 0.3f, 0.7f);
                Fx.Shock(transform.position + Vector3.up * 0.5f, Rune, 4f, 0.4f);
                Fx.GroundRing(transform.position, Rune, 5.5f, 0.45f);
                Fx.Burst(transform.position + Vector3.up * 0.3f, Rune, 80, 12f, 0.2f, 0.8f, 0.6f, Vector3.up, 55f);
                Fx.Flash(transform.position + Vector3.up, Rune, 14f, 7f, 0.35f);
                Sfx.TrapSnap();
                if (owner != null && owner.IsPlayer) Stats.MineHits++;
                Destroy(gameObject);
                return;
            }
        }
    }

    /// <summary>
    /// UN MUR DE PIERRE (la capacite Mur) : il jaillit du sol devant toi en un quart de
    /// seconde, DIX metres de large, quatre et demi de haut (28/09 : plus grand), et
    /// redescend huit secondes plus tard. Qui se tient la ou il sort est PROJETE en
    /// l'air. Pour couper la route d'un porteur, se mettre a l'abri d'un Oeil -- ou
    /// catapulter quelqu'un hors de la rampe.
    /// </summary>
    public class StoneWall : MonoBehaviour
    {
        float age;
        Vector3 down, up;
        const float Life = 8f;

        const float Wide = 10f;
        const float Tall = 4.5f;

        public static bool Raise(Vector3 at, Vector3 facing, Seeker by)
        {
            Vector3 f = new Vector3(facing.x, 0f, facing.z).normalized;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return false;
            float ground = hit.point.y;
            GameObject go = new GameObject("MUR");
            go.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            StoneWall w = go.AddComponent<StoneWall>();
            w.up = new Vector3(at.x, ground, at.z);
            w.down = w.up - Vector3.up * (Tall + 0.3f);
            go.transform.position = w.down;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, Tall * 0.5f, 0f);
            box.size = new Vector3(Wide, Tall, 1.1f);
            Proto.BeginVisualOnly();
            Color stone = new Color(0.34f, 0.33f, 0.31f);
            int blocks = 8;
            float bw = Wide / blocks;
            for (int i = 0; i < blocks; i++)
            {
                float extra = (i % 3) * 0.25f;
                Proto.Cube(go.transform, new Vector3(-Wide * 0.5f + bw * (i + 0.5f), (Tall + extra) * 0.5f, 0f), new Vector3(bw - 0.03f, Tall + extra, 1.1f),
                           i % 2 == 0 ? stone : Palette.Shade(stone, 0.85f), "Pierre");
            }
            for (int k = -1; k <= 1; k++)
            {
                GameObject rune = Proto.Cube(go.transform, new Vector3(k * 3.2f, Tall * 0.6f, -0.57f), new Vector3(0.7f, 0.7f, 0.02f), Color.white, "Rune");
                rune.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(AbilityInfo.Tint(Ability.Mur), 1.8f);
            }
            Proto.EndVisualOnly();
            // Qui se tient la ou il sort part en l'air.
            Vector3 right = new Vector3(f.z, 0f, -f.x);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                Vector3 rel = s.Body.position - w.up;
                if (Mathf.Abs(Vector3.Dot(rel, right)) > Wide * 0.5f || Mathf.Abs(Vector3.Dot(rel, f)) > 1.2f || Mathf.Abs(rel.y) > 2f) continue;
                Combat.Hit(s, Vector3.up * 19f + f * 5f, 0.3f, true, by);
            }
            Ambiance.Burst(null, w.up + Vector3.up * 0.5f, new Color(0.5f, 0.45f, 0.4f));
            Sfx.Crash();
            return true;
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
            t.line.startWidth = 0.16f;
            t.line.endWidth = 0.1f;
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
