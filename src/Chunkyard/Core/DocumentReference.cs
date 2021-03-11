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
            IImmutableList<Uri> contentUris)
        {
            Nonce = nonce;
            ContentUris = contentUris;
        }

        public byte[] Nonce { get; }

        public IImmutableList<Uri> ContentUris { get; }

        public override bool Equals(object? obj)
        {
            return obj is DocumentReference other
                && Nonce.SequenceEqual(other.Nonce)
                && ContentUris.SequenceEqual(other.ContentUris);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nonce, ContentUris);
        }
    }
}
