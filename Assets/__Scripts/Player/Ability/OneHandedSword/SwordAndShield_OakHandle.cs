using UnityEngine;

public class SwordAndShield_OakHandle : MovementAbility
{
    private const float DashSpeed = 18f;
    private const float DashDuration = 0.25f;
    private const float DashCooldown = 3f;

    private const float KnockbackForce = 50f;
    private const float UpKnockbackForce = 20f;

    private LayerMask TargetLayers = LayerMask.GetMask("Player");
    private const float CheckRadius = 0.5f;

    private Vector3 DashDirection;
    private GameObject HitTarget;

    public override float Duration => DashDuration;
    public override float Cooldown => DashCooldown;

    public override void Activate() {}

    public override MovementAbilityResult Execute(PlayerControllerModule controller, float dt, float elapsed)
    {
        if (elapsed <= 0f)
        {
            Vector3 forward = controller.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward;
            DashDirection = forward.normalized;
            HitTarget = null;
        }

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
    public override void OnMovementComplete(PlayerControllerModule controller)
    {
        if (HitTarget == null) return;
        if (HitTarget.TryGetComponent<IMoveable>(out var moveable))
        {
            Vector3 knockback = (DashDirection * KnockbackForce) + (Vector3.up * UpKnockbackForce);
            moveable.ApplyKnockback(knockback);
        }
    }
    private GameObject CheckCollisionAhead(PlayerControllerModule controller, Vector3 direction, float distance)
    {
        CharacterController cc = controller.CC;
        Vector3 origin = controller.transform.position + Vector3.up * (cc.height * 0.5f);

        if (Physics.SphereCast(origin, CheckRadius, direction, out RaycastHit hit, distance, TargetLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject;
        }
        return null;
    }
}