using FishNet.Component.Ownership;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Observing;
using FishNet.Serializing;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkObserver))]
[RequireComponent(typeof(ItemVisuals))]
public class WorldItemGameObject : NetworkBehaviour
{
    private float PickUpDelayTime = 1f;
    public float MoveSpeed = 12f;
    public float PickupRadius = 1f;

    private ItemSlotData Data;
    [HideInInspector] public int WorldItemID;
    private NetworkObject TargetPlayer;
    private readonly SyncVar<NetworkObject> SyncTargetPlayer = new();
    private PlayerInventoryModule TargetInventory;
    public bool Moving { get; private set; }
    private bool HasPickupDelay;

    private float PickUpTimer;
    private float NextLockOnTime;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        PickUpTimer = Time.time;

        SyncTargetPlayer.OnChange += OnTargetChanged;
    }
    private void OnEnable()
    {
        var itemVisuals = GetComponent<ItemVisuals>();

        if (!HasPickupDelay)
            itemVisuals.StartJumpToGroundCoroutine();
        else
            itemVisuals.StartMoveToGround();
    }


    private bool CanPickUp => !HasPickupDelay || Time.time >= PickUpTimer + PickUpDelayTime;

    public void Initialize(int itemId, int quantity, int[] materialArray, bool hasPickupDelay = false)
    {
        Data.ID = itemId;
        Data.Materials = materialArray;
        Data.Quantity = quantity;
        HasPickupDelay = hasPickupDelay;
    }
    public override void WritePayload(NetworkConnection connection, Writer writer)
    {
        writer.WriteInt32(Data.ID);
        writer.WriteInt32(WorldItemID);
        writer.WriteInt32(Data.Quantity);
        writer.WriteArray(Data.Materials);
        writer.WriteBoolean(HasPickupDelay);
    }
    public override void ReadPayload(NetworkConnection connection, Reader reader)
    {
        Data.ID = reader.ReadInt32();
        WorldItemID = reader.ReadInt32();
        Data.Quantity = reader.ReadInt32();
        reader.ReadArray(ref Data.Materials);
        HasPickupDelay = reader.ReadBoolean();
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        WorldItemID = ServerWorldItemStash.Instance.GetNextWorldItemID();
        ServerWorldItemStash.Instance.StashItem(Data, transform.position, WorldItemID);
    }

    private void Update()
    {
        if (!Moving || TargetPlayer == null) return;

        Transform target = TargetPlayer.transform;
        transform.position = Vector3.MoveTowards(transform.position, target.position + new Vector3(0, 0.5f, 0), MoveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) <= PickupRadius)
            CollectItem();
    }

    private void OnTriggerStay(Collider other)
    {
        if (Moving || !CanPickUp)
            return;
        if (Time.time < NextLockOnTime)
            return;
        if (!other.CompareTag("Player"))
            return;

        NextLockOnTime = Time.time + 0.5f;

        if (!other.TryGetComponent(out PlayerInventoryModule inventory))
            return;

        TryLockOn(inventory);
    }

    private void TryLockOn(PlayerInventoryModule inventory)
    {
        if (!inventory.CanAcceptItem(Data))
            return;

        TargetInventory = inventory;
        TargetPlayer = inventory.NetworkObject;

        SyncTargetPlayer.Value = TargetPlayer;
        Moving = true;
    }
    private void OnTargetChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
        TargetPlayer = next;
        Moving = next != null;

        if (next != null && next.TryGetComponent(out PlayerInventoryModule inventory))
            TargetInventory = inventory;
    }
    private void CollectItem()
    {
        if (LocalConnection.ClientId != TargetPlayer.OwnerId || !Moving) return;

        Moving = false;
        TargetInventory.PickUpItem(WorldItemID, Data);
        NetworkObject.Despawn();
    }
    private void OnDestroy()
    {
        SyncTargetPlayer.OnChange -= OnTargetChanged;
    }
}
