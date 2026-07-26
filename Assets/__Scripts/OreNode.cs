using FishNet.Object;
using UnityEngine;

public class OreNode : NetworkBehaviour
{
    public Item Ore;
    public float MiningLevelRequirements = 0;
    public float NodeHealth = 20;
    public int MinOreAmount = 6;
    public int MaxOreAmount = 8;


    public void TakeDamage(float damage, bool isServer)
    {
        if (isServer)
        {
            NodeHealth -= damage;
            Debug.Log($"Taking {damage} Damage, Remaining Health = {NodeHealth}");
            if (NodeHealth <= 0)
            {
                SpawnOre(Random.Range(MinOreAmount, MaxOreAmount + 1));
                GetComponent<NetworkObject>().Despawn();
            }
        }
        else
        {
            //Client Visuals
        }
    }
    [Server]
    private void SpawnOre(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
            spawnPos.z = transform.position.z;

            NetworkObject oreInstance = Instantiate(Ore.WorldItemPrefab, spawnPos, Quaternion.identity);
            oreInstance.GetComponent<WorldItemGameObject>().Initialize(Ore.ID, 1, null);
            ServerManager.Spawn(oreInstance);
        }
    }
}
