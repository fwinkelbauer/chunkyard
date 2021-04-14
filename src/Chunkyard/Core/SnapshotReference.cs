using System;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A reference which can be used to retrieve a <see cref="Snapshot"/> from
    /// a <see cref="ContentStore"/> based on a password based encryption key.
    /// </summary>
    public class SnapshotReference
    {
        public SnapshotReference(
            DocumentReference documentReference,
            byte[] salt,
            int iterations)
        {
            DocumentReference = documentReference;
            Salt = salt;
            Iterations = iterations;
        }

        public DocumentReference DocumentReference { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

        public override bool Equals(object? obj)
        {
            return obj is SnapshotReference other
                && DocumentReference.Equals(other.DocumentReference)
                && Salt.SequenceEqual(other.Salt)
                && Iterations == other.Iterations;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DocumentReference, Salt, Iterations);
        }
    }
}
