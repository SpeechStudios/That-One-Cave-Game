using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class Bow : Weapon
{
    public int TestingLimb;
    public int TestingHandle;

    private BowData Data;
    private float ReloadSpeed;
    internal float ArrowVelocity;

    private float CurrentCharge;
    private bool IsCharging;

    internal List<Ability> ClientPendingAbilties = new();
    internal List<int> ClientPendingEffects = new();
    private Dictionary<string, float> ClientPendingCrits = new();

    internal List<Ability> ServerPendingAbilties = new();
    internal List<int> ServerPendingEffects = new();
    private Dictionary<string, float> ServerPendingCrits = new();

    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray, NetworkRole role)
    {
        Data = WeaponData as BowData;
        base.Initalize(movement, loadout, stats, materialArray, role);
        if (role == NetworkRole.Observer) return;

        Loadout.RebindAnimator(WeaponData.WeaponName);
    }
    public override void InitalizeStats(bool stats, bool abilities)
    {
        if (MaterialArray == null)
        {
            var limb = Data.LimbStats[TestingLimb];
            var handle = Data.HandleStats[TestingHandle];
            if (stats)
            {
                TotalWeaponDamage = limb.BaseDamage;
                TotalWeaponAttackSpeed = limb.BaseAttackSpeed;
                ArrowVelocity = handle.ArrowVelocity;
                TotalWeaponDamage += handle.BonusDamage;
                ReloadSpeed = 0.25f;
            }
            if (abilities)
            {
                PrimaryQAbility = limb.PrimaryQAbility.CreateAbility();
                PrimaryQAbility.Initialize(this, limb.PrimaryQAbility);
                SecondaryEAbility = handle.SecondaryEAbility.CreateAbility();
                SecondaryEAbility.Initialize(this, handle.SecondaryEAbility);
            }
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
                            if (stats)
                            {
                                TotalWeaponDamage = limb.BaseDamage;
                                TotalWeaponAttackSpeed = limb.BaseAttackSpeed;
                            }
                            if (abilities)
                            {
                                PrimaryQAbility = limb.PrimaryQAbility.CreateAbility();
                                PrimaryQAbility.Initialize(this, limb.PrimaryQAbility);
                            }
                        }
                    }
                }
                if (i == 1) // Handle
                {
                    foreach (var handle in Data.HandleStats)
                    {
                        if (handle.MaterialType == type)
                        {
                            if (stats)
                            {
                                ArrowVelocity = handle.ArrowVelocity;
                                TotalWeaponDamage += handle.BonusDamage;
                            }
                            if (abilities)
                            {
                                SecondaryEAbility = handle.SecondaryEAbility.CreateAbility();
                                SecondaryEAbility.Initialize(this, handle.SecondaryEAbility);
                            }
                        }
                    }
                }
            }
        }
    }
    public override void GainStats()
    {
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void RemoveStats()
    {
        Stats.SetWeaponContribution(0, 0);
        SecondaryEAbility.Deinitialize();
        PrimaryQAbility.Deinitialize();
    }
    public override void InterruptAttack()
    {
        IsCharging = false;
        CurrentCharge = 0;
        Loadout.WeaponAnimator.SetBool("Aiming", false);
    }
    public override void AttackRequest()
    {
        if (!ClientCooldown.IsReady || ClientVariables.PrimaryAbility.BlockAttacks || ClientVariables.SecondaryAbility.BlockAttacks)
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

        Loadout.WeaponAnimator.SetBool("Aiming", false);

        float chargedVelocity = ArrowVelocity * CurrentCharge;
        float baseDamage = Stats.GetDamage();
        float totalDamage = baseDamage * CurrentCharge;

        Vector3 spawnPos = Loadout.FPCam.ClientFirePoint.position;
        Vector3 aimDir = Loadout.FPCam.ClientFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, Loadout.transform, chargedVelocity, baseDamage, totalDamage, ClientPendingEffects.ToArray(), 0f, isServer: false);
        ClearEffects(isServer: false);
        ClientCooldown.Start(ReloadSpeed + 0.05f);

        uint Tick = TimeManager.Tick;
        Server_Attack_RPC(CurrentCharge, Tick);
        CurrentCharge = 0f;
    }
    [ServerRpc]
    public void Server_Attack_RPC(float charge, uint tick)
    {
        if (!ServerCooldown.IsReady || ServerVariables.PrimaryAbility.BlockAttacks || ServerVariables.SecondaryAbility.BlockAttacks)
            return;

        uint serverTick = TimeManager.LocalTick;
        uint clampedTick = tick > serverTick ? serverTick : tick;
        if (serverTick - clampedTick > MAX_TICK_DELAY)
            return;
        float passedTime = (float)TimeManager.TimePassed(clampedTick, allowNegative: false);

        Vector3 spawnPos = Loadout.FPCam.ServerFirePoint.position;
        Vector3 aimDir = Loadout.FPCam.ServerFirePoint.forward;

        charge = Mathf.Clamp01(charge);
        float chargedVelocity = ArrowVelocity * charge;
        float baseDamage = Stats.GetDamage();
        float totalDamage = baseDamage * charge;

        SpawnArrow(spawnPos, aimDir, Loadout.transform, chargedVelocity, baseDamage, totalDamage, ServerPendingEffects.ToArray(), passedTime, isServer: true);
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            if (conn == Owner) continue;
            AllTargetFireRPC(conn, Loadout, baseDamage, totalDamage, chargedVelocity, clampedTick, ServerPendingEffects.ToArray());
        }
        ClearEffects(isServer: true, clampedTick);
        ServerCooldown.StartAtTick(clampedTick, ReloadSpeed + 0.05f);
    }

    [TargetRpc]
    public void AllTargetFireRPC(NetworkConnection conn, NetworkObject shooter, float baseDamage, float totalDamage, float velocity, uint tick, int[] effects)
    {
        uint ObserverTick = TimeManager.LocalTick;
        uint clampedTick = tick > ObserverTick ? ObserverTick : tick;
        float passedTime = (float)TimeManager.TimePassed(clampedTick, allowNegative: false);

        Vector3 spawnPos = Loadout.TP_BowFirePoint.position;
        Vector3 aimDir = Loadout.TP_BowFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, shooter.transform, velocity, baseDamage, totalDamage, effects, passedTime, isServer: false);
    }

    public void QueueEffect(Ability ability, int abilityID, bool isServer)
    {
        var pendingEffects = isServer ? ServerPendingEffects : ClientPendingEffects;
        var pendingAbilities = isServer ? ServerPendingAbilties : ClientPendingAbilties;

        Toggle(pendingEffects, abilityID);
        Toggle(pendingAbilities, ability);
    }

    private static void Toggle<T>(ICollection<T> collection, T item)
    {
        if (!collection.Remove(item))
            collection.Add(item);
    }
    public void QueueCrit(string source, float multiplier, bool isServer)
    {
        var Dict = isServer? ServerPendingCrits : ClientPendingCrits;
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
    public void SpawnArrow(Vector3 pos, Vector3 dir, Transform source, float velocity, float baseDamage, float totalDamage, int[] EffectArray, float passedTime, bool isServer)
    {
        Arrow ArrowInstance = ArrowPoolManager.Instance.Get(pos, Quaternion.LookRotation(dir));
        ArrowInstance.Initialize(source, dir, velocity, passedTime, baseDamage, totalDamage, EffectArray, isServer);
    }
    public void ClearEffects(bool isServer, uint tick = 0)
    {
        if (isServer)
        {
            foreach(var ability in ServerPendingAbilties)
            {
                ServerTriggerCooldown(tick, ability);
            }
            ServerPendingAbilties.Clear();
            ServerPendingEffects.Clear();
            ServerPendingCrits.Clear();
        }
        else
        {
            foreach (var ability in ClientPendingAbilties)
            {
                ClientTriggerCooldown(ability);
            }
            ClientPendingAbilties.Clear();
            ClientPendingEffects.Clear();
            ClientPendingCrits.Clear();
        }
    }
}