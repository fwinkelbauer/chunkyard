using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A reference which can be used to retrieve a <see cref="Snapshot"/> from
    /// a <see cref="SnapshotStore"/> based on a password based encryption key.
    /// </summary>
    internal class SnapshotReference : IContentReference
    {
        public SnapshotReference(
            byte[] nonce,
            IReadOnlyCollection<Uri> contentUris,
            byte[] salt,
            int iterations)
        {
            Nonce = nonce;
            ContentUris = contentUris;
            Salt = salt;
            Iterations = iterations;
        }

        public byte[] Nonce { get; }

        public IReadOnlyCollection<Uri> ContentUris { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

        public override bool Equals(object? obj)
        {
            return obj is SnapshotReference other
                && Nonce.SequenceEqual(other.Nonce)
                && ContentUris.SequenceEqual(other.ContentUris)
                && Salt.SequenceEqual(other.Salt)
                && Iterations == other.Iterations;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nonce, ContentUris, Salt, Iterations);
        }
    }
}
