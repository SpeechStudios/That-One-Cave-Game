using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public GameObject InventoryCanvas;
    public List<InventorySlot> Slots;

    [HideInInspector] public PlayerInventoryModule TargetInventory;

    public void Init()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].Setup(i);
        }
    }
    public void Bind(PlayerInventoryModule targetInventory)
    {
        TargetInventory = targetInventory;
        TargetInventory.OnInventoryChanged += HandleInventoryChanged;
    }
    public void SpawnSlots(PlayerInventoryModule inventory, bool isServer)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            ItemSlotData emptySlotData = new();
            inventory.SpawnSlots(emptySlotData, Slots[i].InventorySlotType, isServer);
        }
    }

    private void HandleInventoryChanged(List<SlotPatch> patches)
    {
        foreach (var patch in patches)
        {
            if (patch.Type == SlotType.Ghost)
            {
                DragGhostManager.Instance.UpdateDragGhost(patch.Data);
            }
            if (patch.Type == SlotType.Inventory)
            {
                Slots[patch.Index].SlotData = patch.Data;
                Slots[patch.Index].UpdateUI();
            }
        }
    }
}
