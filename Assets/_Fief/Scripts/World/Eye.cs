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

        // 29/09 (Martin : "l'oeil c'est beaucoup trop facile ; s'il me vise, ca fait un
        // BOUM et ca me fait redescendre ; hyper complique, mais pas trop") : elles voient
        // plus loin, chargent plus vite, et leur tir EXPLOSE et projette hors de la rampe.
        const float Range = 42f;
        const float Angle = 50f;
        const float ChargeTime = 0.95f;
        const float LockTime = 0.4f;       // la fin de la charge : elle ne suit plus
        const float RestTime = 2.4f;
        /// <summary>Le rayon de l'explosion du jet de feu (on est projete meme sans etre touche en plein).</summary>
        public const float BlastRadius = 3.6f;

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
            // (01/10) Sur le flanc de chaque tour d'angle, cote cour, sur une console : avant,
            // elles etaient posees au sommet... sous le toit pointu, cachees dedans.
            for (int k = 0; k < 4; k++)
            {
                Vector3 inward = -corners[k].normalized;
                Build(t, corners[k] + inward * (Castle.TowerSize * 0.5f + 1.1f) + Vector3.up * (Castle.TowerHeight - 4.5f), inward, k, true);
            }
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

            // (01/10 -- "les gargouilles, elles sont moches") : refaite en formes RONDES --
            // spheres et gelules, plus un seul cube -- dans une pierre claire polie qui accroche
            // la lumiere : un corps de lion accroupi, des ailes de chauve-souris a trois
            // doigts, des cornes qui s'enroulent, une queue en crochet, des griffes noires.
            Material stone = MaterialFactory.GetShiny(new Color(0.66f, 0.63f, 0.6f), 0.28f, 0f);
            Material shade = MaterialFactory.GetShiny(new Color(0.5f, 0.48f, 0.47f), 0.25f, 0f);
            Material moss = MaterialFactory.GetShiny(new Color(0.46f, 0.55f, 0.4f), 0.18f, 0f);
            Material horn = MaterialFactory.GetShiny(new Color(0.16f, 0.15f, 0.17f), 0.65f, 0.3f);
            Material wing = MaterialFactory.GetShiny(new Color(0.55f, 0.52f, 0.52f), 0.22f, 0f);
            Material fangs = MaterialFactory.GetShiny(new Color(0.95f, 0.92f, 0.84f), 0.6f, 0f);
            Proto.BeginVisualOnly();
            // Le perchoir : une console arrondie (contre le fut) ou un socle rond (sur le rempart).
            if (corbel)
            {
                Paint(Proto.Capsule(t, new Vector3(0f, -0.95f, -0.7f), new Vector3(1.9f, 1.5f, 1.2f), Color.white, "Console"), shade).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Paint(Proto.Sphere(t, new Vector3(0f, -1.8f, -1.3f), new Vector3(1.2f, 1.6f, 1.0f), Color.white, "Jambage"), shade);
            }
            else
            {
                Paint(Proto.Cylinder(t, new Vector3(0f, -0.95f, 0f), new Vector3(2.1f, 0.3f, 2.1f), Color.white, "Socle"), shade);
                Paint(Proto.Cylinder(t, new Vector3(0f, -0.66f, 0f), new Vector3(2.2f, 0.04f, 2.2f), Color.white, "Liseré"), horn);
            }
            // Le corps : un torse en poire penche en avant, un poitrail moussu, deux cuisses.
            Paint(Proto.Sphere(t, new Vector3(0f, 0.3f, -0.15f), new Vector3(1.3f, 1.3f, 1.75f), Color.white, "Torse"), stone).transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            Paint(Proto.Sphere(t, new Vector3(0f, 0.62f, 0.32f), new Vector3(1.05f, 1.05f, 0.9f), Color.white, "Poitrail"), moss);
            for (int side = -1; side <= 1; side += 2)
            {
                Paint(Proto.Sphere(t, new Vector3(side * 0.5f, -0.2f, -0.5f), new Vector3(0.72f, 0.85f, 1.05f), Color.white, "Cuisse"), stone);
                // La patte avant : une gelule jusqu'a la main, trois griffes.
                Seg(t, new Vector3(side * 0.42f, 0.45f, 0.42f), new Vector3(side * 0.45f, -0.5f, 0.7f), 0.3f, stone);
                Paint(Proto.Sphere(t, new Vector3(side * 0.45f, -0.6f, 0.78f), new Vector3(0.4f, 0.22f, 0.46f), Color.white, "Main"), stone);
                for (int c = -1; c <= 1; c++)
                    Seg(t, new Vector3(side * 0.45f + c * 0.11f, -0.62f, 0.92f), new Vector3(side * 0.45f + c * 0.13f, -0.7f, 1.08f), 0.07f, horn);
                // Le pied arriere.
                Paint(Proto.Sphere(t, new Vector3(side * 0.6f, -0.62f, -0.2f), new Vector3(0.42f, 0.24f, 0.62f), Color.white, "Pied"), stone);
                // L'AILE, repliee dans le dos : trois doigts d'os et des voiles entre eux. Elle
                // s'ouvre quand elle charge (la charniere tourne, voir Animate).
                Transform hinge = new GameObject("Aile").transform;
                hinge.SetParent(t, false);
                hinge.localPosition = new Vector3(side * 0.45f, 0.85f, -0.45f);
                Vector3 elbow = new Vector3(side * 0.8f, 0.55f, -0.1f);
                Seg(hinge, Vector3.zero, elbow, 0.16f, shade);
                Vector3[] tips = { new Vector3(side * 2.2f, 0.9f, -0.2f), new Vector3(side * 2.0f, 0.1f, -0.25f), new Vector3(side * 1.4f, -0.55f, -0.25f) };
                for (int k = 0; k < tips.Length; k++)
                {
                    Seg(hinge, elbow, tips[k], 0.09f - k * 0.015f, shade);
                    Paint(Proto.Sphere(hinge, tips[k], Vector3.one * 0.1f, Color.white, "Ergot"), horn);
                }
                for (int k = 0; k < 3; k++)
                {
                    Vector3 a = k == 0 ? Vector3.zero : tips[k - 1];
                    Vector3 b = tips[k];
                    Vector3 mid = (elbow + a + b) / 3f;
                    GameObject sail = Paint(Proto.Sphere(hinge, mid, new Vector3((b - a).magnitude * 0.9f + 0.5f, 0.9f, 0.05f), Color.white, "Voile"), wing);
                    sail.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg + (side > 0 ? 0f : 180f));
                }
                e.petals.Add(hinge);
            }
            // L'echine : quatre petites pointes noires le long du dos.
            for (int k = 0; k < 4; k++)
                Proto.Cone(t, new Vector3(0f, 0.95f - k * 0.18f, -0.25f - k * 0.3f), 0.09f, 0.26f, Color.white, "Pointe", 6).GetComponent<Renderer>().sharedMaterial = horn;
            // La queue : une gelule qui s'enroule, et sa pointe en fer de fleche.
            Vector3[] tail = { new Vector3(0f, -0.35f, -1.05f), new Vector3(0.3f, -0.55f, -1.55f), new Vector3(0.75f, -0.6f, -1.75f), new Vector3(1.05f, -0.42f, -1.6f) };
            for (int k = 0; k < tail.Length - 1; k++) Seg(t, tail[k], tail[k + 1], 0.24f - k * 0.05f, stone);
            GameObject barb = Paint(Proto.Sphere(t, tail[3] + new Vector3(0.1f, 0.05f, 0.05f), new Vector3(0.3f, 0.12f, 0.2f), Color.white, "Pointe de queue"), horn);
            barb.transform.localRotation = Quaternion.Euler(0f, 30f, 45f);

            // LA TETE (elle tourne) : un crane rond, un museau, un front plisse, deux yeux qui
            // luisent, des cornes qui s'enroulent vers l'arriere, une gueule qui rougeoie.
            GameObject head = new GameObject("Tête");
            head.transform.SetParent(t, false);
            head.transform.localPosition = new Vector3(0f, 1.1f, 0.6f);
            e.ball = head.transform;
            Paint(Proto.Sphere(e.ball, new Vector3(0f, 0.06f, 0f), new Vector3(0.88f, 0.78f, 0.86f), Color.white, "Crâne"), stone);
            Paint(Proto.Sphere(e.ball, new Vector3(0f, -0.06f, 0.45f), new Vector3(0.62f, 0.46f, 0.66f), Color.white, "Museau"), stone);
            Seg(e.ball, new Vector3(-0.28f, 0.24f, 0.36f), new Vector3(0.28f, 0.24f, 0.36f), 0.14f, shade);
            for (int side = -1; side <= 1; side += 2)
            {
                // Les cornes : quatre bouts de gelule qui montent, reculent, s'enroulent.
                Vector3[] h = { new Vector3(side * 0.26f, 0.3f, -0.02f), new Vector3(side * 0.42f, 0.6f, -0.18f), new Vector3(side * 0.5f, 0.8f, -0.45f), new Vector3(side * 0.44f, 0.86f, -0.72f), new Vector3(side * 0.36f, 0.74f, -0.86f) };
                for (int k = 0; k < h.Length - 1; k++) Seg(e.ball, h[k], h[k + 1], 0.17f - k * 0.03f, horn);
                GameObject ear = Paint(Proto.Sphere(e.ball, new Vector3(side * 0.44f, 0.16f, -0.12f), new Vector3(0.1f, 0.34f, 0.2f), Color.white, "Oreille"), stone);
                ear.transform.localRotation = Quaternion.Euler(-20f, 0f, side * -55f);
                GameObject eye = Proto.Sphere(e.ball, new Vector3(side * 0.19f, 0.15f, 0.39f), new Vector3(0.17f, 0.11f, 0.08f), Color.white, "Œil");
                eye.transform.localRotation = Quaternion.Euler(0f, 0f, side * -15f);
                e.eyes.Add(eye.GetComponent<Renderer>());
                Paint(Proto.Sphere(e.ball, new Vector3(side * 0.09f, -0.02f, 0.78f), new Vector3(0.07f, 0.05f, 0.04f), Color.white, "Naseau"), horn);
                // Les crocs du haut, qui depassent.
                GameObject fang = Proto.Cone(e.ball, new Vector3(side * 0.17f, -0.2f, 0.66f), 0.045f, 0.16f, Color.white, "Croc", 6);
                fang.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                fang.GetComponent<Renderer>().sharedMaterial = fangs;
            }
            // La gueule : un fond qui rougeoie, une machoire qui s'ouvre, ses crocs.
            GameObject maw = Proto.Sphere(e.ball, new Vector3(0f, -0.2f, 0.52f), new Vector3(0.44f, 0.16f, 0.46f), Color.white, "Gueule");
            e.iris = maw.GetComponent<Renderer>();
            GameObject j = new GameObject("Mâchoire");
            j.transform.SetParent(e.ball, false);
            j.transform.localPosition = new Vector3(0f, -0.24f, 0.3f);
            e.jaw = j.transform;
            Paint(Proto.Sphere(e.jaw, new Vector3(0f, -0.05f, 0.28f), new Vector3(0.56f, 0.18f, 0.62f), Color.white, "Mâchoire"), stone);
            for (int k = -1; k <= 1; k += 2)
                Proto.Cone(e.jaw, new Vector3(k * 0.16f, 0.02f, 0.5f), 0.045f, 0.15f, Color.white, "Croc", 6).GetComponent<Renderer>().sharedMaterial = fangs;
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

        static GameObject Paint(GameObject go, Material m)
        {
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>Une gelule de "a" a "b" (un os, une corne, une patte), d'epaisseur "thick".</summary>
        static void Seg(Transform t, Vector3 a, Vector3 b, float thick, Material m)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            GameObject g = Paint(Proto.Capsule(t, (a + b) * 0.5f, new Vector3(thick, (len + thick) * 0.5f, thick), Color.white, "Os"), m);
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d / len);
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
            if (suspicion < 0.35f) return;
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
            // (v13) Une cible devenue protegee (respawn) : la charge s'eteint, pas de rayon pour rien.
            if (target == null || target.Body == null || target.Hidden || target.Graced || Smoke.Inside(target.Body.position)) { Cancel(); return; }
            Vector3 chest = target.Body.position + Vector3.up * 1.1f;
            if (timer < ChargeTime / Mathf.Sqrt(Tower.Hardness) - LockTime) aim = chest;
            Look(aim, dt * 10f);
            Vector3 from = ball.position + ball.forward * 0.9f - ball.up * 0.15f;
            beam.enabled = true;
            float k = Mathf.Clamp01(timer / (ChargeTime / Mathf.Sqrt(Tower.Hardness)));
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
            if (timer < ChargeTime / Mathf.Sqrt(Tower.Hardness)) return;
            reticle.enabled = false;
            Fire(from);
        }

        void Fire(Vector3 from)
        {
            Vector3 dir = (aim - from).normalized;
            RaycastHit wall;
            float reach = Range * 1.2f;
            if (Physics.Raycast(from, dir, out wall, reach, ~0, QueryTriggerInteraction.Ignore)) reach = wall.distance + 0.5f;
            Vector3 end = from + dir * Mathf.Min(reach, Range * 1.2f);
            // BOUM : le jet touche en plein, ou l'explosion a son bout -- et on est projete
            // HORS de la rampe (vers le vide) : on redescend.
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s.Body == null) continue;
                Vector3 c = s.Body.position + Vector3.up * 1.1f;
                float along = Vector3.Dot(c - from, dir);
                bool inBeam = along >= 0f && along <= reach && (from + dir * along - c).magnitude <= 1.1f;
                bool inBlast = (c - end).magnitude <= BlastRadius;
                if (!inBeam && !inBlast) continue;
                Vector3 push = new Vector3(c.x, 0f, c.z);
                // Sur la tour : vers le vide. Ailleurs : dans le sens du jet.
                push = Tower.On(s.Body.position) && push.sqrMagnitude > 0.01f ? push.normalized : new Vector3(dir.x, 0f, dir.z).normalized;
                bool graced = s.Graced;
                Combat.Hit(s, push * 34f + Vector3.up * 11f, 0.6f, true, null);
                if (graced) continue;       // protege : des etincelles, pas de coup (ni de secousse)
                Fx.Impact(c, Blaze, 1.8f);
                if (s.IsPlayer)
                {
                    Stats.EyeHits++;
                    if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.5f);
                }
            }
            // L'EXPLOSION la ou il frappe : une sphere, un anneau, une gerbe, un eclair.
            Fx.Shock(end, Alarm, BlastRadius + 0.6f, 0.35f);
            Fx.Shock(end, Blaze, 1.8f, 0.25f);
            Fx.GroundRing(end - Vector3.up * 1f, Flame, BlastRadius + 2f, 0.4f);
            Fx.Column(end - Vector3.up * 1f, Flame, 10f, 0.25f, 1f);
            Fx.Burst(end, Alarm, 110, 14f, 0.24f, 0.7f, 0.4f, -dir, 70f);
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
            if (NearPlayer(40f) && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.2f);
            Sfx.Crash();
            beam.startWidth = 0.35f;
            beam.endWidth = 0.2f;
            state = State.Rest;
            timer = RestTime / Tower.Hardness;
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
