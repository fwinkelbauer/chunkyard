using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Chunkyard
{
    internal class Snapshot
    {
        public Snapshot(DateTime creationTime, IEnumerable<ContentReference> contentReferences)
        {
            CreationTime = creationTime;
            ContentReferences = contentReferences.ToImmutableArray();
        }

        public DateTime CreationTime { get; }

        public ImmutableArray<ContentReference> ContentReferences { get; }
    }
}
