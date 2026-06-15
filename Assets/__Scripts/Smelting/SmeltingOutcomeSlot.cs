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
        SmeltingManager.Instance.TargetSmelter.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        SmeltingManager.Instance.TargetSmelter.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void RightMouseUp()
    {
        SmeltingManager.Instance.TargetSmelter.SlotToGhost(ForgeIndex, SlotIndex, Quantity);
    }
    public override void ShiftRightMouseClicked()
    {
        SmeltingManager.Instance.TargetSmelter.InstantGrab(ForgeIndex, SlotIndex);
    }
}
