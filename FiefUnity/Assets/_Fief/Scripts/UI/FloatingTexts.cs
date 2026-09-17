using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les "+2 Bois" et "+34 or" qui s'envolent depuis l'endroit exact de l'action.
    ///
    /// Les notifications en haut de l'ecran disent CE QUI s'est passe ; ces textes-ci
    /// disent OU. C'est ce lien entre le geste et le retour qui rend une action
    /// satisfaisante plutot que mecanique.
    /// </summary>
    public static class FloatingTexts
    {
        class Entry
        {
            public string text;
            public Vector3 world;
            public Color color;
            public float life;
            public float drift;
        }

        const float Lifetime = 1.5f;
        const int MaxVisible = 24;

        static readonly List<Entry> entries = new List<Entry>();

        public static void Spawn(Vector3 world, string text, Color color)
        {
            Entry e = new Entry();
            e.text = text;
            e.world = world;
            e.color = color;
            e.life = Lifetime;
            // Petit decalage lateral : deux textes simultanes ne se superposent pas.
            e.drift = (entries.Count % 2 == 0) ? 0.35f : -0.35f;
            entries.Add(e);

            while (entries.Count > MaxVisible) entries.RemoveAt(0);
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

        public static void Draw(Camera camera)
        {
            if (camera == null || entries.Count == 0) return;

            GUIStyle style = UiStyle.Centered;
            Color previous = style.normal.textColor;
            FontStyle previousStyle = style.fontStyle;
            style.fontStyle = FontStyle.Bold;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                float t = 1f - Mathf.Clamp01(e.life / Lifetime);

                // Monte et s'estompe.
                Vector3 world = e.world + new Vector3(e.drift, 0.4f + t * 1.7f, 0f);
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue;

                float alpha = Mathf.Clamp01(e.life / 0.5f);
                style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.6f);

                Rect rect = new Rect(screen.x - UiStyle.S(80),
                                     Screen.height - screen.y - UiStyle.S(12),
                                     UiStyle.S(160), UiStyle.S(24));

                GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), e.text, style);
                style.normal.textColor = new Color(e.color.r, e.color.g, e.color.b, alpha);
                GUI.Label(rect, e.text, style);
            }

            style.normal.textColor = previous;
            style.fontStyle = previousStyle;
        }
    }
}
