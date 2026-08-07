using FishNet.Object;
using UnityEngine;

public class OreNode : NetworkBehaviour
{
    public Item Ore;
    public float MiningLevelRequirement = 0;
    public float NodeHealth = 20;
    public int MinOreAmount = 6;
    public int MaxOreAmount = 8;


    public void TakeDamage(float damage, int level, bool isServer)
    {
        if (!isServer)
        {
            //VFX
            return;
        }

        if (level < MiningLevelRequirement)
            return;

        NodeHealth -= damage;
        if (NodeHealth <= 0)
        {
            SpawnOre(Random.Range(MinOreAmount, MaxOreAmount + 1));
            GetComponent<NetworkObject>().Despawn();
        }
    }
    [Server]
    private void SpawnOre(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
            spawnPos.z = transform.position.z;

            NetworkObject worldItem = Instantiate(ServerWorldItemStash.Instance.WorldItemPrefab, spawnPos, Quaternion.identity);
            worldItem.GetComponent<WorldItemGameObject>().Initialize(Ore.ID, 1, null);
            ServerManager.Spawn(worldItem);
        }
    }
}
