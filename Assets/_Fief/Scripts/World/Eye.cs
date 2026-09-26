using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UN OEIL DE LA CITADELLE (27/09 -- Martin : "les PNJ, soit un truc tellement
    /// excellent, soit rien"). Donc RIEN d'humain : plus de gardes, plus de roi. A la
    /// place, des sentinelles de pierre qui FLOTTENT (redessinees le 28/09) : un coeur
    /// de lumiere pris dans HUIT PETALES de pierre -- un diaphragme qui s'ouvre quand il
    /// te voit --, une pupille de chat qui s'arrondit quand il charge, trois eclats de
    /// rune qui tournent autour, deux anneaux. Une forme qu'on lit a cinquante metres,
    /// et qui ne se coince jamais dans un mur puisqu'elle ne marche pas.
    ///
    /// Ce qu'il fait, et ce qu'on voit :
    ///   BLEU     il balaie la cour de son regard (le cone de lumiere, c'est sa vue) ;
    ///   ORANGE   il t'a apercu : il te fixe ;
    ///   ROUGE    il CHARGE : ses petales s'ouvrent grand, un trait rouge le relie a toi
    ///            et s'epaissit, une CIBLE rouge se resserre a tes pieds. Dans la
    ///            derniere demi-seconde il ne te suit plus : bouge !
    ///   BLANC    il tire : un rayon blanc cercle de rouge, une explosion la ou il
    ///            frappe. Touche, tu es projete, etourdi -- et tu lâches la Couronne.
    ///
    /// Il ne regarde que la citadelle et sa tour -- et le porteur de la Couronne.
    /// La Nuee l'aveugle, le Voile te rend invisible, l'Ombre le ralentit.
    /// </summary>
    public class Eye : MonoBehaviour
    {
        public static readonly List<Eye> All = new List<Eye>();

        enum State { Watch, Spot, Charge, Rest }

        State state = State.Watch;
        Seeker target;
        float timer;
        float suspicion;
        Vector3 aim;
        Vector3 home;
        float sweepPhase;
        Transform ball, ringA, ringB, pupil;
        Renderer iris;
        Light cone;
        LineRenderer beam, beamCore, reticle;
        readonly List<Transform> petals = new List<Transform>();
        readonly List<Transform> shards = new List<Transform>();
        float open = 0.2f;
        int shown = -1;

        const float Range = 30f;
        const float Angle = 34f;
        const float ChargeTime = 1.1f;
        const float LockTime = 0.5f;       // la fin de la charge : il ne suit plus
        const float RestTime = 3.4f;       // (2,2 s : sur la rampe, on se faisait mitrailler)

        static readonly Color Calm = new Color(0.45f, 0.7f, 1f);
        static readonly Color Wary = new Color(1f, 0.6f, 0.2f);
        static readonly Color Alarm = new Color(1f, 0.12f, 0.08f);
        static readonly Color Blaze = new Color(1f, 0.92f, 0.85f);

        public bool Charging { get { return state == State.Charge; } }
        public Seeker Target { get { return state == State.Charge || state == State.Spot ? target : null; } }

        /// <summary>Vrai si un Oeil est en train de charger sur "s" (l'ecran bat en rouge).</summary>
        public static bool ChargingAt(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].state == State.Charge && All[i].target == s) return true;
            return false;
        }

        // ================================================================== construction

        /// <summary>Tous les Yeux de la citadelle : les tours d'angle, les portes, la tour de la Couronne.</summary>
        public static void PlaceAll(Transform parent)
        {
            GameObject root = new GameObject("LES YEUX");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            float h = Castle.HalfSize;
            // Au-dessus des quatre tours d'angle.
            Build(t, new Vector3(-h, Castle.TowerHeight + 8f, h), 0f);
            Build(t, new Vector3(h, Castle.TowerHeight + 8f, h), 1f);
            Build(t, new Vector3(h, Castle.TowerHeight + 8f, -h), 2f);
            Build(t, new Vector3(-h, Castle.TowerHeight + 8f, -h), 3f);
            // Au-dessus de chaque porte, cote cour.
            Build(t, new Vector3(0f, Castle.WallHeight + 5f, h - 6f), 4f);
            Build(t, new Vector3(0f, Castle.WallHeight + 5f, -h + 6f), 5f);
            Build(t, new Vector3(h - 6f, Castle.WallHeight + 5f, 0f), 6f);
            Build(t, new Vector3(-h + 6f, Castle.WallHeight + 5f, 0f), 7f);
            // Autour de la tour, un par tour de rampe, qui flottent dans le vide.
            for (int k = 0; k < Tower.Turns; k++)
            {
                Vector3 p = Tower.RampPoint((k + 0.62f) / Tower.Turns);
                Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
                Build(t, p + outward * 9f + Vector3.up * 5f, 8f + k);
            }
        }

        public static Eye Build(Transform parent, Vector3 at, float seed)
        {
            GameObject go = new GameObject("OEIL");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Eye e = go.AddComponent<Eye>();
            e.home = at;
            e.sweepPhase = seed * 1.7f;

            Color stone = new Color(0.4f, 0.38f, 0.36f);
            Color stoneDark = new Color(0.22f, 0.21f, 0.22f);
            Color gold = new Color(0.78f, 0.6f, 0.3f);
            Proto.BeginVisualOnly();
            GameObject b = new GameObject("Globe");
            b.transform.SetParent(go.transform, false);
            e.ball = b.transform;
            // La coque de pierre, derriere ; le coeur de lumiere devant ; la pupille.
            Proto.Sphere(e.ball, new Vector3(0f, 0f, -0.3f), new Vector3(1.7f, 1.7f, 1.5f), stone, "Coque");
            Proto.Cylinder(e.ball, new Vector3(0f, 0f, 0.1f), new Vector3(1.75f, 0.08f, 1.75f), gold, "Cerclage d'or").transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject core = Proto.Sphere(e.ball, new Vector3(0f, 0f, 0.35f), new Vector3(1.05f, 1.05f, 0.8f), Color.white, "Coeur");
            e.iris = core.GetComponent<Renderer>();
            GameObject slit = Proto.Cube(e.ball, new Vector3(0f, 0f, 0.76f), new Vector3(0.14f, 0.7f, 0.05f), new Color(0.03f, 0.03f, 0.04f), "Pupille");
            e.pupil = slit.transform;
            // Les huit petales : un diaphragme de pierre autour du coeur.
            for (int i = 0; i < 8; i++)
            {
                Transform pivot = new GameObject("Pétale").transform;
                pivot.SetParent(e.ball, false);
                pivot.localPosition = new Vector3(0f, 0f, 0.25f);
                pivot.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                Transform hinge = new GameObject("Charnière").transform;
                hinge.SetParent(pivot, false);
                hinge.localPosition = new Vector3(0f, 0.7f, 0f);
                GameObject plate = Proto.Cube(hinge, new Vector3(0f, 0.42f, 0f), new Vector3(0.5f, 0.85f, 0.1f), i % 2 == 0 ? stone : stoneDark, "Plaque");
                GameObject inlay = Proto.Cube(plate.transform, new Vector3(0f, 0.1f, 0.6f), new Vector3(0.25f, 0.5f, 0.2f), Color.white, "Rune");
                inlay.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Calm, 1.6f);
                e.petals.Add(hinge);
            }
            // Deux anneaux qui tournent.
            GameObject ra = new GameObject("Anneau");
            ra.transform.SetParent(go.transform, false);
            e.ringA = ra.transform;
            Ring(e.ringA, 1.75f, gold);
            GameObject rb = new GameObject("Anneau");
            rb.transform.SetParent(go.transform, false);
            e.ringB = rb.transform;
            Ring(e.ringB, 1.45f, stoneDark);
            // Trois eclats de rune en orbite.
            for (int i = 0; i < 3; i++)
            {
                GameObject shard = Proto.Cube(go.transform, Vector3.zero, new Vector3(0.18f, 0.42f, 0.18f), Color.white, "Éclat");
                shard.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Calm, 2.4f);
                e.shards.Add(shard.transform);
            }
            Proto.EndVisualOnly();
            Renderer[] parts = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < parts.Length; i++) parts[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject coneGo = new GameObject("Regard");
            coneGo.transform.SetParent(e.ball, false);
            coneGo.transform.localPosition = new Vector3(0f, 0f, 1f);
            e.cone = coneGo.AddComponent<Light>();
            e.cone.type = LightType.Spot;
            e.cone.spotAngle = Angle * 2f;
            e.cone.range = Range;
            e.cone.intensity = 3f;
            e.cone.color = Calm;
            e.cone.shadows = LightShadows.None;

            e.beam = Line(go.transform, "Rayon", MaterialFactory.GetGlow(Alarm, 3f), 2);
            e.beamCore = Line(go.transform, "Coeur du rayon", MaterialFactory.GetGlow(Blaze, 6f), 2);
            e.reticle = Line(go.transform, "Cible", Ambiance.Additive, 32);
            e.reticle.loop = true;
            e.reticle.startColor = new Color(1f, 0.15f, 0.1f, 0.9f);
            e.reticle.endColor = new Color(1f, 0.4f, 0.2f, 0.9f);

            All.Add(e);
            return e;
        }

        static LineRenderer Line(Transform parent, string name, Material m, int points)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.positionCount = points;
            l.useWorldSpace = true;
            l.sharedMaterial = m;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.enabled = false;
            return l;
        }

        /// <summary>Un anneau : seize petits blocs sur un cercle.</summary>
        static void Ring(Transform t, float radius, Color c)
        {
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                GameObject bit = Proto.Cube(t, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius, new Vector3(0.12f, 0.1f, radius * 0.42f), c, "Maillon");
                bit.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        void OnDestroy() { All.Remove(this); }

        // ================================================================== la vie

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            // Il flotte, ses anneaux tournent.
            transform.position = home + Vector3.up * Mathf.Sin(Time.time * 0.9f + sweepPhase) * 0.35f;
            ringA.localRotation = Quaternion.Euler(Time.time * 40f + sweepPhase * 30f, Time.time * 25f, 20f);
            ringB.localRotation = Quaternion.Euler(-Time.time * 30f, 60f, Time.time * 45f + sweepPhase * 10f);
            Animate(dt);
            ShowMood();

            Season season = Game.Season;
            if (season == null || !season.Running) { Sweep(dt); return; }

            switch (state)
            {
                case State.Watch: Watch(dt); break;
                case State.Spot: Spot(dt); break;
                case State.Charge: Charge(dt); break;
                case State.Rest:
                    timer -= dt;
                    Sweep(dt);
                    if (timer <= 0f) state = State.Watch;
                    break;
            }
        }

        /// <summary>Le balayage lent : il regarde a gauche, a droite, en bas.</summary>
        void Sweep(float dt)
        {
            float t = Time.time * 0.35f + sweepPhase;
            Vector3 dir = Quaternion.Euler(28f + Mathf.Sin(t * 1.7f) * 10f, t * 60f, 0f) * Vector3.forward;
            ball.rotation = Quaternion.Slerp(ball.rotation, Quaternion.LookRotation(dir), dt * 2f);
        }

        void Watch(float dt)
        {
            Sweep(dt);
            Seeker seen = null;
            float best = float.MaxValue;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                float d;
                if (!Interested(s) || !Sees(s, true, out d)) continue;
                if (d < best) { best = d; seen = s; }
            }
            if (seen == null) { suspicion = Mathf.Max(0f, suspicion - dt); return; }
            target = seen;
            state = State.Spot;
            timer = 0f;
        }

        /// <summary>Il fixe : l'iris orange. S'il te garde en vue assez longtemps, il charge.</summary>
        void Spot(float dt)
        {
            float d;
            if (target == null || !Interested(target) || !Sees(target, false, out d)) { state = State.Watch; return; }
            Look(target.Body.position + Vector3.up * 1.1f, dt * 6f);
            suspicion += dt * (target.Has(Ability.Ombre) ? 0.5f : 1f) * (target.CarriesCrown ? 1.6f : 1f);
            if (suspicion < 0.7f) return;
            suspicion = 0f;
            state = State.Charge;
            timer = 0f;
            if (target.IsPlayer || NearPlayer(35f)) Sfx.Alarm();
        }

        /// <summary>
        /// LA CHARGE : un trait rouge le relie a sa cible et s'epaissit. Il la suit --
        /// sauf la derniere demi-seconde : c'est la qu'on esquive. Puis il tire.
        /// </summary>
        void Charge(float dt)
        {
            timer += dt;
            if (target == null || target.Body == null || target.Hidden || Smoke.Inside(target.Body.position)) { Cancel(); return; }
            Vector3 chest = target.Body.position + Vector3.up * 1.1f;
            if (timer < ChargeTime - LockTime) aim = chest;
            Look(aim, dt * 10f);
            Vector3 from = ball.position + ball.forward * 1f;
            beam.enabled = true;
            float k = timer / ChargeTime;
            beam.startWidth = Mathf.Lerp(0.02f, 0.14f, k);
            beam.endWidth = beam.startWidth * 0.6f;
            beam.SetPosition(0, from);
            beam.SetPosition(1, from + (aim - from).normalized * Range * 1.2f);
            // La CIBLE a ses pieds : un cercle rouge qui se resserre et tourne.
            reticle.enabled = true;
            reticle.widthMultiplier = Mathf.Lerp(0.08f, 0.2f, k);
            float r = Mathf.Lerp(3.2f, 0.7f, k);
            Vector3 feet = target.Body.position + Vector3.up * 0.15f;
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f + Time.time * 4f;
                float notch = i % 8 < 2 ? 0.75f : 1f;
                reticle.SetPosition(i, feet + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * notch);
            }
            if (timer < ChargeTime) return;
            reticle.enabled = false;
            Fire(from);
        }

        void Fire(Vector3 from)
        {
            Vector3 dir = (aim - from).normalized;
            RaycastHit wall;
            float reach = Range * 1.2f;
            if (Physics.Raycast(from, dir, out wall, reach, ~0, QueryTriggerInteraction.Ignore)) reach = wall.distance + 0.5f;
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 c = s.Body.position + Vector3.up * 1.1f;
                float along = Vector3.Dot(c - from, dir);
                if (along < 0f || along > reach) continue;
                if ((from + dir * along - c).magnitude > 0.9f) continue;
                Vector3 push = new Vector3(dir.x, 0f, dir.z).normalized;
                Combat.Hit(s, push * 15f + Vector3.up * 5f, 0.7f, true, null);
                Fx.Impact(c, Blaze, 1.3f);
                if (s.IsPlayer) Stats.EyeHits++;
            }
            // L'EXPLOSION la ou il frappe : une sphere, un anneau, une gerbe, un eclair.
            Vector3 end = from + dir * Mathf.Min(reach, 40f);
            Fx.Shock(end, Alarm, 2.6f, 0.3f);
            Fx.Shock(end, Blaze, 1.2f, 0.2f);
            Fx.Burst(end, Alarm, 70, 12f, 0.2f, 0.6f, 0.4f, -dir, 60f);
            Fx.Burst(end, Blaze, 30, 6f, 0.3f, 0.4f, 0f, Vector3.zero, 0f);
            Fx.Ring(end, Alarm, 0.3f, 3.5f, 0.35f, 0.2f, -dir);
            Fx.Flash(end, Alarm, 14f, 6f, 0.3f);
            // Et a la bouche : un eclair, un anneau.
            Fx.Flash(from, Blaze, 14f, 6f, 0.25f);
            Fx.Ring(from, Blaze, 0.3f, 2.5f, 0.3f, 0.2f, dir);
            beamCore.enabled = true;
            beamCore.SetPosition(0, from);
            beamCore.SetPosition(1, end);
            beam.SetPosition(1, end);
            Sfx.Thud();
            if (NearPlayer(40f) && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.12f);
            beam.startWidth = 0.35f;
            beam.endWidth = 0.2f;
            state = State.Rest;
            timer = RestTime;
            firedAt = Time.time;
        }

        float firedAt = -9f;

        void Cancel()
        {
            state = State.Rest;
            timer = 1f;
            beam.enabled = false;
            reticle.enabled = false;
        }

        /// <summary>
        /// La vie du corps : les petales s'ouvrent quand il te voit (grand ouverts quand il
        /// charge), la pupille s'arrondit, les eclats tournent plus vite, le rayon s'eteint.
        /// </summary>
        void Animate(float dt)
        {
            float want = state == State.Charge ? 1f : state == State.Spot ? 0.6f : 0.15f;
            open = Mathf.MoveTowards(open, want, dt * (want > open ? 3f : 1.2f));
            // Ferme : les plaques se penchent devant le coeur ; ouvert : elles s'ecartent.
            float angle = Mathf.Lerp(112f, 18f, open) + (state == State.Charge ? Mathf.Sin(Time.time * 40f) * 3f : 0f);
            for (int i = 0; i < petals.Count; i++) petals[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
            // La pupille de chat : une fente au calme, ronde et large quand il charge.
            pupil.localScale = new Vector3(Mathf.Lerp(0.14f, 0.5f, open), Mathf.Lerp(0.7f, 0.5f, open), 0.05f);
            float speed = 1.2f + open * 4f;
            for (int i = 0; i < shards.Count; i++)
            {
                float a = Time.time * speed + i * 2.094f + sweepPhase;
                float r = Mathf.Lerp(2.1f, 1.5f, open);
                shards[i].localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a * 0.7f) * 0.6f, Mathf.Sin(a) * r);
                shards[i].localRotation = Quaternion.Euler(0f, a * 90f, 45f);
            }
            // Le rayon : il s'amincit et s'eteint en un quart de seconde.
            float since = Time.time - firedAt;
            if (since < 0.3f)
            {
                float k = 1f - since / 0.3f;
                beam.enabled = true;
                beam.startWidth = 0.9f * k;
                beam.endWidth = 0.6f * k;
                beamCore.startWidth = 0.35f * k;
                beamCore.endWidth = 0.25f * k;
            }
            else if (beamCore.enabled) beamCore.enabled = false;
        }

        void Look(Vector3 at, float speed)
        {
            Vector3 d = at - ball.position;
            if (d.sqrMagnitude < 0.01f) return;
            ball.rotation = Quaternion.Slerp(ball.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(speed));
        }

        /// <summary>Qui l'interesse : quiconque est dans la citadelle, et le porteur de la Couronne jusqu'a 40 m.</summary>
        bool Interested(Seeker s)
        {
            if (s == null || s.Body == null || s.Hidden || s.Graced) return false;
            Vector3 p = s.Body.position;
            if (s.CarriesCrown) return (p - transform.position).magnitude < 45f;
            return Castle.Inside(p);
        }

        /// <summary>Dans son cone (ou tout pres), a portee, sans mur ni fumee entre.</summary>
        bool Sees(Seeker s, bool cone, out float distance)
        {
            Vector3 eye = ball.position;
            Vector3 to = s.Body.position + Vector3.up * 1.1f - eye;
            distance = to.magnitude;
            float range = Range * (s.Has(Ability.Ombre) ? 0.7f : 1f);
            if (distance > range) return false;
            if (cone && distance > 6f && Vector3.Angle(ball.forward, to) > Angle) return false;
            if (Smoke.Blocks(eye, eye + to)) return false;
            RaycastHit hit;
            if (Physics.Raycast(eye, to / distance, out hit, distance - 0.6f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(s.Body)) return false;
            return true;
        }

        void ShowMood()
        {
            int mood = state == State.Rest && Time.time - firedAt < 0.15f ? 3 : state == State.Charge ? 2 : state == State.Spot ? 1 : 0;
            if (beam.enabled && state != State.Charge && Time.time - firedAt > 0.3f) beam.enabled = false;
            if (mood == shown) return;
            shown = mood;
            Color c = mood == 3 ? Blaze : mood == 2 ? Alarm : mood == 1 ? Wary : Calm;
            iris.sharedMaterial = MaterialFactory.GetGlow(c, mood == 3 ? 6f : 3f);
            Material shardGlow = MaterialFactory.GetGlow(c, 2.4f);
            for (int i = 0; i < shards.Count; i++) shards[i].GetComponent<Renderer>().sharedMaterial = shardGlow;
            cone.color = c;
            cone.intensity = mood >= 2 ? 5f : 3f;
        }

        bool NearPlayer(float metres)
        {
            Transform p = Game.PlayerTransform;
            return p != null && (p.position - transform.position).magnitude < metres;
        }
    }
}
