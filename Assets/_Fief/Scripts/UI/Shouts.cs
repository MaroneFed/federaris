using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CRIS (05/10 -- Martin : "quand on se fait pousser, a la place du petit icone, je veux
    /// qu'on voie, imaginons que Gotaga joue : GOTAGA T'A POUSSE, avec un mot special"). Un
    /// bandeau en haut, en gros, quand quelqu'un te fait quelque chose -- ou quand tu le lui fais :
    ///
    ///     GOTAGA  t'a envoye valser !           (il t'a pousse)
    ///     Tu as atomise  GOTAGA !               (tu l'as pousse)
    ///     GOTAGA  t'a envoye dans les nuages !  (le KO)
    ///     GOTAGA  t'a pique la Couronne !       (le vol)
    ///
    /// Le pseudo a SA couleur, le reste en blanc. Un mot different a chaque fois (une liste par
    /// situation, tiree au hasard) : c'est le vocabulaire du jeu. Un bandeau a la fois (06/10).
    /// C'est une exception assumee au "zero texte" : c'est Martin qui l'a demandee.
    /// </summary>
    public static class Shouts
    {
        static readonly string[] PushedYou =
        {
            "t'a dégagé !", "t'a envoyé valser !", "t'a mis une patate !", "t'a fait décoller !", "t'a éjecté !",
            "t'a balayé !", "t'a atomisé !", "t'a expédié !", "t'a mis au tapis !", "t'a fumé !", "t'a renvoyé chez toi !",
            "t'a catapulté !"
        };
        static readonly string[] YouPushed =
        {
            "Tu as dégagé", "Tu as envoyé valser", "Tu as mis une patate à", "Tu as fait décoller", "Tu as éjecté",
            "Tu as balayé", "Tu as atomisé", "Tu as expédié", "Tu as mis au tapis", "Tu as fumé", "Tu as catapulté"
        };
        static readonly string[] KoYou = { "t'a envoyé dans les nuages !", "t'a sorti de l'île !", "t'a fait faire le grand plongeon !", "t'a envoyé au tapis... des nuages !" };
        static readonly string[] YouKo = { "Tu as envoyé dans les nuages", "Tu as sorti de l'île", "Tu as fait plonger" };

        sealed class ShoutLine
        {
            public string Before;
            public string Name;
            public string After;
            public Color NameColour;
            public float At;
        }

        static readonly List<ShoutLine> lines = new List<ShoutLine>();
        static readonly System.Random rng = new System.Random();
        const float Life = 2.6f;

        static string Pick(string[] list) { return list[rng.Next(list.Length)]; }

        // (06/10 -- Martin : "Gotaga pour Squeezie, ca joue bien, mais pas trop non plus, sinon
        // c'est horrible") : UN bandeau a la fois ; une simple poussee n'en fait un que si rien
        // n'a crie depuis 4 s. Le KO et la Couronne volee passent toujours.
        static float lastAt = -99f;

        static void Add(string before, Seeker who, string after) { Add(before, who, after, true); }

        static void Add(string before, Seeker who, string after, bool major)
        {
            if (who == null) return;
            if (!major && Time.unscaledTime - lastAt < 4f) return;
            lastAt = Time.unscaledTime;
            // Le meme joueur a l'instant (le vol ET la poussee du meme coup) : un seul bandeau.
            if (lines.Count > 0 && lines[0].Name == who.Name.ToUpperInvariant() && Time.unscaledTime - lines[0].At < 0.5f) return;
            ShoutLine l = new ShoutLine();
            l.Before = before;
            l.Name = who.Name.ToUpperInvariant();
            l.NameColour = Color.Lerp(who.Colour, Color.white, 0.15f);
            l.After = after;
            l.At = Time.unscaledTime;
            lines.Insert(0, l);
            while (lines.Count > 1) lines.RemoveAt(lines.Count - 1);
        }

        /// <summary>"by" t'a pousse (ou un coup d'une capacite).</summary>
        public static void PushedMe(Seeker by) { Add(null, by, Pick(PushedYou), false); }

        /// <summary>Tu as pousse "victim".</summary>
        public static void IPushed(Seeker victim) { Add(Pick(YouPushed), victim, "!", false); }

        /// <summary>Le KO : "by" t'a fait tomber dans les nuages (ou toi, lui).</summary>
        public static void Ko(Seeker by, Seeker victim)
        {
            if (victim != null && victim.IsPlayer) Add(null, by, Pick(KoYou));
            else if (by != null && by.IsPlayer) Add(Pick(YouKo), victim, "!");
        }

        /// <summary>La Couronne volee : a toi, ou par toi.</summary>
        public static void Stolen(Seeker thief, Seeker victim)
        {
            if (victim != null && victim.IsPlayer) Add(null, thief, "t'a piqué la Couronne !");
            else if (thief != null && thief.IsPlayer) Add("Tu as piqué la Couronne à", victim, "!");
        }

        /// <summary>(06/10) Un sort des capacites de fou sur toi : la prison crie toujours, le reste si c'est calme.</summary>
        public static void Cursed(Seeker by, Combat.Affliction what)
        {
            string after;
            switch (what)
            {
                case Combat.Affliction.Prison: after = "t'a mis en prison !"; break;
                case Combat.Affliction.Inverted: after = "t'a retourné le cerveau !"; break;
                case Combat.Affliction.Tiny: after = "t'a rétréci !"; break;
                case Combat.Affliction.Ink: after = "t'a aveuglé !"; break;
                case Combat.Affliction.Balloon: after = "t'a gonflé comme un ballon !"; break;
                case Combat.Affliction.Charmed: after = "t'a hypnotisé !"; break;
                default: return;
            }
            Add(null, by, after, what == Combat.Affliction.Prison);
        }

        public static void Clear() { lines.Clear(); }

        /// <summary>Les bandeaux, en haut au milieu (Hud).</summary>
        public static void Draw()
        {
            if (lines.Count == 0) return;
            float y = Mathf.Round(Screen.height * 0.17f);
            int size = Mathf.RoundToInt(UiStyle.S(34));
            float h = Mathf.Round(size * 1.7f);
            for (int i = 0; i < lines.Count; i++)
            {
                ShoutLine l = lines[i];
                float age = Time.unscaledTime - l.At;
                if (age > Life) { lines.RemoveAt(i); i--; continue; }
                float a = Mathf.Clamp01(age / 0.12f) * Mathf.Clamp01((Life - age) / 0.5f);
                int fs = i == 0 ? size : Mathf.RoundToInt(size * 0.75f);
                float gap = Mathf.Round(fs * 0.35f);
                float wb = string.IsNullOrEmpty(l.Before) ? 0f : Icons.Width(l.Before, fs) + gap;
                float wn = Icons.Width(l.Name, fs);
                float pad = Mathf.Round(fs * 0.5f);
                float wa = string.IsNullOrEmpty(l.After) ? 0f : Icons.Width(l.After, fs) + gap;
                float total = wb + wn + pad * 2f + wa;
                float x = Mathf.Round((Screen.width - total) * 0.5f);
                float lh = i == 0 ? h : Mathf.Round(h * 0.78f);
                // Un fond sombre, une pastille a la couleur du joueur pour son pseudo.
                Icons.Pill(new Rect(x - pad, y, total + pad * 2f, lh), new Color(0.05f, 0.05f, 0.1f, 0.55f * a));
                if (wb > 0f) { Icons.Text(new Rect(x, y, wb, lh), l.Before, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleLeft, true); x += wb; }
                Rect pill = new Rect(x, y + Mathf.Round(lh * 0.1f), wn + pad * 2f, Mathf.Round(lh * 0.8f));
                Icons.Pill(pill, new Color(l.NameColour.r * 0.75f, l.NameColour.g * 0.75f, l.NameColour.b * 0.75f, a));
                Icons.Text(pill, l.Name, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, true);
                x += pill.width + gap;
                if (wa > 0f) Icons.Text(new Rect(x, y, wa, lh), l.After, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleLeft, true);
                y += lh + Mathf.Round(UiStyle.S(8));
            }
        }
    }
}
