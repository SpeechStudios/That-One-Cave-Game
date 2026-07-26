using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
public class Fists : Weapon
{
    [Header("Settings")]
    [SerializeField] private SwingHitDetection HitDetection;
    [SerializeField] private List<SwingData> AnimationSwings;

    private int ServerSwingIndex = 0;
    private int ClientSwingIndex = 0;

    public void OnEnable()
    {
        HitDetection.ClientOnHit += ClientHit;
        HitDetection.ServerOnHit += ServerHit;
    }
    public void OnDisable()
    {
        HitDetection.ClientOnHit -= ClientHit;
        HitDetection.ServerOnHit -= ServerHit;
    }
    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, int[] materialArray)
    {
        base.Initalize(movement, loadout, materialArray);
        HitDetection.Initalize(loadout);
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;
        int SwingIndex = ClientSwingIndex;
        ClientSwingIndex = (ClientSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];
        Loadout.WeaponAnimator.SetTrigger("Attack");
        HitDetection.EnableHitDetection(Swing.AttackData, 0.1f, isServer: false);
        Loadout.StartWeaponCooldown(this, AttackSpeed + 0.05f, isServer: false);
        Server_Attack_RPC();
    }
    [ServerRpc]
    public void Server_Attack_RPC()
    {
        float Now = (float)base.TimeManager.Tick * (float)base.TimeManager.TickDelta;
        if (Now - LastAttackTime < AttackSpeed - AttackTolerance)
            return;
        LastAttackTime = Now;
        int SwingIndex = ServerSwingIndex;
        ServerSwingIndex = (ServerSwingIndex + 1) % AnimationSwings.Count;
        SwingData Swing = AnimationSwings[SwingIndex];
        HitDetection.EnableHitDetection(Swing.AttackData, AttackSpeed, isServer: true);
        Loadout.StartWeaponCooldown(this, AttackSpeed + 0.05f, isServer: true);
        Observer_Attack_RPC(SwingIndex);
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC(int SwingIndex)
    {
    }
    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var Damageable = obj.GetComponent<IDamageable>();
        Damageable.TakeDamage(Damage, false);
    }
    public void ServerHit(GameObject obj)
    {
        var Damageable = obj.GetComponent<IDamageable>();
        Damageable.TakeDamage(Damage, true);
    }
}