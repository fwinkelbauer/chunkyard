namespace Chunkyard.Infrastructure;

/// <summary>
/// A <see cref="IBlobSystem"/> decorator that does not retrieve or store data.
/// </summary>
public sealed class DryRunBlobSystem : IBlobSystem
{
    private readonly IBlobSystem _blobSystem;

    public DryRunBlobSystem(IBlobSystem blobSystem)
    {
        _blobSystem = blobSystem;
    }

    public Blob[] ListBlobs()
    {
        return _blobSystem.ListBlobs();
    }

    public Stream OpenRead(string blobName)
    {
        return Stream.Null;
    }

    public Blob? GetBlob(string blobName)
    {
        return _blobSystem.GetBlob(blobName);
    }

    public Stream OpenWrite(Blob blob)
    {
        return Stream.Null;
    }
}
