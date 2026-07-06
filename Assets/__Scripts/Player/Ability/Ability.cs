using UnityEngine;

public abstract class Ability
{
    public abstract float Cooldown { get; }

    public bool IsOnCooldown(uint lastUsedTick, uint currentTick, float tickDelta)
    {
        if (lastUsedTick == 0) return false;
        float elapsed = (currentTick - lastUsedTick) * tickDelta;
        return elapsed < Cooldown;
    }

    public abstract void Activate();
}


public abstract class MovementAbility : Ability
{
    public abstract float Duration { get; }
    public abstract void Execute(PlayerControllerModule controller, float dt, float elapsed);
}