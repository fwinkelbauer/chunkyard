using System;

namespace Chunkyard
{
    /// <summary>
    /// A reference which describes how to retrieve an encrypted chunk from a
    /// <see cref="ContentStore"/>.
    /// </summary>
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
