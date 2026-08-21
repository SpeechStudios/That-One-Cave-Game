using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public class Bow : Weapon
{
    public int TestingLimb;
    public int TestingHandle;

    private BowData Data;
    private float ReloadSpeed;
    internal float ArrowVelocity;

    private float CurrentCharge;
    private bool IsCharging;
    public bool EffectActive() => ClientPendingEffects.Count > 0;
    public BowEffect BowEffect;
    public List<ArrowEffect> ArrowEffects;

    internal List<(Ability, int)> ClientPendingEffects = new();
    internal List<(Ability, int)> ServerPendingEffects = new();

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
        SpawnArrow(spawnPos, aimDir, Player, chargedVelocity, 0, ClientPendingEffects.Select(x => x.Item2).ToArray(), 0f, isServer: false);
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

        SpawnArrow(spawnPos, aimDir, Player, chargedVelocity, totalDamage, ServerPendingEffects.Select(x => x.Item2).ToArray(), passedTime, isServer: true);
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            if (conn == Owner) continue;
            FireTargetRPC(conn, totalDamage, chargedVelocity, ServerPendingEffects.Select(x => x.Item2).ToArray(), clampedTick);
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
        bool isEnabled;

        if (!pendingEffects.Remove((ability, abilityID)))
        {
            pendingEffects.Add((ability, abilityID));
            isEnabled = true;
        }
        else
        {
            isEnabled = false;
        }

        if (!isServer)
        {
            BowEffectType? effectType = ability switch
            {
                Bow_ExplosiveArrow => BowEffectType.Fire,
                Bow_PoisonArrow => BowEffectType.Poison,
                Bow_JumpShot => BowEffectType.Wind,
                _ => null
            };

            if (effectType.HasValue)
            {
                BowEffect.Toggle(effectType.Value, isEnabled);

                foreach (var arrow in ArrowEffects)
                    arrow.Toggle(effectType.Value, isEnabled);
            }
        }
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
            foreach (var ability in ServerPendingEffects.Select(x => x.Item1))
            {
                ServerTriggerCooldown(tick, ability);
            }
            ServerPendingEffects.Clear();
        }
        else
        {
            foreach (var ability in ServerPendingEffects.Select(x => x.Item1))
            {
                ClientTriggerCooldown(ability);
            }
            if (EffectActive())
            {
                BowEffect.Clear();
                foreach (var arrowEffect in ArrowEffects)
                {
                    arrowEffect.Clear();
                }
            }
            ClientPendingEffects.Clear();
        }
    }

    //Empty Data Classes For Serialization Compile
    [ServerRpc]
    private void UnloadQuiverPacket(UnloadQuiverPacket packet) { }
}