public static class RandomSeed
{
    public static uint Generate(int playerId, int tick, int itemHash, int sourceHash)
    {
        unchecked
        {
            uint hash = 2166136261u;

            hash = (hash ^ (uint)playerId) * 16777619u;
            hash = (hash ^ (uint)tick) * 16777619u;
            hash = (hash ^ (uint)itemHash) * 16777619u;
            hash = (hash ^ (uint)sourceHash) * 16777619u;

            // Final avalanche
            hash ^= hash >> 16;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;

            return hash;
        }
    }
}