using System;
using System.Collections.Immutable;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several files at a specific point in time.
    /// </summary>
    public class Snapshot
    {
        public Snapshot(
            DateTime creationTime,
            IImmutableList<ContentReference> contentReferences)
        {
            CreationTime = creationTime;
            ContentReferences = contentReferences;
        }

        public DateTime CreationTime { get; }

        public IImmutableList<ContentReference> ContentReferences { get; }

        public override bool Equals(object? obj)
        {
            return obj is Snapshot snapshot
                && CreationTime == snapshot.CreationTime
                && ContentReferences.SequenceEqual(snapshot.ContentReferences);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CreationTime, ContentReferences);
        }
    }
}
