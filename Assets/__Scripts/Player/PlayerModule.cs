using FishNet.Object;
using UnityEngine;

public class PlayerModule : NetworkBehaviour
{
    public PlayerControllerModule Controller;
    public PlayerDragGhostModule DragGhost;
    public PlayerInventoryModule Inventory;
    public PlayerCraftingModule Crafting;
    public PlayerSmeltingModule Smelting;
    public PlayerLoadoutModule Loadout;
    public PlayerStatsModule Stats;
    public CamFollowPlayer CameraFollow;
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
        Crafting.ServerInit();
        Smelting.ServerInit();
        Stats.ServerInit();
        Loadout.ClientInit();
    }
    public void ClientInit()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        CameraFollow.ClientInit();
        Stats.ClientInit();
        Stats.HealthBar.SetActive(false);
        Controller.enabled = true;
        DragGhost.enabled = true;
        Inventory.enabled = true;
        Crafting.enabled = true;
        Smelting.enabled = true;
        Loadout.enabled = true;
        Stats.enabled = true;

        Controller.ClientInit();
        DragGhost.ClientInit();
        Inventory.ClientInit();
        Smelting.ClientInit();
        Crafting.ClientInit();
        Loadout.ClientInit();

        Inventory.RequestStart();
    }
}
