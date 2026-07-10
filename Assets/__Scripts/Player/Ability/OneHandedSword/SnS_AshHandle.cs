using UnityEngine;

public class SnS_AshHandle : MovementAbility
{
    private const float MaxChargeSpeed = 20f;
    private const float ChargeDuration = 1f;
    private const float ChargeDamagePercentage = 136f;

    private LayerMask TargetLayers = LayerMask.GetMask("Player");
    private const float CheckRadius = 0.5f;

    private float ChargeSpeed;
    private Vector3 ChargeDirection;
    private GameObject HitTarget;

    public override float Duration => ChargeDuration;
    public override float Cooldown => 8f;

    public override MovementAbilityResult ExecuteMove(PlayerControllerModule controller, Vector2 moveInput, ref AbilityState state, float dt, float elapsed)
    {
        Vector3 forward = controller.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward;

        ChargeDirection = forward.normalized;
        ChargeSpeed = Mathf.Lerp(MaxChargeSpeed / 2, MaxChargeSpeed, elapsed / ChargeDuration);

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
        if (HitTarget == null) return;
        if (HitTarget.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Weapon.Damage * ChargeDamagePercentage, true);
        }
    }
    public override void ClientOnMovementComplete(PlayerControllerModule controller)
    {
        if (HitTarget == null) return;
        if (HitTarget.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(Weapon.Damage * ChargeDamagePercentage, false);
        }
    }
    private GameObject CheckCollisionAhead(PlayerControllerModule controller, Vector3 direction, float distance)
    {
        CharacterController cc = controller.CC;
        Vector3 origin = controller.transform.position + (controller.transform.forward * 0.2f);

        if (Physics.SphereCast(origin, CheckRadius, direction, out RaycastHit hit, distance, TargetLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject;
        }
        return null;
    }
}
