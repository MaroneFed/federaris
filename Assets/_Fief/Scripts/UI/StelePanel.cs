using UnityEngine;

namespace Fief
{
    /// <summary>
    /// TA STELE, ouverte (E devant elle). C'est ton "marche" (Martin, 25/09) :
    ///
    ///   RESERVE        deposer et reprendre tes ressources. Ce qui dort ici
    ///                  echappe a la Malediction -- mais pas aux pillards ;
    ///   RELIQUE        la poser (elle ne compte a la cloche que posee), la
    ///                  reprendre pour le mage, y fondre une relique volee ;
    ///   AMELIORATIONS  ce que ta reserve peut t'acheter.
    ///
    /// Le panneau ne change rien lui-meme : chaque bouton passe par Hoard / Cache
    /// (Request* / Try*). Il se ferme si l'on s'eloigne de plus de 4 m.
    /// </summary>
    public class StelePanel : IPanel
    {
        const float Reach = 4.5f;
        static readonly string[] Tabs = { "RÉSERVE", "RELIQUE", "AMÉLIORATIONS" };

        readonly Stele stele;
        int tab;

        public StelePanel(Stele stele, int firstTab)
        {
            this.stele = stele;
            tab = Mathf.Clamp(firstTab, 0, Tabs.Length - 1);
        }

        public bool IsStillValid
        {
            get
            {
                if (stele == null || Game.PlayerTransform == null) return false;
                Vector3 d = Game.PlayerTransform.position - stele.transform.position;
                d.y = 0f;
                return d.magnitude <= Reach;
            }
        }

        public void Draw()
        {
            Seeker me = Game.Me;
            if (me == null || me.Hoard.Store == null) return;
            Inventory bag = me.Bag;
            Cache store = me.Hoard.Store;

            float w = UiStyle.S(700);
            float h = UiStyle.S(430);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float pad = UiStyle.S(18);
            GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f));
            GUILayout.Label("TA STÈLE", UiStyle.Title);

            // --- les onglets
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Tabs.Length; i++)
            {
                GUIStyle style = i == tab ? UiStyle.ButtonPrimary : UiStyle.Button;
                if (GUILayout.Button(Tabs[i], style, GUILayout.Height(UiStyle.S(30)), GUILayout.Width(UiStyle.S(170)))) tab = i;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(UiStyle.S(10));

            if (tab == 0) DrawStore(store, bag, box.width - pad * 2f);
            else if (tab == 1) DrawRelic(me);
            else UpgradeList.Draw(me);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (tab == 0)
            {
                StashPanel.DepositAll(store, bag);
                GUI.enabled = !store.Contents.IsEmpty;
                if (GUILayout.Button("Tout reprendre", UiStyle.Button, GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(170))))
                {
                    int n = me.Hoard.RequestTakeAll(bag);
                    if (n > 0) Sfx.Pop(); else Sfx.Deny();
                    me.SyncWeight();
                }
                GUI.enabled = true;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer  (Échap)", UiStyle.Button, GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(160))))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        static void DrawStore(Cache store, Inventory bag, float width)
        {
            Season season = Game.Season;
            float curse = season != null ? season.NextCurseIn : -1f;
            string when = curse >= 0f ? "La prochaine Malédiction tombe dans " + Hud.Clock(curse) + "." : "Plus de Malédiction avant la cloche.";
            UiStyle.Tinted(GUILayoutUtility.GetRect(width, UiStyle.S(20)),
                           "Ce qui dort ici échappe à la Malédiction. Pas aux pillards.  " + when, UiStyle.Small, Curse.Violet);
            GUILayout.Space(UiStyle.S(8));
            StashPanel.Rows(store, bag, "STÈLE");
            GUILayout.Space(UiStyle.S(6));
            Rect gauges = GUILayoutUtility.GetRect(width, UiStyle.S(38));
            StashPanel.Gauge(new Rect(gauges.x, gauges.y, gauges.width * 0.47f, gauges.height), "Sac", bag);
            StashPanel.Gauge(new Rect(gauges.x + gauges.width * 0.53f, gauges.y, gauges.width * 0.47f, gauges.height), "Réserve", store.Contents);
        }

        static void DrawRelic(Seeker me)
        {
            Hoard h = me.Hoard;
            string state;
            if (h.Trophy != null) state = "Tu portes la relique de " + h.TrophyFrom.Name + " (puissance " + h.Trophy.Power + "). Fonds-la dans la tienne : 60 % passent.";
            else if (h.Relic == null) state = "Tu n'as pas encore de relique. Le mage la forge avec ce que tu lui portes.";
            else if (h.RelicOnStele) state = "Ta relique repose ici : puissance " + h.FinalScore + ". Elle comptera à la cloche -- si personne ne la vole.";
            else state = "Ta relique est dans tes mains (puissance " + h.Relic.Power + "). Tant qu'elle n'est pas posée, elle ne compte pas.";
            GUILayout.Label(state, UiStyle.Label);
            GUILayout.Space(UiStyle.S(12));

            GUILayout.BeginHorizontal();
            if (h.Trophy != null)
            {
                if (GUILayout.Button("Fondre la relique volée", UiStyle.ButtonPrimary, GUILayout.Height(UiStyle.S(34)), GUILayout.Width(UiStyle.S(260))))
                    Stele.AbsorbTrophy(me);
            }
            if (h.RelicInHand)
            {
                if (GUILayout.Button("Poser ta relique", UiStyle.ButtonPrimary, GUILayout.Height(UiStyle.S(34)), GUILayout.Width(UiStyle.S(220))))
                    Stele.PlaceRelic(me);
            }
            else if (h.RelicOnStele)
            {
                if (GUILayout.Button("Reprendre ta relique (pour le mage)", UiStyle.Button, GUILayout.Height(UiStyle.S(34)), GUILayout.Width(UiStyle.S(320))))
                    Stele.TakeRelic(me);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
