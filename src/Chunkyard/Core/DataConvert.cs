namespace Chunkyard.Core;

/// <summary>
/// A utility class to convert objects into bytes.
/// </summary>
public static class DataConvert
{
    public static byte[] SnapshotToBytes(Snapshot snapshot)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            snapshot,
            JsonContext.Default.Snapshot);
    }

    public static byte[] SnapshotReferenceToBytes(
        SnapshotReference snapshotReference)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            snapshotReference,
            JsonContext.Default.SnapshotReference);
    }

    public static Snapshot BytesToSnapshot(byte[] json)
    {
        return JsonSerializer.Deserialize(
            json,
            JsonContext.Default.Snapshot)
            ?? throw new ArgumentNullException(nameof(json));
    }

    public static SnapshotReference BytesToSnapshotReference(byte[] json)
    {
        return JsonSerializer.Deserialize(
            json,
            JsonContext.Default.SnapshotReference)
            ?? throw new ArgumentNullException(nameof(json));
    }
}
