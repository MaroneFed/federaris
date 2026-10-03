using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CAPACITES (26/09, Martin : "pas d'epee, juste des capacites, on tiendra jamais
    /// rien en main", "trouve une liste de plein de capacites"). Avant CHAQUE manche, on
    /// en choisit deux, neuves (29/09 : "qu'un passif et un clic gauche") :
    ///
    ///   UNE ACTIVE   sur le clic gauche (Reglages > Touche capacite), un temps de recharge ;
    ///                le don d'un sanctuaire la remplace pour la manche.
    ///   UNE PASSIVE  toujours la, sans touche.
    ///
    /// Inspirees des jeux qui ont fait leurs preuves : la ruee et le clignement
    /// (Overwatch), le grappin (Apex, Sekiro), le crochet (Overwatch), l'onde et le
    /// souffle (Smash), le planeur (Zelda), le rappel (Tracer), le mur (Fortnite).
    ///
    /// Classe C# pure : ce qu'elles FONT est dans Player/AbilityUser.cs (toi) et
    /// World/Rival.cs (les bots), par les memes methodes (voir World/Combat.cs).
    /// </summary>
    public enum Ability
    {
        // --- actives
        Ruee, Grappin, Crochet, Onde, Clignement, Bond, Mur, Nuee, Mine, Gel, Voile, Echange, Rappel, Souffle,
        // (05/10 -- "des capacites de malade mental") les actives neuves
        Meteore, Tornade, TrouNoir, Boulet, Geant, Fusee, Ressort, Foudre,
        // (06/10 -- "une prison qui t'enchaine au sol dix secondes, plein de conneries comme ca,
        // des trucs de fou, il m'en faut une vingtaine") les actives de fou
        Prison, Bombe, Inversion, Mini, Glu, Banane, Ballon, Seisme, Gant, Fantome, Taupe, Deluge, Toupie, Encre,
        // (07/10 -- "encore plus de capas, plus, plus, plus") la deuxieme fournee
        Lasso, Missile, Apesanteur, Flammes, Pogo, Geyser, Boomerang, PiegeLoup, Catapulte, Oreillers, Hypnose, Raz, CoupDePied, Cri,
        // (08/10 -- "une version God Mode, que des capacites de malade mental") : les DIVINES,
        // seulement en Mode Dieu
        Apocalypse, ArretTemps, Rayon, Teleport, Tempete, Nuke, MainDeDieu, Essaim, GraviteZero, Invincible,
        // (08/10 au soir -- "encore plus de dinguerie") la deuxieme fournee divine
        Bombardement, Singularite, Dragon, Comete, Cataclysme, Chaos, AnneauFeu, Geole, Tsunami, FoudreChaine,
        // (09/10, v31 -- "encore plus de capacites divines, des trucs de malade") la troisieme fournee divine
        Armee, Volcan, FrappeOrbitale, Rocher, Lune, Ouragan, FrappeCiel, Enclumes, Lilliput, Demence,
        // --- passives (DoubleSaut est la premiere : IsActive s'en sert)
        DoubleSaut, Planeur, Coureur, Porteur, Poigne, Ancrage, Flair, Ombre, PriseFerme, Recharge, Rebond,
        // (05/10) les passives neuves
        Riposte, Vampire, TeteDure, SecondSouffle, Plume,
        // (06/10) et encore -- l'AIMANT est parti (Martin : "un truc qui TP la couronne vers toi,
        // c'est n'importe quoi, il faut quand meme monter la tour")
        BrasLongs, Kangourou, Kamikaze, AngeGardien, Rage, Ninja,
        Miroir, Increvable, Pickpocket, Chanceux, Armure, Sprinter,
        // (08/10) les passives divines (Mode Dieu)
        Colosse, Eclair, MainLourde, Phenix, Sablier,
        Explosif, Lave, Echo,
        Orage, Orbes, Titan
    }

    public static class AbilityInfo
    {
        public const int Count = 119;

        /// <summary>(08/10) Une capacite DIVINE : seulement en Mode Dieu.</summary>
        public static bool IsGod(Ability a) { return a >= Ability.Apocalypse && a <= Ability.Demence || a >= Ability.Colosse; }

        /// <summary>
        /// (08/10) LA TABLE DU MODE DIEU : les divines, et les plus folles des autres. Rien de
        /// "petit" (pas de double saut, pas de coureur) : que des trucs de malade mental.
        /// </summary>
        public static bool InGodPool(Ability a)
        {
            // (10/10 -- Martin : "il faut pas teleporter") : plus de Teleportation ni de Chaos
            // (qui teleporte tout le monde) en Mode Dieu.
            if (a == Ability.Teleport || a == Ability.Chaos) return false;
            if (IsGod(a)) return true;
            switch (a)
            {
                case Ability.Meteore: case Ability.Tornade: case Ability.TrouNoir: case Ability.Boulet: case Ability.Geant:
                case Ability.Foudre: case Ability.Prison: case Ability.Bombe: case Ability.Ballon: case Ability.Seisme:
                case Ability.Gant: case Ability.Deluge: case Ability.Missile: case Ability.Raz: case Ability.Lasso:
                case Ability.Geyser: case Ability.Hypnose: case Ability.Souffle: case Ability.Apesanteur:
                case Ability.Kamikaze: case Ability.Riposte: case Ability.Miroir: case Ability.Rage: case Ability.Vampire:
                case Ability.Planeur: case Ability.Poigne: case Ability.TeteDure:
                    return true;
                default: return false;
            }
        }

        /// <summary>Peut-on la tirer dans ce match (Mode Dieu : la table divine ; sinon : tout sauf les divines) ?</summary>
        public static bool Allowed(Ability a) { return Match.GodMode ? InGodPool(a) : !IsGod(a); }
        /// <summary>29/09 (Martin : "qu'on n'ait qu'un passif et un clic gauche, pas d'autres conneries") : une seule active.</summary>
        public const int MaxActives = 1;

        /// <summary>Les touches des capacites actives, dans l'ordre ou on les a prises. "V" : le don d'un sanctuaire.</summary>
        public static string[] Keys { get { return new[] { FiefInput.BindNames[Settings.ActiveBind] }; } }
        /// <summary>La touche pour pousser (clic droit par defaut ; voir Settings.PushBind).</summary>
        public static string PushKey { get { return FiefInput.BindNames[Settings.PushBind]; } }
        public const string UseKey = "E";

        public static bool IsActive(Ability a) { return a < Ability.DoubleSaut; }

        public static string Name(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: return "Ruée";
                case Ability.Grappin: return "Grappin";
                case Ability.Crochet: return "Crochet";
                case Ability.Onde: return "Onde de choc";
                case Ability.Clignement: return "Clignement";
                case Ability.Bond: return "Bond";
                case Ability.Mur: return "Mur";
                case Ability.Nuee: return "Fumée";
                case Ability.Mine: return "Mine";
                case Ability.Gel: return "Boule de glace";
                case Ability.Voile: return "Invisibilité";
                case Ability.Echange: return "Échange";
                case Ability.Rappel: return "Rappel";
                case Ability.Souffle: return "Souffle";
                case Ability.DoubleSaut: return "Double saut";
                case Ability.Planeur: return "Planeur";
                case Ability.Coureur: return "Coureur";
                case Ability.Porteur: return "Mains libres";
                case Ability.Poigne: return "Grosse poussée";
                case Ability.Ancrage: return "Poids lourd";
                case Ability.Flair: return "Œil de lynx";
                case Ability.Ombre: return "Discret";
                case Ability.PriseFerme: return "Bien accroché";
                case Ability.Recharge: return "Recharge rapide";
                case Ability.Rebond: return "Atterrissage choc";
                case Ability.Meteore: return "Météore";
                case Ability.Tornade: return "Tornade";
                case Ability.TrouNoir: return "Trou noir";
                case Ability.Boulet: return "Boulet de canon";
                case Ability.Geant: return "Géant";
                case Ability.Fusee: return "Fusée";
                case Ability.Ressort: return "Trampoline";
                case Ability.Foudre: return "Foudre";
                case Ability.Riposte: return "Riposte";
                case Ability.Vampire: return "Vampire";
                case Ability.TeteDure: return "Tête dure";
                case Ability.SecondSouffle: return "Second souffle";
                case Ability.Plume: return "Couronne légère";
                case Ability.Prison: return "Prison";
                case Ability.Bombe: return "Bombe collante";
                case Ability.Inversion: return "Tête à l'envers";
                case Ability.Mini: return "Mini";
                case Ability.Glu: return "Colle";
                case Ability.Banane: return "Peau de banane";
                case Ability.Ballon: return "Ballon";
                case Ability.Seisme: return "Séisme";
                case Ability.Gant: return "Gant de boxe";
                case Ability.Fantome: return "Fantôme";
                case Ability.Taupe: return "Taupe";
                case Ability.Deluge: return "Déluge";
                case Ability.Toupie: return "Toupie";
                case Ability.Encre: return "Encre";
                case Ability.BrasLongs: return "Bras longs";
                case Ability.Kangourou: return "Kangourou";
                case Ability.Kamikaze: return "Kamikaze";
                case Ability.AngeGardien: return "Ange gardien";
                case Ability.Rage: return "Rage";
                case Ability.Ninja: return "Ninja";
                case Ability.Lasso: return "Lasso";
                case Ability.Missile: return "Missile";
                case Ability.Apesanteur: return "Flottaison";
                case Ability.Flammes: return "Traînée de feu";
                case Ability.Pogo: return "Pogo";
                case Ability.Geyser: return "Geyser";
                case Ability.Boomerang: return "Boomerang";
                case Ability.PiegeLoup: return "Piège à loup";
                case Ability.Catapulte: return "Catapulte";
                case Ability.Oreillers: return "Bataille d'oreillers";
                case Ability.Hypnose: return "Hypnose";
                case Ability.Raz: return "Raz-de-marée";
                case Ability.CoupDePied: return "Coup de pied";
                case Ability.Cri: return "Cri";
                case Ability.Miroir: return "Miroir";
                case Ability.Increvable: return "Résistant";
                case Ability.Pickpocket: return "Pickpocket";
                case Ability.Chanceux: return "Chanceux";
                case Ability.Armure: return "Armure";
                case Ability.Sprinter: return "Sprinter";
                case Ability.Apocalypse: return "Apocalypse";
                case Ability.ArretTemps: return "Le temps s'arrête";
                case Ability.Rayon: return "Rayon divin";
                case Ability.Teleport: return "Téléportation";
                case Ability.Tempete: return "Tempête";
                case Ability.Nuke: return "Bombe atomique";
                case Ability.MainDeDieu: return "Main de Dieu";
                case Ability.Essaim: return "Essaim de missiles";
                case Ability.GraviteZero: return "Gravité zéro";
                case Ability.Invincible: return "Invincible";
                case Ability.Colosse: return "Colosse";
                case Ability.Eclair: return "Vitesse éclair";
                case Ability.MainLourde: return "Poussée géante";
                case Ability.Phenix: return "Phénix";
                case Ability.Sablier: return "Recharge éclair";
                case Ability.Bombardement: return "Bombardement";
                case Ability.Singularite: return "Trou noir géant";
                case Ability.Dragon: return "Souffle du dragon";
                case Ability.Comete: return "Comète";
                case Ability.Cataclysme: return "Cataclysme";
                case Ability.Chaos: return "Chaos";
                case Ability.AnneauFeu: return "Anneau de feu";
                case Ability.Geole: return "Prison géante";
                case Ability.Tsunami: return "Tsunami";
                case Ability.FoudreChaine: return "Foudre en chaîne";
                case Ability.Explosif: return "Explosif";
                case Ability.Lave: return "Corps de lave";
                case Ability.Armee: return "Armée de haricots";
                case Ability.Volcan: return "Volcan";
                case Ability.FrappeOrbitale: return "Frappe orbitale";
                case Ability.Rocher: return "Rocher géant";
                case Ability.Lune: return "Chute de la lune";
                case Ability.Ouragan: return "Ouragan";
                case Ability.FrappeCiel: return "Frappe du ciel";
                case Ability.Enclumes: return "Pluie d'enclumes";
                case Ability.Lilliput: return "Rétrécissement";
                case Ability.Demence: return "Folie";
                case Ability.Orage: return "Orage";
                case Ability.Orbes: return "Orbes de feu";
                case Ability.Titan: return "Pas de titan";
                default: return "Double lancer";
            }
        }

        /// <summary>Ce que ca fait, en une phrase de tous les jours.</summary>
        public static string Line(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: return "Tu fonces 15 m droit devant. Ceux sur ton chemin sont bousculés.";
                case Ability.Grappin: return "Vise un mur ou la tour, jusqu'à 60 m : une corde t'y tire.";
                case Ability.Crochet: return "Vise un joueur, jusqu'à 40 m : tu le tires jusqu'à toi.";
                case Ability.Onde: return "Une explosion autour de toi : tout le monde à 11 m s'envole.";
                case Ability.Clignement: return "Tu disparais et tu réapparais 20 m plus loin.";
                case Ability.Bond: return "Tu sautes très haut. Ceux qui sont collés à toi sont repoussés.";
                case Ability.Mur: return "Un mur sort du sol devant toi. Ceux qui sont dessus s'envolent.";
                case Ability.Nuee: return "Un gros nuage de fumée : personne ne voit à travers, ni les gargouilles.";
                case Ability.Mine: return "Tu poses une mine. Le premier qui passe à côté saute en l'air.";
                case Ability.Gel: return "Tu lances une boule de glace : ceux qu'elle touche sont poussés et ralentis.";
                case Ability.Voile: return "Tu deviens invisible pendant 7 secondes.";
                case Ability.Echange: return "Vise un joueur, jusqu'à 45 m : vous échangez vos places.";
                case Ability.Rappel: return "Tu reviens là où tu étais il y a 4 secondes.";
                case Ability.Souffle: return "Un coup de vent traverse toute l'île et pousse tout le monde.";
                case Ability.DoubleSaut: return "En l'air, appuie encore sur Espace : tu sautes une 2e fois.";
                case Ability.Planeur: return "Tu as toujours des ailes d'or : tu voles plus vite et plus loin.";
                case Ability.Coureur: return "Tu cours un peu plus vite que les autres.";
                case Ability.Porteur: return "Quand tu as la Couronne, tu n'es plus ralenti et tu peux pousser.";
                case Ability.Poigne: return "Ta poussée envoie 2 fois plus loin et revient plus vite.";
                case Ability.Ancrage: return "Quand on te pousse, tu pars 2 fois moins loin.";
                case Ability.Flair: return "Tu vois ce qui est caché : les mines, les invisibles, à travers la fumée.";
                case Ability.Ombre: return "Les gargouilles mettent 2 fois plus de temps à te viser.";
                case Ability.PriseFerme: return "Le premier coup que tu prends ne te fait pas lâcher la Couronne.";
                case Ability.Recharge: return "Ta capacité revient plus vite.";
                case Ability.Rebond: return "Quand tu retombes de haut, tu fais une onde de choc.";
                case Ability.Meteore: return "Tu sautes, puis tu t'écrases au sol : tout le monde à 10 m s'envole.";
                case Ability.Tornade: return "Une tornade part devant toi et envoie tout le monde en l'air.";
                case Ability.TrouNoir: return "Un trou noir aspire tout le monde devant toi, puis explose.";
                case Ability.Boulet: return "Tu fonces comme un boulet de canon sur 30 m et tu renverses tout.";
                case Ability.Geant: return "Tu deviens géant 7 secondes : on ne peut plus te pousser.";
                case Ability.Fusee: return "Tu décolles très haut, puis tu voles où tu veux. Pas dans le château.";
                case Ability.Ressort: return "Un trampoline apparaît à tes pieds. Tout le monde peut sauter dessus.";
                case Ability.Foudre: return "Vise un joueur : la foudre tombe sur lui un instant après.";
                case Ability.Riposte: return "Celui qui te pousse est repoussé à son tour.";
                case Ability.Vampire: return "Chaque fois que tu frappes quelqu'un, tu cours plus vite 3 secondes.";
                case Ability.TeteDure: return "Les pièges de la tour ne te font plus tomber. Ils te bousculent juste.";
                case Ability.SecondSouffle: return "Quand tu tombes dans les nuages, tu repars avec des ailes d'or, protégé 6 s.";
                case Ability.Plume: return "Avec la Couronne, tu voles aussi vite que les autres.";
                case Ability.Prison: return "Vise un joueur : il est enfermé dans une cage 10 s. Un coup le libère.";
                case Ability.Bombe: return "Vise un joueur : une bombe se colle à lui et explose 2 s après.";
                case Ability.Inversion: return "Vise un joueur : ses touches sont inversées pendant 6 secondes.";
                case Ability.Mini: return "Vise un joueur : il devient tout petit, lent, et s'envole au moindre coup.";
                case Ability.Glu: return "Tu poses une flaque de colle. Ceux qui marchent dedans restent collés.";
                case Ability.Banane: return "Tu laisses 3 peaux de banane derrière toi. Ceux qui marchent dessus glissent.";
                case Ability.Ballon: return "Vise un joueur : il gonfle comme un ballon, s'envole, puis éclate.";
                case Ability.Seisme: return "Tu tapes le sol : tous ceux qui sont debout à 25 m sautent en l'air.";
                case Ability.Gant: return "Un énorme gant de boxe frappe droit devant toi.";
                case Ability.Fantome: return "Pendant 4 s, plus rien ne peut te toucher. Pas avec la Couronne.";
                case Ability.Taupe: return "Tu passes sous terre et tu ressors 20 m plus loin en renversant tout.";
                case Ability.Deluge: return "Des météores tombent là où tu vises.";
                case Ability.Toupie: return "Tu tournes sur toi-même : ceux qui te touchent sont éjectés.";
                case Ability.Encre: return "Vise un joueur : de l'encre noire lui cache l'écran 5 secondes.";
                case Ability.BrasLongs: return "Ta poussée touche de plus loin : 5 m.";
                case Ability.Kangourou: return "Tu sautes beaucoup plus haut.";
                case Ability.Kamikaze: return "Quand on te pousse, tu exploses : ceux autour de toi s'envolent.";
                case Ability.AngeGardien: return "Une fois par manche, si tu tombes dans les nuages, tu reviens où tu étais.";
                case Ability.Rage: return "Chaque coup que tu prends rend ta prochaine poussée plus forte.";
                case Ability.Ninja: return "Reste sans bouger 1 seconde : tu deviens invisible.";
                case Ability.Lasso: return "Vise un joueur : tu l'attrapes et tu le lances là où tu regardes.";
                case Ability.Missile: return "Un missile part chercher le joueur le plus proche et explose.";
                case Ability.Apesanteur: return "Ceux qui sont autour de toi flottent en l'air comme des ballons.";
                case Ability.Flammes: return "Pendant 4 s, tu laisses du feu derrière toi. Qui marche dedans est éjecté.";
                case Ability.Pogo: return "Pendant 5 s, tu rebondis très haut à chaque pas.";
                case Ability.Geyser: return "Vise un joueur : un jet d'eau sort du sol et l'envoie en l'air.";
                case Ability.Boomerang: return "Tu lances un boomerang : il frappe à l'aller et au retour.";
                case Ability.PiegeLoup: return "Tu poses un piège. Celui qui marche dessus est bloqué 3 secondes.";
                case Ability.Catapulte: return "Tu te lances très loin devant toi. Pas avec la Couronne.";
                case Ability.Oreillers: return "Tu lances 6 oreillers droit devant toi : paf, paf, paf !";
                case Ability.Hypnose: return "Vise un joueur : pendant 3 s, il marche vers toi sans pouvoir s'arrêter.";
                case Ability.Raz: return "Une vague part de toi et emporte tout le monde à 20 m.";
                case Ability.CoupDePied: return "Ta prochaine poussée envoie 3 fois plus loin.";
                case Ability.Cri: return "Tu cries très fort : ceux qui sont devant toi sont sonnés.";
                case Ability.Miroir: return "Les sorts qu'on te lance (cage, encre...) repartent sur celui qui les a lancés.";
                case Ability.Increvable: return "Les sorts qu'on te lance durent 2 fois moins longtemps.";
                case Ability.Pickpocket: return "Quand tu prends la Couronne à quelqu'un, tu deviens invisible 2 s.";
                case Ability.Chanceux: return "Une fois sur trois, ta capacité revient tout de suite.";
                case Ability.Armure: return "Le premier coup que tu prends dans la manche ne te fait rien.";
                case Ability.Sprinter: return "Au début de chaque manche, tu cours très vite pendant 20 secondes.";
                case Ability.Apocalypse: return "25 météores tombent tout autour de toi.";
                case Ability.ArretTemps: return "Tous les autres sont gelés sur place. Profites-en pour les pousser !";
                case Ability.Rayon: return "Un laser sort de tes yeux pendant 3 s et renverse tout ce que tu regardes.";
                case Ability.Teleport: return "Tu te téléportes là où tu vises. Pas dans le château, pas avec la Couronne.";
                case Ability.Tempete: return "6 tornades partent de toi dans toutes les directions.";
                case Ability.Nuke: return "Tu poses une bombe. Quelques secondes après, tout saute à 30 m.";
                case Ability.MainDeDieu: return "Vise un joueur : une main géante tombe du ciel et l'écrase.";
                case Ability.Essaim: return "8 missiles partent chercher les autres joueurs.";
                case Ability.GraviteZero: return "Tous ceux à 40 m de toi s'envolent dans le ciel.";
                case Ability.Invincible: return "6 s géant : rien ne peut te toucher. Pas avec la Couronne.";
                case Ability.Colosse: return "Tu es géant toute la manche : on ne peut presque plus te pousser.";
                case Ability.Eclair: return "Tu cours beaucoup plus vite, toute la manche.";
                case Ability.MainLourde: return "Ta poussée touche de plus loin et envoie 2 fois plus loin.";
                case Ability.Phenix: return "Chaque fois que tu tombes dans les nuages, tu reviens où tu étais.";
                case Ability.Sablier: return "Ta capacité revient 3 fois plus vite.";
                case Ability.Bombardement: return "Des bombes tombent en ligne devant toi, sur 40 m.";
                case Ability.Singularite: return "Un énorme trou noir aspire tout le monde à 45 m, puis explose.";
                case Ability.Dragon: return "Pendant 3 s, tu craches du feu devant toi.";
                case Ability.Comete: return "Une énorme comète tombe du ciel là où tu vises.";
                case Ability.Cataclysme: return "Tous ceux qui sont au sol à 100 m sont envoyés dans le ciel.";
                case Ability.Chaos: return "Tous les joueurs dehors échangent leurs places au hasard.";
                case Ability.AnneauFeu: return "Un cercle de feu autour de toi : qui le traverse est éjecté.";
                case Ability.Geole: return "Tous ceux à 50 m de toi sont enfermés dans une cage 4 s.";
                case Ability.Tsunami: return "Une vague géante part de toi et emporte tout le monde à 60 m.";
                case Ability.FoudreChaine: return "Vise un joueur : la foudre le frappe, puis frappe ceux d'à côté.";
                case Ability.Explosif: return "Quand tu pousses quelqu'un, il explose et renverse ceux d'à côté.";
                case Ability.Lave: return "Ton corps brûle : ceux qui te touchent sont éjectés.";
                case Ability.Armee: return "6 petits haricots courent vers les autres joueurs et explosent.";
                case Ability.Volcan: return "Un volcan sort du sol là où tu vises et crache de la lave 5 s.";
                case Ability.FrappeOrbitale: return "Vise un joueur : un laser tombe du ciel et le suit 4 s. Il peut le fuir.";
                case Ability.Rocher: return "Un rocher géant roule devant toi et écrase tout.";
                case Ability.Lune: return "La lune tombe du ciel là où tu vises. Son ombre montre où.";
                case Ability.Ouragan: return "Pendant 6 s, une tornade géante tourne autour de toi.";
                case Ability.FrappeCiel: return "Tu sautes dans le ciel et tu retombes là où tu regardes, avec une explosion.";
                case Ability.Enclumes: return "Une enclume tombe sur la tête de chaque autre joueur.";
                case Ability.Lilliput: return "Tous ceux à 60 m de toi deviennent tout petits 6 s.";
                case Ability.Demence: return "Tous ceux à 60 m de toi ont les touches inversées et l'écran taché d'encre.";
                case Ability.Orage: return "Toutes les 3 s, la foudre tombe sur le joueur le plus proche de toi.";
                case Ability.Orbes: return "3 boules de feu tournent autour de toi. Qui les touche est éjecté.";
                case Ability.Titan: return "Quand tu retombes de haut, le sol tremble et repousse ceux autour.";
                default: return "Ta capacité part 2 fois de suite.";
            }
        }

        /// <summary>Temps de recharge, en secondes (0 : passive).</summary>
        public static float Cooldown(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: return 6f;
                case Ability.Grappin: return 7f;
                case Ability.Crochet: return 10f;
                case Ability.Onde: return 8f;
                case Ability.Clignement: return 5f;
                case Ability.Bond: return 8f;
                case Ability.Mur: return 12f;
                case Ability.Nuee: return 14f;
                case Ability.Mine: return 9f;
                case Ability.Gel: return 8f;
                case Ability.Voile: return 16f;
                case Ability.Echange: return 14f;
                case Ability.Rappel: return 10f;
                case Ability.Souffle: return 9f;
                case Ability.Meteore: return 10f;
                case Ability.Tornade: return 12f;
                case Ability.TrouNoir: return 13f;
                case Ability.Boulet: return 9f;
                case Ability.Geant: return 18f;
                case Ability.Fusee: return 15f;
                case Ability.Ressort: return 10f;
                case Ability.Foudre: return 11f;
                case Ability.Prison: return 16f;
                case Ability.Bombe: return 12f;
                case Ability.Inversion: return 13f;
                case Ability.Mini: return 13f;
                case Ability.Glu: return 12f;
                case Ability.Banane: return 8f;
                case Ability.Ballon: return 14f;
                case Ability.Seisme: return 12f;
                case Ability.Gant: return 8f;
                case Ability.Fantome: return 16f;
                case Ability.Taupe: return 9f;
                case Ability.Deluge: return 14f;
                case Ability.Toupie: return 13f;
                case Ability.Encre: return 12f;
                case Ability.Lasso: return 11f;
                case Ability.Missile: return 12f;
                case Ability.Apesanteur: return 14f;
                case Ability.Flammes: return 12f;
                case Ability.Pogo: return 12f;
                case Ability.Geyser: return 10f;
                case Ability.Boomerang: return 8f;
                case Ability.PiegeLoup: return 10f;
                case Ability.Catapulte: return 9f;
                case Ability.Oreillers: return 9f;
                case Ability.Hypnose: return 14f;
                case Ability.Raz: return 12f;
                case Ability.CoupDePied: return 7f;
                case Ability.Cri: return 10f;
                case Ability.Apocalypse: return 20f;
                case Ability.ArretTemps: return 22f;
                case Ability.Rayon: return 16f;
                case Ability.Teleport: return 12f;
                case Ability.Tempete: return 16f;
                case Ability.Nuke: return 24f;
                case Ability.MainDeDieu: return 18f;
                case Ability.Essaim: return 16f;
                case Ability.GraviteZero: return 18f;
                case Ability.Invincible: return 22f;
                case Ability.Bombardement: return 16f;
                case Ability.Singularite: return 20f;
                case Ability.Dragon: return 14f;
                case Ability.Comete: return 16f;
                case Ability.Cataclysme: return 22f;
                case Ability.Chaos: return 20f;
                case Ability.AnneauFeu: return 16f;
                case Ability.Geole: return 22f;
                case Ability.Tsunami: return 18f;
                case Ability.FoudreChaine: return 14f;
                case Ability.Armee: return 18f;
                case Ability.Volcan: return 20f;
                case Ability.FrappeOrbitale: return 20f;
                case Ability.Rocher: return 16f;
                case Ability.Lune: return 26f;
                case Ability.Ouragan: return 20f;
                case Ability.FrappeCiel: return 16f;
                case Ability.Enclumes: return 24f;
                case Ability.Lilliput: return 18f;
                case Ability.Demence: return 20f;
                default: return 0f;
            }
        }

        /// <summary>Une couleur par capacite : celle de son nom a l'ecran et de son eclat dans le monde.</summary>
        public static Color Tint(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: case Ability.Clignement: case Ability.Coureur: return new Color(1f, 0.62f, 0.3f);
                case Ability.Grappin: case Ability.Crochet: case Ability.BrasLongs: return new Color(0.95f, 0.85f, 0.45f);
                case Ability.Onde: case Ability.Souffle: case Ability.Poigne: case Ability.Rebond: return new Color(0.95f, 0.4f, 0.35f);
                case Ability.Bond: case Ability.DoubleSaut: case Ability.Planeur: return new Color(0.55f, 0.82f, 1f);
                case Ability.Mur: case Ability.Ancrage: case Ability.PriseFerme: case Ability.Porteur: return new Color(0.8f, 0.72f, 0.6f);
                case Ability.Nuee: case Ability.Voile: case Ability.Ombre: return new Color(0.68f, 0.6f, 0.95f);
                case Ability.Mine: return new Color(1f, 0.5f, 0.2f);
                case Ability.Gel: return new Color(0.6f, 0.9f, 1f);
                case Ability.Echange: case Ability.Rappel: case Ability.Recharge: return new Color(0.6f, 1f, 0.7f);
                case Ability.Meteore: case Ability.Boulet: return new Color(1f, 0.45f, 0.18f);
                case Ability.Tornade: case Ability.Plume: case Ability.SecondSouffle: return new Color(0.7f, 0.92f, 1f);
                case Ability.TrouNoir: return new Color(0.62f, 0.32f, 1f);
                case Ability.Geant: case Ability.TeteDure: return new Color(0.95f, 0.62f, 0.4f);
                case Ability.Fusee: case Ability.Ressort: return new Color(1f, 0.38f, 0.55f);
                case Ability.Foudre: return new Color(1f, 0.95f, 0.45f);
                case Ability.Riposte: case Ability.Vampire: case Ability.Rage: case Ability.Gant: return new Color(0.95f, 0.25f, 0.3f);
                case Ability.Prison: return new Color(0.72f, 0.74f, 0.82f);
                case Ability.Bombe: case Ability.Kamikaze: case Ability.Deluge: return new Color(1f, 0.52f, 0.22f);
                case Ability.Inversion: case Ability.Toupie: return new Color(0.98f, 0.5f, 0.85f);
                case Ability.Mini: case Ability.Kangourou: return new Color(0.55f, 0.95f, 0.5f);
                case Ability.Glu: return new Color(0.6f, 0.95f, 0.35f);
                case Ability.Banane: return new Color(1f, 0.9f, 0.3f);
                case Ability.Ballon: return new Color(1f, 0.45f, 0.6f);
                case Ability.Seisme: case Ability.Taupe: return new Color(0.82f, 0.62f, 0.42f);
                case Ability.Fantome: case Ability.AngeGardien: return new Color(0.85f, 0.95f, 1f);
                case Ability.Encre: case Ability.Ninja: case Ability.Pickpocket: return new Color(0.45f, 0.42f, 0.7f);
                case Ability.Lasso: case Ability.Catapulte: case Ability.Boomerang: return new Color(0.85f, 0.68f, 0.42f);
                case Ability.Missile: case Ability.Flammes: case Ability.Cri: return new Color(1f, 0.42f, 0.2f);
                case Ability.Apesanteur: case Ability.Pogo: case Ability.Sprinter: return new Color(0.6f, 0.85f, 1f);
                case Ability.Geyser: case Ability.Raz: return new Color(0.35f, 0.75f, 1f);
                case Ability.PiegeLoup: case Ability.Armure: return new Color(0.7f, 0.72f, 0.78f);
                case Ability.Oreillers: case Ability.Hypnose: case Ability.Miroir: return new Color(0.95f, 0.7f, 1f);
                case Ability.CoupDePied: return new Color(0.95f, 0.3f, 0.3f);
                case Ability.Increvable: case Ability.Chanceux: return new Color(0.5f, 0.95f, 0.55f);
                // Les divines : l'or et le blanc, la lumiere.
                case Ability.Apocalypse: case Ability.Nuke: case Ability.Phenix: return new Color(1f, 0.55f, 0.15f);
                case Ability.ArretTemps: case Ability.Sablier: return new Color(0.55f, 0.85f, 1f);
                case Ability.Rayon: case Ability.MainDeDieu: case Ability.Invincible: case Ability.Colosse: return new Color(1f, 0.88f, 0.45f);
                case Ability.Teleport: case Ability.GraviteZero: return new Color(0.75f, 0.55f, 1f);
                case Ability.Tempete: case Ability.Eclair: return new Color(0.7f, 0.92f, 1f);
                case Ability.Essaim: case Ability.MainLourde: return new Color(1f, 0.4f, 0.3f);
                case Ability.Bombardement: case Ability.Dragon: case Ability.AnneauFeu: case Ability.Explosif: case Ability.Lave: return new Color(1f, 0.38f, 0.12f);
                case Ability.Singularite: case Ability.Chaos: case Ability.Echo: return new Color(0.7f, 0.4f, 1f);
                case Ability.Comete: case Ability.Cataclysme: return new Color(1f, 0.7f, 0.3f);
                case Ability.Geole: return new Color(0.75f, 0.78f, 0.86f);
                case Ability.Tsunami: return new Color(0.3f, 0.7f, 1f);
                case Ability.FoudreChaine: case Ability.Orage: return new Color(1f, 0.95f, 0.5f);
                case Ability.Armee: return new Color(0.5f, 0.9f, 0.4f);
                case Ability.Volcan: case Ability.Orbes: return new Color(1f, 0.32f, 0.08f);
                case Ability.FrappeOrbitale: return new Color(0.4f, 0.85f, 1f);
                case Ability.Rocher: case Ability.Titan: return new Color(0.75f, 0.62f, 0.45f);
                case Ability.Lune: return new Color(0.88f, 0.9f, 1f);
                case Ability.Ouragan: return new Color(0.6f, 0.85f, 0.95f);
                case Ability.FrappeCiel: return new Color(1f, 0.75f, 0.3f);
                case Ability.Enclumes: return new Color(0.55f, 0.58f, 0.66f);
                case Ability.Lilliput: case Ability.Demence: return new Color(0.95f, 0.45f, 0.95f);
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }
    }
}
