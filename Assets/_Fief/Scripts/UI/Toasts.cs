using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les petits messages qui montent en haut de l'ecran ("+3 Bois", "Vendu 12 Pierre...").
    /// Sans eux, on ne SENT pas la boucle de jeu : chaque action doit repondre.
    /// </summary>
    public static class Toasts
    {
        class Entry
        {
            public string text;
            public Color color;
            public float life;
        }

        const float Lifetime = 3.2f;
        const int MaxVisible = 6;

        static readonly List<Entry> entries = new List<Entry>();

        public static void Show(string text, Color color)
        {
            // Si le meme message vient de passer, on le rafraichit au lieu d'empiler.
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].text == text)
                {
                    entries[i].life = Lifetime;
                    return;
                }
            }

            Entry e = new Entry();
            e.text = text;
            e.color = color;
            e.life = Lifetime;
            entries.Add(e);

            while (entries.Count > MaxVisible) entries.RemoveAt(0);
        }

        public static void Show(string text)
        {
            Show(text, UiStyle.Ink);
        }

        public static void Clear()
        {
            entries.Clear();
        }

        public static void Tick(float deltaTime)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                entries[i].life -= deltaTime;
                if (entries[i].life <= 0f) entries.RemoveAt(i);
            }
        }

        public static void Draw()
        {
            if (entries.Count == 0) return;

            // Sous la carte de trouvaille, au tiers de l'ecran : ni sur la boussole,
            // ni sur l'invite du bas.
            float width = Mathf.Min(UiStyle.S(640), Screen.width - UiStyle.S(40));
            float height = UiStyle.S(28);
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.36f;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                float alpha = Mathf.Clamp01(e.life / 0.8f);

                Rect row = new Rect(x, y + i * (height + UiStyle.S(4)), width, height);
                UiStyle.FadeBand(row, new Color(0.03f, 0.025f, 0.02f, 0.7f * alpha));
                float d = UiStyle.S(7);
                UiStyle.Icon(new Rect(row.center.x - d * 0.5f, row.y - d * 0.5f, d, d), UiStyle.Shape.Diamond,
                             new Color(e.color.r, e.color.g, e.color.b, alpha * 0.8f));

                GUIStyle style = UiStyle.Centered;
                Color previous = style.normal.textColor;
                style.normal.textColor = new Color(e.color.r, e.color.g, e.color.b, alpha);
                GUI.Label(row, e.text, style);
                style.normal.textColor = previous;
            }
        }
    }
}
