using UnityEngine;
using System.Collections;

public class Pickaxe : Weapon
{
    [Header("Settings")]
    [SerializeField] private LayerMask OreLayer;
    [SerializeField] private float MaxDamage = 50f;
    [SerializeField] private float MinCooldown = 0.3f;
    private float Range = 5f;
    public override void AttackRequest()
    {
        /*
        if (!CanAttack)
            return;

        CanAttack = false;
        Ray ray = new (cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Range, OreLayer))
        {
            OreNode ore = hit.collider.GetComponent<OreNode>();
            ore.Server_DamageOre_RPC(MaxDamage);
        }
        loadout.StartWeaponCooldown(this);
        */
    }
}
