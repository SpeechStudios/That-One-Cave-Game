using System.Collections;
using UnityEngine;

public class Axe : Weapon
{
    [Header("Settings")]
    [SerializeField] private LayerMask WoodLayer;
    [SerializeField] private float MaxDamage = 50f;
    private float Range = 5f;


    public override void AttackRequest()
    {
        /*
        if (!CanAttack)
            return;

        CanAttack = false;
        Ray ray = new(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Range, WoodLayer))
        {
            WoodNode wood = hit.collider.GetComponent<WoodNode>();
            wood.Server_DamageOre_RPC(MaxDamage);
        }

        // Start cooldown
        Loadout.StartWeaponCooldown(this);
        */
    }
}