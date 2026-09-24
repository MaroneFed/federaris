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
            GameObject relic = Proto.Cube(t, new Vector3(0f, 4.7f, 0f), new Vector3(0.42f, 0.42f, 0.42f),
                                          RuneBlue, "Relique");
            Proto.EndVisualOnly();

            stele.rune = runeGo.GetComponent<Renderer>();
            stele.runeOff = MaterialFactory.Get(new Color(0.18f, 0.20f, 0.24f));
            stele.runeOn = MaterialFactory.GetGlow(RuneBlue, 2.4f);
            if (stele.rune != null) stele.rune.sharedMaterial = stele.runeOff;

            Renderer relicRenderer = relic.GetComponent<Renderer>();
            if (relicRenderer != null) relicRenderer.sharedMaterial = MaterialFactory.GetGlow(RuneBlue, 3f);
            relic.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
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
            if (relicShown != null)
            {
                if (relicShown.gameObject.activeSelf != lit) relicShown.gameObject.SetActive(lit);
                if (lit)
                {
                    relicShown.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
                    Vector3 p = relicShown.localPosition;
                    p.y = 4.7f + Mathf.Sin(Time.time * 1.3f) * 0.12f;
                    relicShown.localPosition = p;
                }
            }
            if (glow != null)
            {
                glow.intensity = Mathf.MoveTowards(glow.intensity, lit ? 1.8f : 0f, Time.deltaTime * 2f);
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
