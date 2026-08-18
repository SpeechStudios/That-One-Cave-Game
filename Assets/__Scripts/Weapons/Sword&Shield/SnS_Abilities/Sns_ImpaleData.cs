using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/Impale")]
public class Sns_ImpaleData : AbilityData
{
    public float ImmobilizeDuration = 2f;
    public float DamagePercentage = 1.72f;
    public float DelayBeforeCast = 0.5f;
    public float AreaRadius = 2f;
    public float AreaLength = 4f;
    public override Ability CreateAbility() => new Sns_Impale();
}
public class Sns_Impale : Ability
{
    private SwordAndShield SwordAndShield;
    private Sns_ImpaleData ImpaleData;

    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        ImpaleData = data as Sns_ImpaleData;
    }
    public override void ClientActivate(uint tick)
    {
        Vector3 p1 = SwordAndShield.ShapeHitDetection.transform.position;
        Vector3 p2 = p1 + (SwordAndShield.ShapeHitDetection.transform.forward * ImpaleData.AreaLength);
        Weapon.Player.Loadout.WeaponAnimator.SetTrigger("Impale");
        SwordAndShield.ShapeHitDetection.TriggerCapsule(p1, p2, ImpaleData.DelayBeforeCast, ImpaleData.AreaRadius, isServer: false,
            clientCallback: (obj, point) => ClientApplyEffect(obj, point));
    }

    public override void ServerActivate(uint tick)
    {
        Vector3 p1 = SwordAndShield.ShapeHitDetection.transform.position;
        Vector3 p2 = p1 + (SwordAndShield.ShapeHitDetection.transform.forward * ImpaleData.AreaLength);
        SwordAndShield.ShapeHitDetection.TriggerCapsule(p1, p2, ImpaleData.DelayBeforeCast, ImpaleData.AreaRadius, isServer: true,
            serverCallback: (obj) => ServerApplyEffect(obj));
    }

    public override void ObserverActivate(uint tick) { }

    private void ClientApplyEffect(GameObject target, Vector3 point)
    {
        //VFX
    }
    private void ServerApplyEffect(GameObject target)
    {
        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(SwordAndShield.Player.Stats.GetDamage() * ImpaleData.DamagePercentage);
        if (target.TryGetComponent<IMoveable>(out var movable))
            movable.ApplyImmobilize(ImpaleData.ImmobilizeDuration);
    }
}
