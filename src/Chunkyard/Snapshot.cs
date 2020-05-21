using System;
using System.Collections.Generic;

namespace Chunkyard
{
    /// <summary>
    /// A snapshot contains a list of references which describe the state of
    /// several files at a specific point in time.
    /// </summary>
    internal class Snapshot
    {
        public Snapshot(
            DateTime creationTime,
            IEnumerable<ContentReference> contentReferences)
        {
            CreationTime = creationTime;
            ContentReferences = new List<ContentReference>(contentReferences);
        }

        public DateTime CreationTime { get; }

        public IEnumerable<ContentReference> ContentReferences { get; }
    }
}
