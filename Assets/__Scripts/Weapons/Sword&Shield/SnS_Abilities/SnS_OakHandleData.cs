using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/OakHandle")]
public class SnS_OakHandleData : AbilityData
{
    public float KnockbackForce = 50f;
    public float KnockbackUpForce = 15f;
    public float DelayBeforeCast = 0.1f;
    public float AreaRadius = 2f;
}
public class SnS_OakHandle : Ability
{
    private SwordAndShield SwordAndShield;
    private SnS_OakHandleData OakHandleData;
    public override System.Type DataType => typeof(SnS_OakHandleData);
    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        OakHandleData = data as SnS_OakHandleData;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Loadout.WeaponAnimator.SetTrigger("ShieldBash");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, OakHandleData.DelayBeforeCast, OakHandleData.AreaRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyEffect(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, OakHandleData.DelayBeforeCast, OakHandleData.AreaRadius, isServer: true,
            serverCallback: (obj) => ServerApplyEffect(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyEffect(GameObject target, Vector3 point)
    {

    }
    private void ServerApplyEffect(GameObject target)
    {
        Vector3 velocity = SwordAndShield.ShapeHitDetection.transform.forward * OakHandleData.KnockbackForce;
        velocity += Vector3.up * OakHandleData.KnockbackUpForce;
        if (target.TryGetComponent<IMoveable>(out var movable))
            movable.ApplyKnockback(velocity);
    }
}
