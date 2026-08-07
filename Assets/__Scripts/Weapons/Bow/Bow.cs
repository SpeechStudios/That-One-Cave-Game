using FishNet.Object;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



public class Bow : Weapon
{
    public int TestingLimb;
    public int TestingHandle;

    private BowData Data;
    private FirstPersonCamera PlayerCam;
    private float ReloadSpeed;
    internal float ArrowVelocity;

    private float CurrentCharge;
    private bool IsCharging;


    private IArrowEffect ClientPendingEffect;
    private Dictionary<string, float> ClientPendingCrits = new();

    private IArrowEffect ServerPendingEffect;
    private Dictionary<string, float> ServerPendingCrits = new();

    private const float MAX_PASSED_TIME = 0.3f;
    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray)
    {
        base.Initalize(movement, loadout, stats, materialArray);
        Loadout.RebindAnimator(WeaponData.WeaponName);
        PlayerCam = Loadout.FPCam;
        Data = WeaponData as BowData;
    }
    public override void GainStats()
    {
        //Testing
        if (MaterialArray == null)
        {
            var limb = Data.LimbStats[TestingLimb];
            var handle = Data.HandleStats[TestingHandle];

            TotalWeaponDamage = limb.BaseDamage;
            TotalWeaponAttackSpeed = limb.BaseAttackSpeed;

            PrimaryQAbility = limb.PrimaryQAbility.CreateAbility();
            PrimaryQAbility.Initialize(this, limb.PrimaryQAbility);

            ArrowVelocity = handle.ArrowVelocity;
            TotalWeaponDamage += handle.BonusDamage;

            SecondaryEAbility = handle.SecondaryEAbility.CreateAbility();
            SecondaryEAbility.Initialize(this, handle.SecondaryEAbility);

        }
        else
        {
            for (int i = 0; i < MaterialArray.Length; i++)
            {
                MaterialType type = (MaterialType)MaterialArray[i];
                ReloadSpeed = 0.25f;

                if (i == 0) // Limb
                {
                    foreach (var limb in Data.LimbStats)
                    {
                        if (limb.MaterialType == type)
                        {
                            TotalWeaponDamage = limb.BaseDamage;
                            TotalWeaponAttackSpeed = limb.BaseAttackSpeed;

                            PrimaryQAbility = limb.PrimaryQAbility.CreateAbility();
                            PrimaryQAbility.Initialize(this, limb.PrimaryQAbility);
                        }
                    }
                }
                if (i == 1) // Handle
                {
                    foreach (var handle in Data.HandleStats)
                    {
                        if (handle.MaterialType == type)
                        {
                            ArrowVelocity = handle.ArrowVelocity;
                            TotalWeaponDamage += handle.BonusDamage;

                            SecondaryEAbility = handle.SecondaryEAbility.CreateAbility();
                            SecondaryEAbility.Initialize(this, handle.SecondaryEAbility);
                        }
                    }
                }
            }
        }
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void RemoveStats()
    {
        TotalWeaponDamage = 0;
        TotalWeaponAttackSpeed = 0;
        ArrowVelocity = 0;
        SecondaryEAbility.Deinitialize();
        PrimaryQAbility.Deinitialize();
        SecondaryEAbility = null;
        PrimaryQAbility = null;
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void InterruptAttack()
    {
        IsCharging = false;
        CurrentCharge = 0;
        Loadout.WeaponAnimator.SetBool("Aiming", false);
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack || ClientBlockAttacks) 
            return;

        if (!IsCharging)
            IsCharging = true;

        CurrentCharge = Mathf.Clamp01(CurrentCharge + Time.deltaTime / Stats.GetAttackSpeed());

        Loadout.WeaponAnimator.SetBool("Aiming", true);
    }
    public override void ReleaseRequest()
    {
        if (!IsCharging) return;
        IsCharging = false;
        ClientCanAttack = false;

        Loadout.WeaponAnimator.SetBool("Aiming", false);

        float chargedVelocity = ArrowVelocity * CurrentCharge;
        float totalDamage = Stats.GetDamage() * CurrentCharge;

        Vector3 spawnPos = Loadout.FPCam.ClientFirePoint.position;
        Vector3 aimDir = Loadout.FPCam.ClientFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, totalDamage, chargedVelocity, 0f, false);
        Loadout.StartWeaponCooldown(this, ReloadSpeed + 0.05f, isServer: false);

        uint Tick = TimeManager.Tick;
        Server_Attack_RPC(CurrentCharge, Tick);
        CurrentCharge = 0f;
    }
    [ServerRpc]
    public void Server_Attack_RPC(float charge, uint tick)
    {
        float Now = TimeManager.Tick * (float)TimeManager.TickDelta;
        if (Now - LastAttackTime < ReloadSpeed - AttackTolerance)
            return;
        LastAttackTime = Now;

        float PassedTime = (float)TimeManager.TimePassed(tick, allowNegative: false);
        PassedTime = Mathf.Min(MAX_PASSED_TIME / 2f, PassedTime);

        Vector3 spawnPos = Loadout.FPCam.ServerFirePoint.position;
        Vector3 aimDir = Loadout.FPCam.ServerFirePoint.forward;

        charge = Mathf.Clamp01(charge);
        float ChargedVelocity = ArrowVelocity * charge;
        float TotalDamage = Stats.GetDamage() * charge;

        SpawnArrow(spawnPos, aimDir, TotalDamage, ChargedVelocity, PassedTime, true);
        ObserversFireRpc(TotalDamage, ChargedVelocity, tick);

        Loadout.StartWeaponCooldown(this, ReloadSpeed + 0.05f, isServer: true);
    }
    [ObserversRpc(ExcludeOwner = true)]
    public void ObserversFireRpc(float damage, float velocity, uint tick)
    {
        float PassedTime = (float)TimeManager.TimePassed(tick, allowNegative: false);
        PassedTime = Mathf.Min(MAX_PASSED_TIME, PassedTime);

        Vector3 spawnPos = Loadout.TP_BowFirePoint.position;
        Vector3 aimDir = Loadout.TP_BowFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, damage, velocity, PassedTime, false);
    }

    public void QueueEffect(IArrowEffect effect, bool isServer) 
    { 
        if (isServer)
            ServerPendingEffect = effect;
        else 
            ClientPendingEffect = effect;
    }
    public void QueueCrit(string source, float multiplier, bool isServer)
    {
        var Dict = isServer ? ServerPendingCrits : ClientPendingCrits;
        Dict[source] = multiplier;
    }
    public void DequeueCrit(string source, bool isServer)
    {
        var Dict = isServer ? ServerPendingCrits : ClientPendingCrits;
        Dict.Remove(source);
    }
    private float GetPendingCritMultiplier(bool isServer)
    {
        var Dict = isServer ? ServerPendingCrits : ClientPendingCrits;
        float Total = 1f;
        foreach (var Value in Dict.Values) Total *= Value;
        return Total;
    }

    public void SpawnArrow(Vector3 position, Vector3 direction, float damage, float velocity, float passedTime, bool isServer)
    {
        Arrow ArrowInstance = ArrowPoolManager.Instance.Get(position, Quaternion.LookRotation(direction));
        float CritMultiplier = GetPendingCritMultiplier(isServer);
        ArrowInstance.Initialize(this, direction, velocity, passedTime, damage * CritMultiplier, isServer, Loadout.transform.root, ServerPendingEffect);
        if (isServer)
        {
            ServerPendingEffect = null;
            ServerPendingCrits.Clear();
        }
        else
        {
            ClientPendingEffect = null;
            ClientPendingCrits.Clear();
        }
    }
    public void SpawnNormalArrow(Vector3 position, Vector3 direction, float damage, float velocity, float passedTime, bool isServer)
    {
        Arrow ArrowInstance = ArrowPoolManager.Instance.Get(position, Quaternion.LookRotation(direction));
        ArrowInstance.Initialize(this, direction, velocity, passedTime, damage, isServer, Loadout.transform.root, null);
    }

}