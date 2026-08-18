using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/JumpShot")]
public class Bow_JumpShotData : AbilityData
{
    public float LaunchUpForce = 12f;
    public float LaunchBackForce = 4f;
    public float CritMultiplier = 2f;
    public float MinAirTimeBeforeGroundCheck = 0.15f;
    public override Ability CreateAbility() => new Bow_JumpShot();
}

public class Bow_JumpShot: MovementAbility
{

    private Bow_JumpShotData JumpShotData;
    private Bow Bow;
    private PlayerControllerModule Controller;

    public override float Duration => 5f;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        JumpShotData = data as Bow_JumpShotData;
    }

    public override MovementAbilityResult ExecuteMove(PlayerControllerModule controller, Vector2 moveInput, ref AbilityState state, float dt, float elapsed)
    {
        Vector3 forward = controller.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward;
        forward.Normalize();

        Vector3 launchVelocity = (Vector3.up * JumpShotData.LaunchUpForce) + (-forward * JumpShotData.LaunchBackForce);

        Bow.Player.Stats.TempCrit = 100f;

        Controller = controller;
        controller.OnLanded -= HandleLanded;
        controller.OnLanded += HandleLanded;

        controller.CC.Move(launchVelocity * dt);
        controller.RefreshGroundedState(launchVelocity);

        return MovementAbilityResult.Completed;
    }
    private void HandleLanded()
    {
        Bow.Player.Stats.TempCrit = 0f;
        if (Controller != null)
        {
            Controller.OnLanded -= HandleLanded;
        }
        Controller = null;
    }
}