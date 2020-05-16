using System.Collections.Generic;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(
            string name,
            IEnumerable<Chunk> chunks,
            byte[] salt,
            int iterations)
        {
            Name = name;
            Chunks = new List<Chunk>(chunks);
            Salt = salt;
            Iterations = iterations;
        }

        public string Name { get; }

        public IEnumerable<Chunk> Chunks { get; }

        public byte[] Salt { get; }

        public int Iterations { get; }

    }
}
