using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le panneau de negoce. Il ne calcule RIEN : il affiche ce que le Market expose
    /// et lui envoie des demandes (RequestSell / RequestBuy). C'est exactement la
    /// separation qu'il faudra pour le multijoueur.
    /// </summary>
    public class MarketPanel : IPanel
    {
        readonly MarketZone zone;

        public MarketPanel(MarketZone marketZone)
        {
            zone = marketZone;
        }

        public bool IsStillValid
        {
            get { return zone != null && Game.Market != null && zone.PlayerIsClose(); }
        }

        public void Draw()
        {
            Market market = Game.Market;
            Inventory inv = Game.Inventory;
            Wallet wallet = Game.Wallet;
            if (market == null || inv == null || wallet == null) return;

            float w = UiStyle.S(880);
            float h = UiStyle.S(430);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float pad = UiStyle.S(18);
            GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f));

            GUILayout.Label("MARCHE CENTRAL", UiStyle.Title);
            GUILayout.Label("Plus tu inondes le marche, plus le prix s'effondre. Les stocks reviennent"
                          + " lentement a l'equilibre : reviens plus tard, le prix aura remonte.",
                            UiStyle.Small);
            GUILayout.Space(UiStyle.S(10));

            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                DrawResourceRow(market, inv, wallet, ResourceInfo.All[i]);
                GUILayout.Space(UiStyle.S(8));
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bourse : " + wallet.Gold + " or        Sac : "
                          + Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg",
                            UiStyle.Head);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer  (Echap)", UiStyle.Button, GUILayout.Height(UiStyle.S(30)), GUILayout.Width(UiStyle.S(160))))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void DrawResourceRow(Market market, Inventory inv, Wallet wallet, ResourceType type)
        {
            int owned = inv.Get(type);
            float sellUnit = market.SellPrice(type);
            float buyUnit = market.BuyPrice(type);
            float trend = market.PriceTrend(type);

            float btnH = UiStyle.S(27);

            GUILayout.BeginHorizontal();

            GUILayout.Label(ResourceInfo.Name(type), UiStyle.Head, GUILayout.Width(UiStyle.S(78)));
            GUILayout.Label("sac " + owned, UiStyle.Label, GUILayout.Width(UiStyle.S(66)));
            GUILayout.Label("stock " + Mathf.RoundToInt(market.Stock(type)), UiStyle.Small, GUILayout.Width(UiStyle.S(82)));
            GUILayout.Label(sellUnit.ToString("0.0") + " or", UiStyle.Value, GUILayout.Width(UiStyle.S(78)));
            GUILayout.Label(TrendText(trend), UiStyle.Small, GUILayout.Width(UiStyle.S(86)));

            GUI.enabled = owned > 0;
            if (GUILayout.Button("Vendre 1", UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(92))))
                Apply(market.RequestSell(inv, wallet, type, 1));

            if (GUILayout.Button("Vendre 10", UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(100))))
                Apply(market.RequestSell(inv, wallet, type, 10));

            string allLabel = owned > 0
                ? "Tout : " + market.QuoteSell(type, owned) + " or"
                : "Tout";
            if (GUILayout.Button(allLabel, UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(150))))
                Apply(market.RequestSell(inv, wallet, type, owned));
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(UiStyle.S(78));
            GUILayout.Label("rachat au marchand : " + buyUnit.ToString("0.0") + " or l'unite",
                            UiStyle.Small, GUILayout.Width(UiStyle.S(312)));

            GUI.enabled = wallet.Gold >= Mathf.RoundToInt(buyUnit) && inv.SpaceFor(type) > 0;
            if (GUILayout.Button("Acheter 1", UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(92))))
                Apply(market.RequestBuy(inv, wallet, type, 1));
            if (GUILayout.Button("Acheter 10", UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(100))))
                Apply(market.RequestBuy(inv, wallet, type, 10));
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static string TrendText(float trend)
        {
            if (trend > 0.25f) return "prix HAUT";
            if (trend < -0.25f) return "prix BAS";
            return "prix normal";
        }

        static void Apply(TradeResult result)
        {
            if (!result.success)
            {
                Sfx.Deny();
                Toasts.Show(result.message, Palette.Iron);
                return;
            }

            Sfx.Coin();
            Toasts.Show(result.message, Palette.Gold);

            if (Game.PlayerTransform != null)
            {
                string label = (result.gold >= 0 ? "+" : "") + result.gold + " or";
                FloatingTexts.Spawn(Game.PlayerTransform.position + Vector3.up * 2.2f,
                                    label, result.gold >= 0 ? Palette.Gold : Palette.Iron);
            }
        }
    }
}
