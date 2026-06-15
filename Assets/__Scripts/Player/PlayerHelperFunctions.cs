using FishNet.Connection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PlayerHelperFunctions
{
    public static bool StackingValid(ItemSlotData itemData, ItemSlotData slotData, int maxStack)
    {
        if (slotData.ID != itemData.ID) return false;
        if (!NullSafeSequenceEqual(itemData.Materials, slotData.Materials)) return false;
        if (slotData.Quantity >= maxStack) return false;
        return true;
    }
    public static bool MaxStackingValid(ItemSlotData itemData, ItemSlotData slotData)
    {
        if (slotData.ID != itemData.ID) return false;
        if (!NullSafeSequenceEqual(itemData.Materials, slotData.Materials)) return false;
        return true;
    }
    public static (int, int) TryStackItems(ItemSlotData itemData, ItemSlotData slotData, int MaxStackSize)
    {
        int remaining = itemData.Quantity;
        int space = MaxStackSize - slotData.Quantity;
        int add = Mathf.Min(space, remaining);

        slotData.Quantity += add;
        itemData.Quantity -= add;
        return (slotData.Quantity, itemData.Quantity);
    }
    public static bool SlotValid<T>(List<T> slots, int i) => i >= 0 && i < slots.Count;
    public static bool TransferValid(ItemSlotData slot1, ItemSlotData slot2)
    {
        if (!Registry.TryGetItem(slot1.ID, out var slotItem)) return false;
        if (slot2.HasItem() && slot2.ID != slot1.ID) return false;
        if (!NullSafeSequenceEqual(slot1.Materials, slot2.Materials)) return false;
        if (slot1.Quantity + slot2.Quantity > slotItem.MaxStackSize) return false;

        return true;
    }
    public static bool NullSafeSequenceEqual(int[] a, int[] b)
    {
        if (a == null || b == null) return true;
        return a.SequenceEqual(b);
    }
    public static List<SlotPatch> SnapshotInventory(List<InventorySlotData> slots, bool isEquipSlotSnapshot)
    {
        List<SlotPatch> patches = new();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Type != ItemSlotType.Inventory && !isEquipSlotSnapshot) continue;
            if (slots[i].Type == ItemSlotType.Inventory && isEquipSlotSnapshot) continue;

            patches.Add(new() { Index = i, Data = slots[i].Data, Type = SlotType.Inventory });
        }
        return patches;
    }
    public static List<SlotPatch> SnapshotSmelter(List<SmeltingSlotData> slots)
    {
        List<SlotPatch> patches = new();
        for (int i = 0; i < slots.Count; i++)
        {
            patches.Add(new() { Index = i, Data = slots[i].Data, Type = SlotType.Smelting });
        }
        return patches;
    }
    public static List<SlotPatch> SnapshotCrafting(List<CraftingSlotData> slots)
    {
        List<SlotPatch> patches = new();
        for (int i = 0; i < slots.Count; i++)
        {
            patches.Add(new() { Index = i, Data = slots[i].Data, Type = SlotType.Crafting });
        }
        return patches;
    }
}
