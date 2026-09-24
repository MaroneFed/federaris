using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE CACHE dans le monde : une butte de terre fraiche, une pierre plate dessus.
    ///
    /// Elle est faite pour ne PAS se voir : basse, de la couleur du sol, sans lumiere.
    /// Seul son proprietaire sait ou elle est (le HUD lui montre ses caches, et a
    /// personne d'autre). Dans une foret sans reperes, la retrouver est deja un jeu.
    ///
    /// Comme le camp, cet objet ne garde rien : le contenu vit dans Cache, une classe
    /// C# pure, rangee dans Hoard.
    /// </summary>
    public class CacheSpot : MonoBehaviour, IInteractable
    {
        public Cache cache;

        static readonly Color Earth = new Color(0.17f, 0.14f, 0.11f);
        static readonly Color EarthDark = new Color(0.12f, 0.10f, 0.08f);
        static readonly Color Lid = new Color(0.25f, 0.25f, 0.25f);

        public static CacheSpot Build(Cache cache, float yaw)
        {
            GameObject root = new GameObject("CACHE " + cache.Number);
            root.transform.position = cache.Position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = root.transform;

            // Un declencheur : on la trouve avec E, on marche dessus sans buter.
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.25f, 0f);
            trigger.size = new Vector3(1.4f, 0.6f, 1.4f);

            Proto.BeginVisualOnly();
            GameObject mound = Proto.Cube(t, new Vector3(0f, 0.04f, 0f), new Vector3(1.3f, 0.2f, 1.1f), Earth, "Butte");
            mound.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            Proto.Cube(t, new Vector3(0.1f, 0.06f, -0.1f), new Vector3(0.9f, 0.18f, 0.8f), EarthDark, "Terre");
            GameObject lid = Proto.Cube(t, new Vector3(-0.05f, 0.17f, 0.05f), new Vector3(0.62f, 0.07f, 0.5f), Lid, "Pierre");
            lid.transform.localRotation = Quaternion.Euler(3f, 34f, -2f);
            Proto.EndVisualOnly();

            CacheSpot spot = root.AddComponent<CacheSpot>();
            spot.cache = cache;
            return spot;
        }

        // ------------------------------------------------------------------ IInteractable

        public Transform Anchor { get { return transform; } }

        public bool CanInteract { get { return cache != null; } }

        public string Prompt
        {
            get
            {
                if (cache == null) return "";
                return "Ta cache " + cache.Number + "   (" + Mathf.RoundToInt(cache.Contents.Weight) + " / "
                       + Mathf.RoundToInt(cache.Contents.MaxWeight) + " kg)";
            }
        }

        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (Game.Hud == null || cache == null) return;
            Game.Hud.OpenPanel(new StashPanel(cache, transform, "CACHE " + cache.Number));
            Sfx.Pop();
        }
    }
}
