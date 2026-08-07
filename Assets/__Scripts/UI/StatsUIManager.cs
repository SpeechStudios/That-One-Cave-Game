using TMPro;
using UnityEngine;

public class StatsUIManager : MonoBehaviour
{
    private PlayerStatsModule TargetStats;

    [Header("Stats UI")]
    public TextMeshProUGUI HealthStatText;
    public TextMeshProUGUI ArmorStatText;
    public TextMeshProUGUI DamageStatText;
    public TextMeshProUGUI AttackSpeedStatText;
    public TextMeshProUGUI MoveSpeedStatText;

    public void Bind(PlayerStatsModule stats)
    {
        TargetStats = stats;
        UpdateStats();
    }
    public void Init()
    {
      
    }
    public void UpdateStats()
    {
        if (TargetStats == null) return;

        HealthStatText.text = $"Health: {TargetStats.GetMaxHealth()}";
        ArmorStatText.text = $"Armor:   {TargetStats.GetArmor()}";
        DamageStatText.text = $"Damage: {TargetStats.GetDamage()}";
        AttackSpeedStatText.text = $"AttackSpeed:   {TargetStats.GetAttackSpeed()}";
        MoveSpeedStatText.text = $"MoveSpeed:   {TargetStats.GetMoveSpeed()}";
    }
}
