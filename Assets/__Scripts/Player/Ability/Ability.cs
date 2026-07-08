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

public enum MovementAbilityResult { Continue, Completed }
public abstract class MovementAbility : Ability
{
    public abstract float Duration { get; }
    public abstract MovementAbilityResult ExecuteMove(PlayerControllerModule controller, Vector2 moveInput, ref AbilityState state ,float dt, float elapsed);
    public virtual void ServerOnMovementComplete(PlayerControllerModule controller) { }
    public virtual void ClientOnMovementComplete(PlayerControllerModule controller) { }
    public virtual void OnInterrupted(PlayerControllerModule controller) { }
}