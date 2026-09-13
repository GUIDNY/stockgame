using UnityEngine;

namespace Echobound.Player
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact();
    }

    /// <summary>Base for anything the player can press E on. Colliders on this object or its children are found by PlayerInteractor.</summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        public abstract string Prompt { get; }
        public virtual bool CanInteract => true;
        public abstract void Interact();
    }
}
