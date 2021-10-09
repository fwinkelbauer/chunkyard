using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several blobs at a specific point in time.
    /// </summary>
    public class Snapshot
    {
        public Snapshot(
            int snapshotId,
            DateTime creationTimeUtc,
            IReadOnlyCollection<BlobReference> blobReferences)
        {
            SnapshotId = snapshotId;
            CreationTimeUtc = creationTimeUtc;
            BlobReferences = blobReferences;
        }

        public int SnapshotId { get; }

        public DateTime CreationTimeUtc { get; }

        public IReadOnlyCollection<BlobReference> BlobReferences { get; }

        public override bool Equals(object? obj)
        {
            return obj is Snapshot other
                && SnapshotId == other.SnapshotId
                && CreationTimeUtc == other.CreationTimeUtc
                && BlobReferences.SequenceEqual(other.BlobReferences);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                SnapshotId,
                CreationTimeUtc,
                BlobReferences);
        }
    }
}
