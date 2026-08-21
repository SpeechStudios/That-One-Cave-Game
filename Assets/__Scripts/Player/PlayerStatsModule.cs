using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GearStatValues
{
    public int Damage;
    public int CritChance;
    public int CritDamage;
    public int Armor;
    public int Health;
    public int MovementSpeed;
    public int AttackSpeed;
}
public class Stats
{
    public int Damage;
    public int AttackSpeed;
    public int CritChance;
    public int CritDamage;
    public int Armor;
    public int Health;
    public int MovementSpeed;
}
public class StatValues
{
    public float Damage;
    public float AttackSpeed;
    public float CritChance;
    public float CritDamage;
    public float Armor;
    public HealthState Health = new();
}
public class WeaponValues
{
    public float Damage;
    public float AttackSpeed;
}
public struct StatPacket
{
   public float Damage;
   public float CritChance;
   public float CritDamage;
   public float Armor;
   public float Health;
   public float MaxHealth;
   public float AttackSpeed;
}

public class PlayerStatsModule : NetworkBehaviour, IDamageable
{
    public float BaseHealth = 100f;
    public float BaseMoveSpeed = 8.5f;
    public float BaseCritChance = 5f;
    public float BaseCritDamage = 50f;

    [Header("StatValue Multipliers")]
    public float DamageMult = 0.04f;
    public float AttackSpeedMult = 0.005f;
    public float CritChanceMult = 0.05f;
    public float CritDamageMult = 2.5f;
    public float ArmorMult = 1;
    public float HealthMult = 5;
    public float MovementSpeedMult = 0.03f;

    internal Stats ServerStats = new();
    internal StatValues ServerValues = new();
    internal WeaponValues ServerWeaponValues = new();
    internal float TempCrit;

    internal Stats ClientStats = new();
    internal StatValues ClientValues = new();
    internal WeaponValues ClientWeaponValues = new();

    private readonly SyncVar<float> MoveSpeed = new SyncVar<float>();


    private Dictionary<string, GearStatValues> ServerGearSources = new();
    private Dictionary<string, GearStatValues> ClientGearSources = new();

    private PlayerUIManager PlayerUI;
    public ThirdPersonHealthBar TP_HealthBar;
    internal float GetMoveSpeed() => MoveSpeed.Value;
    internal float GetDamage()
    {
        float damage = ServerValues.Damage;
        bool isCrit = ServerValues.CritChance + TempCrit > Random.Range(0, 100);
        TempCrit = 0;
        if (isCrit) damage *= 1 + (ServerValues.CritDamage / 100f);
        return damage;
    }
    #region Initalize
    public override void OnStartServer()
    {
        base.OnStartServer();
        TimeManager.OnTick += TimeManager_OnTick;
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        TimeManager.OnTick -= TimeManager_OnTick;
    }
    private void TimeManager_OnTick()
    {
        float tickDelta = (float)TimeManager.TickDelta;
        ServerValues.Health.TickDots(tickDelta);

    }
    public void ServerInit()
    {
        ServerValues.Health = new();
        ServerValues.Health.Init(BaseHealth);
        MoveSpeed.Value = BaseMoveSpeed;
        ServerValues.CritChance = BaseCritChance;
        ServerValues.CritDamage = BaseCritDamage;
        ServerValues.Health.OnHealthChanged += () =>
        {
            var packet = new StatPacket { Health = ServerValues.Health.Value, MaxHealth = ServerValues.Health.MaxValue };
            TargetSync(Owner, packet);
            ObserverSync(Mathf.Round(ServerValues.Health.Value / ServerValues.Health.MaxValue * 100f) / 100f);
        };
    }
    public void ClientInit()
    {
        PlayerUI = PlayerUIManager.Instance;
        ClientValues.Health = new();
        ClientValues.Health.Init(BaseHealth);
        ClientValues.CritChance = BaseCritChance;
        ClientValues.CritDamage = BaseCritDamage;
        PlayerUI.UI_Stats.Bind(this);
    }
    #endregion

    [Server]
    public void TakeDamage(float damage)
    {
        damage = Mathf.Round(damage);
        ServerValues.Health.TakeDamage(damage);
        var packet = new StatPacket { Health = ServerValues.Health.Value, MaxHealth = ServerValues.Health.MaxValue };
        TargetSync(Owner, packet);
        ObserverSync(Mathf.Round(ServerValues.Health.Value / ServerValues.Health.MaxValue * 100f) / 100f);
    }
    [Server]
    public void TakeDamageOverTime(DamageOverTimeProperties properties)
    {
        ServerValues.Health.ApplyDot(properties);
    }
    [Server]
    public void Heal(float value)
    {
        ServerValues.Health.Heal(value);
        var packet = new StatPacket { Health = ServerValues.Health.Value, MaxHealth = ServerValues.Health.MaxValue };
        TargetSync(Owner, packet);
        ObserverSync(Mathf.Round(ServerValues.Health.Value / ServerValues.Health.MaxValue * 100f) / 100f);
    }

    public void SetWeaponContribution(int weaponDamage, float weaponAttackSpeed, bool isServer)
    {
        var weaponValues = isServer ? ServerWeaponValues : ClientWeaponValues;
        weaponValues.Damage = weaponDamage;
        weaponValues.AttackSpeed = weaponAttackSpeed;
        RecalculateStats(isServer);
    }
    public void AddGear(string sourceId, GearStatValues bonuses, bool isServer)
    {
        var gearSources = isServer ? ServerGearSources : ClientGearSources;
        gearSources[sourceId] = bonuses;
        RecalculateStats(isServer);
    }
    public void RemoveGear(string sourceId, bool isServer)
    {
        var gearSources = isServer ? ServerGearSources : ClientGearSources;
        if (gearSources.Remove(sourceId))
            RecalculateStats(isServer);
    }
    private void RecalculateStats(bool isServer)
    {
        var Stats = isServer ? ServerStats : ClientStats;
        var StatValues = isServer ? ServerValues : ClientValues;
        var WeaponStats = isServer ? ServerWeaponValues : ClientWeaponValues;

        StatValues.Damage = WeaponStats.Damage * (1 + (Stats.Damage * DamageMult));
        StatValues.AttackSpeed = Mathf.Max(WeaponStats.AttackSpeed * (1 - (Stats.AttackSpeed * AttackSpeedMult)), 0.05f);
        StatValues.CritChance = BaseCritChance + (Stats.CritChance * CritChanceMult);
        StatValues.CritDamage = BaseCritDamage + (Stats.CritDamage * CritDamageMult);
        StatValues.Health.MaxValue = BaseHealth + Stats.Health * HealthMult;
        StatValues.Armor =  Stats.Armor * ArmorMult;
        if (isServer)
            MoveSpeed.Value = BaseMoveSpeed + Stats.MovementSpeed * MovementSpeedMult;
    }

    [TargetRpc]
    private void TargetSync(NetworkConnection conn, StatPacket packet)
    {
        ClientValues.Health.Value = packet.Health;
        ClientValues.Health.MaxValue = packet.MaxHealth;
        PlayerUI.UI_PlayerOverlay.UpdateHealth(ClientValues.Health.Value, ClientValues.Health.MaxValue);
    }
    [ObserversRpc] 
    private void ObserverSync(float healthRatio)
    {
        TP_HealthBar.Show(healthRatio);
    }
}