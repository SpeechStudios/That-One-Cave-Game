using UnityEngine;
public abstract class AbilityData : ScriptableObject
{
    public string AbilityName;
    public Sprite AbilityIcon;
    public float Cooldown = 4f;

    public bool InterruptAutoAttack;
    public bool BlockAttacks;
    public bool BlockOtherAbilities;
    public abstract Ability CreateAbility();
}
public abstract class Ability
{
    protected Weapon Weapon { get; private set; }
    internal AbilityData Data { get; private set; }

    public event System.Action AbilityComplete;
    protected void CompleteAbility() => AbilityComplete?.Invoke();
    public virtual void Initialize(Weapon weapon, AbilityData data) { Weapon = weapon; Data = data; AbilityComplete += () => Weapon.OnAbilityComplete(Data); }
    public virtual void Deinitialize() { AbilityComplete -= () => Weapon.OnAbilityComplete(Data); }
    public bool WasOnCooldown;
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