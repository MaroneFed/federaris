using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA MUSIQUE. Cinq humeurs, et le jeu passe de l'une a l'autre en fondu :
    ///
    ///   TITRE     l'ecran-titre et les menus ;
    ///   CALME     la montee, rien ne presse ;
    ///   TENSION   quelqu'un porte la Couronne, une gargouille te vise, on se bat, la fin approche ;
    ///   FIN       le match est fini (le podium) ;
    ///   DANSE     (01/10) une manche est gagnee : le vainqueur DANSE dessus.
    ///
    /// LES MORCEAUX DE MARTIN : mets des fichiers audio (mp3, ogg, wav) dans
    /// Assets/_Fief/Resources/Music. Leur NOM dit leur humeur :
    ///     "titre" ou "menu"                         -> TITRE
    ///     (tout autre nom : "calme", "ambiance"...)  -> CALME
    ///     "tension", "combat", "mage", "danger"     -> TENSION
    ///     "danse", "dance", "fete", "victoire"      -> DANSE
    ///     "fin", "final", "podium"                  -> FIN
    /// Plusieurs morceaux par humeur : ils s'enchainent au hasard.
    ///
    /// LA DANSE SUIT LE RYTHME : le vainqueur fait un pas par temps. Mets le tempo du
    /// morceau dans son nom ("danse-128.mp3" : 128 temps par minute) ; sans chiffre, on
    /// compte 120. Et que le morceau commence sur un temps (pas de silence au debut).
    ///
    /// Sans fichier, la musique est FABRIQUEE ici : des nappes lentes en re mineur
    /// pour le calme, un bourdon et un battement de coeur pour la tension, un morceau
    /// dansant pour la fete du vainqueur. C'est sobre, mais le jeu n'est jamais muet.
    ///
    /// Concept Unity : Resources.LoadAll charge tout ce qu'il y a dans un dossier
    /// nomme "Resources" -- n'importe ou dans Assets. C'est la seule facon de
    /// charger un fichier PAR SON NOM depuis le code, sans le glisser dans un champ
    /// de l'Inspector.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        enum Mood { Title, Calm, Tension, End, Dance }

        readonly Dictionary<Mood, List<AudioClip>> tracks = new Dictionary<Mood, List<AudioClip>>();
        readonly Dictionary<AudioClip, float> tempo = new Dictionary<AudioClip, float>();
        AudioSource a, b;
        AudioSource live;
        Mood mood = Mood.Title;
        Mood playing = Mood.End;
        float volume = 0.6f;
        readonly Dictionary<AudioClip, float> resume = new Dictionary<AudioClip, float>();
        float danceWait;
        readonly Dictionary<Mood, AudioClip> lastOf = new Dictionary<Mood, AudioClip>();
        float calmTimer;
        System.Random rng = new System.Random(5);

        static MusicDirector current;
        int danceLoops;
        float danceLast;

        /// <summary>
        /// Le temps de la musique de danse (en temps, depuis son debut) ; -1 si elle ne joue
        /// pas. Le vainqueur (CharacterRig) cale ses pas dessus.
        /// </summary>
        public static float DanceBeat
        {
            get
            {
                if (current == null || current.playing != Mood.Dance || current.live == null || current.live.clip == null || !current.live.isPlaying) return -1f;
                float bpm;
                if (!current.tempo.TryGetValue(current.live.clip, out bpm)) bpm = 120f;
                // Les tours de boucle comptent : sinon la danse repartait a sa premiere figure
                // a chaque tour du morceau (16 s), et on ne voyait jamais les dernieres.
                return (current.danceLoops * current.live.clip.length + current.live.time) * bpm / 60f;
            }
        }

        public static MusicDirector Build()
        {
            // (02/10 -- les vraies musiques de Martin) : UNE musique pour tout le match. Avant,
            // chaque manche recharge la scene et recreait le chef d'orchestre : le morceau
            // repartait du debut a chaque manche. Maintenant il survit au rechargement
            // (DontDestroyOnLoad) et la musique continue.
            if (current != null) return current;
            GameObject go = new GameObject("MUSIQUE");
            Object.DontDestroyOnLoad(go);
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
            current = m;
            return m;
        }

        void OnDestroy() { if (current == this) current = null; }

        void LoadTracks()
        {
            foreach (Mood md in new[] { Mood.Title, Mood.Calm, Mood.Tension, Mood.End, Mood.Dance }) tracks[md] = new List<AudioClip>();
            AudioClip[] found = Resources.LoadAll<AudioClip>("Music");
            for (int i = 0; i < found.Length; i++)
            {
                string n = found[i].name.ToLowerInvariant();
                if (Has(n, "titre", "menu", "title")) tracks[Mood.Title].Add(found[i]);
                else if (Has(n, "tension", "combat", "mage", "danger", "chase")) tracks[Mood.Tension].Add(found[i]);
                else if (Has(n, "danse", "dance", "fete", "fête", "party", "victoire", "victory"))
                {
                    tracks[Mood.Dance].Add(found[i]);
                    tempo[found[i]] = TempoIn(n);
                }
                else if (Has(n, "fin", "final", "podium", "cloche", "end")) tracks[Mood.End].Add(found[i]);
                else tracks[Mood.Calm].Add(found[i]);
            }
            if (found.Length > 0) Debug.Log("[FIEF] Musique : " + found.Length + " morceau(x) trouve(s) dans Resources/Music.");

            // Ce qui manque, on le fabrique.
            if (tracks[Mood.Calm].Count == 0) tracks[Mood.Calm].Add(MusicSynth.Calm());
            if (tracks[Mood.Title].Count == 0) tracks[Mood.Title].AddRange(tracks[Mood.Calm]);
            if (tracks[Mood.Tension].Count == 0) tracks[Mood.Tension].Add(MusicSynth.Tension());
            if (tracks[Mood.End].Count == 0) tracks[Mood.End].AddRange(tracks[Mood.Calm]);
            if (tracks[Mood.Dance].Count == 0)
            {
                AudioClip d = MusicSynth.Dance();
                tracks[Mood.Dance].Add(d);
                tempo[d] = MusicSynth.DanceBpm;
            }
        }

        /// <summary>Le tempo ecrit dans le nom ("danse-128" : 128) ; 120 sinon.</summary>
        static float TempoIn(string name)
        {
            int value = 0, digits = 0, best = 0;
            for (int i = 0; i <= name.Length; i++)
            {
                if (i < name.Length && char.IsDigit(name[i])) { value = value * 10 + (name[i] - '0'); digits++; continue; }
                if (digits >= 2 && digits <= 3 && value >= 60 && value <= 200) best = value;
                value = 0;
                digits = 0;
            }
            return best > 0 ? best : 120f;
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

            // La danse attend son blanc (voir plus bas) avant de partir.
            if (danceWait > 0f)
            {
                danceWait -= dt;
                if (danceWait <= 0f && playing == Mood.Dance && live.clip != null) live.Play();
            }
            if (mood != playing || danceWait <= 0f && !live.isPlaying && live.volume > 0.01f || live.clip != null && !live.loop && live.time > live.clip.length - 2f)
            {
                // Changer d'humeur : le morceau en cours s'efface, le nouveau monte.
                playing = mood;
                AudioSource next = live == a ? b : a;
                List<AudioClip> list = tracks[mood];
                // On retient ou en etait le morceau qu'on quitte : le calme et la tension
                // REPRENNENT la ou ils s'etaient arretes (sinon, a chaque bascule, on reentendait
                // les memes huit premieres secondes).
                if (live.clip != null && live.isPlaying) resume[live.clip] = live.time;
                AudioClip pick = list[rng.Next(list.Count)];
                AudioClip before;
                float at = 0f;
                bool resumable = mood == Mood.Calm || mood == Mood.Tension || mood == Mood.Title;
                if (resumable && lastOf.TryGetValue(mood, out before) && before != null && resume.TryGetValue(before, out at) && at < before.length - 15f)
                    pick = before;
                else
                {
                    at = 0f;
                    // Un autre morceau que le dernier, s'il y en a plusieurs.
                    if (list.Count > 1 && lastOf.TryGetValue(mood, out before) && pick == before) pick = list[(list.IndexOf(pick) + 1) % list.Count];
                }
                lastOf[mood] = pick;
                next.clip = pick;
                next.loop = list.Count == 1 || mood == Mood.Dance;
                next.time = at;
                // La danse part d'un coup, sur le temps : pas de lent fondu.
                next.volume = mood == Mood.Dance ? volume : 0f;
                // (03/10 -- "la musique, elle continue") La musique de la manche se TAIT d'un coup
                // (un quart de seconde), un blanc, la cloche et la voix -- puis la danse part.
                if (mood == Mood.Dance) danceWait = 0.55f;
                else { danceWait = 0f; next.Play(); }
                live = next;
                danceLoops = 0;
                danceLast = 0f;
            }
            // Un tour de boucle de plus (le temps du morceau est revenu en arriere).
            if (playing == Mood.Dance && live.isPlaying)
            {
                if (live.time < danceLast - 0.5f) danceLoops++;
                danceLast = live.time;
            }

            float wanted = Sfx.Muted ? 0f : volume * Settings.Music * (mood == Mood.Tension ? 1.1f : mood == Mood.Dance ? 1.2f : 1f);
            float fade = mood == Mood.Dance ? 3f : 0.25f;
            live.volume = Mathf.MoveTowards(live.volume, wanted, dt * fade);
            AudioSource other = live == a ? b : a;
            other.volume = Mathf.MoveTowards(other.volume, 0f, dt * (mood == Mood.Dance ? 5f : fade));
            if (other.volume <= 0f && other.isPlaying) other.Stop();
        }

        /// <summary>Quelle humeur maintenant ? La tension l'emporte, puis retombe lentement.</summary>
        Mood Decide()
        {
            Menus menus = Game.Menus;
            if (menus != null && menus.Dancing) return Mood.Dance;
            if (menus != null && menus.Current == Menus.State.Ended) return Mood.End;
            if (menus != null && menus.Current != Menus.State.Playing && menus.Current != Menus.State.Paused) return Mood.Title;

            // (02/10) La tension quand ca compte VRAIMENT : quelqu'un porte la Couronne, un
            // sacre commence, les deux dernieres minutes. Avant, le moindre coup ou une
            // gargouille qui te visait faisait basculer la musique pour douze secondes -- avec de
            // vrais morceaux, ca hachait tout.
            bool tense = false;
            if (Crown.Holder != null) tense = true;
            if (Monument.Sacring != null) tense = true;
            if (Game.Season != null && Game.Season.Running && Game.Season.Remaining < 120f) tense = true;

            if (tense) calmTimer = 20f;
            else calmTimer -= Time.unscaledDeltaTime;
            return calmTimer > 0f ? Mood.Tension : Mood.Calm;
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

        /// <summary>Le calme : re mineur, si bemol, fa, do -- quatre accords de 6 s.</summary>
        public static AudioClip Calm()
        {
            float[][] chords =
            {
                new[] { 146.83f, 174.61f, 220.00f, 293.66f },   // re mineur
                new[] { 116.54f, 146.83f, 174.61f, 233.08f },   // si bemol
                new[] { 130.81f, 174.61f, 220.00f, 261.63f },   // fa (renversement)
                new[] { 130.81f, 164.81f, 196.00f, 261.63f }    // do
            };
            return Pads("calme (fabriquée)", chords, 6f, 0.9f);
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

        // ================================================================== la danse

        /// <summary>Le tempo de la musique de danse fabriquee.</summary>
        public const float DanceBpm = 120f;

        /// <summary>
        /// LA MUSIQUE DE LA DANSE DU VAINQUEUR (01/10), tant que Martin n'a pas donne la
        /// sienne : huit mesures a 120 temps par minute qui tournent en boucle -- une grosse
        /// caisse a chaque temps, un clap sur le 2 et le 4, un charleston entre les temps,
        /// une basse qui saute d'octave, des accords piques (do, sol, la mineur, fa : la
        /// suite de mille tubes) et une petite melodie de cloche par-dessus.
        ///
        /// Comment on fabrique un son : un tableau de nombres entre -1 et 1 (22 050 par
        /// seconde). Une note, c'est une sinusoide qui s'eteint ; une grosse caisse, une
        /// sinusoide grave dont la hauteur tombe ; un clap, du bruit (des nombres au hasard)
        /// qui s'eteint tres vite.
        /// </summary>
        public static AudioClip Dance()
        {
            const int Bars = 8;
            float beat = 60f / DanceBpm;
            int count = Mathf.RoundToInt(Rate * beat * 4f * Bars);
            float[] d = new float[count];
            System.Random r = new System.Random(128);
            int[][] chords =
            {
                new[] { 60, 64, 67 },   // do
                new[] { 55, 59, 62 },   // sol
                new[] { 57, 60, 64 },   // la mineur
                new[] { 53, 57, 60 }    // fa
            };
            int[] roots = { 36, 43, 45, 41 };
            // La melodie, en croches (0 : silence), deux mesures par accord.
            int[] lead =
            {
                76, 0, 79, 0, 81, 79, 76, 0,    74, 0, 72, 74, 76, 0, 0, 0,
                74, 0, 79, 0, 83, 81, 79, 0,    81, 79, 76, 74, 79, 0, 0, 0,
                76, 0, 79, 0, 81, 0, 84, 83,    81, 0, 79, 76, 81, 0, 0, 0,
                77, 0, 81, 0, 84, 81, 79, 77,   76, 74, 72, 74, 79, 0, 76, 0
            };
            int eighths = Bars * 8;
            for (int e = 0; e < eighths; e++)
            {
                int at = Mathf.RoundToInt(e * beat * 0.5f * Rate);
                int chord = (e / 16) % 4;
                bool onBeat = e % 2 == 0;
                int beatInBar = (e / 2) % 4;
                if (onBeat) Kick(d, at);
                else Hat(d, at, r, 0.13f);
                if (onBeat && (beatInBar == 1 || beatInBar == 3)) Clap(d, at, r);
                // La basse : la fondamentale, puis l'octave au-dessus.
                Tone(d, at, 0.22f, Freq(roots[chord] + (onBeat ? 0 : 12)), 0.34f, 7f, 1);
                // Les accords, piques entre les temps.
                if (!onBeat)
                    for (int k = 0; k < chords[chord].Length; k++) Tone(d, at, 0.16f, Freq(chords[chord][k]), 0.1f, 15f, 2);
                if (lead[e] > 0) Tone(d, at, 0.5f, Freq(lead[e]), 0.2f, 4.5f, 3);
            }
            // Une cymbale au debut de la boucle.
            for (int i = 0; i < Rate; i++) d[i] += ((float)r.NextDouble() * 2f - 1f) * Mathf.Exp(-i / (float)Rate * 4f) * 0.12f;
            return Finish("danse (fabriquée)", d);
        }

        static float Freq(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

        static void Kick(float[] d, int at)
        {
            int len = Mathf.RoundToInt(Rate * 0.3f);
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / Rate;
                phase += 2f * Mathf.PI * (45f + 95f * Mathf.Exp(-t * 28f)) / Rate;
                d[(at + i) % d.Length] += Mathf.Sin(phase) * Mathf.Exp(-t * 9f) * 0.9f;
            }
        }

        static void Clap(float[] d, int at, System.Random r)
        {
            int len = Mathf.RoundToInt(Rate * 0.2f);
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)r.NextDouble() * 2f - 1f;
                d[(at + i) % d.Length] += noise * Mathf.Exp(-t * 20f) * 0.32f + Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 30f) * 0.18f;
            }
        }

        static void Hat(float[] d, int at, System.Random r, float level)
        {
            int len = Mathf.RoundToInt(Rate * 0.06f);
            float last = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)r.NextDouble() * 2f - 1f;
                // La difference de deux bruits successifs : il ne reste que les aigus.
                d[(at + i) % d.Length] += (noise - last) * Mathf.Exp(-t * 70f) * level;
                last = noise;
            }
        }

        /// <summary>Une note : 1 basse ronde, 2 accord pique (riche en harmoniques), 3 cloche.</summary>
        static void Tone(float[] d, int at, float seconds, float f, float amp, float decay, int kind)
        {
            int len = Mathf.RoundToInt(Rate * seconds);
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / Rate;
                float w = 2f * Mathf.PI * f * t;
                float v;
                if (kind == 1) v = (float)System.Math.Tanh(1.6f * (Mathf.Sin(w) + 0.3f * Mathf.Sin(2f * w)));
                else if (kind == 2) v = Mathf.Sin(w) + Mathf.Sin(2f * w) / 2f + Mathf.Sin(3f * w) / 3f + Mathf.Sin(4f * w) / 4f + Mathf.Sin(5f * w) / 5f;
                else v = Mathf.Sin(w) + 0.5f * Mathf.Sin(2f * w) + 0.22f * Mathf.Sin(3f * w);
                // Une attaque d'un centieme de seconde (sans elle, chaque note "claque").
                float env = Mathf.Min(1f, t * 100f) * Mathf.Exp(-t * decay);
                d[(at + i) % d.Length] += v * env * amp;
            }
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
