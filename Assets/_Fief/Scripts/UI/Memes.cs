using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE MODE MEME (v45 -- Martin : "fais le mode meme : une grosse liste de memes, avec son").
    /// Reglages ▸ Mode meme. Rien ne change au jeu : sur les grands moments qui TE concernent, une
    /// legende de meme claque au milieu de l'ecran (lettres blanches cernees de noir, qui tremblent),
    /// avec son bruit de meme (Sfx.Meme : klaxon, trombone triste, disque raye, BONK...), un coup de
    /// camera et parfois l'ecran qui "frit" (un eclair sature).
    ///
    /// Les legendes sont a nous (pas de texte ni d'image d'un autre) ; les sons aussi, sauf si
    /// Martin depose les siens dans Resources/Sons/Memes/... (voir SfxMemes). Un meme a la fois,
    /// et jamais deux du meme genre en 6 s : l'ecran ne devient pas une foire.
    ///
    /// La liste est en dessous (Kinds) : un moment, ses legendes, son son, son effet.
    /// </summary>
    public static class Memes
    {
        public enum Kind
        {
            KoYou, KoAlone, KoThem, DoubleKo, TripleKo,
            CrownTaken, CrownLost, CrownStolen, MonumentSteal, SacreStart, SacreStopped, Buzzer,
            RoundWon, RoundLost, MatchWon, MatchLost,
            Pushed, PushedThem, Revenge, Summit, ObstacleHit, Ejected,
            Prison, Mini, Inverted, Dance, Refused, Mirror, HomeRun, HotPotato, Enfilade, Surge,
            Start
        }

        sealed class Entry
        {
            public string[] Lines;
            public string Sound;
            public Color Tint;
            public bool Fry;          // l'ecran "frit" (eclair sature)
            public float Punch;       // la camera encaisse
            public Entry(string sound, Color tint, bool fry, float punch, params string[] lines)
            {
                Sound = sound; Tint = tint; Fry = fry; Punch = punch; Lines = lines;
            }
        }

        static readonly Color Red = new Color(1f, 0.3f, 0.25f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        static readonly Color Blue = new Color(0.4f, 0.7f, 1f);
        static readonly Color Green = new Color(0.4f, 1f, 0.5f);
        static readonly Color Pink = new Color(1f, 0.45f, 0.8f);
        static readonly Color Grey = new Color(0.75f, 0.75f, 0.8f);

        /// <summary>LA LISTE : 33 moments, 137 legendes (docs/MODE-MEME.md).</summary>
        static readonly Dictionary<Kind, Entry> Kinds = new Dictionary<Kind, Entry>
        {
            // ---- les chutes
            { Kind.KoYou,        new Entry("Trombone", Grey, false, 4f, "ET C'EST LE DRAME", "BON BAH...", "C'ÉTAIT PAS PRÉVU", "ADIEU MONDE CRUEL", "JE REVIENS... PEUT-ÊTRE", "LES NUAGES M'APPELLENT", "SKILL ISSUE", "OUPS.") },
            { Kind.KoAlone,      new Entry("Sifflet", Grey, false, 3f, "TOUT SEUL ??", "PERSONNE NE T'A TOUCHÉ", "LA GRAVITÉ GAGNE ENCORE", "GÉNIE.", "C'ÉTAIT VOULU (NON)") },
            { Kind.KoThem,       new Entry("Klaxon", Gold, true, 8f, "AU REVOIR", "BYE BYE", "CIAO", "DÉGAGÉ", "DIRECTION LES NUAGES", "BON VOYAGE", "IL EST PARTI OÙ ?") },
            { Kind.DoubleKo,     new Entry("Klaxon", Gold, true, 10f, "DOUBLÉ !!", "ET DE DEUX", "ENCORE UN", "C'EST GRATUIT") },
            { Kind.TripleKo,     new Entry("Fanfare", Gold, true, 14f, "MAIS ARRÊTE", "TRIPLÉ !!!", "IL EST INARRÊTABLE", "QUELQU'UN L'ARRÊTE ?", "C'EST UN MASSACRE") },
            // ---- la Couronne
            { Kind.CrownTaken,   new Entry("Caisse", Gold, false, 6f, "ON EST RICHE", "LE ROI, C'EST MOI", "À GENOUX", "MON PRÉCIEUX", "TROP FACILE", "QUI VEUT LA COURONNE ?") },
            { Kind.CrownLost,    new Entry("Scratch", Red, false, 6f, "QUOI ?!", "NON NON NON", "C'ÉTAIT À MOI", "PERDUE...", "REVIENS !!") },
            { Kind.CrownStolen,  new Entry("Caisse", Green, true, 8f, "MERCI !", "C'EST À MOI MAINTENANT", "VOL À L'ÉTALAGE", "ON PARTAGE ? NON.", "LIVRAISON EXPRESS") },
            { Kind.MonumentSteal,new Entry("Scratch", Red, true, 12f, "À UNE SECONDE PRÈS", "LE VOL DU SIÈCLE", "C'EST CRUEL", "IMPOSSIBLE") },
            { Kind.SacreStart,   new Entry("Suspense", Gold, false, 3f, "PLUS QUE TROIS SECONDES", "NE BOUGE PLUS", "RESPIRE...", "C'EST MAINTENANT") },
            { Kind.SacreStopped, new Entry("Scratch", Red, false, 8f, "SI PRÈS...", "C'ÉTAIT LÀ", "ÇA FAIT MAL", "NOOON") },
            { Kind.Buzzer,       new Entry("Klaxon", Gold, true, 12f, "DERNIÈRE SECONDE !!", "AU BUZZER", "LE TIMING PARFAIT", "ON N'Y CROYAIT PLUS") },
            // ---- les manches
            { Kind.RoundWon,     new Entry("Fanfare", Gold, true, 10f, "GG", "TROP FORT", "CHAMPION", "LÉGENDAIRE", "ÇA, C'EST FAIT", "ON NE M'ARRÊTE PAS") },
            { Kind.RoundLost,    new Entry("Trombone", Grey, false, 0f, "DOMMAGE", "LA PROCHAINE, PEUT-ÊTRE", "C'EST LA VIE", "ON A ESSAYÉ") },
            { Kind.MatchWon,     new Entry("Fanfare", Gold, true, 14f, "VICTOIRE ROYALE", "LE ROI DES NUAGES", "GG À TOUS (SAUF EUX)", "INVAINCU") },
            { Kind.MatchLost,    new Entry("Trombone", Grey, false, 0f, "BIEN ESSAYÉ", "REVANCHE ?", "ON DIRA QUE C'ÉTAIT LE LAG") },
            // ---- les coups
            { Kind.Pushed,       new Entry("Bonk", Red, false, 6f, "BONK", "AÏE", "OUCH", "ÇA PIQUE", "POURQUOI MOI ?") },
            { Kind.PushedThem,   new Entry("Bonk", Green, false, 4f, "BONK", "DÉGAGE", "POUSSE-TOI", "PARDON (NON)") },
            { Kind.Revenge,      new Entry("Boum", Green, true, 8f, "LA VENGEANCE", "RETOUR DE BÂTON", "TU L'AS CHERCHÉ") },
            { Kind.Summit,       new Entry("Boum", Gold, true, 10f, "VIRÉ DU SOMMET", "REDESCENDS", "PAS AUJOURD'HUI") },
            { Kind.ObstacleHit,  new Entry("Bonk", Red, false, 6f, "LE PENDULE A GAGNÉ", "BIEN FAIT", "IL FALLAIT SAUTER", "REGARDE DEVANT TOI") },
            { Kind.Ejected,      new Entry("Sifflet", Red, false, 6f, "RETOUR AU DÉBUT", "TOUT À REFAIRE", "NOOOON", "C'EST PAS GRAVE (SI)") },
            // ---- les sorts
            { Kind.Prison,       new Entry("Suspense", Pink, false, 6f, "EN TAULE", "PRISON", "PAS DE SORTIE", "DIX SECONDES DE RÉFLEXION") },
            { Kind.Mini,         new Entry("Couic", Pink, false, 3f, "PETIT HARICOT", "TOUT MIGNON", "CHÉRIE, J'AI RÉTRÉCI LE HARICOT", "COUIC") },
            { Kind.Inverted,     new Entry("Faux", Pink, false, 3f, "GAUCHE ? DROITE ?", "TOUT EST À L'ENVERS", "JE COMPRENDS RIEN") },
            { Kind.Dance,        new Entry("Fanfare", Pink, false, 2f, "C'EST L'HEURE", "LA PISTE EST À MOI", "ON DANSE !") },
            { Kind.Refused,      new Entry("Faux", Red, false, 2f, "NOPE", "NON.", "PAS ICI", "REFUSÉ") },
            { Kind.Mirror,       new Entry("Boum", Blue, true, 8f, "RETOUR À L'EXPÉDITEUR", "MIROIR, MON BEAU MIROIR", "ÇA TE REVIENT") },
            { Kind.HomeRun,      new Entry("Bonk", Gold, true, 12f, "HOME RUN !!", "DANS LES TRIBUNES", "IL VOLE !") },
            { Kind.HotPotato,    new Entry("Scratch", Gold, false, 6f, "PATATE CHAUDE", "À QUI LE TOUR ?", "PERSONNE NE LA GARDE") },
            { Kind.Enfilade,     new Entry("Fanfare", Blue, false, 8f, "PILOTE DE LIGNE", "QUATRE SUR QUATRE", "TROP PROPRE") },
            { Kind.Surge,        new Entry("Boum", Red, true, 8f, "ÇA DEVIENT SÉRIEUX", "PLUS FORT !", "ATTENTION...") },
            { Kind.Start,        new Entry("Klaxon", Gold, false, 4f, "C'EST PARTI", "QUE LE MEILLEUR GAGNE", "BONNE CHANCE (VOUS EN AUREZ BESOIN)") },
        };

        public static bool On { get { return Settings.Memes; } }

        static readonly Dictionary<Kind, float> lastOf = new Dictionary<Kind, float>();
        static readonly System.Random rng = new System.Random();
        static string line;
        static Color tint;
        static float shownAt = -99f;
        const float Life = 1.8f;

        /// <summary>Un grand moment qui TE concerne : la legende, le son, le coup de camera.</summary>
        public static void Fire(Kind k)
        {
            if (!On) return;
            Entry e;
            if (!Kinds.TryGetValue(k, out e)) return;
            float now = Time.unscaledTime;
            float last;
            if (lastOf.TryGetValue(k, out last) && now - last < 6f) return;
            // Un meme a la fois : un plus petit ne coupe pas celui qui claque encore.
            if (now - shownAt < 0.9f) return;
            lastOf[k] = now;
            shownAt = now;
            line = e.Lines[rng.Next(e.Lines.Length)];
            tint = e.Tint;
            Sfx.Meme(e.Sound, 0.9f);
            OrbitCamera cam = Game.Hud != null ? Game.Hud.orbitCamera : null;
            if (cam != null && e.Punch > 0f) { cam.Punch(e.Punch); cam.Shake(e.Punch * 0.012f); }
            if (e.Fry && Game.Hud != null) Game.Hud.Flash(new Color(e.Tint.r, e.Tint.g * 0.7f, e.Tint.b * 0.4f, 0.45f));
        }

        /// <summary>La legende : lettres blanches cernees de noir, qui claquent puis tremblent (Warnings.Draw).</summary>
        public static void Draw()
        {
            if (!On || line == null) return;
            float age = Time.unscaledTime - shownAt;
            if (age > Life || age < 0f) return;
            float a = Mathf.Clamp01((Life - age) / 0.35f);
            // Elle CLAQUE : plus grosse le premier dixieme, puis sa taille. (On redessine plus grand,
            // jamais de zoom : le texte reste net.)
            float pop = 1f + 0.45f * Mathf.Clamp01(1f - age / 0.12f);
            int fs = Mathf.RoundToInt(UiStyle.S(58) * pop);
            float w = Icons.Width(line, fs) + fs;
            float h = fs * 1.5f;
            // Le tremblement de meme : quelques pixels, des nombres entiers.
            int jx = age < 0.6f ? rng.Next(-3, 4) : 0;
            int jy = age < 0.6f ? rng.Next(-3, 4) : 0;
            Rect r = new Rect(Mathf.Round((Screen.width - w) * 0.5f) + jx, Mathf.Round(Screen.height * 0.36f) + jy, Mathf.Round(w), Mathf.Round(h));
            // La legende cernee : quatre copies noires decalees d'un nombre entier de pixels, puis le blanc.
            int o = Mathf.Max(2, Mathf.RoundToInt(fs * 0.06f));
            Color ink = new Color(0f, 0f, 0f, a);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) Icons.Text(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), line, fs, ink, TextAnchor.MiddleCenter, false);
            Icons.Text(r, line, fs, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, false);
            // Un trait a sa couleur dessous, comme un surlignage.
            float uw = Mathf.Round(w * 0.6f * Mathf.Clamp01(age / 0.2f));
            UiStyle.Fill(new Rect(Mathf.Round((Screen.width - uw) * 0.5f), Mathf.Round(r.yMax - fs * 0.15f), uw, Mathf.Max(3f, Mathf.Round(fs * 0.08f))), new Color(tint.r, tint.g, tint.b, a));
        }
    }
}
