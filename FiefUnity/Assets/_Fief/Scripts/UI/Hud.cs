using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le HUD : or, prestige, inventaire, jauge de poids, invite d'interaction,
    /// reperes a l'ecran, et le panneau modal eventuellement ouvert.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public OrbitCamera orbitCamera;
        public PlayerInteractor interactor;
        public Camera viewCamera;

        IPanel panel;
        bool wasOpen;
        bool showHelp = true;
        float helpTimer = 26f;

        public bool PanelOpen { get { return panel != null; } }

        public void OpenPanel(IPanel newPanel)
        {
            panel = newPanel;
        }

        public void ClosePanel()
        {
            panel = null;
        }

        void Update()
        {
            Toasts.Tick(Time.deltaTime);
            FloatingTexts.Tick(Time.deltaTime);

            if (panel != null && !panel.IsStillValid) panel = null;

            if (FiefInput.CancelPressed)
            {
                if (panel != null) panel = null;
                else Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                        ? CursorLockMode.None : CursorLockMode.Locked;
            }

            if (FiefInput.HelpPressed)
            {
                showHelp = !showHelp;
                helpTimer = showHelp ? 9999f : 0f;
            }
            else if (helpTimer > 0f)
            {
                helpTimer -= Time.deltaTime;
                if (helpTimer <= 0f) showHelp = false;
            }

            bool open = panel != null;

            // Un panneau ouvert = souris libre et joueur fige.
            // A la fermeture, on reprend la main sur la camera.
            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (wasOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            wasOpen = open;

            if (Game.Player != null) Game.Player.InputLocked = open;
            if (orbitCamera != null) orbitCamera.InputLocked = open;
            if (interactor != null) interactor.InputLocked = open;
        }

        void OnGUI()
        {
            UiStyle.Ensure();

            DrawMarkers();
            FloatingTexts.Draw(viewCamera != null ? viewCamera : Camera.main);
            DrawTopBar();
            DrawInventory();
            DrawPrompt();
            Toasts.Draw();
            DrawHelp();

            if (panel != null) panel.Draw();
        }

        // ---------------------------------------------------------------- top bar

        void DrawTopBar()
        {
            if (Game.Wallet == null || Game.Fief == null) return;

            float pad = UiStyle.S(14);
            float w = UiStyle.S(300);
            float h = UiStyle.S(78);
            Rect box = new Rect(pad, pad, w, h);
            UiStyle.Frame(box);

            Rect line1 = new Rect(box.x + UiStyle.S(12), box.y + UiStyle.S(8), box.width - UiStyle.S(24), UiStyle.S(26));
            UiStyle.Fill(new Rect(line1.x, line1.y + UiStyle.S(5), UiStyle.S(14), UiStyle.S(14)), Palette.Gold);
            GUI.Label(new Rect(line1.x + UiStyle.S(22), line1.y, line1.width, line1.height),
                      Game.Wallet.Gold + " or", UiStyle.Value);

            Rect line2 = new Rect(box.x + UiStyle.S(12), box.y + UiStyle.S(38), box.width - UiStyle.S(24), UiStyle.S(22));
            GUI.Label(line2, "Prestige " + Game.Fief.prestige
                             + "   |   Batiments " + Game.Fief.buildingCount + "/6", UiStyle.Label);

            Rect line3 = new Rect(box.x + UiStyle.S(12), box.y + UiStyle.S(57), box.width - UiStyle.S(24), UiStyle.S(18));
            string stock = "Reserve : " + Game.Fief.Stock(ResourceType.Wood) + " bois, "
                         + Game.Fief.Stock(ResourceType.Stone) + " pierre, "
                         + Game.Fief.Stock(ResourceType.Iron) + " fer";
            if (!Game.Fief.hasChest) stock += "   (Coffre requis pour y acceder)";
            GUI.Label(line3, stock, UiStyle.Small);
        }

        // ---------------------------------------------------------------- inventaire + poids

        void DrawInventory()
        {
            Inventory inv = Game.Inventory;
            if (inv == null) return;

            float pad = UiStyle.S(14);
            float w = UiStyle.S(280);
            float h = UiStyle.S(148);
            Rect box = new Rect(pad, Screen.height - h - pad, w, h);
            UiStyle.Frame(box);

            GUI.Label(new Rect(box.x + UiStyle.S(12), box.y + UiStyle.S(6), box.width, UiStyle.S(22)),
                      "SAC", UiStyle.Head);

            float y = box.y + UiStyle.S(32);
            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                Rect row = new Rect(box.x + UiStyle.S(12), y, box.width - UiStyle.S(24), UiStyle.S(22));

                UiStyle.Fill(new Rect(row.x, row.y + UiStyle.S(4), UiStyle.S(13), UiStyle.S(13)),
                             ResourceInfo.Tint(type));
                GUI.Label(new Rect(row.x + UiStyle.S(22), row.y, UiStyle.S(110), row.height),
                          ResourceInfo.Name(type), UiStyle.Label);

                GUIStyle right = UiStyle.Label;
                TextAnchor previous = right.alignment;
                right.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(row.x, row.y, row.width, row.height), inv.Get(type).ToString(), right);
                right.alignment = previous;

                y += UiStyle.S(23);
            }

            // Jauge de poids : verte -> rouge a mesure qu'on se charge.
            float load = inv.Load01;
            Rect bar = new Rect(box.x + UiStyle.S(12), box.y + h - UiStyle.S(34), box.width - UiStyle.S(24), UiStyle.S(14));
            Color fill = Color.Lerp(new Color(0.42f, 0.76f, 0.38f), new Color(0.85f, 0.29f, 0.24f), load);
            UiStyle.Bar(bar, load, fill, UiStyle.BarBg);

            string weightText = Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg";
            GUI.Label(bar, weightText, UiStyle.Centered);

            float speed = Game.Player != null ? Game.Player.TargetSpeed : 0f;
            float full = Game.Config != null ? Game.Config.moveSpeedEmpty : 1f;
            int percent = Mathf.RoundToInt((1f - (full <= 0f ? 0f : speed / full)) * 100f);
            GUI.Label(new Rect(box.x + UiStyle.S(12), box.y + h - UiStyle.S(19), box.width, UiStyle.S(16)),
                      "Vitesse " + speed.ToString("0.0") + " m/s"
                      + (Game.Player != null && Game.Player.IsSprinting
                            ? "   (course)"
                            : percent > 0 ? "   (-" + percent + "%)" : "   (Maj pour courir)"),
                      UiStyle.Small);
        }

        // ---------------------------------------------------------------- invite d'interaction

        void DrawPrompt()
        {
            if (panel != null || interactor == null) return;

            IInteractable target = interactor.Current;
            if (target == null) return;

            float w = UiStyle.S(440);
            float h = UiStyle.S(44);
            Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyle.S(210), w, h);
            UiStyle.Fill(box, new Color(0f, 0f, 0f, 0.55f));

            string key = target.HoldDuration > 0f ? "[E] maintenir" : "[E]";
            UiStyle.Shadowed(box, key + "   " + target.Prompt, UiStyle.Centered);

            if (target.HoldDuration > 0f && interactor.HoldProgress01 > 0.001f)
            {
                Rect bar = new Rect(box.x, box.yMax, box.width, UiStyle.S(5));
                UiStyle.Bar(bar, interactor.HoldProgress01, Palette.Gold, UiStyle.BarBg);
            }
        }

        // ---------------------------------------------------------------- reperes dans le monde

        void DrawMarkers()
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null) return;

            DrawMarker(cam, Game.MarketPosition + Vector3.up * 6f, "MARCHE", Palette.Gold, true);
            DrawMarker(cam, Game.HomeFiefPosition + Vector3.up * 6f, "TON FIEF", Palette.Banner(0), true);

            // La Tour de guet revele les gisements : c'est son effet Phase 1.
            if (Game.Fief != null && Game.Fief.hasWatchtower)
            {
                for (int i = 0; i < ResourceNode.All.Count; i++)
                {
                    ResourceNode node = ResourceNode.All[i];
                    if (node == null || node.IsDepleted) continue;
                    DrawDot(cam, node.transform.position + Vector3.up * 3f, ResourceInfo.Tint(node.type));
                }
            }
        }

        void DrawMarker(Camera cam, Vector3 world, string text, Color color, bool withDistance)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;

            float margin = UiStyle.S(60);
            float x = Mathf.Clamp(sp.x, margin, Screen.width - margin);
            float y = Mathf.Clamp(Screen.height - sp.y, margin, Screen.height - margin);

            string label = text;
            if (withDistance && Game.PlayerTransform != null)
            {
                float d = Vector3.Distance(Game.PlayerTransform.position, world);
                label += "  " + Mathf.RoundToInt(d) + " m";
            }

            Rect rect = new Rect(x - UiStyle.S(70), y - UiStyle.S(11), UiStyle.S(140), UiStyle.S(22));
            UiStyle.Fill(new Rect(x - UiStyle.S(5), y - UiStyle.S(5), UiStyle.S(10), UiStyle.S(10)), color);

            GUIStyle style = UiStyle.Small;
            Color previous = style.normal.textColor;
            TextAnchor anchor = style.alignment;
            style.normal.textColor = color;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(rect.x, rect.y - UiStyle.S(16), rect.width, rect.height), label, style);
            style.normal.textColor = previous;
            style.alignment = anchor;
        }

        void DrawDot(Camera cam, Vector3 world, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            if (sp.x < 0f || sp.x > Screen.width) return;

            float y = Screen.height - sp.y;
            if (y < 0f || y > Screen.height) return;

            float size = UiStyle.S(7);
            UiStyle.Fill(new Rect(sp.x - size * 0.5f, y - size * 0.5f, size, size),
                         new Color(color.r, color.g, color.b, 0.85f));
        }

        // ---------------------------------------------------------------- aide

        void DrawHelp()
        {
            if (!showHelp) return;

            float w = UiStyle.S(330);
            float h = UiStyle.S(192);
            Rect box = new Rect(Screen.width - w - UiStyle.S(14), UiStyle.S(14), w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(12);
            float y = box.y + UiStyle.S(8);

            GUI.Label(new Rect(x, y, box.width, UiStyle.S(22)), "COMMANDES", UiStyle.Head);
            y += UiStyle.S(26);

            string[] lines =
            {
                "ZQSD / WASD      se deplacer",
                "Maj (Shift)        courir - sac leger seulement",
                "Souris                 camera",
                "Molette              zoom",
                "Espace                sauter",
                "E                          recolter / interagir",
                "Echap                  fermer / liberer la souris",
                "F1                        afficher cette aide"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                GUI.Label(new Rect(x, y, box.width - UiStyle.S(24), UiStyle.S(18)), lines[i], UiStyle.Small);
                y += UiStyle.S(18);
            }
        }
    }
}
