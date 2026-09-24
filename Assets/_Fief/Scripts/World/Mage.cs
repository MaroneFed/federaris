using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE MAGE ERRANT : le premier PNJ du jeu.
    ///
    /// Il apparait selon l'agenda de la Saison (Season.CurrentAppearance), a un
    /// endroit tire au hasard, reste deux minutes et demie, puis s'evanouit.
    ///
    /// ON NE LE VOIT PAS DE LOIN : la brume coupe a 20 m. On l'ENTEND. Sa voix est un
    /// AudioSource "3D" : Unity baisse le volume avec la distance et le place a gauche
    /// ou a droite selon ou il se trouve. Tourner la tete, c'est deja le chercher.
    ///
    /// Concept Unity : spatialBlend = 1 rend un son "3D" (0 = il sort des deux
    /// oreilles pareil, comme la musique). La courbe Logarithmic imite l'air reel :
    /// le son faiblit vite pres de la source, puis lentement -- on l'entend encore
    /// a deux cents metres.
    ///
    /// Ce composant ne decide rien de la regle : QUAND il est la vient de Season,
    /// CE QU'IL FORGE vient de Relic. Lui ne fait que se montrer, chanter, et ouvrir
    /// le panneau de forge.
    /// </summary>
    public class Mage : MonoBehaviour, IInteractable
    {
        static readonly Color Robe = new Color(0.13f, 0.14f, 0.23f);
        static readonly Color RobeDark = new Color(0.09f, 0.10f, 0.16f);
        static readonly Color Wood = new Color(0.22f, 0.17f, 0.12f);
        static readonly Color RuneStone = new Color(0.20f, 0.21f, 0.23f);
        static readonly Color Face = new Color(0.03f, 0.03f, 0.04f);
        public static readonly Color Glow = new Color(0.55f, 0.70f, 1f);

        const float FadeSeconds = 1.6f;
        const float Reach = 8f;   // les pierres du cercle proposent aussi "Parler au mage"

        GameConfig cfg;
        GameObject body;
        Transform figure;
        Transform[] orbiters;
        Light halo;
        AudioSource voice;

        int appearance = -1;        // l'apparition dont on occupe la place, -1 = absent
        float shown;                // 0 -> 1 : fondu d'apparition
        bool warned;                // "il repart bientot" deja dit pour cette apparition

        public bool Present { get { return appearance >= 0; } }

        // ================================================================== construction

        public static Mage Build(Transform parent, GameConfig cfg)
        {
            GameObject root = new GameObject("MAGE");
            root.transform.SetParent(parent, false);
            Mage mage = root.AddComponent<Mage>();
            mage.cfg = cfg;

            mage.body = new GameObject("Corps");
            mage.body.transform.SetParent(root.transform, false);
            Transform b = mage.body.transform;

            // Un seul collider plein : on ne lui passe pas au travers, et c'est lui que
            // PlayerInteractor trouve pour proposer "Parler au mage".
            CapsuleCollider capsule = mage.body.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1.2f, 0f);
            capsule.height = 2.4f;
            capsule.radius = 0.6f;

            // --- le cercle de pierres dressees : il marque le lieu, meme mage absent
            // du champ de vision. Sept pierres, hauteurs irregulieres. Elles gardent
            // leur collider : on ne traverse pas une pierre.
            float[] heights = { 1.1f, 0.7f, 0.95f, 0.6f, 1.2f, 0.8f, 0.9f };
            for (int i = 0; i < heights.Length; i++)
            {
                float a = i / (float)heights.Length * Mathf.PI * 2f + 0.3f;
                GameObject s = Proto.Cube(b, new Vector3(Mathf.Cos(a) * 3.3f, heights[i] * 0.5f - 0.1f, Mathf.Sin(a) * 3.3f),
                                          new Vector3(0.45f, heights[i], 0.32f), RuneStone, "Pierre dressee");
                s.transform.localRotation = Quaternion.Euler(Mathf.Sin(i * 2.1f) * 6f, -a * Mathf.Rad2Deg, Mathf.Cos(i * 1.7f) * 5f);
            }

            Proto.BeginVisualOnly();

            // --- la silhouette : une robe en quatre tranches qui se resserrent. Un
            // cone low-poly sans maillage sur mesure : quatre cubes tournes de 45 degres
            // l'un sur l'autre donnent une robe a facettes.
            GameObject fig = new GameObject("Silhouette");
            fig.transform.SetParent(b, false);
            Transform f = fig.transform;
            mage.figure = f;

            float[] widths = { 1.05f, 0.86f, 0.68f, 0.52f };
            for (int i = 0; i < widths.Length; i++)
            {
                GameObject slice = Proto.Cube(f, new Vector3(0f, 0.28f + i * 0.46f, 0f),
                                              new Vector3(widths[i], 0.5f, widths[i]), i % 2 == 0 ? Robe : RobeDark, "Robe");
                slice.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }
            Proto.Cube(f, new Vector3(0f, 2.02f, 0f), new Vector3(0.78f, 0.22f, 0.5f), RobeDark, "Epaules");
            GameObject hood = Proto.Cube(f, new Vector3(0f, 2.3f, 0f), new Vector3(0.46f, 0.5f, 0.46f), Robe, "Capuche");
            hood.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            GameObject peak = Proto.Cube(f, new Vector3(0f, 2.6f, -0.06f), new Vector3(0.26f, 0.3f, 0.26f), Robe, "Pointe");
            peak.transform.localRotation = Quaternion.Euler(-14f, 45f, 0f);
            Proto.Cube(f, new Vector3(0f, 2.26f, 0.2f), new Vector3(0.3f, 0.3f, 0.08f), Face, "Visage");

            // Deux yeux pales dans le noir de la capuche : inquietant, pas effrayant.
            Material eyes = MaterialFactory.GetGlow(Glow, 1.6f);
            GameObject eyeL = Proto.Cube(f, new Vector3(-0.07f, 2.29f, 0.245f), new Vector3(0.05f, 0.025f, 0.01f), Glow, "Oeil");
            GameObject eyeR = Proto.Cube(f, new Vector3(0.07f, 2.29f, 0.245f), new Vector3(0.05f, 0.025f, 0.01f), Glow, "Oeil");
            eyeL.GetComponent<Renderer>().sharedMaterial = eyes;
            eyeR.GetComponent<Renderer>().sharedMaterial = eyes;

            // --- le baton, et sa pierre qui eclaire
            GameObject staff = Proto.Cube(f, new Vector3(0.58f, 1.35f, 0.15f), new Vector3(0.07f, 2.7f, 0.07f), Wood, "Baton");
            staff.transform.localRotation = Quaternion.Euler(4f, 0f, -4f);
            GameObject crystal = Proto.Cube(f, new Vector3(0.66f, 2.78f, 0.17f), new Vector3(0.18f, 0.26f, 0.18f), Glow, "Pierre du baton");
            crystal.transform.localRotation = Quaternion.Euler(45f, 20f, 45f);
            crystal.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(Glow, 3.2f);

            // --- trois runes qui tournent autour de lui
            Material runeGlow = MaterialFactory.GetGlow(Glow, 2f);
            mage.orbiters = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject rune = Proto.Cube(b, Vector3.zero, new Vector3(0.16f, 0.24f, 0.05f), Glow, "Rune");
                rune.GetComponent<Renderer>().sharedMaterial = runeGlow;
                mage.orbiters[i] = rune.transform;
            }

            Proto.EndVisualOnly();

            // --- la lueur : elle ne porte pas loin (la brume l'avale), mais de pres
            // elle bleuit les troncs autour -- on sait qu'on est arrive.
            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(b, false);
            lightGo.transform.localPosition = new Vector3(0.4f, 2.6f, 0.6f);
            mage.halo = lightGo.AddComponent<Light>();
            mage.halo.type = LightType.Point;
            mage.halo.color = Glow;
            mage.halo.range = 16f;
            mage.halo.intensity = 0f;
            mage.halo.shadows = LightShadows.None;

            // --- la voix
            mage.voice = root.AddComponent<AudioSource>();
            mage.voice.clip = Sfx.Drone();
            mage.voice.loop = true;
            mage.voice.playOnAwake = false;
            mage.voice.spatialBlend = 1f;
            mage.voice.rolloffMode = AudioRolloffMode.Logarithmic;
            mage.voice.minDistance = 22f;
            mage.voice.maxDistance = 340f;
            mage.voice.dopplerLevel = 0f;
            mage.voice.volume = 0f;

            mage.body.SetActive(false);
            return mage;
        }

        // ================================================================== vie

        void Update()
        {
            Season season = Game.Season;
            int wanted = season != null ? season.CurrentAppearance : -1;

            if (wanted != appearance)
            {
                if (appearance >= 0) Leave();
                if (wanted >= 0) Appear(wanted);
                appearance = wanted;
            }

            // Le fondu : il monte quand il est la, redescend quand il part.
            float target = Present ? 1f : 0f;
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime / FadeSeconds);
            if (shown <= 0f && !Present)
            {
                if (body.activeSelf) body.SetActive(false);
                if (voice.isPlaying) voice.Stop();
                return;
            }

            // Pause (Time.timeScale = 0) : la voix se tait avec le reste du monde.
            if (Time.timeScale <= 0f) { if (voice.isPlaying) voice.Pause(); return; }
            if (!voice.isPlaying) voice.Play();
            voice.volume = Sfx.Muted ? 0f : 0.95f * shown;

            Animate();

            if (Present && !warned && season.MageTimeLeft < 30f)
            {
                warned = true;
                Toasts.Show("La voix du mage faiblit. Il va bientot repartir.", Glow);
            }
        }

        void Animate()
        {
            float t = Time.time;

            // Il apparait en montant du sol, et s'y renfonce en partant.
            figure.localScale = new Vector3(1f, Mathf.SmoothStep(0.05f, 1f, shown), 1f);

            // Il se tourne lentement vers celui qui approche.
            if (Game.PlayerTransform != null)
            {
                Vector3 to = Game.PlayerTransform.position - figure.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.5f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                    figure.rotation = Quaternion.RotateTowards(figure.rotation, look, 40f * Time.deltaTime);
                }
            }

            for (int i = 0; i < orbiters.Length; i++)
            {
                float a = t * 0.55f + i * 2.094f;
                orbiters[i].localPosition = new Vector3(Mathf.Cos(a) * 1.5f,
                                                        1.5f + Mathf.Sin(t * 1.1f + i * 1.7f) * 0.3f,
                                                        Mathf.Sin(a) * 1.5f);
                orbiters[i].localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }

            float pulse = 1.5f + Mathf.Sin(t * 1.9f) * 0.25f + Mathf.Sin(t * 4.3f) * 0.1f;
            halo.intensity = pulse * shown;
        }

        void Appear(int k)
        {
            Vector3 spot = ChooseSpot(k);
            transform.position = spot;
            body.SetActive(true);
            warned = false;
            shown = 0f;

            string where = Game.PlayerTransform != null
                ? Hud.Direction(Game.PlayerTransform.position, spot)
                : "quelque part";
            Toasts.Show("Le mage chante, " + where + ". Ecoute, et suis sa voix.", Glow);
        }

        void Leave()
        {
            Toasts.Show("La voix du mage s'est tue. Il reviendra ailleurs.", UiStyle.InkDim);
        }

        // ================================================================== ou apparaitre

        /// <summary>
        /// Un endroit tire au hasard, mais pas n'importe lequel :
        ///  - a bonne distance de toi (110 a 260 m) : il faut MARCHER, charge ;
        ///  - pas dans le chateau, pas dans un creux a pierres-lune ;
        ///  - sur un sol presque plat, sans tronc ni rocher dans son cercle ;
        ///  - de preference la ou la foret est claire.
        /// Le hasard est tire de la graine du monde et du numero d'apparition : en
        /// Phase 3, toutes les machines pourront en tirer les memes candidats.
        /// </summary>
        Vector3 ChooseSpot(int k)
        {
            int seed = cfg != null ? cfg.worldSeed : 1;
            System.Random rng = new System.Random(seed * 31 + k * 7919 + 17);

            float half = (cfg != null ? cfg.mapSize : 700f) * 0.5f - 40f;
            float near = cfg != null ? cfg.mageMinDistance : 110f;
            float far = cfg != null ? cfg.mageMaxDistance : 260f;
            Vector3 me = Game.PlayerTransform != null ? Game.PlayerTransform.position : Vector3.zero;

            Vector2 best = Vector2.zero;
            float bestScore = float.MaxValue;
            int valid = 0;

            for (int i = 0; i < 600 && valid < 6; i++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;

                float dx = x - me.x, dz = z - me.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < near || d > far) continue;
                if (!Clear(x, z)) continue;

                valid++;
                float score = Forest.Canopy(x, z) + Ground.Slope(x, z);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new Vector2(x, z);
                }
            }

            if (valid == 0)
            {
                // Rien de parfait (bord de carte, par exemple) : on se rabat sur un point
                // vers le centre de la foret, a mi-distance, en decalant jusqu'a trouver
                // une place libre.
                Vector3 inward = -new Vector3(me.x, 0f, me.z);
                if (inward.sqrMagnitude < 1f) inward = Vector3.forward;
                inward.Normalize();
                best = new Vector2(me.x, me.z) + new Vector2(inward.x, inward.z) * ((near + far) * 0.5f);
                for (int i = 0; i < 40 && !Clear(best.x, best.y); i++)
                    best += new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * 6f;
            }

            return Ground.Place(best.x, best.y, 0f);
        }

        static bool Clear(float x, float z)
        {
            if (Castle.Covers(x, z, 20f)) return false;
            if (Gathering.NearHollow(x, z, 10f)) return false;
            if (Ground.Slope(x, z) > 0.3f) return false;

            Vector3 centre = Ground.Place(x, z, 1.5f);
            Collider[] hits = Physics.OverlapSphere(centre, 3.6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] is MeshCollider) continue;          // le sol
                if (hits[i].GetComponentInParent<PlayerController>() != null) continue;
                return false;
            }
            return true;
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }

        public bool CanInteract { get { return Present && shown > 0.5f; } }

        public string Prompt { get { return "Parler au mage"; } }

        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null || !Present) return;
            Game.Hud.OpenPanel(new ForgePanel(this));
            Sfx.Pop();
        }

        /// <summary>Le panneau de forge se ferme si l'on s'eloigne ou si le mage part.</summary>
        public bool Near(Vector3 position)
        {
            Vector3 d = position - transform.position;
            d.y = 0f;
            return Present && d.magnitude <= Reach;
        }
    }
}
