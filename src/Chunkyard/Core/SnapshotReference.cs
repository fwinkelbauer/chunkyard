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
            byte[] salt,
            int iterations,
            IReadOnlyCollection<Uri> contentUris)
        {
            Salt = salt;
            Iterations = iterations;
            ContentUris = contentUris;
        }

        public IReadOnlyCollection<Uri> ContentUris { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

        public override bool Equals(object? obj)
        {
            return obj is SnapshotReference other
                && Salt.SequenceEqual(other.Salt)
                && Iterations == other.Iterations
                && ContentUris.SequenceEqual(other.ContentUris);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Salt, Iterations, ContentUris);
        }
    }
}
