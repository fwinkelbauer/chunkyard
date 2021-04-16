using System;
using System.Collections.Generic;
using System.Linq;

namespace Chunkyard.Core
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several files at a specific point in time.
    /// </summary>
    public class Snapshot
    {
        private readonly Dictionary<string, BlobReference> _blobReferences;

        public Snapshot(
            int snapshotId,
            DateTime creationTimeUtc,
            IReadOnlyCollection<BlobReference> blobReferences)
        {
            blobReferences.EnsureNotNull(nameof(blobReferences));

            SnapshotId = snapshotId;
            CreationTimeUtc = creationTimeUtc;

            _blobReferences = new Dictionary<string, BlobReference>();

            foreach (var blobReference in blobReferences)
            {
                _blobReferences[blobReference.Name] = blobReference;
            }
        }

        public int SnapshotId { get; }

        public DateTime CreationTimeUtc { get; }

        public IReadOnlyCollection<BlobReference> BlobReferences
            => _blobReferences.Values;

        public BlobReference? Find(string blobName)
        {
            return _blobReferences.TryGetValue(blobName, out var blob)
                ? blob
                : null;
        }

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

        public static DiffSet Diff(Snapshot snapshot1, Snapshot snapshot2)
        {
            snapshot1.EnsureNotNull(nameof(snapshot1));
            snapshot2.EnsureNotNull(nameof(snapshot2));

            var names1 = snapshot1._blobReferences.Keys;
            var names2 = snapshot2._blobReferences.Keys;

            var changed = names1.Intersect(names2)
                .Where(name =>
                {
                    var blob1 = snapshot1._blobReferences[name];
                    var blob2 = snapshot2._blobReferences[name];

                    return !blob1.Equals(blob2);
                })
                .ToArray();

            return new DiffSet(
                names2.Except(names1).ToArray(),
                changed,
                names1.Except(names2).ToArray());
        }
    }
}
