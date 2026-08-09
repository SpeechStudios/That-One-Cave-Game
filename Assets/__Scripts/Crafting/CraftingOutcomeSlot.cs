using GameKit.Dependencies.Utilities;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CraftingOutcomeSlot : ItemSlot, IPointerDownHandler
{
    public void OnRecipeComplete(bool isReady, PlayerCraftingModule targetCrafting)
    {
        if (isReady)
        {
            SlotData.ID = targetCrafting.ClientRecipe.CraftedOutcome.ID;
            SlotData.Quantity = targetCrafting.ClientRecipe.CraftedOutcomeQuantity;
            SlotData.Materials = targetCrafting.Materials.OrderBy(kvp => kvp.Key).Select(kvp => (int)kvp.Value).ToArray();
            UpdateUI();
        }
        else
        {
            SlotData.Clear();
            UpdateUI();
            Debug.Log("Updating UI With Empty");
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
                if (PlayerUI.UI_Crafting.TargetCrafting.CraftItem())
                {
                    UpdateUI();
                    PlayerUI.UI_DragGhost.UpdateUI();
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
        if (PlayerUI.UI_Crafting.TargetCrafting.InstantCraft())
        {
            UpdateUI();
        }
    }
    public override void Increment()
    {
        if (PlayerUI.UI_Crafting.TargetCrafting.CraftItem())
        {
            UpdateUI();
            PlayerUI.UI_DragGhost.UpdateUI();
        }
    }
}
