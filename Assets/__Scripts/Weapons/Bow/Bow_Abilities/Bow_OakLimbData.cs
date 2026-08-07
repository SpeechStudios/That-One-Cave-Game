using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/OakLimb")]
public class Bow_OakLimbData : AbilityData
{
    public int ArrowCount = 10;
    public float DamageMultiplier = 0.3f;
    public float XSpreadAngle = 30f;
    public float YSpreadAngle = 10f;
    public float FireDelay = 1f;
    public float RandomSpreadJitter = 3f;
    public override Ability CreateAbility() => new Bow_OakLimb();
}

public class Bow_OakLimb : Ability
{
    private const float MAX_PASSED_TIME = 0.3f;
    private Bow Bow;
    private Bow_OakLimbData OakLimbData;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        OakLimbData = data as Bow_OakLimbData;
    }

    public override void ClientActivate(uint tick)
    {
        Weapon.StartCoroutine(ClientDelayedFire());
        Weapon.Loadout.WeaponAnimator.SetTrigger("UnloadQuiver");
    }

    private System.Collections.IEnumerator ClientDelayedFire()
    {
        yield return new WaitForSeconds(OakLimbData.FireDelay);
        uint fireTick = Bow.TimeManager.LocalTick;
        FireVolley(fireTick, passedTime: 0f, isServer: false);
        CompleteAbility();
    }

    public override void ServerActivate(uint tick)
    {
        float latency = (float)Bow.TimeManager.TimePassed(tick, allowNegative: false);
        float delayRemaining = Mathf.Max(0f, OakLimbData.FireDelay - latency);
        float arrowPassedTime = Mathf.Clamp(latency - OakLimbData.FireDelay, 0f, MAX_PASSED_TIME);
        Weapon.StartCoroutine(ServerDelayedFire(tick, delayRemaining, arrowPassedTime));
    }

    private System.Collections.IEnumerator ServerDelayedFire(uint activationTick, float delayRemaining, float arrowPassedTime)
    {
        if (delayRemaining > 0f)
            yield return new WaitForSeconds(delayRemaining);
        uint fireTick = Bow.TimeManager.LocalTick;
        FireVolley(fireTick, arrowPassedTime, isServer: true);
        CompleteAbility();
    }

    private void FireVolley(uint tick, float passedTime, bool isServer)
    {
        int arrowsPerRow = Mathf.Max(1, OakLimbData.ArrowCount / 2);
        for (int row = 0; row < 2; row++)
        {
            for (int i = 0; i < arrowsPerRow; i++)
            {
                Vector3 dir = GetSpreadDirection(row, i, arrowsPerRow, isServer);
                FireArrow(dir, tick, passedTime, isServer);
            }
        }
    }

    private Vector3 GetSpreadDirection(int row, int indexInRow, int arrowsPerRow, bool isServer)
    {
        Transform firePoint =isServer ? Bow.Loadout.FPCam.ServerFirePoint : Bow.Loadout.FPCam.ClientFirePoint;
        Vector3 baseDir = firePoint.forward;
        Vector3 up = firePoint.up;
        Vector3 right = firePoint.right;

        float horizontalT = arrowsPerRow > 1 ? (float)indexInRow / (arrowsPerRow - 1) : 0.5f;
        float horizontalAngle = Mathf.Lerp(-OakLimbData.XSpreadAngle * 0.5f, OakLimbData.XSpreadAngle * 0.5f, horizontalT);
        float verticalAngle = row == 0 ? OakLimbData.YSpreadAngle * 0.5f : -OakLimbData.YSpreadAngle * 0.5f;

        // Add random jitter on top of the deterministic grid position
        horizontalAngle += Random.Range(-OakLimbData.RandomSpreadJitter, OakLimbData.RandomSpreadJitter);
        verticalAngle += Random.Range(-OakLimbData.RandomSpreadJitter, OakLimbData.RandomSpreadJitter);

        Quaternion rot = Quaternion.AngleAxis(horizontalAngle, up) * Quaternion.AngleAxis(verticalAngle, right);
        return rot * baseDir;
    }

    private void FireArrow(Vector3 aimDir, uint shotTick, float passedTime, bool isServer)
    {
        Vector3 spawnPos = isServer? Bow.Loadout.FPCam.ServerFirePoint.position : Bow.Loadout.FPCam.ClientFirePoint.position;
        float velocity = Bow.ArrowVelocity;
        float damage = Bow.Stats.GetDamage() * OakLimbData.DamageMultiplier;
        if (isServer)
        {
            Bow.SpawnNormalArrow(spawnPos, aimDir, damage, velocity, passedTime, isServer: true);
            Bow.ObserversFireRpc(damage, velocity, shotTick);
        }
        else
        {
            Bow.SpawnNormalArrow(spawnPos, aimDir, damage, velocity, passedTime: 0f, isServer: false);
        }
    }
}