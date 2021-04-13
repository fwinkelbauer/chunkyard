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
            DateTime creationTime,
            IReadOnlyCollection<BlobReference> blobReferences)
        {
            blobReferences.EnsureNotNull(nameof(blobReferences));

            CreationTime = creationTime;

            _blobReferences = new Dictionary<string, BlobReference>();

            foreach (var blobReference in blobReferences)
            {
                _blobReferences[blobReference.Name] = blobReference;
            }
        }

        public DateTime CreationTime { get; }

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
                && CreationTime == other.CreationTime
                && BlobReferences.SequenceEqual(other.BlobReferences);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CreationTime, BlobReferences);
        }

        public static DiffSet Diff(Snapshot snapshot1, Snapshot snapshot2)
        {
            snapshot1.EnsureNotNull(nameof(snapshot1));
            snapshot2.EnsureNotNull(nameof(snapshot2));

            var names1 = snapshot1.BlobReferences.Select(b => b.Name).ToArray();
            var names2 = snapshot2.BlobReferences.Select(b => b.Name).ToArray();

            var changed = names1.Intersect(names2)
                .Where(
                    name =>
                    {
                        var blob1 = snapshot1.Find(name);
                        var blob2 = snapshot2.Find(name);

                        return !blob1!.Equals(blob2);
                    })
                .ToArray();

            return new DiffSet(
                names2.Except(names1).ToArray(),
                changed,
                names1.Except(names2).ToArray());
        }
    }
}
