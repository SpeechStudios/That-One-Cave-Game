using FishNet.Object;
using UnityEngine;

public class PlayerDragGhostModule : NetworkBehaviour
{
    [HideInInspector] public ItemSlotData ClientGhost;
    [HideInInspector] public ItemSlotData ServerGhost;
    public void ClientInit()
    {
        PlayerUIManager.Instance.UI_DragGhost.Bind(this);
    }
}
