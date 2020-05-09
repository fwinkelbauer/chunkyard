using System.Collections.Generic;
using System.Collections.Immutable;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(string name, IEnumerable<Chunk> chunks)
        {
            Name = name;
            Chunks = chunks.ToImmutableArray();
        }

        public string Name { get; }

        public ImmutableArray<Chunk> Chunks { get; }
    }
}
