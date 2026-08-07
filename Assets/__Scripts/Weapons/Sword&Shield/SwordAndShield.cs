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
    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray)
    {
        Data = WeaponData as SnSData;
        base.Initalize(movement, loadout, stats, materialArray);
        loadout.FPCam.MeleeHitPoint.transform.localPosition = new Vector2(HitDetectionXOffset, loadout.FPCam.MeleeHitPoint.transform.localPosition.y);
        SwingHitDetection.Initalize(loadout);
        ShapeHitDetection.Initalize(loadout, Loadout.HitLayers);

    }
    public override void GainStats()
    {
        //Testing
        if (MaterialArray == null)
        {
            var handle = Data.HandleStats[TestingHandle];
            var blade = Data.BladeStats[TestingBlade];

            TotalWeaponDamage = handle.Damage;
            TotalWeaponAttackSpeed = handle.AttackSpeed;

            Resilliance = handle.Resiliance;
            PrimaryQAbility = handle.PrimaryQAbility.CreateAbility();
            PrimaryQAbility.Initialize(this, handle.PrimaryQAbility);

            TotalWeaponDamage += blade.Damage;

            Resilliance += blade.Resiliance;
            SecondaryEAbility = blade.SecondaryEAbility.CreateAbility();
            SecondaryEAbility.Initialize(this, blade.SecondaryEAbility);
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
                            TotalWeaponDamage = handle.Damage;
                            TotalWeaponAttackSpeed = handle.AttackSpeed;

                            Resilliance = handle.Resiliance;
                            PrimaryQAbility = handle.PrimaryQAbility.CreateAbility();
                            PrimaryQAbility.Initialize(this, handle.PrimaryQAbility);
                        }
                    }
                }
                if (i == 1)
                {
                    foreach (var blade in Data.BladeStats)
                    {
                        if (blade.MaterialType == type)
                        {
                            TotalWeaponDamage += blade.Damage;

                            Resilliance += blade.Resiliance;
                            SecondaryEAbility = blade.SecondaryEAbility.CreateAbility();
                            SecondaryEAbility.Initialize(this, blade.SecondaryEAbility);
                        }
                    }
                }
            }
        }
        if (Resilliance < 0)
        {
            TotalWeaponAttackSpeed -= Resilliance * 0.1f;
        }
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void RemoveStats()
    {
        TotalWeaponDamage = 0;
        TotalWeaponAttackSpeed = 0;
        Resilliance = 0;
        SecondaryEAbility.Deinitialize();
        PrimaryQAbility.Deinitialize();
        SecondaryEAbility = null;
        PrimaryQAbility = null;
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack || ClientBlockAttacks)
            return;
        ClientCanAttack = false;

        int SwingIndex = ClientSwingIndex;
        ClientSwingIndex = (ClientSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];

        Loadout.WeaponAnimator.speed = Swing.Clip.length / Stats.GetAttackSpeed();
        Loadout.WeaponAnimator.SetTrigger("Attack");

        SwingHitDetection.EnableHitDetection(Swing.AttackData, Stats.GetAttackSpeed(), isServer: false);
        Loadout.StartWeaponCooldown(this, Stats.GetAttackSpeed() + AttackTolerance, isServer: false);

        Server_Attack_RPC();
    }
    [ServerRpc]
    public void Server_Attack_RPC()
    {
        float Now = TimeManager.Tick * (float)TimeManager.TickDelta;
        if (Now - LastAttackTime < Stats.GetAttackSpeed() - AttackTolerance || ServerBlockAttacks)
            return;
        LastAttackTime = Now;

        int SwingIndex = ServerSwingIndex;
        ServerSwingIndex = (ServerSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];

        SwingHitDetection.EnableHitDetection(Swing.AttackData, Stats.GetAttackSpeed(), isServer: true);
        Loadout.StartWeaponCooldown(this, Stats.GetAttackSpeed() + AttackTolerance, isServer: true);
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