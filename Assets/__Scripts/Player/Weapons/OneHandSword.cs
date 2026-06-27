using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneHandSword : Weapon
{
    [Header("Settings")]
    [SerializeField] private MeleeHitDetection HitDetection;
    private float Damage;
    private float AttackSpeed;
    private float Resilliance;
    [Space]
    [SerializeField] private float SwingInterval;
    [SerializeField] private float ReturnDuration = 0.2f;
    [SerializeField] private AnimationCurve ReturnEase;

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
    public override void Initalize(PlayerLoadoutModule loadout, int[] materialArray, bool isMainHand)
    {
        base.Initalize(loadout, materialArray, isMainHand);
        HitDetection.Initalize(loadout);
        Loadout.MainHandAnimator.SetBool("OneHandedSword", true);
    }
    public override void Deinitialize()
    {
        Loadout.MainHandAnimator.SetBool("OneHandedSword", false);
    }
    public override void SetStats(int[] materialArray)
    {
        if(materialArray == null)
        {
            AttackSpeed = 0.5f;
            Damage = 5;
            Resilliance = 0;
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

        Loadout.MainHandAnimator.speed = swing.Clip.length / AttackSpeed;
        Loadout.MainHandAnimator.SetTrigger("MH_Attack");

        HitDetection.EnableHitDetection(swing.AttackData, AttackSpeed, IsMainHand, isServer: false);
        Loadout.StartWeaponCooldown(this, AttackSpeed + ReturnDuration, isServer: false);

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

        HitDetection.EnableHitDetection(swing.AttackData, AttackSpeed, IsMainHand, isServer: true);
        Loadout.StartWeaponCooldown(this, AttackSpeed, isServer: true);
        Observer_Attack_RPC(swingIndex);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void Observer_Attack_RPC(int swingIndex)
    {
        //TransformLerp swing = AnimationSwings[swingIndex];

        //if (SwingCoroutine != null) StopCoroutine(SwingCoroutine);
        //SwingCoroutine = StartCoroutine(PerformSwing(swing));
    }

   
    public void ClientHit(GameObject obj, Vector3 hitPos)
    {
        var damageable = obj.GetComponent<DamageableComponent>();
        damageable.TakeDamage(Damage, false);
    }
    public void ServerHit(GameObject obj)
    {
        var damageable = obj.GetComponent<DamageableComponent>();
        damageable.TakeDamage(Damage, true);
    }
}
