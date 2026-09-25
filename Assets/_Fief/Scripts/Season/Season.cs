using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'HORLOGE DE LA MANCHE : sa duree vient du match (4 a 10 min, choisie au salon).
    ///
    /// Classe C# pure : elle ne connait que le temps ecoule. En Phase 3, le serveur
    /// envoie l'heure, et chaque machine en tire exactement la meme chose.
    ///
    /// Elle ne part qu'au "PARTEZ !" du compte a rebours (Menus) : l'intro et le
    /// decompte ne mangent pas le temps de la manche.
    /// </summary>
    public class Season
    {
        public float Duration = 1800f;

        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public bool Over { get; private set; }

        public Season(GameConfig cfg)
        {
            if (Match.Active) Duration = Match.RoundSeconds;
            else if (cfg != null) Duration = Mathf.Max(60f, cfg.seasonMinutes * 60f);
        }

        /// <summary>Arreter l'horloge tout de suite : la manche est gagnee.</summary>
        public void Stop()
        {
            Running = false;
            Over = true;
        }

        public void Begin()
        {
            if (!Over) Running = true;
        }

        public void Tick(float deltaTime)
        {
            if (!Running || Over || deltaTime <= 0f) return;
            Elapsed = Mathf.Min(Duration, Elapsed + deltaTime);
            if (Elapsed >= Duration)
            {
                Over = true;
                Running = false;
            }
        }

        public float Remaining
        {
            get { return Mathf.Max(0f, Duration - Elapsed); }
        }
    }
}
