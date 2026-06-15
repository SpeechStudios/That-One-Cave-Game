using FishNet.Object;
using NUnit.Framework;
using UnityEngine;
public enum ItemType
{
    Material = 0,
    MeleeWeapon = 1,
    RangedWeapon = 2,
    Ammo = 3,
    Pick = 4,
    Axe = 5,
}

[CreateAssetMenu(fileName = "Item", menuName = "New Item")]
public class Item : ScriptableObject
{
    [Header("Identity")]
    public int ID;
    public string Name;
    public int MaxStackSize;
    public Sprite Icon;
    public WorldItemGameObject WorldItemPrefab;
    public NetworkObject EquipPrefab;
    public ItemSlotType ItemSlotType;
    public ItemType ItemType;

    public int Damage;
    public int Accuracy;
    public int ProjectileSpeed;
    public int RecoveryTime;
    public int ChargeTime;
    public int SwingSpeed;
    public bool RequiresAmmo;

    public ResourceType ResourceType;
    public MaterialType MaterialType;
}
