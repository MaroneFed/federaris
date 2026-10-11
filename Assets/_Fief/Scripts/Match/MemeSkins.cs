using System.Collections.Generic;

namespace Fief
{
    /// <summary>
    /// LES CAPACITES DU MODE MEME (v46.1 -- Martin : "t'as pas capte : les capacites sont des memes
    /// que t'adaptes"). Comme le Mode Dieu a sa table (les divines), le mode meme a la sienne : des
    /// MEMES. Chacun est une capacite du jeu dont le meme dit ce qu'elle fait : le cri FAHHH sonne
    /// tout le monde devant toi, le 6 7 fait danser le 6 7 a tous ceux autour, le BONK assomme, le
    /// RICKROLL est un piege, le FANUM TAX te ramene le joueur, le RIZZ l'attire, le SKIBIDI est une
    /// tornade, OHIO un trou noir, UNO REVERSE renvoie les sorts...
    ///
    /// Au lancer, ton haricot CRIE son meme (le son part de lui, tout le monde l'entend) et le mot
    /// claque au-dessus de sa tete pour tous (Memes.CastShout). Sur ta carte : le nom du meme et ce
    /// que ca fait, en une phrase.
    ///
    /// Classe C# pure. Hors du mode meme, rien ne change : les noms et les phrases d'origine.
    /// Les numeros des capacites ne changent pas (le reseau compte dessus) : seul l'habit change.
    /// </summary>
    public static class MemeSkins
    {
        public sealed class Skin
        {
            public readonly string Name;
            public readonly string Line;
            public readonly string Sound;     // le dossier de Sons/Memes (Sfx.Meme)
            public readonly string Shout;     // le mot qui claque au-dessus de la tete
            public Skin(string name, string line, string sound, string shout)
            {
                Name = name; Line = line; Sound = sound; Shout = shout;
            }
        }

        /// <summary>LA TABLE DU MODE MEME : 46 actives, 26 passives.</summary>
        static readonly Dictionary<Ability, Skin> All = new Dictionary<Ability, Skin>
        {
            // ---- les actives : les cris
            { Ability.Cri,         new Skin("FAHHH", "Tu cries FAHHH : tous ceux devant toi sont sonnés.", "Fahhh", "FAHHH") },
            { Ability.Disco,       new Skin("6 7", "Tu cries SIX SEVEN : tous ceux à 9 m font le 6 7, bloqués 3 s.", "SixSept", "6 7 !!") },
            { Ability.Onde,        new Skin("Vine boom", "BOUM : tout le monde à 11 m de toi s'envole.", "Boum", "BOOM") },
            { Ability.Raz,         new Skin("It's so over", "Une vague de larmes part de toi et emporte tout le monde à 20 m.", "Nooo", "IT'S SO OVER") },
            { Ability.Seisme,      new Skin("Bass boosted", "Tu tapes le sol : tous ceux qui sont debout à 25 m sautent en l'air.", "Boum", "BASS BOOSTED") },
            // ---- les coups
            { Ability.Gant,        new Skin("Bonk", "Un énorme BONK frappe droit devant toi.", "Bonk", "BONK") },
            { Ability.Meteore,     new Skin("Ultra bonk", "Tu sautes, puis tu retombes : BONK sur tout le monde à 10 m.", "Bonk", "ULTRA BONK") },
            { Ability.CoupDePied,  new Skin("This is Sparta", "Ta prochaine poussée envoie 3 fois plus loin. SPARTAAA.", "Boum", "SPARTAAA") },
            { Ability.PoingFaucon, new Skin("Sheesh punch", "Tu charges ton poing, puis tu frappes : le joueur touché part très loin.", "Sheesh", "SHEEESH") },
            { Ability.Charge,      new Skin("Leeroy", "Tu fonces 25 m en hurlant : le premier touché est emporté et écrasé au bout.", "Fahhh", "LEEROOOY") },
            { Ability.Boulet,      new Skin("Sigma", "Tu fonces comme un sigma sur 30 m et tu renverses tout.", "Sheesh", "SIGMA") },
            { Ability.Ruee,        new Skin("Speedrun", "Tu fonces 15 m droit devant. Ceux sur ton chemin sont bousculés.", "Sifflet", "SPEEDRUN") },
            { Ability.Oreillers,   new Skin("UwU", "6 oreillers tout mignons droit devant toi : paf, paf, paf !", "Couic", "UWU") },
            { Ability.Foudre,      new Skin("Emotional damage", "Vise un joueur : la foudre lui tombe dessus.", "Boum", "EMOTIONAL DAMAGE") },
            { Ability.Missile,     new Skin("Ratio", "Un ratio guidé part chercher le joueur le plus proche et explose.", "Klaxon", "RATIO") },
            { Ability.Deluge,      new Skin("Bombardiro", "Des bombes tombent du ciel là où tu vises.", "Klaxon", "BOMBARDIRO") },
            { Ability.Tnt,         new Skin("Aw man", "Une caisse de TNT clignote 3 s, puis explose (10 m).", "Suspense", "AW MAN") },
            { Ability.Mine,        new Skin("Surprise", "Tu poses une mine. Le premier qui passe à côté a une surprise.", "Klaxon", "SURPRISE") },
            { Ability.Gel,         new Skin("Ice cold", "Une boule de glace : ceux qu'elle touche sont poussés et ralentis.", "Wow", "ICE COLD") },
            { Ability.Boomerang,   new Skin("It's back", "Un boomerang : il frappe à l'aller et au retour.", "Wow", "IT'S BACK") },
            // ---- les tornades, les trous, les vagues
            { Ability.Tornade,     new Skin("Skibidi", "Une tornade skibidi part devant toi et envoie tout le monde en l'air.", "SixSept", "SKIBIDI") },
            { Ability.TrouNoir,    new Skin("Ohio", "Un trou noir d'Ohio aspire tout le monde devant toi, puis explose.", "Suspense", "OHIO") },
            { Ability.Toupie,      new Skin("OIIA OIIA", "Tu tournes comme le chat : ceux qui te touchent sont éjectés.", "Couic", "OIIA OIIA") },
            { Ability.Flammes,     new Skin("This is fine", "Pendant 4 s, du feu derrière toi. Qui marche dedans est éjecté.", "Suspense", "THIS IS FINE") },
            // ---- les sorts
            { Ability.Hypnose,     new Skin("Rizz", "Vise un joueur : ton rizz l'attire, il marche vers toi 3 s.", "Wow", "RIZZ") },
            { Ability.Crochet,     new Skin("Fanum tax", "Vise un joueur, jusqu'à 40 m : tu le tires jusqu'à toi. Taxé.", "Caisse", "FANUM TAX") },
            { Ability.GobeTout,    new Skin("Mukbang", "Tu aspires le joueur devant toi (15 m), puis tu le recraches très loin.", "Caisse", "MUKBANG") },
            { Ability.Lasso,       new Skin("Yeet", "Vise un joueur : tu l'attrapes et tu le YEET là où tu regardes.", "Wow", "YEET") },
            { Ability.Prison,      new Skin("En taule", "Vise un joueur : en taule 10 s. Un coup le libère.", "Suspense", "EN TAULE") },
            { Ability.Bombe,       new Skin("Patate chaude", "Vise un joueur : une bombe se colle à lui et explose 2 s après.", "Suspense", "PATATE CHAUDE") },
            { Ability.Inversion,   new Skin("Brainrot", "Vise un joueur : sa gauche et sa droite sont inversées 6 s.", "Faux", "BRAINROT") },
            { Ability.Mini,        new Skin("Smol", "Vise un joueur : il devient tout smol, lent, et s'envole au moindre coup.", "Couic", "SMOL") },
            { Ability.Ballon,      new Skin("To the moon", "Vise un joueur : il gonfle, s'envole, puis éclate.", "Wow", "TO THE MOON") },
            { Ability.PiegeLoup,   new Skin("Rickroll", "Tu poses un piège : qui marche dessus est rickrollé, bloqué 3 s.", "Fanfare", "RICKROLL") },
            { Ability.Banane,      new Skin("Skill issue", "3 peaux de banane derrière toi : qui glisse a un skill issue.", "Faux", "SKILL ISSUE") },
            { Ability.Glu,         new Skin("Touch grass", "Une flaque de colle : qui marche dedans reste collé à l'herbe.", "Bruh", "TOUCH GRASS") },
            { Ability.Echange,     new Skin("Uno swap", "Vise un joueur, jusqu'à 45 m : vous échangez vos places.", "Scratch", "UNO SWAP") },
            // ---- les mouvements
            { Ability.Clignement,  new Skin("Lag", "Tu lagues et tu réapparais 20 m plus loin.", "Scratch", "LAG") },
            { Ability.Fantome,     new Skin("Glitch", "Pendant 4 s, plus rien ne te touche. Pas avec la Couronne.", "Scratch", "GLITCH") },
            { Ability.Voile,       new Skin("Tu me vois pas", "Tu deviens invisible pendant 7 secondes.", "Wow", "TU ME VOIS PAS") },
            { Ability.Geant,       new Skin("Absolute unit", "Tu deviens énorme 7 s : on ne peut plus te pousser.", "Boum", "ABSOLUTE UNIT") },
            { Ability.Fusee,       new Skin("Stonks", "Tu décolles to the moon, puis tu voles où tu veux. Pas dans le château.", "Wow", "STONKS") },
            { Ability.Catapulte,   new Skin("YOLO", "Tu te lances très loin devant toi. Pas avec la Couronne.", "Wow", "YOLO") },
            { Ability.Pogo,        new Skin("Tung tung tung", "Pendant 5 s, tu rebondis très haut à chaque pas : tung, tung, tung.", "Bonk", "TUNG TUNG TUNG") },
            { Ability.Ressort,     new Skin("Boing", "Un trampoline apparaît à tes pieds. Tout le monde peut sauter dessus.", "Couic", "BOING") },
            { Ability.Bond,        new Skin("Jumpscare", "Tu sautes très haut. Ceux qui sont collés à toi sont repoussés.", "Boum", "JUMPSCARE") },

            // ---- les passives
            { Ability.Miroir,        new Skin("Uno reverse", "Les sorts qu'on te lance repartent sur celui qui les a lancés.", "Boum", "UNO REVERSE") },
            { Ability.Riposte,       new Skin("Karma", "Celui qui te pousse est repoussé à son tour.", "Boum", "KARMA") },
            { Ability.SecondSouffle, new Skin("We're so back", "Quand tu tombes dans les nuages, tu repars avec des ailes d'or, protégé 6 s.", "Sheesh", "WE'RE SO BACK") },
            { Ability.Plume,         new Skin("Light weight baby", "Avec la Couronne, tu voles aussi vite que les autres.", "Wow", "LIGHT WEIGHT") },
            { Ability.TeteDure,      new Skin("Mewing", "Mâchoire de béton : les pièges de la tour ne te font plus tomber.", "Sheesh", "MEWING") },
            { Ability.Armure,        new Skin("Gigachad", "Le premier coup que tu prends dans la manche ne te fait rien.", "Sheesh", "GIGACHAD") },
            { Ability.Ancrage,       new Skin("Unbothered", "Quand on te pousse, tu pars 2 fois moins loin.", "Bruh", "UNBOTHERED") },
            { Ability.Poigne,        new Skin("Sigma push", "Ta poussée envoie 2 fois plus loin.", "Bonk", "SIGMA PUSH") },
            { Ability.Increvable,    new Skin("Plot armor", "Les sorts qu'on te lance durent 2 fois moins longtemps.", "Wow", "PLOT ARMOR") },
            { Ability.AngeGardien,   new Skin("Not today", "Une fois par manche, si tu tombes dans les nuages, tu reviens où tu étais.", "Nooo", "NOT TODAY") },
            { Ability.Kamikaze,      new Skin("Kaboom", "Quand on te pousse, tu exploses : ceux autour de toi s'envolent.", "Boum", "KABOOM") },
            { Ability.Rage,          new Skin("Gamer rage", "Chaque coup que tu prends rend ta prochaine poussée plus forte.", "Fahhh", "RAGE") },
            { Ability.Ninja,         new Skin("Lowkey", "Reste sans bouger 1 seconde : tu deviens invisible.", "Wow", "LOWKEY") },
            { Ability.Vampire,       new Skin("Main character", "Chaque fois que tu frappes quelqu'un, tu cours plus vite 3 secondes.", "Sheesh", "MAIN CHARACTER") },
            { Ability.Chanceux,      new Skin("W luck", "Une fois sur trois, ta capacité revient tout de suite.", "Fanfare", "W") },
            { Ability.Pickpocket,    new Skin("Sneaky", "Quand tu prends la Couronne à quelqu'un, tu deviens invisible 2 s.", "Caisse", "SNEAKY") },
            { Ability.Coureur,       new Skin("Speedrunner", "Tu cours un peu plus vite que les autres.", "Sifflet", "SPEEDRUNNER") },
            { Ability.Sprinter,      new Skin("Gotta go fast", "Au début de chaque manche, tu cours très vite pendant 20 secondes.", "Sifflet", "GO FAST") },
            { Ability.DoubleSaut,    new Skin("Jump boost", "En l'air, appuie encore sur Espace : tu sautes une 2e fois.", "Couic", "JUMP") },
            { Ability.Kangourou,     new Skin("Bunny hop", "Tu sautes beaucoup plus haut.", "Couic", "BUNNY HOP") },
            { Ability.Planeur,       new Skin("I believe I can fly", "Tu as toujours des ailes d'or : tu voles plus vite et plus loin.", "Wow", "I CAN FLY") },
            { Ability.Rebond,        new Skin("Ground pound", "Quand tu retombes de haut, tu fais une onde de choc.", "Boum", "GROUND POUND") },
            { Ability.Recharge,      new Skin("Grindset", "Ta capacité revient plus vite.", "Sheesh", "GRINDSET") },
            { Ability.Flair,         new Skin("Sus detector", "Tu vois ce qui est caché : les mines, les invisibles, à travers la fumée.", "Suspense", "SUS") },
            { Ability.BrasLongs,     new Skin("Spaghetti arms", "Ta poussée touche de plus loin : 5 m.", "Couic", "SPAGHETTI") },
            { Ability.HomeRun,       new Skin("Home run", "Toutes les 4 poussées réussies, la suivante envoie 3 fois plus loin.", "Bonk", "HOME RUN") },
        };

        public static Skin Of(Ability a)
        {
            Skin s;
            return All.TryGetValue(a, out s) ? s : null;
        }

        public static bool Has(Ability a) { return All.ContainsKey(a); }
        public static int Count { get { return All.Count; } }

        /// <summary>Toute la table, pour la doc (docs/MODE-MEME.md).</summary>
        public static IEnumerable<KeyValuePair<Ability, Skin>> Each() { return All; }
    }
}
