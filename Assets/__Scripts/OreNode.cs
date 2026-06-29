using FishNet.Object;
using UnityEngine;

public class OreNode : NetworkBehaviour, IDamageable
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

    public void TakeDamage(float damage, bool isServer)
    {
        if (isServer)
        {
            Health -= damage;
            while (Health <= CurrentThreshold)
            {
                CurrentThreshold -= Threshold;
                SpawnOre(Random.Range(1, 3));
            }
            if (Health <= 0)
            {
                SpawnOre(Random.Range(2, 5));
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
            Debug.Log("Spawning At: " + spawnPos);
            oreInstance.GetComponent<WorldItemGameObject>().Initialize(Ore.ID, 1, null);
            ServerManager.Spawn(oreInstance);
        }
    }
}
