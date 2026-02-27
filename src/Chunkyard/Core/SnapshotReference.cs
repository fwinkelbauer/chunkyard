namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to retrieve a set of encrypted chunks from a
/// <see cref="SnapshotStore"/>.
/// </summary>
public sealed record SnapshotReference(
    string Salt,
    int Iterations,
    IReadOnlyCollection<string> ChunkIds)
{
    public bool Equals(SnapshotReference? other)
    {
        return other is not null
            && Salt.Equals(other.Salt)
            && Iterations == other.Iterations
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Salt);
        hash.Add(Iterations);

        foreach (var chunkId in ChunkIds)
        {
            hash.Add(chunkId);
        }

        return hash.ToHashCode();
    }
}
