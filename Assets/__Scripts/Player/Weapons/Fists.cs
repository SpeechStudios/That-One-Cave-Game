using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class Fists : Weapon
{
    [Header("Settings")]
    [SerializeField] private MeleeHitDetection HitDetection;
    private float Damage = 4;
    private float AttackSpeed = 0.6f;


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
    public override void Initalize(PlayerLoadoutModule loadout, int[] materialArray)
    {
        base.Initalize(loadout, materialArray);
        HitDetection.Initalize(loadout);
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;

        int swingIndex = ClientSwingIndex;
        ClientSwingIndex = (ClientSwingIndex + 1) % AnimationSwings.Count;
        SwingData swing = AnimationSwings[swingIndex];

        Loadout.WeaponAnimator.SetTrigger("Attack");

        HitDetection.EnableHitDetection(swing.AttackData, 0.1f, isServer: false);
        Loadout.StartWeaponCooldown(this, AttackSpeed + 0.05f, isServer: false);

        Server_Attack_RPC();
    }
    [ServerRpc]
    public void Server_Attack_RPC()
    {
        if (!ServerCanAttack)
            return;
        ServerCanAttack = false;

        int swingIndex = ServerSwingIndex;
        ServerSwingIndex = (ServerSwingIndex + 1) % AnimationSwings.Count;
        SwingData swing = AnimationSwings[swingIndex];

        HitDetection.EnableHitDetection(swing.AttackData, AttackSpeed, isServer: true);
        Loadout.StartWeaponCooldown(this, AttackSpeed + 0.05f, isServer: true);
        Observer_Attack_RPC(swingIndex);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC(int swingIndex)
    {
    }
    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var damageable = obj.GetComponent<IDamageable>();
        damageable.TakeDamage(Damage, false);
    }
    public void ServerHit(GameObject obj)
    {
        var damageable = obj.GetComponent<IDamageable>();
        damageable.TakeDamage(Damage, true);
    }
}
