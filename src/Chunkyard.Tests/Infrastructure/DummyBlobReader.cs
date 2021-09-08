using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyBlobReader : IBlobReader
    {
        private readonly Blob[] _blobs;
        private readonly Func<string, byte[]> _createContent;

        public DummyBlobReader(
            IEnumerable<Blob> blobs,
            Func<string, byte[]>? createContent = null)
        {
            _blobs = blobs.ToArray();
            _createContent = createContent
                ?? (blobName => Encoding.UTF8.GetBytes(blobName));
        }

        public IReadOnlyCollection<Blob> FetchBlobs()
        {
            return _blobs;
        }

        public Stream OpenRead(string blobName)
        {
            return new MemoryStream(
                _createContent(blobName));
        }
    }
}
