using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadoutModule : NetworkBehaviour
{
    public PlayerControllerModule Controller;
    public PlayerInventoryModule Inventory;

    public Animator WeaponAnimator;
    public Transform FP_WeaponParent;
    public Transform TP_WeaponParent;
    public Transform MeleeHitDetectionRoot;
    public Transform BowFirePoint;
    public LayerMask HitLayers;
    public Weapon Fists;

    internal Weapon Weapon;
    internal Weapon Pickaxe;
    internal Weapon Axe;
    private Armor Head;
    private Armor Chest;
    private Armor Legs;

    private int CurrentItemIndex; //0 Weapon, // 1 Pickaxe, //2 Axe
    private bool Initalized;
    private Dictionary<ItemSlotType, EquippedSlot> ServerLoadout = new();
    public void Init()
    {
        Initalized = true;
        Fists.Initalize(Controller, this, null);
    }

    [Server]
    public void EquipItem(Item item, ItemSlotType type, int[] materialArray, NetworkConnection conn)
    {
        NetworkObject itemPrefab = Instantiate(item.EquipPrefab);
        InstanceFinder.ServerManager.Spawn(itemPrefab, conn);
        Observer_Equip_RPC(itemPrefab, materialArray, type);
        ServerLoadout[type] = new EquippedSlot { Item = itemPrefab, IsEquipped = true };
        if (type == ItemSlotType.Weapon || type == ItemSlotType.Pickaxe || type == ItemSlotType.Axe)
        {
            Weapon weapon = itemPrefab.GetComponent<Weapon>();
            weapon.Initalize(Controller, this, materialArray);
        }
    }
    [Server]
    public void UnequipItem(ItemSlotType type, NetworkConnection conn)
    {
        NetworkObject itemPrefab = ServerLoadout[type].Item;
        if (type == ItemSlotType.Weapon || type == ItemSlotType.Pickaxe || type == ItemSlotType.Axe)
        {
            itemPrefab.GetComponent<Weapon>().Deinitialize();
        }
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

        Transform parent;
        bool isFirst = Weapon == null && Pickaxe == null && Axe == null;

        switch (slotType)
        {
            case ItemSlotType.Weapon:
                Weapon = obj.GetComponent<Weapon>();
                parent = isLocalOwner ? FP_WeaponParent : TP_WeaponParent;
                Weapon.Initalize(Controller, this, materialArray);
                break;
            case ItemSlotType.Pickaxe:
                Pickaxe = obj.GetComponent<Weapon>();
                parent = isLocalOwner ? FP_WeaponParent : TP_WeaponParent;
                Pickaxe.Initalize(Controller, this, materialArray);
                break;
            case ItemSlotType.Axe:
                Axe = obj.GetComponent<Weapon>();
                parent = isLocalOwner ? FP_WeaponParent : TP_WeaponParent;
                Axe.Initalize(Controller, this, materialArray);
                break;
            default:
                return;
        }
        if(!isFirst)
        {
            if (CurrentItemIndex == 0 && slotType != ItemSlotType.Weapon)
                obj.gameObject.SetActive(false);
            if (CurrentItemIndex == 1 && slotType != ItemSlotType.Pickaxe)
                obj.gameObject.SetActive(false);
            if (CurrentItemIndex == 2 && slotType != ItemSlotType.Axe)
                obj.gameObject.SetActive(false);
        }
        else
        {
            if (slotType == ItemSlotType.Weapon)
                CurrentItemIndex = 0;
            if (slotType == ItemSlotType.Pickaxe)
                CurrentItemIndex = 1;
            if (slotType == ItemSlotType.Axe)
                CurrentItemIndex = 2;
        }
        
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
            case ItemSlotType.Axe:
                Axe.Deinitialize();
                Axe = null;
                break;
            case ItemSlotType.Pickaxe:
                Pickaxe.Deinitialize();
                Pickaxe = null;
                break;
            default:
                return;
        }
    }

    void Update()
    {
        if (!Initalized) return;
        if (Inventory.InventoryOpen) return;

        SetAttackInputs();
        SetAbilityInputs();
        SetWeaponChangeInputs();
    }
    private void SetAttackInputs()
    {
        bool Attack = Controller.PlayerInput.Player.Attack.IsPressed();

        if (Attack)
        {
            if (CurrentItemIndex == 0)
            {
                if (Weapon != null)
                {
                    Weapon.AttackRequest();
                    return;
                }
                Fists.AttackRequest();
                return;
            }
            if(CurrentItemIndex == 1)
            {
                if (Pickaxe != null)
                {
                    Pickaxe.AttackRequest();
                    return;
                }
            }
            if (CurrentItemIndex == 2)
            {
                if (Axe != null)
                {
                    Axe.AttackRequest();
                    return;
                }
            }
        }

        bool AttackReleased = Controller.PlayerInput.Player.Attack.WasReleasedThisFrame();
        if (AttackReleased)
        {
            if (CurrentItemIndex == 0)
            {
                if (Weapon != null)
                    Weapon.ReleaseRequest();
            }
        }
    }
    private void SetAbilityInputs()
    {
        if (Weapon == null) return;
        bool PrimaryAbility = Controller.PlayerInput.Player.PrimaryAbility.WasPressedThisFrame();
        if (PrimaryAbility)
        {
            Weapon.PrimaryAbilityRequest();
        }

        bool Secondary = Controller.PlayerInput.Player.SecondaryAbility.WasPressedThisFrame();
        if (Secondary)
        {
            Weapon.SecondaryAbilityRequest();
        }
    }
    private void SetWeaponChangeInputs()
    {
        if(Controller.PlayerInput.Player.Option1.WasPressedThisFrame())
        {
            if (Weapon == null) return;

            ShowCurrentWeapon(false);
            CurrentItemIndex = 0;
            ShowCurrentWeapon(true);
        }
        if (Controller.PlayerInput.Player.Option2.WasPressedThisFrame())
        {
            if (Pickaxe == null) return;

            ShowCurrentWeapon(false);
            CurrentItemIndex = 1;
            ShowCurrentWeapon(true);
        }
        if (Controller.PlayerInput.Player.Option3.WasPressedThisFrame())
        {
            if (Axe == null) return;

            ShowCurrentWeapon(false);
            CurrentItemIndex = 2;
            ShowCurrentWeapon(true);
        }
    }
    private void ShowCurrentWeapon(bool enable)
    {
        if (CurrentItemIndex == 0)
            Weapon.gameObject.SetActive(enable);
        if (CurrentItemIndex == 1)
            Pickaxe.gameObject.SetActive(enable);
        if (CurrentItemIndex == 2)
            Axe.gameObject.SetActive(enable);
    }
    public void StartWeaponCooldown(Weapon weapon, float cooldown, bool isServer)
    {
        StartCoroutine(AttackCooldownCoroutine(weapon, cooldown, isServer));
    }
    private IEnumerator AttackCooldownCoroutine(Weapon weapon, float cooldown, bool isServer)
    {
        yield return new WaitForSecondsRealtime(cooldown);
        if (!isServer)
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
