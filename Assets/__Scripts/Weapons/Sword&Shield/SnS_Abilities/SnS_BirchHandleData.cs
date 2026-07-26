using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/SnS/BirchHandle")]
public class SnS_BirchHandleData : AbilityData
{
    public float HealPercentage = 3f;
}
public class SnS_BirchHandle : Ability
{
    private SwordAndShield SwordAndShield;
    private SnS_BirchHandleData BirchHandleData;
    public override System.Type DataType => typeof(SnS_BirchHandleData);

    public override void Initialize(Weapon owner, AbilityData data)
    {
        base.Initialize(owner, data);
        SwordAndShield = owner as SwordAndShield;
        BirchHandleData = data as SnS_BirchHandleData;
    }

    public override void ClientActivate(uint tick)
    {
        Weapon.Loadout.GetComponent<IDamageable>().GainHealth(SwordAndShield.Damage * BirchHandleData.HealPercentage, isServer:false);
    }

    public override void ServerActivate(uint tick)
    {
        Weapon.Loadout.GetComponent<IDamageable>().GainHealth(SwordAndShield.Damage * BirchHandleData.HealPercentage, isServer: true);
    }

    public override void ObserverActivate(uint tick) { }
}