using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES ANNEAUX DE VENT (03/10 -- Martin : "pour faire le chemin de la tour jusqu'a un
    /// ilot, je comprends pas bien ; il faudrait des boosts en l'air, comme dans Minecraft,
    /// quand tu mets du feu, tu peux t'envoler encore un peu plus haut").
    ///
    /// Du haut de la tour vers CHAQUE ilot, une chaine de quatre grands anneaux de vent qui
    /// luisent, poses sur la trajectoire du planeur. On passe DEDANS en planant : un
    /// souffle te PROPULSE -- ta vitesse remonte d'un coup, et pendant un peu plus d'une
    /// seconde le vent te souleve (comme la fusee des elytres de Minecraft, ou le feu de
    /// camp sous les elytres).
    ///
    /// Le porteur de la Couronne en profite aussi : c'est pour lui qu'ils sont poses la ou
    /// ils sont. Il reste lourd (il tombe vite entre deux anneaux), mais en les enfilant
    /// TOUS les quatre, il atteint l'ilot sans courant d'air. En rater un, c'est tomber
    /// court -- et pendant ce temps, les autres, plus legers, lui fondent dessus. C'est
    /// ca, le clip : la Couronne qui enfile les anneaux, un piqué d'aigle au dernier.
    ///
    /// Les bots qui portent la Couronne suivent la chaine (Rival).
    /// </summary>
    public class WindRing : MonoBehaviour
    {
        public static readonly List<WindRing> All = new List<WindRing>();

        /// <summary>Le rayon de l'anneau (on passe dedans) et sa profondeur.</summary>
        public const float Radius = 5.5f;
        const float Depth = 2.6f;
        /// <summary>Combien de temps le vent souleve, et de combien (m/s au debut, puis moins).</summary>
        public const float BoostSeconds = 1.3f;
        public const float BoostLift = 26f;
        /// <summary>Ce qu'on suppose qu'un anneau fait gagner au porteur (m), pour les poser.</summary>
        const float AssumedGain = 14f;
        const int PerRoute = 4;

        /// <summary>L'ilot vers lequel cette chaine mene, et le rang de l'anneau (0 : le premier, pres de la tour).</summary>
        public int Route;
        public int Order;
        Vector3 axis;
        readonly List<Transform> spin = new List<Transform>();

        static readonly Color Air = new Color(0.75f, 0.95f, 1f);

        public Vector3 Centre { get { return transform.position; } }

        // ================================================================== le vol

        /// <summary>
        /// A chaque image de vol : "s" passe-t-il dans un anneau ? Si oui, sa vitesse remonte
        /// d'un coup ("airspeed") et le vent le souleve (BoostUntil). Rend vrai si ca vient
        /// d'arriver.
        /// </summary>
        public static bool Through(Seeker s, ref float airspeed)
        {
            if (s == null || s.Body == null) return false;
            Vector3 p = s.Body.position + Vector3.up * 1f;
            float now = Time.time;
            for (int i = 0; i < All.Count; i++)
            {
                WindRing r = All[i];
                if (r == null) continue;
                Vector3 d = p - r.Centre;
                if (d.sqrMagnitude > (Radius + Depth) * (Radius + Depth)) continue;
                float along = Vector3.Dot(d, r.axis);
                if (Mathf.Abs(along) > Depth) continue;
                if ((d - r.axis * along).magnitude > Radius) continue;
                if (s.LastRing == r && now - s.LastRingAt < 2f) continue;
                // L'ENFILADE : les anneaux d'une meme chaine, dans l'ordre, sans se poser.
                bool follows = s.LastRing != null && s.LastRing.Route == r.Route && s.LastRing.Order == r.Order - 1 && now - s.LastRingAt < 12f;
                s.RingChain = follows ? s.RingChain + 1 : 1;
                s.LastRing = r;
                s.LastRingAt = now;
                s.BoostUntil = now + BoostSeconds;
                bool heavy = s.CarriesCrown;
                airspeed = Mathf.Max(airspeed, heavy ? 20f : 28f);
                r.Fire(s);
                return true;
            }
            return false;
        }

        /// <summary>Ce que le vent de l'anneau ajoute a la montee, maintenant (0 s'il n'y en a plus).</summary>
        public static float LiftOf(Seeker s)
        {
            if (s == null) return 0f;
            float left = s.BoostUntil - Time.time;
            if (left <= 0f) return 0f;
            return BoostLift * (left / BoostSeconds);
        }

        void Fire(Seeker s)
        {
            Color c = s.CarriesCrown ? Wings.Gold : Air;
            Fx.Ring(Centre, c, Radius * 0.9f, Radius * 1.8f, 0.4f, 0.5f, axis);
            Fx.Ring(Centre, Color.white, Radius * 0.5f, Radius * 1.3f, 0.3f, 0.25f, axis);
            Fx.Burst(Centre, c, 40, 14f, 0.22f, 0.6f, 0f, axis, 25f);
            Sfx.Boost(Centre, s.IsPlayer);
            if (s.IsPlayer && Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Kick(5f);
            // Les quatre d'affilee : un gros eclat d'or, et la fanfare si c'est toi -- le clip.
            if (s.RingChain >= PerRoute)
            {
                Fx.Ring(Centre, Wings.Gold, Radius, Radius * 3.2f, 0.7f, 0.7f, axis);
                Fx.Burst(Centre, Wings.Gold, 90, 18f, 0.3f, 1f, 0f, Vector3.zero, 0f);
                Fx.Flash(Centre, Wings.Gold, 25f, 4f, 0.4f);
                if (s.IsPlayer) Sfx.Moment();
                s.RingChain = 0;
            }
        }

        // ================================================================== les bots

        /// <summary>
        /// Pour un bot qui plane de "from" vers "goal" : le prochain anneau a enfiler (devant
        /// lui, sur la chaine qui mene vers "goal", pas trop au-dessus de lui). Null s'il n'y
        /// en a pas.
        /// </summary>
        public static WindRing NextToward(Vector3 from, Vector3 goal)
        {
            WindRing best = null;
            float bestD = float.MaxValue;
            Vector3 toGoal = new Vector3(goal.x - from.x, 0f, goal.z - from.z);
            if (toGoal.sqrMagnitude < 1f) return null;
            toGoal.Normalize();
            for (int i = 0; i < All.Count; i++)
            {
                WindRing r = All[i];
                if (r == null) continue;
                Vector3 d = r.Centre - from;
                Vector3 flat = new Vector3(d.x, 0f, d.z);
                // Devant lui, dans la direction du but, a sa portee et pas au-dessus de lui.
                if (flat.magnitude < 3f || Vector3.Dot(flat.normalized, toGoal) < 0.75f) continue;
                if (d.y > 2f || flat.magnitude > 60f) continue;
                // Et plus pres du but que lui (sinon c'est un anneau d'une autre chaine, derriere).
                if (new Vector2(goal.x - r.Centre.x, goal.z - r.Centre.z).magnitude > new Vector2(goal.x - from.x, goal.z - from.z).magnitude) continue;
                if (flat.magnitude < bestD) { bestD = flat.magnitude; best = r; }
            }
            return best;
        }

        // ================================================================== la pose

        /// <summary>
        /// Une chaine d'anneaux du haut de la tour vers chaque ilot, sur la trajectoire du
        /// porteur de la Couronne (il descend vite : un anneau le remonte).
        /// </summary>
        public static void PlaceAll(Transform parent)
        {
            All.Clear();
            GameObject root = new GameObject("ANNEAUX DE VENT");
            root.transform.SetParent(parent, false);
            float descent = Wings.HeavySink / Wings.HeavyCruise;
            for (int i = 0; i < Ground.IsletCount; i++)
            {
                Vector3 top = Ground.GetIslet(i).Top;
                Vector3 dir = new Vector3(top.x, 0f, top.z).normalized;
                Vector3 start = dir * (Tower.OuterRadius + 4f) + Vector3.up * (Tower.Height - 4f);
                Vector3 end = top - dir * (Ground.GetIslet(i).Radius * 0.6f) + Vector3.up * 8f;
                float span = new Vector2(end.x - start.x, end.z - start.z).magnitude;
                Vector3 prev = start;
                for (int k = 0; k < PerRoute; k++)
                {
                    float f = (k + 1f) / (PerRoute + 1f);
                    Vector3 at = Vector3.Lerp(start, end, f);
                    // La hauteur du porteur a cet endroit, s'il a enfile les anneaux d'avant --
                    // un peu en dessous (on pique pour y entrer, on ne remonte pas pour l'attraper).
                    float natural = start.y - span * f * descent + AssumedGain * k - 2f;
                    at.y = Mathf.Clamp(natural, end.y + 4f, start.y - 6f);
                    Build(root.transform, at, (at - prev).normalized, i, k);
                    prev = at;
                }
            }
        }

        static WindRing Build(Transform parent, Vector3 at, Vector3 axis, int route, int order)
        {
            GameObject go = new GameObject("ANNEAU DE VENT");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.LookRotation(axis, Vector3.up);
            WindRing r = go.AddComponent<WindRing>();
            r.axis = axis;
            r.Route = route;
            r.Order = order;
            All.Add(r);

            Material m = Ambiance.Additive;
            if (m == null) return r;
            // Deux cercles qui luisent et tournent en sens contraire, un troisieme, dore, plus
            // fin : on les voit de tres loin, meme sur le ciel.
            for (int k = 0; k < 3; k++)
            {
                GameObject ring = new GameObject("Cercle de vent");
                ring.transform.SetParent(go.transform, false);
                LineRenderer line = ring.AddComponent<LineRenderer>();
                line.sharedMaterial = m;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 48;
                line.widthMultiplier = k == 2 ? 0.28f : 0.55f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                Color c = k == 2 ? new Color(1f, 0.85f, 0.45f, 0.9f) : new Color(Air.r, Air.g, Air.b, 0.8f);
                line.startColor = c;
                line.endColor = new Color(c.r, c.g, c.b, 0.35f);
                float radius = Radius * (k == 0 ? 1f : k == 1 ? 0.92f : 1.06f);
                for (int i = 0; i < 48; i++)
                {
                    float a = i / 48f * Mathf.PI * 2f;
                    float wobble = k == 1 ? 0.08f * Mathf.Sin(a * 6f) : 0f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * (1f + wobble), Mathf.Sin(a) * radius * (1f + wobble), 0f));
                }
                r.spin.Add(ring.transform);
            }

            // Du vent qui passe au travers, dans le sens du vol : on lit la direction.
            ParticleSystem ps = Ambiance.NewSystem("Vent de l'anneau", go.transform, -Vector3.forward * 3f, m);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(Air.r, Air.g, Air.b, 0.6f), new Color(1f, 1f, 1f, 0.8f));
            main.maxParticles = 80;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 40f;
            ParticleSystem.ShapeModule shape = ps.shape;
            // Un cone ouvert de 2 degres : le vent part tout droit, dans l'axe de l'anneau.
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2f;
            shape.radius = Radius * 0.8f;
            ParticleSystemRenderer rd = ps.GetComponent<ParticleSystemRenderer>();
            rd.renderMode = ParticleSystemRenderMode.Stretch;
            rd.velocityScale = 0.12f;
            rd.lengthScale = 2f;
            Ambiance.FadeInOut(ps, 0.9f);
            ps.Play();
            return r;
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            float t = Time.time;
            for (int k = 0; k < spin.Count; k++)
            {
                spin[k].localRotation = Quaternion.Euler(0f, 0f, t * (k == 1 ? -40f : 25f + k * 10f));
                // Il "respire" : un anneau vivant se remarque.
                float breathe = 1f + 0.05f * Mathf.Sin(t * 2.4f + Order + k);
                spin[k].localScale = new Vector3(breathe, breathe, 1f);
            }
        }
    }
}
