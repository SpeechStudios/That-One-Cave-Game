using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class CraftingSlotData: ISlotContainer
{
    public ItemSlotData Data { get; set; }
    public CraftingComponent Component;
}

public class PlayerCraftingModule : NetworkBehaviour
{
    public PlayerInventoryModule Inventory;
    public PlayerDragGhostModule DragGhost;
    internal List<CraftingSlotData> ClientGrid { get; private set; } = new();
    private List<CraftingSlotData> ServerGrid = new();
    internal CraftingRecipe ClientRecipe;

    public event Action<bool> OnRecipeReady;
    public Dictionary<int, MaterialType> Materials = new();
    public event Action<List<SlotPatch>> OnCraftingSlotsChanged;
    private PlayerUIManager PlayerUI;
    public void ServerInit()
    {
        InitGrid(ServerGrid);
    }
    public void ClientInit()
    {
        PlayerUI = PlayerUIManager.Instance;
        InitGrid(ClientGrid);
        PlayerUI.UI_Crafting.Bind(this);
    }
    private void InitGrid(List<CraftingSlotData> grid)
    {
        grid.Clear();
        for (int i = 0; i < 9; i++)
            grid.Add(new CraftingSlotData());
    }

    #region Client Functions

    [Client]
    public void SlotToGhost(int fromSlot, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ClientGrid, DragGhost.ClientGhost, fromSlot, quantity);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_SlotToGhost_RPC(fromSlot, quantity);
    }
    [Client]
    public void GhostToSlot(int toSlot)
    {
        LocalResponse response = LocalGhostToSlot(ClientGrid, DragGhost.ClientGhost, toSlot);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_GhostToSlot_RPC(toSlot);
    }
    [Client]
    public bool CraftItem()
    {
        LocalResponse response = LocalCraftItem(ClientGrid, DragGhost.ClientGhost);
        if(!response.Accepted) return false;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_CraftItem_RPC();
        return true;
    }
    [Client]
    public void InstantFill(int inventorySlotIndex)
    {
        LocalResponse response = LocalInstantFill(Inventory.ClientSlots, ClientGrid, inventorySlotIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantFill_RPC(inventorySlotIndex);
    }
    [Client]
    public void InstantGrab(int fromSlot)
    {
        LocalResponse response = LocalInstantGrab(Inventory.ClientSlots, ClientGrid, fromSlot);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantGrab_RPC(fromSlot);
    }
    [Client]
    public bool InstantCraft()
    {
        LocalResponse response = LocalInstantCraft(Inventory.ClientSlots, ClientGrid);
        if (!response.Accepted) return false;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantCraft_RPC();
        return true;
    }
    #endregion

    #region Server RPCs

    [ServerRpc]
    private void Server_SlotToGhost_RPC(int fromSlot, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ServerGrid, DragGhost.ServerGhost, fromSlot, quantity);

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
    private void Server_GhostToSlot_RPC(int toSlot)
    {
        LocalResponse response = LocalGhostToSlot(ServerGrid,  DragGhost.ServerGhost, toSlot);

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
    private void Server_CraftItem_RPC()
    {
        LocalResponse response = LocalCraftItem(ServerGrid, DragGhost.ServerGhost);
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

        LocalResponse response = LocalInstantFill(Inventory.ServerSlots, ServerGrid, inventorySlotIndex);
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
    private void Server_InstantGrab_RPC(int fromSlot)
    {
        LocalResponse response = LocalInstantGrab(Inventory.ServerSlots, ServerGrid, fromSlot);
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
    private void Server_InstantCraft_RPC()
    {
        LocalResponse response = LocalInstantCraft(Inventory.ServerSlots, ServerGrid);
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
    private LocalResponse LocalSlotToGhost(List<CraftingSlotData> slots, ItemSlotData ghost, int from, int quantity)
    {
        ItemSlotData slotData = slots[from].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientGrid;

        if (!SlotToGhostValid(slots, ghost, from, quantity))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = from, Data = slotData, Type = SlotType.Crafting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        ghost.ID = slotData.ID;
        ghost.Materials = slotData.Materials;
        ghost.Quantity += quantity;

        slotData.Quantity -= quantity;
        if (slotData.Quantity <= 0)
            slotData.Clear();

        patches.Add(new() { Index = from, Data = slotData, Type = SlotType.Crafting });
        patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalGhostToSlot(List<CraftingSlotData> slots, ItemSlotData ghost, int to)
    {
        ItemSlotData slotData = slots[to].Data;
        Item ghostItem = Registry.TryGetItem(ghost.ID, out var tryGhostItem) ? tryGhostItem : null;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientGrid;

        if (!GhostToSlotValid(slots, ghost, ghostItem, to))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = to, Data = slotData, Type = SlotType.Crafting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        if (PlayerHelperFunctions.StackingValid(ghost, slots[to].Data, ghostItem.MaxStackSize))
        {
            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(ghost, slots[to].Data, ghostItem.MaxStackSize);
            ghost.Quantity = remainder;
            slotData.Quantity = stack;
            if (ghost.Quantity <= 0)
                ghost.Clear();

            patches.Add(new() { Index = to, Data = slotData, Type = SlotType.Crafting });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = true, Patches = patches };
        }

        patches.Add(new() { Index = to, Data = ghost, Type = SlotType.Crafting });
        patches.Add(new() { Data = slotData, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalCraftItem(List<CraftingSlotData> slots, ItemSlotData ghost)
    {
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientGrid;

        if (!CraftItemValid(slots, ghost, out CraftingRecipe recipe, out var materialArray))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.AddRange(PlayerHelperFunctions.SnapshotCrafting(slots));
            patches.Add(new SlotPatch { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        if (!ghost.HasItem())
        {
            ghost.ID = recipe.CraftedOutcome.ID;
            ghost.Quantity = recipe.CraftedOutcomeQuantity;
            ghost.Materials = materialArray;
        }
        else
        {
            ghost.Quantity += recipe.CraftedOutcomeQuantity;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            ItemSlotData newData = slots[i].Data;
            if (newData.HasItem())
            {
                newData.Quantity -= 1;
                if (newData.Quantity <= 0)
                    newData.Clear();
            }
            patches.Add(new SlotPatch { Index = i, Data = newData, Type = SlotType.Crafting });
        }

        patches.Add(new SlotPatch { Data = ghost, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalInstantFill(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, int inventorySlotIndex)
    {
        ItemSlotData inventorySlotData = inventorySlots[inventorySlotIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientGrid;

        if (!InstantFillValid(inventorySlots, inventorySlotIndex, out var item))
        {
            return InvalidateInstantFill(ref patches, slots, inventorySlots, inventorySlotIndex, isClient);
        }

        //Try Stack
        for (int i = 0; i < slots.Count; i++)
        {
            ItemSlotData slotData = slots[i].Data;
            if (!PlayerHelperFunctions.StackingValid(inventorySlotData, slotData, item.MaxStackSize)) continue;

            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(inventorySlotData, slotData, item.MaxStackSize);
            slotData.Quantity = stack;
            inventorySlotData.Quantity = remainder;

            patches.Add(new SlotPatch { Index = i, Data = slotData, Type = SlotType.Crafting });
            if (inventorySlotData.Quantity <= 0)
            {
                inventorySlotData.Clear();
                patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }
        //Try Recipe Fill
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Data.HasItem()) continue;
            if (item.ResourceType != slots[i].Component.ResourceType) continue;

            ItemSlotData slotData = slots[i].Data;
            return new LocalResponse
            {
                Accepted = true,
                Patches = new List<SlotPatch>
                {
                    new() { Index = inventorySlotIndex, Data = slotData, Type = SlotType.Inventory },
                    new() { Index = i, Data = inventorySlotData, Type = SlotType.Crafting }
                }
            };
        }

        if (patches.Count > 0)
        {
            patches.Add(new SlotPatch { Index = inventorySlotIndex, Data = inventorySlotData, Type = SlotType.Inventory });
            return new LocalResponse { Accepted = true, Patches = patches };
        }
        return InvalidateInstantFill(ref patches, slots, inventorySlots, inventorySlotIndex, isClient);
    }
    private LocalResponse LocalInstantGrab(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, int slotIndex)
    {
        ItemSlotData grabbedSlot = slots[slotIndex].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientGrid;

        if (!InstantGrabValid(inventorySlots, slots, grabbedSlot, slotIndex))
        {
            return InvalidateInstantGrab(ref patches, slots, inventorySlots, slotIndex, isClient);
        }
        Item item = Registry.GetItem(grabbedSlot.ID);

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
            if (grabbedSlot.Quantity <= 0)
            {
                grabbedSlot.Clear();
                patches.Add(new SlotPatch { Index = slotIndex, Data = grabbedSlot, Type = SlotType.Crafting });
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }

        //Find Empty Slot
        int emptySlot = inventorySlots.FindIndex(s => s.Type == ItemSlotType.Inventory && !s.Data.HasItem());
        if (emptySlot < 0 && patches.Count == 0)
        {
            return InvalidateInstantGrab(ref patches, slots, inventorySlots, slotIndex, isClient);
        }

        if (emptySlot >= 0)
        {
            ItemSlotData slotData = grabbedSlot;
            grabbedSlot.Clear();
            patches.Add(new SlotPatch { Index = slotIndex, Data = grabbedSlot, Type = SlotType.Crafting });
            patches.Add(new SlotPatch { Index = emptySlot, Data = slotData, Type = SlotType.Inventory });
            return new LocalResponse { Accepted = true, Patches = patches };
        }

        if (patches.Count > 0)
        {
            patches.Add(new SlotPatch { Index = slotIndex, Data = grabbedSlot, Type = SlotType.Crafting });
            return new LocalResponse { Accepted = true, Patches = patches };
        }
        return InvalidateInstantGrab(ref patches, slots, inventorySlots, slotIndex, isClient);
    }
    private LocalResponse LocalInstantCraft(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots)
    {
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientGrid;

        if (!InstantCraftValid(slots, out CraftingRecipe recipe))
        {
            return InvalidateInstantCraft(ref patches, slots, inventorySlots, isClient);
        }

        var materialArray = slots.Where(slot => slot.Data.HasItem() && Registry.TryGetItem(slot.Data.ID, out _))
            .Select(slot => (int)Registry.GetItem(slot.Data.ID).MaterialType).ToArray();

        ItemSlotData craftedOutcome = new()
        {
            ID = recipe.CraftedOutcome.ID,
            Quantity = recipe.CraftedOutcomeQuantity,
            Materials = materialArray
        };

        Item craftedItem = Registry.GetItem(recipe.CraftedOutcome.ID);

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (craftedOutcome.Quantity <= 0) break;
            if (inventorySlots[i].Type != ItemSlotType.Inventory) continue;

            ItemSlotData invData = inventorySlots[i].Data;
            if (!PlayerHelperFunctions.StackingValid(craftedOutcome, invData, craftedItem.MaxStackSize)) continue;

            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(craftedOutcome, invData, craftedItem.MaxStackSize);
            invData.Quantity = stack;
            craftedOutcome.Quantity = remainder;

            patches.Add(new SlotPatch { Index = i, Data = invData, Type = SlotType.Inventory });
        }

        if (craftedOutcome.Quantity > 0)
        {
            int emptySlot = inventorySlots.FindIndex(s => s.Type == ItemSlotType.Inventory && !s.Data.HasItem());
            if (emptySlot < 0 && patches.Count == 0)
            {
                return InvalidateInstantCraft(ref patches, slots, inventorySlots, isClient);
            }

            if (emptySlot >= 0)
            {
                ItemSlotData emptyData = craftedOutcome;
                patches.Add(new SlotPatch { Index = emptySlot, Data = emptyData, Type = SlotType.Inventory });
                craftedOutcome.Quantity = 0;
            }
        }

        for (int j = 0; j < slots.Count; j++)
        {
            ItemSlotData newData = slots[j].Data;
            if (newData.HasItem())
            {
                newData.Quantity -= 1;
                if (newData.Quantity <= 0)
                    newData.Clear();
            }
            patches.Add(new SlotPatch { Index = j, Data = newData, Type = SlotType.Crafting });
        }

        return new LocalResponse { Accepted = true, Patches = patches };
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
            if (patch.Type == SlotType.Crafting)
            {
                List<CraftingSlotData> slots = isServer ? ServerGrid : ClientGrid;
                if (PlayerHelperFunctions.SlotValid(slots, patch.Index))
                    slots[patch.Index].Data = patch.Data;

                if(!isServer) CheckRecipeReady(ClientGrid);
            }
            if (patch.Type == SlotType.Inventory)
            {
                List<InventorySlotData> slots = isServer ? Inventory.ServerSlots : Inventory.ClientSlots;
                if (PlayerHelperFunctions.SlotValid(slots, patch.Index))
                    slots[patch.Index].Data = patch.Data;
            }
        }
    }
    private void CheckRecipeReady(List<CraftingSlotData> slots)
    {
        CraftingRecipe match = Match(slots);
        ClientRecipe = match;
        OnRecipeReady?.Invoke(match != null);
    }
    #endregion

    #region Client RPCs
    [TargetRpc]
    private void Target_SyncSlots(NetworkConnection conn, SlotPatch[] patches)
    {
        LocalSyncSlots(patches.ToList(), false);
        InvokeChange(new List<SlotPatch>(patches));
    }
    #endregion

    #region Validation
    private bool SlotToGhostValid(List<CraftingSlotData> slots, ItemSlotData ghost, int from, int quantity)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, from)) return false;
        if (quantity <= 0 || quantity > slots[from].Data.Quantity) return false;
        if (!PlayerHelperFunctions.TransferValid(slots[from].Data, ghost)) return false;

        return true;
    }
    private bool InstantFillValid(List<InventorySlotData> slots, int index, out Item inventoryItem)
    {
        if (!Registry.TryGetItem(slots[index].Data.ID, out inventoryItem)) return false;
        if (!PlayerHelperFunctions.SlotValid(slots, index)) return false;
        if (inventoryItem.ResourceType == ResourceType.None) return false;

        return true;
    }
    private bool GhostToSlotValid(List<CraftingSlotData> slots, ItemSlotData ghost, Item ghostItem, int to)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, to)) return false;
        if (ghostItem == null) return false;
        if (ghostItem.ItemType !=  ItemType.Material) return false;
        if (slots[to].Component.ResourceType != ResourceType.None && ghostItem.ResourceType != slots[to].Component.ResourceType) return false;
        if (!PlayerHelperFunctions.TransferValid(ghost, slots[to].Data)) return false;

        return true;
    }
    public CraftingRecipe Match(List<CraftingSlotData> grid)
    {
        foreach (CraftingRecipe recipe in Registry.Instance.CraftingRecipeList)
        {
            if (Matches(grid, recipe)) return recipe;
        }
        return null;
    }

    private bool Matches(List<CraftingSlotData> grid, CraftingRecipe recipe)
    {
        for (int i = 0; i < 9; i++)
        {
            CraftingComponent required = recipe.Pattern[i];
            ItemSlotData cell = grid[i].Data;

            if (required.ResourceType == ResourceType.None)
            {
                if (cell.HasItem()) return false;
                continue;
            }

            if (!cell.HasItem()) return false;
            if (!Registry.TryGetItem(cell.ID, out Item item)) return false;
            if (item.ResourceType != required.ResourceType) return false;
        }

        return MaterialGroupsValid(grid, recipe);
    }

    private bool MaterialGroupsValid(List<CraftingSlotData> grid, CraftingRecipe recipe)
    {
        Materials.Clear();
        for (int i = 0; i < 9; i++)
        {
            CraftingComponent required = recipe.Pattern[i];
            if (required.MaterialGroup < 0) continue;

            Item item = Registry.GetItem(grid[i].Data.ID);
            if (Materials.TryGetValue(required.MaterialGroup, out MaterialType existing))
            {
                if (existing != item.MaterialType) return false;
            }
            else
            {
                Materials[required.MaterialGroup] = item.MaterialType;
            }
        }
        return true;
    }
    private bool CraftItemValid(List<CraftingSlotData> slots, ItemSlotData ghost, out CraftingRecipe recipe, out int[] materialArray)
    {
        materialArray = default;
        recipe = Match(slots);
        if (recipe == null) return false;
        if (!Registry.TryGetItem(recipe.CraftedOutcome.ID, out Item craftedItem)) return false;

        materialArray = slots.Where(s => s.Data.HasItem())
            .Select(s => (int)Registry.GetItem(s.Data.ID).MaterialType).ToArray();

        if (ghost.HasItem())
        {
            if (ghost.ID != recipe.CraftedOutcome.ID) return false;
            if (craftedItem.MaxStackSize < ghost.Quantity + recipe.CraftedOutcomeQuantity) return false;
        }

        return true;
    }
    private bool InstantCraftValid(List<CraftingSlotData> slots, out CraftingRecipe recipe)
    {
        recipe = Match(slots);
        if (recipe == null) return false;
        if (!Registry.TryGetItem(recipe.CraftedOutcome.ID, out Item craftedItem)) return false;

        return true;
    }
    private bool InstantGrabValid(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, ItemSlotData itemslot, int slotIndex)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, slotIndex)) return false;
        if (!Registry.TryGetItem(itemslot.ID, out _)) return false;
        if (!inventorySlots.Any(s => s.Type == ItemSlotType.Inventory && s.Data.ID == 0)) return false;
        return true;
    }

    #endregion

    #region Invalidation
    private LocalResponse InvalidateInstantFill(ref List<SlotPatch> patches, List<CraftingSlotData> slots, List<InventorySlotData> inventorySlots, int inventorySlotIndex, bool isClient)
    {
        if (isClient) return new LocalResponse { Accepted = false };
        patches.Add(new() { Index = inventorySlotIndex, Data = inventorySlots[inventorySlotIndex].Data, Type = SlotType.Inventory });
        patches.AddRange(PlayerHelperFunctions.SnapshotCrafting(slots));
        return new LocalResponse { Accepted = false, Patches = patches };
    }
    private LocalResponse InvalidateInstantGrab(ref List<SlotPatch> patches, List<CraftingSlotData> slots, List<InventorySlotData> inventorySlots, int slotIndex, bool isClient)
    {
        if (isClient) return new LocalResponse { Accepted = false };
        patches.Add(new() { Index = slotIndex, Data = slots[slotIndex].Data, Type = SlotType.Crafting });
        patches.AddRange(PlayerHelperFunctions.SnapshotInventory(inventorySlots, false));
        return new LocalResponse { Accepted = false, Patches = patches };
    }
    private LocalResponse InvalidateInstantCraft(ref List<SlotPatch> patches, List<CraftingSlotData> slots, List<InventorySlotData> inventorySlots, bool isClient)
    {
        if (isClient) return new LocalResponse { Accepted = false };
        patches.AddRange(PlayerHelperFunctions.SnapshotCrafting(slots));
        patches.AddRange(PlayerHelperFunctions.SnapshotInventory(inventorySlots, false));
        return new LocalResponse { Accepted = false, Patches = patches };
    }
    #endregion

    private List<SlotPatch> SnapshotSlots(List<SlotPatch> slotPatches)
    {
        var beforePatches = new List<SlotPatch>();
        foreach (var slotPatch in slotPatches)
        {
            switch (slotPatch.Type)
            {
                case SlotType.Crafting:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = ServerGrid[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Inventory:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = PlayerUI.UI_Inventory.TargetInventory.ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Ghost:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = PlayerUI.UI_DragGhost.TargetGhost.ServerGhost });
                    break;
                default:
                    break;
            }
        }
        return beforePatches;
    }
    private void InvokeChange(List<SlotPatch> patches)
    {
        OnCraftingSlotsChanged?.Invoke(patches);
    }
}