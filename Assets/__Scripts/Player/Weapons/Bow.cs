using FishNet.Object;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class Bow : Weapon
{
    private Camera PlayerCam;
    private float Damage;
    private float ChargeSpeed;
    private float ReloadSpeed;
    private float ArrowVelocity;

    private float CurrentArrowVelocity;
    private float CurrentCharge;
    private bool IsCharging;

    private const float MAX_PASSED_TIME = 0.3f;
    public override void Initalize(PlayerLoadoutModule loadout, int[] materialArray)
    {
        base.Initalize(loadout, materialArray);
        Loadout.RebindAnimator("Bow");
        PlayerCam = Camera.main;
    }
    public override void SetStats(int[] materialArray)
    {
        if (materialArray == null)
        {
            ChargeSpeed = 1f;
            ReloadSpeed = 0.25f;
            Damage = 15;
            ArrowVelocity = 40f;
            return;
        }
        for (int i = 0; i < materialArray.Length; i++)
        {
            MaterialType type = (MaterialType)materialArray[i];

            //First Wood Type
            if (i == 0)
            {
                switch (type)
                {
                    case MaterialType.Birch:
                        break;
                    case MaterialType.Oak:
                        break;
                    case MaterialType.Ash:
                        break;
                    case MaterialType.Phantom:
                        break;
                    case MaterialType.Mantium:
                        break;
                    case MaterialType.Swift:
                        break;
                    default:
                        break;
                }
            }
            //Second Wood Type
            if (i == 1)
            {
                switch (type)
                {
                    case MaterialType.Birch:
                        break;
                    case MaterialType.Oak:
                        break;
                    case MaterialType.Ash:
                        break;
                    case MaterialType.Phantom:
                        break;
                    case MaterialType.Mantium:
                        break;
                    case MaterialType.Swift:
                        break;
                    default:
                        break;
                }
            }
        }
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack) return;
        if (!IsCharging)
            IsCharging = true;

        CurrentCharge = Mathf.Clamp01(CurrentCharge + Time.deltaTime * ChargeSpeed);

        Loadout.WeaponAnimator.SetBool("Aiming", true);
        PlayerCam.fieldOfView = Mathf.Lerp(72f, 72f, CurrentCharge);
    }
    public override void ReleaseRequest()
    {
        if (!IsCharging) return;
        IsCharging = false;
        ClientCanAttack = false;

        Loadout.WeaponAnimator.SetBool("Aiming", false);
        float fov = PlayerCam.fieldOfView;
        LeanTween.value(gameObject, fov, 72f, 0.1f).setOnUpdate((float val) => {  PlayerCam.fieldOfView = val; });

        float chargedVelocity = ArrowVelocity * CurrentCharge;
        float totalDamage = Damage * CurrentCharge;

        Vector3 spawnPos = Loadout.BowFirePoint.position;
        Vector3 aimDir = Loadout.BowFirePoint.forward;

        SpawnArrow(spawnPos, aimDir, totalDamage, chargedVelocity, passedTime: 0f, isServer: false);
        Loadout.StartWeaponCooldown(this, ReloadSpeed + 0.05f, isServer: false);

        uint tick = base.TimeManager.Tick;
        Server_Attack_RPC(spawnPos, aimDir, CurrentCharge, tick);
        CurrentCharge = 0f;
    }
    [ServerRpc]
    public void Server_Attack_RPC(Vector3 position, Vector3 direction, float charge, uint tick)
    {
        if (!ServerCanAttack) return;
        ServerCanAttack = false;

        float passedTime = (float)base.TimeManager.TimePassed(tick, allowNegative: false);
        passedTime = Mathf.Min(MAX_PASSED_TIME / 2f, passedTime);

        direction = direction.normalized;
        charge = Mathf.Clamp01(charge);
        float chargedVelocity = ArrowVelocity * charge;
        float totalDamage = Damage * charge;

        SpawnArrow(position, direction, totalDamage, chargedVelocity, passedTime, true);
        ObserversFireRpc(position, direction, totalDamage, chargedVelocity, tick);

        Loadout.StartWeaponCooldown(this, ReloadSpeed + 0.05f, isServer: true);
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ObserversFireRpc(Vector3 position, Vector3 direction, float damage, float velocity, uint tick)
    {
        float passedTime = (float)base.TimeManager.TimePassed(tick, allowNegative: false);
        passedTime = Mathf.Min(MAX_PASSED_TIME, passedTime);

        SpawnArrow(position, direction, damage, velocity, passedTime, false);
    }
    private void SpawnArrow(Vector3 position, Vector3 direction, float damage, float velocity, float passedTime, bool isServer)
    {
        Arrow arrow = ArrowPoolManager.Instance.Get(position, Quaternion.LookRotation(direction));
        arrow.Initialize(direction, velocity, passedTime, damage, isAuthority: base.IsOwner, isServer: isServer, transform.root);
    }

}
