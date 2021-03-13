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
        private readonly Dictionary<string, BlobReference> _lookup;

        public Snapshot(
            DateTime creationTime,
            IReadOnlyCollection<BlobReference> blobReferences)
        {
            CreationTime = creationTime;
            BlobReferences = blobReferences;

            _lookup = new Dictionary<string, BlobReference>();

            foreach (var blobReference in BlobReferences)
            {
                _lookup[blobReference.Name] = blobReference;
            }
        }

        public DateTime CreationTime { get; }

        public IReadOnlyCollection<BlobReference> BlobReferences { get; }

        public BlobReference? Find(string blobName)
        {
            return _lookup.TryGetValue(blobName, out var blob)
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
    }
}
