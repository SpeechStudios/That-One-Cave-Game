using UnityEngine.EventSystems;

public class CraftingSlot : ItemSlot, IPointerDownHandler
{
    public int SlotIndex;

    public override void SlotToGhost()
    {
        PlayerUI.UI_Crafting.TargetCrafting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Crafting.TargetCrafting.GhostToSlot(SlotIndex);
    }
    public override void RightMouseUp()
    {
        PlayerUI.UI_Crafting.TargetCrafting.SlotToGhost(SlotIndex, Quantity);
    }
    public override void ShiftRightMouseClicked()
    {
        PlayerUI.UI_Crafting.TargetCrafting.InstantGrab(SlotIndex);
    }
}
