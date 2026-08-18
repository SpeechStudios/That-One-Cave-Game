using FishNet.Object;
using FishNet.Managing.Timing;
using FishNet.Component.ColliderRollback;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShapeHitDetection : NetworkBehaviour
{
    public enum ShapeType { Sphere, Capsule }

    private class PendingCast
    {
        public int Id;
        public ShapeType Shape;
        public Vector3 P1;
        public Vector3 P2;
        public float Radius;
        public int DelayTicks;
        public int TicksElapsed;
        public PreciseTick StartTick;
        public readonly HashSet<GameObject> HitObjects = new();
        public Action<GameObject, Vector3> ClientCallback;
        public Action<GameObject> ServerCallback;
    }

    private int NextCastId;

    private readonly List<PendingCast> ClientPending = new();
    private readonly List<PendingCast> ServerPending = new();

    private readonly List<PendingCast> ClientCompleted = new();
    private readonly List<PendingCast> ServerCompleted = new();

    private readonly Collider[] OverlapBuffer = new Collider[32];

    private PlayerLoadoutModule Loadout;
    private LayerMask HitLayers;
    public void Initalize(PlayerLoadoutModule loadout, LayerMask hitLayers)
    {
        Loadout = loadout;
        HitLayers = hitLayers;
    }

    public int TriggerSphere(Vector3 point, float delaySeconds, float radius, bool isServer,
        Action<GameObject, Vector3> clientCallback = null, Action<GameObject> serverCallback = null)
    {
        return Trigger(ShapeType.Sphere, point, point, delaySeconds, radius, isServer, clientCallback, serverCallback);
    }

    public int TriggerCapsule(Vector3 p1, Vector3 p2, float delaySeconds, float radius, bool isServer,
        Action<GameObject, Vector3> clientCallback = null, Action<GameObject> serverCallback = null)
    {
        return Trigger(ShapeType.Capsule, p1, p2, delaySeconds, radius, isServer, clientCallback, serverCallback);
    }

    private int Trigger(ShapeType shape, Vector3 p1, Vector3 p2, float delaySeconds, float radius, bool isServer,
        Action<GameObject, Vector3> clientCallback, Action<GameObject> serverCallback)
    {
        int delayTicks = Mathf.CeilToInt(delaySeconds / (float)TimeManager.TickDelta);

        var cast = new PendingCast
        {
            Id = NextCastId++,
            Shape = shape,
            P1 = p1,
            P2 = p2,
            Radius = radius,
            DelayTicks = delayTicks,
            TicksElapsed = 0
        };

        if (!isServer)
        {
            if (!IsOwner) return -1;
            cast.ClientCallback = clientCallback;

            if (ClientPending.Count == 0)
                TimeManager.OnTick += Client_OnTick;

            ClientPending.Add(cast);
        }
        else
        {
            cast.ServerCallback = serverCallback;
            cast.StartTick = TimeManager.GetPreciseTick(TickType.LastPacketTick);

            if (ServerPending.Count == 0)
                TimeManager.OnTick += Server_OnTick;

            ServerPending.Add(cast);
        }

        return cast.Id;
    }

    private void Client_OnTick()
    {
        for (int i = 0; i < ClientPending.Count; i++)
        {
            PendingCast cast = ClientPending[i];
            cast.TicksElapsed++;
            if (cast.TicksElapsed < cast.DelayTicks) continue;

            int hitCount = CastOverlap(cast.Shape, cast.P1, cast.P2, cast.Radius, OverlapBuffer);
            for (int h = 0; h < hitCount; h++)
                TryRegisterHit(OverlapBuffer[h], cast.HitObjects, cast.ClientCallback);

            ClientCompleted.Add(cast);
        }

        if (ClientCompleted.Count > 0)
        {
            foreach (var cast in ClientCompleted) ClientPending.Remove(cast);
            ClientCompleted.Clear();
            if (ClientPending.Count == 0) TimeManager.OnTick -= Client_OnTick;
        }
    }
    private void Server_OnTick()
    {
        for (int i = 0; i < ServerPending.Count; i++)
        {
            PendingCast cast = ServerPending[i];
            cast.TicksElapsed++;
            if (cast.TicksElapsed < cast.DelayTicks) continue;

            PreciseTick checkTick = new(cast.StartTick.Tick + (uint)cast.TicksElapsed);

            RollbackManager rollbackManager = NetworkManager.RollbackManager;
            rollbackManager.Rollback(checkTick, RollbackPhysicsType.Physics);

            int hitCount = CastOverlap(cast.Shape, cast.P1, cast.P2, cast.Radius, OverlapBuffer);

            rollbackManager.Return();

            for (int h = 0; h < hitCount; h++)
                TryRegisterHit(OverlapBuffer[h], cast.HitObjects, cast.ServerCallback);

            ServerCompleted.Add(cast);
        }

        if (ServerCompleted.Count > 0)
        {
            foreach (var cast in ServerCompleted) ServerPending.Remove(cast);
            ServerCompleted.Clear();
            if (ServerPending.Count == 0) TimeManager.OnTick -= Server_OnTick;
        }
    }

    private int CastOverlap(ShapeType shape, Vector3 p1, Vector3 p2, float radius,  Collider[] buffer)
    {
        return shape switch
        {
           
            ShapeType.Sphere => Physics.OverlapSphereNonAlloc(p1, radius, buffer, HitLayers),
            ShapeType.Capsule => Physics.OverlapCapsuleNonAlloc(p1, p2, radius, buffer, HitLayers),
            _ => 0
        };
    }

    private void TryRegisterHit(Collider col, HashSet<GameObject> hitObjects, Action<GameObject, Vector3> callback)
    {
        if (col == null || col.transform == transform.root || hitObjects.Contains(col.gameObject)) return;
        hitObjects.Add(col.gameObject);
        if (IsOwner)
        {
            Vector3 point = col.ClosestPoint(transform.position);
            callback?.Invoke(col.gameObject, point);
        }
    }

    private void TryRegisterHit(Collider col, HashSet<GameObject> hitObjects, Action<GameObject> callback)
    {
        if (!IsValidHit(col, hitObjects)) return;
        hitObjects.Add(col.gameObject);
        callback?.Invoke(col.gameObject);
    }
    private bool IsValidHit(Collider col, HashSet<GameObject> hitObjects)
    {
        if (col == null) return false;
        if (col.transform == Loadout.transform.root) return false;
        if (hitObjects.Contains(col.gameObject)) return false;
        Collider hit = Weapon.GetFirstHitLOS(Loadout.FPCam.transform.position, Loadout.FPCam.transform.forward, Loadout.transform.root, 10f, Loadout.LOSLayers);
        if (col == hit)
            return true;
        return true;
    }
}