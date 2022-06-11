namespace Chunkyard.Core;

/// <summary>
/// A utility class to serialize objects into bytes and back.
/// </summary>
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(LogReference))]
public partial class Serialize : JsonSerializerContext
{
    public static byte[] SnapshotToBytes(Snapshot snapshot)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            snapshot,
            Default.Snapshot);
    }

    public static byte[] LogReferenceToBytes(LogReference logReference)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            logReference,
            Default.LogReference);
    }

    public static Snapshot BytesToSnapshot(byte[] json)
    {
        return JsonSerializer.Deserialize(json, Default.Snapshot)
            ?? throw new ArgumentNullException(nameof(json));
    }

    public static LogReference BytesToLogReference(byte[] json)
    {
        return JsonSerializer.Deserialize(json, Default.LogReference)
            ?? throw new ArgumentNullException(nameof(json));
    }
}
