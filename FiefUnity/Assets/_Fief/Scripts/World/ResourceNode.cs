using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Un gisement : arbre, rocher ou veine de fer.
    /// On maintient E a cote pour recolter. Le noeud s'epuise, retrecit, disparait,
    /// puis reapparait apres un delai. C'est ce qui evite de camper un seul arbre.
    /// </summary>
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        /// <summary>Registre de tous les noeuds vivants (la Tour de guet s'en sert pour les afficher).</summary>
        public static readonly List<ResourceNode> All = new List<ResourceNode>();

        public ResourceType type = ResourceType.Wood;
        public int capacity = 24;
        public int yieldPerHarvest = 3;
        public float harvestDuration = 1.1f;
        public float respawnDelay = 45f;

        int remaining;
        float respawnTimer;
        Transform visual;
        Vector3 visualScale = Vector3.one;

        public int Remaining { get { return remaining; } }
        public bool IsDepleted { get { return remaining <= 0; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Initialise(ResourceType resourceType, int nodeCapacity, Transform visualRoot)
        {
            type = resourceType;
            capacity = Mathf.Max(1, nodeCapacity);
            remaining = capacity;
            visual = visualRoot;
            if (visual != null) visualScale = visual.localScale;
        }

        void Update()
        {
            if (remaining > 0) return;

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                remaining = capacity;
                Toasts.Show(ResourceInfo.Name(type) + " : un gisement a repousse", ResourceInfo.Tint(type));
            }
            ApplyVisual();
        }

        // --- IInteractable ---

        public Transform Anchor { get { return transform; } }

        public bool CanInteract { get { return remaining > 0; } }

        public string Prompt
        {
            get
            {
                if (Game.Inventory != null && Game.Inventory.SpaceFor(type) <= 0)
                    return "Sac plein - va vendre au marche";
                return "Recolter du " + ResourceInfo.Name(type) + "  (" + remaining + " restant)";
            }
        }

        public float HoldDuration
        {
            get
            {
                // Sac plein : pas de maintien, on veut un retour immediat plutot qu'une attente inutile.
                if (Game.Inventory != null && Game.Inventory.SpaceFor(type) <= 0) return 0f;
                return harvestDuration;
            }
        }

        public void Interact()
        {
            if (remaining <= 0) return;

            Inventory inv = Game.Inventory;
            if (inv == null) return;

            if (inv.SpaceFor(type) <= 0)
            {
                Toasts.Show("Sac plein (" + Mathf.RoundToInt(inv.Weight) + " kg) - direction le marche", Palette.Iron);
                return;
            }

            int wanted = Mathf.Min(yieldPerHarvest, remaining);
            int added = inv.TryAdd(type, wanted);
            if (added <= 0)
            {
                Toasts.Show("Sac plein", Palette.Iron);
                return;
            }

            remaining -= added;
            if (remaining <= 0)
            {
                remaining = 0;
                respawnTimer = respawnDelay;
            }

            ApplyVisual();
            Toasts.Show("+" + added + " " + ResourceInfo.Name(type), ResourceInfo.Tint(type));
        }

        /// <summary>Le gisement retrecit a mesure qu'on le vide : lisible sans aucune UI.</summary>
        void ApplyVisual()
        {
            if (visual == null) return;

            if (remaining <= 0)
            {
                if (visual.gameObject.activeSelf) visual.gameObject.SetActive(false);
                return;
            }

            if (!visual.gameObject.activeSelf) visual.gameObject.SetActive(true);
            float ratio = Mathf.Clamp01((float)remaining / capacity);
            float scale = Mathf.Lerp(0.55f, 1f, ratio);
            visual.localScale = new Vector3(visualScale.x * scale, visualScale.y * scale, visualScale.z * scale);
        }
    }
}
