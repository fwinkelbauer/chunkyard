namespace Chunkyard.Tests.Infrastructure;

internal sealed class MemoryBlobSystem : IBlobSystem
{
    private readonly Dictionary<string, Blob> _blobs;
    private readonly Dictionary<string, byte[]> _values;
    private readonly object _lock;

    public MemoryBlobSystem()
    {
        _blobs = new();
        _values = new();
        _lock = new();
    }

    public bool BlobExists(string blobName)
    {
        lock (_lock)
        {
            return _blobs.ContainsKey(blobName);
        }
    }

    public IReadOnlyCollection<Blob> ListBlobs()
    {
        lock (_lock)
        {
            return _blobs.Values
                .OrderBy(b => b.Name)
                .ToArray();
        }
    }

    public Stream OpenRead(string blobName)
    {
        lock (_lock)
        {
            return new MemoryStream(
                _values[blobName].ToArray());
        }
    }

    public Blob GetBlob(string blobName)
    {
        lock (_lock)
        {
            return _blobs[blobName];
        }
    }

    public Stream OpenWrite(Blob blob)
    {
        return new WriteStream(this, blob);
    }

    private void Write(Blob blob, byte[] value)
    {
        lock (_lock)
        {
            _values[blob.Name] = value;
            _blobs[blob.Name] = blob;
        }
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
