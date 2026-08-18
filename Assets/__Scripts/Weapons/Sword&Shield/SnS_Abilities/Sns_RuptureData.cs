using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/Rupture")]
public class Sns_RuptureData : AbilityData
{
    public float DelayBeforeCast = 0.1f;
    public float SphereCastRadius = 1.5f;
    public float DamagePercentage = 0.3f;
    public float DamageInterval = 1f;
    public float DamageDuration = 5f;
    public int MaxStacks = 5;
    public override Ability CreateAbility() => new Sns_Rupture();
}
public class Sns_Rupture : Ability
{
    private SwordAndShield SwordAndShield;
    private Sns_RuptureData RuptureData;

    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        RuptureData = data as Sns_RuptureData;
    }

    public override void ClientActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        Weapon.Player.Loadout.WeaponAnimator.SetTrigger("InstantStrike");
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, RuptureData.DelayBeforeCast, RuptureData.SphereCastRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyDamage(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 origin = SwordAndShield.ShapeHitDetection.transform.position;
        SwordAndShield.ShapeHitDetection.TriggerSphere(origin, RuptureData.DelayBeforeCast, RuptureData.SphereCastRadius, isServer: true,
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
        {
            damageable.TakeDamage(SwordAndShield.Player.Stats.GetDamage() * RuptureData.DamagePercentage);
            DamageOverTimeProperties properties = new()
            {
                Damage = SwordAndShield.Player.Stats.GetDamage() * RuptureData.DamagePercentage,
                Interval = RuptureData.DamageInterval,
                Duration = RuptureData.DamageDuration,
                EffectId = RuptureData.AbilityName,
                SourceId = Weapon.ObjectId,
                RefreshAllDotDurations = true,
                MaxStacks = RuptureData.MaxStacks
            };
            damageable.TakeDamageOverTime(properties);
        }
    }
}
