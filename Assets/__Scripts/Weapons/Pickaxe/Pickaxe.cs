using FishNet.Object;
using UnityEngine;

public class Pickaxe : Weapon
{
    public float HitDelay = 0.2f;
    public float HitRadius = 1.5f;
    public float HitDistance = 1f;

    public LayerMask HitLayers;
    [SerializeField] internal ShapeHitDetection ShapeHitDetection;


    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, int[] materialArray)
    {
        base.Initalize(movement, loadout, materialArray);
        ShapeHitDetection.Initalize(loadout, HitLayers);
        Loadout.RebindAnimator("Pickaxe");
    }

    public override void SetStats(int[] materialArray)
    {
        if (materialArray == null)
        {
            AttackSpeed = 1f;
            Damage = 5;
            return;
        }
        for (int i = 0; i < materialArray.Length; i++)
        {
            MaterialType type = (MaterialType)materialArray[i];

            if (i == 0)
            {
                switch (type)
                {
                    case MaterialType.Birch:
                        AttackSpeed = 0.5f;
                        Damage = 0;
                        break;
                    case MaterialType.Oak:
                        AttackSpeed = 0.6f;
                        Damage = 2;
                        break;
                    case MaterialType.Ash:
                        AttackSpeed = 0.4f;
                        Damage = 0;
                        break;
                    case MaterialType.Phantom:
                        AttackSpeed = 0.3f;
                        Damage = 4;
                        break;
                    case MaterialType.Mantium:
                        AttackSpeed = 0.4f;
                        Damage = 6;
                        break;
                    case MaterialType.Swift:
                        AttackSpeed = 0.2f;
                        Damage = 2;
                        break;
                    default:
                        break;
                }
            }
            if (i == 1)
            {
                switch (type)
                {
                    case MaterialType.Bronze:
                        Damage += 5;
                        break;
                    case MaterialType.Steel:
                        Damage += 9;
                        SecondaryEAbility = AbilityFactory.Create<SnS_SteelBlade>(this);
                        break;
                    case MaterialType.Mithril:
                        Damage += 16;
                        SecondaryEAbility = AbilityFactory.Create<SnS_MithrilBlade>(this);
                        break;
                    case MaterialType.Solsteel:
                        Damage += 23;
                        break;
                    case MaterialType.Brimsteel:
                        Damage += 27;
                        break;
                    case MaterialType.Swiftsteel:
                        Damage += 16;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;

        Loadout.WeaponAnimator.speed = 1 / AttackSpeed;
        Loadout.WeaponAnimator.SetTrigger("Attack");

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: false, clientCallback: (Obj, Point) => ClientHit(Obj, Point));
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

        Vector3 Origin = transform.position + transform.forward * HitDistance;
        ShapeHitDetection.TriggerSphere(Origin, HitDelay, HitRadius, isServer: true, serverCallback: (Obj) => ServerHit(Obj));
        Loadout.StartWeaponCooldown(this, AttackSpeed + 0.05f, isServer: true);

        Observer_Attack_RPC();
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC()
    {
    }

    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var Damageable = obj.GetComponent<OreNode>();
        Damageable.TakeDamage(Damage, false);
    }

    public void ServerHit(GameObject obj)
    {
        var Damageable = obj.GetComponent<OreNode>();
        Damageable.TakeDamage(Damage, true);
    }
}