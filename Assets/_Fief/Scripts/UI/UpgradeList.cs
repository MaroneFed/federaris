using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'onglet AMELIORATIONS de la stele : une ligne par amelioration, avec ce
    /// qu'elle fait, son niveau, son prix (paye avec la reserve de la stele), et un
    /// bouton. Il ne decide rien : il demande a Hoard.TryBuyUpgrade.
    /// </summary>
    public static class UpgradeList
    {
        static Vector2 scroll;

        public static void Draw(Seeker me)
        {
            Hoard h = me.Hoard;
            GUILayout.Label("Payé avec ta réserve, et un peu d'or.", UiStyle.Small);
            GUILayout.Space(UiStyle.S(6));
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(UiStyle.S(250)));
            for (int i = 0; i < UpgradeInfo.Count; i++)
            {
                UpgradeKind k = (UpgradeKind)i;
                int level = h.Level(k);
                int max = UpgradeInfo.MaxLevel(k);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(UiStyle.S(430)));
                string title = UpgradeInfo.Name(k) + (max > 1 ? "   " + level + " / " + max : level > 0 ? "   (acquise)" : "");
                UiStyle.Tinted(GUILayoutUtility.GetRect(UiStyle.S(430), UiStyle.S(22)), title, UiStyle.Head,
                               level > 0 ? Palette.Gold : UiStyle.Ink);
                GUILayout.Label(UpgradeInfo.Effect(k), UiStyle.Small);
                if (level < max) GUILayout.Label("Prix : " + Price(k, level), UiStyle.Tiny);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                GUI.enabled = level < max && h.CanBuy(k, me.Money);
                string label = level >= max ? "Acquise" : "Acheter";
                if (GUILayout.Button(label, level < max ? UiStyle.ButtonPrimary : UiStyle.Button,
                                     GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(120))))
                {
                    if (h.TryBuyUpgrade(k, me.Bag, me.Money))
                    {
                        ApplyToWorld(k);
                        Sfx.Build();
                        Toasts.Show(UpgradeInfo.Name(k) + " : " + UpgradeInfo.Effect(k), Palette.Gold);
                    }
                    else Sfx.Deny();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(UiStyle.S(8));
            }
            GUILayout.EndScrollView();
        }

        static string Price(UpgradeKind k, int level)
        {
            int[] cost = UpgradeInfo.Cost(k, level);
            string s = "";
            for (int i = 0; i < cost.Length; i++)
            {
                if (cost[i] <= 0) continue;
                if (s.Length > 0) s += ", ";
                s += cost[i] + " " + ResourceInfo.Name((ResourceType)i);
            }
            int gold = UpgradeInfo.GoldCost(k, level);
            if (gold > 0) s += (s.Length > 0 ? ", " : "") + gold + " or";
            return s;
        }

        /// <summary>Les effets qui vivent dans le monde (la lanterne est une Light).</summary>
        static void ApplyToWorld(UpgradeKind k)
        {
            if (k == UpgradeKind.Lanterne && Atmosphere.Lamp != null)
                Atmosphere.Lamp.range *= UpgradeInfo.LanternFactor;
        }
    }
}
