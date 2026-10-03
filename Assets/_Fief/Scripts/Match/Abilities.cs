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
        // --- passives (DoubleSaut est la premiere : IsActive s'en sert)
        DoubleSaut, Planeur, Coureur, Porteur, Poigne, Ancrage, Flair, Ombre, PriseFerme, Recharge, Rebond,
        // (05/10) les passives neuves
        Riposte, Vampire, TeteDure, SecondSouffle, Plume,
        // (06/10) et encore -- l'AIMANT est parti (Martin : "un truc qui TP la couronne vers toi,
        // c'est n'importe quoi, il faut quand meme monter la tour")
        BrasLongs, Kangourou, Kamikaze, AngeGardien, Rage, Ninja,
        Miroir, Increvable, Pickpocket, Chanceux, Armure, Sprinter,
        // (08/10) les passives divines (Mode Dieu)
        Colosse, Eclair, MainLourde, Phenix, Sablier
    }

    public static class AbilityInfo
    {
        public const int Count = 93;

        /// <summary>(08/10) Une capacite DIVINE : seulement en Mode Dieu.</summary>
        public static bool IsGod(Ability a) { return a >= Ability.Apocalypse && a <= Ability.Invincible || a >= Ability.Colosse; }

        /// <summary>
        /// (08/10) LA TABLE DU MODE DIEU : les divines, et les plus folles des autres. Rien de
        /// "petit" (pas de double saut, pas de coureur) : que des trucs de malade mental.
        /// </summary>
        public static bool InGodPool(Ability a)
        {
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
                case Ability.Nuee: return "Nuée";
                case Ability.Mine: return "Mine";
                case Ability.Gel: return "Givre";
                case Ability.Voile: return "Voile";
                case Ability.Echange: return "Échange";
                case Ability.Rappel: return "Rappel";
                case Ability.Souffle: return "Souffle";
                case Ability.DoubleSaut: return "Double saut";
                case Ability.Planeur: return "Planeur";
                case Ability.Coureur: return "Coureur";
                case Ability.Porteur: return "Porteur";
                case Ability.Poigne: return "Poigne";
                case Ability.Ancrage: return "Ancrage";
                case Ability.Flair: return "Flair";
                case Ability.Ombre: return "Ombre";
                case Ability.PriseFerme: return "Prise ferme";
                case Ability.Recharge: return "Recharge";
                case Ability.Rebond: return "Rebond";
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
                case Ability.Plume: return "Plume";
                case Ability.Prison: return "Prison";
                case Ability.Bombe: return "Bombe collante";
                case Ability.Inversion: return "Tête à l'envers";
                case Ability.Mini: return "Mini";
                case Ability.Glu: return "Glu";
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
                case Ability.Apesanteur: return "Apesanteur";
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
                case Ability.Increvable: return "Increvable";
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
                case Ability.Eclair: return "Éclair";
                case Ability.MainLourde: return "Main lourde";
                case Ability.Phenix: return "Phénix";
                default: return "Sablier";
            }
        }

        /// <summary>Ce que ca fait, en une phrase de tous les jours.</summary>
        public static string Line(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: return "Quinze mètres d'un trait : qui est sur ta route est bousculé.";
                case Ability.Grappin: return "Vise un mur, un rebord, la tour (60 m) : le grappin t'y tire.";
                case Ability.Crochet: return "Vise un joueur, jusqu'à 40 m : il est tiré jusqu'à toi.";
                case Ability.Onde: return "Une explosion : tout le monde à onze mètres s'envole.";
                case Ability.Clignement: return "Tu disparais et réapparais vingt mètres plus loin.";
                case Ability.Bond: return "Un saut immense vers le ciel ; le souffle repousse ceux qui sont tout près.";
                case Ability.Mur: return "Un mur de dix mètres surgit devant toi ; qui est dessus s'envole.";
                case Ability.Nuee: return "Un nuage de fumée : les gargouilles et les autres ne voient plus rien.";
                case Ability.Mine: return "Pose une mine : elle envoie en l'air tous ceux qui passent à côté.";
                case Ability.Gel: return "Lance une boule de givre : l'éclat bouscule et ralentit.";
                case Ability.Voile: return "Tu deviens invisible pendant sept secondes.";
                case Ability.Echange: return "Vise un joueur, jusqu'à 45 m : vous échangez vos places.";
                case Ability.Rappel: return "Tu reviens là où tu étais il y a quatre secondes.";
                case Ability.Souffle: return "Une vague de vent qui traverse toute l'île et emporte tout le monde.";
                case Ability.DoubleSaut: return "Appuie encore sur Espace en l'air : un second saut.";
                case Ability.Planeur: return "Des ailes d'or pour toujours : tu voles plus vite et plus loin.";
                case Ability.Coureur: return "Tu vas quinze pour cent plus vite.";
                case Ability.Porteur: return "Avec la Couronne, tu n'es plus ralenti et tu peux pousser.";
                case Ability.Poigne: return "Ta poussée envoie deux fois plus loin, et revient plus vite.";
                case Ability.Ancrage: return "On te pousse deux fois moins loin.";
                case Ability.Flair: return "Tu vois l'invisible : les mines, les joueurs voilés, à travers la fumée.";
                case Ability.Ombre: return "Les gargouilles mettent deux fois plus de temps à te repérer.";
                case Ability.PriseFerme: return "Le premier coup ne te fait pas lâcher la Couronne.";
                case Ability.Recharge: return "Tes capacités reviennent un tiers plus vite.";
                case Ability.Rebond: return "Retomber de haut fait une onde de choc autour de toi.";
                case Ability.Meteore: return "Tu bondis, puis tu t'écrases comme une météorite : tout s'envole à dix mètres.";
                case Ability.Tornade: return "Une tornade file devant toi et jette tout le monde vers le ciel.";
                case Ability.TrouNoir: return "Un trou noir s'ouvre devant toi, aspire tout le monde, puis explose.";
                case Ability.Boulet: return "Tu deviens un boulet de canon : trente mètres, et qui est sur ta route décolle.";
                case Ability.Geant: return "Géant pendant sept secondes : on ne te bouge plus, et tu écrases tout.";
                case Ability.Fusee: return "Tu décolles comme une fusée, puis tu voles où tu veux (pas dans la citadelle).";
                case Ability.Ressort: return "Un trampoline à tes pieds : boing ! Et tout le monde peut s'en servir.";
                case Ability.Foudre: return "Vise un joueur : la foudre tombe là où il était, un instant après.";
                case Ability.Riposte: return "Qui te pousse se prend un retour de bâton.";
                case Ability.Vampire: return "Chaque coup que tu donnes te fait courir plus vite trois secondes.";
                case Ability.TeteDure: return "Les pièges ne t'éjectent plus de la tour : ils te bousculent, c'est tout.";
                case Ability.SecondSouffle: return "Tombé dans les nuages ? Tu repars avec des ailes d'or, protégé six secondes.";
                case Ability.Plume: return "Avec la Couronne, tu voles aussi vite que les autres.";
                case Ability.Prison: return "Vise un joueur : une cage l'enchaîne au sol dix secondes. Un coup le libère.";
                case Ability.Bombe: return "Vise un joueur : une bombe se colle à lui et explose deux secondes après.";
                case Ability.Inversion: return "Vise un joueur : ses commandes sont inversées pendant six secondes.";
                case Ability.Mini: return "Vise un joueur : il devient minuscule, lent, et part deux fois plus loin.";
                case Ability.Glu: return "Une flaque de glu devant toi : qui marche dedans est englué.";
                case Ability.Banane: return "Trois peaux de banane derrière toi : qui marche dessus fait un salto.";
                case Ability.Ballon: return "Vise un joueur : il gonfle, s'envole dans le ciel, puis éclate.";
                case Ability.Seisme: return "Tu frappes le sol : tous ceux qui sont debout à 25 m décollent.";
                case Ability.Gant: return "Un gant de boxe géant frappe devant toi : un coup monstrueux.";
                case Ability.Fantome: return "Quatre secondes fantôme : plus rien ne te touche. Pas avec la Couronne.";
                case Ability.Taupe: return "Tu plonges sous terre et ressors vingt mètres plus loin, en éjectant tout.";
                case Ability.Deluge: return "Une pluie de météores tombe là où tu vises.";
                case Ability.Toupie: return "Tu tournes comme une toupie, plus vite : qui te touche est éjecté.";
                case Ability.Encre: return "Vise un joueur : de l'encre lui couvre les yeux cinq secondes.";
                case Ability.BrasLongs: return "Ta poussée porte bien plus loin : cinq mètres.";
                case Ability.Kangourou: return "Tu sautes une fois et demie plus haut.";
                case Ability.Kamikaze: return "Quand on te pousse, tu exploses : tout le monde autour s'envole.";
                case Ability.AngeGardien: return "Une fois par manche, tomber dans les nuages te ramène où tu étais.";
                case Ability.Rage: return "Chaque coup reçu rend ta prochaine poussée plus forte.";
                case Ability.Ninja: return "Immobile une seconde, tu deviens invisible.";
                case Ability.Lasso: return "Vise un joueur : tu l'attrapes et tu le jettes là où tu regardes.";
                case Ability.Missile: return "Un missile qui poursuit le joueur le plus proche, puis explose.";
                case Ability.Apesanteur: return "Tous ceux qui sont autour de toi s'envolent comme des ballons.";
                case Ability.Flammes: return "Pendant quatre secondes, tu laisses une traînée de feu derrière toi.";
                case Ability.Pogo: return "Cinq secondes de bâton sauteur : tu rebondis très haut.";
                case Ability.Geyser: return "Vise un joueur : un geyser jaillit sous ses pieds.";
                case Ability.Boomerang: return "Un boomerang qui frappe à l'aller et au retour.";
                case Ability.PiegeLoup: return "Un piège à tes pieds : qui marche dessus est coincé trois secondes.";
                case Ability.Catapulte: return "Tu te catapultes loin devant toi. Pas avec la Couronne.";
                case Ability.Oreillers: return "Six oreillers d'affilée, droit devant : paf, paf, paf.";
                case Ability.Hypnose: return "Vise un joueur : trois secondes, il marche vers toi.";
                case Ability.Raz: return "Une vague part de toi et emporte tout jusqu'à vingt mètres.";
                case Ability.CoupDePied: return "Ta prochaine poussée envoie trois fois plus loin.";
                case Ability.Cri: return "Un cri si fort que ceux qui sont devant toi sont sonnés.";
                case Ability.Miroir: return "Les sorts qu'on te lance (prison, encre...) reviennent à l'envoyeur.";
                case Ability.Increvable: return "Les sorts qu'on te lance durent deux fois moins longtemps.";
                case Ability.Pickpocket: return "Quand tu voles la Couronne, tu disparais deux secondes.";
                case Ability.Chanceux: return "Une fois sur trois, ta capacité revient tout de suite.";
                case Ability.Armure: return "Le premier coup qu'on te donne dans la manche ne te fait rien.";
                case Ability.Sprinter: return "Les vingt premières secondes de la manche, tu cours bien plus vite.";
                case Ability.Apocalypse: return "Vingt-cinq météores s'abattent tout autour de toi.";
                case Ability.ArretTemps: return "Tous les autres sont figés trois secondes et demie. Pousse-les !";
                case Ability.Rayon: return "Un rayon de lumière balaie tout ce que tu regardes, trois secondes.";
                case Ability.Teleport: return "Tu te téléportes là où tu vises. Pas dans la citadelle, pas avec la Couronne.";
                case Ability.Tempete: return "Six tornades partent de toi dans toutes les directions.";
                case Ability.Nuke: return "Deux secondes et demie... puis tout explose à trente mètres.";
                case Ability.MainDeDieu: return "Vise un joueur : une main géante l'envoie valser dans le vide.";
                case Ability.Essaim: return "Huit missiles qui poursuivent tout le monde.";
                case Ability.GraviteZero: return "Tout le monde à quarante mètres s'envole comme des ballons.";
                case Ability.Invincible: return "Six secondes géant et intouchable. Pas avec la Couronne.";
                case Ability.Colosse: return "Toute la manche, tu es géant : on ne te bouge presque plus.";
                case Ability.Eclair: return "Tu cours une fois et demie plus vite, toute la manche.";
                case Ability.MainLourde: return "Ta poussée porte à cinq mètres et demi et envoie deux fois et demie plus loin.";
                case Ability.Phenix: return "Chaque chute dans les nuages : tu renais là où tu étais, dans une explosion.";
                default: return "Tes capacités reviennent trois fois plus vite.";
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
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }
    }
}
