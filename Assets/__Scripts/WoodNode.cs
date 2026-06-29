using FishNet.Object;
using UnityEngine;

public class WoodNode : NetworkBehaviour, IDamageable
{
    internal bool IsBottomNode;
    internal TreeNode TreeParent;
    internal float Health;


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
                    SpawnWood(Random.Range(3, 6));
                else
                    SpawnWood(Random.Range(1, 3));

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

            NetworkObject oreInstance = Instantiate(TreeParent.Wood.WorldItemPrefab, spawnPos, Quaternion.identity);
            oreInstance.GetComponent<WorldItemGameObject>().Initialize(TreeParent.Wood.ID, 1, null);
            ServerManager.Spawn(oreInstance);


        }
    }
}
