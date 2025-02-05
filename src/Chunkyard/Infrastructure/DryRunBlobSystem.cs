namespace Chunkyard.Infrastructure;

/// <summary>
/// A <see cref="IBlobSystem"/> decorator that does not persist data changes.
/// </summary>
public sealed class DryRunBlobSystem : IBlobSystem
{
    private readonly IBlobSystem _blobSystem;

    public DryRunBlobSystem(IBlobSystem blobSystem)
    {
        _blobSystem = blobSystem;
    }

    public bool BlobExists(string blobName)
    {
        return _blobSystem.BlobExists(blobName);
    }

    public Blob[] ListBlobs()
    {
        return _blobSystem.ListBlobs();
    }

    public Stream OpenRead(string blobName)
    {
        // Do nothing
        return Stream.Null;
    }

    public Blob GetBlob(string blobName)
    {
        return _blobSystem.GetBlob(blobName);
    }

    public Stream OpenWrite(Blob blob)
    {
        // Do nothing
        return Stream.Null;
    }
}
