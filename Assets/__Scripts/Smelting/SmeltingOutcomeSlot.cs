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
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void RightMouseUp()
    {
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, Quantity);
    }
    public override void ShiftRightMouseClicked()
    {
        PlayerUI.UI_Smelting.TargetSmelting.InstantGrab(SlotIndex);
    }
}
