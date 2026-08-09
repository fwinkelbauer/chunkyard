namespace Chunkyard.Core;

/// <summary>
/// A snapshot contains a list of references which describe the state of several
/// blobs at a specific point in time.
/// </summary>
public sealed record Snapshot(
    DateTime CreationTimeUtc,
    IReadOnlyCollection<BlobReference> BlobReferences)
{
    public bool Equals(Snapshot? other)
    {
        return other is not null
            && CreationTimeUtc.Equals(other.CreationTimeUtc)
            && BlobReferences.SequenceEqual(other.BlobReferences);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(CreationTimeUtc);

        foreach (var blobReference in BlobReferences)
        {
            hash.Add(blobReference);
        }

        return hash.ToHashCode();
    }
}
