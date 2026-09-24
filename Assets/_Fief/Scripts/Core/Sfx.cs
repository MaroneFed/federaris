using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Les sons du jeu, SYNTHETISES par le code. Aucun fichier audio, aucun import.
    ///
    /// Comment : un son numerique n'est qu'une liste de nombres entre -1 et 1,
    /// 44100 par seconde. On fabrique ces nombres (sinusoides + bruit + enveloppe
    /// qui decroit) et on les donne a Unity via AudioClip.Create.
    ///
    /// Pourquoi : "une recolte sans tchok ne satisfait pas". Le retour sonore est
    /// ce qui change le plus la sensation, et ca ne coute ni asset ni licence.
    /// On remplacera par de vrais sons plus tard, l'appel ne changera pas.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;

        static AudioSource source;
        static System.Random rng = new System.Random(7);

        static AudioClip[] chop;      // hache dans le bois
        static AudioClip[] pick;      // pioche dans la pierre
        static AudioClip[] clang;     // pic sur le fer
        static AudioClip[] coin;      // vente
        static AudioClip hammer;      // construction
        static AudioClip deny;        // action refusee
        static AudioClip[] step;      // pas
        static AudioClip pop;         // depot / ramassage

        public static bool Muted;

        public static void Init(GameObject host)
        {
            if (host == null) return;

            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.75f;

            rng = new System.Random(7);

            chop = new AudioClip[3];
            pick = new AudioClip[3];
            clang = new AudioClip[3];
            coin = new AudioClip[3];
            step = new AudioClip[4];

            for (int i = 0; i < 3; i++)
            {
                chop[i] = Impact("chop" + i, 165f + i * 14f, 0.16f, 0.55f, 26f);
                pick[i] = Impact("pick" + i, 380f + i * 30f, 0.12f, 0.78f, 40f);
                clang[i] = Metal("clang" + i, 620f + i * 45f, 0.30f);
                coin[i] = Coins("coin" + i, 1180f + i * 90f);
            }
            for (int i = 0; i < 4; i++)
            {
                step[i] = Impact("step" + i, 92f + i * 9f, 0.085f, 0.85f, 52f);
            }

            hammer = Impact("hammer", 120f, 0.34f, 0.5f, 11f);
            deny = Buzz("deny", 128f, 0.22f);
            pop = Impact("pop", 520f, 0.09f, 0.25f, 46f);
        }

        // ------------------------------------------------------------- lecture

        public static void Harvest(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Deadwood: Play(Pick(chop), 0.75f); break;
                case ResourceType.Moonstone: Play(Pick(pick), 0.65f); break;
                case ResourceType.Iron: Play(Pick(clang), 0.55f); break;
            }
        }

        /// <summary>Coup leger, joue en boucle pendant qu'on frappe l'arbre ou le rocher.</summary>
        public static void HarvestTap(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Deadwood: Play(Pick(chop), 0.38f); break;
                case ResourceType.Moonstone: Play(Pick(pick), 0.32f); break;
                case ResourceType.Iron: Play(Pick(clang), 0.26f); break;
            }
        }

        public static void Coin() { Play(Pick(coin), 0.55f); }
        public static void Build() { Play(hammer, 0.85f); }
        public static void Deny() { Play(deny, 0.45f); }
        public static void Pop() { Play(pop, 0.5f); }
        public static void Step() { Play(Pick(step), 0.22f); }

        // ================================================================== la foret
        //
        // Les sons d'ambiance sont des CLIPS, pas des lectures : c'est Soundscape
        // qui les joue, depuis un point de l'espace (un hibou dans un arbre a droite,
        // une branche qui casse derriere). Ils sont fabriques une fois, a la demande.

        static AudioClip wind, owl, crack, creak, howl;

        /// <summary>
        /// Le vent dans les cimes : du bruit, adouci par un filtre dont la frequence
        /// monte et descend (les rafales). Huit secondes qui bouclent sans couture :
        /// la derniere seconde est fondue dans la premiere.
        /// </summary>
        public static AudioClip Wind()
        {
            if (wind != null) return wind;
            const float length = 8f, overlap = 1f;
            int count = Mathf.RoundToInt(Rate * length);
            int extra = Mathf.RoundToInt(Rate * overlap);
            float[] raw = new float[count + extra];
            System.Random r = new System.Random(11);
            float low = 0f, lower = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = (float)i / Rate;
                float gust = 0.5f + 0.5f * Mathf.Sin(t * 0.7f) * Mathf.Sin(t * 0.23f + 1f);
                float alpha = Mathf.Lerp(0.015f, 0.07f, gust);
                float noise = (float)(r.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * alpha;
                lower += (low - lower) * 0.08f;
                raw[i] = lower * (0.6f + gust * 0.8f);
            }
            float[] data = new float[count];
            for (int i = 0; i < count; i++) data[i] = raw[i];
            for (int i = 0; i < extra; i++)
            {
                float k = (float)i / extra;
                data[i] = raw[count + i] * (1f - k) + raw[i] * k;
            }
            Normalize(data, 0.8f);
            wind = AudioClip.Create("vent", count, 1, Rate, false);
            wind.SetData(data, 0);
            return wind;
        }

        /// <summary>Un hibou : "hou... hou-hou", trois notes graves qui glissent un peu.</summary>
        public static AudioClip Owl()
        {
            if (owl != null) return owl;
            int count = Mathf.RoundToInt(Rate * 2.1f);
            float[] data = new float[count];
            float[] starts = { 0f, 0.95f, 1.3f };
            float[] lengths = { 0.5f, 0.28f, 0.55f };
            for (int n = 0; n < starts.Length; n++)
            {
                int from = Mathf.RoundToInt(starts[n] * Rate);
                int len = Mathf.RoundToInt(lengths[n] * Rate);
                float phase = 0f;
                for (int i = 0; i < len && from + i < count; i++)
                {
                    float u = (float)i / len;
                    float f = Mathf.Lerp(390f, 350f, u);
                    phase += 2f * Mathf.PI * f / Rate;
                    float env = Mathf.Sin(u * Mathf.PI);
                    env *= env;
                    data[from + i] += (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.12f) * env;
                }
            }
            Normalize(data, 0.7f);
            owl = FromSamples("hibou", data);
            return owl;
        }

        /// <summary>Une branche qui casse : un claquement sec, puis un second plus petit.</summary>
        public static AudioClip Crack()
        {
            if (crack != null) return crack;
            int count = Mathf.RoundToInt(Rate * 0.6f);
            float[] data = new float[count];
            System.Random r = new System.Random(5);
            float[] at = { 0f, 0.13f, 0.21f };
            float[] level = { 1f, 0.5f, 0.25f };
            for (int k = 0; k < at.Length; k++)
            {
                int from = Mathf.RoundToInt(at[k] * Rate);
                for (int i = from; i < count; i++)
                {
                    float t = (float)(i - from) / Rate;
                    float snap = (float)(r.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-70f * t);
                    float body = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-22f * t) * 0.5f;
                    data[i] += (snap + body) * level[k];
                }
            }
            Normalize(data, 0.85f);
            crack = FromSamples("branche", data);
            return crack;
        }

        /// <summary>
        /// Un grand arbre qui grince : une suite de petits chocs irreguliers, comme
        /// du bois qui frotte sur du bois ("stick-slip"). Leur cadence monte, puis
        /// redescend : c'est ce qui en fait une plainte.
        /// </summary>
        public static AudioClip Creak()
        {
            if (creak != null) return creak;
            const float length = 1.6f;
            int count = Mathf.RoundToInt(Rate * length);
            float[] data = new float[count];
            System.Random r = new System.Random(9);
            float t = 0.05f;
            while (t < length - 0.1f)
            {
                float u = t / length;
                float rateHz = Mathf.Lerp(70f, 140f, Mathf.Sin(u * Mathf.PI));
                float amp = Mathf.Sin(u * Mathf.PI);
                int from = Mathf.RoundToInt(t * Rate);
                int len = Mathf.RoundToInt(Rate * 0.012f);
                for (int i = 0; i < len && from + i < count; i++)
                {
                    float x = (float)i / Rate;
                    data[from + i] += Mathf.Sin(2f * Mathf.PI * 820f * x) * Mathf.Exp(-400f * x) * amp;
                }
                t += 1f / rateHz * (0.8f + (float)r.NextDouble() * 0.4f);
            }
            Normalize(data, 0.6f);
            creak = FromSamples("grincement", data);
            return creak;
        }

        /// <summary>
        /// Un loup, tres loin : une note qui monte, tient, redescend, avec un leger
        /// vibrato, et deux echos plus faibles -- la foret qui repond.
        /// </summary>
        public static AudioClip Howl()
        {
            if (howl != null) return howl;
            const float voice = 3.2f;
            int count = Mathf.RoundToInt(Rate * (voice + 1.4f));
            float[] dry = new float[count];
            float phase = 0f;
            int len = Mathf.RoundToInt(Rate * voice);
            for (int i = 0; i < len; i++)
            {
                float u = (float)i / len;
                float contour = u < 0.25f ? Mathf.Lerp(330f, 520f, u / 0.25f)
                              : u < 0.7f ? Mathf.Lerp(520f, 470f, (u - 0.25f) / 0.45f)
                              : Mathf.Lerp(470f, 300f, (u - 0.7f) / 0.3f);
                float t = (float)i / Rate;
                float f = contour + Mathf.Sin(t * 2f * Mathf.PI * 5f) * 6f;
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Min(1f, u * 8f) * Mathf.Min(1f, (1f - u) * 5f);
                dry[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 3f) * 0.08f) * env;
            }
            float[] data = new float[count];
            int d1 = Mathf.RoundToInt(Rate * 0.37f), d2 = Mathf.RoundToInt(Rate * 0.83f);
            for (int i = 0; i < count; i++)
            {
                data[i] = dry[i];
                if (i >= d1) data[i] += dry[i - d1] * 0.3f;
                if (i >= d2) data[i] += dry[i - d2] * 0.14f;
            }
            Normalize(data, 0.6f);
            howl = FromSamples("loup", data);
            return howl;
        }

        static AudioClip rain, thunder, arrival, forge;

        /// <summary>
        /// L'arrivee du mage : une nappe qui enfle en montant (80 -> 220 Hz), avec
        /// un souffle, et retombe. Deux secondes et demie. On l'entend partout : c'est
        /// l'annonce, pas sa voix (sa voix, elle, vient de lui).
        /// </summary>
        public static void MageArrives()
        {
            if (arrival == null)
            {
                const float length = 2.6f;
                int count = Mathf.RoundToInt(Rate * length);
                float[] data = new float[count];
                System.Random r = new System.Random(71);
                float phase = 0f, phase2 = 0f, low = 0f;
                for (int i = 0; i < count; i++)
                {
                    float u = (float)i / count;
                    float f = Mathf.Lerp(80f, 220f, Mathf.SmoothStep(0f, 1f, u));
                    phase += 2f * Mathf.PI * f / Rate;
                    phase2 += 2f * Mathf.PI * f * 1.5f / Rate;
                    float env = Mathf.Sin(u * Mathf.PI);
                    float noise = (float)(r.NextDouble() * 2.0 - 1.0);
                    low += (noise - low) * 0.05f;
                    data[i] = (Mathf.Sin(phase) + Mathf.Sin(phase2) * 0.4f + low * 2f) * env;
                }
                Normalize(data, 0.7f);
                arrival = FromSamples("arrivee", data);
            }
            Play(arrival, 0.8f);
        }

        /// <summary>La forge : un coup sourd, puis une pluie de clochettes qui monte.</summary>
        public static void Forge()
        {
            if (forge == null)
            {
                const float length = 2.4f;
                int count = Mathf.RoundToInt(Rate * length);
                float[] data = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate;
                    data[i] += Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-3.5f * t) * 1.2f;
                    data[i] += Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-5f * t) * 0.5f;
                }
                float[] notes = { 659.25f, 783.99f, 987.77f, 1318.5f };
                for (int n = 0; n < notes.Length; n++)
                {
                    int start = Mathf.RoundToInt(Rate * (0.12f + n * 0.09f));
                    for (int i = start; i < count; i++)
                    {
                        float t = (float)(i - start) / Rate;
                        data[i] += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * Mathf.Exp(-3f * t) * 0.35f
                                 + Mathf.Sin(2f * Mathf.PI * notes[n] * 2.76f * t) * Mathf.Exp(-6f * t) * 0.1f;
                    }
                }
                Normalize(data, 0.9f);
                forge = FromSamples("forge", data);
            }
            Play(forge, 1f);
        }

        /// <summary>
        /// La pluie : du bruit dont on a retire les graves (on garde le crepitement),
        /// avec de petites gouttes plus fortes semees dedans. Six secondes, en boucle.
        /// </summary>
        public static AudioClip Rain()
        {
            if (rain != null) return rain;
            const float length = 6f, overlap = 0.8f;
            int count = Mathf.RoundToInt(Rate * length);
            int extra = Mathf.RoundToInt(Rate * overlap);
            float[] raw = new float[count + extra];
            System.Random r = new System.Random(21);
            float low = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float noise = (float)(r.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.12f;
                float hiss = noise - low;                       // passe-haut : le crepitement
                raw[i] = hiss * 0.5f + low * 0.35f;
                if (r.NextDouble() < 0.0009) raw[i] += (float)(r.NextDouble() - 0.5) * 2.5f;
            }
            float[] data = new float[count];
            for (int i = 0; i < count; i++) data[i] = raw[i];
            for (int i = 0; i < extra; i++)
            {
                float k = (float)i / extra;
                data[i] = raw[count + i] * (1f - k) + raw[i] * k;
            }
            Normalize(data, 0.7f);
            rain = AudioClip.Create("pluie", count, 1, Rate, false);
            rain.SetData(data, 0);
            return rain;
        }

        /// <summary>
        /// Le tonnerre : un craquement, puis un grondement grave qui roule et
        /// s'eteint en quatre secondes (du bruit tres filtre, module par des vagues).
        /// </summary>
        public static AudioClip Thunder()
        {
            if (thunder != null) return thunder;
            const float length = 5f;
            int count = Mathf.RoundToInt(Rate * length);
            float[] data = new float[count];
            System.Random r = new System.Random(33);
            float low = 0f, lower = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(r.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.03f;
                lower += (low - lower) * 0.05f;
                float roll = 0.6f + 0.4f * Mathf.Sin(t * 7f) * Mathf.Sin(t * 2.3f);
                float env = Mathf.Min(1f, t * 12f) * Mathf.Exp(-0.9f * t);
                float crackle = noise * Mathf.Exp(-18f * t) * 0.25f;
                data[i] = lower * roll * env * 6f + crackle;
            }
            Normalize(data, 0.95f);
            thunder = FromSamples("tonnerre", data);
            return thunder;
        }

        /// <summary>Le clip de la cloche, pour la faire sonner depuis le chateau.</summary>
        public static AudioClip BellClip()
        {
            if (bell == null) BuildBell();
            return bell;
        }

        static void Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            float k = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= k;
        }

        static AudioClip discovery;

        /// <summary>
        /// Une trouvaille : trois notes qui montent (do, mi, sol, une octave au-dessus
        /// de la cloche), chacune avec ses partiels de clochette. Court, clair, et
        /// reconnaissable entre tous : on l'entend, on sait qu'on a trouve quelque chose.
        /// </summary>
        public static void Discovery()
        {
            if (discovery == null)
            {
                const float duration = 2.2f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                float[] notes = { 523.25f, 659.25f, 783.99f };
                for (int n = 0; n < notes.Length; n++)
                {
                    int start = Mathf.RoundToInt(Rate * n * 0.13f);
                    for (int i = start; i < count; i++)
                    {
                        float t = (float)(i - start) / Rate;
                        float env = Mathf.Min(1f, t * 300f) * Mathf.Exp(-2.6f * t);
                        float v = Mathf.Sin(2f * Mathf.PI * notes[n] * t)
                                + Mathf.Sin(2f * Mathf.PI * notes[n] * 2.76f * t) * 0.25f * Mathf.Exp(-4f * t)
                                + Mathf.Sin(2f * Mathf.PI * notes[n] * 5.4f * t) * 0.1f * Mathf.Exp(-7f * t);
                        data[i] += v * env * 0.22f;
                    }
                }
                discovery = FromSamples("trouvaille", data);
            }
            Play(discovery, 0.9f);
        }

        static AudioClip bell;

        /// <summary>
        /// La cloche de fin de Saison. Une cloche n'a pas des harmoniques "justes"
        /// (x2, x3...) comme une corde : ses partiels sont decales (x2,0 ; x2,4 ; x3 ;
        /// x4,5). C'est ce decalage qui fait qu'on reconnait une cloche.
        /// </summary>
        public static void Bell()
        {
            if (bell == null) BuildBell();
            Play(bell, 1f);
        }

        static void BuildBell()
        {
            {
                const float duration = 5f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                float[] ratios = { 0.5f, 1f, 2f, 2.4f, 3f, 4.5f };
                float[] levels = { 0.35f, 1f, 0.6f, 0.45f, 0.3f, 0.2f };
                float[] decays = { 0.5f, 0.8f, 1.2f, 1.6f, 2.2f, 3.2f };
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate;
                    float attack = Mathf.Min(1f, t * 400f);
                    float v = 0f;
                    for (int p = 0; p < ratios.Length; p++)
                        v += Mathf.Sin(2f * Mathf.PI * 196f * ratios[p] * t) * levels[p] * Mathf.Exp(-decays[p] * t);
                    data[i] = v * attack * 0.3f;
                }
                bell = FromSamples("cloche", data);
            }
        }

        static AudioClip Pick(AudioClip[] bank)
        {
            if (bank == null || bank.Length == 0) return null;
            return bank[rng.Next(bank.Length)];
        }

        static void Play(AudioClip clip, float volume)
        {
            if (Muted || source == null || clip == null) return;
            source.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------- synthese

        /// <summary>Un choc : bruit + une basse, le tout qui s'eteint tres vite.</summary>
        static AudioClip Impact(string name, float frequency, float duration, float noiseAmount, float decay)
        {
            int count = Mathf.RoundToInt(Rate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float envelope = Mathf.Exp(-decay * t);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = envelope * (tone * (1f - noiseAmount) + noise * noiseAmount) * 0.85f;
            }

            return FromSamples(name, data);
        }

        /// <summary>Un son metallique : deux harmoniques volontairement desaccordees.</summary>
        static AudioClip Metal(string name, float frequency, float duration)
        {
            int count = Mathf.RoundToInt(Rate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float envelope = Mathf.Exp(-14f * t);
                float a = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float b = Mathf.Sin(2f * Mathf.PI * frequency * 2.76f * t) * 0.6f;
                float c = Mathf.Sin(2f * Mathf.PI * frequency * 5.4f * t) * 0.25f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-90f * t) * 0.5f;
                data[i] = envelope * (a + b + c) * 0.3f + noise * 0.3f;
            }

            return FromSamples(name, data);
        }

        /// <summary>Deux notes claires qui montent : le son de l'argent qui rentre.</summary>
        static AudioClip Coins(string name, float frequency)
        {
            float duration = 0.26f;
            int count = Mathf.RoundToInt(Rate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float value = 0f;

                // premiere note
                float e1 = Mathf.Exp(-22f * t);
                value += Mathf.Sin(2f * Mathf.PI * frequency * t) * e1;

                // seconde note, une quinte au-dessus, legerement retardee
                float t2 = t - 0.07f;
                if (t2 > 0f)
                {
                    float e2 = Mathf.Exp(-20f * t2);
                    value += Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * t2) * e2;
                }

                data[i] = value * 0.33f;
            }

            return FromSamples(name, data);
        }

        /// <summary>Un grognement bas : "non".</summary>
        static AudioClip Buzz(string name, float frequency, float duration)
        {
            int count = Mathf.RoundToInt(Rate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float envelope = Mathf.Min(1f, t * 30f) * Mathf.Exp(-9f * t);
                float saw = Mathf.Repeat(frequency * t, 1f) * 2f - 1f;
                data[i] = envelope * saw * 0.35f;
            }

            return FromSamples(name, data);
        }

        static AudioClip drone;

        /// <summary>
        /// LA VOIX DU MAGE : un bourdonnement grave, en boucle, qu'on entend a travers
        /// la brume bien avant de voir quoi que ce soit.
        ///
        /// Pour qu'une boucle ne "claque" pas a chaque tour, chaque frequence doit
        /// faire un nombre ENTIER de periodes dans la duree du son (6 s). 110 Hz fait
        /// 660 periodes, 110,5 Hz en fait 663 : les deux ensemble battent doucement,
        /// une fois toutes les deux secondes, et c'est ce battement qui sonne "vivant".
        ///
        /// Les harmoniques 3 et 4 montent et descendent lentement, a contretemps :
        /// l'oreille y entend une voix qui change de voyelle, comme un chant sans mots.
        /// </summary>
        public static AudioClip Drone()
        {
            if (drone != null) return drone;

            const float length = 6f;
            int count = Mathf.RoundToInt(Rate * length);
            float[] data = new float[count];
            float peak = 0.0001f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float phase = t / length;                       // 0 -> 1 sur la boucle
                float vowelA = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * phase);
                float vowelB = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * phase * 2f + 1.3f);
                float swell = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * phase * 3f);

                float v = Mathf.Sin(2f * Mathf.PI * 110f * t)
                        + Mathf.Sin(2f * Mathf.PI * 110.5f * t) * 0.8f
                        + Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.45f
                        + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.35f * vowelA
                        + Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.22f * vowelB
                        + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.3f;   // la quinte, lointaine
                v *= swell;
                data[i] = v;
                peak = Mathf.Max(peak, Mathf.Abs(v));
            }

            for (int i = 0; i < count; i++) data[i] *= 0.8f / peak;

            // PAS de FromSamples : son fondu de fin creerait un trou a chaque tour.
            drone = AudioClip.Create("mage", count, 1, Rate, false);
            drone.SetData(data, 0);
            return drone;
        }

        static AudioClip FromSamples(string name, float[] data)
        {
            // Petit fondu de fin : sans lui, la coupure nette fait un "clic".
            int fade = Mathf.Min(600, data.Length / 4);
            for (int i = 0; i < fade; i++)
            {
                data[data.Length - 1 - i] *= (float)i / fade;
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
