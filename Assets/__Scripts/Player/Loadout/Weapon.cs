using FishNet.Object;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    internal Ability QAbility;
    internal Ability EAbility;

    internal bool ClientCanAttack = true;
    internal bool ServerCanAttack = true;

    internal PlayerLoadoutModule Loadout;

    public virtual void Initalize(PlayerLoadoutModule loadout, int[] materialArray)
    {
        Loadout = loadout;
        SetStats(materialArray);
    }
    public virtual void AttackRequest() {  }
    public virtual void ReleaseRequest() { }

    public virtual void Deinitialize() { }
    public virtual void SetStats(int[] materialArray) { }
}