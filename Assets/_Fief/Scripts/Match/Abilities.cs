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
        // --- passives (DoubleSaut est la premiere : IsActive s'en sert)
        DoubleSaut, Planeur, Coureur, Porteur, Poigne, Ancrage, Flair, Ombre, PriseFerme, Recharge, Rebond, Aimant,
        // (05/10) les passives neuves
        Riposte, Vampire, TeteDure, SecondSouffle, Plume
    }

    public static class AbilityInfo
    {
        public const int Count = 39;
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
                default: return "Aimant";
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
                default: return "La Couronne à terre vole jusqu'à toi.";
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
                default: return 0f;
            }
        }

        /// <summary>Une couleur par capacite : celle de son nom a l'ecran et de son eclat dans le monde.</summary>
        public static Color Tint(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: case Ability.Clignement: case Ability.Coureur: return new Color(1f, 0.62f, 0.3f);
                case Ability.Grappin: case Ability.Crochet: case Ability.Aimant: return new Color(0.95f, 0.85f, 0.45f);
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
                case Ability.Riposte: case Ability.Vampire: return new Color(0.95f, 0.25f, 0.3f);
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }
    }
}
