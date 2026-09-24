using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE REGISTRE, au milieu de la cour du chateau : l'ancienne grande stele.
    ///
    /// Depuis que chacun a SA stele (quelque part dans la foret), celle du chateau
    /// ne recoit plus de relique. Elle SAIT. Elle grave le nom de chaque chercheur
    /// et la puissance de la relique posee sur sa stele -- et au-dessus d'elle
    /// flotte, en image, la relique de celui qui mene.
    ///
    /// C'est la seule facon de savoir ou en sont les autres. Venir la lire, c'est
    /// risquer les gardes ; ne pas la lire, c'est jouer a l'aveugle.
    /// </summary>
    public class Registry : MonoBehaviour, IInteractable, IDialogue
    {
        static readonly Color StoneBlack = new Color(0.13f, 0.13f, 0.14f);
        static readonly Color DaisStone = new Color(0.26f, 0.26f, 0.25f);
        static readonly Color Gold = new Color(0.92f, 0.78f, 0.42f);

        Transform leaderShown;
        Transform[] rings = new Transform[0];
        int shownTier = -1;
        Light glow;

        public static Registry Build(Transform parent, Vector3 at)
        {
            GameObject root = new GameObject("LE REGISTRE");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = at;
            Transform t = root.transform;

            Proto.BeginVisualOnly();
            Proto.Cylinder(t, new Vector3(0f, 0.12f, 0f), new Vector3(7.5f, 0.12f, 7.5f), DaisStone, "Dais");
            Proto.Cylinder(t, new Vector3(0f, 0.36f, 0f), new Vector3(5.2f, 0.12f, 5.2f), DaisStone, "Dais");
            Proto.EndVisualOnly();
            Proto.Cube(t, new Vector3(0f, 2.05f, 0f), new Vector3(1.3f, 3.4f, 0.55f), StoneBlack, "Pierre");

            Registry reg = root.AddComponent<Registry>();

            // Les lignes gravees : une par chercheur, qui luisent a peine.
            Proto.BeginVisualOnly();
            Material engraved = MaterialFactory.GetGlow(Gold, 0.9f);
            for (int i = 0; i < 4; i++)
            {
                GameObject line = Proto.Cube(t, new Vector3(0f, 3.0f - i * 0.36f, -0.29f), new Vector3(0.9f, 0.07f, 0.02f), Gold, "Gravure");
                line.GetComponent<Renderer>().sharedMaterial = engraved;
            }
            Proto.Cube(t, new Vector3(0f, 3.85f, 0f), new Vector3(1.45f, 0.22f, 0.7f), StoneBlack, "Chapiteau");
            Proto.EndVisualOnly();

            GameObject shown = new GameObject("Relique du premier");
            shown.transform.SetParent(t, false);
            shown.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            reg.leaderShown = shown.transform;

            GameObject lightGo = new GameObject("Lueur");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.2f, -1.2f);
            reg.glow = lightGo.AddComponent<Light>();
            reg.glow.type = LightType.Point;
            reg.glow.color = Stele.RuneBlue;
            reg.glow.range = 9f;
            reg.glow.intensity = 0f;
            reg.glow.shadows = LightShadows.None;
            return reg;
        }

        Seeker Leader
        {
            get
            {
                Seeker best = null;
                for (int i = 0; i < Game.Seekers.Count; i++)
                    if (best == null || Game.Seekers[i].Score > best.Score) best = Game.Seekers[i];
                return best != null && best.Score > 0 ? best : null;
            }
        }

        void Update()
        {
            Seeker leader = Leader;
            int tier = leader != null ? Relic.Tier(leader.Score) : 0;
            if (tier != shownTier)
            {
                shownTier = tier;
                for (int i = leaderShown.childCount - 1; i >= 0; i--) Destroy(leaderShown.GetChild(i).gameObject);
                if (tier > 0) RelicModels.Build(leaderShown, tier, out rings);
                else rings = new Transform[0];
            }
            leaderShown.Rotate(0f, 30f * Time.deltaTime, 0f, Space.World);
            for (int i = 0; i < rings.Length; i++)
                rings[i].Rotate(new Vector3(i == 1 ? 60f : 0f, i == 2 ? 50f : 0f, i == 0 ? 70f : 25f) * Time.deltaTime, Space.Self);
            glow.intensity = Mathf.MoveTowards(glow.intensity, tier > 0 ? 1.2f + tier * 0.3f : 0.3f, Time.deltaTime);
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return true; } }
        public string Prompt { get { return "Lire le Registre des reliques"; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null) return;
            Game.Hud.OpenPanel(new DialoguePanel(this));
            Sfx.Pop();
        }

        // ================================================================== IDialogue

        public string Speaker { get { return "LE REGISTRE DES RELIQUES"; } }
        public Color Tint { get { return Gold; } }

        public string Body
        {
            get
            {
                // Le classement : du plus puissant au plus faible.
                System.Collections.Generic.List<Seeker> order = new System.Collections.Generic.List<Seeker>(Game.Seekers);
                order.Sort((a, b) => b.Score.CompareTo(a.Score));
                string text = "Les noms sont graves dans la pierre, et changent tout seuls.\n";
                for (int i = 0; i < order.Count; i++)
                {
                    Seeker s = order[i];
                    string what;
                    if (!s.Hoard.StelePlanted) what = "n'a pas encore plante sa stele";
                    else if (s.Score <= 0) what = "sa stele est vide";
                    else what = s.Score + "  --  " + Relic.TierName(Relic.Tier(s.Score));
                    string known = s.IsPlayer ? "" : (Game.Me != null && Game.Me.Knows(s) ? "   (tu sais ou est sa stele)" : "");
                    text += "\n" + (i + 1) + ".  " + s.Name + " : " + what + known;
                }
                return text;
            }
        }

        public int ChoiceCount { get { return 1; } }
        public string ChoiceLabel(int index) { return "Fermer"; }
        public bool ChoiceEnabled(int index) { return true; }
        public bool Choose(int index) { return true; }
    }
}
