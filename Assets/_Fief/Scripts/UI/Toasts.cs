using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE FIL DES EVENEMENTS, EN ICONES (30/09 -- "aucun texte a l'ecran") : a gauche, a
    /// mi-hauteur, une rangee de pastilles par evenement -- qui (sa couleur), ce qu'il a
    /// fait (une icone), a qui (sa couleur). "Rouge -> main -> Couronne -> Bleu" : Rouge a
    /// vole la Couronne a Bleu. Feed.cs dit quoi afficher.
    /// </summary>
    public static class Toasts
    {
        class Entry
        {
            public Color who;
            public bool hasWho;
            public string[] icons;
            public Color[] tints;
            public Color whom;
            public bool hasWhom;
            public float life;
        }

        const float Lifetime = 5f;
        const int MaxVisible = 5;

        static readonly List<Entry> entries = new List<Entry>();

        /// <summary>Un evenement : "who" (ou non) fait "icons" (de ces teintes) a "whom" (ou non).</summary>
        public static void Show(bool hasWho, Color who, string[] icons, Color[] tints, bool hasWhom, Color whom)
        {
            Entry e = new Entry();
            e.hasWho = hasWho;
            e.who = who;
            e.icons = icons;
            e.tints = tints;
            e.hasWhom = hasWhom;
            e.whom = whom;
            e.life = Lifetime;
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

        public static void Draw()
        {
            if (entries.Count == 0) return;
            float h = UiStyle.S(42);
            // (A droite de la jauge de la tour, qui est collee au bord gauche.)
            float x = UiStyle.S(84);
            float y = Screen.height * 0.36f;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                float alpha = Mathf.Clamp01(e.life / 0.6f);
                float slide = (1f - Mathf.Clamp01((Lifetime - e.life) / 0.2f)) * UiStyle.S(-40);
                float cx = x + slide;
                float cy = y + i * (h + UiStyle.S(8));
                int n = e.icons.Length + (e.hasWho ? 1 : 0) + (e.hasWhom ? 1 : 0);
                Icons.Pill(new Rect(cx, cy, n * h + UiStyle.S(12), h), new Color(0.12f, 0.13f, 0.26f, 0.85f), alpha);
                cx += UiStyle.S(6);
                if (e.hasWho) { Player(new Rect(cx, cy + h * 0.1f, h * 0.8f, h * 0.8f), e.who, alpha); cx += h; }
                for (int k = 0; k < e.icons.Length; k++)
                {
                    Color t = e.tints[k];
                    Icons.Draw(new Rect(cx + h * 0.06f, cy + h * 0.06f, h * 0.88f, h * 0.88f), e.icons[k], new Color(t.r, t.g, t.b, alpha));
                    cx += h;
                }
                if (e.hasWhom) Player(new Rect(cx, cy + h * 0.1f, h * 0.8f, h * 0.8f), e.whom, alpha);
            }
        }

        /// <summary>Un joueur : une pastille a sa couleur, une silhouette blanche.</summary>
        static void Player(Rect r, Color c, float alpha)
        {
            Icons.Pill(r, c, alpha);
            Icons.Draw(new Rect(r.x + r.width * 0.16f, r.y + r.height * 0.16f, r.width * 0.68f, r.height * 0.68f), "joueur", new Color(1f, 1f, 1f, alpha), false);
        }
    }
}
