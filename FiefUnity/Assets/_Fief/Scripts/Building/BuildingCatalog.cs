using UnityEngine;

namespace Fief
{
    public enum BuildingId
    {
        Chest = 0,
        Wall = 1,
        Sawmill = 2,
        Workshop = 3,
        Watchtower = 4
    }

    public class BuildingDef
    {
        public BuildingId id;
        public string name;
        public string effect;
        public string phaseNote;
        public int baseCost;
        public int prestige;
        public Color color;
    }

    /// <summary>
    /// Les 5 constructions de la Phase 1. Chacune a un effet qui se SENT en solo :
    /// une construction qui ne sert a rien avant la Phase 2 ne permet pas de valider
    /// la Porte 1 ("la boucle est-elle satisfaisante pendant 20 min ?").
    /// </summary>
    public static class BuildingCatalog
    {
        public static readonly BuildingDef[] All =
        {
            New(BuildingId.Chest, "Coffre", 150, 5,
                "Stocke tes ressources hors du sac : tu repars leger.",
                "Phase 2 : c'est lui que les rivaux viendront voler.",
                new Color(0.55f, 0.40f, 0.25f)),

            New(BuildingId.Wall, "Mur", 80, 3,
                "Delimite ton fief. Bon marche, rapporte du Prestige.",
                "Phase 2 : bois -> hache/feu, pierre -> belier. Destruction a regles.",
                Palette.Structure),

            New(BuildingId.Watchtower, "Tour de guet", 250, 12,
                "Revele tous les gisements de la carte sur ton ecran.",
                "Phase 2 : detectera les intrus, de nuit comme de jour.",
                new Color(0.66f, 0.62f, 0.55f)),

            New(BuildingId.Sawmill, "Scierie", 300, 10,
                "Produit 2 Bois toutes les 8 s, directement dans la reserve du fief.",
                "Phase 2 : tournera grace a des serviteurs... qu'il faudra payer.",
                new Color(0.50f, 0.44f, 0.30f)),

            New(BuildingId.Workshop, "Atelier", 400, 18,
                "-15% sur le cout de toutes tes constructions suivantes.",
                "Phase 2 : fabrication d'outils de siege.",
                new Color(0.62f, 0.50f, 0.38f))
        };

        static BuildingDef New(BuildingId id, string name, int cost, int prestige,
                               string effect, string phaseNote, Color color)
        {
            BuildingDef d = new BuildingDef();
            d.id = id;
            d.name = name;
            d.baseCost = cost;
            d.prestige = prestige;
            d.effect = effect;
            d.phaseNote = phaseNote;
            d.color = color;
            return d;
        }

        public static BuildingDef Get(BuildingId id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].id == id) return All[i];
            return All[0];
        }
    }
}
