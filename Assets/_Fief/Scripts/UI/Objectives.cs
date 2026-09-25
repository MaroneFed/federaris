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

        // Des mots, pas des paragraphes (Martin, 26/09 : "il y a trop de texte").
        static readonly Step[] Steps =
        {
            new Step { text = "Ramasse du bois mort",         hint = "Arbres gris  ·  maintiens E" },
            new Step { text = "Dépose-le à ta stèle",         hint = "Le fil d'or  ·  M : la carte" },
            new Step { text = "Trouve de la pierre-lune",     hint = "Dans les creux qui luisent" },
            new Step { text = "Porte ton sac au mage",        hint = "Sous la colonne bleue" },
            new Step { text = "Pose ta relique sur ta stèle", hint = "Seule elle compte à la cloche" },
            new Step { text = "Achète une amélioration",      hint = "À ta stèle" }
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
                case 0: return bag.Get(ResourceType.Deadwood) >= 4 || Stored(h) > 0 || h.Relic != null;
                case 1: return Stored(h) > 0 || h.Relic != null;
                case 2: return bag.Get(ResourceType.Moonstone) >= 2 || h.Store != null && h.Store.Contents.Get(ResourceType.Moonstone) >= 2 || h.Relic != null;
                case 3: return h.Relic != null;
                case 4: return h.RelicOnStele;
                default: return AnyUpgrade(h) || sawVictories;
            }
        }

        static int Stored(Hoard h)
        {
            return h.Store != null ? h.Store.Contents.TotalUnits : 0;
        }

        static bool AnyUpgrade(Hoard h)
        {
            for (int i = 0; i < UpgradeInfo.Count; i++) if (h.Level((UpgradeKind)i) > 0) return true;
            return false;
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

            // Une seule carte fine en haut a droite : l'etape en cours, puis, une
            // fois les premiers pas faits, seulement ce qui presse (sinon rien).
            string text, hint, kicker;
            Color ink;
            if (done < Steps.Length)
            {
                text = Steps[done].text;
                hint = Steps[done].hint;
                kicker = (done + 1) + "/" + Steps.Length;
                ink = Color.Lerp(UiStyle.Ink, Palette.Gold, flash);
            }
            else
            {
                text = Urgent(season);
                if (string.IsNullOrEmpty(text)) return;
                hint = "";
                kicker = "!";
                ink = new Color(1f, 0.78f, 0.55f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f));
            }

            float w = UiStyle.S(290);
            float h = UiStyle.S(hint.Length > 0 ? 50 : 34);
            Rect card = new Rect(Screen.width - w - UiStyle.S(18), UiStyle.S(18), w, h);
            GUI.Box(card, GUIContent.none, UiStyle.CardBox);
            UiStyle.FadeBand(new Rect(card.x, card.y, 2f, card.height), Palette.Gold);
            UiStyle.Tinted(new Rect(card.x + UiStyle.S(10), card.y + UiStyle.S(6), UiStyle.S(30), UiStyle.S(20)), kicker, UiStyle.Tiny, Palette.Gold);
            UiStyle.Tinted(new Rect(card.x + UiStyle.S(40), card.y + UiStyle.S(5), w - UiStyle.S(48), UiStyle.S(22)), text, UiStyle.Label, ink);
            if (hint.Length > 0)
                UiStyle.Tinted(new Rect(card.x + UiStyle.S(40), card.y + UiStyle.S(26), w - UiStyle.S(48), UiStyle.S(18)), hint, UiStyle.Tiny, UiStyle.InkDim);
        }

        /// <summary>La chose la plus urgente, en quelques mots -- ou rien.</summary>
        static string Urgent(Season season)
        {
            Hoard h = Game.Hoard;
            if (h == null) return "";
            for (int i = 0; i < Rival.All.Count; i++)
            {
                Rival r = Rival.All[i];
                if (r != null && r.seeker.Hoard.Trophy != null && r.seeker.Hoard.TrophyFrom == Game.Me)
                    return "Rattrape " + r.seeker.Name + " : ta relique !";
            }
            float curse = season.NextCurseIn;
            if (curse >= 0f && curse < 45f && !Game.Inventory.IsEmpty && !season.MagePresent) return "Vide ton sac à ta stèle !";
            if (h.Trophy != null) return "Rapporte la relique volée à ta stèle";
            if (season.Remaining < 120f && h.RelicInHand) return "Pose ta relique, vite !";
            if (Game.Mage != null && Game.Mage.Announced) return "Le mage descend : cours-y";
            if (season.MagePresent && !Game.Inventory.IsEmpty) return "Porte ton sac au mage";
            if (h.RelicInHand) return "Pose ta relique sur ta stèle";
            return "";
        }
    }
}
