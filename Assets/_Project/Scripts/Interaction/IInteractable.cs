using UnityEngine;

namespace Horror
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(GameObject who);
    }
}
