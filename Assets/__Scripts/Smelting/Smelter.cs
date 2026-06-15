using FishNet.Object;
using FishNet.Connection;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class SmeltingSlotData : ISlotContainer
{
    public ItemSlotData Data { get; set; }
    public int ForgeIndex;
    public int SlotIndex;
}
public class SmeltingForgeData
{
    public SmeltingRecipe CurrentRecipe;
    public float SmeltingTimer;
    public bool IsSmelting;
}

public class Smelter : NetworkBehaviour, IInteractable
{
    public List<SmeltingForgeData> ClientForges = new();
    private List<SmeltingForgeData> ServerForges = new();

    public List<SmeltingSlotData> ClientSlots = new();
    private List<SmeltingSlotData> ServerSlots = new();

    [HideInInspector] public NetworkObject Target;
    public event Action<List<SlotPatch>> OnSmeltingSlotsChanged;
    public event Action<bool> OnSmeltingValidated;
    public event Action OnSmeltingComplete;

    void Start()
    {
        int forgeCount = SmeltingManager.Instance.Forges.Count;
        for (int i = 0; i < forgeCount; i++)
        {
            ClientForges.Add(new SmeltingForgeData());
            ServerForges.Add(new SmeltingForgeData());

            ClientSlots.Add(new SmeltingSlotData { ForgeIndex = i, SlotIndex = 0, Data = new ItemSlotData() });
            ClientSlots.Add(new SmeltingSlotData { ForgeIndex = i, SlotIndex = 1, Data = new ItemSlotData() });
            ClientSlots.Add(new SmeltingSlotData { ForgeIndex = i, SlotIndex = 2, Data = new ItemSlotData() });

            ServerSlots.Add(new SmeltingSlotData { ForgeIndex = i, SlotIndex = 0, Data = new ItemSlotData() });
            ServerSlots.Add(new SmeltingSlotData { ForgeIndex = i, SlotIndex = 1, Data = new ItemSlotData() });
            ServerSlots.Add(new SmeltingSlotData { ForgeIndex = i, SlotIndex = 2, Data = new ItemSlotData() });
        }
    }
    public void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < ClientForges.Count; i++)
        {
            var forge = ClientForges[i];
            if (!forge.IsSmelting) continue;

            if (forge.SmeltingTimer < forge.CurrentRecipe.SmeltingTime)
                forge.SmeltingTimer += dt;
        }
        for (int i = 0; i < ServerForges.Count; i++)
        {
            var forge = ServerForges[i];
            if (!forge.IsSmelting) continue;
            forge.SmeltingTimer += dt;
            if (forge.SmeltingTimer > forge.CurrentRecipe.SmeltingTime)
                SmeltComplete(i);
        }
    }

    #region Client Commands
    [Client]
    public void Interact(NetworkObject player)
    {
        Target = player;
        SmeltingManager.Instance.Open(this);
    }
    [Client]
    public void CloseInteraction()
    {
        SmeltingManager.Instance.Close();
    }
    [Client]
    public void SlotToGhost(int forgeIndex, int slotIndex, int quantity)
    {
        int syncIndex = GetSyncIndex(forgeIndex, slotIndex);
        LocalResponse response = LocalSlotToGhost(ClientSlots, DragGhostManager.Instance.TargetGhost.ClientGhost, syncIndex, quantity);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_SlotToGhost_RPC(syncIndex, quantity);
    }
    [Client]
    public void GhostToSlot(int forgeIndex, int slotIndex)
    {
        int syncIndex = GetSyncIndex(forgeIndex, slotIndex);
        LocalResponse response = LocalGhostToSlot(ClientSlots, DragGhostManager.Instance.TargetGhost.ClientGhost, syncIndex);
        Debug.Log("Response = " + response.Accepted);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_GhostToSlot_RPC(syncIndex);
    }
    [Client]
    public void InstantGrab(int forgeIndex, int slotIndex)
    {
        int syncIndex = GetSyncIndex(forgeIndex, slotIndex);
        PlayerInventoryModule playerInventory = InventoryManager.Instance.TargetInventory;
        LocalResponse response = LocalInstantGrab(playerInventory.ClientSlots, ClientSlots, syncIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        playerInventory.InvokeChange(response.Patches);
        Server_InstantGrab_RPC(syncIndex);
    }
    [Client]
    public void InstantFill(int inventorySlotIndex)
    {
        PlayerInventoryModule playerInventory = InventoryManager.Instance.TargetInventory;

        LocalResponse response = LocalInstantFill(ClientForges,  ClientSlots, playerInventory.ClientSlots, inventorySlotIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        playerInventory.InvokeChange(response.Patches);
        Server_InstantFill_RPC(inventorySlotIndex);
    }
    #endregion

    #region Server RPC's
    [ServerRpc(RequireOwnership = false)]
    private void Server_SlotToGhost_RPC(int syncIndex, int quantity, NetworkConnection conn = null)
    {
        PlayerModule player = PlayerManager.Instance.GetPlayer(conn);
        LocalResponse response = LocalSlotToGhost(ServerSlots, player.DragGhost.ServerGhost, syncIndex, quantity);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches, player);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true, player);
            Observer_SyncSlots(response.Patches.Where(patch => patch.Type != SlotType.Ghost).ToArray());
        }

    }
    [ServerRpc(RequireOwnership = false)]
    private void Server_GhostToSlot_RPC(int syncIndex, NetworkConnection conn = null)
    {
        PlayerModule player = PlayerManager.Instance.GetPlayer(conn);
        LocalResponse response = LocalGhostToSlot(ServerSlots, player.DragGhost.ServerGhost, syncIndex);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches, player);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true, player);
            Observer_SyncSlots(response.Patches.Where(patch => patch.Type != SlotType.Ghost).ToArray());
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void Server_InstantGrab_RPC(int syncIndex, NetworkConnection conn = null)
    {
        PlayerModule player = PlayerManager.Instance.GetPlayer(conn);
        LocalResponse response = LocalInstantGrab(player.Inventory.ServerSlots, ServerSlots, syncIndex);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches, player);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true, player);
            Observer_SyncSlots(response.Patches.ToArray());
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void Server_InstantFill_RPC(int inventorySlotIndex, NetworkConnection conn = null)
    {
        PlayerModule player = PlayerManager.Instance.GetPlayer(conn);
        LocalResponse response = LocalInstantFill(ServerForges, ServerSlots, player.Inventory.ServerSlots, inventorySlotIndex);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches, player);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true, player);
            Observer_SyncSlots(response.Patches.ToArray());
        }
    }
    #endregion

    #region Local Functions
    private LocalResponse LocalSlotToGhost(List<SmeltingSlotData> slots, ItemSlotData ghost, int syncIndex, int quantity)
    {
        ItemSlotData slotData = slots[syncIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!SlotToGhostValid(slots, slotData, ghost, syncIndex, quantity))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = syncIndex, Data = slotData, Type = SlotType.Smelting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        ghost.ID = slotData.ID;
        ghost.Materials = slotData.Materials;
        ghost.Quantity += quantity;

        slotData.Quantity -= quantity;
        if (slotData.Quantity <= 0)
            slotData.Clear();

        patches.Add(new() { Index = syncIndex, Data = slotData, Type = SlotType.Smelting });
        patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalGhostToSlot(List<SmeltingSlotData> slots, ItemSlotData ghost, int syncIndex)
    {
        Item ghostItem = Registry.TryGetItem(ghost.ID, out var tryGhostItem) ? tryGhostItem : null;
        ItemSlotData slotData = slots[syncIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!GhostToSlotValid(slots, slotData, ghost, ghostItem, syncIndex))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = syncIndex, Data = slotData, Type = SlotType.Smelting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        if (PlayerHelperFunctions.StackingValid(ghost, slotData, ghostItem.MaxStackSize))
        {
            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(ghost, slotData, ghostItem.MaxStackSize);
            ghost.Quantity = remainder;
            slotData.Quantity = stack;
            if (ghost.Quantity <= 0)
                ghost.Clear();

            patches.Add(new() { Index = syncIndex, Data = slotData, Type = SlotType.Smelting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = true, Patches = patches };
        }

        patches.Add(new() { Index = syncIndex, Data = ghost, Type = SlotType.Smelting });
        patches.Add(new() { Data = slotData, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalInstantFill(List<SmeltingForgeData> forges, List<SmeltingSlotData> slots, List<InventorySlotData> inventorySlots, int inventorySlotIndex)
    {
        ItemSlotData inventorySlotData = inventorySlots[inventorySlotIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!InstantFillValid(inventorySlots, inventorySlotIndex, out var item))
        {
            return InvalidateInstantFill(ref patches, slots, inventorySlots, inventorySlotIndex, isClient);
        }

        //Try Stack
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].SlotIndex == 2) continue;

            ItemSlotData slotData = slots[i].Data;
            if (!PlayerHelperFunctions.StackingValid(inventorySlotData, slotData, item.MaxStackSize)) continue;

            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(inventorySlotData, slotData, item.MaxStackSize);
            slotData.Quantity = stack;
            inventorySlotData.Quantity = remainder;

            patches.Add(new SlotPatch { Index = i, Data = slotData, Type = SlotType.Smelting });
            if (inventorySlotData.Quantity <= 0)
            {
                inventorySlotData.Clear();
                patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }
        // Try Recipe Fill
        for (int i = 0; i < forges.Count; i++)
        {
            bool slot0Empty = !slots[i * 3 + 0].Data.HasItem();
            bool slot1Empty = !slots[i * 3 + 1].Data.HasItem();

            if (!slot0Empty && slot1Empty)
            {
                Item existing = Registry.GetItem(slots[i * 3 + 0].Data.ID);
                if (FormsValidRecipe(item, existing))
                {
                    ItemSlotData emptySlot = inventorySlotData;
                    inventorySlotData.Clear();
                    patches.Add(new SlotPatch { Index = (i * 3 + 1), Data = emptySlot, Type = SlotType.Smelting });
                    patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                    return new LocalResponse { Accepted = true, Patches = patches };
                }
            }
            else if (slot0Empty && !slot1Empty)
            {
                Item existing = Registry.GetItem(slots[i * 3 + 1].Data.ID);
                if (FormsValidRecipe(item, existing))
                {
                    ItemSlotData emptySlot = inventorySlotData;
                    inventorySlotData.Clear();
                    patches.Add(new SlotPatch { Index = (i * 3 + 0), Data = emptySlot, Type = SlotType.Smelting });
                    patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                    return new LocalResponse { Accepted = true, Patches = patches };
                }
            }
        }
        // Try Get Empty
        for (int i = 0; i < forges.Count; i++)
        {
            bool slot0Empty = !slots[i * 3 + 0].Data.HasItem();
            bool slot1Empty = !slots[i * 3 + 1].Data.HasItem();

            if (slot0Empty && slot1Empty)
            {
                ItemSlotData emptySlot = inventorySlotData;
                inventorySlotData.Clear();
                patches.Add(new SlotPatch { Index = (i * 3 + 0), Data = emptySlot, Type = SlotType.Smelting });
                patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }

        if(patches.Count > 0)
        {
            patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
            return new LocalResponse { Accepted = true, Patches = patches };
        }
        return InvalidateInstantFill(ref patches, slots, inventorySlots, inventorySlotIndex, isClient);
    }

    private LocalResponse LocalInstantGrab(List<InventorySlotData> inventorySlots, List<SmeltingSlotData> slots, int syncIndex)
    {
        ItemSlotData grabbedSlot = slots[syncIndex].Data;
        Item item = Registry.GetItem(grabbedSlot.ID);
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!InstantGrabValid(inventorySlots, slots, grabbedSlot, syncIndex))
        {
            return InvalidateInstantGrab(ref patches, slots, inventorySlots, syncIndex, isClient);
        } 

        // Stack Slots
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (grabbedSlot.Quantity <= 0) break;
            if (inventorySlots[i].Type != ItemSlotType.Inventory) continue;
            ItemSlotData slotData = inventorySlots[i].Data;
            if (!PlayerHelperFunctions.StackingValid(grabbedSlot, slotData, item.MaxStackSize)) continue;

            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(grabbedSlot, slotData, item.MaxStackSize);
            slotData.Quantity = stack;
            grabbedSlot.Quantity = remainder;

            patches.Add(new SlotPatch { Index = i, Data = slotData, Type = SlotType.Inventory });
            if(grabbedSlot.Quantity <= 0)
            {
                grabbedSlot.Clear();
                patches.Add(new SlotPatch { Index = syncIndex, Data = grabbedSlot, Type = SlotType.Smelting });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }

        //Find Empty Slot
        int emptySlot = inventorySlots.FindIndex(s => s.Type == ItemSlotType.Inventory && !s.Data.HasItem());
        if (emptySlot < 0 && patches.Count == 0)
        {
            return InvalidateInstantGrab(ref patches, slots, inventorySlots, syncIndex, isClient);
        }
        if (emptySlot >= 0)
        {
            ItemSlotData slotData = grabbedSlot;
            grabbedSlot.Clear();
            patches.Add(new SlotPatch { Index = syncIndex, Data = grabbedSlot, Type = SlotType.Smelting });
            patches.Add(new SlotPatch { Index = emptySlot, Data = slotData, Type = SlotType.Inventory });
            return new LocalResponse { Accepted = true, Patches = patches };
        }

        if (patches.Count > 0)
        {
            patches.Add(new SlotPatch { Index = syncIndex, Data = grabbedSlot, Type = SlotType.Smelting });
            return new LocalResponse { Accepted = true, Patches = patches };
        }
        return InvalidateInstantGrab(ref patches, slots, inventorySlots, syncIndex, isClient);
    }
    private void LocalSyncSlots(List<SlotPatch> patches, bool isServer, PlayerModule player = null)
    {
        foreach (SlotPatch patch in patches)
        {
            if (patch.Type == SlotType.Ghost)
            {
                if (isServer)
                    player.DragGhost.ServerGhost = patch.Data;
                else
                    DragGhostManager.Instance.TargetGhost.ClientGhost = patch.Data;
            }
            if (patch.Type == SlotType.Smelting)
            {
                List<SmeltingSlotData> slots = isServer ? ServerSlots : ClientSlots;
                List<SmeltingForgeData> forges = isServer ? ServerForges : ClientForges;
                slots[patch.Index].Data = patch.Data;
                int forgeIndex = slots[patch.Index].ForgeIndex;
                CheckForgeReady(slots, forges[forgeIndex], forgeIndex);
            }
            if(patch.Type == SlotType.Inventory)
            {
                List<InventorySlotData> slots = isServer ? player.Inventory.ServerSlots : InventoryManager.Instance.TargetInventory.ClientSlots;
                slots[patch.Index].Data = patch.Data;
            }
        }
    }
    #endregion

    #region Observer RPC's
    [ObserversRpc]
    private void Observer_SyncSlots(SlotPatch[] patches)
    {
        LocalSyncSlots(patches.ToList(), false);
        InvokeChange(patches.ToList());
    }
    [ObserversRpc]
    private void Observer_SmeltComplete(SlotPatch[] patches, int forgeIndex)
    {
        LocalSyncSlots(patches.ToList(), false);
        ClientForges[forgeIndex].SmeltingTimer = 0;
        OnSmeltingComplete?.Invoke();
        CheckForgeReady(ClientSlots, ClientForges[forgeIndex], forgeIndex);
        InvokeChange(new List<SlotPatch>(patches));
    }
    [TargetRpc]
    private void Target_SyncSlots(NetworkConnection conn, SlotPatch[] patches)
    {
        Debug.Log("Resyncing Slots");
        LocalSyncSlots(patches.ToList(), false);
        InvokeChange(new List<SlotPatch>(patches));
    }
    #endregion

    #region Validation
    private bool SlotToGhostValid(List<SmeltingSlotData> slots, ItemSlotData slotData, ItemSlotData ghost, int syncIndex, int quantity)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, syncIndex)) return false;
        if (quantity <= 0 || quantity > slotData.Quantity) return false;
        if (!PlayerHelperFunctions.TransferValid(slotData, ghost)) return false;

        return true;
    }
    private bool GhostToSlotValid(List<SmeltingSlotData> slots, ItemSlotData slotData, ItemSlotData ghost, Item ghostItem, int syncIndex)
    {
        if (ghostItem == null) return false;
        if (ghostItem.ResourceType != ResourceType.Ore) return false;
        if (!PlayerHelperFunctions.SlotValid(slots, syncIndex)) return false;
        if (!PlayerHelperFunctions.TransferValid(ghost, slotData)) return false;
        return true;
    }
    private bool InstantFillValid(List<InventorySlotData> slots, int index, out Item inventoryItem)
    {
        if (!Registry.TryGetItem(slots[index].Data.ID, out inventoryItem)) return false;
        if (!PlayerHelperFunctions.SlotValid(slots, index)) return false;
        if (inventoryItem.ResourceType != ResourceType.Ore) return false;

        return true;
    }
    private bool InstantGrabValid(List<InventorySlotData> inventorySlots, List<SmeltingSlotData> slots, ItemSlotData itemslot, int syncIndex)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, syncIndex)) return false;
        if (!Registry.TryGetItem(itemslot.ID, out _)) return false;
        return true;
    }
    private bool FormsValidRecipe(Item itemA, Item itemB)
    {
        return Registry.Instance.SmeltingRecipeList.Any(recipe =>
            (recipe.Resource1 == itemA.MaterialType && recipe.Resource2 == itemB.MaterialType) ||
            (recipe.Resource1 == itemB.MaterialType && recipe.Resource2 == itemA.MaterialType));
    }
    #endregion
    #region Invalidation
    private LocalResponse InvalidateInstantFill(ref List<SlotPatch> patches, List<SmeltingSlotData> slots, List<InventorySlotData> inventorySlots, int inventorySlotIndex, bool isClient)
    {
        if (isClient) return new LocalResponse { Accepted = false };
        patches.Add(new() { Index = inventorySlotIndex, Data = inventorySlots[inventorySlotIndex].Data, Type = SlotType.Inventory });
        patches.AddRange(PlayerHelperFunctions.SnapshotSmelter(slots));
        return new LocalResponse { Accepted = false, Patches = patches };
    }
    private LocalResponse InvalidateInstantGrab(ref List<SlotPatch> patches, List<SmeltingSlotData> slots, List<InventorySlotData> inventorySlots, int syncIndex, bool isClient)
    {
        if (isClient) return new LocalResponse { Accepted = false };
        patches.Add(new() { Index = syncIndex, Data = slots[syncIndex].Data, Type = SlotType.Smelting });
        patches.AddRange(PlayerHelperFunctions.SnapshotInventory(inventorySlots, false));
        return new LocalResponse { Accepted = false, Patches = patches };
    }
    #endregion

    private void CheckForgeReady(List<SmeltingSlotData> slots, SmeltingForgeData forge, int forgeIndex)
    {
        ItemSlotData input1 = GetSlot(slots, forgeIndex, 0).Data;
        ItemSlotData input2 = GetSlot(slots, forgeIndex, 1).Data;
        ItemSlotData outcome = GetSlot(slots, forgeIndex, 2).Data;

        if (!Registry.TryGetItem(input1.ID, out var item1) || !Registry.TryGetItem(input2.ID, out var item2))
        {
            InvalidateForge(forge);
            return;
        }

        var previousRecipe = forge.CurrentRecipe;
        var matchingRecipe = Registry.Instance.SmeltingRecipeList.FirstOrDefault(recipe =>
            (recipe.Resource1 == item1.MaterialType && recipe.Resource2 == item2.MaterialType) ||
            (recipe.Resource1 == item2.MaterialType && recipe.Resource2 == item1.MaterialType));

        if (matchingRecipe == null)
        {
            InvalidateForge(forge);
            return;
        }

        if (outcome.HasItem() && outcome.ID != matchingRecipe.SmeltingOutcome.ID)
        {
            InvalidateForge(forge);
            return;
        }

        OnSmeltingValidated?.Invoke(true);

        if (matchingRecipe == previousRecipe) return;

        forge.CurrentRecipe = matchingRecipe;
        forge.IsSmelting = true;
    }
    private void InvalidateForge(SmeltingForgeData forge)
    {
        forge.SmeltingTimer = 0f;
        forge.IsSmelting = false;
        OnSmeltingValidated?.Invoke(false);
        forge.CurrentRecipe = null;
    }
    [Server]
    private void SmeltComplete(int forgeIndex)
    {
        var forge = ServerForges[forgeIndex];
        ItemSlotData slot1Data = GetSlot(ServerSlots, forgeIndex, 0).Data;
        ItemSlotData slot2Data = GetSlot(ServerSlots, forgeIndex, 1).Data;
        ItemSlotData outcomeData = GetSlot(ServerSlots, forgeIndex, 2).Data;

        slot1Data.Quantity--;
        slot2Data.Quantity--;
        if (slot1Data.Quantity <= 0) slot1Data.Clear();
        if (slot2Data.Quantity <= 0) slot2Data.Clear();

        outcomeData.ID = forge.CurrentRecipe.SmeltingOutcome.ID;
        outcomeData.Quantity += forge.CurrentRecipe.OutcomeQuantity;

        forge.SmeltingTimer = 0f;

        CheckForgeReady(ServerSlots, forge, forgeIndex);
        List<SlotPatch> slotPatches = new()
        {
            new SlotPatch { Index = forgeIndex * 3, Data = slot1Data, Type = SlotType.Smelting },
            new SlotPatch { Index = forgeIndex * 3 + 1, Data = slot2Data, Type = SlotType.Smelting },
            new SlotPatch { Index = forgeIndex * 3 + 2, Data = outcomeData, Type = SlotType.Smelting }
        };
        LocalSyncSlots(slotPatches, true);
        Observer_SmeltComplete(slotPatches.ToArray(), forgeIndex);
    }
    private int GetSyncIndex(int forgeIndex, int slotIndex) => forgeIndex * 3 + slotIndex;
    private SmeltingSlotData GetSlot(List<SmeltingSlotData> slots, int forgeIndex, int slot) => slots[forgeIndex * 3 + slot];
    private List<SlotPatch> SnapshotSlots(List<SlotPatch> slotPatches, PlayerModule player = default)
    {
        var beforePatches = new List<SlotPatch>();
        foreach (var slotPatch in slotPatches)
        {
            switch (slotPatch.Type)
            {
                case SlotType.Inventory:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = player.Inventory.ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Smelting:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Smelting, Data = ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Ghost:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = player.DragGhost.ServerGhost});
                    break;
                default:
                    break;
            }
        }
        return beforePatches;
    }
    public void InvokeChange(List<SlotPatch> patches)
    {
        OnSmeltingSlotsChanged?.Invoke(patches);
    }
}
