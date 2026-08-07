using FishNet.Object;
using UnityEngine;

public class WoodNode : NetworkBehaviour
{
    private bool IsBottomNode;
    private float Health;
    private int ChoppingLevelRequirement;
    private int MinAmount, MaxAmount, MinBotAmount, MaxBotAmount;

    private TreeNode _treeParent;
    private TreeNode TreeParent => _treeParent ??= GetComponentInParent<TreeNode>();

    public void ServerInitialize(int choppingLevelRequirment, bool isBottomNode, float health, int minAmount, int maxAmount, int minBotAmount, int maxBotAmount)
    {
        ChoppingLevelRequirement = choppingLevelRequirment;
        IsBottomNode = isBottomNode;
        Health = health;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        MinBotAmount = minBotAmount;
        MaxBotAmount = maxBotAmount;
    }

    public void TakeDamage(float damage, int level, bool isServer)
    {
        if (!isServer)
        {
            //VFX
            return;
        }

        if (IsBottomNode && TreeParent.ServerRemainingNodeCount > 1)
            return;

        if (level < ChoppingLevelRequirement)
            return;

        Health -= damage;
        if (Health <= 0)
        {
            TreeParent.ServerBringNodesDown(this);

            int amount = IsBottomNode
                ? Random.Range(MinBotAmount, MaxBotAmount)
                : Random.Range(MinAmount, MaxAmount);

            SpawnWood(amount);
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [Server]
    private void SpawnWood(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
            spawnPos.z = transform.position.z;

            NetworkObject worldItem = Instantiate(ServerWorldItemStash.Instance.WorldItemPrefab, spawnPos, Quaternion.identity);
            worldItem.GetComponent<WorldItemGameObject>().Initialize(TreeParent.Wood.ID, 1, null);
            ServerManager.Spawn(worldItem);
        }
    }

    [ObserversRpc(BufferLast = true)]
    public void RpcSetPosition(Vector3 targetPosition, bool animate)
    {
        LeanTween.cancel(gameObject);
        if (animate)
            LeanTween.move(gameObject, targetPosition, 0.5f).setEase(LeanTweenType.easeInOutCubic);
        else
            transform.position = targetPosition;
    }
}