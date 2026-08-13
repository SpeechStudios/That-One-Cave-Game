using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "New Ability/Bow/AshHandle")]
public class Bow_ExplosiveArrowData : AbilityData
{
    public float DamageMultiplier = 1.5f;
    public float ExplosionRadius = 5f;
    public GameObject ExplosionEffectPrefab;
    public override Ability CreateAbility() => new Bow_ExplosiveArrow();
    public override void OnHitFunction(HitContext ctx, bool isServer)
    {
        if(!isServer)
        {
            if (ExplosionEffectPrefab != null)
                Instantiate(ExplosionEffectPrefab, ctx.HitPoint, Quaternion.identity);
        }
        Collider[] hits = Physics.OverlapSphere(ctx.HitPoint, ExplosionRadius);
        HashSet<Transform> hitCharacters = new();

        foreach (var col in hits)
        {
            if (!col.TryGetComponent<IDamageable>(out var explosionDamageable))
                continue;

            Transform characterRoot = col.transform.root;
            if (characterRoot == ctx.Source.root)
                continue;
            if (hitCharacters.Contains(characterRoot))
                continue;

            hitCharacters.Add(characterRoot);
            explosionDamageable.TakeDamage(ctx.TotalDamage * DamageMultiplier, isServer);
        }
    }
}
public class Bow_ExplosiveArrow : Ability
{
    private Bow Bow;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
    }
    public override void ClientActivate(uint tick) 
    {
        Bow.QueueEffect(this, Data.ID, isServer: false);
    }

    public override void ServerActivate(uint tick)
    {
        Bow.QueueEffect(this, Data.ID, isServer: true);
    }
}