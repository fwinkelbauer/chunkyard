namespace Chunkyard.Core;

/// <summary>
/// A utility class to convert objects into bytes.
/// </summary>
[JsonSerializable(typeof(Snapshot))]
[JsonSerializable(typeof(SnapshotReference))]
public partial class DataConvert : JsonSerializerContext
{
    public static byte[] SnapshotToBytes(Snapshot snapshot)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            snapshot,
            DataConvert.Default.Snapshot);
    }

    public static byte[] SnapshotReferenceToBytes(
        SnapshotReference snapshotReference)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            snapshotReference,
            DataConvert.Default.SnapshotReference);
    }

    public static Snapshot BytesToSnapshot(byte[] json)
    {
        return JsonSerializer.Deserialize(
            json,
            DataConvert.Default.Snapshot)
            ?? throw new ArgumentNullException(nameof(json));
    }

    public static SnapshotReference BytesToSnapshotReference(byte[] json)
    {
        return JsonSerializer.Deserialize(
            json,
            DataConvert.Default.SnapshotReference)
            ?? throw new ArgumentNullException(nameof(json));
    }
}
