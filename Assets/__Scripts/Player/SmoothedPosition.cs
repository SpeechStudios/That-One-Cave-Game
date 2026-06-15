using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class SmoothedPosition : NetworkBehaviour
{
    [Header("Sync")]
    [SerializeField] private float sendRate = 0.05f; // 20 updates/sec

    [Header("Interpolation")]
    [SerializeField] private float positionLerpSpeed = 15f;

    private Vector3 lastSentPosition;
    private Vector3 targetPosition;

    private float sendTimer;

    public readonly SyncVar<Vector3> syncedPosition = new();

    public override void OnStartClient()
    {
        base.OnStartClient();

        targetPosition = transform.position;

        syncedPosition.OnChange += OnPositionChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        syncedPosition.OnChange -= OnPositionChanged;
    }

    private void Update()
    {
        if (IsOwner)
        {
            sendTimer += Time.deltaTime;

            if (sendTimer >= sendRate)
            {
                sendTimer = 0f;

                if ((transform.position - lastSentPosition).sqrMagnitude > 0.0001f)
                {
                    lastSentPosition = transform.position;
                    SendTransformServerRpc(transform.position);
                }
            }
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                positionLerpSpeed * Time.deltaTime);
        }
    }

    [ServerRpc]
    private void SendTransformServerRpc(Vector3 position)
    {
        syncedPosition.Value = position;
    }

    private void OnPositionChanged(Vector3 prev, Vector3 next, bool asServer)
    {
        if (IsOwner)
            return;

        targetPosition = next;
    }
}