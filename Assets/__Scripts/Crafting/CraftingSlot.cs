using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CraftingSlot : ItemSlot, IPointerDownHandler
{
    public TextMeshProUGUI RequiredMaterialText;
    [HideInInspector] public bool RequirementMet;
    private int SlotIndex;

    public void Setup(CraftingComponent component, int slotIndex)
    {
        RequiredMaterialText.text = $"{component.ResourceType} ({component.RequiredQuantity})";
        SlotIndex = slotIndex;
    }
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
