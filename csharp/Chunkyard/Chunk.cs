using System;

namespace Chunkyard
{
    internal class Chunk
    {
        public Chunk(Uri contentUri, byte[] tag)
        {
            ContentUri = contentUri;
            Tag = tag;
        }

        public Uri ContentUri { get; }

        public byte[] Tag { get; }
    }
}
