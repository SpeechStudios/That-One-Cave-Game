using FishNet.Object;
using FishNet.Managing.Timing;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet.Component.ColliderRollback;

public class MeleeHitDetection : NetworkBehaviour
{
    [Min(1f)] public float Length = 1f;
    [Min(0f)] public float Radius;
    public LayerMask HitLayer;
    public bool ShowGizmos;

    private readonly int DetectionIntervalTicks = 2;
    private readonly float DistanceCheckThreshold = 1.5f;
    private PlayerLoadoutModule Loadout;

    // Client state
    public event Action<GameObject, Vector3> ClientOnHit;
    private bool ClientHitDetectionActive;
    private bool ClientHasPrevPositions;
    private int ClientDurationTicks;
    private int ClientTicksElapsed;
    private Vector3 ClientPrevP1;
    private Transform ClientRootPos;
    private AttackData ClientCurrentAttack;
    private Collider[] ClientOverlapBuffer = new Collider[32];
    private readonly HashSet<GameObject> ClientHitObjects = new();

    // Server state
    public event Action<GameObject> ServerOnHit;
    private bool ServerHitDetectionActive;
    private bool ServerHasPrevPositions;
    private int ServerDurationTicks;
    private int ServerTicksElapsed;
    private PreciseTick ServerClientStartTick;
    private Vector3 ServerPrevP1;
    private Transform ServerRootPos;
    private AttackData ServerCurrentAttack;
    private Collider[] ServerOverlapBuffer = new Collider[32];
    private readonly HashSet<GameObject> ServerHitObjects = new();

    private struct GizmoSnapshot
    {
        public Vector3 P1;
        public Vector3 P2;
        public bool IsSphere;
        public float Length;
        public float Radius;
        public Color Color;
        public float Expiry;
    }
    private readonly List<GizmoSnapshot> GizmoSnapshots = new();
    private readonly float GizmoDuration = 2f;

    public void Initalize(PlayerLoadoutModule loadout)
    {
        Loadout = loadout;
    }

    public void EnableHitDetection(AttackData attack, float duration, bool isServer)
    {
        int durationTicks = Mathf.CeilToInt(duration / (float)TimeManager.TickDelta);

        if (!isServer)
        {
            if (!IsOwner || ClientHitDetectionActive) return;
            ClientCurrentAttack = attack;
            ClientRootPos = Loadout.MeleeHitDetectionRoot;
            StartDetection(isServer: false, durationTicks);
        }
        else
        {
            if (ServerHitDetectionActive) return;
            ServerCurrentAttack = attack;
            ServerRootPos = Loadout.MeleeHitDetectionRoot;
            ServerClientStartTick = TimeManager.GetPreciseTick(TickType.LastPacketTick);
            StartDetection(isServer: true, durationTicks);
        }
    }
    private void StartDetection(bool isServer, int durationTicks)
    {
        if (isServer)
        {
            ServerHitDetectionActive = true;
            ServerDurationTicks = durationTicks;
            ServerTicksElapsed = 0;
            ServerHitObjects.Clear();
            ServerHasPrevPositions = false;
            TimeManager.OnTick += Server_OnTick;
        }
        else
        {
            ClientHitDetectionActive = true;
            ClientDurationTicks = durationTicks;
            ClientTicksElapsed = 0;
            ClientHitObjects.Clear();
            ClientHasPrevPositions = false;
            TimeManager.OnTick += Client_OnTick;
        }
    }
    private void Server_OnTick()
    {
        if (ServerTicksElapsed % DetectionIntervalTicks == 0)
        {
            ComputeSwingPositions(ServerCurrentAttack, ServerRootPos, ServerTicksElapsed, ServerDurationTicks,
                out Vector3 p1, out Vector3 p2);

            PreciseTick checkTick = new(ServerClientStartTick.Tick + (uint)ServerTicksElapsed);
            PerformOverlap(p1, p2, isServer: true, checkTick);
        }

        ServerTicksElapsed++;

        if (ServerTicksElapsed > ServerDurationTicks)
            StopDetection(isServer: true);
    }
    private void Client_OnTick()
    {
        if (ClientTicksElapsed % DetectionIntervalTicks == 0)
        {
            ComputeSwingPositions(ClientCurrentAttack, ClientRootPos, ClientTicksElapsed, ClientDurationTicks,
                out Vector3 p1, out Vector3 p2);
            PerformOverlap(p1, p2, isServer: false, default);
        }

        ClientTicksElapsed++;

        if (ClientTicksElapsed > ClientDurationTicks)
            StopDetection(isServer: false);
    }
    private void StopDetection(bool isServer)
    {
        if (isServer)
        {
            ServerHitDetectionActive = false;
            ServerHasPrevPositions = false;
            TimeManager.OnTick -= Server_OnTick;
        }
        else
        {
            ClientHitDetectionActive = false;
            ClientHasPrevPositions = false;
            TimeManager.OnTick -= Client_OnTick;
        }
    }


    private void ComputeSwingPositions(AttackData attack, Transform root, int ticksElapsed, int durationTicks, out Vector3 p1, out Vector3 p2)
    {
        float t = Mathf.Clamp01((float)ticksElapsed / durationTicks);
        float eased = attack.EaseType.Evaluate(t);

        Vector3 swingDir;
        if (eased < 0.5f)
        {
            float firstHalf = eased / 0.5f;
            swingDir = Vector3.Lerp(attack.Start, attack.Mid, firstHalf);
        }
        else
        {
            float secondHalf = (eased - 0.5f) / 0.5f;
            swingDir = Vector3.Lerp(attack.Mid, attack.End, secondHalf);
        }

        Vector3 worldDir = (swingDir.x * root.right) + (swingDir.y * root.up) + (swingDir.z * root.forward);
        p1 = root.position + worldDir;
        p2 = p1 + root.forward * Length;
    }
    private void PerformOverlap(Vector3 p1, Vector3 p2, bool isServer, PreciseTick preciseTick)
    {
        if (isServer)
        {
            RollbackManager rollbackManager = NetworkManager.RollbackManager;
            rollbackManager.Rollback(preciseTick, RollbackPhysicsType.Physics);

            CastOverlap(p1, p2, ServerHitObjects, ServerOverlapBuffer, isServer);
            if (ServerHasPrevPositions)
                DistanceOverlapCheck(p1, ServerPrevP1, ServerHitObjects, ServerOverlapBuffer, isServer);

            rollbackManager.Return();

            ServerPrevP1 = p1;
            ServerHasPrevPositions = true;
        }
        else
        {
            CastOverlap(p1, p2, ClientHitObjects, ClientOverlapBuffer, isServer);
            if (ClientHasPrevPositions)
                DistanceOverlapCheck(p1, ClientPrevP1, ClientHitObjects, ClientOverlapBuffer, isServer);

            ClientPrevP1 = p1;
            ClientHasPrevPositions = true;
        }
    }

    private void CastOverlap(Vector3 p1, Vector3 p2,  HashSet<GameObject> hitObjects, Collider[] overlapBuffer, bool isServer)
    {
        bool isSphere = Mathf.Approximately(Length, 1f);

        int hitCount = isSphere
            ? Physics.OverlapSphereNonAlloc(p1, Radius, overlapBuffer, HitLayer)
            : Physics.OverlapCapsuleNonAlloc(p1, p2, Radius, overlapBuffer, HitLayer);

        RecordGizmoSnapshot(p1, isSphere, Length, Radius, overlapBuffer, hitObjects, hitCount);

        for (int i = 0; i < hitCount; i++)
            TryRegisterHit(overlapBuffer[i], hitObjects, isServer);
    }
    private void DistanceOverlapCheck(Vector3 p1, Vector3 prevP1, HashSet<GameObject> hitObjects, Collider[] overlapBuffer, bool isServer)
    {
        float distance = Vector3.Distance(p1, prevP1);
        float stepSize = Radius * 2f * DistanceCheckThreshold;
        if (distance < stepSize) return;

        int steps = Mathf.Min(Mathf.FloorToInt(distance / stepSize), 5);
        Vector3 forward = transform.root.forward;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / (steps + 1);
            Vector3 sampleP1 = Vector3.Lerp(prevP1, p1, t);
            Vector3 sampleP2 = sampleP1 + forward * Length;
            CastOverlap(sampleP1, sampleP2, hitObjects, overlapBuffer, isServer);
        }
    }

    private void TryRegisterHit(Collider col, HashSet<GameObject> hitObjects, bool isServer)
    {
        if (!IsValidHit(col, hitObjects)) return;

        hitObjects.Add(col.gameObject);

        if (!isServer && IsOwner)
        {
            Vector3 point = col.ClosestPoint(transform.position);
            ClientOnHit?.Invoke(col.gameObject, point);
        }

        if (isServer)
        {
            ServerOnHit?.Invoke(col.gameObject);
        }
    }

    private bool IsValidHit(Collider col, HashSet<GameObject> hitObjects)
    {
        if (col == null) return false;
        if (col.transform == transform.root) return false;
        if (hitObjects.Contains(col.gameObject)) return false;
        return true;
    }

    // Gizmos
    private void RecordGizmoSnapshot(Vector3 p1, bool isSphere, float length, float radius,
        Collider[] overlapBuffer, HashSet<GameObject> hitObjects, int hitCount)
    {
        if (!ShowGizmos) return;

        bool isValidHit = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (IsValidHit(overlapBuffer[i], hitObjects))
                isValidHit = true;
        }

        GizmoSnapshots.Add(new GizmoSnapshot
        {
            P1 = p1,
            P2 = p1 + transform.root.forward * length,
            IsSphere = isSphere,
            Length = length,
            Radius = radius,
            Color = isValidHit ? Color.red : Color.green,
            Expiry = Time.time + GizmoDuration
        });

        UnityEditor.SceneView.RepaintAll();
    }

    private void Update()
    {
        if (!ShowGizmos) return;
        GizmoSnapshots.RemoveAll(s => s.Expiry < Time.time);
        if (GizmoSnapshots.Count > 0)
            UnityEditor.SceneView.RepaintAll();
    }

    private void OnDrawGizmos()
    {
        if (!ShowGizmos) return;

        foreach (GizmoSnapshot snapshot in GizmoSnapshots)
        {
            Gizmos.color = snapshot.Color;
            DrawGizmoShape(snapshot.P1, snapshot.P2, snapshot.IsSphere, snapshot.Radius);
        }
    }

    private void DrawGizmoShape(Vector3 p1, Vector3 p2, bool isSphere, float radius)
    {
        if (isSphere)
        {
            Gizmos.DrawWireSphere(p1, radius);
            return;
        }

        Gizmos.DrawWireSphere(p1, radius);
        Gizmos.DrawWireSphere(p2, radius);

        Vector3 axis = (p2 - p1).normalized;
        if (axis == Vector3.zero) return;

        Vector3 perp1 = Vector3.Cross(axis, Vector3.up).normalized;
        if (perp1 == Vector3.zero) perp1 = Vector3.Cross(axis, Vector3.forward).normalized;
        Vector3 perp2 = Vector3.Cross(axis, perp1).normalized;

        Gizmos.DrawLine(p1 + perp1 * radius, p2 + perp1 * radius);
        Gizmos.DrawLine(p1 - perp1 * radius, p2 - perp1 * radius);
        Gizmos.DrawLine(p1 + perp2 * radius, p2 + perp2 * radius);
        Gizmos.DrawLine(p1 - perp2 * radius, p2 - perp2 * radius);
    }
}