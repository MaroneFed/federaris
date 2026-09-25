using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LES CAPACITES (26/09, Martin : "pas d'epee, juste des capacites, on tiendra jamais
    /// rien en main", "trouve une liste de plein de capacites"). On en choisit une avant
    /// la premiere manche, puis une entre chaque manche ; on les garde tout le match.
    ///
    ///   ACTIVES   une touche chacune (clic droit, R, C), et un temps de recharge.
    ///             Trois au plus : la quatrieme remplace la plus ancienne.
    ///   PASSIVES  toujours la, sans touche.
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
        // --- passives
        DoubleSaut, Planeur, Coureur, Porteur, Poigne, Ancrage, Flair, Ombre, PriseFerme, Recharge, Rebond, Aimant
    }

    public static class AbilityInfo
    {
        public const int Count = 26;
        public const int MaxActives = 3;

        /// <summary>Les touches des capacites actives, dans l'ordre ou on les a prises. "V" : le don d'un sanctuaire.</summary>
        public static readonly string[] Keys = { "Clic droit", "R", "C" };
        public const string GiftKey = "V";

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
                default: return "Aimant";
            }
        }

        /// <summary>Ce que ca fait, en une phrase de tous les jours.</summary>
        public static string Line(Ability a)
        {
            switch (a)
            {
                case Ability.Ruee: return "Un bond de huit mètres droit devant toi.";
                case Ability.Grappin: return "Vise un mur, un arbre, la tour : le grappin t'y tire.";
                case Ability.Crochet: return "Vise un joueur : il est tiré jusqu'à toi.";
                case Ability.Onde: return "Projette tout le monde autour de toi.";
                case Ability.Clignement: return "Tu disparais et réapparais dix mètres plus loin.";
                case Ability.Bond: return "Un saut immense, droit vers le ciel.";
                case Ability.Mur: return "Un mur de pierre surgit devant toi pendant huit secondes.";
                case Ability.Nuee: return "Un nuage de fumée : les Yeux et les autres ne voient plus rien.";
                case Ability.Mine: return "Pose une mine : qui marche dessus s'envole et lâche la Couronne.";
                case Ability.Gel: return "Lance une boule de givre : ceux qu'elle touche sont ralentis.";
                case Ability.Voile: return "Tu deviens invisible pendant six secondes.";
                case Ability.Echange: return "Vise un joueur : vous échangez vos places.";
                case Ability.Rappel: return "Tu reviens là où tu étais il y a quatre secondes.";
                case Ability.Souffle: return "Une rafale repousse tout ce qui est devant toi.";
                case Ability.DoubleSaut: return "Appuie encore sur Espace en l'air : un second saut.";
                case Ability.Planeur: return "Maintiens Espace en l'air : tu planes. La Couronne ne tombe pas.";
                case Ability.Coureur: return "Tu vas quinze pour cent plus vite.";
                case Ability.Porteur: return "Avec la Couronne, tu n'es plus ralenti et tu peux pousser.";
                case Ability.Poigne: return "Ta poussée envoie deux fois plus loin.";
                case Ability.Ancrage: return "On te pousse deux fois moins loin.";
                case Ability.Flair: return "Tu vois toujours où est la Couronne, même à travers les murs.";
                case Ability.Ombre: return "Les Yeux mettent deux fois plus de temps à te repérer.";
                case Ability.PriseFerme: return "Le premier coup ne te fait pas lâcher la Couronne.";
                case Ability.Recharge: return "Tes capacités reviennent un tiers plus vite.";
                case Ability.Rebond: return "Retomber de haut fait une onde de choc autour de toi.";
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
                case Ability.Souffle: return 7f;
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
                default: return new Color(1f, 0.85f, 0.4f);
            }
        }
    }
}
