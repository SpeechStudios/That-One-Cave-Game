using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode : NetworkBehaviour
{
    public Item Wood;
    public NetworkObject NodePrefab;
    public int ChoppingLevelRequirement = 0;
    public int MinNodes = 4;
    public int MaxNodes = 7;
    public int MinSpawnAmount = 3;
    public int MaxSpawnAmount = 7;
    public int MinBotSpawnAmount = 5;
    public int MaxBotSpawnAmount = 11;
    public int NodeHealth = 20;

    private readonly List<WoodNode> WoodNodes = new();

    public override void OnStartServer()
    {
        base.OnStartServer();
        SpawnNodes();
    }

    private void SpawnNodes()
    {
        int randomNodeCount = Random.Range(MinNodes, MaxNodes + 1);
        for (int i = 0; i < randomNodeCount; i++)
        {
            Vector3 spawnPos = GetSlotPosition(i);
            NetworkObject node = Instantiate(NodePrefab, spawnPos, Quaternion.identity, transform);
            ServerManager.Spawn(node);

            WoodNode woodNode = node.GetComponent<WoodNode>();
            woodNode.ServerInitialize(ChoppingLevelRequirement, i == 0, NodeHealth, MinSpawnAmount, MaxSpawnAmount, MinBotSpawnAmount, MaxBotSpawnAmount);

            WoodNodes.Add(woodNode);

            // Seed buffered position so late joiners snap to the right spot.
            woodNode.RpcSetPosition(spawnPos, animate: false);
        }
    }

    private Vector3 GetSlotPosition(int index)
    {
        return transform.position + new Vector3(0, 0.5f, 0) + Vector3.up * index;
    }

    // Called by WoodNode on the server when it dies.
    public void ServerBringNodesDown(WoodNode deadNode)
    {
        if (!WoodNodes.Remove(deadNode))
            return;

        for (int i = 0; i < WoodNodes.Count; i++)
        {
            WoodNode wn = WoodNodes[i];
            Vector3 targetPosition = GetSlotPosition(i);
            wn.RpcSetPosition(targetPosition, animate: true);
        }
    }

    public int ServerRemainingNodeCount => WoodNodes.Count;
}