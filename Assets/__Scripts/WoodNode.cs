using FishNet.Object;
using UnityEngine;

public class WoodNode : NetworkBehaviour
{
    internal bool IsBottomNode;
    internal TreeNode TreeParent;
    internal float Health;

    [ServerRpc(RequireOwnership = false)]
    public void Server_DamageOre_RPC(float damage)
    {
        TakeDamage(damage);
    }

    [Server]
    private void TakeDamage(float damage)
    {
        if (IsBottomNode && TreeParent.WoodNodes.Count > 1) return;

        Health -= damage;
        if (Health <= 0)
        {
            TreeParent.BringWoodNodesDown(this);

            if(IsBottomNode)
                SpawnWood(Random.Range(8, 10));
            else
                SpawnWood(Random.Range(4, 6));

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

            NetworkObject oreInstance = Instantiate(TreeParent.Wood.WorldItemPrefab, spawnPos, Quaternion.identity);
            oreInstance.GetComponent<WorldItemGameObject>().Initialize(TreeParent.Wood.ID, 1);
            ServerManager.Spawn(oreInstance);


        }
    }
}
