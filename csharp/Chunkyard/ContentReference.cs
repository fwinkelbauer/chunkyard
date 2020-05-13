using System.Collections.Generic;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(string name, IEnumerable<Chunk> chunks)
        {
            Name = name;
            Chunks = new List<Chunk>(chunks);
        }

        public string Name { get; }

        public IEnumerable<Chunk> Chunks { get; }
    }
}
