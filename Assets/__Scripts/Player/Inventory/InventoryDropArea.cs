using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropArea : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            if (DragGhostManager.Instance.TargetGhost.ClientGhost.HasItem())
            {
                Debug.Log("Dropping Item");
                InventoryManager.Instance.TargetInventory.DropItem(DragGhostManager.Instance.TargetGhost.ClientGhost.Quantity);
            }
        }
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (DragGhostManager.Instance.TargetGhost.ClientGhost.HasItem())
            {
                Debug.Log("Dropping Item");
                InventoryManager.Instance.TargetInventory.DropItem(1);
            }
        }
    }

}
