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
        PlayerUI.UI_Smelting.TargetSmelting.SlotToGhost(ForgeIndex, SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Smelting.TargetSmelting.GhostToSlot(ForgeIndex, SlotIndex);
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
