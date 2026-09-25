using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA FORGE DU MAGE : il fond tout ce que tu portes en une relique.
    ///
    /// Tout, pas une partie : on ne marchande pas avec un mage. C'est ce qui oblige
    /// a PREPARER sa venue -- aller rechercher sa cache, doser le sac, penser a la
    /// variete (les trois ressources valent x1,6) -- plutot qu'a vider ses poches au
    /// hasard.
    ///
    /// Le panneau montre AVANT de forger ce que la relique deviendra : la decision
    /// doit se prendre en connaissance de cause. Il ne touche lui-meme a rien : il
    /// appelle Relic.RequestForge, la seule porte entre le sac et la relique.
    /// </summary>
    public class ForgePanel : IPanel
    {
        readonly Mage mage;

        public ForgePanel(Mage mage)
        {
            this.mage = mage;
        }

        public bool IsStillValid
        {
            get { return mage != null && Game.PlayerTransform != null && mage.Near(Game.PlayerTransform.position); }
        }

        public void Draw()
        {
            Inventory bag = Game.Inventory;
            Hoard hoard = Game.Hoard;
            if (bag == null || hoard == null) return;

            Relic relic = hoard.Relic;
            bool onStele = hoard.RelicOnStele;

            float w = UiStyle.S(600);
            float h = UiStyle.S(372);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float pad = UiStyle.S(18);
            GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f));

            UiStyle.Tinted(GUILayoutUtility.GetRect(w - pad * 2f, UiStyle.S(38)), "LE MAGE", UiStyle.Title, Mage.Glow);
            GUILayout.Label(relic == null
                                ? "\"Apporte-moi ce que la forêt cache. J'en ferai une chose qui dure.\""
                                : "\"Encore ? Donne. Elle n'a pas fini de grandir.\"",
                            UiStyle.Small);
            GUILayout.Space(UiStyle.S(12));

            // --- ce qu'on porte, et ce que ca vaut pour lui
            int kinds = 0;
            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int carried = bag.Get(type);
                int already = relic != null ? relic.Get(type) : 0;
                if (carried + already > 0) kinds++;

                GUILayout.BeginHorizontal();
                Pictos.Named(GUILayoutUtility.GetRect(UiStyle.S(150), UiStyle.S(22)), type,
                             UiStyle.Label, carried > 0 ? ResourceInfo.Tint(type) : UiStyle.InkFaint);
                GUILayout.Label("x " + carried, UiStyle.Label, GUILayout.Width(UiStyle.S(70)));
                GUILayout.Label("vaut " + ResourceInfo.ForgeValue(type) + " chacun", UiStyle.Small, GUILayout.Width(UiStyle.S(130)));
                GUILayout.Label(already > 0 ? "déjà fondu : " + already : "", UiStyle.Tiny);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(UiStyle.S(8));
            Rect rule = GUILayoutUtility.GetRect(w - pad * 2f, 1f);
            UiStyle.Rule(rule);
            GUILayout.Space(UiStyle.S(8));

            // --- avant / apres
            int now = relic != null ? relic.Power : 0;
            int after = relic != null ? relic.PreviewPower(bag) : Relic.PowerOf(Carried(bag));

            GUILayout.BeginHorizontal();
            GUILayout.Label(relic == null ? "Pas encore de relique" : "Ta relique : puissance " + now,
                            UiStyle.Head, GUILayout.Width(UiStyle.S(260)));
            if (!bag.IsEmpty && !onStele)
                UiStyle.Tinted(GUILayoutUtility.GetRect(UiStyle.S(260), UiStyle.S(24)),
                               "après la forge : " + after + "   (+" + (after - now) + ")", UiStyle.Head, Palette.Gold);
            GUILayout.EndHorizontal();

            string variety = kinds >= 3 ? "Les trois ressources : puissance x1,6."
                           : kinds == 2 ? "Deux ressources : ×1,25   (trois : ×1,6)"
                           : "Une ressource : ×1   (deux : ×1,25, trois : ×1,6)";
            GUILayout.Label(variety, UiStyle.Small);

            GUILayout.FlexibleSpace();

            // --- la raison pour laquelle on ne peut pas forger, s'il y en a une
            string blocked = null;
            if (onStele) blocked = "Ta relique est sur ta stèle : apporte-la.";
            else if (bag.IsEmpty) blocked = "Ton sac est vide.";
            else if (relic == null && BagValue(bag) < FirstRelicMinimum)
                blocked = "\"Des miettes ?\"  " + BagValue(bag) + " / " + FirstRelicMinimum;
            if (blocked != null) UiStyle.Tinted(GUILayoutUtility.GetRect(w - pad * 2f, UiStyle.S(22)), blocked,
                                                UiStyle.Small, new Color(0.92f, 0.62f, 0.32f));

            GUILayout.BeginHorizontal();
            GUI.enabled = blocked == null;
            if (GUILayout.Button(relic == null ? "Forger ma relique" : "Tout fondre dans ma relique",
                                 UiStyle.ButtonPrimary, GUILayout.Height(UiStyle.S(34)), GUILayout.Width(UiStyle.S(260))))
                Forge();
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer  (Échap)", UiStyle.Button, GUILayout.Height(UiStyle.S(34)), GUILayout.Width(UiStyle.S(160))))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>Une relique ne nait pas de trois brindilles : il faut apporter un vrai sac.</summary>
        public const int FirstRelicMinimum = 20;

        static int BagValue(Inventory bag)
        {
            int v = 0;
            for (int i = 0; i < ResourceInfo.Count; i++) v += bag.Get((ResourceType)i) * ResourceInfo.ForgeValue((ResourceType)i);
            return v;
        }

        static int[] Carried(Inventory bag)
        {
            int[] carried = new int[ResourceInfo.Count];
            for (int i = 0; i < carried.Length; i++) carried[i] = bag.Get((ResourceType)i);
            return carried;
        }

        static void Forge()
        {
            Inventory bag = Game.Inventory;
            Hoard hoard = Game.Hoard;
            if (bag == null || hoard == null || hoard.RelicOnStele || bag.IsEmpty) return;

            bool first = hoard.Relic == null;
            Relic relic = hoard.EnsureRelic();
            int before = relic.Power;
            int melted = relic.RequestForge(bag);
            if (melted <= 0) { Sfx.Deny(); return; }

            // La relique se porte : elle pese dans le sac tant qu'elle n'est pas posee.
            if (Game.Me != null) Game.Me.SyncWeight();

            if (Game.Mage != null) Game.Mage.PlayForge();
            if (Game.Hud != null)
                Game.Hud.ShowDiscovery(first ? "LA RELIQUE EST NÉE" : "LA FORGE",
                                       "Puissance " + relic.Power,
                                       first ? "Pose-la sur ta stèle."
                                             : "+" + (relic.Power - before) + "   ·   " + Relic.TierName(Relic.Tier(relic.Power)),
                                       "", Mage.Glow);
            // Et il murmure un secret : un talisman, une stele rivale, un tresor.
            Toasts.Show("Le mage murmure : " + Secrets.Whisper(Game.Me), new Color(0.78f, 0.6f, 1f));

            // On ferme le panneau : le spectacle de la forge se passe DEVANT toi.
            if (Game.Hud != null) Game.Hud.ClosePanel();
        }
    }
}
