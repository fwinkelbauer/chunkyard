using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A reference which can be used to retrieve a <see cref="Snapshot"/> from
    /// a <see cref="SnapshotStore"/> based on a password based encryption key.
    /// </summary>
    internal class SnapshotReference
    {
        public SnapshotReference(
            IReadOnlyCollection<Uri> contentUris,
            byte[] salt,
            int iterations)
        {
            ContentUris = contentUris;
            Salt = salt;
            Iterations = iterations;
        }

        public IReadOnlyCollection<Uri> ContentUris { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

        public override bool Equals(object? obj)
        {
            return obj is SnapshotReference other
                && ContentUris.SequenceEqual(other.ContentUris)
                && Salt.SequenceEqual(other.Salt)
                && Iterations == other.Iterations;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentUris, Salt, Iterations);
        }
    }
}
