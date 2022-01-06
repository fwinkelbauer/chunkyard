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
        IReadOnlyCollection<Uri> contentUris)
    {
        Blob = blob;
        Nonce = nonce;
        ContentUris = contentUris;
    }

    public Blob Blob { get; }

    public byte[] Nonce { get; }

    public IReadOnlyCollection<Uri> ContentUris { get; }

    public override bool Equals(object? obj)
    {
        return obj is BlobReference other
            && Blob.Equals(other.Blob)
            && Nonce.SequenceEqual(other.Nonce)
            && ContentUris.SequenceEqual(other.ContentUris);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Blob, Nonce, ContentUris);
    }
}
