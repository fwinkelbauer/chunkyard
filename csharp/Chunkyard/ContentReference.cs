using System.Collections.Generic;
using System.Collections.Immutable;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(string name, byte[] nonce, IEnumerable<Chunk> chunks)
        {
            Name = name;
            Nonce = nonce;
            Chunks = chunks.ToImmutableArray();
        }

        public string Name { get; }

        public byte[] Nonce { get; }

        public ImmutableArray<Chunk> Chunks { get; }
    }
}
