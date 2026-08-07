using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/BirchHandle")]
public class Bow_BirchHandleData : AbilityData
{
    public float DamageMultiplierPerTick = 0.2f;
    public float TickInterval = 3f;
    public float Duration = 15f;
    public override Ability CreateAbility() => new Bow_BirchHandle();
}
public class Bow_BirchHandle : Ability
{
    private Bow Bow;
    private Bow_BirchHandleData BirchHandleData;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        BirchHandleData = data as Bow_BirchHandleData;
    }
    public override void ClientActivate(uint tick) 
    {
        Bow.QueueEffect(new PoisonEffect(BirchHandleData.DamageMultiplierPerTick, BirchHandleData.TickInterval, BirchHandleData.Duration), isServer: false);
    }

    public override void ServerActivate(uint tick)
    {
        Bow.QueueEffect(new PoisonEffect(BirchHandleData.DamageMultiplierPerTick, BirchHandleData.TickInterval, BirchHandleData.Duration), isServer : true);
    }
}
public class PoisonEffect : IArrowEffect
{
    private readonly float DamageMultiplierPerTick;
    private readonly float TickInterval;
    private readonly float Duration;

    public PoisonEffect(float damageMultiplierPerTick, float tickInterval, float duration)
    {
        DamageMultiplierPerTick = damageMultiplierPerTick;
        TickInterval = tickInterval;
        Duration = duration;
    }

    public void OnHit(Weapon source, GameObject hitEntity, Vector3 hitPoint, bool isServer, Arrow arrow)
    {
        if (!isServer)
        {
            if (hitEntity.TryGetComponent<IDamageable>(out var clientDamageable))
            {
                //VFX
            }
            return;
        }
        if (hitEntity.TryGetComponent<IDamageable>(out var damageable))
        {
            DamageOverTimeProperties properties = new()
            {
                Damage = source.Stats.GetDamage() * DamageMultiplierPerTick,
                Interval = TickInterval,
                Duration = Duration,
                EffectId = "PoisonEffect",
                MaxStacks = 3,
                SourceId = source.ObjectId,
            };
            damageable.TakeDamage(arrow.Damage, isServer);
            damageable.TakeDamageOverTime(properties, isServer);
        }
    }
}
