using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/SteelBlade")]
public class SnS_SteelBladeData : AbilityData
{
    public float ImmobilizeDuration = 2f;
    public float DamagePercentage = 1.72f;
    public float DelayBeforeCast = 0.5f;
    public float AreaRadius = 2f;
    public float AreaLength = 4f;
}
public class SnS_SteelBlade : Ability
{
    private SwordAndShield SwordAndShield;
    private SnS_SteelBladeData SteelBladeData;
    public override System.Type DataType => typeof(SnS_SteelBladeData);

    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        SteelBladeData = data as SnS_SteelBladeData;
    }
    public override void ClientActivate(uint tick)
    {
        Vector3 p1 = SwordAndShield.ShapeHitDetection.transform.position;
        Vector3 p2 = p1 + (SwordAndShield.ShapeHitDetection.transform.forward * SteelBladeData.AreaLength);
        Weapon.Loadout.WeaponAnimator.SetTrigger("Impale");
        SwordAndShield.ShapeHitDetection.TriggerCapsule(p1, p2, SteelBladeData.DelayBeforeCast, SteelBladeData.AreaRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyEffect(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 p1 = SwordAndShield.ShapeHitDetection.transform.position;
        Vector3 p2 = p1 + (SwordAndShield.ShapeHitDetection.transform.forward * SteelBladeData.AreaLength);
        SwordAndShield.ShapeHitDetection.TriggerCapsule(p1, p2, SteelBladeData.DelayBeforeCast, SteelBladeData.AreaRadius, isServer: true,
            serverCallback: (obj) => ServerApplyEffect(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyEffect(GameObject target, Vector3 point)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Damage * SteelBladeData.DamagePercentage, false);
    }
    private void ServerApplyEffect(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Damage * SteelBladeData.DamagePercentage, true);
        if (target.TryGetComponent<IMoveable>(out var movable))
            movable.ApplyImmobilize(SteelBladeData.ImmobilizeDuration);
    }
}
