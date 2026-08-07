using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public ConnectionManager ConnectionManager;
    public Registry Registry;
    public PlayerUIManager PlayerUIManager;
    private void Awake()
    {
        Registry.Init();
        PlayerUIManager.Init();
    }
    void Start()
    {
        ConnectionManager.StartServer();
        ConnectionManager.StartClient();
        Application.targetFrameRate = 60;
    }
}
