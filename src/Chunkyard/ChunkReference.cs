using System;
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
            int length,
            byte[] tag)
        {
            ContentUri = contentUri;
            Length = length;
            Tag = tag;
        }

        public Uri ContentUri { get; }

        public int Length { get; }

        public byte[] Tag { get; }

        public override bool Equals(object? obj)
        {
            return obj is ChunkReference reference
                && ContentUri.Equals(reference.ContentUri)
                && Length == reference.Length
                && Tag.SequenceEqual(reference.Tag);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentUri, Length, Tag);
        }
    }
}
