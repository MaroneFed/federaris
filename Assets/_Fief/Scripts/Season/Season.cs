using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'HORLOGE DE LA SAISON : trente minutes, puis la cloche.
    ///
    /// Classe C# pure : elle ne connait que le temps ecoule. En Phase 3, le serveur
    /// envoie l'heure, et chaque machine en tire exactement la meme chose.
    ///
    /// (Le mage, sa colonne et la Malediction qui suivait ses departs ont ete
    /// retires le 26/09 : Martin ne comprenait plus pourquoi on recoltait. Il n'y a
    /// plus qu'un chronometre, et un seul but : le butin.)
    /// </summary>
    public class Season
    {
        public float Duration = 1800f;

        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public bool Over { get; private set; }

        public Season(GameConfig cfg)
        {
            if (cfg == null) return;
            Duration = Mathf.Max(60f, cfg.seasonMinutes * 60f);
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
