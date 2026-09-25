using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Un gisement : fagot de bois mort, pierre-lune, caisse de fer ancien.
    /// On maintient E a cote pour recolter. Le noeud s'epuise, retrecit, disparait,
    /// puis reapparait apres un delai. C'est ce qui evite de camper un seul arbre.
    /// </summary>
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        /// <summary>Registre de tous les gisements vivants.</summary>
        public static readonly List<ResourceNode> All = new List<ResourceNode>();

        public ResourceType type = ResourceType.Deadwood;
        public int capacity = 24;
        public int yieldPerHarvest = 3;
        public float harvestDuration = 1.1f;
        public float respawnDelay = 45f;

        public Transform marker;
        public Transform groundRing;

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
            // respawnDelay a zero : ce gisement ne revient JAMAIS (le fer du chateau).
            if (remaining > 0 || respawnDelay <= 0f) return;

            // Pas de message quand un gisement revient : il y en a des dizaines, et
            // l'ecran annoncerait des repousses a trois cents metres dans la brume.
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f) remaining = capacity;
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
                    return "Sac plein";
                return ResourceInfo.Name(type) + "   ×" + remaining;
            }
        }

        /// <summary>
        /// LA mecanique de poids, nouvelle version : plus le sac est lourd, plus le geste
        /// est lent. On reste mobile (on peut fuir, esquiver, se battre en Phase 2),
        /// mais on devient lent a l'ouvrage. Le sac plein coute du TEMPS, pas de la liberte.
        /// </summary>
        public float HoldDuration
        {
            get
            {
                // Sac plein : pas de maintien, on veut un retour immediat plutot qu'une attente inutile.
                if (Game.Inventory == null) return harvestDuration;
                if (Game.Inventory.SpaceFor(type) <= 0) return 0f;

                float penalty = Game.Config != null ? Game.Config.actionPenaltyFull : 2.4f;
                return harvestDuration * Mathf.Lerp(1f, penalty, Game.Inventory.Load01);
            }
        }

        public void Interact()
        {
            if (remaining <= 0) return;

            Inventory inv = Game.Inventory;
            if (inv == null) return;

            if (inv.SpaceFor(type) <= 0)
            {
                Sfx.Deny();
                Hud.FlashBag();
                return;
            }

            int wanted = Mathf.Min(yieldPerHarvest, remaining);
            int added = inv.TryAdd(type, wanted);
            if (added <= 0)
            {
                Sfx.Deny();
                Hud.FlashBag();
                return;
            }

            Sfx.Harvest(type);
            Pickup.Fly(transform.position + Vector3.up * 0.5f, type, added);
            FloatingTexts.Spawn(transform.position + Vector3.up * 1.6f, "+" + added, ResourceInfo.Tint(type));

            remaining -= Mathf.Min(added, wanted);
            if (remaining <= 0)
            {
                remaining = 0;
                respawnTimer = respawnDelay;
            }

            ApplyVisual();
        }

        /// <summary>
        /// Un RIVAL se sert. Meme gisement, memes regles que toi : ce qu'il prend,
        /// tu ne le trouveras plus. C'est ce qui fait du fer du chateau une course.
        /// Passe par Inventory.TryAdd, comme tout le reste. Renvoie ce qui a ete pris.
        /// </summary>
        public int TryTakeFor(Inventory bag, int quantity)
        {
            if (bag == null || remaining <= 0 || quantity <= 0) return 0;
            int added = bag.TryAdd(type, Mathf.Min(quantity, remaining));
            if (added <= 0) return 0;
            remaining -= added;
            if (remaining <= 0)
            {
                remaining = 0;
                respawnTimer = respawnDelay;
            }
            ApplyVisual();
            return added;
        }

        /// <summary>Le gisement retrecit a mesure qu'on le vide : lisible sans aucune UI.</summary>
        void ApplyVisual()
        {
            if (visual == null) return;

            bool alive = remaining > 0;
            if (marker != null && marker.gameObject.activeSelf != alive) marker.gameObject.SetActive(alive);
            if (groundRing != null && groundRing.gameObject.activeSelf != alive) groundRing.gameObject.SetActive(alive);

            if (!alive)
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
