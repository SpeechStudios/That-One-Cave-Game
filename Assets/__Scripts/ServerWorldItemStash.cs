using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class WorldItem
{
    public Vector3 WorldPos;
    public ItemSlotData Data;
}

public class ServerWorldItemStash : NetworkBehaviour
{
    public static ServerWorldItemStash Instance;
    public Dictionary<int, WorldItem> Stash = new();

    public int CurrentItemIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Server]
    public int GetNextWorldItemID()
    {
        return CurrentItemIndex++;
    }

    [Server]
    public WorldItem GetWorldItem(int id)
    {
        if (Stash.TryGetValue(id, out WorldItem worldItem))
            return worldItem;

        return null;
    }

    [Server]
    public void StashItem(ItemSlotData data, Vector3 worldPos, int worldItemID)
    {
        Stash[worldItemID] = new WorldItem {Data = data, WorldPos = worldPos };
    }
    [Server]
    public bool RemoveItem(int id)
    {
        return Stash.Remove(id);
    }
}