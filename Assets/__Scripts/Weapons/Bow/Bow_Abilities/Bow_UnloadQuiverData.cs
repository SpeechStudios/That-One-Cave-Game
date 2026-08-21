using System.Collections;
using System.Linq;
using UnityEngine;

public struct UnloadQuiverPacket
{
    public float Velocity;
    public int[] Effects;
}

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
        Weapon.Player.Loadout.WeaponAnimator.SetTrigger("UnloadQuiver");
    }

    private IEnumerator ClientDelayedFire()
    {
        yield return new WaitForSeconds(UnloadQuiverData.FireDelay);
        FireVolley(Bow.ArrowVelocity, Bow.ClientPendingEffects.Select(x => x.Item2).ToArray(), 0f, NetworkRole.Owner);
        Weapon.AbilityComplete(this, false);
    }

    public override (ObserverType, byte[]) ServerActivate(uint tick)
    {
        uint serverTick = Bow.TimeManager.LocalTick;
        uint clampedTick = tick > serverTick ? serverTick : tick;
        if (serverTick - clampedTick > Weapon.MAX_TICK_DELAY)
            return default;

        float latency = (float)Bow.TimeManager.TimePassed(clampedTick, allowNegative: false);
        float maxPassedTimeSeconds = Weapon.MAX_TICK_DELAY * (float)Weapon.TimeManager.TickDelta;

        float delayRemaining = Mathf.Max(0f, UnloadQuiverData.FireDelay - latency);
        float arrowPassedTime = Mathf.Clamp(latency - UnloadQuiverData.FireDelay, 0f, maxPassedTimeSeconds);


        Weapon.StartCoroutine(ServerDelayedFire(delayRemaining, arrowPassedTime));
        byte[] bytes = Serializer.Serialize(new UnloadQuiverPacket { Velocity = Bow.ArrowVelocity, Effects = Bow.ServerPendingEffects.Select(x => x.Item2).ToArray() });
        return (ObserverType.All, bytes);
    }

    private IEnumerator ServerDelayedFire(float delayRemaining, float arrowPassedTime)
    {
        if (delayRemaining > 0f)
            yield return new WaitForSeconds(delayRemaining);

        FireVolley(Bow.ArrowVelocity, Bow.ServerPendingEffects.Select(x => x.Item2).ToArray(), arrowPassedTime, NetworkRole.Server);
        Weapon.AbilityComplete(this, true);
    }
    public override void ObserverActivate(byte[] bytes, uint tick)
    {
        var packet = Serializer.Deserialize<UnloadQuiverPacket>(bytes);

        float latency = (float)Bow.TimeManager.TimePassed(tick, allowNegative: false);
        float delayRemaining = Mathf.Max(0f, UnloadQuiverData.FireDelay - latency);
        float arrowPassedTime = latency - UnloadQuiverData.FireDelay;

        Weapon.StartCoroutine(ObserverDelayedFire(packet.Velocity, packet.Effects, delayRemaining, arrowPassedTime));
    }
    public IEnumerator ObserverDelayedFire(float velocity, int[] effects, float delayRemaining, float arrowPassedTime)
    {
        if (delayRemaining > 0f)
            yield return new WaitForSeconds(delayRemaining);

        FireVolley(velocity, effects, arrowPassedTime, NetworkRole.Observer);
    }
    private void FireVolley(float velocity, int[] effects, float passedTime, NetworkRole role)
    {
        int arrowsPerRow = Mathf.Max(1, UnloadQuiverData.ArrowCount / 2);
        for (int row = 0; row < 2; row++)
        {
            for (int i = 0; i < arrowsPerRow; i++)
            {
                Vector3 dir = GetSpreadDirection(row, i, arrowsPerRow, role);
                bool isFinalArrow = (row == 1 && i == arrowsPerRow - 1);
                FireArrow(dir, velocity, effects, isFinalArrow, passedTime, role);
            }
        }
    }
    private void FireArrow(Vector3 aimDir, float velocity, int[] effects, bool isFinalArrow, float passedTime,  NetworkRole role)
    {
        Vector3 firePoint = Vector3.zero;
        switch (role)
        {
            case NetworkRole.Owner:
                firePoint = Bow.Player.Loadout.FPCam.ClientFirePoint.position;
                break;
            case NetworkRole.Server:
                firePoint = Bow.Player.Loadout.FPCam.ServerFirePoint.position;
                break;
            case NetworkRole.Observer:
                firePoint = Bow.Player.Loadout.TP_BowFirePoint.position;
                break;
        }
        if (role == NetworkRole.Server)
        {
            float totalDamage = Bow.Player.Stats.GetDamage() * UnloadQuiverData.DamageMultiplier;
            Bow.SpawnArrow(firePoint, aimDir, Bow.Player, velocity, totalDamage, effects, passedTime, isServer: true);
            if (isFinalArrow)
                Bow.ClearEffects(true);
        }
        else
        {
            Bow.SpawnArrow(firePoint, aimDir, Bow.Player, velocity, 0f, effects, passedTime, isServer: false);
            if (isFinalArrow)
                Bow.ClearEffects(false);
        }
    }


    private Vector3 GetSpreadDirection(int row, int indexInRow, int arrowsPerRow, NetworkRole role)
    {
        Transform firePoint;
        switch (role)
        {
            case NetworkRole.Owner:
                firePoint = Bow.Player.Loadout.FPCam.ClientFirePoint;
                break;
            case NetworkRole.Server:
                firePoint = Bow.Player.Loadout.FPCam.ServerFirePoint;
                break;
            case NetworkRole.Observer:
                firePoint = Bow.Player.Loadout.FPCam.ServerFirePoint;
                break;
            default:
                Debug.LogError("Role Not Sent");
                firePoint = null;
                break;
        }
        Vector3 baseDir = firePoint.forward;
        Vector3 up = firePoint.up;
        Vector3 right = firePoint.right;

        float horizontalT = arrowsPerRow > 1 ? (float)indexInRow / (arrowsPerRow - 1) : 0.5f;
        float horizontalAngle = Mathf.Lerp(-UnloadQuiverData.XSpreadAngle * 0.5f, UnloadQuiverData.XSpreadAngle * 0.5f, horizontalT);
        float verticalAngle = row == 0 ? UnloadQuiverData.YSpreadAngle * 0.5f : -UnloadQuiverData.YSpreadAngle * 0.5f;

        horizontalAngle += Random.Range(-UnloadQuiverData.RandomSpreadJitter, UnloadQuiverData.RandomSpreadJitter);
        verticalAngle += Random.Range(-UnloadQuiverData.RandomSpreadJitter, UnloadQuiverData.RandomSpreadJitter);

        Quaternion rot = Quaternion.AngleAxis(horizontalAngle, up) * Quaternion.AngleAxis(verticalAngle, right);
        return rot * baseDir;
    }
}