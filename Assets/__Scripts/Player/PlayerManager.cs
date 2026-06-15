using System.Collections.Generic;
using FishNet.Connection;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    private readonly Dictionary<NetworkConnection, PlayerModule> _players = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPlayer(NetworkConnection conn, PlayerModule order)
    {
        _players[conn] = order;
    }

    public void UnregisterPlayer(NetworkConnection conn)
    {
        _players.Remove(conn);
    }

    public PlayerModule GetPlayer(NetworkConnection conn)
    {
        _players.TryGetValue(conn, out var player);
        return player;
    }
}