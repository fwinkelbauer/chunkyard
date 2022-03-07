namespace Chunkyard.Tests.Infrastructure;

internal class MemoryBlobSystem : IBlobSystem
{
    private readonly Dictionary<string, Blob> _blobs;
    private readonly Dictionary<string, byte[]> _bytes;
    private readonly object _lock;

    public MemoryBlobSystem(
        IEnumerable<Blob>? blobs = null,
        Func<string, byte[]>? createContent = null)
    {
        _blobs = blobs == null
            ? new Dictionary<string, Blob>()
            : blobs.ToDictionary(
                b => b.Name,
                b => b);

        _bytes = new Dictionary<string, byte[]>();
        _lock = new object();

        createContent ??= (blobName => Encoding.UTF8.GetBytes(blobName));

        foreach (var blob in _blobs.Values)
        {
            using var stream = NewWrite(blob);

            stream.Write(
                createContent(blob.Name));
        }
    }

    public bool BlobExists(string blobName)
    {
        lock (_lock)
        {
            return _blobs.ContainsKey(blobName);
        }
    }

    public void RemoveBlob(string blobName)
    {
        lock (_lock)
        {
            _blobs.Remove(blobName);
        }
    }

    public IReadOnlyCollection<Blob> ListBlobs(Fuzzy excludeFuzzy)
    {
        lock (_lock)
        {
            return _blobs.Values
                .Where(b => !excludeFuzzy.IsExcludingMatch(b.Name))
                .ToArray();
        }
    }

    public Stream OpenRead(string blobName)
    {
        lock (_lock)
        {
            return new MemoryStream(
                _bytes[blobName].ToArray());
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
        return new WriteStream(this, blob, true);
    }

    public Stream NewWrite(Blob blob)
    {
        return new WriteStream(this, blob, false);
    }

    private class WriteStream : MemoryStream
    {
        private readonly MemoryBlobSystem _blobSystem;
        private readonly Blob _blob;
        private readonly bool _overwrite;

        public WriteStream(
            MemoryBlobSystem blobSystem,
            Blob blob,
            bool overwrite)
        {
            _blobSystem = blobSystem;
            _blob = blob;
            _overwrite = overwrite;
        }

        protected override void Dispose(bool disposing)
        {
            lock (_blobSystem._lock)
            {
                if (_overwrite)
                {
                    _blobSystem._bytes[_blob.Name] = ToArray();
                }
                else
                {
                    _blobSystem._bytes.Add(_blob.Name, ToArray());
                }

                _blobSystem._blobs[_blob.Name] = _blob;
            }

            base.Dispose(disposing);
        }
    }
}
