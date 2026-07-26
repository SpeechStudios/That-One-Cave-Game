using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/OakHandle")]
public class Bow_OakHandleData : AbilityData
{
    public float ImmobilizeDuration = 3f;
    public float UpwardForce = 2f;
    public float DamageMultiplier = 0.3f;
}

public class Bow_OakHandle : Ability
{
    private Bow Bow;
    private Bow_OakHandleData OakHandleData;
    public override System.Type DataType => typeof(Bow_OakHandleData);
    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        OakHandleData = data as Bow_OakHandleData;
    }
    public override void ClientActivate(uint tick)
    {
        Bow.QueueEffect(new ImmobilizeEffect(OakHandleData.ImmobilizeDuration, OakHandleData.UpwardForce, OakHandleData.DamageMultiplier), isServer: false);
    }
    public override void ServerActivate(uint tick)
    {
        Bow.QueueEffect(new ImmobilizeEffect(OakHandleData.ImmobilizeDuration, OakHandleData.UpwardForce, OakHandleData.DamageMultiplier), isServer: true);
    }
}
public class ImmobilizeEffect : IArrowEffect
{
    private readonly float ImmobilizeDuration;
    private readonly float UpwardForce;
    private readonly float DamageMultiplier;

    public ImmobilizeEffect(float immobilizeDuration, float upwardForce, float damageMultiplier)
    {
        ImmobilizeDuration = immobilizeDuration;
        UpwardForce = upwardForce;
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
            damageable.TakeDamage(source.Damage * DamageMultiplier, isServer);
        }

        if (hitEntity.TryGetComponent<IMoveable>(out var moveable))
        {
            //Vector3 velocity = source.Loadout.BowFirePoint.transform.forward * ImmobilizeDuration;
            //velocity += Vector3.up * UpwardForce;
            moveable.ApplyImmobilize(ImmobilizeDuration);
        } 
    }
}
