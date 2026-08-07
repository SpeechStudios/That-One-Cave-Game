using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Weapon/Bow")]
public class BowData : WeaponData
{
    public List<BowLimbStats> LimbStats;
    public List<BowHandleStats> HandleStats;

    [System.Serializable]
    public class BowLimbStats
    {
        public MaterialType MaterialType;
        public float BaseAttackSpeed;
        public int BaseDamage;
        public AbilityData PrimaryQAbility;
    }
    [System.Serializable]
    public class BowHandleStats
    {
        public MaterialType MaterialType;
        public int BonusDamage;
        public float ArrowVelocity;
        public AbilityData SecondaryEAbility;
    }
}
