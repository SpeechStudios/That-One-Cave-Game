using UnityEngine;

public abstract class Ability
{
    protected Weapon Weapon { get; private set; }
    public virtual void Initialize(Weapon weapon) { Weapon = weapon; }
    public abstract float Cooldown { get; }
    public bool IsOnCooldown(uint lastUsedTick, uint currentTick, float tickDelta)
    {
        if (lastUsedTick == 0) return false;
        float elapsed = (currentTick - lastUsedTick) * tickDelta;
        return elapsed < Cooldown;
    }
    public virtual void ClientActivate(uint tick) { }
    public virtual void ServerActivate(uint tick) { }
    public virtual void ObserverActivate(uint tick) { }
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