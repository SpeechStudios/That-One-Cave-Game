using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/Heal")]
public class Sns_HealData : AbilityData
{
    public float HealPercentage = 3f;
    public override Ability CreateAbility() => new SnS_Heal();
    public override void OnServerHit(HitContext ctx, ref float damage)
    {
        if (ctx.HitEntity.TryGetComponent<IDamageable>(out var damageable))
        {
            ctx.Source.Stats.Heal(damage);
        }
    }
}
public class SnS_Heal : Ability
{
    private SwordAndShield SwordAndShield;
    private Sns_HealData HealData;

    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        HealData = data as Sns_HealData;
    }

    public override void ClientActivate(uint tick)
    {
        //VFX
        SwordAndShield.QueueBAEffect(Data.ID, false);
    }

    public override void ServerActivate(uint tick)
    {
        Weapon.Player.Loadout.GetComponent<IDamageable>().Heal(SwordAndShield.Player.Stats.GetDamage() * HealData.HealPercentage);
        SwordAndShield.QueueBAEffect(Data.ID, true);
    }

    public override void ObserverActivate(uint tick)
    {
        SwordAndShield.QueueBAEffect(Data.ID, false);
    }
}