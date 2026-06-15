using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public ConnectionManager ConnectionManager;
    public Registry Registry;
    public DragGhostManager DragGhostManager;
    public InventoryManager InventoryManager;
    public CraftingManager CraftingManager;
    public SmeltingManager SmeltingManager;
    private void Awake()
    {
        Registry.Init();
        DragGhostManager.Init();
        InventoryManager.Init();
        CraftingManager.Init();
        SmeltingManager.Init();
    }
    void Start()
    {
        ConnectionManager.StartServer();
        ConnectionManager.StartClient();
        Application.targetFrameRate = 60;
    }
}
