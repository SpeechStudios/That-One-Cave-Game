using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Weapon/Pickaxe")]
public class PickaxeData : WeaponData
{
    public List<PickaxeHandleStats> HandleStats;
    public List<PickaxeHeadStats> HeadStats;

    [System.Serializable]
    public class PickaxeHandleStats
    {
        public MaterialType MaterialType;
        public int Damage;
        public float AttackSpeed;
        public int Resiliance;
    }
    [System.Serializable]
    public class PickaxeHeadStats
    {
        public MaterialType MaterialType;
        public int Damage;
        public int MiningLevel;
        public int Resiliance;
    }
}