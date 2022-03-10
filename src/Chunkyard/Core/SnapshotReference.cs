namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to retrieve a <see cref="Snapshot"/> from
/// a <see cref="SnapshotStore"/> based on a password based encryption key.
/// </summary>
public class SnapshotReference : IVersioned
{
    public SnapshotReference(
        int schemaVersion,
        byte[] salt,
        int iterations,
        IReadOnlyCollection<Uri> chunkIds)
    {
        SchemaVersion = schemaVersion;
        Salt = salt;
        Iterations = iterations;
        ChunkIds = chunkIds;
    }

    public int SchemaVersion { get; }

    public byte[] Salt { get; }

    public int Iterations { get; }

    public IReadOnlyCollection<Uri> ChunkIds { get; }

    public override bool Equals(object? obj)
    {
        return obj is SnapshotReference other
            && SchemaVersion == other.SchemaVersion
            && Salt.SequenceEqual(other.Salt)
            && Iterations == other.Iterations
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SchemaVersion, Salt, Iterations, ChunkIds);
    }
}
