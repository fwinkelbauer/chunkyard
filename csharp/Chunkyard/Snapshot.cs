using System;
using System.Collections.Generic;

namespace Chunkyard
{
    public class Snapshot<T> where T : IContentRef
    {
        public Snapshot(DateTime creationTime, IEnumerable<T> contentRefs)
        {
            CreationTime = creationTime;
            ContentRefs = new List<T>(contentRefs);
        }

        public DateTime CreationTime { get; }

        public IReadOnlyCollection<T> ContentRefs { get; }
    }
}
