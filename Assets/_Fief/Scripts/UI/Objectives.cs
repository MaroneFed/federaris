using UnityEngine;

namespace Fief
{
    /// <summary>
    /// "QUE DOIS-JE FAIRE, LA, MAINTENANT ?" -- une carte fine en haut a droite.
    ///
    /// D'abord QUATRE PREMIERS PAS, qui disent a quoi sert tout le reste (Martin,
    /// 26/09 : "tu recoltes du bois, tu sais meme pas pourquoi") : la pierre-lune
    /// vaut de l'or, l'or va a la stele, le vrai butin est au chateau, le bois sert
    /// a construire. Chacun se coche tout seul. Ensuite, seulement ce qui presse.
    ///
    /// Il ne decide rien : il LIT l'etat du jeu et le dit en quelques mots.
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
            new Step { text = "Pierre-lune  ★2",           hint = "" },
            new Step { text = "Dépose à ta stèle",         hint = "" },
            new Step { text = "Pille le château",          hint = "" },
            new Step { text = "T : pose un piège",         hint = "" }
        };

        static int done;
        static float flash;

        public static void Reset()
        {
            done = 0;
            flash = 0f;
        }

        static bool Completed(int step)
        {
            Hoard h = Game.Hoard;
            Inventory bag = Game.Inventory;
            if (h == null || bag == null) return false;
            switch (step)
            {
                case 0: return bag.Get(ResourceType.Moonstone) > 0 || h.Banked > 0;
                case 1: return h.Banked > 0;
                case 2: return Stats.Treasures > 0;
                default: return Stats.Built > 0;
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

            // Une seule carte fine en haut a droite : l'etape en cours, puis, une
            // fois les premiers pas faits, seulement ce qui presse (sinon rien).
            string text, hint, kicker;
            Color ink;
            if (done < Steps.Length)
            {
                text = Steps[done].text;
                hint = "";
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
                if (Rival.All[i] != null && Rival.All[i].IsHuntedThief) return "Rattrape " + Rival.All[i].seeker.Name + " : ton or !";
            if (Guard.HuntingPlayer) return "Les gardes ! Sème-les";
            if (season.Remaining < 120f && h.Carried > 0) return "La cloche ! Dépose vite";
            if (h.Carried >= 20) return "Rapporte ton butin à ta stèle";
            return "";
        }
    }
}
