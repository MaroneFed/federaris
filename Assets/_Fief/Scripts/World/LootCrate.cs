using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Une caisse, un ballot ou une bourse abandonnes, a fouiller.
    ///
    /// C'est la recompense de l'exploration : tu t'ecartes du chemin, tu trouves
    /// quelque chose. Le contenu reste modeste (de quoi payer un bout de mur),
    /// sinon ca court-circuiterait la boucle recolter -> vendre.
    /// </summary>
    public class LootCrate : MonoBehaviour
    {
        public int goldMin = 12;
        public int goldMax = 45;
        public ResourceType resource = ResourceType.Wood;
        public int resourceAmount = 6;

        bool looted;
        Transform lid;

        public void Initialise(Transform crateLid, System.Random rng)
        {
            lid = crateLid;
            resource = ResourceInfo.All[rng.Next(ResourceInfo.Count)];
            resourceAmount = 4 + rng.Next(9);
            goldMin = 10 + rng.Next(16);
            goldMax = goldMin + 14 + rng.Next(30);
        }

        public bool Looted { get { return looted; } }

        public void Open()
        {
            if (looted) return;
            looted = true;

            // le couvercle bascule : on voit de loin ce qu'on a deja fouille
            if (lid != null) lid.localRotation = Quaternion.Euler(-112f, 0f, 0f);

            int gold = Random.Range(goldMin, goldMax + 1);
            if (Game.Wallet != null) Game.Wallet.Add(gold);

            int taken = 0;
            if (Game.Inventory != null) taken = Game.Inventory.TryAdd(resource, resourceAmount);

            Sfx.Coin();
            FloatingTexts.Spawn(transform.position + Vector3.up * 1.4f, "+" + gold + " or", Palette.Gold);
            if (taken > 0)
            {
                FloatingTexts.Spawn(transform.position + Vector3.up * 2.1f,
                                    "+" + taken + " " + ResourceInfo.Name(resource), ResourceInfo.Tint(resource));
            }

            string message = "Butin : " + gold + " or";
            if (taken > 0) message += " et " + taken + " " + ResourceInfo.Name(resource);
            else if (resourceAmount > 0) message += " (sac trop plein pour le reste)";
            Toasts.Show(message, Palette.Gold);
        }
    }

    /// <summary>Le point d'interaction d'une caisse : "Fouiller", en maintenant E.</summary>
    public class LootPoint : MonoBehaviour, IInteractable
    {
        public LootCrate crate;

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return crate != null && !crate.Looted; } }
        public string Prompt { get { return "Fouiller"; } }
        public float HoldDuration { get { return 1.1f; } }

        public void Interact()
        {
            if (crate != null) crate.Open();
        }
    }
}
