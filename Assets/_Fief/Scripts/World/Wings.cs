using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE VOL PLANE (27/09 au soir, refait le 28/09 -- Martin : "je ne sais meme pas
    /// comment on fait pour planer, je n'arrive pas, je sais que ca pourrait etre trop
    /// bien").
    ///
    /// Ce qui a change : il n'y a PLUS RIEN A APPRENDRE.
    ///   - TOUT LE MONDE a des ailes, tout le temps ;
    ///   - elles S'OUVRENT TOUTES SEULES des qu'on tombe dans le vide (plus de six
    ///     metres sous les pieds) : on saute de la tour, d'un ilot, d'une arbaleste...
    ///     et on vole ;
    ///   - on DIRIGE AVEC LA SOURIS, comme on regarde : vers le bas on PIQUE et on prend
    ///     de la vitesse ; vers le haut on REMONTE en depensant cette vitesse (comme un
    ///     oiseau, comme un planeur). Q/D glissent de cote, S freine ;
    ///   - Espace en l'air : on replie les ailes (on tombe comme une pierre), Espace a
    ///     nouveau : on les rouvre ;
    ///   - les COURANTS D'AIR (colonnes de vent entre l'ile et les ilots) font remonter
    ///     qui les traverse en planant.
    ///
    /// LES AILES D'OR : au sommet de la tour (les planeurs sur leurs chevalets), et au
    /// depart d'une arbaleste, on prend des ailes d'or jusqu'a son prochain atterrissage
    /// : elles vont plus vite et descendent moins. Le Planeur (passif) les donne toujours.
    ///
    /// LA COURONNE EST LOURDE (30/09 -- Martin : "avec les elytres, c'est trop facile
    /// de gagner : tu voles et c'est gagne, il faut de la complexite") : son porteur
    /// n'a jamais d'ailes d'or, vole plus lentement (11 m/s) et tombe VITE (8 m/s) :
    /// du sommet, il ne va PAS jusqu'aux Monuments. Il lui faut un COURANT D'AIR (tourner
    /// dedans pour remonter) ou une ARBALESTE de l'ile. Pendant ce temps, les autres, plus
    /// rapides, lui fondent dessus (le pique d'aigle).
    ///
    /// Les reglages du vol sont ici, pour toi comme pour les bots (Fly).
    /// </summary>
    public static class Wings
    {
        /// <summary>La vitesse de croisiere (m/s), a l'horizontale.</summary>
        public const float Cruise = 13f;
        /// <summary>La vitesse la plus haute, en pique.</summary>
        public const float MaxSpeed = 42f;
        /// <summary>La plus basse (en dessous, on decroche : on descend vite).</summary>
        public const float MinSpeed = 7f;
        /// <summary>La chute de base, a vitesse de croisiere (m/s).</summary>
        public const float Sink = 2.8f;
        /// <summary>Les ailes s'ouvrent seules s'il y a plus de tant de metres de vide sous les pieds.</summary>
        public const float OpenAbove = 6f;
        const float Pull = 17f;         // ce que la pente donne (ou reprend) chaque seconde
        /// <summary>Le porteur de la Couronne : sa croisiere, et sa chute (elle pese).</summary>
        public const float HeavyCruise = 11f;
        public const float HeavySink = 8f;

        /// <summary>Combien on descend par metre parcouru, a plat (les bots s'en servent pour viser).</summary>
        public static float Descent(Seeker s)
        {
            if (s != null && s.CarriesCrown) return HeavySink / HeavyCruise;
            if (s != null && (s.HasWings || s.Has(Ability.Planeur))) return 0.15f;
            return 0.22f;
        }

        static readonly Color Cloth = new Color(0.93f, 0.88f, 0.76f);
        static readonly Color Wood = new Color(0.36f, 0.26f, 0.17f);
        public static readonly Color Glow = new Color(0.7f, 0.9f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.4f);

        /// <summary>
        /// A appeler a chaque image pour un joueur (toi ou un bot) : il prend des ailes
        /// d'or au sommet, il les perd en se posant ailleurs.
        /// </summary>
        public static void Tick(Seeker s, bool grounded)
        {
            if (s == null || s.Body == null) return;
            if (grounded) s.Landed();
            Vector3 p = s.Body.position;
            if (Tower.Summit(p))
            {
                if (!s.HasWings) Grant(s, true);
                return;
            }
            if (grounded && s.HasWings && Time.time - s.WingsAt > 0.6f) { s.HasWings = false; s.FreeFlight = false; }
        }

        /// <summary>Des ailes d'or (au sommet, ou tire par une arbaleste).</summary>
        public static void Grant(Seeker s, bool announce)
        {
            if (s == null || s.Body == null) return;
            s.HasWings = true;
            s.WingsAt = Time.time;
            if (!announce) return;
            Fx.Sparks(s.Body.position + Vector3.up * 1.4f, Gold, 40, 5f);
            Fx.Ring(s.Body.position + Vector3.up * 1.2f, Gold, 0.3f, 3f, 0.4f, 0.2f, Vector3.up);
            if (s.IsPlayer)
            {
                Sfx.Pop();
                if (Game.Hud != null) Game.Hud.Tip("ailes", "AILES D'OR ! Saute dans le vide : tes ailes s'ouvrent toutes seules. Regarde en bas pour piquer, en haut pour remonter.");
            }
        }

        /// <summary>Vrai s'il y a plus de "metres" de vide sous "p" (les ailes peuvent s'ouvrir).</summary>
        public static bool VoidBelow(Vector3 p, float metres)
        {
            return !Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, metres + 0.3f, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// UNE IMAGE DE VOL. "airspeed" : la vitesse le long de la trajectoire (on la garde
        /// d'une image a l'autre). "look" : ou l'on regarde (toi : la camera ; un bot : sa
        /// cible). "side" : Q/D (-1..1). "brake" : S. Renvoie la vitesse a donner au corps.
        ///
        /// La physique, simplifiee : piquer transforme la hauteur en vitesse, remonter
        /// transforme la vitesse en hauteur, l'air freine vers la vitesse de croisiere, et
        /// plus on va lentement, plus on s'enfonce.
        /// </summary>
        /// <summary>Le vol libre : sa vitesse de croisiere (le porteur, lourd, va moins vite).</summary>
        public const float FreeCruise = 24f;
        public const float FreeHeavyCruise = 16f;

        public static Vector3 Fly(ref float airspeed, Vector3 look, float side, bool brake, Seeker s, float dt)
        {
            if (s != null && s.FreeFlight) return FlyFree(ref airspeed, look, side, brake, s, dt);
            bool heavy = s != null && s.CarriesCrown;
            bool gold = !heavy && s != null && (s.HasWings || s.Has(Ability.Planeur));
            float cruise = heavy ? HeavyCruise : Cruise * (gold ? 1.25f : 1f);
            float top = MaxSpeed * (gold ? 1.12f : heavy ? 0.8f : 1f);
            float sink = heavy ? HeavySink : Sink * (gold ? 0.75f : 1f);

            // (03/10) Un anneau de vent traverse : la vitesse remonte d'un coup (voir WindRing).
            if (s != null) WindRing.Through(s, ref airspeed);

            if (look.sqrMagnitude < 0.001f) look = Vector3.forward;
            look.Normalize();
            // On ne vole pas a la verticale : on garde toujours un peu d'avancee.
            float slope = Mathf.Clamp(look.y, -0.92f, 0.6f);
            Vector3 flat = new Vector3(look.x, 0f, look.z);
            if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
            flat.Normalize();

            // Piquer donne de la vitesse, remonter en reprend.
            airspeed += -slope * Pull * dt;
            // L'air ramene doucement vers la croisiere (plus fort quand on va trop vite).
            // (Lourd : la vitesse d'un pique se perd vite, pas de planee sans fin.)
            float drag = heavy || airspeed <= cruise ? 0.6f : 0.22f;
            airspeed = Mathf.MoveTowards(airspeed, cruise, Mathf.Abs(airspeed - cruise) * drag * dt + (brake ? 14f * dt : 0f));
            if (brake) airspeed = Mathf.MoveTowards(airspeed, MinSpeed, 10f * dt);
            airspeed = Mathf.Clamp(airspeed, MinSpeed * 0.8f, top);

            float cos = Mathf.Sqrt(1f - slope * slope);
            Vector3 v = flat * airspeed * cos + Vector3.up * airspeed * slope;
            // Plus on va lentement, plus on s'enfonce (le decrochage).
            float stall = Mathf.Clamp(cruise / Mathf.Max(airspeed, 1f), heavy ? 1f : 0.6f, 2.2f);
            v.y -= sink * stall;
            // Glisser de cote.
            Vector3 right = new Vector3(flat.z, 0f, -flat.x);
            v += right * side * 6f;
            // Les courants d'air font remonter.
            if (s != null && s.Body != null) v.y += Thermal.LiftAt(s.Body.position);
            // ... et le souffle d'un anneau de vent, un peu plus d'une seconde.
            v.y += WindRing.LiftOf(s);
            return v;
        }

        /// <summary>
        /// LE VOL LIBRE (apres une arbaleste) : on va EXACTEMENT ou l'on regarde, a vitesse
        /// constante -- vers le haut on monte, vers le bas on descend, sans rien perdre, sans
        /// tomber. S freine, Q/D glissent. Les anneaux de vent et les courants poussent encore.
        /// </summary>
        static Vector3 FlyFree(ref float airspeed, Vector3 look, float side, bool brake, Seeker s, float dt)
        {
            WindRing.Through(s, ref airspeed);
            float cruise = s.CarriesCrown ? FreeHeavyCruise : FreeCruise;
            float want = brake ? cruise * 0.45f : cruise;
            // Les anneaux donnent un elan au-dessus de la croisiere, qui retombe doucement.
            airspeed = Mathf.MoveTowards(airspeed, want, (airspeed > want ? 6f : 18f) * dt);
            if (look.sqrMagnitude < 0.001f) look = Vector3.forward;
            look.Normalize();
            Vector3 dir = new Vector3(look.x, Mathf.Clamp(look.y, -0.95f, 0.95f), look.z).normalized;
            Vector3 v = dir * airspeed;
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude > 0.001f)
            {
                flat.Normalize();
                v += new Vector3(flat.z, 0f, -flat.x) * side * 7f;
            }
            if (s.Body != null)
            {
                v.y += Thermal.LiftAt(s.Body.position) * 0.5f;
                // Pas plus haut que le ciel de l'ile (au-dessus, rien a faire : on redescend).
                if (s.Body.position.y > 140f && v.y > 0f) v.y = 0f;
            }
            v.y += WindRing.LiftOf(s);
            return v;
        }

        /// <summary>La vitesse avec laquelle on ouvre les ailes : celle qu'on avait deja (au moins la croisiere).</summary>
        public static float OpeningSpeed(Vector3 velocity)
        {
            return Mathf.Clamp(velocity.magnitude * 0.8f, Cruise, MaxSpeed);
        }

        /// <summary>
        /// Pour un bot : ou regarder pour aller en planant de "from" jusqu'a "to" (juste
        /// ce qu'il faut de pique pour arriver a sa hauteur).
        /// </summary>
        public static Vector3 LookFor(Vector3 from, Vector3 to, Seeker s)
        {
            float descent = Descent(s);
            Vector3 flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
            float horizontal = Mathf.Max(1f, flat.magnitude);
            float drop = from.y - (to.y + 2f);
            float slope = drop / horizontal;
            // A plat, on descend deja de "descent" metre par metre : au-dela, on pique.
            float y = Mathf.Clamp(-(slope - descent) * 1.2f, -0.85f, 0.15f);
            if (horizontal < 6f) y = -0.7f;
            Vector3 f = flat.normalized;
            return new Vector3(f.x, y, f.z);
        }

        /// <summary>Un bot peut-il rejoindre "to" en planant depuis "from" (sans courant d'air) ?</summary>
        public static bool CanReach(Vector3 from, Vector3 to, Seeker s)
        {
            float horizontal = new Vector2(to.x - from.x, to.z - from.z).magnitude;
            return from.y - to.y > horizontal * Descent(s) + 4f;
        }

        // ================================================================== les chevalets

        /// <summary>Un planeur sur son chevalet : deux ailes de toile tendues sur du bois, un liseré qui luit.</summary>
        public static void BuildRack(Transform parent, Vector3 at, float angle)
        {
            GameObject go = new GameObject("PLANEUR");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
            Transform t = go.transform;
            Proto.BeginVisualOnly();
            // Le chevalet : deux montants, une traverse.
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject leg = Proto.Cube(t, new Vector3(k * 0.6f, 0.55f, 0f), new Vector3(0.1f, 1.1f, 0.1f), Wood, "Montant");
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, k * 8f);
            }
            Proto.Cube(t, new Vector3(0f, 1.08f, 0f), new Vector3(1.4f, 0.08f, 0.08f), Wood, "Traverse");
            Proto.EndVisualOnly();
            // Le planeur pose dessus, ailes repliees en V.
            Transform glider = new GameObject("Ailes").transform;
            glider.SetParent(t, false);
            glider.localPosition = new Vector3(0f, 1.2f, 0f);
            glider.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Proto.BeginVisualOnly();
            Model(glider, 0.8f, 26f, Gold);
            Proto.EndVisualOnly();
        }

        /// <summary>
        /// Des ailes : deux voiles de toile en fleche, des nervures de bois, un liseré
        /// qui luit. "span" : l'envergure (en metres, sur 2) ; "fold" : l'angle du V.
        /// </summary>
        public static void Model(Transform t, float span, float fold, Color edgeColour)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Transform wing = new GameObject("Aile").transform;
                wing.SetParent(t, false);
                wing.localRotation = Quaternion.Euler(0f, 0f, side * fold);
                // Trois panneaux de toile, de plus en plus etroits vers le bout (une aile, pas une planche).
                for (int k = 0; k < 3; k++)
                {
                    float x = side * span * (0.4f + k * 0.72f);
                    float depth = 1.05f - k * 0.28f;
                    GameObject sail = Proto.Cube(wing, new Vector3(x, 0f, -0.1f - k * 0.12f), new Vector3(span * 0.74f, 0.035f, depth), Cloth, "Toile");
                    sail.transform.localRotation = Quaternion.Euler(0f, side * (10f + k * 8f), 0f);
                    GameObject rib = Proto.Cube(wing, new Vector3(x + side * span * 0.36f, 0.03f, -0.1f - k * 0.12f), new Vector3(0.05f, 0.05f, depth + 0.05f), Wood, "Nervure");
                    rib.transform.localRotation = sail.transform.localRotation;
                }
                GameObject spar = Proto.Cube(wing, new Vector3(side * span * 1.1f, 0.04f, 0.38f), new Vector3(span * 2.25f, 0.06f, 0.07f), Wood, "Longeron");
                spar.transform.localRotation = Quaternion.Euler(0f, side * 12f, 0f);
                GameObject edge = Proto.Cube(wing, new Vector3(side * span * 1.05f, 0.01f, -0.62f), new Vector3(span * 2f, 0.05f, 0.05f), Color.white, "Liseré");
                edge.transform.localRotation = Quaternion.Euler(0f, side * 16f, 0f);
                edge.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(edgeColour, 1.4f);
            }
            Proto.Cube(t, new Vector3(0f, 0f, 0f), new Vector3(0.12f, 0.1f, 1.2f), Wood, "Quille");
        }
    }

    /// <summary>
    /// UN COURANT D'AIR (28/09) : une colonne de vent qui monte des nuages jusqu'au
    /// ciel, entre l'ile et chaque ilot. Qui la traverse EN PLANANT remonte : c'est
    /// comme ca qu'on va loin, qu'on rattrape un porteur, qu'on revient d'un ilot, ou
    /// qu'on se rattrape apres avoir ete pousse de l'ile.
    ///
    /// On la voit de loin : des filets de vent qui montent en spirale, trois anneaux
    /// pales qui tournent.
    /// </summary>
    public class Thermal : MonoBehaviour
    {
        public static readonly List<Thermal> All = new List<Thermal>();

        public const float Radius = 11f;
        /// <summary>(30/09 : 11 -> 15, pour que le porteur de la Couronne, lourd, y remonte.)</summary>
        public const float Lift = 15f;
        const float Bottom = -45f;
        // (Jamais aussi haut que le sommet de la tour : on ne vole pas la Couronne par les airs.)
        const float Top = 80f;
        static readonly Color Air = new Color(0.8f, 0.93f, 1f);

        readonly List<Transform> rings = new List<Transform>();

        /// <summary>La poussee vers le haut des courants d'air en "p" (0 hors d'un courant).</summary>
        public static float LiftAt(Vector3 p)
        {
            float best = 0f;
            for (int i = 0; i < All.Count; i++)
            {
                Thermal t = All[i];
                if (t == null) continue;
                Vector3 c = t.transform.position;
                if (p.y < Bottom || p.y > Top) continue;
                float d = new Vector2(p.x - c.x, p.z - c.z).magnitude;
                if (d > Radius) continue;
                // Plus fort au coeur, et s'essouffle en haut.
                float k = (1f - d / Radius) * 0.6f + 0.4f;
                float fade = Mathf.Clamp01((Top - p.y) / 25f);
                best = Mathf.Max(best, Lift * k * fade);
            }
            return best;
        }

        /// <summary>Le courant le plus proche de "p" (null s'il n'y en a pas).</summary>
        public static Thermal Nearest(Vector3 p)
        {
            Thermal best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == null) continue;
                Vector3 c = All[i].transform.position;
                float d = new Vector2(p.x - c.x, p.z - c.z).magnitude;
                if (d < bestD) { bestD = d; best = All[i]; }
            }
            return best;
        }

        /// <summary>Un courant sur le chemin de chaque ilot, un peu apres le bord de l'ile.</summary>
        public static void PlaceAll(Transform parent)
        {
            GameObject root = new GameObject("COURANTS D'AIR");
            root.transform.SetParent(parent, false);
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Vector3 top = Ground.GetIslet(i).Top;
                float a = Mathf.Atan2(top.z, top.x);
                float r = (Ground.EdgeAt(a) + new Vector2(top.x, top.z).magnitude) * 0.5f;
                Build(root.transform, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
        }

        static void Build(Transform parent, Vector3 at)
        {
            GameObject go = new GameObject("COURANT D'AIR");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            Thermal t = go.AddComponent<Thermal>();
            All.Add(t);

            // Les filets de vent : des traits etires qui montent en tournant.
            Material m = Ambiance.Additive;
            if (m != null)
            {
                ParticleSystem ps = Ambiance.NewSystem("Vent qui monte", go.transform, new Vector3(0f, Bottom + 10f, 0f), m);
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.duration = 5f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 4.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(16f, 24f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(Air.r, Air.g, Air.b, 0.5f), new Color(1f, 1f, 1f, 0.8f));
                main.maxParticles = 500;
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 70f;
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = Radius * 0.85f;
                ps.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                ParticleSystem.VelocityOverLifetimeModule swirl = ps.velocityOverLifetime;
                swirl.enabled = true;
                swirl.space = ParticleSystemSimulationSpace.Local;
                swirl.orbitalZ = new ParticleSystem.MinMaxCurve(0.6f);
                swirl.x = new ParticleSystem.MinMaxCurve(0f);
                swirl.y = new ParticleSystem.MinMaxCurve(0f);
                swirl.z = new ParticleSystem.MinMaxCurve(0f);
                ParticleSystemRenderer rd = ps.GetComponent<ParticleSystemRenderer>();
                rd.renderMode = ParticleSystemRenderMode.Stretch;
                rd.velocityScale = 0.18f;
                rd.lengthScale = 2f;
                Ambiance.FadeInOut(ps, 0.9f);
                ps.Play();
            }

            // Trois anneaux pales qui tournent a differentes hauteurs.
            for (int k = 0; k < 3; k++)
            {
                GameObject ring = new GameObject("Anneau de vent");
                ring.transform.SetParent(go.transform, false);
                ring.transform.localPosition = new Vector3(0f, -10f + k * 28f, 0f);
                LineRenderer line = ring.AddComponent<LineRenderer>();
                line.sharedMaterial = Ambiance.Additive;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 40;
                line.widthMultiplier = 0.5f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                Color c = new Color(Air.r, Air.g, Air.b, 0.35f);
                line.startColor = c;
                line.endColor = new Color(1f, 1f, 1f, 0.05f);
                for (int i = 0; i < 40; i++)
                {
                    float a = i / 40f * Mathf.PI * 2f;
                    // Un anneau un peu tordu : une spirale qui s'ouvre.
                    float r = Radius * (0.85f + 0.15f * Mathf.Sin(a * 3f));
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a * 2f) * 1.2f, Mathf.Sin(a) * r));
                }
                t.rings.Add(ring.transform);
            }
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            float time = Time.time;
            for (int k = 0; k < rings.Count; k++)
            {
                Transform r = rings[k];
                r.localRotation = Quaternion.Euler(0f, time * (30f + k * 12f) * (k % 2 == 0 ? 1f : -1f), 0f);
                // Les anneaux montent lentement et recommencent en bas.
                float y = Mathf.Repeat(-10f + k * 28f + time * 6f, 84f) - 10f;
                r.localPosition = new Vector3(0f, y, 0f);
            }
        }
    }

    /// <summary>
    /// LES AILES DANS LE DOS d'un bot (ou d'un joueur en ligne) : repliees tant qu'il
    /// marche, grandes ouvertes quand il plane. Les ailes d'or ont un liseré d'or.
    /// </summary>
    public class WingsOnBack : MonoBehaviour
    {
        public Seeker seeker;
        /// <summary>Mis a jour chaque image par son proprietaire (Rival) : plane-t-il ?</summary>
        public bool Flying;
        Transform wings;
        Transform goldWings;
        float open;

        public static WingsOnBack Attach(Transform body, Seeker s)
        {
            GameObject go = new GameObject("Ailes dans le dos");
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, 1.32f, -0.36f);
            WingsOnBack w = go.AddComponent<WingsOnBack>();
            w.seeker = s;
            w.wings = new GameObject("Ailes").transform;
            w.wings.SetParent(go.transform, false);
            w.goldWings = new GameObject("Ailes d'or").transform;
            w.goldWings.SetParent(go.transform, false);
            Proto.BeginVisualOnly();
            Wings.Model(w.wings, 1f, 0f, Wings.Glow);
            Wings.Model(w.goldWings, 1.1f, 0f, Wings.Gold);
            Proto.EndVisualOnly();
            w.goldWings.gameObject.SetActive(false);
            return w;
        }

        void Update()
        {
            bool gold = seeker != null && (seeker.HasWings || seeker.Has(Ability.Planeur));
            bool show = seeker != null && !seeker.Hidden;
            Transform on = gold ? goldWings : wings;
            Transform off = gold ? wings : goldWings;
            if (off.gameObject.activeSelf) off.gameObject.SetActive(false);
            if (on.gameObject.activeSelf != show) on.gameObject.SetActive(show);
            if (!show) return;
            open = Mathf.MoveTowards(open, Flying ? 1f : 0f, Time.deltaTime * 4f);
            // Repliees : petites et relevees en V ; ouvertes : a plat, pleine envergure, qui battent un peu.
            float flap = Flying ? Mathf.Sin(Time.time * 2.2f) * 3f : 0f;
            on.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.3f, open);
            on.localRotation = Quaternion.Euler(Mathf.Lerp(-70f, -6f, open) + flap, 0f, 0f);
        }
    }

    /// <summary>
    /// CE QU'ON SENT EN VOLANT (ton ecran seulement) : des filets d'air qui filent
    /// autour de la camera, plus vite et plus nombreux quand on pique, et le vent
    /// qui souffle de plus en plus fort.
    ///
    /// Concept Unity : le systeme de particules simule en espace LOCAL (celui de la
    /// camera) : les filets suivent le regard au lieu de rester en arriere.
    /// </summary>
    public class GlideFeel : MonoBehaviour
    {
        ParticleSystem streaks;
        AudioSource wind;
        float amount;
        float speed;

        public static GlideFeel Attach(Transform cameraTransform)
        {
            GameObject go = new GameObject("Sensations du vol");
            go.transform.SetParent(cameraTransform, false);
            GlideFeel f = go.AddComponent<GlideFeel>();
            Material m = Ambiance.Additive;
            if (m != null)
            {
                f.streaks = Ambiance.NewSystem("Filets d'air", go.transform, new Vector3(0f, 0f, 26f), m);
                ParticleSystem.MainModule main = f.streaks.main;
                main.loop = true;
                main.duration = 2f;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.35f), new Color(0.85f, 0.95f, 1f, 0.7f));
                main.maxParticles = 300;
                ParticleSystem.EmissionModule emission = f.streaks.emission;
                emission.rateOverTime = 0f;
                ParticleSystem.ShapeModule shape = f.streaks.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(22f, 14f, 4f);
                ParticleSystem.VelocityOverLifetimeModule rush = f.streaks.velocityOverLifetime;
                rush.enabled = true;
                rush.space = ParticleSystemSimulationSpace.Local;
                rush.x = new ParticleSystem.MinMaxCurve(0f);
                rush.y = new ParticleSystem.MinMaxCurve(0f);
                rush.z = new ParticleSystem.MinMaxCurve(-60f);
                ParticleSystemRenderer rd = f.streaks.GetComponent<ParticleSystemRenderer>();
                rd.renderMode = ParticleSystemRenderMode.Stretch;
                rd.velocityScale = 0.06f;
                rd.lengthScale = 1f;
                Ambiance.FadeInOut(f.streaks, 1f);
                f.streaks.Play();
            }
            f.wind = go.AddComponent<AudioSource>();
            f.wind.clip = Sfx.FlightWind();
            f.wind.loop = true;
            f.wind.spatialBlend = 0f;
            f.wind.volume = 0f;
            f.wind.playOnAwake = false;
            return f;
        }

        /// <summary>"intensity" : 0 (au sol) a 1 (pique a pleine vitesse) ; "metresPerSecond" : la vitesse de vol.</summary>
        public void Set(float intensity, float metresPerSecond)
        {
            amount = intensity;
            speed = metresPerSecond;
        }

        void Update()
        {
            if (streaks != null)
            {
                ParticleSystem.EmissionModule emission = streaks.emission;
                emission.rateOverTime = amount <= 0.01f ? 0f : 20f + amount * 180f;
                ParticleSystem.VelocityOverLifetimeModule rush = streaks.velocityOverLifetime;
                rush.z = new ParticleSystem.MinMaxCurve(-Mathf.Max(20f, speed * 2.2f));
            }
            if (wind == null) return;
            float want = Sfx.Muted ? 0f : amount * 0.55f;
            wind.volume = Mathf.MoveTowards(wind.volume, want, Time.deltaTime * 1.5f);
            wind.pitch = 0.75f + amount * 0.6f;
            if (wind.volume > 0.001f && !wind.isPlaying) wind.Play();
            else if (wind.volume <= 0.001f && wind.isPlaying) wind.Stop();
        }
    }

    /// <summary>
    /// LE SCEAU DE LA CITADELLE (28/09 -- Martin : "les arbaletes pour remonter direct
    /// tout en haut, c'est hyper cheate"). Un dome invisible au-dessus des remparts :
    /// qui ENTRE dans la citadelle PAR LES AIRS (en planant, ou tire par une arbaleste)
    /// est renvoye dehors dans un eclair de runes d'or. On entre par les PORTES, a
    /// pied, et on monte la rampe. On en SORT en volant sans souci : le porteur saute
    /// du sommet et s'en va.
    /// </summary>
    public static class Ward
    {
        public static readonly Color Rune = new Color(1f, 0.8f, 0.35f);

        /// <summary>Au-dessus de la cour et des remparts (hors de la tour elle-meme).</summary>
        public static bool In(Vector3 p)
        {
            return Castle.Inside(p) && p.y > Castle.WallHeight + 1.5f && !Tower.On(p);
        }

        /// <summary>Vrai si l'on passe de dehors a dedans entre "from" et "to".</summary>
        public static bool Crossing(Vector3 from, Vector3 to) { return !In(from) && In(to); }

        /// <summary>LE RENVOI : un eclair de runes la ou il frappe ; renvoie la poussee a donner (dehors, un peu vers le haut).</summary>
        public static Vector3 Repel(Seeker s, Vector3 at)
        {
            Vector3 away = new Vector3(at.x, 0f, at.z);
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            Fx.Ring(at, Rune, 0.5f, 7f, 0.45f, 0.3f, away);
            Fx.Ring(at, Color.white, 0.3f, 4f, 0.3f, 0.15f, away);
            for (int k = 0; k < 6; k++)
            {
                // Les runes du dome s'allument un instant, en couronne autour du choc.
                float a = k / 6f * Mathf.PI * 2f;
                Vector3 side = new Vector3(away.z, 0f, -away.x);
                Vector3 p = at + (side * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * 3.5f;
                Fx.Ring(p, Rune, 0.2f, 1.4f, 0.5f, 0.12f, away);
            }
            Fx.Burst(at, Rune, 70, 11f, 0.2f, 0.6f, 0.2f, away, 60f);
            Fx.Flash(at, Rune, 18f, 6f, 0.35f);
            Sfx.ThudAt(at);
            if (s != null && s.IsPlayer && Game.Hud != null)
            {
                Game.Hud.Tip("sceau", "LE SCEAU DE LA CITADELLE : on n'y entre pas par les airs. Pose-toi dehors et passe par une porte.");
                Game.Hud.Flash(new Color(Rune.r, Rune.g, Rune.b, 0.4f));
                if (Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.3f);
            }
            return away * 16f + Vector3.up * 5f;
        }
    }
}
