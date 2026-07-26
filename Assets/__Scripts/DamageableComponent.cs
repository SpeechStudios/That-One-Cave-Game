using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class DamageableComponent : NetworkBehaviour, IDamageable
{
    public float StartingHealth;

    [Header("UI")]
    public GameObject HealthBar;
    public Transform HealthBarPivot;
    public float HealthBarActiveDuration;
    private float HealthBarActiveTimer;
    private Camera MainCam;

    private readonly HealthState ServerState = new();

    private readonly SyncVar<float> SyncedHealth = new();
    private readonly SyncVar<float> SyncedMaxHealth = new();

    public override void OnStartServer()
    {
        base.OnStartServer();
        base.TimeManager.OnTick += TimeManager_OnTick;
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        base.TimeManager.OnTick -= TimeManager_OnTick;
    }
    private void Awake()
    {
        SyncedHealth.OnChange += OnSyncedHealthChanged;
    }
    public void ServerInit()
    {
        ServerState.Init(StartingHealth);
        PushServerStateToSync();
    }
    public void ClientInit()
    {
        MainCam = Camera.main;
    }
    private void TimeManager_OnTick()
    {
        float tickDelta = (float)base.TimeManager.TickDelta;
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


    public void TakeDamageOverTime(DamageOverTimeProperties properties, bool isServer)
    {
        if (isServer)
        {
            ServerState.ApplyDot(properties);
        }
    }

    public void TakeDamage(float damage, bool isServer)
    {
        if (isServer)
        {
            ServerState.TakeDamage(damage);
            PushServerStateToSync();
        }
        else
        {
            //Client Instant Feedback
            ShowHealthBarOnClientHit();
        }
    }
    public void GainHealth(float value, bool isServer)
    {
        if (isServer)
        {
            ServerState.GainHealth(value);
            PushServerStateToSync();
        }
        else
        {
            ShowHealthBarOnClientHit();
        }
    }

    public void GainArmor(float armor, float capacity, string source, bool isServer)
    {
    }
    public void IncreaseMaxHealth(float health, bool isServer)
    {
        if (isServer)
        {
            ServerState.IncreaseMaxHealth(health);
            PushServerStateToSync();
        }
    }

    private void OnSyncedHealthChanged(float prev, float next, bool asServer)
    {
        if (!base.IsClientInitialized)
            return;

        UpdateHealthBar();
        if (next < prev)
            ShowHealthBarOnClientHit();
    }
    private void ShowHealthBarOnClientHit()
    {
        if (HealthBar == null) return;
        UpdateHealthBar();
        HealthBarActiveTimer = HealthBarActiveDuration;
        HealthBar.SetActive(true);
    }
    private void UpdateHealthBar()
    {
        if (HealthBar == null) return;
        float max = SyncedMaxHealth.Value;
        float ratio = max > 0 ? SyncedHealth.Value / max : 0f;
        HealthBarPivot.localScale = new Vector3(ratio, HealthBarPivot.localScale.y, HealthBarPivot.localScale.z);
    }

    void LateUpdate()
    {
        if (HealthBar == null) return;
        if (!HealthBar.activeInHierarchy) return;

        float camY = MainCam.transform.eulerAngles.y;
        HealthBar.transform.rotation = Quaternion.Euler(0f, camY, 0f);

        HealthBarActiveTimer -= Time.deltaTime;
        if (HealthBarActiveTimer <= 0)
            HealthBar.SetActive(false);
    }
}