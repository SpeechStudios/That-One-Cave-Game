using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Weapon/Axe")]
public class AxeData : WeaponData
{
    public List<AxeHandleStats> HandleStats;
    public List<AxeHeadStats> HeadStats;

    [System.Serializable]
    public class AxeHandleStats
    {
        public MaterialType MaterialType;
        public int Damage;
        public float AttackSpeed;
        public int Resiliance;
    }
    [System.Serializable]
    public class AxeHeadStats
    {
        public MaterialType MaterialType;
        public int Damage;
        public int Resiliance;
        public int ChoppingLevel;
    }
}