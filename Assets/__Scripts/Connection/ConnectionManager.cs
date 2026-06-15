using FishNet.Transporting.Tugboat;
using System.Linq;
using Unity.Multiplayer.PlayMode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public Tugboat Tugboat;
    public void StartServer()
    {
        if (CurrentPlayer.Tags.Contains("HOST"))
        {
            Tugboat.StartConnection(true);
            Tugboat.StartConnection(false);
            Debug.Log("HOST Started");
        }
    }
    public void StartClient()
    {
        if (CurrentPlayer.Tags.Contains("CLIENT"))
        {
            Tugboat.StartConnection(false);
            Debug.Log("CLIENT Started");
        }
    }
}
