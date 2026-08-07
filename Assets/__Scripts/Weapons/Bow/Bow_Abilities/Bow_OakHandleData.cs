using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/OakHandle")]
public class Bow_OakHandleData : AbilityData
{
    public float ImmobilizeDuration = 3f;
    public float DamageMultiplier = 0.3f;
    public override Ability CreateAbility() => new Bow_OakHandle();
}

public class Bow_OakHandle : Ability
{
    private Bow Bow;
    private Bow_OakHandleData OakHandleData;
    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        OakHandleData = data as Bow_OakHandleData;
    }
    public override void ClientActivate(uint tick)
    {
        Bow.QueueEffect(new ImmobilizeEffect(OakHandleData.ImmobilizeDuration, OakHandleData.DamageMultiplier), isServer: false);
    }
    public override void ServerActivate(uint tick)
    {
        Bow.QueueEffect(new ImmobilizeEffect(OakHandleData.ImmobilizeDuration, OakHandleData.DamageMultiplier), isServer: true);
    }
}
public class ImmobilizeEffect : IArrowEffect
{
    private readonly float ImmobilizeDuration;
    private readonly float DamageMultiplier;

    public ImmobilizeEffect(float immobilizeDuration, float damageMultiplier)
    {
        ImmobilizeDuration = immobilizeDuration;
        DamageMultiplier = damageMultiplier;
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
            damageable.TakeDamage(source.Stats.GetDamage() * DamageMultiplier, isServer);
        }

        if (hitEntity.TryGetComponent<IMoveable>(out var moveable))
        {
            //Vector3 velocity = source.Loadout.BowFirePoint.transform.forward * ImmobilizeDuration;
            //velocity += Vector3.up * UpwardForce;
            moveable.ApplyImmobilize(ImmobilizeDuration);
        } 
    }
}
