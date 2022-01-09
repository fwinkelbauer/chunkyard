namespace Chunkyard.Core;

/// <summary>
/// Defines a contract to read and write Blobs.
/// </summary>
public interface IBlobSystem
{
    bool BlobExists(string blobName);

    void RemoveBlob(string blobName);

    IReadOnlyCollection<Blob> ListBlobs(Fuzzy excludeFuzzy);

    Stream OpenRead(string blobName);

    Blob GetBlob(string blobName);

    Stream OpenWrite(Blob blob);
}
