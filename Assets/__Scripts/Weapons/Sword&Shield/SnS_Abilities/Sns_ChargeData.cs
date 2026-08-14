using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/Charge")]
public class Sns_ChargeData : AbilityData
{
    public float MaxChargeSpeed = 20f;
    public float ChargeDamagePercentage = 1.36f;
    public float CheckRadius = 2f;
    public override Ability CreateAbility() => new SnS_Charge();
}
public class SnS_Charge : MovementAbility
{
    private float ChargeSpeed;
    private Vector3 ChargeDirection;
    private GameObject HitTarget;
    private Sns_ChargeData ChargeData;

    public override float Duration => 1f;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        ChargeData = data as Sns_ChargeData;
    }
    public override MovementAbilityResult ExecuteMove(PlayerControllerModule controller, Vector2 moveInput, ref AbilityState state, float dt, float elapsed)
    {
        Vector3 forward = controller.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward;

        ChargeDirection = forward.normalized;
        ChargeSpeed = Mathf.Lerp(ChargeData.MaxChargeSpeed / 2, ChargeData.MaxChargeSpeed, elapsed / Duration);

        state.MoveDirection = ChargeDirection;
        state.MoveSpeed = ChargeSpeed;

        float stepDistance = ChargeSpeed * dt;
        GameObject hit = CheckCollisionAhead(controller, ChargeDirection, stepDistance);
        if (hit != null)
        {
            HitTarget = hit;
            return MovementAbilityResult.Completed;
        }
        controller.CC.Move(ChargeSpeed * dt * ChargeDirection);
        controller.RefreshGroundedState(ChargeDirection * ChargeSpeed);
        return MovementAbilityResult.Continue;
    }

    public override void ServerOnMovementComplete(PlayerControllerModule controller)
    {
        Weapon.AbilityComplete(this,true);
        if (HitTarget == null) return;
        if (HitTarget.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Weapon.Stats.GetDamage() * ChargeData.ChargeDamagePercentage);
        }
    }
    public override void ClientOnMovementComplete(PlayerControllerModule controller)
    {
        Weapon.AbilityComplete(this, false);
    }
    private GameObject CheckCollisionAhead(PlayerControllerModule controller, Vector3 direction, float distance)
    {
        CharacterController cc = controller.CC;
        Vector3 origin = controller.transform.position + (controller.transform.forward * 0.2f);

        if (Physics.SphereCast(origin, ChargeData.CheckRadius, direction, out RaycastHit hit, distance, Weapon.Loadout.HitLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject;
        }
        return null;
    }
}
