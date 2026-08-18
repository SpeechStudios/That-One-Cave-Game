using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/PoisonArrow")]
public class Bow_PoisonArrowData : AbilityData
{
    public float DamageMultiplierPerTick = 0.2f;
    public float TickInterval = 3f;
    public float Duration = 15f;
    public override Ability CreateAbility() => new Bow_PoisonArrow();
    public override void OnClientHit(Vector3 HitPoint, Transform HitEntity)
    {
        if (HitEntity.TryGetComponent<IDamageable>(out var clientDamageable))
        {
            //VFX
        }
    }
    public override void OnServerHit(HitContext ctx, ref float damage)
    {
        if (ctx.HitEntity.TryGetComponent<IDamageable>(out var damageable))
        {
            DamageOverTimeProperties properties = new()
            {
                Damage = damage * DamageMultiplierPerTick,
                Interval = TickInterval,
                Duration = Duration,
                EffectId = "PoisonEffect",
                MaxStacks = 3,
                SourceId = ctx.Source.GetHashCode(),
            };
            damageable.TakeDamage(damage);
            damageable.TakeDamageOverTime(properties);
        }
    }
}
public class Bow_PoisonArrow: Ability
{
    private Bow Bow;
    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
    }
    public override void ClientActivate(uint tick) 
    {
        Bow.QueueEffect(this, Data.ID, isServer: false);
    }

    public override void ServerActivate(uint tick)
    {
        Bow.QueueEffect(this, Data.ID, isServer : true);
    }
}
