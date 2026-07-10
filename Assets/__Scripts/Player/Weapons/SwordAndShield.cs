using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAndShield : Weapon
{
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
    public override void Initalize(PlayerControllerModule movement, PlayerLoadoutModule loadout, int[] materialArray)
    {
        base.Initalize(movement, loadout, materialArray);
        loadout.MeleeHitDetectionRoot.transform.localPosition = new Vector2(HitDetectionXOffset, loadout.MeleeHitDetectionRoot.transform.localPosition.y);
        SwingHitDetection.Initalize(loadout);
        ShapeHitDetection.Initalize(loadout);
        Loadout.RebindAnimator("SwordAndShield");
    }
    public override void Deinitialize()
    {
        Loadout.WeaponAnimator.SetBool("SwordAndShield", false);
    }
    public override void SetStats(int[] materialArray)
    {
        if(materialArray == null)
        {
            AttackSpeed = 0.5f;
            Damage = 5;
            Resilliance = 0;
            PrimaryQAbility = new SnS_AshHandle();
            PrimaryQAbility.Initialize(this);
            SecondaryEAbility = new SnS_BirchHandle();
            SecondaryEAbility.Initialize(this);
            return;
        }
        for (int i = 0; i < materialArray.Length; i++)
        {
            MaterialType type = (MaterialType)materialArray[i];

            //Wood Type
            if (i == 0)
            {
                switch (type)
                {
                    case MaterialType.Birch:
                        AttackSpeed = 0.5f;
                        Damage = 0;
                        Resilliance = 0;
                        break;
                    case MaterialType.Oak:
                        AttackSpeed = 0.6f;
                        Damage = 2;
                        Resilliance = 1;
                        break;
                    case MaterialType.Ash:
                        AttackSpeed = 0.4f;
                        Damage = 0;
                        Resilliance = 2;
                        break;
                    case MaterialType.Phantom:
                        AttackSpeed = 0.3f;
                        Damage = 4;
                        Resilliance = 3;
                        break;
                    case MaterialType.Mantium:
                        AttackSpeed = 0.4f;
                        Damage = 6;
                        Resilliance = 4;
                        break;
                    case MaterialType.Swift:
                        AttackSpeed = 0.2f;
                        Damage = 2;
                        Resilliance = 2;
                        break;
                    default:
                        break;
                }
            }
            //Metal Type
            if (i == 1)
            {
                switch (type)
                {
                    case MaterialType.Bronze:
                        Damage += 5;
                        break;
                    case MaterialType.Steel:
                        Damage += 9;
                        Resilliance -= 1;
                        break;
                    case MaterialType.Mithril:
                        Damage += 16;
                        Resilliance -= 2;
                        break;
                    case MaterialType.Solsteel:
                        Damage += 23;
                        Resilliance -= 3;
                        break;
                    case MaterialType.Brimsteel:
                        Damage += 27;
                        Resilliance -= 4;
                        break;
                    case MaterialType.Swiftsteel:
                        Damage += 16;
                        Resilliance -= 1;
                        break;
                    default:
                        break;
                }
            }
        }
        if (Resilliance < 0)
        {
            AttackSpeed += -Resilliance * 0.1f;
        }
    }
    public override void AttackRequest()
    {
        if (!ClientCanAttack)
            return;
        ClientCanAttack = false;

        int swingIndex = ClientSwingIndex;
        ClientSwingIndex = (ClientSwingIndex + 1) % AnimationSwings.Count;
        SwingData swing = AnimationSwings[swingIndex];

        Loadout.WeaponAnimator.speed = swing.Clip.length / AttackSpeed;
        Loadout.WeaponAnimator.SetTrigger("Attack");

        SwingHitDetection.EnableHitDetection(swing.AttackData,AttackSpeed,isServer: false);
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

        SwingHitDetection.EnableHitDetection(swing.AttackData, AttackSpeed, isServer: true);
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
