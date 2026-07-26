using FishNet.Object;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class Bow : Weapon
{
    private Camera PlayerCam;
    private float ChargeSpeed;
    private float ReloadSpeed;
    internal float ArrowVelocity;

    private float CurrentCharge;
    private bool IsCharging;


    private IArrowEffect ClientPendingEffect;
    private Dictionary<string, float> ClientPendingCrits = new();

    private IArrowEffect ServerPendingEffect;
    private Dictionary<string, float> ServerPendingCrits = new();

    private const float MAX_PASSED_TIME = 0.3f;
    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, int[] materialArray)
    {
        base.Initalize(movement, loadout, materialArray);
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
            PrimaryQAbility = AbilityFactory.Create<Bow_OakLimb>(this);
            SecondaryEAbility = AbilityFactory.Create<Bow_OakHandle>(this);
            return;
        }
        for (int i = 0; i < materialArray.Length; i++)
        {
            MaterialType type = (MaterialType)materialArray[i];
            ReloadSpeed = 0.25f;

            if (i == 0) // Limb
            {
                switch (type)
                {
                    case MaterialType.Birch:
                        ChargeSpeed = 1f;
                        Damage = 10;
                        PrimaryQAbility = AbilityFactory.Create<Bow_BirchLimb>(this);
                        break;
                    case MaterialType.Oak:
                        ChargeSpeed = 1.2f;
                        Damage = 15;
                        PrimaryQAbility = AbilityFactory.Create<Bow_OakLimb>(this);
                        break;
                    case MaterialType.Ash:
                        ChargeSpeed = 0.8f;
                        Damage = 18;
                        PrimaryQAbility = AbilityFactory.Create<Bow_AshLimb>(this);
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
            if (i == 1) // Handle
            {
                switch (type)
                {
                    case MaterialType.Birch:
                        ArrowVelocity = 40f;
                        SecondaryEAbility = AbilityFactory.Create<Bow_BirchHandle>(this);
                        break;
                    case MaterialType.Oak:
                        Damage += 1;
                        ArrowVelocity = 50f;
                        SecondaryEAbility = AbilityFactory.Create<Bow_OakHandle>(this);
                        break;
                    case MaterialType.Ash:
                        Damage += 2;
                        ChargeSpeed -= 0.1f;
                        ReloadSpeed -= 0.05f;
                        ArrowVelocity = 60f;
                        SecondaryEAbility = AbilityFactory.Create<Bow_AshHandle>(this);
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
        float Fov = PlayerCam.fieldOfView;
        LeanTween.value(gameObject, Fov, 72f, 0.1f).setOnUpdate((float Val) => { PlayerCam.fieldOfView = Val; });

        float ChargedVelocity = ArrowVelocity * CurrentCharge;
        float TotalDamage = Damage * CurrentCharge;

        Vector3 SpawnPos = Loadout.BowFirePoint.position;
        Vector3 AimDir = Loadout.BowFirePoint.forward;

        SpawnArrow(SpawnPos, AimDir, TotalDamage, ChargedVelocity, 0f, false);
        Loadout.StartWeaponCooldown(this, ReloadSpeed + 0.05f, isServer: false);

        uint Tick = TimeManager.Tick;
        Server_Attack_RPC(SpawnPos, AimDir, CurrentCharge, Tick);
        CurrentCharge = 0f;
    }
    [ServerRpc]
    public void Server_Attack_RPC(Vector3 position, Vector3 direction, float charge, uint tick)
    {
        float Now = (float)base.TimeManager.Tick * (float)base.TimeManager.TickDelta;
        if (Now - LastAttackTime < ReloadSpeed - AttackTolerance)
            return;
        LastAttackTime = Now;

        float PassedTime = (float)TimeManager.TimePassed(tick, allowNegative: false);
        PassedTime = Mathf.Min(MAX_PASSED_TIME / 2f, PassedTime);

        direction = direction.normalized;
        charge = Mathf.Clamp01(charge);
        float ChargedVelocity = ArrowVelocity * charge;
        float TotalDamage = Damage * charge;

        SpawnArrow(position, direction, TotalDamage, ChargedVelocity, PassedTime, true);
        ObserversFireRpc(position, direction, TotalDamage, ChargedVelocity, tick);

        Loadout.StartWeaponCooldown(this, ReloadSpeed + 0.05f, isServer: true);
    }
    [ObserversRpc(ExcludeOwner = true)]
    public void ObserversFireRpc(Vector3 position, Vector3 direction, float damage, float velocity, uint tick)
    {
        float PassedTime = (float)base.TimeManager.TimePassed(tick, allowNegative: false);
        PassedTime = Mathf.Min(MAX_PASSED_TIME, PassedTime);

        SpawnArrow(position, direction, damage, velocity, PassedTime, false);
    }

    public void QueueEffect(IArrowEffect effect, bool isServer) 
    { 
        if (isServer)
            ServerPendingEffect = effect;
        else 
            ClientPendingEffect = effect;
    }
    public void QueueCrit(string source, float multiplier, bool isServer)
    {
        var Dict = isServer ? ServerPendingCrits : ClientPendingCrits;
        Dict[source] = multiplier;
    }
    public void DequeueCrit(string source, bool isServer)
    {
        var Dict = isServer ? ServerPendingCrits : ClientPendingCrits;
        Dict.Remove(source);
    }
    private float GetPendingCritMultiplier(bool isServer)
    {
        var Dict = isServer ? ServerPendingCrits : ClientPendingCrits;
        float Total = 1f;
        foreach (var Value in Dict.Values) Total *= Value;
        return Total;
    }

    public void SpawnArrow(Vector3 position, Vector3 direction, float damage, float velocity, float passedTime, bool isServer)
    {
        Arrow ArrowInstance = ArrowPoolManager.Instance.Get(position, Quaternion.LookRotation(direction));
        float CritMultiplier = GetPendingCritMultiplier(isServer);
        ArrowInstance.Initialize(this, direction, velocity, passedTime, damage * CritMultiplier, isServer, transform.root, ServerPendingEffect);
        if (isServer)
        {
            ServerPendingEffect = null;
            ServerPendingCrits.Clear();
        }
        else
        {
            ClientPendingEffect = null;
            ClientPendingCrits.Clear();
        }
    }
    public void SpawnNormalArrow(Vector3 position, Vector3 direction, float damage, float velocity, float passedTime, bool isServer)
    {
        Arrow ArrowInstance = ArrowPoolManager.Instance.Get(position, Quaternion.LookRotation(direction));
        ArrowInstance.Initialize(this, direction, velocity, passedTime, damage, isServer, transform.root, null);
    }

}