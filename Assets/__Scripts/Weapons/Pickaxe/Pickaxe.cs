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


    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, PlayerStatsModule stats, int[] materialArray)
    {
        base.Initalize(movement, loadout, stats, materialArray);
        ShapeHitDetection.Initalize(loadout, HitLayers);
        Data = WeaponData as PickaxeData;
    }

    public override void GainStats()
    {
        if (MaterialArray == null)
        {
            TotalWeaponAttackSpeed = 1f;
            TotalWeaponDamage = 5;
            Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
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
        if(Resiliance < 0)
        {
            TotalWeaponAttackSpeed -= TotalWeaponAttackSpeed / 2 * Resiliance;
        }
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }
    public override void RemoveStats()
    {
        TotalWeaponDamage = 0;
        TotalWeaponAttackSpeed = 0;
        Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed);
    }

    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;
        Loadout.WeaponAnimator.speed = 1 / Stats.GetAttackSpeed();
        Loadout.WeaponAnimator.SetTrigger("Attack");

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: false, clientCallback: (Obj, Point) => ClientHit(Obj, Point));
        Loadout.StartWeaponCooldown(this, Stats.GetAttackSpeed() + 0.05f, isServer: false);

        Server_Attack_RPC();
    }

    [ServerRpc]
    public void Server_Attack_RPC()
    {
        float Now = TimeManager.Tick * (float)TimeManager.TickDelta;
        if (Now - LastAttackTime < Stats.GetAttackSpeed() - AttackTolerance)
            return;
        LastAttackTime = Now;

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: true, serverCallback: (Obj) => ServerHit(Obj));
        Loadout.StartWeaponCooldown(this, Stats.GetAttackSpeed() + 0.05f, isServer: true);

        Observer_Attack_RPC();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC()
    {
    }

    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var Damageable = obj.GetComponent<OreNode>();
        Damageable.TakeDamage(Stats.GetDamage(), MiningLevel, false);
    }

    public void ServerHit(GameObject obj)
    {
        var Damageable = obj.GetComponent<OreNode>();
        Damageable.TakeDamage(Stats.GetDamage(), MiningLevel, true);
    }
}