using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/MithrilBlade")]
public class SnS_MithrilBladeData : AbilityData
{
    public float DelayBeforeCast = 0.1f;
    public float SphereCastRadius = 1.5f;
    public float DamagePercentage = 0.3f;
    public float DamageInterval = 1f;
    public float DamageDuration = 5f;
    public int MaxStacks = 5;
}
public class SnS_MithrilBlade : Ability
{
    private SwordAndShield SwordAndShield;
    private SnS_MithrilBladeData MithrilBladeData;
    public override System.Type DataType => typeof(SnS_MithrilBladeData);

    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        MithrilBladeData = data as SnS_MithrilBladeData;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Loadout.WeaponAnimator.SetTrigger("InstantStrike");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, MithrilBladeData.DelayBeforeCast, MithrilBladeData.SphereCastRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyDamage(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, MithrilBladeData.DelayBeforeCast, MithrilBladeData.SphereCastRadius, isServer: true,
            serverCallback: (obj) => ServerApplyDamage(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyDamage(GameObject target, Vector3 point)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(SwordAndShield.Damage * MithrilBladeData.DamagePercentage, false);
            DamageOverTimeProperties properties = new()
            {
                Damage = SwordAndShield.Damage * MithrilBladeData.DamagePercentage,
                Interval = MithrilBladeData.DamageInterval,
                Duration = MithrilBladeData.DamageDuration,
                EffectId = MithrilBladeData.AbilityName,
                SourceId = Weapon.ObjectId,
                RefreshAllDotDurations = true,
                MaxStacks = MithrilBladeData.MaxStacks
            };
            damageable.TakeDamageOverTime(properties, isServer: false);
        }
    }
    private void ServerApplyDamage(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(SwordAndShield.Damage * MithrilBladeData.DamagePercentage, true);
            DamageOverTimeProperties properties = new()
            {
                Damage = SwordAndShield.Damage * MithrilBladeData.DamagePercentage,
                Interval = MithrilBladeData.DamageInterval,
                Duration = MithrilBladeData.DamageDuration,
                EffectId = MithrilBladeData.AbilityName,
                SourceId = Weapon.ObjectId,
                RefreshAllDotDurations = true,
                MaxStacks = MithrilBladeData.MaxStacks
            };
            damageable.TakeDamageOverTime(properties, isServer: true);
        }

    }
}
