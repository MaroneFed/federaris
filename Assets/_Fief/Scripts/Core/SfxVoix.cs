using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CRIS DU MODE MEME (v46 -- Martin : "le mode meme, tous les memes d'internet, genre 67,
    /// fahhh"). Une petite VOIX fabriquee par le code (une synthese a formants : des impulsions de
    /// gorge filtrees par trois resonances, comme une vraie bouche, et du souffle pour les s, f, ch).
    /// Elle crie : FAHHH, SIX SEVEN, BRUH, SHEESH, OOF, WOW, NOOON.
    ///
    /// Ce n'est pas l'enregistrement d'origine (il appartient a quelqu'un) : c'est un haricot qui
    /// le crie. Martin peut deposer les vrais dans Resources/Sons/Memes/Fahhh, SixSept... pour jouer
    /// entre amis -- un fichier pose la passe devant (Sfx.Meme).
    /// </summary>
    public static partial class Sfx
    {
        /// <summary>Un son de la voix : V voyelle, N nasale (m, n), Z voisee + souffle (v, r), S souffle (s, f, ch), P silence.</summary>
        struct Ph
        {
            public char Kind;
            public float Dur;
            public float F1;
            public float F2;
            public float F3;
            public float Pitch0;
            public float Pitch1;
            public float Amp;
            public Ph(char kind, float dur, float f1, float f2, float f3, float p0, float p1, float amp)
            {
                Kind = kind; Dur = dur; F1 = f1; F2 = f2; F3 = f3; Pitch0 = p0; Pitch1 = p1; Amp = amp;
            }
        }

        // Les sons de base. Pour un souffle (S), F1 est le centre du bruit et F2 sa largeur.
        static Ph Vow(float dur, float f1, float f2, float f3, float p0, float p1, float amp) { return new Ph('V', dur, f1, f2, f3, p0, p1, amp); }
        static Ph Fric(float dur, float centre, float width, float amp) { return new Ph('S', dur, centre, width, 0f, 0f, 0f, amp); }
        static Ph Nas(float dur, float f2, float p0, float p1, float amp) { return new Ph('N', dur, 270f, f2, 2600f, p0, p1, amp); }
        static Ph Buzz(float dur, float f1, float f2, float p, float amp) { return new Ph('Z', dur, f1, f2, 2500f, p, p, amp); }
        static Ph Gap(float dur) { return new Ph('P', dur, 0f, 0f, 0f, 0f, 0f, 0f); }

        static AudioClip MakeVoice(string kind)
        {
            switch (kind)
            {
                // Le cri "FAAAAHHH" : un f, puis un A immense qui descend et tremble, et le souffle a la fin.
                case "Fahhh": return Speak("meme fahhh", 1.25f,
                    Fric(0.13f, 1800f, 2400f, 0.5f),
                    Vow(0.25f, 820f, 1250f, 2600f, 290f, 300f, 1f),
                    Vow(0.85f, 850f, 1220f, 2550f, 300f, 190f, 1f),
                    Fric(0.4f, 1200f, 1600f, 0.35f));
                // "SIX... SEVEN" : deux mots, le second qui tombe.
                case "SixSept": return Speak("meme six sept", 1.1f,
                    Fric(0.13f, 6500f, 2500f, 0.6f),
                    Vow(0.13f, 330f, 2150f, 2900f, 190f, 200f, 1f),
                    Gap(0.04f),
                    Fric(0.03f, 2000f, 1200f, 0.5f),
                    Fric(0.11f, 6500f, 2500f, 0.55f),
                    Gap(0.14f),
                    Fric(0.12f, 6500f, 2500f, 0.6f),
                    Vow(0.17f, 560f, 1850f, 2550f, 210f, 180f, 1f),
                    Buzz(0.06f, 300f, 1300f, 175f, 0.5f),
                    Vow(0.08f, 500f, 1450f, 2500f, 170f, 160f, 0.8f),
                    Nas(0.18f, 1700f, 160f, 140f, 0.55f));
                // "BRUH" : grave, plat, decu.
                case "Bruh": return Speak("meme bruh", 1f,
                    Gap(0.02f),
                    Buzz(0.04f, 250f, 800f, 115f, 0.4f),
                    Buzz(0.07f, 420f, 1250f, 112f, 0.6f),
                    Vow(0.42f, 620f, 1180f, 2400f, 110f, 92f, 1f),
                    Fric(0.12f, 1100f, 1500f, 0.2f));
                // "SHEEEESH" : ch, un i qui monte puis retombe, ch.
                case "Sheesh": return Speak("meme sheesh", 1.1f,
                    Fric(0.16f, 3000f, 1800f, 0.6f),
                    Vow(0.3f, 290f, 2300f, 3000f, 210f, 270f, 1f),
                    Vow(0.4f, 290f, 2300f, 3000f, 270f, 200f, 1f),
                    Fric(0.3f, 3000f, 1800f, 0.55f));
                // "OOF" : un ou coupe net.
                case "Oof": return Speak("meme oof", 1f,
                    Vow(0.2f, 360f, 820f, 2300f, 150f, 125f, 1f),
                    Fric(0.13f, 1500f, 2400f, 0.35f));
                // "WOOOW" : ou-a-ou, qui monte.
                case "Wow": return Speak("meme wow", 1f,
                    Vow(0.1f, 300f, 700f, 2300f, 180f, 200f, 0.7f),
                    Vow(0.35f, 760f, 1150f, 2500f, 200f, 300f, 1f),
                    Vow(0.25f, 420f, 800f, 2400f, 300f, 260f, 0.9f));
                // "NOOOON" : n, un o qui descend, n.
                case "Nooo": return Speak("meme nooo", 1.2f,
                    Nas(0.08f, 1500f, 240f, 250f, 0.6f),
                    Vow(1f, 480f, 880f, 2450f, 260f, 150f, 1f),
                    Nas(0.15f, 1000f, 150f, 130f, 0.5f));
            }
            return null;
        }

        /// <summary>
        /// Faire parler la voix. Chaque son glisse vers le suivant (les resonances bougent comme une
        /// bouche), le tout un peu sature : un meme, ca crie.
        /// </summary>
        static AudioClip Speak(string name, float drive, params Ph[] phones)
        {
            float total = 0.05f;
            for (int i = 0; i < phones.Length; i++) total += phones[i].Dur;
            float[] d = Buffer(total);
            System.Random noise = new System.Random(name.Length * 977);
            // Trois resonances de la bouche, et une pour le souffle.
            float[] y1 = new float[3], y2 = new float[3];
            float n1 = 0f, n2 = 0f;
            float phase = 0f, lastPulse = 0f;
            float f1 = phones[0].F1, f2 = phones[0].F2, f3 = phones[0].F3;
            int at = 0;
            for (int p = 0; p < phones.Length; p++)
            {
                Ph ph = phones[p];
                int len = Mathf.RoundToInt(ph.Dur * Rate);
                for (int i = 0; i < len && at < d.Length; i++, at++)
                {
                    float u = (float)i / Mathf.Max(1, len);
                    // L'attaque et la chute de chaque son, pour qu'ils ne claquent pas.
                    float env = Mathf.Clamp01(i / (0.012f * Rate)) * Mathf.Clamp01((len - i) / (0.02f * Rate));
                    float white = (float)(noise.NextDouble() * 2.0 - 1.0);
                    float s = 0f;
                    if (ph.Kind == 'V' || ph.Kind == 'N' || ph.Kind == 'Z')
                    {
                        // Les resonances glissent vers celles du son (le premier tiers).
                        float k = 1f - Mathf.Exp(-6f / (0.33f * Mathf.Max(1, len)));
                        f1 += (ph.F1 - f1) * k; f2 += (ph.F2 - f2) * k; f3 += (ph.F3 - f3) * k;
                        // La gorge : une impulsion par periode (Rosenberg), un peu de vibrato sur les longues.
                        float vib = ph.Dur > 0.4f ? 1f + 0.025f * Mathf.Sin(u * ph.Dur * 2f * Mathf.PI * 5.5f) * u : 1f;
                        float pitch = Mathf.Lerp(ph.Pitch0, ph.Pitch1, u) * vib;
                        phase = Mathf.Repeat(phase + pitch / Rate, 1f);
                        float g = phase < 0.4f ? 0.5f * (1f - Mathf.Cos(Mathf.PI * phase / 0.4f)) : phase < 0.56f ? Mathf.Cos(Mathf.PI * (phase - 0.4f) / 0.32f) : 0f;
                        float src = (g - lastPulse) * 8f + white * 0.04f;
                        lastPulse = g;
                        if (ph.Kind == 'Z') src += white * 0.25f;
                        float[] fs = { f1, f2, f3 };
                        float[] gains = { 1f, ph.Kind == 'N' ? 0.25f : 0.7f, ph.Kind == 'N' ? 0.1f : 0.35f };
                        for (int r = 0; r < 3; r++)
                            s += gains[r] * Resonate(src, fs[r], 70f + 30f * r, ref y1[r], ref y2[r]);
                    }
                    else if (ph.Kind == 'S')
                    {
                        s = Resonate(white, ph.F1, ph.F2, ref n1, ref n2) * 3f;
                        lastPulse = 0f;
                    }
                    d[at] = s * ph.Amp * env;
                }
            }
            // Un peu sature, comme un cri dans un micro.
            for (int i = 0; i < d.Length; i++) d[i] = (float)System.Math.Tanh(d[i] * drive * 3f);
            Normalize(d, 0.9f);
            return FromSamples(name, d);
        }

        /// <summary>Une resonance (filtre a deux poles) a "freq" Hz, large de "width" Hz.</summary>
        static float Resonate(float x, float freq, float width, ref float y1, ref float y2)
        {
            float r = Mathf.Exp(-Mathf.PI * width / Rate);
            float c = 2f * r * Mathf.Cos(2f * Mathf.PI * Mathf.Min(freq, Rate * 0.45f) / Rate);
            float y = (1f - r) * x + c * y1 - r * r * y2;
            y2 = y1;
            y1 = y;
            return y;
        }
    }
}
