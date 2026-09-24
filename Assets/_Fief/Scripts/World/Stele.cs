using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA STELE, au milieu de la cour du chateau.
    ///
    /// A la cloche, seule compte la relique POSEE ici. C'est ce qui fait converger
    /// tout le monde au chateau a la fin de la Saison -- et en Phase 2, c'est ici
    /// qu'on viendra voler celle des autres.
    ///
    /// On peut reprendre sa relique pour aller la renforcer aupres du mage, puis
    /// revenir la poser. Le geste est volontairement un peu long (E maintenu) : poser
    /// sa relique n'est pas un clic distrait.
    ///
    /// La stele REAGIT : quand une relique y est posee, sa rune s'allume, une lueur
    /// bleue baigne le dais et la relique flotte au-dessus, tournant lentement.
    /// </summary>
    public class Stele : MonoBehaviour, IInteractable
    {
        Light glow;
        Transform relicShown;
        int shownTier = -1;
        Transform[] rings = new Transform[0];
        LightBeam legendBeam;
        Renderer rune;
        Material runeOff;
        Material runeOn;

        static readonly Color StoneBlack = new Color(0.13f, 0.13f, 0.14f);
        static readonly Color DaisStone = new Color(0.26f, 0.26f, 0.25f);
        static readonly Color RuneBlue = new Color(0.55f, 0.72f, 1f);

        public static Stele Build(Transform parent, Vector3 at)
        {
            GameObject root = new GameObject("STELE");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = at;
            Transform t = root.transform;

            // Le dais : deux marches rondes, PUREMENT VISUELLES. Un cylindre Unity
            // recoit un collider en capsule ; pour une marche large et basse, la
            // capsule deviendrait une sphere invisible de quatre metres de rayon.
            Proto.BeginVisualOnly();
            Proto.Cylinder(t, new Vector3(0f, 0.12f, 0f), new Vector3(7.5f, 0.12f, 7.5f), DaisStone, "Dais");
            Proto.Cylinder(t, new Vector3(0f, 0.36f, 0f), new Vector3(5.2f, 0.12f, 5.2f), DaisStone, "Dais");
            Proto.EndVisualOnly();

            // La pierre elle-meme, elle, bloque -- et c'est son collider qui la rend
            // "interactive" pour PlayerInteractor.
            Proto.Cube(t, new Vector3(0f, 2.05f, 0f), new Vector3(1.3f, 3.4f, 0.55f), StoneBlack, "Pierre");

            Stele stele = root.AddComponent<Stele>();

            Proto.BeginVisualOnly();
            GameObject runeGo = Proto.Cube(t, new Vector3(0f, 2.5f, -0.29f), new Vector3(0.5f, 0.9f, 0.04f),
                                           RuneBlue, "Rune");
            Proto.Cube(t, new Vector3(0f, 3.85f, 0f), new Vector3(1.45f, 0.22f, 0.7f), StoneBlack, "Chapiteau");
            Proto.EndVisualOnly();
            GameObject relic = new GameObject("Relique");
            relic.transform.SetParent(t, false);
            relic.transform.localPosition = new Vector3(0f, 4.7f, 0f);

            stele.rune = runeGo.GetComponent<Renderer>();
            stele.runeOff = MaterialFactory.Get(new Color(0.18f, 0.20f, 0.24f));
            stele.runeOn = MaterialFactory.GetGlow(RuneBlue, 2.4f);
            if (stele.rune != null) stele.rune.sharedMaterial = stele.runeOff;

            stele.relicShown = relic.transform;
            relic.SetActive(false);

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.2f, -1.2f);
            stele.glow = lightGo.AddComponent<Light>();
            stele.glow.type = LightType.Point;
            stele.glow.color = RuneBlue;
            stele.glow.range = 9f;
            stele.glow.intensity = 0f;
            stele.glow.shadows = LightShadows.None;

            return stele;
        }

        void Update()
        {
            bool lit = Game.Hoard != null && Game.Hoard.RelicOnStele;

            if (rune != null)
            {
                Material want = lit ? runeOn : runeOff;
                if (rune.sharedMaterial != want) rune.sharedMaterial = want;
            }
            int tier = lit ? Relic.Tier(Game.Hoard.FinalScore) : 0;
            if (tier != shownTier) ShowTier(tier);

            if (relicShown != null)
            {
                if (relicShown.gameObject.activeSelf != lit) relicShown.gameObject.SetActive(lit);
                if (lit)
                {
                    relicShown.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
                    Vector3 p = relicShown.localPosition;
                    p.y = 4.7f + tier * 0.15f + Mathf.Sin(Time.time * 1.3f) * 0.12f;
                    relicShown.localPosition = p;
                    // Les anneaux tournent chacun sur son axe : c'est un petit systeme solaire.
                    for (int i = 0; i < rings.Length; i++)
                        rings[i].Rotate(new Vector3(i == 1 ? 60f : 0f, i == 2 ? 50f : 0f, i == 0 ? 70f : 25f) * Time.deltaTime, Space.Self);
                }
            }
            if (glow != null)
            {
                float wanted = lit ? 1.4f + tier * 0.45f : 0f;
                glow.intensity = Mathf.MoveTowards(glow.intensity, wanted, Time.deltaTime * 2f);
                glow.range = 8f + tier * 2f;
            }
        }

        // ------------------------------------------------------------------ la relique posee

        /// <summary>
        /// La relique posee n'a pas toujours la meme allure : plus elle est
        /// puissante, plus elle est grande et compliquee. Babiole : un simple cube.
        /// Fetiche : un joyau et un anneau. Relique : un joyau plus gros, un anneau,
        /// trois eclats en orbite. Tresor : deux anneaux croises, six eclats, de l'or.
        /// Legende : trois anneaux, huit eclats, et une colonne de lumiere qui monte
        /// du chateau et se voit de toute la foret.
        /// </summary>
        void ShowTier(int tier)
        {
            shownTier = tier;
            if (relicShown == null) return;
            for (int i = relicShown.childCount - 1; i >= 0; i--) Destroy(relicShown.GetChild(i).gameObject);

            Color accent = tier >= 4 ? new Color(0.95f, 0.78f, 0.35f) : RuneBlue;
            Material core = MaterialFactory.GetGlow(RuneBlue, 2.4f + tier * 0.3f);
            Material gold = MaterialFactory.GetGlow(accent, 2.2f);

            Proto.BeginVisualOnly();
            if (tier <= 1)
            {
                GameObject cube = Proto.Cube(relicShown, Vector3.zero, Vector3.one * 0.42f, RuneBlue, "Babiole");
                cube.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                cube.GetComponent<Renderer>().sharedMaterial = core;
                rings = new Transform[0];
            }
            else
            {
                float size = 0.3f + tier * 0.1f;
                GameObject top = Proto.Cone(relicShown, Vector3.zero, size, size * 1.2f, RuneBlue, "Joyau", 6);
                GameObject bottom = Proto.Cone(relicShown, Vector3.zero, size, size * 1.5f, RuneBlue, "Joyau", 6);
                bottom.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                top.GetComponent<Renderer>().sharedMaterial = core;
                bottom.GetComponent<Renderer>().sharedMaterial = core;

                int ringCount = tier == 2 ? 1 : tier == 3 ? 1 : tier == 4 ? 2 : 3;
                rings = new Transform[ringCount];
                for (int r = 0; r < ringCount; r++)
                {
                    GameObject ring = new GameObject("Anneau");
                    ring.transform.SetParent(relicShown, false);
                    ring.transform.localRotation = Quaternion.Euler(r * 60f, r * 40f, 0f);
                    float radius = size * 2.1f + r * 0.18f;
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k / 14f * Mathf.PI * 2f;
                        GameObject seg = Proto.Cube(ring.transform, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius),
                                                    new Vector3(0.05f, 0.05f, radius * 0.47f), accent, "Maillon");
                        seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                        seg.GetComponent<Renderer>().sharedMaterial = gold;
                    }
                    rings[r] = ring.transform;
                }

                int shards = tier == 3 ? 3 : tier == 4 ? 6 : tier >= 5 ? 8 : 0;
                for (int k = 0; k < shards; k++)
                {
                    float a = k / (float)shards * Mathf.PI * 2f;
                    Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2f) * 0.3f, Mathf.Sin(a)) * (size * 3.2f);
                    GameObject shard = Proto.Cone(relicShown, p, 0.07f, 0.26f, accent, "Eclat", 4);
                    shard.transform.localRotation = Quaternion.Euler(k * 37f, k * 53f, 0f);
                    shard.GetComponent<Renderer>().sharedMaterial = gold;
                }
            }
            Proto.EndVisualOnly();

            // La legende se voit de loin.
            if (tier >= 5 && legendBeam == null)
            {
                legendBeam = LightBeam.Build(transform, transform.position, new Color(0.95f, 0.82f, 0.45f), 4f, 34f);
                if (legendBeam != null) legendBeam.targetAlpha = 0.75f;
            }
            if (legendBeam != null)
            {
                legendBeam.source = transform.position;
                legendBeam.targetAlpha = tier >= 5 ? 0.75f : 0f;
            }
        }

        // ------------------------------------------------------------------ IInteractable

        public Transform Anchor { get { return transform; } }

        public bool CanInteract
        {
            get
            {
                Hoard h = Game.Hoard;
                if (h == null || h.Relic == null) return false;
                if (Game.Season != null && Game.Season.Over) return false;
                return h.RelicInHand || h.RelicOnStele;
            }
        }

        public string Prompt
        {
            get
            {
                Hoard h = Game.Hoard;
                if (h == null || h.Relic == null) return "";
                return h.RelicOnStele
                    ? "Reprendre ta relique (puissance " + h.Relic.Power + ")"
                    : "Poser ta relique sur la stele (puissance " + h.Relic.Power + ")";
            }
        }

        public float HoldDuration { get { return 1.2f; } }

        public void Interact()
        {
            Hoard h = Game.Hoard;
            if (h == null || h.Relic == null) return;

            if (h.RelicInHand && h.TryPlaceOnStele())
            {
                if (Game.Inventory != null) Game.Inventory.ExtraWeight = 0f;
                Sfx.Build();
                Toasts.Show("Ta relique repose sur la stele. Elle comptera a la cloche.", RuneBlue);
            }
            else if (h.RelicOnStele && h.TryTakeFromStele())
            {
                if (Game.Inventory != null) Game.Inventory.ExtraWeight = Relic.Weight;
                Sfx.Pop();
                Toasts.Show("Tu reprends ta relique. Elle ne compte plus tant qu'elle n'est pas reposee.",
                            Palette.Gold);
            }
        }
    }
}
