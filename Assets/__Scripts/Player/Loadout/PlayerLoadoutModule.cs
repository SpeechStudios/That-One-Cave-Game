using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadoutModule : NetworkBehaviour
{
    public PlayerControllerModule Controller;
    public Animator WeaponAnimator;
    public Transform FP_WeaponParent;
    public Transform TP_WeaponParent;
    public Transform MeleeHitDetectionRoot;
    public Weapon Fists;

    internal Weapon Weapon;
    private Armor Head;
    private Armor Chest;
    private Armor Legs;

    private bool Initalized;
    private Dictionary<ItemSlotType, EquippedSlot> ServerLoadout = new();
    public void Init()
    {
        Initalized = true;
        Fists.Initalize(this, null);
    }

    [Server]
    public void EquipItem(Item item, ItemSlotType type, int[] materialArray, NetworkConnection conn)
    {
        NetworkObject itemPrefab = Instantiate(item.EquipPrefab);
        InstanceFinder.ServerManager.Spawn(itemPrefab, conn);
        Observer_Equip_RPC(itemPrefab, materialArray, type);
        ServerLoadout[type] = new EquippedSlot { Item = itemPrefab, IsEquipped = true };
        Weapon weapon = itemPrefab.GetComponent<Weapon>();
        weapon.Initalize(this, materialArray);
    }
    [Server]
    public void UnequipItem(ItemSlotType type, NetworkConnection conn)
    {
        NetworkObject itemPrefab = ServerLoadout[type].Item;
        itemPrefab.GetComponent<Weapon>().Deinitialize();
        itemPrefab.Despawn();
        Observer_UnEquip_RPC(type);
        ServerLoadout[type] = null;

    }
    [ObserversRpc]
    private void Observer_Equip_RPC(NetworkObject obj, int[] materialArray, ItemSlotType slotType)
    {
        if (IsServerInitialized && !IsHostInitialized) return;
        bool isLocalOwner = obj.Owner == LocalConnection;

        if (isLocalOwner)
            SetLayerRecursively(obj.gameObject, LayerMask.NameToLayer("LocalTools"));

        Weapon weapon = obj.GetComponent<Weapon>();
        Transform parent;

        switch (slotType)
        {
            case ItemSlotType.Weapon:
                Weapon = weapon;
                parent = isLocalOwner ? FP_WeaponParent : TP_WeaponParent;
                break;
            default:
                return;
        }

        weapon.Initalize(this, materialArray);
        obj.transform.SetParent(parent, false);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
    [ObserversRpc]
    private void Observer_UnEquip_RPC(ItemSlotType slotType)
    {
        if (IsServerInitialized && !IsHostInitialized) return;

        switch (slotType)
        {
            case ItemSlotType.Weapon:
                Weapon.Deinitialize();
                Weapon = null;
                break;
            default:
                return;
        }
    }

    void Update()
    {
        if (!Initalized) return;

        bool Attack = Controller.PlayerInput.Player.Attack.IsPressed();
        if (Attack)
        {
            if (Weapon != null)
            {
                Weapon.AttackRequest();
                return;
            }
            Fists.AttackRequest();
            return;
        }
    }
    public void StartWeaponCooldown(Weapon weapon, float cooldown, bool isServer)
    {
        StartCoroutine(AttackCooldownCoroutine(weapon, cooldown, isServer));
    }
    private IEnumerator AttackCooldownCoroutine(Weapon weapon, float cooldown, bool isServer)
    {
        yield return new WaitForSecondsRealtime(cooldown);
        if (isServer)
            weapon.ServerCanAttack = true;
        else
        {
            weapon.ClientCanAttack = true;
            WeaponAnimator.speed = 1;
        }
    }
    public void RebindAnimator(string weaponName)
    {
        StartCoroutine(RebindCoroutine(weaponName));
    }
    private IEnumerator RebindCoroutine(string weaponName)
    {
        yield return new WaitForEndOfFrame();
        WeaponAnimator.gameObject.SetActive(false);
        WeaponAnimator.gameObject.SetActive(true);
        WeaponAnimator.SetBool(weaponName, true);
    }
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
