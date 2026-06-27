using FishNet.Object;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public bool IsMainHand;
    public bool IsTwoHanded;

    internal bool ClientCanAttack = true;
    internal bool ServerCanAttack = true;

    internal PlayerLoadoutModule Loadout;

    public virtual void Initalize(PlayerLoadoutModule loadout, int[] materialArray, bool isMainHand)
    {
        IsMainHand = isMainHand;
        Loadout = loadout;
        SetStats(materialArray);
    }
    public virtual void AttackRequest() { }
    public virtual void Deinitialize() { }
    public virtual void SetStats(int[] materialArray) { }
}