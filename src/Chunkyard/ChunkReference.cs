using System;
using System.Collections.Generic;

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

        public override bool Equals(object? obj)
        {
            return obj is ChunkReference reference
                && EqualityComparer<Uri>.Default.Equals(
                    ContentUri,
                    reference.ContentUri)
                && EqualityComparer<byte[]>.Default.Equals(
                    Tag,
                    reference.Tag);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentUri, Tag);
        }
    }
}
