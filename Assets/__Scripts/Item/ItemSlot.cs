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
public class ItemSlot : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image Icon;
    public TextMeshProUGUI QuantityText;
    [HideInInspector] public ItemSlotData SlotData;

    public bool Incrementing;
    public bool PointerIsOver;
    public int Quantity;

    private float CurrentSpeed = 0f;
    private float Accumulator;

    private readonly float Acceleration = 10f;
    private readonly float MaxSpeed = 10f;

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
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        PointerIsOver = false;
        CurrentSpeed = 0f;
        Accumulator = 0f;
    }
    public virtual void OnPointerDown(PointerEventData e)
    {
        if (DragGhostManager.Instance.Incrementing) return;

        if (e.button == PointerEventData.InputButton.Left)
        {
            if (DragGhostManager.Instance.TargetGhost.ClientGhost.HasItem())
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
            }
            else
            {
                if (!SlotData.HasItem()) return;
                Increment();
                Incrementing = true;
                DragGhostManager.Instance.Incrementing = true;
            }
        }
    }
    public virtual void OnPointerUp(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
        {
            RightMouseUp();

            Incrementing = false;
            CurrentSpeed = 0f;
            Accumulator = 0f;
            Quantity = 0;
            DragGhostManager.Instance.Incrementing = false;
        }
    }
    public virtual void Update()
    {
        if (!Incrementing) return;

        float dt = Time.deltaTime;
        CurrentSpeed = Mathf.Min(CurrentSpeed + Acceleration * dt, MaxSpeed);
        Accumulator += CurrentSpeed * dt;

        while (Accumulator >= 1f)
        {
            Increment();
            Accumulator -= 1f;
        }
    }

    public virtual void SlotToGhost() { }
    public virtual void GhostToSlot() { }
    public virtual void ShiftRightMouseClicked() { }
    public virtual void RightMouseUp() { }
    public virtual void Increment()
    {
        if (!InventoryManager.Instance.TargetInventory.CanIncrement(SlotData, Quantity)) return;
        if (!PointerIsOver) return;

        Quantity++;
        var tempData = new ItemSlotData { ID = SlotData.ID, Materials = SlotData.Materials, Quantity = DragGhostManager.Instance.TargetGhost.ClientGhost.Quantity + Quantity };
        DragGhostManager.Instance.UpdateTempUI(tempData);

        UpdateUI(SlotData.Quantity - Quantity);
    }

}
