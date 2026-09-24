using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CERF BLANC. Il n'a aucune utilite. C'est pour ca qu'on s'en souvient.
    ///
    /// Quelques fois par Saison, il sort de la brume a une vingtaine de metres,
    /// DEVANT toi. Il te regarde. Si tu t'approches, ou au bout de quelques
    /// secondes, il fait volte-face et disparait en trois bonds. Pas de son a son
    /// arrivee : le silence fait plus d'effet que n'importe quelle musique.
    ///
    /// La Corne d'appel a ete "taillee dans la corne d'un cerf que personne n'a
    /// jamais vu". Le joueur qui le voit fait le lien tout seul.
    ///
    /// Il apparait a la limite de la brume : c'est elle qui fait le fondu, pas le
    /// code. A vingt metres il est a peine une forme pale ; il se precise quand il
    /// avance d'un pas.
    /// </summary>
    public class WhiteStag : MonoBehaviour
    {
        enum State { Away, Watching, Fleeing }

        static readonly Color Coat = new Color(0.86f, 0.87f, 0.84f);
        static readonly Color CoatShade = new Color(0.72f, 0.73f, 0.71f);
        static readonly Color Antler = new Color(0.82f, 0.78f, 0.68f);

        State state = State.Away;
        float timer;
        GameObject body;
        Transform[] legs = new Transform[4];
        Transform neck;
        System.Random rng;
        float gait;

        public static WhiteStag Build(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("LE CERF BLANC");
            root.transform.SetParent(parent, false);
            WhiteStag s = root.AddComponent<WhiteStag>();
            s.rng = new System.Random((cfg != null ? cfg.worldSeed : 1) * 31 + 7);
            s.timer = 150f + (float)s.rng.NextDouble() * 120f;     // pas avant deux minutes et demie

            s.body = new GameObject("Corps");
            s.body.transform.SetParent(root.transform, false);
            Transform b = s.body.transform;

            // Une tres legere lueur : dans la brume grise, il doit rester la chose la
            // plus claire du champ de vision.
            Material coat = MaterialFactory.GetGlow(Coat, 0.35f);

            Proto.BeginVisualOnly();
            Paint(Proto.Cube(b, new Vector3(0f, 1.25f, 0f), new Vector3(0.5f, 0.55f, 1.35f), Coat, "Corps"), coat);
            Proto.Cube(b, new Vector3(0f, 1.05f, 0f), new Vector3(0.44f, 0.2f, 1.2f), CoatShade, "Ventre");

            GameObject neckGo = new GameObject("Cou");
            neckGo.transform.SetParent(b, false);
            neckGo.transform.localPosition = new Vector3(0f, 1.45f, 0.6f);
            s.neck = neckGo.transform;
            GameObject neckMesh = Proto.Cube(s.neck, new Vector3(0f, 0.3f, 0.1f), new Vector3(0.24f, 0.7f, 0.26f), Coat, "Cou");
            neckMesh.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            Paint(neckMesh, coat);
            Paint(Proto.Cube(s.neck, new Vector3(0f, 0.68f, 0.32f), new Vector3(0.22f, 0.24f, 0.46f), Coat, "Tête"), coat);
            Proto.Cube(s.neck, new Vector3(0f, 0.62f, 0.56f), new Vector3(0.12f, 0.12f, 0.08f), new Color(0.1f, 0.1f, 0.1f), "Mufle");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject ear = Proto.Cube(s.neck, new Vector3(side * 0.16f, 0.82f, 0.2f), new Vector3(0.14f, 0.06f, 0.08f), CoatShade, "Oreille");
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, side * 25f);
                // Les bois : un merrain et trois andouillers, de chaque cote.
                Vector3 baseP = new Vector3(side * 0.08f, 0.82f, 0.26f);
                GameObject beam = Proto.Cube(s.neck, baseP + new Vector3(side * 0.18f, 0.35f, -0.05f), new Vector3(0.05f, 0.75f, 0.05f), Antler, "Bois");
                beam.transform.localRotation = Quaternion.Euler(-15f, 0f, side * -32f);
                for (int k = 0; k < 3; k++)
                {
                    GameObject tine = Proto.Cube(s.neck, baseP + new Vector3(side * (0.14f + k * 0.11f), 0.25f + k * 0.2f, 0.06f),
                                                 new Vector3(0.035f, 0.28f - k * 0.04f, 0.035f), Antler, "Andouiller");
                    tine.transform.localRotation = Quaternion.Euler(-35f, 0f, side * (8f - k * 12f));
                }
            }

            // Les pattes : un pivot a la hanche, pour pouvoir les faire courir.
            Vector3[] hips = { new Vector3(-0.17f, 1.1f, 0.52f), new Vector3(0.17f, 1.1f, 0.52f),
                               new Vector3(-0.17f, 1.1f, -0.52f), new Vector3(0.17f, 1.1f, -0.52f) };
            for (int i = 0; i < 4; i++)
            {
                GameObject hip = new GameObject("Hanche");
                hip.transform.SetParent(b, false);
                hip.transform.localPosition = hips[i];
                s.legs[i] = hip.transform;
                Proto.Cube(hip.transform, new Vector3(0f, -0.52f, 0f), new Vector3(0.09f, 1.05f, 0.1f), CoatShade, "Patte");
            }
            Proto.Cube(b, new Vector3(0f, 1.4f, -0.72f), new Vector3(0.12f, 0.16f, 0.1f), Color.white, "Queue");
            Proto.EndVisualOnly();

            s.body.SetActive(false);
            return s;
        }

        static void Paint(GameObject go, Material m)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Transform player = Game.PlayerTransform;
            bool playing = Game.Season != null && Game.Season.Running && Game.Menus != null && !Game.Menus.Blocking;
            if (player == null && state != State.Away) return;

            switch (state)
            {
                case State.Away:
                    if (!playing || player == null) return;
                    timer -= dt;
                    if (timer <= 0f && !TryAppear(player)) timer = 20f;
                    break;

                case State.Watching:
                {
                    timer -= dt;
                    Vector3 to = player.position - transform.position;
                    to.y = 0f;
                    // Il tourne la tete vers toi, lentement.
                    Quaternion look = Quaternion.LookRotation(transform.InverseTransformDirection(to.normalized), Vector3.up);
                    float yaw = Mathf.Clamp(Mathf.DeltaAngle(0f, look.eulerAngles.y), -70f, 70f);
                    neck.localRotation = Quaternion.RotateTowards(neck.localRotation, Quaternion.Euler(0f, yaw, 0f), 40f * dt);
                    if (timer <= 0f || to.magnitude < 10f) Flee(player);
                    break;
                }

                case State.Fleeing:
                {
                    timer -= dt;
                    gait += dt * 11f;
                    Vector3 p = transform.position + transform.forward * 8.5f * dt;
                    p.y = Ground.Sample(p.x, p.z) + Mathf.Abs(Mathf.Sin(gait)) * 0.45f;
                    transform.position = p;
                    for (int i = 0; i < 4; i++)
                    {
                        float phase = gait + (i < 2 ? 0f : Mathf.PI);
                        legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase) * 45f, 0f, 0f);
                    }
                    if (timer <= 0f)
                    {
                        body.SetActive(false);
                        state = State.Away;
                        timer = 200f + (float)rng.NextDouble() * 180f;
                    }
                    break;
                }
            }
        }

        /// <summary>Devant toi, a la limite de la brume, sur un sol libre.</summary>
        bool TryAppear(Transform player)
        {
            float sight = Game.Config != null ? Game.Config.sightDistance : 20f;
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return false;
            forward.Normalize();

            for (int i = 0; i < 12; i++)
            {
                float angle = ((float)rng.NextDouble() - 0.5f) * 90f;
                float distance = sight * (0.85f + (float)rng.NextDouble() * 0.2f);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;
                Vector3 at = player.position + dir * distance;
                if (Castle.Covers(at.x, at.z, 5f) || Landmarks.Near(at.x, at.z, 0f)) continue;
                at.y = Ground.Sample(at.x, at.z);

                Collider[] hits = Physics.OverlapCapsule(at + Vector3.up * 0.6f, at + Vector3.up * 1.6f, 1.1f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int h = 0; h < hits.Length; h++)
                {
                    if (hits[h] is MeshCollider) continue;
                    blocked = true;
                    break;
                }
                if (blocked) continue;

                transform.position = at;
                Vector3 face = player.position - at;
                face.y = 0f;
                // De trois quarts : on voit sa silhouette, ses bois, et il tourne la tete.
                transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, 60f, 0f) * face.normalized, Vector3.up);
                neck.localRotation = Quaternion.identity;
                for (int l = 0; l < 4; l++) legs[l].localRotation = Quaternion.identity;
                body.SetActive(true);
                state = State.Watching;
                timer = 7f;
                return true;
            }
            return false;
        }

        void Flee(Transform player)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 40f, 0f) * away.normalized, Vector3.up);
            neck.localRotation = Quaternion.identity;
            state = State.Fleeing;
            timer = 3.2f;
            gait = 0f;
            Sfx.Step();
        }
    }
}
