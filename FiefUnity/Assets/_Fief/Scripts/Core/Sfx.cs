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
                case ResourceType.Wood: Play(Pick(chop), 0.75f); break;
                case ResourceType.Stone: Play(Pick(pick), 0.65f); break;
                case ResourceType.Iron: Play(Pick(clang), 0.55f); break;
            }
        }

        public static void Coin() { Play(Pick(coin), 0.55f); }
        public static void Build() { Play(hammer, 0.85f); }
        public static void Deny() { Play(deny, 0.45f); }
        public static void Pop() { Play(pop, 0.5f); }
        public static void Step() { Play(Pick(step), 0.22f); }

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
