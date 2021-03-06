using System;
using System.Collections.Immutable;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several files at a specific point in time.
    /// </summary>
    public class Snapshot
    {
        public Snapshot(
            DateTime creationTime,
            IImmutableList<BlobReference> blobReferences)
        {
            CreationTime = creationTime;
            BlobReferences = blobReferences;
        }

        public DateTime CreationTime { get; }

        public IImmutableList<BlobReference> BlobReferences { get; }

        public override bool Equals(object? obj)
        {
            return obj is Snapshot other
                && CreationTime == other.CreationTime
                && BlobReferences.SequenceEqual(other.BlobReferences);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CreationTime, BlobReferences);
        }
    }
}
