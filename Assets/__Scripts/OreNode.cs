using FishNet.Object;
using UnityEngine;

public class OreNode : NetworkBehaviour
{
    public Item Ore;
    public float MinHealthRange = 92;
    public float MaxHealthRange = 156;
    public float Threshold = 18f;


    private float CurrentThreshold;
    private float Health;
    private float MaxHealth;
    void Start()
    {
        if(IsServerStarted)
        {
            MaxHealth = Random.Range(MinHealthRange, MaxHealthRange);
            Health = MaxHealth;
            CurrentThreshold = MaxHealth - Threshold;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void Server_DamageOre_RPC(float damage)
    {
        TakeDamage(damage);
    }

    [Server]
    private void TakeDamage(float damage)
    {
        Health -= damage;
        while (Health <= CurrentThreshold)
        {
            CurrentThreshold -= Threshold;
            SpawnOre(Random.Range(1,4));
        }
        if (Health <= 0)
        {
            SpawnOre(Random.Range(3, 6));
            GetComponent<NetworkObject>().Despawn();
        }
    }
    [Server]
    private void SpawnOre(int count)
    {
        Debug.Log($"[SERVER] Spawned Objects: {count}");
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
            spawnPos.z = transform.position.z;

            NetworkObject oreInstance = Instantiate(Ore.WorldItemPrefab, spawnPos, Quaternion.identity);
            oreInstance.GetComponent<WorldItemGameObject>().Initialize(Ore.ID, 1);
            ServerManager.Spawn(oreInstance);

        }
    }
}
