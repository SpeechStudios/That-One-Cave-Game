using FishNet.Object;
using Unity.GraphToolkit.Editor;
using Unity.VisualScripting;
using UnityEngine;
public class WeaponData : ScriptableObject
{
    public string WeaponName;
}
public struct WeaponNetworkVariables
{
    public AbilityNetworkVariables PrimaryAbility;
    public AbilityNetworkVariables SecondaryAbility;
}
public struct AbilityNetworkVariables
{
    public CooldownTimer Cooldown;
    public bool PendingEffect;
    public bool BlockAttacks;
    public bool BlockAbilities;
    public bool BlockSwapping;
}
public class Weapon : NetworkBehaviour
{
    public WeaponData WeaponData;

    internal int[] MaterialArray = null;
    internal int TotalWeaponDamage;
    internal float TotalWeaponAttackSpeed;

    public CooldownTimer ClientCooldown;
    public CooldownTimer ServerCooldown;

    internal Ability PrimaryQAbility;
    internal Ability SecondaryEAbility;

    internal float AttackTolerance = 0.05f;
    internal WeaponNetworkVariables ServerVariables;
    internal WeaponNetworkVariables ClientVariables;

    internal float LastAttackTime;
    internal bool ClientCanAttack = true;

    internal PlayerLoadoutModule Loadout;
    internal PlayerStatsModule Stats;
    private PlayerControllerModule MovementController;

    internal const uint MAX_TICK_DELAY = 30;

    #region Initalization
    public virtual void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray, NetworkRole role)
    {
        MovementController = movement;
        Loadout = loadout;
        Stats = stats;
        
        if (materialArray != null)
            MaterialArray = materialArray;
        else
            MaterialArray = null;
        if (role == NetworkRole.Server)
            InitalizeStats(stats: true, abilities: true);
        if (role == NetworkRole.Owner)
            InitalizeStats(stats: true, abilities: true);
        if (role == NetworkRole.Observer)
            InitalizeStats(stats: false, abilities: true);
    }
    public virtual void InitalizeStats(bool stats, bool abilities) { }
    public void Activate(NetworkRole role)
    {
        if(role == NetworkRole.Server)
        {
            Debug.Log("Activating For Server");
            GainStats(true);
        }
        if(role == NetworkRole.Owner)
        {
            GainStats(false);
            AffixateModel();

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
        if(role == NetworkRole.Observer)
        {
            AffixateModel();
        }
    }
    public virtual void Deactivate(NetworkRole role) 
    {
        if (role == NetworkRole.Server)
        {
            RemoveStats(true);
        }
        if (role == NetworkRole.Owner)
        {
            RemoveStats(false);
        }
    }
    public virtual void GainStats(bool isServer) { }
    public virtual void RemoveStats(bool isServer) { }
    public virtual void AffixateModel() { }
    #endregion

    #region Input Requests
    public virtual void AttackRequest() { }
    public virtual void InterruptAttack() { }
    public virtual void ReleaseRequest() { }
    public void PrimaryAbilityRequest() => RequestActivateAbility(isPrimary: true);
    public void SecondaryAbilityRequest() => RequestActivateAbility(isPrimary: false);
    #endregion

    private void RequestActivateAbility(bool isPrimary)
    {
        if (!IsOwner) return;
        Ability ability = isPrimary ? PrimaryQAbility : SecondaryEAbility;
        if (!AbilityCanBeActivated(ability, ClientVariables, isPrimary)) return;

        uint currentTick = TimeManager.LocalTick;

        switch (ability.Data.CooldownType)
        {
            case CooldownType.Instant:
                ClientTriggerCooldown(ability);
                ClientActivateAbility(ability, currentTick, isPrimary);
                break;
            case CooldownType.TogglePending:
                ToggleAbility(ref ClientVariables, isPrimary);
                ClientActivateAbility(ability, currentTick, isPrimary);
                break;
        }
    }
    [ServerRpc]
    private void ServerActivate(bool isPrimary, uint tick)
    {
        Ability ability = isPrimary ? PrimaryQAbility : SecondaryEAbility;
        if (!AbilityCanBeActivated(ability, ServerVariables, isPrimary)) return;
        uint serverTick = TimeManager.LocalTick;
        uint clampedTick = tick > serverTick ? serverTick : tick;
        if (serverTick - clampedTick > MAX_TICK_DELAY)
            return;

        switch (ability.Data.CooldownType)
        {
            case CooldownType.Instant:
                ServerTriggerCooldown(clampedTick, ability);
                ServerActivateAbility(ability, clampedTick, isPrimary);
                break;
            case CooldownType.TogglePending:
                ToggleAbility(ref ServerVariables, isPrimary);
                ServerActivateAbility(ability, clampedTick, isPrimary);
                break;
            default:
                break;
        }
    }
    [ObserversRpc]
    private void ObserverActivate(bool isPrimary, uint tick)
    {
        if (isPrimary)
        {
            PrimaryQAbility.ObserverActivate(tick);
        }
        else
        {
            SecondaryEAbility.ObserverActivate(tick);
        }
    }
    private bool AbilityCanBeActivated(Ability ability, WeaponNetworkVariables variables, bool isPrimary)
    {
        if (ability == null) return false;
        var cooldown = isPrimary ? variables.PrimaryAbility.Cooldown : variables.SecondaryAbility.Cooldown;
        if (!cooldown.IsReady) return false;
        if (variables.PrimaryAbility.BlockAbilities) return false;
        if (variables.SecondaryAbility.BlockAbilities) return false;

        return true;
    }
    private void ToggleAbility(ref WeaponNetworkVariables variables, bool isPrimary)
    {
        if (isPrimary)
            variables.PrimaryAbility.PendingEffect = !variables.PrimaryAbility.PendingEffect;
        else
            variables.SecondaryAbility.PendingEffect = !variables.SecondaryAbility.PendingEffect;
    }
    private void ClientActivateAbility(Ability ability, uint currentTick, bool isPrimary)
    {
        bool isMovementAbility = ability is MovementAbility;
        var abilityVariables = isPrimary ? ClientVariables.PrimaryAbility : ClientVariables.SecondaryAbility;

        if (ability.Data.BlockAttacks) abilityVariables.BlockAttacks = true;
        if (ability.Data.BlockOtherAbilities) abilityVariables.BlockAbilities = true;
        if (ability.Data.BlockSwapping) abilityVariables.BlockSwapping = true;
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
    private void ServerActivateAbility(Ability ability, uint serverTick, bool isPrimary)
    {
        var abilityVariables = isPrimary ? ServerVariables.PrimaryAbility : ServerVariables.SecondaryAbility;
        if (ability.Data.BlockAttacks) abilityVariables.BlockAttacks = true;
        if (ability.Data.BlockOtherAbilities) abilityVariables.BlockAbilities = true;
        if (ability.Data.BlockSwapping) abilityVariables.BlockSwapping = true;

        if (ability is not MovementAbility)
        {
            ability.ServerActivate(serverTick);
            ObserverActivate(isPrimary, serverTick);
        }
    }

    public void ClientTriggerCooldown(Ability ability)
    {
        if (ability == PrimaryQAbility)
        {
            ClientVariables.PrimaryAbility.Cooldown.Start(PrimaryQAbility.Data.Cooldown);
            ClientVariables.PrimaryAbility.PendingEffect = false;
        }
        if (ability == SecondaryEAbility)
        {
            ClientVariables.SecondaryAbility.Cooldown.Start(SecondaryEAbility.Data.Cooldown);
            ClientVariables.SecondaryAbility.PendingEffect = false;
        }
    }
    public void ServerTriggerCooldown(uint startTick, Ability ability)
    {
        if (ability == PrimaryQAbility)
        {
            ServerVariables.PrimaryAbility.Cooldown.StartAtTick(startTick, PrimaryQAbility.Data.Cooldown);
            ServerVariables.PrimaryAbility.PendingEffect = false;
        }
        if (ability == SecondaryEAbility)
        {
            ServerVariables.SecondaryAbility.Cooldown.StartAtTick(startTick, SecondaryEAbility.Data.Cooldown);
            ServerVariables.SecondaryAbility.PendingEffect = false;
        }
    }
    public void AbilityComplete(Ability ability, bool isServer)
    {
        var variables = isServer? ServerVariables : ClientVariables;
        if (ability == PrimaryQAbility)
        {
            variables.PrimaryAbility.BlockAttacks = false;
            variables.PrimaryAbility.BlockAbilities = false;
            variables.PrimaryAbility.BlockSwapping = false;
        }
        if (ability == SecondaryEAbility)
        {
            variables.SecondaryAbility.BlockAttacks = false;
            variables.SecondaryAbility.BlockAbilities = false;
            variables.SecondaryAbility.BlockSwapping = false;
        }
    }
}