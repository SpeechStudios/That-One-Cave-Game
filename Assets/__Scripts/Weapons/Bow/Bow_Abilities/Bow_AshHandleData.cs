using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/AshHandle")]
public class Bow_AshHandleData : AbilityData
{
    public float DamageMultiplier = 1.5f;
    public float ExplosionRadius = 3f;
    public GameObject ExplosionEffectPrefab;
}
public class Bow_AshHandle : Ability
{
    private Bow Bow;
    private Bow_AshHandleData AshHandleData;
    public override System.Type DataType => typeof(Bow_AshHandleData);

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        AshHandleData = data as Bow_AshHandleData;
    }
    public override void ClientActivate(uint tick) 
    {
        Bow.QueueEffect(new ExplosiveEffect(AshHandleData.DamageMultiplier, AshHandleData.ExplosionRadius, AshHandleData.ExplosionEffectPrefab), isServer: false);
    }

    public override void ServerActivate(uint tick)
    {
        Bow.QueueEffect(new ExplosiveEffect(AshHandleData.DamageMultiplier, AshHandleData.ExplosionRadius), isServer: true);
    }
}
public class ExplosiveEffect : IArrowEffect
{
    private readonly float Radius;
    private readonly float DamageMultiplier;
    private GameObject ExplosionEffectPrefab;

    public ExplosiveEffect(float damageMultiplier, float radius, GameObject explosionEffectPrefab = default)
    {
        DamageMultiplier = damageMultiplier;
        Radius = radius;
        ExplosionEffectPrefab = explosionEffectPrefab;
    }

    public void OnHit(Weapon source, GameObject hitObject, Vector3 hitPoint, bool isServer, Arrow arrow)
    {
        if (!isServer)
        {
            if (ExplosionEffectPrefab != null)
            {
                GameObject explosionEffect = GameObject.Instantiate(ExplosionEffectPrefab, hitPoint, Quaternion.identity);
                explosionEffect.transform.localScale = new Vector3(Radius, Radius, Radius);
            }
            return;
        }

        Collider[] hits = Physics.OverlapSphere(hitPoint, Radius);
        HashSet<Transform> hitCharacters = new();

        foreach (var col in hits)
        {
            if (!col.TryGetComponent<IDamageable>(out var explosionDamageable))
                continue;

            Transform characterRoot = col.transform.root;
            if (characterRoot == source.transform.root)
                continue;
                if (hitCharacters.Contains(characterRoot))
                continue;

            hitCharacters.Add(characterRoot);
            explosionDamageable.TakeDamage(source.Damage * DamageMultiplier, isServer);
        }
    }
}
