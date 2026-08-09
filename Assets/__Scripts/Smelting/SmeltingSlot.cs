using UnityEngine.EventSystems;

public class SmeltingSlot : ItemSlot
{
    private int SlotIndex;

    public void Setup(int slotIndex)
    {
        SlotIndex = slotIndex;
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
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Smelting.TargetSmelting.GhostToSlot(SlotIndex);
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
