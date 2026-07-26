using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/BronzeBlade")]
public class SnS_BronzeBladeData : AbilityData
{
    public float DelayBeforeCast = 0.1f;
    public float SphereCastRadius = 1.5f;
    public float DamagePercentage = 1.5f;
}
public class SnS_BronzeBlade : Ability
{

    private SwordAndShield SwordAndShield;
    private SnS_BronzeBladeData BronzeBladeData;
    public override System.Type DataType => typeof(SnS_BronzeBladeData);
    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        BronzeBladeData = data as SnS_BronzeBladeData;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Loadout.WeaponAnimator.SetTrigger("InstantStrike");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, BronzeBladeData.DelayBeforeCast, BronzeBladeData.SphereCastRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyDamage(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, BronzeBladeData.DelayBeforeCast, BronzeBladeData.SphereCastRadius, isServer: true,
            serverCallback: (obj) => ServerApplyDamage(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyDamage(GameObject target, Vector3 point)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Damage * BronzeBladeData.DamagePercentage, false); 
    }
    private void ServerApplyDamage(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Damage * BronzeBladeData.DamagePercentage, true);
    }
}