using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance;
    public DragGhostUIManager UI_DragGhost;
    public InventoryUIManager UI_Inventory;
    public CraftingUIManager UI_Crafting;
    public SmeltingUIManager UI_Smelting;
    public StatsUIManager UI_Stats;
    public PlayerOverlayUI UI_PlayerOverlay;
    public GameObject DarkMask;

    public InputActionReference ToggleInventoryButton;
    internal bool InventoryOpen;

    public void Init()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        UI_DragGhost.Init();
        UI_Inventory.Init();
        UI_Crafting.Init();
        UI_Smelting.Init();
        UI_Stats.Init();
    }
    private void OnEnable()
    {
        ToggleInventoryButton.action.performed += OnToggleInventory;
        ToggleInventoryButton.action.Enable();
    }

    private void OnDisable()
    {
        ToggleInventoryButton.action.performed -= OnToggleInventory;
        ToggleInventoryButton.action.Disable();
    }
    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (InventoryOpen)
        {
            InventoryOpen = false;
            DarkMask.SetActive(false);
            UI_Inventory.TargetInventory.Close();
            UI_Inventory.InventoryCanvas.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            InventoryOpen = true;
            DarkMask.SetActive(true);
            UI_Inventory.TargetInventory.Open();
            UI_Inventory.InventoryCanvas.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
