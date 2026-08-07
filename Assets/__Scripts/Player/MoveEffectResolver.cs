using UnityEngine;
using FishNet.Object;

public static class MoveEffectResolver
{
    public static float ResolveSpeedMultiplier(in MoveEffectData data, uint currentTick)
    {
        return data.IsSlowed(currentTick) ? data.SlowMultiplier : 1f;
    }

    public static bool ResolveImmobilized(in MoveEffectData data, uint currentTick)
    {
        return data.IsImmobilized(currentTick) || data.IsStaggered(currentTick);
    }

    public static bool ResolveStaggered(in MoveEffectData data, uint currentTick)
    {
        return data.IsStaggered(currentTick);
    }

    public static bool ResolveKnockback(in MoveEffectData data, uint currentTick, out Vector3 velocity)
    {
        if (data.IsKnockedBack(currentTick))
        {
            velocity = data.KnockbackVelocity;
            return true;
        }
        velocity = Vector3.zero;
        return false;
    }

    public static Vector3 DecayKnockback(Vector3 velocity, float decayPerSecond, float dt)
    {
        float speed = velocity.magnitude;
        if (speed <= 0f) return Vector3.zero;

        float newSpeed = Mathf.Max(speed - decayPerSecond * dt, 0f);
        return velocity * (newSpeed / speed);
    }
}
public struct MoveEffectData
{
    public float SlowMultiplier;
    public uint SlowEndTick;

    public uint ImmobilizeEndTick;

    public Vector3 KnockbackVelocity;
    public uint KnockbackEndTick;

    public uint StaggerEndTick;

    public readonly bool IsSlowed(uint currentTick) => currentTick < SlowEndTick;
    public readonly bool IsImmobilized(uint currentTick) => currentTick < ImmobilizeEndTick;
    public readonly bool IsKnockedBack(uint currentTick) => currentTick < KnockbackEndTick;
    public readonly bool IsStaggered(uint currentTick) => currentTick < StaggerEndTick;

    public void SetSlow(float multiplier, uint endTick)
    {
        SlowMultiplier = multiplier;
        SlowEndTick = endTick;
    }

    public void SetImmobilize(uint endTick)
    {
        ImmobilizeEndTick = endTick;
    }

    public void SetKnockback(Vector3 velocity, uint endTick)
    {
        KnockbackVelocity = velocity;
        KnockbackEndTick = endTick;
    }

    public void ClearKnockback()
    {
        KnockbackVelocity = Vector3.zero;
        KnockbackEndTick = 0;
    }

    public void SetStagger(uint endTick)
    {
        StaggerEndTick = endTick;
    }
}
public struct AbilityStateData
{
    public bool OverrideActive;
    public bool IsPrimary;
    public uint StartTick;

    public void Set(bool overrideActive, bool isPrimary, uint startTick)
    {
        OverrideActive = overrideActive;
        IsPrimary = isPrimary;
        StartTick = startTick;
    }

    public void Clear()
    {
        OverrideActive = false;
        IsPrimary = false;
        StartTick = 0;
    }
}
