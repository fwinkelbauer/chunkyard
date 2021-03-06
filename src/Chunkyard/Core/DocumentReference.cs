using System;
using System.Collections.Immutable;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// An implementation of <see cref="IContentReference"/> to store objects in
    /// an <see cref="IContentStore"/>.
    /// </summary>
    public class DocumentReference : IContentReference
    {
        public DocumentReference(
            byte[] nonce,
            IImmutableList<ChunkReference> chunks)
        {
            Nonce = nonce;
            Chunks = chunks;
        }

        public byte[] Nonce { get; }

        public IImmutableList<ChunkReference> Chunks { get; }

        public override bool Equals(object? obj)
        {
            return obj is DocumentReference other
                && Nonce.SequenceEqual(other.Nonce)
                && Chunks.SequenceEqual(other.Chunks);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nonce, Chunks);
        }
    }
}
