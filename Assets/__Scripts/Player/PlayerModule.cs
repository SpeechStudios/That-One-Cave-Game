using FishNet.Object;
using UnityEngine;

public class PlayerModule : NetworkBehaviour
{
    public PlayerControllerModule Controller;
    public PlayerInteractModule Interact;
    public PlayerDragGhostModule DragGhost;
    public PlayerInventoryModule Inventory;
    public PlayerCraftingModule Crafting;
    public PlayerLoadoutModule Loadout;

    public override void OnStartServer()
    {
        base.OnStartServer();
        PlayerManager.Instance.RegisterPlayer(Owner, this);
        ServerInit();
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        PlayerManager.Instance.UnregisterPlayer(Owner);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ClientInit();
    }
    public void ServerInit()
    {
        Inventory.ServerInit();
    }
    public void ClientInit()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        Controller.enabled = true;
        Interact.enabled = true;
        DragGhost.enabled = true;
        Inventory.enabled = true;
        Crafting.enabled = true;
        Loadout.enabled = true;


        Controller.Init();
        Interact.Init();
        DragGhost.Init();
        Inventory.ClientInit();
        Crafting.Init();
        Loadout.Init();

        Inventory.RequestStart();
    }
}
