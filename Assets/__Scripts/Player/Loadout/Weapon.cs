using UnityEngine;

public class Weapon : MonoBehaviour
{
    public bool IsMainHand;
    public bool IsTwoHanded;
    public float AttackCooldown;

    internal bool CanAttack = true;

    public void Initalize(bool isMainHand)
    {
        IsMainHand = isMainHand;
    }
    public virtual void Attack(PlayerLoadoutModule loadout, Camera cam) { }

}