using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LE MODE MEME (v46 -- Martin : "t'as rien capte : le mode meme, c'est comme le mode normal et
    /// le mode Dieu, sauf que la tu mets tous les memes d'internet, genre 67, fahhh"). Un MODE DE JEU,
    /// choisi au salon (Mode : Normal / DIEU / MEME, Match.MemeMode) -- en v45 ce n'etait qu'un reglage.
    /// Les regles du mode normal ; et partout, les memes d'internet :
    ///  - sur chaque grand moment qui TE concerne, la legende claque au milieu de l'ecran (FAHHH,
    ///    BRUH, SKILL ISSUE, FANUM TAX, ABSOLUTE CINEMA...) avec son cri (SfxVoix : la voix du
    ///    haricot crie FAHHH, SIX SEVEN, BRUH, SHEESH, OOF, WOW, NOOON ; ou les bruits : klaxon,
    ///    trombone triste, BONK...), un coup de camera, parfois l'ecran qui "frit" ;
    ///  - TOUTES LES 67 SECONDES, le "6 7" : "SIX SEVEN" a l'ecran de tous, et tous les haricots font
    ///    le geste, les mains qui pesent (CharacterRig.SixSevenUntil) -- l'heure est celle de la
    ///    manche, la meme chez tout le monde en ligne ;
    ///  - les bots s'appellent Skibidi, Gigachad, Le NPC, Rizzler... (Match.NameOf).
    ///
    /// Les mots sont des mots (personne ne possede "BRUH") ; les sons sont fabriques par le code.
    /// Martin peut deposer les vrais enregistrements dans Resources/Sons/Memes/... pour jouer entre
    /// amis -- a remplacer par des sons libres (CC0) avant la sortie sur Steam.
    /// Un meme a la fois, jamais deux du meme genre en 6 s. (Pas d'"AURA" : Martin deteste, 30/09.)
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
            Start, SixSeven
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

        /// <summary>LA LISTE : 34 moments et leurs memes (docs/MODE-MEME.md).</summary>
        static readonly Dictionary<Kind, Entry> Kinds = new Dictionary<Kind, Entry>
        {
            // ---- les chutes
            { Kind.KoYou,        new Entry("RobertWeide", Grey, false, 4f, "DIRECTED BY ROBERT B. WEIDE", "FAHHH", "BRUH", "SKILL ISSUE", "EMOTIONAL DAMAGE", "IT'S SO OVER", "L", "WASTED", "MISSION FAILED", "TO BE CONTINUED...", "OOF", "JE SUIS MORT", "HOUSTON, ON A UN PROBLÈME") },
            { Kind.KoAlone,      new Entry("Bruh", Grey, false, 3f, "BRUH", "NPC MOVE", "SKILL ISSUE", "OHIO MOMENT", "BRAINROT", "100 % GÉNIE", "MAIS POURQUOI", "C'EST QUOI CE GAMEPLAY") },
            { Kind.KoThem,       new Entry("Airhorn", Gold, true, 8f, "YEET", "GET OUT", "RATIO", "CHEH", "SEE YOU NEVER", "BYE BYE", "GO TOUCH GRASS", "NAH, I'D WIN", "CIAO") },
            { Kind.DoubleKo,     new Entry("Airhorn", Gold, true, 10f, "SHEESH", "DOUBLE KILL", "W", "ON GOD", "LET HIM COOK", "IL EST CHAUD") },
            { Kind.TripleKo,     new Entry("Sheesh", Gold, true, 14f, "SHEEEESH", "GIGACHAD", "TRIPLE KILL", "HE'S HIM", "INARRÊTABLE", "SIGMA GRINDSET") },
            // ---- la Couronne
            { Kind.CrownTaken,   new Entry("Wow", Gold, false, 6f, "W", "LET HIM COOK", "MAIN CHARACTER", "SIGMA", "NAH, I'D WIN", "C'EST LE S", "KING", "RIZZ MAXIMUM") },
            { Kind.CrownLost,    new Entry("Fahhh", Red, false, 6f, "FAHHH", "NOOON", "IT'S SO OVER", "L", "EMOTIONAL DAMAGE", "NAN MAIS ALLÔ", "BRUH") },
            { Kind.CrownStolen,  new Entry("FanumTax", Green, true, 8f, "FANUM TAX", "MINE", "W", "NO CAP", "C'EST LE S", "SKILL ISSUE (LUI)", "MERCI BIEN") },
            { Kind.MonumentSteal,new Entry("EmotionalDamage", Red, true, 12f, "FANUM TAX", "EMOTIONAL DAMAGE", "ABSOLUTE CINEMA", "COLD", "LE VOL DU SIÈCLE") },
            { Kind.SacreStart,   new Entry("DunDun", Gold, false, 3f, "LOCK IN", "WE'RE SO BACK", "TIC... TAC...", "6... 7...", "BOUGE PAS") },
            { Kind.SacreStopped, new Entry("Nooo", Red, false, 8f, "NOOON", "IT'S SO OVER", "FAHHH", "SO CLOSE", "L", "ÇA FAIT MAL") },
            { Kind.Buzzer,       new Entry("Airhorn", Gold, true, 12f, "ABSOLUTE CINEMA", "CLUTCH", "SHEESH", "WE'RE SO BACK", "CINÉMA") },
            // ---- les manches
            { Kind.RoundWon,     new Entry("Gigachad", Gold, true, 10f, "W", "GG EZ", "SIGMA", "GIGACHAD", "ABSOLUTE CINEMA", "MOGGED", "C'EST LE S", "HE'S HIM") },
            { Kind.RoundLost,    new Entry("SadViolin", Grey, false, 0f, "L", "BRUH", "IT'S SO OVER", "NPC", "SKILL ISSUE", "C'EST LE LAG") },
            { Kind.MatchWon,     new Entry("Gigachad", Gold, true, 14f, "LE GOAT", "GIGACHAD", "ABSOLUTE CINEMA", "SIGMA DU MOIS", "GG EZ", "THE GOAT HAS SPOKEN") },
            { Kind.MatchLost,    new Entry("SadViolin", Grey, false, 0f, "IT'S SO OVER", "L + RATIO", "C'EST LE LAG", "BRUH", "GG (PAS EZ)") },
            // ---- les coups
            { Kind.Pushed,       new Entry("Oof", Red, false, 6f, "BONK", "OOF", "BRUH", "EMOTIONAL DAMAGE", "RIP", "AÏE") },
            { Kind.PushedThem,   new Entry("Bonk", Green, false, 4f, "BONK", "YEET", "CHEH", "GET OUT", "SIGMA PUSH", "SKILL ISSUE") },
            { Kind.Revenge,      new Entry("VineBoom", Green, true, 8f, "UNO REVERSE", "KARMA", "CHEH", "INSTANT KARMA", "NO U") },
            { Kind.Summit,       new Entry("Yeet", Gold, true, 10f, "YEET", "MOGGED", "GET OUT", "PAS AUJOURD'HUI", "RATIO") },
            { Kind.ObstacleHit,  new Entry("MetalPipe", Red, false, 6f, "BONK", "OOF", "SKILL ISSUE", "NPC MOVE", "REGARDE DEVANT") },
            { Kind.Ejected,      new Entry("Fahhh", Red, false, 6f, "FAHHH", "TO BE CONTINUED...", "IT'S SO OVER", "BACK TO SPAWN", "NOOON") },
            // ---- les sorts
            { Kind.Prison,       new Entry("CoffinDance", Pink, false, 6f, "COFFIN DANCE", "EN TAULE", "NO ESCAPE", "RIP") },
            { Kind.Mini,         new Entry("Oof", Pink, false, 3f, "SMOL", "TINY", "MINI MOI", "COUIC", "BÉBOU") },
            { Kind.Inverted,     new Entry("Huh", Pink, false, 3f, "OHIO", "BRAINROT", "ERROR 404", "WHAT", "JE COMPRENDS RIEN") },
            { Kind.Dance,        new Entry("SixSept", Pink, false, 2f, "6 7", "SKIBIDI", "CHIPI CHIPI CHAPA CHAPA", "LA PISTE EST À MOI") },
            { Kind.Refused,      new Entry("WindowsError", Red, false, 2f, "ERREUR WINDOWS", "NOPE", "ACCESS DENIED", "BRUH") },
            { Kind.Mirror,       new Entry("VineBoom", Blue, true, 8f, "UNO REVERSE", "NO U", "RETURN TO SENDER") },
            { Kind.HomeRun,      new Entry("Bonk", Gold, true, 12f, "HOME RUN", "YEET", "TO THE MOON", "BYE") },
            { Kind.HotPotato,    new Entry("Bruh", Gold, false, 6f, "PATATE CHAUDE", "CHAOS", "BRAINROT", "OHIO") },
            { Kind.Enfilade,     new Entry("Wow", Blue, false, 8f, "SHEESH", "CLEAN", "MAIN CHARACTER", "W") },
            { Kind.Surge,        new Entry("TacoBell", Red, true, 8f, "LOCK IN", "ÇA CHAUFFE", "SHEESH", "IT'S GETTING HOT") },
            { Kind.Start,        new Entry("Airhorn", Gold, false, 4f, "LOCK IN", "LET'S GO", "6 7", "GL HF", "SKIBIDI") },
            // ---- toutes les 67 secondes
            { Kind.SixSeven,     new Entry("SixSept", Pink, true, 6f, "6 7 !!", "SIX SEVEN", "6 7 6 7", "SIX-SEPT !!") },
        };

        /// <summary>Le mode meme est allume (choisi au salon).</summary>
        public static bool On { get { return Match.MemeMode; } }

        static readonly Dictionary<Kind, float> lastOf = new Dictionary<Kind, float>();
        static readonly System.Random rng = new System.Random();
        static string line;
        static Color tint;
        static float shownAt = -99f;
        const float Life = 1.8f;

        /// <summary>Un grand moment qui TE concerne : la legende, le son, le coup de camera.</summary>
        public static void Fire(Kind k) { Fire(k, false); }

        static void Fire(Kind k, bool force)
        {
            if (!On) return;
            Entry e;
            if (!Kinds.TryGetValue(k, out e)) return;
            float now = Time.unscaledTime;
            float last;
            if (lastOf.TryGetValue(k, out last) && now - last < 6f) return;
            // Un meme a la fois : un plus petit ne coupe pas celui qui claque encore.
            if (!force && now - shownAt < 0.9f) return;
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
        /// <summary>
        /// LE "6 7" : toutes les 67 secondes de manche (67, 134, 201...), la meme seconde chez tout le
        /// monde. Tous les haricots font le geste trois secondes, et "SIX SEVEN" claque.
        /// </summary>
        static int sixSevens;
        public const float SixSevenEvery = 67f;

        static void Tick()
        {
            Season season = Game.Season;
            if (season == null) return;
            int n = Mathf.FloorToInt(season.Elapsed / SixSevenEvery);
            // Une nouvelle manche repart de zero.
            if (n < sixSevens) sixSevens = n;
            if (n <= sixSevens) return;
            sixSevens = n;
            if (!season.Running) return;
            CharacterRig.SixSevenUntil = Time.time + 3f;
            Fire(Kind.SixSeven, true);
        }

        // ------------------------------------------------ (v46.1) les capacites-memes

        sealed class Overhead
        {
            public Seeker Who;
            public string Text;
            public Color Tint;
            public float At;
        }
        static readonly List<Overhead> overheads = new List<Overhead>();
        const float OverheadLife = 1.6f;

        /// <summary>
        /// Un joueur lance sa capacite-meme (AbilityCaster.Cast, chez tout le monde grace au
        /// rejeu en ligne) : il CRIE le meme (le son part de lui) et le mot claque au-dessus de sa
        /// tete. Si c'est toi, il claque au milieu de TON ecran (tu ne vois pas ta tete).
        /// </summary>
        public static void CastShout(Seeker s, Ability a)
        {
            if (!On || s == null || s.Body == null) return;
            MemeSkins.Skin sk = MemeSkins.Of(a);
            if (sk == null) return;
            Color c = AbilityInfo.Tint(a);
            Color bright = Color.Lerp(c, Color.white, 0.35f);
            if (s.IsPlayer)
            {
                Sfx.Meme(sk.Sound, 0.85f);
                line = sk.Shout;
                tint = bright;
                shownAt = Time.unscaledTime;
                return;
            }
            Sfx.MemeAt(sk.Sound, s.Body.position, 1f);
            overheads.RemoveAll(o => o.Who == s);
            overheads.Add(new Overhead { Who = s, Text = sk.Shout, Tint = bright, At = Time.unscaledTime });
        }

        /// <summary>Les mots au-dessus des tetes : cernes de noir, qui montent et s'effacent.</summary>
        static void DrawOverheads()
        {
            float now = Time.unscaledTime;
            overheads.RemoveAll(o => o.Who == null || o.Who.Body == null || now - o.At > OverheadLife);
            Camera cam = Camera.main;
            if (cam == null) return;
            for (int i = 0; i < overheads.Count; i++)
            {
                Overhead o = overheads[i];
                float age = now - o.At;
                Vector3 head = o.Who.Body.position + Vector3.up * (3.1f + age * 0.6f);
                Vector3 sp = cam.WorldToScreenPoint(head);
                if (sp.z <= 0.5f || sp.z > 140f) continue;
                float a = Mathf.Clamp01((OverheadLife - age) / 0.4f);
                float pop = 1f + 0.4f * Mathf.Clamp01(1f - age / 0.12f);
                int fs = Mathf.RoundToInt(Mathf.Clamp(UiStyle.S(40) * 18f / Mathf.Max(8f, sp.z), UiStyle.S(16), UiStyle.S(40)) * pop);
                float w = Icons.Width(o.Text, fs) + fs;
                Rect r = new Rect(Mathf.Round(sp.x - w * 0.5f), Mathf.Round(Screen.height - sp.y - fs * 1.2f), Mathf.Round(w), Mathf.Round(fs * 1.4f));
                int off = Mathf.Max(1, Mathf.RoundToInt(fs * 0.06f));
                Color ink = new Color(0f, 0f, 0f, a);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0) Icons.Text(new Rect(r.x + dx * off, r.y + dy * off, r.width, r.height), o.Text, fs, ink, TextAnchor.MiddleCenter, false);
                Icons.Text(r, o.Text, fs, new Color(o.Tint.r, o.Tint.g, o.Tint.b, a), TextAnchor.MiddleCenter, false);
            }
        }

        public static void Draw()
        {
            if (!On) return;
            MemeSounds.Scan();
            MemeSounds.Poll();
            Tick();
            DrawOverheads();
            if (line == null) return;
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
