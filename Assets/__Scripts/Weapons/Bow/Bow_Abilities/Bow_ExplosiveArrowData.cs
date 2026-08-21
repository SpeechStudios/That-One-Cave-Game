using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "New Ability/Bow/AshHandle")]
public class Bow_ExplosiveArrowData : AbilityData
{
    public float DamageMultiplier = 1.5f;
    public float ExplosionRadius = 5f;
    public GameObject ExplosionEffectPrefab;
    public override Ability CreateAbility() => new Bow_ExplosiveArrow();
    public override void OnClientHit(Vector3 HitPoint, Transform HitEntity)
    {
        if (ExplosionEffectPrefab != null)
            Instantiate(ExplosionEffectPrefab, HitPoint, Quaternion.identity);
    }
    public override void OnServerHit(HitContext ctx, ref float damage)
    {
        Collider[] hits = Physics.OverlapSphere(ctx.HitPoint, ExplosionRadius);
        HashSet<Transform> hitCharacters = new();

        foreach (var col in hits)
        {
            if (!col.TryGetComponent<IDamageable>(out var explosionDamageable))
                continue;

            Transform characterRoot = col.transform.root;
            if (characterRoot == ctx.Source.transform)
                continue;
            if (hitCharacters.Contains(characterRoot))
                continue;
            bool hit = Weapon.ExplosiveLOSCheck(ctx.HitPoint, 0.02f, col, 10f, ctx.Source.Loadout.LOSLayers);
            if (col != hit)
                continue;

            hitCharacters.Add(characterRoot);
            explosionDamageable.TakeDamage(damage * DamageMultiplier);

            foreach (var effect in ctx.Effects)
            {
                if (effect is Bow_PoisonArrowData)
                    effect.OnServerHit(new HitContext { HitEntity = characterRoot, HitPoint = ctx.HitPoint, Source = ctx.Source, Effects = ctx.Effects }, ref damage);
            }

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
    public override (ObserverType, byte[]) ServerActivate(uint tick)
    {
        Bow.QueueEffect(this, Data.ID, isServer: true);
        return default;
    }
}