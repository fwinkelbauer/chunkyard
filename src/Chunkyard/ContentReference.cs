using System.Collections.Generic;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(
            string name,
            IEnumerable<ChunkReference> chunks,
            byte[] salt,
            int iterations)
        {
            Name = name;
            Chunks = new List<ChunkReference>(chunks);
            Salt = salt;
            Iterations = iterations;
        }

        public string Name { get; }

        public IEnumerable<ChunkReference> Chunks { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

    }
}
