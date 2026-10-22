namespace Chunkyard.Tests.Infrastructure;

/// <summary>
/// An in-memory implementation of <see cref="IBlobSystem"/>.
/// </summary>
internal sealed class MemoryBlobSystem : IBlobSystem
{
    private readonly Dictionary<string, Blob> _blobs;
    private readonly Dictionary<string, byte[]> _values;

    public MemoryBlobSystem()
    {
        _blobs = new();
        _values = new();
    }

    public Blob[] ListBlobs()
    {
        return _blobs.Values
            .OrderBy(b => b.Name)
            .ToArray();
    }

    public Stream OpenRead(string blobName)
    {
        return new MemoryStream(
            _values[blobName].ToArray());
    }

    public Blob? GetBlob(string blobName)
    {
        return _blobs.TryGetValue(blobName, out var blob)
            ? blob
            : null;
    }

    public void Write(Blob blob, Action<Stream> write)
    {
        using var memoryStream = new MemoryStream();

        write(memoryStream);

        _values[blob.Name] = memoryStream.ToArray();
        _blobs[blob.Name] = blob;
    }
}
