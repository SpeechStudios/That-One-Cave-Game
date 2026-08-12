using FishNet.Connection;
using FishNet.Managing.Timing;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/UnloadQuiver")]
public class Bow_UnloadQuiverData : AbilityData
{
    public int ArrowCount = 10;
    public float DamageMultiplier = 0.3f;
    public float XSpreadAngle = 30f;
    public float YSpreadAngle = 10f;
    public float FireDelay = 1f;
    public float RandomSpreadJitter = 3f;
    public override Ability CreateAbility() => new Bow_UnloadQuiver();
}

public class Bow_UnloadQuiver : Ability
{
    private Bow Bow;
    private Bow_UnloadQuiverData UnloadQuiverData;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        UnloadQuiverData = data as Bow_UnloadQuiverData;
    }

    public override void ClientActivate(uint tick)
    {
        Weapon.StartCoroutine(ClientDelayedFire());
        Weapon.Loadout.WeaponAnimator.SetTrigger("UnloadQuiver");
    }

    private IEnumerator ClientDelayedFire()
    {
        yield return new WaitForSeconds(UnloadQuiverData.FireDelay);
        uint fireTick = Bow.TimeManager.LocalTick;
        FireVolley(fireTick, passedTime: 0f, isServer: false);
        Weapon.AbilityComplete(this, false);
    }

    public override void ServerActivate(uint tick)
    {
        uint serverTick = Bow.TimeManager.LocalTick;
        uint clampedTick = tick > serverTick ? serverTick : tick;
        if (serverTick - clampedTick > Bow.MAX_TICK_DELAY)
            return;

        float latency = (float)Bow.TimeManager.TimePassed(clampedTick, allowNegative: false);
        float maxPassedTimeSeconds = Bow.MAX_TICK_DELAY * (float)Bow.TimeManager.TickDelta;

        float delayRemaining = Mathf.Max(0f, UnloadQuiverData.FireDelay - latency);
        float arrowPassedTime = Mathf.Clamp(latency - UnloadQuiverData.FireDelay, 0f, maxPassedTimeSeconds);

        Weapon.StartCoroutine(ServerDelayedFire(tick, delayRemaining, arrowPassedTime));
    }

    private IEnumerator ServerDelayedFire(uint activationTick, float delayRemaining, float arrowPassedTime)
    {
        if (delayRemaining > 0f)
            yield return new WaitForSeconds(delayRemaining);
        uint fireTick = Bow.TimeManager.LocalTick;
        FireVolley(fireTick, arrowPassedTime, isServer: true);
        Weapon.AbilityComplete(this, true);
    }

    private void FireVolley(uint tick, float passedTime, bool isServer)
    {
        int arrowsPerRow = Mathf.Max(1, UnloadQuiverData.ArrowCount / 2);
        for (int row = 0; row < 2; row++)
        {
            for (int i = 0; i < arrowsPerRow; i++)
            {
                Vector3 dir = GetSpreadDirection(row, i, arrowsPerRow, isServer);
                FireArrow(dir, tick, passedTime, (row == 1 && i == arrowsPerRow - 1), isServer);
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
        float horizontalAngle = Mathf.Lerp(-UnloadQuiverData.XSpreadAngle * 0.5f, UnloadQuiverData.XSpreadAngle * 0.5f, horizontalT);
        float verticalAngle = row == 0 ? UnloadQuiverData.YSpreadAngle * 0.5f : -UnloadQuiverData.YSpreadAngle * 0.5f;

        // Add random jitter on top of the deterministic grid position
        horizontalAngle += Random.Range(-UnloadQuiverData.RandomSpreadJitter, UnloadQuiverData.RandomSpreadJitter);
        verticalAngle += Random.Range(-UnloadQuiverData.RandomSpreadJitter, UnloadQuiverData.RandomSpreadJitter);

        Quaternion rot = Quaternion.AngleAxis(horizontalAngle, up) * Quaternion.AngleAxis(verticalAngle, right);
        return rot * baseDir;
    }

    private void FireArrow(Vector3 aimDir, uint tick, float passedTime, bool isFinalArrow, bool isServer)
    {
        Vector3 spawnPos = isServer? Bow.Loadout.FPCam.ServerFirePoint.position : Bow.Loadout.FPCam.ClientFirePoint.position;
        float velocity = Bow.ArrowVelocity;
        float baseDamage = Bow.Stats.GetDamage();
        float totalDamage = baseDamage * UnloadQuiverData.DamageMultiplier;
        if (isServer)
        {
            Bow.SpawnArrow(spawnPos, aimDir, Bow.Loadout.transform, velocity, baseDamage, totalDamage, Bow.ServerPendingEffects.ToArray(), passedTime, isServer: true);
            foreach (NetworkConnection conn in Weapon.ServerManager.Clients.Values)
            {
                if (conn == Weapon.Owner) continue;
                Bow.AllTargetFireRPC(conn, Weapon.Loadout, baseDamage, totalDamage, velocity, tick, Bow.ServerPendingEffects.ToArray());
            }
            if (isFinalArrow)
                Bow.ClearEffects(true);
        }
        else
        {
            Bow.SpawnArrow(spawnPos, aimDir, Bow.Loadout.transform, velocity, baseDamage, totalDamage, Bow.ClientPendingEffects.ToArray(), 0f, isServer: false);
            if (isFinalArrow)
                Bow.ClearEffects(false);
        }
    }
}