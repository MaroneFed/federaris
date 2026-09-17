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

            float width = UiStyle.S(420);
            float height = UiStyle.S(26);
            float x = (Screen.width - width) * 0.5f;
            float y = UiStyle.S(70);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                float alpha = Mathf.Clamp01(e.life / 0.8f);

                Rect row = new Rect(x, y + i * (height + UiStyle.S(4)), width, height);
                UiStyle.Fill(row, new Color(0f, 0f, 0f, 0.42f * alpha));
                UiStyle.Fill(new Rect(row.x, row.y, UiStyle.S(4), row.height),
                             new Color(e.color.r, e.color.g, e.color.b, alpha));

                GUIStyle style = UiStyle.Centered;
                Color previous = style.normal.textColor;
                style.normal.textColor = new Color(e.color.r, e.color.g, e.color.b, alpha);
                GUI.Label(row, e.text, style);
                style.normal.textColor = previous;
            }
        }
    }
}
