using System.Collections.Generic;
using System.Collections.Immutable;

namespace Chunkyard
{
    internal class ContentReference
    {
        public ContentReference(string contentName, byte[] nonce, IEnumerable<Chunk> chunks)
        {
            ContentName = contentName;
            Nonce = nonce;
            Chunks = chunks.ToImmutableArray();
        }

        public string ContentName { get; }

        public byte[] Nonce { get; }

        public ImmutableArray<Chunk> Chunks { get; }
    }
}
