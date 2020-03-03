using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Chunkyard.Core;

namespace Chunkyard
{
    public class Snapshot<T> where T : IContentRef
    {
        public Snapshot(DateTime creationTime, IEnumerable<T> contentRefs)
        {
            CreationTime = creationTime;
            ContentRefs = contentRefs.ToImmutableArray();
        }

        public DateTime CreationTime { get; }

        public ImmutableArray<T> ContentRefs { get; }
    }
}
