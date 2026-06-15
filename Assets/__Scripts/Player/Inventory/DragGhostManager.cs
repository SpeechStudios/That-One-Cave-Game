using FishNet.Connection;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragGhostManager : MonoBehaviour
{
    public static DragGhostManager Instance { get; private set; }

    [Header("UI")]

    public Image DragIcon;
    public TextMeshProUGUI StackCountText;
    public Canvas Canvas;
    public RectTransform RectTransform;
    public Vector2 Pivot = new(0, 0.5f);
    public bool Incrementing;

    [HideInInspector] public PlayerDragGhostModule TargetGhost;
    public void Init()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
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
        if (TargetGhost == null) return;
        if (!TargetGhost.ClientGhost.HasItem() && !Incrementing) return;

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

        for (int i = 0; i < InventoryManager.Instance.Slots.Count; i++)
        {
            if (InventoryManager.Instance.TargetInventory.GhostToSlot(i))
            {
                return;
            }
        }
        InventoryManager.Instance.TargetInventory.DropItem(TargetGhost.ClientGhost.Quantity);
    }
}
