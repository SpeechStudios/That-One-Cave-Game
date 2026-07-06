using UnityEngine;

public class SwordAndShield_Ability_Birch : MovementAbility
{
    private const float DashSpeed = 18f;
    private const float DashDuration = 0.25f;
    private const float DashCooldown = 3f;

    private Vector3 _dashDirection;

    public override float Duration => DashDuration;
    public override float Cooldown => DashCooldown;

    public override void Activate() { }

    public override void Execute(PlayerControllerModule controller, float dt, float elapsed)
    {
        if (elapsed <= 0f)
        {
            Vector3 forward = controller.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward; // degenerate fallback
            _dashDirection = forward.normalized;
        }

        controller.CC.Move(_dashDirection * DashSpeed * dt);
        controller.RefreshGroundedState(_dashDirection * DashSpeed);
    }
}