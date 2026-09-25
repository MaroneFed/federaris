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
        const int MaxVisible = 3;

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

            // Sur le cote gauche, a mi-hauteur : hors du regard (le centre de l'ecran
            // est a la foret), mais la ou l'oeil tombe entre deux pas. Le plus recent
            // en bas, comme un fil de messages.
            float width = Mathf.Min(UiStyle.S(420), Screen.width * 0.4f);
            float height = UiStyle.S(24);
            float x = UiStyle.S(22);
            float y = Screen.height * 0.52f;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                float alpha = Mathf.Clamp01(e.life / 0.8f) * Mathf.Clamp01((Lifetime - e.life) / 0.15f + 0.2f);
                float slide = (1f - Mathf.Clamp01((Lifetime - e.life) / 0.2f)) * UiStyle.S(-16);

                Rect row = new Rect(x + slide, y + i * (height + UiStyle.S(3)), width, height);
                UiStyle.Fill(new Rect(row.x, row.y, 2f, row.height), new Color(e.color.r, e.color.g, e.color.b, alpha * 0.9f));
                UiStyle.Fill(new Rect(row.x + 2f, row.y, row.width * 0.75f, row.height), new Color(0.03f, 0.025f, 0.02f, 0.45f * alpha));

                GUIStyle style = UiStyle.Label;
                Color previous = style.normal.textColor;
                style.normal.textColor = new Color(e.color.r, e.color.g, e.color.b, alpha);
                GUI.Label(new Rect(row.x + UiStyle.S(10), row.y, row.width - UiStyle.S(12), row.height), e.text, style);
                style.normal.textColor = previous;
            }
        }
    }
}
