namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to store binary data in a
/// <see cref="SnapshotStore"/>.
/// </summary>
public sealed record BlobReference(
    Blob Blob,
    IReadOnlyCollection<string> ChunkIds)
{
    public bool Equals(BlobReference? other)
    {
        return other is not null
            && Blob.Equals(other.Blob)
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Blob);

        foreach (var chunkId in ChunkIds)
        {
            hash.Add(chunkId);
        }

        return hash.ToHashCode();
    }
}
