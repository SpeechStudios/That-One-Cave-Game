using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "New Ability/Bow/BirchLimb")]
public class Bow_BirchLimbData : AbilityData
{
    public int ArrowCount = 3;
    public float ArrowFireInterval = 0.1f;
    public float DamageMultiplier = 0.75f;
    public override Ability CreateAbility() => new Bow_BirchLimb();
}
public class Bow_BirchLimb : Ability
{
    private const float MAX_PASSED_TIME = 0.3f;

    private Bow Bow;
    private Bow_BirchLimbData BirchLimbData;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        BirchLimbData = data as Bow_BirchLimbData;
    }

    public override void ClientActivate(uint tick)
    {
        Bow.StartCoroutine(FireSequence(tick, isServer: false));
    }

    public override void ServerActivate(uint tick)
    {
        Bow.StartCoroutine(FireSequence(tick, isServer: true));
    }

    private IEnumerator FireSequence(uint activationTick, bool isServer)
    {
        for (int i = 0; i < BirchLimbData.ArrowCount; i++)
        {
            uint shotTick = activationTick + (uint)(i * Mathf.RoundToInt(BirchLimbData.ArrowFireInterval / (float)Bow.TimeManager.TickDelta));
            FireArrow(shotTick, isServer);

            if (i < BirchLimbData.ArrowCount - 1)
                yield return new WaitForSeconds(BirchLimbData.ArrowFireInterval);
        }
        CompleteAbility();
    }

    private void FireArrow(uint shotTick, bool isServer)
    {
        Transform firePoint = isServer ? Bow.Loadout.FPCam.ServerFirePoint : Bow.Loadout.FPCam.ClientFirePoint;
        Vector3 spawnPos = firePoint.position;
        Vector3 aimDir = firePoint.forward;
        float velocity = Bow.ArrowVelocity;
        float damage = Bow.Stats.GetDamage() * BirchLimbData.DamageMultiplier;

        if (isServer)
        {
            float passedTime = (float)Bow.TimeManager.TimePassed(shotTick, allowNegative: false);
            passedTime = Mathf.Min(MAX_PASSED_TIME, passedTime);

            Bow.SpawnNormalArrow(spawnPos, aimDir, damage, velocity, passedTime, isServer: true);
            Bow.ObserversFireRpc(damage, velocity, shotTick);
        }
        else
        {
            Bow.SpawnNormalArrow(spawnPos, aimDir, damage, velocity, passedTime: 0f, isServer: false);
        }
    }
}