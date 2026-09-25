using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA MALEDICTION, dans le monde (la regle, elle, est dans Season et Hoard).
    ///
    /// Un moment apres chaque depart du mage, la foret devore ce que chacun porte
    /// dans son sac -- toi ET les rivaux. Ce qui est pose dans ta stele, enterre
    /// dans une cache, ta relique, ton or, tes outils : rien de tout ca ne craint.
    ///
    /// Pourquoi c'est bon pour le jeu (Martin, 25/09) : ca FORCE a rentrer a sa
    /// stele plusieurs fois par Saison. Une stele pleine, c'est une stele qu'on
    /// vient piller ; la garder, c'est ne plus recolter. Il faut choisir.
    ///
    /// On l'entend venir : un glas grave une minute avant, puis a trente et a dix
    /// secondes. L'horloge du haut affiche le compte a rebours.
    /// </summary>
    public class Curse : MonoBehaviour
    {
        public static readonly Color Violet = new Color(0.66f, 0.42f, 0.92f);

        int applied;
        float lastWarned = -1f;
        float flash;

        public static Curse Build()
        {
            GameObject go = new GameObject("MALÉDICTION");
            return go.AddComponent<Curse>();
        }

        void Update()
        {
            Season season = Game.Season;
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 0.6f);
            if (season == null || !season.Running) return;

            // Les avertissements : a 60, 30 et 10 secondes.
            float next = season.NextCurseIn;
            if (next >= 0f)
            {
                float[] marks = { 60f, 30f, 10f };
                for (int i = 0; i < marks.Length; i++)
                {
                    if (next <= marks[i] && (lastWarned < 0f || lastWarned > marks[i]))
                    {
                        lastWarned = marks[i];
                        Sfx.CurseToll();
                        if (marks[i] >= 60f && Game.Inventory != null && !Game.Inventory.IsEmpty)
                            Toasts.Show("Le glas. Rentre à ta stèle.", Violet);
                    }
                }
            }

            // Elle tombe.
            int passed = season.CursesPassed;
            while (applied < passed)
            {
                applied++;
                lastWarned = -1f;
                Strike();
            }
        }

        void Strike()
        {
            for (int i = 0; i < Game.Seekers.Count; i++)
            {
                Seeker s = Game.Seekers[i];
                if (s == null || !s.Alive) continue;
                int[] lost = s.Hoard.RequestCurse(s.Bag);
                s.SyncWeight();
                if (s.IsPlayer) Tell(lost);
            }
            Sfx.CurseStrike();
            flash = 1f;
            if (Game.Hud != null && Game.Hud.orbitCamera != null) Game.Hud.orbitCamera.Shake(0.35f);
        }

        static void Tell(int[] lost)
        {
            int total = 0;
            string what = "";
            for (int i = 0; i < lost.Length; i++)
            {
                if (lost[i] <= 0) continue;
                total += lost[i];
                if (what.Length > 0) what += ", ";
                what += lost[i] + " " + ResourceInfo.Name((ResourceType)i);
            }
            Stats.CurseLost += total;
            if (Game.Hud == null) return;
            if (total == 0)
            {
                Sfx.Discovery();
                Game.Hud.ShowDiscovery("LA MALÉDICTION", "repart bredouille",
                                       "Ton sac était vide. Bien joue.",
                                       "Ta réserve est intacte : " + Stele.StoreSummary(Game.Hoard) + ".", Violet);
            }
            else
                Game.Hud.ShowDiscovery("LA MALÉDICTION", "a dévoré ton sac",
                                       "Perdu : " + what + ".",
                                       "La prochaine fois, dépose tout à ta stèle avant le glas.", Violet);
        }

        void OnGUI()
        {
            if (flash <= 0f) return;
            GUI.depth = -10;
            UiStyle.Ensure();
            // Un voile violet qui se retire par le centre : les bords restent sombres
            // plus longtemps, comme une ombre qui se referme.
            UiStyle.Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.2f, 0.05f, 0.3f, flash * 0.45f));
            float edge = Screen.height * 0.18f;
            Color dark = new Color(0.05f, 0f, 0.08f, flash * 0.6f);
            UiStyle.Fill(new Rect(0f, 0f, Screen.width, edge * flash), dark);
            UiStyle.Fill(new Rect(0f, Screen.height - edge * flash, Screen.width, edge * flash), dark);
        }
    }
}
