namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to store binary data in a
/// <see cref="SnapshotStore"/>.
/// </summary>
public class BlobReference
{
    public BlobReference(
        Blob blob,
        byte[] nonce,
        IReadOnlyCollection<string> chunkIds)
    {
        Blob = blob;
        Nonce = nonce;
        ChunkIds = chunkIds;
    }

    public Blob Blob { get; }

    public byte[] Nonce { get; }

    public IReadOnlyCollection<string> ChunkIds { get; }

    public override bool Equals(object? obj)
    {
        return obj is BlobReference other
            && Blob.Equals(other.Blob)
            && Nonce.SequenceEqual(other.Nonce)
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Blob, Nonce, ChunkIds);
    }
}
