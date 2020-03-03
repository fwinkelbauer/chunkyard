using System.Collections.Generic;
using System.Collections.Immutable;

namespace Chunkyard.Core
{
    public class FastCdcContentRef<T> : IContentRef where T : IContentRef
    {
        public FastCdcContentRef(string name, IEnumerable<T> chunkedContentRefs)
        {
            Name = name;
            ChunkedContentRefs = chunkedContentRefs.ToImmutableArray();
        }

        public string Name { get; }

        public ImmutableArray<T> ChunkedContentRefs { get; }
    }
}
