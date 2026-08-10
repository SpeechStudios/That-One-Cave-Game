using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingSlot : ItemSlot, IPointerDownHandler
{
    public int SlotIndex;

    public override void SlotToGhost()
    {
        if (!SlotData.HasItem()) return;

        PlayerUI.UI_Crafting.TargetCrafting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Crafting.TargetCrafting.GhostToSlot(SlotIndex, PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.Quantity);
    }
    public override void RightMouseClicked()
    {
        if (PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.HasItem())
        {
            PlayerUI.UI_Crafting.TargetCrafting.GhostToSlot(SlotIndex, 1);
        }
        else
        {
            PlayerUI.UI_Crafting.TargetCrafting.SlotToGhost(SlotIndex, Mathf.CeilToInt(SlotData.Quantity / 2f));
        }
    }
    public override void RightDragEnter()
    {
        base.RightDragEnter();
        PlayerUI.UI_Crafting.TargetCrafting.GhostToSlot(SlotIndex, 1);
    }
    public override void ShiftRightMouseClicked()
    {
        PlayerUI.UI_Crafting.TargetCrafting.InstantGrab(SlotIndex);
    }
}
