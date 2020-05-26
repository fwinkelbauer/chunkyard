using System;
using System.Collections.Generic;
using System.Linq;

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
                && ContentUri.Equals(reference.ContentUri)
                && Tag.SequenceEqual(reference.Tag);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentUri, Tag);
        }
    }
}
