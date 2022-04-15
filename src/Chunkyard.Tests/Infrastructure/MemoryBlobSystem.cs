namespace Chunkyard.Tests.Infrastructure;

internal class MemoryBlobSystem : IBlobSystem
{
    private readonly Dictionary<string, Blob> _blobs;
    private readonly Dictionary<string, byte[]> _values;
    private readonly object _lock;

    public MemoryBlobSystem(
        IEnumerable<Blob>? blobs = null,
        Func<string, byte[]>? generate = null)
    {
        _blobs = blobs == null
            ? new Dictionary<string, Blob>()
            : blobs.ToDictionary(
                b => b.Name,
                b => b);

        _values = new Dictionary<string, byte[]>();
        _lock = new object();

        generate ??= (blobName => Encoding.UTF8.GetBytes(blobName));

        foreach (var blob in _blobs.Values)
        {
            Write(blob, generate(blob.Name), true);
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
            _values.Remove(blobName);
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
        return new WriteStream(this, blob, true);
    }

    public Stream NewWrite(Blob blob)
    {
        return new WriteStream(this, blob, false);
    }

    private void Write(Blob blob, byte[] value, bool overwrite)
    {
        lock (_lock)
        {
            if (overwrite)
            {
                _values[blob.Name] = value;
            }
            else
            {
                _values.Add(blob.Name, value);
            }

            _blobs[blob.Name] = blob;
        }
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
            _blobSystem.Write(_blob, ToArray(), _overwrite);

            base.Dispose(disposing);
        }
    }
}
