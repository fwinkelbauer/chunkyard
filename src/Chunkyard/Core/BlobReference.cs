using System;
using System.Collections.Immutable;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// An implementation of <see cref="IContentReference"/> to store binary
    /// data in an <see cref="IContentStore"/>.
    /// </summary>
    public class BlobReference : IContentReference
    {
        public BlobReference(
            string name,
            DateTime creationTimeUtc,
            DateTime lastWriteTimeUtc,
            byte[] nonce,
            IImmutableList<ChunkReference> chunks)
        {
            Name = name;
            CreationTimeUtc = creationTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
            Nonce = nonce;
            Chunks = chunks;
        }

        public string Name { get; }

        public DateTime CreationTimeUtc { get; }

        public DateTime LastWriteTimeUtc { get; }

        public byte[] Nonce { get; }

        public IImmutableList<ChunkReference> Chunks { get; }

        public override bool Equals(object? obj)
        {
            return obj is BlobReference other
                && Name == other.Name
                && CreationTimeUtc.Equals(other.CreationTimeUtc)
                && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc)
                && Nonce.SequenceEqual(other.Nonce)
                && Chunks.SequenceEqual(other.Chunks);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name,
                CreationTimeUtc,
                LastWriteTimeUtc,
                Nonce,
                Chunks);
        }
    }
}
