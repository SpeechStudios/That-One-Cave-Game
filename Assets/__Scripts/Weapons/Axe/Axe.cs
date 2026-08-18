using FishNet.Object;
using UnityEngine;


public class Axe : Weapon
{
    private AxeData Data;
    public float HitDelay = 0.2f;
    public float HitRadius = 1.5f;
    public float HitDistance = 1f;
    public LayerMask HitLayers;
    private int Resiliance;
    private int ChoppingLevel;
    [SerializeField] internal ShapeHitDetection ShapeHitDetection;


    public override void Initalize(PlayerModule player, int[] materialArray, int index, NetworkRole role)
    {
        Data = WeaponData as AxeData;
        base.Initalize(player, materialArray, index, role);
        if (role == NetworkRole.Observer) return;

        ShapeHitDetection.Initalize(player.Loadout, HitLayers);
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
                        ChoppingLevel = head.ChoppingLevel;
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
        Player.Stats.SetWeaponContribution(TotalWeaponDamage, TotalWeaponAttackSpeed, isServer);
    }
    public override void RemoveStats(bool isServer)
    {
        Player.Stats.SetWeaponContribution(0, 0, isServer);
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;

        Player.Loadout.WeaponAnimator.speed = 1 / Player.Stats.ClientValues.AttackSpeed;
        Player.Loadout.WeaponAnimator.SetTrigger("Attack");

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: false, clientCallback: (Obj, Point) => ClientHit(Obj, Point));
        Player.Loadout.StartWeaponCooldown(this, Player.Stats.ClientValues.AttackSpeed + AttackTolerance, isServer: false);

        Server_Attack_RPC();
    }
    [ServerRpc]
    public void Server_Attack_RPC()
    {
        float Now = TimeManager.Tick * (float)TimeManager.TickDelta;
        if (Now - LastAttackTime < Player.Stats.ServerValues.AttackSpeed - AttackTolerance)
            return;
        LastAttackTime = Now;

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: true, serverCallback: (Obj) => ServerHit(Obj));
        Player.Loadout.StartWeaponCooldown(this, Player.Stats.ServerValues.AttackSpeed + 0.05f, isServer: true);

        Observer_Attack_RPC();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC()
    {
    }

    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var Damageable = obj.GetComponent<WoodNode>();
        Debug.Log("Client Hit Found");
        Damageable.TakeDamage(Player.Stats.ClientValues.Damage, ChoppingLevel, false);
    }
    public void ServerHit(GameObject obj)
    {
        var Damageable = obj.GetComponent<WoodNode>();
        Debug.Log("Server Hit Found");
        Damageable.TakeDamage(Player.Stats.ServerValues.Damage, ChoppingLevel, true);
    }
}