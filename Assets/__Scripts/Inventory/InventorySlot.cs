using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum ItemSlotType
{
    Inventory = 0,
    Weapon = 1,
    Head = 2,
    Chest = 3,
    Legs = 4,
    Pickaxe = 5,
    Axe = 6,
}
[System.Serializable]
public struct ItemSlotData
{
    public int ID;
    public int Quantity;
    public int[] Materials;
    public int[] ReinforcedMaterials;

    public void Clear()
    {
        ID = 0;
        Quantity = 0;
        Materials = null;
    }
    public bool HasItem() { return ID > 0; }
}

public class InventorySlot : ItemSlot
{
    public Image HoverImage;
    public ItemSlotType InventorySlotType;
    public int SlotIndex { get; private set; }


    public void Setup(int index)
    {
        SlotIndex = index;
    }

    public override void OnPointerEnter(PointerEventData e)
    {
        base.OnPointerEnter(e);

        if (!PlayerUI.UI_DragGhost.DragIcon.enabled)
            HoverImage.enabled = true;
    }

    public override void OnPointerExit(PointerEventData e)
    {
        base.OnPointerExit(e);
        HoverImage.enabled = false;
    }
    public void OnDisable()
    {
        HoverImage.enabled = false;
    }
    public override void SlotToGhost()
    {
        if (!SlotData.HasItem()) return;

        PlayerUI.UI_Inventory.TargetInventory.SlotToGhost(SlotIndex, SlotData.Quantity);
    }
    public override void GhostToSlot()
    {
        PlayerUI.UI_Inventory.TargetInventory.GhostToSlot(SlotIndex, PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.Quantity);
    }
    public override void RightMouseClicked()
    {
        if(PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.HasItem())
        {
            PlayerUI.UI_Inventory.TargetInventory.GhostToSlot(SlotIndex, 1);
        }
        else
        {
            PlayerUI.UI_Inventory.TargetInventory.SlotToGhost(SlotIndex, Mathf.CeilToInt(SlotData.Quantity / 2f));
        }
    }
    public override void RightDragEnter()
    {
        base.RightDragEnter();
        PlayerUI.UI_Inventory.TargetInventory.GhostToSlot(SlotIndex, 1);
    }
    public override void ShiftRightMouseClicked()
    {
        if (!SlotData.HasItem()) return;

        if (Registry.GetItem(SlotData.ID).ItemType != ItemType.Material)
        {
            if (InventorySlotType == ItemSlotType.Inventory)
            {
                PlayerUI.UI_Inventory.TargetInventory.InstantEquip(SlotIndex);
            }
            else
            {
                PlayerUI.UI_Inventory.TargetInventory.InstantGrab(SlotIndex);
            }
            return;
        }
        else
        {
            if (TabManager.Instance.CurrentTab == Tab.Smelting)
            {
                PlayerUI.UI_Smelting.TargetSmelting.InstantFill(SlotIndex);
            }
        }
    }

}
