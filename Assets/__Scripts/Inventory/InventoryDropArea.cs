using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropArea : MonoBehaviour, IPointerDownHandler
{
    private InventoryUIManager Inventory;
    private DragGhostUIManager DragGhost;
    private void Start()
    {
        Inventory = PlayerUIManager.Instance.UI_Inventory;
        DragGhost = PlayerUIManager.Instance.UI_DragGhost;
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            if (DragGhost.TargetGhost.ClientGhost.HasItem())
            {
                Inventory.TargetInventory.DropItem(DragGhost.TargetGhost.ClientGhost.Quantity);
            }
        }
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (DragGhost.TargetGhost.ClientGhost.HasItem())
            {
                Inventory.TargetInventory.DropItem(1);
            }
        }
    }

}
