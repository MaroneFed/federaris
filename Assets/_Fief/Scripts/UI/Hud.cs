using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// Le HUD : or, prestige, sac, poids, invite d'interaction, reperes a l'ecran.
    ///
    /// Il ne decide RIEN. Il lit l'etat du jeu et l'affiche. La souris, la pause et
    /// le verrouillage des entrees sont geres au meme endroit, dans Menus.cs.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public OrbitCamera orbitCamera;
        public PlayerInteractor interactor;
        public Camera viewCamera;
        public Menus menus;

        IPanel panel;
        bool showHelp = true;
        bool showDiagnostic;
        float helpTimer = 22f;

        public bool PanelOpen { get { return panel != null; } }

        public void OpenPanel(IPanel newPanel) { panel = newPanel; }
        public void ClosePanel() { panel = null; }

        void Update()
        {
            Toasts.Tick(Time.unscaledDeltaTime);
            FloatingTexts.Tick(Time.unscaledDeltaTime);

            if (panel != null && !panel.IsStillValid) panel = null;

            if (FiefInput.ToggleViewPressed && orbitCamera != null)
            {
                orbitCamera.SetFirstPerson(!orbitCamera.firstPerson);
                Toasts.Show(orbitCamera.firstPerson ? "Vue a la premiere personne" : "Vue a la troisieme personne",
                            Palette.Gold);
            }

            if (FiefInput.DiagnosticPressed) showDiagnostic = !showDiagnostic;

            // F4 : on efface le vetement. Si l'ecran se degage, le coupable est trouve.
            if (FiefInput.ToggleClothPressed && Game.Rig != null) Game.Rig.ToggleCloth();

            if (FiefInput.HelpPressed)
            {
                showHelp = !showHelp;
                helpTimer = showHelp ? 99999f : 0f;
            }
            else if (helpTimer > 0f && helpTimer < 99999f)
            {
                helpTimer -= Time.deltaTime;
                if (helpTimer <= 0f) showHelp = false;
            }
        }

        bool Hidden { get { return menus != null && menus.Blocking; } }

        void OnGUI()
        {
            UiStyle.Ensure();
            if (Hidden) return;

            DrawMarkers();
            FloatingTexts.Draw(viewCamera != null ? viewCamera : Camera.main);
            DrawPurse();
            DrawPack();
            DrawPrompt();
            Toasts.Draw();
            DrawHelp();
            DrawBuildError();
            if (showDiagnostic) DrawDiagnostic();

            if (panel != null) panel.Draw();
        }

        // ---------------------------------------------------------------- la bourse

        void DrawPurse()
        {
            if (Game.Wallet == null || Game.Fief == null) return;

            float pad = UiStyle.S(16);
            float w = UiStyle.S(286);
            float h = UiStyle.S(96);
            Rect box = new Rect(pad, pad, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float y = box.y + UiStyle.S(12);
            float inner = w - UiStyle.S(32);

            UiStyle.Chip(new Rect(x, y + UiStyle.S(7), UiStyle.S(16), UiStyle.S(16)), Palette.Gold);
            UiStyle.Tinted(new Rect(x + UiStyle.S(24), y, inner, UiStyle.S(30)),
                           Game.Wallet.Gold.ToString(), UiStyle.Value, Palette.Gold);
            GUIStyle right = UiStyle.Small;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(30)), "OR", right);
            right.alignment = previous;

            y += UiStyle.S(32);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(7);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(20)),
                      Game.Fief.prestige + " prestige      " + Game.Fief.buildingCount + "/6 batiments",
                      UiStyle.Small);
            y += UiStyle.S(20);

            string reserve = "Reserve  " + Game.Fief.Stock(ResourceType.Wood) + " bois   "
                           + Game.Fief.Stock(ResourceType.Stone) + " pierre   "
                           + Game.Fief.Stock(ResourceType.Iron) + " fer";
            if (!Game.Fief.hasChest) reserve = "Reserve verrouillee - construis un Coffre";
            GUI.Label(new Rect(x, y, inner, UiStyle.S(18)), reserve, UiStyle.Tiny);
        }

        // ---------------------------------------------------------------- le sac

        void DrawPack()
        {
            Inventory inv = Game.Inventory;
            if (inv == null) return;

            float pad = UiStyle.S(16);
            float w = UiStyle.S(300);
            float h = UiStyle.S(184);
            Rect box = new Rect(pad, Screen.height - h - pad, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float inner = w - UiStyle.S(32);
            float y = box.y + UiStyle.S(12);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "SAC", UiStyle.Head);

            GUIStyle right = UiStyle.Small;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)),
                      Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg", right);
            right.alignment = previous;

            y += UiStyle.S(28);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int amount = inv.Get(type);

                if (i % 2 == 0)
                    UiStyle.Fill(new Rect(x - UiStyle.S(6), y - UiStyle.S(1), inner + UiStyle.S(12), UiStyle.S(24)),
                                 new Color(1f, 1f, 1f, 0.028f));

                UiStyle.Chip(new Rect(x, y + UiStyle.S(5), UiStyle.S(13), UiStyle.S(13)), ResourceInfo.Tint(type));
                UiStyle.Tinted(new Rect(x + UiStyle.S(21), y, UiStyle.S(120), UiStyle.S(22)),
                               ResourceInfo.Name(type), UiStyle.Label,
                               amount > 0 ? UiStyle.Ink : UiStyle.InkFaint);

                right.alignment = TextAnchor.MiddleRight;
                UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(22)), amount.ToString(), right,
                               amount > 0 ? UiStyle.Ink : UiStyle.InkFaint);
                right.alignment = previous;

                y += UiStyle.S(24);
            }

            // --- la jauge de charge
            y = box.yMax - UiStyle.S(58);
            float load = inv.Load01;
            Color fill = Color.Lerp(new Color(0.44f, 0.78f, 0.40f),
                                    new Color(0.88f, 0.31f, 0.25f), Mathf.Pow(load, 0.85f));
            UiStyle.Bar(new Rect(x, y, inner, UiStyle.S(12)), load, fill, UiStyle.BarBg);

            // --- ce que la charge coute VRAIMENT : la lenteur des gestes
            y += UiStyle.S(17);
            GameConfig cfg = Game.Config;
            float penalty = cfg != null ? Mathf.Lerp(1f, cfg.actionPenaltyFull, load) : 1f;
            float harvest = cfg != null ? cfg.harvestDuration * penalty : 0f;

            UiStyle.Tinted(new Rect(x, y, inner, UiStyle.S(18)),
                           "Gestes  x" + penalty.ToString("0.0") + "   (" + harvest.ToString("0.0") + " s par coup)",
                           UiStyle.Small,
                           penalty > 1.6f ? new Color(0.90f, 0.55f, 0.30f) : UiStyle.InkDim);

            y += UiStyle.S(18);
            float speed = Game.Player != null ? Game.Player.TargetSpeed : 0f;
            bool sprinting = Game.Player != null && Game.Player.IsSprinting;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(18)),
                      "Vitesse  " + speed.ToString("0.0") + " m/s" + (sprinting ? "   (course)" : ""),
                      UiStyle.Tiny);
        }

        // ---------------------------------------------------------------- invite

        void DrawPrompt()
        {
            if (panel != null || interactor == null) return;

            IInteractable target = interactor.Current;
            if (target == null) return;

            float w = UiStyle.S(430);
            float h = UiStyle.S(50);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(216), w, h);

            UiStyle.DropShadow(box, UiStyle.S(14));
            GUI.Box(box, GUIContent.none, UiStyle.CardBox);

            // touche
            float capW = UiStyle.S(34);
            Rect cap = new Rect(box.x + UiStyle.S(13), box.y + (h - capW) * 0.5f, capW, capW);
            UiStyle.Pill(cap);
            UiStyle.Tinted(cap, "E", UiStyle.Centered, Palette.Gold);

            GUIStyle label = UiStyle.Label;
            GUI.Label(new Rect(cap.xMax + UiStyle.S(13), box.y, box.width - capW - UiStyle.S(40), h * 0.62f),
                      target.Prompt, label);

            string hint = target.HoldDuration > 0f ? "maintenir" : "appuyer";
            GUI.Label(new Rect(cap.xMax + UiStyle.S(13), box.y + h * 0.54f,
                               box.width - capW - UiStyle.S(40), h * 0.42f), hint, UiStyle.Tiny);

            if (target.HoldDuration > 0f && interactor.HoldProgress01 > 0.001f)
            {
                Rect bar = new Rect(box.x + UiStyle.S(6), box.yMax - UiStyle.S(5),
                                    box.width - UiStyle.S(12), UiStyle.S(4));
                UiStyle.Bar(bar, interactor.HoldProgress01, Palette.Gold, new Color(0f, 0f, 0f, 0.5f));
            }
        }

        // ---------------------------------------------------------------- reperes

        void DrawMarkers()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            DrawMarker(cam, Game.MarketPosition + Vector3.up * 10f, "MARCHE", Palette.Gold);
            DrawMarker(cam, Game.HomeFiefPosition + Vector3.up * 10f, "TON FIEF", Palette.Banner(0));

            if (Game.Fief != null && Game.Fief.hasWatchtower)
            {
                for (int i = 0; i < ResourceNode.All.Count; i++)
                {
                    ResourceNode node = ResourceNode.All[i];
                    if (node == null || node.IsDepleted) continue;
                    DrawDot(cam, node.transform.position + Vector3.up * 4f, ResourceInfo.Tint(node.type));
                }
            }
        }

        void DrawMarker(Camera cam, Vector3 world, string text, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;

            float margin = UiStyle.S(66);
            float x = Mathf.Clamp(sp.x, margin, Screen.width - margin);
            float y = Mathf.Clamp(Screen.height - sp.y, margin, Screen.height - margin);

            string label = text;
            if (Game.PlayerTransform != null)
                label += "  " + Mathf.RoundToInt(Vector3.Distance(Game.PlayerTransform.position, world)) + " m";

            float dotSize = UiStyle.S(9);
            UiStyle.Fill(new Rect(x - dotSize * 0.5f, y - dotSize * 0.5f, dotSize, dotSize),
                         new Color(0f, 0f, 0f, 0.5f));
            UiStyle.Fill(new Rect(x - dotSize * 0.5f + 1f, y - dotSize * 0.5f + 1f, dotSize - 2f, dotSize - 2f), color);

            GUIStyle style = UiStyle.CenteredSmall;
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(x - UiStyle.S(75) + 1f, y - UiStyle.S(29) + 1f, UiStyle.S(150), UiStyle.S(20)), label, style);
            style.normal.textColor = color;
            GUI.Label(new Rect(x - UiStyle.S(75), y - UiStyle.S(29), UiStyle.S(150), UiStyle.S(20)), label, style);
            style.normal.textColor = original;
        }

        void DrawDot(Camera cam, Vector3 world, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            if (sp.x < 0f || sp.x > Screen.width) return;

            float y = Screen.height - sp.y;
            if (y < 0f || y > Screen.height) return;

            float size = UiStyle.S(6);
            UiStyle.Fill(new Rect(sp.x - size * 0.5f, y - size * 0.5f, size, size),
                         new Color(color.r, color.g, color.b, 0.8f));
        }

        // ---------------------------------------------------------------- diagnostic

        /// <summary>
        /// Une panne pendant la construction du monde s'affiche en grand, en rouge.
        /// Plus besoin d'aller chercher dans la Console : le jeu dit ce qui a casse.
        /// </summary>
        void DrawBuildError()
        {
            if (string.IsNullOrEmpty(Game.BuildError)) return;

            float w = Mathf.Min(Screen.width - UiStyle.S(40), UiStyle.S(760));
            float h = UiStyle.S(150);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(120), w, h);

            UiStyle.DropShadow(box, UiStyle.S(18));
            UiStyle.Fill(box, new Color(0.22f, 0.05f, 0.05f, 0.96f));
            UiStyle.Fill(new Rect(box.x, box.y, box.width, 3f), new Color(0.90f, 0.25f, 0.20f));

            float x = box.x + UiStyle.S(16);
            UiStyle.Tinted(new Rect(x, box.y + UiStyle.S(10), w, UiStyle.S(26)),
                           "LA CONSTRUCTION DU MONDE A ECHOUE", UiStyle.Head, new Color(1f, 0.55f, 0.45f));

            GUIStyle wrapped = UiStyle.Small;
            bool previousWrap = wrapped.wordWrap;
            wrapped.wordWrap = true;
            GUI.Label(new Rect(x, box.y + UiStyle.S(38), w - UiStyle.S(32), h - UiStyle.S(48)),
                      Game.BuildError, wrapped);
            wrapped.wordWrap = previousWrap;
        }

        /// <summary>
        /// Panneau F3. Il repond a la question "je suis dans quoi ?" : il liste ce
        /// dont la boite englobante contient la camera, et ce que touche un rayon
        /// tire vers l'avant. C'est ce qui remplace les allers-retours a l'aveugle.
        /// </summary>
        void DrawDiagnostic()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;

            float w = UiStyle.S(500);
            float h = UiStyle.S(334);
            Rect box = new Rect((Screen.width - w) * 0.5f, UiStyle.S(90), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float y = box.y + UiStyle.S(12);
            float inner = w - UiStyle.S(32);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(24)), "DIAGNOSTIC  (F3)", UiStyle.Head);
            y += UiStyle.S(26);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            y = Line(x, y, inner, "Monde construit en", Game.BuildMilliseconds + " ms");
            y = Line(x, y, inner, "Vue",
                     orbitCamera != null && orbitCamera.firstPerson ? "premiere personne" : "troisieme personne");

            if (Game.PlayerTransform != null)
            {
                Vector3 p = Game.PlayerTransform.position;
                y = Line(x, y, inner, "Joueur",
                         p.x.ToString("0") + " / " + p.y.ToString("0.0") + " / " + p.z.ToString("0"));
                y = Line(x, y, inner, "Sol sous les pieds", Ground.Sample(p.x, p.z).ToString("0.0") + " m");
            }

            if (cam != null)
            {
                Vector3 c = cam.transform.position;
                y = Line(x, y, inner, "Oeil",
                         c.x.ToString("0.0") + " / " + c.y.ToString("0.0") + " / " + c.z.ToString("0.0"));

                // --- DANS QUOI SOMMES-NOUS ?
                //
                // "Contains" ne repondait qu'a moitie : un objet peut remplir l'ecran
                // sans contenir l'oeil -- c'est le cas d'un vetement, dont l'oeil sort
                // par le col. On mesure donc aussi la DISTANCE a chaque morceau du
                // corps, et on nomme les deux plus proches. C'est cette ligne-la qui
                // dit en un mot ce qui bouche la vue.
                string inside = "rien";
                string nearest = "rien";
                if (Game.Rig != null)
                {
                    Renderer[] parts = Game.Rig.GetComponentsInChildren<Renderer>(false);

                    string firstName = null, secondName = null;
                    float firstDist = 99f, secondDist = 99f;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        // ShadowsOnly = la piece porte encore son ombre mais n'est
                        // plus dessinee : elle ne peut donc plus boucher la vue.
                        if (parts[i] == null || !parts[i].enabled) continue;
                        if (parts[i].shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;

                        if (inside == "rien" && parts[i].bounds.Contains(c))
                            inside = parts[i].gameObject.name + " (ton personnage)";

                        float d = Vector3.Distance(parts[i].bounds.ClosestPoint(c), c);
                        if (d < firstDist)
                        {
                            secondDist = firstDist; secondName = firstName;
                            firstDist = d; firstName = parts[i].gameObject.name;
                        }
                        else if (d < secondDist)
                        {
                            secondDist = d; secondName = parts[i].gameObject.name;
                        }
                    }

                    if (firstName != null)
                    {
                        nearest = firstName + " a " + Mathf.RoundToInt(firstDist * 100f) + " cm";
                        if (secondName != null)
                            nearest += ",  " + secondName + " a " + Mathf.RoundToInt(secondDist * 100f) + " cm";
                    }
                }
                y = Line(x, y, inner, "Camera a l'interieur de", inside);
                y = Line(x, y, inner, "Colle a l'oeil", nearest);

                Collider[] touching = Physics.OverlapSphere(c, 0.25f, ~0, QueryTriggerInteraction.Ignore);
                y = Line(x, y, inner, "Solides autour de l'oeil",
                         touching.Length == 0 ? "aucun" : touching[0].gameObject.name
                             + (touching.Length > 1 ? " +" + (touching.Length - 1) : ""));

                RaycastHit hit;
                string ahead = "rien a moins de 40 m";
                if (Physics.Raycast(c, cam.transform.forward, out hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                    ahead = hit.collider.gameObject.name + " a " + hit.distance.ToString("0.0") + " m";
                y = Line(x, y, inner, "Devant toi", ahead);
            }

            y += UiStyle.S(6);
            GUI.Label(new Rect(x, y, inner, UiStyle.S(34)),
                      "Lis-moi \"Colle a l'oeil\".   F4 efface le poncho : si la masse\n"
                      + "disparait c'est le vetement, sinon c'est autre chose.", UiStyle.Tiny);
        }

        float Line(float x, float y, float width, string label, string value)
        {
            GUI.Label(new Rect(x, y, width * 0.52f, UiStyle.S(20)), label, UiStyle.Small);
            UiStyle.Tinted(new Rect(x + width * 0.52f, y, width * 0.48f, UiStyle.S(20)), value,
                           UiStyle.Small, Palette.Gold);
            return y + UiStyle.S(21);
        }

        // ---------------------------------------------------------------- aide

        void DrawHelp()
        {
            if (!showHelp) return;

            float w = UiStyle.S(300);
            float h = UiStyle.S(235);
            Rect box = new Rect(Screen.width - w - UiStyle.S(16), UiStyle.S(16), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(16);
            float inner = w - UiStyle.S(32);
            float y = box.y + UiStyle.S(12);

            GUI.Label(new Rect(x, y, inner, UiStyle.S(22)), "COMMANDES", UiStyle.Head);
            GUIStyle right = UiStyle.Tiny;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, inner, UiStyle.S(22)), "F1", right);
            right.alignment = previous;

            y += UiStyle.S(26);
            UiStyle.Rule(new Rect(x, y, inner, 1f));
            y += UiStyle.S(8);

            string[,] rows =
            {
                { "ZQSD", "se deplacer" },
                { "Maj", "courir" },
                { "Souris", "camera" },
                { "E", "recolter, interagir" },
                { "V", "changer de vue" },
                { "F3", "diagnostic" },
                { "F4", "masquer le poncho" },
                { "Echap", "pause" }
            };

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                UiStyle.Tinted(new Rect(x, y, UiStyle.S(78), UiStyle.S(20)), rows[i, 0], UiStyle.Small, Palette.Gold);
                GUI.Label(new Rect(x + UiStyle.S(84), y, inner - UiStyle.S(84), UiStyle.S(20)), rows[i, 1], UiStyle.Small);
                y += UiStyle.S(21);
            }
        }
    }
}
