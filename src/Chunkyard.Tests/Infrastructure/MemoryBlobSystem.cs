namespace Chunkyard.Tests.Infrastructure
{
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
                using var stream = OpenWrite(blob);

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

        public IReadOnlyCollection<Blob> FetchBlobs(Fuzzy excludeFuzzy)
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

        public Blob FetchMetadata(string blobName)
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

        public void UpdateMetadata(Blob blob)
        {
            lock (_lock)
            {
                _blobs[blob.Name] = blob;
            }
        }

        private class WriteStream : MemoryStream
        {
            private readonly MemoryBlobSystem _blobSystem;
            private readonly Blob _blob;

            public WriteStream(
                MemoryBlobSystem blobSystem,
                Blob blob)
            {
                _blobSystem = blobSystem;
                _blob = blob;
            }

            protected override void Dispose(bool disposing)
            {
                lock (_blobSystem._lock)
                {
                    _blobSystem._bytes[_blob.Name] = ToArray();
                    _blobSystem._blobs[_blob.Name] = _blob;
                }

                base.Dispose(disposing);
            }
        }
    }
}
