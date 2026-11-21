using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE SON DE LA NUIT (refait le 21/11). Martin : "change tous les sons et la musique, horribles pour les oreilles".
    /// Avant, presque tout etait FABRIQUE par le calcul (des sinus et du bruit) : ca sonnait faux. Maintenant ce sont de
    /// VRAIS ENREGISTREMENTS, tous en CC0 (domaine public, voir Resources/Sons/Nuit/LICENCES.txt), ranges par role :
    ///
    ///   Fond      un souffle grave de souterrain, des gouttes, en boucle        Musique  Juhani Junkala, "Horror Atmosphere"
    ///   Coeur     un vrai battement, lent puis rapide                            Aube     un piano melancolique, a la fin
    ///   Frisson   de courtes nappes sombres : le coup de peur                    Cri, Gemit, Grogne, Rugit, Souffle : les betes
    ///   Aboie, Hurle  le chien, les loups                                        Serrure, Feu, Livre, Coffre, Pierre, Chaine : les gestes
    ///
    /// Pour changer un son : remplacer les fichiers du dossier. Il en joue un au hasard, un peu plus grave ou plus aigu
    /// a chaque fois (jamais deux fois tout a fait le meme).
    ///
    /// Concept Unity : Resources.LoadAll charge tous les fichiers d'un dossier "Resources" ; AudioSource.PlayClipAtPoint
    /// joue un son a un endroit du monde : plus il est loin, moins on l'entend.
    /// </summary>
    public static class NightAudio
    {
        const string Dir = "Sons/Nuit/";
        static AudioSource bed, heart, calm, voice;
        static readonly Dictionary<string, AudioClip[]> banks = new Dictionary<string, AudioClip[]>();
        static float nextBeat, nextEvent;

        static AudioSource Source(GameObject host, string name, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = loop; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            return s;
        }

        /// <summary>Tous les sons d'un role (un dossier de Resources/Sons/Nuit), charges une seule fois.</summary>
        static AudioClip[] Bank(string role)
        {
            AudioClip[] clips;
            if (banks.TryGetValue(role, out clips)) return clips;
            clips = Resources.LoadAll<AudioClip>(role.Contains("/") ? role : Dir + role);
            banks[role] = clips;
            return clips;
        }

        static AudioClip Pick(string role)
        {
            AudioClip[] clips = Bank(role);
            return clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];
        }

        /// <summary>Un son du role, joue a cet endroit du monde, un peu plus grave ou aigu au hasard.</summary>
        static void At(string role, Vector3 where, float volume, float minDistance, float pitch)
        {
            AudioClip clip = Pick(role);
            if (clip == null) return;
            GameObject go = new GameObject("Son : " + role);
            go.transform.position = where;
            AudioSource s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = minDistance;
            s.maxDistance = 45f;
            s.pitch = pitch * Random.Range(0.93f, 1.07f);
            s.volume = Mathf.Clamp01(volume * Settings.Volume);
            s.dopplerLevel = 0f;
            s.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, s.pitch) + 0.2f);
        }

        /// <summary>Un son "dans la tete" (pas d'endroit), sur la voie du coeur.</summary>
        static void Near(string role, float volume, float pitch)
        {
            AudioClip clip = Pick(role);
            if (clip == null || voice == null) return;
            voice.pitch = pitch * Random.Range(0.95f, 1.05f);
            voice.PlayOneShot(clip, volume * Settings.Volume);
        }

        public static void Init(GameObject host)
        {
            banks.Clear();
            bed = Source(host, "Fond", true);
            bed.clip = Pick("Fond");
            if (bed.clip != null) bed.Play();
            heart = Source(host, "Coeur", false);
            voice = Source(host, "Voix", false);
            voice.volume = 1f;
            heart.volume = 1f;
            calm = Source(host, "Musique", true);
            calm.clip = Pick("Musique");
            if (calm.clip != null) calm.Play();
            nextBeat = 0f; nextEvent = 14f;
        }

        /// <summary>A chaque image. "fear" va de 0 (calme) a 1 (il est sur toi).</summary>
        public static void Tick(float dt, float fear, bool hunted, Vector3 listener, bool night)
        {
            if (bed == null) return;
            float vol = Settings.Volume;
            // Le fond souffle plus fort quand la peur monte ; la musique, elle, se retire : on n'entend plus que le chateau.
            bed.volume = Mathf.MoveTowards(bed.volume, (night ? 0.45f + 0.25f * fear : 0.1f) * vol, dt * 0.4f);
            calm.volume = Mathf.MoveTowards(calm.volume, (night ? 0.38f * (1f - 0.6f * fear) : 0f) * vol, dt * 0.3f);
            if (!night) return;

            // Le coeur : un vrai battement, qui bat plus vite et plus fort quand le danger approche.
            if (fear > 0.15f)
            {
                nextBeat -= dt;
                if (nextBeat <= 0f)
                {
                    AudioClip[] beats = Bank("Coeur");
                    if (beats.Length > 0)
                    {
                        AudioClip b = beats[fear > 0.6f && beats.Length > 1 ? 0 : beats.Length - 1];
                        heart.pitch = 0.95f + 0.15f * fear;
                        heart.PlayOneShot(b, (0.25f + 0.75f * fear) * vol);
                        nextBeat = Mathf.Max(b.length * 0.95f, 1.2f) / heart.pitch;
                    }
                    else nextBeat = 1f;
                }
            }
            // Le chateau vit : au loin, une chaine, une pierre qui tombe, un gemissement, un souffle.
            nextEvent -= dt;
            if (nextEvent <= 0f)
            {
                nextEvent = Random.Range(14f, 32f);
                float a = Random.Range(0f, 6.2832f);
                Vector3 far = listener + new Vector3(Mathf.Cos(a), 0.4f, Mathf.Sin(a)) * Random.Range(9f, 16f);
                string[] roles = { "Chaine", "Pierre", "Gemit", "Souffle", "Pierre" };
                At(roles[Random.Range(0, roles.Length)], far, 0.55f, 3f, 0.85f);
            }
        }

        /// <summary>Un pas, quelque part : les pas qui te suivent.</summary>
        public static void Step(Vector3 at, float volume) { At("Sons/Pas", at, volume * 0.8f, 2f, 0.8f); }

        /// <summary>(21/11) Un sabot qui frappe la pierre (l'arrivee a cheval) : un pas de pierre, plus grave.</summary>
        public static void Hoof(float volume, float pitch) { Near("Sons/Pas", volume * 0.8f, 0.55f * pitch); }
        public static void Snort() { Near("Renacle", 0.8f, 0.8f); }

        /// <summary>Le coup de peur : une courte nappe sombre, et un grondement dessous.</summary>
        public static void Sting(float volume) { Near("Frisson", volume * 0.9f, 0.9f); }
        /// <summary>Un fracas dans le chateau (une pierre qui tombe, une porte).</summary>
        public static void Bang(Vector3 at, float volume) { At("Pierre", at, volume, 4f, 0.75f); }
        /// <summary>Un cri de bete (ou un gemissement de fantome, au hasard).</summary>
        public static void Scream(Vector3 at, float volume) { At(Random.value < 0.5f ? "Cri" : "Grogne", at, volume, 3f, 1f); }

        // (21/11) Les voix des betes et des animaux.
        public static void Growl(Vector3 at, float volume) { At("Grogne", at, volume, 2.5f, 1f); }
        public static void Roar(Vector3 at, float volume) { At("Rugit", at, volume, 5f, 0.9f); }
        public static void Moan(Vector3 at, float volume) { At("Gemit", at, volume, 2f, 1f); }
        public static void Breath(Vector3 at, float volume) { At("Souffle", at, volume, 1.5f, 1f); }
        public static void Bark(Vector3 at, float volume) { At("Aboie", at, volume, 3f, 1f); }
        public static void Howl(Vector3 at, float volume) { At("Hurle", at, volume, 8f, 1f); }
        // (21/11) Les gestes.
        public static void Lock(Vector3 at) { At("Serrure", at, 0.8f, 1.5f, 1f); }
        public static void Fire(Vector3 at) { At("Feu", at, 0.6f, 2f, 1f); }
        public static void Book(Vector3 at) { At("Livre", at, 0.8f, 2f, 1f); }
        public static void Chest(Vector3 at) { At("Coffre", at, 0.9f, 2f, 0.9f); }
        /// <summary>Le tonnerre (la cour, les eclairs) : un vrai orage enregistre.</summary>
        public static void Thunder(Vector3 at) { At("Tonnerre", at, 1f, 20f, 1f); }
        public static void Chain(Vector3 at) { At("Chaine", at, 0.8f, 3f, 0.9f); }
        /// <summary>Une trouvaille, une recompense : un tintement clair.</summary>
        public static void Treasure(Vector3 at) { At("Tresor", at, 0.8f, 3f, 1f); }

        public static bool HasVoice { get { return false; } }      // (15/11) les voix sont retirees
        public static bool Speak(Vector3 at) { return false; }

        /// <summary>L'aube : la musique de la fin (le piano), une fois ; la musique de la nuit s'efface.</summary>
        public static void Dawn()
        {
            AudioClip end = Pick("Aube");
            if (calm != null) { calm.Stop(); if (end != null) { calm.clip = end; calm.loop = false; calm.volume = 0.6f * Settings.Volume; calm.Play(); } }
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
        // (21/11) Martin : "le gars au debut, il dit 'survis'. De quoi ? C'est qui ce gars ? Il doit etre en train d'agoniser
        // sur le sol et t'expliquer qu'il y a peut-etre des survivants a un certain endroit". C'est maintenant UN VEILLEUR,
        // en armure, couche contre le mur dans une flaque sombre. Il te dit ce qui s'est passe, ou sont les vivants, te
        // tend sa torche, et meurt. Puis le bruit dans ton dos, les torches qui s'eteignent, le noir (on ne voit RIEN).
        public static bool Playing;
        static float t, bars, black;
        static NightPlayer player;
        static GameObject knight, shape, pool;
        static string line = "";
        static int step;
        static Light dying;

        static Vector3 KnightAt, ShapeAt;

        static readonly string[] Lines =
        {
            "Toi... tu es vivant...",
            "Les Veilleurs sont tombés. Tous. L'Abbé a ouvert la crypte... et la nuit est entrée avec lui.",
            "Il reste des vivants. La cuisinière... aux cuisines. Le palefrenier, aux écuries. Le petit novice... en haut du donjon.",
            "Prends ma torche. Ne la laisse jamais mourir. Ravive-la à chaque feu.",
            "Le noir... ne passe pas la lumière. Allume les torches... derrière toi..."
        };
        static readonly float[] When = { 2.0f, 5.2f, 10.6f, 16.6f, 21.0f };
        const float Dies = 25.2f;

        /// <summary>Ou est couche le Veilleur (devant toi, contre le mur) et ou serait la forme (derriere), pour des yeux a "eye".</summary>
        public static void Aim(Vector3 eye)
        {
            KnightAt = new Vector3(eye.x + 0.2f, 0f, eye.z + 3.1f);      // ses pieds ; sa tete est a 1,2 m de toi
            ShapeAt = new Vector3(eye.x, 0f, eye.z - 1.9f);
        }

        public static void Begin(NightPlayer who, Transform world)
        {
            player = who; t = 0f; step = 0; line = ""; bars = 0f; black = 1f;
            Aim(who.transform.position);
            Clear();
            if (Application.isBatchMode) { Finish(); return; }       // les photos de Claude : pas de cinematique
            Stage(world, true, false);
            Playing = true;
            HeldTorch.Raise = 0f;
            player.Face(0f, -38f);
        }

        /// <summary>Pose le Veilleur couche et/ou la forme noire (aussi pour les photos).</summary>
        public static void Stage(Transform world, bool withKnight, bool withShape)
        {
            if (withKnight && knight == null)
            {
                knight = NightKit.Model("chevalier", world);
                if (knight != null)
                {
                    // Couche sur le dos, la tete vers toi (un peu relevee), le visage vers le plafond.
                    knight.transform.position = KnightAt + new Vector3(0f, 0.2f, 0f);
                    knight.transform.rotation = Quaternion.Euler(-82f, 0f, 7f);
                    Material dim = NightKit.Mat("Fer");
                    if (dim != null) foreach (Renderer r in knight.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = dim;
                }
                // La flaque, sombre et luisante, sous lui : plusieurs taches qui se chevauchent, jamais un rond net.
                Proto.BeginVisualOnly();
                pool = new GameObject("Flaque");
                if (world != null) pool.transform.SetParent(world, false);
                Material blood = MaterialFactory.GetShiny(new Color(0.07f, 0.006f, 0.006f), 0.92f, 0f);
                // Une douzaine de petites taches serrees sous le torse et la tete : de pres, elles se fondent en une flaque.
                System.Random rnd = new System.Random(11);
                Vector3[] spots = new Vector3[12];
                float[] sizes = new float[12];
                for (int i = 0; i < spots.Length; i++)
                {
                    float t = i / 11f;
                    spots[i] = new Vector3((float)(rnd.NextDouble() - 0.5) * 0.7f, 0f, -0.9f - t * 1.1f + (float)(rnd.NextDouble() - 0.5) * 0.3f);
                    sizes[i] = 0.22f + (float)rnd.NextDouble() * 0.38f * (1f - Mathf.Abs(t - 0.5f));
                }
                for (int i = 0; i < spots.Length; i++)
                {
                    GameObject spot = Proto.Cylinder(pool.transform, KnightAt + spots[i] + Vector3.up * (0.008f + i * 0.0015f), new Vector3(sizes[i], 0.003f, sizes[i] * (0.75f + 0.04f * (i % 5))), Color.black, "Tache");
                    spot.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                    spot.GetComponent<Renderer>().sharedMaterial = blood;
                }
                Proto.EndVisualOnly();
                // Sa torche tombee, encore allumee : c'est elle qui l'eclaire.
                GameObject lamp = new GameObject("Sa torche");
                lamp.transform.SetParent(knight != null ? knight.transform.parent : world, false);
                lamp.transform.position = KnightAt + new Vector3(0.9f, 0.3f, -1.5f);
                dying = lamp.AddComponent<Light>();
                dying.type = LightType.Point;
                dying.color = new Color(1f, 0.6f, 0.3f);
                dying.range = 5f;
                dying.intensity = 1.6f;
                dying.shadows = LightShadows.Soft;
            }
            if (withShape && shape == null)
            {
                shape = NightKit.Model("penitent_3", world);
                if (shape != null)
                {
                    shape.transform.position = ShapeAt;
                    Material dark = NightKit.Shadow();
                    if (dark != null)
                    {
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
            if (pool != null) { pool.SetActive(false); Object.Destroy(pool); }
            if (dying != null) Object.Destroy(dying.gameObject);
            knight = null; shape = null; pool = null; dying = null;
        }

        /// <summary>(21/11) Le Veilleur reste la ou il est mort (on repasse devant lui) : seule sa lumiere s'en va.</summary>
        static void Finish()
        {
            if (shape != null) { Object.Destroy(shape); shape = null; }
            if (dying != null) { Object.Destroy(dying.gameObject); dying = null; }
            NightKit.SnuffEntry();          // les torches du vestibule ne se rallument pas : la nuit commence dans le noir
            Playing = false; bars = 0f; black = 0f; line = "";
            HeldTorch.Raise = 1f;
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
            const float L = Dies - 5.6f;      // tout ce qui suit sa mort est decale d'autant
            bars = Mathf.MoveTowards(bars, t < 10.6f + L ? 1f : 0f, dt * 1.5f);

            if (t < Dies)
            {
                // Il nous parle : on est penche sur lui. La camera se resserre doucement.
                black = Mathf.MoveTowards(black, 0f, dt / 2f);
                float breathe = Mathf.Sin(t * 1.3f) * 1.2f;
                player.Face(Mathf.Sin(t * 0.4f) * 3f, -36f + breathe);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(72f, 54f, Mathf.Clamp01((t - 1f) / 14f));
                if (dying != null) dying.intensity = 1.6f * (0.8f + 0.2f * Mathf.PerlinNoise(t * 6f, 1f)) * Mathf.Clamp01((Dies - t) / 4f + 0.3f);
                for (int i = 0; i < Lines.Length; i++) if (t >= When[i]) line = Lines[i];
                // Il tend sa torche : elle monte dans ta main.
                if (t > 18f) HeldTorch.Raise = Mathf.Clamp01((t - 18f) / 2f);
                // Il tousse, il souffle.
                if (At(0, 1.2f) || At(1, 9.8f) || At(2, 15.8f)) NightAudio.Step(KnightAt + Vector3.up * 0.5f, 0.5f);
            }
            if (At(3, Dies))
            {
                // Il meurt. Sa torche tombee s'eteint.
                line = "";
                if (dying != null) dying.intensity = 0f;
                NightAudio.Sting(0.5f);
            }
            if (At(4, 5.6f + L))
            {
                // Le bruit, derriere nous.
                NightAudio.Bang(ShapeAt + Vector3.up, 1f);
            }
            if (t >= 5.85f + L && t < 6.5f + L)
            {
                // On se retourne d'un coup.
                float k = Mathf.Clamp01((t - 5.85f - L) / 0.55f);
                k = k * k * (3f - 2f * k);
                player.Face(180f * k, -36f + 26f * k);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(54f, 74f, k);
            }
            if (At(5, 6.5f + L)) NightAudio.Sting(1f);
            if (t >= 6.5f + L && t < 8.4f + L)
            {
                // (16/11) On ne voit RIEN. Seulement les torches qui meurent l'une apres l'autre, et le noir qui vient.
                player.Face(180f, -10f);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(74f, 56f, Mathf.Clamp01((t - 6.5f - L) / 1.9f));
            }
            if (At(6, 7.3f + L)) NightKit.SnuffEntryOne(0);
            if (At(7, 7.8f + L)) NightKit.SnuffEntryOne(1);
            if (At(8, 8.4f + L))
            {
                black = 1f;
                NightAudio.Bang(player.transform.position, 0.6f);
                player.Face(0f, 0f);
                if (cam != null) cam.fieldOfView = 72f;
            }
            if (t >= 9.9f + L) black = Mathf.MoveTowards(black, 0f, dt / 1.2f);
            if (t >= 11.6f + L) Finish();
        }

        /// <summary>Les bandes noires, le noir, et ce que dit le Veilleur.</summary>
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
            if (!Playing) return;
            if (line.Length > 0)
            {
                GUIStyle who = new GUIStyle(text);
                who.fontSize = Mathf.RoundToInt(h * 0.022f);
                who.fontStyle = FontStyle.Italic;
                who.normal.textColor = new Color(0.75f, 0.55f, 0.35f);
                NightFont.Label(new Rect(w * 0.1f, h * 0.76f, w * 0.8f, h * 0.04f), "Un Veilleur, à l'agonie", who);
                GUIStyle say = new GUIStyle(text);
                say.wordWrap = true;
                NightFont.Label(new Rect(w * 0.12f, h * 0.8f, w * 0.76f, h * 0.1f), line, say);
            }
            GUIStyle skip = new GUIStyle(text);
            skip.fontSize = Mathf.RoundToInt(h * 0.016f);
            skip.alignment = TextAnchor.LowerRight;
            skip.normal.textColor = new Color(0.6f, 0.55f, 0.5f, 0.6f);
            GUI.Label(new Rect(0f, 0f, w * 0.97f, h * 0.97f), "Entrée  —  passer", skip);
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
