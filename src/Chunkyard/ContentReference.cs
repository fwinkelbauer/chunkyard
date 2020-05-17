using System.Collections.Generic;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(
            string name,
            IEnumerable<ChunkReference> chunks)
        {
            Name = name;
            Chunks = new List<ChunkReference>(chunks);
        }

        public string Name { get; }

        public IEnumerable<ChunkReference> Chunks { get; }

    }
}
