using System.Collections.Generic;
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
    ///
    /// LES VRAIS SONS (02/10 -- Martin a envoye les packs Kenney, licence CC0 : libres pour
    /// un jeu vendu) : ranges par role dans Assets/_Fief/Resources/Sons/ (Pop, Choc, Fracas,
    /// Cloche, Poussee, Couronne, Carte, Perdue, Voix...). S'il y a des fichiers dans le dossier, on en joue un au
    /// hasard ; sinon, le son fabrique prend le relais. Pour changer un son : remplace les
    /// fichiers du dossier (.ogg, .wav ou .mp3), le code ne bouge pas.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;

        static AudioSource source;
        static System.Random rng = new System.Random(7);

        static AudioClip[] chop;      // hache dans le bois
        static AudioClip[] pick;      // pioche dans la pierre
        static AudioClip[] clang;     // pic sur le fer
        static AudioClip hammer;      // construction
        static AudioClip deny;        // action refusee
        static AudioClip[] step;      // pas
        static AudioClip pop;         // depot / ramassage
        static AudioClip[] rustle;    // fourrager dans les branches, fourrer dans le sac
        static AudioClip[] clatter;   // des branches seches qui s'entrechoquent

        public static bool Muted;

        static readonly Dictionary<string, AudioClip[]> Real = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, AudioClip> Lines = new Dictionary<string, AudioClip>();

        /// <summary>Un vrai son du dossier Resources/Sons/"folder", au hasard. Faux s'il n'y en a pas (on fabrique alors le sien).</summary>
        static bool PlayReal(string folder, float volume)
        {
            if (Muted) return true;
            if (source == null) return false;
            AudioClip[] clips;
            if (!Real.TryGetValue(folder, out clips))
            {
                clips = Resources.LoadAll<AudioClip>("Sons/" + folder);
                Real[folder] = clips;
            }
            if (clips == null || clips.Length == 0) return false;
            AudioClip clip = clips[rng.Next(clips.Length)];
            if (spotOn) Play3D(clip, volume);
            else source.PlayOneShot(clip, volume);
            return true;
        }

        /// <summary>
        /// LA VOIX DE L'ARENE (Kenney, "voiceover pack fighter") : "3", "2", "1", "fight",
        /// "round_2", "final_round", "you_win", "winner"... (le nom du fichier dans
        /// Resources/Sons/Voix). Faux s'il n'existe pas.
        /// </summary>
        public static bool Announce(string line)
        {
            if (Muted || source == null) return false;
            AudioClip clip;
            if (!Lines.TryGetValue(line, out clip))
            {
                clip = Resources.Load<AudioClip>("Sons/Voix/" + line);
                Lines[line] = clip;
            }
            if (clip == null) return false;
            source.PlayOneShot(clip, 0.7f);
            return true;
        }

        /// <summary>Un coup de poing : quelqu'un est pousse ou frappe (un vrai "pouf", Kenney).</summary>
        public static void Punch() { if (!PlayReal("Poussee", 0.6f)) Thud(); }

        static AudioClip boom, subThump, whoop;

        /// <summary>
        /// LA POUSSEE QUI CLAQUE (02/10 -- Martin : "quand ca pousse, je veux que ca fasse un
        /// ENORME bruit") : trois couches jouees ensemble -- le coup de poing (Kenney), un
        /// GRAVE qui cogne dans la poitrine (une note de 70 Hz qui tombe a 35 Hz en un tiers
        /// de seconde) et le souffle de celui qui part. Pleine puissance quand c'est toi qui
        /// pousses ou qu'on te pousse ; ailleurs, en 3D (on l'entend de loin, plus doux).
        /// </summary>
        public static void BigPush(Vector3 at, bool mine)
        {
            if (Muted || source == null) return;
            if (subThump == null) subThump = Sweep("grave de poussee", 70f, 35f, 0.34f, 0.35f);
            if (!mine) Begin3D(at);
            PlayReal("Poussee", mine ? 1f : 0.85f);
            Play(subThump, mine ? 1f : 0.8f);
            Whoosh();
            End3D();
        }

        /// <summary>
        /// LE KO (02/10, le clipper) : quelqu'un qu'on vient de pousser tombe dans les nuages --
        /// un BOUM de canon (55 Hz qui s'effondre, une seconde entiere) et un fracas.
        /// </summary>
        public static void KoBoom(Vector3 at, bool mine)
        {
            if (Muted || source == null) return;
            if (boom == null) boom = Sweep("boum du KO", 55f, 24f, 1.1f, 0.6f);
            if (!mine) Begin3D(at);
            Play(boom, 1f);
            PlayReal("Fracas", mine ? 0.9f : 0.7f);
            End3D();
        }

        /// <summary>
        /// UN ANNEAU DE VENT (03/10) : un souffle, et un "whoup" qui MONTE -- on sent qu'on est
        /// propulse. Le tien en plein, ceux des autres la ou ils passent.
        /// </summary>
        public static void Boost(Vector3 at, bool mine)
        {
            if (Muted || source == null) return;
            if (whoop == null) whoop = Sweep("anneau de vent", 260f, 780f, 0.45f, 0.15f);
            if (!mine) Begin3D(at);
            Play(whoop, mine ? 0.55f : 0.45f);
            Whoosh();
            End3D();
        }

        // ------------------------------------------------------------- les obstacles
        //
        // (03/10, le clipper fou n° 102-106 : "tous les obstacles font le meme bruit de choc")
        // Chacun sa voix, fabriquee ici -- on les reconnait les yeux fermes :
        //   le BUTOIR (pendule)  : un "BOING" de caoutchouc, une note qui ondule en tombant ;
        //   le POING (belier, barres) : un "PAF" de gant de boxe, sourd et sec ;
        //   le MAILLET (marteau) : un "GONG" de cloche, long, aux harmoniques de metal ;
        //   la HERSE, avant de sortir : un "clac-clac" de cliquet ;
        //   l'ANNEAU RATE : un "pfff" qui se degonfle.

        static AudioClip boing, paf, gong, rattle, deflate;

        public static void BoingAt(Vector3 at) { if (Muted || source == null) return; if (boing == null) boing = MakeBoing(); Begin3D(at); Play(boing, 0.9f); End3D(); }
        public static void PafAt(Vector3 at) { if (Muted || source == null) return; if (paf == null) paf = MakePaf(); Begin3D(at); Play(paf, 1f); PlayReal("Choc", 0.35f); End3D(); }
        public static void GongAt(Vector3 at) { if (Muted || source == null) return; if (gong == null) gong = MakeGong(); Begin3D(at); Play(gong, 0.85f); End3D(); }
        public static void RattleAt(Vector3 at) { if (Muted || source == null) return; if (rattle == null) rattle = MakeRattle(); Begin3D(at); Play(rattle, 0.6f); End3D(); }
        public static void Deflate() { if (Muted || source == null) return; if (deflate == null) deflate = Sweep("anneau rate", 520f, 150f, 0.45f, 0.5f); Play(deflate, 0.45f); }

        static AudioClip crowd;

        /// <summary>
        /// LA FOULE (03/10, le clipper fou n° 499 : "pas de cri de foule") : un public invisible
        /// qui fait "OOOOH" sur les gros moments (un KO, une Couronne volee en plein ciel, un sacre
        /// arrache) et qui EXULTE a la victoire. "loud" : 0-1. Fabriquee ici (du souffle filtre
        /// et un chœur de voyelles "o" qui ondulent) ; un vrai son dans Resources/Sons/Foule la
        /// remplace.
        /// </summary>
        public static void Crowd(float loud)
        {
            if (Muted || source == null) return;
            if (PlayReal("Foule", 0.55f * loud)) return;
            if (crowd == null) crowd = MakeCrowd();
            Play(crowd, 0.5f * loud);
        }

        static AudioClip MakeCrowd()
        {
            float seconds = 2.6f;
            int count = Mathf.RoundToInt(Rate * seconds);
            float[] d = new float[count];
            System.Random r = new System.Random(77);
            float a1 = 0f, a2 = 0f;
            // Un choeur : douze voix, chacune sa hauteur (autour de 330 Hz) et son vibrato.
            float[] pitch = new float[12];
            float[] rate = new float[12];
            float[] ph = new float[12];
            for (int v = 0; v < pitch.Length; v++) { pitch[v] = 250f + (float)r.NextDouble() * 180f; rate[v] = 4f + (float)r.NextDouble() * 3f; }
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                // Ca monte vite, ca tient, ca retombe.
                float env = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((seconds - t) / 1.4f);
                // Le brouhaha : un souffle filtre (entre ~300 et ~2000 Hz).
                float n = (float)r.NextDouble() * 2f - 1f;
                a1 += (n - a1) * 0.25f;
                a2 += (a1 - a2) * 0.05f;
                float murmur = (a1 - a2) * 1.4f;
                // Les "ooo" : la hauteur monte un peu avec l'excitation.
                float choir = 0f;
                for (int v = 0; v < pitch.Length; v++)
                {
                    float f = pitch[v] * (1f + 0.1f * Mathf.Clamp01(t / 0.8f)) * (1f + 0.012f * Mathf.Sin(2f * Mathf.PI * rate[v] * t));
                    ph[v] += 2f * Mathf.PI * f / Rate;
                    choir += Mathf.Sin(ph[v]) + 0.35f * Mathf.Sin(ph[v] * 2f);
                }
                d[i] = (murmur * 0.8f + choir * 0.05f) * env;
            }
            Normalize(d, 0.85f);
            return FromSamples("foule", d);
        }

        static AudioClip MakeBoing()
        {
            float seconds = 0.6f;
            int count = Mathf.RoundToInt(Rate * seconds);
            float[] d = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                // Une note qui tombe (240 -> 120 Hz) en ondulant vite : le caoutchouc qui vibre.
                float f = Mathf.Lerp(240f, 120f, t / seconds) * (1f + 0.18f * Mathf.Sin(t * 2f * Mathf.PI * 11f) * Mathf.Exp(-4f * t));
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * Mathf.Exp(-5f * t) * Mathf.Min(1f, t * 300f);
            }
            Normalize(d, 0.95f);
            return FromSamples("boing du butoir", d);
        }

        static AudioClip MakePaf()
        {
            float seconds = 0.3f;
            int count = Mathf.RoundToInt(Rate * seconds);
            float[] d = new float[count];
            System.Random r = new System.Random(5);
            float phase = 0f;
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float f = Mathf.Lerp(110f, 55f, t / seconds);
                phase += 2f * Mathf.PI * f / Rate;
                // Un bruit etouffe (filtre simple) + un grave qui cogne.
                float n = (float)r.NextDouble() * 2f - 1f;
                low += (n - low) * 0.18f;
                d[i] = (Mathf.Sin(phase) * 0.9f * Mathf.Exp(-14f * t) + low * 1.6f * Mathf.Exp(-40f * t)) * Mathf.Min(1f, t * 800f);
            }
            Normalize(d, 0.95f);
            return FromSamples("paf du poing", d);
        }

        static AudioClip MakeGong()
        {
            float seconds = 1.6f;
            int count = Mathf.RoundToInt(Rate * seconds);
            float[] d = new float[count];
            // Des harmoniques "fausses" (pas des multiples entiers) : c'est ce qui fait metal.
            float[] ratio = { 1f, 2.02f, 2.74f, 3.93f, 5.4f };
            float[] level = { 1f, 0.6f, 0.45f, 0.3f, 0.18f };
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float v = 0f;
                for (int k = 0; k < ratio.Length; k++) v += Mathf.Sin(2f * Mathf.PI * 150f * ratio[k] * t) * level[k] * Mathf.Exp(-(1.6f + k * 1.1f) * t);
                d[i] = v * Mathf.Min(1f, t * 500f);
            }
            Normalize(d, 0.9f);
            return FromSamples("gong du maillet", d);
        }

        static AudioClip MakeRattle()
        {
            float seconds = 0.32f;
            int count = Mathf.RoundToInt(Rate * seconds);
            float[] d = new float[count];
            System.Random r = new System.Random(9);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float v = 0f;
                // Trois clics de cliquet, de plus en plus proches.
                float[] at = { 0f, 0.12f, 0.2f };
                for (int k = 0; k < at.Length; k++)
                {
                    float u = t - at[k];
                    if (u < 0f) continue;
                    v += (((float)r.NextDouble() * 2f - 1f) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2400f * u)) * Mathf.Exp(-90f * u);
                }
                d[i] = v;
            }
            Normalize(d, 0.8f);
            return FromSamples("cliquet de la herse", d);
        }

        /// <summary>UN MOMENT A CLIPPER : le coup de cymbale d'orchestre (Kenney, un "hit" qui monte).</summary>
        public static void Moment() { if (!PlayReal("Moment", 0.85f)) Discovery(); }

        /// <summary>
        /// Une note grave qui glisse de "from" a "to" Hz en "seconds", avec un claquement de bruit
        /// au debut ("noise" : sa part) : la base des coups sourds.
        /// </summary>
        static AudioClip Sweep(string name, float from, float to, float seconds, float noise)
        {
            int count = Mathf.RoundToInt(Rate * seconds);
            float[] data = new float[count];
            System.Random r = new System.Random(31);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float k = t / seconds;
                float f = Mathf.Lerp(from, to, 1f - Mathf.Exp(-5f * k));
                phase += 2f * Mathf.PI * f / Rate;
                float body = Mathf.Sin(phase) * Mathf.Exp(-3.2f * k) * Mathf.Min(1f, t * 400f);
                float crack = ((float)r.NextDouble() * 2f - 1f) * Mathf.Exp(-60f * t) * noise;
                data[i] = body + crack;
            }
            Normalize(data, 0.95f);
            return FromSamples(name, data);
        }

        /// <summary>
        /// UNE CARTE CHOISIE, UN DON PRIS (02/10 -- "quand tu choisis une carte, on dirait que
        /// c'est nul") : un petit jingle qui MONTE (Kenney PIZZI04 : +9 demi-tons). L'ancien
        /// (PIZZI07) descendait -- l'oreille entend "rate".
        /// </summary>
        public static void CardPick() { if (!PlayReal("Carte", 0.7f)) Pop(); }

        /// <summary>Tu viens de perdre la Couronne : un jingle qui DESCEND (PIZZI05).</summary>
        public static void Lost() { if (!PlayReal("Perdue", 0.7f)) Deny(); }

        /// <summary>La visee passe sur un bouton de menu.</summary>
        public static void Hover() { PlayReal("Survol", 0.25f); }

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
            step = new AudioClip[4];

            for (int i = 0; i < 3; i++)
            {
                chop[i] = Impact("chop" + i, 165f + i * 14f, 0.16f, 0.55f, 26f);
                pick[i] = Impact("pick" + i, 380f + i * 30f, 0.12f, 0.78f, 40f);
                clang[i] = Metal("clang" + i, 620f + i * 45f, 0.30f);
            }
            for (int i = 0; i < 4; i++)
            {
                step[i] = Impact("step" + i, 92f + i * 9f, 0.085f, 0.85f, 52f);
            }

            rustle = new AudioClip[3];
            for (int i = 0; i < 3; i++) rustle[i] = Rustle("fourrage" + i, 0.28f + i * 0.08f, 11 + i);

            hammer = Impact("hammer", 120f, 0.34f, 0.5f, 11f);
            deny = Buzz("deny", 128f, 0.22f);
            pop = Impact("pop", 520f, 0.09f, 0.25f, 46f);
            clatter = new AudioClip[2];
            for (int i = 0; i < 2; i++) clatter[i] = Clatter("branchages" + i, 5 + i);
        }

        // ------------------------------------------------------------- lecture

        /// <summary>Des branches froissees : on grimpe, on se faufile.</summary>
        public static void Rustle() { if (!PlayReal("Tissu", 0.6f)) Play(Pick(rustle), 0.55f); }
        /// <summary>Le fer contre le fer : une lame qui touche une armure.</summary>
        public static void Clang() { if (!PlayReal("Metal", 0.5f)) Play(Pick(clang), 0.45f); }
        /// <summary>Une pierre qu'on frappe.</summary>
        public static void Chip() { if (!PlayReal("Pierre", 0.5f)) Play(Pick(pick), 0.4f); }
        /// <summary>Un tronc qui s'abat.</summary>
        public static void Crash() { if (PlayReal("Fracas", 0.8f)) return; Play(Pick(clatter), 0.9f); Play(Pick(pick), 0.65f); }

        static AudioClip beep;
        static AudioSource beeper;
        /// <summary>
        /// LE BIP DU DETECTEUR : une note claire et breve. Plus aigu quand on s'approche
        /// ("pitch" de 1 a 2) -- c'est le rythme qui dit la distance, la hauteur la confirme.
        /// </summary>
        public static void Beep(float pitch)
        {
            if (Muted || source == null) return;
            if (beep == null)
            {
                int count = Mathf.RoundToInt(Rate * 0.07f);
                float[] data = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-40f * t);
                    data[i] = env * (Mathf.Sin(2f * Mathf.PI * 1320f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2640f * t)) * 0.35f;
                }
                beep = FromSamples("bip", data);
            }
            // Sa propre source : changer la hauteur de la source commune changerait
            // aussi les sons en cours.
            if (beeper == null)
            {
                beeper = source.gameObject.AddComponent<AudioSource>();
                beeper.playOnAwake = false;
                beeper.spatialBlend = 0f;
            }
            beeper.pitch = Mathf.Clamp(pitch, 0.5f, 2.5f);
            beeper.PlayOneShot(beep, 0.5f);
        }

        public static void Build() { if (!PlayReal("Treuil", 0.8f)) Play(hammer, 0.85f); }
        public static void Deny() { if (!PlayReal("Refus", 0.5f)) Play(deny, 0.45f); }
        public static void Pop() { if (!PlayReal("Pop", 0.55f)) Play(pop, 0.5f); }
        public static void Step() { if (!PlayReal("Pas", 0.3f)) Play(Pick(step), 0.22f); }

        // ================================================================== outils et grands sons
        //
        // (02/10) Les sons de la foret (vent, hibou, loup, corbeau, pluie, tonnerre),
        // du mage, de la forge, de la stele et de la malediction ont ete retires :
        // plus rien ne les jouait depuis le 27/09.

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
            if (PlayReal("Couronne", 0.8f)) return;
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
            if (PlayReal("Cloche", 0.6f)) return;
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
            if (spotOn) { Play3D(clip, volume); return; }
            source.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------- les sons EN 3D
        //
        // (02/10 -- Martin : "fais vraiment gaffe au bruitage") : les bruits du monde (un
        // pendule, une gargouille, une arbaleste, un bot qu'on pousse) etaient joues A PLAT,
        // dans tes oreilles, qu'ils soient a 2 m ou a 150 m : un vacarme permanent. Ils
        // partent maintenant de L'ENDROIT ou ils arrivent : plus fort a gauche s'il est a
        // gauche, et de plus en plus doux avec la distance (plein jusqu'a 6 m, moitie a
        // 12 m, presque rien a 100 m).
        //
        // Concept Unity : une AudioSource avec spatialBlend = 1 est "dans le monde" ; c'est
        // l'AudioListener (sur la camera, a tes yeux) qui l'entend, avec la distance et le
        // cote. On en garde seize, qu'on deplace a tour de role (un "pool").

        static AudioSource[] pool;
        static int poolNext;
        static bool spotOn;
        static Vector3 spot;

        static void Play3D(AudioClip clip, float volume) { Play3D(clip, volume, 1f); }

        static void Play3D(AudioClip clip, float volume, float pitch)
        {
            if (pool == null)
            {
                pool = new AudioSource[16];
                for (int i = 0; i < pool.Length; i++)
                {
                    GameObject go = new GameObject("Son 3D");
                    go.transform.SetParent(source.transform, false);
                    AudioSource a = go.AddComponent<AudioSource>();
                    a.playOnAwake = false;
                    a.spatialBlend = 1f;
                    a.rolloffMode = AudioRolloffMode.Logarithmic;
                    a.minDistance = 6f;
                    a.maxDistance = 140f;
                    a.dopplerLevel = 0f;
                    a.spread = 40f;
                    pool[i] = a;
                }
            }
            AudioSource p = pool[poolNext];
            poolNext = (poolNext + 1) % pool.Length;
            if (p == null) return;
            p.transform.position = spot;
            p.pitch = pitch;
            p.PlayOneShot(clip, volume);
        }

        static void Begin3D(Vector3 at) { spotOn = true; spot = at; }
        static void End3D() { spotOn = false; }

        /// <summary>Les bruits du monde, joues LA OU ILS ARRIVENT (voir plus haut).</summary>
        public static void CrashAt(Vector3 at) { Begin3D(at); Crash(); End3D(); }
        public static void ThudAt(Vector3 at) { Begin3D(at); Thud(); End3D(); }
        public static void ClangAt(Vector3 at) { Begin3D(at); Clang(); End3D(); }
        public static void SnapAt(Vector3 at) { Begin3D(at); TrapSnap(); End3D(); }
        public static void WhooshAt(Vector3 at) { Begin3D(at); Whoosh(); End3D(); }
        public static void ChipAt(Vector3 at) { Begin3D(at); Chip(); End3D(); }
        public static void PunchAt(Vector3 at) { Begin3D(at); Punch(); End3D(); }
        public static void BuildAt(Vector3 at) { Begin3D(at); Build(); End3D(); }
        public static void BellAt(Vector3 at) { Begin3D(at); Bell(); End3D(); }
        public static void LandAt(Vector3 at) { Begin3D(at); Land(); End3D(); }

        /// <summary>Toi, tu retombes : un atterrissage mat (Kenney), plus doux que le "bam" d'un coup.</summary>
        public static void Land() { if (!PlayReal("Atterrissage", 0.5f)) Thud(); }

        /// <summary>
        /// LE SACRE, une cloche par seconde qui MONTE d'un ton a chaque fois (02/10) : on
        /// entend le sacre approcher de sa fin -- do, mi, sol.
        /// </summary>
        public static void SacreTick(int second, Vector3 at)
        {
            if (Muted || source == null) return;
            AudioClip[] clips;
            if (!Real.TryGetValue("Cloche", out clips)) { clips = Resources.LoadAll<AudioClip>("Sons/Cloche"); Real["Cloche"] = clips; }
            if (clips == null || clips.Length == 0) { BellAt(at); return; }
            float[] steps = { 1f, 1.26f, 1.5f, 2f };
            spot = at;
            Play3D(clips[0], 0.9f, steps[Mathf.Clamp(second - 1, 0, steps.Length - 1)]);
        }

        // ------------------------------------------------------------- synthese

        // ------------------------------------------------------------- les voix

        static AudioClip[] voices;

        /// <summary>
        /// UNE VOIX, sans mots : un rival ou un garde qui parle (Martin, 26/09 : "le
        /// texte au-dessus, j'aime pas"). Plus de replique ecrite sur leur tete : un
        /// grognement, une exclamation, un murmure, spatialise la ou ils sont. Le ton
        /// suffit a dire "halte !" ou "c'est a moi".
        ///
        /// Fabrication : une fondamentale qui glisse (80-170 Hz) et ses harmoniques,
        /// ponderees par deux formants de voyelle (le "a", le "o", le "e") -- c'est ce
        /// que fait une gorge. Deux syllabes, une enveloppe douce.
        /// </summary>
        public static void Voice(Vector3 at, int who, bool shout)
        {
            if (Muted) return;
            if (voices == null)
            {
                voices = new AudioClip[8];
                float[] pitches = { 98f, 118f, 138f, 160f };
                for (int i = 0; i < 8; i++) voices[i] = BuildVoice("voix" + i, pitches[i % 4], i >= 4);
            }
            int k = (Mathf.Abs(who) % 4) + (shout ? 4 : 0);
            AudioSource.PlayClipAtPoint(voices[k], at + Vector3.up * 1.6f, shout ? 1f : 0.75f);
        }

        static AudioClip BuildVoice(string name, float f0, bool shout)
        {
            float length = shout ? 0.5f : 0.42f;
            int count = Mathf.RoundToInt(Rate * length);
            float[] data = new float[count];
            // Deux syllabes : chacune sa voyelle (F1, F2).
            float[,] vowels = { { 730f, 1090f }, { 450f, 800f }, { 400f, 1900f } };
            int v0 = rng.Next(3), v1 = rng.Next(3);
            double phase = 0.0;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float u = t / length;
                bool second = u > 0.48f;
                float su = second ? (u - 0.48f) / 0.52f : u / 0.48f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(su)) * (second ? 0.85f : 1f);
                env = Mathf.Pow(Mathf.Max(0f, env), 0.7f);
                // Le ton monte pour un cri, descend pour un murmure.
                float glide = shout ? 1f + 0.25f * Mathf.Sin(Mathf.PI * u) : 1.08f - 0.16f * u;
                float f = f0 * glide * (shout ? 1.35f : 1f);
                phase += 2.0 * Mathf.PI * f / Rate;
                int v = second ? v1 : v0;
                float f1 = vowels[v, 0], f2 = vowels[v, 1];
                float sample = 0f;
                for (int h = 1; h <= 24; h++)
                {
                    float fh = f * h;
                    if (fh > 4000f) break;
                    float w1 = Mathf.Exp(-Mathf.Pow((fh - f1) / 130f, 2f));
                    float w2 = 0.6f * Mathf.Exp(-Mathf.Pow((fh - f2) / 180f, 2f));
                    float amp = (0.25f / h) + w1 + w2;
                    sample += amp * (float)System.Math.Sin(phase * h);
                }
                float breath = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.08f;
                data[i] = (sample * 0.18f + breath) * env;
            }
            return FromSamples(name, data);
        }

        /// <summary>
        /// Des branches seches qu'on rassemble : cinq ou six petits coups de bois
        /// creux, a des hauteurs differentes, qui se chevauchent sur un tiers de seconde.
        /// </summary>
        static AudioClip Clatter(string name, int knocks)
        {
            int count = Mathf.RoundToInt(Rate * 0.45f);
            float[] data = new float[count];
            for (int k = 0; k < knocks; k++)
            {
                int start = Mathf.RoundToInt(Rate * (k * 0.055f + (float)rng.NextDouble() * 0.03f));
                float f = 620f + (float)rng.NextDouble() * 520f;
                float low = 180f + (float)rng.NextDouble() * 90f;
                float gain = 0.55f + (float)rng.NextDouble() * 0.45f;
                for (int i = start; i < count; i++)
                {
                    float t = (float)(i - start) / Rate;
                    float env = Mathf.Exp(-70f * t);
                    if (env < 0.001f) break;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    data[i] += gain * env * (Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * low * t) * 0.3f + noise * 0.35f);
                }
            }
            for (int i = 0; i < count; i++) data[i] *= 0.6f;
            return FromSamples(name, data);
        }

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

        /// <summary>
        /// Un bruit de fourrage : des brindilles qu'on rassemble, des feuilles
        /// seches. Du bruit filtre en bouffees irregulieres, avec de petits
        /// craquements seches seme dedans.
        /// </summary>
        static AudioClip Rustle(string name, float duration, int seed)
        {
            int count = Mathf.RoundToInt(Rate * duration);
            float[] data = new float[count];
            System.Random r = new System.Random(seed);
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float noise = (float)(r.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.35f;
                float hiss = noise - low * 0.6f;
                float puffs = 0.5f + 0.5f * Mathf.Sin(t * 40f + seed) * Mathf.Sin(t * 17f);
                float env = Mathf.Sin(t * Mathf.PI);
                float v = hiss * puffs * env * 0.5f;
                if (r.NextDouble() < 0.0025) v += (float)(r.NextDouble() - 0.5) * 1.6f * env;
                data[i] = v;
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

        static AudioClip alarm, leaf;

        /// <summary>L'ALARME : trois tintements aigus et rapides -- rien a voir avec la cloche.</summary>
        /// <summary>
        /// (05/10) UNE GARGOUILLE TE VISE : deux notes douces qui montent (un "hm-hm ?"), bas et
        /// court -- elle previent, elle ne crie pas. (Avant : l'alarme aigue a trois coups.)
        /// </summary>
        public static void Warn()
        {
            if (warn == null)
            {
                const float duration = 0.42f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                float[] notes = { 392f, 523.25f };
                for (int n = 0; n < notes.Length; n++)
                {
                    int start = Mathf.RoundToInt(Rate * n * 0.13f);
                    for (int i = start; i < count; i++)
                    {
                        float t = (float)(i - start) / Rate;
                        float env = Mathf.Min(1f, t * 60f) * Mathf.Exp(-11f * t);
                        data[i] += (Mathf.Sin(2f * Mathf.PI * notes[n] * t) + Mathf.Sin(2f * Mathf.PI * notes[n] * 2f * t) * 0.15f) * env;
                    }
                }
                Normalize(data, 0.6f);
                warn = FromSamples("vise", data);
            }
            Play(warn, 0.32f);
        }
        static AudioClip warn;

        public static void Alarm()
        {
            Play(AlarmClip(), 0.9f);
        }

        public static AudioClip AlarmClip()
        {
            if (alarm == null)
            {
                const float duration = 1.2f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                for (int n = 0; n < 3; n++)
                {
                    int start = Mathf.RoundToInt(Rate * n * 0.16f);
                    for (int i = start; i < count; i++)
                    {
                        float t = (float)(i - start) / Rate;
                        float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-9f * t);
                        data[i] += (Mathf.Sin(2f * Mathf.PI * 1318.5f * t) + Mathf.Sin(2f * Mathf.PI * 1975.5f * t) * 0.4f) * env * 0.4f;
                    }
                }
                Normalize(data, 0.85f);
                alarm = FromSamples("sentinelle", data);
            }
            return alarm;
        }

        /// <summary>Un pas dans les feuilles mortes : un froissement tres bref.</summary>
        public static void LeafStep()
        {
            if (PlayReal("PasHerbe", 0.3f)) return;
            if (leaf == null)
            {
                const float duration = 0.18f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                System.Random rng = new System.Random(77);
                float prev = 0f;
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate;
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    float crisp = noise - prev * 0.6f;
                    prev = noise;
                    float crackles = rng.NextDouble() < 0.02 ? 1.6f : 1f;
                    data[i] = crisp * Mathf.Exp(-22f * t) * crackles;
                }
                Normalize(data, 0.5f);
                leaf = FromSamples("feuilles", data);
            }
            Play(leaf, 0.16f);
        }

        static AudioClip flightWind;

        /// <summary>
        /// LE VENT DU VOL (28/09) : un souffle continu qui BOUCLE sans couture (la fin se
        /// fond dans le debut). On le joue en boucle pendant qu'on plane (voir GlideFeel).
        /// </summary>
        public static AudioClip FlightWind()
        {
            if (flightWind != null) return flightWind;
            int count = Rate * 3;
            float[] data = new float[count];
            System.Random r = new System.Random(31);
            float low = 0f;
            float low2 = 0f;
            for (int i = 0; i < count; i++)
            {
                float n = (float)r.NextDouble() * 2f - 1f;
                float cutoff = 0.035f + 0.02f * Mathf.Sin(i / (float)Rate * 2.1f);
                low += (n - low) * cutoff;
                low2 += (low - low2) * 0.25f;
                data[i] = low2;
            }
            int x = Rate * 3 / 10;
            int length = count - x;
            float[] loop = new float[length];
            for (int i = 0; i < length; i++) loop[i] = data[i];
            for (int i = 0; i < x; i++)
            {
                float k = (float)i / x;
                loop[i] = data[i] * k + data[length + i] * (1f - k);
            }
            Normalize(loop, 0.6f);
            flightWind = AudioClip.Create("vent du vol", length, 1, Rate, false);
            flightWind.SetData(loop, 0);
            return flightWind;
        }

        static AudioClip whoosh, thud;

        /// <summary>Un coup dans le vide : un souffle bref dont le filtre monte puis descend.</summary>
        public static void Whoosh()
        {
            if (whoosh == null)
            {
                const float duration = 0.28f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                System.Random rng = new System.Random(12);
                float low = 0f;
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate / duration;
                    float cutoff = 0.02f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                    low += (((float)rng.NextDouble() * 2f - 1f) - low) * cutoff;
                    data[i] = low * Mathf.Sin(t * Mathf.PI);
                }
                Normalize(data, 0.7f);
                whoosh = FromSamples("fendre", data);
            }
            Play(whoosh, 0.55f);
        }

        /// <summary>Un coup qui porte : un choc sourd (48 Hz qui tombe) et un craquement bref.</summary>
        public static void Thud()
        {
            if (PlayReal("Choc", 0.55f)) return;
            if (thud == null)
            {
                const float duration = 0.35f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                System.Random rng = new System.Random(5);
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate;
                    float body = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 45f, t / duration) * t) * Mathf.Exp(-14f * t);
                    float crack = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-70f * t) * 0.6f;
                    data[i] = body + crack;
                }
                Normalize(data, 0.9f);
                thud = FromSamples("choc", data);
            }
            Play(thud, 0.9f);
        }

        static AudioClip trapSnap;

        public static void TrapSnap()
        {
            if (PlayReal("Piege", 0.7f)) return;
            if (trapSnap == null) BuildTrapSnap();
            Play(trapSnap, 1f);
        }

        static void BuildTrapSnap()
        {
            {
                const float duration = 0.9f;
                int count = Mathf.RoundToInt(Rate * duration);
                float[] data = new float[count];
                System.Random rng = new System.Random(9);
                float prev = 0f;
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / Rate;
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    float high = noise - prev;              // un filtre passe-haut tout simple
                    prev = noise;
                    float clack = high * Mathf.Exp(-60f * t) * 1.2f;
                    float ring = (Mathf.Sin(2f * Mathf.PI * 1480f * t) + Mathf.Sin(2f * Mathf.PI * 2210f * t) * 0.7f)
                                 * Mathf.Exp(-7f * t) * 0.35f;
                    data[i] = clack + ring;
                }
                Normalize(data, 0.9f);
                trapSnap = FromSamples("piège", data);
            }
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
