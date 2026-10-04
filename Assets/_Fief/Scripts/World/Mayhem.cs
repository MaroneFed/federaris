using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CAPACITES DE FOU (06/10 -- Martin : "il peut y avoir plein de capacites, une prison
    /// ou tu peux rester, ca t'enchaine au sol pendant dix secondes, plein de conneries comme ca,
    /// des trucs de fou, il m'en faut une vingtaine"). Ce qui vit quelques secondes dans le monde :
    ///
    ///   la CAGE de la Prison, le RETRECI du Mini, le BALLON qui s'envole, les ETOILES de la tete a
    ///   l'envers, la BOMBE collante, la flaque de GLU, les PEAUX DE BANANE, la TOUPIE, le DELUGE
    ///   de meteores, le GANT DE BOXE.
    ///
    /// Les sorts eux-memes (prison, glu, encre...) se posent par Combat.Afflict ; tout ce qui
    /// frappe passe par Combat.Hit / Combat.Blast : les protections, le vol de la Couronne et le
    /// jeu en ligne s'appliquent comme pour le reste.
    /// </summary>
    public static class Mayhem
    {
        /// <summary>Le corps visible de "s" (sa figure), pour le grossir, le retrecir, le faire tourner.</summary>
        public static Transform FigureOf(Seeker s)
        {
            if (s == null) return null;
            CharacterRig rig = s.IsPlayer ? Game.Rig : null;
            if (!s.IsPlayer) { Rival r = Rival.Of(s); if (r != null) rig = r.Rig; }
            return rig != null ? rig.transform : null;
        }

        /// <summary>Ce qu'on voit quand un sort tombe sur "victim" (Combat.Afflict l'appelle).</summary>
        public static void Show(Seeker victim, Combat.Affliction what, Seeker by)
        {
            if (victim == null || victim.Body == null) return;
            Vector3 head = victim.Body.position + Vector3.up * 1.8f;
            switch (what)
            {
                case Combat.Affliction.Prison:
                    Cage.Lock(victim, false);
                    break;
                case Combat.Affliction.Frozen:
                    Cage.Lock(victim, true);
                    break;
                case Combat.Affliction.Tiny:
                    Shrink.Begin(victim);
                    Fx.Burst(head, AbilityInfo.Tint(Ability.Mini), 40, 6f, 0.15f, 0.5f, 0f, Vector3.zero, 0f);
                    Sfx.ChipAt(victim.Body.position);
                    break;
                case Combat.Affliction.Balloon:
                    BalloonRide.Begin(victim, by);
                    break;
                case Combat.Affliction.Inverted:
                    Dizzy.Begin(victim);
                    break;
                case Combat.Affliction.Dance:
                {
                    CharacterRig rig = CharacterRig.Of(victim);
                    if (rig != null) rig.Celebrate(3f);
                    Fx.Confetti(head, new[] { new Color(1f, 0.3f, 0.6f), new Color(0.3f, 0.8f, 1f), new Color(1f, 0.9f, 0.3f) }, 30, 6f, Vector3.up, 60f);
                    break;
                }
                case Combat.Affliction.Ink:
                    Fx.Burst(head, new Color(0.08f, 0.06f, 0.16f), 60, 6f, 0.3f, 0.8f, 0.3f, Vector3.zero, 0f);
                    Sfx.ChipAt(victim.Body.position);
                    break;
            }
        }
    }

    /// <summary>LA PRISON : une cage de barreaux et une chaine au sol, tant qu'il est enchaine.</summary>
    public class Cage : MonoBehaviour
    {
        Seeker who;
        float rattle;

        public static void Lock(Seeker s, bool ice)
        {
            Cage old = Find(s);
            if (old != null) return;
            GameObject go = new GameObject((ice ? "GLACE de " : "PRISON de ") + s.Name);
            go.transform.position = s.Body.position;
            Cage c = go.AddComponent<Cage>();
            c.who = s;
            if (ice)
            {
                // (08/10) LE TEMPS S'ARRETE : un bloc de glace bleu, pas de barreaux.
                Color blue = AbilityInfo.Tint(Ability.ArretTemps);
                Proto.BeginVisualOnly();
                GameObject block = Proto.Capsule(go.transform, new Vector3(0f, 1.1f, 0f), new Vector3(1.6f, 1.3f, 1.6f), blue, "Glace");
                block.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Color.Lerp(blue, Color.white, 0.4f), 0.8f);   // (v35) de la glace qui luit, plus un bloc transparent qui sortait noir
                Proto.EndVisualOnly();
                All.Add(c);
                Fx.Burst(s.Body.position + Vector3.up, new Color(0.8f, 0.95f, 1f), 40, 4f, 0.2f, 0.8f, 0.2f, Vector3.zero, 0f);
                Sfx.ChipAt(s.Body.position);
                return;
            }
            Color iron = new Color(0.32f, 0.33f, 0.4f);
            Proto.BeginVisualOnly();
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Proto.Cylinder(go.transform, new Vector3(Mathf.Cos(a) * 1.05f, 1.2f, Mathf.Sin(a) * 1.05f), new Vector3(0.09f, 1.2f, 0.09f), iron, "Barreau");
            }
            Proto.Cylinder(go.transform, new Vector3(0f, 2.42f, 0f), new Vector3(2.3f, 0.06f, 2.3f), iron, "Couvercle");
            Proto.Cylinder(go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.04f, 2.4f), iron, "Socle");
            GameObject ring = Proto.Cylinder(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(2.2f, 0.05f, 2.2f), Color.white, "Bague");
            ring.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(AbilityInfo.Tint(Ability.Prison), 0.8f);
            Proto.EndVisualOnly();
            All.Add(c);
            Fx.Shock(s.Body.position + Vector3.up, AbilityInfo.Tint(Ability.Prison), 2.4f, 0.3f);
            Fx.Burst(s.Body.position + Vector3.up * 2.4f, new Color(0.7f, 0.7f, 0.8f), 40, 6f, 0.15f, 0.5f, 0.6f, Vector3.down, 40f);
            Sfx.ClangAt(s.Body.position);
        }

        static readonly List<Cage> All = new List<Cage>();
        static Cage Find(Seeker s) { for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].who == s) return All[i]; return null; }
        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (who == null || who.Body == null || !who.Rooted)
            {
                if (who != null && who.Body != null)
                {
                    Fx.Burst(transform.position + Vector3.up * 1.2f, new Color(0.7f, 0.7f, 0.8f), 50, 9f, 0.15f, 0.6f, 0.5f, Vector3.up, 80f);
                    Sfx.ClangAt(transform.position);
                }
                Destroy(gameObject);
                return;
            }
            transform.position = who.Body.position;
            rattle -= Time.deltaTime;
            if (rattle <= 0f)
            {
                rattle = 0.6f;
                Fx.GroundRing(transform.position, AbilityInfo.Tint(Ability.Prison), 1.4f, 0.4f);
            }
        }
    }

    /// <summary>LE MINI : le corps retrecit (x0,45), puis revient.</summary>
    public class Shrink : MonoBehaviour
    {
        Seeker who;
        Transform figure;

        public static void Begin(Seeker s)
        {
            Shrink k = s.Body.GetComponent<Shrink>();
            if (k == null) k = s.Body.gameObject.AddComponent<Shrink>();
            k.who = s;
            k.figure = Mayhem.FigureOf(s);
        }

        void Update()
        {
            if (who == null || figure == null) { Destroy(this); return; }
            if (who.Giant) return;    // le Geant decide de sa taille
            bool on = who.Tiny;
            float want = on ? 0.45f : 1f;
            float k = Mathf.MoveTowards(figure.localScale.x, want, Time.deltaTime * 2.5f);
            figure.localScale = new Vector3(k, k, k);
            if (!on && Mathf.Abs(k - 1f) < 0.001f) Destroy(this);
        }
    }

    /// <summary>
    /// LE BALLON : il gonfle (x1,4), un ballon rose au-dessus de sa tete, il s'envole (5 m/s) en
    /// derivant loin de qui l'a vise -- puis POP, il retombe, assomme une demi-seconde.
    /// </summary>
    public class BalloonRide : MonoBehaviour
    {
        Seeker who, by;
        Transform figure, balloon;
        bool popped;

        public static void Begin(Seeker s, Seeker by)
        {
            BalloonRide b = s.Body.GetComponent<BalloonRide>();
            if (b != null) return;
            b = s.Body.gameObject.AddComponent<BalloonRide>();
            b.who = s;
            b.by = by;
            b.figure = Mayhem.FigureOf(s);
            Color c = AbilityInfo.Tint(Ability.Ballon);
            Proto.BeginVisualOnly();
            GameObject go = Proto.Sphere(null, s.Body.position + Vector3.up * 3.4f, new Vector3(1.6f, 1.9f, 1.6f), c, "Ballon");
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.85f, 0f);
            Proto.EndVisualOnly();
            b.balloon = go.transform;
            // Il derive loin de qui l'a vise (et du fut de la tour).
            IMover m = AbilityCaster.MoverOf(s);
            if (m != null && !s.Remote)
            {
                Vector3 away = by != null && by.Body != null ? Combat.Flat(s.Body.position - by.Body.position) : Combat.Flat(s.Body.position);
                if (away.sqrMagnitude < 0.01f) away = s.Body.forward;
                m.Push(away.normalized * 7f + Vector3.up * 6f);
            }
            Sfx.WhooshAt(s.Body.position);
        }

        void Update()
        {
            if (who == null || who.Body == null) { Clean(); return; }
            if (balloon != null) balloon.position = Vector3.Lerp(balloon.position, who.Body.position + Vector3.up * 3.4f, Time.deltaTime * 12f);
            if (figure != null && !who.Giant)
            {
                float k = Mathf.MoveTowards(figure.localScale.x, who.Ballooned ? 1.4f : 1f, Time.deltaTime * 3f);
                figure.localScale = new Vector3(k, k, k);
            }
            if (who.Ballooned)
            {
                // Il monte : chez lui seulement (une marionnette suit sa machine).
                IMover m = AbilityCaster.MoverOf(who);
                if (m != null && !who.Remote) m.Push(Vector3.up * 5f);
                return;
            }
            if (!popped)
            {
                popped = true;
                Vector3 p = balloon != null ? balloon.position : who.Body.position + Vector3.up * 3f;
                Fx.Burst(p, AbilityInfo.Tint(Ability.Ballon), 80, 12f, 0.2f, 0.6f, 0.4f, Vector3.zero, 0f);
                Fx.Shock(p, Color.white, 2.5f, 0.2f);
                Sfx.PafAt(p);
                if (balloon != null) Destroy(balloon.gameObject);
                // POP : assomme une demi-seconde (le coup part de sa machine, une seule fois).
                if (!who.Remote) Combat.Hit(who, Vector3.down * 2f, 0.5f, true, by);
            }
            if (figure == null || Mathf.Abs(figure.localScale.x - 1f) < 0.001f) Clean();
        }

        void Clean()
        {
            if (balloon != null) Destroy(balloon.gameObject);
            Destroy(this);
        }

        void OnDestroy() { if (balloon != null) Destroy(balloon.gameObject); }
    }

    /// <summary>LA TETE A L'ENVERS : des etoiles qui tournent au-dessus de sa tete.</summary>
    public class Dizzy : MonoBehaviour
    {
        Seeker who;
        float t;

        public static void Begin(Seeker s)
        {
            if (s.Body.GetComponent<Dizzy>() != null) return;
            Dizzy d = s.Body.gameObject.AddComponent<Dizzy>();
            d.who = s;
            Sfx.ChipAt(s.Body.position);
        }

        void Update()
        {
            if (who == null || who.Body == null || !who.Inverted) { Destroy(this); return; }
            t -= Time.deltaTime;
            if (t > 0f) return;
            t = 0.12f;
            float a = Time.time * 7f;
            Vector3 p = who.Body.position + Vector3.up * 2.2f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.55f;
            Fx.Sparks(p, AbilityInfo.Tint(Ability.Inversion), 3, 1f);
        }
    }

    /// <summary>LA BOMBE COLLANTE : collee a sa cible, une meche qui crepite, BOUM 2 s apres.</summary>
    public class StickyBomb : MonoBehaviour
    {
        const float Fuse = 2f;
        Seeker by, target;
        float age, tick;

        public static void Stick(Seeker by, Seeker target)
        {
            if (target == null || target.Body == null) return;
            GameObject go = new GameObject("BOMBE sur " + target.Name);
            StickyBomb b = go.AddComponent<StickyBomb>();
            b.by = by;
            b.target = target;
            Proto.BeginVisualOnly();
            GameObject ball = Proto.Sphere(go.transform, Vector3.zero, new Vector3(0.7f, 0.7f, 0.7f), new Color(0.12f, 0.12f, 0.16f), "Bombe");
            ball.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(new Color(0.12f, 0.12f, 0.16f), 0.8f, 0.3f);
            Proto.Cylinder(go.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.06f, 0.14f, 0.06f), new Color(0.75f, 0.6f, 0.4f), "Meche");
            Proto.EndVisualOnly();
            go.transform.position = target.Body.position + Vector3.up * 2.4f;
            Sfx.ChipAt(target.Body.position);
        }

        void Update()
        {
            if (target == null || target.Body == null) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            transform.position = target.Body.position + Vector3.up * 2.4f;
            float s = 1f + 0.25f * Mathf.Sin(age * (8f + age * 14f));
            transform.localScale = new Vector3(s, s, s);
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = Mathf.Lerp(0.35f, 0.06f, age / Fuse);
                Fx.Sparks(transform.position + Vector3.up * 0.5f, new Color(1f, 0.7f, 0.3f), 4, 3f);
                Sfx.ChipAt(transform.position);
            }
            if (age < Fuse) return;
            Vector3 p = target.Body.position;
            Vector3 away = by != null && by.Body != null ? Combat.Flat(p - by.Body.position) : target.Body.forward;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            Combat.Hit(target, away * 30f + Vector3.up * 18f, 0.5f, true, by);
            // Et ceux qui etaient colles a lui.
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker o = Game.Seekers[i];
                if (o == target || o == by || o.Body == null) continue;
                Vector3 d = o.Body.position - p;
                if (Mathf.Abs(d.y) > 3f || Combat.Flat(d).magnitude > 5f) continue;
                Vector3 out2 = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                Combat.Hit(o, out2 * 20f + Vector3.up * 10f, 0.3f, true, by);
            }
            Color c = AbilityInfo.Tint(Ability.Bombe);
            DivineFx.Impact(p, 5f, c, 1f, (by != null && by.IsPlayer) || target.IsPlayer);
            Fx.Burst(p + Vector3.up, c, 140, 20f, 0.25f, 0.8f, 0f, Vector3.zero, 0f);
            Destroy(gameObject);
        }
    }

    /// <summary>LA GLU : une flaque verte de 7 m, 10 s ; qui marche dedans est englue (sauf qui l'a versee).</summary>
    public class GluePuddle : MonoBehaviour
    {
        public const float Radius = 3.5f;
        Seeker owner;
        float until, tick;

        public static bool Pour(Seeker owner, Vector3 at)
        {
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return false;
            GameObject go = new GameObject("GLU de " + owner.Name);
            go.transform.position = hit.point + Vector3.up * 0.03f;
            GluePuddle g = go.AddComponent<GluePuddle>();
            g.owner = owner;
            g.until = Time.time + 10f;
            Color c = AbilityInfo.Tint(Ability.Glu);
            Proto.BeginVisualOnly();
            GameObject disc = Proto.Cylinder(go.transform, Vector3.zero, new Vector3(Radius * 2f, 0.02f, Radius * 2f), c, "Flaque");
            disc.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(c, 0.95f, 0f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.05f;
                Proto.Sphere(go.transform, new Vector3(Mathf.Cos(a) * Radius * 0.6f, 0.08f, Mathf.Sin(a) * Radius * 0.55f), new Vector3(0.6f, 0.18f, 0.6f), c * 0.9f, "Bulle");
            }
            Proto.EndVisualOnly();
            Fx.GroundRing(go.transform.position, c, Radius, 0.4f);
            Sfx.ThudAt(go.transform.position);
            return true;
        }

        void Update()
        {
            if (Time.time > until) { Destroy(gameObject); return; }
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 0.4f;
            Vector3 p = transform.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == owner || s.Body == null) continue;
                Vector3 d = s.Body.position - p;
                if (d.y < -0.5f || d.y > 1.2f || new Vector2(d.x, d.z).magnitude > Radius) continue;
                Combat.Afflict(s, Combat.Affliction.Glue, 0.7f, owner);
            }
        }
    }

    /// <summary>LA PEAU DE BANANE : qui marche dessus glisse et part en salto (sauf qui l'a posee, la premiere seconde).</summary>
    public class BananaPeel : MonoBehaviour
    {
        Seeker owner;
        float born, until;

        public static void Drop(Seeker owner, Vector3 at)
        {
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return;
            GameObject go = new GameObject("BANANE de " + owner.Name);
            go.transform.position = hit.point + Vector3.up * 0.05f;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            BananaPeel b = go.AddComponent<BananaPeel>();
            b.owner = owner;
            b.born = Time.time;
            b.until = Time.time + 25f;
            Color y = AbilityInfo.Tint(Ability.Banane);
            Proto.BeginVisualOnly();
            Proto.Sphere(go.transform, Vector3.zero, new Vector3(0.7f, 0.14f, 0.32f), y, "Peau");
            for (int i = 0; i < 3; i++)
            {
                GameObject leaf = Proto.Sphere(go.transform, new Vector3(Mathf.Cos(i * 2.1f) * 0.42f, 0.02f, Mathf.Sin(i * 2.1f) * 0.42f), new Vector3(0.5f, 0.08f, 0.22f), y, "Lanière");
                leaf.transform.localRotation = Quaternion.Euler(0f, -i * 120f, 0f);
            }
            Proto.EndVisualOnly();
        }

        void Update()
        {
            if (Time.time > until) { Destroy(gameObject); return; }
            Vector3 p = transform.position;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null || s.Remote) continue;
                if (s == owner && Time.time - born < 1.2f) continue;
                Vector3 d = s.Body.position - p;
                if (d.y < -0.4f || d.y > 1f || new Vector2(d.x, d.z).magnitude > 0.9f) continue;
                // Il glisse vers l'avant, les pieds au ciel.
                Vector3 fwd = s.Body.forward;
                Combat.Hit(s, Combat.Flat(fwd).normalized * 14f + Vector3.up * 13f, 0.6f, true, s == owner ? null : owner);
                Fx.Burst(p, AbilityInfo.Tint(Ability.Banane), 30, 6f, 0.15f, 0.5f, 0.3f, Vector3.up, 60f);
                Sfx.BoingAt(p);
                Destroy(gameObject);
                return;
            }
        }
    }

    /// <summary>LA TOUPIE : 4 s, il tourne sur lui-meme, court plus vite, et qui le touche est ejecte.</summary>
    public class SpinAura : MonoBehaviour
    {
        public const float Seconds = 4f;
        Seeker who;
        Transform figure;
        float until, angle;
        readonly Dictionary<Seeker, float> bumped = new Dictionary<Seeker, float>();

        public static void Spin(Seeker s)
        {
            SpinAura a = s.Body.GetComponent<SpinAura>();
            if (a == null) a = s.Body.gameObject.AddComponent<SpinAura>();
            a.who = s;
            a.until = Time.time + Seconds;
            a.figure = Mayhem.FigureOf(s);
            s.RushUntil = Mathf.Max(s.RushUntil, a.until);
            Fx.Shock(s.Body.position + Vector3.up, AbilityInfo.Tint(Ability.Toupie), 3f, 0.3f);
            Sfx.WhooshAt(s.Body.position);
        }

        void Update()
        {
            if (who == null || who.Body == null) { Destroy(this); return; }
            bool on = Time.time < until;
            if (figure != null)
            {
                angle = on ? angle + Time.deltaTime * 1080f : Mathf.MoveTowards(angle, Mathf.Ceil(angle / 360f) * 360f, Time.deltaTime * 1080f);
                figure.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
            if (!on)
            {
                if (figure == null || Mathf.Approximately(Mathf.Repeat(angle, 360f), 0f)) { if (figure != null) figure.localRotation = Quaternion.identity; Destroy(this); }
                return;
            }
            if (Random.value < 0.5f) Fx.Sparks(who.Body.position + Vector3.up, AbilityInfo.Tint(Ability.Toupie), 2, 4f);
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == who || s.Body == null) continue;
                Vector3 d = s.Body.position - who.Body.position;
                if (Mathf.Abs(d.y) > 2.4f || new Vector2(d.x, d.z).magnitude > 2.6f) continue;
                float last;
                if (bumped.TryGetValue(s, out last) && Time.time - last < 0.8f) continue;
                bumped[s] = Time.time;
                Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : who.Body.forward;
                Vector3 swirl = new Vector3(-away.z, 0f, away.x);
                Combat.Hit(s, away * 22f + swirl * 8f + Vector3.up * 10f, 0.3f, true, who);
                Sfx.PafAt(s.Body.position);
            }
        }
    }

    /// <summary>LE DELUGE : sept meteores tombent en 2,5 s autour du point vise (une cible au sol les annonce).</summary>
    public class MeteorShower : MonoBehaviour
    {
        const float Warn = 0.6f;
        int Count = 7;
        Seeker by;
        float age;
        Vector3[] spots;
        float[] at;
        bool[] done;
        Transform[] rocks;

        public static void Rain(Seeker by, Vector3 centre) { Rain(by, centre, 7, 7f, 2.3f, false); }

        /// <summary>
        /// (08/10) "count" meteores en "seconds", dans un rayon "spread" ; "aroundOnly" : jamais au
        /// centre (l'APOCALYPSE tombe autour de son lanceur, pas sur lui).
        /// </summary>
        public static void Rain(Seeker by, Vector3 centre, int count, float spread, float seconds, bool aroundOnly)
        {
            GameObject go = new GameObject(count > 10 ? "APOCALYPSE" : "DELUGE");
            go.transform.position = centre;
            MeteorShower m = go.AddComponent<MeteorShower>();
            m.by = by;
            m.Count = count;
            m.spots = new Vector3[count];
            m.at = new float[count];
            m.done = new bool[count];
            m.rocks = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 r = i == 0 && !aroundOnly ? Vector2.zero : Random.insideUnitCircle * spread;
                if (aroundOnly && r.magnitude < 4f) r = (r.sqrMagnitude > 0.01f ? r.normalized : Vector2.right) * (4f + Random.value * spread * 0.5f);
                Vector3 p = centre + new Vector3(r.x, 0f, r.y);
                RaycastHit hit;
                if (Physics.Raycast(p + Vector3.up * 8f, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore)) p = hit.point;
                m.spots[i] = p;
                m.at[i] = 0.3f + i * seconds / count;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Color c = AbilityInfo.Tint(Ability.Deluge);
            bool all = true;
            for (int i = 0; i < Count; i++)
            {
                if (done[i]) continue;
                all = false;
                float left = at[i] + Warn - age;
                if (age < at[i]) continue;
                if (rocks[i] == null)
                {
                    Proto.BeginVisualOnly();
                    GameObject r = Proto.Sphere(null, spots[i] + Vector3.up * 30f, new Vector3(1.3f, 1.3f, 1.3f), new Color(0.35f, 0.2f, 0.12f), "Météore");
                    r.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(new Color(0.9f, 0.4f, 0.15f), 0.3f, 0f, 1.5f);
                    Proto.EndVisualOnly();
                    rocks[i] = r.transform;
                    // (v33) La cible au sol se remplit jusqu'a l'impact.
                    DivineFx.Mark(spots[i], 3f, c, Warn);
                }
                rocks[i].position = spots[i] + Vector3.up * Mathf.Max(0f, 30f * left / Warn);
                if (Random.value < 0.6f) Fx.Sparks(rocks[i].position, c, 2, 3f);
                Fx.Burst(rocks[i].position + Vector3.up, new Color(1f, 0.6f, 0.25f), 2, 2f, 0.7f, 0.4f, -0.2f, Vector3.up, 20f);
                if (left > 0f) continue;
                done[i] = true;
                Destroy(rocks[i].gameObject);
                Vector3 p = spots[i];
                for (int k = 0; k < Game.Seekers.Count; k++)
                {
                    Seeker s = Game.Seekers[k];
                    if (s == by || s.Body == null) continue;
                    Vector3 d = s.Body.position - p;
                    if (Mathf.Abs(d.y) > 3f || new Vector2(d.x, d.z).magnitude > 3f) continue;
                    Vector3 away = Combat.Flat(d).sqrMagnitude > 0.01f ? Combat.Flat(d).normalized : Vector3.forward;
                    Combat.Hit(s, away * 10f + Vector3.up * 15f, 0.4f, true, by);
                }
                DivineFx.Impact(p, 3.5f, c, 0.55f, false);
            }
            if (all) Destroy(gameObject);
        }

        void OnDestroy()
        {
            for (int i = 0; i < Count; i++) if (rocks[i] != null) Destroy(rocks[i].gameObject);
        }
    }

    /// <summary>LE GANT DE BOXE : un poing rouge geant jaillit devant, et revient.</summary>
    public class BoxingGlove : MonoBehaviour
    {
        Vector3 from, dir;
        float age;

        public static void Throw(Vector3 from, Vector3 dir)
        {
            Color red = AbilityInfo.Tint(Ability.Gant);
            Proto.BeginVisualOnly();
            GameObject go = Proto.Sphere(null, from, new Vector3(1.6f, 1.4f, 1.8f), red, "Gant");
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetShiny(red, 0.8f, 0f);
            Proto.Cylinder(go.transform, new Vector3(0f, 0f, -0.6f), new Vector3(0.7f, 0.4f, 0.7f), Color.white, "Manchette")
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Proto.EndVisualOnly();
            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            BoxingGlove g = go.AddComponent<BoxingGlove>();
            g.from = from;
            g.dir = dir;
        }

        void Update()
        {
            age += Time.deltaTime;
            // Aller en 0,12 s, retour en 0,25 s.
            float k = age < 0.12f ? age / 0.12f : 1f - Mathf.Clamp01((age - 0.12f) / 0.25f);
            transform.position = from + dir * 7f * k;
            if (age > 0.37f) Destroy(gameObject);
        }
    }
}
