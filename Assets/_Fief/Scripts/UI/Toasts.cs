using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE FIL DES EVENEMENTS, EN ICONES (30/09 -- "aucun texte a l'ecran") : a gauche, a
    /// mi-hauteur, une rangee de pastilles par evenement -- qui, ce qu'il a fait (une
    /// icone), a qui. "Mahaut -> main -> Couronne -> Oswin" : Mahaut a vole la Couronne a
    /// Oswin. (02/10 : QUI est ecrit -- son pseudo dans une pastille a sa couleur ; une
    /// couleur seule ne disait rien a qui decouvre le jeu.) Feed.cs dit quoi afficher.
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
            public string whoName;
            public string whomName;
            public float life;
        }

        const float Lifetime = 5f;
        const int MaxVisible = 3;   // (06/10 : 5 -> 3, "pas trop d'infos")

        static readonly List<Entry> entries = new List<Entry>();

        /// <summary>Un evenement : "who" (ou non) fait "icons" (de ces teintes) a "whom" (ou non).</summary>
        public static void Show(bool hasWho, Color who, string[] icons, Color[] tints, bool hasWhom, Color whom)
        {
            Show(hasWho, who, null, icons, tints, hasWhom, whom, null);
        }

        /// <summary>La meme chose, avec les pseudos (null : une silhouette).</summary>
        public static void Show(bool hasWho, Color who, string whoName, string[] icons, Color[] tints, bool hasWhom, Color whom, string whomName)
        {
            Entry e = new Entry();
            e.whoName = whoName;
            e.whomName = whomName;
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
                float whoW = e.hasWho ? PlayerWidth(e.whoName, h) : 0f;
                float whomW = e.hasWhom ? PlayerWidth(e.whomName, h) : 0f;
                float total = whoW + e.icons.Length * h + whomW + UiStyle.S(12) + (e.hasWho ? UiStyle.S(4) : 0f) + (e.hasWhom ? UiStyle.S(4) : 0f);
                // (v43, le designer) Pastille plus legere (60 %), icones un peu plus grosses.
                Icons.Pill(new Rect(cx, cy, total, h), new Color(0.12f, 0.13f, 0.26f, 0.6f), alpha);
                cx += UiStyle.S(6);
                if (e.hasWho) { Player(new Rect(cx, cy + h * 0.1f, whoW, h * 0.8f), e.who, e.whoName, alpha); cx += whoW + UiStyle.S(4); }
                for (int k = 0; k < e.icons.Length; k++)
                {
                    Color t = e.tints[k];
                    Icons.Draw(new Rect(Mathf.Round(cx - h * 0.02f), Mathf.Round(cy - h * 0.02f), Mathf.Round(h * 1.04f), Mathf.Round(h * 1.04f)), e.icons[k], new Color(t.r, t.g, t.b, alpha));
                    cx += h;
                }
                if (e.hasWhom) Player(new Rect(cx + UiStyle.S(4), cy + h * 0.1f, whomW, h * 0.8f), e.whom, e.whomName, alpha);
            }
        }

        static int NameSize(float h) { return Mathf.RoundToInt(h * 0.42f / 2f) * 2; }

        /// <summary>La largeur de la pastille d'un joueur : son pseudo, ou un rond.</summary>
        static float PlayerWidth(string name, float h)
        {
            if (string.IsNullOrEmpty(name)) return h * 0.8f;
            return Mathf.Max(h * 0.8f, Icons.Width(name, NameSize(h)) + h * 0.5f);
        }

        /// <summary>Un joueur : une pastille a sa couleur, et son pseudo dedans (ou une silhouette).</summary>
        static void Player(Rect r, Color c, string name, float alpha)
        {
            Icons.Pill(r, c, alpha);
            if (string.IsNullOrEmpty(name))
            {
                float s = r.height;
                Icons.Draw(new Rect(r.x + s * 0.16f, r.y + s * 0.16f, s * 0.68f, s * 0.68f), "joueur", new Color(1f, 1f, 1f, alpha), false);
                return;
            }
            Icons.Text(r, name, NameSize(r.height / 0.8f), new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleCenter, true);
        }
    }
}
