using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct GearBonuses
{
    public float HPPercent;
    public float ARMPercent;
    public int DMGBonus;
    public float DMGPercent;
    public float MSPercent;
    public float ATSPercent;

    public static GearBonuses operator +(GearBonuses a, GearBonuses b)
    {
        return new GearBonuses
        {
            HPPercent = a.HPPercent + b.HPPercent,
            ARMPercent = a.ARMPercent + b.ARMPercent,
            DMGBonus = a.DMGBonus + b.DMGBonus,
            DMGPercent = a.DMGPercent + b.DMGPercent,
            MSPercent = a.MSPercent + b.MSPercent,
            ATSPercent = a.ATSPercent + b.ATSPercent,
        };
    }
}

public class PlayerStatsModule : NetworkBehaviour, IDamageable
{
    public float BaseHealth;
    public float BaseMoveSpeed;

    [Header("Third Person Health Bar")]
    public GameObject HealthBar;
    public Transform HealthBarPivot;
    public float HealthBarActiveDuration;
    private float HealthBarActiveTimer;
    private Camera MainCam;

    private readonly HealthState ServerState = new();
    private readonly SyncVar<float> SyncedHealth = new();
    private readonly SyncVar<float> SyncedMaxHealth = new();
    private readonly SyncVar<int> SyncedDamage = new(new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.OwnerOnly));
    private readonly SyncVar<float> SyncedAttackSpeed = new(new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.OwnerOnly));
    private readonly SyncVar<int> SyncedArmor = new(new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.OwnerOnly));


    private readonly SyncVar<GearBonuses> SyncedGearBonuses = new(new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.OwnerOnly));
    private readonly Dictionary<string, GearBonuses> GearSources = new();
    private PlayerUIManager PlayerUI;

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
    public void ServerInit()
    {
        ServerState.Init(BaseHealth);
        PushServerStateToSync();
    }
    public void ClientInit()
    {
        MainCam = Camera.main;
        PlayerUI = PlayerUIManager.Instance;
        PlayerUI.UI_Stats.Bind(this);
        SyncedHealth.OnChange += OnSyncedHealthChanged;
        SyncedMaxHealth.OnChange += OnAnyStatChanged;
        SyncedDamage.OnChange += OnAnyStatChanged;
        SyncedAttackSpeed.OnChange += OnAnyStatChanged;
        SyncedArmor.OnChange += OnAnyStatChanged;
        SyncedGearBonuses.OnChange += OnAnyStatChanged;
    }
    #endregion

    public float GetMaxHealth() => SyncedMaxHealth.Value + (SyncedMaxHealth.Value / 100 * SyncedGearBonuses.Value.HPPercent);
    public float GetHealth() => SyncedHealth.Value;
    public float GetArmor() => SyncedArmor.Value + (SyncedArmor.Value / 100 * SyncedGearBonuses.Value.ARMPercent);
    public float GetDamage()
    {
        float baseDamage = SyncedDamage.Value + SyncedGearBonuses.Value.DMGBonus;
        return baseDamage + (baseDamage / 100 * SyncedGearBonuses.Value.DMGPercent);
    }
    public float GetAttackSpeed() => SyncedAttackSpeed.Value - (SyncedAttackSpeed.Value / 100 * SyncedGearBonuses.Value.ATSPercent);
    public float GetMoveSpeed() => BaseMoveSpeed + (BaseMoveSpeed / 100 * SyncedGearBonuses.Value.MSPercent);

    public void TakeDamage(float damage, bool isServer)
    {
        if (isServer)
        {
            ServerState.TakeDamage(damage);
            PushServerStateToSync();
        }
        else
        {
            ShowTPHealthBar(damage);
        }
    }
    public void TakeDamageOverTime(DamageOverTimeProperties properties, bool isServer)
    {
        if (isServer)
        {
            ServerState.ApplyDot(properties);
        }
    }
    public void Heal(float value, bool isServer)
    {
        if (isServer)
        {
            ServerState.Heal(value);
            PushServerStateToSync();
        }
        else
        {
            ShowTPHealthBar();
        }
    }
    public void IncreaseMaxHealth(float health, bool isServer)
    {
        if (isServer)
        {
            ServerState.IncreaseMaxHealth(health);
            PushServerStateToSync();
        }
    }
    public void GainTempHealth(float armor, float capacity, string source, bool isServer)
    {

    }
    public void SetWeaponContribution(int weaponDamage, float weaponAttackSpeed)
    {
        if (!IsServerInitialized) return;
        SyncedDamage.Value = weaponDamage;
        SyncedAttackSpeed.Value = weaponAttackSpeed;
    }

    public void GainArmor(int value)
    {
        if (!IsServerInitialized) return;
        SyncedArmor.Value += value;
    }

    public void SetGearSource(string sourceId, GearBonuses bonuses)
    {
        if (!IsServerInitialized) return;
        GearSources[sourceId] = bonuses;
        RecalculateGearBonuses();
    }
    public void RemoveGearSource(string sourceId)
    {
        if (!IsServerInitialized) return;
        if (GearSources.Remove(sourceId))
            RecalculateGearBonuses();
    }

    private void RecalculateGearBonuses()
    {
        GearBonuses total = default;
        foreach (var bonuses in GearSources.Values)
            total += bonuses;

        SyncedGearBonuses.Value = total;
    }


    private void TimeManager_OnTick()
    {
        float tickDelta = (float)TimeManager.TickDelta;
        float before = ServerState.Health;
        ServerState.TickDots(tickDelta);
        if (ServerState.Health != before)
            PushServerStateToSync();
    }
    private void PushServerStateToSync()
    {
        SyncedHealth.Value = ServerState.Health;
        SyncedMaxHealth.Value = ServerState.MaxHealth;
    }
    private void OnAnyStatChanged(float prev, float next, bool asServer) => PlayerUI.UI_Stats.UpdateStats();
    private void OnAnyStatChanged(int prev, int next, bool asServer) => PlayerUI.UI_Stats.UpdateStats();
    private void OnAnyStatChanged(GearBonuses prev, GearBonuses next, bool asServer) => PlayerUI.UI_Stats.UpdateStats();
    private void OnSyncedHealthChanged(float prev, float next, bool asServer)
    {
        if (!IsClientInitialized)
            return;

        PlayerUI.UI_PlayerOverlay.UpdateHealth(next, SyncedMaxHealth.Value);

        UpdateHealthBar();
        if (next < prev)
            ShowTPHealthBar();
    }
    private void ShowTPHealthBar(float predictedDamage = 0f)
    {
        if (HealthBar == null) return;
        UpdateHealthBar(predictedDamage);
        HealthBarActiveTimer = HealthBarActiveDuration;
        HealthBar.SetActive(true);
    }
    private void UpdateHealthBar(float predictedDamage = 0f)
    {
        if (HealthBar == null) return;
        float max = SyncedMaxHealth.Value;
        float predictedHealth = Mathf.Max(0f, SyncedHealth.Value - predictedDamage);
        float ratio = max > 0 ? predictedHealth / max : 0f;
        HealthBarPivot.localScale = new Vector3(ratio, HealthBarPivot.localScale.y, HealthBarPivot.localScale.z);
    }
    void LateUpdate()
    {
        if (HealthBar == null) return;
        if (!HealthBar.activeInHierarchy) return;

        float camY = MainCam != null ? MainCam.transform.eulerAngles.y : 0f;
        //HealthBar.transform.rotation = Quaternion.Euler(0f, camY, 0f);

        HealthBarActiveTimer -= Time.deltaTime;
        if (HealthBarActiveTimer <= 0)
            HealthBar.SetActive(false);
    }
}