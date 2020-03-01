using System.Collections.Generic;

namespace Chunkyard.Core
{
    public class FastCdcContentRef<T> : IContentRef where T : IContentRef
    {
        public FastCdcContentRef(string name, IEnumerable<T> chunkedContentRefs)
        {
            Name = name;
            ChunkedContentRefs = new List<T>(chunkedContentRefs);
        }

        public string Name { get; }

        public IReadOnlyCollection<T> ChunkedContentRefs { get; }
    }
}
