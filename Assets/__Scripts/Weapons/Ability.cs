using UnityEngine;
public class AbilityData : ScriptableObject
{
    public string AbilityName;
    public Sprite AbilityIcon;
    public float Cooldown = 4f;
}
public abstract class Ability
{
    protected Weapon Weapon { get; private set; }
    protected AbilityData Data { get; private set; }
    public abstract System.Type DataType { get; }
    public virtual void Initialize(Weapon weapon, AbilityData data) { Weapon = weapon; Data = data; }
    public bool IsOnCooldown(uint lastUsedTick, uint currentTick, float tickDelta)
    {
        if (lastUsedTick == 0) return false;
        float elapsed = (currentTick - lastUsedTick) * tickDelta;
        return elapsed < Data.Cooldown;
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