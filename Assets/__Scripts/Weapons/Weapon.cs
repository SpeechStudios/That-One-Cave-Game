using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;
public class WeaponData : ScriptableObject
{
    public string WeaponName;
}
public struct AbilityContext
{

}
public class Weapon : NetworkBehaviour
{
    public WeaponData WeaponData;

    internal int[] MaterialArray = null;
    internal int TotalWeaponDamage;
    internal float TotalWeaponAttackSpeed;
    internal Ability PrimaryQAbility;
    internal Ability SecondaryEAbility;

    internal float AttackTolerance = 0.05f;

    private readonly SyncVar<uint> PrimaryLastUsedTick = new(0u);
    private readonly SyncVar<uint> SecondaryLastUsedTick = new(0u);

    internal float LastAttackTime;
    internal bool ClientCanAttack = true;

    internal bool ClientBlockAttacks = false;
    internal bool ServerBlockAttacks = false;

    internal bool ClientBlockOtherAbilities = false;
    internal bool ServerBlockOtherAbilities = false;

    internal PlayerLoadoutModule Loadout;
    internal PlayerStatsModule Stats;
    private PlayerControllerModule MovementController;

    public virtual void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray)
    {
        MovementController = movement;
        Loadout = loadout;
        Stats = stats;
        if (materialArray != null)
            MaterialArray = materialArray;
        else
            MaterialArray = null;
    }

    public virtual void Activate()
    {
        GainStats();
        Loadout.RebindAnimator(WeaponData.WeaponName);

        if (Loadout.WeaponAnimator != null)
        {
            Loadout.WeaponAnimator.SetTrigger("Attack");
            Loadout.WeaponAnimator.Update(0f);
            int attackStateHash = Loadout.WeaponAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            Loadout.WeaponAnimator.Play(attackStateHash, 0, 0f);
            Loadout.WeaponAnimator.Update(0f);
        }
    }
    public virtual void Deactivate() { RemoveStats(); }
    public virtual void GainStats() { }
    public virtual void RemoveStats() { }
    public virtual void AttackRequest() { }
    public virtual void InterruptAttack() { }
    public virtual void ReleaseRequest() { }

    public void PrimaryAbilityRequest() => RequestActivateAbility(PrimaryQAbility, PrimaryLastUsedTick, isPrimary: true);
    public void SecondaryAbilityRequest() => RequestActivateAbility(SecondaryEAbility, SecondaryLastUsedTick, isPrimary: false);

    private void RequestActivateAbility(Ability ability, SyncVar<uint> lastUsedTick, bool isPrimary)
    {
        if (!IsOwner) return;
        if (ability == null) return;
        if (ClientBlockOtherAbilities) return;

        uint currentTick = TimeManager.LocalTick;
        float tickDelta = (float)TimeManager.TickDelta;
        bool isMovementAbility = ability is MovementAbility;

        if (ability.IsOnCooldown(lastUsedTick.Value, currentTick, tickDelta)) return;

        if (ability.Data.BlockAttacks) ClientBlockAttacks = true;
        if (ability.Data.BlockOtherAbilities) ClientBlockOtherAbilities = true;
        if (ability.Data.InterruptAutoAttack) InterruptAttack();

        if (isMovementAbility)
        {
           MovementController.BeginMovementOverride(isPrimary, currentTick);
        }
        else
        {
           ability.ClientActivate(currentTick);
        }
        ServerActivate(isPrimary, currentTick);
    }

    [ServerRpc]
    private void ServerActivate(bool isPrimary, uint tick)
    {
        if (ServerBlockOtherAbilities) return;

        Ability ability = isPrimary ? PrimaryQAbility : SecondaryEAbility;
        var lastUsedTIck = isPrimary ? PrimaryLastUsedTick : SecondaryLastUsedTick;

        if (ability.Data.BlockAttacks) ServerBlockAttacks = true;
        if (ability.Data.BlockOtherAbilities) ServerBlockOtherAbilities = true;

        lastUsedTIck.Value = tick;
        if (ability is not MovementAbility)
        {
            ability.ServerActivate(tick);
            ObserverActivate(isPrimary, tick);
        }
    }
    [ObserversRpc]
    private void ObserverActivate(bool isPrimary, uint tick)
    {
        /*
        if(isPrimary)
        {
            PrimaryQAbility.ObserverActivate(tick);
        }
        else
        {
            SecondaryEAbility.ObserverActivate(tick);
        }
        */
    }
    public void OnAbilityComplete(AbilityData data)
    {
        if (data.BlockAttacks)
        {
            ClientBlockAttacks = false;
            ServerBlockAttacks = false;
        }
        if (data.BlockOtherAbilities)
        {
            ClientBlockOtherAbilities = false;
            ServerBlockOtherAbilities = false;
        }
    }
}