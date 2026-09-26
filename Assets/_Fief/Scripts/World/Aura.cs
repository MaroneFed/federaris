using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'AURA (28/09 -- Martin : "un jeu plein d'aura : une animation ou on a plein
    /// d'aura, imagine une funk, la Yara Yara funk, un truc bien stylé").
    ///
    /// UN MOMENT D'AURA, quand tu fais quelque chose de fort -- voler la Couronne,
    /// la prendre au sommet, un pique d'aigle, ejecter quelqu'un dans les nuages,
    /// souffler deux joueurs d'un coup :
    ///   - le temps RALENTIT trois quarts de seconde ;
    ///   - un GROS TITRE claque au milieu de l'ecran, et en dessous « +1000 AURA » ;
    ///   - un coup de phonk (cloche, basse 808) ;
    ///   - des FLAMMES D'AURA montent autour de toi, a ta couleur ;
    ///   - le champ de vision s'ouvre, l'ecran tremble, ses bords s'illuminent.
    /// Les bots ont aussi leurs moments (les flammes, le son s'ils sont pres) ; le
    /// ralenti et le titre, c'est pour toi.
    ///
    /// Et en continu : le PORTEUR de la Couronne brule d'une aura d'or (on le voit de
    /// loin) ; si c'est toi, la musique passe en PHONK (voir MusicDirector).
    ///
    /// Concept Unity : Time.timeScale ralentit tout le jeu (1 = normal) ; l'ecran, lui,
    /// compte en temps NON ralenti (Time.unscaledDeltaTime) pour que le titre vive.
    /// </summary>
    public class Aura : MonoBehaviour
    {
        static Aura instance;
        AudioSource voice;
        string title;
        string sub;
        Color tint;
        float shown;              // temps (non ralenti) depuis le debut du moment
        float slow;
        const float TitleSeconds = 2.2f;

        static Aura Get()
        {
            if (instance != null) return instance;
            GameObject go = new GameObject("AURA");
            instance = go.AddComponent<Aura>();
            instance.voice = go.AddComponent<AudioSource>();
            instance.voice.spatialBlend = 0f;
            instance.voice.playOnAwake = false;
            instance.voice.ignoreListenerPause = true;
            instance.shown = 99f;
            return instance;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (slow > 0f && Mathf.Approximately(Time.timeScale, 0.3f)) Time.timeScale = 1f;
        }

        /// <summary>
        /// UN MOMENT D'AURA pour "s". "power" : 1 = normal, 2 = enorme (la victoire).
        /// </summary>
        public static void Moment(Seeker s, string what, Color c, float power)
        {
            if (s == null || s.Body == null) return;
            Aura a = Get();
            Vector3 at = s.Body.position;
            // Les flammes, pour tout le monde.
            AuraFlames.Burn(s.Body, c, 2.5f + power);
            Fx.Column(at, c, 18f * power, 0.35f, 0.9f);
            Fx.Ring(at + Vector3.up * 0.2f, c, 0.5f, 7f * power, 0.5f, 0.4f, Vector3.up);
            Fx.Shock(at + Vector3.up * 1.2f, c, 3.5f * power, 0.4f);
            Fx.Flash(at + Vector3.up * 1.5f, c, 20f, 7f, 0.5f);
            Transform me = Game.PlayerTransform;
            bool near = me != null && (me.position - at).magnitude < 40f;
            if (!s.IsPlayer)
            {
                if (near && !Sfx.Muted) a.voice.PlayOneShot(Phonk.Sting(), 0.35f);
                return;
            }
            // Pour toi : le ralenti, le titre, le son, la camera.
            a.title = what;
            a.sub = "+" + Mathf.RoundToInt(1000f * power) + " AURA";
            a.tint = c;
            a.shown = 0f;
            if (Mathf.Approximately(Time.timeScale, 1f))
            {
                a.slow = 0.75f * power;
                Time.timeScale = 0.3f;
            }
            if (!Sfx.Muted) a.voice.PlayOneShot(Phonk.Sting(), 0.8f);
            if (Game.Hud != null)
            {
                Game.Hud.Flash(new Color(c.r, c.g, c.b, 0.35f));
                if (Game.Hud.orbitCamera != null) { Game.Hud.orbitCamera.Kick(16f); Game.Hud.orbitCamera.Shake(0.3f); }
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            shown += dt;
            if (slow > 0f)
            {
                slow -= dt;
                // (La pause et la fin de manche reglent le temps elles-memes : on ne touche qu'a notre ralenti.)
                if (slow <= 0f && Mathf.Approximately(Time.timeScale, 0.3f)) Time.timeScale = 1f;
            }
        }

        void OnGUI()
        {
            if (shown > TitleSeconds || string.IsNullOrEmpty(title)) return;
            if (Game.Menus != null && Game.Menus.Blocking) return;
            UiStyle.Ensure();
            float k = shown / TitleSeconds;
            // Il claque (grand puis se pose), tremble un instant, puis s'efface.
            float punch = shown < 0.12f ? Mathf.Lerp(1.8f, 1f, shown / 0.12f) : 1f;
            float alpha = k < 0.8f ? 1f : 1f - (k - 0.8f) / 0.2f;
            float shake = shown < 0.3f ? (0.3f - shown) * 20f : 0f;
            Vector2 jolt = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shake;

            // Les bords de l'ecran brulent a sa couleur.
            float edge = UiStyle.S(90) * (1f - k * 0.7f);
            Color band = new Color(tint.r, tint.g, tint.b, 0.35f * alpha);
            UiStyle.FadeBand(new Rect(0f, 0f, Screen.width, edge), band);
            UiStyle.FadeBand(new Rect(0f, Screen.height - edge, Screen.width, edge), band);

            GUIStyle big = new GUIStyle(UiStyle.Big);
            big.alignment = TextAnchor.MiddleCenter;
            big.fontSize = Mathf.RoundToInt(UiStyle.S(64) * punch);
            big.wordWrap = false;
            float y = Screen.height * 0.3f;
            Rect r = new Rect(jolt.x, y + jolt.y, Screen.width, UiStyle.S(90));
            // Une ombre epaisse, un halo de sa couleur, puis le titre blanc chaud.
            for (int i = 0; i < 4; i++)
            {
                float o = UiStyle.S(2 + i);
                UiStyle.Tinted(new Rect(r.x + o, r.y + o, r.width, r.height), title, big, new Color(0f, 0f, 0f, 0.35f * alpha));
            }
            UiStyle.Tinted(new Rect(r.x - 2f, r.y, r.width, r.height), title, big, new Color(tint.r, tint.g, tint.b, 0.8f * alpha));
            UiStyle.Tinted(r, title, big, new Color(1f, 0.97f, 0.9f, alpha));

            GUIStyle small = new GUIStyle(UiStyle.Head);
            small.alignment = TextAnchor.MiddleCenter;
            small.fontSize = UiStyle.S(26);
            float subIn = Mathf.Clamp01((shown - 0.15f) / 0.2f);
            Rect sr = new Rect(0f, y + UiStyle.S(86), Screen.width, UiStyle.S(34));
            UiStyle.Tinted(new Rect(sr.x + 2f, sr.y + 2f, sr.width, sr.height), sub, small, new Color(0f, 0f, 0f, 0.5f * alpha * subIn));
            UiStyle.Tinted(sr, UiStyle.Spaced(sub), small, new Color(tint.r, tint.g, tint.b, alpha * subIn));
        }
    }

    /// <summary>
    /// DES FLAMMES D'AURA autour d'un corps : des langues de lumiere qui montent, a sa
    /// couleur. Quelques secondes apres un moment d'aura ; en continu sur le porteur
    /// de la Couronne (Keep).
    /// </summary>
    public class AuraFlames : MonoBehaviour
    {
        ParticleSystem flames;
        Light glow;
        float until;
        bool keep;

        /// <summary>Allumer (ou raviver) des flammes sur "body" pour "seconds".</summary>
        public static AuraFlames Burn(Transform body, Color c, float seconds)
        {
            if (body == null) return null;
            AuraFlames f = Attach(body, c);
            if (f != null) f.until = Mathf.Max(f.until, Time.time + seconds);
            return f;
        }

        /// <summary>Des flammes qui restent tant que "on" est vrai (le porteur de la Couronne).</summary>
        public static void Keep(Transform body, Color c, bool on)
        {
            if (body == null) return;
            AuraFlames f = body.GetComponentInChildren<AuraFlames>();
            if (!on) { if (f != null) f.keep = false; return; }
            if (f == null) f = Attach(body, c);
            if (f != null) f.keep = true;
        }

        static AuraFlames Attach(Transform body, Color c)
        {
            AuraFlames f = body.GetComponentInChildren<AuraFlames>();
            if (f != null) return f;
            Material m = Ambiance.Additive;
            if (m == null) return null;
            GameObject go = new GameObject("Aura");
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            f = go.AddComponent<AuraFlames>();
            f.flames = Ambiance.NewSystem("Flammes d'aura", go.transform, Vector3.zero, m);
            ParticleSystem.MainModule main = f.flames.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.6f));
            main.maxParticles = 300;
            ParticleSystem.EmissionModule emission = f.flames.emission;
            emission.rateOverTime = 90f;
            ParticleSystem.ShapeModule shape = f.flames.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.55f;
            f.flames.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystemRenderer rd = f.flames.GetComponent<ParticleSystemRenderer>();
            rd.renderMode = ParticleSystemRenderMode.Stretch;
            rd.velocityScale = 0.12f;
            rd.lengthScale = 2f;
            ParticleSystem.SizeOverLifetimeModule shrink = f.flames.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Ambiance.FadeInOut(f.flames, 0.9f);
            f.flames.Play();
            GameObject lg = new GameObject("Lueur d'aura");
            lg.transform.SetParent(go.transform, false);
            lg.transform.localPosition = Vector3.up * 1.2f;
            f.glow = lg.AddComponent<Light>();
            f.glow.type = LightType.Point;
            f.glow.color = c;
            f.glow.range = 7f;
            f.glow.intensity = 1.8f;
            f.glow.shadows = LightShadows.None;
            return f;
        }

        void Update()
        {
            bool on = keep || Time.time < until;
            if (flames != null)
            {
                ParticleSystem.EmissionModule emission = flames.emission;
                emission.rateOverTime = on ? 90f : 0f;
            }
            if (glow != null) glow.intensity = on ? 1.5f + 0.5f * Mathf.Sin(Time.time * 9f) : Mathf.MoveTowards(glow.intensity, 0f, Time.deltaTime * 3f);
            if (!on && glow != null && glow.intensity <= 0f && flames != null && flames.particleCount == 0) Destroy(gameObject);
        }
    }

    /// <summary>
    /// LE PHONK, FABRIQUE PAR LE CODE (en attendant les vrais morceaux de Martin : un
    /// fichier dont le nom contient "phonk" ou "aura", dans Resources/Music, le
    /// remplace -- voir MusicDirector). La recette du genre : une CLOCHE (deux ondes
    /// carrees desaccordees qui s'eteignent vite) qui joue un motif en mineur, une
    /// BASSE 808 (une sinusoide qui glisse vers le grave et sature), un kick, un clap
    /// sur les temps 2 et 4, des charlestons. 140 battements par minute.
    /// </summary>
    public static class Phonk
    {
        const int Rate = 22050;
        const float Bpm = 140f;
        static AudioClip loop, sting;

        // La cloche : quatre mesures de seize doubles-croches (-1 : silence), en demi-tons au-dessus de la base.
        static readonly int[] Bell =
        {
            0, -1, 0, -1, 3, -1, 0, -1, 7, -1, 5, -1, 3, -1, 0, -1,
            0, -1, 0, -1, 3, -1, 0, -1, 8, -1, 7, -1, 5, -1, 3, -1,
            0, -1, 0, -1, 3, -1, 0, -1, 7, -1, 5, -1, 3, -1, 5, -1,
            7, -1, 7, -1, 8, -1, 7, -1, 5, -1, 3, -1, 2, -1, 0, -1
        };
        // La basse (demi-tons sous la tonique, -99 : rien) et les kicks, par double-croche.
        static readonly int[] Bass =
        {
            0, -99, -99, -99, -99, -99, 0, -99, -99, -99, -99, -99, -99, -99, -2, -99,
            -4, -99, -99, -99, -99, -99, -4, -99, -99, -99, -99, -99, -99, -99, -5, -99,
            0, -99, -99, -99, -99, -99, 0, -99, -99, -99, -99, -99, -99, -99, -2, -99,
            -4, -99, -99, -99, -99, -99, -4, -99, -99, -99, 3, -99, -99, -99, 2, -99
        };

        /// <summary>La boucle (quatre mesures, environ sept secondes).</summary>
        public static AudioClip Loop()
        {
            if (loop != null) return loop;
            float step = 60f / Bpm / 4f;
            int count = Mathf.RoundToInt(Rate * step * 64f);
            float[] data = new float[count];
            System.Random rng = new System.Random(808);
            for (int s = 0; s < 64; s++)
            {
                int at = Mathf.RoundToInt(Rate * step * s);
                if (Bell[s] >= 0) Cowbell(data, at, Bell[s], 0.28f);
                if (Bass[s] > -99) Bass808(data, at, Bass[s], step * 5.5f, 0.6f);
                if (s % 16 == 0 || s % 16 == 10) Kick(data, at, 0.7f);
                if (s % 8 == 4) Clap(data, at, rng, 0.35f);
                if (s % 2 == 0) Hat(data, at, rng, s % 4 == 2 ? 0.12f : 0.07f);
            }
            Saturate(data, 1.6f);
            loop = Make("phonk (fabriqué)", data);
            return loop;
        }

        /// <summary>Le coup d'aura : une basse qui tombe, trois coups de cloche, un clap.</summary>
        public static AudioClip Sting()
        {
            if (sting != null) return sting;
            int count = Mathf.RoundToInt(Rate * 1.4f);
            float[] data = new float[count];
            System.Random rng = new System.Random(909);
            float step = 60f / Bpm / 4f;
            Kick(data, 0, 0.9f);
            Bass808(data, 0, 0, 1.2f, 0.9f);
            int[] notes = { 0, 3, 7, 5, 7 };
            for (int i = 0; i < notes.Length; i++) Cowbell(data, Mathf.RoundToInt(Rate * step * i), notes[i], 0.4f);
            Clap(data, Mathf.RoundToInt(Rate * step * 4f), rng, 0.5f);
            Saturate(data, 2f);
            sting = Make("aura", data);
            return sting;
        }

        static float Semi(int n) { return Mathf.Pow(2f, n / 12f); }

        static void Cowbell(float[] d, int at, int note, float level)
        {
            float k = Semi(note);
            float f1 = 540f * k, f2 = 800f * k;
            int len = Mathf.RoundToInt(Rate * 0.22f);
            for (int i = 0; i < len && at + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f1 * t)) + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f2 * t)) * 0.8f;
                float env = Mathf.Exp(-t * 18f) * 0.8f + Mathf.Exp(-t * 60f) * 0.4f;
                d[at + i] += sq * env * level * 0.5f;
            }
        }

        static void Bass808(float[] d, int at, int note, float seconds, float level)
        {
            float f = 49f * Semi(note);          // autour d'un sol grave
            int len = Mathf.RoundToInt(Rate * seconds);
            float phase = 0f;
            for (int i = 0; i < len && at + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float freq = f * (1f + 1.2f * Mathf.Exp(-t * 30f));      // le "glide" du 808
                phase += 2f * Mathf.PI * freq / Rate;
                float env = Mathf.Exp(-t * 2.2f) * Mathf.Clamp01(t * 200f);
                d[at + i] += (float)System.Math.Tanh(Mathf.Sin(phase) * 2.5f) * env * level;
            }
        }

        static void Kick(float[] d, int at, float level)
        {
            int len = Mathf.RoundToInt(Rate * 0.25f);
            float phase = 0f;
            for (int i = 0; i < len && at + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                phase += 2f * Mathf.PI * (45f + 110f * Mathf.Exp(-t * 35f)) / Rate;
                d[at + i] += Mathf.Sin(phase) * Mathf.Exp(-t * 12f) * level;
            }
        }

        static void Clap(float[] d, int at, System.Random rng, float level)
        {
            int len = Mathf.RoundToInt(Rate * 0.18f);
            float low = 0f;
            for (int i = 0; i < len && at + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float n = (float)rng.NextDouble() * 2f - 1f;
                low += (n - low) * 0.35f;
                float bursts = t < 0.03f ? (Mathf.Repeat(t, 0.01f) < 0.004f ? 1f : 0.3f) : 1f;
                d[at + i] += (n - low) * Mathf.Exp(-t * 22f) * bursts * level;
            }
        }

        static void Hat(float[] d, int at, System.Random rng, float level)
        {
            int len = Mathf.RoundToInt(Rate * 0.05f);
            float prev = 0f;
            for (int i = 0; i < len && at + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float n = (float)rng.NextDouble() * 2f - 1f;
                d[at + i] += (n - prev) * 0.5f * Mathf.Exp(-t * 90f) * level;
                prev = n;
            }
        }

        static void Saturate(float[] d, float drive)
        {
            float peak = 0.0001f;
            for (int i = 0; i < d.Length; i++)
            {
                d[i] = (float)System.Math.Tanh(d[i] * drive);
                peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            }
            for (int i = 0; i < d.Length; i++) d[i] *= 0.8f / peak;
        }

        static AudioClip Make(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
