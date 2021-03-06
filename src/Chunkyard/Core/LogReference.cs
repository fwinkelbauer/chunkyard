using System;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// The highest level reference which describes how to retrieve a set of
    /// data stored in a <see cref="ContentStore"/>. This reference can be used
    /// to recreate a key from a password.
    /// </summary>
    public class LogReference
    {
        public LogReference(
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
            return obj is LogReference other
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
