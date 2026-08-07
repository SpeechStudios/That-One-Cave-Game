using UnityEngine;

public class SmeltingOutcomeSlot : ItemSlot
{
    private int ForgeIndex;
    private int SlotIndex;

    public void Setup(int forgeIndex)
    {
        SlotIndex = 2;
        ForgeIndex = forgeIndex;
        SlotData.Clear();
    }

    public override void SlotToGhost()
    {
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void RightMouseUp()
    {
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(ForgeIndex, SlotIndex, Quantity);
    }
    public override void ShiftRightMouseClicked()
    {
        PlayerUI.UI_Smelting.TargetSmelting.InstantGrab(ForgeIndex, SlotIndex);
    }
}
