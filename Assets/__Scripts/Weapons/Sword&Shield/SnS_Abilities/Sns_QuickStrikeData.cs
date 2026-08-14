using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/QuickStrike")]
public class Sns_QuickStrikeData : AbilityData
{
    public float DelayBeforeCast = 0.1f;
    public float SphereCastRadius = 1.5f;
    public float DamagePercentage = 1.5f;
    public override Ability CreateAbility() => new Sns_QuickStrike();
}
public class Sns_QuickStrike : Ability
{
    private SwordAndShield SwordAndShield;
    private Sns_QuickStrikeData QuickStrikeData;
    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        QuickStrikeData = data as Sns_QuickStrikeData;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Loadout.WeaponAnimator.SetTrigger("InstantStrike");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, QuickStrikeData.DelayBeforeCast, QuickStrikeData.SphereCastRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyDamage(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, QuickStrikeData.DelayBeforeCast, QuickStrikeData.SphereCastRadius, isServer: true,
            serverCallback: (obj) => ServerApplyDamage(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyDamage(GameObject target, Vector3 point)
    {
        //VFX
    }
    private void ServerApplyDamage(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Stats.GetDamage() * QuickStrikeData.DamagePercentage);
    }
}