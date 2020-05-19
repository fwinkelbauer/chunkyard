using System;

namespace Chunkyard
{
    public class ChunkReference
    {
        public ChunkReference(
            Uri contentUri,
            byte[] tag)
        {
            ContentUri = contentUri;
            Tag = tag;
        }

        public Uri ContentUri { get; }

        public byte[] Tag { get; }
    }
}
