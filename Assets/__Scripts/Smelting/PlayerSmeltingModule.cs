using FishNet.Object;
using FishNet.Connection;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SmeltingSlotData : ISlotContainer
{
    public ItemSlotData Data { get; set; }
    public int SlotIndex;
}
public class SmeltingForgeData
{
    public SmeltingRecipe CurrentRecipe;
    public float SmeltingTimer;
    public bool IsSmelting;
}

public class PlayerSmeltingModule : NetworkBehaviour
{
    public PlayerInventoryModule Inventory;
    public PlayerDragGhostModule DragGhost;

    public SmeltingForgeData ClientForge = new();
    private SmeltingForgeData ServerForge = new();

    public List<SmeltingSlotData> ClientSlots = new();
    private List<SmeltingSlotData> ServerSlots = new();

    public event Action<List<SlotPatch>> OnSmeltingSlotsChanged;
    public event Action<bool> OnSmeltingValidated;
    public event Action OnSmeltingComplete;

    public void Init()
    {
        PlayerUIManager.Instance.UI_Smelting.Bind(this);
    }
    public void Start()
    {
        ClientSlots.Add(new SmeltingSlotData { SlotIndex = 0, Data = new ItemSlotData() });
        ClientSlots.Add(new SmeltingSlotData { SlotIndex = 1, Data = new ItemSlotData() });
        ClientSlots.Add(new SmeltingSlotData { SlotIndex = 2, Data = new ItemSlotData() });

        ServerSlots.Add(new SmeltingSlotData { SlotIndex = 0, Data = new ItemSlotData() });
        ServerSlots.Add(new SmeltingSlotData { SlotIndex = 1, Data = new ItemSlotData() });
        ServerSlots.Add(new SmeltingSlotData { SlotIndex = 2, Data = new ItemSlotData() });
    }
    public void Update()
    {
        float dt = Time.deltaTime;
        if (ClientForge.IsSmelting)
        {
            if (ClientForge.SmeltingTimer < ClientForge.CurrentRecipe.SmeltingTime)
                ClientForge.SmeltingTimer += dt;
        }
        if (ServerForge.IsSmelting)
        {
            ServerForge.SmeltingTimer += dt;
            if (ServerForge.SmeltingTimer > ServerForge.CurrentRecipe.SmeltingTime)
                SmeltComplete();
        }
    }

    #region Client Commands

    [Client]
    public void SlotToGhost(int slotIndex, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ClientSlots, DragGhost.ClientGhost, slotIndex, quantity);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_SlotToGhost_RPC(slotIndex, quantity);
    }
    [Client]
    public void GhostToSlot(int slotIndex)
    {
        LocalResponse response = LocalGhostToSlot(ClientSlots, DragGhost.ClientGhost, slotIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_GhostToSlot_RPC(slotIndex);
    }
    [Client]
    public void InstantGrab(int slotIndex)
    {
        LocalResponse response = LocalInstantGrab(Inventory.ClientSlots, ClientSlots, slotIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantGrab_RPC(slotIndex);
    }
    [Client]
    public void InstantFill(int inventorySlotIndex)
    {
        LocalResponse response = LocalInstantFill(ClientForge, ClientSlots, Inventory.ClientSlots, inventorySlotIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantFill_RPC(inventorySlotIndex);
    }
    #endregion

    #region Server RPC's
    [ServerRpc]
    private void Server_SlotToGhost_RPC(int syncIndex, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ServerSlots, DragGhost.ServerGhost, syncIndex, quantity);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true);
        }

    }
    [ServerRpc]
    private void Server_GhostToSlot_RPC(int syncIndex)
    {
        LocalResponse response = LocalGhostToSlot(ServerSlots, DragGhost.ServerGhost, syncIndex);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true);
        }
    }
    [ServerRpc]
    private void Server_InstantGrab_RPC(int syncIndex)
    {
        LocalResponse response = LocalInstantGrab(Inventory.ServerSlots, ServerSlots, syncIndex);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true);
        }
    }
    [ServerRpc]
    private void Server_InstantFill_RPC(int inventorySlotIndex)
    {
        LocalResponse response = LocalInstantFill(ServerForge, ServerSlots, Inventory.ServerSlots, inventorySlotIndex);

        if (!response.Accepted)
        {
            List<SlotPatch> before = SnapshotSlots(response.Patches);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            LocalSyncSlots(response.Patches, true);
        }
    }
    #endregion

    #region Local Functions
    private LocalResponse LocalSlotToGhost(List<SmeltingSlotData> slots, ItemSlotData ghost, int slotIndex, int quantity)
    {
        ItemSlotData slotData = slots[slotIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!SlotToGhostValid(slots, slotData, ghost, slotIndex, quantity))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = slotIndex, Data = slotData, Type = SlotType.Smelting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        ghost.ID = slotData.ID;
        ghost.Materials = slotData.Materials;
        ghost.Quantity += quantity;

        slotData.Quantity -= quantity;
        if (slotData.Quantity <= 0)
            slotData.Clear();

        patches.Add(new() { Index = slotIndex, Data = slotData, Type = SlotType.Smelting });
        patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalGhostToSlot(List<SmeltingSlotData> slots, ItemSlotData ghost, int slotIndex)
    {
        Item ghostItem = Registry.TryGetItem(ghost.ID, out var tryGhostItem) ? tryGhostItem : null;
        ItemSlotData slotData = slots[slotIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!GhostToSlotValid(slots, slotData, ghost, ghostItem, slotIndex))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = slotIndex, Data = slotData, Type = SlotType.Smelting });
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

            patches.Add(new() { Index = slotIndex, Data = slotData, Type = SlotType.Smelting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = true, Patches = patches };
        }

        patches.Add(new() { Index = slotIndex, Data = ghost, Type = SlotType.Smelting });
        patches.Add(new() { Data = slotData, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalInstantFill(SmeltingForgeData forge, List<SmeltingSlotData> slots, List<InventorySlotData> inventorySlots, int inventorySlotIndex)
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
        bool slot0Empty = !slots[0].Data.HasItem();
        bool slot1Empty = !slots[1].Data.HasItem();

        if (!slot0Empty && slot1Empty)
        {
            Item existing = Registry.GetItem(slots[0].Data.ID);
            if (FormsValidRecipe(item, existing))
            {
                ItemSlotData emptySlot = inventorySlotData;
                inventorySlotData.Clear();
                patches.Add(new SlotPatch { Index = 1, Data = emptySlot, Type = SlotType.Smelting });
                patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }
        else if (slot0Empty && !slot1Empty)
        {
            Item existing = Registry.GetItem(slots[1].Data.ID);
            if (FormsValidRecipe(item, existing))
            {
                ItemSlotData emptySlot = inventorySlotData;
                inventorySlotData.Clear();
                patches.Add(new SlotPatch { Index = 0, Data = emptySlot, Type = SlotType.Smelting });
                patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }
        // Try Get Empty
        if (slot0Empty && slot1Empty)
        {
            ItemSlotData emptySlot = inventorySlotData;
            inventorySlotData.Clear();
            patches.Add(new SlotPatch { Index = 0, Data = emptySlot, Type = SlotType.Smelting });
            patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
            return new LocalResponse { Accepted = true, Patches = patches };
        }
        if (patches.Count > 0)
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
    private void LocalSyncSlots(List<SlotPatch> patches, bool isServer)
    {
        foreach (SlotPatch patch in patches)
        {
            if (patch.Type == SlotType.Ghost)
            {
                if (isServer)
                    DragGhost.ServerGhost = patch.Data;
                else
                    DragGhost.ClientGhost = patch.Data;
            }
            if (patch.Type == SlotType.Smelting)
            {
                List<SmeltingSlotData> slots = isServer ? ServerSlots : ClientSlots;
                SmeltingForgeData forge = isServer ? ServerForge : ClientForge;
                slots[patch.Index].Data = patch.Data;
                CheckForgeReady(slots, forge);
            }
            if(patch.Type == SlotType.Inventory)
            {
                List<InventorySlotData> slots = isServer ? Inventory.ServerSlots : Inventory.ClientSlots;
                slots[patch.Index].Data = patch.Data;
            }
        }
    }
    [TargetRpc]
    private void Target_SyncSlots(NetworkConnection conn, SlotPatch[] patches)
    {
        Debug.Log("Resyncing Slots");
        LocalSyncSlots(patches.ToList(), false);
        InvokeChange(new List<SlotPatch>(patches));
    }
    [TargetRpc]
    private void Target_SmeltComplete(NetworkConnection conn, SlotPatch[] patches)
    {
        LocalSyncSlots(patches.ToList(), false);
        ClientForge.SmeltingTimer = 0;
        OnSmeltingComplete?.Invoke();
        CheckForgeReady(ClientSlots, ClientForge);
        InvokeChange(new List<SlotPatch>(patches));
    }
    #endregion


    #region Validation
    private bool SlotToGhostValid(List<SmeltingSlotData> slots, ItemSlotData slotData, ItemSlotData ghost, int slotIndex, int quantity)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, slotIndex)) return false;
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

    private void CheckForgeReady(List<SmeltingSlotData> slots, SmeltingForgeData forge)
    {
        ItemSlotData input1 = slots[0].Data;
        ItemSlotData input2 = slots[1].Data;
        ItemSlotData outcome = slots[2].Data;

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
    private void SmeltComplete()
    {
        ItemSlotData slot1Data = ServerSlots[0].Data;
        ItemSlotData slot2Data = ServerSlots[1].Data;
        ItemSlotData outcomeData = ServerSlots[2].Data;

        slot1Data.Quantity--;
        slot2Data.Quantity--;
        if (slot1Data.Quantity <= 0) slot1Data.Clear();
        if (slot2Data.Quantity <= 0) slot2Data.Clear();

        outcomeData.ID = ServerForge.CurrentRecipe.SmeltingOutcome.ID;
        outcomeData.Quantity += ServerForge.CurrentRecipe.OutcomeQuantity;

        ServerForge.SmeltingTimer = 0f;

        CheckForgeReady(ServerSlots, ServerForge);
        List<SlotPatch> slotPatches = new()
        {
            new SlotPatch { Index = 0, Data = slot1Data, Type = SlotType.Smelting },
            new SlotPatch { Index = 1, Data = slot2Data, Type = SlotType.Smelting },
            new SlotPatch { Index = 2, Data = outcomeData, Type = SlotType.Smelting }
        };
        LocalSyncSlots(slotPatches, true);
        Target_SmeltComplete(Owner, slotPatches.ToArray());

        Debug.Log("Smelting Compelte");
    }
    private List<SlotPatch> SnapshotSlots(List<SlotPatch> slotPatches)
    {
        var beforePatches = new List<SlotPatch>();
        foreach (var slotPatch in slotPatches)
        {
            switch (slotPatch.Type)
            {
                case SlotType.Inventory:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = Inventory.ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Smelting:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Smelting, Data = ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Ghost:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = DragGhost.ServerGhost});
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
