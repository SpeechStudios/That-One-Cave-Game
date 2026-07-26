using FishNet.Object;
using UnityEngine;

public class WoodNode : NetworkBehaviour
{
    internal bool IsBottomNode;
    internal TreeNode TreeParent;
    internal float Health;

    private int MinAmount;
    private int MaxAmount;

    private int MinBotAmmount;
    private int MaxBotAmount;

    public void Setup(int min, int max, int minBot, int maxBot)
    {
        MinAmount = min;
        MaxAmount = max;
        MinBotAmmount = minBot;
        MaxBotAmount = maxBot;
    }


    public void TakeDamage(float damage, bool isServer)
    {
        if (IsBottomNode && TreeParent.WoodNodes.Count > 1) return;
        if (isServer)
        {
            Health -= damage;
            if (Health <= 0)
            {
                TreeParent.BringWoodNodesDown(this);

                if (IsBottomNode)
                    SpawnWood(Random.Range(MinBotAmmount, MaxBotAmount));
                else
                    SpawnWood(Random.Range(MinAmount, MaxAmount));

                GetComponent<NetworkObject>().Despawn();
            }
        }
        else
        {
            //Client Visuals
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
}
