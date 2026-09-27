using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES REGLAGES DU JOUEUR (27/09) : sensibilite de la souris, volume, champ de
    /// vision, plein ecran, et les touches de la capacite et de la poussee. Rien a voir avec la partie : ce sont les preferences de
    /// celui qui tient la souris.
    ///
    /// Concept Unity : PlayerPrefs est un petit carnet cle -> valeur qu'Unity garde
    /// sur l'ordinateur (dans le registre sous Windows). Parfait pour des reglages ;
    /// jamais pour l'etat d'une partie (FIEF n'a aucune persistance de partie).
    /// </summary>
    public static class Settings
    {
        public static float Sensitivity = 1f;    // 0,3 a 2,5 : multiplie la sensibilite de base
        public static float Volume = 0.8f;       // 0 a 1
        public static float Fov = 78f;           // 65 a 100 degres
        public static float TextSize = 1f;       // 0,8 a 1,5 : la taille de tout le texte a l'ecran
        public static bool Fullscreen = true;
        /// <summary>
        /// LES TOUCHES A SOI (30/09 -- Martin : "un parametre qui permet de changer cette
        /// touche-la"). Ta capacite active et la poussee se mettent ou l'on veut, parmi
        /// FiefInput.BindNames (les trois clics, F, R, X, V). Par defaut : capacite au clic
        /// gauche, pousser au clic droit. Jamais les deux sur la meme touche.
        /// </summary>
        public static int ActiveBind = 0;
        public static int PushBind = 1;
        /// <summary>TON PSEUDO (29/09 : "il faut mettre le pseudo, les persos c'est quasi tous les memes").</summary>
        public static string Pseudo = "Joueur";
        public const int PseudoLength = 16;

        /// <summary>Changer de pseudo (nettoye : pas vide, pas trop long) et le garder.</summary>
        public static void SetPseudo(string name)
        {
            Load();
            name = name ?? "";
            if (name.Length > PseudoLength) name = name.Substring(0, PseudoLength);
            Pseudo = name;
            Save();
            if (Match.Local != null) Match.Local.Name = Shown;
        }

        /// <summary>Le pseudo tel qu'on l'affiche (jamais vide).</summary>
        public static string Shown { get { string p = (Pseudo ?? "").Trim(); return p.Length > 0 ? p : "Joueur"; } }

        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            Sensitivity = PlayerPrefs.GetFloat("fief.sensibilite", 1f);
            Volume = PlayerPrefs.GetFloat("fief.volume", 0.8f);
            Fov = PlayerPrefs.GetFloat("fief.fov", 78f);
            TextSize = PlayerPrefs.GetFloat("fief.texte", 1f);
            Fullscreen = PlayerPrefs.GetInt("fief.pleinecran", Screen.fullScreen ? 1 : 0) == 1;
            bool oldLeft = PlayerPrefs.GetInt("fief.pousserGauche", 0) == 1;
            ActiveBind = Mathf.Clamp(PlayerPrefs.GetInt("fief.toucheCapacite", oldLeft ? 1 : 0), 0, FiefInput.BindNames.Length - 1);
            PushBind = Mathf.Clamp(PlayerPrefs.GetInt("fief.touchePousser", oldLeft ? 0 : 1), 0, FiefInput.BindNames.Length - 1);
            if (PushBind == ActiveBind) { ActiveBind = 0; PushBind = 1; }
            Pseudo = PlayerPrefs.GetString("fief.pseudo", "Joueur");
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("fief.sensibilite", Sensitivity);
            PlayerPrefs.SetFloat("fief.volume", Volume);
            PlayerPrefs.SetFloat("fief.fov", Fov);
            PlayerPrefs.SetFloat("fief.texte", TextSize);
            PlayerPrefs.SetInt("fief.pleinecran", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("fief.toucheCapacite", ActiveBind);
            PlayerPrefs.SetInt("fief.touchePousser", PushBind);
            PlayerPrefs.SetString("fief.pseudo", Pseudo);
            PlayerPrefs.Save();
        }

        /// <summary>Appliquer tout de suite (au lancement de chaque manche, et a chaque changement).</summary>
        public static void Apply()
        {
            Load();
            AudioListener.volume = Volume;
            if (Game.Config != null) Game.Config.mouseSensitivity = 0.13f * Sensitivity;
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.baseFieldOfView = Fov;
            if (Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
        }

        /// <summary>Changer le reglage "row" d'un cran (-1 / +1). Les lignes : sensibilite, volume, champ de vision, taille du texte, plein ecran.</summary>
        public static void Step(int row, int step)
        {
            Load();
            if (row == 0) Sensitivity = Mathf.Clamp(Mathf.Round((Sensitivity + step * 0.1f) * 10f) / 10f, 0.3f, 2.5f);
            else if (row == 1) Volume = Mathf.Clamp(Mathf.Round((Volume + step * 0.1f) * 10f) / 10f, 0f, 1f);
            else if (row == 2) Fov = Mathf.Clamp(Fov + step * 5f, 65f, 100f);
            else if (row == 3) TextSize = Mathf.Clamp(Mathf.Round((TextSize + step * 0.1f) * 10f) / 10f, 0.8f, 1.5f);
            else if (row == 4) Fullscreen = !Fullscreen;
            else if (row == 5) ActiveBind = NextBind(ActiveBind, step, PushBind);
            else if (row == 6) PushBind = NextBind(PushBind, step, ActiveBind);
            Apply();
            Save();
        }

        public static string Value(int row)
        {
            Load();
            if (row == 0) return Sensitivity.ToString("0.0");
            if (row == 1) return Mathf.RoundToInt(Volume * 100f) + " %";
            if (row == 2) return Mathf.RoundToInt(Fov) + "°";
            if (row == 3) return Mathf.RoundToInt(TextSize * 100f) + " %";
            if (row == 4) return Fullscreen ? "oui" : "non";
            if (row == 5) return FiefInput.BindNames[ActiveBind];
            return FiefInput.BindNames[PushBind];
        }

        /// <summary>La touche suivante (ou precedente) de la liste, en sautant celle deja prise par l'autre.</summary>
        static int NextBind(int from, int step, int taken)
        {
            int n = FiefInput.BindNames.Length;
            int to = from;
            for (int i = 0; i < n; i++)
            {
                to = (to + (step < 0 ? -1 : 1) + n) % n;
                if (to != taken) return to;
            }
            return from;
        }

        public static readonly string[] Labels = { "Sensibilité", "Volume", "Champ de vision", "Taille du texte", "Plein écran", "Touche capacité", "Touche pousser" };
    }
}
