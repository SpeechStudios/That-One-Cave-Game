using UnityEngine;

public class SnS_BirchHandle : Ability
{
    public override float Cooldown => 4f;

    private const float DelayBeforeCast = 0.1f;
    private const float SphereCastRadius = 1.5f;
    private const float DamagePercentage = 1.5f;

    private SwordAndShield SwordAndShield;

    public override void Initialize(Weapon owner)
    {
        base.Initialize(owner);
        SwordAndShield = owner as SwordAndShield;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Loadout.WeaponAnimator.SetTrigger("InstantStrike");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, DelayBeforeCast, SphereCastRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyDamage(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, DelayBeforeCast, SphereCastRadius, isServer: true,
            serverCallback: (obj) => ServerApplyDamage(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyDamage(GameObject target, Vector3 point)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Damage * DamagePercentage, false); 
    }
    private void ServerApplyDamage(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Damage * DamagePercentage, true);
    }
}