using FishNet.Object;
using UnityEngine;

public class Pickaxe : Weapon
{
    private PickaxeData Data;
    public float HitDelay = 0.2f;
    public float HitRadius = 1.5f;
    public float HitDistance = 1f;
    public LayerMask HitLayers;
    private int Resiliance;
    private int MiningLevel;
    [SerializeField] internal ShapeHitDetection ShapeHitDetection;


    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray, NetworkRole role)
    {
        Data = WeaponData as PickaxeData;
        base.Initalize(movement, loadout, stats, materialArray, role);
        if (role == NetworkRole.Observer) return;

        ShapeHitDetection.Initalize(loadout, HitLayers);
    }
    public override void InitalizeStats(bool stats, bool abilities)
    {
        if (MaterialArray == null)
        {
            TotalWeaponAttackSpeed = 1f;
            TotalWeaponDamage = 5;
            return;
        }

        for (int i = 0; i < MaterialArray.Length; i++)
        {
            MaterialType type = (MaterialType)MaterialArray[i];
            if (i == 0) // Handle
            {
                foreach (var handle in Data.HandleStats)
                {
                    if (handle.MaterialType == type)
                    {
                        TotalWeaponAttackSpeed = handle.AttackSpeed;
                        TotalWeaponDamage += handle.Damage;
                        Resiliance = handle.Resiliance;
                    }
                }
            }
            if (i == 1) // Head
            {
                foreach (var head in Data.HeadStats)
                {
                    if (head.MaterialType == type)
                    {
                        TotalWeaponDamage += head.Damage;
                        Resiliance += head.Resiliance;
                        MiningLevel = head.MiningLevel;
                    }
                }
            }

        }
        if (Resiliance < 0)
        {
            TotalWeaponAttackSpeed -= TotalWeaponAttackSpeed / 2 * Resiliance;
        }
    }
    public override void GainStats(bool isServer)
    {  
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed, isServer);
    }
    public override void RemoveStats(bool isServer)
    {
        Stats.SetWeaponContribution(0, 0, isServer);
    }

    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;
        Loadout.WeaponAnimator.speed = 1 / Stats.ClientValues.AttackSpeed;
        Loadout.WeaponAnimator.SetTrigger("Attack");

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: false, clientCallback: (Obj, Point) => ClientHit(Obj, Point));
        Loadout.StartWeaponCooldown(this, Stats.ClientValues.AttackSpeed + 0.05f, isServer: false);

        Server_Attack_RPC();
    }

    [ServerRpc]
    public void Server_Attack_RPC()
    {
        float Now = TimeManager.Tick * (float)TimeManager.TickDelta;
        if (Now - LastAttackTime < Stats.ServerValues.AttackSpeed - AttackTolerance)
            return;
        LastAttackTime = Now;

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: true, serverCallback: (Obj) => ServerHit(Obj));
        Loadout.StartWeaponCooldown(this, Stats.ClientValues.AttackSpeed + 0.05f, isServer: true);

        Observer_Attack_RPC();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC()
    {
    }

    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var Damageable = obj.GetComponent<OreNode>();
        Damageable.TakeDamage(Stats.ClientValues.Damage, MiningLevel, false);
    }

    public void ServerHit(GameObject obj)
    {
        var Damageable = obj.GetComponent<OreNode>();
        Damageable.TakeDamage(Stats.ServerValues.Damage, MiningLevel, true);
    }
}