using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE GARGOUILLE (28/09 -- Martin : "les yeux, il n'y a pas un autre moyen, un
    /// truc un peu plus moyenageux ?"). Rien d'humain, toujours : une bete de pierre
    /// accroupie sur les tours et les remparts, les ailes repliees, des cornes, deux
    /// yeux qui luisent. Sa tete tourne ; elle ne descend jamais de son perchoir. (Dans
    /// le code, la classe s'appelle encore Eye : c'etait des Yeux flottants.)
    ///
    /// Ce qu'elle fait, et ce qu'on voit :
    ///   AMBRE    elle balaie la cour du regard (le cone de lumiere, c'est sa vue) ;
    ///   ORANGE   elle t'a apercu : elle te fixe, ses ailes s'entrouvrent ;
    ///   ROUGE    elle CHARGE : ailes deployees, gueule ouverte et rougeoyante, un trait
    ///            rouge te relie a elle, une CIBLE rouge se resserre a tes pieds. Dans la
    ///            derniere demi-seconde elle ne te suit plus : bouge !
    ///   BLANC    elle crache : un JET DE FEU, une explosion la ou il frappe. Touche,
    ///            tu es projete, etourdi -- et tu lâches la Couronne.
    ///
    /// Elle ne regarde que la citadelle et sa tour -- et le porteur de la Couronne.
    /// La Nuee l'aveugle, le Voile te rend invisible, l'Ombre la ralentit.
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
        float open = 0.2f;
        int shown = -1;

        const float Range = 30f;
        const float Angle = 34f;
        const float ChargeTime = 1.1f;
        const float LockTime = 0.5f;       // la fin de la charge : il ne suit plus
        const float RestTime = 3.4f;       // (2,2 s : sur la rampe, on se faisait mitrailler)

        static readonly Color Calm = new Color(1f, 0.72f, 0.35f);
        static readonly Color Wary = new Color(1f, 0.6f, 0.2f);
        static readonly Color Alarm = new Color(1f, 0.18f, 0.05f);
        static readonly Color Blaze = new Color(1f, 0.9f, 0.6f);
        static readonly Color Flame = new Color(1f, 0.45f, 0.1f);
        readonly List<Renderer> eyes = new List<Renderer>();
        Transform jaw;

        public bool Charging { get { return state == State.Charge; } }
        public Seeker Target { get { return state == State.Charge || state == State.Spot ? target : null; } }

        /// <summary>Vrai si un Oeil est en train de charger sur "s" (l'ecran bat en rouge).</summary>
        public static bool ChargingAt(Seeker s)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] != null && All[i].state == State.Charge && All[i].target == s) return true;
            return false;
        }

        // ================================================================== construction

        /// <summary>
        /// Toutes les gargouilles : sur les quatre tours d'angle (tournees vers la cour),
        /// sur le rempart au-dessus de chaque porte (vers la cour), et six sur des
        /// consoles du fut de la tour, une par tour de rampe, au-dessus du chemin.
        /// </summary>
        public static void PlaceAll(Transform parent)
        {
            GameObject root = new GameObject("LES GARGOUILLES");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            float h = Castle.HalfSize;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            for (int k = 0; k < 4; k++)
                Build(t, corners[k] + Vector3.up * (Castle.TowerHeight + 1.2f), -corners[k], k, false);
            Vector3[] gates = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            for (int k = 0; k < 4; k++)
                Build(t, gates[k] * (h - 1f) + Vector3.up * (Castle.WallHeight + 1.2f), -gates[k], 4 + k, false);
            // Deux par rampe, sur des consoles du fut, au-dessus du chemin (29/09 : quatre rampes).
            for (int r = 0; r < Tower.Ramps; r++)
                for (int k = 0; k < 2; k++)
                {
                    Vector3 p = Tower.RampPoint(r, 0.3f + k * 0.42f + r * 0.03f);
                    Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
                    Build(t, outward * (Tower.Radius + 1.1f) + Vector3.up * (p.y + 7f), outward, 8 + r * 2 + k, true);
                }
        }

        /// <summary>Une gargouille accroupie a "at", tournee vers "facing". "corbel" : posee sur une console du fut.</summary>
        public static Eye Build(Transform parent, Vector3 at, Vector3 facing, float seed, bool corbel)
        {
            GameObject go = new GameObject("GARGOUILLE");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Vector3 f = new Vector3(facing.x, 0f, facing.z);
            go.transform.rotation = Quaternion.LookRotation(f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward, Vector3.up);
            Eye e = go.AddComponent<Eye>();
            e.home = at;
            e.sweepPhase = seed * 1.7f;
            Transform t = go.transform;

            Color stone = new Color(0.45f, 0.43f, 0.4f);
            Color dark = new Color(0.28f, 0.27f, 0.27f);
            Color moss = new Color(0.33f, 0.38f, 0.28f);
            Proto.BeginVisualOnly();
            // Le perchoir : une console de pierre (contre le fut) ou un socle (sur le rempart).
            if (corbel)
            {
                Proto.Cube(t, new Vector3(0f, -0.9f, -0.6f), new Vector3(1.8f, 0.6f, 2.6f), dark, "Console");
                GameObject strut = Proto.Cube(t, new Vector3(0f, -1.9f, -1.2f), new Vector3(1.2f, 1.8f, 0.8f), dark, "Jambage");
                strut.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            }
            else Proto.Cube(t, new Vector3(0f, -0.9f, 0f), new Vector3(2f, 0.6f, 2f), dark, "Socle");
            // Le corps accroupi : le torse penche en avant, les pattes, la queue.
            GameObject torso = Proto.Cube(t, new Vector3(0f, 0.25f, -0.1f), new Vector3(1.3f, 1.2f, 1.7f), stone, "Torse");
            torso.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            Proto.Cube(t, new Vector3(0f, 0.55f, 0.35f), new Vector3(1.05f, 0.9f, 0.9f), moss, "Poitrail");
            for (int side = -1; side <= 1; side += 2)
            {
                Proto.Cube(t, new Vector3(side * 0.55f, -0.35f, -0.55f), new Vector3(0.5f, 0.8f, 0.9f), stone, "Cuisse");
                Proto.Cube(t, new Vector3(side * 0.5f, -0.45f, 0.6f), new Vector3(0.35f, 0.6f, 0.35f), stone, "Patte");
                for (int c = -1; c <= 1; c++)
                    Proto.Cone(t, new Vector3(side * 0.5f + c * 0.1f, -0.7f, 0.82f), 0.06f, 0.2f, dark, "Griffe", 4).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                // Les ailes, repliees dans le dos : elles s'ouvrent quand elle charge.
                Transform hinge = new GameObject("Aile").transform;
                hinge.SetParent(t, false);
                hinge.localPosition = new Vector3(side * 0.45f, 0.75f, -0.45f);
                GameObject bone = Proto.Cube(hinge, new Vector3(side * 0.9f, 0.35f, -0.1f), new Vector3(1.9f, 0.14f, 0.16f), dark, "Os");
                bone.transform.localRotation = Quaternion.Euler(0f, 0f, side * 20f);
                GameObject membrane = Proto.Cube(hinge, new Vector3(side * 0.85f, -0.05f, -0.2f), new Vector3(1.7f, 0.85f, 0.06f), stone, "Membrane");
                membrane.transform.localRotation = Quaternion.Euler(0f, 0f, side * 12f);
                e.petals.Add(hinge);
            }
            GameObject tail = Proto.Cube(t, new Vector3(0f, -0.4f, -1.2f), new Vector3(0.22f, 0.22f, 1.2f), stone, "Queue");
            tail.transform.localRotation = Quaternion.Euler(20f, 15f, 0f);
            Proto.Cone(t, new Vector3(0.18f, -0.62f, -1.8f), 0.18f, 0.35f, dark, "Pointe de queue", 4).transform.localRotation = Quaternion.Euler(-100f, 0f, 0f);

            // La tete (elle tourne) : un crane, un museau, des cornes, deux yeux, une gueule.
            GameObject head = new GameObject("Tête");
            head.transform.SetParent(t, false);
            head.transform.localPosition = new Vector3(0f, 1.05f, 0.55f);
            e.ball = head.transform;
            Proto.Cube(e.ball, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.75f, 0.85f), stone, "Crâne");
            Proto.Cube(e.ball, new Vector3(0f, -0.05f, 0.55f), new Vector3(0.62f, 0.4f, 0.55f), stone, "Museau");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject horn = Proto.Cone(e.ball, new Vector3(side * 0.32f, 0.38f, -0.1f), 0.14f, 0.7f, dark, "Corne", 5);
                horn.transform.localRotation = Quaternion.Euler(-35f, 0f, side * -25f);
                GameObject ear = Proto.Cone(e.ball, new Vector3(side * 0.45f, 0.2f, -0.2f), 0.1f, 0.35f, stone, "Oreille", 4);
                ear.transform.localRotation = Quaternion.Euler(-60f, 0f, side * -70f);
                GameObject eye = Proto.Cube(e.ball, new Vector3(side * 0.2f, 0.16f, 0.43f), new Vector3(0.18f, 0.1f, 0.05f), Color.white, "Œil");
                eye.transform.localRotation = Quaternion.Euler(0f, 0f, side * -15f);
                e.eyes.Add(eye.GetComponent<Renderer>());
                Proto.Cube(e.ball, new Vector3(side * 0.2f, 0.26f, 0.42f), new Vector3(0.26f, 0.08f, 0.1f), dark, "Sourcil").transform.localRotation = Quaternion.Euler(0f, 0f, side * 20f);
            }
            // La gueule : un fond qui rougeoie, une machoire qui s'ouvre, des crocs.
            GameObject maw = Proto.Cube(e.ball, new Vector3(0f, -0.17f, 0.6f), new Vector3(0.45f, 0.14f, 0.42f), Color.white, "Gueule");
            e.iris = maw.GetComponent<Renderer>();
            GameObject j = new GameObject("Mâchoire");
            j.transform.SetParent(e.ball, false);
            j.transform.localPosition = new Vector3(0f, -0.22f, 0.3f);
            e.jaw = j.transform;
            Proto.Cube(e.jaw, new Vector3(0f, -0.06f, 0.28f), new Vector3(0.55f, 0.14f, 0.55f), stone, "Mâchoire");
            for (int k = -1; k <= 1; k += 2)
                Proto.Cone(e.jaw, new Vector3(k * 0.18f, 0.02f, 0.5f), 0.05f, 0.16f, Color.white, "Croc", 4);
            GameObject pupilGo = new GameObject("Braise");
            pupilGo.transform.SetParent(e.ball, false);
            e.pupil = pupilGo.transform;
            Proto.EndVisualOnly();
            // (Plus d'anneaux ni d'eclats : ce qui tourne, c'est sa tete.)
            e.ringA = new GameObject("-").transform;
            e.ringA.SetParent(t, false);
            e.ringB = new GameObject("-").transform;
            e.ringB.SetParent(t, false);
            Renderer[] parts = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < parts.Length; i++) parts[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject coneGo = new GameObject("Regard");
            coneGo.transform.SetParent(e.ball, false);
            coneGo.transform.localPosition = new Vector3(0f, 0.1f, 0.6f);
            e.cone = coneGo.AddComponent<Light>();
            e.cone.type = LightType.Spot;
            e.cone.spotAngle = Angle * 2f;
            e.cone.range = Range;
            e.cone.intensity = 3f;
            e.cone.color = Calm;
            e.cone.shadows = LightShadows.None;

            e.beam = Line(go.transform, "Jet de feu", MaterialFactory.GetGlow(Flame, 3f), 2);
            e.beamCore = Line(go.transform, "Coeur du jet", MaterialFactory.GetGlow(Blaze, 6f), 2);
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
            // Elle respire, a peine.
            transform.position = home + Vector3.up * Mathf.Sin(Time.time * 0.9f + sweepPhase) * 0.03f;
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
            Vector3 from = ball.position + ball.forward * 0.9f - ball.up * 0.15f;
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
            // Le jet de feu : des gerbes de flammes tout le long.
            float length = (end - from).magnitude;
            for (int k = 1; k <= 6; k++)
            {
                Vector3 at = from + dir * (length * k / 7f);
                Fx.Burst(at, k % 2 == 0 ? Flame : Alarm, 14, 3f, 0.5f, 0.5f, -0.2f, Vector3.zero, 0f);
            }
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
            // Les ailes : repliees dans le dos au calme, deployees quand elle charge (et qui battent).
            float flap = state == State.Charge ? Mathf.Sin(Time.time * 14f) * 8f : 0f;
            for (int i = 0; i < petals.Count; i++)
            {
                float side = i == 0 ? -1f : 1f;
                petals[i].localRotation = Quaternion.Euler(Mathf.Lerp(-10f, 10f, open), side * Mathf.Lerp(70f, 5f, open), side * (Mathf.Lerp(-35f, 15f, open) + flap));
            }
            // La gueule s'ouvre quand elle charge.
            if (jaw != null) jaw.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 32f, open * open), 0f, 0f);
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
            iris.sharedMaterial = MaterialFactory.GetGlow(mood >= 2 ? Flame : new Color(0.25f, 0.08f, 0.04f), mood == 3 ? 6f : mood == 2 ? 3.5f : 1f);
            Material eyeGlow = MaterialFactory.GetGlow(c, mood >= 2 ? 5f : 2.6f);
            for (int i = 0; i < eyes.Count; i++) eyes[i].sharedMaterial = eyeGlow;
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
