using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE VEILLEUR : le dernier garde du chateau.
    ///
    /// Il fait sa ronde autour de la stele, lentement, sa lanterne a la main. Quand
    /// on s'approche, il s'arrete et se tourne vers toi. Il sait des choses : ou
    /// chante le mage, ou dorment les talismans qu'il te manque, si ta relique
    /// compte. Il ne se bat pas, il ne vend rien. Il parle.
    ///
    /// ET IL PREPARE LA PHASE 2 : "On me payait, avant." Un garde qu'on ne paie
    /// plus, c'est exactement le differenciateur du jeu (voir CLAUDE.md). Le
    /// premier PNJ du chateau en parle deja -- quand les gardes arriveront, le
    /// joueur saura pourquoi ils se laissent acheter.
    ///
    /// Concept Unity : il bouge sans physique ("kinematic") : c'est le code qui le
    /// deplace, image par image, et le Rigidbody cinematique previent le moteur
    /// physique qu'il bouge -- le joueur le heurte au lieu de le traverser.
    /// </summary>
    public class Veilleur : MonoBehaviour, IInteractable, IDialogue
    {
        static readonly Vector3[] Route =
        {
            new Vector3(-6.5f, 0f, -4f), new Vector3(-6.5f, 0f, 5f),
            new Vector3(6.5f, 0f, 5f), new Vector3(6.5f, 0f, -4f)
        };

        static readonly Color Cloak = new Color(0.30f, 0.29f, 0.27f);
        static readonly Color CloakDark = new Color(0.21f, 0.20f, 0.19f);
        static readonly Color Skin = new Color(0.58f, 0.50f, 0.44f);
        static readonly Color Steel = new Color(0.38f, 0.39f, 0.41f);
        static readonly Color Wood = new Color(0.25f, 0.19f, 0.13f);
        static readonly Color Voice = new Color(0.90f, 0.80f, 0.60f);

        Walker walker;
        int next = 1;
        float pause;
        int talks;
        int page;               // 0 : l'essentiel, 1 : les talismans, 2 : qui il est

        // ================================================================== construction

        public static Veilleur Build(Transform parent)
        {
            GameObject root = new GameObject("LE VEILLEUR");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Route[0];

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1.05f, 0f);
            capsule.height = 2.1f;
            capsule.radius = 0.42f;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            Veilleur v = root.AddComponent<Veilleur>();

            // Le corps : un vieux soldat de grande taille, dans une longue cape grise.
            Walker.Look look = new Walker.Look();
            look.skin = Skin;
            look.shirt = new Color(0.34f, 0.33f, 0.31f);
            look.legs = CloakDark;
            look.boots = new Color(0.14f, 0.11f, 0.09f);
            look.robe = true;
            look.robeColor = Cloak;
            look.robeDark = CloakDark;
            look.bulk = 1.08f;
            look.height = 2.05f;
            Walker w = Walker.Build(root.transform, "Vieux soldat", look);
            w.HoldPole = true;
            w.HoldLantern = true;
            w.RunSpeed = 5f;
            v.walker = w;

            Proto.BeginVisualOnly();
            Transform head = w.Head;
            // Le casque : une calotte d'acier et un nasal.
            Proto.Sphere(head, new Vector3(0f, 0.2f, -0.01f), new Vector3(0.27f, 0.22f, 0.28f), Steel, "Casque");
            Proto.Cube(head, new Vector3(0f, 0.12f, 0.135f), new Vector3(0.035f, 0.13f, 0.025f), Steel, "Nasal");
            Proto.Cube(head, new Vector3(0f, 0.3f, 0f), new Vector3(0.03f, 0.05f, 0.24f), Palette.Shade(Steel, 1.2f), "Crete");
            // Une barbe grise, courte et carree.
            Proto.Cube(head, new Vector3(0f, -0.01f, 0.1f), new Vector3(0.2f, 0.18f, 0.1f), new Color(0.62f, 0.60f, 0.56f), "Barbe");
            // Une cape agrafee sur l'epaule, un cor en bandouliere.
            Proto.Cube(w.Torso, new Vector3(0f, 0.36f, -0.16f), new Vector3(0.58f, 0.36f, 0.05f), CloakDark, "Cape");
            Proto.Cube(w.Torso, new Vector3(-0.2f, 0.5f, 0.12f), new Vector3(0.07f, 0.07f, 0.03f), new Color(0.6f, 0.5f, 0.3f), "Agrafe");
            GameObject horn = Proto.Cone(w.Hips, new Vector3(0.22f, 0.06f, -0.06f), 0.06f, 0.28f, new Color(0.7f, 0.62f, 0.46f), "Cor", 6);
            horn.transform.localRotation = Quaternion.Euler(0f, 0f, 100f);

            // La hallebarde, tenue droite dans la main droite.
            Transform pole = w.Holder(w.HandR, "Hallebarde");
            Proto.Cube(pole, new Vector3(0f, 0.15f, 0f), new Vector3(0.055f, 2.6f, 0.055f), Wood, "Hampe");
            Proto.Cube(pole, new Vector3(0f, 1.25f, 0.12f), new Vector3(0.03f, 0.34f, 0.22f), Steel, "Fer");
            Proto.Cone(pole, new Vector3(0f, 1.45f, 0f), 0.05f, 0.34f, Steel, "Pique", 4);

            // La lanterne, pendue a la main gauche.
            Transform hang = w.Holder(w.HandL, "Lanterne");
            Vector3 lantern = new Vector3(0f, -0.22f, 0f);
            Proto.Cube(hang, new Vector3(0f, -0.05f, 0f), new Vector3(0.015f, 0.12f, 0.015f), Steel, "Anse");
            Proto.Cube(hang, lantern + new Vector3(0f, 0.12f, 0f), new Vector3(0.19f, 0.04f, 0.19f), Steel, "Lanterne");
            Proto.Cube(hang, lantern - new Vector3(0f, 0.11f, 0f), new Vector3(0.19f, 0.04f, 0.19f), Steel, "Lanterne");
            GameObject flame = Proto.Cube(hang, lantern, new Vector3(0.1f, 0.16f, 0.1f), Color.white, "Flamme");
            flame.GetComponent<Renderer>().sharedMaterial = MaterialFactory.GetGlow(new Color(1f, 0.72f, 0.35f), 2.6f);
            flame.AddComponent<Flame>();
            Proto.EndVisualOnly();

            GameObject lightGo = new GameObject("Lanterne du Veilleur");
            lightGo.transform.SetParent(hang, false);
            lightGo.transform.localPosition = lantern + new Vector3(0f, 0.1f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.intensity = 1.2f;
            light.range = 8f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();

            return v;
        }

        // ================================================================== la ronde

        void Update()
        {
            Transform player = Game.PlayerTransform;
            float near = 99f;
            if (player != null)
            {
                Vector3 d = player.position - transform.position;
                d.y = 0f;
                near = d.magnitude;
            }

            walker.Gaze = near < 8f ? player : null;

            // Quelqu'un approche : il s'arrete et le regarde.
            if (near < 4.5f)
            {
                Figures.Face(transform, player.position, 90f);
                return;
            }

            if (pause > 0f)
            {
                pause -= Time.deltaTime;
                return;
            }

            Vector3 goal = Route[next];
            Vector3 to = goal - transform.localPosition;
            to.y = 0f;
            if (to.magnitude < 0.15f)
            {
                next = (next + 1) % Route.Length;
                pause = 5f;
                return;
            }
            Vector3 step = to.normalized * Mathf.Min(1.0f * Time.deltaTime, to.magnitude);
            transform.localPosition += step;
            Figures.Face(transform, transform.position + step, 120f);
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return true; } }
        public string Prompt { get { return "Parler au Veilleur"; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null) return;
            page = 0;
            talks++;
            Game.Hud.OpenPanel(new DialoguePanel(this));
            Sfx.Pop();
        }

        // ================================================================== IDialogue

        public string Speaker { get { return "LE VEILLEUR"; } }
        public Color Tint { get { return Voice; } }

        public string Body
        {
            get
            {
                if (page == 1) return TalismanLines();
                if (page == 2)
                    return "Je garde ce chateau depuis que le roi a perdu sa tete. On me payait, avant. "
                         + "Plus personne ne me paie.\n\nUn garde qu'on ne paie plus finit toujours par ouvrir "
                         + "la porte a quelqu'un. Souviens-t'en, le jour ou il y aura d'autres gardes que moi.";

                string hello;
                if (talks <= 1) hello = "Encore un. Ils viennent tous pour la stele, un jour ou l'autre.";
                else if (talks % 3 == 0) hello = "La brume est plus epaisse ce soir. Ou alors ce sont mes yeux.";
                else if (talks % 3 == 1) hello = "Tu reviens. C'est bien. Ceux qui ne reviennent pas, je ne les revois pas.";
                else hello = "Parle. Je n'ai que ca a faire, ecouter.";
                return hello + "\n\n" + MageLine() + "\n\n" + RelicLine();
            }
        }

        public int ChoiceCount { get { return page == 0 ? 3 : 2; } }

        public string ChoiceLabel(int index)
        {
            if (page == 0)
            {
                if (index == 0) return "Et les talismans ?";
                if (index == 1) return "Qui es-tu ?";
                return "Adieu";
            }
            return index == 0 ? "Revenir" : "Adieu";
        }

        public bool ChoiceEnabled(int index) { return true; }

        public bool Choose(int index)
        {
            if (page == 0)
            {
                if (index == 0) { page = 1; return false; }
                if (index == 1) { page = 2; return false; }
                return true;
            }
            if (index == 0) { page = 0; return false; }
            return true;
        }

        // ------------------------------------------------------------------ ce qu'il sait

        static string MageLine()
        {
            Season season = Game.Season;
            Mage mage = Game.Mage;
            if (season == null) return "";
            if (season.MagePresent && mage != null && Game.PlayerTransform != null)
                return "Le mage chante en ce moment. Je l'entends " + Hud.Direction(Game.PlayerTransform.position, mage.transform.position)
                       + ". Il repart dans " + Hud.Clock(season.MageTimeLeft) + ".";
            if (season.NextMageIn >= 0f)
                return "Le mage se taira encore " + Hud.Clock(season.NextMageIn) + ". Puis il chantera ailleurs.";
            return "Le mage ne chantera plus. Pose ta relique avant la cloche.";
        }

        static string RelicLine()
        {
            Hoard h = Game.Hoard;
            if (h == null || h.Relic == null)
                return "Tu n'as pas de relique. Le mage la forge avec ce que tu portes. Seulement ce que tu portes.";
            if (!h.StelePlanted)
                return "Tu n'as pas plante ta stele. Choisis ta place (P) : c'est la qu'on viendra te voler.";
            if (h.RelicOnStele)
                return "Ta relique est sur ta stele. Elle vaut " + h.FinalScore + ", pour l'instant. Si personne ne l'a trouvee.";
            return "Ta relique pese dans ton sac. Tant qu'elle n'est pas sur ta stele, elle ne compte pas.";
        }

        static string TalismanLines()
        {
            Hoard h = Game.Hoard;
            if (h == null) return "";
            if (h.TalismanCount >= TalismanInfo.Count)
                return "Tu les as tous les six. Personne n'avait jamais fait ca. Le roi lui-meme n'en avait que quatre.";

            string text = "Il y en a six. On dit :\n";
            int told = 0;
            for (int i = 0; i < TalismanInfo.Count && told < 4; i++)
            {
                Talisman t = TalismanInfo.All[i];
                if (h.Has(t)) continue;
                text += "\n  " + TalismanInfo.Name(t) + " -- " + TalismanInfo.Where(t) + ".";
                told++;
            }
            return text;
        }
    }
}
