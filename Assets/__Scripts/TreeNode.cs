using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode : NetworkBehaviour
{
    public Item Wood;
    public NetworkObject NodePrefab;
    public int MinNodes = 3;
    public int MaxNodes = 7;
    public int NodeHealth = 36;
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
            WoodNodes.Add(woodNode);
        }
    }
    public void BringWoodNodesDown(WoodNode node)
    {
        int index = WoodNodes.IndexOf(node);
        WoodNodes.Remove(node);

        for (int i = index; i < WoodNodes.Count; i++)
        {
            Vector3 targetPosition = WoodNodes[i].transform.position + Vector3.down;

            LeanTween.move(WoodNodes[i].gameObject,targetPosition, 0.5f).setEase(LeanTweenType.easeInOutCubic);
        }
    }
}
