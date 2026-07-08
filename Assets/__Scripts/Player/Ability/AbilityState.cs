using System;
using UnityEngine;

[Serializable]
public struct AbilityState
{
    public bool Active;
    public bool IsPrimary;
    public uint StartTick;

    public Vector3 MoveDirection;
    public float MoveSpeed;

    public AbilityState(bool active, bool isPrimary, uint startTick)
    {
        Active = active;
        IsPrimary = isPrimary;
        StartTick = startTick;
        MoveDirection = Vector3.zero;
        MoveSpeed = 0f;
    }
}
