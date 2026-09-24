using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le panneau d'une cache ou de la tente : deux colonnes, le sac et le trou.
    ///
    /// Il ne deplace RIEN lui-meme. Chaque bouton demande a Cache.RequestDeposit ou
    /// RequestWithdraw, qui sont les deux seules portes entre un inventaire et
    /// l'autre (regle du depot : on ne modifie un stock que par Request* / Try*).
    ///
    /// Il se ferme tout seul si l'on s'eloigne de plus de 4 m : impossible de vider
    /// sa cache a distance en gardant le panneau ouvert.
    /// </summary>
    public class StashPanel : IPanel
    {
        const float Reach = 4f;

        readonly Cache cache;
        readonly Transform anchor;
        readonly string title;

        public StashPanel(Cache cache, Transform anchor, string title)
        {
            this.cache = cache;
            this.anchor = anchor;
            this.title = title;
        }

        public bool IsStillValid
        {
            get
            {
                if (cache == null || anchor == null || Game.PlayerTransform == null) return false;
                Vector3 d = Game.PlayerTransform.position - anchor.position;
                d.y = 0f;
                return d.magnitude <= Reach;
            }
        }

        public void Draw()
        {
            Inventory bag = Game.Inventory;
            if (bag == null || cache == null) return;
            Inventory hole = cache.Contents;

            float w = UiStyle.S(640);
            float h = UiStyle.S(318);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float pad = UiStyle.S(18);
            GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f));

            GUILayout.Label(title, UiStyle.Title);
            GUILayout.Label(cache.Number == 0
                                ? "Ta tente se voit de loin. Ce qui dort ici ne pèse plus sur ton dos."
                                : "Personne ne sait qu'elle est là. Ce qui dort ici ne pèse plus sur ton dos.",
                            UiStyle.Small);
            GUILayout.Space(UiStyle.S(10));

            Rows(cache, bag, cache.Number == 0 ? "TENTE" : "CACHE");
            GUILayout.Space(UiStyle.S(8));

            // --- les deux jauges : ce qu'on porte, ce que le trou peut encore avaler
            Rect gauges = GUILayoutUtility.GetRect(box.width - pad * 2f, UiStyle.S(38));
            Gauge(new Rect(gauges.x, gauges.y, gauges.width * 0.47f, gauges.height), "Sac", bag);
            Gauge(new Rect(gauges.x + gauges.width * 0.53f, gauges.y, gauges.width * 0.47f, gauges.height),
                  cache.Number == 0 ? "Tente" : "Cache", hole);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            DepositAll(cache, bag);
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer  (Échap)", UiStyle.Button, GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(160))))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Les lignes "ressource / sac / reserve / deposer / reprendre". Partagees par
        /// les caches, la tente et la stele : un seul endroit ou l'on deplace des
        /// unites, et toujours par RequestDeposit / RequestWithdraw.
        /// </summary>
        public static void Rows(Cache cache, Inventory bag, string holeHeader)
        {
            Inventory hole = cache.Contents;
            // --- en-tetes de colonnes
            GUILayout.BeginHorizontal();
            GUILayout.Label("", UiStyle.Tiny, GUILayout.Width(UiStyle.S(150)));
            GUILayout.Label("SAC", UiStyle.Tiny, GUILayout.Width(UiStyle.S(60)));
            GUILayout.Label(holeHeader, UiStyle.Tiny, GUILayout.Width(UiStyle.S(70)));
            GUILayout.EndHorizontal();

            float btnH = UiStyle.S(27);
            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int inBag = bag.Get(type);
                int inHole = hole.Get(type);

                GUILayout.BeginHorizontal();
                UiStyle.Tinted(GUILayoutUtility.GetRect(UiStyle.S(150), btnH), ResourceInfo.Name(type),
                               UiStyle.Head, ResourceInfo.Tint(type));
                GUILayout.Label(inBag.ToString(), UiStyle.Label, GUILayout.Width(UiStyle.S(60)), GUILayout.Height(btnH));
                GUILayout.Label(inHole.ToString(), UiStyle.Label, GUILayout.Width(UiStyle.S(70)), GUILayout.Height(btnH));

                int canPut = Mathf.Min(inBag, hole.SpaceFor(type));
                GUI.enabled = canPut > 0;
                if (GUILayout.Button("Déposer " + canPut, UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(120))))
                    Report(cache.RequestDeposit(bag, type, canPut), "Déposé", type);

                int canTake = Mathf.Min(inHole, bag.SpaceFor(type));
                GUI.enabled = canTake > 0;
                if (GUILayout.Button("Reprendre " + canTake, UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(130))))
                    Report(cache.RequestWithdraw(bag, type, canTake), "Repris", type);
                GUI.enabled = true;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(UiStyle.S(4));
            }

        }

        /// <summary>Le bouton "Tout deposer".</summary>
        public static void DepositAll(Cache cache, Inventory bag)
        {
            GUI.enabled = !bag.IsEmpty;
            if (GUILayout.Button("Tout déposer", UiStyle.ButtonPrimary, GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(180))))
            {
                int total = 0;
                for (int i = 0; i < ResourceInfo.All.Length; i++)
                {
                    ResourceType type = ResourceInfo.All[i];
                    total += cache.RequestDeposit(bag, type, bag.Get(type));
                }
                if (total > 0)
                {
                    Sfx.Stash();
                    Toasts.Show("Déposé " + total + " unités. Le sac respire.", Palette.Gold);
                }
                else
                {
                    Sfx.Deny();
                    Toasts.Show("Plus de place ici.", UiStyle.InkDim);
                }
            }
            GUI.enabled = true;
        }

        static void Report(int moved, string verb, ResourceType type)
        {
            if (moved > 0)
            {
                Sfx.Pop();
                Toasts.Show(verb + " " + moved + " " + ResourceInfo.Name(type), ResourceInfo.Tint(type));
            }
            else
            {
                Sfx.Deny();
            }
        }

        public static void Gauge(Rect r, string label, Inventory inv)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, UiStyle.S(20)), label, UiStyle.Small);
            GUIStyle right = UiStyle.Small;
            TextAnchor previous = right.alignment;
            right.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(r.x, r.y, r.width, UiStyle.S(20)),
                      Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg", right);
            right.alignment = previous;
            UiStyle.Bar(new Rect(r.x, r.y + UiStyle.S(24), r.width, UiStyle.S(9)), inv.Load01,
                        Palette.Gold, UiStyle.BarBg);
        }
    }
}
