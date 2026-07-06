using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    internal Ability PrimaryAbility;
    internal Ability SecondaryAbility;

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

    public void PrimaryAbilityRequest() => RequestActivateAbility(PrimaryAbility, PrimaryLastUsedTick, isPrimary: true);
    public void SecondaryAbilityRequest() => RequestActivateAbility(SecondaryAbility, SecondaryLastUsedTick, isPrimary: false);

    private void RequestActivateAbility(Ability ability, SyncVar<uint> lastUsedTick, bool isPrimary)
    {
        if (!IsOwner) return;
        if (ability == null) return;

        uint currentTick = TimeManager.LocalTick;
        float tickDelta = (float)TimeManager.TickDelta;

        if (ability.IsOnCooldown(lastUsedTick.Value, currentTick, tickDelta)) return;

        lastUsedTick.Value = currentTick;

        if (ability is MovementAbility movementAbility)
        {
            MovementController.BeginMovementOverride(movementAbility, isPrimary, currentTick);
        }
        else
        {
            ability.Activate();
        }
    }

}