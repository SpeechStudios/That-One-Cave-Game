using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Weapon/SwordnShield")]
public class SnSData : WeaponData
{
    public List<SnSHandleStats> HandleStats;
    public List<SnSBladeStats> BladeStats;

    [System.Serializable]
    public class SnSHandleStats
    {
        public MaterialType MaterialType;
        public float AttackSpeed;
        public int Damage;
        public int Resiliance;
        public AbilityData PrimaryQAbility;

    }
    [System.Serializable]
    public class SnSBladeStats
    {
        public MaterialType MaterialType;
        public int Damage;
        public int Resiliance;
        public AbilityData SecondaryEAbility;
    }

}