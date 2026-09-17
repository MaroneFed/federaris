using UnityEngine;

namespace Fief
{
    /// <summary>Le catalogue de construction, ouvert depuis un emplacement libre.</summary>
    public class BuildPanel : IPanel
    {
        readonly BuildPlot plot;

        public BuildPanel(BuildPlot buildPlot)
        {
            plot = buildPlot;
        }

        public bool IsStillValid
        {
            get { return plot != null && !plot.IsOccupied && plot.PlayerIsClose(); }
        }

        public void Draw()
        {
            FiefState fief = Game.Fief;
            Wallet wallet = Game.Wallet;
            if (fief == null || wallet == null) return;

            float w = UiStyle.S(840);
            float h = UiStyle.S(470);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float pad = UiStyle.S(18);
            GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f));

            GUILayout.Label("CONSTRUIRE  -  emplacement " + (plot.index + 1), UiStyle.Title);
            string discount = fief.buildCostMultiplier < 0.999f
                ? "   (Atelier : -" + Mathf.RoundToInt((1f - fief.buildCostMultiplier) * 100f) + "% applique)"
                : "";
            GUILayout.Label("Bourse : " + wallet.Gold + " or" + discount, UiStyle.Head);
            GUILayout.Space(UiStyle.S(8));

            for (int i = 0; i < BuildingCatalog.All.Length; i++)
            {
                DrawRow(BuildingCatalog.All[i], fief, wallet);
                GUILayout.Space(UiStyle.S(6));
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer  (Echap)", UiStyle.Button, GUILayout.Height(UiStyle.S(30)), GUILayout.Width(UiStyle.S(160))))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void DrawRow(BuildingDef def, FiefState fief, Wallet wallet)
        {
            int cost = fief.CostOf(def);
            bool unique = def.id == BuildingId.Chest
                       || def.id == BuildingId.Workshop
                       || def.id == BuildingId.Watchtower;
            bool alreadyBuilt = unique && fief.Has(def.id);
            bool affordable = wallet.CanAfford(cost);

            GUILayout.BeginHorizontal();

            UiStyle.Fill(GUILayoutUtility.GetRect(UiStyle.S(10), UiStyle.S(44), GUILayout.Width(UiStyle.S(10))), def.color);
            GUILayout.Space(UiStyle.S(8));

            GUILayout.BeginVertical(GUILayout.Width(UiStyle.S(520)));
            GUILayout.Label(def.name + "  -  " + cost + " or   (+" + def.prestige + " prestige)", UiStyle.Head);
            GUILayout.Label(def.effect, UiStyle.Label);
            GUILayout.Label(def.phaseNote, UiStyle.Small);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUI.enabled = affordable && !alreadyBuilt;
            string label = alreadyBuilt ? "Deja construit" : (affordable ? "Construire" : "Or insuffisant");
            if (GUILayout.Button(label, UiStyle.Button, GUILayout.Height(UiStyle.S(38)), GUILayout.Width(UiStyle.S(170))))
            {
                if (plot.TryBuild(def) && Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
    }
}
