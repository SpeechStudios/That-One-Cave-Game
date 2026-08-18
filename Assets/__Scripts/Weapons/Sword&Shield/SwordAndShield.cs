using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;


public class SwordAndShield : Weapon
{
    public int TestingHandle;
    public int TestingBlade;

    private SnSData Data;
    [SerializeField] internal SwingHitDetection SwingHitDetection;
    [SerializeField] internal ShapeHitDetection ShapeHitDetection;
    [SerializeField] private float HitDetectionXOffset;
    [SerializeField] private List<SwingData> AnimationSwings;

    internal List<int> ClientPendingBAEffects = new();
    internal List<int> ServerPendingBAEffects = new();

    private float Resilliance;

    private int ServerSwingIndex = 0;
    private int ClientSwingIndex = 0;

    public override void Initalize(PlayerModule player, int[] materialArray, int index, NetworkRole role)
    {
        Data = WeaponData as SnSData;
        base.Initalize(player, materialArray, index, role);
        if (role == NetworkRole.Observer) return;

        player.Loadout.FPCam.MeleeHitPoint.transform.localPosition = new Vector2(HitDetectionXOffset, player.Loadout.FPCam.MeleeHitPoint.transform.localPosition.y);
        SwingHitDetection.Initalize(player.Loadout);
        ShapeHitDetection.Initalize(player.Loadout, Player.Loadout.HitLayers);

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
    public override void GainStats(bool isServer)
    {
        Player.Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed, isServer);
        if(isServer)
        {
            SwingHitDetection.ServerOnHit += ServerHit;
            SwingHitDetection.ServerSwingComplete += ServerSwingEnded;
        }
        else
        {
            SwingHitDetection.ClientOnHit += ClientHit;
            SwingHitDetection.ClientSwingComplete += ClientSwingEnded;
        }
    }
    public override void RemoveStats(bool isServer)
    {
        Player.Stats.SetWeaponContribution(0, 0, isServer);
        SecondaryEAbility.Deinitialize();
        PrimaryQAbility.Deinitialize();
        if (isServer)
        {
            SwingHitDetection.ServerOnHit -= ServerHit;
            SwingHitDetection.ServerSwingComplete -= ServerSwingEnded;
        }
        else
        {
            SwingHitDetection.ClientOnHit -= ClientHit;
            SwingHitDetection.ClientSwingComplete -= ClientSwingEnded;
        }
    }

    public override void AttackRequest()
    {
        if (!ClientCooldown.IsReady || ClientVariables.PrimaryAbility.BlockAttacks || ClientVariables.SecondaryAbility.BlockAttacks)
            return;

        uint currentTick = TimeManager.LocalTick;

        int SwingIndex = ClientSwingIndex;
        ClientSwingIndex = (ClientSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];

        Player.Loadout.WeaponAnimator.speed = Swing.Clip.length / Player.Stats.ClientValues.AttackSpeed;
        Player.Loadout.WeaponAnimator.SetTrigger("Attack");

        SwingHitDetection.EnableHitDetection(Swing.AttackData, Player.Stats.ClientValues.AttackSpeed, isServer: false);
        ClientCooldown.Start(Player.Stats.ClientValues.AttackSpeed + AttackTolerance);
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

        SwingHitDetection.EnableHitDetection(Swing.AttackData, Player.Stats.ServerValues.AttackSpeed, isServer: true);
        ServerCooldown.StartAtTick(clampedTick, Player.Stats.ServerValues.AttackSpeed + AttackTolerance);
        Observer_Attack_RPC(SwingIndex);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC(int swingIndex)
    {
        ClearBAEffects(false);
    }
    public void QueueBAEffect(int abilityID, bool isServer)
    {
        var pendingEffects = isServer ? ServerPendingBAEffects : ClientPendingBAEffects;

        if (!pendingEffects.Remove(abilityID))
            pendingEffects.Add(abilityID);
    }
    public void ClearBAEffects(bool isServer)
    {
        var pendingEffects = isServer ? ServerPendingBAEffects : ClientPendingBAEffects;
        pendingEffects.Clear();
    }
    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        //VFX
        if (obj.TryGetComponent<IDamageable>(out var damageable))
        {
            foreach (var item in ClientPendingBAEffects)
            {
                var data = Registry.GetAbilityData(item);
                data.OnClientHit(hitPos, obj.transform);
            }
        }
    }
    public void ServerHit(GameObject obj, Vector3 hitPoint)
    {
        if (obj.TryGetComponent<IDamageable>(out var damageable))
        { 
            float Damage = Player.Stats.GetDamage();
            foreach (var item in ServerPendingBAEffects)
            {
                var data = Registry.GetAbilityData(item);
                data.OnServerHit(new HitContext { HitPoint = hitPoint, HitEntity = obj.transform, Source = Player }, ref Damage);
            }
            damageable.TakeDamage(Damage);
        }
    }
    public void ClientSwingEnded()
    {
        ClearBAEffects(false);
    }
    public void ServerSwingEnded()
    {
        ClearBAEffects(true);
    }
}