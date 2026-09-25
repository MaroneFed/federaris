using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA BOUSSOLE, en haut de l'ecran. Dans une foret sans horizon, c'est ton seul
    /// sens de l'orientation -- elle devait donc etre belle ET tout dire.
    ///
    /// Une bande de cuir qui s'efface aux deux bouts, des graduations tous les 15
    /// degres, les points cardinaux en capitales (le Nord en cramoisi), une aiguille
    /// de bronze au centre. Et dessus, des SIGNES, chacun a sa forme :
    ///
    ///   losange bleu      ta stele                (la ou tout se joue)
    ///   triangle vert     ton camp
    ///   point brun        tes caches
    ///   carre dore        le chateau
    ///   point gris        les lieux-dits decouverts
    ///   losange colore    les steles rivales que tu as trouvees
    ///   losange rouge     un rival qui emporte TA relique (il clignote)
    ///   point bleu        le mage -- seulement avec la Corne d'appel
    ///
    /// Regarde un signe (qu'il soit au centre) : sa distance s'affiche dessous.
    ///
    /// (Le 25/09, la boussole avait ete videe "pour forcer a retenir". Martin, le
    /// lendemain : "si on ne se souvient pas ou est la stele, ni le chateau, c'est
    /// bof". Les signes sont revenus -- avec une carte plus petite.)
    /// Un signe hors du champ se colle au bord, en plus petit.
    /// </summary>
    public static class Compass
    {
        struct Mark
        {
            public Vector3 at;
            public UiStyle.Shape shape;
            public Color color;
            public float size;
            public string label;
            public bool pulse;
            public bool key;        // chateau, ta stele : jamais estompes
        }

        static readonly List<Mark> Marks = new List<Mark>();

        /// <summary>Vrai quand la Malediction tombe dans moins de 20 s et que ton sac n'est pas vide : ta stele palpite.</summary>
        public static bool UrgeStele;
        const float HalfSpan = 95f;          // degres visibles de chaque cote

        public static void Draw(Rect band, Transform eye, Vector3 me)
        {
            float heading = eye.eulerAngles.y;

            // --- le fond : cuir sombre, deux filets de bronze
            UiStyle.FadeBand(band, new Color(0.03f, 0.025f, 0.02f, 0.72f));
            UiStyle.FadeBand(new Rect(band.x, band.y, band.width, 1f), new Color(0.62f, 0.5f, 0.3f, 0.9f));
            UiStyle.FadeBand(new Rect(band.x, band.yMax - 1f, band.width, 1f), new Color(0.62f, 0.5f, 0.3f, 0.9f));

            // --- les graduations et les lettres
            string[] names = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            for (int deg = 0; deg < 360; deg += 15)
            {
                float delta = Mathf.DeltaAngle(heading, deg);
                if (Mathf.Abs(delta) > HalfSpan) continue;
                float px = X(band, delta);
                float alpha = Fade(delta);

                if (deg % 45 == 0)
                {
                    int i = deg / 45;
                    bool cardinal = i % 2 == 0;
                    GUIStyle style = cardinal ? UiStyle.Head : UiStyle.CenteredSmall;
                    TextAnchor previous = style.alignment;
                    style.alignment = TextAnchor.MiddleCenter;
                    Color c = i == 0 ? new Color(0.92f, 0.36f, 0.26f) : cardinal ? UiStyle.Ink : UiStyle.InkDim;
                    UiStyle.Tinted(new Rect(px - UiStyle.S(24), band.y, UiStyle.S(48), band.height), names[i], style,
                                   new Color(c.r, c.g, c.b, alpha));
                    style.alignment = previous;
                }
                else
                {
                    UiStyle.Fill(new Rect(px, band.y + band.height * 0.62f, 1f, band.height * 0.26f),
                                 new Color(0.8f, 0.72f, 0.56f, alpha * 0.55f));
                }
            }

            // --- les signes
            Collect(me);
            for (int i = 0; i < Marks.Count; i++) DrawMark(band, Marks[i], heading, me);

            // --- l'aiguille : un triangle de bronze sous la bande, un losange dessus
            float n = UiStyle.S(12);
            UiStyle.Icon(new Rect(band.center.x - n * 0.5f, band.yMax + 1f, n, n), UiStyle.Shape.Triangle, UiStyle.EdgeGold);
            float d = UiStyle.S(7);
            UiStyle.Icon(new Rect(band.center.x - d * 0.5f, band.y - d * 0.5f, d, d), UiStyle.Shape.Diamond, UiStyle.EdgeGold);
        }

        static float X(Rect band, float delta)
        {
            return band.center.x + delta / HalfSpan * band.width * 0.5f;
        }

        /// <summary>Plus on s'eloigne du centre, plus c'est pale : la bande "tourne".</summary>
        static float Fade(float delta)
        {
            return Mathf.Clamp01(1.15f - Mathf.Abs(delta) / HalfSpan);
        }

        static void DrawMark(Rect band, Mark m, float heading, Vector3 me)
        {
            Vector3 to = m.at - me;
            float bearing = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(heading, bearing);
            bool outside = Mathf.Abs(delta) > HalfSpan - 6f;
            float shown = Mathf.Clamp(delta, -(HalfSpan - 6f), HalfSpan - 6f);
            float px = X(band, shown);

            float size = UiStyle.S(m.size) * (outside ? 0.7f : 1f);
            // Le lointain s'estompe (au-dela de 150 m) : la bande reste lisible, et ce
            // qui est proche ressort. Le chateau et ta stele ne s'estompent jamais.
            float far = new Vector2(to.x, to.z).magnitude;
            bool dim = !m.key && !m.pulse && far > 150f;
            if (dim) size *= 0.75f;
            if (m.pulse) size *= 1f + 0.25f * Mathf.Sin(Time.unscaledTime * 7f);
            float cy = band.center.y;
            Color c = m.color;
            c.a *= outside ? 0.6f : Mathf.Max(0.5f, Fade(delta));
            if (dim) c.a *= 0.55f;

            UiStyle.Icon(new Rect(px - size * 0.5f - 1f, cy - size * 0.5f - 1f, size + 2f, size + 2f), m.shape, new Color(0f, 0f, 0f, 0.7f * c.a));
            UiStyle.Icon(new Rect(px - size * 0.5f, cy - size * 0.5f, size, size), m.shape, c);

            // Au centre (a moins de 8 degres), on lit son nom et sa distance.
            if (!outside && Mathf.Abs(delta) < 8f)
            {
                float metres = new Vector2(to.x, to.z).magnitude;
                string text = string.IsNullOrEmpty(m.label) ? Mathf.RoundToInt(metres) + " m" : m.label + "  " + Mathf.RoundToInt(metres) + " m";
                UiStyle.Tinted(new Rect(px - UiStyle.S(110), band.yMax + UiStyle.S(12), UiStyle.S(220), UiStyle.S(18)), text,
                               UiStyle.CenteredSmall, new Color(m.color.r, m.color.g, m.color.b, 0.95f));
            }
        }

        /// <summary>Rassemble ce que TU sais. Rien d'autre : pas de triche.</summary>
        static void Collect(Vector3 me)
        {
            Marks.Clear();
            Seeker self = Game.Me;
            Hoard h = Game.Hoard;

            Add(Game.CastleCentre, UiStyle.Shape.Square, new Color(0.93f, 0.7f, 0.36f), 9f, "Château", false);

            for (int i = 0; i < Landmarks.All.Count; i++)
            {
                Landmark lm = Landmarks.All[i];
                if (lm != null && lm.Discovered)
                    Add(lm.transform.position, UiStyle.Shape.Dot, new Color(0.72f, 0.7f, 0.64f), 8f, Landmarks.Name(lm.kind), false);
            }

            if (h != null)
            {
                if (h.CampPlanted) Add(h.CampPosition, UiStyle.Shape.Triangle, new Color(0.62f, 0.86f, 0.48f), 12f, "Ton camp", false);
                for (int i = 0; i < h.Caches.Count; i++)
                    Add(h.Caches[i].Position, UiStyle.Shape.Dot, new Color(0.78f, 0.58f, 0.36f), 9f, "Cache " + h.Caches[i].Number, false);
                if (h.StelePlanted)
                    Add(h.StelePosition, UiStyle.Shape.Diamond, Stele.RuneBlue, 15f, "Ta stèle", h.Trophy != null || UrgeStele);
            }

            for (int i = 0; i < Stele.All.Count; i++)
            {
                Stele st = Stele.All[i];
                if (st == null || st.owner == null || st.owner == self || self == null || !self.Knows(st.owner)) continue;
                Add(st.transform.position, UiStyle.Shape.Diamond, st.owner.Colour, 12f, "Stèle de " + st.owner.Name, false);
            }

            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r != null && r.seeker.Hoard.Trophy != null && r.seeker.Hoard.TrophyFrom == self)
                    Add(r.transform.position, UiStyle.Shape.Diamond, new Color(1f, 0.3f, 0.22f), 16f, "VOLEUR " + r.seeker.Name, true);
            }

            // Le mage : pendant la descente et ses premieres secondes, TOUT LE MONDE le
            // voit (c'est le largage). Ensuite, seulement avec la Corne d'appel.
            Mage mage = Game.Mage;
            if (mage != null && (mage.Beaconing || h != null && h.Has(Talisman.Corne) && mage.Present))
                Add(mage.Destination, UiStyle.Shape.Dot, new Color(0.62f, 0.8f, 1f), 16f, mage.Present ? "Le mage" : "Le mage descend", true);

            // Ce que le mage t'a murmure apres une forge.
            for (int i = 0; i < Secrets.All.Count; i++)
            {
                Secrets.Secret s = Secrets.All[i];
                if (s.Resolved) continue;
                Add(s.at, UiStyle.Shape.Diamond, new Color(0.78f, 0.6f, 1f), 12f, s.label, false);
            }
        }

        static void Add(Vector3 at, UiStyle.Shape shape, Color color, float size, string label, bool pulse)
        {
            Mark m = new Mark();
            m.at = at;
            m.shape = shape;
            m.color = color;
            m.size = size;
            m.label = label;
            m.pulse = pulse;
            m.key = shape == UiStyle.Shape.Square || label == "Ta stèle";
            Marks.Add(m);
        }
    }
}
