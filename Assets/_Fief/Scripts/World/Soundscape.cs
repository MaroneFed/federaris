using UnityEngine;

namespace Fief
{
    /// <summary>
    /// CE QU'ON ENTEND DANS LA SYLVE. Le vent en continu, et de temps en temps,
    /// quelque part autour de toi : un hibou, une branche qui casse, un grand arbre
    /// qui grince, et -- rarement -- un loup, tres loin.
    ///
    /// Tous ces sons sont joues DEPUIS UN POINT DE L'ESPACE (AudioSource "3D") : la
    /// branche casse derriere toi, a gauche. On se retourne. Il n'y a rien. C'est
    /// exactement l'effet recherche : inquietant, jamais horrible.
    ///
    /// ET LA CLOCHE DU CHATEAU sonne toutes les cinq minutes de la Saison : un coup a
    /// 5:00, deux a 10:00... Elle sonne DEPUIS le chateau : on l'entend de toute la
    /// foret, et on sait de quel cote il est. C'est une horloge et une boussole.
    ///
    /// Concept Unity : un AudioSource est un haut-parleur pose dans le monde. On le
    /// deplace, puis on lui fait jouer un clip avec PlayOneShot. L'AudioListener
    /// (l'oreille, sur la camera) entend chaque haut-parleur selon sa distance et
    /// sa direction.
    /// </summary>
    public class Soundscape : MonoBehaviour
    {
        AudioSource wind;
        AudioSource[] voices = new AudioSource[3];
        AudioSource bellTower;
        int nextVoice;

        float owlTimer;
        float crackTimer;
        float creakTimer;
        float howlTimer;
        int bellsRung;
        int tollsLeft;
        float tollTimer;
        System.Random rng = new System.Random(4242);

        public static Soundscape Build(Transform parent)
        {
            GameObject go = new GameObject("SONS DE LA SYLVE");
            go.transform.SetParent(parent, false);
            Soundscape s = go.AddComponent<Soundscape>();

            // Le vent : partout, sans direction (spatialBlend 0), en boucle.
            s.wind = go.AddComponent<AudioSource>();
            s.wind.clip = Sfx.Wind();
            s.wind.loop = true;
            s.wind.spatialBlend = 0f;
            s.wind.volume = 0.3f;
            s.wind.playOnAwake = false;
            s.wind.Play();

            // Trois voix mobiles pour les bruits de la foret.
            for (int i = 0; i < s.voices.Length; i++)
            {
                GameObject v = new GameObject("Voix " + i);
                v.transform.SetParent(go.transform, false);
                AudioSource a = v.AddComponent<AudioSource>();
                a.spatialBlend = 1f;
                a.rolloffMode = AudioRolloffMode.Logarithmic;
                a.minDistance = 6f;
                a.maxDistance = 120f;
                a.dopplerLevel = 0f;
                a.playOnAwake = false;
                s.voices[i] = a;
            }

            // La cloche, en haut du donjon.
            GameObject tower = new GameObject("Cloche du château");
            tower.transform.SetParent(go.transform, false);
            tower.transform.position = Game.CastleCentre + new Vector3(0f, 30f, Castle.KeepCentre.z);
            s.bellTower = tower.AddComponent<AudioSource>();
            s.bellTower.clip = Sfx.BellClip();
            s.bellTower.spatialBlend = 1f;
            s.bellTower.rolloffMode = AudioRolloffMode.Logarithmic;
            s.bellTower.minDistance = 40f;
            s.bellTower.maxDistance = 600f;
            s.bellTower.dopplerLevel = 0f;
            s.bellTower.playOnAwake = false;

            s.owlTimer = 20f;
            s.crackTimer = 12f;
            s.creakTimer = 30f;
            s.howlTimer = 240f;
            return s;
        }

        void Update()
        {
            // Pause : tout se tait, le vent aussi.
            bool paused = Time.timeScale <= 0f;
            if (paused) { if (wind.isPlaying) wind.Pause(); return; }
            if (!wind.isPlaying) wind.UnPause();

            float dt = Time.deltaTime;
            float t = Time.time;
            // Les rafales : le vent enfle et retombe, lentement.
            wind.volume = Sfx.Muted ? 0f : 0.22f + 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 0.21f) * Mathf.Sin(t * 0.083f + 2f));

            Season season = Game.Season;
            Transform player = Game.PlayerTransform;
            if (season == null || !season.Running || player == null) return;

            owlTimer -= dt;
            crackTimer -= dt;
            creakTimer -= dt;
            howlTimer -= dt;

            if (owlTimer <= 0f) { Around(player, Sfx.Owl(), 18f, 40f, 9f, 0.55f); owlTimer = Range(28f, 65f); }
            if (crackTimer <= 0f) { Around(player, Sfx.Crack(), 10f, 26f, 0.4f, 0.8f); crackTimer = Range(18f, 45f); }
            if (creakTimer <= 0f) { Around(player, Sfx.Creak(), 12f, 30f, 7f, 0.55f); creakTimer = Range(22f, 55f); }
            if (howlTimer <= 0f) { Around(player, Sfx.Howl(), 70f, 110f, 2f, 0.9f); howlTimer = Range(300f, 480f); }

            Bells(season, dt);
        }

        /// <summary>Un son joue depuis un point au hasard autour du joueur.</summary>
        void Around(Transform player, AudioClip clip, float near, float far, float height, float volume)
        {
            if (clip == null || Sfx.Muted) return;
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float d = Range(near, far);
            Vector3 p = player.position + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
            p.y = Ground.Sample(p.x, p.z) + height;

            AudioSource v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.transform.position = p;
            v.pitch = Range(0.92f, 1.08f);
            v.PlayOneShot(clip, volume);
        }

        /// <summary>Un coup de cloche par tranche de cinq minutes, espaces de 2,4 s.</summary>
        void Bells(Season season, float dt)
        {
            int due = Mathf.FloorToInt(season.Elapsed / 300f);
            if (due > bellsRung && season.Remaining > 5f)
            {
                bellsRung = due;
                tollsLeft = due;
                tollTimer = 0f;
                int minutesLeft = Mathf.RoundToInt(season.Remaining / 60f);
                Toasts.Show(due + (due > 1 ? " coups" : " coup") + " de cloche au château. Encore " + minutesLeft + " minutes.",
                            new Color(0.86f, 0.80f, 0.64f));
            }

            if (tollsLeft <= 0) return;
            tollTimer -= dt;
            if (tollTimer > 0f) return;
            tollTimer = 2.4f;
            tollsLeft--;
            if (!Sfx.Muted) bellTower.PlayOneShot(bellTower.clip, 1f);
        }

        float Range(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
