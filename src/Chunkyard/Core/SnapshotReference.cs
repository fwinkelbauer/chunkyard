namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to retrieve a set of encrypted chunks from a
/// <see cref="SnapshotStore"/>.
/// </summary>
public sealed class SnapshotReference
{
    public SnapshotReference(
        string salt,
        int iterations,
        IReadOnlyCollection<string> chunkIds)
    {
        Salt = salt;
        Iterations = iterations;
        ChunkIds = chunkIds;
    }

    public string Salt { get; }

    public int Iterations { get; }

    public IReadOnlyCollection<string> ChunkIds { get; }

    public override bool Equals(object? obj)
    {
        return obj is SnapshotReference other
            && Salt.Equals(other.Salt)
            && Iterations == other.Iterations
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Salt, Iterations, ChunkIds);
    }
}
