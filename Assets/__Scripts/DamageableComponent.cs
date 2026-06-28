using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class DamageableComponent : NetworkBehaviour, IDamageable
{
    public float StartingHealth;

    internal float ServerHealth;
    internal float ServerMaxHealth;

    internal float ClientHealth;
    internal float ClientMaxHealth;

    [Header("UI")]
    public GameObject HealthBar;
    public Transform HealthBarPivot;
    public float HealthBarActiveDuration;

    private float HealthBarActiveTimer;

    private Camera MainCam;
    public void ServerInit()
    {
        ServerMaxHealth = StartingHealth;
        ServerHealth = ServerMaxHealth;
    }
    public void ClientInit()
    {
        ClientMaxHealth = StartingHealth;
        ClientHealth = ClientMaxHealth;
        MainCam = Camera.main;
    }
    public void IncreaseMaxHealth(float Health, bool isServer)
    {
        if (isServer)
        {
            if (ServerHealth == ServerMaxHealth)
                ServerHealth += Health;

            ServerMaxHealth += Health;
        }
        else
        {
            if (ClientHealth == ClientMaxHealth)
                ClientHealth += Health;

            ClientMaxHealth += Health;
            UpdateHealthBar();
        }
    }
    public void TakeDamage(float damage, bool isServer)
    {
        if(isServer)
        {
            ServerHealth -= damage;
            if (ServerHealth <= 0)
            {
                ServerHealth = 0;
            }
        }
        else
        {
            ClientHealth -= damage;
            if(ClientHealth <= 0)
            {
                ClientHealth = 0;
            }
            if (HealthBar == null) return;
            UpdateHealthBar();
            HealthBarActiveTimer = HealthBarActiveDuration;
            HealthBar.SetActive(true);
        }
    }
    public void HealDamage(float value, bool isServer)
    {
        if (isServer)
        {
            ServerHealth += value;
            if (ServerHealth > ServerMaxHealth)
                ServerHealth = ServerMaxHealth;
        }
        else
        {
            ClientHealth += value;
            if (ClientHealth > ClientMaxHealth)
                ClientHealth = ClientMaxHealth;

            UpdateHealthBar();
            HealthBarActiveTimer = HealthBarActiveDuration;
        }
    }
    private void UpdateHealthBar()
    {
        if (HealthBar == null) return;
        float ratio = ClientHealth / ClientMaxHealth;
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
