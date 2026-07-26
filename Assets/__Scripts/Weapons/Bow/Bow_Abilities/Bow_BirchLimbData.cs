using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/BirchLimb")]
public class Bow_BirchLimbData : AbilityData
{
    public int ArrowCount = 3;
    public float ArrowFireInterval = 0.1f;
    public float DamageMultiplier = 0.75f;
}
public class Bow_BirchLimb : Ability
{
    private const float MAX_PASSED_TIME = 0.3f;

    private Bow Bow;
    private Bow_BirchLimbData BirchLimbData;
    public override System.Type DataType => typeof(Bow_BirchLimbData);

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

    private System.Collections.IEnumerator FireSequence(uint activationTick, bool isServer)
    {
        for (int i = 0; i < BirchLimbData.ArrowCount; i++)
        {
            uint shotTick = activationTick + (uint)(i * Mathf.RoundToInt(BirchLimbData.ArrowFireInterval / (float)Bow.TimeManager.TickDelta));
            FireArrow(shotTick, isServer);

            if (i < BirchLimbData.ArrowCount - 1)
                yield return new WaitForSeconds(BirchLimbData.ArrowFireInterval);
        }
    }

    private void FireArrow(uint shotTick, bool isServer)
    {
        Vector3 spawnPos = Bow.Loadout.BowFirePoint.position;
        Vector3 aimDir = Bow.Loadout.BowFirePoint.forward;
        float velocity = Bow.ArrowVelocity;
        float damage = Bow.Damage * BirchLimbData.DamageMultiplier;

        if (isServer)
        {
            float passedTime = (float)Bow.TimeManager.TimePassed(shotTick, allowNegative: false);
            passedTime = Mathf.Min(MAX_PASSED_TIME, passedTime);

            Bow.SpawnNormalArrow(spawnPos, aimDir, damage, velocity, passedTime, isServer: true);
            Bow.ObserversFireRpc(spawnPos, aimDir, damage, velocity, shotTick);
        }
        else
        {
            Bow.SpawnNormalArrow(spawnPos, aimDir, damage, velocity, passedTime: 0f, isServer: false);
        }
    }
}