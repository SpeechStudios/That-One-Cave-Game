using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadoutModule : NetworkBehaviour
{
    public PlayerControllerModule Controller;
    public Animator MainHandAnimator;

    public Transform FP_MainHandParent;
    public Transform FP_OffhandParent;
    public Transform TP_MainHandParent;
    public Transform TP_OffhandParent;
    [Space]
    public Transform MainHandRoot;
    public Transform OffHandRoot;

    [Header("Loadout")]
    internal Weapon MainHand;
    internal Weapon OffHand;
    private Weapon Pickaxe;
    private Weapon Axe;
    private Armor Head;
    private Armor Chest;
    private Armor Legs;

    private Camera PlayerCamera;
    private int CurrentWeaponIndex = 1;
    private bool Initalized;
    private Dictionary<ItemSlotType, EquippedSlot> ServerLoadout = new();
    public void Init()
    {
        PlayerCamera = Camera.main;
        Initalized = true;
    }

    [Server]
    public void EquipItem(Item item, ItemSlotType type, int[] materialArray, NetworkConnection conn)
    {
        NetworkObject itemPrefab = Instantiate(item.EquipPrefab);
        InstanceFinder.ServerManager.Spawn(itemPrefab, conn);
        Observer_Equip_RPC(itemPrefab, materialArray, type);
        ServerLoadout[type] = new EquippedSlot { Item = itemPrefab, IsEquipped = true };
        Weapon weapon = itemPrefab.GetComponent<Weapon>();
        weapon.Initalize(this, materialArray, IsMainHand(type));
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
        bool isMainHandWeapon = IsMainHand(slotType);

        switch (slotType)
        {
            case ItemSlotType.MainHand:
                MainHand = weapon;
                parent = isLocalOwner ? FP_MainHandParent : TP_MainHandParent;
                break;
            case ItemSlotType.OffHand:
                OffHand = weapon;
                parent = isLocalOwner ? FP_OffhandParent : TP_OffhandParent;
                break;
            case ItemSlotType.Pick:
                Pickaxe = weapon;
                parent = isLocalOwner ? FP_MainHandParent : TP_MainHandParent;
                break;
            case ItemSlotType.Axe:
                Axe = weapon;
                parent = isLocalOwner ? FP_MainHandParent : TP_MainHandParent;
                break;
            default:
                return;
        }

        weapon.Initalize(this, materialArray, isMainHandWeapon);
        VisualEquip(slotType);
        obj.transform.SetParent(parent, false);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
    [ObserversRpc]
    private void Observer_UnEquip_RPC(ItemSlotType slotType)
    {
        if (IsServerInitialized && !IsHostInitialized) return;

        if (slotType == ItemSlotType.MainHand)
        {
            MainHand.Deinitialize();
            MainHand = null;
        }
        if (slotType == ItemSlotType.OffHand)
        {
            OffHand.Deinitialize();
            OffHand = null;
        }
        if (slotType == ItemSlotType.Pick)
        {
            Pickaxe.Deinitialize();
            Pickaxe = null;
        }
        if (slotType == ItemSlotType.Axe)
        {
            Axe.Deinitialize();
            Axe = null;
        }

    }

    void Update()
    {
        if (!Initalized) return;

        CheckWeaponSwapInputs();
        bool Attack = Controller.PlayerInput.Player.Attack.IsPressed();
        if (Attack)
        {
            if (CurrentWeaponIndex == 0 && MainHand != null)
            {
                MainHand.AttackRequest();
            }
            if (CurrentWeaponIndex == 1 && Pickaxe != null)
            {
                Pickaxe.AttackRequest();
            }
            if (CurrentWeaponIndex == 2 && Axe != null)
            {
                Axe.AttackRequest();
            }
        }
    }
    public void CheckWeaponSwapInputs()
    {
        if (Controller.PlayerInput.Player.Option1.WasPressedThisFrame())
        {
            UpdateCurrentWeapon(0);
        }
        if (Controller.PlayerInput.Player.Option2.WasPressedThisFrame())
        {
            UpdateCurrentWeapon(1);
        }
        if (Controller.PlayerInput.Player.Option3.WasPressedThisFrame())
        {
            UpdateCurrentWeapon(2);
        }
    }
    public void UpdateCurrentWeapon(int index)
    {
        if (CurrentWeaponIndex == index)
            return;

        SetWeaponActive(false);

        CurrentWeaponIndex = index;

        SetWeaponActive(true);
        //Play Fast Animation
        //Send RPC to update weapon on clients
    }
    private void SetWeaponActive(bool active)
    {
        switch (CurrentWeaponIndex)
        {
            case 0:
                if (MainHand != null)
                    MainHand.gameObject.SetActive(active);
                if (OffHand != null)
                    OffHand.gameObject.SetActive(active);
                break;
            case 1:
                if (Pickaxe != null)
                    Pickaxe.gameObject.SetActive(active);
                break;
            case 2:
                if (Axe != null)
                    Axe.gameObject.SetActive(active);
                break;
        }
    }
    public void VisualEquip(ItemSlotType slotType)
    {
        switch (slotType)
        {
            case ItemSlotType.MainHand:
                if (Axe == null && Pickaxe == null)
                {
                    if (CurrentWeaponIndex != 0)
                        UpdateCurrentWeapon(0);
                }
                else
                {
                    if (CurrentWeaponIndex != 0)
                        MainHand.gameObject.SetActive(false);
                }
                break;
            case ItemSlotType.OffHand:
                if (Axe == null && Pickaxe == null)
                {
                    if (CurrentWeaponIndex != 0)
                        UpdateCurrentWeapon(0);
                }
                else
                {
                    if (CurrentWeaponIndex != 0)
                        OffHand.gameObject.SetActive(false);
                }
                break;
            case ItemSlotType.Pick:
                if (Axe == null && MainHand == null && OffHand == null)
                {
                    if (CurrentWeaponIndex != 1)
                        UpdateCurrentWeapon(1);
                }
                else
                {
                    if (CurrentWeaponIndex != 1)
                        Pickaxe.gameObject.SetActive(false);
                }
                break;
            case ItemSlotType.Axe:
                if (Pickaxe == null && MainHand == null && OffHand == null)
                {
                    if (CurrentWeaponIndex != 2)
                        UpdateCurrentWeapon(2);
                }
                else
                {
                    if (CurrentWeaponIndex != 2)
                        Axe.gameObject.SetActive(false);
                }
                break;
            default:
                break;
        }
    }
    public void StartWeaponCooldown(Weapon weapon, float cooldown, bool isServer)
    {
        StartCoroutine(AttackCooldownCoroutine(weapon, cooldown, isServer));
    }
    public IEnumerator AttackCooldownCoroutine(Weapon weapon, float cooldown, bool isServer)
    {
        yield return new WaitForSecondsRealtime(cooldown);
        if (isServer)
            weapon.ServerCanAttack = true;
        else
            weapon.ClientCanAttack = true;
    }
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private bool IsMainHand(ItemSlotType slotType)
    {
        switch (slotType)
        {
            case ItemSlotType.MainHand:
                return true;
            case ItemSlotType.OffHand:
                return false;
            case ItemSlotType.Pick:
                return true;
            case ItemSlotType.Axe:
                return true;
            default:
                return false;
        }
    }
}
