using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA BESACE (touche Tab), en deux onglets :
    ///   - VICTOIRES : les quatre facons de gagner, et ou tu en es sur chacune ;
    ///   - TALISMANS : tes talismans, un par ligne ; celui que tu survoles tourne
    ///     en 3D a droite. Ceux qui manquent ne montrent qu'une rumeur.
    /// </summary>
    public class TalismanPanel : IPanel
    {
        int selected = -1;

        public bool IsStillValid { get { return Game.Hoard != null; } }

        bool talismansTab;

        public void Draw()
        {
            Hoard hoard = Game.Hoard;
            float w = UiStyle.S(760);
            float h = UiStyle.S(470);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(28);
            float y = box.y + UiStyle.S(22);

            // Deux onglets : les chemins de la victoire, et les talismans.
            if (Tab(new Rect(x, y, UiStyle.S(320), UiStyle.S(38)), "VICTOIRES", !talismansTab)) talismansTab = false;
            if (Tab(new Rect(x + UiStyle.S(340), y, UiStyle.S(320), UiStyle.S(38)), "TALISMANS", talismansTab)) talismansTab = true;
            y += UiStyle.S(44);
            UiStyle.Rule(new Rect(x, y, w - UiStyle.S(56), UiStyle.S(8)));
            y += UiStyle.S(18);

            if (!talismansTab)
            {
                DrawVictories(box, x, y, w - UiStyle.S(56));
                CloseButton(box);
                return;
            }

            // --- la liste
            float listW = UiStyle.S(360);
            float rowH = UiStyle.S(50);
            int hovered = -1;
            for (int i = 0; i < TalismanInfo.Count; i++)
            {
                Talisman t = TalismanInfo.All[i];
                bool owned = hoard.Has(t);
                Rect row = new Rect(x, y + i * (rowH + UiStyle.S(4)), listW, rowH);
                bool over = row.Contains(Event.current.mousePosition);
                if (over) hovered = i;
                if (over || selected == i) GUI.Box(row, GUIContent.none, UiStyle.CardBox);

                float d = UiStyle.S(16);
                Rect icon = new Rect(row.x + UiStyle.S(12), row.center.y - d * 0.5f, d, d);
                if (owned) UiStyle.Chip(icon, TalismanInfo.Tint(t));
                else UiStyle.Icon(icon, UiStyle.Shape.Diamond, new Color(1f, 1f, 1f, 0.12f));

                UiStyle.Tinted(new Rect(row.x + UiStyle.S(40), row.y + UiStyle.S(4), listW, UiStyle.S(24)),
                               owned ? TalismanInfo.Name(t) : "???", UiStyle.Head, owned ? TalismanInfo.Tint(t) : UiStyle.InkFaint);
                UiStyle.Tinted(new Rect(row.x + UiStyle.S(40), row.y + UiStyle.S(26), listW - UiStyle.S(48), UiStyle.S(18)),
                               owned ? TalismanInfo.Effect(t) : "On dit : " + TalismanInfo.Where(t) + ".", UiStyle.Tiny,
                               owned ? UiStyle.InkDim : UiStyle.InkFaint);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) selected = i;
            }

            // --- l'objet en 3D
            int show = hovered >= 0 ? hovered : selected;
            Rect stage = new Rect(x + listW + UiStyle.S(24), y, box.xMax - x - listW - UiStyle.S(52), UiStyle.S(250));
            GUI.Box(stage, GUIContent.none, UiStyle.CardBox);
            if (show >= 0 && hoard.Has(TalismanInfo.All[show]))
            {
                Talisman t = TalismanInfo.All[show];
                Showcase.Show(t, 0.2f);
                if (Showcase.Image != null) GUI.DrawTexture(stage, Showcase.Image, ScaleMode.ScaleToFit, true);
                GUIStyle lore = UiStyle.Small;
                bool wrap = lore.wordWrap;
                lore.wordWrap = true;
                UiStyle.Tinted(new Rect(stage.x, stage.yMax + UiStyle.S(10), stage.width, UiStyle.S(60)),
                               "\"" + TalismanInfo.Lore(t) + "\"", lore, UiStyle.InkDim);
                lore.wordWrap = wrap;
            }
            else
            {
                UiStyle.Tinted(stage, show >= 0 ? "Pas encore trouve." : "Survole un talisman.", UiStyle.CenteredSmall, UiStyle.InkFaint);
            }

            CloseButton(box);
        }

        static void CloseButton(Rect box)
        {
            if (GUI.Button(new Rect(box.xMax - UiStyle.S(190), box.yMax - UiStyle.S(56), UiStyle.S(160), UiStyle.S(34)),
                           "Fermer  (Tab)", UiStyle.Button))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
        }

        static bool Tab(Rect r, string label, bool active)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Color c = active ? new Color(0.93f, 0.78f, 0.45f) : hover ? UiStyle.Ink : UiStyle.InkFaint;
            UiStyle.Tinted(r, UiStyle.Spaced(label), UiStyle.Title, c);
            if (active) UiStyle.FadeBand(new Rect(r.x, r.yMax - 2f, r.width * 0.8f, 2f), new Color(0.86f, 0.7f, 0.36f, 0.9f));
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Les quatre chemins, et ou tu en es sur chacun.</summary>
        static void DrawVictories(Rect box, float x, float y, float width)
        {
            Hoard h = Game.Hoard;
            Seeker me = Game.Me;
            float cardH = UiStyle.S(78);

            // La Relique : ta relique posee, face a celle du premier.
            Seeker leader = null;
            for (int i = 0; i < Game.Seekers.Count; i++)
                if (leader == null || Game.Seekers[i].Score > leader.Score) leader = Game.Seekers[i];
            int mine = me != null ? me.Score : 0;
            int best = leader != null ? leader.Score : 0;
            string reliqueState = best <= 0 ? "Personne n'a encore pose de relique."
                                : leader == me ? "Tu menes : " + mine + "."
                                : "Ta relique : " + mine + ".  En tete : " + leader.Name + ", " + best + ".";
            Card(x, ref y, width, cardH, VictoryKind.Relique, reliqueState, best > 0 ? (float)mine / best : 0f, Stele.RuneBlue);

            int sworn = Game.Garrison != null && me != null ? Game.Garrison.SwornCount(me) : 0;
            int guards = Game.Garrison != null ? Game.Garrison.Guards.Count : 6;
            Card(x, ref y, width, cardH, VictoryKind.Trahison,
                 "Gardes a toi : " + sworn + " / " + guards + ".   Ta bourse : " + (Game.Wallet != null ? Game.Wallet.Gold : 0) + " or.",
                 (float)sworn / guards, new Color(0.95f, 0.78f, 0.35f));

            int found = h != null ? h.TalismanCount : 0;
            Card(x, ref y, width, cardH, VictoryKind.Couronne,
                 "Talismans : " + found + " / " + TalismanInfo.Count + (found >= TalismanInfo.Count ? ".  Va au trone !" : "."),
                 (float)found / TalismanInfo.Count, new Color(0.9f, 0.5f, 0.4f));

            string offer = "";
            float progress = 0f;
            for (int i = 0; i < ResourceInfo.Count; i++)
            {
                int got = h != null ? h.Offered[i] : 0;
                offer += (i > 0 ? ",  " : "") + ResourceInfo.Name((ResourceType)i) + " " + got + "/" + Victories.Offering[i];
                progress += Mathf.Clamp01((float)got / Victories.Offering[i]) / ResourceInfo.Count;
            }
            Card(x, ref y, width, cardH, VictoryKind.Offrande, offer + ".", progress, new Color(0.62f, 0.86f, 0.48f));
        }

        static void Card(float x, ref float y, float width, float height, VictoryKind kind, string state, float progress, Color tint)
        {
            Rect r = new Rect(x, y, width, height - UiStyle.S(8));
            GUI.Box(r, GUIContent.none, UiStyle.CardBox);
            UiStyle.Tinted(new Rect(r.x + UiStyle.S(16), r.y + UiStyle.S(6), r.width, UiStyle.S(24)), Victories.Title(kind), UiStyle.Head, tint);
            UiStyle.Tinted(new Rect(r.x + UiStyle.S(16), r.y + UiStyle.S(28), r.width - UiStyle.S(32), UiStyle.S(18)),
                           Victories.How(kind), UiStyle.Tiny, UiStyle.InkDim);
            UiStyle.Tinted(new Rect(r.x + UiStyle.S(16), r.y + UiStyle.S(44), r.width * 0.62f, UiStyle.S(20)), state, UiStyle.Small, UiStyle.Ink);
            UiStyle.Bar(new Rect(r.x + r.width * 0.66f, r.y + UiStyle.S(50), r.width * 0.3f, UiStyle.S(8)), Mathf.Clamp01(progress), tint, UiStyle.BarBg);
            y += height;
        }

        static GUIStyle rightSmall;
        static GUIStyle RightSmall()
        {
            if (rightSmall == null || rightSmall.fontSize != UiStyle.Small.fontSize)
            {
                rightSmall = new GUIStyle(UiStyle.Small);
                rightSmall.alignment = TextAnchor.MiddleRight;
            }
            return rightSmall;
        }
    }
}
