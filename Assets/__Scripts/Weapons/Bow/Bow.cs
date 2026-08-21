using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class Bow : Weapon
{
    public int TestingLimb;
    public int TestingHandle;
    internal bool FireEffectActive;
    public GameObject FireEffect;
    public List<GameObject> FireArrows;

    private BowData Data;
    private float ReloadSpeed;
    internal float ArrowVelocity;

    private float CurrentCharge;
    private bool IsCharging;

    internal List<Ability> ClientPendingAbilties = new();
    internal List<int> ClientPendingEffects = new();

    internal List<Ability> ServerPendingAbilties = new();
    internal List<int> ServerPendingEffects = new();

    public override void Initalize(PlayerModule player, int[] materialArray, int index, NetworkRole role)
    {
        Data = WeaponData as BowData;
        base.Initalize(player, materialArray,index, role);
        if (role == NetworkRole.Observer) return;

        Player.Loadout.RebindAnimator(WeaponData.WeaponName);
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
    public override void GainStats(bool isServer)
    {
        Player.Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed, isServer);
    }
    public override void RemoveStats(bool isServer)
    {
        Player.Stats.SetWeaponContribution(0, 0, isServer);
        SecondaryEAbility.Deinitialize();
        PrimaryQAbility.Deinitialize();
    }
    public override void InterruptAttack()
    {
        IsCharging = false;
        CurrentCharge = 0;
        Player.Loadout.WeaponAnimator.SetBool("Aiming", false);
    }
    public override void AttackRequest()
    {
        if (!ClientCooldown.IsReady || ClientVariables.PrimaryAbility.BlockAttacks || ClientVariables.SecondaryAbility.BlockAttacks)
            return;
        if (!IsCharging)
            IsCharging = true;

        CurrentCharge = Mathf.Clamp01(CurrentCharge + Time.deltaTime / Player.Stats.ClientValues.AttackSpeed);

        Player.Loadout.WeaponAnimator.SetBool("Aiming", true);
    }
    public override void ReleaseRequest()
    {
        if (!IsCharging) return;
        IsCharging = false;

        Player.Loadout.WeaponAnimator.SetBool("Aiming", false);

        float chargedVelocity = ArrowVelocity * CurrentCharge;

        Vector3 spawnPos = Player.Loadout.FPCam.ClientFirePoint.position;
        Vector3 aimDir = Player.Loadout.FPCam.ClientFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, Player, chargedVelocity, 0, ClientPendingEffects.ToArray(), 0f, isServer: false);
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

        uint serverTick = TimeManager.Tick;
        uint clampedTick = tick > serverTick ? serverTick : tick;
        if (serverTick - clampedTick > MAX_TICK_DELAY)
            return;
        float passedTime = (float)TimeManager.TimePassed(clampedTick, allowNegative: false);

        Vector3 spawnPos = Player.Loadout.FPCam.ServerFirePoint.position;
        Vector3 aimDir = Player.Loadout.FPCam.ServerFirePoint.forward;

        charge = Mathf.Clamp01(charge);
        float chargedVelocity = ArrowVelocity * charge;
        float totalDamage = Player.Stats.GetDamage() * charge;

        SpawnArrow(spawnPos, aimDir, Player, chargedVelocity, totalDamage, ServerPendingEffects.ToArray(), passedTime, isServer: true);
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            if (conn == Owner) continue;
            FireTargetRPC(conn, totalDamage, chargedVelocity, ServerPendingEffects.ToArray(), clampedTick);
        }
        ClearEffects(isServer: true, clampedTick);
        ServerCooldown.StartAtTick(clampedTick, ReloadSpeed + 0.05f);
    }

    [TargetRpc]
    public void FireTargetRPC(NetworkConnection conn, float totalDamage, float velocity, int[] effects, uint tick)
    {
        float passedTime = (float)TimeManager.TimePassed(tick, allowNegative: false);

        Vector3 spawnPos = Player.Loadout.FPCam.ServerFirePoint.position;
        Vector3 aimDir = Player.Loadout.FPCam.ServerFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, null, velocity, totalDamage, effects, passedTime, isServer: false);
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
    public void SpawnArrow(Vector3 pos, Vector3 dir, PlayerModule source, float velocity, float totalDamage, int[] EffectArray, float passedTime, bool isServer)
    {
        Arrow ArrowInstance = ArrowPoolManager.Instance.Get(pos, Quaternion.LookRotation(dir));
        ArrowInstance.Initialize(source, dir, velocity, passedTime, totalDamage, EffectArray, isServer);
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
        }
        else
        {
            foreach (var ability in ClientPendingAbilties)
            {
                ClientTriggerCooldown(ability);
            }
            ClientPendingAbilties.Clear();
            ClientPendingEffects.Clear();
            if(FireEffectActive)
            {
                FireEffectActive = false;
                FireEffect.SetActive(false);
                foreach (var item in FireArrows)
                {
                    item.SetActive(false);
                }
            }
        }
    }

    //Empty Data Classes For Serialization Compile
    [ServerRpc]
    private void UnloadQuiverPacket(UnloadQuiverPacket packet) { }
}