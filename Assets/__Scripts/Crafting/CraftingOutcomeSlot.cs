using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CraftingOutcomeSlot : ItemSlot, IPointerDownHandler
{
    public CraftingManager Manager;
    public void OnRecipeComplete(bool isReady, ItemSlotData itemData)
    {
        if (isReady)
        {
            SlotData = itemData;
            SlotData.Materials = Manager.TargetCrafting.ClientSlots.Select(slot => (int)Registry.GetItem(slot.Data.ID).MaterialType).ToArray();
            UpdateUI();
        }
        else
        {
            SlotData.Clear();
            UpdateUI();
        }
    }
    public override void UpdateUI(int quantity = -1)
    {
        base.UpdateUI();
        //Show Crafted Item Stats
    }
    public override void OnPointerDown(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            if (SlotData.HasItem())
            {
                if (CraftingManager.Instance.TargetCrafting.CraftItem())
                {
                    UpdateUI();
                    DragGhostManager.Instance.UpdateUI();
                }
            }
        }
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                ShiftRightMouseClicked();
            }
            else
            {
                if (SlotData.Quantity == 0) return;
                Increment();
                Incrementing = true;
            }
        }
    }
    public override void ShiftRightMouseClicked()
    {
        if (CraftingManager.Instance.TargetCrafting.InstantCraft())
        {
            Debug.Log("Instant Crafting");
            UpdateUI();
        }
    }
    public override void Increment()
    {
        if (CraftingManager.Instance.TargetCrafting.CraftItem())
        {
            UpdateUI();
            DragGhostManager.Instance.UpdateUI();
        }
    }
}
