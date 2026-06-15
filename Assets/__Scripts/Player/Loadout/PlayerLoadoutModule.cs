using FishNet.Object;
using System.Collections;
using UnityEngine;

public class PlayerLoadoutModule : NetworkBehaviour
{
    public PlayerControllerModule Controller;


    [Header("Loadout")]
    public Weapon MainHand;
    public Weapon OffHand;
    public Weapon Pickaxe;
    public Weapon Axe;
    public Armor Head;
    public Armor Chest;
    public Armor Legs;

    private Camera PlayerCamera;
    private int CurrentWeaponIndex = 1;
    private bool Initalized;
    public void Init()
    {
        PlayerCamera = Camera.main;
        Initalized = true;
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
                MainHand.Attack(this, PlayerCamera);
            }
            if (CurrentWeaponIndex == 1 && Pickaxe != null)
            {
                Pickaxe.Attack(this, PlayerCamera);
            }
            if (CurrentWeaponIndex == 2 && Axe != null)
            {
                Axe.Attack(this, PlayerCamera);
            }
        }
    }
    public void CheckWeaponSwapInputs()
    {
        if(Controller.PlayerInput.Player.Option1.WasPressedThisFrame())
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
        Debug.Log("Updating weapon to index: " + index);
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
    public void EquipNewWeapon(ItemSlotType slotType)
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

    public void StartWeaponCooldown(Weapon weapon)
    {
        StartCoroutine(AttackCooldownCoroutine(weapon));
    }
    public IEnumerator AttackCooldownCoroutine(Weapon weapon)
    {
        yield return new WaitForSecondsRealtime(weapon.AttackCooldown);
        weapon.CanAttack = true;
    }

}
