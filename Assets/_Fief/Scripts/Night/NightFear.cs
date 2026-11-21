using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE SON DE LA PEUR (14/11). Martin, apres avoir joue : "les trucs sont pas flippants du tout".
    /// Ce qui fait peur dans un jeu, c'est d'abord ce qu'on ENTEND. Tout est fabrique ici par le code
    /// (aucun fichier) : on peut donc y jouer tout de suite ; les musiques et les voix de Martin
    /// s'ajoutent par-dessus des qu'il depose ses fichiers.
    ///
    ///   le fond       un vent grave qui ne s'arrete jamais ;
    ///   le coeur      il bat des que la peur monte, de plus en plus vite ;
    ///   le chateau    au hasard, loin : un choc, un fer qui gemit, un cri ;
    ///   les murmures  quand le Penitent est pres (ses vraies voix si Sons/Nuit/Voix/penitent-*.mp3 existent) ;
    ///   le coup       un accord qui grince, quand le Rodeur te voit et quand on t'attrape ;
    ///   la musique    Sons/Nuit/Musique/nuit-ambiance, nuit-traque (quand on te chasse), nuit-aube (15/11 : celles de Martin, Suno).
    ///
    /// Concept Unity : un AudioClip est une liste de nombres entre -1 et 1 (44 100 par seconde). On peut
    /// la calculer soi-meme : un sinus donne une note, du hasard donne du souffle.
    /// </summary>
    public static class NightAudio
    {
        const int Rate = 44100;
        static AudioSource bed, heart, calm, chase;
        static AudioClip beat, sting, whisper, groan, scream, bang, pas, hoof, snort;
        static readonly List<AudioClip> voices = new List<AudioClip>();
        static float nextBeat, nextEvent, nextWhisper;
        static System.Random random = new System.Random(11);

        static float Noise() { return (float)(random.NextDouble() * 2.0 - 1.0); }

        /// <summary>Fabrique un son : "wave" donne la valeur a l'instant t (en secondes).</summary>
        static AudioClip Make(string name, float seconds, System.Func<float, float, float> wave)
        {
            int n = Mathf.RoundToInt(seconds * Rate);
            float[] data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave.Invoke(i / (float)Rate, Noise()), -1f, 1f);
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioSource Source(GameObject host, string name, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = loop; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            return s;
        }

        public static void Init(GameObject host)
        {
            random = new System.Random(11);
            float wind = 0f, low = 0f;
            // Le fond : huit secondes de vent grave. Le debut et la fin sont a zero : la boucle ne claque pas.
            AudioClip air = Make("Vent du chateau", 8f, delegate(float t, float noise)
            {
                wind += (noise - wind) * 0.012f;
                float edge = Mathf.Clamp01(t / 0.4f) * Mathf.Clamp01((8f - t) / 0.4f);
                return (wind * 5f * (0.55f + 0.45f * Mathf.Sin(6.2832f * 0.125f * t)) + Mathf.Sin(6.2832f * 46f * t) * 0.05f
                    + Mathf.Sin(6.2832f * 69.3f * t) * 0.03f) * (0.35f + 0.65f * edge);
            });
            beat = Make("Coeur", 0.5f, delegate(float t, float noise)
            {
                float a = Mathf.Sin(6.2832f * 58f * t) * Mathf.Exp(-t * 22f);
                float u = t - 0.17f;
                float b = u > 0f ? Mathf.Sin(6.2832f * 50f * u) * Mathf.Exp(-u * 24f) * 0.7f : 0f;
                return (a + b) * 0.95f;
            });
            sting = Make("Coup", 1.7f, delegate(float t, float noise)
            {
                float s = Mathf.Sin(6.2832f * 196f * t) + Mathf.Sin(6.2832f * 207.7f * t) + Mathf.Sin(6.2832f * 277.2f * t)
                    + Mathf.Sin(6.2832f * 415.3f * t) * 0.7f + Mathf.Sin(6.2832f * 587.3f * t) * 0.5f;
                return s * 0.2f * Mathf.Exp(-t * 2.3f) + noise * 0.6f * Mathf.Exp(-t * 16f) + Mathf.Sin(6.2832f * 41f * t) * 0.5f * Mathf.Exp(-t * 3f);
            });
            whisper = Make("Murmure", 1.5f, delegate(float t, float noise)
            {
                low += (noise - low) * 0.22f;
                float syllables = Mathf.Pow(Mathf.Abs(Mathf.Sin(6.2832f * 2.4f * t + Mathf.Sin(6.2832f * 1.3f * t) * 1.4f)), 3f);
                float env = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((1.5f - t) / 0.3f);
                return (noise - low) * 0.55f * syllables * env;
            });
            groan = Make("Fer qui gemit", 3f, delegate(float t, float noise)
            {
                float f = 86f - 26f * (t / 3f);
                float env = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((3f - t) / 0.9f);
                return (Mathf.Sin(6.2832f * f * t) * 0.6f + Mathf.Sin(6.2832f * f * 1.51f * t) * 0.22f + noise * 0.03f)
                    * (0.6f + 0.4f * Mathf.Sin(6.2832f * 7f * t)) * env;
            });
            float phase = 0f;
            scream = Make("Cri", 2.2f, delegate(float t, float noise)
            {
                float rise = Mathf.Sin(Mathf.Clamp01(t / 0.35f) * 1.5708f);
                float f = 330f + 290f * rise - 190f * Mathf.Clamp01((t - 0.9f) / 1.3f) + Mathf.Sin(6.2832f * 9f * t) * 22f;
                phase += 6.2832f * f / Rate;
                float s = 0f;
                for (int k = 1; k <= 6; k++) s += Mathf.Sin(phase * k) / k;
                float env = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((2.2f - t) / 0.9f);
                return (s * 0.42f + noise * 0.13f) * env;
            });
            bang = Make("Fracas", 1.3f, delegate(float t, float noise)
            {
                return noise * 0.9f * Mathf.Exp(-t * 9f) + Mathf.Sin(6.2832f * 48f * t) * 0.9f * Mathf.Exp(-t * 5f);
            });

            pas = Make("Pas", 0.18f, delegate(float t, float noise)
            {
                return noise * 0.45f * Mathf.Exp(-t * 40f) + Mathf.Sin(6.2832f * 72f * t) * 0.9f * Mathf.Exp(-t * 26f);
            });
            hoof = Make("Sabot", 0.14f, delegate(float t, float noise)
            {
                return noise * 0.35f * Mathf.Exp(-t * 55f) + Mathf.Sin(6.2832f * 95f * t) * 0.9f * Mathf.Exp(-t * 32f)
                    + Mathf.Sin(6.2832f * 180f * t) * 0.3f * Mathf.Exp(-t * 60f);
            });
            float nose = 0f;
            snort = Make("Renaclement", 0.9f, delegate(float t, float noise)
            {
                nose += (noise - nose) * 0.25f;
                float env = Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 3.2f) * (0.6f + 0.4f * Mathf.Sin(6.2832f * 28f * t));
                return (nose * 2.2f + Mathf.Sin(6.2832f * 70f * t) * 0.2f) * env;
            });
            bed = Source(host, "Fond", true);
            bed.clip = air;
            bed.Play();
            heart = Source(host, "Coeur", false);
            calm = Source(host, "Musique (calme)", true);
            chase = Source(host, "Musique (traque)", true);
            calm.clip = Resources.Load<AudioClip>("Sons/Nuit/Musique/nuit-ambiance");
            chase.clip = null;      // (15/11) Martin : "la musique est horrible quand on se fait voir". Plus de musique de traque.
            if (calm.clip != null) calm.Play();
            if (chase.clip != null) chase.Play();
            voices.Clear();
            // (15/11) Martin : "je deteste les voix". Plus aucune voix, plus aucun murmure : seulement le chateau.
            nextBeat = 0f; nextEvent = 9f; nextWhisper = 2f;
        }

        /// <summary>A chaque image. "fear" va de 0 (calme) a 1 (il est sur toi).</summary>
        public static void Tick(float dt, float fear, bool hunted, Vector3 listener, bool night)
        {
            if (bed == null) return;
            float vol = Settings.Volume;
            bed.volume = Mathf.MoveTowards(bed.volume, (night ? 0.5f + 0.3f * fear : 0.08f) * vol, dt * 0.4f);
            bed.pitch = 1f + 0.35f * fear;       // le vent monte quand on te chasse : c'est lui, la musique de traque
            calm.volume = Mathf.MoveTowards(calm.volume, (night && !hunted ? 0.4f : 0f) * vol, dt * 0.5f);
            chase.volume = Mathf.MoveTowards(chase.volume, (night && hunted ? 0.65f : 0f) * vol, dt * 0.9f);
            if (!night) return;

            if (fear > 0.12f)
            {
                nextBeat -= dt;
                if (nextBeat <= 0f)
                {
                    heart.pitch = 0.9f + 0.28f * fear;
                    heart.PlayOneShot(beat, (0.3f + 0.7f * fear) * vol);
                    nextBeat = Mathf.Lerp(1.25f, 0.40f, fear);
                }
            }
            nextEvent -= dt;
            if (nextEvent <= 0f)
            {
                nextEvent = Random.Range(12f, 30f);
                float a = Random.Range(0f, 6.2832f);
                Vector3 far = listener + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * Random.Range(7f, 13f);
                int pick = Random.Range(0, 4);
                AudioSource.PlayClipAtPoint(pick % 2 == 0 ? groan : bang, far, 0.7f * vol);
            }
            if (fear > 2f)       // les murmures : retires
            {
                nextWhisper -= dt;
                if (nextWhisper <= 0f)
                {
                    nextWhisper = Random.Range(2.5f, 5f);
                    float a = Random.Range(0f, 6.2832f);
                    Vector3 close = listener + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.2f;
                    AudioClip clip = voices.Count > 0 ? voices[Random.Range(0, voices.Count)] : whisper;
                    AudioSource.PlayClipAtPoint(clip, close, (0.4f + 0.6f * fear) * vol);
                }
            }
        }

        /// <summary>Un pas, quelque part : les pas qui te suivent.</summary>
        public static void Step(Vector3 at, float volume) { if (pas != null) AudioSource.PlayClipAtPoint(pas, at, volume * Settings.Volume); }

        /// <summary>(21/11) Un sabot qui frappe la pierre (l'arrivee a cheval).</summary>
        public static void Hoof(float volume, float pitch) { if (heart != null && hoof != null) { heart.pitch = pitch; heart.PlayOneShot(hoof, volume * Settings.Volume); } }
        public static void Snort() { if (heart != null && snort != null) { heart.pitch = 1f; heart.PlayOneShot(snort, 0.9f * Settings.Volume); } }

        public static void Sting(float volume) { if (heart != null) heart.PlayOneShot(sting, volume * Settings.Volume); }
        public static void Bang(Vector3 at, float volume) { if (bang != null) AudioSource.PlayClipAtPoint(bang, at, volume * Settings.Volume); }
        public static void Scream(Vector3 at, float volume) { if (groan != null) AudioSource.PlayClipAtPoint(groan, at, volume * Settings.Volume); }      // plus de cri : un fer qui gemit

        /// <summary>La voix de l'homme de l'ouverture, si Martin l'a deposee (Sons/Nuit/Voix/intro-homme). Renvoie false sinon.</summary>
        public static bool HasVoice { get { return false; } }      // (15/11) les voix sont retirees

        public static bool Speak(Vector3 at)
        {
            AudioClip line = HasVoice ? Resources.Load<AudioClip>("Sons/Nuit/Voix/intro-homme") : null;
            if (line == null) return false;
            AudioSource.PlayClipAtPoint(line, at, Settings.Volume);
            return true;
        }

        /// <summary>L'aube : la musique de la fin, une fois.</summary>
        public static void Dawn()
        {
            AudioClip end = Resources.Load<AudioClip>("Sons/Nuit/Musique/nuit-aube");
            if (end != null && heart != null) heart.PlayOneShot(end, 0.7f * Settings.Volume);
        }
    }

    /// <summary>
    /// L'OUVERTURE (14/11), telle que Martin l'a racontee : "un gars nous dit : c'est plus comme avant,
    /// tout a change ; il entend un bruit ; on se retourne, il y a une forme noire ; quand on se
    /// retourne, on ne voit plus rien, il est parti en hurlant".
    ///
    /// Une CINEMATIQUE : pendant douze secondes le joueur ne controle rien, c'est le code qui tourne sa
    /// tete. Deux bandes noires en haut et en bas disent "regarde". Entree : passer.
    ///
    /// Concept Unity : pas de video. On deplace la vraie camera du jeu, image par image, selon l'heure
    /// (t). C'est comme ca que sont faites presque toutes les cinematiques de jeu.
    /// </summary>
    public static class NightCinema
    {
        public static bool Playing;
        static float t, bars, black;
        static NightPlayer player;
        static GameObject knight, shape;
        static string line = "";
        static int step;
        // (15/11) La voix de Martin ("Ne restez pas ici", 9,7 s) : trois phrases, puis le cri a 8,4 s. Quand elle est
        // la, tout ce qui suit les phrases est retarde de "late" secondes pour que le cri tombe dans le noir.
        static bool voiced;
        static float late;
        static Material dark;

        static Vector3 KnightAt, ShapeAt;

        /// <summary>Ou se tiennent l'homme (devant) et la forme noire (derriere), pour un joueur pose a "eye" et tourne vers le nord.</summary>
        public static void Aim(Vector3 eye)
        {
            KnightAt = new Vector3(eye.x, 0f, eye.z + 3.4f);
            ShapeAt = new Vector3(eye.x, 0f, eye.z - 1.9f);
        }

        public static void Begin(NightPlayer who, Transform world)
        {
            player = who; t = 0f; step = 0; line = ""; bars = 0f; black = 1f;
            Aim(who.transform.position);
            voiced = NightAudio.HasVoice;
            late = voiced ? 1.4f : 0f;
            Clear();
            if (Application.isBatchMode) { Finish(); return; }       // les photos de Claude : pas de cinematique
            Stage(world, true, false);
            Playing = true;
            player.Face(0f, 0f);
        }

        /// <summary>Pose le chevalier et/ou la forme noire (aussi pour les photos).</summary>
        public static void Stage(Transform world, bool withKnight, bool withShape)
        {
            if (withKnight && knight == null)
            {
                knight = NightKit.Model("chevalier", world);
                if (knight != null) { knight.transform.position = KnightAt; knight.transform.rotation = Quaternion.Euler(0f, 180f, 0f); }
            }
            if (withShape && shape == null)
            {
                shape = NightKit.Model("penitent_3", world);
                if (shape != null)
                {
                    shape.transform.position = ShapeAt;
                    Renderer r = shape.GetComponentInChildren<Renderer>();
                    if (r != null && r.sharedMaterial != null)
                    {
                        // Noire comme un trou : on ne lit que sa forme, et deux points rouges.
                        dark = NightKit.Shadow();
                        if (dark == null) dark = new Material(r.sharedMaterial);
                        dark.color = new Color(0.02f, 0.02f, 0.025f);
                        dark.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.03f) * 2.6f);
                        foreach (Renderer each in shape.GetComponentsInChildren<Renderer>(true)) each.sharedMaterial = dark;
                    }
                }
            }
        }

        public static void Clear()
        {
            // On les cache tout de suite (Destroy n'agit qu'a la fin de l'image).
            if (knight != null) { knight.SetActive(false); Object.Destroy(knight); }
            if (shape != null) { shape.SetActive(false); Object.Destroy(shape); }
            knight = null; shape = null;
        }

        static void Finish()
        {
            Clear();
            NightKit.SnuffEntry();          // les torches du vestibule ne se rallument pas : la nuit commence dans le noir
            Playing = false; bars = 0f; black = 0f; line = "";
            if (player != null) { player.Face(0f, 0f); if (player.View != null) player.View.fieldOfView = 72f; }
        }

        static bool At(int index, float time)
        {
            if (step != index || t < time) return false;
            step++;
            return true;
        }

        public static void Tick(float dt)
        {
            if (!Playing) return;
            if (FiefInput.ConfirmPressed) { Finish(); return; }
            t += dt;
            Camera cam = player.View;
            float L = late;
            bars = Mathf.MoveTowards(bars, t < 10.6f + L ? 1f : 0f, dt * 1.5f);

            if (t < 5.6f + L)
            {
                // Il nous parle. La camera se resserre doucement sur lui.
                black = Mathf.MoveTowards(black, 0f, dt / 1.6f);
                player.Face(0f, -3f);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(72f, 58f, Mathf.Clamp01((t - 1f) / 4.5f));
            }
            if (At(0, voiced ? 0.8f : 1.8f)) { line = "C'est plus comme avant."; NightAudio.Speak(KnightAt + Vector3.up * 1.7f); }
            if (At(1, voiced ? 3.1f : 3.7f)) line = "Tout a changé.";
            if (voiced && step == 2 && t >= 5.6f) line = "Ne restez pas ici.";
            if (At(2, 5.6f + L))
            {
                // Le bruit, derriere nous. Il sursaute.
                line = "";
                NightAudio.Bang(ShapeAt + Vector3.up, 1f);
                if (knight != null) knight.transform.position = KnightAt + new Vector3(0.12f, 0f, 0.25f);
                // (16/11) Martin : "pourquoi on voit le monstre ? on ne doit PAS le voir". On se retourne : il n'y a rien.
                // Seulement les torches qui meurent l'une apres l'autre, et le noir qui vient.
            }
            if (t >= 5.85f + L && t < 6.5f + L)
            {
                // On se retourne d'un coup.
                float k = Mathf.Clamp01((t - 5.85f - L) / 0.55f);
                k = k * k * (3f - 2f * k);
                player.Face(180f * k, -3f - 9f * k);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(58f, 74f, k);
            }
            if (At(3, 6.5f + L))
            {
                // La forme noire. Et lui, derriere nous, qui s'enfuit en hurlant.
                NightAudio.Sting(1f);
                if (knight != null) { Object.Destroy(knight); knight = null; }
            }
            if (t >= 6.5f + L && t < 8.4f + L)
            {
                player.Face(180f, -12f);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(74f, 56f, Mathf.Clamp01((t - 6.5f - L) / 1.9f));
            }
            if (At(4, 6.9f + L)) { if (!voiced) NightAudio.Scream(KnightAt + new Vector3(0f, 1.6f, 6f), 1f); }      // la voix de Martin a son propre cri
            if (At(5, 7.3f + L)) NightKit.SnuffEntryOne(0);
            if (At(6, 7.8f + L)) NightKit.SnuffEntryOne(1);
            if (At(7, 8.4f + L))
            {
                // Noir. Quand la lumiere revient, il n'y a plus personne.
                black = 1f;
                NightAudio.Bang(player.transform.position, 0.6f);
                Clear();
                player.Face(0f, 0f);
                if (cam != null) cam.fieldOfView = 72f;
            }
            if (t >= 9.9f + L) black = Mathf.MoveTowards(black, 0f, dt / 1.2f);
            if (t >= 11.6f + L) Finish();
        }

        /// <summary>Les bandes noires, le noir, et ce que dit l'homme.</summary>
        public static void Draw(float w, float h, GUIStyle text)
        {
            if (black > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, black);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            }
            if (bars > 0f)
            {
                float band = Mathf.Round(h * 0.11f * bars);
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, w, band), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, h - band, w, band), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            if (Playing && line.Length > 0) NightFont.Label(new Rect(w * 0.1f, h * 0.80f, w * 0.8f, h * 0.08f), line, text);
        }
    }

    /// <summary>
    /// LE NOIR SUR LES BORDS : l'image s'assombrit vers les coins, et de plus en plus quand la peur monte.
    /// On a l'impression de voir par une fente : on ne sait plus ce qu'il y a sur les cotes.
    /// </summary>
    public static class NightVeil
    {
        static Texture2D veil;

        public static void Draw(float w, float h, float fear)
        {
            if (veil == null)
            {
                const int size = 128;
                veil = new Texture2D(size, size, TextureFormat.RGBA32, false);
                veil.hideFlags = HideFlags.HideAndDontSave;
                veil.wrapMode = TextureWrapMode.Clamp;
                Color[] px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x - 63.5f) / 63.5f, dy = (y - 63.5f) / 63.5f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((r - 0.38f) / 0.78f);
                        px[y * size + x] = new Color(0f, 0f, 0f, a * a * (3f - 2f * a));
                    }
                veil.SetPixels(px);
                veil.Apply();
            }
            GUI.color = new Color(1f, 1f, 1f, 0.72f + 0.28f * fear);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), veil);
            if (fear > 0.5f)
            {
                // Tout pres : un voile rouge qui bat avec le coeur.
                GUI.color = new Color(0.5f, 0f, 0f, (fear - 0.5f) * 0.22f * (0.6f + 0.4f * Mathf.Sin(Time.time * 9f)));
                GUI.DrawTexture(new Rect(0f, 0f, w, h), veil);
            }
            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// L'ECRITURE (15/11). Martin : "je deteste la police d'ecriture". C'etait celle d'Unity par defaut (Arial).
    /// On prend une police a empattements du PC (Garamond si elle est la, sinon Palatino, Book Antiqua...),
    /// celle des livres anciens, et chaque phrase porte une ombre douce pour se lire sur n'importe quel fond.
    ///
    /// Concept Unity : CreateDynamicFontFromOSFont emprunte une police installee sur l'ordinateur ; on lui
    /// donne plusieurs noms, il garde la premiere qu'il trouve.
    /// </summary>
    public static class NightFont
    {
        static Font font;

        static Font gothic;

        /// <summary>(21/11) La police des titres : une gothique (Old English), celle des manuscrits.</summary>
        public static Font Title()
        {
            if (gothic == null) gothic = Font.CreateDynamicFontFromOSFont(new[] { "Old English Text MT", "Centaur", "Book Antiqua", "Georgia" }, 64);
            return gothic;
        }

        public static Font Get()
        {
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Centaur", "Book Antiqua", "Palatino Linotype", "Cambria", "Georgia", "Times New Roman" }, 36);
            return font;
        }

        public static void Label(Rect where, string words, GUIStyle style)
        {
            Color ink = style.normal.textColor, keep = GUI.color;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            for (int dx = -2; dx <= 2; dx += 2)
                for (int dy = -2; dy <= 2; dy += 2)
                    if (dx != 0 || dy != 0) GUI.Label(new Rect(where.x + dx, where.y + dy + 1f, where.width, where.height), words, style);
            style.normal.textColor = ink;
            GUI.color = keep;
            GUI.Label(where, words, style);
        }
    }
}
