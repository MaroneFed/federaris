using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA BOUSSOLE, en haut de l'ecran : une bande de cuir qui s'efface aux deux
    /// bouts, des graduations tous les 15 degres, les points cardinaux (le Nord en
    /// cramoisi), une aiguille de bronze au centre.
    ///
    /// ET RIEN D'AUTRE (Martin, 25/09/2026 : "faut rien indiquer sur la boussole,
    /// comme ca ca force a retenir"). Ni ta stele, ni le mage, ni le chateau. Elle
    /// dit ou est le nord -- le reste, c'est a toi de t'en souvenir : "ma stele est
    /// au sud-ouest du chateau, apres le grand chene". C'est ca, se perdre dans une
    /// foret, et c'est ca qui la rend immense.
    /// </summary>
    public static class Compass
    {
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
    }
}
