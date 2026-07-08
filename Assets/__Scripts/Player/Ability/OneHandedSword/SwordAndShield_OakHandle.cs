using UnityEngine;

public class SwordAndShield_OakHandle : MovementAbility
{
    private const float MaxDashSpeed = 20f;
    private const float DashDuration = 1f;
    private const float DashCooldown = 3f;
    private float DashSpeed;
    private float KnockbackDamage = 36f;

    private const float KnockbackForce = 50f;
    private const float UpKnockbackForce = 0f;


    private LayerMask TargetLayers = LayerMask.GetMask("Player");
    private const float CheckRadius = 0.5f;

    private Vector3 DashDirection;
    private GameObject HitTarget;

    public override float Duration => DashDuration;
    public override float Cooldown => DashCooldown;

    public override void Activate() {}

    public override MovementAbilityResult ExecuteMove(PlayerControllerModule controller, Vector2 moveInput, ref AbilityState state, float dt, float elapsed)
    {
        Vector3 forward = controller.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward;

        DashDirection = forward.normalized;
        DashSpeed = Mathf.Lerp(MaxDashSpeed/2, MaxDashSpeed, elapsed / DashDuration);

        state.MoveDirection = DashDirection;
        state.MoveSpeed = DashSpeed;

        float stepDistance = DashSpeed * dt;
        GameObject hit = CheckCollisionAhead(controller, DashDirection, stepDistance);
        if (hit != null)
        {
            HitTarget = hit;
            return MovementAbilityResult.Completed;
        }
        controller.CC.Move(DashSpeed * dt * DashDirection);
        controller.RefreshGroundedState(DashDirection * DashSpeed);
        return MovementAbilityResult.Continue;
    }

    public override void ServerOnMovementComplete(PlayerControllerModule controller)
    {
        if (HitTarget == null) return;
        if (HitTarget.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(KnockbackDamage, true);
        }
    }
    public override void ClientOnMovementComplete(PlayerControllerModule controller)
    {
        if (HitTarget == null) return;
        if (HitTarget.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(KnockbackDamage, false);
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