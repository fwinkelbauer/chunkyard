using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyBlobWriter : IBlobWriter
    {
        private readonly Dictionary<string, Blob> _blobs;
        private readonly Dictionary<string, MemoryStream> _streams;
        private readonly object _lock;

        public DummyBlobWriter(
            Blob[]? blobs = null)
        {
            _blobs = blobs == null
                ? new Dictionary<string, Blob>()
                : blobs.ToDictionary(
                    b => b.Name,
                    b => b);

            _streams = new Dictionary<string, MemoryStream>();
            _lock = new object();
        }

        public Blob? FindBlob(string blobName)
        {
            if (_blobs.TryGetValue(blobName, out var blob))
            {
                return blob;
            }

            return null;
        }

        public Stream OpenWrite(string blobName)
        {
            var stream = new MemoryStream();

            lock (_lock)
            {
                _streams[blobName] = stream;
            }

            return stream;
        }

        public void UpdateBlobMetadata(Blob blob)
        {
            _blobs[blob.Name] = blob;
        }

        public byte[]? ShowContent(string blobName)
        {
            lock (_lock)
            {
                if (_streams.TryGetValue(blobName, out var stream))
                {
                    return stream.ToArray();
                }
            }

            return null;
        }
    }
}
