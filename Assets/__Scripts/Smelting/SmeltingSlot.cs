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
        SmeltingManager.Instance.TargetSmelting.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        SmeltingManager.Instance.TargetSmelting.GhostToSlot(ForgeIndex, SlotIndex);
    }
    public override void RightMouseUp()
    {
        SmeltingManager.Instance.TargetSmelting.SlotToGhost(ForgeIndex, SlotIndex, Quantity);
    }
    public override void ShiftRightMouseClicked()
    {
        SmeltingManager.Instance.TargetSmelting.InstantGrab(ForgeIndex, SlotIndex);
    }
}
