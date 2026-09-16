using UnityEngine;

namespace Fief
{
    /// <summary>
    /// Tout ce avec quoi on peut interagir avec la touche E implemente cette interface :
    /// un arbre, le marche, un emplacement de construction, un coffre.
    ///
    /// Concept C# utile ici : le joueur ne connait AUCUNE de ces classes. Il ne connait
    /// que l'interface. On peut donc ajouter un nouvel interactif (un PNJ serviteur en
    /// Phase 2, par exemple) sans toucher une ligne du joueur.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Le point du monde utilise pour mesurer la distance au joueur.</summary>
        Transform Anchor { get; }

        /// <summary>Faux = on ne propose meme pas l'interaction (noeud epuise, etc.).</summary>
        bool CanInteract { get; }

        /// <summary>Texte affiche a l'ecran, ex : "Recolter du Bois".</summary>
        string Prompt { get; }

        /// <summary>0 = action instantanee. Sinon, duree de maintien de la touche E.</summary>
        float HoldDuration { get; }

        void Interact();
    }
}
