using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragGhostUIManager : MonoBehaviour
{


    [Header("UI")]
    public Image DragIcon;
    public TextMeshProUGUI StackCountText;
    public Canvas Canvas;
    public RectTransform RectTransform;
    public Vector2 Pivot = new(0, 0.5f);

    [HideInInspector] public PlayerDragGhostModule TargetGhost;
    private InventoryUIManager UI_Inventory;
    internal bool IsRightDragging;
    internal bool IsShiftRightDragging;
    internal HashSet<ItemSlot> RightDraggedSlots = new();
    private void Start()
    {
        UI_Inventory = PlayerUIManager.Instance.UI_Inventory;
    }
    public void Init()
    {

    }

    public void Bind(PlayerDragGhostModule targetGhost)
    {
        TargetGhost = targetGhost;
    }
    public void UpdateDragGhost(ItemSlotData data)
    {
        if (!TargetGhost.ClientGhost.HasItem()) { ClearDragGhost(); return; }
        TargetGhost.ClientGhost = data;
        UpdateUI();
    }
    public void UpdateUI()
    {
        if(!TargetGhost.ClientGhost.HasItem())
        {
            DragIcon.enabled = false;
            StackCountText.text = "";
        }
        else
        {
            DragIcon.sprite = Registry.GetItem(TargetGhost.ClientGhost.ID).Icon;
            DragIcon.enabled = true;
            StackCountText.text = TargetGhost.ClientGhost.Quantity > 1 ? TargetGhost.ClientGhost.Quantity.ToString() : string.Empty;
        }
    }
    public void UpdateTempUI(ItemSlotData data)
    {
        DragIcon.sprite = Registry.GetItem(data.ID).Icon;
        DragIcon.enabled = true;
        StackCountText.text = data.Quantity > 1 ? data.Quantity.ToString() : string.Empty;
    }

    public void Update()
    {
        if (IsRightDragging && Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            StopRightDrag();
        }
        if(IsShiftRightDragging && Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
        {
            IsShiftRightDragging = false;
        }

        if (TargetGhost == null) return;
        if (!TargetGhost.ClientGhost.HasItem()) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(Canvas.transform as RectTransform,  screenPos + Pivot, Canvas.worldCamera, out Vector2 localPos);
        RectTransform.anchoredPosition = localPos;
    }

    public void ClearDragGhost()
    {
        TargetGhost.ClientGhost.Clear();
        UpdateUI();
    }
    public void ReturnToSender()
    {
        if (!TargetGhost.ClientGhost.HasItem()) return;

        for (int i = 0; i < UI_Inventory.Slots.Count; i++)
        {
            if (UI_Inventory.TargetInventory.GhostToSlot(i, TargetGhost.ClientGhost.Quantity))
            {
                return;
            }
        }
        UI_Inventory.TargetInventory.DropItem(TargetGhost.ClientGhost.Quantity);
    }
    public void StartRightDrag()
    {
        IsRightDragging = true;
        RightDraggedSlots.Clear();
    }
    public void StopRightDrag()
    {
        IsRightDragging = false;
        RightDraggedSlots.Clear();
    }
}
