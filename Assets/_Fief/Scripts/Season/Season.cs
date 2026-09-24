using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'HORLOGE DE LA SAISON, et l'agenda du mage.
    ///
    /// Classe C# pure : elle ne connait que le temps ecoule. Tout le reste -- ou est
    /// le mage, combien de temps il reste -- se DEDUIT de ce seul nombre. Il n'y a
    /// donc aucun etat a synchroniser : en Phase 3, le serveur envoie l'heure, et
    /// chaque machine en tire exactement les memes apparitions.
    ///
    /// L'AGENDA (reglable dans GameConfig, section "La Saison") :
    ///
    ///     premiere apparition     2:00
    ///     puis toutes les         4:30
    ///     pendant                 2:30
    ///
    /// Une apparition qui deborderait sur la cloche n'a pas lieu : le mage ne
    /// disparait jamais en plein milieu parce que la Saison s'acheve.
    /// </summary>
    public class Season
    {
        public float Duration = 1800f;
        public float FirstMage = 120f;
        public float MageInterval = 270f;
        public float MageStay = 150f;

        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public bool Over { get; private set; }

        public Season(GameConfig cfg)
        {
            if (cfg == null) return;
            Duration = Mathf.Max(60f, cfg.seasonMinutes * 60f);
            FirstMage = Mathf.Max(0f, cfg.mageFirstAppearance);
            MageInterval = Mathf.Max(10f, cfg.mageInterval);
            MageStay = Mathf.Clamp(cfg.mageStay, 5f, MageInterval);
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

        /// <summary>Debut de la k-ieme apparition, en secondes depuis le debut.</summary>
        public float AppearanceStart(int k)
        {
            return FirstMage + k * MageInterval;
        }

        /// <summary>Une apparition n'a lieu que si elle se termine avant la cloche.</summary>
        public bool AppearanceHappens(int k)
        {
            return k >= 0 && AppearanceStart(k) + MageStay <= Duration + 0.01f;
        }

        public int AppearanceCount
        {
            get
            {
                int k = 0;
                while (AppearanceHappens(k)) k++;
                return k;
            }
        }

        /// <summary>Index de l'apparition en cours, ou -1 si le mage n'est pas la.</summary>
        public int CurrentAppearance
        {
            get
            {
                if (!Running) return -1;
                float t = Elapsed - FirstMage;
                if (t < 0f) return -1;
                int k = Mathf.FloorToInt(t / MageInterval);
                if (!AppearanceHappens(k)) return -1;
                return Elapsed < AppearanceStart(k) + MageStay ? k : -1;
            }
        }

        public bool MagePresent
        {
            get { return CurrentAppearance >= 0; }
        }

        /// <summary>Temps avant que le mage ne s'en aille (0 s'il n'est pas la).</summary>
        public float MageTimeLeft
        {
            get
            {
                int k = CurrentAppearance;
                return k < 0 ? 0f : AppearanceStart(k) + MageStay - Elapsed;
            }
        }

        /// <summary>Temps avant la prochaine apparition, ou -1 s'il n'y en aura plus.</summary>
        public float NextMageIn
        {
            get
            {
                for (int k = 0; AppearanceHappens(k); k++)
                {
                    float start = AppearanceStart(k);
                    if (start > Elapsed) return start - Elapsed;
                }
                return -1f;
            }
        }
    }
}
