using System;
using System.Collections.Generic;

namespace Chunkyard
{
    internal class Snapshot
    {
        public Snapshot(DateTime creationTime, IEnumerable<ContentReference> contentReferences)
        {
            CreationTime = creationTime;
            ContentReferences = new List<ContentReference>(contentReferences);
        }

        public DateTime CreationTime { get; }

        public IReadOnlyCollection<ContentReference> ContentReferences { get; }
    }
}
