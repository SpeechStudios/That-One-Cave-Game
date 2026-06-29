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
    internal List<CraftingSlotData> ClientSlots { get; private set; } = new();
    private List<CraftingSlotData> ServerSlots = new();
    private LocalResponse ClientCraftResponse;

    internal CraftingRecipe ClientRecipe;
    private CraftingRecipe ServerRecipe;

    public event Action<bool> OnRecipeReady;

    public event Action<List<SlotPatch>> OnCraftingSlotsChanged;
    public void Init()
    {
        CraftingManager.Instance.Bind(this);
    }

    #region Client Functions
    [Client]
    public void CloseCrafting()
    {
        List<SlotPatch> patches = LocalClearCraftingTable(Inventory.ClientSlots, ClientSlots, ClientRecipe);
        InvokeChange(patches);
        Inventory.InvokeChange(patches);
        Server_CloseCrafting_RPC();
    }
    [Client]
    public void SelectRecipe(int recipeID)
    {
        bool newValidRecipe = LocalSelectRecipe(recipeID, ref ClientRecipe);
        Debug.Log(newValidRecipe);
        if (!newValidRecipe) return;

        List<SlotPatch> patches = LocalClearCraftingTable(Inventory.ClientSlots, ClientSlots, ClientRecipe);
        InvokeChange(patches);
        Inventory.InvokeChange(patches);
        Server_SelectRecipe_RPC(recipeID);
    }
    [Client]
    public void SlotToGhost(int fromSlot, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ClientSlots, DragGhost.ClientGhost, fromSlot, quantity);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_SlotToGhost_RPC(fromSlot, quantity);
    }
    [Client]
    public void GhostToSlot(int toSlot)
    {
        LocalResponse response = LocalGhostToSlot(ClientSlots, DragGhost.ClientGhost, toSlot);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_GhostToSlot_RPC(toSlot);
    }
    [Client]
    public bool CraftItem()
    {
        LocalResponse response = LocalCraftItem(ClientSlots, DragGhost.ClientGhost, ClientRecipe);
        if(!response.Accepted) return false;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_CraftItem_RPC();
        return true;
    }
    [Client]
    public void InstantFill(int inventorySlotIndex)
    {
        LocalResponse response = LocalInstantFill(Inventory.ClientSlots, ClientSlots, inventorySlotIndex);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantFill_RPC(inventorySlotIndex);
    }
    [Client]
    public void InstantGrab(int fromSlot)
    {
        LocalResponse response = LocalInstantGrab(Inventory.ClientSlots, ClientSlots, fromSlot);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Inventory.InvokeChange(response.Patches);
        Server_InstantGrab_RPC(fromSlot);
    }
    [Client]
    public bool InstantCraft()
    {
        LocalResponse response = LocalInstantCraft(Inventory.ClientSlots, ClientSlots, ClientRecipe);
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
    private void Server_CloseCrafting_RPC()
    {
        LocalClearCraftingTable(Inventory.ServerSlots, ServerSlots, ServerRecipe);
    }

    [ServerRpc]
    private void Server_SelectRecipe_RPC(int recipeID)
    {
        bool newValidRecipe = LocalSelectRecipe(recipeID, ref ServerRecipe);

        if (!newValidRecipe)
        {
            List<SlotPatch> before = PlayerHelperFunctions.SnapshotCrafting(ServerSlots);
            before.AddRange(PlayerHelperFunctions.SnapshotInventory(Inventory.ServerSlots, false));
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
           LocalClearCraftingTable(Inventory.ServerSlots, ServerSlots, ServerRecipe);
        }

    }
    [ServerRpc]
    private void Server_SlotToGhost_RPC(int fromSlot, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ServerSlots, DragGhost.ServerGhost, fromSlot, quantity);

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
        LocalResponse response = LocalGhostToSlot(ServerSlots,  DragGhost.ServerGhost, toSlot);

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
        LocalResponse response = LocalCraftItem(ServerSlots, DragGhost.ServerGhost, ServerRecipe);
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

        LocalResponse response = LocalInstantFill(Inventory.ServerSlots, ServerSlots, inventorySlotIndex);
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
        LocalResponse response = LocalInstantGrab(Inventory.ServerSlots, ServerSlots, fromSlot);
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
        LocalResponse response = LocalInstantCraft(Inventory.ServerSlots, ServerSlots, ServerRecipe);
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
    private bool LocalSelectRecipe(int recipeID, ref CraftingRecipe currentRecipe) 
    {
        var newRecipe = Registry.GetCraftingRecipe(recipeID);
        if (newRecipe == null) return false;
        if (currentRecipe!=null && newRecipe.ID == currentRecipe.ID) return false;

        currentRecipe = newRecipe;
        return true;
    }
    private List<SlotPatch> LocalClearCraftingTable(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, CraftingRecipe recipe)
    {
        bool isServer = slots == ServerSlots;
        List<SlotPatch> patches = new();

        Dictionary<int, ItemSlotData> workingInventory = new();
        for (int i = 0; i < inventorySlots.Count; i++)
            workingInventory[i] = inventorySlots[i].Data;

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            if (!slots[slotIndex].Data.HasItem()) continue;
            Item item = Registry.GetItem(slots[slotIndex].Data.ID);
            ItemSlotData returning = slots[slotIndex].Data;

            // Stack into existing inventory slots
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (returning.Quantity <= 0) break;
                if (inventorySlots[i].Type != ItemSlotType.Inventory) continue;
                ItemSlotData invData = workingInventory[i];
                if (!PlayerHelperFunctions.StackingValid(returning, invData, item.MaxStackSize)) continue;

                var (stack, remainder) = PlayerHelperFunctions.TryStackItems(returning, invData, item.MaxStackSize);
                invData.Quantity = stack;
                returning.Quantity = remainder;
                workingInventory[i] = invData;
            }

            if (returning.Quantity <= 0) continue;

            int emptySlot = -1;
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (inventorySlots[i].Type != ItemSlotType.Inventory) continue;
                if (workingInventory[i].HasItem()) continue;
                emptySlot = i;
                break;
            }
            if (emptySlot >= 0)
            {
                ItemSlotData emptyData = workingInventory[emptySlot];
                emptyData.ID = returning.ID;
                emptyData.Quantity = returning.Quantity;
                emptyData.Materials = returning.Materials;
                workingInventory[emptySlot] = emptyData;
            }
            else if (isServer)
            {
                Vector3 dropPos = transform.position + transform.forward * 1f;
                WorldItemGameObject worldObject = Instantiate(Registry.GetItem(returning.ID).WorldItemPrefab, dropPos, Quaternion.identity);
                worldObject.Initialize(returning.ID, returning.Quantity, returning.Materials, true);
                Spawn(worldObject);
            }
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (workingInventory[i].ID != inventorySlots[i].Data.ID ||
                workingInventory[i].Quantity != inventorySlots[i].Data.Quantity)
                patches.Add(new SlotPatch { Index = i, Data = workingInventory[i], Type = SlotType.Inventory });
        }
        for (int i = 0; i < slots.Count; i++)
        {
            ItemSlotData slotData = slots[i].Data;
            slotData.Clear();
            patches.Add(new SlotPatch { Index = i, Data = slotData, Type = SlotType.Crafting });
        }
        LocalSyncSlots(patches, isServer);
        slots.Clear();
        if (recipe != null)
        {
            foreach (CraftingComponent component in recipe.Components)
            {
                slots.Add(new CraftingSlotData
                {
                    Data = new ItemSlotData(),
                    Component = component,
                });
            }
        }
        return patches;
    }
    private LocalResponse LocalSlotToGhost(List<CraftingSlotData> slots, ItemSlotData ghost, int from, int quantity)
    {
        ItemSlotData slotData = slots[from].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

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
        bool isClient = slots == ClientSlots;

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
    private LocalResponse LocalCraftItem(List<CraftingSlotData> slots, ItemSlotData ghost, CraftingRecipe recipe)
    {
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!CraftItemValid(slots, ghost, recipe, out var materialArray))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.AddRange(PlayerHelperFunctions.SnapshotCrafting(slots));
            patches.Add(new SlotPatch { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        //Create item
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
        //Remove Crafting Resources
        for (int i = 0; i < slots.Count; i++)
        {
            ItemSlotData newData = slots[i].Data;
            newData.Quantity -= slots[i].Component.RequiredQuantity;
            if (newData.Quantity <= 0)
                newData.Clear();

            patches.Add(new SlotPatch { Index = i, Data = newData, Type = SlotType.Crafting });
        }

        patches.Add(new SlotPatch { Data = ghost, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalInstantFill(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, int inventorySlotIndex)
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
        bool isClient = slots == ClientSlots;

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
    private LocalResponse LocalInstantCraft(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, CraftingRecipe recipe)
    {
        var materialArray = slots.Where(slot => Registry.TryGetItem(slot.Data.ID, out _))
            .Select(slot => (int)Registry.GetItem(slot.Data.ID).MaterialType).ToArray();

        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!InstantCraftValid(slots, recipe))
        {
            return InvalidateInstantCraft(ref patches, slots, inventorySlots, isClient);
        }

        ItemSlotData craftedOutcome = new()
        {
            ID = recipe.CraftedOutcome.ID,
            Quantity = recipe.CraftedOutcomeQuantity,
            Materials = materialArray
        };

        Item craftedItem = Registry.GetItem(recipe.CraftedOutcome.ID);


        // Stack with existing matching inventory slots
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

        // Move remainder into first empty inventory slot
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

        // Remove Resources
        for (int j = 0; j < slots.Count; j++)
        {
            ItemSlotData newData = slots[j].Data;
            newData.Quantity -= slots[j].Component.RequiredQuantity;
            if (newData.Quantity <= 0)
                newData.Clear();

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
                List<CraftingSlotData> slots = isServer ? ServerSlots : ClientSlots;
                if (PlayerHelperFunctions.SlotValid(slots, patch.Index))
                    slots[patch.Index].Data = patch.Data;

                CheckRecipeReady(slots);
            }
            if(patch.Type == SlotType.Inventory)
            {
                List<InventorySlotData> slots = isServer ? Inventory.ServerSlots : Inventory.ClientSlots;
                if (PlayerHelperFunctions.SlotValid(slots, patch.Index))
                    slots[patch.Index].Data = patch.Data;
            }
        }
    }
    private void CheckRecipeReady(List<CraftingSlotData> slots)
    {
        if (slots.Count == 0)
        {
            OnRecipeReady?.Invoke(false);
            return;
        }

        foreach (CraftingSlotData slot in slots)
        {
            if (!Registry.TryGetItem(slot.Data.ID, out Item item)) { OnRecipeReady?.Invoke(false); return; }
            if (item.ResourceType != slot.Component.ResourceType) { OnRecipeReady?.Invoke(false); return; }
            if (slot.Data.Quantity < slot.Component.RequiredQuantity) { OnRecipeReady?.Invoke(false); return; }
        }

        OnRecipeReady?.Invoke(true);
    }
    #endregion

    #region Client RPCs
    [TargetRpc]
    private void Target_SyncSlots(NetworkConnection conn, SlotPatch[] patches)
    {
        LocalSyncSlots(patches.ToList(), false);
        CheckRecipeReady(ClientSlots);
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
        if (ghostItem.ResourceType != slots[to].Component.ResourceType) return false;
        if (!PlayerHelperFunctions.TransferValid(ghost, slots[to].Data)) return false;

        return true;
    }
    private bool CraftItemValid(List<CraftingSlotData> slots, ItemSlotData ghost, CraftingRecipe recipe, out int[] materialArray)
    {
        materialArray = default;
        if (recipe == null) return false;
        if (slots.Count != recipe.Components.Count) return false;
        if (!Registry.TryGetItem(recipe.CraftedOutcome.ID, out Item craftedItem)) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].Data.HasItem()) return false;
            if (!Registry.TryGetItem(slots[i].Data.ID, out Item item)) return false;
            if (item.ResourceType != recipe.Components[i].ResourceType) return false;
            if (slots[i].Data.Quantity < recipe.Components[i].RequiredQuantity) return false;
        }

        materialArray = slots.Select(slot => (int)Registry.GetItem(slot.Data.ID).MaterialType).ToArray();

        if (ghost.HasItem())
        {
            if (ghost.ID != recipe.CraftedOutcome.ID) return false;
            if (!Registry.TryGetItem(recipe.CraftedOutcome.ID, out Item item)) return false;
            if (item.MaxStackSize < ghost.Quantity + recipe.CraftedOutcomeQuantity) return false;
            if (ghost.Materials != null && !ghost.Materials.SequenceEqual(materialArray)) return false;
        }

        return true;
    }
    private bool InstantCraftValid(List<CraftingSlotData> slots, CraftingRecipe recipe)
    {
        if (recipe == null) return false;
        if (slots.Count != recipe.Components.Count) return false;
        if (!Registry.TryGetItem(recipe.CraftedOutcome.ID, out Item craftedItem)) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].Data.HasItem()) return false;
            if (!Registry.TryGetItem(slots[i].Data.ID, out Item item)) return false;
            if (item.ResourceType != recipe.Components[i].ResourceType) return false;
            if (slots[i].Data.Quantity < recipe.Components[i].RequiredQuantity) return false;
        }
        return true;
    }
    private bool InstantGrabValid(List<InventorySlotData> inventorySlots, List<CraftingSlotData> slots, ItemSlotData itemslot, int slotIndex)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, slotIndex)) return false;
        if (!Registry.TryGetItem(itemslot.ID, out _)) return false;
        if (!inventorySlots.Any(s => s.Type == ItemSlotType.Inventory && s.Data.ID < 0)) return false;
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
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Inventory:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = InventoryManager.Instance.TargetInventory.ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Ghost:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = DragGhostManager.Instance.TargetGhost.ServerGhost });
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