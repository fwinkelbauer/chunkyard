using System;
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

        public DummyBlobWriter(
            string[]? blobNames = null)
        {
            _blobs = blobNames == null
                ? new Dictionary<string, Blob>()
                : blobNames.ToDictionary(
                    b => b,
                    b => new Blob(b, DateTime.UtcNow));

            _streams = new Dictionary<string, MemoryStream>();
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

            _streams[blobName] = stream;

            return stream;
        }

        public void UpdateBlobMetadata(Blob blob)
        {
            _blobs[blob.Name] = blob;
        }

        public byte[] ShowContent(string blobName)
        {
            return _streams[blobName].ToArray();
        }
    }
}
