using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/ShieldBash")]
public class Sns_ShieldBashData : AbilityData
{
    public float KnockbackForce = 50f;
    public float KnockbackUpForce = 15f;
    public float DelayBeforeCast = 0.1f;
    public float AreaRadius = 2f;
    public override Ability CreateAbility() => new Sns_ShieldBash();
}
public class Sns_ShieldBash : Ability
{
    private SwordAndShield SwordAndShield;
    private Sns_ShieldBashData ShieldBashData;
    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        ShieldBashData = data as Sns_ShieldBashData;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Player.Loadout.WeaponAnimator.SetTrigger("ShieldBash");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, ShieldBashData.DelayBeforeCast, ShieldBashData.AreaRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyEffect(obj, point));
    }

    public override (ObserverType, byte[]) ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, ShieldBashData.DelayBeforeCast, ShieldBashData.AreaRadius, isServer: true,
            serverCallback: (obj) => ServerApplyEffect(obj));
        return default;
    }


    private void ClientApplyEffect(GameObject target, Vector3 point)
    {

    }
    private void ServerApplyEffect(GameObject target)
    {
        Vector3 velocity = SwordAndShield.ShapeHitDetection.transform.forward * ShieldBashData.KnockbackForce;
        velocity += Vector3.up * ShieldBashData.KnockbackUpForce;
        if (target.TryGetComponent<IMoveable>(out var movable))
            movable.ApplyKnockback(velocity);
    }
}
