using System;

namespace Chunkyard
{
    internal class Chunk
    {
        public Chunk(
            Uri contentUri,
            string fingerprint,
            byte[] nonce,
            byte[] tag)
        {
            ContentUri = contentUri;
            Fingerprint = fingerprint;
            Nonce = nonce;
            Tag = tag;
        }

        public Uri ContentUri { get; }

        public string Fingerprint { get; }

        public byte[] Nonce { get; }

        public byte[] Tag { get; }
    }
}
