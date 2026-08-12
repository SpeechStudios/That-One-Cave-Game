using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/Immobilize")]
public class Bow_ImmobilizeData: AbilityData
{
    public float ImmobilizeDuration = 3f;
    public float DamageMultiplier = 0.3f;
    public override Ability CreateAbility() => new Bow_Immobilize();
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
            damageable.TakeDamage(ctx.TotalDamage * DamageMultiplier, isServer);
        }

        if (ctx.HitEntity.TryGetComponent<IMoveable>(out var moveable))
        {
            //Vector3 velocity = source.Loadout.BowFirePoint.transform.forward * ImmobilizeDuration;
            //velocity += Vector3.up * UpwardForce;
            moveable.ApplyImmobilize(ImmobilizeDuration);
        }
    }
}

public class Bow_Immobilize: Ability
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
        Bow.QueueEffect(this, Data.ID, isServer: true);
    }
}