using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES SONS DU MODE MEME (v45 -- Martin : "fais le mode meme, avec son"). Onze bruits de meme,
    /// chacun dans son dossier : Resources/Sons/Memes/Klaxon, Trombone, Scratch, Bonk, Boum, Fanfare,
    /// Suspense, Couic, Faux, Sifflet, Caisse. Un fichier pose dans le dossier (.wav, .mp3, .ogg) est
    /// joue a la place, au hasard s'il y en a plusieurs ; sans fichier, le code fabrique le sien.
    ///
    /// Pourquoi fabriques et pas telecharges : les vrais sons de meme (le "vine boom", le "bruh", le
    /// klaxon MLG...) appartiennent a quelqu'un. Les mettre dans un jeu vendu sur Steam, c'est risquer
    /// le retrait de la page. Ceux-ci sont a nous. Martin peut deposer les siens dans les dossiers
    /// pour jouer entre amis -- a remplacer par des sons libres (CC0) avant la sortie.
    /// </summary>
    public static partial class Sfx
    {
        static readonly Dictionary<string, AudioClip> memeSynth = new Dictionary<string, AudioClip>();

        /// <summary>Le son de meme "kind" : un fichier du dossier Sons/Memes/kind, sinon le notre.</summary>
        public static void Meme(string kind, float volume)
        {
            if (Muted || source == null) return;
            PlayMeme(kind, volume);
        }

        /// <summary>(v46.1) Le meme crie par un joueur : le son part de lui (on l'entend jusqu'a ~60 m).</summary>
        public static void MemeAt(string kind, Vector3 at, float volume)
        {
            if (Muted || source == null) return;
            Begin3D(at, 18f);
            PlayMeme(kind, volume);
            End3D();
        }

        /// <summary>
        /// (v46.2 -- "le vrai meme qui gueule, hyper fort") : d'abord le VRAI son (MemesSons/, a cote
        /// du jeu), joue DEUX FOIS en meme temps -- deux fois plus fort qu'un son normal ; puis un
        /// fichier de Resources/Sons/Memes ; sinon celui du code.
        /// </summary>
        static void PlayMeme(string kind, float volume)
        {
            AudioClip real = MemeSounds.Real(kind);
            if (real != null) { Play(real, 1f); Play(real, 1f); return; }
            if (PlayReal("Memes/" + kind, Mathf.Max(volume, 1f))) return;
            Play(MemeClip(MemeSounds.FallbackOf(kind)), Mathf.Max(volume, 1f));
        }

        static AudioClip MemeClip(string kind)
        {
            AudioClip clip;
            if (!memeSynth.TryGetValue(kind, out clip) || clip == null)
            {
                clip = MakeMeme(kind);
                memeSynth[kind] = clip;
            }
            return clip;
        }

        static AudioClip MakeMeme(string kind)
        {
            switch (kind)
            {
                case "Klaxon": return MemeKlaxon();
                case "Trombone": return MemeTrombone();
                case "Scratch": return MemeScratch();
                case "Bonk": return MemeBonk();
                case "Boum": return MemeBoum();
                case "Fanfare": return MemeFanfare();
                case "Suspense": return MemeSuspense();
                case "Couic": return MemeCouic();
                case "Faux": return MemeFaux();
                case "Sifflet": return MemeSifflet();
                case "Caisse": return MemeCaisse();
            }
            // (v46) Les cris : FAHHH, SIX SEVEN, BRUH... (SfxVoix).
            return MakeVoice(kind);
        }

        static float[] Buffer(float seconds) { return new float[Mathf.RoundToInt(Rate * seconds)]; }
        static float Saw(float phase) { return 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f)); }

        /// <summary>Le klaxon de stade : trois coups d'un accord de dents de scie, sature.</summary>
        static AudioClip MemeKlaxon()
        {
            float[] d = Buffer(1.3f);
            float[] freqs = { 466f, 587f, 698f };
            float[] ph = new float[3];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float blast = t < 0.2f ? 1f : t < 0.28f ? 0f : t < 0.48f ? 1f : t < 0.56f ? 0f : t < 1.25f ? 1f : 0f;
                float s = 0f;
                for (int k = 0; k < 3; k++) { ph[k] += 2f * Mathf.PI * freqs[k] * (1f + 0.004f * Mathf.Sin(t * 30f)) / Rate; s += Saw(ph[k]); }
                d[i] = Mathf.Clamp(s * 0.9f, -1f, 1f) * blast;
            }
            Normalize(d, 0.9f);
            return FromSamples("meme klaxon", d);
        }

        /// <summary>Le trombone triste : quatre notes qui descendent, la derniere qui tremble ("wah wah wah waaah").</summary>
        static AudioClip MemeTrombone()
        {
            float[] d = Buffer(2.4f);
            float[] notes = { 311f, 294f, 277f, 262f };
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                int n = Mathf.Min(3, Mathf.FloorToInt(t / 0.45f));
                float local = t - n * 0.45f;
                float f = notes[n] * (n == 3 ? 1f + 0.03f * Mathf.Sin(local * 2f * Mathf.PI * 6f) : 1f);
                phase += 2f * Mathf.PI * f / Rate;
                float env = n < 3 ? Mathf.Sin(Mathf.Clamp01(local / 0.42f) * Mathf.PI) : Mathf.Min(1f, local * 8f) * Mathf.Exp(-0.8f * local);
                // Une sourdine qui s'ouvre et se ferme : le "wah".
                float wah = 0.5f + 0.5f * Mathf.Sin(Mathf.Clamp01(local / 0.45f) * Mathf.PI);
                d[i] = (Saw(phase) * (0.4f + 0.6f * wah) + Mathf.Sin(phase) * 0.5f) * env;
            }
            Normalize(d, 0.85f);
            return FromSamples("meme trombone", d);
        }

        /// <summary>Le disque raye : un bruit filtre qui monte et descend tres vite.</summary>
        static AudioClip MemeScratch()
        {
            float[] d = Buffer(0.55f);
            System.Random r = new System.Random(11);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float sweep = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 7f));
                float n = (float)(r.NextDouble() * 2.0 - 1.0);
                lp += (n - lp) * (0.05f + 0.6f * sweep);
                d[i] = lp * (1f - t / 0.55f) * 1.6f;
            }
            Normalize(d, 0.9f);
            return FromSamples("meme scratch", d);
        }

        /// <summary>Le BONK : un coup de bois sec et creux.</summary>
        static AudioClip MemeBonk()
        {
            float[] d = Buffer(0.35f);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = Mathf.Lerp(520f, 260f, Mathf.Clamp01(t * 12f));
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = (Mathf.Sin(phase) + 0.4f * Mathf.Sin(phase * 2.7f)) * Mathf.Exp(-22f * t);
            }
            Normalize(d, 0.95f);
            return FromSamples("meme bonk", d);
        }

        /// <summary>Le gros BOUM dramatique : une basse qui tombe, saturee, et qui resonne.</summary>
        static AudioClip MemeBoum()
        {
            float[] d = Buffer(1.6f);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = Mathf.Lerp(110f, 38f, Mathf.Clamp01(t * 3f));
                phase += 2f * Mathf.PI * f / Rate;
                float s = Mathf.Sin(phase) * 2.2f;
                d[i] = (float)System.Math.Tanh(s) * Mathf.Exp(-2.2f * t) * Mathf.Min(1f, t * 400f);
            }
            Normalize(d, 0.95f);
            return FromSamples("meme boum", d);
        }

        /// <summary>La fanfare "ta-da" : deux notes de cuivre, la seconde tenue.</summary>
        static AudioClip MemeFanfare()
        {
            float[] d = Buffer(1.5f);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                bool second = t > 0.18f;
                float f = second ? 784f : 523f;
                phase += 2f * Mathf.PI * f / Rate;
                float local = second ? t - 0.18f : t;
                float env = second ? Mathf.Min(1f, local * 30f) * Mathf.Exp(-1.2f * local) : Mathf.Min(1f, local * 30f) * (1f - local / 0.18f);
                d[i] = (Saw(phase) * 0.6f + Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 1.5f) * 0.3f) * env;
            }
            Normalize(d, 0.85f);
            return FromSamples("meme fanfare", d);
        }

        /// <summary>Le suspense "dun dun DUUUN" : trois notes graves, la derniere longue.</summary>
        static AudioClip MemeSuspense()
        {
            float[] d = Buffer(2.2f);
            float[] starts = { 0f, 0.35f, 0.7f };
            float[] notes = { 147f, 139f, 110f };
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                int n = t >= 0.7f ? 2 : t >= 0.35f ? 1 : 0;
                float local = t - starts[n];
                phase += 2f * Mathf.PI * notes[n] / Rate;
                float env = n < 2 ? Mathf.Min(1f, local * 40f) * Mathf.Exp(-6f * local) : Mathf.Min(1f, local * 40f) * Mathf.Exp(-1.1f * local);
                d[i] = (Saw(phase) * 0.7f + Mathf.Sin(phase * 0.5f) * 0.6f) * env;
            }
            Normalize(d, 0.9f);
            return FromSamples("meme suspense", d);
        }

        /// <summary>Le couic : un petit jouet qu'on ecrase.</summary>
        static AudioClip MemeCouic()
        {
            float[] d = Buffer(0.3f);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 1300f + 900f * Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI);
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(phase + 2f * Mathf.Sin(phase * 0.5f)) * Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI);
            }
            Normalize(d, 0.75f);
            return FromSamples("meme couic", d);
        }

        /// <summary>Le buzzer de la mauvaise reponse : deux notes basses et grincantes.</summary>
        static AudioClip MemeFaux()
        {
            float[] d = Buffer(0.8f);
            float a = 0f, b = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                bool second = t > 0.32f;
                float f = second ? 92f : 110f;
                a += 2f * Mathf.PI * f / Rate;
                b += 2f * Mathf.PI * f * 1.06f / Rate;
                float local = second ? t - 0.32f : t;
                float env = Mathf.Min(1f, local * 60f) * (second ? 1f - local / 0.48f : 1f - local / 0.32f);
                d[i] = Mathf.Clamp((Saw(a) + Saw(b)) * 0.8f, -1f, 1f) * env;
            }
            Normalize(d, 0.8f);
            return FromSamples("meme faux", d);
        }

        /// <summary>Le sifflet a coulisse qui tombe : "fiiiiouuu".</summary>
        static AudioClip MemeSifflet()
        {
            float[] d = Buffer(0.9f);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float f = Mathf.Lerp(1800f, 300f, Mathf.Pow(t / 0.9f, 0.8f));
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(phase) * Mathf.Min(1f, t * 20f) * (1f - t / 0.9f);
            }
            Normalize(d, 0.7f);
            return FromSamples("meme sifflet", d);
        }

        /// <summary>Le tiroir-caisse : un "kling" de cloche et une piece qui tinte.</summary>
        static AudioClip MemeCaisse()
        {
            float[] d = Buffer(0.9f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float bell = Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3136f * t) * 0.4f;
                float coin = t > 0.15f ? Mathf.Sin(2f * Mathf.PI * 4186f * t) * Mathf.Exp(-14f * (t - 0.15f)) * 0.5f : 0f;
                d[i] = bell * Mathf.Exp(-5f * t) + coin;
            }
            Normalize(d, 0.8f);
            return FromSamples("meme caisse", d);
        }
    }
}
