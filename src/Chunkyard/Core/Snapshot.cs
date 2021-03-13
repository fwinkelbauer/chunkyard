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
    }
}
