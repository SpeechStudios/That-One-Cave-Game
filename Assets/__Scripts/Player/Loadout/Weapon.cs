using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    internal Ability PrimaryQAbility;
    internal Ability SecondaryEAbility;

    internal float Damage;
    internal float AttackSpeed;

    private readonly SyncVar<uint> PrimaryLastUsedTick = new(0u);
    private readonly SyncVar<uint> SecondaryLastUsedTick = new(0u);

    internal bool ClientCanAttack = true;
    internal bool ServerCanAttack = true;

    internal PlayerLoadoutModule Loadout;
    private PlayerControllerModule MovementController;

    public virtual void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, int[] materialArray)
    {
        MovementController = movement;
        Loadout = loadout;
        SetStats(materialArray);
    }
    public virtual void Deinitialize() { }
    public virtual void SetStats(int[] materialArray) { }
    public virtual void AttackRequest() { }
    public virtual void ReleaseRequest() { }

    public void PrimaryAbilityRequest() => RequestActivateAbility(PrimaryQAbility, PrimaryLastUsedTick, isPrimary: true);
    public void SecondaryAbilityRequest() => RequestActivateAbility(SecondaryEAbility, SecondaryLastUsedTick, isPrimary: false);

    private void RequestActivateAbility(Ability ability, SyncVar<uint> lastUsedTick, bool isPrimary)
    {
        if (!IsOwner) return;
        if (ability == null) return;

        uint currentTick = TimeManager.LocalTick;
        float tickDelta = (float)TimeManager.TickDelta;
        bool isMovementAbility = ability is MovementAbility;

        if (ability.IsOnCooldown(lastUsedTick.Value, currentTick, tickDelta)) return;

        if (isMovementAbility)
        {
            MovementController.BeginMovementOverride(isPrimary, currentTick);
        }
        else
        {
            ability.ClientActivate(currentTick);
        }

        ServerActivate(isPrimary, currentTick, isMovementAbility);
    }

    [ServerRpc]
    private void ServerActivate(bool isPrimary, uint tick, bool isMovementAbility)
    {
        if (isPrimary)
        {
            PrimaryLastUsedTick.Value = tick;
            if (!isMovementAbility)
            {
                PrimaryQAbility.ServerActivate(tick);
                ObserverActivate(true, tick);
            } 
        }
        else
        {
            SecondaryLastUsedTick.Value = tick;
            if (!isMovementAbility)
            {
                SecondaryEAbility.ServerActivate(tick);
                ObserverActivate(false, tick);
            }
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
}