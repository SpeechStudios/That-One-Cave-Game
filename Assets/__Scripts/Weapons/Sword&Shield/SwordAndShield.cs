using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;


public class SwordAndShield : Weapon
{
    public int TestingHandle;
    public int TestingBlade;

    private SnSData Data;
    [SerializeField] internal SwingHitDetection SwingHitDetection;
    [SerializeField] internal ShapeHitDetection ShapeHitDetection;
    [SerializeField] private float HitDetectionXOffset;
    [SerializeField] private List<SwingData> AnimationSwings;

    private float Resilliance;

    private int ServerSwingIndex = 0;
    private int ClientSwingIndex = 0;

    public void OnEnable()
    {
        SwingHitDetection.ClientOnHit += ClientHit;
        SwingHitDetection.ServerOnHit += ServerHit;
    }
    public void OnDisable()
    {
        SwingHitDetection.ClientOnHit -= ClientHit;
        SwingHitDetection.ServerOnHit -= ServerHit;
    }
    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray, NetworkRole role)
    {
        Data = WeaponData as SnSData;
        base.Initalize(movement, loadout, stats, materialArray, role);
        if (role == NetworkRole.Observer) return;

        loadout.FPCam.MeleeHitPoint.transform.localPosition = new Vector2(HitDetectionXOffset, loadout.FPCam.MeleeHitPoint.transform.localPosition.y);
        SwingHitDetection.Initalize(loadout);
        ShapeHitDetection.Initalize(loadout, Loadout.HitLayers);

    }
    public override void InitalizeStats(bool stats, bool abilties)
    {
        if (MaterialArray == null)
        {
            var handle = Data.HandleStats[TestingHandle];
            var blade = Data.BladeStats[TestingBlade];
            if (stats)
            {
                TotalWeaponDamage = handle.Damage;
                TotalWeaponAttackSpeed = handle.AttackSpeed;
                Resilliance = handle.Resiliance;
                TotalWeaponDamage += blade.Damage;
                Resilliance += blade.Resiliance;
            }
            if (abilties)
            {
                PrimaryQAbility = handle.PrimaryQAbility.CreateAbility();
                PrimaryQAbility.Initialize(this, handle.PrimaryQAbility);
                SecondaryEAbility = blade.SecondaryEAbility.CreateAbility();
                SecondaryEAbility.Initialize(this, blade.SecondaryEAbility);
            }
        }
        else
        {
            for (int i = 0; i < MaterialArray.Length; i++)
            {
                MaterialType type = (MaterialType)MaterialArray[i];

                if (i == 0)
                {
                    foreach (var handle in Data.HandleStats)
                    {
                        if (handle.MaterialType == type)
                        {
                            if (stats)
                            {
                                TotalWeaponDamage = handle.Damage;
                                TotalWeaponAttackSpeed = handle.AttackSpeed;
                                Resilliance = handle.Resiliance;
                            }
                            if (abilties)
                            {
                                PrimaryQAbility = handle.PrimaryQAbility.CreateAbility();
                                PrimaryQAbility.Initialize(this, handle.PrimaryQAbility);
                            }
                        }
                    }
                }
                if (i == 1)
                {
                    foreach (var blade in Data.BladeStats)
                    {
                        if (blade.MaterialType == type)
                        {
                            if (stats)
                            {
                                TotalWeaponDamage += blade.Damage;
                                Resilliance += blade.Resiliance;
                            }
                            if (abilties)
                            {
                                SecondaryEAbility = blade.SecondaryEAbility.CreateAbility();
                                SecondaryEAbility.Initialize(this, blade.SecondaryEAbility);
                            }
                        }
                    }
                }
            }
        }
        if (stats)
        {
            if (Resilliance < 0)
            {
                TotalWeaponAttackSpeed -= Resilliance * 0.1f;
            }
        }
    }
    public override void GainStats()
    {
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void RemoveStats()
    {
        Stats.SetWeaponContribution(0, 0);
        SecondaryEAbility.Deinitialize();
        PrimaryQAbility.Deinitialize();
    }

    public override void AttackRequest()
    {
        if (!ClientCooldown.IsReady || ClientVariables.PrimaryAbility.BlockAttacks || ClientVariables.SecondaryAbility.BlockAttacks)
            return;

        uint currentTick = TimeManager.LocalTick;

        int SwingIndex = ClientSwingIndex;
        ClientSwingIndex = (ClientSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];

        Loadout.WeaponAnimator.speed = Swing.Clip.length / Stats.GetAttackSpeed();
        Loadout.WeaponAnimator.SetTrigger("Attack");

        SwingHitDetection.EnableHitDetection(Swing.AttackData, Stats.GetAttackSpeed(), isServer: false);
        ClientCooldown.Start(Stats.GetAttackSpeed() + AttackTolerance);

        Server_Attack_RPC(currentTick);
    }

    [ServerRpc]
    public void Server_Attack_RPC(uint tick)
    {
        if (!ServerCooldown.IsReady || ServerVariables.PrimaryAbility.BlockAttacks || ServerVariables.SecondaryAbility.BlockAttacks)
            return;

        uint serverTick = TimeManager.LocalTick;
        uint clampedTick = tick > serverTick ? serverTick : tick;
        if (serverTick - clampedTick > MAX_TICK_DELAY)
            return;

        int SwingIndex = ServerSwingIndex;
        ServerSwingIndex = (ServerSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];

        SwingHitDetection.EnableHitDetection(Swing.AttackData, Stats.GetAttackSpeed(), isServer: true);
        ServerCooldown.StartAtTick(clampedTick, Stats.GetAttackSpeed() + AttackTolerance);
        Observer_Attack_RPC(SwingIndex);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC(int swingIndex)
    {
    }

    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var Damageable = obj.GetComponent<IDamageable>();
        Damageable.TakeDamage(Stats.GetDamage(), false);
    }
    public void ServerHit(GameObject obj)
    {
        var Damageable = obj.GetComponent<IDamageable>();
        Damageable.TakeDamage(Stats.GetDamage(), true);
    }
}