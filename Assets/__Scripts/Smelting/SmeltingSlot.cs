using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;

public class SmeltingSlot : ItemSlot
{
    private int ForgeIndex;
    private int SlotIndex;

    public void Setup(int forgeIndex, int slotIndex)
    {
        SlotIndex = slotIndex;
        ForgeIndex = forgeIndex;
        SlotData.Clear();
    }
    public override void OnPointerEnter(PointerEventData e)
    {
        base.OnPointerEnter(e);
    }

    public override void OnPointerExit(PointerEventData e)
    {
        base.OnPointerExit(e);
    }
    public override void SlotToGhost()
    {
        SmeltingManager.Instance.TargetSmelter.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        SmeltingManager.Instance.TargetSmelter.GhostToSlot(ForgeIndex, SlotIndex);
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
