using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUIManager : MonoBehaviour
{
    public GameObject InventoryCanvas;
    public List<InventorySlot> Slots;

    [HideInInspector] public PlayerInventoryModule TargetInventory;

    private DragGhostUIManager UI_DragGhost;
    private void Start()
    {
        UI_DragGhost = PlayerUIManager.Instance.UI_DragGhost;
    }
    public void Init()
    {
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
               UI_DragGhost.UpdateDragGhost(patch.Data);
            }
            if (patch.Type == SlotType.Inventory)
            {
                Slots[patch.Index].SlotData = patch.Data;
                Slots[patch.Index].UpdateUI();
            }
        }
    }
}
