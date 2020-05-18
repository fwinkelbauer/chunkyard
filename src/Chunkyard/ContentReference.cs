using System.Collections.Generic;

namespace Chunkyard
{
    public class ContentReference
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
