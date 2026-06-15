using FishNet.Object;
using UnityEngine;

public interface IInteractable
{
    void Interact(NetworkObject player);

    void CloseInteraction();
}
