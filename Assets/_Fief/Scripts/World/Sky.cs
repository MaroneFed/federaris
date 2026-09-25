using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE CIEL QUI TOURNE PENDANT LA SAISON.
    ///
    /// Trente minutes, c'est une fin d'apres-midi qui devient la nuit. On entre
    /// dans la sylve sous un ciel couvert, gris-vert ; vers le milieu de la Saison
    /// la lumiere jaunit et baisse ; dans les dernieres minutes il fait nuit, la
    /// brume devient bleue, et seules comptent la lanterne, les torches du chateau
    /// et les braseros. La cloche sonne a minuit.
    ///
    /// Le joueur n'a pas besoin de regarder l'horloge pour sentir que la fin
    /// approche : il la VOIT. C'est la Porte 1 -- "haletante du debut a la fin".
    ///
    /// ET UNE FOIS PAR SAISON, L'ORAGE. Deux minutes et demie de pluie, et des
    /// eclairs : pendant un dixieme de seconde la brume s'ouvre, et on voit la foret
    /// jusqu'a cent metres -- des centaines de troncs noirs qu'on ne soupconnait
    /// pas. Puis le noir, puis le tonnerre, une a trois secondes plus tard.
    ///
    /// On ne touche jamais aux reglages de GameConfig : on part des valeurs posees
    /// par Atmosphere au lancement, et on les fait glisser.
    /// </summary>
    public class Sky : MonoBehaviour
    {
        // --- le jour, lu au lancement
        Color dayHaze, daySky, dayEquator, dayGround, daySun;
        float dayDensity, daySunIntensity, dayElevation, dayFar;

        // --- l'orage
        float stormStart, stormEnd;
        bool stormAnnounced, stormOver;
        float stormLevel;               // 0 -> 1 : la pluie monte, puis retombe
        float nextFlash;
        float flash;                    // 0 -> 1 : l'eclair en cours
        float thunderIn = -1f;
        ParticleSystem rain;
        AudioSource rainSound;
        AudioSource thunderSound;

        Camera view;
        System.Random rng;

        const float StormLength = 150f;

        public static Sky Build(GameConfig cfg, Camera view, Transform player)
        {
            GameObject go = new GameObject("LE CIEL");
            Sky sky = go.AddComponent<Sky>();
            sky.view = view;
            sky.rng = new System.Random((cfg != null ? cfg.worldSeed : 1) * 37 + 1);

            sky.dayHaze = RenderSettings.fogColor;
            sky.dayDensity = RenderSettings.fogDensity;
            sky.daySky = RenderSettings.ambientSkyColor;
            sky.dayEquator = RenderSettings.ambientEquatorColor;
            sky.dayGround = RenderSettings.ambientGroundColor;
            sky.dayFar = view != null ? view.farClipPlane : 50f;
            if (Atmosphere.Sun != null)
            {
                sky.daySun = Atmosphere.Sun.color;
                sky.daySunIntensity = Atmosphere.Sun.intensity;
                sky.dayElevation = Atmosphere.Sun.transform.eulerAngles.x;
            }

            // L'orage : entre 9 et 17 minutes, deux minutes et demie.
            sky.stormStart = 540f + (float)sky.rng.NextDouble() * 480f;
            sky.stormEnd = sky.stormStart + StormLength;

            if (player != null) sky.rain = BuildRain(player);

            sky.rainSound = go.AddComponent<AudioSource>();
            sky.rainSound.clip = Sfx.Rain();
            sky.rainSound.loop = true;
            sky.rainSound.spatialBlend = 0f;
            sky.rainSound.volume = 0f;
            sky.rainSound.playOnAwake = false;

            sky.thunderSound = go.AddComponent<AudioSource>();
            sky.thunderSound.spatialBlend = 0f;
            sky.thunderSound.playOnAwake = false;
            return sky;
        }

        void Update()
        {
            if (Time.timeScale <= 0f)
            {
                if (rainSound.isPlaying) rainSound.Pause();
                return;
            }

            Season season = Game.Season;
            float progress = season != null && (season.Running || season.Over) ? season.Elapsed / season.Duration : 0f;
            float elapsed = season != null ? season.Elapsed : 0f;

            UpdateStorm(elapsed, season != null && season.Running);
            Apply(progress);
        }

        // ================================================================== la lumiere

        /// <summary>
        /// Trois moments : le jour (0), le crepuscule (0,6), la nuit (1). Entre deux,
        /// on melange. Pendant l'orage, tout s'assombrit un peu ; pendant un eclair,
        /// tout s'illumine et la brume recule.
        /// </summary>
        void Apply(float p)
        {
            float dusk = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.6f, p));
            float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.97f, p));

            Color duskHaze = new Color(0.19f, 0.16f, 0.14f);
            Color nightHaze = new Color(0.055f, 0.07f, 0.11f);
            Color haze = Color.Lerp(Color.Lerp(dayHaze, duskHaze, dusk), nightHaze, night);

            float ambient = Mathf.Lerp(1f, 0.42f, night) * Mathf.Lerp(1f, 0.9f, dusk);
            Color nightTint = new Color(0.75f, 0.85f, 1.25f);
            Color tint = Color.Lerp(Color.white, nightTint, night);

            Color sunColor = Color.Lerp(Color.Lerp(daySun, new Color(1f, 0.72f, 0.48f), dusk),
                                        new Color(0.55f, 0.66f, 0.95f), night);
            float sunIntensity = daySunIntensity * Mathf.Lerp(1f, 0.8f, dusk) * Mathf.Lerp(1f, 0.4f, night);
            float elevation = Mathf.Lerp(dayElevation, 34f, dusk);

            // L'orage : plus sombre, plus bleu, la brume un peu plus epaisse.
            float storm = stormLevel;
            haze = Color.Lerp(haze, haze * new Color(0.7f, 0.75f, 0.85f), storm);
            ambient *= Mathf.Lerp(1f, 0.75f, storm);
            float density = dayDensity * Mathf.Lerp(1f, 1.12f, night) * Mathf.Lerp(1f, 1.15f, storm);

            // L'eclair : un instant, tout est blanc-bleu, et la brume recule.
            if (flash > 0f)
            {
                haze = Color.Lerp(haze, new Color(0.55f, 0.6f, 0.7f), flash * 0.7f);
                ambient = Mathf.Lerp(ambient, 2.2f, flash);
                sunColor = Color.Lerp(sunColor, new Color(0.85f, 0.9f, 1f), flash);
                sunIntensity = Mathf.Lerp(sunIntensity, 2.6f, flash);
                density *= Mathf.Lerp(1f, 0.28f, flash);
            }

            // EN HAUTEUR, LA BRUME S'OUVRE (26/09) : du haut d'un geant ou de la terrasse
            // du donjon, on voit loin. Entre 8 et 24 m au-dessus du sol, la brume
            // s'eclaircit jusqu'a un cinquieme de son epaisseur, et le plan lointain
            // recule d'autant.
            float clarity = 0f;
            if (view != null)
            {
                Vector3 eye = view.transform.position;
                clarity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 24f, eye.y - Ground.Sample(eye.x, eye.z)));
            }
            density *= Mathf.Lerp(1f, 0.2f, clarity);

            RenderSettings.fogColor = haze;
            RenderSettings.fogDensity = density;
            RenderSettings.ambientSkyColor = Scale(daySky * tint, ambient);
            RenderSettings.ambientEquatorColor = Scale(dayEquator * tint, ambient);
            RenderSettings.ambientGroundColor = Scale(dayGround, ambient);

            if (view != null)
            {
                view.backgroundColor = haze;
                view.farClipPlane = dayFar * (1f + flash * 1.8f) * Mathf.Lerp(1f, 5f, clarity);
            }
            if (Atmosphere.Sun != null)
            {
                Atmosphere.Sun.color = sunColor;
                Atmosphere.Sun.intensity = sunIntensity;
                Atmosphere.Sun.transform.rotation = Quaternion.Euler(elevation, 38f, 0f);
            }
        }

        static Color Scale(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        // ================================================================== l'orage

        void UpdateStorm(float elapsed, bool running)
        {
            float dt = Time.deltaTime;
            bool inStorm = running && elapsed >= stormStart && elapsed < stormEnd;

            if (inStorm && !stormAnnounced)
            {
                stormAnnounced = true;
                nextFlash = 6f;
            }
            if (!inStorm && stormAnnounced && !stormOver && elapsed >= stormEnd)
            {
                stormOver = true;
            }

            stormLevel = Mathf.MoveTowards(stormLevel, inStorm ? 1f : 0f, dt / 12f);

            // La pluie et son bruit suivent le niveau de l'orage.
            if (rain != null)
            {
                ParticleSystem.EmissionModule emission = rain.emission;
                emission.rateOverTime = 1500f * stormLevel;
                if (stormLevel > 0.01f && !rain.isPlaying) rain.Play();
            }
            rainSound.volume = Sfx.Muted ? 0f : 0.55f * stormLevel;
            if (stormLevel > 0.01f && !rainSound.isPlaying) rainSound.Play();
            else if (stormLevel <= 0.01f && rainSound.isPlaying) rainSound.Stop();

            // Les eclairs : un double eclat, puis le tonnerre.
            flash = Mathf.MoveTowards(flash, 0f, dt * 7f);
            if (inStorm && stormLevel > 0.5f)
            {
                nextFlash -= dt;
                if (nextFlash <= 0f)
                {
                    flash = 1f;
                    nextFlash = 9f + (float)rng.NextDouble() * 16f;
                    thunderIn = 0.9f + (float)rng.NextDouble() * 2.2f;
                    secondFlash = 0.16f;
                }
            }
            if (secondFlash > 0f)
            {
                secondFlash -= dt;
                if (secondFlash <= 0f) flash = Mathf.Max(flash, 0.8f);
            }
            if (thunderIn > 0f)
            {
                thunderIn -= dt;
                if (thunderIn <= 0f && !Sfx.Muted)
                {
                    thunderSound.pitch = 0.85f + (float)rng.NextDouble() * 0.3f;
                    thunderSound.PlayOneShot(Sfx.Thunder(), 1f);
                }
            }
        }

        float secondFlash;

        /// <summary>
        /// La pluie : des gouttes etirees dans le sens de leur chute (mode "Stretch"),
        /// qui meurent en touchant quoi que ce soit (toits, sol, feuillage) -- sinon
        /// il pleuvrait dans la salle du trone.
        /// </summary>
        static ParticleSystem BuildRain(Transform player)
        {
            ParticleSystem ps = Ambiance.RainSystem(player);
            return ps;
        }
    }
}
