using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Chunkyard
{
    internal class Snapshot
    {
        public Snapshot(DateTime creationTime, string hostname, IEnumerable<ContentReference> contentReferences)
        {
            CreationTime = creationTime;
            Hostname = hostname ?? string.Empty;
            ContentReferences = contentReferences.ToImmutableArray();
        }

        public DateTime CreationTime { get; }

        public string Hostname { get; }

        public ImmutableArray<ContentReference> ContentReferences { get; }
    }
}
