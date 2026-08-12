using UnityEngine;
public class HitContext
{
    public Vector3 HitPoint;
    public Transform HitEntity;
    public Transform Source;
    public float BaseDamage;
    public float TotalDamage;
}
public enum CooldownType { Instant, TogglePending };
public abstract class AbilityData : ScriptableObject
{
    public int ID;
    public CooldownType CooldownType;
    public string AbilityName;
    public Sprite AbilityIcon;
    public float Cooldown = 4f;

    public bool InterruptAutoAttack;
    public bool BlockAttacks;
    public bool BlockOtherAbilities;
    public bool BlockSwapping;
    public abstract Ability CreateAbility();
    public virtual void SpawnInitalizeClientVisuals() { }
    public virtual void OnHitFunction(HitContext ctx, bool isServer) {  }
}
public abstract class Ability
{
    protected Weapon Weapon { get; private set; }
    internal AbilityData Data { get; private set; }

    public virtual void Initialize(Weapon weapon, AbilityData data) { Weapon = weapon; Data = data; }
    public virtual void Deinitialize() { }
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