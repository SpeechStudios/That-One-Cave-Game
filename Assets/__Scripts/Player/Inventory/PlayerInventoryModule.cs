using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
public struct SlotPatch
{
    public int Index;
    public ItemSlotData Data;
    public SlotType Type;
}
public class InventorySlotData : ISlotContainer
{
    public ItemSlotData Data { get; set; }
    public ItemSlotType Type;
}
public class EquippedSlot
{
    public NetworkObject Item;
    public bool IsEquipped;
}
public class LocalResponse
{
    public bool Accepted;
    public List<SlotPatch> Patches;
}

public class PlayerInventoryModule : NetworkBehaviour
{
    public PlayerInteractModule InteractModule;
    public PlayerDragGhostModule DragGhost;
    public PlayerLoadoutModule Loadout;

    public InputActionReference ToggleInventoryButton;
    private bool IsInventoryOpen;

    [Space]



    [HideInInspector] public List<InventorySlotData> ClientSlots = new();
    [HideInInspector] public List<InventorySlotData> ServerSlots = new();
    private Dictionary<ItemSlotType, int> EquipSlotLookup = new();
    public event Action<List<SlotPatch>> OnInventoryChanged;
    private bool StartingItemsGiven;

    #region Initalization 
    public void ClientInit()
    {
        InventoryManager.Instance.Bind(this);
        InventoryManager.Instance.SpawnSlots(this, false);
    }
    public void ServerInit()
    {
        InventoryManager.Instance.SpawnSlots(this, true);
    }
    [ServerRpc]
    public void RequestStart()
    {
        RequestStartingItems();
    }
    [Server]
    private void RequestStartingItems()
    {
        if (StartingItemsGiven) return;
        StartingItemsGiven = true;

        List<SlotPatch> patches = new();
        foreach (Item item in Registry.GetStartingItems())
        {
            if (item == null) continue;
            ItemSlotData data = new()
            {
                ID = item.ID,
                Quantity = 1,
            };
            int worldItemId = ServerWorldItemStash.Instance.GetNextWorldItemID();
            ServerWorldItemStash.Instance.StashItem(data, transform.position, worldItemId);
            LocalResponse response = LocalPickUp(ServerSlots, data, out var _, worldItemId);
            LocalSyncSlots(response.Patches, true);
            patches.AddRange(response.Patches);
        }
        Target_SyncSlots(Owner, patches.ToArray());
    }
    public void SpawnSlots(ItemSlotData data, ItemSlotType type, bool bindForServer)
    {
        if (bindForServer)
        {
            ServerSlots.Add(new InventorySlotData { Data = data, Type = type });
            if (type != ItemSlotType.Inventory && !EquipSlotLookup.ContainsKey(type))
                EquipSlotLookup.Add(type, ServerSlots.Count - 1);
        }
        else
        {
            ClientSlots.Add(new InventorySlotData { Data = data, Type = type });
            if (type != ItemSlotType.Inventory && !EquipSlotLookup.ContainsKey(type))
                EquipSlotLookup.Add(type, ClientSlots.Count - 1);
        }
    }
    #endregion

    #region Inventory Toggle
    private void OnEnable()
    {
        ToggleInventoryButton.action.performed += OnToggleInventory;
        ToggleInventoryButton.action.Enable();
    }

    private void OnDisable()
    {
        ToggleInventoryButton.action.performed -= OnToggleInventory;
        ToggleInventoryButton.action.Disable();
    }

    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (IsInventoryOpen)
            Close();
        else
            Open();

        if (InteractModule.IsInteracting)
        {
            InteractModule.CloseInteraction();
            return;
        }
    }

    public void Open()
    {
        if (IsInventoryOpen) return;
        IsInventoryOpen = true;
        InventoryManager.Instance.InventoryCanvas.SetActive(true);
        DragGhostManager.Instance.ReturnToSender();
        UpdateCursorState();
    }
    public void Close()
    {
        if (!IsInventoryOpen) return;
        IsInventoryOpen = false;
        InventoryManager.Instance.InventoryCanvas.SetActive(false);
        DragGhostManager.Instance.ReturnToSender();
        UpdateCursorState();
    }
    private void UpdateCursorState()
    {
        Cursor.visible = IsInventoryOpen;
        Cursor.lockState = IsInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
    #endregion

    #region Client Functions
    [Client]
    public void PickUpItem(int worldItemID, ItemSlotData data)
    {
        LocalResponse response = LocalPickUp(ClientSlots, data, out var _);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_PickUpItem_RPC(worldItemID);
    }
    [Client]
    public void DropItem(int quantity)
    {
        int itemID = DragGhost.ClientGhost.ID;
        LocalResponse response = LocalDrop(DragGhost.ClientGhost, quantity, true);
        if (!response.Accepted) return;

        Vector3 dropPos = transform.position + transform.forward * 1f;
        WorldItemGameObject worldObject = Instantiate(Registry.GetItem(itemID).WorldItemPrefab, dropPos, Quaternion.identity);
        worldObject.Initialize(itemID, quantity, true);
        Spawn(worldObject);

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_DropItem_RPC(quantity);
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
    public bool GhostToSlot(int toSlot)
    {
        LocalResponse response = LocalGhostToSlot(ClientSlots, DragGhost.ClientGhost, toSlot);
        if (!response.Accepted) return false;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_GhostToSlot_RPC(toSlot);
        return true;
    }
    [Client]
    public void InstantEquip(int fromSlot)
    {
        LocalResponse response = LocalInstantEquip(ClientSlots, fromSlot);
        if (!response.Accepted) return;

        LocalSyncSlots(response.Patches, false);
        InvokeChange(response.Patches);
        Server_InstantEquip_RPC(fromSlot);
    }
    #endregion

    #region Server RPCs
    [ServerRpc]
    private void Server_PickUpItem_RPC(int worldItemID)
    {
        WorldItem worldItem = ServerWorldItemStash.Instance.GetWorldItem(worldItemID);
        if (worldItem == null) return;

        LocalResponse response = LocalPickUp(ServerSlots, worldItem.Data, out var amountNotPickedUp, worldItemID);
        if (!response.Accepted)
        {
            Debug.Log($"<color=red> Pick Up Rollback: Response Accepted: {response.Accepted}</color>");
            List<SlotPatch> before = SnapshotSlots(response.Patches);
            Target_SyncSlots(Owner, before.ToArray());
        }
        else
        {
            ServerWorldItemStash.Instance.RemoveItem(worldItemID);
            LocalSyncSlots(response.Patches, true);
            if (amountNotPickedUp > 0)
            {
                ItemSlotData newWorldItem = new()
                {
                    ID = worldItem.Data.ID,
                    Quantity = amountNotPickedUp,
                    Materials = worldItem.Data.Materials
                };

                Vector3 dropPos = transform.position + transform.forward * 1f;
                WorldItemGameObject worldObject = Instantiate(Registry.GetItem(worldItem.Data.ID).WorldItemPrefab, dropPos, Quaternion.identity);
                worldObject.Initialize(worldItem.Data.ID, amountNotPickedUp, true);
                Spawn(worldObject);
            }
        }
    }
    [ServerRpc]
    private void Server_DropItem_RPC(int quantity)
    {
        LocalResponse response = LocalDrop(DragGhost.ServerGhost, quantity, false);

        if (!response.Accepted)
        {
            Debug.Log($"<color=red>Drop Item Rollback: Response Accepted: {response.Accepted}</color>");
            List<SlotPatch> before = SnapshotSlots(response.Patches);
            Target_SyncSlots(Owner, before.ToArray());
            return;
        }
        else
        {
            LocalSyncSlots(response.Patches, true);
        }
    }
    [ServerRpc]
    private void Server_SlotToGhost_RPC(int fromSlot, int quantity)
    {
        LocalResponse response = LocalSlotToGhost(ServerSlots, DragGhost.ServerGhost, fromSlot, quantity);

        if (!response.Accepted)
        {
            Debug.Log($"<color=red>SlotToGhost Rollback: Response Accepted: {response.Accepted}</color>");
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
        LocalResponse response = LocalGhostToSlot(ServerSlots, DragGhost.ServerGhost, toSlot, Owner);

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
    private void Server_InstantEquip_RPC(int fromSlot)
    {
        LocalResponse response = LocalInstantEquip(ServerSlots, fromSlot, Owner);

        if (!response.Accepted)
        {
            Debug.Log($"<color=red>Instant Equip Rollback: Response Accepted: {response.Accepted}</color>");
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
    private LocalResponse LocalPickUp(List<InventorySlotData> slots, ItemSlotData data, out int amountNotPickedUp, int worldItemID = 0)
    {
        Item item = Registry.GetItem(data.ID);
        List<SlotPatch> patches = new();
        amountNotPickedUp = 0;
        bool isClient = slots == ClientSlots;

        if (!PickUpValid(slots, item, data, worldItemID))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.AddRange(PlayerHelperFunctions.SnapshotInventory(slots,false));
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        // Try Stack
        for (int i = 0; i < slots.Count; i++)
        {
            ItemSlotData slotData = slots[i].Data;
            if (!PlayerHelperFunctions.StackingValid(data, slotData, item.MaxStackSize)) continue;

            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(data, slotData, item.MaxStackSize);
            slotData.Quantity = stack;
            data.Quantity = remainder;

            patches.Add(new SlotPatch { Index = i, Data = slotData, Type = SlotType.Inventory });
            if (data.Quantity <= 0)
            {
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }
        // Try Get Empty
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData inventorySlotData = slots[i];
            ItemSlotData slotData = slots[i].Data;

            if (inventorySlotData.Type != ItemSlotType.Inventory) continue;
            if (!(inventorySlotData.Data.Quantity == 0)) continue;

            int add = Mathf.Min(item.MaxStackSize, data.Quantity);
            slotData.ID = data.ID;
            slotData.Quantity = add;
            data.Quantity -= add;

            patches.Add(new SlotPatch { Index = i, Data = slotData, Type = SlotType.Inventory });
            if (data.Quantity <= 0)
            {
                return new LocalResponse { Accepted = true, Patches = patches };
            }
        }

        amountNotPickedUp = data.Quantity;
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalDrop(ItemSlotData slotData, int quantity, bool isClient)
    {
        List<SlotPatch> patches = new();

        if (!DropValid(slotData, quantity))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new SlotPatch { Data = slotData, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }      

        slotData.Quantity -= quantity;
        if (slotData.Quantity <= 0)
            slotData.Clear();

        patches.Add(new SlotPatch { Data = slotData, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalSlotToGhost(List<InventorySlotData> slots, ItemSlotData ghost, int from, int quantity, NetworkConnection conn = default)
    {
        ItemSlotData slotData = slots[from].Data;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!SlotToGhostValid(slots, ghost, from, quantity))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = from, Data = slotData, Type = SlotType.Inventory });
            patches.Add(new() {Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        if (slots == ServerSlots && CheckIfIsEquipRequest(slots, from))
        {
            if (!EquipValid())
            {
                patches.Add(new() { Index = from, Data = slotData, Type = SlotType.Inventory });
                patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
                return new LocalResponse { Accepted = false, Patches = patches };
            }

            if (quantity == slots[from].Data.Quantity)
                Loadout.UnequipItem(slots[from].Type, conn);
        }

        ghost.ID = slotData.ID;
        ghost.Materials = slotData.Materials;
        ghost.Quantity += quantity;

        slotData.Quantity -= quantity;
        if (slotData.Quantity <= 0)
            slotData.Clear();

        patches.Add(new() { Index = from, Data = slotData, Type = SlotType.Inventory });
        patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalGhostToSlot(List<InventorySlotData> slots, ItemSlotData ghost, int to, NetworkConnection conn = default)
    {
        ItemSlotData slotData = slots[to].Data;
        Item ghostItem = Registry.TryGetItem(ghost.ID, out var tryGhostItem) ? tryGhostItem : null;
        Item toItem = Registry.TryGetItem(slotData.ID, out var toitem) ? toitem : null;
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!GhostToSlotValid(slots, ghost, ghostItem, to))
        {
            if (isClient) return new LocalResponse { Accepted = false };
            patches.Add(new() { Index = to, Data = slotData, Type = SlotType.Inventory });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = false, Patches = patches };
        }

        if (slots == ServerSlots && CheckIfIsEquipRequest(slots,to))
        {
            if (!EquipValid())
                return new LocalResponse { Accepted = false };

            if (toItem != null)
                Loadout.UnequipItem(slots[to].Type, conn);

            Loadout.EquipItem(tryGhostItem,  slots[to].Type, slots[to].Data.Materials, conn);
        }

        if (PlayerHelperFunctions.StackingValid(ghost, slots[to].Data, ghostItem.MaxStackSize))
        {
            var (stack,remainder) = PlayerHelperFunctions.TryStackItems(ghost, slots[to].Data, ghostItem.MaxStackSize);
            ghost.Quantity = remainder;
            slotData.Quantity = stack;
            if (ghost.Quantity <= 0)
                ghost.Clear();

            patches.Add(new() { Index = to, Data = slotData, Type = SlotType.Inventory });
            patches.Add(new() { Data = ghost, Type = SlotType.Ghost });
            return new LocalResponse { Accepted = true, Patches = patches };
        }

        patches.Add(new() { Index = to, Data = ghost, Type = SlotType.Inventory });
        patches.Add(new() { Data = slotData, Type = SlotType.Ghost });
        return new LocalResponse { Accepted = true, Patches = patches };
    }
    private LocalResponse LocalInstantEquip(List<InventorySlotData> slots, int from, NetworkConnection conn = default)
    {
        ItemSlotData slotData = slots[from].Data;
        Item item = Registry.GetItem(slotData.ID);
        List<SlotPatch> patches = new();
        bool isClient = slots == ClientSlots;

        if (!EquipRequestValid(slots, item, from))
        {
            return InvalidateInstantEquip(ref patches, slots, from, isClient);
        }

        List<int> unEquipSlots = GetEffectedEquipSlots(item.ItemSlotType);
        int to = GetEquipSlotIndex(item.ItemSlotType);

        //Try Stack
        if (PlayerHelperFunctions.MaxStackingValid(slots[from].Data, slots[to].Data))
        {
            ItemSlotData slotToData = slots[to].Data;
            if (slots[to].Data.Quantity == item.MaxStackSize)
                return new LocalResponse { Accepted = false };

            var (stack, remainder) = PlayerHelperFunctions.TryStackItems(slotData, slotToData, item.MaxStackSize);
            slotData.Quantity = remainder;
            slotData.Quantity = stack;
            if (slotData.Quantity <= 0)
                slotData.Clear();

            return new LocalResponse
            {
                Accepted = true,
                Patches = new List<SlotPatch>
                {
                    new() { Index = to, Data = slotToData, Type = SlotType.Inventory },
                    new() { Index = from, Data = slotData, Type = SlotType.Inventory }
                }
            };
        }

        //Unequip overflow items
        bool hasOverflow = unEquipSlots.Count == 2 && slots[unEquipSlots[0]].Data.HasItem()  && slots[unEquipSlots[1]].Data.HasItem();

        if (hasOverflow)
        {
            int emptySlot = slots.FindIndex(s => s.Type == ItemSlotType.Inventory && !s.Data.HasItem());
            if (emptySlot < 0) return InvalidateInstantEquip(ref patches, slots, from, isClient);

            if (!isClient) Loadout.UnequipItem(slots[unEquipSlots[1]].Type, conn);

            ItemSlotData overflowData = slots[unEquipSlots[1]].Data;
            ItemSlotData emptyData = slots[emptySlot].Data;

            (overflowData, emptyData) = (emptyData, overflowData);

            patches.Add(new SlotPatch { Index = emptySlot, Data = emptyData, Type = SlotType.Inventory });
            patches.Add(new SlotPatch { Index = unEquipSlots[1], Data = overflowData, Type = SlotType.Inventory });
        }

        int firstSlot = unEquipSlots.Count > 0 ? unEquipSlots[0] : to;

        ItemSlotData fromData = slots[from].Data;
        ItemSlotData firstSlotData = slots[firstSlot].Data;

        if (!isClient)
        {
            if (firstSlotData.HasItem())
                Loadout.UnequipItem(slots[firstSlot].Type, conn);
            Loadout.EquipItem(item, slots[to].Type, slots[to].Data.Materials, conn);
        }

        (fromData, firstSlotData) = (firstSlotData, fromData);

        patches.Add(new SlotPatch { Index = from, Data = fromData, Type = SlotType.Inventory });
        patches.Add(new SlotPatch { Index = firstSlot, Data = firstSlotData, Type = SlotType.Inventory });

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
            else if (patch.Type == SlotType.Inventory)
            {
                List<InventorySlotData> slots = isServer ? ServerSlots : ClientSlots;
                if (PlayerHelperFunctions.SlotValid(slots, patch.Index))
                    slots[patch.Index].Data = patch.Data;
            }
        }
    }
    public bool CheckIfIsEquipRequest(List<InventorySlotData> slots, int toSlot)
    {
        if (slots[toSlot].Type != ItemSlotType.Inventory)
            return true;

        return false;
    }

    public List<int> GetEffectedEquipSlots(ItemSlotType type)
    {
        List<int> indices = new();
        switch (type)
        {
            case ItemSlotType.MainHand:
                indices.Add(EquipSlotLookup[ItemSlotType.MainHand]);
                break;
            case ItemSlotType.OffHand:
                indices.Add(EquipSlotLookup[ItemSlotType.OffHand]);
                break;
            case ItemSlotType.TwoHanded:
                indices.Add(EquipSlotLookup[ItemSlotType.MainHand]);
                indices.Add(EquipSlotLookup[ItemSlotType.OffHand]);
                break;
            case ItemSlotType.AnyHand:
                if(Loadout.MainHand == null)
                    indices.Add(EquipSlotLookup[ItemSlotType.MainHand]);
                else if(Loadout.OffHand == null)
                    indices.Add(EquipSlotLookup[ItemSlotType.OffHand]);
                else
                    indices.Add(EquipSlotLookup[ItemSlotType.MainHand]);
                break;
            case ItemSlotType.Pick:
                indices.Add(EquipSlotLookup[ItemSlotType.Pick]);
                break;
            case ItemSlotType.Axe:
                indices.Add(EquipSlotLookup[ItemSlotType.Axe]);
                break;
            case ItemSlotType.Head:
                indices.Add(EquipSlotLookup[ItemSlotType.Head]);
                break;
            case ItemSlotType.Chest:
                indices.Add(EquipSlotLookup[ItemSlotType.Chest]);
                break;
            case ItemSlotType.Legs:
               indices.Add(EquipSlotLookup[ItemSlotType.Legs]);
                break;
            default:
                break;
        }
        return indices;
    }
    public int GetEquipSlotIndex(ItemSlotType type)
    {
        switch (type)
        {
            case ItemSlotType.MainHand:
                return EquipSlotLookup[ItemSlotType.MainHand];
            case ItemSlotType.OffHand:
                return EquipSlotLookup[ItemSlotType.OffHand];
            case ItemSlotType.TwoHanded:
                return EquipSlotLookup[ItemSlotType.MainHand];
            case ItemSlotType.AnyHand:
                if (Loadout.MainHand == null)
                    return EquipSlotLookup[ItemSlotType.MainHand];
                else if (Loadout.OffHand == null)
                    return EquipSlotLookup[ItemSlotType.OffHand];
                else
                    return EquipSlotLookup[ItemSlotType.MainHand];
            case ItemSlotType.Pick:
                return EquipSlotLookup[type];
            case ItemSlotType.Axe:
                return EquipSlotLookup[type];
            case ItemSlotType.Head:
                return EquipSlotLookup[type];
            case ItemSlotType.Chest:
                return EquipSlotLookup[type];
            case ItemSlotType.Legs:
                return EquipSlotLookup[type];
            default:
                break;
        }
        return default;
    }

    #endregion

    #region  Client RPCs
    [TargetRpc]
    public void Target_SyncSlots(NetworkConnection conn, SlotPatch[] patches)
    {
        LocalSyncSlots(patches.ToList(), false);
        InvokeChange(new List<SlotPatch>(patches));
    }


    #endregion

    #region Validation
    private bool PickUpValid(List<InventorySlotData> slots, Item item, ItemSlotData data, int worldItemID)
    {
        if (item == null) return false;
        if (data.Quantity <= 0) return false;

        if (slots == ServerSlots)
        {
            WorldItem worldItem = ServerWorldItemStash.Instance.GetWorldItem(worldItemID);
            if (worldItem == null) return false;
            float distance = Vector3.Distance(transform.position, worldItem.WorldPos);
            if (distance > 5) return false;

            if (worldItem.Data.ID != data.ID) return false;
            if (!PlayerHelperFunctions.NullSafeSequenceEqual(worldItem.Data.Materials, data.Materials)) return false;
            if (worldItem.Data.Quantity != data.Quantity) return false;
        }
        
        return true;
    }
    private bool DropValid(ItemSlotData slotData, int quantity)
    {
        if (!slotData.HasItem()) return false;
        if (quantity <= 0 || quantity > slotData.Quantity) return false;
        if (!Registry.TryGetItem(slotData.ID, out _)) return false;
        return true;
    }
    public bool SlotToGhostValid(List<InventorySlotData> slots, ItemSlotData ghost, int from, int quantity)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, from)) return false;
        InventorySlotData slotData = slots[from];
        if (quantity <= 0 || quantity > slotData.Data.Quantity) return false;
        if (!PlayerHelperFunctions.TransferValid(slotData.Data, ghost)) return false;

        return true;
    }
    private bool GhostToSlotValid(List<InventorySlotData> slots, ItemSlotData ghost, Item ghostItem, int to)
    {

        if (!PlayerHelperFunctions.SlotValid(slots, to)) return false;
        InventorySlotData slotData = slots[to];
        if (!SlotTypeValid(ghostItem, slotData)) return false;
        if (!PlayerHelperFunctions.TransferValid(ghost, slotData.Data)) return false;

        return true;
    }
    public bool CanAcceptItem(ItemSlotData item)
    {
        foreach (var slot in ClientSlots)
        {
            if (slot.Type != ItemSlotType.Inventory) continue;

            if (!slot.Data.HasItem() || PlayerHelperFunctions.StackingValid(item,slot.Data,Registry.GetItem(item.ID).MaxStackSize))
            {
                return true;
            }
        }
        return false;
    }
    public bool CanIncrement(ItemSlotData data, int quantity)
    {
        if (data.Quantity - quantity <= 0) return false;
        if (DragGhost.ClientGhost.HasItem() && DragGhost.ClientGhost.ID != data.ID) return false;
        if (!PlayerHelperFunctions.NullSafeSequenceEqual(DragGhost.ClientGhost.Materials, data.Materials)) return false;
        if (DragGhost.ClientGhost.Quantity + quantity >= Registry.GetItem(data.ID).MaxStackSize) return false;
        return true;
    }
    private bool EquipValid()
    {
        return true;
    }
    private bool EquipRequestValid(List<InventorySlotData> slots, Item item, int from)
    {
        if (!PlayerHelperFunctions.SlotValid(slots, from)) return false;
        if (!slots[from].Data.HasItem()) return false;
        if (slots[from].Type != ItemSlotType.Inventory) return false;
        if (item == null) return false;
        if (item.ItemSlotType == ItemSlotType.Inventory) return false;
        return true;
    }
    private bool SlotTypeValid(Item item, InventorySlotData slotData)
    {
        if (slotData.Type == ItemSlotType.Inventory)
            return true;

        var allowed = item.ItemSlotType;

        switch (slotData.Type)
        {
            case ItemSlotType.Axe:
            case ItemSlotType.Pick:
            case ItemSlotType.Head:
            case ItemSlotType.Chest:
            case ItemSlotType.Legs:
                return allowed == slotData.Type;

            case ItemSlotType.MainHand:
                {
                    return allowed == slotData.Type || allowed == ItemSlotType.AnyHand || allowed == ItemSlotType.TwoHanded;
                }
            case ItemSlotType.OffHand:

            default:
                return false;
        }
    }
    #endregion

    #region Invalidation
    private LocalResponse InvalidateInstantEquip(ref List<SlotPatch> patches, List<InventorySlotData> slots, int slotIndex, bool isClient)
    {
        if (isClient) return new LocalResponse { Accepted = false };
        patches.Add(new() { Index = slotIndex, Data = slots[slotIndex].Data, Type = SlotType.Inventory });
        patches.AddRange(PlayerHelperFunctions.SnapshotInventory(slots, true));
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
                case SlotType.Inventory:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = ServerSlots[slotPatch.Index].Data, Index = slotPatch.Index });
                    break;
                case SlotType.Ghost:
                    beforePatches.Add(new SlotPatch { Type = SlotType.Inventory, Data = DragGhost.ServerGhost });
                    break;
                default:
                    break;
            }
        }
        return beforePatches;
    }

    public void InvokeChange(List<SlotPatch> patches)
    {
        OnInventoryChanged?.Invoke(patches);
    }
}
