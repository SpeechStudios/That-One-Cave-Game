using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/PoisonArrow")]
public class Bow_PoisonArrowData : AbilityData
{
    public float DamageMultiplierPerTick = 0.2f;
    public float TickInterval = 3f;
    public float Duration = 15f;
    public override Ability CreateAbility() => new Bow_PoisonArrow();
    public override void OnHitFunction(HitContext ctx, bool isServer)
    {
        if (!isServer)
        {
            if (ctx.HitEntity.TryGetComponent<IDamageable>(out var clientDamageable))
            {
                //VFX
            }
            return;
        }
        if (ctx.HitEntity.TryGetComponent<IDamageable>(out var damageable))
        {
            DamageOverTimeProperties properties = new()
            {
                Damage = ctx.BaseDamage * DamageMultiplierPerTick,
                Interval = TickInterval,
                Duration = Duration,
                EffectId = "PoisonEffect",
                MaxStacks = 3,
                SourceId = ctx.Source.GetHashCode(),
            };
            damageable.TakeDamage(ctx.TotalDamage, isServer);
            damageable.TakeDamageOverTime(properties, isServer);
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
