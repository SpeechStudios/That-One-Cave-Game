using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CraftingSlot : ItemSlot, IPointerDownHandler
{
    public TextMeshProUGUI RequiredMaterialText;
    private CraftingComponent Component;
    [HideInInspector] public bool RequirementMet;
    private int SlotIndex;

    public void Setup(CraftingComponent component, int slotIndex)
    {
        Component = component;
        RequiredMaterialText.text = $"{component.ResourceType} ({component.RequiredQuantity})";
        SlotIndex = slotIndex;
    }
    public override void SlotToGhost()
    {
        CraftingManager.Instance.TargetCrafting.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        CraftingManager.Instance.TargetCrafting.GhostToSlot(SlotIndex);
    }
    public override void RightMouseUp()
    {
        CraftingManager.Instance.TargetCrafting.SlotToGhost(SlotIndex, Quantity);
    }
    public override void ShiftRightMouseClicked()
    {
        CraftingManager.Instance.TargetCrafting.InstantGrab(SlotIndex);
    }
}
