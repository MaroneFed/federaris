using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA BESACE (touche Tab) : tes talismans, un par ligne, et celui que tu
    /// survoles tourne en 3D a droite. Ceux qui te manquent ne montrent qu'une
    /// rumeur : l'endroit ou l'on dit qu'il dort.
    /// </summary>
    public class TalismanPanel : IPanel
    {
        int selected = -1;

        public bool IsStillValid { get { return Game.Hoard != null; } }

        public void Draw()
        {
            Hoard hoard = Game.Hoard;
            float w = UiStyle.S(760);
            float h = UiStyle.S(460);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float x = box.x + UiStyle.S(28);
            float y = box.y + UiStyle.S(22);
            GUI.Label(new Rect(x, y, w, UiStyle.S(38)), UiStyle.Spaced("BESACE"), UiStyle.Title);
            UiStyle.Tinted(new Rect(x, y, w - UiStyle.S(56), UiStyle.S(38)), hoard.TalismanCount + " / " + TalismanInfo.Count + " talismans",
                           RightSmall(), UiStyle.InkDim);
            y += UiStyle.S(44);
            UiStyle.Rule(new Rect(x, y, w - UiStyle.S(56), UiStyle.S(8)));
            y += UiStyle.S(18);

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

            if (GUI.Button(new Rect(box.xMax - UiStyle.S(190), box.yMax - UiStyle.S(56), UiStyle.S(160), UiStyle.S(34)),
                           "Fermer  (Tab)", UiStyle.Button))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
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
