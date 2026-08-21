using FishNet.Connection;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "New Ability/Bow/TripleShot")]
public class Bow_TrippleShotData : AbilityData
{
    public int ArrowCount = 3;
    public float ArrowFireInterval = 0.1f;
    public float DamageMultiplier = 0.75f;
    public override Ability CreateAbility() => new Bow_TrippleShot();
}
public class Bow_TrippleShot : Ability
{
    private Bow Bow;
    private Bow_TrippleShotData TrippleShotData;

    public override void Initialize(Weapon weapon, AbilityData data)
    {
        base.Initialize(weapon, data);
        Bow = weapon as Bow;
        TrippleShotData = data as Bow_TrippleShotData;
    }

    public override void ClientActivate(uint tick)
    {
        Bow.StartCoroutine(FireSequence(tick, isServer: false));
    }

    public override (ObserverType, byte[]) ServerActivate(uint tick)
    {
        Bow.StartCoroutine(FireSequence(tick, isServer: true));
        return default;
    }

    private IEnumerator FireSequence(uint activationTick, bool isServer)
    {
        for (int i = 0; i < TrippleShotData.ArrowCount; i++)
        {
            uint shotTick = activationTick + (uint)(i * Mathf.RoundToInt(TrippleShotData.ArrowFireInterval / (float)Bow.TimeManager.TickDelta));
            FireArrow(shotTick, isServer, i == TrippleShotData.ArrowCount - 1);

            if (i < TrippleShotData.ArrowCount - 1)
                yield return new WaitForSeconds(TrippleShotData.ArrowFireInterval);
        }
        Weapon.AbilityComplete(this, isServer);
    }

    private void FireArrow(uint tick, bool isServer, bool isFinalArrow)
    {
        Transform firePoint = isServer ? Bow.Player.Loadout.FPCam.ServerFirePoint : Bow.Player.Loadout.FPCam.ClientFirePoint;
        Vector3 spawnPos = firePoint.position;
        Vector3 aimDir = firePoint.forward;
        float velocity = Bow.ArrowVelocity;
        float totalDamage = Bow.Player.Stats.GetDamage() * TrippleShotData.DamageMultiplier;

        if (isServer)
        {
            uint serverTick = Bow.TimeManager.LocalTick;
            uint clampedTick = tick > serverTick ? serverTick : tick;
            if (serverTick - clampedTick > Weapon.MAX_TICK_DELAY)
                return;
            float passedTime = (float)Bow.TimeManager.TimePassed(clampedTick, allowNegative: false);

            Bow.SpawnArrow(spawnPos, aimDir, Weapon.Player, velocity, totalDamage, Bow.ServerPendingEffects.ToArray(), passedTime, isServer: true);
            foreach (NetworkConnection conn in Weapon.ServerManager.Clients.Values)
            {
                if (conn == Weapon.Owner) continue;
                Bow.FireTargetRPC(conn,  totalDamage, velocity,Bow.ServerPendingEffects.ToArray(), clampedTick);
            }
            if (isFinalArrow)
                Bow.ClearEffects(true, clampedTick);
        }
        else
        {
            Bow.SpawnArrow(spawnPos, aimDir, null, velocity, totalDamage, Bow.ClientPendingEffects.ToArray(), 0f, isServer: false);
            if (isFinalArrow)
                Bow.ClearEffects(false);
        }
    }
}