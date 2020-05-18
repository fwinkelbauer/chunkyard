using System.Collections.Generic;
using System.Linq;

namespace Chunkyard
{
    public class ContentReference
    {
        public ContentReference(
            string name,
            IEnumerable<ChunkReference> chunks)
        {
            Name = name;
            Chunks = chunks.ToList();
        }

        public string Name { get; }

        public IEnumerable<ChunkReference> Chunks { get; }

    }
}
