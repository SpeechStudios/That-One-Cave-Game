using UnityEngine;
using UnityEngine.EventSystems;

public class SmeltingSlot : ItemSlot
{
    private int SlotIndex;

    public void Setup(int slotIndex)
    {
        SlotIndex = slotIndex;
        SlotData.Clear();
    }
    public override void SlotToGhost()
    {
        if (!SlotData.HasItem()) return;
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Smelting.TargetSmelting.GhostToSlot(SlotIndex, PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.Quantity);
    }
    public override void RightMouseClicked()
    {
        if (PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.HasItem())
        {
            PlayerUI.UI_Smelting.TargetSmelting.GhostToSlot(SlotIndex, 1);
        }
        else
        {
            if (!SlotData.HasItem()) return;
            PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, Mathf.CeilToInt(SlotData.Quantity / 2f));
        }
    }
    public override void RightDragEnter()
    {
        base.RightDragEnter();
        PlayerUI.UI_Smelting.TargetSmelting.GhostToSlot(SlotIndex, 1);
    }
    public override void ShiftRightMouseClicked()
    {
        if (!SlotData.HasItem()) return;
        PlayerUI.UI_Smelting.TargetSmelting.InstantGrab(SlotIndex);
    }
}
