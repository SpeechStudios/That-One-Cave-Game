using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/AshLimb")]
public class Bow_AshLimbData : AbilityData
{
    public float LaunchUpForce = 12f;
    public float LaunchBackForce = 4f;
    public float CritMultiplier = 2f;
    public float MinAirTimeBeforeGroundCheck = 0.15f;
}

public class Bow_AshLimb : MovementAbility
{
    private Bow_AshLimbData AshLimbData;
    private Bow Bow;
    private PlayerControllerModule Controller;
    public override System.Type DataType => typeof(Bow_AshLimbData);


    public override float Duration => 5f;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        AshLimbData = data as Bow_AshLimbData;
    }

    public override MovementAbilityResult ExecuteMove(PlayerControllerModule controller, Vector2 moveInput, ref AbilityState state, float dt, float elapsed)
    {
        Vector3 forward = controller.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = controller.transform.forward;
        forward.Normalize();

        Vector3 launchVelocity = (Vector3.up * AshLimbData.LaunchUpForce) + (-forward * AshLimbData.LaunchBackForce);

        Bow.QueueCrit(AshLimbData.AbilityName, AshLimbData.CritMultiplier, isServer: true);

        Controller = controller;
        controller.OnLanded -= HandleLanded;
        controller.OnLanded += HandleLanded;

        controller.CC.Move(launchVelocity * dt);
        controller.RefreshGroundedState(launchVelocity);

        return MovementAbilityResult.Completed;
    }
    private void HandleLanded()
    {
        Bow.DequeueCrit(AshLimbData.AbilityName, isServer: true);
        if (Controller != null)
        {
            Controller.OnLanded -= HandleLanded;
        }
        Controller = null;
    }
}