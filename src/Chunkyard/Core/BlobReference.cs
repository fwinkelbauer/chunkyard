namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to store binary data in a
/// <see cref="SnapshotStore"/>.
/// </summary>
public sealed class BlobReference
{
    public BlobReference(
        Blob blob,
        IReadOnlyCollection<string> chunkIds)
    {
        Blob = blob;
        ChunkIds = chunkIds;
    }

    public Blob Blob { get; }

    public IReadOnlyCollection<string> ChunkIds { get; }

    public override bool Equals(object? obj)
    {
        return obj is BlobReference other
            && Blob.Equals(other.Blob)
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Blob, ChunkIds);
    }
}
