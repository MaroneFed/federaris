using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CAPACITES DE MALADE (05/10 -- Martin : "fais le mec qui veut vraiment des capacites de
    /// malade mental, qu'on pousse un peu plus loin ; ouvre le choix beaucoup, beaucoup plus
    /// large"). Les actives qui vivent quelques secondes dans le monde :
    ///
    ///   METEORE    tu bondis, puis tu t'ecrases : tout s'envole a dix metres ;
    ///   TORNADE    une tornade file devant toi et aspire tout le monde vers le ciel ;
    ///   TROU NOIR  il aspire tout ce qui est autour, puis il explose ;
    ///   GEANT      sept secondes geant : on ne te bouge plus, et tu bouscules tout ce que tu touches ;
    ///   RESSORT    un trampoline a tes pieds -- tout le monde peut s'en servir ;
    ///   FOUDRE     elle tombe la ou ta cible etait, 0,8 s plus tard (on peut l'esquiver).
    ///
    /// (Le BOULET et la FUSEE n'ont besoin de rien qui dure : voir AbilityCaster.Cast.)
    /// Tout ce qui touche passe par Combat.Hit / Combat.Blast : les protections, le Vol de la
    /// Couronne, le jeu en ligne s'appliquent comme pour le reste.
    /// </summary>
    public class MeteorStrike : MonoBehaviour
    {
        public const float Radius = 10f;
        Seeker by;
        IMover mover;
        Vector3 flat;
        float age;
        bool diving;

        public static void Begin(Seeker s, IMover m, Vector3 flat)
        {
            if (s == null || s.Body == null || m == null) return;
            MeteorStrike old = s.Body.GetComponent<MeteorStrike>();
            if (old != null) Destroy(old);
            MeteorStrike k = s.Body.gameObject.AddComponent<MeteorStrike>();
            k.by = s;
            k.mover = m;
            k.flat = flat;
            m.Push(Vector3.up * 19f + flat * 7f);
            Fx.Column(s.Body.position, AbilityInfo.Tint(Ability.Meteore), 16f, 0.3f, 0.6f);
            Fx.Trail(s.Body, AbilityInfo.Tint(Ability.Meteore), 1.6f, 1.2f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || by == null || by.Body == null) { Destroy(this); return; }
            age += dt;
            if (!diving && age > 0.5f)
            {
                diving = true;
                mover.Dash((Vector3.down + flat * 0.35f).normalized, 46f, 1.6f);
                Sfx.WhooshAt(transform.position);
            }
            if (!diving) return;
            bool ground = Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, 1.4f, ~0, QueryTriggerInteraction.Ignore);
            if (!ground && age < 2.4f)
            {
                if (Random.value < 0.6f) Fx.Burst(transform.position + Vector3.up, new Color(1f, 0.5f, 0.2f), 6, 3f, 0.25f, 0.4f, 0f, Vector3.up, 30f);
                return;
            }
            Impact();
        }

        void Impact()
        {
            Vector3 p = transform.position;
            Color c = AbilityInfo.Tint(Ability.Meteore);
            mover.Dash(Vector3.zero, 0f, 0f);
            Combat.Blast(p, Radius, 30f, 15f, by);
            Fx.Shock(p + Vector3.up * 0.8f, c, Radius, 0.55f);
            Fx.Shock(p + Vector3.up * 0.8f, Color.white, Radius * 0.5f, 0.3f);
            Fx.GroundRing(p, c, Radius + 2f, 0.6f);
            Fx.GroundRing(p, new Color(1f, 0.85f, 0.4f), Radius * 0.6f, 0.4f);
            Fx.Burst(p + Vector3.up * 0.3f, c, 180, 22f, 0.3f, 0.9f, 0.4f, Vector3.up, 75f);
            Fx.Burst(p + Vector3.up * 0.1f, new Color(0.62f, 0.55f, 0.48f), 90, 12f, 0.55f, 1.2f, 0.5f, Vector3.up, 85f);
            Fx.Flash(p + Vector3.up, c, 30f, 9f, 0.5f);
            AbilityCaster.ShakeNear(p, 0.6f);
            Sfx.KoBoom(p, by.IsPlayer);
            Destroy(this);
        }
    }

    /// <summary>LA TORNADE : un entonnoir de vent qui file a 14 m/s pendant 3,5 s et jette tout le monde en l'air.</summary>
    public class Twister : MonoBehaviour
    {
        const float Speed = 14f;
        const float Life = 3.5f;
        const float Radius = 3.4f;
        Seeker by;
        Vector3 dir;
        float age, ringTimer, soundTimer;
        readonly List<Seeker> struck = new List<Seeker>();

        public static void Launch(Seeker s, Vector3 from, Vector3 flat)
        {
            GameObject go = new GameObject("TORNADE");
            go.transform.position = from;
            Twister t = go.AddComponent<Twister>();
            t.by = s;
            t.dir = flat.normalized;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            if (age > Life) { Destroy(gameObject); return; }
            Vector3 p = transform.position + dir * Speed * dt;
            // Elle rase le sol (et passe le vide a sa hauteur).
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 6f, Vector3.down, out hit, 14f, ~0, QueryTriggerInteraction.Ignore)) p.y = Mathf.Lerp(p.y, hit.point.y, 0.3f);
            transform.position = p;
            Color c = AbilityInfo.Tint(Ability.Tornade);
            ringTimer -= dt;
            if (ringTimer <= 0f)
            {
                ringTimer = 0.05f;
                float h = Random.value * 9f;
                float r = Mathf.Lerp(0.6f, 3.4f, h / 9f);
                Fx.Ring(p + Vector3.up * h, Color.Lerp(c, Color.white, Random.value * 0.5f), r * 0.8f, r * 1.25f, 0.3f, 0.12f, Vector3.up);
                if (Random.value < 0.4f) Fx.Burst(p + Vector3.up * 0.2f, new Color(0.65f, 0.58f, 0.5f), 8, 5f, 0.35f, 0.5f, 0.2f, Vector3.up, 70f);
            }
            soundTimer -= dt;
            if (soundTimer <= 0f) { soundTimer = 0.45f; Sfx.WhooshAt(p); }
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null || struck.Contains(s)) continue;
                Vector3 d = s.Body.position - p;
                if (d.y < -1.5f || d.y > 9f || new Vector2(d.x, d.z).magnitude > Radius) continue;
                struck.Add(s);
                Vector3 swirl = new Vector3(-d.z, 0f, d.x).normalized;
                Combat.Hit(s, Vector3.up * 27f + swirl * 7f + dir * 8f, 0.45f, true, by);
                Fx.Impact(s.Body.position + Vector3.up * 1.1f, c, 1.2f);
            }
        }
    }

    /// <summary>LE TROU NOIR : il aspire (deux fois) ce qui est a 15 m, puis il explose.</summary>
    public class Vortex : MonoBehaviour
    {
        public const float Pull = 15f;
        const float Blow = 1.7f;
        Seeker by;
        float age, ringTimer;
        int pulls;

        public static void Open(Seeker s, Vector3 at)
        {
            GameObject go = new GameObject("TROU NOIR");
            go.transform.position = at;
            Vortex v = go.AddComponent<Vortex>();
            v.by = s;
            Fx.Shock(at + Vector3.up * 1.2f, new Color(0.15f, 0.05f, 0.3f), 2.5f, 0.4f);
            Sfx.WhooshAt(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 c = transform.position + Vector3.up * 1.2f;
            Color tint = AbilityInfo.Tint(Ability.TrouNoir);
            ringTimer -= dt;
            if (ringTimer <= 0f && age < Blow)
            {
                ringTimer = 0.09f;
                Fx.Ring(c, Color.Lerp(tint, Color.white, Random.value * 0.3f), Pull, 0.4f, 0.45f, 0.18f, Vector3.up);
                Fx.Shock(c, new Color(0.08f, 0.02f, 0.18f), 1.6f + Mathf.Sin(age * 20f) * 0.3f, 0.12f);
            }
            if (pulls < 2 && age > 0.15f + pulls * 0.6f)
            {
                pulls++;
                for (int i = 0; i < Game.Seekers.Count; i++)
                {
                    Seeker s = Game.Seekers[i];
                    if (s == by || s.Body == null) continue;
                    Vector3 d = transform.position - s.Body.position;
                    float flatD = new Vector2(d.x, d.z).magnitude;
                    if (flatD > Pull || Mathf.Abs(d.y) > 6f || flatD < 0.8f) continue;
                    Vector3 toward = new Vector3(d.x, 0f, d.z) / flatD;
                    Combat.Hit(s, toward * Mathf.Clamp(flatD * 1.5f, 8f, 22f) + Vector3.up * 4f, 0.2f, false, by);
                }
            }
            if (age < Blow) return;
            Combat.Blast(transform.position, 8f, 30f, 15f, by);
            Fx.Shock(c, tint, 9f, 0.5f);
            Fx.Shock(c, Color.white, 5f, 0.3f);
            Fx.Burst(c, tint, 200, 24f, 0.25f, 0.9f, 0f, Vector3.zero, 0f);
            Fx.Flash(c, tint, 28f, 9f, 0.5f);
            Fx.GroundRing(transform.position, tint, 11f, 0.5f);
            AbilityCaster.ShakeNear(transform.position, 0.5f);
            Sfx.KoBoom(transform.position, by != null && by.IsPlayer);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// LE GEANT : sept secondes, le corps grossit (x1,7), on ne te bouge presque plus (Combat.Hit),
    /// ta poussee porte plus loin (Combat.Shove), et qui te touche est bouscule.
    /// </summary>
    public class GiantAura : MonoBehaviour
    {
        public const float Seconds = 7f;
        Seeker who;
        Transform figure;
        float until;
        readonly Dictionary<Seeker, float> bumped = new Dictionary<Seeker, float>();

        public static void Grow(Seeker s)
        {
            if (s == null || s.Body == null) return;
            GiantAura a = s.Body.GetComponent<GiantAura>();
            if (a == null) a = s.Body.gameObject.AddComponent<GiantAura>();
            a.who = s;
            a.until = Time.time + Seconds;
            s.GiantUntil = a.until;
            CharacterRig rig = s.IsPlayer ? Game.Rig : null;
            if (!s.IsPlayer) { Rival r = Rival.Of(s); if (r != null) rig = r.Rig; }
            a.figure = rig != null ? rig.transform : null;
            Color c = AbilityInfo.Tint(Ability.Geant);
            Fx.Shock(s.Body.position + Vector3.up * 1.5f, c, 4f, 0.4f);
            Fx.Column(s.Body.position, c, 12f, 0.5f, 1.4f);
            Sfx.KoBoom(s.Body.position, s.IsPlayer);
        }

        void Update()
        {
            if (who == null || who.Body == null) { Destroy(this); return; }
            bool on = Time.time < until;
            float want = on ? 1.7f : 1f;
            if (figure != null)
            {
                float k = Mathf.MoveTowards(figure.localScale.x, want, Time.deltaTime * 2.5f);
                figure.localScale = new Vector3(k, k, k);
                if (!on && Mathf.Abs(k - 1f) < 0.001f) { Destroy(this); return; }
            }
            else if (!on) { Destroy(this); return; }
            if (!on) return;
            // Qui le touche est bouscule (une fois par 0,7 s chacun).
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == who || s.Body == null) continue;
                Vector3 d = s.Body.position - who.Body.position;
                if (Mathf.Abs(d.y) > 2.6f || new Vector2(d.x, d.z).magnitude > 2.3f) continue;
                float last;
                if (bumped.TryGetValue(s, out last) && Time.time - last < 0.7f) continue;
                bumped[s] = Time.time;
                Vector3 away = new Vector3(d.x, 0f, d.z);
                away = away.sqrMagnitude > 0.01f ? away.normalized : who.Body.forward;
                Combat.Hit(s, away * 22f + Vector3.up * 8f, 0.25f, true, who);
            }
        }
    }

    /// <summary>LE RESSORT : un trampoline pose au sol, 14 s ; qui marche dessus (toi compris) part a 25 m.</summary>
    public class Spring : MonoBehaviour
    {
        public static readonly List<Spring> All = new List<Spring>();
        Seeker owner;
        float until;
        Transform pad;
        float squash;
        readonly Dictionary<Seeker, float> used = new Dictionary<Seeker, float>();

        public static Spring Place(Seeker owner, Vector3 at)
        {
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return null;
            int mine = 0;
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].owner == owner) mine++;
            if (mine >= 2)
                for (int i = 0; i < All.Count; i++)
                    if (All[i] != null && All[i].owner == owner) { Destroy(All[i].gameObject); break; }
            GameObject go = new GameObject("RESSORT de " + owner.Name);
            go.transform.position = hit.point + Vector3.up * 0.02f;
            Spring s = go.AddComponent<Spring>();
            s.owner = owner;
            s.until = Time.time + 14f;
            Color c = AbilityInfo.Tint(Ability.Ressort);
            Proto.BeginVisualOnly();
            Proto.Cylinder(go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(2.4f, 0.05f, 2.4f), new Color(0.16f, 0.17f, 0.24f), "Socle");
            for (int k = 0; k < 4; k++)
                Proto.Cylinder(go.transform, new Vector3(0f, 0.16f + k * 0.12f, 0f), new Vector3(1.6f - k * 0.08f, 0.03f, 1.6f - k * 0.08f), new Color(0.75f, 0.75f, 0.8f), "Spire");
            s.pad = new GameObject("Toile").transform;
            s.pad.SetParent(go.transform, false);
            s.pad.localPosition = new Vector3(0f, 0.62f, 0f);
            GameObject top = Proto.Cylinder(s.pad, Vector3.zero, new Vector3(2.2f, 0.06f, 2.2f), Color.white, "Toile");
            top.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.5f, 0f);
            GameObject dot = Proto.Cylinder(s.pad, new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.02f, 0.9f), Color.white, "Cible");
            dot.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Color.white, 0.6f);
            Proto.EndVisualOnly();
            All.Add(s);
            Sfx.BuildAt(go.transform.position);
            return s;
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (Time.time > until) { Destroy(gameObject); return; }
            squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 4f);
            if (pad != null) pad.localScale = new Vector3(1f + 0.15f * squash, 1f - 0.6f * squash, 1f + 0.15f * squash);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 d = s.Body.position - transform.position;
                if (d.y < -0.4f || d.y > 1.6f || new Vector2(d.x, d.z).magnitude > 1.3f) continue;
                float last;
                if (used.TryGetValue(s, out last) && Time.time - last < 1f) continue;
                used[s] = Time.time;
                Bounce(s);
            }
        }

        void Bounce(Seeker s)
        {
            squash = 1f;
            Color c = AbilityInfo.Tint(Ability.Ressort);
            if (s == owner || s.Remote)
            {
                // Le proprietaire (et un ami, chez lui) : un saut, pas un coup.
                IMover m = AbilityCaster.MoverOf(s);
                if (m != null && !s.Remote) m.Push(Vector3.up * 26f + s.Body.forward * 4f);
            }
            else Combat.Hit(s, Vector3.up * 26f, 0f, false, owner);
            Fx.Ring(transform.position + Vector3.up * 0.7f, c, 0.8f, 3f, 0.3f, 0.2f, Vector3.up);
            Fx.Column(transform.position, c, 14f, 0.3f, 0.6f);
            Sfx.BoingAt(transform.position);
        }
    }

    /// <summary>LA FOUDRE : un cercle qui crepite la ou la cible etait, et 0,8 s plus tard, l'eclair.</summary>
    public class Lightning : MonoBehaviour
    {
        const float Delay = 0.8f;
        const float Radius = 2.6f;
        Seeker by;
        float age, crackle;

        public static void Call(Seeker s, Vector3 at)
        {
            RaycastHit hit;
            if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) at = hit.point;
            GameObject go = new GameObject("FOUDRE");
            go.transform.position = at;
            Lightning l = go.AddComponent<Lightning>();
            l.by = s;
            Sfx.ChipAt(at);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector3 p = transform.position;
            Color c = AbilityInfo.Tint(Ability.Foudre);
            crackle -= dt;
            if (age < Delay && crackle <= 0f)
            {
                crackle = 0.1f;
                Fx.GroundRing(p, c, Radius, 0.15f);
                Fx.Sparks(p + Vector3.up * 0.2f, c, 6, 3f);
            }
            if (age < Delay) return;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == by || s.Body == null) continue;
                Vector3 d = s.Body.position - p;
                if (Mathf.Abs(d.y) > 3f || new Vector2(d.x, d.z).magnitude > Radius) continue;
                Vector3 away = new Vector3(d.x, 0f, d.z);
                away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                Combat.Hit(s, Vector3.up * 16f + away * 8f, 0.7f, true, by);
            }
            Fx.Column(p, Color.white, 70f, 0.25f, 0.5f);
            Fx.Column(p, c, 70f, 0.5f, 1.2f);
            Fx.Shock(p + Vector3.up, c, Radius + 1f, 0.35f);
            Fx.Flash(p + Vector3.up * 3f, c, 40f, 10f, 0.4f);
            Fx.Burst(p + Vector3.up * 0.3f, c, 90, 14f, 0.15f, 0.5f, 0.2f, Vector3.up, 80f);
            AbilityCaster.ShakeNear(p, 0.4f);
            Sfx.KoBoom(p, by != null && by.IsPlayer);
            Destroy(gameObject);
        }
    }
}
