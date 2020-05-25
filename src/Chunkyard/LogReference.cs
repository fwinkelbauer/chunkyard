using System;
using System.Collections.Generic;

namespace Chunkyard
{
    /// <summary>
    /// The highest level reference which describes how to retrieve a set of
    /// data stored in a <see cref="ContentStore"/>. This reference can be used
    /// to recreate a key from a password.
    /// </summary>
    public class LogReference
    {
        public LogReference(
            ContentReference contentReference,
            byte[] salt,
            int iterations)
        {
            ContentReference = contentReference;
            Salt = salt;
            Iterations = iterations;
        }

        public ContentReference ContentReference { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

        public override bool Equals(object? obj)
        {
            return obj is LogReference reference
                && EqualityComparer<ContentReference>.Default.Equals(
                    ContentReference,
                    reference.ContentReference)
                && EqualityComparer<byte[]>.Default.Equals(
                    Salt,
                    reference.Salt)
                && Iterations == reference.Iterations;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContentReference, Salt, Iterations);
        }
    }
}
