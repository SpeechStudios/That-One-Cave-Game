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
    public List<WoodNode> WoodNodes = new();
    void Start()
    {
        if (IsServerStarted)
        {
            SpawnNodes();
        }
    }
    private void SpawnNodes()
    {
        int randomNodeCount = Random.Range(MinNodes, MaxNodes + 1);
        for (int i = 0; i < randomNodeCount; i++)
        {
            NetworkObject node = Instantiate(NodePrefab, transform.position + new Vector3(0,0.5f,0) + Vector3.up * i, Quaternion.identity, transform);
            ServerManager.Spawn(node);
            WoodNode woodNode = node.GetComponent<WoodNode>();
            if (i == 0)
                woodNode.IsBottomNode = true;

            woodNode.TreeParent = this;
            woodNode.Health = NodeHealth;
            woodNode.Setup(MinSpawnAmount, MaxSpawnAmount, MinBotSpawnAmount, MaxBotSpawnAmount);
            WoodNodes.Add(woodNode);
        }
    }
    public void BringWoodNodesDown(WoodNode node)
    {
        if (!WoodNodes.Remove(node))
            return;

        for (int i = 0; i < WoodNodes.Count; i++)
        {
            WoodNode wn = WoodNodes[i];

            LeanTween.cancel(wn.gameObject);

            Vector3 targetPosition = transform.position + new Vector3(0, 0.5f, 0) + Vector3.up * i;
            LeanTween.move(wn.gameObject, targetPosition, 0.5f).setEase(LeanTweenType.easeInOutCubic);
        }
    }
}
