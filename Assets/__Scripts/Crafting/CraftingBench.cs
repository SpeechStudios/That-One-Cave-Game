using FishNet.Object;
using UnityEngine;

public class CraftingBench : MonoBehaviour, IInteractable
{

    [Client]
    public void Interact(NetworkObject player)
    {
        CraftingManager.Instance.Open();
    }
    [Client]
    public void CloseInteraction()
    {
        CraftingManager.Instance.Close();
    }

}
