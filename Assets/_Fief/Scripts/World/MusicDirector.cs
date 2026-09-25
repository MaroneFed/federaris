using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA MUSIQUE. Quatre humeurs, et le jeu passe de l'une a l'autre en fondu :
    ///
    ///   TITRE     l'ecran-titre ;
    ///   FORET     l'exploration, le calme inquietant ;
    ///   TENSION   le mage descend, un garde court, on se bat, un voleur file ;
    ///   FIN       la cloche a sonne.
    ///
    /// LES MORCEAUX DE MARTIN : mets des fichiers audio (mp3, ogg, wav) dans
    /// Assets/_Fief/Resources/Music. Leur NOM dit leur humeur :
    ///     "titre" ou "menu"                         -> TITRE
    ///     "foret", "calme", "explor", "ambiance"    -> FORET
    ///     "tension", "combat", "mage", "danger"     -> TENSION
    ///     "fin", "final", "victoire", "cloche"      -> FIN
    /// Plusieurs morceaux par humeur : ils s'enchainent au hasard.
    ///
    /// Sans fichier, la musique est FABRIQUEE ici : des nappes lentes en re mineur
    /// pour la foret, un bourdon et un battement de coeur pour la tension. C'est
    /// sobre, mais le jeu n'est jamais muet.
    ///
    /// Concept Unity : Resources.LoadAll charge tout ce qu'il y a dans un dossier
    /// nomme "Resources" -- n'importe ou dans Assets. C'est la seule facon de
    /// charger un fichier PAR SON NOM depuis le code, sans le glisser dans un champ
    /// de l'Inspector.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        enum Mood { Title, Forest, Tension, End }

        readonly Dictionary<Mood, List<AudioClip>> tracks = new Dictionary<Mood, List<AudioClip>>();
        AudioSource a, b;
        AudioSource live;
        Mood mood = Mood.Title;
        Mood playing = Mood.End;
        float volume = 0.4f;
        float calmTimer;
        System.Random rng = new System.Random(5);

        public static MusicDirector Build()
        {
            GameObject go = new GameObject("MUSIQUE");
            MusicDirector m = go.AddComponent<MusicDirector>();
            m.a = go.AddComponent<AudioSource>();
            m.b = go.AddComponent<AudioSource>();
            foreach (AudioSource s in new[] { m.a, m.b })
            {
                s.spatialBlend = 0f;
                s.playOnAwake = false;
                s.volume = 0f;
                s.ignoreListenerPause = true;
            }
            m.live = m.a;
            m.LoadTracks();
            return m;
        }

        void LoadTracks()
        {
            foreach (Mood md in new[] { Mood.Title, Mood.Forest, Mood.Tension, Mood.End }) tracks[md] = new List<AudioClip>();
            AudioClip[] found = Resources.LoadAll<AudioClip>("Music");
            for (int i = 0; i < found.Length; i++)
            {
                string n = found[i].name.ToLowerInvariant();
                if (Has(n, "titre", "menu", "title")) tracks[Mood.Title].Add(found[i]);
                else if (Has(n, "tension", "combat", "mage", "danger", "chase")) tracks[Mood.Tension].Add(found[i]);
                else if (Has(n, "fin", "final", "victoire", "cloche", "end")) tracks[Mood.End].Add(found[i]);
                else tracks[Mood.Forest].Add(found[i]);
            }
            if (found.Length > 0) Debug.Log("[FIEF] Musique : " + found.Length + " morceau(x) trouve(s) dans Resources/Music.");

            // Ce qui manque, on le fabrique.
            if (tracks[Mood.Forest].Count == 0) tracks[Mood.Forest].Add(MusicSynth.Forest());
            if (tracks[Mood.Title].Count == 0) tracks[Mood.Title].AddRange(tracks[Mood.Forest]);
            if (tracks[Mood.Tension].Count == 0) tracks[Mood.Tension].Add(MusicSynth.Tension());
            if (tracks[Mood.End].Count == 0) tracks[Mood.End].AddRange(tracks[Mood.Forest]);
        }

        static bool Has(string name, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++) if (name.Contains(keys[i])) return true;
            return false;
        }

        void Update()
        {
            mood = Decide();
            float dt = Time.unscaledDeltaTime;

            if (mood != playing || !live.isPlaying && live.volume > 0.01f || live.clip != null && !live.loop && live.time > live.clip.length - 2f)
            {
                // Changer d'humeur : le morceau en cours s'efface, le nouveau monte.
                playing = mood;
                AudioSource next = live == a ? b : a;
                List<AudioClip> list = tracks[mood];
                next.clip = list[rng.Next(list.Count)];
                next.loop = list.Count == 1;
                next.volume = 0f;
                next.Play();
                live = next;
            }

            float wanted = Sfx.Muted ? 0f : volume * (mood == Mood.Tension ? 1.1f : 1f);
            live.volume = Mathf.MoveTowards(live.volume, wanted, dt * 0.25f);
            AudioSource other = live == a ? b : a;
            other.volume = Mathf.MoveTowards(other.volume, 0f, dt * 0.25f);
            if (other.volume <= 0f && other.isPlaying) other.Stop();
        }

        /// <summary>Quelle humeur maintenant ? La tension l'emporte, puis retombe lentement.</summary>
        Mood Decide()
        {
            Menus menus = Game.Menus;
            if (menus != null && menus.Current == Menus.State.Ended) return Mood.End;
            if (menus != null && menus.Current != Menus.State.Playing && menus.Current != Menus.State.Paused) return Mood.Title;

            bool tense = false;
            for (int i = 0; i < Guard.All.Count && !tense; i++) if (Guard.All[i] != null && Guard.All[i].Chasing) tense = true;
            Seeker me = Game.Me;
            if (me != null && Time.time - me.LastHurt < 6f) tense = true;
            // Quelqu'un porte la Couronne : tout le monde court.
            if (Crown.Holder != null) tense = true;
            // Une bete te chasse.
            for (int i = 0; i < Beast.All.Count && !tense; i++) if (Beast.All[i] != null && Beast.All[i].Hunting(me)) tense = true;
            if (Game.Season != null && Game.Season.Running && Game.Season.Remaining < 120f) tense = true;

            if (tense) calmTimer = 12f;
            else calmTimer -= Time.unscaledDeltaTime;
            return calmTimer > 0f ? Mood.Tension : Mood.Forest;
        }
    }

    /// <summary>
    /// La musique fabriquee, quand Martin n'a pas encore donne ses morceaux.
    /// Des accords tenus par des voix douces (sinusoides desaccordees : c'est ce
    /// desaccord qui fait "choeur"), qui entrent et sortent lentement.
    /// </summary>
    public static class MusicSynth
    {
        const int Rate = 22050;          // la moitie du CD : largement assez pour des nappes

        /// <summary>La foret : re mineur, si bemol, fa, do -- quatre accords de 6 s.</summary>
        public static AudioClip Forest()
        {
            float[][] chords =
            {
                new[] { 146.83f, 174.61f, 220.00f, 293.66f },   // re mineur
                new[] { 116.54f, 146.83f, 174.61f, 233.08f },   // si bemol
                new[] { 130.81f, 174.61f, 220.00f, 261.63f },   // fa (renversement)
                new[] { 130.81f, 164.81f, 196.00f, 261.63f }    // do
            };
            return Pads("forêt (fabriquée)", chords, 6f, 0.9f);
        }

        /// <summary>La tension : un bourdon grave, une seconde mineure aigue, un coeur qui bat.</summary>
        public static AudioClip Tension()
        {
            const float length = 8f;
            int count = Mathf.RoundToInt(Rate * length);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float drone = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f;
                float high = (Mathf.Sin(2f * Mathf.PI * 587.33f * t) + Mathf.Sin(2f * Mathf.PI * 622.25f * t)) * 0.05f
                             * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 4f));
                // Le coeur : deux coups (lub-dub) par seconde et quart.
                float beat = Mathf.Repeat(t, 1.25f);
                float heart = Mathf.Sin(2f * Mathf.PI * 52f * beat) * Mathf.Exp(-18f * beat)
                            + Mathf.Sin(2f * Mathf.PI * 48f * Mathf.Max(0f, beat - 0.22f)) * Mathf.Exp(-18f * Mathf.Max(0f, beat - 0.22f)) * 0.7f
                              * (beat > 0.22f ? 1f : 0f);
                data[i] = drone * 0.5f + high + heart * 0.9f;
            }
            return Finish("tension (fabriquée)", data);
        }

        static AudioClip Pads(string name, float[][] chords, float chordSeconds, float level)
        {
            float length = chords.Length * chordSeconds;
            int count = Mathf.RoundToInt(Rate * length);
            float[] data = new float[count];
            for (int c = 0; c < chords.Length; c++)
            {
                // Chaque accord deborde un peu sur le suivant : pas de trou entre eux.
                int from = Mathf.RoundToInt(Rate * c * chordSeconds);
                int len = Mathf.RoundToInt(Rate * chordSeconds * 1.35f);
                for (int i = 0; i < len; i++)
                {
                    int at = (from + i) % count;
                    float t = (float)i / Rate;
                    float u = (float)i / len;
                    float env = Mathf.Sin(u * Mathf.PI);
                    env *= env;
                    float v = 0f;
                    for (int n = 0; n < chords[c].Length; n++)
                    {
                        float f = chords[c][n];
                        v += Mathf.Sin(2f * Mathf.PI * f * t) + Mathf.Sin(2f * Mathf.PI * f * 1.004f * t) * 0.7f
                           + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.12f;
                    }
                    data[at] += v * env * level / chords[c].Length;
                }
            }
            return Finish(name, data);
        }

        static AudioClip Finish(string name, float[] data)
        {
            float peak = 0.0001f;
            for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            for (int i = 0; i < data.Length; i++) data[i] *= 0.7f / peak;
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
