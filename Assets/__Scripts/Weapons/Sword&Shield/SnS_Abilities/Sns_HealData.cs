using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/Heal")]
public class Sns_HealData : AbilityData
{
    public float HealPercentage = 3f;
    public override Ability CreateAbility() => new SnS_Heal();
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
        Weapon.Loadout.GetComponent<IDamageable>().Heal(SwordAndShield.Stats.GetDamage() * HealData.HealPercentage, isServer:false);
    }

    public override void ServerActivate(uint tick)
    {
        Weapon.Loadout.GetComponent<IDamageable>().Heal(SwordAndShield.Stats.GetDamage() * HealData.HealPercentage, isServer: true);
    }

    public override void ObserverActivate(uint tick) { }
}