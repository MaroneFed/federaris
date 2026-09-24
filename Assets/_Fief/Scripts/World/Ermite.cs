using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'ERMITE, dans la Tour effondree. Voute, barbu, un baton, un feu, une marmite.
    ///
    /// Il connait la foret. Il te dit ou est le lieu-dit que tu n'as pas encore vu,
    /// et ou est le creux a pierres-lune le plus proche. Et il fait une INFUSION :
    /// douze bois mort, et pendant trois minutes, le poids de ton sac ne ralentit
    /// plus tes gestes. C'est la seule chose au monde qui donne du prix au bois mort
    /// au-dela de ce que le mage en fait -- et ca change la facon de jouer : on
    /// garde du bois pour le moment ou l'on va remplir son sac a ras bord.
    ///
    /// Il ne bouge pas. Il se tourne vers toi quand tu approches, vers son feu
    /// quand tu t'en vas.
    /// </summary>
    public class Ermite : MonoBehaviour, IInteractable, IDialogue
    {
        static readonly Color Cloak = new Color(0.25f, 0.27f, 0.19f);
        static readonly Color CloakDark = new Color(0.18f, 0.19f, 0.14f);
        static readonly Color Skin = new Color(0.52f, 0.44f, 0.38f);
        static readonly Color Beard = new Color(0.74f, 0.72f, 0.68f);
        static readonly Color Wood = new Color(0.26f, 0.20f, 0.14f);
        static readonly Color Voice = new Color(0.66f, 0.84f, 0.56f);

        Walker walker;
        Vector3 fireWorld;
        int page;
        bool met;
        bool firstTime;

        public static Ermite Build(Transform tower)
        {
            // Dans la tour (rayon interieur ~3,3 m), porte au sud : le feu au milieu,
            // l'ermite de l'autre cote, face a la porte.
            Vector3 fire = new Vector3(-0.3f, 0f, -0.6f);
            GameObject root = new GameObject("L'ERMITE");
            root.transform.SetParent(tower, false);
            root.transform.localPosition = new Vector3(0.6f, 0f, 1.5f);

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.45f;

            Ermite e = root.AddComponent<Ermite>();
            e.fireWorld = tower.TransformPoint(fire);

            // Le corps : un vieil homme voute dans une robe de bure, capuche baissee,
            // une longue barbe, un baton noueux.
            Walker.Look look = new Walker.Look();
            look.skin = Skin;
            look.shirt = CloakDark;
            look.legs = CloakDark;
            look.boots = new Color(0.2f, 0.15f, 0.1f);
            look.robe = true;
            look.robeColor = Cloak;
            look.robeDark = CloakDark;
            look.height = 1.75f;
            Walker w = Walker.Build(root.transform, "Vieil homme", look);
            w.Stoop = 16f;
            w.HoldPole = true;
            e.walker = w;
            ModelSkin.TryDress(w, "Ermite", look.height, 0);

            Proto.BeginVisualOnly();
            Transform head = w.Head;
            // Capuche rabattue sur les epaules, crane degarni, sourcils broussailleux.
            Proto.Cube(w.Neck, new Vector3(0f, -0.02f, -0.12f), new Vector3(0.4f, 0.16f, 0.2f), Cloak, "Capuche");
            Proto.Cube(head, new Vector3(0f, 0.12f, -0.03f), new Vector3(0.25f, 0.18f, 0.22f), Beard, "Cheveux");
            Proto.Cube(head, new Vector3(0f, 0.19f, 0.115f), new Vector3(0.17f, 0.03f, 0.03f), Beard, "Sourcils");
            GameObject beard = Proto.Cube(head, new Vector3(0f, -0.1f, 0.1f), new Vector3(0.19f, 0.38f, 0.08f), Beard, "Barbe");
            beard.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Proto.Cube(head, new Vector3(0f, 0.06f, 0.125f), new Vector3(0.12f, 0.035f, 0.03f), Beard, "Moustache");
            // Une besace, et un chapelet de champignons seches a la ceinture.
            GameObject satchel = Proto.Cube(w.Hips, new Vector3(0.24f, -0.12f, 0.02f), new Vector3(0.1f, 0.24f, 0.26f), new Color(0.32f, 0.24f, 0.16f), "Besace");
            satchel.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            for (int k = 0; k < 4; k++)
                Proto.Cube(w.Hips, new Vector3(-0.2f, -0.06f - k * 0.07f, 0.1f), new Vector3(0.06f, 0.05f, 0.06f), new Color(0.6f, 0.5f, 0.36f), "Champignon");

            Transform staff = w.Holder(w.HandR, "Bâton");
            GameObject shaft = Proto.Cube(staff, new Vector3(0f, -0.05f, 0f), new Vector3(0.06f, 2.0f, 0.06f), Wood, "Bâton");
            shaft.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            GameObject knot = Proto.Cube(staff, new Vector3(-0.04f, 0.96f, 0f), new Vector3(0.14f, 0.12f, 0.12f), Palette.Shade(Wood, 0.8f), "Noeud");
            knot.transform.localRotation = Quaternion.Euler(20f, 30f, 10f);
            Proto.EndVisualOnly();

            Campfire(tower, fire);
            e.transform.rotation = Quaternion.LookRotation(Flat(e.fireWorld - e.transform.position), Vector3.up);
            return e;
        }

        /// <summary>Un feu allume : pierres, buches, flammes, marmite sur trepied, et sa lumiere.</summary>
        static void Campfire(Transform t, Vector3 at)
        {
            Proto.BeginVisualOnly();
            for (int k = 0; k < 7; k++)
            {
                float a = k / 7f * Mathf.PI * 2f;
                Proto.Cube(t, at + new Vector3(Mathf.Cos(a) * 0.55f, 0.08f, Mathf.Sin(a) * 0.55f),
                           new Vector3(0.24f, 0.16f, 0.2f), new Color(0.26f, 0.26f, 0.27f), "Pierre");
            }
            for (int k = 0; k < 3; k++)
            {
                GameObject log = Proto.Cube(t, at + new Vector3(0f, 0.12f, 0f), new Vector3(0.12f, 0.12f, 0.8f), Wood, "Bûche");
                log.transform.localRotation = Quaternion.Euler(8f, k * 60f, 0f);
            }
            Material flame = MaterialFactory.GetGlow(new Color(1f, 0.56f, 0.2f), 2.4f);
            for (int k = 0; k < 3; k++)
            {
                GameObject f = Proto.Cube(t, at + new Vector3((k - 1) * 0.12f, 0.35f, (k % 2) * 0.1f),
                                          new Vector3(0.18f, 0.4f - k * 0.06f, 0.18f), Color.white, "Flamme");
                f.GetComponent<Renderer>().sharedMaterial = flame;
                f.AddComponent<Flame>();
            }
            // Le trepied et la marmite.
            for (int k = 0; k < 3; k++)
            {
                float a = k / 3f * 360f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                GameObject leg = Proto.Cube(t, at + dir * 0.45f + new Vector3(0f, 0.6f, 0f), new Vector3(0.04f, 1.3f, 0.04f),
                                            new Color(0.12f, 0.12f, 0.13f), "Trépied");
                leg.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(20f, 0f, 0f);
            }
            Proto.Cylinder(t, at + new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.18f, 0.46f), new Color(0.1f, 0.1f, 0.11f), "Marmite");
            Proto.EndVisualOnly();
            Proto.Blocker(t, at + new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 1.2f), "Feu");

            GameObject lightGo = new GameObject("Feu de l'Ermite");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = at + new Vector3(0f, 0.9f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.3f);
            light.intensity = 1.6f;
            light.range = 10f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<LampFlicker>();
            Ambiance.Embers(t, at + new Vector3(0f, 0.5f, 0f));
        }

        void Update()
        {
            Transform player = Game.PlayerTransform;
            bool near = player != null && Flat(player.position - transform.position).magnitude < 6f;
            walker.Gaze = near ? player : null;
            Figures.Face(transform, near ? player.position : fireWorld, 60f);
        }

        // ================================================================== IInteractable

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return true; } }
        public string Prompt { get { return "Parler à l'Ermite"; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null) return;
            page = 0;
            firstTime = !met;
            met = true;
            Game.Hud.OpenPanel(new DialoguePanel(this));
            Sfx.Pop();
        }

        // ================================================================== IDialogue

        public string Speaker { get { return "L'ERMITE"; } }
        public Color Tint { get { return Voice; } }

        public string Body
        {
            get
            {
                if (page == 1) return HollowLine();

                string hello = firstTime
                    ? "Ah. Quelqu'un qui marche au lieu de courir. Assieds-toi, le feu est pour tout le monde."
                    : "Te revoilà. Le feu t'attendait.";

                string brew;
                if (Game.Brewed)
                    brew = "Mon infusion te tient encore " + Hud.Clock(Game.Hoard.BrewUntil - Game.Season.Elapsed) + ".";
                else
                    brew = "Mon infusion : " + Hoard.BrewCost + " bois mort, et pendant trois minutes ton sac ne pèsera "
                         + "plus sur tes gestes. Tu récolteras charge comme si tu étais léger.";
                return hello + "\n\n" + LandmarkLine() + "\n\n" + brew;
            }
        }

        public int ChoiceCount { get { return page == 0 ? 3 : 2; } }

        public string ChoiceLabel(int index)
        {
            if (page == 0)
            {
                if (index == 0) return "Donner " + Hoard.BrewCost + " bois mort pour l'infusion";
                if (index == 1) return "Ou trouver des pierres-lune ?";
                return "Adieu";
            }
            return index == 0 ? "Revenir" : "Adieu";
        }

        public bool ChoiceEnabled(int index)
        {
            if (page != 0 || index != 0) return true;
            return Game.Hoard != null && Game.Inventory != null && Game.Season != null && Game.Season.Running
                   && !Game.Brewed && Game.Inventory.Get(ResourceType.Deadwood) >= Hoard.BrewCost;
        }

        public bool Choose(int index)
        {
            if (page == 0)
            {
                if (index == 0)
                {
                    if (Game.Hoard.RequestBrew(Game.Inventory, Game.Season.Elapsed))
                    {
                        Sfx.Build();
                        Toasts.Show("L'infusion est amère et brûlante. Pendant trois minutes, ton sac ne pèse plus sur tes gestes.",
                                    Voice);
                    }
                    return false;
                }
                if (index == 1) { page = 1; return false; }
                return true;
            }
            if (index == 0) { page = 0; return false; }
            return true;
        }

        // ------------------------------------------------------------------ ce qu'il sait

        /// <summary>Le lieu-dit le plus proche que tu n'as pas encore vu.</summary>
        string LandmarkLine()
        {
            Transform player = Game.PlayerTransform;
            if (player == null) return "";
            Landmark best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Landmarks.All.Count; i++)
            {
                Landmark m = Landmarks.All[i];
                if (m == null || m.Discovered || m.transform == transform.parent) continue;
                float d = Flat(m.transform.position - player.position).magnitude;
                if (d < bestDistance) { bestDistance = d; best = m; }
            }
            if (best == null) return "Tu connais la sylve mieux que moi, maintenant. Ça arrive rarement.";
            return Landmarks.Name(best.kind) + " est " + Hud.Direction(player.position, best.transform.position)
                   + ", a " + Paces(bestDistance) + " pas d'ici. Tu n'y es jamais allé, ça se voit.";
        }

        static string HollowLine()
        {
            Transform player = Game.PlayerTransform;
            if (player == null || Gathering.HollowSpotCount == 0) return "Je ne sais pas.";
            Vector2 me = new Vector2(player.position.x, player.position.z);
            float best = float.MaxValue;
            Vector2 spot = me;
            for (int i = 0; i < Gathering.HollowSpotCount; i++)
            {
                Vector2 h = Gathering.HollowSpot(i);
                float d = (h - me).magnitude;
                if (d < best) { best = d; spot = h; }
            }
            return "Le creux le plus proche est " + Hud.Direction(player.position, new Vector3(spot.x, 0f, spot.y))
                   + ", a " + Paces(best) + " pas. Les pierres y luisent, tu ne peux pas le rater.\n\n"
                   + "Et si tu croises un feu-follet, suis-le. Ils vont toujours là où les pierres chantent.";
        }

        /// <summary>Un pas, c'est trois quarts de metre. On compte en pas dans la sylve.</summary>
        static int Paces(float metres)
        {
            return Mathf.RoundToInt(metres / 0.75f / 10f) * 10;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v : Vector3.forward;
        }
    }
}
