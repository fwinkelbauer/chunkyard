namespace Chunkyard.Core;

/// <summary>
/// A snapshot contains a list of references which describe the state of several
/// blobs at a specific point in time.
/// </summary>
public sealed class Snapshot
{
    public Snapshot(
        DateTime creationTimeUtc,
        IReadOnlyCollection<BlobReference> blobReferences)
    {
        CreationTimeUtc = creationTimeUtc;
        BlobReferences = blobReferences;
    }

    public DateTime CreationTimeUtc { get; }

    public IReadOnlyCollection<BlobReference> BlobReferences { get; }

    public override bool Equals(object? obj)
    {
        return obj is Snapshot other
            && CreationTimeUtc == other.CreationTimeUtc
            && BlobReferences.SequenceEqual(other.BlobReferences);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            CreationTimeUtc,
            BlobReferences);
    }
}
