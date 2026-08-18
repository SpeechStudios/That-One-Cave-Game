using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadoutModule : NetworkBehaviour
{
    public PlayerModule Player;
    public Transform TP_WeaponParent;
    public Transform TP_BowFirePoint;
    public FirstPersonCamera FPCam;
    public LayerMask HitLayers;
    public LayerMask LOSLayers;

    internal MainCamera MainCam;
    internal Animator WeaponAnimator;
    internal PlayerUIManager PlayerUI;

    internal Weapon Weapon;
    internal Weapon Pickaxe;
    internal Weapon Axe;
    private Armor Head;
    private Armor Chest;
    private Armor Legs;

    private int ClientCurrentItemIndex;
    private int ServerCurrentItemIndex;
    private bool Initalized;
    private Dictionary<ItemSlotType, EquippedSlot> ServerLoadout = new();

    public void ClientInit()
    {
        Initalized = true;
        MainCam = Camera.main.GetComponent<MainCamera>();
        WeaponAnimator = MainCam.MainAnim;
        PlayerUI = PlayerUIManager.Instance;
    }

    [Server]
    public void EquipItem(Item item, ItemSlotType type, int[] materialArray, NetworkConnection conn)
    {
        NetworkObject itemPrefab = Instantiate(item.EquipPrefab);
        InstanceFinder.ServerManager.Spawn(itemPrefab, conn);
        ServerLoadout[type] = new EquippedSlot { Item = itemPrefab, IsEquipped = true };

        Weapon weapon = itemPrefab.GetComponent<Weapon>();
        weapon.Initalize(Player, materialArray, SlotToIndex(type), NetworkRole.Server);
        AssignSlot(type, weapon);

        bool activateWeapon = (Weapon == null && Pickaxe == null && Axe == null) || ServerCurrentItemIndex == SlotToIndex(type);
        if (activateWeapon)
        {
            ServerCurrentItemIndex = SlotToIndex(type);
            weapon.Activate(NetworkRole.Server);
        }
        else
        {
            itemPrefab.gameObject.SetActive(false);
        }

        Target_Equip_RPC(conn, item.ID, itemPrefab, materialArray, type, activateWeapon);
        Observers_Equip_RPC(itemPrefab, materialArray, type, activateWeapon);
    }
    [TargetRpc]
    private void Target_Equip_RPC(NetworkConnection conn, int itemID, NetworkObject obj, int[] materialArray, ItemSlotType slotType, bool activate)
    {
        SetLayerRecursively(obj.gameObject, LayerMask.NameToLayer("LocalTools"));

        Weapon weapon = obj.GetComponent<Weapon>();
        weapon.Initalize(Player, materialArray, SlotToIndex(slotType), NetworkRole.Server);
        AssignSlot(slotType, weapon);
        AssignIcon(slotType, itemID);
        obj.transform.SetParent(MainCam.MainHandPos, false);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (activate)
        {
            ClientCurrentItemIndex = SlotToIndex(slotType);
            weapon.Activate(NetworkRole.Owner);
            SelectItem(slotType);
        }
        else
        {
            obj.gameObject.SetActive(false);
        }

    }
    [ObserversRpc(ExcludeOwner = true)]
    private void Observers_Equip_RPC(NetworkObject obj, int[] materialArray, ItemSlotType slotType, bool activate)
    {
        Weapon weapon = obj.GetComponent<Weapon>();
        weapon.Initalize(Player, materialArray, SlotToIndex(slotType), NetworkRole.Observer);
        AssignSlot(slotType, weapon);

        obj.transform.SetParent(TP_WeaponParent, false);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (activate)
        {
            ClientCurrentItemIndex = SlotToIndex(slotType);
            weapon.Activate(NetworkRole.Observer);
        }
        else
        {
            obj.gameObject.SetActive(false);
        }
    }

    [Server]
    public void UnequipItem(ItemSlotType type, NetworkConnection conn)
    {
        NetworkObject itemPrefab = ServerLoadout[type].Item;
        Weapon weapon = itemPrefab.GetComponent<Weapon>();

        if (SlotToIndex(type) == ServerCurrentItemIndex)
            weapon.Deactivate(NetworkRole.Server);

        itemPrefab.Despawn();
        ServerLoadout[type] = null;
        AssignSlot(type, null);

        Target_UnEquip_RPC(conn, type);
        Observers_UnEquip_RPC(type);
    }
    public bool IsWeaponEquipped(int index)
    {
        if (index == ClientCurrentItemIndex)
            return true;

        return false;
    }
    [TargetRpc]
    private void Target_UnEquip_RPC(NetworkConnection conn, ItemSlotType slotType)
    {
        RemoveIcon(slotType);
        AssignSlot(slotType, null);
        if (SlotToIndex(slotType) == ClientCurrentItemIndex)
        {
            PlayerUI.UI_PlayerOverlay.SelectItem(-1);
        }

    }
    [ObserversRpc(ExcludeOwner = true)]
    private void Observers_UnEquip_RPC(ItemSlotType slotType)
    {
        AssignSlot(slotType, null);
        return;
    }

    void Update()
    {
        if (!Initalized) return;
        if (PlayerUI.InventoryOpen) return;

        SetAttackInputs();
        SetAbilityInputs();
        SetWeaponChangeInputs();
    }

    private void SetAttackInputs()
    {
        bool Attack = Player.Controller.PlayerInput.Player.Attack.IsPressed();

        if (Attack)
        {
            if (ClientCurrentItemIndex == 0)
            {
                if (Weapon != null)
                {
                    Weapon.AttackRequest();
                    return;
                }
                return;
            }
            if (ClientCurrentItemIndex == 1)
            {
                if (Pickaxe != null)
                {
                    Pickaxe.AttackRequest();
                    return;
                }
            }
            if (ClientCurrentItemIndex == 2)
            {
                if (Axe != null)
                {
                    Axe.AttackRequest();
                    return;
                }
            }
        }

        bool AttackReleased = Player.Controller.PlayerInput.Player.Attack.WasReleasedThisFrame();
        if (AttackReleased)
        {
            if (ClientCurrentItemIndex == 0)
            {
                if (Weapon != null)
                    Weapon.ReleaseRequest();
            }
        }
    }
    private void SetAbilityInputs()
    {
        if (Weapon == null) return;
        bool PrimaryAbility = Player.Controller.PlayerInput.Player.PrimaryAbility.WasPressedThisFrame();
        if (PrimaryAbility)
        {
            Weapon.PrimaryAbilityRequest();
        }

        bool Secondary = Player.Controller.PlayerInput.Player.SecondaryAbility.WasPressedThisFrame();
        if (Secondary)
        {
            Weapon.SecondaryAbilityRequest();
        }
    }

    private void SetWeaponChangeInputs()
    {
        if (Weapon != null  && ClientCurrentItemIndex == 0)
        {
            if (Weapon.ClientVariables.PrimaryAbility.BlockSwapping || Weapon.ClientVariables.SecondaryAbility.BlockSwapping) return;
        }
        if (Player.Controller.PlayerInput.Player.Option1.WasPressedThisFrame())
        {
            if (Weapon == null) return;
            SwapTo(0);
            PlayerUI.UI_PlayerOverlay.SelectItem(0);
        }
        if (Player.Controller.PlayerInput.Player.Option2.WasPressedThisFrame())
        {
            if (Pickaxe == null) return;
            SwapTo(1);
            PlayerUI.UI_PlayerOverlay.SelectItem(1);
        }
        if (Player.Controller.PlayerInput.Player.Option3.WasPressedThisFrame())
        {
            if (Axe == null) return;
            SwapTo(2);
            PlayerUI.UI_PlayerOverlay.SelectItem(2);
        }
    }

    private void SwapTo(int index)
    {
        if (index == ClientCurrentItemIndex) return;

        ShowCurrentWeapon(NetworkRole.Owner, false);
        ClientCurrentItemIndex = index;
        ShowCurrentWeapon(NetworkRole.Owner, true);

        Server_SwapWeapon(index);
    }

    [ServerRpc]
    private void Server_SwapWeapon(int index)
    {
        if (index == ServerCurrentItemIndex) return;
        if (Weapon != null && ServerCurrentItemIndex == 0)
        {
            if (Weapon.ServerVariables.PrimaryAbility.BlockSwapping || Weapon.ServerVariables.SecondaryAbility.BlockSwapping) return;
        }

        ShowCurrentWeapon(NetworkRole.Server, false);
        ServerCurrentItemIndex = index;
        ShowCurrentWeapon(NetworkRole.Server, true);

        Observer_SwapWeapon(index);
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_SwapWeapon(int index)
    {
        if (index == ClientCurrentItemIndex) return;
        ShowCurrentWeapon(NetworkRole.Observer, false);
        ClientCurrentItemIndex = index;
        ShowCurrentWeapon(NetworkRole.Observer, true);
    }
    private void ShowCurrentWeapon(NetworkRole role, bool enable)
    {
        Weapon current = GetSlotByIndex(role == NetworkRole.Server ? ServerCurrentItemIndex : ClientCurrentItemIndex);
        if (current == null) return;
        current.gameObject.SetActive(enable);

        if (enable)
            current.Activate(role);
        else
            current.Deactivate(role);
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
    public void StartAbilityCooldown(Weapon weapon, float cooldown, bool isServer)
    {
        StartCoroutine(AbilityCooldownCoroutine(weapon, cooldown, isServer));
    }

    private IEnumerator AbilityCooldownCoroutine(Weapon weapon, float cooldown, bool isServer)
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

    private void SelectItem(ItemSlotType type)
    {
        switch (type)
        {
            case ItemSlotType.Weapon:
                PlayerUI.UI_PlayerOverlay.SelectItem(0);
                break;
            case ItemSlotType.Pickaxe:
                PlayerUI.UI_PlayerOverlay.SelectItem(1);
                break;
            case ItemSlotType.Axe:
                PlayerUI.UI_PlayerOverlay.SelectItem(2);
                break;
        }
    }
    private void AssignSlot(ItemSlotType type, Weapon weapon)
    {
        switch (type)
        {
            case ItemSlotType.Weapon:
                Weapon = weapon;
                break;
            case ItemSlotType.Pickaxe:
                Pickaxe = weapon;
                break;
            case ItemSlotType.Axe:
                Axe = weapon;
                break;
        }
    }
    private void AssignIcon(ItemSlotType type, int itemID)
    {
        switch (type)
        {
            case ItemSlotType.Weapon:
                PlayerUI.UI_PlayerOverlay.WeaponIcon.enabled = true;
                PlayerUI.UI_PlayerOverlay.WeaponIcon.sprite = Registry.GetItem(itemID).Icon;
                break;
            case ItemSlotType.Pickaxe:
                PlayerUI.UI_PlayerOverlay.PickaxeIcon.enabled = true;
                PlayerUI.UI_PlayerOverlay.PickaxeIcon.sprite = Registry.GetItem(itemID).Icon;
                break;
            case ItemSlotType.Axe:
                PlayerUI.UI_PlayerOverlay.AxeIcon.enabled = true;
                PlayerUI.UI_PlayerOverlay.AxeIcon.sprite = Registry.GetItem(itemID).Icon;
                break;
        }
    }
    private void RemoveIcon(ItemSlotType type)
    {
        switch (type)
        {
            case ItemSlotType.Weapon:
                PlayerUI.UI_PlayerOverlay.WeaponIcon.enabled = false;
                PlayerUI.UI_PlayerOverlay.WeaponIcon.sprite = null;
                break;
            case ItemSlotType.Pickaxe:
                PlayerUI.UI_PlayerOverlay.PickaxeIcon.enabled = false;
                PlayerUI.UI_PlayerOverlay.PickaxeIcon.sprite = null;
                break;
            case ItemSlotType.Axe:
                PlayerUI.UI_PlayerOverlay.AxeIcon.enabled = false;
                PlayerUI.UI_PlayerOverlay.AxeIcon.sprite = null;
                break;
        }

    }

    private Weapon GetSlot(ItemSlotType type)
    {
        switch (type)
        {
            case ItemSlotType.Weapon: return Weapon;
            case ItemSlotType.Pickaxe: return Pickaxe;
            case ItemSlotType.Axe: return Axe;
            default: return null;
        }
    }

    private Weapon GetSlotByIndex(int index)
    {
        switch (index)
        {
            case 0: return Weapon;
            case 1: return Pickaxe;
            case 2: return Axe;
            default: return null;
        }
    }

    private int SlotToIndex(ItemSlotType type)
    {
        switch (type)
        {
            case ItemSlotType.Weapon: return 0;
            case ItemSlotType.Pickaxe: return 1;
            case ItemSlotType.Axe: return 2;
            default: return -1;
        }
    }
}