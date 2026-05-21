namespace Chunkyard.Tests.Infrastructure;

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

    public Stream OpenWrite(Blob blob)
    {
        return new WriteStream(this, blob);
    }

    private void Write(Blob blob, byte[] value)
    {
        _values[blob.Name] = value;
        _blobs[blob.Name] = blob;
    }

    private sealed class WriteStream : MemoryStream
    {
        private readonly MemoryBlobSystem _blobSystem;
        private readonly Blob _blob;

        public WriteStream(MemoryBlobSystem blobSystem, Blob blob)
        {
            _blobSystem = blobSystem;
            _blob = blob;
        }

        protected override void Dispose(bool disposing)
        {
            _blobSystem.Write(_blob, ToArray());

            base.Dispose(disposing);
        }
    }
}
