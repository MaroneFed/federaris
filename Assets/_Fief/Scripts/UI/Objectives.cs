using UnityEngine;

namespace Fief
{
    /// <summary>
    /// "QUE DOIS-JE FAIRE, LA, MAINTENANT ?" -- la question a laquelle le jeu ne
    /// repondait pas (Martin : "on comprend rien au jeu").
    ///
    /// Un encart en haut a droite. D'abord, six PREMIERS PAS, dans l'ordre ; chacun
    /// se coche tout seul quand on l'a fait, avec un petit son. Ensuite, une seule
    /// ligne qui change selon la situation : le mage chante, ta relique est en
    /// main, un voleur court avec elle, la cloche approche...
    ///
    /// Il ne decide rien : il LIT l'etat du jeu (sac, Hoard, Saison) et le dit en
    /// francais.
    /// </summary>
    public static class Objectives
    {
        struct Step
        {
            public string text;
            public string hint;
        }

        static readonly Step[] Steps =
        {
            new Step { text = "Ramasse du bois mort",
                       hint = "Au pied des arbres morts (gris, sans feuilles) : des fagots. Maintiens E." },
            new Step { text = "Plante ta stele (P)",
                       hint = "Loin des chemins, dans un fourre. Elle chante doucement : on peut la trouver a l'oreille." },
            new Step { text = "Ramasse de la pierre-lune",
                       hint = "Dans les creux ou des pierres bleues luisent. Suis les lucioles, ou un feu-follet." },
            new Step { text = "Porte ton sac au mage",
                       hint = "Quand une colonne bleue monte au-dessus des arbres, cours-y. Il fond ce que tu portes." },
            new Step { text = "Pose ta relique sur ta stele",
                       hint = "E devant ta stele. Seule une relique posee compte a la cloche." },
            new Step { text = "Choisis ton chemin vers la victoire",
                       hint = "Tab : l'onglet Victoires montre les quatre facons de gagner." }
        };

        static int done;
        static float flash;
        static bool sawVictories;

        public static void Reset()
        {
            done = 0;
            flash = 0f;
            sawVictories = false;
        }

        /// <summary>Appele quand on ouvre la besace : la derniere etape est franchie.</summary>
        public static void VictoriesSeen() { sawVictories = true; }

        static bool Completed(int step)
        {
            Hoard h = Game.Hoard;
            Inventory bag = Game.Inventory;
            if (h == null || bag == null) return false;
            switch (step)
            {
                case 0: return bag.Get(ResourceType.Deadwood) >= 4 || h.Relic != null;
                case 1: return h.StelePlanted;
                case 2: return bag.Get(ResourceType.Moonstone) >= 2 || h.Relic != null;
                case 3: return h.Relic != null;
                case 4: return h.RelicOnStele;
                default: return sawVictories;
            }
        }

        public static void Draw()
        {
            Season season = Game.Season;
            if (season == null || !season.Running) return;

            // On avance d'une etape des qu'elle est faite (plusieurs d'un coup si besoin).
            while (done < Steps.Length && Completed(done))
            {
                done++;
                flash = 1f;
                Sfx.Pop();
            }
            if (flash > 0f) flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.5f);

            float w = UiStyle.S(330);
            float x = Screen.width - w - UiStyle.S(18);
            float y = UiStyle.S(18);

            if (done < Steps.Length)
            {
                float h = UiStyle.S(78) + UiStyle.S(24) * Steps.Length + UiStyle.S(48);
                Rect box = new Rect(x, y, w, h);
                UiStyle.Frame(box);
                float ix = x + UiStyle.S(18);
                float iy = y + UiStyle.S(14);
                UiStyle.Tinted(new Rect(ix, iy, w, UiStyle.S(24)), UiStyle.Spaced("PREMIERS PAS"), UiStyle.Head, Palette.Gold);
                iy += UiStyle.S(30);
                UiStyle.Rule(new Rect(ix, iy, w - UiStyle.S(36), UiStyle.S(6)));
                iy += UiStyle.S(12);

                for (int i = 0; i < Steps.Length; i++)
                {
                    bool finished = i < done;
                    bool current = i == done;
                    float d = UiStyle.S(9);
                    Color mark = finished ? new Color(0.62f, 0.86f, 0.48f) : current ? Palette.Gold : UiStyle.InkFaint;
                    UiStyle.Icon(new Rect(ix, iy + UiStyle.S(7), d, d), finished ? UiStyle.Shape.Diamond : UiStyle.Shape.Dot, mark);
                    Color text = finished ? UiStyle.InkFaint : current ? UiStyle.Ink : UiStyle.InkDim;
                    UiStyle.Tinted(new Rect(ix + UiStyle.S(18), iy, w - UiStyle.S(40), UiStyle.S(22)), Steps[i].text,
                                   current ? UiStyle.Label : UiStyle.Small, text);
                    if (finished) UiStyle.Fill(new Rect(ix + UiStyle.S(18), iy + UiStyle.S(11), UiStyle.S(8) * Steps[i].text.Length * 0.9f, 1f),
                                               new Color(1f, 1f, 1f, 0.25f));
                    iy += UiStyle.S(24);
                }

                // L'indice de l'etape en cours, en clair.
                GUIStyle hint = UiStyle.Tiny;
                bool wrap = hint.wordWrap;
                hint.wordWrap = true;
                Color glow = Color.Lerp(UiStyle.InkDim, Palette.Gold, flash);
                UiStyle.Tinted(new Rect(ix, iy + UiStyle.S(4), w - UiStyle.S(36), UiStyle.S(40)), Steps[done].hint, hint, glow);
                hint.wordWrap = wrap;
                return;
            }

            // Apres les premiers pas : une seule ligne, la plus urgente.
            string now = Urgent(season);
            if (string.IsNullOrEmpty(now)) return;
            Rect line = new Rect(x, y, w, UiStyle.S(52));
            GUI.Box(line, GUIContent.none, UiStyle.CardBox);
            UiStyle.Tinted(new Rect(line.x + UiStyle.S(14), line.y + UiStyle.S(4), w, UiStyle.S(18)), "MAINTENANT", UiStyle.Tiny, Palette.Gold);
            GUIStyle body = UiStyle.Small;
            bool w2 = body.wordWrap;
            body.wordWrap = true;
            UiStyle.Tinted(new Rect(line.x + UiStyle.S(14), line.y + UiStyle.S(20), w - UiStyle.S(28), UiStyle.S(30)), now, body, UiStyle.Ink);
            body.wordWrap = w2;
        }

        /// <summary>La chose la plus urgente, dite en une phrase.</summary>
        static string Urgent(Season season)
        {
            Hoard h = Game.Hoard;
            if (h == null) return "";
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r != null && r.seeker.Hoard.Trophy != null && r.seeker.Hoard.TrophyFrom == Game.Me)
                    return r.seeker.Name + " emporte ta relique ! Rattrape-le (losange rouge sur la boussole), puis E.";
            }
            if (h.Trophy != null) return "Tu portes la relique de " + h.TrophyFrom.Name + " : cours a ta stele pour la fondre.";
            if (season.Remaining < 120f && h.RelicInHand) return "La cloche approche : pose ta relique sur ta stele, vite.";
            if (season.MagePresent && !Game.Inventory.IsEmpty) return "Le mage chante (" + Hud.Clock(season.MageTimeLeft) + ") : porte-lui ton sac.";
            if (h.RelicInHand) return "Ta relique est en main : pose-la sur ta stele (E), sinon elle ne compte pas.";
            if (season.NextMageIn >= 0f && season.NextMageIn < 40f) return "Le mage arrive dans " + Hud.Clock(season.NextMageIn) + " : prepare ton sac.";
            if (season.NextMageIn >= 0f) return "Prochain mage dans " + Hud.Clock(season.NextMageIn) + ". Remplis ton sac, ou avance sur une autre victoire (Tab).";
            return "Le mage ne reviendra plus. Defends ta stele jusqu'a la cloche.";
        }
    }
}
