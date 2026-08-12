using FishNet;
using FishNet.Managing.Timing;

[System.Serializable]
public struct CooldownTimer
{
    public uint EndTick;

    public void Start(float durationSeconds)
    {
        uint durationTicks = InstanceFinder.TimeManager.TimeToTicks(durationSeconds, TickRounding.RoundUp);
        EndTick = InstanceFinder.TimeManager.Tick + durationTicks;
    }

    public void StartAtTick(uint startTick, float durationSeconds)
    {
        uint durationTicks = InstanceFinder.TimeManager.TimeToTicks(durationSeconds, TickRounding.RoundUp);
        EndTick = startTick + durationTicks;
    }

    public readonly bool IsReady => InstanceFinder.TimeManager.Tick >= EndTick;

    public readonly uint TicksRemaining
    {
        get
        {
            uint current = InstanceFinder.TimeManager.Tick;
            return current >= EndTick ? 0 : EndTick - current;
        }
    }

    public readonly float SecondsRemaining =>
        (float)InstanceFinder.TimeManager.TicksToTime(TicksRemaining);

    public readonly float TimeOfNextAttack =>
        (float)InstanceFinder.TimeManager.TicksToTime(EndTick);
}