using UnityEngine;

public class SmeltingOutcomeSlot : ItemSlot
{
    private int SlotIndex;

    public void Setup()
    {
        SlotIndex = 2;
        SlotData.Clear();
    }

    public override void SlotToGhost()
    {
        if (!SlotData.HasItem()) return;
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        if (!SlotData.HasItem()) return;
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void RightMouseClicked()
    {
        if (!SlotData.HasItem()) return;
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, 1);
    }
    public override void ShiftRightMouseClicked()
    {
        if (!SlotData.HasItem()) return;
        PlayerUI.UI_Smelting.TargetSmelting.InstantGrab(SlotIndex);
    }
}
