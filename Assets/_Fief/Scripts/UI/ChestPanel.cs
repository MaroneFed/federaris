using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Le coffre : deposer allege le sac (et te rend ta vitesse),
    /// retirer recharge le sac dans la limite du poids.
    /// </summary>
    public class ChestPanel : IPanel
    {
        readonly Chest chest;

        public ChestPanel(Chest owner)
        {
            chest = owner;
        }

        public bool IsStillValid
        {
            get { return chest != null && Game.Fief != null && chest.PlayerIsClose(); }
        }

        public void Draw()
        {
            FiefState fief = Game.Fief;
            Inventory inv = Game.Inventory;
            if (fief == null || inv == null) return;

            float w = UiStyle.S(720);
            float h = UiStyle.S(330);
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyle.Frame(box);

            float pad = UiStyle.S(18);
            GUILayout.BeginArea(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f));

            GUILayout.Label("COFFRE DU FIEF", UiStyle.Title);
            GUILayout.Label("Ce qui dort ici ne pese rien. Le sac, lui, te ralentit.", UiStyle.Small);
            GUILayout.Space(UiStyle.S(10));

            float btnH = UiStyle.S(27);

            for (int i = 0; i < ResourceInfo.All.Length; i++)
            {
                ResourceType type = ResourceInfo.All[i];
                int inBag = inv.Get(type);
                int inChest = fief.Stock(type);

                GUILayout.BeginHorizontal();
                GUILayout.Label(ResourceInfo.Name(type), UiStyle.Head, GUILayout.Width(UiStyle.S(80)));
                GUILayout.Label("sac " + inBag, UiStyle.Label, GUILayout.Width(UiStyle.S(70)));
                GUILayout.Label("coffre " + inChest, UiStyle.Label, GUILayout.Width(UiStyle.S(100)));

                GUI.enabled = inBag > 0;
                if (GUILayout.Button("Deposer tout", UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(130))))
                {
                    int moved = inv.TryRemove(type, inBag);
                    fief.AddStock(type, moved);
                    Sfx.Pop();
                    Toasts.Show("Depose " + moved + " " + ResourceInfo.Name(type), ResourceInfo.Tint(type));
                }
                GUI.enabled = true;

                int room = inv.SpaceFor(type);
                int takeable = Mathf.Min(room, inChest);
                GUI.enabled = takeable > 0;
                if (GUILayout.Button("Retirer " + takeable, UiStyle.Button, GUILayout.Height(btnH), GUILayout.Width(UiStyle.S(130))))
                {
                    int taken = fief.TakeStock(type, takeable);
                    int added = inv.TryAdd(type, taken);
                    if (added < taken) fief.AddStock(type, taken - added);
                    Sfx.Pop();
                    Toasts.Show("Retire " + added + " " + ResourceInfo.Name(type), ResourceInfo.Tint(type));
                }
                GUI.enabled = true;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(UiStyle.S(6));
            }

            GUILayout.Space(UiStyle.S(6));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tout deposer", UiStyle.Button, GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(180))))
            {
                int total = 0;
                for (int i = 0; i < ResourceInfo.All.Length; i++)
                {
                    ResourceType type = ResourceInfo.All[i];
                    int moved = inv.TryRemove(type, inv.Get(type));
                    fief.AddStock(type, moved);
                    total += moved;
                }
                Sfx.Pop();
                Toasts.Show(total > 0 ? "Sac vide dans le coffre (" + total + " unites)" : "Sac deja vide", Palette.Gold);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Sac : " + Mathf.RoundToInt(inv.Weight) + " / " + Mathf.RoundToInt(inv.MaxWeight) + " kg",
                            UiStyle.Head);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Fermer  (Echap)", UiStyle.Button, GUILayout.Height(UiStyle.S(32)), GUILayout.Width(UiStyle.S(160))))
            {
                if (Game.Hud != null) Game.Hud.ClosePanel();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
