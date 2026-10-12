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

        /// <summary>LA TABLE DU MODE MEME : 40 actives, 26 passives -- que des memes connus.</summary>
        static readonly Dictionary<Ability, Skin> All = new Dictionary<Ability, Skin>
        {
            // ---- les actives
            { Ability.Cri, new Skin("FAHHH", "Tu cries FAHHH : tous ceux devant toi sont sonnés.", "Fahhh", "FAHHH") },
            { Ability.Disco, new Skin("6 7", "Tu cries SIX SEVEN : tous ceux à 9 m font le 6 7, bloqués 3 s.", "SixSept", "6 7 !!") },
            { Ability.Onde, new Skin("Vine boom", "BOUM : tout le monde à 11 m de toi s'envole.", "VineBoom", "BOOM") },
            { Ability.Gant, new Skin("Metal pipe", "Une barre de métal frappe droit devant toi : CLANG.", "MetalPipe", "CLANG") },
            { Ability.Meteore, new Skin("Bonk", "Tu sautes, puis tu retombes : BONK sur tout le monde à 10 m.", "Bonk", "BONK") },
            { Ability.CoupDePied, new Skin("This is Sparta", "Ta prochaine poussée envoie 3 fois plus loin. SPARTAAA.", "Sparta", "SPARTAAA") },
            { Ability.PoingFaucon, new Skin("Sheesh", "Tu charges ton poing, puis tu frappes : le joueur touché part très loin.", "Sheesh", "SHEEESH") },
            { Ability.Charge, new Skin("Leeroy Jenkins", "Tu fonces 25 m en hurlant : le premier touché est emporté et écrasé au bout.", "Leeroy", "LEEROOOY") },
            { Ability.Boulet, new Skin("Sigma boy", "Tu fonces comme un sigma sur 30 m et tu renverses tout.", "Sigma", "SIGMA") },
            { Ability.Ruee, new Skin("Tralalero tralala", "Tu fonces 15 m comme le requin en baskets. Ceux sur ton chemin sont bousculés.", "Tralalero", "TRALALERO") },
            { Ability.Foudre, new Skin("Emotional damage", "Vise un joueur : la foudre lui tombe dessus.", "EmotionalDamage", "EMOTIONAL DAMAGE") },
            { Ability.Missile, new Skin("Bombardiro crocodilo", "Un crocodile-bombardier part chercher le joueur le plus proche et explose.", "Bombardiro", "BOMBARDIRO") },
            { Ability.Deluge, new Skin("MLG airstrike", "Des bombes tombent du ciel là où tu vises.", "Airhorn", "MLG") },
            { Ability.Tnt, new Skin("Aw man", "Une caisse de TNT clignote 3 s, puis explose (10 m).", "AwMan", "AW MAN") },
            { Ability.Mine, new Skin("Sus", "Un piège sus : le premier qui passe à côté saute en l'air.", "AmongUs", "SUS") },
            { Ability.Tornade, new Skin("Skibidi", "Une tornade skibidi part devant toi et envoie tout le monde en l'air.", "Skibidi", "SKIBIDI") },
            { Ability.TrouNoir, new Skin("Ohio", "Un trou noir d'Ohio aspire tout le monde devant toi, puis explose.", "Ohio", "OHIO") },
            { Ability.Toupie, new Skin("OIIA OIIA", "Tu tournes comme le chat : ceux qui te touchent sont éjectés.", "Oiia", "OIIA OIIA") },
            { Ability.Flammes, new Skin("This is fine", "Pendant 4 s, du feu derrière toi. Qui marche dedans est éjecté.", "ThisIsFine", "THIS IS FINE") },
            { Ability.Hypnose, new Skin("Rizz", "Vise un joueur : ton rizz l'attire, il marche vers toi 3 s.", "Rizz", "RIZZ") },
            { Ability.Crochet, new Skin("Fanum tax", "Vise un joueur, jusqu'à 40 m : tu le tires jusqu'à toi. Taxé.", "FanumTax", "FANUM TAX") },
            { Ability.Lasso, new Skin("Yeet", "Vise un joueur : tu l'attrapes et tu le YEET là où tu regardes.", "Yeet", "YEET") },
            { Ability.Prison, new Skin("Coffin dance", "Vise un joueur : enfermé 10 s, la danse du cercueil joue. Un coup le libère.", "CoffinDance", "COFFIN DANCE") },
            { Ability.Bombe, new Skin("Mission failed", "Vise un joueur : une bombe se colle à lui et explose 2 s après.", "MissionFailed", "MISSION FAILED") },
            { Ability.Inversion, new Skin("Huh cat", "Vise un joueur : sa gauche et sa droite sont inversées 6 s. HUH ?", "Huh", "HUH ?") },
            { Ability.Mini, new Skin("Roblox oof", "Vise un joueur : il devient tout petit, lent, et s'envole au moindre coup.", "Oof", "OOF") },
            { Ability.Ballon, new Skin("Shooting stars", "Vise un joueur : il gonfle et part dans les étoiles, puis éclate.", "ShootingStars", "SHOOTING STARS") },
            { Ability.PiegeLoup, new Skin("Rickroll", "Tu poses un piège : qui marche dessus est rickrollé, bloqué 3 s.", "Rickroll", "RICKROLL") },
            { Ability.Banane, new Skin("Windows error", "3 peaux de banane derrière toi : qui glisse plante comme Windows.", "WindowsError", "ERREUR") },
            { Ability.Glu, new Skin("Brr brr patapim", "Une flaque de colle : qui marche dedans prend racine comme Patapim.", "Patapim", "PATAPIM") },
            { Ability.Echange, new Skin("Uno reverse", "Vise un joueur, jusqu'à 45 m : vous échangez vos places.", "VineBoom", "UNO REVERSE") },
            { Ability.Clignement, new Skin("Lag", "Tu lagues et tu réapparais 20 m plus loin.", "Lag", "LAG") },
            { Ability.Voile, new Skin("You can't see me", "Tu deviens invisible pendant 7 secondes.", "JohnCena", "YOU CAN'T SEE ME") },
            { Ability.Geant, new Skin("Gigachad", "Tu deviens énorme 7 s : on ne peut plus te pousser.", "Gigachad", "GIGACHAD") },
            { Ability.Fusee, new Skin("Stonks", "Tu décolles to the moon, puis tu voles où tu veux. Pas dans le château.", "Stonks", "STONKS") },
            { Ability.Pogo, new Skin("Tung tung tung sahur", "Pendant 5 s, tu rebondis très haut à chaque pas : tung, tung, tung.", "TungTung", "TUNG TUNG TUNG") },
            { Ability.Ressort, new Skin("Boing", "Un trampoline apparaît à tes pieds. Tout le monde peut sauter dessus.", "Boing", "BOING") },
            { Ability.Raz, new Skin("Sad violin", "Une vague de larmes part de toi et emporte tout le monde à 20 m.", "SadViolin", "IT'S SO OVER") },
            { Ability.Seisme, new Skin("Taco Bell", "Tu tapes le sol (BONG) : tous ceux qui sont debout à 25 m sautent en l'air.", "TacoBell", "BONG") },
            { Ability.Catapulte, new Skin("Chipi chipi chapa chapa", "Tu te lances très loin devant toi en dansant. Pas avec la Couronne.", "ChipiChapa", "CHIPI CHAPA") },

            // ---- les passives
            { Ability.Miroir, new Skin("Uno reverse card", "Les sorts qu'on te lance repartent sur celui qui les a lancés.", "VineBoom", "UNO REVERSE") },
            { Ability.Riposte, new Skin("Instant karma", "Celui qui te pousse est repoussé à son tour.", "VineBoom", "KARMA") },
            { Ability.SecondSouffle, new Skin("We're so back", "Quand tu tombes dans les nuages, tu repars avec des ailes d'or, protégé 6 s.", "Sheesh", "WE'RE SO BACK") },
            { Ability.Plume, new Skin("Light weight baby", "Avec la Couronne, tu voles aussi vite que les autres.", "Wow", "LIGHT WEIGHT") },
            { Ability.TeteDure, new Skin("Mewing", "Mâchoire de béton : les pièges de la tour ne te font plus tomber.", "Sheesh", "MEWING") },
            { Ability.Armure, new Skin("Plot armor", "Le premier coup que tu prends dans la manche ne te fait rien.", "Sheesh", "PLOT ARMOR") },
            { Ability.Ancrage, new Skin("Chill guy", "Quand on te pousse, tu pars 2 fois moins loin. Tranquille.", "Bruh", "CHILL GUY") },
            { Ability.Poigne, new Skin("Sigma push", "Ta poussée envoie 2 fois plus loin.", "Bonk", "SIGMA PUSH") },
            { Ability.Increvable, new Skin("Built different", "Les sorts qu'on te lance durent 2 fois moins longtemps.", "Wow", "BUILT DIFFERENT") },
            { Ability.AngeGardien, new Skin("Main character", "Une fois par manche, si tu tombes dans les nuages, tu reviens où tu étais.", "Nooo", "MAIN CHARACTER") },
            { Ability.Kamikaze, new Skin("Kaboom", "Quand on te pousse, tu exploses : ceux autour de toi s'envolent.", "VineBoom", "KABOOM") },
            { Ability.Rage, new Skin("Gamer rage", "Chaque coup que tu prends rend ta prochaine poussée plus forte.", "Fahhh", "RAGE") },
            { Ability.Ninja, new Skin("Hide and seek", "Reste sans bouger 1 seconde : tu deviens invisible.", "Wow", "CACHÉ") },
            { Ability.Vampire, new Skin("Sigma grindset", "Chaque fois que tu frappes quelqu'un, tu cours plus vite 3 secondes.", "Sigma", "GRINDSET") },
            { Ability.Chanceux, new Skin("W", "Une fois sur trois, ta capacité revient tout de suite.", "Sheesh", "W") },
            { Ability.Pickpocket, new Skin("Sneaky", "Quand tu prends la Couronne à quelqu'un, tu deviens invisible 2 s.", "FanumTax", "SNEAKY") },
            { Ability.Coureur, new Skin("Speedrunner", "Tu cours un peu plus vite que les autres.", "Tralalero", "SPEEDRUN") },
            { Ability.Sprinter, new Skin("Gotta go fast", "Au début de chaque manche, tu cours très vite pendant 20 secondes.", "Tralalero", "GO FAST") },
            { Ability.DoubleSaut, new Skin("Double jump", "En l'air, appuie encore sur Espace : tu sautes une 2e fois.", "Boing", "JUMP") },
            { Ability.Kangourou, new Skin("Bunny hop", "Tu sautes beaucoup plus haut.", "Boing", "BUNNY HOP") },
            { Ability.Planeur, new Skin("I believe I can fly", "Tu as toujours des ailes d'or : tu voles plus vite et plus loin.", "ShootingStars", "I CAN FLY") },
            { Ability.Rebond, new Skin("Ground pound", "Quand tu retombes de haut, tu fais une onde de choc.", "VineBoom", "GROUND POUND") },
            { Ability.Recharge, new Skin("No cooldown", "Ta capacité revient plus vite.", "Sheesh", "NO COOLDOWN") },
            { Ability.Flair, new Skin("Sus detector", "Tu vois ce qui est caché : les mines, les invisibles, à travers la fumée.", "AmongUs", "SUS") },
            { Ability.BrasLongs, new Skin("Spaghetti arms", "Ta poussée touche de plus loin : 5 m.", "Boing", "SPAGHETTI") },
            { Ability.HomeRun, new Skin("Home run", "Toutes les 4 poussées réussies, la suivante envoie 3 fois plus loin.", "Bonk", "HOME RUN") },
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
