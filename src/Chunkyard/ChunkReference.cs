using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard
{
    public class ChunkReference
    {
        public ChunkReference(
            Uri contentUri,
            string fingerprint,
            IEnumerable<byte> nonce,
            IEnumerable<byte> tag)
        {
            ContentUri = contentUri;
            Fingerprint = fingerprint;
            Nonce = nonce.ToList();
            Tag = tag.ToList();
        }

        public Uri ContentUri { get; }

        public string Fingerprint { get; }

        public IEnumerable<byte> Nonce { get; }

        public IEnumerable<byte> Tag { get; }
    }
}
