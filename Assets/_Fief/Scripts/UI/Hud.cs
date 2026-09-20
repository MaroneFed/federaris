using UnityEngine;

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
        float helpTimer = 22f;

        public bool PanelOpen { get { return panel != null; } }

        public void OpenPanel(IPanel newPanel) { panel = newPanel; }
        public void ClosePanel() { panel = null; }

        void Update()
        {
            Toasts.Tick(Time.unscaledDeltaTime);
            FloatingTexts.Tick(Time.unscaledDeltaTime);

            if (panel != null && !panel.IsStillValid) panel = null;

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

        // ---------------------------------------------------------------- aide

        void DrawHelp()
        {
            if (!showHelp) return;

            float w = UiStyle.S(300);
            float h = UiStyle.S(172);
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
