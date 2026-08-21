using FishNet.Serializing;

public static class Serializer
{
    public static byte[] Serialize<T>(T value)
    {
        var writer = WriterPool.Retrieve();
        try
        {
            writer.Write(value);
            return writer.GetArraySegment().ToArray();
        }
        finally
        {
            writer.Store();
        }
    }

    public static T Deserialize<T>(byte[] data)
    {
        var reader = ReaderPool.Retrieve(data, null);
        try
        {
            return reader.Read<T>();
        }
        finally
        {
            reader.Store();
        }
    }
}