using System.Collections.Generic;

namespace Chunkyard
{
    public class ContentReference
    {
        public ContentReference(
            string name,
            byte[] nonce,
            IEnumerable<ChunkReference> chunks)
        {
            Name = name;
            Nonce = nonce;
            Chunks = new List<ChunkReference>(chunks);
        }

        public string Name { get; }

        public byte[] Nonce { get; }

        public IEnumerable<ChunkReference> Chunks { get; }

    }
}
