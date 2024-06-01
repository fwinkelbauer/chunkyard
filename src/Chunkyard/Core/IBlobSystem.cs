namespace Chunkyard.Core;

/// <summary>
/// Defines a contract to read and write Blobs.
/// </summary>
public interface IBlobSystem
{
    bool BlobExists(string blobName);

    Blob[] ListBlobs();

    Stream OpenRead(string blobName);

    Blob GetBlob(string blobName);

    Stream OpenWrite(Blob blob);
}
