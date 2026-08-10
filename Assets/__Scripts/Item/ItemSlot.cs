using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum SlotType
{
    Inventory,
    Crafting,
    Smelting,
    Ghost,
    Empty,
}
public class ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image Icon;
    public TextMeshProUGUI QuantityText;
    [HideInInspector] public ItemSlotData SlotData;

    internal bool PointerIsOver;
    internal int Quantity;


    internal PlayerUIManager PlayerUI;

    public virtual void Start()
    {
        PlayerUI = PlayerUIManager.Instance;
    }
    public virtual void UpdateUI(int quantity = -1)
    {
        int quantityUI = quantity < 0 ? SlotData.Quantity : quantity;
        if (!SlotData.HasItem() || quantityUI == 0)
        {
            Icon.enabled = false;
            QuantityText.text = "";
        }
        else
        {
            Icon.enabled = true;
            Icon.sprite = Registry.GetItem(SlotData.ID).Icon;
            QuantityText.text = quantityUI > 1 ? quantityUI.ToString() : "";
        }
    }
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        PointerIsOver = true;
        if (PlayerUI.UI_DragGhost.IsRightDragging)
        {
            RightDragEnter();
        }
        if(PlayerUI.UI_DragGhost.IsShiftRightDragging)
        {
            ShiftRightMouseClicked();
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        PointerIsOver = false;
    }
    public virtual void OnPointerDown(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            if (PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.HasItem())
            {
                GhostToSlot();
            }
            else
            {
                SlotToGhost();
            }
        }
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (Keyboard.current.shiftKey.isPressed)
            {
                ShiftRightMouseClicked();
                PlayerUI.UI_DragGhost.IsShiftRightDragging = true;
                return;
            }

            RightMouseClicked();

            if (PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.HasItem())
            {
                PlayerUI.UI_DragGhost.StartRightDrag();
            }
        }
    }

    public virtual void RightDragEnter()
    {
        if (PlayerUI.UI_DragGhost.RightDraggedSlots.Contains(this))
            return;

        if (!PlayerUI.UI_DragGhost.TargetGhost.ClientGhost.HasItem())
            return;

        PlayerUI.UI_DragGhost.RightDraggedSlots.Add(this);
    }
    public virtual void SlotToGhost() { }
    public virtual void GhostToSlot() { }
    public virtual void RightMouseClicked() { }
    public virtual void ShiftRightMouseClicked() { }

}
